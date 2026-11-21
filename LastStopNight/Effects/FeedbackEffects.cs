using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Effects;

/// <summary>Tremurul camerei (pericol, ranire).</summary>
/// <param name="rumble">Cu vuietul lui; fals cand sunetul vine din alta parte (lovitura).</param>
public sealed class ShakeEffect(float duration, float magnitude, bool rumble = true) : SceneEffect
{
    private readonly Random _rnd = new();

    protected override void OnStart(Scene scene)
    {
        if (rumble) scene.PlaySound("rumble", Math.Min(1, 0.4f + duration / 2));
    }

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (Elapsed >= duration) { IsFinished = true; scene.CameraOffset = PointF.Empty; return; }
        float m = magnitude * (1 - Elapsed / duration);
        scene.CameraOffset = new PointF(((float)_rnd.NextDouble() * 2 - 1) * m, ((float)_rnd.NextDouble() * 2 - 1) * m);
    }
}

/// <summary>Flash rosu la pierderea integritatii.</summary>
public sealed class DamageFlashEffect : SceneEffect
{
    private const float Duration = 0.8f;

    protected override void OnStart(Scene scene) => scene.PlaySound("hit");

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (Elapsed >= Duration) IsFinished = true;
    }

    public override void DrawWorld(Graphics g, Scene scene) =>
        g.FillRectangle(Gfx.Brush((int)(150 * (1 - Elapsed / Duration)), 200, 0, 0), -10, -10, Screen2D.Width + 20, Screen2D.Height + 20);
}

/// <summary>Telefonul de pe perete suna: unde sonore si un LED rosu in jurul unui punct al locatiei.</summary>
public sealed class PhoneRingEffect(PointF point) : SceneEffect
{
    private const float RingPeriod = 1.6f;
    private int _lastRing = -1;

    private bool Ringing => Elapsed % RingPeriod < 0.9f;

    /// <summary>Soneria porneste o data la fiecare ciclu (sunetul tine cat partea „aprinsa”).</summary>
    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        int ring = (int)(Elapsed / RingPeriod);
        if (ring == _lastRing) return;
        _lastRing = ring;
        scene.PlaySound("phone_ring", 0.8f);
    }

    public override void DrawWorld(Graphics g, Scene scene)
    {
        if (!Ringing) return;
        float t = Elapsed % 0.3f / 0.3f;
        var pen = Gfx.Pen(Color.FromArgb((int)(220 * (1 - t)), 255, 230, 160), 0.8f);
        for (int i = 0; i < 2; i++)
        {
            float r = 6 + t * 8 + i * 5;
            g.DrawArc(pen, point.X - r, point.Y - r, r * 2, r * 2, -50, 100);
            g.DrawArc(pen, point.X - r, point.Y - r, r * 2, r * 2, 130, 100);
        }
        g.FillRectangle(Gfx.Brush(Elapsed % 0.2f < 0.1f ? 220 : 60, 255, 60, 60), point.X + 3, point.Y - 8, 2, 2);
    }

    public override void DrawOverlay(Graphics g, RectangleF view, float scale, Scene scene)
    {
        if (!Ringing) return;
        var pos = new PointF(view.X + (point.X - 14) * scale, view.Y + (point.Y - 22) * scale);
        g.DrawString("RRRING", FontCache.Px("Consolas", 7, scale, FontStyle.Bold), Gfx.Brush(230, 255, 220, 150), pos);
    }
}

/// <summary>Text plutitor (ex. „-35 Integritate”) care urca si dispare.</summary>
public sealed class FloatingTextEffect(string text, Color color, PointF worldPos, float delay) : SceneEffect
{
    private const float Duration = 2.2f;

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (Elapsed >= Duration + delay) IsFinished = true;
    }

    public override void DrawOverlay(Graphics g, RectangleF view, float scale, Scene scene)
    {
        float t = (Elapsed - delay) / Duration;
        if (t < 0) return;
        int a = Math.Clamp((int)(255 * (t < 0.7f ? 1 : 1 - (t - 0.7f) / 0.3f)), 0, 255);
        var font = FontCache.Px("Consolas", 9, scale, FontStyle.Bold);
        var size = g.MeasureString(text, font);
        float x = Math.Clamp(view.X + worldPos.X * scale - size.Width / 2, view.X + 4, view.Right - size.Width - 4);
        float y = view.Y + (worldPos.Y - 18 * Ease(t)) * scale;
        g.DrawString(text, font, Gfx.Brush(a, 0, 0, 0), x + scale * 0.6f, y + scale * 0.6f);
        g.DrawString(text, font, Gfx.Brush(Color.FromArgb(a, color)), x, y);
    }
}
