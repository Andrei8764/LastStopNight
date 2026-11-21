"""LAST STOP: NIGHT SHIFT, povestea principala (versiunea extinsa)."""
from common import (AND, add, add_companion, add_remarks, block, char, cmp, dec, look, npc, obj, playable,
                    prop, setv)
from black_pine import black_pine_locations

ID = "night_shift"

PROPERTIES = [
    prop("player.life", 0, 100, 100, "Integritate", "bar", "heart", "#D8333F"),
    prop("player.suspicion", 0, 100, 0, "Suspiciune", "bar", "eye", "#9A4FD0"),
    prop("player.energy", 0, 100, 80, "Energie", "bar", "bolt", "#E8C440"),
    prop("inventory.money", 0, 900, 0, "Bani", "value", "coin", fmt="${0}"),
    prop("story.hasHandbook", 0, 1, 0, "Registru", "flag", "book"),
    prop("story.evidence", 0, 3, 0, "Dovezi", "value", "clue", fmt="{0}/3"),
    prop("story.night", 1, 3, 1, "Noaptea", "value", "moon", fmt="{0}/3"),
    prop("story.cat", 0, 3, 0, "Pisica", "hidden"),
    prop("story.trust", 0, 1, 0, "Încredere în proprietar", "hidden"),
]

REDIRECTS = [
    {"condition": cmp("player.life", "<=", 0), "target": "ending.captured", "description": "Nu mai ai putere să te ridici"},
    {"condition": cmp("player.suspicion", ">=", 100), "target": "ending.replaced", "description": "Te-au văzut. Toți."},
]

CHARACTERS = [
    char("alex", "Alex Morgan", desc="Protagonistul, angajat nou."),
    char("faceless", "Clientul fără reflexie", has_reflection=False),
    char("red_woman", "Femeia în rochie roșie"),
    char("double", "Dublura"),
    char("patron", "Client de noapte"),
    char("ioana", "Ioana", desc="Ospătăriță nouă, a răspuns la același anunț."),
]

PLAYABLE = [
    playable("alex", "Ospătar nou, trei nopți", "A acceptat slujba pentru bani. Citește regulile, de obicei."),
    playable("ioana", "Ospătăriță, tot din anunț", "Glumește când îi e frică. Restaurantul nu pare să știe că există."),
]


def cat(night):
    return look("cat.%d" % night, 300, "Pisica",
                ["Sub tejghea stă o pisică neagră. Mă studiază ca pe un client care nu lasă bacșiș. Îi dau o bucată de șuncă.",
                 "Pisica e tot acolo. Azi îmi dă voie s-o mângâi. Toarce ca un motor vechi. Primul lucru normal din noaptea asta.",
                 "Pisica mi se urcă în brațe și nu mai vrea să coboare. Ok, am înțeles. Tu ai fost aici înaintea tuturor."][night - 1],
                effects=[add("story.cat", 1)], y=166)


HANDBOOK = (
    "În sertar e un registru legat în piele, cu un brad pe copertă. Pe prima pagină, scris apăsat: "
    "REGULI. CITEȘTE-LE. CHIAR LE CITEȘTE.\n\n"
    "1. Dacă un client nu apare în geam, nu-i da de mâncare. Stinge lumina și du-te în bucătărie.\n"
    "2. Femeia în roșu, după ora 1: nu te uita în ochii ei. Pune-i un pahar cu apă și pleacă.\n"
    "3. Cine comandă carne crudă primește carne crudă. Nu-l contrazice și nu te uita cum mănâncă.\n"
    "4. Trei bătăi în ușa din spate, când sala e goală: oprește ventilația și nu deschide.\n"
    "5. La 3 fix nu răspunde la telefon. Dacă răspunzi, nu-ți spune numele.\n"
    "6. Dacă intră cineva care arată ca tine: încuie bucătăria, stinge lumina, așteaptă.\n\n"
    "Ultima pagină e ruptă. Cineva a scris pe margine, cu alt pix: nu le crede pe toate."
)

