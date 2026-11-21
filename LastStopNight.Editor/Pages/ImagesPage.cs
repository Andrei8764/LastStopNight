namespace LastStopNight.Editor.Pages;

/// <summary>Imaginile din arhiva: lista, previzualizare, import si stergere.</summary>
public sealed class ImagesPage : EditorPage
{
    private readonly ListBox _list = new()
    {
        Dock = DockStyle.Left,
        Width = 300,
        BackColor = Ui.Panel,
        ForeColor = Ui.Fore,
        BorderStyle = BorderStyle.None,
        Font = new Font("Consolas", 9.5f),
        IntegralHeight = false,
    };
    private readonly PictureBox _preview = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 18, 22) };
    private readonly ComboBox _folder = Ui.Combo(["images/", "images/sprites/", "images/closeups/", "images/endings/", "images/ui/"], true);
    private readonly Label _info = Ui.Label("");

    public ImagesPage(EditorContext ctx) : base(ctx)
    {
        _folder.SelectedIndex = 0;
        _folder.Dock = DockStyle.None;
        _folder.Width = 170;
        Reload();
        _list.SelectedIndexChanged += (_, _) =>
        {
            var bmp = Ctx.GetImage(_list.SelectedItem as string);
            _info.Text = bmp is null ? "" : $"{bmp.Width} × {bmp.Height} px";
            _preview.Invalidate();
        };
        _preview.Paint += (_, e) =>
        {
            if (Ctx.GetImage(_list.SelectedItem as string) is not { } bmp) return;
            Ui.PixelArt(e.Graphics);
            float s = Math.Max(1, (float)Math.Floor(Math.Min(_preview.Width / (float)bmp.Width, _preview.Height / (float)bmp.Height)));
            e.Graphics.DrawImage(bmp, 10, 10, bmp.Width * s, bmp.Height * s);
        };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        bar.Controls.Add(Ui.Label("Folder:"));
        bar.Controls.Add(_folder);
        bar.Controls.Add(Ui.Button("Importă imagini...", (_, _) => Import()));
        bar.Controls.Add(Ui.Button("Șterge imaginea", (_, _) => Remove()));
        bar.Controls.Add(_info);
        Controls.Add(_preview);
        Controls.Add(_list);
        Controls.Add(bar);
        Controls.Add(Title("IMAGINI", "Fișierele din directorul images/ al arhivei. Căile sunt relative (ex. images/kitchen.png)."));
    }

    private void Reload()
    {
        _list.Items.Clear();
        _list.Items.AddRange(Ctx.ImagePaths.Cast<object>().ToArray());
    }

    private void Import()
    {
        using var dlg = new OpenFileDialog { Multiselect = true, Filter = "Imagini (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        string folder = (_folder.Text.Trim().TrimEnd('/') + "/").Replace('\\', '/');
        if (!folder.StartsWith("images/")) folder = "images/" + folder;
        foreach (var f in dlg.FileNames)
            Ctx.SetFile(folder + Path.GetFileName(f).ToLowerInvariant().Replace(' ', '_'), File.ReadAllBytes(f));
        Reload();
        StructureChanged = true;
    }

    private void Remove()
    {
        if (_list.SelectedItem is not string path) return;
        bool used = Ctx.Story.ReferencedImages().Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        var msg = used ? $"„{path}” este folosită în poveste. O ștergi oricum?" : $"Ștergi „{path}” din arhivă?";
        if (MessageBox.Show(this, msg, "Ștergere imagine", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Ctx.RemoveFile(path);
        Reload();
        StructureChanged = true;
    }

    public override string? Commit() => null;
}
