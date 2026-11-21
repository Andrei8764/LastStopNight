"""Locatiile restaurantului Black Pine Diner, folosite de doua povesti (Night Shift si 1987)."""
from common import decor, light, look


def black_pine_locations(examine=True):
    ex = examine
    return [
        {"id": "exterior", "name": "Parcarea", "image": "images/diner_exterior.png", "floorY": 162,
         "minX": 14, "maxX": 306, "windows": [], "ambient": ["rain", "fog"], "sounds": ["rain", "wind"], "points": {}, "actorTint": "#A4ACCC",
         "decor": [decor("moths", 90, 44, 16, 14, fps=6)],
         "lights": [light(205, 70, 36, "#FF3040", 55, 0.35), light(99, 56, 22, "#FFE0A0", 50, 0.1)],
         "examine": [
             look("ext.sign", 43, "Panoul", "BLACK PINE DINER. Dedesubt, zgâriat cu cheia: NU OPRI. Mersi, foarte util acum.", y=80),
             look("ext.car", 290, "Mașina", "Mașina mea. Pe bancheta din spate e geaca pe care o caut de o săptămână. Măcar ceva e normal aici.", y=142),
             look("ext.moon", 252, "Luna", "Lună plină. Și acum trei zile era tot plină. Hai că nu mă mai gândesc la asta.", hidden=True),
         ] if ex else []},
        {"id": "exterior_dawn", "name": "Parcarea, dimineața", "image": "images/diner_exterior_dawn.png", "floorY": 162,
         "minX": 14, "maxX": 306, "windows": [], "ambient": ["fog"], "sounds": ["birds", "wind"], "points": {}, "decor": [], "lights": [], "examine": [],
         "actorTint": "#F0D0C4"},
        {"id": "dining", "name": "Sala", "image": "images/diner_interior.png", "floorY": 174,
         "minX": 14, "maxX": 308, "windows": [{"x": 40, "y": 28, "w": 142, "h": 62}], "ambient": ["dust"], "sounds": ["room", "hum"], "actorTint": "#F2E6DA",
         "points": {"phone": {"x": 189, "y": 64}, "switch": {"x": 188, "y": 95}},
         "decor": [decor("fan", 180, 1, 40, 12, fps=10), decor("open_sign", 8, 44, 22, 9, fps=3, random=True),
                   decor("steam", 271, 66, 10, 16, fps=5), decor("cat", 296, 161, 16, 14, fps=2)],
         "lights": [light(276, 18, 18, "#FF5080", 60, 0.5), light(153, 112, 24, "#FFA040", 55, 0.05),
                    light(245, 106, 12, "#A0D8E8", 40)],
         "examine": [
             look("din.jukebox", 153, "Tonomatul",
                  "Are un singur disc: Tura de noapte (încă o dată). Apăs butonul. Nimic. Apoi cineva din bucătărie începe să fredoneze refrenul.", y=116),
             look("din.menu", 234, "Meniul",
                  "Specialitatea casei: plăcintă cu afine. Prețul a fost șters și rescris de vreo șase ori. Ultimul zice doar: nu întreba.", y=46),
             look("din.phone", 189, "Telefonul",
                  "Telefon cu disc. Lipsesc cifrele 0 și 3. Cineva chiar n-a vrut să mai sune nimeni la 03:00.", y=67),
             look("din.poster", 37, "Afișul", "ANGAJĂM. Dedesubt, cu pixul, mic de tot: din nou.", hidden=True),
         ] if ex else []},
        {"id": "kitchen", "name": "Bucătăria", "image": "images/kitchen.png", "floorY": 172, "minX": 16, "maxX": 294, "actorTint": "#DCE6EA",
         "windows": [], "ambient": ["dust"], "sounds": ["hum"], "points": {},
         "decor": [decor("steam", 70, 84, 10, 16, fps=5)],
         "lights": [light(163, 52, 16, "#FF2020", 55, 0.2), light(163, 12, 30, "#C0F0FF", 40, 0.45)],
         "examine": [
             look("kit.vent", 230, "Ventilația", "Pe grilaj e prins un fir de păr. Lung, negru. Nimeni de aici nu are părul așa.", y=22),
             look("kit.fridge", 296, "Frigiderul",
                  "Lapte, ouă, muștar. Și o farfurie acoperită cu folie, cu un bilețel: NU PENTRU OAMENI. Închid frigiderul.", y=90),
             look("kit.pans", 70, "Tigăile", "Toate tigăile lucesc de curate, în afară de una. N-o să întreb.", hidden=True),
         ] if ex else []},
        {"id": "office", "name": "Biroul", "image": "images/office.png", "floorY": 176, "minX": 14, "maxX": 306, "actorTint": "#E0D0B4",
         "windows": [], "ambient": ["dust"], "sounds": ["room", "clock"], "points": {},
         "decor": [decor("led", 206, 108, 3, 3, fps=2)],
         "lights": [light(124, 100, 30, "#FFC070", 55, 0.05), light(192, 95, 22, "#60FF90", 35, 0.15)],
         "examine": [
             look("off.board", 58, "Avizierul",
                  "Poze cu foști angajați, legate cu ață roșie. Pe câteva dintre ele, fețele sunt zgâriate cu pixul. Nu vreau să știu de ce.", y=57),
             look("off.frame", 162, "Angajatul lunii",
                  "Pe ramă scrie Angajatul lunii. Poza din ea e atât de ștearsă încât ar putea fi oricine. Poate fi chiar poza mea.", y=49),
             look("off.cabinet", 260, "Dosarele",
                  "Dosare de personal. Alex Morgan, Alex Pop, Alex Ionescu, Alex Varga... Toți Alex. Cineva are o problemă cu numele.", y=100),
             look("off.pc", 192, "Calculatorul",
                  "Ecranul zice: utilizator ALEX, parolă ALEX. Ultima conectare: 1987. Siguranța cibernetică la nivel de pădure.",
                  hidden=True),
             look("off.window", 300, "Fereastra", "Afară, între copaci, stă cineva. Clipesc. Nu mai e nimeni. Super.", hidden=True),
         ] if ex else []},
        # biroul in 1987: inca fara nicio poza pe pereti
        {"id": "office87", "name": "Biroul (1987)", "image": "images/office_1987.png", "floorY": 176, "minX": 14,
         "maxX": 306, "windows": [], "ambient": ["dust"], "sounds": ["room", "clock"], "points": {}, "actorTint": "#E0D0B4",
         "decor": [], "lights": [light(124, 100, 30, "#FFC070", 55, 0.05)], "examine": []},
    ]
