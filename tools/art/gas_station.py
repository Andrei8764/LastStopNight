"""Benzinaria de la km 13: pompele, magazinul, magazia si pompele in zori."""
import random

from PIL import Image, ImageDraw

from .pixel import H, W, add_light, checker_floor, dgrad, forest, grain, text, tint, vignette


def gas_exterior():
    img = Image.new("RGB", (W, H))
    dgrad(img, (0, 0, W, 112), (8, 10, 22), (26, 30, 46), steps=8)
    rnd = random.Random(7)
    d = ImageDraw.Draw(img)
    for _ in range(35):
        v = rnd.randint(60, 150)
        d.point((rnd.randint(0, W - 1), rnd.randint(0, 60)), fill=(v, v, v + 10))
    forest(img, 112, 55, 26, 58, (14, 18, 26), 71)
    forest(img, 116, 18, 46, 80, (9, 11, 16), 72)
    dgrad(img, (0, 112, W, H), (30, 30, 34), (12, 12, 14), steps=5)
    grain(img, (0, 112, W, H), 4, seed=9)
    d = ImageDraw.Draw(img)
    for x in range(0, W, 30):
        d.rectangle([x, 170, x + 14, 171], fill=(180, 170, 110))  # marcaj drum
    light = Image.new("RGB", (W, H))
    ld = ImageDraw.Draw(light)
    # magazinul (dreapta)
    d.rectangle([196, 74, 318, 140], fill=(56, 54, 58))
    d.rectangle([196, 70, 318, 76], fill=(160, 30, 36))
    text(d, 222, 71, "KM 13 MARKET", (250, 240, 230))
    for x in (204, 236, 268):
        d.rectangle([x, 86, x + 26, 118], fill=(210, 220, 190))
        d.line([(x, 102), (x + 26, 102)], fill=(150, 160, 130))
        for k in range(3):
            d.rectangle([x + 3 + k * 8, 104, x + 8 + k * 8, 117], fill=(120 + k * 30, 90, 70))
        ld.rectangle([x - 3, 84, x + 29, 120], fill=(70, 74, 54))
    d.rectangle([298, 86, 314, 140], fill=(190, 200, 170))
    d.line([(306, 86), (306, 140)], fill=(90, 90, 80))
    ld.rectangle([294, 84, 318, 142], fill=(60, 64, 46))
    # copertina pompelor
    d.rectangle([30, 52, 186, 60], fill=(200, 200, 204))
    d.rectangle([30, 60, 186, 63], fill=(160, 30, 36))
    for px_ in (40, 172):
        d.rectangle([px_, 63, px_ + 4, 140], fill=(150, 150, 156))
    ld.polygon([(40, 64), (176, 64), (196, 150), (20, 150)], fill=(34, 40, 44))
    # pompe
    for px_ in (70, 126):
        d.rectangle([px_, 104, px_ + 18, 140], fill=(190, 40, 44))
        d.rectangle([px_ + 3, 108, px_ + 15, 116], fill=(30, 40, 30))
        text(d, px_ + 4, 110, "13", (120, 230, 140))
        d.rectangle([px_ + 2, 120, px_ + 6, 126], fill=(40, 40, 40))
        d.line([(px_ + 18, 118), (px_ + 22, 130), (px_ + 20, 140)], fill=(20, 20, 20))
        d.rectangle([px_ - 2, 140, px_ + 20, 143], fill=(120, 120, 120))
    # panoul cu preturi
    d.rectangle([4, 40, 26, 90], fill=(30, 30, 34))
    d.rectangle([14, 90, 16, 140], fill=(60, 60, 64))
    text(d, 7, 44, "KM", (240, 60, 60))
    text(d, 7, 51, "13", (240, 60, 60))
    for i, pr in enumerate(("6.13", "6.66", "7.13")):
        text(d, 7, 61 + i * 8, pr, (255, 200, 80))
    ld.rectangle([2, 38, 28, 92], fill=(60, 30, 20))
    # cabina telefonica
    d.rectangle([180, 100, 190, 140], fill=(40, 60, 90))
    d.rectangle([182, 104, 188, 120], fill=(110, 140, 160))
    add_light(img, light, 4)
    vignette(img, 0.5)
    return img


