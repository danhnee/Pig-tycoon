using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum MonsterMoveKind { Basic, Special }

    /// <summary>
    /// One monster attack. Thiết kế người dùng (02/11):
    /// - Đòn cơ bản né được (lướt né có miễn sát thương). Đỡ chủ động giảm hao Chống Chịu.
    /// - Đòn đặc biệt KHÔNG né được, chỉ phản kích được, tỉ lệ gốc 5%, tăng nhờ kỹ năng thuần của người chơi.
    /// - Rút Thể lực là cơ chế của từng đòn, không theo hạng quái.
    /// </summary>
    public sealed class MonsterMove
    {
        public string Name = "";
        public MonsterMoveKind Kind;
        public float DamageMultiplier = 1f;
        /// <summary>Thời gian báo trước (giây thực) từ lúc quái lấy đà đến lúc đòn chạm.</summary>
        public float TelegraphSeconds = 0.18f;
        /// <summary>Phần Thể lực bị rút = sát thương sau giảm trừ × hệ số này (0 = đòn không rút sức).</summary>
        public float StaminaDrainRatio;
        /// <summary>Hồi chiêu riêng của đòn đặc biệt (giây thực).</summary>
        public float CooldownSeconds;
        public string Text = "";
    }

    /// <summary>
    /// Bộ đòn của từng loại quái, dựng theo hành vi trong GDD 19.1–19.2 (ai trói, ai húc, ai gọi sét, ai làm lạnh...).
    /// Số liệu là thiết kế tạm (ADR-0024).
    /// </summary>
    public static class MonsterMovesets
    {
        static MonsterMove B(string name, float drain = 0f, string text = "") =>
            new MonsterMove { Name = name, Kind = MonsterMoveKind.Basic, TelegraphSeconds = 0.18f, StaminaDrainRatio = drain, Text = text };

        static MonsterMove S(string name, float dmg, float telegraph, float drain, float cd, string text) =>
            new MonsterMove { Name = name, Kind = MonsterMoveKind.Special, DamageMultiplier = dmg, TelegraphSeconds = telegraph, StaminaDrainRatio = drain, CooldownSeconds = cd, Text = text };

        static readonly Dictionary<EnemyTypeId, MonsterMove[]> Table = new Dictionary<EnemyTypeId, MonsterMove[]>
        {
            [EnemyTypeId.ChuotGamKho] = new[] { B("Gặm") },
            [EnemyTypeId.KeCatDay] = new[] { B("Cứa"), S("Quất dây điện", 1.4f, 0.55f, 0.8f, 9f, "Dây điện quất trúng: tê người, rút sức") },
            [EnemyTypeId.KeBatHeo] = new[] { B("Đẩy"), S("Quăng thòng lọng", 1.2f, 0.6f, 1.0f, 10f, "Thòng lọng siết: vùng vẫy rút sức") },
            [EnemyTypeId.KeCaoPha] = new[] { B("Cào"), S("Cào xé", 1.6f, 0.5f, 0f, 8f, "Cú cào xé rách giáp") },
            [EnemyTypeId.BayNhay] = new[] { B("Cắn", 0.3f, "Cắn liên tục, rút sức") },
            [EnemyTypeId.ThietGiapCongThanh] = new[] { B("Húc"), S("Nện công thành", 2.0f, 0.75f, 0.6f, 10f, "Cú nện làm choáng váng, hụt hơi") },
            [EnemyTypeId.UfoTrinhSat] = new[] { B("Tia dò") },
            [EnemyTypeId.UfoThuHoach] = new[] { B("Tia kéo", 0.4f, "Chùm kéo hút cạn sức") },
            [EnemyTypeId.UfoPhaoHam] = new[] { B("Đạn plasma"), S("Plasma tụ lực", 1.8f, 0.8f, 0f, 12f, "Phát plasma tụ lực") },
            [EnemyTypeId.BongHuyen] = new[] { B("Chạm lạnh", 0.6f, "Cái lạnh huyền bí rút sức"), S("Xuyên hồn", 1.5f, 0.6f, 1.2f, 11f, "Bóng xuyên qua người, rút cạn sức") },
            [EnemyTypeId.ChoSuong] = new[] { B("Cắn"), S("Vồ trong sương", 1.5f, 0.5f, 0.3f, 8f, "Lao ra từ sương") },
            [EnemyTypeId.ThuBao] = new[] { B("Vả"), S("Gọi sét", 1.7f, 0.7f, 0.8f, 10f, "Sét đánh xuống: tê liệt, rút sức") },
            [EnemyTypeId.DauLinhTrom] = new[] { B("Chém"), S("Rìu quét", 1.8f, 0.6f, 0f, 9f, "Rìu quét ngang") },
            [EnemyTypeId.KeMangDich] = new[] { B("Phun dịch", 0.5f, "Mầm bệnh làm mệt lả"), S("Ói dịch", 1.4f, 0.65f, 1.0f, 12f, "Bãi dịch đặc rút sức") },
            [EnemyTypeId.AlienGrunt] = new[] { B("Bắn") },
            [EnemyTypeId.AlienElite] = new[] { B("Trượng"), S("Sóng trượng", 1.6f, 0.6f, 0.5f, 9f, "Sóng năng lượng") },
            [EnemyTypeId.ChuaTromRiuSat] = new[] { B("Rìu"), S("Rìu Sắt bổ", 2.5f, 0.8f, 0.8f, 8f, "Nhát bổ Rìu Sắt") },
            [EnemyTypeId.UfoMothership] = new[] { B("Pháo"), S("Chùm kéo lớn", 2.0f, 0.9f, 1.2f, 12f, "Chùm kéo lớn hút sức") },
            [EnemyTypeId.ThucTheSuong] = new[] { B("Sương quất", 0.4f, "Sương lạnh rút sức"), S("Sương nuốt", 2.2f, 0.9f, 1.5f, 12f, "Sương nuốt trọn") },
            [EnemyTypeId.BossAlienWarlord] = new[] { B("Đấm"), S("Sóng xung kích", 2.2f, 0.8f, 0.6f, 10f, "Sóng xung kích") },
        };

        static readonly MonsterMove[] Fallback = { B("Đánh") };

        public static MonsterMove[] For(EnemyTypeId type) => Table.TryGetValue(type, out var m) ? m : Fallback;

        /// <summary>Đặc biệt được tung khi hết hồi chiêu với xác suất 35% mỗi lần ra đòn (ADR-0024); còn lại đòn cơ bản.</summary>
        public static MonsterMove Pick(EnemyTypeId type, double now, ref double specialReadyAt, double roll)
        {
            var moves = For(type);
            MonsterMove? special = null;
            foreach (var m in moves) if (m.Kind == MonsterMoveKind.Special) { special = m; break; }
            if (special != null && now >= specialReadyAt && roll < 0.35)
            {
                specialReadyAt = now + special.CooldownSeconds;
                return special;
            }
            foreach (var m in moves) if (m.Kind == MonsterMoveKind.Basic) return m;
            return moves[0];
        }
    }

    /// <summary>
    /// Tỉ lệ phản kích đòn đặc biệt: gốc 5% (người dùng chốt), cộng thêm hoàn toàn từ kỹ năng thuần của người chơi:
    /// căn đúng khoảnh khắc đòn chạm (căn chuẩn animation), phản ứng đúng lúc đòn đã báo (không bấm sẵn / bấm loạn),
    /// chuỗi phản kích thành công liên tiếp, và kỹ năng Phản Chấn đang mở. Trần 90%. Số cộng là thiết kế tạm (ADR-0024).
    /// </summary>
    public static class CounterResolver
    {
        public const float BaseChance = 0.05f;
        public const float MaxChance = 0.90f;

        /// <param name="pressToImpactSeconds">|lúc bấm − lúc đòn chạm|.</param>
        /// <param name="pressedAfterTelegraph">bấm sau khi quái đã lấy đà (đọc đòn), không bấm trước.</param>
        /// <param name="pressesDuringTelegraph">số lần bấm trong lúc báo đòn (bấm loạn &gt; 1 thì mất mọi điểm cộng).</param>
        public static float Chance(float pressToImpactSeconds, bool pressedAfterTelegraph, int pressesDuringTelegraph, int successStreak, bool phanChanActive)
        {
            if (!pressedAfterTelegraph || pressesDuringTelegraph > 1) return BaseChance;
            float c = BaseChance;
            if (pressToImpactSeconds <= 0.08f) c += 0.45f;       // khớp đúng khung đòn chạm
            else if (pressToImpactSeconds <= 0.15f) c += 0.30f;
            else if (pressToImpactSeconds <= 0.25f) c += 0.15f;
            c += 0.10f;                                           // đọc đòn: bấm một lần, sau khi quái lấy đà
            c += Math.Min(3, Math.Max(0, successStreak)) * 0.05f; // chuỗi phản kích liên tiếp
            if (phanChanActive) c += 0.20f;                       // kỹ năng Phản Chấn đang mở cửa sổ
            return Math.Min(MaxChance, c);
        }
    }
}
