using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using LastStopNight.Core.Engine;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;

namespace LastStopNight.Net;

/// <summary>Ce se arata despre blocul in care e un jucator de pe alt drum.</summary>
public sealed record StoryBlockInfo(string Title, bool IsEnding);

public sealed record RemoteMove(int PlayerId, string CharacterId, string BlockId, float X, bool FacingLeft, bool Walking);

/// <summary>
/// Sesiunea multiplayer (P2P: unul dintre jucatori e gazda, nu exista server).
/// - Gazda tine lobby-ul, aplica actiunile pe motorul ei si le trimite tuturor, numerotate.
/// - Implicit fiecare alege liber: cine alege pleaca, ceilalti pot merge dupa el (vezi NetSession.Free si NetSession.Timelines).
///   Optional (din lobby), vot comun: gazda numara voturile si castiga majoritatea.
/// - Clientii trimit doar cereri (vot, examinare, pozitie) si aplica actiunile primite, in ordine.
/// - Dupa fiecare actiune, clientii trimit amprenta starii; daca difera, gazda retrimite tot jurnalul.
/// Toata logica ruleaza in <see cref="Pump"/>, apelat de joc pe firul interfetei.
/// </summary>
public sealed partial class NetSession : IGameSession, IDisposable
{
    public const float VoteSeconds = 20f;
    /// <summary>Dupa atatea secunde fara niciun semn de la celalalt, conexiunea e considerata pierduta.</summary>
    public const double SilenceSeconds = 20;
    private const int HostId = 1;

    private readonly List<Connection> _peers = new();          // gazda: clientii; clientul: gazda
    private readonly ConcurrentQueue<Connection> _incoming = new(); // gazda: conexiuni acceptate, inca neprocesate
    private readonly TcpListener? _listener;
    private readonly IReadOnlyList<StoryPackage> _catalog;
    private readonly Dictionary<(int Timeline, int LogCount), string> _hashes = new(); // gazda: amprenta fiecarui drum dupa N actiuni
    private readonly Dictionary<int, int> _votes = new();      // jucator -> index decizie (in blocul curent al drumului lui)
    private readonly List<int> _voteOrder = new();
    private readonly Dictionary<int, Stopwatch> _voteClocks = new(); // gazda: drum -> de cand s-a votat prima data
    private readonly Dictionary<int, float> _clientVoteLeft = new(); // client: drum -> secunde ramase
    private readonly Stopwatch _moveClock = Stopwatch.StartNew();
    private int _nextId = HostId + 1;
    private int _seq;
    private (float X, bool FacingLeft, bool Walking, string BlockId) _lastMove;
    private bool _disposed;

    private NetSession(bool isHost, string name, IReadOnlyList<StoryPackage> catalog, TcpListener? listener)
    {
        IsHost = isHost;
        LocalName = name;
        _catalog = catalog;
        _listener = listener;
    }

