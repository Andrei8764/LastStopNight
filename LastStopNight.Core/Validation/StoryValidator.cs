using LastStopNight.Core.IO;
using LastStopNight.Core.Model;

namespace LastStopNight.Core.Validation;

public enum IssueSeverity { Error, Warning }

public sealed record ValidationIssue(IssueSeverity Severity, string Where, string Message)
{
    public override string ToString() => $"[{(Severity == IssueSeverity.Error ? "EROARE" : "Avertisment")}] {Where}: {Message}";
}

/// <summary>
/// Verifica coerenta povestii inainte de salvare sau rulare: chei de stare, destinatii, personaje,
/// imagini, fundaturi si blocuri la care nu se poate ajunge. Erorile fac povestea de nejucat;
/// avertismentele sunt lucruri probabil gresite, dar nu fatale.
/// </summary>
public static class StoryValidator
{
    public static List<ValidationIssue> Validate(StoryPackage pkg) => new Run(pkg).Execute();

    /// <summary>O validare: povestea, indexurile ei (chei, ID-uri) si problemele gasite.</summary>
    private sealed class Run(StoryPackage pkg)
    {
        private readonly StoryDefinition _story = pkg.Story;
        private readonly List<ValidationIssue> _issues = new();
        private readonly HashSet<string> _keys = pkg.Story.Properties.Select(p => p.Key).ToHashSet();
        private readonly HashSet<string> _playable = pkg.Story.Playable.Select(p => p.CharacterId).ToHashSet();
        private readonly HashSet<string> _blockIds = new();

        public List<ValidationIssue> Execute()
        {
            CheckProperties();
            CheckBlockIds();
            CheckStoryHeader();
            CheckRedirects();
            CheckLocationSounds();
            foreach (var block in _story.Blocks) CheckBlock(block);
            CheckImages();
            CheckReachability();
            return _issues;
        }

        private void Error(string where, string message) => _issues.Add(new(IssueSeverity.Error, where, message));
        private void Warning(string where, string message) => _issues.Add(new(IssueSeverity.Warning, where, message));

        private void CheckLocationSounds()
        {
            foreach (var l in _story.Locations)
                foreach (var s in l.Sounds.Where(s => !LocationDefinition.KnownSounds.Contains(s)))
                    Warning($"Locația {l.Id}", $"Sunetul „{s}” nu există (se pot folosi: {string.Join(", ", LocationDefinition.KnownSounds)}).");
        }

        private void CheckProperties()
        {
            foreach (var g in _story.Properties.GroupBy(p => p.Key).Where(g => g.Count() > 1))
                Error("Proprietăți", $"Cheia „{g.Key}” este definită de {g.Count()} ori.");
            foreach (var p in _story.Properties)
            {
                if (string.IsNullOrWhiteSpace(p.Key)) Error("Proprietăți", "O proprietate nu are cheie.");
                if (p.Min > p.Max) Error($"Proprietatea {p.Key}", "Min este mai mare decât Max.");
                if (p.Initial < p.Min || p.Initial > p.Max) Warning($"Proprietatea {p.Key}", "Valoarea inițială este în afara [min, max].");
            }
        }

        private void CheckBlockIds()
        {
            foreach (var b in _story.Blocks)
            {
                if (string.IsNullOrWhiteSpace(b.Id)) Error("Blocuri", "Un bloc nu are ID.");
                else if (!_blockIds.Add(b.Id)) Error($"Blocul {b.Id}", "ID duplicat.");
            }
        }

        private void CheckStoryHeader()
        {
            if (_story.FindBlock(_story.StartBlockId) is null) Error("Poveste", $"Blocul de start „{_story.StartBlockId}” nu există.");
            if (_story.FindCharacter(_story.PlayerCharacterId) is null)
                Warning("Poveste", $"Personajul jucătorului „{_story.PlayerCharacterId}” nu există.");
            foreach (var p in _story.Playable.Where(p => _story.FindCharacter(p.CharacterId) is null))
                Error("Personaje jucabile", $"Personajul „{p.CharacterId}” nu există în lista de personaje.");
            if (_story.Playable.Count > 0 && !_playable.Contains(_story.PlayerCharacterId))
                Warning("Personaje jucabile", "Protagonistul nu e în lista de personaje jucabile.");
            if (_story.MaxPlayers < 1) Error("Poveste", "MaxPlayers trebuie să fie cel puțin 1.");
        }

