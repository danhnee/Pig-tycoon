using System;
using System.Collections.Generic;
using System.Globalization;

namespace PigTycoon.Core
{
    #region 1. DeterministicHashUtility & DaySaveRandom (StardewValley.Hashing.HashUtility & Utility.CreateDaySaveRandom)

    /// <summary>
    /// Bộ băm tất định đa nền tảng (FNV-1a &amp; xxHash32 avalanche) và bộ sinh số ngẫu nhiên theo hạt giống Ngày/Lưu
    /// lấy cảm hứng trực tiếp từ <c>StardewValley.Hashing.HashUtility</c> và <c>StardewValley.Utility.CreateDaySaveRandom</c>.
    /// Đảm bảo <c>SYNCED_RANDOM</c> trong <see cref="GameStateQuery"/> luôn trả về kết quả 100% giống nhau trên mọi hệ điều hành.
    /// </summary>
    public static class DeterministicHashUtility
    {
        private const uint FnvPrime = 16777619u;
        private const uint FnvOffsetBasis = 2166136261u;

        public static int GetDeterministicHashCode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            uint hash = FnvOffsetBasis;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                hash ^= (byte)(c & 0xFF);
                hash *= FnvPrime;
                hash ^= (byte)((c >> 8) & 0xFF);
                hash *= FnvPrime;
            }

