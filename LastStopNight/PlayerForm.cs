using System.Diagnostics;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Effects;
using LastStopNight.Game;
using LastStopNight.Rendering;
using LastStopNight.Ui;
using LastStopNight.Ui.Views;

namespace LastStopNight;

/// <summary>
/// Fereastra jocului: structura ei (scena, HUD-ul, panoul de jos), bucla de joc si desenarea.
/// Restul comportamentului e impartit pe fisiere: Story (povestea si deciziile), Input (tastatura si
/// mouse), Menus (meniuri), Multiplayer (retea) si DevTools (teste automate cu capturi).
/// </summary>
public sealed partial class PlayerForm : Form
{
    private enum Mode { Menu, Playing, Ending }

    private const int HudHeight = 46;
    private const int BottomPanelHeight = 236;
    /// <summary>~100 de cadre pe secunda; timer-ul de sistem e setat la 1 ms in Program (timeBeginPeriod).</summary>
    private const int FrameIntervalMs = 10;
    /// <summary>Un cadru nu avanseaza lumea cu mai mult de atat (dupa o pauza lunga, ex. fereastra mutata).</summary>
    private const float MaxFrameSeconds = 0.05f;
    private const float IdleSecondsBeforeLine = 40;
    private const float ControlsHintSeconds = 22, ControlsHintFadeSeconds = 3;
    /// <summary>Cat dureaza clipirea unei bare din HUD dupa o schimbare (1 / viteza).</summary>
    private const float HudFlashSpeed = 1.4f;
    /// <summary>Banda de final apare dupa scurt timp, lin.</summary>
    private const float EndingBannerDelay = 0.8f, EndingBannerFade = 1.2f;
    private static readonly Color PanelColor = Color.FromArgb(16, 11, 13);

