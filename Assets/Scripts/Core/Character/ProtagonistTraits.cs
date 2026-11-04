using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>Trạng thái lưu của hai đặc tính cơ bản (GDD 8.1) và hiệu ứng "Ngủ Say" (GDD 10.8).</summary>
    [Serializable]
    public class ProtagonistTraitState
    {
        /// <summary>GameClock.TotalElapsedGameMinutes lần cuối Khoa huýt sáo; null = chưa dùng.</summary>
        public int? WhistleLastUsedGameMinute;
        /// <summary>Ngày nông trại An đã dùng lượt phát hiện bệnh tiềm ẩn miễn phí; -1 = chưa dùng.</summary>
        public int FreeScreeningDay = -1;
        /// <summary>Heo đã được An kiểm miễn phí: bán có kiểm dịch ở chợ ngày không mất phí 60G.</summary>
        public List<string> FreeScreenedPigIds = new List<string>();
        /// <summary>Dấu hiệu ẩn đã lộ qua Mắt Nhà Nghề, dạng "pigId|khóa".</summary>
        public List<string> RevealedSigns = new List<string>();
        /// <summary>Số lần ngủ chuẩn có "Ngủ Say" (Khoa sau đủ 6 khóa): mỗi 2 lần xóa 1 tầng Mệt Dội.</summary>
        public int DeepSleepNights;

        public ProtagonistTraitState Clone() => new ProtagonistTraitState
        {
            WhistleLastUsedGameMinute = WhistleLastUsedGameMinute,
            FreeScreeningDay = FreeScreeningDay,
            FreeScreenedPigIds = new List<string>(FreeScreenedPigIds ?? new List<string>()),
            RevealedSigns = new List<string>(RevealedSigns ?? new List<string>()),
            DeepSleepNights = DeepSleepNights,
        };
    }

    /// <summary>
    /// Hai đặc tính cơ bản mở ngay (GDD 8.1), chỉ phục vụ việc hằng ngày:
    /// - Khoa "Tiếng Huýt Chăn Đàn": tối đa 12 heo trong 20 m đi theo Khoa 60 giây thực, không tốn Stamina theo phút, hồi chiêu 2 giờ game.
    /// - An "Mắt Nhà Nghề": quan sát 1 con 5 giây để lộ 1 dấu hiệu sức khỏe / tính chất phụ bị ẩn;
    ///   mỗi ngày 1 lần miễn phí phát hiện bệnh tiềm ẩn của 1 con (thay cho kiểm dịch 60G).
    /// Lệnh "Lùa về Nơi Trú" (tù và, GDD 3.7) là hành động riêng: <see cref="GameEngine.CommandHerdRecallToRefuge"/>.
    /// </summary>
    public static class ProtagonistTraits
    {
        public const float WhistleRadiusMeters = 20f;
        public const int WhistleMaxPigs = 12;
        public const float WhistleFollowRealSeconds = 60f;
        public const int WhistleCooldownGameMinutes = 120;
        public const float ObserveRealSeconds = 5f;

        public static int WhistleCooldownMinutesLeft(GameEngine engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var last = engine.TraitState.WhistleLastUsedGameMinute;
            if (!last.HasValue) return 0;
            // Mệnh Ấn Mục Đồng (GDD 12.4): Tiếng Huýt / Mắt Nhà Nghề hồi chiêu −50%
            int cd = engine.Character.EngravedSeals.Contains("AnMucDong") ? WhistleCooldownGameMinutes / 2 : WhistleCooldownGameMinutes;
            return Math.Max(0, last.Value + cd - engine.Clock.TotalElapsedGameMinutes);
        }

        /// <summary>
        /// Khoa huýt sáo tại (x, y): chọn tối đa 12 heo sống, không cách ly, trong 20 m (gần nhất trước).
        /// Không trừ Stamina. Runtime cho các heo trả về đi theo Khoa trong <see cref="WhistleFollowRealSeconds"/> giây.
        /// </summary>
        public static (bool success, List<string> pigIds, string message) KhoaWhistle(GameEngine engine, float x, float y)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var none = new List<string>();
            if (engine.Character.Identity != PlayerId.Khoa)
                return (false, none, "Tiếng Huýt Chăn Đàn là đặc tính riêng của Khoa.");
            int left = WhistleCooldownMinutesLeft(engine);
            if (left > 0)
                return (false, none, $"Tiếng Huýt Chăn Đàn đang hồi: còn {left / 60}:{left % 60:00} giờ game.");

            var candidates = new List<(Pig pig, float d2)>();
            float r2 = WhistleRadiusMeters * WhistleRadiusMeters;
            foreach (var p in engine.Pigs)
            {
                if (p == null || !p.IsAlive || p.IsIsolated || p.IsInProcessingLot) continue;
                float dx = p.PositionX - x, dy = p.PositionY - y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= r2) candidates.Add((p, d2));
            }
            candidates.Sort((a, b) => a.d2.CompareTo(b.d2));
            var ids = new List<string>();
            for (int i = 0; i < candidates.Count && ids.Count < WhistleMaxPigs; i++) ids.Add(candidates[i].pig.Id);

            engine.TraitState.WhistleLastUsedGameMinute = engine.Clock.TotalElapsedGameMinutes;
            string msg = ids.Count > 0
                ? $"Khoa huýt sáo: {ids.Count} heo đi theo trong {WhistleFollowRealSeconds:0} giây."
                : "Khoa huýt sáo nhưng không có heo nào trong 20 m.";
            return (true, ids, msg);
        }

        // ------------------------------------------------------------------ An — Mắt Nhà Nghề

        public static bool HasLatentDisease(Pig pig) =>
            pig != null && (pig.ActiveDisease.HasValue || pig.LatentDiseaseRiskMultiplier > 1.05f);

        /// <summary>
        /// Sau 5 giây quan sát một con (runtime đo thời gian), lộ 1 dấu hiệu sức khỏe hoặc tính chất phụ còn ẩn.
        /// Thứ tự: mầm bệnh tiềm ẩn → sức đề kháng kém → từng tính chất phụ khi heo chưa giám định.
        /// </summary>
        public static (bool revealed, string message) AnObservePig(GameEngine engine, string pigId)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (engine.Character.Identity != PlayerId.An)
                return (false, "Mắt Nhà Nghề là đặc tính riêng của An.");
            var pig = engine.Pigs.Find(p => p != null && p.Id == pigId && p.IsAlive);
            if (pig == null) return (false, "Không thấy con heo này.");

            foreach (var (key, text) in HiddenSigns(pig))
            {
                string token = pig.Id + "|" + key;
                if (engine.TraitState.RevealedSigns.Contains(token)) continue;
                engine.TraitState.RevealedSigns.Add(token);
                return (true, $"Mắt Nhà Nghề — {pig.Name}: {text}");
            }
            return (false, $"Mắt Nhà Nghề — {pig.Name}: không còn dấu hiệu ẩn nào.");
        }

        /// <summary>Các dấu hiệu ẩn đang có trên con heo (khóa ổn định, mô tả tiếng Việt).</summary>
        public static IEnumerable<(string key, string text)> HiddenSigns(Pig pig)
        {
            if (pig.LatentDiseaseRiskMultiplier > 1.05f)
                yield return ("benh_tiem_an", $"có mầm bệnh tiềm ẩn (nguy cơ phát bệnh ×{pig.LatentDiseaseRiskMultiplier:0.##}).");
            if (pig.InfectionRiskMultiplier > 1.0f)
                yield return ("de_khang_kem", $"sức đề kháng kém, dễ nhiễm bệnh ×{pig.InfectionRiskMultiplier:0.##}.");
            if (!pig.IsIdentified && pig.Traits != null)
            {
                foreach (var t in pig.Traits)
                {
                    if (t == null || string.IsNullOrEmpty(t.Id)) continue;
                    yield return ("tinh_chat:" + t.Id, $"tính chất phụ ẩn [{t.Name}]{(t.IsSealed ? " (đã phong ấn)" : "")}.");
                }
            }
        }

        /// <summary>
        /// Mỗi ngày 1 lần: An phát hiện miễn phí bệnh tiềm ẩn của 1 con. Con được kiểm coi như đã kiểm dịch:
        /// bán có kiểm dịch ở chợ ngày không mất phí 60G.
        /// </summary>
        public static (bool success, bool diseaseFound, string message) AnFreeScreening(GameEngine engine, string pigId)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (engine.Character.Identity != PlayerId.An)
                return (false, false, "Mắt Nhà Nghề là đặc tính riêng của An.");
            var pig = engine.Pigs.Find(p => p != null && p.Id == pigId && p.IsAlive);
            if (pig == null) return (false, false, "Không thấy con heo này.");
            if (engine.TraitState.FreeScreeningDay == engine.Clock.CurrentDay)
                return (false, false, "Hôm nay An đã dùng lượt kiểm bệnh miễn phí.");

            engine.TraitState.FreeScreeningDay = engine.Clock.CurrentDay;
            if (!engine.TraitState.FreeScreenedPigIds.Contains(pig.Id)) engine.TraitState.FreeScreenedPigIds.Add(pig.Id);
            bool sick = HasLatentDisease(pig);
            if (sick && pig.LatentDiseaseRiskMultiplier > 1.05f)
                AddSign(engine, pig.Id, "benh_tiem_an");
            string msg = sick
                ? $"An kiểm {pig.Name}: PHÁT HIỆN bệnh tiềm ẩn! Nên cách ly trước khi bán."
                : $"An kiểm {pig.Name}: sạch bệnh. Bán có kiểm dịch hôm nay không mất phí.";
            return (true, sick, msg);
        }

        public static bool FreeScreeningAvailable(GameEngine engine) =>
            engine != null && engine.Character.Identity == PlayerId.An && engine.TraitState.FreeScreeningDay != engine.Clock.CurrentDay;

        public static bool IsQuarantineFeeWaived(GameEngine engine, string pigId) =>
            engine != null && engine.TraitState.FreeScreenedPigIds.Contains(pigId);

        /// <summary>Gọi khi bán có kiểm dịch: true nếu con này đã được An kiểm miễn phí (phí 60G được miễn).</summary>
        public static bool TryConsumeFreeQuarantine(GameEngine engine, string pigId) =>
            engine != null && engine.TraitState.FreeScreenedPigIds.Remove(pigId);

        private static void AddSign(GameEngine engine, string pigId, string key)
        {
            string token = pigId + "|" + key;
            if (!engine.TraitState.RevealedSigns.Contains(token)) engine.TraitState.RevealedSigns.Add(token);
        }
    }
}
