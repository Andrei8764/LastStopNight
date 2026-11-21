using System.Diagnostics;
using System.Net;
using LastStopNight.Core.Engine;
using LastStopNight.Core.IO;
using LastStopNight.Net;

namespace StoryCheck;

/// <summary>
/// Multiplayer pe localhost, cu TCP real: o gazda si un client joaca KM 13 de la lobby pana la final,
/// plus cazurile grele (egalitate la vot, fiecare alege liber, mers dupa altul, indicii la comun, privit, concluzii,
/// stare stricata, lobby plin, iesire, conexiune inactiva).
/// Gazda asculta doar pe loopback: fara firewall, fara router.
/// </summary>
public static class NetworkTests
{
    private const int TestPort = 37013;
    private static readonly TimeSpan PumpTimeout = TimeSpan.FromSeconds(5);

    public static async Task Run(Report report, IReadOnlyList<string> storyPaths)
    {
        var km = storyPaths.FirstOrDefault(p => p.Contains("km13"));
        if (km is null) return;
        report.Section("Multiplayer (gazdă + client pe localhost)");
        var story = StoryArchive.Load(km);
        await FullGame(report, story);
        await IdleConnectionStaysOpen(report, story);
        JoinCodes(report);
        ProtocolRoundTrip(report);
    }

