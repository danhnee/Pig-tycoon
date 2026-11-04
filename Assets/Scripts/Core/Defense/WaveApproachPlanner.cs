using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// GDD 3.8 "Hướng tiếp cận": the outer ring has 8–12 entry points in four sectors (N/E/S/W) whose weights are
    /// re-seeded every cycle; a major wave uses 1–3 sectors, a minor wave uses 1. There is no fixed "monster side".
    /// The plan is deterministic in (base seed, cycle, wave index) so intel shown before a wave (Radar, scouting,
    /// Mộng Nhãn) and the wave that actually arrives always agree.
    /// </summary>
    public static class WaveApproachPlanner
    {
        public static readonly ApproachSector[] AllSectors =
            { ApproachSector.North, ApproachSector.East, ApproachSector.South, ApproachSector.West };

        /// <summary>Per-cycle sector weights in [0.5, 1.5]; the same for every wave of one cycle.</summary>
        public static Dictionary<ApproachSector, double> CycleWeights(int baseSeed, int cycleNumber)
        {
            var rng = new Random(unchecked(baseSeed * 31 + cycleNumber * 7919));
            return AllSectors.ToDictionary(s => s, _ => 0.5 + rng.NextDouble());
        }

        /// <summary>
        /// Sectors a wave comes from. Only sectors that actually have entry points are eligible. Minor: exactly 1.
        /// Major: 1–3 (capped by the eligible count), picked by weight without replacement.
        /// </summary>
        public static List<ApproachSector> PlanSectors(int baseSeed, int cycleNumber, int waveIndexInCycle, bool major,
            IEnumerable<ApproachSector> sectorsWithEntries)
        {
            var eligible = AllSectors.Where(s => sectorsWithEntries.Contains(s)).ToList();
            var result = new List<ApproachSector>();
            if (eligible.Count == 0) return result;
            var weights = CycleWeights(baseSeed, cycleNumber);
            var rng = new Random(unchecked(baseSeed * 131 + cycleNumber * 7919 + waveIndexInCycle * 104729 + (major ? 1 : 0)));
            int count = major ? Math.Min(eligible.Count, 1 + rng.Next(3)) : 1;
            while (result.Count < count)
            {
                var pool = eligible.Where(s => !result.Contains(s)).ToList();
                double total = pool.Sum(s => weights[s]);
                double roll = rng.NextDouble() * total;
                foreach (var s in pool)
                {
                    roll -= weights[s];
                    if (roll <= 0) { result.Add(s); break; }
                }
                if (result.Count < count && roll > 0) result.Add(pool[pool.Count - 1]);   // float rounding guard
            }
            return result;
        }

        public static string SectorName(ApproachSector s) => s switch
        {
            ApproachSector.North => "Bắc",
            ApproachSector.East => "Đông",
            ApproachSector.South => "Nam",
            _ => "Tây",
        };

        public static string Describe(IEnumerable<ApproachSector> sectors) => string.Join(" & ", sectors.Select(SectorName));
    }

    /// <summary>
    /// GDD 2.3 + 18.2: a major wave spends 100% of the threat budget (EnemyData.CalculateWaveThreatBudget), a
    /// minor wave 15–30% of it, buying monsters by their GDD 19 threat points from the given pool. At least one
    /// monster always comes.
    /// </summary>
    public static class WaveRosterPlanner
    {
        public static int PointsFor(int budget, bool major, int seed)
        {
            if (major) return budget;
            double share = 0.15 + new Random(seed).NextDouble() * 0.15;
            return (int)Math.Round(budget * share);
        }

        /// <summary>GDD 19.1: the fourteen monsters. Elites: Đầu Lĩnh Trộm, Kẻ Mang Dịch.</summary>
        public static readonly EnemyTypeId[] Gdd19Monsters =
        {
            EnemyTypeId.ChuotGamKho, EnemyTypeId.KeCatDay, EnemyTypeId.KeBatHeo, EnemyTypeId.KeCaoPha, EnemyTypeId.BayNhay,
            EnemyTypeId.ThietGiapCongThanh, EnemyTypeId.UfoTrinhSat, EnemyTypeId.UfoThuHoach, EnemyTypeId.UfoPhaoHam,
            EnemyTypeId.BongHuyen, EnemyTypeId.ChoSuong, EnemyTypeId.ThuBao, EnemyTypeId.DauLinhTrom, EnemyTypeId.KeMangDich,
        };

        public static bool IsElite(EnemyTypeId t) => t == EnemyTypeId.DauLinhTrom || t == EnemyTypeId.KeMangDich;

        /// <summary>
        /// Monsters a wave may buy. GDD 19.1: Thú Bão only during Giông Bão. GDD 2.3: a minor wave has no Boss and at most
        /// one elite, only from cycle 6 (the cap is applied in BuildWave).
        /// </summary>
        public static List<EnemyTypeId> PoolFor(int cycle, bool major, WeatherType weather)
        {
            var pool = new List<EnemyTypeId>();
            foreach (var t in Gdd19Monsters)
            {
                if (t == EnemyTypeId.ThuBao && weather != WeatherType.GiongBao) continue;
                if (!major && IsElite(t) && cycle < 6) continue;
                pool.Add(t);
            }
            return pool;
        }

        /// <summary>GDD 19.2: a Boss joins the major wave every 5 cycles: 5/20/35 Chúa Trộm Rìu Sắt, 10/25/40 Mẫu Hạm
        /// UFO, 15/30/45 Thực Thể Sương; each return has +15% HP. Null when this cycle has no Boss.</summary>
        public static (EnemyTypeId boss, int appearance)? BossFor(int cycle)
        {
            if (cycle < 5 || cycle % 5 != 0) return null;
            int slot = (cycle / 5 - 1) % 3;
            int appearance = (cycle / 5 - 1) / 3;   // 0 = first time
            var boss = slot == 0 ? EnemyTypeId.ChuaTromRiuSat : slot == 1 ? EnemyTypeId.UfoMothership : EnemyTypeId.ThucTheSuong;
            return (boss, appearance);
        }

        public static float BossHpMultiplier(int appearance) => (float)Math.Pow(1.15, appearance);

        /// <summary>A whole wave: the Boss (if any, major only, paid from the budget first) + monsters bought with the
        /// rest; a minor wave keeps at most one elite.</summary>
        public static List<EnemyTypeId> BuildWave(int cycle, bool major, WeatherType weather, int budget, int seed)
        {
            int points = PointsFor(budget, major, seed);
            var roster = new List<EnemyTypeId>();
            var boss = major ? BossFor(cycle) : null;
            if (boss.HasValue)
            {
                roster.Add(boss.Value.boss);
                points = Math.Max(0, points - EnemyData.CreateGdd19Canonical(boss.Value.boss).ThreatPoints);
            }
            var bought = Build(points, PoolFor(cycle, major, weather), seed);
            if (!major)
            {
                bool hadElite = false;
                bought = bought.Where(t => !IsElite(t) || !hadElite && (hadElite = true)).ToList();
            }
            if (boss.HasValue && bought.Count == 0) return roster;
            roster.AddRange(bought);
            return roster;
        }

        public static List<EnemyTypeId> Build(int points, IReadOnlyList<EnemyTypeId> pool, int seed)
        {
            var roster = new List<EnemyTypeId>();
            if (pool == null || pool.Count == 0) return roster;
            var cost = pool.ToDictionary(t => t, t => Math.Max(1, EnemyData.CreateGdd19Canonical(t).ThreatPoints));
            var rng = new Random(seed);
            int left = points;
            int guard = 0;
            while (guard++ < 500)
            {
                var affordable = pool.Where(t => cost[t] <= left).ToList();
                if (affordable.Count == 0) break;
                var pick = affordable[rng.Next(affordable.Count)];
                roster.Add(pick);
                left -= cost[pick];
            }
            if (roster.Count == 0) roster.Add(pool.OrderBy(t => cost[t]).First());
            return roster;
        }
    }

    /// <summary>
    /// Where monsters walk after they appear. Entries stay spread over the four sectors (GDD 3.8: the sector is intel),
    /// but every ground monster first follows the outer lane round the fence to ONE breach point and attacks the farm
    /// from there, so the player has one front to fortify (user, 02/11). Flying and occult monsters fly straight in.
    /// The lane is the fence rectangle grown by <c>laneOffset</c> metres; the route takes the shorter way round.
    /// </summary>
    public static class FarmApproachRoute
    {
        public static List<(float x, float y)> Build(float entryX, float entryY,
            float fx0, float fy0, float fx1, float fy1, float laneOffset, float breachX, float breachY)
        {
            float x0 = fx0 - laneOffset, y0 = fy0 - laneOffset, x1 = fx1 + laneOffset, y1 = fy1 + laneOffset;
            float w = x1 - x0, h = y1 - y0, per = 2f * (w + h);
            // perimeter parameter, counter-clockwise from the south-west corner
            float Param(float x, float y)
            {
                x = Math.Clamp(x, x0, x1); y = Math.Clamp(y, y0, y1);
                float dS = y - y0, dE = x1 - x, dN = y1 - y, dW = x - x0;
                float m = Math.Min(Math.Min(dS, dE), Math.Min(dN, dW));
                if (m == dS) return x - x0;
                if (m == dE) return w + (y - y0);
                if (m == dN) return w + h + (x1 - x);
                return 2f * w + h + (y1 - y);
            }
            (float, float) Point(float t)
            {
                t = ((t % per) + per) % per;
                if (t <= w) return (x0 + t, y0);
                if (t <= w + h) return (x1, y0 + (t - w));
                if (t <= 2f * w + h) return (x1 - (t - w - h), y1);
                return (x0, y1 - (t - 2f * w - h));
            }
            float ta = Param(entryX, entryY), tb = Param(breachX, breachY);
            float fwd = ((tb - ta) % per + per) % per;
            int dir = fwd <= per - fwd ? 1 : -1;
            float len = dir > 0 ? fwd : per - fwd;
            var corners = new[] { 0f, w, w + h, 2f * w + h };
            var route = new List<(float x, float y)> { Point(ta) };
            var passed = new List<float>();
            foreach (var c in corners)
            {
                float along = dir > 0 ? ((c - ta) % per + per) % per : ((ta - c) % per + per) % per;
                if (along > 0.01f && along < len - 0.01f) passed.Add(along);
            }
            passed.Sort();
            foreach (var a in passed) route.Add(Point(ta + dir * a));
            route.Add(Point(tb));
            route.Add((breachX, breachY));
            return route;
        }
    }
}
