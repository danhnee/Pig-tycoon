using System;
using System.Collections.Generic;
using PigTycoon.Core;

internal static class SpaceBagTests
{
#if SPACE_BAG_TEST_RUNNER
    public static void Main() => Run();
#endif
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    public static void Run()
    {
        assertions = 0;
        var warehouse = new UnifiedInventory();
        warehouse.AddItem("CamThuong", 10, 2);
        warehouse.AddItem("CamThuong", 6, 0);
        var bag = new SpaceBag();
        Check(bag.Capacity == 3 && bag.Count == 0 && bag.Tier == SpaceBagTier.Basic, "Basic bag starts empty, capacity three individual items.");
        for (int i = 0; i < 3; i++) Check(bag.TryWithdrawOne(warehouse, 0, out _), "Withdraw one owned item.");
        Check(bag.Count == 3 && warehouse.Slots[0].Stack == 7 && warehouse.Slots[1].Stack == 6, "Identical items each occupy capacity; withdraw preserves other quality.");
        Check(!bag.TryWithdrawOne(warehouse, 0, out _) && warehouse.CountId("CamThuong") == 13, "Full bag does not debit warehouse.");
        Check(bag.Items[0].Quality == 2 && bag.Items[0].Stack == 1, "Keep quality, one item per carried instance.");
        var external = bag.Items[0]; external.Stack = 100; external.Quality = 99;
        Check(bag.Items[0].Stack == 1 && bag.Items[0].Quality == 2, "Items getter cannot mutate bag state.");
        Check(bag.TryReturnOne(warehouse, 0, out _) && bag.Count == 2 && warehouse.Slots[0].Stack == 8 && warehouse.Slots[1].Stack == 6, "Return preserves quality and conservation.");
        Check(bag.TryConsumeOne(0, out var used, out _) && used!.Quality == 2 && used.Stack == 1 && bag.Count == 1 && warehouse.CountId("CamThuong") == 14, "Consume exactly one carried item, not warehouse stock.");
        Check(!bag.TryConsumeOne(-1, out _, out _) && !bag.TryReturnOne(warehouse, 99, out _) && !bag.TryWithdrawOne(warehouse, 99, out _), "Invalid indexes cause no mutation.");

        QualifiedItemRegistry.Register("O", "BagTestEvidence", "Evidence", "", "Relic", 0, 0, "story_quest_item");
        warehouse.AddItem("BagTestEvidence", 1);
        Check(!bag.TryWithdrawOne(warehouse, warehouse.Slots.Count - 1, out _) && warehouse.CountId("BagTestEvidence") == 1, "Registry-tagged story evidence stays outside bag.");
        string canonicalEvidence = string.Empty;
        foreach (var definition in QualifiedItemRegistry.GetAllDefinitions())
            if (definition.ItemId != "BagTestEvidence" && ItemContextTagManager.HasBaseTag(definition.QualifiedItemId, "story_quest_item"))
            { canonicalEvidence = definition.QualifiedItemId; break; }
        Check(canonicalEvidence.Length > 0, "Canonical catalog defines story evidence tags.");
        warehouse.AddItem(canonicalEvidence);
        Check(!bag.TryWithdrawOne(warehouse, warehouse.Slots.Count - 1, out _), "Canonical story evidence cannot consume bag capacity.");

        var snapshot = bag.Capture();
        var restored = new SpaceBag();
        Check(restored.TryRestore(snapshot, out _) && restored.Count == 1 && restored.Items[0].Quality == 2, "Restore actual carried items and quality.");
        snapshot.Items[0].Quality = 87;
        Check(restored.Items[0].Quality == 2 && bag.Items[0].Quality == 2, "Snapshot and restore use deep copies.");
        var clone = restored.Capture().Clone(); clone.Items.Clear();
        Check(restored.Count == 1, "Snapshot Clone is independent.");
        var advanced = new SpaceBagSnapshot { Tier = SpaceBagTier.Advanced };
        for (int i = 0; i < 8; i++) advanced.Items.Add(QualifiedItemRegistry.Create("CamThuong", 1, i % 3));
        Check(restored.TryRestore(advanced, out _) && restored.Capacity == 8 && restored.Count == 8, "Explicit saved advanced tier holds eight total items.");
        Check(!restored.TryWithdrawOne(warehouse, 0, out _), "Advanced cannot carry ninth item.");
        advanced.Items.Add(QualifiedItemRegistry.Create("CamThuong"));
        Check(!restored.TryRestore(advanced, out _) && restored.Count == 8, "Over-capacity restore refuses without dropping existing items.");
        var stacked = new SpaceBagSnapshot(); stacked.Items.Add(QualifiedItemRegistry.Create("CamThuong", 100));
        Check(!restored.TryRestore(stacked, out _) && restored.Count == 8, "Save cannot bypass individual-item capacity with stacks.");
        var evidence = new SpaceBagSnapshot(); evidence.Items.Add(QualifiedItemRegistry.Create("BagTestEvidence"));
        Check(!restored.TryRestore(evidence, out _) && restored.Count == 8, "Save cannot put permanent story evidence into bag.");
        Check(!restored.TryRestore(new SpaceBagSnapshot { Tier = (SpaceBagTier)44 }, out _), "Undefined tiers rejected.");
        Check(!restored.TryRestore(new SpaceBagSnapshot { Items = null! }, out _) && restored.Count == 8, "Malformed null collection refuses without loss.");
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(restored.Capture());
        var fromJson = new SpaceBag();
        Check(fromJson.TryRestore(Newtonsoft.Json.JsonConvert.DeserializeObject<SpaceBagSnapshot>(serialized), out _) && fromJson.Count == 8 && fromJson.Tier == SpaceBagTier.Advanced && fromJson.Items[2].Quality == 2, "JSON round trip preserves tier, contents and qualities.");
        Check(restored.TryRestore(null, out _) && restored.Count == 0 && restored.Capacity == 3 && warehouse.CountId("CamThuong") == 14, "Legacy null data yields empty basic bag without moving warehouse items.");

        var farm = new Farm(); var stocks = new UnifiedInventory(); stocks.BindFarm(farm);
        stocks.AddItem("CamThuong", 2, 1); float feed = farm.FeedStockKg;
        Check(restored.TryWithdrawOne(stocks, 0, out _) && Math.Abs(farm.FeedStockKg - (feed - 3.5f)) < 0.01f, "Withdraw updates bound warehouse stocks once.");
        Check(restored.TryReturnOne(stocks, 0, out _) && Math.Abs(farm.FeedStockKg - feed) < 0.01f, "Return updates bound warehouse stocks once.");
        var custom = stocks.Slots[0].Clone(); custom.DisplayName = "Owned named item"; stocks.ClearAndRestore(new[] { custom });
        Check(restored.TryWithdrawOne(stocks, 0, out _) && restored.Items[0].DisplayName == custom.DisplayName, "Withdrawal preserves instance identity metadata.");
        Check(restored.TryReturnOne(stocks, 0, out _) && stocks.Slots[0].DisplayName == custom.DisplayName, "Return preserves warehouse identity metadata.");
        var ordinary = custom.Clone(1); ordinary.DisplayName = "Ordinary";
        Check(restored.TryWithdrawOne(stocks, 0, out _), "Carry named item before returning beside a differently named stack.");
        stocks.ClearAndRestore(new[] { ordinary });
        Check(restored.TryReturnOne(stocks, 0, out _) && stocks.Slots.Count == 2 && stocks.Slots[0].DisplayName == "Ordinary" && stocks.Slots[1].DisplayName == custom.DisplayName, "Return keeps differently named owned instances separate without losing identity.");
        var fullStack = QualifiedItemRegistry.Create("CamThuong", int.MaxValue, 1); stocks.ClearAndRestore(new[] { fullStack });
        Check(restored.TryRestore(new SpaceBagSnapshot { Items = new List<UnifiedItemInstance> { custom.Clone(1) } }, out _), "Prepare overflow boundary.");
        Check(!restored.TryReturnOne(stocks, 0, out _) && restored.Count == 1 && stocks.Slots[0].Stack == int.MaxValue, "Warehouse stack overflow refuses atomically.");
        Console.WriteLine($"SpaceBag: {assertions} assertions passed.");
    }
}
