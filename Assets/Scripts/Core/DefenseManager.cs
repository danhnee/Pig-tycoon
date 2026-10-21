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
    }

    [Serializable]
    public class DefenseManager
    {
        public int HallLevel { get; set; } = 1;
        public List<string> UnlockedBlueprints { get; set; } = new List<string> { "NoXuyenVan", "BayChongGo", "ThapCanh", "RaoGo" };
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
                BuildCostWood = 20,
                Damage = 40f,
                AttackRange = 7.0f,
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
                Description = "Đại trận hồi phục sinh lực và giải độc tức thì cho toàn bộ đàn heo trang trại.",
                IsBachVan = true
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

            var newBldg = new DefenseBuildingInstance
            {
                InstanceId = $"bldg_{Guid.NewGuid():N}",
                BlueprintId = bp.Id,
                Name = bp.Name,
                Category = bp.Category,
                IsBachVan = bp.IsBachVan,
                Durability = bp.Id == "BayChongGo" ? 10 : 100,
                MaxDurability = bp.Id == "BayChongGo" ? 10 : 100,
                MaxHp = bp.Id == "BayChongGo" ? 120 : (bp.IsBachVan ? 5000 : 1500),
                CurrentHp = bp.Id == "BayChongGo" ? 120 : (bp.IsBachVan ? 5000 : 1500),
                AmmoCurrent = bp.Id == "BayChongGo" ? 10 : (bp.Id == "NoXuyenVan" ? 50 : (bp.IsBachVan ? 120 : 24)),
                AmmoCapacity = bp.Id == "BayChongGo" ? 10 : (bp.Id == "NoXuyenVan" ? 50 : (bp.IsBachVan ? 120 : 24)),
                AmmoName = bp.Id == "BayChongGo" ? "Cọc Chông" : (bp.Id == "NoXuyenVan" ? "Tên Thép" : "Đơn Vị Cộng Hưởng"),
                AmmoCostPerShot = bp.Id == "BayChongGo" ? 1 : (bp.Id == "NoXuyenVan" ? 1 : (bp.IsBachVan ? 50 : 1)),
                CooldownSeconds = bp.Id == "BayChongGo" ? 2.5f : (bp.Id == "NoXuyenVan" ? 1.2f : 75f),
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
