using LastStopNight.Net;
using LastStopNight.Rendering;
using static LastStopNight.Ui.MenuStyle;

namespace LastStopNight.Ui.Views;

/// <summary>Ce arata gazda in lobby despre conectare: codul principal, alte coduri si o nota.</summary>
/// <param name="Title">Unde merge codul principal (ex. „COD (PRIN INTERNET)”).</param>
/// <param name="ExtraCodes">Alte coduri: reteaua locala, Tailscale, Radmin...</param>
public sealed record LobbyConnectionInfo(string Title, string Code, IReadOnlyList<string> ExtraCodes, string Note);

/// <summary>
/// Lobby-ul multiplayer: in stanga codurile (doar gazda) si jucatorii, in dreapta cartonasele personajelor
/// (cine l-a luat), dedesubt butoanele. Optiunile meniului sunt, in ordine: personajele, apoi butoanele.
/// </summary>
public static class LobbyView
{
    private const float CardWidth = 58, CardHeight = 104, CardGap = 6, CardsLeft = 156, ButtonsTop = 150;

    public static void Draw(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuScreen m, NetSession net,
                            ImageCache? images, LobbyConnectionInfo? connection)
    {
        Shade(g, view, 1f, 235);
        float top = Logo(g, view, s, m.Title, m.Subtitle, m.Time, 18, 10);
        var story = net.HasEngine ? net.Package!.Story : null;
        if (story is null || net.Lobby.Players.Count == 0)
        {
            Wrap(g, net.Status ?? "Aștept răspunsul gazdei...", Body(s, 7f), Warm, R(view, s, 22, top + 6, 260, 20));
            DrawButtons(g, view, s, hits, m, 0);
            return;
        }

        float y = top + 2;
        if (connection is not null) y = DrawConnection(g, view, s, connection, y);
        DrawPlayers(g, view, s, net, story, images, y, m.Time);

        for (int i = 0; i < story.Playable.Count; i++)
        {
            var pl = story.Playable[i];
            var c = story.FindCharacter(pl.CharacterId);
            var owner = net.Lobby.Players.FirstOrDefault(p => p.CharacterId == pl.CharacterId);
            bool mine = owner?.Id == net.LocalId;
            bool sel = i == m.Selected;
            var r = R(view, s, CardsLeft + i * (CardWidth + CardGap), 40 - (sel ? 3 : 0), CardWidth, CardHeight);
            hits.Add(r);
            Card(g, r, s, sel, owner is null || mine);
            CharacterCard.DrawPerson(g, s, images, c, new PointF(r.X + 17 * s, r.Y + 6 * s), sel, m.Time);
            g.DrawString(c?.Name ?? pl.CharacterId, Mono(s, 6.2f), Gfx.Brush(sel ? Ink : Muted), r.X + 4 * s, r.Y + 56 * s);
            g.DrawString(pl.CharacterId == story.PlayerCharacterId ? "protagonist" : "însoțitor",
                FontCache.Px("Georgia", 4.6f, s, FontStyle.Italic), Gfx.Brush(Warm), r.X + 4 * s, r.Y + 64 * s);
            Wrap(g, owner is null ? "liber" : mine ? "tu" : owner.Name, Mono(s, 5.4f), owner is null ? Muted : mine ? Neon : Ink,
                new RectangleF(r.X + 4 * s, r.Y + 72 * s, r.Width - 8 * s, 10 * s));
            Wrap(g, pl.Role, Body(s, 4.4f), Dim, new RectangleF(r.X + 4 * s, r.Y + 82 * s, r.Width - 8 * s, 20 * s));
        }
        DrawButtons(g, view, s, hits, m, story.Playable.Count);
    }

    /// <summary>Codul mare, codurile secundare si nota despre internet / firewall. Intoarce y-ul de sub ele.</summary>
    private static float DrawConnection(Graphics g, RectangleF view, float s, LobbyConnectionInfo info, float y)
    {
        g.DrawString(info.Title, Mono(s, 5.2f), Gfx.Brush(Muted), view.X + 20 * s, view.Y + y * s);
        g.DrawString(info.Code, Mono(s, 11f), Gfx.Brush(Warm), view.X + 20 * s, view.Y + (y + 6) * s);
        y += 19;
        foreach (var extra in info.ExtraCodes)
        {
            g.DrawString(extra, Mono(s, 5.6f), Gfx.Brush(Ink), view.X + 20 * s, view.Y + y * s);
            y += 7;
        }
        Wrap(g, info.Note, Body(s, 4.6f), Muted, R(view, s, 20, y + 1, 130, 30));
        return y + 32;
    }

