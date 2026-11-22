using System.Drawing.Drawing2D;
using LastStopNight.Rendering;

namespace LastStopNight.Ui;

/// <summary>
/// Stilul comun al meniurilor: fonturi, culori, logo cu neon, liste si cartonase.
/// Coordonatele sunt in unitatile lumii (320x180), inmultite cu scara ecranului.
/// </summary>
public static class MenuStyle
{
    public static readonly Color Neon = Color.FromArgb(226, 38, 50);
    public static readonly Color Ink = Color.FromArgb(238, 228, 214);
    public static readonly Color Muted = Color.FromArgb(150, 140, 132);
    public static readonly Color Dim = Color.FromArgb(90, 84, 80);
    public static readonly Color Warm = Color.FromArgb(255, 214, 150);
    public static readonly Color PanelLine = Color.FromArgb(120, 36, 42);

    public static RectangleF R(RectangleF view, float s, float x, float y, float w, float h) =>
        new(view.X + x * s, view.Y + y * s, w * s, h * s);

    public static Font Title(float s, float px = 22) => FontCache.Px("Georgia", px, s, FontStyle.Bold);
    public static Font Body(float s, float px = 6.2f) => FontCache.Px("Segoe UI", px, s);
    public static Font Mono(float s, float px = 7.5f, FontStyle st = FontStyle.Bold) => FontCache.Px("Consolas", px, s, st);

    /// <summary>Intuneca partea stanga a scenei, ca meniul sa se citeasca peste orice fundal.</summary>
    public static void Shade(Graphics g, RectangleF view, float width01 = 0.62f, int alpha = 225)
    {
        // desenate de GameCanvas in imaginea interna a lumii (vezi Screen2D.RequestTint), nu aici la rezolutia ecranului
        Screen2D.RequestTint(width01, alpha, Color.FromArgb(8, 5, 7));
        Screen2D.RequestTint(1, 70, Color.Black);
    }

    /// <summary>Titlul in stil firma de neon: stralucire rosie in spate si un palpait rar.</summary>
    public static float Logo(Graphics g, RectangleF view, float s, string title, string? subtitle, float time, float x = 22, float y = 16)
    {
        var f = Title(s, title.Length > 14 ? 17 : 22);
        var size = g.MeasureString(title, f);
        float px = view.X + x * s, py = view.Y + y * s;
        bool off = (int)(time * 7) % 53 == 0 || (int)(time * 7) % 53 == 2;
        int a = off ? 90 : 255;
        for (int i = 3; i >= 1; i--)
            g.DrawString(title, f, Gfx.Brush(off ? 10 : 22, 255, 40, 50), px - i * 0.7f * s, py - i * 0.5f * s);
        g.DrawString(title, f, Gfx.Brush(a, 0, 0, 0), px + 1.2f * s, py + 1.2f * s);
        g.DrawString(title, f, Gfx.Brush(a, Neon.R, Neon.G, Neon.B), px, py);
        float bottom = py + size.Height * 0.92f;
        if (!string.IsNullOrEmpty(subtitle))
        {
            var sf = FontCache.Px("Georgia", 7.5f, s, FontStyle.Italic);
            g.DrawString(subtitle, sf, Gfx.Brush(210, Ink.R, Ink.G, Ink.B), px + 1 * s, bottom - 2 * s);
            bottom += g.MeasureString(subtitle, sf).Height;
        }
        g.FillRectangle(Gfx.Brush(160, Neon.R, Neon.G, Neon.B), px + 1 * s, bottom + 1 * s, 46 * s, Math.Max(1, 0.8f * s));
        return (bottom - view.Y) / s + 4;
    }

    /// <summary>Sageata mica din fata optiunii selectate (desenata, nu caracter).</summary>
    public static void Arrow(Graphics g, float x, float y, float h, Color c)
    {
        g.FillPolygon(Gfx.Brush(c), [new PointF(x, y), new PointF(x + h * 0.6f, y + h / 2), new PointF(x, y + h)]);
    }

