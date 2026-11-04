using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 13–19: Build Room levels, per-structure data, power radius, durability, reload price, waves, monsters.</summary>
internal static class DefenseStructureTests
{
    public static void RunHallUpgrade()
    {
        var e = new GameEngine(501);
        Require(e.Defense.HallLevel == 1, "new farm starts with Phòng Xây Dựng 1");
        Require(e.Defense.GetBlueprint("ThapHoQuang")!.RequiredHallLevel == 2 && e.Defense.GetBlueprint("RadarUyHiep")!.RequiredHallLevel == 1
                && e.Defense.GetBlueprint("DaiThienLoi")!.RequiredHallLevel == 3 && e.Defense.GetBlueprint("CuuTieuLoiPhao")!.RequiredHallLevel == 3,
            "GDD 13.2 tiers: Tinh = cấp 1, Cao cấp = cấp 2, Bạch Vân/Thiên Lôi = cấp 3");
        e.Defense.UnlockedBlueprints.Add("ThapHoQuang");
        Require(!e.Defense.ConstructBuilding("ThapHoQuang").success, "level-2 blueprint locked at hall 1");
        e.Farm.Gold = 5999;
        e.Inventory.AddItem(ConstructionHall.SteelItemId, 40);
        Require(!ConstructionHall.TryUpgrade(e).success && e.Defense.HallLevel == 1, "6.000G needed");
        e.Farm.Gold = 6000;
        e.Clock.IsDuringCombatWave = true;
        Require(!ConstructionHall.TryUpgrade(e).success, "no upgrade during a wave");
        e.Clock.IsDuringCombatWave = false;
        var up = ConstructionHall.TryUpgrade(e);
        Require(up.success && e.Defense.HallLevel == 2 && e.Farm.Gold == 0 && e.Inventory.CountId(ConstructionHall.SteelItemId) == 0,
            "upgrade to 2 costs 6.000G + 40 thép: " + up.message);
        Require(e.Defense.ConstructBuilding("ThapHoQuang").success, "level-2 blueprint buildable after the upgrade");
        e.Farm.Gold = 18000;
        e.Inventory.AddItem(ConstructionHall.SteelItemId, 100);
        Require(!ConstructionHall.TryUpgrade(e).success, "level 3 also needs 2 Linh Kiện Tinh");
        e.Inventory.AddItem(DefenseRepairs.FineComponentId, 2);
        Require(ConstructionHall.TryUpgrade(e).success && e.Defense.HallLevel == 3 && e.Farm.Gold == 0, "upgrade to 3: 18.000G + 100 thép + 2 LKT");
        Require(!ConstructionHall.TryUpgrade(e).success, "cấp 3 is the maximum");

        e.Defense.UnlockedBlueprints.Add("CuuTieuLoiPhao");
        e.Farm.Gold = 50000;
        e.Farm.FarmLevel = 11;
        Require(!e.Defense.TryConstructBuilding("CuuTieuLoiPhao", e.Farm, e.Ledger).success, "Bạch Vân needs farm level 12");
        e.Farm.FarmLevel = 12;
        Require(e.Defense.TryConstructBuilding("CuuTieuLoiPhao", e.Farm, e.Ledger).success, "Bạch Vân at hall 3 + farm 12");
    }

