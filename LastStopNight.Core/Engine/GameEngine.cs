using System.Text;
using LastStopNight.Core.Model;

namespace LastStopNight.Core.Engine;

/// <summary>
/// Motorul povestii: parcurge graful de blocuri, evalueaza conditiile, aplica efectele
/// si verifica redirectionarile automate. Nu depinde de interfata grafica.
/// Este determinist: nu foloseste numere aleatoare sau ora, deci aceleasi actiuni dau aceeasi stare
/// (vezi <see cref="Log"/>, <see cref="Replay"/> si <see cref="StateHash"/>).
/// </summary>
public sealed class GameEngine
{
    private readonly List<GameAction> _log = new();
    private readonly List<string> _path = new();

    public GameEngine(StoryDefinition story)
    {
        Story = story ?? throw new ArgumentNullException(nameof(story));
        State = new GameState(story.Properties);
    }

    public StoryDefinition Story { get; }
    public GameState State { get; }
    public StoryBlock? CurrentBlock { get; private set; }
    public bool IsFinished => CurrentBlock?.IsEnding == true;

    /// <summary>Toate actiunile aplicate de la start, in ordine.</summary>
    public IReadOnlyList<GameAction> Log => _log;

    /// <summary>Blocurile prin care a trecut tura, in ordine, de la start pana la cel curent (pentru harta).</summary>
    public IReadOnlyList<string> Path => _path;

    /// <summary>Echipa: protagonistul, apoi insotitorii.</summary>
    public IReadOnlyCollection<string> Party => State.Party;

    /// <summary>Insotitorii din echipa (fara protagonist).</summary>
    public IEnumerable<string> Companions => State.Party.Where(p => p != Story.PlayerCharacterId);

    public event EventHandler<BlockChangedEventArgs>? BlockChanged;
    public event EventHandler<StateChangedEventArgs>? StateChanged;

    /// <summary>Porneste tura. Insotitorii necunoscuti sau in plus fata de MaxPlayers sunt ignorati.</summary>
    public void Start(IEnumerable<string>? companions = null)
    {
        var party = new List<string> { Story.PlayerCharacterId };
        foreach (var c in companions ?? [])
            if (c != Story.PlayerCharacterId && Story.FindPlayable(c) is not null && !party.Contains(c)
                && party.Count < Math.Max(1, Story.MaxPlayers))
                party.Add(c);
        State.SetParty(party);
        State.Reset();
        _log.Clear();
        _path.Clear();
        var start = Story.FindBlock(Story.StartBlockId)
                    ?? throw new InvalidOperationException($"Blocul de start „{Story.StartBlockId}” nu există.");
        MoveTo(start, null);
        StateChanged?.Invoke(this, new StateChangedEventArgs([]));
    }

    /// <summary>Reporneste si reaplica un sir de actiuni (ex. „Continua” dintr-o salvare).</summary>
    public void Replay(IEnumerable<string> companions, IEnumerable<GameAction> actions)
    {
        Start(companions);
        foreach (var a in actions) Apply(a);
    }

    /// <summary>Textul blocului curent, cu paragrafele insotitorilor din echipa.</summary>
    public string CurrentText => CurrentBlock?.TextFor(Party) ?? "";

    public IReadOnlyList<DecisionOption> GetDecisions()
    {
        if (CurrentBlock is null) return [];
        return CurrentBlock.Decisions
            .Select((d, i) => new DecisionOption(i, d, d.IsAvailable(State)))
            .Where(o => o.Decision.IsVisible(State) && (o.IsAvailable || o.Decision.ShowWhenLocked))
            .ToList();
    }

    /// <summary>Aplica o actiune. Intoarce textul vazut, pentru examinari (altfel null).</summary>
    public string? Apply(GameAction action)
    {
        if (CurrentBlock is null) throw new InvalidOperationException("Jocul nu a pornit.");
        if (action.BlockId != CurrentBlock.Id)
            throw new InvalidOperationException($"Acțiunea e pentru blocul „{action.BlockId}”, dar jocul e în „{CurrentBlock.Id}”.");
        switch (action)
        {
            case ChooseAction c:
                if (c.Index < 0 || c.Index >= CurrentBlock.Decisions.Count)
                    throw new ArgumentOutOfRangeException(nameof(action), $"Decizia {c.Index} nu există în „{CurrentBlock.Id}”.");
                ChooseInternal(CurrentBlock.Decisions[c.Index], c);
                return null;
            case ExamineAction x:
                var ex = GetExamines().FirstOrDefault(e => e.Id == x.ExamineId)
                         ?? throw new InvalidOperationException($"Obiectul „{x.ExamineId}” nu poate fi examinat aici.");
                return ExamineInternal(ex, x);
            case PartyAction p:
                if (p.Members.Count == 0 || p.Members.Any(m => Story.FindPlayable(m) is null))
                    throw new ArgumentException("Echipa poate avea doar personaje jucabile.", nameof(action));
                _log.Add(p);
                State.SetParty(p.Members.Distinct());
                StateChanged?.Invoke(this, new StateChangedEventArgs([]));
                return null;
            case ShareAction sh:
                if (Story.FindProperty(sh.Key) is not { Shared: true })
                    throw new ArgumentException($"Proprietatea „{sh.Key}” nu e pusă la comun.", nameof(action));
                _log.Add(sh);
                var change = State.Set(sh.Key, sh.Value);
                StateChanged?.Invoke(this, new StateChangedEventArgs(change.Delta != 0 ? [change] : []));
                return null;
            default:
                throw new NotSupportedException(action.GetType().Name);
        }
    }

