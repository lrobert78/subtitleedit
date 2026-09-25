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
    [ObservableProperty] private SpeakerProfileRow? _selectedRow;
    [ObservableProperty] private string _summaryText;

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

    public void Initialize(Subtitle subtitle)
    {
        var rows = new List<SpeakerProfileRow>();
        var actors = subtitle.Paragraphs
            .Where(p => !string.IsNullOrWhiteSpace(p.Actor))
            .GroupBy(p => SpeakerProfileCollection.NormalizeActor(p.Actor!), StringComparer.OrdinalIgnoreCase)
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
            rows.Add(new SpeakerProfileRow(profile, actor.Key, actor.Count()));
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
