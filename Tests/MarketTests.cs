using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 20: day market sells pigs for gold; night market trades the day's slots, gold only on the ~18% gold slots.</summary>
internal static class MarketTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static void AdvanceToHour(GameEngine e, int hour)
    {
        for (int guard = 0; guard < 48 && e.Clock.CurrentHour != hour; guard++) e.Tick(60);
    }

    public static void RunNightMarketCounter()
    {
        var e = new GameEngine(seed: 20261101);
        e.Farm.AddGold(20000);
        e.Village.RefreshStockForDay(e.Clock.CurrentDay);
        var night = e.Village.GetVillager(VillagerId.ChoDem);
        Check(night != null && night.Listings.Count == 8, "the night market shows the day's 8 slots");
        Check(ReferenceEquals(night!.Listings, e.Economy.NightListings), "the counter sells the live EconomyManager slots");
        var anySlot = night.Listings[0];
        Check(!e.TradeWithVillager(VillagerId.ChoDem, anySlot.SlotId, null, null, payWithGold: anySlot.AcceptsGold).success,
            "closed during the day (opens 18:00)");

        AdvanceToHour(e, 20);
        e.Village.RefreshStockForDay(e.Clock.CurrentDay);
        night = e.Village.GetVillager(VillagerId.ChoDem)!;
        var noGold = night.Listings.FirstOrDefault(l => !l.AcceptsGold && !l.IsSoldOut);
        if (noGold != null)
            Check(!e.TradeWithVillager(VillagerId.ChoDem, noGold.SlotId, null, null, payWithGold: true).success,
                "a barter-only slot refuses gold");
        var gold = night.Listings.FirstOrDefault(l => l.AcceptsGold && !l.IsSoldOut);
        if (gold != null)
        {
            int before = e.Farm.Gold;
            var res = e.TradeWithVillager(VillagerId.ChoDem, gold.SlotId, null, null, payWithGold: true);
            Check(res.success, "a gold slot sells for gold at night: " + res.message);
            Check(e.Farm.Gold == before - gold.GoldPrice && gold.IsSoldOut, "gold paid and the slot is gone");
            bool isGear = EquipmentAndTacticalCatalog.Find(res.item!.Id) != null;
            Check(isGear ? e.Character.OwnedEquipment.Contains(res.item.Id) : e.Inventory.CountId(res.item.Id) >= 1,
                "the bought item is in the warehouse (gear: in the owned equipment)");
        }
        int goldSlots = 0, all = 0;
        for (int seed = 1; seed <= 60; seed++)
        {
            var m = new EconomyManager(seed);
            m.GenerateNightMarket(1);
            goldSlots += m.NightListings.Count(l => l.AcceptsGold);
            all += m.NightListings.Count;
        }
        double share = goldSlots / (double)all;
        Check(share > 0.10 && share < 0.27, $"about 18% of night slots take gold (measured {share:P0})");
    }

    public static void RunDayMarketSell()
    {
        var e = new GameEngine(seed: 7);
        var pig = Pig.CreateDefault("p_sell", "Heo bán", GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 20, initialWeight: 90f);
        pig.Stage = PigStage.TruongThanh;
        e.Pigs.Add(pig);
        var (price, _) = e.Economy.CalculatePigSalePrice(pig, e.Farm.FarmLevel);
        int before = e.Farm.Gold;
        var res = e.SellPigAtDayMarket(pig.Id, isQuarantined: true);
        Check(res.success && price > 0, "an adult pig sells at the day market: " + res.message);
        Check(e.Farm.Gold == before + res.goldEarned && !e.Pigs.Contains(pig), "gold in, pig out of the herd");
        Check(res.goldEarned == Math.Max(10, price - e.Economy.DayMarket.QuarantineFeePerPig), "quarantine fee deducted");
    }
}
