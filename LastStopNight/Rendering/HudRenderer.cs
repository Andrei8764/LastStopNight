using System.Drawing.Drawing2D;
using LastStopNight.Core.Engine;
using LastStopNight.Core.Model;

namespace LastStopNight.Rendering;

/// <summary>Deseneaza HUD-ul pe baza proprietatilor definite in poveste (nimic nu e codat fix).</summary>
public static class HudRenderer
{
    /// <summary>
    /// HUD-ul ca banda deasupra scenei:
    /// - stanga: portretele echipei;
    /// - mijloc: proprietatile vizibile (bare segmentate, valori, flaguri), care clipesc si tremura cand se schimba;
    /// - dreapta: noaptea si ora, pe un afisaj digital.
    /// Totul vine din poveste, nimic nu e codat fix.
    /// </summary>
    /// <param name="flashes">Proprietatile schimbate de curand (cheie → 1..0), care clipesc si tremura.</param>
    /// <param name="nameOf">Numele de sub portret (in multiplayer, numele jucatorului); implicit prenumele personajului.</param>
    public static void DrawStrip(Graphics g, Rectangle band, StoryDefinition story, GameState state, StoryBlock? block,
                                 ImageCache images, IReadOnlyList<CharacterDefinition> party,
                                 IReadOnlyDictionary<string, float>? flashes = null,
                                 Func<CharacterDefinition, string>? nameOf = null)
    {
        Screen2D.SetSmooth(g);
        var strip = new StripPainter(g, band);
        strip.DrawBackground();
        float right = strip.DrawClock(story, state, block);
        float left = strip.DrawParty(party, images, nameOf);
        strip.DrawProperties(story, state, images, flashes, left, right);
    }

    /// <summary>
    /// Desenarea benzii HUD. Totul e proiectat pentru o banda inalta de 46 de pixeli si scalat cu <see cref="_k"/>,
    /// ca HUD-ul sa arate la fel la orice marime a ferestrei.
    /// </summary>
    private sealed class StripPainter(Graphics g, Rectangle band)
    {
        private const float DesignHeight = 46, IconWidth = 18, PropertyGap = 14;
        private const int BarSegments = 10;
        private readonly float _k = band.Height / DesignHeight;
        private readonly Font _label = FontCache.Get("Segoe UI", 6.6f * band.Height / DesignHeight, FontStyle.Bold);
        private readonly Font _small = FontCache.Get("Segoe UI", 6.4f * band.Height / DesignHeight);
        private readonly Font _value = FontCache.Get("Consolas", 11f * band.Height / DesignHeight, FontStyle.Bold);
        private readonly Brush _labelBrush = Gfx.Brush(255, 156, 142, 134);
        private readonly Brush _valueBrush = Gfx.Brush(255, 240, 230, 216);
        private readonly Brush _dimBrush = Gfx.Brush(255, 90, 82, 80);

        public void DrawBackground()
        {
            using (var lg = new LinearGradientBrush(new PointF(0, band.Top), new PointF(0, band.Bottom + 1),
                       Color.FromArgb(26, 17, 20), Color.FromArgb(11, 7, 9)))
                g.FillRectangle(lg, band);
            g.DrawLine(Gfx.Pen(Color.FromArgb(40, 255, 255, 255), 1), band.Left, band.Top, band.Right, band.Top);
            g.FillRectangle(Gfx.Brush(255, 96, 20, 28), band.Left, band.Bottom - 2 * _k, band.Width, 2 * _k);
        }

