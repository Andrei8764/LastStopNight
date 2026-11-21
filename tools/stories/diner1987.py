"""BLACK PINE: 1987. Prequel. Prima tura a lui Dan, cu proprietarul inca la tejghea."""
from common import add, add_companion, block, char, cmp, dec, look, npc, obj, playable, prop, setv
from black_pine import black_pine_locations

ID = "diner1987"

PROPERTIES = [
    prop("player.nerve", 0, 100, 70, "Sânge rece", "bar", "bolt", "#E8C440"),
    prop("inventory.money", 0, 500, 0, "Bacșiș", "value", "coin", fmt="${0}"),
    prop("story.clues", 0, 4, 0, "Ce știi", "value", "clue", fmt="{0}/4", shared=True),
    prop("story.signed", 0, 1, 0, "Contract", "flag", "book"),
    prop("story.fakeName", 0, 1, 0, "Nume fals", "hidden"),
    prop("story.page", 0, 1, 0, "Pagina", "hidden"),
]

REDIRECTS = [
    {"condition": cmp("player.nerve", "<=", 0), "target": "d87.end.flash", "description": "Îți cedează nervii"},
]

CHARACTERS = [
    char("dan", "Dan", desc="Ospătar nou, 19 ani, 1987."),
    char("owner", "Proprietarul", has_reflection=False),
    char("red_woman", "Femeia în roșu"),
    char("patron", "Camionagiul"),
    char("dancer_m", "Perechea de la tonomat (el)"),
    char("dancer_f", "Perechea de la tonomat (ea)"),
    char("waiter", "Ceilalți ospătari", has_reflection=False),
    char("lucia", "Lucia", desc="Ospătăriță, 18 ani, angajată în aceeași zi cu Dan."),
]

PLAYABLE = [
    playable("dan", "Ospătar nou, 19 ani", "Are o Dacie roșie legată cu sârmă și nevoie de bani. Citește scrisul mic."),
    playable("lucia", "Ospătăriță, prima zi", "Dansează cu tava printre mese. Zâmbește perfect la poze. Prea perfect."),
]

DANCERS = [npc("dancer_m", 146), npc("dancer_f", 162, True)]