    public static void RunAttackCapsAndSpecs()
    {
        var e = new GameEngine(502);
        Require(e.Defense.ConstructBuilding("NoXuyenVan").success && e.Defense.ConstructBuilding("NoXuyenVan").success, "two Nỏ at hall 1");
        Require(!e.Defense.ConstructBuilding("NoXuyenVan").success, "hall 1 allows only 2 Nỏ (GDD 13.2)");
        e.Defense.HallLevel = 2;
        Require(e.Defense.ConstructBuilding("NoXuyenVan").success && !e.Defense.ConstructBuilding("NoXuyenVan").success, "hall 2 allows 3 Nỏ");

        foreach (var bp in e.Defense.GetAllBlueprints())
            Require(DefenseStructureSpecs.Get(bp.Id) != null, "every blueprint has a GDD data row: " + bp.Id);
        var walls = new[] { ("RaoGo", 300), ("RaoThep", 1200), ("CongGiaCo", 4000), ("UBaoCat", 800) };
        e.Defense.HallLevel = 3;
        foreach (var bp in e.Defense.GetAllBlueprints()) if (!e.Defense.UnlockedBlueprints.Contains(bp.Id)) e.Defense.UnlockedBlueprints.Add(bp.Id);
        foreach (var (id, hp) in walls)
        {
            var w = e.Defense.ConstructBuilding(id).building!;
            Require(w.MaxHp == hp && w.CurrentHp == hp && w.AmmoCapacity == 0, $"{id} has {hp} HP and no ammo (blocks, never shoots)");
            Require(DefenseStructureSpecs.Get(id)!.Role == StructureRole.Barrier && DefenseStructureSpecs.Get(id)!.Damage == 0f, id + " is a barrier");
        }
        var arc = e.Defense.ConstructBuilding("ThapHoQuang").building!;
        var sky = e.Defense.ConstructBuilding("ThapPhongKhongSkyhook").building!;
        Require(arc.AmmoCapacity == 24 && Math.Abs(arc.CooldownSeconds - 2.2f) < 1e-4 && sky.AmmoCapacity == 30 && sky.AmmoCostPerShot == 3,
            "Hồ Quang 24 pin / 2,2 s; Skyhook 30 đạn, 3 mỗi loạt");
        var no = DefenseStructureSpecs.Get("NoXuyenVan")!;
        Require(no.Damage == 40f && no.Pierce == 2 && no.RangeM == 22f && no.PinSeconds == 0.5f && no.MagazineShots == 10 && no.MagazineReloadS == 4f,
            "Nỏ: 40/mũi, xuyên 2, 22 m, ghim 0,5 s, mỗi 10 mũi lên dây 4 s");
        Require(DefenseStructureSpecs.CanTarget(DefenseStructureSpecs.Get("ThapPhongKhongSkyhook")!, EnemyTypeId.UfoPhaoHam)
                && !DefenseStructureSpecs.CanTarget(DefenseStructureSpecs.Get("ThapPhongKhongSkyhook")!, EnemyTypeId.KeBatHeo)
                && !DefenseStructureSpecs.CanTarget(no, EnemyTypeId.UfoTrinhSat), "Skyhook air only, Nỏ ground only");
    }

    public static void RunPowerRadiusAndWires()
    {
        var e = new GameEngine(503);
        e.Defense.HallLevel = 2;
        e.Defense.UnlockedBlueprints.Add("ThapHoQuang");
        var arc = e.Defense.ConstructBuilding("ThapHoQuang").building!;
        arc.HasPosition = true; arc.PosX = 0f; arc.PosY = 0f;
        Require(e.Defense.NeedsPower(arc) && !e.Defense.IsOperational(arc), "a placed Hồ Quang with no generator in 12 m does not fire");
        var gen = e.Defense.ConstructBuilding("MayPhatDien").building!;
        gen.HasPosition = true; gen.PosX = 13f; gen.PosY = 0f;
        Require(!e.Defense.IsOperational(arc), "generator 13 m away is out of range");
        gen.PosX = 11f;
        Require(e.Defense.IsOperational(arc), "generator within 12 m powers the tower");
        var nỏ = e.Defense.ConstructBuilding("NoXuyenVan").building!;
        Require(!e.Defense.NeedsPower(nỏ) && e.Defense.IsOperational(nỏ), "Nỏ needs no power");
        Require(e.Defense.CutWireBranch("cutter1", arc) == 1 && !e.Defense.IsOperational(arc), "Kẻ Cắt Dây cuts the branch");
        Require(e.Defense.RestoreWiresCutBy("cutter1") == 1 && e.Defense.IsOperational(arc), "killing the cutter restores power");
        gen.IsBroken = true;
        Require(!e.Defense.IsOperational(arc), "a broken generator powers nothing");
    }

