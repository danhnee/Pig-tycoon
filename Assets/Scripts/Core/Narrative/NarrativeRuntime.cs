using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PigTycoon.Core
{
    // Narrative runtime (ADR-0027): dialogue with portraits and choices, scripted scenes, skip that keeps state whole,
    // backlog, read-line skip and auto mode. Content is authored as SceneDefinition command lists (NarrativeContent*.cs);
    // the view only renders what the runner exposes and reports taps / finished stage commands back.

    /// <summary>Portrait ids the art set must provide per speaker (see <see cref="NarrativeCast"/>). Missing art falls back to Neutral.</summary>
    public static class Portraits
    {
        public const string Neutral = "neutral", Happy = "happy", Sad = "sad", Angry = "angry", Surprised = "surprised",
            Thinking = "thinking", Worried = "worried", Laugh = "laugh";
        // An: warm and "too normal"; the hidden side shows only through these three (user direction 2 Nov 2026).
        public const string AnTooBright = "too_bright", AnStare = "stare", AnSilentTilt = "silent_tilt";
        // Khoa has no facial expressions in Season 1 (story bible 2.4); emotion appears only in reflections.
        public const string KhoaReflectFrown = "reflect_frown", KhoaReflectSmile = "reflect_smile", KhoaReflectCry = "reflect_cry";
    }

    public enum StageSide { Left, Right }
    public enum NarrativeTrigger { Manual, NewGame, DayStart, EnterLocation, Signal, Talk, WaveEnded, Sleep, QuestCompleted }

    public enum NarrativeCmd
    {
        Say, Narrate, Choice, Label, Goto, IfGoto, Effect,
        // stage (presentation only; ignored by skip)
        ActorPlace, ActorMove, ActorFace, Emote, Camera, Fade, Pause, Shake, Sfx, Music, Banner, Backdrop,
        SkipPoint, End
    }

    public enum NarrativeEffectKind { SetFlag, ClearFlag, Gold, Item, Signal, Affinity, MailNow, MailTomorrow, QuestStart, Custom, PlayScene }

    public sealed class NarrativeEffect
    {
        public NarrativeEffectKind Kind;
        public string Id = "";
        public int Amount;
        public override string ToString() => $"{Kind}:{Id}:{Amount}";
    }

    public sealed class NarrativeOption
    {
        public string Text = "";
        /// <summary>GameStateQuery string (STORY_FLAG etc.). Empty = always.</summary>
        public string Condition = "";
        public bool ShowWhenLocked;
        public string LockedReason = "";
        public List<NarrativeEffect> Effects = new List<NarrativeEffect>();
        public string GotoLabel = "";
    }

    public sealed class NarrativeCommand
    {
        public NarrativeCmd Kind;
        public string Speaker = "";      // cast id; "" = narrator
        public string Portrait = Portraits.Neutral;
        public StageSide Side = StageSide.Left;
        public string Text = "";         // may contain {an:..|khoa:..} and {player}
        public string Label = "";
        public string Condition = "";
        public NarrativeEffect Effect;
        public List<NarrativeOption> Options;
        public int DefaultOption;        // used when the scene is skipped past this choice
        public string Actor = "";
        public float X, Y;
        public CardinalDirection Facing;
        public int Ms;
        public bool Async;               // stage command does not block the script
    }

    public sealed class SceneDefinition
    {
        public string Id = "";
        public string Title = "";
        public NarrativeTrigger Trigger = NarrativeTrigger.Manual;
        public string TriggerArg = "";   // location id, signal name, npc id
        public string Condition = "";    // GameStateQuery
        public bool Once = true;
        public int Priority;
        public bool Skippable = true;
        public string Recap = "";        // shown when skipped (HSR-style summary)
        public string Location = "";     // map the scene plays on ("" = wherever the player is)
        public string Tag = "";          // NarrativeTalk.TagDaily / TagRepeat for NPC small talk
        public readonly List<NarrativeCommand> Commands = new List<NarrativeCommand>();

        // ---- builder (content files read like a script) ----
        public SceneDefinition Say(string speaker, string portrait, string text, StageSide side = StageSide.Left)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Say, Speaker = speaker, Portrait = portrait, Text = text, Side = side }); return this; }
        public SceneDefinition Narrate(string text) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Narrate, Text = text }); return this; }
        public SceneDefinition Choice(string prompt, int defaultOption, params NarrativeOption[] options)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Choice, Text = prompt, Options = options.ToList(), DefaultOption = defaultOption }); return this; }
        public SceneDefinition Label(string label) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Label, Label = label }); return this; }
        public SceneDefinition Goto(string label) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Goto, Label = label }); return this; }
        public SceneDefinition IfGoto(string condition, string label) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.IfGoto, Condition = condition, Label = label }); return this; }
        public SceneDefinition Do(NarrativeEffectKind kind, string id, int amount = 0)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Effect, Effect = new NarrativeEffect { Kind = kind, Id = id, Amount = amount } }); return this; }
        public SceneDefinition Flag(string flag) => Do(NarrativeEffectKind.SetFlag, flag);
        public SceneDefinition Place(string actor, float x, float y, CardinalDirection facing)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.ActorPlace, Actor = actor, X = x, Y = y, Facing = facing }); return this; }
        /// <summary>
        /// Stage marks name real map spots so content never hard-codes tiles: "village:gate", "village:well",
        /// "village:eatery", "village:spawn", "village:npc:man" (NPC home spot), "village:station:quan_ba_sau",
        /// "farm:station:main_house", "town:npc:npc_post"… The view resolves them from the map layout json.
        /// </summary>
        public SceneDefinition PlaceAt(string actor, string mark, CardinalDirection facing)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.ActorPlace, Actor = actor, Label = mark, Facing = facing }); return this; }
        public SceneDefinition MoveTo(string actor, string mark, bool async = false)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.ActorMove, Actor = actor, Label = mark, Async = async }); return this; }
        public SceneDefinition CameraTo(string mark, int ms) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Camera, Label = mark, Ms = ms }); return this; }
        /// <summary>Queue another scene to play right after this one (prologue chains).</summary>
        public SceneDefinition Then(string sceneId) => Do(NarrativeEffectKind.PlayScene, sceneId);
        public SceneDefinition Move(string actor, float x, float y, bool async = false)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.ActorMove, Actor = actor, X = x, Y = y, Async = async }); return this; }
        public SceneDefinition Face(string actor, CardinalDirection facing)
        { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.ActorFace, Actor = actor, Facing = facing, Async = true }); return this; }
        public SceneDefinition Emote(string actor, string emote) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Emote, Actor = actor, Label = emote, Async = true }); return this; }
        public SceneDefinition Camera(float x, float y, int ms) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Camera, X = x, Y = y, Ms = ms }); return this; }
        public SceneDefinition FadeOut(int ms = 500) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Fade, Label = "out", Ms = ms }); return this; }
        public SceneDefinition FadeIn(int ms = 500) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Fade, Label = "in", Ms = ms }); return this; }
        public SceneDefinition Pause(int ms) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Pause, Ms = ms }); return this; }
        public SceneDefinition Shake(int ms = 300) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Shake, Ms = ms, Async = true }); return this; }
        public SceneDefinition Sfx(string id) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Sfx, Label = id, Async = true }); return this; }
        public SceneDefinition Music(string id) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Music, Label = id, Async = true }); return this; }
        /// <summary>Chapter / location title card ("Chương 1 — Trại Cũ").</summary>
        public SceneDefinition Banner(string text, int ms = 1800) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Banner, Text = text, Ms = ms }); return this; }
        /// <summary>Full-screen illustrated background (VN "CG") for scenes away from the playable maps; "" clears it.</summary>
        public SceneDefinition Backdrop(string id, int fadeMs = 600) { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.Backdrop, Label = id, Ms = fadeMs }); return this; }
        public SceneDefinition End() { Commands.Add(new NarrativeCommand { Kind = NarrativeCmd.End }); return this; }

        public static NarrativeOption Opt(string text, string gotoLabel = "", params NarrativeEffect[] effects)
            => new NarrativeOption { Text = text, GotoLabel = gotoLabel, Effects = effects.ToList() };
        public static NarrativeEffect Fx(NarrativeEffectKind kind, string id, int amount = 0) => new NarrativeEffect { Kind = kind, Id = id, Amount = amount };
    }

    /// <summary>Everything the runner needs from the game. <see cref="EngineNarrativeHost"/> is the real one; tests use a fake.</summary>
    public interface INarrativeHost
    {
        PlayerId Protagonist { get; }
        int Day { get; }
        bool Check(string condition);
        void Apply(NarrativeEffect effect);
        void MarkSceneSeen(string sceneId);
        bool HasSeenScene(string sceneId);
    }

    /// <summary>Player-facing reading settings (Ren'Py defaults adapted for touch). Values are UX defaults, not balance.</summary>
    [Serializable]
    public sealed class NarrativeSettings
    {
        /// <summary>Characters per second; 0 = instant.</summary>
        public float TextCps = 45f;
        public float AutoDelayBase = 1.2f;
        public float AutoDelayPerChar = 0.035f;
        /// <summary>Hold-to-skip also skips lines never read (off by default, like Ren'Py skip_unseen).</summary>
        public bool SkipUnread;
        /// <summary>1 = default size; the UI scales text by this (accessibility).</summary>
        public float TextScale = 1f;
    }

    /// <summary>Saved narrative state: story flags, read lines (for skip), NPC talk-today marks, reading settings.</summary>
    [Serializable]
    public sealed class NarrativeState
    {
        public HashSet<string> Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ReadLines = new HashSet<string>(StringComparer.Ordinal);
        public Dictionary<string, int> TalkedOnDay = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public NarrativeSettings Settings = new NarrativeSettings();
        /// <summary>Openers waiting for a morning (StoryPacing), oldest first.</summary>
        public List<string> Waiting = new List<string>();
    }

    public enum RunnerPhase { Idle, Line, Choice, Stage, Pause, Banner }

    public sealed class LineView
    {
        public string SpeakerId = "";
        public string SpeakerName = "";
        public string NameplateColor = "";
        public string Portrait = Portraits.Neutral;
        public StageSide Side;
        public bool IsNarration;
        public string FullText = "";
        public int VisibleChars;
        public bool Complete => VisibleChars >= FullText.Length;
        public bool ReadBefore;
        public string VisibleText => FullText.Substring(0, Math.Min(VisibleChars, FullText.Length));
    }

    public sealed class ChoiceView
    {
        public int Index;
        public string Text = "";
        public bool Enabled;
        public string LockedReason = "";
    }

    public sealed class BacklogEntry
    {
        public string Speaker = "";
        public string Text = "";
        public bool IsChoice;
        public bool IsRecap;
    }

    public sealed class StageEvent
    {
        public int Seq;
        public NarrativeCommand Command;
        public bool Blocking;
    }

    /// <summary>
    /// Plays one scene at a time. Flow: Start → Tick/Tap/Choose/StageDone until Idle. Effects are the only state changes;
    /// skipping fast-forwards the remaining script with stage commands ignored and choices taken at their default, so a
    /// skip can never leave the game half-updated (Stardew Shane-event bug).
    /// </summary>
    public sealed class NarrativeRunner
    {
        public const int BacklogCapacity = 200;
        const int StepGuard = 10000;

        readonly INarrativeHost host;
        public readonly NarrativeState State;
        public SceneDefinition Scene { get; private set; }
        public RunnerPhase Phase { get; private set; } = RunnerPhase.Idle;
        public LineView Line { get; private set; }
        public List<ChoiceView> Choices { get; } = new List<ChoiceView>();
        public string ChoicePrompt { get; private set; } = "";
        public string BannerText { get; private set; } = "";
        /// <summary>Current full-screen backdrop id ("" = live world).</summary>
        public string BackdropId { get; private set; } = "";
        public bool Auto { get; set; }
        /// <summary>Held fast-forward (touch: long press; PC: Ctrl).</summary>
        public bool FastForward { get; set; }
        /// <summary>No view attached: stage commands complete instantly (Core tests, playtest bot).</summary>
        public bool Headless { get; set; } = true;
        public bool InScene => Phase != RunnerPhase.Idle;
        /// <summary>Gameplay input must be locked while this is true.</summary>
        public bool LocksInput => InScene;

        readonly List<BacklogEntry> backlog = new List<BacklogEntry>();
        public IReadOnlyList<BacklogEntry> Backlog => backlog;
        readonly List<StageEvent> stageQueue = new List<StageEvent>();
        /// <summary>Stage commands for the view to perform, in order. The view removes them with <see cref="TakeStageEvents"/>.</summary>
        public IReadOnlyList<StageEvent> PendingStage => stageQueue;
        int stageSeq, blockingSeq = -1;

        int pc;
        float charAccumulator, autoTimer, pauseLeft;
        public event Action<SceneDefinition, bool> SceneEnded; // (scene, skipped)

        public NarrativeRunner(INarrativeHost host, NarrativeState state)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            State = state ?? new NarrativeState();
        }

        public bool Start(SceneDefinition scene)
        {
            if (scene == null || InScene) return false;
            Scene = scene;
            pc = 0;
            Auto = false;
            Step();
            return true;
        }

        public List<StageEvent> TakeStageEvents()
        {
            var list = new List<StageEvent>(stageQueue);
            stageQueue.Clear();
            return list;
        }

        /// <summary>The view finished a blocking stage command (camera pan, actor walk, fade).</summary>
        public void StageDone(int seq)
        {
            if (Phase != RunnerPhase.Stage || seq != blockingSeq) return;
            blockingSeq = -1;
            Step();
        }

        public void Tick(float dt)
        {
            switch (Phase)
            {
                case RunnerPhase.Line:
                    if (!Line.Complete)
                    {
                        float cps = State.Settings.TextCps;
                        if (cps <= 0f || FastForward && CanFastForward()) Line.VisibleChars = Line.FullText.Length;
                        else
                        {
                            charAccumulator += dt * cps;
                            int add = (int)charAccumulator;
                            charAccumulator -= add;
                            Line.VisibleChars = Math.Min(Line.FullText.Length, Line.VisibleChars + add);
                        }
                        autoTimer = 0f;
                    }
                    else if (FastForward && CanFastForward()) AdvanceLine();
                    else if (Auto)
                    {
                        autoTimer += dt;
                        if (autoTimer >= State.Settings.AutoDelayBase + State.Settings.AutoDelayPerChar * Line.FullText.Length) AdvanceLine();
                    }
                    break;
                case RunnerPhase.Pause:
                case RunnerPhase.Banner:
                    pauseLeft -= dt;
                    if (pauseLeft <= 0f || FastForward) Step();
                    break;
            }
        }

        bool CanFastForward() => Line != null && (Line.ReadBefore || State.Settings.SkipUnread);

        /// <summary>Tap anywhere: first tap completes the line, the next one advances (VN standard).</summary>
        public void Tap()
        {
            if (Phase == RunnerPhase.Line)
            {
                if (!Line.Complete) Line.VisibleChars = Line.FullText.Length;
                else AdvanceLine();
            }
            else if (Phase == RunnerPhase.Banner) Step();
        }

        void AdvanceLine()
        {
            Line = null;
            Step();
        }

        public bool Choose(int index)
        {
            if (Phase != RunnerPhase.Choice) return false;
            var cmd = Scene.Commands[pc];
            var view = Choices.FirstOrDefault(c => c.Index == index);
            if (view == null || !view.Enabled) return false;
            var opt = cmd.Options[index];
            AddBacklog("", opt.Text, isChoice: true);
            Choices.Clear();
            ApplyOption(opt);
            Step();
            return true;
        }

        void ApplyOption(NarrativeOption opt)
        {
            foreach (var e in opt.Effects) host.Apply(e);
            pc = string.IsNullOrEmpty(opt.GotoLabel) ? pc + 1 : FindLabel(opt.GotoLabel);
        }

        /// <summary>Skip the rest of the scene: effects still apply, choices take their default. Returns false if not skippable.</summary>
        public bool Skip()
        {
            if (!InScene || Scene == null || !Scene.Skippable) return false;
            Choices.Clear();
            // Line / Stage / Pause / Banner already moved pc past their command; Choice leaves pc on the choice.
            int guard = 0;
            while (pc < Scene.Commands.Count && guard++ < StepGuard)
            {
                var c = Scene.Commands[pc];
                switch (c.Kind)
                {
                    case NarrativeCmd.End: pc = Scene.Commands.Count; break;
                    case NarrativeCmd.Effect: host.Apply(c.Effect); pc++; break;
                    case NarrativeCmd.Goto: pc = FindLabel(c.Label); break;
                    case NarrativeCmd.IfGoto: pc = host.Check(c.Condition) ? FindLabel(c.Label) : pc + 1; break;
                    case NarrativeCmd.Choice:
                        var opt = DefaultOptionFor(c);
                        if (opt == null) pc++;
                        else ApplyOption(opt);
                        break;
                    default: pc++; break;
                }
            }
            stageQueue.Clear();
            if (!string.IsNullOrEmpty(Scene.Recap)) AddBacklog("", Scene.Recap, isRecap: true);
            Finish(skipped: true);
            return true;
        }

        NarrativeOption DefaultOptionFor(NarrativeCommand c)
        {
            if (c.Options == null || c.Options.Count == 0) return null;
            int d = Math.Max(0, Math.Min(c.DefaultOption, c.Options.Count - 1));
            if (host.Check(c.Options[d].Condition)) return c.Options[d];
            return c.Options.FirstOrDefault(o => host.Check(o.Condition));
        }

        int FindLabel(string label)
        {
            int i = Scene.Commands.FindIndex(c => c.Kind == NarrativeCmd.Label && c.Label == label);
            if (i < 0) throw new InvalidOperationException($"Scene {Scene.Id}: missing label '{label}'");
            return i + 1;
        }

        void Step()
        {
            int guard = 0;
            while (guard++ < StepGuard)
            {
                if (pc >= Scene.Commands.Count) { Finish(skipped: false); return; }
                var c = Scene.Commands[pc];
                switch (c.Kind)
                {
                    case NarrativeCmd.Say:
                    case NarrativeCmd.Narrate:
                        ShowLine(c);
                        return;
                    case NarrativeCmd.Choice:
                        ShowChoice(c);
                        return;
                    case NarrativeCmd.Label: pc++; break;
                    case NarrativeCmd.SkipPoint: pc++; break;
                    case NarrativeCmd.Goto: pc = FindLabel(c.Label); break;
                    case NarrativeCmd.IfGoto: pc = host.Check(c.Condition) ? FindLabel(c.Label) : pc + 1; break;
                    case NarrativeCmd.Effect: host.Apply(c.Effect); pc++; break;
                    case NarrativeCmd.End: Finish(skipped: false); return;
                    case NarrativeCmd.Pause:
                        pc++;
                        pauseLeft = c.Ms / 1000f;
                        Phase = RunnerPhase.Pause;
                        return;
                    case NarrativeCmd.Banner:
                        pc++;
                        BannerText = Resolve(c.Text);
                        pauseLeft = c.Ms / 1000f;
                        Phase = RunnerPhase.Banner;
                        return;
                    case NarrativeCmd.Backdrop:
                        BackdropId = c.Label;
                        goto default;
                    default:
                        // stage command
                        pc++;
                        var ev = new StageEvent { Seq = ++stageSeq, Command = c, Blocking = !c.Async };
                        if (Headless) break;
                        stageQueue.Add(ev);
                        if (ev.Blocking) { blockingSeq = ev.Seq; Phase = RunnerPhase.Stage; return; }
                        break;
                }
            }
            throw new InvalidOperationException($"Scene {Scene?.Id}: step guard hit (goto loop?)");
        }

        void ShowLine(NarrativeCommand c)
        {
            var cast = NarrativeCast.Get(c.Speaker);
            Line = new LineView
            {
                SpeakerId = c.Speaker,
                SpeakerName = c.Kind == NarrativeCmd.Narrate ? "" : cast?.DisplayName ?? c.Speaker,
                NameplateColor = cast?.NameplateColor ?? "",
                Portrait = cast != null && cast.Portraits.Contains(c.Portrait) ? c.Portrait : Portraits.Neutral,
                Side = c.Side,
                IsNarration = c.Kind == NarrativeCmd.Narrate,
                FullText = Resolve(c.Text),
                ReadBefore = State.ReadLines.Contains(LineKey()),
            };
            if (State.Settings.TextCps <= 0f) Line.VisibleChars = Line.FullText.Length;
            charAccumulator = 0f;
            autoTimer = 0f;
            MarkRead();
            AddBacklog(Line.SpeakerName, Line.FullText);
            pc++;
            Phase = RunnerPhase.Line;
            // pc already points past the line; AdvanceLine → Step continues from there.
        }

        void ShowChoice(NarrativeCommand c)
        {
            Choices.Clear();
            ChoicePrompt = Resolve(c.Text);
            for (int i = 0; i < c.Options.Count; i++)
            {
                var o = c.Options[i];
                bool ok = host.Check(o.Condition);
                if (!ok && !o.ShowWhenLocked) continue;
                Choices.Add(new ChoiceView { Index = i, Text = Resolve(o.Text), Enabled = ok, LockedReason = ok ? "" : o.LockedReason });
            }
            if (!Choices.Any(x => x.Enabled))
            {
                // nothing selectable: behave like the default path rather than soft-locking the player
                Choices.Clear();
                pc++;
                Step();
                return;
            }
            Phase = RunnerPhase.Choice;
        }

        string LineKey() => Scene.Id + ":" + pc;
        void MarkRead() => State.ReadLines.Add(LineKey());

        void AddBacklog(string speaker, string text, bool isChoice = false, bool isRecap = false)
        {
            backlog.Add(new BacklogEntry { Speaker = speaker, Text = text, IsChoice = isChoice, IsRecap = isRecap });
            if (backlog.Count > BacklogCapacity) backlog.RemoveAt(0);
        }

        void Finish(bool skipped)
        {
            var scene = Scene;
            Phase = RunnerPhase.Idle;
            Line = null;
            Choices.Clear();
            BannerText = "";
            BackdropId = "";
            blockingSeq = -1;
            host.MarkSceneSeen(scene.Id);
            Scene = null;
            SceneEnded?.Invoke(scene, skipped);
        }

        /// <summary>Protagonist variants: "{an:chị|khoa:anh}" and the player's name "{player}".</summary>
        public string Resolve(string text) => ResolveFor(text, host.Protagonist);

        public static string ResolveFor(string text, PlayerId who)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '{')
                {
                    int close = text.IndexOf('}', i);
                    if (close > i)
                    {
                        string body = text.Substring(i + 1, close - i - 1);
                        if (body == "player") { sb.Append(who == PlayerId.An ? "An" : "Khoa"); i = close + 1; continue; }
                        if (body.StartsWith("an:", StringComparison.Ordinal))
                        {
                            int bar = body.IndexOf("|khoa:", StringComparison.Ordinal);
                            if (bar > 0)
                            {
                                sb.Append(who == PlayerId.An ? body.Substring(3, bar - 3) : body.Substring(bar + 6));
                                i = close + 1;
                                continue;
                            }
                        }
                    }
                }
                sb.Append(text[i]);
                i++;
            }
            return sb.ToString();
        }
    }

    /// <summary>Scene library + trigger matching (Stardew event preconditions). One scene plays at a time.</summary>
    public sealed class NarrativeDirector
    {
        readonly Dictionary<string, SceneDefinition> scenes = new Dictionary<string, SceneDefinition>(StringComparer.OrdinalIgnoreCase);
        readonly INarrativeHost host;
        public NarrativeRunner Runner { get; }
        readonly Queue<SceneDefinition> queued = new Queue<SceneDefinition>();

        readonly NarrativeState state;

        public NarrativeDirector(INarrativeHost host, NarrativeState state)
        {
            this.host = host;
            this.state = state;
            Runner = new NarrativeRunner(host, state);
            Runner.SceneEnded += (_, __) => { if (queued.Count > 0) Runner.Start(queued.Dequeue()); };
        }

        public IEnumerable<SceneDefinition> Scenes => scenes.Values;
        public void Register(SceneDefinition s) => scenes[s.Id] = s;
        public void RegisterAll(IEnumerable<SceneDefinition> list) { foreach (var s in list) Register(s); }
        public SceneDefinition Get(string id) => scenes.TryGetValue(id, out var s) ? s : null;

        public bool Eligible(SceneDefinition s) => !(s.Once && host.HasSeenScene(s.Id)) && host.Check(s.Condition);

        /// <summary>Highest-priority eligible scene for this trigger, or null.</summary>
        public SceneDefinition Find(NarrativeTrigger trigger, string arg = "")
            => scenes.Values
                .Where(s => s.Trigger == trigger && (string.IsNullOrEmpty(s.TriggerArg) || string.Equals(s.TriggerArg, arg, StringComparison.OrdinalIgnoreCase)))
                .Where(Eligible)
                .OrderByDescending(s => s.Priority).ThenBy(s => s.Id, StringComparer.Ordinal)
                .FirstOrDefault();

        /// <summary>Fire a trigger: plays the matching scene now, or queues it behind the current one. Returns the scene.</summary>
        public SceneDefinition Fire(NarrativeTrigger trigger, string arg = "")
        {
            // mornings: a waiting quest opener first, then the morning's own scene, and only on a quiet morning a memory
            if (trigger == NarrativeTrigger.DayStart && PlayWaiting(memories: false) is SceneDefinition opener) return opener;
            var s = Find(trigger, arg);
            if (s == null && trigger == NarrativeTrigger.DayStart) return PlayWaiting(memories: true);
            // a quest-start / chapter card unlocked by finishing something waits for the next morning (StoryPacing)
            if (s != null && trigger == NarrativeTrigger.QuestCompleted && StoryPacing.IsOpener(s.Id)) { Defer(s.Id); return null; }
            if (s == null || queued.Contains(s) || Runner.Scene == s) return null;
            if (Runner.InScene) queued.Enqueue(s);
            else Runner.Start(s);
            return s;
        }

        /// <summary>Chained from inside a scene (PlayScene effect): openers wait for a morning, the rest play next.</summary>
        public bool PlayOrDefer(string sceneId)
        {
            if (StoryPacing.IsOpener(sceneId)) { Defer(sceneId); return true; }
            return Play(sceneId);
        }

        public void Defer(string sceneId)
        {
            var s = Get(sceneId);
            if (s == null || (s.Once && host.HasSeenScene(s.Id)) || state.Waiting.Contains(s.Id, StringComparer.OrdinalIgnoreCase)) return;
            state.Waiting.Add(s.Id);
        }

        /// <summary>Plays the oldest waiting opener that is still due (one per morning). Stale ones are dropped.</summary>
        public SceneDefinition PlayWaiting(bool memories)
        {
            for (int i = 0; i < state.Waiting.Count; i++)
            {
                bool isMemory = state.Waiting[i].StartsWith("P0", StringComparison.OrdinalIgnoreCase);
                if (isMemory != memories) continue;
                var s = Get(state.Waiting[i]);
                state.Waiting.RemoveAt(i); i--;
                if (s == null || !Eligible(s)) continue; // stale (seen, or no longer due)
                if (Runner.InScene) queued.Enqueue(s); else Runner.Start(s);
                return s;
            }
            return null;
        }

        public bool Play(string sceneId)
        {
            var s = Get(sceneId);
            if (s == null) return false;
            if (Runner.InScene) { queued.Enqueue(s); return true; }
            return Runner.Start(s);
        }
    }
}
