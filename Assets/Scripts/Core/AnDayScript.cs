namespace PigTycoon.Core
{
    /// <summary>
    /// Một ngày của An trên đồng hồ giờ hiện có. Không đổi mốc ngày của GameClock.
    /// Kịch bản này không trừ máu.
    /// </summary>
    public enum AnDayBeat
    {
        Sleep,
        WakeAndFeed,
        FenceCheck,
        IndoorRest,
        MarketGlance,
        HerdRound
    }

    public static class AnDayScript
    {
        public static AnDayBeat BeatAt(int hour, int minute)
        {
            if (hour < 0) hour = 0;
            if (hour > 23) hour = 23;
            if (minute < 0) minute = 0;
            if (minute > 59) minute = 59;

            int mins = (hour * 60) + minute;
            if (mins >= 6 * 60 && mins < 9 * 60) return AnDayBeat.WakeAndFeed;
            if (mins >= 9 * 60 && mins < 12 * 60) return AnDayBeat.FenceCheck;
            if (mins >= 12 * 60 && mins < 14 * 60) return AnDayBeat.IndoorRest;
            if (mins >= 14 * 60 && mins < 18 * 60) return AnDayBeat.MarketGlance;
            if (mins >= 18 * 60 && mins < 21 * 60) return AnDayBeat.HerdRound;
            return AnDayBeat.Sleep;
        }

        public static void Apply(CharacterData an, AnDayBeat beat)
        {
            int hp = an.Hp;
            if (beat == AnDayBeat.Sleep)
            {
                an.Stamina.DailyLoad = 0f;
            }
            else if (beat == AnDayBeat.WakeAndFeed)
            {
                an.Stamina.CurrentStamina = an.Stamina.MaxStamina;
            }

            an.Hp = hp < 1 ? 1 : hp;
        }
    }
}
