using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Phần vòng đời đàn của GameEngine (GDD chương 3–7): phối giống & sinh F1/F2, tính chất phụ, bệnh tiềm ẩn, lây 6 giờ,
    /// Say Nắng, Sương Mù/Mộng Nhãn, áp lực mật độ, kiểm dịch khi bán. Tách file để GameEngine.cs chỉ đổi vài dòng gọi.
    /// </summary>
    public partial class GameEngine
    {
        // Trạng thái tạm trong phiên (không lưu): ngày người mua từ chối heo chưa kiểm dịch, giờ nắng chưa hạ nhiệt, chuỗi ngày sương mù.
        private readonly Dictionary<string, int> unquarantinedRefusalDay = new Dictionary<string, int>();
        private readonly Dictionary<string, float> uncooledHeatHours = new Dictionary<string, float>();
        /// <summary>GDD 6.4: a pig collapsed from Say Nắng dies after this many more uncooled hours.</summary>
        public const int HeatCollapseDeathHours = 1;
        /// <summary>Hours a pig has been heading for heat collapse (UI: "quá nhiệt" warning from 1 h).</summary>
        public float UncooledHeatHours(string pigId) => uncooledHeatHours.TryGetValue(pigId, out var h) ? h : 0f;
        private int fogDaysInRow;

        /// <summary>Số heo đang sống (dùng cho UI/khởi tạo đàn).</summary>
        public int LivingPigCount => Pigs.FindAll(p => p.IsAlive).Count;

        /// <summary>Tung tính chất phụ (GDD 5.3) và bệnh tiềm ẩn (GDD 5.2) cho heo mới vào trại — sinh ra hoặc đàn khởi đầu.</summary>
        public void PrepareNewPig(Pig pig, bool badGene = false)
        {
            if (pig == null) return;
            HerdLifecycleRules.RollSecondaryTraits(pig, random, badGene);
            HerdLifecycleRules.RollLatentDisease(pig, random);
        }

        // ==================================================================
        // GDD 4.4 / 4.8 — Phối giống
        // ==================================================================

        private static bool IsBreedingAdult(Pig p) =>
            p != null && p.IsAlive && p.AgeDays >= BreedingLedger.MinAgeDays &&
            (p.Stage == PigStage.TruongThanh || p.Stage == PigStage.HeoGia);

        /// <summary>Danh sách bạn phối hợp lệ cho một heo (khác giới, đủ tuổi). Dùng cho thẻ heo.</summary>
        public List<Pig> ListBreedingPartners(string pigId)
        {
            var result = new List<Pig>();
            var pig = Pigs.Find(p => p.Id == pigId);
            if (!IsBreedingAdult(pig)) return result;
            string wanted = pig!.Gender == "Cai" ? "Duc" : "Cai";
            foreach (var other in Pigs)
            {
                if (other != pig && other.Gender == wanted && IsBreedingAdult(other)) result.Add(other);
            }
            return result;
        }

        private HashSet<string> LineageWithinTwoGenerations(Pig pig)
        {
            var set = new HashSet<string> { pig.Id };
            void AddParents(string id, int depth)
            {
                if (string.IsNullOrEmpty(id) || depth > 2) return;
                set.Add(id);
                var p = Pigs.Find(x => x.Id == id);
                if (p == null) return;
                AddParents(p.MotherId, depth + 1);
                AddParents(p.FatherId, depth + 1);
            }
            AddParents(pig.MotherId, 1);
            AddParents(pig.FatherId, 1);
            return set;
        }

        /// <summary>Cận huyết (GDD 4.4): hai con chung huyết thống trong 2 đời (bố/mẹ–con, ông bà–cháu, chung bố/mẹ hoặc ông bà).</summary>
        public bool IsInbredPair(Pig mother, Pig father)
        {
            var a = LineageWithinTwoGenerations(mother);
            var b = LineageWithinTwoGenerations(father);
            foreach (var id in a) if (b.Contains(id)) return true;
            return false;
        }

        /// <summary>Kiểm tra cặp phối và trả về tỉ lệ thụ thai GDD 4.8 bước 1 (không tung).</summary>
        public (bool ok, float chance, string message) PreviewPigBreeding(string motherPigId, string fatherPigId)
        {
            var mother = Pigs.Find(p => p.Id == motherPigId && p.IsAlive);
            var father = Pigs.Find(p => p.Id == fatherPigId && p.IsAlive);
            if (mother == null || father == null)
                return (false, 0f, "Không tìm thấy heo nái hoặc heo đực hợp lệ.");
            if (mother.Gender != "Cai" || father.Gender != "Duc")
                return (false, 0f, "Phải chọn đúng 1 heo Cái và 1 heo Đực.");
            if (!IsBreedingAdult(mother) || !IsBreedingAdult(father))
                return (false, 0f, $"Chỉ heo trưởng thành từ ngày {BreedingLedger.MinAgeDays} (hoặc heo già) mới phối được.");
            if (mother.GestationDaysRemaining > 0)
                return (false, 0f, $"Nái [{mother.Name}] đang mang thai ({mother.GestationDaysRemaining} ngày còn lại).");
            if (mother.LittersBorn >= BreedingLedger.MaxLitters)
                return (false, 0f, $"Nái [{mother.Name}] đã đẻ đủ {BreedingLedger.MaxLitters} lứa.");
            if (mother.DaysSinceLastBirth < BreedingLedger.RestDays)
                return (false, 0f, $"Nái [{mother.Name}] cần nghỉ {BreedingLedger.RestDays} ngày sau đẻ.");
            if (mother.LastMatingDay == Clock.CurrentDay)
                return (false, 0f, $"Nái [{mother.Name}] đã được phối hôm nay; mai thử lại.");
            if ((mother.GeneLine == GeneLineId.KimTho || father.GeneLine == GeneLineId.KimTho) && Farm.HabitatIndex < 60)
                return (false, 0f, "Kim Thọ ngừng sinh sản khi Sinh Cảnh dưới 60.");
            int activePregnancies = Pigs.FindAll(p => p.IsAlive && p.GestationDaysRemaining > 0).Count;
            if (activePregnancies >= BreedingLedger.MaxConcurrentPregnancies)
                return (false, 0f, $"Đã đạt trần {BreedingLedger.MaxConcurrentPregnancies} ca mang thai cùng lúc.");

            float chance = GeneticsAndBreedingSystem.CalculatePairConceptionRate(mother, father, Herd.Tier, Herd.TransmissionStacks);
            return (true, chance, $"Tỉ lệ thụ thai {chance * 100f:0.#}%");
        }

        private (bool scheduled, string message) SchedulePigBreedingGdd(string motherPigId, string fatherPigId, double? deterministicConceptionRoll)
        {
            var (ok, chance, msg) = PreviewPigBreeding(motherPigId, fatherPigId);
            if (!ok) return (false, msg);
            var mother = Pigs.Find(p => p.Id == motherPigId)!;
            var father = Pigs.Find(p => p.Id == fatherPigId)!;

            mother.LastMatingDay = Clock.CurrentDay;
            double roll = deterministicConceptionRoll ?? random.NextDouble();
            // Rối Loạn Sinh Sản (GDD 6.4): không triệu chứng, nái chỉ "phối hụt liên tiếp" cho tới khi thú y chẩn đoán
            bool reproductiveDisorder = mother.ActiveDisease == DiseaseId.RoiLoanSinhSan || father.ActiveDisease == DiseaseId.RoiLoanSinhSan;
            if (reproductiveDisorder || roll >= chance)
            {
                return (false, $"[{mother.Name}] × [{father.Name}] không đậu thai lần này (tỉ lệ {chance * 100f:0.#}%).");
            }

            bool inbred = IsInbredPair(mother, father);
            mother.GestationDaysRemaining = BreedingLedger.GestationDays;
            mother.PendingFatherId = father.Id;
            mother.PendingFatherGene = father.GeneLine;
            mother.PendingFatherStage = father.Stage;
            mother.PendingInbred = inbred;

            bool elder = mother.Stage == PigStage.HeoGia || father.Stage == PigStage.HeoGia;
            int gen = GeneticsAndBreedingSystem.OffspringGeneration(mother, father);
            string tag = elder ? " (có heo già)" : (inbred ? " (cận huyết)" : (gen == 2 ? " (F2)" : (gen == 1 ? " (F1)" : "")));
            string text = $"Đã phối [{mother.Name}] × [{father.Name}]{tag}: đậu thai! Dự kiến đẻ sau {BreedingLedger.GestationDays} ngày.";
            OnLogEmitted?.Invoke(text);
            return (true, text);
        }

        private void ProcessDailyPregnanciesGdd()
        {
            var newPiglets = new List<Pig>();

            for (int i = 0; i < Pigs.Count; i++)
            {
                var mother = Pigs[i];
                if (!mother.IsAlive || mother.Gender != "Cai" || mother.GestationDaysRemaining <= 0)
                    continue;

                mother.GestationDaysRemaining -= 1;
                if (mother.GestationDaysRemaining > 0)
                    continue;

                mother.DaysSinceLastBirth = 0;
                if (mother.Mood < 20)
                {
                    OnLogEmitted?.Invoke($"Nái [{mother.Name}] bị Hoảng Loạn (< 20 Tinh thần) nên mất lứa sinh!");
                    continue;
                }

                var father = Pigs.Find(p => p.Id == mother.PendingFatherId);
                if (father == null)
                {
                    // Đực đã rời trại: giữ dòng & tuổi lúc phối
                    var fatherRarity = GeneticsAndBreedingSystem.GetGeneProfile(mother.PendingFatherGene).DefaultRarity;
                    father = Pig.CreateDefault(mother.PendingFatherId, "Đực đã rời trại", mother.PendingFatherGene, fatherRarity, "Duc", 20, 90f);
                    father.Stage = mother.PendingFatherStage;
                }

                int litter = GeneticsAndBreedingSystem.CalculateLitterSizeGdd48(
                    random.Next(0, 6),
                    hasManDeTrait: HerdLifecycleRules.HasActiveTrait(mother, "MatTay", "ManDe"),
                    motherIsPhongMau: mother.GeneLine == GeneLineId.PhongMau,
                    motherIsLamKhe: mother.GeneLine == GeneLineId.LamKhe,
                    isInbred: mother.PendingInbred);

                bool isElderBirth = mother.Stage == PigStage.HeoGia || mother.PendingFatherStage == PigStage.HeoGia;
                bool isKimThoElder = (mother.Stage == PigStage.HeoGia && mother.GeneLine == GeneLineId.KimTho) ||
                                     (mother.PendingFatherStage == PigStage.HeoGia && mother.PendingFatherGene == GeneLineId.KimTho);
                double litterK = 0.22 + 0.10 * random.NextDouble(); // GDD 4.8 bước 4: gieo K một lần cho lứa
                int generation = GeneticsAndBreedingSystem.OffspringGeneration(mother, father);
                bool isF2 = generation == 2;
                int bornAlive = 0, stillborn = 0;

                for (int k = 0; k < litter; k++)
                {
                    var line = GeneticsAndBreedingSystem.RollOffspringLineGdd44(
                        mother, father, isF2, random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble());
                    GeneLineId childLine = line.geneLine;
                    GeneRarity childRarity = line.rarity;

                    ElderPigletEvaluation? elderEval = null;
                    if (isElderBirth)
                    {
                        double[] pens = { random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble() };
                        elderEval = GeneticsAndBreedingSystem.EvaluateElderBreedingFullGdd48(
                            isKimThoElder: isKimThoElder,
                            habitatIndex: Farm.HabitatIndex,
                            motherMood: mother.Mood,
                            baseRarity: childRarity,
                            conceptionRoll: 0.0, // đã đậu thai lúc phối
                            healthyOrAbnormalRoll: random.NextDouble(),
                            fiveIndependentPenaltyRolls: pens,
                            eliteRoll: random.NextDouble(),
                            eliteTypeRoll: random.NextDouble(),
                            awakenRoll: random.NextDouble(),
                            awakenTierRoll: random.NextDouble(),
                            litterHealthyK: litterK);
                        if (elderEval.AwakenedDominantGene)
                        {
                            // Thức tỉnh gen trội: tung lại dòng trong bậc Hiếm/Quý/Huyền Thoại
                            childRarity = elderEval.FinalRarity;
                            var lines = GeneticsAndBreedingSystem.LinesOfRarity(childRarity);
                            childLine = lines[random.Next(lines.Length)];
                        }
                    }

                    string childId = $"pig_{Clock.CurrentDay}_{mother.Id}_{k + 1}_{newPiglets.Count + 1}";
                    string childGender = (k % 2 == 0) ? "Duc" : "Cai";
                    string lineName = GeneticsAndBreedingSystem.GetGeneProfile(childLine).Name;
                    var child = Pig.CreateDefault(childId, $"Heo Con {lineName} #{Pigs.Count + newPiglets.Count + 1}",
                        childLine, childRarity, childGender, initialAge: 0, initialWeight: 1.5f);
                    child.MotherId = mother.Id;
                    child.FatherId = mother.PendingFatherId;
                    child.Generation = generation;
                    child.IsInbred = mother.PendingInbred;
                    child.PositionX = mother.PositionX + (k % 3 - 1) * 1.2f;
                    child.PositionY = mother.PositionY + (k / 3 - 1) * 1.2f;
                    // GDD 5.4: dòng đã định danh trong ván thì heo con hiện tên ngay
                    if (!child.IsIdentified && Pigs.Exists(p => p.GeneLine == childLine && p.IsIdentified)) child.IsIdentified = true;

                    bool badGene = false;
                    if (elderEval != null)
                    {
                        if (elderEval.AbnormalCategory == ElderOffspringAbnormalCategory.Stillborn_ChetYeu && !elderEval.IsEliteOffspring)
                        {
                            child.Name = $"[Chết Yểu] {child.Name}";
                            child.Die();
                            newPiglets.Add(child);
                            stillborn++;
                            continue;
                        }
                        ApplyElderOffspringStats(child, elderEval);
                        badGene = elderEval.AbnormalCategory == ElderOffspringAbnormalCategory.BadGene_GenXau && !elderEval.IsEliteOffspring;
                    }
                    else if (generation == 1 && !child.IsInbred)
                    {
                        child.GrowthRateMultiplier *= 1.08f; // ưu thế lai F1 (giữ nguyên luật cũ)
                    }
                    if (child.IsInbred) child.GrowthRateMultiplier *= 0.90f; // giữ nguyên luật cũ

                    PrepareNewPig(child, badGene);
                    newPiglets.Add(child);
                    bornAlive++;
                }

                mother.LittersBorn += 1;
                OnLogEmitted?.Invoke($"Nái [{mother.Name}] đã đẻ {bornAlive} heo con sống ({stillborn} chết yểu) — lứa {mother.LittersBorn}/{BreedingLedger.MaxLitters}.");
            }

            if (newPiglets.Count == 0) return;
            Pigs.AddRange(newPiglets);
            foreach (var born in newPiglets)
            {
                if (born.IsAlive) EventBus.Publish(new PigBorn(born.Id, born.GeneLine, born.Rarity, Clock.CurrentDay));
                else EventBus.Publish(new PigDied(born.Id, "Chết yểu sau sinh (Hậu duệ heo già - GDD 4.8)", Clock.CurrentDay));
            }
        }

        /// <summary>GDD 4.4 / 4.8 bước 4–5: Thể Trạng Yếu, Gen Xấu, phạt 5 chỉ số, Ưu Tú Toàn Diện / Đột Phá.</summary>
        private static void ApplyElderOffspringStats(Pig child, ElderPigletEvaluation eval)
        {
            if (eval.IsEliteOffspring)
            {
                child.Name = $"[Ưu Tú] {child.Name}";
                child.EliteSpecializationType = eval.EliteKind == EliteSpecializationKind.ToanDien_AllStatsPlus10Pct ? "ToanDien" : "DotPha";
                if (eval.EliteKind == EliteSpecializationKind.ToanDien_AllStatsPlus10Pct)
                {
                    child.GrowthRateMultiplier *= 1.10f;
                    child.MaxWeightKg *= 1.10f;
                    child.FeedConversionMultiplier /= 1.10f;
                    child.Temperament = Math.Min(100, (int)Math.Round(child.Temperament * 1.10f));
                }
                else
                {
                    switch (eval.BreakthroughStatIndex)
                    {
                        case 0: child.GrowthRateMultiplier *= 1.40f; break;
                        case 1: child.MaxWeightKg *= 1.40f; break;
                        case 2: child.FeedConversionMultiplier /= 1.40f; break;
                        case 3: break; // Sức khỏe đã ở trần 100
                        default: child.Temperament = Math.Min(100, (int)Math.Round(child.Temperament * 1.40f)); break;
                    }
                }
                return;
            }

            var pen = eval.IndependentPenalties;
            child.GrowthRateMultiplier *= 1f - pen.GrowthRatePenalty;
            child.MaxWeightKg *= 1f - pen.MaxWeightPenalty;
            child.FeedConversionMultiplier *= 1f + pen.FeedConversionPenalty;
            child.Temperament = Math.Max(0, (int)Math.Round(child.Temperament * (1f - pen.TemperamentPenalty)));
            int healthCap = eval.AbnormalCategory == ElderOffspringAbnormalCategory.WeakBody_TheTrangYeu ? 70 : 100;
            child.MaxHealth = Math.Max(1, (int)Math.Round(healthCap * (1f - pen.MaxHealthPenalty)));
            child.Health = child.MaxHealth;
            if (eval.AbnormalCategory == ElderOffspringAbnormalCategory.WeakBody_TheTrangYeu) child.InfectionRiskMultiplier = 1.3f;
            if (eval.AbnormalCategory == ElderOffspringAbnormalCategory.BadGene_GenXau) child.LatentDiseaseRiskMultiplier = 2f;
        }

        // ==================================================================
        // GDD 3.3 / 4.3 / 5.2 / 6.4 / 7.2 — theo giờ
        // ==================================================================

        private void TickHerdHealthHour(int hour)
        {
            var living = Pigs.FindAll(p => p.IsAlive);

            // Sương Mù: Tinh thần −1/giờ; Mộng Nhãn −3/giờ (GDD 7.2, 5.2)
            if (CurrentWeather == WeatherType.SuongMu)
            {
                foreach (var p in living)
                {
                    if (p.IsIsolated) continue;
                    HerdLifecycleRules.ApplyAgitation(p, p.GeneLine == GeneLineId.MongNhan ? 3 : 1, living, Clock.CurrentDay);
                }
            }

            // Say Nắng (GDD 6.4): không có nguồn mát 2 giờ game → gục (mắc Say Nắng). Thiết Bì Say Nắng ×1,5.
            if (CurrentWeather == WeatherType.NangGat)
            {
                foreach (var p in living)
                {
                    if (p.IsIsolated) continue;
                    bool cooled = (!DeepMechanics.PowerGrid.IsBreakerTripped && GridZones.IsWithinCoolingRadius(p.PositionX, p.PositionY, p.GeneLine))
                                  || GridZones.IsUnderTempShade(p.PositionX, p.PositionY);
                    if (cooled)
                    {
                        uncooledHeatHours.Remove(p.Id);
                        // GDD 6.4: a collapsed pig brought to mud/water/shade (or under a Mái Che Tạm / mist pump) recovers
                        if (p.ActiveDisease == DiseaseId.SayNang) { p.ActiveDisease = null; p.HeatCollapsedHours = 0; Notify($"[{p.Name}] đã hạ nhiệt, hết Say Nắng."); }
                        continue;
                    }
                    if (p.ActiveDisease == DiseaseId.SayNang)
                    {
                        // GDD 6.4: "gục thêm 1 giờ → chết"
                        p.HeatCollapsedHours += 1;
                        if (p.HeatCollapsedHours >= HeatCollapseDeathHours)
                        {
                            p.Die();
                            EventBus.Publish(new PigDied(p.Id, "Say Nắng", Clock.CurrentDay));
                            Notify($"[{p.Name}] chết vì Say Nắng: gục quá 1 giờ không được hạ nhiệt.");
                        }
                        continue;
                    }
                    uncooledHeatHours.TryGetValue(p.Id, out float heat);
                    heat += p.GeneLine == GeneLineId.ThietBi ? 1.5f : 1f;
                    uncooledHeatHours[p.Id] = heat;
                    if (heat >= 2f && !p.ActiveDisease.HasValue && p.ImmuneDays <= 0)
                    {
                        p.ActiveDisease = DiseaseId.SayNang;
                        p.HeatCollapsedHours = 0;
                        OnLogEmitted?.Invoke($"[{p.Name}] gục vì Say Nắng! Còn 1 giờ: dội Xô Nước, dựng Mái Che Tạm hoặc lùa tới bùn/bồn.");
                    }
                }
            }
            else if (uncooledHeatHours.Count > 0)
            {
                uncooledHeatHours.Clear();
            }

            // GDD 6.5: sau ca đầu, cứ 6 giờ kiểm tra lây một lần (chỉ nhóm A/B)
            if (hour % 6 == 0) RunSixHourSpreadCheck(living);
        }

        /// <summary>Lây tiếp xúc 6 giờ/lần (GDD 6.5). Trả về số ca mới.</summary>
        public int RunSixHourSpreadCheck(List<Pig>? living = null)
        {
            living ??= Pigs.FindAll(p => p.IsAlive);
            var sources = living.FindAll(p => !p.IsIsolated && p.ActiveDisease.HasValue &&
                                              HerdLifecycleRules.IsContagious(p.ActiveDisease.Value) &&
                                              p.GeneLine != GeneLineId.ThietBi); // Thiết Bì không làm cầu nối lây
            if (sources.Count == 0) return 0;

            int corpses = Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            var cap = Farm.CalculateCapacityReport(living.Count, corpses);
            bool hasLeader = Herd.Leader != null;
            float r2 = HerdLifecycleRules.ContactRadiusMeters * HerdLifecycleRules.ContactRadiusMeters;
            var infected = new List<(Pig pig, DiseaseId disease)>();

            foreach (var target in living)
            {
                if (target.IsIsolated || target.ActiveDisease.HasValue || target.ImmuneDays > 0) continue;
                foreach (var src in sources)
                {
                    var disease = src.ActiveDisease!.Value;
                    if (disease == DiseaseId.GheKySinh && target.GeneLine == GeneLineId.ThietBi) continue; // miễn Ghẻ
                    float dx = src.PositionX - target.PositionX, dy = src.PositionY - target.PositionY;
                    if (dx * dx + dy * dy > r2) continue;
                    float p = HerdLifecycleRules.SpreadChance(target, disease, cap.DiseaseDensityMultiplier, Farm.HabitatIndex,
                        CurrentWeather, hasLeader, Farm.LocalCrowdingContagionMultiplier);
                    if (random.NextDouble() < p) { infected.Add((target, disease)); break; }
                }
            }

            foreach (var (pig, disease) in infected)
            {
                pig.ActiveDisease = disease;
                OnLogEmitted?.Invoke($"[{pig.Name}] bị lây {disease} từ heo bệnh gần đó.");
            }
            return infected.Count;
        }

        // ==================================================================
        // Theo ngày (06:00)
        // ==================================================================

        private void ApplyDailyHerdPressure(float density)
        {
            var living = Pigs.FindAll(p => p.IsAlive);

            // GDD 3.3: Tinh thần −1 / −2 / −4 mỗi ngày khi quá tải 101–120% / 121–150% / >150%
            int densityLoss = HerdLifecycleRules.DensityDailyMoodLoss(density);
            if (densityLoss > 0)
            {
                foreach (var p in living) if (!p.IsIsolated) HerdLifecycleRules.ApplyAgitation(p, densityLoss, living, Clock.CurrentDay);
            }

            foreach (var p in living)
            {
                // Bệnh tiềm ẩn bộc phát khi gặp nguồn phát (GDD 5.2 cột Tiềm ẩn + 6.4)
                if (!p.ActiveDisease.HasValue && p.LatentDisease.HasValue && HerdLifecycleRules.LatentTriggered(p, CurrentWeather))
                {
                    p.ActiveDisease = p.LatentDisease;
                    p.LatentDisease = null; // Lôi Tâm warns only when mood < 30 (LoiTamSeizure)
                }

                // Hư Mạch: ÁLSK +Thấp/ngày mỗi con (GDD 6.4)
                if (p.ActiveDisease == DiseaseId.HuMach) Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 1);

                // Mưa Bức Xạ khi Đang lớn: +1,5% đột biến lên một bậc, Dị Biến 0,05% (GDD 4.4, 4.8, 7.2)
                if (CurrentWeather == WeatherType.MuaBucXa && p.Stage == PigStage.DangLon && p.GeneLine != GeneLineId.HuThe)
                {
                    double roll = random.NextDouble();
                    if (roll < 0.0005)
                    {
                        p.GeneLine = GeneLineId.HuThe;
                        p.Rarity = GeneRarity.DiBien;
                        p.IsIdentified = false;
                        p.SecondaryTraitsRolled = false;
                        HerdLifecycleRules.RollSecondaryTraits(p, random);
                    }
                    else if (roll < 0.015 && p.Rarity < GeneRarity.HuyenThoai)
                    {
                        p.Rarity = GeneticsAndBreedingSystem.UpgradeRarityOneTier(p.Rarity);
                        var lines = GeneticsAndBreedingSystem.LinesOfRarity(p.Rarity);
                        p.GeneLine = lines[random.Next(lines.Length)];
                        p.IsIdentified = p.Rarity < GeneRarity.Hiem || Pigs.Exists(o => o != p && o.GeneLine == p.GeneLine && o.IsIdentified);
                    }
                }
            }
        }

        /// <summary>Sương Mù ngày thứ 2 trở đi: ÁLSK +Cao (GDD 3.4); Mộng Nhãn: mỗi con +Thấp, trong Bầy Đàn giảm ÁLSK sương mù 30% (GDD 5.2).</summary>
        private void ApplyFogEventPressure()
        {
            if (CurrentWeather != WeatherType.SuongMu) { fogDaysInRow = 0; return; }
            fogDaysInRow += 1;
            var mongNhan = Pigs.FindAll(p => p.IsAlive && p.GeneLine == GeneLineId.MongNhan);
            float fog = fogDaysInRow >= 2 ? 6f : 0f;
            if (fog > 0f && mongNhan.Count > 0 && Herd.Tier != HerdStateTier.KhongCo) fog *= 0.70f;
            int add = (int)Math.Round(fog) + mongNhan.Count;
            if (add > 0) Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + add);
        }

        // ==================================================================
        // GDD 4.5 — kiểm dịch trước khi bán
        // ==================================================================

        /// <summary>Trả về lỗi nếu chợ ngày không nhận con heo này; null = được bán.</summary>
        private string? CheckDayMarketHealthGate(Pig pig, bool isQuarantined, double? deterministicSaleRoll)
        {
            if (isQuarantined && pig.ActiveDisease.HasValue)
                return $"Kiểm dịch phát hiện [{pig.Name}] mắc {pig.ActiveDisease.Value}: không được bán cho tới khi chữa khỏi.";
            if (!isQuarantined && HerdLifecycleRules.IsKnownSick(pig))
                return $"[{pig.Name}] đang bệnh ({pig.ActiveDisease!.Value}): phải chữa trước khi bán.";
            if (isQuarantined) return null;

            if (unquarantinedRefusalDay.TryGetValue(pig.Id, out int day) && day == Clock.CurrentDay)
                return $"Người mua đã từ chối [{pig.Name}] hôm nay vì chưa kiểm dịch. Kiểm dịch ({Economy.DayMarket.QuarantineFeePerPig}G) hoặc quay lại ngày mai.";
            float rate = HerdLifecycleRules.UnquarantinedSaleSuccessRate(pig);
            double roll = deterministicSaleRoll ?? random.NextDouble();
            if (roll < rate) return null;
            unquarantinedRefusalDay[pig.Id] = Clock.CurrentDay;
            return $"Người mua nghi ngại [{pig.Name}] chưa kiểm dịch nên không mua (tỉ lệ thành công {rate * 100f:0}%).";
        }
    }
}
