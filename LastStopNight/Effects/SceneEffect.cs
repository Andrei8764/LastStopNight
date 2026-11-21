using System.Drawing.Drawing2D;
using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Effects;

/// <summary>
/// Efect vizual atasat unei scene. Poate desena in lumea 2D (coordonate logice 320x180)
/// si/sau peste ea, la rezolutia ecranului (text clar).
/// </summary>
public abstract class SceneEffect
{
    protected float Elapsed { get; private set; }
    public bool IsFinished { get; protected set; }

    private bool _started;

    public virtual void Update(float dt, Scene scene)
    {
        if (!_started)
        {
            _started = true;
            OnStart(scene);
        }
        Elapsed += dt;
    }

    /// <summary>La primul cadru al efectului (de obicei porneste sunetul lui).</summary>
    protected virtual void OnStart(Scene scene) { }
    public virtual void DrawWorld(Graphics g, Scene scene) { }
    public virtual void DrawOverlay(Graphics g, RectangleF view, float scale, Scene scene) { }

    protected static float Ease(float t) => 1 - MathF.Pow(1 - Math.Clamp(t, 0, 1), 3);
}

/// <summary>Intuneric cu „lanterna” in jurul personajului (scenele cu luminile stinse).</summary>
public static class Darkness
{
    public static void Draw(Graphics g, PointF center, float radius, int alpha)
    {
        using var path = new GraphicsPath { FillMode = FillMode.Alternate };
        path.AddRectangle(new RectangleF(-10, -10, Screen2D.Width + 20, Screen2D.Height + 20));
        path.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        g.FillPath(Gfx.Brush(alpha, 2, 2, 6), path);

        using var hole = new GraphicsPath();
        hole.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        using var pg = new PathGradientBrush(hole)
        {
            CenterColor = Color.FromArgb(0, 2, 2, 6),
            SurroundColors = [Color.FromArgb(alpha, 2, 2, 6)],
            FocusScales = new PointF(0.25f, 0.25f),
        };
        g.FillPath(pg, hole);
    }
}
