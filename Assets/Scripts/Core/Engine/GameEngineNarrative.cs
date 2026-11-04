using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>The real narrative host: conditions are GameStateQuery strings, effects go through the normal engine paths.</summary>
    public sealed class EngineNarrativeHost : INarrativeHost
    {
        readonly GameEngine engine;

        static EngineNarrativeHost()
        {
            GameStateQuery.RegisterQueryType("STORY_FLAG", (q, ctx) => q.Length >= 2 && ctx.Engine.NarrativeState.Flags.Contains(q[1]));
            GameStateQuery.RegisterQueryType("STORY_QUEST_DONE", (q, ctx) => q.Length >= 2 && ctx.Engine.Quests.Completed.Contains(q[1]));
            GameStateQuery.RegisterQueryType("STORY_QUEST_ACTIVE", (q, ctx) => q.Length >= 2 && ctx.Engine.Quests.ActiveMain?.Id == q[1]);
            GameStateQuery.RegisterQueryType("SCENE_SEEN", (q, ctx) => q.Length >= 2 && ctx.Engine.TriggerManager.EventsSeen.Contains(q[1]));
        }

        public EngineNarrativeHost(GameEngine engine) { this.engine = engine; }

        public PlayerId Protagonist => engine.Story.ActiveProtagonist;
        public int Day => engine.Clock.CurrentDay;
        public bool Check(string condition) => GameStateQuery.CheckConditions(condition, engine);
        public void MarkSceneSeen(string sceneId) => engine.TriggerManager.EventsSeen.Add(sceneId);
        public bool HasSeenScene(string sceneId) => engine.TriggerManager.EventsSeen.Contains(sceneId);

        public void Apply(NarrativeEffect e)
        {
            switch (e.Kind)
            {
                case NarrativeEffectKind.SetFlag: engine.NarrativeState.Flags.Add(e.Id); break;
                case NarrativeEffectKind.ClearFlag: engine.NarrativeState.Flags.Remove(e.Id); break;
                case NarrativeEffectKind.Gold:
                    if (e.Amount > 0) engine.Ledger.Earn(e.Amount, LedgerReason.ContractReward, "story_" + e.Id);
                    break;
                case NarrativeEffectKind.Item: if (e.Amount > 0) engine.Inventory.AddItem(e.Id, e.Amount); break;
                case NarrativeEffectKind.Signal:
                    if (Enum.TryParse(e.Id, out QuestSignal s)) engine.SignalQuest(s);
                    break;
                case NarrativeEffectKind.Affinity:
                    // Only quest scenes may carry this (user rule: talk and gifts never raise affinity; NarrativeTests enforce it).
                    if (NarrativeTalk.SpecialistFor(e.Id) is NpcSpecialistId id) NpcContracts.AddAffinity(engine, id, e.Amount);
                    break;
                case NarrativeEffectKind.MailNow: engine.Mailbox.DeliverMailNow(e.Id); break;
                case NarrativeEffectKind.MailTomorrow: engine.Mailbox.EnqueueMailForTomorrow(e.Id); break;
                case NarrativeEffectKind.QuestStart: engine.NarrativeState.Flags.Add("quest_open_" + e.Id); break;
                case NarrativeEffectKind.Custom:
                    // Q20 ending choice (Chapter 4): Core finishes the quest itself, so the epilogue never depends on the view
                    if (e.Id.StartsWith(NarrativeContentChapter4.EndingCustomPrefix, System.StringComparison.Ordinal)
                        && System.Enum.TryParse(e.Id.Substring(NarrativeContentChapter4.EndingCustomPrefix.Length), out StoryEndingId chosen))
                    {
                        engine.Quests.ChooseEnding(engine, chosen);
                        break;
                    }
                    engine.RaiseNarrativeCustom(e);
                    break;
                case NarrativeEffectKind.PlayScene: engine.Narrative.PlayOrDefer(e.Id); break;
            }
        }
    }

    /// <summary>Who-talks-about-what: story talk scenes first, then the NPC's daily line (one per day), then the repeat line.</summary>
    public static class NarrativeTalk
    {
        public const string TagDaily = "daily", TagRepeat = "repeat";

        public static NpcSpecialistId? SpecialistFor(string castId) => castId switch
        {
            CastIds.Man => NpcSpecialistId.NongCong,
            CastIds.TamVa => NpcSpecialistId.KySuXayDung,
            CastIds.Dung => NpcSpecialistId.BacSiThuY,
            CastIds.Cuc => NpcSpecialistId.NhaNghienCuuGen,
            CastIds.Le => NpcSpecialistId.NhaBuon,
            CastIds.AnhCa => NpcSpecialistId.QuanLyCa,
            CastIds.OngTon => NpcSpecialistId.ThuKho,
            CastIds.Do => NpcSpecialistId.VeBinhKhien,
            CastIds.Mien => NpcSpecialistId.XaThuNo,
            CastIds.Chot => NpcSpecialistId.KySuChienTruong,
            CastIds.TuTroi => NpcSpecialistId.ThoSanUfo,
            CastIds.Van => NpcSpecialistId.ThichKhachDem,
            _ => null
        };

        public static QuestSignal? TalkSignalFor(string castId) => castId switch
        {
            CastIds.BaSau => QuestSignal.TalkBaSau,
            CastIds.TamVa => QuestSignal.TalkTamVa,
            CastIds.Le => QuestSignal.TalkLe,
            CastIds.OngTon => QuestSignal.TalkOngTon,
            CastIds.Dung => QuestSignal.TalkDung,
            CastIds.Cuc => QuestSignal.TalkCuc,
            CastIds.Man => QuestSignal.TalkMan,
            CastIds.AnhCa => QuestSignal.TalkAnhCa,
            _ => null
        };

        /// <summary>Deterministic per day and NPC (no string.GetHashCode: it differs between runtimes).</summary>
        public static int DailyIndex(int day, string npcId, int count)
        {
            int h = 17;
            foreach (char c in npcId) h = h * 31 + c;
            return count <= 0 ? 0 : (int)((uint)(day * 7919 + h) % (uint)count);
        }
    }

    public partial class GameEngine
    {
        public NarrativeState NarrativeState { get; private set; } = new NarrativeState();
        NarrativeDirector narrative;
        /// <summary>Scene library + runner. The view drives <see cref="NarrativeDirector.Runner"/>; Core fires triggers.</summary>
        public NarrativeDirector Narrative => narrative ??= CreateNarrative();
        /// <summary>Custom effects for the presentation layer (open a popup, start a puzzle, play a reflection).</summary>
        public event Action<NarrativeEffect> NarrativeCustom;
        internal void RaiseNarrativeCustom(NarrativeEffect e) => NarrativeCustom?.Invoke(e);

        NarrativeDirector CreateNarrative()
        {
            var d = new NarrativeDirector(new EngineNarrativeHost(this), NarrativeState);
            d.RegisterAll(StoryPacing.Apply(NarrativeLibrary.All()));
            return d;
        }

        /// <summary>Fire a narrative trigger (the view calls this on map enter; Core calls it on day start, signals, waves).</summary>
        public SceneDefinition FireNarrative(NarrativeTrigger trigger, string arg = "") => Narrative.Fire(trigger, arg);

        /// <summary>
        /// Talk to a cast member: a pending story talk scene, else today's line, else the repeat line. Talking marks the
        /// NPC as talked-to today and sends the quest Talk signal; it never changes affinity.
        /// </summary>
        public SceneDefinition TalkTo(string castId)
        {
            var d = Narrative;
            if (d.Runner.InScene || string.IsNullOrEmpty(castId)) return null;
            var mine = d.Scenes.Where(s => s.Trigger == NarrativeTrigger.Talk && string.Equals(s.TriggerArg, castId, StringComparison.OrdinalIgnoreCase) && d.Eligible(s)).ToList();
            var pick = mine.Where(s => s.Tag != NarrativeTalk.TagDaily && s.Tag != NarrativeTalk.TagRepeat)
                .OrderByDescending(s => s.Priority).ThenBy(s => s.Id, StringComparer.Ordinal).FirstOrDefault();
            bool talkedToday = NarrativeState.TalkedOnDay.TryGetValue(castId, out int day) && day == Clock.CurrentDay;
            if (pick == null && talkedToday) pick = mine.FirstOrDefault(s => s.Tag == NarrativeTalk.TagRepeat);
            if (pick == null)
            {
                var pool = mine.Where(s => s.Tag == NarrativeTalk.TagDaily).OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
                if (pool.Count > 0) pick = pool[NarrativeTalk.DailyIndex(Clock.CurrentDay, castId, pool.Count)];
            }
            NarrativeState.TalkedOnDay[castId] = Clock.CurrentDay;
            if (NarrativeTalk.TalkSignalFor(castId) is QuestSignal sig) SignalQuest(sig);
            if (pick != null) d.Runner.Start(pick);
            return pick;
        }

        /// <summary>Boss waves that ended, win or lose (story rule SB:337: a boss counts from its after-wave record).</summary>
        public int BossWavesFought { get; set; }
        public int MajorWavesFought { get; set; }
        /// <summary>Cycle number in which the latest major wave ended (Q20 opens on the next major wave from cycle 20).</summary>
        public int LastMajorWaveCycle { get; set; }

        /// <summary>
        /// Call once per ended wave, AFTER the wave's own bookkeeping (MajorWavesSurvived, boss count), so quest steps that
        /// read those counters complete on this wave. <paramref name="cycleOfWave"/> is the cycle the wave belonged to
        /// (capture it before the clock closes the cycle). Story counters only; rewards stay where they are.
        /// </summary>
        public void RecordWaveEndedForStory(bool major, bool boss, int cycleOfWave)
        {
            if (major)
            {
                MajorWavesFought += 1;
                LastMajorWaveCycle = cycleOfWave;
                if (boss) BossWavesFought += 1;
            }
            Quests.Evaluate(this);
            FireNarrative(NarrativeTrigger.WaveEnded, boss ? "boss" : major ? "major" : "minor");
        }

        /// <summary>
        /// What the UI shows about a villager or mercenary next to the dialogue box / in the journal (read-only).
        /// Affinity comes only from personal quests, favours and finished contracts (ADR-0026); talking never changes it.
        /// </summary>
        public NpcRelationView RelationOf(string castId)
        {
            var cast = NarrativeCast.Get(castId);
            var v = new NpcRelationView { CastId = castId ?? "", Name = cast?.DisplayName ?? castId ?? "", Role = cast?.Role ?? "" };
            if (NarrativeTalk.SpecialistFor(castId) is NpcSpecialistId id)
            {
                var r = NpcRelations.Get(id);
                v.Hireable = true;
                v.Unlocked = r.Unlocked;
                v.Affinity = r.Affinity;
                v.Status = NpcContracts.Status(this, id);
                v.Label = !r.Unlocked ? "Chưa mở hảo cảm" : r.Affinity >= 100f ? "Tri kỷ" : $"Hảo cảm {r.Affinity:0}/100";
            }
            else v.Label = "";
            return v;
        }

        internal NarrativeSaveData CaptureNarrative() => new NarrativeSaveData
        {
            Flags = NarrativeState.Flags.OrderBy(f => f, StringComparer.Ordinal).ToList(),
            ReadLines = NarrativeState.ReadLines.OrderBy(f => f, StringComparer.Ordinal).ToList(),
            TalkedOnDay = new Dictionary<string, int>(NarrativeState.TalkedOnDay),
            Waiting = new List<string>(NarrativeState.Waiting),
            Settings = NarrativeState.Settings,
            BossWavesFought = BossWavesFought,
            MajorWavesFought = MajorWavesFought,
            LastMajorWaveCycle = LastMajorWaveCycle,
        };

        internal void RestoreNarrative(NarrativeSaveData data)
        {
            if (data == null) return;
            NarrativeState.Flags.Clear();
            foreach (var f in data.Flags ?? new List<string>()) if (!string.IsNullOrWhiteSpace(f)) NarrativeState.Flags.Add(f);
            NarrativeState.ReadLines.Clear();
            foreach (var f in data.ReadLines ?? new List<string>()) if (!string.IsNullOrWhiteSpace(f)) NarrativeState.ReadLines.Add(f);
            NarrativeState.TalkedOnDay.Clear();
            foreach (var kv in data.TalkedOnDay ?? new Dictionary<string, int>()) NarrativeState.TalkedOnDay[kv.Key] = kv.Value;
            if (data.Settings != null) NarrativeState.Settings = data.Settings;
            NarrativeState.Waiting = new List<string>(data.Waiting ?? new List<string>());
            // older saves: a defeated boss was certainly fought
            BossWavesFought = Math.Max(data.BossWavesFought, DeepMechanics.CycleBossesDefeatedCount);
            MajorWavesFought = Math.Max(data.MajorWavesFought, MajorWavesSurvived);
            LastMajorWaveCycle = data.LastMajorWaveCycle;
        }
    }

    public sealed class NpcRelationView
    {
        public string CastId = "", Name = "", Role = "";
        /// <summary>False for story-only people (Bà Sáu, Anh Tư, Bí nhân): no affinity bar.</summary>
        public bool Hireable;
        public bool Unlocked;
        public float Affinity;
        /// <summary>"Chưa mở hảo cảm" / "Hảo cảm 37/100" / "Tri kỷ" ("" when not hireable).</summary>
        public string Label = "";
        /// <summary>NpcContracts.Status: contract, personal-quest step or hire threshold.</summary>
        public string Status = "";
    }

    [Serializable]
    public sealed class NarrativeSaveData
    {
        public List<string> Flags = new List<string>();
        public List<string> ReadLines = new List<string>();
        public Dictionary<string, int> TalkedOnDay = new Dictionary<string, int>();
        public NarrativeSettings Settings = new NarrativeSettings();
        public List<string> Waiting = new List<string>();
        public int BossWavesFought;
        public int MajorWavesFought;
        public int LastMajorWaveCycle;
    }
}
