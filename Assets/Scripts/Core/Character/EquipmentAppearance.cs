using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>One visible piece on the character: which slot, which art family draws it, where it sits in the draw order.</summary>
    public sealed class GearLayer
    {
        public EquipSlot Slot;
        public string ItemBaseId = "";
        public ItemTier Quality = ItemTier.I;
        /// <summary>Sprite-library family for this piece on this protagonist, e.g. "an__wpn_dao_rua".</summary>
        public string Family = "";
        /// <summary>Same piece drawn for its quality, e.g. "an__wpn_dao_rua__III" (art optional; falls back to <see cref="Family"/>).</summary>
        public string QualityFamily = "";
        /// <summary>Sorting offset relative to the body sprite (negative = behind the body).</summary>
        public int Order;
    }

    /// <summary>
    /// Paper-doll appearance (GDD 12.1: six independent slots, no sets). The body sheet is the protagonist in plain
    /// clothes with nothing in hand; every equipped piece adds its own layer, synced frame by frame with the body clip.
    /// Changing one slot changes only that layer. Pieces whose art is missing draw nothing (the body still shows).
    /// </summary>
    public static class EquipmentAppearance
    {
        /// <summary>Draw order bottom→top when facing the camera. Weapon and bracelet move behind the body when the
        /// hand holding them is on the far side (facing up, and facing left with a right-hand weapon).</summary>
        static readonly EquipSlot[] FrontOrder = { EquipSlot.Pants, EquipSlot.Boots, EquipSlot.BodyArmor, EquipSlot.Necklace, EquipSlot.Bracelet, EquipSlot.Weapon };

        public static int Order(EquipSlot slot, CardinalDirection facing)
        {
            bool farHand = facing == CardinalDirection.North || facing == CardinalDirection.West;
            if (farHand && (slot == EquipSlot.Weapon || slot == EquipSlot.Bracelet)) return slot == EquipSlot.Weapon ? -2 : -1;
            if (facing == CardinalDirection.North && slot == EquipSlot.Necklace) return -3; // necklace hidden by the back
            int i = System.Array.IndexOf(FrontOrder, slot);
            return i < 0 ? 1 : i + 1;
        }

        /// <summary>
        /// Weapons are drawn only while the body plays a fighting clip (attack, skill, counter, guard), like a
        /// Stardew tool that appears when swung: walking, idling and farm work show empty hands (4 Nov user report
        /// "vũ khí gắn chặt dù chưa trang bị"). Other slots are worn, so they always show.
        /// </summary>
        public static bool ShowsWeapon(string? clip)
        {
            if (string.IsNullOrEmpty(clip)) return false;
            return clip!.StartsWith("attack") || clip.StartsWith("skill") || clip.StartsWith("counter") || clip.StartsWith("guard");
        }

        public static bool Visible(EquipSlot slot, string? clip) => slot != EquipSlot.Weapon || ShowsWeapon(clip);

        /// <summary>The unarmed body sheet family the gear layers sit on.</summary>
        public static string BaseBodyFamily(PlayerId who) => who == PlayerId.Khoa ? "khoa" : "an";
        public static string Prefix(PlayerId who) => who == PlayerId.Khoa ? "khoa__" : "an__";
        public static string FamilyFor(PlayerId who, string itemBaseId) => Prefix(who) + itemBaseId;

        /// <summary>The layers to draw for the character's current equipment and facing.</summary>
        public static List<GearLayer> Layers(CharacterData c, CardinalDirection facing)
        {
            var list = new List<GearLayer>();
            if (c == null) return list;
            foreach (var kv in c.Equipment)
            {
                var item = kv.Value;
                if (item == null) continue;
                string family = FamilyFor(c.Identity, item.BaseId);
                list.Add(new GearLayer
                {
                    Slot = kv.Key,
                    ItemBaseId = item.BaseId,
                    Quality = item.Quality,
                    Family = family,
                    QualityFamily = item.Quality == ItemTier.I ? family : family + "__" + item.Quality,
                    Order = Order(kv.Key, facing),
                });
            }
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            return list;
        }

        /// <summary>Every (protagonist, piece) art family the paper-doll can ask for: the art request list.</summary>
        public static IEnumerable<string> AllFamilies()
        {
            foreach (var who in new[] { PlayerId.An, PlayerId.Khoa })
                foreach (var item in EquipmentAndTacticalCatalog.AllEquipmentItems)
                    yield return FamilyFor(who, item.Id);
        }
    }
}