BLOCKS = [
    # ================================================================ intro
    block("intro.arrival", "Sosirea",
          "Plouă de la ultima benzinărie. Parchez în fața restaurantului Black Pine Diner. Firma roșie bâzâie "
          "deasupra ușii, ca o muscă prinsă în borcan.\n\n"
          "300 de dolari pe tură, trei nopți, de la 10 seara la 6 dimineața. Anunțul nu zicea nimic despre restul. "
          "Anunțurile bune nu zic niciodată.",
          location="exterior", time="21:50", player_x=262, chapter="LAST STOP: NIGHT SHIFT",
          decisions=[dec("Intră în restaurant", "intro.counter", hotspot=203, hy=110),
                     dec("Urcă înapoi în mașină și pleacă", "ending.quit", hotspot=290, hy=142)]),
    block("intro.counter", "Primul contact",
          "Înăuntru miroase a cafea arsă și a clor. Nu e nimeni. Pe tejghea e un bilet prins sub o cheie ruginită:\n\n"
          "\"Bine ai venit, Alex. Biroul e în spate. Citește registrul ÎNAINTE de miezul nopții.\"\n\n"
          "Ciudat. Nu-mi amintesc să-i fi spus cuiva cum mă cheamă.",
          location="dining", time="21:58", player_x=24,
          props=[obj("note", 205, 106)],
          examine=[look("ic.note", 210, "Biletul",
                        "Scris de mână, cu pix albastru, de cineva care face liste. Cheia e ruginită și nu deschide "
                        "nimic din ce am încercat. O păstrez, totuși.", y=109)],
          decisions=[dec("Mergi în birou", "intro.office", hotspot=300, hy=60),
                     dec("Lasă biletul și apucă-te de treabă", "night1.open")]),
    block("intro.office", "Biroul",
          "Biroul e mic, cald și miroase a hârtie veche. Pe pereți, poze cu foști angajați. Pe birou, un monitor "
          "pornit care arată sala din spatele meu. Mă întorc. Sala e goală. Pe monitor, la fel. Bine.",
          location="office", time="22:00", player_x=30,
          decisions=[dec("Deschide sertarul biroului", "intro.handbook", hotspot=120, hy=130),
                     dec("Nu e treaba ta. Înapoi la tejghea", "night1.open")]),
    block("intro.handbook", "Registrul", HANDBOOK,
          location="office", time="22:05", player_x=130, closeup="images/closeups/handbook.png",
          decisions=[dec("Citește regulile de două ori", "night1.open", [setv("story.hasHandbook", 1)]),
                     dec("Închide registrul, sună a glumă", "night1.open")]),

    # ================================================================ noaptea 1
    block("night1.open", "Primii clienți",
          "Prima oră e aproape plictisitoare. Un camionagiu vrea cafea neagră și nu zice nimic. O pereche se ceartă "
          "în șoaptă pe cine conduce la întoarcere.\n\n"
          "Pe la miezul nopții sala se golește. Clopoțelul sună. Intră un bărbat înalt, la costum, și se oprește "
          "lângă masa de la geam. Zâmbește. Ceva nu e bine cu el, dar nu-mi dau seama ce.",
          location="dining", time="00:40", player_x=250, chapter="NOAPTEA 1: Prima tură",
          characters=[npc("faceless", 112), npc("patron", 270, True)], examine=[cat(1)],
          decisions=[dec("Uită-te în geam, pe lângă el", "night1.reflection", hotspot=150, hy=60),
                     dec("Du-te și ia-i comanda", "night1.served", hotspot=132)]),
    block("night1.reflection", "Clientul fără reflexie",
          "Mă prefac că șterg o masă și mă uit în geam. Mă văd pe mine, cârpa, lampa care se leagănă. "
          "Locul unde stă el e gol. Complet gol.\n\n"
          "Nu se mișcă. Doar capul i se întoarce spre mine. Încet. Prea încet.",
          location="dining", time="00:45", player_x=150, mood="dim", characters=[npc("faceless", 112)],
          decisions=[dec("Stinge lumina și fugi în bucătărie", "night1.safe", hotspot=188, hy=95),
                     dec("Întreabă-l direct ce e", "night1.danger", hotspot=126)]),
    block("night1.served", "Greșeala",
          "Îi pun farfuria în față. Nu mănâncă. Nu clipește. Doar zâmbește, cu prea mulți dinți, până când "
          "lampa de deasupra pâlpâie și scaunul rămâne gol.\n\n"
          "Pe masă a rămas o pată neagră și lipicioasă. Miroase a pământ ud.",
          location="dining", time="00:50", player_x=132, fx=["flicker"], characters=[npc("faceless", 112)],
          decisions=[dec("Curăță pata", "night1.knocks", [add("player.energy", -5)], hotspot=112, hy=120),
                     dec("Lasă pata și du-te în spate", "night1.knocks", [add("player.suspicion", 5)])]),
    block("night1.safe", "În bucătărie",
          "Sting lumina și mă strecor în bucătărie. Mă lipesc de frigider. E rece și bâzâie, ca mine.\n\n"
          "Pași. Rari și grei. Se opresc fix în fața ușii batante. Prin geamul rotund nu se vede nimic. "
          "Apoi apare o pată de abur, de parcă cineva respiră pe sticlă.",
          location="kitchen", time="01:30", player_x=280, mood="dark",
          decisions=[dec("Stai nemișcat", "night1.knocks", [add("player.energy", -5)]),
                     dec("Deschide ușa", "night1.danger", hotspot=20, hy=79)]),
    block("night1.danger", "Regula încălcată",
          "E deja lângă mine. Lumina tremură și fața lui se topește, ca o mască de ceară ținută prea aproape de foc. "
          "Simt un frig în umăr, de parcă cineva mi-a furat o bucată de căldură.",
          location="dining", time="01:10", player_x=150, mood="red", fx=["shake", "flicker"],
          characters=[npc("faceless", 128)],
          decisions=[dec("Fugi", "night1.knocks",
                         [add("player.life", -35), add("player.suspicion", 20), add("player.energy", -15)])]),
    block("night1.knocks", "Trei bătăi",
          "Pe la 2 se face liniște. Sala e goală, cafeaua s-a răcit.\n\n"
          "Cineva bate în ușa din spate a bucătăriei. Trei bătăi. Pauză. Încă trei. Ventilația duce mirosul de "
          "prăjeli afară, în pădure. Poate de-aia a venit.",
          location="kitchen", time="02:10", player_x=40, mood="dim",
          decisions=[dec("Oprește ventilația și taci", "night1.knocks.quiet", hotspot=230, hy=22),
                     dec("Întreabă cine e", "night1.knocks.voice", [add("player.suspicion", 10)]),
                     dec("Deschide ușa", "night1.knocks.open", hotspot=163, hy=100)]),
    block("night1.knocks.quiet", "Liniște",
          "Ventilatorul se oprește cu un oftat. În liniștea care rămâne aud cum cineva, afară, adulmecă aerul. "
          "Lung. Apoi pași care se îndepărtează prin frunze.\n\nRespir abia după un minut.",
          location="kitchen", time="02:14", player_x=230, mood="dark",
          decisions=[dec("Înapoi la tejghea", "night1.phone", [add("player.energy", -5)])]),
    block("night1.knocks.voice", "Vocea",
          "\"Alex, sunt eu, proprietarul. Mi-am uitat cheile. Deschide-mi, te rog, că plouă.\"\n\n"
          "Vocea e caldă și obosită. Exact cum ți-ai imagina un proprietar. Doar că nu l-am văzut niciodată "
          "și nu știu cum vorbește.",
          location="kitchen", time="02:12", player_x=150, mood="dim",
          decisions=[dec("Nu deschide", "night1.phone"),
                     dec("Deschide, totuși e șeful", "night1.knocks.open", hotspot=163, hy=100)]),
    block("night1.knocks.open", "Ușa din spate",
          "Afară nu e nimeni. Doar ploaia și pădurea. Fac un pas să mă uit mai bine și o mână rece mă prinde "
          "de încheietură, din întuneric, de undeva de jos.\n\nTrag de mână până mă ustură pielea.",
          location="kitchen", time="02:15", player_x=163, mood="red", fx=["shake"],
          decisions=[dec("Trage ușa și încuie", "night1.phone", [add("player.life", -25), add("player.suspicion", 10)])]),
    block("night1.phone", "Telefonul de la 3",
          "03:00. Telefonul de pe perete sună. Șapte ori. Se oprește. Sună din nou.\n\n"
          "Robotul pornește singur și aud o voce pe care o știu prea bine. A mea. \"Alex? Sunt eu. Sunt afară, "
          "în parcare. Lasă-mă înăuntru. Spune-mi doar cum te cheamă.\"",
          location="dining", time="03:00", player_x=240, mood="dim", fx=["ring"],
          decisions=[dec("Lasă-l să sune", "night1.photo"),
                     dec("Ridică, dar nu zice nimic", "night1.photo", [add("player.suspicion", 10)], hotspot=189, hy=67),
                     dec("Răspunde: Sunt Alex", "ending.captured", hotspot=189, hy=67),
                     dec("Las-o pe Ioana să răspundă", "night1.ioana.phone", requires="ioana")]),
    block("night1.ioana.phone", "Ioana la telefon",
          "Ioana ridică receptorul înainte să apuc să zic ceva. \"Black Pine, bună seara. Nu, nu cunosc niciun Alex. "
          "Eu sunt ospătărița.\"\n\n"
          "Pe fir se face liniște. Apoi vocea mea, de acolo, zice încet, ca pentru cineva de lângă ea: \"Nu e pe listă.\" "
          "Și închide.\n\n"
          "Ioana pune receptorul jos. \"Cineva ține o listă\", zice. \"Și eu nu sunt pe ea. Nu știu dacă e bine.\"",
          location="dining", time="03:01", player_x=200, mood="dim",
          decisions=[dec("Notează asta. E o dovadă", "night1.photo", [add("story.evidence", 1)])]),
    block("night1.photo", "Fotografia",
          "Ca să nu adorm, scotocesc prin dulapul din birou. Printre facturi îngălbenite găsesc o poză: "
          "echipa restaurantului, în fața firmei, 1987.\n\n"
          "Al treilea din stânga poartă uniforma mea. Are fața mea. Pe ecuson scrie ALEX.",
          location="office", time="05:40", player_x=250, closeup="images/closeups/photo.png",
          decisions=[dec("Bagă poza în buzunar", "night1.dawn", [add("story.evidence", 1)]),
                     dec("Pune-o la loc și uită de ea", "night1.dawn")]),
    block("night1.dawn", "Dimineața",
          "La 6 fix ies în parcare. Cerul e roz, păsările cântă, totul arată ca o reclamă la cereale.\n\n"
          "Pe parbriz e un plic. Înăuntru, 300 de dolari și un bilet: \"Ne vedem diseară, Alex.\" "
          "Nu l-am auzit pe nimeni ieșind.",
          location="exterior_dawn", time="06:00", player_x=200, mood="dawn",
          examine=[look("dawn1.car", 280, "Capota", "Pe capotă sunt urme de palme. Mici. Din interior.", y=146)],
          decisions=[dec("Ia banii. Te întorci diseară", "night2.open",
                         [add("inventory.money", 300), setv("story.night", 2), add("player.energy", 15)], hotspot=294, hy=138),
                     dec("Ia banii și nu te mai întorci", "ending.runaway", [add("inventory.money", 300)])]),

    # ================================================================ noaptea 2
    block("night2.open", "Carne crudă",
          "A doua noapte începe prea liniștit. Pe la 12 și jumătate vine un tip cu barbă și șapcă. Se așază lângă "
          "geam și comandă friptură. Crudă. \"Complet crudă, puișor. Fără sare.\"\n\n"
          "Bucătarul nu există, deci bucătarul sunt eu.",
          location="dining", time="00:30", player_x=250, chapter="NOAPTEA 2: Ceva nu e în regulă",
          characters=[npc("patron", 66)], examine=[cat(2)],
          decisions=[dec("Pune farfuria pe masă și nu te uita", "night2.meat.safe", hotspot=84, hy=120),
                     dec("Spune-i că nu servim carne crudă", "night2.meat.angry"),
                     dec("Uită-te cum mănâncă", "night2.meat.look", hotspot=84, hy=120)]),
    block("night2.meat.safe", "Nu te uita",
          "Las farfuria și mă întorc cu spatele. Aud ceva ce n-o să uit prea curând. Ceva ud. Ceva care se rupe.\n\n"
          "Când mă întorc, farfuria e curată lună și pe masă sunt 20 de dolari. Bacșiș generos, măcar.",
          location="dining", time="00:40", player_x=110, characters=[npc("patron", 66)],
          decisions=[dec("Strânge masa", "night2.red", [add("inventory.money", 20)], hotspot=100, hy=120)]),
    block("night2.meat.angry", "Nu-l contrazice",
          "\"Nu servim carne crudă\", zic. Se lasă o liniște în care se aude până și frigiderul.\n\n"
          "Se ridică. E mai înalt decât părea. Mult mai înalt. Mă împinge în tejghea fără să mă atingă, "
          "doar cu privirea. Apoi iese în ploaie.",
          location="dining", time="00:35", player_x=200, mood="red", fx=["shake"],
          decisions=[dec("Ridică-te de jos", "night2.red", [add("player.life", -15)])]),
    block("night2.meat.look", "M-am uitat",
          "Mă uit. Nu folosește tacâmuri. Nici mâini. Gura i se deschide mai mult decât ar trebui să se deschidă "
          "o gură.\n\nSub geacă, pe uniforma lui veche de ospătar, are un ecuson ruginit: ALEX, 1961. "
          "Apoi se oprește din mestecat și se uită la mine.",
          location="dining", time="00:40", player_x=84, mood="red", characters=[npc("patron", 66)],
          decisions=[dec("Fugi la tejghea", "night2.red",
                         [add("player.life", -10), add("player.suspicion", 15), add("story.evidence", 1)])]),
    block("night2.red", "Femeia în roșu",
          "La 1 și 5 intră o femeie într-o rochie roșie, udă leoarcă, deși afară s-a oprit ploaia de o oră. "
          "Se așază la tejghea și așteaptă. Știu că așteaptă să mă uit la ea.",
          location="dining", time="01:05", player_x=40, characters=[npc("red_woman", 246, True)],
          decisions=[dec("Pune-i un pahar cu apă, cu ochii în jos", "night2.red.safe", hotspot=222, hy=110),
                     dec("Uită-te la ea", "night2.red.danger", hotspot=232)]),
    block("night2.red.safe", "Paharul cu apă",
          "Pun paharul fără să ridic privirea. O aud cum bea. Înghițituri lungi și ude. Cu coada ochiului văd "
          "că buzele ei nu se mișcă.\n\n"
          "Când paharul e gol, o voce îmi șoptește direct în ureche, deși ea e la doi metri: \"Nu ești primul Alex.\"",
          location="dining", time="01:20", player_x=222, mood="dim", characters=[npc("red_woman", 246, True)],
          decisions=[dec("Notează ce a zis pe un șervețel", "night2.camera", [add("story.evidence", 1)]),
                     dec("Încearcă să uiți", "night2.camera")]),
    block("night2.red.danger", "Privirea",
          "Mă uit. Ochii ei sunt negri de la o margine la alta, ca două fântâni. Zâmbește și toate luminile din "
          "restaurant se sting deodată.\n\nCeva ud îmi atinge obrazul. Cred că a fost mâna ei. Sper.",
          location="dining", time="01:15", player_x=222, mood="red", fx=["shake", "flicker"],
          characters=[npc("red_woman", 236, True)],
          decisions=[dec("Fugi în bucătărie", "night2.camera",
                         [add("player.life", -20), add("player.suspicion", 15)], hotspot=305, hy=45)]),
    block("night2.camera", "Înregistrarea",
          "Mai târziu, în birou, derulez înregistrările camerei. 01:22. Eu, la tejghea, servind un client fără "
          "față.\n\n"
          "Doar că la 01:22 eram în bucătărie. Sunt sigur. Pe ecran, celălalt eu se oprește, se întoarce încet "
          "și se uită direct în cameră. Îmi face cu mâna.",
          location="office", time="04:10", player_x=190, closeup="images/closeups/camera.png", fx=["flicker"],
          decisions=[dec("Copiază înregistrarea pe o casetă", "night2.dawn", [add("story.evidence", 1)]),
                     dec("Șterge tot", "night2.dawn")]),
    block("night2.dawn", "A doua dimineață",
          "Mașina pornește abia a treia oară. Mă uit în oglinda retrovizoare la restaurant. Firma e stinsă, "
          "dar mi s-a părut că cineva stă la geam și îmi face cu mâna.\n\n"
          "Pe scaunul din dreapta, alt plic cu 300 de dolari. Încă o noapte.",
          location="exterior_dawn", time="06:00", player_x=230, mood="dawn",
          examine=[look("dawn2.radio", 290, "Radioul din mașină",
                        "La radio: accident la kilometrul 13, lângă benzinărie. Drumul e închis până la prânz. "
                        "Ciudat, n-am auzit niciodată de o benzinărie pe drumul ăsta.", y=140)],
          decisions=[dec("Du-te acasă și dormi", "night3.open",
                         [add("inventory.money", 300), setv("story.night", 3), add("player.energy", 15)], hotspot=300, hy=138)]),

    # ================================================================ noaptea 3
    block("night3.open", "Ultima tură",
          "Ultima noapte. Pe tejghea, un bilet nou: \"Termină tura. Orice vezi. Oricine intră.\"\n\n"
          "Am recitit registrul de trei ori. Paginile par să se contrazică acum: una zice să încui bucătăria, "
          "alta să nu te ascunzi niciodată. Și din spate se aude cineva fluierând melodia pe care o fluier eu "
          "când am emoții.",
          location="dining", time="22:10", player_x=250, chapter="NOAPTEA 3: Ultimul schimb", mood="dim",
          fx=["flicker"], examine=[cat(3)],
          decisions=[dec("Recitește dovezile adunate", "night3.call"),
                     dec("Ignoră tot și muncește", "night3.call", [add("player.energy", -10)])]),
    block("night3.call", "Proprietarul",
          "03:00. Telefonul. De data asta nu sună de șapte ori, sună o dată. Robotul pornește și, în sfârșit, "
          "aud vocea proprietarului. Răgușită, de om bătrân.\n\n"
          "\"Regula 6 e greșită. Când vine celălalt, nu te ascunde. Deschide-i și lasă-l să-ți ia locul. "
          "E mai simplu așa. Pentru toată lumea.\"",
          location="dining", time="03:00", player_x=240, mood="dim", fx=["ring"],
          decisions=[dec("Ai încredere în registru", "night3.knocks"),
                     dec("Ai încredere în proprietar", "night3.knocks", [setv("story.trust", 1), add("player.suspicion", 10)]),
                     dec("Întreabă-l cum arată celălalt", "night3.call.ask", hotspot=189, hy=67)]),
    block("night3.call.ask", "Cum arată",
          "\"Cum arată celălalt?\" întreb în receptor.\n\n"
          "Pauză lungă. Apoi râde. Și râsul nu mai e al unui om bătrân. E al meu. "
          "\"Ca tine, Alex. Doar că mai odihnit.\"",
          location="dining", time="03:02", player_x=189, mood="red",
          decisions=[dec("Trântește telefonul", "night3.knocks", [add("player.suspicion", 5)])]),
    block("night3.knocks", "Eu, la ușă",
          "Pe la 5 fără un sfert, bătăi în ușa din spate. Trei. Și vocea mea, de afară: "
          "\"Hai, frate, deschide. E frig. Sunt eu, pe bune.\"\n\n"
          "Se aude până și cum își freacă palmele. Exact cum le frec eu.",
          location="kitchen", time="04:45", player_x=60, mood="dark",
          decisions=[dec("Nu deschide", "night3.double"),
                     dec("Deschide", "night3.double",
                         [add("player.life", -40), add("player.suspicion", 20)], hotspot=163, hy=100)]),
    block("night3.double", "Dublura",
          "05:30. Clopoțelul de la intrare. Intră un tip cu geaca udă, cu părul meu, cu mersul meu. "
          "Pe ecuson scrie ALEX.\n\n"
          "Se oprește în mijlocul sălii și zâmbește cum zâmbesc eu în poze. \"Gata tura\", zice. "
          "\"Du-te acasă. Preiau eu de aici.\"",
          location="dining", time="05:30", player_x=250, mood="dim", characters=[npc("double", 60)],
          decisions=[dec("Arată-i dovezile", "ending.sunrise", [add("inventory.money", 300)],
                         condition=AND(cmp("player.life", ">", 0), cmp("story.evidence", ">=", 2)),
                         hint="ai nevoie de cel puțin 2 dovezi"),
                     dec("Du-te spre el", "ending.replaced", hotspot=84),
                     dec("Încuie bucătăria, stinge lumina și așteaptă", "ending.secret",
                         condition=cmp("story.evidence", ">=", 3), hotspot=306, hy=45, hint="ai nevoie de toate cele 3 dovezi"),
                     dec("Ia pisica în brațe și ieși prin spate", "ending.cat",
                         condition=cmp("story.cat", ">=", 3), hotspot=292, hy=166, show_locked=False),
                     dec("Fă cum a zis proprietarul: lasă-l să preia", "ending.replaced",
                         condition=cmp("story.trust", "==", 1), show_locked=False),
                     dec("Ioana: \"Dacă ești el, spune-mi cum mă cheamă\"", "ending.ioana", requires="ioana")]),

    # ================================================================ finaluri
    block("ending.sunrise", "The Last Sunrise",
          "Pun dovezile pe tejghea, una câte una: poza, șervețelul, caseta. Celălalt se uită la ele mult. "
          "Zâmbetul i se topește ca ceara. Când ridic ochii, sala e goală.\n\n"
          "La 6 ies în parcare. Soarele răsare peste brazi și pădurea arată, pentru prima dată, doar ca o pădure. "
          "Nu mă uit înapoi. Bine, mă uit o dată.",
          background="images/endings/ending_sunrise.png", time="06:00", ending="good"),
    block("ending.ioana", "Doi pe tură",
          "\"Dacă ești el\", zice Ioana, \"spune-mi cum mă cheamă.\"\n\n"
          "Celălalt deschide gura. Zâmbește. Nu zice nimic. Nu știe. N-avea de unde: în trei nopți n-am vorbit despre ea "
          "cu nimeni de aici. Restaurantul ține minte numele, dar doar pe cele pe care i le spui.\n\n"
          "Se topește încet, ca o lumânare uitată aprinsă. La 6 ieșim amândoi în parcare. Pe ușă e un afiș nou, scris "
          "de mână: ANGAJĂM. DOI.",
          background="images/endings/ending_sunrise.png", time="06:00", ending="good"),
    block("ending.captured", "Employee of the Month",
          "Vocea din receptor râde cu gura mea. Luminile se sting. Când se aprind la loc, eu sunt cel din spatele "
          "geamului.\n\n"
          "O săptămână mai târziu, un tip nou găsește registrul pe tejghea. Pe perete, în rama cu Angajatul "
          "lunii, e o poză cu mine. Nu zâmbesc.",
          background="images/endings/ending_captured.png", time="", ending="bad"),
    block("ending.replaced", "Employee of the Month",
          "Îi întind mâna. E rece și cunoscută. Clipesc și el poartă geaca mea. Eu nu mai port nimic. "
          "Nicio culoare, nicio greutate.\n\n"
          "În noaptea următoare, Alex servește cafea la tejghea. Eu stau la masa de lângă geam și aștept. "
          "Geamul nu mă mai arată.",
          background="images/endings/ending_replaced.png", time="", ending="bad"),
    block("ending.secret", "The Fourth Night",
          "Încui bucătăria și sting lumina. Pașii lui trec pe lângă ușă, o dată, de două ori, apoi intră în pereți. "
          "În întuneric, dovezile se leagă: ani diferiți, aceeași față, același ecuson.\n\n"
          "Dimineață caut restaurantul pe hartă. Drumul e acolo. Pădurea e acolo. Black Pine Diner nu e nicăieri. "
          "Foștii angajați n-au plecat niciodată. Ei sunt restaurantul.",
          background="images/endings/ending_secret.png", time="06:00", ending="secret"),
    block("ending.cat", "Pisica de noapte",
          "Iau pisica în brațe. Se lipește de mine și toarce atât de tare încât nu-l mai aud pe celălalt. "
          "Ies prin ușa din spate, prin ploaie, până la mașină. Nimeni nu ne oprește. Nimeni nu îndrăznește.\n\n"
          "La radio e 06:02. Pisica doarme pe scaunul din dreapta. Cred că o s-o cheme Regula 7.",
          background="images/endings/ending_cat.png", time="06:02", ending="secret"),
    block("ending.quit", "Plecarea",
          "Pornesc motorul. În oglinda retrovizoare, firma roșie pâlpâie de trei ori, de parcă mi-ar face cu mâna. "
          "N-o să aflu niciodată ce era acolo.\n\n"
          "Uneori, la 3 noaptea, îmi sună telefonul de pe un număr necunoscut. Nu răspund niciodată.",
          background="images/endings/ending_quit.png", time="21:52", ending="alternative"),
    block("ending.runaway", "300 de dolari",
          "Iau plicul și conduc până nu mai văd brazi. 300 de dolari pentru o noapte. Nu e rău.\n\n"
          "Peste o lună văd din nou anunțul pe internet. Același salariu, aceleași ore. La rubrica Contact "
          "scrie numele meu.",
          background="images/endings/ending_quit.png", time="06:10", ending="alternative"),
]

