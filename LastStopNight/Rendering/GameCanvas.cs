namespace LastStopNight.Rendering;

/// <summary>
/// Suprafata de joc. Lumea se deseneaza direct pe ecran printr-o transformare 320x180 → fereastra
/// (scalare „nearest neighbor”), deci personajele se pot misca la pozitii fractionare, fara sacadari.
/// Deasupra e o banda rezervata HUD-ului; peste lume se deseneaza textul la rezolutia ecranului.
/// </summary>
public sealed class GameCanvas : Control
{
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
        g.TranslateTransform(world.X, world.Y);
        g.ScaleTransform(ws, ws);
        Screen2D.SetPixelArt(g);
        DrawWorld?.Invoke(g);
        g.Restore(state);

        if (Cover)
        {
            // in afara zonei de meniu, scena e putin mai intunecata (meniul ramane in centru)
            using var region = new Region(new RectangleF(0, TopBand, ClientSize.Width, ClientSize.Height - TopBand));
            region.Exclude(view);
            g.FillRegion(Gfx.Brush(120, 0, 0, 0), region);
        }
        g.SetClip(view);
        Screen2D.SetSmooth(g);
        DrawOverlay?.Invoke(g, view, s);
        g.ResetClip();
    }
}
