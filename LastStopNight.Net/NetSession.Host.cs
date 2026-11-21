using System.Diagnostics;
using System.Net.Sockets;
using LastStopNight.Core.Engine;
using LastStopNight.Core.IO;

namespace LastStopNight.Net;

/// <summary>
/// Partea gazdei: accepta jucatori, tine lobby-ul, numara voturile (pe fiecare drum), aplica actiunile
/// si le trimite tuturor, numerotate. Clientul desincronizat primeste toate drumurile din nou.
/// </summary>
public sealed partial class NetSession
{
    private async Task AcceptLoop()
    {
        while (!_disposed && _listener is not null)
        {
            try
            {
                var tcp = await _listener.AcceptTcpClientAsync();
                var c = new Connection(tcp);
                c.Start();
                _incoming.Enqueue(c);
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                if (_disposed) return;
            }
        }
    }


    private void Broadcast(NetMessage m, Connection? except = null)
    {
        foreach (var c in _peers) if (c != except && c.PlayerId != 0) c.Send(m);
    }

    private void BroadcastLobby()
    {
        Broadcast(new LobbyMsg(Lobby));
        LobbyChanged?.Invoke();
    }

    private void HostHandle(Connection c, NetMessage m)
    {
        if (c.PlayerId == 0 && m is not HelloMsg) return; // intai trebuie sa se prezinte
        var player = Lobby.Players.FirstOrDefault(p => p.Id == c.PlayerId);
        switch (m)
        {
            case HelloMsg h:
                if (h.Protocol != NetJson.ProtocolVersion)
                    Refuse(c, $"Versiuni diferite ale jocului (tu: {h.Protocol}, gazda: {NetJson.ProtocolVersion}). Folosiți același exe.");
                else if (Lobby.InGame) Refuse(c, "Tura a început deja. Intră după ce se întorc în lobby.");
                else if (Lobby.Players.Count >= Math.Max(1, Package!.Story.MaxPlayers))
                    Refuse(c, $"Lobby-ul e plin ({Package.Story.MaxPlayers} jucători).");
                else
                {
                    c.PlayerId = _nextId++;
                    bool send = !h.Stories.Contains(Lobby.Fingerprint);
                    c.Send(new WelcomeMsg(c.PlayerId, Lobby.StoryTitle, Lobby.Fingerprint, send));
                    if (send) c.Send(new StoryDataMsg(Convert.ToBase64String(StoryArchive.ToZipBytes(Package!))));
                    var free = Package!.Story.Playable.Select(p => p.CharacterId)
                        .FirstOrDefault(id => Lobby.Players.All(p => p.CharacterId != id));
                    Lobby.Players.Add(new LobbyPlayer { Id = c.PlayerId, Name = Clean(h.Name), CharacterId = free });
                    BroadcastLobby();
                }
                break;
            case PickMsg pick when player is not null && !Lobby.InGame:
                TryPick(player, pick.CharacterId);
                break;
            case ReadyMsg r when player is not null && !Lobby.InGame:
                player.Ready = r.Ready;
                BroadcastLobby();
                break;
            case MoveMsg mv when player is not null && Lobby.InGame:
                var relay = mv with { PlayerId = player.Id };
                Broadcast(relay, c);
                if (Assignments.TryGetValue(player.Id, out var ch))
                    RemoteMoved?.Invoke(new RemoteMove(player.Id, ch, mv.BlockId, mv.X, mv.FacingLeft, mv.Walking));
                break;
            case VoteMsg v when player is not null && Lobby.InGame:
                CastVote(player.Id, v.BlockId, v.Index);
                break;
            case JoinReqMsg j when player is not null && Lobby.InGame:
                HostJoin(player.Id, j.TargetPlayer);
                break;
            case ExamineReqMsg x when player is not null && Lobby.InGame && !IsWatching(player.Id):
                if (Assignments.TryGetValue(player.Id, out var who))
                    ApplyAndBroadcast(TimelineOf(player.Id), new ExamineAction(x.BlockId, x.ExamineId, who));
                break;
            case HashMsg hm when player is not null:
                // clientul raporteaza o stare diferita (sau actiuni in alta ordine): ii retrimitem toate drumurile
                bool known = _hashes.TryGetValue((hm.Timeline, hm.LogCount), out var mine);
                if (hm.LogCount < 0 || (known && mine != hm.Hash))
                {
                    Resyncs++;
                    c.Send(Snapshot());
                }
                break;
            case ByeMsg:
                c.Close("A ieșit.");
                break;
        }
    }

