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

        #region Stardew Valley 1.6 Authentic Combat Formulas

        /// <summary>
        /// Công thức Tốc độ vung đòn (Swipe Speed ms) từ MeleeWeapon.cs:1358:
        /// swipeSpeed = (400 - speed * 40 - addedSpeed * 40) * (1 - weaponSpeedMultiplier)
        /// </summary>
        public static float CalculateSwipeSpeed(int weaponSpeed = 0, float addedSpeed = 0f, float weaponSpeedMultiplier = 0f)
        {
            float baseSwipe = (400f - weaponSpeed * 40f) - addedSpeed * 40f;
            baseSwipe *= (1f - weaponSpeedMultiplier);
            return Math.Max(50f, baseSwipe);
        }

        /// <summary>
        /// Công thức Tỉ lệ Chí mạng (Crit Chance) từ GameLocation.cs:
        /// critChance = weaponCritChance + luckLevel * (weaponCritChance / 40.0)
        /// Scout: x1.5; Dagger: +0.005, x1.12
        /// </summary>
        public static float CalculateCriticalChance(float weaponCritChance, int luckLevel = 0, bool isScout = false, bool isDagger = false)
        {
            float chance = weaponCritChance + luckLevel * (weaponCritChance / 40f);
            if (isScout) chance *= 1.5f;
            if (isDagger)
            {
                chance += 0.005f;
                chance *= 1.12f;
            }
            return Math.Clamp(chance, 0f, 1f);
        }

        /// <summary>
        /// Công thức Tính Sát thương thực tế (GameLocation.damageMonster):
        /// - Roll: Random(min, max + 1)
        /// - Crit: roll * critMultiplier (* 2 nếu có Desperado)
        /// - Sức mạnh tấn công: max(1, dmg + attack * 3)
        /// - Nghề nghiệp: x1.1 (Fighter), x1.15 (Brute)
        /// - Kháng đối thủ: max(1, dmg - resilience)
        /// </summary>
        public static int CalculateStardewDamage(int minDamage, int maxDamage, int attackerAttack = 0, float professionMultiplier = 1.0f, int defenderResilience = 0, bool isCrit = false, float critMultiplier = 3.0f, bool isDesperado = false, int randomRoll = -1)
        {
            int baseRoll = randomRoll >= 0 ? randomRoll : ((minDamage + maxDamage) / 2);
            float dmg = baseRoll;
            if (isCrit)
            {
                dmg *= critMultiplier;
                if (isDesperado) dmg *= 2.0f;
            }
            dmg = Math.Max(1f, dmg + attackerAttack * 3f);
            dmg *= professionMultiplier;
            int finalDmg = Math.Max(1, (int)Math.Round(dmg) - defenderResilience);
            return finalDmg;
        }

        /// <summary>
        /// Phản sát thương khi Parry thành công (Monster.handleParried: damage / 2)
        /// </summary>
        public static int CalculateParryCounterDamage(int incomingDamage)
        {
            return Math.Max(1, incomingDamage / 2);
        }

        /// <summary>
        /// Hệ số đẩy lùi quái vật khi Parry (Monster.handleParried):
        /// Đảo ngược vận tốc x3.5 (quái đất) hoặc x2.0 (quái bay)
        /// </summary>
        public static float GetParryKnockbackMultiplier(bool isGlider)
        {
            return isGlider ? 2.0f : 3.5f;
        }

        #endregion
    }
}
