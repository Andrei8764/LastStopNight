"""Unelte comune de desen: gradiente cu dithering, lumina, vigneta, fontul pixel, copaci, podele, contur."""
import math
import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter


W, H = 320, 180


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def dgrad(img, box, c1, c2, steps=6, horizontal=False):
    """Gradient cu dithering ordonat (aspect retro)."""
    px = img.load()
    x0, y0, x1, y1 = box
    span = (x1 - x0) if horizontal else (y1 - y0)
    for y in range(max(0, y0), min(img.height, y1)):
        for x in range(max(0, x0), min(img.width, x1)):
            p = (x - x0) if horizontal else (y - y0)
            t = p / max(1, span - 1) * steps
            th = (BAYER[y % 4][x % 4] + 0.5) / 16
            s = math.floor(t) + (1 if (t - math.floor(t)) > th else 0)
            px[x, y] = lerp(c1, c2, min(steps, s) / steps)


def grain(img, box, amt, seed=0):
    rnd = random.Random(seed)
    px = img.load()
    x0, y0, x1, y1 = box
    for y in range(max(0, y0), min(img.height, y1)):
        for x in range(max(0, x0), min(img.width, x1)):
            n = rnd.randint(-amt, amt)
            p = px[x, y]
            px[x, y] = tuple(max(0, min(255, p[i] + n)) for i in range(3))


def add_light(img, layer, blur):
    l = layer.filter(ImageFilter.GaussianBlur(blur)) if blur else layer
    img.paste(ImageChops.add(img, l))


def darken(img, box, factor):
    px = img.load()
    x0, y0, x1, y1 = box
    for y in range(max(0, y0), min(img.height, y1)):
        for x in range(max(0, x0), min(img.width, x1)):
            p = px[x, y]
            px[x, y] = tuple(int(p[i] * factor) for i in range(3))


def vignette(img, strength=0.55):
    px = img.load()
    cx, cy = img.width / 2, img.height / 2
    for y in range(img.height):
        for x in range(img.width):
            d = math.hypot((x - cx) / cx, (y - cy) / cy) / 1.4142
            f = 1 - strength * max(0, d - 0.35) / 0.65
            p = px[x, y]
            px[x, y] = tuple(int(p[i] * f) for i in range(3))


def tint(img, color, amount):
    over = Image.new("RGB", img.size, color)
    return Image.blend(img, over, amount)


FONT = {
    "A": [".#.", "#.#", "###", "#.#", "#.#"], "B": ["##.", "#.#", "##.", "#.#", "##."],
    "C": [".##", "#..", "#..", "#..", ".##"], "D": ["##.", "#.#", "#.#", "#.#", "##."],
    "E": ["###", "#..", "##.", "#..", "###"], "F": ["###", "#..", "##.", "#..", "#.."],
    "G": [".##", "#..", "#.#", "#.#", ".##"], "H": ["#.#", "#.#", "###", "#.#", "#.#"],
    "I": ["###", ".#.", ".#.", ".#.", "###"], "J": ["..#", "..#", "..#", "#.#", ".#."],
    "K": ["#.#", "#.#", "##.", "#.#", "#.#"], "L": ["#..", "#..", "#..", "#..", "###"],
    "M": ["#...#", "##.##", "#.#.#", "#...#", "#...#"], "N": ["#..#", "##.#", "#.##", "#..#", "#..#"],
    "O": [".#.", "#.#", "#.#", "#.#", ".#."], "P": ["##.", "#.#", "##.", "#..", "#.."],
    "Q": [".#.", "#.#", "#.#", "##.", ".##"], "R": ["##.", "#.#", "##.", "#.#", "#.#"],
    "S": [".##", "#..", ".#.", "..#", "##."], "T": ["###", ".#.", ".#.", ".#.", ".#."],
    "U": ["#.#", "#.#", "#.#", "#.#", "###"], "V": ["#.#", "#.#", "#.#", "#.#", ".#."],
    "W": ["#...#", "#...#", "#.#.#", "##.##", "#...#"], "X": ["#.#", "#.#", ".#.", "#.#", "#.#"],
    "Y": ["#.#", "#.#", ".#.", ".#.", ".#."], "Z": ["###", "..#", ".#.", "#..", "###"],
    "0": ["###", "#.#", "#.#", "#.#", "###"], "1": [".#.", "##.", ".#.", ".#.", "###"],
    "2": ["##.", "..#", ".#.", "#..", "###"], "3": ["##.", "..#", ".#.", "..#", "##."],
    "4": ["#.#", "#.#", "###", "..#", "..#"], "5": ["###", "#..", "##.", "..#", "##."],
    "6": [".##", "#..", "###", "#.#", "###"], "7": ["###", "..#", ".#.", ".#.", ".#."],
    "8": ["###", "#.#", "###", "#.#", "###"], "9": ["###", "#.#", "###", "..#", "##."],
    ":": ["...", ".#.", "...", ".#.", "..."], ".": ["...", "...", "...", "...", ".#."],
    "-": ["...", "...", "###", "...", "..."], "?": ["##.", "..#", ".#.", "...", ".#."],
    "!": [".#.", ".#.", ".#.", "...", ".#."], " ": ["...", "...", "...", "...", "..."],
    "$": [".##", "##.", ".#.", ".##", "##."], "/": ["..#", "..#", ".#.", "#..", "#.."],
}


