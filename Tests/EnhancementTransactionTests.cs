using System;
using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;

internal static class EnhancementTransactionTests
{
#if ENHANCEMENT_TEST_RUNNER
    public static void Main() => Run();
#endif
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    static GameEngine Ready(int level = 0, SpecialtyPaperColor color = SpecialtyPaperColor.Lam, int seed = 41)
    {
        var e = new GameEngine(seed);
        e.DeepMechanics.MidGameGateUnlocked = true;
        e.DeepMechanics.EquippedPaperColor = color;
        e.DeepMechanics.intEquippedPaperEnhancementLevel = level;
        e.WorldLocations.CurrentLocationId = EnhancementTransactions.WorkshopLocationId;
        e.WorldLocations.PlayerTileX = EnhancementTransactions.StationTileX;
        e.WorldLocations.PlayerTileY = EnhancementTransactions.StationTileY;
        e.Farm.Gold = 100000;
        e.Inventory.AddItem(EnhancementTransactions.PaperId(color), 1, 2);
        var quote = EnhancementTransactions.Quote(e);
        foreach (var material in quote.Materials) e.Inventory.AddItem(material.Key, material.Value);
        e.Inventory.AddItem(EnhancementTransactions.StoneId, 20);
        return e;
    }
    public static void Run()
    {
        checks = 0;
        int[,] costs = { {200,400,700}, {600,1000,1600}, {1500,2500,4000}, {4000,7000,12000} };
        int[,] primary = { {3,5,8}, {4,6,9}, {3,5,8}, {2,3,5} };
        string[] mats = { "muc_an", "to_vang", "bot_tu_tinh", "huyet_trien", "tinh_hoa_di_bien" };
        for (int color = 0; color < 4; color++)
            for (int level = 0; level < 3; level++)
            {
                var e = Ready(level, (SpecialtyPaperColor)color);
                var q = EnhancementTransactions.Quote(e);
                Check(q.Gold == costs[color, level] && q.Materials[mats[color]] == primary[color, level], "Exact GDD primary material/gold recipe.");
                Check(level == 0 ? q.Materials.Count == 1 : q.Materials[mats[color + 1]] == level, "Exact GDD secondary material recipe.");
            }
        var ready = Ready();
        int gold = ready.Farm.Gold;
        ready.WorldLocations.CurrentLocationId = "Farm";
        Check(!EnhancementTransactions.TryEnhance(ready, 0).Attempted && ready.Farm.Gold == gold, "No global overview or outdoors enhancement.");
        ready.WorldLocations.CurrentLocationId = EnhancementTransactions.WorkshopLocationId;
        ready.WorldLocations.PlayerTileX += 10;
        Check(!EnhancementTransactions.TryEnhance(ready, 0).Attempted, "Must approach actual station.");
        ready.WorldLocations.PlayerTileX = EnhancementTransactions.StationTileX;
        ready.DeepMechanics.MidGameGateUnlocked = false;
        Check(!EnhancementTransactions.TryEnhance(ready, 0).Attempted, "Requires progression.");
        ready.DeepMechanics.MidGameGateUnlocked = true;
        ready.Clock.IsDuringCombatWave = true;
        Check(!EnhancementTransactions.TryEnhance(ready, 0).Attempted, "No workbench transaction during raid.");
        ready.Clock.IsDuringCombatWave = false;
        Check(!EnhancementTransactions.TryEnhance(ready, -1).Attempted && !EnhancementTransactions.TryEnhance(ready, 1).Attempted, "+1 requires no stones; reject invalid submissions.");
        ready.Farm.Gold = 199;
        Check(!EnhancementTransactions.TryEnhance(ready, 0).Attempted && ready.Inventory.CountId("muc_an") == 3 && ready.Farm.Gold == 199, "Unaffordable operation has no partial debit.");
        ready.Farm.Gold = 1000;
        ready.Inventory.ConsumeItem("Paper_Blue", 1);
        Check(!EnhancementTransactions.TryEnhance(ready, 0).Attempted && ready.Farm.Gold == 1000, "Equipped color flag is not paper ownership.");
        ready.Inventory.AddItem("Paper_Blue", 1, 2);
        var success = EnhancementTransactions.TryEnhance(ready, 0);
        Check(success.Attempted && success.Succeeded && success.NewLevel == 1 && ready.DeepMechanics.intEquippedPaperEnhancementLevel == 1, "Paid +1 succeeds.");
        Check(ready.Farm.Gold == 800 && ready.Inventory.CountId("muc_an") == 0 && ready.Inventory.CountId("Paper_Blue") == 1 && ready.Inventory.Slots.Any(item => item.QualifiedItemId == "(G)Paper_Blue" && item.Quality == 2), "Success pays exact cost and preserves owned paper quality.");
        Check(ready.Ledger.GetTotalSpentByReason(LedgerReason.EnhanceWorkshop) == 200, "Charge recorded once in real ledger.");

        var partial = Ready(1); partial.Inventory.ConsumeItem("to_vang", 1); gold = partial.Farm.Gold;
        Check(!EnhancementTransactions.TryEnhance(partial, 2).Attempted && partial.Farm.Gold == gold && partial.Inventory.CountId("muc_an") == 5 && partial.Inventory.CountId(EnhancementTransactions.StoneId) == 20, "Missing secondary ingredient never charges primary material, gold or stones.");
        partial.Inventory.AddItem("to_vang", 1);
        partial.Inventory.ConsumeItem(EnhancementTransactions.StoneId, 20);
        Check(!EnhancementTransactions.TryEnhance(partial, 2).Attempted && partial.Inventory.CountId("to_vang") == 1, "Unowned stones cause no ingredient debit.");

        bool sawFailure = false;
        for (int seed = 1; seed < 40 && !sawFailure; seed++)
        {
            var e = Ready(1, SpecialtyPaperColor.Lam, seed);
            var result = EnhancementTransactions.TryEnhance(e, 0);
            if (!result.Succeeded)
            {
                sawFailure = true;
                Check(result.Attempted && e.DeepMechanics.intEquippedPaperEnhancementLevel == 1 && e.Inventory.CountId("Paper_Blue") == 1, "Failed roll retains paper and level.");
                Check(e.Farm.Gold == 99600 && e.Inventory.CountId("muc_an") == 0 && e.Inventory.CountId("to_vang") == 0, "Failed roll still pays exact submitted recipe.");
            }
        }
        Check(sawFailure, "Genuine engine RNG can fail non-guaranteed enhancement.");
        var high = Ready(2, SpecialtyPaperColor.Do);
        var cap = EnhancementTransactions.TryEnhance(high, 8);
        Check(Math.Abs(cap.SuccessRate - 0.80) < 0.00001 && high.Inventory.CountId(EnhancementTransactions.StoneId) == 12, "+3 cap80%, consume eight owned stones.");
        Check(!EnhancementTransactions.TryEnhance(Ready(2), 9).Attempted, "Refuse more than eight stones, not silently clamp and charge.");
        var terminal = Ready(); terminal.DeepMechanics.intEquippedPaperEnhancementLevel = 3;
        Check(!EnhancementTransactions.TryEnhance(terminal, 0).Attempted, "No +4.");
        var invalid = Ready(); invalid.DeepMechanics.EquippedPaperColor = (SpecialtyPaperColor)17;
        Check(!EnhancementTransactions.TryEnhance(invalid, 0).Attempted && invalid.Farm.Gold == 100000, "Unknown paper color rejected without payment.");
        var absentStation = Ready(); absentStation.WorldLocations.GetLocation(EnhancementTransactions.WorkshopLocationId)!.Machines.Clear();
        Check(!EnhancementTransactions.TryEnhance(absentStation, 0).Attempted, "A location ID alone cannot fabricate a missing station.");
        var control = Ready(seed: 993); control.NextEnhancementRoll(); double expectedSecondRoll = control.NextEnhancementRoll();
        var sequence = Ready(seed: 993);
        int published = 0;
        using var subscription = sequence.EventBus.Subscribe<PaperEnhancedEvent>(evt => published++);
        sequence.WorldLocations.CurrentLocationId = "Farm";
        EnhancementTransactions.TryEnhance(sequence, 0);
        sequence.WorldLocations.CurrentLocationId = EnhancementTransactions.WorkshopLocationId;
        Check(EnhancementTransactions.TryEnhance(sequence, 0).Attempted && sequence.NextEnhancementRoll() == expectedSecondRoll, "Rejected request consumes no RNG; real attempt consumes one engine roll.");
        Check(published == 1, "Only a real paid attempt publishes the canonical enhancement event.");
        Console.WriteLine($"Enhancement transactions: {checks} assertions passed.");
    }
}