        private void CheckRedirects()
        {
            foreach (var r in _story.Redirects)
            {
                string where = $"Redirecționarea → {r.Target}";
                if (r.Condition is null) Error(where, "Nu are condiție.");
                CheckCondition(where, r.Condition);
                if (!_blockIds.Contains(r.Target)) Error(where, "Blocul destinație nu există.");
            }
        }

        private void CheckBlock(StoryBlock b)
        {
            string where = $"Blocul {b.Id}";
            if (b.Location is not null && _story.FindLocation(b.Location) is null) Error(where, $"Locația „{b.Location}” nu există.");
            if (_story.ResolveBackground(b) is null) Warning(where, "Nu are fundal (nici locație, nici imagine proprie).");
            foreach (var c in b.Characters.Where(c => _story.FindCharacter(c.Id) is null))
                Error(where, $"Personajul „{c.Id}” nu există.");
            if (b.IsEnding && b.Decisions.Count > 0) Warning(where, "Bloc final cu decizii, deciziile vor fi ignorate.");
            if (!b.IsEnding && b.Decisions.Count == 0) Error(where, "Bloc fără decizii care nu este final (fundătură).");
            if (!b.IsEnding && b.Decisions.Count > 0 && b.Decisions.All(d => d.Condition is not null))
                Warning(where, "Toate deciziile au condiții, verifică să rămână mereu măcar una disponibilă.");
            CheckCompanionKeys(where, b.CompanionText.Keys);

            foreach (var ex in (_story.FindLocation(b.Location)?.Examine ?? []).Concat(b.Examine))
                CheckExamine($"{where}, obiectul {ex.Label}", ex);
            foreach (var g in b.Examine.GroupBy(e => e.Id).Where(g => g.Count() > 1 && g.Key.Length > 0))
                Error(where, $"ID de obiect duplicat: {g.Key}.");

            for (int i = 0; i < b.Decisions.Count; i++)
                CheckDecision($"{where}, decizia {i + 1}", b.Decisions[i]);
        }

        private void CheckExamine(string where, ExamineDefinition ex)
        {
            CheckCompanionKeys(where, ex.TextBy.Keys);
            if (string.IsNullOrWhiteSpace(ex.Id)) Error(where, "Nu are ID.");
            CheckCondition(where, ex.Condition);
            foreach (var e in ex.Effects.Where(e => !_keys.Contains(e.Key)))
                Error(where, $"Efectul modifică proprietatea necunoscută {e.Key}.");
        }

        private void CheckDecision(string where, DecisionDefinition d)
        {
            if (string.IsNullOrWhiteSpace(d.Text)) Warning(where, "Nu are text.");
            if (!_blockIds.Contains(d.Target)) Error(where, $"Destinația „{d.Target}” nu există.");
            CheckCondition(where, d.Condition);
            if (d.RequiresCharacter is not null && !_playable.Contains(d.RequiresCharacter))
                Error(where, $"Cere personajul „{d.RequiresCharacter}”, care nu e jucabil.");
            foreach (var e in d.Effects.Where(e => !_keys.Contains(e.Key)))
                Error(where, $"Efectul modifică proprietatea necunoscută „{e.Key}”.");
        }

        private void CheckCondition(string where, ConditionDefinition? c)
        {
            if (c is null) return;
            foreach (var k in c.ReferencedKeys().Where(k => !_keys.Contains(k)))
                Error(where, $"Condiția folosește proprietatea necunoscută „{k}”.");
        }

        private void CheckCompanionKeys(string where, IEnumerable<string> ids)
        {
            foreach (var id in ids.Where(id => !_playable.Contains(id)))
                Warning(where, $"„{id}” nu e un personaj jucabil, textul lui nu va apărea niciodată.");
        }

        private void CheckImages()
        {
            foreach (var img in _story.ReferencedImages().Distinct().Where(i => !pkg.HasFile(i)))
                Error("Imagini", $"Lipsește fișierul „{img}” din arhivă.");
        }

        /// <summary>Blocurile la care nu se poate ajunge din start (sau dintr-o redirectionare), pe graful deciziilor.</summary>
        private void CheckReachability()
        {
            if (_story.FindBlock(_story.StartBlockId) is null) return;
            var seen = new HashSet<string>();
            var queue = new Queue<string>([_story.StartBlockId]);
            foreach (var r in _story.Redirects) queue.Enqueue(r.Target);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!seen.Add(id)) continue;
                foreach (var d in _story.FindBlock(id)?.Decisions ?? []) queue.Enqueue(d.Target);
            }
            foreach (var b in _story.Blocks.Where(b => !seen.Contains(b.Id)))
                Warning($"Blocul {b.Id}", "Nu poate fi atins din blocul de start.");
        }
    }
}
