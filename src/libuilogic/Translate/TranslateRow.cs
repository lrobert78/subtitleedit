using CommunityToolkit.Mvvm.ComponentModel;
using Nikse.SubtitleEdit.Core.Common;

namespace Nikse.SubtitleEdit.UiLogic.Translate;

public partial class TranslateRow : ObservableObject
{
    public int Number { get; set; }
    public TimeSpan Show { get; set; }
    public TimeSpan Hide { get; set; }
    public string Duration { get; set; }
    public string Text { get; set; }
    public string SpeakerId { get; set; }
    public string Actor { get; set; }
    public SpeakerGender Gender { get; set; }
    [ObservableProperty] private string _translatedText;

    public double DurationTotalMilliseconds => (Hide - Show).TotalMilliseconds;

    public TranslateRow()
    {
        Duration = string.Empty;
        Text = string.Empty;
        SpeakerId = string.Empty;
        Actor = string.Empty;
        TranslatedText = string.Empty;
    }

    public static TranslateRow FromParagraph(Paragraph paragraph, SpeakerProfileCollection profiles)
    {
        var actor = paragraph.Actor ?? string.Empty;
        var profile = profiles.FindByActor(actor);
        return new TranslateRow
        {
            Number = paragraph.Number,
            Show = paragraph.StartTime.TimeSpan,
            Hide = paragraph.EndTime.TimeSpan,
            Duration = paragraph.Duration.ToShortDisplayString(),
            Text = paragraph.Text,
            SpeakerId = profile?.Id ?? string.Empty,
            Actor = actor,
            Gender = profile?.Gender ?? SpeakerGender.Unknown,
        };
    }
}