BLOCKS = [
    block("d87.start", "Prima zi",
          "Octombrie 1987. Am 19 ani, o Dacie roșie cu ușa din dreapta legată cu sârmă și un anunț decupat din "
          "ziar: \"Ospătar, tura de noapte, plata foarte bună\".\n\n"
          "Proprietarul mă așteaptă în ușă. Șapca trasă pe ochi, șorț alb, mâini mari. \"Tu ești băiatul nou. "
          "Intră, că se răcește cafeaua.\"",
          location="exterior", time="21:30", player_x=262, mood="sepia", chapter="1987: Prima zi",
          characters=[npc("owner", 203, True)],
          decisions=[dec("Intră după el", "d87.contract", hotspot=203, hy=110)]),
    block("d87.contract", "Contractul",
          "În birou mirosul de hârtie e nou, nu vechi. Pe perete nu e încă nicio poză. Proprietarul pune în fața "
          "mea un contract și un pix.\n\n\"Semnezi aici. Cu numele întreg. Restaurantul ține minte numele.\"",
          location="office87", time="21:40", player_x=110, mood="sepia", closeup="images/closeups/contract.png",
          characters=[npc("owner", 180, True)],
          decisions=[dec("Semnează repede", "d87.shift", [setv("story.signed", 1)]),
                     dec("Citește tot, inclusiv scrisul mic", "d87.fineprint", [add("story.clues", 1)])]),
    block("d87.fineprint", "Scrisul mic",
          "Clauza 13, scrisă atât de mic încât trebuie să mă aplec: \"Angajatul rămâne în serviciul restaurantului "
          "până când acesta găsește un înlocuitor cu același nume.\"\n\n"
          "Proprietarul nu zice nimic. Doar bate cu degetul în masă. Tic. Tic. Tic.",
          location="office87", time="21:45", player_x=110, mood="sepia", characters=[npc("owner", 180, True)],
          decisions=[dec("Semnează oricum, ai nevoie de bani", "d87.shift", [setv("story.signed", 1), add("player.nerve", -10)]),
                     dec("Semnează cu alt nume", "d87.shift", [setv("story.signed", 1), setv("story.fakeName", 1)]),
                     dec("Pune pixul jos și pleacă", "d87.end.road")]),
    block("d87.shift", "Sala plină",
          "La 11 sala e plină. Camionagii, o pereche care dansează lângă tonomat, fum de țigară. Proprietarul "
          "stă la tejghea și glumește cu o femeie într-o rochie roșie. Ea râde și îmi face cu ochiul.\n\n"
          "Nimeni nu pare să se teamă de nimic. Asta mă sperie un pic.",
          location="dining", time="23:00", player_x=40, mood="sepia",
          characters=[npc("patron", 70), npc("owner", 268, True), npc("red_woman", 236)] + DANCERS,
          examine=[look("d87.woman", 236, "Femeia în roșu",
                        "\"Ești nou\", zice. \"Pari de treabă. Ascultă-mă: nu-l lăsa să-ți facă poză.\" Apoi râde, "
                        "ca și cum ar fi fost o glumă.", effects=[add("story.clues", 1)]),
                   look("d87.trucker", 70, "Camionagiul",
                        "\"Vin aici de zece ani\", zice. \"Ospătarii se schimbă. Patronul niciodată. Nici măcar nu "
                        "îmbătrânește, ce zici de asta?\"", effects=[add("inventory.money", 5)])],
          decisions=[dec("Servește cafea la toată lumea", "d87.night", [add("inventory.money", 12)]),
                     dec("Uită-te în geam la toată sala", "d87.mirror", [add("story.clues", 1)], hotspot=120, hy=60)]),
    block("d87.mirror", "Geamul",
          "În geam se văd toți: camionagiul, perechea, femeia în roșu, eu cu tava. Proprietarul nu.\n\n"
          "Mă întorc. El e acolo, la tejghea, și se uită fix la mine. Își duce un deget la buze.",
          location="dining", time="23:20", player_x=120, mood="sepia",
          characters=[npc("patron", 70), npc("owner", 268, True), npc("red_woman", 236)] + DANCERS,
          decisions=[dec("Fă-te că n-ai văzut nimic", "d87.night", [add("player.nerve", -15)])]),
    block("d87.night", "Registrul",
          "Pe la 2, în bucătărie, proprietarul scrie ceva într-un registru gros, legat în piele. Un brad pe copertă.\n\n"
          "\"Reguli\", zice fără să ridice ochii. \"Pentru cei care vin după tine. Restaurantul are nevoie de "
          "cineva care să-l țină minte.\"",
          location="kitchen", time="02:00", player_x=60, mood="sepia", characters=[npc("owner", 200, True)],
          decisions=[dec("Întreabă-l pentru cine", "d87.why"),
                     dec("Rupe ultima pagină cât nu se uită", "d87.page", [setv("story.page", 1), add("story.clues", 1)], hotspot=190),
                     dec("Lucia îl ține de vorbă, tu citești registrul", "d87.lucia", [add("story.clues", 1)], requires="lucia")]),
    block("d87.lucia", "Plăcinta cu afine",
          "Lucia îl întreabă pe proprietar de rețeta plăcintei cu afine. El lasă pixul jos și începe să povestească, "
          "cu mâinile, ca un bunic.\n\n"
          "Eu citesc registrul pe furiș. Prima regulă e scrisă cu cerneală proaspătă: DACĂ UN CLIENT NU APARE ÎN GEAM... "
          "Regulile sunt pentru Alex. Dar scrisul de mână e al meu.",
          location="kitchen", time="02:04", player_x=180, mood="sepia", characters=[npc("owner", 120)],
          decisions=[dec("Închide registrul fără zgomot", "d87.photo")]),
    block("d87.why", "Pentru cine",
          "\"Pentru Alex\", zice. \"Vine un Alex mereu. Peste un an, peste zece, peste treizeci. Și o să aibă "
          "nevoie de reguli.\"\n\n\"Eu nu mă cheamă Alex\", zic. Zâmbește sub șapcă. \"Încă.\"",
          location="kitchen", time="02:05", player_x=150, mood="sepia", characters=[npc("owner", 200, True)],
          decisions=[dec("Înapoi la treabă", "d87.photo", [add("player.nerve", -10)])]),
    block("d87.page", "Ultima pagină",
          "Pagina se rupe aproape fără zgomot. O bag în buzunar. Pe ea e un singur rând, scris de altă mână, "
          "mai tremurată:\n\n\"NU LĂSA PE NIMENI SĂ-ȚI FACĂ POZĂ.\"\n\n"
          "Deci de-aia lipsește ultima pagină.",
          location="kitchen", time="02:06", player_x=190, mood="sepia", characters=[npc("owner", 120)],
          decisions=[dec("Ascunde pagina și zâmbește", "d87.photo")]),
    block("d87.photo", "Poza pentru perete",
          "Dimineața, proprietarul ne strânge pe toți în fața firmei. Are un aparat foto vechi, cu bliț. "
          "\"Poza pentru perete\", zice. \"Toată echipa. Zâmbiți.\"\n\n"
          "Ceilalți patru ospătari se aliniază. Au fețe obosite și ochi foarte calmi. Locul al treilea din stânga e gol.",
          location="exterior_dawn", time="05:50", player_x=262, mood="sepia",
          # patru ospatari la rand; al treilea loc din stanga e gol, iar proprietarul tine aparatul
          characters=[npc("owner", 112), npc("waiter", 144, True), npc("waiter", 161, True),
                      npc("waiter", 199, True), npc("waiter", 216, True)],
          props=[obj("photo_camera", 117, 134, front=True)],
          decisions=[dec("Zâmbește pentru poză", "d87.end.flash", condition=cmp("story.fakeName", "==", 0),
                         hotspot=180, show_locked=False),
                     dec("Zâmbește pentru poză", "d87.end.alex", condition=cmp("story.fakeName", "==", 1),
                         hotspot=180, show_locked=False),
                     dec("Ascunde-te după mașină", "d87.end.road", condition=cmp("story.page", "==", 1),
                         hint="nu ai niciun motiv să fugi", hotspot=290, hy=142),
                     dec("Ia tu aparatul și fă poza", "d87.end.owner", condition=cmp("story.clues", ">=", 3),
                         hint="nu știi destul (3)", hotspot=124, hy=138)]),
    block("d87.end.road", "Drumul spre casă",
          "Mă bag în Dacie și pornesc. Blițul se declanșează în spatele meu, alb, de trei ori, dar eu sunt deja "
          "pe drum.\n\n"
          "Peste treizeci de ani o să văd o poză veche, cu un loc gol pe rândul de ospătari, și o să știu exact "
          "cui i-a fost păstrat.",
          background="images/endings/d87_road.png", time="06:00", ending="good"),
    block("d87.end.flash", "Pe perete",
          "Zâmbesc. Blițul e mai alb decât orice. Când pot să văd din nou, sunt în ramă, pe peretele din birou, "
          "cu ceilalți.\n\nDe aici se vede toată sala. E chiar plăcut. Doar că nu mai pot să clipesc.",
          background="images/endings/d87_flash.png", time="05:51", ending="bad"),
    block("d87.end.alex", "ALEX",
          "Zâmbesc. Blițul. Pe ecusonul meu, unde scria Dan, acum scrie numele pe care l-am inventat în contract: "
          "ALEX.\n\n"
          "Restaurantul a ținut minte numele. O să-l aștepte. Un an, zece, treizeci și ceva. Până într-o noapte "
          "ploioasă, când un tip pe nume Alex Morgan o să parcheze în fața firmei roșii.",
          background="images/endings/d87_flash.png", time="05:51", ending="secret"),
    block("d87.end.owner", "Noul proprietar",
          "Iau aparatul din mâinile lui. Nu se opune. Pare chiar ușurat. Fac poza: patru ospătari și un loc gol.\n\n"
          "Proprietarul își scoate șapca și mi-o pune pe cap. \"Acum tu ții minte\", zice, și pleacă pe jos în "
          "pădure. Din seara asta, eu scriu regulile.",
          background="images/endings/d87_owner.png", time="06:00", ending="alternative"),
]