    private static string Clean(string name)
    {
        name = new string(name.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
        return name.Length == 0 ? "Jucător" : name.Length > 16 ? name[..16] : name;
    }

    private static void Refuse(Connection c, string reason)
    {
        c.Send(new RefuseMsg(reason));
        c.Close(reason);
    }

    private void RemovePlayer(Connection c)
    {
        if (c.PlayerId == 0) return;
        Lobby.Players.RemoveAll(p => p.Id == c.PlayerId);
        _votes.Remove(c.PlayerId);
        _voteOrder.Remove(c.PlayerId);
        _watchers.Remove(c.PlayerId);
        BroadcastLobby();
        PlayerLeft?.Invoke(c.PlayerId);
        if (Lobby.InGame) ResolveVotes(TimelineOf(c.PlayerId), force: false);
    }

    private void TryPick(LobbyPlayer player, string? characterId)
    {
        if (characterId is not null && (Package!.Story.FindPlayable(characterId) is null
                                        || Lobby.Players.Any(p => p != player && p.CharacterId == characterId)))
            return; // personaj inexistent sau deja luat
        player.CharacterId = characterId;
        BroadcastLobby();
    }

    /// <summary>De ce nu poate porni inca tura (null = poate).</summary>
    public string? StartBlocker()
    {
        if (!IsHost || Package is null) return "Doar gazda pornește tura.";
        if (Lobby.Players.Count < 2) return "Așteptăm încă un jucător.";
        if (Lobby.Players.Any(p => p.CharacterId is null)) return "Fiecare trebuie să-și aleagă personajul.";
        if (Lobby.Players.All(p => p.CharacterId != Package.Story.PlayerCharacterId))
            return $"Cineva trebuie să fie {Package.Story.FindCharacter(Package.Story.PlayerCharacterId)?.Name ?? "protagonistul"} (spune povestea).";
        if (Lobby.Players.Any(p => !p.Ready && !p.IsHost)) return "Așteptăm să fie toți gata.";
        return null;
    }

    public void Start(IEnumerable<string> companions) => StartGame();

    public void StartGame()
    {
        if (StartBlocker() is not null) return;
        Assignments = Lobby.Players.ToDictionary(p => p.Id, p => p.CharacterId!);
        var comps = Assignments.Values.Where(id => id != Package!.Story.PlayerCharacterId).ToList();
        Lobby.InGame = true;
        _seq = 0;
        _hashes.Clear();
        ClearAllVotes();
        LocalPlayer = Assignments[LocalId];
        Broadcast(new LobbyMsg(Lobby));
        Broadcast(new StartMsg(comps, Assignments));
        Engine.Start(comps);
        ResetTimelines(comps);
        _hashes[(FirstTimeline, 0)] = Engine.StateHash();
        Started?.Invoke();
    }

    /// <summary>Gazda alege, in lobby: fiecare alege liber (Da) sau vot comun (Nu).</summary>
    public void SetAllowSplit(bool allow)
    {
        if (!IsHost || Lobby.AllowSplit == allow) return;
        Lobby.AllowSplit = allow;
        BroadcastLobby();
    }

    /// <summary>Gazda: toata lumea inapoi in lobby (dupa un final sau din pauza).</summary>
    public void ReturnToLobby()
    {
        if (!IsHost) return;
        Lobby.InGame = false;
        foreach (var p in Lobby.Players) p.Ready = p.IsHost;
        ClearAllVotes();
        Broadcast(new ReturnToLobbyMsg());
        BroadcastLobby();
        ReturnedToLobby?.Invoke();
    }

    private void CastVote(int playerId, string blockId, int index)
    {
        int timeline = TimelineOf(playerId);
        if (_timelines.GetValueOrDefault(timeline)?.CurrentBlock is not { } b || b.Id != blockId || index < 0 || index >= b.Decisions.Count) return;
        if (!b.Decisions[index].IsAvailable(_timelines[timeline].State) || IsWatching(playerId)) return;
        if (Lobby.AllowSplit)
        {
            FreeChoose(playerId, timeline, b, index);
            return;
        }
        _voteOrder.Remove(playerId);
        _voteOrder.Add(playerId);
        _votes[playerId] = index;
        if (!_voteClocks.ContainsKey(timeline)) _voteClocks[timeline] = Stopwatch.StartNew();
        if (!ResolveVotes(timeline, force: false)) SendVotes();
    }

    private void SendVotes()
    {
        var left = _voteClocks.Keys.ToDictionary(t => t, SecondsLeft);
        Broadcast(new VotesMsg(new Dictionary<int, int>(_votes), left));
        VotesChanged?.Invoke();
    }

    private void ClearVotes(int timeline)
    {
        foreach (var id in _votes.Keys.Where(id => TimelineOf(id) == timeline).ToList())
        {
            _votes.Remove(id);
            _voteOrder.Remove(id);
        }
        _voteClocks.Remove(timeline);
    }

    private void ClearAllVotes()
    {
        _votes.Clear();
        _voteOrder.Clear();
        _voteClocks.Clear();
    }

    /// <summary>
    /// Votul comun (modul cu „Fiecare alege liber: Nu”): decide cand au votat toti cei de pe drum (sau a expirat timpul).
    /// Castiga majoritatea; la egalitate, varianta celui care a votat primul.
    /// (Ramura cu despartirea pe grupuri ramane pentru voturile date cand gazda schimba modul in timpul turei.)
    /// </summary>
    private bool ResolveVotes(int timeline, bool force)
    {
        var players = PlayersOn(timeline);
        var votes = _votes.Where(kv => players.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        if (votes.Count == 0) { if (force) ClearVotes(timeline); return false; }
        bool everyone = players.All(votes.ContainsKey);
        if (!everyone && !force) return false;
        if (_timelines[timeline].CurrentBlock is not { } block) return false;

        var order = _voteOrder.Where(votes.ContainsKey).ToList();
        var groups = votes.GroupBy(kv => kv.Value)
            .Select(g => (Index: g.Key, Players: g.Select(kv => kv.Key).OrderBy(order.IndexOf).ToList()))
            .OrderByDescending(g => g.Players.Count).ThenBy(g => order.IndexOf(g.Players[0]))
            .ToList();
        ClearVotes(timeline);
        SendVotes();

        if (Lobby.AllowSplit && everyone && groups.Count > 1)
        {
            foreach (var g in groups.Skip(1))
            {
                int newTimeline = _nextTimeline;
                _seq++;
                Fork(timeline, newTimeline, g.Players);
                _hashes[(newTimeline, _timelines[newTimeline].Log.Count)] = _timelines[newTimeline].StateHash();
                Broadcast(new ForkMsg(_seq, timeline, newTimeline, g.Players));
                ContinueAs(newTimeline, block.Id, g.Index, g.Players);
            }
            ContinueAs(timeline, block.Id, groups[0].Index, groups[0].Players);
        }
        else
        {
            var winner = groups[0];
            ApplyAndBroadcast(timeline, new ChooseAction(block.Id, winner.Index, CharacterOf(winner.Players[0], Package!.Story.PlayerCharacterId)));
        }
        return true;
    }

    /// <summary>Dupa despartire: echipa drumului ramane doar cu personajele grupului, apoi se aplica decizia lui.</summary>
    private void ContinueAs(int timeline, string blockId, int index, List<int> players)
    {
        var members = players.Select(p => CharacterOf(p, Package!.Story.PlayerCharacterId)).Distinct().ToList();
        ApplyAndBroadcast(timeline, new PartyAction(blockId, members[0], members));
        ApplyAndBroadcast(timeline, new ChooseAction(blockId, index, members[0]));
    }

    private string CharacterOf(int playerId, string fallback) => Assignments.GetValueOrDefault(playerId, fallback);

    private void ApplyAndBroadcast(int timeline, GameAction action)
    {
        if (_timelines.GetValueOrDefault(timeline) is not { } engine || engine.CurrentBlock?.Id != action.BlockId) return;
        var before = SharedValues(engine);
        string? text;
        try { text = engine.Apply(action); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { return; } // cerere depasita
        _seq++;
        _hashes[(timeline, engine.Log.Count)] = engine.StateHash();
        Broadcast(new ActionMsg(_seq, timeline, action));
        if (action is ExamineAction x && text is not null && timeline == TimelineOf(LocalId)) RaiseExamined(x, text);
        if (action is ShareAction sh && timeline == TimelineOf(LocalId)) Shared?.Invoke(sh);
        if (engine.CurrentBlock?.Id != action.BlockId) ClearVotes(timeline);
        ShareFinds(timeline, action, before);
    }
}
