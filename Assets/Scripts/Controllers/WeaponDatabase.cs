using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public static class WeaponDatabase
    {
        private static readonly Dictionary<string, WeaponDefinitionSO> cache = new Dictionary<string, WeaponDefinitionSO>();

        public static WeaponDefinitionSO GetWeapon(string id)
        {
            if (cache.TryGetValue(id, out var weapon) && weapon != null)
            {
                return weapon;
            }

            var newWeapon = ScriptableObject.CreateInstance<WeaponDefinitionSO>();
            switch (id)
            {
                case "kiem_sat":
                    newWeapon.Id = "kiem_sat";
                    newWeapon.DisplayName = "Kiếm Thép";
                    newWeapon.ActionType = CombatActionType.Slash;
                    newWeapon.BaseDamage = 30f;
                    newWeapon.AttackRange = 1.5f;
                    newWeapon.ArcAngle = 120f;
                    newWeapon.Cooldown = 0.30f;
                    newWeapon.KnockbackForce = 3.0f;
                    newWeapon.Icon = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_kiem_sat.png", 24);
                    break;

                case "bua_ta":
                    newWeapon.Id = "bua_ta";
                    newWeapon.DisplayName = "Búa Tạ";
                    newWeapon.ActionType = CombatActionType.Smash;
                    newWeapon.BaseDamage = 45f;
                    newWeapon.AttackRange = 1.1f;
                    newWeapon.ArcAngle = 90f;
                    newWeapon.Cooldown = 0.65f;
                    newWeapon.KnockbackForce = 5.5f;
                    newWeapon.Icon = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_bua_ta.png", 24);
                    break;

                case "giao_tre":
                    newWeapon.Id = "giao_tre";
                    newWeapon.DisplayName = "Giáo Tre";
                    newWeapon.ActionType = CombatActionType.Thrust;
                    newWeapon.BaseDamage = 25f;
                    newWeapon.AttackRange = 2.0f;
                    newWeapon.ArcAngle = 40f;
                    newWeapon.Cooldown = 0.40f;
                    newWeapon.KnockbackForce = 3.5f;
                    newWeapon.Icon = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_giao_tre.png", 24);
                    break;

                case "dao_go":
                default:
                    newWeapon.Id = "dao_go";
                    newWeapon.DisplayName = "Đao Gỗ";
                    newWeapon.ActionType = CombatActionType.Slash;
                    newWeapon.BaseDamage = 15f;
                    newWeapon.AttackRange = 1.35f;
                    newWeapon.ArcAngle = 120f;
                    newWeapon.Cooldown = 0.35f;
                    newWeapon.KnockbackForce = 2.5f;
                    newWeapon.Icon = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_dao_go.png", 24);
                    break;
            }

            cache[id] = newWeapon;
            return newWeapon;
        }
    }
}
