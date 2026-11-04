using System;
using PigTycoon.Core;

/// <summary>GDD 2.3 / 23: the player sees the waves only as far as the default warning, the Radar and the Intel allow.</summary>
internal static class WaveForecastTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void RunMajorWaveNeedsWarningOrIntel()
    {
        var e = new GameEngine(5);
        var c = e.Clock;
        Check(c.MajorWarningDays >= 1 && c.MajorWarningDays <= 2, "default major warning is 1–2 days");
        if (c.GetDaysUntilMajorWave() > c.MajorWarningDays)
            Check(!WaveForecast.Major(e).Known, "day 1 without Intel: the major wave day is unknown");

        var (min, max) = WaveForecast.StageCycleRange(c);
        Check(c.IsGoldenSeason ? (min == 12 && max == 15) : (max - min >= 2), "Intel 1 reveals the stage band");

        int guard = 0;
        while (c.GetDaysUntilMajorWave() > c.MajorWarningDays && guard++ < 20) e.Tick(24 * 60);
        var known = WaveForecast.Major(e);
        Check(known.Known && known.Exact && known.MinDays == c.GetDaysUntilMajorWave(), "inside the warning window the day is exact: " + known.Text);
    }

    public static void RunMinorWaveNoticeWindow()
    {
        for (int seed = 1; seed < 60; seed++)
        {
            var e = new GameEngine(seed);
            var c = e.Clock;
            if (c.MinorWavesRemainingInCycle <= 0) continue;
            Check(c.MinorWaveNoticeHours >= 2 && c.MinorWaveNoticeHours <= 6, "minor notice is 2–6 h");
            Check(!WaveForecast.Minor(e).known, "a minor wave 24 h+ away is not shown");
            int notice = WaveForecast.MinorNoticeHours(e);
            int toWindow = c.HoursUntilNextMinorWave - notice;
            if (toWindow > 0) e.Tick(toWindow * 60);
            if (c.IsDuringCombatWave || c.MinorWavesRemainingInCycle <= 0) continue;
            var m = WaveForecast.Minor(e);
            Check(m.known && m.hours <= notice, $"inside the {notice} h window the minor wave is announced ({m.text})");
            return;
        }
        throw new InvalidOperationException("no seed with a minor wave in cycle 1");
    }

    public static void RunGoldenSeasonQuietStart()
    {
        int found = 0;
        for (int seed = 1; seed < 4000 && found < 3; seed++)
        {
            var c = new GameClock(seed: seed);
            if (!c.IsGoldenSeason) continue;
            found++;
            Check(c.CycleTotalDays >= 12 && c.CycleTotalDays <= 15, "golden season is 12–15 days");
            Check(c.HoursUntilNextMinorWave >= GameClock.GoldenSeasonQuietDays * 24, "no minor wave in the first 5 days of a golden season");
            Check(WaveForecast.GoldenSeasonText(c).Length > 0, "the golden season is announced");
        }
        Check(found > 0, "some seed rolls a golden season (8 %)");
    }

    public static void RunForecastSurvivesSave()
    {
        var e = new GameEngine(9);
        var re = new GameEngine(1);
        re.RestoreFromSave(e.CreateSaveData());
        Check(re.Clock.MajorWarningDays == e.Clock.MajorWarningDays && re.Clock.MinorWaveNoticeHours == e.Clock.MinorWaveNoticeHours
              && re.Clock.IsGoldenSeason == e.Clock.IsGoldenSeason, "warning rolls survive save/load");
    }
}
