using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameEngine
    {
        public GameClock Clock { get; private set; }
        public Farm Farm { get; private set; }
        public HerdManager Herd { get; private set; }
        public EconomyManager Economy { get; private set; }
        public DefenseManager Defense { get; private set; }
        public CharacterData Character { get; private set; }
        public List<Pig> Pigs { get; private set; }
        public HerdFlowField FlowField { get; private set; }
        public FarmGridAndZones GridZones { get; private set; }
        public PetNpcAndOccultManager PetsAndNpcs { get; private set; }
        public StoryJournalSystem Story { get; private set; }
        public DeepMechanicsSystem DeepMechanics { get; private set; }

        public WeatherType CurrentWeather { get; set; } = WeatherType.QuangDang;
        public int MajorWavesSurvived { get; set; } = 0;
        public int MinorWavesDefeated { get; set; } = 0;
        public int TotalPigsSold { get; set; } = 0;
        public int TotalGoldEarned { get; set; } = 0;

        public event Action<string>? OnLogEmitted;

        private readonly Random random = new Random();

        public GameEngine()
        {
            Clock = new GameClock();
            Farm = new Farm();
            Herd = new HerdManager();
            Economy = new EconomyManager();
            Defense = new DefenseManager();
            Character = new CharacterData(PlayerId.An);
            Pigs = new List<Pig>();
            FlowField = new HerdFlowField();
            GridZones = new FarmGridAndZones();
            PetsAndNpcs = new PetNpcAndOccultManager();
            Story = new StoryJournalSystem();
            DeepMechanics = new DeepMechanicsSystem();
        }

        public void Tick(int minutes = 1)
        {
            int totalHoursBefore = Clock.CurrentDay * 24 + Clock.CurrentHour;
            var tickResult = Clock.TickMinutes(minutes);
            int totalHoursAfter = Clock.CurrentDay * 24 + Clock.CurrentHour;
            int elapsedHours = Math.Max(0, totalHoursAfter - totalHoursBefore);

            for (int h = 0; h < elapsedHours; h++)
            {
                OnHourlyTick();
            }

            if (Clock.GetTimeOfDay() == TimeOfDay.Toi && Clock.CurrentMinute == 0)
            {
                if (Economy.RollMysticMerchant(Clock.CurrentHour, Farm.EventPressureIndex))
                {
                    OnLogEmitted?.Invoke("Bí Nhân nửa người nửa quỷ đã xuất hiện sát ranh giới trại!");
                }
            }

            if (tickResult.NewDayStarted)
            {
                OnDailyReset();
            }
        }

        /// <summary>
        /// Mô phỏng theo từng giờ game (GDD 3.7, 4.6, 5.2):
        /// - Tầm phục vụ 30m (ngoài 30m: đánh dấu giảm 10% tăng trọng và -1 Tinh thần/giờ).
        /// - Nguồn mát 25m / Lam Khê 20m trong thời tiết Nắng Gắt.
        /// - Chen Chúc cục bộ (> 16 heo trong 1 Ô Đất 10x10m: lây bệnh Nhóm A ×1.3, lan hoảng +10%, -1 Tinh thần/giờ).
        /// - Tiến độ Khu Xử Lý xác (Thiêu Hủy 6h / Tái Chế Sinh Học 18h thu hồi cám).
        /// </summary>
        private void OnHourlyTick()
        {
            var plotCounts = new Dictionary<(int px, int py), List<Pig>>();
            int maxPigsInSinglePlot = 0;

            for (int i = 0; i < Pigs.Count; i++)
            {
                var p = Pigs[i];
                if (!p.IsAlive || p.IsIsolated) continue;

                // 1. Tầm phục vụ 30m
                var (withinService, _, hourlyMoodDelta) = GridZones.EvaluateServiceCoverage(p.PositionX, p.PositionY);
                if (!withinService)
                {
                    p.OutsideServiceHoursToday += 1;
                    p.Mood = Math.Max(0, p.Mood + hourlyMoodDelta);
                }

                // 2. Nguồn mát trong Nắng Gắt
                if (CurrentWeather == WeatherType.NangGat)
                {
                    bool cooled = GridZones.IsWithinCoolingRadius(p.PositionX, p.PositionY, p.GeneLine);
                    if (!cooled)
                    {
                        p.Comfort = Math.Max(0, p.Comfort - 4);
                        p.Mood = Math.Max(0, p.Mood - 1);
                    }
                    else if (p.Traits.Exists(t => t.Id == "ThichBun" && !t.IsSealed))
                    {
                        p.Mood = Math.Min(100, p.Mood + 1);
                    }
                }

                // 3. Gom nhóm theo Ô Đất 10x10m để kiểm tra Chen Chúc cục bộ
                int plotX = (int)Math.Floor(p.PositionX / FarmGridAndZones.LandPlotMeters);
                int plotY = (int)Math.Floor(p.PositionY / FarmGridAndZones.LandPlotMeters);
                var key = (plotX, plotY);
                if (!plotCounts.TryGetValue(key, out var list))
                {
                    list = new List<Pig>();
                    plotCounts[key] = list;
                }
                list.Add(p);
                if (list.Count > maxPigsInSinglePlot)
                {
                    maxPigsInSinglePlot = list.Count;
                }
            }

            var (isCrowded, contagionMult, panicBonus, crowdMoodPenalty) =
                FarmGridAndZones.EvaluateLocalCrowding(maxPigsInSinglePlot, 1.0f, 0);

            Farm.LocalCrowdingActive = isCrowded;
            Farm.LocalCrowdingContagionMultiplier = contagionMult;
            Farm.LocalCrowdingPanicBonus = panicBonus;

            if (isCrowded)
            {
                foreach (var kv in plotCounts)
                {
                    if (kv.Value.Count > 16)
                    {
                        for (int i = 0; i < kv.Value.Count; i++)
                        {
                            kv.Value[i].Mood = Math.Max(0, kv.Value[i].Mood + crowdMoodPenalty);
                        }
                    }
                }
            }

            // 4. Tiến trình Khu Xử Lý xác (Thiêu Hủy / Tái Chế Sinh Học)
            int recoveredPortions = GridZones.TickCarcassProcessingHours(1);
            if (recoveredPortions > 0)
            {
                float recoveredKg = recoveredPortions * 3.5f;
                Farm.FeedStockKg += recoveredKg;
                OnLogEmitted?.Invoke($"Khu Xử Lý hoàn tất Tái Chế Sinh Học: thu hồi +{recoveredPortions} khẩu phần cám (+{recoveredKg:0.0} kg)!");
            }

            // Xóa xác đã hoàn tất xử lý khỏi danh sách Pigs
            for (int i = GridZones.ActiveCarcassJobs.Count - 1; i >= 0; i--)
            {
                var job = GridZones.ActiveCarcassJobs[i];
                if (job.Completed && !string.IsNullOrEmpty(job.PigId))
                {
                    Pigs.RemoveAll(p => !p.IsAlive && p.Id == job.PigId);
                }
            }
        }

        private void OnDailyReset()
        {
            OnLogEmitted?.Invoke($"=== BẮT ĐẦU NGÀY {Clock.CurrentDay} (Chu kỳ {Clock.CycleNumber}) ===");

            // 1. Ca làm việc hàng ngày của NPC & Thú cưng
            var (curedByVet, scFromWorker, _) = PetsAndNpcs.ExecuteDailyNpcShiftOnPigs(Pigs, Farm);
            if (curedByVet > 0 || scFromWorker > 0)
            {
                OnLogEmitted?.Invoke($"Ca trực NPC: Bác Sĩ Thú Y chữa {curedByVet} heo, Nông Công dọn chuồng +{scFromWorker} SC.");
            }

            var livingPigs = Pigs.FindAll(p => p.IsAlive);
            int corpsesOutside = Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            var capReport = Farm.CalculateCapacityReport(livingPigs.Count, corpsesOutside);

            bool isInHerd = Herd.Tier != HerdStateTier.KhongCo;
            float densityPenalty = capReport.RawDensityRatio > 1.5f ? 0.15f : (capReport.RawDensityRatio > 1.2f ? 0.08f : 0f);

            // 2. Cho đàn heo ăn theo khẩu phần 4 giai đoạn tuổi (0.5 / 2.5 / 3.5 / 4.2 kg)
            foreach (var p in livingPigs)
            {
                bool hasHauAn = p.Traits.Exists(t => t.Id == "HauAn" && !t.IsSealed);
                float neededKg = GeneticsAndBreedingSystem.GetDailyFeedKilograms(p.Stage, p.GeneLine, hasHauAn);
                if (p.Traits.Exists(t => t.Id == "NhipTimCham"))
                {
                    neededKg *= 0.75f;
                }

                if (Farm.FeedStockKg >= neededKg)
                {
                    Farm.FeedStockKg -= neededKg;
                    p.FedToday = true;
                    p.Hunger = 0;
                }
                else
                {
                    p.FedToday = false;
                    p.Hunger = Math.Min(100, p.Hunger + 35);
                }
            }

            // 3. Tiến tuổi, định hình phẩm chất thịt (Đang lớn -> Trưởng thành), thuần dưỡng Xích Mao & tăng trọng
            foreach (var p in Pigs)
            {
                p.AdvanceDay(Farm.HabitatIndex, isInHerd, densityPenalty);
            }

            // 4. Kiểm tra tỷ lệ tử vong heo non mồ côi mẹ (GDD 4.4 / P2.03)
            ProcessOrphanPigletMortality();

            // 5. Xử lý heo nái mang thai & sinh sản F1/F2 + 6 bước Hậu duệ heo già
            ProcessDailyPregnancies();

            // 6. Xử lý dịch bệnh, Chuồng Nhiễm & tính trạng Hư Thể thức tỉnh
            livingPigs = Pigs.FindAll(p => p.IsAlive);
            corpsesOutside = Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            capReport = Farm.CalculateCapacityReport(livingPigs.Count, corpsesOutside);
            int activeDiseaseCount = ProcessDailyDiseaseAndDeviantEffects(livingPigs, capReport, corpsesOutside);

            // 7. Kinh tế bảo trì, Sinh Cảnh (SC), Áp lực Sự kiện (ÁLSK) & Bầy Đàn
            Farm.SpendGold(capReport.DailyEmptyMaintenanceCost);

            bool leaderSafe = Herd.Leader == null || !Herd.Leader.IsUnderAttack;
            Farm.UpdateDailyHabitat(1, corpsesOutside, activeDiseaseCount, capReport.RawDensityRatio, true, leaderSafe);

            int lotPenalty = GridZones.GetProcessingLotDailyHabitatPenalty();
            if (lotPenalty < 0)
            {
                Farm.HabitatIndex = Math.Max(0, Farm.HabitatIndex + lotPenalty);
            }

            int elderPigs = livingPigs.FindAll(p => p.Stage == PigStage.HeoGia).Count;
            Farm.UpdateDailyEventPressure(elderPigs, livingPigs.Count, capReport.RawDensityRatio, corpsesOutside);

            var herdRes = Herd.AdvanceDay(Pigs, Farm, MajorWavesSurvived);
            if (herdRes.tierChanged && !string.IsNullOrEmpty(herdRes.message))
            {
                OnLogEmitted?.Invoke(herdRes.message);
            }

            if (Herd.Tier >= HerdStateTier.SoKhai && !Story.HerdEmptySpotObserved)
            {
                var qRes = Story.CompleteQuestStep("Q11");
                if (qRes.completed)
                {
                    OnLogEmitted?.Invoke("[CỐT TRUYỆN · CHỖ TRỐNG GIỮA ĐÀN] Đàn heo đạt Bầy Đàn Sơ Khai và tự động chừa trống một vòng tròn đường kính 1,2m ở góc Tây Bắc (trùng vị trí ảnh năm 2018)!");
                }
            }

            DeepMechanics.EvaluateMidGameGateAndLocks(Farm, Herd, MajorWavesSurvived);

            Economy.DayMarket.CurrentPigsSoldToday = 0;
            Economy.GenerateNightMarket(Farm.FarmLevel);

            Character.Stamina.DailyLoad = 0;
            Character.Stamina.CurrentStamina = Character.GetEffectiveMaxStamina();
            Character.CurrentGuard = Character.MaxGuard;
            Character.IsFainted = false;
            Character.AnBatKhuatTriggeredThisWave = false;
        }

        private void ProcessOrphanPigletMortality()
        {
            for (int i = 0; i < Pigs.Count; i++)
            {
                var piglet = Pigs[i];
                if (!piglet.IsAlive || piglet.Stage != PigStage.HeoNon || string.IsNullOrEmpty(piglet.MotherId))
                    continue;

                bool motherAlive = Pigs.Exists(m => m.Id == piglet.MotherId && m.IsAlive);
                if (motherAlive) continue;

                bool hasPhongMauNearby = Pigs.Exists(m =>
                    m.IsAlive &&
                    m.Gender == "Cai" &&
                    m.GeneLine == GeneLineId.PhongMau &&
                    (m.Stage == PigStage.TruongThanh || m.Stage == PigStage.HeoGia) &&
                    ((m.PositionX - piglet.PositionX) * (m.PositionX - piglet.PositionX) +
                     (m.PositionY - piglet.PositionY) * (m.PositionY - piglet.PositionY)) <= 100f);

                float mortalityRate = GeneticsAndBreedingSystem.CalculateOrphanPigletDailyMortalityRate(hasPhongMauNearby, Herd.Tier);
                if (mortalityRate > 0f && random.NextDouble() < mortalityRate)
                {
                    piglet.Die();
                    OnLogEmitted?.Invoke($"Heo non mồ côi [{piglet.Name}] đã tử vong do thiếu mẹ và không có nái Phong Mẫu nuôi hộ.");
                }
            }
        }

        private void ProcessDailyPregnancies()
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

                int baseLitter = BreedingLedger.LitterSize(random.Next(0, 6), mother.PendingInbred);
                if (mother.GeneLine == GeneLineId.PhongMau)
                {
                    baseLitter += 2;
                }

                bool isElderBirth = mother.Stage == PigStage.HeoGia || mother.PendingFatherStage == PigStage.HeoGia;
                int bornAliveCount = 0;

                for (int k = 0; k < baseLitter; k++)
                {
                    GeneLineId childGene = BreedingLedger.Inherit(
                        random.NextDouble(),
                        mother.GeneLine,
                        mother.PendingFatherGene,
                        GeneLineId.HongDien);

                    var profile = GeneticsAndBreedingSystem.GetGeneProfile(childGene);
                    GeneRarity childRarity = profile.DefaultRarity;

                    ElderPigletEvaluation? elderEval = null;
                    if (isElderBirth)
                    {
                        elderEval = GeneticsAndBreedingSystem.EvaluateElderOffspringSixSteps(
                            Farm.HabitatIndex,
                            mother.Mood,
                            childRarity,
                            conceptionRoll: 0.10, // Đã thụ thai ở bước phối
                            healthyOrAbnormalRoll: random.NextDouble(),
                            penaltyRoll: random.NextDouble(),
                            eliteRoll: random.NextDouble(),
                            awakenRoll: random.NextDouble());

                        if (elderEval.AbnormalCategory == ElderOffspringAbnormalCategory.Stillborn_ChetYeu && !elderEval.IsEliteOffspring)
                        {
                            continue;
                        }
                        childRarity = elderEval.FinalRarity;
                    }

                    string childId = $"pig_{Clock.CurrentDay}_{mother.Id}_{k + 1}_{newPiglets.Count + 1}";
                    string childGender = (k % 2 == 0) ? "Duc" : "Cai";
                    var child = Pig.CreateDefault(
                        childId,
                        $"Heo Con {childGene} #{Pigs.Count + newPiglets.Count + 1}",
                        childGene,
                        childRarity,
                        childGender,
                        initialAge: 0,
                        initialWeight: 1.5f);

                    child.MotherId = mother.Id;
                    child.FatherId = mother.PendingFatherId;
                    child.Generation = Math.Min(2, mother.Generation + 1);
                    child.IsInbred = mother.PendingInbred;
                    child.PositionX = mother.PositionX + (k % 3 - 1) * 1.2f;
                    child.PositionY = mother.PositionY + (k / 3 - 1) * 1.2f;

                    if (child.Generation == 1 && !child.IsInbred && mother.GeneLine != mother.PendingFatherGene)
                    {
                        // Ưu thế lai F1: +8% tốc độ tăng trưởng
                        child.GrowthRateMultiplier *= 1.08f;
                    }
                    else if (child.IsInbred)
                    {
                        // Phạt cận huyết: -10% tốc độ tăng trưởng
                        child.GrowthRateMultiplier *= 0.90f;
                    }

                    if (elderEval != null)
                    {
                        if (elderEval.IsEliteOffspring)
                        {
                            child.GrowthRateMultiplier *= 1.12f;
                            child.Name = $"[Ưu Tú] {child.Name}";
                        }
                        else if (elderEval.AbnormalCategory == ElderOffspringAbnormalCategory.WeakBody_TheTrangYeu)
                        {
                            float factor = 1.0f - elderEval.StatPenaltyFraction;
                            child.MaxWeightKg *= factor;
                            child.Health = Math.Max(30, (int)Math.Round(100 * factor));
                        }
                        else if (elderEval.AbnormalCategory == ElderOffspringAbnormalCategory.BadGene_GenXau)
                        {
                            child.Traits.Add(new PigTrait
                            {
                                Id = "DeNhiem",
                                Name = "Dễ Nhiễm",
                                IsPositive = false,
                                IsDeviant = false,
                                Description = "Nguy cơ mắc bệnh Nhóm A và B tăng +25%."
                            });
                        }
                    }

                    newPiglets.Add(child);
                    bornAliveCount++;
                }

                mother.LittersBorn += 1;
                OnLogEmitted?.Invoke($"Nái [{mother.Name}] đã sinh {bornAliveCount} heo con khỏe mạnh (Lứa {mother.LittersBorn}/{BreedingLedger.MaxLitters})!");
            }

            if (newPiglets.Count > 0)
            {
                Pigs.AddRange(newPiglets);
            }
        }

        private int ProcessDailyDiseaseAndDeviantEffects(List<Pig> livingPigs, FarmCapacityReport capReport, int corpsesOutside)
        {
            Farm.IsBarnContaminated = DiseaseBoard.IsContaminatedPen(
                capReport.RawDensityRatio,
                Farm.HabitatIndex,
                corpsesOutside > 0 || Farm.CorpseLotCount > 0);

            // Kiểm tra tính trạng Hư Thể đã thức tỉnh trên các heo không bị cách ly
            for (int i = 0; i < livingPigs.Count; i++)
            {
                var p = livingPigs[i];
                if (p.IsIsolated || p.GeneLine != GeneLineId.HuThe) continue;

                for (int t = 0; t < p.Traits.Count; t++)
                {
                    var tr = p.Traits[t];
                    if (!tr.IsDeviant || tr.IsPositive || !tr.IsAwakened || tr.IsSealed) continue;

                    if (tr.Id == "HutSuong")
                    {
                        Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 6);
                        CurrentWeather = WeatherType.SuongMu;
                    }
                    else if (tr.Id == "TiengKeuLech")
                    {
                        livingPigs.ForEach(other =>
                        {
                            if (!other.IsIsolated) other.Mood = Math.Max(0, other.Mood - 5);
                        });
                    }
                    else if (tr.Id == "BongDoi")
                    {
                        Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 8);
                    }
                }
            }

            // Đếm số ca bệnh đang hoạt động và kiểm tra bùng phát bệnh mới
            int activeDiseases = livingPigs.FindAll(p => p.ActiveDisease.HasValue).Count;
            float corpseFactor = 1.0f + corpsesOutside * 0.25f;
            float specialFactor = Farm.LocalCrowdingContagionMultiplier * (Farm.IsBarnContaminated ? 1.35f : 1.0f);
            float outbreakChance = DiseaseBoard.OutbreakChance(
                Farm.HabitatIndex,
                capReport.DiseaseDensityMultiplier,
                1.0f,
                corpseFactor,
                specialFactor);

            // Chỉ tự động lây nhiễm khi trại ô nhiễm / quá tải / có xác ngoài hoặc đã có ca nhiễm không cách ly
            bool hasUnisolatedSick = livingPigs.Exists(p => !p.IsIsolated && p.ActiveDisease.HasValue);
            if (Farm.HabitatIndex < 50 || capReport.RawDensityRatio > 1.20f || corpsesOutside > 0 || hasUnisolatedSick)
            {
                for (int i = 0; i < livingPigs.Count; i++)
                {
                    var p = livingPigs[i];
                    if (p.IsIsolated || p.ActiveDisease.HasValue || p.ImmuneDays > 0) continue;

                    float individualChance = outbreakChance;
                    if (p.Traits.Exists(t => t.Id == "DeNhiem" && !t.IsSealed))
                    {
                        individualChance *= 1.25f;
                    }

                    if (random.NextDouble() < individualChance)
                    {
                        p.ActiveDisease = ChooseWeatherAppropriateDisease();
                        if (p.ActiveDisease == DiseaseId.LoiTam) p.SeizurePending = true;
                        activeDiseases++;
                    }
                }
            }

            return activeDiseases;
        }

        private DiseaseId ChooseWeatherAppropriateDisease()
        {
            return CurrentWeather switch
            {
                WeatherType.Mua or WeatherType.RetDam => DiseaseId.HoHapLanh,
                WeatherType.NomAm => DiseaseId.GheKySinh,
                WeatherType.NangGat => DiseaseId.SayNang,
                WeatherType.GiongBao => DiseaseId.LoiTam,
                WeatherType.MuaBucXa => DiseaseId.NamPhatQuang,
                _ => Farm.IsBarnContaminated ? DiseaseId.SotBun : DiseaseId.HoHapLanh
            };
        }

        /// <summary>
        /// Ghép đôi phối giống giữa heo nái và heo đực trong đàn (GDD 4.4, 4.8 / P2.03).
        /// </summary>
        public (bool scheduled, string message) SchedulePigBreeding(
            string motherPigId,
            string fatherPigId,
            double? deterministicConceptionRoll = null)
        {
            var mother = Pigs.Find(p => p.Id == motherPigId && p.IsAlive);
            var father = Pigs.Find(p => p.Id == fatherPigId && p.IsAlive);
            if (mother == null || father == null)
                return (false, "Không tìm thấy heo nái hoặc heo đực hợp lệ.");
            if (mother.Gender != "Cai" || father.Gender != "Duc")
                return (false, "Phải chọn đúng 1 heo Cái và 1 heo Đực.");
            if (mother.AgeDays < BreedingLedger.MinAgeDays || father.AgeDays < BreedingLedger.MinAgeDays)
                return (false, $"Heo phối giống phải đạt tối thiểu {BreedingLedger.MinAgeDays} ngày tuổi.");
            if (mother.Stage == PigStage.HeoNon || mother.Stage == PigStage.DangLon ||
                father.Stage == PigStage.HeoNon || father.Stage == PigStage.DangLon)
                return (false, "Chỉ heo Trưởng Thành hoặc Heo Già mới có thể phối giống.");
            if (mother.GestationDaysRemaining > 0)
                return (false, $"Nái [{mother.Name}] đang mang thai ({mother.GestationDaysRemaining} ngày còn lại).");
            if (mother.LittersBorn >= BreedingLedger.MaxLitters)
                return (false, $"Nái [{mother.Name}] đã sinh tối đa {BreedingLedger.MaxLitters} lứa.");
            if (mother.DaysSinceLastBirth < BreedingLedger.RestDays)
                return (false, $"Nái [{mother.Name}] cần nghỉ hồi sức {BreedingLedger.RestDays} ngày sau sinh.");
            if (mother.ActiveDisease == DiseaseId.RoiLoanSinhSan || father.ActiveDisease == DiseaseId.RoiLoanSinhSan)
                return (false, "Heo mắc Rối Loạn Sinh Sản không thể phối giống!");

            int activePregnancies = Pigs.FindAll(p => p.IsAlive && p.GestationDaysRemaining > 0).Count;
            if (activePregnancies >= BreedingLedger.MaxConcurrentPregnancies)
                return (false, $"Đã đạt trần {BreedingLedger.MaxConcurrentPregnancies} ca mang thai đồng thời!");

            bool isElderPair = mother.Stage == PigStage.HeoGia || father.Stage == PigStage.HeoGia;
            float threshold = isElderPair
                ? BreedingLedger.BaseConception * BreedingLedger.OldConceptionFactor
                : BreedingLedger.BaseConception;

            if (mother.Traits.Exists(t => t.Id == "MatTay") || father.Traits.Exists(t => t.Id == "MatTay"))
            {
                threshold = Math.Min(0.95f, threshold + 0.10f);
            }

            double roll = deterministicConceptionRoll ?? random.NextDouble();
            if (roll >= threshold)
            {
                return (false, $"Phối giống giữa [{mother.Name}] và [{father.Name}] không đậu thai (ngưỡng {threshold * 100f:0.0}%).");
            }

            bool inbred = (!string.IsNullOrEmpty(mother.FatherId) && mother.FatherId == father.Id) ||
                          (!string.IsNullOrEmpty(father.MotherId) && father.MotherId == mother.Id) ||
                          (!string.IsNullOrEmpty(mother.MotherId) && mother.MotherId == father.MotherId);

            mother.GestationDaysRemaining = BreedingLedger.GestationDays;
            mother.PendingFatherId = father.Id;
            mother.PendingFatherGene = father.GeneLine;
            mother.PendingFatherStage = father.Stage;
            mother.PendingInbred = inbred;

            string tag = isElderPair ? " (Cặp Heo Già 6 bước)" : (inbred ? " (Cận huyết)" : " (Lai F1/F2)");
            return (true, $"Đã phối giống thành công [{mother.Name}] × [{father.Name}]{tag}! Dự kiến sinh sau {BreedingLedger.GestationDays} ngày.");
        }

        /// <summary>
        /// Kéo xác heo chết về Khu Xử Lý và vận hành Thiêu Hủy (6h) hoặc Tái Chế Sinh Học (18h).
        /// </summary>
        public (bool started, string message, int staminaCost) ProcessDeadPigCarcass(string pigId, CarcassProcessMethod method)
        {
            var deadPig = Pigs.Find(p => p.Id == pigId && !p.IsAlive);
            if (deadPig == null)
            {
                return (false, "Không tìm thấy xác heo cần xử lý.", 0);
            }

            float dx = deadPig.PositionX - GridZones.ProcessingLotCenter.x;
            float dy = deadPig.PositionY - GridZones.ProcessingLotCenter.y;
            float distMeters = (float)Math.Sqrt(dx * dx + dy * dy);
            int staCost = FarmGridAndZones.CalculateDragCarcassStaminaCost(distMeters);

            var (started, msg, _) = GridZones.StartCarcassProcessing(
                Farm,
                deadPig.Id,
                deadPig.WeightKg,
                method,
                deadPig.ActiveDisease);

            if (started)
            {
                deadPig.IsInProcessingLot = true;
                Character.Stamina.CurrentStamina = Math.Max(0, Character.Stamina.CurrentStamina - staCost);
                OnLogEmitted?.Invoke(msg);
            }

            return (started, msg, staCost);
        }

        /// <summary>
        /// Thực thi 5 động từ xử lý sức khỏe (Cách ly, Xử lý môi trường, Điều trị cá thể, Phòng ngừa đàn, Nghiên cứu) trực tiếp từ GameEngine.
        /// </summary>
        public (bool success, string message) ExecuteHealthTreatment(
            HealthTreatmentVerb verb,
            string? targetPigId = null,
            FarmDrug drug = FarmDrug.HaNhiet,
            DiseaseId researchTarget = DiseaseId.NamPhatQuang)
        {
            Pig? target = string.IsNullOrEmpty(targetPigId)
                ? Pigs.Find(p => p.IsAlive && p.ActiveDisease.HasValue)
                : Pigs.Find(p => p.Id == targetPigId);

            var res = PetsAndNpcs.ExecuteHealthVerbOnPigs(verb, Farm, target, Pigs, drug, researchTarget);
            if (res.success)
            {
                OnLogEmitted?.Invoke(res.message);
            }
            return res;
        }

        /// <summary>
        /// Giải quyết 4 sự kiện Huyền Bí thuộc phạm vi Người B (Vườn Mười Hai Quả, Đồng Cỏ Không Bóng, Bí Nhân Mua Già, Đổi Một Lấy Một).
        /// </summary>
        public (bool resolved, string message) TriggerMemberBOccultEvent(MemberBOccultEventId eventId, int choiceIndex)
        {
            var res = PetsAndNpcs.ResolveMemberBOccultEvent(eventId, choiceIndex, Farm, Pigs);
            if (res.resolved)
            {
                OnLogEmitted?.Invoke(res.outcomeMessage);
            }
            return res;
        }

        /// <summary>
        /// Giải quyết bất kỳ sự kiện nào trong 14 sự kiện huyền bí (E01-E14 — GDD 24.1).
        /// </summary>
        public (bool resolved, string message) TriggerAnyMysteryEventGdd24(
            MysteryEventId eventId,
            int choiceIndex,
            bool hasBuaHoiHuong = false,
            bool hasLaBanTanVu = false)
        {
            var res = PetsAndNpcs.ResolveAnyMysteryEventGdd24(
                eventId,
                choiceIndex,
                Farm,
                Pigs,
                gateOrFogProgressRatio: 1.0f,
                useBuaHoiHuong: hasBuaHoiHuong,
                useLaBanTanVu: hasLaBanTanVu);
            if (res.resolved)
            {
                OnLogEmitted?.Invoke(res.outcomeMessage);
            }
            return res;
        }

        /// <summary>
        /// Giám định gen qua 4 kênh (GDD 5.4): Chợ Ngày (180G), Chợ Đêm (420G), Phòng Nghiên Cứu (300G), Bí Nhân (100%).
        /// </summary>
        public (bool success, int costGold, float effectiveChance, string message) AppraisePigGeneAtChannel(
            string pigId,
            AppraisalChannel channel,
            double? deterministicRoll = null)
        {
            var pig = Pigs.Find(p => p.Id == pigId && p.IsAlive);
            if (pig == null)
                return (false, 0, 0f, "Không tìm thấy heo cần giám định.");

            var (identified, finalChance, goldCost, msg) = GeneticsAndBreedingSystem.AppraisePigGene(pig, channel, Farm, deterministicRoll);
            OnLogEmitted?.Invoke(msg);
            return (identified, goldCost, finalChance, msg);
        }

        /// <summary>
        /// Thi triển 1 trong 24 chiêu võ học (GDD 12.7) kèm kiểm tra Hợp Kích 4 phái (A -> C -> B -> D -> A).
        /// </summary>
        public (bool executed, float damageMultiplier, string comboTriggered, string message) ExecuteMartialSkillWithCombo(string skillId)
        {
            var skillDef = MartialSkillCatalog.GetByIdOrFallback(skillId, Character.PrimarySchool, CombatSlotType.TheCong);
            Character.ActiveMartialSkills[skillDef.SlotType] = skillDef.Id;
            var syn = CharacterData.EvaluateSchoolComboSynergy(Character.LastExecutedSchool, skillDef.School);
            var (executed, msg, dmgMult) = Character.TryExecuteMartialSkill(skillDef.SlotType);
            if (executed)
            {
                OnLogEmitted?.Invoke(msg);
            }
            return (executed, dmgMult, syn.isSynergy ? syn.synergyName : string.Empty, msg);
        }

        /// <summary>
        /// Giải câu đố khe lò sưởi 4 lớp (Cốt truyện Mùa 1 - S04/Q16) và kiểm tra cảm xúc mặt phản chiếu.
        /// </summary>
        public (bool opened, string message) InteractStoryFurnaceSlot(
            bool scrapeSoot,
            bool tryWrongItem,
            bool turn24Inward,
            float handsOffWaitMinutes)
        {
            var res = Story.AttemptFurnaceSlotPuzzle(
                scrapedSoot: scrapeSoot,
                triedOtherBackpackItemFirst: tryWrongItem,
                insertedAncientLeather24: true,
                number24FacingInward: turn24Inward,
                waitedTwoMinutesHandsOff: handsOffWaitMinutes >= 2.0f);
            OnLogEmitted?.Invoke(res.message);
            return res;
        }

        public (string realFace, string reflectionFace, string description) CheckPlayerReflectiveSurface(string surfaceId)
        {
            var res = StoryJournalSystem.EvaluateReflectiveSurfaceEmotion(surfaceId);
            OnLogEmitted?.Invoke(res.storyContext);
            return res;
        }

        /// <summary>
        /// Phát lệnh Lùa Đàn về Nơi Trú Đàn (hoặc Tiếng Huýt Khoa) và cập nhật FlowField + vị trí đàn heo.
        /// </summary>
        public (int shelteredCount, int totalEligible, string message) CommandHerdRecallToRefuge(bool khoaWhistle = false)
        {
            var agents = new List<PigSteeringAgent>();
            var eligiblePigs = new List<Pig>();
            bool hasShepherdDog = PetsAndNpcs.HasPet(PetId.ChoChanDan);

            for (int i = 0; i < Pigs.Count; i++)
            {
                var p = Pigs[i];
                if (!p.IsAlive || p.IsIsolated) continue;

                eligiblePigs.Add(p);
                agents.Add(new PigSteeringAgent
                {
                    PigId = p.Id,
                    X = p.PositionX,
                    Y = p.PositionY,
                    BaseSpeedMetersPerSec = 2.6f,
                    GeneLine = p.GeneLine,
                    IsPanicking = p.MoodState == MoodState.HoangLoan,
                    IsIsolated = p.IsIsolated,
                    IsAlive = p.IsAlive,
                    Mood = p.Mood
                });
            }

            if (khoaWhistle)
            {
                FlowField.ExecuteKhoaWhistle(agents, GridZones.HerdRefugeCenter.x, GridZones.HerdRefugeCenter.y, hasShepherdDog);
            }

            var (recalled, eligible, ratio, gameHours) = FlowField.ExecuteRecallToRefuge(
                agents,
                GridZones.HerdRefugeCenter,
                GridZones.HerdRefugeEntranceCount,
                GridZones.HerdRefugeMinEntranceWidthMeters,
                hasShepherdDog);

            for (int i = 0; i < eligiblePigs.Count; i++)
            {
                eligiblePigs[i].PositionX = agents[i].X;
                eligiblePigs[i].PositionY = agents[i].Y;
                eligiblePigs[i].Mood = agents[i].Mood;
            }

            string msg = $"Lệnh Lùa Đàn về Nơi Trú: {recalled}/{eligible} heo ({ratio * 100f:0}%) đã vào Nơi Trú trong {gameHours:0.1} giờ game.";
            OnLogEmitted?.Invoke(msg);
            return (recalled, eligible, msg);
        }

        public (bool success, int goldEarned, string message) SellPigAtDayMarket(string pigId, bool isQuarantined = true)
        {
            var pig = Pigs.Find(p => p.Id == pigId);
            if (pig == null || !pig.IsAlive)
            {
                return (false, 0, "Heo không tồn tại hoặc đã chết!");
            }

            var (priceGold, _) = Economy.CalculatePigSalePrice(pig, Farm.FarmLevel);
            if (priceGold <= 0)
            {
                return (false, 0, "Không thể bán heo này ở chợ ngày!");
            }

            int finalGold = priceGold;
            if (isQuarantined)
            {
                finalGold -= Economy.DayMarket.QuarantineFeePerPig;
            }

            Pigs.Remove(pig);
            Farm.AddGold(finalGold);
            Farm.AddFarmExp(finalGold / 100);
            TotalPigsSold += 1;
            TotalGoldEarned += finalGold;
            Economy.DayMarket.CurrentPigsSoldToday += 1;

            return (true, finalGold, $"Đã bán {pig.Name} thu về {finalGold}G (Ông Tồn đếm số 7 là '{StoryJournalSystem.FormatEdgeTownNumber(7)}').");
        }

        /// <summary>
        /// Xử lý chu trình nhân vật bị Ngất (do Vỡ Thanh Chống Chịu = 0 dù còn Stamina, HOẶC Cạn Thể Lực Đỡ Đòn = 0 dù còn Chống Chịu)
        /// và được Tám Vá & Mận kéo về Trại Hồi Sức / Hiên Nhà Chính (GDD 8.2, 9.4, 19):
        /// - Phạt 10% Vàng đang mang, -20 Thể trạng (Condition), trôi qua +2 giờ game để hồi sức.
        /// - Hồi phục lại 60% Thanh Chống Chịu và 60% Thể Lực khả dụng tại hiên nhà chính.
        /// </summary>
        public (int goldLost, float restoredStamina, float restoredGuard, string rescueMessage) ExecutePlayerFaintRescue(string? overrideReason = null)
        {
            string reason = !string.IsNullOrWhiteSpace(overrideReason)
                ? overrideReason!
                : (!string.IsNullOrWhiteSpace(Character.LastFaintReason)
                    ? Character.LastFaintReason
                    : "Vỡ Thanh Chống Chịu / Kiệt Sức Đỡ Đòn");

            int goldPenalty = Math.Max(0, (int)Math.Round(Farm.Gold * 0.10f));
            if (goldPenalty > 0)
            {
                Farm.SpendGold(goldPenalty);
            }

            Character.Stamina.Condition = Math.Max(20, Character.Stamina.Condition - 20);
            Clock.TickMinutes(120); // Trôi 2 giờ game để đưa về trại hồi sức

            Character.IsFainted = false;
            Character.Hp = Character.MaxHp;
            Character.CurrentGuard = (float)Math.Round(Character.MaxGuard * 0.60f, 1);
            Character.Stamina.CurrentStamina = (float)Math.Round(Character.GetEffectiveMaxStamina() * 0.60f, 1);

            string msg = $"[CỨU HỘ VỀ TRẠI HỒI SỨC] Nhân vật bị ngất do [{reason}]! Tám Vá và Mận đã kéo về Hiên Nhà Chính hồi sức sau 2 giờ (-{goldPenalty}G phí cấp cứu, -20 Thể trạng, hồi lại {Character.CurrentGuard:0} Chống Chịu & {Character.Stamina.CurrentStamina:0} Thể Lực).";
            OnLogEmitted?.Invoke(msg);
            return (goldPenalty, Character.Stamina.CurrentStamina, Character.CurrentGuard, msg);
        }

        /// <summary>
        /// Mô phỏng nhân vật nhận đòn từ quái vật trong giao tranh (Auto-Guard hoặc Active Parry),
        /// tự động kích hoạt kéo về trại hồi sức nếu Thanh Chống Chịu hoặc Thể Lực chạm 0.
        /// </summary>
        public (CombatHitResolution hit, bool rescuedToFarmhouse, string rescueLog) SimulatePlayerCombatEncounter(
            float rawEnemyDamage,
            bool isActiveParry,
            bool isHoldingGuardStance = false)
        {
            var hit = Character.ProcessIncomingAttack(rawEnemyDamage, isActiveParry, isHoldingGuardStance);
            OnLogEmitted?.Invoke(hit.SummaryMessage);

            if (hit.CausedFaint)
            {
                var rescue = ExecutePlayerFaintRescue(hit.FaintReason);
                return (hit, true, rescue.rescueMessage);
            }

            return (hit, false, string.Empty);
        }

        public GameSaveData CreateSaveData()
        {
            var data = new GameSaveData();

            // Trạng thái đồng hồ
            data.CurrentDay = Clock.CurrentDay;
            data.CurrentHour = Clock.CurrentHour;
            data.CurrentMinute = Clock.CurrentMinute;
            data.CycleNumber = Clock.CycleNumber;
            data.CycleTotalDays = Clock.CycleTotalDays;
            data.CycleDayIndex = Clock.CycleDayIndex;
            data.MinorWavesRemainingInCycle = Clock.MinorWavesRemainingInCycle;
            data.HoursUntilNextMinorWave = Clock.HoursUntilNextMinorWave;
            data.SleepCooldownHoursRemaining = Clock.SleepCooldownHoursRemaining;
            data.IsDuringCombatWave = Clock.IsDuringCombatWave;

            // Trạng thái chung
            data.CurrentWeather = CurrentWeather;
            data.MajorWavesSurvived = MajorWavesSurvived;
            data.MinorWavesDefeated = MinorWavesDefeated;
            data.TotalPigsSold = TotalPigsSold;
            data.TotalGoldEarned = TotalGoldEarned;

            // Nông trại
            data.FarmLevel = Farm.FarmLevel;
            data.FarmExp = Farm.FarmExp;
            data.HabitatIndex = Farm.HabitatIndex;
            data.EventPressureIndex = Farm.EventPressureIndex;
            data.Gold = Farm.Gold;
            data.FuelLiters = Farm.FuelLiters;
            data.CorpseLotCount = Farm.CorpseLotCount;
            data.FeedStockKg = Farm.FeedStockKg;
            data.MedicalHerbCount = Farm.MedicalHerbCount;
            data.VaccineDoseCount = Farm.VaccineDoseCount;
            data.LocalCrowdingActive = Farm.LocalCrowdingActive;
            data.LocalCrowdingContagionMultiplier = Farm.LocalCrowdingContagionMultiplier;
            data.LocalCrowdingPanicBonus = Farm.LocalCrowdingPanicBonus;
            data.IsBarnContaminated = Farm.IsBarnContaminated;
            data.Infrastructure = Farm.Infrastructure;

            // Bầy Đàn
            data.HerdTier = Herd.Tier;
            data.HerdConsecutiveDaysInState = Herd.ConsecutiveDaysInState;
            data.HerdTransmissionStacks = Herd.TransmissionStacks;
            data.HerdLeader = Herd.Leader;
            data.HerdSuccessionTraining = Herd.SuccessionTraining;

            // Lưới công trình, Khu Xử Lý, Thú cưng, NPC & Nghiên cứu bệnh
            data.PlacedFarmStructures = new List<PlacedFarmStructure>(GridZones.Structures);
            data.ActiveCarcassJobs = new List<CarcassProcessingJob>(GridZones.ActiveCarcassJobs);
            data.ActivePets = new List<ActivePetSlot>(PetsAndNpcs.ActivePets);
            data.HiredNpcs = new List<HiredNpcRecord>(PetsAndNpcs.HiredNpcs);
            data.ResearchedDiseases = new List<DiseaseId>(PetsAndNpcs.ResearchedDiseases);

            // Nhân vật
            data.CharacterId = Character.Id;
            data.CharacterName = Character.Name;
            data.CharacterLevel = Character.Level;
            data.CharacterExp = Character.Exp;
            data.Hp = Character.Hp;
            data.MaxHp = Character.MaxHp;
            data.Rage = Character.Rage;
            data.CurrentGuard = Character.CurrentGuard;
            data.MaxGuard = Character.MaxGuard;
            data.CurrentStamina = Character.Stamina.CurrentStamina;
            data.MaxStamina = Character.Stamina.MaxStamina;
            data.DailyLoad = Character.Stamina.DailyLoad;
            data.Condition = Character.Stamina.Condition;
            data.ReboundFatigueStacks = Character.Stamina.ReboundFatigueStacks;
            data.FaintCount = Character.FaintCount;
            data.PrimarySchool = Character.PrimarySchool;

            // Cốt truyện & Cơ chế chuyên sâu
            data.StoryChapter = Story.CurrentChapter;
            data.ChosenEnding = Story.ChosenEnding;
            data.SeventhChannelCut = Story.SeventhChannelCut;
            data.HerdEmptySpotObserved = Story.HerdEmptySpotObserved;
            data.MidGameGateUnlocked = DeepMechanics.MidGameGateUnlocked;
            data.UnlockedSpecialtyLocksCount = DeepMechanics.UnlockedSpecialtyLocksCount;
            data.EquippedPaperEnhancementLevel = DeepMechanics.intEquippedPaperEnhancementLevel;
            data.PowerGeneratorLevel = DeepMechanics.PowerGrid.GeneratorLevel;

            // Danh sách heo
            foreach (var pig in Pigs)
            {
                data.Pigs.Add(new PigSaveData
                {
                    Id = pig.Id,
                    Name = pig.Name,
                    AgeDays = pig.AgeDays,
                    WeightKg = pig.WeightKg,
                    Stage = pig.Stage,
                    Gender = pig.Gender,
                    GeneLine = pig.GeneLine,
                    Rarity = pig.Rarity,
                    IsIdentified = pig.IsIdentified,
                    IsXichMaoDomesticated = pig.IsXichMaoDomesticated,
                    XichMaoDomesticationDays = pig.XichMaoDomesticationDays,
                    Health = pig.Health,
                    Temperament = pig.Temperament,
                    Mood = pig.Mood,
                    Bonding = pig.Bonding,
                    MeatQuality = pig.MeatQuality,
                    GrowthRateMultiplier = pig.GrowthRateMultiplier,
                    MaxWeightKg = pig.MaxWeightKg,
                    IsQuarantinedForSale = pig.IsQuarantinedForSale,
                    IsAlive = pig.IsAlive,
                    IsInProcessingLot = pig.IsInProcessingLot,
                    DaysDeceased = pig.DaysDeceased,
                    PositionX = pig.PositionX,
                    PositionY = pig.PositionY,
                    Hunger = pig.Hunger,
                    Comfort = pig.Comfort,
                    FedToday = pig.FedToday,
                    OutsideServiceHoursToday = pig.OutsideServiceHoursToday,
                    IsIsolated = pig.IsIsolated,
                    ActiveDisease = pig.ActiveDisease,
                    ImmuneDays = pig.ImmuneDays,
                    SeizurePending = pig.SeizurePending,
                    MotherId = pig.MotherId,
                    FatherId = pig.FatherId,
                    Generation = pig.Generation,
                    IsInbred = pig.IsInbred,
                    GestationDaysRemaining = pig.GestationDaysRemaining,
                    LittersBorn = pig.LittersBorn,
                    DaysSinceLastBirth = pig.DaysSinceLastBirth,
                    PendingFatherId = pig.PendingFatherId,
                    PendingFatherGene = pig.PendingFatherGene,
                    PendingFatherStage = pig.PendingFatherStage,
                    PendingInbred = pig.PendingInbred,
                    ShapingDaysRecorded = pig.ShapingDaysRecorded,
                    ShapingDaysFed = pig.ShapingDaysFed,
                    ShapingMoodSum = pig.ShapingMoodSum,
                    ShapingHabitatSum = pig.ShapingHabitatSum,
                    ShapingSickDays = pig.ShapingSickDays,
                    Traits = new List<PigTrait>(pig.Traits)
                });
            }

            // Công trình phòng thủ
            data.HallLevel = Defense.HallLevel;
            data.UnlockedBlueprints = new List<string>(Defense.UnlockedBlueprints);
            data.Buildings = new List<DefenseBuildingInstance>(Defense.Buildings);

            return data;
        }

        public void RestoreFromSave(GameSaveData data)
        {
            if (data == null)
                return;

            Clock.RestoreFromSave(
                data.CurrentDay,
                data.CurrentHour,
                data.CurrentMinute,
                data.CycleNumber,
                data.CycleTotalDays,
                data.CycleDayIndex,
                data.MinorWavesRemainingInCycle,
                data.HoursUntilNextMinorWave,
                data.SleepCooldownHoursRemaining,
                data.IsDuringCombatWave);

            Farm.FarmLevel = data.FarmLevel;
            Farm.FarmExp = data.FarmExp;
            Farm.HabitatIndex = data.HabitatIndex;
            Farm.EventPressureIndex = data.EventPressureIndex;
            Farm.Gold = data.Gold;
            Farm.FuelLiters = data.FuelLiters;
            Farm.CorpseLotCount = data.CorpseLotCount;
            Farm.FeedStockKg = data.FeedStockKg > 0f || data.SaveVersion >= 2 ? data.FeedStockKg : 300f;
            Farm.MedicalHerbCount = data.SaveVersion >= 2 ? data.MedicalHerbCount : 5;
            Farm.VaccineDoseCount = data.SaveVersion >= 2 ? data.VaccineDoseCount : 5;
            Farm.LocalCrowdingActive = data.LocalCrowdingActive;
            Farm.LocalCrowdingContagionMultiplier = data.LocalCrowdingContagionMultiplier > 0f ? data.LocalCrowdingContagionMultiplier : 1.0f;
            Farm.LocalCrowdingPanicBonus = data.LocalCrowdingPanicBonus;
            Farm.IsBarnContaminated = data.IsBarnContaminated;
            Farm.Infrastructure = data.Infrastructure ?? new FarmInfrastructure();

            Herd.RestoreState(
                data.HerdTier,
                data.HerdConsecutiveDaysInState,
                data.HerdTransmissionStacks,
                data.HerdLeader,
                data.HerdSuccessionTraining);

            GridZones.RestoreGridState(data.PlacedFarmStructures, data.ActiveCarcassJobs);

            if (data.ActivePets != null)
            {
                PetsAndNpcs.ActivePets.Clear();
                foreach (var pet in data.ActivePets)
                {
                    if (pet != null) PetsAndNpcs.ActivePets.Add(pet);
                }
            }

            if (data.HiredNpcs != null)
            {
                PetsAndNpcs.HiredNpcs.Clear();
                foreach (var npc in data.HiredNpcs)
                {
                    if (npc != null) PetsAndNpcs.HiredNpcs.Add(npc);
                }
            }

            if (data.ResearchedDiseases != null)
            {
                PetsAndNpcs.ResearchedDiseases.Clear();
                foreach (var d in data.ResearchedDiseases)
                {
                    PetsAndNpcs.ResearchedDiseases.Add(d);
                }
            }

            Defense.HallLevel = data.HallLevel;
            Defense.UnlockedBlueprints = data.UnlockedBlueprints != null
                ? new List<string>(data.UnlockedBlueprints)
                : new List<string>();
            Defense.Buildings = new List<DefenseBuildingInstance>();
            if (data.Buildings != null)
            {
                foreach (var savedBuilding in data.Buildings)
                {
                    if (savedBuilding == null ||
                        string.IsNullOrWhiteSpace(savedBuilding.InstanceId) ||
                        string.IsNullOrWhiteSpace(savedBuilding.BlueprintId))
                    {
                        continue;
                    }

                    Defense.Buildings.Add(new DefenseBuildingInstance
                    {
                        InstanceId = savedBuilding.InstanceId,
                        BlueprintId = savedBuilding.BlueprintId,
                        Name = string.IsNullOrWhiteSpace(savedBuilding.Name)
                            ? savedBuilding.BlueprintId
                            : savedBuilding.Name,
                        Category = savedBuilding.Category,
                        IsBachVan = savedBuilding.IsBachVan,
                        Durability = savedBuilding.Durability,
                        MaxHp = savedBuilding.MaxHp,
                        CurrentHp = savedBuilding.CurrentHp,
                        IsBroken = savedBuilding.IsBroken,
                        AmmoCurrent = savedBuilding.AmmoCurrent,
                        AmmoCapacity = savedBuilding.AmmoCapacity,
                        AmmoName = savedBuilding.AmmoName ?? string.Empty,
                        AmmoCostPerShot = savedBuilding.AmmoCostPerShot,
                        CooldownSeconds = savedBuilding.CooldownSeconds,
                        CurrentCooldownRemaining = savedBuilding.CurrentCooldownRemaining,
                        FireMode = savedBuilding.FireMode ?? "TuDo"
                    });
                }
            }

            if (data.CharacterId == PlayerRoster.KhoaDataId || data.CharacterName == "Khoa")
            {
                PlayerRoster.Apply(Character, PlayerId.Khoa);
            }
            else if (data.CharacterId == PlayerRoster.AnDataId || data.CharacterName == "An")
            {
                PlayerRoster.Apply(Character, PlayerId.An);
            }

            Character.Id = string.IsNullOrWhiteSpace(data.CharacterId)
                ? Character.Id
                : data.CharacterId;
            Character.Name = string.IsNullOrWhiteSpace(data.CharacterName)
                ? Character.Name
                : data.CharacterName;
            Character.Level = data.CharacterLevel;
            Character.Exp = data.CharacterExp;
            Character.Hp = data.Hp;
            Character.MaxHp = data.MaxHp;
            Character.Rage = data.Rage;
            if (data.MaxGuard > 0f) Character.MaxGuard = data.MaxGuard;
            if (data.CurrentGuard > 0f) Character.CurrentGuard = data.CurrentGuard;
            if (data.MaxStamina > 0f) Character.Stamina.MaxStamina = data.MaxStamina;
            if (data.CurrentStamina > 0f) Character.Stamina.CurrentStamina = data.CurrentStamina;
            Character.Stamina.DailyLoad = data.DailyLoad;
            if (data.Condition > 0) Character.Stamina.Condition = data.Condition;
            Character.Stamina.ReboundFatigueStacks = data.ReboundFatigueStacks;
            Character.FaintCount = data.FaintCount;
            Character.PrimarySchool = data.PrimarySchool;

            Story.CurrentChapter = data.StoryChapter;
            Story.ChosenEnding = data.ChosenEnding;
            Story.SeventhChannelCut = data.SeventhChannelCut;
            Story.HerdEmptySpotObserved = data.HerdEmptySpotObserved;
            DeepMechanics.MidGameGateUnlocked = data.MidGameGateUnlocked;
            DeepMechanics.UnlockedSpecialtyLocksCount = data.UnlockedSpecialtyLocksCount;
            DeepMechanics.intEquippedPaperEnhancementLevel = data.EquippedPaperEnhancementLevel;
            if (data.PowerGeneratorLevel > 0) DeepMechanics.PowerGrid.GeneratorLevel = data.PowerGeneratorLevel;

            CurrentWeather = data.CurrentWeather;
            MajorWavesSurvived = data.MajorWavesSurvived;
            MinorWavesDefeated = data.MinorWavesDefeated;
            TotalPigsSold = data.TotalPigsSold;
            TotalGoldEarned = data.TotalGoldEarned;

            Pigs.Clear();

            if (data.Pigs != null)
            {
                foreach (var savedPig in data.Pigs)
                {
                    if (savedPig == null ||
                        string.IsNullOrWhiteSpace(savedPig.Id) ||
                        string.IsNullOrWhiteSpace(savedPig.Name))
                    {
                        continue;
                    }

                    var pig = Pig.CreateDefault(
                        savedPig.Id,
                        savedPig.Name,
                        savedPig.GeneLine,
                        savedPig.Rarity,
                        string.IsNullOrWhiteSpace(savedPig.Gender) ? "Duc" : savedPig.Gender,
                        savedPig.AgeDays);

                    pig.WeightKg = savedPig.WeightKg;
                    pig.Stage = savedPig.Stage;
                    pig.IsIdentified = savedPig.IsIdentified;
                    pig.IsXichMaoDomesticated = savedPig.IsXichMaoDomesticated;
                    pig.XichMaoDomesticationDays = savedPig.XichMaoDomesticationDays;
                    pig.Health = (int)savedPig.Health;
                    pig.Temperament = (int)savedPig.Temperament;
                    pig.Mood = (int)savedPig.Mood;
                    pig.Bonding = savedPig.Bonding;
                    pig.MeatQuality = savedPig.MeatQuality;
                    pig.GrowthRateMultiplier = savedPig.GrowthRateMultiplier;
                    pig.MaxWeightKg = savedPig.MaxWeightKg;
                    pig.IsQuarantinedForSale = savedPig.IsQuarantinedForSale;
                    pig.IsAlive = savedPig.IsAlive;
                    pig.IsInProcessingLot = savedPig.IsInProcessingLot;
                    pig.DaysDeceased = savedPig.DaysDeceased;
                    pig.PositionX = savedPig.PositionX;
                    pig.PositionY = savedPig.PositionY;
                    pig.Hunger = savedPig.Hunger;
                    pig.Comfort = savedPig.Comfort > 0 ? savedPig.Comfort : 70;
                    pig.FedToday = savedPig.FedToday;
                    pig.OutsideServiceHoursToday = savedPig.OutsideServiceHoursToday;
                    pig.IsIsolated = savedPig.IsIsolated;
                    pig.ActiveDisease = savedPig.ActiveDisease;
                    pig.ImmuneDays = savedPig.ImmuneDays;
                    pig.SeizurePending = savedPig.SeizurePending;
                    pig.MotherId = savedPig.MotherId ?? string.Empty;
                    pig.FatherId = savedPig.FatherId ?? string.Empty;
                    pig.Generation = savedPig.Generation;
                    pig.IsInbred = savedPig.IsInbred;
                    pig.GestationDaysRemaining = savedPig.GestationDaysRemaining;
                    pig.LittersBorn = savedPig.LittersBorn;
                    pig.DaysSinceLastBirth = savedPig.DaysSinceLastBirth;
                    pig.PendingFatherId = savedPig.PendingFatherId ?? string.Empty;
                    pig.PendingFatherGene = savedPig.PendingFatherGene;
                    pig.PendingFatherStage = savedPig.PendingFatherStage;
                    pig.PendingInbred = savedPig.PendingInbred;
                    pig.ShapingDaysRecorded = savedPig.ShapingDaysRecorded;
                    pig.ShapingDaysFed = savedPig.ShapingDaysFed;
                    pig.ShapingMoodSum = savedPig.ShapingMoodSum;
                    pig.ShapingHabitatSum = savedPig.ShapingHabitatSum;
                    pig.ShapingSickDays = savedPig.ShapingSickDays;
                    if (savedPig.Traits != null && savedPig.Traits.Count > 0)
                    {
                        pig.Traits = new List<PigTrait>(savedPig.Traits);
                    }

                    Pigs.Add(pig);
                }
            }
        }
    }
}
