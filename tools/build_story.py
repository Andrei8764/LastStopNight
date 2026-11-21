"""Construieste toate povestile.

Rulare (din radacina repo-ului):
    python tools/build_story.py            # regenereaza imaginile + story.json + arhivele ZIP
    python tools/build_story.py --no-art   # doar story.json + arhivele (foloseste imaginile existente)

Rezultat:
    stories/_art/images/...                         toate imaginile generate (rezerva comuna)
    stories/<id>/story.json + images/               sursa fiecarei povesti (editabila si din Editor)
    LastStopNight/Assets/stories/<id>.zip           arhivele incarcate de Player

ATENTIE: suprascrie stories/<id>/story.json. Daca ai modificat o poveste in Editor, n-o mai reconstrui de aici.
"""
import json
import os
import shutil
import sys
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOLS = os.path.join(ROOT, "tools")
sys.path.insert(0, TOOLS)
sys.path.insert(0, os.path.join(TOOLS, "stories"))

ART = os.path.join(ROOT, "stories", "_art")
OUT_ZIPS = os.path.join(ROOT, "LastStopNight", "Assets", "stories")

import diner1987  # noqa: E402
import km13  # noqa: E402
import night_shift  # noqa: E402

MODULES = [night_shift, km13, diner1987]


def referenced_images(node, found):
    if isinstance(node, dict):
        for v in node.values():
            referenced_images(v, found)
    elif isinstance(node, list):
        for v in node:
            referenced_images(v, found)
    elif isinstance(node, str) and node.startswith("images/"):
        found.add(node)
    return found


def build(module):
    story = module.STORY
    folder = os.path.join(ROOT, "stories", module.ID)
    if os.path.isdir(os.path.join(folder, "images")):
        shutil.rmtree(os.path.join(folder, "images"))
    os.makedirs(folder, exist_ok=True)
    with open(os.path.join(folder, "story.json"), "w", encoding="utf-8") as f:
        json.dump(story, f, ensure_ascii=False, indent=2)

    images = sorted(referenced_images(story, set()))
    for rel in images:
        src = os.path.join(ART, rel)
        if not os.path.exists(src):
            raise SystemExit(f"[{module.ID}] lipsește imaginea {rel}")
        dst = os.path.join(folder, rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copyfile(src, dst)

    os.makedirs(OUT_ZIPS, exist_ok=True)
    zip_path = os.path.join(OUT_ZIPS, module.ID + ".zip")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        z.write(os.path.join(folder, "story.json"), "story.json")
        for rel in images:
            z.write(os.path.join(folder, rel), rel)
    endings = sum(1 for b in story["blocks"] if b.get("isEnding"))
    print(f"  {module.ID}: {len(story['blocks'])} blocuri, {endings} finaluri, {len(images)} imagini -> {zip_path}")


def main():
    if "--no-art" not in sys.argv:
        import art
        count = art.generate(os.path.join(ART, "images"))
        print(f"  {count} imagini desenate în {os.path.join(ART, 'images')}")
    for m in MODULES:
        build(m)


if __name__ == "__main__":
    main()
