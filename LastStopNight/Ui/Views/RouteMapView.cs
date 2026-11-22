using LastStopNight.Core.Engine;
using LastStopNight.Core.Model;
using LastStopNight.Rendering;
using static LastStopNight.Ui.MenuStyle;

namespace LastStopNight.Ui.Views;

/// <summary>Un drum de pe harta: cine e pe el (nume + personaj), culoarea lui si motorul (starea).</summary>
public sealed record MapRoute(IReadOnlyList<(string Name, CharacterDefinition? Character)> Members, GameEngine Engine, bool IsMine);

/// <summary>
/// Harta turei (tasta H): in stanga arborele blocurilor prin care ati trecut, ramificat unde v-ati despartit,
/// cu fiecare jucator in punctul unde e acum si cate decizii neincercate au ramas in fiecare bloc; in dreapta,
/// cate un cartonas pe drum: cine e acolo, unde, la ce ora si cum ii merge (barele din HUD).
/// </summary>
public static class RouteMapView
{
    private static readonly Color[] RouteColors =
    [
        Color.FromArgb(255, 190, 90), Color.FromArgb(110, 200, 255), Color.FromArgb(150, 230, 140), Color.FromArgb(240, 130, 200),
    ];
    private static readonly Color Shared = Color.FromArgb(225, 215, 200);

    // zona arborelui si a cartonaselor (unitati logice, ecran 320x180)
    private const float TreeLeft = 16, TreeTop = 34, TreeBottom = 168, LaneWidth = 50, MaxRow = 11, MinRow = 7;
    private const float CardsLeft = 214, CardWidth = 96;

    public static Color ColorOf(int route) => RouteColors[route % RouteColors.Length];

    public static void Draw(Graphics g, RectangleF view, float s, StoryDefinition story, IReadOnlyList<MapRoute> routes,
                            ImageCache? images, float time)
    {
        Screen2D.RequestTint(1, 255, Color.FromArgb(12, 9, 11)); // opac: harta trebuie sa se citeasca peste orice scena
        g.DrawString("HARTA TUREI", Mono(s, 9f), Gfx.Brush(Neon), view.X + 14 * s, view.Y + 8 * s);
        g.DrawString((routes.Count > 1 ? $"{routes.Count} drumuri.  " : "") + "+2 = alegeri încă neîncercate.  H sau Esc: închide",
            Body(s, 4.6f), Gfx.Brush(Muted), view.X + 15 * s, view.Y + 20 * s);

        var tree = RouteTree.Build(routes.Select(r => r.Engine.Path).ToList());
        DrawTree(g, view, s, story, tree, routes, images, time);
        float y = 10;
        for (int i = 0; i < routes.Count; i++) y = DrawCard(g, view, s, story, routes[i], i, images, y) + 4;
    }

    private static void DrawTree(Graphics g, RectangleF view, float s, StoryDefinition story, List<RouteNode> tree,
                                 IReadOnlyList<MapRoute> routes, ImageCache? images, float time)
    {
        if (tree.Count == 0) return;
        int depth = tree.Max(n => n.Depth) + 1;
        float row = Math.Clamp((TreeBottom - TreeTop) / depth, MinRow, MaxRow);
        int visible = (int)((TreeBottom - TreeTop) / row);
        int first = Math.Max(0, depth - visible); // drumuri lungi: se vad ultimele randuri (unde sunteti acum)
        PointF At(RouteNode n) => new(view.X + (TreeLeft + n.Lane * LaneWidth) * s, view.Y + (TreeTop + (n.Depth - first) * row + 3) * s);

        foreach (var n in tree.Where(n => n.Parent is not null && n.Depth > first))
        {
            var a = At(n.Parent!);
            var b = At(n);
            g.DrawLine(Gfx.Pen(Color.FromArgb(200, NodeColor(n)), Math.Max(1, 0.8f * s)), a.X, a.Y, b.X, a.Y + (b.Y - a.Y) * 0.4f);
            g.DrawLine(Gfx.Pen(Color.FromArgb(200, NodeColor(n)), Math.Max(1, 0.8f * s)), b.X, a.Y + (b.Y - a.Y) * 0.4f, b.X, b.Y);
        }
        if (first > 0)
            g.DrawString("...", Body(s, 5f), Gfx.Brush(Muted), view.X + (TreeLeft - 2) * s, view.Y + (TreeTop - 6) * s);

        var labelFont = Body(s, 4.3f);
        foreach (var n in tree.Where(n => n.Depth >= first))
        {
            var p = At(n);
            var block = story.FindBlock(n.BlockId);
            var color = NodeColor(n);
            float r = 1.8f * s;
            g.FillEllipse(Gfx.Brush(color), p.X - r, p.Y - r, r * 2, r * 2);

            string title = block?.Title ?? n.BlockId;
            var textColor = block is { IsEnding: true } ? Overlays.EndingLabel(block.EndingType).Color : n.Current.Count > 0 ? Ink : Muted;
            int untried = Untried(block, n);
            string extra = untried > 0 ? $"  +{untried}" : "";
            float maxWidth = (LaneWidth - 6) * s;
            title = Fit(g, title, labelFont, maxWidth - g.MeasureString(extra, labelFont).Width);
            g.DrawString(title, labelFont, Gfx.Brush(textColor), p.X + 3 * s, p.Y - 3.4f * s);
            if (extra.Length > 0)
                g.DrawString(extra, labelFont, Gfx.Brush(Dim), p.X + 3 * s + g.MeasureString(title, labelFont).Width - s, p.Y - 3.4f * s);

            // cine e acum aici: inel care pulseaza si portretele
            if (n.Current.Count == 0) continue;
            float pulse = 2.6f + 0.8f * MathF.Sin(time * 4);
            g.DrawEllipse(Gfx.Pen(ColorOf(n.Current[0]), Math.Max(1, 0.7f * s)), p.X - pulse * s, p.Y - pulse * s, pulse * 2 * s, pulse * 2 * s);
            float bx = p.X - 12 * s;
            foreach (var route in n.Current)
                foreach (var (_, c) in routes[route].Members)
                {
                    if (c is null || images is null || Portraits.Badge(images, c) is not { } badge) continue;
                    g.DrawImage(badge, bx, p.Y - 4 * s, 7 * s, 7 * s);
                    bx -= 8 * s;
                }
        }

        Color NodeColor(RouteNode n) => n.Routes.Count > 1 ? Shared : ColorOf(n.Routes[0]);
    }

