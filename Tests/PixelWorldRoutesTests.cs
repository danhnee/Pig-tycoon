#if PIXEL_WORLD_TESTS
using PigTycoon.Core;

static class PixelWorldRoutesTests
{
    static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    /// <summary>4 Nov: walk-in interiors (Stardew) are rooms behind a door, not a fifth map.</summary>
    static void VerifyInteriors()
    {
        Require(PixelWorldRoutes.Maps.Count == 4, "interiors do not add main maps");
        var house = PixelWorldInteriors.ForBuilding("main_house");
        Require(house != null && house.ParentLocationId == "Farm", "main house opens into a room of the farm");
        Require(house!.DoorPortalId == "Farm->Interior_NhaChinh" && house.ExitPortalId == "Interior_NhaChinh->Farm", "portal ids pair up");
        Require(PixelWorldRoutes.ForLocation("Interior_NhaChinh").SceneName == "Interior_NhaChinh", "interior scene binding");
        Require(PixelWorldRoutes.ForLocation("FarmHouse").LocationId == "Farm", "old saves at 'FarmHouse' still load the farm");
        Require(PixelWorldRoutes.CanTravel("Farm", "Interior_NhaChinh") && PixelWorldRoutes.CanTravel("Interior_NhaChinh", "Farm"), "door both ways");
        Require(!PixelWorldRoutes.CanTravel("Interior_NhaChinh", "MarketTown"), "a room only opens onto its own map");
        Require(!PixelWorldRoutes.CanTravel("MarketTown", "Interior_QuanBaSau") && PixelWorldRoutes.CanTravel("Hub_Village", "Interior_QuanBaSau"), "Bà Sáu's eatery is in the village");

        var e = new GameEngine();
        PixelWorldRoutes.Register(e.WorldLocations);
        e.WorldLocations.CurrentLocationId = "Farm";
        var gold = e.Farm.Gold;
        var inside = e.WorldLocations.TryWarpPlayer("Interior_NhaChinh", 5, 3, e);
        Require(inside.warped, inside.message);
        Require(e.WorldLocations.GetLocation("Interior_NhaChinh")!.IsOutdoors == false, "interior is indoors");
        var saved = SaveSystem.Serialize(e.CreateSaveData());
        Require(SaveSystem.TryDeserialize(saved, out var data, out var err), err);
        var loaded = new GameEngine(); loaded.RestoreFromSave(data!);
        Require(loaded.WorldLocations.CurrentLocationId == "Interior_NhaChinh", "saving inside the house reloads inside");
        Require(!e.WorldLocations.TryWarpPlayer("MarketTown", 0, 0, e).warped, "no shortcut from the house to the market");
        var back = e.WorldLocations.TryWarpPlayer("Farm", 40, 30, e);
        Require(back.warped && e.Farm.Gold == gold, "out again, state unchanged");
    }