        /// <summary>Ceasul digital (cu „88:88” stins in spate, ca la un afisaj real) si noaptea. Intoarce marginea lor din stanga.</summary>
        public float DrawClock(StoryDefinition story, GameState state, StoryBlock? block)
        {
            float right = band.Right - 10 * _k;
            if (block is null || string.IsNullOrWhiteSpace(block.Time)) return right;
            var lcd = FontCache.Get("Consolas", 15f * _k, FontStyle.Bold);
            var ghost = g.MeasureString("88:88", lcd);
            var box = new RectangleF(right - ghost.Width - 10 * _k, band.Y + 6 * _k, ghost.Width + 10 * _k, 30 * _k);
            FillRounded(g, Gfx.Brush(255, 6, 5, 5), box, 3 * _k);
            DrawRounded(g, Gfx.Pen(Color.FromArgb(70, 34, 30), Math.Max(1, _k)), box, 3 * _k);
            float ty = box.Y + (box.Height - ghost.Height) / 2;
            g.DrawString("88:88", lcd, Gfx.Brush(26, 255, 170, 70), box.X + 5 * _k, ty);
            g.DrawString(block.Time, lcd, Gfx.Brush(30, 255, 150, 50), box.X + 5 * _k + 0.8f * _k, ty + 0.8f * _k);
            g.DrawString(block.Time, lcd, Gfx.Brush(255, 255, 186, 92), box.X + 5 * _k, ty);
            right = box.X - 8 * _k;
            if (story.FindProperty("story.night") is { } night && state.Has(night.Key))
            {
                string text = $"NOAPTEA {state.Get(night.Key)}/{night.Max}";
                var size = g.MeasureString(text, _label);
                g.DrawString(text, _label, _labelBrush, right - size.Width, band.Y + (band.Height - size.Height) / 2);
                right -= size.Width + 10 * _k;
            }
            return right;
        }

        /// <summary>Portretele echipei, cu numele dedesubt, si o linie despartitoare. Intoarce x-ul de dupa ele.</summary>
        public float DrawParty(IReadOnlyList<CharacterDefinition> party, ImageCache images, Func<CharacterDefinition, string>? nameOf)
        {
            float x = band.X + 8 * _k;
            foreach (var c in party)
            {
                var box = new RectangleF(x, band.Y + 5 * _k, 26 * _k, 26 * _k);
                FillRounded(g, Gfx.Brush(255, 40, 28, 32), box, 3 * _k);
                if (Portraits.Head(images, c) is { } head)
                    DrawPixelated(() => g.DrawImage(head, box.X + 1 * _k, box.Y + 1 * _k, box.Width - 2 * _k, box.Height - 2 * _k));
                DrawRounded(g, Gfx.Pen(Color.FromArgb(140, 60, 66), Math.Max(1, _k)), box, 3 * _k);
                var name = nameOf?.Invoke(c) ?? c.Name.Split(' ')[0];
                var size = g.MeasureString(name, _small);
                g.DrawString(name, _small, _labelBrush, box.X + (box.Width - size.Width) / 2, box.Bottom + 0.5f * _k);
                x = box.Right + 6 * _k;
            }
            if (party.Count > 0)
            {
                g.DrawLine(Gfx.Pen(Color.FromArgb(70, 40, 46), Math.Max(1, _k)), x + 2 * _k, band.Y + 8 * _k, x + 2 * _k, band.Bottom - 10 * _k);
                x += 12 * _k;
            }
            return x;
        }

        /// <summary>Proprietatile vizibile intre <paramref name="left"/> si <paramref name="right"/>; barele impart spatiul ramas.</summary>
        public void DrawProperties(StoryDefinition story, GameState state, ImageCache images,
                                   IReadOnlyDictionary<string, float>? flashes, float left, float right)
        {
            var props = story.Properties.Where(p => p.Display != HudDisplay.Hidden && state.Has(p.Key)).ToList();
            float fixedWidth = props.Where(p => p.Display != HudDisplay.Bar)
                .Sum(p => (IconWidth + PropertyGap) * _k + Math.Max(g.MeasureString(p.HudLabel.ToUpperInvariant(), _label).Width,
                                                                    g.MeasureString(p.FormatValue(p.Max), _value).Width));
            int bars = props.Count(p => p.Display == HudDisplay.Bar);
            float barWidth = bars == 0 ? 0
                : Math.Clamp((right - left - fixedWidth) / bars - (IconWidth + PropertyGap + 20) * _k, 40 * _k, 104 * _k);

            float x = left;
            foreach (var p in props)
            {
                float flash = flashes is not null && flashes.TryGetValue(p.Key, out var f) ? f : 0;
                x = DrawProperty(p, state.Get(p.Key), images, flash, barWidth, x);
            }
        }

