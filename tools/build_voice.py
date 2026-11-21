"""Vocea naratorului: citeste textul fiecarui bloc (si paragrafele insotitorilor) cu o voce neurala romaneasca.

Rulare (din radacina repo-ului, dupa build_story.py):
    python tools/build_voice.py          # genereaza doar ce lipseste sau s-a schimbat, apoi reface arhivele
    python tools/build_voice.py --check  # doar spune cate fisiere lipsesc (fara internet)

Ce face:
    - pentru fiecare bloc: stories/<id>/voice/<bloc>.wav, plus <bloc>@<insotitor>.wav pentru paragrafele insotitorilor;
    - vocea: Emil (protagonist barbat) sau Alina (protagonista), prin edge-tts (serviciul de citire al Microsoft Edge);
    - MP3-ul primit e convertit cu ffmpeg in WAV IMA ADPCM, 22050 Hz mono (~11 KB pe secunda), pe care jocul il citeste;
    - textele deja citite stau in tools/.voice_cache (dupa amprenta textului), deci nu se cer din nou;
    - la sfarsit pune vocile in arhivele din LastStopNight/Assets/stories.

Cerinte: pip install edge-tts, ffmpeg in PATH, internet (doar pentru textele noi).
"""
import asyncio
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CACHE = os.path.join(ROOT, "tools", ".voice_cache")
OUT_ZIPS = os.path.join(ROOT, "LastStopNight", "Assets", "stories")
STORIES = ["night_shift", "km13", "diner1987"]

# vocea naratorului dupa protagonist (povestea e scrisa la persoana intai, de el)
VOICES = {"alex": "ro-RO-EmilNeural", "dan": "ro-RO-EmilNeural", "mara": "ro-RO-AlinaNeural"}
DEFAULT_VOICE = "ro-RO-EmilNeural"
RATE = "+4%"  # un pic mai repede decat implicit: povestile au mult text


def spoken(text):
    """Textul asa cum il citeste vocea: fara ghilimele si cu pauze clare intre paragrafe."""
    text = text.replace("„", "").replace("”", "").replace('"', "")
    text = re.sub(r"\.\.\.", "…", text)
    text = re.sub(r"\n{2,}", "\n", text).strip()
    return text


def block_texts(story):
    """(cale in arhiva, text) pentru tot ce citeste naratorul."""
    for b in story["blocks"]:
        main = b.get("text", "")
        paper = b.get("paper")
        if paper:
            main = "\n\n".join(x for x in [main, paper.get("title") or "", paper.get("text", "")] if x)
        if main.strip():
            yield f"voice/{b['id']}.wav", main
        for companion, extra in (b.get("companionText") or {}).items():
            if extra.strip():
                yield f"voice/{b['id']}@{companion}.wav", extra


def cache_path(voice, text):
    key = hashlib.sha1(f"{voice}|{RATE}|{spoken(text)}".encode("utf8")).hexdigest()[:20]
    return os.path.join(CACHE, f"{key}.mp3")


async def synthesize(voice, text, mp3):
    import edge_tts
    tmp = mp3 + ".part"
    for attempt in range(4):
        try:
            await edge_tts.Communicate(spoken(text), voice, rate=RATE).save(tmp)
            os.replace(tmp, mp3)
            return
        except Exception as ex:  # retea instabila: mai incercam
            if attempt == 3:
                raise SystemExit(f"nu am putut genera vocea: {ex}")
            await asyncio.sleep(2 + attempt * 3)


def to_adpcm(mp3, wav):
    os.makedirs(os.path.dirname(wav), exist_ok=True)
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", mp3, "-ar", "22050", "-ac", "1", "-c:a", "adpcm_ima_wav", wav],
                   check=True)


async def build(story_id, check_only):
    folder = os.path.join(ROOT, "stories", story_id)
    with open(os.path.join(folder, "story.json"), encoding="utf8") as f:
        story = json.load(f)
    voice = VOICES.get(story.get("playerCharacterId"), DEFAULT_VOICE)
    items = list(block_texts(story))
    missing = [(rel, text) for rel, text in items if not os.path.exists(cache_path(voice, text))]
    if check_only:
        print(f"  {story_id}: {len(items)} texte, {len(missing)} de generat ({voice})")
        return
    os.makedirs(CACHE, exist_ok=True)
    sem = asyncio.Semaphore(2)

    async def one(text):
        async with sem:
            await synthesize(voice, text, cache_path(voice, text))

    await asyncio.gather(*(one(text) for _, text in missing))

    # vocile vechi (blocuri sterse) nu mai raman in poveste
    vdir = os.path.join(folder, "voice")
    if os.path.isdir(vdir):
        shutil.rmtree(vdir)
    for rel, text in items:
        to_adpcm(cache_path(voice, text), os.path.join(folder, rel))

    zip_path = os.path.join(OUT_ZIPS, story_id + ".zip")
    with zipfile.ZipFile(zip_path, "r") as z:
        keep = {n: z.read(n) for n in z.namelist() if not n.startswith("voice/")}
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        for name, data in keep.items():
            z.writestr(name, data)
        for rel, _ in items:
            z.write(os.path.join(folder, rel), rel)
    size = sum(os.path.getsize(os.path.join(folder, rel)) for rel, _ in items)
    print(f"  {story_id}: {len(items)} voci ({len(missing)} noi), {size / 1e6:.1f} MB, {voice} -> {zip_path}")


async def main():
    check = "--check" in sys.argv
    if not check and shutil.which("ffmpeg") is None:
        raise SystemExit("lipsește ffmpeg (winget install Gyan.FFmpeg)")
    for s in STORIES:
        await build(s, check)


if __name__ == "__main__":
    asyncio.run(main())
