using System.Linq;
using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum SpaceBagTier { Basic = 0, Advanced = 1 }

    /// <summary>Explicit persisted carried contents, separate from warehouse inventory.</summary>
    [Serializable]
    public sealed class SpaceBagSnapshot
    {
        public SpaceBagTier Tier { get; set; } = SpaceBagTier.Basic;
        public List<UnifiedItemInstance> Items { get; set; } = new List<UnifiedItemInstance>();

        public SpaceBagSnapshot Clone()
        {
            var copy = new SpaceBagSnapshot { Tier = Tier };
            if (Items == null) { copy.Items = null!; return copy; }
            foreach (var item in Items) copy.Items.Add(item == null ? null! : item.Clone());
            return copy;
        }
    }

    /// <summary>
    /// Carried bag: three individual items initially, eight in the advanced tier.
    /// Latest user instruction overrides GDD v6 paragraph 02368's four-slot quick bag.
    /// Warehouse quantities remain in UnifiedInventory; no automatic legacy migration.
    /// No upgrade command is exposed until a legitimate acquisition source is specified.
    /// </summary>
    public sealed class SpaceBag
    {
        private readonly object sync = new object();
        private readonly List<UnifiedItemInstance> items = new List<UnifiedItemInstance>();
        private SpaceBagTier tier;

        public SpaceBagTier Tier { get { lock (sync) return tier; } }
        public int Capacity { get { lock (sync) return CapacityFor(tier); } }
        public int Count { get { lock (sync) return items.Count; } }
        public IReadOnlyList<UnifiedItemInstance> Items => Capture().Items.AsReadOnly();

        private static int CapacityFor(SpaceBagTier value) => value == SpaceBagTier.Advanced ? 8 : 3;

        /// <summary>
        /// Slots in use: same item + same quality share a slot up to CarryRules.BagStackPerSlot
        /// (GDD v6.0 L285 "mỗi loại tối đa 3 cái khi vào đợt quái").
        /// </summary>
        private static int SlotsUsed(IEnumerable<UnifiedItemInstance> list)
        {
            int slots = 0;
            foreach (var g in list.GroupBy(i => (i.QualifiedItemId, i.Quality)))
            {
                slots += (g.Count() + CarryRules.BagStackPerSlot - 1) / CarryRules.BagStackPerSlot;
            }
            return slots;
        }

        private bool HasRoomFor(UnifiedItemInstance incoming)
        {
            // GDD L285 per-type cap counts every quality of the item together (ADR-0013), so quality splits
            // cannot carry more than three of one type. Applied on add only: restore keeps old saves loadable.
            int sameType = items.Count(i => string.Equals(i.QualifiedItemId, incoming.QualifiedItemId, StringComparison.OrdinalIgnoreCase));
            if (sameType >= CarryRules.BagMaxPerType) return false;
            var probe = new List<UnifiedItemInstance>(items) { incoming };
            return SlotsUsed(probe) <= CapacityFor(tier);
        }

        /// <summary>Slots currently used (for UI sockets).</summary>
        public int UsedSlots { get { lock (sync) return SlotsUsed(items); } }
        private static bool IsStoryEvidence(UnifiedItemInstance item) =>
            ItemContextTagManager.HasBaseTag(item.QualifiedItemId, "story_quest_item");

        public SpaceBagSnapshot Capture()
        {
            lock (sync)
            {
                var saved = new SpaceBagSnapshot { Tier = tier };
                foreach (var item in items) saved.Items.Add(item.Clone());
                return saved;
            }
        }

        /// <summary>
        /// Load-only restoration, including explicit saved advanced ownership. Null is an old
        /// save with no bag: empty basic bag. Invalid data is refused without changing this bag.
        /// The caller must retain invalid save data for recovery; no items are silently trimmed.
        /// </summary>
        public bool TryRestore(SpaceBagSnapshot? saved, out string message)
        {
            lock (sync)
            {
                if (saved == null)
                {
                    items.Clear(); tier = SpaceBagTier.Basic; message = string.Empty; return true;
                }
                if ((saved.Tier != SpaceBagTier.Basic && saved.Tier != SpaceBagTier.Advanced) ||
                    saved.Items == null || SlotsUsed(saved.Items.Where(i => i != null)) > CapacityFor(saved.Tier))
                { message = "Dữ liệu túi không gian không hợp lệ; giữ nguyên vật phẩm để khôi phục."; return false; }
                var restored = new List<UnifiedItemInstance>();
                foreach (var item in saved.Items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.QualifiedItemId) || item.Stack != 1 || item.Quality < 0 || IsStoryEvidence(item))
                    { message = "Vật phẩm lưu trong túi không hợp lệ; không tự bỏ hay đổi vật phẩm."; return false; }
                    restored.Add(item.Clone());
                }
                items.Clear(); items.AddRange(restored); tier = saved.Tier;
                message = string.Empty; return true;
            }
        }

        public bool TryWithdrawOne(UnifiedInventory warehouse, int warehouseIndex, out string message)
        {
            if (warehouse == null) throw new ArgumentNullException(nameof(warehouse));
            lock (sync) lock (warehouse)
            {
                if (!WarehouseTransfer.TryTakeOne(warehouse, warehouseIndex, out var carried, out message)) return false;
                if (!HasRoomFor(carried))
                {
                    // put it straight back under the same locks: no item is lost or duplicated
                    WarehouseTransfer.TryPutOne(warehouse, carried, out _);
                    message = "Túi đã đầy."; return false;
                }
                items.Add(carried);
                message = string.Empty; return true;
            }
        }

        public bool TryReturnOne(UnifiedInventory warehouse, int bagIndex, out string message)
        {
            if (warehouse == null) throw new ArgumentNullException(nameof(warehouse));
            lock (sync) lock (warehouse)
            {
                if (bagIndex < 0 || bagIndex >= items.Count)
                { message = "Vật phẩm không còn trong túi."; return false; }
                if (!WarehouseTransfer.TryPutOne(warehouse, items[bagIndex], out message)) return false;
                items.RemoveAt(bagIndex);
                message = string.Empty; return true;
            }
        }

        /// <summary>
        /// Combat bag add (loot picked up during a raid or expedition). Capacity is the bracelet's own space.
        /// </summary>
        internal bool TryAddCarried(UnifiedItemInstance item, out string message)
        {
            lock (sync)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.QualifiedItemId) || item.Stack != 1 || item.Quality < 0 || IsStoryEvidence(item))
                { message = "Vật phẩm không thể cho vào túi."; return false; }
                if (!HasRoomFor(item)) { message = "Túi đã đầy."; return false; }
                items.Add(item.Clone()); message = string.Empty; return true;
            }
        }

        /// <summary>Consume one already-carried item; warehouse stocks are not touched.</summary>
        public bool TryConsumeOne(int bagIndex, out UnifiedItemInstance? consumed, out string message)
        {
            lock (sync)
            {
                consumed = null;
                if (bagIndex < 0 || bagIndex >= items.Count)
                { message = "Vật phẩm không còn trong túi."; return false; }
                consumed = items[bagIndex].Clone(); items.RemoveAt(bagIndex);
                message = string.Empty; return true;
            }
        }
    }

    /// <summary>
    /// One-item moves between a carried space (hand or bag) and the warehouse, shared by SpaceBag and the hand.
    /// Callers hold the locks. The warehouse space has a hard slot ceiling (<see cref="CarryRules.WarehouseLinkMaxSlots"/>).
    /// </summary>
    internal static class WarehouseTransfer
    {
        public static bool TryTakeOne(UnifiedInventory warehouse, int warehouseIndex, out UnifiedItemInstance carried, out string message)
        {
            carried = null!;
            if (warehouseIndex < 0 || warehouseIndex >= warehouse.Slots.Count)
            { message = "Vật phẩm không còn trong kho."; return false; }
            var owned = warehouse.Slots[warehouseIndex];
            if (owned.Stack <= 0 || string.IsNullOrWhiteSpace(owned.QualifiedItemId) || owned.Quality < 0)
            { message = "Vật phẩm trong kho không hợp lệ."; return false; }
            if (ItemContextTagManager.HasBaseTag(owned.QualifiedItemId, "story_quest_item"))
            { message = "Chứng cứ được giữ trong sổ tay, không cho vào túi không gian."; return false; }

            var after = Copy(warehouse);
            carried = owned.Clone(1);
            if (--after[warehouseIndex].Stack == 0) after.RemoveAt(warehouseIndex);
            // Existing warehouse API updates bound Farm stocks but selects by ID only.
            // Restore the precomputed exact-quality result to preserve every other stack.
            if (!warehouse.ConsumeItem(owned.QualifiedItemId, 1))
            { message = "Kho không có đủ vật phẩm để lấy."; return false; }
            warehouse.ClearAndRestore(after);
            message = string.Empty; return true;
        }

        public static bool TryPutOne(UnifiedInventory warehouse, UnifiedItemInstance carried, out string message)
        {
            long total = 0;
            foreach (var item in warehouse.Slots)
                if (string.Equals(item.QualifiedItemId, carried.QualifiedItemId, StringComparison.OrdinalIgnoreCase)) total += item.Stack;
            if (total >= int.MaxValue)
            { message = "Kho không thể nhận thêm số lượng này."; return false; }

            var after = Copy(warehouse);
            var matching = after.Find(item => item.Quality == carried.Quality &&
                string.Equals(item.QualifiedItemId, carried.QualifiedItemId, StringComparison.OrdinalIgnoreCase) &&
                item.ItemId == carried.ItemId && item.DisplayName == carried.DisplayName);
            if (matching == null && after.Count >= CarryRules.WarehouseLinkMaxSlots)
            { message = "Kho đã đủ " + CarryRules.WarehouseLinkMaxSlots + " ô loại vật phẩm."; return false; }
            if (matching == null) after.Add(carried.Clone()); else matching.Stack++;
            warehouse.AddItem(carried.QualifiedItemId, 1, carried.Quality);
            warehouse.ClearAndRestore(after);
            message = string.Empty; return true;
        }

        static List<UnifiedItemInstance> Copy(UnifiedInventory warehouse)
        {
            var copy = new List<UnifiedItemInstance>();
            foreach (var item in warehouse.Slots) copy.Add(item.Clone());
            return copy;
        }
    }
}
