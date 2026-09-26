using System;

namespace Nikse.SubtitleEdit.Core.Common
{
    /// <summary>
    /// A speaker-labelled audio interval, independent of the engine that detected it.
    /// Times are in subtitle milliseconds; no subtitle text or timing is supplied by the engine.
    /// </summary>
    public sealed class SpeakerDiarizationSegment
    {
        public double StartMilliseconds { get; }
        public double EndMilliseconds { get; }
        public string SpeakerId { get; }

        public SpeakerDiarizationSegment(double startMilliseconds, double endMilliseconds, string speakerId)
        {
            if (!double.IsFinite(startMilliseconds) || !double.IsFinite(endMilliseconds) ||
                startMilliseconds < 0 || endMilliseconds <= startMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(endMilliseconds), "A diarization interval must have finite, increasing, non-negative times.");
            }

            if (string.IsNullOrWhiteSpace(speakerId))
            {
                throw new ArgumentException("A diarization interval must identify a speaker.", nameof(speakerId));
            }

            StartMilliseconds = startMilliseconds;
            EndMilliseconds = endMilliseconds;
            SpeakerId = speakerId.Trim();
        }
    }
}
