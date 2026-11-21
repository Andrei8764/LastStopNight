using System.Net;
using LastStopNight.Game;
using LastStopNight.Net;

namespace LastStopNight;

/// <summary>
/// Moduri de dezvoltare care joaca singure si salveaza capturi (verificari vizuale si de retea).
/// </summary>
public sealed partial class PlayerForm
{
    /// <summary>
    /// Mod de dezvoltare: captureaza ecranele de meniu, apoi parcurge automat o lista de decizii si salveaza
    /// capturi PNG ale ferestrei. Numerele negative inseamna „examineaza obiectul cu indexul -n-1”.
    /// </summary>
    public async Task RunSnapshotsAsync(string outDir, int storyIndex, string? companion, IReadOnlyList<int> decisions, float wait)
    {
        Directory.CreateDirectory(outDir);
        TopMost = true;
        int shot = 0;
        async Task Capture(string name)
        {
            await Task.Delay(TimeSpan.FromSeconds(wait));
            using var bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, ClientSize);
            bmp.Save(Path.Combine(outDir, $"{shot++:00}_{name}.png"));
        }

        if (storyIndex < 0)
        {
            // „continue”: meniul cu Continua, apoi reluarea salvarii
            ShowMainMenu();
            await Capture("menu_continue");
            if (SaveGame.Load() is { } save) ContinueSaved(save);
            await Capture("continued");
            Close();
            return;
        }
        if (storyIndex < _catalog.Count) Select(_catalog[storyIndex]);
        ShowMainMenu();
        await Capture("menu");
        ShowStories();
        await Capture("stories");
        ShowCharacters();
        if (companion is not null && _menu is not null) { _menu.Selected = Math.Max(0, _menu.Items.FindIndex(i => i.Label.StartsWith(char.ToUpper(companion[0]) + companion[1..]))); }
        await Capture("characters");
        StartGame(companion is null ? [] : [companion]);
        foreach (int d in decisions)
        {
            _text.Complete();
            if (d < 0)
            {
                var ex = _engine!.GetExamines()[-d - 1];
                var it = _scene!.Interactables.First(i => i.Examine == ex);
                _scene.Player?.WalkTo(it.X, () => Use(it));
                await Task.Delay(1800);
                await Capture(_engine.CurrentBlock!.Id + "_examine");
                if (_scene?.Followers.Count > 0)
                {
                    await Task.Delay(2500);
                    await Capture(_engine.CurrentBlock!.Id + "_remark");
                }
                continue;
            }
            await Capture(_engine!.CurrentBlock!.Id);
            var option = _engine.GetDecisions().FirstOrDefault(o => o.Index == d && o.IsAvailable)
                ?? throw new ArgumentException($"Blocul {_engine.CurrentBlock.Id} nu are decizia disponibilă cu indexul {d}.");
            var start = _engine.CurrentBlock;
            Activate(option);
            for (int i = 0; i < 200 && _engine.CurrentBlock == start; i++) await Task.Delay(30);
        }
        _text.Complete();
        await Capture(_engine!.CurrentBlock!.Id);
        _showMap = true;
        await Capture("harta");
        _showMap = false;
        if (_mode == Mode.Playing)
        {
            ShowPause();
            await Capture("pause");
            ShowSettings(ShowPause);
            await Capture("settings");
            ResumeGame();
        }
        File.WriteAllText(Path.Combine(outDir, "perf.txt"), $"Timp mediu de desenare pe cadru: {AveragePaintMs:F2} ms ({_paintCount} cadre)");
        Close();
    }

    private bool _testDriven;

    /// <summary>
    /// Mod de dezvoltare: doua instante ale jocului (gazda si client) joaca impreuna KM 13 pe acest PC
    /// si salveaza capturi. Pornire: --mp-test host &lt;dir&gt; si --mp-test join &lt;dir&gt;.
    /// </summary>
    public async Task RunMpTestAsync(string role, string outDir)
    {
        Directory.CreateDirectory(outDir);
        _testDriven = true;
        var screen = Screen.PrimaryScreen!.WorkingArea;
        StartPosition = FormStartPosition.Manual;
        Bounds = new Rectangle(role == "host" ? screen.Left : screen.Left + screen.Width / 2, screen.Top, screen.Width / 2, (int)(screen.Height * 0.92));
        int shot = 0;
        async Task Capture(string name, int delay = 900)
        {
            await Task.Delay(delay);
            using var bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
            DrawToBitmap(bmp, new Rectangle(Point.Empty, ClientSize));
            bmp.Save(Path.Combine(outDir, $"{shot++:00}_{role}_{name}.png"));
        }
        async Task<bool> Until(Func<bool> cond, int ms = 20000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!cond()) { if (sw.ElapsedMilliseconds > ms) return false; await Task.Delay(40); }
            return true;
        }

        int[] path = [0, 1, 0, 0, 0, 0, 0, 3, 0, 0, 0];
        // LASTSTOP_MP_SPLIT=1: la km.start votati diferit, apoi fiecare isi joaca drumul lui pana la un final
        bool splitTest = Environment.GetEnvironmentVariable("LASTSTOP_MP_SPLIT") == "1";
        const int MaxSplitSteps = 30;
        var log = new List<string>();
        try
        {
            if (role == "host")
            {
                _settings.PlayerName = "Andrei";
                Select(_catalog[1]);
                HostGame(_catalog[1]);
                await Capture("lobby_asteapta", 1500);
                log.Add("client intrat: " + await Until(() => _net!.Lobby.Players.Count == 2));
                log.Add("client gata: " + await Until(() => _net!.StartBlocker() is null));
                await Capture("lobby_gata");
                _net!.StartGame();
            }
            else
            {
                _settings.PlayerName = "Prieten";
                await Task.Delay(2500);
                ShowJoin();
                if (Environment.GetEnvironmentVariable("LASTSTOP_JOIN_CODE") is { Length: > 0 } forced)
                {
                    _joinText = forced; // exact codul din lobby, cum l-ar lipi prietenul
                    log.Add($"intru cu codul {forced}");
                }
                else if (Environment.GetEnvironmentVariable("LASTSTOP_JOIN_LAN") == "1")
                {
                    // ca un prieten din aceeasi retea: gaseste jocul in lista si intra prin IP-ul de retea
                    log.Add("joc găsit în rețeaua locală: " + await Until(() => _discovery?.Games.Any(g => NetInfo.Rank(g.EndPoint.Address) == 3) == true, 15000));
                    var game = _discovery!.Games.OrderByDescending(g => NetInfo.Rank(g.EndPoint.Address)).First();
                    log.Add($"intru la {game.EndPoint} ({game.Host}: {game.Story})");
                    _joinText = JoinCode.Encode(game.EndPoint.Address, game.EndPoint.Port);
                }
                else _joinText = JoinCode.Encode(IPAddress.Loopback, NetJson.DefaultPort);
                await Capture("intra_cu_cod", 1200);
                Connect(_joinText);
                log.Add("conectat: " + await Until(() => _net is { HasEngine: true } n && n.Lobby.Players.Count == 2));
                await Capture("lobby_client");
                _net!.SetReady(true);
            }

            log.Add("tura a pornit: " + await Until(() => _mode == Mode.Playing && _engine?.CurrentBlock is not null));
            if (role == "join")
            {
                // clientul se plimba si se uita la congelator; gazda trebuie sa vada bula deasupra lui Vlad
                var ex = _engine!.GetExamines().First(e => e.Id == "st.freezer");
                _scene!.Player!.WalkTo(ex.X, () => Use(_scene.Interactables.First(i => i.Examine == ex)));
                await Capture("examinare", 1800);
            }
            else await Capture("vlad_examineaza", 2600);

            for (int step = 0; step < (splitTest ? MaxSplitSteps : path.Length); step++)
            {
                if (_engine!.CurrentBlock!.IsEnding) break;
                var block = _engine.CurrentBlock.Id;
                await Task.Delay(role == "host" ? 1400 : 700);
                int idx = !splitTest ? path[step]
                    : block == "km.start" ? (role == "host" ? 0 : 1)
                    : _engine.GetDecisions().First(o => o.IsAvailable).Index;
                var opt = _engine.GetDecisions().FirstOrDefault(o => o.Index == idx && o.IsAvailable);
                if (opt is null) { log.Add($"lipsă decizia {idx} în {block}"); break; }
                _session!.RequestChoose(opt);
                if (role == "join" && block is "km.hitch" or "km.start") await Capture("vot_" + block, 400);
                if (role == "host" && block == "km.hitch") await Capture("vot_" + block, 300);
                if (!await Until(() => _engine.CurrentBlock!.Id != block, 25000)) { log.Add($"blocat în {block}"); break; }
                if (splitTest && block == "km.start")
                {
                    log.Add($"despărțiți: {_net!.IsSplit}, eu în {_engine.CurrentBlock!.Id}, echipa [{string.Join(", ", _engine.Party)}]");
                    await Capture("drum_separat", 1800);
                    _showMap = true;
                    await Capture("harta_despartire", 300);
                    _showMap = false;
                }
                else if (_engine.CurrentBlock!.Id is "km.vlad.engine" or "km.final" || _engine.CurrentBlock.IsEnding)
                    await Capture(_engine.CurrentBlock.Id, 1600);
            }
            log.Add($"final: {_engine!.CurrentBlock!.Id}, amprentă {_engine.StateHash()}");
            if (role == "host" && splitTest)
            {
                log.Add("și celălalt a ajuns la un final: " + await Until(() => _net!.OtherRoutes.All(r => r.Block.IsEnding), 60000));
                foreach (var r in _net!.OtherRoutes) log.Add($"  {r.Name} ({r.CharacterId}): {r.Block.Title}");
                await Capture("ambele_finaluri", 800);
                _showMap = true;
                await Capture("harta_finaluri", 300);
                _showMap = false;
            }
            if (role == "host")
            {
                await Task.Delay(2500);
                _net!.ReturnToLobby();
                await Capture("inapoi_in_lobby", 1200);
                await Task.Delay(1500);
            }
            else
            {
                log.Add("înapoi în lobby: " + await Until(() => _mode == Mode.Menu && _menu?.Title == "Lobby", 15000));
                await Capture("client_lobby_dupa_final", 800);
            }
        }
        catch (Exception ex) { log.Add("EROARE: " + ex); }
        File.WriteAllLines(Path.Combine(outDir, $"log_{role}.txt"), log);
        LeaveNet(null, showMenu: false);
        await Task.Delay(400);
        Close();
    }
}
