using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Features.Tools.SpeakerProfiles;

namespace UITests.Features.Tools.SpeakerProfiles;

public class SpeakerProfilesViewModelTests
{
    [Fact]
    public void Initialize_AddsActorsAndKeepsExistingMetadata()
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph("One", 0, 1000) { Actor = " Speaker  1 " });
        subtitle.Paragraphs.Add(new Paragraph("Two", 1000, 2000) { Actor = "Speaker 1" });
        subtitle.SpeakerProfiles.AddOrUpdate(new SpeakerProfile
        {
            Id = "speaker-1",
            DisplayName = "Speaker 1",
            Gender = SpeakerGender.Female,
            Confidence = 0.9,
            Source = "classifier",
        });

        var vm = new SpeakerProfilesViewModel();
        vm.Initialize(subtitle);

        var row = Assert.Single(vm.Rows);
        Assert.Equal("speaker-1", row.Id);
        Assert.Equal(2, row.LineCount);
        Assert.Equal(SpeakerGender.Female, row.Gender);
        Assert.Equal("90%", row.ConfidenceDisplay);
    }

    [Fact]
    public void Ok_ReturnsRenameAndMarksChangedGenderAsManual()
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph("One", 0, 1000) { Actor = "Speaker 1" });

        var vm = new SpeakerProfilesViewModel();
        vm.Initialize(subtitle);
        vm.Rows[0].Name = "Anna";
        vm.Rows[0].Gender = SpeakerGender.Female;

        vm.OkCommand.Execute(null);

        Assert.True(vm.OkPressed);
        Assert.Equal("Anna", vm.RenamedSpeakers["Speaker 1"]);
        var profile = Assert.Single(vm.ResultProfiles.Profiles);
        Assert.Equal("Anna", profile.DisplayName);
        Assert.Equal(SpeakerGender.Female, profile.Gender);
        Assert.Equal("manual", profile.Source);
        Assert.Null(profile.Confidence);
    }
}
