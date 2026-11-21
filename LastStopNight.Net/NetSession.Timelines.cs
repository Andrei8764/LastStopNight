using LastStopNight.Core.Engine;

namespace LastStopNight.Net;

/// <summary>
/// Drumurile separate. La inceput toti jucatorii sunt pe drumul 0. Cand voteaza diferit (si gazda permite),
/// fiecare grup merge mai departe pe drumul lui: un motor nou, copie exacta a celui vechi, in care echipa
/// ramane doar cu personajele grupului. Fiecare calculator tine toate drumurile (nu doar pe al lui),
/// ca gazda sa poata verifica amprenta oricaruia si ca jucatorii sa stie unde e celalalt.
///
/// Motorul local (<see cref="Engine"/>) ramane mereu acelasi obiect, chiar daca jucatorul local trece pe
/// alt drum: la despartire, copia o primeste grupul celalalt. Asa interfata nu trebuie sa se reaboneze.
/// </summary>
public sealed partial class NetSession
{
    /// <summary>Un drum: ID-ul lui, motorul (starea) si jucatorii de pe el.</summary>
    public sealed record RouteInfo(int Timeline, GameEngine Engine, IReadOnlyList<int> Players);

    private const int FirstTimeline = 0;

    private readonly Dictionary<int, GameEngine> _timelines = new(); // drum -> motor
    private readonly Dictionary<int, int> _timelineOf = new();        // jucator -> drum
    private List<string> _startCompanions = new();
    private int _nextTimeline = FirstTimeline + 1;

    /// <summary>Doi jucatori sunt pe drumuri diferite (pentru interfata: „Drumuri separate”).</summary>
    public bool IsSplit => _timelines.Count > 1;

    /// <summary>Drumul pe care e un jucator.</summary>
    public int TimelineOf(int playerId) => _timelineOf.GetValueOrDefault(playerId, FirstTimeline);

    /// <summary>Jucatorul e pe acelasi drum cu jucatorul local (il vezi in scena, votati impreuna).</summary>
    public bool OnMyRoute(int playerId) => TimelineOf(playerId) == TimelineOf(LocalId);

    /// <summary>Motorul drumului pe care e un jucator (pentru interfata si teste).</summary>
    public GameEngine? EngineOf(int playerId) => _timelines.GetValueOrDefault(TimelineOf(playerId));

    /// <summary>Jucatorii conectati de pe drumul jucatorului local (inclusiv el).</summary>
    public IEnumerable<LobbyPlayer> RoutePlayers => Lobby.Players.Where(p => OnMyRoute(p.Id));

    /// <summary>Toate drumurile turei, in ordinea aparitiei, cu jucatorii de pe fiecare (pentru harta).</summary>
    public IReadOnlyList<RouteInfo> Routes =>
        _timelines.OrderBy(kv => kv.Key)
            .Select(kv => new RouteInfo(kv.Key, kv.Value, _timelineOf.Where(p => p.Value == kv.Key).Select(p => p.Key).Order().ToList()))
            .ToList();

    /// <summary>Toti jucatorii conectati de pe un drum.</summary>
    private List<int> PlayersOn(int timeline) => Lobby.Players.Where(p => TimelineOf(p.Id) == timeline).Select(p => p.Id).ToList();

    /// <summary>Un singur drum, cu toti jucatorii: inceputul turei.</summary>
    private void ResetTimelines(IEnumerable<string> companions)
    {
        _startCompanions = companions.ToList();
        _timelines.Clear();
        _timelineOf.Clear();
        _timelines[FirstTimeline] = Engine;
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
        var copy = new GameEngine(Package!.Story);
        copy.Replay(_startCompanions, source.Log);
        if (players.Contains(LocalId))
        {
            _timelines[newTimeline] = source; // motorul local merge cu jucatorul local
            _timelines[from] = copy;
        }
        else _timelines[newTimeline] = copy;
        foreach (var p in players) _timelineOf[p] = newTimeline;
        _nextTimeline = Math.Max(_nextTimeline, newTimeline + 1);
        Forked?.Invoke(players.ToList());
    }

    /// <summary>Reface toate drumurile dintr-o resincronizare.</summary>
    private void RestoreTimelines(List<string> companions, List<TimelineData> timelines)
    {
        _startCompanions = companions.ToList();
        _timelines.Clear();
        _timelineOf.Clear();
        int local = (timelines.FirstOrDefault(t => t.Players.Contains(LocalId)) ?? timelines.FirstOrDefault())?.Id ?? FirstTimeline;
        foreach (var t in timelines)
        {
            var engine = t.Id == local ? Engine : new GameEngine(Package!.Story);
            engine.Replay(companions, t.Actions);
            _timelines[t.Id] = engine;
            foreach (var p in t.Players) _timelineOf[p] = t.Id;
            _nextTimeline = Math.Max(_nextTimeline, t.Id + 1);
        }
    }

    private List<TimelineData> SnapshotTimelines() =>
        _timelines.OrderBy(kv => kv.Key)
            .Select(kv => new TimelineData(kv.Key, _timelineOf.Where(p => p.Value == kv.Key).Select(p => p.Key).Order().ToList(), kv.Value.Log.ToList()))
            .ToList();
}
