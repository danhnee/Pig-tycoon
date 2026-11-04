using System;

namespace PigTycoon.Core
{
    [Serializable]
    public class ClockTickResult
    {
        public int MinutesAdvanced;
        public bool NewDayStarted;
        public bool TimeOfDayChanged;
        public TimeOfDay PreviousTimeOfDay;
        public TimeOfDay CurrentTimeOfDay;
        public bool IsMajorWaveDay;
        public bool IsMinorWaveTriggered;
    }

    [Serializable]
    public class GameClock
    {
        public const float SecondsPerGameMinute = 40f / 60f;
        public const float SecondsPerGameHour = 40f;
        public const float SecondsPerGameDay = 960f;

        public int CurrentDay { get; private set; }
        public int DayNumber => CurrentDay;
        // Farm day starts at06:00; day0 is supported only for deterministic legacy migration.
        public int TotalElapsedGameMinutes => (CurrentDay - 1) * 1440 + ((CurrentHour + 18) % 24) * 60 + CurrentMinute;
        public int CurrentHour { get; private set; }
        public int CurrentMinute { get; private set; }

        public int CycleNumber { get; private set; }
        public int CycleTotalDays { get; private set; }
        public int CycleDayIndex { get; private set; }

        public int MinorWavesRemainingInCycle { get; private set; }
        public int HoursUntilNextMinorWave { get; private set; }
        public int SleepCooldownHoursRemaining { get; set; }
        public bool IsDuringCombatWave { get; set; }
        public bool IsMinorWaveTriggered { get; set; } = false;

        private readonly Random random;
        /// <summary>Separate stream for the warning rolls so the cycle/minor-wave rolls stay as they were for a seed.</summary>
        private readonly Random forecastRandom;

        /// <summary>GDD 2.3: 8 % of cycles are a long "Mùa Kinh Tế Vàng" (12–15 days, no minor wave in the first 5 days).</summary>
        public bool IsGoldenSeason { get; private set; }
        /// <summary>GDD 2.3 default warning of the major wave: 1–2 days before (rolled per cycle). See WaveForecast.</summary>
        public int MajorWarningDays { get; private set; } = 2;
        /// <summary>GDD 2.3 default warning of the next minor wave: 2–6 game hours before (rolled per wave).</summary>
        public int MinorWaveNoticeHours { get; private set; } = 4;
        public const int GoldenSeasonQuietDays = 5;

        public GameClock(int initialDay = 1, int initialHour = 6, int initialMinute = 0, int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value ^ 0x5A5A5A5A) : new Random();
            forecastRandom = seed.HasValue ? new Random(seed.Value ^ 0x13579BDF) : new Random();
            CurrentDay = initialDay;
            CurrentHour = initialHour;
            CurrentMinute = initialMinute;
            CycleNumber = 1;
            CycleDayIndex = 1;
            SleepCooldownHoursRemaining = 0;
            IsDuringCombatWave = false;

            CycleTotalDays = RollCycleLength(1, false, false);
            MinorWavesRemainingInCycle = RollMinorWaveCount(1);
            HoursUntilNextMinorWave = ScheduleNextMinorWave();
            MajorWarningDays = 1 + forecastRandom.Next(2);
        }

        public TimeOfDay GetTimeOfDay()
        {
            if (CurrentHour >= 6 && CurrentHour < 10) return TimeOfDay.Sang;
            if (CurrentHour >= 10 && CurrentHour < 14) return TimeOfDay.Trua;
            if (CurrentHour >= 14 && CurrentHour < 18) return TimeOfDay.Chieu;
            return TimeOfDay.Toi;
        }

