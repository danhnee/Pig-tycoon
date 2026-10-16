using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class PigTrait
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public bool IsPositive;
        public bool IsDeviant;
        public bool IsAwakened;
        public bool IsSealed;
        public string Description = string.Empty;
    }

    [Serializable]
    public class Pig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int AgeDays { get; set; }
        public float WeightKg { get; set; }
        public PigStage Stage { get; set; }
        public string Gender { get; set; } = "Duc";

        public GeneLineId GeneLine { get; set; }
        public GeneRarity Rarity { get; set; }
        public bool IsIdentified { get; set; }
        public bool IsXichMaoDomesticated { get; set; }

        public List<PigTrait> Traits { get; set; } = new List<PigTrait>();

        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int Temperament { get; set; } = 50;
        public int Mood { get; set; } = 70;
        public MoodState MoodState => GetMoodState();
        public float Bonding { get; set; } = 40f;
        public float AdhesionScore => (Bonding + Mood) / 2f;

        public MeatQuality MeatQuality { get; set; } = MeatQuality.B;
        public float GrowthRateMultiplier { get; set; } = 1.0f;
        public float FeedConversionMultiplier { get; set; } = 1.0f;
        public float MaxWeightKg { get; set; } = 110f;
        public bool IsQuarantinedForSale { get; set; } = false;
        public bool IsAlive { get; set; } = true;
        public bool IsInProcessingLot { get; set; } = false;
        public int DaysDeceased { get; set; } = 0;

        // Trạng thái Giám định 4 kênh & Bảo hiểm tích lũy (GDD 5.4)
        public int AppraisalFailureCount { get; set; } = 0; // Mỗi lần thất bại +5% (tối đa +15%)
        public int AppraisalLockHoursRemaining { get; set; } = 0; // Thất bại khóa cá thể 24h

        // Trạng thái Hậu duệ heo già 6 bước & Đột phá F1/F2 (GDD 4.4, 4.8)
        public float InfectionRiskMultiplier { get; set; } = 1.0f;
        public float LatentDiseaseRiskMultiplier { get; set; } = 1.0f;
        public bool IsDominantBreakthroughF1 { get; set; } = false;
        public string EliteSpecializationType { get; set; } = string.Empty; // "ToanDien" hoặc "DotPha"

        // Trạng thái không gian, ăn uống & dịch vụ (GDD 3.7, 4.1)
        public float PositionX { get; set; } = 0f;
        public float PositionY { get; set; } = 0f;
        public int Hunger { get; set; } = 0;
        public int Comfort { get; set; } = 70;
        public bool FedToday { get; set; } = true;
        public int OutsideServiceHoursToday { get; set; } = 0;
        public bool IsIsolated { get; set; } = false;

        // Trạng thái bệnh tật & miễn dịch (GDD 6.4–6.5)
        public DiseaseId? ActiveDisease { get; set; } = null;
        public int ImmuneDays { get; set; } = 0;
        public bool SeizurePending { get; set; } = false;

        // Trạng thái huyết thống, phối giống & mang thai (GDD 4.4, 4.8)
        public string MotherId { get; set; } = string.Empty;
        public string FatherId { get; set; } = string.Empty;
        public int Generation { get; set; } = 0; // 0 = F0, 1 = F1, 2 = F2+
        public bool IsInbred { get; set; } = false;
        public int GestationDaysRemaining { get; set; } = 0;
        public int LittersBorn { get; set; } = 0;
        public int DaysSinceLastBirth { get; set; } = 99;
        public string PendingFatherId { get; set; } = string.Empty;
        public GeneLineId PendingFatherGene { get; set; } = GeneLineId.HongDien;
        public PigStage PendingFatherStage { get; set; } = PigStage.TruongThanh;
        public bool PendingInbred { get; set; } = false;

        // Thuần dưỡng Xích Mao (3 ngày) & Cửa sổ định hình thịt giai đoạn Đang lớn (GDD 4.2, 4.7, 5.2)
        public int XichMaoDomesticationDays { get; set; } = 0;
        public int ShapingDaysRecorded { get; set; } = 0;
        public int ShapingDaysFed { get; set; } = 0;
        public int ShapingMoodSum { get; set; } = 0;
        public int ShapingHabitatSum { get; set; } = 0;
        public int ShapingSickDays { get; set; } = 0;

        private readonly Random random = new Random();

        public static Pig CreateDefault(string id, string name, GeneLineId geneLine, GeneRarity rarity, string gender = "Duc", int initialAge = 0, float initialWeight = 1.5f)
        {
            bool isHighRarity = rarity != GeneRarity.Thuong && rarity != GeneRarity.Kha;
            var traits = new List<PigTrait>();

            if (geneLine == GeneLineId.HuThe)
            {
                traits.Add(new PigTrait
                {
                    Id = "ThitHuKhong",
                    Name = "Thịt Hư Không",
                    IsPositive = true,
                    IsDeviant = true,
                    Description = "Chợ đêm có đơn trao đổi riêng, giá trị x3"
                });
                traits.Add(new PigTrait
                {
                    Id = "HutSuong",
                    Name = "Hút Sương",
                    IsPositive = false,
                    IsDeviant = true,
                    IsAwakened = false, // Ngủ ban đầu
                    IsSealed = false,
                    Description = "Khi thức: ÁLSK +Vừa/ngày, thiên về sương mù"
                });
            }

            var pig = new Pig
            {
                Id = id,
                Name = name,
                AgeDays = initialAge,
                WeightKg = initialWeight,
                Stage = PigStage.HeoNon,
                Gender = gender,
                GeneLine = geneLine,
                Rarity = rarity,
                IsIdentified = !isHighRarity,
                IsXichMaoDomesticated = false,
                Traits = traits,
                Health = 100,
                Temperament = 50,
                Mood = 70,
                Bonding = geneLine == GeneLineId.KimTho ? 60f : 40f,
                MeatQuality = MeatQuality.B,
                GrowthRateMultiplier = geneLine == GeneLineId.LoiMach ? 1.22f : (geneLine == GeneLineId.HongDien ? 1.10f : 1.0f),
                MaxWeightKg = geneLine == GeneLineId.HongDien ? 105f : (geneLine == GeneLineId.KimTho ? 95f : (geneLine == GeneLineId.ThietBi ? 130f : 110f)),
                IsQuarantinedForSale = false,
                IsAlive = true,
                IsInProcessingLot = false,
                DaysDeceased = 0
            };

            pig.UpdateStageAndLimits();
            return pig;
        }

        public MoodState GetMoodState()
        {
            if (Mood >= 60) return MoodState.BinhOn;
            if (Mood >= 40) return MoodState.BatAn;
            if (Mood >= 20) return MoodState.HoangSo;
            return MoodState.HoangLoan;
        }

        public void UpdateStageAndLimits()
        {
            int adultEndDay = GeneLine == GeneLineId.MocCuoc ? 34 : 28;
            if (AgeDays <= 4) Stage = PigStage.HeoNon;
            else if (AgeDays <= 10) Stage = PigStage.DangLon;
            else if (AgeDays <= adultEndDay) Stage = PigStage.TruongThanh;
            else Stage = PigStage.HeoGia;
        }

        public (bool sealedSuccess, string message) SealDeviantTrait(string traitId = "")
        {
            if (!IsAlive) return (false, "Heo đã chết.");
            PigTrait? target = string.IsNullOrEmpty(traitId)
                ? Traits.Find(t => t.IsDeviant && !t.IsPositive)
                : Traits.Find(t => t.Id == traitId && t.IsDeviant);

            if (target == null)
            {
                return (false, "Không tìm thấy tính trạng Dị Biến cần phong ấn.");
            }
            if (target.IsAwakened)
            {
                return (false, "Tính trạng Dị Biến đã thức tỉnh, không thể phong ấn!");
            }

            target.IsSealed = true;
            return (true, $"Đã phong ấn vĩnh viễn tính trạng Dị Biến [{target.Name}]!");
        }

        public (bool trained, string message) TrainXichMaoDomesticationDay()
        {
            if (!IsAlive || GeneLine != GeneLineId.XichMao)
                return (false, "Chỉ dòng gen Xích Mao mới cần thuần dưỡng chiến đấu.");
            if (IsXichMaoDomesticated)
                return (true, "Xích Mao này đã hoàn tất thuần dưỡng.");
            if (Mood < 60)
                return (false, "Cần Tinh thần Bình Ổn (≥ 60) để thuần dưỡng Xích Mao.");

            XichMaoDomesticationDays += 1;
            Bonding = Math.Min(100f, Bonding + 5f);
            if (XichMaoDomesticationDays >= 3)
            {
                IsXichMaoDomesticated = true;
                return (true, $"{Name} đã hoàn tất 3 ngày thuần dưỡng Xích Mao (tăng trọng +8%, mở húc trả kẻ trộm)!");
            }
            return (true, $"Thuần dưỡng Xích Mao [{Name}]: {XichMaoDomesticationDays}/3 ngày.");
        }

        public void RecordGrowthShapingSample(bool fedFullRation, int habitatIndex)
        {
            if (Stage != PigStage.DangLon || !IsAlive) return;
            ShapingDaysRecorded += 1;
            if (fedFullRation) ShapingDaysFed += 1;
            ShapingMoodSum += Mood;
            ShapingHabitatSum += habitatIndex;
            if (ActiveDisease.HasValue) ShapingSickDays += 1;
        }

        public void AdvanceDay(int habitatIndex, bool isInHerdState, float densityPenalty)
        {
            if (!IsAlive)
            {
                DaysDeceased += 1;
                return;
            }

            if (Stage == PigStage.DangLon)
            {
                RecordGrowthShapingSample(FedToday, habitatIndex);
            }

            AgeDays += 1;
            PigStage prevStage = Stage;
            UpdateStageAndLimits();

            if (ImmuneDays > 0)
            {
                ImmuneDays -= 1;
            }

            if (GestationDaysRemaining == 0)
            {
                DaysSinceLastBirth += 1;
            }

            // 1. Hòa nhập đàn (tính trạng Dễ Bảo +3/ngày)
            float bondingRate = 3.0f;
            if (habitatIndex < 30) bondingRate = -1.0f;
            else if (habitatIndex < 50) bondingRate = 1.5f;

            if (GeneLine == GeneLineId.KimTho) bondingRate *= 2.0f;
            if (Traits.Exists(t => t.Id == "DeBao" && !t.IsSealed)) bondingRate += 3.0f;
            Bonding = Math.Max(0f, Math.Min(100f, Bonding + bondingRate));

            // 2. Tinh thần hồi phục & tác động bệnh tật
            if (MoodState != MoodState.HoangLoan && FedToday)
            {
                Mood = Math.Min(100, Mood + 5);
            }
            else if (!FedToday)
            {
                Mood = Math.Max(0, Mood - 12);
                Health = Math.Max(0, Health - 8);
            }

            if (ActiveDisease.HasValue)
            {
                ApplyDailyDiseaseSymptoms(ActiveDisease.Value);
                if (!IsAlive) return;
            }

            // 3. Khóa phẩm chất thịt (C/B/A/S) và Phong ấn Hư Thể khi kết thúc Đang Lớn -> Trưởng Thành
            if (prevStage == PigStage.DangLon && Stage == PigStage.TruongThanh)
            {
                if (ShapingDaysRecorded > 0)
                {
                    float feedRatio = (float)ShapingDaysFed / ShapingDaysRecorded;
                    float avgMood = (float)ShapingMoodSum / ShapingDaysRecorded;
                    int avgSc = ShapingHabitatSum / ShapingDaysRecorded;
                    MeatQuality = GeneticsAndBreedingSystem.EvaluateMeatQualityShaping(feedRatio, avgMood, avgSc, ShapingSickDays);
                }

                if (GeneLine == GeneLineId.HuThe)
                {
                    var badTrait = Traits.Find(t => t.IsDeviant && !t.IsPositive);
                    if (badTrait != null && !badTrait.IsAwakened)
                    {
                        badTrait.IsSealed = true;
                    }
                }
            }

            // 4. Kiểm tra thức tỉnh Hư Thể
            if (GeneLine == GeneLineId.HuThe)
            {
                var badTrait = Traits.Find(t => t.IsDeviant && !t.IsPositive);
                if (badTrait != null && !badTrait.IsAwakened && !badTrait.IsSealed)
                {
                    bool hasBadCondition = Mood < 40 || MoodState == MoodState.HoangLoan || Health < 60;
                    if (hasBadCondition)
                    {
                        double chance = isInHerdState ? 0.50 : 1.0;
                        if (random.NextDouble() < chance)
                        {
                            badTrait.IsAwakened = true;
                        }
                    }
                }
            }

            // 5. Tự động tích lũy thuần dưỡng Xích Mao nếu đủ điều kiện
            if (GeneLine == GeneLineId.XichMao && !IsXichMaoDomesticated && Stage == PigStage.TruongThanh && Mood >= 60 && FedToday)
            {
                TrainXichMaoDomesticationDay();
            }

            // 6. Tăng trọng
            GrowWeight(densityPenalty);
            OutsideServiceHoursToday = 0;

            // 7. Tử vong tự nhiên
            CheckNaturalMortality();
        }

        private void ApplyDailyDiseaseSymptoms(DiseaseId disease)
        {
            switch (disease)
            {
                case DiseaseId.HoHapLanh:
                case DiseaseId.GheKySinh:
                    Health = Math.Max(0, Health - 6);
                    Mood = Math.Max(0, Mood - 5);
                    break;
                case DiseaseId.DichTaHeo:
                    Health = Math.Max(0, Health - 15);
                    Mood = Math.Max(0, Mood - 12);
                    break;
                case DiseaseId.SotBun:
                    Health = Math.Max(0, Health - 8);
                    Mood = Math.Max(0, Mood - 6);
                    break;
                case DiseaseId.SayNang:
                    Health = Math.Max(0, Health - 10);
                    Mood = Math.Max(0, Mood - 10);
                    break;
                case DiseaseId.RoiLoanSinhSan:
                    Mood = Math.Max(0, Mood - 6);
                    break;
                case DiseaseId.NamPhatQuang:
                    Health = Math.Max(0, Health - 8);
                    Mood = Math.Max(0, Mood - 8);
                    break;
                case DiseaseId.LoiTam:
                    SeizurePending = true;
                    Health = Math.Max(0, Health - 10);
                    Mood = Math.Max(0, Mood - 10);
                    break;
                case DiseaseId.HuMach:
                    Health = Math.Max(0, Health - 25);
                    Mood = Math.Max(0, Mood - 15);
                    break;
            }

            if (Health <= 0)
            {
                Die();
            }
        }

        private void GrowWeight(float densityPenalty)
        {
            if (Stage == PigStage.HeoGia)
            {
                WeightKg = Math.Max(45f, WeightKg - 1.0f);
                return;
            }

            if (!FedToday)
            {
                return;
            }

            float dailyGain = 0f;
            if (Stage == PigStage.HeoNon) dailyGain = 2.1f;
            else if (Stage == PigStage.DangLon) dailyGain = 8.0f;
            else if (Stage == PigStage.TruongThanh) dailyGain = 3.0f;

            float effectiveRate = GrowthRateMultiplier * (1f - densityPenalty);
            if (IsXichMaoDomesticated) effectiveRate *= 1.08f;
            if (OutsideServiceHoursToday > 0) effectiveRate *= 0.90f; // Ngoài tầm phục vụ 30m: -10% tăng trọng (GDD 3.7)
            if (MoodState == MoodState.BatAn) effectiveRate *= 0.95f;  // Bất an: -5% tăng trọng (GDD 4.2)
            if (ActiveDisease.HasValue) effectiveRate *= 0.75f;
            if (Traits.Exists(t => t.Id == "HauAn")) effectiveRate *= 1.12f;
            if (Traits.Exists(t => t.Id == "KenAn" && !t.IsSealed)) effectiveRate *= 0.85f;

            WeightKg = Math.Min(MaxWeightKg, WeightKg + dailyGain * effectiveRate);
        }

        private void CheckNaturalMortality()
        {
            int maxLifespan = GeneLine == GeneLineId.KimTho ? 56 : 40;
            int mortalityStartAge = maxLifespan - 4;

            if (AgeDays >= mortalityStartAge)
            {
                int daysOver = AgeDays - mortalityStartAge + 1;
                double deathChance = Math.Min(1.0, 0.05 * daysOver);
                if (random.NextDouble() < deathChance)
                {
                    Die();
                }
            }
        }

        public void Die()
        {
            IsAlive = false;
            Health = 0;
            Mood = 0;
        }

        public void ApplyNeoTinhThan(int threshold)
        {
            if (IsAlive && Mood < threshold)
            {
                Mood = threshold;
            }
        }
    }
}
