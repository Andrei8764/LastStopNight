using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Effects;

/// <summary>Ploaie in prim-plan.</summary>
public sealed class RainEffect : SceneEffect
{
    private readonly Random _rnd = new(1);
    private readonly PointF[] _drops = new PointF[150];

    public RainEffect()
    {
        for (int i = 0; i < _drops.Length; i++)
            _drops[i] = new PointF(_rnd.Next(-20, Screen2D.Width + 20), _rnd.Next(0, Screen2D.Height));
    }

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        for (int i = 0; i < _drops.Length; i++)
        {
            var p = _drops[i];
            p.X -= 40 * dt;
            p.Y += 260 * dt;
            if (p.Y > Screen2D.Height) { p.Y = -6; p.X = _rnd.Next(0, Screen2D.Width + 40); }
            _drops[i] = p;
        }
    }

    public override void DrawWorld(Graphics g, Scene scene)
    {
        var pen = Gfx.Pen(Color.FromArgb(70, 170, 180, 210), 0.6f);
        foreach (var p in _drops) g.DrawLine(pen, p.X, p.Y, p.X - 1.5f, p.Y + 6);
    }
}

/// <summary>Praf care pluteste prin lumina lampilor (interioare).</summary>
public sealed class DustEffect : SceneEffect
{
    private readonly Random _rnd = new(5);
    private readonly (PointF P, float Phase, float Speed)[] _motes = new (PointF, float, float)[40];

    public DustEffect()
    {
        for (int i = 0; i < _motes.Length; i++)
            _motes[i] = (new PointF(_rnd.Next(0, Screen2D.Width), _rnd.Next(10, 150)), (float)_rnd.NextDouble() * 6, 2 + (float)_rnd.NextDouble() * 4);
    }

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        for (int i = 0; i < _motes.Length; i++)
        {
            var (p, ph, sp) = _motes[i];
            p.Y -= sp * dt * 0.6f;
            p.X += MathF.Sin(Elapsed * 0.7f + ph) * dt * 3;
            if (p.Y < 6) { p.Y = 150; p.X = _rnd.Next(0, Screen2D.Width); }
            _motes[i] = (p, ph, sp);
        }
    }

    public override void DrawWorld(Graphics g, Scene scene)
    {
        foreach (var (p, ph, _) in _motes)
        {
            int a = 30 + (int)(40 * (MathF.Sin(Elapsed * 2 + ph) + 1) / 2);
            g.FillRectangle(Gfx.Brush(a, 255, 230, 190), p.X, p.Y, 1, 1);
        }
    }
}

/// <summary>Ceata joasa care aluneca incet (exterioare).</summary>
public sealed class FogEffect : SceneEffect
{
    public override void DrawWorld(Graphics g, Scene scene)
    {
        var glow = Scene.Glow;
        using var attrs = Screen2D.AlphaTint(0.16f, 0.8f, 0.82f, 0.9f);
        var src = new RectangleF(0, 0, glow.Width, glow.Height);
        for (int i = 0; i < 7; i++)
        {
            float x = (Elapsed * (3 + i * 0.7f) + i * 60) % (Screen2D.Width + 200) - 150;
            float y = scene.FloorY - 30 + (i % 3) * 7;
            var pts = new[] { new PointF(x, y), new PointF(x + 180, y), new PointF(x, y + 34) };
            g.DrawImage(glow, pts, src, GraphicsUnit.Pixel, attrs);
        }
    }
}

/// <summary>Luminile palpaie la intervale aleatoare.</summary>
public sealed class FlickerEffect : SceneEffect
{
    private readonly Random _rnd = new();
    private float _next = 0.8f, _offUntil;

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (Elapsed >= _next)
        {
            _offUntil = Elapsed + 0.04f + (float)_rnd.NextDouble() * 0.14f;
            scene.PlaySound("buzz", 0.35f + (float)_rnd.NextDouble() * 0.25f, 0.9f + (float)_rnd.NextDouble() * 0.2f);
            _next = Elapsed + 0.25f + (float)_rnd.NextDouble() * (_rnd.Next(4) == 0 ? 0.3f : 2.6f);
        }
    }

    public override void DrawWorld(Graphics g, Scene scene)
    {
        if (Elapsed >= _offUntil) return;
        g.FillRectangle(Gfx.Brush(175, 0, 0, 0), -10, -10, Screen2D.Width + 20, Screen2D.Height + 20);
    }
}
