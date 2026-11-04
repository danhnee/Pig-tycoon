using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 12.1: ngoại hình theo từng ô (paper-doll), không có "bộ" — đổi một món chỉ đổi lớp của ô đó.</summary>
public static class EquipmentAppearanceTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var c = new CharacterData(PlayerId.An);
        PlayerRoster.Apply(c, PlayerId.An);
        foreach (var slot in Enum.GetValues(typeof(EquipSlot)).Cast<EquipSlot>()) c.Equipment[slot] = null;
        c.EquipItem(EquipmentAndTacticalCatalog.Find("wpn_dao_rua")!);
        c.EquipItem(EquipmentAndTacticalCatalog.Find("armor_ao_mua_dau")!);
        c.EquipItem(EquipmentAndTacticalCatalog.Find("boot_giay_dinh#III")!);

        var down = EquipmentAppearance.Layers(c, CardinalDirection.South);
        Check(down.Count == 3, "3 món mặc → 3 lớp, ô trống không vẽ");
        Check(down.Select(l => l.Family).SequenceEqual(new[] { "an__boot_giay_dinh", "an__armor_ao_mua_dau", "an__wpn_dao_rua" }),
            "thứ tự nhìn xuống: giày dưới, giáp giữa, vũ khí trên cùng: " + string.Join(",", down.Select(l => l.Family)));
        Check(down.Single(l => l.Slot == EquipSlot.Boots).QualityFamily == "an__boot_giay_dinh__III", "lớp theo phẩm chất có họ riêng");

        // swapping only the body armor changes only that layer
        c.EquipItem(EquipmentAndTacticalCatalog.Find("armor_giap_luoi_thep")!);
        var after = EquipmentAppearance.Layers(c, CardinalDirection.South);
        Check(after.Single(l => l.Slot == EquipSlot.BodyArmor).Family == "an__armor_giap_luoi_thep", "đổi giáp thân → đổi lớp giáp");
        Check(after.Where(l => l.Slot != EquipSlot.BodyArmor).Select(l => l.Family)
              .SequenceEqual(down.Where(l => l.Slot != EquipSlot.BodyArmor).Select(l => l.Family)), "các ô khác giữ nguyên (không có bộ)");

        // facing away: the weapon goes behind the body
        Check(EquipmentAppearance.Order(EquipSlot.Weapon, CardinalDirection.North) < 0 && EquipmentAppearance.Order(EquipSlot.Weapon, CardinalDirection.South) > 0,
            "quay lưng: vũ khí sau thân; nhìn xuống: trước thân");
        // 4 Nov: weapon shows only when fighting (Stardew-style); worn pieces always show
        foreach (var clip in new[] { "idle", "walk", "run", "interact_feed", "horn", "whistle", "dodge", "hit", "" })
            Check(!EquipmentAppearance.Visible(EquipSlot.Weapon, clip), "đi/đứng/làm việc: tay không, không vẽ vũ khí (" + clip + ")");
        foreach (var clip in new[] { "attack", "attack_slash_2", "skill_thien_la_slash", "counter" })
            Check(EquipmentAppearance.Visible(EquipSlot.Weapon, clip), "ra đòn: hiện vũ khí (" + clip + ")");
        Check(EquipmentAppearance.Visible(EquipSlot.BodyArmor, "walk") && EquipmentAppearance.Visible(EquipSlot.Boots, "idle"), "giáp, giày luôn hiện");
        Check(EquipmentAppearance.AllFamilies().Count() == 2 * EquipmentAndTacticalCatalog.AllEquipmentItems.Count, "danh sách art: mỗi món × 2 nhân vật");
    }
}
