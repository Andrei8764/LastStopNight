namespace LastStopNight.Core.Model;

/// <summary>Atmosfera unei scene: filtrul de culoare si lumina pus peste ea.</summary>
public enum SceneMood { Normal, Dim, Dark, Red, Dawn, Sepia }

public enum EndingType { None, Good, Bad, Secret, Alternative }

/// <summary>Un nod din graful povestii: o scena cu text, prezentare 2D si decizii.</summary>
public sealed class StoryBlock
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string Time { get; set; } = "";
    /// <summary>Daca e setat, la intrarea in bloc se afiseaza un card de capitol.</summary>
    public string? Chapter { get; set; }

    // ------------------------------------------------------------------ scena 2D

    /// <summary>Locatia 2D (fundal + podea + ferestre). Poate lipsi la finaluri.</summary>
    public string? Location { get; set; }
    /// <summary>Fundal propriu (suprascrie imaginea locatiei).</summary>
    public string? Background { get; set; }
    /// <summary>Imagine de prim-plan afisata peste scena (registru, fotografie...).</summary>
    public string? Closeup { get; set; }
    /// <summary>Hartie scrisa aratata peste scena (reguli, bilet), cu textul ei lizibil.</summary>
    public PaperDefinition? Paper { get; set; }
    public SceneMood Mood { get; set; } = SceneMood.Normal;
    /// <summary>Efecte vizuale: "flicker", "shake", "ring", "rain".</summary>
    public List<string> Fx { get; set; } = new();
    /// <summary>Unde sta protagonistul la intrarea in bloc (null = ramane unde era).</summary>
    public int? PlayerX { get; set; }
    public List<CharacterPlacement> Characters { get; set; } = new();
    /// <summary>Obiecte de examinat specifice acestui bloc (pe langa cele ale locatiei).</summary>
    public List<ExamineDefinition> Examine { get; set; } = new();
    /// <summary>Obiecte care exista doar in acest bloc (ex. masina oprita la pompa), peste decorul locatiei.</summary>
    public List<DecorDefinition> Props { get; set; } = new();
    /// <summary>Lumini care exista doar in acest bloc (ex. farurile masinii).</summary>
    public List<LightDefinition> Lights { get; set; } = new();

    // ------------------------------------------------------------------ insotitori

    /// <summary>Paragraf adaugat la text cand insotitorul cu acest ID e in echipa.</summary>
    public Dictionary<string, string> CompanionText { get; set; } = new();
    /// <summary>Protagonistul e singur in scena (insotitorii nu se deseneaza), ex. in bucatarie pe intuneric.</summary>
    public bool Solo { get; set; }

    // ------------------------------------------------------------------ final si decizii

    public bool IsEnding { get; set; }
    public EndingType EndingType { get; set; } = EndingType.None;
    public string? EndingTitle { get; set; }
    public List<DecisionDefinition> Decisions { get; set; } = new();

    /// <summary>Textul blocului, cu paragrafele insotitorilor din echipa.</summary>
    public string TextFor(IEnumerable<string> party)
    {
        var extra = party.Where(CompanionText.ContainsKey).Select(id => CompanionText[id]).ToList();
        return extra.Count == 0 ? Text : Text + "\n\n" + string.Join("\n\n", extra);
    }

    public override string ToString() => $"{Id}: {Title}";
}
