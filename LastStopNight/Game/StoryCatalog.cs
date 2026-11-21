using System.Diagnostics;
using LastStopNight.Core.IO;

namespace LastStopNight.Game;

/// <summary>
/// Povestile pe care le poate juca jucatorul, in ordinea din meniu (<c>order</c> din story.json).
/// Surse: arhivele din Assets/stories de langa exe, cele incorporate in exe (versiunea publicata,
/// un singur fisier), cele deschise din fisier si cele primite de la gazda in multiplayer.
/// </summary>
public sealed class StoryCatalog
{
    private const string EmbeddedPrefix = "stories/";
    private readonly List<(string Source, StoryPackage Package)> _entries = new();

    public IReadOnlyList<StoryPackage> Stories => _entries.Select(e => e.Package).ToList();
    public int Count => _entries.Count;
    public StoryPackage this[int index] => _entries[index].Package;

    /// <summary>
    /// Incarca povestile de pe disc (sau doar fisierul explicit primit ca argument), apoi pe cele
    /// incorporate in exe care nu exista deja pe disc. O poveste stricata e sarita, nu opreste jocul.
    /// </summary>
    public static StoryCatalog Load(string? explicitPath)
    {
        var catalog = new StoryCatalog();
        var paths = new List<string>();
        if (explicitPath is not null) paths.Add(explicitPath);
        else
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Assets", "stories");
            if (Directory.Exists(dir)) paths.AddRange(Directory.GetFiles(dir, "*.zip"));
        }
        foreach (var p in paths)
        {
            try { catalog._entries.Add((p, StoryArchive.Load(p))); }
            catch (Exception ex) when (IsBadStory(ex)) { Debug.WriteLine($"{p}: {ex.Message}"); }
        }
        if (explicitPath is null) catalog.AddEmbedded();
        catalog.Sort();
        return catalog;
    }

    private void AddEmbedded()
    {
        var asm = typeof(StoryCatalog).Assembly;
        foreach (var name in asm.GetManifestResourceNames().Where(n => n.StartsWith(EmbeddedPrefix) && n.EndsWith(".zip")))
        {
            try
            {
                using var stream = asm.GetManifestResourceStream(name)!;
                AddIfNew("(inclusă) " + name, StoryArchive.LoadZip(stream));
            }
            catch (Exception ex) when (IsBadStory(ex)) { Debug.WriteLine($"{name}: {ex.Message}"); }
        }
    }

    private static bool IsBadStory(Exception ex) =>
        ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException or NotSupportedException;

    /// <summary>Adauga povestea daca nu exista deja una cu acelasi titlu.</summary>
    public void AddIfNew(string source, StoryPackage package)
    {
        if (FindByTitle(package.Story.Title) is null) _entries.Add((source, package));
    }

    /// <summary>Adauga (sau inlocuieste) o poveste deschisa dintr-un fisier.</summary>
    public void AddFromFile(string path, StoryPackage package)
    {
        _entries.RemoveAll(e => string.Equals(e.Source, path, StringComparison.OrdinalIgnoreCase));
        _entries.Add((path, package));
    }

    public StoryPackage? FindByTitle(string title) => _entries.FirstOrDefault(e => e.Package.Story.Title == title).Package;

    public int IndexOf(StoryPackage? package) => _entries.FindIndex(e => e.Package == package);

    private void Sort() => _entries.Sort((a, b) => a.Package.Story.Order.CompareTo(b.Package.Story.Order));
}
