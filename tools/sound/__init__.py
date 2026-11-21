"""Toate sunetele jocului, generate procedural (fara fisiere descarcate, fara licente).

    from sound import generate
    generate("LastStopNight/Assets/sounds")   # scrie *.wav (22050 Hz, mono, 16 biti)

Conventii de nume (folosite de joc):
    ui_*      meniuri            step_*   pasi           end_*    finaluri (good/bad/secret/alt)
    amb_*     ambiante in bucla (numele scurt e folosit de locatii: "rain" -> amb_rain.wav)
    music_*   muzica in bucla
Buclele (amb_*, music_*) au capetele legate: se pot repeta la nesfarsit fara pocnituri.
"""
import math
import os

from .synth import (RATE, add, bandpass, brown, buf, echo, env_adsr, env_decay, highpass, lowpass, mix_into, mul,
                    noise, normalize, note, rng, scale, softclip, tone, write_wav)


# ------------------------------------------------------------------ meniuri si interfata

def ui_move():
    return env_decay(add(scale(tone(1320, 0.05, "tri"), 0.6), scale(tone(2640, 0.05), 0.15)), 0.045)


def ui_confirm():
    a = env_decay(tone(660, 0.09, "tri"), 0.09)
    b = env_decay(tone(990, 0.16, "tri"), 0.16)
    return add(a, [0.0] * int(0.06 * RATE) + b)


def ui_back():
    a = env_decay(tone(740, 0.08, "tri"), 0.08)
    b = env_decay(tone(494, 0.14, "tri"), 0.14)
    return add(a, [0.0] * int(0.06 * RATE) + b)


def step(seed):
    r = rng(seed)
    thump = env_decay(tone(85 + r.uniform(-8, 8), 0.09, "sine", 55), 0.08)
    scuff = env_decay(bandpass(noise(0.07, r), 300, 2400), 0.05, attack=0.002)
    return add(scale(thump, 0.8), scale(normalize(scuff, 1), 0.45))


def choose():
    thump = env_decay(tone(110, 0.3, "sine", 60), 0.25)
    click = env_decay(highpass(noise(0.02, rng(11)), 1500), 0.015)
    shimmer = env_decay(tone(880, 0.25, "tri"), 0.22)
    return add(thump, scale(normalize(click, 1), 0.4), scale(shimmer, 0.12))


def locked():
    out = buf(0.26)
    for at in (0.0, 0.12):
        mix_into(out, env_adsr(lowpass(tone(98, 0.1, "square"), 900), 0.004, 0.02), at, 0.8)
    return out


def examine():
    r = rng(23)
    n = bandpass(noise(0.32, r), 900, 4500)
    flutter = [0.5 + 0.5 * math.sin(i / RATE * math.pi * 2 * 17 + math.sin(i / RATE * 9)) for i in range(len(n))]
    return env_adsr(mul(n, flutter), 0.03, 0.15, 0.9)


def paper():
    r = rng(29)
    out = env_adsr(bandpass(noise(0.4, r), 1500, 7000), 0.01, 0.2, 0.5)
    for _ in range(14):
        mix_into(out, env_decay(highpass(noise(0.012, r), 2500), 0.01), r.uniform(0.0, 0.35), r.uniform(0.4, 1.0))
    return out


# ------------------------------------------------------------------ efectele din scena

def phone_ring():
    """Soneria unui telefon vechi de perete: doua clopotele lovite de 20 de ori pe secunda, 0,9 s."""
    n = int(0.9 * RATE)
    bell = add(tone(1180, 0.9), scale(tone(1250, 0.9), 0.8), scale(tone(2390, 0.9), 0.25))
    hammer = [0.55 + 0.45 * math.sin(i / RATE * math.pi * 2 * 20) for i in range(n)]
    return env_adsr(mul(bell, hammer), 0.01, 0.08)


def buzz():
    """Neonul care palpaie: bazait electric pe 100 Hz, taiat in reprize scurte."""
    r = rng(37)
    hum = add(tone(100, 0.5, "saw"), scale(tone(200, 0.5, "square"), 0.3), scale(noise(0.5, r), 0.2))
    gate = []
    on = True
    for i in range(len(hum)):
        if r.random() < 0.0012:
            on = not on
        gate.append(1.0 if on else 0.15)
    return env_adsr(lowpass(mul(hum, lowpass(gate, 60)), 2400), 0.01, 0.12)


