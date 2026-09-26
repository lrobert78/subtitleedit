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
6. A diarization assignment abstraction and an optional WhisperX/pyannote path. **Complete.**
7. Optional local voice-gender suggestions with explicit review. **Complete.**

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
When a video is open, the profile editor offers **Play voice sample** for the selected speaker.
It chooses a subtitle line of practical length and plays only that line through the existing
video player; profiles with no assigned lines have no sample. This supports manual review of
gender without pretending that diarization itself can identify it.

**Suggest from voices** is an optional, local classifier run in the same editor. It requires
Python 3 and `numpy`, `librosa`, `onnxruntime`, and `huggingface_hub` in that Python environment.
Use a virtual environment (`python3 -m venv <folder>` followed by
`<folder>/bin/python -m pip install numpy librosa onnxruntime huggingface_hub` on macOS/Linux),
then set `SUBTITLEEDIT_GENDER_PYTHON` to its Python executable before starting Subtitle Edit.
Without that variable, the app tries the system `python3` (or `python` on Windows). The public, pinned
[`syntropicsignal-ai/gender-voice-classifier`](https://huggingface.co/syntropicsignal-ai/gender-voice-classifier)
ONNX model is downloaded on first use. Audio is decoded from the open video locally with ffmpeg;
it is not uploaded. Up to three 3–8 second lines per unidentified speaker are analyzed. A
suggestion appears only when at least two clips agree with strong model scores. The user must
select a row and choose **Accept suggestion** before the profile changes. Existing female, male,
and non-binary profiles are skipped, and an accepted suggestion records `source: "classifier"`
and its score; a subsequent manual change records `source: "manual"` instead. This binary model
estimates a voice category, not a person's gender identity, and does not reliably cover every
voice, accent, or recording condition;
`Unknown` is the correct outcome when evidence is insufficient.

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

With a subtitle and video open, **Tools → Detect speakers in video...** starts the existing
WhisperX speech-to-text engine with `--diarize` for this run. WhisperX uses pyannote for speaker
diarization and may require a Hugging Face access token and acceptance of the model's terms. If
needed, enter it in the masked **Hugging Face token (this window only)** field after selecting
WhisperX. It is passed to the WhisperX child process as `HF_TOKEN`, not saved in Subtitle Edit
settings or placed in its command-line arguments. A previously saved `--hf_token` argument is
moved into this session-only field when the speech-to-text window opens. On platforms without
the bundled WhisperX build, the dialog starts with MOSS Diarize instead. The transcript is used
only for its speaker labels and times: the open subtitle keeps its text and timing, gains actor
assignments and `Unknown` gender profiles, and can be reviewed in **Speaker profiles...**.

## First vertical slice

The first usable milestone is complete when a user can:

1. open or generate subtitles containing `Speaker 1`, `Speaker 2`, etc.;
2. review and edit speaker names and genders;
3. translate with the advanced local engine using those metadata;
4. retain exactly the same segments and timestamps; and
5. reopen the subtitle with its speaker profiles restored from the sidecar.
