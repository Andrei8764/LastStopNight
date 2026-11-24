namespace LastStopNight.Rendering;

/// <summary>
/// Fonturile se creeaza o singura data (crearea unui Font la fiecare cadru era una din cauzele sacadarii).
/// </summary>
public static class FontCache
{
    private static readonly Dictionary<(string, int, FontStyle), Font> Fonts = new();

    /// <summary>
    /// Font de <paramref name="sizePt"/> puncte, socotite la 96 DPI (1 pt = 4/3 pixeli), dar creat in pixeli:
    /// marimea nu depinde de scalarea din Windows (125%, 150%...). In puncte, pe un laptop cu scalare 150%
    /// textul iesea de 1,5 ori mai mare, iar scena (desenata in pixeli) nu, deci titlurile ieseau din cartonase.
    /// </summary>
    public static Font Get(string family, float sizePt, FontStyle style = FontStyle.Regular)
    {
        int key = (int)MathF.Round(Math.Max(5, sizePt) * 2);
        if (!Fonts.TryGetValue((family, key, style), out var f))
            Fonts[(family, key, style)] = f = new Font(family, key / 2f * 96f / 72f, style, GraphicsUnit.Pixel);
        return f;
    }

    private static readonly Dictionary<string, bool> Installed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Fontul e instalat in Windows (ex. „Segoe Print” pentru scrisul de mana de pe hartii).</summary>
    public static bool Exists(string family)
    {
        if (!Installed.TryGetValue(family, out bool ok))
        {
            using var f = new Font(family, 10);
            Installed[family] = ok = string.Equals(f.Name, family, StringComparison.OrdinalIgnoreCase);
        }
        return ok;
    }

    /// <summary>Font pentru o inaltime in pixeli logici, la scala curenta.</summary>
    public static Font Px(string family, float logicalPx, float scale, FontStyle style = FontStyle.Regular) =>
        Get(family, logicalPx * scale * 0.75f, style);
}
