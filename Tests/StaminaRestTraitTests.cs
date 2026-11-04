using System;
using PigTycoon.Core;

/// <summary>GDD 8.1 (đặc tính cơ bản), 9.1–9.5 (Thể lực, ngủ) và 10.4–10.8 (6 khóa chuyên biệt).</summary>
internal static class StaminaRestTraitTests
{
    // Đặt đồng hồ tại 20:00 ngày 1, không còn đợt quái nhỏ nào trong chu kỳ (để ngủ không bị đánh thức).
    static GameEngine EveningEngine(int seed, int minorWaves = 0, int hoursUntilMinor = 0)
    {
        var e = new GameEngine(seed);
        e.Clock.RestoreFromSave(1, 20, 0, 1, 7, 1, minorWaves, hoursUntilMinor, 0, false);
        e.CurrentWeather = WeatherType.QuangDang;
        return e;
    }

    public static void RunSleepUsesEngineTick()
    {
        var e = EveningEngine(41);
        var pig = Pig.CreateDefault("p_sleep", "Heo Ngủ", GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 12, initialWeight: 80f);
        e.Pigs.Add(pig);
        int age = pig.AgeDays;
        int processedBefore = e.LastDailyProcessedFarmDay;
        e.Character.Stamina.CurrentStamina = 30f;
        e.Character.Stamina.DailyLoad = 40f;
        var r = PlayerRestCommands.Sleep(e);
        Require(r.success && e.Clock.CurrentDay == 2 && e.Clock.CurrentHour == 6 && e.Clock.CurrentMinute == 0, "Ngủ phải tới 06:00 ngày hôm sau.");
        Require(e.LastDailyProcessedFarmDay > processedBefore, "Ngủ phải chạy xử lý qua ngày của GameEngine (GDD 9.5).");
        Require(pig.AgeDays == age + 1, "Heo phải già thêm 1 ngày khi người chơi ngủ qua đêm.");
        Require(e.Character.Stamina.DailyLoad == 0f && e.Character.Stamina.CurrentStamina >= e.Character.Stamina.BaseMaxStamina * 0.8f,
            "Ngủ chuẩn xóa Tải ngày, Thể lực >= 80%.");
        Require(e.Clock.SleepCooldownHoursRemaining == GameClock.SleepCooldownHours, "Cooldown ngủ 6 giờ sau khi dậy.");

        var fog = EveningEngine(42);
        fog.CurrentWeather = WeatherType.SuongMu;
        Require(!PlayerRestCommands.Sleep(fog).success && fog.Clock.CurrentHour == 20, "Không ngủ được trong Sương Mù (GDD 9.5).");
        var monsters = EveningEngine(43);
        Require(!PlayerRestCommands.Sleep(monsters, monstersInsideFarm: true).success && monsters.Clock.CurrentHour == 20,
            "Không ngủ được khi có quái trong ranh giới trại (GDD 9.5).");

        var woken = EveningEngine(44, minorWaves: 1, hoursUntilMinor: 2);
        woken.Character.Stamina.DailyLoad = 50f;
        woken.Character.Stamina.CurrentStamina = 20f;
        var w = PlayerRestCommands.Sleep(woken);
        Require(w.success && woken.Clock.CurrentHour == 22 && woken.Clock.IsMinorWaveTriggered, "Đợt quái nhỏ đánh thức người đang ngủ.");
        Require(woken.Character.Stamina.DailyLoad == 38f && woken.Clock.SleepCooldownHoursRemaining == 0,
            "Bị đánh thức: chỉ hồi như ngồi nghỉ 6/giờ, không xóa Tải ngày, không tính cooldown.");
    }

