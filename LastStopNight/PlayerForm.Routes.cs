using LastStopNight.Core.Engine;
using LastStopNight.Effects;
using LastStopNight.Net;
using LastStopNight.Ui;
using LastStopNight.Ui.Views;
using MenuItem = LastStopNight.Ui.MenuItem;

namespace LastStopNight;

/// <summary>
/// Multiplayer, „fiecare alege liber”: mersul dupa altcineva (tasta F sau butonul), trecerea pe drumul altuia,
/// privitul dupa ce ai terminat si ecranul de concluzii de la sfarsit.
/// </summary>
public sealed partial class PlayerForm
{
    /// <summary>Concluziile s-au aratat deja in tura asta (nu le redeschidem singuri dupa ce le inchizi).</summary>
    private bool _conclusionsShown;

    private string PlayerName(int id) => _net?.Lobby.Players.FirstOrDefault(p => p.Id == id)?.Name ?? "?";

    private string NamesOf(IEnumerable<int> ids) => string.Join(", ", ids.Select(PlayerName));

    /// <summary>Cineva a trecut pe alt drum. Daca esti tu, scena se reface pe drumul nou.</summary>
    private void OnMoved(int playerId)
    {
        if (_net is null || _engine?.CurrentBlock is not { } block) return;
        _hudKey = default;
        if (playerId == _net.LocalId)
        {
            _pendingChanges.Clear();
            _scene = null;
            _splitNotice = _net.Watching ? $"Te uiți la {NamesOf(_net.RoutePlayers.Select(p => p.Id))}." : $"Ai venit după {NamesOf(_net.RoutePlayers.Where(p => p.Id != _net.LocalId).Select(p => p.Id))}.";
            EnterBlock(block, null);
            return;
        }
        _scene?.Remotes.RemoveAll(r => !_net.RemoteCharacters.Contains(r.Definition.Id));
        if (_net.OnMyRoute(playerId) && !_net.IsWatching(playerId))
        {
            _scene?.Effects.Add(new FloatingTextEffect($"{PlayerName(playerId)} a venit după tine.", Color.FromArgb(255, 200, 120), new PointF(160, 34), 0));
            PlaySfx("join", 0.7f);
        }
        if (_mode == Mode.Playing) BuildDecisionButtons();
        else if (_mode == Mode.Ending) BuildEndingButtons();
    }

    /// <summary>Alt drum a gasit un indiciu: apare si la tine, cu numele celui care l-a gasit.</summary>
    private void OnShared(ShareAction sh)
    {
        if (_engine is null || _scene is null) return;
        var def = _engine.Story.FindProperty(sh.Key);
        foreach (var c in _pendingChanges.Where(c => c.Key == sh.Key && c.Delta > 0))
            _scene.Effects.Add(new FloatingTextEffect($"+{c.Delta} {def?.HudLabel ?? sh.Key} (de la {_net?.NameOfCharacter(sh.Player) ?? sh.Player})",
                Color.FromArgb(140, 210, 255), new PointF(160, 40), 0.2f));
        foreach (var c in _pendingChanges.Where(c => c.Key == sh.Key)) _flashes[c.Key] = 1f;
        _pendingChanges.RemoveAll(c => c.Key == sh.Key);
        PlaySfx("examine", 0.6f);
        if (_mode == Mode.Playing) BuildDecisionButtons(); // o decizie poate cere mai multe indicii
    }

    /// <summary>Butoanele de sub text cat timp te uiti la altcineva.</summary>
    private void BuildWatchButtons(NetSession net)
    {
        var list = new List<(string, Action, bool, string?, Bitmap?, int?)>
        {
            ($"Te uiți la {NamesOf(net.RoutePlayers.Select(p => p.Id))}. Alege el.", () => { }, false, null, null, null),
        };
        foreach (var p in net.JoinTargets)
        {
            int id = p.Id;
            list.Add(($"Uită-te la {p.Name}", () => net.RequestJoin(id), true, null, null, null));
        }
        if (net.IsHost) list.Add(("Înapoi în lobby (pentru toți)", net.ReturnToLobby, true, null, null, null));
        SetButtons(list.ToArray());
    }

