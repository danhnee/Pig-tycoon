using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [CreateAssetMenu(fileName = "NewWeaponDefinition", menuName = "PigTycoon/Combat/Weapon Definition")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "dao_go";
        public string DisplayName = "Đao Gỗ";
        public Sprite Icon;

        [Header("Combat Mechanics")]
        public CombatActionType ActionType = CombatActionType.Slash;
        public WeaponSpecialSkill SpecialSkill = WeaponSpecialSkill.Parry;
        public float BaseDamage = 15f;
        public int MinDamage = 12;
        public int MaxDamage = 18;
        public float CritChance = 0.02f;
        public float CritMultiplier = 3.0f;
        public float AttackRange = 1.35f;
        public float ArcAngle = 120f;
        public float Cooldown = 0.35f;
        public float SecondaryCooldown = 1.5f;
        public float KnockbackForce = 2.5f;

        [Header("Visual Effects")]
        public Sprite CustomVFXSprite;
        public Color VFXColor = new Color(0.22f, 0.74f, 0.97f, 0.9f); // Xanh lam sáng

        public WeaponData ToWeaponData()
        {
            return new WeaponData
            {
                Id = Id,
                Name = DisplayName,
                ActionType = ActionType,
                SpecialSkill = SpecialSkill,
                BaseDamage = BaseDamage,
                MinDamage = MinDamage,
                MaxDamage = MaxDamage,
                CritChance = CritChance,
                CritMultiplier = CritMultiplier,
                AttackRange = AttackRange,
                ArcAngle = ArcAngle,
                Cooldown = Cooldown,
                SecondaryCooldown = SecondaryCooldown,
                KnockbackForce = KnockbackForce
            };
        }
    }
}
