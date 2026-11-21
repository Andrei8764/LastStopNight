namespace LastStopNight.Rendering;

/// <summary>Pensule si creioane refolosite.</summary>
public static class Gfx
{
    private static readonly Dictionary<int, SolidBrush> Brushes = new();
    private static readonly Dictionary<(int, float), Pen> Pens = new();

    public static SolidBrush Brush(Color c)
    {
        int k = c.ToArgb();
        if (!Brushes.TryGetValue(k, out var b)) Brushes[k] = b = new SolidBrush(c);
        return b;
    }

    public static SolidBrush Brush(int a, int r, int g, int b) => Brush(Color.FromArgb(Math.Clamp(a, 0, 255), r, g, b));

    public static Pen Pen(Color c, float width = 1)
    {
        var k = (c.ToArgb(), width);
        if (!Pens.TryGetValue(k, out var p)) Pens[k] = p = new Pen(c, width);
        return p;
    }
}
