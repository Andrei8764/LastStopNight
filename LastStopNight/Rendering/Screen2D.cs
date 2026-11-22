using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LastStopNight.Rendering;

/// <summary>Rezolutia logica a jocului; lumea se deseneaza in coordonate 320x180, scalate pe ecran.</summary>
public static class Screen2D
{
    public const int Width = 320;
    public const int Height = 180;

    /// <summary>
    /// O intunecare (sau un fundal opac) peste toata zona meniului: <paramref name="Width01"/> &lt; 1 = gradient de la
    /// stanga spre dreapta, care se stinge la acea fractiune din latime.
    /// </summary>
    public readonly record struct Tint(float Width01, int Alpha, Color Color);

    private static List<Tint> _requested = new(), _pending = new();

    /// <summary>
    /// Cere intunecarea scenei din spatele unui meniu / al hartii / al unei hartii scrise. Nu deseneaza nimic acum:
    /// suprafata de joc o aplica la urmatorul cadru, in imaginea interna a lumii (de ~7 ori mai putini pixeli decat
    /// ecranul complet). Un strat semitransparent pe tot ecranul de 2560x1440 costa ~6 ms in GDI+, un gradient ~14 ms.
    /// </summary>
    public static void RequestTint(float width01, int alpha, Color color)
    {
        if (alpha > 0) _requested.Add(new Tint(width01, Math.Clamp(alpha, 0, 255), color));
    }

    /// <summary>Intunecarile cerute in cadrul trecut (pentru cadrul de acum); cererile noi incep de la zero.</summary>
    public static IReadOnlyList<Tint> TakeTints()
    {
        (_pending, _requested) = (_requested, _pending);
        _requested.Clear();
        return _pending;
    }

    public static void SetPixelArt(Graphics g)
    {
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.SmoothingMode = SmoothingMode.None;
        g.CompositingQuality = CompositingQuality.HighSpeed;
    }

    public static void SetSmooth(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.PixelOffsetMode = PixelOffsetMode.Default;
        g.InterpolationMode = InterpolationMode.Bilinear;
    }

    public static ImageAttributes AlphaTint(float alpha, float r = 1, float g = 1, float b = 1)
    {
        var m = new ColorMatrix(
        [
            [r, 0, 0, 0, 0],
            [0, g, 0, 0, 0],
            [0, 0, b, 0, 0],
            [0, 0, 0, alpha, 0],
            [0, 0, 0, 0, 1],
        ]);
        var a = new ImageAttributes();
        a.SetColorMatrix(m);
        return a;
    }

    /// <summary>Matrice sepia (pentru amintirile din 1987).</summary>
    public static ImageAttributes Sepia(float amount)
    {
        float k = Math.Clamp(amount, 0, 1);
        float[][] sepia =
        [
            [0.393f, 0.349f, 0.272f, 0, 0],
            [0.769f, 0.686f, 0.534f, 0, 0],
            [0.189f, 0.168f, 0.131f, 0, 0],
            [0, 0, 0, 1, 0],
            [0, 0, 0, 0, 1],
        ];
        var m = new float[5][];
        for (int i = 0; i < 5; i++)
        {
            m[i] = new float[5];
            for (int j = 0; j < 5; j++) m[i][j] = (i == j ? 1 - k : 0) + sepia[i][j] * k;
        }
        m[3][3] = 1;
        m[4][4] = 1;
        var a = new ImageAttributes();
        a.SetColorMatrix(new ColorMatrix(m));
        return a;
    }

    public static Color ParseColor(string? html, Color fallback)
    {
        try { return string.IsNullOrWhiteSpace(html) ? fallback : ColorTranslator.FromHtml(html); }
        catch { return fallback; }
    }

    /// <summary>Creeaza un bitmap pixel cu pixel (LockBits) dintr-o functie care intoarce ARGB.</summary>
    public static Bitmap Generate(int w, int h, Func<int, int, int> argb)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        var buf = new int[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                buf[y * w + x] = argb(x, y);
        Marshal.Copy(buf, 0, data.Scan0, buf.Length);
        bmp.UnlockBits(data);
        return bmp;
    }
}