    public static void RunDurability()
    {
        var b = new DefenseBuildingInstance { BlueprintId = "NoXuyenVan", Durability = 100, MaxHp = 1500, CurrentHp = 1500 };
        for (int i = 0; i < 5; i++) DefenseDurability.ApplyUseWear(b, 0.4f);
        Require(b.Durability == 98 && b.DurabilityLostFiring == 2, "Nỏ wears 0,4 per arrow: 5 arrows = −2");
        DefenseDurability.ApplyHit(null, b, 250f, heavyBlow: false);
        Require(b.CurrentHp == 1250 && b.Durability == 96 && b.DurabilityLostHits == 2, "every 100 HP taken = −1 durability");
        DefenseDurability.ApplyHit(null, b, 50f, heavyBlow: true);
        Require(b.Durability == 92, "50 more HP completes a 100 block (−1) and the heavy blow costs −3");
        Require(DefenseDurability.StateOf(61) == DurabilityState.Tot && DefenseDurability.StateOf(60) == DurabilityState.Mon
                && DefenseDurability.StateOf(30) == DurabilityState.NguyKich && DefenseDurability.StateOf(0) == DurabilityState.Hong, "GDD 18 bands");
        Require(DefenseDurability.CooldownMultiplier(DurabilityState.Mon) == 1.05f && DefenseDurability.JamChance(DurabilityState.Mon) == 0.03f
                && DefenseDurability.CooldownMultiplier(DurabilityState.NguyKich) == 1.15f && DefenseDurability.DamageMultiplier(DurabilityState.NguyKich) == 0.9f
                && DefenseDurability.BachVanFailChance(DurabilityState.NguyKich) == 0.05f, "Mòn/Nguy kịch effects");
        DefenseDurability.ApplyHit(null, b, 5000f, false);
        Require(b.IsBroken && b.CurrentHp == 0, "HP 0 = Hỏng at once");
        var c = new DefenseBuildingInstance { BlueprintId = "ThapHoQuang", Durability = 8 };
        DefenseDurability.ApplyLightning(c);
        Require(c.Durability == 0 && c.IsBroken, "lightning −10");
    }

    public static void RunReloadCostAndShield()
    {
        var e = new GameEngine(504);
        var b = e.Defense.ConstructBuilding("NoXuyenVan").building!;
        b.AmmoCurrent = 20;
        e.Farm.Gold = 1000;
        e.Clock.IsDuringCombatWave = true;
        Require(!DefenseReloads.Reload(e, b.InstanceId).success, "no reload during a wave");
        e.Clock.IsDuringCombatWave = false;
        e.Inventory.AddItem("TenThep", 10);
        var r = DefenseReloads.Reload(e, b.InstanceId);
        Require(r.success && r.goldSpent == 120 && e.Farm.Gold == 880 && b.AmmoCurrent == 50 && e.Inventory.CountId("TenThep") == 0,
            "30 arrows: 10 from stock + 20 × 6G = 120G: " + r.message);
        e.Defense.HallLevel = 2;
        e.Defense.UnlockedBlueprints.Add("MayCongHuongHuyenBi");
        var res = e.Defense.ConstructBuilding("MayCongHuongHuyenBi").building!;
        res.AmmoCurrent = 0;
        Require(!DefenseReloads.Reload(e, res.InstanceId).success && e.Farm.Gold == 880, "unpriced fuel is not sold for gold at the farm");

        e.Defense.UnlockedBlueprints.Add("MayChieuKhien");
        var shield = e.Defense.ConstructBuilding("MayChieuKhien").building!;
        Require(shield.ShieldPoints == 1500 && shield.AmmoCurrent == 3, "a projector starts with one 1.500-point pack up");
        shield.HasPosition = true; shield.IsPowered = true;
        var gen = e.Defense.ConstructBuilding("MayPhatDien").building!; gen.HasPosition = true;
        var wall = e.Defense.ConstructBuilding("RaoGo").building!; wall.HasPosition = true; wall.PosX = 5f;
        DefenseDurability.ApplyHit(e.Defense, wall, 1600f, false);
        Require(wall.CurrentHp == 300 && shield.ShieldPoints == 1400 && shield.AmmoCurrent == 2, "the shield takes the blow, a second pack opens");
    }

