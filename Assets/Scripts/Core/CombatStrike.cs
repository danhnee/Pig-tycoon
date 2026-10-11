using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// Đòn sandbox của người C. Dao Rựa đòn nhẹ = 14. Vũ khí nặng cộng 1% mỗi điểm STR gốc.
    /// Sát thương nhận theo GDD 12.3: Gốc × 100 / (100 + Phòng thủ) × (1 − RES gốc × 0,5%).
    /// Thể lực đòn nhẹ 2,5 và 30% thành tải ngày, GDD 9.2.
    /// </summary>
    public static class CombatMath
    {
        public const int DaoRuaLightDamage = 14;
        public const int BuaTaHeavyDamage = 48;
        public const float LightStamina = 2.5f;
        public const float HeavyStamina = 7f;
        public const float LightDailyLoadFraction = 0.30f;
        public const float HeavyDailyLoadFraction = 0.40f;

        public static int HeavyRaw(int baseDamage, int attackerStr)
        {
            if (attackerStr < 0)
            {
                attackerStr = 0;
            }

            float scaled = baseDamage * (1f + (attackerStr * 0.01f));
            return (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
        }

        public static int DamageReceived(int raw, int defense, int targetRes)
        {
            if (raw <= 0)
            {
                return 0;
            }

            if (defense < 0)
            {
                defense = 0;
            }

            float mitigated = raw * 100f / (100f + defense);
            float resFactor = 1f - (targetRes * 0.005f);
            if (resFactor < 0f)
            {
                resFactor = 0f;
            }

            int dealt = (int)Math.Round(mitigated * resFactor, MidpointRounding.AwayFromZero);
            return dealt < 0 ? 0 : dealt;
        }
    }

    public sealed class CombatDummy
    {
        public int MaxHp;
        public int Hp;
        public int Defense;
        public int Resilience;

        public CombatDummy(int maxHp, int defense, int resilience)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            Defense = defense;
            Resilience = resilience;
        }

        public bool Alive => Hp > 0;
    }

    public readonly struct StrikeResult
    {
        public bool Landed { get; }
        public int Damage { get; }
        public float StaminaSpent { get; }
        public float DailyLoadAdded { get; }

        public StrikeResult(bool landed, int damage, float staminaSpent, float dailyLoadAdded)
        {
            Landed = landed;
            Damage = damage;
            StaminaSpent = staminaSpent;
            DailyLoadAdded = dailyLoadAdded;
        }
    }

    public static class CombatStrike
    {
        public static StrikeResult Light(float stamina, CombatDummy dummy)
        {
            return Hit(stamina, CombatMath.LightStamina, CombatMath.LightDailyLoadFraction, CombatMath.DaoRuaLightDamage, dummy);
        }

        public static StrikeResult Heavy(float stamina, int attackerStr, CombatDummy dummy)
        {
            int raw = CombatMath.HeavyRaw(CombatMath.BuaTaHeavyDamage, attackerStr);
            return Hit(stamina, CombatMath.HeavyStamina, CombatMath.HeavyDailyLoadFraction, raw, dummy);
        }

        private static StrikeResult Hit(float stamina, float cost, float loadFraction, int raw, CombatDummy dummy)
        {
            if (dummy == null || !dummy.Alive || stamina < cost)
            {
                return new StrikeResult(false, 0, 0f, 0f);
            }

            int dealt = CombatMath.DamageReceived(raw, dummy.Defense, dummy.Resilience);
            dummy.Hp -= dealt;
            if (dummy.Hp < 0)
            {
                dummy.Hp = 0;
            }

            return new StrikeResult(true, dealt, cost, cost * loadFraction);
        }
    }
}