    static int Main()
    {
        // Latest user contract supersedes the old five-map / permanent forest route.
        Require(PixelWorldRoutes.Maps.Count == 4, "exactly four MAIN maps; service rooms are not a fifth map");
        Require(PixelWorldRoutes.ForLocation("Farm_Core").LocationId == "Farm", "service core belongs to pasture");
        Require(PixelWorldRoutes.ForLocation("BoundaryEdge").LocationId == "Farm", "ordinary fence boundary is not forest access");
        var locked = new GameEngine();
        PixelWorldRoutes.Register(locked.WorldLocations);
        Require(!locked.WorldLocations.TryWarpPlayer("Forest_Primeval", 0, 0, locked).warped, "no forest warp outside an active encounter");
        Require(!locked.WorldLocations.TryWarpPlayer("Observatory", 0, 0, locked).warped, "existing room progression gate remains enforced");
        VerifyInteriors();
        VerifyOccultDirector();
        VerifyOccultExpeditionRoutes();
        VerifyVillageTransactions();
        VerifyDefenseBuildTransactions();
        PlayerRestTests.Run();
        DefenseRepairTests.Run();
        var engine = new GameEngine();
        PixelWorldRoutes.Register(engine.WorldLocations);
        Require(PixelWorldRoutes.Maps.Count == 4, "four main maps");
        foreach (var map in PixelWorldRoutes.Maps)
            Require(engine.WorldLocations.GetLocation(map.LocationId) != null, map.LocationId);
        var pigs = engine.Pigs;
        var inventory = engine.Inventory;
        var gold = engine.Farm.Gold;
        var day = engine.Clock.CurrentDay;
        foreach (var id in new[] { "Hub_Village", "MarketTown", "Farm" })
        {
            Require(PixelWorldRoutes.CanTravel(engine.WorldLocations.CurrentLocationId, id), "route " + id);
            var result = engine.WorldLocations.TryWarpPlayer(id, 0, 0, engine);
            Require(result.warped, result.message);
            Require(ReferenceEquals(pigs, engine.Pigs) && ReferenceEquals(inventory, engine.Inventory), "same session");
            Require(engine.Farm.Gold == gold && engine.Clock.CurrentDay == day, "travel must not reset state");
            var saved = SaveSystem.Serialize(engine.CreateSaveData());
            Require(SaveSystem.TryDeserialize(saved, out var data, out var error), error);
            var restored = new GameEngine();
            PixelWorldRoutes.Register(restored.WorldLocations);
            restored.RestoreFromSave(data!);
            Require(restored.WorldLocations.CurrentLocationId == id, "save map " + id);
        }
        Require(!PixelWorldRoutes.CanTravel("Forest_Primeval", "MarketTown"), "reject disconnected route");
        Require(!PixelWorldRoutes.CanTravel("Farm", "missing"), "reject missing map");
        Require(PixelWorldRoutes.ForLocation("Farm_Main").LocationId == "Farm", "scene aliases migrate");
        Require(PixelWorldRoutes.ForLocation("BoRiaVillage2D").LocationId == "Hub_Village", "village alias");
        Require(PixelWorldRoutes.ForLocation("FarmHouse").LocationId == "Farm", "legacy house save binding");
        Require(PixelWorldRoutes.ForLocation("BoundaryEdge").LocationId == "Farm", "legacy boundary save binding");
        PixelWorldRoutes.Register(engine.WorldLocations);
        Require(engine.WorldLocations.GetLocation("Farm")!.Warps.Count(w => w.TargetLocationId == "Farm_Core") == 1, "idempotent register");
        bool refused = false;
        try { engine.WorldLocations.RegisterLocation(new GameLocation { LocationId = "Farm" }); }
        catch (ArgumentException) { refused = true; }
        Require(refused, "duplicate registration cannot replace farm state");
        Console.WriteLine("Pixel world route and save integration assertions passed.");
        return 0;
    }

