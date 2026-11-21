using LastStopNight.Core.Model;

namespace LastStopNight.Core.IO;

/// <summary>Povestea incarcata in memorie, impreuna cu fisierele (imaginile) din arhiva.</summary>
public sealed class StoryPackage
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

    public StoryPackage(StoryDefinition story) { Story = story; }

    public StoryDefinition Story { get; set; }
    public string? SourcePath { get; set; }
    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/', '.');

    public byte[]? GetFile(string? path) =>
        path is not null && _files.TryGetValue(Normalize(path), out var data) ? data : null;

    public void SetFile(string path, byte[] data) => _files[Normalize(path)] = data;
    public bool RemoveFile(string path) => _files.Remove(Normalize(path));
    public bool HasFile(string? path) => path is not null && _files.ContainsKey(Normalize(path));

    public IEnumerable<string> ImagePaths =>
        _files.Keys.Where(k => k.StartsWith("images/", StringComparison.OrdinalIgnoreCase)).OrderBy(k => k);
}
