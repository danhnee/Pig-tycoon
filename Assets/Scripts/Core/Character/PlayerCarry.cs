using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Numbers for ADR-0012. Entries marked OPEN are not in the GDD: conservative defaults, listed as questions in the ADR.
    /// </summary>
    public static class CarryRules
    {
        public const int WheelSlotCount = 6;               // user ruling 30-31 Oct 2026
        public const int BagBasicSlots = 3;                // ADR-0011 / user
        public const int BagPremiumSlots = 8;              // ADR-0011 / user
        public const int WarehouseLinkMaxSlots = 100;      // user ruling
        public const int BagStackPerSlot = 3;              // GDD v6.0 L285: "mỗi loại tối đa 3 cái khi vào đợt quái"
        public const int BagMaxPerType = 3;                // GDD v6.0 L285, all qualities of one item together (ADR-0013)
        public const int HandStackLimit = 1;               // OPEN: hand is "1 stack"; stack size unknown, so 1 item
        public const int WarehouseStackLimitPerSlot = int.MaxValue; // OPEN: legacy warehouse stacks are unbounded
    }

    /// <summary>
    /// The six-slot Tool Wheel. Tool ids are the names of the Presentation StardewToolType values (Core cannot reference them).
    /// Owned tools are all <see cref="AllToolIds"/>; slots hold the equipped subset, the rest is the storage side.
    /// </summary>
    public sealed class ToolWheel
    {
        public static readonly string[] AllToolIds =
        {
            "CamHat", "XoNuoc", "BuaGo", "HangRao", "MangAn", "MangNuoc", "BanChai", "KinhLup", "DaoGo", "KhuTrung", "BachVan"
        };
        // feed bucket, water bucket, repair hammer, weapon, care brush, whistle/horn
        public static readonly string[] DefaultSlots = { "CamHat", "XoNuoc", "BuaGo", "DaoGo", "BanChai", "BachVan" };

        private readonly string[] slots = new string[CarryRules.WheelSlotCount];
        public int SelectedIndex { get; private set; }

        public ToolWheel() { ResetToDefault(); }

        public IReadOnlyList<string> Slots => slots;
        public string SelectedToolId => slots[SelectedIndex];
        public static bool IsKnownTool(string? id) => id != null && Array.IndexOf(AllToolIds, id) >= 0;

        public void ResetToDefault()
        {
            for (int i = 0; i < slots.Length; i++) slots[i] = DefaultSlots[i];
            SelectedIndex = 0;
        }

        public int IndexOf(string toolId) => Array.IndexOf(slots, toolId);
        public IEnumerable<string> StoredToolIds()
        {
            foreach (var id in AllToolIds) if (IndexOf(id) < 0) yield return id;
        }

        public bool TrySelect(int index)
        {
            if (index < 0 || index >= slots.Length || string.IsNullOrEmpty(slots[index])) return false;
            SelectedIndex = index; return true;
        }

        /// <summary>Select an equipped tool by id. False when it is not on the wheel.</summary>
        public bool TrySelectTool(string toolId)
        {
            int i = IndexOf(toolId);
            return i >= 0 && TrySelect(i);
        }

        /// <summary>Cycle to the next (+1) or previous (-1) equipped slot, skipping empty ones.</summary>
        public bool Rotate(int direction)
        {
            if (direction == 0) return false;
            int step = direction > 0 ? 1 : -1;
            for (int n = 1; n < slots.Length; n++)
            {
                int i = ((SelectedIndex + step * n) % slots.Length + slots.Length) % slots.Length;
                if (string.IsNullOrEmpty(slots[i])) continue;
                SelectedIndex = i; return true;
            }
            return false;
        }

        /// <summary>Put a stored (or other-slot) tool on a sector. A tool already on another sector swaps places.</summary>
        public bool TryEquip(int slot, string toolId, out string message)
        {
            if (slot < 0 || slot >= slots.Length) { message = "Ô vòng dụng cụ không hợp lệ."; return false; }
            if (!IsKnownTool(toolId)) { message = "Dụng cụ không tồn tại."; return false; }
            int from = IndexOf(toolId);
            if (from == slot) { message = string.Empty; return true; }
            string displaced = slots[slot];
            slots[slot] = toolId;
            if (from >= 0) slots[from] = displaced; // swap; empty displaced leaves the origin sector empty
            message = string.Empty;
            return true;
        }

        /// <summary>Remove a tool to storage. The wheel always keeps at least one tool; selection moves off an emptied sector.</summary>
        public bool TryUnequip(int slot, out string message)
        {
            if (slot < 0 || slot >= slots.Length || string.IsNullOrEmpty(slots[slot])) { message = "Ô trống."; return false; }
            int equipped = 0;
            foreach (var s in slots) if (!string.IsNullOrEmpty(s)) equipped++;
            if (equipped <= 1) { message = "Vòng cần giữ ít nhất một dụng cụ."; return false; }
            slots[slot] = string.Empty;
            if (SelectedIndex == slot) Rotate(1);
            message = string.Empty;
            return true;
        }

        public ToolWheelSnapshot Capture() => new ToolWheelSnapshot { Slots = new List<string>(slots), SelectedIndex = SelectedIndex };

        /// <summary>Invalid or missing data falls back to the default wheel; nothing is lost (all tools stay owned).</summary>
        public void Restore(ToolWheelSnapshot? saved)
        {
            ResetToDefault();
            if (saved?.Slots == null || saved.Slots.Count != slots.Length) return;
            var seen = new HashSet<string>();
            int equipped = 0;
            foreach (var id in saved.Slots)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!IsKnownTool(id) || !seen.Add(id)) return;
                equipped++;
            }
            if (equipped == 0) return;
            for (int i = 0; i < slots.Length; i++) slots[i] = saved.Slots[i] ?? string.Empty;
            SelectedIndex = 0;
            if (!TrySelect(saved.SelectedIndex)) Rotate(1);
        }
    }

    [Serializable]
    public sealed class ToolWheelSnapshot
    {
        public List<string> Slots { get; set; } = new List<string>();
        public int SelectedIndex { get; set; }
    }

    [Serializable]
    public sealed class PlayerCarrySnapshot
    {
        public bool HasBracelet { get; set; }
        public bool StarterPickupSpawned { get; set; }
        public bool StarterFedHerd { get; set; }
        public bool StarterDepositedItem { get; set; }
        public UnifiedItemInstance? HandItem { get; set; }
        public ToolWheelSnapshot? Wheel { get; set; }
    }

    /// <summary>
    /// ADR-0012. Hand (before the bracelet) / bracelet bag (combat bag, 3..8) / warehouse link (out of combat, 100 slots),
    /// the starter quest that grants the bracelet, and the tool wheel. Bag contents stay in <see cref="SpaceBag"/>.
    /// </summary>
    public sealed class PlayerCarry
    {
        public const string StarterQuestTitle = "Nhiệm vụ khởi đầu";
        public const string StarterQuestObjectives = "Cho đàn ăn một lần; gửi một vật phẩm vào kho.";

        private readonly GameEngine engine;

        public PlayerCarry(GameEngine engine) { this.engine = engine ?? throw new ArgumentNullException(nameof(engine)); }

        public ToolWheel Wheel { get; } = new ToolWheel();
        public bool HasBracelet { get; private set; }
        public UnifiedItemInstance? HandItem { get; private set; }
        public bool StarterFedHerd { get; private set; }
        public bool StarterDepositedItem { get; private set; }
        /// <summary>One-time world pickup (a sack of CamThuong, the trough feed) so the deposit objective is doable from the world.</summary>
        public const string StarterPickupItemId = "CamThuong";
        public bool StarterPickupSpawned { get; private set; }
        public bool ShouldSpawnStarterPickup => !StarterPickupSpawned && !StarterDepositedItem && !HasBracelet && HandItem == null;
        public void MarkStarterPickupSpawned() { StarterPickupSpawned = true; }
        public bool StarterQuestComplete => StarterFedHerd && StarterDepositedItem;

        /// <summary>Raid wave in progress, or inside an occult-forest expedition.</summary>
        public bool InCombat => engine.Clock.IsDuringCombatWave || engine.ForestAndEvents.Expedition.IsActive;

        // ---------------------------------------------------------------- starter quest + bracelet
        public void NotifyHerdFed()
        {
            if (StarterFedHerd) return;
            StarterFedHerd = true;
            TryCompleteStarterQuest();
        }

        private void TryCompleteStarterQuest()
        {
            if (!StarterQuestComplete || HasBracelet) return;
            HasBracelet = true; // basic tier: SpaceBag.Tier stays Basic (3). No premium upgrade command exists yet.
        }

        // ---------------------------------------------------------------- warehouse link
        public bool CanUseWarehouseLink(out string message)
        {
            if (!HasBracelet) { message = "Chưa có Vòng đeo tay: hãy mang đồ tới kho."; return false; }
            if (InCombat) { message = "Liên kết kho đóng trong giao tranh."; return false; }
            message = string.Empty; return true;
        }

        /// <summary>Bag to warehouse (bracelet space). Via the link out of combat, or in person at the warehouse.</summary>
        public bool TryReturnFromBag(int bagIndex, bool inPerson, out string message)
        {
            if (!HasBracelet) { message = "Chưa có Vòng đeo tay."; return false; }
            if (!inPerson && !CanUseWarehouseLink(out message)) return false;
            return engine.SpaceBag.TryReturnOne(engine.Inventory, bagIndex, out message);
        }

        /// <summary>Warehouse to bag. In combat only at the warehouse door; the remote link is closed.</summary>
        public bool TryWithdrawToBag(int warehouseIndex, bool inPerson, out string message)
        {
            if (!HasBracelet) { message = "Chưa có Vòng đeo tay."; return false; }
            if (!inPerson && !CanUseWarehouseLink(out message)) return false;
            return engine.SpaceBag.TryWithdrawOne(engine.Inventory, warehouseIndex, out message);
        }

        // ---------------------------------------------------------------- hand (before the bracelet)
        /// <summary>Before the bracelet: take one warehouse item in hand (at the warehouse, in person).</summary>
        public bool TryTakeToHand(int warehouseIndex, out string message)
        {
            if (HasBracelet) { message = "Đã có Vòng đeo tay: dùng túi."; return false; }
            if (HandItem != null) { message = "Tay đang cầm đồ."; return false; }
            lock (engine.Inventory)
            {
                if (!WarehouseTransfer.TryTakeOne(engine.Inventory, warehouseIndex, out var carried, out message)) return false;
                HandItem = carried;
            }
            message = string.Empty; return true;
        }

        /// <summary>Put the hand item into the warehouse, in person. Counts for the starter quest.</summary>
        public bool TryDepositHand(out string message)
        {
            if (HandItem == null) { message = "Tay không cầm gì."; return false; }
            lock (engine.Inventory)
            {
                if (!WarehouseTransfer.TryPutOne(engine.Inventory, HandItem, out message)) return false;
            }
            HandItem = null;
            StarterDepositedItem = true;
            TryCompleteStarterQuest();
            message = string.Empty; return true;
        }

        // ---------------------------------------------------------------- pickups
        /// <summary>
        /// World pickup of up to <paramref name="quantity"/> items. No bracelet: into the hand (HandStackLimit).
        /// Bracelet in combat: into the bag (one item per slot). Bracelet out of combat: straight to the warehouse link.
        /// Returns how many were accepted; the rest stays on the ground.
        /// </summary>
        public int TryPickUp(string itemId, int quantity, int quality, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) return 0;
            var one = QualifiedItemRegistry.Create(itemId, 1, quality);
            if (ItemContextTagManager.HasBaseTag(one.QualifiedItemId, "story_quest_item"))
            { message = "Chứng cứ được giữ trong sổ tay."; return 0; }
            int accepted = 0;
            if (!HasBracelet)
            {
                if (HandItem == null) { HandItem = one.Clone(); accepted = 1; }
                else if (SameKind(HandItem, one) && HandItem.Stack < CarryRules.HandStackLimit)
                { HandItem.Stack++; accepted = 1; }
                if (accepted == 0) message = "Tay đang cầm đồ: gửi vào kho trước.";
                return accepted;
            }
            if (InCombat)
            {
                while (accepted < quantity && engine.SpaceBag.TryAddCarried(one, out message)) accepted++;
                if (accepted > 0) message = string.Empty;
                return accepted;
            }
            lock (engine.Inventory)
            {
                while (accepted < quantity && WarehouseTransfer.TryPutOne(engine.Inventory, one, out message)) accepted++;
            }
            if (accepted > 0) message = string.Empty;
            return accepted;
        }

        private static bool SameKind(UnifiedItemInstance a, UnifiedItemInstance b) =>
            a.QualifiedItemId == b.QualifiedItemId && a.Quality == b.Quality;

        // ---------------------------------------------------------------- save / load
        public PlayerCarrySnapshot Capture() => new PlayerCarrySnapshot
        {
            HasBracelet = HasBracelet,
            StarterPickupSpawned = StarterPickupSpawned,
            StarterFedHerd = StarterFedHerd,
            StarterDepositedItem = StarterDepositedItem,
            HandItem = HandItem?.Clone(),
            Wheel = Wheel.Capture()
        };

        /// <summary>
        /// Null (old save): pre-bracelet, unless the old save already used the carried bag (items, or the advanced tier),
        /// in which case the bracelet is granted so no possession or purchase is taken away.
        /// </summary>
        public void Restore(PlayerCarrySnapshot? saved, SpaceBag restoredBag)
        {
            HasBracelet = false; StarterPickupSpawned = false; HandItem = null; StarterFedHerd = StarterDepositedItem = false;
            if (saved == null)
            {
                if (restoredBag != null && (restoredBag.Count > 0 || restoredBag.Tier != SpaceBagTier.Basic))
                { HasBracelet = true; StarterFedHerd = StarterDepositedItem = true; }
                Wheel.Restore(null);
                return;
            }
            StarterPickupSpawned = saved.StarterPickupSpawned;
            StarterFedHerd = saved.StarterFedHerd;
            StarterDepositedItem = saved.StarterDepositedItem;
            HasBracelet = saved.HasBracelet || StarterQuestComplete ||
                (restoredBag != null && (restoredBag.Count > 0 || restoredBag.Tier != SpaceBagTier.Basic));
            if (!HasBracelet && saved.HandItem != null && !string.IsNullOrWhiteSpace(saved.HandItem.QualifiedItemId) && saved.HandItem.Stack > 0)
                HandItem = saved.HandItem.Clone();
            Wheel.Restore(saved.Wheel);
        }
    }
}
