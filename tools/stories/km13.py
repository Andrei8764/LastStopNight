"""KM 13: benzinaria de pe drumul prin padure. O singura noapte, cu Mara la casa."""
from common import (AND, add, add_companion, add_remarks, block, char, cmp, dec, decor, light, look, npc,
                    obj, playable, prop, setv)

ID = "km13"

PROPERTIES = [
    prop("player.fear", 0, 100, 10, "Frică", "bar", "eye", "#9A4FD0"),
    prop("player.life", 0, 100, 100, "Integritate", "bar", "heart", "#D8333F"),
    prop("story.clues", 0, 7, 0, "Indicii", "value", "clue"),
    prop("story.promise", 0, 1, 0, "Promisiune", "hidden"),
    prop("story.radio", 0, 2, 0, "Radio", "hidden"),
]

REDIRECTS = [
    {"condition": cmp("player.fear", ">=", 100), "target": "km.end.lights", "description": "Nu mai poți să respiri de frică"},
    {"condition": cmp("player.life", "<=", 0), "target": "km.end.lights", "description": "Totul se face alb"},
]

CHARACTERS = [
    char("mara", "Mara", desc="Casieră în tura de noapte."),
    char("girl", "Fetița în pelerină galbenă", has_reflection=False),
    char("hitchhiker", "Omul cu pălărie"),
    char("patron", "Camionagiul"),
    char("vlad", "Vlad", desc="Colegul de tură. Mecanic. Doarme mult."),
]

PLAYABLE = [
    playable("mara", "Casieră, tura de noapte", "Pe ecuson scrie Mara. Nu știe încă de ce contează asta."),
    playable("vlad", "Coleg de tură, mecanic", "Doarme pe scaun lângă casă. Știe orice mașină după sunet."),
]

LOCATIONS = [
    {"id": "gas", "name": "Pompele", "image": "images/gas_exterior.png", "floorY": 160, "minX": 12, "maxX": 300,
     "windows": [], "ambient": ["fog"], "sounds": ["night", "wind", "hum"], "points": {}, "actorTint": "#8C98C0",
     "decor": [decor("tube", 50, 61, 30, 3, fps=8, random=True), decor("tube", 130, 61, 30, 3, fps=7, random=True),
               decor("moths", 100, 64, 16, 14, fps=6)],
     "lights": [light(108, 74, 60, "#D0F0FF", 35, 0.3), light(15, 66, 24, "#FF8040", 50, 0.1),
                light(250, 102, 46, "#FFF0B0", 35)],
     "examine": [
         look("gas.sign", 15, "Panoul cu prețuri", "Benzina e 6,13. Motorina 6,66. Cineva de la firmă are un simț al umorului foarte specific.", y=62),
         look("gas.booth", 185, "Cabina telefonică", "Cabină telefonică. Funcționează cu fise. Fise! Nu mai fac fise din 2000.", hidden=True),
     ]},
    {"id": "store", "name": "Magazinul", "image": "images/gas_store.png", "floorY": 172, "minX": 14, "maxX": 300,
     "windows": [{"x": 11, "y": 31, "w": 118, "h": 64}], "ambient": [], "sounds": ["hum", "room"], "points": {"phone": {"x": 300, "y": 62}},
     "actorTint": "#E6EEE6",
     "decor": [decor("radio", 278, 99, 14, 9, fps=3), decor("led", 307, 30, 3, 3, fps=2),
               decor("wall_phone", 294, 56, 12, 26, fps=1)],
     "lights": [light(251, 105, 34, "#60A0FF", 55, 0.12), light(298, 100, 14, "#80FF90", 30)],
     "examine": [
         look("st.freezer", 251, "Congelatorul",
              "Înghețată de vanilie, expirată din 1999. Nimeni n-a observat 25 de ani. Sincer, respect.", y=100),
         look("st.poster", 152, "Afișul LIPSĂ",
              "LIPSĂ. Ioana, 7 ani, pelerină galbenă. Văzută ultima dată la km 13, pe 14 octombrie 1998. "
              "Afișul e nou. Lipiciul e încă ud. Cine l-a lipit azi?",
              effects=[add("story.clues", 1)], y=41),
         look("st.cctv", 290, "Monitorul camerei",
              "Camera 2 arată pompele. Lângă pompa 1 stă o fetiță în pelerină galbenă. Mă uit pe geam. Nu e nimeni. "
              "Mă uit pe monitor. E acolo.",
              effects=[add("player.fear", 10)], y=38),
         look("st.gum", 200, "Raftul cu gumă", "Gumă cu mentă, gumă cu căpșuni, gumă cu... lacrimi? Ah, nu, cu lămâie. Am nevoie de somn.",
              hidden=True),
     ]},
    {"id": "back", "name": "Magazia", "image": "images/gas_back.png", "floorY": 172, "minX": 14, "maxX": 300,
     "windows": [], "ambient": ["dust"], "sounds": ["room"], "points": {}, "actorTint": "#DCCCA8",
     "decor": [], "lights": [light(160, 20, 26, "#FFD890", 60, 0.6)],
     "examine": [
         look("bk.calendar", 222, "Calendarul",
              "Calendar din 1998, rămas pe octombrie. Ziua de 14 e încercuită cu roșu. Lângă, cineva a scris: tura M.",
              effects=[add("story.clues", 1), add("player.fear", 5)], y=45),
         look("bk.mop", 298, "Mopul", "Mopul are numele meu scris pe coadă. Nu l-am scris eu. Cred.", hidden=True),
     ]},
]

