"""Black Pine Diner: parcarea (noaptea si in zori), sala, bucataria si biroul."""
import random

from PIL import Image, ImageDraw

from .pixel import H, W, add_light, checker_floor, darken, dgrad, forest, grain, lamp, text, tint, vignette


def diner_exterior():
    img = Image.new("RGB", (W, H))
    dgrad(img, (0, 0, W, 118), (6, 8, 20), (30, 28, 44), steps=8)
    rnd = random.Random(3)
    d = ImageDraw.Draw(img)
    for _ in range(45):
        x, y = rnd.randint(0, W - 1), rnd.randint(0, 70)
        v = rnd.randint(70, 160)
        d.point((x, y), fill=(v, v, v + 10))
    light = Image.new("RGB", (W, H))
    ld = ImageDraw.Draw(light)
    d.ellipse([244, 18, 262, 36], fill=(224, 222, 200))
    d.ellipse([248, 21, 252, 25], fill=(196, 194, 176))
    d.ellipse([254, 27, 257, 30], fill=(200, 198, 180))
    ld.ellipse([232, 6, 274, 48], fill=(40, 40, 46))
    forest(img, 116, 60, 30, 62, (16, 20, 28), 11)
    forest(img, 120, 26, 50, 90, (10, 13, 18), 12)
    # sol / parcare
    dgrad(img, (0, 116, W, H), (26, 26, 32), (10, 10, 14), steps=6)
    grain(img, (0, 116, W, H), 4, seed=5)
    d = ImageDraw.Draw(img)
    for x in range(0, W, 26):
        d.line([(x + 8, 160), (x + 2, 179)], fill=(60, 60, 54))
    # cladirea
    d.rectangle([108, 80, 292, 86], fill=(36, 36, 44))
    d.rectangle([112, 86, 288, 130], fill=(50, 38, 36))
    d.rectangle([112, 116, 288, 130], fill=(78, 80, 88))
    d.line([(112, 117), (288, 117)], fill=(150, 150, 160))
    d.line([(108, 87), (292, 87)], fill=(210, 30, 42))
    ld.line([(108, 87), (292, 87)], fill=(150, 10, 20), width=3)
    for i, x in enumerate(range(116, 284, 17)):
        if 190 <= x <= 210:
            continue
        d.rectangle([x, 92, x + 13, 112], fill=(236, 196, 120))
        d.line([(x, 102), (x + 13, 102)], fill=(170, 120, 70))
        d.rectangle([x + 3, 104, x + 9, 112], fill=(120, 30, 30))  # separeu
        ld.rectangle([x - 2, 90, x + 15, 114], fill=(90, 66, 30))
        # reflexii pe asfaltul ud
        for k in range(6):
            yy = 134 + k * 6
            ld.line([(x + 3, yy), (x + 10, yy)], fill=(50 - k * 7, 36 - k * 5, 16 - k * 2))
    d.rectangle([196, 90, 210, 130], fill=(210, 170, 100))
    d.line([(203, 90), (203, 130)], fill=(90, 70, 50))
    d.rectangle([196, 90, 210, 130], outline=(40, 30, 30))
    ld.rectangle([192, 88, 214, 132], fill=(80, 60, 26))
    # firma DINER
    d.rectangle([166, 60, 238, 78], fill=(22, 16, 18))
    d.rectangle([166, 60, 238, 78], outline=(60, 50, 50))
    d.line([(176, 78), (176, 80)], fill=(60, 50, 50))
    d.line([(228, 78), (228, 80)], fill=(60, 50, 50))
    text(d, 172, 64, "DINER", (255, 70, 80), sc=2)
    neon = Image.new("RGB", (W, H))
    text(ImageDraw.Draw(neon), 172, 64, "DINER", (230, 20, 40), sc=2)
    add_light(img, neon, 3)
    add_light(img, neon, 6)
    # panou BLACK PINE
    d.rectangle([22, 86, 24, 128], fill=(30, 22, 16))
    d.rectangle([62, 86, 64, 128], fill=(30, 22, 16))
    d.rectangle([14, 66, 72, 98], fill=(64, 46, 32))
    d.rectangle([14, 66, 72, 98], outline=(34, 24, 18))
    text(d, 23, 70, "BLACK", (200, 178, 140))
    text(d, 27, 78, "PINE", (200, 178, 140))
    text(d, 23, 88, "DINER", (170, 150, 120))
    # stalp de iluminat
    d.rectangle([90, 50, 91, 132], fill=(50, 50, 56))
    d.line([(90, 50), (98, 50)], fill=(50, 50, 56))
    d.rectangle([96, 50, 101, 53], fill=(90, 90, 90))
    d.line([(97, 54), (100, 54)], fill=(255, 230, 170))
    ld.polygon([(97, 55), (101, 55), (126, 150), (72, 150)], fill=(46, 40, 26))
    ld.ellipse([88, 44, 110, 62], fill=(130, 110, 70))
    # masina lui Alex
    d.polygon([(270, 140), (276, 132), (300, 132), (308, 140), (316, 141), (316, 150), (262, 150), (262, 141)],
              fill=(26, 26, 34))
    d.polygon([(278, 134), (288, 134), (288, 139), (274, 139)], fill=(54, 60, 76))
    d.polygon([(291, 134), (299, 134), (304, 139), (291, 139)], fill=(54, 60, 76))
    d.ellipse([266, 146, 276, 156], fill=(8, 8, 10))
    d.ellipse([300, 146, 310, 156], fill=(8, 8, 10))
    d.rectangle([314, 142, 316, 145], fill=(255, 40, 40))
    ld.ellipse([306, 136, 324, 152], fill=(120, 10, 10))
    ld.line([(315, 152), (315, 172)], fill=(60, 6, 6), width=2)
    add_light(img, light, 3)
    vignette(img, 0.5)
    return img


