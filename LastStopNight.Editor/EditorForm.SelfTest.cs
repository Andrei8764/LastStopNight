using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Core.Validation;

namespace LastStopNight.Editor;

/// <summary>Modul de verificare automata (--selftest): viziteaza toate paginile si confirma ca nu se pierde nimic.</summary>
public sealed partial class EditorForm
{
    /// <summary>
    /// Mod de dezvoltare: selecteaza fiecare nod din arbore (ceea ce salveaza pagina anterioara in model),
    /// face capturi si verifica ca un ciclu incarcare → salvare a paginilor nu modifica povestea.
    /// </summary>
    public async Task SelfTestAsync(string outDir)
    {
        Directory.CreateDirectory(outDir);
        TopMost = true;
        var report = new List<string>();
        var nodes = new List<TreeNode>();
        void Collect(TreeNodeCollection c) { foreach (TreeNode n in c) { nodes.Add(n); Collect(n.Nodes); } }
        Collect(_tree.Nodes);
        var shots = new HashSet<string> { "story", "props", "locations", "characters", "images" };
        int i = 0;
        foreach (var n in nodes)
        {
            _tree.SelectedNode = n;
            await Task.Delay(120);
            string key = n.Tag switch { StoryBlock b => "block_" + b.Id, string s => s, _ => "" };
            if (shots.Contains(key) || key is "block_night1.open" or "block_night3.double")
            {
                await Task.Delay(400);
                using var bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
                using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, ClientSize);
                bmp.Save(Path.Combine(outDir, $"{i++:00}_{key}.png"));
            }
        }
        _tree.SelectedNode = _tree.Nodes[0];
        await Task.Delay(100);
        report.Add($"Noduri vizitate: {nodes.Count}");
        report.Add($"Modificat după vizitarea tuturor paginilor (trebuie False): {IsDirty}");
        report.Add($"Probleme de validare: {StoryValidator.Validate(_ctx!.Package).Count}");
        File.WriteAllLines(Path.Combine(outDir, "report.txt"), report);
        _savedJson = StoryArchive.Serialize(_ctx.Story); // nu intreba la inchidere
        _ctx.FilesChanged = false;
        Close();
    }
}
