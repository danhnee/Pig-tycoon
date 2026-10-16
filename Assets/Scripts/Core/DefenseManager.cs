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
    }
}

