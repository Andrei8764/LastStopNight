using LastStopNight.Core.Engine;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Core.Validation;

namespace StoryCheck;

/// <summary>
/// Verificarea completa a unei povesti: validare, explorarea tuturor starilor accesibile (pentru fiecare
/// echipa posibila), reaplicarea jurnalului, JSON round-trip si textele fara simboluri tipografice ciudate.
/// </summary>
public static class StoryExplorer
{
    /// <summary>Simboluri care nu au ce cauta in texte (au fost scoase intentionat la cererea jucatorilor).</summary>
    private static readonly string[] OddSymbols = ["—", "…", "◆"];
    /// <summary>Cat de des (o data la cate stari noi) se verifica reaplicarea jurnalului; toate ar dura prea mult.</summary>
    private const int ReplaySampleEvery = 97;

    public static void Run(Report report, string path)
    {
        var pkg = StoryArchive.Load(path);
        var story = pkg.Story;
        report.Section($"{story.Title} ({Path.GetFileName(path)}): {story.Blocks.Count} blocuri, {pkg.Files.Count} fișiere");
        var issues = StoryValidator.Validate(pkg);
        foreach (var i in issues) report.Info(i.ToString());
        report.Check(issues.All(i => i.Severity != IssueSeverity.Error), "validare fără erori");

        var result = new Exploration();
        var parties = new List<string[]> { Array.Empty<string>() };
        parties.AddRange(story.Companions.Select(c => new[] { c.CharacterId }));
        foreach (var party in parties)
        {
            int states = Explore(story, party, result);
            report.Info($"Echipa [{string.Join(", ", new[] { story.PlayerCharacterId }.Concat(party))}]: {states} stări distincte");
        }
        foreach (var kv in result.Endings.OrderByDescending(k => k.Value)) report.Info($"  {kv.Key}: atins din {kv.Value} stări");

        var allEndings = story.Blocks.Where(b => b.IsEnding).Select(b => b.Id).ToList();
        var missingEndings = allEndings.Except(result.Endings.Keys).ToList();
        report.Check(missingEndings.Count == 0, $"toate cele {allEndings.Count} finaluri sunt accesibile" +
            (missingEndings.Count > 0 ? $" (lipsesc: {string.Join(", ", missingEndings)})" : ""));
        var unvisited = story.Blocks.Where(b => !result.VisitedBlocks.Contains(b.Id)).Select(b => b.Id).ToList();
        report.Check(unvisited.Count == 0, "toate blocurile sunt atinse" + (unvisited.Count > 0 ? $" (neatinse: {string.Join(", ", unvisited)})" : ""));
        foreach (var block in result.DeadEnds) report.Info($"fundătură în {block}");
        report.Check(result.DeadEnds.Count == 0, "nicio fundătură");
        report.Check(result.ReplayFailures == 0, $"reaplicarea jurnalului dă aceeași stare ({result.ReplayChecks} verificări)");
        CheckSoloRoutes(report, story);

        var json = StoryArchive.Serialize(story);
        report.Check(StoryArchive.Serialize(StoryArchive.Deserialize(json)) == json, "JSON round-trip identic");
        report.Check(!AllTexts(story).Any(t => OddSymbols.Any(t.Contains)), "fără simboluri ciudate în texte");
    }

    /// <summary>
    /// Multiplayer cu drumuri separate: un jucator poate ramane singur pe drumul lui (si insotitorul fara protagonist).
    /// Cel mai greu caz e singur de la inceput: nicio stare accesibila nu trebuie sa ramana fara decizie.
    /// </summary>
    private static void CheckSoloRoutes(Report report, StoryDefinition story)
    {
        if (story.MaxPlayers < 2 || !story.Companions.Any()) return;
        foreach (var solo in story.Playable.Select(p => p.CharacterId))
        {
            var result = new Exploration();
            var companions = story.Companions.Select(c => c.CharacterId).Where(c => c == solo).ToArray();
            int states = Explore(story, companions, result, solo);
            report.Check(result.DeadEnds.Count == 0 && result.Endings.Count > 0,
                $"drum separat, {story.FindCharacter(solo)?.Name ?? solo} singur: {states} stări, {result.Endings.Count} finaluri, nicio fundătură" +
                (result.DeadEnds.Count > 0 ? $" (fundături: {string.Join(", ", result.DeadEnds.Distinct())})" : ""));
        }
    }

