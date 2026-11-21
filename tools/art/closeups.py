"""Prim-planurile (160x120): registrul, fotografia, monitorul, ziarul, contractul."""
import random

from PIL import Image, ImageDraw

from .diner import diner_interior
from .pixel import add_light, dgrad, grain, pine, text, tint


def closeup_handbook():
    img = Image.new("RGB", (160, 120))
    dgrad(img, (0, 0, 160, 120), (60, 40, 26), (34, 22, 14), steps=5)
    d = ImageDraw.Draw(img)
    for y in range(0, 120, 9):
        d.line([(0, y), (160, y + 3)], fill=(46, 30, 20))
    # coperta
    d.polygon([(10, 14), (70, 10), (76, 108), (16, 112)], fill=(22, 30, 26))
    d.polygon([(12, 16), (68, 12), (74, 106), (18, 110)], outline=(110, 90, 60))
    text(d, 24, 26, "EMPLOYEE", (196, 176, 130))
    text(d, 24, 34, "HANDBOOK", (196, 176, 130))
    pine(d, 44, 86, 36, (170, 150, 110))
    text(d, 26, 94, "BLACK PINE", (150, 120, 90))
    # pagina
    d.polygon([(82, 12), (150, 16), (146, 112), (78, 108)], fill=(222, 210, 180))
    grain(img, (78, 12, 150, 112), 10, seed=3)
    d = ImageDraw.Draw(img)
    text(d, 92, 20, "REGULI", (150, 20, 20))
    rnd = random.Random(5)
    y = 32
    for i in range(1, 7):
        text(d, 88, y, str(i) + ".", (40, 30, 26))
        for k in range(2):
            ln = rnd.randint(28, 46)
            d.line([(97, y + 1 + k * 5), (97 + ln, y + 1 + k * 5)], fill=(80, 66, 56))
        y += 12
    d.line([(100, 104), (138, 104)], fill=(160, 30, 30))
    return img


def closeup_photo():
    img = Image.new("RGB", (160, 120))
    dgrad(img, (0, 0, 160, 120), (40, 30, 24), (20, 14, 10), steps=4)
    d = ImageDraw.Draw(img)
    d.rectangle([14, 8, 146, 112], fill=(232, 222, 198))
    d.rectangle([22, 16, 138, 94], fill=(150, 126, 96))
    dgrad(img, (22, 16, 138, 50), (190, 170, 136), (150, 126, 96), steps=4)
    d = ImageDraw.Draw(img)
    d.rectangle([30, 40, 130, 70], fill=(110, 90, 70))
    d.rectangle([60, 26, 100, 36], fill=(80, 64, 50))
    text(d, 66, 29, "DINER", (200, 180, 150))
    for i in range(5):
        x = 36 + i * 20
        skin = (200, 176, 150)
        d.ellipse([x, 52, x + 10, 63], fill=skin)
        d.rectangle([x, 51, x + 10, 55], fill=(70, 54, 40))
        d.rectangle([x - 1, 64, x + 11, 94], fill=(80, 70, 60))
        d.rectangle([x + 1, 66, x + 9, 80], fill=(220, 210, 190))  # sort
        d.point((x + 3, 57), fill=(30, 20, 14))
        d.point((x + 7, 57), fill=(30, 20, 14))
    # al treilea: Alex, incercuit
    d.ellipse([70, 46, 90, 70], outline=(170, 20, 20))
    d.rectangle([77, 70, 83, 72], fill=(250, 250, 240))
    text(d, 30, 100, "BLACK PINE 1987", (90, 70, 50))
    grain(img, (14, 8, 146, 112), 12, seed=7)
    rnd = random.Random(8)
    d = ImageDraw.Draw(img)
    for _ in range(4):
        x = rnd.randint(22, 138)
        y0 = rnd.randint(16, 50)
        d.line([(x, y0), (x + rnd.randint(-4, 4), y0 + rnd.randint(15, 40))], fill=(186, 170, 146))
    return img