def rumble():
    r = rng(41)
    return env_adsr(lowpass(brown(1.4, r, 0.995), 140), 0.08, 0.9, 1.0)


def hit():
    r = rng(43)
    body = env_decay(tone(160, 0.35, "sine", 45), 0.3)
    crack = env_decay(lowpass(noise(0.15, r), 3000), 0.1, attack=0.001)
    return softclip(add(body, scale(crack, 0.7)), 2.2)


def chapter():
    """Lovitura grava de la cardul de capitol: boom adanc cu coada lunga."""
    r = rng(47)
    boom = env_decay(tone(70, 2.4, "sine", 38), 2.2, attack=0.01)
    sub = env_decay(lowpass(brown(2.4, r, 0.997), 90), 2.0, attack=0.02)
    metal = env_decay(add(tone(311, 2.4), scale(tone(467, 2.4), 0.6), scale(tone(622, 2.4), 0.3)), 2.3, attack=0.005)
    return add(boom, scale(sub, 0.6), scale(metal, 0.12))


def vhs():
    """Banda VHS care porneste: fasait, ton care se clatina si un clic de mecanism."""
    r = rng(53)
    n = int(0.9 * RATE)
    hiss = scale(highpass(noise(0.9, r), 2000), 0.35)
    wobble, ph = [], 0.0
    for i in range(n):
        f = 440 * (1 + 0.03 * math.sin(i / RATE * math.pi * 2 * 5.5)) * (0.6 + 0.4 * min(1, i / (0.4 * RATE)))
        ph += f / RATE
        wobble.append(math.sin(2 * math.pi * ph) * 0.4)
    click = env_decay(lowpass(noise(0.03, r), 1200), 0.02)
    return env_adsr(add(hiss, wobble, scale(click, 1.2)), 0.005, 0.25)


def vote():
    return add(env_decay(tone(1568, 0.08, "tri"), 0.06), [0.0] * int(0.07 * RATE) + env_decay(tone(1175, 0.1, "tri"), 0.08))


def chime(notes):
    out = buf(0.7)
    for k, nm in enumerate(notes):
        mix_into(out, env_decay(add(tone(note(nm), 0.5, "tri"), scale(tone(note(nm) * 2, 0.5), 0.2)), 0.45), k * 0.09, 0.7)
    return out


# ------------------------------------------------------------------ finaluri

def pad(freqs, seconds, rnd, shape="tri", detune=0.003):
    """Acord tinut, cu voci usor dezacordate (sunet „cald”)."""
    out = buf(seconds)
    for f in freqs:
        for d in (-detune, detune):
            mix_into(out, tone(f * (1 + d), seconds, shape, phase=rnd.random()), 0, 1.0 / len(freqs))
    return out


def end_good():
    r = rng(61)
    base = env_adsr(lowpass(pad([note(n) for n in ("C3", "G3", "E4")], 4.0, r), 1800), 0.8, 2.0)
    arp = buf(4.0)
    for k, nm in enumerate(("C5", "E5", "G5", "C6", "G5", "E5")):
        mix_into(arp, env_decay(tone(note(nm), 1.2, "tri"), 1.1), 0.3 + k * 0.33, 0.25)
    return add(base, echo(arp, 0.27, 0.4, 3)[:len(base)])


def end_bad():
    r = rng(67)
    drone = env_adsr(lowpass(pad([note("A1"), note("A#1"), note("E2"), note("D#3")], 4.0, r, "saw", 0.006), 600), 0.05, 2.2)
    hitp = env_decay(tone(90, 1.5, "sine", 40), 1.4)
    return softclip(add(drone, hitp), 1.6)


def end_secret():
    r = rng(71)
    out = buf(4.0)
    for k, nm in enumerate(("E5", "F#5", "G#5", "A#5", "C6", "D6", "E6")):
        b = env_decay(add(tone(note(nm), 2.0), scale(tone(note(nm) * 2.76, 2.0), 0.3)), 1.8, attack=0.002)
        mix_into(out, b, 0.15 + k * 0.22, 0.3)
    shimmer = env_adsr(lowpass(pad([note("E3"), note("B3")], 4.0, r), 900), 1.0, 2.0)
    return add(echo(out, 0.31, 0.45, 3)[:len(out)], scale(shimmer, 0.6))


def end_alt():
    r = rng(73)
    chord = env_adsr(lowpass(pad([note(n) for n in ("D3", "A3", "G4", "E4")], 4.0, r), 1400), 0.6, 2.4)
    bell = env_decay(tone(note("A5"), 2.5, "tri"), 2.3)
    return add(chord, mix_into(buf(4.0), bell, 0.4, 0.3))