    public static void RunPigDamageAndWaveDebrief()
    {
        var e = new GameEngine(505);
        var pig = Pig.CreateDefault("p1", "Heo Thử", GeneLineId.HongDien, GeneRarity.Thuong);
        e.Pigs.Add(pig);
        int died = 0, started = 0, ended = 0;
        e.EventBus.Subscribe<PigDied>(_ => died++);
        e.EventBus.Subscribe<WaveStarted>(_ => started++);
        e.EventBus.Subscribe<WaveEnded>(_ => ended++);
        var nỏ = e.Defense.ConstructBuilding("NoXuyenVan").building!;
        WaveDebrief.Begin(e, 1, 120, false);
        Require(started == 1 && e.Clock.IsDuringCombatWave, "WaveStarted reaches the Core EventBus and freezes the clock");
        Require(!MonsterPigDamage.Apply(e, pig, 60f, "Lính") && pig.Health == 40 && pig.IsAlive, "monster damage lowers pig Health");
        Require(MonsterPigDamage.Apply(e, pig, 50f, "Lính") && !pig.IsAlive && died == 1 && e.Defense.PigsLostThisWave == 1, "Health 0 kills the pig (PigDied)");
        nỏ.AmmoCurrent = 30; nỏ.AmmoUsedThisWave = 20;
        int gold = e.Farm.Gold;
        var report = WaveDebrief.Close(e, 1, true, 4, 95);
        Require(ended == 1 && !e.Clock.IsDuringCombatWave, "WaveEnded runs Core's wave-end hooks");
        Require(report.AmmoUsed == 20 && report.ArrowsRecovered == 6 && e.Inventory.CountId("TenThep") == 6, "30% of the arrows come back");
        Require(report.GoldReward == 95 && e.Farm.Gold == gold + 95 && report.PigsLost == 1, "reward from the kills, pigs lost counted");
        Require(report.ReloadGoldEstimate == 20 * 6 - 6 * 6 && e.DeepMechanics.BattleReports.Count > 0 && report.ToVietnamese().Contains("BÁO CÁO SAU ĐỢT"),
            "report estimates reloading (stock first) and is recorded");
    }

    public static void RunSaveAndMonsters()
    {
        var e = new GameEngine(506);
        var b = e.Defense.ConstructBuilding("NoXuyenVan").building!;
        b.HasPosition = true; b.PosX = 41f; b.PosY = 57f;
        Require(SaveSystem.TryDeserialize(SaveSystem.Serialize(e.CreateSaveData()), out var data, out _), "save");
        var r = new GameEngine(506);
        r.RestoreFromSave(data!);
        var rb = r.Defense.Buildings.Single(x => x.InstanceId == b.InstanceId);
        Require(rb.HasPosition && rb.PosX == 41f && rb.PosY == 57f, "structure position survives a save/load");

        Require(EnemyData.CreateByType(EnemyTypeId.ThietGiapCongThanh).MaxHp == 1100f && EnemyData.CreateByType(EnemyTypeId.UfoPhaoHam).MaxHp == 1900f
                && EnemyData.CreateByType(EnemyTypeId.ChoSuong).MaxHp == 420f && EnemyData.CreateByType(EnemyTypeId.BongHuyen).MaxHp == 500f
                && EnemyData.CreateUfoMothership(1).MaxHp == 20000f, "GDD 19 HP: 1.100 / 1.900 / 420 / 500 / Mẫu Hạm 20.000");

        e.Defense.AddContaminationZone(10f, 10f);
        PlagueZones.Advance(e, 11 * 60);
        Require(!e.Defense.PlagueContaminatedPen, "under 12 h nothing yet");
        PlagueZones.Advance(e, 60);
        Require(e.Defense.PlagueContaminatedPen && e.Farm.IsBarnContaminated && e.Defense.ContaminationZones.Count == 0, "12 h unlimed = Chuồng Nhiễm");
    }

    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
