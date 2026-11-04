using System;
using PigTycoon.Core;

internal static class DefenseRepairTests
{
    public static void Run()
    {
        var e = new GameEngine(77);
        var b = e.Defense.ConstructBuilding("NoXuyenVan").building!;
        b.Durability = 80;
        e.Farm.Gold = 10000;
        e.Inventory.AddItem("item_alloy_scrap", 100);
        int gold = e.Farm.Gold;
        e.WorldLocations.CurrentLocationId = "Market";
        Require(!DefenseRepairs.Request(e, b.InstanceId).success && e.Farm.Gold == gold, "Repair requires being on the farm (Phòng Xây Dựng).");
        PixelWorldRoutes.Register(e.WorldLocations);
        e.WorldLocations.CurrentLocationId = "Farm"; // the 2D farm map (3 Nov: "Farm_Core" never exists in play)
        e.Clock.IsDuringCombatWave = true;
        Require(!DefenseRepairs.Request(e, b.InstanceId).success, "No repair during raid.");
        e.Clock.IsDuringCombatWave = false;
        Require(!DefenseRepairs.Request(e, b.InstanceId).success && e.Farm.Gold == gold, "GDD 13.2: no NPC on duty, the hall does not repair.");
        e.PetsAndNpcs.HireNpc(NpcSpecialistId.NongCong); // any farm NPC staffs the hall at 40%
        var quote = DefenseRepairs.Quote(e, b);
        var request = DefenseRepairs.Request(e, b.InstanceId);
        Require(request.success && b.Durability == 80, "Paid order must not repair instantly.");
        Require(e.Farm.Gold == gold - quote.Gold && e.Inventory.CountId("item_alloy_scrap") == 100 - quote.Scrap, "Deduct quote once.");
        Require(!DefenseRepairs.Request(e, b.InstanceId).success, "No duplicate orders.");
        var saved = SaveSystem.Serialize(e.CreateSaveData());
        Require(SaveSystem.TryDeserialize(saved, out var data, out _), "Save repair queue.");
        var restored = new GameEngine(77);
        restored.RestoreFromSave(data!);
        DefenseRepairs.Advance(restored, quote.Minutes - 1);
        Require(restored.Defense.Buildings[0].Durability == 80, "Wait the full repair duration after restore.");
        DefenseRepairs.Advance(restored, 1);
        Require(restored.Defense.Buildings[0].Durability == 100 && restored.RepairOrders.Count == 0, "Complete queued repair.");
        foreach (var bp in e.Defense.GetAllBlueprints())
        {
            var tier = DefenseRepairs.Tier(bp);
            Require(bp.IsBachVan ? tier == 3 : tier < 3, "Bach Van is the highest repair tier.");
        }
    }
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
