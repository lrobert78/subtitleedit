using System.Threading;
using System.Threading.Tasks;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Features.Tools.SpeakerProfiles;

namespace UITests.Features.Tools.SpeakerProfiles;

public class SpeakerGenderSuggestionRunnerTests
{
    [Fact]
    public async Task ExistingReviewedGenderIsSkippedBeforeStartingClassifier()
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph("One", 0, 4000) { Actor = "Speaker 1" });
        subtitle.Paragraphs.Add(new Paragraph("Two", 5000, 9000) { Actor = "Speaker 1" });
        subtitle.SpeakerProfiles.AddOrUpdate(new SpeakerProfile
        {
            DisplayName = "Speaker 1",
            Gender = SpeakerGender.NonBinary,
            Source = "manual",
        });

        var suggestions = await SpeakerGenderSuggestionRunner.RunAsync(
            subtitle, "missing-video", "missing-ffmpeg", null, CancellationToken.None);

        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task OneUsableLineDoesNotStartClassifier()
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph("One", 0, 4000) { Actor = "Speaker 1" });
        subtitle.Paragraphs.Add(new Paragraph("Too short", 5000, 6000) { Actor = "Speaker 1" });

        var suggestions = await SpeakerGenderSuggestionRunner.RunAsync(
            subtitle, "missing-video", "missing-ffmpeg", null, CancellationToken.None);

        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task OverlappingOtherSpeakerIsNotUsedAsEvidence()
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph("One", 0, 4000) { Actor = "Speaker 1" });
        subtitle.Paragraphs.Add(new Paragraph("Two", 5000, 9000) { Actor = "Speaker 1" });
        subtitle.Paragraphs.Add(new Paragraph("Other voice", 6000, 8000) { Actor = "Speaker 2" });

        var suggestions = await SpeakerGenderSuggestionRunner.RunAsync(
            subtitle, "missing-video", "missing-ffmpeg", null, CancellationToken.None);

        Assert.Empty(suggestions);
    }
}
