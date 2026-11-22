using LastStopNight.Core.Engine;
using LastStopNight.Core.Model;
using LastStopNight.Rendering;
using static LastStopNight.Ui.MenuStyle;

namespace LastStopNight.Ui.Views;

/// <summary>Finalul unui jucator, pentru ecranul de concluzii.</summary>
public sealed record PlayerEnding(string Name, CharacterDefinition? Character, GameEngine Engine, bool IsMe);

/// <summary>
/// Concluziile turei (multiplayer, cand toti au ajuns la un final): finalul fiecaruia, unul langa altul,
/// ce ati gasit impreuna (proprietatile puse la comun, ex. indiciile) si cate finaluri mai sunt de descoperit.
/// </summary>
public static class ConclusionView
{
    private const float CardHeight = 68, CardGap = 5, Left = 20, Width = 280;

    public static void Draw(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuScreen m, StoryDefinition story,
                            IReadOnlyList<PlayerEnding> endings, ImageCache? images)
    {
        Screen2D.RequestTint(1, 255, Color.FromArgb(10, 7, 9)); // opac: banda de final din spate nu trebuie sa se vada prin el
        float top = Logo(g, view, s, m.Title, m.Subtitle, m.Time, Left, 6);
        g.DrawString(Verdict(endings), FontCache.Px("Georgia", 6.4f, s, FontStyle.Italic), Gfx.Brush(Warm), view.X + Left * s, view.Y + top * s);
        float cardsTop = top + 11;

        int n = Math.Max(1, endings.Count);
        float w = (Width - (n - 1) * CardGap) / n;
        for (int i = 0; i < endings.Count; i++) DrawCard(g, view, s, story, endings[i], images, Left + i * (w + CardGap), cardsTop, w);

        float y = cardsTop + CardHeight + 4;
        var shared = story.Properties.Where(p => p.Shared && p.Display != HudDisplay.Hidden).ToList();
        foreach (var p in shared)
        {
            int best = endings.Count == 0 ? p.Initial : endings.Max(e => e.Engine.State.Get(p.Key));
            g.DrawString($"Împreună, {p.HudLabel.ToLowerInvariant()}: {p.FormatValue(best)}" + (p.Format?.Contains('/') == true ? "" : $" din {p.Max}"),
                Mono(s, 6f), Gfx.Brush(Ink), view.X + Left * s, view.Y + y * s);
            y += 8;
        }
        int distinct = endings.Select(e => e.Engine.CurrentBlock?.Id).Distinct().Count();
        int total = story.Blocks.Count(b => b.IsEnding);
        g.DrawString($"Finaluri diferite în tura asta: {distinct}.  Mai sunt {total - distinct} de descoperit.",
            Body(s, 5.4f), Gfx.Brush(Muted), view.X + Left * s, view.Y + y * s);
        DrawButtons(g, view, s, hits, m);
    }

    /// <summary>O fraza despre cum s-a terminat tura pentru toata echipa.</summary>
    private static string Verdict(IReadOnlyList<PlayerEnding> endings)
    {
        var types = endings.Select(e => e.Engine.CurrentBlock?.EndingType ?? EndingType.None).ToList();
        if (types.Count == 0) return "";
        bool together = endings.All(e => e.Engine.Path.SequenceEqual(endings[0].Engine.Path)); // acelasi drum de la cap la coada
        bool sameEnd = endings.Select(e => e.Engine.CurrentBlock?.Id).Distinct().Count() == 1;
        if (types.All(t => t is EndingType.Good or EndingType.Secret))
            return together ? "Ați rămas împreună până la capăt și ați scăpat toți."
                : sameEnd ? "Pe drumuri diferite, dar ați ajuns în același loc. Și ați scăpat."
                : "Pe drumuri diferite, dar ați scăpat toți. Ce ați aflat se leagă.";
        if (types.All(t => t == EndingType.Bad))
            return together ? "Ați căzut împreună. Noaptea a câștigat de data asta."
                : "Despărțiți, v-a prins pe fiecare. Poate împreună vedeați mai mult.";
        return "Nu toți ați avut aceeași noapte. Unul a văzut ce i-a scăpat celuilalt.";
    }

    private static void DrawCard(Graphics g, RectangleF view, float s, StoryDefinition story, PlayerEnding e, ImageCache? images, float x, float y, float w)
    {
        var box = R(view, s, x, y, w, CardHeight);
        Card(g, box, s, e.IsMe);
        var block = e.Engine.CurrentBlock;
        if (e.Character is { } c && images is not null && Portraits.Badge(images, c) is { } badge)
            g.DrawImage(badge, box.X + 4 * s, box.Y + 4 * s, 12 * s, 12 * s);
        string who = e.Name + (e.Character is null ? "" : $" ({e.Character.Name.Split(' ')[0]})") + (e.IsMe ? ", tu" : "");
        g.DrawString(who, Mono(s, 5.6f), Gfx.Brush(Ink), box.X + 19 * s, box.Y + 5 * s);
        if (block is null) return;
        var (label, color) = Overlays.EndingLabel(block.EndingType);
        g.DrawString(label, Mono(s, 5f), Gfx.Brush(color), box.X + 19 * s, box.Y + 11.5f * s);
        Wrap(g, block.EndingTitle ?? block.Title, FontCache.Px("Georgia", 8f, s, FontStyle.Bold), Ink,
            new RectangleF(box.X + 4 * s, box.Y + 19 * s, box.Width - 8 * s, 20 * s));
        string summary = string.Join("  ", story.Properties.Where(p => p.Display == HudDisplay.Value && !p.Shared)
            .Select(p => $"{p.HudLabel}: {p.FormatValue(e.Engine.State.Get(p.Key))}"));
        Wrap(g, $"{e.Engine.Path.Count - 1} pași, ora {block.Time}. {summary}", Body(s, 4.6f), Muted,
            new RectangleF(box.X + 4 * s, box.Y + 40 * s, box.Width - 8 * s, 14 * s));
        Wrap(g, Excerpt(block.Text), Body(s, 4.4f), Dim, new RectangleF(box.X + 4 * s, box.Y + 53 * s, box.Width - 8 * s, 16 * s));
    }

    /// <summary>Prima fraza a textului de final (fara paragrafele insotitorilor).</summary>
    private static string Excerpt(string text)
    {
        text = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
        int dot = text.IndexOfAny(['.', '!', '?']);
        return dot > 0 && dot < 140 ? text[..(dot + 1)] : text.Length > 140 ? text[..137] + "..." : text;
    }

    private static void DrawButtons(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuScreen m)
    {
        float x = Left;
        var font = Mono(s, 6.2f);
        for (int i = 0; i < m.Items.Count; i++)
        {
            var it = m.Items[i];
            float bw = g.MeasureString(it.Label, font).Width / s + 14;
            var r = R(view, s, x, 152, bw, 11);
            hits.Add(r);
            bool sel = i == m.Selected;
            g.FillRectangle(Gfx.Brush(sel ? 140 : 70, it.Enabled ? 140 : 60, 24, 30), r);
            if (sel) g.FillRectangle(Gfx.Brush(Neon), r.X, r.Y, 1.2f * s, r.Height);
            g.DrawString(it.Label, font, Gfx.Brush(!it.Enabled ? Dim : sel ? Ink : Muted), r.X + 7 * s, r.Y + 1.8f * s);
            x += bw + 5;
        }
        Footer(g, view, s, m.Current?.Hint ?? "Săgeți: alegi    Enter: confirmi");
    }
}