def window_view(img, box, seed):
    x0, y0, x1, y1 = box
    sub = Image.new("RGB", (x1 - x0, y1 - y0))
    dgrad(sub, (0, 0, sub.width, sub.height), (10, 14, 26), (22, 26, 38), steps=5)
    forest(sub, sub.height - 4, 18, 14, 34, (8, 10, 16), seed, -5, sub.width + 5)
    img.paste(sub, (x0, y0))


def diner_interior(lights_on=True):
    img = Image.new("RGB", (W, H))
    dgrad(img, (0, 0, W, 104), (26, 14, 16), (52, 28, 26), steps=6)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 9], fill=(16, 12, 12))
    for x in range(0, W, 6):
        d.line([(x, 10), (x, 102)], fill=(44, 24, 22))
    # lambriu rosu
    d.rectangle([0, 102, W, 150], fill=(96, 20, 26))
    d.line([(0, 102), (W, 102)], fill=(170, 170, 180))
    d.line([(0, 104), (W, 104)], fill=(70, 70, 78))
    for x in range(0, W, 10):
        d.line([(x, 106), (x, 149)], fill=(80, 16, 22))
    # ferestre
    window_view(img, (40, 28, 182, 90), 21)
    for x in (40, 87, 134, 182):
        d.rectangle([x - 1, 26, x + 1, 92], fill=(110, 110, 120))
    d.rectangle([38, 26, 184, 27], fill=(130, 130, 140))
    d.rectangle([38, 90, 184, 92], fill=(130, 130, 140))
    # usa de la intrare
    d.rectangle([6, 34, 32, 150], fill=(60, 60, 68))
    d.rectangle([9, 38, 29, 100], fill=(18, 22, 34))
    d.rectangle([9, 104, 29, 146], fill=(70, 70, 80))
    d.rectangle([27, 106, 28, 116], fill=(190, 190, 190))
    d.ellipse([17, 28, 21, 32], fill=(200, 170, 60))  # clopotel
    # afis lipit stramb langa usa
    d.polygon([(34, 40), (39, 39), (40, 50), (35, 51)], fill=(214, 204, 170))
    d.line([(35, 43), (38, 43)], fill=(140, 40, 40))
    d.line([(36, 46), (38, 46)], fill=(90, 80, 70))
    # separeuri
    for bx in (42, 90):
        d.rectangle([bx, 108, bx + 6, 150], fill=(150, 26, 32))
        d.rectangle([bx + 34, 108, bx + 40, 150], fill=(150, 26, 32))
        d.line([(bx + 1, 109), (bx + 1, 148)], fill=(200, 60, 64))
        d.line([(bx + 35, 109), (bx + 35, 148)], fill=(200, 60, 64))
        d.rectangle([bx + 8, 120, bx + 32, 123], fill=(190, 182, 168))
        d.rectangle([bx + 19, 124, bx + 21, 150], fill=(120, 120, 128))
        d.rectangle([bx + 10, 117, bx + 12, 119], fill=(200, 200, 210))  # solnita
        d.rectangle([bx + 14, 116, bx + 15, 119], fill=(170, 20, 20))  # ketchup
        d.rectangle([bx + 26, 116, bx + 30, 119], fill=(170, 170, 180))  # suport servetele
        d.line([(bx + 27, 116), (bx + 29, 116)], fill=(240, 240, 240))
    # tonomatul (jukebox)
    jx = 140
    d.rectangle([jx, 104, jx + 26, 150], fill=(70, 30, 26))
    d.pieslice([jx, 92, jx + 26, 118], 180, 360, fill=(70, 30, 26))
    d.arc([jx + 2, 94, jx + 24, 116], 180, 360, fill=(230, 160, 60))
    d.arc([jx + 5, 97, jx + 21, 113], 180, 360, fill=(220, 60, 60))
    d.rectangle([jx + 5, 110, jx + 21, 124], fill=(40, 26, 24))
    for i in range(3):
        d.line([(jx + 7, 113 + i * 4), (jx + 19, 113 + i * 4)], fill=(200, 180, 120))
    for i in range(5):
        d.rectangle([jx + 4 + i * 4, 128, jx + 5 + i * 4, 146], fill=(150, 90, 40) if i % 2 else (220, 140, 60))
    d.rectangle([jx + 9, 147, jx + 17, 150], fill=(30, 20, 18))
    # telefon + intrerupator
    d.rectangle([184, 58, 194, 76], fill=(34, 34, 36))
    d.rectangle([185, 59, 193, 62], fill=(20, 20, 22))
    for r in range(3):
        for c in range(3):
            d.point((186 + c * 3, 65 + r * 3), fill=(150, 150, 150))
    d.line([(193, 76), (195, 84), (192, 92)], fill=(20, 20, 22))
    d.rectangle([186, 92, 191, 99], fill=(190, 186, 170))
    d.rectangle([188, 94, 189, 96], fill=(60, 60, 60))
    # perete in spatele tejghelei: meniu, ceas, neon, usa bucatariei
    d.rectangle([206, 30, 262, 62], fill=(20, 22, 20))
    d.rectangle([206, 30, 262, 62], outline=(90, 70, 50))
    text(d, 214, 34, "MENU", (220, 220, 200))
    for i in range(4):
        d.line([(210, 43 + i * 5), (240, 43 + i * 5)], fill=(150, 150, 140))
        d.line([(248, 43 + i * 5), (258, 43 + i * 5)], fill=(150, 150, 140))
    d.ellipse([226, 12, 240, 26], fill=(220, 214, 200))
    d.ellipse([226, 12, 240, 26], outline=(60, 50, 40))
    d.line([(233, 19), (233, 14)], fill=(20, 20, 20))
    d.line([(233, 19), (237, 19)], fill=(20, 20, 20))
    d.rectangle([292, 28, 318, 102], fill=(70, 60, 56))
    d.rectangle([294, 30, 316, 102], fill=(110, 100, 92))
    d.ellipse([300, 40, 310, 50], fill=(150, 120, 70))
    d.ellipse([300, 40, 310, 50], outline=(60, 60, 60))
    # aparat de cafea
    d.rectangle([266, 80, 286, 102], fill=(140, 140, 150))
    d.rectangle([269, 84, 283, 92], fill=(40, 40, 46))
    d.rectangle([271, 96, 279, 101], fill=(30, 22, 18))
    # tejghea
    d.rectangle([198, 112, 320, 116], fill=(204, 196, 184))
    d.line([(198, 112), (320, 112)], fill=(240, 236, 226))
    d.rectangle([200, 117, 320, 150], fill=(120, 122, 132))
    d.rectangle([200, 126, 320, 131], fill=(160, 24, 30))
    for x in range(204, 320, 12):
        d.line([(x, 132), (x, 150)], fill=(100, 102, 110))
    d.rectangle([214, 106, 220, 111], fill=(230, 230, 230))  # cana
    # vitrina cu placinte
    d.rectangle([232, 100, 258, 111], fill=(170, 200, 210))
    d.rectangle([233, 101, 257, 110], fill=(40, 50, 56))
    for px_ in (237, 245, 253):
        d.pieslice([px_ - 3, 104, px_ + 3, 112], 180, 360, fill=(200, 140, 70))
    d.line([(233, 101), (257, 101)], fill=(230, 240, 245))
    # cafetiere
    d.rectangle([288, 104, 294, 111], fill=(60, 40, 30))
    d.rectangle([289, 102, 293, 104], fill=(30, 30, 30))
    d.rectangle([298, 104, 304, 111], fill=(150, 90, 30))
    d.rectangle([299, 102, 303, 104], fill=(30, 30, 30))
    # podea
    checker_floor(img, 150, H, (196, 190, 178), (26, 24, 26))
    d = ImageDraw.Draw(img)
    # scaune de bar
    for sx in (210, 236, 262, 288):
        d.rectangle([sx - 1, 146, sx + 1, 168], fill=(170, 170, 178))
        d.rectangle([sx - 5, 168, sx + 5, 169], fill=(120, 120, 128))
        d.ellipse([sx - 7, 141, sx + 7, 148], fill=(180, 26, 34))
        d.line([(sx - 5, 142), (sx + 3, 142)], fill=(230, 90, 90))
    light = Image.new("RGB", (W, H))
    if lights_on:
        ld = ImageDraw.Draw(light)
        for x in (66, 114, 162, 232, 276):
            lamp(d, light, x, 0, 14, floor=176)
        neon = Image.new("RGB", (W, H))
        nd = ImageDraw.Draw(neon)
        text(d, 268, 10, "GOOD", (255, 120, 150))
        text(d, 268, 17, "FOOD", (255, 120, 150))
        text(nd, 268, 10, "GOOD", (200, 30, 70))
        text(nd, 268, 17, "FOOD", (200, 30, 70))
        add_light(img, neon, 2)
        ld.rectangle([196, 104, 320, 118], fill=(40, 30, 16))
        add_light(img, light, 4)
    else:
        darken(img, (0, 0, W, H), 0.45)
    vignette(img, 0.45)
    return img


