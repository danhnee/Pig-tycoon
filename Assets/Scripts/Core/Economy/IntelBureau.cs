using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>What a bought intel level says, split as GDD 23 asks: Cảnh báo / Tin đã xác nhận / Suy đoán.</summary>
    public sealed class IntelReport
    {
        public int Level;
        public int CycleNumber;
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Confirmed = new List<string>();
        public readonly List<string> Speculation = new List<string>();
        public IEnumerable<string> AllLines => Warnings.Concat(Confirmed).Concat(Speculation);
    }

    /// <summary>
    /// GDD 23 — Thông tin &amp; Tình báo about the next major wave and the running cycle. Level 0 is free; levels 1–4 are
    /// bought at the Bàn Tình Báo for 150/300/450/600G, each needing the level below; level 5 only from Bí nhân or an
    /// event. Bought intel expires when the cycle changes. The report never shows a level that was not bought.
    /// L1 event type (70–85% accurate, here 78%) + cycle length range; L2 1–2 true contributing factors; L3 threshold
    /// band ("đợt lớn sau c±2 ngày", ÁLSK band); L4 window ±1 day with time of day; L5 everything exact.
    /// The 78% accuracy point, the 20-point ÁLSK band and the band offsets are ADR-0023 provisional choices.
    /// </summary>
    [Serializable]
    public sealed class IntelBureau
    {
        public static readonly int[] LevelCostGold = { 0, 150, 300, 450, 600 };
        public const int MaxPurchasableLevel = 4;
        public const int FullLevel = 5;
        public const double Level1Accuracy = 0.78;          // GDD 23: 70–85%
        public const int MajorWaveHourOfDay = 20;           // mirrors EnemyWaveSpawner.MajorWaveHour (Presentation)

        public int Level { get; private set; }
        public int CycleNumber { get; private set; } = 1;

        public static int CostOf(int level) => level >= 1 && level <= MaxPurchasableLevel ? LevelCostGold[level] : 0;

        /// <summary>Intel expires when the cycle changes (GDD 23); thresholds and windows are re-seeded per cycle.</summary>
        public void SyncCycle(int cycleNumber)
        {
            if (cycleNumber == CycleNumber) return;
            CycleNumber = cycleNumber;
            Level = 0;
        }

        public void Restore(int level, int cycleNumber)
        {
            CycleNumber = Math.Max(1, cycleNumber);
            Level = Math.Clamp(level, 0, FullLevel);
        }

        /// <summary>Buys level N at the Bàn Tình Báo: N in 1..4, the farm must already hold N−1 this cycle.</summary>
        public const string DeskBlueprintId = "BanTinhBao";
        public const int QuestionsPerDay = 2;
        public int QuestionsDay { get; set; } = -1;
        public int QuestionsToday { get; set; }

        public (bool success, string message) Purchase(GameEngine engine, int level)
        {
            SyncCycle(engine.Clock.CycleNumber);
            // GDD 15: Bàn Tình Báo (Tinh, 5.000G) "2 lượt hỏi": the desk has to be built; 2 questions a day (C-11)
            if (!engine.Defense.Buildings.Exists(b => b.BlueprintId == DeskBlueprintId))
                return (false, "Cần xây Bàn Tình Báo trước (bản vẽ Tinh, mua ở chợ đêm).");
            if (QuestionsDay != engine.Clock.CurrentDay) { QuestionsDay = engine.Clock.CurrentDay; QuestionsToday = 0; }
            if (QuestionsToday >= QuestionsPerDay) return (false, $"Bàn Tình Báo đã trả lời {QuestionsPerDay} lượt hôm nay; mai hỏi tiếp.");
            if (level < 1 || level > MaxPurchasableLevel)
                return (false, "Bàn Tình Báo chỉ bán tin cấp 1–4. Cấp 5 chỉ có từ Bí nhân hoặc sự kiện.");
            if (Level >= level) return (false, $"Đã có tin cấp {Level} cho chu kỳ này.");
            if (level != Level + 1) return (false, $"Phải có tin cấp {level - 1} trước khi mua cấp {level}.");
            int cost = CostOf(level);
            if (!engine.Ledger.TrySpend(cost, LedgerReason.IntelPurchase, $"intel_c{CycleNumber}_l{level}"))
                return (false, $"Không đủ vàng ({cost}G).");
            Level = level;
            QuestionsToday++;
            engine.DeepMechanics.SpecialtyLocks.RecordIntelQuestion(); // An khóa 2: "dùng 40 lượt hỏi"
            return (true, $"Đã mua tin cấp {level} ({cost}G). Tin hết hạn khi sang chu kỳ mới.");
        }

        /// <summary>GDD 23: level 5 comes only from Bí nhân or an event, and skips the level ladder.</summary>
        public void GrantFull(int cycleNumber)
        {
            SyncCycle(cycleNumber);
            Level = FullLevel;
        }

        public IntelReport BuildReport(GameEngine engine)
        {
            SyncCycle(engine.Clock.CycleNumber);
            var r = new IntelReport { Level = Level, CycleNumber = CycleNumber };
            var clock = engine.Clock;
            int daysLeft = clock.GetDaysUntilMajorWave();
            int length = clock.CycleTotalDays;
            bool boss = WaveScheduleAndReportSystem.GetScheduledCycleBoss(CycleNumber).HasValue;
            // per-cycle noise, stable while the cycle lasts, independent of what was bought
            var rng = new Random(unchecked(engine.BaseSeed * 7919 + CycleNumber * 104729 + 23));
            bool l1Correct = rng.NextDouble() < Level1Accuracy;
            int rangeLow = Math.Max(1, length - rng.Next(0, 4));
            int bandShift = rng.Next(-1, 2);
            int windowShift = rng.Next(-1, 2);
            int pressureOffset = rng.Next(0, 21);

            r.Warnings.Add("Một tín hiệu bất ổn đang hình thành.");
            if (Level == 0)
                return r;

            if (Level < FullLevel)
            {
                bool saysBoss = l1Correct ? boss : !boss;
                r.Speculation.Add(saysBoss ? "Nhiều khả năng đợt lớn tới có Boss chu kỳ." : "Nhiều khả năng đợt lớn tới là đợt thường, không Boss.");
                r.Speculation.Add($"Chu kỳ này dài {rangeLow}–{rangeLow + 3} ngày.");
            }

            if (Level >= 2)
            {
                var factors = ContributingFactors(engine);
                if (Level >= FullLevel)
                    r.Confirmed.Add(factors.Count == 0 ? "Không yếu tố nào trong trại đang đẩy áp lực sự kiện."
                        : "Yếu tố góp phần: " + string.Join("; ", factors.Select(f => f.detail)) + ".");
                else
                    r.Confirmed.Add(factors.Count == 0 ? "Chưa yếu tố nào trong trại góp phần đáng kể; áp lực đến từ nhịp chu kỳ."
                        : string.Join(" và ", factors.Take(2).Select(f => f.name)) + " đang góp phần.");
            }

            if (Level >= 3 && Level < FullLevel)
            {
                int center = Math.Max(0, daysLeft + bandShift);
                r.Warnings.Add($"Đợt lớn sau {center}±2 ngày.");
                int pressure = engine.Farm.EventPressureIndex;
                int low = Math.Clamp(pressure - pressureOffset, 0, 80);
                r.Confirmed.Add($"Áp lực sự kiện (ÁLSK) đang trong dải {low}–{low + 20}.");
            }

            if (Level >= 4 && Level < FullLevel)
            {
                int center = Math.Max(1, daysLeft + windowShift);
                r.Warnings.Add($"Cửa sổ đợt lớn mở trong {center - 1}–{center + 1} ngày, thiên về {TimeOfDayVi(MajorWaveHourOfDay)}.");
            }

            if (Level >= FullLevel)
            {
                r.Warnings.Add(daysLeft == 0
                    ? $"Đợt lớn đến TỐI NAY lúc {MajorWaveHourOfDay:00}:00."
                    : $"Đợt lớn đến ngày {clock.CurrentDay + daysLeft} (còn {daysLeft} ngày) lúc {MajorWaveHourOfDay:00}:00.");
                var bossId = WaveScheduleAndReportSystem.GetScheduledCycleBoss(CycleNumber);
                r.Confirmed.Add(bossId.HasValue ? $"Đợt lớn có Boss: {bossId.Value}." : "Đợt lớn không có Boss.");
                r.Confirmed.Add($"Chu kỳ {CycleNumber} dài đúng {length} ngày; đang ngày {clock.CycleDayIndex}; còn {clock.MinorWavesRemainingInCycle} đợt nhỏ.");
                r.Confirmed.Add($"ÁLSK hiện tại: {engine.Farm.EventPressureIndex}.");
            }
            return r;
        }

        private static string TimeOfDayVi(int hour) =>
            hour >= 6 && hour < 10 ? "buổi sáng" : hour >= 10 && hour < 14 ? "buổi trưa" : hour >= 14 && hour < 18 ? "buổi chiều" : "buổi tối";

        /// <summary>The true ÁLSK contributors of the farm (Farm.UpdateDailyEventPressure, GDD 5.3/6.4), strongest first.</summary>
        public static List<(string name, string detail, int weight)> ContributingFactors(GameEngine engine)
        {
            var list = new List<(string name, string detail, int weight)>();
            var living = engine.Pigs.FindAll(p => p.IsAlive);
            int elders = living.Count(p => p.Stage == PigStage.HeoGia);
            int softElder = (int)(living.Count * 0.15f);
            if (elders > softElder) list.Add(("Heo già", $"{elders} heo già (ngưỡng mềm {softElder})", 2 + (elders - softElder) / 10));
            int corpses = engine.Pigs.Count(p => !p.IsAlive && !p.IsInProcessingLot);
            if (corpses > 0) list.Add(("Xác heo ngoài bãi", $"{corpses} xác heo ngoài bãi", Math.Min(12, corpses * 3)));
            if (engine.Farm.CorpseLotCount > 0) list.Add(("Khu tập kết xác", $"{engine.Farm.CorpseLotCount} lô xác đang chờ", engine.Farm.CorpseLotCount));
            float density = engine.Farm.CalculateCapacityReport(living.Count, corpses).RawDensityRatio;
            if (density > 1.20f) list.Add(("Chuồng quá tải", $"mật độ {density:0.00}", density > 1.50f ? 6 : 3));
            if (engine.Farm.HabitatIndex < 50) list.Add(("Sinh Cảnh suy giảm", $"Sinh Cảnh {engine.Farm.HabitatIndex}", engine.Farm.HabitatIndex < 30 ? 6 : 3));
            if (engine.CurrentWeather == WeatherType.SuongMu) list.Add(("Sương mù", "sương mù đang phủ trại", 3));
            int sick = living.Count(p => p.ActiveDisease.HasValue);
            if (sick > 0) list.Add(("Heo bệnh", $"{sick} heo đang bệnh", Math.Min(6, sick)));
            return list.OrderByDescending(f => f.weight).ToList();
        }
    }
}
