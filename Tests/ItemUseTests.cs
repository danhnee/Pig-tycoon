using System;
using PigTycoon.Core;

/// <summary>GDD 12.8 farm / info / event items do what their table text says when used from the warehouse.</summary>
internal static class ItemUseTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static GameEngine EngineWithPig(out Pig pig)
    {
        var e = new GameEngine(11);
        pig = Pig.CreateDefault("item_pig", "Heo Thử", GeneLineId.HongDien, GeneRarity.Thuong, "Cai", 16, 95f);
        e.Pigs.Add(pig);
        return e;
    }

    public static void RunFarmItems()
    {
        var e = EngineWithPig(out var pig);
        Check(!ItemUse.Use(e, "CamLinhChi", pig.Id).ok, "an item not in the warehouse cannot be used");

        e.Inventory.AddItem("CamLinhChi", 2);
        pig.Mood = 50;
        var r = ItemUse.Use(e, "CamLinhChi", pig.Id);
        Check(r.ok && pig.Mood == 55 && pig.LinhChiFedDay == e.Clock.CurrentDay && e.Inventory.CountId("CamLinhChi") == 1, "Cám Linh Chi: +5 mood, marks the day, consumed");
        Check(!ItemUse.Use(e, "CamLinhChi", pig.Id).ok && e.Inventory.CountId("CamLinhChi") == 1, "one Cám Linh Chi per pig per day");

        e.Inventory.AddItem("ThuocAnThanHeo", 1);
        pig.SeizurePending = true; pig.Mood = 40;
        Check(ItemUse.Use(e, "ThuocAnThanHeo", pig.Id).ok && pig.Mood == 70 && !pig.SeizurePending, "sedative: +30 mood, stops the Lôi Tâm fit");

        e.Inventory.AddItem("XoNuoc", 1);
        Check(!ItemUse.Use(e, "XoNuoc", pig.Id).ok, "the bucket only helps a heat-struck pig");
        pig.ActiveDisease = DiseaseId.SayNang;
        float sta = e.Character.Stamina.CurrentStamina;
        float douse = FarmLaborStamina.Cost(e, FarmLaborTask.DouseBucket);
        Check(douse >= ItemUse.DouseStamina * 0.6f && douse <= ItemUse.DouseStamina * 2f, "douse cost is the GDD 9.2 base 3 through the 9.3 multipliers");
        Check(ItemUse.Use(e, "XoNuoc", pig.Id).ok && pig.ActiveDisease == null && e.Inventory.CountId("XoNuoc") == 1
              && Math.Abs(e.Character.Stamina.CurrentStamina - (sta - douse)) < 0.05f, "bucket: cures Say Nắng for the douse cost and is kept");

        int shelterCap = e.Farm.CalculateCapacityReport(1, 0).CapacityByShelters;
        e.Inventory.AddItem("MaiCheTam", 1);
        Check(ItemUse.Use(e, "MaiCheTam").ok && e.Farm.CalculateCapacityReport(1, 0).CapacityByShelters == shelterCap + 4, "Mái Che Tạm counts as a roof for 4 pigs");
        e.Tick(24 * 60);
        Check(e.Farm.TempShadePigsToday == 0, "Mái Che Tạm lasts one day");
    }

    public static void RunCalmFeedSoftensAgitation()
    {
        var e = EngineWithPig(out var pig);
        var herd = new System.Collections.Generic.List<Pig> { pig };
        pig.Mood = 80;
        HerdLifecycleRules.ApplyAgitation(pig, 10, herd, e.Clock.CurrentDay);
        int plain = 80 - pig.Mood;
        e.Inventory.AddItem("CamHaHoa", 1);
        Check(ItemUse.Use(e, "CamHaHoa", pig.Id).ok, "Cám Hạ Hỏa is usable on a pig");
        pig.Mood = 80;
        HerdLifecycleRules.ApplyAgitation(pig, 10, herd, e.Clock.CurrentDay);
        int calm = 80 - pig.Mood;
        Check(plain > 0 && calm == (int)Math.Round(plain * 0.8f), $"Cám Hạ Hỏa: mood loss −20 % that day ({plain} → {calm})");
        pig.Mood = 80;
        HerdLifecycleRules.ApplyAgitation(pig, 10, herd, e.Clock.CurrentDay + 1);
        Check(80 - pig.Mood == plain, "the calm feed wears off the next day");
    }

    public static void RunHeirTrainingNeedsLinhChi()
    {
        var e = EngineWithPig(out var pig);
        pig.Bonding = 80f; pig.Mood = 90;
        e.Herd.SelectSuccessionCandidate(pig);
        e.Herd.AdvanceDay(e.Pigs, e.Farm, 0, dayEnded: 1);
        Check(e.Herd.SuccessionTraining.BonusSuccessionPercentage == 0, "a day without Cám Linh Chi does not train the heir");
        pig.LinhChiFedDay = 2;
        e.Herd.AdvanceDay(e.Pigs, e.Farm, 0, dayEnded: 2);
        Check(e.Herd.SuccessionTraining.BonusSuccessionPercentage == 3, "a day with Cám Linh Chi trains +3 %");
    }

    public static void RunInfoAndEventItems()
    {
        var e = EngineWithPig(out _);
        e.Inventory.AddItem("TinDonCho", 1);
        var rumour = ItemUse.Use(e, "TinDonCho");
        Check(rumour.ok && rumour.message.Contains(MarketCycleThemes.NameVi(e.Economy.DayMarket.Theme)), "Tin Đồn Chợ names the running cycle theme");

        e.Inventory.AddItem("BanDoHuongQuai", 1);
        var map = ItemUse.Use(e, "BanDoHuongQuai");
        Check(map.ok && map.message.Contains("tới từ"), "Bản Đồ Hướng Quái names a direction: " + map.message);

        e.Inventory.AddItem("ChiaCongNguoc", 1);
        Check(!ItemUse.Use(e, "ChiaCongNguoc").ok && e.Inventory.CountId("ChiaCongNguoc") == 1, "the key needs an open gate and is kept when refused");

        e.Inventory.AddItem("LaDoSuong", 1);
        Check(ItemUse.Use(e, "LaDoSuong").ok && e.FogWarningUntilMinute >= e.Clock.TotalElapsedGameMinutes + 23 * 60, "Lá Dò Sương watches for 24 hours");

        var saved = e.CreateSaveData();
        var restored = new GameEngine(11);
        restored.RestoreFromSave(saved);
        Check(restored.FogWarningUntilMinute == e.FogWarningUntilMinute, "the fog watch survives save/load");
    }

    public static void RunQuickBagFromWarehouse()
    {
        var e = new GameEngine(seed: 20261031);
        e.Inventory.AddItem("BinhHoiLuc", 2);
        Check(!QuickBag.LoadFromWarehouse(e, QuickItemId.BinhHoiLuc).ok, "no bracelet: the remote link is closed");
        Check(QuickBag.LoadFromWarehouse(e, QuickItemId.BinhHoiLuc, inPerson: true).ok, "in person at the warehouse door works");
        e.Carry.NotifyHerdFed();
        e.Inventory.AddItem("starter_seed", 1);
        int idx = System.Linq.Enumerable.ToList(e.Inventory.Slots).FindIndex(i => i.ItemId == "starter_seed");
        Check(e.Carry.TryTakeToHand(idx, out var m) && e.Carry.TryDepositHand(out m) && e.Carry.HasBracelet, "bracelet granted");
        var r = QuickBag.LoadFromWarehouse(e, QuickItemId.BinhHoiLuc);
        Check(r.ok && QuickBag.Count(e.Character, QuickItemId.BinhHoiLuc) == 2 && !e.Inventory.ContainsId("BinhHoiLuc"), "the link moves warehouse bottles into the quick bag");
        Check(!QuickBag.LoadFromWarehouse(e, QuickItemId.BinhHoiLuc).ok, "an empty warehouse cannot load");
        e.Clock.IsDuringCombatWave = true;
        Check(!QuickBag.UnloadToWarehouse(e, QuickItemId.BinhHoiLuc).ok, "the link is closed during a wave");
        e.Clock.IsDuringCombatWave = false;
        Check(QuickBag.UnloadToWarehouse(e, QuickItemId.BinhHoiLuc).ok && e.Inventory.CountId("BinhHoiLuc") == 1, "unload returns one to the warehouse");
    }

    public static void RunHeatstrokeIncident()
    {
        var e = EngineWithPig(out var pig);
        pig.PositionX = 9000f; pig.PositionY = 9000f; // far from any mud, basin or roof
        pig.ImmuneDays = 0;
        e.ChangeWeather(WeatherType.NangGat);
        e.Tick(120);
        Check(pig.ActiveDisease == DiseaseId.SayNang && pig.IsAlive, "2 uncooled hours in Nắng Gắt: the pig collapses");
        e.Tick(60);
        Check(!pig.IsAlive, "collapsed 1 more hour without cooling: the pig dies (GDD 6.4)");

        var e2 = EngineWithPig(out var pig2);
        pig2.PositionX = 9000f; pig2.PositionY = 9000f; pig2.ImmuneDays = 0;
        e2.ChangeWeather(WeatherType.NangGat);
        e2.Tick(120);
        Check(pig2.ActiveDisease == DiseaseId.SayNang, "second pig collapses too");
        e2.Inventory.AddItem("MaiCheTam", 1);
        Check(ItemUse.Use(e2, "MaiCheTam", placeX: pig2.PositionX + 2f, placeY: pig2.PositionY).ok, "a temporary roof is raised next to it");
        e2.Tick(60);
        Check(pig2.IsAlive && pig2.ActiveDisease == null, $"a pig under Mái Che Tạm recovers instead of dying (alive {pig2.IsAlive}, {pig2.ActiveDisease}, collapsed {pig2.HeatCollapsedHours}h, shades {e2.GridZones.TempShades.Count}, pos {pig2.PositionX},{pig2.PositionY})");
        var re = new GameEngine(11); re.RestoreFromSave(e2.CreateSaveData());
        Check(re.GridZones.IsUnderTempShade(pig2.PositionX + 2f, pig2.PositionY), "the shade survives save/load for the day");
    }

    public static void RunLoiTamSeizure()
    {
        var e = EngineWithPig(out var pig);
        var near = Pig.CreateDefault("near", "Heo Gần", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 16, 95f);
        var far = Pig.CreateDefault("far", "Heo Xa", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 16, 95f);
        pig.PositionX = 0; pig.PositionY = 0; near.PositionX = 5; near.PositionY = 0; far.PositionX = 20; far.PositionY = 0;
        near.Mood = 80; far.Mood = 80;
        e.Pigs.Add(near); e.Pigs.Add(far);
        pig.ActiveDisease = DiseaseId.LoiTam;
        pig.Mood = 40;
        Check(!LoiTamSeizure.ShouldWarn(e, pig), "no warning while mood ≥ 30");
        pig.Mood = 20;
        Check(LoiTamSeizure.ShouldWarn(e, pig), "mood < 30: the 10 s warning starts");
        LoiTamSeizure.BeginWarning(pig);
        e.Inventory.AddItem("ThuocAnThanHeo", 1);
        Check(ItemUse.Use(e, "ThuocAnThanHeo", pig.Id).ok, "sedative during the warning");
        var calm = LoiTamSeizure.Resolve(e, pig, sheltered: false, roll: 0.0);
        Check(!calm.fit && pig.IsAlive && near.Mood == 80, "sedated in time: no fit");

        pig.Mood = 20;
        LoiTamSeizure.BeginWarning(pig);
        var roof = LoiTamSeizure.Resolve(e, pig, sheltered: true, roll: 0.0);
        Check(!roof.fit && pig.IsAlive, "herded under a roof in time: no fit");
        Check(!LoiTamSeizure.ShouldWarn(e, pig), "6 h cooldown after a warning");

        e.Tick(LoiTamSeizure.CooldownMinutes);
        pig.PositionX = 0; pig.PositionY = 0; near.PositionX = 5; near.PositionY = 0; far.PositionX = 20; far.PositionY = 0;
        near.Mood = 80; far.Mood = 80; pig.ActiveDisease = DiseaseId.LoiTam;
        pig.Mood = 20;
        Check(LoiTamSeizure.ShouldWarn(e, pig), "after the cooldown it can warn again");
        LoiTamSeizure.BeginWarning(pig);
        var fit = LoiTamSeizure.Resolve(e, pig, sheltered: false, roll: 0.9);
        Check(fit.fit && !fit.died && pig.IsAlive && near.Mood == LoiTamSeizure.PanicMood && far.Mood == 80 && fit.panicked == 1,
              "the fit panics pigs within 8 m and spares the rest; roll ≥ 25 % survives");
        pig.LastSeizureMinute = -1; pig.Mood = 20;
        LoiTamSeizure.BeginWarning(pig);
        Check(LoiTamSeizure.Resolve(e, pig, sheltered: false, roll: 0.1).died && !pig.IsAlive, "roll < 25 %: sudden death");
    }

    public static void RunHungerAndCarryRaiseStamina()
    {
        var e = new GameEngine(3);
        float fed = FarmLaborStamina.Cost(e, FarmLaborTask.CleanPlot);
        e.Tick(9 * 60);
        float baseNow = FarmLaborStamina.Cost(e, FarmLaborTask.CleanPlot);
        Check(e.Character.Stamina.HungerCostFactor(e.Clock.TotalElapsedGameMinutes) == StaminaSystem.HungryCostFactor, "9 h without a meal: hungry (+10 %)");
        Check(PlayerRestCommands.EatMeal(e, false).success, "eat a meal");
        float afterMeal = FarmLaborStamina.Cost(e, FarmLaborTask.CleanPlot);
        Check(afterMeal < baseNow, $"a meal removes the hunger cost ({baseNow} → {afterMeal})");
        e.Tick(17 * 60);
        Check(e.Character.Stamina.HungerCostFactor(e.Clock.TotalElapsedGameMinutes) == StaminaSystem.StarvingCostFactor, "17 h without a meal: +25 %");
        Check(Math.Abs(CarryCapacity.StaminaMultiplier(40f, 80) - 1.35f) < 1e-4f && Math.Abs(CarryCapacity.StaminaMultiplier(40f, 30) - 1.50f) < 1e-4f
              && CarryCapacity.StaminaMultiplier(20f, 10) == 1f, "carry > 35 kg: +35 %, +15 % more under Thể trạng 40");
        var re = new GameEngine(3); re.RestoreFromSave(e.CreateSaveData());
        Check(re.Character.Stamina.LastMealMinute == e.Character.Stamina.LastMealMinute, "last meal time survives save/load");
        Check(fed > 0f, "sanity");
    }
}
