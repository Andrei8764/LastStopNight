using LastStopNight.Core.Model;

namespace LastStopNight.Core.Engine;

/// <summary>O decizie a blocului curent, impreuna cu disponibilitatea ei in starea curenta.</summary>
/// <param name="Index">Pozitia in lista completa de decizii a blocului (folosita de <see cref="ChooseAction"/>).</param>
public sealed record DecisionOption(int Index, DecisionDefinition Decision, bool IsAvailable);

/// <summary>O modificare a unei proprietati, raportata dupa o decizie sau o examinare.</summary>
public readonly record struct PropertyChange(string Key, int OldValue, int NewValue)
{
    public int Delta => NewValue - OldValue;
}

public sealed class BlockChangedEventArgs(StoryBlock current, RedirectDefinition? redirect) : EventArgs
{
    public StoryBlock Current { get; } = current;
    /// <summary>Nenul daca intrarea in bloc a fost fortata de o redirectionare automata.</summary>
    public RedirectDefinition? Redirect { get; } = redirect;
}

public sealed class StateChangedEventArgs(IReadOnlyList<PropertyChange> changes) : EventArgs
{
    public IReadOnlyList<PropertyChange> Changes { get; } = changes;
}
