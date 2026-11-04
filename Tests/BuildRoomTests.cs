using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>
/// GDD 13.2 + 11.1 (story Q06): Phòng Xây Dựng needs an NPC on duty; efficiency 40/70/100%, real time = base / efficiency,
/// build slots 1 / 1(+1) / 2(+1), structures finish after the game time, nobody on duty = the room stops.
/// HANDBOOK P1.02/P1.21: the new-game An/Khoa choice.
/// </summary>
internal static class BuildRoomTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void RunDutyAndEfficiency()
    {
        var e = new GameEngine(seed: 21);
        Check(ConstructionHall.OnDutyNpc(e) == null && ConstructionHall.EfficiencyPercent(e) == 0, "nobody on duty at start (story 1.16)");
        Check(ConstructionHall.BuildSlots(e) == 0, "no NPC on duty: no build slot");

        e.PetsAndNpcs.HireNpc(NpcSpecialistId.VeBinhKhien);
        Check(ConstructionHall.OnDutyNpc(e) == null, "a mercenary never staffs a farm room (GDD 11)");

        e.PetsAndNpcs.HireNpc(NpcSpecialistId.NongCong);
        Check(ConstructionHall.EfficiencyPercent(e) == 40, "another farm NPC: 40%");

        e.PetsAndNpcs.HireNpc(NpcSpecialistId.KySuXayDung);
        Check(ConstructionHall.OnDutyNpc(e)!.Id == NpcSpecialistId.KySuXayDung, "Kỹ Sư is picked first");
        Check(ConstructionHall.EfficiencyPercent(e) == 70, "Kỹ Sư without trait: 70%");
        e.PetsAndNpcs.HiredNpcs.First(n => n.Id == NpcSpecialistId.KySuXayDung).TraitUnlocked = true;
        Check(ConstructionHall.EfficiencyPercent(e) == 100, "Kỹ Sư with trait: 100%");

        // GDD example: Skyhook 8 h → 20 h / 11.4 h / 8 h
        int b = ConstructionHall.BaseBuildMinutes("ThapPhongKhongSkyhook");
        Check(b == 480, "Skyhook base 8 h");
        Check(ConstructionHall.RealMinutes(b, 40) == 1200 && ConstructionHall.RealMinutes(b, 70) == 686 &&
              ConstructionHall.RealMinutes(b, 100) == 480, "real time = base / efficiency");
        Check(ConstructionHall.RealMinutes(b, 0) == -1, "no efficiency, no time");

        Check(ConstructionHall.BuildSlots(1, false) == 1 && ConstructionHall.BuildSlots(1, true) == 1, "hall 1: 1 slot");
        Check(ConstructionHall.BuildSlots(2, false) == 1 && ConstructionHall.BuildSlots(2, true) == 2, "hall 2: 1 (+1)");
        Check(ConstructionHall.BuildSlots(3, false) == 2 && ConstructionHall.BuildSlots(3, true) == 3, "hall 3: 2 (+1)");
        e.Defense.HallLevel = 3;
        Check(ConstructionHall.BuildSlots(e) == 3, "trait Kỹ Sư on duty at hall 3: 3 slots");
        e.PetsAndNpcs.HiredNpcs.First(n => n.Id == NpcSpecialistId.KySuXayDung).IsActive = false;
        Check(ConstructionHall.BuildSlots(e) == 2 && ConstructionHall.EfficiencyPercent(e) == 40, "Kỹ Sư away: Nông Công takes over");
    }

    public static void RunConstructionQueue()
    {
        var e = new GameEngine(seed: 22);
        e.Farm.Gold = 50000;
        int gold = e.Farm.Gold;
        var start = ConstructionQueue.TryStart(e, "RaoGo", 7f, 9f, true);
        Check(!start.success && e.Farm.Gold == gold && e.Defense.ConstructionOrders.Count == 0, "no NPC on duty: nothing starts, no gold taken");

        e.PetsAndNpcs.HireNpc(NpcSpecialistId.NongCong);
        int cost = e.Defense.GetBlueprint("RaoGo")!.BuildCostGold;
        start = ConstructionQueue.TryStart(e, "RaoGo", 7f, 9f, true);
        Check(start.success && e.Farm.Gold == gold - cost, "order paid once when it starts");
        Check(e.Defense.Buildings.Count == 0, "the structure does not appear at once");
        Check(!ConstructionQueue.TryStart(e, "BanChong").success, "hall 1 has one build slot");
        Check(ConstructionQueue.QuoteMinutes(e, "RaoGo") == 150, "Thường 60 min at 40% = 150 min");

        ConstructionQueue.Advance(e, 149);
        Check(e.Defense.Buildings.Count == 0 && e.Defense.ConstructionOrders[0].Progress > 0.99f, "149 of 150 minutes: still đang thi công");

        // the room stops while nobody is on duty
        e.PetsAndNpcs.HiredNpcs[0].IsActive = false;
        ConstructionQueue.Advance(e, 600);
        Check(e.Defense.Buildings.Count == 0 && ConstructionQueue.RemainingMinutes(e, e.Defense.ConstructionOrders[0]) == -1, "paused without NPC");
        e.PetsAndNpcs.HiredNpcs[0].IsActive = true;

        // save / load mid-construction keeps the order and its progress
        var json = SaveSystem.Serialize(e.CreateSaveData());
        Check(SaveSystem.TryDeserialize(json, out var data, out _), "save with an order");
        var r = new GameEngine(seed: 22);
        r.RestoreFromSave(data!);
        Check(r.Defense.ConstructionOrders.Count == 1 && r.Defense.ConstructionOrders[0].WorkDoneCenti == e.Defense.ConstructionOrders[0].WorkDoneCenti,
            "order and progress survive save/load");

        var done = ConstructionQueue.Advance(r, 1);
        var built = r.Defense.Buildings.SingleOrDefault(x => x.BlueprintId == "RaoGo");
        Check(done.Count == 1 && built != null && built.HasPosition && built.PosX == 7f && built.PosY == 9f, "finished at its placed cell");
        Check(r.Defense.ConstructionOrders.Count == 0, "the slot is free again");

        // in-progress orders count against the caps (hall 1: Nỏ 2)
        r.Defense.ConstructBuilding("NoXuyenVan"); // the layout's starter tower
        Check(!ConstructionQueue.TryStart(r, "NoXuyenVan").success, "GDD 13.1: the Tinh Nỏ blueprint must be bought before building another");
        r.Defense.UnlockedBlueprints.Add("NoXuyenVan"); // bought at the night market
        Check(ConstructionQueue.TryStart(r, "NoXuyenVan").success, "second Nỏ starts");
        r.Defense.HallLevel = 3; // more slots, Nỏ cap 4
        Check(r.Defense.CheckConstructionRules("BanChong", r.Defense.PendingBlueprintIds()).ok, "unrelated blueprint still allowed");
        r.Defense.HallLevel = 1;
        Check(!r.Defense.CheckConstructionRules("NoXuyenVan", r.Defense.PendingBlueprintIds()).ok, "pending Nỏ counts toward hall-1 cap of 2");

        // GameEngine.Tick drives the queue
        var t = new GameEngine(seed: 23);
        t.Farm.Gold = 50000;
        t.PetsAndNpcs.HireNpc(NpcSpecialistId.KySuXayDung);
        t.PetsAndNpcs.HiredNpcs[0].TraitUnlocked = true;
        Check(ConstructionQueue.TryStart(t, "BanChong").success, "Kỹ Sư starts a trap");
        t.Tick(30);
        t.Tick(30);
        Check(t.Clock.IsDuringCombatWave || t.Defense.Buildings.Any(x => x.BlueprintId == "BanChong"), "60 game minutes at 100% builds a Thường structure");

        // old saves without the field load with an empty queue
        var old = e.CreateSaveData();
        old.ConstructionOrders = null!;
        var o = new GameEngine(seed: 22);
        o.RestoreFromSave(old);
        Check(o.Defense.ConstructionOrders.Count == 0, "old save: empty queue");
    }

    public static void RunRepairNeedsDutyAndQ06()
    {
        var e = new GameEngine(seed: 24);
        foreach (var id in new[] { "Q01", "Q02", "Q03", "Q04", "Q05" }) e.Quests.Completed.Add(id);
        e.Quests.Evaluate(e);
        Check(e.Quests.ActiveMain!.Id == "Q06", "Q06 is active after Q05");
        e.PetsAndNpcs.HireNpc(NpcSpecialistId.VeBinhKhien);
        e.SignalQuest(QuestSignal.NpcHired);
        e.SignalQuest(QuestSignal.FenceRepaired);
        Check(e.Quests.ActiveMain!.Id == "Q06", "a mercenary is not a hall NPC");
        e.PetsAndNpcs.HireNpc(NpcSpecialistId.KySuXayDung);
        e.SignalQuest(QuestSignal.NpcHired);
        Check(e.Quests.Completed.Contains("Q06"), "Q06 completes: hall staffed + fence repaired");
    }

    public static void RunCharacterSelect()
    {
        Check(NewGameSetup.ShouldOfferCharacterSelect(false, false, false), "fresh game shows the start screen");
        Check(!NewGameSetup.ShouldOfferCharacterSelect(true, false, false), "restored save skips it");
        Check(!NewGameSetup.ShouldOfferCharacterSelect(false, true, false), "launcher preset skips it");
        Check(!NewGameSetup.ShouldOfferCharacterSelect(false, false, true), "video autopilot / smoke keep An");

        var e = new GameEngine(seed: 25);
        Check(e.Character.Identity == PlayerId.An, "default An");
        NewGameSetup.ChooseProtagonist(e, PlayerId.Khoa);
        Check(e.Character.Identity == PlayerId.Khoa && e.Character.Id == PlayerRoster.KhoaDataId &&
              e.Character.BasicTraitId == PlayerRoster.KhoaBasicTraitId, "Khoa roster applied");
        Check(e.Character.BaseStats.Str == 24 && e.Character.BaseStats.Res == 22, "GDD 8.1 Khoa base stats");
        Check(e.Story.ActiveProtagonist == PlayerId.Khoa && e.DeepMechanics.SpecialtyLocks.Owner == PlayerId.Khoa, "story protagonist in sync");
        Check(NewGameSetup.StatLine(PlayerId.Khoa) == "STR 24 • AGI 18 • CTRL 18 • RES 22" &&
              NewGameSetup.StatLine(PlayerId.An) == "STR 17 • AGI 23 • CTRL 24 • RES 18", "start screen stats from the roster");

        var json = SaveSystem.Serialize(e.CreateSaveData());
        Check(SaveSystem.TryDeserialize(json, out var data, out _), "save Khoa");
        var r = new GameEngine(seed: 25);
        r.RestoreFromSave(data!);
        Check(r.Character.Identity == PlayerId.Khoa && r.Story.ActiveProtagonist == PlayerId.Khoa, "choice survives save/load");
    }
}
