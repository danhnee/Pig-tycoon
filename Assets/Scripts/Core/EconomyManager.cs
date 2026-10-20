using System;
using System.Collections.Generic;

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
    }

    [Serializable]
    public class MysticMerchantState
    {
        public bool IsActive;
        public int SpawnTimeHour;
        public int DurationHoursRemaining;
        public int DistanceFromBoundaryMeters;
        public List<GameItem> OfferedItems = new List<GameItem>();
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
        /// GDD 20.2: Áp dụng 8 chủ đề chu kỳ thị trường (MarketTheme).
        /// </summary>
        public void ApplyMarketCycleTheme(MarketTheme theme)
        {
            DayMarket.Theme = theme;
            DayMarket.ThemePriceMultiplier = 1.0f;
            DayMarket.FeedPriceMultiplier = 1.0f;
            DayMarket.PigletBuyPriceMultiplier = 1.0f;
            DayMarket.EquipmentPriceMultiplier = 1.0f;
            DayMarket.BuildingMaterialPriceMultiplier = 1.0f;
            DayMarket.QuarantineFeePerPig = 60;

            switch (theme)
            {
                case MarketTheme.CanBang:
                    break;
                case MarketTheme.MuaHeoRe:
                    DayMarket.PigletBuyPriceMultiplier = 0.75f;
                    DayMarket.ThemePriceMultiplier = 0.85f;
                    break;
                case MarketTheme.ConSotThit:
                    DayMarket.ThemePriceMultiplier = 1.30f;
                    DayMarket.FeedPriceMultiplier = 1.10f;
                    break;
                case MarketTheme.KhanHiemThucAn:
                    DayMarket.FeedPriceMultiplier = 1.40f;
                    DayMarket.ThemePriceMultiplier = 1.05f;
                    break;
                case MarketTheme.HoangLoanDich:
                    DayMarket.ThemePriceMultiplier = 0.70f;
                    DayMarket.QuarantineFeePerPig = 90;
                    break;
                case MarketTheme.KhanHiemHauDotKich:
                    DayMarket.ThemePriceMultiplier = 1.15f;
                    DayMarket.BuildingMaterialPriceMultiplier = 1.25f;
                    break;
                case MarketTheme.MuaTrangBi:
                    DayMarket.EquipmentPriceMultiplier = 0.80f;
                    break;
                case MarketTheme.MuaCongTrinh:
                    DayMarket.BuildingMaterialPriceMultiplier = 0.80f;
                    break;
            }
        }

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

            if (!isQuarantined && pig.ActiveDisease.HasValue)
            {
                unquarantinedSickContaminationDetected = true;
                DayMarket.UnquarantinedSickPenaltyDaysRemaining = Math.Max(DayMarket.UnquarantinedSickPenaltyDaysRemaining, 3);
            }
        }

        /// <summary>
        /// Chuyển ngày cho thị trường: giảm số ngày phạt bán heo bệnh lậu, xóa bộ đếm ngày, giữ ô Chợ Đêm đã khóa của An.
        /// </summary>
        public void AdvanceMarketDay(int farmLevel, bool revealNextCycleTheme = false)
        {
            DayMarket.CurrentPigsSoldToday = 0;
            DayMarket.BreedSoldTodayCounts.Clear();
            if (DayMarket.UnquarantinedSickPenaltyDaysRemaining > 0)
            {
                DayMarket.UnquarantinedSickPenaltyDaysRemaining -= 1;
            }
            DayMarket.IsNextCycleThemeRevealed = revealNextCycleTheme;
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

        public void GenerateNightMarket(int farmLevel)
        {
            NightMarketListing? reservedListing = NightListings.Find(x => x != null && x.IsReservedByAnLock);
            NightListings.Clear();
            var sampleItems = new List<GameItem>
            {
                new GameItem { Id = "TraLaVong", Name = "Trà Lá Vông", Tier = ItemTier.I, ReferenceValueGold = 650, Category = "Consumable", Description = "Ngủ ngắn 3h" },
                new GameItem { Id = "ThuocAnThan", Name = "Thuốc An Thần", Tier = ItemTier.II, ReferenceValueGold = 1400, Category = "Consumable", Description = "Ngủ chuẩn xóa cooldown" },
                new GameItem { Id = "BinhHoiLuc", Name = "Bình Hồi Lực", Tier = ItemTier.I, ReferenceValueGold = 90, Category = "Consumable", Description = "Hoàn 40 Stamina" },
                new GameItem { Id = "BanVeNoXuyenVan", Name = "Bản vẽ Nỏ Xuyên Vân", Tier = ItemTier.II, ReferenceValueGold = 2400, Category = "Blueprint", Description = "Bản vẽ công trình" }
            };

            for (int i = 0; i < 8; i++)
            {
                if (i == 0 && reservedListing != null)
                {
                    NightListings.Add(reservedListing);
                    continue;
                }

                var item = sampleItems[i % sampleItems.Count];
                bool acceptsGold = random.NextDouble() < 0.18; // 18% ô nhận vàng
                int goldPrice = acceptsGold ? (int)Math.Round(item.ReferenceValueGold * 1.4f) : 0;

                NightListings.Add(new NightMarketListing
                {
                    SlotId = $"slot_{i + 1}",
                    OfferedItem = item,
                    Quantity = 1,
                    RequestedItemDescription = acceptsGold ? $"Chấp nhận Vàng hoặc trao đổi {item.Category}" : $"Đòi hỏi {item.Category} tương đương",
                    RequestedCategory = item.Category,
                    MinimumReferenceValue = item.ReferenceValueGold,
                    AcceptsGold = acceptsGold,
                    GoldPrice = goldPrice,
                    IsReservedByAnLock = false
                });
            }
        }

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
