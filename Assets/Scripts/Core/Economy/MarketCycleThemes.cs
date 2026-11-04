using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// GDD 20.2 — one row of the eight cycle themes. Weight, pig price, buying power and the night-market note are the
    /// GDD table; FeedMin/FeedMax only for Khan Hiếm Thức Ăn ("giá cám +35–70%"); MysticFactor is GDD 21 for
    /// Hoảng Loạn Dịch ×2,0 / Mùa Heo Rẻ ×1,5 / Cân Bằng ×1,0 and 1,0 for the others (ADR-0023, provisional).
    /// </summary>
    public sealed class MarketThemeProfile
    {
        public MarketTheme Theme;
        public string NameVi = "";
        public int WeightPercent;
        public float PigPriceMin, PigPriceMax;
        public float BuyingPowerMin, BuyingPowerMax;
        public float FeedMin = 1f, FeedMax = 1f;
        public string NightMarketNoteVi = "";
        public string PressureNoteVi = "";
        public double MysticFactor = 1.0;
    }

    public static class MarketCycleThemes
    {
        public static readonly IReadOnlyList<MarketThemeProfile> All = new List<MarketThemeProfile>
        {
            new MarketThemeProfile { Theme = MarketTheme.CanBang, NameVi = "Cân Bằng", WeightPercent = 24,
                PigPriceMin = 0.95f, PigPriceMax = 1.05f, BuyingPowerMin = 0.90f, BuyingPowerMax = 1.10f,
                NightMarketNoteVi = "Trung tính", PressureNoteVi = "Chuẩn", MysticFactor = 1.0 },
            new MarketThemeProfile { Theme = MarketTheme.MuaHeoRe, NameVi = "Mùa Heo Rẻ", WeightPercent = 12,
                PigPriceMin = 0.65f, PigPriceMax = 0.85f, BuyingPowerMin = 0.50f, BuyingPowerMax = 0.70f,
                NightMarketNoteVi = "Nhiều ô đòi heo; heo định giá ×1,0", PressureNoteVi = "Dễ kẹt heo → già", MysticFactor = 1.5 },
            new MarketThemeProfile { Theme = MarketTheme.ConSotThit, NameVi = "Cơn Sốt Thịt", WeightPercent = 12,
                PigPriceMin = 1.15f, PigPriceMax = 1.45f, BuyingPowerMin = 1.20f, BuyingPowerMax = 1.60f,
                NightMarketNoteVi = "Đấu giá sôi động", PressureNoteVi = "Cám dỗ bán sạch" },
            new MarketThemeProfile { Theme = MarketTheme.KhanHiemThucAn, NameVi = "Khan Hiếm Thức Ăn", WeightPercent = 10,
                PigPriceMin = 0.95f, PigPriceMax = 1.10f, BuyingPowerMin = 0.90f, BuyingPowerMax = 1.00f,
                FeedMin = 1.35f, FeedMax = 1.70f,
                NightMarketNoteVi = "Ô đòi cám", PressureNoteVi = "Giá cám +35–70%" },
            new MarketThemeProfile { Theme = MarketTheme.HoangLoanDich, NameVi = "Hoảng Loạn Dịch", WeightPercent = 8,
                PigPriceMin = 0.60f, PigPriceMax = 0.90f, BuyingPowerMin = 0.40f, BuyingPowerMax = 0.70f,
                NightMarketNoteVi = "Thuốc, vắc-xin nhiều ô", PressureNoteVi = "Nguy cơ Dịch Tả Heo", MysticFactor = 2.0 },
            new MarketThemeProfile { Theme = MarketTheme.KhanHiemHauDotKich, NameVi = "Khan Hiếm Hậu Đột Kích", WeightPercent = 10,
                PigPriceMin = 1.10f, PigPriceMax = 1.35f, BuyingPowerMin = 1.00f, BuyingPowerMax = 1.40f,
                NightMarketNoteVi = "Vật liệu sửa, đạn nhiều hơn", PressureNoteVi = "Giữ được đàn thì lãi" },
            new MarketThemeProfile { Theme = MarketTheme.MuaTrangBi, NameVi = "Mùa Trang Bị", WeightPercent = 12,
                PigPriceMin = 0.95f, PigPriceMax = 1.05f, BuyingPowerMin = 0.90f, BuyingPowerMax = 1.10f,
                NightMarketNoteVi = "Trang bị, bí kíp, Mực Phù ×2", PressureNoteVi = "Cơ hội combat" },
            new MarketThemeProfile { Theme = MarketTheme.MuaCongTrinh, NameVi = "Mùa Công Trình", WeightPercent = 12,
                PigPriceMin = 0.95f, PigPriceMax = 1.05f, BuyingPowerMin = 0.90f, BuyingPowerMax = 1.10f,
                NightMarketNoteVi = "Bản vẽ, đạn ×2; giá vàng −15%", PressureNoteVi = "Cơ hội phòng thủ" },
        };

        /// <summary>GDD 20.3 buying power (adult pigs/day) by game stage: early 20–60, mid 50–120, late 100–220.</summary>
        public static (int min, int max) PurchasingPowerRange(int gameStage) =>
            gameStage >= 2 ? (100, 220) : gameStage == 1 ? (50, 120) : (20, 60);

        /// <summary>GDD 20.2: "Mùa Công Trình — giá vàng −15%" on the night market's gold slots.</summary>
        public const float MuaCongTrinhNightGoldMultiplier = 0.85f;

        /// <summary>GDD 20.2 world reaction: after a raid pigs are scarce, +10–35% for 1–3 days.</summary>
        public const float PostRaidBonusMin = 0.10f, PostRaidBonusMax = 0.35f;
        public const int PostRaidDaysMin = 1, PostRaidDaysMax = 3;

        public static MarketThemeProfile Get(MarketTheme theme)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Theme == theme) return All[i];
            return All[0];
        }

        public static string NameVi(MarketTheme theme) => Get(theme).NameVi;

        /// <summary>One theme by the GDD weights (24/12/12/10/8/10/12/12, sum 100).</summary>
        public static MarketTheme RollWeighted(Random rng)
        {
            int total = 0;
            for (int i = 0; i < All.Count; i++) total += All[i].WeightPercent;
            double r = rng.NextDouble() * total;
            for (int i = 0; i < All.Count; i++)
            {
                r -= All[i].WeightPercent;
                if (r < 0) return All[i].Theme;
            }
            return All[All.Count - 1].Theme;
        }

        internal static float Uniform(Random rng, float min, float max) =>
            max <= min ? min : (float)(min + rng.NextDouble() * (max - min));
    }
}
