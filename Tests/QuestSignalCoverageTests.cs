using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Every quest step must be completable in real play: each QuestSignal a step listens for has to be sent from game code
/// (not only from tests or from the narrative listeners). 3 Nov 2026: WaterRefilled was never sent, so Q01 — and with it
/// the whole main line Q02–Q20 — could not finish.
/// </summary>
internal static class QuestSignalCoverageTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        string root = Directory.GetCurrentDirectory();
        while (root != null && !File.Exists(Path.Combine(root, "global.json"))) root = Path.GetDirectoryName(root);
        Check(root != null, "repo root");
        string scripts = Path.Combine(root!, "Assets", "Scripts");
        string book = File.ReadAllText(Directory.GetFiles(Path.Combine(scripts, "Core"), "StoryQuestBook.cs", SearchOption.AllDirectories)[0]);

        // signals the quest steps wait for: Sig("…", "Qxx", QuestSignal.X)
        var needed = Regex.Matches(book, @"Sig\(""[^""]*"",\s*""[^""]*"",\s*QuestSignal\.(\w+)\)").Select(m => m.Groups[1].Value).Distinct().ToList();
        Check(needed.Count > 0, "quest steps found");

        // emitters: game code outside the quest book and the narrative listeners, plus the station map
        var emitted = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("NarrativeContent", StringComparison.Ordinal) && Path.GetFileName(f) != "StoryQuestBook.cs")
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"QuestSignal\.(\w+)").Select(m => m.Groups[1].Value))
            .ToHashSet();
        var stationMap = Regex.Match(book, @"SignalForStation\(string stationId\) => stationId switch\s*\{(.*?)\};", RegexOptions.Singleline).Groups[1].Value;
        foreach (Match m in Regex.Matches(stationMap, @"QuestSignal\.(\w+)")) emitted.Add(m.Groups[1].Value);

        var orphans = needed.Where(s => !emitted.Contains(s)).ToList();
        Check(orphans.Count == 0, "quest steps wait for signals the game never sends: " + string.Join(", ", orphans));
    }
}
