using LastStopNight.Core.Engine;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Effects;
using LastStopNight.Game;
using LastStopNight.Net;
using LastStopNight.Rendering;
using LastStopNight.Ui;

namespace LastStopNight;

/// <summary>
/// Povestea: catalogul, sesiunea de joc, intrarea in blocuri, deciziile, examinarile si salvarea.
/// Toate actiunile trec prin <see cref="IGameSession"/>: local se aplica imediat, in multiplayer le ordoneaza gazda.
/// </summary>
public sealed partial class PlayerForm
{
    /// <summary>Ce zice personajul daca jucatorul sta degeaba (vezi <see cref="IdleSecondsBeforeLine"/>).</summary>
    private static readonly string[] IdleLines =
    [
        "Picioarele mele vor concediu medical.",
        "Încă o cafea și o să văd prin pereți.",
        "Dacă stau nemișcat, poate nu mă observă nimeni.",
        "Ceasul ăla merge cumva... înapoi?",
        "Mama zicea să-mi iau o slujbă normală.",
        "Hm. Liniștea asta nu-mi place deloc.",
    ];

    private readonly EndingsTracker _endings = new();

    private readonly List<PropertyChange> _pendingChanges = new();

    private readonly Dictionary<string, float> _flashes = new();

    private readonly ToolTip _tips = new();

    private StoryCatalog _catalog = new();

    private readonly Random _rnd = new();

    private StoryPackage? _package;

    private ImageCache? _images;

    private GameEngine? _engine;

    private IGameSession? _session;

    private List<string> _companions = new();

    private bool _replaying;

    private float _modeTime, _idleTime, _playTime;

    /// <summary>Incarca povestile (de pe disc si din exe) si deschide meniul principal.</summary>
    private void LoadCatalog(string? explicitPath)
    {
        _catalog = StoryCatalog.Load(explicitPath);
        if (_catalog.Count == 0) { OpenStoryDialog(); return; }
        Select(_catalog[0]);
        ShowMainMenu();
    }

    private void OpenStoryDialog()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Deschide o poveste",
            Filter = "Poveste (*.zip;*.json)|*.zip;*.json|Toate fișierele|*.*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var pkg = StoryArchive.Load(dlg.FileName);
            _catalog.AddFromFile(dlg.FileName, pkg);
            Select(pkg);
            ShowStories();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "Povestea nu a putut fi încărcată:\n\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Alege povestea curenta pentru single player: motor nou, sesiune locala, scena din spatele meniului.</summary>
    private void Select(StoryPackage pkg)
    {
        if (_package == pkg && _engine is not null) return;
        var engine = new GameEngine(pkg.Story);
        var session = new LocalSession(engine);
        session.Examined += OnExamined;
        UseStory(pkg, engine, session);
        BuildBackdrop();
    }

    /// <summary>
    /// Leaga interfata de o poveste si de motorul ei. Acelasi drum pentru single player (motor + sesiune
    /// locala) si multiplayer (motorul si sesiunea vin din <see cref="NetSession"/>).
    /// </summary>
    private void UseStory(StoryPackage pkg, GameEngine engine, IGameSession session)
    {
        _images?.Dispose();
        _package = pkg;
        _images = new ImageCache(pkg);
        _engine = engine;
        _engine.BlockChanged += OnBlockChanged;
        _engine.StateChanged += (_, e) => { if (!_replaying && _net?.Replaying != true) _pendingChanges.AddRange(e.Changes); };
        _session = session;
        _hudKey = default;
        Text = pkg.Story.Title;
    }

    /// <summary>Trece interfata in modul de joc: fara meniu, cu HUD-ul si panoul de text.</summary>
    private void ShowPlayingLayout()
    {
        _menu = null;
        _paused = false;
        _playTime = 0;
        _text.CharsPerSecond = _settings.CharsPerSecond;
        SetLayout(playing: true);
        _mode = Mode.Playing;
        _audio.SetMusic(null);
    }

    /// <summary>Scena din spatele meniului: locatia de start a povestii, cu ceata, fara personaje.</summary>
    private void BuildBackdrop()
    {
        if (_package is null || _images is null) return;
        var story = _package.Story;
        var start = story.FindBlock(story.StartBlockId);
        var b = new StoryBlock { Id = "__menu", Location = start?.Location, Background = start?.Background, Fx = ["fog"] };
        _scene = new Scene(story, b, _images, null, includePlayer: false);
    }

