using System;

namespace Nikse.SubtitleEdit.Core.Common
{
    public sealed class SpeakerProfile
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public SpeakerGender Gender { get; set; }
        public double? Confidence { get; set; }
        public string Source { get; set; }

        public SpeakerProfile()
        {
            Id = Guid.NewGuid().ToString("N");
            DisplayName = string.Empty;
            Source = string.Empty;
        }

        public SpeakerProfile(SpeakerProfile profile)
        {
            Id = profile.Id;
            DisplayName = profile.DisplayName;
            Gender = profile.Gender;
            Confidence = profile.Confidence;
            Source = profile.Source;
        }
    }
}
