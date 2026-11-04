using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// GDD v6.0 chương 3–7 dồn về một chỗ: bảng EXP cấp trại (3.5), áp lực mật độ (3.3), hiệu ứng bậc Sinh Cảnh (6.1),
    /// tính chất phụ (5.3 / 4.8 bước 6), bệnh tiềm ẩn theo dòng (5.2), chọn bệnh theo nguồn phát (6.4), lây 6 giờ (6.5),
    /// tỉ lệ bán không kiểm dịch (4.5). Số nào GDD không cho được ghi PROVISIONAL trong ADR-0019.
    /// </summary>
    public static class HerdLifecycleRules
    {
        // ------------------------------------------------------------------
        // 3.5 Cấp trại: GDD chỉ cho mốc đầu mỗi dải (1.200 / 5.000 / 16.000 / 40.000 / 95.000, trần 300.000).
        // Mốc từng cấp bên trong dải chia đều tuyến tính — PROVISIONAL (ADR-0019).
        // ------------------------------------------------------------------
        public const int MaxFarmLevel = 20;
        private static readonly int[] FarmLevelExpThreshold = BuildFarmLevelThresholds();

        private static int[] BuildFarmLevelThresholds()
        {
            var t = new int[MaxFarmLevel + 1];
            void Band(int firstLevel, int lastLevel, int fromExp, int toExp)
            {
                int levels = lastLevel - firstLevel + 1;
                for (int i = 0; i < levels; i++)
                {
                    t[firstLevel + i] = fromExp + (int)Math.Round((toExp - fromExp) * (double)i / levels);
                }
            }
            Band(1, 2, 0, 1200);
            Band(3, 4, 1200, 5000);
            Band(5, 7, 5000, 16000);
            Band(8, 10, 16000, 40000);
            Band(11, 14, 40000, 95000);
            Band(15, 20, 95000, 300000);
            return t;
        }

        /// <summary>EXP tích lũy cần để đứng ở cấp <paramref name="level"/> (cấp 1 = 0).</summary>
        public static int CumulativeExpForLevel(int level) => FarmLevelExpThreshold[Math.Clamp(level, 1, MaxFarmLevel)];

        public static int FarmLevelForExp(int cumulativeExp)
        {
            int level = 1;
            for (int l = 2; l <= MaxFarmLevel; l++)
            {
                if (cumulativeExp >= FarmLevelExpThreshold[l]) level = l;
            }
            return level;
        }

        // ------------------------------------------------------------------
        // 3.3 Áp lực mật độ (nhóm Trại)
        // ------------------------------------------------------------------
        public static int DensityDailyMoodLoss(float density)
        {
            if (density > 1.50f) return 4;
            if (density > 1.20f) return 2;
            if (density > 1.00f) return 1;
            return 0;
        }

        /// <summary>Tranh máng: lượng cám cần cấp ×1,10 khi quá tải (từ 101%).</summary>
        public static float DensityFeedMultiplier(float density) => density > 1.00f ? 1.10f : 1.00f;

        /// <summary>Tăng trọng: ≤70% +3%, 121–150% −8%, &gt;150% −15%.</summary>
        public static float DensityGrowthModifier(float density)
        {
            if (density <= 0.70f) return 0.03f;
            if (density > 1.50f) return -0.15f;
            if (density > 1.20f) return -0.08f;
            return 0f;
        }

        public static float DensityMoodRecoveryBonus(float density) => density <= 0.70f ? 0.10f : 0f;

        // ------------------------------------------------------------------
        // 6.1 Năm bậc Sinh Cảnh (nhóm Trại)
        // ------------------------------------------------------------------
        public static float HabitatGrowthModifier(int sc)
        {
            if (sc >= 90) return 0.04f;
            if (sc >= 50) return 0f;
            if (sc >= 30) return -0.05f;
            return -0.12f;
        }

        public static float HabitatMoodRecoveryBonus(int sc)
        {
            if (sc >= 90) return 0.15f;
            if (sc >= 70) return 0.05f;
            return 0f;
        }

        public static float HabitatExtraBondingPerDay(int sc) => sc >= 90 ? 1f : 0f;

        public static int HabitatDailyMoodLoss(int sc) => sc < 30 ? 2 : 0;

        /// <summary>Ổn Định (và cao hơn): heo non chết do rét/thiếu mẹ −30%.</summary>
        public static float HabitatPigletMortalityFactor(int sc) => sc >= 70 ? 0.70f : 1.00f;

        /// <summary>Trong Lành: tính chất xấu Hư Thể thức tỉnh −25%.</summary>
        public static double HabitatDeviantAwakenFactor(int sc) => sc >= 90 ? 0.75 : 1.0;

        /// <summary>Trong Lành: +5% cơ hội lên hạng thịt.</summary>
        public static double HabitatMeatUpgradeChance(int sc) => sc >= 90 ? 0.05 : 0.0;

        // ------------------------------------------------------------------
        // 4.2 Tính khí + 5.2 Thổ Trầm "Tâm Tĩnh": giảm Tinh thần khi bị kích động
        // ------------------------------------------------------------------
        /// <summary>
        /// Lượng Tinh thần mất thực tế. Tính khí: mỗi điểm trên 50 mất ít hơn 1%, dưới 50 mất nhiều hơn 1%/điểm (nhóm Gen &amp; tính chất).
        /// Thổ Trầm chỉ mất 50% (×0,5); heo khác trong 5 m của Thổ Trầm mất −20% (×0,8).
        /// </summary>
        public static int AgitationMoodLoss(Pig pig, int baseLoss, bool nearThoTram)
        {
            if (baseLoss <= 0 || pig == null) return 0;
            float temperament = 1f - (pig.Temperament - 50) / 100f;
            float factor = temperament;
            if (pig.GeneLine == GeneLineId.ThoTram) factor *= 0.5f;
            else if (nearThoTram) factor *= 0.8f;
            return Math.Max(0, (int)Math.Round(baseLoss * Math.Max(0f, factor)));
        }

        public static bool IsNearThoTram(Pig pig, IList<Pig> herd)
        {
            for (int i = 0; i < herd.Count; i++)
            {
                var o = herd[i];
                if (o == null || o == pig || !o.IsAlive || o.GeneLine != GeneLineId.ThoTram) continue;
                float dx = o.PositionX - pig.PositionX, dy = o.PositionY - pig.PositionY;
                if (dx * dx + dy * dy <= 25f) return true;
            }
            return false;
        }

        public static void ApplyAgitation(Pig pig, int baseLoss, IList<Pig> herd, int today = -1)
        {
            if (pig == null || !pig.IsAlive) return;
            int loss = AgitationMoodLoss(pig, baseLoss, IsNearThoTram(pig, herd));
            if (pig.CalmFeedDay >= 0 && pig.CalmFeedDay == today) loss = (int)Math.Round(loss * 0.8f); // Cám Hạ Hỏa (GDD 12.8)
            pig.Mood = Math.Max(0, pig.Mood - loss);
        }

        // ------------------------------------------------------------------
        // 5.3 Tính chất phụ — mã giữ tương thích với các hệ đang đọc (HauAn, MatTay, DeNhiem...)
        // ------------------------------------------------------------------
        private static readonly (string id, string name, string desc)[] GoodTraits =
        {
            ("DeBao", "Trung Thành", "Luôn bám theo thủ lĩnh/đàn, không bao giờ chạy lạc khi Hoảng sợ."),
            ("DanDon", "Lì Đòn", "Sống sót 1 lần khi bị quái hạ (còn 1 HP), 1 lần/đời."),
            ("NhayDem", "Kêu Báo", "Phát hiện quái tàng hình trong 6 m và kêu báo động."),
            ("MatTay", "Mắn Đẻ", "Nái +1 con/lứa."),
            ("HauAn", "Ăn Khỏe", "Thức ăn cho 1 kg −6%."),
            ("ThichBun", "Sạch Sẽ", "Không làm bẩn khu nằm; SC khu có nó −50% hao."),
        };

        private static readonly (string id, string name, string desc)[] BadTraits =
        {
            ("LuoiDi", "Tò Mò", "Hay ra rìa trại ban đêm; là mục tiêu dễ của Kẻ Bắt Heo."),
            ("HayHuc", "Cắn Đàn", "Mật độ > 100%: cắn heo bên cạnh, gây vết thương dễ nhiễm trùng."),
            ("SoDong", "Ồn Ào", "Khi Hoảng sợ, kêu làm heo trong 8 m mất Tinh thần −2/giây."),
            ("DeNhiem", "Hiếm Muộn", "Thụ thai −10%; cần thú y chẩn đoán mới phân biệt với xui ngẫu nhiên."),
            ("KenAn", "Kén Ăn", "Bỏ bữa khi đổi loại cám: tăng trọng ngày đó −30%."),
            ("KhoNgu", "Đoản Thọ", "Tuổi thọ −3 ngày."),
        };

        private static readonly (string id, string name, string desc)[] DeviantGood =
        {
            ("ThitHuKhong", "Thịt Hư Không", "Chợ đêm có đơn trao đổi riêng, giá trị ×3."),
            ("DaPhanQuang", "Vô Ảnh", "UFO không khóa được chùm kéo vào cá thể này."),
            ("NhipTimCham", "Cộng Sinh", "Heo trong 5 m tăng trọng +5%, Tinh thần hồi +10%."),
        };

        private static readonly (string id, string name, string desc)[] DeviantBad =
        {
            ("HutSuong", "Hút Sương", "Khi thức: ÁLSK +Vừa/ngày, thiên về sương mù."),
            ("TiengKeuLech", "Tự Phân Rã", "Khi thức: tuổi thọ −50%; xác gây SC −6/ngày."),
            ("BongDoi", "Hư Nhiễm", "Khi thức: mang Hư Mạch, lây qua tiếp xúc."),
        };

        /// <summary>Đánh dấu thẻ đã tung tính chất phụ (không hiện trên thẻ).</summary>
        public static bool HasRolledSecondaryTraits(Pig pig) => pig != null && pig.SecondaryTraitsRolled;

        /// <summary>
        /// GDD 4.8 bước 6 / 5.3: số phụ 0 (40%) • 1 (45%) • 2 (15%); tốt/xấu Thường–Khá 60/40, Hiếm trở lên 50/50.
        /// Gen Xấu: thêm 1 phụ xấu chắc chắn + 30% phụ xấu thứ 2. Hư Thể: 1 Dị Biến tốt + 1 Dị Biến xấu (ngủ).
        /// Gọi một lần cho mỗi cá thể; lần sau không làm gì.
        /// </summary>
        public static void RollSecondaryTraits(Pig pig, Random rng, bool badGene = false)
        {
            if (pig == null || rng == null || pig.SecondaryTraitsRolled) return;
            pig.SecondaryTraitsRolled = true;

            if (pig.GeneLine == GeneLineId.HuThe)
            {
                pig.Traits.RemoveAll(t => t.IsDeviant);
                var g = DeviantGood[rng.Next(DeviantGood.Length)];
                var b = DeviantBad[rng.Next(DeviantBad.Length)];
                pig.Traits.Add(new PigTrait { Id = g.id, Name = g.name, IsPositive = true, IsDeviant = true, Description = g.desc });
                pig.Traits.Add(new PigTrait { Id = b.id, Name = b.name, IsPositive = false, IsDeviant = true, Description = b.desc });
                return;
            }

            double countRoll = rng.NextDouble();
            int count = countRoll < 0.40 ? 0 : (countRoll < 0.85 ? 1 : 2);
            double goodShare = pig.Rarity >= GeneRarity.Hiem ? 0.50 : 0.60;
            for (int i = 0; i < count; i++)
            {
                bool good = rng.NextDouble() < goodShare;
                AddDistinct(pig, good ? GoodTraits : BadTraits, good, rng);
            }

            if (badGene)
            {
                AddDistinct(pig, BadTraits, false, rng);
                if (rng.NextDouble() < 0.30) AddDistinct(pig, BadTraits, false, rng);
            }
        }

        private static void AddDistinct(Pig pig, (string id, string name, string desc)[] pool, bool good, Random rng)
        {
            var free = new List<(string id, string name, string desc)>();
            foreach (var t in pool)
            {
                if (!pig.Traits.Exists(x => x.Id == t.id)) free.Add(t);
            }
            if (free.Count == 0) return;
            var pick = free[rng.Next(free.Count)];
            pig.Traits.Add(new PigTrait { Id = pick.id, Name = pick.name, IsPositive = good, Description = pick.desc });
        }

        public static bool HasActiveTrait(Pig pig, params string[] ids)
        {
            if (pig?.Traits == null) return false;
            foreach (var t in pig.Traits)
            {
                if (t.IsSealed) continue;
                for (int i = 0; i < ids.Length; i++) if (t.Id == ids[i]) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // 5.2 cột "Tiềm ẩn": bệnh tiềm ẩn theo dòng (Cứng Khớp, Mộng Du không phải bệnh — 6.5)
        // ------------------------------------------------------------------
        public static (DiseaseId disease, float chance)? LatentDiseaseOfLine(GeneLineId line)
        {
            return line switch
            {
                GeneLineId.ThoTram => (DiseaseId.SotBun, 0.01f),
                GeneLineId.XichMao => (DiseaseId.LoiTam, 0.03f),
                GeneLineId.PhongMau => (DiseaseId.RoiLoanSinhSan, 0.02f),
                GeneLineId.TinhQuang => (DiseaseId.NamPhatQuang, 0.08f),
                GeneLineId.LoiMach => (DiseaseId.LoiTam, 0.10f),
                GeneLineId.KimTho => (DiseaseId.RoiLoanSinhSan, 0.03f),
                _ => null
            };
        }

        /// <summary>
        /// Tung bệnh tiềm ẩn lúc sinh. Gen Xấu: ×2 (LatentDiseaseRiskMultiplier). Cận huyết: +8 đ% (GDD 4.4) — chỉ cộng cho dòng
        /// có bệnh tiềm ẩn (PROVISIONAL, ADR-0019).
        /// </summary>
        public static void RollLatentDisease(Pig pig, Random rng)
        {
            if (pig == null || rng == null || pig.LatentDisease.HasValue) return;
            var latent = LatentDiseaseOfLine(pig.GeneLine);
            if (latent == null) return;
            float chance = latent.Value.chance + (pig.IsInbred ? 0.08f : 0f);
            chance *= Math.Max(1f, pig.LatentDiseaseRiskMultiplier);
            if (rng.NextDouble() < Math.Min(1f, chance)) pig.LatentDisease = latent.Value.disease;
        }

        /// <summary>
        /// Bệnh tiềm ẩn bộc phát khi gặp đúng nguồn phát của bảng 6.4: Lôi Tâm khi Tinh thần &lt; 30; Nấm Phát Quang khi Mưa Bức Xạ
        /// hoặc trời ẩm (Mưa, Nồm Ẩm); Sốt Bùn khi Mưa; Rối Loạn Sinh Sản khi Tinh thần thấp (&lt; 40, PROVISIONAL).
        /// </summary>
        public static bool LatentTriggered(Pig pig, WeatherType weather)
        {
            if (pig == null || !pig.LatentDisease.HasValue) return false;
            return pig.LatentDisease.Value switch
            {
                DiseaseId.LoiTam => pig.Mood < 30,
                DiseaseId.NamPhatQuang => weather == WeatherType.MuaBucXa || weather == WeatherType.Mua || weather == WeatherType.NomAm,
                DiseaseId.SotBun => weather == WeatherType.Mua,
                DiseaseId.RoiLoanSinhSan => pig.Mood < 40,
                _ => false
            };
        }

        // ------------------------------------------------------------------
        // 6.4 Chọn bệnh nhóm A/B theo nguồn phát (công thức 6.5 chỉ áp cho A và B)
        // ------------------------------------------------------------------
        public static DiseaseId? ChooseOutbreakDisease(Pig pig, WeatherType weather, bool barnContaminated)
        {
            if (weather == WeatherType.NomAm)
            {
                // Ghẻ Ký Sinh: Nồm Ẩm, ổ lót bẩn. Thiết Bì miễn Ghẻ.
                if (pig.GeneLine != GeneLineId.ThietBi) return DiseaseId.GheKySinh;
                return barnContaminated ? DiseaseId.SotBun : (DiseaseId?)null;
            }
            if (weather == WeatherType.Mua)
            {
                // Sốt Bùn: bãi bùn bẩn + Mưa; nếu bãi chưa bẩn thì Hô Hấp Lạnh (Mưa)
                return barnContaminated ? DiseaseId.SotBun : DiseaseId.HoHapLanh;
            }
            if (weather == WeatherType.RetDam) return DiseaseId.HoHapLanh;
            // Không có nguồn thời tiết: Chuồng Nhiễm/bãi bẩn → Sốt Bùn; còn lại là "miễn dịch đàn thấp" → Hô Hấp Lạnh
            return barnContaminated ? DiseaseId.SotBun : DiseaseId.HoHapLanh;
        }

        public static bool IsContagious(DiseaseId id)
        {
            var g = DiseaseBoard.GroupOf(id);
            return g == DiseaseGroup.A || g == DiseaseGroup.B;
        }

        /// <summary>
        /// GDD 6.5: P(lây) = 10% × Mật độ × SC × Thời tiết × Tinh thần (Bất an ×1,1) × Thủ lĩnh (0,75 nếu có).
        /// Thêm nhóm A ×1,3 khi Chen Chúc (3.7) và hệ số dễ lây cá thể (Sức khỏe &lt; 40 ×1,5; Thể Trạng Yếu ×1,3).
        /// </summary>
        public static float SpreadChance(Pig target, DiseaseId disease, float densityFactor, int habitat, WeatherType weather,
            bool hasLeader, float localCrowdingMultiplier)
        {
            float p = 0.10f * densityFactor * DiseaseBoard.HabitatDiseaseFactor(habitat) * DiseaseBoard.WeatherSpread(disease, weather, false);
            if (target.MoodState == MoodState.BatAn) p *= 1.1f;
            if (hasLeader) p *= 0.75f;
            if (DiseaseBoard.GroupOf(disease) == DiseaseGroup.A) p *= Math.Max(1f, localCrowdingMultiplier);
            if (target.Health < 40) p *= 1.5f;
            p *= Math.Max(1f, target.InfectionRiskMultiplier);
            return Math.Clamp(p, 0f, 1f);
        }

        /// <summary>Bán kính tiếp xúc cho kiểm tra lây = 1 Ô Đất (10 m) — PROVISIONAL (ADR-0019).</summary>
        public const float ContactRadiusMeters = 10f;

        // ------------------------------------------------------------------
        // 4.5 Kiểm dịch: tỉ lệ bán thành công nếu KHÔNG kiểm dịch; heo chưa định danh tính như Thường
        // ------------------------------------------------------------------
        public static float UnquarantinedSaleSuccessRate(Pig pig)
        {
            if (pig == null) return 0f;
            GeneRarity r = pig.IsIdentified ? pig.Rarity : GeneRarity.Thuong;
            return r switch
            {
                GeneRarity.Thuong => 0.70f,
                GeneRarity.Kha => 0.65f,
                GeneRarity.Hiem => 0.55f,
                GeneRarity.Quy => 0.45f,
                GeneRarity.HuyenThoai => 0.35f,
                _ => 0f
            };
        }

        /// <summary>Bệnh "đã biết": có triệu chứng. Rối Loạn Sinh Sản không triệu chứng (6.4), bệnh tiềm ẩn chưa lộ.</summary>
        public static bool IsKnownSick(Pig pig) => pig != null && pig.ActiveDisease.HasValue && pig.ActiveDisease.Value != DiseaseId.RoiLoanSinhSan;

        /// <summary>Có bệnh mà người mua phát hiện sau đó (kể cả bệnh không triệu chứng và bệnh tiềm ẩn).</summary>
        public static bool CarriesAnyIllness(Pig pig) => pig != null && (pig.ActiveDisease.HasValue || pig.LatentDisease.HasValue);

        // ------------------------------------------------------------------
        // 6.2 Dọn bãi / vệ sinh: +4 mỗi lần, tối đa +12/ngày; Stamina 8–12 (GDD 9.2) +25% nếu SC &lt; 40, +15% khi Mưa
        // ------------------------------------------------------------------
        public const int CleaningBaseStamina = 8; // cận dưới của khoảng 8–12 (ADR-0019)
        public const int MaxCountedCleaningsPerDay = 3;

        public static float CleaningStaminaCost(GameEngine engine)
        {
            float extra = 0f;
            if (engine.Farm.HabitatIndex < 40) extra += 0.25f;
            if (engine.CurrentWeather == WeatherType.Mua) extra += 0.15f;
            return PigCareCommands.CareStaminaCost(engine, CleaningBaseStamina) * (1f + extra);
        }

        public static (bool success, string message) TryCleanYard(GameEngine engine)
        {
            if (engine == null) return (false, "Chưa có dữ liệu trại.");
            if (engine.Farm.CleaningsToday >= MaxCountedCleaningsPerDay)
                return (false, $"Hôm nay đã dọn đủ {MaxCountedCleaningsPerDay} lần (SC tối đa +12/ngày).");
            float cost = CleaningStaminaCost(engine);
            if (!engine.Character.Stamina.TryConsumeStamina(cost, 1f))
                return (false, $"Không đủ thể lực để dọn bãi (cần {cost:0.#}).");
            engine.Farm.CleaningsToday += 1;
            engine.DeepMechanics.SpecialtyLocks.RecordManualCleaning(); // tự tay dọn vệ sinh (khóa Khoa 2, nhiệm vụ Nông Công)
            return (true, $"Đã dọn bãi, vệ sinh khu nằm (lần {engine.Farm.CleaningsToday}/{MaxCountedCleaningsPerDay} hôm nay, SC +4 khi chốt ngày, −{cost:0.#} thể lực).");
        }
    }
}
