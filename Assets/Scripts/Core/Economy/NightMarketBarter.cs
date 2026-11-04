using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>What kind of pig a "món muốn nhận" asks for (GDD 20.1 examples, GDD 21 Bí nhân demands).</summary>
    public enum WantedPigKind
    {
        None,
        AdultGradeA,        // "heo trưởng thành hạng A"
        PigletCommon,       // "heo con Thường"
        IdentifiedKhaPlus,  // "heo gen Khá đã định danh"
        AnyAdult,           // "heo trưởng thành"
        Unidentified,       // Bí nhân: heo gen "?"
        QuyPlus,            // Bí nhân: heo Quý+
        Elder,              // Bí nhân: heo già (3–8 con)
        Corpse              // Bí nhân: xác heo
    }

    /// <summary>GDD 20.1 "món muốn nhận": one exact wanted item (or pig kind) with a quantity.</summary>
    [Serializable]
    public class NightMarketWant
    {
        public string ItemId = "";            // inventory item id (unqualified); empty for pigs and group-only wants
        public string NameVi = "";
        public int Quantity = 1;
        public string Group = "";             // NightMarketBarter.Group* — substitution happens only inside one group
        public int UnitGttc;                  // GTTC per unit at generation time (pigs: estimate, day price × 0,9)
        public WantedPigKind PigKind = WantedPigKind.None;
        public bool GroupOnly;                // Bí nhân "vật phẩm bậc III–IV": any item of the group, worth TotalGttc
        public bool IsPig => PigKind != WantedPigKind.None;
        public int TotalGttc => UnitGttc * Math.Max(1, Quantity);

        public string Describe() => GroupOnly
            ? $"{NameVi} (tổng ≥ {TotalGttc:N0}G GTTC)"
            : $"{Quantity}× {NameVi}";
    }

    public sealed class BarterEvaluation
    {
        public bool Ok;
        public string Message = "";
        public int OfferedGttc;
        public int RequiredGttc;
        public int FeeGold;
        public readonly List<string> Lines = new List<string>();
    }

    /// <summary>
    /// GDD 20.1 night-market barter law: each stall asks for 1–2 exact wanted items; a want may be replaced only by
    /// items of the same group (heo ↔ heo, vật liệu ↔ vật liệu, vật phẩm cùng bậc) worth ≥ 110% of it; 18% of
    /// stalls also take gold at GTTC × 1,4; every barter pays a 5% GTTC fee in gold; selling an item for gold gets
    /// 40% of its GTTC; pigs are valued at the day price × 0,9. Bí nhân (GDD 21) uses the same wants, never gold, no
    /// substitution, and asks 130–200% of the GTTC he gives.
    /// </summary>
    public static class NightMarketBarter
    {
        public const string GroupPig = "Heo";
        public const string GroupMaterial = "VatLieu";
        public const string GroupItemPrefix = "VatPham:";
        public const string GroupItemTierIIIPlus = "VatPham:III+";

        public const float SubstituteMinRatio = 1.10f;
        public const double GoldSlotShare = 0.18;
        public const float GoldSlotPriceRatio = 1.4f;
        public const float BarterFeeRatio = 0.05f;
        public const float SellForGoldRatio = 0.40f;
        public const float PigGttcRatio = 0.9f;
        public const float MysticRateMin = 1.30f, MysticRateMax = 2.00f;
        public const int MysticPressureMin = 2, MysticPressureMax = 10;

        public const string FullIntelItemId = "TinhBaoToanPhan";
        public const string BlueprintPrefix = "BanVe:";

        public static bool IsNight(int hour) => hour >= 18 || hour < 6;

        public static int FeeFor(NightMarketListing listing) =>
            listing.IsMysticListing ? 0 : (int)Math.Round(ListingGttc(listing) * BarterFeeRatio);

        public static int ListingGttc(NightMarketListing listing) =>
            Math.Max(0, listing.OfferedItem.ReferenceValueGold) * Math.Max(1, listing.Quantity);

        public static string GroupOf(string category, ItemTier tier)
        {
            switch ((category ?? "").ToLowerInvariant())
            {
                case "pig": return GroupPig;
                case "feed":
                case "resource":
                case "material":
                case "ammo": return GroupMaterial;
                default: return GroupItemPrefix + tier;
            }
        }

        public static string GroupOfItem(string itemId)
        {
            NightMarketCatalog.EnsureRegistered();
            var data = QualifiedItemRegistry.GetData(itemId);
            return GroupOf(data?.Category ?? "Object", TierOf(itemId));
        }

        public static bool GroupMatches(string wantGroup, string offeredGroup)
        {
            if (string.Equals(wantGroup, offeredGroup, StringComparison.Ordinal)) return true;
            return wantGroup == GroupItemTierIIIPlus &&
                   (offeredGroup == GroupItemPrefix + ItemTier.III || offeredGroup == GroupItemPrefix + ItemTier.IV);
        }

        public static string GroupLabelVi(string group)
        {
            if (group == GroupPig) return "heo";
            if (group == GroupMaterial) return "vật liệu";
            if (group == GroupItemTierIIIPlus) return "vật phẩm bậc III–IV";
            if (group != null && group.StartsWith(GroupItemPrefix, StringComparison.Ordinal))
                return "vật phẩm bậc " + group.Substring(GroupItemPrefix.Length);
            return group ?? "";
        }

        /// <summary>Tier of an item: the GDD tactical catalog, then this night-market catalog, then the GTTC band
        /// rule of ADR-0023 (I &lt; 300G ≤ II &lt; 700G ≤ III &lt; 1.500G ≤ IV) for items the code gives no tier.</summary>
        public static ItemTier TierOf(string itemId)
        {
            var data = QualifiedItemRegistry.GetData(itemId);
            string bare = data?.ItemId ?? StripPrefix(itemId);
            foreach (var item in EquipmentAndTacticalCatalog.AllTwentyTacticalItems)
                if (string.Equals(item.Id, bare, StringComparison.OrdinalIgnoreCase)) return item.Tier;
            var entry = NightMarketCatalog.Find(bare);
            if (entry != null) return entry.Tier;
            return TierByGttc(data?.BasePriceGold ?? 0);
        }

        public static ItemTier TierByGttc(int gttc) =>
            gttc < 300 ? ItemTier.I : gttc < 700 ? ItemTier.II : gttc < 1500 ? ItemTier.III : ItemTier.IV;

        public static int UnitGttcOfItem(string itemId)
        {
            NightMarketCatalog.EnsureRegistered();
            return Math.Max(0, QualifiedItemRegistry.GetData(itemId)?.BasePriceGold ?? 0);
        }

        private static string StripPrefix(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            int close = id.StartsWith("(", StringComparison.Ordinal) ? id.IndexOf(')') : -1;
            return close > 0 ? id.Substring(close + 1) : id;
        }

        public static bool PigMatches(WantedPigKind kind, Pig pig)
        {
            if (pig == null) return false;
            if (kind == WantedPigKind.Corpse) return !pig.IsAlive;
            if (!pig.IsAlive) return false;
            switch (kind)
            {
                case WantedPigKind.AdultGradeA: return pig.Stage == PigStage.TruongThanh && pig.MeatQuality >= MeatQuality.A;
                case WantedPigKind.PigletCommon: return pig.Stage == PigStage.HeoNon && pig.Rarity == GeneRarity.Thuong;
                case WantedPigKind.IdentifiedKhaPlus: return pig.IsIdentified && pig.Rarity >= GeneRarity.Kha && pig.Rarity != GeneRarity.DiBien;
                case WantedPigKind.AnyAdult: return pig.Stage == PigStage.TruongThanh;
                case WantedPigKind.Unidentified: return !pig.IsIdentified;
                case WantedPigKind.QuyPlus: return pig.IsIdentified && pig.Rarity >= GeneRarity.Quy;
                case WantedPigKind.Elder: return pig.Stage == PigStage.HeoGia;
                default: return false;
            }
        }

        /// <summary>
        /// Checks an offer against a listing's wants. Exact wants are matched first; every unmet want must be covered
        /// by leftover items of the same group worth ≥ 110% of it (Bí nhân: no substitution). Items of a group no want
        /// asks for are refused. Bí nhân listings also need the whole offer ≥ MinimumReferenceValue (130–200%).
        /// </summary>
        public static BarterEvaluation Evaluate(NightMarketListing listing, IReadOnlyList<(string itemId, int quantity)> items,
            IReadOnlyList<Pig> pigs, Func<Pig, int> pigGttc)
        {
            NightMarketCatalog.EnsureRegistered();
            var result = new BarterEvaluation { FeeGold = FeeFor(listing) };
            var itemPool = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var (id, qty) in items ?? Array.Empty<(string, int)>())
            {
                if (qty <= 0) continue;
                string qid = QualifiedItemRegistry.QualifyItemId(id);
                itemPool[qid] = (itemPool.TryGetValue(qid, out var prior) ? prior : 0) + qty;
                result.OfferedGttc += UnitGttcOfItem(qid) * qty;
            }
            var pigPool = new List<Pig>(pigs ?? Array.Empty<Pig>());
            foreach (var p in pigPool) result.OfferedGttc += Math.Max(0, pigGttc(p));

            var unmet = new List<NightMarketWant>();
            foreach (var want in listing.Wants)
            {
                result.RequiredGttc += want.TotalGttc;
                if (want.GroupOnly) { unmet.Add(want); continue; }
                if (want.IsPig)
                {
                    var match = pigPool.Where(p => PigMatches(want.PigKind, p)).Take(want.Quantity).ToList();
                    if (match.Count >= want.Quantity)
                    {
                        foreach (var p in match) pigPool.Remove(p);
                        result.Lines.Add($"√ Đúng món: {want.Describe()}");
                        continue;
                    }
                }
                else
                {
                    string qid = QualifiedItemRegistry.QualifyItemId(want.ItemId);
                    if (itemPool.TryGetValue(qid, out int have) && have >= want.Quantity)
                    {
                        itemPool[qid] = have - want.Quantity;
                        result.Lines.Add($"√ Đúng món: {want.Describe()}");
                        continue;
                    }
                }
                unmet.Add(want);
            }
            if (listing.IsMysticListing) result.RequiredGttc = Math.Max(result.RequiredGttc, listing.MinimumReferenceValue);

            // leftovers by group
            var leftover = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in itemPool)
            {
                if (pair.Value <= 0) continue;
                string g = GroupOfItem(pair.Key);
                leftover[g] = (leftover.TryGetValue(g, out var v) ? v : 0) + UnitGttcOfItem(pair.Key) * pair.Value;
            }
            foreach (var p in pigPool)
                leftover[GroupPig] = (leftover.TryGetValue(GroupPig, out var v) ? v : 0) + Math.Max(0, pigGttc(p));

            var wantGroups = listing.Wants.Select(w => w.Group).Distinct().ToList();
            foreach (var g in leftover.Keys)
                if (!wantGroups.Any(wg => GroupMatches(wg, g)))
                    return Fail(result, $"Người bán không nhận {GroupLabelVi(g)} cho ô này (chỉ đổi cùng nhóm với món muốn nhận).");

            if (result.OfferedGttc <= 0) return Fail(result, "Chưa đặt món nào để trao đổi.");

            var needByGroup = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var want in unmet)
            {
                if (listing.IsMysticListing && !want.GroupOnly)
                    return Fail(result, $"Bí Nhân chỉ nhận đúng món: thiếu {want.Describe()}.");
                double ratio = want.GroupOnly ? 1.0 : SubstituteMinRatio;
                int need = (int)Math.Ceiling(want.TotalGttc * ratio);
                needByGroup[want.Group] = (needByGroup.TryGetValue(want.Group, out var n) ? n : 0) + need;
            }
            foreach (var pair in needByGroup)
            {
                int available = leftover.Where(l => GroupMatches(pair.Key, l.Key)).Sum(l => l.Value);
                string kind = listing.Wants.Any(w => w.Group == pair.Key && w.GroupOnly) ? "đủ giá trị" : "thay thế ≥110%";
                if (available < pair.Value)
                    return Fail(result, $"Thiếu món muốn nhận; đồ cùng nhóm {GroupLabelVi(pair.Key)} mới đạt {available:N0}/{pair.Value:N0}G GTTC ({kind}).");
                result.Lines.Add($"√ Thay bằng {GroupLabelVi(pair.Key)}: {available:N0}/{pair.Value:N0}G GTTC ({kind})");
            }
            if (listing.IsMysticListing && result.OfferedGttc < listing.MinimumReferenceValue)
                return Fail(result, $"Bí Nhân đòi tổng {listing.MinimumReferenceValue:N0}G GTTC, mới đặt {result.OfferedGttc:N0}G.");

            result.Ok = true;
            result.Message = result.FeeGold > 0 ? $"Đủ điều kiện. Phí trao đổi 5%: {result.FeeGold:N0}G." : "Đủ điều kiện trao đổi.";
            return result;
        }

        private static BarterEvaluation Fail(BarterEvaluation r, string message)
        {
            r.Ok = false;
            r.Message = message;
            return r;
        }

        /// <summary>GTTC of a pig on the night market: day price × 0,9 (Mùa Heo Rẻ ×1,0). Corpses use the same rule.</summary>
        public static int PigGttc(EconomyManager economy, Pig pig, int farmLevel) =>
            (int)Math.Round(economy.CalculatePigSalePrice(pig, farmLevel).priceGold * economy.DayMarket.BarterPigValueRatio);

        /// <summary>Gives the reward of a stall: full intel and blueprints are not warehouse items.</summary>
        public static void GrantReward(GameEngine engine, GameItem reward, int count)
        {
            if (string.Equals(reward.Id, FullIntelItemId, StringComparison.OrdinalIgnoreCase))
            {
                engine.Intel.GrantFull(engine.Clock.CycleNumber);
                return;
            }
            if (reward.Id == RareSources.SecretManualId) { engine.Character.SecretManuals += count; return; } // a learning token
            // GDD 12.3 gear (quality in the id) is worn, not stored: it joins the owned equipment
            if (EquipmentAndTacticalCatalog.Find(reward.Id) is EquipmentItem gear)
            {
                if (!engine.Character.OwnedEquipment.Contains(gear.Id)) engine.Character.OwnedEquipment.Add(gear.Id);
                return;
            }
            if (reward.Id.StartsWith(BlueprintPrefix, StringComparison.Ordinal))
            {
                string bp = reward.Id.Substring(BlueprintPrefix.Length);
                if (!engine.Defense.UnlockedBlueprints.Contains(bp)) engine.Defense.UnlockedBlueprints.Add(bp);
                return;
            }
            if (QualifiedItemRegistry.GetData(reward.Id) == null)
                QualifiedItemRegistry.Register("O", reward.Id, reward.Name, reward.Description, reward.Category,
                    reward.ReferenceValueGold, 0, "village");
            engine.Inventory.AddItem(reward.Id, count);
        }

        /// <summary>GDD 20: at the night market an item sold for gold fetches only 40% of its GTTC.</summary>
        public static (bool success, int goldEarned, string message) SellItemsForGold(GameEngine engine,
            IReadOnlyList<(string itemId, int quantity)> items)
        {
            if (!IsNight(engine.Clock.CurrentHour)) return (false, 0, "Chợ đêm chỉ mở 18:00–06:00.");
            if (items == null || items.Count == 0) return (false, 0, "Chưa chọn món để bán.");
            NightMarketCatalog.EnsureRegistered();
            var sum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            long gttc = 0;
            foreach (var (id, qty) in items)
            {
                if (qty <= 0) return (false, 0, "Số lượng bán phải dương.");
                var data = QualifiedItemRegistry.GetData(id);
                if (data == null || data.BasePriceGold <= 0) return (false, 0, "Món này không có giá trị tham chiếu.");
                string qid = data.QualifiedItemId;
                int total = (sum.TryGetValue(qid, out var prior) ? prior : 0) + qty;
                if (!engine.Inventory.ContainsId(qid, total)) return (false, 0, "Không sở hữu đủ vật phẩm.");
                sum[qid] = total;
                gttc += (long)data.BasePriceGold * qty;
            }
            int gold = (int)Math.Round(gttc * SellForGoldRatio);
            if (gold <= 0) return (false, 0, "Giá trị quá thấp.");
            foreach (var pair in sum) engine.Inventory.ConsumeItem(pair.Key, pair.Value);
            engine.Ledger.Earn(gold, LedgerReason.NightMarketItemSale, "night_market_sale");
            return (true, gold, $"Đã bán lấy {gold:N0}G (40% GTTC {gttc:N0}G).");
        }
    }

    /// <summary>
    /// The goods the night market and Bí nhân deal in. GTTC comes from the code catalogs (EquipmentAndTacticalCatalog,
    /// QualifiedItemRegistry, DefenseBlueprint.BuildCostGold as in ADR-0016) or the GDD (Trà Lá Vông ≈ 650G, Tên Thép
    /// 6G/mũi); the few Bí nhân items with no number anywhere are provisional (ADR-0023).
    /// </summary>
    public static class NightMarketCatalog
    {
        public enum Kind { Consumable, Medicine, GenePaper, Ammo, Material, Equipment, Blueprint, Feed, Mystic }

        public sealed class Entry
        {
            public string Id = "";
            public string Name = "";
            public ItemTier Tier;
            public int UnitGttc;
            public string Category = "";
            public string Description = "";
            public Kind Kind;
            public int LotQuantity = 1;
            public bool IsBachVan;
        }

        // items with no GTTC/tier in the code: GDD-sourced (Trà Lá Vông, Tên Thép) or ADR-0023 provisional (marked P)
        private static readonly List<Entry> Extra = new List<Entry>
        {
            new Entry { Id = RareSources.SecretManualId, Name = "Bí Kíp", Tier = ItemTier.III, UnitGttc = 2400, Category = "Manual", Kind = Kind.Equipment,
                Description = "Mở 1 Tuyệt kỹ hoặc 1 kỹ năng ngoài phái (GDD 12.6)." },                               // GTTC TẠM (C-08)
            new Entry { Id = RareSources.InkId, Name = "Mực Ấn", Tier = ItemTier.I, UnitGttc = 60, Category = "Material", Kind = Kind.Material, LotQuantity = 3,
                Description = "Vật liệu gia cường Giấy Lam (GDD 10.5)." },                                         // GTTC TẠM (C-08)
            new Entry { Id = "TraLaVong", Name = "Trà Lá Vông", Tier = ItemTier.I, UnitGttc = 650, Category = "Consumable", Kind = Kind.Consumable,
                Description = "Ngủ ngắn 3 giờ bất chấp cooldown." },
            new Entry { Id = "TenThep", Name = "Tên Thép", Tier = ItemTier.I, UnitGttc = 6, Category = "Ammo", Kind = Kind.Ammo, LotQuantity = 50,
                Description = "Đạn cho Nỏ Xuyên Vân (6G/mũi)." },
            new Entry { Id = NightMarketBarter.FullIntelItemId, Name = "Tình Báo Toàn Phần (cấp 5)", Tier = ItemTier.IV, UnitGttc = 1500, Category = "Info", Kind = Kind.Mystic,
                Description = "Tình báo cấp 5 cho chu kỳ này: điều kiện, cửa sổ và giờ đợt lớn." },               // P
            new Entry { Id = "QuaMong", Name = "Quả Mộng", Tier = ItemTier.IV, UnitGttc = 3200, Category = "Consumable", Kind = Kind.Mystic,
                Description = "Ngủ ngay: Stamina 100%, Thể trạng +8, xóa 1 tầng Mệt Dội." },                       // P
            new Entry { Id = "HuongMeThao", Name = "Hương Mê Thảo", Tier = ItemTier.III, UnitGttc = 2000, Category = "Consumable", Kind = Kind.Mystic,
                Description = "Ngủ sâu: Stamina 100%, Thể trạng +6, xóa Tải ngày." },                               // P
            new Entry { Id = "ThuocHuMach", Name = "Thuốc chữa Hư Mạch", Tier = ItemTier.III, UnitGttc = 900, Category = "Medicine", Kind = Kind.Mystic,
                Description = "Chữa heo nhiễm Hư Mạch." },                                                          // P
        };

        private static readonly string[] TacticalOfferIds =
        {
            "BinhHoiLuc", "BangGac", "PhaoSang", "LuuKhoi", "BomKeo", "TinhNoDan", "TinDonCho", "LaDoSuong",
            "CamLinhChi", "CamHaHoa", "ThuocAnThanHeo", "MaiCheTam", "BuaHoiHuong",
        };
        private static readonly string[] MedicineOfferIds = { "ThuocHoHap", "ThuocTieuHoa", "ThuocKySinh", "ThuocHaNhiet", "VacXinTongHop" };
        private static readonly string[] PaperOfferIds = { "Paper_Blue", "Paper_Yellow", "Paper_Purple" };
        private static readonly string[] MaterialOfferIds = { "XangSinhHoc", "TinhThach" };
        /// <summary>
        /// GDD 20 night market: gear "gần như chỉ bậc I–II; bậc III–IV ≤ 2% số ô". Every GDD 12.3 piece is offered at II;
        /// III shows up through the stall's own rare draw. PROVISIONAL (ADR-0017 addendum): GTTC = quality-I price × the
        /// quality's stat bonus (II ×3, III ×5) — the GDD gives no prices above quality I.
        /// </summary>
        private static IEnumerable<Entry> GearOffers()
        {
            foreach (var g in EquipmentAndTacticalCatalog.AllEquipmentItems)
                foreach (var q in new[] { ItemTier.II, ItemTier.III })
                {
                    var piece = g.WithQuality(q);
                    yield return new Entry { Id = piece.Id, Name = piece.Name, Tier = q, UnitGttc = MartialProgression.PriceGold(g) * EquipmentQuality.BonusFor(q),
                        Category = "Equipment", Description = piece.MechanicText, Kind = Kind.Equipment, LotQuantity = 1 };
                }
        }
        private static readonly string[] MysticOfferIds =
        {
            NightMarketBarter.FullIntelItemId, "HoSoGiongLoai", "QuaMong", "HuongMeThao", "NgocPhaSuong",
            "Paper_Purple", "ThuocHuMach", "BanDoHuongQuai", // no Đỏ: "Đỏ chỉ ở đấu giá" (GDD 10.3)
        };

        /// <summary>A paper as a night-stall entry (they are rolled apart from the weighted pool, GDD 10.3).</summary>
        public static Entry? PaperEntry(string id)
        {
            EnsureRegistered();
            return Resolve(id, Kind.GenePaper);
        }

        public static Entry? Find(string id)
        {
            foreach (var e in Extra)
                if (string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }

        /// <summary>Registers the catalog items the registry lacks (as VillageEconomy does for village goods).</summary>
        public static void EnsureRegistered()
        {
            foreach (var e in Extra)
                if (QualifiedItemRegistry.GetData(e.Id) == null)
                    QualifiedItemRegistry.Register("O", e.Id, e.Name, e.Description, e.Category, e.UnitGttc, 0, "night_market");
            foreach (var t in EquipmentAndTacticalCatalog.AllTwentyTacticalItems)
                if (QualifiedItemRegistry.GetData(t.Id) == null)
                    QualifiedItemRegistry.Register("O", t.Id, t.Name, t.Description, t.Category, t.ReferenceValueGold, 0, "night_market");
        }

        private static Entry? FromTactical(string id, Kind kind)
        {
            foreach (var t in EquipmentAndTacticalCatalog.AllTwentyTacticalItems)
                if (t.Id == id)
                    return new Entry { Id = t.Id, Name = t.Name, Tier = t.Tier, UnitGttc = t.ReferenceValueGold, Category = t.Category,
                        Description = t.Description, Kind = kind, LotQuantity = t.ReferenceValueGold < 100 ? 2 : 1 };
            return null;
        }

        private static Entry? FromRegistry(string id, Kind kind, int lot)
        {
            var d = QualifiedItemRegistry.GetData(id);
            if (d == null || d.BasePriceGold <= 0) return null;
            return new Entry { Id = d.ItemId, Name = d.DisplayName, Tier = NightMarketBarter.TierOf(id), UnitGttc = d.BasePriceGold,
                Category = d.Category, Description = d.Description, Kind = kind, LotQuantity = lot };
        }

        private static Entry? Resolve(string id, Kind kind, int lot = 1) =>
            Find(id) is Entry extra
                ? new Entry { Id = extra.Id, Name = extra.Name, Tier = extra.Tier, UnitGttc = extra.UnitGttc, Category = extra.Category,
                    Description = extra.Description, Kind = kind, LotQuantity = extra.LotQuantity }
                : FromTactical(id, kind) ?? FromRegistry(id, kind, lot);

        public static List<Entry> NightOffers(IEnumerable<(string id, string name, int cost, bool bachVan)>? blueprints)
        {
            EnsureRegistered();
            var list = new List<Entry>();
            void Add(Entry? e) { if (e != null && e.UnitGttc > 0) list.Add(e); }
            Add(Resolve("TraLaVong", Kind.Consumable));
            foreach (var id in TacticalOfferIds) Add(Resolve(id, Kind.Consumable));
            foreach (var id in MedicineOfferIds) Add(Resolve(id, Kind.Medicine, 3));
            // papers are not in the weighted pool: they come by their own rare roll (GDD 10.3, EconomyManager)
            Add(Resolve(RareSources.SecretManualId, Kind.Equipment));   // GDD 20: bí kíp by barter; Mùa Trang Bị ×2
            Add(Resolve(RareSources.InkId, Kind.Material, 3));          // GDD 10.6: Mực Ấn (I, chợ đêm)
            Add(Resolve("TenThep", Kind.Ammo));
            Add(Resolve("XangSinhHoc", Kind.Material, 2));
            Add(Resolve("TinhThach", Kind.Material, 1));
            foreach (var e in GearOffers()) Add(e);
            if (blueprints != null)
                foreach (var bp in blueprints)
                    if (!bp.bachVan && bp.cost > 0)   // Bạch Vân blueprints only at the auction and from Bí nhân (GDD 20)
                        list.Add(BlueprintEntry(bp));
            return list;
        }

        public static List<Entry> MysticOffers(IEnumerable<(string id, string name, int cost, bool bachVan)>? blueprints)
        {
            EnsureRegistered();
            var list = new List<Entry>();
            foreach (var id in MysticOfferIds)
                if (Resolve(id, Kind.Mystic) is Entry e && e.UnitGttc > 0) { e.LotQuantity = 1; list.Add(e); }
            // "Bản vẽ Cao cấp/Bạch Vân": Bạch Vân if any is still locked, else the three costliest locked blueprints
            var bps = (blueprints ?? Enumerable.Empty<(string id, string name, int cost, bool bachVan)>()).Where(b => b.cost > 0).ToList();
            var rare = bps.Where(b => b.bachVan).ToList();
            if (rare.Count == 0) rare = bps.OrderByDescending(b => b.cost).ThenBy(b => b.id, StringComparer.Ordinal).Take(3).ToList();
            foreach (var bp in rare) list.Add(BlueprintEntry(bp));
            return list;
        }

        private static Entry BlueprintEntry((string id, string name, int cost, bool bachVan) bp) => new Entry
        {
            Id = NightMarketBarter.BlueprintPrefix + bp.id, Name = "Bản vẽ " + bp.name, Tier = NightMarketBarter.TierByGttc(bp.cost),
            UnitGttc = bp.cost, Category = "Blueprint", Description = "Học bản vẽ công trình này vĩnh viễn.", Kind = Kind.Blueprint,
            IsBachVan = bp.bachVan,
        };

        /// <summary>The item wants a stall can ask for (non-pig): (id, kind used for the theme bias).</summary>
        public static readonly (string id, Kind kind)[] ItemWants =
        {
            ("CamTangTrong", Kind.Feed), ("CamThuong", Kind.Feed), ("XangSinhHoc", Kind.Material), ("PhanHuuCo", Kind.Material),
            ("TenThep", Kind.Ammo), ("BinhHoiLuc", Kind.Consumable), ("BangGac", Kind.Consumable), ("PhaoSang", Kind.Consumable),
            ("LuuKhoi", Kind.Consumable), ("BomKeo", Kind.Consumable), ("ThuocHoHap", Kind.Medicine), ("VacXinTongHop", Kind.Medicine),
        };

        public static readonly WantedPigKind[] NightPigWants =
            { WantedPigKind.AdultGradeA, WantedPigKind.PigletCommon, WantedPigKind.IdentifiedKhaPlus, WantedPigKind.AnyAdult };

        public static string PigKindNameVi(WantedPigKind kind) => kind switch
        {
            WantedPigKind.AdultGradeA => "heo trưởng thành hạng A",
            WantedPigKind.PigletCommon => "heo con Thường",
            WantedPigKind.IdentifiedKhaPlus => "heo gen Khá+ đã định danh",
            WantedPigKind.AnyAdult => "heo trưởng thành",
            WantedPigKind.Unidentified => "heo gen \"?\"",
            WantedPigKind.QuyPlus => "heo Quý+ đã định danh",
            WantedPigKind.Elder => "heo già",
            WantedPigKind.Corpse => "xác heo",
            _ => "heo",
        };

        /// <summary>A template pig of a kind, priced by the live day market to estimate a want's GTTC.</summary>
        public static Pig TemplatePig(WantedPigKind kind)
        {
            var rarity = kind switch
            {
                WantedPigKind.IdentifiedKhaPlus => GeneRarity.Kha,
                WantedPigKind.Unidentified => GeneRarity.Hiem,
                WantedPigKind.QuyPlus => GeneRarity.Quy,
                _ => GeneRarity.Thuong,
            };
            var pig = Pig.CreateDefault("_want_template", "", GeneLineId.HongDien, rarity, initialAge: 20, initialWeight: 100f);
            pig.Stage = kind == WantedPigKind.PigletCommon ? PigStage.HeoNon : kind == WantedPigKind.Elder ? PigStage.HeoGia : PigStage.TruongThanh;
            if (kind == WantedPigKind.PigletCommon) pig.WeightKg = 8f;
            if (kind == WantedPigKind.AdultGradeA) pig.MeatQuality = MeatQuality.A;
            pig.IsIdentified = kind == WantedPigKind.IdentifiedKhaPlus || kind == WantedPigKind.QuyPlus || rarity == GeneRarity.Thuong;
            if (kind == WantedPigKind.Corpse) pig.IsAlive = false;
            return pig;
        }
    }
}
