using System.Net;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Net;
using LastStopNight.Ui;
using LastStopNight.Ui.Views;
using static LastStopNight.Ui.MenuStyle;
using MenuItem = LastStopNight.Ui.MenuItem;

namespace LastStopNight;

/// <summary>
/// Multiplayer: gazduire, intrare cu cod sau din reteaua locala, lobby si sincronizarea in joc.
/// Logica de retea e in LastStopNight.Net (NetSession); aici e doar legatura cu interfata.
/// </summary>
public sealed partial class PlayerForm
{
    private NetSession? _net;

    private LanDiscovery? _discovery;

    private PortMapping? _portMap;

    private bool _portMapPending;

    private readonly Dictionary<string, (float X, bool FacingLeft, string BlockId)> _remotePos = new();

    private string _joinText = "";

    private string? _joinStatus;

    private bool _joining;

    private float _lanRefresh;

    /// <summary>LASTSTOP_LOOPBACK=1: doar pe acest PC (teste automate: fara firewall, fara UPnP pe router).</summary>
    private static bool LoopbackOnly => Environment.GetEnvironmentVariable("LASTSTOP_LOOPBACK") == "1";

    private void ShowMultiplayer()
    {
        var m = new MenuScreen("Multiplayer") { Subtitle = "în doi, fără server", Back = ShowMainMenu };
        m.Items.Add(new MenuItem("Găzduiește o tură", () => ShowStories(host: true))
        {
            Hint = "Alegi povestea și primești un cod. Prietenul tău îl lipește la „Intră cu un cod”.",
        });
        m.Items.Add(new MenuItem("Intră cu un cod", ShowJoin) { Hint = "Sau alege un joc găzduit în aceeași rețea." });
        m.Items.Add(new MenuItem("Numele tău")
        {
            Value = () => _settings.PlayerName + (m.Current?.Label == "Numele tău" && (int)(m.Time * 2) % 2 == 0 ? "_" : " "),
            TypeChar = c => EditName(c),
            Hint = "Scrie direct de la tastatură. Așa te vede celălalt jucător.",
        });
        m.Items.Add(new MenuItem("Înapoi", ShowMainMenu));
        OpenMenu(m);
    }

