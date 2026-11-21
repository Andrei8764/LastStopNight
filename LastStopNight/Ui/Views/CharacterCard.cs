using LastStopNight.Core.Model;
using LastStopNight.Rendering;
using static LastStopNight.Ui.MenuStyle;

namespace LastStopNight.Ui.Views;

/// <summary>Personajul de pe un cartonas (alegerea insotitorului, lobby): sprite-ul animat si textul de sub el.</summary>
public static class CharacterCard
{
    /// <summary>Respira cand sta; merge pe loc cand cartonasul e selectat.</summary>
    public static void DrawPerson(Graphics g, float s, ImageCache? images, CharacterDefinition? c, PointF at, bool active, float time)
    {
        if (c is null || images?.Get(c.Sprite) is not { } sheet) return;
        const float walkFps = 8, idleFps = 1.4f;
        var frames = active && c.WalkFrames.Length > 0 ? c.WalkFrames : c.IdleFrames;
        int frame = frames.Length == 0 ? 0 : frames[(int)(time * (active ? walkFps : idleFps)) % frames.Length];
        Sprite(g, sheet, frame, c.FrameWidth, c.FrameHeight, new RectangleF(at.X, at.Y, c.FrameWidth * s, c.FrameHeight * s));
    }

    /// <summary>Numele, rolul si descrierea, in partea de jos a cartonasului.</summary>
    public static void DrawText(Graphics g, float s, RectangleF card, string name, string role, string blurb, bool selected)
    {
        float y = card.Y + 60 * s;
        g.DrawString(name, Mono(s, 6.6f), Gfx.Brush(selected ? Ink : Muted), card.X + 5 * s, y);
        g.DrawString(role, FontCache.Px("Georgia", 4.8f, s, FontStyle.Italic), Gfx.Brush(selected ? Warm : Dim), card.X + 5 * s, y + 9 * s);
        Wrap(g, blurb, Body(s, 4.7f), selected ? Ink : Muted,
            new RectangleF(card.X + 5 * s, y + 17 * s, card.Width - 10 * s, card.Bottom - y - 19 * s));
    }
}
