namespace LastStopNight.Core.Model;

/// <summary>Redirectionare automata: dupa aplicarea efectelor, daca conditia e adevarata, se sare la Target.</summary>
public sealed class RedirectDefinition
{
    public ConditionDefinition? Condition { get; set; }
    public string Target { get; set; } = "";
    public string Description { get; set; } = "";
}
