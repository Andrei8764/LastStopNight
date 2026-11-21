using LastStopNight.Core.Engine;

namespace LastStopNight.Net;

/// <summary>
/// Drumurile separate. La inceput toti jucatorii sunt pe drumul 0. Cand cineva alege altceva decat ceilalti,
/// grupurile merg mai departe pe drumuri diferite: un motor nou, copie exacta a celui vechi, in care echipa
/// ramane doar cu personajele grupului. Oricine poate trece apoi pe drumul altcuiva („merg dupa el”), iar
/// cine a ajuns la un final se poate uita la ceilalti (privitor: vede scena, dar nu alege nimic).
/// Fiecare calculator tine toate drumurile (nu doar pe al lui), ca gazda sa poata verifica amprenta
/// oricaruia si ca jucatorii sa stie unde e celalalt.
///
/// Motorul local (<see cref="Engine"/>) ramane mereu acelasi obiect, chiar daca jucatorul local trece pe
/// alt drum: la despartire, copia o primeste grupul celalalt; cand treci pe alt drum, motorul tau reface
/// drumul acela (iar drumul lasat in urma primeste o copie). Asa interfata nu trebuie sa se reaboneze.
/// </summary>
public sealed partial class NetSession
{
    /// <summary>Un drum: ID-ul lui, motorul (starea) si jucatorii de pe el (inclusiv cei care au terminat acolo).</summary>
    public sealed record RouteInfo(int Timeline, GameEngine Engine, IReadOnlyList<int> Players);

    /// <summary>Cineva de pe drumul tau a ales si a plecat: poti merge dupa el.</summary>
    public sealed record FollowOffer(int Timeline, IReadOnlyList<int> Players, string DecisionText);

    private const int FirstTimeline = 0;

    private readonly Dictionary<int, GameEngine> _timelines = new(); // drum -> motor
    private readonly Dictionary<int, int> _timelineOf = new();        // jucator -> drum
    private readonly HashSet<int> _watchers = new();                  // jucatori care au terminat si se uita la altii
    private readonly Dictionary<int, int> _finishedOn = new();        // jucator -> drumul pe care a ajuns la final (daca a plecat de acolo)
    private readonly Dictionary<int, ForkOrigin> _origins = new();    // drum -> din ce drum s-a desprins si unde
    private List<string> _startCompanions = new();
    private int _nextTimeline = FirstTimeline + 1;

    /// <summary>Motorul local se reface (trecere pe alt drum, resincronizare): interfata ignora blocurile intermediare.</summary>
    public bool Replaying { get; private set; }

    /// <summary>Doi jucatori sunt pe drumuri diferite (pentru interfata: „Drumuri separate”).</summary>
    public bool IsSplit => _timelines.Count > 1;

    /// <summary>Drumul pe care e un jucator.</summary>
    public int TimelineOf(int playerId) => _timelineOf.GetValueOrDefault(playerId, FirstTimeline);

    /// <summary>Jucatorul e pe acelasi drum cu jucatorul local (il vezi in scena, votati impreuna).</summary>
    public bool OnMyRoute(int playerId) => TimelineOf(playerId) == TimelineOf(LocalId);

    /// <summary>Jucatorul a terminat si se uita la alt drum (nu alege nimic, nu apare in scena).</summary>
    public bool IsWatching(int playerId) => _watchers.Contains(playerId);

    /// <summary>Jucatorul local se uita la altcineva.</summary>
    public bool Watching => _watchers.Contains(LocalId);

    /// <summary>Motorul drumului pe care e un jucator (pentru interfata si teste).</summary>
    public GameEngine? EngineOf(int playerId) => _timelines.GetValueOrDefault(TimelineOf(playerId));

    /// <summary>Finalul la care a ajuns un jucator (null = inca joaca).</summary>
    public GameEngine? EndingOf(int playerId) =>
        _finishedOn.TryGetValue(playerId, out var t) ? _timelines.GetValueOrDefault(t)
        : EngineOf(playerId) is { IsFinished: true } e && !_watchers.Contains(playerId) ? e : null;

