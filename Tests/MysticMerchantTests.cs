using System;
using PigTycoon.Core;

/// <summary>GDD 21: Bí nhân — one roll per night (3% × cycle factor × (1+ÁLSK/100), max 25%), a random window
/// 18:00–04:00, exactly 2 game hours, never by day.</summary>
internal static class MysticMerchantTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static (int nightsVisited, int badWindows, int dayVisits) Simulate(int nights, int pressure, MarketTheme theme, int seed)
    {
        var eco = new EconomyManager(seed);
        int visited = 0, bad = 0, dayVisits = 0;
        for (int day = 1; day <= nights; day++)
        {
            int activeHours = 0, firstHour = -1, lastHour = -1;
            for (int k = 0; k < 24; k++)
            {
                int hour = (6 + k) % 24;                    // the farm day runs 06:00 -> 06:00
                eco.UpdateMysticMerchantHour(day, hour, pressure, theme);
                if (!eco.MysticMerchant.IsActive) continue;
                if (hour >= 6 && hour < 18) dayVisits++;
                activeHours++;
                if (firstHour < 0) firstHour = k;
                lastHour = k;
            }
            if (activeHours > 0)
            {
                visited++;
                int startHour = (6 + firstHour) % 24;
                bool inWindow = (startHour >= 18 || startHour < 3) && (lastHour - firstHour + 1) == 2;
                if (activeHours != 2 || !inWindow) bad++;
            }
        }
        return (visited, bad, dayVisits);
    }

    public static void RunNightWindow()
    {
        const int nights = 20000;
        var (calm, bad1, day1) = Simulate(nights, 0, MarketTheme.CanBang, 3);
        double calmRate = calm / (double)nights;
        Check(calmRate > 0.022 && calmRate < 0.038, $"~3% of nights at ÁLSK 0 (measured {calmRate:P1})");
        Check(bad1 == 0 && day1 == 0, "exactly 2 consecutive night hours, starting 18:00-02:00, never by day");
        var (tense, _, _) = Simulate(nights, 100, MarketTheme.HoangLoanDich, 4);
        double tenseRate = tense / (double)nights;
        Check(tenseRate > 0.10 && tenseRate < 0.14, $"×2 theme × (1+100/100) = 12% (measured {tenseRate:P1})");
        var (capped, _, _) = Simulate(nights, 400, MarketTheme.HoangLoanDich, 5);
        Check(capped / (double)nights < 0.27, "never above 25% per night");
    }
}
