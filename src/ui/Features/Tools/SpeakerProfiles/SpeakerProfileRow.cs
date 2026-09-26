using CommunityToolkit.Mvvm.ComponentModel;
using Nikse.SubtitleEdit.Core.Common;

namespace Nikse.SubtitleEdit.Features.Tools.SpeakerProfiles;

public partial class SpeakerProfileRow : ObservableObject
{
    public string Id { get; }
    public string OriginalName { get; }
    public int LineCount { get; }
    public double? Confidence { get; }
    public string Source { get; }
    public SpeakerGender OriginalGender { get; }
    public int? SampleParagraphIndex { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private SpeakerGender _gender;
    [ObservableProperty] private SpeakerGender? _suggestedGender;
    [ObservableProperty] private double? _suggestedConfidence;

    public bool AcceptedSuggestion { get; private set; }

    public string ConfidenceDisplay => Confidence.HasValue ? $"{Confidence.Value:P0}" : string.Empty;
    public string SuggestionDisplay => SuggestedGender.HasValue && SuggestedConfidence.HasValue
        ? $"{SuggestedGender} ({SuggestedConfidence.Value:P0})"
        : string.Empty;

    partial void OnGenderChanged(SpeakerGender value) => AcceptedSuggestion = false;

    partial void OnSuggestedGenderChanged(SpeakerGender? value) => OnPropertyChanged(nameof(SuggestionDisplay));

    partial void OnSuggestedConfidenceChanged(double? value) => OnPropertyChanged(nameof(SuggestionDisplay));

    public void AcceptSuggestion()
    {
        if (Gender != SpeakerGender.Unknown || SuggestedGender == null)
        {
            return;
        }

        Gender = SuggestedGender.Value;
        AcceptedSuggestion = true;
    }

    public SpeakerProfileRow(SpeakerProfile profile, string originalName, int lineCount, int? sampleParagraphIndex = null)
    {
        Id = profile.Id;
        OriginalName = originalName;
        LineCount = lineCount;
        Confidence = profile.Confidence;
        Source = profile.Source;
        OriginalGender = profile.Gender;
        SampleParagraphIndex = sampleParagraphIndex;
        _name = profile.DisplayName;
        _gender = profile.Gender;
    }
}
