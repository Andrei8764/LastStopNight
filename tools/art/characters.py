"""Personajele (24x48, 6 cadre: 2 de stat, 4 de mers): cap din profil, brate si genunchi animati,
umbre pe haine si contur colorat (selout)."""
import random

from PIL import Image, ImageDraw

from .pixel import px, rect, selout, sh


FW, FH = 24, 48


FRAMES = ["idle0", "idle1", "walk0", "walk1", "walk2", "walk3"]


STEP = {"idle0": (0, 0), "idle1": (0, 0), "walk0": (-3, 3), "walk1": (-1, 1), "walk2": (3, -3), "walk3": (1, -1)}


BASE = dict(skin=(222, 178, 146), hair=(46, 32, 24), shirt=(56, 62, 76), pants=(36, 36, 48), shoes=(22, 18, 18),
            eye=(24, 16, 14), lips=None)


KINDS = {
    "alex": dict(),
    "double": dict(skin=(206, 194, 186)),
    "faceless": dict(skin=(214, 212, 204), hair=None, shirt=(22, 22, 28), pants=(20, 20, 26), shoes=(10, 10, 12)),
    "red_woman": dict(skin=(236, 210, 198), hair=(20, 12, 14), shirt=(184, 22, 36), pants=None, shoes=(150, 12, 24),
                      lips=(170, 14, 28)),
    "patron": dict(skin=(204, 150, 116), hair=(86, 56, 32), shirt=(150, 44, 38), pants=(56, 72, 110),
                   shoes=(64, 42, 26)),
    "mara": dict(skin=(230, 188, 160), hair=(128, 62, 34), shirt=(44, 60, 104), pants=(38, 38, 52)),
    "dan": dict(skin=(216, 168, 134), hair=(100, 66, 32), shirt=(226, 218, 200), pants=(30, 30, 40)),
    "owner": dict(skin=(192, 150, 122), hair=(110, 110, 106), shirt=(226, 222, 210), pants=(54, 48, 44)),
    "hitchhiker": dict(skin=(34, 30, 28), hair=None, shirt=(58, 50, 42), pants=(32, 30, 28), shoes=(22, 18, 14)),
    "waiter": dict(skin=(214, 204, 196), hair=(26, 22, 22), shirt=(232, 230, 222), pants=(24, 24, 30)),
    "dancer_m": dict(skin=(214, 170, 138), hair=(20, 18, 20), shirt=(30, 30, 34), pants=(60, 80, 120),
                     shoes=(220, 220, 220)),
    "dancer_f": dict(skin=(236, 200, 176), hair=(232, 200, 120), shirt=(40, 150, 160), pants=None,
                     shoes=(236, 236, 236), lips=(190, 50, 70)),
    # insotitorii care pot fi alesi din meniu
    "ioana": dict(skin=(232, 196, 170), hair=(74, 42, 30), shirt=(108, 176, 156), pants=None, shoes=(44, 32, 30),
                  lips=(178, 62, 72)),
    "vlad": dict(skin=(210, 166, 136), hair=(46, 70, 50), shirt=(62, 86, 132), pants=(62, 86, 132), shoes=(66, 46, 30)),
    "lucia": dict(skin=(226, 182, 152), hair=(38, 24, 22), shirt=(222, 118, 150), pants=None, shoes=(240, 240, 236),
                  lips=(196, 58, 92)),
}


DRESSES = ("red_woman", "dancer_f", "ioana", "lucia")


def palette(kind):
    p = dict(BASE)
    p.update(KINDS[kind])
    return p


