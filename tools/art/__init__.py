"""Toata grafica jocului, desenata procedural (fara resurse externe) si determinist (seed-uri fixe).

Fiecare modul deseneaza o tema (diner.py, gas_station.py, characters.py...). Catalogul de mai jos
spune ce fisier PNG produce fiecare functie; `generate()` le scrie pe disc. Aceeasi comanda da mereu
exact aceleasi fisiere, bit cu bit, deci imaginile pot fi regenerate oricand fara surprize.
"""
import os

from . import closeups, decor, diner, endings, gas_station, icons, props
from .characters import KINDS, child, person, sheet_of

HUD_ICONS = ("heart", "eye", "bolt", "coin", "book", "clue", "moon")


def catalog():
    """Lista (cale relativa in images/, functie care deseneaza imaginea)."""
    items = [
        # Black Pine Diner
        ("diner_exterior.png", diner.diner_exterior),
        ("diner_exterior_dawn.png", diner.diner_exterior_dawn),
        ("diner_interior.png", lambda: diner.diner_interior(True)),
        ("kitchen.png", diner.kitchen),
        ("office.png", diner.office),
        ("office_1987.png", lambda: diner.office(bare=True)),
        # benzinaria de la km 13
        ("gas_exterior.png", gas_station.gas_exterior),
        ("gas_store.png", gas_station.gas_store),
        ("gas_back.png", gas_station.gas_back),
        ("gas_dawn.png", gas_station.gas_dawn),
        # finaluri
        ("endings/ending_sunrise.png", endings.ending_sunrise),
        ("endings/ending_captured.png", endings.ending_captured),
        ("endings/ending_replaced.png", endings.ending_replaced),
        ("endings/ending_secret.png", endings.ending_secret),
        ("endings/ending_quit.png", endings.ending_quit),
        ("endings/ending_cat.png", endings.ending_cat),
        ("endings/km13_peace.png", lambda: endings.km13_ending("peace")),
        ("endings/km13_lights.png", lambda: endings.km13_ending("lights")),
        ("endings/km13_radio.png", lambda: endings.km13_ending("radio")),
        ("endings/km13_wait.png", lambda: endings.km13_ending("wait")),
        ("endings/d87_flash.png", lambda: endings.ending_d87("flash")),
        ("endings/d87_road.png", lambda: endings.ending_d87("road")),
        ("endings/d87_owner.png", lambda: endings.ending_d87("owner")),
        # prim-planuri
        ("closeups/handbook.png", closeups.closeup_handbook),
        ("closeups/photo.png", closeups.closeup_photo),
        ("closeups/camera.png", closeups.closeup_camera),
        ("closeups/newspaper.png", closeups.closeup_newspaper),
        ("closeups/contract.png", closeups.closeup_contract),
        # decor animat
        ("decor/fan.png", decor.decor_fan),
        ("decor/open_sign.png", decor.decor_open_sign),
        ("decor/steam.png", decor.decor_steam),
        ("decor/cat.png", decor.decor_cat),
        ("decor/led.png", decor.decor_led),
        ("decor/moths.png", decor.decor_moths),
        ("decor/tube.png", decor.decor_tube),
        ("decor/radio.png", decor.decor_radio),
        ("decor/wall_phone.png", props.wall_phone),
        # obiecte care apar doar in unele blocuri
        ("props/car.png", lambda: props.car_shadow_rows(props.car(lights=True))),
        ("props/car_driver.png", lambda: props.car_shadow_rows(props.car(driver=True, lights=True))),
        ("props/beams.png", props.beams),
        ("props/note.png", props.note),
        ("props/photo_camera.png", props.photo_camera),
    ]
    items += [(f"sprites/{kind}.png", lambda k=kind: sheet_of(lambda f: person(k, f))) for kind in KINDS]
    items.append(("sprites/girl.png", lambda: sheet_of(child)))
    items += [(f"ui/{name}.png", lambda n=name: icons.icon(n)) for name in HUD_ICONS]
    return items


def generate(images_dir):
    """Deseneaza tot catalogul in images_dir. Intoarce numarul de imagini scrise."""
    items = catalog()
    for rel, draw in items:
        path = os.path.join(images_dir, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        draw().save(path)
    return len(items)
