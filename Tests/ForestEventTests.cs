using System;
using PigTycoon.Core;

/// <summary>Forest events in the play flow: enter through the gate, resolve at the site, leave with full reward.</summary>
internal static class ForestEventTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var e = new GameEngine(seed: 21);
        var director = e.ForestAndEvents.Director;
        var rare = Pig.CreateDefault("p_rare", "Heo lạ", GeneLineId.TinhQuang, GeneRarity.HuyenThoai, initialAge: 20, initialWeight: 80f);
        e.Pigs.Add(rare);
        Check(!rare.IsIdentified, "a rare pig starts unidentified");
        Check(director.TrySeedGateOfferForScenario(e, MysteryEventId.E07_GiamDinhVoDanh), "a gate is offered");
        Check(director.TryEnterOfferedGate(e), "the player steps through");
        Check(!e.ForestAndEvents.Expedition.EvaluateExit(e.Clock.CurrentDay, e.SpaceBag).canExit, "cannot leave before acting");
        var res = director.ResolveActiveEvent(e, 0);
        Check(res.resolved && rare.IsIdentified, "E07 identifies the unknown pig: " + res.message);
        var exit = e.ForestAndEvents.Expedition.EvaluateExit(e.Clock.CurrentDay, e.SpaceBag);
        Check(exit.canExit && exit.rewardPercentTier == 100, "complete: free exit with full reward");
        Check(!director.ResolveActiveEvent(e, 0).resolved, "one resolution per expedition");
    }
}