            return Avalanche32((int)hash);
        }

        public static int GetDeterministicHashCode(params int[] values)
        {
            if (values == null || values.Length == 0)
                return 0;

            uint hash = FnvOffsetBasis;
            for (int i = 0; i < values.Length; i++)
            {
                uint v = (uint)values[i];
                hash ^= v & 0xFFu;
                hash *= FnvPrime;
                hash ^= (v >> 8) & 0xFFu;
                hash *= FnvPrime;
                hash ^= (v >> 16) & 0xFFu;
                hash *= FnvPrime;
                hash ^= (v >> 24) & 0xFFu;
                hash *= FnvPrime;
            }

            return Avalanche32((int)hash);
        }

        private static int Avalanche32(int h)
        {
            uint x = (uint)h;
            x ^= x >> 15;
            x *= 0x85EBCA77u;
            x ^= x >> 13;
            x *= 0xC2B2AE3Du;
            x ^= x >> 16;
            return (int)(x & 0x7FFFFFFFu);
        }

        public static Random CreateDaySaveRandom(int baseSeed, int daysPlayed, int subSeedA = 0, int subSeedB = 0, int subSeedC = 0)
        {
            int seed = GetDeterministicHashCode(baseSeed, daysPlayed, subSeedA, subSeedB, subSeedC);
            return new Random(seed);
        }
    }

    #endregion

    #region 2. QualifiedItemRegistry, ParsedItemData, ItemContextTagManager, ItemQueryResolver & UnifiedInventory

    /// <summary>
    /// Định nghĩa dữ liệu vật phẩm đã phân tích cú pháp, tương đương <c>StardewValley.ItemTypeDefinitions.ParsedItemData</c>.
    /// Mọi vật phẩm trong Pig Tycoon đều có <see cref="QualifiedItemId"/> chuẩn hóa theo tiền tố loại:
    /// <list type="bullet">
    /// <item><description><c>(F)</c> — Thức ăn chăn nuôi (Feed)</description></item>
    /// <item><description><c>(M)</c> — Thuốc thú y, thảo dược &amp; vắc-xin (Medicine)</description></item>
    /// <item><description><c>(G)</c> — Giấy Đặc Tính &amp; vật phẩm giám định gen (Gene / Specialty Paper)</description></item>
    /// <item><description><c>(W)</c> — Vũ khí, giáp &amp; bùa hộ mệnh (Weapon / Equipment)</description></item>
    /// <item><description><c>(O)</c> — Nguyên liệu, nông sản, nhiên liệu &amp; kỷ vật cốt truyện (Object / Relic)</description></item>
    /// <item><description><c>(BC)</c> — Máy chế biến &amp; thiết bị trại (BigCraftable / Machine)</description></item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ParsedItemData
    {
        public string TypeDefinitionId { get; set; } = "(O)";
        public string ItemId { get; set; } = string.Empty;
        public string QualifiedItemId { get; set; } = string.Empty;
        public string InternalName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Object";
        public int BasePriceGold { get; set; }
        public float WeightKgPerUnit { get; set; } = 0.5f;
        public List<string> ContextTags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Thực thể vật phẩm trong kho đồ hoặc máy chế biến, tương đương <c>StardewValley.Item</c>.
    /// </summary>
    [Serializable]
    public sealed class UnifiedItemInstance
    {
        public string QualifiedItemId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Stack { get; set; } = 1;
        public int Quality { get; set; } = 0; // 0: Thường, 1: Bạc, 2: Vàng, 3: Tinh Tuyển

        public UnifiedItemInstance Clone(int? overrideStack = null)
        {
            return new UnifiedItemInstance
            {
                QualifiedItemId = QualifiedItemId,
                ItemId = ItemId,
                DisplayName = DisplayName,
                Stack = overrideStack ?? Stack,
                Quality = Quality
            };
        }
    }

    /// <summary>
    /// Quản lý Context Tags cho mọi vật phẩm trong game, tương đương <c>StardewValley.ItemContextTagManager</c>.
    /// Hỗ trợ kiểm tra tag trực tiếp và phủ định bằng dấu <c>!</c>.
    /// </summary>
    public static class ItemContextTagManager
    {
        private static readonly Dictionary<string, HashSet<string>> BaseTagsByQualifiedId =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public static string SanitizeContextTag(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            return raw.Trim().Replace(' ', '_').Replace('-', '_').ToLowerInvariant();
        }

        public static ISet<string> GetBaseContextTags(string itemIdOrQualifiedId)
        {
            var data = QualifiedItemRegistry.GetData(itemIdOrQualifiedId);
            if (data == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (BaseTagsByQualifiedId.TryGetValue(data.QualifiedItemId, out var cached))
                return cached;

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "item_" + SanitizeContextTag(data.InternalName),
                "id_" + SanitizeContextTag(data.TypeDefinitionId.Trim('(', ')')) + "_" + SanitizeContextTag(data.ItemId),
                "category_" + SanitizeContextTag(data.Category)
            };

            for (int i = 0; i < data.ContextTags.Count; i++)
            {
                string sanitized = SanitizeContextTag(data.ContextTags[i]);
                if (!string.IsNullOrEmpty(sanitized))
                    set.Add(sanitized);
            }

            BaseTagsByQualifiedId[data.QualifiedItemId] = set;
            return set;
        }

        public static bool HasBaseTag(string itemIdOrQualifiedId, string queryTag)
        {
            if (string.IsNullOrWhiteSpace(queryTag))
                return false;

            var tags = GetBaseContextTags(itemIdOrQualifiedId);
            return DoesTagMatch(queryTag.Trim(), tags);
        }

        public static bool DoesTagMatch(string queryTag, ISet<string> tags)
        {
            if (string.IsNullOrWhiteSpace(queryTag) || tags == null)
                return false;

            string trimmed = queryTag.Trim();
            if (trimmed.StartsWith("!", StringComparison.Ordinal))
            {
                string inner = SanitizeContextTag(trimmed.Substring(1));
                return !tags.Contains(inner);
            }

            return tags.Contains(SanitizeContextTag(trimmed));
        }

        public static void ClearCache()
        {
            BaseTagsByQualifiedId.Clear();
        }
    }

    /// <summary>
    /// Sổ đăng ký vật phẩm chuẩn hóa toàn cục tương đương <c>StardewValley.ItemRegistry</c>.
    /// Hợp nhất toàn bộ Cám, Thuốc, Giấy Đặc Tính, Vũ Khí, Nguyên Liệu, Kỷ Vật và Máy Chế Biến về một chuẩn duy nhất.
    /// </summary>
    public static class QualifiedItemRegistry
    {
        public const string PrefixFeed = "(F)";
        public const string PrefixMedicine = "(M)";
        public const string PrefixGenePaper = "(G)";
        public const string PrefixWeaponEquip = "(W)";
        public const string PrefixObject = "(O)";
        public const string PrefixBigCraftable = "(BC)";

        private static readonly Dictionary<string, ParsedItemData> ByQualifiedId =
            new Dictionary<string, ParsedItemData>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> UnqualifiedToQualifiedMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static QualifiedItemRegistry()
        {
            RegisterCanonicalCatalog();
        }

        public static void RegisterCanonicalCatalog()
        {
            ByQualifiedId.Clear();
            UnqualifiedToQualifiedMap.Clear();
            ItemContextTagManager.ClearCache();

            // 1. Nhóm Thức Ăn Chăn Nuôi (F) — GDD 4.2 & DataCatalog
            Register(PrefixFeed, "CamThuong", "Cám Thường", "Cám tiêu chuẩn cho đàn heo (3.5 kg/khẩu phần).", "Feed", 12, 3.5f,
                "category_feed", "feed_basic", "farm_supply");
            Register(PrefixFeed, "CamTangTrong", "Cám Tăng Trọng", "Tăng +25% tốc độ tăng trưởng, giảm nhẹ độ bền sức khỏe.", "Feed", 28, 3.5f,
                "category_feed", "feed_growth", "farm_supply");
            Register(PrefixFeed, "CamThaoDuoc", "Cám Thảo Dược", "Tăng đề kháng bệnh +15% và hồi Tinh thần +4.", "Feed", 35, 3.5f,
                "category_feed", "feed_herbal", "farm_supply");
            Register(PrefixFeed, "CamTinhLuyen", "Cám Tinh Luyện", "Cám cao cấp cho heo giống Quý/Truyền Thuyết (+15% tăng trọng, +8 Tinh thần).", "Feed", 65, 3.5f,
                "category_feed", "feed_refined", "rarity_epic");

            // 2. Nhóm Thuốc Thú Y & Vắc-xin (M) — GDD 6.3
            Register(PrefixMedicine, "ThuocHoHap", "Thuốc Hô Hấp", "Đặc trị Hô Hấp Lạnh & Khò Khè cho đàn heo.", "Medicine", 45, 0.2f,
                "category_medicine", "cure_respiratory");
            Register(PrefixMedicine, "ThuocTieuHoa", "Thuốc Tiêu Hóa", "Đặc trị Sốt Bùn & Chướng Bụng.", "Medicine", 40, 0.2f,
                "category_medicine", "cure_digestive");
            Register(PrefixMedicine, "ThuocKySinh", "Thuốc Ký Sinh", "Đặc trị Ghẻ Ký Sinh trong thời tiết Nồm Ẩm.", "Medicine", 38, 0.2f,
                "category_medicine", "cure_parasite");
            Register(PrefixMedicine, "ThuocHaNhiet", "Thuốc Hạ Nhiệt", "Đặc trị Say Nắng & Sốt Cấp.", "Medicine", 50, 0.2f,
                "category_medicine", "cure_heatstroke");
            Register(PrefixMedicine, "ThuocAnThan", "Thuốc An Thần", "Ổn định Tinh thần cho heo Hoảng Loạn hoặc Lôi Tâm.", "Medicine", 55, 0.2f,
                "category_medicine", "cure_panic");
            Register(PrefixMedicine, "VacXinTongHop", "Vắc-xin Tổng Hợp", "Phòng ngừa dịch bệnh toàn đàn trong 5 ngày.", "Medicine", 85, 0.3f,
                "category_medicine", "vaccine_item");
            Register(PrefixMedicine, "ThaoDuocKho", "Thảo Dược Khô", "Nguyên liệu bào chế thuốc và cám thảo dược.", "Medicine", 18, 0.25f,
                "category_medicine", "herb_item", "craft_ingredient");

            // 3. Nhóm Giấy Đặc Tính & Vật Phẩm Gen (G) — GDD 10.5
            Register(PrefixGenePaper, "Paper_Blue", "Giấy Đặc Tính Lam", "Giấy mở khóa Đặc Tính cơ bản (tỷ lệ gốc 55%).", "GenePaper", 150, 0.1f,
                "category_gene_paper", "paper_blue", "specialty_unlock");
            Register(PrefixGenePaper, "Paper_Yellow", "Giấy Đặc Tính Vàng", "Giấy mở khóa Đặc Tính trung cấp (tỷ lệ gốc 72%).", "GenePaper", 320, 0.1f,
                "category_gene_paper", "paper_yellow", "specialty_unlock");
            Register(PrefixGenePaper, "Paper_Purple", "Giấy Đặc Tính Tím", "Giấy mở khóa Đặc Tính cao cấp (tỷ lệ gốc 86%).", "GenePaper", 640, 0.1f,
                "category_gene_paper", "paper_purple", "specialty_unlock", "rarity_rare");
            Register(PrefixGenePaper, "Paper_Red", "Giấy Đặc Tính Đỏ", "Giấy mở khóa Đặc Tính bảo đảm 100%.", "GenePaper", 1200, 0.1f,
                "category_gene_paper", "paper_red", "specialty_unlock", "rarity_legendary");
            Register(PrefixGenePaper, "MauGenTho", "Mẫu Gen Thô", "Mẫu sinh học thu từ heo Quý/Hiếm để giải mã tại Phòng Nghiên Cứu.", "GenePaper", 95, 0.2f,
                "category_gene_paper", "gene_sample", "machine_input");
            Register(PrefixGenePaper, "AnTinGiaToc24", "Miếng Da Cổ Số 24", "Kỷ vật gia tộc khắc số 24 dùng cho khe lò sưởi nhà chính (S04/Q16).", "GenePaper", 0, 0.1f,
                "category_gene_paper", "story_quest_item", "relic_item");

            // 4. Nhóm Vũ Khí, Giáp & Bùa Hộ Mệnh (W) — GDD 8.2, 12.6, 24.1
            Register(PrefixWeaponEquip, "gay_sat", "Gậy Sắt Trấn Chuồng", "Vũ khí cận chiến (+6 STR).", "Equipment", 220, 2.5f,
                "category_equipment", "weapon_item");
            Register(PrefixWeaponEquip, "dao_bau", "Dao Bầu Thợ Mổ", "Vũ khí tốc độ cao (+4 STR, +5 AGI).", "Equipment", 280, 1.5f,
                "category_equipment", "weapon_item");
            Register(PrefixWeaponEquip, "thuong_tam_vong", "Thương Tầm Vông", "Vũ khí tầm xa giữ khoảng cách (+5 STR, +4 DEF).", "Equipment", 310, 2.0f,
                "category_equipment", "weapon_item");
            Register(PrefixWeaponEquip, "bua_ta", "Búa Tạ Phá Giáp", "Vũ khí hạng nặng (+10 STR, -2 AGI).", "Equipment", 450, 5.0f,
                "category_equipment", "weapon_item");
            Register(PrefixWeaponEquip, "ao_vai_day", "Áo Vải Dày", "Giáp nhẹ chống cào cấu (+5 DEF).", "Equipment", 160, 1.5f,
                "category_equipment", "armor_item");
            Register(PrefixWeaponEquip, "giap_da_heo", "Giáp Da Heo Thiết Bì", "Giáp cứng chế từ da Thiết Bì (+12 DEF, +2 END).", "Equipment", 520, 4.0f,
                "category_equipment", "armor_item");
            Register(PrefixWeaponEquip, "vong_co_bac", "Vòng Cổ Bạc (Kỷ Vật)", "Kỷ vật phản chiếu ánh trăng ở Q19 (+4 END, +4 AGI).", "Equipment", 380, 0.2f,
                "category_equipment", "accessory_item", "story_quest_item", "reflective_relic");
            Register(PrefixWeaponEquip, "LaBanTanVu", "La Bàn Tàn Vụ", "Bảo vật định hướng trong Sương Mù và sự kiện huyền bí E01–E14.", "Equipment", 500, 0.4f,
                "category_equipment", "occult_ward", "accessory_item");
            Register(PrefixWeaponEquip, "BuaHoiHuong", "Bùa Hồi Hướng", "Bùa trấn áp tà khí trong 14 sự kiện huyền bí Bờ Rìa.", "Equipment", 420, 0.1f,
                "category_equipment", "occult_ward", "accessory_item");

            // 5. Nhóm Nguyên Liệu, Nông Sản, Nhiên Liệu & Kỷ Vật (O)
            Register(PrefixObject, "PhanHeoTho", "Phân Heo Thô", "Phụ phẩm thu gom hàng ngày từ chuồng trại, dùng ủ phân hoặc nạp hầm Biogas.", "Resource", 5, 1.0f,
                "category_resource", "manure_item", "machine_input");
            Register(PrefixObject, "PhanHuuCo", "Phân Hữu Cơ Tinh Chế", "Phân bón sinh học đã ủ chín giúp tăng Sinh Cảnh và bán giá cao.", "Resource", 32, 1.0f,
                "category_resource", "fertilizer_item", "machine_output");
            Register(PrefixObject, "XangSinhHoc", "Xăng Sinh Học (Can 5L)", "Nhiên liệu vận hành Máy Phát Điện Phụ của trại (+5 Lít).", "Resource", 45, 4.0f,
                "category_resource", "fuel_item", "machine_output");
            Register(PrefixObject, "TinhThach", "Tinh Thạch Bạch Vân", "Đá năng lượng rơi từ quái tinh anh dùng nâng cấp giấy đặc tính và tháp Bạch Vân.", "Resource", 180, 0.5f,
                "category_resource", "crystal_item", "rarity_rare", "machine_input");
            Register(PrefixObject, "ManhGiayKienHang", "Mảnh Giấy Kiện Hàng #1–#6", "Mảnh ghi chú tìm thấy trong các kiện hàng gửi về trại.", "Relic", 0, 0.05f,
                "category_relic", "story_quest_item");
            Register(PrefixObject, "BatMenXanhSutMieng", "Bát Men Xanh Sứt Miệng", "Chiếc bát đặt ở cổng trại sau khi cắt kênh thứ 7 (Kết thúc Mùa 1).", "Relic", 0, 0.3f,
                "category_relic", "story_quest_item");

            // 6. Nhóm Máy Chế Biến Trại (BC) — tương đương Data/Machines.json của Stardew Valley 1.6
            Register(PrefixBigCraftable, "MayTronCam", "Máy Trộn Cám Thảo Dược", "Chế biến Cám Thường + Thảo Dược Khô thành Cám Thảo Dược.", "Machine", 600, 15.0f,
                "category_machine", "farm_machine");
            Register(PrefixBigCraftable, "HamBiogas", "Hầm Khí Sinh Học Biogas", "Chuyển hóa Phân Heo Thô thành Xăng Sinh Học (+5L) và giảm ô nhiễm chuồng.", "Machine", 850, 25.0f,
                "category_machine", "farm_machine");
            Register(PrefixBigCraftable, "HoUPhan", "Hố Ủ Phân Vi Sinh", "Ủ Phân Heo Thô thành Phân Hữu Cơ Tinh Chế.", "Machine", 350, 10.0f,
                "category_machine", "farm_machine");
            Register(PrefixBigCraftable, "BanGiaiMaGen", "Bàn Giải Mã Gen", "Phân tích Mẫu Gen Thô thành Giấy Đặc Tính Lam/Vàng.", "Machine", 1100, 18.0f,
                "category_machine", "farm_machine");
            Register(PrefixBigCraftable, "BanEpGiayDacTinh", "Bàn Ép Giấy Đặc Tính", "Kết hợp Giấy Đặc Tính cùng Tinh Thạch để nâng bậc giấy.", "Machine", 1400, 20.0f,
                "category_machine", "farm_machine");
        }

        public static ParsedItemData Register(
            string typePrefix,
            string itemId,
            string displayName,
            string description,
            string category,
            int basePriceGold,
            float weightKg,
            params string[] contextTags)
        {
            string normalizedPrefix = typePrefix.StartsWith("(", StringComparison.Ordinal)
                ? typePrefix
                : "(" + typePrefix.Trim() + ")";
            string qualifiedId = normalizedPrefix + itemId;

            var parsed = new ParsedItemData
            {
                TypeDefinitionId = normalizedPrefix,
                ItemId = itemId,
                QualifiedItemId = qualifiedId,
                InternalName = itemId,
                DisplayName = displayName,
                Description = description,
                Category = category,
                BasePriceGold = basePriceGold,
                WeightKgPerUnit = weightKg,
                ContextTags = contextTags != null ? new List<string>(contextTags) : new List<string>()
            };

            ByQualifiedId[qualifiedId] = parsed;
            if (!UnqualifiedToQualifiedMap.ContainsKey(itemId))
            {
                UnqualifiedToQualifiedMap[itemId] = qualifiedId;
            }

            return parsed;
        }

        public static string QualifyItemId(string itemIdOrQualifiedId)
        {
            if (string.IsNullOrWhiteSpace(itemIdOrQualifiedId))
                return string.Empty;

            string trimmed = itemIdOrQualifiedId.Trim();
            if (ByQualifiedId.TryGetValue(trimmed, out var exact))
                return exact.QualifiedItemId;

            if (UnqualifiedToQualifiedMap.TryGetValue(trimmed, out var mapped))
                return mapped;

            if (trimmed.StartsWith("(", StringComparison.Ordinal))
                return trimmed;

            return PrefixObject + trimmed;
        }

        public static ParsedItemData? GetData(string itemIdOrQualifiedId)
        {
            string qid = QualifyItemId(itemIdOrQualifiedId);
            if (string.IsNullOrEmpty(qid))
                return null;
            return ByQualifiedId.TryGetValue(qid, out var data) ? data : null;
        }

        public static UnifiedItemInstance Create(string itemIdOrQualifiedId, int amount = 1, int quality = 0)
        {
            var data = GetData(itemIdOrQualifiedId);
            string qid = data != null ? data.QualifiedItemId : QualifyItemId(itemIdOrQualifiedId);
            string rawId = data != null ? data.ItemId : itemIdOrQualifiedId.Trim();
            string displayName = data != null ? data.DisplayName : rawId;

            return new UnifiedItemInstance
            {
                QualifiedItemId = qid,
                ItemId = rawId,
                DisplayName = displayName,
                Stack = Math.Max(1, amount),
                Quality = Math.Clamp(quality, 0, 3)
            };
        }

        public static IReadOnlyCollection<ParsedItemData> GetAllDefinitions()
        {
            return ByQualifiedId.Values;
        }
    }

    /// <summary>
    /// Bộ phân giải truy vấn vật phẩm đa năng, tương đương <c>StardewValley.Internal.ItemQueryResolver</c>.
    /// Hỗ trợ:
    /// <list type="bullet">
    /// <item><description><c>ALL_ITEMS [typePrefix]</c></description></item>
    /// <item><description><c>ITEMS_BY_TAG &lt;tag1&gt; [tag2...]</c></description></item>
    /// <item><description><c>RANDOM_ITEMS &lt;tagOrPrefix&gt; [count]</c></description></item>
    /// <item><description>Hoặc truyền trực tiếp mã vật phẩm có/không có tiền tố (ví dụ <c>(F)CamThaoDuoc</c> hoặc <c>CamThaoDuoc</c>)</description></item>
    /// </list>
    /// </summary>
    public static class ItemQueryResolver
    {
        public static List<UnifiedItemInstance> TryResolve(string query, Random? rng = null, int defaultStack = 1)
        {
            var results = new List<UnifiedItemInstance>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            string[] tokens = query.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return results;

            string head = tokens[0].ToUpperInvariant();
            if (head == "ALL_ITEMS")
            {
                string prefixFilter = tokens.Length > 1 ? tokens[1] : string.Empty;
                foreach (var def in QualifiedItemRegistry.GetAllDefinitions())
                {
                    if (string.IsNullOrEmpty(prefixFilter) ||
                        def.TypeDefinitionId.Equals(prefixFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(QualifiedItemRegistry.Create(def.QualifiedItemId, defaultStack));
                    }
                }
                return results;
            }

            if (head == "ITEMS_BY_TAG")
            {
                foreach (var def in QualifiedItemRegistry.GetAllDefinitions())
                {
                    bool allMatch = true;
                    for (int i = 1; i < tokens.Length; i++)
                    {
                        if (!ItemContextTagManager.HasBaseTag(def.QualifiedItemId, tokens[i]))
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch)
                    {
                        results.Add(QualifiedItemRegistry.Create(def.QualifiedItemId, defaultStack));
                    }
                }
                return results;
            }

            if (head == "RANDOM_ITEMS")
            {
                string filter = tokens.Length > 1 ? tokens[1] : string.Empty;
                int pickCount = tokens.Length > 2 && int.TryParse(tokens[2], out int parsedCount)
                    ? Math.Max(1, parsedCount)
                    : 1;

                var pool = new List<ParsedItemData>();
                foreach (var def in QualifiedItemRegistry.GetAllDefinitions())
                {
                    if (string.IsNullOrEmpty(filter) ||
                        def.TypeDefinitionId.Equals(filter, StringComparison.OrdinalIgnoreCase) ||
                        ItemContextTagManager.HasBaseTag(def.QualifiedItemId, filter))
                    {
                        pool.Add(def);
                    }
                }

                if (pool.Count > 0)
                {
                    var effectiveRng = rng ?? new Random(20260601);
                    for (int i = 0; i < pickCount; i++)
                    {
                        var chosen = pool[effectiveRng.Next(pool.Count)];
                        results.Add(QualifiedItemRegistry.Create(chosen.QualifiedItemId, defaultStack));
                    }
                }
                return results;
            }

            // Mặc định: mã vật phẩm đơn lẻ kèm số lượng tùy chọn
            int stack = tokens.Length > 1 && int.TryParse(tokens[1], out int s) ? Math.Max(1, s) : defaultStack;
            results.Add(QualifiedItemRegistry.Create(tokens[0], stack));
            return results;
        }
    }

    /// <summary>
    /// Giao diện kho đồ chuẩn hóa tương đương <c>StardewValley.Inventories.IInventory</c>.
    /// </summary>
    public interface IUnifiedInventory
    {
        IReadOnlyList<UnifiedItemInstance> Slots { get; }
        int CountId(string itemIdOrQualifiedId);
        int CountByContextTag(string tag);
        bool ContainsId(string itemIdOrQualifiedId, int minimum = 1);
        int AddItem(string itemIdOrQualifiedId, int amount = 1, int quality = 0);
        bool ConsumeItem(string itemIdOrQualifiedId, int amount = 1);
    }

    /// <summary>
    /// Kho đồ hợp nhất của người chơi &amp; nông trại, tự động đồng bộ hai chiều với <see cref="Farm"/>
    /// (<c>FeedStockKg</c>, <c>MedicalHerbCount</c>, <c>VaccineDoseCount</c>, <c>FuelLiters</c>).
    /// </summary>
    [Serializable]
    public sealed class UnifiedInventory : IUnifiedInventory
    {
        private readonly List<UnifiedItemInstance> slots = new List<UnifiedItemInstance>();
        [NonSerialized]
        private Farm? boundFarm;

        public IReadOnlyList<UnifiedItemInstance> Slots => slots;

        public void BindFarm(Farm farm)
        {
            boundFarm = farm;
        }

        public int CountId(string itemIdOrQualifiedId)
        {
            string qid = QualifiedItemRegistry.QualifyItemId(itemIdOrQualifiedId);
            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].QualifiedItemId.Equals(qid, StringComparison.OrdinalIgnoreCase))
                    total += slots[i].Stack;
            }
            return total;
        }

        public int CountByContextTag(string tag)
        {
            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (ItemContextTagManager.HasBaseTag(slots[i].QualifiedItemId, tag))
                    total += slots[i].Stack;
            }
            return total;
        }

        public bool ContainsId(string itemIdOrQualifiedId, int minimum = 1)
        {
            return CountId(itemIdOrQualifiedId) >= Math.Max(1, minimum);
        }

        public int AddItem(string itemIdOrQualifiedId, int amount = 1, int quality = 0)
        {
            if (amount <= 0)
                return 0;

            var created = QualifiedItemRegistry.Create(itemIdOrQualifiedId, amount, quality);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].QualifiedItemId.Equals(created.QualifiedItemId, StringComparison.OrdinalIgnoreCase) &&
                    slots[i].Quality == created.Quality)
                {
                    slots[i].Stack += amount;
                    SyncAdditionToFarmStocks(created.QualifiedItemId, amount);
                    return slots[i].Stack;
                }
            }

            slots.Add(created);
            SyncAdditionToFarmStocks(created.QualifiedItemId, amount);
            return created.Stack;
        }

        public bool ConsumeItem(string itemIdOrQualifiedId, int amount = 1)
        {
            if (amount <= 0)
                return true;

            string qid = QualifiedItemRegistry.QualifyItemId(itemIdOrQualifiedId);
            if (CountId(qid) < amount)
                return false;

            int remaining = amount;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (!slots[i].QualifiedItemId.Equals(qid, StringComparison.OrdinalIgnoreCase))
                    continue;

                int take = Math.Min(slots[i].Stack, remaining);
                slots[i].Stack -= take;
                remaining -= take;
                if (slots[i].Stack <= 0)
                    slots.RemoveAt(i);
            }

            SyncConsumptionToFarmStocks(qid, amount);
            return true;
        }

        public void ClearAndRestore(IEnumerable<UnifiedItemInstance>? savedItems)
        {
            slots.Clear();
            if (savedItems == null)
                return;

            foreach (var item in savedItems)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.QualifiedItemId) && item.Stack > 0)
                {
                    slots.Add(item.Clone());
                }
            }
        }

        private void SyncAdditionToFarmStocks(string qualifiedId, int amount)
        {
            if (boundFarm == null)
                return;

            if (ItemContextTagManager.HasBaseTag(qualifiedId, "category_feed"))
            {
                boundFarm.FeedStockKg += amount * 3.5f;
            }
            else if (qualifiedId.Equals("(M)ThaoDuocKho", StringComparison.OrdinalIgnoreCase))
            {
                boundFarm.MedicalHerbCount += amount;
            }
            else if (qualifiedId.Equals("(M)VacXinTongHop", StringComparison.OrdinalIgnoreCase))
            {
                boundFarm.VaccineDoseCount += amount;
            }
            else if (qualifiedId.Equals("(O)XangSinhHoc", StringComparison.OrdinalIgnoreCase))
            {
                boundFarm.FuelLiters += amount * 5;
            }
        }

        private void SyncConsumptionToFarmStocks(string qualifiedId, int amount)
        {
            if (boundFarm == null)
                return;

            if (ItemContextTagManager.HasBaseTag(qualifiedId, "category_feed"))
            {
                boundFarm.FeedStockKg = Math.Max(0f, boundFarm.FeedStockKg - amount * 3.5f);
            }
            else if (qualifiedId.Equals("(M)ThaoDuocKho", StringComparison.OrdinalIgnoreCase))
            {
                boundFarm.MedicalHerbCount = Math.Max(0, boundFarm.MedicalHerbCount - amount);
            }
            else if (qualifiedId.Equals("(M)VacXinTongHop", StringComparison.OrdinalIgnoreCase))
            {
                boundFarm.VaccineDoseCount = Math.Max(0, boundFarm.VaccineDoseCount - amount);
            }
        }
    }

    #endregion

    #region 3. GameStateQuery Condition Engine (StardewValley.GameStateQuery & DefaultResolvers)

    /// <summary>
    /// Ngữ cảnh đánh giá điều kiện hợp nhất tương đương <c>StardewValley.Delegates.GameStateQueryContext</c>.
    /// </summary>
    public sealed class GameStateQueryContext
    {
        public GameEngine Engine { get; }
        public GameLocation? Location { get; }
        public UnifiedItemInstance? TargetItem { get; }
        public UnifiedItemInstance? InputItem { get; }
        public Random Random { get; }

        public GameStateQueryContext(
            GameEngine engine,
            GameLocation? location = null,
            UnifiedItemInstance? targetItem = null,
            UnifiedItemInstance? inputItem = null,
            Random? customRandom = null)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Location = location ?? engine.WorldLocations?.CurrentLocation;
            TargetItem = targetItem;
            InputItem = inputItem;
            Random = customRandom ?? DeterministicHashUtility.CreateDaySaveRandom(
                engine.BaseSeed,
                engine.Clock.CurrentDay,
                engine.Clock.CurrentHour,
                engine.TotalPigsSold);
        }
    }

    public delegate bool GameStateQueryDelegate(string[] query, GameStateQueryContext context);

    /// <summary>
    /// Một điều kiện đơn lẻ đã được phân tích cú pháp và lưu bộ nhớ đệm, tương đương <c>StardewValley.GameStateQuery.ParsedGameStateQuery</c>.
    /// </summary>
    public readonly struct ParsedGameStateQuery
    {
        public readonly bool Negated;
        public readonly string[] Query;
        public readonly GameStateQueryDelegate? Resolver;
        public readonly string? Error;

        public ParsedGameStateQuery(bool negated, string[] query, GameStateQueryDelegate? resolver, string? error)
        {
            Negated = negated;
            Query = query;
            Resolver = resolver;
            Error = error;
        }
    }

    /// <summary>
    /// Bộ máy đánh giá điều kiện toàn cục (<c>GameStateQuery</c>) lấy mẫu trực tiếp từ <c>StardewValley.GameStateQuery</c>.
    /// Thay thế toàn bộ các câu lệnh <c>if</c> rời rạc và chuỗi mô tả chết bằng ngôn ngữ truy vấn có bộ nhớ đệm,
    /// hỗ trợ phủ định <c>!</c>, tổ hợp <c>ANY</c>, <c>SYNCED_RANDOM</c> và 36 resolver chuyên biệt của Pig Tycoon GDD v6.0 &amp; Story S1.
    /// </summary>
    public static class GameStateQuery
    {
        private static readonly Dictionary<string, GameStateQueryDelegate> QueryTypeLookup =
            new Dictionary<string, GameStateQueryDelegate>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ParsedGameStateQuery[]> ParseCache =
            new Dictionary<string, ParsedGameStateQuery[]>(StringComparer.Ordinal);

        static GameStateQuery()
        {
            RegisterDefaultResolvers();
        }

        public static void RegisterQueryType(string queryKey, GameStateQueryDelegate queryDelegate)
        {
            if (string.IsNullOrWhiteSpace(queryKey))
                throw new ArgumentException("Khóa GameStateQuery không được rỗng.", nameof(queryKey));
            QueryTypeLookup[queryKey.Trim()] = queryDelegate ?? throw new ArgumentNullException(nameof(queryDelegate));
            ParseCache.Clear();
        }

        public static bool IsImmutablyTrue(string? queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
                return true;
            string trimmed = queryString.Trim();
            return trimmed.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Equals("!FALSE", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsImmutablyFalse(string? queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
                return false;
            string trimmed = queryString.Trim();
            return trimmed.Equals("FALSE", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Equals("!TRUE", StringComparison.OrdinalIgnoreCase);
        }

        public static bool CheckConditions(string? queryString, GameEngine engine, GameLocation? location = null, UnifiedItemInstance? targetItem = null, UnifiedItemInstance? inputItem = null)
        {
            if (string.IsNullOrWhiteSpace(queryString) || IsImmutablyTrue(queryString))
                return true;
            if (IsImmutablyFalse(queryString))
                return false;

            var ctx = new GameStateQueryContext(engine, location, targetItem, inputItem);
            return CheckConditions(queryString, ctx);
        }

        public static bool CheckConditions(string? queryString, GameStateQueryContext context)
        {
            if (string.IsNullOrWhiteSpace(queryString) || IsImmutablyTrue(queryString))
                return true;
            if (IsImmutablyFalse(queryString))
                return false;

            ParsedGameStateQuery[] parsed = Parse(queryString!);
            return CheckConditionsImpl(parsed, context);
        }

        public static ParsedGameStateQuery[] Parse(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
                return Array.Empty<ParsedGameStateQuery>();

            if (ParseCache.TryGetValue(queryString, out var cached))
                return cached;

            List<string> rawQueries = SplitRawQueries(queryString);
            var parsedList = new ParsedGameStateQuery[rawQueries.Count];

            for (int i = 0; i < rawQueries.Count; i++)
            {
                List<string> tokens = SplitTokensRespectingQuotes(rawQueries[i]);
                if (tokens.Count == 0)
                {
                    parsedList[i] = new ParsedGameStateQuery(false, Array.Empty<string>(), Resolver_TRUE, null);
                    continue;
                }

                string key = tokens[0];
                bool negated = false;
                if (key.StartsWith("!", StringComparison.Ordinal))
                {
                    negated = true;
                    key = key.Substring(1);
                    tokens[0] = key;
                }

                if (QueryTypeLookup.TryGetValue(key, out var resolver))
                {
                    parsedList[i] = new ParsedGameStateQuery(negated, tokens.ToArray(), resolver, null);
                }
                else
                {
                    parsedList[i] = new ParsedGameStateQuery(negated, tokens.ToArray(), null, $"Không tìm thấy resolver cho khóa '{key}'.");
                }
            }

            if (ParseCache.Count > 2048)
                ParseCache.Clear();

            ParseCache[queryString] = parsedList;
            return parsedList;
        }

        private static bool CheckConditionsImpl(ParsedGameStateQuery[] parsedQueries, GameStateQueryContext context)
        {
            for (int i = 0; i < parsedQueries.Length; i++)
            {
                var q = parsedQueries[i];
                if (q.Resolver == null)
                    return false;

                bool result = q.Resolver(q.Query, context);
                if (q.Negated)
                    result = !result;

                if (!result)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tách các điều kiện phân cách bởi dấu phẩy <c>,</c> nhưng giữ nguyên dấu phẩy nằm trong cặp ngoặc kép <c>"..."</c>.
        /// </summary>
        public static List<string> SplitRawQueries(string queryString)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(queryString))
                return result;

            bool inQuotes = false;
            int start = 0;
            for (int i = 0; i < queryString.Length; i++)
            {
                char c = queryString[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    string sub = queryString.Substring(start, i - start).Trim();
                    if (sub.Length > 0)
                        result.Add(sub);
                    start = i + 1;
                }
            }

            if (start < queryString.Length)
            {
                string tail = queryString.Substring(start).Trim();
                if (tail.Length > 0)
                    result.Add(tail);
            }

            return result;
        }

        private static List<string> SplitTokensRespectingQuotes(string rawQuery)
        {
            var tokens = new List<string>();
            bool inQuotes = false;
            int tokenStart = -1;

            for (int i = 0; i < rawQuery.Length; i++)
            {
                char c = rawQuery[i];
                if (c == '"')
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                        tokenStart = i + 1;
                    }
                    else
                    {
                        inQuotes = false;
                        tokens.Add(rawQuery.Substring(tokenStart, i - tokenStart));
                        tokenStart = -1;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (tokenStart >= 0)
                    {
                        tokens.Add(rawQuery.Substring(tokenStart, i - tokenStart));
                        tokenStart = -1;
                    }
                }
                else if (tokenStart < 0)
                {
                    tokenStart = i;
                }
            }

            if (tokenStart >= 0)
            {
                tokens.Add(rawQuery.Substring(tokenStart));
            }

            return tokens;
        }

        private static void RegisterDefaultResolvers()
        {
            RegisterQueryType("TRUE", Resolver_TRUE);
            RegisterQueryType("FALSE", Resolver_FALSE);
            RegisterQueryType("ANY", Resolver_ANY);

            // Thời gian, Ngày chơi, Chu kỳ & Thời tiết
            RegisterQueryType("TIME", Resolver_TIME);
            RegisterQueryType("DAY_PLAYED", Resolver_DAY_PLAYED);
            RegisterQueryType("CYCLE_NUMBER", Resolver_CYCLE_NUMBER);
            RegisterQueryType("WEATHER", Resolver_WEATHER);

            // Nhân vật chính & Kinh tế & Trang trại
            RegisterQueryType("PROTAGONIST", Resolver_PROTAGONIST);
            RegisterQueryType("PLAYER_CURRENT_MONEY", Resolver_PLAYER_CURRENT_MONEY);
            RegisterQueryType("PLAYER_MONEY_EARNED", Resolver_PLAYER_MONEY_EARNED);
            RegisterQueryType("PLAYER_FARM_LEVEL", Resolver_PLAYER_FARM_LEVEL);
            RegisterQueryType("PLAYER_STAMINA", Resolver_PLAYER_STAMINA);
            RegisterQueryType("PLAYER_LOCATION", Resolver_PLAYER_LOCATION);

            // Kho đồ, Thư tín, Sự kiện, Trigger & Nhiệm vụ
            RegisterQueryType("PLAYER_HAS_ITEM", Resolver_PLAYER_HAS_ITEM);
            RegisterQueryType("PLAYER_HAS_ITEM_TAG", Resolver_PLAYER_HAS_ITEM_TAG);
            RegisterQueryType("PLAYER_HAS_MAIL", Resolver_PLAYER_HAS_MAIL);
            RegisterQueryType("PLAYER_HAS_SEEN_EVENT", Resolver_PLAYER_HAS_SEEN_EVENT);
            RegisterQueryType("PLAYER_HAS_RUN_TRIGGER_ACTION", Resolver_PLAYER_HAS_RUN_TRIGGER_ACTION);
            RegisterQueryType("PLAYER_HAS_QUEST", Resolver_PLAYER_HAS_QUEST);
            RegisterQueryType("PLAYER_QUEST_COMPLETED", Resolver_PLAYER_QUEST_COMPLETED);
            RegisterQueryType("PLAYER_CONVERSATION_TOPIC", Resolver_PLAYER_CONVERSATION_TOPIC);
            RegisterQueryType("PLAYER_NPC_TRUST", Resolver_PLAYER_NPC_TRUST);

            // Công trình, Lưới điện & Phòng thủ
            RegisterQueryType("BUILDINGS_CONSTRUCTED", Resolver_BUILDINGS_CONSTRUCTED);
            RegisterQueryType("BUILDING_ACTIVE_POWERED", Resolver_BUILDING_ACTIVE_POWERED);
            RegisterQueryType("POWER_GRID_RATIO", Resolver_POWER_GRID_RATIO);
            RegisterQueryType("BREAKER_TRIPPED", Resolver_BREAKER_TRIPPED);

            // Đàn heo, Gen, Sinh cảnh & Áp lực sự kiện
            RegisterQueryType("HERD_COUNT", Resolver_HERD_COUNT);
            RegisterQueryType("HERD_TIER", Resolver_HERD_TIER);
            RegisterQueryType("HERD_ELDER_COUNT", Resolver_HERD_ELDER_COUNT);
            RegisterQueryType("HERD_HAS_WHITE_FOOT_PIG", Resolver_HERD_HAS_WHITE_FOOT_PIG);
            RegisterQueryType("HERD_RARE_GENES_IDENTIFIED", Resolver_HERD_RARE_GENES_IDENTIFIED);
            RegisterQueryType("HABITAT_INDEX", Resolver_HABITAT_INDEX);
            RegisterQueryType("EVENT_PRESSURE", Resolver_EVENT_PRESSURE);

            // Tiến trình Cổng Giữa Game, Khóa Đặc Tính, Tình báo & Cốt truyện
            RegisterQueryType("INTEL_TIER", Resolver_INTEL_TIER);
            RegisterQueryType("MID_GAME_GATE_OPEN", Resolver_MID_GAME_GATE_OPEN);
            RegisterQueryType("SPECIALTY_LOCKS_UNLOCKED", Resolver_SPECIALTY_LOCKS_UNLOCKED);
            RegisterQueryType("CYCLE_BOSSES_DEFEATED", Resolver_CYCLE_BOSSES_DEFEATED);
            RegisterQueryType("MAJOR_WAVES_SURVIVED", Resolver_MAJOR_WAVES_SURVIVED);
            RegisterQueryType("SEVENTH_CHANNEL_CUT", Resolver_SEVENTH_CHANNEL_CUT);
            RegisterQueryType("HERD_EMPTY_SPOT_OBSERVED", Resolver_HERD_EMPTY_SPOT_OBSERVED);

            // Kiểm tra vật phẩm ngữ cảnh (Target / Input) & Ngẫu nhiên tất định
            RegisterQueryType("ITEM_CONTEXT_TAG", Resolver_ITEM_CONTEXT_TAG);
            RegisterQueryType("ITEM_ID", Resolver_ITEM_ID);
            RegisterQueryType("RANDOM", Resolver_RANDOM);
            RegisterQueryType("SYNCED_RANDOM", Resolver_SYNCED_RANDOM);
        }

        private static bool Resolver_TRUE(string[] query, GameStateQueryContext ctx) => true;
        private static bool Resolver_FALSE(string[] query, GameStateQueryContext ctx) => false;

        private static bool Resolver_ANY(string[] query, GameStateQueryContext ctx)
        {
            for (int i = 1; i < query.Length; i++)
            {
                if (CheckConditions(query[i], ctx))
                    return true;
            }
            return false;
        }

        private static bool Resolver_TIME(string[] query, GameStateQueryContext ctx)
        {
            int minH = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 0;
            int maxH = query.Length > 2 && int.TryParse(query[2], out int b) ? b : 23;
            int curH = ctx.Engine.SimulatedHourOverride ?? ctx.Engine.Clock.CurrentHour;
            return curH >= minH && curH <= maxH;
        }

        private static bool Resolver_DAY_PLAYED(string[] query, GameStateQueryContext ctx)
        {
            int minDay = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 1;
            int maxDay = query.Length > 2 && int.TryParse(query[2], out int b) ? b : int.MaxValue;
            int curDay = ctx.Engine.Clock.CurrentDay;
            return curDay >= minDay && curDay <= maxDay;
        }

        private static bool Resolver_CYCLE_NUMBER(string[] query, GameStateQueryContext ctx)
        {
            int minCycle = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 1;
            int maxCycle = query.Length > 2 && int.TryParse(query[2], out int b) ? b : int.MaxValue;
            int curCycle = ctx.Engine.Clock.CycleNumber;
            return curCycle >= minCycle && curCycle <= maxCycle;
        }

        private static bool Resolver_WEATHER(string[] query, GameStateQueryContext ctx)
        {
            string current = ctx.Engine.CurrentWeather.ToString();
            for (int i = 1; i < query.Length; i++)
            {
                if (current.Equals(query[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool Resolver_PROTAGONIST(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            string active = ctx.Engine.Story.ActiveProtagonist.ToString();
            return active.Equals(query[1], StringComparison.OrdinalIgnoreCase);
        }

        private static bool Resolver_PLAYER_CURRENT_MONEY(string[] query, GameStateQueryContext ctx)
        {
            int minGold = query.Length > 1 && int.TryParse(query[1], out int g) ? g : 0;
            return ctx.Engine.Farm.Gold >= minGold;
        }

        private static bool Resolver_PLAYER_MONEY_EARNED(string[] query, GameStateQueryContext ctx)
        {
            int minEarned = query.Length > 1 && int.TryParse(query[1], out int g) ? g : 0;
            return ctx.Engine.TotalGoldEarned >= minEarned;
        }

        private static bool Resolver_PLAYER_FARM_LEVEL(string[] query, GameStateQueryContext ctx)
        {
            int minLevel = query.Length > 1 && int.TryParse(query[1], out int lv) ? lv : 1;
            int maxLevel = query.Length > 2 && int.TryParse(query[2], out int maxLv) ? maxLv : int.MaxValue;
            return ctx.Engine.Farm.FarmLevel >= minLevel && ctx.Engine.Farm.FarmLevel <= maxLevel;
        }

        private static bool Resolver_PLAYER_STAMINA(string[] query, GameStateQueryContext ctx)
        {
            float minSta = query.Length > 1 && float.TryParse(query[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float s) ? s : 0f;
            return ctx.Engine.Character.Stamina.CurrentStamina >= minSta;
        }

        private static bool Resolver_PLAYER_LOCATION(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            string locId = ctx.Location?.LocationId ?? ctx.Engine.WorldLocations.CurrentLocationId;
            return locId.Equals(query[1], StringComparison.OrdinalIgnoreCase);
        }

        private static bool Resolver_PLAYER_HAS_ITEM(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            int minCount = query.Length > 2 && int.TryParse(query[2], out int c) ? Math.Max(1, c) : 1;
            return ctx.Engine.Inventory.ContainsId(query[1], minCount);
        }

        private static bool Resolver_PLAYER_HAS_ITEM_TAG(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            int minCount = query.Length > 2 && int.TryParse(query[2], out int c) ? Math.Max(1, c) : 1;
            return ctx.Engine.Inventory.CountByContextTag(query[1]) >= minCount;
        }

        private static bool Resolver_PLAYER_HAS_MAIL(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            return ctx.Engine.Mailbox.HasReceivedOrQueuedMail(query[1]);
        }

        private static bool Resolver_PLAYER_HAS_SEEN_EVENT(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            return ctx.Engine.TriggerManager.EventsSeen.Contains(query[1]);
        }

        private static bool Resolver_PLAYER_HAS_RUN_TRIGGER_ACTION(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            return ctx.Engine.TriggerManager.TriggerActionsRun.Contains(query[1]);
        }

        private static bool Resolver_PLAYER_HAS_QUEST(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            var quest = ctx.Engine.Story.Quests.Find(q => q.QuestId.Equals(query[1], StringComparison.OrdinalIgnoreCase));
            return quest != null && (quest.IsUnlocked || quest.IsCompleted);
        }

        private static bool Resolver_PLAYER_QUEST_COMPLETED(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            var quest = ctx.Engine.Story.Quests.Find(q => q.QuestId.Equals(query[1], StringComparison.OrdinalIgnoreCase));
            return quest != null && quest.IsCompleted;
        }

        private static bool Resolver_PLAYER_CONVERSATION_TOPIC(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            return ctx.Engine.ConversationTopics.IsTopicActive(query[1]);
        }

        private static bool Resolver_PLAYER_NPC_TRUST(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 3)
                return false;
            string npcId = query[1];
            int minTrust = int.TryParse(query[2], out int t) ? t : 1;
            return ctx.Engine.Story.NpcTrustTier.TryGetValue(npcId, out int cur) && cur >= minTrust;
        }

        private static bool Resolver_BUILDINGS_CONSTRUCTED(string[] query, GameStateQueryContext ctx)
        {
            string targetBlueprint = query.Length > 1 ? query[1] : "ALL";
            int minCount = query.Length > 2 && int.TryParse(query[2], out int c) ? c : 1;

            int count = 0;
            var defBuildings = ctx.Engine.Defense.Buildings;
            for (int i = 0; i < defBuildings.Count; i++)
            {
                var b = defBuildings[i];
                if (b == null || b.IsBroken || b.CurrentHp <= 0)
                    continue;
                if (targetBlueprint.Equals("ALL", StringComparison.OrdinalIgnoreCase) ||
                    b.BlueprintId.Equals(targetBlueprint, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            var farmStructures = ctx.Engine.GridZones.Structures;
            for (int i = 0; i < farmStructures.Count; i++)
            {
                var s = farmStructures[i];
                if (s == null)
                    continue;
                if (targetBlueprint.Equals("ALL", StringComparison.OrdinalIgnoreCase) ||
                    s.StructureType.Equals(targetBlueprint, StringComparison.OrdinalIgnoreCase) ||
                    s.InstanceId.Equals(targetBlueprint, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count >= minCount;
        }

        private static bool Resolver_BUILDING_ACTIVE_POWERED(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            return ctx.Engine.Defense.HasPoweredBuilding(query[1]);
        }

        private static bool Resolver_POWER_GRID_RATIO(string[] query, GameStateQueryContext ctx)
        {
            float minR = query.Length > 1 && float.TryParse(query[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float a) ? a : 0f;
            float maxR = query.Length > 2 && float.TryParse(query[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) ? b : 99f;
            float cap = Math.Max(1f, ctx.Engine.DeepMechanics.PowerGrid.GetContinuousCapacity());
            float ratio = ctx.Engine.DeepMechanics.PowerGrid.CurrentDemandUnits / cap;
            return ratio >= minR && ratio <= maxR;
        }

        private static bool Resolver_BREAKER_TRIPPED(string[] query, GameStateQueryContext ctx)
        {
            return ctx.Engine.DeepMechanics.PowerGrid.IsBreakerTripped;
        }

        private static bool Resolver_HERD_COUNT(string[] query, GameStateQueryContext ctx)
        {
            int minCount = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 1;
            int maxCount = query.Length > 2 && int.TryParse(query[2], out int b) ? b : int.MaxValue;
            int alive = ctx.Engine.Pigs.FindAll(p => p.IsAlive).Count;
            return alive >= minCount && alive <= maxCount;
        }

        private static bool Resolver_HERD_TIER(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2)
                return false;
            if (Enum.TryParse<HerdStateTier>(query[1], true, out var targetTier))
                return ctx.Engine.Herd.Tier >= targetTier;
            if (int.TryParse(query[1], out int minTierInt))
                return (int)ctx.Engine.Herd.Tier >= minTierInt;
            return false;
        }

        private static bool Resolver_HERD_ELDER_COUNT(string[] query, GameStateQueryContext ctx)
        {
            int minElders = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 1;
            int elders = ctx.Engine.Pigs.FindAll(p => p.IsAlive && p.Stage == PigStage.HeoGia).Count;
            return elders >= minElders;
        }

        private static bool Resolver_HERD_HAS_WHITE_FOOT_PIG(string[] query, GameStateQueryContext ctx)
        {
            return ctx.Engine.Pigs.Exists(p =>
                p.IsAlive &&
                (p.Id.Equals("pig_white_foot_2018", StringComparison.OrdinalIgnoreCase) ||
                 p.Name.IndexOf("Chân Trắng", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static bool Resolver_HERD_RARE_GENES_IDENTIFIED(string[] query, GameStateQueryContext ctx)
        {
            int minCount = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 1;
            int identifiedRare = ctx.Engine.Pigs.FindAll(p => p.IsIdentified && p.Rarity >= GeneRarity.Hiem).Count;
            return Math.Max(identifiedRare, ctx.Engine.DeepMechanics.RareOrHigherGenesIdentifiedCount) >= minCount;
        }

        private static bool Resolver_HABITAT_INDEX(string[] query, GameStateQueryContext ctx)
        {
            int minVal = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 0;
            int maxVal = query.Length > 2 && int.TryParse(query[2], out int b) ? b : 100;
            int sc = ctx.Engine.Farm.HabitatIndex;
            return sc >= minVal && sc <= maxVal;
        }

        private static bool Resolver_EVENT_PRESSURE(string[] query, GameStateQueryContext ctx)
        {
            int minVal = query.Length > 1 && int.TryParse(query[1], out int a) ? a : 0;
            int maxVal = query.Length > 2 && int.TryParse(query[2], out int b) ? b : 100;
            int alsk = ctx.Engine.Farm.EventPressureIndex;
            return alsk >= minVal && alsk <= maxVal;
        }

        private static bool Resolver_INTEL_TIER(string[] query, GameStateQueryContext ctx)
        {
            int minTier = query.Length > 1 && int.TryParse(query[1], out int t) ? t : 1;
            return ctx.Engine.DeepMechanics.CurrentIntelTier >= minTier;
        }

        private static bool Resolver_MID_GAME_GATE_OPEN(string[] query, GameStateQueryContext ctx)
        {
            return ctx.Engine.DeepMechanics.MidGameGateUnlocked;
        }

        private static bool Resolver_SPECIALTY_LOCKS_UNLOCKED(string[] query, GameStateQueryContext ctx)
        {
            int minLocks = query.Length > 1 && int.TryParse(query[1], out int l) ? l : 1;
            return ctx.Engine.DeepMechanics.UnlockedSpecialtyLocksCount >= minLocks;
        }

        private static bool Resolver_CYCLE_BOSSES_DEFEATED(string[] query, GameStateQueryContext ctx)
        {
            int minBosses = query.Length > 1 && int.TryParse(query[1], out int b) ? b : 1;
            return ctx.Engine.DeepMechanics.CycleBossesDefeatedCount >= minBosses;
        }

        private static bool Resolver_MAJOR_WAVES_SURVIVED(string[] query, GameStateQueryContext ctx)
        {
            int minWaves = query.Length > 1 && int.TryParse(query[1], out int w) ? w : 1;
            return ctx.Engine.MajorWavesSurvived >= minWaves;
        }

        private static bool Resolver_SEVENTH_CHANNEL_CUT(string[] query, GameStateQueryContext ctx)
        {
            return ctx.Engine.Story.SeventhChannelCut;
        }

        private static bool Resolver_HERD_EMPTY_SPOT_OBSERVED(string[] query, GameStateQueryContext ctx)
        {
            return ctx.Engine.Story.HerdEmptySpotObserved;
        }

        private static bool Resolver_ITEM_CONTEXT_TAG(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 3)
                return false;

            var item = query[1].Equals("Input", StringComparison.OrdinalIgnoreCase)
                ? ctx.InputItem
                : ctx.TargetItem;

            if (item == null)
                return false;

            for (int i = 2; i < query.Length; i++)
            {
                if (!ItemContextTagManager.HasBaseTag(item.QualifiedItemId, query[i]))
                    return false;
            }
            return true;
        }

        private static bool Resolver_ITEM_ID(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 3)
                return false;

            var item = query[1].Equals("Input", StringComparison.OrdinalIgnoreCase)
                ? ctx.InputItem
                : ctx.TargetItem;

            if (item == null)
                return false;

            string expectedQid = QualifiedItemRegistry.QualifyItemId(query[2]);
            return item.QualifiedItemId.Equals(expectedQid, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Resolver_RANDOM(string[] query, GameStateQueryContext ctx)
        {
            if (query.Length < 2 || !double.TryParse(query[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double chance))
                return false;
            return ctx.Random.NextDouble() < chance;
        }

        private static bool Resolver_SYNCED_RANDOM(string[] query, GameStateQueryContext ctx)
        {
            // Định dạng: SYNCED_RANDOM <day|hour|cycle> <key> <chance>
            if (query.Length < 4 || !double.TryParse(query[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double chance))
                return false;

            string interval = query[1].ToLowerInvariant();
            int timeSlot = interval switch
            {
                "hour" => ctx.Engine.Clock.CurrentDay * 24 + ctx.Engine.Clock.CurrentHour,
                "cycle" => ctx.Engine.Clock.CycleNumber,
                _ => ctx.Engine.Clock.CurrentDay
            };

            int keyHash = DeterministicHashUtility.GetDeterministicHashCode(query[2]);
            var syncedRng = DeterministicHashUtility.CreateDaySaveRandom(ctx.Engine.BaseSeed, timeSlot, keyHash);
            return syncedRng.NextDouble() < chance;
        }
    }

    #endregion

    #region 4. TriggerActionManager, TriggerActionData & Action Registry (StardewValley.Triggers.TriggerActionManager)

    /// <summary>
    /// Dữ liệu một quy tắc kích hoạt hành động hướng dữ liệu, tương đương <c>StardewValley.GameData.TriggerActionData</c>.
    /// </summary>
    [Serializable]
    public sealed class TriggerActionData
    {
        public string Id { get; set; } = string.Empty;
        public string Trigger { get; set; } = "DayStarted";
        public string Condition { get; set; } = "TRUE";
        public string SkipPermanentlyCondition { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new List<string>();
        public bool MarkActionApplied { get; set; } = true;
    }

    public delegate bool TriggerActionDelegate(string[] args, GameStateQueryContext context, out string? error);

    /// <summary>
    /// Bộ quản lý Trigger &amp; Hậu quả dây chuyền hướng dữ liệu, tương đương <c>StardewValley.Triggers.TriggerActionManager</c>.
    /// Mọi biến cố trong vòng đời game (<c>DayStarted</c>, <c>DayEnding</c>, <c>HourlyTick</c>, <c>PigSold</c>, <c>PigBorn</c>,
    /// <c>PigDied</c>, <c>BuildingConstructed</c>, <c>WaveEnded</c>, <c>LocationChanged</c>, <c>ItemPlacedInMachine</c>,
    /// <c>ReflectiveSurfaceChecked</c>, <c>Manual</c>) đều đi qua bộ máy này để đánh giá <see cref="GameStateQuery"/>
    /// và thực thi chuỗi hành động thật trên <see cref="GameEngine"/>.
    /// </summary>
    [Serializable]
    public sealed class TriggerActionManager
    {
        public const string TriggerDayStarted = "DayStarted";
        public const string TriggerDayEnding = "DayEnding";
        public const string TriggerHourlyTick = "HourlyTick";
        public const string TriggerLocationChanged = "LocationChanged";
        public const string TriggerPigSold = "PigSold";
        public const string TriggerPigBorn = "PigBorn";
        public const string TriggerPigDied = "PigDied";
        public const string TriggerBuildingConstructed = "BuildingConstructed";
        public const string TriggerWaveEnded = "WaveEnded";
        public const string TriggerItemPlacedInMachine = "ItemPlacedInMachine";
        public const string TriggerReflectiveSurfaceChecked = "ReflectiveSurfaceChecked";
        public const string TriggerManual = "Manual";

        private static readonly Dictionary<string, TriggerActionDelegate> ActionHandlers =
            new Dictionary<string, TriggerActionDelegate>(StringComparer.OrdinalIgnoreCase);

        private readonly List<TriggerActionData> registeredTriggers = new List<TriggerActionData>();

        public HashSet<string> TriggerActionsRun { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EventsSeen { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public int TotalActionsExecuted { get; private set; }

        static TriggerActionManager()
        {
            RegisterDefaultActions();
        }

        public TriggerActionManager()
        {
            RegisterCanonicalPigTycoonTriggerRules();
        }

        public IReadOnlyList<TriggerActionData> RegisteredTriggers => registeredTriggers;

        public static void RegisterAction(string actionName, TriggerActionDelegate handler)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                throw new ArgumentException("Tên Action không được rỗng.", nameof(actionName));
            ActionHandlers[actionName.Trim()] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void RegisterTriggerRule(TriggerActionData rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Id))
                return;

            registeredTriggers.RemoveAll(r => r.Id.Equals(rule.Id, StringComparison.OrdinalIgnoreCase));
            registeredTriggers.Add(rule);
        }

        /// <summary>
        /// Phát một sự kiện Trigger tương đương <c>TriggerActionManager.Raise(...)</c> của Stardew Valley 1.6.
        /// Trả về danh sách các <c>TriggerActionData.Id</c> đã thỏa điều kiện và thực thi thành công.
        /// </summary>
        public List<string> Raise(
            string triggerName,
            GameEngine engine,
            GameLocation? location = null,
            UnifiedItemInstance? targetItem = null,
            UnifiedItemInstance? inputItem = null)
        {
            var firedIds = new List<string>();
            if (string.IsNullOrWhiteSpace(triggerName) || engine == null)
                return firedIds;

            var context = new GameStateQueryContext(engine, location, targetItem, inputItem);

            for (int i = 0; i < registeredTriggers.Count; i++)
            {
                var rule = registeredTriggers[i];
                if (!HasMatchingTriggerToken(rule.Trigger, triggerName))
                    continue;

                if (rule.MarkActionApplied && TriggerActionsRun.Contains(rule.Id))
                    continue;

                if (!string.IsNullOrWhiteSpace(rule.SkipPermanentlyCondition) &&
                    GameStateQuery.CheckConditions(rule.SkipPermanentlyCondition, context))
                {
                    TriggerActionsRun.Add(rule.Id);
                    continue;
                }

                if (!GameStateQuery.CheckConditions(rule.Condition, context))
                    continue;

                bool allSucceeded = true;
                if (!string.IsNullOrWhiteSpace(rule.Action))
                {
                    if (!TryRunAction(rule.Action, context, out _))
                        allSucceeded = false;
                }

                for (int a = 0; a < rule.Actions.Count; a++)
                {
                    if (!string.IsNullOrWhiteSpace(rule.Actions[a]))
                    {
                        if (!TryRunAction(rule.Actions[a], context, out _))
                            allSucceeded = false;
                    }
                }

                if (allSucceeded)
                {
                    if (rule.MarkActionApplied)
                        TriggerActionsRun.Add(rule.Id);

                    firedIds.Add(rule.Id);
                }
            }

            return firedIds;
        }

        public bool TryRunAction(string actionCommand, GameStateQueryContext context, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(actionCommand))
                return true;

            string[] args = actionCommand.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0)
                return true;

            if (!ActionHandlers.TryGetValue(args[0], out var handler))
            {
                error = $"Action '{args[0]}' chưa được đăng ký trong TriggerActionManager.";
                return false;
            }

            bool ok = handler(args, context, out error);
            if (ok)
                TotalActionsExecuted++;
            return ok;
        }

        private static bool HasMatchingTriggerToken(string ruleTriggers, string raisedTrigger)
        {
            if (string.IsNullOrWhiteSpace(ruleTriggers))
                return false;

            string[] parts = ruleTriggers.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Equals(raisedTrigger, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void RegisterDefaultActions()
        {
            RegisterAction("AddMoney", Action_AddMoney);
            RegisterAction("AddItem", Action_AddItem);
            RegisterAction("RemoveItem", Action_RemoveItem);
            RegisterAction("AddMail", Action_AddMail);
            RegisterAction("StartQuest", Action_StartQuest);
            RegisterAction("CompleteQuest", Action_CompleteQuest);
            RegisterAction("MarkEventSeen", Action_MarkEventSeen);
            RegisterAction("AddConversationTopic", Action_AddConversationTopic);
            RegisterAction("AdjustNpcTrust", Action_AdjustNpcTrust);
            RegisterAction("AdjustEventPressure", Action_AdjustEventPressure);
            RegisterAction("AdjustHabitat", Action_AdjustHabitat);
            RegisterAction("AddThreatBudget", Action_AddThreatBudget);
            RegisterAction("UnlockBlueprint", Action_UnlockBlueprint);
            RegisterAction("SetStoryFlag", Action_SetStoryFlag);
            RegisterAction("EvaluateMidGameGate", Action_EvaluateMidGameGate);
            RegisterAction("Null", (string[] _, GameStateQueryContext __, out string? err) => { err = null; return true; });
        }

        private static bool Action_AddMoney(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2 || !int.TryParse(args[1], out int amount))
            {
                error = "AddMoney yêu cầu số tiền nguyên.";
                return false;
            }

            if (amount >= 0)
            {
                ctx.Engine.Ledger.Earn(amount, LedgerReason.WaveReward, "TriggerAction_AddMoney");
                ctx.Engine.TotalGoldEarned += amount;
            }
            else
            {
                ctx.Engine.Farm.SpendGold(Math.Min(ctx.Engine.Farm.Gold, -amount));
            }
            return true;
        }

        private static bool Action_AddItem(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
            {
                error = "AddItem yêu cầu mã vật phẩm.";
                return false;
            }

            int count = args.Length > 2 && int.TryParse(args[2], out int c) ? Math.Max(1, c) : 1;
            int quality = args.Length > 3 && int.TryParse(args[3], out int q) ? Math.Clamp(q, 0, 3) : 0;
            ctx.Engine.Inventory.AddItem(args[1], count, quality);
            return true;
        }

        private static bool Action_RemoveItem(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
            {
                error = "RemoveItem yêu cầu mã vật phẩm.";
                return false;
            }

            int count = args.Length > 2 && int.TryParse(args[2], out int c) ? Math.Max(1, c) : 1;
            return ctx.Engine.Inventory.ConsumeItem(args[1], count);
        }

        private static bool Action_AddMail(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
            {
                error = "AddMail yêu cầu mã thư.";
                return false;
            }

            bool tomorrow = args.Length > 2 && args[2].Equals("tomorrow", StringComparison.OrdinalIgnoreCase);
            if (tomorrow)
                ctx.Engine.Mailbox.EnqueueMailForTomorrow(args[1]);
            else
                ctx.Engine.Mailbox.DeliverMailNow(args[1]);
            return true;
        }

        private static bool Action_StartQuest(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
            {
                error = "StartQuest yêu cầu mã nhiệm vụ.";
                return false;
            }

            string qId = args[1];
            var existing = ctx.Engine.Story.Quests.Find(q => q.QuestId.Equals(qId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.IsUnlocked = true;
            }
            else
            {
                ctx.Engine.Story.Quests.Add(new StoryQuestRecord
                {
                    QuestId = qId,
                    Title = $"Nhiệm vụ {qId}",
                    Chapter = ctx.Engine.Story.CurrentChapter,
                    IsUnlocked = true,
                    IsCompleted = false
                });
            }
            return true;
        }

        private static bool Action_CompleteQuest(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
            {
                error = "CompleteQuest yêu cầu mã nhiệm vụ.";
                return false;
            }

            ctx.Engine.Story.CompleteQuestStep(args[1]);
            ctx.Engine.PlayerStats.Increment("QuestsCompleted", 1);
            return true;
        }

        private static bool Action_MarkEventSeen(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
            {
                error = "MarkEventSeen yêu cầu mã sự kiện.";
                return false;
            }

            ctx.Engine.TriggerManager.EventsSeen.Add(args[1]);
            return true;
        }

        private static bool Action_AddConversationTopic(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
            {
                error = "AddConversationTopic yêu cầu mã chủ đề.";
                return false;
            }

            int days = args.Length > 2 && int.TryParse(args[2], out int d) ? Math.Max(1, d) : 3;
            ctx.Engine.ConversationTopics.StartTopic(args[1], days);
            return true;
        }

        private static bool Action_AdjustNpcTrust(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 3 || !int.TryParse(args[2], out int delta))
            {
                error = "AdjustNpcTrust yêu cầu <npcId> <delta>.";
                return false;
            }

            string npcId = args[1];
            int current = ctx.Engine.Story.NpcTrustTier.TryGetValue(npcId, out int cur) ? cur : 1;
            ctx.Engine.Story.NpcTrustTier[npcId] = Math.Clamp(current + delta, 0, 5);
            return true;
        }

        private static bool Action_AdjustEventPressure(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2 || !int.TryParse(args[1], out int delta))
                return false;
            ctx.Engine.Farm.EventPressureIndex = Math.Clamp(ctx.Engine.Farm.EventPressureIndex + delta, 0, 100);
            return true;
        }

        private static bool Action_AdjustHabitat(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2 || !int.TryParse(args[1], out int delta))
                return false;
            ctx.Engine.Farm.HabitatIndex = Math.Clamp(ctx.Engine.Farm.HabitatIndex + delta, 0, 100);
            return true;
        }

        private static bool Action_AddThreatBudget(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2 || !int.TryParse(args[1], out int delta))
                return false;
            ctx.Engine.Defense.PendingThreatBudgetBonus = Math.Max(0, ctx.Engine.Defense.PendingThreatBudgetBonus + delta);
            return true;
        }

        private static bool Action_UnlockBlueprint(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 2)
                return false;
            if (!ctx.Engine.Defense.UnlockedBlueprints.Contains(args[1]))
                ctx.Engine.Defense.UnlockedBlueprints.Add(args[1]);
            return true;
        }

        private static bool Action_SetStoryFlag(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            if (args.Length < 3)
                return false;

            bool val = args[2].Equals("true", StringComparison.OrdinalIgnoreCase) || args[2] == "1";
            if (args[1].Equals("SeventhChannelCut", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Engine.Story.SeventhChannelCut = val;
            }
            else if (args[1].Equals("HerdEmptySpotObserved", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Engine.Story.HerdEmptySpotObserved = val;
            }
            return true;
        }

        private static bool Action_EvaluateMidGameGate(string[] args, GameStateQueryContext ctx, out string? error)
        {
            error = null;
            ctx.Engine.DeepMechanics.EvaluateMidGameGateAndLocks(
                ctx.Engine.Farm,
                ctx.Engine.Herd,
                ctx.Engine.MajorWavesSurvived);
            return true;
        }

        private void RegisterCanonicalPigTycoonTriggerRules()
        {
            // 1. Gói khởi đầu Ngày 1: Thư chào mừng của Bà Sáu & Mở khóa nhiệm vụ đầu game
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_Day1_WelcomePackage",
                Trigger = TriggerDayStarted,
                Condition = "DAY_PLAYED 1 1",
                Actions = new List<string>
                {
                    "AddMail Mail_BaSau_Day1 now",
                    "StartQuest Q01",
                    "AddItem (F)CamThuong 3",
                    "AddItem (M)ThaoDuocKho 2"
                },
                MarkActionApplied = true
            });

            // 2. Q03 / S01: Đạt Bầy Đàn Sơ Khai -> Phát hiện khoảng trống 1,2m ở góc gốc mít (HerdEmptySpotObserved)
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_Q11_HerdEmptySpotAtJackfruit",
                Trigger = $"{TriggerDayStarted} {TriggerHourlyTick}",
                Condition = "HERD_TIER SoKhai, !HERD_EMPTY_SPOT_OBSERVED",
                Actions = new List<string>
                {
                    "CompleteQuest Q11",
                    "SetStoryFlag HerdEmptySpotObserved true",
                    "AddConversationTopic Topic_EmptySpot_12m 4",
                    "AddMail Mail_TamVa_FenceNotice tomorrow"
                },
                MarkActionApplied = true
            });

            // 3. Sự kiện 02:00 sáng "Sáu Với Một": khi chưa cắt kênh thứ 7, kích hoạt chủ đề hội thoại và tăng nhẹ ÁLSK
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_02AM_SixWithOne_Phenomenon",
                Trigger = TriggerHourlyTick,
                Condition = "TIME 2 2, !SEVENTH_CHANNEL_CUT",
                Actions = new List<string>
                {
                    "AddConversationTopic Topic_02AM_Footsteps 1",
                    "AdjustEventPressure 2",
                    "MarkEventSeen Event_02AM_Observed"
                },
                MarkActionApplied = false
            });

            // 4. GDD 10.2: Tự động đánh giá và mở Cổng Giữa Game kèm thư thông báo và phần thưởng khi đủ điều kiện
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_GDD102_MidGameGate_Evaluation",
                Trigger = $"{TriggerDayStarted} {TriggerPigSold} {TriggerWaveEnded} {TriggerBuildingConstructed}",
                Condition = "PLAYER_FARM_LEVEL 7, HERD_COUNT 8, !MID_GAME_GATE_OPEN",
                Actions = new List<string>
                {
                    "EvaluateMidGameGate"
                },
                MarkActionApplied = false
            });

            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_GDD102_MidGameGate_OpenedReward",
                Trigger = $"{TriggerDayStarted} {TriggerPigSold} {TriggerWaveEnded}",
                Condition = "MID_GAME_GATE_OPEN",
                Actions = new List<string>
                {
                    "AddMail Mail_MidGameGate_Unlocked now",
                    "AddItem (G)Paper_Yellow 1",
                    "AddConversationTopic Topic_MidGame_Expansion 5",
                    "CompleteQuest Q08"
                },
                MarkActionApplied = true
            });

            // 5. Bán heo đầu tiên ở Chợ Ngày -> Mở chủ đề hội thoại Ông Tồn đếm "sáu với một" & thưởng nhiệm vụ Q02
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_FirstPigSold_EdgeTownReaction",
                Trigger = TriggerPigSold,
                Condition = "PLAYER_MONEY_EARNED 100",
                Actions = new List<string>
                {
                    "CompleteQuest Q02",
                    "AddConversationTopic Topic_OngTon_SixWithOne 3",
                    "AdjustNpcTrust OngTon 1"
                },
                MarkActionApplied = true
            });

            // 6. Sinh heo con thuộc phẩm chất Hiếm trở lên -> Thư chúc mừng từ Lệ & Mẫu Gen Thô
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_RarePigletBorn_GeneSampleGift",
                Trigger = TriggerPigBorn,
                Condition = "HERD_COUNT 2",
                Actions = new List<string>
                {
                    "AddItem (G)MauGenTho 1",
                    "AddConversationTopic Topic_NewPigletLitter 2"
                },
                MarkActionApplied = true
            });

            // 7. Sống sót qua Đợt Quái Lớn đầu tiên -> Hoàn thành Q05, mở bản vẽ tháp phòng thủ & tăng tín nhiệm Tám Vá
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_FirstMajorWaveSurvived_DefenseMilestone",
                Trigger = TriggerWaveEnded,
                Condition = "MAJOR_WAVES_SURVIVED 1",
                Actions = new List<string>
                {
                    "CompleteQuest Q05",
                    "AdjustNpcTrust TamVa 1",
                    "AddItem (O)TinhThach 2",
                    "AddConversationTopic Topic_WaveSurvived_Rebuild 3"
                },
                MarkActionApplied = true
            });

            // 8. Soi bề mặt phản chiếu (Gương / Kính / Máng nước / Vòng cổ) -> Ghi nhận sự kiện tâm lý nhân vật
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_ReflectiveSurface_FirstCheck",
                Trigger = TriggerReflectiveSurfaceChecked,
                Condition = "TRUE",
                Actions = new List<string>
                {
                    "MarkEventSeen Event_ReflectiveSurface_Checked",
                    "AddConversationTopic Topic_Mirror_Asymmetry 2"
                },
                MarkActionApplied = true
            });

            // 9. Cắt kênh thứ 7 -> Đặt Bát Men Xanh Sứt Miệng ở cổng trại và hoàn tất Q20
            RegisterTriggerRule(new TriggerActionData
            {
                Id = "Canon_SeventhChannelCut_EpilogueBowl",
                Trigger = $"{TriggerDayStarted} {TriggerManual}",
                Condition = "SEVENTH_CHANNEL_CUT",
                Actions = new List<string>
                {
                    "AddItem (O)BatMenXanhSutMieng 1",
                    "CompleteQuest Q20",
                    "AddConversationTopic Topic_SeventhChannel_Peace 7"
                },
                MarkActionApplied = true
            });
        }
    }

    #endregion

    #region 5. GameLocation World Graph, AnimalHouse Interiors & MachineProcessor (Data/Locations, Buildings & Machines)

    /// <summary>
    /// Điểm dịch chuyển (Warp Portal) giữa hai <see cref="GameLocation"/>, tương đương <c>StardewValley.Warp</c>.
    /// </summary>
    [Serializable]
    public sealed class LocationWarpPortal
    {
        public int SourceTileX { get; set; }
        public int SourceTileY { get; set; }
        public string TargetLocationId { get; set; } = string.Empty;
        public int TargetTileX { get; set; }
        public int TargetTileY { get; set; }
        public string RequiredCondition { get; set; } = "TRUE";
    }

    /// <summary>
    /// Quy tắc chuyển đổi vật phẩm tự động hàng ngày của công trình (tương đương <c>BuildingItemConversion</c> trong <c>Data/Buildings.json</c>).
    /// </summary>
    [Serializable]
    public sealed class BuildingItemConversionRule
    {
        public string RuleId { get; set; } = string.Empty;
        public string RequiredItemId { get; set; } = string.Empty;
        public int RequiredCount { get; set; } = 1;
        public string ProducedItemId { get; set; } = string.Empty;
        public int ProducedCount { get; set; } = 1;
        public int MaxDailyConversions { get; set; } = 3;
    }

    /// <summary>
    /// Quy tắc đầu ra của máy chế biến, tương đương <c>MachineOutputRule</c> trong <c>Data/Machines.json</c> của Stardew Valley 1.6.
    /// </summary>
    [Serializable]
    public sealed class MachineOutputRule
    {
        public string RuleId { get; set; } = string.Empty;
        public string Trigger { get; set; } = "ItemPlacedInMachine"; // ItemPlacedInMachine | DayUpdate
        public string RequiredInputItemId { get; set; } = string.Empty;
        public string RequiredContextTag { get; set; } = string.Empty;
        public int RequiredInputCount { get; set; } = 1;
        public string? AdditionalConsumedItemId { get; set; }
        public int AdditionalConsumedCount { get; set; } = 0;
        public string Condition { get; set; } = "TRUE";
        public string OutputItemId { get; set; } = string.Empty;
        public int OutputCount { get; set; } = 1;
        public int MinutesUntilReady { get; set; } = 60;
    }

    /// <summary>
    /// Một máy chế biến đặt trong <see cref="GameLocation"/>, vận hành theo <see cref="MachineOutputRule"/>
    /// và giảm <see cref="MinutesUntilReady"/> mỗi nhịp 10 phút (<c>PerformTenMinuteUpdate</c>).
    /// </summary>
    [Serializable]
    public sealed class PlacedMachineProcessor
    {
        public string InstanceId { get; set; } = string.Empty;
        public string MachineQualifiedId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public List<MachineOutputRule> OutputRules { get; set; } = new List<MachineOutputRule>();

        public UnifiedItemInstance? HeldOutput { get; set; }
        public int MinutesUntilReady { get; set; }
        public bool IsReadyForHarvest => HeldOutput != null && MinutesUntilReady <= 0;

        public (bool accepted, string message) TryPlaceInput(
            string inputItemId,
            GameEngine engine,
            GameLocation location)
        {
            if (HeldOutput != null)
                return (false, $"Máy [{DisplayName}] đang bận hoặc có sản phẩm chờ thu hoạch.");

            var inputItem = QualifiedItemRegistry.Create(inputItemId, 1);
            var ctx = new GameStateQueryContext(engine, location, inputItem, inputItem);

            for (int i = 0; i < OutputRules.Count; i++)
            {
                var rule = OutputRules[i];
                if (!rule.Trigger.Equals("ItemPlacedInMachine", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(rule.RequiredInputItemId))
                {
                    string reqQid = QualifiedItemRegistry.QualifyItemId(rule.RequiredInputItemId);
                    if (!inputItem.QualifiedItemId.Equals(reqQid, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(rule.RequiredContextTag) &&
                    !ItemContextTagManager.HasBaseTag(inputItem.QualifiedItemId, rule.RequiredContextTag))
                {
                    continue;
                }

                if (!GameStateQuery.CheckConditions(rule.Condition, ctx))
                    continue;

                if (!engine.Inventory.ContainsId(inputItem.QualifiedItemId, rule.RequiredInputCount))
                    return (false, $"Không đủ {rule.RequiredInputCount}× {inputItem.DisplayName} trong kho.");

                if (!string.IsNullOrWhiteSpace(rule.AdditionalConsumedItemId) &&
                    rule.AdditionalConsumedCount > 0 &&
                    !engine.Inventory.ContainsId(rule.AdditionalConsumedItemId!, rule.AdditionalConsumedCount))
                {
                    return (false, $"Thiếu phụ liệu {rule.AdditionalConsumedCount}× {rule.AdditionalConsumedItemId}.");
                }

                engine.Inventory.ConsumeItem(inputItem.QualifiedItemId, rule.RequiredInputCount);
                if (!string.IsNullOrWhiteSpace(rule.AdditionalConsumedItemId) && rule.AdditionalConsumedCount > 0)
                {
                    engine.Inventory.ConsumeItem(rule.AdditionalConsumedItemId!, rule.AdditionalConsumedCount);
                }

                HeldOutput = QualifiedItemRegistry.Create(rule.OutputItemId, rule.OutputCount);
                MinutesUntilReady = Math.Max(10, rule.MinutesUntilReady);

                engine.TriggerManager.Raise(
                    TriggerActionManager.TriggerItemPlacedInMachine,
                    engine,
                    location,
                    HeldOutput,
                    inputItem);

                return (true, $"Đã nạp {rule.RequiredInputCount}× {inputItem.DisplayName} vào [{DisplayName}] (chờ {MinutesUntilReady} phút).");
            }

            return (false, $"Vật phẩm [{inputItem.DisplayName}] không phù hợp với [{DisplayName}].");
        }

        public bool PerformTenMinuteUpdate(int minutesElapsed = 10)
        {
            if (HeldOutput == null || MinutesUntilReady <= 0)
                return false;

            MinutesUntilReady = Math.Max(0, MinutesUntilReady - Math.Max(1, minutesElapsed));
            return MinutesUntilReady == 0;
        }

        public (bool collected, UnifiedItemInstance? item, string message) CollectOutput(GameEngine engine)
        {
            if (HeldOutput == null)
                return (false, null, $"Máy [{DisplayName}] đang trống.");
            if (MinutesUntilReady > 0)
                return (false, null, $"Máy [{DisplayName}] còn {MinutesUntilReady} phút nữa mới hoàn tất.");

            var output = HeldOutput;
            HeldOutput = null;
            MinutesUntilReady = 0;

            engine.Inventory.AddItem(output.QualifiedItemId, output.Stack, output.Quality);
            engine.PlayerStats.Increment("MachinesProcessed", 1);
            return (true, output, $"Đã thu hoạch {output.Stack}× {output.DisplayName} từ [{DisplayName}].");
        }
    }

    /// <summary>
    /// Một địa điểm trong đồ thị thế giới (<c>GameLocation</c>), lấy mẫu trực tiếp từ <c>StardewValley.GameLocation</c> &amp; <c>AnimalHouse</c>.
    /// Sở hữu danh sách cổng dịch chuyển (<see cref="Warps"/>), ô tương tác (<see cref="ActionTiles"/>),
    /// máy chế biến (<see cref="Machines"/>), quy tắc chuyển đổi công trình (<see cref="DailyConversions"/>),
    /// và nhận các lời gọi vòng đời thống nhất: <see cref="OnDayStarted"/>, <see cref="PerformTenMinuteUpdate"/>, <see cref="PerformTileAction"/>.
    /// </summary>
    [Serializable]
    public sealed class GameLocation
    {
        public string LocationId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsOutdoors { get; set; } = true;
        public bool IsAnimalHouse { get; set; }
        public int MaxAnimalOccupants { get; set; }
        public bool AllowAnimalPregnancy { get; set; } = true;

        public List<LocationWarpPortal> Warps { get; set; } = new List<LocationWarpPortal>();
        public Dictionary<string, string> ActionTiles { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<PlacedMachineProcessor> Machines { get; set; } = new List<PlacedMachineProcessor>();
        public List<BuildingItemConversionRule> DailyConversions { get; set; } = new List<BuildingItemConversionRule>();
        public List<string> ResidentAnimalIds { get; set; } = new List<string>();
        public Dictionary<string, string> ModData { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void OnDayStarted(GameEngine engine)
        {
            // 1. Nếu là Chuồng Chăn Nuôi (AnimalHouse), mỗi heo đang sống tạo ra Phân Heo Thô mỗi sáng
            if (IsAnimalHouse)
            {
                int livingHere = engine.Pigs.FindAll(p => p.IsAlive && (!p.IsIsolated || LocationId == "Barn_Quarantine")).Count;
                if (livingHere > 0 && LocationId == "Barn_Primary")
                {
                    int manureGenerated = Math.Min(MaxAnimalOccupants > 0 ? MaxAnimalOccupants : 12, livingHere);
                    engine.Inventory.AddItem("(O)PhanHeoTho", manureGenerated);
                }
            }

            // 2. Thực thi các quy tắc chuyển đổi tự động hàng ngày (BuildingItemConversions)
            for (int i = 0; i < DailyConversions.Count; i++)
            {
                var rule = DailyConversions[i];
                for (int c = 0; c < rule.MaxDailyConversions; c++)
                {
                    if (engine.Inventory.ContainsId(rule.RequiredItemId, rule.RequiredCount))
                    {
                        engine.Inventory.ConsumeItem(rule.RequiredItemId, rule.RequiredCount);
                        engine.Inventory.AddItem(rule.ProducedItemId, rule.ProducedCount);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public int PerformTenMinuteUpdate(int minutesElapsed = 10)
        {
            int readyCount = 0;
            for (int i = 0; i < Machines.Count; i++)
            {
                if (Machines[i].PerformTenMinuteUpdate(minutesElapsed))
                    readyCount++;
            }
            return readyCount;
        }

        public (bool executed, string message) PerformTileAction(int tileX, int tileY, GameEngine engine)
        {
            string key = $"{tileX},{tileY}";
            if (!ActionTiles.TryGetValue(key, out string? actionCommand) || string.IsNullOrWhiteSpace(actionCommand))
                return (false, $"Không có tương tác tại ô ({tileX}, {tileY}) thuộc [{DisplayName}].");

            string[] parts = actionCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string verb = parts[0];

            if (verb.Equals("CheckReflectiveSurface", StringComparison.OrdinalIgnoreCase))
            {
                string surfaceId = parts.Length > 1 ? parts[1] : "Mirror_FarmHouse";
                var refl = engine.CheckPlayerReflectiveSurface(surfaceId);
                return (true, $"[{DisplayName}] Soi {surfaceId}: Mặt thật={refl.realFace}, Phản chiếu={refl.reflectionFace} ({refl.description})");
            }

            if (verb.Equals("OpenMailbox", StringComparison.OrdinalIgnoreCase))
            {
                var readMail = engine.Mailbox.ReadNextMail();
                if (readMail == null)
                    return (true, "Hộp thư nông trại hiện không có thư mới.");
                return (true, $"Đã đọc thư [{readMail}].");
            }

            if (verb.Equals("InspectJackfruitCorner", StringComparison.OrdinalIgnoreCase))
            {
                engine.Story.HerdEmptySpotObserved = true;
                engine.TriggerManager.Raise(TriggerActionManager.TriggerManual, engine, this);
                return (true, "Quan sát góc gốc mít Tây Bắc (-18, 18): khoảng trống đường kính 1,2m khớp hoàn toàn với bức ảnh năm 2018.");
            }

            if (verb.Equals("OpenDayMarketShop", StringComparison.OrdinalIgnoreCase))
            {
                return (true, $"Mở sạp Chợ Ngày Bờ Rìa (Chủ đề hiện tại: {engine.Economy.DayMarket.Theme}).");
            }

            return (false, $"Hành động ô '{actionCommand}' chưa được hỗ trợ.");
        }
    }

    /// <summary>
    /// Đồ thị Thế giới hợp nhất quản lý toàn bộ các <see cref="GameLocation"/> của Pig Tycoon,
    /// tương đương danh sách <c>Game1.locations</c> và hệ thống <c>Game1.warpFarmer</c> trong Stardew Valley 1.6.
    /// </summary>
    [Serializable]
    public sealed class WorldLocationGraph
    {
        private readonly Dictionary<string, GameLocation> locationsById =
            new Dictionary<string, GameLocation>(StringComparer.OrdinalIgnoreCase);

        public string CurrentLocationId { get; set; } = "Farm";
        public int PlayerTileX { get; set; } = 12;
        public int PlayerTileY { get; set; } = 14;

        public GameLocation CurrentLocation =>
            locationsById.TryGetValue(CurrentLocationId, out var loc) ? loc : GetOrCreateDefaultFarm();

        public IReadOnlyCollection<GameLocation> AllLocations => locationsById.Values;

        public WorldLocationGraph()
        {
            BuildCanonicalLocations();
        }

        public GameLocation? GetLocation(string locationId)
        {
            if (string.IsNullOrWhiteSpace(locationId))
                return null;
            return locationsById.TryGetValue(locationId.Trim(), out var loc) ? loc : null;
        }

        public (bool warped, string message) TryWarpPlayer(string targetLocationId, int targetX, int targetY, GameEngine engine)
        {
            var target = GetLocation(targetLocationId);
            if (target == null)
                return (false, $"Địa điểm '{targetLocationId}' không tồn tại trong đồ thị thế giới.");

            // Kiểm tra điều kiện cổng Warp nếu đi từ vị trí hiện tại
            var current = CurrentLocation;
            for (int i = 0; i < current.Warps.Count; i++)
            {
                var w = current.Warps[i];
                if (w.TargetLocationId.Equals(targetLocationId, StringComparison.OrdinalIgnoreCase))
                {
                    if (!GameStateQuery.CheckConditions(w.RequiredCondition, engine, current))
                    {
                        return (false, $"Cổng di chuyển sang [{target.DisplayName}] đang khóa (yêu cầu: {w.RequiredCondition}).");
                    }
                }
            }

            CurrentLocationId = target.LocationId;
            PlayerTileX = targetX;
            PlayerTileY = targetY;

            engine.TriggerManager.Raise(TriggerActionManager.TriggerLocationChanged, engine, target);
            return (true, $"Đã di chuyển sang [{target.DisplayName}] tại tọa độ ({targetX}, {targetY}).");
        }

        public void OnDayStartedAllLocations(GameEngine engine)
        {
            foreach (var loc in locationsById.Values)
            {
                loc.OnDayStarted(engine);
            }
        }

        public int PerformTenMinuteUpdateAllLocations(int minutesElapsed = 10)
        {
            int readyTotal = 0;
            foreach (var loc in locationsById.Values)
            {
                readyTotal += loc.PerformTenMinuteUpdate(minutesElapsed);
            }
            return readyTotal;
        }

        private GameLocation GetOrCreateDefaultFarm()
        {
            if (!locationsById.TryGetValue("Farm", out var farm))
            {
                BuildCanonicalLocations();
                farm = locationsById["Farm"];
            }
            return farm;
        }

        private void BuildCanonicalLocations()
        {
            locationsById.Clear();

            // 1. Farm (Nông trại chính ngoài trời — 5 phân khu)
            var farm = new GameLocation
            {
                LocationId = "Farm",
                DisplayName = "Nông Trại Heo Bờ Rìa (5 Khu)",
                IsOutdoors = true,
                IsAnimalHouse = false
            };
            farm.ActionTiles["10,10"] = "OpenMailbox";
            farm.ActionTiles["-18,18"] = "InspectJackfruitCorner";
            farm.ActionTiles["8,12"] = "CheckReflectiveSurface Trough_S07";
            farm.Warps.Add(new LocationWarpPortal { SourceTileX = 12, SourceTileY = 8, TargetLocationId = "FarmHouse", TargetTileX = 5, TargetTileY = 9 });
            farm.Warps.Add(new LocationWarpPortal { SourceTileX = 18, SourceTileY = 14, TargetLocationId = "Barn_Primary", TargetTileX = 4, TargetTileY = 8 });
            farm.Warps.Add(new LocationWarpPortal { SourceTileX = 24, SourceTileY = 14, TargetLocationId = "Barn_Quarantine", TargetTileX = 3, TargetTileY = 6 });
            farm.Warps.Add(new LocationWarpPortal { SourceTileX = 35, SourceTileY = 20, TargetLocationId = "MarketTown", TargetTileX = 2, TargetTileY = 10 });
            farm.Warps.Add(new LocationWarpPortal { SourceTileX = 28, SourceTileY = 6, TargetLocationId = "GeneLab", TargetTileX = 4, TargetTileY = 7 });
            farm.Warps.Add(new LocationWarpPortal { SourceTileX = 30, SourceTileY = 4, TargetLocationId = "Observatory", TargetTileX = 4, TargetTileY = 6, RequiredCondition = "PLAYER_FARM_LEVEL 3" });
            farm.Warps.Add(new LocationWarpPortal { SourceTileX = 0, SourceTileY = 25, TargetLocationId = "BoundaryEdge", TargetTileX = 5, TargetTileY = 5 });

            // Đặt sẵn 3 máy chế biến cốt lõi tại Nông trại: Máy Trộn Cám, Hầm Biogas, Hố Ủ Phân
            farm.Machines.Add(CreateCanonicalMachine("machine_feed_mixer_1", "(BC)MayTronCam", "Máy Trộn Cám Thảo Dược", 15, 12));
            farm.Machines.Add(CreateCanonicalMachine("machine_biogas_1", "(BC)HamBiogas", "Hầm Khí Sinh Học Biogas", 20, 16));
            farm.Machines.Add(CreateCanonicalMachine("machine_compost_1", "(BC)HoUPhan", "Hố Ủ Phân Vi Sinh", 22, 16));
            locationsById[farm.LocationId] = farm;

            // 2. FarmHouse (Nội thất Nhà Chính — Gương soi, Lò sưởi S04/Q16, Bàn sổ tay)
            var farmHouse = new GameLocation
            {
                LocationId = "FarmHouse",
                DisplayName = "Nhà Chính Nông Trại",
                IsOutdoors = false
            };
            farmHouse.ActionTiles["3,4"] = "CheckReflectiveSurface Mirror_FarmHouse";
            farmHouse.ActionTiles["7,4"] = "CheckReflectiveSurface Window_Q10";
            farmHouse.Warps.Add(new LocationWarpPortal { SourceTileX = 5, SourceTileY = 9, TargetLocationId = "Farm", TargetTileX = 12, TargetTileY = 9 });
            locationsById[farmHouse.LocationId] = farmHouse;

            // 3. Barn_Primary (Nội thất Chuồng Nuôi Chính — AnimalHouse)
            var barnPrimary = new GameLocation
            {
                LocationId = "Barn_Primary",
                DisplayName = "Khu Chuồng Nuôi Chính (AnimalHouse)",
                IsOutdoors = false,
                IsAnimalHouse = true,
                MaxAnimalOccupants = 24,
                AllowAnimalPregnancy = true
            };
            barnPrimary.DailyConversions.Add(new BuildingItemConversionRule
            {
                RuleId = "Conv_AutoCompost_Barn",
                RequiredItemId = "(O)PhanHeoTho",
                RequiredCount = 2,
                ProducedItemId = "(O)PhanHuuCo",
                ProducedCount = 1,
                MaxDailyConversions = 2
            });
            barnPrimary.Warps.Add(new LocationWarpPortal { SourceTileX = 4, SourceTileY = 8, TargetLocationId = "Farm", TargetTileX = 18, TargetTileY = 15 });
            locationsById[barnPrimary.LocationId] = barnPrimary;

            // 4. Barn_Quarantine (Nội thất Chuồng Cách Ly Y Tế — AnimalHouse)
            var barnQuarantine = new GameLocation
            {
                LocationId = "Barn_Quarantine",
                DisplayName = "Chuồng Cách Ly Dịch Tễ",
                IsOutdoors = false,
                IsAnimalHouse = true,
                MaxAnimalOccupants = 8,
                AllowAnimalPregnancy = false
            };
            barnQuarantine.Warps.Add(new LocationWarpPortal { SourceTileX = 3, SourceTileY = 6, TargetLocationId = "Farm", TargetTileX = 24, TargetTileY = 15 });
            locationsById[barnQuarantine.LocationId] = barnQuarantine;

            // 5. MarketTown (Thị Trấn Bờ Rìa — Chợ Ngày & Chợ Đêm)
            var marketTown = new GameLocation
            {
                LocationId = "MarketTown",
                DisplayName = "Thị Trấn Cổ Bờ Rìa (Chợ Ngày & Chợ Đêm)",
                IsOutdoors = true
            };
            marketTown.ActionTiles["6,10"] = "OpenDayMarketShop";
            marketTown.Warps.Add(new LocationWarpPortal { SourceTileX = 2, SourceTileY = 10, TargetLocationId = "Farm", TargetTileX = 34, TargetTileY = 20 });
            locationsById[marketTown.LocationId] = marketTown;

            // 6. GeneLab (Phòng Nghiên Cứu Gen & Xưởng Giấy Đặc Tính)
            var geneLab = new GameLocation
            {
                LocationId = "GeneLab",
                DisplayName = "Phòng Nghiên Cứu Gen & Xưởng Giấy",
                IsOutdoors = false
            };
            geneLab.Machines.Add(CreateCanonicalMachine("machine_gene_decoder_1", "(BC)BanGiaiMaGen", "Bàn Giải Mã Gen", 5, 4));
            geneLab.Machines.Add(CreateCanonicalMachine("machine_paper_press_1", "(BC)BanEpGiayDacTinh", "Bàn Ép Giấy Đặc Tính", 8, 4));
            geneLab.Warps.Add(new LocationWarpPortal { SourceTileX = 4, SourceTileY = 7, TargetLocationId = "Farm", TargetTileX = 28, TargetTileY = 7 });
            locationsById[geneLab.LocationId] = geneLab;

            // 7. Observatory (Đài Thiên Văn Mái Xanh Viền Vàng — Zone 05)
            var observatory = new GameLocation
            {
                LocationId = "Observatory",
                DisplayName = "Đài Thiên Văn Cổ (Zone 05)",
                IsOutdoors = false
            };
            observatory.Warps.Add(new LocationWarpPortal { SourceTileX = 4, SourceTileY = 6, TargetLocationId = "Farm", TargetTileX = 30, TargetTileY = 5 });
            locationsById[observatory.LocationId] = observatory;

            // 8. BoundaryEdge (Ranh Giới Rừng Nguyên Sinh & Điểm Gặp Bí Nhân)
            var boundary = new GameLocation
            {
                LocationId = "BoundaryEdge",
                DisplayName = "Ranh Giới Rừng Nguyên Sinh",
                IsOutdoors = true
            };
            boundary.Warps.Add(new LocationWarpPortal { SourceTileX = 5, SourceTileY = 5, TargetLocationId = "Farm", TargetTileX = 1, TargetTileY = 25 });
            locationsById[boundary.LocationId] = boundary;
        }

        private static PlacedMachineProcessor CreateCanonicalMachine(string instanceId, string machineQid, string displayName, int tileX, int tileY)
        {
            var processor = new PlacedMachineProcessor
            {
                InstanceId = instanceId,
                MachineQualifiedId = machineQid,
                DisplayName = displayName,
                TileX = tileX,
                TileY = tileY
            };

            if (machineQid.Equals("(BC)MayTronCam", StringComparison.OrdinalIgnoreCase))
            {
                processor.OutputRules.Add(new MachineOutputRule
                {
                    RuleId = "Rule_FeedMixer_HerbalFeed",
                    Trigger = "ItemPlacedInMachine",
                    RequiredInputItemId = "(F)CamThuong",
                    RequiredInputCount = 1,
                    AdditionalConsumedItemId = "(M)ThaoDuocKho",
                    AdditionalConsumedCount = 1,
                    Condition = "TRUE",
                    OutputItemId = "(F)CamThaoDuoc",
                    OutputCount = 2,
                    MinutesUntilReady = 60
                });
            }
            else if (machineQid.Equals("(BC)HamBiogas", StringComparison.OrdinalIgnoreCase))
            {
                processor.OutputRules.Add(new MachineOutputRule
                {
                    RuleId = "Rule_Biogas_BioFuel",
                    Trigger = "ItemPlacedInMachine",
                    RequiredInputItemId = "(O)PhanHeoTho",
                    RequiredInputCount = 3,
                    Condition = "TRUE",
                    OutputItemId = "(O)XangSinhHoc",
                    OutputCount = 1,
                    MinutesUntilReady = 120
                });
            }
            else if (machineQid.Equals("(BC)HoUPhan", StringComparison.OrdinalIgnoreCase))
            {
                processor.OutputRules.Add(new MachineOutputRule
                {
                    RuleId = "Rule_Compost_OrganicFertilizer",
                    Trigger = "ItemPlacedInMachine",
                    RequiredInputItemId = "(O)PhanHeoTho",
                    RequiredInputCount = 2,
                    Condition = "TRUE",
                    OutputItemId = "(O)PhanHuuCo",
                    OutputCount = 1,
                    MinutesUntilReady = 90
                });
            }
            else if (machineQid.Equals("(BC)BanGiaiMaGen", StringComparison.OrdinalIgnoreCase))
            {
                processor.OutputRules.Add(new MachineOutputRule
                {
                    RuleId = "Rule_GeneDecoder_BluePaper",
                    Trigger = "ItemPlacedInMachine",
                    RequiredInputItemId = "(G)MauGenTho",
                    RequiredInputCount = 1,
                    Condition = "TRUE",
                    OutputItemId = "(G)Paper_Blue",
                    OutputCount = 1,
                    MinutesUntilReady = 120
                });
            }
            else if (machineQid.Equals("(BC)BanEpGiayDacTinh", StringComparison.OrdinalIgnoreCase))
            {
                processor.OutputRules.Add(new MachineOutputRule
                {
                    RuleId = "Rule_PaperPress_BlueToYellow",
                    Trigger = "ItemPlacedInMachine",
                    RequiredInputItemId = "(G)Paper_Blue",
                    RequiredInputCount = 2,
                    AdditionalConsumedItemId = "(O)TinhThach",
                    AdditionalConsumedCount = 1,
                    Condition = "TRUE",
                    OutputItemId = "(G)Paper_Yellow",
                    OutputCount = 1,
                    MinutesUntilReady = 90
                });
                processor.OutputRules.Add(new MachineOutputRule
                {
                    RuleId = "Rule_PaperPress_YellowToPurple",
                    Trigger = "ItemPlacedInMachine",
                    RequiredInputItemId = "(G)Paper_Yellow",
                    RequiredInputCount = 2,
                    AdditionalConsumedItemId = "(O)TinhThach",
                    AdditionalConsumedCount = 2,
                    Condition = "TRUE",
                    OutputItemId = "(G)Paper_Purple",
                    OutputCount = 1,
                    MinutesUntilReady = 150
                });
            }

            return processor;
        }
    }

    #endregion

    #region 6. PlayerMailboxSystem, ConversationTopicTracker, StatsDictionary & WorldTraversalUtility

    /// <summary>
    /// Hệ thống Hộp Thư (<c>mailbox</c> / <c>mailForTomorrow</c> / <c>mailReceived</c>) lấy mẫu từ <c>StardewValley.Farmer</c>.
    /// </summary>
    [Serializable]
    public sealed class PlayerMailboxSystem
    {
        public List<string> Mailbox { get; } = new List<string>();
        public List<string> MailForTomorrow { get; } = new List<string>();
        public HashSet<string> MailReceived { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void DeliverMailNow(string mailId)
        {
            if (string.IsNullOrWhiteSpace(mailId))
                return;
            string id = mailId.Trim();
            if (!MailReceived.Contains(id) && !Mailbox.Contains(id))
            {
                Mailbox.Add(id);
            }
        }

        public void EnqueueMailForTomorrow(string mailId)
        {
            if (string.IsNullOrWhiteSpace(mailId))
                return;
            string id = mailId.Trim();
            if (!MailReceived.Contains(id) && !MailForTomorrow.Contains(id))
            {
                MailForTomorrow.Add(id);
            }
        }

        public string? ReadNextMail()
        {
            if (Mailbox.Count == 0)
                return null;
            string next = Mailbox[0];
            Mailbox.RemoveAt(0);
            MailReceived.Add(next);
            return next;
        }

        public void OnNewDayRollover()
        {
            for (int i = 0; i < MailForTomorrow.Count; i++)
            {
                string id = MailForTomorrow[i];
                if (!Mailbox.Contains(id) && !MailReceived.Contains(id))
                    Mailbox.Add(id);
            }
            MailForTomorrow.Clear();
        }

        public bool HasReceivedOrQueuedMail(string mailId)
        {
            if (string.IsNullOrWhiteSpace(mailId))
                return false;
            string id = mailId.Trim();
            return MailReceived.Contains(id) || Mailbox.Contains(id) || MailForTomorrow.Contains(id);
        }
    }

    /// <summary>
    /// Theo dõi các Chủ đề Hội thoại đang hoạt động theo số ngày đếm ngược (<c>Farmer.activeDialogueEvents</c> / <c>ConversationTopic</c>).
    /// Giúp NPC và Thị trấn Bờ Rìa phản ứng tự nhiên trong nhiều ngày sau mỗi hành động của người chơi.
    /// </summary>
    [Serializable]
    public sealed class ConversationTopicTracker
    {
        public Dictionary<string, int> ActiveTopicsDaysRemaining { get; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public void StartTopic(string topicId, int durationDays = 3)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                return;
            ActiveTopicsDaysRemaining[topicId.Trim()] = Math.Max(1, durationDays);
        }

        public bool IsTopicActive(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                return false;
            return ActiveTopicsDaysRemaining.TryGetValue(topicId.Trim(), out int days) && days > 0;
        }

        public void DecrementTopicsOnNewDay()
        {
            var keys = new List<string>(ActiveTopicsDaysRemaining.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string k = keys[i];
                int rem = ActiveTopicsDaysRemaining[k] - 1;
                if (rem <= 0)
                    ActiveTopicsDaysRemaining.Remove(k);
                else
                    ActiveTopicsDaysRemaining[k] = rem;
            }
        }
    }

    /// <summary>
    /// Từ điển Thống kê &amp; Thành tựu trọn đời tương đương <c>StardewValley.Stats</c>.
    /// </summary>
    [Serializable]
    public sealed class PlayerStatsDictionary
    {
        public Dictionary<string, uint> Values { get; } =
            new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> UnlockedAchievements { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public uint Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return 0u;
            return Values.TryGetValue(key.Trim(), out uint val) ? val : 0u;
        }

        public uint Increment(string key, uint amount = 1u)
        {
            if (string.IsNullOrWhiteSpace(key))
                return 0u;
            string k = key.Trim();
            uint next = Get(k) + amount;
            Values[k] = next;
            CheckForAchievements();
            return next;
        }

        public void CheckForAchievements()
        {
            if (Get("PigsSold") >= 1u)
                UnlockedAchievements.Add("Ach_FirstPigSold");
            if (Get("PigsSold") >= 20u)
                UnlockedAchievements.Add("Ach_SeasonedBreeder");
            if (Get("WavesSurvived") >= 1u)
                UnlockedAchievements.Add("Ach_BorderDefender");
            if (Get("MachinesProcessed") >= 5u)
                UnlockedAchievements.Add("Ach_BioEngineer");
        }
    }

    /// <summary>
    /// Tiện ích duyệt toàn bộ thực thể trong thế giới tương đương <c>StardewValley.Internal.ForEachItemHelper</c> &amp; <c>Utility.ForEach*</c>.
    /// </summary>
    public static class WorldTraversalUtility
    {
        public static void ForEachLocation(GameEngine engine, Action<GameLocation> action)
        {
            if (engine == null || action == null)
                return;
            foreach (var loc in engine.WorldLocations.AllLocations)
            {
                action(loc);
            }
        }

        public static void ForEachItemInWorld(GameEngine engine, Action<UnifiedItemInstance, string> action)
        {
            if (engine == null || action == null)
                return;

            var slots = engine.Inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                action(slots[i], "PlayerInventory");
            }

            foreach (var loc in engine.WorldLocations.AllLocations)
            {
                for (int m = 0; m < loc.Machines.Count; m++)
                {
                    var held = loc.Machines[m].HeldOutput;
                    if (held != null)
                        action(held, $"{loc.LocationId}:{loc.Machines[m].InstanceId}");
                }
            }
        }
    }

    #endregion
}
