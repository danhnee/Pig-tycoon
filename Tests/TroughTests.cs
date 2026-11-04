using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>Feeders and basins hold real stock; pigs on an interactive farm eat their ration only at a filled feeder.</summary>
internal static class TroughTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static GameEngine FarmWithFeeder(out Pig pig)
    {
        var e = new GameEngine(7);
        var map = new PlayableMapConfiguration { ConfigurationId = "trough-test", MapX = -60f, MapY = -45f, MapWidth = 120f, MapHeight = 90f,
            PastureX = -20f, PastureY = -14f, PastureWidth = 40f, PastureHeight = 28f, InteractiveHerdOwnership = true };
        map.ServicePoints.Add(new FarmServicePoint { Id = "feeder_0", X = 0f, Y = 0f, Feed = true });
        map.ServicePoints.Add(new FarmServicePoint { Id = "basin_1", X = 4f, Y = 0f, Water = true });
        e.ConfigurePlayableMap(map);
        pig = Pig.CreateDefault("trough_pig", "Heo Máng", GeneLineId.HongDien, GeneRarity.Thuong, "Cai", 16, 95f);
        pig.PositionX = 1f; pig.PositionY = 1f;
        e.Pigs.Add(pig);
        pig.Mood = 80;
        return e;
    }

    public static void RunTroughsDriveFeeding()
    {
        var e = FarmWithFeeder(out var pig);
        Check(TroughCommands.WantsFood(e, pig), "an unfed pig wants food");
        Check(!TroughCommands.PigEatsAtFeeder(e, pig.Id, "feeder_0").ate, "an empty feeder feeds nobody");

        float stock = e.Farm.FeedStockKg;
        float bag = TroughCommands.TakeFeedFromStock(e, 20f);
        Check(Math.Abs(bag - Math.Min(20f, stock)) < 1e-3f && Math.Abs(e.Farm.FeedStockKg - (stock - bag)) < 1e-3f, "scooping debits the warehouse");
        float put = TroughCommands.FillFeeder(e, "feeder_0", bag);
        Check(Math.Abs(put - bag) < 1e-3f && Math.Abs(TroughCommands.Point(e, "feeder_0")!.FeedKg - bag) < 1e-3f, "pouring fills the feeder");
        Check(TroughCommands.FillFeeder(e, "feeder_0", 999f) <= TroughCommands.FeederCapacityKg - bag + 1e-3f, "a feeder holds at most 50 kg");

        float before = TroughCommands.Point(e, "feeder_0")!.FeedKg;
        var r = TroughCommands.PigEatsAtFeeder(e, pig.Id, "feeder_0");
        Check(r.ate && pig.FedToday && pig.Hunger == 0, "a pig eats its ration at a filled feeder");
        Check(Math.Abs(TroughCommands.Point(e, "feeder_0")!.FeedKg - (before - r.kg)) < 1e-3f, "the ration comes out of the feeder, not the warehouse");
        Check(!TroughCommands.PigEatsAtFeeder(e, pig.Id, "feeder_0").ate && !TroughCommands.WantsFood(e, pig), "one ration per day");

        Check(TroughCommands.FillBasin(e, "basin_1", 50f) == 50f && TroughCommands.PigDrinksAtBasin(e, "basin_1", 2f), "basins hold water pigs drink");
        Check(TroughCommands.FillFeeder(e, "basin_1", 5f) == 0f && TroughCommands.FillBasin(e, "feeder_0", 5f) == 0f, "feed and water go to the right trough");

        string id = TroughCommands.RegisterPoint(e, 9f, 9f, feed: true, water: false);
        Check(id.Length > 0 && TroughCommands.Nearest(e, 9.5f, 9f, true, 2f)?.Id == id, "a player-built feeder becomes a Core service point");

        var save = e.CreateSaveData(); var back = new GameEngine(); back.RestoreFromSave(save);
        Check(Math.Abs(TroughCommands.Point(back, "feeder_0")!.FeedKg - TroughCommands.Point(e, "feeder_0")!.FeedKg) < 1e-3f, "trough stock survives save/load");
    }

    public static void RunBuyFeedSack()
    {
        var e = FarmWithFeeder(out _);
        e.Tick(4 * 60); // 10:00 on day 0 (the clock starts at 06:00)
        long gold = e.Farm.Gold; float stock = e.Farm.FeedStockKg;
        var r = TroughCommands.BuyFeedSack(e);
        Check(r.ok && Math.Abs(e.Farm.FeedStockKg - stock - TroughCommands.FeedSackKg) < 1e-3f && e.Farm.Gold == gold - TroughCommands.FeedSackPrice(e), "a feed sack costs gold and fills the warehouse");
        e.Tick(10 * 60); // 20:00
        Check(!TroughCommands.BuyFeedSack(e).ok, "the day market is closed at night");
    }

    public static void RunUnfedPigsGoHungryOnInteractiveFarm()
    {
        var e = FarmWithFeeder(out var pig);
        float stock = e.Farm.FeedStockKg;
        e.Tick(24 * 60); // a whole day with empty feeders
        Check(!pig.FedToday && pig.Hunger > 0, "with interactive ownership, a pig that never reached a filled feeder is unfed");
        Check(Math.Abs(e.Farm.FeedStockKg - stock) < 1e-3f, "the warehouse no longer feeds the herd by itself");
    }
}
