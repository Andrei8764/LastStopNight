"""Iconitele HUD (12x12)."""

from PIL import Image, ImageDraw

from .pixel import text


def icon(name):
    im = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if name == "heart":
        rows = ["..##..##..", ".####.####", "##########", "##########", ".########.", "..######..",
                "...####...", "....##...."]
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v == "#":
                    d.point((1 + c, 2 + r), fill=(220, 40, 50))
        d.point((3, 3), fill=(255, 160, 160))
    elif name == "eye":
        d.ellipse([1, 3, 10, 8], fill=(230, 220, 230))
        d.ellipse([4, 3, 7, 8], fill=(150, 60, 200))
        d.rectangle([5, 5, 6, 6], fill=(10, 0, 10))
    elif name == "bolt":
        d.polygon([(7, 0), (2, 7), (5, 7), (4, 11), (9, 4), (6, 4)], fill=(250, 210, 60))
    elif name == "coin":
        d.ellipse([1, 1, 10, 10], fill=(220, 170, 50))
        d.ellipse([2, 2, 9, 9], outline=(250, 220, 110))
        text(d, 4, 3, "$", (120, 80, 10))
    elif name == "book":
        d.rectangle([2, 1, 9, 10], fill=(110, 70, 40))
        d.rectangle([3, 1, 3, 10], fill=(70, 40, 20))
        d.rectangle([9, 2, 10, 10], fill=(230, 220, 190))
        d.point((6, 4), fill=(220, 200, 140))
        d.line([(5, 6), (7, 6)], fill=(220, 200, 140))
    elif name == "clue":
        d.ellipse([1, 1, 7, 7], outline=(200, 200, 210))
        d.ellipse([2, 2, 6, 6], fill=(80, 120, 160))
        d.line([(6, 6), (10, 10)], fill=(150, 110, 70))
        d.line([(7, 6), (10, 9)], fill=(150, 110, 70))
    elif name == "moon":
        d.ellipse([1, 1, 10, 10], fill=(230, 230, 200))
        d.ellipse([4, 0, 12, 8], fill=(0, 0, 0, 0))
    return im
