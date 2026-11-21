using LastStopNight.Core.Model;
using LastStopNight.Game;
using LastStopNight.Rendering;

namespace LastStopNight.Actors;

/// <summary>Personaj din poveste: sta pe loc si se uita dupa jucator.</summary>
public sealed class NpcActor : Actor
{
    public NpcActor(CharacterDefinition def, SpriteSheet sheet, CharacterPlacement placement)
        : base(def, sheet, placement.X)
    {
        FacingLeft = placement.FacingLeft;
        AnimTime = placement.X * 0.37f; // desincronizeaza respiratia
    }

    public override void Update(float dt, Scene scene)
    {
        base.Update(dt, scene);
        if (scene.Player is { } p && MathF.Abs(p.X - X) > 6)
            FacingLeft = p.X < X;
    }
}