def leg(d, hip_x, step, top, pants, shoe, dark=1.0):
    """Picior cu genunchi: coapsa si gamba de cate 3 px, pantoful cu varful spre dreapta."""
    col = sh(pants, dark)
    knee = (hip_x + int(step * 0.55) + (1 if step > 0 else 0), 37)
    foot = (hip_x + step, 43)
    for k in range(3):
        d.line([(hip_x + k, top), (knee[0] + k, knee[1])], fill=col)
        d.line([(knee[0] + k, knee[1]), (foot[0] + k, foot[1])], fill=col)
    px(d, knee[0] + 2, knee[1], sh(col, 1.15))  # lumina pe genunchi
    sc = sh(shoe, dark)
    rect(d, foot[0], 44, foot[0] + 3, 45, sc)
    px(d, foot[0] + 4, 45, sc)
    rect(d, foot[0], 44, foot[0] + 2, 44, sh(sc, 1.3))


def arm(d, sx, sy, swing, sleeve, skin, length=11, width=2, bare=False):
    """Brat din umar; mana (2x2) se leagana invers fata de piciorul de pe aceeasi parte."""
    hx, hy = sx + swing, sy + length
    col = skin if bare else sleeve
    for k in range(width):
        d.line([(sx + k, sy), (hx + k, hy - 2)], fill=col)
    px(d, sx + width - 1, sy, sh(col, 1.2))
    rect(d, hx, hy - 1, hx + 1, hy, skin)
    px(d, hx + 1, hy, sh(skin, 0.8))


def torso(d, y, c, top=14, bottom=29, x0=7, x1=16):
    rect(d, x0, top + y, x1, bottom + y, c)
    px(d, x0, top + y, (0, 0, 0, 0))
    px(d, x1, top + y, (0, 0, 0, 0))
    d.line([(x1, top + 1 + y), (x1, bottom + y)], fill=sh(c, 1.22))   # lumina din fata
    d.line([(x0, top + 1 + y), (x0, bottom + y)], fill=sh(c, 0.72))   # umbra din spate
    d.line([(x0 + 1, top + 1 + y), (x0 + 1, bottom + y)], fill=sh(c, 0.86))
    d.line([(x0 + 1, bottom + y), (x1 - 1, bottom + y)], fill=sh(c, 0.7))


def head(d, y0, p, kind, frame):
    s = p["skin"]
    rect(d, 8, y0, 15, y0 + 9, s)
    for (x, yy) in ((8, y0), (15, y0), (8, y0 + 9), (15, y0 + 9)):
        px(d, x, yy, (0, 0, 0, 0))
    d.line([(8, y0 + 1), (8, y0 + 8)], fill=sh(s, 0.82))          # ceafa in umbra
    d.line([(9, y0 + 9), (14, y0 + 9)], fill=sh(s, 0.84))         # barbia
    px(d, 16, y0 + 5, sh(s, 0.95))                                 # nasul (profil 3/4)
    px(d, 16, y0 + 6, sh(s, 0.8))
    px(d, 10, y0 + 5, sh(s, 0.78))                                 # urechea
    px(d, 10, y0 + 6, sh(s, 0.7))
    if kind in ("faceless", "hitchhiker", "owner"):
        return
    eye = p["eye"]
    if kind == "double" and frame == "idle1":
        eye = (250, 250, 255)
    if kind == "waiter":
        # ochi „foarte calmi”: pleoape lasate
        d.line([(12, y0 + 5), (13, y0 + 5)], fill=sh(s, 0.55))
        d.line([(14, y0 + 5), (15, y0 + 5)], fill=sh(s, 0.55))
    else:
        px(d, 12, y0 + 5, eye)
        px(d, 14, y0 + 5, eye)
        px(d, 12, y0 + 4, sh(p["hair"] or s, 0.9) if p["hair"] else sh(s, 0.75))
        px(d, 14, y0 + 4, sh(p["hair"] or s, 0.9) if p["hair"] else sh(s, 0.75))
    lips = p.get("lips") or sh(s, 0.72)
    d.line([(13, y0 + 7), (14, y0 + 7)], fill=lips)
    if kind == "double":
        d.line([(12, y0 + 7), (15, y0 + 7)], fill=(90, 36, 36))


