using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// Every authored scene (ADR-0027). Canon: Docs/COT_TRUYEN_PIG_TYCOON.md, newest revision 1 Nov 2026 (An route 1B / 5B).
    /// Scene ids: P0x_khoa / P0x_an (prologue), Qxx_* (quest beats), talk_&lt;npc&gt;_* (village talk).
    /// Custom effect ids the view must handle are listed in <see cref="NarrativeCustomIds"/>.
    /// </summary>
    public static class NarrativeLibrary
    {
        public static IEnumerable<SceneDefinition> All() =>
            NarrativeContentStart.Scenes()
                .Concat(NarrativeContentPrologueKhoa.Scenes())
                .Concat(NarrativeContentPrologueAn.Scenes())
                .Concat(NarrativeContentVillage.Scenes())
                .Concat(NarrativeContentChapter1.Scenes())
                .Concat(NarrativeContentChapter2.Scenes())
                .Concat(NarrativeContentChapter3.Scenes())
                .Concat(NarrativeContentChapter4.Scenes());
    }

    /// <summary>Custom effect ids (NarrativeEffectKind.Custom) for the presentation layer.</summary>
    public static class NarrativeCustomIds
    {
        /// <summary>"goto_map:Farm" / "goto_map:Hub_Village" / "goto_map:MarketTown" / "goto_map:Forest_Primeval" — move the player after the scene.</summary>
        public const string GotoMapPrefix = "goto_map:";
        /// <summary>"reflection:&lt;portraitId&gt;" — show the protagonist's reflection (water, window, mirror) with that expression.</summary>
        public const string ReflectionPrefix = "reflection:";
        /// <summary>"notebook:&lt;entryId&gt;" — open Khoa's notebook / An's letter page on that entry.</summary>
        public const string NotebookPrefix = "notebook:";
        /// <summary>"tutorial:&lt;verb&gt;" — show the control hint for move / interact / feed / water / whistle / dodge / counter.</summary>
        public const string TutorialPrefix = "tutorial:";
    }
}
