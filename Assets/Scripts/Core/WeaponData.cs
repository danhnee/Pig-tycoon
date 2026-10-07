namespace PigTycoon.Core
{
    public enum CombatActionType
    {
        Slash,   // Vung / Chém: Góc quạt ~120° (Kiếm, Dao, Rìu, Liềm)
        Smash,   // Bổ / Đập: Nâng quá đầu và dập xuống đất (Búa, Chùy, Cuốc)
        Thrust   // Đâm / Chọc: Phóng thẳng mũi nhọn với sải dài (Giáo, Đinh ba, Kiếm nhọn)
    }

    public class WeaponData
    {
        public string Id { get; set; } = "dao_go";
        public string Name { get; set; } = "Đao Gỗ";
        public CombatActionType ActionType { get; set; } = CombatActionType.Slash;
        public float BaseDamage { get; set; } = 15f;
        public float AttackRange { get; set; } = 1.35f;
        public float ArcAngle { get; set; } = 120f;
        public float Cooldown { get; set; } = 0.35f;
        public float KnockbackForce { get; set; } = 2.5f;

        public static WeaponData CreateDefaultSword()
        {
            return new WeaponData
            {
                Id = "dao_go",
                Name = "Đao Gỗ",
                ActionType = CombatActionType.Slash,
                BaseDamage = 15f,
                AttackRange = 1.35f,
                ArcAngle = 120f,
                Cooldown = 0.35f,
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
                BaseDamage = 35f,
                AttackRange = 1.1f,
                ArcAngle = 90f,
                Cooldown = 0.65f,
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
                BaseDamage = 22f,
                AttackRange = 2.0f,
                ArcAngle = 45f,
                Cooldown = 0.40f,
                KnockbackForce = 3.5f
            };
        }
    }
}
