using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Game;
using LastStopNight.Rendering;
using static LastStopNight.Ui.MenuStyle;

namespace LastStopNight.Ui.Views;

/// <summary>
/// Ecranele de alegere dinaintea turei: povestile (cartonase cu miniatura), insotitorul si galeria de finaluri.
/// Coordonatele sunt in unitatile lumii (320x180), inmultite cu scara <c>s</c>.
/// </summary>
public static class StoryPickerViews
{
    private const float StoryCardMaxWidth = 88, StoryCardHeight = 122, StoryCardGap = 7, StoryCardsWidth = 300;
    private const float CompanionCardWidth = 64, CompanionCardGap = 6, CompanionCardsLeft = 122, CharacterCardHeight = 110;

    /// <summary>
    /// Cartonasele povestilor (plus unul „Din fisier...” daca meniul are mai multe optiuni decat povesti).
    /// <paramref name="thumbnail"/> da fundalul blocului de start al fiecarei povesti.
    /// </summary>
    public static void DrawStoryCards(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuScreen m,
                                      IReadOnlyList<StoryPackage> stories, Func<StoryPackage, Bitmap?> thumbnail, EndingsTracker endings)
    {
        Screen2D.RequestTint(1, 150, Color.Black);
        Logo(g, view, s, m.Title, null, m.Time, 18, 10);
        int n = m.Items.Count;
        float cw = Math.Min(StoryCardMaxWidth, (StoryCardsWidth - StoryCardGap * (n - 1)) / n);
        float x0 = (320 - (cw * n + StoryCardGap * (n - 1))) / 2;
        for (int i = 0; i < n; i++)
        {
            bool sel = i == m.Selected;
            var r = R(view, s, x0 + i * (cw + StoryCardGap), 40 - (sel ? 3 : 0), cw, StoryCardHeight);
            hits.Add(r);
            Card(g, r, s, sel);
            if (i < stories.Count) DrawStoryCard(g, s, r, cw, stories[i], thumbnail(stories[i]), endings, sel);
            else
            {
                // titlul poate avea doua randuri cand sunt multe povesti (cartonase inguste): textul de sub el vine dupa el
                var titleFont = Mono(s, 7f);
                var titleBox = new RectangleF(r.X + 6 * s, r.Y + 50 * s, r.Width - 12 * s, 30 * s);
                float titleH = g.MeasureString("Din fișier...", titleFont, (int)titleBox.Width).Height;
                Wrap(g, "Din fișier...", titleFont, sel ? Ink : Muted, titleBox);
                Wrap(g, "o poveste .zip făcută în Editor", Body(s, 5.4f), Dim,
                    new RectangleF(r.X + 6 * s, titleBox.Y + titleH + 2 * s, r.Width - 12 * s, 30 * s));
            }
        }
        Footer(g, view, s, "Stânga / dreapta: alegi povestea    Enter: continui    Esc: înapoi");
    }

    private static void DrawStoryCard(Graphics g, float s, RectangleF r, float cw, StoryPackage pkg, Bitmap? thumb,
                                      EndingsTracker endings, bool selected)
    {
        var story = pkg.Story;
        if (thumb is not null)
        {
            var tr = new RectangleF(r.X + 4 * s, r.Y + 4 * s, r.Width - 8 * s, (r.Width - 8 * s) * 9 / 16);
            var im = g.InterpolationMode;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.DrawImage(thumb, tr);
            g.InterpolationMode = im;
            if (!selected) g.FillRectangle(Gfx.Brush(110, 0, 0, 0), tr);
        }
        // titlu, autor si descriere unul sub altul, dupa cat ocupa fiecare
        float y = r.Y + (4 + (cw - 8) * 9 / 16 + 3) * s, tw = r.Width - 10 * s;
        var tf = Mono(s, 6.4f);
        var th = g.MeasureString(story.Title, tf, (int)tw).Height;
        Wrap(g, story.Title, tf, selected ? Neon : Ink, new RectangleF(r.X + 5 * s, y, tw, th + 1));
        y += th;
        var af = FontCache.Px("Georgia", 5.2f, s, FontStyle.Italic);
        var ah = g.MeasureString(story.Author, af, (int)tw).Height;
        Wrap(g, story.Author, af, Muted, new RectangleF(r.X + 5 * s, y, tw, ah + 1));
        y += ah + 2 * s;
        Wrap(g, story.Description, Body(s, 4.9f), selected ? Ink : Muted, new RectangleF(r.X + 5 * s, y, tw, r.Bottom - 13 * s - y));
        int found = endings.Count(story.Title), total = story.Blocks.Count(b => b.IsEnding);
        g.DrawString($"Finaluri: {found}/{total}", Mono(s, 5.4f), Gfx.Brush(found == total ? Warm : Muted), r.X + 5 * s, r.Bottom - 10 * s);
    }

