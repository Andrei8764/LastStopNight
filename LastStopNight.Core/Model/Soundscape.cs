
namespace LastStopNight.Core.Model;

/// <summary>
/// Ce se aude in fundal intr-un bloc: buclele locatiei (ploaie, vant, neon...), plus o drona de tensiune
/// in scenele intunecate sau rosii. Finalurile au in loc de ambianta o tema scurta, dupa tipul finalului.
/// </summary>
public static class Soundscape
{
    public const string MenuMusic = "music_menu";

    /// <summary>Volumul fiecarei bucle (unele sunt gandite ca fundal discret, altele ca prim-plan).</summary>
    private static readonly Dictionary<string, float> LoopVolume = new()
    {
        ["rain"] = 0.9f,
        ["wind"] = 0.6f,
        ["night"] = 0.8f,
        ["hum"] = 0.45f,
        ["room"] = 0.6f,
        ["clock"] = 0.5f,
        ["birds"] = 0.7f,
        ["tension"] = 0.8f,
    };

    /// <summary>Numele fisierului unei ambiante („rain” -&gt; „amb_rain”).</summary>
    public static string FileName(string sound) => "amb_" + sound.Trim().ToLowerInvariant();

    public static List<(string Name, float Volume)> Loops(StoryBlock block, LocationDefinition? location)
    {
        var result = new List<(string, float)>();
        if (block.IsEnding) return result;
        var sounds = location?.Sounds is { Count: > 0 } s ? s
            : (location?.Ambient ?? []).Where(a => a.Equals("rain", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var name in sounds) result.Add((FileName(name), LoopVolume.GetValueOrDefault(name.Trim().ToLowerInvariant(), 0.7f)));
        if (block.Mood is SceneMood.Dark or SceneMood.Red) result.Add((FileName("tension"), LoopVolume["tension"]));
        return result;
    }

    /// <summary>Tema care se aude cand ajungi la un final.</summary>
    public static string EndingSting(EndingType type) => type switch
    {
        EndingType.Good => "end_good",
        EndingType.Bad => "end_bad",
        EndingType.Secret => "end_secret",
        _ => "end_alt",
    };
}