def hair(d, y0, p, kind):
    h = p["hair"]
    if h is None:
        return
    hl, hd = sh(h, 1.45), sh(h, 0.7)
    if kind in ("alex", "double", "waiter"):
        rect(d, 8, y0 - 1, 15, y0 + 1, h)
        rect(d, 7, y0, 9, y0 + 5, h)
        px(d, 15, y0 + 2, h)
        px(d, 8, y0 - 1, (0, 0, 0, 0))
        d.line([(10, y0 - 1), (13, y0 - 1)], fill=hl)
        px(d, 7, y0 + 5, hd)
        if kind == "waiter":
            d.line([(11, y0), (11, y0 + 1)], fill=hd)  # carare pe o parte, cu gel
            px(d, 14, y0 - 1, (240, 240, 240))
    elif kind == "mara":
        rect(d, 8, y0 - 1, 15, y0 + 1, h)
        rect(d, 7, y0, 9, y0 + 6, h)
        px(d, 15, y0 + 2, h)
        rect(d, 5, y0 + 3, 7, y0 + 12, h)          # coada
        px(d, 7, y0 + 3, (220, 60, 80))            # elasticul
        d.line([(10, y0 - 1), (13, y0 - 1)], fill=hl)
        d.line([(5, y0 + 5), (5, y0 + 11)], fill=hd)
    elif kind == "red_woman":
        rect(d, 8, y0 - 1, 15, y0 + 1, h)
        rect(d, 6, y0, 9, y0 + 17, h)
        rect(d, 15, y0 + 1, 16, y0 + 8, h)
        d.line([(10, y0 - 1), (12, y0 - 1)], fill=hl)
        d.line([(7, y0 + 4), (7, y0 + 16)], fill=hl)
    elif kind == "dan":
        rect(d, 8, y0 - 1, 15, y0 + 1, h)
        rect(d, 6, y0, 9, y0 + 9, h)               # par lung la spate, anii '80
        px(d, 15, y0 + 2, h)
        d.line([(9, y0 - 1), (13, y0 - 1)], fill=hl)
        d.line([(6, y0 + 4), (6, y0 + 9)], fill=hd)
    elif kind == "patron":
        rect(d, 7, y0 - 2, 15, y0 + 1, (176, 32, 32))   # sapca
        rect(d, 14, y0 + 1, 19, y0 + 2, (132, 22, 22))
        d.line([(9, y0 - 2), (12, y0 - 2)], fill=(220, 70, 70))
        rect(d, 7, y0 + 2, 9, y0 + 4, h)
        rect(d, 11, y0 + 6, 16, y0 + 9, h)              # barba
        px(d, 16, y0 + 9, (0, 0, 0, 0))
        d.line([(13, y0 + 7), (14, y0 + 7)], fill=sh(p["skin"], 0.6))
        px(d, 12, y0 + 8, hl)
    elif kind == "owner":
        rect(d, 7, y0 - 2, 15, y0 + 3, (40, 44, 40))    # sapca trasa pe ochi
        rect(d, 13, y0 + 3, 19, y0 + 4, (28, 30, 28))
        d.line([(9, y0 - 2), (12, y0 - 2)], fill=(70, 76, 70))
        rect(d, 9, y0 + 4, 15, y0 + 5, sh(p["skin"], 0.45))  # umbra cozorocului peste ochi
        rect(d, 7, y0 + 3, 8, y0 + 6, h)
        d.line([(12, y0 + 7), (16, y0 + 7)], fill=(150, 150, 146))  # mustata
        px(d, 13, y0 + 8, sh(p["skin"], 0.7))
    elif kind == "dancer_m":
        rect(d, 8, y0 - 2, 16, y0 + 1, h)              # tolomac
        rect(d, 7, y0, 9, y0 + 5, h)
        d.line([(11, y0 - 2), (15, y0 - 2)], fill=sh(h, 3.0))
    elif kind == "ioana":
        rect(d, 8, y0 - 1, 15, y0 + 1, h)
        rect(d, 7, y0, 9, y0 + 4, h)
        px(d, 15, y0 + 2, h)
        rect(d, 6, y0 - 3, 9, y0, h)                   # coc
        d.line([(10, y0 - 1), (13, y0 - 1)], fill=hl)
        px(d, 7, y0 - 3, hl)
        px(d, 9, y0 - 2, (230, 230, 230))              # creionul infipt in coc
        px(d, 10, y0 - 3, (230, 200, 90))
    elif kind == "lucia":
        rect(d, 6, y0 - 2, 17, y0 + 2, h)              # carlionti, anii '80
        rect(d, 5, y0, 9, y0 + 10, h)
        rect(d, 15, y0 + 1, 17, y0 + 5, h)
        for (cx, cy) in ((7, y0 - 1), (11, y0 - 2), (15, y0 - 1), (6, y0 + 4), (6, y0 + 8), (16, y0 + 3)):
            px(d, cx, cy, hl)
        px(d, 6, y0 - 2, (0, 0, 0, 0))
        px(d, 17, y0 - 2, (0, 0, 0, 0))
        rect(d, 12, y0 - 3, 14, y0 - 2, (250, 250, 250))  # fundita
    elif kind == "vlad":
        rect(d, 7, y0 - 2, 16, y0 + 2, h)              # caciula
        d.line([(7, y0 + 2), (16, y0 + 2)], fill=sh(h, 1.4))
        px(d, 7, y0 - 2, (0, 0, 0, 0))
        px(d, 16, y0 - 2, (0, 0, 0, 0))
        for (bx, by) in ((12, y0 + 8), (14, y0 + 8), (13, y0 + 9), (15, y0 + 9), (11, y0 + 9)):
            px(d, bx, by, sh(p["skin"], 0.72))         # barba nerasa
    elif kind == "dancer_f":
        rect(d, 6, y0 - 3, 17, y0 + 2, h)              # par cu volum, anii '80
        rect(d, 5, y0, 9, y0 + 11, h)
        rect(d, 15, y0 + 1, 17, y0 + 6, h)
        px(d, 6, y0 - 3, (0, 0, 0, 0))
        px(d, 17, y0 - 3, (0, 0, 0, 0))
        d.line([(8, y0 - 3), (14, y0 - 3)], fill=hl)
        d.line([(5, y0 + 3), (5, y0 + 10)], fill=hd)
        px(d, 9, y0 - 2, (250, 90, 140))


