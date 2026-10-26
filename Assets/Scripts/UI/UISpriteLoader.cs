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

            // 3. Direct path check under CurrentDirectory
            string direct3 = Path.Combine(Directory.GetCurrentDirectory(), cleanPath);
            if (File.Exists(direct3)) return direct3;

            // 4. Fallback check via File Index
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

            string cleanPath = relativePathUnderAssets.Replace('\\', '/').TrimStart('/');
            if (cleanPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(7);
            }

#if UNITY_EDITOR
            try
            {
                string editorAssetPath = "Assets/" + cleanPath;
                var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(editorAssetPath);
                if (editorSprite == null)
                {
                    var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(editorAssetPath);
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite sp) { editorSprite = sp; break; }
                    }
                }
                if (editorSprite == null && !Application.isPlaying)
                {
                    string projectDir = Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();
                    string diskPath = Path.Combine(projectDir, editorAssetPath);
                    if (File.Exists(diskPath))
                    {
                        UnityEditor.AssetDatabase.ImportAsset(editorAssetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                        editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(editorAssetPath);
                        if (editorSprite == null)
                        {
                            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(editorAssetPath);
                            foreach (var a in allAssets)
                            {
                                if (a is Sprite sp) { editorSprite = sp; break; }
                            }
                        }
                    }
                }
                if (editorSprite == null)
                {
                    var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(editorAssetPath);
                    if (tex != null)
                    {
                        editorSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
                        editorSprite.name = Path.GetFileNameWithoutExtension(cleanPath);
                    }
                }
                if (editorSprite != null)
                {
                    spriteCache[cacheKey] = editorSprite;
                    return editorSprite;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UISpriteLoader] Editor load asset failed for {cleanPath}: {ex.Message}");
            }
