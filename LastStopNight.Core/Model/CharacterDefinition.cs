namespace LastStopNight.Core.Model;

/// <summary>Un personaj care poate fi ales din meniu, cu o scurta prezentare.</summary>
public sealed class PlayableDefinition
{
    public string CharacterId { get; set; } = "";
    /// <summary>Rolul, afisat pe cartonas (ex. „Ospatarita, prima tura”).</summary>
    public string Role { get; set; } = "";
    /// <summary>O fraza despre personaj.</summary>
    public string Blurb { get; set; } = "";

    public override string ToString() => $"{CharacterId}: {Role}";
}

/// <summary>Un personaj cu spritesheet: cadre de dimensiune fixa, asezate pe orizontala.</summary>
public sealed class CharacterDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Sprite { get; set; } = "";
    public int FrameWidth { get; set; } = 24;
    public int FrameHeight { get; set; } = 48;
    public int[] IdleFrames { get; set; } = [0];
    public int[] WalkFrames { get; set; } = [0];
    /// <summary>Clientul fara reflexie are false: nu apare in geamuri.</summary>
    public bool HasReflection { get; set; } = true;
    public string Description { get; set; } = "";

    public override string ToString() => $"{Id}: {Name}";
}
