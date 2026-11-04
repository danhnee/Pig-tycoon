using System;

namespace PigTycoon.Core
{
    /// <summary>Saved state of the cycle theme, post-raid scarcity and bought intel (null in old saves).</summary>
    [Serializable]
    public class EconomyIntelSnapshot
    {
        public MarketTheme Theme = MarketTheme.CanBang;
        public int ThemeCycleNumber = 1;
        public bool ThemeRolled;
        public float ThemePriceMultiplier = 1f;
        public float ThemeBuyingPowerMultiplier = 1f;
        public float FeedPriceMultiplier = 1f;
        public int PurchasingPowerBase = 40;
        public bool NextThemePreRolled;
        public MarketTheme NextCyclePreviewTheme = MarketTheme.CanBang;
        public int PostRaidScarcityDaysRemaining;
        public float PostRaidScarcityBonus;
        public int IntelLevel;
        public int IntelCycleNumber = 1;
    }

    /// <summary>GDD 20.2 / 23 wiring of the cycle boundary into the engine (cycle theme seeding, intel expiry).</summary>
    public partial class GameEngine
    {
        /// <summary>GDD 20.3 game stage for buying power: 0 early, 1 after the mid-game gate, 2 late (cycle 20+).</summary>
        public int MarketGameStage => Clock.CycleNumber >= 20 ? 2 : DeepMechanics.MidGameGateUnlocked ? 1 : 0;

        /// <summary>
        /// Called on every clock block and after each wave: when the cycle number moved on, the new cycle gets its
        /// theme (GDD 20.2) and bought intel expires (GDD 23). Cycle 1 keeps the neutral Cân Bằng (ADR-0023).
        /// </summary>
        public void SyncEconomyCycle()
        {
            if (Economy.DayMarket.ThemeCycleNumber != Clock.CycleNumber)
            {
                var theme = Economy.SeedCycleTheme(Clock.CycleNumber, MarketGameStage);
                OnLogEmitted?.Invoke($"Chu kỳ {Clock.CycleNumber}: chủ đề chợ \"{MarketCycleThemes.NameVi(theme)}\".");
            }
            Intel.SyncCycle(Clock.CycleNumber);
        }

        /// <summary>GDD 20.2 world reaction after a raid (any wave): pigs scarce +10–35% for 1–3 days.</summary>
        private void ApplyPostRaidMarketReaction()
        {
            SyncEconomyCycle();
            Economy.ApplyPostRaidScarcity(duringDayMarket: Clock.CurrentHour >= 6 && Clock.CurrentHour < 18);
        }

        internal EconomyIntelSnapshot CaptureEconomyIntel()
        {
            var dm = Economy.DayMarket;
            return new EconomyIntelSnapshot
            {
                Theme = dm.Theme, ThemeCycleNumber = dm.ThemeCycleNumber, ThemeRolled = dm.ThemeRolled,
                ThemePriceMultiplier = dm.ThemePriceMultiplier, ThemeBuyingPowerMultiplier = dm.ThemeBuyingPowerMultiplier,
                FeedPriceMultiplier = dm.FeedPriceMultiplier, PurchasingPowerBase = dm.PurchasingPowerBase,
                NextThemePreRolled = dm.NextThemePreRolled, NextCyclePreviewTheme = dm.NextCyclePreviewTheme,
                PostRaidScarcityDaysRemaining = dm.PostRaidScarcityDaysRemaining, PostRaidScarcityBonus = dm.PostRaidScarcityBonus,
                IntelLevel = Intel.Level, IntelCycleNumber = Intel.CycleNumber,
            };
        }

        internal void RestoreEconomyIntel(EconomyIntelSnapshot? s)
        {
            var dm = Economy.DayMarket;
            if (s == null)
            {
                // old save: keep the neutral theme for the cycle it was saved in, no bought intel
                dm.ThemeCycleNumber = Clock.CycleNumber;
                Intel.Restore(0, Clock.CycleNumber);
                return;
            }
            Economy.ApplyMarketCycleTheme(s.Theme);
            dm.ThemeCycleNumber = s.ThemeCycleNumber;
            dm.ThemeRolled = s.ThemeRolled;
            dm.ThemePriceMultiplier = s.ThemePriceMultiplier;
            dm.ThemeBuyingPowerMultiplier = s.ThemeBuyingPowerMultiplier;
            dm.FeedPriceMultiplier = s.FeedPriceMultiplier;
            dm.PurchasingPowerBase = s.PurchasingPowerBase;
            dm.DailyPurchasingPowerCap = Math.Max(1, (int)Math.Round(dm.PurchasingPowerBase * dm.ThemeBuyingPowerMultiplier));
            dm.NextThemePreRolled = s.NextThemePreRolled;
            dm.NextCyclePreviewTheme = s.NextCyclePreviewTheme;
            dm.PostRaidScarcityDaysRemaining = Math.Max(0, s.PostRaidScarcityDaysRemaining);
            dm.PostRaidScarcityBonus = s.PostRaidScarcityBonus;
            Intel.Restore(s.IntelLevel, s.IntelCycleNumber);
        }
    }
}
