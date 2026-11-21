using System.Diagnostics;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Core.Validation;
using LastStopNight.Editor.Pages;

namespace LastStopNight.Editor;

/// <summary>
/// Aplicatia de editare: arbore cu structura povestii in stanga, pagina de editare in dreapta,
/// lista de probleme de validare jos. Salveaza ca arhiva ZIP (story.json + images/) sau ca folder.
/// </summary>
public sealed partial class EditorForm : Form
{
    private readonly TreeView _tree = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Ui.Panel,
        ForeColor = Ui.Fore,
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 9.5f),
        HideSelection = false,
        FullRowSelect = true,
        ItemHeight = 22,
    };
    private readonly Panel _host = new() { Dock = DockStyle.Fill, BackColor = Ui.Back };
    private readonly ListBox _issues = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(24, 22, 26),
        ForeColor = Ui.Fore,
        BorderStyle = BorderStyle.None,
        Font = new Font("Consolas", 9f),
        IntegralHeight = false,
    };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 22, ForeColor = Color.FromArgb(170, 162, 156), Padding = new Padding(6, 4, 0, 0) };

    private EditorContext? _ctx;
    private EditorPage? _page;
    private string _savedJson = "";
    private bool _suppressSelect;

    public EditorForm(string? path)
    {
        Text = "Last Stop - Editor de povești";
        ClientSize = new Size(1400, 900);
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ui.Back;
        ForeColor = Ui.Fore;
        KeyPreview = true;

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 280, BackColor = Color.FromArgb(60, 30, 36), FixedPanel = FixedPanel.Panel1 };
        split.Panel1.Controls.Add(_tree);
        split.Panel2.Controls.Add(_host);
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 140 };
        bottom.Controls.Add(_issues);
        bottom.Controls.Add(_status);
        Controls.Add(split);
        Controls.Add(bottom);
        Controls.Add(BuildMenu());
        Shown += (_, _) => split.SplitterDistance = 280;

        _tree.BeforeSelect += (_, e) => { if (!_suppressSelect && !CommitPage()) e.Cancel = true; };
        _tree.AfterSelect += (_, e) => { if (!_suppressSelect) ShowPage(e.Node); };
        _issues.DoubleClick += (_, _) => GoToIssue();
        FormClosing += (_, e) => { if (!ConfirmDiscard()) e.Cancel = true; };

        Load += (_, _) =>
        {
            string? p = path ?? FindDefaultStory();
            if (p is not null && File.Exists(p)) Open(p);
            else NewStory();
        };
    }

    // ------------------------------------------------------------------ meniu

    private MenuStrip BuildMenu()
    {
        var m = new MenuStrip { BackColor = Color.FromArgb(36, 32, 38), ForeColor = Ui.Fore };
        var file = new ToolStripMenuItem("&Fișier");
        file.DropDownItems.Add(new ToolStripMenuItem("Poveste nouă", null, (_, _) => { if (ConfirmDiscard()) NewStory(); }, Keys.Control | Keys.N));
        file.DropDownItems.Add(new ToolStripMenuItem("Deschide...", null, (_, _) => OpenDialog(), Keys.Control | Keys.O));
        file.DropDownItems.Add(new ToolStripMenuItem("Salvează", null, (_, _) => Save(false), Keys.Control | Keys.S));
        file.DropDownItems.Add(new ToolStripMenuItem("Salvează ca...", null, (_, _) => Save(true), Keys.Control | Keys.Shift | Keys.S));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("Ieșire", null, (_, _) => Close()));
        var story = new ToolStripMenuItem("&Poveste");
        story.DropDownItems.Add(new ToolStripMenuItem("Adaugă bloc", null, (_, _) => AddBlock(null), Keys.Control | Keys.B));
        story.DropDownItems.Add(new ToolStripMenuItem("Duplică blocul", null, (_, _) => AddBlock(SelectedBlock()), Keys.Control | Keys.D));
        story.DropDownItems.Add(new ToolStripMenuItem("Șterge blocul", null, (_, _) => DeleteBlock(), Keys.Control | Keys.Delete));
        story.DropDownItems.Add(new ToolStripSeparator());
        story.DropDownItems.Add(new ToolStripMenuItem("Validează", null, (_, _) => RunValidation(true), Keys.F6));
        story.DropDownItems.Add(new ToolStripMenuItem("Testează în Player", null, (_, _) => TestInPlayer(), Keys.F5));
        m.Items.AddRange([file, story]);
        foreach (ToolStripMenuItem i in m.Items)
            foreach (ToolStripItem d in i.DropDownItems) { d.BackColor = Color.FromArgb(36, 32, 38); d.ForeColor = Ui.Fore; }
        return m;
    }

    private static string? FindDefaultStory()
    {
        // la rulare din bin/Debug/net10.0-windows: povestea sursa este in ../../../../stories/night_shift/story.json
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "stories", "night_shift", "story.json");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    // ------------------------------------------------------------------ fisiere

    private void NewStory()
    {
        var s = new StoryDefinition
        {
            Title = "Poveste nouă",
            StartBlockId = "start",
            Properties = [new StatePropertyDefinition { Key = "player.life", Min = 0, Max = 100, Initial = 100, HudLabel = "Integritate", Display = HudDisplay.Bar, Color = "#D8333F" }],
            Blocks =
            [
                new StoryBlock { Id = "start", Title = "Început", Text = "Scrie aici prima scenă.", Decisions = [new DecisionDefinition { Text = "Continuă", Target = "end" }] },
                new StoryBlock { Id = "end", Title = "Final", Text = "Sfârșit.", IsEnding = true, EndingType = EndingType.Good },
            ],
        };
        SetPackage(new StoryPackage(s));
    }

    private void OpenDialog()
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new OpenFileDialog { Filter = "Poveste (*.zip;*.json)|*.zip;*.json" };
        if (dlg.ShowDialog(this) == DialogResult.OK) Open(dlg.FileName);
    }

    private void Open(string path)
    {
        try { SetPackage(StoryArchive.Load(path)); }
        catch (Exception ex) { MessageBox.Show(this, "Nu s-a putut deschide povestea:\n\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SetPackage(StoryPackage pkg)
    {
        _ctx?.Dispose();
        _ctx = new EditorContext(pkg);
        _savedJson = StoryArchive.Serialize(pkg.Story);
        _page = null;
        RebuildTree(null);
        RunValidation(false);
        UpdateTitle();
    }

    private bool Save(bool saveAs)
    {
        if (_ctx is null || !CommitPage()) return false;
        string? path = _ctx.Package.SourcePath;
        if (saveAs || path is null)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "Arhivă poveste (*.zip)|*.zip|Folder (story.json + images/)|*.json",
                FileName = path is null ? "poveste.zip" : Path.GetFileName(path),
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return false;
            path = dlg.FileName;
        }
        var errors = RunValidation(false).Count(i => i.Severity == IssueSeverity.Error);
        if (errors > 0 && MessageBox.Show(this, $"Povestea are {errors} erori de validare. Salvezi oricum?", "Validare",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;
        try
        {
            StoryArchive.SaveAuto(_ctx.Package, path);
            _savedJson = StoryArchive.Serialize(_ctx.Story);
            _ctx.FilesChanged = false;
            UpdateTitle();
            _status.Text = $"Salvat: {path}";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Salvarea a eșuat:\n\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool IsDirty => _ctx is not null && (_ctx.FilesChanged || StoryArchive.Serialize(_ctx.Story) != _savedJson);

    private bool ConfirmDiscard()
    {
        if (_ctx is null) return true;
        if (_page is not null && _page.Commit() is null) { } // salveaza in model ce se poate, pentru verificarea „dirty”
        if (!IsDirty) return true;
        var r = MessageBox.Show(this, "Există modificări nesalvate. Le salvezi?", "Modificări", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return r == DialogResult.No || (r == DialogResult.Yes && Save(false));
    }

    private void UpdateTitle()
    {
        if (_ctx is null) return;
        string file = _ctx.Package.SourcePath is { } p ? Path.GetFileName(p) : "(nesalvată)";
        Text = $"{_ctx.Story.Title} - {file}{(IsDirty ? " *" : "")} - Editor Last Stop";
    }

    // ------------------------------------------------------------------ arbore si pagini

    private void RebuildTree(object? select)
    {
        if (_ctx is null) return;
        _suppressSelect = true;
        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        var s = _ctx.Story;
        TreeNode Add(TreeNodeCollection parent, string text, object tag, Color? color = null)
        {
            var n = new TreeNode(text) { Tag = tag };
            if (color is { } c) n.ForeColor = c;
            parent.Add(n);
            return n;
        }
        Add(_tree.Nodes, s.Title, "story");
        Add(_tree.Nodes, $"Proprietăți ({s.Properties.Count})", "props");
        Add(_tree.Nodes, $"Redirecționări ({s.Redirects.Count})", "redirects");
        Add(_tree.Nodes, $"Locații 2D ({s.Locations.Count})", "locations");
        Add(_tree.Nodes, $"Personaje ({s.Characters.Count})", "characters");
        Add(_tree.Nodes, $"Imagini ({_ctx.ImagePaths.Length})", "images");
        var blocks = Add(_tree.Nodes, $"Blocuri ({s.Blocks.Count})", "blocks");
        foreach (var b in s.Blocks)
        {
            var color = b.IsEnding ? Color.FromArgb(230, 120, 120) : b.Id == s.StartBlockId ? Color.FromArgb(140, 220, 150) : (Color?)null;
            var bn = Add(blocks.Nodes, $"{b.Id}   {b.Title}", b, color);
            for (int i = 0; i < b.Decisions.Count; i++)
            {
                var d = b.Decisions[i];
                Add(bn.Nodes, $"→ {d.Target}   {d.Text}{(d.Condition is null ? "" : "  [cond]")}", (b, i), Color.FromArgb(150, 145, 140));
            }
        }
        blocks.Expand();
        _tree.EndUpdate();
        var target = Find(_tree.Nodes, select ?? "story") ?? _tree.Nodes[0];
        _tree.SelectedNode = target;
        _suppressSelect = false;
        ShowPage(target);
    }

    private static TreeNode? Find(TreeNodeCollection nodes, object tag)
    {
        foreach (TreeNode n in nodes)
        {
            if (Equals(n.Tag, tag) || (tag is StoryBlock b && n.Tag is StoryBlock nb && nb == b)) return n;
            if (Find(n.Nodes, tag) is { } f) return f;
        }
        return null;
    }

    private void ShowPage(TreeNode? node)
    {
        if (_ctx is null || node is null) return;
        EditorPage? page = node.Tag switch
        {
            "story" => new StoryPage(_ctx),
            "props" => new PropertiesPage(_ctx),
            "redirects" => new RedirectsPage(_ctx),
            "locations" => new LocationsPage(_ctx),
            "characters" => new CharactersPage(_ctx),
            "images" => new ImagesPage(_ctx),
            StoryBlock b => new BlockPage(_ctx, b),
            ValueTuple<StoryBlock, int> t => new BlockPage(_ctx, t.Item1, t.Item2),
            _ => null,
        };
        _host.SuspendLayout();
        foreach (Control c in _host.Controls) c.Dispose();
        _host.Controls.Clear();
        _page = page;
        if (page is not null) _host.Controls.Add(page);
        else _host.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Selectează un bloc din listă.\nCtrl+B adaugă un bloc nou, F6 validează, F5 testează în Player.",
        });
        _host.ResumeLayout();
    }

    /// <summary>Scrie pagina curenta in model. False (cu mesaj) daca datele nu sunt valide.</summary>
    private bool CommitPage()
    {
        if (_page is null) return true;
        var error = _page.Commit();
        if (error is not null)
        {
            MessageBox.Show(this, error, "Date nevalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (_page.StructureChanged)
        {
            var current = _tree.SelectedNode?.Tag;
            BeginInvoke(() => RebuildTree(current));
        }
        UpdateTitle();
        return true;
    }

    // ------------------------------------------------------------------ comenzi

    private StoryBlock? SelectedBlock() => _tree.SelectedNode?.Tag switch
    {
        StoryBlock b => b,
        ValueTuple<StoryBlock, int> t => t.Item1,
        _ => null,
    };

    private void AddBlock(StoryBlock? template)
    {
        if (_ctx is null || !CommitPage()) return;
        string baseId = template is null ? "bloc.nou" : template.Id + ".copie";
        string id = baseId;
        for (int i = 2; _ctx.Story.FindBlock(id) is not null; i++) id = $"{baseId}{i}";
        StoryBlock block;
        if (template is null)
            block = new StoryBlock { Id = id, Title = "Bloc nou", Location = _ctx.Story.Locations.FirstOrDefault()?.Id };
        else
        {
            block = StoryArchive.Deserialize(StoryArchive.Serialize(new StoryDefinition { Blocks = [template] })).Blocks[0];
            block.Id = id;
        }
        int at = template is null ? _ctx.Story.Blocks.Count : _ctx.Story.Blocks.IndexOf(template) + 1;
        _ctx.Story.Blocks.Insert(at, block);
        _page = null;
        RebuildTree(block);
        UpdateTitle();
    }

    private void DeleteBlock()
    {
        if (_ctx is null || SelectedBlock() is not { } b) return;
        int refs = _ctx.Story.Blocks.SelectMany(x => x.Decisions).Count(d => d.Target == b.Id);
        string msg = $"Ștergi blocul „{b.Id}”?" + (refs > 0 ? $"\n\nAtenție: {refs} decizii trimit către el." : "");
        if (MessageBox.Show(this, msg, "Ștergere bloc", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _ctx.Story.Blocks.Remove(b);
        _page = null;
        RebuildTree("blocks");
        RunValidation(false);
        UpdateTitle();
    }

    private List<ValidationIssue> RunValidation(bool commitFirst)
    {
        if (_ctx is null || (commitFirst && !CommitPage())) return [];
        var issues = StoryValidator.Validate(_ctx.Package);
        _issues.Items.Clear();
        if (issues.Count == 0) _issues.Items.Add("✔ Povestea este validă: nicio eroare, niciun avertisment.");
        foreach (var i in issues) _issues.Items.Add(i);
        int e = issues.Count(i => i.Severity == IssueSeverity.Error);
        _status.Text = $"Validare: {e} erori, {issues.Count - e} avertismente   (dublu-click pe o problemă pentru a merge la bloc)";
        return issues;
    }

    private void GoToIssue()
    {
        if (_ctx is null || _issues.SelectedItem is not ValidationIssue issue) return;
        var m = System.Text.RegularExpressions.Regex.Match(issue.Where, @"Blocul (\S+?)(,|$)");
        if (m.Success && _ctx.Story.FindBlock(m.Groups[1].Value) is { } b && Find(_tree.Nodes, b) is { } node)
            _tree.SelectedNode = node;
    }

    private void TestInPlayer()
    {
        if (_ctx is null || !CommitPage()) return;
        if (RunValidation(false).Any(i => i.Severity == IssueSeverity.Error)
            && MessageBox.Show(this, "Povestea are erori. Pornești totuși Player-ul?", "Validare", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;
        string? exe = FindPlayer();
        if (exe is null)
        {
            using var dlg = new OpenFileDialog { Title = "Unde este LastStopNight.exe?", Filter = "Player|LastStopNight.exe" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            exe = dlg.FileName;
        }
        string tmp = Path.Combine(Path.GetTempPath(), "LastStopNight_test.zip");
        string? keep = _ctx.Package.SourcePath;
        StoryArchive.Save(_ctx.Package, tmp);
        _ctx.Package.SourcePath = keep;
        Process.Start(new ProcessStartInfo(exe, $"\"{tmp}\"") { UseShellExecute = false });
    }

    private static string? FindPlayer()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            foreach (var cfg in new[] { "Debug", "Release" })
            {
                var c = Path.Combine(dir.FullName, "LastStopNight", "bin", cfg, "net10.0-windows", "LastStopNight.exe");
                if (File.Exists(c)) return c;
            }
            var same = Path.Combine(dir.FullName, "LastStopNight.exe");
            if (File.Exists(same)) return same;
        }
        return null;
    }
}
