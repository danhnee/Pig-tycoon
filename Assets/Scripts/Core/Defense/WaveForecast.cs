using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// What the player is allowed to know about the coming waves (GDD 2.3 / 2.4 / 23). 3 Nov audit: the HUD always showed
    /// the exact day of the major wave and the hour of the next minor wave, which made Intel and the Radar worthless.
    /// <list type="bullet">
    /// <item>Major wave: exact once it is within the default warning (1–2 days, rolled per cycle). Before that, only what the
    /// Intel bought this cycle reveals: level 1 the stage's cycle length, level 3 ±2 days, level 4 ±1 day and the time of
    /// day, level 5 exact.</item>
    /// <item>Minor wave: unknown until 2–6 game hours before it (rolled per wave); a powered Radar Uy Hiếp warns earlier.</item>
    /// <item>Golden season (Mùa Kinh Tế Vàng): announced, with no minor wave in its first 5 days.</item>
    /// </list>
    /// Decisions: design clarifications §C-15.
    /// </summary>
    public static class WaveForecast
    {
        /// <summary>TẠM (C-15): hours of extra minor-wave notice from a powered Radar Uy Hiếp ("Radar có thể báo sớm hơn").</summary>
        public const int RadarExtraNoticeHours = 6;
        public const string RadarId = "RadarUyHiep";

        public readonly struct MajorInfo
        {
            public readonly bool Known;      // any number at all
            public readonly bool Exact;
            public readonly int MinDays, MaxDays;
            public readonly string Text;
            public MajorInfo(bool known, bool exact, int min, int max, string text) { Known = known; Exact = exact; MinDays = min; MaxDays = max; Text = text; }
        }

        public static MajorInfo Major(GameEngine e)
        {
            var c = e.Clock;
            int left = c.GetDaysUntilMajorWave();
            int level = e.Intel.CycleNumber == c.CycleNumber ? e.Intel.Level : 0;
            if (left <= c.MajorWarningDays || level >= IntelBureau.FullLevel)
                return new MajorInfo(true, true, left, left, left == 0 ? "Đợt lớn: ĐÊM NAY" : $"Đợt lớn: còn {left} ngày");
            if (level >= 4)
                return new MajorInfo(true, false, Math.Max(1, left - 1), left + 1, $"Đợt lớn: {Math.Max(1, left - 1)}–{left + 1} ngày nữa, về đêm");
            if (level >= 3)
                return new MajorInfo(true, false, Math.Max(1, left - 2), left + 2, $"Đợt lớn: khoảng {left}±2 ngày");
            if (level >= 1)
            {
                var (min, max) = StageCycleRange(c);
                int lo = Math.Max(1, min - c.CycleDayIndex), hi = Math.Max(lo, max - c.CycleDayIndex);
                return new MajorInfo(true, false, lo, hi, $"Chu kỳ dài {min}–{max} ngày: đợt lớn sau {lo}–{hi} ngày");
            }
            return new MajorInfo(false, false, -1, -1, "Đợt lớn: chưa rõ ngày (hỏi Bàn Tình Báo)");
        }

        /// <summary>The cycle-length band the clock rolled from (what Intel level 1 reveals: "Chu kỳ này dài 7–10 ngày").</summary>
        public static (int min, int max) StageCycleRange(GameClock c)
        {
            if (c.IsGoldenSeason) return (12, 15);
            if (c.CycleNumber <= 7) return (6, 8);
            if (c.CycleNumber <= 19) return (7, 10);
            return (8, 12);
        }

        public static int MinorNoticeHours(GameEngine e) =>
            e.Clock.MinorWaveNoticeHours + (e.Defense.HasPoweredBuilding(RadarId) ? RadarExtraNoticeHours : 0);

        /// <summary>Known only inside the notice window; text is approximate ("khoảng N giờ").</summary>
        public static (bool known, int hours, string text) Minor(GameEngine e)
        {
            var c = e.Clock;
            if (c.MinorWavesRemainingInCycle <= 0 || c.HoursUntilNextMinorWave <= 0) return (false, -1, "");
            if (c.HoursUntilNextMinorWave > MinorNoticeHours(e)) return (false, -1, "");
            int h = c.HoursUntilNextMinorWave;
            return (true, h, h <= 1 ? "Đợt nhỏ: sắp tới (dưới 1 giờ)" : $"Đợt nhỏ: khoảng {h} giờ nữa");
        }

        public static string GoldenSeasonText(GameClock c) =>
            c.IsGoldenSeason ? "Mùa Kinh Tế Vàng: chu kỳ dài, áp lực quái thấp, 5 ngày đầu không có đợt nhỏ." : "";
    }
}
