namespace PigTycoon.Core
{
    public enum CombatActionType
    {
        Slash,   // Vung / Chém: Góc quạt ~120° (Kiếm, Dao, Rìu, Liềm)
        Smash,   // Bổ / Đập: Nâng quá đầu và dập xuống đất (Búa, Chùy, Cuốc)
        Thrust   // Đâm / Chọc: Phóng thẳng mũi nhọn với sải dài (Giáo, Đinh ba, Kiếm nhọn)
    }

    public enum WeaponSpecialSkill
    {
        Parry,   // Đỡ đòn & Phản kích 100% block + damage/2 phản đòn (Kiếm, Đao)
        Slam,    // Nện đất sóng chấn động AoE hất tung quái vật (Búa)
        Flurry   // Đâm liên hoàn 4 phát tốc biến (Giáo, Dao găm)
    }

    public class WeaponData
    {
        public string Id { get; set; } = "dao_go";
        public string Name { get; set; } = "Đao Gỗ";
        public CombatActionType ActionType { get; set; } = CombatActionType.Slash;
        public WeaponSpecialSkill SpecialSkill { get; set; } = WeaponSpecialSkill.Parry;
        public float BaseDamage { get; set; } = 15f;
        public int MinDamage { get; set; } = 12;
        public int MaxDamage { get; set; } = 18;
        public float CritChance { get; set; } = 0.02f;
        public float CritMultiplier { get; set; } = 3.0f;
        public float AttackRange { get; set; } = 1.35f;
        public float ArcAngle { get; set; } = 120f;
        public float Cooldown { get; set; } = 0.35f;
        public float SecondaryCooldown { get; set; } = 1.5f;
        public float KnockbackForce { get; set; } = 2.5f;

        public static WeaponData CreateDefaultSword()
        {
            return new WeaponData
            {
                Id = "dao_go",
                Name = "Đao Gỗ",
                ActionType = CombatActionType.Slash,
                SpecialSkill = WeaponSpecialSkill.Parry,
                BaseDamage = 15f,
                MinDamage = 12,
                MaxDamage = 18,
                CritChance = 0.02f,
                CritMultiplier = 3.0f,
                AttackRange = 1.35f,
                ArcAngle = 120f,
                Cooldown = 0.35f,
                SecondaryCooldown = 1.5f,
                KnockbackForce = 2.5f
            };
        }

        public static WeaponData CreateHammer()
        {
            return new WeaponData
            {
                Id = "bua_ta",
                Name = "Búa Tạ",
                ActionType = CombatActionType.Smash,
                SpecialSkill = WeaponSpecialSkill.Slam,
                BaseDamage = 35f,
                MinDamage = 28,
                MaxDamage = 42,
                CritChance = 0.02f,
                CritMultiplier = 3.0f,
                AttackRange = 1.1f,
                ArcAngle = 90f,
                Cooldown = 0.65f,
                SecondaryCooldown = 2.0f,
                KnockbackForce = 5.0f
            };
        }

        public static WeaponData CreateSpear()
        {
            return new WeaponData
            {
                Id = "giao_tre",
                Name = "Giáo Tre",
                ActionType = CombatActionType.Thrust,
                SpecialSkill = WeaponSpecialSkill.Flurry,
                BaseDamage = 22f,
                MinDamage = 18,
                MaxDamage = 26,
                CritChance = 0.06f,
                CritMultiplier = 3.0f,
                AttackRange = 2.0f,
                ArcAngle = 45f,
                Cooldown = 0.40f,
                SecondaryCooldown = 1.8f,
                KnockbackForce = 3.5f
            };
        }
    }
}
