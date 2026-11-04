using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PigTycoon.Core
{
    /// <summary>
    /// Stardew-style pacing for the authored story (4 Nov playtest: "phải ngồi skip hết 5 phút mới vào gameplay"; the
    /// story played as one continuous lecture instead of unfolding through people and days).
    /// <list type="bullet">
    /// <item>A new game opens on the short hand-over scene only (the player signs for the farm and starts playing).
    /// The rest of the prologue (P01–P08) becomes memories: one comes back each morning, skippable.</item>
    /// <item>Openers (chapter cards, quest start scenes, memories) never play back-to-back with the scene that unlocked
    /// them: they wait for the next morning. At most one waiting opener plays per morning.</item>
    /// <item>Scenes the player starts (talking to someone, walking into a place, finishing a step) still play right away.</item>
    /// </list>
    /// Content files are not edited: this pass re-wires triggers when the library is built and the director defers.
    /// </summary>
    public static class StoryPacing
    {
        /// <summary>Scene that opens a new game for each route (the farm hand-over), instead of P01.</summary>
        public static readonly Dictionary<string, string> OpeningScene = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Khoa"] = "P08_khoa_d",
            ["An"] = "P08_an_c",
        };

        static readonly Regex Opener = new Regex(@"^(P0\d|C\d_card|C\d_arrive|Q\d\d_start)", RegexOptions.IgnoreCase);

        /// <summary>True for scenes that introduce something (memory, chapter card, new quest) and so wait for a morning.</summary>
        public static bool IsOpener(string sceneId) =>
            !string.IsNullOrEmpty(sceneId) && Opener.IsMatch(sceneId) && !OpeningScene.Values.Contains(sceneId, StringComparer.OrdinalIgnoreCase);

        /// <summary>Re-wires the library: P01 no longer starts a new game; the hand-over scene does, and queues P01.</summary>
        public static IEnumerable<SceneDefinition> Apply(IEnumerable<SceneDefinition> scenes)
        {
            var list = scenes.ToList();
            foreach (var s in list)
            {
                if (s.Trigger == NarrativeTrigger.NewGame && s.Id.StartsWith("P01", StringComparison.OrdinalIgnoreCase))
                    s.Trigger = NarrativeTrigger.Manual;
                foreach (var kv in OpeningScene)
                {
                    if (!string.Equals(s.Id, kv.Value, StringComparison.OrdinalIgnoreCase)) continue;
                    s.Trigger = NarrativeTrigger.NewGame;
                    s.Condition = "PROTAGONIST " + kv.Key;
                    s.Priority = 10;
                    // the memories begin the next morning
                    var first = list.FirstOrDefault(x => x.Id.StartsWith("P01", StringComparison.OrdinalIgnoreCase)
                                                        && x.Condition.Contains("PROTAGONIST " + kv.Key));
                    if (first != null && !s.Commands.Any(c => c.Kind == NarrativeCmd.Effect && c.Effect?.Id == first.Id))
                        s.Commands.Insert(0, new NarrativeCommand { Kind = NarrativeCmd.Effect,
                            Effect = new NarrativeEffect { Kind = NarrativeEffectKind.PlayScene, Id = first.Id } });
                }
            }
            return list;
        }
    }
}
