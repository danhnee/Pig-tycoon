using System;
using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD 3.8: sector plan per wave (minor 1 sector, major 1–3), re-seeded each cycle, only sectors with entries.</summary>
internal static class WaveApproachTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void RunSectorRules()
    {
        var all = WaveApproachPlanner.AllSectors;
        var majorCounts = new HashSet<int>();
        var firstSectors = new HashSet<ApproachSector>();
        for (int cycle = 1; cycle <= 40; cycle++)
        {
            var minor = WaveApproachPlanner.PlanSectors(20261101, cycle, 0, false, all);
            Check(minor.Count == 1, "a minor wave uses exactly one sector");
            var major = WaveApproachPlanner.PlanSectors(20261101, cycle, 9, true, all);
            Check(major.Count >= 1 && major.Count <= 3, "a major wave uses 1-3 sectors");
            Check(major.Distinct().Count() == major.Count, "sectors are not repeated");
            majorCounts.Add(major.Count);
            firstSectors.Add(minor[0]);
            Check(WaveApproachPlanner.PlanSectors(20261101, cycle, 9, true, all).SequenceEqual(major), "the plan is deterministic");
        }
        Check(majorCounts.Count == 3, "over many cycles majors use 1, 2 and 3 sectors");
        Check(firstSectors.Count == 4, "no fixed monster side: every sector comes up over many cycles");

        var onlyEastWest = new[] { ApproachSector.East, ApproachSector.West };
        for (int cycle = 1; cycle <= 20; cycle++)
        {
            var major = WaveApproachPlanner.PlanSectors(7, cycle, 0, true, onlyEastWest);
            Check(major.All(onlyEastWest.Contains) && major.Count <= 2, "only sectors that have entry points are used");
        }
        Check(WaveApproachPlanner.PlanSectors(7, 1, 0, true, Array.Empty<ApproachSector>()).Count == 0, "no entries, no sectors");
    }

    public static void RunRosterBudget()
    {
        var pool = new[] { EnemyTypeId.KeBatHeo, EnemyTypeId.KeCaoPha, EnemyTypeId.ChuotGamKho };
        int budget = EnemyData.CalculateWaveThreatBudget(1, 12);
        Check(WaveRosterPlanner.PointsFor(budget, true, 1) == budget, "a major wave spends the whole budget");
        for (int s = 0; s < 50; s++)
        {
            int minor = WaveRosterPlanner.PointsFor(budget, false, s);
            Check(minor >= (int)Math.Floor(budget * 0.15) && minor <= (int)Math.Ceiling(budget * 0.30), "minor wave = 15-30% of the budget");
            var roster = WaveRosterPlanner.Build(minor, pool, s);
            int spent = roster.Sum(t => EnemyData.CreateGdd19Canonical(t).ThreatPoints);
            Check(roster.Count >= 1, "at least one monster comes");
            Check(roster.Count == 1 || spent <= minor, "the roster never overspends");
        }
        var major = WaveRosterPlanner.Build(budget, pool, 3);
        Check(major.Sum(t => EnemyData.CreateGdd19Canonical(t).ThreatPoints) <= budget && major.Count > 3, "a major wave is several monsters within budget");
    }

    public static void RunRosterRulesGdd19()
    {
        Check(WaveRosterPlanner.BossFor(4) == null && WaveRosterPlanner.BossFor(6) == null, "no Boss outside every 5th cycle");
        Check(WaveRosterPlanner.BossFor(5)!.Value.boss == EnemyTypeId.ChuaTromRiuSat, "cycle 5: Chúa Trộm Rìu Sắt");
        Check(WaveRosterPlanner.BossFor(10)!.Value.boss == EnemyTypeId.UfoMothership, "cycle 10: Mẫu Hạm UFO");
        Check(WaveRosterPlanner.BossFor(15)!.Value.boss == EnemyTypeId.ThucTheSuong, "cycle 15: Thực Thể Sương");
        var again = WaveRosterPlanner.BossFor(20)!.Value;
        Check(again.boss == EnemyTypeId.ChuaTromRiuSat && again.appearance == 1, "cycle 20: Chúa Trộm again");
        Check(Math.Abs(WaveRosterPlanner.BossHpMultiplier(1) - 1.15f) < 0.001f, "+15% HP on the second visit");
        Check(!WaveRosterPlanner.PoolFor(3, true, WeatherType.QuangDang).Contains(EnemyTypeId.ThuBao), "Thú Bão only in a storm");
        Check(WaveRosterPlanner.PoolFor(3, true, WeatherType.GiongBao).Contains(EnemyTypeId.ThuBao), "Thú Bão comes with Giông Bão");
        Check(!WaveRosterPlanner.PoolFor(5, false, WeatherType.QuangDang).Any(WaveRosterPlanner.IsElite), "no elite in a minor wave before cycle 6");
        Check(!WaveRosterPlanner.PoolFor(9, true, WeatherType.QuangDang).Contains(EnemyTypeId.AlienGrunt), "only the GDD 19.1 monsters");
        for (int seed = 0; seed < 200; seed++)
        {
            var minor = WaveRosterPlanner.BuildWave(8, false, WeatherType.QuangDang, EnemyData.CalculateWaveThreatBudget(8, 40) * 3, seed);
            Check(minor.Count(WaveRosterPlanner.IsElite) <= 1, "a minor wave has at most one elite");
            Check(!minor.Contains(EnemyTypeId.ChuaTromRiuSat), "no Boss in a minor wave");
        }
        var bossWave = WaveRosterPlanner.BuildWave(5, true, WeatherType.QuangDang, EnemyData.CalculateWaveThreatBudget(5, 30), 1);
        Check(bossWave[0] == EnemyTypeId.ChuaTromRiuSat, "the cycle-5 major wave brings its Boss");
    }

    public static void RunNoTravelDuringWave()
    {
        var engine = new GameEngine(seed: 5);
        Check(PixelWorldRoutes.CanTravel("Farm", "MarketTown", engine), "the market road is open in peace");
        engine.Clock.IsDuringCombatWave = true;
        Check(!PixelWorldRoutes.CanTravel("Farm", "MarketTown", engine), "no leaving the farm during a wave");
        Check(!PixelWorldRoutes.CanTravel("Farm", "Hub_Village", engine), "nor to the village");
        engine.Clock.IsDuringCombatWave = false;
        Check(PixelWorldRoutes.CanTravel("Farm", "Hub_Village", engine), "open again after the wave");
    }

    public static void RunEngineEntries()
    {
        var engine = new GameEngine(seed: 20261101);
        var map = new PlayableMapConfiguration
        {
            ConfigurationId = "test_pixelmyth_farm",
            MapX = 0, MapY = 0, MapWidth = 184, MapHeight = 160,
            PastureX = 32, PastureY = 34, PastureWidth = 120, PastureHeight = 90,
            RefugeX = 92, RefugeY = 96,
        };
        foreach (var (s, x, y) in new (ApproachSector, float, float)[]
                 {
                     (ApproachSector.North, 46, 154), (ApproachSector.North, 132, 154),
                     (ApproachSector.East, 178, 112), (ApproachSector.East, 178, 76), (ApproachSector.East, 178, 36),
                     (ApproachSector.South, 146, 6), (ApproachSector.South, 96, 5), (ApproachSector.South, 46, 6),
                     (ApproachSector.West, 5, 44), (ApproachSector.West, 5, 84), (ApproachSector.West, 5, 120),
                 })
            map.MonsterEntries.Add(new FarmApproachPoint { Sector = s, X = x, Y = y });
        engine.ConfigurePlayableMap(map);
        Check(engine.GridZones.MonsterSpawnPoints.Count == 11, "the map's 11 entries replace the legacy defaults");
        var (sectors, entries) = engine.PlanWaveApproach(major: true, waveIndexInCycle: 0);
        Check(sectors.Count >= 1 && entries.Count >= 2, "a major wave has entries");
        Check(entries.All(e => sectors.Contains(e.sector)), "every entry belongs to a planned sector");
        Check(entries.All(e => e.x >= 0 && e.x <= 184 && e.y >= 0 && e.y <= 160), "entries lie on the map");
    }

    /// <summary>Every entry, from any sector, walks the outer lane to the one south breach (user 02/11).</summary>
    public static void RunConvergingRoute()
    {
        var entries = new (float x, float y)[] { (46, 154), (178, 112), (146, 6), (5, 84), (178, 36), (5, 120) };
        foreach (var (ex, ey) in entries)
        {
            var r = FarmApproachRoute.Build(ex, ey, 32, 34, 152, 124, 9, 92, 31);
            Check(r[r.Count - 1] == (92f, 31f), "every route ends at the breach");
            Check(r[r.Count - 2] == (92f, 25f), "every route reaches the breach along the lane");
            for (int i = 0; i < r.Count - 1; i++)
            {
                var (x, y) = r[i];
                bool onLane = Math.Abs(x - 23) < 0.01f || Math.Abs(x - 161) < 0.01f || Math.Abs(y - 25) < 0.01f || Math.Abs(y - 133) < 0.01f;
                Check(onLane, $"waypoint {i} ({x},{y}) of entry ({ex},{ey}) is on the lane");
            }
            Check(r.Count <= 5, "the shorter way round passes at most two corners");
        }
    }
}
