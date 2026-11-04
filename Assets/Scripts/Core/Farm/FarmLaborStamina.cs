using System;

namespace PigTycoon.Core
{
    /// <summary>Loại việc trại theo bảng GDD 9.2.</summary>
    public enum FarmLaborTask
    {
        FeedTrough,        // Rải thức ăn / đổ máng nhỏ: 4, 100%, +20% nếu bao > 20 kg
        RefillWaterBasin,  // Thay nước bồn: 6, 100%, Nắng Gắt ×1,15
        DouseBucket,       // Dội Xô Nước: 3, 100%, Nắng Gắt ×1,15
        Medicate,          // Tiêm/cho uống thuốc (1 con): 2, 100%, +50% nếu heo hoảng
        CleanPlot,         // Dọn bãi / rải vôi / thay ổ lót (1 ô): 8–12, 100%, +25% nếu SC < 40, +15% khi Mưa
        DragCarcass,       // Kéo xác heo về Khu Xử Lý: 12, 100%
        BuildRepair,       // Xây / Sửa: 100% (GDD chỉ cho 10/giờ phụ Phòng Xây Dựng; thao tác nhanh: ADR-0018)
        ReloadStructure,   // Nạp đạn/nhiên liệu (mỗi công trình): 5, 100%
        GatherMaterial,    // Chặt/đập/gom vật liệu: 8/lần, 80%
        MarketTrip,        // Đi chợ (1 lượt): 5, 100%, mang heo theo +2/con
        BrushPig,          // Chải heo (Chăm sóc): ADR-0018 PROVISIONAL 4, 100%
    }

    /// <summary>
    /// Tiêu hao Thể lực việc trại 2 lớp (GDD 9.1–9.3): trừ tức thời + ghi Tải ngày theo % của bảng 9.2.
    /// Thể lực thực tế = Gốc × (1 + ΣMôi trường + ΣSức khỏe + ΣTải) × (1 − ΣGiảm trừ), giới hạn ×0,60–×2,00.
    /// Phần ghi vào Tải ngày không hồi bằng hồi phục ngoài combat; chỉ hồi bằng ăn, nghỉ dài, ngủ (GDD 9.1, 9.4).
    /// </summary>
    public static class FarmLaborStamina
    {
        public static (float baseCost, float loadShare) Spec(FarmLaborTask task) => task switch
        {
            FarmLaborTask.FeedTrough => (4f, 1.0f),
            FarmLaborTask.RefillWaterBasin => (6f, 1.0f),
            FarmLaborTask.DouseBucket => (3f, 1.0f),
            FarmLaborTask.Medicate => (2f, 1.0f),
            FarmLaborTask.CleanPlot => (10f, 1.0f),      // GDD 8–12: giữa khoảng (ADR-0018)
            FarmLaborTask.DragCarcass => (12f, 1.0f),
            FarmLaborTask.BuildRepair => (10f, 1.0f),    // GDD: 10 mỗi giờ; thao tác nhanh truyền baseOverride
            FarmLaborTask.ReloadStructure => (5f, 1.0f),
            FarmLaborTask.GatherMaterial => (8f, 0.8f),
            FarmLaborTask.MarketTrip => (5f, 1.0f),
            FarmLaborTask.BrushPig => (4f, 1.0f),        // ADR-0018 PROVISIONAL
            _ => (0f, 1.0f)
        };

        /// <summary>Hệ số riêng của việc (cột "Hệ số riêng" GDD 9.2).</summary>
        public static float TaskFactor(GameEngine engine, FarmLaborTask task, float bagKg = 0f, bool pigPanicking = false, int pigsBroughtToMarket = 0)
        {
            float f = 1f;
            // GDD 12.3 Dao Rựa "Cắt": phát cỏ, dọn bụi −20% Stamina
            if ((task == FarmLaborTask.CleanPlot || task == FarmLaborTask.GatherMaterial) && engine.Character != null && MartialProgression.Wears(engine.Character, "Cat"))
                f *= 0.8f;
            switch (task)
            {
                case FarmLaborTask.FeedTrough:
                    if (bagKg > 20f) f *= 1.20f;
                    break;
                case FarmLaborTask.RefillWaterBasin:
                case FarmLaborTask.DouseBucket:
                    if (engine.CurrentWeather == WeatherType.NangGat) f *= 1.15f;
                    break;
                case FarmLaborTask.Medicate:
                    if (pigPanicking) f *= 1.50f;
                    break;
                case FarmLaborTask.CleanPlot:
                    if (engine.Farm.HabitatIndex < 40) f *= 1.25f;
                    if (engine.CurrentWeather == WeatherType.Mua) f *= 1.15f;
                    break;
            }
            return f;
        }