    private void StartGame(IEnumerable<string>? companions = null)
    {
        if (_session is null) return;
        _companions = (companions ?? _companions).ToList();
        _pendingChanges.Clear();
        _flashes.Clear();
        _scene = null;
        ShowPlayingLayout();
        _session.Start(_companions);
    }

    /// <summary>„Continua”: reaplica actiunile salvate pe un motor nou (motorul e determinist).</summary>
    private void ContinueSaved(SaveGame save)
    {
        if (_catalog.FindByTitle(save.StoryTitle) is not { } pkg || _session is null) return;
        Select(pkg);
        _companions = save.Companions.ToList();
        ShowPlayingLayout();
        _replaying = true;
        try { _engine!.Replay(_companions, save.Actions); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // salvare dintr-o versiune mai veche a povestii: o luam de la capat, nu blocam jocul
            _replaying = false;
            SaveGame.Clear();
            StartGame();
            return;
        }
        finally { _replaying = false; }
        _scene = null;
        _pendingChanges.Clear();
        EnterBlock(_engine!.CurrentBlock!, null);
    }

    private void SaveProgress()
    {
        if (_net is not null) return; // in multiplayer nu salvam (tura e a tuturor)
        if (_engine?.CurrentBlock is not { } b || _package is null) return;
        if (b.IsEnding) { SaveGame.Clear(); return; }
        new SaveGame
        {
            StoryTitle = _package.Story.Title,
            Companions = _companions.ToList(),
            Actions = _engine.Log.ToList(),
            BlockTitle = b.Title,
            Time = b.Time,
            SavedAt = DateTime.Now,
        }.Save();
    }

    private void OnBlockChanged(object? sender, BlockChangedEventArgs e)
    {
        if (_replaying || _net?.Replaying == true) return; // motorul se reface: conteaza doar blocul de la sfarsit
        EnterBlock(e.Current, e.Redirect);
    }

    private void EnterBlock(StoryBlock block, RedirectDefinition? redirect)
    {
        if (_images is null || _engine is null) return;
        _modeTime = 0;
        _idleTime = 0;
        var seen = _scene?.SeenExamines ?? new HashSet<string>();
        // in multiplayer decizia se aude cand se aplica (dupa vot), nu cand votezi
        if (_net is not null && _scene is { Player: not null } && !block.IsEnding) PlaySfx("choose");
        if (_net is not null)
            _scene = new Scene(_engine.Story, block, _images, _scene, includePlayer: !block.IsEnding && !_net.Watching,
                playerCharacter: _net.LocalPlayer, remotes: RemotePositions())
            { SeenExamines = seen };
        else
            _scene = new Scene(_engine.Story, block, _images, _scene, includePlayer: !block.IsEnding, companions: _engine.Companions)
            { SeenExamines = seen };
        if (_mode == Mode.Menu) ShowPlayingLayout(); // multiplayer: tura a pornit cat eram in lobby
        _mode = block.IsEnding ? Mode.Ending : Mode.Playing;

        UseBlockSoundscape(block);
        if (_splitNotice is not null)
        {
            _scene.Effects.Add(new FloatingTextEffect(_splitNotice, Color.FromArgb(255, 200, 120), new PointF(160, 34), 0.4f));
            PlaySfx("leave", 0.8f);
            _splitNotice = null;
        }
        if (block.Closeup is not null || block.Paper is not null) PlaySfx("paper", 0.7f);
        ShowStateFeedback(redirect);
        _text.SetText(block.Title, block.Time, _engine.CurrentText, instant: _settings.TextSpeed == TextSpeed.Instant);
        float voiceDelay = _scene.HasChapterCard ? 2.6f : 0.4f; // cartonasul de capitol se citeste in liniste
        float spoken = NarrateBlock(block, voiceDelay);
        ShowDialogue(block, _engine.CurrentText, voiceDelay, spoken);

        if (block.IsEnding)
        {
            _endings.Add(_engine.Story.Title, block.Id);
            if (_net is not null) BuildEndingButtons();
            else
                SetButtons(("Joacă din nou", () => StartGame(), true, null, null, 1), ("Meniul principal", ShowMainMenu, true, null, null, 2));
        }
        else BuildDecisionButtons();
        SaveProgress();
    }

