using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Game;
using LastStopNight.Rendering;
using LastStopNight.Ui;
using LastStopNight.Ui.Views;
using static LastStopNight.Ui.MenuStyle;
using MenuItem = LastStopNight.Ui.MenuItem;

namespace LastStopNight;

/// <summary>
/// Meniurile single player, desenate peste scena: principal, povesti, insotitor, setari, finaluri, pauza.
/// </summary>
public sealed partial class PlayerForm
{
    private MenuScreen? _menu;

    /// <summary>Imaginile pentru miniaturile povestilor (incarcate o singura data).</summary>
    private readonly Dictionary<StoryPackage, ImageCache> _thumbs = new();

    private Action _menuBack = () => { };

    private bool _paused;

    /// <summary>Meniul care primeste tastele: in ecranele de meniu sau in pauza.</summary>
    private MenuScreen? ActiveMenu => _mode == Mode.Menu || _paused ? _menu : null;

    private void OpenMenu(MenuScreen m, bool overGame = false)
    {
        m.EnsureValid();
        m.Sound ??= PlayMenuSound;
        _menu = m;
        if (overGame) return;
        _showMap = false;
        UseMenuSoundscape();
        _paused = false;
        _mode = Mode.Menu;
        SetLayout(playing: false);
        if (_scene is null || _scene.Block.Id != "__menu") BuildBackdrop();
    }

    private void ShowMainMenu()
    {
        var m = new MenuScreen("LAST STOP") { Subtitle = "povești din tura de noapte" };
        var save = SaveGame.Load();
        if (save is not null && _catalog.FindByTitle(save.StoryTitle) is not null && save.Actions.Count > 0)
            m.Items.Add(new MenuItem("Continuă", () => ContinueSaved(save))
            {
                Hint = $"{save.StoryTitle}, {save.BlockTitle} ({save.Time}). Salvat pe {save.SavedAt:dd.MM, HH:mm}.",
            });
        m.Items.Add(new MenuItem("Joc nou", ShowStories) { Hint = "Alegi povestea, apoi cu cine faci tura." });
        m.Items.Add(new MenuItem("Multiplayer", ShowMultiplayer) { Hint = "În doi, fiecare cu personajul lui. Fără server: unul găzduiește." });
        m.Items.Add(new MenuItem("Finaluri găsite", ShowEndings)
        {
            Hint = $"Ai găsit {_catalog.Stories.Sum(p => _endings.Count(p.Story.Title))} din " +
                   $"{_catalog.Stories.Sum(p => p.Story.Blocks.Count(b => b.IsEnding))} finaluri.",
        });
        m.Items.Add(new MenuItem("Setări", () => ShowSettings(ShowMainMenu)));
        m.Items.Add(new MenuItem("Ieșire", Close));
        OpenMenu(m);
    }

    /// <summary>Un mesaj simplu (ex. „gazda a iesit”) cu un singur buton.</summary>
    private void ShowNotice(string title, string text, Action back)
    {
        var m = new MenuScreen(title) { Back = back };
        m.Items.Add(new MenuItem("Înapoi", back));
        m.Draw = (g, view, s, hits) =>
        {
            Shade(g, view, 0.8f);
            float top = Logo(g, view, s, title, null, m.Time);
            Wrap(g, text, Body(s, 6.6f), Ink, R(view, s, 24, top + 4, 210, 90));
            BackButton(g, view, s, hits, 20, 150, 60);
        };
        OpenMenu(m);
    }

    /// <summary>Miniatura unei povesti: fundalul blocului de start (imaginile se incarca o singura data).</summary>
    private Bitmap? Thumbnail(StoryPackage pkg)
    {
        if (!_thumbs.TryGetValue(pkg, out var cache)) _thumbs[pkg] = cache = new ImageCache(pkg);
        var story = pkg.Story;
        return cache.Get(story.ResolveBackground(story.FindBlock(story.StartBlockId) ?? new StoryBlock()));
    }

    private void ShowStories() => ShowStories(host: false);

    /// <param name="host">Alegerea povestii pentru o tura gazduita (multiplayer).</param>
    private void ShowStories(bool host)
    {
        var m = new MenuScreen(host ? "Ce găzduiești?" : "Alege povestea") { Horizontal = true, Back = host ? ShowMultiplayer : ShowMainMenu };
        foreach (var p in _catalog.Stories)
        {
            m.Items.Add(new MenuItem(p.Story.Title, () => { Select(p); if (host) HostGame(p); else ShowCharacters(); }));
        }
        if (!host) m.Items.Add(new MenuItem("Din fișier...", OpenStoryDialog));
        m.Selected = Math.Max(0, _catalog.IndexOf(_package));
        m.SelectionChanged = i => { if (i < _catalog.Count) Select(_catalog[i]); };
        m.Draw = (g, view, s, hits) => StoryPickerViews.DrawStoryCards(g, view, s, hits, m, _catalog.Stories, Thumbnail, _endings);
        OpenMenu(m);
    }

