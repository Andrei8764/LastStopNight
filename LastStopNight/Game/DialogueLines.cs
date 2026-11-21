using System.Text.RegularExpressions;

namespace LastStopNight.Game;

/// <summary>O replica din textul blocului: cine o spune, ce spune si unde e in text (pentru sincronizarea cu naratorul).</summary>
public sealed record DialogueLine(string CharacterId, string Text, int Offset);

/// <summary>
/// Gaseste replicile din textul unui bloc, ca sa apara si in bule deasupra personajelor (nu doar in textul de jos).
/// Textul e scris la persoana intai de protagonist, deci:
/// - „...”, zic / spun / intreb  -> protagonistul;
/// - un nume (Ioana, Vlad...) + zice / spune / intreaba...  -> personajul acela;
/// - ghilimele fara verb de vorbire (un bilet, radioul, un afis) nu sunt replici si raman doar in text.
/// </summary>
public static partial class DialogueLines
{
    [GeneratedRegex("[\"„“]([^\"„“”]{2,}?)[\"”“]")]
    private static partial Regex Quote();

    private static readonly string[] FirstPerson =
        ["zic", "spun", "întreb", "strig", "răspund", "șoptesc", "murmur", "mormăi", "continui", "adaug", "insist"];

    private static readonly string[] ThirdPerson =
        ["zice", "spune", "întreabă", "strigă", "răspunde", "șoptește", "murmură", "mormăie", "râde", "oftează",
         "continuă", "adaugă", "insistă", "zâmbește", "se strâmbă", "ridică din umeri", "zise", "spuse"];

    /// <param name="speakers">Personajele din scena: ID-ul si numele (se cauta dupa primul nume).</param>
    /// <param name="protagonist">Cine povesteste (persoana intai).</param>
    public static List<DialogueLine> Find(string text, IReadOnlyList<(string Id, string Name)> speakers, string protagonist)
    {
        var lines = new List<DialogueLine>();
        int at = 0;
        foreach (var paragraph in text.Split("\n\n"))
        {
            int start = text.IndexOf(paragraph, at, StringComparison.Ordinal);
            at = start + paragraph.Length;
            var quotes = Quote().Matches(paragraph);
            if (quotes.Count == 0) continue;
            string outside = Quote().Replace(paragraph, " ").ToLowerInvariant();
            bool first = FirstPerson.Any(v => HasWord(outside, v));
            bool third = ThirdPerson.Any(v => HasWord(outside, v));
            string? named = speakers.Where(s => s.Id != protagonist)
                .Select(s => (s.Id, Index: NameIndex(Quote().Replace(paragraph, " "), s.Name)))
                .Where(x => x.Index >= 0).OrderBy(x => x.Index).Select(x => x.Id).FirstOrDefault();
            string? speaker = third && named is not null ? named : first ? protagonist : null;
            if (speaker is null) continue;
            foreach (Match q in quotes)
            {
                string line = q.Groups[1].Value.Trim().TrimEnd(',');
                if (line.Length >= 2) lines.Add(new DialogueLine(speaker, line, Math.Max(0, start) + q.Index));
            }
        }
        return lines;
    }

    private static bool HasWord(string text, string word) =>
        Regex.IsMatch(text, $@"(?<![\p{{L}}-]){Regex.Escape(word)}(?![\p{{L}}])");

    /// <summary>Unde apare primul nume al personajului (si formele lui: Ioanei, lui Vlad...), -1 daca deloc.</summary>
    private static int NameIndex(string text, string fullName)
    {
        string name = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        if (name.Length < 3) return -1;
        var m = Regex.Match(text, $@"(?<!\p{{L}}){Regex.Escape(name[..^1])}\p{{L}}*");
        return m.Success ? m.Index : -1;
    }
}
