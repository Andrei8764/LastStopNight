using LastStopNight.Core.Engine;
using LastStopNight.Core.Model;

namespace LastStopNight.Game;

/// <summary>Starea tastelor de miscare, citita la fiecare cadru.</summary>
public sealed class InputState
{
    public bool Left { get; set; }
    public bool Right { get; set; }
}

/// <summary>Un punct din scena unde jucatorul poate apasa E: o decizie sau un obiect de examinat.</summary>
public sealed record Interactable(int X, int? Y, string Label, DecisionOption? Decision, ExamineDefinition? Examine)
{
    public bool IsAvailable => Decision?.IsAvailable ?? true;
    public bool IsHidden => Examine?.Hidden == true;
}
