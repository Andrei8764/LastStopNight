using LastStopNight.Core.Engine;
using LastStopNight.Core.IO;

namespace LastStopNight.Net;

/// <summary>
/// Partea clientului: aplica, in ordine, actiunile confirmate de gazda (pe drumul fiecareia) si ii trimite
/// dupa fiecare amprenta starii, ca gazda sa observe orice desincronizare.
/// </summary>
public sealed partial class NetSession
{

    private void ClientHandle(NetMessage m)
    {
        var host = _peers.FirstOrDefault();
        switch (m)
        {
            case WelcomeMsg w:
                LocalId = w.YourId;
                Status = w.SendingStory ? "Primim povestea de la gazdă..." : null;
                if (!w.SendingStory)
                {
                    var pkg = _catalog.FirstOrDefault(p => StoryArchive.Fingerprint(p) == w.Fingerprint);
                    if (pkg is null) { End("Nu am găsit povestea gazdei."); return; }
                    SetStory(pkg);
                }
                break;
            case StoryDataMsg sd:
                using (var ms = new MemoryStream(Convert.FromBase64String(sd.Zip)))
                {
                    var pkg = StoryArchive.LoadZip(ms);
                    SetStory(pkg);
                    Status = null;
                    StoryReceived?.Invoke(pkg);
                }
                break;
            case RefuseMsg r:
                End(r.Reason);
                return;
            case LobbyMsg l:
                Lobby = l.Lobby;
                LobbyChanged?.Invoke();
                break;
            case StartMsg st when HasEngine:
                Assignments = st.Assignments;
                LocalPlayer = Assignments.GetValueOrDefault(LocalId, "");
                Lobby.InGame = true;
                _seq = 0;
                _votes.Clear();
                _clientVoteLeft.Clear();
                Engine.Start(st.Companions);
                ResetTimelines(st.Companions);
                Started?.Invoke();
                break;
            case ActionMsg a when HasEngine:
                if (a.Seq != _seq + 1 || !_timelines.TryGetValue(a.Timeline, out var engine))
                {
                    host?.Send(new HashMsg(a.Timeline, -1, "")); // ordine gresita sau drum necunoscut: cerem resincronizare
                    break;
                }
                _seq = a.Seq;
                string? text;
                try { text = engine.Apply(a.Action); }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    host?.Send(new HashMsg(a.Timeline, engine.Log.Count, "desincronizat"));
                    break;
                }
                if (a.Action is ExamineAction x && text is not null && a.Timeline == TimelineOf(LocalId)) RaiseExamined(x, text);
                if (a.Action is ShareAction sh && a.Timeline == TimelineOf(LocalId)) Shared?.Invoke(sh);
                host?.Send(new HashMsg(a.Timeline, engine.Log.Count, engine.StateHash()));
                break;
            case ForkMsg f when HasEngine:
                if (f.Seq != _seq + 1 || !_timelines.ContainsKey(f.From))
                {
                    host?.Send(new HashMsg(f.From, -1, ""));
                    break;
                }
                _seq = f.Seq;
                Fork(f.From, f.NewTimeline, f.Players);
                break;
            case JoinMsg j when HasEngine:
                if (j.Seq != _seq + 1 || !_timelines.ContainsKey(j.To))
                {
                    host?.Send(new HashMsg(j.To, -1, ""));
                    break;
                }
                _seq = j.Seq;
                MovePlayer(j.Player, j.To, j.Watch);
                break;
            case ResyncMsg rs when HasEngine:
                RestoreTimelines(rs);
                _seq = rs.Seq;
                Resynced?.Invoke();
                break;
            case VotesMsg v:
                _votes.Clear();
                foreach (var kv in v.Votes) _votes[kv.Key] = kv.Value;
                _clientVoteLeft.Clear();
                foreach (var kv in v.SecondsLeft) _clientVoteLeft[kv.Key] = kv.Value;
                VotesChanged?.Invoke();
                break;
            case MoveMsg mv:
                if (Assignments.TryGetValue(mv.PlayerId, out var ch))
                    RemoteMoved?.Invoke(new RemoteMove(mv.PlayerId, ch, mv.BlockId, mv.X, mv.FacingLeft, mv.Walking));
                break;
            case ReturnToLobbyMsg:
                Lobby.InGame = false;
                _votes.Clear();
                ReturnedToLobby?.Invoke();
                break;
            case ByeMsg b:
                End(b.Reason);
                return;
        }
    }
}
