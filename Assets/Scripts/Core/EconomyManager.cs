using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameItem
    {
        public string Id;
        public string Name;
        public ItemTier Tier;
        public int ReferenceValueGold; // GTTC
        public string Category; // Pig, Material, Consumable, Blueprint, Special
        public string Description;
    }

    [Serializable]
    public class DayMarketState
    {
        public MarketTheme Theme = MarketTheme.CanBang;
        public float ThemePriceMultiplier = 1.0f;
        public int DailyPurchasingPowerCap = 40;
        public int CurrentPigsSoldToday = 0;
        public int QuarantineFeePerPig = 60;
    }

    [Serializable]
    public class NightMarketListing
    {
        public string SlotId;
        public GameItem OfferedItem;
        public int Quantity;
        public string RequestedItemDescription;
        public string RequestedCategory;
        public int MinimumReferenceValue;
        public bool AcceptsGold;
        public int GoldPrice;
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

        private readonly Random random = new Random();

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

            int finalPrice = (int)Math.Round(baseRefPrice * meatMult * stageMult * breedMult * DayMarket.ThemePriceMultiplier * purchasingPressure);
            return (Math.Max(10, finalPrice), noQuarantineRate);
        }

        public void GenerateNightMarket(int farmLevel)
        {
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
                    GoldPrice = goldPrice
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
