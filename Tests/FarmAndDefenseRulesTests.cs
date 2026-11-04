using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>3 Nov batch 2: farm expansion (C-10), fire mode set before a wave, Bàn Chông charges, faint per GDD 8.2.</summary>
internal static class FarmAndDefenseRulesTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void RunExpansion()
    {
        var e = new GameEngine(31);
        e.Farm.Gold = 10000;
        int cap0 = e.Farm.CalculateCapacityReport(0, 0).EffectiveCapacity;
        Check(!FarmExpansion.ExpandLandPlot(e).ok, "level 1 allows the starting 12 plots only");
        e.Farm.AddFarmExp(100000);
        Check(FarmExpansion.PlotLimit(e.Farm.FarmLevel) > 12, "levels open more land");
        int plots = e.Farm.Infrastructure.LandPlots; int gold = e.Farm.Gold;
        Check(FarmExpansion.ExpandLandPlot(e).ok && e.Farm.Infrastructure.LandPlots == plots + 1 && e.Farm.Gold == gold - FarmExpansion.PlotGold, "a land plot costs 400G");
        Check(!FarmExpansion.BuildShelterRoof(e).ok, "a roof needs planks");
        e.Inventory.AddItem(FarmExpansion.WoodId, 100);
        int roofs = e.Farm.Infrastructure.Shelters;
        Check(FarmExpansion.BuildShelterRoof(e).ok && e.Farm.Infrastructure.Shelters == roofs + 1, "a roof is built");
        Check(e.Farm.CalculateCapacityReport(0, 0).EffectiveCapacity > cap0, "capacity is no longer frozen (shelters were the bottleneck)");
    }

    public static void RunDefenseRules()
    {
        var e = new GameEngine(32);
        var trap = e.Defense.ConstructBuilding("BanChong").building!;
        Check(trap.AmmoCurrent == SpikeTrapSpec.Charges && trap.Durability > SpikeTrapSpec.Charges, "Bàn Chông: charges are ammo, durability stays health");
        var nỏ = e.Defense.ConstructBuilding("NoXuyenVan").building!;
        Check(e.Defense.SetTowerFireMode(nỏ.InstanceId, TowerFireMode.TietKiem).success, "fire mode is set before the wave");
        e.Clock.IsDuringCombatWave = true;
        Check(!e.Defense.SetTowerFireMode(nỏ.InstanceId, TowerFireMode.TuDo).success && nỏ.FireMode == "TietKiem", "GDD 13: no fire-mode changes during a wave");
    }

    public static void RunFaint()
    {
        var e = new GameEngine(33);
        e.Farm.Gold = 1000; e.Character.Stamina.Condition = 15;
        e.Clock.IsDuringCombatWave = true;
        var r = e.ExecutePlayerFaintRescue("test");
        Check(e.Character.IsFainted && e.Farm.Gold == 900 && e.Character.Stamina.Condition == 0, "GDD 8.2/19: knocked out until the wave ends, −10% gold, Thể trạng −20 with no floor");
        e.Clock.IsDuringCombatWave = false;
        Check(e.WakeAfterWave().Length > 0 && !e.Character.IsFainted && e.Character.CurrentGuard > 0, "wakes at the main house after the wave");
    }
}
