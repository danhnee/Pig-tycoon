using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public sealed class DefenseRepairOrder
    {
        public string BuildingId = "";
        public int RemainingMinutes;
        public DefenseRepairOrder Copy() => new DefenseRepairOrder { BuildingId = BuildingId, RemainingMinutes = RemainingMinutes };
    }

    public sealed class DefenseRepairQuote
    {
        public int Tier, Gold, Scrap, FineComponents, Minutes;
    }

    /// <summary>GDD 18; tier assignments authorized by the user on 2026-10-30, ADR-0010.</summary>
    public static class DefenseRepairs
    {
        public const string ScrapId = "item_alloy_scrap";
        public const string FineComponentId = "item_fine_component";
        static readonly int[] GoldPerBlock = { 20, 50, 80, 160 };
        static readonly int[] ScrapPerBlock = { 2, 3, 4, 7 };
        static readonly int[] MinutesPerBlock = { 15, 30, 45, 60 };
        static readonly HashSet<string> Heavy = new HashSet<string>(StringComparer.Ordinal)
        { "ThapHoQuang", "ThapPhongKhongSkyhook", "DaiThienLoi", "MayChieuKhien", "MayCongHuongHuyenBi", "DaiDoDiTuong" };
        static readonly HashSet<string> Medium = new HashSet<string>(StringComparer.Ordinal)
        { "NoXuyenVan", "RadarUyHiep", "BanTinhBao", "TramTiepSongCho", "TramKhiTuong", "TramHoiSuc",
          "DenPhaCongNghiep", "MayPhatDien", "BomLamMat", "CotThuLoi", "RaoThep", "CongGiaCo", "LuoiEmp", "TamSocDien", "DenHieuDanhChan" };

        public static int Tier(DefenseBlueprint blueprint)
        {
            if (blueprint == null) throw new ArgumentNullException(nameof(blueprint));
            return blueprint.IsBachVan ? 3 : Heavy.Contains(blueprint.Id) ? 2 : Medium.Contains(blueprint.Id) ? 1 : 0;
        }

        public static DefenseRepairQuote Quote(GameEngine engine, DefenseBuildingInstance building)
        {
            var bp = engine.Defense.GetBlueprint(building.BlueprintId) ?? throw new ArgumentException("Unknown blueprint.");
            int tier = Tier(bp);
            int hpWear = building.MaxHp > 0 ? (int)Math.Ceiling(100d * Math.Max(0, building.MaxHp - building.CurrentHp) / building.MaxHp) : 0;
            int blocks = (Math.Max(Math.Max(0, 100 - building.Durability), hpWear) + 9) / 10;
            if (building.IsBroken) blocks = Math.Max(1, blocks);
            // Strongest damage channel determines the workload; HP and operating wear are not charged twice.
            // GDD 13.2 / 11.1: efficiency of the NPC on duty (40 / 70 / 100%); the quote with nobody on duty is shown at
            // the 40% rate, but Request refuses it and Advance pauses ("phòng ngừng hoạt động").
            int efficiencyPercent = ConstructionHall.EfficiencyPercent(engine);
            if (efficiencyPercent <= 0) efficiencyPercent = ConstructionHall.OtherNpcEfficiencyPercent;
            return new DefenseRepairQuote
            {
                Tier = tier,
                Gold = checked(blocks * GoldPerBlock[tier] + (building.IsBroken ? (bp.BuildCostGold + 9) / 10 : 0)),
                Scrap = checked(blocks * ScrapPerBlock[tier]),
                FineComponents = tier == 3 ? blocks : 0,
                Minutes = (int)Math.Ceiling(blocks * MinutesPerBlock[tier] * 100d / efficiencyPercent)
            };
        }

        public static (bool success, string message) Request(GameEngine engine, string buildingId)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (engine.Clock.IsDuringCombatWave) return (false, "Chỉ sửa ngoài đợt quái.");
            // The Phòng Xây Dựng stands on the farm map ("Farm"); the old 3D layout called it "Farm_Core". Requiring only
            // "Farm_Core" blocked every repair in the 2D game, where no map has that id (3 Nov audit).
            string loc = engine.WorldLocations.CurrentLocationId;
            if (loc != "Farm" && loc != "Farm_Core") return (false, "Đến Phòng Xây Dựng ở trang trại để đặt sửa.");
            if (engine.Defense.HallLevel < 1) return (false, "Cần Phòng Xây Dựng.");
            if (ConstructionHall.OnDutyNpc(engine) == null) return (false, "Phòng Xây Dựng chưa có NPC trực — không sửa được.");
            var building = engine.Defense.Buildings.Find(b => b.InstanceId == buildingId);
            if (building == null) return (false, "Không tìm thấy công trình.");
            if (engine.RepairOrders.Exists(o => o.BuildingId == buildingId)) return (false, "Công trình đã có lệnh sửa.");
            var quote = Quote(engine, building);
            if (quote.Minutes == 0) return (false, "Công trình không cần sửa.");
            if (engine.Farm.Gold < quote.Gold || engine.Inventory.CountId(ScrapId) < quote.Scrap ||
                engine.Inventory.CountId(FineComponentId) < quote.FineComponents)
                return (false, $"Cần {quote.Gold}G, {quote.Scrap} phế liệu, {quote.FineComponents} Linh Kiện Tinh; thời gian {quote.Minutes} phút game.");
            if (!engine.Ledger.TrySpend(quote.Gold, LedgerReason.RepairStructure, buildingId)) return (false, "Không đủ vàng.");
            engine.Inventory.ConsumeItem(ScrapId, quote.Scrap);
            engine.Inventory.ConsumeItem(FineComponentId, quote.FineComponents);
            engine.RepairOrders.Add(new DefenseRepairOrder { BuildingId = buildingId, RemainingMinutes = quote.Minutes });
            return (true, $"Đã đặt sửa {building.Name}: {quote.Gold}G, {quote.Scrap} phế liệu, {quote.FineComponents} Linh Kiện Tinh; {quote.Minutes} phút game.");
        }

        public static void Advance(GameEngine engine, int elapsedMinutes)
        {
            if (elapsedMinutes <= 0 || engine.Clock.IsDuringCombatWave) return;
            PlagueZones.Advance(engine, elapsedMinutes); // GDD 19.1: Kẻ Mang Dịch zones turn into Chuồng Nhiễm after 12 h
            if (engine.RepairOrders.Count > 0 && ConstructionHall.OnDutyNpc(engine) == null) return; // GDD 13.2: room stopped
            for (int i = engine.RepairOrders.Count - 1; i >= 0; i--)
            {
                var order = engine.RepairOrders[i];
                order.RemainingMinutes = Math.Max(0, order.RemainingMinutes - elapsedMinutes);
                if (order.RemainingMinutes > 0) continue;
                var building = engine.Defense.Buildings.Find(b => b.InstanceId == order.BuildingId);
                if (building != null)
                {
                    building.Durability = 100;
                    building.CurrentHp = building.MaxHp;
                    building.IsBroken = false;
                }
                engine.RepairOrders.RemoveAt(i);
            }
        }
    }
}
