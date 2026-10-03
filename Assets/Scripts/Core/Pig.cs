using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class PigTrait
    {
        public string Id;
        public string Name;
        public bool IsPositive;
        public bool IsDeviant;
        public bool IsAwakened;
        public bool IsSealed;
        public string Description;
    }

    [Serializable]
    public class Pig
    {
        public string Id { get; set; }
        public string Name { get; set; }
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
        public int Temperament { get; set; } = 50;
        public int Mood { get; set; } = 70;
        public MoodState MoodState => GetMoodState();
        public float Bonding { get; set; } = 40f;
        public float AdhesionScore => (Bonding + Mood) / 2f;

        public MeatQuality MeatQuality { get; set; } = MeatQuality.B;
        public float GrowthRateMultiplier { get; set; } = 1.0f;
        public float MaxWeightKg { get; set; } = 110f;
        public bool IsQuarantinedForSale { get; set; } = false;
        public bool IsAlive { get; set; } = true;
        public bool IsInProcessingLot { get; set; } = false;
        public int DaysDeceased { get; set; } = 0;

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
                GrowthRateMultiplier = geneLine == GeneLineId.LoiMach ? 1.22f : 1.0f,
                MaxWeightKg = geneLine == GeneLineId.HongDien ? 105f : (geneLine == GeneLineId.KimTho ? 95f : 110f),
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

        public void AdvanceDay(int habitatIndex, bool isInHerdState, float densityPenalty)
        {
            if (!IsAlive)
            {
                DaysDeceased += 1;
                return;
            }

            AgeDays += 1;
            PigStage prevStage = Stage;
            UpdateStageAndLimits();

            // 1. Hòa nhập đàn
            float bondingRate = 3.0f;
            if (habitatIndex < 30) bondingRate = -1.0f;
            else if (habitatIndex < 50) bondingRate = 1.5f;

            if (GeneLine == GeneLineId.KimTho) bondingRate *= 2.0f;
            Bonding = Math.Max(0f, Math.Min(100f, Bonding + bondingRate));

            // 2. Tinh thần hồi phục
            if (MoodState != MoodState.HoangLoan)
            {
                Mood = Math.Min(100, Mood + 5);
            }

            // 3. Phong ấn Hư Thể khi kết thúc Đang Lớn
            if (GeneLine == GeneLineId.HuThe && prevStage == PigStage.DangLon && Stage == PigStage.TruongThanh)
            {
                var badTrait = Traits.Find(t => t.IsDeviant && !t.IsPositive);
                if (badTrait != null && !badTrait.IsAwakened)
                {
                    badTrait.IsSealed = true;
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

            // 5. Tăng trọng
            GrowWeight(densityPenalty);

            // 6. Tử vong tự nhiên
            CheckNaturalMortality();
        }

        private void GrowWeight(float densityPenalty)
        {
            if (Stage == PigStage.HeoGia)
            {
                WeightKg = Math.Max(45f, WeightKg - 1.0f);
                return;
            }

            float dailyGain = 0f;
            if (Stage == PigStage.HeoNon) dailyGain = 2.1f;
            else if (Stage == PigStage.DangLon) dailyGain = 8.0f;
            else if (Stage == PigStage.TruongThanh) dailyGain = 3.0f;

            float effectiveRate = GrowthRateMultiplier * (1f - densityPenalty);
            if (IsXichMaoDomesticated) effectiveRate *= 1.08f;

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
