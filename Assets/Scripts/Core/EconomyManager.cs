using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public ItemTier Tier { get; set; } = ItemTier.I;
        public int ReferenceValueGold { get; set; } = 0; // GTTC
        public string Category { get; set; } = "Material"; // Pig, Material, Consumable, Blueprint, Special
        public string Description { get; set; } = "";
        public int Quantity { get; set; } = 1;

        public GameItem Clone(int newQuantity = 1)
        {
            return new GameItem
            {
                Id = Id,
                Name = Name,
                Tier = Tier,
                ReferenceValueGold = ReferenceValueGold,
                Category = Category,
                Description = Description,
                Quantity = newQuantity
            };
        }
    }

    public static class ItemCatalog
    {
        public static readonly List<GameItem> NightMarketPool = new List<GameItem>
        {
            new GameItem { Id = "TraLaVong", Name = "Trà Lá Vông", Tier = ItemTier.I, ReferenceValueGold = 650, Category = "Consumable", Description = "Ngủ ngắn 3h hồi sức ngay lập tức." },
            new GameItem { Id = "ThuocAnThan", Name = "Thuốc An Thần", Tier = ItemTier.II, ReferenceValueGold = 1400, Category = "Consumable", Description = "Ngủ chuẩn xóa toàn bộ cooldown ngủ." },
            new GameItem { Id = "BinhHoiLuc", Name = "Bình Hồi Lực", Tier = ItemTier.I, ReferenceValueGold = 90, Category = "Consumable", Description = "Hoàn lại 40 Stamina tức thì." },
            new GameItem { Id = "ThuocBoHuyet", Name = "Thuốc Bổ Huyết", Tier = ItemTier.II, ReferenceValueGold = 320, Category = "Consumable", Description = "Hồi phục 80 HP khi chiến đấu." },
            new GameItem { Id = "LinhDanNguHanh", Name = "Linh Đan Ngũ Hành", Tier = ItemTier.III, ReferenceValueGold = 2800, Category = "Consumable", Description = "Tẩy rửa tủy tạng, cải thiện thể chất đàn heo." },
            new GameItem { Id = "BanVeNoXuyenVan", Name = "Bản vẽ Nỏ Xuyên Vân", Tier = ItemTier.II, ReferenceValueGold = 2400, Category = "Blueprint", Description = "Bản vẽ công trình phòng thủ nỏ liên châu." },
            new GameItem { Id = "BanVeCuuTieuLoiPhao", Name = "Bản vẽ Cửu Tiêu Lôi Pháo", Tier = ItemTier.III, ReferenceValueGold = 7500, Category = "Blueprint", Description = "Bản vẽ vũ khí tối thượng Bạch Vân." },
            new GameItem { Id = "BanVeThoatNuoc2", Name = "Bản vẽ Thoát Nước Cấp 2", Tier = ItemTier.II, ReferenceValueGold = 1800, Category = "Blueprint", Description = "Cải thiện tiêu thoát nước, giảm ô nhiễm trại." },
            new GameItem { Id = "GoTinhChe", Name = "Gỗ Tinh Chế", Tier = ItemTier.I, ReferenceValueGold = 30, Category = "Material", Description = "Gỗ sấy cao cấp dùng gia cố hàng rào kiên cố." },
            new GameItem { Id = "ThepCuongLuc", Name = "Thép Cường Lực", Tier = ItemTier.II, ReferenceValueGold = 120, Category = "Material", Description = "Hợp kim thép chịu lực chống chọi quái phá hoại." },
            new GameItem { Id = "PhaLeUFO", Name = "Pha Lê Năng Lượng UFO", Tier = ItemTier.III, ReferenceValueGold = 950, Category = "Material", Description = "Mảnh vỡ động cơ UFO phát sáng lân tinh." },
            new GameItem { Id = "DaHeoThietBi", Name = "Da Heo Thiết Bì Đanh", Tier = ItemTier.II, ReferenceValueGold = 450, Category = "Material", Description = "Lớp bì dày như giáp sắt của heo Thiết Bì." },
            new GameItem { Id = "NanhXichMao", Name = "Nanh Heo Xích Mao", Tier = ItemTier.II, ReferenceValueGold = 520, Category = "Material", Description = "Nanh nhọn hoang dã chứa huyết khí hung bạo." }
        };

        public static readonly List<GameItem> MysticPool = new List<GameItem>
        {
            new GameItem { Id = "BanVeCuuTieuLoiPhao", Name = "Bản vẽ Cửu Tiêu Lôi Pháo", Tier = ItemTier.III, ReferenceValueGold = 7500, Category = "Blueprint", Description = "Thần khí phòng thủ Bạch Vân tối thượng." },
            new GameItem { Id = "TinhHoaHuThe", Name = "Tinh Hoa Hư Thể Cổ Đại", Tier = ItemTier.IV, ReferenceValueGold = 8000, Category = "Special", Description = "Phôi gen dị biến hư vô, mở khóa tiềm năng tối thượng." },
            new GameItem { Id = "HatGiongNghinNam", Name = "Hạt Giống Dược Điển Cổ", Tier = ItemTier.III, ReferenceValueGold = 3500, Category = "Special", Description = "Dược liệu quý giúp đàn heo miễn nhiễm mọi dịch bệnh." },
            new GameItem { Id = "DanTayTuyDiThu", Name = "Đan Dược Tẩy Tủy Dị Thú", Tier = ItemTier.III, ReferenceValueGold = 4500, Category = "Special", Description = "Tẩy sạch toàn bộ tật xấu bẩm sinh của heo giống." }
        };
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
        public string SlotId { get; set; } = "";
        public GameItem OfferedItem { get; set; } = new GameItem();
        public int Quantity { get; set; } = 1;
        public string RequestedItemDescription { get; set; } = "";
        public string RequestedCategory { get; set; } = "Any";
        public int MinimumReferenceValue { get; set; } = 0;
        public bool AcceptsGold { get; set; } = false;
        public int GoldPrice { get; set; } = 0;
        public bool IsSoldOut { get; set; } = false;
    }

    [Serializable]
    public class MysticMerchantState
    {
        public bool IsActive { get; set; } = false;
        public int SpawnTimeHour { get; set; } = 0;
        public int DurationHoursRemaining { get; set; } = 2;
        public int DistanceFromBoundaryMeters { get; set; } = 25;
        public List<GameItem> OfferedItems { get; set; } = new List<GameItem>();
        public List<NightMarketListing> Listings { get; set; } = new List<NightMarketListing>();
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
            var pool = ItemCatalog.NightMarketPool;

            int goldSlotsTarget = 1; // 18% ~ 1/8 slot
            int goldAssigned = 0;

            for (int i = 0; i < 8; i++)
            {
                var item = pool[(i + (farmLevel * 2)) % pool.Count].Clone();

                bool acceptsGold = false;
                if (goldAssigned < goldSlotsTarget && (i == 0 || random.NextDouble() < 0.18))
                {
                    acceptsGold = true;
                    goldAssigned++;
                }

                int goldPrice = acceptsGold ? (int)Math.Round(item.ReferenceValueGold * 1.4f) : 0;

                string reqCat = "Any";
                if (!acceptsGold)
                {
                    reqCat = (i % 4) switch
                    {
                        0 => "Material",
                        1 => "Pig",
                        2 => "Consumable",
                        _ => "Blueprint"
                    };
                }

                string desc = acceptsGold
                    ? $"Chấp nhận {goldPrice}G Vàng hoặc trao đổi {item.Category} (≥ {item.ReferenceValueGold}G GTTC)"
                    : (reqCat == "Pig"
                        ? $"Đòi hỏi Heo sống tương đương (≥ {item.ReferenceValueGold}G GTTC)"
                        : $"Đòi hỏi {reqCat} tương đương (≥ {item.ReferenceValueGold}G GTTC)");

                NightListings.Add(new NightMarketListing
                {
                    SlotId = $"slot_{i + 1}",
                    OfferedItem = item,
                    Quantity = 1,
                    RequestedItemDescription = desc,
                    RequestedCategory = reqCat,
                    MinimumReferenceValue = item.ReferenceValueGold,
                    AcceptsGold = acceptsGold,
                    GoldPrice = goldPrice,
                    IsSoldOut = false
                });
            }

            if (goldAssigned == 0 && NightListings.Count > 0)
            {
                NightListings[0].AcceptsGold = true;
                NightListings[0].GoldPrice = (int)Math.Round(NightListings[0].OfferedItem.ReferenceValueGold * 1.4f);
                NightListings[0].RequestedItemDescription = $"Chấp nhận {NightListings[0].GoldPrice}G Vàng hoặc trao đổi {NightListings[0].OfferedItem.Category} (≥ {NightListings[0].OfferedItem.ReferenceValueGold}G GTTC)";
            }
        }

        public void GenerateMysticMerchantStock(int farmLevel)
        {
            MysticMerchant.Listings.Clear();
            MysticMerchant.OfferedItems.Clear();

            var pool = ItemCatalog.MysticPool;
            for (int i = 0; i < pool.Count; i++)
            {
                var item = pool[i].Clone();
                MysticMerchant.OfferedItems.Add(item);

                string reqCat = (i % 2 == 0) ? "Pig" : "Special";
                string desc = reqCat == "Pig"
                    ? $"Đòi hỏi Heo Quý / Dị Biến (≥ {item.ReferenceValueGold}G GTTC)"
                    : $"Đòi hỏi Vật phẩm Đặc biệt / Cấm kỵ (≥ {item.ReferenceValueGold}G GTTC)";

                MysticMerchant.Listings.Add(new NightMarketListing
                {
                    SlotId = $"mystic_slot_{i + 1}",
                    OfferedItem = item,
                    Quantity = 1,
                    RequestedItemDescription = desc,
                    RequestedCategory = reqCat,
                    MinimumReferenceValue = item.ReferenceValueGold,
                    AcceptsGold = false, // Tuyệt đối 0% nhận Vàng
                    GoldPrice = 0,
                    IsSoldOut = false
                });
            }
        }

        public bool RollMysticMerchant(int currentHour, int eventPressure)
        {
            bool isNight = currentHour >= 18 || currentHour < 4;
            if (!isNight)
            {
                MysticMerchant.IsActive = false;
                MysticMerchant.Listings.Clear();
                MysticMerchant.OfferedItems.Clear();
                return false;
            }

            if (MysticMerchant.IsActive)
            {
                return true;
            }

            double chance = Math.Min(0.25, 0.03 * (1.0 + eventPressure / 100.0));
            if (random.NextDouble() < chance)
            {
                MysticMerchant.IsActive = true;
                MysticMerchant.SpawnTimeHour = currentHour;
                MysticMerchant.DurationHoursRemaining = 2; // Tồn tại đúng 2h
                MysticMerchant.DistanceFromBoundaryMeters = 25; // 25m sát ranh giới trại
                GenerateMysticMerchantStock(1);
                return true;
            }
            return false;
        }

        public void ForceSpawnMysticMerchant(int currentHour = 20)
        {
            MysticMerchant.IsActive = true;
            MysticMerchant.SpawnTimeHour = currentHour;
            MysticMerchant.DurationHoursRemaining = 2;
            MysticMerchant.DistanceFromBoundaryMeters = 25;
            GenerateMysticMerchantStock(1);
        }

        public void UpdateMysticMerchant(int currentHour)
        {
            if (!MysticMerchant.IsActive) return;

            bool isNight = currentHour >= 18 || currentHour < 4;
            if (!isNight)
            {
                MysticMerchant.IsActive = false;
                MysticMerchant.Listings.Clear();
                MysticMerchant.OfferedItems.Clear();
                return;
            }

            int hoursPassed = (currentHour - MysticMerchant.SpawnTimeHour + 24) % 24;
            if (hoursPassed >= 2)
            {
                MysticMerchant.IsActive = false;
                MysticMerchant.Listings.Clear();
                MysticMerchant.OfferedItems.Clear();
            }
            else
            {
                MysticMerchant.DurationHoursRemaining = 2 - hoursPassed;
            }
        }

        public (bool success, GameItem? boughtItem, string message) PurchaseNightListingWithGold(
            Farm farm,
            string slotId,
            int day = 1,
            int hour = 20,
            int minute = 0)
        {
            if (farm == null) return (false, null, "Không tìm thấy dữ liệu trang trại!");
            var listing = NightListings.Find(l => l.SlotId == slotId);
            if (listing == null) return (false, null, $"Không tìm thấy ô hàng {slotId} tại Chợ Đêm!");
            if (listing.IsSoldOut) return (false, null, $"Ô hàng {listing.OfferedItem.Name} đã bán hết!");
            if (!listing.AcceptsGold) return (false, null, $"Ô hàng này chỉ chấp nhận Trao Đổi (Barter), không nhận Vàng!");

            if (farm.Gold < listing.GoldPrice)
            {
                return (false, null, $"Không đủ Vàng! Cần {listing.GoldPrice}G (hiện có: {farm.Gold}G).");
            }

            farm.SpendGold(listing.GoldPrice);
            listing.IsSoldOut = true;

            Ledger.RecordTransaction(
                day: day,
                hour: hour,
                minute: minute,
                type: TransactionType.Expense,
                category: TransactionCategory.DauGia,
                amountGold: listing.GoldPrice,
                balanceAfter: farm.Gold,
                description: $"Mua chợ đêm: {listing.OfferedItem.Name} (x{listing.Quantity})"
            );

            return (true, listing.OfferedItem, $"Đã mua thành công {listing.OfferedItem.Name} với giá {listing.GoldPrice}G.");
        }

        public (bool success, GameItem? exchangedItem, int excessValueGold, string message) BarterNightListing(
            string slotId,
            List<GameItem>? offeredItems,
            Pig? offeredPig = null,
            int farmLevel = 1)
        {
            var listing = NightListings.Find(l => l.SlotId == slotId);
            if (listing == null) return (false, null, 0, $"Không tìm thấy ô hàng {slotId} tại Chợ Đêm!");
            if (listing.IsSoldOut) return (false, null, 0, $"Ô hàng {listing.OfferedItem.Name} đã được giao dịch hết!");

            return ExecuteBarter(listing, offeredItems, offeredPig, farmLevel);
        }

        public (bool success, GameItem? exchangedItem, int excessValueGold, string message) BarterMysticMerchantListing(
            string slotId,
            List<GameItem>? offeredItems,
            Pig? offeredPig = null,
            int farmLevel = 1)
        {
            if (!MysticMerchant.IsActive)
            {
                return (false, null, 0, "Bí Nhân hiện không có mặt tại ranh giới trang trại!");
            }

            var listing = MysticMerchant.Listings.Find(l => l.SlotId == slotId);
            if (listing == null) return (false, null, 0, $"Không tìm thấy ô hàng {slotId} của Bí Nhân!");
            if (listing.IsSoldOut) return (false, null, 0, $"Vật phẩm cấm kỵ {listing.OfferedItem.Name} đã được đổi!");

            return ExecuteBarter(listing, offeredItems, offeredPig, farmLevel);
        }

        private (bool success, GameItem? exchangedItem, int excessValueGold, string message) ExecuteBarter(
            NightMarketListing listing,
            List<GameItem>? offeredItems,
            Pig? offeredPig,
            int farmLevel)
        {
            int totalOfferedGTTC = 0;

            if (offeredItems != null)
            {
                foreach (var item in offeredItems)
                {
                    if (item == null || item.Quantity <= 0) continue;

                    if (listing.RequestedCategory != "Any" && listing.RequestedCategory != "Pig")
                    {
                        if (!string.Equals(item.Category, listing.RequestedCategory, StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, null, 0, $"Danh mục không khớp! Gian hàng yêu cầu [{listing.RequestedCategory}], bạn đang nộp [{item.Name} ({item.Category})].");
                        }
                    }

                    totalOfferedGTTC += item.ReferenceValueGold * item.Quantity;
                }
            }

            if (offeredPig != null)
            {
                if (!offeredPig.IsAlive)
                {
                    return (false, null, 0, "Không thể đem heo đã chết đi trao đổi!");
                }

                if (offeredPig.Rarity == GeneRarity.DiBien)
                {
                    if (listing.SlotId.StartsWith("mystic"))
                    {
                        totalOfferedGTTC += 5000;
                    }
                    else
                    {
                        return (false, null, 0, "Chợ đêm bình thường không dám nhận heo Dị Biến! Chỉ có Bí Nhân mới thu mua loài này.");
                    }
                }
                else
                {
                    var (price, _) = CalculatePigSalePrice(offeredPig, farmLevel);
                    int pigGttc = (int)Math.Round(price * 0.90f);
                    totalOfferedGTTC += pigGttc;
                }
            }

            if (listing.RequestedCategory == "Pig" && offeredPig == null)
            {
                return (false, null, 0, $"Gian hàng này yêu cầu trao đổi bằng Heo sống (tối thiểu {listing.MinimumReferenceValue}G GTTC)!");
            }

            if (totalOfferedGTTC < listing.MinimumReferenceValue)
            {
                return (false, null, 0, $"Tổng giá trị tham chiếu (GTTC) chưa đủ! Yêu cầu: {listing.MinimumReferenceValue}G, bạn mới cung cấp: {totalOfferedGTTC}G.");
            }

            listing.IsSoldOut = true;
            int excessValue = totalOfferedGTTC - listing.MinimumReferenceValue;

            string excessNote = excessValue > 0 ? $" (Dư {excessValue}G GTTC không hoàn lại theo luật Chợ Đêm)" : "";
            return (true, listing.OfferedItem, excessValue, $"Trao đổi thành công {listing.OfferedItem.Name}! Tổng GTTC nộp: {totalOfferedGTTC}G{excessNote}.");
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
