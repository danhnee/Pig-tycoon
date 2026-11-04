#if CORRECTIVE_SAVE_TESTS
using PigTycoon.Core;

static class CorrectiveSaveTests
{
    static int checks;
    static void Require(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static int Main()
    {
        var engine = new GameEngine();
        engine.Inventory.AddItem("test_saved_ore", 5, 2);
        Require(engine.SpaceBag.TryWithdrawOne(engine.Inventory, engine.Inventory.Slots.ToList().FindIndex(i => i.ItemId == "test_saved_ore"), out var error), error);
        Require(engine.Inventory.CountId("test_saved_ore") == 4 && engine.SpaceBag.Count == 1, "warehouse and carried stock split");
        engine.ForestAndEvents.Expedition.TryBegin(MysteryEventId.E01_VuonMuoiHaiQua, 1);
        engine.ForestAndEvents.Expedition.RecordProgress(50);
        engine.WorldLocations.CurrentLocationId = "Forest_Primeval";
        var json = SaveSystem.Serialize(engine.CreateSaveData());
        Require(SaveSystem.TryDeserialize(json, out var saved, out error), error);
        var restored = new GameEngine();
        restored.RestoreFromSave(saved);
        Require(restored.Inventory.CountId("test_saved_ore") == 4, "warehouse not copied into bag");
        Require(restored.SpaceBag.Count == 1 && restored.SpaceBag.Items[0].Quality == 2, "bag quality and count survive JSON");
        Require(restored.ForestAndEvents.Expedition.IsActive && restored.ForestAndEvents.Expedition.CompletionPercent == 50, "event ownership and progress survive JSON");
        Require(restored.WorldLocations.CurrentLocationId == "Forest_Primeval", "valid expedition can resume forest");
        Require(restored.SpaceBag.TryReturnOne(restored.Inventory, 0, out error), error);
        Require(restored.Inventory.CountId("test_saved_ore") == 5 && restored.SpaceBag.Count == 0, "return after loading conserves inventory");

        var legacy = engine.CreateSaveData();
        legacy.SpaceBag = null;
        legacy.OccultExpedition = null;
        restored.RestoreFromSave(legacy);
        Require(restored.SpaceBag.Capacity == 3 && restored.SpaceBag.Count == 0, "old save gets empty basic bag");
        Require(restored.Inventory.CountId("test_saved_ore") == 4, "legacy warehouse remains intact");
        Require(restored.WorldLocations.CurrentLocationId == "Farm", "old always-open forest save evacuated to pasture");

        var invalid = engine.CreateSaveData();
        invalid.SpaceBag = new SpaceBagSnapshot { Tier = SpaceBagTier.Basic };
        for (int i = 0; i < 4; i++) invalid.SpaceBag.Items.Add(QualifiedItemRegistry.Create("test_oversized_bag"));
        Require(!SaveSystem.TryDeserialize(SaveSystem.Serialize(invalid), out _, out error) && !string.IsNullOrEmpty(error), "bad bag JSON rejected with explanation, not truncated");
        var before = SaveSystem.Serialize(restored.CreateSaveData());
        bool rejected = false;
        try { restored.RestoreFromSave(invalid); } catch (ArgumentException) { rejected = true; }
        Require(rejected, "direct restore rejects invalid bag");
        Require(SaveSystem.Serialize(restored.CreateSaveData()) == before, "bad restore cannot mutate current session");
        Console.WriteLine($"Corrective save integration: {checks} checks passed.");
        return 0;
    }
}
#endif