    private sealed class Exploration
    {
        public Dictionary<string, int> Endings { get; } = new();
        public HashSet<string> VisitedBlocks { get; } = new();
        public List<string> DeadEnds { get; } = new();
        public int ReplayChecks { get; set; }
        public int ReplayFailures { get; set; }
    }

    /// <summary>
    /// Cautare in latime peste stari (starile identice, dupa amprenta, se comaseaza). O actiune e indexul unei
    /// decizii (&gt;= 0) sau -(k+1) = examinarea obiectului k (doar cele cu efecte, inca neexaminate).
    /// </summary>
    /// <returns>Numarul de stari distincte.</returns>
    /// <param name="solo">Daca e dat, echipa se reduce imediat la acest personaj (drum separat in multiplayer).</param>
    private static int Explore(StoryDefinition story, string[] party, Exploration result, string? solo = null)
    {
        var seen = new HashSet<string>();
        var queue = new Queue<List<int>>([[]]);
        while (queue.Count > 0)
        {
            var seq = queue.Dequeue();
            var e = Replay(story, party, seq, solo);
            if (!seen.Add(e.StateHash())) continue;
            if (seen.Count % ReplaySampleEvery == 1) CheckLogReplay(story, party, e, result);
            result.VisitedBlocks.Add(e.CurrentBlock!.Id);
            if (e.IsFinished)
            {
                result.Endings[e.CurrentBlock.Id] = result.Endings.GetValueOrDefault(e.CurrentBlock.Id) + 1;
                continue;
            }
            var options = e.GetDecisions().Where(o => o.IsAvailable).ToList();
            if (options.Count == 0) result.DeadEnds.Add(e.CurrentBlock.Id);
            foreach (var o in options) queue.Enqueue([.. seq, o.Index]);
            var examines = e.GetExamines();
            for (int k = 0; k < examines.Count; k++)
                if (examines[k].Effects.Count > 0 && !e.State.WasExamined(examines[k].Id)) queue.Enqueue([.. seq, -(k + 1)]);
        }
        return seen.Count;
    }

    private static GameEngine Replay(StoryDefinition story, string[] party, List<int> seq, string? solo = null)
    {
        var e = new GameEngine(story);
        e.Start(party);
        if (solo is not null) e.Apply(new PartyAction(e.CurrentBlock!.Id, solo, [solo]));
        foreach (var a in seq)
        {
            if (a >= 0) e.Choose(a);
            else e.Examine(e.GetExamines()[-a - 1]);
        }
        return e;
    }

    /// <summary>Reaplicarea jurnalului pe un motor nou trebuie sa dea exact aceeasi stare (salvare, retea).</summary>
    private static void CheckLogReplay(StoryDefinition story, string[] party, GameEngine e, Exploration result)
    {
        result.ReplayChecks++;
        var copy = new GameEngine(story);
        copy.Replay(party, e.Log);
        if (copy.StateHash() != e.StateHash()) result.ReplayFailures++;
    }

    private static IEnumerable<string> AllTexts(StoryDefinition story) =>
        story.Blocks.SelectMany(b => new[] { b.Text, b.Title }
                .Concat(b.Decisions.Select(d => d.Text))
                .Concat(b.Examine.Select(x => x.Text))
                .Concat(b.CompanionText.Values)
                .Concat(b.Examine.SelectMany(x => x.TextBy.Values)))
            .Concat(story.Locations.SelectMany(l => l.Examine.SelectMany(x => x.TextBy.Values.Append(x.Text))))
            .Concat(story.Playable.SelectMany(p => new[] { p.Role, p.Blurb }));
}
