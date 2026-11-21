using System.Globalization;
using System.Text.RegularExpressions;
using LastStopNight.Core.Model;

namespace LastStopNight.Core.Expressions;

public sealed class ExpressionException(string message) : Exception(message);

/// <summary>
/// Transforma text de forma „story.evidence &gt;= 2 AND (player.life &gt; 0 OR story.night == 3)”
/// in arborele de conditii. Folosit de editor pentru a edita conditiile ca text.
/// Gramatica:  or := and ("OR" and)* ;  and := atom ("AND" atom)* ;  atom := "(" or ")" | key op int
/// </summary>
public static class ConditionParser
{
    private static readonly Regex Tokenizer =
        new(@"\s*(\(|\)|>=|<=|==|!=|>|<|=|&&|\|\||-?\d+|[A-Za-z_][A-Za-z0-9_.]*)\s*", RegexOptions.Compiled);

    public static ConditionDefinition? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var tokens = Tokenize(text);
        int pos = 0;
        var result = ParseOr(tokens, ref pos);
        if (pos != tokens.Count) throw new ExpressionException($"Simbol neașteptat: „{tokens[pos]}”.");
        return result;
    }

    public static bool TryParse(string? text, out ConditionDefinition? condition, out string? error)
    {
        try { condition = Parse(text); error = null; return true; }
        catch (ExpressionException ex) { condition = null; error = ex.Message; return false; }
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            var m = Tokenizer.Match(text, i);
            if (!m.Success || m.Index != i)
            {
                if (string.IsNullOrWhiteSpace(text[i..])) break;
                throw new ExpressionException($"Caracter nevalid la poziția {i + 1}: „{text[i]}”.");
            }
            tokens.Add(m.Groups[1].Value);
            i += m.Length;
        }
        return tokens;
    }

    private static bool IsKeyword(string? t, string kw, string alt) =>
        t is not null && (t.Equals(kw, StringComparison.OrdinalIgnoreCase) || t == alt);

    private static ConditionDefinition ParseOr(List<string> t, ref int pos)
    {
        var items = new List<ConditionDefinition> { ParseAnd(t, ref pos) };
        while (pos < t.Count && IsKeyword(t[pos], "OR", "||")) { pos++; items.Add(ParseAnd(t, ref pos)); }
        return items.Count == 1 ? items[0] : new OrCondition(Flatten<OrCondition>(items));
    }

    private static ConditionDefinition ParseAnd(List<string> t, ref int pos)
    {
        var items = new List<ConditionDefinition> { ParseAtom(t, ref pos) };
        while (pos < t.Count && IsKeyword(t[pos], "AND", "&&")) { pos++; items.Add(ParseAtom(t, ref pos)); }
        return items.Count == 1 ? items[0] : new AndCondition(Flatten<AndCondition>(items));
    }

    private static ConditionDefinition[] Flatten<T>(List<ConditionDefinition> items) where T : CompositeCondition =>
        items.SelectMany(c => c is T same ? same.Children : [c]).ToArray();

    private static ConditionDefinition ParseAtom(List<string> t, ref int pos)
    {
        if (pos >= t.Count) throw new ExpressionException("Expresie incompletă.");
        if (t[pos] == "(")
        {
            pos++;
            var inner = ParseOr(t, ref pos);
            if (pos >= t.Count || t[pos] != ")") throw new ExpressionException("Lipsește „)”.");
            pos++;
            return inner;
        }
        string key = t[pos++];
        if (!char.IsLetter(key[0]) && key[0] != '_') throw new ExpressionException($"Se aștepta o proprietate, nu „{key}”.");
        if (pos >= t.Count) throw new ExpressionException($"Lipsește operatorul după „{key}”.");
        string op = t[pos++] == "=" ? "==" : t[pos - 1];
        if (!ComparisonCondition.Operators.Contains(op)) throw new ExpressionException($"Operator nevalid: „{op}”.");
        if (pos >= t.Count || !int.TryParse(t[pos], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            throw new ExpressionException($"Se aștepta un număr întreg după „{key} {op}”.");
        pos++;
        return new ComparisonCondition(key, op, value);
    }
}

/// <summary>
/// Transforma text de forma „player.life -35; player.suspicion +20; story.night = 2” in efecte.
/// </summary>
public static class EffectParser
{
    private static readonly Regex Item =
        new(@"^\s*([A-Za-z_][A-Za-z0-9_.]*)\s*(\+=|-=|=|\+|-)?\s*(-?\d+)\s*$", RegexOptions.Compiled);

    public static List<EffectDefinition> Parse(string? text)
    {
        var list = new List<EffectDefinition>();
        if (string.IsNullOrWhiteSpace(text)) return list;
        foreach (var raw in text.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Item.Match(raw);
            if (!m.Success) throw new ExpressionException($"Efect nevalid: „{raw.Trim()}”. Exemplu: player.life -20");
            string key = m.Groups[1].Value, op = m.Groups[2].Value;
            int value = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            list.Add(op switch
            {
                "=" => new EffectDefinition { Key = key, Op = EffectOperation.Set, Value = value },
                "-" or "-=" => new EffectDefinition { Key = key, Op = EffectOperation.Add, Value = -value },
                _ => new EffectDefinition { Key = key, Op = EffectOperation.Add, Value = value },
            });
        }
        return list;
    }

    public static string Format(IEnumerable<EffectDefinition> effects) =>
        string.Join("; ", effects.Select(e => e.ToExpression()));
}
