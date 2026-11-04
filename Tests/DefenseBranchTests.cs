using System;
using PigTycoon.Core;

/// <summary>GDD 16.1: nhánh nâng cấp công trình tấn công — đúng tên, đúng số, đổi băng đạn khi chọn, đọc được mã cũ.</summary>
public static class DefenseBranchTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var baseNo = DefenseStructureSpecs.Get("NoXuyenVan")!;
        var xich = DefenseBranches.For("NoXuyenVan", "1A_XichLien")!;
        Check(xich.ChainTargets == 3 && xich.BindEveryNth == 5 && xich.BindSeconds == 1f && xich.AmmoCapacity == 40, "Xích Liên: 3 mục tiêu, 5 mũi trói 1 giây, chứa 40");
        Check(baseNo.ChainTargets == 0 && baseNo.AmmoCapacity == 50, "bảng gốc không bị đổi theo nhánh");
        var troi = DefenseBranches.For("NoXuyenVan", "1C")!;
        Check(troi.Targets == StructureTargets.GroundAndAir && troi.AmmoCapacity == 36 && troi.Pierce == 1, "Nỏ Trời: bắn bay thấp, chứa 36, mất xuyên");
        Check(DefenseBranches.For("NoXuyenVan", "1B")!.FireZoneM == 2f && DefenseBranches.For("NoXuyenVan", "1B")!.FireZoneS == 4f, "Hỏa Tiễn: vùng cháy 2 m 4 giây");
        Check(DefenseBranches.For("ThapHoQuang", "2A")!.ChainTargets == 6, "Lưới Điện nảy 6");
        Check(DefenseBranches.For("ThapHoQuang", "2B")!.StunChance == 0.2f && DefenseBranches.For("ThapHoQuang", "2B")!.StunSeconds == 0.8f, "Tê Liệt 20% choáng 0,8 giây");
        var caoAp = DefenseBranches.For("ThapHoQuang", "2C")!;
        Check(caoAp.ChainTargets == 1 && caoAp.Damage == 150f, "Cao Áp 1 tia 150");
        Check(DefenseStructureSpecs.Get("ThapPhongKhongSkyhook")!.ScreenM == 4f && DefenseBranches.For("ThapPhongKhongSkyhook", "3A")!.ScreenM == 6f
              && DefenseBranches.For("ThapPhongKhongSkyhook", "3A")!.ScreenS == 6f, "Màn mảnh gốc 4 m/4 s, Mảnh Vụn 6 m/6 s");
        Check(DefenseBranches.For("ThapPhongKhongSkyhook", "3B")!.GroundedSeconds == 3f, "Móc Trời kéo UFO xuống 3 giây");
        var loiVong = DefenseBranches.For("DaiThienLoi", "4A")!;
        Check(loiVong.Bolts == 12 && loiVong.Damage == 320f, "Lôi Võng 12 × 320");
        Check(DefenseBranches.For("DaiThienLoi", "4B")!.ChargeToFire == 30, "Thu Hồn 30 điện tích");
        var cuong = DefenseBranches.For("DaiThienLoi", "4C")!;
        Check(cuong.Bolts == 8 && cuong.Damage == 600f && cuong.WearPerUse == 25f, "Cuồng Lôi 8 × 600, hao mòn 25");
        var tu = DefenseBranches.For("CuuTieuLoiPhao", "5A")!;
        Check(tu.ManualChargeS == 16f && tu.Damage == 5200f, "Tụ Nhanh 16 giây, 5.200");
        var xuyen = DefenseBranches.For("CuuTieuLoiPhao", "5B")!;
        Check(xuyen.LinePierce && xuyen.Damage == 3500f && xuyen.RangeM == 60f, "Xuyên Tâm 60 m, 3.500 mỗi mục tiêu");
        Check(DefenseBranches.For("CuuTieuLoiPhao", "5C")!.AmmoCapacity == 150, "Dung Lượng chứa 150");
        // a branch code of another structure does nothing
        Check(DefenseBranches.For("NoXuyenVan", "2A")!.ChainTargets == 0, "mã nhánh của tháp khác không áp vào Nỏ");
        // legacy placeholder codes from older saves resolve by slot
        Check(DefenseBranches.Normalize("1A_TocBan") == "1A_XichLien" && DefenseBranches.Normalize("4C_CongHuongGiongBao") == "4C_CuongLoi", "mã cũ đọc theo vị trí");

        // choosing Xích Liên sets the magazine to 40 at once; one branch per structure; needs Phòng Xây Dựng 2
        var e = new GameEngine(seed: 5);
        e.Defense.HallLevel = 1;
        foreach (var kv in e.Defense.BlueprintRegistry) if (!e.Defense.UnlockedBlueprints.Contains(kv.Key)) e.Defense.UnlockedBlueprints.Add(kv.Key);
        e.Defense.ConstructBuilding("NoXuyenVan");
        var no = e.Defense.Buildings.Find(b => b.BlueprintId == "NoXuyenVan")!;
        Check(!e.Defense.SelectAttackTowerUpgradeBranch(no.InstanceId, "1A").success, "Phòng Xây Dựng cấp 1 chưa chọn nhánh");
        e.Defense.HallLevel = 2;
        no.AmmoCurrent = no.AmmoCapacity;
        var res = e.Defense.SelectAttackTowerUpgradeBranch(no.InstanceId, "1A");
        Check(res.success && no.ChosenUpgradeBranch == "1A_XichLien" && no.AmmoCapacity == 40 && no.AmmoCurrent == 40, "chọn Xích Liên: băng đạn 40 ngay: " + res.message);
        Check(res.message.Contains("Xích Liên"), "thông báo có tên nhánh");
        Check(!e.Defense.SelectAttackTowerUpgradeBranch(no.InstanceId, "1B").success, "mỗi công trình chỉ 1 nhánh");
    }
}