    public static void RunFarmLaborLoadAndRegen()
    {
        var e = new GameEngine(51);
        e.CurrentWeather = WeatherType.QuangDang;
        e.Clock.RestoreFromSave(1, 15, 0, 1, 7, 1, 0, 0, 0, false); // Chiều: hệ số Buổi 0
        var s = e.Character.Stamina;
        s.LastMealMinute = e.Clock.TotalElapsedGameMinutes; // just ate: no GDD 9.3 hunger term in this base-cost check
        var spent = FarmLaborStamina.TrySpend(e, FarmLaborTask.RefillWaterBasin);
        Require(spent.success && Math.Abs(spent.cost - 6f) < 0.01f && Math.Abs(s.DailyLoad - 6f) < 0.01f,
            "Thay nước bồn: 6 Thể lực, 100% thành Tải ngày (GDD 9.2).");
        float afterLabor = s.CurrentStamina;
        e.Character.TickCombatRegen(10f, false, 10f);
        Require(Math.Abs(s.CurrentStamina - afterLabor) < 0.01f, "Thể lực việc trại không hồi bằng hồi ngoài combat (GDD 9.4).");

        s.TryConsumeStamina(10f, 0.3f); // đòn combat: 30% thành Tải ngày
        e.Character.TickCombatRegen(10f, false, 10f);
        Require(Math.Abs(s.CurrentStamina - afterLabor + 3f) < 0.01f, "Combat: chỉ phần Hao phí tức thời (70%) hồi lại nhanh.");

        e.CurrentWeather = WeatherType.NangGat;
        float hot = FarmLaborStamina.Cost(e, FarmLaborTask.RefillWaterBasin);
        Require(Math.Abs(hot - 6f * 1.15f * 1.15f) < 0.05f, "Nắng Gắt: hệ số riêng ×1,15 và môi trường +15%, bỏ hệ số Buổi.");

        Require(PlayerRestCommands.EatMeal(e, false).success && s.DailyLoad == 0f, "Bữa thường +20 hồi Tải ngày (9) trước (GDD 9.4).");
        PlayerRestCommands.EatMeal(e, true);
        PlayerRestCommands.EatMeal(e, false);
        Require(!PlayerRestCommands.EatMeal(e, false).success, "Tối đa 3 bữa/ngày.");

        var low = new GameEngine(52);
        low.Character.Stamina.CurrentStamina = 1f;
        Require(!FarmLaborStamina.TrySpend(low, FarmLaborTask.CleanPlot).success && low.DeepMechanics.SpecialtyLocks.ManualCleanings == 0,
            "Không đủ Thể lực thì không làm và không tính lần dọn.");
    }

    public static void RunOverexertionAndReboundDecay()
    {
        var s = new StaminaSystem();
        s.DailyLoad = s.MaxStamina * 0.60f;
        var d1 = s.EvaluateEndOfDay(() => 0.0);
        Require(d1.conditionLost == 8 && s.Condition == 92 && !s.IsExhausted, "Tải ngày > 55%: Thể trạng −8.");
        Require(s.DailyLoad > 0f, "Qua ngày không tự xóa Tải ngày; chỉ ăn/nghỉ/ngủ mới hồi.");

        s.DailyLoad = s.MaxStamina * 0.80f;
        var d2 = s.EvaluateEndOfDay(() => 0.10);
        Require(d2.conditionLost == 15 && d2.becameExhausted && s.IsExhausted && s.IsFatigued, "Tải ngày > 75%: −15 và 20% Kiệt Sức.");
        s.PerformSleepReset();
        Require(!s.IsExhausted, "Ngủ chuẩn xóa Kiệt Sức.");

        s.ReboundFatigueStacks = 2;
        s.DailyLoad = 0f;
        Require(s.EvaluateEndOfDay(null).cleanDay && s.ReboundFatigueStacks == 1, "Ngày sạch giảm 1 tầng Mệt Dội.");
        s.ForcedRestToday = true;
        Require(!s.EvaluateEndOfDay(null).cleanDay && s.ReboundFatigueStacks == 1, "Ngày nghỉ cưỡng bức không phải ngày sạch.");

        s.ReboundFatigueStacks = 2;
        int c = s.Condition = 50;
        s.PerformSleepReset();
        Require(s.Condition == c + 4, "Mỗi tầng Mệt Dội làm hồi Thể trạng −15% (6 × 0,70 ≈ 4).");
        Require(Math.Abs(s.BaseMaxStamina - s.MaxStamina * 0.84f) < 0.01f, "Mệt Dội: Stamina tối đa −8%/tầng.");

        var e = EveningEngine(53);
        e.Character.Stamina.DailyLoad = e.Character.Stamina.MaxStamina * 0.60f;
        e.Tick(11 * 60); // qua 06:00 không ngủ
        Require(e.Character.Stamina.Condition == 92, "Qua ngày trong GameEngine áp phạt quá sức cuối ngày.");
    }

