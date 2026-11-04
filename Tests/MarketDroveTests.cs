using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>C-01/C-02: pigs are driven to the day market to be sold; bought pigs come home with the drove.</summary>
internal static class MarketDroveTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static GameEngine InteractiveFarm()
    {
        var e = new GameEngine(21);
        var map = new PlayableMapConfiguration { ConfigurationId = "drove-test", MapX = -60f, MapY = -45f, MapWidth = 120f, MapHeight = 90f,
            PastureX = -20f, PastureY = -14f, PastureWidth = 40f, PastureHeight = 28f, RefugeX = 5f, RefugeY = 6f, InteractiveHerdOwnership = true };
        e.ConfigurePlayableMap(map);
        PixelWorldRoutes.Register(e.WorldLocations);
        e.WorldLocations.CurrentLocationId = "Farm";
        e.Pigs.Add(Pig.CreateDefault("adult", "Heo Lớn", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 16, 100f));
        e.Pigs.Add(Pig.CreateDefault("piglet", "Heo Con", GeneLineId.LamKhe, GeneRarity.Thuong, "Cai", 2, 6f));
        var sow = Pig.CreateDefault("sow", "Nái", GeneLineId.HongDien, GeneRarity.Thuong, "Cai", 16, 100f); sow.GestationDaysRemaining = 2; sow.Mood = 80;
        e.Pigs.Add(sow);
        e.Tick(4 * 60); // 10:00
        return e;
    }

    public static void RunDriveAndSell()
    {
        var e = InteractiveFarm();
        Check(!e.SellPigAtDayMarket("adult").success, "a pig still on the farm cannot be sold");
        Check(e.Drove.Consign(e, "adult").ok && e.Drove.Consign(e, "piglet").ok && e.Drove.Consign(e, "sow").ok, "pigs join the drove at the farm");
        Check(!e.Drove.Consign(e, "adult").ok, "no double entry");
        float stamina = e.Character.Stamina.CurrentStamina;
        var (cost, adults, piglets) = e.Drove.DriveCost(e);
        Check(adults == 2 && piglets == 1 && Math.Abs(cost - (10f * MarketDrove.DroveTravelMinutes + 3f * MarketDrove.DroveTravelMinutes)) < 1e-3f,
            "GDD 9.2: driving adults 10/min + carrying each piglet 3/min");
        int sowMood = e.Pigs.First(p => p.Id == "sow").Mood;
        var warp = e.WorldLocations.TryWarpPlayer("MarketTown", 0, 0, e);
        Check(warp.warped && e.Drove.AtMarket, "the drove travels with the player");
        Check(e.Character.Stamina.CurrentStamina < stamina, "the drive costs stamina");
        Check(e.Pigs.First(p => p.Id == "sow").Mood == Math.Max(0, sowMood - 20), "GDD 4.4: a pregnant sow on the road gets stress +20");
        var sale = e.SellPigAtDayMarket("adult");
        Check(sale.success && !e.Pigs.Exists(p => p.Id == "adult") && !e.Drove.Contains("adult"), "a pig in the drove sells at the market and leaves the herd");
        Check(e.WorldLocations.TryWarpPlayer("Farm", 0, 0, e).warped, "back to the farm");
        Check(!e.Drove.AtMarket && e.Drove.PigIds.Count == 0, "unsold pigs come home with the drove");
        var back = e.Pigs.First(p => p.Id == "piglet");
        Check(Math.Abs(back.PositionX - 5f) < 1e-3f && Math.Abs(back.PositionY - 6f) < 1e-3f, "they walk back in by the shelter");
        var save = e.CreateSaveData(); var re = new GameEngine(); re.RestoreFromSave(save);
        Check(!re.Drove.AtMarket, "drove state is saved");
    }

    public static void RunBuyPigs()
    {
        var e = InteractiveFarm();
        var offers = MarketPigShop.TodayOffers(e);
        Check(offers.Count >= MarketPigShop.MinOffers && offers.Count <= MarketPigShop.MaxOffers, "3–5 pigs on sale a day");
        Check(MarketPigShop.TodayOffers(e) == offers && offers.All(o => o.PriceGold > 0), "the same list all day, priced");
        Check(!MarketPigShop.Buy(e, offers[0].OfferId).ok, "pigs are bought at the market town, not from the farm");
        e.WorldLocations.TryWarpPlayer("MarketTown", 0, 0, e);
        long gold = e.Farm.Gold;
        var r = MarketPigShop.Buy(e, offers[0].OfferId);
        Check(r.ok && e.Farm.Gold == gold - offers[0].PriceGold && offers[0].Sold, "buying costs the price");
        var bought = e.Pigs.Last();
        Check(bought.IsIdentified == PigText.LineVisible(offers[0].Rarity, false) && e.Drove.IsAway(bought.Id), "a bought rare pig is a '?' (Thường/Khá known) and travels home with the drove");
        Check(!MarketPigShop.Buy(e, offers[0].OfferId).ok, "a sold offer is gone");
        e.WorldLocations.TryWarpPlayer("Farm", 0, 0, e);
        Check(!e.Drove.IsAway(bought.Id) && e.Pigs.Contains(bought), "it arrives on the farm");
    }
}
