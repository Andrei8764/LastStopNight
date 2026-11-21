namespace LastStopNight.Core.Model;

/// <summary>O locatie 2D: imaginea de fundal si geometria folosita de player (podea, limite, ferestre).</summary>
public sealed class LocationDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    /// <summary>Linia (Y, in pixeli logici 320x180) pe care calca personajele.</summary>
    public int FloorY { get; set; } = 170;
    public int MinX { get; set; } = 10;
    public int MaxX { get; set; } = 310;
    /// <summary>Zone in care se deseneaza reflexiile personajelor.</summary>
    public List<RectDefinition> Windows { get; set; } = new();
    /// <summary>Efecte permanente ale locatiei (ex. "rain").</summary>
    public List<string> Ambient { get; set; } = new();
    /// <summary>
    /// Sunetele de fundal ale locatiei, in bucla (ex. "rain", "wind"); vezi <see cref="KnownSounds"/>.
    /// Gol = se deduc din <see cref="Ambient"/> (ploaia se si aude).
    /// </summary>
    public List<string> Sounds { get; set; } = new();
    /// <summary>Puncte numite (ex. „phone”) folosite de efecte.</summary>
    public Dictionary<string, PointDefinition> Points { get; set; } = new();
    /// <summary>Obiecte animate puse peste fundal (ventilator, pisica, firma care palpaie...).</summary>
    public List<DecorDefinition> Decor { get; set; } = new();
    /// <summary>Surse de lumina care palpaie sau pulseaza.</summary>
    public List<LightDefinition> Lights { get; set; } = new();
    /// <summary>Obiecte la care te poti uita (E), prezente in toate blocurile din locatie.</summary>
    public List<ExamineDefinition> Examine { get; set; } = new();
    /// <summary>Culoarea luminii care cade pe personaje (ex. #A0A8D0 noaptea afara). Gol = fara nuantare.</summary>
    public string? ActorTint { get; set; }

    /// <summary>Ambiantele pe care le are jocul (fisierele amb_*.wav).</summary>
    public static readonly IReadOnlyList<string> KnownSounds = ["rain", "wind", "night", "hum", "room", "clock", "birds", "tension"];

    public override string ToString() => $"{Id}: {Name}";
}

public sealed class RectDefinition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}

public sealed class PointDefinition
{
    public int X { get; set; }
    public int Y { get; set; }
}
