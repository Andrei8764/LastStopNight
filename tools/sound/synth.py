"""Primitive de sinteza: oscilatoare, zgomot, filtre, anvelope si scrierea WAV.

Totul e determinist (fiecare sunet isi are propriul Random cu seed fix) si foloseste doar biblioteca standard.
Buclele se construiesc „circular”: evenimentele care trec de capat continua de la inceput, iar filtrele
ruleaza de doua ori peste buffer, asa ca sfarsitul se leaga perfect de inceput (fara pocnituri).
"""
import array
import math
import random
import wave

RATE = 22050
TAU = 2 * math.pi


def buf(seconds):
    return [0.0] * int(round(seconds * RATE))


def mix_into(dst, src, at=0.0, gain=1.0, wrap=False):
    """Aduna src in dst incepand de la secunda `at`. Cu wrap, ce trece de capat continua de la inceput."""
    start = int(round(at * RATE))
    n = len(dst)
    for i, v in enumerate(src):
        j = start + i
        if wrap:
            dst[j % n] += v * gain
        elif j < n:
            dst[j] += v * gain
    return dst


# ------------------------------------------------------------------ surse

def tone(freq, seconds, shape="sine", freq_end=None, phase=0.0):
    """Oscilator cu frecventa care poate aluneca liniar pana la freq_end."""
    n = int(round(seconds * RATE))
    out = [0.0] * n
    f1 = freq_end if freq_end is not None else freq
    for i in range(n):
        f = freq + (f1 - freq) * (i / max(1, n - 1))
        phase += f / RATE
        p = phase % 1.0
        if shape == "sine":
            out[i] = math.sin(TAU * p)
        elif shape == "square":
            out[i] = 1.0 if p < 0.5 else -1.0
        elif shape == "tri":
            out[i] = 4 * abs(p - 0.5) - 1
        else:  # saw
            out[i] = 2 * p - 1
    return out


def noise(seconds, rnd):
    return [rnd.uniform(-1, 1) for _ in range(int(round(seconds * RATE)))]


def brown(seconds, rnd, leak=0.985):
    """Zgomot „maro” (adanc, ca un vuiet): integrarea zgomotului alb, cu scurgere."""
    out, v = [], 0.0
    for _ in range(int(round(seconds * RATE))):
        v = v * leak + rnd.uniform(-1, 1) * 0.15
        out.append(v)
    return normalize(out, 1.0)


# ------------------------------------------------------------------ filtre

def lowpass(x, cutoff, circular=False):
    """Filtru trece-jos cu un pol. Cu circular=True, starea porneste din capatul bufferului (bucle)."""
    a = 1 - math.exp(-TAU * cutoff / RATE)
    y = 0.0
    if circular:
        for v in x:
            y += a * (v - y)
    out = [0.0] * len(x)
    for i, v in enumerate(x):
        y += a * (v - y)
        out[i] = y
    return out


def highpass(x, cutoff, circular=False):
    low = lowpass(x, cutoff, circular)
    return [a - b for a, b in zip(x, low)]


def bandpass(x, lo, hi, circular=False):
    return lowpass(highpass(x, lo, circular), hi, circular)


# ------------------------------------------------------------------ anvelope

def env_decay(x, seconds_to_silence, attack=0.003):
    n = len(x)
    att = max(1, int(attack * RATE))
    k = math.log(1000) / max(1, seconds_to_silence * RATE)
    return [v * min(1.0, i / att) * math.exp(-k * i) for i, v in enumerate(x[:n])]


def env_adsr(x, attack, release, sustain=1.0):
    n = len(x)
    a, r = max(1, int(attack * RATE)), max(1, int(release * RATE))
    out = []
    for i, v in enumerate(x):
        g = sustain
        if i < a:
            g = sustain * i / a
        if i > n - r:
            g *= max(0.0, (n - i) / r)
        out.append(v * g)
    return out


def scale(x, g):
    return [v * g for v in x]


def add(*parts):
    n = max(len(p) for p in parts)
    out = [0.0] * n
    for p in parts:
        for i, v in enumerate(p):
            out[i] += v
    return out


def mul(x, y):
    return [a * b for a, b in zip(x, y)]


def normalize(x, peak):
    m = max((abs(v) for v in x), default=0.0)
    return x if m == 0 else [v * peak / m for v in x]


def softclip(x, drive=1.5):
    return [math.tanh(v * drive) / math.tanh(drive) for v in x]


def echo(x, delay, feedback, taps=4, extra=0.0, wrap=False):
    """Ecou simplu (cateva repetari atenuate); extra = secunde adaugate la coada."""
    out = x + [0.0] * (0 if wrap else int(extra * RATE))
    d = int(delay * RATE)
    for t in range(1, taps + 1):
        g = feedback ** t
        n = len(out)
        for i, v in enumerate(x):
            j = i + d * t
            if wrap:
                out[j % n] += v * g
            elif j < n:
                out[j] += v * g
    return out


# ------------------------------------------------------------------ note

def note(name):
    """Frecventa unei note („A4”, „C#3”)."""
    names = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1, "A": 0, "A#": 1, "B": 2}
    octave = int(name[-1])
    return 440.0 * 2 ** ((names[name[:-1]] + (octave - 4) * 12) / 12)


# ------------------------------------------------------------------ fisiere

def write_wav(path, samples, peak=0.9):
    data = normalize(samples, peak)
    pcm = array.array("h", (max(-32767, min(32767, int(round(v * 32767)))) for v in data))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())


def rng(seed):
    return random.Random(seed)
