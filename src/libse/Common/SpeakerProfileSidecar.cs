using System;
using System.Globalization;
using System.IO;
using System.Text;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace Nikse.SubtitleEdit.Core.Common
{
    public static class SpeakerProfileSidecar
    {
        public const int CurrentSchemaVersion = 1;

        public static string GetFileName(string subtitleFileName)
        {
            if (string.IsNullOrWhiteSpace(subtitleFileName))
            {
                return string.Empty;
            }

            return Path.ChangeExtension(subtitleFileName, "speakers.json");
        }

        public static bool TryLoad(string subtitleFileName, out SpeakerProfileCollection profiles, out string error)
        {
            profiles = new SpeakerProfileCollection();
            error = string.Empty;
            var fileName = GetFileName(subtitleFileName);
            if (fileName.Length == 0 || !File.Exists(fileName))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(fileName, Encoding.UTF8);
                profiles = Deserialize(json);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                profiles = new SpeakerProfileCollection();
                return false;
            }
        }

        public static void Save(string subtitleFileName, SpeakerProfileCollection profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            var fileName = GetFileName(subtitleFileName);
            if (fileName.Length == 0)
            {
                throw new ArgumentException("A subtitle file name is required", nameof(subtitleFileName));
            }

            var directory = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryFileName = fileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporaryFileName, Serialize(profiles), new UTF8Encoding(false));
                if (File.Exists(fileName))
                {
                    File.Replace(temporaryFileName, fileName, null);
                }
                else
                {
                    File.Move(temporaryFileName, fileName);
                }
            }
            finally
            {
                if (File.Exists(temporaryFileName))
                {
                    File.Delete(temporaryFileName);
                }
            }
        }

        public static string Serialize(SpeakerProfileCollection profiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"schemaVersion\": " + CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture) + ",");
            sb.AppendLine("  \"speakers\": [");
            for (var i = 0; i < profiles.Profiles.Count; i++)
            {
                var profile = profiles.Profiles[i];
                sb.AppendLine("    {");
                sb.AppendLine("      \"id\": \"" + Encode(profile.Id) + "\",");
                sb.AppendLine("      \"displayName\": \"" + Encode(profile.DisplayName) + "\",");
                sb.AppendLine("      \"gender\": \"" + profile.Gender + "\",");
                sb.AppendLine("      \"confidence\": " + ToJsonNumber(profile.Confidence) + ",");
                sb.AppendLine("      \"source\": \"" + Encode(profile.Source) + "\"");
                sb.Append("    }");
                sb.AppendLine(i + 1 < profiles.Profiles.Count ? "," : string.Empty);
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static SpeakerProfileCollection Deserialize(string json)
        {
            var input = (json ?? string.Empty).Trim();
            if (input.Length == 0)
            {
                throw new FormatException("The speaker profile sidecar is empty");
            }

            if (!int.TryParse(Json.ReadTag(input, "schemaVersion"), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var schemaVersion))
            {
                throw new FormatException("The speaker profile sidecar has no valid schema version");
            }

            if (schemaVersion != CurrentSchemaVersion)
            {
                throw new NotSupportedException("Unsupported speaker profile schema version: " + schemaVersion);
            }

            var speakersStart = input.IndexOf("\"speakers\"", StringComparison.Ordinal);
            if (speakersStart < 0)
            {
                throw new FormatException("The speaker profile sidecar has no speakers array");
            }

            var arrayStart = input.IndexOf('[', speakersStart);
            if (arrayStart < 0)
            {
                throw new FormatException("The speaker profile sidecar has an invalid speakers array");
            }

            var arrayEnd = input.LastIndexOf(']');
            if (arrayEnd < arrayStart)
            {
                throw new FormatException("The speaker profile sidecar has an invalid speakers array");
            }

            var result = new SpeakerProfileCollection();
            var objects = Json.ReadObjectArray(input.Substring(arrayStart, arrayEnd - arrayStart + 1));
            if (objects == null)
            {
                throw new FormatException("The speaker profile sidecar has an invalid speakers array");
            }

            foreach (var item in objects)
            {
                var displayName = ReadString(item, "displayName");
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var profile = new SpeakerProfile
                {
                    Id = ReadString(item, "id"),
                    DisplayName = displayName,
                    Gender = ReadGender(item),
                    Confidence = ReadConfidence(item),
                    Source = ReadString(item, "source"),
                };
                if (string.IsNullOrWhiteSpace(profile.Id))
                {
                    profile.Id = Guid.NewGuid().ToString("N");
                }

                result.AddOrUpdate(profile);
            }

            return result;
        }

        private static string Encode(string value)
        {
            return Json.EncodeJsonText(value ?? string.Empty);
        }

        private static string ReadString(string json, string tag)
        {
            var value = Json.ReadTag(json, tag);
            return value == null ? string.Empty : Json.DecodeJsonText(value);
        }

        private static SpeakerGender ReadGender(string json)
        {
            return Enum.TryParse(ReadString(json, "gender"), true, out SpeakerGender gender)
                ? gender
                : SpeakerGender.Unknown;
        }

        private static double? ReadConfidence(string json)
        {
            return double.TryParse(Json.ReadTag(json, "confidence"), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var confidence)
                ? confidence
                : (double?)null;
        }

        private static string ToJsonNumber(double? value)
        {
            return value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value)
                ? value.Value.ToString("R", CultureInfo.InvariantCulture)
                : "null";
        }
    }
}
