using System.Runtime.InteropServices;
using LastStopNight.Effects;

namespace LastStopNight;

/// <summary>
/// Tastatura si mouse-ul: mers, E, cifrele pentru decizii, Esc, codul Konami si click-urile in scena.
/// </summary>
public sealed partial class PlayerForm
{
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
    private static bool IsDown(Keys k) => (GetAsyncKeyState((int)k) & 0x8000) != 0;

    private static readonly Keys[] Konami = [Keys.Up, Keys.Up, Keys.Down, Keys.Down, Keys.Left, Keys.Right, Keys.Left, Keys.Right, Keys.B, Keys.A];

    private readonly List<Keys> _keyHistory = new();

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            TrackKonami(keyData);
            ActiveMenu?.HandleKey(keyData);
            return true;
        }
        if (keyData is Keys.Enter or Keys.Escape or Keys.Tab && ActiveMenu is { } m)
        {
            m.HandleKey(keyData == Keys.Tab ? Keys.Down : keyData);
            return true;
        }
        if (keyData == Keys.F11) { ToggleFullscreen(); return true; }
        if (keyData == (Keys.Control | Keys.V) && ActiveMenu?.Current?.TypeChar is { } type && Clipboard.ContainsText())
        {
            foreach (char ch in Clipboard.GetText().Trim()) if (!char.IsControl(ch)) type(ch);
            return true;
        }
        if (keyData == Keys.F1) { ShowHelp(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void TrackKonami(Keys k)
    {
        _keyHistory.Add(k);
        if (_keyHistory.Count > Konami.Length) _keyHistory.RemoveAt(0);
        if (!_keyHistory.SequenceEqual(Konami)) return;
        _keyHistory.Clear();
        _settings.CrtUnlocked = true;
        _settings.Crt = !_settings.Crt;
        _settings.Save();
        PlaySfx("vhs");
        _scene?.Effects.Add(new FloatingTextEffect(_settings.Crt ? "Mod VHS: pornit. Bine ai venit în 1987." : "Mod VHS: oprit.",
            Color.FromArgb(160, 255, 180), new PointF(160, 30), 0));
        if (_menu?.Title == "Setări") ShowSettings(_menuBack);
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (ActiveMenu?.Current?.TypeChar is not null && !char.IsControl(e.KeyChar)) { ActiveMenu.HandleChar(e.KeyChar); e.Handled = true; }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _idleTime = 0;
        if (e.KeyCode is Keys.A or Keys.B) TrackKonami(e.KeyCode);
        if (ActiveMenu is { } menu)
        {
            if (e.KeyCode is Keys.Space or Keys.Back) menu.HandleKey(e.KeyCode);
            e.SuppressKeyPress = e.KeyCode is Keys.Space;
            return;
        }
        if (e.KeyCode == Keys.F && _mode == Mode.Playing && !_paused && _net?.Follow is not null) { _net.FollowNow(); return; }
        if (e.KeyCode == Keys.H && _mode is Mode.Playing or Mode.Ending) { ToggleMap(); return; }
        if (e.KeyCode == Keys.Escape && _showMap) { ToggleMap(); return; }
        if (_mode == Mode.Ending && _net is null && e.KeyCode == Keys.Enter && _modeTime > 1f) { StartGame(); e.Handled = true; return; }
        if (_mode == Mode.Ending && _net is null && e.KeyCode == Keys.Escape) { ShowMainMenu(); return; }
        if (_mode != Mode.Playing || _scene is null) return;

        if (e.KeyCode == Keys.Escape) OnEscapeInGame();
        else if (e.KeyCode == Keys.E && _scene.CloseupVisible && _scene.ShowCloseup) _scene.ShowCloseup = false;
        else if (e.KeyCode == Keys.E && _scene.NearestInteractable() is { } h) Use(h);
        else if (e.KeyCode is >= Keys.D1 and <= Keys.D9 || e.KeyCode is >= Keys.NumPad1 and <= Keys.NumPad9)
        {
            int idx = e.KeyCode >= Keys.NumPad1 ? e.KeyCode - Keys.NumPad1 : e.KeyCode - Keys.D1;
            var options = _engine?.GetDecisions() ?? [];
            if (idx < options.Count) Activate(options[idx]);
        }
        else if (e.KeyCode == Keys.Space) _text.Complete();
        else if (e.KeyCode == Keys.C && _scene.HasCloseup)
        {
            _scene.ShowCloseup = !_scene.ShowCloseup;
            PlaySfx("paper", 0.5f);
        }
        e.SuppressKeyPress = e.KeyCode is Keys.Space or Keys.E;
    }

    /// <summary>Esc in joc: opreste mersul automat; daca nu mergea nimic, deschide pauza.</summary>
    private void OnEscapeInGame()
    {
        if (_scene?.Player is { IsAutoWalking: true } p) { p.CancelWalk(); return; }
        if (_scene?.CloseupVisible == true && _scene.ShowCloseup) { _scene.ShowCloseup = false; return; }
        ShowPause();
    }

    private void OnCanvasClick(object? sender, MouseEventArgs e)
    {
        _idleTime = 0;
        if (ActiveMenu is { } menu) { menu.Click(e.Location); return; }
        if (_mode != Mode.Playing || _scene is null) return;
        if (_scene.HasCloseup && _scene.ShowCloseup) { _scene.ShowCloseup = false; return; }
        var w = _canvas.ToWorld(e.Location);
        var hit = _scene.Interactables.Where(h => !h.IsHidden).FirstOrDefault(h => MathF.Abs(h.X - w.X) <= 8 && w.Y > _scene.FloorY - 70);
        if (hit is not null)
        {
            var scene = _scene;
            if (scene.Player is { } p) p.WalkTo(Math.Clamp(hit.X, scene.MinX, scene.MaxX), () => { if (_scene == scene) Use(hit); });
            else Use(hit);
            return;
        }
        _scene.Player?.WalkTo(Math.Clamp(w.X, _scene.MinX, _scene.MaxX), null);
    }

    private void ShowHelp() => MessageBox.Show(this,
        "A / D sau săgețile        mergi prin scenă\n" +
        "Click în scenă            mergi până acolo\n" +
        "E                         folosești ce e lângă tine\n" +
        "1 - 9                     alegi o decizie\n" +
        "Space                     tot textul deodată\n" +
        "C                         ascunzi / arăți imaginea mare sau hârtia\n" +
        "Esc                       pauză (sau te oprești din mers)\n" +
        "H                         harta turei (unde e fiecare)\n" +
        "F                         multiplayer: mergi după cine a ales\n" +
        "F11                       ecran complet\n\n" +
        "Licăririle calde din scenă sunt decizii: mergi acolo și apeși E.\n" +
        "Sclipirile reci sunt lucruri la care te poți uita. Unele ascund mai mult decât par.\n" +
        "Dacă ai un însoțitor, ascultă și ce zice el. Și ține ochii pe geamuri.",
        "Cum se joacă", MessageBoxButtons.OK, MessageBoxIcon.Information);

    /// <summary>
    /// Mersul se citeste direct din starea tastelor (nu din evenimente), ca sa fie fluid si sa nu depinda
    /// de repetarea automata a tastei. Doar cat fereastra e activa si nu e deschis un meniu.
    /// </summary>
    private void ReadMovementKeys()
    {
        bool active = (ActiveForm == this || _testDriven) && _mode == Mode.Playing && !_paused;
        _scene!.Input.Left = active && (IsDown(Keys.A) || IsDown(Keys.Left));
        _scene.Input.Right = active && (IsDown(Keys.D) || IsDown(Keys.Right));
        if (_scene.Input.Left || _scene.Input.Right) _idleTime = 0;
    }
}