# ------------------------------------------------------------------ Ioana, insotitoarea
add_companion(BLOCKS, "ioana", texts={
    "intro.arrival": "Pe scaunul din dreapta, Ioana își verifică rujul în oglinda parasolarului. Am răspuns amândoi la "
                     "același anunț. Ea zice că e un semn. Eu zic că e o coincidență cu salariu.",
    "intro.counter": "Ioana citește biletul peste umărul meu. \"Pe mine nu mă salută nimeni\", zice. \"Cred că sunt figurantă.\"",
    "night1.open": "Ioana ia comenzile de la mesele din stânga. Trece pe lângă bărbatul la costum fără să-l vadă, "
                   "ca pe un scaun în plus.",
    "night1.phone": "Ioana se uită la telefon, apoi la mine. \"Vocea aia nu știe cum mă cheamă pe mine\", zice încet.",
    "night2.open": "Ioana a venit și ea. Zice că nu-și amintește cum a ajuns acasă aseară, dar banii erau în buzunar.",
    "night3.open": "Ioana își leagă șorțul fără să zică nimic. Azi nu mai glumește.",
    "night3.double": "Ioana se uită de la mine la el și înapoi. Apoi face un pas în față.",
    "ending.sunrise": "Ioana iese în parcare după mine și își aprinde o țigară. \"Data viitoare\", zice, "
                      "\"căutăm un anunț cu salariu mic.\"",
    "ending.replaced": "Ioana vine și în noaptea următoare. Îmi pune un pahar cu apă pe masă, fără să ridice privirea. "
                       "Știe regulile.",
    "ending.captured": "Ioana n-a mai venit. Pe lista de angajați numele ei nu apare nicăieri. Nici n-a apărut vreodată.",
    "ending.secret": "Când caut numele Ioanei pe internet, găsesc un singur rezultat: un anunț de angajare din 1987. "
                     "Același salariu.",
    "ending.cat": "Ioana stă în spate, cu pisica în brațe. Pe ea o lasă. Pe mine nu m-a lăsat niciodată.",
    "ending.quit": "Ioana nu urcă în mașină. Îmi face cu mâna din ușa restaurantului, cu șorțul deja pe ea.",
    "ending.runaway": "Ioana zice că ea mai rămâne o noapte. Doar una.",
}, solo=["intro.office", "intro.handbook", "night1.safe", "night1.knocks", "night1.knocks.quiet", "night1.knocks.voice",
         "night1.knocks.open", "night2.camera", "night3.knocks"])