    /// <summary>Toti jucatorii au ajuns la un final (ecranul de concluzii).</summary>
    public bool AllFinished => InGame && Lobby.Players.Count > 0 && Lobby.Players.All(p => EndingOf(p.Id) is not null);

    /// <summary>Jucatorii conectati de pe drumul jucatorului local (inclusiv el), fara privitori.</summary>
    public IEnumerable<LobbyPlayer> RoutePlayers => Lobby.Players.Where(p => OnMyRoute(p.Id) && !_watchers.Contains(p.Id));

    /// <summary>Jucatorii de pe alte drumuri care inca joaca: la ei poti trece (sau te poti uita, daca ai terminat).</summary>
    public IEnumerable<LobbyPlayer> JoinTargets =>
        Lobby.Players.Where(p => !OnMyRoute(p.Id) && !_watchers.Contains(p.Id) && EngineOf(p.Id) is { IsFinished: false });

    /// <summary>Toate drumurile turei, in ordinea aparitiei, cu jucatorii de pe fiecare (pentru harta si concluzii).</summary>
    public IReadOnlyList<RouteInfo> Routes =>
        _timelines.OrderBy(kv => kv.Key)
            .Select(kv => new RouteInfo(kv.Key, kv.Value, ShownOn(kv.Key)))
            .Where(r => r.Players.Count > 0)
            .ToList();

    /// <summary>Cine apare pe un drum: cei care joaca pe el si cei care au terminat pe el (nu si privitorii).</summary>
    private List<int> ShownOn(int timeline) =>
        _timelineOf.Where(p => p.Value == timeline && !_watchers.Contains(p.Key)).Select(p => p.Key)
            .Concat(_finishedOn.Where(p => p.Value == timeline).Select(p => p.Key))
            .Distinct().Order().ToList();

    /// <summary>
    /// Drumul tau s-a desprins chiar acum din drumul cuiva care a ales si a plecat (esti tot in blocul acela):
    /// poti merge dupa el. Se calculeaza la fel pe toate calculatoarele, din jurnale.
    /// </summary>
    public FollowOffer? Follow
    {
        get
        {
            if (!InGame || Watching || !_origins.TryGetValue(TimelineOf(LocalId), out var o) || Engine.CurrentBlock?.Id != o.BlockId
                || !_timelines.TryGetValue(o.From, out var other) || other.IsFinished)
                return null;
            var choice = other.Log.Skip(o.LogCount).OfType<ChooseAction>().FirstOrDefault(c => c.BlockId == o.BlockId);
            var players = ActiveOn(o.From);
            if (choice is null || players.Count == 0 || choice.Index >= Engine.CurrentBlock.Decisions.Count) return null;
            return new FollowOffer(o.From, players, Engine.CurrentBlock.Decisions[choice.Index].Text);
        }
    }

    /// <summary>Toti jucatorii conectati de pe un drum.</summary>
    private List<int> PlayersOn(int timeline) => Lobby.Players.Where(p => TimelineOf(p.Id) == timeline).Select(p => p.Id).ToList();

    /// <summary>Jucatorii conectati care chiar joaca pe un drum (fara privitori).</summary>
    private List<int> ActiveOn(int timeline) => PlayersOn(timeline).Where(p => !_watchers.Contains(p)).ToList();

    /// <summary>Un singur drum, cu toti jucatorii: inceputul turei.</summary>
    private void ResetTimelines(IEnumerable<string> companions)
    {
        _startCompanions = companions.ToList();
        _timelines.Clear();
        _timelineOf.Clear();
        _watchers.Clear();
        _finishedOn.Clear();
        _origins.Clear();
        _received.Clear();
        _clueSources.Clear();
        _timelines[FirstTimeline] = Engine;
        _received[FirstTimeline] = new();
        foreach (var id in Assignments.Keys) _timelineOf[id] = FirstTimeline;
        _nextTimeline = FirstTimeline + 1;
    }

