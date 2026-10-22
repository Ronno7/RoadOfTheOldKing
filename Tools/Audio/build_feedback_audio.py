"""Original deterministic feedback cues; no downloaded samples or runtime synthesis.

Run: python Tools/Audio/build_feedback_audio.py
Outputs mono 22.05 kHz PCM:
  World/Rewards/RewardReveal.wav  - a reward appears: one soft, short bronze glint (user rejected the long chime, 21 Oct).
  Combat/ChargeReady.wav          - the charged cleave reaches full strength: a tiny bright metal ring.
Both stay quiet and brief so they sit under combat and ambience rather than announcing themselves.
"""
from pathlib import Path
import math
import random
import struct
import wave

ROOT = Path(__file__).resolve().parents[2]
AUDIO = ROOT / "RoadOfTheOldKing/Assets/Audio"
RATE = 22050


def write(path, samples, peak_target):
    peak = max(abs(x) for x in samples)
    gain = peak_target / peak
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(RATE)
        stream.writeframes(b"".join(struct.pack("<h", round(x * gain * 32767)) for x in samples))
    print(path.name, len(samples), "samples; peak", round(peak * gain, 3))


def render(duration, voice):
    count = round(duration * RATE)
    out = []
    for i in range(count):
        t = i / RATE
        attack = min(1, t / .003)  # Click-free onset.
        tail = min(1, (duration - t) / .04)  # Tapered end.
        out.append(voice(t, i) * attack * tail)
    return out


def reveal():
    rng = random.Random(4127)
    noise = [rng.uniform(-1, 1) for _ in range(round(.36 * RATE))]
    state = [0.0]

    def voice(t, i):
        # One struck bronze glint: soft fundamental, an inharmonic shimmer that dies quickly, a breath of air.
        v = math.sin(2 * math.pi * 1175 * t) * .5 * math.exp(-t * 11)
        v += math.sin(2 * math.pi * 1175 * 2.76 * t) * .14 * math.exp(-t * 26)
        state[0] += .35 * (noise[i] - state[0])
        v += (noise[i] - state[0]) * .05 * math.exp(-t * 30)
        return v
    return render(.36, voice)


def charge_ready():
    def voice(t, i):
        # A brief rising "shing": a bright ring that lifts a semitone as it settles.
        f = 1568 * (1 + .06 * min(1, t / .05))
        v = math.sin(2 * math.pi * f * t) * .45 * math.exp(-t * 24)
        v += math.sin(2 * math.pi * f * 2.4 * t) * .12 * math.exp(-t * 40)
        return v
    return render(.2, voice)


if __name__ == "__main__":
    write(AUDIO / "World/Rewards/RewardReveal.wav", reveal(), .45)
    write(AUDIO / "Combat/ChargeReady.wav", charge_ready(), .4)