        /// <summary>O proprietate: iconita, eticheta si bara / valoare / flag. Intoarce x-ul pentru urmatoarea.</summary>
        private float DrawProperty(StatePropertyDefinition p, int value, ImageCache images, float flash, float barWidth, float x)
        {
            float shake = flash > 0 ? MathF.Sin(flash * 60) * 2.2f * _k * flash : 0;
            bool lit = p.Display != HudDisplay.Flag || value != 0;
            if (images.Get(p.Icon) is { } icon) DrawIcon(icon, x, shake, lit);
            float tx = x + IconWidth * _k + shake;
            string label = p.HudLabel.ToUpperInvariant();
            g.DrawString(label, _label, lit ? _labelBrush : _dimBrush, tx, band.Y + 6 * _k);
            float labelWidth = g.MeasureString(label, _label).Width;
            float width;
            switch (p.Display)
            {
                case HudDisplay.Bar:
                    DrawSegmentedBar(p, value, flash, new RectangleF(tx, band.Y + 20 * _k, barWidth, 9 * _k));
                    width = Math.Max(barWidth + 20 * _k, labelWidth);
                    break;
                case HudDisplay.Flag:
                    g.DrawString(value != 0 ? "DA" : "nu", _value, value != 0 ? _valueBrush : _dimBrush, tx, band.Y + 17 * _k);
                    width = Math.Max(labelWidth, g.MeasureString(p.FormatValue(p.Max), _value).Width);
                    break;
                default:
                    g.DrawString(p.FormatValue(value), _value, flash > 0 ? Gfx.Brush(255, 255, 230, 150) : _valueBrush, tx, band.Y + 17 * _k);
                    width = Math.Max(labelWidth, g.MeasureString(p.FormatValue(p.Max), _value).Width);
                    break;
            }
            return tx - shake + width + PropertyGap * _k;
        }

        /// <summary>Iconita proprietatii; un flag stins (0) e desenat estompat.</summary>
        private void DrawIcon(Bitmap icon, float x, float shake, bool lit)
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            if (lit) g.DrawImage(icon, x + shake, band.Y + 13 * _k, 16 * _k, 16 * _k);
            else
            {
                using var dim = Screen2D.AlphaTint(0.35f, 0.6f, 0.6f, 0.6f);
                g.DrawImage(icon, [new PointF(x, band.Y + 13 * _k), new PointF(x + 16 * _k, band.Y + 13 * _k), new PointF(x, band.Y + 29 * _k)],
                    new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel, dim);
            }
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.PixelOffsetMode = PixelOffsetMode.Default;
        }

        /// <summary>Bara din segmente (ultimul segment partial e mai transparent); la o schimbare clipeste alb.</summary>
        private void DrawSegmentedBar(StatePropertyDefinition p, int value, float flash, RectangleF bar)
        {
            var color = Screen2D.ParseColor(p.Color, Color.IndianRed);
            float fraction = p.Max == p.Min ? 0 : (value - p.Min) / (float)(p.Max - p.Min);
            float segmentWidth = bar.Width / BarSegments;
            g.FillRectangle(Gfx.Brush(255, 8, 6, 7), bar.X - 1 * _k, bar.Y - 1 * _k, bar.Width + 1 * _k, bar.Height + 2 * _k);
            for (int i = 0; i < BarSegments; i++)
            {
                float fill = Math.Clamp(fraction * BarSegments - i, 0, 1);
                var seg = new RectangleF(bar.X + i * segmentWidth, bar.Y, segmentWidth - 1 * _k, bar.Height);
                g.FillRectangle(Gfx.Brush(255, 44, 34, 38), seg);
                if (fill <= 0) continue;
                g.FillRectangle(Gfx.Brush(Color.FromArgb((int)(255 * (0.35f + 0.65f * fill)), color)), seg);
                g.FillRectangle(Gfx.Brush(70, 255, 255, 255), seg.X, seg.Y, seg.Width, 2 * _k);
                g.FillRectangle(Gfx.Brush(60, 0, 0, 0), seg.X, seg.Bottom - 2 * _k, seg.Width, 2 * _k);
            }
            if (flash > 0) g.FillRectangle(Gfx.Brush((int)(170 * flash), 255, 255, 255), bar);
            g.DrawString(value.ToString(), _label, flash > 0 ? Gfx.Brush(255, 255, 240, 200) : _valueBrush, bar.Right + 3 * _k, bar.Y - 0.5f * _k);
        }

