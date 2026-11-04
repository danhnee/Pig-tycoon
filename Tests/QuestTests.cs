using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>Season 1 quest line (Docs/COT_TRUYEN_PIG_TYCOON.md Q01–Q20): story titles, steps driven by real actions, rewards, save.</summary>
internal static class QuestTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var e = new GameEngine(seed: 5);
        Check(StoryQuestBook.Main.Count == 20 && StoryQuestBook.Main[0].Title == "Cậu tới nhận trại"
              && StoryQuestBook.Main[19].Title == "Rìu Sắt trả hàng", "titles follow the story doc");
        Check(e.Quests.ActiveMain!.Id == "Q01", "the game starts on Q01");
        e.Quests.Evaluate(e);
        Check(e.Quests.ActiveMain!.Id == "Q01", "Q01 does not finish by itself");

        int gold = e.Farm.Gold;
        e.SignalQuest(QuestSignal.FeedPoured);
        e.SignalQuest(QuestSignal.WaterRefilled);
        e.SignalQuest(QuestSignal.FenceTempPatched);
        Check(e.Quests.ActiveMain!.Id == "Q01", "Q01 still needs the stray pig");
        e.CommandHerdRecallToRefuge();
        Check(e.Quests.Completed.Contains("Q01") && e.Farm.Gold == gold + 150, "Q01 pays 150G");

        // signals sent before a quest is active do not count for it
        Check(e.Quests.ActiveMain!.Id == "Q02" && !e.Quests.Has("Q02", QuestSignal.FenceTempPatched), "Q02 starts empty");
        e.SignalQuest(QuestSignal.CapacityBoardRead);
        e.SignalQuest(QuestSignal.FeedPoured);
        e.SignalQuest(QuestSignal.WaterRefilled);
        Check(e.Quests.ActiveMain!.Id == "Q03", "Q02 done");

        e.CommandHerdRecallToRefuge();
        Check(e.Quests.ActiveMain!.Id == "Q03", "Q03 needs a survived major wave");
        e.MajorWavesSurvived = 1;
        e.Quests.Evaluate(e);
        Check(e.Quests.ActiveMain!.Id == "Q04", "Q03 done after the first major wave");

        var view = e.Quests.View(e, e.Quests.ActiveMain);
        Check(view!.Objective.StartsWith("Chưa mở") || e.Farm.FarmLevel >= 3, "Q04 shows its lock until farm level 3");

        var data = e.CreateSaveData();
        var e2 = new GameEngine(seed: 5);
        e2.RestoreFromSave(data);
        Check(e2.Quests.Completed.SetEquals(e.Quests.Completed) && e2.Quests.ActiveMain!.Id == "Q04", "quests survive save/load");
    }
}
