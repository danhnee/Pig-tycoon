using System; using System.Linq; using System.Text; using PigTycoon.Core;
var sb = new StringBuilder();
var scenes = NarrativeLibrary.All().ToList();
var cmds = scenes.SelectMany(s => s.Commands).ToList();
sb.AppendLine("# Narrative asset manifest (generated from Core content, 2 Nov 2026)\n");
sb.AppendLine($"Scenes: {scenes.Count}. Regenerate: `dotnet run --project tools/narrative/manifest -- Docs/plans/2026-11-02-narrative-asset-manifest.md`.\n");
sb.AppendLine("## 1. Portraits — `UI/Portraits/<cast>_<portrait>.png` (missing → neutral)\n");
foreach (var g in NarrativeCast.PortraitManifest().GroupBy(x => x.character))
{
    var used = cmds.Where(c => c.Kind == NarrativeCmd.Say && c.Speaker == g.Key).Select(c => c.Portrait).Distinct().ToHashSet();
    sb.AppendLine($"- **{g.Key}** ({NarrativeCast.Get(g.Key).DisplayName}): " + string.Join(", ", g.Select(x => used.Contains(x.portrait) ? x.portrait : x.portrait + " _(chưa dùng)_")));
}
sb.AppendLine("\n## 2. Backdrops — pass 1 = 22 base plates (priority order); pass 2 = detailed variants (fallback to base)\n");
var used2 = NarrativeBackdrops.UsedIds().ToList();
foreach (var b in NarrativeBackdrops.BaseSet)
{
    var variants = used2.Where(u => NarrativeBackdrops.Base(u) == b && u != b).ToList();
    sb.AppendLine($"- **{b}**" + (variants.Count > 0 ? ": " + string.Join(", ", variants) : ""));
}
void List(string title, Func<NarrativeCommand, bool> pred, Func<NarrativeCommand, string> sel)
{
    var ids = cmds.Where(pred).Select(sel).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList();
    sb.AppendLine($"\n## {title} ({ids.Count})\n");
    sb.AppendLine(string.Join(", ", ids.Select(x => "`" + x + "`")));
}
List("3. Music ids (\"\" = silence)", c => c.Kind == NarrativeCmd.Music, c => c.Label);
List("4. Sfx ids", c => c.Kind == NarrativeCmd.Sfx, c => c.Label);
List("5. Emotes", c => c.Kind == NarrativeCmd.Emote, c => c.Label);
List("6. Custom effects (reflection:/notebook:/tutorial:/goto_map:)", c => c.Kind == NarrativeCmd.Effect && c.Effect.Kind == NarrativeEffectKind.Custom, c => c.Effect.Id);
List("7. Stage marks used", c => (c.Kind == NarrativeCmd.ActorPlace || c.Kind == NarrativeCmd.ActorMove || c.Kind == NarrativeCmd.Camera), c => c.Label);
System.IO.File.WriteAllText(args[0], sb.ToString());
Console.WriteLine("scenes " + scenes.Count);
if (args.Length > 1)
{
    var an = new StringBuilder();
    an.AppendLine("# Animation request list (generated from Core AnimationClips)");
    an.AppendLine();
    an.AppendLine("Each clip: 4 facings (down/left/up/right), 72×84 cells, same pivot as idle. The runtime plays the first clip of its fallback chain that exists, so clips can land one at a time.");
    an.AppendLine();
    an.AppendLine("## Body (An and Khoa each)");
    an.AppendLine();
    an.AppendLine("| P | Clip | Min frames | Loop | Shows |");
    an.AppendLine("|---|---|---|---|---|");
    foreach (var r in AnimationClips.BodyManifest().OrderBy(r => r.Priority))
        an.AppendLine($"| {r.Priority} | `{r.Clip}` | {r.MinFrames} | {(r.Loop ? "yes" : "")} | {r.Purpose} |");
    an.AppendLine();
    an.AppendLine("## Weapon overlay layers (per protagonist per weapon: `an__wpn_dao_rua`, …)");
    an.AppendLine();
    foreach (var w in EquipmentAndTacticalCatalog.AllEquipmentItems.Where(i => i.Slot == EquipSlot.Weapon))
        an.AppendLine($"- **{w.Name}** (`{w.Id}`, {AnimationClips.Tag(w.Archetype)}): " + string.Join(", ", AnimationClips.WeaponOverlayClips(w.Archetype).Select(c => "`" + c + "`")));
    an.AppendLine();
    an.AppendLine("Weapon-specific basic strings (optional, they beat the class version): `attack_<weaponId>_1..3`. A skill on one specific weapon: `skill_<skill>_<weaponId>`.");
    System.IO.File.WriteAllText(args[1], an.ToString());
    Console.WriteLine("anim clips " + AnimationClips.BodyManifest().Count());
}