# masina omului cu palarie, oprita la pompa 2 (cu farul aprins)
CAR_AT_PUMP = [obj("car", 146, 132)]
CAR_AT_PUMP_LIGHTS = [light(148, 148, 14, "#FFF0C0", 55)]

BLOCKS = [
    block("km.start", "Tura de noapte",
          "Ultimul client a plecat la 11. De atunci sunt doar eu, radioul și congelatorul care bâzâie ca un "
          "frigider cu anxietate.\n\n"
          "Benzinăria de la kilometrul 13 nu e pe nicio hartă online. Șeful zice că e mai bine așa. Șeful zice multe.",
          location="store", time="23:40", player_x=280, chapter="KM 13: Tura de noapte",
          decisions=[dec("Dă mai tare la radio", "km.radio1", [setv("story.radio", 1)], hotspot=285, hy=103),
                     dec("Ieși să verifici pompele", "km.pumps", hotspot=16)]),
    block("km.radio1", "Știrile de la miezul nopții",
          "Radioul se aude prost, cu foșnet. Un crainic cu voce de anii 90: \"...accident grav la kilometrul 13. "
          "Șoferul a fugit de la locul faptei. Fetița, în vârstă de 7 ani, nu a fost găsită.\"\n\n"
          "Mă uit la ceas. Radioul ăsta n-are cum să prindă un post din 1998.",
          location="store", time="23:50", player_x=285, examine=[],
          decisions=[dec("Schimbă postul", "km.trucker", [add("story.clues", 1)]),
                     dec("Ascultă până la capăt", "km.radio2", [add("story.clues", 1), add("player.fear", 10)])]),
    block("km.radio2", "Până la capăt",
          "\"...casiera benzinăriei, Mara, a declarat că nu a văzut nimic.\"\n\n"
          "Mara. Pe ecusonul meu scrie Mara. În 1998 aveam patru ani. Radioul se oprește singur, cu un clic.",
          location="store", time="23:55", player_x=285, mood="dim",
          decisions=[dec("Respiră și mergi mai departe", "km.trucker", [add("story.clues", 1), setv("story.radio", 2)])]),
    block("km.pumps", "Pompele",
          "Afară e ceață. Tuburile de neon de sub copertină pâlpâie. Pompa 1 pornește singură. Contorul se "
          "învârte și se oprește la 13,13 litri. Nimeni n-a ținut pistolul.",
          location="gas", time="23:50", player_x=20,
          decisions=[dec("Oprește pompa", "km.trucker", [add("player.fear", 5)], hotspot=79, hy=118),
                     dec("Intră repede înapoi", "km.trucker")]),
    block("km.trucker", "Camionagiul",
          "La 12 și jumătate intră un camionagiu. Ia o cafea, un covrig și se uită de trei ori spre geam "
          "înainte să plătească.\n\n"
          "\"Ai grijă la km 13, fată\", zice. \"Am văzut-o de trei ori pe marginea drumului pe fetița aia. "
          "Eu nu mai opresc niciodată.\"",
          location="store", time="00:30", player_x=290, characters=[npc("patron", 250)],
          decisions=[dec("Întreabă-l de fetiță", "km.trucker.talk", [add("story.clues", 1)], hotspot=262),
                     dec("Dă-i restul și atât", "km.girl")]),
    block("km.trucker.talk", "Ce a văzut",
          "\"Pelerină galbenă, ca de ploaie. Stă cu fața spre pădure. Odată am oprit. M-am uitat în oglindă "
          "și era pe bancheta din spate.\"\n\n"
          "Își bea cafeaua dintr-o înghițitură, deși e fierbinte. \"Nu-i da nimic. Și nu-i promite nimic.\"",
          location="store", time="00:35", player_x=262, characters=[npc("patron", 250)],
          decisions=[dec("Mulțumește-i", "km.girl")]),
    block("km.girl", "Fetița",
          "La 1 și 13 clopoțelul nu sună, dar în magazin e o fetiță. Pelerină galbenă, cizme galbene, apă care "
          "picură pe gresie. Stă lângă geam.\n\n\"Ați văzut-o pe mama?\"",
          location="store", time="01:13", player_x=200, mood="dim", characters=[npc("girl", 70)],
          decisions=[dec("Întreab-o cum o cheamă", "km.girl.name", hotspot=90),
                     dec("Uită-te în geam, pe lângă ea", "km.girl.mirror", [add("player.fear", 15), add("story.clues", 1)], hotspot=110, hy=60),
                     dec("Sună la poliție", "km.police", [add("player.fear", 10)], hotspot=300, hy=66)]),
    block("km.girl.name", "Ioana",
          "\"Ioana\", zice. \"Mama a zis să aștept aici până vine ea cu mașina. Dar a trecut mult. Mi-e frig.\"\n\n"
          "Pelerina ei e udă, deși afară nu plouă de două zile.",
          location="store", time="01:16", player_x=90, mood="dim", characters=[npc("girl", 70)],
          decisions=[dec("Promite-i că o ajuți s-o găsească", "km.hitch", [setv("story.promise", 1)]),
                     dec("Spune-i frumos să plece", "km.hitch", [add("player.fear", 10)])]),
    block("km.girl.mirror", "În geam",
          "În geam, magazinul e gol. Rafturile, congelatorul, eu. Fetița nu e acolo.\n\n"
          "Pe sticlă, din interior, cineva a scris cu degetul în aburi: EL SE ÎNTOARCE.",
          location="store", time="01:15", player_x=110, mood="red", characters=[npc("girl", 70)],
          decisions=[dec("Întoarce-te spre ea", "km.hitch")]),
    block("km.police", "112",
          "Formez. Sună de mult. Răspunde o operatoare, cu voce plată: \"Dispeceratul de la kilometrul 13 a fost "
          "desființat în 1999. Vă rugăm să nu mai sunați.\"\n\nTon. Când mă întorc, fetița stă chiar lângă mine.",
          location="store", time="01:18", player_x=300, mood="dim", characters=[npc("girl", 282)],
          decisions=[dec("Pune receptorul jos", "km.hitch")]),
    block("km.hitch", "Omul cu pălărie",
          "La 3 fără 20, faruri. O mașină veche, cu un far spart, oprește la pompa 2. Coboară un om înalt, cu "
          "palton lung și pălărie. Nu-i văd fața, doar două puncte de lumină unde ar trebui să fie ochii.\n\n"
          "Fetița dispare. Omul bate în geam cu o monedă. \"Plinul. Plătesc cash.\"",
          location="gas", time="02:40", player_x=40, mood="dim", characters=[npc("hitchhiker", 236, True)],
          props=CAR_AT_PUMP, lights=CAR_AT_PUMP_LIGHTS,
          decisions=[dec("Servește-l", "km.hitch.serve", hotspot=128, hy=118),
                     dec("Spune-i că pompele sunt stricate", "km.hitch.lie", [add("player.fear", 20)]),
                     dec("Uită-te la mașina lui", "km.hitch.car", [add("story.clues", 1), add("player.fear", 10)],
                         hotspot=152, hy=152),
                     dec("Lasă-l pe Vlad să se uite la motor", "km.vlad.engine", [add("story.clues", 2), add("player.fear", 5)],
                         requires="vlad", hotspot=176, hy=146)]),
    block("km.vlad.engine", "Motorul",
          "Vlad ridică capota cu o scuză despre ulei. Omul cu pălărie nu se întoarce, doar se uită la pădure.\n\n"
          "Motorul e rece. De tot. Rece ca în februarie. Mașina asta n-a mers de ani buni, deși a oprit aici acum un minut. "
          "Pe blocul motor cineva a zgâriat cu un cui o dată: 14.10.1998.\n\n"
          "Vlad lasă capota jos foarte încet. \"Intrăm\", zice. \"Acum.\"",
          location="gas", time="02:44", player_x=176, mood="dim", characters=[npc("hitchhiker", 236, True)],
          props=CAR_AT_PUMP, lights=CAR_AT_PUMP_LIGHTS,
          decisions=[dec("Înapoi în magazin, repede", "km.back")]),
    block("km.hitch.serve", "Plata",
          "Îi fac plinul. Se uită în pădure tot timpul, cu spatele la mine. Plătește cu o bancnotă de "
          "10.000 de lei. Din cele vechi, cu Grigorescu. Nu mai sunt bune de 20 de ani.\n\n"
          "\"Păstrează restul\", zice. \"Pentru fetiță.\"",
          location="gas", time="02:50", player_x=128, characters=[npc("hitchhiker", 236, True)],
          props=CAR_AT_PUMP, lights=CAR_AT_PUMP_LIGHTS,
          decisions=[dec("Ia bancnota", "km.back", [add("story.clues", 1)])]),
    block("km.hitch.lie", "Stricate",
          "\"Pompele sunt stricate\", zic. Zâmbește, cred. Se aude ca un zâmbet.\n\n"
          "\"Nu sunt stricate. Nimic nu e stricat aici de 25 de ani.\" Pompa 2 pornește singură și se oprește "
          "la 13,13.",
          location="gas", time="02:45", player_x=60, mood="red", fx=["shake"], characters=[npc("hitchhiker", 236, True)],
          props=CAR_AT_PUMP, lights=CAR_AT_PUMP_LIGHTS,
          decisions=[dec("Fugi înăuntru", "km.back", [add("player.life", -15)])]),
    block("km.hitch.car", "Bara din față",
          "Mă duc spre mașină ca și cum aș verifica presiunea în roți. Bara din față e îndoită. În îndoitură "
          "sunt prinse bucăți de plastic galben. Ca de pelerină.\n\n"
          "Când ridic ochii, el stă chiar în spatele meu.",
          location="gas", time="02:46", player_x=152, mood="red", characters=[npc("hitchhiker", 170, True)],
          props=CAR_AT_PUMP, lights=CAR_AT_PUMP_LIGHTS,
          decisions=[dec("Treci pe lângă el, fără grabă", "km.back")]),
    block("km.back", "Pana de curent",
          "La 3 și 13 se ia curentul. Totul. Mă duc pe bâjbâite în magazie, la tabloul electric. Becul de "
          "rezervă se aprinde singur.\n\nFetița e acolo, cu fața spre perete. Arată cu degetul spre calendar.",
          location="back", time="03:13", player_x=60, mood="dim", characters=[npc("girl", 236)],
          decisions=[dec("Urcă siguranțele", "km.final", hotspot=125, hy=60),
                     dec("Stai lângă ea pe întuneric", "km.final", [add("player.fear", -15)])]),
    block("km.final", "Farurile",
          "Când ies, mașina cu farul spart e pe mijlocul drumului, cu motorul pornit. Farurile bat drept în "
          "fetița care stă pe marginea șoselei, cu fața spre pădure.\n\n"
          "Omul cu pălărie stă la volan. Așteaptă ceva. Poate pe mine.",
          location="gas", time="04:44", player_x=40, mood="dim",
          characters=[npc("girl", 150)],
          props=[obj("beams", 109, 132, front=True), obj("car_driver", 236, 146, front=True)],
          lights=[light(238, 162, 18, "#FFF0C0", 80), light(150, 150, 30, "#FFF0C0", 45)],
          decisions=[dec("Spune-i în față ce a făcut la km 13", "km.end.peace",
                         condition=cmp("story.clues", ">=", 3), hint="nu știi destul (3 indicii)", hotspot=268, hy=154),
                     dec("Dă radioul pe 13,13 și lasă-l să audă", "km.end.radio",
                         condition=AND(cmp("story.radio", ">=", 2), cmp("story.clues", ">=", 5)), show_locked=False),
                     dec("Ține fetița de mână până se face lumină", "km.end.wait",
                         condition=cmp("story.promise", "==", 1), hint="nu i-ai promis nimic", hotspot=150, hy=140),
                     dec("Fugi în magazin și încuie ușa", "km.end.lights")]),
    block("km.end.peace", "Kilometrul 13",
          "\"Tu ai lovit-o\", îi zic. \"Pe 14 octombrie 1998. Ai fugit și ai lăsat-o în ploaie.\"\n\n"
          "Motorul se oprește. Omul coboară, își scoate pălăria și, pentru prima oară, îi văd fața. E bătrân și "
          "plânge. Fetița se întoarce, îl ia de mână și pornesc amândoi pe marginea drumului, spre pădure.\n\n"
          "Dimineață, pe banca de lângă pompe, găsesc o pelerină galbenă, împăturită frumos.",
          background="images/endings/km13_peace.png", time="06:00", ending="good"),
    block("km.end.radio", "13,13 MHz",
          "Dau radioul pe 13,13 și îl întorc spre drum. Din difuzor vine vocea mea: \"Casiera a declarat că a "
          "văzut tot. Numărul mașinii, fața șoferului, tot.\"\n\n"
          "Farurile se sting. Mașina nu mai e acolo. Nici fetița. Doar radioul, care repetă știrea până dimineață, "
          "de fiecare dată un pic mai aproape de adevăr.",
          background="images/endings/km13_radio.png", time="05:13", ending="secret"),
    block("km.end.wait", "Schimbul de dimineață",
          "Țin fetița de mână. E rece ca apa din râu. Nu zicem nimic. Mașina stă pe loc până se luminează, "
          "apoi pur și simplu nu mai e.\n\n"
          "La 6 vine colegul de dimineață. \"Ai stat singură toată noaptea?\" mă întreabă. Mă uit la mâna mea. "
          "E udă.",
          background="images/endings/km13_wait.png", time="06:00", ending="alternative"),
    block("km.end.lights", "Faruri",
          "Fug. Încui ușa. Farurile se aprind mai tare, apoi mai tare, până când magazinul e alb, rafturile sunt "
          "albe, mâinile mele sunt albe.\n\n"
          "Dimineața, colegul găsește benzinăria deschisă și goală. Pe monitorul camerei 2, lângă pompa 1, "
          "acum sunt două siluete.",
          background="images/endings/km13_lights.png", time="", ending="bad"),
]

