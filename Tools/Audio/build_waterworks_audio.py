"""Original deterministic waterworks Foley; no downloaded samples or runtime synthesis.

Run: python Tools/Audio/build_waterworks_audio.py
Outputs mono 22.05 kHz PCM. Gears/water use periodic noise with matching loop boundaries.
"""
from pathlib import Path
import math
import random
import struct
import wave

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "RoadOfTheOldKing/Assets/Audio/World/Waterworks"
RATE = 22050


def render(name, duration, kind, loop=False):
    count = round(duration * RATE)
    rng = random.Random(2040 + kind)
    noise = [rng.uniform(-1, 1) for _ in range(count)]
    smooth = 0.0
    samples = []
    for i in range(count):
        t = i / RATE
        smooth += .09 * (noise[i] - smooth)
        if kind == 0:  # Bright bronze latch, short and damped.
            v = sum(math.sin(2 * math.pi * f * t) * math.exp(-t * decay)
                    for f, decay in ((740, 20), (1181, 27), (1930, 40))) * .22
            v += noise[i] * math.exp(-t * 95) * .18
        elif kind == 1:  # Two dull blocked knocks, clearly unlike the latch.
            v = 0
            for offset in (0, .11):
                u = t - offset
                if u >= 0:
                    v += (math.sin(2 * math.pi * 136 * u) * .44 +
                          smooth * .6) * math.exp(-u * 35)
        elif kind == 2:  # Eight ratchet teeth in a one-second loop.
            u = t % .125
            v = (noise[i] * .2 + math.sin(2 * math.pi * 460 * u) * .22) * math.exp(-u * 75)
            v += smooth * .16
        elif kind == 3:  # Weight/passage landing: low wooden/iron thunk.
            v = (math.sin(2 * math.pi * 82 * t) * .5 + smooth * .9) * math.exp(-t * 12)
            v += math.sin(2 * math.pi * 327 * t) * math.exp(-t * 28) * .15
        elif kind == 4:  # Filtered moving water with quiet rolling bubbles.
            v = smooth * 1.1 * (.78 + .12 * math.sin(2 * math.pi * 2 * t))
            v += math.sin(2 * math.pi * 96 * t + 1.4 * math.sin(2 * math.pi * 3 * t)) * .035
        else:  # Short gate scrape with a wooden initial catch.
            v = smooth * .85 * math.sin(math.pi * min(1, t / duration))
            v += math.sin(2 * math.pi * 187 * t) * math.exp(-t * 25) * .25
        if not loop:
            v *= min(1, t / .003, (duration - t) / .025)
        samples.append(v)
    if loop:
        # Crossfade the last 20 ms back to the beginning; retain original duration.
        n = round(.02 * RATE)
        for j in range(n):
            blend = j / (n - 1)
            samples[-n + j] = samples[-n + j] * (1 - blend) + samples[j] * blend
        samples = samples[n:]
    peak = max(abs(x) for x in samples)
    gain = min(1, .85 / peak)
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / (name + ".wav")), "wb") as stream:
        stream.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        stream.writeframes(b"".join(struct.pack("<h", round(x * gain * 32767)) for x in samples))
    print(name, count, "samples; peak", round(peak * gain, 3))


if __name__ == "__main__":
    for args in (("Latch", .35, 0), ("Blocked", .32, 1), ("Ratchet", 1, 2, True),
                 ("Weight", .65, 3), ("Water", 2, 4, True), ("Gate", .55, 5)):
        render(*args)
