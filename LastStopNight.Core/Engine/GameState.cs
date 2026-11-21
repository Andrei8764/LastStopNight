using LastStopNight.Core.Model;

namespace LastStopNight.Core.Engine;

/// <summary>
/// Starea curenta a jocului. Incapsuleaza valorile proprietatilor si garanteaza
/// ca ele raman mereu in intervalul [min, max] definit in poveste.
/// </summary>
public sealed class GameState : IStateReader
{
    private readonly Dictionary<string, int> _values = new();
    private readonly Dictionary<string, StatePropertyDefinition> _definitions;
    private readonly HashSet<string> _examined = new();
    private readonly HashSet<string> _party = new();

    public GameState(IEnumerable<StatePropertyDefinition> definitions)
    {
        _definitions = definitions.ToDictionary(d => d.Key);
        Reset();
    }

    public IReadOnlyDictionary<string, int> Values => _values;
    /// <summary>Creste la fiecare modificare; util pentru a redesena HUD-ul doar cand e nevoie.</summary>
    public int Version { get; private set; }
    public IReadOnlyCollection<string> Examined => _examined;
    public bool WasExamined(string id) => _examined.Contains(id);
    public IReadOnlyCollection<string> Party => _party;
    public bool HasCharacter(string characterId) => _party.Contains(characterId);

    internal void SetParty(IEnumerable<string> ids)
    {
        _party.Clear();
        _party.UnionWith(ids);
        Version++;
    }
    internal bool MarkExamined(string id) => _examined.Add(id);

    public void Reset()
    {
        // echipa nu se reseteaza: e aleasa inainte de start si ramane pe toata tura
        _values.Clear();
        _examined.Clear();
        Version++;
        foreach (var d in _definitions.Values) _values[d.Key] = d.Clamp(d.Initial);
    }

    public bool Has(string key) => _values.ContainsKey(key);

    public int Get(string key) =>
        _values.TryGetValue(key, out var v) ? v : throw new KeyNotFoundException($"Proprietate necunoscută: {key}");

    /// <summary>Seteaza valoarea, limitata la [min, max]. Returneaza modificarea efectiva.</summary>
    public PropertyChange Set(string key, int value)
    {
        if (!_definitions.TryGetValue(key, out var def))
            throw new KeyNotFoundException($"Proprietate necunoscută: {key}");
        int old = _values[key];
        _values[key] = def.Clamp(value);
        Version++;
        return new PropertyChange(key, old, _values[key]);
    }

    public PropertyChange Apply(EffectDefinition effect) => Set(effect.Key, effect.Compute(Get(effect.Key)));
}
