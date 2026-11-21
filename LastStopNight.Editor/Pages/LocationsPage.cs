using LastStopNight.Core.Model;

namespace LastStopNight.Editor.Pages;

/// <summary>Locatiile 2D, cu previzualizare (podea, limite de mers si ferestre de reflexie).</summary>
public sealed class LocationsPage : EditorPage
{
    private readonly DataGridView _grid = Ui.Grid();
    private readonly PictureBox _preview = new() { Dock = DockStyle.Right, Width = 480, BackColor = Color.Black };

    public LocationsPage(EditorContext ctx) : base(ctx)
    {
        Ui.AddText(_grid, "id", "ID", 70);
        Ui.AddText(_grid, "name", "Nume", 110);
        var img = Ui.AddCombo(_grid, "image", "Imagine", ctx.ImagePaths, 140);
        Ui.AddText(_grid, "floor", "Podea Y", 45);
        Ui.AddText(_grid, "minx", "Min X", 40);
        Ui.AddText(_grid, "maxx", "Max X", 40);
        Ui.AddText(_grid, "windows", "Ferestre (x,y,w,h; ...)", 110);
        Ui.AddText(_grid, "ambient", "Ambient", 50);
        Ui.AddText(_grid, "sounds", "Sunete", 60);
        Ui.AddText(_grid, "points", "Puncte (nume=x,y; ...)", 110);
        Ui.EnsureItems(img, ctx.Story.Locations.Select(l => l.Image));
        foreach (var l in ctx.Story.Locations)
        {
            int i = _grid.Rows.Add(l.Id, l.Name, l.Image, l.FloorY, l.MinX, l.MaxX,
                string.Join("; ", l.Windows.Select(w => $"{w.X},{w.Y},{w.W},{w.H}")),
                string.Join(", ", l.Ambient),
                string.Join(", ", l.Sounds),
                string.Join("; ", l.Points.Select(p => $"{p.Key}={p.Value.X},{p.Value.Y}")));
            _grid.Rows[i].Tag = l; // decorul, luminile si obiectele de examinat se pastreaza
        }
        _grid.SelectionChanged += (_, _) => _preview.Invalidate();
        _grid.CellValueChanged += (_, _) => _preview.Invalidate();
        _preview.Paint += PaintPreview;
        Load += (_, _) => { if (_grid.Rows.Count > 1) _grid.CurrentCell = _grid.Rows[0].Cells[1]; _preview.Invalidate(); };
        Controls.Add(_grid);
        Controls.Add(_preview);
        Controls.Add(Title("LOCAȚII 2D",
            "Rezoluție logică 320x180. Roșu = podeaua, verde = limitele de mers, albastru = ferestre (reflexii), galben = obiecte de examinat. " +
            "Sunete: " + string.Join(", ", LocationDefinition.KnownSounds) + "."));
    }

    private void PaintPreview(object? sender, PaintEventArgs e)
    {
        var row = _grid.CurrentRow;
        if (row is null || row.IsNewRow) return;
        var bmp = Ctx.GetImage(Ui.Cell(row, "image"));
        float s = _preview.Width / 320f;
        var g = e.Graphics;
        Ui.PixelArt(g);
        if (bmp is not null) g.DrawImage(bmp, 0, 0, 320 * s, 180 * s);
        int.TryParse(Ui.Cell(row, "floor"), out int floor);
        int.TryParse(Ui.Cell(row, "minx"), out int minx);
        int.TryParse(Ui.Cell(row, "maxx"), out int maxx);
        using var red = new Pen(Color.Red, 2);
        using var green = new Pen(Color.Lime, 2);
        using var blue = new Pen(Color.DeepSkyBlue, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        g.DrawLine(red, 0, floor * s, 320 * s, floor * s);
        g.DrawLine(green, minx * s, (floor - 50) * s, minx * s, floor * s);
        g.DrawLine(green, maxx * s, (floor - 50) * s, maxx * s, floor * s);
        try { foreach (var w in ParseWindows(Ui.Cell(row, "windows"))) g.DrawRectangle(blue, w.X * s, w.Y * s, w.W * s, w.H * s); }
        catch (FormatException) { }
        if (row.Tag is LocationDefinition loc)
        {
            using var yellow = new SolidBrush(Color.Gold);
            using var font = new Font("Segoe UI", 7.5f);
            foreach (var ex in loc.Examine)
            {
                g.FillEllipse(yellow, ex.X * s - 4, (floor - 50) * s - 4, 8, 8);
                g.DrawString(ex.Label, font, yellow, ex.X * s + 5, (floor - 50) * s - 14);
            }
        }
    }

    private static List<RectDefinition> ParseWindows(string s) =>
        s.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(part =>
        {
            var n = Ui.IntList(part);
            if (n.Length != 4) throw new FormatException($"Fereastra „{part}” trebuie să aibă forma x,y,w,h.");
            return new RectDefinition { X = n[0], Y = n[1], W = n[2], H = n[3] };
        }).ToList();

    public override string? Commit()
    {
        try
        {
            var old = Ctx.Story.Locations.Select(l => l.Id).ToList();
            var list = Ui.Rows(_grid).Select(r =>
            {
                var points = new Dictionary<string, PointDefinition>();
                foreach (var p in Ui.Cell(r, "points").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var kv = p.Split('=', 2);
                    var xy = kv.Length == 2 ? Ui.IntList(kv[1]) : [];
                    if (xy.Length != 2) throw new FormatException($"Punctul „{p}” trebuie să aibă forma nume=x,y.");
                    points[kv[0].Trim()] = new PointDefinition { X = xy[0], Y = xy[1] };
                }
                var original = r.Tag as LocationDefinition;
                return new LocationDefinition
                {
                    Decor = original?.Decor ?? new(),
                    Lights = original?.Lights ?? new(),
                    Examine = original?.Examine ?? new(),
                    ActorTint = original?.ActorTint,
                    Id = Ui.Cell(r, "id"),
                    Name = Ui.Cell(r, "name"),
                    Image = Ui.Cell(r, "image"),
                    FloorY = Ui.Int(r, "floor", "Podea Y"),
                    MinX = Ui.Int(r, "minx", "Min X"),
                    MaxX = Ui.Int(r, "maxx", "Max X"),
                    Windows = ParseWindows(Ui.Cell(r, "windows")),
                    Ambient = Ui.Cell(r, "ambient").Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Sounds = Ui.Cell(r, "sounds").Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Points = points,
                };
            }).ToList();
            Ctx.Story.Locations = list;
            StructureChanged = !old.SequenceEqual(list.Select(l => l.Id));
            return null;
        }
        catch (FormatException ex) { return ex.Message; }
    }
}
