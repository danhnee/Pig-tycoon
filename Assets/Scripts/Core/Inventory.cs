#nullable enable
using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class InventoryStack
    {
        public string ItemId = string.Empty;
        public float Quantity;
        public float MaxStack = 99f;
    }

    [Serializable]
    public class InventoryManager
    {
        public const int StorageSlotCount = 27;
        public const int HotbarSlotCount = 8;
        private static readonly string[] DefaultHotbarBindings =
        {
            "cam_heo", "xo_nuoc", "bua_go", "coc_go",
            "mang_an", "mang_nuoc", "ban_chai", "kinh_lup"
        };

        public List<InventoryStack?> Slots = new List<InventoryStack?>();
        public List<string> HotbarItemIds = new List<string>();
        public int SelectedHotbarIndex;
        // Đọc tương thích save v2 được tạo trước khi chuyển sang inventory theo ô.
        public List<InventoryStack?> Items = new List<InventoryStack?>();
        // Các trường scalar vẫn được serialize để đọc save Unity JsonUtility phiên bản 2.
        public float FeedKg;
        public float WaterLiters;
        public int CarriedFences = 10;
        public int CarriedFeeders = 2;
        public int CarriedWaterTroughs = 2;

        public InventoryManager()
        {
            for (int i = 0; i < StorageSlotCount; i++)
                Slots.Add(null);

            // Dữ liệu khởi đầu thực của game được khởi tạo trong Core.
            SetStartingItem(2, "bua_go", 1f);
            SetStartingItem(3, "coc_go", 10f);
            SetStartingItem(4, "mang_an", 2f);
            SetStartingItem(5, "mang_nuoc", 2f);
            SetStartingItem(6, "ban_chai", 1f);
            SetStartingItem(7, "kinh_lup", 1f);
            SetStartingItem(8, "dao_gam", 1f);
            SetStartingItem(9, "khu_trung", 5f);
            SetStartingItem(10, "bach_van", 1f);
            SetStartingItem(12, "thao_duoc", 3f);
            SetStartingItem(13, "trai_tim", 5f);

            HotbarItemIds.AddRange(DefaultHotbarBindings);
            SyncLegacyFieldsFromSlots();
        }

        private void SetStartingItem(int slot, string itemId, float quantity)
        {
            Slots[slot] = new InventoryStack
            {
                ItemId = itemId,
                Quantity = quantity,
                MaxStack = GetDefaultMaxStack(itemId)
            };
        }

        public float GetQuantity(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || Slots == null)
                return 0f;

            float total = 0f;
            foreach (var stack in Slots)
            {
                if (stack != null && stack.ItemId == itemId)
                    total += stack.Quantity;
            }
            return total;
        }

        public float AddItem(string itemId, float quantity, float maxStack = 99f)
        {
            ValidateItem(itemId);
            if (quantity < 0f || float.IsNaN(quantity) || float.IsInfinity(quantity))
                throw new ArgumentOutOfRangeException(nameof(quantity));
            if (maxStack <= 0f || float.IsNaN(maxStack) || float.IsInfinity(maxStack))
                throw new ArgumentOutOfRangeException(nameof(maxStack));
            if (quantity == 0f)
                return 0f;

            bool isContainerResource = itemId == "cam_heo" || itemId == "xo_nuoc";
            int firstSlot = itemId == "cam_heo" ? 0 : itemId == "xo_nuoc" ? 1 : 2;
            int endSlot = isContainerResource ? firstSlot + 1 : Slots.Count;
            if (isContainerResource)
                maxStack = GetDefaultMaxStack(itemId);

            float remaining = quantity;
            for (int i = firstSlot; i < endSlot && remaining > 0f; i++)
            {
                var stack = Slots[i];
                if (stack == null || stack.ItemId != itemId)
                    continue;

                float stackLimit = stack.MaxStack > 0f ? stack.MaxStack : maxStack;
                float moved = Math.Min(remaining, Math.Max(0f, stackLimit - stack.Quantity));
                stack.Quantity += moved;
                remaining -= moved;
            }

            for (int i = firstSlot; i < endSlot && remaining > 0f; i++)
            {
                if (Slots[i] != null)
                    continue;

                float moved = Math.Min(remaining, maxStack);
                Slots[i] = new InventoryStack
                {
                    ItemId = itemId,
                    Quantity = moved,
                    MaxStack = maxStack
                };
                remaining -= moved;
            }

            SyncLegacyFieldsFromSlots();
            return remaining;
        }

        public void SetQuantity(string itemId, float quantity, float maxStack = 99f)
        {
            ValidateItem(itemId);
            if (quantity < 0f || float.IsNaN(quantity) || float.IsInfinity(quantity))
                throw new ArgumentOutOfRangeException(nameof(quantity));
            if (quantity > 0f && (maxStack <= 0f || float.IsNaN(maxStack) || float.IsInfinity(maxStack)))
                throw new ArgumentOutOfRangeException(nameof(maxStack));

            if (Slots == null)
                Slots = new List<InventoryStack?>();

            var previousSlots = new List<InventoryStack?>();
            foreach (var slot in Slots)
            {
                previousSlots.Add(slot == null ? null : new InventoryStack
                {
                    ItemId = slot.ItemId,
                    Quantity = slot.Quantity,
                    MaxStack = slot.MaxStack
                });
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                if (slot != null && slot.ItemId == itemId)
                    Slots[i] = null;
            }

            if (quantity == 0f)
            {
                SyncLegacyFieldsFromSlots();
                return;
            }

            float remaining = AddItem(itemId, quantity, maxStack);
            if (remaining > 0f)
            {
                Slots = previousSlots;
                SyncLegacyFieldsFromSlots();
                throw new InvalidOperationException("Không đủ ô trống để đặt số lượng vật phẩm yêu cầu.");
            }
            SyncLegacyFieldsFromSlots();
        }

        public bool TryRemove(string itemId, float quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0f || float.IsNaN(quantity) || float.IsInfinity(quantity) || GetQuantity(itemId) < quantity)
                return false;

            float remaining = quantity;
            for (int i = 0; i < Slots.Count && remaining > 0f; i++)
            {
                var stack = Slots[i];
                if (stack == null || stack.ItemId != itemId)
                    continue;

                float removed = Math.Min(stack.Quantity, remaining);
                stack.Quantity -= removed;
                remaining -= removed;
                if (stack.Quantity <= 0f)
                    Slots[i] = null;
            }

            SyncLegacyFieldsFromSlots();
            return remaining <= 0f;
        }

        public bool TryConsume(string itemId, int quantity = 1)
        {
            return TryRemove(itemId, quantity);
        }

        public bool TryMove(int sourceSlot, int targetSlot, float quantity = float.PositiveInfinity)
        {
            if (!IsValidSlot(sourceSlot) || !IsValidSlot(targetSlot) || sourceSlot == targetSlot ||
                sourceSlot < 2 || targetSlot < 2 || quantity <= 0f || float.IsNaN(quantity))
                return false;

            var source = Slots[sourceSlot];
            if (source == null || source.Quantity <= 0f)
                return false;

            float movedQuantity = Math.Min(source.Quantity, quantity);
            var target = Slots[targetSlot];

            if (target == null)
            {
                if (movedQuantity == source.Quantity)
                {
                    Slots[targetSlot] = source;
                    Slots[sourceSlot] = null;
                }
                else
                {
                    Slots[targetSlot] = new InventoryStack
                    {
                        ItemId = source.ItemId,
                        Quantity = movedQuantity,
                        MaxStack = source.MaxStack
                    };
                    source.Quantity -= movedQuantity;
                }
                SyncLegacyFieldsFromSlots();
                return true;
            }

            if (target.ItemId == source.ItemId)
            {
                float moved = Math.Min(movedQuantity, Math.Max(0f, target.MaxStack - target.Quantity));
                if (moved <= 0f)
                    return false;

                target.Quantity += moved;
                source.Quantity -= moved;
                if (source.Quantity <= 0f)
                    Slots[sourceSlot] = null;
                SyncLegacyFieldsFromSlots();
                return true;
            }

            if (movedQuantity != source.Quantity)
                return false;

            Slots[sourceSlot] = target;
            Slots[targetSlot] = source;
            SyncLegacyFieldsFromSlots();
            return true;
        }

        public bool BindHotbar(int hotbarIndex, string itemId)
        {
            if (hotbarIndex < 0 || hotbarIndex >= HotbarSlotCount || string.IsNullOrWhiteSpace(itemId))
                return false;

            if (HotbarItemIds == null)
                HotbarItemIds = new List<string>();
            while (HotbarItemIds.Count < HotbarSlotCount)
                HotbarItemIds.Add(string.Empty);

            HotbarItemIds[hotbarIndex] = itemId;
            return true;
        }

        public string GetHotbarItemId(int hotbarIndex)
        {
            return HotbarItemIds != null && hotbarIndex >= 0 && hotbarIndex < HotbarItemIds.Count
                ? HotbarItemIds[hotbarIndex]
                : string.Empty;
        }

        public void SelectHotbarSlot(int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= HotbarSlotCount)
                throw new ArgumentOutOfRangeException(nameof(hotbarIndex));
            SelectedHotbarIndex = hotbarIndex;
        }

        public InventoryManager DeepCopy()
        {
            float legacyFeedKg = FeedKg;
            float legacyWaterLiters = WaterLiters;
            int legacyCarriedFences = CarriedFences;
            int legacyCarriedFeeders = CarriedFeeders;
            int legacyCarriedWaterTroughs = CarriedWaterTroughs;

            var copy = new InventoryManager
            {
                HotbarItemIds = new List<string>(),
                Items = new List<InventoryStack?>(),
                SelectedHotbarIndex = Math.Max(0, Math.Min(HotbarSlotCount - 1, SelectedHotbarIndex))
            };

            bool hasLegacyItems = Items != null && Items.Count > 0;
            if (hasLegacyItems)
            {
                foreach (var legacyStack in Items ?? new List<InventoryStack?>())
                {
                    if (legacyStack == null || string.IsNullOrWhiteSpace(legacyStack.ItemId) || legacyStack.Quantity <= 0f)
                        continue;

                    try
                    {
                        copy.SetQuantity(
                            legacyStack.ItemId,
                            legacyStack.Quantity,
                            legacyStack.MaxStack > 0f ? legacyStack.MaxStack : 99f);
                    }
                    catch (InvalidOperationException)
                    {
                        // Save cũ có thể vượt sức chứa mới; giữ các vật phẩm khác đọc được.
                    }
                }
            }
            else
            {
                copy.Slots = new List<InventoryStack?>();
                List<InventoryStack?>? sourceSlots = Slots ?? new List<InventoryStack?>();
                for (int i = 0; i < StorageSlotCount; i++)
                {
                    var stack = i < sourceSlots.Count ? sourceSlots[i] : null;
                    copy.Slots.Add(stack == null || string.IsNullOrWhiteSpace(stack.ItemId) || stack.Quantity <= 0f
                        ? null
                        : new InventoryStack
                        {
                            ItemId = stack.ItemId,
                            Quantity = stack.Quantity,
                            MaxStack = stack.MaxStack > 0f ? stack.MaxStack : 99f
                        });
                }
            }

            for (int i = 0; i < HotbarSlotCount; i++)
            {
                string itemId = GetHotbarItemId(i);
                copy.HotbarItemIds.Add(string.IsNullOrWhiteSpace(itemId) ? DefaultHotbarBindings[i] : itemId);
            }

            // Các bản save v2 đời trước giữ tài nguyên mang theo ở thuộc tính riêng,
            // còn stack vật phẩm trong Items. Đưa các giá trị đó vào ô mới sau khi chép.
            copy.FeedKg = legacyFeedKg;
            copy.WaterLiters = legacyWaterLiters;
            copy.CarriedFences = legacyCarriedFences;
            copy.CarriedFeeders = legacyCarriedFeeders;
            copy.CarriedWaterTroughs = legacyCarriedWaterTroughs;

            if (hasLegacyItems)
            {
                copy.SetQuantity("cam_heo", Math.Max(0f, legacyFeedKg), 20f);
                copy.SetQuantity("xo_nuoc", Math.Max(0f, legacyWaterLiters), 50f);
                copy.SetQuantity("coc_go", Math.Max(0, legacyCarriedFences));
                copy.SetQuantity("mang_an", Math.Max(0, legacyCarriedFeeders));
                copy.SetQuantity("mang_nuoc", Math.Max(0, legacyCarriedWaterTroughs));
            }
            else
            {
                copy.SyncLegacyFieldsFromSlots();
            }

            return copy;
        }

        private bool IsValidSlot(int slot) => slot >= 0 && slot < StorageSlotCount;

        private static void ValidateItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Mã vật phẩm không hợp lệ.", nameof(itemId));
        }

        private static float GetDefaultMaxStack(string itemId)
        {
            return itemId == "cam_heo" ? 20f : itemId == "xo_nuoc" ? 50f : 99f;
        }

        private void SyncLegacyFieldsFromSlots()
        {
            FeedKg = GetQuantity("cam_heo");
            WaterLiters = GetQuantity("xo_nuoc");
            CarriedFences = (int)GetQuantity("coc_go");
            CarriedFeeders = (int)GetQuantity("mang_an");
            CarriedWaterTroughs = (int)GetQuantity("mang_nuoc");
        }
    }
}
