using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>Chuỗi hoạt ảnh: mỗi vũ khí có đòn riêng, mỗi võ kỹ có dáng riêng đổi theo vũ khí, luôn lùi về clip chung.</summary>
public static class AnimationClipsTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var basic = AnimationClips.BasicAttack("wpn_bua_ta", WeaponArchetype.Smash, 2);
        Check(basic.SequenceEqual(new[] { "attack_wpn_bua_ta_2", "attack_wpn_bua_ta", "attack_smash_2", "attack_smash", "attack" }), "đòn thường: món → loại → chung: " + string.Join(",", basic));
        Check(AnimationClips.BasicAttack(null, WeaponArchetype.None, 9).SequenceEqual(new[] { "attack_unarmed_3", "attack_unarmed", "attack" }), "tay không, combo kẹp 1–3");
        var skill = AnimationClips.Skill("pig.skill.chan_dia", "wpn_bua_ta", WeaponArchetype.Smash, SkillShape.PushCircle);
        Check(skill.Take(5).SequenceEqual(new[] { "skill_chan_dia_wpn_bua_ta", "skill_chan_dia_smash", "skill_chan_dia", "shape_pushcircle_smash", "shape_pushcircle" })
              && skill.Last() == "attack", "võ kỹ: theo món → theo loại → chung → hình dạng → đòn thường");
        Check(AnimationClips.Work("feed").SequenceEqual(new[] { "interact_feed", "interact" }), "việc trại có clip riêng");
        var manifest = AnimationClips.BodyManifest().ToList();
        Check(manifest.Select(m => m.Clip).Distinct().Count() == manifest.Count, "danh sách clip không trùng");
        int activeSkills = MartialSkillCatalog.AllSkills.Count(s => !s.IsPassive);
        Check(manifest.Count(m => m.Clip.StartsWith("skill_")) == activeSkills * 5, "mỗi võ kỹ chủ động: 1 dáng chung + 4 biến thể theo loại vũ khí");
        Check(manifest.Count(m => m.Clip.StartsWith("attack_")) == 12, "4 loại vũ khí × chuỗi 3 đòn");
    }
}
