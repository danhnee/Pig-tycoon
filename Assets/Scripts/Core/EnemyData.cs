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

        public bool CanLeapLowFence { get; set; } = false;
        public bool SpreadsDiseaseOnHit { get; set; } = false;
        public DiseaseId? CarriedDisease { get; set; } = null;
        public bool EmpoweredInStorm { get; set; } = false;
        public int StealsFeedPortions { get; set; } = 0;

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

        /// <summary>
        /// Quái của B (GDD 19.1): Bầy Nhảy - nhảy qua hàng rào gỗ thấp 1 lớp, tốc độ cao.
        /// </summary>
        public static EnemyData CreateBayNhay()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.BayNhay,
                Name = "Bầy Nhảy",
                MaxHp = 85f,
                CurrentHp = 85f,
                MoveSpeed = 4.2f,
                AttackDamage = 11f,
                AttackCooldown = 0.9f,
                AttackRange = 1.2f,
                GoldReward = 22,
                ExpReward = 12,
                CanLeapLowFence = true
            };
        }

        /// <summary>
        /// Quái của B (GDD 19.1): Thú Bão - cường hóa tốc độ và sát thương trong thời tiết Giông Bão.
        /// </summary>
        public static EnemyData CreateThuBao(WeatherType weather = WeatherType.QuangDang)
        {
            bool storm = weather == WeatherType.GiongBao;
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.ThuBao,
                Name = "Thú Bão",
                MaxHp = storm ? 340f : 260f,
                CurrentHp = storm ? 340f : 260f,
                MoveSpeed = storm ? 3.6f : 2.7f,
                AttackDamage = storm ? 28f : 20f,
                AttackCooldown = 1.3f,
                AttackRange = 1.5f,
                GoldReward = 70,
                ExpReward = 35,
                EmpoweredInStorm = true
            };
        }

        /// <summary>
        /// Quái của B (GDD 19.1): Kẻ Mang Dịch - phát tán mầm bệnh (Dịch Tả Heo / Nấm Phát Quang) khi tiếp cận chuồng.
        /// </summary>
        public static EnemyData CreateKeMangDich()
        {
            return new EnemyData
            {
                Id = $"enemy_{Guid.NewGuid():N}",
                TypeId = EnemyTypeId.KeMangDich,
                Name = "Kẻ Mang Dịch",
                MaxHp = 210f,
                CurrentHp = 210f,
                MoveSpeed = 1.9f,
                AttackDamage = 14f,
                AttackCooldown = 1.5f,
                AttackRange = 1.6f,
                GoldReward = 55,
                ExpReward = 28,
                SpreadsDiseaseOnHit = true,
                CarriedDisease = DiseaseId.DichTaHeo
            };
        }

        public static EnemyData CreateByType(EnemyTypeId typeId, int dayNumber = 1, WeatherType weather = WeatherType.QuangDang)
        {
            switch (typeId)
            {
                case EnemyTypeId.ChuotGamKho:
                    var rat = CreateChuotGamKho();
                    rat.StealsFeedPortions = 2;
                    return rat;
                case EnemyTypeId.KeBatHeo: return CreateKeBatHeo();
                case EnemyTypeId.KeCaoPha: return CreateKeCaoPha();
                case EnemyTypeId.BayNhay: return CreateBayNhay();
                case EnemyTypeId.ThuBao: return CreateThuBao(weather);
                case EnemyTypeId.KeMangDich: return CreateKeMangDich();
                case EnemyTypeId.DauLinhTrom: return CreateDauLinhTrom();
                case EnemyTypeId.UfoTrinhSat: return CreateUfoTrinhSat();
                case EnemyTypeId.UfoThuHoach: return CreateUfoThuHoach();
                case EnemyTypeId.AlienGrunt: return CreateAlienGrunt(dayNumber);
                case EnemyTypeId.AlienElite: return CreateAlienElite(dayNumber);
                case EnemyTypeId.BossAlienWarlord: return CreateBossAlienWarlord(dayNumber);
                case EnemyTypeId.UfoMothership: return CreateUfoMothership(dayNumber);
                case EnemyTypeId.KeCatDay:
                    return new EnemyData { Id = $"enemy_{Guid.NewGuid():N}", TypeId = EnemyTypeId.KeCatDay, Name = "Kẻ Cắt Dây", MaxHp = 95f, CurrentHp = 95f, MoveSpeed = 3.1f, AttackDamage = 10f, AttackCooldown = 1.0f, AttackRange = 1.0f, GoldReward = 28, ExpReward = 14, ThreatPoints = 10, GddCanonicalMaxHp = 150f };
                case EnemyTypeId.ThietGiapCongThanh:
                    return new EnemyData { Id = $"enemy_{Guid.NewGuid():N}", TypeId = EnemyTypeId.ThietGiapCongThanh, Name = "Thiết Giáp Công Thành", MaxHp = 620f, CurrentHp = 620f, MoveSpeed = 1.4f, AttackDamage = 35f, AttackCooldown = 2.0f, AttackRange = 1.4f, GoldReward = 140, ExpReward = 65, ThreatPoints = 55, GddCanonicalMaxHp = 1400f };
                case EnemyTypeId.UfoPhaoHam:
                    return new EnemyData { Id = $"enemy_{Guid.NewGuid():N}", TypeId = EnemyTypeId.UfoPhaoHam, Name = "UFO Pháo Hạm", MaxHp = 520f, CurrentHp = 520f, MoveSpeed = 2.1f, AttackDamage = 32f, AttackCooldown = 1.8f, AttackRange = 3.5f, GoldReward = 160, ExpReward = 75, ThreatPoints = 75, GddCanonicalMaxHp = 1100f };
                case EnemyTypeId.BongHuyen:
                    return new EnemyData { Id = $"enemy_{Guid.NewGuid():N}", TypeId = EnemyTypeId.BongHuyen, Name = "Bóng Huyền", MaxHp = 190f, CurrentHp = 190f, MoveSpeed = 3.0f, AttackDamage = 18f, AttackCooldown = 1.1f, AttackRange = 1.4f, GoldReward = 65, ExpReward = 30, ThreatPoints = 18, GddCanonicalMaxHp = 300f };
                case EnemyTypeId.ChoSuong:
                    return new EnemyData { Id = $"enemy_{Guid.NewGuid():N}", TypeId = EnemyTypeId.ChoSuong, Name = "Chó Sương", MaxHp = 140f, CurrentHp = 140f, MoveSpeed = 3.9f, AttackDamage = 16f, AttackCooldown = 0.9f, AttackRange = 1.1f, GoldReward = 45, ExpReward = 22, ThreatPoints = 10, GddCanonicalMaxHp = 260f };
                case EnemyTypeId.ChuaTromRiuSat:
                    return new EnemyData { Id = $"enemy_boss_{Guid.NewGuid():N}", TypeId = EnemyTypeId.ChuaTromRiuSat, Name = "Chúa Trộm Rìu Sắt", MaxHp = 2800f, CurrentHp = 2800f, MoveSpeed = 2.3f, AttackDamage = 48f, AttackCooldown = 1.6f, AttackRange = 2.0f, GoldReward = 1000, ExpReward = 450, ThreatPoints = 300, GddCanonicalMaxHp = 12000f };
                case EnemyTypeId.ThucTheSuong:
                    return new EnemyData { Id = $"enemy_boss_{Guid.NewGuid():N}", TypeId = EnemyTypeId.ThucTheSuong, Name = "Thực Thể Sương", MaxHp = 4200f, CurrentHp = 4200f, MoveSpeed = 2.0f, AttackDamage = 52f, AttackCooldown = 1.7f, AttackRange = 2.8f, GoldReward = 1500, ExpReward = 650, SpreadsDiseaseOnHit = true, CarriedDisease = DiseaseId.NamPhatQuang, ThreatPoints = 600, GddCanonicalMaxHp = 28000f };
                default:
                    return CreateKeBatHeo();
            }
        }

        public int ThreatPoints { get; set; } = 12;
        public float GddCanonicalMaxHp { get; set; } = 220f;

        /// <summary>
        /// GDD 18.2: Ngân sách Uy Hiếp = 100 * (1 + 0.18 * (ChuKy - 1)) * (1 + 0.004 * SoHeo) * HeSoTrangThaiTrai.
        /// </summary>
        public static int CalculateWaveThreatBudget(int cycleNumber, int pigCount, float farmStateMultiplier = 1.0f)
        {
            int c = Math.Max(1, cycleNumber);
            int p = Math.Max(0, pigCount);
            float f = Math.Max(0.5f, farmStateMultiplier);
            double budget = 100.0 * (1.0 + 0.18 * (c - 1)) * (1.0 + 0.004 * p) * f;
            return (int)Math.Round(budget);
        }

        /// <summary>
        /// GDD 19.1-19.2: Khởi tạo thông số chuẩn GDD (bao gồm HP chuẩn Boss 12.000 / 20.000 / 28.000 và Điểm Uy Hiếp).
        /// </summary>
        public static EnemyData CreateGdd19Canonical(EnemyTypeId typeId)
        {
            var e = CreateByType(typeId);
            switch (typeId)
            {
                case EnemyTypeId.ChuotGamKho: e.ThreatPoints = 5; e.GddCanonicalMaxHp = 80f; break;
                case EnemyTypeId.BayNhay: e.ThreatPoints = 6; e.GddCanonicalMaxHp = 120f; break;
                case EnemyTypeId.KeCatDay: e.ThreatPoints = 10; e.GddCanonicalMaxHp = 150f; break;
                case EnemyTypeId.ChoSuong: e.ThreatPoints = 10; e.GddCanonicalMaxHp = 260f; break;
                case EnemyTypeId.KeBatHeo: e.ThreatPoints = 12; e.GddCanonicalMaxHp = 220f; break;
                case EnemyTypeId.KeCaoPha: e.ThreatPoints = 15; e.GddCanonicalMaxHp = 280f; break;
                case EnemyTypeId.ThuBao: e.ThreatPoints = 16; e.GddCanonicalMaxHp = 320f; break;
                case EnemyTypeId.BongHuyen: e.ThreatPoints = 18; e.GddCanonicalMaxHp = 300f; break;
                case EnemyTypeId.KeMangDich: e.ThreatPoints = 20; e.GddCanonicalMaxHp = 350f; break;
                case EnemyTypeId.UfoTrinhSat: e.ThreatPoints = 22; e.GddCanonicalMaxHp = 240f; break;
                case EnemyTypeId.AlienGrunt: e.ThreatPoints = 25; e.GddCanonicalMaxHp = 300f; break;
                case EnemyTypeId.DauLinhTrom: e.ThreatPoints = 45; e.GddCanonicalMaxHp = 900f; break;
                case EnemyTypeId.ThietGiapCongThanh: e.ThreatPoints = 55; e.GddCanonicalMaxHp = 1400f; break;
                case EnemyTypeId.UfoThuHoach: e.ThreatPoints = 60; e.GddCanonicalMaxHp = 850f; break;
                case EnemyTypeId.AlienElite: e.ThreatPoints = 65; e.GddCanonicalMaxHp = 950f; break;
                case EnemyTypeId.UfoPhaoHam: e.ThreatPoints = 75; e.GddCanonicalMaxHp = 1100f; break;
                case EnemyTypeId.ChuaTromRiuSat: e.ThreatPoints = 300; e.GddCanonicalMaxHp = 12000f; e.MaxHp = 12000f; e.CurrentHp = 12000f; break;
                case EnemyTypeId.UfoMothership: e.ThreatPoints = 500; e.GddCanonicalMaxHp = 20000f; e.MaxHp = 20000f; e.CurrentHp = 20000f; break;
                case EnemyTypeId.ThucTheSuong: e.ThreatPoints = 600; e.GddCanonicalMaxHp = 28000f; e.MaxHp = 28000f; e.CurrentHp = 28000f; break;
            }
            return e;
        }

        /// <summary>
        /// GDD 19.2: 3 Phase của Boss (Phase 1: 100%-65%, Phase 2: 65%-30%, Phase 3: <30%).
        /// </summary>
        public int GetBossPhase()
        {
            float ratio = MaxHp > 0f ? CurrentHp / MaxHp : 0f;
            if (ratio > 0.65f) return 1;
            if (ratio > 0.30f) return 2;
            return 3;
        }

        public string GetBossPhaseBehavior()
        {
            int phase = GetBossPhase();
            return TypeId switch
            {
                EnemyTypeId.ChuaTromRiuSat => phase switch
                {
                    1 => "Phase 1 (100-65%): Phá cổng chính bằng Rìu Sắt, gọi Kẻ Bắt Heo",
                    2 => "Phase 2 (65-30%): Ném Bom Khói che tầm nhìn trụ, gọi Đầu Lĩnh Trộm & Kẻ Cắt Dây",
                    _ => "Phase 3 (<30%): Cuồng Nộ +35% tốc đánh, xoay rìu diện rộng"
                },
                EnemyTypeId.UfoMothership => phase switch
                {
                    1 => "Phase 1 (100-65%): Thả UFO Trinh Sát & Thu Hoạch, bắn tia laser quét",
                    2 => "Phase 2 (65-30%): Bật Khiên Năng Lượng, phóng xung EMP tắt điện vùng 20m trong 8s",
                    _ => "Phase 3 (<30%): Mở Cổng Hút Tập Trung hút nhiều heo cùng lúc, mưa pháo plasma"
                },
                EnemyTypeId.ThucTheSuong => phase switch
                {
                    1 => "Phase 1 (100-65%): Phủ Sương Đen toàn trại, triệu hồi Bóng Huyền & Chó Sương",
                    2 => "Phase 2 (65-30%): Dịch chuyển tức thời giữa các bóng tối, chỉ nhận 100% sát thương khi bị Đèn Pha rọi sáng",
                    _ => "Phase 3 (<30%): Phân Tách Ảo Ảnh (3 bản thể), rút Tinh Thần toàn đàn heo mỗi giây"
                },
                _ => $"Phase {phase}"
            };
        }
    }
}

