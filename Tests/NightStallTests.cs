using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>Night auction (blueprints, sealed bid) and pets at the night market (traded for adult pigs).</summary>
internal static class NightStallTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static void ToHour(GameEngine e, int hour) { for (int g = 0; g < 48 && e.Clock.CurrentHour != hour; g++) e.Tick(60); }

    public static void RunAuction()
    {
        var e = new GameEngine(seed: 11);
        e.Farm.AddGold(200000);
        Check(!e.Auction.Bid(e, 0, 999999).won, "no auction below farm level 8");
        while (e.Farm.FarmLevel < NightAuction.UnlockFarmLevel) e.Farm.AddFarmExp(5000);
        e.DeepMechanics.MidGameGateUnlocked = true; // GDD 13.1: Cao cấp blueprint lots come after the mid-game gate
        ToHour(e, 20);
        e.Auction.EnsureLots(e);
        // 3 Nov (C-08): item lots (papers, stones, Linh Kiện Tinh, Bí Kíp) may join; blueprints and gear keep their counts
        Check(e.Auction.Lots.Count(l => !l.IsGear && !l.IsItem) == NightAuction.LotsPerNight && e.Auction.Lots.Count(l => l.IsGear) == 1,
            "three blueprint lots and one quality III–IV gear lot a night");
        Check(e.Auction.Lots.All(l => !e.Defense.UnlockedBlueprints.Contains(l.BlueprintId)), "lots are blueprints the farm lacks");
        var lot = e.Auction.Lots[0];
        Check(lot.RivalBidGold >= lot.ReserveGold && lot.RivalBidGold <= lot.ReserveGold * 1.5 + 1, "rival bids 100-150% of reserve");
        Check(!e.Auction.Bid(e, 0, lot.ReserveGold - 1).won, "below reserve is refused");
        int gold = e.Farm.Gold;
        var win = e.Auction.Bid(e, 0, (int)Math.Ceiling(lot.ReserveGold * 1.5) + 1);
        Check(win.won && e.Defense.UnlockedBlueprints.Contains(lot.BlueprintId), "a top bid wins and teaches the blueprint: " + win.message);
        Check(e.Farm.Gold < gold, "the winner pays");
        Check(!e.Auction.Bid(e, 0, 999999).won, "a lot is sold once");
    }

    public static void RunPets()
    {
        var e = new GameEngine(seed: 12);
        Check(!PetMarket.Adopt(e, PetId.MeoKho).success, "no pet slot before farm level 3");
        while (e.Farm.FarmLevel < 3) e.Farm.AddFarmExp(500);
        ToHour(e, 10);
        Check(!PetMarket.Adopt(e, PetId.MeoKho).success, "pets are traded at the night market only");
        ToHour(e, 20);
        for (int i = 0; i < 4; i++)
            e.Pigs.Add(new Pig { Id = "PET_PAY_" + i, Name = "Heo " + i, Stage = PigStage.TruongThanh, WeightKg = 95f, GeneLine = GeneLineId.KimTho });
        int pigs = e.Pigs.Count;
        var cat = PetMarket.Adopt(e, PetId.MeoKho);
        Check(cat.success && e.PetsAndNpcs.HasPet(PetId.MeoKho) && e.Pigs.Count == pigs - 1, "Mèo Kho for 1 adult pig: " + cat.message);
        var goose = PetMarket.Adopt(e, PetId.NgongBaoDong);
        Check(goose.success && e.Pigs.Count == pigs - 3, "a guard pet asks 2 adult pigs: " + goose.message);
        Check(!PetMarket.Adopt(e, PetId.QuaTin).success, "2 slots at farm level 3");
    }
}
