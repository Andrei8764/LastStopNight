using System.Globalization;
using System.Runtime.InteropServices;
using LastStopNight.Game;

namespace LastStopNight;

/// <summary>
/// Punctul de intrare al jocului.
/// <list type="bullet">
/// <item><c>LastStopNight.exe [poveste.zip | story.json]</c>: jocul normal (optional, cu o singura poveste).</item>
/// <item><c>--snapshots &lt;dir&gt; &lt;poveste[:insotitor] | continue&gt; &lt;decizii, ex. 0,1,-1&gt; [secunde]</c>: joaca singur si face capturi.</item>
/// <item><c>--mp-test host|join &lt;dir&gt;</c>: doua instante joaca impreuna multiplayer si fac capturi.</item>
/// </list>
/// Variabile de mediu pentru teste: LASTSTOP_DATA (alt dosar pentru salvari), LASTSTOP_LOOPBACK=1 (retea doar
/// pe acest PC), LASTSTOP_NOUPNP=1 (fara deschiderea portului pe router), LASTSTOP_JOIN_CODE / LASTSTOP_JOIN_LAN,
/// LASTSTOP_MUTE=1 (fara sunet la difuzoare).
/// </summary>
internal static class Program
{
    // rezolutia implicita a timerelor Windows e ~15,6 ms; cu 1 ms bucla de joc e mult mai regulata
    [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint ms);
    [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint ms);

    private const float DefaultSnapshotWaitSeconds = 3.2f;

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        InstallCrashHandler();
        timeBeginPeriod(1);
        try { Application.Run(CreateForm(args)); }
        finally { timeEndPeriod(1); }
    }

    private static PlayerForm CreateForm(string[] args)
    {
        int mp = Array.IndexOf(args, "--mp-test");
        if (mp >= 0 && args.Length > mp + 2)
            return DevForm(args[mp + 2], form => form.RunMpTestAsync(args[mp + 1], args[mp + 2]));

        int snap = Array.IndexOf(args, "--snapshots");
        if (snap >= 0 && args.Length > snap + 3)
        {
            // „1” = povestea 1; „1:vlad” = povestea 1 cu Vlad ca insotitor; „continue” = reia salvarea
            var storyArg = args[snap + 2].Split(':');
            int story = storyArg[0] == "continue" ? -1 : int.Parse(storyArg[0], CultureInfo.InvariantCulture);
            string? companion = storyArg.Length > 1 ? storyArg[1] : null;
            var decisions = args[snap + 3].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.Parse(d, CultureInfo.InvariantCulture)).ToList();
            float wait = args.Length > snap + 4 ? float.Parse(args[snap + 4], CultureInfo.InvariantCulture) : DefaultSnapshotWaitSeconds;
            return DevForm(args[snap + 1], form => form.RunSnapshotsAsync(args[snap + 1], story, companion, decisions, wait));
        }

        return new PlayerForm(args.FirstOrDefault(a => !a.StartsWith("--")));
    }

    /// <summary>Fereastra unui mod de dezvoltare: ruleaza scenariul la afisare; o eroare ajunge in error.txt, nu intr-un dialog.</summary>
    private static PlayerForm DevForm(string outDir, Func<PlayerForm, Task> scenario)
    {
        Environment.SetEnvironmentVariable("LASTSTOP_MUTE", "1"); // testele automate nu fac zgomot
        var form = new PlayerForm(null);
        form.Shown += async (_, _) =>
        {
            try { await scenario(form); }
            catch (Exception ex)
            {
                Directory.CreateDirectory(outDir);
                File.WriteAllText(Path.Combine(outDir, "error.txt"), ex.ToString());
                form.Close();
            }
        };
        return form;
    }

    /// <summary>Nicio eroare nu ajunge la fereastra bruta .NET: un mesaj scurt, iar detaliile in crash.log.</summary>
    private static void InstallCrashHandler()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(e.Exception, canContinue: true);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ReportCrash(e.ExceptionObject as Exception, canContinue: false);
    }

    private static string CrashLogPath => AppData.PathOf("crash.log");

    private static void ReportCrash(Exception? ex, bool canContinue)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.AppendAllText(CrashLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        MessageBox.Show(
            "Ceva n-a mers cum trebuia." + (canContinue ? " Poți continua să joci." : "") +
            $"\n\n{ex?.Message}\n\nDetaliile sunt salvate în:\n{CrashLogPath}",
            "Last Stop: eroare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
