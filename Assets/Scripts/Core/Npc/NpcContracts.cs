using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>Phẩm chất = vai trò theo độ trọng yếu (ADR-0026 §2), không phải độ hiếm hay sức mạnh.</summary>
    public enum RoleTier { Trang, Luc, Lam, Tim, Cam }

    /// <summary>Hai kiểu hợp đồng người dùng chốt: theo chu kỳ (nhỏ / lớn) hoặc theo đợt. Không có kiểu theo ngày.</summary>
    public enum ContractKind { ChuKyNho, ChuKyLon, TheoDot }

    /// <summary>Thước đo nhiệm vụ riêng và việc nhờ của từng NPC (việc thật trong game).</summary>
    public enum NpcMetric
    {
        ManualCleanings, FeedStockKg, QuarantinedSales, NightBarters, ContractedNpcs, StructuresOwned,
        IdentifiedLines, MajorWaves, MinorWaves, EliteKills, UfoKills, ThiefKills
    }

    public sealed class NpcProfile
    {
        public NpcSpecialistId Id;
        public string Role = "";
        public RoleTier Tier;
        public float Criticality;
        public bool IsMercenary;
        /// <summary>Giá nền mỗi chu kỳ nhỏ (NPC trại: lương GDD × 8; lính: điểm × 655G). Cam dùng <see cref="DailyRate"/>.</summary>
        public int BasePricePerCycle;
        /// <summary>Cam: giá nền mỗi ngày (GDD 11.1) cho hợp đồng chu kỳ lớn.</summary>
        public int DailyRate;
        public NpcMetric Metric;
        /// <summary>Nhiệm vụ riêng 3 bước: bước 1 mở thanh hảo cảm (lính: mở quyền thuê), bước 2 / 3 mở cổng 50 / 80.</summary>
        public int[] QuestTargets = new int[3];
        public string[] QuestTitles = new string[3];
        /// <summary>Việc nhờ mỗi chu kỳ (+5 hảo cảm gốc): thêm chừng này đơn vị thước đo trong chu kỳ (hoặc đạt mức, với thước đo trạng thái).</summary>
        public int FavorTarget;
        public string FavorText = "";
    }

    public sealed class NpcRelation
    {
        public NpcSpecialistId Id;
        public int QuestStage;            // 0 = chưa làm nhiệm vụ riêng; 1–3 = đã xong bước đó
        public float Affinity;            // 0–100, chỉ có nghĩa khi QuestStage ≥ 1
        public int TermsCompleted;        // kì hợp đồng làm trọn → thị trường công nhận năng lực
        public int FavorDoneCycle;        // chu kỳ đã làm việc nhờ (1 lần/chu kỳ)
        public int FavorBaselineCycle;
        public int FavorBaseline;
        public bool Unlocked => QuestStage >= 1;
    }

    public sealed class NpcContract
    {
        public NpcSpecialistId Id;
        public ContractKind Kind;
        public int Terms;                 // số chu kỳ hoặc số đợt
        public int TotalPrice;
        public int Deposit;
        public int InstallmentsLeft;      // chu kỳ: số lần trả còn lại; theo đợt: số đợt còn lại
        public int InstallmentAmount;     // chu kỳ: mỗi lần trả; theo đợt: giá mỗi đợt
        public int NextDueDay;            // chu kỳ lớn: ngày trả kế tiếp
        public int[] LargeCycleDays = Array.Empty<int>();
        public int StartDay;
        public bool TrustSkipUsed;        // mốc 100: bỏ qua 1 lần trễ thanh toán
        /// <summary>Đơn giá đã chốt khi ký / gia hạn (chu kỳ nhỏ: G/chu kỳ; chu kỳ lớn: G/ngày; theo đợt: G/đợt).</summary>
        public int UnitPrice;
        /// <summary>Đã trả qua các kì (không tính cọc) — kì cuối trả đúng phần còn lại, không làm tròn dư.</summary>
        public int PaidInstallments;
    }

    /// <summary>State that lives in the engine and in the save.</summary>
    public sealed class NpcRelationsState
    {
        public Dictionary<NpcSpecialistId, NpcRelation> Relations { get; } = new Dictionary<NpcSpecialistId, NpcRelation>();
        public List<NpcContract> Contracts { get; } = new List<NpcContract>();
        public Dictionary<NpcMetric, int> Counters { get; } = new Dictionary<NpcMetric, int>();

        public NpcRelation Get(NpcSpecialistId id)
        {
            if (!Relations.TryGetValue(id, out var r)) Relations[id] = r = new NpcRelation { Id = id };
            return r;
        }

        public NpcContract? ContractOf(NpcSpecialistId id) => Contracts.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    /// ADR-0026 — thuê NPC theo hợp đồng có kì hạn:
    /// nhiệm vụ riêng → (NPC thường) thanh hảo cảm → ngưỡng thuê theo phẩm chất (tối đa 30) → hợp đồng theo chu kỳ (cọc 30%) hoặc theo đợt (lính).
    /// Hảo cảm 30/50/80/100 khó dần; ≥ 50 tăng hiệu suất và là điều kiện mở đặc tính. Giá định theo năng lực, đổi theo thị trường mỗi chu kỳ.
    /// </summary>
    public static class NpcContracts
    {
        public const float DepositRatio = 0.30f;
        public const float WavesPerCycle = 2.5f;              // GDD 2.3: 1 đợt lớn + 1–2 đợt nhỏ / chu kỳ giữa game
        public const float RecognitionPerTerm = 0.03f;        // TẠM (ADR-0026 §4)
        public const float RecognitionCap = 0.30f;            // TẠM
        public const int MinLargeCycles = 2;
        /// <summary>Người dùng 02/11: hảo cảm cao giữ được giá cũ khi gia hạn dù thị trường đã định giá NPC cao hơn. Mốc 50 (TẠM, ADR-0026).</summary>
        public const float PriceLockAffinity = 50f;
        /// <summary>Ép giá xuống theo thị trường khi giá thị trường rớt: không cần hảo cảm nhưng hảo cảm −5 (TẠM).</summary>
        public const float ForceDownAffinityLoss = 5f;
        public static readonly int[] WaveTermOptions = { 3, 5, 8 };

        static NpcProfile P(NpcSpecialistId id, string role, RoleTier tier, float crit, bool merc, int cyclePrice, int daily,
            NpcMetric m, int q1, int q2, int q3, string t1, string t2, string t3, int favor, string favorText) =>
            new NpcProfile
            {
                Id = id, Role = role, Tier = tier, Criticality = crit, IsMercenary = merc, BasePricePerCycle = cyclePrice, DailyRate = daily,
                Metric = m, QuestTargets = new[] { q1, q2, q3 }, QuestTitles = new[] { t1, t2, t3 }, FavorTarget = favor, FavorText = favorText,
            };

        /// <summary>ADR-0026 §2 (điểm trọng yếu, phẩm chất) và §4 (giá nền).</summary>
        public static readonly IReadOnlyList<NpcProfile> Profiles = new[]
        {
            P(NpcSpecialistId.NongCong, "Nông Công", RoleTier.Trang, 1.00f, false, 480, 0, NpcMetric.ManualCleanings, 10, 40, 120,
                "Dọn bãi 10 lần", "Dọn bãi 40 lần", "Dọn bãi 120 lần", 5, "dọn bãi thêm 5 lần trong chu kỳ này"),
            P(NpcSpecialistId.ThuKho, "Thủ Kho", RoleTier.Luc, 2.00f, false, 800, 0, NpcMetric.FeedStockKg, 200, 500, 1000,
                "Kho cám đạt 200 kg", "Kho cám đạt 500 kg", "Kho cám đạt 1.000 kg", 150, "giữ kho cám từ 150 kg trong chu kỳ này"),
            P(NpcSpecialistId.NhaBuon, "Nhà Buôn", RoleTier.Luc, 2.20f, false, 1200, 0, NpcMetric.NightBarters, 3, 15, 30,
                "Trao đổi 3 lần ở chợ đêm", "Trao đổi 15 lần ở chợ đêm", "Trao đổi 30 lần ở chợ đêm", 2, "trao đổi thêm 2 lần ở chợ đêm trong chu kỳ này"),
            P(NpcSpecialistId.QuanLyCa, "Quản Lý Ca", RoleTier.Tim, 3.55f, false, 960, 0, NpcMetric.ContractedNpcs, 2, 3, 4,
                "Có 2 NPC đang làm theo hợp đồng", "Có 3 NPC đang làm theo hợp đồng", "Có 4 NPC đang làm theo hợp đồng", 2, "giữ 2 NPC làm việc trong chu kỳ này"),
            P(NpcSpecialistId.BacSiThuY, "Bác Sĩ Thú Y", RoleTier.Tim, 3.45f, false, 1280, 0, NpcMetric.QuarantinedSales, 5, 40, 150,
                "Bán 5 heo đã kiểm dịch", "Bán 40 heo đã kiểm dịch", "Bán 150 heo đã kiểm dịch", 5, "bán thêm 5 heo đã kiểm dịch trong chu kỳ này"),
            P(NpcSpecialistId.KySuXayDung, "Kỹ Sư Xây Dựng", RoleTier.Cam, 4.50f, false, 1600, 200, NpcMetric.StructuresOwned, 3, 8, 15,
                "Có 3 công trình phòng thủ", "Có 8 công trình phòng thủ", "Có 15 công trình phòng thủ", 4, "giữ 4 công trình phòng thủ còn đứng trong chu kỳ này"),
            P(NpcSpecialistId.NhaNghienCuuGen, "Nhà Nghiên Cứu Gen", RoleTier.Cam, 4.50f, false, 1760, 220, NpcMetric.IdentifiedLines, 2, 4, 6,
                "Định danh 2 dòng gen", "Định danh 4 dòng gen", "Định danh 6 dòng gen", 2, "đã định danh từ 2 dòng gen khi chu kỳ này kết thúc"),
            P(NpcSpecialistId.VeBinhKhien, "Vệ Binh Khiên", RoleTier.Luc, 2.25f, true, 1473, 0, NpcMetric.MajorWaves, 2, 6, 12,
                "Sống sót 2 đợt lớn", "Sống sót 6 đợt lớn", "Sống sót 12 đợt lớn", 1, "sống sót đợt lớn của chu kỳ này"),
            P(NpcSpecialistId.KySuChienTruong, "Kỹ Sư Chiến Trường", RoleTier.Luc, 2.50f, true, 1640, 0, NpcMetric.MinorWaves, 3, 10, 25,
                "Đẩy lui 3 đợt nhỏ", "Đẩy lui 10 đợt nhỏ", "Đẩy lui 25 đợt nhỏ", 1, "đẩy lui 1 đợt nhỏ trong chu kỳ này"),
            P(NpcSpecialistId.XaThuNo, "Xạ Thủ Nỏ", RoleTier.Lam, 2.70f, true, 1770, 0, NpcMetric.EliteKills, 2, 10, 30,
                "Hạ 2 quái tinh anh", "Hạ 10 quái tinh anh", "Hạ 30 quái tinh anh", 1, "hạ 1 quái tinh anh trong chu kỳ này"),
            P(NpcSpecialistId.ThichKhachDem, "Thích Khách Đêm", RoleTier.Lam, 3.20f, true, 2100, 0, NpcMetric.ThiefKills, 5, 20, 60,
                "Hạ 5 quái trộm", "Hạ 20 quái trộm", "Hạ 60 quái trộm", 3, "hạ 3 quái trộm trong chu kỳ này"),
            P(NpcSpecialistId.ThoSanUfo, "Thợ Săn UFO", RoleTier.Tim, 3.75f, true, 2460, 0, NpcMetric.UfoKills, 1, 5, 15,
                "Hạ 1 UFO", "Hạ 5 UFO", "Hạ 15 UFO", 1, "hạ 1 UFO trong chu kỳ này"),
        };

        public static NpcProfile? Profile(NpcSpecialistId id) => Profiles.FirstOrDefault(p => p.Id == id);

        public static string TierName(RoleTier t) => t switch
        {
            RoleTier.Trang => "Trắng · Phổ thông",
            RoleTier.Luc => "Lục · Hậu cần",
            RoleTier.Lam => "Lam · Chuyên môn",
            RoleTier.Tim => "Tím · Then chốt",
            _ => "Cam · Trụ cột",
        };

        /// <summary>ADR-0026 §3: ngưỡng hảo cảm để thuê (cao nhất 30); lính chỉ cần xong nhiệm vụ mở đầu.</summary>
        public static int HireAffinity(NpcProfile p) => p.IsMercenary ? 0 : p.Tier switch
        {
            RoleTier.Trang => 10, RoleTier.Luc => 15, RoleTier.Lam => 20, RoleTier.Tim => 25, _ => 30,
        };

        /// <summary>GDD 11.1: số NPC tối đa (tính cả lính dài hạn) theo cấp trại.</summary>
        public static int MaxNpcs(int farmLevel) => farmLevel >= 15 ? 8 : farmLevel >= 10 ? 6 : farmLevel >= 5 ? 4 : 2;

        // ------------------------------------------------------------------ thước đo, nhiệm vụ riêng, việc nhờ
        public static int Metric(GameEngine e, NpcMetric m)
        {
            var locks = e.DeepMechanics.SpecialtyLocks;
            var st = e.NpcRelations;
            return m switch
            {
                NpcMetric.ManualCleanings => locks.TotalManualCleanings,
                NpcMetric.FeedStockKg => (int)e.Farm.FeedStockKg,
                NpcMetric.QuarantinedSales => locks.TotalQuarantinedSales,
                NpcMetric.NightBarters => locks.TotalNightBarters,
                NpcMetric.ContractedNpcs => st.Contracts.Count,
                NpcMetric.StructuresOwned => e.Defense.Buildings.Count(b => !b.IsBroken),
                NpcMetric.IdentifiedLines => locks.IdentifiedLines?.Count ?? 0,
                NpcMetric.MajorWaves => e.MajorWavesSurvived,
                NpcMetric.MinorWaves => e.MinorWavesDefeated,
                _ => st.Counters.TryGetValue(m, out var n) ? n : 0,
            };
        }

        static bool IsStateMetric(NpcMetric m) => m == NpcMetric.FeedStockKg || m == NpcMetric.ContractedNpcs || m == NpcMetric.StructuresOwned || m == NpcMetric.IdentifiedLines;

        /// <summary>Kill counters for the combat quests (called by the runtime when a monster dies).</summary>
        public static void RecordKill(GameEngine e, EnemyTypeId t, bool elite)
        {
            void Inc(NpcMetric m) { e.NpcRelations.Counters.TryGetValue(m, out var n); e.NpcRelations.Counters[m] = n + 1; }
            if (elite) Inc(NpcMetric.EliteKills);
            if (t == EnemyTypeId.UfoTrinhSat || t == EnemyTypeId.UfoThuHoach || t == EnemyTypeId.UfoPhaoHam || t == EnemyTypeId.UfoMothership) Inc(NpcMetric.UfoKills);
            if (t == EnemyTypeId.KeBatHeo || t == EnemyTypeId.DauLinhTrom || t == EnemyTypeId.KeCatDay || t == EnemyTypeId.ChuaTromRiuSat) Inc(NpcMetric.ThiefKills);
        }

        /// <summary>Checks the personal quest; completing a step grants its affinity and (step 1) opens the bar / the right to hire.</summary>
        public static string CheckQuest(GameEngine e, NpcSpecialistId id)
        {
            var p = Profile(id);
            if (p == null) return "";
            var r = e.NpcRelations.Get(id);
            if (r.QuestStage >= 3) return "";
            int value = Metric(e, p.Metric);
            if (value < p.QuestTargets[r.QuestStage]) return "";
            r.QuestStage++;
            AddAffinity(e, id, 10f);
            return r.QuestStage == 1
                ? (p.IsMercenary ? $"{p.Role}: xong nhiệm vụ riêng — có thể thuê bằng vàng." : $"{p.Role}: xong nhiệm vụ riêng — mở thanh hảo cảm.")
                : $"{p.Role}: xong nhiệm vụ riêng bước {r.QuestStage} — mở cổng hảo cảm {(r.QuestStage == 2 ? 50 : 80)}.";
        }

        /// <summary>Một việc nhờ mỗi chu kỳ (+5 gốc), đo bằng việc thật làm trong chu kỳ.</summary>
        public static string CheckFavor(GameEngine e, NpcSpecialistId id)
        {
            var p = Profile(id);
            if (p == null) return "";
            var r = e.NpcRelations.Get(id);
            if (!r.Unlocked || r.FavorDoneCycle == e.Clock.CycleNumber) return "";
            int value = Metric(e, p.Metric);
            if (r.FavorBaselineCycle != e.Clock.CycleNumber) { r.FavorBaselineCycle = e.Clock.CycleNumber; r.FavorBaseline = value; }
            bool done = IsStateMetric(p.Metric) ? value >= p.FavorTarget : value - r.FavorBaseline >= p.FavorTarget;
            if (!done) return "";
            r.FavorDoneCycle = e.Clock.CycleNumber;
            AddAffinity(e, id, 5f);
            return $"{p.Role}: xong việc nhờ ({p.FavorText}).";
        }

        // ------------------------------------------------------------------ hảo cảm
        /// <summary>ADR-0026 §3: hệ số theo dải (×1 / ×0,6 / ×0,35 / ×0,2) và cổng 50 / 80 theo nhiệm vụ riêng bước 2 / 3.</summary>
        public static float BandMultiplier(float affinity) => affinity < 30f ? 1f : affinity < 50f ? 0.6f : affinity < 80f ? 0.35f : 0.2f;

        public static float AffinityCap(NpcRelation r) => r.QuestStage >= 3 ? 100f : r.QuestStage >= 2 ? 79f : 49f;

        public static void AddAffinity(GameEngine e, NpcSpecialistId id, float basePoints)
        {
            var r = e.NpcRelations.Get(id);
            if (!r.Unlocked) return;
            if (basePoints < 0f) { r.Affinity = Math.Max(0f, r.Affinity + basePoints); return; }
            float cap = AffinityCap(r);
            float left = basePoints;
            // trừ dần qua từng dải để điểm vượt mốc vẫn chịu hệ số của dải mới
            while (left > 0.0001f && r.Affinity < cap)
            {
                float band = BandMultiplier(r.Affinity);
                float edge = r.Affinity < 30f ? 30f : r.Affinity < 50f ? 50f : r.Affinity < 80f ? 80f : 100f;
                float room = Math.Min(edge, cap) - r.Affinity;
                float gain = Math.Min(room, left * band);
                r.Affinity += gain;
                left -= gain / band;
                if (gain <= 0f) break;
            }
            r.Affinity = (float)Math.Round(Math.Min(cap, r.Affinity), 2);
        }

        /// <summary>Hiệu suất cộng thêm theo mốc (TẠM): 50 → +10%, 80 → +20%, 100 → +30%. Không đổi giá.</summary>
        public static float EfficiencyBonus(GameEngine e, NpcSpecialistId id)
        {
            var r = e.NpcRelations.Get(id);
            if (!r.Unlocked) return 0f;
            return r.Affinity >= 100f ? 0.30f : r.Affinity >= 80f ? 0.20f : r.Affinity >= 50f ? 0.10f : 0f;
        }

        /// <summary>Người dùng: thiếu 50 hảo cảm thì chưa mở được đặc tính, dù đã có giấy và xong 6 khóa.</summary>
        public static bool AffinityAllowsTrait(GameEngine e, NpcSpecialistId id) => e.NpcRelations.Get(id).Affinity >= 50f;

        // ------------------------------------------------------------------ giá
        /// <summary>Biến động theo chủ đề chợ (ADR-0026 §4, biên độ TẠM).</summary>
        static (float lo, float hi) ThemeRange(MarketTheme theme, NpcProfile p)
        {
            switch (theme)
            {
                case MarketTheme.HoangLoanDich when p.Id == NpcSpecialistId.BacSiThuY: return (0.25f, 0.40f);
                case MarketTheme.MuaCongTrinh when p.Id == NpcSpecialistId.KySuXayDung || p.Id == NpcSpecialistId.KySuChienTruong: return (0.15f, 0.25f);
                case MarketTheme.KhanHiemHauDotKich when p.IsMercenary:
                case MarketTheme.MuaTrangBi when p.IsMercenary: return (0.15f, 0.30f);
                case MarketTheme.ConSotThit when p.Id == NpcSpecialistId.NhaBuon: return (0.15f, 0.25f);
                case MarketTheme.MuaHeoRe when p.Id == NpcSpecialistId.NhaBuon: return (-0.20f, -0.10f);
                case MarketTheme.KhanHiemThucAn when p.Id == NpcSpecialistId.ThuKho || p.Id == NpcSpecialistId.NongCong: return (0.10f, 0.20f);
                default: return (-0.05f, 0.05f);
            }
        }

        /// <summary>Hệ số thị trường của chu kỳ hiện tại (cố định trong cả chu kỳ, gieo theo ván + chu kỳ + NPC).</summary>
        public static float MarketFactor(GameEngine e, NpcProfile p)
        {
            var (lo, hi) = ThemeRange(e.Economy.DayMarket.Theme, p);
            var rng = new Random(unchecked(e.BaseSeed * 7919 + e.Clock.CycleNumber * 104729 + (int)p.Id * 31));
            float theme = lo + (float)rng.NextDouble() * (hi - lo);   // chủ đề không ảnh hưởng vai trò này: dao động nền ±5%
            float recognition = Math.Min(RecognitionCap, RecognitionPerTerm * e.NpcRelations.Get(p.Id).TermsCompleted);
            return (1f + recognition) * (1f + theme);
        }

        public static int CyclePrice(GameEngine e, NpcProfile p) => (int)Math.Round(p.BasePricePerCycle * MarketFactor(e, p));
        public static int DailyPrice(GameEngine e, NpcProfile p) => (int)Math.Round(p.DailyRate * MarketFactor(e, p));
        public static int WavePrice(GameEngine e, NpcProfile p) => (int)Math.Round(CyclePrice(e, p) / WavesPerCycle);

        public static bool KindAllowed(NpcProfile p, ContractKind k) =>
            p.Tier == RoleTier.Cam ? k == ContractKind.ChuKyLon
            : p.IsMercenary ? k == ContractKind.ChuKyNho || k == ContractKind.TheoDot
            : k == ContractKind.ChuKyNho;

        public sealed class Quote
        {
            public bool Ok;
            public string Reason = "";
            public int Total, Deposit, Installment, Installments;
            public int[] LargeCycleDays = Array.Empty<int>();
            public string Summary = "";
        }

        public static Quote QuoteFor(GameEngine e, NpcSpecialistId id, ContractKind kind, int terms)
        {
            var q = new Quote();
            var p = Profile(id);
            if (p == null) { q.Reason = "Không có người này."; return q; }
            if (!KindAllowed(p, kind)) { q.Reason = $"{p.Role} không nhận kiểu hợp đồng này."; return q; }
            if (kind == ContractKind.ChuKyLon && terms < MinLargeCycles) { q.Reason = $"{p.Role} chỉ nhận tối thiểu {MinLargeCycles} chu kỳ lớn."; return q; }
            if (kind == ContractKind.ChuKyNho && (terms < 1 || terms > 3)) { q.Reason = "Theo chu kỳ nhỏ: 1–3 chu kỳ."; return q; }
            if (kind == ContractKind.TheoDot && !WaveTermOptions.Contains(terms)) { q.Reason = "Theo đợt: 3, 5 hoặc 8 đợt."; return q; }
            switch (kind)
            {
                case ContractKind.ChuKyNho:
                {
                    int per = CyclePrice(e, p);
                    q.Total = per * terms;
                    q.Deposit = (int)Math.Round(q.Total * DepositRatio);
                    q.Installments = terms;
                    q.Installment = (int)Math.Ceiling((q.Total - q.Deposit) / (double)terms);
                    q.Summary = $"{terms} chu kỳ × {per:N0}G = {q.Total:N0}G · cọc {q.Deposit:N0}G · trả {q.Installment:N0}G mỗi đợt lớn";
                    break;
                }
                case ContractKind.ChuKyLon:
                {
                    var rng = new Random(unchecked(e.BaseSeed * 613 + e.Clock.CurrentDay * 97 + (int)id));
                    q.LargeCycleDays = Enumerable.Range(0, terms).Select(_ => 13 + rng.Next(3)).ToArray();
                    int daily = DailyPrice(e, p);
                    int days = q.LargeCycleDays.Sum();
                    q.Total = daily * days;
                    q.Deposit = (int)Math.Round(q.Total * DepositRatio);
                    q.Installments = terms;
                    q.Installment = (int)Math.Ceiling((q.Total - q.Deposit) / (double)terms);
                    q.Summary = $"{terms} chu kỳ lớn ({string.Join("+", q.LargeCycleDays)} = {days} ngày) × {daily:N0}G = {q.Total:N0}G · cọc {q.Deposit:N0}G · trả {q.Installment:N0}G cuối mỗi chu kỳ lớn";
                    break;
                }
                default:
                {
                    int wave = WavePrice(e, p);
                    q.Total = wave * terms;
                    q.Deposit = 0;
                    q.Installments = terms;
                    q.Installment = wave;
                    q.Summary = $"{terms} đợt × {wave:N0}G = {q.Total:N0}G · trả {wave:N0}G khi mỗi đợt kết thúc";
                    break;
                }
            }
            q.Ok = true;
            return q;
        }

        public static int MarketUnitPrice(GameEngine e, NpcProfile p, ContractKind kind) =>
            kind == ContractKind.ChuKyLon ? DailyPrice(e, p) : kind == ContractKind.TheoDot ? WavePrice(e, p) : CyclePrice(e, p);

        public enum RenewPricing { Locked, Market, KeptHigher, ForcedDown }

        /// <summary>
        /// Gia hạn hợp đồng đang chạy (người dùng 02/11):
        /// - thị trường định giá NPC cao hơn giá đang thuê: hảo cảm ≥ 50 giữ được giá cũ, dưới 50 phải theo giá thị trường;
        /// - thị trường rớt: muốn thì ép giá xuống theo thị trường (không cần hảo cảm, hảo cảm −5), không thì giữ giá cũ.
        /// </summary>
        public static (int unit, RenewPricing pricing) RenewalUnit(GameEngine e, NpcContract c, bool forceDown)
        {
            var p = Profile(c.Id)!;
            int market = MarketUnitPrice(e, p, c.Kind);
            if (market > c.UnitPrice)
                return e.NpcRelations.Get(c.Id).Affinity >= PriceLockAffinity ? (c.UnitPrice, RenewPricing.Locked) : (market, RenewPricing.Market);
            if (market < c.UnitPrice)
                return forceDown ? (market, RenewPricing.ForcedDown) : (c.UnitPrice, RenewPricing.KeptHigher);
            return (market, RenewPricing.Market);
        }

        public static (bool ok, string message) Renew(GameEngine e, NpcSpecialistId id, int terms, bool forceDown = false)
        {
            var c = e.NpcRelations.ContractOf(id);
            if (c == null) return (false, "Chưa có hợp đồng để gia hạn.");
            var p = Profile(id)!;
            if (c.Kind == ContractKind.ChuKyNho && (terms < 1 || terms > 3)) return (false, "Gia hạn theo chu kỳ nhỏ: 1–3 chu kỳ.");
            if (c.Kind == ContractKind.ChuKyLon && terms < 1) return (false, "Gia hạn ít nhất 1 chu kỳ lớn.");
            if (c.Kind == ContractKind.TheoDot && !WaveTermOptions.Contains(terms)) return (false, "Gia hạn theo đợt: 3, 5 hoặc 8 đợt.");
            var (unit, pricing) = RenewalUnit(e, c, forceDown);
            int[] addDays = Array.Empty<int>();
            int added;
            if (c.Kind == ContractKind.ChuKyLon)
            {
                var rng = new Random(unchecked(e.BaseSeed * 613 + e.Clock.CurrentDay * 97 + (int)id + c.Terms * 7));
                addDays = Enumerable.Range(0, terms).Select(_ => 13 + rng.Next(3)).ToArray();
                added = unit * addDays.Sum();
            }
            else added = unit * terms;
            int deposit = c.Kind == ContractKind.TheoDot ? 0 : (int)Math.Round(added * DepositRatio);
            if (deposit > 0 && !e.Ledger.TrySpend(deposit, LedgerReason.NpcContract, $"npc_renew_{id}_{e.Clock.CurrentDay}_{c.Terms}"))
                return (false, $"Không đủ vàng đặt cọc gia hạn {deposit:N0}G.");
            c.InstallmentsLeft += terms;
            c.Terms += terms;
            c.TotalPrice += added;
            c.Deposit += deposit;
            c.InstallmentAmount = (int)Math.Ceiling((c.TotalPrice - c.Deposit - c.PaidInstallments) / (double)c.InstallmentsLeft);
            c.UnitPrice = unit;
            if (addDays.Length > 0) c.LargeCycleDays = c.LargeCycleDays.Concat(addDays).ToArray();
            if (pricing == RenewPricing.ForcedDown) AddAffinity(e, id, -ForceDownAffinityLoss);
            string why = pricing switch
            {
                RenewPricing.Locked => $"giữ giá cũ nhờ hảo cảm ≥ {PriceLockAffinity:0} (thị trường đang trả {MarketUnitPrice(e, p, c.Kind):N0}G)",
                RenewPricing.ForcedDown => $"ép giá xuống theo thị trường, hảo cảm −{ForceDownAffinityLoss:0}",
                RenewPricing.KeptHigher => "giữ giá cũ (thị trường đang thấp hơn)",
                _ => "theo giá thị trường",
            };
            return (true, $"Gia hạn {p.Role} thêm {terms} {(c.Kind == ContractKind.TheoDot ? "đợt" : c.Kind == ContractKind.ChuKyLon ? "chu kỳ lớn" : "chu kỳ")} · {unit:N0}G/{(c.Kind == ContractKind.ChuKyLon ? "ngày" : c.Kind == ContractKind.TheoDot ? "đợt" : "chu kỳ")} — {why}" + (deposit > 0 ? $" · cọc {deposit:N0}G." : "."));
        }

        /// <summary>Why this NPC cannot be hired now ("" = can).</summary>
        public static string HireBlockedReason(GameEngine e, NpcSpecialistId id)
        {
            var p = Profile(id);
            if (p == null) return "Không có người này.";
            var r = e.NpcRelations.Get(id);
            if (!r.Unlocked) return $"Chưa xong nhiệm vụ riêng: {p.QuestTitles[0]} ({Metric(e, p.Metric)}/{p.QuestTargets[0]}).";
            int need = HireAffinity(p);
            if (r.Affinity < need) return $"Hảo cảm {r.Affinity:0}/{need} — chưa đủ để thuê.";
            if (e.NpcRelations.ContractOf(id) != null) return $"{p.Role} đang có hợp đồng.";
            int cap = MaxNpcs(e.Farm.FarmLevel);
            if (e.NpcRelations.Contracts.Count >= cap) return $"Đã đủ {cap} NPC cho cấp trại {e.Farm.FarmLevel}.";
            return "";
        }

        public static (bool ok, string message) Sign(GameEngine e, NpcSpecialistId id, ContractKind kind, int terms)
        {
            string block = HireBlockedReason(e, id);
            if (block.Length > 0) return (false, block);
            var q = QuoteFor(e, id, kind, terms);
            if (!q.Ok) return (false, q.Reason);
            var p = Profile(id)!;
            if (q.Deposit > 0 && !e.Ledger.TrySpend(q.Deposit, LedgerReason.NpcContract, $"npc_deposit_{id}_{e.Clock.CurrentDay}"))
                return (false, $"Không đủ vàng đặt cọc {q.Deposit:N0}G.");
            var c = new NpcContract
            {
                Id = id, Kind = kind, Terms = terms, TotalPrice = q.Total, Deposit = q.Deposit,
                InstallmentsLeft = q.Installments, InstallmentAmount = q.Installment, StartDay = e.Clock.CurrentDay,
                LargeCycleDays = q.LargeCycleDays,
                NextDueDay = kind == ContractKind.ChuKyLon ? e.Clock.CurrentDay + q.LargeCycleDays[0] : 0,
                UnitPrice = MarketUnitPrice(e, p, kind),
            };
            e.NpcRelations.Contracts.Add(c);
            if (e.PetsAndNpcs.HireNpc(id))
            {
                var rec = e.PetsAndNpcs.HiredNpcs.Find(n => n.Id == id);
                if (rec != null) rec.DailyWageGold = 0; // lương trả theo hợp đồng, không trừ theo ngày
            }
            e.SignalQuest(QuestSignal.NpcHired);
            return (true, $"Ký hợp đồng {p.Role}: {q.Summary}.");
        }

        static void EndContract(GameEngine e, NpcContract c, bool completed, string why, List<string> log)
        {
            var p = Profile(c.Id)!;
            e.NpcRelations.Contracts.Remove(c);
            e.PetsAndNpcs.HiredNpcs.RemoveAll(n => n.Id == c.Id);
            if (completed)
            {
                var r = e.NpcRelations.Get(c.Id);
                r.TermsCompleted++;
                AddAffinity(e, c.Id, 6f);
                log.Add($"{p.Role} làm trọn hợp đồng — thị trường công nhận năng lực (+{RecognitionPerTerm * 100f:0}% giá kì sau). Ký lại ở chợ.");
            }
            else log.Add($"{p.Role} nghỉ việc: {why}");
        }

        /// <summary>Chấm dứt sớm: mất cọc, hảo cảm −15.</summary>
        public static (bool ok, string message) Terminate(GameEngine e, NpcSpecialistId id)
        {
            var c = e.NpcRelations.ContractOf(id);
            if (c == null) return (false, "Không có hợp đồng.");
            AddAffinity(e, id, -15f);
            var log = new List<string>();
            EndContract(e, c, false, $"chấm dứt sớm — mất cọc {c.Deposit:N0}G, hảo cảm −15.", log);
            return (true, log[0]);
        }

        /// <summary>Thu một khoản đến hạn. Không đủ vàng: nghỉ việc, mất cọc, hảo cảm −10 (mốc 100 bỏ qua 1 lần).</summary>
        static bool Collect(GameEngine e, NpcContract c, int amount, List<string> log)
        {
            if (c.InstallmentsLeft <= 1) amount = Math.Max(0, c.TotalPrice - c.Deposit - c.PaidInstallments); // kì cuối: đúng phần còn lại
            if (amount <= 0) return true;
            if (e.Ledger.TrySpend(amount, LedgerReason.NpcContract, $"npc_{c.Id}_{e.Clock.CurrentDay}_{c.InstallmentsLeft}"))
            {
                c.PaidInstallments += amount;
                return true;
            }
            var r = e.NpcRelations.Get(c.Id);
            if (r.Affinity >= 100f && !c.TrustSkipUsed && c.InstallmentsLeft > 1)
            {
                // Tri kỷ: hoãn khoản này sang các kì sau (không xóa nợ)
                c.TrustSkipUsed = true;
                c.InstallmentAmount = (int)Math.Ceiling((c.TotalPrice - c.Deposit - c.PaidInstallments) / (double)(c.InstallmentsLeft - 1));
                log.Add($"{Profile(c.Id)!.Role} (Tri kỷ) cho hoãn {amount:N0}G sang các kì sau.");
                return true;
            }
            AddAffinity(e, c.Id, -10f);
            EndContract(e, c, false, $"không đủ {amount:N0}G đến hạn — mất cọc, hảo cảm −10.", log);
            return false;
        }

        /// <summary>Daily: quests, favours, large-cycle instalments.</summary>
        public static List<string> OnDay(GameEngine e)
        {
            var log = new List<string>();
            foreach (var p in Profiles)
            {
                string q = CheckQuest(e, p.Id);
                if (q.Length > 0) log.Add(q);
                string f = CheckFavor(e, p.Id);
                if (f.Length > 0) log.Add(f);
            }
            foreach (var c in e.NpcRelations.Contracts.ToArray())
            {
                if (c.Kind != ContractKind.ChuKyLon || e.Clock.CurrentDay < c.NextDueDay) continue;
                if (!Collect(e, c, c.InstallmentAmount, log)) continue;
                c.InstallmentsLeft--;
                int done = c.Terms - c.InstallmentsLeft;
                if (c.InstallmentsLeft <= 0) EndContract(e, c, true, "", log);
                else c.NextDueDay += c.LargeCycleDays[done];
            }
            return log;
        }

        /// <summary>After a wave: theo đợt pays and counts down (+1 hảo cảm for mercenaries); theo chu kỳ nhỏ settles at the major wave.</summary>
        public static List<string> OnWaveEnded(GameEngine e, bool major)
        {
            var log = new List<string>();
            foreach (var c in e.NpcRelations.Contracts.ToArray())
            {
                var p = Profile(c.Id)!;
                if (p.IsMercenary) AddAffinity(e, c.Id, 1f);
                if (c.Kind == ContractKind.TheoDot)
                {
                    if (!Collect(e, c, c.InstallmentAmount, log)) continue;
                    if (--c.InstallmentsLeft <= 0) EndContract(e, c, true, "", log);
                }
                else if (c.Kind == ContractKind.ChuKyNho && major)
                {
                    if (!Collect(e, c, c.InstallmentAmount, log)) continue;
                    if (--c.InstallmentsLeft <= 0) EndContract(e, c, true, "", log);
                }
            }
            return log;
        }

        /// <summary>One-line status for the recruit counter.</summary>
        public static string Status(GameEngine e, NpcSpecialistId id)
        {
            var p = Profile(id);
            if (p == null) return "";
            var r = e.NpcRelations.Get(id);
            var c = e.NpcRelations.ContractOf(id);
            if (c != null)
                return c.Kind == ContractKind.TheoDot ? $"Đang làm: còn {c.InstallmentsLeft} đợt" :
                       c.Kind == ContractKind.ChuKyLon ? $"Đang làm: còn {c.InstallmentsLeft} chu kỳ lớn (kì trả kế: ngày {c.NextDueDay})" :
                       $"Đang làm: còn {c.InstallmentsLeft} chu kỳ";
            if (!r.Unlocked) return $"Nhiệm vụ riêng: {p.QuestTitles[0]} ({Math.Min(Metric(e, p.Metric), p.QuestTargets[0])}/{p.QuestTargets[0]})";
            string next = r.QuestStage < 3 ? $" · bước {r.QuestStage + 1}: {p.QuestTitles[r.QuestStage]} ({Metric(e, p.Metric)}/{p.QuestTargets[r.QuestStage]})" : "";
            string aff = p.IsMercenary ? $"Hảo cảm {r.Affinity:0}" : $"Hảo cảm {r.Affinity:0}/{HireAffinity(p)} để thuê";
            return aff + next;
        }
    }
}
