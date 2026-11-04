using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>C-08: every rare progression good has a source in play.</summary>
internal static class RareSourcesTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var e = new GameEngine(9);
        int manuals = e.Character.SecretManuals;
        string loot = SpecialtyPaperActions.OnMajorWaveWon(e, bossKilled: true, () => 0.99);
        Check(e.Character.SecretManuals == manuals + 1 && e.Inventory.CountId(DefenseRepairs.FineComponentId) >= 1 &&
              e.Inventory.CountId(EnhancementTransactions.StoneId) >= 1 && e.Inventory.CountId(RareSources.PurpleDustId) == 2, "a cycle Boss leaves Bí Kíp, Linh Kiện Tinh, stones, Bột Tử Tinh: " + loot);
        Check(e.Inventory.CountId(RareSources.BloodSealId) == 0, "Huyết Triện only from Bosses of cycle ≥ 15");
        Check(SpecialtyPaperActions.RollDrop(e, SpecialtyPaperActions.CycleBossRates, 0.0) == null, "GDD 10.2: no specialty paper before the mid-game gate");

        // auction item lots appear over a run of nights (rolled per night, deterministic by seed)
        bool sawStone = false, sawFine = false, sawManual = false;
        while (e.Farm.FarmLevel < NightAuction.UnlockFarmLevel) e.Farm.AddFarmExp(5000);
        for (int night = 0; night < 40; night++)
        {
            e.Tick(24 * 60);
            if (e.Clock.CurrentHour < 18) e.Tick((20 - e.Clock.CurrentHour) * 60);
            e.Auction.EnsureLots(e);
            sawStone |= e.Auction.Lots.Any(l => l.ItemId == EnhancementTransactions.StoneId);
            sawFine |= e.Auction.Lots.Any(l => l.ItemId == DefenseRepairs.FineComponentId);
            sawManual |= e.Auction.Lots.Any(l => l.ItemId == RareSources.SecretManualId);
            Check(!e.Auction.Lots.Any(l => l.ItemId.StartsWith("(G)Paper")), "no paper lots before the mid-game gate");
        }
        Check(sawStone && sawFine && sawManual, "Đá Cường Hóa, Linh Kiện Tinh and Bí Kíp lots show up at the auction");

        Check(NightMarketCatalog.MysticOffers(null).All(o => o.Id != "Paper_Red"), "GDD 10.3: Đỏ only at the auction, not with Bí nhân");
        Check(NightMarketCatalog.NightOffers(null).All(o => o.Kind != NightMarketCatalog.Kind.GenePaper), "papers are not in the ordinary stall pool");
        Check(NightMarketCatalog.NightOffers(null).Any(o => o.Id == RareSources.SecretManualId) && NightMarketCatalog.NightOffers(null).Any(o => o.Id == RareSources.InkId),
            "GDD 20: Bí kíp and Mực Ấn can be bartered at the night market");
    }
}
