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
5. A provider-neutral context-batch protocol for OpenAI-compatible APIs and Gemini.
6. A diarization assignment abstraction, followed by an optional pyannote backend.

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

## First vertical slice

The first usable milestone is complete when a user can:

1. open or generate subtitles containing `Speaker 1`, `Speaker 2`, etc.;
2. review and edit speaker names and genders;
3. translate with the advanced local engine using those metadata;
4. retain exactly the same segments and timestamps; and
5. reopen the subtitle with its speaker profiles restored from the sidecar.
