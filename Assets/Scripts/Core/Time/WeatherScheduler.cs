using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// GDD 7.2 "Mười loại thời tiết": weather lasts 1–3 days; the next spell is drawn from the table frequencies
    /// (Quang Đãng 25, Nhiều Mây 20, Mưa 15, Nắng Gắt 12, Sương Mù 8, Giông Bão 6, Rét Đậm 5, Nồm Ẩm 5, Gió Khô 3,
    /// Mưa Bức Xạ 1 — the last only from cycle 6). Deterministic in (base seed, day) so a reload rolls the same.
    /// </summary>
    public static class WeatherScheduler
    {
        public static readonly (WeatherType weather, int percent)[] Table =
        {
            (WeatherType.QuangDang, 25), (WeatherType.NhieuMay, 20), (WeatherType.NangGat, 12), (WeatherType.Mua, 15),
            (WeatherType.GiongBao, 6), (WeatherType.SuongMu, 8), (WeatherType.RetDam, 5), (WeatherType.NomAm, 5),
            (WeatherType.GioKho, 3), (WeatherType.MuaBucXa, 1),
        };

        public const int MuaBucXaFromCycle = 6;

        public static (WeatherType weather, int days) Roll(int baseSeed, int day, int cycleNumber)
        {
            var rng = new Random(unchecked(baseSeed * 397 + day * 7919 + 17));
            int total = 0;
            foreach (var (w, p) in Table) if (w != WeatherType.MuaBucXa || cycleNumber >= MuaBucXaFromCycle) total += p;
            int roll = rng.Next(total);
            var pick = WeatherType.QuangDang;
            foreach (var (w, p) in Table)
            {
                if (w == WeatherType.MuaBucXa && cycleNumber < MuaBucXaFromCycle) continue;
                if (roll < p) { pick = w; break; }
                roll -= p;
            }
            return (pick, 1 + rng.Next(3));
        }
    }
}
