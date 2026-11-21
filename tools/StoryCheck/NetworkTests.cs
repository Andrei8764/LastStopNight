using System.Diagnostics;
using System.Net;
using LastStopNight.Core.Engine;
using LastStopNight.Core.IO;
using LastStopNight.Net;

namespace StoryCheck;

/// <summary>
/// Multiplayer pe localhost, cu TCP real: o gazda si un client joaca KM 13 de la lobby pana la final,
/// plus cazurile grele (egalitate la vot, drumuri separate, stare stricata, lobby plin, iesire, conexiune inactiva).
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
        host.SetAllowSplit(false); // cu drumuri separate, doua voturi diferite ar desparti jucatorii
        Pump(() => !client.Lobby.AllowSplit, host, client);
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
    /// Drumuri separate: la km.start gazda alege radioul, clientul pompele. Fiecare continua singur, cu echipa
    /// redusa la personajul lui; fiecare calculator tine ambele drumuri, identice cu ale celuilalt.
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
        Pump(() => client.Votes.Count == 1, host, client);
        report.Check(host.Engine.CurrentBlock!.Id == "km.start", "cu un singur vot, drumul așteaptă votul celuilalt");
        client.RequestChoose(client.Engine.GetDecisions().First(o => o.Index == 1));
        bool split = Pump(() => client.IsSplit && client.Engine.CurrentBlock?.Id == "km.pumps" && host.Engine.CurrentBlock?.Id == "km.radio1", host, client);
        report.Check(split && host.IsSplit, "voturi diferite: fiecare merge pe drumul lui (gazda la radio, clientul la pompe)");
        report.Check(host.Engine.Party.SequenceEqual(["mara"]) && client.Engine.Party.SequenceEqual(["vlad"]),
            "după despărțire, fiecare drum are doar personajul lui");
        report.Check(SameRoutes(host, client, hostId, clientId) && !host.RemoteCharacters.Any() && !client.RemoteCharacters.Any(),
            "ambele calculatoare țin ambele drumuri, identice (iar celălalt nu mai apare în scena ta)");
        report.Check(client.Routes.Count == 2 && client.Routes.All(r => r.Engine.Path.SequenceEqual(host.Routes.First(h => h.Timeline == r.Timeline).Engine.Path))
                     && host.Engine.Path.SequenceEqual(["km.start", "km.radio1"]) && client.Engine.Path.SequenceEqual(["km.start", "km.pumps"]),
            "harta: fiecare drum își știe blocurile, la fel pe ambele calculatoare");

        int before = client.Engine.Log.Count;
        client.RequestChoose(client.Engine.GetDecisions().First(o => o.IsAvailable));
        report.Check(Pump(() => client.Engine.Log.Count > before && SameRoutes(host, client, hostId, clientId), host, client)
                     && host.Engine.CurrentBlock!.Id == "km.radio1",
            "pe drumul lui, clientul decide singur, fără să aștepte gazda");

        client.Engine.State.Set("player.fear", 99); // stricam drumul clientului
        int resyncs = host.Resyncs;
        client.RequestChoose(client.Engine.GetDecisions().First(o => o.IsAvailable));
        report.Check(Pump(() => host.Resyncs > resyncs && client.IsSplit && SameRoutes(host, client, hostId, clientId), host, client),
            "o stare stricată pe un drum separat e reparată (se retrimit toate drumurile)");
    }

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
            new ResyncMsg(["vlad"], [new TimelineData(0, [1], [new ExamineAction("km.start", "st.poster", "mara")]),
                                     new TimelineData(1, [2], [new PartyAction("km.start", "vlad", ["vlad"])])], 7),
            new ReturnToLobbyMsg(),
            new ByeMsg("gata"),
        ];
        bool all = messages.All(m => NetJson.Serialize(NetJson.Deserialize(NetJson.Serialize(m))!) == NetJson.Serialize(m));
        report.Check(all, $"toate cele {messages.Length} tipuri de mesaje trec prin JSON neschimbate");
    }
}