    /// <summary>Protagonistul (fix, „TU”) si optiunile: „Singur” sau unul dintre insotitori.</summary>
    public static void DrawCompanionChoice(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuScreen m,
                                           StoryDefinition story, List<PlayableDefinition> companions, ImageCache? images)
    {
        Screen2D.RequestTint(1, 150, Color.Black);
        Logo(g, view, s, m.Title, story.Title, m.Time, 18, 10);

        var hero = story.FindPlayable(story.PlayerCharacterId);
        var heroChar = story.FindCharacter(story.PlayerCharacterId);
        var heroRect = R(view, s, 16, 50, 84, CharacterCardHeight);
        Card(g, heroRect, s, false);
        g.DrawString("TU", Mono(s, 5.6f), Gfx.Brush(Neon), heroRect.X + 5 * s, heroRect.Y + 4 * s);
        CharacterCard.DrawPerson(g, s, images, heroChar, new PointF(heroRect.X + 30 * s, heroRect.Y + 10 * s), false, m.Time);
        CharacterCard.DrawText(g, s, heroRect, heroChar?.Name ?? story.PlayerCharacterId, hero?.Role ?? "Protagonist",
            hero?.Blurb ?? "Spune povestea.", true);
        g.DrawString("+", Title(s, 14), Gfx.Brush(Muted), view.X + 104 * s, view.Y + 94 * s);

        for (int i = 0; i < m.Items.Count; i++)
        {
            bool sel = i == m.Selected;
            var r = R(view, s, CompanionCardsLeft + i * (CompanionCardWidth + CompanionCardGap), 50 - (sel ? 3 : 0),
                CompanionCardWidth, CharacterCardHeight);
            hits.Add(r);
            Card(g, r, s, sel);
            if (i == 0)
            {
                DrawEmptySilhouette(g, s, r, sel);
                CharacterCard.DrawText(g, s, r, "Singur", "Fără însoțitor", "Așa cum a fost scrisă povestea. Nimeni nu-ți ține spatele.", sel);
                continue;
            }
            var pc = companions[i - 1];
            var c = story.FindCharacter(pc.CharacterId);
            CharacterCard.DrawPerson(g, s, images, c, new PointF(r.X + 20 * s, r.Y + 10 * s), sel, m.Time);
            CharacterCard.DrawText(g, s, r, c?.Name ?? pc.CharacterId, pc.Role, pc.Blurb, sel);
        }
        Footer(g, view, s, "Stânga / dreapta: alegi    Enter: începe tura    Esc: înapoi");
    }

    /// <summary>Silueta punctata de pe cartonasul „Singur”: nimeni langa tine.</summary>
    private static void DrawEmptySilhouette(Graphics g, float s, RectangleF card, bool selected)
    {
        var ghost = new RectangleF(card.X + 20 * s, card.Y + 12 * s, 24 * s, 46 * s);
        using var pen = new Pen(Color.FromArgb(selected ? 200 : 110, 150, 140, 132), Math.Max(1, 0.6f * s)) { DashPattern = [3, 3] };
        g.DrawEllipse(pen, ghost.X + 6 * s, ghost.Y, 12 * s, 12 * s);
        g.DrawRectangle(pen, ghost.X + 3 * s, ghost.Y + 14 * s, 18 * s, 30 * s);
    }

    /// <summary>Cate un cartonas pe poveste, cu toate finalurile ei: gasite (tip + titlu) sau „???”.</summary>
    public static void DrawEndingsGallery(Graphics g, RectangleF view, float s, List<RectangleF> hits, MenuScreen m,
                                          IReadOnlyList<StoryPackage> stories, EndingsTracker endings)
    {
        Screen2D.RequestTint(1, 175, Color.Black);
        Logo(g, view, s, m.Title, null, m.Time, 18, 10);
        int n = Math.Max(1, stories.Count);
        float cw = (296 - 6 * (n - 1)) / n;
        for (int i = 0; i < stories.Count; i++)
        {
            var story = stories[i].Story;
            var r = R(view, s, 12 + i * (cw + 6), 42, cw, 112);
            Card(g, r, s, false);
            g.DrawString(story.Title, Mono(s, 5.8f), Gfx.Brush(Ink), r.X + 4 * s, r.Y + 4 * s);
            float y = 16;
            foreach (var b in story.Blocks.Where(b => b.IsEnding))
            {
                bool found = endings.Has(story.Title, b.Id);
                var (label, color) = Overlays.EndingLabel(b.EndingType);
                g.DrawString(found ? label : "???", Mono(s, 4.4f), Gfx.Brush(found ? 255 : 110, color.R, color.G, color.B), r.X + 4 * s, r.Y + y * s);
                g.DrawString(found ? b.EndingTitle ?? b.Title : "nedescoperit", Body(s, 5.2f), Gfx.Brush(found ? Ink : Dim),
                    r.X + 4 * s, r.Y + (y + 4.5f) * s);
                y += 11.5f;
            }
        }
        BackButton(g, view, s, hits, 12, 160, 50);
    }
}
