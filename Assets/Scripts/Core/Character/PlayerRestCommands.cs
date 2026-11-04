using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// Ngủ, nghỉ và ăn của nhân vật người chơi (GDD 9.4–9.5).
    /// Ngủ tua thời gian qua <see cref="GameEngine.Tick"/> nên mô phỏng từng giờ và qua ngày đều chạy.
    /// </summary>
    public static class PlayerRestCommands
    {
        /// <summary>
        /// Ngủ chuẩn tới 06:00 (GDD 9.5). Không ngủ được trong đợt quái, khi Sương Mù, hoặc khi có quái trong ranh giới trại.
        /// <paramref name="monstersInsideFarm"/> do runtime báo (Core không thấy vị trí quái).
        /// Nếu một đợt quái nhỏ nổ ra giữa giấc, người chơi bị đánh thức: chỉ hồi như ngồi nghỉ 6/giờ (ADR-0018).
        /// </summary>
        public static (bool success, string message, int hoursSlept) Sleep(GameEngine engine, bool monstersInsideFarm = false)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var clock = engine.Clock;
            var reason = SleepBlockedReason(engine, monstersInsideFarm);
            if (reason != null) return (false, reason, 0);

            int startTotal = clock.TotalElapsedGameMinutes;
            engine.WeatherStatus.IsOutdoors = false; // ngủ trong nhà: không dính trạng thái thời tiết (GDD 7.2, ADR-0022)
            int remaining = clock.MinutesUntilNextMorning();
            bool interrupted = false;
            while (remaining > 0)
            {
                int step = Math.Min(remaining, 60 - clock.CurrentMinute);
                int before = clock.TotalElapsedGameMinutes;
                engine.Tick(step);
                int advanced = clock.TotalElapsedGameMinutes - before;
                remaining -= advanced;
                if (advanced <= 0 || clock.IsDuringCombatWave || clock.IsMinorWaveTriggered)
                {
                    interrupted = remaining > 0;
                    break;
                }
            }

            int sleptMinutes = clock.TotalElapsedGameMinutes - startTotal;
            var stamina = engine.Character.Stamina;
            if (interrupted)
            {
                int hours = sleptMinutes / 60;
                stamina.RestForGameHours(hours);
                return (true, $"Bị đánh thức sau {hours} giờ: quái đang kéo tới! Chỉ hồi như ngồi nghỉ.", hours);
            }

            clock.MarkWokeFromStandardSleep();
            int bonusCondition = 0;
            if (engine.DeepMechanics.SpecialtyTraitUnlocked && engine.Character.Identity == PlayerId.Khoa)
            {
                // GDD 10.8 Khoa "Ngủ Say": ngủ chuẩn Thể trạng +3, cứ 2 ngày xóa 1 tầng Mệt Dội.
                bonusCondition = 3;
                engine.TraitState.DeepSleepNights++;
                if (engine.TraitState.DeepSleepNights % 2 == 0 && stamina.ReboundFatigueStacks > 0)
                    stamina.ReboundFatigueStacks -= 1;
            }
            stamina.PerformSleepReset(bonusCondition);
            int shownHours = (int)Math.Ceiling(sleptMinutes / 60.0);
            return (true, $"Đã ngủ {shownHours} giờ đến 06:00 sáng.", shownHours);
        }

        /// <summary>null nếu được ngủ; nếu không, lý do (tiếng Việt) theo GDD 9.5.</summary>
        public static string? SleepBlockedReason(GameEngine engine, bool monstersInsideFarm = false)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (engine.Clock.IsDuringCombatWave) return "Không thể ngủ trong khi có đợt quái tấn công!";
            if (engine.CurrentWeather == WeatherType.SuongMu) return "Không thể ngủ khi Sương Mù đang phủ trại!";
            if (monstersInsideFarm) return "Không thể ngủ khi còn quái trong ranh giới trại!";
            if (engine.Clock.SleepCooldownHoursRemaining > 0)
                return $"Chưa thể ngủ! Cooldown còn {engine.Clock.SleepCooldownHoursRemaining} giờ game.";
            return null;
        }

        /// <summary>Ngồi nghỉ N giờ game: tua thời gian qua GameEngine.Tick và hồi 6 Thể lực/giờ (GDD 9.4).</summary>
        public static (bool success, string message, float recovered) Rest(GameEngine engine, int hours)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (hours <= 0) return (false, "Số giờ nghỉ không hợp lệ.", 0f);
            if (engine.Clock.IsDuringCombatWave) return (false, "Không thể ngồi nghỉ trong đợt quái!", 0f);
            int start = engine.Clock.TotalElapsedGameMinutes;
            engine.Tick(hours * 60);
            int restedHours = (engine.Clock.TotalElapsedGameMinutes - start) / 60;
            float recovered = engine.Character.Stamina.RestForGameHours(restedHours);
            return (true, $"Ngồi nghỉ {restedHours} giờ: +{recovered:0} Thể lực.", recovered);
        }

        /// <summary>Ăn một bữa: thường +20, tốt +35, tối đa 3 bữa/ngày (GDD 9.4).</summary>
        public static (bool success, string message, float recovered) EatMeal(GameEngine engine, bool goodMeal)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (!engine.Character.Stamina.TryEatMeal(goodMeal, out float recovered))
                return (false, $"Đã ăn đủ {StaminaSystem.MaxMealsPerDay} bữa hôm nay.", 0f);
            engine.Character.Stamina.LastMealMinute = engine.Clock.TotalElapsedGameMinutes;
            return (true, $"Ăn {(goodMeal ? "bữa tốt" : "bữa thường")}: +{recovered:0} Thể lực.", recovered);
        }
    }
}
