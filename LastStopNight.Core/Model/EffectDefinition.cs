namespace LastStopNight.Core.Model;

public enum EffectOperation { Add, Set }

/// <summary>Modificarea unei proprietati de stare. Rezultatul este limitat la [min, max] de <see cref="Engine.GameState"/>.</summary>
public sealed class EffectDefinition
{
    public string Key { get; set; } = "";
    public EffectOperation Op { get; set; } = EffectOperation.Add;
    public int Value { get; set; }

    public int Compute(int current) => Op switch
    {
        EffectOperation.Add => current + Value,
        EffectOperation.Set => Value,
        _ => current,
    };

    public string ToExpression() => Op == EffectOperation.Set
        ? $"{Key} = {Value}"
        : $"{Key} {(Value >= 0 ? "+" : "-")}{Math.Abs(Value)}";

    public override string ToString() => ToExpression();
}