    private readonly GameCanvas _canvas = new() { Dock = DockStyle.Fill };
    private readonly StoryTextPanel _text = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _choices = new()
    {
        Dock = DockStyle.Right,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        BackColor = PanelColor,
        Padding = new Padding(10, 14, 12, 8),
    };
    private readonly Panel _bottom = new() { Dock = DockStyle.Bottom, Height = BottomPanelHeight, BackColor = PanelColor };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = FrameIntervalMs };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly GameSettings _settings = GameSettings.Load();

    private Scene? _scene;
    private Mode _mode = Mode.Menu;
    private double _lastTime;

    // HUD-ul se redeseneaza intr-un bitmap doar cand se schimba starea (cheia), nu la fiecare cadru
    private Bitmap? _hudCache;
    private (int, string?, Size, StoryPackage?) _hudKey;

    // masurarea timpului de desenare (perf.txt in modul de capturi)
    private readonly Stopwatch _paintWatch = new();
    private double _paintTotal;
    private int _paintCount;

    public PlayerForm(string? storyPath)
    {
        Text = "Last Stop";
        BackColor = Color.Black;
        ClientSize = new Size(1180, 860);
        MinimumSize = new Size(820, 640);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        DoubleBuffered = true;
        if (Environment.ProcessPath is { } exe)
            try { Icon = Icon.ExtractAssociatedIcon(exe); } catch (ArgumentException) { }

        _bottom.Controls.Add(_text);
        _bottom.Controls.Add(_choices);
        Controls.Add(_canvas);
        Controls.Add(_bottom);

        _canvas.DrawWorld += g => { _paintWatch.Restart(); _scene?.RenderWorld(g); };
        _canvas.DrawOverlay += (g, v, s) =>
        {
            DrawOverlay(g, v, s);
            _paintTotal += _paintWatch.Elapsed.TotalMilliseconds;
            _paintCount++;
        };
        _canvas.DrawBand += DrawBand;
        _canvas.MouseClick += OnCanvasClick;
        _canvas.MouseMove += (_, e) => ActiveMenu?.Hover(e.Location);
        _choices.SizeChanged += (_, _) => LayoutChoices();
        Resize += (_, _) => FitChoicesWidth();
        _timer.Tick += (_, _) => Tick();
        Load += (_, _) =>
        {
            FitChoicesWidth();
            ApplyFullscreen(_settings.Fullscreen);
            ApplyAudioSettings();
            LoadCatalog(storyPath);
            _timer.Start();
        };
    }

    /// <summary>Timpul mediu de desenare a unui cadru (ms), pentru verificarea fluiditatii.</summary>
    public double AveragePaintMs => _paintCount == 0 ? 0 : _paintTotal / _paintCount;

    /// <summary>Coloana cu decizii: 38% din latime, intre 340 si 520 de pixeli.</summary>
    private void FitChoicesWidth() => _choices.Width = Math.Clamp(ClientSize.Width * 38 / 100, 340, 520);

    // ------------------------------------------------------------------ fereastra

    private void ToggleFullscreen()
    {
        _settings.Fullscreen = !_settings.Fullscreen;
        _settings.Save();
        ApplyFullscreen(_settings.Fullscreen);
    }

    private void ApplyFullscreen(bool on)
    {
        if (on)
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal; // ca maximizarea sa acopere si bara de activitati
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
        }
    }

    /// <summary>Meniurile ocupa tot ecranul; in joc apar HUD-ul de sus si panoul de text de jos.</summary>
    private void SetLayout(bool playing)
    {
        _bottom.Visible = playing;
        _canvas.TopBand = playing ? HudHeight : 0;
        _canvas.Cover = !playing;
    }

    // ------------------------------------------------------------------ bucla de joc

    private void Tick()
    {
        double now = _clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Min(MaxFrameSeconds, now - _lastTime);
        _lastTime = now;
        _modeTime += dt;
        if (_menu is not null) _menu.Time += dt;
        PumpNet(dt);
        FadeHudFlashes(dt);
        if (_scene is null) return;

        ReadMovementKeys();
        if (!_paused || _net is not null) // in multiplayer lumea nu se opreste cand deschizi meniul
        {
            _scene.Update(dt);
            foreach (var action in _scene.TakePosted()) action();
            PlaySceneSounds();
            _text.Advance(dt);
            if (_mode == Mode.Playing) _playTime += dt;
        }
        if (_net is not null && _mode == Mode.Playing && _scene.Player is { } me)
            _net.SendMove(me.X, me.FacingLeft, me.IsWalking);
        SayIdleLineIfBored(dt);
        _canvas.Invalidate();
    }

    private void FadeHudFlashes(float dt)
    {
        foreach (var key in _flashes.Keys.ToList())
            if ((_flashes[key] -= dt * HudFlashSpeed) <= 0) _flashes.Remove(key);
    }

    /// <summary>Daca jucatorul sta degeaba, personajul mai zice cate ceva.</summary>
    private void SayIdleLineIfBored(float dt)
    {
        if (_mode != Mode.Playing || _paused || !_text.IsComplete || _scene?.Player is null) return;
        _idleTime += dt;
        if (_idleTime <= IdleSecondsBeforeLine) return;
        _idleTime = 0;
        _scene.Say(IdleLines[_rnd.Next(IdleLines.Length)]);
    }

    // ------------------------------------------------------------------ desenare

    private void DrawBand(Graphics g, Rectangle band)
    {
        if (_mode == Mode.Menu || band.Height <= 0 || _engine is null || _package is null || _images is null) return;
        var party = _engine.Party.OrderBy(p => p == _engine.Story.PlayerCharacterId ? 0 : 1)
            .Select(_engine.Story.FindCharacter).OfType<CharacterDefinition>().ToList();
        if (_flashes.Count > 0)
        {
            // cat timp o bara clipeste, HUD-ul se deseneaza la fiecare cadru
            HudRenderer.DrawStrip(g, band, _package.Story, _engine.State, _scene?.Block, _images, party, _flashes, HudName);
            _hudKey = default;
            return;
        }
        // in multiplayer, numele de sub portrete se schimba cand intra sau iese cineva
        var key = (_engine.State.Version + (_net?.Lobby.Players.Count ?? 0) * 100000, _scene?.Block.Id, band.Size, _package);
        if (_hudCache is null || key != _hudKey)
        {
            _hudCache?.Dispose();
            _hudCache = new Bitmap(Math.Max(1, band.Width), Math.Max(1, band.Height), System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var hg = Graphics.FromImage(_hudCache))
                HudRenderer.DrawStrip(hg, new Rectangle(Point.Empty, band.Size), _package.Story, _engine.State, _scene?.Block, _images, party, null, HudName);
            _hudKey = key;
        }
        g.DrawImageUnscaled(_hudCache, band.Location);
    }

    private void DrawOverlay(Graphics g, RectangleF view, float s)
    {
        if (_scene is null || _package is null || _images is null) return;

        if (_mode == Mode.Menu)
        {
            _menu?.Render(g, view, s);
            if (_settings.Crt) Overlays.DrawCrt(g, view, _clock.Elapsed.TotalSeconds);
            return;
        }

        // fundalurile opace (harta, concluziile, cartonasul de capitol) sunt in imaginea lumii, deci ce e desenat
        // aici, la rezolutia ecranului, s-ar vedea prin ele: bulele, hartia si indiciile asteapta pana se inchid
        bool covered = _showMap || _scene.HasChapterCard || (_paused && _menu?.Title == "Concluzii");
        if (!covered)
        {
            if (_scene.Paper is { } paper && _scene.CloseupProgress > 0) Overlays.DrawPaper(g, view, s, paper, _scene.CloseupProgress);
            foreach (var e in _scene.Effects)
                if (e is not ChapterCardEffect) e.DrawOverlay(g, view, s, _scene);
        }
        if (_mode == Mode.Playing && _scene.Player is not null && !_paused && !covered) DrawPlayingHints(g, view, s);
        if (_mode == Mode.Playing && _net is { Votes.Count: > 0 } net) Overlays.DrawVoteBanner(g, view, s, net);
        else if (_mode == Mode.Playing && !_showMap && _net?.Follow is { } follow)
            Overlays.DrawBanner(g, view, s, $"{NamesOf(follow.Players)} a ales: „{follow.DecisionText}”.   F: mergi după el   sau alege altceva");
        else if (_mode == Mode.Playing && !_showMap && _net is { Watching: true } w)
            Overlays.DrawBanner(g, view, s, $"Te uiți la {NamesOf(w.RoutePlayers.Select(p => p.Id))}.   Esc: uită-te la altcineva");
        else if (_mode is Mode.Playing or Mode.Ending && !_showMap && _net is { IsSplit: true } split && _engine is { } eng)
            Overlays.DrawOtherRoutes(g, view, s, split, id => eng.Story.FindCharacter(id)?.Name.Split(' ')[0] ?? id);
        if (_mode == Mode.Ending)
        {
            var story = _package.Story;
            float fade = Math.Clamp((_modeTime - EndingBannerDelay) / EndingBannerFade, 0, 1);
            Overlays.DrawEndingBanner(g, view, s, _scene.Block, fade, _endings.Count(story.Title), story.Blocks.Count(b => b.IsEnding));
        }
        // cardul de capitol acopera tot, deci vine ultimul (inainte de meniul de pauza)
        foreach (var e in _scene.Effects.OfType<ChapterCardEffect>()) e.DrawOverlay(g, view, s, _scene);
        if (_showMap && !_paused && _engine is not null)
            RouteMapView.Draw(g, view, s, _engine.Story, MapRoutes(), _images, (float)_clock.Elapsed.TotalSeconds);
        if (_paused) _menu?.Render(g, view, s);
        if (_settings.Crt) Overlays.DrawCrt(g, view, _clock.Elapsed.TotalSeconds);
    }

    /// <summary>Indiciul obiectului de langa jucator, indiciul prim-planului si randul cu tastele de la inceput.</summary>
    private void DrawPlayingHints(Graphics g, RectangleF view, float s)
    {
        var (h, fade) = _scene!.Focus;
        if (h is not null && !_scene.Effects.OfType<SpeechBubbleEffect>().Any(b => !b.IsFinished))
        {
            string verb = h.Decision is null || h.IsAvailable ? h.Label
                : $"{h.Label} ({(h.Decision.Decision.LockedHint ?? "nu încă").ToLowerInvariant()})";
            HudRenderer.DrawHint(g, view, s, _scene.Anchor(h), verb, h.IsAvailable, h.Decision is null, fade);
        }
        if (_scene.CloseupVisible) Overlays.DrawCloseupHint(g, view, s, _scene.Paper is null ? 106 : 170);
        else Overlays.DrawControlsHint(g, view, s, Math.Clamp((ControlsHintSeconds - _playTime) / ControlsHintFadeSeconds, 0, 1));
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        LeaveNet(null, showMenu: false);
        _timer.Stop();
        _audio.Dispose();
        _images?.Dispose();
        _hudCache?.Dispose();
        foreach (var c in _thumbs.Values) c.Dispose();
        base.OnFormClosed(e);
    }
}
