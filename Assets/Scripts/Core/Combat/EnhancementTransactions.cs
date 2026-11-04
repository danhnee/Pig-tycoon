using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PigTycoon.Core
{
    public sealed class EnhancementQuote
    {
        public SpecialtyPaperColor Color { get; }
        public int CurrentLevel { get; }
        public int TargetLevel => CurrentLevel + 1;
        public int Gold { get; }
        public IReadOnlyDictionary<string, int> Materials { get; }
        public int MaxStones => TargetLevel == 1 ? 0 : TargetLevel == 2 ? 5 : 8;
        public double SuccessRate(int stones) => TargetLevel == 1 ? 1d : TargetLevel == 2 ?
            Math.Min(0.95d, 0.65d + 0.06d * stones) : Math.Min(0.80d, 0.40d + 0.05d * stones);

        internal EnhancementQuote(SpecialtyPaperColor color, int level, int gold, Dictionary<string, int> materials)
        { Color = color; CurrentLevel = level; Gold = gold; Materials = new ReadOnlyDictionary<string, int>(materials); }
    }

    public sealed class EnhancementResult
    {
        public bool Attempted { get; internal set; }
        public bool Succeeded { get; internal set; }
        public double SuccessRate { get; internal set; }
        public int NewLevel { get; internal set; }
        public string Message { get; internal set; } = string.Empty;
    }

    /// <summary>
    /// Paid paper enhancement, GDD v6 paragraphs 02035–02076. The selected owned paper
    /// stays intact on failure; gold, recipe materials and submitted stones are spent.
    /// Full recipes only: the document does not define how to combine shortage ratios
    /// of two different materials, so this command does not invent that balance rule.
    /// </summary>
    public static class EnhancementTransactions
    {
        public const string WorkshopLocationId = "GeneLab";
        public const string StationInstanceId = "machine_paper_press_1";
        public const int StationTileX = 8, StationTileY = 4;
        public const string StoneId = "da_cuong_hoa";
        private static readonly string[] MaterialIds = { "muc_an", "to_vang", "bot_tu_tinh", "huyet_trien", "tinh_hoa_di_bien" };
        private static readonly int[,] GoldCosts = { { 200, 400, 700 }, { 600, 1000, 1600 }, { 1500, 2500, 4000 }, { 4000, 7000, 12000 } };
        private static readonly int[,] PrimaryCounts = { { 3, 5, 8 }, { 4, 6, 9 }, { 3, 5, 8 }, { 2, 3, 5 } };

        public static string PaperId(SpecialtyPaperColor color)
        {
            switch (color)
            {
                case SpecialtyPaperColor.Lam: return "(G)Paper_Blue";
                case SpecialtyPaperColor.Vang: return "(G)Paper_Yellow";
                case SpecialtyPaperColor.Tim: return "(G)Paper_Purple";
                case SpecialtyPaperColor.Do: return "(G)Paper_Red";
                default: throw new ArgumentOutOfRangeException(nameof(color));
            }
        }

        public static EnhancementQuote Quote(GameEngine engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var color = engine.DeepMechanics.EquippedPaperColor;
            int index = (int)color, level = engine.DeepMechanics.intEquippedPaperEnhancementLevel;
            if (index < 0 || index > 3 || level < 0 || level > 2)
                throw new ArgumentException("Giấy hoặc cấp gia cường không hợp lệ.");
            var materials = new Dictionary<string, int> { { MaterialIds[index], PrimaryCounts[index, level] } };
            if (level > 0) materials.Add(MaterialIds[index + 1], level);
            return new EnhancementQuote(color, level, GoldCosts[index, level], materials);
        }

        public static EnhancementResult TryEnhance(GameEngine engine, int stones)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            lock (engine) lock (engine.Inventory)
            {
                int oldLevel = engine.DeepMechanics.intEquippedPaperEnhancementLevel;
                EnhancementResult Reject(string message) => new EnhancementResult { NewLevel = oldLevel, Message = message };
                if (engine.Clock.IsDuringCombatWave) return Reject("Không gia cường trong đợt quái.");
                if (!engine.DeepMechanics.MidGameGateUnlocked ||
                    !engine.CoreAndVillage.IsRoomUnlocked(FarmCoreRoomId.XuongCuongHoa, engine.Farm.FarmLevel, engine.DeepMechanics.MidGameGateUnlocked))
                    return Reject("Chưa mở Xưởng Cường Hóa sau Cổng Giữa Game.");
                if (engine.WorldLocations.CurrentLocationId != WorkshopLocationId)
                    return Reject("Đến bàn gia cường trong xưởng để thực hiện.");
                var workshop = engine.WorldLocations.GetLocation(WorkshopLocationId);
                var station = workshop?.Machines.Find(machine => machine.InstanceId == StationInstanceId);
                if (station == null || Math.Abs((long)engine.WorldLocations.PlayerTileX - station.TileX) > 1 ||
                    Math.Abs((long)engine.WorldLocations.PlayerTileY - station.TileY) > 1)
                    return Reject("Cần đứng cạnh bàn gia cường.");

                EnhancementQuote quote;
                try { quote = Quote(engine); }
                catch (ArgumentException ex) { return Reject(ex.Message); }
                if (stones < 0 || stones > quote.MaxStones) return Reject("Số Đá Cường Hóa không hợp lệ cho cấp giấy này.");
                if (!engine.Inventory.ContainsId(PaperId(quote.Color))) return Reject("Không sở hữu giấy đã chọn.");
                if (engine.Farm.Gold < quote.Gold) return Reject("Không đủ vàng gia cường.");
                var deductions = new Dictionary<string, int>();
                foreach (var material in quote.Materials) deductions.Add(material.Key, material.Value);
                if (stones > 0) deductions.Add(StoneId, stones);
                foreach (var deduction in deductions)
                    if (!engine.Inventory.ContainsId(deduction.Key, deduction.Value))
                        return Reject("Chưa đủ nguyên liệu hoặc Đá Cường Hóa; chưa trừ tài nguyên.");

                // Plan the whole debit before charging. These recipe materials have no
                // Farm feed/medicine/fuel projection; preserve paper and unrelated stacks.
                var remaining = new List<UnifiedItemInstance>();
                foreach (var item in engine.Inventory.Slots) remaining.Add(item.Clone());
                foreach (var deduction in deductions)
                {
                    string id = QualifiedItemRegistry.QualifyItemId(deduction.Key);
                    int count = deduction.Value;
                    for (int i = remaining.Count - 1; i >= 0 && count > 0; i--)
                    {
                        if (!remaining[i].QualifiedItemId.Equals(id, StringComparison.OrdinalIgnoreCase)) continue;
                        int take = Math.Min(count, remaining[i].Stack);
                        remaining[i].Stack -= take; count -= take;
                        if (remaining[i].Stack == 0) remaining.RemoveAt(i);
                    }
                    if (count != 0) return Reject("Kho đã thay đổi; chưa trừ tài nguyên.");
                }
                double rate = quote.SuccessRate(stones);
                double roll = engine.NextEnhancementRoll();
                if (!engine.Ledger.TrySpend(quote.Gold, LedgerReason.EnhanceWorkshop, "paper_" + quote.Color + "_" + quote.TargetLevel))
                    return Reject("Không đủ vàng; chưa trừ nguyên liệu.");
                engine.Inventory.ClearAndRestore(remaining);
                bool succeeded = roll < rate;
                if (succeeded) engine.DeepMechanics.intEquippedPaperEnhancementLevel = quote.TargetLevel;
                engine.EventBus.Publish(new PaperEnhancedEvent(quote.Color, engine.DeepMechanics.intEquippedPaperEnhancementLevel, succeeded));
                return new EnhancementResult
                {
                    Attempted = true, Succeeded = succeeded, SuccessRate = rate,
                    NewLevel = engine.DeepMechanics.intEquippedPaperEnhancementLevel,
                    Message = succeeded ? $"Gia cường thành công lên +{quote.TargetLevel}." : $"Gia cường thất bại; giữ nguyên giấy +{oldLevel}."
                };
            }
        }
    }
}
