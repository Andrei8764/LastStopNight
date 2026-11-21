"""Mini-limbaj pentru scrierea povestilor in Python.

Fiecare functie intoarce un dictionar cu exact structura din story.json (aceleasi chei, in aceeasi ordine),
ca povestile sa se scrie scurt: block(...), dec(...), look(...) in loc de JSON scris de mana.
"""


# ---------------------------------------------------------------- conditii si efecte

def cmp(key, op, value):
    return {"type": "COMPARISON", "key": key, "operator": op, "value": value}


def AND(*children):
    return {"type": "AND", "children": list(children)}


def OR(*children):
    return {"type": "OR", "children": list(children)}


def add(key, value):
    return {"key": key, "op": "add", "value": value}


def setv(key, value):
    return {"key": key, "op": "set", "value": value}


# ---------------------------------------------------------------- blocuri, decizii, obiecte

def dec(text, target, effects=None, condition=None, hotspot=None, hint=None, show_locked=True, hy=None, requires=None):
    """Decizie. hotspot/hy = punctul din scena (x, y) unde se poate alege cu E; requires = doar cu acel insotitor."""
    d = {"text": text, "target": target, "effects": effects or []}
    if requires:
        d["requiresCharacter"] = requires
    if hy is not None:
        d["hotspotY"] = hy
    if condition is not None:
        d["condition"] = condition
    if hotspot is not None:
        d["hotspotX"] = hotspot
    if hint:
        d["lockedHint"] = hint
    if not show_locked:
        d["showWhenLocked"] = False
    return d


def npc(cid, x, facing_left=False):
    """Personaj (NPC) asezat in scena unui bloc."""
    return {"id": cid, "x": x, "facingLeft": facing_left}


def look(id, x, label, text, effects=None, condition=None, hidden=False, once=True, y=None, by=None):
    """Obiect de examinat (tasta E). by = ce observa un insotitor la el (id personaj -> text)."""
    e = {"id": id, "x": x, "label": label, "text": text, "effects": effects or [], "once": once, "hidden": hidden,
         "textBy": by or {}}
    if y is not None:
        e["y"] = y
    if condition is not None:
        e["condition"] = condition
    return e


def block(id, title, text, location=None, time="", decisions=None, chapter=None, mood="normal", fx=None,
          player_x=None, characters=None, closeup=None, background=None, ending=None, ending_title=None,
          examine=None, props=None, lights=None):
    """Bloc narativ. ending = "good" / "bad" / "secret" / "alternative" face din el un final."""
    b = {"id": id, "title": title, "text": text, "location": location, "time": time, "mood": mood,
         "fx": fx or [], "characters": characters or [], "examine": examine or [], "decisions": decisions or [],
         "props": props or [], "lights": lights or []}
    if chapter:
        b["chapter"] = chapter
    if player_x is not None:
        b["playerX"] = player_x
    if closeup:
        b["closeup"] = closeup
    if background:
        b["background"] = background
    if ending:
        b["isEnding"] = True
        b["endingType"] = ending
        b["endingTitle"] = ending_title or title
    return b


def playable(cid, role, blurb):
    """Personaj care poate fi ales din meniu (protagonistul sau un insotitor)."""
    return {"characterId": cid, "role": role, "blurb": blurb}


# ---------------------------------------------------------------- insotitori

def add_companion(blocks, companion, texts, solo=()):
    """Adauga in blocurile existente paragrafele insotitorului (texts: id bloc -> paragraf) si marcheaza
    scenele in care protagonistul e singur (solo: insotitorul nu apare)."""
    by_id = {b["id"]: b for b in blocks}
    for block_id, text in texts.items():
        by_id[block_id].setdefault("companionText", {})[companion] = text
    for block_id in solo:
        by_id[block_id]["solo"] = True


def add_remarks(examines, companion, remarks):
    """Ce zice insotitorul la obiectele de examinat (remarks: id obiect -> text)."""
    for e in examines:
        if e["id"] in remarks:
            e["textBy"][companion] = remarks[e["id"]]


# ---------------------------------------------------------------- poveste, personaje, scena

def prop(key, lo, hi, init, label, display="value", icon=None, color=None, fmt=None):
    """Proprietate de stare afisata in HUD (display: bar / value / flag / hidden)."""
    p = {"key": key, "min": lo, "max": hi, "initial": init, "hudLabel": label, "display": display}
    if icon:
        p["icon"] = "images/ui/%s.png" % icon
    if color:
        p["color"] = color
    if fmt:
        p["format"] = fmt
    return p


def char(id, name, has_reflection=True, desc=""):
    """Personaj cu spritesheet-ul standard images/sprites/<id>.png (24x48, 2 cadre de stat + 4 de mers)."""
    return {"id": id, "name": name, "sprite": "images/sprites/%s.png" % id, "frameWidth": 24, "frameHeight": 48,
            "idleFrames": [0, 1], "walkFrames": [2, 3, 4, 5], "hasReflection": has_reflection, "description": desc}


def decor(image, x, y, fw, fh, fps=4, random=False, front=False):
    """Decor animat al unei locatii, din images/decor/."""
    return {"image": "images/decor/%s.png" % image, "x": x, "y": y, "frameWidth": fw, "frameHeight": fh,
            "fps": fps, "random": random, "front": front}


# dimensiunile obiectelor din images/props/ (latime, inaltime)
PROP_SIZES = {"car": (80, 30), "car_driver": (80, 30), "beams": (130, 50), "note": (11, 6), "photo_camera": (9, 8)}


def obj(name, x, y, front=False):
    """Obiect care exista doar intr-un bloc (masina de la pompa, biletul de pe tejghea...)."""
    w, h = PROP_SIZES[name]
    return {"image": "images/props/%s.png" % name, "x": x, "y": y, "frameWidth": w, "frameHeight": h,
            "fps": 1, "random": False, "front": front}


def light(x, y, r, color, alpha=60, flicker=0.0):
    """Halou de lumina; flicker 0 = constant, 1 = palpaie puternic."""
    return {"x": x, "y": y, "radius": r, "color": color, "alpha": alpha, "flicker": flicker}
