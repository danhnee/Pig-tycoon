using System;

namespace PigTycoon.Core
{
    [Serializable]
    public class EnemyData
    {
        public string Id { get; set; } = "";
        public EnemyTypeId TypeId { get; set; } = EnemyTypeId.KeBatHeo;
        public string Name { get; set; } = "Kẻ Bắt Heo";
        public float MaxHp { get; set; } = 150f;
        public float CurrentHp { get; set; } = 150f;
        public float MoveSpeed { get; set; } = 2.5f;
        public float AttackDamage { get; set; } = 12f;
        public float AttackCooldown { get; set; } = 1.2f;
        public float AttackRange { get; set; } = 1.1f;
        public int GoldReward { get; set; } = 35;
        public int ExpReward { get; set; } = 15;

        public bool IsAlive => CurrentHp > 0f;

        public float TakeDamage(float rawDamage, int attackerStr = 0, int defenderRes = 0)
        {
            float actualDamage = CombatFormulas.CalculateDamage(rawDamage, attackerStr, defenderRes);
            CurrentHp = Math.Max(0f, CurrentHp - actualDamage);
            return actualDamage;
        }

        public static EnemyData CreateKeBatHeo()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.KeBatHeo,
                Name = "Kẻ Bắt Heo",
                MaxHp = 150f,
                CurrentHp = 150f,
                MoveSpeed = 2.5f,
                AttackDamage = 12f,
                AttackCooldown = 1.2f,
                AttackRange = 1.1f,
                GoldReward = 35,
                ExpReward = 15
            };
        }

        public static EnemyData CreateKeCaoPha()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.KeCaoPha,
                Name = "Kẻ Cào Phá",
                MaxHp = 120f,
                CurrentHp = 120f,
                MoveSpeed = 2.0f,
                AttackDamage = 15f,
                AttackCooldown = 1.4f,
                AttackRange = 1.0f,
                GoldReward = 25,
                ExpReward = 10
            };
        }

        public static EnemyData CreateChuotGamKho()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.ChuotGamKho,
                Name = "Chuột Gặm Kho",
                MaxHp = 40f,
                CurrentHp = 40f,
                MoveSpeed = 3.8f,
                AttackDamage = 6f,
                AttackCooldown = 0.8f,
                AttackRange = 0.8f,
                GoldReward = 10,
                ExpReward = 5
            };
        }

        public static EnemyData CreateDauLinhTrom()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.DauLinhTrom,
                Name = "Đầu Lĩnh Trộm",
                MaxHp = 500f,
                CurrentHp = 500f,
                MoveSpeed = 2.2f,
                AttackDamage = 25f,
                AttackCooldown = 1.5f,
                AttackRange = 1.3f,
                GoldReward = 120,
                ExpReward = 60
            };
        }
    }
}
