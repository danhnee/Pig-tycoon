using System;
using PigTycoon.Core;

/// <summary>GDD 7.2 trạng thái thời tiết lên người ngoài trời, trang bị phòng tránh 12.3 và hệ số 9.3 (ADR-0022).</summary>
internal static class PlayerWeatherStatusTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < 0.001f;

    private static GameEngine Outdoors(WeatherType weather, int seed = 7)
    {
        var e = new GameEngine(seed: seed);
        e.CurrentWeather = weather;
        e.WeatherStatus.IsOutdoors = true;
        e.WeatherStatus.IsFarFromRoof = true;
        return e;
    }

    private static void Wear(GameEngine e, string itemId) =>
        e.Character.EquipItem(EquipmentAndTacticalCatalog.Find(itemId) ?? throw new InvalidOperationException(itemId));

    public static void RunSunstrokeAndRain()
    {
        // Cảm Nắng: 2 giờ liên tục ngoài trời 10:00–16:00, 20% dính, trần Thể lực −10%, nghỉ mát 1 giờ thì hết.
        var e = Outdoors(WeatherType.NangGat);
        var ws = e.WeatherStatus;
        ws.TickHour(e, 11, () => 0.0);
        Check(!ws.CamNang, "1 giờ nắng chưa đủ để Cảm Nắng");
        ws.TickHour(e, 12, () => 0.0);
        Check(ws.CamNang, "2 giờ liên tục 10:00–12:00, roll < 20% thì Cảm Nắng");
        var st = e.Character.Stamina;
        Check(Near(ws.StaminaCap(st), st.EffectiveMaxStamina - 0.10f * st.BaseMaxStamina), "Cảm Nắng: trần −10% Stamina tối đa");
        st.CurrentStamina = st.EffectiveMaxStamina;
        ws.EnforceStaminaCap(st);
        Check(Near(st.CurrentStamina, st.BaseMaxStamina * 0.9f), "Thể lực bị kẹp dưới trần Cảm Nắng");
        ws.IsOutdoors = false;
        ws.TickHour(e, 13, () => 0.0);
        Check(!ws.CamNang, "Nghỉ bóng mát (trong nhà) 1 giờ thì hết Cảm Nắng");

        var miss = Outdoors(WeatherType.NangGat);
        miss.WeatherStatus.TickHour(miss, 11, () => 0.25);
        miss.WeatherStatus.TickHour(miss, 12, () => 0.25);
        Check(!miss.WeatherStatus.CamNang, "Roll ≥ 20% thì không dính");
        var evening = Outdoors(WeatherType.NangGat);
        evening.WeatherStatus.TickHour(evening, 17, () => 0.0);
        evening.WeatherStatus.TickHour(evening, 18, () => 0.0);
        Check(!evening.WeatherStatus.CamNang, "Ngoài khung 10:00–16:00 không Cảm Nắng");

        // Ướt Lạnh: sau 1 giờ dưới Mưa; Miễn dịch hao ×1,5; kéo dài 3 giờ sau khi vào nhà; 9.3 +5%.
        var dry = Outdoors(WeatherType.Mua);
        var wet = Outdoors(WeatherType.Mua);
        wet.WeatherStatus.TickHour(wet, 9, () => 0.9);
        Check(wet.WeatherStatus.UotLanh, "1 giờ ngoài trời dưới Mưa thì Ướt Lạnh");
        Check(wet.WeatherStatus.ApplyImmunityLoss(wet.Character, 10) == 15, "Ướt Lạnh: Miễn dịch hao ×1,5");
        float m0 = FarmLaborStamina.EnvironmentMultiplier(dry);
        float m1 = FarmLaborStamina.EnvironmentMultiplier(wet);
        Check(Near(m1 - m0, 0.05f), $"9.3: Ướt Lạnh +5% Thể lực ({m0} → {m1})");
        wet.WeatherStatus.IsOutdoors = false;
        wet.WeatherStatus.TickHour(wet, 10, () => 0.9);
        wet.WeatherStatus.TickHour(wet, 11, () => 0.9);
        Check(wet.WeatherStatus.UotLanh, "Vào nhà 2 giờ vẫn còn Ướt Lạnh");
        wet.WeatherStatus.TickHour(wet, 12, () => 0.9);
        Check(!wet.WeatherStatus.UotLanh, "Hết Ướt Lạnh 3 giờ sau khi vào nhà");

        var coat = Outdoors(WeatherType.Mua);
        Wear(coat, "armor_ao_mua_dau");
        coat.WeatherStatus.TickHour(coat, 9, () => 0.0);
        Check(!coat.WeatherStatus.UotLanh, "Áo Mưa Dầu miễn Ướt Lạnh");
    }

    public static void RunStormLightningAndChill()
    {
        var e = Outdoors(WeatherType.GiongBao);
        var ws = e.WeatherStatus;
        e.Character.CurrentGuard = 100f;
        ws.TickHour(e, 9, () => 0.0);
        Check(Near(e.Character.CurrentGuard, 60f), "Sét đánh: Chống Chịu −40 (không có HP)");
        Check(ws.TryTakeLightningStrike(out var strike) && Near(strike.StunSeconds, 3f) && strike.HpLost == 40, "Sét: choáng 3 giây cho runtime");
        Check(!ws.HasCamGio(e), "Cảm Gió chỉ tung mỗi 2 giờ ngoài trời");
        ws.TickHour(e, 10, () => 0.0);
        Check(ws.HasCamGio(e) && e.Character.Stamina.Immunity == 90, "Cảm Gió: 2 giờ ngoài trời, 10% dính, Miễn dịch −10");
        Check(Near(ws.StaminaFactorSum(e), 0.10f), "9.3: Cảm Gió +10% Thể lực");
        ws.TickHour(e, 11, () => 0.0);
        Check(Near(e.Character.CurrentGuard, 1f), "ADR-0022: sét không làm ngất, còn 1 Chống Chịu");

        var snap = ws.Capture();
        snap.CamGioDay = e.Clock.CurrentDay - 1;
        ws.Restore(snap);
        Check(!ws.HasCamGio(e), "Cảm Gió chỉ kéo dài suốt ngày đã dính");

        var roofed = Outdoors(WeatherType.GiongBao);
        roofed.WeatherStatus.IsFarFromRoof = false;
        float roofedHp = roofed.Character.CurrentGuard;
        roofed.WeatherStatus.TickHour(roofed, 9, () => 0.0);
        Check(Near(roofed.Character.CurrentGuard, roofedHp) &&!roofed.WeatherStatus.TryTakeLightningStrike(out _), "Gần mái che ≤ 15 m: không bị sét");

        var ring = Outdoors(WeatherType.GiongBao);
        Wear(ring, "brc_vong_cach_dien");
        float hp = ring.Character.CurrentGuard;
        ring.WeatherStatus.TickHour(ring, 9, () => 0.01);
        Check(Near(ring.Character.CurrentGuard, hp), "Vòng Cách Điện: 3% × 0,2 = 0,6% — roll 1% không trúng");
        ring.WeatherStatus.TickHour(ring, 10, () => 0.005);
        Check(ring.WeatherStatus.TryTakeLightningStrike(out var s2) && s2.StunSeconds == 0f, "Vòng Cách Điện: miễn choáng điện");
    }

    public static void RunRadiationColdDryFog()
    {
        var e = Outdoors(WeatherType.MuaBucXa);
        var ws = e.WeatherStatus;
        for (int h = 7; h <= 12; h++) ws.TickHour(e, h, () => 0.9);
        Check(ws.NhiemXaStacks == 5, "Nhiễm Xạ tối đa 5 tầng");
        Check(e.Character.Stamina.Immunity == 80, "Mỗi tầng Nhiễm Xạ Miễn dịch −4");
        Check(Near(ws.HpRegenMultiplier, 0.5f) && Near(ws.StaminaFactorSum(e), 0.10f), "≥ 3 tầng: HP hồi −50%, 9.3 +10% Thể lực");
        ws.IsOutdoors = false;
        ws.TickHour(e, 13, () => 0.9);
        Check(ws.NhiemXaStacks == 4, "ADR-0022: trong nhà giảm 1 tầng mỗi giờ");
        var lead = Outdoors(WeatherType.MuaBucXa);
        Wear(lead, "armor_ao_chi");
        lead.WeatherStatus.TickHour(lead, 7, () => 0.9);
        Check(lead.WeatherStatus.NhiemXaStacks == 0, "Áo Chì miễn Nhiễm Xạ");

        var cold = Outdoors(WeatherType.RetDam);
        Check(cold.WeatherStatus.HasCongTay(cold) && Near(cold.WeatherStatus.TaskTimeMultiplier(cold), 1.15f), "Cóng Tay: thao tác +15%");
        cold.WeatherStatus.IsOutdoors = false;
        Check(!cold.WeatherStatus.HasCongTay(cold), "Trong nhà không Cóng Tay");
        var pants = Outdoors(WeatherType.RetDam);
        Wear(pants, "pants_quan_da_lot_bong");
        Check(!pants.WeatherStatus.HasCongTay(pants), "Quần Da Lót Bông miễn Cóng Tay");

        var dry = Outdoors(WeatherType.GioKho);
        float extra = dry.WeatherStatus.ExtraSprintStaminaPerSecond(dry);
        Check(Near(extra, 0.2f), "Khô Rát: chạy +0,2 Stamina/giây");
        var run = PlayerLocomotion.Advance(true, 18, 100f, 1f, extra);
        Check(Near(run.StaminaDelta, -1.5f), "Chạy AGI 18 dưới Gió Khô tốn 1,3 + 0,2 = 1,5/giây");
        Check(Near(PlayerLocomotion.Advance(true, 18, 100f, 1f).StaminaDelta, -1.3f), "Không Khô Rát: giữ 1,3/giây");

        var fog = Outdoors(WeatherType.SuongMu);
        Check(fog.WeatherStatus.HasLacHuong(fog) && fog.WeatherStatus.MinimapRadiusMeters(fog) == 15f, "Lạc Hướng: bản đồ nhỏ 15 m");
        Wear(fog, "nck_mat_day_den_bao");
        Check(!fog.WeatherStatus.HasLacHuong(fog) && fog.WeatherStatus.MinimapRadiusMeters(fog) == null, "Mặt Dây Đèn Bão miễn Lạc Hướng");
    }

    public static void RunEngineTickAndSave()
    {
        var e = Outdoors(WeatherType.MuaBucXa, seed: 3);
        e.Tick(60);
        Check(e.WeatherStatus.NhiemXaStacks == 1, "GameEngine.OnHourlyTick chạy trạng thái thời tiết");

        string json = SaveSystem.Serialize(e.CreateSaveData());
        Check(SaveSystem.TryDeserialize(json, out var data, out string err), "đọc lại save: " + err);
        var loaded = new GameEngine(seed: 3);
        loaded.RestoreFromSave(data);
        Check(loaded.WeatherStatus.NhiemXaStacks == 1, "Save/Load giữ tầng Nhiễm Xạ");

        data.PlayerWeatherStatus = null; // save cũ
        var old = new GameEngine(seed: 3);
        old.RestoreFromSave(data);
        Check(old.WeatherStatus.NhiemXaStacks == 0 && !old.WeatherStatus.UotLanh, "Save cũ: không có trạng thái thời tiết");

        var sleeper = Outdoors(WeatherType.Mua, seed: 5);
        var res = PlayerRestCommands.Sleep(sleeper);
        Check(!res.success || !sleeper.WeatherStatus.IsOutdoors, "Ngủ trong nhà: không còn ngoài trời");
    }
}
