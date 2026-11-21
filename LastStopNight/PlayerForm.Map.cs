using LastStopNight.Core.Model;
using LastStopNight.Ui.Views;

namespace LastStopNight;

/// <summary>
/// Harta turei (tasta H, sau „Harta” din pauza): drumurile parcurse, unde e fiecare jucator si cum ii merge.
/// In single player e un singur drum (tu si insotitorul); in multiplayer, cate unul pentru fiecare grup despartit.
/// </summary>
public sealed partial class PlayerForm
{
    private bool _showMap;

    private void ToggleMap()
    {
        _showMap = !_showMap;
        PlaySfx("paper", 0.6f);
    }

    private List<MapRoute> MapRoutes()
    {
        if (_engine is null) return [];
        var story = _engine.Story;
        if (_net is not { InGame: true } net)
        {
            var members = new List<(string, CharacterDefinition?)> { (_settings.PlayerName, story.FindCharacter(story.PlayerCharacterId)) };
            foreach (var c in _engine.Companions)
                if (story.FindCharacter(c) is { } def) members.Add((def.Name.Split(' ')[0], def));
            return [new MapRoute(members, _engine, IsMine: true)];
        }
        return net.Routes.Select(r => new MapRoute(
                r.Players.Select(id => (net.Lobby.Players.FirstOrDefault(p => p.Id == id)?.Name ?? "a ieșit", story.FindCharacter(net.CharacterOf(id)))).ToList(),
                r.Engine, r.Players.Contains(net.LocalId)))
            .ToList();
    }
}