        /// <summary>Deseneaza cu pixeli clari (pixel art marit), apoi revine la desenarea neteda.</summary>
        private void DrawPixelated(Action draw)
        {
            var im = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            draw();
            g.InterpolationMode = im;
            g.PixelOffsetMode = PixelOffsetMode.Default;
        }
    }

    /// <summary>
    /// Indiciul de langa obiect: o tasta „E” mica si numele actiunii, fara cutie peste scena.
    /// Apare lin (fade) si sta deasupra obiectului, nu peste personaj.
    /// </summary>
    public static void DrawHint(Graphics g, RectangleF view, float s, PointF anchor, string label, bool enabled, bool look, float fade)
    {
        int a = (int)(255 * Math.Clamp(fade, 0, 1));
        if (a <= 4) return;
        var font = FontCache.Px("Segoe UI", 6f, s, FontStyle.Bold);
        var keyFont = FontCache.Px("Consolas", 5.6f, s, FontStyle.Bold);
        var size = g.MeasureString(label, font);
        float key = 8 * s, gap = 2.5f * s;
        float total = (enabled ? key + gap : 0) + size.Width;
        float cx = view.X + anchor.X * s;
        float y = view.Y + (anchor.Y - 12 - (1 - fade) * 3) * s - key;
        if (y < view.Y + 2 * s) y = view.Y + (anchor.Y + 12) * s; // prea sus: sub obiect
        float x = Math.Clamp(cx - total / 2, view.X + 3 * s, view.Right - total - 3 * s);

        // umbra moale in spatele textului, ca sa se citeasca pe orice fundal
        FillRounded(g, Gfx.Brush(a * 110 / 255, 0, 0, 0), new RectangleF(x - 2 * s, y - 1 * s, total + 4 * s, key + 2 * s), 3 * s);
        if (enabled)
        {
            var kr = new RectangleF(x, y, key, key);
            FillRounded(g, Gfx.Brush(a * 235 / 255, 236, 228, 214), kr, 1.6f * s);
            FillRounded(g, Gfx.Brush(a * 235 / 255, 190, 180, 166), new RectangleF(kr.X, kr.Bottom - 1.6f * s, kr.Width, 1.6f * s), 0.8f * s);
            var ks = g.MeasureString("E", keyFont);
            g.DrawString("E", keyFont, Gfx.Brush(a, 30, 22, 24), kr.X + (key - ks.Width) / 2, kr.Y + (key - 1.2f * s - ks.Height) / 2);
            x += key + gap;
        }
        var col = !enabled ? Color.FromArgb(a, 170, 170, 180) : look ? Color.FromArgb(a, 196, 226, 255) : Color.FromArgb(a, 255, 226, 170);
        float ty = y + (key - size.Height) / 2;
        g.DrawString(label, font, Gfx.Brush(a * 200 / 255, 0, 0, 0), x + 0.6f * s, ty + 0.6f * s);
        g.DrawString(label, font, Gfx.Brush(col), x, ty);
    }

    public static void DrawCenteredText(Graphics g, string text, Font font, Color color, RectangleF view, float y, float s, bool shadow = true)
    {
        var size = g.MeasureString(text, font);
        float x = view.X + (view.Width - size.Width) / 2;
        if (shadow) g.DrawString(text, font, Gfx.Brush(color.A, 0, 0, 0), x + s * 0.8f, y + s * 0.8f);
        g.DrawString(text, font, Gfx.Brush(color), x, y);
    }

    public static GraphicsPath Rounded(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = Math.Max(1, rad * 2);
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void FillRounded(Graphics g, Brush b, RectangleF r, float rad)
    {
        using var p = Rounded(r, rad);
        g.FillPath(b, p);
    }

    public static void DrawRounded(Graphics g, Pen pen, RectangleF r, float rad)
    {
        using var p = Rounded(r, rad);
        g.DrawPath(pen, p);
    }
}
