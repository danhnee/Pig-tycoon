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
        public FinancialLedger Ledger { get; private set; } = new FinancialLedger();
        public FarmFeedInventory FeedInventory { get; private set; } = new FarmFeedInventory();

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

        /// <summary>
        /// Mua cám chăn nuôi tại Chợ Ngày bằng Vàng (GDD 20, 22).
        /// Tiêu hao vàng từ trang trại, cộng cám vào kho, và ghi nhận giao dịch vào Sổ Cái (Ledger).
        /// </summary>
        public (bool success, int goldSpent, string message) BuyFeedAtDayMarket(
            Farm farm,
            FeedTypeId feedType,
            int packageKg,
            int day = 1,
            int hour = 8,
            int minute = 0)
        {
            if (farm == null) return (false, 0, "Không tìm thấy dữ liệu trang trại!");
            if (packageKg <= 0) return (false, 0, "Khối lượng cám phải lớn hơn 0!");

            var feedDef = FeedDatabase.GetFeed(feedType);
            int totalCost = packageKg * feedDef.PricePerKgGold;

            if (farm.Gold < totalCost)
            {
                return (false, 0, $"Không đủ Vàng! Cần {totalCost}G để mua {packageKg}kg {feedDef.Name} (hiện có: {farm.Gold}G).");
            }

            farm.SpendGold(totalCost);
            FeedInventory.AddStock(feedType, packageKg);

            // Ghi nhận vào Sổ Cái Tài Chính (Ledger)
            Ledger.RecordTransaction(
                day: day,
                hour: hour,
                minute: minute,
                type: TransactionType.Expense,
                category: TransactionCategory.MuaCam,
                amountGold: totalCost,
                balanceAfter: farm.Gold,
                description: $"Mua {packageKg}kg {feedDef.Name} ({feedDef.PricePerKgGold}G/kg)"
            );

            return (true, totalCost, $"Đã mua {packageKg}kg {feedDef.Name} với giá {totalCost}G.");
        }

        /// <summary>
        /// Tính toán Báo cáo Ngân Sách Kép (Dual Budget Report - GDD 22):
        /// Ngân sách 1 (Tiền mặt Vàng) + Ngân sách 2 (Kho Giá Trị Tham Chiếu GTTC của đàn heo & cám).
        /// </summary>
        public DualBudgetReport CalculateDualBudgetReport(Farm farm, List<Pig> pigs, int day = 1)
        {
            var dailySummary = Ledger.GetDailySummary(day);

            int totalHerdValue = 0;
            if (pigs != null)
            {
                foreach (var p in pigs)
                {
                    if (p != null && p.IsAlive)
                    {
                        var (price, _) = CalculatePigSalePrice(p, farm?.FarmLevel ?? 1);
                        // Giá trị tham chiếu GTTC heo tính bằng 90% giá chợ ngày theo GDD 20.1
                        totalHerdValue += (int)Math.Round(price * 0.90f);
                    }
                }
            }

            int feedValue = FeedInventory.CalculateTotalFeedValueGold();
            int cashGold = farm?.Gold ?? 0;

            return new DualBudgetReport
            {
                CashGoldBalance = cashGold,
                TodayIncomeGold = dailySummary.TotalIncomeGold,
                TodayExpenseGold = dailySummary.TotalExpenseGold,
                TotalHerdEstimatedValueGold = totalHerdValue,
                TotalFeedStockValueGold = feedValue,
                TotalMaterialsValueGold = 0
            };
        }
    }
}
