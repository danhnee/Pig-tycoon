using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// Taking pigs to the day market and bringing bought pigs home (3 Nov user review: "mang heo đi bán… chưa làm được").
    /// Design: Docs/design/2026-11-03-design-clarifications.md §C-01, §C-02.
    ///
    /// GDD 20: the day market (06:00–18:00) buys and sells meat and breeding pigs. GDD 9.2 "Vận chuyển": carrying a
    /// piglet costs 3 Stamina/min and driving adult pigs 10/min, both at 70 % daily load. GDD 4.4: a pregnant sow
    /// should not be moved (stress +20).
    ///
    /// Flow on the interactive farm:
    ///   1. At the farm the player puts pigs in the drove (<see cref="Consign"/>); they gather at the farm gate.
    ///   2. Walking to the market town (<see cref="OnTravel"/>) takes the drove along: it costs the transport stamina
    ///      for <see cref="DroveTravelMinutes"/> and a pregnant sow gets the +20 stress (Tinh thần −20).
    ///   3. At the market only pigs in the drove can be sold; pigs bought there join the drove.
    ///   4. Walking back to the farm brings the drove home (bought and unsold pigs appear at the gate).
    /// A drove can only leave the farm with the player; pigs not in it stay on the farm.
    /// </summary>
    public sealed class MarketDrove
    {
        /// <summary>Game minutes of driving between the farm gate and the market (C-01, TẠM).</summary>
        public const int DroveTravelMinutes = 4;
        public const float DriveStaminaPerMinute = 10f, CarryPigletStaminaPerMinute = 3f, TransportLoadShare = 0.70f;
        public const int PregnantTransportStress = 20;
        /// <summary>How many pigs one person can drive at once (C-01, TẠM: one drove = one pen of the farm gate).</summary>
        public const int MaxDroveSize = 12;

        public List<string> PigIds { get; } = new List<string>();
        /// <summary>True while the drove is at the market with the player.</summary>
        public bool AtMarket { get; set; }

        public bool Contains(string pigId) => PigIds.Contains(pigId);

        public static string FarmId => "Farm";
        public static string MarketId => "MarketTown";

        public (bool ok, string message) Consign(GameEngine e, string pigId)
        {
            var pig = e.Pigs.Find(p => p.Id == pigId && p.IsAlive);
            if (pig == null) return (false, "Không thấy con heo này.");
            if (AtMarket) return (false, "Đoàn heo đang ở chợ.");
            if (PigIds.Contains(pigId)) return (false, $"{pig.Name} đã ở trong đoàn đi chợ.");
            if (pig.IsIsolated) return (false, $"{pig.Name} đang cách ly: không đưa ra chợ.");
            if (PigIds.Count >= MaxDroveSize) return (false, $"Một đoàn lùa được tối đa {MaxDroveSize} con.");
            PigIds.Add(pigId);
            string warn = pig.GestationDaysRemaining > 0 ? " Nái đang mang thai: đi đường sẽ bị căng thẳng (Tinh thần −20)." : "";
            return (true, $"{pig.Name} vào đoàn đi chợ ({PigIds.Count}/{MaxDroveSize}).{warn}");
        }

        public (bool ok, string message) Unconsign(GameEngine e, string pigId)
        {
            if (AtMarket) return (false, "Đoàn heo đang ở chợ.");
            if (!PigIds.Remove(pigId)) return (false, "Con heo này không ở trong đoàn.");
            var pig = e.Pigs.Find(p => p.Id == pigId);
            return (true, $"{pig?.Name ?? "Heo"} ra khỏi đoàn đi chợ.");
        }

        /// <summary>Stamina and load the drive costs for the current drove (GDD 9.2 transport rates × travel minutes).</summary>
        public (float stamina, int adults, int piglets) DriveCost(GameEngine e)
        {
            int adults = 0, piglets = 0;
            foreach (var id in PigIds)
            {
                var p = e.Pigs.Find(x => x.Id == id && x.IsAlive);
                if (p == null) continue;
                if (p.Stage == PigStage.HeoNon) piglets++; else adults++;
            }
            float cost = (adults > 0 ? DriveStaminaPerMinute * DroveTravelMinutes : 0f) + piglets * CarryPigletStaminaPerMinute * DroveTravelMinutes;
            return (cost, adults, piglets);
        }

        /// <summary>
        /// Called by the world graph when the player changes map. Returns a line for the player, or "" when the drove is
        /// not involved. Leaving the farm for the market with a drove costs the transport stamina; arriving home ends it.
        /// </summary>
        public string OnTravel(GameEngine e, string fromLocation, string toLocation)
        {
            PigIds.RemoveAll(id => !e.Pigs.Exists(p => p.Id == id && p.IsAlive));
            if (fromLocation == FarmId && toLocation == MarketId && PigIds.Count > 0 && !AtMarket)
            {
                var (cost, adults, piglets) = DriveCost(e);
                e.Character.Stamina.ForceConsumeStamina(cost, TransportLoadShare);
                foreach (var id in PigIds)
                {
                    var p = e.Pigs.Find(x => x.Id == id);
                    if (p != null && p.GestationDaysRemaining > 0) p.Mood = Math.Max(0, p.Mood - PregnantTransportStress);
                }
                AtMarket = true;
                return $"Lùa {adults} heo lớn{(piglets > 0 ? $", bế {piglets} heo con" : "")} ra chợ (−{cost:0} Thể lực).";
            }
            if (toLocation == FarmId && AtMarket)
            {
                int home = PigIds.Count;
                foreach (var id in PigIds)
                {
                    var p = e.Pigs.Find(x => x.Id == id);
                    if (p == null || e.PlayableMap == null) continue;
                    p.PositionX = e.PlayableMap.RefugeX; p.PositionY = e.PlayableMap.RefugeY; // they walk in by the shelter
                }
                PigIds.Clear();
                AtMarket = false;
                return home > 0 ? $"Đoàn heo về trại: {home} con." : "";
            }
            if (fromLocation == MarketId && toLocation != FarmId && AtMarket && PigIds.Count > 0)
                return "Đoàn heo đi theo bạn.";
            return "";
        }

        /// <summary>Is this pig away from the farm with the drove right now?</summary>
        public bool IsAway(string pigId) => AtMarket && PigIds.Contains(pigId);
    }

    /// <summary>One pig the day market sells today (C-02).</summary>
    public sealed class MarketPigOffer
    {
        public string OfferId = "";
        public GeneRarity Rarity;
        public GeneLineId Line;
        public PigStage Stage;
        public string Gender = "Cai";
        public int AgeDays;
        public float WeightKg;
        public int PriceGold;
        public bool Sold;
    }

    /// <summary>
    /// GDD 20: the day market sells meat and breeding pigs. C-02 (TẠM): 3–5 pigs a day, refreshed at 06:00; their line
    /// follows the natural rarity roll (GDD 5.1) and they arrive unidentified ("?"); the price is what the market would
    /// pay for that pig × <see cref="BuyMarkup"/> (the GDD's day-market band is 80–125 % of base).
    /// </summary>
    public static class MarketPigShop
    {
        public const float BuyMarkup = 1.15f;
        public const int MinOffers = 3, MaxOffers = 5;

        public static List<MarketPigOffer> TodayOffers(GameEngine e)
        {
            if (e.MarketPigOffersDay != e.Clock.CurrentDay)
            {
                e.MarketPigOffersDay = e.Clock.CurrentDay;
                e.MarketPigOffers.Clear();
                var rng = new Random(unchecked(e.BaseSeed * 7919 + e.Clock.CurrentDay * 104729));
                int n = MinOffers + rng.Next(MaxOffers - MinOffers + 1);
                for (int i = 0; i < n; i++)
                {
                    var rarity = GeneticsAndBreedingSystem.NaturalRarity(rng.NextDouble());
                    var lines = GeneticsAndBreedingSystem.LinesOfRarity(rarity);
                    var line = lines[rng.Next(lines.Length)];
                    bool piglet = rng.NextDouble() < 0.5;
                    int age = piglet ? 2 + rng.Next(3) : 6 + rng.Next(5);
                    var probe = Pig.CreateDefault("probe", "probe", line, rarity, rng.NextDouble() < 0.5 ? "Cai" : "Duc", age, piglet ? 8f : 45f);
                    int basePrice = Math.Max(1, e.Economy.CalculatePigSalePrice(probe, e.Farm.FarmLevel).priceGold);
                    e.MarketPigOffers.Add(new MarketPigOffer
                    {
                        OfferId = $"offer_{e.Clock.CurrentDay}_{i}", Rarity = rarity, Line = line, Stage = probe.Stage, Gender = probe.Gender,
                        AgeDays = age, WeightKg = probe.WeightKg, PriceGold = (int)Math.Round(basePrice * BuyMarkup),
                    });
                }
            }
            return e.MarketPigOffers;
        }

        public static (bool ok, string message) Buy(GameEngine e, string offerId)
        {
            int h = e.Clock.CurrentHour;
            if (h < TroughCommands.MarketOpenHour || h >= TroughCommands.MarketCloseHour) return (false, "Chợ ngày bán heo từ 06:00 đến 18:00.");
            if (e.InteractiveHerdOwned && e.WorldLocations.CurrentLocationId != MarketDrove.MarketId) return (false, "Mua heo ở chợ thị trấn.");
            var offer = TodayOffers(e).Find(o => o.OfferId == offerId);
            if (offer == null || offer.Sold) return (false, "Con heo này đã bán.");
            if (e.Drove.PigIds.Count >= MarketDrove.MaxDroveSize) return (false, $"Đoàn đã đủ {MarketDrove.MaxDroveSize} con: về trại trước đã.");
            if (!e.Ledger.TrySpend(offer.PriceGold, LedgerReason.BuyPiglet, offer.OfferId)) return (false, $"Không đủ vàng ({offer.PriceGold}G).");
            offer.Sold = true;
            string id = "pig_bought_" + offer.OfferId;
            var pig = Pig.CreateDefault(id, "Heo mới " + (e.Pigs.Count + 1), offer.Line, offer.Rarity, offer.Gender, offer.AgeDays, offer.WeightKg);
            pig.IsIdentified = PigText.LineVisible(offer.Rarity, false); // GDD 5.1: Thường/Khá known, rarer "?" until appraised
            if (e.PlayableMap != null) { pig.PositionX = e.PlayableMap.RefugeX; pig.PositionY = e.PlayableMap.RefugeY; }
            e.PrepareNewPig(pig);
            e.Pigs.Add(pig);
            e.Drove.PigIds.Add(id);
            e.Drove.AtMarket = true; // it is here, at the market, with the player
            return (true, $"Mua {pig.Name} (−{offer.PriceGold}G). Heo theo đoàn về trại.");
        }
    }
}