    /// <summary>La final, in multiplayer: te poti uita la cine inca joaca, vezi concluziile sau revii in lobby.</summary>
    private void BuildEndingButtons()
    {
        if (_net is not { } net) return;
        var list = new List<(string, Action, bool, string?, Bitmap?, int?)>();
        if (net.AllFinished) list.Add(("Concluziile turei", ShowConclusions, true, "Finalul fiecăruia și ce ați aflat împreună.", null, 1));
        int n = list.Count;
        foreach (var p in net.JoinTargets)
        {
            int id = p.Id;
            list.Add(($"Uită-te la {p.Name} (încă joacă)", () => net.RequestJoin(id), true, "Vezi ce face, fără să alegi tu.", null, ++n));
        }
        if (net.IsHost)
            list.Add((net.AllFinished ? "Înapoi în lobby (pentru toți)" : "Înapoi în lobby (ceilalți încă joacă)", net.ReturnToLobby, true, null, null, null));
        else if (net.AllFinished) list.Add(("Așteptăm gazda să aleagă...", () => { }, false, null, null, null));
        list.Add(("Părăsește jocul", () => LeaveNet(null), true, null, null, null));
        SetButtons(list.ToArray());
    }

    /// <summary>Verificat la fiecare cadru: cand ajunge si ultimul la final, toti vad concluziile.</summary>
    private void CheckConclusions()
    {
        if (_net is not { AllFinished: true } || _conclusionsShown || _mode != Mode.Ending || _paused || _modeTime < 3f) return;
        _conclusionsShown = true;
        BuildEndingButtons();
        ShowConclusions();
    }

    private void ShowConclusions()
    {
        if (_net is not { } net || _engine is null) return;
        var story = _engine.Story;
        var endings = net.Lobby.Players
            .Select(p => net.EndingOf(p.Id) is { } e ? new PlayerEnding(p.Name, story.FindCharacter(net.CharacterOf(p.Id)), e, p.Id == net.LocalId) : null)
            .OfType<PlayerEnding>().ToList();
        _paused = true;
        var m = new MenuScreen("Concluzii") { Subtitle = story.Title, Horizontal = true, Back = ResumeGame };
        if (net.IsHost) m.Items.Add(new MenuItem("Înapoi în lobby", net.ReturnToLobby) { Hint = "Pentru toți: alegeți altă tură." });
        m.Items.Add(new MenuItem("Harta turei", () => { ResumeGame(); _showMap = true; }) { Hint = "Pe unde a mers fiecare (H sau Esc o închide)." });
        m.Items.Add(new MenuItem("Închide", ResumeGame) { Hint = "Te întorci la finalul tău." });
        m.Draw = (g, view, s, hits) => ConclusionView.Draw(g, view, s, hits, m, story, endings, _images);
        OpenMenu(m, overGame: true);
        PlaySfx("paper", 0.6f);
    }

    /// <summary>Optiunile din pauza pentru drumurile celorlalti.</summary>
    private void AddRouteItems(MenuScreen m, NetSession net)
    {
        foreach (var p in net.JoinTargets)
        {
            int id = p.Id;
            m.Items.Add(net.Watching
                ? new MenuItem($"Uită-te la {p.Name}", () => { ResumeGame(); net.RequestJoin(id); }) { Hint = "Vezi ce face, fără să alegi tu." }
                : new MenuItem($"Treci pe drumul lui {p.Name}", () => { ResumeGame(); net.RequestJoin(id); })
                {
                    Hint = "Mergi după el: ajungi unde e acum, cu ce a găsit el. Drumul tău rămâne pe hartă.",
                });
        }
    }
}
