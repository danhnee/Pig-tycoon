using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>ADR-0026: phẩm chất theo vai trò, nhiệm vụ riêng, hảo cảm dài hơi, giá theo năng lực và thị trường, hợp đồng chu kỳ / đợt.</summary>
public static class NpcContractTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void RunTiersAndAffinity()
    {
        // Phẩm chất theo điểm trọng yếu; nghiên cứu và kỹ sư xây dựng ngang nhau
        Check(NpcContracts.Profiles.Count == 12, "12 NPC (7 trại + 5 lính)");
        var gen = NpcContracts.Profile(NpcSpecialistId.NhaNghienCuuGen)!;
        var ks = NpcContracts.Profile(NpcSpecialistId.KySuXayDung)!;
        Check(gen.Tier == RoleTier.Cam && ks.Tier == RoleTier.Cam && gen.Criticality == ks.Criticality, "Nghiên cứu = Kỹ Sư Xây Dựng: cùng Cam, cùng 4,50");
        Check(NpcContracts.Profile(NpcSpecialistId.NongCong)!.Tier == RoleTier.Trang, "Nông Công Trắng");
        foreach (var p in NpcContracts.Profiles)
        {
            var expect = p.Criticality < 1.75f ? RoleTier.Trang : p.Criticality < 2.6f ? RoleTier.Luc : p.Criticality < 3.4f ? RoleTier.Lam : p.Criticality < 4.2f ? RoleTier.Tim : RoleTier.Cam;
            Check(p.Tier == expect, $"{p.Role}: màu khớp điểm trọng yếu");
        }
        // Ngưỡng thuê tối đa 30; lính 0
        Check(NpcContracts.Profiles.Max(NpcContracts.HireAffinity) == 30, "ngưỡng thuê cao nhất 30");
        Check(NpcContracts.Profiles.Where(p => p.IsMercenary).All(p => NpcContracts.HireAffinity(p) == 0), "lính không cần hảo cảm");

        var e = new GameEngine(seed: 41);
        var id = NpcSpecialistId.BacSiThuY;
        Check(NpcContracts.HireBlockedReason(e, id).StartsWith("Chưa xong nhiệm vụ riêng"), "chưa xong nhiệm vụ → không thuê");
        NpcContracts.AddAffinity(e, id, 50f);
        Check(e.NpcRelations.Get(id).Affinity == 0f, "chưa mở thanh thì không có hảo cảm");
        // Nhiệm vụ riêng bước 1: bán 5 heo đã kiểm dịch (tổng thật, không bị chặn bởi luật lộ khóa)
        for (int i = 0; i < 5; i++) e.DeepMechanics.SpecialtyLocks.RecordQuarantinedDayMarketSale();
        Check(NpcContracts.CheckQuest(e, id).Length > 0 && e.NpcRelations.Get(id).Affinity == 10f, "xong bước 1: mở thanh, +10");
        Check(NpcContracts.HireBlockedReason(e, id).StartsWith("Hảo cảm 10/25"), "Tím cần 25");
        // Dải 0–30 ×1: +20 → 30; dải 30–50 ×0,6: +10 → 36
        NpcContracts.AddAffinity(e, id, 20f);
        Check(Math.Abs(e.NpcRelations.Get(id).Affinity - 30f) < 0.01f, "lên 30");
        NpcContracts.AddAffinity(e, id, 10f);
        Check(Math.Abs(e.NpcRelations.Get(id).Affinity - 36f) < 0.01f, "dải 30–50 ×0,6");
        // Cổng 50: chưa xong bước 2 → dừng ở 49
        NpcContracts.AddAffinity(e, id, 100f);
        Check(Math.Abs(e.NpcRelations.Get(id).Affinity - 49f) < 0.01f, "dừng ở 49 khi chưa xong bước 2");
        Check(!NpcContracts.AffinityAllowsTrait(e, id) && NpcContracts.EfficiencyBonus(e, id) == 0f, "dưới 50: chưa mở đặc tính, chưa tăng hiệu suất");
        for (int i = 0; i < 35; i++) e.DeepMechanics.SpecialtyLocks.RecordQuarantinedDayMarketSale();
        NpcContracts.CheckQuest(e, id); // bước 2 (40 heo): +10 gốc; ở dải 30–50 ×0,6 → 49 + 1 (tới 50) rồi ×0,35
        var rel = e.NpcRelations.Get(id);
        Check(rel.QuestStage == 2 && rel.Affinity > 50f && rel.Affinity < 54f, $"qua cổng 50 với hệ số dải mới ({rel.Affinity})");
        Check(NpcContracts.AffinityAllowsTrait(e, id) && Math.Abs(NpcContracts.EfficiencyBonus(e, id) - 0.10f) < 0.001f, "≥ 50: đủ điều kiện đặc tính, +10% hiệu suất");
        NpcContracts.AddAffinity(e, id, 200f);
        Check(Math.Abs(rel.Affinity - 79f) < 0.01f, "cổng 80 cần bước 3");
    }

    public static void RunPricesAndContracts()
    {
        var e = new GameEngine(seed: 42);
        // Giá nền theo GDD: lương ngày × 8; lính = điểm × 655 (Vệ Binh = 3 heo trưởng thành × 491)
        Check(NpcContracts.Profile(NpcSpecialistId.NongCong)!.BasePricePerCycle == 480 && NpcContracts.Profile(NpcSpecialistId.VeBinhKhien)!.BasePricePerCycle == 1473, "giá nền GDD");
        var nong = NpcContracts.Profile(NpcSpecialistId.NongCong)!;
        float f = NpcContracts.MarketFactor(e, nong);
        Check(f >= 0.95f && f <= 1.05f, "Cân Bằng: dao động ±5%");
        e.Economy.DayMarket.Theme = MarketTheme.HoangLoanDich;
        float vet = NpcContracts.MarketFactor(e, NpcContracts.Profile(NpcSpecialistId.BacSiThuY)!);
        Check(vet >= 1.25f && vet <= 1.40f, "Hoảng Loạn Dịch: Bác Sĩ +25–40%");
        e.Economy.DayMarket.Theme = MarketTheme.CanBang;

        // Lính: xong nhiệm vụ là thuê được bằng vàng; theo đợt
        var ve = NpcSpecialistId.VeBinhKhien;
        e.MajorWavesSurvived = 2;
        NpcContracts.OnDay(e);
        Check(e.NpcRelations.Get(ve).Unlocked && NpcContracts.HireBlockedReason(e, ve) == "", "lính thuê được ngay sau nhiệm vụ");
        Check(!NpcContracts.QuoteFor(e, ve, ContractKind.ChuKyLon, 2).Ok, "lính không ký chu kỳ lớn");
        Check(!NpcContracts.QuoteFor(e, ve, ContractKind.TheoDot, 4).Ok, "theo đợt chỉ 3/5/8");
        long gold0 = e.Farm.Gold;
        var signed = NpcContracts.Sign(e, ve, ContractKind.TheoDot, 3);
        Check(signed.ok && e.Farm.Gold == gold0 && e.PetsAndNpcs.HiredNpcs.Exists(n => n.Id == ve && n.DailyWageGold == 0), "theo đợt: không cọc, không lương ngày");
        int wave = e.NpcRelations.ContractOf(ve)!.InstallmentAmount;
        NpcContracts.OnWaveEnded(e, major: false);
        Check(e.Farm.Gold == gold0 - wave && e.NpcRelations.ContractOf(ve)!.InstallmentsLeft == 2, "trả mỗi đợt");
        NpcContracts.OnWaveEnded(e, major: true);
        NpcContracts.OnWaveEnded(e, major: false);
        Check(e.NpcRelations.ContractOf(ve) == null && !e.PetsAndNpcs.HiredNpcs.Exists(n => n.Id == ve) && e.NpcRelations.Get(ve).TermsCompleted == 1, "hết 3 đợt: hết hợp đồng, được công nhận năng lực");
        Check(NpcContracts.MarketFactor(e, NpcContracts.Profile(ve)!) > 1.0f, "công nhận năng lực +3%");

        // NPC Cam: bắt buộc ≥ 2 chu kỳ lớn 13–15 ngày, cọc 30%, trả theo kì
        var gen = NpcSpecialistId.NhaNghienCuuGen;
        var rg = e.NpcRelations.Get(gen);
        rg.QuestStage = 1; rg.Affinity = 30f;
        Check(!NpcContracts.QuoteFor(e, gen, ContractKind.ChuKyNho, 1).Ok && !NpcContracts.QuoteFor(e, gen, ContractKind.ChuKyLon, 1).Ok, "Cam: chỉ chu kỳ lớn, tối thiểu 2");
        var q = NpcContracts.QuoteFor(e, gen, ContractKind.ChuKyLon, 2);
        Check(q.Ok && q.LargeCycleDays.Length == 2 && q.LargeCycleDays.All(d => d >= 13 && d <= 15), "chu kỳ lớn 13–15 ngày");
        Check(q.Deposit == (int)Math.Round(q.Total * 0.30), "cọc 30%");
        e.Farm.Gold = 20000;
        Check(NpcContracts.Sign(e, gen, ContractKind.ChuKyLon, 2).ok && e.Farm.Gold == 20000 - q.Deposit, "ký và trừ cọc");

        // Trần NPC theo cấp trại (GDD 11.1): cấp 1 → 2 NPC
        var nongId = NpcSpecialistId.NongCong;
        var rn = e.NpcRelations.Get(nongId); rn.QuestStage = 1; rn.Affinity = 10f;
        Check(NpcContracts.Sign(e, nongId, ContractKind.ChuKyNho, 1).ok, "NPC thứ 2");
        var tk = NpcSpecialistId.ThuKho; var rt = e.NpcRelations.Get(tk); rt.QuestStage = 1; rt.Affinity = 15f;
        Check(NpcContracts.HireBlockedReason(e, tk).StartsWith("Đã đủ 2 NPC"), "cấp trại 1 tối đa 2 NPC");

        // Không đủ vàng ở kì trả: nghỉ việc, mất cọc, hảo cảm −10
        e.Farm.Gold = 0;
        float affBefore = rn.Affinity;
        NpcContracts.OnWaveEnded(e, major: true);
        Check(e.NpcRelations.ContractOf(nongId) == null && rn.Affinity < affBefore, "thiếu vàng: nghỉ việc, hảo cảm giảm");

        // Gia hạn (người dùng 02/11): thị trường tăng → hảo cảm ≥ 50 giữ giá cũ; thị trường rớt → ép giá được, hảo cảm −5
        var gc = e.NpcRelations.ContractOf(gen)!;
        int locked = gc.UnitPrice;
        gc.UnitPrice = locked - 40;                     // giả lập: thị trường nay định giá cao hơn giá đang thuê
        Check(NpcContracts.RenewalUnit(e, gc, false).pricing == NpcContracts.RenewPricing.Market, "hảo cảm 30: phải theo giá thị trường");
        rg.QuestStage = 2; rg.Affinity = 55f;
        Check(NpcContracts.RenewalUnit(e, gc, false) == (locked - 40, NpcContracts.RenewPricing.Locked), "hảo cảm ≥ 50: giữ giá cũ");
        e.Farm.Gold = 20000;
        int left = gc.InstallmentsLeft;
        Check(NpcContracts.Renew(e, gen, 1).ok && gc.InstallmentsLeft == left + 1 && gc.LargeCycleDays.Length == 3 && gc.UnitPrice == locked - 40, "gia hạn 1 chu kỳ lớn với giá cũ");
        gc.UnitPrice = locked + 60;                     // giả lập: thị trường rớt
        Check(NpcContracts.RenewalUnit(e, gc, false).pricing == NpcContracts.RenewPricing.KeptHigher, "không ép: giữ giá cũ, không mất hảo cảm");
        float a0 = rg.Affinity;
        Check(NpcContracts.Renew(e, gen, 1, forceDown: true).ok && gc.UnitPrice == locked && rg.Affinity < a0, "ép giá xuống: giá thị trường, hảo cảm giảm");

        // Lưu / nạp
        var e2 = new GameEngine(seed: 43);
        e2.RestoreFromSave(e.CreateSaveData());
        Check(e2.NpcRelations.ContractOf(gen)?.Kind == ContractKind.ChuKyLon && e2.NpcRelations.Get(ve).TermsCompleted == 1 && Math.Abs(e2.NpcRelations.Get(gen).Affinity - rg.Affinity) < 0.01f
              && e2.NpcRelations.ContractOf(gen)!.UnitPrice == locked, "hợp đồng, đơn giá & hảo cảm được lưu");
    }
}
