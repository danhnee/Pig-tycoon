using System;

namespace PigTycoon.Core
{
    public static class CombatFormulas
    {
        /// <summary>
        /// Tính toán sát thương thực tế dựa trên Base Damage của vũ khí, chỉ số Str của kẻ tấn công và Res của phòng thủ.
        /// </summary>
        public static float CalculateDamage(float baseDamage, int attackerStr = 0, int defenderRes = 0)
        {
            float strMultiplier = 1.0f + (Math.Max(0, attackerStr - 10) * 0.03f);
            float resMitigation = 100f / (100f + Math.Max(0, defenderRes) * 2.0f);
            float finalDamage = baseDamage * strMultiplier * resMitigation;
            return (float)Math.Round(Math.Max(1.0f, finalDamage), 1);
        }

        /// <summary>
        /// Tính toán lượng Nộ Khí (Rage) tích tụ khi nhận đòn (GDD mục 12: tối thiểu 5, tối đa 25 nộ mỗi đòn)
        /// </summary>
        public static int CalculateRageGainOnDamage(float damageReceived)
        {
            return Math.Clamp((int)(damageReceived * 0.6f), 5, 25);
        }

        /// <summary>
        /// Tính toán khoảng cách bật lùi (Knockback) sau khi giảm trừ bởi trọng lượng
        /// </summary>
        public static float CalculateKnockbackDistance(float weaponForce, float targetWeight = 1.0f)
        {
            return weaponForce / Math.Max(0.5f, targetWeight);
        }
    }
}
