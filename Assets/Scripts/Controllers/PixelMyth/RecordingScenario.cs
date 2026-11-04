using System;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Opt-in scenario for recording the gameplay video: launch the player with
    /// <c>-pigtycoon-scenario=video</c>. Installs the frame recorder and the demo autopilot, which opens one real
    /// six-hour mystic gate offer (E08 "Đổi Một Lấy Một") through Core when the route reaches the gate, so the Primeval
    /// Forest can be shown without waiting for the 0.25-8 %/day roll. Nothing changes without the flag; no button exists
    /// in the play flow.
    /// </summary>
    public static class RecordingScenario
    {
        public const string Flag = "-pigtycoon-scenario=video";
        private static bool installed;

        public static bool IsRequested => Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (installed || !IsRequested) return;
            installed = true;
            DemoFrameRecorder.Create();
            DemoAutopilot.Create();
        }

        /// <summary>Same call the earlier scenario made: a real Core gate offer, only valid on the Farm.</summary>
        public static bool SeedGate(GameEngine engine)
        {
            bool open = engine.ForestAndEvents.Director.TrySeedGateOfferForScenario(engine, MysteryEventId.E08_DoiMotLayMot)
                        || engine.ForestAndEvents.Director.HasOpenGate(engine);
            Debug.Log($"[RecordingScenario] mystic gate offer open: {open}");
            return open;
        }
    }
}
