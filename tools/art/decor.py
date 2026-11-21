"""Decorul animat al locatiilor (spritesheet-uri orizontale)."""
import math
import random

from PIL import Image, ImageDraw

from .pixel import outline, sheet, text


def decor_fan():
    """Ventilator de tavan vazut din lateral: palele se rotesc (4 cadre, 40x12)."""
    frames = []
    for i in range(4):
        im = Image.new("RGBA", (40, 12), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.line([(20, 0), (20, 5)], fill=(30, 26, 24))
        d.rectangle([17, 5, 23, 8], fill=(80, 60, 40))
        left, right = [(18, 6), (12, 9), (16, 7), (10, 4)][i]
        d.line([(17, 7), (17 - left, 7)], fill=(110, 80, 50), width=2)
        d.line([(23, 7), (23 + right, 7)], fill=(110, 80, 50), width=2)
        d.rectangle([18, 9, 22, 11], fill=(230, 210, 150))
        frames.append(im)
    return sheet(frames)


def decor_open_sign():
    frames = []
    for lit in (True, True, True, False):
        im = Image.new("RGBA", (22, 9), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.rectangle([0, 0, 21, 8], fill=(20, 24, 30, 200))
        text(d, 2, 2, "OPEN", (110, 255, 150) if lit else (40, 70, 50))
        frames.append(im)
    return sheet(frames)


def decor_steam():
    frames = []
    rnd = random.Random(3)
    for i in range(4):
        im = Image.new("RGBA", (10, 16), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        for k in range(5):
            y = 14 - ((k * 3 + i * 2) % 15)
            x = 4 + int(2 * math.sin((y + i) * 0.8))
            a = int(140 * y / 15)
            d.point((x, y), fill=(230, 230, 230, a))
            if rnd.random() < 0.5:
                d.point((x + 1, y), fill=(230, 230, 230, a // 2))
        frames.append(im)
    return sheet(frames)


def decor_cat():
    """Pisica neagra, asezata: coada se misca, clipeste in ultimul cadru (4 cadre, 16x14)."""
    frames = []
    for i in range(4):
        im = Image.new("RGBA", (16, 14), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        c = (16, 14, 18)
        d.ellipse([3, 5, 12, 13], fill=c)          # corp
        d.ellipse([7, 1, 13, 7], fill=c)           # cap
        d.polygon([(7, 3), (8, 0), (9, 2)], fill=c)  # urechi
        d.polygon([(11, 2), (12, 0), (13, 3)], fill=c)
        tail = [[(3, 11), (1, 9), (1, 6)], [(3, 11), (0, 10), (0, 7)], [(3, 11), (1, 10), (2, 7)], [(3, 11), (1, 9), (1, 6)]][i]
        d.line(tail, fill=c, width=1)
        eye = (240, 210, 60) if i != 3 else c
        d.point((9, 4), fill=eye)
        d.point((11, 4), fill=eye)
        frames.append(outline(im, (40, 34, 30, 255)))
    return sheet(frames)


def decor_led():
    on = Image.new("RGBA", (3, 3), (0, 0, 0, 0))
    ImageDraw.Draw(on).rectangle([0, 0, 2, 2], fill=(255, 40, 40))
    off = Image.new("RGBA", (3, 3), (0, 0, 0, 0))
    ImageDraw.Draw(off).rectangle([0, 0, 2, 2], fill=(90, 10, 10))
    return sheet([on, on, off, off])


def decor_moths():
    frames = []
    pts = [[(3, 4), (11, 2), (7, 11)], [(5, 2), (12, 6), (4, 10)], [(2, 7), (9, 3), (10, 11)]]
    for p in pts:
        im = Image.new("RGBA", (16, 14), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        for (x, y) in p:
            d.point((x, y), fill=(230, 220, 190))
            d.point((x + 1, y), fill=(180, 170, 150))
        frames.append(im)
    return sheet(frames)


def decor_tube():
    """Tub fluorescent care palpaie (copertina benzinariei)."""
    frames = []
    for lit in (True, True, True, True, False, True, False, True):
        im = Image.new("RGBA", (30, 3), (0, 0, 0, 0))
        ImageDraw.Draw(im).rectangle([0, 0, 29, 2], fill=(235, 250, 255) if lit else (80, 90, 96))
        frames.append(im)
    return sheet(frames)


def decor_radio():
    frames = []
    for i in range(3):
        im = Image.new("RGBA", (14, 9), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.rectangle([0, 1, 13, 8], fill=(90, 60, 40))
        d.rectangle([2, 3, 7, 6], fill=(40, 30, 24))
        d.point((10, 4), fill=(200, 180, 120))
        d.line([(12, 1), (13, 0)], fill=(150, 150, 150))
        d.point((3 + i * 2, 4), fill=(255, 210, 120))  # acul de frecventa se plimba
        frames.append(im)
    return sheet(frames)