    public bool IsHost { get; }
    public string LocalName { get; }
    public int LocalId { get; private set; }
    public int Port { get; private set; }
    public LobbyState Lobby { get; private set; } = new();
    public StoryPackage? Package { get; private set; }
    public GameEngine Engine { get; private set; } = null!;
    public bool HasEngine => Package is not null;
    public bool InGame => Lobby.InGame;
    /// <summary>Personajul acestui calculator (dupa start).</summary>
    public string LocalPlayer { get; private set; } = "";
    public Dictionary<int, string> Assignments { get; private set; } = new();
    /// <summary>Voturile jucatorilor de pe drumul tau (ceilalti voteaza separat, pe drumurile lor).</summary>
    public IReadOnlyDictionary<int, int> Votes => _votes.Where(kv => OnMyRoute(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
    public float VoteSecondsLeft => SecondsLeft(TimelineOf(LocalId));

    private float SecondsLeft(int timeline) => IsHost
        ? _voteClocks.TryGetValue(timeline, out var clock) ? Math.Max(0, VoteSeconds - (float)clock.Elapsed.TotalSeconds) : 0
        : _clientVoteLeft.GetValueOrDefault(timeline);
    public string? Status { get; private set; }
    /// <summary>De cate ori a trebuit gazda sa resincronizeze un client (0 = totul a mers din prima).</summary>
    public int Resyncs { get; private set; }

    public event EventHandler<ExamineResult>? Examined;
    public event Action? LobbyChanged;
    public event Action? Started;
    public event Action? Resynced;
    public event Action? VotesChanged;
    public event Action? ReturnedToLobby;
    public event Action<RemoteMove>? RemoteMoved;
    public event Action<int>? PlayerLeft;
    public event Action<StoryPackage>? StoryReceived;
    /// <summary>Drumurile s-au despartit: acesti jucatori au trecut pe un drum nou.</summary>
    public event Action<IReadOnlyList<int>>? Forked;
    /// <summary>Sesiunea s-a terminat (gazda a iesit, conexiunea a cazut, refuz).</summary>
    public event Action<string>? Ended;

    // ------------------------------------------------------------------ pornire

    /// <summary>Gazduieste: asculta pe primul port liber incepand cu <paramref name="port"/>.</summary>
    /// <param name="bind">Adresa pe care asculta (implicit toate; testele folosesc doar loopback).</param>
    public static NetSession Host(StoryPackage story, string name, IReadOnlyList<StoryPackage> catalog, int port = NetJson.DefaultPort,
                                  IPAddress? bind = null)
    {
        TcpListener? listener = null;
        for (int p = port; p < port + 10 && listener is null; p++)
        {
            try
            {
                var l = new TcpListener(bind ?? IPAddress.Any, p);
                l.Start();
                listener = l;
            }
            catch (SocketException) { }
        }
        if (listener is null) throw new InvalidOperationException($"Nu am găsit niciun port liber între {port} și {port + 9}.");

        var s = new NetSession(true, name, catalog, listener) { LocalId = HostId };
        s.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        s.SetStory(story);
        s.Lobby = new LobbyState
        {
            StoryTitle = story.Story.Title,
            Fingerprint = StoryArchive.Fingerprint(story),
            Players = [new LobbyPlayer { Id = HostId, Name = name, IsHost = true, Ready = true, CharacterId = story.Story.PlayerCharacterId }],
        };
        _ = s.AcceptLoop();
        return s;
    }

    /// <summary>Intra la o gazda. Conectarea e asincrona; rezultatul vine prin Lobby / Ended.</summary>
    public static async Task<NetSession> JoinAsync(IPEndPoint host, string name, IReadOnlyList<StoryPackage> catalog, CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(6));
            try { await tcp.ConnectAsync(host, timeout.Token); }
            catch (OperationCanceledException) { tcp.Dispose(); throw new TimeoutException("Gazda nu răspunde (verifică codul, firewall-ul sau portul)."); }
        }
        var s = new NetSession(false, name, catalog, null);
        var c = new Connection(tcp) { PlayerId = HostId };
        s._peers.Add(c);
        c.Start();
        c.Send(new HelloMsg(NetJson.ProtocolVersion, name, catalog.Select(StoryArchive.Fingerprint).ToList()));
        s.Status = "Așteptăm gazda...";
        return s;
    }

    private void SetStory(StoryPackage pkg)
    {
        Package = pkg;
        Engine = new GameEngine(pkg.Story);
    }

    // ------------------------------------------------------------------ bucla (pe firul interfetei)

    /// <summary>Proceseaza mesajele primite. Se apeleaza des (la fiecare cadru).</summary>
    public void Pump(float dt)
    {
        if (_disposed) return;
        while (_incoming.TryDequeue(out var nc)) _peers.Add(nc);
        if (!IsHost)
            foreach (var t in _clientVoteLeft.Keys.ToList()) _clientVoteLeft[t] = Math.Max(0, _clientVoteLeft[t] - dt);

        foreach (var c in _peers.ToList())
        {
            if (c.IsOpen && (DateTime.UtcNow - c.LastReceived).TotalSeconds > SilenceSeconds)
                c.Close("Celălalt jucător nu mai răspunde (internetul lui a căzut sau a închis jocul).");
            while (c.Inbox.TryDequeue(out var m))
            {
                if (IsHost) HostHandle(c, m); else ClientHandle(m);
                if (_disposed) return;
            }
            if (!c.IsOpen)
            {
                _peers.Remove(c);
                if (IsHost) RemovePlayer(c);
                else { End(c.CloseReason ?? "Conexiunea cu gazda s-a pierdut."); return; }
            }
        }
        if (IsHost)
            foreach (var (timeline, clock) in _voteClocks.ToList())
                if (clock.Elapsed.TotalSeconds >= VoteSeconds) ResolveVotes(timeline, force: true);
    }

    private void End(string reason)
    {
        if (_disposed) return;
        Dispose();
        Ended?.Invoke(reason);
    }

    /// <summary>Anunta interfata ce a vazut cine a examinat (pe gazda si pe client, dupa aplicarea actiunii).</summary>
    private void RaiseExamined(ExamineAction x, string text)
    {
        if (Package!.Story.FindExamine(x.BlockId, x.ExamineId) is { } def)
            Examined?.Invoke(this, new ExamineResult(def, x.Player, text));
    }

