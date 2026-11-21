using LastStopNight.Audio;
using LastStopNight.Net;
using StoryCheck;

// Verificarea automata a jocului:
//   - fiecare poveste: validare, toate starile accesibile, finaluri, fundaturi, reaplicarea jurnalului;
//   - teste pentru nucleu (parsere, stare, motor, arhive, validator), sunet (WAV, mixer) si multiplayer (TCP pe localhost).
// Rulare (din radacina repo-ului):
//   dotnet run --project tools/StoryCheck                 toate povestile din LastStopNight/Assets/stories
//   dotnet run --project tools/StoryCheck -- a.zip b.zip  doar povestile date (testele de retea cer km13.zip)
//   dotnet run --project tools/StoryCheck -- --probe      doar citeste reteaua (IP, Tailscale, UPnP; nu deschide porturi)
//                                                         si verifica placa de sunet (un clopotel scurt)
// Codul de iesire e 0 doar daca toate verificarile trec.

if (args.Contains("--probe"))
{
    Console.WriteLine($"IP în rețeaua locală: {NetInfo.LocalIPv4()}");
    foreach (var (label, ip) in NetInfo.VirtualNetworks()) Console.WriteLine($"Rețea virtuală {label}: {ip}");
    var probe = await PortMapping.ProbeAsync(TimeSpan.FromSeconds(5));
    Console.WriteLine($"UPnP: {(probe.Supported ? "da" : "nu")}. {probe.Message} IP extern: {probe.ExternalIp?.ToString() ?? "necunoscut"}");
    using (var audio = new AudioEngine(SoundBank.FromFolder(Path.Combine("LastStopNight", "Assets", "sounds"))))
    {
        Console.WriteLine($"Placa de sunet: {(audio.HasOutput ? "da (se aude un clopoțel)" : "nu")}");
        audio.Play("join");
        Thread.Sleep(900);
    }
    return 0;
}

var paths = (args.Length > 0 ? args : Directory.GetFiles(Path.Combine("LastStopNight", "Assets", "stories"), "*.zip"))
    .OrderBy(p => p).ToList();
var report = new Report();
foreach (var path in paths) StoryExplorer.Run(report, path);
CoreTests.Run(report, paths);
AudioTests.Run(report, paths);
await NetworkTests.Run(report, paths);

Console.WriteLine(report.Failures == 0 ? "\nTOATE VERIFICĂRILE AU TRECUT" : $"\n{report.Failures} VERIFICĂRI EȘUATE");
return report.Failures == 0 ? 0 : 1;
