#if PLAYABLE_DEMO_TESTS
using System;
using System.Linq;
using PigTycoon.Core;
using PigTycoon.PlayableDemo3D;

internal static class PlayableFarmSessionTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static GameEngine Engine()
    {
        var e = new GameEngine(20260601);
        e.SwitchActiveProtagonist(PlayerId.Khoa);
        e.Pigs.Add(Pig.CreateDefault("real-pig", "Heo thật", GeneLineId.HongDien, GeneRarity.Thuong,
            initialAge: 14, initialWeight: 100f));
        return e;
    }
    private static void Main()
    {
        var e = Engine();
        var boundary = Engine();
        int boundaryAge = boundary.Pigs[0].AgeDays;
        boundary.Tick(18 * 60);
        Check(boundary.Clock.CurrentDay == 1 && boundary.Clock.CurrentHour == 0 && boundary.Pigs[0].AgeDays == boundaryAge,
            "Midnight must not age pigs or start the next farmday");
        boundary.Tick(6 * 60);
        Check(boundary.Clock.CurrentDay == 2 && boundary.Clock.CurrentHour == 6 && boundary.Pigs[0].AgeDays == boundaryAge + 1,
            "06:00 must roll the farmday exactly once");
        Check(new GameSaveData().SaveVersion <= SaveSystem.CurrentSaveVersion, "Current writer version must be accepted by loader");
        var s = new PlayableFarmSession(e);
        Check(ReferenceEquals(s.Engine, e), "Session must retain canonical engine");
        Check(s.GetPig("visual-only") == null, "Must not invent visual-only pig state");
        var pig = e.Pigs[0];
        pig.IsIdentified = false;
        s.SetPlayerPosition(0f, 0f);
        Check(!s.InspectPig("missing").Success, "Missing pig must reject");
        pig.PositionX = 50f;
        Check(!s.InspectPig(pig.Id).Success, "Distant pig must reject");
        pig.PositionX = 0f;
        Check(s.InspectPig(pig.Id).Success, "Near actual pig inspect");
        Check(!s.InspectPig(pig.Id).Message.Contains(pig.GeneLine.ToString()), "Hidden gene cannot leak through inspect");

        var market = new FarmInteractionPoint("market", FarmInteractionKind.Market, 8f, 0f);
        int beforeGold = e.Farm.Gold;
        Check(!s.SellPig(market, pig.Id, true).Success && e.Pigs.Count == 1 && e.Farm.Gold == beforeGold,
            "Remote market cannot sell");
        s.SetPlayerPosition(8f, 0f);
        e.Clock.TickMinutes(12 * 60);
        Check(!s.SellPig(market, pig.Id, true).Success && e.Pigs.Count == 1, "Closed market must reject");
        e.Clock.TickMinutes(12 * 60);
        pig.PositionX = market.X; pig.PositionY = market.Y;
        Check(s.SellPig(market, pig.Id, true).Success && e.Pigs.Count == 0 && e.Farm.Gold > beforeGold,
            "Sale must remove actual ID and credit gold");
        int afterSale = e.Farm.Gold;
        Check(!s.SellPig(market, pig.Id, true).Success && e.Farm.Gold == afterSale, "No double sale");

        e = Engine(); s = new PlayableFarmSession(e);
        for (int i = 0; i < 15; i++)
            e.Pigs.Add(Pig.CreateDefault("pig-" + i, "Heo", GeneLineId.HongDien, GeneRarity.Thuong));
        e.Pigs[0].PositionX = 21f;
        Check(s.StartWhistle().Success && s.FollowingPigIds.Count == 12, "Whistle max 12 within 20m");
        Check(!s.FollowingPigIds.Contains("real-pig"), "Out-of-radius pig must not follow");
        Check(!s.StartWhistle().Success, "Whistle cooldown must reject repeated command");
        s.SetPlayerPosition(10f, 0f);
        float priorX = e.Pigs[1].PositionX;
        s.AdvanceMotion(1f, (_, __, ___, ____) => true);
        Check(e.Pigs[1].PositionX > priorX && e.Pigs[1].PositionX <= priorX + MovementSpec.AdultPigWalkMetersPerSecond,
            "Following must be timed motion at Core speed, not teleport");
        s.AdvanceMotion(60f, (_, __, ___, ____) => true);
        Check(s.FollowingPigIds.Count == 0, "Whistle ends after 60 real seconds");
        e.SwitchActiveProtagonist(PlayerId.An);
        e.Clock.TickMinutes(120);
        Check(!s.StartWhistle().Success, "An must not receive Khoa ability");

        e = Engine(); s = new PlayableFarmSession(e);
        s.SetPlayerPosition(3f, 4f);
        s.InspectPig(e.Pigs[0].Id);
        string json = s.CaptureSave();
        e.Farm.Gold = 0; e.Pigs.Clear(); s.SetPlayerPosition(99f, 99f);
        Check(s.RestoreSave(json).Success, "Round-trip restore");
        Check(e.Pigs.Count == 1 && e.Pigs[0].Id == "real-pig" && e.Farm.Gold == 2500,
            "Core IDs and resources restored into same engine");
        Check(s.PlayerX == 3f && s.PlayerY == 4f, "Player position restored");
        Check(!s.RestoreSave("{}").Success, "Reject missing-version envelope");
        Check(!s.RestoreSave("{broken").Success, "Reject malformed save");
        var oldSave = e.CreateSaveData(); oldSave.SaveVersion = 1;
        Check(SaveSystem.TryDeserialize(SaveSystem.Serialize(oldSave), out var migrated, out _) && migrated.SaveVersion == 1,
            "Legacy v1 remains readable");
        migrated.FeedStockKg = 0f; migrated.MedicalHerbCount = 0; migrated.VaccineDoseCount = 2;
        e.RestoreFromSave(migrated);
        Check(e.Farm.FeedStockKg == 0f && e.Farm.MedicalHerbCount == 0 && e.Farm.VaccineDoseCount == 2,
            "Legacy v1 explicit stocks must survive migration without free refill");
        e = Engine(); s = new PlayableFarmSession(e);
        pig = e.Pigs[0]; pig.Hunger = 70;
        float stock = e.Farm.FeedStockKg;
        var care = PigCareCommands.TryFeedPig(e, pig.Id, 0f, 0f);
        Check(care.success && e.Farm.FeedStockKg < stock && pig.Hunger == 0, "Feeding consumes actual stock and relieves hunger");
        float afterFeed = e.Farm.FeedStockKg;
        Check(!PigCareCommands.TryFeedPig(e, pig.Id, 0f, 0f).success && e.Farm.FeedStockKg == afterFeed,
            "Same daily ration cannot be charged twice");
        string rationSave = SaveSystem.Serialize(e.CreateSaveData());
        e.RestoreFromSave(LoadTemporarySave(rationSave));
        Check(!PigCareCommands.TryFeedPig(e, pig.Id, 0f, 0f).success, "Paid ration persists across save/load");
        e.Tick(18 * 60);
        Check(e.Clock.CurrentHour == 0 && e.Clock.CurrentDay == 1, "Manual ration remains in same farmday at midnight");
        e.Tick(6 * 60);
        Check(Math.Abs(e.Farm.FeedStockKg - afterFeed) < 0.001f, "First rollup after manual feed must not double charge");
        e.Tick(24 * 60);
        Check(e.Farm.FeedStockKg < afterFeed, "Later day must require a new ration");
        e = Engine(); e.Farm.FeedStockKg = 0f;
        float stamina = e.Character.Stamina.CurrentStamina;
        Check(!PigCareCommands.TryFeedPig(e, e.Pigs[0].Id, 0f, 0f).success && e.Character.Stamina.CurrentStamina == stamina,
            "Empty stock rejects before stamina debit");
        e.Farm.FeedStockKg = 300f;
        Check(!PigCareCommands.TryFeedPig(e, e.Pigs[0].Id, 50f, 0f).success && e.Farm.FeedStockKg == 300f,
            "Remote feeding cannot debit stock");
        e.Character.Stamina.CurrentStamina = 0f;
        Check(!PigCareCommands.TryFeedPig(e, e.Pigs[0].Id, 0f, 0f).success && e.Farm.FeedStockKg == 300f,
            "Exhaustion cannot debit stock");
        e = Engine(); s = new PlayableFarmSession(e);
        var fence = new FarmInteractionPoint("gap-a", FarmInteractionKind.Construction, 0f, 0f);
        int fenceGold = e.Farm.Gold;
        Check(s.PlaceTemporaryFence(fence).Success && e.Farm.Gold == fenceGold - 30, "Temporary fence must charge source30G");
        Check(!s.PlaceTemporaryFence(fence).Success && e.Farm.Gold == fenceGold - 30, "Fence cannot doublecharge or duplicate");
        var water = new FarmInteractionPoint("water", FarmInteractionKind.Water, 0f, 0f);
        Check(s.ChangeWater(water).Success && !s.ChangeWater(water).Success, "Waterchange durable and once per day");
        var dirty = new PlayableFarmSession(Engine()); dirty.RegisterWaterStation("water");
        var clean = new PlayableFarmSession(Engine()); clean.RegisterWaterStation("water"); clean.ChangeWater(water);
        dirty.AdvanceSimulationMinutes(24 * 60); clean.AdvanceSimulationMinutes(24 * 60);
        Check(clean.Engine.Farm.HabitatIndex == dirty.Engine.Farm.HabitatIndex + 2, "Dirty water must cause realSC−2 at rollup");
        var bed = new FarmInteractionPoint("bed", FarmInteractionKind.Sleep, 0f, 0f);
        e = Engine(); s = new PlayableFarmSession(e);
        int age = e.Pigs[0].AgeDays;
        e.Character.Stamina.DailyLoad = 40f;
        Check(s.Sleep(bed).Success && e.Pigs[0].AgeDays == age + 1 && e.Character.Stamina.DailyLoad == 0f,
            "Sleep must advance realdaily simulation and reset load");
        Check(!s.Sleep(bed).Success, "Sleep must respect cooldown");
        e = Engine(); s = new PlayableFarmSession(e);
        int ammoGold = e.Farm.Gold;
        Check(!s.BuySteelArrows(new FarmInteractionPoint("day", FarmInteractionKind.Market, 0f, 0f), 10).Success,
            "Day market must not sell restrictedammo");
        var night = new FarmInteractionPoint("night", FarmInteractionKind.NightMarket, 0f, 0f);
        Check(!s.BuySteelArrows(night, 10).Success && e.Farm.Gold == ammoGold, "Nightcounterclosed duringday");
        e.Clock.TickMinutes(12 * 60);
        Check(!s.BuySteelArrows(night, 10).Success && e.Farm.Gold == ammoGold, "No guaranteedgoldammo counter bypassing rolled18percentslots");
        var ammoListing = new NightMarketListing { SlotId = "rolled-ammo", OfferedItem = new GameItem { Id = "TenThep", Name = "Tên Thép" },
            Quantity = 10, AcceptsGold = false, GoldPrice = 6 };
        e.Economy.NightListings.Add(ammoListing);
        Check(!s.BuySteelArrows(night, 10).Success, "Barteronlyslot cannot silentlyacceptgold");
        ammoListing.AcceptsGold = true;
        Check(s.BuySteelArrows(night, 10).Success && e.Farm.Gold == ammoGold - 60 && e.Inventory.CountId("TenThep") == 10,
            "Source6G/mui purchased into actualinventory atnight");
        var tower = e.Defense.ConstructBuilding("NoXuyenVan").building!; tower.AmmoCurrent = 40;
        var reload = new FarmInteractionPoint("tower", FarmInteractionKind.Defense, 0f, 0f) { CoreInstanceId = tower.InstanceId };
        e.Clock.IsDuringCombatWave = true;
        Check(!s.Preload(reload).Success && e.Inventory.CountId("TenThep") == 10, "No reload or inventory debit in combat");
        e.Clock.IsDuringCombatWave = false;
        Check(s.Preload(reload).Success && tower.AmmoCurrent == 50 && e.Inventory.CountId("TenThep") == 0,
            "Reload must transferinventory to tower exactly once");
        e = Engine(); s = new PlayableFarmSession(e);
        Check(s.StartGuiding(e.Pigs[0].Id).Success, "Start guiding realpig");
        s.SetPlayerPosition(6f, 0f);
        s.SetNavigationObstacle(new MapRect(2f, -32f, 1f, 34f), true);
        s.AdvanceMotion(20f, (ax, ay, bx, by) => !(bx >= 2f && bx < 3f && by < 2f));
        Check(e.Pigs[0].PositionX > 3f, $"Sharedflowfield must route aroundfence via opengate; got {e.Pigs[0].PositionX},{e.Pigs[0].PositionY}");
        e = Engine(); s = new PlayableFarmSession(e);
        e.Pigs.Add(Pig.CreateDefault("same-point", "Heo", GeneLineId.LamKhe, GeneRarity.Thuong));
        s.StartWhistle(); s.SetPlayerPosition(8f, 0f);
        s.AdvanceMotion(5f, (_, __, ___, ____) => true);
        float overlapX = e.Pigs[0].PositionX - e.Pigs[1].PositionX, overlapY = e.Pigs[0].PositionY - e.Pigs[1].PositionY;
        Check(overlapX * overlapX + overlapY * overlapY > 0.01f, "Coincidentpigs must separate while following");
        var bulk = Engine(); int bulkAge = bulk.Pigs[0].AgeDays; bulk.Tick(3 * 24 * 60);
        Check(bulk.Clock.CurrentDay == 4 && bulk.Pigs[0].AgeDays == bulkAge + 3, "Bulk tick must process every06boundary");
        foreach (int version in new[] { 1, 2 })
        foreach (int hour in new[] { 2, 6, 23 })
        {
            var legacy = Engine().CreateSaveData(); legacy.SaveVersion = version; legacy.CurrentDay = 2;
            legacy.CurrentHour = hour; legacy.CurrentMinute = hour == 23 ? 59 : 0;
            legacy.FeedStockKg = 0; legacy.MedicalHerbCount = 0; legacy.VaccineDoseCount = 0;
            int savedAge = legacy.Pigs[0].AgeDays;
            var restored = new GameEngine(); restored.RestoreFromSave(legacy);
            Check(restored.Clock.CurrentDay == (hour < 6 ? 1 : 2) && restored.Farm.FeedStockKg == 0 &&
                restored.Farm.MedicalHerbCount == 0 && restored.Farm.VaccineDoseCount == 0,
                "Legacy clockmigration preservesphase and explicitzerostocks");
            Check(legacy.SaveVersion == version && legacy.CurrentDay == 2, "Migration cannot mutate originalsave");
            if (hour == 2)
            {
                var resave = restored.CreateSaveData();
                restored.RestoreFromSave(resave); restored.Tick(4 * 60);
                Check(restored.Clock.CurrentDay == 2 && restored.Pigs[0].AgeDays == savedAge,
                    "Legacy midnightrollup checkpoint must survive v3resave and skip duplicate06update");
            }
        }
        var early = Engine().CreateSaveData(); early.SaveVersion = 2; early.CurrentDay = 1; early.CurrentHour = 2;
        e.RestoreFromSave(early);
        Check(e.Clock.CurrentDay == 0 && e.Clock.TotalElapsedGameMinutes == -240, "Prestartlegacyphase must not silentlyjump24h");
        var savedBefore = Engine(); savedBefore.Tick(23 * 60 + 59);
        int beforeAge = savedBefore.Pigs[0].AgeDays; var beforeSix = savedBefore.CreateSaveData();
        savedBefore.RestoreFromSave(beforeSix); savedBefore.Tick(1);
        Check(savedBefore.Clock.CurrentHour == 6 && savedBefore.Pigs[0].AgeDays == beforeAge + 1,
            "05:59 save reload then06 must rollup once");
        var afterSix = savedBefore.CreateSaveData(); savedBefore.RestoreFromSave(afterSix); savedBefore.Tick(1);
        Check(savedBefore.Pigs[0].AgeDays == beforeAge + 1, "06:00 save reload must not duplicate rollup");
        e = Engine(); s = new PlayableFarmSession(e); e.SwitchActiveProtagonist(PlayerId.An);
        string emptyInventorySave = s.CaptureSave(); e.Inventory.AddItem("TenThep", 10);
        s.RestoreSave(emptyInventorySave);
        Check(e.Inventory.CountId("TenThep") == 0 && e.Character.Identity == PlayerId.An && e.Story.ActiveProtagonist == PlayerId.An,
            "Restore must clear emptyinventory and preserve bothAn identityand storyroute");
        e = Engine();
        var map = new PlayableMapConfiguration { ConfigurationId = "test-map", MapX = -60f, MapY = -45f,
            MapWidth = 120f, MapHeight = 90f, PastureX = -20f, PastureY = -14f, PastureWidth = 40f, PastureHeight = 28f };
        map.ServicePoints.Add(new FarmServicePoint { Id = "actual-service", X = 10.25f, Y = 5f, Feed = true, Water = true, Shelter = true });
        e.ConfigurePlayableMap(map);
        e.Pigs[0].PositionX = 10f; e.Pigs[0].PositionY = 5f;
        e.Tick(60);
        Check(e.Pigs[0].PositionX == 10f && e.Pigs[0].PositionY == 5f, "Validconfiguredpasture mustnot snapto legacyx4");
        Check(e.FlowField.Width == 120 && e.FlowField.Height == 90 && e.GridZones.Structures.Count == 0,
            "Configuredmap must matchflowbounds and omitphantomservices");
        Check(e.GridZones.EvaluateServiceCoverage(10.25f, 5f).withinServiceRadius &&
            !e.GridZones.EvaluateServiceCoverage(40.30f, 5f).withinServiceRadius,
            "Core servicequery must match exactauthoredstation notphantomgridcenters");
        var mapSave = e.CreateSaveData(); var mappedRestore = new GameEngine(); mappedRestore.RestoreFromSave(mapSave);
        Check(mappedRestore.PlayableMap != null && mappedRestore.PlayableMap.ConfigurationId == "test-map" &&
            mappedRestore.Pigs[0].PositionX == 10f && mappedRestore.GridZones.Structures.Count == 0,
            "Mapconfiguration and actualpigs/services must persist together");
        bool rejectedMap = false;
        try { e.ConfigurePlayableMap(new PlayableMapConfiguration { MapWidth = float.NaN }); }
        catch (ArgumentException) { rejectedMap = true; }
        Check(rejectedMap, "Invalidmapbounds mustreject");
        e = Engine(); e.ConfigurePlayableMap(map); s = new PlayableFarmSession(e);
        s.State.Fences.Add(new FarmFenceState { Id = "real-fence", X = 20f, Y = 0f });
        var raid = new PlayableFarmRaid(s);
        Check(!raid.TryStartScheduledMinor(-55f, 0f, (_, __) => false).Success, "Raidcannotstartwithoutscheduledarrival");
        e.Clock.IsMinorWaveTriggered = true;
        Check(!raid.TryStartScheduledMinor(-55f, 0f, (_, __) => true).Success, "Raidcannotspawnincamera");
        Check(!raid.TryStartScheduledMinor(1f, 0f, (_, __) => false).Success, "Raidcannotspawnnearplayerorpig");
        Check(raid.TryStartScheduledMinor(-55f, 0f, (_, __) => false).Success && raid.Enemies.Count == 2,
            "Realminorarrival starts documented27threatpair");
        Check(raid.Enemies[0].Model.CurrentHp == 150f && raid.Enemies[1].Model.CurrentHp == 120f,
            "SourceHP150/120mustnotbedayscaledorreplacedbymetadata");
        int waveMinute = e.Clock.TotalElapsedGameMinutes; e.Tick(60);
        Check(e.Clock.TotalElapsedGameMinutes == waveMinute && raid.LastReport == null, "Realcombatpausesclockwithoutinstantoutcome");
        float attackStamina = e.Character.Stamina.CurrentStamina;
        Check(!raid.AttackEnemy(raid.Enemies[0].Model.Id).Success && e.Character.Stamina.CurrentStamina == attackStamina,
            "Outofrangeattackmustnotdebitstamina");
        s.SetPlayerPosition(raid.Enemies[0].X, raid.Enemies[0].Y);
        Check(!raid.AttackEnemy(raid.Enemies[0].Model.Id).Success, "Unarmed character must not receive a free sword");
        e.Character.EquipItem(EquipmentAndTacticalCatalog.AllEquipmentItems.First(item => item.Id == "wpn_dao_rua"));
        Check(!raid.AttackEnemy(raid.Enemies[0].Model.Id, true).Success, "K must not grant a free hammer");
        var pausedEnemy = raid.Enemies[0]; float pausedHp = pausedEnemy.Model.CurrentHp, pausedX = pausedEnemy.X;
        pausedEnemy.Cooldown = 1f; raid.Paused = true;
        raid.Advance(2f, (_, __, ___, ____) => true);
        Check(!raid.AttackEnemy(pausedEnemy.Model.Id).Success && pausedEnemy.Model.CurrentHp == pausedHp &&
            pausedEnemy.X == pausedX && pausedEnemy.Cooldown == 1f, "Pause must freeze enemy movement, HP, attacks and cooldowns");
        raid.Paused = false;
        Check(raid.AttackEnemy(pausedEnemy.Model.Id).Success, "Owned Dao Rựa must strike in range");
        Check(!raid.AttackEnemy(pausedEnemy.Model.Id).Success, "No duplicate strike inside source cooldown");
        raid.Advance(0.49f, (_, __, ___, ____) => false);
        Check(!raid.AttackEnemy(pausedEnemy.Model.Id).Success, "Dao Rựa cannot strike before0.5seconds");
        raid.Advance(0.02f, (_, __, ___, ____) => false);
        Check(raid.AttackEnemy(pausedEnemy.Model.Id).Success, "Dao Rựa becomes ready after source0.5seconds");
        int killGold = e.Farm.Gold, expectedKillGold = raid.Enemies[0].Model.GoldReward + raid.Enemies[1].Model.GoldReward;
        foreach (var raider in raid.Enemies)
        {
            s.SetPlayerPosition(raider.X, raider.Y);
            for (int i = 0; i < 20 && raider.Model.IsAlive; i++)
            { raid.AttackEnemy(raider.Model.Id); raid.Advance(0.6f, (_, __, ___, ____) => false); }
        }
        Check(!raid.Active && !e.Clock.IsDuringCombatWave && raid.LastReport != null && raid.LastReport.EnemiesDefeated == 2 &&
            raid.LastReport.PigsLostOrAbducted == 0 && e.Farm.Gold == killGold + expectedKillGold,
            "Waveoutcome/rewardderivefromactualkills,notaggregateorforcedloss");
        e = Engine(); e.ConfigurePlayableMap(map); s = new PlayableFarmSession(e); raid = new PlayableFarmRaid(s);
        s.State.Fences.Add(new FarmFenceState { Id = "fence-live", X = 20f, Y = 0f });
        e.Clock.IsMinorWaveTriggered = true; raid.TryStartScheduledMinor(-55f, 0f, (_, __) => false);
        var thief = raid.Enemies[0]; var crawler = raid.Enemies[1];
        thief.X = 0f; thief.Y = 0f; s.SetPlayerPosition(10f, 10f);
        raid.Advance(0.1f, (_, __, ___, ____) => true);
        Check(thief.CapturedPigId == "real-pig" && s.ExternallyDrivenPigIds.Contains("real-pig"),
            "ThiefcapturesactualCorepigID and takesmovementownership");
        e.Character.EquipItem(EquipmentAndTacticalCatalog.AllEquipmentItems.First(item => item.Id == "wpn_dao_rua"));
        s.SetPlayerPosition(thief.X, thief.Y); raid.AttackEnemy(thief.Model.Id);
        Check(string.IsNullOrEmpty(thief.CapturedPigId) && !s.ExternallyDrivenPigIds.Contains("real-pig") && e.Pigs.Count == 1,
            "Hittingthief releasesactualpig withoutinventingreplacement");
        crawler.X = 20f; crawler.Y = 0f; s.SetPlayerPosition(10f, 10f);
        raid.Advance(0.1f, (_, __, ___, ____) => false);
        Check(s.State.Fences[0].Hp < 300f, "Crawler damages actualpersisted300HPfence");
        thief.X = e.Pigs[0].PositionX; thief.Y = e.Pigs[0].PositionY;
        raid.Advance(0.1f, (_, __, ___, ____) => false);
        Check(thief.CapturedPigId == "real-pig", "Thiefcanrecaptureafterrelease");
        thief.X = -59.99f;
        raid.Advance(0.1f, (_, __, ___, ____) => true);
        Check(thief.Escaped && e.Pigs.Count == 0, "Onlyactualboundaryescape removespigID exactlyonce");
        raid.Advance(0.2f, (_, __, ___, ____) => true);
        Check(e.Pigs.Count == 0, "Repeatedescapedtick cannotremoveanotherpig");
        e = Engine(); e.ConfigurePlayableMap(map); s = new PlayableFarmSession(e);
        s.RegisterFence("persistent-fence", 20f, 0f, true); s.State.Fences[0].Hp = 247f;
        string roundTrip = s.CaptureSave();
        for (int i = 0; i < 3; i++)
        {
            Check(s.RestoreSave(roundTrip).Success, "Repeatedmappedreloadmustsucceed");
            string next = s.CaptureSave(); Check(next == roundTrip, "Three mappedCore/sidecar roundtripsmustnotdriftonebyte"); roundTrip = next;
        }
        Check(s.State.Fences.Count == 1 && s.State.Fences[0].Hp == 247f, "Damagedfence mustnothealorduplicateonreload");
        e = Engine(); s = new PlayableFarmSession(e);
        e.Character.CurrentGuard = 0f; e.Character.Stamina.CurrentStamina = 0f; e.Character.Stamina.Condition = 0;
        string exhausted = s.CaptureSave();
        e.Character.CurrentGuard = 100f; e.Character.Stamina.CurrentStamina = 100f; e.Character.Stamina.Condition = 100;
        s.RestoreSave(exhausted);
        Check(e.Character.CurrentGuard == 0f && e.Character.Stamina.CurrentStamina == 0f && e.Character.Stamina.Condition == 0,
            "Reloadmustnothealexplicitzeroguard/stamina/condition");
        e = Engine(); e.ConfigurePlayableMap(map); s = new PlayableFarmSession(e);
        var ownedTower = e.Defense.ConstructBuilding("NoXuyenVan").building!;
        raid = new PlayableFarmRaid(s) { TowerPosition = _ => (0f, 0f) };
        raid.TowerPierceTargets = (_, __) => raid.Enemies.Select(enemy => enemy.Model.Id).ToArray();
        e.Clock.IsMinorWaveTriggered = true; raid.TryStartScheduledMinor(-55f, 0f, (_, __) => false);
        s.SetPlayerPosition(30f, 30f);
        foreach (var enemy in raid.Enemies) { enemy.X = -10f; enemy.Y = 0f; }
        raid.Paused = true; raid.Advance(2f, (_, __, ___, ____) => false);
        Check(ownedTower.AmmoCurrent == 50, "Pause must freeze tower shots and ammunition");
        raid.Paused = false; raid.Advance(0.05f, (_, __, ___, ____) => false);
        Check(ownedTower.AmmoCurrent == 49 && raid.Enemies[0].Model.CurrentHp == 110f && raid.Enemies[1].Model.CurrentHp == 80f,
            "One steel arrow must pierce two actual targets for source40 each");
        Check(s.State.TowerWearTenths[ownedTower.InstanceId] == 4, "First shot must retain0.4wear rather than vanish");
        for (int i = 0; i < 5 && raid.Active; i++) raid.Advance(1.3f, (_, __, ___, ____) => false);
        Check(!raid.Active && Math.Abs(raid.ExactTowerWearSpent - 1.6f) < 0.001f && ownedTower.Durability == 99 &&
            s.State.TowerWearTenths[ownedTower.InstanceId] == 6, "Short wave must retain/report every fractionalwear unit");
        s.SetPlayerPosition(0f, 0f);
        var ownedTowerPoint = new FarmInteractionPoint("owned-tower", FarmInteractionKind.Defense, 0f, 0f) { CoreInstanceId = ownedTower.InstanceId };
        Check(s.CollectRecoveredArrows(ownedTowerPoint).Success && e.Inventory.CountId("TenThep") == 1 &&
            s.State.RecoverableArrowTenths == 2, "Four spent arrows recover30percent with persisted fractional carry");
        Check(!s.CollectRecoveredArrows(ownedTowerPoint).Success, "Recovery cannot duplicate collected arrows");
        var again = new PlayableFarmRaid(s) { TowerPosition = _ => (0f, 0f) };
        again.TowerPierceTargets = (_, __) => again.Enemies.Select(enemy => enemy.Model.Id).ToArray();
        e.Clock.IsMinorWaveTriggered = true; again.TryStartScheduledMinor(-55f, 0f, (_, __) => false);
        s.SetPlayerPosition(30f, 30f);
        foreach (var enemy in again.Enemies) { enemy.X = -10f; enemy.Y = 0f; enemy.Model.CurrentHp = 40f; }
        again.Advance(0.05f, (_, __, ___, ____) => false);
        Check(!again.Active && Math.Abs(again.ExactTowerWearSpent - 0.4f) < 0.001f && ownedTower.Durability == 98 &&
            s.State.TowerWearTenths[ownedTower.InstanceId] == 0, "Wear carry must survive raid boundaries, including single-shotwaves");
        e = Engine(); s = new PlayableFarmSession(e);
        e.Character.EquipItem(EquipmentAndTacticalCatalog.AllEquipmentItems.First(item => item.Id == "wpn_dao_rua"));
        string gearSave = s.CaptureSave(); e.Character.Equipment[EquipSlot.Weapon] = null; e.Character.RecalculateBonusStats();
        Check(s.RestoreSave(gearSave).Success && e.Character.Equipment[EquipSlot.Weapon]?.Id == "wpn_dao_rua" &&
            e.Character.BaseStats.Str == 24 && e.Character.BonusStats.Str == 2, "Owned weapon and additive bonuses must survive reload");
        Console.WriteLine("PASS PlayableDemo3D: " + checks + " assertions");
    }
    private static GameSaveData LoadTemporarySave(string json)
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pig-demo-ration-" + Guid.NewGuid() + ".json");
        System.IO.File.WriteAllText(path, json);
        try { return SaveSystem.Load(path); }
        finally { System.IO.File.Delete(path); }
    }
}

#endif