        /// <summary>Thời tiết loại Cố định (GDD 7.2): bỏ hệ số Buổi.</summary>
        public static bool IsFixedWeather(WeatherType w) =>
            w == WeatherType.NangGat || w == WeatherType.Mua || w == WeatherType.GiongBao || w == WeatherType.MuaBucXa;

        /// <summary>Hệ số môi trường + sức khỏe + tải của GDD 9.3 (đã kẹp ×0,60–×2,00).</summary>
        public static float EnvironmentMultiplier(GameEngine engine, float carriedKg = 0f, float reduction = 0f)
        {
            float sum = 0f;
            var w = engine.CurrentWeather;
            if (!IsFixedWeather(w))
            {
                sum += engine.Clock.GetTimeOfDay() switch
                {
                    TimeOfDay.Sang => -0.05f,
                    TimeOfDay.Trua => 0.10f,
                    TimeOfDay.Chieu => 0f,
                    _ => 0.05f, // Tối không đèn (Core chưa biết đèn: ADR-0018)
                };
            }
            sum += w switch
            {
                WeatherType.NangGat => 0.15f,
                WeatherType.Mua => 0.10f,
                WeatherType.GiongBao => 0.20f,
                WeatherType.RetDam => 0.10f,
                WeatherType.NomAm => 0.05f,
                WeatherType.NhieuMay => -0.03f,
                _ => 0f
            };
            int cond = engine.Character.Stamina.Condition;
            sum += cond >= 80 ? 0f : cond >= 60 ? 0.05f : cond >= 40 ? 0.10f : cond >= 20 ? 0.20f : 0.35f;
            // 9.3 "Trạng thái thời tiết": Ướt Lạnh +5% / Cảm Gió +10% / Nhiễm Xạ ≥ 3 +10% (Cảm Nắng là trần: PlayerWeatherStatus.StaminaCap)
            sum += engine.WeatherStatus.StaminaFactorSum(engine);
            // 9.3 "Đói": not eaten > 8 h +10 %, > 16 h +25 %
            sum += engine.Character.Stamina.HungerCostFactor(engine.Clock.TotalElapsedGameMinutes);
            if (carriedKg > CarryCapacity.HeavyKilograms)
            {
                sum += 0.35f;
                if (cond < 40) sum += 0.15f; // GDD 9.2: mang vác nặng, Thể trạng < 40 thêm +15%
            }
            float m = (1f + sum) * (1f - Math.Clamp(reduction, 0f, 1f));
            return Math.Clamp(m, 0.60f, 2.00f);
        }

        /// <summary>Thể lực thực tế của một việc (chưa trừ).</summary>
        public static float Cost(GameEngine engine, FarmLaborTask task, float? baseOverride = null,
            float bagKg = 0f, bool pigPanicking = false, float carriedKg = 0f, int pigsBroughtToMarket = 0)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var (baseCost, _) = Spec(task);
            float b = baseOverride ?? baseCost;
            if (task == FarmLaborTask.MarketTrip) b += 2f * Math.Max(0, pigsBroughtToMarket);
            float cost = b * TaskFactor(engine, task, bagKg, pigPanicking, pigsBroughtToMarket) * EnvironmentMultiplier(engine, carriedKg);
            return (float)Math.Round(cost, 1);
        }

        /// <summary>
        /// Trừ Thể lực việc trại: tức thời + Tải ngày theo % GDD 9.2; quy đổi EXP lao động (GDD 8.2);
        /// "Dọn vệ sinh" được ghi vào khóa chuyên biệt 2 của Khoa (GDD 10.8).
        /// </summary>
        public static (bool success, float cost, string message) TrySpend(GameEngine engine, FarmLaborTask task, float? baseOverride = null,
            float bagKg = 0f, bool pigPanicking = false, float carriedKg = 0f, int pigsBroughtToMarket = 0)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            float cost = Cost(engine, task, baseOverride, bagKg, pigPanicking, carriedKg, pigsBroughtToMarket);
            var stamina = engine.Character.Stamina;
            var (_, share) = Spec(task);
            if (!stamina.TryConsumeStamina(cost, share))
                return (false, cost, $"Kiệt sức! Cần {cost:0.#} Thể lực (còn {stamina.CurrentStamina:0}). Hãy ăn, nghỉ hoặc ngủ.");
            MartialProgression.AddLaborStamina(engine.Character, cost);
            if (task == FarmLaborTask.CleanPlot) engine.DeepMechanics.SpecialtyLocks.RecordManualCleaning();
            return (true, cost, string.Empty);
        }
    }
}
