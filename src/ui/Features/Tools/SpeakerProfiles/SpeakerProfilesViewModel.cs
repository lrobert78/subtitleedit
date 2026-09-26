using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Logic;
using Nikse.SubtitleEdit.Logic.Config;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Nikse.SubtitleEdit.Features.Tools.SpeakerProfiles;

public partial class SpeakerProfilesViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<SpeakerProfileRow> _rows;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlaySampleCommand))]
    private SpeakerProfileRow? _selectedRow;
    [ObservableProperty] private string _summaryText;
    [ObservableProperty] private bool _isPlayVisible;

    private Action<int>? _playLine;
    private Action? _stopPlayback;
    private bool _hasPlayed;

    public Window? Window { get; set; }
    public bool OkPressed { get; private set; }
    public SpeakerProfileCollection ResultProfiles { get; private set; }
    public Dictionary<string, string> RenamedSpeakers { get; private set; }

    public SpeakerProfilesViewModel()
    {
        _rows = new ObservableCollection<SpeakerProfileRow>();
        _summaryText = string.Empty;
        ResultProfiles = new SpeakerProfileCollection();
        RenamedSpeakers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public void Initialize(Subtitle subtitle, Action<int>? playLine = null, Action? stopPlayback = null)
    {
        _playLine = playLine;
        _stopPlayback = stopPlayback;
        _hasPlayed = false;
        IsPlayVisible = playLine != null;
        var rows = new List<SpeakerProfileRow>();
        var actors = subtitle.Paragraphs
            .Select((paragraph, index) => (paragraph, index))
            .Where(item => !string.IsNullOrWhiteSpace(item.paragraph.Actor))
            .GroupBy(item => SpeakerProfileCollection.NormalizeActor(item.paragraph.Actor!), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var representedProfileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var actor in actors)
        {
            var existing = subtitle.SpeakerProfiles.FindByActor(actor.Key);
            var profile = existing == null
                ? new SpeakerProfile { DisplayName = actor.Key }
                : new SpeakerProfile(existing);
            profile.DisplayName = actor.Key;
            representedProfileIds.Add(profile.Id);
            rows.Add(new SpeakerProfileRow(profile, actor.Key, actor.Count(), ChooseSampleParagraphIndex(actor)));
        }

        foreach (var profile in subtitle.SpeakerProfiles.Profiles
                     .Where(profile => !representedProfileIds.Contains(profile.Id))
                     .OrderBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new SpeakerProfileRow(new SpeakerProfile(profile), profile.DisplayName, 0));
        }

        Rows = new ObservableCollection<SpeakerProfileRow>(rows);
        SelectedRow = Rows.FirstOrDefault();
        SummaryText = string.Format(
            Se.Language.Tools.SpeakerProfiles.SummaryXSpeakersYLines,
            actors.Count,
            actors.Sum(group => group.Count()));
    }

    private static int? ChooseSampleParagraphIndex(IEnumerable<(Paragraph paragraph, int index)> lines)
    {
        var candidates = lines
            .Where(item => item.paragraph.DurationTotalSeconds > 0)
            .OrderBy(item => item.index)
            .ToList();
        var shortSample = candidates
            .Where(item => item.paragraph.DurationTotalSeconds is >= 0.5 and <= 8)
            .OrderByDescending(item => item.paragraph.DurationTotalSeconds)
            .ThenBy(item => item.index)
            .FirstOrDefault();
        if (shortSample.paragraph != null)
        {
            return shortSample.index;
        }

        return candidates
            .OrderBy(item => Math.Abs(item.paragraph.DurationTotalSeconds - 4))
            .ThenBy(item => item.index)
            .Select(item => (int?)item.index)
            .FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanPlaySample))]
    private void PlaySample()
    {
        if (SelectedRow?.SampleParagraphIndex is not int index || _playLine == null)
        {
            return;
        }

        _hasPlayed = true;
        _playLine(index);
    }

    private bool CanPlaySample() => _playLine != null && SelectedRow?.SampleParagraphIndex != null;

    public void OnClosing()
    {
        if (_hasPlayed)
        {
            _stopPlayback?.Invoke();
        }
    }

    [RelayCommand]
    private void Ok()
    {
        var profiles = new SpeakerProfileCollection();
        var renamed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in Rows)
        {
            var name = string.IsNullOrWhiteSpace(row.Name) ? row.OriginalName : row.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.OriginalName))
            {
                renamed[row.OriginalName] = name;
            }

            var genderChanged = row.Gender != row.OriginalGender;
            profiles.AddOrUpdate(new SpeakerProfile
            {
                Id = row.Id,
                DisplayName = name,
                Gender = row.Gender,
                Confidence = genderChanged ? null : row.Confidence,
                Source = genderChanged ? "manual" : row.Source,
            });
        }

        ResultProfiles = profiles;
        RenamedSpeakers = renamed;
        OkPressed = true;
        Window?.Close();
    }

    [RelayCommand]
    private void Cancel() => Window?.Close();

    internal void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Window?.Close();
        }
    }
}
