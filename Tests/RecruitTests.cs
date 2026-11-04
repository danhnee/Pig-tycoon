using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD §11: recruit venues and costs; wages; pet slots by farm level.</summary>
internal static class RecruitTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        var e = new GameEngine(seed: 9);
        e.Farm.AddGold(5000);
        // ADR-0026 (người dùng 02/11): mở khóa chỉ cho phép thuê — phải xong nhiệm vụ riêng và đủ hảo cảm; lương theo hợp đồng
        var blocked = RecruitmentRules.Hire(e, NpcSpecialistId.BacSiThuY);
        Check(!blocked.success && blocked.message.StartsWith("Chưa xong nhiệm vụ riêng"), "the vet needs its personal quest first");
        var rel = e.NpcRelations.Get(NpcSpecialistId.BacSiThuY);
        rel.QuestStage = 1; rel.Affinity = 25f;
        int before = e.Farm.Gold;
        var vet = RecruitmentRules.Hire(e, NpcSpecialistId.BacSiThuY);
        Check(vet.success, "the vet is hired at the day market: " + vet.message);
        var c = e.NpcRelations.ContractOf(NpcSpecialistId.BacSiThuY)!;
        Check(c.Kind == ContractKind.ChuKyNho && e.Farm.Gold == before - c.Deposit && c.Deposit == (int)Math.Round(c.TotalPrice * 0.30), "1 small cycle, 30% deposit");
        Check(e.PetsAndNpcs.HiredNpcs.First(n => n.Id == NpcSpecialistId.BacSiThuY).DailyWageGold == 0, "no daily wage under a contract");
        Check(!RecruitmentRules.Hire(e, NpcSpecialistId.BacSiThuY).success, "no double hire");
        Check(!RecruitmentRules.Hire(e, NpcSpecialistId.QuanLyCa).success, "the shift manager is hired at night only");
        Check(PetNpcAndOccultManager.PetSlotsFor(2) == 0 && PetNpcAndOccultManager.PetSlotsFor(3) == 2
              && PetNpcAndOccultManager.PetSlotsFor(8) == 4 && PetNpcAndOccultManager.PetSlotsFor(14) == 6, "GDD pet slots 2/4/6 at levels 3/8/14");
        Check(!e.PetsAndNpcs.TryEquipPet(PetId.MeoKho, 1), "no pet slot before farm level 3");
        Check(e.PetsAndNpcs.TryEquipPet(PetId.MeoKho, 3), "a pet at farm level 3");
    }
}
