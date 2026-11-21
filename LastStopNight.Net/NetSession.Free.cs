using LastStopNight.Core.Engine;
using LastStopNight.Core.Model;

namespace LastStopNight.Net;

/// <summary>
/// Modul „fiecare alege liber” (<see cref="LobbyState.AllowSplit"/>): fara vot si fara asteptare.
/// - Cine alege pleaca imediat; ceilalti de pe drum raman in bloc, pe un drum nou, si pot merge dupa el
///   (<see cref="Follow"/>) sau pot alege altceva.
/// - Oricine poate trece oricand pe drumul altcuiva care inca joaca; cine a terminat se poate uita la ceilalti.
/// - Proprietatile puse la comun (indiciile) gasite pe un drum se aduna si pe celelalte drumuri.
/// Totul e decis de gazda si trimis ca actiuni numerotate, deci ramane determinist pe toate calculatoarele.
/// </summary>
public sealed partial class NetSession
{
    private readonly Dictionary<int, HashSet<string>> _received = new();                     // gazda: drum -> sursele de indicii incluse in valorile lui
    private readonly Dictionary<string, (string Key, int Delta)> _clueSources = new();         // gazda: sursa -> proprietatea si cat a adus

    /// <summary>Un jucator a trecut pe alt drum (merge dupa cineva sau se uita).</summary>
    public event Action<int>? Moved;

    /// <summary>Alt drum a gasit ceva pus la comun (ex. un indiciu) si a ajuns si pe drumul tau.</summary>
    public event Action<ShareAction>? Shared;

    /// <summary>Jucatorul local trece pe drumul altui jucator (sau se uita la el, daca a terminat).</summary>
    public void RequestJoin(int targetPlayer)
    {
        if (!InGame) return;
        if (IsHost) HostJoin(LocalId, targetPlayer);
        else _peers.FirstOrDefault()?.Send(new JoinReqMsg(targetPlayer));
    }

    /// <summary>Mergi dupa cine a ales si a plecat chiar acum de pe drumul tau.</summary>
    public void FollowNow()
    {
        if (Follow is { } f) RequestJoin(f.Players[0]);
    }

    private void HostJoin(int player, int targetPlayer)
    {
        if (!Lobby.InGame || player == targetPlayer || Lobby.Players.All(p => p.Id != targetPlayer) || _watchers.Contains(targetPlayer)) return;
        int from = TimelineOf(player), to = TimelineOf(targetPlayer);
        if (from == to || _timelines.GetValueOrDefault(to) is not { CurrentBlock: { } block } target || target.IsFinished) return;
        bool watch = _watchers.Contains(player) || _finishedOn.ContainsKey(player) || _timelines.GetValueOrDefault(from)?.IsFinished == true;
        _votes.Remove(player);
        _voteOrder.Remove(player);
        _seq++;
        MovePlayer(player, to, watch);
        Broadcast(new JoinMsg(_seq, player, to, watch));
        if (!watch)
        {
            // personajul lui intra in echipa drumului (cu textele si deciziile lui)
            var members = ActiveOn(to).Select(p => CharacterOf(p, Package!.Story.PlayerCharacterId)).Distinct().ToList();
            ApplyAndBroadcast(to, new PartyAction(block.Id, CharacterOf(player, members[0]), members));
        }
        if (Lobby.InGame && !Lobby.AllowSplit) ResolveVotes(from, force: false);
    }

    /// <summary>
    /// Cine alege pleaca imediat. Ceilalti de pe drum (in afara de cel a carui decizie e, ex. „Lasa-l pe Vlad...”,
    /// care merge cu el) raman in bloc pe un drum nou. Privitorii raman cu cel care a ales.
    /// </summary>
    private void FreeChoose(int playerId, int timeline, StoryBlock block, int index)
    {
        if (_watchers.Contains(playerId)) return;
        var decision = block.Decisions[index];
        var active = ActiveOn(timeline);
        var group = new List<int> { playerId };
        group.AddRange(active.Where(p => p != playerId && decision.RequiresCharacter is { } rc && CharacterOf(p, "") == rc));
        var others = active.Except(group).ToList();
        if (others.Count == 0)
        {
            ApplyAndBroadcast(timeline, new ChooseAction(block.Id, index, CharacterOf(playerId, Package!.Story.PlayerCharacterId)));
            return;
        }
        int newTimeline = _nextTimeline;
        _seq++;
        Fork(timeline, newTimeline, others);
        _hashes[(newTimeline, _timelines[newTimeline].Log.Count)] = _timelines[newTimeline].StateHash();
        Broadcast(new ForkMsg(_seq, timeline, newTimeline, others));
        var stay = others.Select(p => CharacterOf(p, Package!.Story.PlayerCharacterId)).Distinct().ToList();
        ApplyAndBroadcast(newTimeline, new PartyAction(block.Id, stay[0], stay));
        ContinueAs(timeline, block.Id, index, group);
    }

    /// <summary>Valorile proprietatilor puse la comun, inainte de o actiune (ca sa vedem ce a gasit).</summary>
    private Dictionary<string, int> SharedValues(GameEngine engine) =>
        Package!.Story.Properties.Where(p => p.Shared).ToDictionary(p => p.Key, p => engine.State.Get(p.Key));

    /// <summary>
    /// Gazda: dupa o decizie sau o examinare, ce a crescut o proprietate pusa la comun ajunge si pe celelalte drumuri
    /// care inca joaca. Sursa (decizia sau obiectul) se numara o singura data: daca doua drumuri gasesc acelasi
    /// indiciu, nu se aduna de doua ori.
    /// </summary>
    private void ShareFinds(int timeline, GameAction action, Dictionary<string, int> before)
    {
        if (before.Count == 0 || action is not (ChooseAction or ExamineAction)) return;
        var engine = _timelines[timeline];
        var mine = _received.TryGetValue(timeline, out var r) ? r : _received[timeline] = new();
        string where = action is ChooseAction c ? $"{c.BlockId}#{c.Index}" : $"x:{((ExamineAction)action).ExamineId}";
        var fresh = new List<string>();
        foreach (var (key, old) in before)
        {
            int delta = engine.State.Get(key) - old;
            if (delta <= 0) continue;
            string source = $"{key}|{where}";
            if (mine.Add(source))
            {
                _clueSources[source] = (key, delta);
                fresh.Add(source);
            }
            else if (!engine.IsFinished && engine.CurrentBlock is { } b)
                // l-a gasit deja alt drum si a ajuns aici: nu-l numaram de doua ori
                ApplyAndBroadcast(timeline, new ShareAction(b.Id, action.Player, key, engine.State.Get(key) - delta));
        }
        if (fresh.Count == 0) return;
        foreach (var (other, e) in _timelines.ToList())
        {
            if (other == timeline || e.IsFinished || e.CurrentBlock is not { } b || ActiveOn(other).Count == 0) continue;
            var got = _received.TryGetValue(other, out var g) ? g : _received[other] = new();
            foreach (var group in fresh.Where(got.Add).GroupBy(s => _clueSources[s].Key))
                ApplyAndBroadcast(other, new ShareAction(b.Id, action.Player, group.Key, e.State.Get(group.Key) + group.Sum(s => _clueSources[s].Delta)));
        }
    }
}
