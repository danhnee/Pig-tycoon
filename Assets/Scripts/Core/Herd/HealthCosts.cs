using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// GDD 6.5 "Chi phí điển hình" of the five health verbs, charged by GameEngine.ExecuteHealthTreatment (3 Nov audit:
    /// every verb was free). C-07 picks the values the GDD gives as ranges.
    /// </summary>
    public static class HealthCosts
    {
        public const int IsolationFenceGold = 30;           // "Rào tạm 30G/đoạn": one segment per pig moved in
        public const int LimeGoldPerPlot = 20;              // "Vôi 20G/ô"
        public const float LimeStaminaPerPlot = 10f;        // "8–12 Stamina/ô" → middle (C-07)
        public const float TreatmentStamina = 2f;           // "2 Stamina/con"
        public const int VaccineGoldPerPig = 50;            // "Vắc-xin 50G/con; chỉ có tác dụng với heo chưa nhiễm"

        /// <summary>"40–150G/con" by how hard the disease is (C-07, TẠM): environment C 40, contagious A 60, sticky B 80, gene D 150.</summary>
        public static int TreatmentGold(DiseaseId disease) => DiseaseBoard.GroupOf(disease) switch
        {
            DiseaseGroup.C => 40,
            DiseaseGroup.A => 60,
            DiseaseGroup.B => 80,
            _ => 150,
        };

        /// <summary>Gold and stamina a verb will cost before it runs (0/0 when the verb will refuse anyway).</summary>
        public static (int gold, float stamina) Quote(GameEngine e, HealthTreatmentVerb verb, Pig? target)
        {
            switch (verb)
            {
                case HealthTreatmentVerb.CachLy: return (IsolationFenceGold, 0f);
                case HealthTreatmentVerb.XuLyMoiTruong: return (LimeGoldPerPlot, LimeStaminaPerPlot);
                case HealthTreatmentVerb.DieuTriCaThe:
                    return target?.ActiveDisease is DiseaseId d ? (TreatmentGold(d), TreatmentStamina) : (0, 0f);
                case HealthTreatmentVerb.PhongNguaDan:
                    int healthy = e.Pigs.FindAll(p => p.IsAlive && !p.ActiveDisease.HasValue && p.ImmuneDays <= 0).Count;
                    return (healthy * VaccineGoldPerPig, 0f);
                default: return (0, 0f); // research charges its own 120 G
            }
        }
    }
}
