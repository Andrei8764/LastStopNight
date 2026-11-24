namespace LastStopNight.Rendering;

/// <summary>
/// Scalarea din Windows (100% = 1, 150% = 1.5, 200% = 2). O folosesc doar panourile fixe ale ferestrei: banda HUD,
/// panoul cu textul si butoanele de decizie, ca pe un laptop cu ecran dens sa nu iasa minuscule.
/// Scena si ce se deseneaza peste ea se scaleaza deja cu marimea ferestrei, deci nu trec pe aici.
/// </summary>
public static class UiScale
{
    public static float Value { get; private set; } = 1;

    /// <summary>Scalarea pentru DPI-ul dat; LASTSTOP_UISCALE=1.5 o simuleaza (teste pe un monitor fara scalare).</summary>
    public static void SetDpi(int dpi)
    {
        float forced = float.TryParse(Environment.GetEnvironmentVariable("LASTSTOP_UISCALE"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0;
        Value = Math.Clamp(forced > 0 ? forced : dpi / 96f, 1f, 4f);
    }

    public static int Px(int pixelsAt100) => (int)MathF.Round(pixelsAt100 * Value);

    public static float Px(float pixelsAt100) => pixelsAt100 * Value;

    /// <summary>Font de interfata: <paramref name="sizePt"/> puncte la 100%, marit cu scalarea din Windows.</summary>
    public static Font Font(string family, float sizePt, FontStyle style = FontStyle.Regular) =>
        FontCache.Get(family, sizePt * Value, style);
}
