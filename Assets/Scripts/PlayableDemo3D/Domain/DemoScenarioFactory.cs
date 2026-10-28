using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PigTycoon.Core;

namespace PigTycoon.PlayableDemo3D
{
    public enum DemoScenarioKind { Care, PreRaidFixture }

    [Serializable]
    public sealed class DemoPigSeed
    {
        public string Suffix = "", Name = "";
        public int AgeDays = 14;
        public float WeightKg = 100f, X, Y;
        public int Hunger = 60;
        public GeneLineId GeneLine = GeneLineId.HongDien;
    }

    [Serializable]
    public sealed class DemoScenarioLayout
    {
        public PlayableMapConfiguration Map = new PlayableMapConfiguration();
        public float PlayerX, PlayerY = -8f;
        public float ManX = -6f, ManY = -8f;
        public float DefenseX, DefenseY = -4f;
        public List<DemoPigSeed> Pigs = new List<DemoPigSeed>();

        public void Validate()
        {
            Map.Validate();
            var serviceIds = new HashSet<string>();
            foreach (var point in Map.ServicePoints)
                if (!serviceIds.Add(point.Id)) throw new ArgumentException("Authored services need unique stable IDs.");
            if (!Map.MapBounds.Contains(PlayerX, PlayerY) || !Map.MapBounds.Contains(ManX, ManY) ||
                !Map.MapBounds.Contains(DefenseX, DefenseY)) throw new ArgumentException("Scenario actors must lie within the reviewed map.");
            if (Pigs == null || Pigs.Count == 0) throw new ArgumentException("Scenario needs an explicit live Core herd.");
            var ids = new HashSet<string>();
            foreach (var pig in Pigs)
                if (pig == null || string.IsNullOrWhiteSpace(pig.Suffix) || !ids.Add(pig.Suffix) ||
                    !Map.PastureBounds.Contains(pig.X, pig.Y) || pig.AgeDays < 0 || pig.WeightKg <= 0f || float.IsNaN(pig.WeightKg) ||
                    float.IsInfinity(pig.WeightKg) || pig.Hunger < 0 || pig.Hunger > 100 || !Enum.IsDefined(typeof(GeneLineId), pig.GeneLine))
                    throw new ArgumentException("Pig IDs, ages, weights and pasture positions must be explicit and valid.");
        }
    }

    [Serializable]
    public sealed class DemoPigSetupSnapshot
    {
        public string Id = "";
        public float X, Y, WeightKg;
        public int AgeDays, Health, Hunger, LastPaidRationDay;
        public bool FedToday;
    }
    [Serializable]
    public sealed class DemoScenarioManifest
    {
        public string ProfileId = "", Label = "", SaveFileName = "", ResourceProvenance = "";
        public bool ScenarioChoicesAreNotCanon = true;
        public bool InteractiveHerdOwnership, StrategicSetupAdvanceOnly;
        public int Seed, SimulatedMinutesAdvanced, InitialGold, SetupFarmLevel, SetupFarmExp;
        public bool SetupLearnedNoBlueprint;
        public float InitialFeedKg;
        public float FinalFeedKg;
        public int FinalDay, FinalHour, FinalMinute, HoursUntilScheduledMinor, LivePigCount, Capacity;
        public string TowerInstanceId = "";
        public List<string> ActualPigIds = new List<string>();
        public List<DemoPigSetupSnapshot> InitialHerd = new List<DemoPigSetupSnapshot>();
        public List<DemoPigSetupSnapshot> FinalHerd = new List<DemoPigSetupSnapshot>();
    }

    public sealed class DemoScenario
    {
        public GameEngine Engine { get; }
        public DemoScenarioLayout Layout { get; }
        public DemoScenarioManifest Manifest { get; }
        public string ProfileId => Manifest.ProfileId;
        public string SaveFileName => Manifest.SaveFileName;
        public string TowerInstanceId => Manifest.TowerInstanceId;
        public DemoScenario(GameEngine engine, DemoScenarioLayout layout, DemoScenarioManifest manifest)
        { Engine = engine; Layout = layout; Manifest = manifest; }
    }

