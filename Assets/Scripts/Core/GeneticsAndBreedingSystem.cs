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

    [Serializable]
    public sealed class ElderPigletEvaluation
    {
        public bool Conceived;
        public float HealthyThresholdK;
        public bool IsHealthy;
        public ElderOffspringAbnormalCategory AbnormalCategory;
        public float StatPenaltyFraction; // 5% - 20%
        public bool IsEliteOffspring;     // 2%
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
        public string Name = "";
        public bool IsGood;
        public bool IsDeviant;
        public string EffectDescription = "";
        public string CountermeasureAfterIdentify = "";
    }

    /// <summary>
    /// Hệ thống vòng đời chi tiết theo GDD 4.1–4.8 & Di truyền 12 dòng gen + 12 tính trạng phụ + 6 tính trạng Hư Thể (GDD 5.1–5.7, P1.04, P2.03, P4.08).
    /// </summary>
    public static class GeneticsAndBreedingSystem
    {
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
                baseKg *= 1.10f; // Háu Ăn: ăn +10%, tăng trọng +12%
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
        /// Quy trình 6 bước sinh "Hậu duệ heo già" (GDD 4.8 / P2.03):
        /// 1. Xác suất thụ thai = 70% × 0.55 = 38.5%.
        /// 2. Tỷ lệ sinh khỏe K ∈ [22%, 32%] nội suy theo Sinh Cảnh (SC) và Tinh thần mẹ.
        /// 3. Phần không khỏe (1 - K) chia thành: 35% Chết yểu, 40% Thể Trạng Yếu, 25% Gen Xấu.
        /// 4. Phạt chỉ số 5–20% trên cá thể sống sót không khỏe hoàn toàn.
        /// 5. 2% cơ hội xuất hiện Hậu Duệ Ưu Tú.
        /// 6. Nếu là Hậu Duệ Ưu Tú: 45% cơ hội Thức tỉnh gen trội (+1 bậc hiếm).
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

            // Bước 1: Thụ thai (70% * 0.55 = 38.5%)
            float conceptionThreshold = BreedingLedger.BaseConception * BreedingLedger.OldConceptionFactor;
            result.Conceived = conceptionRoll < conceptionThreshold;
            if (!result.Conceived)
            {
                return result;
            }

            // Bước 2: Tỷ lệ khỏe mạnh K ∈ [0.22, 0.32]
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
                // Bước 3: Phân bổ (1 - K) -> 35% Chết yểu, 40% Thể Trạng Yếu, 25% Gen Xấu
                result.IsHealthy = false;
                double normalized = (healthyOrAbnormalRoll - k) / Math.Max(0.0001, 1.0 - k);
                if (normalized < 0.35)
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.Stillborn_ChetYeu;
                }
                else if (normalized < 0.75) // 0.35 + 0.40
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.WeakBody_TheTrangYeu;
                }
                else
                {
                    result.AbnormalCategory = ElderOffspringAbnormalCategory.BadGene_GenXau;
                }

                // Bước 4: Phạt chỉ số 5–20%
                float clampedPenaltyRoll = (float)Math.Max(0.0, Math.Min(1.0, penaltyRoll));
                result.StatPenaltyFraction = 0.05f + 0.15f * clampedPenaltyRoll;
            }

            // Bước 5: 2% cơ hội Hậu Duệ Ưu Tú (chỉ áp dụng nếu không Chết yểu)
            if (result.AbnormalCategory != ElderOffspringAbnormalCategory.Stillborn_ChetYeu && eliteRoll < 0.02)
            {
                result.IsEliteOffspring = true;
                result.StatPenaltyFraction = 0f;

                // Bước 6: 45% cơ hội Thức tỉnh gen trội
                if (awakenRoll < 0.45)
                {
                    result.AwakenedDominantGene = true;
                    result.FinalRarity = UpgradeRarityOneTier(baseRarity);
                }
            }

            return result;
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
                BaseGrowthMultiplier = 1.05f, MaxWeightKg = 110f,
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
                BaseGrowthMultiplier = 1.04f, MaxWeightKg = 115f, ExtraLitterBonus = 2,
                PassiveSummary = "Mát sữa (+1–2 heo con/lứa), nhận nuôi heo non mồ côi trong 10m (0% chết yểu)."
            },
            [GeneLineId.ThietBi] = new GeneProfileInfo
            {
                GeneLine = GeneLineId.ThietBi, Name = "Thiết Bì", DefaultRarity = GeneRarity.Hiem,
                BaseGrowthMultiplier = 0.98f, MaxWeightKg = 130f, PhysicalDamageTakenMultiplier = 0.75f,
                PassiveSummary = "Da dày kháng 25% sát thương vật lý và chống hiệu ứng kéo/bắt cóc."
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
                PassiveSummary = "Mỗi cá thể trưởng thành/già tính bằng 3 heo gắn kết, +15% tỷ lệ kế vị Thủ Lĩnh."
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
        /// Danh mục 12 tính trạng phụ (6 Tốt, 6 Xấu + Biện pháp khắc chế sau giám định) và 6 tính trạng Dị Biến Hư Thể (GDD 5.4–5.6).
        /// </summary>
        public static readonly IReadOnlyList<SecondaryTraitDefinition> AllTraitDefinitions = new List<SecondaryTraitDefinition>
        {
            // 6 Tính trạng Tốt
            new SecondaryTraitDefinition { TraitId = "HauAn", Name = "Háu Ăn", IsGood = true, EffectDescription = "Tăng trọng +12%, tiêu thụ cám +10%.", CountermeasureAfterIdentify = "Đặt gần Máng ăn chính để tối ưu tốc độ vỗ béo." },
            new SecondaryTraitDefinition { TraitId = "ThichBun", Name = "Thích Bùn", IsGood = true, EffectDescription = "Trong Nắng Gắt nếu có Hố bùn: Tinh thần +5/ngày.", CountermeasureAfterIdentify = "Ưu tiên thả ở khu có Hố bùn làm mát." },
            new SecondaryTraitDefinition { TraitId = "DanDon", Name = "Dạn Đòn", IsGood = true, EffectDescription = "Giảm 30% tụt Tinh thần khi có tiếng động giao tranh.", CountermeasureAfterIdentify = "Có thể thả ở vòng ngoài gần Chốt Canh Gác." },
            new SecondaryTraitDefinition { TraitId = "DeBao", Name = "Dễ Bảo", IsGood = true, EffectDescription = "Tăng Hòa nhập +3/ngày, hưởng ứng lệnh Lùa nhanh hơn 25%.", CountermeasureAfterIdentify = "Chọn làm hạt nhân Bồi Dưỡng Kế Vị." },
            new SecondaryTraitDefinition { TraitId = "MatTay", Name = "Mát Tay", IsGood = true, EffectDescription = "Tỷ lệ thụ thai +10% khi phối giống.", CountermeasureAfterIdentify = "Ưu tiên giữ làm heo giống F1/F2." },
            new SecondaryTraitDefinition { TraitId = "NhayDem", Name = "Nhạy Đêm", IsGood = true, EffectDescription = "Phát hiện sớm kẻ trộm ban đêm trong bán kính 14m.", CountermeasureAfterIdentify = "Kết hợp cùng Chó Ngao Canh ở cổng phụ." },

            // 6 Tính trạng Xấu (kèm biện pháp khắc chế sau giám định)
            new SecondaryTraitDefinition { TraitId = "KenAn", Name = "Kén Ăn", IsGood = false, EffectDescription = "Tăng trọng -15% nếu chỉ ăn cám thô.", CountermeasureAfterIdentify = "Trộn Thảo Dược / Khoáng chất vào Máng ăn để khôi phục 100% tăng trọng." },
            new SecondaryTraitDefinition { TraitId = "SoDong", Name = "Sợ Động", IsGood = false, EffectDescription = "Dễ hoảng sợ khi có sấm sét hoặc tiếng nổ.", CountermeasureAfterIdentify = "Giữ trong bán kính 20m của Thủ Lĩnh (Neo Tinh Thần) hoặc đặt gần Đèn Pha." },
            new SecondaryTraitDefinition { TraitId = "HayHuc", Name = "Hay Húc", IsGood = false, EffectDescription = "Làm heo xung quanh giảm -2 Tinh thần/ngày nếu mật độ > 100%.", CountermeasureAfterIdentify = "Giữ mật độ chuồng ≤ 100% hoặc tách sang Ô Đất thưa." },
            new SecondaryTraitDefinition { TraitId = "LuoiDi", Name = "Lười Đi", IsGood = false, EffectDescription = "Tốc độ di chuyển -20%, dễ nằm ngoài tầm phục vụ 30m.", CountermeasureAfterIdentify = "Đặt Máng ăn và Bồn nước trong bán kính ≤ 15m quanh vị trí ngủ." },
            new SecondaryTraitDefinition { TraitId = "DeNhiem", Name = "Dễ Nhiễm", IsGood = false, EffectDescription = "Nguy cơ mắc bệnh Nhóm A và B tăng +25%.", CountermeasureAfterIdentify = "Tiêm Vắc-xin định kỳ mỗi 3 ngày và giữ Sinh Cảnh ≥ 70." },
            new SecondaryTraitDefinition { TraitId = "KhoNgu", Name = "Khó Ngủ", IsGood = false, EffectDescription = "Hồi Thoải mái ban đêm giảm -30% nếu thiếu Mái trú.", CountermeasureAfterIdentify = "Đảm bảo dư Mái trú ấm và thay ổ rơm sạch." },

            // 6 Tính trạng Dị Biến (Hư Thể - GDD 5.6)
            new SecondaryTraitDefinition { TraitId = "ThitHuKhong", Name = "Thịt Hư Không", IsGood = true, IsDeviant = true, EffectDescription = "Chợ Đêm và Bí nhân định giá trao đổi ×3.", CountermeasureAfterIdentify = "Giữ đến Trưởng thành để giao dịch tại Chợ Đêm." },
            new SecondaryTraitDefinition { TraitId = "DaPhanQuang", Name = "Da Phản Quang", IsGood = true, IsDeviant = true, EffectDescription = "Phản xạ 20% tia kéo UFO và giảm sát thương bức xạ.", CountermeasureAfterIdentify = "Dùng làm mồi nhử dưới ô bảo vệ của Tháp Phòng Không Skyhook." },
            new SecondaryTraitDefinition { TraitId = "NhipTimCham", Name = "Nhịp Tim Chậm", IsGood = true, IsDeviant = true, EffectDescription = "Tiêu hao thức ăn -25% và kháng độc tố Nhóm D.", CountermeasureAfterIdentify = "Nuôi dài ngày tích lũy trọng lượng cực đại." },
            new SecondaryTraitDefinition { TraitId = "HutSuong", Name = "Hút Sương", IsGood = false, IsDeviant = true, EffectDescription = "Khi thức tỉnh: ÁLSK +Vừa/ngày và gọi Sương Mù.", CountermeasureAfterIdentify = "Phong ấn vĩnh viễn ngay ở giai đoạn Heo non (0–2 ngày tuổi)." },
            new SecondaryTraitDefinition { TraitId = "TiengKeuLech", Name = "Tiếng Kêu Lệch", IsGood = false, IsDeviant = true, EffectDescription = "Khi thức tỉnh: Làm giảm -5 Tinh thần đàn mỗi đêm.", CountermeasureAfterIdentify = "Phong ấn vĩnh viễn ở giai đoạn Heo non hoặc đưa vào Khu Cách Ly." },
            new SecondaryTraitDefinition { TraitId = "BongDoi", Name = "Bóng Đôi", IsGood = false, IsDeviant = true, EffectDescription = "Khi thức tỉnh: Thu hút Bóng Huyền và Chó Sương trong đợt quái đêm.", CountermeasureAfterIdentify = "Phong ấn vĩnh viễn ở giai đoạn Heo non." }
        };

        /// <summary>
        /// Áp dụng biện pháp khắc chế sau khi heo đã được giám định (IsIdentified == true) (GDD 5.5).
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

            var trait = pig.Traits.Find(t => t.Id == traitId);
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
