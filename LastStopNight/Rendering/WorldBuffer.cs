using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LastStopNight.Rendering;

/// <summary>
/// Imaginea interna a lumii: o sectiune DIB din Windows, pe care GDI+ deseneaza ca pe un Bitmap obisnuit
/// (aceeasi memorie), iar marirea pe ecran o face StretchBlt din GDI (mult mai rapid decat DrawImage din GDI+
/// la rezolutii mari: ~1 ms in loc de ~6 ms pentru 2560x1440).
/// </summary>
internal sealed class WorldBuffer : IDisposable
{
    private const int SrcCopy = 0x00CC0020, ColorOnColor = 3, DibRgbColors = 0;

    private readonly IntPtr _dc, _dib, _old;

    public WorldBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        var info = new BitmapInfoHeader
        {
            Size = Marshal.SizeOf<BitmapInfoHeader>(),
            Width = width,
            Height = -height, // negativ = de sus in jos, ca Bitmap
            Planes = 1,
            BitCount = 32,
        };
        _dc = CreateCompatibleDC(IntPtr.Zero);
        _dib = CreateDIBSection(_dc, ref info, DibRgbColors, out var bits, IntPtr.Zero, 0);
        if (_dib == IntPtr.Zero) throw new InvalidOperationException("Nu pot crea imaginea interna a scenei.");
        _old = SelectObject(_dc, _dib);
        Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, bits);
    }

    public int Width { get; }
    public int Height { get; }
    /// <summary>Pe aici deseneaza GDI+ (memoria e a sectiunii DIB).</summary>
    public Bitmap Bitmap { get; }

    /// <summary>Mareste imaginea in <paramref name="dest"/>, doar in <paramref name="clip"/> (pixeli „nearest neighbor”).</summary>
    public void StretchTo(Graphics g, Rectangle dest, Rectangle clip)
    {
        GdiFlush(); // tot ce a desenat GDI+ e in memorie inainte de copiere
        var hdc = g.GetHdc();
        try
        {
            int saved = SaveDC(hdc);
            IntersectClipRect(hdc, clip.Left, clip.Top, clip.Right, clip.Bottom);
            SetStretchBltMode(hdc, ColorOnColor);
            StretchBlt(hdc, dest.X, dest.Y, dest.Width, dest.Height, _dc, 0, 0, Width, Height, SrcCopy);
            RestoreDC(hdc, saved);
        }
        finally { g.ReleaseHdc(hdc); }
    }

    public void Dispose()
    {
        Bitmap.Dispose();
        SelectObject(_dc, _old);
        DeleteObject(_dib);
        DeleteDC(_dc);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public int Size, Width, Height;
        public short Planes, BitCount;
        public int Compression, SizeImage, XPelsPerMeter, YPelsPerMeter, ClrUsed, ClrImportant;
    }

    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BitmapInfoHeader info, int usage, out IntPtr bits, IntPtr section, int offset);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
    [DllImport("gdi32.dll")] private static extern int SaveDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern bool RestoreDC(IntPtr hdc, int saved);
    [DllImport("gdi32.dll")] private static extern int IntersectClipRect(IntPtr hdc, int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] private static extern int SetStretchBltMode(IntPtr hdc, int mode);
    [DllImport("gdi32.dll")]
    private static extern bool StretchBlt(IntPtr dest, int x, int y, int w, int h, IntPtr src, int sx, int sy, int sw, int sh, int rop);
}
