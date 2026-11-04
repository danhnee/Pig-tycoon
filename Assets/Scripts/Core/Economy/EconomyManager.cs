using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameItem
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public ItemTier Tier;
        public int ReferenceValueGold; // GTTC
        public string Category = string.Empty; // Pig, Material, Consumable, Blueprint, Special
        public string Description = string.Empty;
        public int Quantity = 1;
    }

    [Serializable]
    public class DayMarketState
    {
        public MarketTheme Theme = MarketTheme.CanBang;
        public float ThemePriceMultiplier = 1.0f;
        public float FeedPriceMultiplier = 1.0f;
        public float PigletBuyPriceMultiplier = 1.0f;
        public float EquipmentPriceMultiplier = 1.0f;
        public float BuildingMaterialPriceMultiplier = 1.0f;
        public int DailyPurchasingPowerCap = 40;
        public int CurrentPigsSoldToday = 0;
        public int QuarantineFeePerPig = 60;

        // Tác động chéo trực tiếp (GDD 4.5, 5.1, 10.8, 18.2):
        // - Bán heo ủ bệnh không kiểm dịch bị phát hiện -> phạt -10% sức mua toàn Chợ Ngày trong 3 ngày
        // - Bán dồn cùng 1 dòng gen trong ngày (>3 con) -> bão hòa cầu cục bộ dòng gen đó (-2%/con vượt ngưỡng)
        // - Mở khóa Đặc tính của An ('Mặc Cả Mềm' / 'Mặc Cả 90%') -> giảm 10% phí kiểm dịch & vật tư, tăng giá đàm phán
        public int UnquarantinedSickPenaltyDaysRemaining = 0;
        public float UnquarantinedSickPenaltyMultiplier => UnquarantinedSickPenaltyDaysRemaining > 0 ? 0.90f : 1.0f;
        public float ProtagonistNegotiationBonusMultiplier = 1.0f;
        public Dictionary<GeneLineId, int> BreedSoldTodayCounts = new Dictionary<GeneLineId, int>();
        public MarketTheme NextCyclePreviewTheme = MarketTheme.CanBang;
        public bool IsNextCycleThemeRevealed = false;

        // GDD 20.2/20.3 (ADR-0023): the running cycle theme is seeded per cycle; its pig-price and buying-power
        // multipliers are re-rolled inside the theme's GDD range at every daily refresh. Cycle 1 stays neutral.
        public int ThemeCycleNumber = 1;
        public bool ThemeRolled = false;
        public bool NextThemePreRolled = false;
        public float ThemeBuyingPowerMultiplier = 1.0f;
        public float NightGoldPriceMultiplier = 1.0f;     // Mùa Công Trình: giá vàng −15%
        public float BarterPigValueRatio = 0.9f;          // GDD 20.1 heo = giá chợ ngày × 0,9; Mùa Heo Rẻ ×1,0
        public int PurchasingPowerBase = 40;              // GDD 20.3 stage range (early 20–60)
        // GDD 20.2 world reaction: after a raid pigs are scarce, +10–35% for 1–3 market days
        public int PostRaidScarcityDaysRemaining = 0;
        public float PostRaidScarcityBonus = 0f;
        public float PostRaidPigPriceMultiplier => PostRaidScarcityDaysRemaining > 0 ? 1f + PostRaidScarcityBonus : 1f;
    }

    [Serializable]
    public class NightMarketListing
    {
        public string SlotId = string.Empty;
        public GameItem OfferedItem = new GameItem();
        public int Quantity;
        public string RequestedItemDescription = string.Empty;
        public string RequestedCategory = string.Empty;
        public int MinimumReferenceValue;
        public bool AcceptsGold;
        public int GoldPrice;
        public bool IsReservedByAnLock;
        public bool IsSoldOut => Quantity <= 0;
        // GDD 20.1: 1–2 exact "món muốn nhận"; empty = legacy village counter (category + minimum GTTC)
        public List<NightMarketWant> Wants = new List<NightMarketWant>();
        public bool IsMysticListing;                      // GDD 21 Bí nhân: never gold, 130–200% GTTC
        public float MysticRate;
    }

    [Serializable]
    public class MysticMerchantState
    {
        public bool IsActive;
        public int SpawnTimeHour;
        public int DurationHoursRemaining;
        public int DistanceFromBoundaryMeters;
        public List<GameItem> OfferedItems = new List<GameItem>();
        public List<NightMarketListing> Stock = new List<NightMarketListing>();   // GDD 21: 2–4 slots
        public int StockNight = -1;
    }

    [Serializable]
    public class EconomyManager
    {
        public DayMarketState DayMarket { get; private set; } = new DayMarketState();
        public List<NightMarketListing> NightListings { get; private set; } = new List<NightMarketListing>();
        public MysticMerchantState MysticMerchant { get; private set; } = new MysticMerchantState();

        private readonly Random random;

        public EconomyManager()
        {
            random = new Random();
        }

        public EconomyManager(int seed)
        {
            random = new Random(seed);
        }

        /// <summary>
        /// GDD 20.2: Áp dụng 8 chủ đề chu kỳ thị trường (MarketTheme), deterministic: pig price, buying power and feed at
        /// the middle of the theme's GDD range (Cơn Sốt Thịt 115–145% → ×1,30). Feed ×1,10 in Cơn Sốt Thịt, quarantine
        /// 90G in Hoảng Loạn Dịch and the equipment/material multipliers are older provisional numbers (ADR-0023).
        /// </summary>
        public void ApplyMarketCycleTheme(MarketTheme theme)
        {
            var profile = MarketCycleThemes.Get(theme);
            DayMarket.Theme = theme;
            DayMarket.ThemePriceMultiplier = (profile.PigPriceMin + profile.PigPriceMax) * 0.5f;
            DayMarket.ThemeBuyingPowerMultiplier = (profile.BuyingPowerMin + profile.BuyingPowerMax) * 0.5f;
            DayMarket.FeedPriceMultiplier = (profile.FeedMin + profile.FeedMax) * 0.5f;
            DayMarket.PigletBuyPriceMultiplier = 1.0f;
            DayMarket.EquipmentPriceMultiplier = 1.0f;
            DayMarket.BuildingMaterialPriceMultiplier = 1.0f;
            DayMarket.NightGoldPriceMultiplier = 1.0f;
            DayMarket.BarterPigValueRatio = NightMarketBarter.PigGttcRatio;
            DayMarket.QuarantineFeePerPig = 60;

            switch (theme)
            {
                case MarketTheme.CanBang:
                    break;
                case MarketTheme.MuaHeoRe:
                    DayMarket.PigletBuyPriceMultiplier = 0.75f;
                    DayMarket.BarterPigValueRatio = 1.0f;          // GDD 20.2 "heo định giá ×1,0"
                    break;
                case MarketTheme.ConSotThit:
                    DayMarket.FeedPriceMultiplier = 1.10f;
                    break;
                case MarketTheme.KhanHiemThucAn:
                    break;
                case MarketTheme.HoangLoanDich:
                    DayMarket.QuarantineFeePerPig = 90;
                    break;
                case MarketTheme.KhanHiemHauDotKich:
                    DayMarket.BuildingMaterialPriceMultiplier = 1.25f;
                    break;
                case MarketTheme.MuaTrangBi:
                    DayMarket.EquipmentPriceMultiplier = 0.80f;
                    break;
                case MarketTheme.MuaCongTrinh:
                    DayMarket.BuildingMaterialPriceMultiplier = 0.80f;
                    DayMarket.NightGoldPriceMultiplier = MarketCycleThemes.MuaCongTrinhNightGoldMultiplier;
                    break;
            }
            DayMarket.DailyPurchasingPowerCap = Math.Max(1, (int)Math.Round(DayMarket.PurchasingPowerBase * DayMarket.ThemeBuyingPowerMultiplier));
        }

        /// <summary>
        /// GDD 20.2: a new cycle starts → its theme is drawn by the GDD weights (or the theme already pre-rolled for the
        /// "next cycle" preview), applied, and its pig price / buying power rolled inside the theme's range.
        /// </summary>
        public MarketTheme SeedCycleTheme(int cycleNumber, int gameStage = 0)
        {
            var theme = DayMarket.NextThemePreRolled ? DayMarket.NextCyclePreviewTheme : MarketCycleThemes.RollWeighted(random);
            ApplyMarketCycleTheme(theme);
            DayMarket.ThemeRolled = true;
            DayMarket.ThemeCycleNumber = cycleNumber;
            RollThemeDay(gameStage);
            DayMarket.NextCyclePreviewTheme = MarketCycleThemes.RollWeighted(random);
            DayMarket.NextThemePreRolled = true;
            DayMarket.IsNextCycleThemeRevealed = false;
            return theme;
        }

        /// <summary>The theme the next cycle will have (rolled now if not yet), for Tin Đồn Chợ / Trạm Tiếp Sóng.</summary>
        public MarketTheme PeekNextCycleTheme()
        {
            if (!DayMarket.NextThemePreRolled)
            {
                DayMarket.NextCyclePreviewTheme = MarketCycleThemes.RollWeighted(random);
                DayMarket.NextThemePreRolled = true;
            }
            return DayMarket.NextCyclePreviewTheme;
        }

        /// <summary>
        /// GDD 20.2/20.3 daily refresh: pig price and buying power inside the theme's range, base buying power inside the
        /// stage range (0 early 20–60, 1 mid 50–120, 2 late 100–220), feed +35–70% in Khan Hiếm Thức Ăn. Cycle 1 (no
        /// seeded theme yet) keeps the neutral ×1,0 / 40 pigs.
        /// </summary>
        public void RollThemeDay(int gameStage)
        {
            if (!DayMarket.ThemeRolled) return;
            var profile = MarketCycleThemes.Get(DayMarket.Theme);
            DayMarket.ThemePriceMultiplier = MarketCycleThemes.Uniform(random, profile.PigPriceMin, profile.PigPriceMax);
            DayMarket.ThemeBuyingPowerMultiplier = MarketCycleThemes.Uniform(random, profile.BuyingPowerMin, profile.BuyingPowerMax);
            if (profile.FeedMax > profile.FeedMin)
                DayMarket.FeedPriceMultiplier = MarketCycleThemes.Uniform(random, profile.FeedMin, profile.FeedMax);
            var (min, max) = MarketCycleThemes.PurchasingPowerRange(gameStage);
            DayMarket.PurchasingPowerBase = min + random.Next(max - min + 1);
            DayMarket.DailyPurchasingPowerCap = Math.Max(1, (int)Math.Round(DayMarket.PurchasingPowerBase * DayMarket.ThemeBuyingPowerMultiplier));
        }

        /// <summary>
        /// GDD 20.2 world reaction: after a raid pigs are scarce, +10–35% for 1–3 days. A raid ending outside day-market
        /// hours starts counting at the next market day.
        /// </summary>
        public void ApplyPostRaidScarcity(bool duringDayMarket)
        {
            float bonus = MarketCycleThemes.Uniform(random, MarketCycleThemes.PostRaidBonusMin, MarketCycleThemes.PostRaidBonusMax);
            int days = MarketCycleThemes.PostRaidDaysMin + random.Next(MarketCycleThemes.PostRaidDaysMax - MarketCycleThemes.PostRaidDaysMin + 1);
            DayMarket.PostRaidScarcityBonus = Math.Max(DayMarket.PostRaidScarcityDaysRemaining > 0 ? DayMarket.PostRaidScarcityBonus : 0f, bonus);
            DayMarket.PostRaidScarcityDaysRemaining = Math.Max(DayMarket.PostRaidScarcityDaysRemaining, days + (duringDayMarket ? 0 : 1));
        }

        internal int NextInt(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);

        /// <summary>Locked blueprints (id, name, build cost, Bạch Vân) for blueprint stalls; set by GameEngine.</summary>
        public Func<IEnumerable<(string id, string name, int cost, bool bachVan)>>? BlueprintSource { get; set; }

        public (int priceGold, float successChanceWithoutQuarantine) CalculatePigSalePrice(Pig pig, int farmLevel)
        {
            if (pig.Rarity == GeneRarity.DiBien)
            {
                return (0, 0f);
            }

            float meatMult = pig.MeatQuality switch
            {
                MeatQuality.C => 0.8f,
                MeatQuality.B => 1.0f,
                MeatQuality.A => 1.2f,
                MeatQuality.S => 1.5f,
                _ => 1.0f
            };

            float breedMult = 1.0f;
            float noQuarantineRate = 0.70f;

            if (pig.IsIdentified)
            {
                if (pig.Rarity == GeneRarity.Kha) { breedMult = 1.10f; noQuarantineRate = 0.65f; }
                else if (pig.Rarity == GeneRarity.Hiem) { breedMult = 1.30f; noQuarantineRate = 0.55f; }
                else if (pig.Rarity == GeneRarity.Quy) { breedMult = 1.60f; noQuarantineRate = 0.45f; }
                else if (pig.Rarity == GeneRarity.HuyenThoai) { breedMult = 2.00f; noQuarantineRate = 0.35f; }
            }

            bool isRareOrAbove = pig.IsIdentified && (
                pig.Rarity == GeneRarity.Hiem ||
                pig.Rarity == GeneRarity.Quy ||
                pig.Rarity == GeneRarity.HuyenThoai
            );

            float stageMult = 1.0f;
            float baseRefPrice = pig.WeightKg * 3.0f;

            if (pig.Stage == PigStage.HeoNon)
            {
                stageMult = isRareOrAbove ? 1.0f : 0.35f;
                baseRefPrice = 315f;
            }
            else if (pig.Stage == PigStage.DangLon)
            {
                stageMult = isRareOrAbove ? 1.0f : 0.55f;
                baseRefPrice = 315f;
            }
            else if (pig.Stage == PigStage.TruongThanh)
            {
                stageMult = (pig.WeightKg >= 95f && pig.WeightKg <= 115f) ? 1.0f : 0.85f;
            }
            else if (pig.Stage == PigStage.HeoGia)
            {
                stageMult = 0.25f;
            }

            float purchasingPressure = 1.0f;
            float halfCap = DayMarket.DailyPurchasingPowerCap * 0.5f;
            if (DayMarket.CurrentPigsSoldToday > halfCap)
            {
                float extra = DayMarket.CurrentPigsSoldToday - halfCap;
                float penalty = Math.Min(0.25f, extra * 0.005f);
                purchasingPressure = 1.0f - penalty;
            }

            // Bão hòa dòng gen cục bộ nếu bán dồn >3 con cùng dòng gen trong 1 ngày
            float breedSaturationMult = 1.0f;
            if (DayMarket.BreedSoldTodayCounts != null &&
                DayMarket.BreedSoldTodayCounts.TryGetValue(pig.GeneLine, out int sameBreedSold) &&
                sameBreedSold > 3)
            {
                breedSaturationMult = Math.Max(0.84f, 1.0f - (sameBreedSold - 3) * 0.02f);
            }

            float rawPrice = baseRefPrice
                * meatMult
                * stageMult
                * breedMult
                * DayMarket.ThemePriceMultiplier
                * DayMarket.PostRaidPigPriceMultiplier
                * purchasingPressure
                * breedSaturationMult
                * DayMarket.UnquarantinedSickPenaltyMultiplier
                * DayMarket.ProtagonistNegotiationBonusMultiplier;

            int finalPrice = (int)Math.Round(rawPrice);
            return (Math.Max(10, finalPrice), noQuarantineRate);
        }

        /// <summary>
        /// Ghi nhận giao dịch bán heo và áp dụng phản hồi nhân quả ngược vào thị trường:
        /// - Tăng bộ đếm heo bán trong ngày và bộ đếm bão hòa theo dòng gen.
        /// - Nếu bán heo mang mầm bệnh mà KHÔNG kiểm dịch -> kích hoạt phạt -10% sức mua Chợ Ngày trong 3 ngày (GDD 4.5 &amp; 5.1).
        /// </summary>
        public void RecordPigSaleAndApplyMarketFeedback(
            Pig pig,
            bool isQuarantined,
            out bool unquarantinedSickContaminationDetected)
        {
            unquarantinedSickContaminationDetected = false;
            DayMarket.CurrentPigsSoldToday += 1;

            if (DayMarket.BreedSoldTodayCounts.TryGetValue(pig.GeneLine, out int currentCount))
            {
                DayMarket.BreedSoldTodayCounts[pig.GeneLine] = currentCount + 1;
            }
            else
            {
                DayMarket.BreedSoldTodayCounts[pig.GeneLine] = 1;
            }

            // GDD 4.5: người mua phát hiện bệnh sau đó — kể cả bệnh không triệu chứng hoặc bệnh tiềm ẩn
            if (!isQuarantined && HerdLifecycleRules.CarriesAnyIllness(pig))
            {
                unquarantinedSickContaminationDetected = true;
                DayMarket.UnquarantinedSickPenaltyDaysRemaining = Math.Max(DayMarket.UnquarantinedSickPenaltyDaysRemaining, 3);
            }
        }

        /// <summary>
        /// Chuyển ngày cho thị trường: giảm số ngày phạt bán heo bệnh lậu, xóa bộ đếm ngày, giữ ô Chợ Đêm đã khóa của An.
        /// </summary>
        public void AdvanceMarketDay(int farmLevel, bool revealNextCycleTheme = false, int gameStage = 0)
        {
            DayMarket.CurrentPigsSoldToday = 0;
            DayMarket.BreedSoldTodayCounts.Clear();
            if (DayMarket.UnquarantinedSickPenaltyDaysRemaining > 0)
            {
                DayMarket.UnquarantinedSickPenaltyDaysRemaining -= 1;
            }
            if (DayMarket.PostRaidScarcityDaysRemaining > 0)
            {
                DayMarket.PostRaidScarcityDaysRemaining -= 1;
            }
            DayMarket.IsNextCycleThemeRevealed = revealNextCycleTheme;
            RollThemeDay(gameStage);
            GenerateNightMarket(farmLevel);
        }

        public bool ReserveNightMarketSlotForAn(string slotId)
        {
            var slot = NightListings.Find(x => x.SlotId == slotId);
            if (slot == null) return false;
            foreach (var s in NightListings) s.IsReservedByAnLock = false;
            slot.IsReservedByAnLock = true;
            return true;
        }

        public const int NightSlotsPerDay = 8;

        /// <summary>
        /// GDD 20 / 20.1 / 20.2: the day's 8 night stalls. Each sells a lot drawn from NightMarketCatalog (theme biases the
        /// draw: Hoảng Loạn Dịch medicine ×2, Khan Hiếm Hậu Đột Kích ammo/material ×2, Mùa Trang Bị equipment ×2, Mùa Công
        /// Trình blueprints/ammo ×2) and asks 1–2 exact wants of about the same total GTTC (Mùa Heo Rẻ pig wants ×2, Khan
        /// Hiếm Thức Ăn feed wants ×2). 18% of stalls also take gold at GTTC × 1,4 (× 0,85 in Mùa Công Trình), drawn
        /// from tier I–II goods so tier III–IV gold stalls stay near 0%.
        /// </summary>
        /// <summary>GDD 20: "bậc III–IV ≤ 2% số ô" at the night market.</summary>
        public const double HighTierGearShare = 0.02;

        public void GenerateNightMarket(int farmLevel)
        {
            NightMarketListing? reservedListing = NightListings.Find(x => x != null && x.IsReservedByAnLock);
            NightListings.Clear();
            var theme = DayMarket.Theme;
            var offers = NightMarketCatalog.NightOffers(BlueprintSource?.Invoke());
            var lowTier = offers.FindAll(o => o.Tier <= ItemTier.II);
            // GDD 20: gear keeps the share 6 gear entries used to have; gear III–IV stays at ≤ 2% of stalls
            int gearII = offers.Count(o => o.Kind == NightMarketCatalog.Kind.Equipment && o.Tier <= ItemTier.II);
            int gearHigh = offers.Count(o => o.Kind == NightMarketCatalog.Kind.Equipment && o.Tier >= ItemTier.III);
            int others = offers.Count - gearII - gearHigh;
            double Weight(NightMarketCatalog.Entry e)
            {
                if (e.Kind != NightMarketCatalog.Kind.Equipment) return NightOfferWeight(theme, e.Kind);
                if (e.Tier <= ItemTier.II) return NightOfferWeight(theme, e.Kind) * 6.0 / Math.Max(1, gearII);
                return HighTierGearShare / (1.0 - HighTierGearShare) * (others + 6.0) / Math.Max(1, gearHigh);
            }

            for (int i = 0; i < NightSlotsPerDay; i++)
            {
                if (i == 0 && reservedListing != null)
                {
                    NightListings.Add(reservedListing);
                    continue;
                }

                bool acceptsGold = random.NextDouble() < NightMarketBarter.GoldSlotShare; // 18% ô nhận vàng
                var entry = PickWeighted(acceptsGold && lowTier.Count > 0 ? lowTier : offers, Weight);
                NightListings.Add(MakeNightListing(entry, i, acceptsGold, theme, farmLevel));
            }

            // GDD 10.3 "Ô hàng chợ đêm (hiếm)": Lam 5 %, Vàng 2 %, Tím 0,6 %, no Đỏ, and only after the mid-game gate (10.2).
            // Each hit replaces one ordinary stall (never An's reserved one).
            if (MidGameGateOpen)
            {
                foreach (var (paperId, chance) in new[] { ("Paper_Blue", 0.05), ("Paper_Yellow", 0.02), ("Paper_Purple", 0.006) })
                {
                    if (random.NextDouble() >= chance) continue;
                    var paper = NightMarketCatalog.NightOffers(null).Find(o => o.Id == paperId) ?? NightMarketCatalog.PaperEntry(paperId);
                    if (paper == null) continue;
                    int slot = 1 + random.Next(Math.Max(1, NightListings.Count - 1));
                    if (slot >= NightListings.Count || NightListings[slot].IsReservedByAnLock) continue;
                    NightListings[slot] = MakeNightListing(paper, slot, false, theme, farmLevel);
                }
            }
        }

        /// <summary>Set by the engine each hour: papers reach the night stalls only after the mid-game gate (GDD 10.2).</summary>
        public bool MidGameGateOpen { get; set; }

        private NightMarketListing MakeNightListing(NightMarketCatalog.Entry entry, int i, bool acceptsGold, MarketTheme theme, int farmLevel)
        {
                int lot = Math.Max(1, entry.LotQuantity);
                int total = entry.UnitGttc * lot;
                var listing = new NightMarketListing
                {
                    SlotId = $"slot_{i + 1}",
                    OfferedItem = new GameItem { Id = entry.Id, Name = entry.Name, Tier = entry.Tier, ReferenceValueGold = entry.UnitGttc,
                        Category = entry.Category, Description = entry.Description },
                    Quantity = lot,
                    RequestedCategory = "Any",
                    AcceptsGold = acceptsGold,
                    GoldPrice = acceptsGold ? (int)Math.Round(total * NightMarketBarter.GoldSlotPriceRatio * DayMarket.NightGoldPriceMultiplier) : 0,
                    IsReservedByAnLock = false
                };
                listing.Wants.AddRange(BuildNightWants(theme, total, farmLevel));
                listing.MinimumReferenceValue = listing.Wants.Sum(w => w.TotalGttc);
                listing.RequestedItemDescription = DescribeWants(listing);
                return listing;
        }

        private static double NightOfferWeight(MarketTheme theme, NightMarketCatalog.Kind kind)
        {
            switch (theme)
            {
                case MarketTheme.HoangLoanDich: return kind == NightMarketCatalog.Kind.Medicine ? 2.0 : 1.0;
                case MarketTheme.KhanHiemHauDotKich: return kind == NightMarketCatalog.Kind.Ammo || kind == NightMarketCatalog.Kind.Material ? 2.0 : 1.0;
                case MarketTheme.MuaTrangBi: return kind == NightMarketCatalog.Kind.Equipment ? 2.0 : 1.0;
                case MarketTheme.MuaCongTrinh: return kind == NightMarketCatalog.Kind.Blueprint || kind == NightMarketCatalog.Kind.Ammo ? 2.0 : 1.0;
                default: return 1.0;
            }
        }

        private T PickWeighted<T>(IReadOnlyList<T> items, Func<T, double> weight)
        {
            double total = 0;
            for (int i = 0; i < items.Count; i++) total += Math.Max(0, weight(items[i]));
            double r = random.NextDouble() * total;
            for (int i = 0; i < items.Count; i++)
            {
                r -= Math.Max(0, weight(items[i]));
                if (r < 0) return items[i];
            }
            return items[items.Count - 1];
        }

        /// <summary>GTTC of a wanted pig kind under today's market (day price × 0,9; Mùa Heo Rẻ × 1,0).</summary>
        public int EstimateWantedPigGttc(WantedPigKind kind, int farmLevel) =>
            Math.Max(1, NightMarketBarter.PigGttc(this, NightMarketCatalog.TemplatePig(kind), farmLevel));

        private List<NightMarketWant> BuildNightWants(MarketTheme theme, int totalGttc, int farmLevel)
        {
            var wants = new List<NightMarketWant>();
            int count = random.NextDouble() < 0.5 ? 1 : 2;   // GDD 20.1 "1–2 món muốn nhận"
            double firstShare = count == 1 ? 1.0 : 0.6 + random.NextDouble() * 0.2;
            for (int k = 0; k < count; k++)
            {
                double share = k == 0 ? firstShare : 1.0 - firstShare;
                double value = Math.Max(1.0, totalGttc * share);
                NightMarketWant? want = null;
                for (int attempt = 0; attempt < 6 && want == null; attempt++)
                {
                    double pigWeight = theme == MarketTheme.MuaHeoRe ? 2.0 : 1.0;           // "nhiều ô đòi heo"
                    double feedWeight = theme == MarketTheme.KhanHiemThucAn ? 2.0 : 1.0;     // "ô đòi cám"
                    var choices = new List<(WantedPigKind pig, string item, double w)>();
                    foreach (var p in NightMarketCatalog.NightPigWants) choices.Add((p, "", pigWeight));
                    foreach (var (id, kind) in NightMarketCatalog.ItemWants)
                        choices.Add((WantedPigKind.None, id, kind == NightMarketCatalog.Kind.Feed ? feedWeight : 1.0));
                    var pick = PickWeighted(choices, c => c.w);
                    if (wants.Any(w => w.PigKind == pick.pig && w.ItemId == pick.item)) continue;
                    if (pick.pig != WantedPigKind.None)
                    {
                        int unit = EstimateWantedPigGttc(pick.pig, farmLevel);
                        int qty = (int)Math.Round(value / unit);
                        if (qty < 1 || qty > 2) continue;                                   // GDD examples: 1–2 heo
                        want = new NightMarketWant { PigKind = pick.pig, NameVi = NightMarketCatalog.PigKindNameVi(pick.pig),
                            Quantity = qty, UnitGttc = unit, Group = NightMarketBarter.GroupPig };
                    }
                    else
                    {
                        int unit = NightMarketBarter.UnitGttcOfItem(pick.item);
                        var data = QualifiedItemRegistry.GetData(pick.item);
                        if (unit <= 0 || data == null) continue;
                        want = new NightMarketWant { ItemId = data.ItemId, NameVi = data.DisplayName, UnitGttc = unit,
                            Quantity = Math.Max(1, (int)Math.Round(value / unit)), Group = NightMarketBarter.GroupOfItem(pick.item) };
                    }
                }
                if (want != null) wants.Add(want);
            }
            if (wants.Count == 0)
            {
                var data = QualifiedItemRegistry.GetData("BinhHoiLuc");
                int unit = Math.Max(1, NightMarketBarter.UnitGttcOfItem("BinhHoiLuc"));
                wants.Add(new NightMarketWant { ItemId = "BinhHoiLuc", NameVi = data?.DisplayName ?? "Bình Hồi Lực", UnitGttc = unit,
                    Quantity = Math.Max(1, (int)Math.Round(totalGttc / (double)unit)), Group = NightMarketBarter.GroupOfItem("BinhHoiLuc") });
            }
            return wants;
        }

        public static string DescribeWants(NightMarketListing listing)
        {
            string wants = string.Join(" + ", listing.Wants.Select(w => w.Describe()));
            if (listing.IsMysticListing)
                return $"Muốn nhận: {wants} · tổng ≥ {listing.MinimumReferenceValue:N0}G GTTC ({listing.MysticRate:P0}) · không nhận vàng";
            string gold = listing.AcceptsGold ? $" · hoặc {listing.GoldPrice:N0}G vàng" : "";
            return $"Muốn nhận: {wants}{gold} · thay cùng nhóm ≥110% GTTC · phí 5%";
        }

        /// <summary>
        /// GDD 21: Bí nhân's stock for one visit — 2–4 distinct slots from his pool, each asking 130–200% of its GTTC in
        /// the rare things he wants (heo gen "?", heo Quý+, 3–8 heo già, xác heo, vật phẩm bậc III–IV); Hoảng Loạn Dịch
        /// leans to corpses, Mùa Heo Rẻ to elder pigs (×2 weight, ADR-0023).
        /// </summary>
        public List<NightMarketListing> GenerateMysticStock(MarketTheme theme, int night, int farmLevel)
        {
            var pool = NightMarketCatalog.MysticOffers(BlueprintSource?.Invoke());
            int slots = Math.Min(pool.Count, 2 + random.Next(3));
            var stock = new List<NightMarketListing>();
            for (int i = 0; i < slots; i++)
            {
                var entry = pool[random.Next(pool.Count)];
                pool.Remove(entry);
                float rate = MarketCycleThemes.Uniform(random, NightMarketBarter.MysticRateMin, NightMarketBarter.MysticRateMax);
                int required = (int)Math.Ceiling(entry.UnitGttc * rate);
                var kinds = new List<(WantedPigKind kind, double w)>
                {
                    (WantedPigKind.Unidentified, 1.0), (WantedPigKind.QuyPlus, 1.0),
                    (WantedPigKind.Elder, theme == MarketTheme.MuaHeoRe ? 2.0 : 1.0),
                    (WantedPigKind.Corpse, theme == MarketTheme.HoangLoanDich ? 2.0 : 1.0),
                    (WantedPigKind.None, 1.0),   // vật phẩm bậc III–IV
                };
                var kind = PickWeighted(kinds, k => k.w).kind;
                NightMarketWant want;
                if (kind == WantedPigKind.None)
                {
                    want = new NightMarketWant { NameVi = "vật phẩm bậc III–IV", GroupOnly = true, Quantity = 1, UnitGttc = required,
                        Group = NightMarketBarter.GroupItemTierIIIPlus };
                }
                else
                {
                    int unit = EstimateWantedPigGttc(kind, farmLevel);
                    int need = (int)Math.Ceiling(required / (double)unit);
                    int qty = kind == WantedPigKind.Elder ? Math.Clamp(need, 3, 8)              // GDD 21: 3–8 heo già
                            : kind == WantedPigKind.Corpse ? Math.Clamp(need, 1, 3)            // ADR-0023
                            : 1;
                    want = new NightMarketWant { PigKind = kind, NameVi = NightMarketCatalog.PigKindNameVi(kind), Quantity = qty,
                        UnitGttc = unit, Group = NightMarketBarter.GroupPig };
                }
                var listing = new NightMarketListing
                {
                    SlotId = $"binhan_{night}_{i + 1}",
                    OfferedItem = new GameItem { Id = entry.Id, Name = entry.Name, Tier = entry.Tier, ReferenceValueGold = entry.UnitGttc,
                        Category = entry.Category, Description = entry.Description },
                    Quantity = 1,
                    RequestedCategory = "Any",
                    MinimumReferenceValue = required,
                    AcceptsGold = false,
                    GoldPrice = 0,
                    IsMysticListing = true,
                    MysticRate = rate,
                };
                listing.Wants.Add(want);
                listing.RequestedItemDescription = DescribeWants(listing);
                stock.Add(listing);
            }
            return stock;
        }

        private int mysticPlannedDay = -1;
        private int mysticPlannedStart = -1;   // night-hour offset from 18:00, -1 = not tonight

        /// <summary>
        /// GDD 21: Bí nhân comes on one random window of the night (18:00–04:00) and stays exactly 2 game hours.
        /// The chance is rolled ONCE per night: 3% × cycle factor (Hoảng Loạn Dịch ×2.0, Mùa Heo Rẻ ×1.5, else ×1.0)
        /// × (1 + ÁLSK/100), at most 25%. Call every game hour; the farm day runs 06:00 → 06:00.
        /// </summary>
        public void UpdateMysticMerchantHour(int day, int hour, int eventPressure, MarketTheme theme)
        {
            int nightHour = hour >= 18 ? hour - 18 : (hour < 6 ? hour + 6 : -1);
            if (nightHour < 0)
            {
                MysticMerchant.IsActive = false;
                MysticMerchant.DurationHoursRemaining = 0;
                return;
            }
            if (mysticPlannedDay != day)
            {
                mysticPlannedDay = day;
                double factor = MarketCycleThemes.Get(theme).MysticFactor;   // Hoảng Loạn Dịch ×2,0, Mùa Heo Rẻ ×1,5, else ×1,0
                double chance = Math.Min(0.25, 0.03 * factor * (1.0 + Math.Max(0, eventPressure) / 100.0));
                mysticPlannedStart = random.NextDouble() < chance ? random.Next(0, 9) : -1;   // starts 18:00..02:00, gone by 04:00
                MysticMerchant.StockNight = day;
                MysticMerchant.Stock.Clear();   // same list instance: VillageManager's Bí nhân counter reads it live
                if (mysticPlannedStart >= 0) MysticMerchant.Stock.AddRange(GenerateMysticStock(theme, day, 1));
                MysticMerchant.OfferedItems = MysticMerchant.Stock.Select(l => l.OfferedItem).ToList();
            }
            bool active = mysticPlannedStart >= 0 && nightHour >= mysticPlannedStart && nightHour < mysticPlannedStart + 2;
            MysticMerchant.IsActive = active;
            MysticMerchant.DurationHoursRemaining = active ? mysticPlannedStart + 2 - nightHour : 0;
            if (active)
            {
                MysticMerchant.SpawnTimeHour = (18 + mysticPlannedStart) % 24;
                MysticMerchant.DistanceFromBoundaryMeters = 25;
            }
        }

        /// <summary>Legacy single roll kept for older callers/tests; the engine uses UpdateMysticMerchantHour.</summary>
        public bool RollMysticMerchant(int currentHour, int eventPressure)
        {
            bool isNight = currentHour >= 18 || currentHour < 4;
            if (!isNight)
            {
                MysticMerchant.IsActive = false;
                return false;
            }

            double chance = Math.Min(0.25, 0.03 * (1.0 + eventPressure / 100.0));
            if (random.NextDouble() < chance)
            {
                MysticMerchant.IsActive = true;
                MysticMerchant.SpawnTimeHour = currentHour;
                MysticMerchant.DurationHoursRemaining = 2; // Tồn tại 2h
                MysticMerchant.DistanceFromBoundaryMeters = 25;
                return true;
            }
            return false;
        }
    }
}
