namespace LastStopNight.Rendering;

/// <summary>
/// Fonturile se creeaza o singura data (crearea unui Font la fiecare cadru era una din cauzele sacadarii).
/// </summary>
public static class FontCache
{
    private static readonly Dictionary<(string, int, FontStyle), Font> Fonts = new();

    public static Font Get(string family, float sizePt, FontStyle style = FontStyle.Regular)
    {
        int key = (int)MathF.Round(Math.Max(5, sizePt) * 2);
        if (!Fonts.TryGetValue((family, key, style), out var f))
            Fonts[(family, key, style)] = f = new Font(family, key / 2f, style);
        return f;
    }

    /// <summary>Font pentru o inaltime in pixeli logici, la scala curenta.</summary>
    public static Font Px(string family, float logicalPx, float scale, FontStyle style = FontStyle.Regular) =>
        Get(family, logicalPx * scale * 0.75f, style);
}
