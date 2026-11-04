using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// Where the rare progression goods come from (3 Nov audit: Bí Kíp, Linh Kiện Tinh, Đá Cường Hóa, Bột Tử Tinh,
    /// Huyết Triện and Mực Ấn had no source in play, so Tuyệt kỹ, off-school skills, hall level 3, every Bạch Vân
    /// structure and paper enhancement were unreachable). Decisions: design clarifications §C-08.
    ///
    /// GDD sources honoured: Bí kíp at the night-market barter (ch. 20, ×2 in Mùa Trang Bị); Mực Ấn at the night market;
    /// Bột Tử Tinh from Bosses; Đá Cường Hóa at the auction and 1–2 from a Boss; Huyết Triện from cycle Bosses ≥ 15;
    /// auction paper lots 15/10/5/2 % a night after the mid-game gate (10.3). Quantities and rates the GDD leaves open
    /// are TẠM and named here.
    /// </summary>
    public static class RareSources
    {
        // ------------------------------------------------------------------ APIs for Argon's night-market UI (4 Nov)
        /// <summary>Mực Ấn the player holds (an inventory item, not a character stat).</summary>
        public static int SealInkCount(GameEngine e) => e.Inventory.CountId(InkId);

        /// <summary>Tonight's night-market listing that offers <paramref name="rewardId"/> (and is not sold out), or null.
        /// Its price is the listing's own wants / gold price: nothing is invented here.</summary>
        public static NightMarketListing? FindNightListing(GameEngine e, string rewardId) =>
            e.Economy.NightListings.Find(l => l != null && !l.IsSoldOut && l.OfferedItem != null
                && string.Equals(l.OfferedItem.Id, rewardId, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// "Đổi Bí Kíp" card: points the player to tonight's listing that sells it. The trade itself is the normal barter
        /// on that listing (the player picks the goods), so the result carries the slot to open, or why there is none.
        /// </summary>
        public static (bool ok, string message) ExchangeSecretManual(GameEngine e) => PointToListing(e, SecretManualId, "Bí Kíp");

        public static (bool ok, string message) ExchangeSealInk(GameEngine e) => PointToListing(e, InkId, "Mực Ấn");

        static (bool ok, string message) PointToListing(GameEngine e, string id, string name)
        {
            if (!NightMarketBarter.IsNight(e.Clock.CurrentHour)) return (false, "Chợ đêm chỉ mở từ 18:00 tới 06:00.");
            var l = FindNightListing(e, id);
            if (l == null) return (false, $"Đêm nay không ai bán {name}. Ghé lại đêm khác.");
            string price = l.Wants.Count > 0 ? string.Join(" + ", l.Wants.ConvertAll(w => w.Describe())) : l.RequestedItemDescription;
            if (l.AcceptsGold && l.GoldPrice > 0) price += $" (hoặc {l.GoldPrice}G)";
            return (true, $"{name}: người bán ô {l.SlotId} muốn đổi {price}. Chọn ô đó để đổi.");
        }

        public const string SecretManualId = "BiKip";
        public const string InkId = "muc_an";
        public const string PurpleDustId = "bot_tu_tinh";
        public const string BloodSealId = "huyet_trien";

        // ---- Boss rewards (C-08) ----------------------------------------------------------------
        public const int BossFineComponents = 1;      // TẠM: one Linh Kiện Tinh per cycle Boss (hall lv3 needs 2, Bạch Vân 2–3)
        public const int BossSecretManuals = 1;       // TẠM: a Boss is the surest way to a Bí Kíp
        public const int BossPurpleDust = 2;          // TẠM (GDD: "Bột Tử Tinh … Boss")
        public const int BossBloodSealFromCycle = 15; // GDD: "Huyết Triện (IV, Boss chu kỳ ≥ 15 …)"

        /// <summary>Goods a cycle Boss leaves (GDD 10.6 sources + C-08). Returns the player line ("" when nothing).</summary>
        public static string OnCycleBossDefeated(GameEngine e, int cycleNumber, Func<double> roll)
        {
            var got = new List<string>();
            e.Inventory.AddItem(DefenseRepairs.FineComponentId, BossFineComponents); got.Add($"{BossFineComponents} Linh Kiện Tinh");
            e.Character.SecretManuals += BossSecretManuals; got.Add($"{BossSecretManuals} Bí Kíp");
            int stones = roll() < 0.5 ? 1 : 2; // GDD: "Boss 1–2 viên"
            e.Inventory.AddItem(EnhancementTransactions.StoneId, stones); got.Add($"{stones} Đá Cường Hóa");
            e.Inventory.AddItem(PurpleDustId, BossPurpleDust); got.Add($"{BossPurpleDust} Bột Tử Tinh");
            if (cycleNumber >= BossBloodSealFromCycle) { e.Inventory.AddItem(BloodSealId, 1); got.Add("1 Huyết Triện"); }
            // GDD 13.1: Bạch Vân blueprints from "Boss chu kỳ ≥ 10" (C-09: 1 in 4, TẠM)
            if (cycleNumber >= 10 && roll() < 0.25)
            {
                var bv = e.Defense.GetAllBlueprints().Where(b => DefenseManager.TierOf(b.Id) == "BachVan" && !e.Defense.UnlockedBlueprints.Contains(b.Id))
                    .OrderBy(b => b.Id, StringComparer.Ordinal).FirstOrDefault();
                if (bv != null) { e.Defense.UnlockedBlueprints.Add(bv.Id); got.Add("bản vẽ " + bv.Name); }
            }
            return "Boss để lại: " + string.Join(", ", got) + ".";
        }

        // ---- Night auction item lots (C-08) ------------------------------------------------------
        /// <summary>GDD 10.3: "Đấu giá đêm (nguồn chính) 15 % / 10 % / 5 % / 2 %": the chance of a paper lot each night.</summary>
        public static readonly (SpecialtyPaperColor color, double chance, int reserve)[] AuctionPaperLots =
        {
            (SpecialtyPaperColor.Lam, 0.15, 600),   // reserves TẠM: about 3× the colour's first enhancement gold (10.5)
            (SpecialtyPaperColor.Vang, 0.10, 1800),
            (SpecialtyPaperColor.Tim, 0.05, 4500),
            (SpecialtyPaperColor.Do, 0.02, 12000),
        };
        public const double AuctionStoneLotChance = 0.30, AuctionFineComponentLotChance = 0.20, AuctionManualLotChance = 0.15; // TẠM
        public const int StoneReserveGold = 500, FineComponentReserveGold = 3000, ManualReserveGold = 2500;                     // TẠM

        public static string PaperName(SpecialtyPaperColor c) => c switch
        {
            SpecialtyPaperColor.Lam => "Giấy Chuyên Biệt Lam",
            SpecialtyPaperColor.Vang => "Giấy Chuyên Biệt Vàng",
            SpecialtyPaperColor.Tim => "Giấy Chuyên Biệt Tím",
            _ => "Giấy Chuyên Biệt Đỏ",
        };

        /// <summary>Gives a won auction item lot. Bí Kíp is a learning token, not a warehouse item.</summary>
        public static void Grant(GameEngine e, string itemId, int count)
        {
            if (itemId == SecretManualId) e.Character.SecretManuals += count;
            else e.Inventory.AddItem(itemId, count);
        }
    }
}
