using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [CreateAssetMenu(fileName = "NewWeaponDefinition", menuMenuName = "PigTycoon/Combat/Weapon Definition")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "dao_go";
        public string DisplayName = "Đao Gỗ";
        public Sprite Icon;

        [Header("Combat Mechanics")]
        public CombatActionType ActionType = CombatActionType.Slash;
        public float BaseDamage = 15f;
        public float AttackRange = 1.35f;
        public float ArcAngle = 120f;
        public float Cooldown = 0.35f;
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
                BaseDamage = BaseDamage,
                AttackRange = AttackRange,
                ArcAngle = ArcAngle,
                Cooldown = Cooldown,
                KnockbackForce = KnockbackForce
            };
        }
    }
}
