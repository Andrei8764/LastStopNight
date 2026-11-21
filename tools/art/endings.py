"""Imaginile de final ale celor trei povesti."""
import random

from PIL import Image, ImageDraw

from .diner import diner_exterior, diner_interior, office
from .gas_station import gas_dawn, gas_exterior, gas_store
from .pixel import H, W, add_light, dgrad, forest, grain, text, text_width, tint, vignette


def ending_sunrise():
    img = Image.new("RGB", (W, H))
    dgrad(img, (0, 0, W, 120), (40, 30, 70), (250, 150, 70), steps=10)
    d = ImageDraw.Draw(img)
    d.ellipse([140, 92, 184, 136], fill=(255, 214, 120))
    light = Image.new("RGB", (W, H))
    ImageDraw.Draw(light).ellipse([110, 66, 214, 160], fill=(120, 70, 20))
    add_light(img, light, 12)
    forest(img, 124, 50, 24, 50, (70, 40, 60), 41)
    forest(img, 130, 20, 40, 76, (40, 24, 40), 42)
    dgrad(img, (0, 128, W, H), (60, 40, 50), (24, 16, 24), steps=5)
    d = ImageDraw.Draw(img)
    d.polygon([(150, 128), (174, 128), (240, H), (84, H)], fill=(50, 40, 48))
    d.line([(162, 132), (162, 136)], fill=(200, 170, 120))
    d.line([(162, 144), (162, 152)], fill=(200, 170, 120))
    d.line([(162, 162), (162, 178)], fill=(200, 170, 120))
    # silueta lui Alex care pleaca
    d.rectangle([170, 140, 173, 150], fill=(20, 14, 20))
    d.ellipse([169, 135, 174, 140], fill=(20, 14, 20))
    d.line([(170, 150), (169, 157)], fill=(20, 14, 20))
    d.line([(173, 150), (174, 157)], fill=(20, 14, 20))
    # restaurantul stins, in departare
    d.rectangle([40, 112, 92, 128], fill=(30, 20, 30))
    d.rectangle([52, 104, 76, 110], fill=(30, 20, 30))
    text(d, 54, 105, "DINER", (60, 30, 40))
    return img


