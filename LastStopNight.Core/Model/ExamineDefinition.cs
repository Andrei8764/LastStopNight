namespace LastStopNight.Core.Model;

/// <summary>Un obiect din scena la care Alex se poate uita. Poate modifica starea (o singura data).</summary>
public sealed class ExamineDefinition
{
    public string Id { get; set; } = "";
    public int X { get; set; }
    /// <summary>Inaltimea obiectului in scena (pentru marcaj). Null = la nivelul ochilor personajului.</summary>
    public int? Y { get; set; }
    public string Label { get; set; } = "";
    public string Text { get; set; } = "";
    public List<EffectDefinition> Effects { get; set; } = new();
    public ConditionDefinition? Condition { get; set; }
    /// <summary>Efectele se aplica doar prima data.</summary>
    public bool Once { get; set; } = true;
    /// <summary>Fara marcaj in scena: il gasesti doar daca treci pe langa (easter egg).</summary>
    public bool Hidden { get; set; }
    /// <summary>Ce observa alt personaj decat protagonistul la acelasi obiect (cheie = ID personaj).</summary>
    public Dictionary<string, string> TextBy { get; set; } = new();

    public string TextFor(string? characterId) =>
        characterId is not null && TextBy.TryGetValue(characterId, out var t) && !string.IsNullOrWhiteSpace(t) ? t : Text;

    public override string ToString() => $"{Id} @{X}: {Label}";
}
