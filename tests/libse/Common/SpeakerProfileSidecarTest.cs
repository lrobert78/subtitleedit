using Nikse.SubtitleEdit.Core.Common;

namespace LibSETests.Common;

public class SpeakerProfileSidecarTest
{
    [Fact]
    public void GetFileName_ReplacesOnlyTheSubtitleExtension()
    {
        var result = SpeakerProfileSidecar.GetFileName(Path.Combine("folder", "film.en.srt"));

        Assert.Equal(Path.Combine("folder", "film.en.speakers.json"), result);
    }

    [Fact]
    public void SaveAndTryLoad_RoundTripsProfiles()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var subtitleFileName = Path.Combine(directory, "film.en.srt");
            var profiles = new SpeakerProfileCollection();
            profiles.AddOrUpdate(new SpeakerProfile
            {
                Id = "speaker-1",
                DisplayName = "Anna \"A\"",
                Gender = SpeakerGender.Female,
                Confidence = 0.92,
                Source = "manual\nreview",
            });

            SpeakerProfileSidecar.Save(subtitleFileName, profiles);
            var loaded = SpeakerProfileSidecar.TryLoad(subtitleFileName, out var result, out var error);

            Assert.True(loaded, error);
            var profile = Assert.Single(result.Profiles);
            Assert.Equal("speaker-1", profile.Id);
            Assert.Equal("Anna \"A\"", profile.DisplayName);
            Assert.Equal(SpeakerGender.Female, profile.Gender);
            Assert.Equal(0.92, profile.Confidence);
            Assert.Equal("manual\nreview", profile.Source);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Save_AtomicallyReplacesAnExistingSidecar()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var subtitleFileName = Path.Combine(directory, "film.srt");
            var first = new SpeakerProfileCollection();
            first.AddOrUpdate(new SpeakerProfile { DisplayName = "Old" });
            SpeakerProfileSidecar.Save(subtitleFileName, first);

            var second = new SpeakerProfileCollection();
            second.AddOrUpdate(new SpeakerProfile { DisplayName = "New", Gender = SpeakerGender.Male });
            SpeakerProfileSidecar.Save(subtitleFileName, second);

            Assert.True(SpeakerProfileSidecar.TryLoad(subtitleFileName, out var loaded, out var error), error);
            var profile = Assert.Single(loaded.Profiles);
            Assert.Equal("New", profile.DisplayName);
            Assert.Equal(SpeakerGender.Male, profile.Gender);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void TryLoad_DamagedSidecarReturnsEmptyProfilesAndAnError()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var subtitleFileName = Path.Combine(directory, "film.srt");
            File.WriteAllText(SpeakerProfileSidecar.GetFileName(subtitleFileName), "not json");

            var loaded = SpeakerProfileSidecar.TryLoad(subtitleFileName, out var profiles, out var error);

            Assert.False(loaded);
            Assert.Empty(profiles.Profiles);
            Assert.NotEmpty(error);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Deserialize_RejectsAnUnsupportedSchemaVersion()
    {
        const string json = "{\"schemaVersion\":2,\"speakers\":[]}";

        var exception = Assert.Throws<NotSupportedException>(() => SpeakerProfileSidecar.Deserialize(json));

        Assert.Contains("2", exception.Message);
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SpeakerProfileSidecarTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
