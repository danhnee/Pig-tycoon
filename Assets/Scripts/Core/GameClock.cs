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
        public int CurrentHour { get; private set; }
        public int CurrentMinute { get; private set; }

        public int CycleNumber { get; private set; }
        public int CycleTotalDays { get; private set; }
        public int CycleDayIndex { get; private set; }

        public int MinorWavesRemainingInCycle { get; private set; }
        public int HoursUntilNextMinorWave { get; private set; }
        public int SleepCooldownHoursRemaining { get; private set; }
        public bool IsDuringCombatWave { get; set; }

        private readonly Random random = new Random();

        public GameClock(int initialDay = 1, int initialHour = 6, int initialMinute = 0)
        {
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
                    CurrentDay += 1;
                    CycleDayIndex += 1;
                    newDayStarted = true;
                }
            }

            TimeOfDay curTimeOfDay = GetTimeOfDay();
            bool isMajorWaveDay = (CycleDayIndex >= CycleTotalDays);

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

        public void CompleteMajorWave(bool hadSevereLosses = false, bool highEventPressure = false)
        {
            CycleNumber += 1;
            CycleDayIndex = 1;
            CycleTotalDays = RollCycleLength(CycleNumber, hadSevereLosses, highEventPressure);
            MinorWavesRemainingInCycle = RollMinorWaveCount(CycleNumber);
            HoursUntilNextMinorWave = ScheduleNextMinorWave();
        }

        private int RollCycleLength(int cycle, bool hadLosses, bool highPressure)
        {
            if (random.NextDouble() < 0.08)
            {
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
            return 24 + random.Next(48);
        }

        public int GetDaysUntilMajorWave()
        {
            return Math.Max(0, CycleTotalDays - CycleDayIndex);
        }
    }
}
