using LastStopNight.Core.IO;
using LastStopNight.Core.Model;

namespace LastStopNight.Editor;

/// <summary>Starea partajata de paginile editorului: pachetul curent, imaginile decodate si operatiile comune.</summary>
public sealed class EditorContext : IDisposable
{
    private readonly Dictionary<string, Bitmap?> _images = new(StringComparer.OrdinalIgnoreCase);

    public EditorContext(StoryPackage package) { Package = package; }

    public StoryPackage Package { get; }
    public StoryDefinition Story => Package.Story;
    public bool FilesChanged { get; set; }

    public string[] BlockIds => Story.Blocks.Select(b => b.Id).ToArray();
    public string[] ImagePaths => Package.ImagePaths.ToArray();
    public string[] ImagesIn(string folder) =>
        Package.ImagePaths.Where(p => p.StartsWith(folder, StringComparison.OrdinalIgnoreCase)).ToArray();

    public Bitmap? GetImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (_images.TryGetValue(path, out var bmp)) return bmp;
        var bytes = Package.GetFile(path);
        if (bytes is not null)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                using var tmp = new Bitmap(ms);
                bmp = new Bitmap(tmp);
            }
            catch (ArgumentException) { bmp = null; }
        }
        _images[path] = bmp;
        return bmp;
    }

    public void SetFile(string path, byte[] data)
    {
        Package.SetFile(path, data);
        if (_images.Remove(path, out var old)) old?.Dispose();
        FilesChanged = true;
    }

    public void RemoveFile(string path)
    {
        Package.RemoveFile(path);
        if (_images.Remove(path, out var old)) old?.Dispose();
        FilesChanged = true;
    }

    /// <summary>Redenumeste un bloc si actualizeaza toate referintele catre el.</summary>
    public void RenameBlock(string oldId, string newId)
    {
        if (Story.StartBlockId == oldId) Story.StartBlockId = newId;
        foreach (var r in Story.Redirects.Where(r => r.Target == oldId)) r.Target = newId;
        foreach (var d in Story.Blocks.SelectMany(b => b.Decisions).Where(d => d.Target == oldId)) d.Target = newId;
    }

    public void Dispose()
    {
        foreach (var b in _images.Values) b?.Dispose();
        _images.Clear();
    }
}