#endif

            // Fallback 1: Tải từ Resources (Tương thích chuẩn khi build lên Android / iOS / WebGL)
            string resPath = cleanPath;
            if (resPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                resPath = resPath.Substring(0, resPath.Length - 4);
            if (resPath.StartsWith("Art/", System.StringComparison.OrdinalIgnoreCase))
                resPath = resPath.Substring(4);

            Sprite resSprite = Resources.Load<Sprite>(resPath) ?? 
                               Resources.Load<Sprite>("Art/" + resPath) ?? 
                               Resources.Load<Sprite>(Path.GetFileNameWithoutExtension(cleanPath));
            if (resSprite != null)
            {
                spriteCache[cacheKey] = resSprite;
                return resSprite;
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

        public static Sprite GetVillagerAvatar(VillagerId id)
        {
            string fileName = id switch
            {
                VillagerId.BaSau => "npc_ba_sau.png",
                VillagerId.TamVa => "npc_tam_va.png",
                VillagerId.CoDung => "npc_co_dung.png",
                VillagerId.AnhCa => "npc_anh_ca.png",
                VillagerId.OngTon => "npc_ong_ton.png",
                VillagerId.BiNhan => "npc_bi_nhan.png",
                _ => "npc_ba_sau.png"
            };
            return LoadSprite($"Art/Sprites/Characters/NPCs/{fileName}", 32) ?? LoadSprite("Art/UI/Icons/icon_heart_full.png", 32);
        }

        public static Sprite GetFrameWoodPanel() => LoadSprite("Art/UI/Frames/frame_wood_panel.png", 32, new Vector4(12, 12, 12, 12));
        public static Sprite GetFrameParchment() => LoadSprite("Art/UI/Frames/frame_parchment.png", 32, new Vector4(10, 10, 10, 10));
        public static Sprite GetFrameSlotNormal() => LoadSprite("Art/UI/Frames/frame_slot_normal.png", 32, new Vector4(8, 8, 8, 8));
        public static Sprite GetFrameSlotSelected() => LoadSprite("Art/UI/Frames/frame_slot_selected.png", 32, new Vector4(10, 10, 10, 10));
        public static Sprite GetFrameSlotHighlight() => GetFrameSlotSelected();
        public static Sprite GetFrameActionButton() => LoadSprite("Art/UI/Frames/frame_action_button.png", 32, new Vector4(16, 16, 16, 16));

        private static Sprite solidWhiteSprite;
        public static Sprite GetSolidWhiteSprite()
        {
            if (solidWhiteSprite != null) return solidWhiteSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            tex.name = "ProceduralSolidWhite";
            solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2);
            solidWhiteSprite.name = "SolidWhiteSprite";
            return solidWhiteSprite;
        }
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
        public static Sprite GetBarnSprite() => LoadSprite("Art/Sprites/Environment/Buildings/shelter_barn.png", 32);
        public static Sprite GetWaterWellSprite() => LoadSprite("Art/Sprites/Environment/Buildings/water_well.png", 32);
        public static Sprite GetFeedSiloSprite() => LoadSprite("Art/Sprites/Environment/Buildings/feed_silo.png", 32);
        public static Sprite GetCorpseLotSprite() => LoadSprite("Art/Sprites/Environment/Buildings/corpse_lot.png", 32);
        public static Sprite GetDefenseTowerSprite() => LoadSprite("Art/Sprites/Environment/Buildings/defense_tower.png", 32);
        public static Sprite GetSpikeTrapIdleSprite() => LoadSprite("Art/Sprites/Environment/Traps/spike_trap_idle.png", 32);
        public static Sprite GetSpikeTrapSprungSprite() => LoadSprite("Art/Sprites/Environment/Traps/spike_trap_sprung.png", 32);
        public static Sprite GetCuuTieuLoiPhaoSprite() => LoadSprite("Art/Sprites/Environment/Buildings/cuu_tieu_loi_phao.png", 32);
        public static Sprite GetDefenseAmmoCrateSprite() => LoadSprite("Art/Sprites/Environment/Buildings/defense_ammo_crate.png", 32);
        public static Sprite GetVfxLightningStrikeSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_lightning_strike.png", 32);
        public static Sprite GetSmashImpactVfx() => LoadSprite("Art/Sprites/VFX/Combat/vfx_smash_impact.png", 32);
        public static Sprite GetNailSprite() => LoadSprite("Art/UI/Items/item_rusty_nail.png", 32);
        public static Sprite GetSawbladeSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_sawblade.png", 32) ?? GetNailSprite();
        public static Sprite GetGroundBoneSpikeFrame(int frameIndex)
        {
            int idx = Mathf.Clamp(frameIndex, 0, 3);
            return LoadSprite($"Art/Sprites/VFX/Combat/vfx_ground_bone_spike_{idx}.png", 32) ?? GetSpikeTrapSprungSprite();
        }
        public static Sprite GetFlameStreamFrame(int frameIndex)
        {
            int idx = Mathf.Clamp(frameIndex, 0, 3);
            return LoadSprite($"Art/Sprites/VFX/Combat/vfx_flame_stream_{idx}.png", 32, new Vector2(0.5f, 0.06f)) ?? GetTorchFireVFX();
        }
        public static Sprite GetTarScorchSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_tar_scorch.png", 32) ?? GetSoftCircleSprite();
        public static Sprite GetTarSmokeSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_tar_smoke.png", 32) ?? GetSoftCircleSprite();
        public static Sprite GetSpikeVfxSprite() => GetGroundBoneSpikeFrame(2);
        public static Sprite GetIronBoltSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_iron_bolt.png", 32) ?? GetSpikeVfxSprite();
        public static Sprite GetQuantumOrbSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_quantum_orb.png", 32) ?? GetSoftCircleSprite();
        public static Sprite GetCannonballSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_cannonball.png", 32) ?? GetSoftCircleSprite();
        public static Sprite GetAcidGlobSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_acid_glob.png", 32) ?? GetSoftCircleSprite();
        public static Sprite GetVerminSpineSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_vermin_spine.png", 32) ?? GetSpikeVfxSprite();
        public static Sprite GetHarpoonSpearSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_harpoon_spear.png", 32) ?? GetSpikeVfxSprite();
        public static Sprite GetEmpOrbSprite() => LoadSprite("Art/Sprites/VFX/Combat/vfx_emp_orb.png", 32) ?? GetSoftCircleSprite();

        private static Sprite softCircleSprite;
        public static Sprite GetSoftCircleSprite()
        {
            if (softCircleSprite != null) return softCircleSprite;
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cols = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1f - (dist / radius));
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            tex.name = "ProceduralSoftCircle";
            softCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            softCircleSprite.name = "SoftCircleSprite";
            return softCircleSprite;
        }

        private static Sprite softShadowSprite;
        public static Sprite GetSoftShadowSprite()
        {
            if (softShadowSprite != null) return softShadowSprite;
            int w = 32;
            int h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            float cx = (w - 1) * 0.5f;
            float cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d);
                    alpha = alpha * alpha;
                    cols[y * w + x] = new Color(0f, 0f, 0f, alpha * 0.65f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            tex.name = "ProceduralSoftShadow";
            softShadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
            softShadowSprite.name = "SoftShadowSprite";
            return softShadowSprite;
        }

        public static Sprite GetGridSelector() => LoadSprite("Art/UI/Controls/grid_selector.png", 32);

        public static Sprite GetEmoteSprite(string emoteKey)
        {
            if (string.IsNullOrEmpty(emoteKey)) return null;
            string raw = emoteKey.Trim();
            string key = raw.ToLowerInvariant();

            string fileName = null;

            // 1. Tim / Yêu thương / Tình cảm (<3, ❤️, ❤, ♥, tim, 5 tim, love, heart, \u2764, \u2665)
            if (raw.IndexOf('\u2764') >= 0 || raw.IndexOf('\u2665') >= 0 ||
                raw.Contains("<3") || raw.Contains("❤️") || raw.Contains("❤") || raw.Contains("♥") || raw.Contains("💖") || raw.Contains("💕") ||
                key.Contains("<3") || key.Contains("tim") || key.Contains("heart") || key.Contains("love"))
            {
                Sprite heartSpr = LoadSprite("Art/UI/Emotes/emote_heart.png", 32);
                if (heartSpr != null) return heartSpr;
                return GetIconHeartFull();
            }
            // 2. Đói / Thức ăn / Cám (cám, đói, ăn, cam, doi, hungry, food, feed)
            else if (key == "cám" || key == "cam" || key == "đói" || key == "doi" || key == "ăn" || key == "an" ||
                     key.Contains("hungry") || key.Contains("food") || key.Contains("feed") || key.Contains("cám") || key.Contains("đói"))
            {
                fileName = "emote_hungry.png";
            }
            // 3. Khát / Nước uống (nước, khát, uống, nuoc, khat, uong, thirsty, water)
            else if (key == "nước" || key == "nuoc" || key == "khát" || key == "khat" || key == "uống" || key == "uong" ||
                     key.Contains("thirsty") || key.Contains("water") || key.Contains("nước") || key.Contains("khát"))
            {
                fileName = "emote_thirsty.png";
            }
            // 4. Buồn ngủ / Nghỉ ngơi (zzz, ngủ, buồn ngủ, ngu, sleep, sleepy)
            else if (key == "zzz" || key == "ngủ" || key == "ngu" || key.Contains("sleep") || key.Contains("ngủ") || key.Contains("buồn ngủ"))
            {
                fileName = "emote_sleepy.png";
            }
            // 5. Tắm bùn / Thích bùn (bùn, tắm bùn, bun, mud)
            else if (key == "bùn" || key == "bun" || key.Contains("mud") || key.Contains("bùn") || key.Contains("tắm bùn"))
            {
                fileName = "emote_mud.png";
            }
            // 6. Nghi ngờ / Thắc mắc (?, ??, ???, alert, question, nghi ngờ, tò mò)
            else if (key == "?" || key == "??" || key == "???" || key.Contains("alert") || key.Contains("question") ||
                     key.Contains("nghi ngờ") || key.Contains("tò mò"))
            {
                fileName = "emote_alert.png";
            }
            // 7. Hoảng loạn / Cầu cứu (!, !!, sos, 🆘, panic, scared, sợ, cứu)
            else if (raw.Contains("🆘") || key == "!" || key == "!!" || key == "!!!" || key == "sos" || key == "🆘" ||
                     key.Contains("panic") || key.Contains("scared") || key.Contains("sợ") || key.Contains("cứu"))
            {
                fileName = "emote_panic.png";
            }
            // 8. Bị UFO / Alien bắt cóc (👽, ufo, alien, bắt cóc, bat coc)
            else if (raw.Contains("👽") || key == "👽" || key.Contains("ufo") || key.Contains("alien") || key.Contains("bắt cóc") || key.Contains("bat coc"))
            {
                fileName = "emote_alien.png";
            }
            // 9. Vui vẻ / No nê / Khỏe mạnh (no nê, khỏe mạnh, no, khỏe, happy, star, ✨, vui)
            else if (raw.Contains("✨") || key.Contains("no nê") || key.Contains("no ne") || key == "no" ||
                     key.Contains("khỏe mạnh") || key.Contains("khoe manh") || key.Contains("khỏe") || key.Contains("khoe") ||
                     key.Contains("happy") || key.Contains("star") || key == "✨" || key.Contains("vui"))
            {
                fileName = "emote_happy.png";
            }
            // 10. Chờ đợi / Suy nghĩ (..., dots, wait, chờ)
            else if (key == "..." || key.Contains("dots") || key.Contains("wait") || key.Contains("chờ"))
            {
                fileName = "emote_dots.png";
            }
            // Direct emote file name
            else if (key.StartsWith("emote_"))
            {
                fileName = key.EndsWith(".png") ? key : key + ".png";
            }

            if (fileName != null)
            {
                return LoadSprite("Art/UI/Emotes/" + fileName, 32);
            }

            if (key.EndsWith(".png"))
            {
                return LoadSprite("Art/UI/Emotes/" + key, 32);
            }

            return null;
        }

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

        public static Sprite GetBlueprintSprite(string blueprintId = null)
        {
            Sprite sp = LoadSprite("Art/UI/Items/item_blueprint.png", 32);
            if (sp != null) return sp;
            return GetFrameParchment();
        }

        public static Sprite GetTowerSprite(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return GetDefenseTowerSprite();

            string customPath = $"Art/Sprites/Environment/Buildings/defense_{blueprintId.ToLowerInvariant()}.png";
            Sprite sp = LoadSprite(customPath, 32);
            if (sp != null) return sp;

            return blueprintId switch
            {
                // 1. Cơ Bản & Bạch Vân
                "NoXuyenVan" => GetBachVanTowerFrame("NoXuyenVan", 0),
                "BayChongGo" => GetBachVanTowerFrame("BayChongGo", 0),
                "CuuTieuLoiPhao" => GetBachVanTowerFrame("CuuTieuLoiPhao", 0),
                "ThienNhanVongDai" => GetBachVanTowerFrame("ThienNhanVongDai", 0),
                "VanLinhHoiNguyenTran" => GetBachVanTowerFrame("VanLinhHoiNguyenTran", 0),

                // 2. Alien Tech (Dedicated 16-bit Pixel Art Assets)
                "TruPlasmaHoiTu" => LoadSprite("Art/Sprites/Environment/Buildings/AlienTowers/TruPlasmaHoiTu_idle.png", 32) ?? GetDefenseTowerSprite(),
                "PhaoCoiLuongTu" => LoadSprite("Art/Sprites/Environment/Buildings/AlienTowers/PhaoCoiLuongTu_idle.png", 32) ?? GetDefenseTowerSprite(),
                "ThapPhanTrongLuc" => LoadSprite("Art/Sprites/Environment/Buildings/AlienTowers/ThapPhanTrongLuc_idle.png", 32) ?? GetDefenseTowerSprite(),
                "TruXungDienEMP" => LoadSprite("Art/Sprites/Environment/Buildings/AlienTowers/TruXungDienEMP_idle.png", 32) ?? GetDefenseTowerSprite(),
                "PhaoDaiPhanVatChat" => LoadSprite("Art/Sprites/Environment/Buildings/AlienTowers/PhaoDaiPhanVatChat_idle.png", 32) ?? GetDefenseTowerSprite(),

                // 3. Bandit Scrap
                "NoLienChauThep" => GetBanditTowerFrame("NoLienChauThep", 0),
                "MayBanRiuCua" => GetBanditTowerFrame("MayBanRiuCua", 0),
                "ThapPhunLuaHacIn" => GetBanditTowerFrame("ThapPhunLuaHacIn", 0),
                "PhaoLaoMocXich" => GetBanditTowerFrame("PhaoLaoMocXich", 0),
                "DaiBacChuaTrom" => GetBanditTowerFrame("DaiBacChuaTrom", 0),

                // 4. Bio & Mystic
                "UGaiDocGamNhom" => GetBioTowerFrame("UGaiDocGamNhom", 0),
                "TruNamBaoTuMu" => GetBioTowerFrame("TruNamBaoTuMu", 0),
                "OngBomDichAxit" => GetBioTowerFrame("OngBomDichAxit", 0),
                "CotToHutHon" => GetBioTowerFrame("CotToHutHon", 0),
                "CotDiCotSuongHuyen" => GetBioTowerFrame("CotDiCotSuongHuyen", 0),

                _ => GetDefenseTowerSprite()
            };
        }

        /// <summary>
        /// Nạp frame sprite cụ thể cho tháp ngoài hành tinh (0=idle, 1=charge, 2=fire, 3=cooldown)
        /// </summary>
        public static Sprite GetAlienTowerFrame(string blueprintId, int frameIndex)
        {
            if (string.IsNullOrEmpty(blueprintId)) return GetDefenseTowerSprite();
            string suffix = frameIndex switch
            {
                1 => "charge",
                2 => "fire",
                3 => "cooldown",
                _ => "idle"
            };
            string path = $"Art/Sprites/Environment/Buildings/AlienTowers/{blueprintId}_{suffix}.png";
            Sprite sp = LoadSprite(path, 32);
            if (sp != null) return sp;
            return GetTowerSprite(blueprintId);
        }

        /// <summary>
        /// Nạp frame sprite cụ thể cho tháp tạp phế phe Băng Cướp (0=idle, 1=charge, 2=fire, 3=cooldown)
        /// </summary>
        public static Sprite GetBanditTowerFrame(string blueprintId, int frameIndex)
        {
            if (string.IsNullOrEmpty(blueprintId)) return GetDefenseTowerSprite();
            string suffix = frameIndex switch
            {
                1 => "charge",
                2 => "fire",
                3 => "cooldown",
                _ => "idle"
            };
            string path = $"Art/Sprites/Environment/Buildings/BanditTowers/{blueprintId}_{suffix}.png";
            Sprite sp = LoadSprite(path, 32);
            if (sp != null) return sp;
            return GetDefenseTowerSprite();
        }

        /// <summary>
        /// Nạp frame sprite cụ thể cho tháp Dịch Họa & Huyền Bí (0=idle, 1=charge, 2=fire, 3=cooldown)
        /// </summary>
        public static Sprite GetBioTowerFrame(string blueprintId, int frameIndex)
        {
            if (string.IsNullOrEmpty(blueprintId)) return GetDefenseTowerSprite();
            string suffix = frameIndex switch
            {
                1 => "charge",
                2 => "fire",
                3 => "cooldown",
                _ => "idle"
            };
            string path = $"Art/Sprites/Environment/Buildings/BioTowers/{blueprintId}_{suffix}.png";
            Sprite sp = LoadSprite(path, 32);
            if (sp != null) return sp;
            return GetDefenseTowerSprite();
        }

        /// <summary>
        /// Nạp frame sprite cụ thể cho tháp Bạch Vân & Cơ Bản (0=idle, 1=charge, 2=fire, 3=cooldown)
        /// </summary>
        public static Sprite GetBachVanTowerFrame(string blueprintId, int frameIndex)
        {
            if (string.IsNullOrEmpty(blueprintId)) return GetDefenseTowerSprite();
            string suffix = frameIndex switch
            {
                1 => "charge",
                2 => "fire",
                3 => "cooldown",
                _ => "idle"
            };
            string path = $"Art/Sprites/Environment/Buildings/BachVanTowers/{blueprintId}_{suffix}.png";
            Sprite sp = LoadSprite(path, 32);
            if (sp != null) return sp;
            return GetDefenseTowerSprite();
        }

        public static Sprite GetLootSprite(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey)) return GetIconWoodPlank();

            if (itemKey.StartsWith("item_blueprint_"))
            {
                return GetBlueprintSprite(itemKey);
            }

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
                "item_quantum_chip" => LoadSprite("Art/UI/Items/item_quantum_chip.png", 32),
                "item_mutant_corn" => LoadSprite("Art/UI/Items/item_mutant_corn.png", 32),
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

        // --- Bandit & Outlaw Ecosystem Sprites ---
        public static Sprite GetThiefSprite() => LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_down.png", 32) ?? LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_down.png", 32);
        public static Sprite GetBreakerSprite() => LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_down.png", 32) ?? LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_down.png", 32);
        public static Sprite GetPestSprite() => LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_down.png", 32) ?? LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_down.png", 32);
        public static Sprite GetDauLinhTromSprite() => LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_down.png", 32);
        public static Sprite GetBossChuaTromSprite() => LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_down.png", 32);
        public static Sprite GetBanditOutpostSprite() => LoadSprite("Art/Sprites/Environment/Buildings/building_storage_lvl1.png", 32);
    }
}
