"""Original deterministic feedback cues; no downloaded samples or runtime synthesis.

Run: python Tools/Audio/build_feedback_audio.py
Outputs mono 22.05 kHz PCM:
  World/Rewards/RewardReveal.wav  - a reward appears: one soft, short bronze glint (user rejected the long chime, 21 Oct).
  Combat/ChargeReady.wav          - the charged cleave reaches full strength: a tiny bright metal ring.
  World/Rewards/CoinPickup.wav    - a bronze coin is picked up: two tiny, soft clinks (user, 22 Oct: very subtle).
  World/Rewards/RewardHoldUp.wav  - the hero holds up a find: two soft rising bronze notes, under a second.
  Combat/SurfaceStone.wav         - the axe strikes stone: a short dry clack.
  Combat/SurfaceBrush.wav         - the axe strikes brush: a quick leafy crackle.
  Combat/SurfaceEarth.wav         - the axe strikes earth: a dull thump.
  Combat/PotBreak.wav             - a pot breaks: a few ceramic clinks over a short rattle.
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


def hold_up():
    def note(t, start, hz, level):
        if t < start:
            return 0.0
        k = t - start
        return (math.sin(2 * math.pi * hz * k) * .55 * math.exp(-k * 7) +
                math.sin(2 * math.pi * hz * 2.76 * k) * .12 * math.exp(-k * 20)) * level * min(1, k / .004)

    return render(.7, lambda t, i: note(t, 0, 784, .8) + note(t, .13, 1175, 1))


def surface_stone():
    rng = random.Random(5101)

    def voice(t, i):
        # Two hard inharmonic knocks that die at once, with a grain of grit on the attack.
        v = math.sin(2 * math.pi * 1850 * t) * .55 * math.exp(-t * 70)
        v += math.sin(2 * math.pi * 3120 * t) * .3 * math.exp(-t * 95)
        return v + rng.uniform(-1, 1) * .35 * math.exp(-t * 120)
    return render(.11, voice)


def surface_brush():
    rng = random.Random(5102)
    state = [0.0, 0.0]
    crackles = [0, .035, .06, .095]

    def voice(t, i):
        # High-passed noise in quick bursts: dry leaves, not a whoosh.
        raw = rng.uniform(-1, 1)
        state[0] += .55 * (raw - state[0])
        bright = raw - state[0]
        level = sum(math.exp(-(t - c) * 75) for c in crackles if t >= c)
        return bright * .6 * min(1, level)
    return render(.17, voice)


def surface_earth():
    rng = random.Random(5103)
    state = [0.0]

    def voice(t, i):
        # A low thump with a little dull soil in it.
        state[0] += .12 * (rng.uniform(-1, 1) - state[0])
        return math.sin(2 * math.pi * (150 - 300 * t) * t) * .8 * math.exp(-t * 32) + state[0] * 1.6 * math.exp(-t * 38)
    return render(.13, voice)


def pot_break():
    rng = random.Random(5104)
    state = [0.0]
    clinks = [(0, 2350, 1.0), (.018, 3480, .7), (.044, 2790, .55), (.075, 4120, .4), (.118, 3050, .28)]

    def voice(t, i):
        # Separate ceramic pieces over a dry rattle: one break, not a ring.
        v = sum(math.sin(2 * math.pi * hz * (t - s)) * level * .45 * math.exp(-(t - s) * 85)
                for s, hz, level in clinks if t >= s)
        raw = rng.uniform(-1, 1)
        state[0] += .4 * (raw - state[0])
        return v + (raw - state[0]) * .3 * math.exp(-t * 22)
    return render(.26, voice)


def coin_pickup():
    def clink(t, f, gain):
        # A small struck bronze disc: a soft partial and a faint inharmonic overtone, gone in ~60 ms.
        if t < 0:
            return 0.0
        v = math.sin(2 * math.pi * f * t) * math.exp(-t * 55)
        v += math.sin(2 * math.pi * f * 2.71 * t) * .25 * math.exp(-t * 90)
        return v * gain

    def voice(t, i):
        # Two clinks, the second softer and slightly lower, like a coin settling into a purse.
        return clink(t, 2350, .5) + clink(t - .055, 2110, .3)
    return render(.17, voice)


if __name__ == "__main__":
    write(AUDIO / "World/Rewards/RewardReveal.wav", reveal(), .45)
    write(AUDIO / "Combat/ChargeReady.wav", charge_ready(), .4)
    write(AUDIO / "World/Rewards/CoinPickup.wav", coin_pickup(), .3)
    write(AUDIO / "World/Rewards/RewardHoldUp.wav", hold_up(), .42)
    write(AUDIO / "Combat/SurfaceStone.wav", surface_stone(), .34)
    write(AUDIO / "Combat/SurfaceBrush.wav", surface_brush(), .3)
    write(AUDIO / "Combat/SurfaceEarth.wav", surface_earth(), .38)
    write(AUDIO / "Combat/PotBreak.wav", pot_break(), .36)
