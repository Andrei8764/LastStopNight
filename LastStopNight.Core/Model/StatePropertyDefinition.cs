namespace LastStopNight.Core.Model;

public enum HudDisplay { Bar, Value, Flag, Hidden }

/// <summary>O proprietate numerica a starii (ex. player.life), cu limite si afisare in HUD.</summary>
public sealed class StatePropertyDefinition
{
    public string Key { get; set; } = "";
    public int Min { get; set; }
    public int Max { get; set; } = 100;
    public int Initial { get; set; }
    public string HudLabel { get; set; } = "";
    public HudDisplay Display { get; set; } = HudDisplay.Value;
    public string? Icon { get; set; }
    /// <summary>Culoare HTML pentru bara (ex. "#D8333F").</summary>
    public string? Color { get; set; }
    /// <summary>Format de afisare, unde {0} este valoarea (ex. "${0}", "{0}/3").</summary>
    public string? Format { get; set; }
    /// <summary>
    /// Multiplayer, drumuri separate: ce gaseste un drum (ex. indiciile) se aduna si pe celelalte drumuri.
    /// </summary>
    public bool Shared { get; set; }

    public int Clamp(int value) => Math.Clamp(value, Min, Math.Max(Min, Max));

    public string FormatValue(int value) =>
        string.IsNullOrWhiteSpace(Format) ? value.ToString() : string.Format(Format, value);

    public override string ToString() => Key;
}