    private void ShowCharacters()
    {
        if (_package is null) return;
        var story = _package.Story;
        var companions = story.Companions.ToList();
        if (companions.Count == 0) { StartGame([]); return; }

        var m = new MenuScreen("Cu cine faci tura?") { Horizontal = true, Back = ShowStories };
        m.Items.Add(new MenuItem("Singur", () => StartGame([])));
        foreach (var c in companions)
        {
            var id = c.CharacterId;
            m.Items.Add(new MenuItem(story.FindCharacter(id)?.Name ?? id, () => StartGame([id])));
        }
        m.Selected = _companions.Count > 0 ? Math.Max(0, companions.FindIndex(c => c.CharacterId == _companions[0]) + 1) : 0;
        m.Draw = (g, view, s, hits) => StoryPickerViews.DrawCompanionChoice(g, view, s, hits, m, story, companions, _images);
        OpenMenu(m);
    }

    private void ShowSettings(Action back)
    {
        _menuBack = back;
        bool overGame = _paused;
        var m = new MenuScreen("Setări") { Back = back };
        void Changed() => _settings.Save();
        m.Items.Add(new MenuItem("Ecran complet")
        {
            Value = () => _settings.Fullscreen ? "Da" : "Nu",
            Adjust = _ => ToggleFullscreen(),
            Hint = "Tasta F11 face același lucru.",
        });
        m.Items.Add(new MenuItem("Viteza textului")
        {
            Value = () => GameSettings.Label(_settings.TextSpeed),
            Adjust = d =>
            {
                _settings.TextSpeed = (TextSpeed)(((int)_settings.TextSpeed + d + 4) % 4);
                _text.CharsPerSecond = _settings.CharsPerSecond;
                Changed();
            },
            Hint = "Cât de repede apare textul scenei.",
        });
        m.Items.Add(VolumeItem("Efecte", () => _settings.SfxVolume, v => _settings.SfxVolume = v, "Pași, uși, telefoane, decizii. Tasta M oprește tot sunetul."));
        m.Items.Add(VolumeItem("Ambianță", () => _settings.AmbientVolume, v => _settings.AmbientVolume = v, "Ploaia, vântul, neonul, greierii."));
        m.Items.Add(VolumeItem("Muzică", () => _settings.MusicVolume, v => _settings.MusicVolume = v, "Tema din meniuri."));
        m.Items.Add(new MenuItem("Numele tău")
        {
            Value = () => _settings.PlayerName + (m.Current?.Label == "Numele tău" && (int)(m.Time * 2) % 2 == 0 ? "_" : " "),
            TypeChar = EditName,
            Hint = "Scrie direct de la tastatură. Îl vor vedea ceilalți în multiplayer.",
        });
        if (_settings.CrtUnlocked)
            m.Items.Add(new MenuItem("Mod VHS")
            {
                Value = () => _settings.Crt ? "Pornit" : "Oprit",
                Adjust = _ => { _settings.Crt = !_settings.Crt; Changed(); },
                Hint = "L-ai găsit. Bravo.",
            });
        else
            m.Items.Add(new MenuItem("???") { Enabled = false });
        m.Items.Add(new MenuItem("Înapoi", back));
        OpenMenu(m, overGame);
    }

    private void ShowEndings()
    {
        var m = new MenuScreen("Finaluri găsite") { Back = ShowMainMenu };
        m.Items.Add(new MenuItem("Înapoi", ShowMainMenu));
        m.Draw = (g, view, s, hits) => StoryPickerViews.DrawEndingsGallery(g, view, s, hits, m, _catalog.Stories, _endings);
        OpenMenu(m);
    }

    private void ShowPause()
    {
        if (_mode != Mode.Playing || _package is null) return;
        _paused = true;
        if (_scene?.Player is { } p) p.CancelWalk();
        if (_scene is not null) _scene.HideCloseup = true; // valorile din meniu nu se vad pe hartia alba
        var m = new MenuScreen("Pauză") { Subtitle = _package.Story.Title, Back = ResumeGame };
        m.Items.Add(new MenuItem("Reia", ResumeGame));
        m.Items.Add(new MenuItem("Hartă", () => { ResumeGame(); ToggleMap(); }) { Hint = "Pe unde ați trecut și unde e fiecare acum (tasta H)." });
        m.Items.Add(new MenuItem("Setări", () => ShowSettings(ShowPause)));
        m.Items.Add(new MenuItem("Controale", ShowHelp));
        if (_net is not null)
        {
            if (_net.IsHost) m.Items.Add(new MenuItem("Înapoi în lobby", _net.ReturnToLobby) { Hint = "Pentru toți jucătorii. Tura de acum se pierde." });
            m.Items.Add(new MenuItem("Părăsește jocul", () => LeaveNet(null)) { Hint = "În multiplayer jocul nu se oprește cât stai în meniu." });
        }
        else
        {
            m.Items.Add(new MenuItem("Tură nouă", () => StartGame()) { Hint = "O iei de la capăt cu același însoțitor." });
            m.Items.Add(new MenuItem("Meniul principal", ShowMainMenu) { Hint = "Progresul e salvat. Îl reiei cu Continuă." });
        }
        OpenMenu(m, overGame: true);
    }

    private void ResumeGame()
    {
        _paused = false;
        _menu = null;
        if (_scene is not null) _scene.HideCloseup = false;
        _text.CharsPerSecond = _settings.CharsPerSecond;
    }
}
