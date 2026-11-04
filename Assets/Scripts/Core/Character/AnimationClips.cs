using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>One requested animation: clip name, what it shows, minimum frames and art priority (1 = first).</summary>
    public sealed class ClipRequest
    {
        public string Clip = "";
        public string Purpose = "";
        public int MinFrames;
        public int Priority;
        public bool Loop;
    }

    /// <summary>
    /// Animation names and fallback chains (user direction 3 Nov 2026: every weapon its own moves, every skill its own
    /// move that varies with the weapon, many high-quality clips). The runtime plays the FIRST clip of a chain that the
    /// sheet family has, so art can be added one clip at a time: a skill on a weapon falls back to the skill on the
    /// weapon class, the skill alone, the weapon's attack, the weapon-class attack, then "attack". Gear overlay layers use
    /// the same chains, so a weapon layer always matches the body's clip.
    /// </summary>
    public static class AnimationClips
    {
        public static string Tag(WeaponArchetype a) => a switch
        {
            WeaponArchetype.Slash => "slash",
            WeaponArchetype.Thrust => "thrust",
            WeaponArchetype.Smash => "smash",
            WeaponArchetype.Ranged => "ranged",
            _ => "unarmed",
        };

        public static string SkillKey(string skillId) =>
            string.IsNullOrEmpty(skillId) ? "" : skillId.StartsWith("pig.skill.") ? skillId.Substring("pig.skill.".Length) : skillId;

        /// <summary>Basic attack, combo step 1–3 (a light three-hit string reads better than one repeated swing).</summary>
        public static List<string> BasicAttack(string weaponBaseId, WeaponArchetype archetype, int comboStep)
        {
            string tag = Tag(archetype);
            int step = comboStep < 1 ? 1 : comboStep > 3 ? 3 : comboStep;
            var chain = new List<string>();
            if (!string.IsNullOrEmpty(weaponBaseId))
            {
                chain.Add($"attack_{weaponBaseId}_{step}");
                chain.Add($"attack_{weaponBaseId}");
            }
            chain.Add($"attack_{tag}_{step}");
            chain.Add($"attack_{tag}");
            chain.Add("attack");
            return chain;
        }

        /// <summary>A martial skill cast with the equipped weapon.</summary>
        public static List<string> Skill(string skillId, string weaponBaseId, WeaponArchetype archetype, SkillShape shape)
        {
            string key = SkillKey(skillId), tag = Tag(archetype);
            var chain = new List<string>();
            if (key.Length > 0)
            {
                if (!string.IsNullOrEmpty(weaponBaseId)) chain.Add($"skill_{key}_{weaponBaseId}");
                chain.Add($"skill_{key}_{tag}");
                chain.Add($"skill_{key}");
            }
            chain.Add($"shape_{shape.ToString().ToLowerInvariant()}_{tag}");
            chain.Add($"shape_{shape.ToString().ToLowerInvariant()}");
            chain.AddRange(BasicAttack(weaponBaseId, archetype, 1));
            return chain;
        }

        /// <summary>Farm work and other one-shot actions ("feed", "water", "clean", "repair", "gather", "carry", "observe").</summary>
        public static List<string> Work(string kind) => string.IsNullOrEmpty(kind)
            ? new List<string> { "interact" }
            : new List<string> { $"interact_{kind}", "interact" };

        public static List<string> Dodge(WeaponArchetype a) => new List<string> { $"dodge_{Tag(a)}", "dodge", "run" };
        public static List<string> Counter(WeaponArchetype a) => new List<string> { $"counter_{Tag(a)}", "counter", $"attack_{Tag(a)}", "attack" };
        public static List<string> Hit(bool heavy) => heavy ? new List<string> { "hit_heavy", "hit" } : new List<string> { "hit_light", "hit" };
        public static List<string> Faint() => new List<string> { "faint", "death" };
        public static List<string> Whistle(bool khoa) => khoa ? new List<string> { "whistle", "interact" } : new List<string> { "horn", "whistle", "interact" };

        /// <summary>
        /// The art request for a protagonist body, in priority order. Frame minimums follow top pixel-art practice
        /// (anticipation → action with smear frame → follow-through → recovery for attacks; contact/passing/up/down for
        /// walks). Every clip has 4 facings (down/left/up/right), 72×84 cells, same pivot.
        /// </summary>
        public static IEnumerable<ClipRequest> BodyManifest()
        {
            ClipRequest R(string c, string p, int f, int pr, bool loop = false) => new ClipRequest { Clip = c, Purpose = p, MinFrames = f, Priority = pr, Loop = loop };
            // P1: locomotion and the basic combat loop
            yield return R("idle", "Đứng thở, vai nhún nhẹ, tóc/áo đung đưa", 8, 1, true);
            yield return R("walk", "Đi: dáng người thật (cao ~56 px, chân ~45%), 2 bước/chu kỳ, bàn chân dang tối đa ~0,4 chiều cao, khung giữa 2 chân chụm, tay đánh, nhún 1–2 px", 8, 1, true);
            yield return R("run", "Chạy: nghiêng tới trước, gối gập, bàn chân dang tối đa ~0,65 chiều cao, có khung bay, tóc/áo bay", 8, 1, true);
            foreach (var a in new[] { WeaponArchetype.Slash, WeaponArchetype.Thrust, WeaponArchetype.Smash, WeaponArchetype.Ranged })
                for (int s = 1; s <= 3; s++)
                    yield return R($"attack_{Tag(a)}_{s}", $"Đòn thường {s}/3 kiểu {Tag(a)}: lấy đà → khung nhòe (smear) → theo đà → thu thế", s == 3 ? 10 : 8, 1);
            yield return R("dodge", "Lướt né: ngồi thấp, bật, lướt, chống tay tiếp đất", 8, 1);
            yield return R("counter", "Phản kích: gạt đòn rồi đánh trả ngay", 8, 1);
            yield return R("hit_light", "Trúng đòn nhẹ: giật đầu/vai 1 nhịp", 4, 1);
            yield return R("hit_heavy", "Trúng đòn nặng: lùi 2 bước, mất thăng bằng", 6, 1);
            yield return R("faint", "Ngất (Chống Chịu về 0): khuỵu gối, ngã nghiêng", 10, 1);
            // P2: farm life (the core loop of the game)
            foreach (var w in new[] { ("feed", "Múc cám đổ máng"), ("water", "Xách xô đổ nước"), ("clean", "Cào dọn chuồng"), ("repair", "Gõ búa vá rào"),
                                      ("gather", "Cúi nhặt vật liệu"), ("carry", "Vác bao trên vai (đi được)"), ("observe", "Ngồi xổm quan sát heo (An)") })
                yield return R($"interact_{w.Item1}", w.Item2, 8, 2, w.Item1 == "carry");
            yield return R("whistle", "Khoa: huýt sáo gom đàn, tay đưa lên miệng", 8, 2);
            yield return R("horn", "Thổi tù và lùa đàn về Nơi Trú", 10, 2);
            yield return R("idle_fidget_1", "Đứng lâu: vươn vai / phủi bụi áo", 12, 2);
            yield return R("idle_fidget_2", "Đứng lâu: An xem sổ hồng / Khoa ghi sổ", 12, 2);
            // P3: one move per skill, each with a weapon-class variant
            foreach (var s in MartialSkillCatalog.AllSkills.Where(x => !x.IsPassive))
            {
                yield return R($"skill_{SkillKey(s.Id)}", $"Võ kỹ {s.Name} ({s.Shape}) — dáng chung", 10, 3);
                foreach (var a in new[] { WeaponArchetype.Slash, WeaponArchetype.Thrust, WeaponArchetype.Smash, WeaponArchetype.Ranged })
                    yield return R($"skill_{SkillKey(s.Id)}_{Tag(a)}", $"{s.Name} với vũ khí {Tag(a)}: cùng nhịp, đổi tay cầm/quỹ đạo", 10, 4);
            }
        }

        /// <summary>Clips each weapon overlay family needs (same names as the body, so the layer follows the body).</summary>
        public static IEnumerable<string> WeaponOverlayClips(WeaponArchetype a)
        {
            yield return "idle"; yield return "walk"; yield return "run";
            for (int s = 1; s <= 3; s++) yield return $"attack_{Tag(a)}_{s}";
            yield return "dodge"; yield return "counter";
            foreach (var s in MartialSkillCatalog.AllSkills.Where(x => !x.IsPassive))
                yield return $"skill_{SkillKey(s.Id)}_{Tag(a)}";
        }
    }
}
