using Nikse.SubtitleEdit.Core.Common;

namespace LibSETests.Common;

public class SpeakerOverlapAssignerTest
{
    [Fact]
    public void GreatestOverlapAssignsSpeakerWithoutChangingSubtitle()
    {
        var line = new Paragraph("A translated line", 1000, 3000) { Actor = "Reviewed actor" };
        var originalId = line.Id;
        var segments = new[]
        {
            new SpeakerDiarizationSegment(0, 1500, "speaker-1"),
            new SpeakerDiarizationSegment(1500, 3200, "speaker-2"),
        };

        var assigned = SpeakerOverlapAssigner.Assign([line], segments);

        Assert.Equal("speaker-2", assigned[line]);
        Assert.Equal("A translated line", line.Text);
        Assert.Equal("Reviewed actor", line.Actor);
        Assert.Equal(1000, line.StartTime.TotalMilliseconds);
        Assert.Equal(3000, line.EndTime.TotalMilliseconds);
        Assert.Equal(originalId, line.Id);
    }

    [Fact]
    public void OverlappingIntervalsForOneSpeakerAreCountedOnce()
    {
        var line = new Paragraph("Line", 0, 1000);
        var segments = new[]
        {
            new SpeakerDiarizationSegment(0, 400, "speaker-1"),
            new SpeakerDiarizationSegment(0, 400, "speaker-1"),
            new SpeakerDiarizationSegment(450, 1000, "speaker-2"),
        };

        var assigned = SpeakerOverlapAssigner.Assign([line], segments);

        Assert.Equal("speaker-2", assigned[line]);
    }

    [Fact]
    public void EqualOverlapAndNoOverlapRemainUnassigned()
    {
        var tied = new Paragraph("Tie", 0, 1000);
        var silent = new Paragraph("Silent", 2000, 3000);
        var segments = new[]
        {
            new SpeakerDiarizationSegment(0, 500, "speaker-1"),
            new SpeakerDiarizationSegment(500, 1000, "speaker-2"),
        };

        var assigned = SpeakerOverlapAssigner.Assign([tied, silent], segments);

        Assert.Empty(assigned);
    }
}
