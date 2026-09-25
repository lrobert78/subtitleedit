using Nikse.SubtitleEdit.Core.Common;

namespace LibSETests.Common;

public class SpeakerProfileCollectionTest
{
    [Fact]
    public void FindByActor_IgnoresCaseAndRepeatedWhitespace()
    {
        var profiles = new SpeakerProfileCollection();
        profiles.AddOrUpdate(new SpeakerProfile
        {
            DisplayName = "Speaker  1",
            Gender = SpeakerGender.Female,
        });

        var profile = profiles.FindByActor("  speaker 1 ");

        Assert.NotNull(profile);
        Assert.Equal(SpeakerGender.Female, profile.Gender);
    }

    [Fact]
    public void AddOrUpdate_UsesTheStableIdBeforeTheDisplayName()
    {
        var profiles = new SpeakerProfileCollection();
        var original = profiles.AddOrUpdate(new SpeakerProfile
        {
            Id = "speaker-1",
            DisplayName = "Speaker 1",
            Gender = SpeakerGender.Unknown,
        });

        var updated = profiles.AddOrUpdate(new SpeakerProfile
        {
            Id = "speaker-1",
            DisplayName = "Anna",
            Gender = SpeakerGender.Female,
        });

        Assert.Same(original, updated);
        Assert.Single(profiles.Profiles);
        Assert.Equal("Anna", updated.DisplayName);
        Assert.Equal(SpeakerGender.Female, updated.Gender);
    }

    [Fact]
    public void SubtitleCopyConstructor_DeepCopiesSpeakerProfiles()
    {
        var subtitle = new Subtitle();
        subtitle.SpeakerProfiles.AddOrUpdate(new SpeakerProfile
        {
            Id = "speaker-1",
            DisplayName = "Speaker 1",
            Gender = SpeakerGender.Male,
        });

        var copy = new Subtitle(subtitle);
        copy.SpeakerProfiles.Profiles[0].DisplayName = "Changed";

        Assert.NotSame(subtitle.SpeakerProfiles, copy.SpeakerProfiles);
        Assert.Equal("Speaker 1", subtitle.SpeakerProfiles.Profiles[0].DisplayName);
        Assert.Equal("Changed", copy.SpeakerProfiles.Profiles[0].DisplayName);
    }

    [Fact]
    public void ReloadLoadSubtitle_ClearsProfilesFromThePreviousFile()
    {
        var subtitle = new Subtitle();
        subtitle.SpeakerProfiles.AddOrUpdate(new SpeakerProfile { DisplayName = "Speaker 1" });

        subtitle.ReloadLoadSubtitle(
            ["1", "00:00:00,000 --> 00:00:01,000", "Hello"],
            "next.srt",
            new Nikse.SubtitleEdit.Core.SubtitleFormats.SubRip());

        Assert.Empty(subtitle.SpeakerProfiles.Profiles);
    }
}
