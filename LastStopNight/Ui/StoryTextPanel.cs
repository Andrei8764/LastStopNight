using LastStopNight.Rendering;

namespace LastStopNight.Ui;

/// <summary>Panoul cu textul scenei, afisat treptat (efect de masina de scris).</summary>
public sealed class StoryTextPanel : Control
{
    private string _fullText = "";
    private float _shown;
    private int _lastDrawn = -1;

    public StoryTextPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Color.FromArgb(14, 10, 12);
        Padding = new Padding(22, 14, 18, 10);
        Cursor = Cursors.Hand;
    }

    public string Heading { get; private set; } = "";
    public string SubHeading { get; private set; } = "";
    public float CharsPerSecond { get; set; } = 75;
    public bool IsComplete => _shown >= _fullText.Length;

    public void SetText(string heading, string subHeading, string text, bool instant = false)
    {
        Heading = heading;
        SubHeading = subHeading;
        _fullText = text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        _shown = instant ? _fullText.Length : 0;
        Invalidate();
    }

    public void Complete()
    {
        _shown = _fullText.Length;
        Invalidate();
    }

    public void Advance(float dt)
    {
        if (IsComplete) return;
        // pauza scurta dupa punct, ca la o citire normala
        int i = (int)_shown;
        float speed = i > 0 && i < _fullText.Length && ".!?".Contains(_fullText[i - 1]) ? CharsPerSecond * 0.25f : CharsPerSecond;
        _shown = Math.Min(_fullText.Length, _shown + dt * speed);
        if ((int)_shown != _lastDrawn) Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Complete();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var r = new Rectangle(Padding.Left, Padding.Top, Width - Padding.Horizontal, Height - Padding.Vertical);
        // marginea de „jurnal”: o linie rosie verticala, ca la un caiet
        float k = UiScale.Value;
        g.FillRectangle(Gfx.Brush(255, 120, 26, 34), Padding.Left - 12 * k, Padding.Top + 2 * k, 2 * k, Height - Padding.Vertical - 6 * k);
        var hFont = UiScale.Font("Georgia", 13f, FontStyle.Bold);
        var sFont = UiScale.Font("Consolas", 9f, FontStyle.Bold);
        var tFont = UiScale.Font("Georgia", 11.5f);
        TextRenderer.DrawText(g, Heading, hFont, r.Location, Color.FromArgb(224, 60, 66));
        var hs = TextRenderer.MeasureText(g, Heading, hFont);
        if (!string.IsNullOrEmpty(SubHeading))
        {
            var ss = TextRenderer.MeasureText(g, SubHeading, sFont);
            var chip = new RectangleF(r.X + hs.Width + 8 * k, r.Y + 3 * k, ss.Width + 6 * k, ss.Height + 2 * k);
            HudRenderer.FillRounded(g, Gfx.Brush(255, 34, 24, 26), chip, 4 * k);
            TextRenderer.DrawText(g, SubHeading, sFont, new Point((int)(chip.X + 3 * k), (int)(chip.Y + k)), Color.FromArgb(240, 180, 100));
        }
        g.DrawLine(Gfx.Pen(Color.FromArgb(60, 40, 44), k), r.X, r.Y + hs.Height + 3 * k, r.Right, r.Y + hs.Height + 3 * k);
        int gap = UiScale.Px(10);
        var body = new Rectangle(r.X, r.Y + hs.Height + gap, r.Width, r.Height - hs.Height - gap);
        _lastDrawn = (int)Math.Min(_fullText.Length, _shown);
        TextRenderer.DrawText(g, _fullText[.._lastDrawn], tFont, body, Color.FromArgb(228, 218, 204),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix);
        if (!IsComplete)
            TextRenderer.DrawText(g, "Space sau click: tot textul", UiScale.Font("Segoe UI", 8.5f),
                new Point(r.Right - UiScale.Px(170), Height - UiScale.Px(22)), Color.FromArgb(100, 90, 86));
        g.DrawLine(Gfx.Pen(Color.FromArgb(96, 20, 28), 2 * k), 0, k, Width, k);
    }
}
