using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 8.2 and 12: equipment pairs, play style, 24 skills, learning rules, casting costs, hợp kích, Nộ, level and save.</summary>
internal static class MartialProgressionTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    static void Wear(CharacterData c, params string[] ids)
    {
        foreach (var id in ids)
        {
            if (!c.OwnedEquipment.Contains(id)) c.OwnedEquipment.Add(id);
            Check(MartialProgression.Equip(c, id).ok, "equip " + id);
        }
    }

    public static void RunCatalog()
    {
        Check(MartialSkillCatalog.AllSkills.Count == 24, "24 skills");
        foreach (MartialSchool school in Enum.GetValues(typeof(MartialSchool)))
            Check(MartialSkillCatalog.AllSkills.Count(s => s.School == school) == 6, "6 per school");
        var lướt = MartialSkillCatalog.Find("pig.skill.bo_phap_luot")!;
        Check(lướt.SlotType == CombatSlotType.TheCong && lướt.StaminaCost == 8 && lướt.CooldownSeconds == 3 && lướt.ReqValue == 24, "Bộ Pháp Lướt row");
        var voAnh = MartialSkillCatalog.Find("pig.skill.vo_anh_bo")!;
        Check(voAnh.SlotType == CombatSlotType.TheBien && voAnh.RageCost == 70 && voAnh.CooldownSeconds == 60 && voAnh.IsUltimate, "Vô Ảnh Bộ row");
        Check(MartialSkillCatalog.Find("pig.skill.hoi_khi")!.IsPassive, "Hồi Khí passive");
        var phanChan = MartialSkillCatalog.Find("pig.skill.phan_chan")!;
        Check(phanChan.HpCost == 6 && phanChan.ReqValue2 == 28, "Phản Chấn 12 STA/6 HP, STR 34 + RES 28");

        Check(EquipmentAndTacticalCatalog.AllEquipmentItems.Count == 16, "16 pieces in 12.3");
        Check(EquipmentAndTacticalCatalog.Find("wpn_no_tay")!.WeightScore == 2, "Nỏ Tay is Vừa");
        Check(EquipmentAndTacticalCatalog.Find("pants_quan_giap_xich")!.WeightScore == 3, "Quần Giáp Xích is Nặng");
        Check(EquipmentAndTacticalCatalog.Find("wpn_bua_ta")!.BaseDamage == 48, "Búa Tạ 48");
    }

    public static void RunPairsAndStyle()
    {
        var c = new CharacterData(PlayerId.Khoa);
        Check(!c.IsMartialSlotUnlocked(CombatSlotType.TheCong), "nothing worn: Công locked");
        Wear(c, "wpn_dao_rua");
        Check(!c.IsMartialSlotUnlocked(CombatSlotType.TheCong), "weapon alone does not open Công");
        Wear(c, "boot_guoc_gio");
        Check(c.IsMartialSlotUnlocked(CombatSlotType.TheCong) && !c.IsMartialSlotUnlocked(CombatSlotType.TheThu), "weapon + boots open Công only");

        // light build: 6 pieces of weight 1 → Khinh Thân (6–9)
        Wear(c, "armor_ao_mua_dau", "pants_quan_da_lot_bong", "brc_vong_cach_dien", "nck_coi_chan_heo");
        Check(c.GetTotalWeightScore() == 6 && c.GetPlaystyle() == "KhinhThân", "all light = Khinh Thân");
        Check(Math.Abs(c.GetMoveSpeedMultiplier() - 1.08f) < 0.001f, "Khinh Thân +8% run");

        // heavy build: Búa Tạ 3 + Giáp Lưới Thép 3 + Quần Giáp Xích 3 + rest light = 12 → Cân Bằng; add Giày Đinh 2 → 13
        Wear(c, "wpn_bua_ta", "armor_giap_luoi_thep", "pants_quan_giap_xich");
        Check(c.GetTotalWeightScore() == 12 && c.GetPlaystyle() == "CânBằng", "12 = Cân Bằng");
        Wear(c, "boot_giay_dinh", "brc_vong_huyet_nha");
        Check(c.GetTotalWeightScore() == 14 && c.GetPlaystyle() == "TrọngGiáp", "14 = Trọng Giáp");
        Check(c.BonusStats.Str == 1, "Vòng Huyết Nha STR thưởng +1 shows as bonus");

        // incoming damage = Gốc × 100/(100+Thủ) × (1 − RES gốc × 0.5%)
        float def = c.GetTotalEquipmentDefense();
        float expected = 50f * 100f / (100f + def) * (1f - c.BaseStats.Res * 0.005f);
        var hit = c.ProcessIncomingAttack(50f, isActiveParry: false);
        Check(Math.Abs(hit.MitigatedDamage - (float)Math.Round(expected, 1)) < 0.11f, $"GDD damage formula ({hit.MitigatedDamage} vs {expected})");
        Check(hit.RageGained == 0, "taking hits gives no Nộ");
    }

    public static void RunLearningAndCasting()
    {
        var e = new GameEngine(seed: 3);
        var c = e.Character;
        PlayerRoster.Apply(c, PlayerId.Khoa); // STR 24, main school B
        Check(c.PrimarySchool == MartialSchool.TrongKich_B, "Khoa = Trọng Kích");
        Check(c.Equipment[EquipSlot.Weapon]?.Id == "wpn_dao_rua", "new game: Dao Rựa");
        Check(!c.IsMartialSlotUnlocked(CombatSlotType.TheCong), "new game: no skill slot yet");
        Check(c.LearnedSkills.Count == 0, "no innate skills (GDD 8)");

        var phaGiap = MartialSkillCatalog.Find("pig.skill.pha_giap")!;
        Check(!MartialProgression.CanLearn(c, phaGiap).ok, "needs Điểm Võ");
        c.MartialPoints = 10;
        Check(MartialProgression.RequiredStat(c, phaGiap, out _) == 23, "main school −15%: 26 → 23");
        e.Farm.Gold = 5000;
        Check(MartialProgression.Learn(e, phaGiap.Id).ok && e.Farm.Gold == 4200 && c.MartialPoints == 0, "Cấp 1 = 800G + 10 Điểm Võ");
        Check(MartialProgression.SlottedId(c, CombatSlotType.TheCong) == phaGiap.Id, "first learned skill goes into its slot");

        // other school without a Bí Kíp, linked school stat floors
        c.MartialPoints = 200;
        Check(!MartialProgression.CanLearn(c, MartialSkillCatalog.Find("pig.skill.bo_phap_luot")!).ok, "A is not linked to B: needs Bí Kíp");
        Check(!MartialProgression.CanLearn(c, MartialSkillCatalog.Find("pig.skill.moc_keo")!).ok, "C is not B's linked school (ring is one-way)");
        var truBo = MartialSkillCatalog.Find("pig.skill.tru_bo")!; // B → D is the link
        Check(MartialProgression.Relation(MartialSchool.TrongKich_B, MartialSchool.SinhTon_D) == SchoolRelation.Linked, "B uses B and D");
        Check(MartialProgression.RequiredStat(c, truBo, out _) == 30, "linked Cấp 1 needs RES 30");
        Check(MartialProgression.LearnCost(c, truBo).gold == 1000, "linked fee +25%");
        Check(!MartialProgression.CanLearn(c, MartialSkillCatalog.Find("pig.skill.thien_la")!).ok, "ultimate only in main school");

        // casting: slot locked until boots are worn
        double t = 100;
        Check(!MartialProgression.Cast(c, CombatSlotType.TheCong, t).Ok, "Công locked without boots");
        Wear(c, "boot_giay_dinh");
        c.Stamina.CurrentStamina = 100f;
        var r = MartialProgression.Cast(c, CombatSlotType.TheCong, t);
        Check(r.Ok && r.Skill == phaGiap, "Phá Giáp cast");
        // Dao Rựa(1)+Giày Đinh(2)+4 empty(4) = 7 → Khinh Thân: B mismatched +15%
        Check(Math.Abs(r.StaminaPaid - (float)Math.Round(9 * 1.15f, 1)) < 0.05f, $"style mismatch +15% ({r.StaminaPaid})");
        Check(!MartialProgression.Cast(c, CombatSlotType.TheCong, t + 1).Ok, "5 s cooldown (per skill)");
        Check(MartialProgression.Cast(c, CombatSlotType.TheCong, t + 5.01).Ok, "ready after 5 s");

        // Thế Biến uses Nộ
        c.LearnedSkills.Add("pig.skill.neo_kich");
        c.ActiveMartialSkills[CombatSlotType.TheBien] = "pig.skill.neo_kich";
        Wear(c, "brc_vong_huyet_nha", "nck_coi_chan_heo");
        c.Rage = 30;
        Check(!MartialProgression.Cast(c, CombatSlotType.TheBien, t + 10).Ok, "Neo Kích needs 40 Nộ");
        c.Rage = 45;
        Check(MartialProgression.Cast(c, CombatSlotType.TheBien, t + 10).Ok && c.Rage == 5, "pays 40 Nộ");

        // Thế Thủ Huyết Tế: Trụ Bộ 6 STA / 3 HP
        c.LearnedSkills.Add("pig.skill.tru_bo");
        c.ActiveMartialSkills[CombatSlotType.TheThu] = "pig.skill.tru_bo";
        Wear(c, "armor_giap_da_thuoc", "pants_quan_giap_xich");
        c.Stamina.CurrentStamina = 2f;
        float guard = c.CurrentGuard;
        var thu = MartialProgression.Cast(c, CombatSlotType.TheThu, t + 20);
        Check(thu.Ok && thu.HpPaid == 3 && Math.Abs(c.CurrentGuard - (guard - 3f)) < 0.01f && MartialProgression.IsUnstoppable(c, t + 21), "Huyết Tế pays 3 Chống Chịu, 3 s unstoppable");
    }

    public static void RunCombos()
    {
        var c = new CharacterData(PlayerId.An);
        Wear(c, "wpn_dao_rua", "boot_guoc_gio", "armor_ao_mua_dau", "pants_quan_da_lot_bong", "brc_vong_cach_dien", "nck_coi_chan_heo");
        c.Stamina.CurrentStamina = 200f;
        c.LearnedSkills.AddRange(new[] { "pig.skill.bo_phap_luot", "pig.skill.moc_keo", "pig.skill.tru_bo", "pig.skill.doan_anh_tram", "pig.skill.phan_chan", "pig.skill.ho_than" });

        c.ActiveMartialSkills[CombatSlotType.TheCong] = "pig.skill.bo_phap_luot";
        Check(MartialProgression.Cast(c, CombatSlotType.TheCong, 10).Ok, "lướt");
        c.ActiveMartialSkills[CombatSlotType.TheCong] = "pig.skill.moc_keo";
        c.SkillReadyAt.Clear();
        var anhMoc = MartialProgression.Cast(c, CombatSlotType.TheCong, 11.5);
        Check(anhMoc.Combo == "Ảnh Móc" && anhMoc.RangeMultiplier == 2f, "Ảnh Móc doubles the pull within 2 s");

        c.SkillReadyAt.Clear();
        c.ActiveMartialSkills[CombatSlotType.TheCong] = "pig.skill.bo_phap_luot";
        MartialProgression.Cast(c, CombatSlotType.TheCong, 20);
        c.SkillReadyAt.Clear();
        c.ActiveMartialSkills[CombatSlotType.TheCong] = "pig.skill.moc_keo";
        Check(MartialProgression.Cast(c, CombatSlotType.TheCong, 23).Combo == "", "outside 2 s: no combo");

        c.ActiveMartialSkills[CombatSlotType.TheThu] = "pig.skill.phan_chan";
        Check(MartialProgression.Cast(c, CombatSlotType.TheThu, 30).Ok, "Phản Chấn");
        Check(MartialProgression.TryCounter(c, 30.2, 20f, out float back) && Math.Abs(back - 30f) < 0.01f, "counter reflects 150%");
        c.ActiveMartialSkills[CombatSlotType.TheThu] = "pig.skill.ho_than";
        c.SkillReadyAt.Clear();
        var shield = MartialProgression.Cast(c, CombatSlotType.TheThu, 31);
        Check(shield.Combo == "Phản Thủ" && Math.Abs(c.ShieldHp - 60f) < 0.01f, "Phản Thủ: shield 40 × 1.5");
        Check(Math.Abs(c.AbsorbWithShield(25f)) < 0.01f && Math.Abs(c.ShieldHp - 35f) < 0.01f, "shield soaks damage");
    }

    /// <summary>GDD 12.1–12.6 phối: one-way ring, each school's hợp kích, Song Thế 4th slot, lướt by play style, đổi phái, túi nhanh.</summary>
    public static void RunPhoi()
    {
        Check(MartialProgression.Relation(MartialSchool.TocBo_A, MartialSchool.KhongChe_C) == SchoolRelation.Linked
              && MartialProgression.Relation(MartialSchool.KhongChe_C, MartialSchool.TocBo_A) == SchoolRelation.Other
              && MartialProgression.Relation(MartialSchool.SinhTon_D, MartialSchool.TocBo_A) == SchoolRelation.Linked, "ring A→C→B→D→A");
        Check(MartialProgression.SchoolCombo(MartialSchool.KhongChe_C).name == "Trói Chùy", "C owns Trói Chùy");

        var c = new CharacterData(PlayerId.An);
        Wear(c, "wpn_dao_rua", "boot_guoc_gio", "armor_ao_mua_dau", "pants_quan_da_lot_bong", "brc_vong_cach_dien", "nck_coi_chan_heo");
        c.LearnedSkills.AddRange(new[] { "pig.skill.bo_phap_luot", "pig.skill.moc_keo", "pig.skill.troi_luoi", "pig.skill.chan_dia" });
        // Trói Chùy: Thủ → Công fits the two pair slots
        c.ActiveMartialSkills[CombatSlotType.TheThu] = "pig.skill.troi_luoi";
        c.ActiveMartialSkills[CombatSlotType.TheCong] = "pig.skill.chan_dia";
        Check(MartialProgression.ComboReady(c, "pig.skill.troi_luoi", "pig.skill.chan_dia"), "Trói Chùy chains with Thủ + Công");
        // Ảnh Móc: Công → Công needs the Song Thế slot
        c.ActiveMartialSkills[CombatSlotType.TheCong] = "pig.skill.bo_phap_luot";
        Check(!MartialProgression.Assign(c, CombatSlotType.TheSongThe, "pig.skill.moc_keo").ok || !c.IsMartialSlotUnlocked(CombatSlotType.TheSongThe), "no seal: slot 4 closed");
        Check(!MartialProgression.ComboReady(c, "pig.skill.bo_phap_luot", "pig.skill.moc_keo"), "same-tag pair not ready without Song Thế");
        c.EngravedSeals.Add("AnSongThe");
        Check(MartialProgression.Assign(c, CombatSlotType.TheSongThe, "pig.skill.moc_keo").ok, "Song Thế takes any learned skill");
        Check(MartialProgression.ComboReady(c, "pig.skill.bo_phap_luot", "pig.skill.moc_keo"), "Ảnh Móc ready with Song Thế");
        c.Stamina.CurrentStamina = 100f; c.Rage = 20;
        Check(MartialProgression.Cast(c, CombatSlotType.TheCong, 10).Ok, "lướt");
        var song = MartialProgression.Cast(c, CombatSlotType.TheSongThe, 11);
        Check(song.Ok && song.RagePaid == 8 && song.StaminaPaid == 0f && song.Combo == "Ảnh Móc", "Song Thế pays 8 Nộ and chains Ảnh Móc");

        // lướt né: 9 STA × Khinh Thân 0.8 = 7.2; Guốc Gió second dodge within 1 s free
        c.Stamina.CurrentStamina = 100f;
        float d1 = MartialProgression.TryDodge(c, 20), d2 = MartialProgression.TryDodge(c, 20.5), d3 = MartialProgression.TryDodge(c, 22);
        Check(Math.Abs(d1 - 7.2f) < 0.05f && d2 == 0f && Math.Abs(d3 - 7.2f) < 0.05f, $"dodge {d1}/{d2}/{d3}");
        var heavy = new CharacterData(PlayerId.Khoa);
        Wear(heavy, "wpn_bua_ta", "boot_giay_dinh", "armor_giap_luoi_thep", "pants_quan_giap_xich", "brc_vong_huyet_nha", "nck_coi_chan_heo");
        Check(heavy.GetPlaystyle() == "TrọngGiáp" && Math.Abs(MartialProgression.TryDodge(heavy, 5) - 11.2f) < 0.05f, "Trọng Giáp dodge +25%");
        Check(MartialProgression.ControlSeconds(heavy, 2.5f) > 2.5f, "CTRL lengthens control");

        // đổi phái chính
        var e = new GameEngine(seed: 4);
        e.Farm.Gold = 50000;
        Check(MartialProgression.ChangePrimarySchool(e, MartialSchool.TocBo_A).ok && e.Farm.Gold == 30000, "school change 20.000G");
        Check(!MartialProgression.ChangePrimarySchool(e, MartialSchool.SinhTon_D).ok, "once per 3 cycles");

        // túi nhanh
        var b = e.Character;
        for (int i = 0; i < 3; i++) Check(QuickBag.Buy(e, QuickItemId.BinhHoiLuc).ok, "buy flask");
        Check(!QuickBag.Buy(e, QuickItemId.BinhHoiLuc).ok, "max 3 per kind");
        Check(!QuickBag.Buy(e, QuickItemId.TinhNoDan).ok, "Tinh Nộ Đan is not sold for gold");
        b.Stamina.CurrentStamina = 10f;
        Check(QuickBag.Use(b, QuickItemId.BinhHoiLuc).ok && Math.Abs(b.Stamina.CurrentStamina - 50f) < 0.01f && QuickBag.Count(b, QuickItemId.BinhHoiLuc) == 2, "flask +40");
        var data = e.CreateSaveData();
        var e2 = new GameEngine(seed: 4); e2.RestoreFromSave(data);
        Check(QuickBag.Count(e2.Character, QuickItemId.BinhHoiLuc) == 2 && e2.Character.PrimarySchool == MartialSchool.TocBo_A, "bag and school saved");
    }

    public static void RunRageLevelSave()
    {
        var e = new GameEngine(seed: 9);
        var c = e.Character;
        int mp = c.MartialPoints;
        for (int i = 0; i < 20; i++) MartialProgression.OnKill(c, KillClass.Normal);
        Check(c.Rage == 40 && c.MartialPoints == mp + 1, "20 normal kills: +40 Nộ, +1 Điểm Võ");
        MartialProgression.OnKill(c, KillClass.Elite);
        Check(c.Rage == 50 && c.MartialPoints == mp + 6, "elite: +10 Nộ, +5 Điểm Võ");
        MartialProgression.TickRage(c, 1f, 5f, false);
        Check(c.Rage == 50, "no decay within 10 s");
        MartialProgression.TickRage(c, 2f, 12f, false);
        Check(c.Rage == 40, "−5 Nộ/s after 10 s");
        MartialProgression.OnWaveEnded(c, 1);
        Check(c.Rage == 0, "Nộ resets at wave end");

        c.Level = 1; c.Exp = 0; c.StatPoints = 0;
        int lv = MartialProgression.AddExp(c, MartialProgression.ExpToNext(1) + MartialProgression.ExpToNext(2) + MartialProgression.ExpToNext(3) + MartialProgression.ExpToNext(4));
        float guardMax1 = 100f + (c.BaseStats.Res + c.BonusStats.Res) * 2.5f;
        Check(lv == 4 && c.Level == 5 && c.StatPoints == 8 && c.MaxGuard >= guardMax1 + 12f - 0.01f, "4 levels: +8 points, +12 max Chống Chịu");
        Check(Math.Abs(c.Stamina.MaxStamina - 188f) < 0.01f, "level 5: +8 Stamina");
        int agi = c.BaseStats.Agi;
        Check(MartialProgression.SpendStatPoint(c, SkillStat.Agi).ok && c.BaseStats.Agi == agi + 1 && c.StatPoints == 7, "spend a point");
        c.BaseStats.Str = 40; c.StatPoints = 1;
        Check(!MartialProgression.SpendStatPoint(c, SkillStat.Str).ok, "above 40 a point costs 2");
        Check(MartialProgression.AddLaborStamina(c, 25f) == 0 && c.Exp > 0 || c.Level > 5, "labor: 1 EXP per 10 Stamina");

        // buy + save/load
        e.Farm.Gold = 2000;
        Check(MartialProgression.Buy(e, "boot_giay_dinh").ok && e.Farm.Gold == 1700, "Giày Đinh 300G (provisional)");
        MartialProgression.Equip(c, "boot_giay_dinh");
        c.LearnedSkills.Add("pig.skill.pha_giap");
        c.ActiveMartialSkills[CombatSlotType.TheCong] = "pig.skill.pha_giap";
        var data = e.CreateSaveData();
        var e2 = new GameEngine(seed: 9);
        e2.RestoreFromSave(data);
        var c2 = e2.Character;
        Check(c2.Level == c.Level && c2.BaseStats.Agi == c.BaseStats.Agi && c2.StatPoints == c.StatPoints, "level and stats saved");
        Check(c2.Equipment[EquipSlot.Boots]?.Id == "boot_giay_dinh" && c2.OwnedEquipment.Contains("boot_giay_dinh"), "equipment saved");
        Check(MartialProgression.SlottedId(c2, CombatSlotType.TheCong) == "pig.skill.pha_giap" && c2.IsMartialSlotUnlocked(CombatSlotType.TheCong), "skills saved");
    }

    /// <summary>Bàn Khắc (GDD 12.4) có nguồn thật; Giấy Chuyên Biệt phải sở hữu mới dùng; khắc và giấy được lưu.</summary>
    public static void RunEngravingAndPaper()
    {
        var e = new GameEngine(seed: 11);
        var c = e.Character;
        // Chưa đủ điều kiện → không khắc được
        Check(!MartialProgression.EngraveRune(e, "PhuNoHoa").ok, "rune locked at start");
        // Sự kiện Đỏ trước Cổng Giữa Game chỉ đếm, không trao Bàn Khắc
        MartialProgression.OnMysteryEventResolved(e, MysteryEventId.E12_GiuCongNguoc);
        Check(e.CoreAndVillage.RedEventsCompletedCount == 1 && !e.CoreAndVillage.HasHighTierEngravingTableBlueprint, "red event counted, no table before gate");
        Check(MartialProgression.OnMysteryEventResolved(e, MysteryEventId.E01_VuonMuoiHaiQua) == "" && e.CoreAndVillage.RedEventsCompletedCount == 1, "non-red event not counted");
        e.DeepMechanics.MidGameGateUnlocked = true;
        MartialProgression.OnMysteryEventResolved(e, MysteryEventId.E10_DotSanTrongSuong);
        Check(e.CoreAndVillage.HasHighTierEngravingTableBlueprint, "red event after the gate gives the Bàn Khắc");
        e.MajorWavesSurvived = 12; c.Level = 15; e.Defense.HallLevel = 2;
        Check(MartialProgression.RuneSystemOpen(e), "rune system open");
        Check(MartialProgression.EngraveRune(e, "PhuNoHoa").ok && c.EngravedRunes.Contains("PhuNoHoa"), "tier-I rune engraves (100%)");
        // Đếm chu kỳ từ khi mở Phù Văn
        MartialProgression.TickRuneCycles(e);
        Check(e.CoreAndVillage.RuneSystemLastCountedCycle == e.Clock.CycleNumber, "cycle counter anchored");
        e.CoreAndVillage.RuneSystemLastCountedCycle -= 5;
        MartialProgression.TickRuneCycles(e);
        Check(e.CoreAndVillage.CyclesSinceRuneSystemUnlocked == 5, "5 cycles counted");
        // Mệnh Ấn: cần ≥ 18 đợt lớn, cấp ≥ 22, 2 Boss, 1 sự kiện Đỏ
        Check(!MartialProgression.EngraveSeal(e, "AnSongThe").ok, "seal needs level 22 / 18 waves / 2 bosses");
        e.MajorWavesSurvived = 18; c.Level = 22; c.RecalculateBonusStats(); e.DeepMechanics.CycleBossesDefeatedCount = 2;
        float maxBefore = c.MaxGuard;
        Check(MartialProgression.EngraveSeal(e, "AnSongThe").ok && c.IsMartialSlotUnlocked(CombatSlotType.TheSongThe), "Song Thế engraved → 4th slot");
        Check(c.MaxGuard < maxBefore, "seal lowers max guard");

        // Giấy: không có giấy → không dùng / gia cường / rã được
        Check(!SpecialtyPaperActions.SelectNextOwned(e), "no paper owned");
        Check(!SpecialtyPaperActions.Use(e, () => 0.0).ok, "cannot use unowned paper");
        Check(!SpecialtyPaperActions.Salvage(e).ok, "cannot salvage unowned paper");
        e.Inventory.AddItem(EnhancementTransactions.PaperId(SpecialtyPaperColor.Vang), 2);
        Check(SpecialtyPaperActions.SelectNextOwned(e) && e.DeepMechanics.EquippedPaperColor == SpecialtyPaperColor.Vang, "selects the owned colour");
        Check(SpecialtyPaperActions.Use(e, () => 0.0).ok && SpecialtyPaperActions.OwnedCount(e, SpecialtyPaperColor.Vang) == 1, "use consumes one paper");
        Check(e.DeepMechanics.SpecialtyLocks.RevealedLocks >= 1, "paper reveals a lock");
        int dustBefore = e.Inventory.CountId("bot_tu_tinh");
        Check(SpecialtyPaperActions.Salvage(e).ok && SpecialtyPaperActions.OwnedCount(e, SpecialtyPaperColor.Vang) == 0, "salvage consumes one paper");
        Check(e.Inventory.CountId("bot_tu_tinh") == dustBefore + 2, "GDD: salvaging a Vàng paper gives 2 Bột Tử Tinh");
        e.Inventory.AddItem(EnhancementTransactions.PaperId(SpecialtyPaperColor.Do), 1);
        e.DeepMechanics.EquippedPaperColor = SpecialtyPaperColor.Do;
        Check(!SpecialtyPaperActions.Salvage(e).ok && SpecialtyPaperActions.OwnedCount(e, SpecialtyPaperColor.Do) == 1, "GDD gives no red-paper yield: refused, paper kept");
        e.Inventory.ConsumeItem(EnhancementTransactions.PaperId(SpecialtyPaperColor.Do));
        e.DeepMechanics.EquippedPaperColor = SpecialtyPaperColor.Vang;

        // Nguồn giấy GDD 10.3: đợt lớn Lam 8%/Vàng 3%/Tím 0,8%/Đỏ 0,2%; Boss 30/15/5/1,5%
        int lamBefore = SpecialtyPaperActions.OwnedCount(e, SpecialtyPaperColor.Lam);
        Check(SpecialtyPaperActions.RollDrop(e, SpecialtyPaperActions.MajorWaveRates, 0.001) == SpecialtyPaperColor.Do, "0.1% roll on a major wave is red");
        Check(SpecialtyPaperActions.RollDrop(e, SpecialtyPaperActions.MajorWaveRates, 0.10) == SpecialtyPaperColor.Lam, "10% roll on a major wave is blue (Đỏ+Tím+Vàng = 4%, +Lam = 12%)");
        Check(SpecialtyPaperActions.RollDrop(e, SpecialtyPaperActions.MajorWaveRates, 0.13) == null, "13% roll on a major wave drops nothing");
        Check(SpecialtyPaperActions.RollDrop(e, SpecialtyPaperActions.CycleBossRates, 0.50) == SpecialtyPaperColor.Lam, "50% roll on a Boss is blue (cumulative 51.5%)");
        Check(SpecialtyPaperActions.OwnedCount(e, SpecialtyPaperColor.Lam) == lamBefore + 2, "drops land in the inventory");

        // Lưu và nạp
        var e2 = new GameEngine(seed: 12);
        e2.RestoreFromSave(e.CreateSaveData());
        Check(e2.Character.EngravedRunes.Contains("PhuNoHoa") && e2.Character.EngravedSeals.Contains("AnSongThe"), "runes and seals saved");
        Check(e2.CoreAndVillage.HasHighTierEngravingTableBlueprint && e2.CoreAndVillage.RedEventsCompletedCount == 2 && e2.CoreAndVillage.CyclesSinceRuneSystemUnlocked == 5, "engraving progress saved");
        Check(e2.DeepMechanics.EquippedPaperColor == SpecialtyPaperColor.Vang, "paper colour saved");

        // An khóa 2: mỗi lần mua tin tình báo là một lượt hỏi
        int before = e.DeepMechanics.SpecialtyLocks.IntelQuestions;
        e.Farm.Gold += 1000;
        e.Defense.ConstructBuilding(IntelBureau.DeskBlueprintId); // GDD 15: the desk must exist
        Check(e.Intel.Purchase(e, 1).success && e.DeepMechanics.SpecialtyLocks.IntelQuestions == before + 1, "intel purchase counts as a question");
    }

    /// <summary>GDD 12.3: Dao Rựa giảm Stamina phát cỏ/dọn bãi; Còi Chăn Heo xoa dịu heo hoảng loạn 1 lần/2 phút.</summary>
    public static void RunEquipmentMechanics()
    {
        var e = new GameEngine(seed: 21);
        var c = e.Character;
        MartialProgression.Unequip(c, EquipSlot.Weapon);
        float plain = FarmLaborStamina.Cost(e, FarmLaborTask.CleanPlot);
        c.OwnedEquipment.Add("wpn_dao_rua");
        Check(MartialProgression.Equip(c, "wpn_dao_rua").ok, "equip Dao Rựa");
        float withKnife = FarmLaborStamina.Cost(e, FarmLaborTask.CleanPlot);
        Check(Math.Abs(withKnife - plain * 0.8f) < 0.11f, $"Dao Rựa −20% dọn bãi ({plain} → {withKnife})");

        var pig = new Pig { Id = "COI_TEST", Name = "Heo thử", Stage = PigStage.TruongThanh, WeightKg = 80f, GeneLine = GeneLineId.KimTho };
        pig.Mood = 10;
        Check(pig.MoodState == MoodState.HoangLoan, "pig panicking");
        double readyAt = 0;
        Check(MartialProgression.TryCalmWithHerdingWhistle(c, new[] { pig }, 0, ref readyAt) == 0, "no whistle, no effect");
        c.OwnedEquipment.Add("nck_coi_chan_heo");
        Check(MartialProgression.Equip(c, "nck_coi_chan_heo").ok, "equip Còi Chăn Heo");
        Check(MartialProgression.TryCalmWithHerdingWhistle(c, new[] { pig }, 0, ref readyAt) == 1 && pig.Mood == 20, "+10 morale");
        pig.Mood = 10;
        Check(MartialProgression.TryCalmWithHerdingWhistle(c, new[] { pig }, 60, ref readyAt) == 0, "cooldown 2 minutes");
        Check(MartialProgression.TryCalmWithHerdingWhistle(c, new[] { pig }, 121, ref readyAt) == 1, "ready again after 2 minutes");
    }

    /// <summary>GDD 11.2: 5 lính đánh thuê đúng bảng (HP/sát thương/tầm/hành vi); chỉ lính đang thuê mới vào trận; Mắt Ưng +25%.</summary>
    public static void RunMercenaries()
    {
        Check(MercenarySpecs.All.Count == 5, "5 mercenaries");
        var ve = MercenarySpecs.Find(NpcSpecialistId.VeBinhKhien)!;
        Check(ve.MaxHp == 420 && ve.Damage == 18 && ve.Melee, "Vệ Binh Khiên 420 HP, 18/đòn, cận");
        var xa = MercenarySpecs.Find(NpcSpecialistId.XaThuNo)!;
        Check(xa.MaxHp == 180 && xa.Damage == 35 && xa.RangeMeters == 18f && xa.Targeting == MercenaryTargeting.PreferElite, "Xạ Thủ Nỏ 180 HP, 35/phát, 18 m, săn tinh anh");
        var ufo = MercenarySpecs.Find(NpcSpecialistId.ThoSanUfo)!;
        Check(ufo.Damage == 90 && ufo.RangeMeters == 25f && ufo.Targeting == MercenaryTargeting.AirOnly, "Thợ Săn UFO 90/phát, 25 m, trên không");
        Check(MercenarySpecs.Find(NpcSpecialistId.ThichKhachDem)!.Damage == 45 && MercenarySpecs.Find(NpcSpecialistId.KySuChienTruong)!.MaxHp == 220, "Thích Khách 45, Kỹ Sư 220 HP");
        Check(MercenarySpecs.MaxStamina == 150f && MercenarySpecs.RetreatSeconds == 8f, "Stamina 150, retreat 8 s");
        Check(Math.Abs(MercenarySpecs.HitDamage(xa, true) - 43.75f) < 0.01f && MercenarySpecs.HitDamage(ve, true) == 18f, "Mắt Ưng +25% chỉ cho Xạ Thủ");

        var e = new GameEngine(seed: 31);
        Check(MercenarySpecs.OnDuty(e).Count == 0, "nobody hired");
        e.PetsAndNpcs.HiredNpcs.Add(new HiredNpcRecord { Id = NpcSpecialistId.XaThuNo, Name = "Miên" });
        e.PetsAndNpcs.HiredNpcs.Add(new HiredNpcRecord { Id = NpcSpecialistId.VeBinhKhien, Name = "Độ", IsActive = false });
        var duty = MercenarySpecs.OnDuty(e);
        Check(duty.Count == 1 && duty[0].Id == NpcSpecialistId.XaThuNo, "only active hired mercenaries fight");
        Check(MercenarySpecs.IsAirTarget(EnemyTypeId.UfoThuHoach) && !MercenarySpecs.IsAirTarget(EnemyTypeId.KeBatHeo), "air targets");
    }
}
