using System.Globalization;
using System.Text;
using System.Text.Json;
using Nikse.SubtitleEdit.Core.Common;

namespace Nikse.SubtitleEdit.UiLogic.Translate;

public static class ContextBatchTranslationProtocol
{
    public const string Instructions =
        "\n\nThe user message is JSON. History contains recent source/translation pairs for context only. " +
        "Lines contains the subtitles to translate. Optional speakerId, speaker, and gender fields are metadata: " +
        "use them for pronouns, grammatical gender, tone, and consistent address, but never translate or alter them. " +
        "Gender may be Unknown and must not be guessed. Answer with ONLY a JSON object mapping every requested " +
        "line number to its translated text, for example {\"1\":\"...\",\"2\":\"...\"}. Include every number " +
        "exactly once and no additional keys.";

    public sealed record HistoryItem(
        string Source,
        string Translation,
        string SpeakerId,
        string Speaker,
        SpeakerGender Gender);

    public sealed record Line(
        int Number,
        string Text,
        string SpeakerId,
        string Speaker,
        SpeakerGender Gender);

    public static string BuildSystemPrompt(string prompt, string sourceLanguage, string targetLanguage)
    {
        return (prompt ?? string.Empty)
                   .Replace("{0}", sourceLanguage)
                   .Replace("{1}", targetLanguage)
                   .Trim() + Instructions;
    }

    public static string BuildUserContent(IReadOnlyList<HistoryItem> history, IReadOnlyList<Line> lines)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("history");
            foreach (var item in history)
            {
                writer.WriteStartObject();
                writer.WriteString("source", ToWireText(item.Source));
                writer.WriteString("translation", ToWireText(item.Translation));
                WriteSpeakerMetadata(writer, item.SpeakerId, item.Speaker, item.Gender);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteStartArray("lines");
            foreach (var line in lines)
            {
                writer.WriteStartObject();
                writer.WriteNumber("n", line.Number);
                writer.WriteString("text", ToWireText(line.Text));
                WriteSpeakerMetadata(writer, line.SpeakerId, line.Speaker, line.Gender);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static Dictionary<int, string> ParseTranslations(string responseText)
    {
        var result = new Dictionary<int, string>();
        var json = ExtractJsonObject(responseText);
        if (json == null)
        {
            return result;
        }

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String ||
                !int.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ||
                !result.TryAdd(number, FromWireText(property.Value.GetString())))
            {
                result.Clear();
                return result;
            }
        }

        return result;
    }

    public static bool IsComplete(IReadOnlyDictionary<int, string> translations, IReadOnlyList<Line> lines)
    {
        if (translations.Count != lines.Count)
        {
            return false;
        }

        foreach (var line in lines)
        {
            if (!translations.TryGetValue(line.Number, out var translation) ||
                translation.Length == 0 && !string.IsNullOrWhiteSpace(line.Text))
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteSpeakerMetadata(Utf8JsonWriter writer, string speakerId, string speaker, SpeakerGender gender)
    {
        if (!string.IsNullOrWhiteSpace(speakerId))
        {
            writer.WriteString("speakerId", speakerId);
        }

        if (!string.IsNullOrWhiteSpace(speaker))
        {
            writer.WriteString("speaker", speaker);
        }

        if (gender != SpeakerGender.Unknown || !string.IsNullOrWhiteSpace(speakerId) || !string.IsNullOrWhiteSpace(speaker))
        {
            writer.WriteString("gender", gender.ToString());
        }
    }

    private static string ToWireText(string text) => (text ?? string.Empty).Replace(Environment.NewLine, "\n");

    private static string FromWireText(string? text) =>
        (text ?? string.Empty).Replace("\r\n", "\n").Replace("\n", Environment.NewLine).Trim();

    private static string? ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        var candidate = text.Substring(start, end - start + 1);
        try
        {
            using var _ = JsonDocument.Parse(candidate);
            return candidate;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
