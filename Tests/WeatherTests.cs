using System;
using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 7.2: weather changes over days, spells last 1-3 days, table frequencies, Mưa Bức Xạ only from cycle 6.</summary>
internal static class WeatherTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void RunFrequencies()
    {
        var counts = new Dictionary<WeatherType, int>();
        int n = 20000;
        for (int d = 0; d < n; d++)
        {
            var (w, days) = WeatherScheduler.Roll(20261101, d, 3);
            Check(days >= 1 && days <= 3, "a spell lasts 1-3 days");
            Check(w != WeatherType.MuaBucXa, "no radiation rain before cycle 6");
            counts[w] = counts.TryGetValue(w, out var c) ? c + 1 : 1;
        }
        foreach (var (w, p) in WeatherScheduler.Table)
        {
            if (w == WeatherType.MuaBucXa) continue;
            double share = (counts.TryGetValue(w, out var c) ? c : 0) / (double)n * 100.0;
            double expected = p * 100.0 / 99.0;
            Check(Math.Abs(share - expected) < 1.5, $"{w} share {share:0.0}% vs GDD {expected:0.0}%");
        }
        int bx = 0;
        for (int d = 0; d < n; d++) if (WeatherScheduler.Roll(7, d, 6).weather == WeatherType.MuaBucXa) bx++;
        Check(bx > n * 0.004 && bx < n * 0.02, "radiation rain ~1% from cycle 6");
    }

    public static void RunEngineDays()
    {
        var e = new GameEngine(seed: 11);
        var seen = new HashSet<WeatherType>();
        int changes = 0; var last = e.CurrentWeather; int runLength = 0, maxRun = 0;
        for (int day = 0; day < 60; day++)
        {
            int start = e.Clock.CurrentDay;
            for (int guard = 0; guard < 30 && e.Clock.CurrentDay == start; guard++) e.Tick(60);
            seen.Add(e.CurrentWeather);
            if (e.CurrentWeather != last) { changes++; runLength = 1; last = e.CurrentWeather; } else runLength++;
            maxRun = Math.Max(maxRun, runLength);
        }
        Check(seen.Count >= 5, $"60 days show several kinds of weather (saw {seen.Count})");
        Check(changes >= 10, "the weather actually changes");
        Check(e.WeatherDaysRemaining >= 1 && e.WeatherDaysRemaining <= 3, "a spell is running");
    }
}
