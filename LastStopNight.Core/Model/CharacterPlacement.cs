namespace LastStopNight.Core.Model;

/// <summary>Un personaj (non-jucator) plasat in scena unui bloc.</summary>
public sealed class CharacterPlacement
{
    public string Id { get; set; } = "";
    public int X { get; set; }
    public bool FacingLeft { get; set; }

    public override string ToString() => $"{Id}@{X}{(FacingLeft ? "<" : "")}";
}