def gas_store():
    img = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(img)
    dgrad(img, (0, 0, W, 140), (170, 176, 166), (130, 136, 126), steps=5)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 8], fill=(60, 62, 60))
    for x in range(20, W, 60):
        d.rectangle([x, 9, x + 30, 11], fill=(240, 250, 240))
    # vitrina (ferestre pentru reflexii) cu pompele afara
    d.rectangle([10, 30, 130, 96], fill=(10, 14, 24))
    sub = gas_exterior().crop((20, 50, 200, 150)).resize((118, 64))
    sub = Image.blend(sub, Image.new("RGB", sub.size, (6, 8, 16)), 0.45)
    img.paste(sub, (11, 31))
    d = ImageDraw.Draw(img)
    for x in (10, 50, 90, 130):
        d.rectangle([x - 1, 28, x + 1, 98], fill=(90, 92, 96))
    d.rectangle([8, 28, 132, 29], fill=(110, 112, 116))
    d.rectangle([8, 96, 132, 98], fill=(110, 112, 116))
    # raft-uri
    rnd = random.Random(12)
    for sx in (140, 186):
        d.rectangle([sx, 64, sx + 38, 140], fill=(200, 200, 196))
        for sy in (70, 88, 106, 124):
            d.rectangle([sx, sy + 12, sx + 38, sy + 13], fill=(140, 140, 140))
            x = sx + 2
            while x < sx + 34:
                w = rnd.randint(3, 6)
                h = rnd.randint(5, 11)
                c = rnd.choice([(200, 40, 40), (240, 200, 40), (40, 120, 200), (60, 160, 80), (230, 120, 30), (150, 60, 160)])
                d.rectangle([x, sy + 12 - h, x + w, sy + 11], fill=c)
                x += w + 1
    # congelator cu lumina albastra
    d.rectangle([232, 70, 270, 140], fill=(150, 160, 170))
    d.rectangle([235, 74, 267, 132], fill=(120, 170, 220))
    for y in range(80, 132, 12):
        d.rectangle([238, y, 264, y + 1], fill=(200, 220, 240))
    light = Image.new("RGB", (W, H))
    ld = ImageDraw.Draw(light)
    ld.rectangle([228, 66, 274, 142], fill=(20, 40, 70))
    # tejgheaua cu casa de marcat, radioul sta pe ea (decor)
    d.rectangle([276, 108, 320, 112], fill=(120, 80, 50))
    d.rectangle([278, 113, 320, 150], fill=(90, 60, 40))
    d.rectangle([290, 96, 306, 108], fill=(60, 60, 64))
    d.rectangle([292, 98, 304, 102], fill=(80, 220, 120))
    # monitor CCTV deasupra
    d.rectangle([280, 26, 312, 50], fill=(40, 40, 44))
    d.rectangle([283, 29, 309, 47], fill=(70, 90, 80))
    for y in range(30, 47, 2):
        d.line([(284, y), (308, y)], fill=(56, 74, 66))
    # afis „copil disparut”
    d.rectangle([140, 26, 164, 56], fill=(226, 220, 196))
    text(d, 142, 28, "LIPSA", (160, 20, 20))
    d.rectangle([146, 35, 158, 47], fill=(232, 196, 40))
    d.rectangle([149, 38, 155, 46], fill=(220, 210, 200))
    for i in range(2):
        d.line([(143, 50 + i * 3), (161, 50 + i * 3)], fill=(100, 90, 80))
    # ceas
    d.ellipse([196, 20, 210, 34], fill=(230, 230, 220))
    d.line([(203, 27), (203, 22)], fill=(20, 20, 20))
    d.line([(203, 27), (207, 29)], fill=(20, 20, 20))
    checker_floor(img, 140, H, (190, 190, 180), (120, 124, 120), tile=16)
    add_light(img, light, 6)
    img = tint(img, (40, 60, 50), 0.15)
    vignette(img, 0.5)
    return img


def gas_back():
    img = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(img)
    dgrad(img, (0, 0, W, 142), (70, 66, 60), (46, 42, 38), steps=5)
    d = ImageDraw.Draw(img)
    for x in range(0, W, 24):
        d.line([(x, 0), (x, 142)], fill=(56, 52, 48))
    rnd = random.Random(21)
    # cutii stivuite
    for (x, y, w, h) in ((20, 110, 34, 32), (24, 84, 26, 26), (60, 118, 28, 24), (230, 104, 40, 38), (240, 76, 28, 28)):
        c = (150 + rnd.randint(-15, 15), 116, 70)
        d.rectangle([x, y, x + w, y + h], fill=c)
        d.line([(x, y + h // 2), (x + w, y + h // 2)], fill=(110, 84, 50))
        d.rectangle([x + 4, y + 4, x + 12, y + 8], fill=(220, 210, 190))
    # tabloul electric
    d.rectangle([110, 40, 140, 80], fill=(140, 144, 150))
    for r in range(3):
        for c in range(4):
            d.rectangle([114 + c * 6, 46 + r * 10, 117 + c * 6, 52 + r * 10], fill=(40, 40, 44))
    d.polygon([(122, 30), (126, 22), (124, 30), (128, 30), (122, 40), (124, 32)], fill=(240, 200, 40))
    # usa din spate
    d.rectangle([166, 60, 200, 142], fill=(80, 84, 90))
    d.rectangle([168, 100, 198, 103], fill=(160, 160, 164))
    d.rectangle([176, 66, 190, 78], fill=(12, 14, 20))
    # calendar 1998
    d.rectangle([210, 30, 234, 60], fill=(230, 226, 214))
    d.rectangle([210, 30, 234, 38], fill=(180, 30, 36))
    text(d, 212, 31, "1998", (250, 240, 230))
    for r in range(4):
        for c in range(5):
            d.point((213 + c * 4, 42 + r * 4), fill=(80, 70, 60))
    d.ellipse([220, 49, 226, 55], outline=(200, 20, 20))
    # galeata si mop
    d.rectangle([290, 120, 306, 142], fill=(60, 90, 140))
    d.line([(298, 60), (302, 124)], fill=(150, 120, 80), width=2)
    # bec
    d.line([(160, 0), (160, 16)], fill=(30, 30, 30))
    d.ellipse([156, 16, 164, 24], fill=(255, 230, 160))
    checker_floor(img, 142, H, (90, 86, 80), (60, 56, 52), tile=14)
    light = Image.new("RGB", (W, H))
    ld = ImageDraw.Draw(light)
    ld.ellipse([110, -20, 210, 70], fill=(90, 70, 30))
    ld.polygon([(156, 24), (164, 24), (230, 176), (90, 176)], fill=(40, 32, 14))
    add_light(img, light, 8)
    vignette(img, 0.65)
    return img


def gas_dawn():
    img = gas_exterior()
    img = tint(img, (240, 160, 120), 0.3)
    dgrad(img, (0, 0, W, 34), (110, 80, 120), (220, 140, 110), steps=6)
    return img
