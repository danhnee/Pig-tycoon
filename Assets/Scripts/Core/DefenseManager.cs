using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum TowerFireMode
    {
        TuDo = 0,        // Tự do: bắn mọi mục tiêu trong tầm
        TietKiem = 1,    // Tiết kiệm: chỉ bắn khi mục tiêu vào 60% tầm hoặc nhóm >= 3 con (-35% hao đạn)
        ChiTinhAnh = 2   // Chỉ tinh anh: chỉ bắn Quái Tinh Anh, UFO, hoặc Boss
    }

    [Serializable]
    public class DefenseBlueprint
    {
        public string Id = "";
        public string CatalogId = "";
        public string Name = "";
        public BuildingCategory Category;
        public string Rarity = "Thuong";
        public int RequiredHallLevel = 1;
        public int BuildCostGold = 100;
        public int PowerUnitsContinuous = 0;
        public int PowerUnitsPeak = 0;
        public bool IsBachVan = false;
        public List<string> AvailableUpgradeBranches = new List<string>();
    }

    [Serializable]
    public class DefenseBuildingInstance
    {
        public string InstanceId = "";
        public string BlueprintId = "";
        public string Name = "";
        public BuildingCategory Category;
        public bool IsBachVan;
        public int Durability = 100;
        public int MaxHp = 1500;
        public int CurrentHp = 1500;
        public bool IsBroken;
        public int AmmoCurrent;
        public int AmmoCapacity;
        public string AmmoName = "";
        public int AmmoCostPerShot;
        public float CooldownSeconds;
        public float CurrentCooldownRemaining;
        public string FireMode = "TuDo";
        public int UpgradeLevel = 1; // 1..3
        public string ChosenUpgradeBranch = ""; // 1A/1B/1C, 2A/2B/2C, 3A/3B/3C, 4A/4B/4C
        public string ConnectedWireId = "";
        public bool IsPowered = true;
    }

    [Serializable]
    public class DefenseManager
    {
        public int HallLevel { get; set; } = 1;
        public List<string> UnlockedBlueprints { get; set; } = new List<string>
        {
            "NoXuyenVan", "ThapCanh", "RaoGo", "CongGiaCo", "UBaoCat", "BanChong",
            "DenPhaCongNghiep", "BomLamMat", "ChotCanhGac", "CoTapKet", "TramHoiSuc", "MayPhatDien"
        };
        public List<DefenseBuildingInstance> Buildings { get; set; } = new List<DefenseBuildingInstance>();
        private readonly Dictionary<string, DefenseBlueprint> blueprintRegistry = new Dictionary<string, DefenseBlueprint>();

        public IReadOnlyDictionary<string, DefenseBlueprint> BlueprintRegistry => blueprintRegistry;

        public DefenseManager()
        {
            InitBlueprints();
        }

        private void RegisterBlueprint(
            string id,
            string catalogId,
            string name,
            BuildingCategory category,
            string rarity,
            int hallLevel,
            int goldCost,
            int powerCont = 0,
            int powerPeak = 0,
            bool isBachVan = false,
            string[]? branches = null)
        {
            var bp = new DefenseBlueprint
            {
                Id = id,
                CatalogId = catalogId,
                Name = name,
                Category = category,
                Rarity = rarity,
                RequiredHallLevel = hallLevel,
                BuildCostGold = goldCost,
                PowerUnitsContinuous = powerCont,
                PowerUnitsPeak = powerPeak,
                IsBachVan = isBachVan
            };
            if (branches != null)
            {
                bp.AvailableUpgradeBranches.AddRange(branches);
            }
            blueprintRegistry[id] = bp;
        }

        private void InitBlueprints()
        {
            // 1. Nhóm Thông tin — Trinh thám đợt quái (4 công trình, GDD 14.1)
            RegisterBlueprint("ThapCanh", "pig.building.thap_canh_quan_sat", "Tháp Canh Quan Sát", BuildingCategory.ThongTin_TrinhTham, "Thuong", 1, 650, 0, 0);
            RegisterBlueprint("RadarUyHiep", "pig.building.radar_uy_hiep", "Radar Uy Hiếp", BuildingCategory.ThongTin_TrinhTham, "Kha", 2, 1450, 6, 8);
            RegisterBlueprint("MayCongHuongHuyenBi", "pig.building.may_cong_huong_huyen_bi", "Máy Cộng Hưởng Huyền Bí", BuildingCategory.ThongTin_TrinhTham, "Hiem", 2, 2200, 8, 12);
            RegisterBlueprint("ThienNhanVongDai", "pig.building.thien_nhan_vong_dai", "Bạch Vân · Thiên Nhãn Vọng Đài", BuildingCategory.ThongTin_TrinhTham, "BachVan", 3, 12500, 12, 20, isBachVan: true);

            // 2. Nhóm Thông tin — Hàng ngày (4 công trình, GDD 14.2)
            RegisterBlueprint("TramKhiTuong", "pig.building.tram_khi_tuong", "Trạm Khí Tượng", BuildingCategory.ThongTin_HangNgay, "Thuong", 1, 580, 2, 2);
            RegisterBlueprint("TramTiepSongCho", "pig.building.tram_tiep_song_cho", "Trạm Tiếp Sóng Chợ", BuildingCategory.ThongTin_HangNgay, "Kha", 1, 720, 3, 4);
            RegisterBlueprint("BanTinhBao", "pig.building.ban_tinh_bao", "Bàn Tình Báo", BuildingCategory.ThongTin_HangNgay, "Kha", 2, 950, 2, 2);
            RegisterBlueprint("DaiDoDiTuong", "pig.building.dai_do_di_tuong", "Đài Dò Dị Tượng", BuildingCategory.ThongTin_HangNgay, "Hiem", 2, 1600, 6, 10);

            // 3. Nhóm Hỗ trợ (6 công trình, GDD 15)
            RegisterBlueprint("TramHoiSuc", "pig.building.tram_hoi_suc", "Trạm Hồi Sức", BuildingCategory.HoTro, "Thuong", 1, 620, 4, 6);
            RegisterBlueprint("DenPhaCongNghiep", "pig.building.den_pha_cong_nghiep", "Đèn Pha Công Nghiệp", BuildingCategory.HoTro, "Kha", 1, 480, 4, 5);
            RegisterBlueprint("MayPhatDien", "pig.building.may_phat_dien", "Máy Phát Điện", BuildingCategory.HoTro, "Thuong", 1, 850, 0, 0);
            RegisterBlueprint("BomLamMat", "pig.building.bom_lam_mat", "Bơm Làm Mát", BuildingCategory.HoTro, "Kha", 1, 520, 5, 6);
            RegisterBlueprint("CotThuLoi", "pig.building.cot_thu_loi", "Cột Thu Lôi", BuildingCategory.HoTro, "Kha", 2, 780, 0, 0);
            RegisterBlueprint("VanLinhHoiNguyenTran", "pig.building.van_linh_hoi_nguyen_tran", "Bạch Vân · Vạn Linh Hồi Nguyên Trận", BuildingCategory.HoTro, "BachVan", 3, 16000, 15, 25, isBachVan: true);

            // 4. Nhóm Tấn công (5 công trình, GDD 16 + 3 nhánh nâng cấp GDD 16.1)
            RegisterBlueprint("NoXuyenVan", "pig.building.no_xuyen_van", "Nỏ Xuyên Vân", BuildingCategory.TanCong, "Tinh", 1, 2400, 4, 6, false,
                new[] { "1A_TocBan", "1B_XuyenHang", "1C_TenDocLua" });
            RegisterBlueprint("ThapHoQuang", "pig.building.thap_ho_quang", "Tháp Hồ Quang", BuildingCategory.TanCong, "Kha", 2, 3200, 10, 18, false,
                new[] { "2A_NhayChuoi", "2B_TeLiet", "2C_QuaTaiHoQuang" });
            RegisterBlueprint("ThapPhongKhongSkyhook", "pig.building.thap_phong_khong_skyhook", "Tháp Phòng Không Skyhook", BuildingCategory.TanCong, "Hiem", 2, 3800, 12, 22, false,
                new[] { "3A_CatTiaKeo", "3B_LuoiPhaKhien", "3C_TenLuaDanhChan" });
            RegisterBlueprint("DaiThienLoi", "pig.building.dai_thien_loi", "Đài Thiên Lôi", BuildingCategory.TanCong, "Quy", 3, 5400, 16, 30, false,
                new[] { "4A_SamRenDienRong", "4B_CotSetTrongDiem", "4C_CongHuongGiongBao" });
            RegisterBlueprint("CuuTieuLoiPhao", "pig.building.cuu_tieu_loi_phao", "Bạch Vân · Cửu Tiêu Lôi Pháo", BuildingCategory.TanCong, "BachVan", 3, 18000, 20, 45, isBachVan: true);

            // 5. Nhóm Phòng thủ (4 công trình chuẩn + biến thể Rào Gỗ / Rào Thép, GDD 17)
            RegisterBlueprint("RaoGo", "pig.building.hang_rao", "Hàng Rào Gỗ", BuildingCategory.PhongThu, "Thuong", 1, 60, 0, 0);
            RegisterBlueprint("RaoThep", "pig.building.hang_rao", "Hàng Rào Thép", BuildingCategory.PhongThu, "Kha", 2, 180, 0, 0);
            RegisterBlueprint("CongGiaCo", "pig.building.cong_gia_co", "Cổng Gia Cố", BuildingCategory.PhongThu, "Kha", 1, 320, 0, 0);
            RegisterBlueprint("MayChieuKhien", "pig.building.may_chieu_khien", "Máy Chiếu Khiên", BuildingCategory.PhongThu, "Hiem", 2, 2100, 10, 16);
            RegisterBlueprint("UBaoCat", "pig.building.u_bao_cat", "Ụ Bao Cát", BuildingCategory.PhongThu, "Thuong", 1, 140, 0, 0);

            // 6. Nhóm Bẫy (5 công trình, GDD 17)
            RegisterBlueprint("BanChong", "pig.building.ban_chong", "Bàn Chông", BuildingCategory.Bay, "Thuong", 1, 160, 0, 0);
            RegisterBlueprint("TamSocDien", "pig.building.tam_soc_dien", "Tấm Sốc Điện", BuildingCategory.Bay, "Kha", 2, 340, 4, 8);
            RegisterBlueprint("HoKeo", "pig.building.ho_keo", "Hố Keo", BuildingCategory.Bay, "Thuong", 1, 200, 0, 0);
            RegisterBlueprint("LuoiEmp", "pig.building.luoi_emp", "Lưới EMP", BuildingCategory.Bay, "Hiem", 2, 520, 6, 12);
            RegisterBlueprint("MinNo", "pig.building.min_no", "Mìn Nổ", BuildingCategory.Bay, "Kha", 2, 380, 0, 0);

            // 7. Nhóm Triển khai (3 công trình, GDD 17)
            RegisterBlueprint("ChotCanhGac", "pig.building.chot_canh_gac", "Chốt Canh Gác", BuildingCategory.TrienKhai, "Thuong", 1, 350, 0, 0);
            RegisterBlueprint("CoTapKet", "pig.building.co_tap_ket", "Cờ Tập Kết", BuildingCategory.TrienKhai, "Thuong", 1, 280, 0, 0);
            RegisterBlueprint("DenHieuDanhChan", "pig.building.den_hieu_danh_chan", "Đèn Hiệu Đánh Chặn", BuildingCategory.TrienKhai, "Kha", 2, 640, 4, 6);
            // Đồng bộ thông số chuẩn xác với Bảng 1.5-D GDD v6.0 (Single Source of Truth)
            foreach (var spec in CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D)
            {
                if (blueprintRegistry.TryGetValue(spec.BlueprintId, out var existing))
                {
                    existing.BuildCostGold = spec.BuildCostGold;
                    existing.PowerUnitsContinuous = spec.PowerContinuous;
                    existing.PowerUnitsPeak = spec.PowerPeak;
                    existing.IsBachVan = spec.IsBachVan;
                }
            }
        }

        /// <summary>
        /// Đếm tổng số định danh công trình duy nhất theo GDD 14–17 (31 công trình chuẩn khi gộp Hàng Rào Gỗ/Thép).
        /// </summary>
        public int GetUniqueGddBuildingCatalogCount()
        {
            var uniqueCatalogIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in blueprintRegistry)
            {
                uniqueCatalogIds.Add(kv.Value.CatalogId);
            }
            return uniqueCatalogIds.Count;
        }

        /// <summary>
        /// Trần tổng công trình phòng thủ theo Cấp Phòng Xây Dựng (GDD 13.2: Cấp 1: 6 • Cấp 2: 14 • Cấp 3: 24 • Cấp 4: 32).
        /// </summary>
        public int GetMaxActiveTowersByHallLevel() => HallLevel switch
        {
            1 => 6,
            2 => 14,
            3 => 24,
            _ => 32
        };

        public (bool success, string message, DefenseBuildingInstance? building) ConstructBuilding(string blueprintId)
        {
            if (!blueprintRegistry.TryGetValue(blueprintId, out var bp))
                return (false, "Bản vẽ không tồn tại!", null);

            if (!UnlockedBlueprints.Contains(blueprintId))
                return (false, "Chưa học bản vẽ này!", null);

            if (HallLevel < bp.RequiredHallLevel)
                return (false, $"Phòng Xây Dựng cấp {HallLevel} không đủ (cần cấp {bp.RequiredHallLevel})!", null);

            if (bp.IsBachVan && Buildings.Exists(b => b.BlueprintId == blueprintId && !b.IsBroken))
                return (false, "Mỗi trại chỉ sở hữu tối đa 1 công trình mỗi loại Bạch Vân!", null);

            // Kiểm tra trần mỗi loại theo Bảng 1.5-D GDD v6.0
            for (int i = 0; i < CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D.Count; i++)
            {
                var spec = CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D[i];
                if (string.Equals(spec.BlueprintId, blueprintId, StringComparison.OrdinalIgnoreCase))
                {
                    int sameTypeCount = 0;
                    for (int j = 0; j < Buildings.Count; j++)
                    {
                        if (!Buildings[j].IsBroken && string.Equals(Buildings[j].BlueprintId, blueprintId, StringComparison.OrdinalIgnoreCase))
                            sameTypeCount++;
                    }
                    if (sameTypeCount >= spec.MaxPerFarm)
                        return (false, $"Đã đạt giới hạn tối đa {spec.MaxPerFarm} công trình [{bp.Name}] trên toàn trại (Bảng 1.5-D GDD)!", null);
                    break;
                }
            }

            // Kiểm tra trần tổng tháp theo Cấp Phòng Xây Dựng (GDD 13.2: 6 / 14 / 24 / 32) cho các công trình trinh thám/hỗ trợ/tấn công/triển khai
            if (HallLevel <= 3 && bp.Category != BuildingCategory.PhongThu && bp.Category != BuildingCategory.Bay)
            {
                int activeCount = 0;
                for (int i = 0; i < Buildings.Count; i++)
                {
                    var b = Buildings[i];
                    if (b != null && !b.IsBroken && b.Category != BuildingCategory.PhongThu && b.Category != BuildingCategory.Bay)
                        activeCount++;
                }
                int maxCap = GetMaxActiveTowersByHallLevel();
                if (activeCount >= maxCap)
                {
                    return (false, $"Phòng Xây Dựng cấp {HallLevel} chỉ cho phép vận hành tối đa {maxCap} công trình (GDD 13.2)!", null);
                }
            }

            var newBldg = new DefenseBuildingInstance
            {
                InstanceId = $"bldg_{Guid.NewGuid():N}",
                BlueprintId = bp.Id,
                Name = bp.Name,
                Category = bp.Category,
                IsBachVan = bp.IsBachVan,
                Durability = 100,
                MaxHp = bp.IsBachVan ? 5000 : 1500,
                CurrentHp = bp.IsBachVan ? 5000 : 1500,
                AmmoCurrent = bp.Id == "NoXuyenVan" ? 50 : (bp.IsBachVan ? 120 : 24),
                AmmoCapacity = bp.Id == "NoXuyenVan" ? 50 : (bp.IsBachVan ? 120 : 24),
                AmmoName = bp.Id == "NoXuyenVan" ? "Tên Thép" : "Đơn Vị Cộng Hưởng",
                AmmoCostPerShot = bp.Id == "NoXuyenVan" ? 1 : (bp.IsBachVan ? 50 : 1),
                CooldownSeconds = bp.Id == "NoXuyenVan" ? 1.2f : 75f,
                FireMode = bp.IsBachVan ? "ThuCong" : "TuDo"
            };

            Buildings.Add(newBldg);
            return (true, $"Đã xây thành công {bp.Name}!", newBldg);
        }

        /// <summary>
        /// Đổi chế độ bắn cho tháp tấn công tự động (Tự do / Tiết kiệm / Chỉ tinh anh - GDD 13.4).
        /// </summary>
        public (bool success, string message) SetTowerFireMode(string instanceId, TowerFireMode mode)
        {
            var b = Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null) return (false, "Không tìm thấy công trình.");
            if (b.IsBachVan) return (false, "Công trình Bạch Vân chỉ kích hoạt thủ công!");

            b.FireMode = mode switch
            {
                TowerFireMode.TietKiem => "TietKiem",
                TowerFireMode.ChiTinhAnh => "ChiTinhAnh",
                _ => "TuDo"
            };
            return (true, $"Đã chuyển chế độ bắn của [{b.Name}] sang [{b.FireMode}].");
        }

        /// <summary>
        /// Chọn nhánh nâng cấp chuyên sâu cho 4 tháp tấn công chủ lực (1A/1B/1C, 2A/2B/2C, 3A/3B/3C, 4A/4B/4C - GDD 16.1).
        /// </summary>
        public (bool success, string message) SelectAttackTowerUpgradeBranch(string instanceId, string branchCode)
        {
            var b = Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null) return (false, "Không tìm thấy công trình.");
            if (!blueprintRegistry.TryGetValue(b.BlueprintId, out var bp) || bp.AvailableUpgradeBranches.Count == 0)
                return (false, $"Công trình [{b.Name}] không có nhánh nâng cấp vũ khí.");

            string? matched = bp.AvailableUpgradeBranches.Find(x =>
                x.StartsWith(branchCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, branchCode, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(matched))
                return (false, $"Nhánh [{branchCode}] không hợp lệ cho [{b.Name}].");

            b.ChosenUpgradeBranch = matched!;
            b.UpgradeLevel = Math.Max(2, b.UpgradeLevel + 1);
            b.MaxHp += 400;
            b.CurrentHp = b.MaxHp;
            return (true, $"Đã nâng cấp [{b.Name}] lên Cấp {b.UpgradeLevel} theo nhánh [{b.ChosenUpgradeBranch}]!");
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

            string buildingName = string.IsNullOrWhiteSpace(b.Name)
                ? b.BlueprintId ?? "Công trình"
                : b.Name;

            if (b.AmmoCurrent < b.AmmoCostPerShot)
                return (false, $"{buildingName} không đủ nhiên liệu/đạn!");

            if (b.CurrentCooldownRemaining > 0f)
                return (false, $"{buildingName} đang trong thời gian hồi chiêu!");

            b.AmmoCurrent -= b.AmmoCostPerShot;
            b.Durability = Math.Max(0, b.Durability - 22);
            b.CurrentCooldownRemaining = b.CooldownSeconds;

            return (true, $"ĐÃ KÍCH HOẠT THỦ CÔNG {buildingName.ToUpper()}!");
        }

        // =====================================================================
        // TƯƠNG TÁC HỆ THỐNG THỜI GIAN THỰC: LƯỚI ĐIỆN <-> NHIÊN LIỆU <-> TÌNH BÁO <-> ÂM CHẤN
        // =====================================================================

        public int PendingThreatBudgetBonus { get; set; } = 0;
        public int LastWaveAcousticShockScore { get; set; } = 0;

        /// <summary>
        /// Kiểm tra trại có công trình thuộc blueprintId đang nguyên vẹn và được cấp điện hay không.
        /// </summary>
        public bool HasPoweredBuilding(string blueprintId)
        {
            for (int i = 0; i < Buildings.Count; i++)
            {
                var b = Buildings[i];
                if (b != null &&
                    !b.IsBroken &&
                    b.CurrentHp > 0 &&
                    b.Durability > 0 &&
                    string.Equals(b.BlueprintId, blueprintId, StringComparison.OrdinalIgnoreCase) &&
                    b.IsPowered)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Đồng bộ trực tiếp danh sách công trình Phòng thủ với Lưới Điện (PowerGridState) và Nhiên liệu (Farm.FuelLiters):
        /// - Tính tổng công suất phát từ Máy Phát Điện còn hoạt động (tiêu hao nhiên liệu khi tải cao).
        /// - Tính tổng phụ tải điện liên tục (hoặc điện đỉnh khi đang có đợt quái).
        /// - Nếu quá tải > 140% làm sập cầu dao hoặc đứt dây nổi -> ngắt nguồn điện (IsPowered = false) của các công trình dùng điện!
        /// </summary>
        public (float totalDemandUnits, int effectiveCapacityUnits, bool breakerTripped, int poweredBuildingsCount) SynchronizeWithPowerGrid(
            PowerGridState powerGrid,
            Farm farm,
            bool isDuringWave,
            bool consumeHourlyFuel)
        {
            if (powerGrid == null)
                return (0f, 0, false, 0);

            int activeGenerators = 0;
            for (int i = 0; i < Buildings.Count; i++)
            {
                var b = Buildings[i];
                if (b != null && !b.IsBroken && b.CurrentHp > 0 && b.BlueprintId == "MayPhatDien")
                {
                    activeGenerators++;
                }
            }

            if (activeGenerators > 0)
            {
                if (farm != null && consumeHourlyFuel)
                {
                    int fuelNeeded = Math.Max(1, (int)Math.Ceiling(activeGenerators * 0.5f));
                    if (farm.FuelLiters >= fuelNeeded)
                    {
                        farm.FuelLiters -= fuelNeeded;
                        powerGrid.GeneratorLevel = Math.Max(powerGrid.GeneratorLevel, Math.Min(3, 1 + activeGenerators));
                    }
                    else
                    {
                        farm.FuelLiters = 0;
                        powerGrid.GeneratorLevel = 1; // Hết xăng -> chỉ còn máy phát cơ bản
                    }
                }
                else
                {
                    powerGrid.GeneratorLevel = Math.Max(powerGrid.GeneratorLevel, Math.Min(3, 1 + activeGenerators));
                }
            }

            float demand = 0f;
            var poweredNodes = powerGrid.ComputePoweredNodesBfs();
            for (int i = 0; i < Buildings.Count; i++)
            {
                var b = Buildings[i];
                if (b == null || b.IsBroken || b.CurrentHp <= 0) continue;

                bool reachableFromGrid = powerGrid.IsBuildingPoweredByGrid(b, poweredNodes);
                if (!reachableFromGrid)
                {
                    b.IsPowered = false;
                    continue;
                }

                if (blueprintRegistry.TryGetValue(b.BlueprintId, out var bp))
                {
                    int watts = isDuringWave ? Math.Max(bp.PowerUnitsContinuous, bp.PowerUnitsPeak) : bp.PowerUnitsContinuous;
                    demand += watts;
                }
            }

            if (demand > 0f)
            {
                powerGrid.CurrentDemandUnits = demand;
            }

            var gridTick = powerGrid.EvaluateGridTick(isDuringWave ? 1.0f : 5.0f);
            bool tripped = powerGrid.IsBreakerTripped;
            poweredNodes = powerGrid.ComputePoweredNodesBfs();
            int poweredCount = 0;

            for (int i = 0; i < Buildings.Count; i++)
            {
                var b = Buildings[i];
                if (b == null || b.IsBroken || b.CurrentHp <= 0)
                {
                    if (b != null) b.IsPowered = false;
                    continue;
                }

                bool reachableFromGrid = powerGrid.IsBuildingPoweredByGrid(b, poweredNodes);
                bool requiresPower = blueprintRegistry.TryGetValue(b.BlueprintId, out var bp) &&
                                     (bp.PowerUnitsContinuous > 0 || bp.PowerUnitsPeak > 0);

                b.IsPowered = reachableFromGrid && (!requiresPower || !tripped);
                if (b.IsPowered) poweredCount++;

                if (gridTick.durabilityLossPerSec > 0 && requiresPower && reachableFromGrid)
                {
                    b.Durability = Math.Max(0, b.Durability - gridTick.durabilityLossPerSec);
                    if (b.Durability == 0) b.IsBroken = true;
                }
            }

            int cap = isDuringWave ? powerGrid.GetPeakCapacity() : powerGrid.GetContinuousCapacity();
            return (powerGrid.CurrentDemandUnits, cap, tripped, poweredCount);
        }

        /// <summary>
        /// Tính bậc Tình báo thời gian thực (0..5) dựa trên các công trình Trinh thám đang có điện và chỉ số INT của nhân vật.
        /// </summary>
        public int ComputeLiveIntelTier(int baseIntelTier, bool isAnProtagonistWithPreviewLock)
        {
            int bonus = 0;
            if (HasPoweredBuilding("ThapCanh")) bonus += 1;
            if (HasPoweredBuilding("RadarUyHiep")) bonus += 1;
            if (HasPoweredBuilding("BanTinhBao")) bonus += 1;
            if (HasPoweredBuilding("MayCongHuongHuyenBi")) bonus += 1;
            if (HasPoweredBuilding("ThienNhanVongDai")) bonus += 2;
            if (isAnProtagonistWithPreviewLock) bonus += 1;
            return Math.Clamp(Math.Max(baseIntelTier, 1 + bonus), 0, 5);
        }

        /// <summary>
        /// Thực thi một nhịp bắn phòng thủ của toàn bộ tháp trong đợt quái:
        /// - Tiêu hao đạn theo chế độ bắn (TuDo / TietKiem -35% / ChiTinhAnh).
        /// - Tính tổng sát thương phòng tuyến và điểm Chấn Động Âm Thanh (Acoustic Shock) tác động lên heo nái mang thai ngoài Nơi Trú.
        /// </summary>
        public (int totalTowerDamageDealt, int totalAmmoSpent, int acousticShockScore, int buildingsDamagedCount) ExecuteDefenseWaveSalvo(
            int incomingRaiderStrength,
            bool triggerBachVanStrike)
        {
            int totalDamage = 0;
            int ammoSpent = 0;
            int acousticShock = 0;
            int damagedCount = 0;

            for (int i = 0; i < Buildings.Count; i++)
            {
                var b = Buildings[i];
                if (b == null || b.IsBroken || b.CurrentHp <= 0 || !b.IsPowered) continue;

                if (b.IsBachVan)
                {
                    if (triggerBachVanStrike && b.AmmoCurrent >= b.AmmoCostPerShot && b.CurrentCooldownRemaining <= 0f)
                    {
                        TriggerBachVanManual(b.InstanceId);
                        ammoSpent += b.AmmoCostPerShot;
                        totalDamage += 650;
                        acousticShock += 35; // Cửu Tiêu Lôi Pháo tạo âm chấn cực lớn
                    }
                    continue;
                }

                if (b.Category == BuildingCategory.TanCong || b.Category == BuildingCategory.Bay)
                {
                    int shotCost = Math.Max(1, b.AmmoCostPerShot);
                    if (b.FireMode == "TietKiem")
                    {
                        shotCost = Math.Max(1, (int)Math.Floor(shotCost * 0.65f));
                    }

                    if (b.AmmoCapacity > 0 && b.AmmoCurrent >= shotCost)
                    {
                        b.AmmoCurrent -= shotCost;
                        ammoSpent += shotCost;
                        int baseTowerDmg = b.BlueprintId switch
                        {
                            "NoXuyenVan" => 55,
                            "ThapHoQuang" => 85,
                            "ThapPhongKhongSkyhook" => 95,
                            "DaiThienLoi" => 140,
                            _ => 35
                        };
                        totalDamage += baseTowerDmg * Math.Max(1, b.UpgradeLevel);
                    }
                    else if (b.AmmoCapacity == 0)
                    {
                        totalDamage += 30;
                    }

                    if (b.BlueprintId == "DaiThienLoi" || b.BlueprintId == "MinNo")
                    {
                        acousticShock += 15;
                    }
                }

                // Quái phản kích lên công trình phòng thủ/hàng rào tuyến đầu
                if (incomingRaiderStrength > totalDamage / 2 &&
                    (b.Category == BuildingCategory.PhongThu || b.Category == BuildingCategory.TanCong))
                {
                    int hpLoss = Math.Max(25, incomingRaiderStrength / 4);
                    b.CurrentHp = Math.Max(0, b.CurrentHp - hpLoss);
                    b.Durability = Math.Max(0, b.Durability - 5);
                    damagedCount++;
                    if (b.CurrentHp == 0 || b.Durability == 0)
                    {
                        b.IsBroken = true;
                    }
                }
            }

            LastWaveAcousticShockScore = acousticShock;
            return (totalDamage, ammoSpent, acousticShock, damagedCount);
        }
    }
}