    static void VerifyOccultDirector()
    {
        int SeedFor(double low, double high)
        {
            for (int seed = 0; seed < 100000; seed++)
            {
                var random = new Random(unchecked(seed ^ 16777619));
                if (random.NextDouble() >= .05) continue;
                double color = random.NextDouble();
                if (color >= low && color < high) return seed;
            }
            throw new Exception("No deterministic test fixture seed");
        }
        // Fixture begins before the first day-start roll; production already rolls in the constructor.
        GameEngine BeforeFirstRoll(int seed)
        { var e = new GameEngine(seed); e.ForestAndEvents.Expedition.Restore(null); return e; }
        var gate = BeforeFirstRoll(SeedFor(.45, .85));
        gate.Farm.EventPressureIndex = 100;
        gate.Pigs.Clear();
        var unknown = Pig.CreateDefault("unknown", "unknown", GeneLineId.HongDien, GeneRarity.Quy);
        unknown.IsIdentified = false; gate.Pigs.Add(unknown);
        Require(gate.ForestAndEvents.Director.TryOfferDay(gate).offered, "real eligible daily event offers gate");
        Require(!gate.ForestAndEvents.Expedition.IsActive && gate.ForestAndEvents.Director.HasOpenGate(gate), "offer does not auto-enter portal");
        var saved = gate.ForestAndEvents.Expedition.CreateSaveData();
        gate.ForestAndEvents.Expedition.Restore(saved);
        Require(!gate.ForestAndEvents.Director.TryOfferDay(gate).offered, "save cannot reroll daily event");
        gate.Clock.TickMinutes(359);
        Require(gate.ForestAndEvents.Director.HasOpenGate(gate), "gate remains until six hours");
        gate.Clock.TickMinutes(1);
        Require(!gate.ForestAndEvents.Director.HasOpenGate(gate) && !gate.ForestAndEvents.Director.TryEnterOfferedGate(gate), "gate closes at exactly six hours");
        var fog = BeforeFirstRoll(SeedFor(0, .45));
        fog.Farm.EventPressureIndex = 70;
        Require(fog.ForestAndEvents.Director.TryOfferDay(fog).offered && fog.ForestAndEvents.Expedition.IsActive && !fog.ForestAndEvents.Expedition.IsGateEvent, "qualified fog starts real session");
        var green = BeforeFirstRoll(SeedFor(.85, 1)); green.Farm.EventPressureIndex = 100;
        Require(!green.ForestAndEvents.Director.TryOfferDay(green).offered && !green.ForestAndEvents.Expedition.IsActive, "missing green eligibility seeds never become invented conditions");
        var locked = new GameEngine(SeedFor(.45, .85)); locked.Farm.EventPressureIndex = 100; locked.Pigs.Clear();
        Require(!locked.ForestAndEvents.Director.TryOfferDay(locked).offered, "no unqualified yellow event");
    }

