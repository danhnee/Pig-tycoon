using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// Night auction (GDD 20 table "Đấu giá (bậc III–IV, giấy Đỏ, bản vẽ Bạch Vân)"; opens with farm level 8–10, GDD
    /// level table; lots refresh at 18:00 with the night market). Sealed bid against one rival per lot: the player wins
    /// when the bid is at least the rival's, pays the bid, and learns the blueprint for good (GDD 13.2).
    /// PROVISIONAL (ADR-0016): GDD gives no reserve or rival rule. Reserve = the blueprint's build cost, rival bid
    /// 100–150% of reserve, seeded by day so a reload does not reroll it.
    /// </summary>
    [Serializable]
    public sealed class NightAuction
    {
        public const int UnlockFarmLevel = 8;
        public const int LotsPerNight = 3;

        public sealed class Lot
        {
            /// <summary>Blueprint lots unlock a structure; gear lots (<see cref="GearItemId"/> set) give a quality III–IV piece.</summary>
            public string BlueprintId = "";
            public string GearItemId = "";
            public bool IsGear => GearItemId.Length > 0;
            /// <summary>Item lots (C-08): papers, Đá Cường Hóa, Linh Kiện Tinh, Bí Kíp.</summary>
            public string ItemId = "";
            public int ItemCount;
            public bool IsItem => ItemId.Length > 0;
            public string Name = "";
            public int ReserveGold;
            public int RivalBidGold;
            public bool Sold;
            public bool WonByPlayer;
        }

        public int Day { get; private set; } = -1;
        public List<Lot> Lots { get; } = new List<Lot>();

        public static bool IsOpenHour(int hour) => hour >= 18 || hour < 6;

        /// <summary>The auction night a given hour belongs to (00:00–05:59 is still the previous evening's auction).</summary>
        public static int NightOf(int day, int hour) => hour < 6 ? day - 1 : day;

        public void EnsureLots(GameEngine engine)
        {
            int night = NightOf(engine.Clock.CurrentDay, engine.Clock.CurrentHour);
            if (night == Day) return;
            Day = night;
            Lots.Clear();
            var rng = new Random(unchecked(engine.BaseSeed * 31 + night * 977));
            // GDD 13.1: Cao cấp after the mid-game gate; Bạch Vân "rất hiếm" (C-09: one night in ten, TẠM)
            bool bachVanNight = rng.NextDouble() < 0.10;
            var pool = engine.Defense.GetAllBlueprints()
                .Where(b => !engine.Defense.UnlockedBlueprints.Contains(b.Id) && b.BuildCostGold > 0 &&
                            ((DefenseManager.TierOf(b.Id) == "CaoCap" && engine.DeepMechanics.MidGameGateUnlocked) ||
                             (DefenseManager.TierOf(b.Id) == "BachVan" && bachVanNight)))
                .OrderBy(b => b.Id, StringComparer.Ordinal)
                .ToList();
            for (int i = 0; i < LotsPerNight && pool.Count > 0; i++)
            {
                var bp = pool[rng.Next(pool.Count)];
                pool.Remove(bp);
                Lots.Add(new Lot
                {
                    BlueprintId = bp.Id,
                    Name = bp.Name,
                    ReserveGold = bp.BuildCostGold,
                    RivalBidGold = (int)Math.Round(bp.BuildCostGold * (1.0 + rng.NextDouble() * 0.5)),
                });
            }
            // GDD 20: the auction is the source of quality III–IV gear (GDD 12.3). One gear lot per night.
            // PROVISIONAL (ADR-0017 addendum): IV in 1 of 4 nights; reserve = quality-I price × the quality's stat bonus
            // (III ×5, IV ×8) — the GDD gives no prices for III–IV.
            var gearPool = EquipmentAndTacticalCatalog.AllEquipmentItems
                .Where(g => !engine.Character.OwnedEquipment.Any(o => EquipmentQuality.BaseId(o) == g.Id && EquipmentQuality.QualityOf(o) >= ItemTier.III))
                .OrderBy(g => g.Id, StringComparer.Ordinal).ToList();
            if (gearPool.Count > 0)
            {
                var g = gearPool[rng.Next(gearPool.Count)];
                var q = rng.NextDouble() < 0.25 ? ItemTier.IV : ItemTier.III;
                var piece = g.WithQuality(q);
                int reserve = MartialProgression.PriceGold(g) * EquipmentQuality.BonusFor(q);
                Lots.Add(new Lot
                {
                    GearItemId = piece.Id,
                    Name = piece.Name,
                    ReserveGold = reserve,
                    RivalBidGold = (int)Math.Round(reserve * (1.0 + rng.NextDouble() * 0.5)),
                });
            }
            // C-08: item lots. Papers follow GDD 10.3 (15/10/5/2 % a night, after the mid-game gate; Đỏ only here).
            if (engine.DeepMechanics.MidGameGateUnlocked)
                foreach (var (color, chance, reserve) in RareSources.AuctionPaperLots)
                    if (rng.NextDouble() < chance) AddItemLot(EnhancementTransactions.PaperId(color), 1, RareSources.PaperName(color), reserve, rng);
            if (rng.NextDouble() < RareSources.AuctionStoneLotChance)
            { int n = 1 + rng.Next(2); AddItemLot(EnhancementTransactions.StoneId, n, $"{n} Đá Cường Hóa", n * RareSources.StoneReserveGold, rng); }
            if (rng.NextDouble() < RareSources.AuctionFineComponentLotChance)
                AddItemLot(DefenseRepairs.FineComponentId, 1, "Linh Kiện Tinh", RareSources.FineComponentReserveGold, rng);
            if (rng.NextDouble() < RareSources.AuctionManualLotChance)
                AddItemLot(RareSources.SecretManualId, 1, "Bí Kíp", RareSources.ManualReserveGold, rng);
        }

        void AddItemLot(string itemId, int count, string name, int reserve, Random rng) =>
            Lots.Add(new Lot { ItemId = itemId, ItemCount = count, Name = name, ReserveGold = reserve,
                RivalBidGold = (int)Math.Round(reserve * (1.0 + rng.NextDouble() * 0.5)) });

        public (bool won, string message) Bid(GameEngine engine, int lotIndex, int bidGold)
        {
            if (engine.Farm.FarmLevel < UnlockFarmLevel) return (false, $"Đấu giá đêm mở từ cấp trại {UnlockFarmLevel}.");
            if (!IsOpenHour(engine.Clock.CurrentHour)) return (false, "Đấu giá chỉ mở 18:00–06:00.");
            EnsureLots(engine);
            if (lotIndex < 0 || lotIndex >= Lots.Count) return (false, "Không có lô này.");
            var lot = Lots[lotIndex];
            if (lot.Sold) return (false, "Lô này đã gõ búa.");
            if (bidGold < lot.ReserveGold) return (false, $"Giá khởi điểm {lot.ReserveGold}G.");
            if (engine.Farm.Gold < bidGold) return (false, "Không đủ vàng.");
            lot.Sold = true;
            if (bidGold < lot.RivalBidGold)
                return (false, lot.IsGear || lot.IsItem ? $"Gõ búa: người khác trả cao hơn. {lot.Name} đã có chủ." : $"Gõ búa: người khác trả cao hơn. Bản vẽ {lot.Name} đã có chủ.");
            if (!engine.Ledger.TrySpend(bidGold, LedgerReason.AuctionBid, "auction_" + (lot.IsItem ? lot.ItemId : lot.IsGear ? lot.GearItemId : lot.BlueprintId)))
            { lot.Sold = false; return (false, "Không đủ vàng."); }
            lot.WonByPlayer = true;
            if (lot.IsItem)
            {
                RareSources.Grant(engine, lot.ItemId, lot.ItemCount);
            }
            else if (lot.IsGear)
            {
                if (!engine.Character.OwnedEquipment.Contains(lot.GearItemId)) engine.Character.OwnedEquipment.Add(lot.GearItemId);
            }
            else if (!engine.Defense.UnlockedBlueprints.Contains(lot.BlueprintId)) engine.Defense.UnlockedBlueprints.Add(lot.BlueprintId);
            engine.SignalQuest(QuestSignal.AuctionWon);
            return (true, lot.IsGear || lot.IsItem ? $"Gõ búa! {lot.Name} về tay bạn ({bidGold}G)." : $"Gõ búa! Bản vẽ {lot.Name} thuộc về trại ({bidGold}G).");
        }
    }
}
