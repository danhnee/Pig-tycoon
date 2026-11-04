#if PLAYABLE_DEMO_RUNTIME_TESTS
using System;
using System.IO;
using PigTycoon.Core;
using PigTycoon.PlayableDemo3D;
internal static class PlayableRuntimeBindingTests
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < 0.0001f, message);
    private static void Reject(Action action, string message)
    { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } Check(rejected, message); }
    private static void Main()
    {
        string normalRoot = Path.Combine(Path.GetTempPath(), "pig-tycoon-normal-profile");
        string isolated = Path.Combine(Path.GetTempPath(), "pig-tycoon-acceptance-profile");
        var normal = PlayableDemoLaunchOptions.Resolve(Array.Empty<string>(), normalRoot, true);
        Check(normal.SaveDirectory == Path.GetFullPath(normalRoot) && normal.LoadExistingSave && normal.OwnSimulationClock && !normal.IsAcceptance,
            "Normal play preserves its profile load and automatic clock");
        var fresh = PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonAcceptance", "-pigTycoonSaveRoot", isolated, "-pigTycoonFreshAcceptance" }, normalRoot, true);
        Check(fresh.IsAcceptance && fresh.SaveDirectory == Path.GetFullPath(isolated) && !fresh.LoadExistingSave && !fresh.OwnSimulationClock,
            "Fresh acceptance isolates storage and disables automatic clock before initialization");
        var reload = PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonAcceptance", "-pigTycoonSaveRoot", isolated, "-pigTycoonAcceptanceReload" }, normalRoot, true);
        Check(reload.IsAcceptance && reload.LoadExistingSave && !reload.OwnSimulationClock && reload.SaveDirectory == fresh.SaveDirectory,
            "A second acceptance process loads the same isolated profile with driver-owned clock");
        Check(PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonAcceptance", "-pigTycoonSaveRoot", isolated, "-pigTycoonAcceptanceReload" }, normalRoot, false).LoadExistingSave,
            "Explicit acceptance reload loads its profile even when the inspector's ordinary load toggle is off");
        Check(Path.Combine(fresh.SaveDirectory, DemoScenarioFactory.Create(DemoScenarioKind.Care).SaveFileName) !=
              Path.Combine(fresh.SaveDirectory, DemoScenarioFactory.Create(DemoScenarioKind.PreRaidFixture).SaveFileName), "Acceptance keeps care/pre-raid filenames distinct");
        var strayFresh = PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonFreshAcceptance" }, normalRoot, true);
        Check(strayFresh.LoadExistingSave && strayFresh.OwnSimulationClock, "Fresh flag has no effect outside opt-in acceptance");
        Check(!PlayableDemoLaunchOptions.Resolve(Array.Empty<string>(), normalRoot, false).LoadExistingSave, "Normal explicit no-load setting is preserved");
        Reject(() => PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonAcceptance" }, normalRoot, true), "Acceptance cannot fall back to a production save directory");
        Reject(() => PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonSaveRoot", "relative/profile" }, normalRoot, true), "An override must be an absolute directory");
        Reject(() => PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonSaveRoot" }, normalRoot, true), "A missing save-root value rejects before initialization");
        Reject(() => PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonSaveRoot", isolated, "-pigTycoonSaveRoot", normalRoot }, normalRoot, true), "Multiple save roots cannot choose an ambiguous save owner");
        Reject(() => PlayableDemoLaunchOptions.Resolve(new[] { "-pigTycoonAcceptance", "-pigTycoonFreshAcceptance", "-pigTycoonAcceptanceReload", "-pigTycoonSaveRoot", isolated }, normalRoot, true), "Fresh and reload acceptance cannot conflict");
        var walk = PlayerLocomotion.Advance(false, 18, 100, 1);
        var run = PlayerLocomotion.Advance(true, 18, 100, 1);
        var exhausted = PlayerLocomotion.Advance(true, 18, 0, 1);
        var w = new FarmLocomotionAnimationSample(walk.MetersPerSecond, -20, 0, 1, walk.IsRunning, 0, 0);
        var r = new FarmLocomotionAnimationSample(run.MetersPerSecond, -20, 0, 1, run.IsRunning, 0, 0);
        var tired = new FarmLocomotionAnimationSample(exhausted.MetersPerSecond, -20, 0, 1, exhausted.IsRunning, 0, 0);
        Near(w.HorizontalSpeed, 4.5f, "Gravity/slope Y displacement cannot inflate locomotion speed");
        Check(!w.IsRunning && r.IsRunning && !tired.IsRunning, "State follows actual Core locomotion, including exhausted Shift fallback");
        var slowedRun = new FarmLocomotionAnimationSample(3, 0, 0, 1, run.IsRunning, 0, 0);
        var fastWalk = new FarmLocomotionAnimationSample(8, 0, 0, 1, walk.IsRunning, 0, 0);
        Check(slowedRun.IsRunning && !fastWalk.IsRunning, "Equipment/fatigue speed overlap cannot choose the wrong Core run state");
        var vertical = new FarmLocomotionAnimationSample(0, -20, 0, 1, false, 2, 4);
        Near(vertical.HorizontalSpeed, 0, "A purely vertical correction remains idle");
        Near(vertical.WalkPlaybackRate, 1, "Idle does not freeze any action clip");
        var measured = new FarmLocomotionAnimationSample(3, 0, 4, 1, true, 2, 4);
        Near(measured.WalkPlaybackRate, 2.5f, "Walk playback rate uses only measured horizontal movement and a measured reference");
        Near(measured.RunPlaybackRate, 1.25f, "Run playback rate uses its separate measured reference");
        Near(w.WalkPlaybackRate, 1, "Unmeasured clips keep their authored playback rate");
        Console.WriteLine("PASS Runtime binding: " + checks + " assertions");
    }
}
#endif
