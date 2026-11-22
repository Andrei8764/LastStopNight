namespace LastStopNight.Rendering;

/// <summary>
/// Suprafata de joc. Lumea (320x180 logic) se deseneaza intr-o imagine de 2-3 ori mai mare (personajele se misca
/// tot la pozitii fractionare, fara sacadari), marita apoi pe ecran dintr-o singura operatie („nearest neighbor”).
/// Desenata direct la rezolutia ecranului, fiecare fundal, lumina si filtru se scala separat: pe ecran complet
/// (2560x1440) asta ducea la ~40 ms pe cadru. In ferestre mici, lumea se deseneaza tot direct.
/// Deasupra e o banda rezervata HUD-ului; peste lume se deseneaza textul la rezolutia ecranului.
/// </summary>
public sealed class GameCanvas : Control
{
    /// <summary>Cea mai mare rezolutie interna a lumii: 3 x 320x180 = 960x540.</summary>
    private const int MaxWorldScale = 3;

    private WorldBuffer? _worldBuffer;

    public GameCanvas()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Color.Black;
    }

    /// <summary>Inaltimea benzii de sus rezervate HUD-ului (nu acopera niciodata scena).</summary>
    public int TopBand { get; set; } = 46;

    /// <summary>
    /// Lumea acopera toata fereastra (fara benzi negre), taind marginile care nu incap; meniurile se
    /// deseneaza in continuare in dreptunghiul 16:9 din mijloc, ca sa nu iasa nimic din ecran.
    /// </summary>
    public bool Cover { get; set; }

    /// <summary>Desenarea lumii, in coordonate logice 320x180.</summary>
    public event Action<Graphics>? DrawWorld;

    /// <summary>Elemente desenate peste lume, la rezolutia ecranului (prompturi, text, carduri).</summary>
    public event Action<Graphics, RectangleF, float>? DrawOverlay;

    /// <summary>Desenarea benzii HUD de deasupra scenei.</summary>
    public event Action<Graphics, Rectangle>? DrawBand;

    public new float Scale => Math.Max(0.5f, Math.Min(ClientSize.Width / (float)Screen2D.Width, (ClientSize.Height - TopBand) / (float)Screen2D.Height));

    public RectangleF ViewRect
    {
        get
        {
            float s = Scale;
            float w = Screen2D.Width * s, h = Screen2D.Height * s;
            return new RectangleF(MathF.Round((ClientSize.Width - w) / 2), MathF.Round(TopBand + (ClientSize.Height - TopBand - h) / 2), w, h);
        }
    }

    /// <summary>Converteste un punct de pe ecran in coordonate logice ale lumii.</summary>
    public PointF ToWorld(Point p)
    {
        var v = ViewRect;
        return new PointF((p.X - v.X) / Scale, (p.Y - v.Y) / Scale);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var view = ViewRect;
        float s = Scale;

        // fundal negru doar in marginile din jurul scenei
        using (var region = new Region(ClientRectangle))
        {
            region.Exclude(view);
            g.FillRegion(Brushes.Black, region);
        }
        var band = new Rectangle(0, 0, ClientSize.Width, TopBand);
        g.FillRectangle(Gfx.Brush(255, 14, 10, 12), band);
        g.DrawLine(Gfx.Pen(Color.FromArgb(90, 20, 26), 2), 0, TopBand - 1, ClientSize.Width, TopBand - 1);
        DrawBand?.Invoke(g, band);

        var tints = Screen2D.TakeTints();
        var state = g.Save();
        var world = view;
        float ws = s;
        if (Cover)
        {
            ws = Math.Max(ClientSize.Width / (float)Screen2D.Width, (ClientSize.Height - TopBand) / (float)Screen2D.Height);
            world = new RectangleF((ClientSize.Width - Screen2D.Width * ws) / 2, TopBand + (ClientSize.Height - TopBand - Screen2D.Height * ws) / 2,
                                   Screen2D.Width * ws, Screen2D.Height * ws);
        }
        g.SetClip(Cover ? new RectangleF(0, TopBand, ClientSize.Width, ClientSize.Height - TopBand) : view);
        // zona meniului, in coordonatele lumii (cu Cover, lumea e mai mare decat zona meniului)
        var viewInWorld = new RectangleF((view.X - world.X) / ws, (view.Y - world.Y) / ws, view.Width / ws, view.Height / ws);
        int k = Math.Min(MaxWorldScale, (int)MathF.Floor(ws));
        if (k >= 2 && ws > k + 0.01f) DrawWorldBuffered(g, world, k, viewInWorld, tints);
        else
        {
            g.TranslateTransform(world.X, world.Y);
            g.ScaleTransform(ws, ws);
            Screen2D.SetPixelArt(g);
            DrawWorld?.Invoke(g);
            ApplyTints(g, viewInWorld, tints);
        }
        g.Restore(state);
        g.SetClip(view);
        Screen2D.SetSmooth(g);
        DrawOverlay?.Invoke(g, view, s);
        g.ResetClip();
    }

    /// <summary>Lumea in imaginea interna (de <paramref name="k"/> ori 320x180), apoi marita o singura data pe ecran.</summary>
    private void DrawWorldBuffered(Graphics g, RectangleF world, int k, RectangleF viewInWorld, IReadOnlyList<Screen2D.Tint> tints)
    {
        int w = Screen2D.Width * k, h = Screen2D.Height * k;
        if (_worldBuffer is null || _worldBuffer.Width != w || _worldBuffer.Height != h)
        {
            _worldBuffer?.Dispose();
            _worldBuffer = new WorldBuffer(w, h);
        }
        using (var bg = Graphics.FromImage(_worldBuffer.Bitmap))
        {
            bg.Clear(Color.Black);
            bg.ScaleTransform(k, k);
            Screen2D.SetPixelArt(bg);
            DrawWorld?.Invoke(bg);
            ApplyTints(bg, viewInWorld, tints);
        }
        var clip = Rectangle.Round(g.ClipBounds);
        _worldBuffer.StretchTo(g, Rectangle.Round(world), clip);
    }

    /// <summary>
    /// Intunecarile cerute de meniuri (<see cref="Screen2D.RequestTint"/>), desenate peste lume, la rezolutia ei.
    /// Cu Cover, scena din afara zonei meniului e si ea putin mai intunecata.
    /// </summary>
    private void ApplyTints(Graphics g, RectangleF view, IReadOnlyList<Screen2D.Tint> tints)
    {
        if (Cover)
        {
            using var region = new Region(new RectangleF(-1, -1, Screen2D.Width + 2, Screen2D.Height + 2));
            region.Exclude(view);
            g.FillRegion(Gfx.Brush(120, 0, 0, 0), region);
        }
        foreach (var t in tints)
        {
            if (t.Width01 >= 1)
            {
                g.FillRectangle(Gfx.Brush(t.Alpha, t.Color.R, t.Color.G, t.Color.B), view);
                continue;
            }
            var r = new RectangleF(view.X, view.Y, view.Width * t.Width01, view.Height);
            using var lg = new System.Drawing.Drawing2D.LinearGradientBrush(new PointF(r.Left - 1, 0), new PointF(r.Right + 1, 0),
                Color.FromArgb(t.Alpha, t.Color), Color.FromArgb(0, t.Color));
            g.FillRectangle(lg, r);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _worldBuffer?.Dispose();
        base.Dispose(disposing);
    }
}