def person(kind, frame):
    im = Image.new("RGBA", (FW, FH), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = palette(kind)
    bob = 1 if frame in ("idle1", "walk1", "walk3") else 0
    back, front = STEP[frame]
    y = bob
    dress = kind in DRESSES
    coat = kind == "hitchhiker"

    # bratul din spate (in umbra), apoi picioarele
    sleeve = p["shirt"]
    if kind in ("alex", "double"):
        sleeve = p["shirt"]
    if kind == "mara":
        sleeve = p["shirt"]
    if kind == "dancer_m":
        sleeve = (36, 34, 38)
    if kind == "vlad":
        sleeve = (128, 126, 132)          # camasa gri de sub salopeta
    bare = kind in ("red_woman", "dancer_f")
    arm(d, 7, 15 + y, -front // 2 - 1, sh(sleeve, 0.7), sh(p["skin"], 0.8), width=3 if kind == "owner" else 2, bare=bare)

    if dress:
        leg_skin = sh(p["skin"], 0.92)
        for hip, st, dk in ((9, back, 0.8), (13, front, 1.0)):
            x0 = hip + st // 2
            rect(d, x0, 38, x0 + 1, 43, sh(leg_skin, dk))
            rect(d, x0 - 1, 44, x0 + 2, 45, sh(p["shoes"], dk))
            px(d, x0 + 3, 45, sh(p["shoes"], dk))
    else:
        top = 30 + y
        leg(d, 8, back, top, p["pants"], p["shoes"], dark=0.78)
        leg(d, 12, front, top, p["pants"], p["shoes"])

    # trunchiul si hainele
    shirt = p["shirt"]
    if coat:
        d.polygon([(7, 13 + y), (16, 13 + y), (19, 40), (4, 40)], fill=shirt)
        d.line([(16, 14 + y), (19, 40)], fill=sh(shirt, 1.25))
        d.line([(7, 14 + y), (4, 40)], fill=sh(shirt, 0.7))
        d.line([(12, 15 + y), (12, 40)], fill=sh(shirt, 0.65))
        d.polygon([(12, 14 + y), (15, 14 + y), (12, 21 + y)], fill=sh(shirt, 1.35))  # rever
        d.line([(5, 39), (18, 39)], fill=sh(shirt, 0.6))
        rect(d, 13, 28 + y, 14, 29 + y, sh(shirt, 0.55))  # buzunar
    elif kind == "red_woman":
        torso(d, y, shirt, bottom=24)
        d.polygon([(6, 24 + y), (17, 24 + y), (20, 39), (3, 39)], fill=shirt)
        d.line([(17, 25 + y), (20, 39)], fill=sh(shirt, 1.25))
        d.line([(9, 26 + y), (7, 38)], fill=sh(shirt, 0.65))
        d.line([(13, 26 + y), (14, 38)], fill=sh(shirt, 0.72))
        d.line([(6, 24 + y), (17, 24 + y)], fill=sh(shirt, 0.6))
        d.line([(4, 39), (19, 39)], fill=sh(shirt, 0.6))
        rect(d, 10, 14 + y, 13, 15 + y, p["skin"])  # decolteu
    elif kind in ("ioana", "lucia"):
        torso(d, y, shirt, bottom=23)
        d.polygon([(6, 23 + y), (17, 23 + y), (19, 37), (4, 37)], fill=shirt)
        d.line([(17, 24 + y), (19, 37)], fill=sh(shirt, 1.25))
        d.line([(5, 37), (18, 37)], fill=sh(shirt, 0.6))
        d.polygon([(8, 22 + y), (15, 22 + y), (16, 35), (7, 35)], fill=(236, 232, 222))   # sortul alb
        d.line([(15, 23 + y), (16, 35)], fill=(250, 248, 240))
        d.line([(8, 22 + y), (15, 22 + y)], fill=(190, 186, 176))
        rect(d, 10, 14 + y, 13, 15 + y, (240, 240, 236))                                   # guler
        rect(d, 14, 17 + y, 15, 18 + y, (240, 220, 120))                                   # ecuson
    elif kind == "dancer_f":
        torso(d, y, shirt, bottom=23)
        d.polygon([(6, 23 + y), (17, 23 + y), (21, 37), (2, 37)], fill=shirt)
        d.line([(17, 24 + y), (21, 37)], fill=sh(shirt, 1.25))
        d.line([(3, 37), (20, 37)], fill=sh(shirt, 0.6))
        rnd = random.Random(5)
        for _ in range(9):
            px(d, rnd.randint(5, 18), rnd.randint(25, 35) + (y if rnd.random() < 0.5 else 0), (236, 236, 226))
        d.line([(7, 23 + y), (16, 23 + y)], fill=(236, 236, 226))  # cordon
    else:
        torso(d, y, shirt)

    if kind in ("alex", "double"):
        rect(d, 10, 14 + y, 13, 15 + y, (230, 230, 226))           # gulerul
        rect(d, 7, 22 + y, 16, 33 + y, (214, 210, 200))            # sortul
        d.line([(7, 22 + y), (16, 22 + y)], fill=(160, 156, 146))
        d.line([(16, 23 + y), (16, 33 + y)], fill=(236, 234, 226))
        d.line([(8, 23 + y), (8, 33 + y)], fill=(170, 166, 156))
        px(d, 12, 27 + y, (190, 186, 176))
        rect(d, 14, 17 + y, 15, 18 + y, (240, 236, 220))           # ecusonul
    elif kind == "faceless":
        d.polygon([(10, 14 + y), (13, 14 + y), (12, 21 + y), (11, 21 + y)], fill=(230, 230, 226))
        d.line([(11, 15 + y), (11, 22 + y)], fill=(130, 14, 22))
        d.line([(12, 15 + y), (12, 23 + y)], fill=(160, 20, 30))
        px(d, 15, 20 + y, (60, 60, 70))
    elif kind == "mara":
        rect(d, 8, 15 + y, 15, 28 + y, (176, 32, 40))              # vesta
        d.line([(15, 15 + y), (15, 28 + y)], fill=(210, 60, 64))
        d.line([(8, 15 + y), (8, 28 + y)], fill=(130, 20, 28))
        d.line([(11, 15 + y), (11, 28 + y)], fill=(140, 22, 30))
        rect(d, 13, 18 + y, 14, 19 + y, (246, 220, 110))           # ecusonul MARA
        rect(d, 10, 14 + y, 12, 14 + y, (230, 230, 230))
    elif kind == "dan":
        rect(d, 8, 15 + y, 15, 27 + y, (150, 26, 34))              # vesta rosie
        d.line([(15, 15 + y), (15, 27 + y)], fill=(190, 50, 56))
        rect(d, 10, 14 + y, 13, 15 + y, (20, 20, 24))              # papion
        px(d, 11, 15 + y, (60, 60, 66))
        rect(d, 14, 18 + y, 15, 19 + y, (240, 236, 220))
    elif kind == "waiter":
        rect(d, 8, 15 + y, 15, 28 + y, (22, 22, 26))               # vesta neagra
        d.line([(15, 15 + y), (15, 28 + y)], fill=(54, 54, 60))
        rect(d, 10, 14 + y, 13, 15 + y, (16, 16, 18))              # papion
        rect(d, 11, 16 + y, 12, 27 + y, (232, 230, 222))
        px(d, 11, 20 + y, (120, 120, 120))
        px(d, 11, 24 + y, (120, 120, 120))
    elif kind == "owner":
        rect(d, 7, 18 + y, 16, 36 + y, (210, 204, 190))            # sort lung
        d.line([(16, 19 + y), (16, 36 + y)], fill=(236, 232, 220))
        d.line([(7, 18 + y), (16, 18 + y)], fill=(170, 164, 150))
        for (sx_, sy_) in ((10, 24), (14, 29), (11, 33)):
            px(d, sx_, sy_ + y, (130, 40, 36))                      # pete
    elif kind == "patron":
        for yy in range(14, 30):
            for xx in range(7, 17):
                if (xx + yy // 2) % 3 == 0 and im.getpixel((xx, yy + y))[3]:
                    px(d, xx, yy + y, sh(shirt, 0.62))
        d.line([(12, 14 + y), (12, 29 + y)], fill=sh(shirt, 0.55))
    elif kind == "vlad":
        rect(d, 7, 14 + y, 16, 18 + y, (128, 126, 132))         # camasa, sus
        d.line([(9, 14 + y), (9, 21 + y)], fill=sh(shirt, 1.3))  # bretelele salopetei
        d.line([(14, 14 + y), (14, 21 + y)], fill=sh(shirt, 1.3))
        rect(d, 9, 21 + y, 14, 26 + y, sh(shirt, 1.1))           # pieptarul
        px(d, 9, 21 + y, (220, 200, 120))
        px(d, 14, 21 + y, (220, 200, 120))
        d.line([(15, 24 + y), (15, 28 + y)], fill=(170, 170, 176))  # cheia franceza din buzunar
    elif kind == "dancer_m":
        rect(d, 10, 14 + y, 13, 22 + y, (226, 226, 220))           # tricoul alb sub geaca
        d.line([(14, 15 + y), (14, 28 + y)], fill=(80, 80, 90))     # luciul gecii de piele
        px(d, 9, 18 + y, (90, 90, 100))

    # bratul din fata
    arm(d, 15, 15 + y, front // 2 + 1, sh(sleeve, 0.95), p["skin"], width=3 if kind == "owner" else 2, bare=bare)

    # gat, cap, par
    rect(d, 11, 12 + y, 12, 13 + y, sh(p["skin"], 0.78))
    y0 = 3 + y
    head(d, y0, p, kind, frame)
    hair(d, y0, p, kind)
    if kind == "hitchhiker":
        rect(d, 3, y0, 20, y0 + 1, (30, 26, 22))                    # borul palariei
        rect(d, 8, y0 - 4, 15, y0, (36, 30, 26))
        d.line([(8, y0 - 1), (15, y0 - 1)], fill=(70, 20, 20))      # panglica
        d.line([(9, y0 - 4), (13, y0 - 4)], fill=(60, 54, 48))
        if frame != "idle1":
            px(d, 12, y0 + 5, (226, 210, 150))
            px(d, 14, y0 + 5, (226, 210, 150))
    if kind == "faceless":
        for i, x in enumerate(range(11, 16)):
            px(d, x, y0 + 7, (236, 232, 220) if i % 2 == 0 else (70, 20, 24))  # prea multi dinti
        px(d, 12, y0 + 4, sh(p["skin"], 0.85))
        px(d, 14, y0 + 4, sh(p["skin"], 0.85))
    if kind == "red_woman":
        px(d, 12, y0 + 5, (8, 6, 6))
        px(d, 14, y0 + 5, (8, 6, 6))
    return selout(im)


def child(frame):
    """Fetita in pelerina galbena, cu gluga trasa si apa care picura de pe ea."""
    im = Image.new("RGBA", (FW, FH), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    bob = 1 if frame in ("idle1", "walk1", "walk3") else 0
    back, front = STEP[frame]
    y = 16 + bob
    yl, yd, yh = (234, 198, 44), (176, 132, 24), (255, 232, 120)
    for hip, st, dk in ((10, back, 0.75), (13, front, 1.0)):
        x0 = hip + st // 2
        rect(d, x0, 24 + y, x0 + 1, 43, sh((66, 66, 78), dk))
        rect(d, x0 - 1, 41, x0 + 2, 45, sh(yl, dk))               # cizmele galbene
        px(d, x0 + 3, 45, sh(yl, dk))
        px(d, x0, 41, sh(yh, dk))
    d.polygon([(8, 9 + y), (15, 9 + y), (18, 25 + y), (5, 25 + y)], fill=yl)
    d.line([(15, 10 + y), (18, 25 + y)], fill=yh)
    d.line([(8, 10 + y), (5, 24 + y)], fill=yd)
    d.line([(11, 12 + y), (11, 24 + y)], fill=yd)
    d.line([(6, 25 + y), (17, 25 + y)], fill=yd)
    rect(d, 15, 16 + y, 16, 17 + y, (232, 214, 200))              # mana
    d.ellipse([7, 0 + y, 16, 11 + y], fill=yl)                    # gluga
    d.arc([7, 0 + y, 16, 11 + y], 200, 300, fill=yh)
    rect(d, 10, 3 + y, 15, 9 + y, (238, 228, 222))                # fata palida
    px(d, 15, 3 + y, yl)
    d.line([(10, 3 + y), (10, 9 + y)], fill=(210, 200, 196))
    px(d, 12, 5 + y, (24, 24, 34))
    px(d, 14, 5 + y, (24, 24, 34))
    px(d, 13, 8 + y, (190, 170, 170))
    for k, (dx, dy) in enumerate(((7, 27), (12, 28), (16, 27))):
        if (k + bob) % 2 == 0:
            px(d, dx, dy + y, (150, 190, 230))                        # picaturi
    return selout(im)


def sheet_of(draw):
    out = Image.new("RGBA", (FW * len(FRAMES), FH), (0, 0, 0, 0))
    for i, f in enumerate(FRAMES):
        out.alpha_composite(draw(f), (i * FW, 0))
    return out


def sprite_sheets():
    kinds = list(KINDS)
    res = [("sprites/%s.png" % k, sheet_of(lambda f, k=k: person(k, f))) for k in kinds]
    res.append(("sprites/girl.png", sheet_of(child)))
    return res