def kitchen():
    img = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 140], fill=(150, 156, 150))
    for y in range(0, 140, 8):
        d.line([(0, y), (W, y)], fill=(118, 124, 120))
    for y in range(0, 140, 8):
        off = 0 if (y // 8) % 2 == 0 else 4
        for x in range(off, W, 8):
            d.line([(x, y), (x, y + 7)], fill=(118, 124, 120))
    grain(img, (0, 0, W, 140), 6, seed=8)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 6], fill=(40, 42, 44))
    # usa batanta spre sala
    d.rectangle([6, 60, 32, 142], fill=(110, 100, 92))
    d.rectangle([6, 60, 32, 142], outline=(60, 54, 50))
    d.ellipse([12, 72, 26, 86], fill=(230, 180, 110))
    d.ellipse([12, 72, 26, 86], outline=(60, 60, 60))
    # aragaz + hota
    d.polygon([(36, 46), (112, 46), (104, 62), (44, 62)], fill=(130, 134, 140))
    d.line([(36, 46), (112, 46)], fill=(180, 184, 190))
    d.line([(44, 30), (104, 30)], fill=(60, 60, 64))
    for i, px_ in enumerate(range(48, 104, 12)):
        r = 4 + i % 3
        d.line([(px_, 30), (px_, 34)], fill=(60, 60, 64))
        d.ellipse([px_ - r, 34, px_ + r, 34 + r * 2], fill=(40, 40, 44))
        d.line([(px_ + r, 34 + r), (px_ + r + 4, 34 + r)], fill=(40, 40, 44))
    d.rectangle([38, 104, 110, 142], fill=(112, 116, 124))
    d.rectangle([38, 100, 110, 104], fill=(150, 154, 160))
    for bx in (50, 74, 98):
        d.ellipse([bx - 6, 100, bx + 6, 103], fill=(30, 30, 32))
    d.rectangle([44, 112, 104, 132], fill=(60, 62, 68))
    d.line([(44, 116), (104, 116)], fill=(160, 160, 170))
    # usa din spate + EXIT
    d.rectangle([146, 60, 180, 142], fill=(64, 68, 74))
    d.rectangle([146, 60, 180, 142], outline=(40, 42, 46))
    d.rectangle([157, 70, 169, 84], fill=(12, 14, 20))
    d.rectangle([148, 104, 178, 107], fill=(150, 150, 156))
    d.rectangle([152, 48, 174, 56], fill=(40, 10, 10))
    text(d, 155, 50, "EXIT", (255, 60, 60))
    # ventilatie
    d.rectangle([214, 14, 246, 30], fill=(100, 104, 108))
    for y in range(16, 30, 3):
        d.line([(216, y), (244, y)], fill=(50, 52, 56))
    # rafturi
    for y in (52, 76):
        d.rectangle([194, y, 296, y + 2], fill=(90, 90, 96))
    rnd = random.Random(4)
    for y in (52, 76):
        x = 198
        while x < 290:
            w = rnd.randint(5, 10)
            h = rnd.randint(8, 16)
            c = rnd.choice([(150, 40, 30), (190, 170, 90), (90, 110, 70), (160, 160, 170), (120, 80, 50)])
            d.rectangle([x, y - h, x + w, y - 1], fill=c)
            x += w + rnd.randint(2, 4)
    # masa de preparare
    d.rectangle([192, 106, 296, 109], fill=(176, 180, 186))
    d.rectangle([196, 110, 200, 142], fill=(120, 124, 130))
    d.rectangle([288, 110, 292, 142], fill=(120, 124, 130))
    d.rectangle([196, 128, 292, 130], fill=(120, 124, 130))
    d.rectangle([220, 100, 236, 105], fill=(150, 90, 70))  # tocator
    d.rectangle([250, 98, 254, 105], fill=(200, 200, 210))
    # frigider
    d.rectangle([300, 40, 320, 142], fill=(170, 174, 178))
    d.line([(300, 80), (320, 80)], fill=(110, 112, 118))
    d.rectangle([303, 60, 304, 72], fill=(90, 90, 96))
    checker_floor(img, 142, H, (120, 124, 126), (36, 38, 42), tile=14)
    light = Image.new("RGB", (W, H))
    ld = ImageDraw.Draw(light)
    d = ImageDraw.Draw(img)
    d.rectangle([140, 8, 186, 11], fill=(230, 245, 250))
    ld.rectangle([136, 6, 190, 14], fill=(90, 110, 110))
    ld.polygon([(140, 12), (186, 12), (230, 176), (96, 176)], fill=(24, 30, 30))
    ld.ellipse([150, 40, 176, 62], fill=(120, 10, 10))
    ld.ellipse([10, 70, 28, 88], fill=(80, 56, 20))
    add_light(img, light, 4)
    img = tint(img, (20, 40, 50), 0.18)
    vignette(img, 0.6)
    return img


