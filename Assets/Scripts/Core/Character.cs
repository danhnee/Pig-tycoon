using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class BaseStats
    {
        public int Str = 24;
        public int Agi = 18;
        public int Ctrl = 18;
        public int Res = 22;
        public int Luk = 0;
    }

    [Serializable]
    public class BonusStats
    {
        public int Str = 0;
        public int Agi = 0;
        public int Ctrl = 0;
        public int Res = 0;
        public int Luk = 0;
    }

    [Serializable]
    public class StaminaSystem
    {
        public float CurrentStamina = 180f;
        public float MaxStamina = 180f;
        public float DailyLoad = 0f;
        public int Condition = 100;
        public int Immunity = 100;
        public int ReboundFatigueStacks = 0; // Mỗi tầng giảm 8% MaxStamina (GDD 9.5)

        /// <summary>
        /// Trần thể lực sau khi trừ phạt Mệt Dội (Rebound Fatigue)
        /// </summary>
        public float BaseMaxStamina => Math.Max(0f, MaxStamina * (1f - Math.Min(4, ReboundFatigueStacks) * 0.08f));

        /// <summary>
        /// Giới hạn thể lực hiệu dụng tức thời: Trần thể lực trừ đi Tải ngày (Daily Load)
        /// </summary>
        public float EffectiveMaxStamina => Math.Max(0f, BaseMaxStamina - DailyLoad);

        /// <summary>
        /// Vùng mệt mỏi theo GDD 9.4: CurrentStamina < 20% MaxStamina.
        /// Chạy -15%, đòn nặng / combo trễ +20%, không khóa hành động.
        /// </summary>
        public bool IsFatigued => CurrentStamina < (MaxStamina * 0.2f);

        /// <summary>
        /// Tỷ lệ thể lực tức thời so với MaxStamina (0.0 đến 1.0)
        /// </summary>
        public float StaminaRatio => MaxStamina > 0f ? Math.Clamp(CurrentStamina / MaxStamina, 0f, 1f) : 0f;

        /// <summary>
        /// Tỷ lệ Tải ngày so với MaxStamina (0.0 đến 1.0)
        /// </summary>
        public float DailyLoadRatio => MaxStamina > 0f ? Math.Clamp(DailyLoad / MaxStamina, 0f, 1f) : 0f;

        public bool CanAfford(float cost) => CurrentStamina >= cost;

        /// <summary>
        /// Tiêu hao thể lực theo cơ chế 2 lớp:
        /// - Trừ tức thời vào CurrentStamina
        /// - Tích lũy vào DailyLoad theo tỷ lệ quy định (GDD 9.2: combat 30-40%, lướt 30%)
        /// </summary>
        public bool TryConsumeStamina(float amount, float dailyLoadRatio = 0.3f)
        {
            if (amount <= 0f) return true;
            if (CurrentStamina < amount) return false;

            CurrentStamina -= amount;
            DailyLoad = Math.Min(MaxStamina, DailyLoad + (amount * dailyLoadRatio));

            // Đảm bảo CurrentStamina không vượt quá EffectiveMaxStamina khi DailyLoad tăng
            if (CurrentStamina > EffectiveMaxStamina)
            {
                CurrentStamina = EffectiveMaxStamina;
            }

            return true;
        }

        /// <summary>
        /// Tiêu hao cưỡng bức (cho phép cạn về 0)
        /// </summary>
        public void ForceConsumeStamina(float amount, float dailyLoadRatio = 0.3f)
        {
            if (amount <= 0f) return;
            float actualCost = Math.Min(CurrentStamina, amount);
            CurrentStamina = Math.Max(0f, CurrentStamina - amount);
            DailyLoad = Math.Min(MaxStamina, DailyLoad + (actualCost * dailyLoadRatio));

            if (CurrentStamina > EffectiveMaxStamina)
            {
                CurrentStamina = EffectiveMaxStamina;
            }
        }

        /// <summary>
        /// Hồi phục Thể lực tức thời đến trần EffectiveMaxStamina (Tải ngày không tự hồi - GDD 9.4)
        /// </summary>
        public void RegenerateStamina(float amount)
        {
            if (amount <= 0f) return;
            float cap = EffectiveMaxStamina;
            CurrentStamina = Math.Min(cap, CurrentStamina + amount);
        }

        /// <summary>
        /// Ngủ chuẩn theo GDD 9.5: Xóa sạch Tải ngày, khôi phục Stamina >= 80% tối đa, phục hồi Condition/Immunity
        /// </summary>
        public void PerformSleepReset()
        {
            DailyLoad = 0f;
            float baseCap = BaseMaxStamina;
            CurrentStamina = Math.Max(CurrentStamina, baseCap * 0.8f);
            Condition = Math.Min(100, Condition + 6);
            Immunity = Math.Min(100, Immunity + 5);
        }
    }

    public enum EquipSlot
    {
        Weapon,
        Boots,
        BodyArmor,
        Pants,
        Bracelet,
        Necklace
    }

    [Serializable]
    public class EquipmentItem
    {
        public string Id;
        public string Name;
        public EquipSlot Slot;
        public int WeightScore = 1; // 1=Nhẹ, 2=Vừa, 3=Nặng
        public int Defense;
        public int BonusStr;
        public int BonusAgi;
        public int BonusCtrl;
        public int BonusRes;
        public string SpecialMechanic;
    }

    [Serializable]
    public class CharacterData
    {
        public string Id = "char_1";
        public string Name = "Khoa";
        public int Level = 1;
        public int Exp = 0;

        public BaseStats BaseStats = new BaseStats();
        public BonusStats BonusStats = new BonusStats();

        public int Hp = 100;
        public int MaxHp = 100;
        public int Rage = 0; // Nộ khí 0 - 100

        public StaminaSystem Stamina = new StaminaSystem();

        [NonSerialized]
        public Dictionary<EquipSlot, EquipmentItem> Equipment = new Dictionary<EquipSlot, EquipmentItem>();
        public MartialSchool PrimarySchool = MartialSchool.TrongKich_B;

        [NonSerialized]
        public Dictionary<CombatSlotType, string> ActiveMartialSkills = new Dictionary<CombatSlotType, string>();

        public CharacterData()
        {
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                Equipment[slot] = null;
            }

            ActiveMartialSkills[CombatSlotType.TheCong] = null;
            ActiveMartialSkills[CombatSlotType.TheThu] = null;
            ActiveMartialSkills[CombatSlotType.TheBien] = null;
        }

        public string GetPlaystyle()
        {
            int totalWeight = 0;
            foreach (var kvp in Equipment)
            {
                if (kvp.Value != null) totalWeight += kvp.Value.WeightScore;
                else totalWeight += 1;
            }

            if (totalWeight <= 9) return "KhinhThân";
            if (totalWeight <= 13) return "CânBằng";
            return "TrọngGiáp";
        }

        public bool EquipItem(EquipmentItem item)
        {
            Equipment[item.Slot] = item;
            RecalculateBonusStats();
            return true;
        }

        public void RecalculateBonusStats()
        {
            int s = 0, a = 0, c = 0, r = 0;
            foreach (var kvp in Equipment)
            {
                if (kvp.Value != null)
                {
                    s += kvp.Value.BonusStr;
                    a += kvp.Value.BonusAgi;
                    c += kvp.Value.BonusCtrl;
                    r += kvp.Value.BonusRes;
                }
            }

            // Trần riêng tối đa +8 mỗi chỉ số thưởng
            BonusStats.Str = Math.Min(8, s);
            BonusStats.Agi = Math.Min(8, a);
            BonusStats.Ctrl = Math.Min(8, c);
            BonusStats.Res = Math.Min(8, r);
        }

        /// <summary>
        /// Tính chi phí Thể lực Lướt né theo GDD 9.2 & 12.2:
        /// Gốc 9.0 STA. AGI gốc >= 35: né -15%. Khinh Thân: né -20%. Trọng Giáp: né +25%.
        /// </summary>
        public float CalculateDodgeCost()
        {
            float baseCost = 9.0f;
            float reduction = 0f;

            if (BaseStats.Agi >= 35)
            {
                reduction += 0.15f;
            }

            string playstyle = GetPlaystyle();
            if (playstyle == "KhinhThân")
            {
                reduction += 0.20f;
            }
            else if (playstyle == "TrọngGiáp")
            {
                reduction -= 0.25f;
            }

            float finalCost = baseCost * (1f - reduction);
            return Math.Max(1.0f, finalCost);
        }

        /// <summary>
        /// Tính chi phí Thể lực đòn đánh theo nhịp combo (GDD 9.2 & 12):
        /// Nhịp 1 & 2: 2.5 STA (30% Daily Load)
        /// Nhịp 3 (Finisher): 5.0 STA (40% Daily Load)
        /// </summary>
        public (float staminaCost, float dailyLoadRatio, float damageMultiplier) GetComboStepData(int comboStep)
        {
            switch (comboStep)
            {
                case 0: // Nhịp 1 (Light Strike)
                    return (2.5f, 0.30f, 1.0f);
                case 1: // Nhịp 2 (Follow-up Swing, nhanh hơn 15%)
                    return (2.5f, 0.30f, 1.15f);
                case 2: // Nhịp 3 (Heavy Finisher / Slam, sát thương x1.5, knockback x1.6)
                default:
                    return (5.0f, 0.40f, 1.50f);
            }
        }
    }
}