    /// <summary>Lista verticala standard (meniul principal, setari, pauza).</summary>
    public static void DrawList(Graphics g, RectangleF view, float s, MenuScreen m, List<RectangleF> hits)
    {
        Shade(g, view);
        float top = Logo(g, view, s, m.Title, m.Subtitle, m.Time);
        var font = Mono(s, 7.6f);
        var valFont = Mono(s, 7f, FontStyle.Regular);
        float y = Math.Max(top + 8, 64);
        float x = 26, w = 150;
        // randurile se strang cand sunt multe optiuni, ca ultima sa nu intre peste textul de jos
        float step = Math.Min(14, (view.Height / s - FooterSpace - y) / Math.Max(1, m.Items.Count));
        float h = Math.Min(12.5f, step - 0.5f);
        for (int i = 0; i < m.Items.Count; i++)
        {
            var it = m.Items[i];
            bool sel = i == m.Selected;
            var row = R(view, s, x - 6, y - 2, w + 12, h);
            hits.Add(row);
            if (sel)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(m.Time * 5);
                g.FillRectangle(Gfx.Brush(70 + (int)(30 * pulse), 120, 20, 28), row);
                g.FillRectangle(Gfx.Brush(Neon), row.X, row.Y, 1.5f * s, row.Height);
                Arrow(g, view.X + (x - 3.5f) * s, view.Y + (y + 1.6f) * s, 5 * s, Warm);
            }
            var col = !it.Enabled ? Dim : sel ? Ink : Muted;
            g.DrawString(it.Label, font, Gfx.Brush(col), view.X + (x + 2) * s, view.Y + y * s);
            if (it.Value is not null)
            {
                string v = it.Value();
                if (it.Adjust is not null && sel) v = "<  " + v + "  >";
                var vs = g.MeasureString(v, valFont);
                float vx = view.X + (x + w) * s - vs.Width;
                g.DrawString(v, valFont, Gfx.Brush(sel ? Warm : Muted), vx, view.Y + (y + 0.6f) * s);
                if (it.Adjust is not null) m.AddValueHit(i, new RectangleF(vx, row.Y, vs.Width, row.Height));
            }
            y += step;
        }
        Footer(g, view, s, m.Current?.Hint ?? DefaultHint(m));
    }

    public static string DefaultHint(MenuScreen m) =>
        m.Back is null ? "Săgeți sau mouse: alegi    Enter: confirmi" : "Săgeți sau mouse: alegi    Enter: confirmi    Esc: înapoi";

    /// <summary>Spatiul (unitati logice) lasat jos pentru textul de ajutor.</summary>
    private const float FooterSpace = 16;

    public static void Footer(Graphics g, RectangleF view, float s, string text)
    {
        var f = Body(s, 5.6f);
        g.DrawString(text, f, Gfx.Brush(170, Muted.R, Muted.G, Muted.B), view.X + 22 * s, view.Bottom - 12 * s);
    }

    /// <summary>Butonul „Inapoi” al ecranelor fara lista (mesaje, galerii); se adauga si la zonele de click.</summary>
    public static void BackButton(Graphics g, RectangleF view, float s, List<RectangleF> hits, float x, float y, float w)
    {
        var b = R(view, s, x, y, w, 12);
        hits.Add(b);
        g.FillRectangle(Gfx.Brush(90, 120, 20, 28), b);
        Arrow(g, b.X + 4 * s, b.Y + 3.5f * s, 5 * s, Warm);
        g.DrawString("Înapoi", Mono(s), Gfx.Brush(Ink), b.X + 10 * s, b.Y + 2 * s);
    }

    /// <summary>Cartonas cu rama; cel selectat e ridicat si are rama de neon.</summary>
    public static void Card(Graphics g, RectangleF r, float s, bool selected, bool enabled = true)
    {
        HudRenderer.FillRounded(g, Gfx.Brush(110, 0, 0, 0), new RectangleF(r.X + 2 * s, r.Y + 3 * s, r.Width, r.Height), 3 * s);
        HudRenderer.FillRounded(g, Gfx.Brush(selected ? 240 : 215, 20, 13, 16), r, 3 * s);
        var pen = Gfx.Pen(selected ? Neon : enabled ? PanelLine : Color.FromArgb(60, 46, 48), Math.Max(1, (selected ? 1 : 0.6f) * s));
        HudRenderer.DrawRounded(g, pen, r, 3 * s);
    }

    /// <summary>Text pe mai multe randuri intr-un dreptunghi.</summary>
    public static void Wrap(Graphics g, string text, Font f, Color c, RectangleF r)
    {
        // LineLimit: nu desena randuri taiate pe jumatate la marginea de jos
        using var fmt = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.LineLimit };
        g.DrawString(text, f, Gfx.Brush(c), r, fmt);
    }

    /// <summary>Un cadru dintr-un spritesheet, marit cu pixeli clari.</summary>
    public static void Sprite(Graphics g, Bitmap sheet, int frame, int fw, int fh, RectangleF dest, bool flip = false)
    {
        var im = g.InterpolationMode;
        var po = g.PixelOffsetMode;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        var src = new RectangleF(frame * fw, 0, fw, fh);
        if (flip) dest = new RectangleF(dest.Right, dest.Y, -dest.Width, dest.Height);
        g.DrawImage(sheet, new[] { new PointF(dest.Left, dest.Top), new PointF(dest.Right, dest.Top), new PointF(dest.Left, dest.Bottom) }, src, GraphicsUnit.Pixel);
        g.InterpolationMode = im;
        g.PixelOffsetMode = po;
    }
}