    /// <summary>
    /// Schimbarile de stare: barele din HUD clipesc si tremura; valorile fara bara (bani, indicii)
    /// apar si ca text plutitor deasupra personajului.
    /// </summary>
    private void ShowStateFeedback(RedirectDefinition? redirect, bool fromExamine = false)
    {
        if (_scene is null || _engine is null) return;
        float delay = _scene.HasChapterCard ? 2.6f : 0.3f;
        var anchor = fromExamine ? new PointF(_scene.Player?.X ?? 160, 30)
            : _scene.Player is { } p ? new PointF(p.X, _scene.FloorY - 56) : new PointF(160, 70);
        foreach (var c in _pendingChanges)
        {
            var def = _engine.Story.FindProperty(c.Key);
            if (def?.Display == HudDisplay.Hidden) continue;
            _flashes[c.Key] = 1f;
            if (def?.Display == HudDisplay.Bar)
            {
                if (c.Key == "player.life" && c.Delta < 0)
                {
                    _scene.Effects.Add(new DamageFlashEffect());
                    _scene.Effects.Add(new ShakeEffect(0.6f, 4, rumble: false));
                }
                continue;
            }
            string label = def?.HudLabel ?? c.Key;
            string txt = def?.Display == HudDisplay.Flag ? (c.NewValue != 0 ? $"+ {label}" : $"- {label}")
                : c.Key == "story.night" ? $"{label} {def?.FormatValue(c.NewValue)}"
                : $"{(c.Delta > 0 ? "+" : "")}{c.Delta} {label}";
            var color = def?.Color is { } col ? Screen2D.ParseColor(col, Color.White)
                : c.Key.Contains("money") ? Color.Gold : Color.FromArgb(140, 210, 255);
            _scene.Effects.Add(new FloatingTextEffect(txt, color, anchor, delay));
            delay += 0.35f;
        }
        if (redirect is not null)
            _scene.Effects.Add(new FloatingTextEffect(redirect.Description, Color.FromArgb(255, 90, 90), new PointF(160, 40), delay));
        _pendingChanges.Clear();
    }

    private void RefreshInteractables()
    {
        if (_engine is null || _scene is null) return;
        _scene.Interactables.Clear();
        foreach (var o in _engine.GetDecisions().Where(o => o.Decision.HotspotX is not null))
            _scene.Interactables.Add(new Interactable(o.Decision.HotspotX!.Value, o.Decision.HotspotY, o.Decision.Text, o, null));
        foreach (var ex in _engine.GetExamines())
            _scene.Interactables.Add(new Interactable(ex.X, ex.Y, ex.Label, null, ex));
    }

    private void BuildDecisionButtons()
    {
        if (_engine is null || _scene is null || _images is null) return;
        if (_net is { Watching: true } watching)
        {
            _scene.Interactables.Clear();
            BuildWatchButtons(watching);
            return;
        }
        RefreshInteractables();
        int n = 0;
        var buttons = _engine.GetDecisions().Select(o =>
        {
            n++;
            string label = o.IsAvailable
                ? o.Decision.Text
                : $"{o.Decision.Text}  ({(o.Decision.LockedHint ?? "nu încă").ToLowerInvariant()})";
            var who = o.Decision.RequiresCharacter is { } rc ? _engine.Story.FindCharacter(rc) : null;
            string? tip = who is not null ? $"Decizie a lui {who.Name}: există doar pentru că {who.Name} e în tură."
                : o.IsAvailable ? (o.Decision.HotspotX is not null ? "Personajul merge întâi până acolo. Poți să mergi și singur și să apeși E." : null)
                : o.Decision.LockedHint;
            Bitmap? icon = !o.IsAvailable ? ButtonIcons.Locked
                : who is not null ? Portraits.Badge(_images, who)
                : o.Decision.HotspotX is not null ? ButtonIcons.Walk : null;
            var opt = o;
            return (label, (Action)(() => Activate(opt)), o.IsAvailable, tip, icon, (int?)n);
        }).ToList();
        // multiplayer: cineva a ales si a plecat; butonul de mers dupa el sta la urma (cifrele raman ale deciziilor)
        if (_net?.Follow is { } follow)
        {
            var net = _net;
            buttons.Add(($"Mergi după {NamesOf(follow.Players)}: „{follow.DecisionText}”  (F)", () => net.FollowNow(), true,
                "Ajungi unde a ajuns el, cu ce a găsit. Sau alege tu altceva și mergi pe drumul tău.", ButtonIcons.Walk, null));
        }
        SetButtons(buttons.ToArray());
        if (_net is null) return;
        var story = _engine.Story;
        var choiceButtons = _choices.Controls.OfType<ChoiceButton>().ToList();
        var options = _engine.GetDecisions();
        for (int i = 0; i < choiceButtons.Count && i < options.Count; i++)
            foreach (var voter in _net.VotersFor(options[i].Index))
                if (story.FindCharacter(_net.CharacterOf(voter)) is { } vc && Portraits.Badge(_images, vc) is { } badge)
                    choiceButtons[i].Voters.Add(badge);
    }

