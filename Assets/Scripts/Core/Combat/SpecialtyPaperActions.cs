using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// Player-facing paper commands (GDD 10.4–10.5): a paper must be owned to be chosen, used, enhanced or salvaged,
    /// and using or salvaging it consumes one from the inventory. The UI calls only these.
    /// </summary>
    public static class SpecialtyPaperActions
    {
        /// <summary>GDD 10.3 bảng nguồn giấy: một lần tung cho mỗi nguồn (Lam / Vàng / Tím / Đỏ).</summary>
        public static readonly double[] MajorWaveRates = { 0.08, 0.03, 0.008, 0.002 };
        public static readonly double[] CycleBossRates = { 0.30, 0.15, 0.05, 0.015 };

        /// <summary>One toss against a source's colour rates (rarest first); adds the paper to the inventory.</summary>
        public static SpecialtyPaperColor? RollDrop(GameEngine e, double[] rates, double roll)
        {
            if (!e.DeepMechanics.MidGameGateUnlocked) return null; // GDD 10.2: papers only drop after the mid-game gate
            var order = new[] { SpecialtyPaperColor.Do, SpecialtyPaperColor.Tim, SpecialtyPaperColor.Vang, SpecialtyPaperColor.Lam };
            double acc = 0;
            foreach (var color in order)
            {
                acc += rates[(int)color];
                if (roll < acc)
                {
                    e.Inventory.AddItem(EnhancementTransactions.PaperId(color));
                    return color;
                }
            }
            return null;
        }

        /// <summary>After a won major wave: 1 toss for surviving it, and 1 separate toss when a cycle Boss fell (GDD 10.3).</summary>
        public static string OnMajorWaveWon(GameEngine e, bool bossKilled, Func<double> roll)
        {
            var parts = new System.Collections.Generic.List<string>();
            var a = RollDrop(e, MajorWaveRates, roll());
            if (a.HasValue) parts.Add($"Giấy Chuyên Biệt {a.Value} (sống sót đợt lớn)");
            string bossLoot = "";
            if (bossKilled)
            {
                var b = RollDrop(e, CycleBossRates, roll());
                if (b.HasValue) parts.Add($"Giấy Chuyên Biệt {b.Value} (hạ Boss chu kỳ)");
                bossLoot = RareSources.OnCycleBossDefeated(e, e.Clock.CycleNumber, roll); // C-08
            }
            string papers = parts.Count == 0 ? "" : "Nhận " + string.Join(", ", parts) + ".";
            return (papers + " " + bossLoot).Trim();
        }

        public static bool Owns(GameEngine e, SpecialtyPaperColor color) => e.Inventory.ContainsId(EnhancementTransactions.PaperId(color));

        public static int OwnedCount(GameEngine e, SpecialtyPaperColor color) => e.Inventory.CountId(EnhancementTransactions.PaperId(color));

        /// <summary>Selects the next owned paper colour; returns false when the player owns none.</summary>
        public static bool SelectNextOwned(GameEngine e)
        {
            int cur = (int)e.DeepMechanics.EquippedPaperColor;
            for (int k = 1; k <= 4; k++)
            {
                var color = (SpecialtyPaperColor)((cur + k) % 4);
                if (!Owns(e, color)) continue;
                if (color != e.DeepMechanics.EquippedPaperColor) e.DeepMechanics.intEquippedPaperEnhancementLevel = 0;
                e.DeepMechanics.EquippedPaperColor = color;
                return true;
            }
            return false;
        }

        public static (bool ok, string message) Use(GameEngine e, Func<double> roll)
        {
            var color = e.DeepMechanics.EquippedPaperColor;
            if (!Owns(e, color)) return (false, $"Không có Giấy {color} trong kho.");
            if (e.DeepMechanics.SpecialtyLocks.RevealedLocks >= SpecialtyLockTracker.LockCount) return (false, "Cả 6 khóa đã lộ điều kiện; giữ giấy lại.");
            var (_, msg) = e.DeepMechanics.SpecialtyLocks.UsePaper(color, e.DeepMechanics.intEquippedPaperEnhancementLevel, roll);
            e.Inventory.ConsumeItem(EnhancementTransactions.PaperId(color));
            e.DeepMechanics.intEquippedPaperEnhancementLevel = 0; // giấy đã dùng; tờ kế tiếp bắt đầu từ +0
            e.DeepMechanics.EvaluateMidGameGateAndLocks(e.Farm, e.Herd, e.MajorWavesSurvived);
            return (true, msg);
        }

        /// <summary>Paid enhancement through the checked transaction (gold, recipe, stones, workshop station).</summary>
        public static EnhancementResult Enhance(GameEngine e)
        {
            int stones = 0;
            try { stones = Math.Min(e.Inventory.CountId(EnhancementTransactions.StoneId), EnhancementTransactions.Quote(e).MaxStones); }
            catch (ArgumentException) { }
            return EnhancementTransactions.TryEnhance(e, stones);
        }

        /// <summary>
        /// GDD 10.x material sources: salvaging paper Lam → 2 Tơ Vàng, Vàng → 2 Bột Tử Tinh, Tím → 1 Huyết Triện.
        /// The yield follows the paper's colour; the caller no longer picks a tier (the panel always passed II).
        /// The GDD gives no salvage yield for Đỏ, so a red paper is refused instead of guessing one.
        /// </summary>
        public static (bool ok, string message) Salvage(GameEngine e)
        {
            var color = e.DeepMechanics.EquippedPaperColor;
            if (!Owns(e, color)) return (false, $"Không có Giấy {color} để rã.");
            var y = SalvageYield(color);
            if (y.qty == 0) return (false, $"Giấy {color} không rã được (chưa có vật liệu thu về).");
            e.Inventory.ConsumeItem(EnhancementTransactions.PaperId(color));
            e.DeepMechanics.intEquippedPaperEnhancementLevel = 0;
            e.Inventory.AddItem(y.materialId, y.qty);
            return (true, $"Rã Giấy {color}: +{y.qty} {y.name}.");
        }

        public static (string materialId, string name, int qty) SalvageYield(SpecialtyPaperColor color) => color switch
        {
            SpecialtyPaperColor.Lam => ("to_vang", "Tơ Vàng", 2),
            SpecialtyPaperColor.Vang => ("bot_tu_tinh", "Bột Tử Tinh", 2),
            SpecialtyPaperColor.Tim => ("huyet_trien", "Huyết Triện", 1),
            _ => ("", "", 0),
        };
    }
}