def closeup_camera():
    """Monitorul de supraveghere: carcasa CRT, imagine verzuie cu linii, el (celalalt Alex) care face cu mana."""
    img = Image.new("RGB", (160, 120))
    dgrad(img, (0, 0, 160, 120), (26, 20, 16), (12, 9, 8), steps=4)
    d = ImageDraw.Draw(img)
    # carcasa
    d.rounded_rectangle([6, 4, 154, 112], radius=10, fill=(150, 144, 124))
    d.rounded_rectangle([6, 4, 154, 112], radius=10, outline=(96, 92, 78))
    d.line([(14, 6), (146, 6)], fill=(196, 190, 170))
    d.rounded_rectangle([14, 11, 146, 97], radius=8, fill=(40, 40, 36))
    # imaginea de pe camera: sala, in tonuri de verde
    sub = diner_interior(lights_on=True).crop((150, 40, 320, 180)).resize((124, 78), Image.NEAREST)
    sub = sub.convert("L").convert("RGB")
    sub = tint(sub, (70, 130, 90), 0.45)
    mask = Image.new("L", sub.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, 123, 77], radius=7, fill=255)
    img.paste(sub, (18, 15), mask)
    d = ImageDraw.Draw(img)
    # celalalt eu, la tejghea, intors spre camera, cu mana ridicata
    x = 66
    d.rectangle([x - 2, 56, x + 10, 84], fill=(14, 22, 16))
    d.rectangle([x, 68, x + 8, 84], fill=(190, 206, 190))          # sortul
    d.ellipse([x - 1, 44, x + 9, 56], fill=(226, 240, 226))
    d.rectangle([x - 1, 43, x + 9, 47], fill=(14, 22, 16))
    d.point((x + 2, 50), fill=(250, 255, 250))
    d.point((x + 6, 50), fill=(250, 255, 250))
    d.line([(x + 2, 53), (x + 6, 53)], fill=(60, 70, 62))
    d.line([(x + 10, 58), (x + 16, 48)], fill=(14, 22, 16), width=3)  # bratul ridicat
    d.rectangle([x + 15, 44, x + 18, 47], fill=(226, 240, 226))
    # linii de scanare, zgomot, o banda de interferenta
    rnd = random.Random(4)
    for y in range(15, 93, 2):
        d.line([(18, y), (141, y)], fill=(22, 34, 26))
    for _ in range(140):
        v = rnd.randint(90, 170)
        d.point((rnd.randint(18, 141), rnd.randint(15, 92)), fill=(v, v + 20, v))
    for y in range(70, 74):
        d.line([(18, y), (141, y)], fill=(110, 140, 116))
    text(d, 22, 18, "CAM 2", (220, 236, 220))
    text(d, 104, 18, "01:22", (220, 236, 220))
    d.ellipse([22, 84, 26, 88], fill=(230, 30, 30))
    text(d, 29, 83, "REC", (236, 70, 70))
    # stralucirea sticlei curbate
    light = Image.new("RGB", (160, 120))
    ImageDraw.Draw(light).ellipse([30, 14, 110, 50], fill=(26, 34, 28))
    add_light(img, light, 8)
    d = ImageDraw.Draw(img)
    # butoane si ledul de pornire
    d.rectangle([22, 102, 62, 105], fill=(110, 106, 90))
    for k in range(3):
        d.ellipse([100 + k * 10, 101, 105 + k * 10, 106], fill=(80, 76, 66))
    d.ellipse([136, 101, 141, 106], fill=(70, 200, 90))
    return img


def closeup_newspaper():
    img = Image.new("RGB", (160, 120))
    dgrad(img, (0, 0, 160, 120), (60, 44, 30), (36, 26, 18), steps=4)
    d = ImageDraw.Draw(img)
    d.polygon([(14, 10), (146, 14), (142, 112), (10, 108)], fill=(214, 204, 176))
    grain(img, (10, 10, 146, 112), 10, seed=5)
    d = ImageDraw.Draw(img)
    text(d, 22, 18, "GAZETA DE NORD", (40, 34, 30))
    d.line([(20, 25), (138, 26)], fill=(40, 34, 30))
    text(d, 22, 30, "ACCIDENT LA KM 13", (150, 20, 20), sc=1)
    d.rectangle([22, 40, 66, 76], fill=(110, 100, 86))
    d.rectangle([30, 48, 58, 70], fill=(232, 196, 40))
    d.rectangle([38, 52, 50, 64], fill=(220, 210, 200))
    rnd = random.Random(9)
    for y in range(40, 100, 5):
        x0 = 72 if y < 78 else 22
        d.line([(x0, y), (x0 + rnd.randint(40, 62 if x0 == 72 else 110), y)], fill=(90, 80, 70))
    text(d, 22, 92, "14 OCT 1998", (40, 34, 30))
    return img


def closeup_contract():
    img = Image.new("RGB", (160, 120))
    dgrad(img, (0, 0, 160, 120), (50, 34, 24), (28, 18, 12), steps=4)
    d = ImageDraw.Draw(img)
    d.polygon([(30, 6), (132, 8), (130, 116), (28, 114)], fill=(232, 224, 200))
    grain(img, (28, 6, 132, 116), 8, seed=6)
    d = ImageDraw.Draw(img)
    text(d, 52, 12, "CONTRACT", (40, 30, 26))
    rnd = random.Random(2)
    for y in range(24, 88, 5):
        d.line([(36, y), (36 + rnd.randint(60, 88), y)], fill=(110, 96, 84))
    text(d, 36, 92, "SEMNATURA", (40, 30, 26))
    d.line([(36, 104), (120, 104)], fill=(60, 50, 40))
    pts = [(60, 102), (66, 96), (70, 104), (76, 95), (82, 103), (90, 98), (98, 101)]
    d.line(pts, fill=(170, 10, 10), width=1)
    pine(d, 118, 30, 14, (60, 90, 60))
    return img
