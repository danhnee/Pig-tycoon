using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// Feeders and water basins on the playable farm hold real stock in Core (3 Nov user review: "how do scattered pigs
    /// get to the feeding spots… it can't do anything yet"). Until now the troughs were view-only counters and Core fed
    /// every pig automatically from the warehouse, so filling a trough changed nothing.
    ///
    /// With <see cref="PlayableMapConfiguration.InteractiveHerdOwnership"/> on, a pig gets its daily ration only by
    /// eating at a feeder that has feed (<see cref="PigEatsAtFeeder"/>); the end-of-day rollup then marks pigs that did
    /// not eat as unfed (existing rule in <see cref="PigCareCommands.FeedDailyRollup"/>). The ration size is the existing
    /// <see cref="PigCareCommands.DailyFeedKilograms"/>; no new balance numbers.
    /// Capacities (50 kg feeder, 100 L basin, 20 kg scoop) are the values the scene objects already used.
    /// </summary>
    public static class TroughCommands
    {
        public const float FeederCapacityKg = 50f;
        public const float BasinCapacityL = 100f;

        public const float FeedSackKg = 50f;
        /// <summary>TẠM (ADR-0028): the GDD gives no feed price; 2 G/kg before the market's feed-price multiplier.</summary>
        public const float FeedPricePerKgProvisional = 2f;
        public const int MarketOpenHour = 6, MarketCloseHour = 18;

        public static int FeedSackPrice(GameEngine e) =>
            (int)Math.Round(FeedSackKg * FeedPricePerKgProvisional * Math.Max(0.1f, e.Economy.DayMarket.FeedPriceMultiplier));

        /// <summary>Buy one sack of feed at the day market into the farm's warehouse (the silo).</summary>
        public static (bool ok, string message) BuyFeedSack(GameEngine e)
        {
            int h = e.Clock.CurrentHour;
            if (h < MarketOpenHour || h >= MarketCloseHour) return (false, "Chợ ngày bán cám từ 06:00 đến 18:00.");
            int price = FeedSackPrice(e);
            if (!e.Ledger.TrySpend(price, LedgerReason.BuyFeed, "feed_sack")) return (false, $"Không đủ vàng ({price}G).");
            e.Farm.FeedStockKg += FeedSackKg;
            return (true, $"Mua {FeedSackKg:0} kg cám (−{price}G). Kho cám: {e.Farm.FeedStockKg:0} kg.");
        }

        public static FarmServicePoint? Point(GameEngine e, string pointId) =>
            e.PlayableMap?.ServicePoints.Find(p => p.Id == pointId);

        /// <summary>Scoop feed out of the warehouse stock (silo) into the carried bag.</summary>
        public static float TakeFeedFromStock(GameEngine e, float kg)
        {
            if (kg <= 0f || float.IsNaN(kg)) return 0f;
            float taken = Math.Min(kg, Math.Max(0f, e.Farm.FeedStockKg));
            e.Farm.FeedStockKg -= taken;
            return taken;
        }

        /// <summary>Pour carried feed into a feeder. Returns the kilograms that fit; the rest stays in the bag.</summary>
        public static float FillFeeder(GameEngine e, string pointId, float kg)
        {
            var p = Point(e, pointId);
            if (p == null || !p.Feed || kg <= 0f || float.IsNaN(kg)) return 0f;
            float put = Math.Min(kg, FeederCapacityKg - p.FeedKg);
            if (put <= 0f) return 0f;
            p.FeedKg += put;
            return put;
        }

        /// <summary>Pour water into a basin. Returns the litres that fit.</summary>
        public static float FillBasin(GameEngine e, string pointId, float liters)
        {
            var p = Point(e, pointId);
            if (p == null || !p.Water || liters <= 0f || float.IsNaN(liters)) return 0f;
            float put = Math.Min(liters, BasinCapacityL - p.WaterL);
            if (put <= 0f) return 0f;
            p.WaterL += put;
            return put;
        }

        public static bool AteToday(GameEngine e, Pig pig) => pig.LastPaidRationDay >= e.Clock.CurrentDay;

        /// <summary>Should this pig go looking for a feeder? Alive, not isolated, not panicking, no ration yet today.</summary>
        public static bool WantsFood(GameEngine e, Pig pig) =>
            pig != null && pig.IsAlive && !pig.IsIsolated && pig.Mood >= 20 && !AteToday(e, pig);

        /// <summary>
        /// A pig eats its daily ration at a feeder. Feed comes out of the trough (not the warehouse). An empty or
        /// too-low trough refuses: the pig stays hungry and the trough shows it.
        /// </summary>
        public static (bool ate, float kg, string reason) PigEatsAtFeeder(GameEngine e, string pigId, string pointId)
        {
            var pig = e.Pigs.Find(x => x.Id == pigId && x.IsAlive);
            var p = Point(e, pointId);
            if (pig == null || p == null || !p.Feed) return (false, 0f, "no pig or feeder");
            if (pig.IsIsolated || pig.Mood < 20) return (false, 0f, "isolated or panicking");
            if (AteToday(e, pig)) return (false, 0f, "already fed today");
            float kg = PigCareCommands.DailyFeedKilograms(e, pig);
            if (p.FeedKg + 1e-4f < kg) return (false, 0f, "feeder empty");
            p.FeedKg = Math.Max(0f, p.FeedKg - kg);
            pig.LastPaidRationDay = e.Clock.CurrentDay;
            pig.Hunger = 0;
            pig.FedToday = true;
            e.Ecosystem.RecordTroughFeeding(pig);
            return (true, kg, "");
        }

        /// <summary>A pig drinks at a basin (the basin is the herd's water and cooling source; GDD 7.x heat stroke).</summary>
        public static bool PigDrinksAtBasin(GameEngine e, string pointId, float liters)
        {
            var p = Point(e, pointId);
            if (p == null || !p.Water || p.WaterL <= 0f) return false;
            p.WaterL = Math.Max(0f, p.WaterL - Math.Max(0f, liters));
            return true;
        }

        /// <summary>Register a feeder / basin the player builds, so pigs and the daily rollup see it.</summary>
        public static string RegisterPoint(GameEngine e, float x, float y, bool feed, bool water)
        {
            if (e.PlayableMap == null) return "";
            string prefix = feed ? "feeder_p" : "basin_p";
            int n = 0;
            while (e.PlayableMap.ServicePoints.Exists(p => p.Id == prefix + n)) n++;
            var point = new FarmServicePoint { Id = prefix + n, X = x, Y = y, Feed = feed, Water = water };
            e.PlayableMap.ServicePoints.Add(point);
            return point.Id;
        }

        public static void RemovePoint(GameEngine e, string pointId) => e.PlayableMap?.ServicePoints.RemoveAll(p => p.Id == pointId);

        /// <summary>The feeder or basin nearest to (x, y) within <paramref name="maxMeters"/>; null when none.</summary>
        public static FarmServicePoint? Nearest(GameEngine e, float x, float y, bool feed, float maxMeters)
        {
            FarmServicePoint? best = null;
            float bestD = maxMeters * maxMeters;
            if (e.PlayableMap == null) return null;
            foreach (var p in e.PlayableMap.ServicePoints)
            {
                if (feed ? !p.Feed : !p.Water) continue;
                float d = (p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y);
                if (d <= bestD) { bestD = d; best = p; }
            }
            return best;
        }
    }
}