def office(bare=False):
    """bare=True: biroul din 1987, cand pe pereti inca nu era nicio poza."""
    img = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(img)
    for x in range(0, W):
        c = (36, 42, 32) if (x // 5) % 2 == 0 else (30, 36, 27)
        d.line([(x, 0), (x, 118)], fill=c)
    grain(img, (0, 0, W, 118), 3, seed=9)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 118, W, 152], fill=(62, 40, 26))
    d.line([(0, 118), (W, 118)], fill=(100, 70, 46))
    for x in range(0, W, 16):
        d.line([(x, 120), (x, 151)], fill=(50, 32, 20))
    if bare:
        # 1987: peretii sunt inca goi. Doar un cui unde o sa stea rama si un calendar nou.
        d.line([(162, 30), (162, 33)], fill=(120, 110, 90))
        d.point((162, 29), fill=(170, 160, 140))
        d.rectangle([40, 36, 64, 66], fill=(226, 220, 204))
        d.rectangle([40, 36, 64, 44], fill=(40, 90, 60))
        text(d, 42, 37, "1987", (240, 240, 230))
        for r in range(4):
            for c in range(5):
                d.point((43 + c * 4, 48 + r * 4), fill=(80, 70, 60))
    else:
        # avizier cu fotografii si sfori rosii
        d.rectangle([24, 30, 92, 84], fill=(150, 112, 70))
        d.rectangle([24, 30, 92, 84], outline=(80, 56, 34))
        pins = []
        rnd = random.Random(2)
        for (x, y) in ((30, 36), (52, 40), (72, 34), (34, 60), (58, 62), (76, 58)):
            d.rectangle([x, y, x + 12, y + 14], fill=(220, 210, 190))
            d.rectangle([x + 2, y + 2, x + 10, y + 10], fill=(90 + rnd.randint(0, 30), 80, 70))
            d.point((x + 6, y), fill=(220, 30, 30))
            pins.append((x + 6, y))
        for a, b in ((0, 4), (1, 3), (2, 5), (4, 2)):
            d.line([pins[a], pins[b]], fill=(190, 20, 24))
        # „Angajatul lunii”
        d.rectangle([140, 26, 184, 72], fill=(150, 120, 50))
        d.rectangle([143, 29, 181, 69], fill=(30, 28, 26))
        d.ellipse([154, 36, 170, 54], fill=(150, 130, 116))
        d.rectangle([152, 54, 172, 69], fill=(50, 54, 62))
        d.rectangle([154, 34, 170, 40], fill=(40, 28, 20))
        d.point((159, 45), fill=(10, 10, 10))
        d.point((165, 45), fill=(10, 10, 10))
        text(d, 136, 76, "EMPLOYEE", (170, 150, 90))
        text(d, 142, 83, "OF THE", (170, 150, 90))
        text(d, 146, 90, "MONTH", (170, 150, 90))
    # fereastra mica
    window_view(img, (284, 28, 314, 70), 30)
    d.rectangle([282, 26, 316, 72], outline=(70, 50, 34))
    d.line([(299, 26), (299, 72)], fill=(70, 50, 34))
    # dulap de acte
    d.rectangle([246, 76, 276, 152], fill=(96, 100, 104))
    for y in (80, 104, 128):
        d.rectangle([249, y, 273, y + 20], outline=(60, 62, 66))
        d.rectangle([257, y + 8, 265, y + 10], fill=(170, 170, 176))
    # birou
    d.rectangle([96, 112, 234, 118], fill=(110, 72, 44))
    d.line([(96, 112), (234, 112)], fill=(150, 104, 66))
    d.rectangle([100, 119, 140, 152], fill=(84, 54, 32))
    d.rectangle([192, 119, 230, 152], fill=(84, 54, 32))
    for y in (124, 136):
        d.rectangle([104, y, 136, y + 9], outline=(60, 38, 22))
        d.rectangle([116, y + 4, 124, y + 5], fill=(190, 160, 90))
    # monitor CRT
    d.rectangle([170, 80, 214, 112], fill=(176, 170, 150))
    d.rectangle([175, 84, 209, 106], fill=(40, 70, 50))
    for y in range(85, 106, 2):
        d.line([(176, y), (208, y)], fill=(30, 56, 40))
    d.rectangle([186, 112, 198, 114], fill=(150, 146, 130))
    # lampa de birou
    d.line([(120, 111), (124, 96)], fill=(40, 40, 40))
    d.polygon([(118, 90), (130, 90), (134, 98), (114, 98)], fill=(30, 80, 50))
    d.rectangle([116, 109, 128, 111], fill=(40, 40, 40))
    # hartii
    d.polygon([(140, 110), (160, 108), (162, 111), (142, 112)], fill=(230, 226, 210))
    d.rectangle([150, 105, 164, 110], fill=(50, 40, 34))  # registrul
    # podea
    d.rectangle([0, 152, W, H], fill=(70, 46, 30))
    for y in range(152, H, 6):
        d.line([(0, y), (W, y)], fill=(54, 34, 22))
    rnd = random.Random(1)
    for y in range(152, H, 6):
        for _ in range(4):
            x = rnd.randint(0, W)
            d.line([(x, y), (x, y + 5)], fill=(54, 34, 22))
    light = Image.new("RGB", (W, H))
    ld = ImageDraw.Draw(light)
    ld.polygon([(116, 98), (132, 98), (170, 152), (80, 152)], fill=(60, 46, 20))
    ld.ellipse([100, 84, 150, 120], fill=(80, 64, 30))
    ld.rectangle([172, 82, 212, 108], fill=(20, 60, 36))
    add_light(img, light, 5)
    vignette(img, 0.6)
    return img


def diner_exterior_dawn():
    img = diner_exterior()
    img = tint(img, (250, 150, 120), 0.28)
    dgrad(img, (0, 0, W, 40), (120, 80, 110), (200, 120, 110), steps=6)
    return img
