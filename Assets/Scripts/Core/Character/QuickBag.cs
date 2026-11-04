using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    public enum QuickItemId { BinhHoiLuc, BangGac, LuuKhoi, BomKeo, PhaoSang, TinhNoDan }

    /// <summary>
    /// GDD 12.5 túi nhanh: 4 ô, mỗi loại tối đa 3 cái khi vào đợt quái. Combat items with the GDD tier, price (day market,
    /// gold, tier I only) and mechanic. Items without a gold price come from trade, auction or events.
    /// </summary>
    public static class QuickBag
    {
        public const int Slots = 4;
        public const int MaxPerKind = 3;

        public sealed class Def
        {
            public QuickItemId Id;
            public string Name = "";
            public int Tier;
            public int PriceGold; // 0 = không bán bằng vàng
            public string Text = "";
            public string Source = "";
        }

        public static readonly IReadOnlyList<Def> All = new List<Def>
        {
            new Def { Id = QuickItemId.BinhHoiLuc, Name = "Bình Hồi Lực", Tier = 1, PriceGold = 90, Text = "Hoàn 40 Stamina phần hao phí tức thời; uống 1 giây, trúng đòn bị ngắt", Source = "Chợ ngày 90G" },
            new Def { Id = QuickItemId.BangGac, Name = "Băng Gạc", Tier = 1, PriceGold = 60, Text = "+30 Chống Chịu trong 5 giây; dùng được cho lính", Source = "Chợ ngày 60G" },
            new Def { Id = QuickItemId.LuuKhoi, Name = "Lựu Khói", Tier = 2, Text = "Quái trong 4 m mất mục tiêu 3 giây — để rút lui", Source = "Trao đổi" },
            new Def { Id = QuickItemId.BomKeo, Name = "Bom Keo", Tier = 2, Text = "Vũng keo 3 m, 5 giây — dựng cửa hẹp tạm", Source = "Trao đổi" },
            new Def { Id = QuickItemId.PhaoSang, Name = "Pháo Sáng", Tier = 1, PriceGold = 80, Text = "Lộ tàng hình trong 12 m, 10 giây", Source = "Chợ ngày 80G" },
            new Def { Id = QuickItemId.TinhNoDan, Name = "Tinh Nộ Đan", Tier = 3, Text = "+50 Nộ Khí ngay", Source = "Đấu giá" },
        };

        public static Def Get(QuickItemId id) => All.First(d => d.Id == id);

        public static int Count(CharacterData c, QuickItemId id) => c.QuickItems.TryGetValue(id, out var n) ? n : 0;

        public static List<QuickItemId> Kinds(CharacterData c) =>
            c.QuickItems.Where(kv => kv.Value > 0).Select(kv => kv.Key).OrderBy(k => (int)k).ToList();

        public static (bool ok, string message) CanAdd(CharacterData c, QuickItemId id)
        {
            int have = Count(c, id);
            if (have >= MaxPerKind) return (false, $"{Get(id).Name}: tối đa {MaxPerKind} cái.");
            if (have == 0 && Kinds(c).Count >= Slots) return (false, $"Túi nhanh chỉ có {Slots} ô.");
            return (true, "");
        }

        public static (bool ok, string message) Add(CharacterData c, QuickItemId id)
        {
            var (ok, msg) = CanAdd(c, id);
            if (!ok) return (false, msg);
            c.QuickItems[id] = Count(c, id) + 1;
            return (true, $"+1 {Get(id).Name} ({Count(c, id)}/{MaxPerKind}).");
        }

        /// <summary>Day market sells tier-I items for gold (GDD 12.5 / 20.3).</summary>
        public static (bool ok, string message) Buy(GameEngine e, QuickItemId id)
        {
            var d = Get(id);
            if (d.PriceGold <= 0) return (false, $"{d.Name}: không bán bằng vàng ({d.Source}).");
            var (can, why) = CanAdd(e.Character, id);
            if (!can) return (false, why);
            if (!e.Ledger.TrySpend(d.PriceGold, LedgerReason.BuyEquipment, "quick_" + id)) return (false, $"Không đủ vàng ({d.PriceGold}G).");
            return Add(e.Character, id);
        }

        /// <summary>Warehouse id of a quick-bag kind (the same ids the night market and the warehouse use).</summary>
        public static string WarehouseId(QuickItemId id) => id.ToString();

        public static bool TryParseWarehouseId(string itemId, out QuickItemId id) =>
            Enum.TryParse(itemId, out id) && Enum.IsDefined(typeof(QuickItemId), id);

        /// <summary>
        /// Puts one item from the warehouse into the quick bag (3 Nov: bought or bartered Bình Hồi Lực sat in the warehouse
        /// with no way into the F5–F8 bag). Same rule as the bracelet: through the link out of combat, or in person at the
        /// warehouse door (ADR-0012).
        /// </summary>
        public static (bool ok, string message) LoadFromWarehouse(GameEngine e, QuickItemId id, bool inPerson = false)
        {
            if (!inPerson && !e.Carry.CanUseWarehouseLink(out var why)) return (false, why);
            string wid = WarehouseId(id);
            if (!e.Inventory.ContainsId(wid)) return (false, $"Kho không có {Get(id).Name}.");
            var (can, full) = CanAdd(e.Character, id);
            if (!can) return (false, full);
            e.Inventory.ConsumeItem(wid);
            return Add(e.Character, id);
        }

        public static (bool ok, string message) UnloadToWarehouse(GameEngine e, QuickItemId id, bool inPerson = false)
        {
            if (!inPerson && !e.Carry.CanUseWarehouseLink(out var why)) return (false, why);
            if (Count(e.Character, id) <= 0) return (false, $"Túi nhanh không có {Get(id).Name}.");
            e.Character.QuickItems[id] = Count(e.Character, id) - 1;
            if (e.Character.QuickItems[id] <= 0) e.Character.QuickItems.Remove(id);
            e.Inventory.AddItem(WarehouseId(id), 1);
            return (true, $"Cất 1 {Get(id).Name} vào kho.");
        }

        /// <summary>Bình Hồi Lực is drunk over this many seconds; a hit in that time interrupts it and keeps the bottle (C-13).</summary>
        public const float DrinkSeconds = 1f;

        /// <summary>
        /// Uses one item. Core applies the stat part (Stamina, Nộ); the runtime plays the area part (smoke, glue, flare)
        /// and the 5-second bandage tick, and is told which by the returned id.
        /// </summary>
        public static (bool ok, string message) Use(CharacterData c, QuickItemId id)
        {
            if (Count(c, id) <= 0) return (false, $"Hết {Get(id).Name}.");
            c.QuickItems[id] = Count(c, id) - 1;
            if (c.QuickItems[id] <= 0) c.QuickItems.Remove(id);
            switch (id)
            {
                case QuickItemId.BinhHoiLuc:
                    c.Stamina.CurrentStamina = Math.Min(c.GetEffectiveMaxStamina(), c.Stamina.CurrentStamina + 40f);
                    break;
                case QuickItemId.TinhNoDan:
                    c.Rage = Math.Min(100, c.Rage + 50);
                    break;
            }
            return (true, Get(id).Name + ": " + Get(id).Text);
        }

        public static List<string> Save(CharacterData c) => c.QuickItems.Select(kv => kv.Key + ":" + kv.Value).ToList();

        public static void Load(CharacterData c, List<string>? entries)
        {
            c.QuickItems.Clear();
            if (entries == null) return;
            foreach (var e in entries)
            {
                var parts = e.Split(':');
                if (parts.Length == 2 && Enum.TryParse(parts[0], out QuickItemId id) && int.TryParse(parts[1], out int n) && n > 0)
                    c.QuickItems[id] = Math.Min(MaxPerKind, n);
            }
        }
    }
}
