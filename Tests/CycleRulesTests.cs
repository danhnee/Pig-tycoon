using System;
using PigTycoon.Core;

/// <summary>GDD 2.3 cycle adjustments and 3.5 wave EXP, as the world calls them (3 Nov 2026: the world used to pass nothing).</summary>
internal static class CycleRulesTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static int CycleLengthAfter(int seed, int pigsLost, int herd, int pressure)
    {
        var e = new GameEngine(seed);
        for (int i = 0; i < herd; i++) e.Pigs.Add(Pig.CreateDefault("p" + i, "p" + i, GeneLineId.HongDien, GeneRarity.Thuong, "Cai", 16, 95f));
        e.Farm.EventPressureIndex = pressure;
        e.CloseMajorWaveCycle(pigsLost, herd);
        return e.Clock.CycleTotalDays;
    }

    public static void Run()
    {
        // same seed → same roll; only the adjustments differ (golden-season rolls are skipped: they ignore adjustments)
        int checkedSeeds = 0;
        for (int seed = 1; seed < 60 && checkedSeeds < 10; seed++)
        {
            int baseLen = CycleLengthAfter(seed, 0, 10, 0);
            if (baseLen >= 12) continue;
            checkedSeeds++;
            Check(CycleLengthAfter(seed, 3, 10, 0) == baseLen + 1, "losing ≥ 30 % of the herd adds a recovery day");
            Check(CycleLengthAfter(seed, 2, 10, 0) == baseLen, "losing < 30 % changes nothing");
            Check(CycleLengthAfter(seed, 0, 10, 60) == Math.Max(6, baseLen - 1), "Áp Lực Sự Kiện ≥ 60 shortens the cycle by a day (min 6)");
            Check(CycleLengthAfter(seed, 0, 10, 59) == baseLen, "pressure 59 does not");
        }
        Check(checkedSeeds == 10, "enough non-golden rolls checked");
        Check(GameEngine.WaveSurvivalFarmExp(1) == 150 && GameEngine.WaveSurvivalFarmExp(7) == 1050 && GameEngine.WaveSurvivalFarmExp(40) == 3000,
            "surviving a wave gives 150 × cycle farm EXP, at most 3,000");
    }
}
