using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using LastStopNight.Core.Model;

namespace LastStopNight.Core.IO;

/// <summary>
/// Persistenta povestii: arhiva ZIP cu story.json + images/, sau story.json simplu
/// (imaginile se citesc atunci din directorul alaturat).
/// </summary>
public static class StoryArchive
{
    public const string StoryFileName = "story.json";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowOutOfOrderMetadataProperties = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string Serialize(StoryDefinition story) => JsonSerializer.Serialize(story, JsonOptions);

    public static StoryDefinition Deserialize(string json) =>
        JsonSerializer.Deserialize<StoryDefinition>(json, JsonOptions)
        ?? throw new InvalidDataException("story.json este gol.");

    public static StoryPackage Load(string path)
    {
        var pkg = Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? LoadFolder(path)
            : LoadZip(path);
        pkg.SourcePath = path;
        return pkg;
    }

    private static StoryPackage LoadZip(string zipPath)
    {
        using var fs = File.OpenRead(zipPath);
        return LoadZip(fs);
    }

    /// <summary>Incarca o poveste dintr-un flux ZIP (resursa incorporata in exe, transfer prin retea).</summary>
    public static StoryPackage LoadZip(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = zip.GetEntry(StoryFileName)
                    ?? zip.Entries.FirstOrDefault(e => e.FullName.EndsWith("/" + StoryFileName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidDataException($"Arhiva nu conține {StoryFileName}.");
        string prefix = entry.FullName[..^StoryFileName.Length];

        StoryPackage pkg;
        using (var reader = new StreamReader(entry.Open()))
            pkg = new StoryPackage(Deserialize(reader.ReadToEnd()));

        foreach (var e in zip.Entries)
        {
            if (e.FullName.EndsWith('/') || e == entry || !e.FullName.StartsWith(prefix)) continue;
            using var ms = new MemoryStream();
            using (var s = e.Open()) s.CopyTo(ms);
            pkg.SetFile(e.FullName[prefix.Length..], ms.ToArray());
        }
        return pkg;
    }

    private static StoryPackage LoadFolder(string jsonPath)
    {
        var pkg = new StoryPackage(Deserialize(File.ReadAllText(jsonPath)));
        string root = Path.GetDirectoryName(Path.GetFullPath(jsonPath))!;
        string images = Path.Combine(root, "images");
        if (Directory.Exists(images))
            foreach (var f in Directory.EnumerateFiles(images, "*", SearchOption.AllDirectories))
                pkg.SetFile(Path.GetRelativePath(root, f), File.ReadAllBytes(f));
        return pkg;
    }

    /// <summary>Salveaza dupa extensie: .json → folder (story.json + images/), altfel ZIP.</summary>
    public static void SaveAuto(StoryPackage pkg, string path)
    {
        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)) SaveFolder(pkg, path);
        else Save(pkg, path);
    }

    /// <summary>Salveaza ca story.json + fisierele din pachet in directorul alaturat.</summary>
    public static void SaveFolder(StoryPackage pkg, string jsonPath)
    {
        string root = Path.GetDirectoryName(Path.GetFullPath(jsonPath))!;
        File.WriteAllText(jsonPath, Serialize(pkg.Story));
        foreach (var (path, data) in pkg.Files)
        {
            string full = Path.GetFullPath(Path.Combine(root, path));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue; // fara iesiri din folder
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            if (!File.Exists(full) || !File.ReadAllBytes(full).AsSpan().SequenceEqual(data)) File.WriteAllBytes(full, data);
        }
        pkg.SourcePath = jsonPath;
    }

    /// <summary>Salveaza ca ZIP. Se scriu doar imaginile din pachet (inclusiv cele nefolosite).</summary>
    public static void Save(StoryPackage pkg, string zipPath)
    {
        string tmp = zipPath + ".tmp";
        using (var fs = File.Create(tmp)) WriteZip(pkg, fs);
        File.Move(tmp, zipPath, overwrite: true);
        pkg.SourcePath = zipPath;
    }

    public static void WriteZip(StoryPackage pkg, Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        var storyEntry = zip.CreateEntry(StoryFileName, CompressionLevel.Optimal);
        using (var w = new StreamWriter(storyEntry.Open())) w.Write(Serialize(pkg.Story));
        foreach (var (path, data) in pkg.Files.OrderBy(f => f.Key))
        {
            var e = zip.CreateEntry(path, CompressionLevel.Optimal);
            using var s = e.Open();
            s.Write(data);
        }
    }

    public static byte[] ToZipBytes(StoryPackage pkg)
    {
        using var ms = new MemoryStream();
        WriteZip(pkg, ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Amprenta povestii (SHA-256 pe story.json + imagini). Doi jucatori pot juca impreuna doar daca au
    /// exact aceeasi poveste; altfel gazda o trimite.
    /// </summary>
    public static string Fingerprint(StoryPackage pkg)
    {
        using var sha = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        sha.AppendData(System.Text.Encoding.UTF8.GetBytes(Serialize(pkg.Story)));
        foreach (var (path, data) in pkg.Files.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            sha.AppendData(System.Text.Encoding.UTF8.GetBytes(path));
            sha.AppendData(data);
        }
        return Convert.ToHexString(sha.GetHashAndReset())[..16];
    }
}
