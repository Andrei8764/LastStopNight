using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Effects;

/// <summary>Trecere din negru la intrarea intr-o scena noua.</summary>
public sealed class FadeInEffect(float duration = 0.5f) : SceneEffect
{
    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (Elapsed >= duration) IsFinished = true;
    }

    public override void DrawWorld(Graphics g, Scene scene) =>
        g.FillRectangle(Gfx.Brush((int)(255 * (1 - Ease(Elapsed / duration))), 0, 0, 0), -10, -10, Screen2D.Width + 20, Screen2D.Height + 20);
}

/// <summary>Bula cu ce observa personajul cand se uita la un obiect (tasta E).</summary>
public sealed class SpeechBubbleEffect : SceneEffect
{
    private readonly string _text;
    private readonly float _duration;
    private readonly float _delay;

    /// <param name="speaker">Cine vorbeste (null = jucatorul).</param>
    /// <param name="delay">Dupa cate secunde apare (ex. replica insotitorului, dupa a jucatorului).</param>
    public SpeechBubbleEffect(string text, Actors.Actor? speaker = null, float delay = 0)
    {
        _text = text;
        _duration = 2.5f + text.Length / 22f;
        _delay = delay;
        Speaker = speaker;
    }

    public Actors.Actor? Speaker { get; }

    /// <summary>Cat timp ramane pe ecran, inclusiv pauza de dinainte.</summary>
    public float TotalDuration => _delay + _duration;

    public void Dismiss() => IsFinished = true;

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (Elapsed >= _delay + _duration) IsFinished = true;
    }

    public override void DrawOverlay(Graphics g, RectangleF view, float scale, Scene scene)
    {
        if ((Speaker ?? scene.Player) is not { } p || Elapsed < _delay) return;
        float elapsed = Elapsed - _delay;
        float k = Math.Min(1, elapsed / 0.15f) * Math.Min(1, (_duration - elapsed) / 0.3f);
        int a = Math.Clamp((int)(235 * k), 0, 255);
        var font = FontCache.Get("Segoe UI", Math.Max(9, 4.6f * scale));
        float maxW = Math.Min(view.Width * 0.55f, 120 * scale);
        int shown = Math.Min(_text.Length, (int)(elapsed * 60) + 1);
        var visible = _text[..shown];
        var size = g.MeasureString(_text, font, (int)maxW);
        float bx = Math.Clamp(view.X + p.X * scale - size.Width / 2, view.X + 6, view.Right - size.Width - 18);
        float by = Math.Max(view.Y + 6, view.Y + (scene.FloorY - 58) * scale - size.Height - 14);
        var box = new RectangleF(bx - 8, by - 6, size.Width + 16, size.Height + 12);
        HudRenderer.FillRounded(g, Gfx.Brush(a, 240, 234, 220), box, 6);
        float tail = Math.Clamp(view.X + p.X * scale, box.Left + 10, box.Right - 10);
        g.FillPolygon(Gfx.Brush(a, 240, 234, 220), [new PointF(tail - 6, box.Bottom - 1), new PointF(tail + 6, box.Bottom - 1), new PointF(tail, box.Bottom + 8)]);
        g.DrawString(visible, font, Gfx.Brush(a, 30, 24, 22), new RectangleF(bx, by, size.Width + 2, size.Height + 4));
    }
}

/// <summary>Card de capitol pe ecran negru („NOAPTEA 2: Ceva nu e in regula”).</summary>
public sealed class ChapterCardEffect(string text) : SceneEffect
{
    private const float Hold = 2.0f, FadeOut = 1.0f;

    protected override void OnStart(Scene scene) => scene.PlaySound("chapter");

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (Elapsed >= Hold + FadeOut) IsFinished = true;
    }

    public override void DrawOverlay(Graphics g, RectangleF view, float scale, Scene scene)
    {
        float k = Elapsed < Hold ? 1 : 1 - (Elapsed - Hold) / FadeOut;
        g.FillRectangle(Gfx.Brush((int)(255 * k), 6, 4, 6), view);

        int split = text.IndexOf(": ", StringComparison.Ordinal);
        string main = split > 0 ? text[..split] : text;
        string? sub = split > 0 ? text[(split + 2)..] : null;
        float textA = Math.Clamp(Elapsed / 0.6f, 0, 1) * k;
        var big = FontCache.Px("Georgia", 26, scale, FontStyle.Bold);
        var small = FontCache.Px("Georgia", 12, scale, FontStyle.Italic);
        var sz = g.MeasureString(main, big);
        float cy = view.Y + view.Height / 2;
        g.DrawString(main, big, Gfx.Brush((int)(255 * textA), 210, 30, 40), view.X + (view.Width - sz.Width) / 2, cy - sz.Height);
        if (sub is not null)
        {
            var s2 = g.MeasureString(sub, small);
            g.DrawString(sub, small, Gfx.Brush((int)(255 * textA), 225, 215, 200), view.X + (view.Width - s2.Width) / 2, cy + 2 * scale);
        }
        float lw = 60 * scale * Ease(Elapsed / 1.2f);
        g.DrawLine(Gfx.Pen(Color.FromArgb((int)(160 * textA), 150, 20, 30), Math.Max(1, scale * 0.5f)),
            view.X + view.Width / 2 - lw, cy, view.X + view.Width / 2 + lw, cy);
    }
}
