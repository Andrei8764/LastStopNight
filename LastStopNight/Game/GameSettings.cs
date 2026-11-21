namespace LastStopNight.Game;

public enum TextSpeed { Slow, Normal, Fast, Instant }

/// <summary>Setarile jucatorului, pastrate intre sesiuni.</summary>
public sealed class GameSettings
{
    public bool Fullscreen { get; set; }
    public TextSpeed TextSpeed { get; set; } = TextSpeed.Normal;
    /// <summary>Filtrul VHS; apare in Setari abia dupa ce l-ai gasit (codul Konami).</summary>
    public bool Crt { get; set; }
    public bool CrtUnlocked { get; set; }
    /// <summary>Volumele, de la 0 (oprit) la <see cref="MaxVolume"/>.</summary>
    public int SfxVolume { get; set; } = 8;
    public int AmbientVolume { get; set; } = 7;
    public int MusicVolume { get; set; } = 6;
    /// <summary>Tot sunetul oprit (tasta M).</summary>
    public bool Muted { get; set; }
    public string PlayerName { get; set; } = Environment.UserName.Length is > 0 and <= 16 ? Environment.UserName : "Jucător";

    public const int MaxVolume = 10;

    public float CharsPerSecond => TextSpeed switch
    {
        TextSpeed.Slow => 40,
        TextSpeed.Fast => 140,
        TextSpeed.Instant => 100000,
        _ => 75,
    };

    public static string Label(TextSpeed s) => s switch
    {
        TextSpeed.Slow => "Lentă",
        TextSpeed.Fast => "Rapidă",
        TextSpeed.Instant => "Instant",
        _ => "Normală",
    };

    public static GameSettings Load() => AppData.Load<GameSettings>("settings.json") ?? new GameSettings();
    public void Save() => AppData.Save("settings.json", this);
}
