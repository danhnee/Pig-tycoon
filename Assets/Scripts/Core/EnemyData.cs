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
        public bool IsElite { get; set; } = false;
        public bool IsBoss { get; set; } = false;
        public float SiegeDamageMultiplier { get; set; } = 1.0f;
        public float CommandAuraRadius { get; set; } = 0f;
        public float CommandAuraSpeedBuff { get; set; } = 0f;

        public bool IsAlive => CurrentHp > 0f;

        public float TakeDamage(float rawDamage, int attackerStr = 0, int defenderRes = 0)
        {
            float actualDamage = CombatFormulas.CalculateDamage(rawDamage, attackerStr, defenderRes);
            CurrentHp = Math.Max(0f, CurrentHp - actualDamage);
            return actualDamage;
        }

        public static EnemyData CreateKeBatHeo(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.08f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.05f;
            float baseHp = 150f * hpMult;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.KeBatHeo,
                Name = "Kẻ Bắt Heo",
                MaxHp = baseHp,
                CurrentHp = baseHp,
                MoveSpeed = 2.5f,
                AttackDamage = 12f * dmgMult,
                AttackCooldown = 1.2f,
                AttackRange = 1.1f,
                GoldReward = (int)(35 * (1f + Math.Max(0, dayNumber - 1) * 0.05f)),
                ExpReward = 15
            };
        }

        public static EnemyData CreateKeCaoPha(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.08f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.05f;
            float baseHp = 120f * hpMult;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.KeCaoPha,
                Name = "Kẻ Cào Phá",
                MaxHp = baseHp,
                CurrentHp = baseHp,
                MoveSpeed = 2.0f,
                AttackDamage = 15f * dmgMult,
                AttackCooldown = 1.4f,
                AttackRange = 1.0f,
                GoldReward = (int)(25 * (1f + Math.Max(0, dayNumber - 1) * 0.05f)),
                ExpReward = 10
            };
        }

        public static EnemyData CreateChuotGamKho(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.06f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.04f;
            float baseHp = 40f * hpMult;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.ChuotGamKho,
                Name = "Chuột Gặm Kho",
                MaxHp = baseHp,
                CurrentHp = baseHp,
                MoveSpeed = 3.8f,
                AttackDamage = 6f * dmgMult,
                AttackCooldown = 0.8f,
                AttackRange = 0.8f,
                GoldReward = (int)(10 * (1f + Math.Max(0, dayNumber - 1) * 0.04f)),
                ExpReward = 5
            };
        }

        public static EnemyData CreateDauLinhTrom(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.12f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.08f;
            float baseHp = 500f * hpMult;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.DauLinhTrom,
                Name = "Đầu Lĩnh Trộm",
                MaxHp = baseHp,
                CurrentHp = baseHp,
                MoveSpeed = 2.3f,
                AttackDamage = 26f * dmgMult,
                AttackCooldown = 1.4f,
                AttackRange = 1.3f,
                GoldReward = (int)(120 * (1f + Math.Max(0, dayNumber - 1) * 0.08f)),
                ExpReward = 60,
                IsElite = true,
                CommandAuraRadius = 10f,
                CommandAuraSpeedBuff = 0.20f
            };
        }

        public static EnemyData CreateBossChuaTromRiuSat(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.16f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.10f;
            float baseHp = 4000f * hpMult;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.ChuaTromRiuSat,
                Name = "Chúa Trộm Rìu Sắt",
                MaxHp = baseHp,
                CurrentHp = baseHp,
                MoveSpeed = 2.1f,
                AttackDamage = 48f * dmgMult,
                AttackCooldown = 1.6f,
                AttackRange = 1.8f,
                GoldReward = (int)(550 * (1f + Math.Max(0, dayNumber - 1) * 0.10f)),
                ExpReward = 280,
                IsBoss = true,
                SiegeDamageMultiplier = 3.0f
            };
        }

        public static EnemyData CreateUfoTrinhSat(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.08f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.05f;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.UfoTrinhSat,
                Name = "UFO Trinh Sát",
                MaxHp = 160f * hpMult,
                CurrentHp = 160f * hpMult,
                MoveSpeed = 3.2f,
                AttackDamage = 10f * dmgMult,
                AttackCooldown = 1.0f,
                AttackRange = 1.8f,
                GoldReward = 60,
                ExpReward = 25
            };
        }

        public static EnemyData CreateUfoThuHoach(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.10f;
            float dmgMult = 1f + Math.Max(0, dayNumber - 1) * 0.08f;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.UfoThuHoach,
                Name = "UFO Thu Hoạch",
                MaxHp = 650f * hpMult,
                CurrentHp = 650f * hpMult,
                MoveSpeed = 2.0f,
                AttackDamage = 20f * dmgMult,
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
                ExpReward = 500,
                IsBoss = true
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
                ExpReward = 1000,
                IsBoss = true
            };
        }

        public static EnemyData CreateChoSuong(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.12f;
            return new EnemyData
            {
                Id = $"enemy_chosuong_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.ChoSuong,
                Name = "Chó Sương Huyền Bí",
                MaxHp = 420f * hpMult,
                CurrentHp = 420f * hpMult,
                MoveSpeed = 1.8f,
                AttackDamage = 20f,
                AttackCooldown = 0.9f,
                AttackRange = 1.1f,
                GoldReward = 380,
                ExpReward = 110,
                IsBoss = false
            };
        }

        public static EnemyData CreateBongHuyen(int dayNumber = 1)
        {
            float hpMult = 1f + Math.Max(0, dayNumber - 1) * 0.15f;
            return new EnemyData
            {
                Id = $"enemy_bonghuyen_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.BongHuyen,
                Name = "Bóng Huyền Hư Không",
                MaxHp = 500f * hpMult,
                CurrentHp = 500f * hpMult,
                MoveSpeed = 1.35f,
                AttackDamage = 25f,
                AttackCooldown = 1.2f,
                AttackRange = 1.3f,
                GoldReward = 450,
                ExpReward = 140,
                IsBoss = false
            };
        }
    }
}

