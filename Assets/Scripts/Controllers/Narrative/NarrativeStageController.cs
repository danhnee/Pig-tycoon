using System;
using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// What the dialogue UI (Argon's VnStoryDialoguePopup) implements. The box itself reads <see cref="NarrativeRunner"/>
    /// every frame; this interface only covers presentation commands the controller cannot do alone.
    /// </summary>
    public interface INarrativeView
    {
        /// <summary>True while the view is on screen and able to show scenes.</summary>
        bool Ready { get; }
        /// <summary>Emote, Fade, Shake, Sfx, Music, Backdrop, Banner frames… Return false if not handled; call done() when a
        /// blocking command has finished (the controller waits for it).</summary>
        bool Perform(StageEvent ev, Action done);
        /// <summary>reflection:, notebook:, tutorial: custom effects.</summary>
        void OnCustom(NarrativeEffect effect);
    }

    /// <summary>
    /// Runs story scenes in the world (ADR-0027): ticks the runner, locks gameplay input, walks actors to map marks,
    /// pans the camera, travels for scenes bound to another map and for "goto_map:". Without a dialogue view it keeps
    /// the game moving: talk shows the first line as a bubble, other scenes are skipped (effects kept) with the recap.
    /// </summary>
    public sealed class NarrativeStageController : MonoBehaviour
    {
        public static NarrativeStageController Instance { get; private set; }
        public static INarrativeView View { get; set; }
        static bool ViewReady => View != null && View.Ready;

        const float ActorArriveDistance = 0.25f;
        const float ActorMoveTimeout = 8f;

        GameEngine boundEngine;
        string lastLocation;
        readonly List<Pending> pending = new List<Pending>();
        string pendingGotoMap;
        bool lockedInput;
        Transform cameraAnchor;
        Transform cameraPrevTarget;

        sealed class Pending
        {
            public StageEvent Event;
            public Func<bool> Finished;
            public float Deadline;
            public Action OnTimeout;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("NarrativeStage");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<NarrativeStageController>();
        }

        static readonly bool Recording = Environment.CommandLine.Contains(RecordingScenario.Flag);

        static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        /// <summary>
        /// Talk to an NPC through the story runtime. Returns null when there is no engine (caller shows its own bubble),
        /// "" when the dialogue view is showing the scene, or the first line to show as a bubble when there is no view.
        /// </summary>
        public static string Talk(PixelMythNpc npc)
        {
            var e = Engine;
            if (e == null || npc == null || e.Narrative.Runner.InScene) return null;
            var scene = e.TalkTo(npc.CastId);
            if (scene == null) return null;
            if (ViewReady) return "";
            string line = FirstLine(scene, e.Story.ActiveProtagonist);
            e.Narrative.Runner.Skip();
            if (e.Narrative.Runner.InScene) e.Narrative.Runner.Skip(); // unskippable talk: still never block the world
            return line ?? "…";
        }

        static string FirstLine(SceneDefinition s, PlayerId who)
        {
            var c = s.Commands.FirstOrDefault(x => x.Kind == NarrativeCmd.Say || x.Kind == NarrativeCmd.Narrate);
            return c == null ? null : NarrativeRunner.ResolveFor(c.Text, who);
        }

        void Update()
        {
            var e = Engine;
            if (e == null) return;
            if (e != boundEngine)
            {
                if (boundEngine != null) boundEngine.NarrativeCustom -= OnCustom;
                boundEngine = e;
                e.NarrativeCustom += OnCustom;
                pending.Clear();
                lastLocation = null;
                // Recorded demo starts mid-game (no character select): treat the prologue as played so chapter 1 and
                // village talk show in the video.
                if (Recording) e.NarrativeState.Flags.Add("prologue_done");
            }
            var runner = e.Narrative.Runner;
            var travel = FindTravel();

            // Entering a map (new game, load, travel) fires the story trigger once the travel has landed.
            string here = travel != null ? travel.CurrentLocationId : e.WorldLocations.CurrentLocationId;
            if (!string.IsNullOrEmpty(here) && here != lastLocation && (travel == null || !travel.IsTravelling))
            {
                lastLocation = here;
                e.FireNarrative(NarrativeTrigger.EnterLocation, here);
            }

            if (!runner.InScene)
            {
                ReleaseInput();
                RestoreCamera();
                ReleaseNpcs();
                if (pendingGotoMap != null && travel != null && !travel.IsTravelling)
                {
                    string map = pendingGotoMap;
                    pendingGotoMap = null;
                    if (travel.CurrentLocationId == map) e.FireNarrative(NarrativeTrigger.EnterLocation, map); // already there
                    else travel.TravelToLocation(map);
                }
                return;
            }

            if (!ViewReady)
            {
                // No dialogue UI yet: never leave the player stuck behind an invisible scene.
                runner.Headless = true;
                var scene = runner.Scene;
                if (runner.Skip() && scene != null && !string.IsNullOrEmpty(scene.Recap))
                    PlayerInteractionController.Instance?.ShowFeedback(scene.Recap);
                return;
            }

            runner.Headless = false;
            // Recorded demo: dialogue reads itself (auto mode) so the autopilot never waits on a tap.
            if (Recording && !runner.Auto) runner.Auto = true;
            LockInput();

            // A scene bound to another map plays after the travel lands there.
            var bound = runner.Scene?.Location;
            if (!string.IsNullOrEmpty(bound) && travel != null && travel.CurrentLocationId != bound)
            {
                if (!travel.IsTravelling && !travel.TravelToLocation(bound)) runner.Scene.Location = ""; // no route: play here
                return;
            }
            if (travel != null && travel.IsTravelling) return;

            foreach (var ev in runner.TakeStageEvents()) Begin(ev, runner);
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                bool timedOut = Time.unscaledTime > p.Deadline;
                if (p.Finished() || timedOut)
                {
                    if (timedOut) p.OnTimeout?.Invoke();
                    pending.RemoveAt(i);
                    if (p.Event.Blocking) runner.StageDone(p.Event.Seq);
                }
            }
            runner.Tick(Time.unscaledDeltaTime);
        }

        void Begin(StageEvent ev, NarrativeRunner runner)
        {
            var c = ev.Command;
            switch (c.Kind)
            {
                case NarrativeCmd.ActorPlace:
                {
                    var at = Resolve(c);
                    if (at.HasValue) Place(c.Actor, at.Value, c.Facing);
                    Done(ev, runner);
                    return;
                }
                case NarrativeCmd.ActorMove:
                {
                    var at = Resolve(c);
                    if (!at.HasValue) { Done(ev, runner); return; }
                    StartMove(ev, c.Actor, at.Value);
                    return;
                }
                case NarrativeCmd.ActorFace:
                    Face(c.Actor, c.Facing);
                    Done(ev, runner);
                    return;
                case NarrativeCmd.Camera:
                {
                    var at = Resolve(c);
                    if (at.HasValue) PanCamera(at.Value);
                    float until = Time.unscaledTime + Mathf.Max(0, c.Ms) / 1000f;
                    Wait(ev, () => Time.unscaledTime >= until, c.Ms / 1000f + 1f);
                    return;
                }
            }
            bool finished = false;
            if (View != null && View.Perform(ev, () => finished = true)) Wait(ev, () => finished, Mathf.Max(2f, c.Ms / 1000f + 2f));
            else Done(ev, runner);
        }

        void Done(StageEvent ev, NarrativeRunner runner)
        {
            if (ev.Blocking) runner.StageDone(ev.Seq);
        }

        void Wait(StageEvent ev, Func<bool> finished, float timeoutSeconds, Action onTimeout = null)
            => pending.Add(new Pending { Event = ev, Finished = finished, Deadline = Time.unscaledTime + timeoutSeconds, OnTimeout = onTimeout });

        static Vector2? Resolve(NarrativeCommand c)
        {
            if (string.IsNullOrEmpty(c.Label)) return new Vector2(c.X, c.Y);
            if (NarrativeMarks.Table.TryGetValue(c.Label, out var p)) return p;
            Debug.LogWarning($"[Narrative] unknown stage mark '{c.Label}'");
            return null;
        }

        // ---- actors ----
        static PixelMythNpc FindNpc(string castId) =>
            PixelMythNpc.All.FirstOrDefault(n => n != null && string.Equals(n.CastId, castId, StringComparison.OrdinalIgnoreCase));

        static bool IsPlayer(string actor) => string.IsNullOrEmpty(actor) || actor == "player" || actor == CastIds.An || actor == CastIds.Khoa;

        void Place(string actor, Vector2 at, CardinalDirection facing)
        {
            if (IsPlayer(actor))
            {
                var pc = PlayerMobileController.Instance;
                if (pc == null) return;
                var body = pc.GetComponent<Rigidbody2D>();
                if (body != null) { body.position = at; body.linearVelocity = Vector2.zero; }
                pc.transform.position = new Vector3(at.x, at.y, pc.transform.position.z);
                Face(actor, facing);
                return;
            }
            var npc = FindNpc(actor);
            if (npc == null) return;
            npc.InScene = true;
            npc.ScriptedTarget = null;
            npc.Warp(at);
            npc.FaceToward(ToFacing(facing));
        }

        void StartMove(StageEvent ev, string actor, Vector2 at)
        {
            if (IsPlayer(actor))
            {
                var pc = PlayerMobileController.Instance;
                if (pc == null) { Done(ev, Engine.Narrative.Runner); return; }
                pc.CanMove = true; // the scripted input drives the real locomotion and walk animation
                Wait(ev, () =>
                {
                    Vector2 d = at - (Vector2)pc.transform.position;
                    if (d.magnitude <= ActorArriveDistance) { pc.ScriptedMoveInput = null; return true; }
                    pc.ScriptedMoveInput = d.normalized * 0.7f; // walk pace, not a sprint
                    return false;
                }, ActorMoveTimeout, () => { pc.ScriptedMoveInput = null; Place(actor, at, CardinalDirection.South); });
                return;
            }
            var npc = FindNpc(actor);
            if (npc == null) { Done(ev, Engine.Narrative.Runner); return; }
            npc.InScene = true;
            npc.ScriptedTarget = at;
            Wait(ev, () => npc == null || npc.ScriptedArrived, ActorMoveTimeout, () => { if (npc != null) { npc.ScriptedTarget = null; npc.Warp(at); } });
        }

        void Face(string actor, CardinalDirection facing)
        {
            if (IsPlayer(actor))
            {
                var pc = PlayerMobileController.Instance;
                pc?.GetComponent<CharacterSpriteAnimator>()?.FaceDirection(ToFacing(facing));
                return;
            }
            var npc = FindNpc(actor);
            if (npc != null) { npc.InScene = true; npc.FaceToward(ToFacing(facing)); }
        }

        static CharacterFacing ToFacing(CardinalDirection d) => d switch
        {
            CardinalDirection.North => CharacterFacing.Up,
            CardinalDirection.East => CharacterFacing.Right,
            CardinalDirection.West => CharacterFacing.Left,
            _ => CharacterFacing.Down,
        };

        static void ReleaseNpcs()
        {
            foreach (var n in PixelMythNpc.All)
                if (n != null && n.InScene) { n.InScene = false; n.ScriptedTarget = null; }
        }

        // ---- camera ----
        void PanCamera(Vector2 at)
        {
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
            if (follow == null) return;
            if (cameraAnchor == null) cameraAnchor = new GameObject("NarrativeCameraAnchor").transform;
            if (cameraPrevTarget == null) cameraPrevTarget = follow.Target;
            cameraAnchor.position = new Vector3(at.x, at.y, 0f);
            follow.Target = cameraAnchor;
        }

        void RestoreCamera()
        {
            if (cameraPrevTarget == null) return;
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
            if (follow != null) follow.Target = cameraPrevTarget;
            cameraPrevTarget = null;
        }

        // ---- input lock ----
        void LockInput()
        {
            var pc = PlayerMobileController.Instance;
            // Recorded demo: the autopilot keeps playing while dialogue reads itself on top (no stalled route).
            if (pc == null || lockedInput || Recording) return;
            lockedInput = true;
            pc.CanMove = false;
        }

        void ReleaseInput()
        {
            if (!lockedInput) return;
            lockedInput = false;
            var pc = PlayerMobileController.Instance;
            if (pc != null) { pc.ScriptedMoveInput = null; pc.CanMove = true; }
        }

        // ---- custom effects ----
        void OnCustom(NarrativeEffect fx)
        {
            if (fx.Id.StartsWith(NarrativeCustomIds.GotoMapPrefix, StringComparison.Ordinal))
            {
                pendingGotoMap = fx.Id.Substring(NarrativeCustomIds.GotoMapPrefix.Length);
                return;
            }
            View?.OnCustom(fx);
        }

        static PixelWorldTravelController cachedTravel;
        static PixelWorldTravelController FindTravel()
        {
            if (cachedTravel == null) cachedTravel = FindFirstObjectByType<PixelWorldTravelController>();
            return cachedTravel;
        }
    }
}