    private static void DrawPlayers(Graphics g, RectangleF view, float s, NetSession net, Core.Model.StoryDefinition story,
                                    ImageCache? images, float y, float time)
    {
        g.DrawString("JUCĂTORI", Mono(s, 5.2f), Gfx.Brush(Muted), view.X + 20 * s, view.Y + y * s);
        y += 7;
        foreach (var p in net.Lobby.Players)
        {
            var c = story.FindCharacter(p.CharacterId);
            if (c is not null && images is not null && Portraits.Badge(images, c) is { } badge)
                g.DrawImage(badge, view.X + 20 * s, view.Y + y * s, 9 * s, 9 * s);
            string tag = p.IsHost ? "gazdă" : p.Ready ? "gata" : "alege...";
            g.DrawString(p.Name + (p.Id == net.LocalId ? " (tu)" : ""), Mono(s, 6f), Gfx.Brush(Ink), view.X + 32 * s, view.Y + (y + 0.5f) * s);
            g.DrawString($"{c?.Name ?? "fără personaj"}, {tag}", Body(s, 4.8f), Gfx.Brush(p.Ready || p.IsHost ? Warm : Muted),
                view.X + 32 * s, view.Y + (y + 6) * s);
            y += 14;
        }
        if (net.Lobby.Players.Count < Math.Max(1, story.MaxPlayers))
            g.DrawString("așteptăm încă un jucător...", Body(s, 5f), Gfx.Brush((int)(150 + 80 * MathF.Sin(time * 3)), Muted.R, Muted.G, Muted.B),
                view.X + 20 * s, view.Y + y * s);
    }

    private static void DrawOption(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuItem it, bool sel, float y)
    {
        var font = Mono(s, 5.6f);
        string text = $"{it.Label}: {it.Value!()}";
        if (sel && it.Enabled) text = $"{it.Label}:  <  {it.Value!()}  >";
        float w = g.MeasureString(text, font).Width / s + 12;
        var r = R(view, s, 20, y, w, 11);
        hits.Add(r);
        g.FillRectangle(Gfx.Brush(sel ? 140 : 60, it.Enabled ? 90 : 50, 30, 50), r);
        if (sel) g.FillRectangle(Gfx.Brush(Neon), r.X, r.Y, 1.2f * s, r.Height);
        g.DrawString(text, font, Gfx.Brush(!it.Enabled ? Muted : sel ? Ink : Warm), r.X + 6 * s, r.Y + 2.2f * s);
    }

    /// <summary>Butoanele de sub cartonase (Porneste / Gata / Copiaza / Iesi), pe un rand.</summary>
    private static void DrawButtons(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuScreen m, int first)
    {
        float x = CardsLeft, optionY = ButtonsTop;
        var font = Mono(s, 6.2f);
        for (int i = first; i < m.Items.Count; i++)
        {
            var it = m.Items[i];
            if (it.Value is not null)
            {
                // optiunile (ex. „Drumuri separate: Da”) stau in coloana din stanga, sub jucatori
                DrawOption(g, view, s, hits, it, i == m.Selected, optionY);
                optionY += 13;
                continue;
            }
            float w = g.MeasureString(it.Label, font).Width / s + 14;
            var r = R(view, s, x, ButtonsTop, w, 11);
            hits.Add(r);
            bool sel = i == m.Selected;
            g.FillRectangle(Gfx.Brush(sel ? 140 : 70, it.Enabled ? 140 : 60, 24, 30), r);
            if (sel) g.FillRectangle(Gfx.Brush(Neon), r.X, r.Y, 1.2f * s, r.Height);
            g.DrawString(it.Label, font, Gfx.Brush(!it.Enabled ? Dim : sel ? Ink : Muted), r.X + 7 * s, r.Y + 1.8f * s);
            x += w + 5;
        }
        Footer(g, view, s, m.Current?.Hint ?? DefaultHint(m));
    }
}