    /// <summary>Proceseaza mesajele sesiunilor pana cand conditia devine adevarata (sau expira timpul).</summary>
    private static bool Pump(Func<bool> until, params NetSession[] sessions)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < PumpTimeout)
        {
            foreach (var x in sessions) x.Pump(0.01f);
            if (until()) return true;
            Thread.Sleep(5);
        }
        return false;
    }

    private static Task<NetSession> JoinLocal(NetSession host, string name, IReadOnlyList<StoryPackage> catalog) =>
        NetSession.JoinAsync(new IPEndPoint(IPAddress.Loopback, host.Port), name, catalog);

    private static async Task FullGame(Report report, StoryPackage story)
    {
        var host = NetSession.Host(story, "Gazda", [story], TestPort, IPAddress.Loopback);
        var client = await JoinLocal(host, "Client", []); // clientul nu are povestea: gazda trebuie sa i-o trimita

        report.Check(Pump(() => client.HasEngine && client.Lobby.Players.Count == 2, host, client), "clientul intră și primește povestea de la gazdă");
        report.Check(client.HasEngine && StoryArchive.Fingerprint(client.Package!) == StoryArchive.Fingerprint(story), "povestea primită e identică (amprentă)");
        report.Check(client.Lobby.Players.FirstOrDefault(p => p.Id == client.LocalId)?.CharacterId == "vlad", "clientul primește automat personajul liber (Vlad)");
        report.Check(client.Lobby.AllowSplit, "implicit, fiecare alege liber");
        host.SetAllowSplit(false); // primele teste: vot comun
        Pump(() => !client.Lobby.AllowSplit, host, client);
        client.Pick("mara");
        Pump(() => false, host, client);
        report.Check(client.Lobby.Players.First(p => p.Id == client.LocalId).CharacterId == "vlad", "nu poți lua personajul altcuiva");
        report.Check(host.StartBlocker() is not null, "gazda nu poate porni până nu e clientul gata");
        client.SetReady(true);
        report.Check(Pump(() => host.StartBlocker() is null, host, client), "după Gata, tura poate porni");
        host.StartGame();
        report.Check(Pump(() => client.InGame && client.Engine.CurrentBlock is not null, host, client) && client.LocalPlayer == "vlad"
                     && host.Engine.StateHash() == client.Engine.StateHash(), "tura pornește la amândoi, în aceeași stare");

        ClientExamine(report, host, client);
        VotePath(report, host, client);
        TieGoesToFirstVote(report, host, client);
        CorruptedClientIsResynced(report, host, client);
        SplitRoutes(report, host, client);

        var third = await JoinLocal(host, "Al treilea", [story]);
        string? refused = null;
        third.Ended += r => refused = r;
        report.Check(Pump(() => refused is not null, host, client, third), $"al treilea jucător e refuzat ({refused})");

        client.Leave();
        report.Check(Pump(() => host.Lobby.Players.Count == 1, host), "când clientul iese, gazda continuă singură");
        host.Leave();
    }

    /// <summary>Examinarea clientului: gazda o aplica, amandoi vad textul lui Vlad.</summary>
    private static void ClientExamine(Report report, NetSession host, NetSession client)
    {
        string? seenByHost = null, seenByClient = null;
        host.Examined += (_, r) => seenByHost = r.Text;
        client.Examined += (_, r) => seenByClient = r.Text;
        client.RequestExamine(client.Engine.GetExamines().First(e => e.Id == "st.freezer"));
        report.Check(Pump(() => seenByHost is not null && seenByClient is not null, host, client) && seenByClient!.StartsWith("Vlad")
                     && host.Engine.StateHash() == client.Engine.StateHash(), "examinarea clientului ajunge la amândoi (cu replica lui Vlad)");
    }

    /// <summary>Drumul pana la km.final, cu voturi; la km.hitch amandoi aleg decizia lui Vlad.</summary>
    private static void VotePath(Report report, NetSession host, NetSession client)
    {
        int[] path = [0, 1, 0, 0, 0, 0, 0, 3, 0, 0];
        bool allSame = true;
        foreach (var index in path)
        {
            int before = host.Engine.Log.Count;
            string block = host.Engine.CurrentBlock!.Id;
            client.RequestChoose(client.Engine.GetDecisions().First(o => o.Index == index));
            Pump(() => client.Votes.Count == 1 && host.Votes.Count == 1, host, client);
            host.RequestChoose(host.Engine.GetDecisions().First(o => o.Index == index));
            if (!Pump(() => host.Engine.Log.Count > before && client.Engine.Log.Count == host.Engine.Log.Count, host, client))
            {
                allSame = false;
                report.Info($"blocat la {block}");
                break;
            }
            if (host.Engine.StateHash() != client.Engine.StateHash()) allSame = false;
        }
        report.Check(allSame && host.Engine.CurrentBlock!.Id == "km.final"
                     && host.Engine.Log.Any(a => a is ChooseAction c && c.BlockId == "km.hitch" && c.Index == 3),
            "votul comun duce amândoi jucătorii la km.final prin scena lui Vlad (aceeași amprentă la fiecare pas)");
    }

    private static void TieGoesToFirstVote(Report report, NetSession host, NetSession client)
    {
        var finalBlock = host.Engine.CurrentBlock!;
        int first = host.Engine.GetDecisions().First(o => o.IsAvailable).Index;
        int second = host.Engine.GetDecisions().Last(o => o.IsAvailable).Index;
        host.RequestChoose(host.Engine.GetDecisions().First(o => o.Index == first));
        Pump(() => client.Votes.Count == 1, host, client);
        client.RequestChoose(client.Engine.GetDecisions().First(o => o.Index == second));
        Pump(() => host.Engine.CurrentBlock!.Id != finalBlock.Id && client.Engine.Log.Count == host.Engine.Log.Count, host, client);
        report.Check(host.Engine.CurrentBlock!.Id == finalBlock.Decisions[first].Target && host.Engine.StateHash() == client.Engine.StateHash(),
            "la egalitate de voturi câștigă primul vot");
    }

    /// <summary>Desincronizare fortata: gazda observa amprenta diferita si retrimite jurnalul.</summary>
    private static void CorruptedClientIsResynced(Report report, NetSession host, NetSession client)
    {
        host.ReturnToLobby();
        Pump(() => !client.InGame, host, client);
        client.SetReady(true);
        Pump(() => host.StartBlocker() is null, host, client);
        host.StartGame();
        Pump(() => client.InGame && client.Engine.Log.Count == 0 && client.Engine.CurrentBlock?.Id == "km.start", host, client);
        client.Engine.State.Set("player.fear", 99);
        client.RequestChoose(client.Engine.GetDecisions()[0]);
        host.RequestChoose(host.Engine.GetDecisions()[0]);
        report.Check(Pump(() => host.Resyncs > 0 && client.Engine.StateHash() == host.Engine.StateHash(), host, client),
            "o stare stricată la client e reparată automat (resincronizare)");
    }

    /// <summary>
    /// Fiecare alege liber: la km.start gazda alege radioul si pleaca imediat; clientul ramane, apoi merge dupa ea (F).
    /// La radio se despart (fiecare alta decizie, fiecare cu un indiciu): indiciile se aduna pe ambele drumuri.
    /// Clientul termina primul si se uita la gazda; cand termina si ea, amandoi vad concluziile.
    /// </summary>
    private static void SplitRoutes(Report report, NetSession host, NetSession client)
    {
        host.ReturnToLobby();
        Pump(() => !client.InGame, host, client);
        host.SetAllowSplit(true);
        client.SetReady(true);
        Pump(() => host.StartBlocker() is null && client.Lobby.AllowSplit, host, client);
        host.StartGame();
        Pump(() => client.InGame && client.Engine.Log.Count == 0 && client.Engine.CurrentBlock?.Id == "km.start", host, client);
        int hostId = host.LocalId, clientId = client.LocalId;

        host.RequestChoose(host.Engine.GetDecisions().First(o => o.Index == 0));
        bool left = Pump(() => client.IsSplit && client.Follow is not null && client.EngineOf(hostId)?.CurrentBlock?.Id == "km.radio1", host, client);
        report.Check(left && host.Engine.CurrentBlock!.Id == "km.radio1" && client.Engine.CurrentBlock!.Id == "km.start",
            "cine alege pleacă imediat (gazda la radio), fără vot; celălalt rămâne unde era");
        report.Check(client.Follow is { DecisionText: "Dă mai tare la radio" } f && f.Players.SequenceEqual([hostId]) && host.Follow is null,
            "celălalt primește „Mara a ales: Dă mai tare la radio”, cu varianta să meargă după ea");
        report.Check(host.Engine.Party.SequenceEqual(["mara"]) && client.Engine.Party.SequenceEqual(["vlad"]) && !host.RemoteCharacters.Any(),
            "până merge după ea, fiecare drum are doar personajul lui");

        client.FollowNow();
        bool joined = Pump(() => !client.IsSplit || client.OnMyRoute(hostId), host, client)
                      && Pump(() => host.Engine.Party.Count == 2 && client.Engine.StateHash() == host.Engine.StateHash(), host, client);
        report.Check(joined && client.Engine.CurrentBlock!.Id == "km.radio1" && client.Engine.Party.OrderBy(x => x).SequenceEqual(["mara", "vlad"])
                     && client.RemoteCharacters.SequenceEqual(["mara"]) && host.RemoteCharacters.SequenceEqual(["vlad"]) && client.Follow is null,
            "F: clientul merge după gazdă (aceeași stare, Vlad intră înapoi în echipă, se văd în scenă)");
        report.Check(client.Routes.Count == 1 && client.Routes[0].Players.Count == 2 && SameMaps(host, client),
            "pe hartă sunteți din nou pe același drum (cel lăsat gol nu mai apare), la fel pe ambele calculatoare");

        int clues = host.Engine.State.Get("story.clues");
        client.RequestChoose(client.Engine.GetDecisions().First(o => o.Index == 0)); // Schimba postul: +1 indiciu
        Pump(() => client.Engine.CurrentBlock?.Id == "km.trucker" && host.Follow is not null, host, client);
        host.RequestChoose(host.Engine.GetDecisions().First(o => o.Index == 1));     // Asculta pana la capat: +1 indiciu
        bool apart = Pump(() => host.Engine.CurrentBlock?.Id == "km.radio2" && SameRoutes(host, client, hostId, clientId)
                                && client.Engine.State.Get("story.clues") == clues + 2, host, client);
        report.Check(apart && host.Engine.State.Get("story.clues") == clues + 2,
            $"pe drumuri diferite, indiciile se pun la comun ({clues} + 1 al tău + 1 al celuilalt = {clues + 2} pe ambele drumuri)");

        client.Engine.State.Set("player.fear", 99); // stricam drumul clientului
        int resyncs = host.Resyncs;
        client.RequestChoose(client.Engine.GetDecisions().First(o => o.IsAvailable));
        report.Check(Pump(() => host.Resyncs > resyncs && client.IsSplit && SameRoutes(host, client, hostId, clientId), host, client),
            "o stare stricată pe un drum separat e reparată (se retrimit toate drumurile)");

        bool clientDone = PlayToEnd(client, host, client);
        string clientEnding = client.Engine.CurrentBlock!.Id;
        report.Check(clientDone && !client.AllFinished && client.JoinTargets.Any(p => p.Id == hostId), $"clientul ajunge la un final ({clientEnding}), gazda încă joacă");
        client.RequestJoin(hostId);
        bool watching = Pump(() => client.Watching && client.Engine.StateHash() == host.Engine.StateHash() && host.IsWatching(clientId), host, client);
        int before = host.Engine.Log.Count;
        client.RequestChoose(client.Engine.GetDecisions().First(o => o.IsAvailable));
        Pump(() => false, host, client);
        report.Check(watching && host.Engine.Log.Count == before && !host.RemoteCharacters.Any() && client.RemoteCharacters.SequenceEqual(["mara"])
                     && client.EndingOf(clientId)?.CurrentBlock?.Id == clientEnding,
            "după final, clientul se uită la gazdă: vede ce vede ea, dar nu poate alege și nu apare în scena ei");

        bool hostDone = PlayToEnd(host, host, client);
        report.Check(hostDone && Pump(() => client.AllFinished && host.AllFinished, host, client)
                     && client.EndingOf(hostId)?.CurrentBlock?.Id == host.Engine.CurrentBlock!.Id
                     && host.EndingOf(clientId)?.CurrentBlock?.Id == clientEnding && SameMaps(host, client),
            $"când termină și gazda ({host.Engine.CurrentBlock!.Id}), amândoi văd aceleași concluzii (finalul fiecăruia)");
    }

    /// <summary>Alege prima decizie disponibila pana la un final.</summary>
    private static bool PlayToEnd(NetSession player, params NetSession[] all)
    {
        for (int i = 0; i < 40 && !player.Engine.IsFinished; i++)
        {
            int count = player.Engine.Log.Count;
            var option = player.Engine.GetDecisions().FirstOrDefault(o => o.IsAvailable);
            if (option is null) return false;
            player.RequestChoose(option);
            if (!Pump(() => player.Engine.Log.Count > count && all.All(x => x.Routes.Count == all[0].Routes.Count), all)) return false;
        }
        Pump(() => false, all);
        return player.Engine.IsFinished;
    }

    private static bool SameMaps(NetSession host, NetSession client) =>
        host.Routes.Count == client.Routes.Count
        && host.Routes.Zip(client.Routes).All(p => p.First.Timeline == p.Second.Timeline && p.First.Players.SequenceEqual(p.Second.Players)
                                                   && p.First.Engine.StateHash() == p.Second.Engine.StateHash());

    private static bool SameRoutes(NetSession host, NetSession client, int hostId, int clientId) =>
        host.EngineOf(clientId)?.StateHash() == client.Engine.StateHash()
        && client.EngineOf(hostId)?.StateHash() == host.Engine.StateHash()
        && host.Engine.StateHash() != client.Engine.StateHash();

    /// <summary>Semnalul de viata: doi jucatori care nu fac nimic mai mult decat limita de liniste raman conectati.</summary>
    private static async Task IdleConnectionStaysOpen(Report report, StoryPackage story)
    {
        var host = NetSession.Host(story, "Gazda", [story], TestPort, IPAddress.Loopback);
        var client = await JoinLocal(host, "Client", [story]);
        string? ended = null;
        client.Ended += r => ended = r;
        Pump(() => client.Lobby.Players.Count == 2, host, client);
        double idleSeconds = NetSession.SilenceSeconds + 5;
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < idleSeconds)
        {
            host.Pump(0.1f);
            client.Pump(0.1f);
            Thread.Sleep(100);
        }
        report.Check(ended is null && host.Lobby.Players.Count == 2,
            $"după {idleSeconds:0}s fără nicio acțiune, conexiunea rămâne deschisă (semnal de viață)");
        client.Leave();
        host.Leave();
    }

    private static void JoinCodes(Report report)
    {
        var ep = new IPEndPoint(IPAddress.Parse("192.168.1.37"), 27013);
        var code = JoinCode.Encode(ep.Address, ep.Port);
        report.Check(JoinCode.TryParse(code, out var back) && back.Equals(ep)
                     && JoinCode.TryParse(code.ToLowerInvariant().Replace("0", "o"), out var sloppy) && sloppy.Equals(ep)
                     && JoinCode.TryParse("192.168.1.37", out var plainIp) && plainIp.Port == NetJson.DefaultPort,
            $"codul de intrare {code} se decodează corect (și scris cu litere mici, și ca IP)");
        report.Check(!JoinCode.TryParse("ABCDE", out _) && !JoinCode.TryParse("UUUUU-UUUUU", out _), "codurile greșite sunt respinse");
    }

    /// <summary>Fiecare tip de mesaj trece prin JSON si se intoarce la fel (contractul dintre doua exe-uri).</summary>
    private static void ProtocolRoundTrip(Report report)
    {
        NetMessage[] messages =
        [
            new HelloMsg(NetJson.ProtocolVersion, "Ana", ["abc"]),
            new WelcomeMsg(2, "KM 13", "fp", false),
            new RefuseMsg("plin"),
            new PickMsg("vlad"),
            new ReadyMsg(true),
            new StartMsg(["vlad"], new() { [1] = "mara", [2] = "vlad" }),
            new MoveMsg(2, "km.start", 120.5f, true, false),
            new VoteMsg(2, "km.start", 1),
            new VotesMsg(new() { [1] = 0, [2] = 1 }, new() { [0] = 12.5f }),
            new ExamineReqMsg("km.start", "st.freezer"),
            new ActionMsg(3, 0, new ChooseAction("km.start", 0, "mara")),
            new ForkMsg(4, 0, 1, [2]),
            new ActionMsg(5, 1, new PartyAction("km.start", "vlad", ["vlad"])),
            new HashMsg(1, 4, "00ff"),
            new JoinReqMsg(1),
            new JoinMsg(6, 2, 0, true),
            new ActionMsg(7, 1, new ShareAction("km.radio1", "mara", "story.clues", 3)),
            new ResyncMsg(["vlad"], [new TimelineData(0, [1], [new ExamineAction("km.start", "st.poster", "mara")]),
                                     new TimelineData(1, [2], [new PartyAction("km.start", "vlad", ["vlad"])], new ForkOrigin(0, "km.start", 1))], 7,
                [2], new() { [2] = 1 }),
            new ReturnToLobbyMsg(),
            new ByeMsg("gata"),
        ];
        bool all = messages.All(m => NetJson.Serialize(NetJson.Deserialize(NetJson.Serialize(m))!) == NetJson.Serialize(m));
        report.Check(all, $"toate cele {messages.Length} tipuri de mesaje trec prin JSON neschimbate");
    }
}
