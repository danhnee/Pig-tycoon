using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum MercenaryTargeting { Nearest, PreferElite, AirOnly, StealthAndThieves }

    /// <summary>
    /// Lính đánh thuê trong trận (GDD 11.2, bảng 5 lính). HP, sát thương, tầm và hành vi lấy đúng bảng.
    /// GDD không ghi nhịp đánh và mức hao Stamina mỗi đòn → hai số đó là TẠM (ADR-0025).
    /// </summary>
    public sealed class MercenarySpec
    {
        public NpcSpecialistId Id;
        public string Role = "";
        public string SpriteFamily = "";
        public float MaxHp;
        public float Damage;
        public float RangeMeters;
        public bool Melee;
        public MercenaryTargeting Targeting;
        public string Signature = "";
    }

    public static class MercenarySpecs
    {
        /// <summary>GDD 11.2: Stamina riêng 150; hết thì rút về Cờ Tập Kết 8 giây.</summary>
        public const float MaxStamina = 150f;
        public const float RetreatSeconds = 8f;
        /// <summary>TẠM (ADR-0025): GDD không ghi nhịp đánh của lính.</summary>
        public const float AttackIntervalSeconds = 1.0f;
        /// <summary>TẠM (ADR-0025): GDD không ghi Stamina mỗi đòn.</summary>
        public const float StaminaPerAttack = 5f;
        /// <summary>GDD 11.2 Xạ Thủ Nỏ "Mắt Ưng": mục tiêu bị đánh dấu nhận +25% sát thương.</summary>
        public const float MarkedBonus = 0.25f;
        /// <summary>GDD 11.2 Thích Khách Đêm "Thấy Trong Tối": lộ tàng hình 8 m.</summary>
        public const float RevealMeters = 8f;

        public static readonly IReadOnlyList<MercenarySpec> All = new[]
        {
            new MercenarySpec { Id = NpcSpecialistId.VeBinhKhien, Role = "Vệ Binh Khiên", SpriteFamily = "merc_do", MaxHp = 420, Damage = 18, RangeMeters = 1.3f, Melee = true, Targeting = MercenaryTargeting.Nearest, Signature = "Chặn cửa: quái nhỏ không đi xuyên qua" },
            new MercenarySpec { Id = NpcSpecialistId.XaThuNo, Role = "Xạ Thủ Nỏ", SpriteFamily = "merc_mien", MaxHp = 180, Damage = 35, RangeMeters = 18f, Melee = false, Targeting = MercenaryTargeting.PreferElite, Signature = "Săn tinh anh; Mắt Ưng +25% lên mục tiêu bị đánh dấu" },
            new MercenarySpec { Id = NpcSpecialistId.KySuChienTruong, Role = "Kỹ Sư Chiến Trường", SpriteFamily = "merc_chot", MaxHp = 220, Damage = 12, RangeMeters = 1.3f, Melee = true, Targeting = MercenaryTargeting.Nearest, Signature = "Đặt lại bẫy đã cạn (2 lần/đợt)" },
            new MercenarySpec { Id = NpcSpecialistId.ThoSanUfo, Role = "Thợ Săn UFO", SpriteFamily = "merc_tu_troi", MaxHp = 200, Damage = 90, RangeMeters = 25f, Melee = false, Targeting = MercenaryTargeting.AirOnly, Signature = "Cắt chùm kéo: trúng UFO đang kéo → heo rơi xuống an toàn" },
            new MercenarySpec { Id = NpcSpecialistId.ThichKhachDem, Role = "Thích Khách Đêm", SpriteFamily = "merc_van", MaxHp = 240, Damage = 45, RangeMeters = 1.3f, Melee = true, Targeting = MercenaryTargeting.StealthAndThieves, Signature = "Truy vết quái tàng hình và quái trộm; lộ tàng hình 8 m" },
        };

        public static MercenarySpec? Find(NpcSpecialistId id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return null;
        }

        public static bool IsAirTarget(EnemyTypeId t) =>
            t == EnemyTypeId.UfoTrinhSat || t == EnemyTypeId.UfoThuHoach || t == EnemyTypeId.UfoPhaoHam || t == EnemyTypeId.UfoMothership;

        public static bool IsThief(EnemyTypeId t) =>
            t == EnemyTypeId.KeBatHeo || t == EnemyTypeId.DauLinhTrom || t == EnemyTypeId.KeCatDay || t == EnemyTypeId.ChuaTromRiuSat;

        /// <summary>Hired mercenaries that fight this wave (active records whose specialist is in the GDD 11.2 table).</summary>
        public static List<MercenarySpec> OnDuty(GameEngine e)
        {
            var list = new List<MercenarySpec>();
            foreach (var r in e.PetsAndNpcs.HiredNpcs)
            {
                if (!r.IsActive) continue;
                var s = Find(r.Id);
                if (s != null) list.Add(s);
            }
            return list;
        }

        /// <summary>Damage of one hit; Xạ Thủ Nỏ adds Mắt Ưng on marked targets.</summary>
        public static float HitDamage(MercenarySpec s, bool targetMarked) =>
            s.Damage * (s.Id == NpcSpecialistId.XaThuNo && targetMarked ? 1f + MarkedBonus : 1f);
    }
}