# ------------------------------------------------------------------ Lucia, insotitoarea
add_companion(BLOCKS, "lucia", texts={
    "d87.start": "Lângă Dacie așteaptă o fată cu părul cârlionțat și o geantă prea mare: Lucia, angajată tot azi, tot din "
                 "anunț. \"Tu ești Dan? Super. Măcar nu sunt singura nouă.\"",
    "d87.contract": "Lucia semnează înaintea mea, repede, fără să citească. Proprietarul se uită la semnătura ei și zâmbește.",
    "d87.shift": "Lucia dansează cu tava printre mese. Camionagiii o adoră deja.",
    "d87.night": "Lucia spală pahare și se uită la registru peste umăr, fără să întoarcă capul.",
    "d87.photo": "Lucia își aranjează părul și se pune la capătul rândului. Lângă ea mai e un loc gol. Doi, de fapt.",
    "d87.end.flash": "În ramă, lângă mine, Lucia zâmbește perfect. Ea a știut mereu să zâmbească la poze.",
    "d87.end.road": "Lucia e pe scaunul din dreapta. A sărit în mașină în ultima secundă, cu tava încă în mână.",
    "d87.end.alex": "Pe ecusonul Luciei acum scrie IOANA. N-am idee de ce. Restaurantul are planurile lui.",
    "d87.end.owner": "Lucia rămâne cu mine. Cineva trebuie să facă plăcinta cu afine.",
})
for b in BLOCKS:
    for e in b["examine"]:
        if e["id"] == "d87.woman":
            e["textBy"]["lucia"] = ("Femeia în roșu se uită la Lucia: \"Tu ai ochi de om care pleacă la timp. "
                                    "Pleacă la timp, puiule.\"")
        if e["id"] == "d87.trucker":
            e["textBy"]["lucia"] = "Camionagiul îi face cu ochiul Luciei: \"Să nu te lași pozată, auzi?\""

STORY = {
    "title": "BLACK PINE: 1987",
    "order": 3,
    "author": "Înainte de tot",
    "description": "Cu 39 de ani înainte de Alex, un băiat pe nume Dan își începe prima tură la Black Pine Diner. "
                   "Proprietarul e încă acolo, registrul e încă gol, iar pe perete nu e nicio poză. Deocamdată.",
    "version": 1,
    "startBlockId": "d87.start",
    "playerCharacterId": "dan",
    "properties": PROPERTIES,
    "redirects": REDIRECTS,
    "locations": black_pine_locations(examine=False),
    "playable": PLAYABLE,
    "maxPlayers": 2,
    "characters": CHARACTERS,
    "blocks": BLOCKS,
}
