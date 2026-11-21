using System.Runtime.CompilerServices;
using LastStopNight.Core.Model;

namespace LastStopNight.Rendering;

/// <summary>Portrete (capul personajului) decupate din primul cadru al spritesheet-ului, pentru HUD si butoane.</summary>
public static class Portraits
{
    private static readonly ConditionalWeakTable<Bitmap, Bitmap> Heads = new();
    private static readonly ConditionalWeakTable<Bitmap, Bitmap> Badges = new();

    /// <summary>Capul (16x16 pixeli din sprite), sau null daca personajul n-are imagine.</summary>
    public static Bitmap? Head(ImageCache images, CharacterDefinition c)
    {
        if (images.Get(c.Sprite) is not { } sheet) return null;
        return Heads.GetValue(sheet, s =>
        {
            int fw = Math.Min(c.FrameWidth, s.Width);
            var crop = new Rectangle(Math.Max(0, fw / 2 - 8), 0, Math.Min(16, fw), Math.Min(16, s.Height));
            var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.DrawImage(s, new Rectangle(0, 0, crop.Width, crop.Height), crop, GraphicsUnit.Pixel);
            return bmp;
        });
    }

    /// <summary>Portret mic (24x24) cu rama, pentru butoanele de decizie legate de un personaj.</summary>
    public static Bitmap? Badge(ImageCache images, CharacterDefinition c)
    {
        if (Head(images, c) is not { } head) return null;
        return Badges.GetValue(head, h =>
        {
            var bmp = new Bitmap(24, 24, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.FillRectangle(Gfx.Brush(255, 44, 30, 34), 0, 0, 24, 24);
            g.DrawImage(h, 0, 0, 24, 24);
            g.DrawRectangle(Gfx.Pen(Color.FromArgb(170, 70, 76), 1), 0, 0, 23, 23);
            return bmp;
        });
    }
}
