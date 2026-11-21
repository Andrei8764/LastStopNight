namespace LastStopNight.Rendering;

/// <summary>Iconite mici, desenate pixel cu pixel, pentru butoanele de decizie.</summary>
public static class ButtonIcons
{
    private static readonly string[] Steps =
    [
        "....##....",
        "....##....",
        "...####...",
        "..#.##.#..",
        "....##....",
        "...#..#...",
        "..#....#..",
        ".#......#.",
    ];

    private static readonly string[] Lock =
    [
        "...####...",
        "..#....#..",
        "..#....#..",
        ".########.",
        ".###..###.",
        ".###..###.",
        ".########.",
        "..........",
    ];

    public static readonly Bitmap Walk = Make(Steps, Color.FromArgb(230, 200, 140));
    public static readonly Bitmap Locked = Make(Lock, Color.FromArgb(150, 140, 140));

    private static Bitmap Make(string[] rows, Color c, int scale = 2)
    {
        var bmp = new Bitmap(rows[0].Length * scale, rows.Length * scale);
        using var g = Graphics.FromImage(bmp);
        using var b = new SolidBrush(c);
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] == '#') g.FillRectangle(b, x * scale, y * scale, scale, scale);
        return bmp;
    }
}
