using LastStopNight.Core.Model;
using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Actors;

/// <summary>
/// Personajul altui jucator (multiplayer): pozitia vine prin retea de ~15 ori pe secunda,
/// iar aici e netezita ca mersul sa para continuu.
/// </summary>
public sealed class RemoteActor : Actor
{
    private float _targetX;
    private bool _walking;

    public RemoteActor(CharacterDefinition def, SpriteSheet sheet, float x) : base(def, sheet, x) => _targetX = x;

    public void SetTarget(float x, bool facingLeft, bool walking, bool snap = false)
    {
        _targetX = x;
        _walking = walking;
        if (snap || MathF.Abs(x - X) > 80) X = x; // salt mare (alta scena): fara „alunecare” prin camera
        if (!walking) FacingLeft = facingLeft;
        else if (MathF.Abs(x - X) > 0.5f) FacingLeft = x < X;
    }

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        float d = _targetX - X;
        X += MathF.Sign(d) * Math.Min(MathF.Abs(d), Math.Max(PlayerActor.MaxSpeed * 1.2f, MathF.Abs(d) * 8) * dt);
        IsWalking = _walking || MathF.Abs(d) > 1f;
    }
}