LOCATIONS = black_pine_locations()
for loc in LOCATIONS:
    add_remarks(loc["examine"], "ioana", {
        "din.jukebox": "Ioana: \"Melodia asta o fredona bunica mea. Și nu cred că a ascultat-o vreodată la radio.\"",
        "din.menu": "Ioana: \"Am lucrat la trei restaurante. Nicio plăcintă cu afine n-a fost vreodată de afine.\"",
        "din.phone": "Ioana: \"Fără 0 și fără 3. Deci nici la 112 nu poți suna. Super loc de muncă.\"",
        "ext.sign": "Ioana: \"NU OPRI. Am oprit. Acum ce facem cu informația asta?\"",
        "ext.car": "Ioana: \"Geaca aia e a mea, de fapt. Ți-am zis că mi-o dai înapoi.\"",
        "kit.fridge": "Ioana: \"NU PENTRU OAMENI. Bine că sunt vegetariană.\"",
        "kit.vent": "Ioana se uită la firul de păr și își strânge mai bine coada. \"Nu e al meu. Sper.\"",
        "off.board": "Ioana: \"Ăsta seamănă cu tine. Și ăsta. Și... ok, toți seamănă cu tine.\"",
        "off.frame": "Ioana: \"Angajatul lunii. Pe mine nu m-ar pune. Nici nu știu dacă m-ar vedea în poză.\"",
    })
