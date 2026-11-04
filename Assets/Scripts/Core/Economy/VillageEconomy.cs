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
        BiNhan,     // Bí Nhân - Thương Nhân Nửa Người Nửa Quỷ Ngoài Rào
        ChoDem      // Chợ Đêm thị trấn (GDD 20): EconomyManager.NightListings, trao đổi là chính, 18% ô nhận vàng
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
            anhCa.Listings.Add(new NightMarketListing
            {
                SlotId = "anhca_thep",
                OfferedItem = new GameItem { Id = "ThepCuongLuc", Name = "Thép Cường Lực (x5)", Tier = ItemTier.II, ReferenceValueGold = 600, Category = "Material", Description = "Thép chịu lực siêu bền rèn từ lò than đước, gia cố chuồng và bẫy chống càn." },
                Quantity = 1,
                RequestedCategory = "Material",
                RequestedItemDescription = "Chấp nhận 840G Vàng hoặc Gỗ Xẻ / Da Thú (≥ 600G GTTC)",
                MinimumReferenceValue = 600,
                AcceptsGold = true,
                GoldPrice = 840
            });
            anhCa.Listings.Add(new NightMarketListing
            {
                SlotId = "anhca_daocan",
                OfferedItem = new GameItem { Id = "DaoCanTrong", Name = "Dao Cân Trọng Rã Thịt", Tier = ItemTier.III, ReferenceValueGold = 2400, Category = "Weapon", Description = "Thần đao gia truyền của thợ mổ lành nghề, tăng 25% sản lượng thịt khi sơ chế." },
                Quantity = 1,
                RequestedCategory = "Pig",
                RequestedItemDescription = "Thuần Barter: Yêu cầu 1 chú Heo sống khỏe mạnh (≥ 2400G GTTC)",
                MinimumReferenceValue = 2400,
                AcceptsGold = false,
                GoldPrice = 0
            });
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
            ongTon.Listings.Add(new NightMarketListing
            {
                SlotId = "ongton_so",
                OfferedItem = new GameItem { Id = "BanVeThoatNuoc2", Name = "Bản vẽ Thoát Nước Cấp 2", Tier = ItemTier.II, ReferenceValueGold = 1800, Category = "Blueprint", Description = "Bản đồ thoát nước tiêu úng cổ truyền, giảm 50% ô nhiễm chuồng nuôi." },
                Quantity = 1,
                RequestedCategory = "Material",
                RequestedItemDescription = "Chấp nhận 2520G Vàng hoặc Vật liệu quý (≥ 1800G GTTC)",
                MinimumReferenceValue = 1800,
                AcceptsGold = true,
                GoldPrice = 2520
            });
            ongTon.Listings.Add(new NightMarketListing
            {
                SlotId = "ongton_bua",
                OfferedItem = new GameItem { Id = "PhuTranTrach", Name = "Bùa Bạch Vân Trấn Trạch", Tier = ItemTier.III, ReferenceValueGold = 3500, Category = "Special", Description = "Bùa niêm phong xua đuổi âm khí và giảm 80% tỷ lệ đột kích đêm." },
                Quantity = 1,
                RequestedCategory = "Consumable",
                RequestedItemDescription = "Thuần Barter: Yêu cầu Dược liệu quý hoặc Đan dược (≥ 3500G GTTC)",
                MinimumReferenceValue = 3500,
                AcceptsGold = false,
                GoldPrice = 0
            });
            Villagers[VillagerId.OngTon] = ongTon;

            // 6. BÍ NHÂN - Cực Đông Rừng Cấm (Thần bí, chỉ tiếp đêm)
            var biNhan = new VillagerProfile
            {
                Id = VillagerId.BiNhan,
                Name = "Bí Nhân",
                Title = "Thương Nhân Dị Thú Ranh Giới",
                LocationDescription = "Đứng khuất bóng dưới gốc đa ngàn năm rủ rễ đỏ quạch. Đôi mắt lập lòe trong sương đặc.",
                AtmosphereQuote = "Đem thứ không thuộc về cõi này tới đây... Ta không dùng tiền phàm, chỉ nhận máu lạ và dị biến...",
                IsOpenDay = false,
                IsOpenNight = true
            };
            biNhan.Listings.Add(new NightMarketListing
            {
                SlotId = "binhan_phale",
                OfferedItem = new GameItem { Id = "PhaLeUFO", Name = "Pha Lê Năng Lượng UFO", Tier = ItemTier.III, ReferenceValueGold = 950, Category = "Material", Description = "Mảnh động cơ phát sáng kỳ dị, cung cấp nguồn năng lượng vĩnh cửu." },
                Quantity = 1,
                RequestedCategory = "Material",
                RequestedItemDescription = "Thuần Barter: Nhận Kim Loại Lạ hoặc Heo Dị Biến (≥ 950G GTTC)",
                MinimumReferenceValue = 950,
                AcceptsGold = false,
                GoldPrice = 0
            });
            biNhan.Listings.Add(new NightMarketListing
            {
                SlotId = "binhan_tinhhoa",
                OfferedItem = new GameItem { Id = "TinhHoaHuThe", Name = "Tinh Hoa Hư Thể Cổ Đại", Tier = ItemTier.IV, ReferenceValueGold = 8000, Category = "Special", Description = "Phôi gen dị biến hư vô tối thượng, ban cho đàn heo thể chất thần thoại." },
                Quantity = 1,
                RequestedCategory = "Special",
                RequestedItemDescription = "Thuần Barter: Chỉ nhận Phôi Kỳ Dị hoặc Heo Dị Biến (≥ 8000G GTTC)",
                MinimumReferenceValue = 8000,
                AcceptsGold = false,
                GoldPrice = 0
            });
            // GDD 21: with an engine, his counter is tonight's generated stock (2–4 slots), only while he is here
            if (MysticStockSource != null) biNhan.Listings = MysticStockSource() ?? new List<NightMarketListing>();
            Villagers[VillagerId.BiNhan] = biNhan;

            // 7. CHỢ ĐÊM (GDD 20): the day's live night-market slots from EconomyManager, open 18:00-06:00
            Villagers[VillagerId.ChoDem] = new VillagerProfile
            {
                Id = VillagerId.ChoDem,
                Name = "Chợ Đêm",
                Title = "Chợ Đêm Thị Trấn",
                LocationDescription = "Dãy sạp đèn lồng tím ở quảng trường phía đông thị trấn.",
                AtmosphereQuote = "",
                IsOpenDay = false,
                IsOpenNight = true,
                Listings = NightMarketSource?.Invoke() ?? new List<NightMarketListing>()
            };
            foreach (var profile in Villagers.Values)
                foreach (var listing in profile.Listings)
                {
                    var item = listing.OfferedItem;
                    if (QualifiedItemRegistry.GetData(item.Id) == null)
                        QualifiedItemRegistry.Register("O", item.Id, item.Name, item.Description, item.Category,
                            item.ReferenceValueGold, 0, "village");
                }
        }

        /// <summary>Live night-market slots (EconomyManager.NightListings); set by GameEngine.</summary>
        public Func<List<NightMarketListing>>? NightMarketSource { get; set; }

        /// <summary>Bí nhân's stock for tonight (EconomyManager.MysticMerchant.Stock); set by GameEngine.</summary>
        public Func<List<NightMarketListing>>? MysticStockSource { get; set; }

        public VillagerProfile? GetVillager(VillagerId id)
        {
            if (Villagers.TryGetValue(id, out var profile)) return profile;
            return null;
        }

        public int StockDay { get; private set; } = -1;
        public List<string> SoldSlots { get; private set; } = new List<string>();

        public void RefreshStockForDay(int day)
        {
            if (StockDay != day) RestoreStock(day, null);
        }

        public void RestoreStock(int day, List<string>? soldSlots)
        {
            StockDay = day;
            SoldSlots = soldSlots == null ? new List<string>() : new List<string>(soldSlots);
            InitializeVillagers();
            foreach (var profile in Villagers.Values)
                foreach (var listing in profile.Listings)
                    if (SoldSlots.Contains(listing.SlotId)) listing.Quantity = 0;
        }

        public (bool success, GameItem? item, int excessValueGold, string message) TradeWithVillager(
            GameEngine engine, VillagerId villagerId, string slotId, List<GameItem>? offeredItems,
            Pig? offeredPig, bool payWithGold)
        {
            lock (engine)
            {
                RefreshStockForDay(engine.Clock.CurrentDay);
                if (!Villagers.TryGetValue(villagerId, out var profile)) return (false, null, 0, "Không tìm thấy dân làng.");
                bool night = engine.Clock.CurrentHour >= 18 || engine.Clock.CurrentHour < 6;
                if (night ? !profile.IsOpenNight : !profile.IsOpenDay) return (false, null, 0, "Cửa hàng đang đóng.");
                var listing = profile.Listings.Find(l => l.SlotId == slotId);
                if (listing == null || listing.IsSoldOut) return (false, null, 0, "Món hàng không có hoặc đã bán hết.");
                if (listing.Wants.Count > 0)
                    return TradeForWants(engine, villagerId, listing, offeredItems,
                        offeredPig == null ? new List<Pig>() : new List<Pig> { offeredPig }, payWithGold);
                var offered = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                long total = 0;
                if (payWithGold)
                {
                    if (villagerId == VillagerId.BiNhan || !listing.AcceptsGold) return (false, null, 0, "Giao dịch này không nhận vàng.");
                    if (listing.GoldPrice <= 0 || engine.Farm.Gold < listing.GoldPrice) return (false, null, 0, "Không đủ vàng.");
                }
                else
                {
                    if (offeredItems != null)
                        foreach (var item in offeredItems)
                        {
                            if (item == null || item.Quantity <= 0) return (false, null, 0, "Số lượng trao đổi phải dương.");
                            var definition = QualifiedItemRegistry.GetData(item.Id);
                            if (definition == null) return (false, null, 0, "Vật phẩm không có trong danh mục.");
                            if (listing.RequestedCategory != "Any" && listing.RequestedCategory != "Pig" &&
                                !string.Equals(definition.Category, listing.RequestedCategory, StringComparison.OrdinalIgnoreCase))
                                return (false, null, 0, "Loại vật phẩm không đúng yêu cầu.");
                            string id = definition.QualifiedItemId;
                            long count = (offered.TryGetValue(id, out var prior) ? prior : 0L) + item.Quantity;
                            if (count > int.MaxValue || !engine.Inventory.ContainsId(id, (int)count)) return (false, null, 0, "Không sở hữu đủ vật phẩm trao đổi.");
                            offered[id] = (int)count;
                            total += (long)definition.BasePriceGold * item.Quantity;
                        }
                    if (offeredPig != null)
                    {
                        if (!engine.Pigs.Contains(offeredPig) || !offeredPig.IsAlive) return (false, null, 0, "Heo không thuộc đàn hoặc đã chết.");
                        if (offeredPig.Rarity == GeneRarity.DiBien && villagerId != VillagerId.BiNhan) return (false, null, 0, "Chỉ Bí Nhân nhận heo Dị Biến.");
                        var price = engine.Economy.CalculatePigSalePrice(offeredPig, engine.Farm.FarmLevel).priceGold;
                        total += (int)Math.Round(price * 0.90f); // Existing C branch barter valuation, unchanged.
                    }
                    if (listing.RequestedCategory == "Pig" && offeredPig == null) return (false, null, 0, "Giao dịch yêu cầu heo sống.");
                    if (total < listing.MinimumReferenceValue || total > int.MaxValue) return (false, null, 0, "Tổng giá trị trao đổi không hợp lệ hoặc chưa đủ.");
                }
                var reward = listing.OfferedItem;
                if (QualifiedItemRegistry.GetData(reward.Id) == null)
                    QualifiedItemRegistry.Register("O", reward.Id, reward.Name, reward.Description, reward.Category,
                        reward.ReferenceValueGold, 0, "village");
                int rewardCount = listing.Quantity;
                if (engine.Inventory.CountId(reward.Id) > int.MaxValue - rewardCount) return (false, null, 0, "Kho không nhận thêm được số lượng này.");
                if (payWithGold)
                {
                    if (!engine.Ledger.TrySpend(listing.GoldPrice, LedgerReason.AuctionBid, slotId)) return (false, null, 0, "Không đủ vàng.");
                }
                else
                {
                    foreach (var pair in offered) engine.Inventory.ConsumeItem(pair.Key, pair.Value);
                    if (offeredPig != null) engine.Pigs.Remove(offeredPig);
                }
                engine.Inventory.AddItem(reward.Id, rewardCount);
                listing.Quantity = 0;
                SoldSlots.Add(slotId);
                return (true, reward, payWithGold ? 0 : (int)total - listing.MinimumReferenceValue,
                    "Đã giao dịch: " + reward.Name);
            }
        }

        /// <summary>Same as TradeWithVillager but with several pigs (GDD 20.1 "2 heo trưởng thành…", GDD 21 "3–8 heo già").</summary>
        public (bool success, GameItem? item, int excessValueGold, string message) TradeWithVillagerPigs(
            GameEngine engine, VillagerId villagerId, string slotId, List<GameItem>? offeredItems,
            IReadOnlyList<Pig>? offeredPigs, bool payWithGold)
        {
            lock (engine)
            {
                if (offeredPigs == null || offeredPigs.Count <= 1)
                    return TradeWithVillager(engine, villagerId, slotId, offeredItems,
                        offeredPigs != null && offeredPigs.Count == 1 ? offeredPigs[0] : null, payWithGold);
                RefreshStockForDay(engine.Clock.CurrentDay);
                if (!Villagers.TryGetValue(villagerId, out var profile)) return (false, null, 0, "Không tìm thấy dân làng.");
                bool night = engine.Clock.CurrentHour >= 18 || engine.Clock.CurrentHour < 6;
                if (night ? !profile.IsOpenNight : !profile.IsOpenDay) return (false, null, 0, "Cửa hàng đang đóng.");
                var listing = profile.Listings.Find(l => l.SlotId == slotId);
                if (listing == null || listing.IsSoldOut) return (false, null, 0, "Món hàng không có hoặc đã bán hết.");
                if (listing.Wants.Count == 0) return (false, null, 0, "Quầy này chỉ nhận một con heo mỗi lần.");
                return TradeForWants(engine, villagerId, listing, offeredItems, new List<Pig>(offeredPigs), payWithGold);
            }
        }

        /// <summary>
        /// GDD 20.1 / 21 barter by "món muốn nhận": exact wants or same-group ≥110% substitutes (NightMarketBarter),
        /// 5% GTTC fee in gold for the night market, gold only on gold stalls; Bí nhân: only while he is here, never
        /// gold, 130–200% GTTC, each trade raises ÁLSK by 2–10.
        /// </summary>
        private (bool success, GameItem? item, int excessValueGold, string message) TradeForWants(
            GameEngine engine, VillagerId villagerId, NightMarketListing listing, List<GameItem>? offeredItems,
            List<Pig> offeredPigs, bool payWithGold)
        {
            bool mystic = listing.IsMysticListing || villagerId == VillagerId.BiNhan;
            if (mystic && !engine.Economy.MysticMerchant.IsActive)
                return (false, null, 0, "Bí Nhân không có ở đây. Chỉ khi đèn lồng xanh sáng ngoài rào mới gặp được hắn.");
            var reward = listing.OfferedItem;
            int rewardCount = Math.Max(1, listing.Quantity);
            if (payWithGold)
            {
                if (mystic || !listing.AcceptsGold) return (false, null, 0, "Giao dịch này không nhận vàng.");
                if (listing.GoldPrice <= 0 || !engine.Ledger.TrySpend(listing.GoldPrice, LedgerReason.NightMarketGoldSlot, listing.SlotId))
                    return (false, null, 0, "Không đủ vàng.");
                NightMarketBarter.GrantReward(engine, reward, rewardCount);
                listing.Quantity = 0;
                SoldSlots.Add(listing.SlotId);
                return (true, reward, 0, "Đã mua bằng vàng: " + reward.Name);
            }

            var items = new List<(string itemId, int quantity)>();
            var owned = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (offeredItems != null)
                foreach (var item in offeredItems)
                {
                    if (item == null || item.Quantity <= 0) return (false, null, 0, "Số lượng trao đổi phải dương.");
                    var definition = QualifiedItemRegistry.GetData(item.Id);
                    if (definition == null) return (false, null, 0, "Vật phẩm không có trong danh mục.");
                    string id = definition.QualifiedItemId;
                    long count = (owned.TryGetValue(id, out var prior) ? prior : 0L) + item.Quantity;
                    if (count > int.MaxValue || !engine.Inventory.ContainsId(id, (int)count)) return (false, null, 0, "Không sở hữu đủ vật phẩm trao đổi.");
                    owned[id] = (int)count;
                    items.Add((id, item.Quantity));
                }
            var seen = new HashSet<Pig>();
            foreach (var pig in offeredPigs)
            {
                if (pig == null || !seen.Add(pig)) return (false, null, 0, "Heo trao đổi bị trùng.");
                if (!engine.Pigs.Contains(pig)) return (false, null, 0, "Heo không thuộc đàn.");
                if (!pig.IsAlive && !mystic) return (false, null, 0, "Heo đã chết; chỉ Bí Nhân nhận xác heo.");
                if (pig.Rarity == GeneRarity.DiBien && !mystic) return (false, null, 0, "Chỉ Bí Nhân nhận heo Dị Biến.");
            }

            var eval = NightMarketBarter.Evaluate(listing, items, offeredPigs,
                p => NightMarketBarter.PigGttc(engine.Economy, p, engine.Farm.FarmLevel));
            if (!eval.Ok) return (false, null, 0, eval.Message);
            if (engine.Inventory.CountId(reward.Id) > int.MaxValue - rewardCount) return (false, null, 0, "Kho không nhận thêm được số lượng này.");
            if (eval.FeeGold > 0 && !engine.Ledger.TrySpend(eval.FeeGold, LedgerReason.NightMarketBarterFee, listing.SlotId))
                return (false, null, 0, $"Không đủ {eval.FeeGold}G phí trao đổi (5% GTTC).");

            foreach (var pair in owned) engine.Inventory.ConsumeItem(pair.Key, pair.Value);
            foreach (var pig in offeredPigs) engine.Pigs.Remove(pig);
            NightMarketBarter.GrantReward(engine, reward, rewardCount);
            listing.Quantity = 0;
            SoldSlots.Add(listing.SlotId);
            string tail = eval.FeeGold > 0 ? $" (phí {eval.FeeGold}G)" : "";
            if (mystic)
            {
                int pressure = engine.Economy.NextInt(NightMarketBarter.MysticPressureMin, NightMarketBarter.MysticPressureMax + 1);
                engine.Farm.EventPressureIndex = Math.Min(100, engine.Farm.EventPressureIndex + pressure);
                tail = $" — ÁLSK +{pressure}";
            }
            return (true, reward, Math.Max(0, eval.OfferedGttc - eval.RequiredGttc), "Đã trao đổi: " + reward.Name + tail);
        }
    }
}
