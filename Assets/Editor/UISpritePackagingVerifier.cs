using System;
using PigTycoon.Presentation;
using UnityEditor;
using UnityEngine;

namespace PigTycoon.EditorTools
{
    public static class UISpritePackagingVerifier
    {
        // Invoke through tools/headless_runner.py; this deliberately bypasses raw
        // developer Asset paths to test the resource-loading route used by Android.
        public static void Verify()
        {
            string[] paths =
            {
                "Art/UI/Frames/frame_wood_panel.png",
                "Art/UI/Emotes/emote_hungry.png",
                "Art/Sprites/Characters/NPCs/npc_ba_sau.png",
                "Art/Sprites/Environment/Buildings/shelter_barn.png",
                "Art/Sprites/Environment/Buildings/water_well.png",
                "Art/Sprites/Environment/Buildings/feed_silo.png",
                "Art/Sprites/Environment/Buildings/corpse_lot.png",
                "Art/Sprites/Environment/Buildings/defense_tower.png",
                "Art/Sprites/Environment/Traps/spike_trap_idle.png",
                "Art/Sprites/Environment/Traps/spike_trap_sprung.png"
            };
            foreach (string path in paths)
            {
                Sprite sprite = UISpriteLoader.LoadPackagedSprite(path);
                Require(sprite != null && sprite.texture.width > 2 && sprite.texture.height > 2,
                    "Required packaged source artwork did not load: " + path);
                Require(Mathf.Approximately(sprite.pixelsPerUnit, 32), "PPU changed: " + path);
            }
            const string panel = "Art/UI/Frames/frame_wood_panel.png";
            var noBorder = UISpriteLoader.LoadPackagedSprite(panel);
            var bordered = UISpriteLoader.LoadPackagedSprite(panel, 32, new Vector4(12, 12, 12, 12));
            Require(noBorder != bordered && bordered.border == new Vector4(12, 12, 12, 12),
                "Sprite cache incorrectly aliases different slicing borders.");
            Require(UISpriteLoader.LoadPackagedSprite("Assets/" + panel) == noBorder,
                "Assets path prefix should resolve to the same packaged sprite.");
            Require(UISpriteLoader.LoadPackagedSprite("Art/does-not-exist.png") == null,
                "Unknown artwork must not be replaced by a placeholder sprite.");
            Require(UISpriteLoader.GetTowerSprite("BanChong") == UISpriteLoader.GetSpikeTrapIdleSprite(),
                "Canonical BanChong must bind the existing spike-trap artwork.");
            Debug.Log("UISpritePackagingVerifier PASS: packaged source artwork, 32PPU, border cache isolation, normalized path, missing asset and canonical BanChong binding.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