    public static void RunKhoaWhistle()
    {
        var e = new GameEngine(61);
        e.SwitchActiveProtagonist(PlayerId.Khoa);
        for (int i = 0; i < 15; i++)
        {
            var p = Pig.CreateDefault($"w{i:00}", $"Heo {i}", GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 12, initialWeight: 80f);
            p.PositionX = 50f + i; p.PositionY = 50f;
            e.Pigs.Add(p);
        }
        var far = Pig.CreateDefault("w_far", "Heo Xa", GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 12, initialWeight: 80f);
        far.PositionX = 50f; far.PositionY = 75f;
        e.Pigs.Add(far);
        float sta = e.Character.Stamina.CurrentStamina;
        var r = ProtagonistTraits.KhoaWhistle(e, 50f, 50f);
        Require(r.success && r.pigIds.Count == 12 && !r.pigIds.Contains("w_far") && r.pigIds.Contains("w00") && !r.pigIds.Contains("w14"),
            "Huýt sáo: tối đa 12 heo gần nhất trong 20 m.");
        Require(e.Character.Stamina.CurrentStamina == sta, "Huýt sáo không tốn Thể lực.");
        Require(!ProtagonistTraits.KhoaWhistle(e, 50f, 50f).success, "Hồi chiêu 2 giờ game.");
        e.Tick(119);
        Require(!ProtagonistTraits.KhoaWhistle(e, 50f, 50f).success, "Chưa đủ 2 giờ game.");
        e.Tick(1);
        Require(ProtagonistTraits.KhoaWhistle(e, 50f, 50f).success, "Đủ 2 giờ game thì huýt lại được.");

        var an = new GameEngine(62);
        Require(!ProtagonistTraits.KhoaWhistle(an, 0f, 0f).success, "An không có Tiếng Huýt Chăn Đàn.");
    }

    public static void RunAnTradeEye()
    {
        var e = new GameEngine(71);
        Require(e.Character.Identity == PlayerId.An && e.Story.ActiveProtagonist == PlayerId.An,
            "Ván mới: nhân vật và cốt truyện cùng là An.");
        var pig = Pig.CreateDefault("eye", "Heo Mắt", GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 15, initialWeight: 100f);
        pig.Stage = PigStage.TruongThanh;
        pig.LatentDiseaseRiskMultiplier = 1.5f;
        pig.IsIdentified = false;
        pig.Traits.Add(new PigTrait { Id = "HauAn", Name = "Háu Ăn", Description = "ăn nhiều" });
        e.Pigs.Add(pig);

        var first = ProtagonistTraits.AnObservePig(e, "eye");
        Require(first.revealed && first.message.Contains("bệnh tiềm ẩn"), "Quan sát 5 giây lộ dấu hiệu sức khỏe ẩn trước.");
        var second = ProtagonistTraits.AnObservePig(e, "eye");
        Require(second.revealed && second.message.Contains("Háu Ăn"), "Lần quan sát sau lộ tính chất phụ ẩn.");
        Require(!ProtagonistTraits.AnObservePig(e, "eye").revealed, "Hết dấu hiệu ẩn thì không lộ thêm.");

        var scr = ProtagonistTraits.AnFreeScreening(e, "eye");
        Require(scr.success && scr.diseaseFound, "Kiểm miễn phí phát hiện bệnh tiềm ẩn.");
        Require(!ProtagonistTraits.AnFreeScreening(e, "eye").success, "Mỗi ngày chỉ 1 lần.");

        var clean = Pig.CreateDefault("clean", "Heo Sạch", GeneLineId.HongDien, GeneRarity.Thuong, initialAge: 15, initialWeight: 100f);
        clean.Stage = PigStage.TruongThanh;
        e.Pigs.Add(clean);
        var e2 = new GameEngine(72);
        e2.Pigs.Add(clean);
        e2.Clock.RestoreFromSave(1, 9, 0, 1, 7, 1, 0, 0, 0, false);
        Require(ProtagonistTraits.AnFreeScreening(e2, "clean").success && ProtagonistTraits.IsQuarantineFeeWaived(e2, "clean"), "Con đã kiểm được miễn phí kiểm dịch.");
        int price = e2.Economy.CalculatePigSalePrice(clean, e2.Farm.FarmLevel).priceGold;
        var sold = e2.SellPigAtDayMarket("clean", isQuarantined: true);
        Require(sold.success && sold.goldEarned == price, "Bán có kiểm dịch: không trừ 60G cho con An đã kiểm (GDD 8.1).");
        Require(e2.DeepMechanics.SpecialtyLocks.QuarantinedDayMarketSales == 1, "Bán đã kiểm dịch được ghi cho khóa 5 (bị trần 50% khi chưa lộ).");

        var khoa = new GameEngine(73);
        khoa.SwitchActiveProtagonist(PlayerId.Khoa);
        khoa.Pigs.Add(Pig.CreateDefault("k", "Heo K", GeneLineId.HongDien, GeneRarity.Thuong));
        Require(!ProtagonistTraits.AnObservePig(khoa, "k").revealed, "Khoa không có Mắt Nhà Nghề.");
    }