    public void Choose(int decisionIndex)
    {
        if (CurrentBlock is null) throw new InvalidOperationException("Jocul nu a pornit.");
        Apply(new ChooseAction(CurrentBlock.Id, decisionIndex, Story.PlayerCharacterId));
    }

    public void Choose(DecisionDefinition decision)
    {
        if (CurrentBlock is null || !CurrentBlock.Decisions.Contains(decision))
            throw new InvalidOperationException("Decizia nu aparține blocului curent.");
        Apply(new ChooseAction(CurrentBlock.Id, CurrentBlock.Decisions.IndexOf(decision), Story.PlayerCharacterId));
    }

    private void ChooseInternal(DecisionDefinition decision, ChooseAction action)
    {
        if (!decision.IsAvailable(State))
            throw new InvalidOperationException("Decizia nu este disponibilă în starea curentă.");
        _log.Add(action);

        var changes = decision.Effects.Select(State.Apply).Where(c => c.Delta != 0).ToList();
        if (changes.Count > 0) StateChanged?.Invoke(this, new StateChangedEventArgs(changes));

        var redirect = FindRedirect();
        var targetId = redirect?.Target ?? decision.Target;
        var target = Story.FindBlock(targetId)
                     ?? throw new InvalidOperationException($"Blocul destinație „{targetId}” nu există.");
        MoveTo(target, redirect);
    }

    /// <summary>Obiectele de examinat din blocul curent (ale locatiei + ale blocului), filtrate dupa conditie.</summary>
    public IReadOnlyList<ExamineDefinition> GetExamines()
    {
        if (CurrentBlock is null || CurrentBlock.IsEnding) return [];
        var loc = Story.FindLocation(CurrentBlock.Location);
        return (loc?.Examine ?? []).Concat(CurrentBlock.Examine)
            .Where(e => e.Condition?.Evaluate(State) ?? true).ToList();
    }

    /// <summary>
    /// Protagonistul se uita la un obiect: intoarce textul si aplica efectele (o singura data, daca Once).
    /// Poate declansa o redirectionare (ex. o examinare periculoasa).
    /// </summary>
    public string Examine(ExamineDefinition examine)
    {
        if (CurrentBlock is null) throw new InvalidOperationException("Jocul nu a pornit.");
        return ExamineInternal(examine, new ExamineAction(CurrentBlock.Id, examine.Id, Story.PlayerCharacterId));
    }

    private string ExamineInternal(ExamineDefinition examine, ExamineAction action)
    {
        _log.Add(action);
        bool first = State.MarkExamined(examine.Id);
        if (first || !examine.Once)
        {
            var changes = examine.Effects.Select(State.Apply).Where(c => c.Delta != 0).ToList();
            if (changes.Count > 0) StateChanged?.Invoke(this, new StateChangedEventArgs(changes));
            if (FindRedirect() is { } r && Story.FindBlock(r.Target) is { } target) MoveTo(target, r);
        }
        return examine.TextFor(action.Player);
    }

    /// <summary>
    /// Amprenta starii (bloc, valori, obiecte examinate, echipa), stabila intre calculatoare.
    /// Doua jocuri cu aceeasi amprenta sunt in aceeasi stare.
    /// </summary>
    public string StateHash()
    {
        var sb = new StringBuilder();
        sb.Append(CurrentBlock?.Id).Append('|');
        foreach (var kv in State.Values.OrderBy(k => k.Key, StringComparer.Ordinal)) sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
        sb.Append('|');
        foreach (var e in State.Examined.OrderBy(k => k, StringComparer.Ordinal)) sb.Append(e).Append(';');
        sb.Append('|');
        foreach (var p in State.Party.OrderBy(k => k, StringComparer.Ordinal)) sb.Append(p).Append(';');
        return Fnv1a64(sb.ToString()).ToString("x16");
    }

    /// <summary>FNV-1a pe 64 de biti: acelasi rezultat pe orice calculator (string.GetHashCode difera de la o rulare la alta).</summary>
    private static ulong Fnv1a64(string text)
    {
        const ulong offsetBasis = 14695981039346656037, prime = 1099511628211;
        ulong hash = offsetBasis;
        foreach (char ch in text) { hash ^= ch; hash *= prime; }
        return hash;
    }

    private RedirectDefinition? FindRedirect() =>
        Story.Redirects.FirstOrDefault(r => r.Condition is not null && r.Condition.Evaluate(State));

    private void MoveTo(StoryBlock block, RedirectDefinition? redirect)
    {
        CurrentBlock = block;
        _path.Add(block.Id);
        BlockChanged?.Invoke(this, new BlockChangedEventArgs(block, redirect));
    }
}