# ------------------------------------------------------------------ bucle de ambianta

def brown_loop(seconds, rnd, cutoff=30):
    """Vuiet adanc pentru bucle: zgomot alb filtrat circular (capetele se leaga, spre deosebire de brown())."""
    return normalize(lowpass(noise(seconds, rnd), cutoff, circular=True), 1.0)


def loop_filter(x, lo=None, hi=None):
    if lo:
        x = highpass(x, lo, circular=True)
    if hi:
        x = lowpass(x, hi, circular=True)
    return x


def amb_rain():
    r = rng(101)
    secs = 8.0
    bed = loop_filter(noise(secs, r), 400, 3500)
    bed = loop_filter(bed, None, 5000)
    out = scale(normalize(bed, 1), 0.5)
    for _ in range(900):  # picaturi
        d = env_decay(bandpass(noise(0.02, r), 1500, 6000), 0.012, attack=0.001)
        mix_into(out, d, r.uniform(0, secs), r.uniform(0.05, 0.35), wrap=True)
    for _ in range(40):  # picaturi mari pe tabla
        d = env_decay(tone(r.uniform(900, 1700), 0.05, "sine", r.uniform(500, 800)), 0.04)
        mix_into(out, d, r.uniform(0, secs), r.uniform(0.1, 0.3), wrap=True)
    return out


def amb_wind():
    r = rng(103)
    secs = 10.0
    base = normalize(loop_filter(brown_loop(secs, r), 60, 700), 1)
    n = len(base)
    # rafale: anvelopa lenta cu perioada care intra exact de cateva ori in bucla
    gust = [0.55 + 0.3 * math.sin(2 * math.pi * 2 * i / n) + 0.15 * math.sin(2 * math.pi * 5 * i / n + 1.3) for i in range(n)]
    whistle = normalize(loop_filter(noise(secs, r), 600, 900), 1)
    return add(mul(base, gust), scale(mul(whistle, [g * g for g in gust]), 0.25))


def amb_hum():
    """Neon si frigider: 100 Hz cu armonice (buclele de 4 s contin un numar intreg de perioade)."""
    r = rng(107)
    secs = 4.0
    h = add(tone(100, secs), scale(tone(200, secs), 0.5), scale(tone(300, secs), 0.25), scale(tone(500, secs), 0.08))
    hiss = scale(normalize(loop_filter(noise(secs, r), 3000, 8000), 1), 0.05)
    return add(scale(h, 0.5), hiss)


def amb_room():
    r = rng(109)
    secs = 6.0
    tone_ = normalize(loop_filter(noise(secs, r), 80, 500), 1)
    fridge = add(tone(50, secs), scale(tone(150, secs), 0.3))
    return add(scale(tone_, 0.6), scale(fridge, 0.25))


def amb_night():
    """Greieri si un vant usor (parcarea benzinariei)."""
    r = rng(113)
    secs = 8.0
    out = scale(normalize(loop_filter(brown_loop(secs, r), 50, 400), 1), 0.35)
    for cricket, base_f in ((0, 4400), (1, 4900), (2, 3900)):
        t = r.uniform(0, 0.5)
        while t < secs:
            for p in range(3):  # tril de 3 pulsuri
                c = env_adsr(tone(base_f + r.uniform(-40, 40), 0.03), 0.004, 0.012)
                mix_into(out, c, t + p * 0.05, 0.25 if cricket else 0.3, wrap=True)
            t += r.uniform(0.6, 1.1)
    return out


def amb_clock():
    """Ceasul din birou: tic-tac, o bataie pe secunda (4 s)."""
    r = rng(127)
    out = scale(normalize(loop_filter(noise(4.0, r), 100, 400), 1), 0.08)
    for k in range(4):
        f = 2600 if k % 2 == 0 else 2200
        tick = add(env_decay(tone(f, 0.03), 0.02, attack=0.0005), scale(env_decay(highpass(noise(0.01, r), 3000), 0.008), 0.4))
        mix_into(out, tick, k * 1.0, 0.6, wrap=True)
    return out


