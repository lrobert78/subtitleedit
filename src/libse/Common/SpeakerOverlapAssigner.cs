using System;
using System.Collections.Generic;
using System.Linq;

namespace Nikse.SubtitleEdit.Core.Common
{
    /// <summary>
    /// Assigns a speaker to each subtitle line by the greatest unique audio overlap.
    /// It does not mutate lines and leaves unsupported or tied lines unassigned.
    /// </summary>
    public static class SpeakerOverlapAssigner
    {
        public static Dictionary<Paragraph, string> Assign(
            IReadOnlyList<Paragraph> lines,
            IReadOnlyList<SpeakerDiarizationSegment> segments)
        {
            var result = new Dictionary<Paragraph, string>();
            foreach (var line in lines)
            {
                var start = line.StartTime.TotalMilliseconds;
                var end = line.EndTime.TotalMilliseconds;
                if (end <= start)
                {
                    continue;
                }

                var overlapBySpeaker = segments
                    .Where(segment => segment.StartMilliseconds < end && segment.EndMilliseconds > start)
                    .GroupBy(segment => segment.SpeakerId, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new
                    {
                        Speaker = group.Key,
                        Duration = UnionDuration(group
                            .Select(segment => (
                                Start: Math.Max(start, segment.StartMilliseconds),
                                End: Math.Min(end, segment.EndMilliseconds)))
                            .OrderBy(interval => interval.Start)),
                    })
                    .OrderByDescending(item => item.Duration)
                    .ToList();

                if (overlapBySpeaker.Count == 0 || overlapBySpeaker[0].Duration <= 0 ||
                    overlapBySpeaker.Count > 1 &&
                    Math.Abs(overlapBySpeaker[0].Duration - overlapBySpeaker[1].Duration) < 0.01)
                {
                    continue;
                }

                result[line] = overlapBySpeaker[0].Speaker;
            }

            return result;
        }

        private static double UnionDuration(IEnumerable<(double Start, double End)> intervals)
        {
            var total = 0.0;
            var currentEnd = double.NegativeInfinity;
            foreach (var (start, end) in intervals)
            {
                if (start >= currentEnd)
                {
                    total += end - start;
                }
                else if (end > currentEnd)
                {
                    total += end - currentEnd;
                }

                currentEnd = Math.Max(currentEnd, end);
            }

            return total;
        }
    }
}
