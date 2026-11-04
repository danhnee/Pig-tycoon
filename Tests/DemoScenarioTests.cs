#if PLAYABLE_DEMO_SCENARIO_TESTS
using System;
using System.Linq;
using Newtonsoft.Json;
using PigTycoon.Core;
using PigTycoon.PlayableDemo3D;
internal static class DemoScenarioTests
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action, string message)
    { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } catch (InvalidOperationException) { rejected = true; } Check(rejected, message); }
    private static void Main()
    {
        var care = DemoScenarioFactory.Create(DemoScenarioKind.Care);
        Check(care.Engine.Character.Identity == PlayerId.Khoa && care.Engine.Character.Equipment[EquipSlot.Weapon]?.Id == "wpn_dao_rua", "Care opens as Khoa with the declared owned Dao Rựa");
        Check(care.Engine.Pigs.Count > 0 && care.Engine.Pigs.All(p => p.IsAlive) && care.Engine.Pigs.Select(p => p.Id).Distinct().Count() == care.Engine.Pigs.Count,
            "Every initial pig must be a live unique Core ID");
        Check(care.Engine.Pigs.All(p => care.Layout.Map.PastureBounds.Contains(p.PositionX, p.PositionY)), "All pigs including stray start inside declared pasture");
        Check(care.Engine.Farm.CalculateCapacityReport(care.Engine.Pigs.Count, 0).EffectiveCapacity >= care.Engine.Pigs.Count, "Initial capacity must come from actual Core infrastructure");
        Check(care.Engine.Defense.Buildings.Count == 0 && care.Engine.PetsAndNpcs.HiredNpcs.Count == 0 && care.Engine.Inventory.CountId("TenThep") == 0,
            "Ordinary care must not inherit defense fixture inventory or free staff");
        Check(care.Manifest.ScenarioChoicesAreNotCanon && !string.IsNullOrWhiteSpace(care.Manifest.ResourceProvenance), "Manifest must label scenario choices and resource provenance");
        Check(care.Engine.Farm.FarmLevel == 1 && !care.Engine.Defense.UnlockedBlueprints.Contains("NoXuyenVan"), "Care cannot inherit fixture progression or a learned rare Nỏ blueprint");
        var raid = DemoScenarioFactory.Create(DemoScenarioKind.PreRaidFixture);
        Check(!ReferenceEquals(care.Engine, raid.Engine) && care.SaveFileName != raid.SaveFileName && care.ProfileId != raid.ProfileId,
            "Care and pre-raid must have separate engines, save names and progress profiles");
        Check(raid.Engine.Clock.HoursUntilNextMinorWave == 2 && raid.Engine.Clock.MinorWavesRemainingInCycle > 0 && !raid.Engine.Clock.IsDuringCombatWave && !raid.Engine.Clock.IsMinorWaveTriggered,
            "Fixture must stop two real scheduled game hours before arrival without forcing a wave");
        Check(raid.Manifest.SimulatedMinutesAdvanced > 0 && raid.Engine.MinorWavesDefeated == 0 && raid.Engine.MajorWavesSurvived == 0 && raid.Engine.DeepMechanics.BattleReports.Count == 0,
            "Pre-raid clock advance cannot fabricate prior victories");
        var tower = raid.Engine.Defense.Buildings.Single();
        Check(raid.Engine.Farm.FarmLevel >= 3 && raid.Engine.Defense.HallLevel == 1 && raid.Engine.Defense.UnlockedBlueprints.Contains("NoXuyenVan"),
            "The declared defense fixture must satisfy farm-level, hall and learned-blueprint prerequisites");
        Check(raid.Manifest.SetupFarmExp == 0 && raid.Manifest.SetupFarmLevel == 3 && raid.Manifest.SetupLearnedNoBlueprint && raid.Engine.Farm.FarmExp == 0,
            "Declared fixture level must retain zero XP and cannot claim an earned progression history");
        Check(tower.InstanceId == DemoScenarioFactory.OwnedTowerId && tower.BlueprintId == "NoXuyenVan" && tower.AmmoCurrent == 0 && raid.Engine.Inventory.CountId("TenThep") == 50,
            "Fixture owns one empty actual Nỏ and 50 actual arrows, not a fabricated listing");
        Check(!raid.Engine.Economy.NightListings.Any(l => l.OfferedItem.Id == "TenThep"), "Fixture cannot manufacture a market listing");
        var session = new PlayableFarmSession(raid.Engine);
        session.SetPlayerPosition(raid.Layout.DefenseX, raid.Layout.DefenseY);
        var station = new FarmInteractionPoint("fixture-tower", FarmInteractionKind.Defense, raid.Layout.DefenseX, raid.Layout.DefenseY) { CoreInstanceId = tower.InstanceId };
        Check(session.Preload(station).Success && tower.AmmoCurrent == 50 && raid.Engine.Inventory.CountId("TenThep") == 0, "Preload transfers actual owned stock");
        raid.Engine.Tick(120);
        Check(raid.Engine.Clock.IsMinorWaveTriggered, "Minor arrival must follow actual Core clock progression");
        Check(tower.IsPowered && !raid.Engine.DeepMechanics.PowerGrid.IsBreakerTripped,
            "The actual fixture must retain its existing Core base-grid supply after the real warning advance");
        var liveRaid = new PlayableFarmRaid(session) { TowerPosition = id => id == tower.InstanceId ? (raid.Layout.DefenseX, raid.Layout.DefenseY) : null };
        liveRaid.TowerPierceTargets = (_, __) => liveRaid.Enemies.Select(e => e.Model.Id).ToArray();
        var approach = raid.Layout.Map.MonsterEntries[0];
        Check(liveRaid.TryStartScheduledMinor(approach.X, approach.Y, (_, __) => false).Success && raid.Engine.Clock.IsDuringCombatWave,
            "Actual scheduled fixture arrival starts the event-backed live raid and pauses the farm clock");
        int arrivalMinute = raid.Engine.Clock.TotalElapsedGameMinutes;
        session.AdvanceSimulationMinutes(10);
        Check(raid.Engine.Clock.TotalElapsedGameMinutes == arrivalMinute, "Farm time must stay frozen during the actual fixture raid");
        for (int i = 0; i < 600 && liveRaid.Active; i++) liveRaid.Advance(0.5f, (_, __, ___, ____) => true);
        Check(!liveRaid.Active && liveRaid.LastReport != null && liveRaid.LastReport.AmmoSpent > 0 && tower.AmmoCurrent < 50,
            "The unmodified fixture can spend actual preloaded stock and resolve a real event-derived domain raid");
        session.AdvanceSimulationMinutes(1);
        Check(!raid.Engine.Clock.IsDuringCombatWave && raid.Engine.Clock.TotalElapsedGameMinutes == arrivalMinute + 1,
            "Resolving the fixture resumes the existing clock exactly once");
        var careSession = new PlayableFarmSession(care.Engine);
        var manPoint = new FarmInteractionPoint("npc.man", FarmInteractionKind.Dialogue, care.Layout.ManX, care.Layout.ManY);
        int gold = care.Engine.Farm.Gold, habitat = care.Engine.Farm.HabitatIndex, staff = care.Engine.PetsAndNpcs.HiredNpcs.Count;
        float feed = care.Engine.Farm.FeedStockKg;
        careSession.SetPlayerPosition(care.Layout.ManX, care.Layout.ManY);
        var dialogue = PlayableNpcConversation.Talk(careSession, manPoint, "Man");
        Check(dialogue.Success && dialogue.Message.Contains("Mận") && care.Engine.ConversationTopics.IsTopicActive(PlayableNpcConversation.ManCareTopic),
            "Talking to Mận must be a real Core conversation event");
        Check(care.Engine.Farm.Gold == gold && care.Engine.Farm.FeedStockKg == feed && care.Engine.Farm.HabitatIndex == habitat && care.Engine.PetsAndNpcs.HiredNpcs.Count == staff,
            "Free dialogue cannot grant or execute a hired job, stock or habitat buff");
        Check(!PlayableNpcConversation.HasActiveFarmContract(care.Engine, "Man"), "Resident Mận is not automatically an employed farm worker");
        Check(PlayableNpcConversation.EmploymentStatus(care.Engine, "Man") == "Chưa tuyển vào trại" && !dialogue.Message.Contains("Core"),
            "Employment status is separate ordinary UI, with no implementation lecture in dialogue");
        careSession.SetPlayerPosition(care.Layout.ManX + 10, care.Layout.ManY);
        Check(!PlayableNpcConversation.Talk(careSession, manPoint, "Man").Success, "Distant NPC talk must reject");
        careSession.SetPlayerPosition(care.Layout.ManX, care.Layout.ManY);
        Check(!PlayableNpcConversation.Talk(careSession, manPoint, "unknown").Success, "Unknown resident ID cannot create a conversation");
        string save = careSession.CaptureSave(); care.Engine.ConversationTopics.ActiveTopicsDaysRemaining.Clear();
        Check(careSession.RestoreSave(save).Success && care.Engine.ConversationTopics.IsTopicActive(PlayableNpcConversation.ManCareTopic), "Talk state must survive the actual demo save envelope");
        careSession.State.ScenarioProfileId = care.ProfileId;
        var raidSession = new PlayableFarmSession(DemoScenarioFactory.Create(DemoScenarioKind.PreRaidFixture).Engine);
        raidSession.State.ScenarioProfileId = raid.ProfileId;
        raidSession.RequiredTowerInstanceId = raid.TowerInstanceId;
        string raidSave = raidSession.CaptureSave();
        int careGold = care.Engine.Farm.Gold;
        Check(!careSession.RestoreSave(raidSave).Success && care.Engine.Farm.Gold == careGold && care.Engine.Defense.Buildings.Count == 0,
            "A pre-raid save cannot overwrite care progress");
        var corruptedTower = JsonConvert.DeserializeObject<PlayableFarmSave>(raidSave)!;
        corruptedTower.Core.Buildings.Single().InstanceId = "different-owned-no";
        string currentTower = raidSession.Engine.Defense.Buildings.Single().InstanceId;
        Check(!raidSession.RestoreSave(JsonConvert.SerializeObject(corruptedTower)).Success && raidSession.Engine.Defense.Buildings.Single().InstanceId == currentTower,
            "Missing exact fixture tower must reject before mutation; no blueprint-ID fallback");
        Check(raidSession.RestoreSave(raidSave).Success && raidSession.Engine.Defense.Buildings.Single().InstanceId == raid.TowerInstanceId,
            "Repeated restore preserves the exact instance ID without creating another GUID");
        var duplicate = JsonConvert.DeserializeObject<PlayableFarmSave>(raidSave)!;
        duplicate.Core.Buildings.Add(JsonConvert.DeserializeObject<DefenseBuildingInstance>(JsonConvert.SerializeObject(duplicate.Core.Buildings[0]))!);
        Check(!raidSession.RestoreSave(JsonConvert.SerializeObject(duplicate)).Success, "Duplicate exact tower IDs cannot become ambiguous bindings");
        var wrongBlueprint = JsonConvert.DeserializeObject<PlayableFarmSave>(raidSave)!;
        wrongBlueprint.Core.Buildings[0].BlueprintId = "BanChong";
        Check(!raidSession.RestoreSave(JsonConvert.SerializeObject(wrongBlueprint)).Success, "Exact ID cannot refer to a different blueprint");
        var invalid = DemoScenarioFactory.DefaultLayout(); invalid.Pigs[1].Suffix = invalid.Pigs[0].Suffix;
        Reject(() => DemoScenarioFactory.Create(DemoScenarioKind.Care, invalid), "Duplicate live pig IDs must fail scenario setup");
        invalid = DemoScenarioFactory.DefaultLayout(); invalid.Pigs[1].X = 100;
        Reject(() => DemoScenarioFactory.Create(DemoScenarioKind.Care, invalid), "A stray outside the pasture must fail setup");
        invalid = DemoScenarioFactory.DefaultLayout(); invalid.Map.ServicePoints.Clear();
        Reject(() => DemoScenarioFactory.Create(DemoScenarioKind.Care, invalid), "A herd cannot rely on phantom capacity without actual services");
        invalid = DemoScenarioFactory.DefaultLayout(); invalid.Map.ServicePoints.Add(invalid.Map.ServicePoints[0]);
        Reject(() => DemoScenarioFactory.Create(DemoScenarioKind.Care, invalid), "Duplicate station IDs cannot fabricate capacity");
        invalid = DemoScenarioFactory.DefaultLayout(); invalid.Map.MonsterEntries.Clear();
        Reject(() => DemoScenarioFactory.Create(DemoScenarioKind.PreRaidFixture, invalid), "A pre-raid fixture needs actual lawful spawn entries");
        int beforeTalk = care.Engine.Farm.Gold;
        care.Engine.PetsAndNpcs.HiredNpcs.Add(new HiredNpcRecord { Id = NpcSpecialistId.NongCong, Name = "Mận", IsActive = true, DailyWageGold = 40 });
        Check(PlayableNpcConversation.HasActiveFarmContract(care.Engine, "Man") && PlayableNpcConversation.Talk(careSession, manPoint, "Man").Success && care.Engine.Farm.Gold == beforeTalk,
            "Talking to an actually hired Mận only reads contract status and cannot execute a paid shift");
        Check(PlayableNpcConversation.EmploymentStatus(care.Engine, "Man") == "Đang có hợp đồng nông công", "The displayed contract status reflects the actual employment record");
        Console.WriteLine("PASS Demo scenarios/NPC: " + checks + " assertions");
    }
}
#endif
