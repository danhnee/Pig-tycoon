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
    }

    [Serializable]
    public class BonusStats
    {
        public int Str = 0;
        public int Agi = 0;
        public int Ctrl = 0;
        public int Res = 0;
    }

    [Serializable]
    public class StaminaSystem
    {
        public float CurrentStamina = 180f;
        public float MaxStamina = 180f;
        public float DailyLoad = 0f;
        public int Condition = 100;
        public int Immunity = 100;
        public int ReboundFatigueStacks = 0;
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

        public Dictionary<EquipSlot, EquipmentItem> Equipment = new Dictionary<EquipSlot, EquipmentItem>();
        public MartialSchool PrimarySchool = MartialSchool.TrongKich_B;

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
    }
}
