"""Genereaza sunetele jocului in LastStopNight/Assets/sounds (determinist: aceleasi fisiere la fiecare rulare).

Rulare (din radacina repo-ului):
    python tools/build_sounds.py
"""
import os
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tools"))

import sound  # noqa: E402

if __name__ == "__main__":
    out = os.path.join(ROOT, "LastStopNight", "Assets", "sounds")
    t = time.time()
    count = sound.generate(out)
    print(f"  {count} sunete scrise in {out} ({time.time() - t:.1f}s)")
