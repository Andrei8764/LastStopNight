using LastStopNight.Core.Model;
using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Actors;

/// <summary>Personajul jucatorului: controlat cu A/D sau sageti, ori mers automat catre un punct.</summary>
public sealed class PlayerActor : Actor
{
    public const float MaxSpeed = 76f;
    private float? _targetX;
    private Action? _onArrive;
    private float _speed;

    public PlayerActor(CharacterDefinition def, SpriteSheet sheet, float x) : base(def, sheet, x) { }

    public bool IsAutoWalking => _targetX is not null;

    public void WalkTo(float x, Action? onArrive)
    {
        _targetX = x;
        _onArrive = onArrive;
    }

    public void CancelWalk()
    {
        _targetX = null;
        _onArrive = null;
    }

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        float dir = 0;
        if (scene.Input.Left) dir -= 1;
        if (scene.Input.Right) dir += 1;
        if (dir != 0) CancelWalk();
        else if (_targetX is float t)
        {
            float d = t - X;
            if (MathF.Abs(d) <= 1.2f)
            {
                X = t;
                var arrive = _onArrive;
                CancelWalk();
                if (arrive is not null) scene.Post(arrive);
            }
            else dir = MathF.Sign(d);
        }

        // accelerare scurta: pornirea si oprirea nu mai sunt bruste
        float target = dir == 0 ? 0 : MaxSpeed;
        _speed += (target - _speed) * Math.Min(1, dt * 14);
        IsWalking = dir != 0;
        if (IsWalking)
        {
            FacingLeft = dir < 0;
            float step = Math.Max(_speed, 25) * dt;
            if (_targetX is float tt) step = Math.Min(step, MathF.Abs(tt - X));
            X = Math.Clamp(X + dir * step, scene.MinX, scene.MaxX);
        }
    }
}
