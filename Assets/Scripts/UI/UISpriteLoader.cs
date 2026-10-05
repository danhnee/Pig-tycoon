using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public static class UISpriteLoader
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static Sprite LoadSprite(string relativePathUnderAssets, int pixelsPerUnit = 32, Vector4 border = default)
        {
            if (spriteCache.TryGetValue(relativePathUnderAssets, out var cached) && cached != null)
            {
                return cached;
            }

            string fullPath = Path.Combine(Application.dataPath, relativePathUnderAssets);
            if (!File.Exists(fullPath))
            {
                // Fallback nếu chạy ở thư mục khác hoặc unit tests
                string altPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", relativePathUnderAssets);
                if (File.Exists(altPath))
                {
                    fullPath = altPath;
                }
                else
                {
                    return null;
                }
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point; // Giữ pixel-art luôn sắc nét
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.LoadImage(fileData);

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    border
                );
                sprite.name = Path.GetFileNameWithoutExtension(relativePathUnderAssets);
                spriteCache[relativePathUnderAssets] = sprite;
                return sprite;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UISpriteLoader] Không thể nạp sprite {relativePathUnderAssets}: {ex.Message}");
                return null;
            }
        }

        public static Sprite GetToolIcon(StardewToolType tool)
        {
            string fileName = tool switch
            {
                StardewToolType.CamHat => "tool_cam_hat.png",
                StardewToolType.XoNuoc => "tool_xo_nuoc.png",
                StardewToolType.BuaGo => "tool_bua_go.png",
                StardewToolType.BanChai => "tool_ban_chai.png",
                StardewToolType.KinhLup => "tool_kinh_lup.png",
                StardewToolType.DaoGo => "tool_dao_go.png",
                StardewToolType.KhuTrung => "tool_khu_trung.png",
                StardewToolType.BachVan => "tool_bach_van.png",
                _ => "tool_cam_hat.png"
            };
            return LoadSprite("Art/UI/" + fileName);
        }

        public static Sprite GetPigAvatar(GeneLineId geneLine)
        {
            string folder = geneLine switch
            {
                GeneLineId.HongDien => "hong_dien",
                GeneLineId.LamKhe => "lam_khe",
                GeneLineId.KimTho => "kim_tho",
                GeneLineId.HuThe => "hu_the",
                _ => "hong_dien"
            };
            return LoadSprite($"Art/Sprites/Pigs/{folder}/idle.png", 16);
        }

        public static Sprite GetFrameWoodPanel() => LoadSprite("Art/UI/frame_wood_panel.png", 32, new Vector4(12, 12, 12, 12));
        public static Sprite GetFrameParchment() => LoadSprite("Art/UI/frame_parchment.png", 32, new Vector4(10, 10, 10, 10));
        public static Sprite GetFrameSlotNormal() => LoadSprite("Art/UI/frame_slot_normal.png", 32, new Vector4(8, 8, 8, 8));
        public static Sprite GetFrameSlotSelected() => LoadSprite("Art/UI/frame_slot_selected.png", 32, new Vector4(10, 10, 10, 10));
        public static Sprite GetFrameActionButton() => LoadSprite("Art/UI/frame_action_button.png", 32, new Vector4(16, 16, 16, 16));
        public static Sprite GetJoystickBase() => LoadSprite("Art/UI/joystick_base.png", 32);
        public static Sprite GetJoystickKnob() => LoadSprite("Art/UI/joystick_knob.png", 32);
        public static Sprite GetIconGold() => LoadSprite("Art/UI/icon_coin_gold.png", 32);
        public static Sprite GetIconHeartFull() => LoadSprite("Art/UI/icon_heart_full.png", 32);
        public static Sprite GetIconHeartEmpty() => LoadSprite("Art/UI/icon_heart_empty.png", 32);
        public static Sprite GetIconEnergyBolt() => LoadSprite("Art/UI/icon_energy_bolt.png", 32);
        public static Sprite GetIconCloseCross() => LoadSprite("Art/UI/icon_close_cross.png", 32);
        public static Sprite GetIconBackpack() => LoadSprite("Art/UI/tool_balo.png", 32);
        public static Sprite GetIconWoodPlank() => LoadSprite("Art/UI/item_wood_plank.png", 32);
        public static Sprite GetGridSelector() => LoadSprite("Art/UI/grid_selector.png", 32);

        public static Sprite GetFenceSprite(bool north, bool east, bool south, bool west)
        {
            int mask = (north ? 1 : 0) | (east ? 2 : 0) | (south ? 4 : 0) | (west ? 8 : 0);
            return GetFenceSprite(mask);
        }

        public static Sprite GetFenceSprite(int mask)
        {
            string fileName = mask switch
            {
                0  => "fence_post.png",
                1  => "fence_end_n.png",
                2  => "fence_end_e.png",
                3  => "fence_corner_ne.png",
                4  => "fence_end_s.png",
                5  => "fence_v.png",
                6  => "fence_corner_se.png",
                7  => "fence_t_east.png",
                8  => "fence_end_w.png",
                9  => "fence_corner_nw.png",
                10 => "fence_h.png",
                11 => "fence_t_north.png",
                12 => "fence_corner_sw.png",
                13 => "fence_t_west.png",
                14 => "fence_t_south.png",
                15 => "fence_cross.png",
                _  => "fence_post.png"
            };
            return LoadSprite("Art/Sprites/Environment/" + fileName);
        }
    }
}
