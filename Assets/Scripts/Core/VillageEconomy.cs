using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum VillagerId
    {
        BaSau,      // Bà Sáu - Quán Nước & Bếp Ăn Đầu Làng
        TamVa,      // Tám Vá - Thợ Mộc & Sửa Chữa Rào Trại
        CoDung,     // Cô Dung - Tiệm Thuốc Thú Y & Dược Liệu
        AnhCa,      // Anh Ca - Quản Lý Trạm Cân "6+1" & Chợ Phiên
        OngTon,     // Ông Tồn - Trọng Tài Xóm & Người Giữ Sổ Đen
        BiNhan      // Bí Nhân - Thương Nhân Nửa Người Nửa Quỷ Ngoài Rào
    }

    [Serializable]
    public class VillagerProfile
    {
        public VillagerId Id { get; set; }
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string LocationDescription { get; set; } = "";
        public string AtmosphereQuote { get; set; } = "";
        public bool IsOpenDay { get; set; } = true;
        public bool IsOpenNight { get; set; } = false;
        public List<NightMarketListing> Listings { get; set; } = new List<NightMarketListing>();
    }

    [Serializable]
    public class VillageManager
    {
        public Dictionary<VillagerId, VillagerProfile> Villagers { get; private set; } = new Dictionary<VillagerId, VillagerProfile>();

        public VillageManager()
        {
            InitializeVillagers();
        }

        public void InitializeVillagers()
        {
            Villagers.Clear();

            // 1. BÀ SÁU - Quán Nước Đầu Làng
            var baSau = new VillagerProfile
            {
                Id = VillagerId.BaSau,
                Name = "Bà Sáu",
                Title = "Chủ Quán Nước & Bếp Ăn Đầu Làng",
                LocationDescription = "Quán mái lá đơn sơ dưới gốc thị già đầu dốc đất đỏ. Khói bếp mù mịt mùi rơm ẩm.",
                AtmosphereQuote = "Ăn đi cậu. Cái bát úp đó chớ có lật ngửa, khách quen chưa tới. Chuyện hôm qua hả? Mai trời nắng.",
                IsOpenDay = true,
                IsOpenNight = true
            };
            baSau.Listings.Add(new NightMarketListing
            {
                SlotId = "basau_chao",
                OfferedItem = new GameItem { Id = "ChaoNongBaSau", Name = "Bát Cháo Nóng Đầu Làng", Tier = ItemTier.I, ReferenceValueGold = 60, Category = "Consumable", Description = "Bát cháo hành ấm bụng, phục hồi 50 Stamina và làm dịu tinh thần." },
                Quantity = 1,
                RequestedCategory = "Consumable",
                RequestedItemDescription = "Chấp nhận 84G Vàng hoặc 1 món đồ ăn/cám tương đương (≥ 60G GTTC)",
                MinimumReferenceValue = 60,
                AcceptsGold = true,
                GoldPrice = 84
            });
            baSau.Listings.Add(new NightMarketListing
            {
                SlotId = "basau_tra",
                OfferedItem = new GameItem { Id = "TraDangBoRia", Name = "Ấm Trà Đắng Bờ Rìa", Tier = ItemTier.I, ReferenceValueGold = 120, Category = "Consumable", Description = "Trà hãm từ lá đắng mọc ven suối, xóa 1 tầng Mệt Dội và tỉnh táo ngay tức thì." },
                Quantity = 1,
                RequestedCategory = "Consumable",
                RequestedItemDescription = "Chấp nhận 168G Vàng hoặc thảo mộc đổi ngang (≥ 120G GTTC)",
                MinimumReferenceValue = 120,
                AcceptsGold = true,
                GoldPrice = 168
            });
            Villagers[VillagerId.BaSau] = baSau;

            // 2. TÁM VÁ - Thợ Mộc & Sửa Rào
            var tamVa = new VillagerProfile
            {
                Id = VillagerId.TamVa,
                Name = "Tám Vá",
                Title = "Thợ Mộc Xóm Bờ Rìa",
                LocationDescription = "Gian xưởng bừa bộn phoi bào và cọc gỗ gãy. Tiếng đục đẽo cọc cạch không ngừng.",
                AtmosphereQuote = "Rào toạc ba thước à? Gió lốc cục bộ thôi. Tôi ghi biên bản 'thời tiết' rồi, lát vác búa lên đóng lại cho.",
                IsOpenDay = true,
                IsOpenNight = false
            };
            tamVa.Listings.Add(new NightMarketListing
            {
                SlotId = "tamva_go",
                OfferedItem = new GameItem { Id = "BoGoXeGiaCo", Name = "Kiện Gỗ Xẻ Gia Cố (x10)", Tier = ItemTier.I, ReferenceValueGold = 300, Category = "Material", Description = "Gỗ xẻ dày dặn tẩm dầu hắc chống mối mọt và móng vuốt cào phá." },
                Quantity = 1,
                RequestedCategory = "Material",
                RequestedItemDescription = "Đòi hỏi Da Heo Thiết Bì hoặc Kim Loại phế liệu (≥ 300G GTTC)",
                MinimumReferenceValue = 300,
                AcceptsGold = true,
                GoldPrice = 420
            });
            tamVa.Listings.Add(new NightMarketListing
            {
                SlotId = "tamva_coc",
                OfferedItem = new GameItem { Id = "CocRaoMoi", Name = "Cọc Rào Lõi Sắt (x5)", Tier = ItemTier.II, ReferenceValueGold = 250, Category = "Material", Description = "Cọc rào gỗ lõi sắt chắc chắn, dựng rào tăng +150 HP phòng tuyến." },
                Quantity = 1,
                RequestedCategory = "Material",
                RequestedItemDescription = "Thuần Barter: Yêu cầu Đinh Rỉ hoặc Da Thú (≥ 250G GTTC)",
                MinimumReferenceValue = 250,
                AcceptsGold = false,
                GoldPrice = 0
            });
            Villagers[VillagerId.TamVa] = tamVa;

            // 3. CÔ DUNG - Tiệm Thuốc Thú Y & Dược Liệu
            var coDung = new VillagerProfile
            {
                Id = VillagerId.CoDung,
                Name = "Cô Dung",
                Title = "Dược Sư Thú Y Bờ Rìa",
                LocationDescription = "Ngôi nhà gỗ ngào ngạt mùi thuốc nam phơi khô và rễ cây ngâm rượu.",
                AtmosphereQuote = "Heo mà bồn chồn run chân thì chớ có nhìn lâu vào mắt nó. Hòa gói này vào máng nước rồi đi ngủ sớm.",
                IsOpenDay = true,
                IsOpenNight = true
            };
            coDung.Listings.Add(new NightMarketListing
            {
                SlotId = "dung_botkhangdich",
                OfferedItem = new GameItem { Id = "BotKhangDich", Name = "Gói Thuốc Bột Kháng Dịch (x3)", Tier = ItemTier.II, ReferenceValueGold = 450, Category = "Consumable", Description = "Hòa vào máng nước giúp cả đàn tăng 40% đề kháng, chặn đứng nguy cơ lây dịch." },
                Quantity = 1,
                RequestedCategory = "Consumable",
                RequestedItemDescription = "Chấp nhận 630G Vàng hoặc Cám Dược Liệu/Nanh Heo (≥ 450G GTTC)",
                MinimumReferenceValue = 450,
                AcceptsGold = true,
                GoldPrice = 630
            });
            coDung.Listings.Add(new NightMarketListing
            {
                SlotId = "dung_anthan",
                OfferedItem = new GameItem { Id = "ThuocAnThan", Name = "Thuốc An Thần", Tier = ItemTier.II, ReferenceValueGold = 1400, Category = "Consumable", Description = "Thuốc ngủ chuẩn liều cao giúp Khoa xóa sạch thời gian hồi chiêu ngủ." },
                Quantity = 1,
                RequestedCategory = "Material",
                RequestedItemDescription = "Thuần Barter: Yêu cầu Nanh Heo Xích Mao hoặc Da Đanh (≥ 1400G GTTC)",
                MinimumReferenceValue = 1400,
                AcceptsGold = false,
                GoldPrice = 0
            });
            Villagers[VillagerId.CoDung] = coDung;

            // 4. ANH CA - Quản Lý Trạm Cân "6+1"
            var anhCa = new VillagerProfile
            {
                Id = VillagerId.AnhCa,
                Name = "Anh Ca",
                Title = "Người Trực Trạm Cân '6+1' & Chợ Phiên",
                LocationDescription = "Chiếc bàn gỗ đen bóng đặt dưới đình làng cũ. Cân đồng chỉ có vạch '6' và nhảy sang '8'.",
                AtmosphereQuote = "Tên tôi trên giấy khai sinh kệ tôi, ở xóm này gọi Ca là được rồi. Đặt heo lên bàn cân đi, thừa thiếu tính sau.",
                IsOpenDay = true,
                IsOpenNight = true
            };
            Villagers[VillagerId.AnhCa] = anhCa;

            // 5. ÔNG TỒN - Thủ Kho & Người Giữ Sổ
            var ongTon = new VillagerProfile
            {
                Id = VillagerId.OngTon,
                Name = "Ông Tồn",
                Title = "Thủ Kho & Trọng Tài Xóm Bờ Rìa",
                LocationDescription = "Ngồi trầm ngâm bên ngọn đèn dầu, tay cầm cuốn sổ da đen chép từng dấu nêm lạ.",
                AtmosphereQuote = "Có những thứ không có trong cột thu chi, nhưng thiếu một gạch là đêm đó rào lại lung lay.",
                IsOpenDay = true,
                IsOpenNight = true
            };
            Villagers[VillagerId.OngTon] = ongTon;
        }

        public VillagerProfile? GetVillager(VillagerId id)
        {
            if (Villagers.TryGetValue(id, out var profile)) return profile;
            return null;
        }

        public (bool success, GameItem? item, int excessValueGold, string message) TradeWithVillager(
            VillagerId villagerId,
            string slotId,
            Farm farm,
            EconomyManager economy,
            List<GameItem>? offeredItems,
            Pig? offeredPig,
            bool payWithGold,
            int day,
            int hour,
            int minute)
        {
            if (!Villagers.TryGetValue(villagerId, out var profile))
            {
                return (false, null, 0, "Dân làng này không tồn tại hoặc đã rời đi!");
            }

            bool isNight = hour >= 18 || hour < 6;
            if (isNight && !profile.IsOpenNight)
            {
                return (false, null, 0, $"{profile.Name} đã đóng cửa then cài đi ngủ. Ban đêm không tiếp khách.");
            }
            if (!isNight && !profile.IsOpenDay)
            {
                return (false, null, 0, $"{profile.Name} chỉ mở cửa vào ban đêm.");
            }

            var listing = profile.Listings.Find(l => l.SlotId == slotId);
            if (listing == null)
            {
                return (false, null, 0, $"Không tìm thấy món hàng {slotId} tại chỗ của {profile.Name}!");
            }
            if (listing.IsSoldOut)
            {
                return (false, null, 0, $"Món hàng {listing.OfferedItem.Name} đã được đổi hết hôm nay!");
            }

            // Thanh toán bằng Vàng
            if (payWithGold)
            {
                if (!listing.AcceptsGold)
                {
                    return (false, null, 0, $"{profile.Name} từ chối nhận Vàng mặt cho món này! Chỉ chấp nhận Trao Đổi (Barter).");
                }
                if (farm.Gold < listing.GoldPrice)
                {
                    return (false, null, 0, $"Không đủ Vàng! Cần {listing.GoldPrice}G (hiện có: {farm.Gold}G).");
                }

                farm.SpendGold(listing.GoldPrice);
                listing.IsSoldOut = true;

                economy.Ledger.RecordTransaction(
                    day: day,
                    hour: hour,
                    minute: minute,
                    type: TransactionType.Expense,
                    category: TransactionCategory.DauGia,
                    amountGold: listing.GoldPrice,
                    balanceAfter: farm.Gold,
                    description: $"Giao dịch với {profile.Name}: {listing.OfferedItem.Name}"
                );

                return (true, listing.OfferedItem, 0, $"Đã mua thành công {listing.OfferedItem.Name} từ {profile.Name} với giá {listing.GoldPrice}G.");
            }

            // Trao đổi Barter
            int totalOfferedGTTC = 0;
            if (offeredItems != null)
            {
                foreach (var it in offeredItems)
                {
                    if (it == null || it.Quantity <= 0) continue;
                    if (listing.RequestedCategory != "Any" && listing.RequestedCategory != "Pig")
                    {
                        if (!string.Equals(it.Category, listing.RequestedCategory, StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, null, 0, $"{profile.Name} chỉ cần món thuộc danh mục [{listing.RequestedCategory}], bạn đang đưa [{it.Name} ({it.Category})].");
                        }
                    }
                    totalOfferedGTTC += it.ReferenceValueGold * it.Quantity;
                }
            }

            if (offeredPig != null)
            {
                if (!offeredPig.IsAlive)
                {
                    return (false, null, 0, "Dân làng Bờ Rìa không nhận heo đã chết!");
                }
                if (offeredPig.Rarity == GeneRarity.DiBien)
                {
                    return (false, null, 0, $"{profile.Name} hoảng hốt xua tay từ chối heo Dị Biến! Chỉ có Bí Nhân ngoài rào mới dám nhận.");
                }

                var (price, _) = economy.CalculatePigSalePrice(offeredPig, farm.FarmLevel);
                int pigGttc = (int)Math.Round(price * 0.90f);
                totalOfferedGTTC += pigGttc;
            }

            if (listing.RequestedCategory == "Pig" && offeredPig == null)
            {
                return (false, null, 0, $"{profile.Name} yêu cầu đổi bằng Heo sống (≥ {listing.MinimumReferenceValue}G GTTC)!");
            }

            if (totalOfferedGTTC < listing.MinimumReferenceValue)
            {
                return (false, null, 0, $"Tổng GTTC chưa đủ! Yêu cầu: {listing.MinimumReferenceValue}G GTTC, bạn mới đưa: {totalOfferedGTTC}G GTTC.");
            }

            listing.IsSoldOut = true;
            int excessValue = totalOfferedGTTC - listing.MinimumReferenceValue;
            string excessNote = excessValue > 0 ? $" (Dư {excessValue}G GTTC không hoàn lại theo luật Bờ Rìa)" : "";

            return (true, listing.OfferedItem, excessValue, $"Đã đổi thành công {listing.OfferedItem.Name} từ {profile.Name}! Tổng GTTC: {totalOfferedGTTC}G{excessNote}.");
        }
    }
}