    /// <summary>
    /// Explicit demonstration fixtures, not a new canonical balance book. Every resource is declared
    /// in a manifest; the separate pre-raid fixture never rewrites ordinary care progress.
    /// </summary>
    public static class DemoScenarioFactory
    {
        public const int ScenarioSeed = 20260601;
        public const string OwnedTowerId = "demo.preraid.no-xuyen-van.01";
        public const string CareProfile = "demo.care.v1";
        public const string PreRaidProfile = "demo.preraid.v1";

        public static string SerializeLaunch(DemoScenario scenario, PlayableFarmSession session, bool restored, int actualPigViews)
        {
            return JsonConvert.SerializeObject(new
            {
                Setup = scenario.Manifest,
                Actual = new { RestoredProfileSave = restored, ProfileId = session.State.ScenarioProfileId,
                    InteractiveHerdOwnership = session.Engine.InteractiveHerdOwned, Herd = Snapshot(session.Engine),
                    Day = session.Engine.Clock.CurrentDay, Hour = session.Engine.Clock.CurrentHour, Minute = session.Engine.Clock.CurrentMinute,
                    session.Engine.Farm.Gold, session.Engine.Farm.FeedStockKg, LivePigViews = actualPigViews,
                    LivePigIds = session.Engine.Pigs.FindAll(p => p.IsAlive).ConvertAll(p => p.Id),
                    TowerInstanceId = scenario.TowerInstanceId }
            }, Formatting.Indented);
        }

