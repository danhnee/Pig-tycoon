using System;

namespace PigTycoon.Core
{
    public enum LitterLossReason
    {
        None,
        NoMother,
        SowPanic
    }

    public sealed class BreedingSow
    {
        public string Id = "";
        public bool IsFemale = true;
        public bool Alive = true;
        public int AgeDays = 20;
        public PigStage Stage = PigStage.TruongThanh;
        public int Mood = 70;
        public DiseaseId? Disease;
        public GeneLineId Gene = GeneLineId.HongDien;
        public int LittersBorn;
        public int DaysSinceBirth = 99;
        public int GestationDaysLeft;
        public string FatherId = "";
        public GeneLineId FatherGene = GeneLineId.LamKhe;
        public bool InbredPair;
        public bool ParentsAreF1;
    }

    public sealed class BirthOutcome
    {
        public bool Born;
        public LitterLossReason Loss = LitterLossReason.None;
        public int Count;
        public GeneLineId[] Genes = Array.Empty<GeneLineId>();
        public float LatentDiseaseRisk;
    }

    /// <summary>
    /// Phối giống theo GDD 4.4: ngày 12, mang thai 3 ngày, nghỉ 2 ngày, tối đa 5 lứa, thụ thai 70%, heo già còn 55%.
    /// Nái chết hoặc tinh thần dưới 20 thì lứa không sinh ra con. GDD ghi heo non thiếu mẹ chết 12%; bản này chốt mất cả lứa khi nái không còn hoặc đang hoảng loạn.
    /// Trần 8 ca mang thai cùng lúc là trần prototype của người B.
    /// </summary>
    public static class BreedingLedger
    {
        public const int MinAgeDays = 12;
        public const int GestationDays = 3;
        public const int RestDays = 2;
        public const int MaxLitters = 5;
        public const int MaxConcurrentPregnancies = 8;
        public const float BaseConception = 0.70f;
        public const float OldConceptionFactor = 0.55f;

        public static bool Conceives(double roll, PigStage stage)
        {
            float threshold = stage == PigStage.HeoGia ? BaseConception * OldConceptionFactor : BaseConception;
            return roll < threshold;
        }

        public static bool TrySchedule(BreedingSow sow, bool fatherReady, int pregnanciesAlready, double roll)
        {
            if (!sow.IsFemale || !sow.Alive || !fatherReady)
            {
                return false;
            }

            if (sow.AgeDays < MinAgeDays || sow.Stage == PigStage.HeoNon || sow.Stage == PigStage.DangLon)
            {
                return false;
            }

            if (sow.LittersBorn >= MaxLitters || sow.DaysSinceBirth < RestDays || sow.GestationDaysLeft > 0)
            {
                return false;
            }

            if (sow.Disease == DiseaseId.RoiLoanSinhSan || pregnanciesAlready >= MaxConcurrentPregnancies)
            {
                return false;
            }

            if (!Conceives(roll, sow.Stage))
            {
                return false;
            }

            sow.GestationDaysLeft = GestationDays;
            return true;
        }

        public static int LitterSize(int rollFromZeroToFive, bool inbred)
        {
            int size = 6 + rollFromZeroToFive;
            if (size > 11) size = 11;
            if (inbred) size -= 2;
            if (size < 1) size = 1;
            return size;
        }

        public static GeneLineId Inherit(double roll, GeneLineId mother, GeneLineId father, GeneLineId commonFallback)
        {
            if (roll < 0.25) return father;
            if (roll < 0.50) return mother;
            return commonFallback;
        }

        public static BirthOutcome GiveBirth(BreedingSow sow, int litterRoll, double[] geneRolls, GeneLineId commonFallback)
        {
            var outcome = new BirthOutcome();
            sow.GestationDaysLeft = 0;
            sow.DaysSinceBirth = 0;
            if (!sow.Alive)
            {
                outcome.Loss = LitterLossReason.NoMother;
                return outcome;
            }

            if (sow.Mood < 20)
            {
                outcome.Loss = LitterLossReason.SowPanic;
                return outcome;
            }

            int count = LitterSize(litterRoll, sow.InbredPair);
            var genes = new GeneLineId[count];
            for (int i = 0; i < count; i++)
            {
                double roll = geneRolls != null && i < geneRolls.Length ? geneRolls[i] : 0.9;
                genes[i] = Inherit(roll, sow.Gene, sow.FatherGene, commonFallback);
            }

            outcome.Born = true;
            outcome.Count = count;
            outcome.Genes = genes;
            outcome.LatentDiseaseRisk = sow.InbredPair ? 0.08f : 0f;
            if (sow.ParentsAreF1)
            {
                outcome.LatentDiseaseRisk += 0.015f;
            }

            sow.LittersBorn += 1;
            return outcome;
        }
    }
}
