using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class LootItemData
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public LootCategory Category { get; set; } = LootCategory.Material;
        public LootRarity Rarity { get; set; } = LootRarity.Thuong;
        public int Quantity { get; set; } = 1;
        public int GoldValue { get; set; } = 10;
        public string SpriteKey { get; set; } = "";
        public string Description { get; set; } = "";
    }

    [Serializable]
    public class LootDropRule
    {
        public string ItemId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public LootCategory Category { get; set; } = LootCategory.Material;
        public LootRarity Rarity { get; set; } = LootRarity.Thuong;
        public float DropRate { get; set; } = 0.5f;
        public int MinQuantity { get; set; } = 1;
        public int MaxQuantity { get; set; } = 1;
        public int GoldValue { get; set; } = 10;
        public string SpriteKey { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public static class LootDatabase
    {
        private static readonly Dictionary<EnemyTypeId, List<LootDropRule>> tables = new Dictionary<EnemyTypeId, List<LootDropRule>>();
        private static readonly Random defaultRng = new Random();
        public static readonly Dictionary<string, string> BlueprintItemToBlueprintId = new Dictionary<string, string>
        {
            { "item_blueprint_plasma_beam", "TruPlasmaHoiTu" },
            { "item_blueprint_quantum_mortar", "PhaoCoiLuongTu" },
            { "item_blueprint_graviton_tractor", "ThapPhanTrongLuc" },
            { "item_blueprint_emp_sentry", "TruXungDienEMP" },
            { "item_blueprint_antimatter_spire", "PhaoDaiPhanVatChat" },

            { "item_blueprint_gatling_crossbow", "NoLienChauThep" },
            { "item_blueprint_saw_catapult", "MayBanRiuCua" },
            { "item_blueprint_tar_flamethrower", "ThapPhunLuaHacIn" },
            { "item_blueprint_harpoon_ballista", "PhaoLaoMocXich" },
            { "item_blueprint_warlord_howitzer", "DaiBacChuaTrom" },

            { "item_blueprint_spine_ejector", "UGaiDocGamNhom" },
            { "item_blueprint_spore_emitter", "TruNamBaoTuMu" },
            { "item_blueprint_corrosive_spitter", "OngBomDichAxit" },
            { "item_blueprint_leech_obelisk", "CotToHutHon" },
            { "item_blueprint_bone_spire", "CotDiCotSuongHuyen" }
        };


        static LootDatabase()
        {
            InitializeTables();
        }

        private static void InitializeTables()
        {
            // 1. Kẻ Cào Phá (Fence Breaker - Chuyên phá rào) -> Rơi vật liệu gỗ, đinh, cọc rào
            tables[EnemyTypeId.KeCaoPha] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_wood_plank",
                    DisplayName = "Gỗ Xẻ",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.75f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 12,
                    SpriteKey = "item_wood_plank",
                    Description = "Thanh gỗ xẻ nguyên khối, dùng sửa chữa hoặc chế tạo hàng rào."
                },
                new LootDropRule
                {
                    ItemId = "item_rusty_nail",
                    DisplayName = "Đinh Rỉ",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.45f,
                    MinQuantity = 2,
                    MaxQuantity = 4,
                    GoldValue = 15,
                    SpriteKey = "item_rusty_nail",
                    Description = "Những chiếc đinh sắt bị rỉ sét thu gom từ rào trại."
                },
                new LootDropRule
                {
                    ItemId = "item_broken_post",
                    DisplayName = "Cọc Rào Cũ",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.30f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 20,
                    SpriteKey = "item_broken_post",
                    Description = "Cọc rào còn nguyên khung, có thể tái sử dụng để dựng lại rào."
                },
                new LootDropRule
                {
                    ItemId = "item_gold_pouch",
                    DisplayName = "Túi Tiền Cướp Được",
                    Category = LootCategory.Currency,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.15f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 35,
                    SpriteKey = "icon_coin_gold",
                    Description = "Túi tiền vải thô chứa vàng trộm cắp."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_saw_catapult",
                    DisplayName = "Bản Vẽ: Máy Bắn Rìu Cưa Gai Vụn",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.08f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 150,
                    SpriteKey = "item_blueprint_saw_catapult",
                    Description = "Bản vẽ chế tạo cỗ máy bắn rìu cưa sắt xoay nổ mảnh đinh."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_tar_flamethrower",
                    DisplayName = "Bản Vẽ: Tháp Phun Lửa Hắc Ín",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.06f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 180,
                    SpriteKey = "item_blueprint_tar_flamethrower",
                    Description = "Bản vẽ tháp phun hắc ín thiêu đốt mặt đất."
                }
            };

            // 2. Kẻ Bắt Heo (Pig Snatcher - Trộm heo) -> Rơi bao cám ngô, túi tiền, dây thừng
            tables[EnemyTypeId.KeBatHeo] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_corn_feed",
                    DisplayName = "Bao Cám Ngô",
                    Category = LootCategory.Feed,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.80f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 18,
                    SpriteKey = "item_corn_feed",
                    Description = "Bao tải cám ngô thơm bùi, dùng đổ trực tiếp vào máng ăn cho đàn heo."
                },
                new LootDropRule
                {
                    ItemId = "item_gold_pouch",
                    DisplayName = "Túi Vàng Nhỏ",
                    Category = LootCategory.Currency,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.50f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 25,
                    SpriteKey = "icon_coin_gold",
                    Description = "Túi vàng nhỏ kẻ trộm cướp giật được."
                },
                new LootDropRule
                {
                    ItemId = "item_rope",
                    DisplayName = "Dây Thừng Trói Heo",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.20f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 40,
                    SpriteKey = "item_rope",
                    Description = "Cuộn dây thừng bện gai chuyên dùng để trói bắt gia súc."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_gatling_crossbow",
                    DisplayName = "Bản Vẽ: Nỏ Liên Châu Bọc Thép",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.10f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 100,
                    SpriteKey = "item_blueprint_gatling_crossbow",
                    Description = "Bản vẽ nỏ liên châu bắn liên thanh 3 mũi."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_tar_flamethrower",
                    DisplayName = "Bản Vẽ: Tháp Phun Lửa Hắc Ín",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.06f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 180,
                    SpriteKey = "item_blueprint_tar_flamethrower",
                    Description = "Bản vẽ tháp phun hắc ín thiêu đốt mặt đất."
                }
            };

            // 3. Chuột Gặm Kho (Silo Pest - Dịch hại) -> Rơi mẩu phân bón, đuôi chuột, ngô đột biến
            tables[EnemyTypeId.ChuotGamKho] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_fertilizer",
                    DisplayName = "Phân Bón Vi Sinh",
                    Category = LootCategory.Special,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.70f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 8,
                    SpriteKey = "item_fertilizer",
                    Description = "Phân hữu cơ vi sinh, tăng độ màu mỡ cho đồng cỏ."
                },
                new LootDropRule
                {
                    ItemId = "item_rat_tail",
                    DisplayName = "Đuôi Chuột Sấy Khô",
                    Category = LootCategory.Special,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.35f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 18,
                    SpriteKey = "item_rat_tail",
                    Description = "Đuôi chuột kho, có thể bán cho thầy thuốc thú y."
                },
                new LootDropRule
                {
                    ItemId = "item_mutant_corn",
                    DisplayName = "Hạt Ngô Đột Biến",
                    Category = LootCategory.Feed,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.10f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 50,
                    SpriteKey = "item_mutant_corn",
                    Description = "Hạt ngô phát sáng kỳ lạ ăn cắp từ kho huyền bí."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_spine_ejector",
                    DisplayName = "Bản Vẽ: Ụ Gai Độc Gặm Nhấm",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.10f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 80,
                    SpriteKey = "item_blueprint_spine_ejector",
                    Description = "Bản vẽ bẫy kén phóng gai độc 360 độ."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_spore_emitter",
                    DisplayName = "Bản Vẽ: Trụ Nấm Phun Bào Tử Mù",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.08f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 150,
                    SpriteKey = "item_blueprint_spore_emitter",
                    Description = "Bản vẽ trụ nấm độc phun mây bào tử gây mù và trúng độc."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_corrosive_spitter",
                    DisplayName = "Bản Vẽ: Ống Bơm Dịch Nhầy Axit",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.07f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 180,
                    SpriteKey = "item_blueprint_corrosive_spitter",
                    Description = "Bản vẽ ống bơm axit nảy làm mòn giáp quái."
                }
            };

            // 4. UFO Trinh Sát & Thu Hoạch (Alien Tech - Ngoài hành tinh) -> Rơi hợp kim, cáp quang, lõi plasma
            tables[EnemyTypeId.UfoTrinhSat] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_alloy_scrap",
                    DisplayName = "Mảnh Hợp Kim Nhẹ",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.80f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 30,
                    SpriteKey = "item_alloy_scrap",
                    Description = "Mảnh kim loại siêu bền màu bạc ánh xanh từ vỏ đĩa bay."
                },
                new LootDropRule
                {
                    ItemId = "item_quantum_fiber",
                    DisplayName = "Cáp Quang Tử",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.45f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 55,
                    SpriteKey = "item_quantum_fiber",
                    Description = "Dây dẫn quang học phát sáng dùng cho công nghệ lơ lửng."
                },
                new LootDropRule
                {
                    ItemId = "item_plasma_core",
                    DisplayName = "Lõi Plasma Siêu Nhỏ",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.15f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 95,
                    SpriteKey = "item_plasma_core",
                    Description = "Khối cầu năng lượng plasma ổn định cung cấp điện cho phi thuyền."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_plasma_beam",
                    DisplayName = "Bản Vẽ: Trụ Tia Plasma Hội Tụ",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.08f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 150,
                    SpriteKey = "item_blueprint_plasma_beam",
                    Description = "Bản vẽ trụ chiếu tia plasma liên tục ramp-up DPS."
                }
            };

            tables[EnemyTypeId.UfoThuHoach] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_alloy_scrap",
                    DisplayName = "Mảnh Hợp Kim Nhẹ",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.90f,
                    MinQuantity = 2,
                    MaxQuantity = 3,
                    GoldValue = 30,
                    SpriteKey = "item_alloy_scrap",
                    Description = "Mảnh hợp kim siêu bền nhặt từ mảnh vỡ đĩa bay."
                },
                new LootDropRule
                {
                    ItemId = "item_quantum_fiber",
                    DisplayName = "Cáp Quang Tử",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.60f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 55,
                    SpriteKey = "item_quantum_fiber",
                    Description = "Cáp năng lượng quang tử."
                },
                new LootDropRule
                {
                    ItemId = "item_plasma_core",
                    DisplayName = "Lõi Plasma Siêu Nhỏ",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.30f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 95,
                    SpriteKey = "item_plasma_core",
                    Description = "Lõi năng lượng nén mật độ cao."
                },
                new LootDropRule
                {
                    ItemId = "item_quantum_chip",
                    DisplayName = "Chip Lượng Tử Vỡ",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.CucPham,
                    DropRate = 0.05f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 300,
                    SpriteKey = "item_quantum_chip",
                    Description = "Bộ xử lý lượng tử tối tân từ Mẫu Hạm UFO."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_quantum_mortar",
                    DisplayName = "Bản Vẽ: Pháo Cối Phân Rã Lượng Tử",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.06f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 250,
                    SpriteKey = "item_blueprint_quantum_mortar",
                    Description = "Bản vẽ pháo cối bắn đạn cầu plasma nổ AoE."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_emp_sentry",
                    DisplayName = "Bản Vẽ: Trụ Xung Điện EMP Phòng Không",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.04f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 300,
                    SpriteKey = "item_blueprint_emp_sentry",
                    Description = "Bản vẽ trụ EMP hạ gục UFO và phá khiên."
                }
            };

            // 5. Đầu Lĩnh Trộm (Elite Leader)
            tables[EnemyTypeId.DauLinhTrom] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_gold_pouch",
                    DisplayName = "Túi Vàng Lớn",
                    Category = LootCategory.Currency,
                    Rarity = LootRarity.Hiem,
                    DropRate = 1.0f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 120,
                    SpriteKey = "icon_coin_gold",
                    Description = "Túi vàng nặng trĩu của tướng cướp."
                },
                new LootDropRule
                {
                    ItemId = "item_wood_plank",
                    DisplayName = "Gỗ Xẻ",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.90f,
                    MinQuantity = 3,
                    MaxQuantity = 5,
                    GoldValue = 12,
                    SpriteKey = "item_wood_plank",
                    Description = "Bó gỗ xẻ."
                },
                new LootDropRule
                {
                    ItemId = "item_rope",
                    DisplayName = "Dây Thừng Trói Heo",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.70f,
                    MinQuantity = 2,
                    MaxQuantity = 2,
                    GoldValue = 40,
                    SpriteKey = "item_rope",
                    Description = "Dây thừng cao cấp."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_harpoon_ballista",
                    DisplayName = "Bản Vẽ: Pháo Lao Móc Xích Quỷ",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.12f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 350,
                    SpriteKey = "item_blueprint_harpoon_ballista",
                    Description = "Bản vẽ pháo phóng lao sắt móc xích giật lùi mục tiêu."
                }
            };

            // 5b. Chúa Trộm Rìu Sắt (Iron-Axe Bandit Lord - Boss)
            tables[EnemyTypeId.ChuaTromRiuSat] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_iron_axe_head",
                    DisplayName = "Lưỡi Rìu Sắt Đại Tướng",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.CucPham,
                    DropRate = 1.0f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 350,
                    SpriteKey = "item_alloy_scrap",
                    Description = "Lưỡi rìu sắt khổng lồ sắc bén của Chúa Trộm, dùng để rèn vũ khí hạng nặng."
                },
                new LootDropRule
                {
                    ItemId = "item_gold_pouch_huge",
                    DisplayName = "Rương Vàng Cướp Bóc",
                    Category = LootCategory.Currency,
                    Rarity = LootRarity.CucPham,
                    DropRate = 1.0f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 450,
                    SpriteKey = "icon_coin_gold",
                    Description = "Rương chứa đầy tiền vàng và bảo vật cướp bóc được từ các trang trại lân cận."
                },
                new LootDropRule
                {
                    ItemId = "item_wood_plank",
                    DisplayName = "Gỗ Lim Thượng Hạng",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Tot,
                    DropRate = 1.0f,
                    MinQuantity = 6,
                    MaxQuantity = 10,
                    GoldValue = 15,
                    SpriteKey = "item_wood_plank",
                    Description = "Các thanh gỗ lim chắc nịch lấy từ cỗ xe công thành của băng cướp."
                },
                new LootDropRule
                {
                    ItemId = "item_rope",
                    DisplayName = "Dây Xích Sắt Khóa Chuồng",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.85f,
                    MinQuantity = 3,
                    MaxQuantity = 4,
                    GoldValue = 50,
                    SpriteKey = "item_rope",
                    Description = "Cuộn dây xích sắt chuyên dụng để khóa then phá cửa chuồng trại."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_warlord_howitzer",
                    DisplayName = "Bản Vẽ: Đại Bác Công Thành Chúa Trộm",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.CucPham,
                    DropRate = 0.20f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 600,
                    SpriteKey = "item_blueprint_warlord_howitzer",
                    Description = "Bản vẽ đại bác tối thượng của Chúa Trộm nổ rung chuyển màn hình."
                }
            };

            // 6. Lính Ngoài Hành Tinh (Alien Grunt)
            tables[EnemyTypeId.AlienGrunt] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_alloy_scrap",
                    DisplayName = "Mảnh Hợp Kim Nhẹ",
                    Category = LootCategory.Material,
                    Rarity = LootRarity.Thuong,
                    DropRate = 0.85f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 30,
                    SpriteKey = "item_alloy_scrap",
                    Description = "Mảnh giáp hợp kim nhẹ của lính dị tộc."
                },
                new LootDropRule
                {
                    ItemId = "item_quantum_fiber",
                    DisplayName = "Cáp Quang Tử",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.35f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 55,
                    SpriteKey = "item_quantum_fiber",
                    Description = "Cáp vi mạch từ trang bị của lính ngoài hành tinh."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_graviton_tractor",
                    DisplayName = "Bản Vẽ: Tháp Phát Phản Trọng Lực",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.05f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 280,
                    SpriteKey = "item_blueprint_graviton_tractor",
                    Description = "Bản vẽ tháp phát sóng tractor beam làm chậm và cắt đứt dây bắt heo."
                }
            };

            // 7. Quái Tinh Nhuệ Ngoài Hành Tinh (Alien Elite)
            tables[EnemyTypeId.AlienElite] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_quantum_fiber",
                    DisplayName = "Cáp Quang Tử",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.80f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 55,
                    SpriteKey = "item_quantum_fiber",
                    Description = "Cáp quang tử cao cấp dẫn truyền năng lượng trượng plasma."
                },
                new LootDropRule
                {
                    ItemId = "item_plasma_core",
                    DisplayName = "Lõi Plasma Siêu Nhỏ",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.45f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 95,
                    SpriteKey = "item_plasma_core",
                    Description = "Khối cầu năng lượng plasma ổn định từ trượng chỉ huy."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_emp_sentry",
                    DisplayName = "Bản Vẽ: Trụ Xung Điện EMP Phòng Không",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.04f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 300,
                    SpriteKey = "item_blueprint_emp_sentry",
                    Description = "Bản vẽ trụ EMP hạ gục UFO và phá khiên."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_graviton_tractor",
                    DisplayName = "Bản Vẽ: Tháp Phát Phản Trọng Lực",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.05f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 280,
                    SpriteKey = "item_blueprint_graviton_tractor",
                    Description = "Bản vẽ tháp phát sóng tractor beam làm chậm và cắt đứt dây bắt heo."
                }
            };

            // 8. Boss Đại Tướng Alien Warlord
            tables[EnemyTypeId.BossAlienWarlord] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_plasma_core",
                    DisplayName = "Lõi Plasma Siêu Cấp",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Hiem,
                    DropRate = 1.0f,
                    MinQuantity = 2,
                    MaxQuantity = 4,
                    GoldValue = 95,
                    SpriteKey = "item_plasma_core",
                    Description = "Nguồn năng lượng cực đại điều khiển vũ khí tối thượng của Boss."
                },
                new LootDropRule
                {
                    ItemId = "item_quantum_chip",
                    DisplayName = "Bộ Xử Lý Lượng Tử",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.CucPham,
                    DropRate = 0.50f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 300,
                    SpriteKey = "item_quantum_chip",
                    Description = "Bộ xử lý lượng tử tối tân từ Đại Tướng Warlord."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_antimatter_spire",
                    DisplayName = "Bản Vẽ: Pháo Đài Phản Vật Chất Khổng Lồ",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.CucPham,
                    DropRate = 0.15f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 800,
                    SpriteKey = "item_blueprint_antimatter_spire",
                    Description = "Bản vẽ đại pháo phản vật chất Mega Beam quét toàn bản đồ."
                }
            };

            // 9. Tàu Mẹ UFO (Mothership)
            tables[EnemyTypeId.UfoMothership] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_quantum_chip",
                    DisplayName = "Lõi Ma Trận Tàu Mẹ",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.CucPham,
                    DropRate = 1.0f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 500,
                    SpriteKey = "item_quantum_chip",
                    Description = "Trái tim công nghệ ma trận điều khiển toàn bộ hạm đội đĩa bay."
                },
                new LootDropRule
                {
                    ItemId = "item_plasma_core",
                    DisplayName = "Lõi Plasma Siêu Cấp",
                    Category = LootCategory.Tech,
                    Rarity = LootRarity.Hiem,
                    DropRate = 1.0f,
                    MinQuantity = 5,
                    MaxQuantity = 8,
                    GoldValue = 95,
                    SpriteKey = "item_plasma_core",
                    Description = "Cụm pin năng lượng plasma khổng lồ của tàu mẹ."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_antimatter_spire",
                    DisplayName = "Bản Vẽ: Pháo Đài Phản Vật Chất Khổng Lồ",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.CucPham,
                    DropRate = 0.15f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 800,
                    SpriteKey = "item_blueprint_antimatter_spire",
                    Description = "Bản vẽ đại pháo phản vật chất Mega Beam quét toàn bản đồ."
                }
            };

            // 10. Chó Sương (Mist Hound)
            tables[EnemyTypeId.ChoSuong] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_rat_tail",
                    DisplayName = "Đuôi Quái Thú Sấy Khô",
                    Category = LootCategory.Special,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.70f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 25,
                    SpriteKey = "item_rat_tail",
                    Description = "Đuôi dị thú săn đêm trong sương mù."
                },
                new LootDropRule
                {
                    ItemId = "item_mutant_corn",
                    DisplayName = "Hạt Ngô Đột Biến",
                    Category = LootCategory.Feed,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.35f,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    GoldValue = 50,
                    SpriteKey = "item_mutant_corn",
                    Description = "Hạt ngô phát quang hấp thụ tà khí sương mù."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_leech_obelisk",
                    DisplayName = "Bản Vẽ: Cột Tổ Hút Hồn Tầm Huyết",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.10f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 350,
                    SpriteKey = "item_blueprint_leech_obelisk",
                    Description = "Bản vẽ cột tổ phóng bọ hút máu quái hồi máu cho rào trại."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_bone_spire",
                    DisplayName = "Bản Vẽ: Cột Dị Cốt Sương Huyền",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.CucPham,
                    DropRate = 0.15f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 650,
                    SpriteKey = "item_blueprint_bone_spire",
                    Description = "Bản vẽ cột dị cốt trồi gai xương từ lòng đất đâm xuyên và khóa kỹ năng."
                }
            };

            // 11. Bóng Huyền (Shadow Phantom)
            tables[EnemyTypeId.BongHuyen] = new List<LootDropRule>
            {
                new LootDropRule
                {
                    ItemId = "item_mutant_corn",
                    DisplayName = "Hạt Ngô Đột Biến",
                    Category = LootCategory.Feed,
                    Rarity = LootRarity.Hiem,
                    DropRate = 0.60f,
                    MinQuantity = 2,
                    MaxQuantity = 3,
                    GoldValue = 50,
                    SpriteKey = "item_mutant_corn",
                    Description = "Hạt ngũ cốc biến dị rơi ra từ bóng ma."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_corrosive_spitter",
                    DisplayName = "Bản Vẽ: Ống Bơm Dịch Nhầy Axit",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.Tot,
                    DropRate = 0.07f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 180,
                    SpriteKey = "item_blueprint_corrosive_spitter",
                    Description = "Bản vẽ ống bơm axit nảy làm mòn giáp quái."
                },
                new LootDropRule
                {
                    ItemId = "item_blueprint_bone_spire",
                    DisplayName = "Bản Vẽ: Cột Dị Cốt Sương Huyền",
                    Category = LootCategory.Blueprint,
                    Rarity = LootRarity.CucPham,
                    DropRate = 0.15f,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    GoldValue = 650,
                    SpriteKey = "item_blueprint_bone_spire",
                    Description = "Bản vẽ cột dị cốt trồi gai xương từ lòng đất đâm xuyên và khóa kỹ năng."
                }
            };
        }

        public static List<LootItemData> RollLoot(EnemyTypeId enemyType, Random? rng = null)
        {
            var random = rng ?? defaultRng;
            var results = new List<LootItemData>();

            if (!tables.TryGetValue(enemyType, out var rules))
            {
                // Fallback mặc định nếu chưa cấu hình
                rules = tables[EnemyTypeId.KeCaoPha];
            }

            foreach (var rule in rules)
            {
                double roll = random.NextDouble();
                if (roll <= rule.DropRate)
                {
                    int qty = random.Next(rule.MinQuantity, rule.MaxQuantity + 1);
                    results.Add(new LootItemData
                    {
                        Id = rule.ItemId,
                        DisplayName = rule.DisplayName,
                        Category = rule.Category,
                        Rarity = rule.Rarity,
                        Quantity = qty,
                        GoldValue = rule.GoldValue,
                        SpriteKey = rule.SpriteKey,
                        Description = rule.Description
                    });
                }
            }

            // Đảm bảo quái chết luôn rơi ít nhất 1 món nếu danh sách trống (pity drop)
            if (results.Count == 0 && rules.Count > 0)
            {
                var fallbackRule = rules[0];
                results.Add(new LootItemData
                {
                    Id = fallbackRule.ItemId,
                    DisplayName = fallbackRule.DisplayName,
                    Category = fallbackRule.Category,
                    Rarity = fallbackRule.Rarity,
                    Quantity = 1,
                    GoldValue = fallbackRule.GoldValue,
                    SpriteKey = fallbackRule.SpriteKey,
                    Description = fallbackRule.Description
                });
            }

            return results;
        }

        public static string GetRarityHexColor(LootRarity rarity)
        {
            return rarity switch
            {
                LootRarity.Thuong => "#FFFFFF",   // Trắng
                LootRarity.Tot => "#39E75F",      // Xanh lá ngọc
                LootRarity.Hiem => "#3EB4FF",     // Xanh dương sáng
                LootRarity.CucPham => "#FFA726", // Cam hổ phách
                _ => "#FFFFFF"
            };
        }

        public static LootItemData CreateLootItem(string itemId, int quantity = 1)
        {
            foreach (var kvp in tables)
            {
                foreach (var rule in kvp.Value)
                {
                    if (rule.ItemId == itemId)
                    {
                        return new LootItemData
                        {
                            Id = rule.ItemId,
                            DisplayName = rule.DisplayName,
                            Category = rule.Category,
                            Rarity = rule.Rarity,
                            Quantity = quantity,
                            GoldValue = rule.GoldValue,
                            SpriteKey = rule.SpriteKey,
                            Description = rule.Description
                        };
                    }
                }
            }

            return new LootItemData
            {
                Id = itemId,
                DisplayName = itemId,
                Category = LootCategory.Material,
                Rarity = LootRarity.Thuong,
                Quantity = quantity,
                GoldValue = 10,
                SpriteKey = itemId,
                Description = ""
            };
        }
    }
}