        public ClockTickResult TickMinutes(int minutes = 1)
        {
            TimeOfDay prevTimeOfDay = GetTimeOfDay();
            if (IsDuringCombatWave || minutes <= 0)
            {
                // GDD 2 / sổ tay 17.4: đồng hồ ngày dừng trong đợt quái.
                return new ClockTickResult
                {
                    MinutesAdvanced = 0,
                    NewDayStarted = false,
                    TimeOfDayChanged = false,
                    PreviousTimeOfDay = prevTimeOfDay,
                    CurrentTimeOfDay = prevTimeOfDay,
                    IsMajorWaveDay = CycleDayIndex >= CycleTotalDays,
                    IsMinorWaveTriggered = false
                };
            }

            bool newDayStarted = false;
            bool isMinorWaveTriggered = false;

            CurrentMinute += minutes;
            while (CurrentMinute >= 60)
            {
                CurrentMinute -= 60;
                CurrentHour += 1;

                if (SleepCooldownHoursRemaining > 0)
                {
                    SleepCooldownHoursRemaining = Math.Max(0, SleepCooldownHoursRemaining - 1);
                }

                if (HoursUntilNextMinorWave > 0)
                {
                    HoursUntilNextMinorWave -= 1;
                    if (HoursUntilNextMinorWave == 0 && MinorWavesRemainingInCycle > 0)
                    {
                        isMinorWaveTriggered = true;
                        MinorWavesRemainingInCycle -= 1;
                        HoursUntilNextMinorWave = ScheduleNextMinorWave();
                    }
                }

                if (CurrentHour >= 24)
                {
                    CurrentHour = 0;
                }
                if (CurrentHour == 6)
                {
                    CurrentDay += 1;
                    CycleDayIndex += 1;
                    newDayStarted = true;
                }
            }

            TimeOfDay curTimeOfDay = GetTimeOfDay();
            bool isMajorWaveDay = (CycleDayIndex >= CycleTotalDays);
            IsMinorWaveTriggered = isMinorWaveTriggered;

            return new ClockTickResult
            {
                MinutesAdvanced = minutes,
                NewDayStarted = newDayStarted,
                TimeOfDayChanged = prevTimeOfDay != curTimeOfDay,
                PreviousTimeOfDay = prevTimeOfDay,
                CurrentTimeOfDay = curTimeOfDay,
                IsMajorWaveDay = isMajorWaveDay,
                IsMinorWaveTriggered = isMinorWaveTriggered
            };
        }

        /// <summary>
        /// Chỉ tua đồng hồ (không chạy mô phỏng giờ/ngày của GameEngine). Gameplay phải ngủ qua
        /// <see cref="PlayerRestCommands.Sleep"/> để GameEngine.Tick xử lý từng giờ và qua ngày (GDD 9.5).
        /// </summary>
        public (bool success, string message, int hoursSlept) PerformStandardSleep(bool bypassCooldown = false)
        {
            if (IsDuringCombatWave)
            {
                return (false, "Không thể ngủ trong khi có đợt quái tấn công!", 0);
            }

            if (!bypassCooldown && SleepCooldownHoursRemaining > 0)
            {
                return (false, $"Chưa thể ngủ! Cooldown còn {SleepCooldownHoursRemaining} giờ game.", 0);
            }

            int hoursToSleep = CurrentHour >= 6 ? (24 - CurrentHour) + 6 : 6 - CurrentHour;
            TickMinutes(hoursToSleep * 60 - CurrentMinute);
            CurrentMinute = 0;
            CurrentHour = 6;
            SleepCooldownHoursRemaining = 6;

            return (true, $"Đã ngủ {hoursToSleep} giờ đến 06:00 sáng.", hoursToSleep);
        }

        /// <summary>Số phút game từ bây giờ tới 06:00 sáng kế tiếp (đúng 24 giờ nếu đang 06:00).</summary>
        public int MinutesUntilNextMorning()
        {
            int hoursToSleep = CurrentHour >= 6 ? (24 - CurrentHour) + 6 : 6 - CurrentHour;
            return hoursToSleep * 60 - CurrentMinute;
        }

        /// <summary>GDD 9.5: cooldown ngủ 6 giờ game sau khi dậy. PlayerRestCommands gọi sau khi đã tua qua GameEngine.Tick.</summary>
        public const int SleepCooldownHours = 6;
        public void MarkWokeFromStandardSleep() => SleepCooldownHoursRemaining = SleepCooldownHours;

