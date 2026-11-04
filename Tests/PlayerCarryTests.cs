using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>ADR-0012: tool wheel, hand/bracelet/warehouse link, combat-only bag limit, starter quest, persistence.</summary>
internal static class PlayerCarryTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static GameEngine Fresh() => new GameEngine(seed: 20261031);

    private static int Idx(GameEngine e, string id) => e.Inventory.Slots.ToList().FindIndex(i => i.ItemId == id);

    private static void GrantBracelet(GameEngine e)
    {
        e.Carry.NotifyHerdFed();
        e.Inventory.AddItem("test_carry_seed", 1);
        Check(e.Carry.TryTakeToHand(e.Inventory.Slots.ToList().FindIndex(i => i.ItemId == "test_carry_seed"), out var m), m);
        Check(e.Carry.TryDepositHand(out m), m);
        Check(e.Carry.HasBracelet, "starter quest grants the bracelet");
    }

    public static void RunToolWheel()
    {
        var w = new ToolWheel();
        Check(w.Slots.Count == 6 && w.Slots.All(s => s.Length > 0), "wheel has exactly six equipped slots by default");
        Check(w.Slots.Distinct().Count() == 6 && w.Slots.All(ToolWheel.IsKnownTool), "default slots are distinct known tools");
        Check(w.SelectedToolId == "CamHat", "default selected tool drives actions");
        Check(w.Rotate(1) && w.SelectedToolId == "XoNuoc" && w.Rotate(-1) && w.SelectedToolId == "CamHat", "rotate cycles forward and back");
        Check(w.Rotate(-1) && w.SelectedIndex == 5, "rotate wraps");
        Check(w.StoredToolIds().Contains("HangRao") && !w.StoredToolIds().Contains("CamHat"), "storage side holds unequipped tools");
        Check(w.TryEquip(2, "HangRao", out _) && w.Slots[2] == "HangRao" && w.StoredToolIds().Contains("BuaGo"), "equip from storage replaces sector");
        Check(w.TryEquip(0, "DaoGo", out _) && w.Slots[0] == "DaoGo" && w.Slots[3] == "CamHat", "equipping an equipped tool swaps sectors");
        Check(!w.TryEquip(0, "NoSuchTool", out _) && !w.TryEquip(6, "CamHat", out _) && !w.TryEquip(-1, "CamHat", out _), "invalid equip refused");
        Check(w.TrySelect(2) && w.TryUnequip(2, out _) && w.Slots[2] == "" && w.SelectedIndex != 2 && w.SelectedToolId.Length > 0, "unequip moves selection off the empty sector");
        Check(!w.TrySelect(2), "cannot select an empty sector");
        for (int i = 0; i < 6; i++) w.TryUnequip(i, out _);
        Check(w.Slots.Count(s => s.Length > 0) == 1, "wheel keeps at least one tool");
        var snap = w.Capture(); var w2 = new ToolWheel(); w2.Restore(snap);
        Check(w2.SelectedToolId == w.SelectedToolId && w2.Slots.SequenceEqual(w.Slots), "wheel snapshot round-trips");
        var bad = new ToolWheelSnapshot { Slots = { "CamHat", "CamHat", "", "", "", "" } };
        var w3 = new ToolWheel(); w3.Restore(bad);
        Check(w3.Slots.SequenceEqual(ToolWheel.DefaultSlots), "duplicate/invalid wheel save falls back to default");
    }

    public static void RunBraceletAndWarehouse()
    {
        var e = Fresh();
        Check(!e.Carry.HasBracelet && e.Carry.HandItem == null, "starts pre-bracelet with empty hand");
        Check(!e.Carry.CanUseWarehouseLink(out _), "no warehouse link before the bracelet");
        Check(!e.Carry.TryWithdrawToBag(0, true, out _) && !e.Carry.TryReturnFromBag(0, true, out _), "no bag before the bracelet");

        Check(e.Carry.TryPickUp("test_carry_ore", 3, 0, out _) == CarryRules.HandStackLimit && e.Carry.HandItem != null, "hand takes one stack");
        Check(e.Carry.TryPickUp("test_carry_ore", 1, 0, out var full) == 0 && full.Length > 0, "full hand refuses a second pickup");
        Check(e.Inventory.CountId("test_carry_ore") == 0, "pickup does not teleport into the warehouse before the bracelet");
        Check(e.Carry.TryDepositHand(out _) && e.Carry.HandItem == null && e.Inventory.CountId("test_carry_ore") == 1, "deposit at the warehouse in person");
        Check(!e.Carry.HasBracelet && e.Carry.StarterDepositedItem && !e.Carry.StarterFedHerd, "one objective alone does not grant the bracelet");
        Check(!e.Carry.TryDepositHand(out _), "cannot deposit an empty hand");

        e.Carry.NotifyHerdFed();
        Check(e.Carry.HasBracelet && e.Carry.StarterQuestComplete, "feeding the herd completes the starter quest and grants the bracelet");
        Check(e.SpaceBag.Tier == SpaceBagTier.Basic && e.SpaceBag.Capacity == 3, "basic bracelet bag is 3");

        Check(e.Carry.CanUseWarehouseLink(out _), "link opens out of combat");
        Check(e.Carry.TryWithdrawToBag(Idx(e, "test_carry_ore"), false, out var m), m);
        Check(e.SpaceBag.Count == 1 && e.Inventory.CountId("test_carry_ore") == 0, "withdraw via link fills the bag");
        Check(e.Carry.TryReturnFromBag(0, false, out m) && e.SpaceBag.Count == 0, "return via link");
        Check(e.Carry.TryPickUp("test_carry_herb", 5, 0, out _) == 5 && e.Inventory.CountId("test_carry_herb") == 5 && e.SpaceBag.Count == 0, "out-of-combat pickups are not limited by the bag");

        int free = CarryRules.WarehouseLinkMaxSlots - e.Inventory.Slots.Count;
        for (int i = 0; i < free; i++) e.Inventory.AddItem("test_carry_fill_" + i);
        Check(e.Inventory.Slots.Count == 100, "warehouse at 100 slots");
        Check(e.Carry.TryPickUp("test_carry_new_kind", 1, 0, out _) == 0, "a 101st item type is refused");
        Check(e.Carry.TryPickUp("test_carry_herb", 1, 0, out _) == 1 && e.Inventory.CountId("test_carry_herb") == 6, "existing stacks still accept items at 100 slots");
        Check(e.Carry.TryWithdrawToBag(Idx(e, "test_carry_fill_0"), false, out _), "withdraw to bag frees a slot");
        Check(e.Inventory.Slots.Count == 99, "slot freed");
    }

    public static void RunCombatBag()
    {
        var e = Fresh(); GrantBracelet(e);
        for (int i = 0; i < 4; i++) e.Inventory.AddItem("test_carry_heal_" + i);
        e.Clock.IsDuringCombatWave = true;
        Check(e.Carry.InCombat && !e.Carry.CanUseWarehouseLink(out _), "warehouse link closed during a raid wave");
        Check(!e.Carry.TryWithdrawToBag(Idx(e, "test_carry_heal_0"), false, out _) && e.SpaceBag.Count == 0, "no remote withdraw in combat");
        Check(e.Carry.TryWithdrawToBag(Idx(e, "test_carry_heal_0"), true, out _), "in person at the warehouse door still allowed");
        for (int i = 0; i < 2; i++) Check(e.Carry.TryPickUp("test_carry_loot" + i, 1, 0, out _) == 1, "combat pickup fills bag");
        Check(e.SpaceBag.Count == 3 && e.Carry.TryPickUp("test_carry_loot9", 1, 0, out var msg) == 0 && msg.Length > 0, "3-item limit applies in combat");
        Check(!e.Carry.TryReturnFromBag(0, false, out _), "no remote return in combat");
        e.Clock.IsDuringCombatWave = false;
        Check(e.Carry.TryReturnFromBag(0, false, out _) && e.Carry.CanUseWarehouseLink(out _), "link reopens after the wave");

        var x = Fresh(); GrantBracelet(x);
        Check(x.ForestAndEvents.Expedition.TryBegin(MysteryEventId.E01_VuonMuoiHaiQua, 1), "expedition begins");
        Check(x.Carry.InCombat && !x.Carry.CanUseWarehouseLink(out _), "occult expedition closes the link");
        Check(x.Carry.TryPickUp("test_carry_loot", 1, 0, out _) == 1 && x.SpaceBag.Count == 1, "expedition pickup uses the bag");

        var snap = new SpaceBagSnapshot { Tier = SpaceBagTier.Advanced };
        var p = Fresh(); GrantBracelet(p);
        Check(p.SpaceBag.TryRestore(snap, out _) && p.SpaceBag.Capacity == 8, "premium capacity 8");
        p.Clock.IsDuringCombatWave = true;
        int got = 0; for (int i = 0; i < 12; i++) got += p.Carry.TryPickUp("test_carry_l" + i, 1, 0, out _);
        Check(got == 8 && p.SpaceBag.Count == 8, "premium bag stops at eight in combat");
    }

    /// <summary>
    /// GDD v6.0 L285 "mỗi loại tối đa 3 cái khi vào đợt quái" + Codex R4 P1: the 3-per-type cap counts every
    /// quality of one item together, so splitting qualities into separate stacks cannot carry more of one type.
    /// </summary>
    public static void RunBagTypeCap()
    {
        var p = Fresh(); GrantBracelet(p);
        Check(p.SpaceBag.TryRestore(new SpaceBagSnapshot { Tier = SpaceBagTier.Advanced }, out _), "premium bag");
        p.Clock.IsDuringCombatWave = true;
        int got = 0;
        foreach (var q in new[] { 0, 0, 1, 1, 2 }) got += p.Carry.TryPickUp("test_carry_bandage", 1, q, out _);
        Check(got == CarryRules.BagMaxPerType && p.SpaceBag.Count == 3, "quality splits cannot exceed three of one type");
        Check(p.Carry.TryPickUp("test_carry_tonic", 1, 0, out _) == 1, "a different type still fits");
        Check(p.SpaceBag.UsedSlots <= p.SpaceBag.Capacity, "slot budget respected");
    }

    public static void RunSaveLoad()
    {
        var e = Fresh();
        e.Carry.TryPickUp("test_carry_ore", 1, 1, out _);
        e.Carry.NotifyHerdFed();
        e.Carry.Wheel.TryEquip(1, "HangRao", out _); e.Carry.Wheel.TrySelect(3);
        Check(SaveSystem.TryDeserialize(SaveSystem.Serialize(e.CreateSaveData()), out var data, out var err), err);
        var r = Fresh(); r.RestoreFromSave(data!);
        Check(!r.Carry.HasBracelet && r.Carry.HandItem != null && r.Carry.HandItem.Quality == 1 && r.Carry.StarterFedHerd, "pre-bracelet state, hand item, objectives persist");
        Check(r.Carry.Wheel.Slots[1] == "HangRao" && r.Carry.Wheel.SelectedIndex == 3, "wheel persists");
        Check(r.Carry.TryDepositHand(out _) && r.Carry.HasBracelet, "finishing the quest after load grants the bracelet");
        Check(SaveSystem.TryDeserialize(SaveSystem.Serialize(r.CreateSaveData()), out data, out err), err);
        var r2 = Fresh(); r2.RestoreFromSave(data!);
        Check(r2.Carry.HasBracelet && r2.Carry.HandItem == null, "bracelet persists");

        var old = e.CreateSaveData(); old.PlayerCarry = null; old.SpaceBag = null;
        var o = Fresh(); o.RestoreFromSave(old);
        Check(!o.Carry.HasBracelet && o.Carry.HandItem == null && o.Carry.Wheel.Slots.SequenceEqual(ToolWheel.DefaultSlots), "old save: pre-bracelet, default wheel");
        var oldBag = Fresh(); oldBag.Inventory.AddItem("test_carry_ore", 2);
        Check(oldBag.SpaceBag.TryWithdrawOne(oldBag.Inventory, Idx(oldBag, "test_carry_ore"), out _), "legacy bag item");
        var legacy = oldBag.CreateSaveData(); legacy.PlayerCarry = null;
        var l = Fresh(); l.RestoreFromSave(legacy);
        Check(l.Carry.HasBracelet && l.SpaceBag.Count == 1, "old save that already carried bag items keeps access to them");
        var starterDone = e.CreateSaveData(); starterDone.PlayerCarry = new PlayerCarrySnapshot { StarterFedHerd = true, StarterDepositedItem = true };
        var s = Fresh(); s.RestoreFromSave(starterDone);
        Check(s.Carry.HasBracelet, "completed starter quest implies the bracelet");
    }
}
