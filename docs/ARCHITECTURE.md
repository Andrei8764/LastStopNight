# Arhitectura proiectului Last Stop

Documentul e pentru cineva care deschide proiectul prima dată. Explică ce face fiecare parte, cum circulă datele și unde schimbi un lucru anume.

## 1. Pe scurt

Un joc 2D de tip poveste interactivă (C# / .NET 10, WinForms), cu un **Editor** pentru povești și cu **multiplayer P2P**.

- **Povestea e date, nu cod.** Un `story.json` descrie blocurile (scenele), deciziile, condițiile, efectele, locațiile 2D și personajele. Imaginile stau lângă el într-un ZIP.
- **Motorul e determinist.** Aceeași poveste, aceeași echipă și aceleași acțiuni dau mereu aceeași stare. Pe asta se bazează:
  - salvarea (se salvează doar lista de acțiuni);
  - multiplayer-ul (se trimit doar acțiunile);
  - testele (se compară amprente de stare).
- **Grafica și sunetele sunt generate procedural** de scripturi Python. Nu există imagini desenate de mână sau sunete înregistrate, iar regenerarea dă aceleași fișiere, bit cu bit.

## 2. Proiectele

```
LastStopNight.slnx
├─ LastStopNight.Core/        bibliotecă fără interfață: modelul poveștii, motorul, parsere, arhive, validare
├─ LastStopNight.Net/         multiplayer P2P: protocol, conexiuni TCP, sesiune (gazdă / client), coduri, LAN, UPnP
├─ LastStopNight.Audio/       sunetul: citirea WAV, mixerul software, ieșirea la placa de sunet (winmm), fără dependințe
├─ LastStopNight/             jocul (WinForms): scena 2D, HUD, meniuri, multiplayer în interfață
├─ LastStopNight.Editor/      editorul de povești (WinForms)
├─ tools/
│  ├─ StoryCheck/             verificarea automată (povești, nucleu, sunet, rețea); rulează înainte de orice publicare
│  ├─ art/                    grafica procedurală (Python + Pillow)
│  ├─ sound/                  sunetele procedurale (Python, doar biblioteca standard)
│  ├─ build_sounds.py         sunetele → LastStopNight/Assets/sounds/*.wav
│  ├─ build_voice.py          vocea naratorului (edge-tts + ffmpeg) → voice/ din arhivele poveștilor
│  ├─ stories/                poveștile scrise în Python (generează story.json)
│  └─ build_story.py          imagini → story.json → arhivele ZIP din LastStopNight/Assets/stories
├─ stories/                   ieșirea lui build_story.py: _art/images (toate imaginile) și <poveste>/story.json
├─ docs/                      documentație și capturi
├─ Directory.Build.props      setări comune tuturor proiectelor C# (nullable, versiune, avertismente)
├─ .editorconfig              stilul codului (citit de IDE și de `dotnet format`)
└─ build_release.ps1          verificare + exe-ul de trimis (dist/LastStopNight.exe)
```

**Dependențele merg într-o singură direcție:** `Core` ← `Net` ← `LastStopNight`, iar `Audio` (care nu depinde de nimic) ← `LastStopNight`. `Editor` depinde doar de `Core`; `StoryCheck` de `Core`, `Net` și `Audio`. `Core` nu știe nimic de WinForms, rețea sau sunet.

## 3. LastStopNight.Core

| Dosar | Ce conține |
|---|---|
| `Model/` | Forma unei povești, 1:1 cu `story.json`: `StoryDefinition` (rădăcina), `StoryBlock`, `DecisionDefinition`, `ConditionDefinition` (COMPARISON / AND / OR, polimorfic), `EffectDefinition`, `StatePropertyDefinition`. Pentru scena 2D: `LocationDefinition`, `ExamineDefinition`, `CharacterDefinition`, `PlayableDefinition`, `SceneObjects` (decor, lumini). |
| `Engine/` | `GameEngine` aplică acțiunile (`ChooseAction`, `ExamineAction`, `PartyAction`) și evaluează condițiile, efectele și redirecționările. Ține jurnalul de acțiuni și drumul parcurs (`Path`) și calculează amprenta stării (`StateHash`). `RouteTree` pune drumurile într-un arbore, pentru hartă. `GameState` păstrează valorile, mereu limitate la [min, max]. `IGameSession` / `LocalSession` sunt calea prin care interfața cere acțiuni. |
| `Expressions/` | `ConditionParser` / `EffectParser` transformă text ↔ obiecte, pentru Editor (`story.evidence >= 2 AND player.life > 0`). |
| `IO/` | `StoryPackage` (povestea + fișierele ei, în memorie). `StoryArchive` încarcă și salvează ZIP sau folder și calculează amprenta poveștii (SHA-256). |
| `Validation/` | `StoryValidator` semnalează destinații lipsă, chei necunoscute, imagini lipsă, fundături și blocuri la care nu se poate ajunge. |

### Cum avansează povestea

```
interfața ── RequestChoose / RequestExamine ──> IGameSession
                                                   │  LocalSession: aplică imediat
                                                   │  NetSession:   gazda ordonează, apoi toți aplică
                                                   ▼
                                     GameEngine.Apply(acțiune)
                                       ├─ efecte → GameState (limitat la [min, max]) → evenimentul StateChanged
                                       ├─ redirecționări automate (ex. viață 0 → final)
                                       └─ blocul nou → evenimentul BlockChanged → interfața construiește scena
```

## 4. LastStopNight.Net (multiplayer)

- **`Protocol.cs`:** mesajele (JSON, câte unul pe linie, cu discriminatorul `t`) și versiunea protocolului.
- **`Connection.cs`:** o conexiune TCP.
  - Citirea și scrierea merg pe fire separate.
  - Mesajele primite așteaptă într-o coadă.
  - La fiecare 2 secunde pleacă un semnal de viață.
- **`NetSession`:** sesiunea, împărțită în cinci fișiere:
  - `NetSession.cs`: starea comună, bucla `Pump()` și acțiunile jucătorului local;
  - `NetSession.Host.cs`: lobby, voturi, ordonarea acțiunilor, resincronizare;
  - `NetSession.Client.cs`: aplicarea acțiunilor, amprenta după fiecare;
  - `NetSession.Timelines.cs`: drumurile separate. Fiecare drum are motorul lui. Când cineva alege și pleacă, ceilalți rămân pe un drum nou (`ForkMsg` + `PartyAction`, care schimbă echipa). Trecerea pe drumul altcuiva sau privitul după final: `JoinMsg`. Motorul local rămâne mereu același obiect;
  - `NetSession.Free.cs`: modul „fiecare alege” (plecarea imediată, `Follow`, `RequestJoin`) și indiciile puse la comun (`ShareAction` pentru proprietățile cu `shared: true`).
- **`JoinCode`, `NetInfo`, `LanDiscovery`, `PortMapping`:** codul de intrare, adresele locale și virtuale, anunțul în rețeaua locală și UPnP pe router.

**Regula de aur:** logica de rețea rulează doar în `NetSession.Pump()`, chemat de joc pe firul interfeței, deci nu există cod de joc care rulează în paralel. Firele de rețea doar pun mesaje în cozi.

## 4b. LastStopNight.Audio (sunetul)

- **`WavClip`:** citește un WAV PCM pe 16 biți (mono / stereo, orice frecvență) și îl aduce la mono, 22 050 Hz.
- **`SoundBank`:** sunetele după nume, dintr-un dosar (`Assets/sounds` lângă exe) sau din resursele exe-ului publicat (`sounds/*.wav`).
- **`Mixer`:** pur, testabil fără placă de sunet. Voci cu volum, viteză, buclă și fade; trei canale (efecte, ambianță, muzică) cu volumul lor; suma e limitată la [-1, 1]; cel mult 24 de voci.
- **`WaveOutPlayer`:** 6 buffere mici (~23 ms) trimise la placa de sunet prin winmm, pe un fir separat. Fără placă de sunet, jocul rămâne mut, fără erori.
- **`AudioEngine`:** ce folosește jocul: `Play` (efecte), `SetAmbient` (buclele locației, cu trecere lină între locații), `SetMusic`, `StopLoops`.

**Cum ajung sunetele în joc:** scena și efectele nu știu de sunet; ele cer un sunet după nume (`Scene.PlaySound`), iar `PlayerForm.Audio.cs` le redă la fiecare cadru. Ambianța vine din date: `LocationDefinition.Sounds` (ex. `rain`, `wind`, `hum`) plus o dronă de tensiune în scenele `dark` / `red`, după regulile din `Core/Model/Soundscape.cs`.

## 5. LastStopNight (jocul)

| Fișier / dosar | Responsabilitate |
|---|---|
| `Program.cs` | Pornirea, prinderea erorilor (crash.log), modurile de dezvoltare. |
| `PlayerForm.cs` | Fereastra: structura (scenă, HUD, panou), bucla de joc, desenarea. |
| `PlayerForm.Story.cs` | Catalogul, sesiunea, intrarea în blocuri, deciziile, examinările, salvarea. |
| `PlayerForm.Input.cs` | Tastatura și mouse-ul. |
| `PlayerForm.Menus.cs` | Meniurile single player (principal, povești, însoțitor, setări, finaluri, pauză). |
| `PlayerForm.Multiplayer.cs` | Găzduire, intrare, lobby, sincronizarea în joc. |
| `PlayerForm.DevTools.cs` | Rulări automate cu capturi (`--snapshots`, `--mp-test`). |
| `PlayerForm.Map.cs` | Harta turei (tasta H): drumurile pentru `RouteMapView`, în single player și în multiplayer. |
| `PlayerForm.Audio.cs` | Sunetul: efectele cerute de scenă, ambianța locației, muzica meniului, temele finalurilor, volumele din Setări. |
| `PlayerForm.Voice.cs` | Naratorul (vocea fiecărui bloc, din `voice/` al poveștii) și replicile din text puse în bule (`Game/DialogueLines`), sincronizate cu vocea. |
| `PlayerForm.Routes.cs` | Multiplayer: „Mergi după...” (tasta F), trecerea pe alt drum, privitul după final și ecranul de concluzii (`ConclusionView`). |
| `Game/` | `Scene` (scena 2D a unui bloc: actori, decor, lumini, marcaje, prim-plan). `StoryCatalog` (de unde vin poveștile). `SaveGame`, `GameSettings`, `AppData`, `EndingsTracker` (fișierele din %AppData%). |
| `Actors/` | `Actor` → `PlayerActor` (tastatură / click), `NpcActor`, `FollowerActor` (însoțitorul), `RemoteActor` (celălalt jucător). |
| `Effects/` | `SceneEffect` → atmosferă (ploaie, praf, ceață, pâlpâit), feedback (tremur, flash, text plutitor, telefon), prezentare (fade, bule, carduri de capitol). |
| `Rendering/` | `GameCanvas` (lumea 320x180 scalată), `HudRenderer`, `ImageCache`, `SpriteSheet`, `FontCache`, `Gfx` (pensule refolosite), `Portraits`, `Screen2D`. |
| `Ui/` | Meniurile desenate în scenă (`MenuScreen`, `MenuStyle`), controalele panoului de jos (`StoryTextPanel`, `ChoiceButton`), `Overlays` (banda de final, de vot, filtrul VHS). |
| `Ui/Views/` | Ecranele cu cartonașe: poveștile, însoțitorul, galeria de finaluri, lobby-ul, harta turei (`RouteMapView`), concluziile turei (`ConclusionView`). Sunt funcții de desenare pure, care primesc datele ca parametri. |

**De ce `PlayerForm` e `partial`:** fereastra coordonează totul (o singură stare: modul, scena, sesiunea), dar fiecare fișier are o singură temă. Desenarea ecranelor e scoasă în `Ui/Views`, ca să poată fi citită fără restul.

## 6. Editorul

- **`EditorForm`:** arborele poveștii, fișierele, validarea, „Testează în Player”.
- **`EditorForm.SelfTest.cs`:** autotestul (`--selftest`), care verifică că vizitarea tuturor paginilor nu modifică povestea.
- **`EditorContext`:** povestea deschisă și cache-ul de imagini.
- **`Ui`:** fabrica de controale cu stilul comun.
- **`Pages/`:** câte o pagină pe nod din arbore: poveste, proprietăți, redirecționări, locații, personaje, imagini, bloc.

## 7. Grafica și poveștile (Python)

- **`tools/art/`:** un modul pe temă:
  - `pixel` (unelte de desen comune);
  - `diner`, `gas_station`, `endings`, `closeups`, `decor`, `characters`, `props`, `icons`.

  `art/__init__.py` conține **catalogul**: ce fișier produce fiecare funcție.
- **`tools/stories/`:**
  - `common.py` e mini-limbajul (`block`, `dec`, `look`, `cmp`...), care produce exact structura din story.json;
  - `black_pine.py` conține locațiile comune pentru Night Shift și 1987;
  - câte un fișier pe poveste.
- **`tools/build_story.py`:** desenează imaginile, scrie `stories/<id>/story.json` și face arhivele din `LastStopNight/Assets/stories/`.
- **`tools/sound/`:** `synth.py` (oscilatoare, zgomot, filtre, anvelope, ecou) și catalogul din `__init__.py`. Buclele (`amb_*`, `music_*`) se construiesc circular, deci se leagă fără pocnituri. `tools/build_sounds.py` scrie fișierele în `LastStopNight/Assets/sounds/`.

> Dacă ai modificat o poveste din Editor, nu mai rula `build_story.py` pentru ea: rescrie `story.json` din Python.

## 8. Configurare și variabile de mediu

| Ce | Unde |
|---|---|
| Setările jucătorului, salvarea, finalurile găsite, crash.log | `%AppData%\LastStopNight\` |
| `LASTSTOP_DATA` | Alt dosar pentru fișierele de mai sus (testele automate nu ating salvările reale). |
| `LASTSTOP_LOOPBACK=1` | Multiplayer doar pe acest PC (fără firewall, fără router). |
| `LASTSTOP_NOUPNP=1` | Gazda nu încearcă să deschidă portul pe router. |
| `LASTSTOP_MUTE=1` | Sunetul nu ajunge la difuzoare (modurile `--snapshots` și `--mp-test` îl pun singure). |
| `LASTSTOP_JOIN_CODE`, `LASTSTOP_JOIN_LAN=1` | Doar pentru `--mp-test join`: cu ce cod intră sau dacă alege jocul din rețeaua locală. |
| `LASTSTOP_UISCALE=1.5` | Simulează scalarea din Windows (150%) pentru panoul de jos, butoane și HUD, ca să vezi jocul ca pe un laptop cu ecran dens. |
| `LASTSTOP_MP_SPLIT=1` | Pentru `--mp-test` (ambele ferestre): „fiecare alege”; la prima decizie aleg diferit, fiecare își joacă drumul, cine termină primul se uită la celălalt, apoi amândoi văd concluziile. Fără ea, testul folosește votul comun. |
| Porturi | TCP 27013 (sau următorul liber) pentru joc, UDP 27014 pentru anunțul în rețeaua locală. |

## 9. Verificare

```powershell
dotnet build LastStopNight.slnx
```

```powershell
dotnet run --project tools/StoryCheck
```

```powershell
dotnet run --project tools/StoryCheck -- --probe
```

```powershell
LastStopNight.Editor\bin\Debug\net10.0-windows\LastStopNight.Editor.exe --selftest <dir>
```

```powershell
LastStopNight\bin\Debug\net10.0-windows\LastStopNight.exe --snapshots <dir> 1:vlad 0,1,0
```

```powershell
python -m pyflakes tools
```

```powershell
dotnet format LastStopNight.slnx --verify-no-changes --diagnostics IDE0005
```

- **`dotnet build`:** compilează toată soluția (trebuie 0 avertismente).
- **`StoryCheck`:** poveștile, nucleul, sunetul (fișierele, mixerul, numele folosite) și multiplayer-ul pe localhost.
- **`StoryCheck -- --probe`:** doar citește rețeaua (IP, Tailscale, UPnP) și nu deschide porturi; verifică și placa de sunet (un clopoțel scurt).
- **Editorul cu `--selftest`:** vizitează toate paginile și confirmă că nu se pierde nimic.
- **Jocul cu `--snapshots`:** joacă singur și face capturi.
- **`pyflakes`:** cod Python nefolosit sau greșit.
- **`dotnet format`:** using-urile nefolosite.
