using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// 6 khóa của Đặc tính Chuyên biệt ẩn (GDD 10.4, 10.5, 10.8).
    /// - Khóa lộ theo thứ tự 1 → 6 chỉ bằng Giấy Chuyên Biệt; mỗi lượt nhắm khóa thấp nhất chưa lộ ("sịt" thì lượt sau thử lại).
    /// - Lộ điều kiện chưa phải hoàn thành: phải làm xong nhiệm vụ của khóa đã lộ.
    /// - Tiến độ trước khi lộ được ghi ngầm nhưng chỉ công nhận tối đa 50%.
    /// - Tên và hiệu ứng chỉ hiện (và chỉ có hiệu lực) khi xong cả 6.
    /// </summary>
    [Serializable]
    public class SpecialtyLockTracker
    {
        public const int LockCount = 6;

        public PlayerId Owner = PlayerId.An;
        public int RevealedLocks;
        public int CompletedMask;

        // Bộ đếm theo đúng điều kiện GDD 10.8 (Khoa | An)
        public int MajorWavesSurvived;       // khóa 1: 10 đợt lớn (Khoa: nhà chính không bị phá)
        public int ManualCleanings;          // Khoa khóa 2: tự tay dọn vệ sinh 150 lần
        public int IntelQuestions;           // An khóa 2: xây Bàn Tình Báo và dùng 40 lượt hỏi
        public bool IntelDeskBuilt;
        public int HerdStateStreakDays;      // Khoa khóa 3: giữ Bầy Đàn liên tục 10 ngày
        public bool BigF2HerdReached;        // An khóa 3: >= 150 con cùng lúc, >= 20% F2
        public int RareQuyLinesIdentified;   // Khoa khóa 4: định danh cả 2 dòng Quý
        public int LinesIdentified;          // An khóa 4: định danh 6 dòng ...
        public bool LegendOrDeviantIdentified; // ... >= 1 Huyền Thoại hoặc Dị Biến
        public int QuarantinedDayMarketSales; // Khoa khóa 5: bán 500 heo đã kiểm dịch ở chợ ngày
        public int NightMarketBarters;       // An khóa 5: 30 giao dịch trao đổi ở chợ đêm
        public int TransmissionSuccessions;  // Khoa khóa 6: 1 lần kế vị nhận tầng Truyền Thừa
        public int IntelFiveOccultEvents;    // An khóa 6: 3 sự kiện huyền bí đã có tình báo cấp 5

        public List<GeneLineId> IdentifiedLines = new List<GeneLineId>();
        public int LastSeenMajorWaves;
        public int LastSeenTransmissionStacks;

        public int CompletedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < LockCount; i++) if ((CompletedMask & (1 << i)) != 0) n++;
                return n;
            }
        }

        public bool AllComplete => CompletedCount >= LockCount;
        public bool IsRevealed(int lockNumber) => lockNumber >= 1 && lockNumber <= RevealedLocks;
        public bool IsCompleted(int lockNumber) => lockNumber >= 1 && lockNumber <= LockCount && (CompletedMask & (1 << (lockNumber - 1))) != 0;

        /// <summary>Cộng tiến độ; khi khóa chưa lộ chỉ công nhận tối đa 50% yêu cầu (GDD 10.4).</summary>
        private int Add(int current, int lockNumber, int required, int amount)
        {
            int next = current + Math.Max(0, amount);
            return IsRevealed(lockNumber) ? next : Math.Min(next, required / 2);
        }

        // Tổng thật (không bị chặn bởi luật lộ khóa) — dùng cho nhiệm vụ riêng của NPC (ADR-0026)
        public int TotalManualCleanings, TotalQuarantinedSales, TotalNightBarters;

        public void RecordManualCleaning(int count = 1) { ManualCleanings = Add(ManualCleanings, 2, 150, count); TotalManualCleanings += Math.Max(0, count); }
        public void RecordIntelQuestion(int count = 1) => IntelQuestions = Add(IntelQuestions, 2, 40, count);
        public void RecordQuarantinedDayMarketSale(int count = 1) { QuarantinedDayMarketSales = Add(QuarantinedDayMarketSales, 5, 500, count); TotalQuarantinedSales += Math.Max(0, count); }
        public void RecordNightMarketBarter(int count = 1) { NightMarketBarters = Add(NightMarketBarters, 5, 30, count); TotalNightBarters += Math.Max(0, count); }
        public void RecordOccultEventResolved(int intelTier)
        {
            if (intelTier >= 5) IntelFiveOccultEvents = Add(IntelFiveOccultEvents, 6, 3, 1);
        }

        /// <summary>Quan sát trạng thái trại một lần mỗi ngày (GameEngine gọi ở lúc qua ngày).</summary>
        public void ObserveDay(GameEngine engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            Owner = engine.Character.Identity;

            int waves = engine.MajorWavesSurvived;
            if (waves > LastSeenMajorWaves) MajorWavesSurvived = Add(MajorWavesSurvived, 1, 10, waves - LastSeenMajorWaves);
            LastSeenMajorWaves = waves;

            if (engine.Herd.Tier != HerdStateTier.KhongCo) HerdStateStreakDays = Add(HerdStateStreakDays, 3, 10, 1);
            else if (!IsCompleted(3)) HerdStateStreakDays = 0;

            int stacks = engine.Herd.TransmissionStacks;
            if (stacks > LastSeenTransmissionStacks) TransmissionSuccessions = Add(TransmissionSuccessions, 6, 1, stacks - LastSeenTransmissionStacks);
            LastSeenTransmissionStacks = stacks;

            IntelDeskBuilt = engine.Defense.Buildings.Exists(b => b != null && b.BlueprintId == "BanTinhBao" && !b.IsBroken);

            int living = 0, f2 = 0;
            foreach (var p in engine.Pigs)
            {
                if (p == null || !p.IsAlive) continue;
                living++;
                if (p.Generation >= 2) f2++;
                if (!p.IsIdentified) continue;
                if (!IdentifiedLines.Contains(p.GeneLine))
                {
                    IdentifiedLines.Add(p.GeneLine);
                    LinesIdentified = Add(LinesIdentified, 4, 6, 1);
                    if (p.GeneLine == GeneLineId.LoiMach || p.GeneLine == GeneLineId.MongNhan)
                        RareQuyLinesIdentified = Add(RareQuyLinesIdentified, 4, 2, 1);
                }
                if (IsRevealed(4) && p.Rarity >= GeneRarity.HuyenThoai) LegendOrDeviantIdentified = true;
            }
            if (IsRevealed(3) && living >= 150 && f2 * 5 >= living) BigF2HerdReached = true;
        }

        /// <summary>Đánh dấu hoàn thành các khóa đã lộ đủ điều kiện; trả về danh sách khóa vừa xong.</summary>
        public List<int> EvaluateCompletion()
        {
            var done = new List<int>();
            for (int n = 1; n <= RevealedLocks && n <= LockCount; n++)
            {
                if (IsCompleted(n) || !ConditionMet(n)) continue;
                CompletedMask |= 1 << (n - 1);
                done.Add(n);
            }
            return done;
        }

        private bool ConditionMet(int n)
        {
            if (Owner == PlayerId.Khoa)
            {
                return n switch
                {
                    1 => MajorWavesSurvived >= 10,
                    2 => ManualCleanings >= 150,
                    3 => HerdStateStreakDays >= 10,
                    4 => RareQuyLinesIdentified >= 2,
                    5 => QuarantinedDayMarketSales >= 500,
                    6 => TransmissionSuccessions >= 1,
                    _ => false
                };
            }
            return n switch
            {
                1 => MajorWavesSurvived >= 10,
                2 => IntelDeskBuilt && IntelQuestions >= 40,
                3 => BigF2HerdReached,
                4 => LinesIdentified >= 6 && LegendOrDeviantIdentified,
                5 => NightMarketBarters >= 30,
                6 => IntelFiveOccultEvents >= 3,
                _ => false
            };
        }

        /// <summary>(tiến độ, yêu cầu) của khóa n cho chủ hiện tại; chỉ nên hiển thị khi khóa đã lộ.</summary>
        public (int progress, int required) Progress(int n)
        {
            if (Owner == PlayerId.Khoa)
            {
                return n switch
                {
                    1 => (MajorWavesSurvived, 10),
                    2 => (ManualCleanings, 150),
                    3 => (HerdStateStreakDays, 10),
                    4 => (RareQuyLinesIdentified, 2),
                    5 => (QuarantinedDayMarketSales, 500),
                    6 => (TransmissionSuccessions, 1),
                    _ => (0, 0)
                };
            }
            return n switch
            {
                1 => (MajorWavesSurvived, 10),
                2 => (IntelDeskBuilt ? IntelQuestions : 0, 40),
                3 => (BigF2HerdReached ? 1 : 0, 1),
                4 => (LegendOrDeviantIdentified ? LinesIdentified : Math.Min(LinesIdentified, 5), 6),
                5 => (NightMarketBarters, 30),
                6 => (IntelFiveOccultEvents, 3),
                _ => (0, 0)
            };
        }

        /// <summary>
        /// Dùng 1 Giấy Chuyên Biệt (GDD 10.4–10.5): số lượt theo màu/cấp; mỗi lượt nhắm khóa thấp nhất chưa lộ.
        /// Trả về số khóa vừa lộ và thông báo (chỉ lộ ĐIỀU KIỆN; tên/hiệu ứng vẫn "???").
        /// </summary>
        public (int revealed, string message) UsePaper(SpecialtyPaperColor color, int enhancementLevel, Func<double> roll)
        {
            if (roll == null) throw new ArgumentNullException(nameof(roll));
            if (RevealedLocks >= LockCount) return (0, "Cả 6 khóa đã lộ điều kiện.");
            var (guaranteed, extraChance) = CanonicalSagaAndGdd6Systems.GetSpecialtyPaperExtraRollSpecGdd105(color, enhancementLevel);
            int rolls = guaranteed + (extraChance > 0f && roll() < extraChance ? 1 : 0);
            int revealed = 0;
            var lines = new List<string>();
            for (int i = 0; i < rolls && RevealedLocks < LockCount; i++)
            {
                int target = RevealedLocks + 1;
                float rate = CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(color, target, enhancementLevel);
                if (rate > 0f && roll() < rate)
                {
                    RevealedLocks = target;
                    revealed++;
                    lines.Add($"Khóa {target} lộ: {ConditionText(Owner, target)}");
                }
                else lines.Add($"Lượt {i + 1}: sịt (khóa {target}).");
            }
            return (revealed, string.Join("\n", lines));
        }

        public static string ConditionText(PlayerId owner, int lockNumber)
        {
            foreach (var spec in CanonicalSagaAndGdd6Systems.KhoaAndAnTwelveSpecialtyLocksGdd108)
                if (spec.CharacterIdentity == owner && spec.LockNumber == lockNumber) return spec.UnlockConditionSummary;
            return "???";
        }

        public SpecialtyLockTracker Clone()
        {
            var c = (SpecialtyLockTracker)MemberwiseClone();
            c.IdentifiedLines = new List<GeneLineId>(IdentifiedLines ?? new List<GeneLineId>());
            return c;
        }
    }
}
