namespace LastStopNight.Core.Model;

/// <summary>Sprite animat pus peste fundalul unei locatii.</summary>
public sealed class DecorDefinition
{
    public string Image { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public float Fps { get; set; } = 4;
    /// <summary>Cadrele se aleg aleator (palpaire), nu in ordine.</summary>
    public bool Random { get; set; }
    /// <summary>Se deseneaza peste personaje (prim-plan).</summary>
    public bool Front { get; set; }
}

/// <summary>Halou de lumina: cerc colorat, semi-transparent, care poate palpai.</summary>
public sealed class LightDefinition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Radius { get; set; } = 20;
    public string Color { get; set; } = "#FFD080";
    /// <summary>Intensitatea (0-255).</summary>
    public int Alpha { get; set; } = 60;
    /// <summary>0 = constant, 1 = palpaie puternic.</summary>
    public float Flicker { get; set; }
}
