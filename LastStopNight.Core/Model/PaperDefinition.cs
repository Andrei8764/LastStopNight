namespace LastStopNight.Core.Model;

/// <summary>
/// O hartie scrisa, aratata mare peste scena (regulile din registru, un bilet), in loc sa fie doar in textul de jos.
/// Naratorul o citeste dupa textul blocului.
/// </summary>
public sealed class PaperDefinition
{
    /// <summary>Titlul scris apasat sus (ex. „REGULI”); poate lipsi.</summary>
    public string? Title { get; set; }
    public string Text { get; set; } = "";
    /// <summary>Hartie mica (bilet), nu pagina de registru.</summary>
    public bool Note { get; set; }
}

/// <summary>Unde stau in arhiva vocile naratorului: cate un WAV pe bloc si cate unul pe paragraful fiecarui insotitor.</summary>
public static class StoryVoice
{
    public const string Folder = "voice/";

    public static string PathFor(string blockId, string? companion = null) =>
        companion is null ? $"{Folder}{blockId}.wav" : $"{Folder}{blockId}@{companion}.wav";

    /// <summary>Ce citeste naratorul pentru un bloc: textul, apoi hartia (daca are).</summary>
    public static string SpokenText(StoryBlock block) =>
        block.Paper is { } p ? $"{block.Text}\n\n{p.Title}\n\n{p.Text}".Trim() : block.Text;
}
