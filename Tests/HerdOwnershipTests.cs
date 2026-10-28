#if PLAYABLE_HERD_OWNERSHIP_TESTS
using System;
using System.Linq;
using Newtonsoft.Json;
using PigTycoon.Core;
using PigTycoon.PlayableDemo3D;
internal static class HerdOwnershipTests
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < 0.0001f;
    private static DemoScenarioLayout Layout(bool bothNearFeeder = false)
    {
        var layout = DemoScenarioFactory.DefaultLayout(); layout.Map.ConfigurationId = "test.interactive-owner.centered";
        layout.Map.ServicePoints.Clear();
        layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "actual.feeder", X = -3, Y = -0.6f, Feed = true });
        layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "actual.water", X = 3, Y = -0.6f, Water = true });
        layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "actual.shelter", X = 4.6f, Y = 2.25f, Shelter = true });
        layout.Pigs[0].X = -2.6f; layout.Pigs[0].Y = 0.4f;
        layout.Pigs[1].X = bothNearFeeder ? -4f : 12f; layout.Pigs[1].Y = bothNearFeeder ? 0.4f : 8f;
        return layout;
    }
    private static FarmInteractionPoint Feeder() => new FarmInteractionPoint("actual.feeder", FarmInteractionKind.Feed, -3, -0.6f);
    private static void Footprint()
    {
        var scenario = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout()); var e = scenario.Engine; var s = new PlayableFarmSession(e);
        s.SetPlayerPosition(0, -8); float radius = (float)Math.Sqrt(2f * 2f + 0.15f * 0.15f);
        s.RegisterFence("box4m", 0, 0, true, radius); e.Clock.IsMinorWaveTriggered = true;
        s.SetFenceBoxFootprint("box4m", 0, 0, 2, 0.15f, 0);
        var raid = new PlayableFarmRaid(s); Check(raid.TryStartScheduledMinor(0, 42, (_, __) => false).Success, "Actual north entry passes Core spawn checks");
        var crawler = raid.Enemies.Single(n => n.Model.TypeId == EnemyTypeId.KeCaoPha); crawler.X = 0; crawler.Y = 2.6f;
        raid.Advance(0.05f, (_, __, ___, ____) => false);
        Check(s.State.Fences[0].Hp == 300 && crawler.AttackSequence == 0,
            "A4×.3m thin box must not be damaged from2.45m beyond its broad face with source AttackRange1; actual HP=" + s.State.Fences[0].Hp + "; registered radius=" + radius);
        var footprint = s.State.Fences[0];
        Check(footprint.DistanceSquaredToSurface(0, 1.05f) <= 1 && footprint.DistanceSquaredToSurface(2.9f, 0) <= 1,
            "The existing1m attack range admits actual nearby broad-face and endpoint contacts");
        Check(footprint.DistanceSquaredToSurface(2.8f, 1) > 1, "An enemy outside1m of the real box corner cannot attack via a circumcircle");
        crawler.X = 0; crawler.Y = 1.05f; crawler.Cooldown = 0;
        raid.Advance(0.05f, (_, __, ___, ____) => false);
        Check(footprint.Hp == 285 && crawler.AttackSequence == 1, "Actual close-face attack retains source15damage and actual attack cue");
        s.SetFenceBoxFootprint("box4m", 0, 0, 2, 0.15f, (float)Math.PI / 2);
        Check(footprint.DistanceSquaredToSurface(2.6f, 0) > 1 && footprint.DistanceSquaredToSurface(0, 2.9f) <= 1,
            "A90-degree authored box rotates its thin face and long end correctly");
        s.SetFenceBoxFootprint("box4m", 0, 0, 2, 0.15f, (float)Math.PI / 4);
        float diagonal = 2.6f / (float)Math.Sqrt(2);
        Check(footprint.DistanceSquaredToSurface(-diagonal, diagonal) > 1, "A diagonal authored box uses its real orientation without changing fence direction rules");
        var calm = new PlayableFarmSession(DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout()).Engine);
        calm.RegisterFence("saved-box", -4, -2, true, radius); calm.SetFenceBoxFootprint("saved-box", -4, -2, 2, 0.15f, (float)Math.PI / 4);
        Check(calm.RestoreSave(calm.CaptureSave()).Success && calm.State.Fences.Single().HasBoxFootprint &&
            Near(calm.State.Fences[0].BoxYawRadians, (float)Math.PI / 4) && calm.State.Fences[0].Hp == 300,
            "Footprint metadata and source HP survive an actual demo save roundtrip");
    }
    private static void Main()
    {
        var scenario = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout()); var e = scenario.Engine;
        float stock = e.Farm.FeedStockKg;
        Check(e.Pigs.All(p => p.Hunger == 60), "Regression starts with actual hungry60 fixture, not a hunger0 control");
        foreach (var pig in e.Pigs) pig.FedToday = false;
        e.Tick(60);
        Check(Near(e.Pigs[0].PositionX, -2.6f) && Near(e.Pigs[0].PositionY, 0.4f) && Near(e.Pigs[1].PositionX, 12) && Near(e.Pigs[1].PositionY, 8),
            "Actual hungry first hour cannot teleport toward phantom origin: " + string.Join(";", e.Pigs.Select(p => p.PositionX + "," + p.PositionY)));
        Check(Near(e.Farm.FeedStockKg, stock) && e.Pigs.All(p => p.Hunger == 60 && !p.FedToday && p.LastPaidRationDay == -1),
            "Hourly needs assessment cannot debit phantom feed, satisfy hunger or fabricate ration credit");
        Check(e.Ecosystem.LastHourlyAgentDecisions.Count == 2 && e.Ecosystem.LastHourlyAgentDecisions.All(d => d.ChosenState == PigUtilityBehaviorState.SeekingFeeder) && e.Ecosystem.TotalEmergentInteractionsProcessed > 0,
            "Hourly ecological needs/decisions continue running for the interactive herd");
        var s = new PlayableFarmSession(e); s.RegisterFeedStation(Feeder()); s.SetPlayerPosition(-3, -0.6f);
        string hungrySave = s.CaptureSave();
        var restored = new PlayableFarmSession(new GameEngine(20260601));
        Check(restored.RestoreSave(hungrySave).Success, "Hungry interactive state restores through the actual envelope");
        restored.Engine.Tick(60);
        Check(Near(restored.Engine.Pigs[0].PositionX, -2.6f) && Near(restored.Engine.Pigs[1].PositionX, 12) && Near(restored.Engine.Farm.FeedStockKg, stock) && restored.Engine.Pigs[0].Hunger == 60,
            "Save/reload cannot restore the competing legacy positional/feed owner");
        restored.RegisterFeedStation(Feeder());
        float previousX = restored.Engine.Pigs[1].PositionX, previousY = restored.Engine.Pigs[1].PositionY;
        restored.AdvanceMotion(0.1f, (_, __, ___, ____) => false);
        Check(restored.Engine.Pigs[1].PositionX == previousX && restored.Engine.Pigs[1].PositionY == previousY, "Actual realtime boundary can hold the hungry pig behind an obstacle");
        restored.Engine.Tick(60);
        Check(restored.Engine.Pigs[1].PositionX == previousX && restored.Engine.Pigs[1].PositionY == previousY, "Hourly AI cannot bypass the real motion owner's blocked segment");
        restored.AdvanceMotion(0.1f, (_, __, ___, ____) => true);
        Check(restored.Engine.Pigs[1].PositionX != previousX || restored.Engine.Pigs[1].PositionY != previousY,
            "The actual session still moves a hungry pig toward its configured feeder when traversal is available");

        var manual = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout(true));
        var paid = new PlayableFarmSession(manual.Engine); paid.RegisterFeedStation(Feeder()); paid.SetPlayerPosition(-3, -0.6f);
        float beforePaid = manual.Engine.Farm.FeedStockKg;
        float sourceRations = manual.Engine.Pigs.Sum(p => PigCareCommands.DailyFeedKilograms(manual.Engine, p));
        foreach (var pig in manual.Engine.Pigs) Check(paid.FeedPig(Feeder(), pig.Id).Success, "Manual feed consumes the actual source ration at the actual feeder");
        Check(Near(manual.Engine.Farm.FeedStockKg, beforePaid - sourceRations) && manual.Engine.Pigs.All(p => p.LastPaidRationDay == 1), "One debit records one actual full ration per pig");
        foreach (var pig in manual.Engine.Pigs) pig.Hunger = 60; // Stress the old hourly supplemental-feeding overlap despite durable paid credit.
        var paidReload = new PlayableFarmSession(new GameEngine(20260601));
        Check(paidReload.RestoreSave(paid.CaptureSave()).Success, "Paid ration credit and interactive ownership restore together");
        paidReload.AdvanceSimulationMinutes(60);
        Check(Near(paidReload.Engine.Farm.FeedStockKg, beforePaid - sourceRations) && paidReload.Engine.Pigs.All(p => p.Hunger == 60), "A hungry paid ration cannot be charged again by the first hourly tick");
        paidReload.AdvanceSimulationMinutes(23 * 60);
        Check(paidReload.Engine.Clock.CurrentDay == 2 && paidReload.Engine.Clock.CurrentHour == 6 && paidReload.Engine.LastDailyProcessedFarmDay == 1,
            "Actual farm-day rollup still executes once at06:00");
        Check(Near(paidReload.Engine.Farm.FeedStockKg, beforePaid - sourceRations) && paidReload.Engine.Pigs.All(p => p.FedToday && p.LastPaidRationDay == 1),
            "Manual feeding plus hourly simulation/save/reload/daily rollup cannot double-charge paid feed");
        Check(paidReload.Engine.Pigs.All(p => p.AgeDays == 15), "Daily age/progression is not muted with legacy positional feeding");
        paidReload.Engine.Tick(24 * 60);
        Check(paidReload.Engine.Clock.CurrentDay == 3 && paidReload.Engine.Clock.CurrentHour == 6 &&
            Near(paidReload.Engine.Farm.FeedStockKg, beforePaid - sourceRations) && paidReload.Engine.Pigs.All(p => !p.FedToday && p.Hunger == 35 && p.LastPaidRationDay == 1),
            "The next unpaid06 rollup consumes no warehouse feed and applies the existing unfed/Hunger+35 path");

        var guided = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout());
        var guidedSession = new PlayableFarmSession(guided.Engine); var guidedPig = guided.Engine.Pigs[0];
        guidedPig.PositionX = 25; guidedPig.PositionY = -8; guidedSession.SetPlayerPosition(25, -8);
        Check(guidedSession.StartGuiding(guidedPig.Id).Success, "An actual guided route can be established outside pasture but inside the map");
        guided.Engine.Tick(60);
        Check(guidedPig.PositionX == 25 && guidedPig.PositionY == -8 && guidedSession.FollowingPigIds.Contains(guidedPig.Id),
            "Hourly clamp cannot snap a valid guided pig back into pasture");

        var anomaly = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout()).Engine;
        anomaly.Pigs[0].Id = "pig_white_foot_2018"; anomaly.Pigs[0].PositionX = -17.5f; anomaly.Pigs[0].PositionY = 17.5f;
        anomaly.Pigs[1].PositionX = -18.2f; anomaly.Pigs[1].PositionY = 18.2f; anomaly.Clock.TickMinutes(19 * 60); anomaly.Tick(60);
        Check(anomaly.Clock.CurrentHour == 2 && anomaly.Pigs[0].PositionX == -17.5f && anomaly.Pigs[0].PositionY == 17.5f &&
            anomaly.Pigs[1].PositionX == -18.2f && anomaly.Pigs[1].PositionY == 18.2f && anomaly.CausalChains.HerdLeavesEmptySpotAtJackfruitCorner,
            "Both02 legacy positional branches yield to the interactive owner while anomaly metadata still advances");
        var corpse = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout()).Engine;
        corpse.Pigs[1].Die(); corpse.Pigs[1].PositionX = corpse.Pigs[0].PositionX + 1; corpse.Pigs[1].PositionY = corpse.Pigs[0].PositionY;
        float comfort = corpse.Pigs[0].Comfort; corpse.Tick(60);
        Check(Near(corpse.Pigs[0].PositionX, -2.6f) && Near(corpse.Pigs[0].PositionY, 0.4f) && corpse.Pigs[0].Comfort < comfort &&
            corpse.Ecosystem.LastHourlyAgentDecisions.Single().ChosenState == PigUtilityBehaviorState.AvoidingBiohazardCorpse,
            "Corpse avoidance keeps its decision/comfort effect without an hourly positional jump: XY=" + corpse.Pigs[0].PositionX + "," + corpse.Pigs[0].PositionY +
            " comfort=" + comfort + "→" + corpse.Pigs[0].Comfort + " decisions=" + string.Join(",", corpse.Ecosystem.LastHourlyAgentDecisions.Select(d => d.ChosenState)));
        var dog = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout()).Engine;
        dog.PetsAndNpcs.TryEquipPet(PetId.ChoChanDan); dog.Pigs[0].PositionX = 35; dog.Pigs[0].PositionY = 0; dog.Pigs[0].Hunger = 0;
        dog.Ecosystem.TickHourlyEcologyAndAgentAi(dog);
        Check(dog.Pigs[0].PositionX == 35 && dog.Pigs[0].PositionY == 0 && dog.Ecosystem.LastHourlyAgentDecisions[0].ChosenState == PigUtilityBehaviorState.FlockingWithAlphaLeader,
            "Shepherd needs/decision remains while the external motion owner controls its real path");

        var contaminated = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout(true));
        var sickMeal = new PlayableFarmSession(contaminated.Engine); sickMeal.SetPlayerPosition(-3, -0.6f);
        contaminated.Engine.Pigs[0].ActiveDisease = DiseaseId.DichTaHeo;
        Check(sickMeal.FeedPig(Feeder(), contaminated.Engine.Pigs[0].Id).Success && contaminated.Engine.Ecosystem.ContaminatedTroughsCount == 1,
            "A real successfully consumed GroupA meal owns its single trough-contamination consequence");
        Check(!sickMeal.FeedPig(Feeder(), contaminated.Engine.Pigs[0].Id).Success && contaminated.Engine.Ecosystem.ContaminatedTroughsCount == 1,
            "A rejected duplicate meal cannot contaminate again");
        var contaminationReload = new PlayableFarmSession(new GameEngine(20260601));
        Check(contaminationReload.RestoreSave(sickMeal.CaptureSave()).Success && contaminationReload.Engine.InteractiveHerdOwned &&
            contaminationReload.Engine.Ecosystem.ContaminatedTroughsCount == 1 && contaminationReload.Engine.Pigs[0].LastPaidRationDay == 1,
            "Owner, paid credit and real trough contamination survive save/reload");
        contaminationReload.Engine.Tick(60);
        Check(contaminationReload.Engine.Ecosystem.ContaminatedTroughsCount == 1, "An hourly assessment cannot replay a real meal consequence");

        var legacy = new GameEngine(20260601); var legacyPig = Pig.CreateDefault("legacy-owner", "Legacy", GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 14, initialWeight: 100);
        legacyPig.PositionX = -6; legacyPig.PositionY = -4; legacyPig.Hunger = 60; legacy.Pigs.Add(legacyPig);
        float legacyStock = legacy.Farm.FeedStockKg; legacy.Ecosystem.TickHourlyEcologyAndAgentAi(legacy);
        Check(legacyPig.PositionX != -6 && legacy.Farm.FeedStockKg < legacyStock,
            "An unconfigured legacy engine retains its existing aggregate owner for compatibility");
        var disease = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout()).Engine;
        disease.Pigs[0].ActiveDisease = DiseaseId.NamPhatQuang; disease.Clock.TickMinutes(14 * 60);
        disease.Ecosystem.TickHourlyEcologyAndAgentAi(disease);
        Check(disease.Ecosystem.NamPhatQuangNightMinimapBonusMeters == 8 && disease.Ecosystem.LastHourlyAgentDecisions.Count == 2,
            "Nonmovement disease/night ecology continues under interactive ownership");
        var stormLayout = Layout(); stormLayout.Pigs[0].GeneLine = GeneLineId.LoiMach;
        var storm = DemoScenarioFactory.Create(DemoScenarioKind.Care, stormLayout).Engine;
        var stormData = storm.CreateSaveData(); stormData.CurrentWeather = WeatherType.GiongBao; storm.RestoreFromSave(stormData);
        storm.DeepMechanics.PowerGrid.CapacitorChargeUnits = 0; storm.Tick(60);
        Check(storm.Ecosystem.LoiMachStormCapacitorBonusUnits > 0 && storm.DeepMechanics.PowerGrid.CapacitorChargeUnits > 0 && storm.CurrentWeather == WeatherType.GiongBao,
            "Existing weather/gene/power ecological effects still run under interactive ownership");
        var preRaid = DemoScenarioFactory.Create(DemoScenarioKind.PreRaidFixture, Layout());
        Check(preRaid.Manifest.SimulatedMinutesAdvanced == 35 * 60 && preRaid.Engine.Clock.HoursUntilNextMinorWave == 2 && !preRaid.Engine.Clock.IsMinorWaveTriggered,
            "The actual pre-raid35-hour simulation keeps its naturally scheduled arrival behavior");
        Check(Near(preRaid.Engine.Pigs[0].PositionX, -2.6f) && Near(preRaid.Engine.Pigs[0].PositionY, 0.4f) && Near(preRaid.Engine.Pigs[1].PositionX, 12) && Near(preRaid.Engine.Pigs[1].PositionY, 8),
            "Correct interactive ownership is configured before pre-raid simulation, preserving real fixture positions");
        Check(preRaid.Manifest.InteractiveHerdOwnership && preRaid.Manifest.StrategicSetupAdvanceOnly &&
            preRaid.Manifest.InitialHerd.All(p => p.Hunger == 60) && preRaid.Manifest.FinalHerd.All(p => p.Hunger == 95 && !p.FedToday && p.LastPaidRationDay == -1) &&
            preRaid.Manifest.FinalFeedKg == preRaid.Manifest.InitialFeedKg && preRaid.Engine.Pigs.All(p => p.AgeDays == 15),
            "Strategic35h setup records actual unattended hunger/age/ration flags/stock without restorative resets or phantom meals");
        Check(preRaid.Manifest.FinalHerd.All(p => p.Health == preRaid.Engine.Pigs.Single(actual => actual.Id == p.Id).Health) &&
            preRaid.Manifest.FinalHerd[0].Health < preRaid.Manifest.InitialHerd[0].Health,
            "The unattended fixture retains and records the actual health consequences instead of resetting actors");

        var migrationScenario = DemoScenarioFactory.Create(DemoScenarioKind.Care, Layout());
        var migration = new PlayableFarmSession(migrationScenario.Engine); migration.State.ScenarioProfileId = migrationScenario.ProfileId;
        string modern = migration.CaptureSave(); var old = JsonConvert.DeserializeObject<PlayableFarmSave>(modern)!;
        old.Version = 2; old.Core.PlayableMap!.InteractiveHerdOwnership = false;
        old.Core.FeedStockKg = 0; old.Core.Pigs[0].LastPaidRationDay = 1; old.Presentation.Actions.Add(new FarmActionEvidence { Action = "historical-action", TargetId = old.Core.Pigs[0].Id });
        Check(migration.RestoreSave(JsonConvert.SerializeObject(old)).Success && migration.Engine.InteractiveHerdOwned && migration.Engine.Farm.FeedStockKg == 0 &&
            migration.Engine.Pigs[0].LastPaidRationDay == 1 && migration.State.Actions.Any(a => a.Action == "historical-action"),
            "Known old demo profile/map migrates ownership without refund/re-feed or lost paid history");
        string upgraded = migration.CaptureSave();
        Check(JsonConvert.DeserializeObject<PlayableFarmSave>(upgraded)!.Version == 3 && migration.RestoreSave(upgraded).Success && migration.Engine.Farm.FeedStockKg == 0,
            "Upgraded envelope3 is stable and idempotent");
        var unknownOld = JsonConvert.DeserializeObject<PlayableFarmSave>(modern)!; unknownOld.Version = 2; unknownOld.Presentation.ScenarioProfileId = "";
        Check(!migration.RestoreSave(JsonConvert.SerializeObject(unknownOld)).Success, "Missing old demo identity cannot guess an interactive migration");
        var mismatched = JsonConvert.DeserializeObject<PlayableFarmSave>(modern)!; mismatched.Core.PlayableMap!.InteractiveHerdOwnership = false;
        Check(!migration.RestoreSave(JsonConvert.SerializeObject(mismatched)).Success && migration.Engine.InteractiveHerdOwned,
            "Explicit envelope3 ownership mismatch rejects before mutating the live scene mode");
        Check(!new PlayableMapConfiguration().InteractiveHerdOwnership, "New ownership remains opt-in; missing old Core fields default to legacy");
        foreach (int version in new[] { 1, 2 })
        {
            var oldCore = legacy.CreateSaveData(); oldCore.SaveVersion = version; oldCore.CurrentDay = 2; oldCore.CurrentHour = 2;
            oldCore.FeedStockKg = 0; oldCore.MedicalHerbCount = 0; oldCore.VaccineDoseCount = 0;
            var legacyReload = new GameEngine(20260601); legacyReload.RestoreFromSave(oldCore);
            Check(!legacyReload.InteractiveHerdOwned && legacyReload.Farm.FeedStockKg == 0 && legacyReload.Farm.MedicalHerbCount == 0 &&
                legacyReload.Farm.VaccineDoseCount == 0 && legacyReload.Ecosystem.ContaminatedTroughsCount == 0,
                "Generic Corev" + version + " retains default legacy mode and explicit zero resources without invented contamination history");
        }
        Footprint();
        Console.WriteLine("PASS Herd ownership: " + checks + " assertions");
    }
}

#endif