for b in BLOCKS:
    add_remarks(b["examine"], "ioana", {
        "cat.1": "Ioana: \"Nu-i da șuncă, că se învață.\" Pisica se uită la ea ca la o trădătoare.",
        "cat.2": "Ioana o scarpină pe pisică după ureche. \"Tu știi tot, nu?\"",
        "cat.3": "Ioana: \"Dacă plecăm, o luăm și pe ea. Nu discut.\"",
        "ic.note": "Ioana: \"Pe mine nu scrie nicăieri. Ce frumos, sunt angajată pe nevăzute.\"",
    })

STORY = {
    "title": "LAST STOP: NIGHT SHIFT",
    "order": 1,
    "author": "Black Pine Diner",
    "description": "Alex acceptă trei ture de noapte la un restaurant izolat în pădure. Salariul e prea bun ca să fie adevărat. "
                   "Uită-te în geamuri, citește regulile și nu-ți spune numele la telefon.",
    "version": 2,
    "startBlockId": "intro.arrival",
    "playerCharacterId": "alex",
    "properties": PROPERTIES,
    "redirects": REDIRECTS,
    "locations": LOCATIONS,
    "playable": PLAYABLE,
    "maxPlayers": 2,
    "characters": CHARACTERS,
    "blocks": BLOCKS,
}
