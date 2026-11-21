using System.Drawing.Imaging;
using LastStopNight.Core.IO;

namespace LastStopNight.Rendering;

/// <summary>Cache de imagini decodate din pachetul povestii.</summary>
public sealed class ImageCache : IDisposable
{
    private readonly StoryPackage _package;
    private readonly Dictionary<string, Bitmap?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SpriteSheet> _sheets = new(StringComparer.OrdinalIgnoreCase);

    public ImageCache(StoryPackage package) { _package = package; }

    public Bitmap? Get(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (_cache.TryGetValue(path, out var bmp)) return bmp;
        var bytes = _package.GetFile(path);
        if (bytes is not null)
        {
            using var ms = new MemoryStream(bytes);
            using var tmp = new Bitmap(ms);
            // PArgb se deseneaza mai repede in GDI+
            bmp = new Bitmap(tmp.Width, tmp.Height, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.DrawImage(tmp, 0, 0, tmp.Width, tmp.Height);
        }
        _cache[path] = bmp;
        return bmp;
    }

    public SpriteSheet? GetSheet(string path, int frameWidth, int frameHeight)
    {
        if (_sheets.TryGetValue(path, out var s)) return s;
        var bmp = Get(path);
        if (bmp is null || frameWidth <= 0) return null;
        s = new SpriteSheet(bmp, frameWidth, frameHeight);
        _sheets[path] = s;
        return s;
    }

    public SpriteSheet? GetSheet(Core.Model.CharacterDefinition def) => GetSheet(def.Sprite, def.FrameWidth, def.FrameHeight);

    public void Dispose()
    {
        foreach (var s in _sheets.Values) s.Dispose();
        foreach (var b in _cache.Values) b?.Dispose();
        _cache.Clear();
        _sheets.Clear();
    }
}
