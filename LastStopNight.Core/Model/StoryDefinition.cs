namespace LastStopNight.Core.Model;

/// <summary>Radacina povestii: metadate, proprietati de stare, locatii 2D, personaje si blocuri narative.</summary>
public sealed class StoryDefinition
{
    public string Title { get; set; } = "Poveste nouă";
    /// <summary>Ordinea in lista de povesti de pe ecranul de titlu.</summary>
    public int Order { get; set; }
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public int Version { get; set; } = 1;
    public string StartBlockId { get; set; } = "";
    public string PlayerCharacterId { get; set; } = "alex";
    /// <summary>
    /// Personajele care pot fi alese din meniu. Protagonistul (<see cref="PlayerCharacterId"/>) spune povestea;
    /// ceilalti sunt insotitori: il urmeaza, comenteaza ce vad si deblocheaza decizii proprii.
    /// </summary>
    public List<PlayableDefinition> Playable { get; set; } = new();
    /// <summary>Cati jucatori incap intr-o tura (protagonist + insotitori).</summary>
    public int MaxPlayers { get; set; } = 2;

    /// <summary>Insotitorii disponibili (toate personajele jucabile, mai putin protagonistul).</summary>
    public IEnumerable<PlayableDefinition> Companions => Playable.Where(p => p.CharacterId != PlayerCharacterId);

    public List<StatePropertyDefinition> Properties { get; set; } = new();
    public List<RedirectDefinition> Redirects { get; set; } = new();
    public List<LocationDefinition> Locations { get; set; } = new();
    public List<CharacterDefinition> Characters { get; set; } = new();
    public List<StoryBlock> Blocks { get; set; } = new();

    public StoryBlock? FindBlock(string? id) =>
        id is null ? null : Blocks.FirstOrDefault(b => b.Id == id);

    public LocationDefinition? FindLocation(string? id) =>
        id is null ? null : Locations.FirstOrDefault(l => l.Id == id);

    public PlayableDefinition? FindPlayable(string? id) => Playable.FirstOrDefault(p => p.CharacterId == id);

    public CharacterDefinition? FindCharacter(string? id) =>
        id is null ? null : Characters.FirstOrDefault(c => c.Id == id);

    public StatePropertyDefinition? FindProperty(string? key) =>
        key is null ? null : Properties.FirstOrDefault(p => p.Key == key);

    /// <summary>Un obiect de examinat dintr-un bloc (al blocului sau al locatiei lui), indiferent de conditie.</summary>
    public ExamineDefinition? FindExamine(string blockId, string examineId) =>
        FindBlock(blockId) is { } block
            ? (FindLocation(block.Location)?.Examine ?? []).Concat(block.Examine).FirstOrDefault(e => e.Id == examineId)
            : null;

    /// <summary>Fundalul efectiv al unui bloc: imaginea proprie sau cea a locatiei.</summary>
    public string? ResolveBackground(StoryBlock block) =>
        !string.IsNullOrWhiteSpace(block.Background) ? block.Background : FindLocation(block.Location)?.Image;

    /// <summary>Toate caile de imagini referite de poveste (pentru validare si export).</summary>
    public IEnumerable<string> ReferencedImages()
    {
        foreach (var l in Locations)
        {
            if (!string.IsNullOrWhiteSpace(l.Image)) yield return l.Image;
            foreach (var d in l.Decor) if (!string.IsNullOrWhiteSpace(d.Image)) yield return d.Image;
        }
        foreach (var c in Characters) if (!string.IsNullOrWhiteSpace(c.Sprite)) yield return c.Sprite;
        foreach (var p in Properties) if (!string.IsNullOrWhiteSpace(p.Icon)) yield return p.Icon!;
        foreach (var b in Blocks)
        {
            if (!string.IsNullOrWhiteSpace(b.Background)) yield return b.Background!;
            if (!string.IsNullOrWhiteSpace(b.Closeup)) yield return b.Closeup!;
            foreach (var p in b.Props) if (!string.IsNullOrWhiteSpace(p.Image)) yield return p.Image;
        }
    }
}
