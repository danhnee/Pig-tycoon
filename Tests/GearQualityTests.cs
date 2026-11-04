using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 12.3 / 12.5 / 20: phẩm chất trang bị I–IV và nguồn (chợ ngày bậc I, chợ đêm I–II, đấu giá III–IV).</summary>
public static class GearQualityTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var guoc = EquipmentAndTacticalCatalog.Find("boot_guoc_gio")!;
        Check(guoc.Quality == ItemTier.I && guoc.BonusAgi == 1, "Guốc Gió bậc I: AGI +1");
        var guoc4 = EquipmentAndTacticalCatalog.Find("boot_guoc_gio#IV")!;
        Check(guoc4.Quality == ItemTier.IV && guoc4.BonusAgi == 8 && guoc4.Name == "Guốc Gió IV" && guoc4.BaseId == "boot_guoc_gio", "Guốc Gió IV: AGI +8");
        Check(EquipmentAndTacticalCatalog.Find("boot_guoc_gio#II")!.BonusAgi == 3 && EquipmentAndTacticalCatalog.Find("boot_guoc_gio#III")!.BonusAgi == 5, "II +3, III +5");
        var giap3 = EquipmentAndTacticalCatalog.Find("armor_giap_luoi_thep#III")!;
        Check(giap3.Defense == 22 && giap3.BonusStr == 0, "Thủ không đổi theo phẩm chất; món không có chỉ số thưởng thì không thêm");
        Check(guoc.BonusAgi == 1, "bản catalog gốc không bị sửa");

        var e = new GameEngine(seed: 13);
        e.Farm.AddGold(100000);
        Check(!MartialProgression.Buy(e, "boot_guoc_gio#II").ok, "chợ ngày không bán bậc II");
        Check(MartialProgression.Buy(e, "boot_guoc_gio").ok, "chợ ngày bán bậc I");
        // wearing quality IV raises the bonus stat (capped at +8 per stat by RecalculateBonusStats)
        e.Character.OwnedEquipment.Add("boot_guoc_gio#IV");
        Check(MartialProgression.Equip(e.Character, "boot_guoc_gio#IV").ok && e.Character.BonusStats.Agi >= 8, "mặc Guốc Gió IV: AGI thưởng 8");

        // night market reward: gear goes to owned equipment, not the warehouse
        var reward = new GameItem { Id = "wpn_giao_moc#II", Name = "Giáo Móc II" };
        NightMarketBarter.GrantReward(e, reward, 1);
        Check(e.Character.OwnedEquipment.Contains("wpn_giao_moc#II") && e.Inventory.CountId("wpn_giao_moc#II") == 0, "đổi ở chợ đêm: thành trang bị sở hữu");

        // auction: one quality III–IV gear lot per night, won lot is owned
        e.Farm.FarmLevel = NightAuction.UnlockFarmLevel;
        while (e.Clock.CurrentHour != 19) e.Tick(60);
        e.Auction.EnsureLots(e);
        var gearLots = e.Auction.Lots.Where(l => l.IsGear).ToList();
        Check(gearLots.Count == 1 && EquipmentQuality.QualityOf(gearLots[0].GearItemId) >= ItemTier.III, "đấu giá có 1 lô trang bị bậc III–IV");
        int idx = e.Auction.Lots.IndexOf(gearLots[0]);
        var won = e.Auction.Bid(e, idx, gearLots[0].RivalBidGold);
        Check(won.won && e.Character.OwnedEquipment.Contains(gearLots[0].GearItemId), "thắng lô trang bị: " + won.message);

        // night market: high-tier gear stays rare (≤ 2% of stalls) over many nights
        int stalls = 0, high = 0, gear = 0;
        var econ = e.Economy;
        for (int n = 0; n < 400; n++)
        {
            econ.GenerateNightMarket(10);
            foreach (var l in econ.NightListings)
            {
                stalls++;
                if (EquipmentAndTacticalCatalog.Find(l.OfferedItem.Id) != null) { gear++; if (l.OfferedItem.Tier >= ItemTier.III) high++; }
            }
        }
        Check(high <= stalls * 0.03, $"bậc III–IV ở chợ đêm ≤ ~2% số ô ({high}/{stalls})");
        Check(gear > 0 && gear < stalls * 0.5, $"trang bị không lấn át chợ đêm ({gear}/{stalls})");
    }
}