    public static void RunSpecialtyLocks()
    {
        var e = new GameEngine(81);
        e.SwitchActiveProtagonist(PlayerId.Khoa);
        var t = e.DeepMechanics.SpecialtyLocks;
        e.Farm.FarmLevel = 10;
        e.MajorWavesSurvived = 12;
        e.Tick(24 * 60);
        Require(e.DeepMechanics.UnlockedSpecialtyLocksCount == 0 && !e.DeepMechanics.SpecialtyTraitUnlocked,
            "Cấp trại/đợt lớn không tự mở khóa (GDD 10.4).");
        Require(t.MajorWavesSurvived == 5, "Tiến độ trước khi lộ chỉ công nhận tối đa 50% (12 đợt → 5/10).");

        for (int i = 0; i < 200; i++) t.RecordManualCleaning();
        Require(t.ManualCleanings == 75, "Dọn vệ sinh trước khi lộ: trần 75/150.");

        var lam = t.UsePaper(SpecialtyPaperColor.Lam, 0, () => 0.99);
        Require(lam.revealed == 0 && t.RevealedLocks == 0, "Giấy sịt: mất lượt, khóa vẫn ẩn.");
        var red = t.UsePaper(SpecialtyPaperColor.Do, 0, () => 0.0);
        Require(red.revealed == 3 && t.RevealedLocks == 3, "Giấy Đỏ: 2 lượt chắc chắn + 30% lượt 3, lộ theo thứ tự 1→3.");

        for (int i = 0; i < 75; i++) t.RecordManualCleaning();
        e.MajorWavesSurvived = 17;
        e.Tick(24 * 60);
        Require(t.IsCompleted(1) && t.IsCompleted(2) && !t.IsCompleted(4) && e.DeepMechanics.UnlockedSpecialtyLocksCount == 2,
            "Khóa đã lộ và xong nhiệm vụ mới tính hoàn thành.");
        Require(!e.DeepMechanics.SpecialtyTraitUnlocked && e.DeepMechanics.SpecialtyTraitDisplayName == "???" && !e.Herd.KhoaHasNguoiCuaDanUnlocked,
            "Chưa đủ 6 khóa: không có hiệu ứng, tên vẫn ẩn.");

        t.UsePaper(SpecialtyPaperColor.Do, 3, () => 0.0);
        Require(t.RevealedLocks == 6, "Lộ đủ 6 khóa.");
        t.HerdStateStreakDays = 10; t.RareQuyLinesIdentified = 2; t.QuarantinedDayMarketSales = 500; t.TransmissionSuccessions = 1;
        e.DeepMechanics.EvaluateMidGameGateAndLocks(e.Farm, e.Herd, e.MajorWavesSurvived);
        Require(e.DeepMechanics.SpecialtyTraitUnlocked && e.DeepMechanics.SpecialtyTraitDisplayName != "???", "Xong cả 6: hiệu ứng có hiệu lực.");

        var saved = new GameEngine(82);
        saved.RestoreFromSave(e.CreateSaveData());
        Require(saved.DeepMechanics.SpecialtyLocks.RevealedLocks == 6 && saved.DeepMechanics.SpecialtyLocks.CompletedCount == 6,
            "6 khóa được lưu/tải.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
