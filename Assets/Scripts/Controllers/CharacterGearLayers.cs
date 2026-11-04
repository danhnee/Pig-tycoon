using System.Collections.Generic;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Paper-doll equipment on the player (GDD 12.1, <see cref="EquipmentAppearance"/>): one child SpriteRenderer per
    /// equipped slot, each showing the same clip / frame / facing as the body from that piece's own sheet family
    /// ("an__wpn_dao_rua", optionally "an__wpn_dao_rua__III"). No sets: each slot is independent; a piece without art
    /// draws nothing. Sorting follows the body (Y-sort) with per-slot offsets from Core.
    /// </summary>
    [RequireComponent(typeof(CharacterSpriteAnimator))]
    public sealed class CharacterGearLayers : MonoBehaviour
    {
        CharacterSpriteAnimator body;
        SpriteRenderer bodyRenderer;
        readonly Dictionary<EquipSlot, SpriteRenderer> layers = new Dictionary<EquipSlot, SpriteRenderer>();
        /// <summary>True when the paper-doll drew the weapon layer this frame (the socket overlay then stays hidden).</summary>
        public bool DrewWeapon { get; private set; }
        readonly Dictionary<string, PixelMythSpriteLibrary> libCache = new Dictionary<string, PixelMythSpriteLibrary>();

        void Awake()
        {
            body = GetComponent<CharacterSpriteAnimator>();
            bodyRenderer = GetComponent<SpriteRenderer>();
        }

        SpriteRenderer LayerFor(EquipSlot slot)
        {
            if (layers.TryGetValue(slot, out var r) && r != null) return r;
            var go = new GameObject("Gear_" + slot);
            go.transform.SetParent(transform, false);
            r = go.AddComponent<SpriteRenderer>();
            r.sharedMaterial = bodyRenderer.sharedMaterial;
            layers[slot] = r;
            return r;
        }

        PixelMythSpriteLibrary Library(string family)
        {
            if (string.IsNullOrEmpty(family)) return null;
            if (!libCache.TryGetValue(family, out var lib))
            {
                lib = PixelMythRegistry.Find(family);
                libCache[family] = lib; // cache misses too: missing art is the normal case until it is drawn
            }
            return lib;
        }

        static CardinalDirection ToCardinal(CharacterFacing f) => f switch
        {
            CharacterFacing.Up => CardinalDirection.North,
            CharacterFacing.Left => CardinalDirection.West,
            CharacterFacing.Right or CharacterFacing.Side => CardinalDirection.East,
            _ => CardinalDirection.South,
        };

        void LateUpdate()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            var c = engine?.Character;
            var myth = body != null ? body.Myth : null;
            foreach (var r in layers.Values) if (r != null) r.enabled = false;
            DrewWeapon = false;
            if (c == null || myth == null || bodyRenderer == null || !bodyRenderer.enabled) return;

            var facing = body.CurrentFacing;
            foreach (var layer in EquipmentAppearance.Layers(c, ToCardinal(facing)))
            {
                if (!EquipmentAppearance.Visible(layer.Slot, myth.ShownClip)) continue; // weapon only when fighting
                var lib = Library(layer.QualityFamily) ?? Library(layer.Family);
                var clip = lib != null ? lib.Get(myth.ShownClip) : null;
                if (clip == null && lib != null)
                    foreach (var alt in myth.ShownChain)
                        if ((clip = lib.Get(alt)) != null) break;
                if (clip == null) continue;
                var frames = clip.For(facing);
                if (frames == null || frames.Length == 0) continue;
                var sprite = frames[Mathf.Clamp(myth.ShownFrame, 0, frames.Length - 1)];
                if (sprite == null) continue;
                var r = LayerFor(layer.Slot);
                r.sprite = sprite;
                r.flipX = bodyRenderer.flipX;
                r.color = bodyRenderer.color;
                r.sortingLayerID = bodyRenderer.sortingLayerID;
                r.sortingOrder = bodyRenderer.sortingOrder + layer.Order;
                r.enabled = true;
                if (layer.Slot == EquipSlot.Weapon) DrewWeapon = true;
            }
        }
    }
}
