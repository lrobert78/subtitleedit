using Avalonia.Platform;
using Nikse.SubtitleEdit.Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nikse.SubtitleEdit.Features.Tools.SpeakerProfiles;

internal static class SpeakerGenderSuggestionRunner
{
    private sealed record Sample(double Start, double Duration);
    private sealed record SpeakerRequest(string Actor, List<Sample> Samples);
    private sealed record Request(string Video, string Ffmpeg, int? AudioTrackIndex, List<SpeakerRequest> Speakers);
    private sealed record Prediction(string Actor, double FemaleProbability, int Samples);
    private sealed record Response(List<Prediction> Predictions);

    internal sealed record Suggestion(string Actor, SpeakerGender Gender, double Confidence);

    internal static async Task<List<Suggestion>> RunAsync(
        Subtitle subtitle, string videoFileName, string ffmpegFileName, int? audioTrackIndex,
        CancellationToken cancellationToken)
    {
        var requests = BuildRequests(subtitle);
        if (requests.Count == 0)
        {
            return new List<Suggestion>();
        }

        var scriptPath = Path.Combine(Path.GetTempPath(), $"subtitleedit-gender-{Guid.NewGuid():N}.py");
        try
        {
            var asset = new Uri("avares://SubtitleEdit/Assets/SpeechToText/SpeakerGenderSuggestions.py");
            await using (var input = AssetLoader.Open(asset))
            await using (var output = File.Create(scriptPath))
            {
                await input.CopyToAsync(output, cancellationToken);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("SUBTITLEEDIT_GENDER_PYTHON") is { Length: > 0 } pythonPath
                    ? pythonPath
                    : OperatingSystem.IsWindows() ? "python" : "python3",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add(scriptPath);
            using var process = new Process { StartInfo = startInfo };
            try
            {
                process.Start();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Python 3 is required. Set SUBTITLEEDIT_GENDER_PYTHON to a Python environment with the classifier packages.",
                    exception);
            }

            using var registration = cancellationToken.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
            });
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(
                new Request(videoFileName, ffmpegFileName, audioTrackIndex, requests)));
            process.StandardInput.Close();
            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr)
                    ? "The local voice classifier failed."
                    : stderr.Trim().Split('\n').Last().Trim());
            }

            var response = JsonSerializer.Deserialize<Response>(stdout);
            return FilterSuggestions(response?.Predictions ?? new List<Prediction>());
        }
        finally
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }
    }

    private static List<SpeakerRequest> BuildRequests(Subtitle subtitle)
    {
        var assigned = subtitle.Paragraphs
            .Where(p => !string.IsNullOrWhiteSpace(p.Actor))
            .ToList();
        return assigned
            .Where(p => p.DurationTotalSeconds is >= 3 and <= 8)
            .GroupBy(p => SpeakerProfileCollection.NormalizeActor(p.Actor!), StringComparer.OrdinalIgnoreCase)
            .Where(group => subtitle.SpeakerProfiles.FindByActor(group.Key)?.Gender is null or SpeakerGender.Unknown)
            .Select(group => new SpeakerRequest(group.Key, group
                .OrderByDescending(p => p.DurationTotalSeconds)
                .ThenBy(p => p.StartTime.TotalSeconds)
                .Where(p => !assigned.Any(other =>
                    !ReferenceEquals(p, other) &&
                    other.StartTime.TotalSeconds < p.EndTime.TotalSeconds &&
                    other.EndTime.TotalSeconds > p.StartTime.TotalSeconds &&
                    Math.Min(p.EndTime.TotalSeconds, other.EndTime.TotalSeconds) -
                    Math.Max(p.StartTime.TotalSeconds, other.StartTime.TotalSeconds) > 0.25 &&
                    !string.Equals(SpeakerProfileCollection.NormalizeActor(p.Actor!),
                        SpeakerProfileCollection.NormalizeActor(other.Actor!), StringComparison.OrdinalIgnoreCase)))
                .Take(3)
                .Select(p => new Sample(p.StartTime.TotalSeconds, 3))
                .ToList()))
            .Where(request => request.Samples.Count >= 2)
            .ToList();
    }

    private static List<Suggestion> FilterSuggestions(IEnumerable<Prediction> predictions)
    {
        return predictions
            .Where(p => p.Samples >= 2 && double.IsFinite(p.FemaleProbability) &&
                        p.FemaleProbability is >= 0 and <= 1)
            .Where(p => p.FemaleProbability is <= 0.1 or >= 0.9)
            .Select(p => new Suggestion(p.Actor,
                p.FemaleProbability >= 0.9 ? SpeakerGender.Female : SpeakerGender.Male,
                Math.Max(p.FemaleProbability, 1 - p.FemaleProbability)))
            .ToList();
    }
}
