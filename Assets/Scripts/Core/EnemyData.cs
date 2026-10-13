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

        public static EnemyData CreateUfoTrinhSat()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.UfoTrinhSat,
                Name = "UFO Trinh Sát",
                MaxHp = 160f,
                CurrentHp = 160f,
                MoveSpeed = 3.2f,
                AttackDamage = 10f,
                AttackCooldown = 1.0f,
                AttackRange = 1.8f,
                GoldReward = 60,
                ExpReward = 25
            };
        }

        public static EnemyData CreateUfoThuHoach()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.UfoThuHoach,
                Name = "UFO Thu Hoạch",
                MaxHp = 650f,
                CurrentHp = 650f,
                MoveSpeed = 2.0f,
                AttackDamage = 20f,
                AttackCooldown = 1.6f,
                AttackRange = 2.2f,
                GoldReward = 150,
                ExpReward = 70
            };
        }

        public static EnemyData CreateAlienGrunt(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.08f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.05f;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.AlienGrunt,
                Name = "Lính Ngoài Hành Tinh",
                MaxHp = 180f * hpMult,
                CurrentHp = 180f * hpMult,
                MoveSpeed = 2.8f,
                AttackDamage = 14f * dmgMult,
                AttackCooldown = 1.2f,
                AttackRange = 1.3f,
                GoldReward = 75,
                ExpReward = 35
            };
        }

        public static EnemyData CreateAlienElite(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.12f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.08f;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.AlienElite,
                Name = "Chỉ Huy Dị Tộc (Elite)",
                MaxHp = 450f * hpMult,
                CurrentHp = 450f * hpMult,
                MoveSpeed = 2.4f,
                AttackDamage = 28f * dmgMult,
                AttackCooldown = 1.5f,
                AttackRange = 3.2f,
                GoldReward = 220,
                ExpReward = 90
            };
        }

        public static EnemyData CreateBossAlienWarlord(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.18f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.12f;
            return new EnemyData
            {
                Id = $"enemy_boss_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.BossAlienWarlord,
                Name = "Đại Tướng Alien Warlord",
                MaxHp = 3500f * hpMult,
                CurrentHp = 3500f * hpMult,
                MoveSpeed = 2.1f,
                AttackDamage = 55f * dmgMult,
                AttackCooldown = 1.8f,
                AttackRange = 2.2f,
                GoldReward = 1200,
                ExpReward = 500
            };
        }

        public static EnemyData CreateUfoMothership(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.20f;
            return new EnemyData
            {
                Id = $"enemy_mothership_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.UfoMothership,
                Name = "Tàu Mẹ UFO Đổ Bộ",
                MaxHp = 8000f * hpMult,
                CurrentHp = 8000f * hpMult,
                MoveSpeed = 0.5f,
                AttackDamage = 40f,
                AttackCooldown = 3.0f,
                AttackRange = 5.0f,
                GoldReward = 3000,
                ExpReward = 1000
            };
        }
    }
}