    static void VerifyOccultExpeditionRoutes()
    {
        var engine = new GameEngine();
        PixelWorldRoutes.Register(engine.WorldLocations);
        var session = engine.ForestAndEvents.Expedition;
        Require(!PixelWorldRoutes.CanTravel("Farm", "Forest_Primeval"), "no ordinary forest road");
        Require(engine.WorldLocations.TryWarpPlayer("Barn_Quarantine", 0, 0, engine).warped, "authored interior remains reachable");
        engine.WorldLocations.CurrentLocationId = "Hub_Village";
        Require(!engine.WorldLocations.TryWarpPlayer("GeneLab", 0, 0, engine).warped, "cannot bypass adjacency into any known room");
        engine.WorldLocations.CurrentLocationId = "Farm";
        Require(!session.TryBegin(MysteryEventId.E10_DotSanTrongSuong, 1), "defense fog is not an expedition");
        Require(!session.TryBegin(MysteryEventId.E11_CanQuetUfo, 1), "air raid is not an expedition");
        Require(session.TryBegin(MysteryEventId.E01_VuonMuoiHaiQua, 1), "qualified portal encounter can start");
        Require(!session.TryBegin(MysteryEventId.E02_DongCoKhongBong, 1), "active encounter cannot be replaced");
        Require(engine.WorldLocations.TryWarpPlayer("Forest_Primeval", 0, 0, engine).warped, "active portal reaches forest");
        Require(!engine.WorldLocations.TryWarpPlayer("Farm", 0, 0, engine).warped, "no free exit before completion");
        Require(!session.RecordProgress(float.NaN) && !session.RecordProgress(101), "reject corrupt progress");
        Require(session.RecordProgress(50), "actual task completion updates encounter");
        Require(!session.EvaluateExit(1, engine.SpaceBag, "BuaHoiHuong").canExit, "cannot claim unowned exit item");
        engine.Inventory.AddItem("BuaHoiHuong");
        Require(engine.SpaceBag.TryWithdrawOne(engine.Inventory, engine.Inventory.Slots.ToList().FindIndex(i => i.ItemId == "BuaHoiHuong"), out _), "carry actual exit charm");
        Require(engine.WorldLocations.TryWarpPlayer("Farm", 0, 0, engine, "BuaHoiHuong").warped, "owned charm permits gate exit at fifty percent");
        Require(engine.SpaceBag.Count == 0 && !session.IsActive, "exit consumes item once and closes session");
        Require(!engine.WorldLocations.TryWarpPlayer("Forest_Primeval", 0, 0, engine).warped, "completed encounter does not permanently unlock forest");
        Require(session.TryBegin(MysteryEventId.E02_DongCoKhongBong, 2), "fog encounter begins");
        Require(engine.WorldLocations.TryWarpPlayer("Forest_Primeval", 0, 0, engine).warped, "fog reaches forest edge");
        engine.Inventory.AddItem("BuaHoiHuong");
        Require(engine.SpaceBag.TryWithdrawOne(engine.Inventory, engine.Inventory.Slots.ToList().FindIndex(i => i.ItemId == "BuaHoiHuong"), out _), "carry actual exit charm");
        Require(!engine.WorldLocations.TryWarpPlayer("Farm", 0, 0, engine, "BuaHoiHuong").warped && engine.SpaceBag.Count == 1, "charm cannot bypass fog and is not consumed");
        engine.Inventory.AddItem("LaBanTanVu");
        Require(!session.EvaluateExit(3, engine.SpaceBag, "LaBanTanVu").canExit, "remote warehouse cannot supply forest exit");
        Require(engine.SpaceBag.TryWithdrawOne(engine.Inventory, engine.Inventory.Slots.ToList().FindIndex(i => i.ItemId == "LaBanTanVu"), out _), "carry compass");
        Require(!session.EvaluateExit(2, engine.SpaceBag, "LaBanTanVu").canExit, "compass locked before one day");
        Require(session.EvaluateExit(3, engine.SpaceBag, "LaBanTanVu").rewardPercentTier == 30, "compass gives thirty percent after one day");
        var bad = session.CreateSaveData(); bad.CompletionPercent = float.NaN;
        Require(!OccultExpeditionSession.IsValidSnapshot(bad, out _), "corrupt active state rejected without reset");
        var clone = session.CreateSaveData();
        clone.CompletionPercent = 100;
        Require(session.CompletionPercent == 0, "save DTO cannot mutate live encounter");
        var restored = new OccultExpeditionSession();
        restored.Restore(session.CreateSaveData());
        Require(restored.IsActive && !restored.IsGateEvent && restored.EvaluateExit(5, engine.SpaceBag).canExit, "saved elapsed day enforces three-day exit");
        Require(!restored.RecordProgress(-1), "cannot reduce progress");
        Require(session.RecordProgress(100), "complete encounter");
        Require(engine.WorldLocations.TryWarpPlayer("Farm", 0, 0, engine).warped && engine.SpaceBag.Count == 2, "completion exits without consuming unrelated item");
    }

