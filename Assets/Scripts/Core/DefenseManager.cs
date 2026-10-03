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
        public bool IsBachVan;
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
    }

    [Serializable]
    public class DefenseManager
    {
        public int HallLevel { get; set; } = 1;
        public List<string> UnlockedBlueprints { get; set; } = new List<string> { "NoXuyenVan", "ThapCanh", "RaoGo" };
        public List<DefenseBuildingInstance> Buildings { get; set; } = new List<DefenseBuildingInstance>();
        private readonly Dictionary<string, DefenseBlueprint> blueprintRegistry = new Dictionary<string, DefenseBlueprint>();

        public DefenseManager()
        {
            InitBlueprints();
        }

        private void InitBlueprints()
        {
            blueprintRegistry["NoXuyenVan"] = new DefenseBlueprint
            {
                Id = "NoXuyenVan",
                Name = "Nỏ Xuyên Vân",
                Category = BuildingCategory.TanCong,
                Rarity = "Tinh",
                RequiredHallLevel = 1,
                BuildCostGold = 2400,
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
                IsBachVan = true
            };
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
                CooldownSeconds = bp.Id == "NoXuyenVan" ? 1.2f : 75f
            };

            Buildings.Add(newBldg);
            return (true, $"Đã xây thành công {bp.Name}!", newBldg);
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