# ------------------------------------------------------------------ Vlad, insotitorul
add_companion(BLOCKS, "vlad", texts={
    "km.start": "Bine, nu chiar singură. Vlad, colegul de tură, doarme pe un scaun lângă casă de la 11 fără un sfert. "
                "Zice că el e tura de zi care a rămas din greșeală.",
    "km.pumps": "Vlad iese după mine, cu mâinile în buzunare. \"Pompa asta n-a mai mers din 2009\", zice. "
                "\"Am reparat-o eu. Adică, am încercat.\"",
    "km.trucker": "Camionagiul se uită lung la Vlad. \"Tu încă lucrezi aici?\" Vlad ridică din umeri: \"Cineva trebuie.\"",
    "km.girl": "Vlad, care până acum dormea, e brusc foarte treaz și foarte departe de geam.",
    "km.hitch": "Vlad îmi șoptește: \"Dacia 1300, din '78. Farul stâng spart, bara îndoită. Am mai văzut-o. În poze.\"",
    "km.final": "Vlad stă lângă mine. Pentru prima dată în noaptea asta nu face nicio glumă.",
    "km.end.peace": "Vlad ia pelerina și o pune frumos în dulapul cu obiecte pierdute. \"Dacă vine cineva după ea\", zice.",
    "km.end.lights": "Pe monitor, lângă cele două siluete, e un scaun gol. Exact cum îl lăsa Vlad.",
    "km.end.radio": "Vlad zice că el a dormit toată noaptea și n-a auzit nimic. Dar fredonează știrea.",
    "km.end.wait": "Vlad zice că el a dormit toată noaptea și n-a văzut nimic. Dar are cizmele ude.",
}, solo=["km.back"])

