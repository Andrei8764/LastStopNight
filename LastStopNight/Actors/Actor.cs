using System.Drawing.Imaging;
using LastStopNight.Core.Model;
using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Actors;

/// <summary>Baza pentru orice personaj din scena: pozitie, orientare si animatie din spritesheet.</summary>
public abstract class Actor
{
    protected Actor(CharacterDefinition definition, SpriteSheet sheet, float x)
    {
        Definition = definition;
        Sheet = sheet;
        X = x;
    }

    public CharacterDefinition Definition { get; }
    public SpriteSheet Sheet { get; }
    public float X { get; set; }
    public bool FacingLeft { get; set; }
    public bool IsWalking { get; protected set; }
    protected float AnimTime { get; set; }

    /// <summary>Lungimea unui pas (pixeli logici): la viteza maxima, cam 4-5 pasi pe secunda.</summary>
    public const float StrideLength = 17;
    /// <summary>Peste atat (intr-un cadru) e o teleportare, nu mers.</summary>
    private const float MaxStepPerFrame = 8;
    private float _lastX = float.NaN, _stride;

    /// <summary>
    /// Adevarat cand personajul tocmai a mai facut un pas (pentru sunetul pasilor). Primul pas se aude
    /// repede dupa pornire, nu abia dupa un pas intreg.
    /// </summary>
    public bool TakeStep()
    {
        float dx = float.IsNaN(_lastX) ? 0 : Math.Min(MaxStepPerFrame, MathF.Abs(X - _lastX));
        _lastX = X;
        if (!IsWalking) { _stride = StrideLength * 0.6f; return false; }
        _stride += dx;
        if (_stride < StrideLength) return false;
        _stride -= StrideLength;
        return true;
    }

    public virtual void Update(float dt, Scene scene) => AnimTime += dt;

    protected int CurrentFrame()
    {
        var frames = IsWalking && Definition.WalkFrames.Length > 0 ? Definition.WalkFrames : Definition.IdleFrames;
        if (frames.Length == 0) return 0;
        float fps = IsWalking ? 9f : 1.4f;
        return frames[(int)(AnimTime * fps) % frames.Length];
    }

    /// <summary>Deseneaza personajul cu picioarele pe linia floorY, la pozitie fractionara (miscare fina).</summary>
    public void Draw(Graphics g, int floorY, ImageAttributes? attrs = null)
    {
        float x = X - Sheet.FrameWidth / 2f;
        float y = floorY - Sheet.FrameHeight + 2;
        g.FillEllipse(Gfx.Brush(80, 0, 0, 0), x + 4, floorY - 1, Sheet.FrameWidth - 8, 4);
        Sheet.Draw(g, CurrentFrame(), FacingLeft, x, y, attrs);
    }

    /// <summary>Reflexia in geam: semi-transparenta, rece la culoare, decupata la fereastra.</summary>
    public void DrawReflection(Graphics g, Rectangle window, ImageAttributes attrs)
    {
        if (!Definition.HasReflection) return;
        float x = X - Sheet.FrameWidth / 2f;
        if (x + Sheet.FrameWidth < window.Left || x > window.Right) return;
        float y = window.Bottom - Sheet.FrameHeight * 0.8f;
        var clip = g.Clip;
        g.SetClip(window, System.Drawing.Drawing2D.CombineMode.Intersect);
        Sheet.Draw(g, CurrentFrame(), FacingLeft, x, y, attrs);
        g.Clip = clip;
    }
}
