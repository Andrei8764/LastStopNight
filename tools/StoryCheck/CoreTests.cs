using System.Text.Json;
using LastStopNight.Core.Engine;
using LastStopNight.Core.Expressions;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Core.Validation;

namespace StoryCheck;

/// <summary>Teste mici, fara retea: parsere, starea jocului, motorul, arhivele si validatorul.</summary>
public static class CoreTests
{
    public static void Run(Report report, IReadOnlyList<string> storyPaths)
    {
        report.Section("Teste generale");
        Parsers(report);
        State(report);
        ActionLog(report);
        RouteMap(report);
        var nightShift = storyPaths.FirstOrDefault(p => p.Contains("night_shift"));
        if (nightShift is not null)
        {
            Redirect(report, nightShift);
            ArchiveRoundTrip(report, nightShift);
            ValidatorCatchesErrors(report, nightShift);
        }
    }

    private static void Parsers(Report report)
    {
        var c = ConditionParser.Parse("story.evidence >= 2 AND (player.life > 0 OR story.night = 3)");
        report.Check(c!.ToExpression() == "story.evidence >= 2 AND (player.life > 0 OR story.night == 3)", "parser condiții AND/OR/paranteze");
        report.Check(ConditionParser.Parse("a > 1 && b < 2 || c == 3")!.ToExpression() == "(a > 1 AND b < 2) OR c == 3",
            "AND leagă mai tare decât OR (și && / || merg ca AND / OR)");
        report.Check(ConditionParser.Parse("x >= -5")!.ToExpression() == "x >= -5", "numerele negative sunt acceptate");
        report.Check(ConditionParser.Parse("   ") is null, "o condiție goală înseamnă „mereu adevărat” (null)");
        report.Check(!ConditionParser.TryParse("story.evidence >> 2", out _, out _), "condiție nevalidă respinsă");
        report.Check(!ConditionParser.TryParse("(a > 1", out _, out var err) && err!.Contains(')'), "paranteză neînchisă: mesaj clar");
        report.Check(EffectParser.Format(EffectParser.Parse("player.life -35; player.suspicion +20; story.night = 2"))
                     == "player.life -35; player.suspicion +20; story.night = 2", "parser efecte");
        report.Check(EffectParser.Format(EffectParser.Parse("a += 3, b -= 2")) == "a +3; b -2", "efecte cu += / -= și virgulă");
        report.Throws<ExpressionException>(() => EffectParser.Parse("player.life cu 5"), "efect nevalid respins");
    }

    private static void State(Report report)
    {
        var state = new GameState([new StatePropertyDefinition { Key = "life", Min = 0, Max = 100, Initial = 150 }]);
        report.Check(state.Get("life") == 100, "valoarea inițială e limitată la [min, max]");
        var change = state.Apply(new EffectDefinition { Key = "life", Op = EffectOperation.Add, Value = -130 });
        report.Check(state.Get("life") == 0 && change.Delta == -100, "efectele sunt limitate la min, iar diferența raportată e cea reală");
        int version = state.Version;
        state.Set("life", 40);
        report.Check(state.Version > version, "orice modificare crește Version (HUD-ul se redesenează)");
        report.Throws<KeyNotFoundException>(() => state.Get("lipsă"), "o proprietate necunoscută e eroare, nu 0 tăcut");
    }

    /// <summary>Harta turei: drumul fiecarui motor si arborele lor (partea comuna o data, ramuri unde se despart).</summary>
    private static void RouteMap(Report report)
    {
        var tree = RouteTree.Build([["a", "b", "c"], ["a", "b", "d"], ["a", "e"]]);
        var byId = tree.ToDictionary(n => n.BlockId);
        report.Check(tree.Count == 5 && byId["a"].Routes.Count == 3 && byId["b"].Routes.Count == 2
                     && byId["c"].Lane == byId["a"].Lane && byId["d"].Lane != byId["c"].Lane && byId["e"].Lane != byId["d"].Lane
                     && byId["c"].Current.SequenceEqual([0]) && byId["d"].Current.SequenceEqual([1]) && byId["e"].Depth == 1,
            "harta: partea comună apare o dată, drumurile despărțite primesc coloane proprii");
        report.Check(RouteTree.Build([["a", "b"], ["a", "c", "b"]]).Count(n => n.BlockId == "b") == 2,
            "harta: același bloc atins pe drumuri diferite rămâne în ramuri separate");
    }

    private static void ActionLog(Report report)
    {
        var log = new List<GameAction> { new ChooseAction("a", 1, "alex"), new ExamineAction("b", "x", "ioana") };
        var back = JsonSerializer.Deserialize<List<GameAction>>(JsonSerializer.Serialize(log))!;
        report.Check(back.SequenceEqual(log), "jurnalul de acțiuni se salvează și se reîncarcă identic");
    }

    private static void Redirect(Report report, string path)
    {
        var engine = new GameEngine(StoryArchive.Load(path).Story);
        engine.Start();
        engine.State.Set("player.life", 30);
        foreach (var i in new[] { 0, 1, 0, 1 }) engine.Choose(i); // → night1.danger
        engine.Choose(0);                                          // player.life -35
        report.Check(engine.CurrentBlock!.Id == "ending.captured" && engine.State.Get("player.life") == 0,
            "redirecționare automată player.life = 0 → ending.captured (cu limitare la min)");
        report.Throws<InvalidOperationException>(() => engine.Apply(new ChooseAction("alt.bloc", 0, "alex")),
            "o acțiune pentru alt bloc decât cel curent e refuzată");
    }

    private static void ArchiveRoundTrip(Report report, string path)
    {
        var pkg = StoryArchive.Load(path);
        var bytes = StoryArchive.ToZipBytes(pkg);
        using var ms = new MemoryStream(bytes);
        var back = StoryArchive.LoadZip(ms);
        report.Check(StoryArchive.Fingerprint(back) == StoryArchive.Fingerprint(pkg) && back.Files.Count == pkg.Files.Count,
            "arhiva scrisă în memorie și recitită are aceeași amprentă (transferul prin rețea)");
        back.SetFile("images/extra.png", [1, 2, 3]);
        report.Check(StoryArchive.Fingerprint(back) != StoryArchive.Fingerprint(pkg), "un fișier în plus schimbă amprenta");
    }

    private static void ValidatorCatchesErrors(Report report, string path)
    {
        var pkg = StoryArchive.Load(path);
        var block = pkg.Story.Blocks.First(b => !b.IsEnding);
        block.Decisions[0].Target = "nu.exista";
        block.Decisions[0].Effects.Add(new EffectDefinition { Key = "proprietate.inventata", Value = 1 });
        pkg.Story.StartBlockId = "lipsa";
        var errors = StoryValidator.Validate(pkg).Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Message).ToList();
        report.Check(errors.Any(m => m.Contains("nu.exista")) && errors.Any(m => m.Contains("proprietate.inventata"))
                     && errors.Any(m => m.Contains("start")), "validatorul prinde destinații, chei și start inexistente");
    }
}