        public static DemoScenario Create(DemoScenarioKind kind, DemoScenarioLayout? requestedLayout = null)
        {
            var layout = CloneLayout(requestedLayout ?? DefaultLayout());
            layout.Validate();
            if (kind != DemoScenarioKind.Care && kind != DemoScenarioKind.PreRaidFixture) throw new ArgumentOutOfRangeException(nameof(kind));
            bool preRaid = kind == DemoScenarioKind.PreRaidFixture;
            string profile = preRaid ? PreRaidProfile : CareProfile;
            var engine = new GameEngine(ScenarioSeed);
            layout.Map.InteractiveHerdOwnership = true;
            engine.ConfigurePlayableMap(layout.Map);
            engine.Defense.HallLevel = 1;
            engine.Defense.UnlockedBlueprints.Remove("NoXuyenVan");
            engine.SwitchActiveProtagonist(PlayerId.Khoa);
            foreach (var item in EquipmentAndTacticalCatalog.AllEquipmentItems)
                if (item.Id == "wpn_dao_rua") { engine.Character.EquipItem(item); break; }
            foreach (var seed in layout.Pigs)
            {
                var pig = Pig.CreateDefault(profile + ".pig." + seed.Suffix, seed.Name, seed.GeneLine, GeneRarity.Thuong,
                    initialAge: seed.AgeDays, initialWeight: seed.WeightKg);
                pig.UpdateStageAndLimits(); pig.PositionX = seed.X; pig.PositionY = seed.Y;
                pig.Hunger = seed.Hunger; engine.Pigs.Add(pig);
            }
            // Match the explicitly authored service count, preserving the12 plots from the handover.
            int feed = 0, water = 0, shelter = 0;
            foreach (var point in layout.Map.ServicePoints) { if (point.Feed) feed++; if (point.Water) water++; if (point.Shelter) shelter++; }
            engine.Farm.Infrastructure = new FarmInfrastructure { LandPlots = 12, Feeders = feed, WaterTroughs = water, Shelters = shelter };
            var manifest = new DemoScenarioManifest
            {
                ProfileId = profile, Seed = ScenarioSeed,
                Label = preRaid ? "Fixture thử phòng thủ — đã sở hữu trang bị, không phải tiến độ chơi đã hoàn thành" : "Demo chăm trại — cấu hình trải nghiệm, chưa phải số khởi đầu cân bằng chính thức",
                SaveFileName = preRaid ? "pig-tycoon-demo3d-preraid-v1.json" : "pig-tycoon-demo3d-care-v1.json",
                InitialGold = engine.Farm.Gold, InitialFeedKg = engine.Farm.FeedStockKg,
                InteractiveHerdOwnership = engine.InteractiveHerdOwned, StrategicSetupAdvanceOnly = preRaid,
                ResourceProvenance = "Khoa; Dao Rựa sở hữu sẵn trong cấu hình demo. Vàng/cám lấy đúng mặc định Core và được ghi rõ; số heo, tuổi, cân nặng, tọa độ và hạ tầng phục vụ là lựa chọn demo, không khẳng định là số trong truyện. Hội trường cấp1 không có nhân sự. Không tự hoàn thành quest hoặc nhận thưởng."
            };
            manifest.InitialHerd = Snapshot(engine);
            if (preRaid)
            {
                // Director-approved fixture prerequisite, never a claim of earned campaign progress.
                // Existing XP APIs disagree with one another/source; do not retune or invoke either here.
                engine.Farm.FarmLevel = 3;
                engine.Defense.UnlockedBlueprints.Add("NoXuyenVan");
                manifest.SetupLearnedNoBlueprint = true;
                if (engine.Farm.FarmLevel != 3 || engine.Defense.HallLevel != 1)
                    throw new InvalidOperationException("The declared defense fixture must meet its farm-level/hall prerequisites.");
                var built = engine.Defense.ConstructBuilding("NoXuyenVan");
                if (!built.success || built.building == null) throw new InvalidOperationException("Core does not support the declared owned fixture tower.");
                built.building.InstanceId = OwnedTowerId; built.building.AmmoCurrent = 0;
                if (built.building.AmmoCapacity != 50) throw new InvalidOperationException("Source fixture requires actual capacity50; do not silently retune it.");
                engine.Inventory.AddItem("TenThep", 50); manifest.TowerInstanceId = built.building.InstanceId;
                manifest.ResourceProvenance += " Fixture phòng thủ khai báo cấp trại3, EXP0, hội trường1 và bản vẽ Nỏ đã học; cấp/bản vẽ là thiết lập kịch bản, không phải thành tích đã chơi. Không gọi công thức cộng EXP. Sở hữu sẵn đúng một Nỏ rỗng và 50 Tên Thép; không tuyên bố đã mua/xây/chế tạo trong một phiên chơi lịch sử. Không tạo ô chợ đêm.";
                if (engine.Clock.MinorWavesRemainingInCycle < 1 || engine.Clock.HoursUntilNextMinorWave < 2)
                    throw new InvalidOperationException("Declared fixture seed has no suitable naturally scheduled minor arrival.");
                while (engine.Clock.HoursUntilNextMinorWave > 2)
                {
                    engine.Tick(1); manifest.SimulatedMinutesAdvanced++;
                    if (engine.Clock.IsMinorWaveTriggered || engine.Clock.CycleDayIndex >= engine.Clock.CycleTotalDays)
                        throw new InvalidOperationException("Fixture cannot skip an arrival or major battle to reach its start point.");
                }
                manifest.ResourceProvenance += " Mốc35 giờ là bước tiến thiết lập chiến lược của đồng hồ/sức khỏe/tuổi/kinh tế; chưa gắn điều hướng/vật cản thực, không phải35 giờ đã chơi hay phát lại chuyển động. Giữ nguyên nhu cầu/khẩu phần thực sau thời gian không chăm; không phục hồi đói hoặc cấp bữa ăn ngầm.";
                ValidateMinorComposition(engine);
                if (!HasClearEntry(layout, engine)) throw new InvalidOperationException("No actual declared approach point satisfies the25m protected-point rule.");
            }
            ValidateHerd(engine, layout);
            manifest.SetupFarmLevel = engine.Farm.FarmLevel;
            manifest.SetupFarmExp = engine.Farm.FarmExp;
            manifest.FinalFeedKg = engine.Farm.FeedStockKg;
            manifest.FinalHerd = Snapshot(engine);
            manifest.FinalDay = engine.Clock.CurrentDay; manifest.FinalHour = engine.Clock.CurrentHour; manifest.FinalMinute = engine.Clock.CurrentMinute;
            manifest.HoursUntilScheduledMinor = engine.Clock.HoursUntilNextMinorWave;
            manifest.LivePigCount = engine.Pigs.FindAll(p => p.IsAlive).Count;
            manifest.Capacity = engine.Farm.CalculateCapacityReport(manifest.LivePigCount, 0).EffectiveCapacity;
            foreach (var pig in engine.Pigs) manifest.ActualPigIds.Add(pig.Id);
            return new DemoScenario(engine, layout, manifest);
        }
        private static List<DemoPigSetupSnapshot> Snapshot(GameEngine engine) => engine.Pigs.ConvertAll(p =>
            new DemoPigSetupSnapshot { Id = p.Id, X = p.PositionX, Y = p.PositionY, WeightKg = p.WeightKg,
                AgeDays = p.AgeDays, Health = p.Health, Hunger = p.Hunger, FedToday = p.FedToday, LastPaidRationDay = p.LastPaidRationDay });