    private void EditName(char c)
    {
        if (c == '\b') { if (_settings.PlayerName.Length > 0) _settings.PlayerName = _settings.PlayerName[..^1]; }
        else if (_settings.PlayerName.Length < 16 && (char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')) _settings.PlayerName += c;
        _settings.Save();
    }

    private void HostGame(StoryPackage pkg)
    {
        LeaveNet(null, showMenu: false);
        try
        {
            var net = NetSession.Host(pkg, _settings.PlayerName, _catalog.Stories,
                bind: LoopbackOnly ? IPAddress.Loopback : null);
            AttachNet(net);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Net.Sockets.SocketException)
        {
            ShowNotice("Nu pot găzdui", ex.Message, ShowMultiplayer);
            return;
        }
        // anunt in reteaua locala + deschiderea portului pe router (pe fundal, poate dura cateva secunde)
        _lanAddr = null;
        _vpnAddrs = LoopbackOnly ? new() : NetInfo.VirtualNetworks();
        _discovery = new LanDiscovery();
        var announced = _net!;
        _discovery.StartAnnouncing(() => (_settings.PlayerName, announced.Lobby.StoryTitle, announced.Lobby.Players.Count,
            announced.Package?.Story.MaxPlayers ?? 2, announced.Port), LoopbackOnly);
        // testele nu ating routerul (LASTSTOP_LOOPBACK=1 sau LASTSTOP_NOUPNP=1)
        if (LoopbackOnly || Environment.GetEnvironmentVariable("LASTSTOP_NOUPNP") == "1") { ShowLobby(); return; }
        _portMapPending = true;
        int port = _net!.Port;
        _ = Task.Run(async () =>
        {
            var pm = await PortMapping.TryOpenAsync(port, TimeSpan.FromSeconds(4));
            BeginInvoke(() =>
            {
                _portMapPending = false;
                if (_net?.Port == port) _portMap = pm; else pm.Dispose();
            });
        });
        ShowLobby();
    }

    private IPAddress? _lanAddr;

    private List<(string Label, IPAddress Address)> _vpnAddrs = new();

    /// <summary>
    /// Codurile gazdei: internet (daca routerul a deschis portul si are IP public), retele virtuale
    /// (Tailscale, Radmin...) si reteaua locala. Adresele se citesc o singura data, nu la fiecare cadru.
    /// </summary>
    private (string? Internet, string Lan, List<(string Label, string Code)> Vpn, string Best) HostCodes()
    {
        int port = _net?.Port ?? NetJson.DefaultPort;
        _lanAddr ??= LoopbackOnly ? IPAddress.Loopback : NetInfo.LocalIPv4();
        string lan = JoinCode.Encode(_lanAddr, port);
        string? internet = _portMap is { Mapped: true, IsPublic: true, ExternalIp: { } ip } ? JoinCode.Encode(ip, port) : null;
        var vpn = _vpnAddrs.Select(v => (v.Label, JoinCode.Encode(v.Address, port))).ToList();
        // codul principal: internet daca routerul a deschis portul, altfel reteaua locala;
        // codurile Tailscale / Radmin merg doar daca si prietenul e in aceeasi retea virtuala, deci stau separat
        return (internet, lan, vpn, internet ?? lan);
    }

    /// <summary>Ce scrie in lobby despre conectare: codul potrivit, celelalte coduri si ce sa faci daca nu merge.</summary>
    private LobbyConnectionInfo LobbyConnection(NetSession net)
    {
        var (internet, lan, vpn, best) = HostCodes();
        var extra = new List<string>();
        if (internet is not null) extra.Add($"aceeași rețea: {lan}");
        extra.AddRange(vpn.Select(v => $"{v.Label}: {v.Code}"));
        string note = (_portMapPending ? "Încerc să deschid portul pe router (UPnP)... "
            : internet is not null ? "Routerul a deschis portul: codul mare merge și prin internet. "
            : $"Codul mare merge doar în aceeași rețea. Prin internet: {_portMap?.Message ?? "UPnP indisponibil."} " +
              $"Deschide portul {net.Port} (TCP) în router sau folosiți Tailscale / Radmin VPN. ")
            + (vpn.Count > 0 ? "Codurile de rețea virtuală merg dacă și prietenul e în rețeaua ta virtuală. " : "")
            + "Dacă Windows a întrebat de rețea, trebuia apăsat Permite.";
        return new LobbyConnectionInfo(internet is not null ? "COD (PRIN INTERNET)" : "COD (ACEEAȘI REȚEA / WI-FI)", best, extra, note);
    }

    private void ShowJoin()
    {
        _discovery?.Dispose();
        _discovery = new LanDiscovery();
        _discovery.StartListening();
        _joinStatus = null;
        BuildJoinMenu(0);
    }

    private void BuildJoinMenu(int selected)
    {
        var m = new MenuScreen("Intră într-o tură") { Subtitle = "lipește codul primit de la gazdă (Ctrl+V)", Back = LeaveJoin };
        m.Items.Add(new MenuItem("Cod")
        {
            Value = () => (_joinText.Length == 0 ? "(scrie sau lipește)" : _joinText) + (m.Current?.Label == "Cod" && (int)(m.Time * 2) % 2 == 0 ? "_" : " "),
            TypeChar = c =>
            {
                if (c == '\b') { if (_joinText.Length > 0) _joinText = _joinText[..^1]; }
                else if (_joinText.Length < 24 && !char.IsWhiteSpace(c)) _joinText += char.ToUpperInvariant(c);
            },
            Activate = () => Connect(_joinText),
            Hint = "Codul are forma ABCDE-FGHJK. Merge și o adresă IP, ex. 192.168.1.20.",
        });
        m.Items.Add(new MenuItem("Conectează", () => Connect(_joinText)) { Enabled = !_joining });
        foreach (var game in _discovery?.Games ?? [])
        {
            var gm = game;
            m.Items.Add(new MenuItem($"{gm.Host}: {gm.Story} ({gm.Players}/{gm.Max})", () => Connect(gm.EndPoint.ToString()))
            {
                Hint = $"Joc găzduit în rețeaua ta locală ({gm.EndPoint}).",
                Enabled = !_joining && gm.Players < gm.Max,
            });
        }
        m.Items.Add(new MenuItem("Înapoi", LeaveJoin));
        m.Selected = Math.Min(selected, m.Items.Count - 1);
        var baseDraw = m;
        m.Draw = (g, view, s, hits) =>
        {
            DrawList(g, view, s, baseDraw, hits);
            string status = _joinStatus ?? ((_discovery?.Games.Count ?? 0) == 0 ? "Caut jocuri în rețeaua locală..." : "");
            if (status.Length > 0)
                Wrap(g, status, Body(s, 6f), _joinStatus is not null && !_joining ? Color.FromArgb(240, 120, 110) : Warm,
                    R(view, s, 24, 150, 260, 14));
        };
        OpenMenu(m);
    }

    private void LeaveJoin()
    {
        _discovery?.Dispose();
        _discovery = null;
        ShowMultiplayer();
    }

    private async void Connect(string text)
    {
        if (_joining) return;
        if (!JoinCode.TryParse(text, out var ep)) { _joinStatus = "Codul nu e valid. Verifică literele (ex. 4K7QM-2XD9A)."; return; }
        _joining = true;
        _joinStatus = $"Mă conectez la {ep}...";
        try
        {
            var net = await NetSession.JoinAsync(ep, _settings.PlayerName, _catalog.Stories);
            _discovery?.Dispose();
            _discovery = null;
            AttachNet(net);
            ShowLobby();
        }
        catch (Exception ex) when (ex is TimeoutException or System.Net.Sockets.SocketException or IOException)
        {
            _joinStatus = ex is TimeoutException ? ex.Message : $"Nu m-am putut conecta: {ex.Message}";
        }
        finally { _joining = false; }
    }

    private void AttachNet(NetSession net)
    {
        _net = net;
        _remotePos.Clear();
        int players = net.Lobby.Players.Count;
        net.LobbyChanged += () =>
        {
            int now = net.Lobby.Players.Count;
            if (now != players) PlaySfx(now > players ? "join" : "leave", 0.8f);
            players = now;
            if (_menu?.Title == "Lobby") ShowLobby();
        };
        net.Started += () => { _remotePos.Clear(); _flashes.Clear(); _pendingChanges.Clear(); _conclusionsShown = false; _routesKey = ""; };
        net.Moved += OnMoved;
        net.Shared += OnShared;
        net.Resynced += () => { if (_engine?.CurrentBlock is { } b) { _scene = null; EnterBlock(b, null); } };
        net.VotesChanged += () => { if (_mode == Mode.Playing) BuildDecisionButtons(); };
        net.ReturnedToLobby += ShowLobby;
        net.RemoteMoved += OnRemoteMoved;
        net.PlayerLeft += _ => { _scene?.Remotes.RemoveAll(r => !net.RemoteCharacters.Contains(r.Definition.Id)); _hudKey = default; };
        net.StoryReceived += pkg =>
        {
            _catalog.AddIfNew("(de la gazdă)", pkg);
        };
        net.Examined += OnExamined;
        net.Forked += OnForked;
        net.Ended += reason => BeginInvoke(() => OnNetEnded(reason));
        if (net.HasEngine) UseNetStory();
    }

    /// <summary>Povestea gazdei devine povestea curenta (motorul e al sesiunii de retea).</summary>
    private void UseNetStory()
    {
        if (_net is null || !_net.HasEngine || _engine == _net.Engine) return;
        UseStory(_net.Package!, _net.Engine, _net);
        if (_mode == Mode.Menu) BuildBackdrop();
    }

    private void PumpNet(float dt)
    {
        if (_net is not null)
        {
            _net.Pump(dt);
            if (_net is not null && _net.HasEngine && _engine != _net.Engine)
            {
                UseNetStory();
                if (_menu?.Title == "Lobby") ShowLobby();
            }
            RefreshRouteButtons();
            CheckConclusions();
        }
        // lista de jocuri din retea se reimprospateaza o data pe secunda
        if (_menu?.Title == "Intră într-o tură" && (_lanRefresh += dt) > 1f)
        {
            _lanRefresh = 0;
            var titles = _menu.Items.Select(i => i.Label).ToList();
            var now = (_discovery?.Games ?? []).Select(g => $"{g.Host}: {g.Story} ({g.Players}/{g.Max})").ToList();
            if (!titles.Skip(2).Take(titles.Count - 3).SequenceEqual(now)) BuildJoinMenu(_menu.Selected);
        }
    }

    private string _routesKey = "";

    /// <summary>
    /// Butoanele care depind de ceilalti (mergi dupa el, uita-te la el, concluzii) se refac cand se schimba
    /// ceva pe drumurile lor, nu doar cand se schimba blocul tau.
    /// </summary>
    private void RefreshRouteButtons()
    {
        if (_net is not { InGame: true } net || _mode is not (Mode.Playing or Mode.Ending) || _scene is null) return;
        string key = $"{_mode}|{net.Watching}|{string.Join(",", net.JoinTargets.Select(p => p.Id))}|{net.AllFinished}|{net.Follow?.Timeline}";
        if (key == _routesKey) return;
        bool first = _routesKey.Length == 0;
        _routesKey = key;
        if (first) return;
        if (_mode == Mode.Ending) BuildEndingButtons(); else BuildDecisionButtons();
    }

    private Dictionary<string, (float X, bool FacingLeft, string BlockId)> RemotePositions() =>
        _net is null ? new() : _net.RemoteCharacters.ToDictionary(id => id, id => _remotePos.GetValueOrDefault(id, (-1f, false, "")));

    private void OnRemoteMoved(RemoteMove m)
    {
        if (_net?.OnMyRoute(m.PlayerId) == false) return; // e pe alt drum: nu e in scena ta
        _remotePos[m.CharacterId] = (m.X, m.FacingLeft, m.BlockId);
        if (_scene is null || _scene.Block.Id != m.BlockId) return;
        var actor = _scene.Remotes.FirstOrDefault(r => r.Definition.Id == m.CharacterId);
        if (actor is null && _engine?.Story.FindCharacter(m.CharacterId) is { } def && _images?.GetSheet(def) is { } sheet && _scene.Player is not null)
        {
            actor = new Actors.RemoteActor(def, sheet, m.X);
            _scene.Remotes.Add(actor);
        }
        actor?.SetTarget(m.X, m.FacingLeft, m.Walking);
    }

    /// <summary>Mesajul aratat dupa despartire (in scena noua, dupa ce se aplica decizia).</summary>
    private string? _splitNotice;

    /// <summary>Drumurile s-au despartit: spunem cine a plecat pe alt drum.</summary>
    private void OnForked(IReadOnlyList<int> moved)
    {
        if (_net is null || _engine is null) return;
        var net = _net;
        var others = net.Lobby.Players.Where(p => p.Id != net.LocalId && !net.OnMyRoute(p.Id))
            .Select(p => $"{p.Name} ({_engine.Story.FindCharacter(net.CharacterOf(p.Id))?.Name.Split(' ')[0] ?? "?"})").ToList();
        _splitNotice = others.Count == 0 ? null : $"V-ați despărțit: {string.Join(", ", others)} merge pe alt drum.";
        _hudKey = default;
        // cine a plecat nu mai sta in scena ta (cand ramai in acelasi bloc, scena nu se reface)
        _scene?.Remotes.RemoveAll(r => !net.RemoteCharacters.Contains(r.Definition.Id));
    }

    private string HudName(CharacterDefinition c) =>
        _net?.NameOfCharacter(c.Id) ?? (c.Id == _net?.LocalPlayer ? _settings.PlayerName : null) ?? c.Name.Split(' ')[0];

    private void OnNetEnded(string reason)
    {
        if (_net is null) return;
        LeaveNet(null, showMenu: false);
        ShowNotice("Multiplayer", reason, ShowMainMenu);
    }

    /// <summary>Iesi din sesiunea de retea si revino la single player.</summary>
    private void LeaveNet(string? reason, bool showMenu = true)
    {
        var net = _net;
        _net = null;
        net?.Leave();
        _discovery?.Dispose();
        _discovery = null;
        var pm = _portMap;
        _portMap = null;
        if (pm is not null) _ = Task.Run(pm.Dispose); // inchide portul pe router, pe fundal
        _remotePos.Clear();
        _paused = false;
        if (net is null) return;
        // inapoi la o poveste locala, cu motor local
        _engine = null;
        var pkg = _catalog.IndexOf(net.Package) >= 0 ? net.Package : _catalog.Count > 0 ? _catalog[0] : null;
        _package = null;
        if (pkg is not null) Select(pkg);
        if (showMenu) ShowMainMenu();
    }

    private void ShowLobby()
    {
        if (_net is null) return;
        var net = _net;
        var story = net.HasEngine ? net.Package!.Story : null;
        var me = net.Lobby.Players.FirstOrDefault(p => p.Id == net.LocalId);
        int selected = _menu?.Title == "Lobby" ? _menu.Selected : -1;
        var m = new MenuScreen("Lobby") { Subtitle = story?.Title ?? "mă conectez...", Back = () => LeaveNet(null) };

        if (story is not null && !net.Lobby.InGame)
        {
            foreach (var p in story.Playable)
            {
                var id = p.CharacterId;
                var owner = net.Lobby.Players.FirstOrDefault(x => x.CharacterId == id);
                m.Items.Add(new MenuItem(story.FindCharacter(id)?.Name ?? id, () => net.Pick(id))
                {
                    Enabled = owner is null || owner.Id == net.LocalId,
                    Hint = id == story.PlayerCharacterId ? "Protagonistul: spune povestea (textul e la persoana întâi, al lui)."
                        : "Însoțitor: are replici, decizii și paragrafe doar ale lui.",
                });
            }
            m.Items.Add(new MenuItem("Decizii")
            {
                Value = () => net.Lobby.AllowSplit ? "Fiecare alege" : "Vot comun",
                Adjust = _ => net.SetAllowSplit(!net.Lobby.AllowSplit),
                Enabled = net.IsHost,
                Hint = (net.Lobby.AllowSplit
                    ? "Cine alege pleacă imediat; ceilalți merg după el (F) sau aleg altceva. Indiciile se pun la comun."
                    : "Votați și așteptați; câștigă majoritatea și rămâneți mereu împreună.") + (net.IsHost ? "" : " Alege gazda."),
            });
            if (net.IsHost)
            {
                string? blocker = net.StartBlocker();
                m.Items.Add(new MenuItem("Pornește tura", net.StartGame) { Enabled = blocker is null, Hint = blocker ?? "Toată lumea e gata." });
                m.Items.Add(new MenuItem("Copiază codul", () =>
                {
                    Clipboard.SetText(HostCodes().Best);
                })
                { Hint = "Trimite-l prietenului tău (Discord, WhatsApp...)." });
            }
            else
            {
                bool ready = me?.Ready == true;
                m.Items.Add(new MenuItem(ready ? "Nu mai sunt gata" : "Sunt gata", () => net.SetReady(!ready))
                {
                    Enabled = me?.CharacterId is not null,
                    Hint = ready ? "Așteptăm gazda să pornească tura." : "Alege un personaj, apoi spune că ești gata.",
                });
            }
        }
        m.Items.Add(new MenuItem("Ieși", () => LeaveNet(null)));
        if (selected >= 0) m.Selected = Math.Min(selected, m.Items.Count - 1);
        else m.Selected = Math.Max(0, m.Items.FindIndex(i => i.Label is "Pornește tura" or "Sunt gata"));
        m.Draw = (g, view, s, hits) => LobbyView.Draw(g, view, s, hits, m, net, _images, net.IsHost ? LobbyConnection(net) : null);
        OpenMenu(m);
    }

}
