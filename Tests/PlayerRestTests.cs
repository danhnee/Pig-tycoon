using System;
using PigTycoon.Core;

internal static class PlayerRestTests
{
    public static void Run()
    {
        var engine = new GameEngine(37);
        engine.Character.Stamina.CurrentStamina = 10;
        engine.Character.Stamina.DailyLoad = 25;
        var first = PlayerRestCommands.Sleep(engine);
        Require(first.success, "First standard sleep must succeed.");
        Require(engine.Character.Stamina.DailyLoad == 0, "Successful sleep clears daily load.");
        Require(engine.Character.Stamina.CurrentStamina >= engine.Character.Stamina.BaseMaxStamina * 0.8f,
            "Use the canonical GDD sleep reset.");
        engine.Character.Stamina.CurrentStamina = 12;
        engine.Character.Stamina.DailyLoad = 7;
        engine.Character.Hp = 40;
        int day = engine.Clock.CurrentDay;
        var repeated = PlayerRestCommands.Sleep(engine);
        Require(!repeated.success, "A building must not bypass the six-hour sleep cooldown.");
        Require(engine.Character.Stamina.CurrentStamina == 12 && engine.Character.Stamina.DailyLoad == 7 &&
            engine.Character.Hp == 40 && engine.Clock.CurrentDay == day,
            "Rejected sleep cannot grant health, stamina or time advancement.");
        var combat = new GameEngine(38);
        combat.Clock.IsDuringCombatWave = true;
        combat.Character.Stamina.CurrentStamina = 9;
        Require(!PlayerRestCommands.Sleep(combat).success && combat.Character.Stamina.CurrentStamina == 9,
            "Resting at a building cannot heal during a raid.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
