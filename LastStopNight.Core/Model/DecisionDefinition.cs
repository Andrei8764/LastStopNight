namespace LastStopNight.Core.Model;

/// <summary>O muchie din graf: textul alegerii, blocul destinatie, conditia si efectele.</summary>
public sealed class DecisionDefinition
{
    public string Text { get; set; } = "";
    public string Target { get; set; } = "";
    public ConditionDefinition? Condition { get; set; }
    public List<EffectDefinition> Effects { get; set; } = new();

    /// <summary>Pozitia X din scena unde Alex trebuie sa ajunga pentru a declansa decizia (optional).</summary>
    public int? HotspotX { get; set; }
    /// <summary>Inaltimea obiectului de care tine decizia (pentru marcaj). Null = la nivelul ochilor.</summary>
    public int? HotspotY { get; set; }
    /// <summary>Daca decizia e indisponibila, este afisata (dezactivata) sau ascunsa.</summary>
    public bool ShowWhenLocked { get; set; } = true;
    /// <summary>Explicatie pentru jucator cand decizia e blocata.</summary>
    public string? LockedHint { get; set; }
    /// <summary>Decizia exista doar daca acest personaj e in echipa (ex. „Vlad se uita la motor”).</summary>
    public string? RequiresCharacter { get; set; }

    public bool IsAvailable(IStateReader state) =>
        (RequiresCharacter is null || state.HasCharacter(RequiresCharacter)) && (Condition?.Evaluate(state) ?? true);

    /// <summary>Deciziile legate de un personaj care lipseste nu apar deloc (nici blocate).</summary>
    public bool IsVisible(IStateReader state) =>
        RequiresCharacter is null || state.HasCharacter(RequiresCharacter);

    public override string ToString() => $"{Text} → {Target}";
}
