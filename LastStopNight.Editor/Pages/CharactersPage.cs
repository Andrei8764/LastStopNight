using LastStopNight.Core.Model;

namespace LastStopNight.Editor.Pages;

/// <summary>Personajele si spritesheet-urile lor, cu previzualizarea cadrelor.</summary>
public sealed class CharactersPage : EditorPage
{
    private readonly DataGridView _grid = Ui.Grid();
    private readonly PictureBox _preview = new() { Dock = DockStyle.Bottom, Height = 150, BackColor = Color.FromArgb(20, 18, 22) };

    public CharactersPage(EditorContext ctx) : base(ctx)
    {
        Ui.AddText(_grid, "id", "ID", 70);
        Ui.AddText(_grid, "name", "Nume", 120);
        var sprite = Ui.AddCombo(_grid, "sprite", "Spritesheet", ctx.ImagesIn("images/sprites/"), 140);
        Ui.AddText(_grid, "fw", "Lățime cadru", 45);
        Ui.AddText(_grid, "fh", "Înălțime cadru", 45);
        Ui.AddText(_grid, "idle", "Cadre idle", 50);
        Ui.AddText(_grid, "walk", "Cadre mers", 60);
        Ui.AddCheck(_grid, "refl", "Reflexie", 40);
        Ui.AddText(_grid, "desc", "Descriere", 140);
        Ui.EnsureItems(sprite, ctx.Story.Characters.Select(c => c.Sprite));
        foreach (var c in ctx.Story.Characters)
            _grid.Rows.Add(c.Id, c.Name, c.Sprite, c.FrameWidth, c.FrameHeight, string.Join(",", c.IdleFrames),
                string.Join(",", c.WalkFrames), c.HasReflection, c.Description);
        _grid.SelectionChanged += (_, _) => _preview.Invalidate();
        _preview.Paint += (_, e) =>
        {
            var row = _grid.CurrentRow;
            if (row is null || row.IsNewRow || Ctx.GetImage(Ui.Cell(row, "sprite")) is not { } bmp) return;
            Ui.PixelArt(e.Graphics);
            float s = Math.Min(3f, (_preview.Height - 10) / (float)bmp.Height);
            e.Graphics.DrawImage(bmp, 10, 5, bmp.Width * s, bmp.Height * s);
        };
        Load += (_, _) => { if (_grid.Rows.Count > 1) _grid.CurrentCell = _grid.Rows[0].Cells[1]; _preview.Invalidate(); };
        Controls.Add(_grid);
        Controls.Add(_preview);
        Controls.Add(Title("PERSONAJE",
            "Spritesheet orizontal cu cadre de mărime fixă. „Reflexie” debifat = personajul nu apare în geamuri."));
    }

    public override string? Commit()
    {
        try
        {
            var old = Ctx.Story.Characters.Select(c => c.Id).ToList();
            var list = Ui.Rows(_grid).Select(r => new CharacterDefinition
            {
                Id = Ui.Cell(r, "id"),
                Name = Ui.Cell(r, "name"),
                Sprite = Ui.Cell(r, "sprite"),
                FrameWidth = Ui.Int(r, "fw", "Lățime cadru"),
                FrameHeight = Ui.Int(r, "fh", "Înălțime cadru"),
                IdleFrames = Ui.IntList(Ui.Cell(r, "idle")),
                WalkFrames = Ui.IntList(Ui.Cell(r, "walk")),
                HasReflection = Ui.Check(r, "refl"),
                Description = Ui.Cell(r, "desc"),
            }).ToList();
            Ctx.Story.Characters = list;
            StructureChanged = !old.SequenceEqual(list.Select(c => c.Id));
            return null;
        }
        catch (FormatException ex) { return "Personaje: " + ex.Message; }
    }
}
