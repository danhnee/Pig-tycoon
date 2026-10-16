using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum ElderOffspringAbnormalCategory
    {
        None_Healthy,
        Stillborn_ChetYeu,       // 35% của phần không khỏe
        WeakBody_TheTrangYeu,   // 40% của phần không khỏe
        BadGene_GenXau           // 25% của phần không khỏe
    }

    public enum EliteSpecializationKind
    {
        None = 0,
        ToanDien_AllStatsPlus10Pct = 1, // 50% Toàn diện: +10% toàn bộ 5 chỉ số
        DotPha_SingleStatPlus40Pct = 2  // 50% Đột phá: +40% 1 chỉ số ngẫu nhiên
    }

    public enum AppraisalChannel
    {
        ChoNgay = 0,                // 180 Vàng: Hiếm 70%, Quý 40%, Huyền Thoại 0%, Dị Biến 0%
        ChoDemGiamDinhCapCao = 1,   // 420 Vàng / Đổi: Hiếm 90%, Quý 72%, Huyền Thoại 50%, Dị Biến 35%
        PhongNghienCuu = 2,         // 300 Vàng + 1 Mẫu Sinh Học: Hiếm 95%, Quý 85%, Huyền Thoại 65%, Dị Biến 50%
        BiNhanHoSoGiongLoai = 3     // Hồ Sơ Giống Loài: 100% mọi bậc hiếm
    }

    [Serializable]
    public sealed class ElderOffspring5StatPenalties
    {
        public float GrowthRatePenalty;     // 5% - 20%
        public float MaxWeightPenalty;      // 5% - 20%
        public float FeedConversionPenalty; // 5% - 20%
        public float MaxHealthPenalty;      // 5% - 20%
        public float TemperamentPenalty;    // 5% - 20%
    }

    [Serializable]
    public sealed class ElderPigletEvaluation
    {
        public bool Conceived;
        public float HealthyThresholdK;
        public bool IsHealthy;
        public ElderOffspringAbnormalCategory AbnormalCategory;
        public float StatPenaltyFraction; // 5% - 20%
        public ElderOffspring5StatPenalties IndependentPenalties = new ElderOffspring5StatPenalties();
        public bool IsEliteOffspring;     // 2%
        public EliteSpecializationKind EliteKind = EliteSpecializationKind.None;
        public int BreakthroughStatIndex = 0; // 0..4
        public bool AwakenedDominantGene; // 45% khi là Ưu Tú
        public GeneRarity FinalRarity;
    }

    [Serializable]
    public sealed class GeneProfileInfo
    {
        public GeneLineId GeneLine;
        public string Name = "";
        public GeneRarity DefaultRarity;
        public float BaseGrowthMultiplier = 1.0f;
        public float MaxWeightKg = 110f;
        public float FeedConsumptionMultiplier = 1.0f;
        public float MoveSpeedMultiplier = 1.0f;
        public float PhysicalDamageTakenMultiplier = 1.0f;
        public int ExtraLitterBonus = 0;
        public int CohesiveWeight = 1;
        public string PassiveSummary = "";
    }

    [Serializable]
    public sealed class SecondaryTraitDefinition
    {
        public string TraitId = "";
        public string CanonicalGddId = "";
        public string Name = "";
        public string CanonicalGddName = "";
        public bool IsGood;
        public bool IsDeviant;
        public string EffectDescription = "";
        public string CountermeasureAfterIdentify = "";
    }

    /// <summary>
    /// Hệ thống vòng đời chi tiết theo GDD 4.1–4.8 & Di truyền 12 dòng gen + 12 tính trạng phụ + 6 tính trạng Hư Thể + Giám định 4 kênh (GDD 5.1–5.7, P1.04, P2.03, P4.08).
    /// </summary>
    public static class GeneticsAndBreedingSystem
    {
        public const float GddElderConceptionFactor = 0.15f;       // GDD 1.5 & 4.8: Heo già -85% (×0.15 -> 10.5%)
        public const float GddKimThoElderConceptionFactor = 0.85f; // GDD 1.5 & 4.8: Kim Thọ già -15% (×0.85 -> 59.5%)

        /// <summary>
        /// Khẩu phần ăn tiêu chuẩn mỗi ngày theo 4 giai đoạn tuổi (GDD 4.1):
        /// Heo non: 0.5 kg, Đang lớn: 2.5 kg, Trưởng thành: 3.5 kg, Heo già: 4.2 kg.
        /// Dòng gen Thổ Trầm giảm 18% lượng cám tiêu thụ.
        /// </summary>
        public static float GetDailyFeedKilograms(PigStage stage, GeneLineId geneLine = GeneLineId.HongDien, bool hasHauAnTrait = false)
        {
            float baseKg = stage switch
            {
                PigStage.HeoNon => 0.5f,
                PigStage.DangLon => 2.5f,
                PigStage.TruongThanh => 3.5f,
                PigStage.HeoGia => 4.2f,
                _ => 2.5f
            };

            if (geneLine == GeneLineId.ThoTram)
            {
                baseKg *= 0.82f; // -18%
            }

            if (hasHauAnTrait)
            {
                baseKg *= 1.10f; // Ăn Khỏe / Háu Ăn: ăn +10%, tăng trọng +12%
            }

            return baseKg;
        }

        /// <summary>
        /// Tỷ lệ tử vong mỗi ngày của heo non mồ côi mẹ (GDD 4.4 / P2.03):
        /// Mặc định 12%/ngày; giảm còn 0% nếu có nái Phong Mẫu trong bán kính 10m; giảm một nửa (6%) khi Bầy Đàn đạt bậc Vững/Hưng Thịnh.
        /// </summary>
        public static float CalculateOrphanPigletDailyMortalityRate(bool hasPhongMauFosterWithin10m, HerdStateTier herdTier)
        {
            if (hasPhongMauFosterWithin10m)
            {
                return 0.0f;
            }

            float rate = 0.12f;
            if (herdTier == HerdStateTier.Vung || herdTier == HerdStateTier.HungThinh)
            {
                rate *= 0.50f; // 6%
            }
            return rate;
        }

        /// <summary>
        /// Cửa sổ định hình phẩm chất thịt (C / B / A / S) trong giai đoạn Đang lớn (ngày 3–9) khi bước sang Trưởng thành (ngày 10) (GDD 4.2 & 4.7).
        /// </summary>
        public static MeatQuality EvaluateMeatQualityShaping(
            float avgFeedSatisfactionRatio,
            float avgMoodDuringGrowth,
            int avgHabitatIndexDuringGrowth,
            int daysSickDuringGrowth)
        {
            if (avgFeedSatisfactionRatio >= 0.95f &&
                avgMoodDuringGrowth >= 75f &&
                avgHabitatIndexDuringGrowth >= 80 &&
                daysSickDuringGrowth == 0)
            {
                return MeatQuality.S;
            }

            if (avgFeedSatisfactionRatio >= 0.85f &&
                avgMoodDuringGrowth >= 60f &&
                avgHabitatIndexDuringGrowth >= 65 &&
                daysSickDuringGrowth <= 1)
            {
                return MeatQuality.A;
            }

            if (avgFeedSatisfactionRatio >= 0.65f &&
                avgMoodDuringGrowth >= 40f &&
                daysSickDuringGrowth <= 3)
            {
                return MeatQuality.B;
            }

            return MeatQuality.C;
        }

        /// <summary>
        /// Bước 1 GDD 4.8: Tính xác suất thụ thai chính thức theo GDD v6.0 Mục 1.5 & 4.8:
        /// - Cơ bản: 70%
        /// - Có bố/mẹ Heo Già thường: ×0.15 (-85% -> 10.5%)
        /// - Có bố/mẹ Kim Thọ Già: ×0.85 (-15% -> 59.5%)
        /// - Thiết Bì: ×0.88 | Hiếm Muộn: ×0.90 | Đực Phong Mẫu: ×1.08 | Nái Bất an: ×0.90
        /// - Bầy Đàn Hưng Thịnh: +5% | Truyền Thừa: +3%/tầng | Kẹp trong [1%, 95%].
        /// </summary>
        public static float CalculateConceptionRateGdd48(
            bool isElderPair,
            bool isKimThoElder,
            bool hasThietBiParent = false,
            bool hasHiemMuonTrait = false,
            bool fatherIsPhongMau = false,
            bool motherIsBatAn = false,
            bool herdIsHungThinh = false,
            int transmissionStacks = 0)
        {
            float rate = BreedingLedger.BaseConception; // 0.70f
            if (isElderPair)
            {
                rate *= isKimThoElder ? GddKimThoElderConceptionFactor : GddElderConceptionFactor;
            }

            if (hasThietBiParent) rate *= 0.88f;
            if (hasHiemMuonTrait) rate *= 0.90f;
            if (fatherIsPhongMau) rate *= 1.08f;
            if (motherIsBatAn) rate *= 0.90f;
            if (herdIsHungThinh) rate += 0.05f;
            if (transmissionStacks > 0) rate += 0.03f * transmissionStacks;

            return Math.Clamp(rate, 0.01f, 0.95f);
        }

        /// <summary>
        /// Bước 2 GDD 4.8: Quy mô lứa đẻ chuẩn GDD v6.0:
        /// Gốc 6–11 (trung bình 8) + Mắn Đẻ (+1) + Phong Mẫu (+1) - Lam Khê (-1) - Cận huyết (-2), tối thiểu 1.
        /// </summary>
        public static int CalculateLitterSizeGdd48(
            int rollFromZeroToFive,
            bool hasManDeTrait,
            bool motherIsPhongMau,
            bool motherIsLamKhe,
            bool isInbred)
        {
            int size = 6 + Math.Clamp(rollFromZeroToFive, 0, 5);
            if (hasManDeTrait) size += 1;
            if (motherIsPhongMau) size += 1;
            if (motherIsLamKhe) size -= 1;
            if (isInbred) size -= 2;
            return Math.Max(1, size);
        }

        /// <summary>
        /// Quy trình 6 bước sinh "Hậu duệ heo già" (tương thích ngược với các bài test hiện hữu).
        /// </summary>
        public static ElderPigletEvaluation EvaluateElderOffspringSixSteps(
            int habitatIndex,
            int motherMood,
            GeneRarity baseRarity,
            double conceptionRoll,
            double healthyOrAbnormalRoll,
            double penaltyRoll,
            double eliteRoll,
            double awakenRoll)
        {
            var result = new ElderPigletEvaluation
            {
                FinalRarity = baseRarity
            };

            float conceptionThreshold = BreedingLedger.BaseConception * BreedingLedger.OldConceptionFactor;
            result.Conceived = conceptionRoll < conceptionThreshold;
            if (!result.Conceived)
            {
                return result;
            }

            float scFactor = Math.Max(0f, Math.Min(1f, habitatIndex / 100f));
            float moodFactor = Math.Max(0f, Math.Min(1f, motherMood / 100f));
            float k = 0.22f + 0.10f * ((scFactor + moodFactor) * 0.5f);
            result.HealthyThresholdK = k;

            if (healthyOrAbnormalRoll < k)
            {
                result.IsHealthy = true;
                result.AbnormalCategory = ElderOffspringAbnormalCategory.None_Healthy;
                result.StatPenaltyFraction = 0f;
            }
            else
            {
                result.IsHealthy = false;
                double normalized = (healthyOrAbnormalRoll - k) / Math.Max(0.0001, 1.0 - k);
                if (normalized < 0.35)
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.Stillborn_ChetYeu;
                }
                else if (normalized < 0.75)
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.WeakBody_TheTrangYeu;
                }
                else
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.BadGene_GenXau;
                }

                float clampedPenaltyRoll = (float)Math.Max(0.0, Math.Min(1.0, penaltyRoll));
                result.StatPenaltyFraction = 0.05f + 0.15f * clampedPenaltyRoll;
            }

            // Bước 5 (phạt 5 chỉ số độc lập 5%-20% cho mọi hậu duệ không Ưu Tú)
            float basePen = 0.05f + 0.15f * (float)Math.Clamp(penaltyRoll, 0.0, 1.0);
            result.IndependentPenalties = new ElderOffspring5StatPenalties
            {
                GrowthRatePenalty = basePen,
                MaxWeightPenalty = 0.05f + 0.15f * (float)Math.Clamp((penaltyRoll * 1.37) % 1.0, 0.0, 1.0),
                FeedConversionPenalty = 0.05f + 0.15f * (float)Math.Clamp((penaltyRoll * 1.73) % 1.0, 0.0, 1.0),
                MaxHealthPenalty = 0.05f + 0.15f * (float)Math.Clamp((penaltyRoll * 2.11) % 1.0, 0.0, 1.0),
                TemperamentPenalty = 0.05f + 0.15f * (float)Math.Clamp((penaltyRoll * 2.53) % 1.0, 0.0, 1.0)
            };

            if (result.AbnormalCategory != ElderOffspringAbnormalCategory.Stillborn_ChetYeu && eliteRoll < 0.02)
            {
                result.IsEliteOffspring = true;
                result.StatPenaltyFraction = 0f;
                result.IndependentPenalties = new ElderOffspring5StatPenalties();
                result.EliteKind = awakenRoll < 0.225 ? EliteSpecializationKind.ToanDien_AllStatsPlus10Pct : EliteSpecializationKind.DotPha_SingleStatPlus40Pct;

                if (awakenRoll < 0.45)
                {
                    result.AwakenedDominantGene = true;
                    result.FinalRarity = UpgradeRarityOneTier(baseRarity);
                }
            }

            return result;
        }

        /// <summary>
        /// Quy trình 6 bước Phối giống Heo Già đầy đủ theo đúng GDD v6.0 Mục 1.5 & 4.8:
        /// - Bước 1: Thụ thai heo già 10.5% (×0.15) hoặc Kim Thọ già 59.5% (×0.85) + các hệ số.
        /// - Bước 4: Tỷ lệ sinh khỏe K ∈ [22%, 32%]; phần (1 - K) chia 35% Chết yểu / 40% Thể Trạng Yếu (MaxHP 70, ×1.3 nhiễm bệnh) / 25% Gen Xấu (1 tính trạng xấu chắc chắn + 30% tính trạng xấu thứ 2, ×2 bệnh tiềm ẩn).
        /// - Bước 5: Mọi hậu duệ heo già sống sót không phải Ưu Tú đều chịu phạt 5–20% ĐỘC LẬP trên 5 chỉ số.
        /// - Bước 6: 2% Ưu Tú (không chịu phạt Bước 5, chia 50% Toàn Diện +10% cả 5 chỉ số / 50% Đột Phá +40% 1 chỉ số) + 45% Thức tỉnh gen (80% Hiếm / 18% Quý / 2% Huyền Thoại).
        /// </summary>
        public static ElderPigletEvaluation EvaluateElderBreedingFullGdd48(
            bool isKimThoElder,
            int habitatIndex,
            int motherMood,
            GeneRarity baseRarity,
            double conceptionRoll,
            double healthyOrAbnormalRoll,
            double[] fiveIndependentPenaltyRolls,
            double eliteRoll,
            double eliteTypeRoll,
            double awakenRoll,
            double awakenTierRoll = 0.50)
        {
            var result = new ElderPigletEvaluation
            {
                FinalRarity = baseRarity
            };

            float threshold = CalculateConceptionRateGdd48(isElderPair: true, isKimThoElder: isKimThoElder);
            result.Conceived = conceptionRoll < threshold;
            if (!result.Conceived)
            {
                return result;
            }

            float scFactor = Math.Clamp(habitatIndex / 100f, 0f, 1f);
            float moodFactor = Math.Clamp(motherMood / 100f, 0f, 1f);
            float k = 0.22f + 0.10f * ((scFactor + moodFactor) * 0.5f);
            result.HealthyThresholdK = k;

            if (healthyOrAbnormalRoll < k)
            {
                result.IsHealthy = true;
                result.AbnormalCategory = ElderOffspringAbnormalCategory.None_Healthy;
            }
            else
            {
                result.IsHealthy = false;
                double normalized = (healthyOrAbnormalRoll - k) / Math.Max(0.0001, 1.0 - k);
                if (normalized < 0.35)
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.Stillborn_ChetYeu;
                }
                else if (normalized < 0.75)
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.WeakBody_TheTrangYeu;
                }
                else
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.BadGene_GenXau;
                }
            }

            if (result.AbnormalCategory == ElderOffspringAbnormalCategory.Stillborn_ChetYeu)
            {
                return result;
            }

            // Bước 5: Phạt 5-20% độc lập trên 5 chỉ số
            float GetPen(int idx)
            {
                double r = (fiveIndependentPenaltyRolls != null && idx < fiveIndependentPenaltyRolls.Length)
                    ? fiveIndependentPenaltyRolls[idx]
                    : 0.5;
                return 0.05f + 0.15f * (float)Math.Clamp(r, 0.0, 1.0);
            }

            result.IndependentPenalties = new ElderOffspring5StatPenalties
            {
                GrowthRatePenalty = GetPen(0),
                MaxWeightPenalty = GetPen(1),
                FeedConversionPenalty = GetPen(2),
                MaxHealthPenalty = GetPen(3),
                TemperamentPenalty = GetPen(4)
            };
            result.StatPenaltyFraction = result.IndependentPenalties.GrowthRatePenalty;

            // Bước 6: 2% Ưu Tú trong nhóm Khỏe mạnh
            if (result.IsHealthy && eliteRoll < 0.02)
            {
                result.IsEliteOffspring = true;
                result.StatPenaltyFraction = 0f;
                result.IndependentPenalties = new ElderOffspring5StatPenalties(); // Không chịu phạt chỉ số heo già
                result.EliteKind = eliteTypeRoll < 0.50
                    ? EliteSpecializationKind.ToanDien_AllStatsPlus10Pct
                    : EliteSpecializationKind.DotPha_SingleStatPlus40Pct;
                result.BreakthroughStatIndex = (int)Math.Floor(Math.Clamp(eliteTypeRoll * 10.0, 0.0, 4.99));

                if (awakenRoll < 0.45)
                {
                    result.AwakenedDominantGene = true;
                    if (awakenTierRoll < 0.80) result.FinalRarity = GeneRarity.Hiem;
                    else if (awakenTierRoll < 0.98) result.FinalRarity = GeneRarity.Quy;
                    else result.FinalRarity = GeneRarity.HuyenThoai;
                }
            }

            return result;
        }

        /// <summary>
        /// Giám định dòng gen ẩn theo 4 kênh chính thức (GDD 5.4):
        /// - Chợ Ngày (180G): Hiếm 70%, Quý 40%, Huyền Thoại 0%, Dị Biến 0%
        /// - Chợ Đêm Giám Định Cấp Cao (420G): Hiếm 90%, Quý 72%, Huyền Thoại 50%, Dị Biến 35%
        /// - Phòng Nghiên Cứu (300G + 1 Mẫu Sinh Học): Hiếm 95%, Quý 85%, Huyền Thoại 65%, Dị Biến 50%
        /// - Bí Nhân Hồ Sơ Giống Loài: 100% mọi bậc hiếm
        /// Thành công: mở tên dòng gen + tính trạng phụ. Thất bại: khóa cá thể 24h và cộng +5% bảo hiểm (tối đa +15%).
        /// </summary>
        public static (bool identified, float finalChance, int goldCost, string message) AppraisePigGene(
            Pig pig,
            AppraisalChannel channel,
            Farm farm,
            double? deterministicRoll = null)
        {
            if (pig == null || !pig.IsAlive)
                return (false, 0f, 0, "Heo không hợp lệ hoặc đã chết.");
            if (pig.IsIdentified)
                return (true, 1.0f, 0, $"Heo [{pig.Name}] đã được giám định trước đó ({pig.GeneLine}).");
            if (pig.AppraisalLockHoursRemaining > 0 && channel != AppraisalChannel.BiNhanHoSoGiongLoai)
                return (false, 0f, 0, $"Heo [{pig.Name}] đang bị khóa giám định ({pig.AppraisalLockHoursRemaining}h còn lại sau lần thất bại trước).");

            int goldCost = channel switch
            {
                AppraisalChannel.ChoNgay => 180,
                AppraisalChannel.ChoDemGiamDinhCapCao => 420,
                AppraisalChannel.PhongNghienCuu => 300,
                AppraisalChannel.BiNhanHoSoGiongLoai => 0,
                _ => 180
            };

            float baseChance = (channel, pig.Rarity) switch
            {
                (AppraisalChannel.BiNhanHoSoGiongLoai, _) => 1.00f,
                (_, GeneRarity.Thuong) => 1.00f,
                (_, GeneRarity.Kha) => 1.00f,

                (AppraisalChannel.ChoNgay, GeneRarity.Hiem) => 0.70f,
                (AppraisalChannel.ChoNgay, GeneRarity.Quy) => 0.40f,
                (AppraisalChannel.ChoNgay, GeneRarity.HuyenThoai) => 0.00f,
                (AppraisalChannel.ChoNgay, GeneRarity.DiBien) => 0.00f,

                (AppraisalChannel.ChoDemGiamDinhCapCao, GeneRarity.Hiem) => 0.90f,
                (AppraisalChannel.ChoDemGiamDinhCapCao, GeneRarity.Quy) => 0.72f,
                (AppraisalChannel.ChoDemGiamDinhCapCao, GeneRarity.HuyenThoai) => 0.50f,
                (AppraisalChannel.ChoDemGiamDinhCapCao, GeneRarity.DiBien) => 0.35f,

                (AppraisalChannel.PhongNghienCuu, GeneRarity.Hiem) => 0.95f,
                (AppraisalChannel.PhongNghienCuu, GeneRarity.Quy) => 0.85f,
                (AppraisalChannel.PhongNghienCuu, GeneRarity.HuyenThoai) => 0.65f,
                (AppraisalChannel.PhongNghienCuu, GeneRarity.DiBien) => 0.50f,
                _ => 0.50f
            };

            if (baseChance <= 0f)
            {
                return (false, 0f, 0, $"Kênh [{channel}] không đủ khả năng giám định bậc [{pig.Rarity}] (tỉ lệ 0%)!");
            }

            if (goldCost > 0 && farm != null && !farm.SpendGold(goldCost))
            {
                return (false, 0f, goldCost, $"Không đủ {goldCost} Vàng để giám định tại [{channel}]!");
            }

            float pityBonus = Math.Min(0.15f, pig.AppraisalFailureCount * 0.05f);
            float finalChance = Math.Min(1.00f, baseChance + pityBonus);
            double roll = deterministicRoll ?? new Random().NextDouble();

            if (roll < finalChance)
            {
                pig.IsIdentified = true;
                pig.AppraisalLockHoursRemaining = 0;
                return (true, finalChance, goldCost, $"GIÁM ĐỊNH THÀNH CÔNG tại [{channel}]: [{pig.Name}] thuộc dòng [{pig.GeneLine}] ({pig.Rarity})!");
            }

            pig.AppraisalFailureCount = Math.Min(3, pig.AppraisalFailureCount + 1);
            pig.AppraisalLockHoursRemaining = 24;
            return (false, finalChance, goldCost, $"Giám định thất bại tại [{channel}] (tỉ lệ {finalChance * 100f:0}%). Cá thể bị khóa 24h, cộng bảo hiểm tích lũy +{Math.Min(15, pig.AppraisalFailureCount * 5)}%!");
        }

        public static GeneRarity UpgradeRarityOneTier(GeneRarity rarity)
        {
            return rarity switch
            {
                GeneRarity.Thuong => GeneRarity.Kha,
                GeneRarity.Kha => GeneRarity.Hiem,
                GeneRarity.Hiem => GeneRarity.Quy,
                GeneRarity.Quy => GeneRarity.HuyenThoai,
                GeneRarity.HuyenThoai => GeneRarity.HuyenThoai,
                _ => rarity
            };
        }

        private static readonly Dictionary<GeneLineId, GeneProfileInfo> GeneProfiles = new Dictionary<GeneLineId, GeneProfileInfo>
        {
            [GeneLineId.HongDien] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.HongDien, Name = "Hồng Điền", DefaultRarity = GeneRarity.Thuong,
                BaseGrowthMultiplier = 1.10f, MaxWeightKg = 105f,
                PassiveSummary = "Dễ nuôi, tăng trọng ổn định +10%, thích nghi tiêu chuẩn."
            },
            [GeneLineId.LamKhe] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.LamKhe, Name = "Lam Khê", DefaultRarity = GeneRarity.Thuong,
                BaseGrowthMultiplier = 1.05f, MaxWeightKg = 110f, ExtraLitterBonus = -1,
                PassiveSummary = "Nhận làm mát từ Bồn nước trong bán kính 20m, kháng nóng và giữ chuồng sạch."
            },
            [GeneLineId.MocCuoc] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.MocCuoc, Name = "Mộc Cước", DefaultRarity = GeneRarity.Thuong,
                BaseGrowthMultiplier = 1.00f, MaxWeightKg = 108f, MoveSpeedMultiplier = 1.15f,
                PassiveSummary = "Chân khỏe, tốc độ di chuyển và chạy về Nơi Trú +15%."
            },
            [GeneLineId.ThoTram] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.ThoTram, Name = "Thổ Trầm", DefaultRarity = GeneRarity.Kha,
                BaseGrowthMultiplier = 1.02f, MaxWeightKg = 120f, FeedConsumptionMultiplier = 0.82f,
                PassiveSummary = "Tiêu thụ cám -18%, điềm tĩnh giảm 25% nguy cơ hoảng loạn."
            },
            [GeneLineId.XichMao] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.XichMao, Name = "Xích Mao", DefaultRarity = GeneRarity.Kha,
                BaseGrowthMultiplier = 1.08f, MaxWeightKg = 118f,
                PassiveSummary = "Sau 3 ngày thuần dưỡng ở giai đoạn Trưởng thành sẽ húc trả kẻ trộm tiếp cận."
            },
            [GeneLineId.PhongMau] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.PhongMau, Name = "Phong Mẫu", DefaultRarity = GeneRarity.Kha,
                BaseGrowthMultiplier = 1.04f, MaxWeightKg = 115f, ExtraLitterBonus = 1,
                PassiveSummary = "Mát sữa (+1 heo con/lứa, đực +8% thụ thai), nhận nuôi heo non mồ côi trong 10m (0% chết yểu)."
            },
            [GeneLineId.ThietBi] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.ThietBi, Name = "Thiết Bì", DefaultRarity = GeneRarity.Hiem,
                BaseGrowthMultiplier = 0.98f, MaxWeightKg = 130f, PhysicalDamageTakenMultiplier = 0.75f,
                PassiveSummary = "Da dày kháng 25% sát thương vật lý và chống hiệu ứng kéo/bắt cóc (thụ thai ×0.88)."
            },
            [GeneLineId.TinhQuang] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.TinhQuang, Name = "Tinh Quang", DefaultRarity = GeneRarity.Hiem,
                BaseGrowthMultiplier = 1.12f, MaxWeightKg = 112f,
                PassiveSummary = "Phát sáng dịu ban đêm (12m), giảm 30% tụt Tinh thần đàn vào ban đêm."
            },
            [GeneLineId.LoiMach] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.LoiMach, Name = "Lôi Mạch", DefaultRarity = GeneRarity.Quy,
                BaseGrowthMultiplier = 1.22f, MaxWeightKg = 125f,
                PassiveSummary = "Tăng trưởng +22%, hấp thụ điện năng trong Giông Bão (cần Cột Thu Lôi để tránh Lôi Tâm)."
            },
            [GeneLineId.MongNhan] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.MongNhan, Name = "Mộng Nhãn", DefaultRarity = GeneRarity.Quy,
                BaseGrowthMultiplier = 1.06f, MaxWeightKg = 110f,
                PassiveSummary = "Cảnh báo sớm 18m trước quái Sương/Dị Tượng và giảm ÁLSK tích tụ."
            },
            [GeneLineId.KimTho] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.KimTho, Name = "Kim Thọ", DefaultRarity = GeneRarity.HuyenThoai,
                BaseGrowthMultiplier = 1.15f, MaxWeightKg = 95f, CohesiveWeight = 3,
                PassiveSummary = "Mỗi cá thể trưởng thành/già tính bằng 3 heo gắn kết, +15% tỷ lệ kế vị Thủ Lĩnh, heo già thụ thai ×0.85."
            },
            [GeneLineId.HuThe] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.HuThe, Name = "Hư Thể", DefaultRarity = GeneRarity.DiBien,
                BaseGrowthMultiplier = 1.18f, MaxWeightKg = 110f,
                PassiveSummary = "Dị biến Hư Không: giá trị Chợ Đêm ×3, mang tính trạng Dị Biến cần phong ấn lúc Heo non."
            }
        };

        public static GeneProfileInfo GetGeneProfile(GeneLineId geneLine)
        {
            return GeneProfiles.TryGetValue(geneLine, out var info) ? info : GeneProfiles[GeneLineId.HongDien];
        }

        public static IReadOnlyDictionary<GeneLineId, GeneProfileInfo> AllGeneProfiles => GeneProfiles;

        /// <summary>
        /// Danh mục 18 tính trạng (6 Tốt, 6 Xấu + Biện pháp khắc chế sau giám định, 6 Dị Biến Hư Thể) theo GDD 5.3.
        /// Hỗ trợ song song cả mã định danh GDD 5.3 và mã tương thích ngược.
        /// </summary>
        public static readonly IReadOnlyList<SecondaryTraitDefinition> AllTraitDefinitions = new List<SecondaryTraitDefinition>
        {
            // 6 Tính trạng Tốt (GDD 5.3: Trung Thành, Lì Đòn, Kêu Báo, Mắn Đẻ, Ăn Khỏe, Sạch Sẽ)
            new SecondaryTraitDefinition { TraitId = "HauAn", CanonicalGddId = "AnKhoe", Name = "Ăn Khỏe (Háu Ăn)", CanonicalGddName = "Ăn Khỏe", IsGood = true, EffectDescription = "Tăng trọng +8–12%, tiêu thụ cám +10%.", CountermeasureAfterIdentify = "Đặt gần Máng ăn chính để tối ưu tốc độ vỗ béo." },
            new SecondaryTraitDefinition { TraitId = "ThichBun", CanonicalGddId = "SachSe", Name = "Sạch Sẽ (Thích Bùn)", CanonicalGddName = "Sạch Sẽ", IsGood = true, EffectDescription = "Giảm -20% ô nhiễm từ cá thể và +5 Tinh thần khi có nguồn mát.", CountermeasureAfterIdentify = "Ưu tiên thả ở khu trung tâm giữ Sinh Cảnh." },
            new SecondaryTraitDefinition { TraitId = "DanDon", CanonicalGddId = "LiDon", Name = "Lì Đòn (Dạn Đòn)", CanonicalGddName = "Lì Đòn", IsGood = true, EffectDescription = "+15% máu tối đa và giảm 30% tụt Tinh thần khi giao tranh.", CountermeasureAfterIdentify = "Có thể thả ở vòng ngoài gần Chốt Canh Gác." },
            new SecondaryTraitDefinition { TraitId = "DeBao", CanonicalGddId = "TrungThanh", Name = "Trung Thành (Dễ Bảo)", CanonicalGddName = "Trung Thành", IsGood = true, EffectDescription = "Tốc độ tăng Hòa nhập +20% (+3/ngày), không tự phá rào.", CountermeasureAfterIdentify = "Chọn làm hạt nhân Bồi Dưỡng Kế Vị." },
            new SecondaryTraitDefinition { TraitId = "MatTay", CanonicalGddId = "ManDe", Name = "Mắn Đẻ (Mát Tay)", CanonicalGddName = "Mắn Đẻ", IsGood = true, EffectDescription = "+1 heo con mỗi lứa và +10% tỷ lệ thụ thai.", CountermeasureAfterIdentify = "Ưu tiên giữ làm heo giống F1/F2." },
            new SecondaryTraitDefinition { TraitId = "NhayDem", CanonicalGddId = "KeuBao", Name = "Kêu Báo (Nhạy Đêm)", CanonicalGddName = "Kêu Báo", IsGood = true, EffectDescription = "Cảnh báo sớm 3 giây khi quái lại gần trong bán kính 12–14m.", CountermeasureAfterIdentify = "Kết hợp cùng Chó Ngao Canh ở cổng phụ." },

            // 6 Tính trạng Xấu (GDD 5.3: Kén Ăn, Ồn Ào, Cắn Đàn, Tò Mò, Hiếm Muộn, Đoản Thọ)
            new SecondaryTraitDefinition { TraitId = "KenAn", CanonicalGddId = "KenAn", Name = "Kén Ăn", CanonicalGddName = "Kén Ăn", IsGood = false, EffectDescription = "Đổi loại cám làm giảm -20% tăng trọng trong 1 ngày (-15% nếu chỉ ăn cám thô).", CountermeasureAfterIdentify = "Giữ một loại cám cố định hoặc trộn Thảo Dược vào Máng ăn." },
            new SecondaryTraitDefinition { TraitId = "SoDong", CanonicalGddId = "OnAo", Name = "Ồn Ào (Sợ Động)", CanonicalGddName = "Ồn Ào", IsGood = false, EffectDescription = "Khi hoảng làm heo trong 8m giảm thêm -3 Tinh thần.", CountermeasureAfterIdentify = "Trang bị Còi Chăn Heo hoặc đặt gần Thủ Lĩnh (Neo Tinh Thần)." },
            new SecondaryTraitDefinition { TraitId = "HayHuc", CanonicalGddId = "CanDan", Name = "Cắn Đàn (Hay Húc)", CanonicalGddName = "Cắn Đàn", IsGood = false, EffectDescription = "Khi mật độ > 100%, mỗi ngày cắn 1 heo làm giảm -5 Tinh thần.", CountermeasureAfterIdentify = "Giữ mật độ chuồng ≤ 90% hoặc tách sang Ô Đất thưa." },
            new SecondaryTraitDefinition { TraitId = "LuoiDi", CanonicalGddId = "ToMo", Name = "Tò Mò (Hay Đi Lạc)", CanonicalGddName = "Tò Mò", IsGood = false, EffectDescription = "Hay tiến gần hàng rào ngoài hoặc lệch khỏi tầm phục vụ 30m.", CountermeasureAfterIdentify = "Dùng rào thép khu rìa hoặc đặt Máng ăn trong bán kính ≤ 15m." },
            new SecondaryTraitDefinition { TraitId = "DeNhiem", CanonicalGddId = "HiemMuon", Name = "Hiếm Muộn (Dễ Nhiễm)", CanonicalGddName = "Hiếm Muộn", IsGood = false, EffectDescription = "Xác suất thụ thai ×0.90 và nguy cơ nhiễm bệnh +25%.", CountermeasureAfterIdentify = "Thú y chẩn đoán + dùng thuốc điều hòa nội tiết & tiêm Vắc-xin." },
            new SecondaryTraitDefinition { TraitId = "KhoNgu", CanonicalGddId = "DoanTho", Name = "Đoản Thọ (Khó Ngủ)", CanonicalGddName = "Đoản Thọ", IsGood = false, EffectDescription = "Tuổi thọ tối đa -3 ngày và hồi Thoải mái ban đêm -30% nếu thiếu mái.", CountermeasureAfterIdentify = "Theo dõi tuổi và bán ở giai đoạn Trưởng thành trước khi sang già." },

            // 6 Tính trạng Dị Biến (Hư Thể - GDD 5.3: Thịt Hư Không, Vô Ảnh, Cộng Sinh, Hút Sương, Tự Phân Rã, Hư Nhiễm)
            new SecondaryTraitDefinition { TraitId = "ThitHuKhong", CanonicalGddId = "ThitHuKhong", Name = "Thịt Hư Không", CanonicalGddName = "Thịt Hư Không", IsGood = true, IsDeviant = true, EffectDescription = "Chợ Đêm và Bí nhân có đơn trao đổi riêng, giá trị ×3.", CountermeasureAfterIdentify = "Giữ đến Trưởng thành để giao dịch tại Chợ Đêm." },
            new SecondaryTraitDefinition { TraitId = "DaPhanQuang", CanonicalGddId = "VoAnh", Name = "Vô Ảnh (Da Phản Quang)", CanonicalGddName = "Vô Ảnh", IsGood = true, IsDeviant = true, EffectDescription = "Miễn khóa mục tiêu từ UFO Trinh Sát và phản xạ 20% tia kéo.", CountermeasureAfterIdentify = "Dùng làm mồi nhử dưới ô bảo vệ của Tháp Phòng Không Skyhook." },
            new SecondaryTraitDefinition { TraitId = "NhipTimCham", CanonicalGddId = "CongSinh", Name = "Cộng Sinh (Nhịp Tim Chậm)", CanonicalGddName = "Cộng Sinh", IsGood = true, IsDeviant = true, EffectDescription = "Heo trong 5m tăng +5% tăng trọng, +10% hồi Tinh thần và giảm -25% hao cám.", CountermeasureAfterIdentify = "Nuôi dài ngày ở trung tâm đàn." },
            new SecondaryTraitDefinition { TraitId = "HutSuong", CanonicalGddId = "HutSuong", Name = "Hút Sương", CanonicalGddName = "Hút Sương", IsGood = false, IsDeviant = true, EffectDescription = "Khi thức tỉnh: ÁLSK +Vừa/ngày và thiên về thời tiết Sương Mù.", CountermeasureAfterIdentify = "Phong ấn vĩnh viễn khi nuôi lên Trưởng thành mà không để thức tỉnh." },
            new SecondaryTraitDefinition { TraitId = "TiengKeuLech", CanonicalGddId = "TuPhanRa", Name = "Tự Phân Rã (Tiếng Kêu Lệch)", CanonicalGddName = "Tự Phân Rã", IsGood = false, IsDeviant = true, EffectDescription = "Khi thức tỉnh: 15% tự chết mỗi đêm và làm giảm -5 Tinh thần đàn.", CountermeasureAfterIdentify = "Phong ấn vĩnh viễn hoặc đưa vào Khu Cách Ly / bán ngay." },
            new SecondaryTraitDefinition { TraitId = "BongDoi", CanonicalGddId = "HuNhiem", Name = "Hư Nhiễm (Bóng Đôi)", CanonicalGddName = "Hư Nhiễm", IsGood = false, IsDeviant = true, EffectDescription = "Khi thức tỉnh: Lây biến dạng trong 4m nếu chuồng bẩn và thu hút Bóng Huyền.", CountermeasureAfterIdentify = "Cách ly bắt buộc, khử trùng chuồng mỗi ngày." }
        };

        /// <summary>
        /// Áp dụng biện pháp khắc chế sau khi heo đã được giám định (IsIdentified == true) (GDD 5.3 & 5.4).
        /// </summary>
        public static (bool countermeasureApplied, string message) ApplyBadTraitCountermeasure(Pig pig, string traitId)
        {
            if (pig == null || !pig.IsAlive)
            {
                return (false, "Heo không hợp lệ.");
            }

            if (!pig.IsIdentified)
            {
                return (false, "Phải Giám Định heo trước khi áp dụng biện pháp khắc chế tính trạng xấu!");
            }

            var trait = pig.Traits.Find(t =>
                string.Equals(t.Id, traitId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Name, traitId, StringComparison.OrdinalIgnoreCase));
            if (trait == null)
            {
                return (false, "Heo không mang tính trạng này.");
            }

            if (trait.IsDeviant && trait.IsAwakened)
            {
                return (false, "Tính trạng Dị Biến đã thức tỉnh không thể phong ấn!");
            }

            trait.IsSealed = true;
            pig.Mood = Math.Min(100, pig.Mood + 5);
            return (true, $"Đã áp dụng biện pháp khắc chế tính trạng [{trait.Name}] sau giám định!");
        }
    }
}