    /// <summary>
    /// Jucatorii dati trec pe un drum nou, care porneste din exact aceeasi stare ca drumul <paramref name="from"/>.
    /// Pe gazda si pe client se intampla identic (determinist).
    /// </summary>
    private void Fork(int from, int newTimeline, IReadOnlyCollection<int> players)
    {
        var source = _timelines[from];
        _origins[newTimeline] = new ForkOrigin(from, source.CurrentBlock?.Id ?? "", source.Log.Count);
        var copy = new GameEngine(Package!.Story);
        copy.Replay(_startCompanions, source.Log);
        if (players.Contains(LocalId))
        {
            _timelines[newTimeline] = source; // motorul local merge cu jucatorul local
            _timelines[from] = copy;
        }
        else _timelines[newTimeline] = copy;
        foreach (var p in players) _timelineOf[p] = newTimeline;
        _received[newTimeline] = new HashSet<string>(_received.GetValueOrDefault(from) ?? []);
        _nextTimeline = Math.Max(_nextTimeline, newTimeline + 1);
        Forked?.Invoke(players.ToList());
    }

    /// <summary>
    /// Un jucator trece pe drumul <paramref name="to"/> (merge dupa cineva sau, daca a terminat, se uita).
    /// Drumul lasat in urma ramane (pentru harta si pentru finalul lui). Pe gazda si pe client se intampla identic.
    /// </summary>
    private void MovePlayer(int player, int to, bool watch)
    {
        int from = TimelineOf(player);
        if (from == to || !_timelines.ContainsKey(to)) return;
        if (_timelines.GetValueOrDefault(from) is { IsFinished: true } && !_watchers.Contains(player)) _finishedOn.TryAdd(player, from);
        if (player == LocalId)
        {
            var copy = new GameEngine(Package!.Story);
            copy.Replay(_startCompanions, Engine.Log);
            _timelines[from] = copy;
            Replaying = true;
            try { Engine.Replay(_startCompanions, _timelines[to].Log); }
            finally { Replaying = false; }
            _timelines[to] = Engine;
        }
        _timelineOf[player] = to;
        if (watch) _watchers.Add(player); else _watchers.Remove(player);
        Moved?.Invoke(player);
    }

    /// <summary>Reface toate drumurile dintr-o resincronizare.</summary>
    private void RestoreTimelines(ResyncMsg rs)
    {
        _startCompanions = rs.Companions.ToList();
        _timelines.Clear();
        _timelineOf.Clear();
        _origins.Clear();
        _watchers.Clear();
        _watchers.UnionWith(rs.Watchers);
        _finishedOn.Clear();
        foreach (var kv in rs.FinishedOn) _finishedOn[kv.Key] = kv.Value;
        int local = (rs.Timelines.FirstOrDefault(t => t.Players.Contains(LocalId)) ?? rs.Timelines.FirstOrDefault())?.Id ?? FirstTimeline;
        Replaying = true;
        try
        {
            foreach (var t in rs.Timelines)
            {
                var engine = t.Id == local ? Engine : new GameEngine(Package!.Story);
                engine.Replay(rs.Companions, t.Actions);
                _timelines[t.Id] = engine;
                foreach (var p in t.Players) _timelineOf[p] = t.Id;
                if (t.Origin is not null) _origins[t.Id] = t.Origin;
                _nextTimeline = Math.Max(_nextTimeline, t.Id + 1);
            }
        }
        finally { Replaying = false; }
    }

    private List<TimelineData> SnapshotTimelines() =>
        _timelines.OrderBy(kv => kv.Key)
            .Select(kv => new TimelineData(kv.Key, _timelineOf.Where(p => p.Value == kv.Key).Select(p => p.Key).Order().ToList(),
                kv.Value.Log.ToList(), _origins.GetValueOrDefault(kv.Key)))
            .ToList();

    private ResyncMsg Snapshot() =>
        new(_startCompanions, SnapshotTimelines(), _seq, _watchers.Order().ToList(), new Dictionary<int, int>(_finishedOn));
}