        public static DemoScenarioLayout DefaultLayout()
        {
            var layout = new DemoScenarioLayout
            {
                Map = new PlayableMapConfiguration { ConfigurationId = "demo.farm.reference-layout.v1", MapX = -60, MapY = -45, MapWidth = 120, MapHeight = 90,
                    PastureX = -20, PastureY = -14, PastureWidth = 40, PastureHeight = 28, RefugeX = 0, RefugeY = 6 }
            };
            layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "demo.feed", X = -2, Y = -1, Feed = true });
            layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "demo.water", X = 2, Y = -1, Water = true });
            layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "demo.shelter", X = 1, Y = 4, Shelter = true, Cooling = true });
            foreach (var point in new[] { (-15f, 40f, ApproachSector.North), (15f, 40f, ApproachSector.North),
                (55f, -10f, ApproachSector.East), (55f, 10f, ApproachSector.East), (-15f, -40f, ApproachSector.South),
                (15f, -40f, ApproachSector.South), (-55f, -10f, ApproachSector.West), (-55f, 10f, ApproachSector.West) })
                layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = point.Item1, Y = point.Item2, Sector = point.Item3 });
            layout.Pigs.Add(new DemoPigSeed { Suffix = "01", Name = "Hồng Điền 01", X = -2, Y = 1 });
            layout.Pigs.Add(new DemoPigSeed { Suffix = "02", Name = "Hồng Điền 02", X = 12, Y = 8 });
            return layout;
        }

        private static DemoScenarioLayout CloneLayout(DemoScenarioLayout layout) =>
            JsonConvert.DeserializeObject<DemoScenarioLayout>(JsonConvert.SerializeObject(layout)) ?? throw new ArgumentException("Cannot read scenario layout.");
        private static void ValidateHerd(GameEngine engine, DemoScenarioLayout layout)
        {
            var ids = new HashSet<string>();
            foreach (var pig in engine.Pigs)
                if (!pig.IsAlive || !ids.Add(pig.Id) || !layout.Map.PastureBounds.Contains(pig.PositionX, pig.PositionY))
                    throw new InvalidOperationException("Simulation did not retain the declared live herd and actual positions.");
            if (engine.Farm.CalculateCapacityReport(engine.Pigs.Count, 0).EffectiveCapacity < engine.Pigs.Count)
                throw new InvalidOperationException("Authored infrastructure cannot hold the declared herd.");
        }
        private static void ValidateMinorComposition(GameEngine engine)
        {
            int budget = EnemyData.CalculateWaveThreatBudget(engine.Clock.CycleNumber, engine.Pigs.Count);
            if (27 < budget * 0.15f || 27 > budget * 0.30f) throw new InvalidOperationException("Declared27-threat pair is outside the source minor-wave ratio.");
        }
        private static bool HasClearEntry(DemoScenarioLayout layout, GameEngine engine)
        {
            foreach (var point in layout.Map.MonsterEntries)
            {
                bool clear = layout.Map.MapBounds.Contains(point.X, point.Y) && Far(point.X, point.Y, layout.PlayerX, layout.PlayerY) &&
                    Far(point.X, point.Y, layout.ManX, layout.ManY) && Far(point.X, point.Y, layout.DefenseX, layout.DefenseY);
                foreach (var pig in engine.Pigs) clear &= Far(point.X, point.Y, pig.PositionX, pig.PositionY);
                foreach (var service in layout.Map.ServicePoints) clear &= Far(point.X, point.Y, service.X, service.Y);
                if (clear) return true;
            }
            return false;
        }
        private static bool Far(float ax, float ay, float bx, float by) { float x = ax - bx, y = ay - by; return x * x + y * y >= 625f; }
    }
}
