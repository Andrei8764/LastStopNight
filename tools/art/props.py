"""Obiectele care apar doar in unele blocuri: masina, conul farului, biletul, aparatul foto, telefonul."""
import random

from PIL import Image, ImageDraw

from .pixel import px, rect, sh


CAR_W, CAR_H = 80, 30


def car(driver=False, lights=False):
    """Masina veche a omului cu palarie, vazuta din lateral, cu botul spre stanga.
    Farul e spart, iar in bara indoita sunt prinse bucati de plastic galben."""
    im = Image.new("RGBA", (CAR_W, CAR_H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    body, roof = (74, 36, 40), (88, 44, 48)
    glass = (22, 28, 38)
    # caroseria de jos
    d.polygon([(2, 15), (16, 13), (66, 13), (78, 15), (78, 24), (2, 24)], fill=body)
    d.line([(3, 15), (16, 13), (66, 13), (77, 15)], fill=sh(body, 1.6))
    d.line([(2, 20), (78, 20)], fill=sh(body, 0.75))                  # linia laterala
    d.line([(2, 24), (78, 24)], fill=sh(body, 0.55))
    # cabina
    d.polygon([(18, 13), (27, 4), (55, 4), (64, 13)], fill=roof)
    d.line([(27, 4), (55, 4)], fill=sh(roof, 1.5))
    d.polygon([(21, 12), (28, 6), (38, 6), (38, 12)], fill=glass)     # parbriz / geam fata
    d.polygon([(41, 12), (41, 6), (54, 6), (60, 12)], fill=glass)     # geam spate
    d.line([(30, 7), (26, 11)], fill=(70, 80, 96))                    # luciu pe geam
    d.line([(46, 7), (44, 9)], fill=(70, 80, 96))
    d.line([(39, 5), (39, 23)], fill=sh(body, 0.6))                   # usa
    d.line([(20, 13), (20, 23)], fill=sh(body, 0.7))
    rect(d, 33, 15, 35, 15, (180, 180, 186))                          # manere
    rect(d, 43, 15, 45, 15, (180, 180, 186))
    rnd = random.Random(13)
    for _ in range(7):                                                # rugina
        px(d, rnd.randint(4, 74), rnd.randint(17, 23), (120, 70, 40))
    if driver:
        rect(d, 30, 8, 36, 12, (14, 12, 12))                          # silueta de la volan
        rect(d, 28, 8, 37, 8, (10, 8, 8))                             # borul palariei
        rect(d, 31, 6, 35, 7, (12, 10, 10))
        px(d, 32, 10, (240, 220, 150))
        px(d, 34, 10, (240, 220, 150))
    # rotile, cu aripile decupate
    for cx in (15, 63):
        d.ellipse([cx - 9, 14, cx + 9, 31], fill=(30, 18, 20))   # aripa
        d.ellipse([cx - 7, 16, cx + 7, 29], fill=(10, 10, 12))
        d.ellipse([cx - 5, 18, cx + 5, 27], fill=(24, 24, 26))
        d.ellipse([cx - 3, 20, cx + 3, 25], fill=(120, 120, 126))
        px(d, cx - 1, 21, (200, 200, 206))
    # bara din fata, indoita, cu bucati de pelerina galbena
    d.line([(0, 19), (2, 21), (1, 23), (4, 23)], fill=(170, 170, 176), width=1)
    px(d, 1, 20, (232, 196, 40))
    px(d, 2, 22, (232, 196, 40))
    px(d, 0, 21, (200, 160, 30))
    rect(d, 76, 19, 79, 22, (160, 160, 166))                          # bara din spate
    # farul (spart, cu o crapatura) si stopul
    rect(d, 2, 15, 4, 17, (250, 236, 180) if lights else (120, 120, 110))
    px(d, 3, 16, (60, 60, 54))
    px(d, 4, 15, (60, 60, 54))
    rect(d, 76, 15, 78, 17, (230, 40, 40) if lights else (120, 20, 20))
    return im


def car_shadow_rows(im):
    """Umbra pe asfalt sub masina."""
    d = ImageDraw.Draw(im)
    for x in range(4, 76):
        if im.getpixel((x, 29))[3] == 0:
            px(d, x, 29, (0, 0, 0, 90))
    return im


def beams(w=130, h=50, apex=(129, 30)):
    """Conul farului: lumina galbuie, transparenta, care se stinge cu distanta."""
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px_ = im.load()
    ax, ay = apex
    for x in range(w):
        t = (ax - x) / ax
        if t < 0:
            continue
        top, bot = ay - ay * t, ay + (h - ay) * t
        mid, half = (top + bot) / 2, max(0.5, (bot - top) / 2)
        for y in range(h):
            if top <= y <= bot:
                v = max(0.0, 1 - abs(y - mid) / half) ** 0.6
                a = int(165 * (1 - t) ** 0.5 * v)
                if a > 2:
                    # benzi usoare de lumina (pixel art, nu gradient perfect)
                    if (x // 2 + y) % 7 == 0:
                        a = int(a * 0.8)
                    px_[x, y] = (255, 242, 196, a)
    return im


def note():
    """Biletul de pe tejghea, prins sub o cheie ruginita."""
    im = Image.new("RGBA", (11, 6), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(0, 2), (9, 1), (10, 5), (1, 5)], fill=(234, 228, 206))
    d.line([(2, 3), (7, 3)], fill=(120, 110, 100))
    d.line([(4, 1), (8, 4)], fill=(150, 104, 54))
    px(d, 3, 1, (170, 120, 64))
    px(d, 4, 0, (170, 120, 64))
    return im


def photo_camera():
    """Aparatul foto cu blit al proprietarului."""
    im = Image.new("RGBA", (9, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rect(d, 0, 3, 8, 7, (26, 24, 24))
    rect(d, 1, 3, 7, 3, (60, 58, 56))
    d.ellipse([2, 4, 5, 7], fill=(80, 90, 110))
    px(d, 3, 5, (180, 200, 230))
    rect(d, 5, 0, 8, 2, (210, 210, 200))                              # blitul
    px(d, 6, 1, (255, 255, 240))
    return im


def wall_phone():
    """Telefonul de pe peretele magazinului (pentru 112)."""
    im = Image.new("RGBA", (12, 26), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rect(d, 3, 0, 11, 15, (196, 186, 160))
    d.line([(11, 1), (11, 15)], fill=(226, 218, 196))
    d.line([(3, 15), (11, 15)], fill=(150, 140, 120))
    rect(d, 0, 1, 2, 13, (176, 166, 140))                             # receptorul
    rect(d, 0, 0, 3, 2, (176, 166, 140))
    rect(d, 0, 12, 3, 14, (176, 166, 140))
    for r in range(3):
        for c in range(3):
            px(d, 5 + c * 2, 4 + r * 3, (70, 64, 56))
    for k in range(10):                                                # cablul spiralat
        px(d, 1 + (k % 2), 15 + k, (60, 56, 50))
    return im
