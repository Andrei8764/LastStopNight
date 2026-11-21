using System.Text.Json;

namespace LastStopNight.Game;

/// <summary>Fisierele jocului din %AppData%\LastStopNight (setari, salvare, finaluri).</summary>
public static class AppData
{
    /// <summary>Variabila LASTSTOP_DATA muta datele in alt dosar (teste automate fara sa atinga salvarile reale).</summary>
    public static string Dir { get; set; } = Environment.GetEnvironmentVariable("LASTSTOP_DATA") is { Length: > 0 } d ? d
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LastStopNight");

    public static string PathOf(string name) => Path.Combine(Dir, name);

    public static T? Load<T>(string name) where T : class
    {
        try
        {
            var p = PathOf(name);
            return File.Exists(p) ? JsonSerializer.Deserialize<T>(File.ReadAllText(p)) : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or NotSupportedException)
        {
            return null; // un fisier stricat nu trebuie sa blocheze jocul
        }
    }

    public static void Save<T>(string name, T value)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var tmp = PathOf(name + ".tmp");
            File.WriteAllText(tmp, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, PathOf(name), overwrite: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static void Delete(string name)
    {
        try { File.Delete(PathOf(name)); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
