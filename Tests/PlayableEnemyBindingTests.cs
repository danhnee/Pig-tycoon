#if PLAYABLE_DEMO_ENEMY_TESTS
using System;
using System.Linq;
using PigTycoon.Core;
using PigTycoon.PlayableDemo3D;
internal static class PlayableEnemyBindingTests
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Main()
    {
        var scenario = DemoScenarioFactory.Create(DemoScenarioKind.Care);
        var e = scenario.Engine; var session = new PlayableFarmSession(e);
        e.Pigs[0].PositionX = 1.3f; e.Pigs[0].PositionY = 0.5f; e.Pigs[1].IsIsolated = true;
        session.SetPlayerPosition(30, 30); e.Clock.IsMinorWaveTriggered = true;
        var raid = new PlayableFarmRaid(session);
        Check(raid.TryStartScheduledMinor(-55, 0, (_, __) => false).Success, "Test uses actual raid enemy factories and event boundary");
        var thief = raid.Enemies.Single(n => n.Model.TypeId == EnemyTypeId.KeBatHeo);
        var crawler = raid.Enemies.Single(n => n.Model.TypeId == EnemyTypeId.KeCaoPha);
        thief.X = -0.5f; thief.Y = 0.5f; crawler.X = -1; crawler.Y = 0.5f;
        session.RegisterFence("actual-fence", 0.5f, 0.5f, true);
        e.FlowField.SetWorldRectBlocked(0, 0, 1, 1, true); raid.RebuildObstacles();
        raid.Advance(0.5f, (_, __, ___, ____) => true);
        Check(thief.X < 0 && thief.CapturedPigId.Length == 0, "Near-target steering cannot enter a blocked fence cell even with a permissive physical callback");
        var crawlerCursor = new FarmRaidAnimationCursor();
        Check(crawler.AttackSequence > 0 && session.State.Fences[0].Hp < 300 && crawlerCursor.Read(crawler).Attack,
            "A crawler Attack cue follows real damage to the actual persistent fence");
        for (int i = 0; i < 80 && session.State.Fences[0].Hp > 0; i++) raid.Advance(0.5f, (_, __, ___, ____) => true);
        Check(session.State.Fences[0].Hp == 0 && thief.X < 0, "Actual attacks create a lasting breach while the old scene obstacle remains closed");
        // Emulate the controller's actual disabled-collider/navigation rebuild after ApplyState.
        e.FlowField.SetWorldRectBlocked(0, 0, 1, 1, false); raid.RebuildObstacles();
        for (int i = 0; i < 40 && thief.CapturedPigId.Length == 0; i++) raid.Advance(0.05f, (_, __, ___, ____) => true);
        Check(thief.X >= 0 && thief.CapturedPigId == e.Pigs[0].Id && thief.BindSequence == 1,
            "An opened breach becomes traversable and binds the actual Core pig exactly once");
        Check(e.Pigs[0].PositionX == 1.3f && e.Pigs[0].PositionY == 0.5f && thief.CaptureSeparation > 0f,
            "Capture preserves the pig position and records its actual separation without a cosmetic offset");
        var cursor = new FarmRaidAnimationCursor(); var captured = cursor.Read(thief);
        Check(captured.Bind && captured.IsDragging && !captured.IsDead, "Bind/Drag cues reflect the actual captured pig state");
        Check(!cursor.Read(thief).Bind, "Polling a rendered frame cannot replay the same capture cue");
        float capturedEnemyX = thief.X, capturedPigX = e.Pigs[0].PositionX;
        int blockedPigSweeps = 0;
        raid.Advance(0.2f, (ax, ay, bx, by) =>
        { if (ax > 1f && bx < ax) { blockedPigSweeps++; return false; } return true; });
        Check(blockedPigSweeps > 0 && Math.Abs(thief.X - capturedEnemyX) < 0.00001f && e.Pigs[0].PositionX == capturedPigX,
            "If the pig sweep hits a solid fence, the enemy step rolls back rather than dragging the pig through it: sweeps=" + blockedPigSweeps +
            " enemy=" + capturedEnemyX + "→" + thief.X + " pig=" + capturedPigX + "→" + e.Pigs[0].PositionX + " separation=" + thief.CaptureSeparation);
        raid.Advance(0.1f, (_, __, ___, ____) => true);
        float actualSeparation = (float)Math.Sqrt(Math.Pow(thief.X - e.Pigs[0].PositionX, 2) + Math.Pow(thief.Y - e.Pigs[0].PositionY, 2));
        Check(e.Pigs[0].PositionX != capturedPigX && Math.Abs(actualSeparation - thief.CaptureSeparation) < 0.0001f,
            "A free drag preserves the actual capture separation in Core positions");
        float stoppedX = thief.X, stoppedY = thief.Y;
        raid.Advance(0.5f, (_, __, ___, ____) => false);
        Check(thief.X == stoppedX && thief.Y == stoppedY, "The enemy must honor the swept physical boundary callback while dragging");
        float pigXAtRelease = e.Pigs[0].PositionX, pigYAtRelease = e.Pigs[0].PositionY;
        session.SetPlayerPosition(thief.X + 1.6f, thief.Y);
        Check(raid.AttackEnemy(thief.Model.Id).Success && thief.HitSequence == 1 && thief.CapturedPigId.Length == 0,
            "Only a real equipped weapon hit emits Hit and releases the actual captured pig");
        Check(e.Pigs[0].PositionX == pigXAtRelease && e.Pigs[0].PositionY == pigYAtRelease,
            "Releasing the captured pig preserves its actual position");
        var hit = cursor.Read(thief);
        Check(hit.Hit && !hit.IsDragging, "A real hit stops the Drag visual state");
        for (int i = 0; i < 20 && thief.Model.IsAlive; i++)
        { raid.Advance(0.51f, (_, __, ___, ____) => false); raid.AttackEnemy(thief.Model.Id); }
        Check(!thief.Model.IsAlive && cursor.Read(thief).Death, "Death cue follows actual zero HP");
        Check(!cursor.Read(thief).Death && cursor.Read(thief).IsDead, "Death clip starts once while the resolved death state remains visible");
        int hp = e.Character.Hp, gold = e.Farm.Gold; float corpseX = thief.X, corpseY = thief.Y;
        raid.Advance(5, (_, __, ___, ____) => true);
        Check(thief.X == corpseX && thief.Y == corpseY && e.Character.Hp == hp && e.Farm.Gold == gold && !raid.AttackEnemy(thief.Model.Id).Success,
            "A retained death visual cannot move, damage, receive another hit or award twice");
        var freshScenario = DemoScenarioFactory.Create(DemoScenarioKind.Care);
        var s = new PlayableFarmSession(freshScenario.Engine); s.SetPlayerPosition(30, 30);
        freshScenario.Engine.Clock.IsMinorWaveTriggered = true; var telegraphRaid = new PlayableFarmRaid(s);
        telegraphRaid.TryStartScheduledMinor(-55, 0, (_, __) => false);
        var attacker = telegraphRaid.Enemies[0]; attacker.X = 29.5f; attacker.Y = 30;
        var telegraphCursor = new FarmRaidAnimationCursor();
        telegraphRaid.Advance(0.05f, (_, __, ___, ____) => false);
        Check(attacker.TelegraphSequence == 1 && telegraphCursor.Read(attacker).Telegraph && attacker.AttackSequence == 0,
            "Telegraph cue precedes the existing real attack resolution");
        telegraphRaid.Advance(0.2f, (_, __, ___, ____) => false);
        Check(attacker.AttackSequence == 1 && telegraphCursor.Read(attacker).Attack,
            "Attack cue follows the existing telegraph timer rather than an animation-created damage event");
        var escapeScenario = DemoScenarioFactory.Create(DemoScenarioKind.Care);
        var escapeSession = new PlayableFarmSession(escapeScenario.Engine); escapeSession.SetPlayerPosition(30, 30);
        escapeScenario.Engine.Clock.IsMinorWaveTriggered = true; var escapeRaid = new PlayableFarmRaid(escapeSession);
        escapeRaid.TryStartScheduledMinor(-55, 0, (_, __) => false);
        var escaping = escapeRaid.Enemies.Single(n => n.Model.TypeId == EnemyTypeId.KeBatHeo);
        var actualPig = escapeScenario.Engine.Pigs[0]; escapeScenario.Engine.Pigs[1].IsIsolated = true;
        actualPig.PositionX = -59.2f; actualPig.PositionY = 0.5f; escaping.X = -59.9f; escaping.Y = 0.5f;
        escapeRaid.Advance(0.05f, (_, __, ___, ____) => true);
        Check(escaping.CapturedPigId == actualPig.Id && actualPig.PositionX == -59.2f, "Boundary test captures the actual pig without snapping its position");
        for (int i = 0; i < 30 && escapeScenario.Layout.Map.MapBounds.Contains(escaping.X, escaping.Y); i++)
            escapeRaid.Advance(0.05f, (_, __, ___, ____) => true);
        Check(!escapeScenario.Layout.Map.MapBounds.Contains(escaping.X, escaping.Y) && !escaping.Escaped && escapeScenario.Engine.Pigs.Contains(actualPig),
            "The enemy crossing alone cannot remove a tethered pig that remains inside the map");
        for (int i = 0; i < 60 && !escaping.Escaped; i++) escapeRaid.Advance(0.05f, (_, __, ___, ____) => true);
        Check(escaping.Escaped && !escapeScenario.Engine.Pigs.Contains(actualPig),
            "Only the actual pig boundary crossing resolves its one loss");
        for (int i = 0; i < 20 && escapeRaid.Active; i++) escapeRaid.Advance(0.5f, (_, __, ___, ____) => true);
        Check(!escapeRaid.Active && escapeRaid.LastReport?.PigsLostOrAbducted == 1,
            "The event-derived report waits for every actual enemy to resolve and counts the single pig loss");
        escapeRaid.Advance(1, (_, __, ___, ____) => true);
        Check(escapeScenario.Engine.Pigs.Count == 1, "A resolved escape cannot remove a second pig");
        Console.WriteLine("PASS Enemy binding: " + checks + " assertions");
    }
}
#endif
