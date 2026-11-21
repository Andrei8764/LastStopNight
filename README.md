# Last Stop

Joc 2D de tip poveste interactivă, cu 3 povești de groază de tura de noapte, însoțitori, editor de povești și multiplayer în doi (P2P). C# / .NET 10, WinForms.

## Pornire rapidă

Ai nevoie de .NET SDK 10. Python 3 cu Pillow îți trebuie doar dacă regenerezi grafica.

**Jocul:**

```powershell
dotnet run --project LastStopNight
```

**Editorul de povești:**

```powershell
dotnet run --project LastStopNight.Editor
```

**Verificarea completă:** poveștile, nucleul, sunetul și multiplayer-ul pe localhost.

```powershell
dotnet run --project tools/StoryCheck
```

**Exe-ul de trimis unui prieten** (`dist\LastStopNight.exe`):

```powershell
powershell -ExecutionPolicy Bypass -File build_release.ps1
```

**Regenerarea graficii, a story.json-urilor și a arhivelor:**

```powershell
python tools/build_story.py
```

**Regenerarea sunetelor** (doar Python 3, fără pachete):

```powershell
python tools/build_sounds.py
```

**Vocea naratorului** (după `build_story.py`; cere `pip install edge-tts`, ffmpeg și internet doar pentru textele noi):

```powershell
python tools/build_voice.py
```

Vocile neurale românești (Emil, Alina) se generează o singură dată și se pun în arhivele poveștilor. Jocul nu are nevoie de internet.

## Documentație

| Document | Despre |
|---|---|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Cum e organizat codul, ce face fiecare parte, configurare, verificare. |
| [PLAN.md](PLAN.md) | Planul inițial, ce s-a implementat în fiecare versiune, formatul story.json, controale. |
| [docs/MULTIPLAYER.md](docs/MULTIPLAYER.md) | Cum joci în doi (găzduire, cod, probleme de conectare). |
| [docs/PLAN_V4_MULTIPLAYER.md](docs/PLAN_V4_MULTIPLAYER.md) | Proiectarea meniurilor, a însoțitorilor și a multiplayer-ului. |
| [docs/REVIEW.md](docs/REVIEW.md) | Review-ul poveștilor și al graficii. |
