using LastStopNight.Core.Model;
using LastStopNight.Net;
using LastStopNight.Rendering;

namespace LastStopNight.Ui;

/// <summary>
/// Ce se deseneaza peste scena in timpul jocului, la rezolutia ecranului: banda de final, banda de vot,
/// indiciile de control si filtrul VHS. Functii pure: primesc tot ce le trebuie ca parametri.
/// Coordonatele sunt in unitatile lumii (320x180), inmultite cu scara <c>s</c>.
/// </summary>
public static class Overlays
{
    public static (string Label, Color Color) EndingLabel(EndingType type) => type switch
    {
        EndingType.Good => ("FINAL BUN", Color.FromArgb(120, 220, 140)),
        EndingType.Bad => ("FINAL RĂU", Color.FromArgb(230, 50, 60)),
        EndingType.Secret => ("FINAL SECRET", Color.FromArgb(200, 170, 255)),
        _ => ("ALT FINAL", Color.FromArgb(200, 200, 200)),
    };

    /// <summary>Banda cu tipul si titlul finalului; <paramref name="fade"/> 0..1 o face sa apara lin.</summary>
    public static void DrawEndingBanner(Graphics g, RectangleF view, float s, StoryBlock block, float fade, int found, int total)
    {
        if (fade <= 0) return;
        var (label, color) = EndingLabel(block.EndingType);
        var band = new RectangleF(view.X, view.Y + 112 * s, view.Width, 50 * s);
        g.FillRectangle(Gfx.Brush((int)(170 * fade), 0, 0, 0), band);
        HudRenderer.DrawCenteredText(g, label, FontCache.Px("Consolas", 8, s, FontStyle.Bold), Color.FromArgb((int)(255 * fade), color),
            view, band.Y + 3 * s, s);
        HudRenderer.DrawCenteredText(g, block.EndingTitle ?? block.Title, FontCache.Px("Georgia", 26, s, FontStyle.Bold),
            Color.FromArgb((int)(255 * fade), 236, 228, 216), view, band.Y + 12 * s, s);
        HudRenderer.DrawCenteredText(g, $"Finaluri găsite: {found} din {total}", FontCache.Px("Consolas", 6, s),
            Color.FromArgb((int)(180 * fade), 200, 190, 180), view, band.Bottom - 9 * s, s);
    }

    /// <summary>Multiplayer: cine a votat, pe cine se mai asteapta si cate secunde au mai ramas.</summary>
    public static void DrawVoteBanner(Graphics g, RectangleF view, float s, NetSession net)
    {
        var names = net.Votes.Keys.Select(id => net.Lobby.Players.FirstOrDefault(p => p.Id == id)?.Name ?? "?").ToList();
        var waiting = net.RoutePlayers.Where(p => !net.Votes.ContainsKey(p.Id)).Select(p => p.Name).ToList();
        string text = waiting.Count == 0 ? "Se decide..."
            : $"Au votat: {string.Join(", ", names)}.  Așteptăm: {string.Join(", ", waiting)}  ({net.VoteSecondsLeft:0}s)";
        DrawBanner(g, view, s, text);
    }

    /// <summary>Banda de sus, centrata (vot, „mergi dupa el”, „te uiti la...”).</summary>
    public static void DrawBanner(Graphics g, RectangleF view, float s, string text)
    {
        var font = MenuStyle.Body(s, 5.6f);
        var size = g.MeasureString(text, font);
        var box = new RectangleF(view.X + (view.Width - size.Width) / 2 - 6 * s, view.Y + 4 * s, size.Width + 12 * s, size.Height + 3 * s);
        HudRenderer.FillRounded(g, Gfx.Brush(200, 20, 12, 14), box, 3 * s);
        HudRenderer.DrawRounded(g, Gfx.Pen(Color.FromArgb(200, 120, 70), Math.Max(1, 0.5f * s)), box, 3 * s);
        g.DrawString(text, font, Gfx.Brush(MenuStyle.Warm), box.X + 6 * s, box.Y + 1.5f * s);
    }

    /// <summary>Drumuri separate: unde e fiecare jucator de pe alt drum (sus, sub HUD).</summary>
    public static void DrawOtherRoutes(Graphics g, RectangleF view, float s, NetSession net, Func<string, string> characterName)
    {
        float y = view.Y + 4 * s;
        var font = MenuStyle.Body(s, 5f);
        foreach (var (name, characterId, block) in net.OtherRoutes)
        {
            string text = $"{name} ({characterName(characterId)}), pe alt drum: {block.Title}{(block.IsEnding ? " (final)" : "")}";
            var size = g.MeasureString(text, font);
            var box = new RectangleF(view.Right - size.Width - 10 * s, y, size.Width + 6 * s, size.Height + 2 * s);
            HudRenderer.FillRounded(g, Gfx.Brush(170, 14, 12, 20), box, 3 * s);
            g.DrawString(text, font, Gfx.Brush(200, 190, 200, 235), box.X + 3 * s, box.Y + 1 * s);
            y += box.Height / s * s + 2 * s;
        }
    }

    /// <summary>Randul cu tastele de la inceputul turei (<paramref name="alpha"/> 0..1, ca sa dispara lin).</summary>
    public static void DrawControlsHint(Graphics g, RectangleF view, float s, float alpha)
    {
        if (alpha <= 0) return;
        g.DrawString("A/D mers    click în scenă    E folosește    1-9 decizii    Esc pauză", FontCache.Px("Segoe UI", 5f, s),
            Gfx.Brush((int)(120 * alpha), 200, 190, 180), view.X + 4 * s, view.Bottom - 9 * s);
    }

    public static void DrawCloseupHint(Graphics g, RectangleF view, float s) =>
        HudRenderer.DrawCenteredText(g, "C sau click: închide", FontCache.Px("Segoe UI", 5.4f, s),
            Color.FromArgb(170, 220, 210, 200), view, view.Y + 106 * s, s);

    /// <summary>Easter egg: filtrul „VHS” (codul Konami) adauga linii de scanare si o banda care coboara.</summary>
    public static void DrawCrt(Graphics g, RectangleF view, double seconds)
    {
        const float lineSpacing = 3, bandHeight = 22, bandSpeed = 60;
        var pen = Gfx.Pen(Color.FromArgb(55, 0, 0, 0), 1);
        for (float y = view.Y; y < view.Bottom; y += lineSpacing) g.DrawLine(pen, view.X, y, view.Right, y);
        float band = (float)(seconds * bandSpeed % (view.Height + 80)) - 40;
        g.FillRectangle(Gfx.Brush(18, 255, 255, 255), view.X, view.Y + band, view.Width, bandHeight);
        g.DrawString("PLAY", FontCache.Get("Consolas", 14, FontStyle.Bold), Gfx.Brush(200, 230, 230, 230), view.X + 14, view.Y + 12);
    }
}
