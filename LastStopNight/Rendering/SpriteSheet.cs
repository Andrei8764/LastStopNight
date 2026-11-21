using System.Drawing.Imaging;

namespace LastStopNight.Rendering;

/// <summary>Spritesheet orizontal cu cadre de marime fixa; pastreaza si varianta oglindita.</summary>
public sealed class SpriteSheet : IDisposable
{
    private readonly Bitmap _flipped;

    public SpriteSheet(Bitmap source, int frameWidth, int frameHeight)
    {
        Source = source;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameCount = Math.Max(1, source.Width / frameWidth);
        _flipped = (Bitmap)source.Clone();
        _flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
    }

    public Bitmap Source { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public int FrameCount { get; }

    /// <summary>Deseneaza un cadru la o pozitie fractionara (miscare fina la scalare).</summary>
    public void Draw(Graphics g, int frame, bool flipped, float x, float y, ImageAttributes? attrs = null)
    {
        frame = ((frame % FrameCount) + FrameCount) % FrameCount;
        var bmp = flipped ? _flipped : Source;
        int sx = (flipped ? FrameCount - 1 - frame : frame) * FrameWidth;
        var dest = new RectangleF(x, y, FrameWidth, FrameHeight);
        if (attrs is null) g.DrawImage(bmp, dest, new RectangleF(sx, 0, FrameWidth, FrameHeight), GraphicsUnit.Pixel);
        else
        {
            var pts = new[] { dest.Location, new PointF(dest.Right, dest.Top), new PointF(dest.Left, dest.Bottom) };
            g.DrawImage(bmp, pts, new RectangleF(sx, 0, FrameWidth, FrameHeight), GraphicsUnit.Pixel, attrs);
        }
    }

    public void Dispose() => _flipped.Dispose();
}
