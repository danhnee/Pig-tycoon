using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public static class UISpriteLoader
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, string> filePathIndex = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        private static bool isIndexBuilt = false;

        private static void EnsureFileIndex()
        {
            if (isIndexBuilt) return;
            isIndexBuilt = true;

            string rootPath = Application.dataPath;
            if (!Directory.Exists(rootPath))
            {
                rootPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            }

            if (!Directory.Exists(rootPath)) return;

            try
            {
                var files = Directory.GetFiles(rootPath, "*.png", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (!filePathIndex.ContainsKey(fileName))
                    {
                        filePathIndex[fileName] = file;
                    }

                    string relPath = file.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                    filePathIndex[relPath] = file;
                    filePathIndex["Assets/" + relPath] = file;
                    filePathIndex["Art/" + relPath] = file;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UISpriteLoader] Không thể quét chỉ mục Art: {ex.Message}");
            }
        }

        public static string ResolveFullPath(string relativePathUnderAssets)
        {
            if (string.IsNullOrEmpty(relativePathUnderAssets)) return null;

            string cleanPath = relativePathUnderAssets.Replace('\\', '/').TrimStart('/');
            if (cleanPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(7);
            }

            // 1. Direct path check under Application.dataPath
            string direct1 = Path.Combine(Application.dataPath, cleanPath);
            if (File.Exists(direct1)) return direct1;

            // 2. Direct path check under CurrentDirectory/Assets
            string direct2 = Path.Combine(Directory.GetCurrentDirectory(), "Assets", cleanPath);
            if (File.Exists(direct2)) return direct2;

            // 3. Fallback check via File Index
            EnsureFileIndex();

            if (filePathIndex.TryGetValue(cleanPath, out string indexedPath) && File.Exists(indexedPath))
            {
                return indexedPath;
            }

            string fileName = Path.GetFileName(cleanPath);
            if (filePathIndex.TryGetValue(fileName, out string byFileName) && File.Exists(byFileName))
            {
                return byFileName;
            }

            return null;
        }

        public static Sprite LoadSprite(string relativePathUnderAssets, int pixelsPerUnit = 32, Vector4 border = default)
        {
            return LoadSprite(relativePathUnderAssets, pixelsPerUnit, new Vector2(0.5f, 0.5f), border);
        }

        public static Sprite LoadSprite(string relativePathUnderAssets, int pixelsPerUnit, Vector2 pivot, Vector4 border = default)
        {
            string cacheKey = $"{relativePathUnderAssets}_{pixelsPerUnit}_{pivot.x:F3}_{pivot.y:F3}";
            if (spriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            string fullPath = ResolveFullPath(relativePathUnderAssets);
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                return null;
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
                    pivot,
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    border
                );
                sprite.name = Path.GetFileNameWithoutExtension(relativePathUnderAssets);
                spriteCache[cacheKey] = sprite;
                return sprite;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UISpriteLoader] Không thể nạp sprite {relativePathUnderAssets}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tải sprite vũ khí 24x24 px với PPU 64 và điểm neo (pivot) đặt chính xác tại chuôi kiếm (handle grip).
        /// </summary>
        public static Sprite LoadWeaponSprite(string weaponId)
        {
            string path = $"Art/Sprites/Weapons/weapon_{weaponId}.png";
            // Vũ khí 24x24 px: Chuôi kiếm tại x=11.5, y=3.5 (tọa độ bottom-left trong Unity)
            Vector2 handlePivot = new Vector2(11.5f / 24f, 3.5f / 24f);
            Sprite sp = LoadSprite(path, 64, handlePivot);
            if (sp == null)
            {
                sp = LoadSprite("Art/Sprites/Weapons/weapon_dao_go.png", 64, handlePivot);
            }
            return sp;
        }

        public static Sprite GetToolIcon(StardewToolType tool)
        {
            string fileName = tool switch
            {
                StardewToolType.CamHat => "tool_cam_hat.png",
                StardewToolType.XoNuoc => "tool_xo_nuoc.png",
                StardewToolType.BuaGo => "tool_bua_go.png",
                StardewToolType.HangRao => "item_wood_plank.png",
                StardewToolType.MangAn => "item_feeder.png",
                StardewToolType.MangNuoc => "item_water_trough.png",
                StardewToolType.BanChai => "tool_ban_chai.png",
                StardewToolType.KinhLup => "tool_kinh_lup.png",
                StardewToolType.DaoGo => "tool_dao_go.png",
                StardewToolType.KhuTrung => "tool_khu_trung.png",
                StardewToolType.BachVan => "tool_bach_van.png",
                _ => "tool_cam_hat.png"
            };

            if (fileName.StartsWith("item_"))
            {
                return LoadSprite("Art/UI/Items/" + fileName);
            }
            return LoadSprite("Art/UI/Tools/" + fileName);
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

        public static Sprite GetFrameWoodPanel() => LoadSprite("Art/UI/Frames/frame_wood_panel.png", 32, new Vector4(12, 12, 12, 12));
        public static Sprite GetFrameParchment() => LoadSprite("Art/UI/Frames/frame_parchment.png", 32, new Vector4(10, 10, 10, 10));
        public static Sprite GetFrameSlotNormal() => LoadSprite("Art/UI/Frames/frame_slot_normal.png", 32, new Vector4(8, 8, 8, 8));
        public static Sprite GetFrameSlotSelected() => LoadSprite("Art/UI/Frames/frame_slot_selected.png", 32, new Vector4(10, 10, 10, 10));
        public static Sprite GetFrameSlotHighlight() => GetFrameSlotSelected();
        public static Sprite GetFrameActionButton() => LoadSprite("Art/UI/Frames/frame_action_button.png", 32, new Vector4(16, 16, 16, 16));
        public static Sprite GetJoystickBase() => LoadSprite("Art/UI/Controls/joystick_base.png", 32);
        public static Sprite GetJoystickKnob() => LoadSprite("Art/UI/Controls/joystick_knob.png", 32);
        public static Sprite GetIconGold() => LoadSprite("Art/UI/Icons/icon_coin_gold.png", 32);
        public static Sprite GetIconHeartFull() => LoadSprite("Art/UI/Icons/icon_heart_full.png", 32);
        public static Sprite GetIconHeartEmpty() => LoadSprite("Art/UI/Icons/icon_heart_empty.png", 32);
        public static Sprite GetIconEnergyBolt() => LoadSprite("Art/UI/Icons/icon_energy_bolt.png", 32);
        public static Sprite GetIconCloseCross() => LoadSprite("Art/UI/Icons/icon_close_cross.png", 32);
        public static Sprite GetIconBackpack() => LoadSprite("Art/UI/Tools/tool_balo.png", 32);
        public static Sprite GetIconWoodPlank() => LoadSprite("Art/UI/Items/item_wood_plank.png", 32);
        public static Sprite GetIconFeeder() => LoadSprite("Art/UI/Items/item_feeder.png", 32);
        public static Sprite GetIconWaterTrough() => LoadSprite("Art/UI/Items/item_water_trough.png", 32);
        public static Sprite GetFeederSprite() => LoadSprite("Art/Sprites/Environment/Buildings/feeder_trough.png", 32);
        public static Sprite GetWaterTroughSprite() => LoadSprite("Art/Sprites/Environment/Buildings/water_trough.png", 32);
        public static Sprite GetGridSelector() => LoadSprite("Art/UI/Controls/grid_selector.png", 32);

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
            return LoadSprite("Art/Sprites/Environment/Fences/" + fileName);
        }

        public static Sprite GetLootSprite(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey)) return GetIconWoodPlank();

            // 1. Kiểm tra trực tiếp trong Art/UI/Items/
            Sprite uiSpr = LoadSprite($"Art/UI/Items/{itemKey}.png", 32);
            if (uiSpr != null) return uiSpr;

            // 2. Tra cứu theo từ khóa
            return itemKey switch
            {
                "item_wood_plank" => GetIconWoodPlank(),
                "item_corn_feed" => LoadSprite("Art/UI/Items/item_corn_feed.png", 32),
                "item_rusty_nail" => LoadSprite("Art/UI/Items/item_rusty_nail.png", 32),
                "item_broken_post" => LoadSprite("Art/UI/Items/item_broken_post.png", 32),
                "item_rat_tail" => LoadSprite("Art/UI/Items/item_rat_tail.png", 32),
                "item_fertilizer" => LoadSprite("Art/UI/Items/item_fertilizer.png", 32),
                "item_alloy_scrap" => LoadSprite("Art/UI/Items/item_alloy_scrap.png", 32),
                "item_quantum_fiber" => LoadSprite("Art/UI/Items/item_quantum_fiber.png", 32),
                "item_plasma_core" => LoadSprite("Art/UI/Items/item_plasma_core.png", 32),
                "item_rope" => LoadSprite("Art/UI/Items/item_rope.png", 32),
                "icon_coin_gold" => GetIconGold(),
                _ => GetIconWoodPlank()
            };
        }

        public static Sprite GetFenceDamagedOverlay() => LoadSprite("Art/Sprites/Environment/Fences/fence_damaged_overlay.png", 32);
        public static Sprite GetFenceBrokenSprite() => LoadSprite("Art/Sprites/Environment/Fences/fence_broken.png", 32);
        public static Sprite GetTractorBeamVFX() => LoadSprite("Art/Sprites/VFX/Telegraphs/vfx_tractor_beam.png", 32, new Vector2(0.5f, 0.05f));
        public static Sprite GetTorchFireVFX() => LoadSprite("Art/Sprites/VFX/Telegraphs/vfx_torch_fire.png", 32, new Vector2(0.5f, 0.1f));

        // --- Alien & UFO Ecosystem Sprites ---
        public static Sprite GetUfoMothershipSprite() => LoadSprite("Art/Sprites/Vehicles/ufo_mothership.png", 32);
        public static Sprite GetUfoScoutSprite() => LoadSprite("Art/Sprites/Vehicles/ufo_scout_down.png", 32);
        public static Sprite GetAlienGruntSprite() => LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down.png", 32);
        public static Sprite GetAlienEliteSprite() => LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down.png", 32);
        public static Sprite GetBossAlienWarlordSprite() => LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down.png", 32);
    }
}
