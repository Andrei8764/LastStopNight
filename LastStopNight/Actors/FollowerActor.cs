using LastStopNight.Core.Model;
using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Actors;

/// <summary>
/// Insotitorul ales din meniu: merge dupa jucator, la cativa pasi in spatele lui,
/// si se intoarce in aceeasi directie cand jucatorul se opreste.
/// </summary>
public sealed class FollowerActor : Actor
{
    private const float Speed = 70f, Behind = 17f;

    public FollowerActor(CharacterDefinition def, SpriteSheet sheet, float x) : base(def, sheet, x)
    {
        AnimTime = 0.7f; // nu respira in acelasi ritm cu jucatorul
    }

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (scene.Player is not { } p) { IsWalking = false; return; }
        // locul „din spatele” jucatorului; langa margine trece de partea cealalta
        float side = p.FacingLeft ? 1 : -1;
        float target = p.X + side * Behind;
        if (target < scene.MinX || target > scene.MaxX) target = p.X - side * Behind;
        target = Math.Clamp(target, scene.MinX, scene.MaxX);
        float d = target - X;
        IsWalking = MathF.Abs(d) > (IsWalking ? 1.5f : 7f);
        if (IsWalking)
        {
            FacingLeft = d < 0;
            float speed = Math.Min(Speed + MathF.Abs(d) * 1.5f, 120f);
            X += MathF.Sign(d) * Math.Min(MathF.Abs(d), speed * dt);
        }
        else if (!p.IsWalking) FacingLeft = p.FacingLeft;
    }
}