    /// <summary>Cate destinatii din bloc n-a incercat inca nimeni (pe harta: „+2”).</summary>
    private static int Untried(StoryBlock? block, RouteNode n)
    {
        if (block is null || block.IsEnding) return 0;
        return block.Decisions.Select(d => d.Target).Distinct().Count(t => n.Children.All(c => c.BlockId != t));
    }

    private static string Fit(Graphics g, string text, Font font, float width)
    {
        if (g.MeasureString(text, font).Width <= width) return text;
        while (text.Length > 3 && g.MeasureString(text + "...", font).Width > width) text = text[..^1];
        return text.TrimEnd() + "...";
    }

    /// <summary>Cartonasul unui drum. Intoarce y-ul de sub el.</summary>
    private static float DrawCard(Graphics g, RectangleF view, float s, StoryDefinition story, MapRoute route, int index,
                                  ImageCache? images, float y)
    {
        var e = route.Engine;
        var block = e.CurrentBlock;
        var bars = story.Properties.Where(p => p.Display == HudDisplay.Bar).ToList();
        var values = story.Properties.Where(p => p.Display == HudDisplay.Value).ToList();
        float height = 30 + bars.Count * 7 + (values.Count > 0 ? 7 : 0);
        var box = R(view, s, CardsLeft, y, CardWidth, height);
        HudRenderer.FillRounded(g, Gfx.Brush(210, 24, 18, 22), box, 3 * s);
        HudRenderer.DrawRounded(g, Gfx.Pen(Color.FromArgb(route.IsMine ? 230 : 120, ColorOf(index)), Math.Max(1, (route.IsMine ? 0.9f : 0.5f) * s)), box, 3 * s);
        g.FillRectangle(Gfx.Brush(ColorOf(index)), box.X, box.Y + 3 * s, 1.4f * s, box.Height - 6 * s);

        float x = CardsLeft + 4, ty = y + 3;
        foreach (var (_, c) in route.Members)
        {
            if (c is not null && images is not null && Portraits.Badge(images, c) is { } badge)
                g.DrawImage(badge, view.X + x * s, view.Y + ty * s, 8 * s, 8 * s);
            x += 9;
        }
        string who = string.Join(", ", route.Members.Select(m => m.Character is null ? m.Name : $"{m.Name} ({m.Character.Name.Split(' ')[0]})"));
        g.DrawString(Fit(g, who + (route.IsMine ? "  (tu)" : ""), Mono(s, 5f), (CardWidth - (x - CardsLeft) - 3) * s),
            Mono(s, 5f), Gfx.Brush(Ink), view.X + x * s, view.Y + (ty + 1.5f) * s);
        ty += 10;

        string where = block is null ? "" : block.IsEnding
            ? $"{Overlays.EndingLabel(block.EndingType).Label}: {block.EndingTitle ?? block.Title}"
            : $"{block.Title}, {story.FindLocation(block.Location)?.Name ?? ""}";
        var whereColor = block is { IsEnding: true } ? Overlays.EndingLabel(block.EndingType).Color : Warm;
        g.DrawString(Fit(g, where, Body(s, 4.6f), (CardWidth - 8) * s), Body(s, 4.6f), Gfx.Brush(whereColor), view.X + (CardsLeft + 4) * s, view.Y + ty * s);
        ty += 7;
        g.DrawString($"{block?.Time}, {e.Path.Count - 1} pași", Body(s, 4.2f), Gfx.Brush(Muted), view.X + (CardsLeft + 4) * s, view.Y + ty * s);
        ty += 8;

        foreach (var p in bars)
        {
            int v = e.State.Get(p.Key);
            float t = p.Max > p.Min ? (v - p.Min) / (float)(p.Max - p.Min) : 0;
            g.DrawString(Fit(g, p.HudLabel, Body(s, 4.2f), 30 * s), Body(s, 4.2f), Gfx.Brush(Muted), view.X + (CardsLeft + 4) * s, view.Y + (ty - 1) * s);
            var bar = R(view, s, CardsLeft + 36, ty + 0.5f, CardWidth - 52, 3.4f);
            g.FillRectangle(Gfx.Brush(60, 255, 255, 255), bar);
            var col = p.Color is { } hex ? Screen2D.ParseColor(hex, Warm) : Warm;
            g.FillRectangle(Gfx.Brush(col), bar.X, bar.Y, bar.Width * Math.Clamp(t, 0, 1), bar.Height);
            g.DrawString(p.FormatValue(v), Body(s, 4f), Gfx.Brush(Ink), bar.Right + 2 * s, view.Y + (ty - 1) * s);
            ty += 7;
        }
        if (values.Count > 0)
            g.DrawString(Fit(g, string.Join("   ", values.Select(p => $"{p.HudLabel}: {p.FormatValue(e.State.Get(p.Key))}")), Body(s, 4.2f), (CardWidth - 8) * s),
                Body(s, 4.2f), Gfx.Brush(Ink), view.X + (CardsLeft + 4) * s, view.Y + ty * s);
        return y + height;
    }
}