def ending_captured():
    img = diner_interior(lights_on=False)
    img = tint(img, (90, 0, 0), 0.35)
    d = ImageDraw.Draw(img)
    d.rectangle([110, 22, 210, 132], fill=(150, 110, 40))
    d.rectangle([114, 26, 206, 128], fill=(24, 20, 18))
    tw = text_width("EMPLOYEE OF THE MONTH")
    text(d, 160 - tw // 2, 30, "EMPLOYEE OF THE MONTH", (200, 170, 90))
    # portretul lui Alex, palid, cu ochi negri
    d.ellipse([140, 44, 180, 92], fill=(186, 170, 160))
    d.rectangle([140, 40, 180, 54], fill=(46, 32, 24))
    d.ellipse([140, 36, 180, 56], fill=(46, 32, 24))
    d.rectangle([150, 62, 156, 68], fill=(5, 5, 5))
    d.rectangle([164, 62, 170, 68], fill=(5, 5, 5))
    d.line([(152, 80), (168, 80)], fill=(90, 50, 50))
    d.rectangle([130, 92, 190, 124], fill=(52, 56, 66))
    d.rectangle([168, 100, 176, 103], fill=(230, 230, 220))
    text(d, 146, 116, "ALEX", (230, 230, 220))
    light = Image.new("RGB", (W, H))
    ImageDraw.Draw(light).ellipse([100, 10, 220, 150], fill=(70, 10, 10))
    add_light(img, light, 18)
    vignette(img, 0.7)
    return img


def ending_replaced():
    img = diner_interior(lights_on=True)
    img = tint(img, (10, 30, 70), 0.4)
    d = ImageDraw.Draw(img)
    # Alex palid, asezat la masa de langa geam, fara reflexie
    x = 104
    d.rectangle([x, 100, x + 12, 120], fill=(70, 74, 86))
    d.ellipse([x + 1, 86, x + 11, 99], fill=(190, 196, 200))
    d.rectangle([x + 1, 84, x + 11, 89], fill=(50, 40, 34))
    d.point((x + 8, 92), fill=(0, 0, 0))
    d.point((x + 4, 92), fill=(0, 0, 0))
    d.rectangle([x + 12, 114, x + 18, 117], fill=(190, 196, 200))
    d.rectangle([x + 18, 112, x + 21, 117], fill=(230, 230, 230))
    vignette(img, 0.7)
    return img


def ending_secret():
    img = Image.new("RGB", (W, H))
    dgrad(img, (0, 0, W, H), (196, 178, 140), (150, 128, 92), steps=6)
    grain(img, (0, 0, W, H), 8, seed=13)
    d = ImageDraw.Draw(img)
    rnd = random.Random(6)
    for _ in range(26):
        x, y = rnd.randint(0, W), rnd.randint(0, H)
        r = rnd.randint(10, 26)
        d.ellipse([x - r, y - r // 2, x + r, y + r // 2], fill=(110, 130, 90))
    for _ in range(140):
        x, y = rnd.randint(0, W), rnd.randint(0, H)
        d.point((x, y), fill=(70, 90, 60))
    pts = [(0, 120), (60, 110), (110, 96), (160, 92), (210, 84), (260, 70), (320, 60)]
    d.line(pts, fill=(120, 60, 40), width=3)
    d.line(pts, fill=(220, 200, 150), width=1)
    d.line([(160, 92), (150, 140), (170, 180)], fill=(160, 120, 80), width=2)
    text(d, 60, 124, "BLACK PINE RD", (80, 50, 40))
    for r in (14, 15):
        d.ellipse([178 - r, 88 - r, 178 + r, 88 + r], outline=(180, 20, 24))
    text(d, 176, 85, "?", (180, 20, 24))
    # fotografii vechi, pe harta
    for i, (x, y) in enumerate(((24, 20), (46, 30), (250, 116))):
        d.rectangle([x, y, x + 30, y + 22], fill=(230, 220, 200))
        d.rectangle([x + 2, y + 2, x + 28, y + 17], fill=(110, 96, 80))
        for k in range(4):
            d.rectangle([x + 4 + k * 6, y + 7, x + 7 + k * 6, y + 16], fill=(60, 50, 40))
        text(d, x + 3, y + 17, ("1961", "1987", "2026")[i], (90, 70, 50))
    img = tint(img, (20, 20, 40), 0.25)
    vignette(img, 0.75)
    return img


def ending_quit():
    img = Image.new("RGB", (W, H))
    dgrad(img, (0, 0, W, 110), (8, 8, 18), (24, 22, 36), steps=6)
    forest(img, 112, 60, 30, 70, (12, 14, 20), 51)
    dgrad(img, (0, 108, W, H), (16, 16, 20), (6, 6, 8), steps=4)
    d = ImageDraw.Draw(img)
    d.polygon([(150, 110), (170, 110), (300, H), (20, H)], fill=(30, 30, 34))
    for y0, y1 in ((114, 118), (126, 134), (146, 160)):
        d.line([(160, y0), (160, y1)], fill=(170, 160, 110))
    # masina care pleaca
    d.rectangle([150, 120, 170, 130], fill=(18, 18, 22))
    d.rectangle([152, 116, 168, 121], fill=(18, 18, 22))
    d.rectangle([151, 125, 154, 127], fill=(255, 60, 60))
    d.rectangle([166, 125, 169, 127], fill=(255, 60, 60))
    light = Image.new("RGB", (W, H))
    ld = ImageDraw.Draw(light)
    ld.ellipse([140, 116, 164, 136], fill=(120, 10, 10))
    ld.ellipse([156, 116, 180, 136], fill=(120, 10, 10))
    # firma DINER in oglinda retrovizoare
    d.rectangle([14, 14, 94, 46], fill=(30, 30, 36))
    d.rectangle([16, 16, 92, 44], fill=(10, 10, 16))
    text(d, 34, 26, "RENID", (240, 60, 70), sc=2)
    nd = ImageDraw.Draw(light)
    text(nd, 34, 26, "RENID", (160, 10, 20), sc=2)
    add_light(img, light, 4)
    vignette(img, 0.55)
    return img


def ending_cat():
    """Alex in masina, la rasarit, cu pisica pe scaunul din dreapta."""
    img = Image.new("RGB", (W, H))
    dgrad(img, (0, 0, W, 110), (90, 60, 110), (250, 170, 90), steps=10)
    forest(img, 108, 40, 20, 46, (80, 50, 70), 61)
    dgrad(img, (0, 106, W, 140), (70, 50, 60), (40, 30, 40), steps=4)
    d = ImageDraw.Draw(img)
    d.polygon([(140, 106), (180, 106), (260, 140), (60, 140)], fill=(60, 50, 56))
    # interiorul masinii (bord + parbriz)
    d.rectangle([0, 0, W, 12], fill=(20, 18, 22))
    d.polygon([(0, 12), (30, 12), (0, 100)], fill=(20, 18, 22))
    d.polygon([(W, 12), (W - 30, 12), (W, 100)], fill=(20, 18, 22))
    d.rectangle([0, 132, W, H], fill=(30, 26, 30))
    d.rectangle([0, 130, W, 134], fill=(50, 44, 48))
    d.ellipse([40, 120, 110, 190], outline=(15, 12, 14), width=6)  # volan
    d.rectangle([150, 128, 190, 132], fill=(80, 200, 140))  # radio
    text(d, 152, 136, "06:02", (90, 220, 150))
    # pisica pe scaun, la dreapta
    cx, cy = 236, 150
    c = (14, 12, 16)
    d.rectangle([200, 140, 300, H], fill=(70, 40, 36))
    d.ellipse([cx - 14, cy - 10, cx + 14, cy + 16], fill=c)
    d.ellipse([cx - 8, cy - 26, cx + 10, cy - 6], fill=c)
    d.polygon([(cx - 8, cy - 20), (cx - 6, cy - 32), (cx - 1, cy - 24)], fill=c)
    d.polygon([(cx + 4, cy - 24), (cx + 9, cy - 32), (cx + 10, cy - 19)], fill=c)
    d.rectangle([cx - 4, cy - 18, cx - 2, cy - 16], fill=(240, 210, 60))
    d.rectangle([cx + 4, cy - 18, cx + 6, cy - 16], fill=(240, 210, 60))
    d.line([(cx + 14, cy + 10), (cx + 24, cy), (cx + 22, cy - 10)], fill=c, width=3)
    light = Image.new("RGB", (W, H))
    ImageDraw.Draw(light).ellipse([120, 60, 200, 130], fill=(110, 70, 20))
    add_light(img, light, 14)
    vignette(img, 0.5)
    return img


def km13_ending(kind):
    if kind == "peace":
        img = gas_dawn()
        d = ImageDraw.Draw(img)
        # pelerina galbena, impaturita pe banca
        d.rectangle([140, 140, 186, 143], fill=(110, 80, 50))
        d.rectangle([142, 144, 144, 152], fill=(80, 60, 40))
        d.rectangle([182, 144, 184, 152], fill=(80, 60, 40))
        d.polygon([(152, 140), (172, 140), (168, 134), (156, 134)], fill=(232, 196, 40))
        return img
    if kind == "lights":
        img = gas_exterior()
        light = Image.new("RGB", (W, H))
        ld = ImageDraw.Draw(light)
        ld.ellipse([110, 110, 210, 170], fill=(230, 230, 200))
        ld.ellipse([160, 110, 260, 170], fill=(230, 230, 200))
        add_light(img, light, 10)
        add_light(img, light, 2)
        return img
    if kind == "radio":
        img = Image.new("RGB", (W, H))
        dgrad(img, (0, 0, W, H), (30, 22, 18), (12, 8, 6), steps=6)
        d = ImageDraw.Draw(img)
        d.rectangle([60, 40, 260, 150], fill=(110, 72, 44))
        d.rectangle([66, 46, 254, 144], outline=(70, 46, 28))
        d.rectangle([80, 60, 200, 100], fill=(230, 210, 150))
        for i in range(13):
            d.line([(86 + i * 9, 64), (86 + i * 9, 70 + (i % 2) * 4)], fill=(60, 40, 30))
        d.rectangle([136, 60, 138, 100], fill=(200, 30, 30))
        text(d, 112, 86, "13.13 MHZ", (60, 40, 30))
        for cx in (222, 222):
            d.ellipse([cx - 16, 66, cx + 16, 98], fill=(60, 40, 26))
        for k in range(8):
            d.line([(80, 110 + k * 4), (240, 110 + k * 4)], fill=(80, 54, 34))
        grain(img, (0, 0, W, H), 14, seed=4)
        vignette(img, 0.7)
        return img
    if kind == "wait":
        img = gas_store()
        img = tint(img, (250, 220, 170), 0.25)
        return img
    raise ValueError(kind)


def ending_d87(kind):
    if kind == "flash":
        img = diner_exterior()
        img = tint(img, (200, 170, 120), 0.35)
        d = ImageDraw.Draw(img)
        for i, x in enumerate(range(130, 260, 24)):
            d.rectangle([x, 128, x + 10, 152], fill=(80, 70, 60))
            d.ellipse([x + 1, 118, x + 9, 128], fill=(200, 176, 150))
            d.rectangle([x + 2, 132, x + 8, 144], fill=(230, 220, 200))
        light = Image.new("RGB", (W, H))
        ImageDraw.Draw(light).ellipse([90, 70, 290, 190], fill=(150, 146, 130))
        add_light(img, light, 40)
        return img
    if kind == "road":
        img = Image.new("RGB", (W, H))
        dgrad(img, (0, 0, W, 110), (40, 30, 60), (200, 120, 90), steps=8)
        forest(img, 112, 50, 30, 64, (40, 26, 40), 88)
        dgrad(img, (0, 108, W, H), (40, 30, 34), (16, 12, 14), steps=4)
        d = ImageDraw.Draw(img)
        d.polygon([(150, 110), (170, 110), (300, H), (20, H)], fill=(36, 32, 36))
        d.rectangle([148, 120, 172, 132], fill=(140, 30, 30))  # masina rosie din '87
        d.rectangle([152, 115, 168, 121], fill=(120, 26, 26))
        img = tint(img, (200, 160, 110), 0.2)
        vignette(img, 0.6)
        return img
    if kind == "owner":
        img = office(bare=True)
        img = tint(img, (80, 50, 30), 0.3)
        d = ImageDraw.Draw(img)
        # silueta din spatele biroului, cu sapca
        d.rectangle([150, 84, 176, 112], fill=(200, 196, 184))
        d.rectangle([152, 66, 174, 84], fill=(70, 56, 50))
        d.rectangle([150, 62, 178, 70], fill=(40, 44, 40))
        d.rectangle([170, 68, 182, 70], fill=(30, 32, 30))
        vignette(img, 0.7)
        return img
    raise ValueError(kind)
