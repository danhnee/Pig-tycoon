using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class HerdConditionsReport
    {
        public int CohesivePigsCount;
        public bool Condition1_Met;
        public int CurrentSC;
        public bool Condition2_Met;
        public float AvgMood;
        public bool HasKimTho;
        public int RequiredAvgMood;
        public bool AnyPanic;
        public bool Condition3_Met;
        public float DensityRatio;
        public bool Condition4_Met;
        public int ElderCohesiveCount;
        public bool Condition5_Met;
        public bool HasActiveEpidemic;
        public bool HasInfectedBarn;
        public bool Condition6_Met;
        public int FarmLevel;
        public int MajorWavesSurvived;
        public bool Condition7_Met;
        public bool AllConditionsMet;
        public int ConsecutiveDaysMet;
    }

    [Serializable]
    public class AlphaLeaderData
    {
        public string PigId;
        public string Name;
        public int DaysAsLeader;
        public int NeoThreshold = 25;
        public int NeoRadiusMeters = 20;
        public bool IsUnderAttack;
    }

    [Serializable]
    public class SuccessionTrainingData
    {
        public string CandidatePigId;
        public int ConsecutiveCareDays;
        public int BonusSuccessionPercentage;
        public int BonusTransmissionPercentage;
    }

    [Serializable]
    public class HerdManager
    {
        public HerdStateTier Tier { get; private set; } = HerdStateTier.KhongCo;
        public int ConsecutiveDaysInState { get; private set; } = 0;
        public AlphaLeaderData Leader { get; private set; } = null;
        public SuccessionTrainingData SuccessionTraining { get; private set; } = new SuccessionTrainingData();
        public int TransmissionStacks { get; private set; } = 0;

        private int daysConditionMetStreak = 0;
        private int daysConditionFailedStreak = 0;
        private readonly Random random = new Random();

        public HerdConditionsReport EvaluateConditions(List<Pig> pigs, Farm farm, int majorWavesSurvived, bool hasActiveEpidemic = false, bool hasInfectedBarn = false)
        {
            var livingPigs = pigs.FindAll(p => p.IsAlive);
            var capacityReport = farm.CalculateCapacityReport(livingPigs.Count, 0);
            int currentSC = farm.HabitatIndex;

            // 1. Số heo gắn kết (Kim Thọ trưởng thành/già = 3)
            int cohesiveCount = 0;
            foreach (var p in livingPigs)
            {
                if (p.Bonding >= 50f)
                {
                    if (p.GeneLine == GeneLineId.KimTho && (p.Stage == PigStage.TruongThanh || p.Stage == PigStage.HeoGia))
                    {
                        cohesiveCount += 3;
                    }
                    else
                    {
                        cohesiveCount += 1;
                    }
                }
            }

            if (currentSC < 50 && currentSC >= 30) cohesiveCount = Math.Min(80, cohesiveCount);
            else if (currentSC < 30) cohesiveCount = 0;

            bool cond1 = cohesiveCount >= 100;
            bool cond2 = currentSC >= 50;

            bool hasKimTho = livingPigs.Exists(p => p.GeneLine == GeneLineId.KimTho);
            int reqMood = hasKimTho ? 55 : 60;
            float totalMood = 0;
            bool anyPanic = false;
            foreach (var p in livingPigs)
            {
                totalMood += p.Mood;
                if (p.MoodState == MoodState.HoangLoan) anyPanic = true;
            }
            float avgMood = livingPigs.Count > 0 ? totalMood / livingPigs.Count : 0f;
            bool cond3 = avgMood >= reqMood && !anyPanic;

            float density = capacityReport.RawDensityRatio;
            bool cond4 = density >= 0.71f && density <= 1.10f;

            int elderCohesive = livingPigs.FindAll(p => p.Stage == PigStage.HeoGia && p.Bonding >= 65f).Count;
            bool cond5 = elderCohesive >= 6;

            bool cond6 = !hasActiveEpidemic && !hasInfectedBarn;
            bool cond7 = farm.FarmLevel >= 6 && majorWavesSurvived >= 4;

            bool allMet = cond1 && cond2 && cond3 && cond4 && cond5 && cond6 && cond7;

            return new HerdConditionsReport
            {
                CohesivePigsCount = cohesiveCount,
                Condition1_Met = cond1,
                CurrentSC = currentSC,
                Condition2_Met = cond2,
                AvgMood = avgMood,
                HasKimTho = hasKimTho,
                RequiredAvgMood = reqMood,
                AnyPanic = anyPanic,
                Condition3_Met = cond3,
                DensityRatio = density,
                Condition4_Met = cond4,
                ElderCohesiveCount = elderCohesive,
                Condition5_Met = cond5,
                HasActiveEpidemic = hasActiveEpidemic,
                HasInfectedBarn = hasInfectedBarn,
                Condition6_Met = cond6,
                FarmLevel = farm.FarmLevel,
                MajorWavesSurvived = majorWavesSurvived,
                Condition7_Met = cond7,
                AllConditionsMet = allMet,
                ConsecutiveDaysMet = daysConditionMetStreak
            };
        }

        public (bool tierChanged, string message) AdvanceDay(List<Pig> pigs, Farm farm, int majorWavesSurvived)
        {
            var report = EvaluateConditions(pigs, farm, majorWavesSurvived);

            if (report.AllConditionsMet)
            {
                daysConditionMetStreak += 1;
                daysConditionFailedStreak = 0;
            }
            else
            {
                daysConditionMetStreak = 0;
                daysConditionFailedStreak += 1;
            }

            bool tierChanged = false;
            string message = null;

            if (Tier == HerdStateTier.KhongCo)
            {
                if (daysConditionMetStreak >= 3)
                {
                    Tier = HerdStateTier.SoKhai;
                    ConsecutiveDaysInState = 1;
                    tierChanged = true;
                    message = "Đạt Trạng Thái Bầy Đàn (Bậc Sơ Khai)!";
                }
            }
            else
            {
                ConsecutiveDaysInState += 1;

                if ((farm.HabitatIndex < 30 && daysConditionFailedStreak >= 1) || daysConditionFailedStreak >= 3)
                {
                    DisbandHerd();
                    return (true, "Bầy Đàn đã tan rã!");
                }

                if (Tier == HerdStateTier.SoKhai)
                {
                    if (Leader != null && Leader.DaysAsLeader >= 5 && farm.HabitatIndex >= 70)
                    {
                        Tier = HerdStateTier.Vung;
                        tierChanged = true;
                        message = "Bầy Đàn đã nâng lên Bậc VỮNG!";
                    }
                }
                else if (Tier == HerdStateTier.Vung)
                {
                    if (ConsecutiveDaysInState >= 10 && farm.HabitatIndex >= 90 && TransmissionStacks >= 1)
                    {
                        Tier = HerdStateTier.HungThinh;
                        tierChanged = true;
                        message = "Bầy Đàn đã nâng lên Bậc HƯNG THỊNH!";
                    }
                }

                ProcessLeaderAndSuccession(pigs, majorWavesSurvived);
            }

            if (Leader != null && !Leader.IsUnderAttack)
            {
                int threshold = Leader.NeoThreshold;
                pigs.ForEach(p => p.ApplyNeoTinhThan(threshold));
            }

            return (tierChanged, message);
        }

        private void ProcessLeaderAndSuccession(List<Pig> pigs, int majorWavesSurvived)
        {
            var livingPigs = pigs.FindAll(p => p.IsAlive);
            if (Leader != null)
            {
                var curLeader = livingPigs.Find(p => p.Id == Leader.PigId);
                if (curLeader == null || !curLeader.IsAlive)
                {
                    int loss = curLeader?.GeneLine == GeneLineId.KimTho ? 40 : 25;
                    livingPigs.ForEach(p => p.Mood = Math.Max(0, p.Mood - loss));
                    Leader = null;
                    return;
                }
                Leader.DaysAsLeader += 1;
                return;
            }

            ElectNewLeader(livingPigs, majorWavesSurvived);
        }

        private void ElectNewLeader(List<Pig> livingPigs, int majorWavesSurvived)
        {
            // 1. Kiểm tra kế vị
            if (!string.IsNullOrEmpty(SuccessionTraining.CandidatePigId))
            {
                var candidate = livingPigs.Find(p => p.Id == SuccessionTraining.CandidatePigId);
                if (candidate != null && candidate.Bonding >= 75f)
                {
                    bool isKimTho = candidate.GeneLine == GeneLineId.KimTho;
                    double rate = (isKimTho ? 0.20 : 0.05) + (SuccessionTraining.BonusSuccessionPercentage / 100.0);
                    if (random.NextDouble() < rate)
                    {
                        PromoteToLeader(candidate, true);
                        return;
                    }
                }
            }

            // 2. Tìm ứng viên sáng lập
            foreach (var pig in livingPigs)
            {
                bool isKimTho = pig.GeneLine == GeneLineId.KimTho;
                float minBonding = isKimTho ? 50f : 65f;
                if (pig.Stage == PigStage.HeoGia && pig.Bonding >= minBonding && pig.Mood >= 60 && pig.AgeDays >= 8)
                {
                    double rate = 0.06 + (0.0015 * Math.Max(0, pig.Bonding - 65f));
                    if (majorWavesSurvived > 0) rate += 0.02;
                    if (isKimTho) rate *= 1.5;
                    rate = Math.Min(0.20, rate);

                    if (random.NextDouble() < rate)
                    {
                        PromoteToLeader(pig, false);
                        break;
                    }
                }
            }
        }

        private void PromoteToLeader(Pig pig, bool isSuccession)
        {
            bool isKimTho = pig.GeneLine == GeneLineId.KimTho;

            if (isSuccession && TransmissionStacks < 3)
            {
                double transRate = (isKimTho ? 0.50 : 0.25) + (SuccessionTraining.BonusTransmissionPercentage / 100.0);
                if (random.NextDouble() < transRate)
                {
                    TransmissionStacks = Math.Min(3, TransmissionStacks + 1);
                    if (isKimTho && TransmissionStacks < 3 && random.NextDouble() < 0.30)
                    {
                        TransmissionStacks = Math.Min(3, TransmissionStacks + 1);
                    }
                }
            }

            int neoBonus = TransmissionStacks * 5;
            Leader = new AlphaLeaderData
            {
                PigId = pig.Id,
                Name = pig.Name,
                DaysAsLeader = 0,
                NeoThreshold = 25 + neoBonus,
                NeoRadiusMeters = 20 + neoBonus,
                IsUnderAttack = false
            };

            SuccessionTraining = new SuccessionTrainingData();
        }

        public void DisbandHerd()
        {
            Tier = HerdStateTier.KhongCo;
            ConsecutiveDaysInState = 0;
            daysConditionMetStreak = 0;
            daysConditionFailedStreak = 0;
        }
    }
}
