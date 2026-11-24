using LastStopNight.Rendering;

namespace LastStopNight.Ui;

/// <summary>
/// Buton de decizie desenat de mana: nu fura focusul tastaturii (A/D/E raman la joc),
/// textul se rupe pe mai multe randuri, iconita sta in dreapta.
/// </summary>
public sealed class ChoiceButton : Button
{
    private bool _hover, _down;

    public ChoiceButton()
    {
        SetStyle(ControlStyles.Selectable, false);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Font = UiScale.Font("Segoe UI", 10.5f);
        Height = UiScale.Px(44);
        Margin = new Padding(0, 0, 0, UiScale.Px(6));
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

    /// <summary>Numarul tastei (1-9), desenat ca o tasta in stanga; null = fara tasta.</summary>
    public int? Number { get; set; }

    /// <summary>Multiplayer: portretele jucatorilor care au votat decizia asta.</summary>
    public List<Image> Voters { get; } = new();

    /// <summary>Ajusteaza inaltimea dupa text (decizii lungi pe doua randuri).</summary>
    public void FitHeight(int width)
    {
        Width = width;
        var size = TextRenderer.MeasureText(Text, Font, new Size(Math.Max(50, width - TextLeft - UiScale.Px(44)), 1000), TextFormatFlags.WordBreak);
        Height = Math.Max(UiScale.Px(40), size.Height + UiScale.Px(18));
    }

    private int TextLeft => UiScale.Px(Number is null ? 14 : 42);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var back = !Enabled ? Color.FromArgb(18, 14, 15)
            : _down ? Color.FromArgb(92, 22, 30)
            : _hover ? Color.FromArgb(54, 20, 25)
            : Color.FromArgb(26, 18, 20);
        g.Clear(Parent?.BackColor ?? Color.Black);
        var box = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
        float k = UiScale.Value, radius = 4 * k;
        HudRenderer.FillRounded(g, Gfx.Brush(back), box, radius);
        var border = Enabled ? (_hover ? Color.FromArgb(200, 54, 62) : Color.FromArgb(80, 30, 36)) : Color.FromArgb(46, 34, 36);
        HudRenderer.DrawRounded(g, Gfx.Pen(border, k), box, radius);
        if (_hover && Enabled) g.FillRectangle(Gfx.Brush(255, 214, 44, 56), 1, 6 * k, 3 * k, Height - 13 * k);

        if (Number is int n)
        {
            // tasta cu cifra, ca pe tastatura
            var key = new RectangleF(11 * k, (Height - 22 * k) / 2f, 22 * k, 22 * k);
            var face = Enabled ? Color.FromArgb(232, 224, 210) : Color.FromArgb(70, 62, 62);
            HudRenderer.FillRounded(g, Gfx.Brush(Enabled ? Color.FromArgb(150, 140, 126) : Color.FromArgb(46, 40, 40)), new RectangleF(key.X, key.Y + 2 * k, key.Width, key.Height), radius);
            HudRenderer.FillRounded(g, Gfx.Brush(face), key, radius);
            TextRenderer.DrawText(g, n.ToString(), UiScale.Font("Consolas", 10.5f, FontStyle.Bold), Rectangle.Round(key),
                Enabled ? Color.FromArgb(34, 24, 26) : Color.FromArgb(120, 110, 108), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        int right = Width - UiScale.Px(12);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; // iconitele sunt pixel art
        if (Image is not null)
        {
            int iw = UiScale.Px(Image.Width), ih = UiScale.Px(Image.Height);
            g.DrawImage(Image, Width - iw - UiScale.Px(12), (Height - ih) / 2, iw, ih);
            right -= iw + UiScale.Px(6);
        }
        foreach (var v in Voters)
        {
            int vw = UiScale.Px(v.Width), vh = UiScale.Px(v.Height);
            right -= vw;
            g.DrawImage(v, right, (Height - vh) / 2, vw, vh);
            right -= UiScale.Px(4);
        }
        if (Voters.Count > 0) HudRenderer.DrawRounded(g, Gfx.Pen(Color.FromArgb(230, 200, 120), 1.5f * k), box, radius);
        var tr = new Rectangle(TextLeft, 0, Math.Max(40, right - TextLeft - UiScale.Px(4)), Height);
        var color = Enabled ? Color.FromArgb(238, 228, 216) : Color.FromArgb(120, 110, 106);
        TextRenderer.DrawText(g, Text, Font, tr, color, TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
    }
}
