using System;
using System.Collections.Generic;
using System.Linq;

namespace Nikse.SubtitleEdit.Core.Common
{
    public sealed class SpeakerProfileCollection
    {
        public List<SpeakerProfile> Profiles { get; }

        public SpeakerProfileCollection()
        {
            Profiles = new List<SpeakerProfile>();
        }

        public SpeakerProfileCollection(SpeakerProfileCollection collection)
        {
            Profiles = collection?.Profiles.Select(profile => new SpeakerProfile(profile)).ToList() ??
                       new List<SpeakerProfile>();
        }

        public SpeakerProfile FindByActor(string actor)
        {
            var normalizedActor = NormalizeActor(actor);
            if (normalizedActor.Length == 0)
            {
                return null;
            }

            return Profiles.FirstOrDefault(profile =>
                string.Equals(NormalizeActor(profile.DisplayName), normalizedActor, StringComparison.OrdinalIgnoreCase));
        }

        public SpeakerGender GetGender(string actor)
        {
            return FindByActor(actor)?.Gender ?? SpeakerGender.Unknown;
        }

        public SpeakerProfile AddOrUpdate(SpeakerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var existing = FindById(profile.Id) ?? FindByActor(profile.DisplayName);
            if (existing == null)
            {
                var copy = new SpeakerProfile(profile);
                Profiles.Add(copy);
                return copy;
            }

            existing.DisplayName = profile.DisplayName;
            existing.Gender = profile.Gender;
            existing.Confidence = profile.Confidence;
            existing.Source = profile.Source;
            return existing;
        }

        private SpeakerProfile FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return Profiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static string NormalizeActor(string actor)
        {
            return string.Join(" ", (actor ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
