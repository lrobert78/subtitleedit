# Speaker-aware translation

This fork extends Subtitle Edit's existing actor and context-batch translation support. It does
not replace the editor, waveform, media pipeline, speech-to-text engines, or subtitle formats.

## Invariants

- Translation may update subtitle text only.
- Segment count, order, IDs, timestamps, actors, and speaker profiles are never accepted from a
  translation model.
- A batch with a missing or unexpected result is rejected before any of its rows are applied.
- Diarization assigns actors by deterministic time overlap and never retimes subtitles.
- Speaker gender is optional and defaults to `Unknown`.

## Delivery sequence

1. Speaker profile model and versioned `*.speakers.json` sidecar. **Complete.**
2. Automatic sidecar loading/saving and a speaker profile editor. **Complete.**
3. Actor and gender metadata in `TranslateRow`. **Complete.**
4. Actor and gender fields in the existing advanced llama.cpp/Ollama batch protocol. **Complete.**
5. A provider-neutral context-batch protocol for OpenAI-compatible APIs and Gemini. **Complete.**
6. A diarization assignment abstraction. **Complete.** An optional pyannote backend remains future work.

## Sidecar format

Subtitle formats such as SRT cannot store speaker profiles. For `film.en.srt`, the fork therefore
uses `film.en.speakers.json`:

```json
{
  "schemaVersion": 1,
  "speakers": [
    {
      "id": "speaker-1",
      "displayName": "Anna",
      "gender": "Female",
      "confidence": 0.92,
      "source": "manual"
    }
  ]
}
```

`Paragraph.Actor` remains the segment-to-speaker assignment. Profiles are matched to actors using
case-insensitive, whitespace-normalized display names. The sidecar is optional: a missing or
damaged file must never prevent the subtitle itself from opening.

The editor is available from **Tools → Speaker profiles...**. It lists every actor used by the
subtitle, allows names and gender metadata to be corrected, and records manual gender changes with
`source: "manual"`. Saving the subtitle writes the sidecar when profiles exist (or updates an
existing sidecar); opening the subtitle restores it automatically. Auto Cast creates an `Unknown`
profile for each diarized speaker so its result is ready for review.

## Context-aware translation

The advanced llama.cpp and Ollama engines, the generic OpenAI-compatible engine, and Google Gemini
all receive numbered batches instead of unrelated single lines. Each request includes recent
source/translation pairs plus the immutable speaker ID, actor name, and reviewed gender when these
are available. `Unknown` is sent explicitly for identified speakers and the prompt forbids the
model from guessing it.

Cloud engines use the same strict numbered-result contract as the local engines: every requested
number must occur exactly once, with no extra keys. An incomplete response is retried and then
split into a smaller batch; no partial response is applied. Only translated text is copied back to
the rows, so speaker metadata and subtitle timing cannot be changed by the service.

## Diarization assignment

`SpeakerDiarizationSegment` carries only a time interval and a speaker identifier. The shared
`SpeakerOverlapAssigner` maps these intervals to existing subtitle paragraphs by the largest
covered duration. Overlapping intervals for the same speaker are counted once; silent lines and
exact ties remain unassigned. The assigner does not edit subtitles. Auto Cast converts its current
speech-to-text speaker labels into these intervals, so a later diarization backend can supply the
same data without depending on transcription text or changing subtitle timing.

## First vertical slice

The first usable milestone is complete when a user can:

1. open or generate subtitles containing `Speaker 1`, `Speaker 2`, etc.;
2. review and edit speaker names and genders;
3. translate with the advanced local engine using those metadata;
4. retain exactly the same segments and timestamps; and
5. reopen the subtitle with its speaker profiles restored from the sidecar.