def text(d, x, y, s, col, sc=1):
    """Font pixel cu latime variabila (3 coloane, 4-5 pentru M, N, W)."""
    cx = x
    for ch in s.upper():
        g = FONT.get(ch, FONT["?"])
        for r, row in enumerate(g):
            for c, v in enumerate(row):
                if v == "#":
                    d.rectangle([cx + c * sc, y + r * sc, cx + c * sc + sc - 1, y + r * sc + sc - 1], fill=col)
        cx += (len(g[0]) + 1) * sc
    return cx


def text_width(s, sc=1):
    return sum((len(FONT.get(ch, FONT["?"])[0]) + 1) * sc for ch in s.upper()) - sc


def pine(d, x, base, h, col):
    layers = max(3, h // 9)
    w = h * 0.5
    for i in range(layers):
        top = base - h + i * (h / layers) * 0.8
        bw = w * (0.3 + 0.7 * (i + 1) / layers)
        bot = top + h / layers * 1.7
        d.polygon([(x, top), (x - bw / 2, bot), (x + bw / 2, bot)], fill=col)
    d.rectangle([x - 1, base - h * 0.15, x + 1, base], fill=col)


def forest(img, base, count, hmin, hmax, col, seed, x0=-10, x1=W + 10):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for _ in range(count):
        pine(d, rnd.randint(x0, x1), base + rnd.randint(-2, 3), rnd.randint(hmin, hmax), col)


def checker_floor(img, y0, y1, c1, c2, tile=12, seed=1):
    d = ImageDraw.Draw(img)
    y, row, hgt = y0, 0, 4
    while y < y1:
        for i in range(-1, W // tile + 2):
            x = i * tile + (row % 2) * 0
            col = c1 if (i + row) % 2 == 0 else c2
            d.rectangle([x, y, x + tile - 1, y + hgt - 1], fill=col)
        y += hgt
        row += 1
        hgt += 1
    # umbra spre margini / jos
    px = img.load()
    for yy in range(y0, y1):
        for xx in range(W):
            f = 0.55 + 0.45 * (1 - (yy - y0) / (y1 - y0)) * (1 - abs(xx - W / 2) / (W / 2) * 0.6)
            p = px[xx, yy]
            px[xx, yy] = tuple(int(p[i] * f) for i in range(3))


def lamp(d, light, x, y_cord, length, warm=(255, 190, 110), cone=True, floor=170):
    d.line([(x, y_cord), (x, y_cord + length)], fill=(20, 18, 18))
    y = y_cord + length
    d.polygon([(x - 2, y), (x + 2, y), (x + 5, y + 4), (x - 5, y + 4)], fill=(70, 50, 30))
    d.line([(x - 4, y + 4), (x + 4, y + 4)], fill=(255, 230, 170))
    ld = ImageDraw.Draw(light)
    ld.ellipse([x - 8, y - 2, x + 8, y + 10], fill=tuple(int(c * 0.7) for c in warm))
    if cone:
        ld.polygon([(x - 4, y + 5), (x + 4, y + 5), (x + 26, floor), (x - 26, floor)],
                   fill=tuple(int(c * 0.16) for c in warm))


def outline(img, color=(8, 6, 10, 255)):
    a = img.split()[3]
    dil = a.filter(ImageFilter.MaxFilter(3))
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", img.size, color), mask=dil)
    out.alpha_composite(img)
    return out


def sheet(frames):
    w, h = frames[0].size
    out = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        out.alpha_composite(f, (i * w, 0))
    return out


def sh(c, f):
    """Umbra/lumina: f < 1 intuneca, f > 1 lumineaza (cu un pic de cald in lumini)."""
    if f <= 1:
        return tuple(max(0, int(v * f)) for v in c[:3])
    return tuple(min(255, int(v * f + 10 * (f - 1))) for v in c[:3])


def px(d, x, y, c):
    d.point((x, y), fill=c)


def rect(d, x0, y0, x1, y1, c):
    d.rectangle([x0, y0, x1, y1], fill=c)


def selout(img, strength=0.38):
    """Contur colorat: fiecare pixel de margine ia culoarea vecinului, mult mai inchisa."""
    w, h = img.size
    src = img.load()
    out = img.copy()
    o = out.load()
    for y in range(h):
        for x in range(w):
            if src[x, y][3] > 0:
                continue
            best = None
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3] > 0:
                    c = src[nx, ny]
                    if best is None or sum(c[:3]) < sum(best[:3]):
                        best = c
            if best is not None:
                o[x, y] = (int(best[0] * strength), int(best[1] * strength), int(best[2] * strength) + 4, 255)
    return out
