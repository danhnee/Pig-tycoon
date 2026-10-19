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
        public string PigId = string.Empty;
        public string Name = string.Empty;
        public int DaysAsLeader;
        public int NeoThreshold = 25;
        public int NeoRadiusMeters = 20;
        public bool IsUnderAttack;
    }

    [Serializable]
    public class SuccessionTrainingData
    {
        public string CandidatePigId = string.Empty;
        public int ConsecutiveCareDays;
        public int BonusSuccessionPercentage;
        public int BonusTransmissionPercentage;
    }

    [Serializable]
    public class HerdManager
    {
        public HerdStateTier Tier { get; private set; } = HerdStateTier.KhongCo;
        public int ConsecutiveDaysInState { get; private set; } = 0;
        public AlphaLeaderData? Leader { get; private set; } = null;
        public SuccessionTrainingData SuccessionTraining { get; private set; } = new SuccessionTrainingData();
        public int TransmissionStacks { get; private set; } = 0;
        public bool KhoaHasNguoiCuaDanUnlocked { get; set; } = false;

        private int daysConditionMetStreak = 0;
        private int daysConditionFailedStreak = 0;
        private readonly Random random = new Random();

        /// <summary>
        /// Tính hào quang trấn an đàn từ các cá thể Heo Già gắn bó (Bonding >= 65):
        /// Mỗi heo già gắn bó giúp giảm +2 điểm hoảng loạn khi có sấm sét/quái tập kích (tối đa +12).
        /// </summary>
        public int CalculateElderCalmAuraBonus(List<Pig> pigs)
        {
            if (pigs == null || pigs.Count == 0) return 0;
            int count = 0;
            for (int i = 0; i < pigs.Count; i++)
            {
                var p = pigs[i];
                if (p != null && p.IsAlive && p.Stage == PigStage.HeoGia && p.Bonding >= 65f)
                {
                    count++;
                }
            }
            return Math.Min(12, count * 2);
        }

        /// <summary>
        /// Phản ứng xã hội tức thì của Bầy Đàn khi 1 cá thể bị bán hoặc chết (GDD 4.6 &amp; 10.8):
        /// - Nếu là Thủ Lĩnh Đàn (Leader): toàn đàn tụt Tinh thần (-40 nếu Kim Thọ, -25 thường; giảm 30% nếu Khoa có 'Người Của Đàn'),
        ///   xóa Thủ lĩnh hiện tại và kiểm tra Kế Vị Khẩn Cấp từ ứng viên đã bồi dưỡng.
        /// - Nếu là heo gắn bó cao (Bonding >= 65): các heo đứng gần trong bán kính 15m bị giảm Tinh thần &amp; Hòa nhập do nhớ đàn.
        /// </summary>
        public void OnPigRemovedFromHerd(
            Pig removedPig,
            List<Pig> remainingPigs,
            bool diedViolentlyOrSick,
            out int moodPenaltyApplied,
            out bool wasAlphaLeader,
            out bool emergencySuccessionPromoted)
        {
            moodPenaltyApplied = 0;
            wasAlphaLeader = false;
            emergencySuccessionPromoted = false;
            if (removedPig == null || remainingPigs == null) return;

            if (Leader != null && Leader.PigId == removedPig.Id)
            {
                wasAlphaLeader = true;
                int baseLoss = removedPig.GeneLine == GeneLineId.KimTho ? 40 : 25;
                if (KhoaHasNguoiCuaDanUnlocked)
                {
                    baseLoss = (int)Math.Round(baseLoss * 0.70f);
                }
                moodPenaltyApplied = baseLoss;

                for (int i = 0; i < remainingPigs.Count; i++)
                {
                    var p = remainingPigs[i];
                    if (p != null && p.IsAlive && p.Id != removedPig.Id)
                    {
                        p.Mood = Math.Max(0, p.Mood - baseLoss);
                    }
                }

                Leader = null;

                // Kiểm tra Kế Vị Khẩn Cấp nếu đã có ứng viên đủ điều kiện
                if (!string.IsNullOrEmpty(SuccessionTraining.CandidatePigId))
                {
                    var candidate = remainingPigs.Find(p => p != null && p.IsAlive && p.Id == SuccessionTraining.CandidatePigId);
                    if (candidate != null && candidate.Bonding >= 75f && SuccessionTraining.BonusSuccessionPercentage >= 15)
                    {
                        PromoteToLeader(candidate, isSuccession: true);
                        emergencySuccessionPromoted = true;
                    }
                }
                return;
            }

            if (removedPig.Bonding >= 65f)
            {
                int socialLoss = diedViolentlyOrSick ? 6 : 3;
                moodPenaltyApplied = socialLoss;
                for (int i = 0; i < remainingPigs.Count; i++)
                {
                    var p = remainingPigs[i];
                    if (p == null || !p.IsAlive || p.Id == removedPig.Id) continue;
                    float dx = p.PositionX - removedPig.PositionX;
                    float dy = p.PositionY - removedPig.PositionY;
                    if (dx * dx + dy * dy <= 225f) // trong bán kính 15m
                    {
                        p.Mood = Math.Max(0, p.Mood - socialLoss);
                    }
                }
            }
        }

        public (bool selected, string message) SelectSuccessionCandidate(Pig candidate)
        {
            if (candidate == null || !candidate.IsAlive)
                return (false, "Ứng viên kế vị không tồn tại hoặc đã chết.");
            if (candidate.Stage != PigStage.TruongThanh && candidate.Stage != PigStage.HeoGia)
                return (false, "Ứng viên kế vị phải ở giai đoạn Trưởng thành hoặc Heo già.");
            if (candidate.Bonding < 75f)
                return (false, "Ứng viên kế vị cần đạt Hòa nhập đàn ≥ 75.");

            if (SuccessionTraining.CandidatePigId != candidate.Id)
            {
                SuccessionTraining = new SuccessionTrainingData
                {
                    CandidatePigId = candidate.Id,
                    ConsecutiveCareDays = 0,
                    BonusSuccessionPercentage = 0,
                    BonusTransmissionPercentage = 0
                };
            }
            return (true, $"Đã chọn [{candidate.Name}] làm ứng viên Bồi Dưỡng Kế Vị Thủ Lĩnh.");
        }

        public void TrainSuccessionCandidateDaily(List<Pig> pigs)
        {
            if (string.IsNullOrEmpty(SuccessionTraining.CandidatePigId)) return;
            var candidate = pigs.Find(p => p.Id == SuccessionTraining.CandidatePigId && p.IsAlive);
            if (candidate == null || candidate.Bonding < 75f || candidate.Mood < 60)
            {
                return;
            }

            SuccessionTraining.ConsecutiveCareDays += 1;
            SuccessionTraining.BonusSuccessionPercentage = Math.Min(35, SuccessionTraining.BonusSuccessionPercentage + 5);
            SuccessionTraining.BonusTransmissionPercentage = Math.Min(25, SuccessionTraining.BonusTransmissionPercentage + 5);
        }

        /// <summary>
        /// GDD 4.6 &amp; 10.8: Bồi Dưỡng Kế Vị chuẩn:
        /// - Mỗi ngày chăm sóc hợp lệ tăng +3% tỷ lệ kế vị và +3% tỷ lệ Truyền Thừa (nếu Khoa đã mở "Người Của Đàn" thì được +1%/ngày kế vị thêm = +4%/ngày), tối đa +30%.
        /// - Nếu bỏ 1 ngày không chăm sóc thì giảm -5% (không âm).
        /// </summary>
        public (int bonusSuccessionPct, int bonusTransmissionPct, int consecutiveDays) TrainSuccessionCandidateCanonicalGdd46(
            List<Pig> pigs,
            bool caredToday = true,
            bool khoaHasNguoiCuaDan = false)
        {
            if (string.IsNullOrEmpty(SuccessionTraining.CandidatePigId))
                return (0, 0, 0);

            var candidate = pigs.Find(p => p.Id == SuccessionTraining.CandidatePigId && p.IsAlive);
            if (candidate == null || candidate.Bonding < 75f || candidate.Mood < 60 || !caredToday)
            {
                SuccessionTraining.ConsecutiveCareDays = 0;
                SuccessionTraining.BonusSuccessionPercentage = Math.Max(0, SuccessionTraining.BonusSuccessionPercentage - 5);
                SuccessionTraining.BonusTransmissionPercentage = Math.Max(0, SuccessionTraining.BonusTransmissionPercentage - 5);
                return (SuccessionTraining.BonusSuccessionPercentage, SuccessionTraining.BonusTransmissionPercentage, 0);
            }

            int dailySuccessionGain = 3 + ((khoaHasNguoiCuaDan || KhoaHasNguoiCuaDanUnlocked) ? 1 : 0);
            SuccessionTraining.ConsecutiveCareDays += 1;
            SuccessionTraining.BonusSuccessionPercentage = Math.Min(30, SuccessionTraining.BonusSuccessionPercentage + dailySuccessionGain);
            SuccessionTraining.BonusTransmissionPercentage = Math.Min(30, SuccessionTraining.BonusTransmissionPercentage + 3);
            return (SuccessionTraining.BonusSuccessionPercentage, SuccessionTraining.BonusTransmissionPercentage, SuccessionTraining.ConsecutiveCareDays);
        }

        public void RestoreState(
            HerdStateTier tier,
            int consecutiveDaysInState,
            int transmissionStacks,
            AlphaLeaderData? leader,
            SuccessionTrainingData? successionTraining)
        {
            Tier = tier;
            ConsecutiveDaysInState = Math.Max(0, consecutiveDaysInState);
            TransmissionStacks = Math.Clamp(transmissionStacks, 0, 3);
            Leader = leader;
            SuccessionTraining = successionTraining ?? new SuccessionTrainingData();
        }

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
            TrainSuccessionCandidateDaily(pigs);
            bool hasEpidemic = pigs.Exists(p => p.IsAlive && !p.IsIsolated && p.ActiveDisease.HasValue && DiseaseBoard.GroupOf(p.ActiveDisease.Value) == DiseaseGroup.A);
            var report = EvaluateConditions(pigs, farm, majorWavesSurvived, hasEpidemic, farm.IsBarnContaminated);

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
            string message = string.Empty;

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