def amb_birds():
    """Pasari in zori: ciripit rar."""
    r = rng(131)
    secs = 10.0
    out = scale(normalize(loop_filter(brown_loop(secs, r), 60, 500), 1), 0.25)
    t = 0.3
    while t < secs:
        f0 = r.uniform(2500, 4200)
        for k in range(r.randint(2, 5)):
            f1 = f0 * r.uniform(1.1, 1.5)
            chirp = env_adsr(tone(f0, 0.07, "sine", f1), 0.01, 0.04)
            mix_into(out, chirp, t + k * 0.11, r.uniform(0.15, 0.3), wrap=True)
        t += r.uniform(0.9, 2.2)
    return out


def amb_tension():
    """Drona joasa care bate (55 / 55,5 Hz) si un val de zgomot: scenele intunecate si cele rosii."""
    r = rng(137)
    secs = 8.0
    n = int(secs * RATE)
    drone = add(tone(55, secs), scale(tone(55.5, secs), 0.9), scale(tone(82.5, secs), 0.35), scale(tone(116.5, secs), 0.15, ))
    swell = [0.5 - 0.5 * math.cos(2 * math.pi * i / n) for i in range(n)]
    air = mul(normalize(loop_filter(noise(secs, r), 200, 1200), 1), swell)
    return add(scale(drone, 0.55), scale(air, 0.25))


def music_menu():
    """Tema meniului: patru acorduri lente in La minor, cu o melodie rara si ecou; 16 s in bucla."""
    r = rng(149)
    secs = 16.0
    out = buf(secs)
    chords = [("A2", "E3", "C4", "E4"), ("F2", "C3", "A3", "E4"), ("D2", "A2", "F3", "D4"), ("E2", "B2", "G#3", "E4")]
    for k, ch in enumerate(chords):
        p = env_adsr(lowpass(pad([note(n) for n in ch], 4.6, r, "tri", 0.004), 1100), 1.2, 1.5)
        mix_into(out, p, k * 4.0, 0.5, wrap=True)
    melody = [(0.5, "E5"), (1.5, "C5"), (2.5, "B4"), (4.5, "C5"), (5.5, "A4"), (8.5, "D5"), (9.5, "F5"), (10.5, "E5"),
              (12.5, "G#4"), (13.5, "B4"), (14.5, "E5")]
    mel = buf(secs)
    for at, nm in melody:
        mix_into(mel, env_decay(add(tone(note(nm), 1.6, "tri"), scale(tone(note(nm) * 2, 1.6), 0.15)), 1.5), at, 0.22, wrap=True)
    mel = echo(mel, 0.375, 0.42, 4, wrap=True)
    bass = buf(secs)
    for k, nm in enumerate(("A1", "F1", "D1", "E1")):
        mix_into(bass, env_adsr(lowpass(tone(note(nm), 3.9, "tri"), 300), 0.3, 1.0), k * 4.0, 0.35, wrap=True)
    return add(out, mel, bass)


SOUNDS = {
    "ui_move": (ui_move, 0.5), "ui_confirm": (ui_confirm, 0.6), "ui_back": (ui_back, 0.55),
    "step_1": (lambda: step(1), 0.8), "step_2": (lambda: step(2), 0.8), "step_3": (lambda: step(3), 0.8),
    "choose": (choose, 0.8), "locked": (locked, 0.6), "examine": (examine, 0.6), "paper": (paper, 0.6),
    "phone_ring": (phone_ring, 0.7), "buzz": (buzz, 0.6), "rumble": (rumble, 0.9), "hit": (hit, 0.9),
    "chapter": (chapter, 0.9), "vhs": (vhs, 0.7), "vote": (vote, 0.55),
    "join": (lambda: chime(("C5", "E5", "G5")), 0.6), "leave": (lambda: chime(("G5", "E5", "C5")), 0.6),
    "end_good": (end_good, 0.75), "end_bad": (end_bad, 0.8), "end_secret": (end_secret, 0.75), "end_alt": (end_alt, 0.75),
    "amb_rain": (amb_rain, 0.6), "amb_wind": (amb_wind, 0.6), "amb_hum": (amb_hum, 0.35), "amb_room": (amb_room, 0.35),
    "amb_night": (amb_night, 0.55), "amb_clock": (amb_clock, 0.45), "amb_birds": (amb_birds, 0.5),
    "amb_tension": (amb_tension, 0.6), "music_menu": (music_menu, 0.6),
}


def generate(folder):
    os.makedirs(folder, exist_ok=True)
    for name, (make, peak) in SOUNDS.items():
        write_wav(os.path.join(folder, name + ".wav"), make(), peak)
    return len(SOUNDS)
