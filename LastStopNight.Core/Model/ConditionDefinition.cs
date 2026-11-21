using System.Text.Json.Serialization;

namespace LastStopNight.Core.Model;

/// <summary>Acces doar-citire la valorile starii — suficient pentru evaluarea conditiilor.</summary>
public interface IStateReader
{
    int Get(string key);
    bool Has(string key);
    /// <summary>Personajul face parte din echipa care joaca (pentru deciziile legate de un personaj).</summary>
    bool HasCharacter(string characterId) => true;
}

/// <summary>
/// Nod abstract din arborele de conditii. Tipurile concrete (COMPARISON, AND, OR) sunt
/// serializate polimorfic prin campul „type”.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ComparisonCondition), "COMPARISON")]
[JsonDerivedType(typeof(AndCondition), "AND")]
[JsonDerivedType(typeof(OrCondition), "OR")]
public abstract class ConditionDefinition
{
    public abstract bool Evaluate(IStateReader state);
    public abstract IEnumerable<string> ReferencedKeys();
    public abstract string ToExpression();
    public override string ToString() => ToExpression();
}

/// <summary>Frunza: compara o proprietate cu o constanta.</summary>
public sealed class ComparisonCondition : ConditionDefinition
{
    public static readonly string[] Operators = ["==", "!=", "<", "<=", ">", ">="];

    public string Key { get; set; } = "";
    public string Operator { get; set; } = "==";
    public int Value { get; set; }

    public ComparisonCondition() { }
    public ComparisonCondition(string key, string op, int value) { Key = key; Operator = op; Value = value; }

    public override bool Evaluate(IStateReader state)
    {
        int v = state.Get(Key);
        return Operator switch
        {
            "==" => v == Value,
            "!=" => v != Value,
            "<" => v < Value,
            "<=" => v <= Value,
            ">" => v > Value,
            ">=" => v >= Value,
            _ => throw new InvalidOperationException($"Operator necunoscut: {Operator}"),
        };
    }

    public override IEnumerable<string> ReferencedKeys() { yield return Key; }
    public override string ToExpression() => $"{Key} {Operator} {Value}";
}

/// <summary>Baza comuna pentru nodurile compuse.</summary>
public abstract class CompositeCondition : ConditionDefinition
{
    public List<ConditionDefinition> Children { get; set; } = new();
    protected abstract string Keyword { get; }

    public override IEnumerable<string> ReferencedKeys() => Children.SelectMany(c => c.ReferencedKeys());

    public override string ToExpression() =>
        string.Join($" {Keyword} ", Children.Select(c => c is CompositeCondition ? $"({c.ToExpression()})" : c.ToExpression()));
}

public sealed class AndCondition : CompositeCondition
{
    protected override string Keyword => "AND";
    public AndCondition() { }
    public AndCondition(params ConditionDefinition[] children) { Children = children.ToList(); }
    public override bool Evaluate(IStateReader state) => Children.All(c => c.Evaluate(state));
}

public sealed class OrCondition : CompositeCondition
{
    protected override string Keyword => "OR";
    public OrCondition() { }
    public OrCondition(params ConditionDefinition[] children) { Children = children.ToList(); }
    public override bool Evaluate(IStateReader state) => Children.Any(c => c.Evaluate(state));
}