    private void SetButtons(params (string Text, Action Action, bool Enabled, string? Tip, Bitmap? Icon, int? Number)[] buttons)
    {
        _choices.SuspendLayout();
        foreach (Control c in _choices.Controls) c.Dispose();
        _choices.Controls.Clear();
        foreach (var (txt, action, enabled, tip, icon, number) in buttons)
        {
            var b = new ChoiceButton { Text = txt, Enabled = enabled, Image = icon, Number = number };
            b.Click += (_, _) => action();
            if (tip is not null) _tips.SetToolTip(b, tip);
            _choices.Controls.Add(b);
        }
        _choices.ResumeLayout();
        LayoutChoices();
    }

    private void LayoutChoices()
    {
        int w = _choices.ClientSize.Width - _choices.Padding.Horizontal - (_choices.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
        foreach (Control c in _choices.Controls)
            if (c is ChoiceButton cb) cb.FitHeight(Math.Max(120, w));
    }

    /// <summary>Activeaza o decizie: daca are punct in scena, personajul merge intai pana acolo.</summary>
    private void Activate(DecisionOption option)
    {
        if (_scene is null || _mode != Mode.Playing || _paused) return;
        if (!option.IsAvailable) { PlaySfx("locked"); return; }
        var scene = _scene;
        if (option.Decision.HotspotX is int hx && scene.Player is { } player)
        {
            player.WalkTo(Math.Clamp(hx, scene.MinX, scene.MaxX), () => Execute(scene, option));
            return;
        }
        Execute(scene, option);
    }

    private void Execute(Scene origin, DecisionOption option)
    {
        if (_engine is null || _session is null || _scene != origin || _engine.CurrentBlock != origin.Block) return;
        PlaySfx(_net is null || _net.Lobby.AllowSplit ? "choose" : "vote");
        try { _session.RequestChoose(option); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Eroare în poveste", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Use(Interactable it)
    {
        if (_engine is null || _scene is null || _session is null) return;
        if (it.Decision is { } d)
        {
            if (d.IsAvailable) Execute(_scene, d);
            else
            {
                PlaySfx("locked");
                _scene.Say(d.Decision.LockedHint ?? "Nu încă.");
            }
            return;
        }
        if (it.Examine is { } ex)
        {
            _scene.SeenExamines.Add(ex.Id);
            _examineOrigin = _scene;
            PlaySfx("examine");
            _session.RequestExamine(ex);
        }
    }

    private Scene? _examineOrigin;

    /// <summary>Dupa o examinare: jucatorul spune ce vede, apoi insotitorul isi zice parerea.</summary>
    private void OnExamined(object? sender, ExamineResult r)
    {
        var origin = _examineOrigin;
        _examineOrigin = null;
        if (_scene is null || (origin is not null && _scene != origin)) { BuildDecisionButtons(); return; } // examinarea a schimbat scena
        origin = _scene;
        var speaker = origin.FindActor(r.Player);
        var bubble = origin.Say(r.Text, speaker == origin.Player ? null : speaker);
        foreach (var f in origin.Followers)
            if (r.Examine.TextBy.TryGetValue(f.Definition.Id, out var remark) && remark != r.Text)
                origin.Say(remark, f, bubble.TotalDuration - 0.2f);
        ShowStateFeedback(null, fromExamine: true);
        RefreshInteractables(); // un obiect poate debloca altele
        BuildDecisionButtons();
        SaveProgress();
    }
}
