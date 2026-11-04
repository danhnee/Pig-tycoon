using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>One structure being built in Phòng Xây Dựng (GDD 13.2); saved with the game.</summary>
    [Serializable]
    public sealed class ConstructionOrder
    {
        public string OrderId = "";
        public string BlueprintId = "";
        public string Name = "";
        public float PosX;
        public float PosY;
        public bool HasPosition;
        /// <summary>Build time at 100% efficiency, in game minutes.</summary>
        public int BaseMinutes;
        /// <summary>Work done in hundredths of a base minute: each game minute adds the room's efficiency percent.</summary>
        public int WorkDoneCenti;
        public int GoldPaid;

        public int WorkNeededCenti => Math.Max(0, BaseMinutes) * 100;
        public float Progress => WorkNeededCenti <= 0 ? 1f : Math.Min(1f, WorkDoneCenti / (float)WorkNeededCenti);
        public bool IsDone => WorkDoneCenti >= WorkNeededCenti;

        public ConstructionOrder Copy() => new ConstructionOrder
        {
            OrderId = OrderId, BlueprintId = BlueprintId, Name = Name, PosX = PosX, PosY = PosY, HasPosition = HasPosition,
            BaseMinutes = BaseMinutes, WorkDoneCenti = WorkDoneCenti, GoldPaid = GoldPaid
        };
    }

    public partial class DefenseManager
    {
        /// <summary>GDD 13.2: structures under construction ("đang thi công"), at most <see cref="ConstructionHall.BuildSlots(GameEngine)"/>.</summary>
        public List<ConstructionOrder> ConstructionOrders { get; set; } = new List<ConstructionOrder>();

        public List<string> PendingBlueprintIds() => ConstructionOrders.ConvertAll(o => o.BlueprintId);
    }

    /// <summary>
    /// GDD 13.2 + 11.1: every structure needs (1) a learned blueprint, (2) a Phòng Xây Dựng of the right level and (3) an
    /// NPC on duty. Real time = base time / efficiency (40% other NPC, 70% Kỹ Sư, 100% Kỹ Sư with trait); without anyone
    /// on duty the room stops (orders pause, nothing new starts). Gold is paid when the order starts.
    /// </summary>
    public static class ConstructionQueue
    {
        public static int FreeSlots(GameEngine engine) =>
            Math.Max(0, ConstructionHall.BuildSlots(engine) - engine.Defense.ConstructionOrders.Count);

        /// <summary>Checks everything except gold; used by the popup to show why a blueprint cannot start.</summary>
        public static (bool ok, string message) CanStart(GameEngine engine, string blueprintId)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var def = engine.Defense;
            var bp = def.GetBlueprint(blueprintId);
            if (bp == null) return (false, "Bản vẽ không hợp lệ.");
            if (ConstructionHall.OnDutyNpc(engine) == null)
                return (false, "Phòng Xây Dựng chưa có NPC trực — thuê một NPC trang trại (Kỹ Sư Xây Dựng tốt nhất) để thi công.");
            var rules = def.CheckConstructionRules(blueprintId, def.PendingBlueprintIds());
            if (!rules.ok) return rules;
            if (DefenseStructureSpecs.Get(blueprintId)?.Tier == "BachVan" && engine.Farm.FarmLevel < DefenseStructureSpecs.BachVanFarmLevel)
                return (false, $"Bản vẽ Bạch Vân cần Phòng Xây Dựng cấp 3 và Cấp trại ≥ {DefenseStructureSpecs.BachVanFarmLevel} (GDD 13.2).");
            int slots = ConstructionHall.BuildSlots(engine);
            if (def.ConstructionOrders.Count >= slots)
                return (false, $"Hết ô thi công ({def.ConstructionOrders.Count}/{slots}). Chờ công trình đang thi công xong.");
            return (true, "");
        }

        /// <summary>Real game minutes a new order of this blueprint would take now; -1 = nobody on duty.</summary>
        public static int QuoteMinutes(GameEngine engine, string blueprintId) =>
            ConstructionHall.RealMinutes(ConstructionHall.BaseBuildMinutes(blueprintId), ConstructionHall.EfficiencyPercent(engine));

        public static (bool success, string message, ConstructionOrder? order) TryStart(
            GameEngine engine, string blueprintId, float posX = 0f, float posY = 0f, bool hasPosition = false)
        {
            var check = CanStart(engine, blueprintId);
            if (!check.ok) return (false, check.message, null);
            var bp = engine.Defense.GetBlueprint(blueprintId)!;
            if (engine.Farm.Gold < bp.BuildCostGold) return (false, $"Không đủ vàng xây dựng (cần {bp.BuildCostGold:N0}G).", null);
            var order = new ConstructionOrder
            {
                OrderId = $"build_{Guid.NewGuid():N}",
                BlueprintId = bp.Id,
                Name = bp.Name,
                PosX = posX,
                PosY = posY,
                HasPosition = hasPosition,
                BaseMinutes = ConstructionHall.BaseBuildMinutes(bp.Id),
                GoldPaid = bp.BuildCostGold
            };
            if (bp.BuildCostGold > 0 && !engine.Ledger.TrySpend(bp.BuildCostGold, LedgerReason.BuildStructure, order.OrderId))
                return (false, "Không đủ vàng xây dựng.", null);
            engine.Defense.ConstructionOrders.Add(order);
            int minutes = QuoteMinutes(engine, bp.Id);
            return (true, $"Bắt đầu thi công {bp.Name} (−{bp.BuildCostGold:N0}G): {FormatMinutes(minutes)} giờ game ở hiệu suất {ConstructionHall.EfficiencyPercent(engine)}%.", order);
        }

        /// <summary>
        /// Advances every order by the elapsed game minutes at the room's current efficiency; finished orders become real
        /// structures (with their saved position) and signal StructureBuilt. Returns the structures completed.
        /// </summary>
        public static List<DefenseBuildingInstance> Advance(GameEngine engine, int elapsedMinutes)
        {
            var done = new List<DefenseBuildingInstance>();
            if (engine == null || elapsedMinutes <= 0) return done;
            var orders = engine.Defense.ConstructionOrders;
            if (orders.Count == 0) return done;
            int efficiency = ConstructionHall.EfficiencyPercent(engine);
            if (efficiency <= 0) return done; // "Không có NPC trực: phòng ngừng hoạt động"
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                long work = (long)o.WorkDoneCenti + (long)elapsedMinutes * efficiency;
                o.WorkDoneCenti = (int)Math.Min(work, o.WorkNeededCenti);
            }
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                if (!o.IsDone) continue;
                orders.RemoveAt(i);
                i--;
                var built = engine.Defense.ConstructBuilding(o.BlueprintId);
                if (!built.success || built.building == null)
                {
                    // Cannot happen with the reserved caps; never keep the gold of a structure that was not delivered.
                    if (o.GoldPaid > 0) engine.Ledger.Earn(o.GoldPaid, LedgerReason.DismantleRefund, o.OrderId);
                    continue;
                }
                built.building.PosX = o.PosX;
                built.building.PosY = o.PosY;
                built.building.HasPosition = o.HasPosition;
                done.Add(built.building);
                engine.SignalQuest(QuestSignal.StructureBuilt);
            }
            return done;
        }

        /// <summary>Minutes of game time left for an order at the current efficiency; -1 = paused (nobody on duty).</summary>
        public static int RemainingMinutes(GameEngine engine, ConstructionOrder order)
        {
            int eff = ConstructionHall.EfficiencyPercent(engine);
            if (eff <= 0) return -1;
            int left = Math.Max(0, order.WorkNeededCenti - order.WorkDoneCenti);
            return (left + eff - 1) / eff;
        }

        /// <summary>"h:mm" for a duration in game minutes; "—" when the room is stopped.</summary>
        public static string FormatMinutes(int minutes) =>
            minutes < 0 ? "—" : $"{minutes / 60}:{minutes % 60:00}";

        /// <summary>Drops invalid saved orders (unknown blueprint, empty id) when a save is restored.</summary>
        public static void RestoreOrders(DefenseManager defense, List<ConstructionOrder>? saved)
        {
            defense.ConstructionOrders = new List<ConstructionOrder>();
            if (saved == null) return;
            foreach (var o in saved)
                if (o != null && !string.IsNullOrWhiteSpace(o.BlueprintId) && defense.GetBlueprint(o.BlueprintId) != null && o.BaseMinutes > 0)
                    defense.ConstructionOrders.Add(o.Copy());
        }
    }
}
