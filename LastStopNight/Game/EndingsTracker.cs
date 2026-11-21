namespace LastStopNight.Game;

/// <summary>Tine minte ce finaluri a descoperit jucatorul (fisier text in %AppData%).</summary>
public sealed class EndingsTracker
{
    private readonly string _path = AppData.PathOf("endings.txt");
    private readonly HashSet<string> _found = new();

    public EndingsTracker()
    {
        try { if (File.Exists(_path)) _found.UnionWith(File.ReadAllLines(_path)); } catch (IOException) { }
    }

    public int Count(string storyTitle) => _found.Count(f => f.StartsWith(storyTitle + "|"));

    public bool Has(string storyTitle, string blockId) => _found.Contains($"{storyTitle}|{blockId}");

    public void Add(string storyTitle, string blockId)
    {
        if (!_found.Add($"{storyTitle}|{blockId}")) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllLines(_path, _found);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
