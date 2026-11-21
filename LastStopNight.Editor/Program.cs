namespace LastStopNight.Editor;

internal static class Program
{
    /// <summary>
    /// Editorul de povesti. Argument optional: o poveste (.zip sau story.json) de deschis.
    /// Mod de dezvoltare: --selftest &lt;dir&gt; (capturi + verificare round-trip).
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        int t = Array.IndexOf(args, "--selftest");
        var form = new EditorForm(args.FirstOrDefault(a => !a.StartsWith("--") && (t < 0 || a != args.ElementAtOrDefault(t + 1))));
        if (t >= 0 && args.Length > t + 1)
            form.Shown += async (_, _) => { await Task.Delay(500); await form.SelfTestAsync(args[t + 1]); };
        Application.Run(form);
    }
}
