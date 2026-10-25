using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class DefenseBlueprint
    {
        public string Id;
        public string Name;
        public BuildingCategory Category;
        public string Rarity;
        public int RequiredHallLevel;
        public int BuildCostGold;
        public int BuildCostWood = 0;
        public float Damage = 40f;
        public float AttackRange = 7.0f;
        public string Description = "";
        public bool IsBachVan;

        // Faction-Themed Crafting & Combat Archetypes
        public string Faction = "Base"; // "Alien", "Bandit", "BioMystic", "Base", "BachVan"
        public int Tier = 1;
        public string AttackType = "Direct"; // "LaserBeam", "ParabolicMortar", "GravitonTractor", "EMPShockwave", "AntimatterMegaBeam", "RapidBallistic", "ParabolicSaw", "ConeFlamethrower", "HarpoonPull", "RecoilHowitzer", "RadialSpines", "ToxicSporeCloud", "CorrosiveAcidGlob", "LeechSwarm", "AncientGroundSpikes"
        public Dictionary<string, int> CraftingMaterials = new Dictionary<string, int>();
        public float SpecialValue = 0f;
        public int AmmoCapacity = 50;
        public string AmmoName = "Đạn";
        public float CooldownSeconds = 1.2f;
        public int MaxHp = 1500;
        public int Durability = 100;
    }

    [Serializable]
    public class DefenseBuildingInstance
    {
        public string InstanceId;
        public string BlueprintId;
        public string Name;
        public BuildingCategory Category;
        public bool IsBachVan;
        public int Durability = 100;
        public int MaxDurability = 100;
        public int MaxHp = 1500;
        public int CurrentHp = 1500;
        public bool IsBroken;
        public int AmmoCurrent;
        public int AmmoCapacity;
        public string AmmoName;
        public int AmmoCostPerShot;
        public float CooldownSeconds;
        public float CurrentCooldownRemaining;
        public string FireMode = "TuDo";
        public float Damage = 40f;
        public float AttackRange = 7.0f;
        public float WorldX;
        public float WorldY;

        // Archetype Properties
        public string Faction = "Base";
        public int Tier = 1;
        public string AttackType = "Direct";
        public float SpecialValue = 0f;
    }

    [Serializable]
    public class DefenseManager
    {
        public int HallLevel { get; set; } = 1;
        public List<string> UnlockedBlueprints { get; set; } = new List<string> { "NoXuyenVan", "BayChongGo", "ThapCanh", "RaoGo" };
        public List<DefenseBuildingInstance> Buildings { get; set; } = new List<DefenseBuildingInstance>();
        public Dictionary<string, int> MonsterMaterials { get; set; } = new Dictionary<string, int>();

        private readonly Dictionary<string, DefenseBlueprint> blueprintRegistry = new Dictionary<string, DefenseBlueprint>();

        public DefenseManager()
        {
            InitBlueprints();
        }

        public void AddMonsterMaterial(string materialId, int count)
        {
            if (string.IsNullOrEmpty(materialId) || count <= 0) return;
            if (!MonsterMaterials.ContainsKey(materialId))
            {
                MonsterMaterials[materialId] = 0;
            }
            MonsterMaterials[materialId] += count;
        }

        public int GetMonsterMaterialCount(string materialId)
        {
            if (string.IsNullOrEmpty(materialId)) return 0;
            return MonsterMaterials.TryGetValue(materialId, out var count) ? count : 0;
        }

        public bool UnlockBlueprint(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return false;
            if (!UnlockedBlueprints.Contains(blueprintId))
            {
                UnlockedBlueprints.Add(blueprintId);
                return true;
            }
            return false;
        }

        public bool CanCraftBlueprint(string blueprintId)
        {
            if (!blueprintRegistry.TryGetValue(blueprintId, out var bp)) return false;
            if (!UnlockedBlueprints.Contains(blueprintId)) return false;
            if (HallLevel < bp.RequiredHallLevel) return false;

            if (bp.CraftingMaterials != null)
            {
                foreach (var req in bp.CraftingMaterials)
                {
                    if (GetMonsterMaterialCount(req.Key) < req.Value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void InitBlueprints()
        {
            // ==========================================
            // 0. CÁC CÔNG TRÌNH CƠ BẢN & BẠCH VÂN (GDD v6.0)
            // ==========================================
            blueprintRegistry["NoXuyenVan"] = new DefenseBlueprint
            {
                Id = "NoXuyenVan",
                Name = "Nỏ Xuyên Vân",
                Category = BuildingCategory.TanCong,
                Rarity = "Tinh",
                RequiredHallLevel = 1,
                BuildCostGold = 2400,
                BuildCostWood = 20,
                Damage = 40f,
                AttackRange = 7.0f,
                AttackType = "Direct",
                Faction = "Base",
                Tier = 1,
                AmmoCapacity = 50,
                AmmoName = "Tên Thép",
                CooldownSeconds = 1.2f,
                MaxHp = 1500,
                Description = "Tháp nỏ liên châu bắn tỉa tự động 22m, tiêu diệt quái vật từ xa.",
                IsBachVan = false
            };

            blueprintRegistry["BayChongGo"] = new DefenseBlueprint
            {
                Id = "BayChongGo",
                Name = "Bẫy Chông Gỗ",
                Category = BuildingCategory.Bay,
                Rarity = "Thuong",
                RequiredHallLevel = 1,
                BuildCostGold = 150,
                BuildCostWood = 4,
                Damage = 60f,
                AttackRange = 1.0f,
                AttackType = "Direct",
                Faction = "Base",
                Tier = 1,
                AmmoCapacity = 10,
                AmmoName = "Cọc Chông",
                CooldownSeconds = 2.5f,
                MaxHp = 120,
                Durability = 10,
                Description = "Bẫy ngầm đất. Quái giẫm lên sẽ bung chông gây 60 sát thương và làm chậm 50%.",
                IsBachVan = false
            };

            blueprintRegistry["CuuTieuLoiPhao"] = new DefenseBlueprint
            {
                Id = "CuuTieuLoiPhao",
                Name = "Bạch Vân · Cửu Tiêu Lôi Pháo",
                Category = BuildingCategory.TanCong,
                Rarity = "BachVan",
                RequiredHallLevel = 3,
                BuildCostGold = 18000,
                BuildCostWood = 50,
                Damage = 250f,
                AttackRange = 99f,
                AttackType = "BachVanLightning",
                Faction = "BachVan",
                Tier = 4,
                AmmoCapacity = 120,
                AmmoName = "Đơn Vị Cộng Hưởng",
                CooldownSeconds = 75f,
                MaxHp = 5000,
                Description = "Thần khí Bạch Vân giáng sấm sét hủy diệt toàn bộ quái vật trên bản đồ khi kích hoạt.",
                IsBachVan = true
            };

            blueprintRegistry["ThienNhanVongDai"] = new DefenseBlueprint
            {
                Id = "ThienNhanVongDai",
                Name = "Bạch Vân · Thiên Nhãn Vọng Đài",
                Category = BuildingCategory.ThongTin_TrinhTham,
                Rarity = "BachVan",
                RequiredHallLevel = 3,
                BuildCostGold = 12500,
                BuildCostWood = 40,
                Damage = 0f,
                AttackRange = 99f,
                AttackType = "BachVanVision",
                Faction = "BachVan",
                Tier = 3,
                AmmoCapacity = 30,
                AmmoName = "Dầu Estropic",
                CooldownSeconds = 150f,
                MaxHp = 4000,
                Description = "Đài quan sát trinh sát trước thời điểm và quân số các đợt quái đột kích.",
                IsBachVan = true
            };

            blueprintRegistry["VanLinhHoiNguyenTran"] = new DefenseBlueprint
            {
                Id = "VanLinhHoiNguyenTran",
                Name = "Bạch Vân · Vạn Linh Hồi Nguyên Trận",
                Category = BuildingCategory.HoTro,
                Rarity = "BachVan",
                RequiredHallLevel = 3,
                BuildCostGold = 16000,
                BuildCostWood = 45,
                Damage = 0f,
                AttackRange = 99f,
                AttackType = "BachVanHealing",
                Faction = "BachVan",
                Tier = 3,
                AmmoCapacity = 20,
                AmmoName = "Tinh Thạch Bạch Vân",
                CooldownSeconds = 180f,
                MaxHp = 4500,
                Description = "Đại trận hồi phục sinh lực và giải độc tức thì cho toàn bộ đàn heo trang trại.",
                IsBachVan = true
            };

            // ==========================================
            // NHÓM 1: 5 THÁP CÔNG NGHỆ NGOÀI HÀNH TINH (ALIEN TECH)
            // ==========================================
            blueprintRegistry["TruPlasmaHoiTu"] = new DefenseBlueprint
            {
                Id = "TruPlasmaHoiTu",
                Name = "Trụ Tia Plasma Hội Tụ",
                Category = BuildingCategory.TanCong,
                Rarity = "Tinh",
                Faction = "Alien",
                Tier = 1,
                RequiredHallLevel = 1,
                BuildCostGold = 800,
                CraftingMaterials = new Dictionary<string, int> { { "item_alloy_scrap", 3 }, { "item_quantum_fiber", 1 } },
                Damage = 40f,
                AttackRange = 7.0f,
                AttackType = "LaserBeam",
                SpecialValue = 90f, // Max ramp-up DPS
                AmmoCapacity = 60,
                AmmoName = "Lõi Plasma Nén",
                CooldownSeconds = 0.25f,
                MaxHp = 1400,
                Description = "Chiếu tia Plasma xanh lam liên tục. Sát thương tăng dần từ 30 lên tối đa 90 DPS sau 3 giây."
            };

            blueprintRegistry["PhaoCoiLuongTu"] = new DefenseBlueprint
            {
                Id = "PhaoCoiLuongTu",
                Name = "Pháo Cối Phân Rã Lượng Tử",
                Category = BuildingCategory.TanCong,
                Rarity = "Tinh",
                Faction = "Alien",
                Tier = 2,
                RequiredHallLevel = 2,
                BuildCostGold = 1800,
                CraftingMaterials = new Dictionary<string, int> { { "item_alloy_scrap", 5 }, { "item_plasma_core", 2 }, { "item_quantum_fiber", 1 } },
                Damage = 140f,
                AttackRange = 15.0f,
                AttackType = "ParabolicMortar",
                SpecialValue = 0.25f, // +25% vulnerability
                AmmoCapacity = 30,
                AmmoName = "Đạn Cối Phân Rã",
                CooldownSeconds = 3.5f,
                MaxHp = 1800,
                Description = "Bắn đạn cối theo quỹ đạo vòng cung Parabol, nổ AoE 3m gây 140 sát thương và làm quái dính thêm 25% sát thương."
            };

            blueprintRegistry["ThapPhanTrongLuc"] = new DefenseBlueprint
            {
                Id = "ThapPhanTrongLuc",
                Name = "Tháp Phát Phản Trọng Lực",
                Category = BuildingCategory.HoTro,
                Rarity = "CaoCap",
                Faction = "Alien",
                Tier = 3,
                RequiredHallLevel = 3,
                BuildCostGold = 3200,
                CraftingMaterials = new Dictionary<string, int> { { "item_quantum_fiber", 4 }, { "item_plasma_core", 3 }, { "item_alloy_scrap", 2 } },
                Damage = 0f,
                AttackRange = 9.0f,
                AttackType = "GravitonTractor",
                SpecialValue = 0.70f, // 70% Slow
                AmmoCapacity = 40,
                AmmoName = "Tụ Điện Trọng Lực",
                CooldownSeconds = 1.0f,
                MaxHp = 2200,
                Description = "Chùm Tractor Beam nghịch đảo làm chậm 70% quái vật và lập tức cắt đứt dây bắt trộm giải cứu đàn heo."
            };

            blueprintRegistry["TruXungDienEMP"] = new DefenseBlueprint
            {
                Id = "TruXungDienEMP",
                Name = "Trụ Xung Điện EMP Phòng Không",
                Category = BuildingCategory.TanCong,
                Rarity = "CaoCap",
                Faction = "Alien",
                Tier = 3,
                RequiredHallLevel = 3,
                BuildCostGold = 4500,
                CraftingMaterials = new Dictionary<string, int> { { "item_plasma_core", 4 }, { "item_quantum_fiber", 3 }, { "item_quantum_chip", 1 } },
                Damage = 220f,
                AttackRange = 12.0f,
                AttackType = "EMPShockwave",
                SpecialValue = 2.5f, // 2.5s Stun
                AmmoCapacity = 25,
                AmmoName = "Lôi Cầu EMP",
                CooldownSeconds = 4.0f,
                MaxHp = 2500,
                Description = "Phóng quả cầu EMP nổ sóng xung kích 5m, phá 100% Khiên Plasma và làm choáng rơi UFO trong 2.5 giây."
            };

            blueprintRegistry["PhaoDaiPhanVatChat"] = new DefenseBlueprint
            {
                Id = "PhaoDaiPhanVatChat",
                Name = "Pháo Đài Phản Vật Chất Khổng Lồ",
                Category = BuildingCategory.TanCong,
                Rarity = "CucPham",
                Faction = "Alien",
                Tier = 4,
                RequiredHallLevel = 3,
                BuildCostGold = 12000,
                CraftingMaterials = new Dictionary<string, int> { { "item_quantum_chip", 4 }, { "item_plasma_core", 8 }, { "item_alloy_scrap", 10 } },
                Damage = 1200f,
                AttackRange = 30.0f,
                AttackType = "AntimatterMegaBeam",
                SpecialValue = 1200f,
                AmmoCapacity = 10,
                AmmoName = "Khối Hạt Phản Vật Chất",
                CooldownSeconds = 8.0f,
                MaxHp = 6000,
                Description = "Đại pháo tối thượng tụ năng lượng rồi quét tia Mega Beam xuyên tuyến toàn bản đồ, hủy diệt mọi quái vật cản đường."
            };

            // ==========================================
            // NHÓM 2: 5 THÁP TẠP PHẾ BĂNG CƯỚP (BANDIT SCRAP)
            // ==========================================
            blueprintRegistry["NoLienChauThep"] = new DefenseBlueprint
            {
                Id = "NoLienChauThep",
                Name = "Nỏ Liên Châu Bọc Thép",
                Category = BuildingCategory.TanCong,
                Rarity = "Thuong",
                Faction = "Bandit",
                Tier = 1,
                RequiredHallLevel = 1,
                BuildCostGold = 600,
                CraftingMaterials = new Dictionary<string, int> { { "item_wood_plank", 6 }, { "item_rusty_nail", 12 }, { "item_rope", 2 } },
                Damage = 18f,
                AttackRange = 8.0f,
                AttackType = "RapidBallistic",
                SpecialValue = 3f, // 3 shots/burst
                AmmoCapacity = 90,
                AmmoName = "Tên Thép Nhỏ",
                CooldownSeconds = 0.6f,
                MaxHp = 1300,
                Description = "Tang trống nỏ xoay cơ học xả 3 mũi tên liên thanh tốc độ cao, quét sạch bầy lâu la đột kích."
            };

            blueprintRegistry["MayBanRiuCua"] = new DefenseBlueprint
            {
                Id = "MayBanRiuCua",
                Name = "Máy Bắn Rìu Cưa Gai Vụn",
                Category = BuildingCategory.TanCong,
                Rarity = "Tinh",
                Faction = "Bandit",
                Tier = 2,
                RequiredHallLevel = 2,
                BuildCostGold = 1200,
                CraftingMaterials = new Dictionary<string, int> { { "item_wood_plank", 8 }, { "item_broken_post", 4 }, { "item_rusty_nail", 16 } },
                Damage = 95f,
                AttackRange = 11.0f,
                AttackType = "ParabolicSaw",
                SpecialValue = 30f, // Bleed DoT
                AmmoCapacity = 35,
                AmmoName = "Lưỡi Cưa Gai Rỉ",
                CooldownSeconds = 2.8f,
                MaxHp = 1600,
                Description = "Bắn lưỡi cưa sắt xoay vèo hình vòng cung, cắm xuống đất nổ văng 8 mảnh đinh gây chảy máu cho quái xung quanh."
            };

            blueprintRegistry["ThapPhunLuaHacIn"] = new DefenseBlueprint
            {
                Id = "ThapPhunLuaHacIn",
                Name = "Tháp Phun Lửa Hắc Ín",
                Category = BuildingCategory.TanCong,
                Rarity = "Tinh",
                Faction = "Bandit",
                Tier = 2,
                RequiredHallLevel = 2,
                BuildCostGold = 1600,
                CraftingMaterials = new Dictionary<string, int> { { "item_broken_post", 4 }, { "item_rusty_nail", 20 }, { "item_rope", 3 } },
                Damage = 65f,
                AttackRange = 5.0f,
                AttackType = "ConeFlamethrower",
                SpecialValue = 4.0f, // Burn 4s
                AmmoCapacity = 80,
                AmmoName = "Bình Hắc Ín Cháy",
                CooldownSeconds = 1.2f,
                MaxHp = 1900,
                Description = "Phun luồng lửa hình nón thiêu đốt mặt đất dài 4 ô gạch, thiêu đốt quái 4 giây. Quái dính hắc ín chịu gấp đôi sát thương."
            };

            blueprintRegistry["PhaoLaoMocXich"] = new DefenseBlueprint
            {
                Id = "PhaoLaoMocXich",
                Name = "Pháo Lao Móc Xích Quỷ",
                Category = BuildingCategory.TanCong,
                Rarity = "CaoCap",
                Faction = "Bandit",
                Tier = 3,
                RequiredHallLevel = 3,
                BuildCostGold = 3500,
                CraftingMaterials = new Dictionary<string, int> { { "item_broken_post", 8 }, { "item_rope", 6 }, { "item_rusty_nail", 30 }, { "item_gold_pouch", 1 } },
                Damage = 260f,
                AttackRange = 10.0f,
                AttackType = "HarpoonPull",
                SpecialValue = 3.0f, // Pull 3m
                AmmoCapacity = 25,
                AmmoName = "Lao Thép Móc Xích",
                CooldownSeconds = 3.2f,
                MaxHp = 2600,
                Description = "Bắn mũi lao sắt khổng lồ cắm vào quái vật, giật lùi quái lại 3 mét và trói cứng chân trong 2 giây."
            };

            blueprintRegistry["DaiBacChuaTrom"] = new DefenseBlueprint
            {
                Id = "DaiBacChuaTrom",
                Name = "Đại Bác Công Thành Chúa Trộm",
                Category = BuildingCategory.TanCong,
                Rarity = "CucPham",
                Faction = "Bandit",
                Tier = 4,
                RequiredHallLevel = 3,
                BuildCostGold = 8500,
                CraftingMaterials = new Dictionary<string, int> { { "item_broken_post", 12 }, { "item_rope", 10 }, { "item_rusty_nail", 50 }, { "item_gold_pouch", 3 } },
                Damage = 850f,
                AttackRange = 16.0f,
                AttackType = "RecoilHowitzer",
                SpecialValue = 1.5f, // Knockback + Stun
                AmmoCapacity = 15,
                AmmoName = "Pháo Sắt Đúc Đặc",
                CooldownSeconds = 5.0f,
                MaxHp = 4500,
                Description = "Khẩu đại bác nòng lớn giật mạnh tóe khói đen, phóng đạn đại bác nổ rung chuyển màn hình, hất tung và gây choáng toàn bộ quái vật."
            };

            // ==========================================
            // NHÓM 3: 5 THÁP DỊCH HỌA & HUYỀN BÍ (BIO & MYSTIC)
            // ==========================================
            blueprintRegistry["UGaiDocGamNhom"] = new DefenseBlueprint
            {
                Id = "UGaiDocGamNhom",
                Name = "Ụ Gai Độc Gặm Nhấm",
                Category = BuildingCategory.Bay,
                Rarity = "Thuong",
                Faction = "BioMystic",
                Tier = 1,
                RequiredHallLevel = 1,
                BuildCostGold = 500,
                CraftingMaterials = new Dictionary<string, int> { { "item_rat_tail", 4 }, { "item_fertilizer", 3 } },
                Damage = 40f,
                AttackRange = 4.0f,
                AttackType = "RadialSpines",
                SpecialValue = 12f, // 12 spines
                AmmoCapacity = 40,
                AmmoName = "Gai Độc Hữu Cơ",
                CooldownSeconds = 1.8f,
                MaxHp = 350,
                Durability = 40,
                Description = "Tự động bung kén phóng 12 mũi gai hữu cơ màu tím tỏa đều 360 độ khi có quái lại gần trong 4m."
            };

            blueprintRegistry["TruNamBaoTuMu"] = new DefenseBlueprint
            {
                Id = "TruNamBaoTuMu",
                Name = "Trụ Nấm Phun Bào Tử Mù",
                Category = BuildingCategory.Bay,
                Rarity = "Tinh",
                Faction = "BioMystic",
                Tier = 2,
                RequiredHallLevel = 2,
                BuildCostGold = 1400,
                CraftingMaterials = new Dictionary<string, int> { { "item_rat_tail", 6 }, { "item_fertilizer", 4 }, { "item_mutant_corn", 2 } },
                Damage = 45f,
                AttackRange = 6.0f,
                AttackType = "ToxicSporeCloud",
                SpecialValue = 6.0f, // 6s duration
                AmmoCapacity = 30,
                AmmoName = "Bào Tử Nấm Độc",
                CooldownSeconds = 4.5f,
                MaxHp = 600,
                Durability = 30,
                Description = "Phun đám mây khói xanh rêu mù mịt, làm quái mất mục tiêu đi lảo đảo và dính 45 DPS bỏ qua 50% giáp."
            };

            blueprintRegistry["OngBomDichAxit"] = new DefenseBlueprint
            {
                Id = "OngBomDichAxit",
                Name = "Ống Bơm Dịch Nhầy Axit",
                Category = BuildingCategory.TanCong,
                Rarity = "Tinh",
                Faction = "BioMystic",
                Tier = 2,
                RequiredHallLevel = 2,
                BuildCostGold = 1800,
                CraftingMaterials = new Dictionary<string, int> { { "item_rat_tail", 5 }, { "item_mutant_corn", 4 } },
                Damage = 80f,
                AttackRange = 8.0f,
                AttackType = "CorrosiveAcidGlob",
                SpecialValue = 0.40f, // -40% armor shred
                AmmoCapacity = 35,
                AmmoName = "Bọc Axit Neon",
                CooldownSeconds = 2.4f,
                MaxHp = 1700,
                Description = "Bắn khối cầu dịch nhầy nảy 2 nhịp vỡ toang thành bãi axit, gây 80 sát thương và làm mòn 40% giáp của quái."
            };

            blueprintRegistry["CotToHutHon"] = new DefenseBlueprint
            {
                Id = "CotToHutHon",
                Name = "Cột Tổ Hút Hồn Tầm Huyết",
                Category = BuildingCategory.HoTro,
                Rarity = "CaoCap",
                Faction = "BioMystic",
                Tier = 3,
                RequiredHallLevel = 3,
                BuildCostGold = 3800,
                CraftingMaterials = new Dictionary<string, int> { { "item_mutant_corn", 6 }, { "item_rat_tail", 4 }, { "item_quantum_fiber", 3 } },
                Damage = 60f,
                AttackRange = 9.0f,
                AttackType = "LeechSwarm",
                SpecialValue = 0.50f, // 50% heal conversion
                AmmoCapacity = 50,
                AmmoName = "Bọ Hút Hồn",
                CooldownSeconds = 1.0f,
                MaxHp = 2400,
                Description = "Thả đàn bọ hút 60 máu/giây từ quái vật mạnh nhất, chuyển hóa 50% lượng máu hút được để sửa chữa hàng rào trại."
            };

            blueprintRegistry["CotDiCotSuongHuyen"] = new DefenseBlueprint
            {
                Id = "CotDiCotSuongHuyen",
                Name = "Cột Dị Cốt Sương Huyền",
                Category = BuildingCategory.TanCong,
                Rarity = "CucPham",
                Faction = "BioMystic",
                Tier = 4,
                RequiredHallLevel = 3,
                BuildCostGold = 9000,
                CraftingMaterials = new Dictionary<string, int> { { "item_mutant_corn", 10 }, { "item_rat_tail", 8 }, { "item_quantum_chip", 2 } },
                Damage = 750f,
                AttackRange = 14.0f,
                AttackType = "AncientGroundSpikes",
                SpecialValue = 4.0f, // Lock skills 4s
                AmmoCapacity = 15,
                AmmoName = "Hóa Thạch Cổ Sương",
                CooldownSeconds = 5.5f,
                MaxHp = 4800,
                Description = "Triệu hồi cọc xương cổ đại trồi từ lòng đất dưới chân quái, đâm xuyên gây 750 sát thương và khóa kỹ năng trong 4 giây."
            };
        }

        private static DefenseManager? _defaultManager;
        public static DefenseManager Default
        {
            get
            {
                if (_defaultManager == null)
                {
                    _defaultManager = new DefenseManager();
                }
                return _defaultManager;
            }
        }

        public static DefenseBlueprint GetRegisteredBlueprint(string blueprintId)
        {
            return Default.GetBlueprint(blueprintId);
        }

        public static DefenseBuildingInstance CreateDefaultInstance(string blueprintId, float worldX = 0f, float worldY = 0f)
        {
            var bp = GetRegisteredBlueprint(blueprintId);
            if (bp == null) return null;
            return new DefenseBuildingInstance
            {
                InstanceId = $"tower_{bp.Id}_{Guid.NewGuid():N}",
                BlueprintId = bp.Id,
                Name = bp.Name,
                Category = bp.Category,
                IsBachVan = bp.IsBachVan,
                Faction = bp.Faction,
                Tier = bp.Tier,
                AttackType = bp.AttackType,
                SpecialValue = bp.SpecialValue,
                Durability = bp.Durability,
                MaxDurability = bp.Durability,
                MaxHp = bp.MaxHp,
                CurrentHp = bp.MaxHp,
                AmmoCurrent = bp.AmmoCapacity,
                AmmoCapacity = bp.AmmoCapacity,
                AmmoName = bp.AmmoName,
                AmmoCostPerShot = bp.IsBachVan ? 50 : 1,
                CooldownSeconds = bp.CooldownSeconds,
                Damage = bp.Damage,
                AttackRange = bp.AttackRange,
                WorldX = worldX,
                WorldY = worldY
            };
        }

        public DefenseBlueprint GetBlueprint(string blueprintId)
        {
            if (blueprintRegistry.TryGetValue(blueprintId, out var bp)) return bp;
            return null;
        }

        public List<DefenseBlueprint> GetAllBlueprints()
        {
            return new List<DefenseBlueprint>(blueprintRegistry.Values);
        }

        public (bool success, string message, DefenseBuildingInstance building) ConstructBuilding(string blueprintId)
        {
            if (!blueprintRegistry.TryGetValue(blueprintId, out var bp))
                return (false, "Bản vẽ không tồn tại!", null);

            if (!UnlockedBlueprints.Contains(blueprintId))
                return (false, "Chưa học bản vẽ này!", null);

            if (HallLevel < bp.RequiredHallLevel)
                return (false, $"Phòng Xây Dựng cấp {HallLevel} không đủ (cần cấp {bp.RequiredHallLevel})!", null);

            if (bp.IsBachVan && Buildings.Exists(b => b.BlueprintId == blueprintId && !b.IsBroken))
                return (false, "Mỗi trại chỉ sở hữu tối đa 1 công trình mỗi loại Bạch Vân!", null);

            // Kiểm tra và trừ nguyên liệu quái vật
            if (bp.CraftingMaterials != null)
            {
                foreach (var req in bp.CraftingMaterials)
                {
                    if (GetMonsterMaterialCount(req.Key) < req.Value)
                    {
                        return (false, $"Thiếu nguyên liệu {req.Key}! Cần {req.Value} (hiện có {GetMonsterMaterialCount(req.Key)}).", null);
                    }
                }

                // Trừ nguyên liệu
                foreach (var req in bp.CraftingMaterials)
                {
                    MonsterMaterials[req.Key] -= req.Value;
                }
            }

            var newBldg = new DefenseBuildingInstance
            {
                InstanceId = $"bldg_{Guid.NewGuid():N}",
                BlueprintId = bp.Id,
                Name = bp.Name,
                Category = bp.Category,
                IsBachVan = bp.IsBachVan,
                Faction = bp.Faction,
                Tier = bp.Tier,
                AttackType = bp.AttackType,
                SpecialValue = bp.SpecialValue,
                Durability = bp.Durability,
                MaxDurability = bp.Durability,
                MaxHp = bp.MaxHp,
                CurrentHp = bp.MaxHp,
                AmmoCurrent = bp.AmmoCapacity,
                AmmoCapacity = bp.AmmoCapacity,
                AmmoName = bp.AmmoName,
                AmmoCostPerShot = bp.IsBachVan ? 50 : 1,
                CooldownSeconds = bp.CooldownSeconds,
                Damage = bp.Damage,
                AttackRange = bp.AttackRange
            };

            Buildings.Add(newBldg);
            return (true, $"Đã xây thành công {bp.Name}!", newBldg);
        }

        public (bool success, string message) RepairBuilding(string instanceId, int hpAmount)
        {
            var b = Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null) return (false, "Không tìm thấy công trình!");

            if (b.CurrentHp >= b.MaxHp)
                return (false, "Công trình đang còn nguyên vẹn 100%!");

            b.CurrentHp = Math.Min(b.MaxHp, b.CurrentHp + hpAmount);
            if (b.CurrentHp > 0) b.IsBroken = false;
            return (true, $"Đã sửa chữa {b.Name}! Máu hiện tại: {b.CurrentHp}/{b.MaxHp}");
        }

        public (bool success, string message) PreloadAmmo(string instanceId, int amount, bool isDuringWave)
        {
            if (isDuringWave)
                return (false, "Không được nạp đạn trong đợt quái!");

            var b = Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null) return (false, "Không tìm thấy công trình!");

            int space = b.AmmoCapacity - b.AmmoCurrent;
            int add = Math.Min(space, amount);
            b.AmmoCurrent += add;
            return (true, $"Đã nạp {add} {b.AmmoName}. Hiện có {b.AmmoCurrent}/{b.AmmoCapacity}.");
        }

        public (bool success, string message) TriggerBachVanManual(string instanceId)
        {
            var b = Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null || !b.IsBachVan)
                return (false, "Chỉ công trình Bạch Vân mới được kích hoạt thủ công!");

            if (b.AmmoCurrent < b.AmmoCostPerShot)
                return (false, $"{b.Name} không đủ nhiên liệu/đạn!");

            if (b.CurrentCooldownRemaining > 0f)
                return (false, $"{b.Name} đang trong thời gian hồi chiêu!");

            b.AmmoCurrent -= b.AmmoCostPerShot;
            b.Durability = Math.Max(0, b.Durability - 22);
            b.CurrentCooldownRemaining = b.CooldownSeconds;

            return (true, $"ĐÃ KÍCH HOẠT THỦ CÔNG {b.Name.ToUpper()}!");
        }
    }
}
