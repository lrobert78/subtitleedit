"""Opt-in, local voice-presentation suggestions. Reads one JSON request on stdin."""

import json
import subprocess
import sys


def main():
    try:
        import librosa
        import numpy as np
        import onnxruntime as ort
        from huggingface_hub import hf_hub_download
    except ImportError as exc:
        raise RuntimeError(
            "Missing local Python packages. Install numpy librosa onnxruntime huggingface_hub "
            "in a virtual environment and set SUBTITLEEDIT_GENDER_PYTHON to its Python executable."
        ) from exc

    request = json.load(sys.stdin)
    model_path = hf_hub_download(
        repo_id="syntropicsignal-ai/gender-voice-classifier",
        filename="gender_classifier_200k.onnx",
        revision="412d58a2c62d4d2c54f63fe8519110d1c7b09a6c",
    )
    session = ort.InferenceSession(model_path, providers=["CPUExecutionProvider"])
    predictions = []

    for speaker in request["Speakers"]:
        probabilities = []
        for sample in speaker["Samples"]:
            command = [
                request["Ffmpeg"], "-nostdin", "-v", "error", "-ss", str(sample["Start"]),
                "-t", str(sample["Duration"]), "-i", request["Video"], "-vn",
            ]
            if request.get("AudioTrackIndex") is not None:
                command += ["-map", "0:" + str(request["AudioTrackIndex"])]
            command += ["-ac", "1", "-ar", "16000", "-f", "f32le", "pipe:1"]
            result = subprocess.run(
                command,
                capture_output=True,
                check=True,
            )
            audio = np.frombuffer(result.stdout, dtype="<f4")[:48000]
            if len(audio) < 47500:
                continue
            audio = np.pad(audio, (0, 48000 - len(audio)))
            mfcc = librosa.feature.mfcc(
                y=audio, sr=16000, n_mfcc=40, n_fft=512, hop_length=160, n_mels=80
            )
            mfcc = (mfcc - mfcc.mean(axis=1, keepdims=True)) / (
                mfcc.std(axis=1, keepdims=True) + 1e-8
            )
            logit = float(session.run(["logits"], {"mfcc": mfcc[None].astype(np.float32)})[0][0, 0])
            probabilities.append(float(1 / (1 + np.exp(-logit))))

        # A high average alone can hide a disagreeing clip. Require both consensus and
        # a strong result, and leave everything else for the human reviewer.
        if len(probabilities) >= 2 and (all(p >= 0.9 for p in probabilities) or all(p <= 0.1 for p in probabilities)):
            predictions.append({
                "Actor": speaker["Actor"],
                "FemaleProbability": sum(probabilities) / len(probabilities),
                "Samples": len(probabilities),
            })

    print(json.dumps({"Predictions": predictions}))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