    static void VerifyVillageTransactions()
    {
        var engine = new GameEngine();
        engine.Farm.Gold = 5000;
        var listing = engine.Village.GetVillager(VillagerId.BaSau)!.Listings[0];
        int price = listing.GoldPrice;
        string reward = listing.OfferedItem.Id;
        var first = engine.TradeWithVillager(VillagerId.BaSau, listing.SlotId, null, null, true);
        Require(first.success && engine.Farm.Gold == 5000 - price, "one canonical gold debit");
        Require(engine.Inventory.CountId(reward) == 1, "gold purchase actually grants item");
        Require(engine.Ledger.Transactions.Count == 1 && engine.Ledger.Transactions[0].AmountGold == price, "one ledger entry");
        var again = engine.TradeWithVillager(VillagerId.BaSau, listing.SlotId, null, null, true);
        Require(!again.success && engine.Inventory.CountId(reward) == 1 && engine.Farm.Gold == 5000 - price, "cannot buy sold stock twice");
        Require(SaveSystem.TryDeserialize(SaveSystem.Serialize(engine.CreateSaveData()), out var saved, out var error), error);
        var restored = new GameEngine();
        restored.RestoreFromSave(saved!);
        Require(!restored.TradeWithVillager(VillagerId.BaSau, listing.SlotId, null, null, true).success, "save/load cannot replenish sold stock");
        Require(restored.Inventory.CountId(reward) == 1 && restored.Ledger.Transactions.Count == 1, "save keeps actual inventory and ledger");

        QualifiedItemRegistry.Register("O", "test_trade_scrap", "Test scrap", "test fixture", "Material", 125, 0);
        engine.Inventory.AddItem("test_trade_scrap", 2);
        var spoof = new GameItem { Id = "test_trade_scrap", Quantity = 1, ReferenceValueGold = 999999, Category = "Material" };
        Require(!engine.TradeWithVillager(VillagerId.TamVa, "tamva_coc", new() { spoof }, null, false).success, "caller cannot spoof GTTC");
        var doubleOffer = new GameItem { Id = "test_trade_scrap", Quantity = 2 };
        Require(!engine.TradeWithVillager(VillagerId.TamVa, "tamva_coc", new() { doubleOffer, doubleOffer }, null, false).success, "aggregate duplicate offers before checking ownership");
        Require(engine.Inventory.CountId("test_trade_scrap") == 2, "failed barter preserves payment");
        var barter = engine.TradeWithVillager(VillagerId.TamVa, "tamva_coc", new() { doubleOffer }, null, false);
        Require(barter.success && engine.Inventory.CountId("test_trade_scrap") == 0, "barter consumes real inventory");
        Require(engine.Inventory.CountId("CocRaoMoi") == 1 && engine.Farm.Gold == 5000 - price, "barter grants reward without gold charge");
        Require(!engine.TradeWithVillager(VillagerId.TamVa, "tamva_coc", new() { doubleOffer }, null, false).success, "cannot barter same stock twice");
        Require(!engine.TradeWithVillager(VillagerId.BiNhan, "binhan_phale", null, null, true).success, "mystic merchant never takes gold");
        Require(!engine.TradeWithVillager(VillagerId.AnhCa, "anhca_daocan", null, "invented-pig", false).success, "cannot trade another owner's pig");
        engine.Village.RefreshStockForDay(engine.Clock.CurrentDay + 1);
        Require(!engine.Village.GetVillager(VillagerId.TamVa)!.Listings[1].IsSoldOut, "new day replenishes vendor stock");
    }

    static void VerifyDefenseBuildTransactions()
    {
        var engine = new GameEngine();
        engine.Farm.Gold = 10000;
        var blueprint = engine.Defense.GetBlueprint("BanChong")!;
        int gold = engine.Farm.Gold;
        engine.Defense.HallLevel = 0;
        Require(!engine.Defense.TryConstructBuilding("BanChong", engine.Farm, engine.Ledger).success, "locked hall rejects construction");
        Require(engine.Farm.Gold == gold && engine.Ledger.Transactions.Count == 0 && engine.Defense.Buildings.Count == 0, "failed build charges nothing");
        engine.Defense.HallLevel = 1;
        var built = engine.Defense.TryConstructBuilding("BanChong", engine.Farm, engine.Ledger);
        Require(built.success && engine.Farm.Gold == gold - blueprint.BuildCostGold, "canonical build cost charged once");
        Require(engine.Ledger.Transactions.Count == 1, "build uses canonical ledger");
        Require(built.building!.Durability == SpikeTrapSpec.Charges && built.building.AmmoCapacity == 6, "GDD17 six trap activations");
        Require(SpikeTrapSpec.Damage == 60 && Math.Abs(SpikeTrapSpec.SlowMultiplier - .8f) < .001f && SpikeTrapSpec.SlowSeconds == 2, "GDD17 trap damage and slow");
        engine.Farm.Gold = 0;
        int count = engine.Defense.Buildings.Count;
        Require(!engine.Defense.TryConstructBuilding("BanChong", engine.Farm, engine.Ledger).success && engine.Defense.Buildings.Count == count, "unaffordable build creates nothing");
    }
}
#endif