for loc in LOCATIONS:
    add_remarks(loc["examine"], "vlad", {
        "gas.sign": "Vlad: \"Prețurile le-a pus șeful. Când l-am întrebat de ce 6,66, a zis că așa a fost mereu.\"",
        "st.freezer": "Vlad scoate o înghețată. \"Expirată din '99? A rezistat până acum, rezist și eu.\"",
        "st.poster": "Vlad: \"Lipiciul e de la noi din magazie. Doar noi avem din ăsta.\"",
        "st.cctv": "Vlad: \"E acolo de când m-am angajat. Eu îi zic bună seara.\"",
        "bk.calendar": "Vlad: \"Tura M. Pe atunci era o Mariana. Sau Marcel. Sau... Mara?\"",
        "gas.booth": "Vlad: \"Am o fisă. O țin de noroc. Nu, n-o folosesc. Nu știi pe cine prinzi.\"",
    })

STORY = {
    "title": "KM 13",
    "order": 2,
    "author": "Benzinăria de la kilometrul 13",
    "description": "Mara lucrează noaptea la o benzinărie care nu apare pe nicio hartă. Radioul prinde știri din 1998, "
                   "pompele pornesc singure, iar pe la 1 și 13 intră o fetiță într-o pelerină galbenă.",
    "version": 1,
    "startBlockId": "km.start",
    "playerCharacterId": "mara",
    "properties": PROPERTIES,
    "redirects": REDIRECTS,
    "locations": LOCATIONS,
    "playable": PLAYABLE,
    "maxPlayers": 2,
    "characters": CHARACTERS,
    "blocks": BLOCKS,
}