        public void CompleteMajorWave(bool hadSevereLosses = false, bool highEventPressure = false)
        {
            CycleNumber += 1;
            CycleDayIndex = 0; // đợt lớn đóng chu kỳ vào đêm ngày cuối; 06:00 kế tiếp là ngày 1 của chu kỳ mới (playtest: chu kỳ bị hụt 1 ngày)
            CycleTotalDays = RollCycleLength(CycleNumber, hadSevereLosses, highEventPressure);
            MinorWavesRemainingInCycle = RollMinorWaveCount(CycleNumber);
            HoursUntilNextMinorWave = ScheduleNextMinorWave();
            MajorWarningDays = 1 + forecastRandom.Next(2);
        }

        private int RollCycleLength(int cycle, bool hadLosses, bool highPressure)
        {
            IsGoldenSeason = false;
            if (random.NextDouble() < 0.08)
            {
                IsGoldenSeason = true;
                return 12 + random.Next(4); // 12 - 15 ngày: Mùa Kinh Tế Vàng
            }

            int min = 6, max = 8;
            if (cycle <= 7) { min = 6; max = 8; }
            else if (cycle <= 19) { min = 7; max = 10; }
            else { min = 8; max = 12; }

            int length = min + random.Next(max - min + 1);
            if (highPressure) length = Math.Max(6, length - 1);
            if (hadLosses) length += 1;
            return length;
        }

        private int RollMinorWaveCount(int cycle)
        {
            if (cycle <= 7) return random.NextDouble() < 0.6 ? 1 : 0;
            if (cycle <= 19) return 1 + (random.NextDouble() < 0.5 ? 1 : 0);
            return 1 + random.Next(3);
        }

        private int ScheduleNextMinorWave()
        {
            int hours = 24 + random.Next(48);
            if (IsGoldenSeason)
            {
                // GDD 2.3: "không đợt nhỏ trong 5 ngày đầu" of a golden season
                int hoursIntoCycle = Math.Max(0, CycleDayIndex - 1) * 24 + ((CurrentHour + 18) % 24);
                hours = Math.Max(hours, GoldenSeasonQuietDays * 24 - hoursIntoCycle);
            }
            MinorWaveNoticeHours = 2 + (forecastRandom?.Next(5) ?? 2);
            return hours;
        }

        /// <summary>Restores the warning state saved next to the clock (older saves keep the defaults).</summary>
        public void RestoreForecast(bool goldenSeason, int majorWarningDays, int minorNoticeHours)
        {
            IsGoldenSeason = goldenSeason;
            if (majorWarningDays >= 1 && majorWarningDays <= 2) MajorWarningDays = majorWarningDays;
            if (minorNoticeHours >= 2 && minorNoticeHours <= 6) MinorWaveNoticeHours = minorNoticeHours;
        }

        public int GetDaysUntilMajorWave()
        {
            return Math.Max(0, CycleTotalDays - CycleDayIndex);
        }

        public void RestoreFromSave(
            int currentDay,
            int currentHour,
            int currentMinute,
            int cycleNumber,
            int cycleTotalDays,
            int cycleDayIndex,
            int minorWavesRemaining,
            int hoursUntilNextMinorWave,
            int sleepCooldownHoursRemaining,
            bool isDuringCombatWave)
        {
            CurrentDay = Math.Max(0, currentDay);
            CurrentHour = Math.Clamp(currentHour, 0, 23);
            CurrentMinute = Math.Clamp(currentMinute, 0, 59);
            CycleNumber = Math.Max(1, cycleNumber);
            CycleTotalDays = Math.Max(1, cycleTotalDays);
            CycleDayIndex = Math.Clamp(cycleDayIndex, 0, CycleTotalDays);
            MinorWavesRemainingInCycle = Math.Max(0, minorWavesRemaining);
            HoursUntilNextMinorWave = Math.Max(0, hoursUntilNextMinorWave);
            SleepCooldownHoursRemaining = Math.Max(0, sleepCooldownHoursRemaining);
            IsDuringCombatWave = isDuringCombatWave;
        }
    }
}
