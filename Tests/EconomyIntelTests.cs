using System;
using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 20.1/20.2 (cycle themes, night-market wants), GDD 21 (Bí nhân stock) and GDD 23 (intel 0–5).</summary>
internal static class EconomyIntelTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static void ToHour(GameEngine e, int hour) { for (int g = 0; g < 48 && e.Clock.CurrentHour != hour; g++) e.Tick(60); }

    private static Pig Adult(string id, MeatQuality q = MeatQuality.B, PigStage stage = PigStage.TruongThanh)
    {
        var p = Pig.CreateDefault(id, id, GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 20, initialWeight: 100f);
        p.Stage = stage;
        p.MeatQuality = q;
        return p;
    }

    /// <summary>1000 draws follow the GDD 20.2 weights 24/12/12/10/8/10/12/12 (each within 4σ).</summary>
    public static void RunThemeWeights()
    {
        const int n = 1000;
        var rng = new Random(20261102);
        var counts = new Dictionary<MarketTheme, int>();
        for (int i = 0; i < n; i++)
        {
            var t = MarketCycleThemes.RollWeighted(rng);
            counts[t] = counts.TryGetValue(t, out var c) ? c + 1 : 1;
        }
        Check(MarketCycleThemes.All.Sum(p => p.WeightPercent) == 100, "theme weights sum to 100");
        foreach (var profile in MarketCycleThemes.All)
        {
            double p = profile.WeightPercent / 100.0;
            double expected = n * p, sigma = Math.Sqrt(n * p * (1 - p));
            int got = counts.TryGetValue(profile.Theme, out var c) ? c : 0;
            Check(Math.Abs(got - expected) <= 4 * sigma, $"{profile.NameVi}: {got}/1000, expected ≈{expected:0}");
        }
    }

    /// <summary>The engine seeds a theme when a cycle closes, applies its GDD ranges daily, and the raid reaction.</summary>
    public static void RunThemeInEngine()
    {
        var e = new GameEngine(seed: 501);
        Check(e.Economy.DayMarket.Theme == MarketTheme.CanBang && !e.Economy.DayMarket.ThemeRolled, "cycle 1 is a neutral Cân Bằng");
        var seen = new HashSet<MarketTheme>();
        for (int cycle = 0; cycle < 30; cycle++)
        {
            e.Clock.CompleteMajorWave();
            e.Tick(10);
            var dm = e.Economy.DayMarket;
            var prof = MarketCycleThemes.Get(dm.Theme);
            seen.Add(dm.Theme);
            Check(dm.ThemeRolled && dm.ThemeCycleNumber == e.Clock.CycleNumber, "the new cycle got a seeded theme");
            Check(dm.ThemePriceMultiplier >= prof.PigPriceMin - 1e-4 && dm.ThemePriceMultiplier <= prof.PigPriceMax + 1e-4,
                $"{prof.NameVi}: pig price ×{dm.ThemePriceMultiplier:0.000} inside the GDD range");
            Check(dm.ThemeBuyingPowerMultiplier >= prof.BuyingPowerMin - 1e-4 && dm.ThemeBuyingPowerMultiplier <= prof.BuyingPowerMax + 1e-4,
                $"{prof.NameVi}: buying power inside the GDD range");
            var (min, max) = MarketCycleThemes.PurchasingPowerRange(e.MarketGameStage);
            Check(dm.PurchasingPowerBase >= min && dm.PurchasingPowerBase <= max, "base buying power in the stage range");
            Check(dm.Theme != MarketTheme.MuaCongTrinh || Math.Abs(dm.NightGoldPriceMultiplier - 0.85f) < 1e-4, "Mùa Công Trình gold −15%");
            Check(dm.Theme != MarketTheme.MuaHeoRe || Math.Abs(dm.BarterPigValueRatio - 1f) < 1e-4, "Mùa Heo Rẻ values pigs ×1,0");
            Check(dm.Theme == MarketTheme.MuaHeoRe || Math.Abs(dm.BarterPigValueRatio - 0.9f) < 1e-4, "pigs ×0,9 otherwise");
        }
        Check(seen.Count >= 5, $"the engine really varies the theme ({seen.Count} themes in 30 cycles)");

        // daily refresh stays inside the range
        ToHour(e, 5); e.Tick(120);
        var day = e.Economy.DayMarket;
        var p2 = MarketCycleThemes.Get(day.Theme);
        Check(day.ThemePriceMultiplier >= p2.PigPriceMin - 1e-4 && day.ThemePriceMultiplier <= p2.PigPriceMax + 1e-4, "daily re-roll stays in range");

        // GDD 20.2 world reaction after a raid: pigs scarce +10–35% for 1–3 days
        var pig = Adult("raid_pig");
        e.Pigs.Add(pig);
        ToHour(e, 10);
        int before = e.Economy.CalculatePigSalePrice(pig, 1).priceGold;
        e.EventBus.Publish(new WaveEnded(1, true, 0, e.Clock.CurrentDay));
        var dmr = e.Economy.DayMarket;
        Check(dmr.PostRaidScarcityDaysRemaining >= 1 && dmr.PostRaidScarcityDaysRemaining <= 3, "scarcity lasts 1–3 days");
        Check(dmr.PostRaidScarcityBonus >= 0.10f - 1e-4 && dmr.PostRaidScarcityBonus <= 0.35f + 1e-4, "scarcity +10–35%");
        int after = e.Economy.CalculatePigSalePrice(pig, 1).priceGold;
        Check(Math.Abs(after - before * (1 + dmr.PostRaidScarcityBonus)) <= 2, $"pig price rises with scarcity ({before} → {after})");
        for (int d = 0; d < 4; d++) { ToHour(e, 5); e.Tick(120); }
        Check(e.Economy.DayMarket.PostRaidScarcityDaysRemaining == 0, "scarcity wears off");

        // save / load keeps the theme
        var save = e.CreateSaveData();
        var e2 = new GameEngine(seed: 77);
        e2.RestoreFromSave(save);
        Check(e2.Economy.DayMarket.Theme == e.Economy.DayMarket.Theme && e2.Economy.DayMarket.ThemeRolled
              && Math.Abs(e2.Economy.DayMarket.ThemePriceMultiplier - e.Economy.DayMarket.ThemePriceMultiplier) < 1e-5, "theme survives save/load");
    }

    /// <summary>1000 generated night stalls: ~18% take gold at GTTC × 1,4, each asks 1–2 wants, gold stalls tier I–II.</summary>
    public static void RunNightStallGeneration()
    {
        var eco = new EconomyManager(20261102);
        int slots = 0, gold = 0, highTierGold = 0;
        for (int i = 0; i < 125; i++)
        {
            eco.GenerateNightMarket(5);
            foreach (var l in eco.NightListings)
            {
                slots++;
                Check(l.Wants.Count >= 1 && l.Wants.Count <= 2, "each stall lists 1–2 wanted items");
                Check(l.Wants.All(w => w.Quantity >= 1 && w.UnitGttc > 0), "wants are concrete");
                Check(l.Wants.Where(w => w.IsPig).All(w => w.Quantity <= 2), "pig wants ask 1–2 pigs");
                Check(!string.IsNullOrEmpty(l.RequestedItemDescription) && l.RequestedItemDescription.Contains("Muốn nhận"), "wants are shown");
                if (!l.AcceptsGold) continue;
                gold++;
                if (l.OfferedItem.Tier >= ItemTier.III) highTierGold++;
                int total = l.OfferedItem.ReferenceValueGold * l.Quantity;
                Check(l.GoldPrice == (int)Math.Round(total * 1.4f), $"gold price = GTTC × 1,4 ({l.GoldPrice} vs {total})");
            }
        }
        Check(slots == 1000, "1000 stalls");
        double share = gold / 1000.0;
        Check(share > 0.144 && share < 0.216, $"gold stalls ≈18% (measured {share:P1})");
        Check(highTierGold <= 20, "tier III–IV gold stalls ≤ 2%");
    }

    private static NightMarketListing MakeListing(string slot, string offerId, int offerGttc, params NightMarketWant[] wants)
    {
        var l = new NightMarketListing
        {
            SlotId = slot,
            OfferedItem = new GameItem { Id = offerId, Name = offerId, Tier = ItemTier.II, ReferenceValueGold = offerGttc, Category = "Combat" },
            Quantity = 1,
            RequestedCategory = "Any",
        };
        l.Wants.AddRange(wants);
        l.MinimumReferenceValue = l.Wants.Sum(w => w.TotalGttc);
        return l;
    }

    private static NightMarketWant ItemWant(string id, int qty) => new NightMarketWant
    {
        ItemId = id, NameVi = id, Quantity = qty, UnitGttc = NightMarketBarter.UnitGttcOfItem(id), Group = NightMarketBarter.GroupOfItem(id)
    };

    /// <summary>GDD 20.1: exact wants, same-group ≥110% substitutes, wrong group refused, 5% fee, 40% gold sale.</summary>
    public static void RunBarterRules()
    {
        var e = new GameEngine(seed: 61);
        e.Farm.AddGold(5000);
        ToHour(e, 20);
        e.Village.RefreshStockForDay(e.Clock.CurrentDay);
        var feedWant = ItemWant("CamTangTrong", 3);   // 3 × 28 = 84 GTTC
        Check(feedWant.Group == NightMarketBarter.GroupMaterial, "feed is a material");
        var stall = MakeListing("test_feed", "LuuKhoi", 180, feedWant);
        e.Economy.NightListings.Add(stall);

        Check(!e.TradeWithVillager(VillagerId.ChoDem, stall.SlotId, null, null, false).success, "nothing offered is refused");
        e.Inventory.AddItem("BinhHoiLuc", 2);
        var wrong = e.TradeWithVillager(VillagerId.ChoDem, stall.SlotId, new List<GameItem> { new GameItem { Id = "BinhHoiLuc", Quantity = 2 } }, null, false);
        Check(!wrong.success, "an item of another group cannot replace a material want");

        int camBefore = e.Inventory.CountId("CamThuong"), luuBefore = e.Inventory.CountId("LuuKhoi");
        e.Inventory.AddItem("CamThuong", 8);
        var seven = e.TradeWithVillager(VillagerId.ChoDem, stall.SlotId, new List<GameItem> { new GameItem { Id = "CamThuong", Quantity = 7 } }, null, false);
        Check(!seven.success, "7 Cám Thường = 84 < 110% of 84 is refused: " + seven.message);
        int goldBefore = e.Farm.Gold;
        var eight = e.TradeWithVillager(VillagerId.ChoDem, stall.SlotId, new List<GameItem> { new GameItem { Id = "CamThuong", Quantity = 8 } }, null, false);
        Check(eight.success, "8 Cám Thường = 96 ≥ 92,4 substitutes: " + eight.message);
        Check(goldBefore - e.Farm.Gold == 9, $"5% GTTC fee of 180 = 9G (paid {goldBefore - e.Farm.Gold})");
        Check(e.Inventory.CountId("CamThuong") == camBefore && e.Inventory.CountId("LuuKhoi") == luuBefore + 1 && stall.IsSoldOut,
            $"goods swapped once (cám {camBefore}→{e.Inventory.CountId("CamThuong")}, lựu {luuBefore}→{e.Inventory.CountId("LuuKhoi")})");

        var exactStall = MakeListing("test_exact", "BomKeo", 190, ItemWant("CamTangTrong", 3));
        e.Economy.NightListings.Add(exactStall);
        e.Inventory.AddItem("CamTangTrong", 3);
        Check(e.TradeWithVillager(VillagerId.ChoDem, exactStall.SlotId, new List<GameItem> { new GameItem { Id = "CamTangTrong", Quantity = 3 } }, null, false).success,
            "the exact wanted item trades");

        // pigs: 2 adults wanted; one adult (not worth 110% of two) is refused, two pass via the multi-pig call
        int adultGttc = e.Economy.EstimateWantedPigGttc(WantedPigKind.AnyAdult, 1);
        var pigStall = MakeListing("test_pigs", "TinhNoDan", 310, new NightMarketWant
            { PigKind = WantedPigKind.AnyAdult, NameVi = "heo trưởng thành", Quantity = 2, UnitGttc = adultGttc, Group = NightMarketBarter.GroupPig });
        e.Economy.NightListings.Add(pigStall);
        var a = Adult("bar_a"); var b = Adult("bar_b"); var old = Adult("bar_old", stage: PigStage.HeoGia);
        e.Pigs.Add(a); e.Pigs.Add(b); e.Pigs.Add(old);
        Check(!e.TradeWithVillager(VillagerId.ChoDem, pigStall.SlotId, null, "bar_a", false).success, "one adult does not cover two");
        Check(!e.TradeWithVillagerPigs(VillagerId.ChoDem, pigStall.SlotId, null, new[] { "bar_a", "bar_old" }, false).success,
            "an elder is not an adult and is worth less than 110%");
        var pigsOk = e.TradeWithVillagerPigs(VillagerId.ChoDem, pigStall.SlotId, null, new[] { "bar_a", "bar_b" }, false);
        Check(pigsOk.success && !e.Pigs.Contains(a) && !e.Pigs.Contains(b) && e.Pigs.Contains(old), "two adults trade: " + pigsOk.message);

        // gold stall vs barter-only
        var goldStall = MakeListing("test_gold", "PhaoSang", 110, ItemWant("BinhHoiLuc", 1));
        goldStall.AcceptsGold = true; goldStall.GoldPrice = (int)Math.Round(110 * 1.4f);
        e.Economy.NightListings.Add(goldStall);
        int g0 = e.Farm.Gold;
        Check(e.TradeWithVillager(VillagerId.ChoDem, goldStall.SlotId, null, null, true).success && g0 - e.Farm.Gold == 154, "gold stall: GTTC × 1,4");
        var barterOnly = MakeListing("test_nogold", "PhaoSang", 110, ItemWant("BinhHoiLuc", 1));
        e.Economy.NightListings.Add(barterOnly);
        Check(!e.TradeWithVillager(VillagerId.ChoDem, barterOnly.SlotId, null, null, true).success, "a barter-only stall refuses gold");

        // selling for gold: 40% of GTTC, night only
        e.Inventory.AddItem("BinhHoiLuc", 3);
        int have = e.Inventory.CountId("BinhHoiLuc");
        int g1 = e.Farm.Gold;
        var sale = NightMarketBarter.SellItemsForGold(e, new List<(string, int)> { ("BinhHoiLuc", have) });
        Check(sale.success && e.Farm.Gold - g1 == (int)Math.Round(90 * have * 0.4) && e.Inventory.CountId("BinhHoiLuc") == 0,
            $"selling for gold gets 40% GTTC ({sale.goldEarned}G for {have})");
        ToHour(e, 10);
        e.Inventory.AddItem("BinhHoiLuc", 1);
        Check(!NightMarketBarter.SellItemsForGold(e, new List<(string, int)> { ("BinhHoiLuc", 1) }).success, "no night sale by day");
    }

    /// <summary>GDD 21: 2–4 slots from his pool at 130–200% GTTC, no gold, only while present, ÁLSK +2..+10 per trade.</summary>
    public static void RunMysticMerchant()
    {
        var eco = new EconomyManager(2111);
        var sizes = new HashSet<int>();
        int visits = 0;
        for (int day = 1; day <= 4000 && visits < 120; day++)
        {
            eco.UpdateMysticMerchantHour(day, 18, 400, MarketTheme.HoangLoanDich);
            if (eco.MysticMerchant.Stock.Count == 0) continue;
            visits++;
            var stock = eco.MysticMerchant.Stock;
            sizes.Add(stock.Count);
            Check(stock.Count >= 2 && stock.Count <= 4, "2–4 slots");
            Check(stock.Select(s => s.OfferedItem.Id).Distinct().Count() == stock.Count, "distinct goods");
            foreach (var s in stock)
            {
                Check(s.IsMysticListing && !s.AcceptsGold && s.GoldPrice == 0, "never gold");
                int gttc = s.OfferedItem.ReferenceValueGold;
                Check(s.MinimumReferenceValue >= gttc * 1.30 - 1 && s.MinimumReferenceValue <= gttc * 2.00 + 1, "asks 130–200% GTTC");
                var w = s.Wants.Single();
                if (w.PigKind == WantedPigKind.Elder) Check(w.Quantity >= 3 && w.Quantity <= 8, "3–8 elder pigs");
                if (w.PigKind == WantedPigKind.Corpse) Check(w.Quantity >= 1 && w.Quantity <= 3, "1–3 corpses");
            }
        }
        Check(visits >= 50 && sizes.SetEquals(new[] { 2, 3, 4 }), $"stock sizes 2,3,4 all occur ({string.Join(",", sizes)})");

        var e = new GameEngine(seed: 2112);
        ToHour(e, 21);
        e.Village.RefreshStockForDay(e.Clock.CurrentDay);
        var stall = new NightMarketListing
        {
            SlotId = "binhan_test", Quantity = 1, IsMysticListing = true, MysticRate = 1.5f, MinimumReferenceValue = 2250,
            OfferedItem = new GameItem { Id = NightMarketBarter.FullIntelItemId, Name = "Tình Báo Toàn Phần", Tier = ItemTier.IV, ReferenceValueGold = 1500, Category = "Info" },
        };
        stall.Wants.Add(new NightMarketWant { NameVi = "vật phẩm bậc III–IV", GroupOnly = true, Quantity = 1, UnitGttc = 2250, Group = NightMarketBarter.GroupItemTierIIIPlus });
        e.Economy.MysticMerchant.Stock.Add(stall);
        e.Economy.MysticMerchant.IsActive = false;
        e.Inventory.AddItem("HoSoGiongLoai", 3);   // tier III, 950 each
        var offer = new List<GameItem> { new GameItem { Id = "HoSoGiongLoai", Quantity = 3 } };
        Check(!e.TradeWithVillager(VillagerId.BiNhan, stall.SlotId, offer, null, false).success, "no trade while he is away");
        e.Economy.MysticMerchant.IsActive = true;
        Check(!e.TradeWithVillager(VillagerId.BiNhan, stall.SlotId, null, null, true).success, "he never takes gold");
        Check(!e.TradeWithVillager(VillagerId.BiNhan, stall.SlotId, new List<GameItem> { new GameItem { Id = "HoSoGiongLoai", Quantity = 2 } }, null, false).success,
            "1.900 < 2.250 (150%) is refused");
        int gold = e.Farm.Gold, pressure = e.Farm.EventPressureIndex;
        var ok = e.TradeWithVillager(VillagerId.BiNhan, stall.SlotId, offer, null, false);
        Check(ok.success, "2.850 ≥ 2.250 trades: " + ok.message);
        int rise = e.Farm.EventPressureIndex - pressure;
        Check(rise >= 2 && rise <= 10 && e.Farm.Gold == gold, $"ÁLSK +2..+10 ({rise}), no gold fee");
        Check(e.Intel.Level == IntelBureau.FullLevel, "his full intel package gives level 5 without the ladder");

        // no substitution with him: a Quý+ pig want is not met by a plain pig
        var quy = new NightMarketListing { SlotId = "binhan_quy", Quantity = 1, IsMysticListing = true, MinimumReferenceValue = 10,
            OfferedItem = new GameItem { Id = "BanDoHuongQuai", Name = "Bản Đồ Hướng Quái", ReferenceValueGold = 280, Category = "Info" } };
        quy.Wants.Add(new NightMarketWant { PigKind = WantedPigKind.QuyPlus, NameVi = "heo Quý+", Quantity = 1, UnitGttc = 5, Group = NightMarketBarter.GroupPig });
        e.Economy.MysticMerchant.Stock.Add(quy);
        e.Pigs.Add(Adult("plain_pig", MeatQuality.S));
        Check(!e.TradeWithVillager(VillagerId.BiNhan, quy.SlotId, null, "plain_pig", false).success, "Bí nhân only takes the exact rare thing");
    }

    /// <summary>GDD 23: 150/300/450/600G in order, level 5 not for sale, expiry at the next cycle, nothing unbought shown.</summary>
    public static void RunIntelLevels()
    {
        var e = new GameEngine(seed: 2323);
        e.Farm.AddGold(3000);
        var r0 = e.Intel.BuildReport(e);
        Check(r0.Level == 0 && r0.Warnings.Count == 1 && r0.Confirmed.Count == 0 && r0.Speculation.Count == 0, "level 0 is only a vague signal");
        Check(!e.Intel.Purchase(e, 1).success, "GDD 15: no intel before the Bàn Tình Báo is built");
        e.Defense.ConstructBuilding(IntelBureau.DeskBlueprintId);
        Check(!e.Intel.Purchase(e, 2).success, "level 2 needs level 1");
        int g = e.Farm.Gold;
        Check(e.Intel.Purchase(e, 1).success && g - e.Farm.Gold == 150, "level 1 costs 150G");
        var r1 = e.Intel.BuildReport(e);
        Check(r1.Speculation.Count == 2 && r1.Confirmed.Count == 0 && !r1.AllLines.Any(l => l.Contains("±") || l.Contains("ÁLSK") || l.Contains("Cửa sổ")),
            "level 1: type + cycle length range as speculation, nothing more");
        Check(r1.Speculation.Any(l => l.Contains($"–")), "cycle length range shown");
        Check(e.Intel.Purchase(e, 2).success && g - e.Farm.Gold == 450, "level 2 costs 300G");
        var r2 = e.Intel.BuildReport(e);
        Check(r2.Confirmed.Count == 1 && !r2.AllLines.Any(l => l.Contains("±") || l.Contains("Cửa sổ")), "level 2 adds 1–2 true factors only");
        Check(!e.Intel.Purchase(e, 3).success && g - e.Farm.Gold == 450, "GDD 15: the desk answers 2 questions a day");
        e.Intel.QuestionsToday = 0; // next day
        Check(e.Intel.Purchase(e, 3).success && g - e.Farm.Gold == 900, "level 3 costs 450G");
        var r3 = e.Intel.BuildReport(e);
        var band = r3.Warnings.FirstOrDefault(l => l.Contains("±2"));
        Check(band != null && !r3.AllLines.Any(l => l.Contains("Cửa sổ")), "level 3: 'đợt lớn sau c±2 ngày', no window yet");
        int center = int.Parse(new string(band!.Substring(band.IndexOf("sau ", StringComparison.Ordinal) + 4).TakeWhile(char.IsDigit).ToArray()));
        Check(Math.Abs(center - e.Clock.GetDaysUntilMajorWave()) <= 2, "the band holds the truth");
        Check(e.Intel.Purchase(e, 4).success && g - e.Farm.Gold == 1500, "level 4 costs 600G");
        var r4 = e.Intel.BuildReport(e);
        Check(r4.Warnings.Any(l => l.Contains("Cửa sổ") && l.Contains("buổi tối")), "level 4: ±1 day window with time of day");
        Check(!r4.AllLines.Any(l => l.Contains("dài đúng") || l.Contains("20:00")), "level 4 does not leak the exact level-5 facts");
        Check(!e.Intel.Purchase(e, 5).success && e.Intel.Level == 4, "level 5 is not sold at the Bàn Tình Báo");

        e.Clock.CompleteMajorWave();
        e.Tick(10);
        Check(e.Intel.Level == 0 && e.Intel.CycleNumber == e.Clock.CycleNumber, "intel expires at the next cycle");
        e.Intel.GrantFull(e.Clock.CycleNumber);
        var r5 = e.Intel.BuildReport(e);
        Check(r5.Level == 5 && r5.Confirmed.Any(l => l.Contains("dài đúng")) && r5.Speculation.Count == 0, "level 5 is full and exact");

        var save = e.CreateSaveData();
        var e2 = new GameEngine(seed: 1);
        e2.RestoreFromSave(save);
        Check(e2.Intel.Level == 5 && e2.Intel.CycleNumber == e.Clock.CycleNumber, "intel survives save/load");
    }
}
