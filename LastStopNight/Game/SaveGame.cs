using LastStopNight.Core.Engine;

namespace LastStopNight.Game;

/// <summary>
/// Salvarea unei ture: povestea, echipa si jurnalul de actiuni. „Continua” reaplica actiunile pe
/// un motor nou; motorul e determinist, deci ajunge exact in acelasi loc.
/// </summary>
public sealed class SaveGame
{
    public string StoryTitle { get; set; } = "";
    public List<string> Companions { get; set; } = new();
    public List<GameAction> Actions { get; set; } = new();
    public string BlockTitle { get; set; } = "";
    public string Time { get; set; } = "";
    public DateTime SavedAt { get; set; }

    public static SaveGame? Load() => AppData.Load<SaveGame>("save.json");
    public void Save() => AppData.Save("save.json", this);
    public static void Clear() => AppData.Delete("save.json");
}