    // ------------------------------------------------------------------ actiunile jucatorului local

    public void Pick(string? characterId)
    {
        if (IsHost) { if (Lobby.Players.FirstOrDefault(p => p.Id == LocalId) is { } me) TryPick(me, characterId); }
        else _peers.FirstOrDefault()?.Send(new PickMsg(characterId));
    }

    public void SetReady(bool ready)
    {
        if (IsHost) return;
        _peers.FirstOrDefault()?.Send(new ReadyMsg(ready));
    }

    /// <summary>Decizia jucatorului local (in modul vot comun: votul lui).</summary>
    public void RequestChoose(DecisionOption option)
    {
        if (Engine.CurrentBlock is not { } b || !option.IsAvailable || Watching) return;
        if (IsHost) CastVote(LocalId, b.Id, option.Index);
        else _peers.FirstOrDefault()?.Send(new VoteMsg(LocalId, b.Id, option.Index));
    }

    public void RequestExamine(ExamineDefinition examine)
    {
        if (Engine.CurrentBlock is not { } b || Watching) return;
        if (IsHost) ApplyAndBroadcast(TimelineOf(LocalId), new ExamineAction(b.Id, examine.Id, LocalPlayer));
        else _peers.FirstOrDefault()?.Send(new ExamineReqMsg(b.Id, examine.Id));
    }

    /// <summary>Pozitia personajului local (trimisa de maximum ~15 ori pe secunda, doar cand se schimba).</summary>
    public void SendMove(float x, bool facingLeft, bool walking)
    {
        if (!InGame || Engine.CurrentBlock is not { } b) return;
        var key = (MathF.Round(x, 1), facingLeft, walking, b.Id);
        if (key == _lastMove && _moveClock.ElapsedMilliseconds < 1000) return;
        if (_moveClock.ElapsedMilliseconds < 66) return;
        _moveClock.Restart();
        _lastMove = key;
        var m = new MoveMsg(LocalId, b.Id, x, facingLeft, walking);
        if (IsHost) Broadcast(m); else _peers.FirstOrDefault()?.Send(m);
    }

    /// <summary>Cine de pe drumul tau a votat decizia cu acest index (pentru portretele de pe butoane).</summary>
    public IEnumerable<int> VotersFor(int index) => _votes.Where(kv => kv.Value == index && OnMyRoute(kv.Key)).Select(kv => kv.Key);

    public string CharacterOf(int playerId) => Assignments.GetValueOrDefault(playerId, "");

    /// <summary>Numele jucatorului care are acest personaj (pentru HUD si voturi).</summary>
    public string? NameOfCharacter(string characterId) =>
        Assignments.FirstOrDefault(kv => kv.Value == characterId) is { Value: not null } kv && kv.Key != 0
            ? Lobby.Players.FirstOrDefault(p => p.Id == kv.Key)?.Name : null;

    /// <summary>
    /// Personajele celorlalti jucatori conectati de pe drumul tau (cei de pe alte drumuri nu sunt in scena ta,
    /// iar privitorii nu apar deloc).
    /// </summary>
    public IEnumerable<string> RemoteCharacters =>
        Assignments.Where(kv => kv.Key != LocalId && OnMyRoute(kv.Key) && !IsWatching(kv.Key) && Lobby.Players.Any(p => p.Id == kv.Key))
            .Select(kv => kv.Value);

    /// <summary>Unde e fiecare jucator de pe alt drum: numele, personajul si blocul (pentru interfata).</summary>
    public IEnumerable<(string Name, string CharacterId, StoryBlockInfo Block)> OtherRoutes =>
        Lobby.Players.Where(p => !OnMyRoute(p.Id) && !IsWatching(p.Id) && EngineOf(p.Id)?.CurrentBlock is not null)
            .Select(p => (p.Name, CharacterOf(p.Id), new StoryBlockInfo(EngineOf(p.Id)!.CurrentBlock!.Title, EngineOf(p.Id)!.CurrentBlock!.IsEnding)));

    public void Leave(string reason = "A ieșit din joc.")
    {
        if (_disposed) return;
        if (IsHost) Broadcast(new ByeMsg("Gazda a închis jocul."));
        else _peers.FirstOrDefault()?.Send(new ByeMsg(reason));
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _listener?.Stop(); } catch (SocketException) { }
        foreach (var c in _peers) c.Close("Închis.");
        _peers.Clear();
        while (_incoming.TryDequeue(out var c)) c.Close("Închis.");
    }
}
