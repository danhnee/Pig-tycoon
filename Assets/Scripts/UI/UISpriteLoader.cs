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
        private static Sprite solidWhiteSprite;
        private static Sprite softCircleSprite;
        private static Sprite softShadowSprite;
        private static Dictionary<string, string> packagedPaths;
        private const string ResourcePrefix = "PigTycoonArt/";

        [System.Serializable]
        private sealed class PackagedArtCatalog
        {
            public string[] paths;
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            string clean = path.Replace('\\', '/').TrimStart('/');
            if (clean.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(7);
            return clean;
        }

        private static byte[] LoadPackagedBytes(string path)
        {
            string clean = NormalizeAssetPath(path);
            if (clean == null) return null;
            if (packagedPaths == null)
            {
                packagedPaths = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
                TextAsset catalogAsset = Resources.Load<TextAsset>(ResourcePrefix + "art-index");
                if (catalogAsset == null)
                {
                    packagedPaths = null;
                    return null;
                }
                PackagedArtCatalog catalog = JsonUtility.FromJson<PackagedArtCatalog>(catalogAsset.text);
                if (catalog == null || catalog.paths == null)
                    throw new System.InvalidOperationException("PigTycoonArt/art-index is not a valid PNG catalog.");
                foreach (string entry in catalog.paths) packagedPaths.Add(entry, entry);
                foreach (string entry in catalog.paths)
                {
                    string fileName = Path.GetFileName(entry);
                    if (!packagedPaths.ContainsKey(fileName)) packagedPaths.Add(fileName, entry);
                }
                Resources.UnloadAsset(catalogAsset);
            }
            if (!packagedPaths.TryGetValue(clean, out string canonical)) return null;
            string key = ResourcePrefix + canonical.Substring(0, canonical.Length - 4);
            TextAsset bytesAsset = Resources.Load<TextAsset>(key);
            if (bytesAsset == null)
                throw new System.IO.FileNotFoundException("Packaged PNG declared by art-index is missing: " + canonical);
            byte[] bytes = bytesAsset.bytes;
            Resources.UnloadAsset(bytesAsset);
            return bytes;
        }

        // Raw PNGs loaded at runtime are copied beside the player data by PlayableDemoBuild.
        // Built players must not depend on a developer's working directory or Assets folder.
        public static string ArtRootPath => Application.isEditor
            ? Application.dataPath
            : Path.Combine(Application.dataPath, "PigTycoonArt");

        private static void EnsureFileIndex()
        {
            if (isIndexBuilt) return;
            isIndexBuilt = true;

            string rootPath = ArtRootPath;

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

            // 1. Exact path in the editor Assets tree or the standalone art package.
            string direct1 = Path.Combine(ArtRootPath, cleanPath);
            if (File.Exists(direct1)) return direct1;

            // 2. Fallback check via File Index
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
            return LoadSpriteInternal(relativePathUnderAssets, pixelsPerUnit, pivot, border, false);
        }

        // Used by the build verifier to exercise the same APK-safe path inside the editor.
        public static Sprite LoadPackagedSprite(string assetPath, int pixelsPerUnit = 32, Vector4 border = default)
        {
            return LoadSpriteInternal(assetPath, pixelsPerUnit, new Vector2(0.5f, 0.5f), border, true);
        }

        private static Sprite LoadSpriteInternal(string path, int pixelsPerUnit, Vector2 pivot, Vector4 border, bool packagedOnly)
        {
            if (NormalizeAssetPath(path) == null) return null;
            string cacheKey = System.FormattableString.Invariant($"{NormalizeAssetPath(path)}|{pixelsPerUnit}|{pivot.x:R}|{pivot.y:R}|{border.x:R}|{border.y:R}|{border.z:R}|{border.w:R}|{packagedOnly}");
            if (spriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            try
            {
                byte[] fileData = packagedOnly || !Application.isEditor ? LoadPackagedBytes(path) : null;
                if (fileData == null && !packagedOnly)
                {
                    string fullPath = ResolveFullPath(path);
                    if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath)) fileData = File.ReadAllBytes(fullPath);
                    else if (Application.isEditor) fileData = LoadPackagedBytes(path);
                }
                if (fileData == null) return null;
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point; // Giữ pixel-art luôn sắc nét
                texture.wrapMode = TextureWrapMode.Clamp;
                if (!texture.LoadImage(fileData))
                {
                    Object.Destroy(texture);
                    throw new System.IO.InvalidDataException("PNG data could not be decoded: " + path);
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    pivot,
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    border
                );
                sprite.name = Path.GetFileNameWithoutExtension(path);
                spriteCache[cacheKey] = sprite;
                return sprite;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UISpriteLoader] Không thể nạp sprite {path}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tải sprite vũ khí 24x24 px với PPU 64 và điểm neo (pivot) đặt chính xác tại chuôi kiếm (handle grip).
        /// </summary>
        public static Sprite LoadWeaponSprite(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) weaponId = "wpn_dao_rua";
            string path = $"Art/Sprites/Weapons/{weaponId}.png";
            Vector2 handlePivot = new Vector2(11.5f / 24f, 3.5f / 24f);
            Sprite sp = LoadSprite(path, 64, handlePivot);
            if (sp == null)
            {
                sp = LoadSprite($"Art/Sprites/Weapons/weapon_{weaponId}.png", 64, handlePivot);
            }
            if (sp == null)
            {
                sp = LoadSprite("Art/Sprites/Weapons/wpn_dao_rua.png", 64, handlePivot)
                    ?? LoadSprite("Art/Sprites/Weapons/weapon_dao_go.png", 64, handlePivot);
            }
            return sp;
        }

        public static Sprite GetSkillIcon(string skillId, int size = 24)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            string stem = skillId.StartsWith("pig.skill.", System.StringComparison.OrdinalIgnoreCase)
                ? skillId.Substring(10)
                : skillId;
            return LoadSprite($"Art/PixelMyth/UI/Icons/skills/{stem}_{size}.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/skills/{skillId}_{size}.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/skills/{stem}_24.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/skills/{stem}_16.png", 32);
        }

        public static string GetQualityColorHex(ItemTier tier) => tier switch
        {
            ItemTier.II  => "#4ADE80",
            ItemTier.III => "#60A5FA",
            ItemTier.IV  => "#C084FC",
            _            => "#E2E8F0",
        };

        /// <summary>Dark slot background per quality (the bright rim colour is <see cref="GetQualityFrameColor"/>).</summary>
        public static Color GetQualitySlotBackground(ItemTier tier) => tier switch
        {
            ItemTier.II  => new Color(0.16f, 0.36f, 0.22f, 0.96f),
            ItemTier.III => new Color(0.16f, 0.28f, 0.46f, 0.96f),
            ItemTier.IV  => new Color(0.38f, 0.18f, 0.48f, 0.98f),
            _            => new Color(0.32f, 0.24f, 0.17f, 0.95f),
        };

        public static Sprite GetEquipIcon(string itemId, int size = 24)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            int hashIdx = itemId.IndexOf('#');
            if (hashIdx >= 0) itemId = itemId.Substring(0, hashIdx);
            return LoadSprite($"Art/PixelMyth/UI/Icons/equip/{itemId}_{size}.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/weapons/{itemId}_{size}.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/equip/{itemId}_24.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/equip/{itemId}_16.png", 32);
        }

        public static Sprite GetQuickItemIcon(QuickItemId kind, int size = 16)
        {
            string stem = kind switch
            {
                QuickItemId.BinhHoiLuc => "binh_hoi_luc",
                QuickItemId.BangGac    => "bang_gac",
                QuickItemId.LuuKhoi    => "luu_khoi",
                QuickItemId.BomKeo     => "bom_keo",
                QuickItemId.PhaoSang   => "phao_sang",
                QuickItemId.TinhNoDan  => "tinh_no_dan",
                _                      => "binh_hoi_luc"
            };
            return LoadSprite($"Art/PixelMyth/UI/Icons/quick/{stem}_{size}.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/equip/{stem}_{size}.png", 32)
                ?? LoadSprite($"Art/PixelMyth/UI/Icons/quick/{stem}_16.png", 32);
        }

        public static Sprite LoadWeaponOverlayFrame(string weaponId, CharacterFacing facing, int frameIndex)
        {
            if (string.IsNullOrEmpty(weaponId)) weaponId = "wpn_dao_rua";
            if (!weaponId.StartsWith("wpn_", System.StringComparison.OrdinalIgnoreCase))
            {
                weaponId = weaponId switch
                {
                    "dao_go" or "dao_rua" => "wpn_dao_rua",
                    "giao_tre" or "giao_moc" => "wpn_giao_moc",
                    "bua_ta" => "wpn_bua_ta",
                    "no_tay" => "wpn_no_tay",
                    _ => "wpn_dao_rua"
                };
            }
            int row = facing switch
            {
                CharacterFacing.Left => 1,
                CharacterFacing.Up => 2,
                CharacterFacing.Right or CharacterFacing.Side => 3,
                _ => 0
            };
            int col = Mathf.Clamp(frameIndex, 0, 5);
            string cacheKey = $"wpn_ovl:{weaponId}:{row}:{col}";
            if (spriteCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;

            Sprite sheet = LoadSprite($"Art/PixelMyth/Characters/{weaponId}/{weaponId}_attack.png", 32);
            if (sheet == null || sheet.texture == null) return null;
            var tex = sheet.texture;
            int cw = tex.width / 6;
            int ch = tex.height / 4;
            if (cw <= 0 || ch <= 0) return null;
            // Texture y=0 is bottom row ("right"=row 3), top row is "down"=row 0
            int texY = (3 - row) * ch;
            int texX = col * cw;
            var sp = Sprite.Create(tex, new Rect(texX, texY, cw, ch), new Vector2(38f / 76f, 6f / 80f), 32f);
            sp.name = $"{weaponId}_atk_{row}_{col}";
            spriteCache[cacheKey] = sp;
            return sp;
        }

        /// <summary>
        /// Loads cell (row=0 Down, col=0) of the unarmed protagonist base sheet (`khoa_idle` or `an_idle`)
        /// at native 72x84 pixel resolution so C-panel Paper-Doll gear layers (`GetHeroPaperDollGearLayerSprite`)
        /// align pixel-for-pixel on top of the base body (`2026-11-03-paperdoll-gear-art-spec.md`).
        /// </summary>
        public static Sprite GetHeroPaperDollBaseSprite(PlayerId id)
        {
            string fam = EquipmentAppearance.BaseBodyFamily(id);
            return LoadCharacterClipCell(fam, "idle", 0, 0, 4, 4);
        }

        /// <summary>
        /// Loads cell (row=0 Down, col=0) of an equipped GDD 12.3 Paper-Doll gear layer (`khoa__<itemId>__III`
        /// with automatic fallback to `khoa__<itemId>`) so each equipped slot renders independently on the C-panel.
        /// </summary>
        public static Sprite GetHeroPaperDollGearLayerSprite(GearLayer layer)
        {
            if (string.IsNullOrEmpty(layer.Family)) return null;
            if (!string.IsNullOrEmpty(layer.QualityFamily) && layer.QualityFamily != layer.Family)
            {
                Sprite qSp = LoadCharacterClipCell(layer.QualityFamily, "idle", 0, 0, 4, 4);
                if (qSp != null) return qSp;
            }
            return LoadCharacterClipCell(layer.Family, "idle", 0, 0, 4, 4);
        }

        public static Color GetQualityFrameColor(ItemTier q) => q switch
        {
            ItemTier.IV => new Color(1.00f, 0.82f, 0.28f, 1f), // Amber-Gold Masterwork
            ItemTier.III => new Color(0.46f, 0.78f, 1.00f, 1f), // Damascus Blue-Steel
            ItemTier.II => new Color(0.56f, 0.88f, 0.52f, 1f), // Honed Emerald-Brass
            _ => new Color(0.76f, 0.70f, 0.60f, 1f),              // Standard Iron/Wood
        };

        public static string QualityRoman(ItemTier q) => q switch
        {
            ItemTier.IV => "IV",
            ItemTier.III => "III",
            ItemTier.II => "II",
            _ => "I",
        };

        public static Sprite GetHeroPaperDoll25D(PlayerId id, bool combatPose = false, int eqTier = 0)
        {
            string key = id == PlayerId.An ? "an" : "khoa";
            string suffix = combatPose ? "combat_25d" : "paperdoll_25d";
            Sprite hd = LoadSprite($"Art/PixelMyth/UI/Portraits/{key}_{suffix}.png", 32);
            if (hd != null && hd.texture != null)
            {
                hd.texture.filterMode = FilterMode.Bilinear;
                return hd;
            }
            Sprite port = GetPortrait(key, "normal");
            if (port != null) return port;
            return LoadCharacterCellZero(key);
        }

        public static Sprite LoadCharacterClipCell(string folder, string clipName, int row, int col, int totalRows = 4, int totalCols = 4)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(clipName)) return null;
            string cacheKey = $"char_clip_cell:{folder}:{clipName}:{row}:{col}:{totalRows}:{totalCols}";
            if (spriteCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;

            Sprite sheet = LoadSprite($"Art/PixelMyth/Characters/{folder}/{folder}_{clipName}.png", 32);
            if (sheet == null || sheet.texture == null) return null;
            var tex = sheet.texture;
            // Auto-detect exact 72x84 or 72x68 cell grid so 6-col / 8-col sheets never slice 2 characters wide
            int actualCols = (tex.width % 72 == 0) ? Mathf.Max(1, tex.width / 72) : Mathf.Max(1, totalCols);
            int actualRows = (tex.height % 84 == 0) ? Mathf.Max(1, tex.height / 84)
                           : ((tex.height % 68 == 0) ? Mathf.Max(1, tex.height / 68) : Mathf.Max(1, totalRows));
            int cw = tex.width / actualCols;
            int ch = tex.height / actualRows;
            if (cw <= 0 || ch <= 0) return null;
            int r = Mathf.Clamp(row, 0, actualRows - 1);
            int c = Mathf.Clamp(col, 0, actualCols - 1);
            int texY = (actualRows - 1 - r) * ch;
            int texX = c * cw;
            var sp = Sprite.Create(tex, new Rect(texX, texY, cw, ch), new Vector2(0.5f, 5f / ch), 32f);
            sp.name = $"{folder}_{clipName}_{r}_{c}";
            spriteCache[cacheKey] = sp;
            return sp;
        }

        public static Sprite GetPortrait(string actorId, string emotion = "neutral")
        {
            if (string.IsNullOrWhiteSpace(actorId)) actorId = "an";
            string cleanActor = actorId.Trim();
            string lowerActor = cleanActor.ToLowerInvariant();
            if (lowerActor == "hero_an" || lowerActor == "player_an") lowerActor = "an";
            else if (lowerActor == "hero_khoa" || lowerActor == "player_khoa") lowerActor = "khoa";
            else if (lowerActor == "co_dung" || lowerActor == "bac_si_dung") lowerActor = "dung";
            else if (lowerActor == "cho_dem") lowerActor = "do";

            string cleanEmo = string.IsNullOrWhiteSpace(emotion) ? "neutral" : emotion.Trim().ToLowerInvariant();
            cleanEmo = cleanEmo switch
            {
                "normal" => "neutral",
                "over_bright_smile" => "too_bright",
                "long_stare" => "stare",
                "silent_head_tilt" => "silent_tilt",
                "thoughtful" => "thinking",
                _ => cleanEmo
            };

            // Story 2.4 (#125 item 3): Khoa's face never changes in Season 1 outside reflections (reflect_frown/smile/cry)
            if (lowerActor == "khoa" && !cleanEmo.StartsWith("reflect_", System.StringComparison.Ordinal))
            {
                cleanEmo = "neutral";
            }

            string[] candidates =
            {
                $"Art/PixelMyth/UI/Portraits/{lowerActor}_{cleanEmo}.png",
                $"Art/PixelMyth/UI/Portraits/portrait_{lowerActor}_{cleanEmo}.png",
                $"Art/PixelMyth/UI/Portraits/{lowerActor}_neutral.png",
                $"Art/PixelMyth/UI/Portraits/portrait_{lowerActor}.png",
                $"Art/PixelMyth/UI/Portraits/portrait_{cleanActor}.png",
            };

            foreach (string rel in candidates)
            {
                Sprite sp = LoadSprite(rel, 32);
                if (sp != null && sp.texture != null)
                {
                    sp.texture.filterMode = FilterMode.Bilinear;
                    return sp;
                }
            }
            return LoadCharacterCellZero(lowerActor);
        }

        private static Sprite cachedNeutralSilhouettePortrait;
        /// <summary>
        /// Fallback neutral dark-toned silhouette bust (`#143` item 3) when an NPC portrait is missing
        /// so `VnStoryDialoguePopup` never renders a white square (`sprite == null`).
        /// </summary>
        public static Sprite GetNeutralSilhouettePortrait()
        {
            if (cachedNeutralSilhouettePortrait != null) return cachedNeutralSilhouettePortrait;
            const int w = 48, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[w * h];
            float cx = w * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Head circle around (24, 43), r = 10; shoulders ellipse around (24, 14), rx = 18, ry = 22
                    float dHead = ((x - cx) * (x - cx)) / (9.5f * 9.5f) + ((y - 43f) * (y - 43f)) / (10.5f * 10.5f);
                    float dBody = ((x - cx) * (x - cx)) / (17.5f * 17.5f) + ((y - 12f) * (y - 12f)) / (22.0f * 22.0f);
                    float dNeck = (Mathf.Abs(x - cx) <= 4.5f && y >= 26 && y <= 36) ? 0.5f : 2.0f;
                    float d = Mathf.Min(dHead, Mathf.Min(dBody, dNeck));
                    if (d <= 1.0f)
                    {
                        bool rim = d > 0.82f;
                        px[y * w + x] = rim
                            ? new Color32(82, 68, 54, 235)
                            : new Color32(28, 24, 32, 225);
                    }
                    else
                    {
                        px[y * w + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            cachedNeutralSilhouettePortrait = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            cachedNeutralSilhouettePortrait.name = "neutral_silhouette_portrait";
            return cachedNeutralSilhouettePortrait;
        }

        public static Sprite GetEventBanner(MysteryEventId eventId)
        {
            string code = eventId.ToString();
            Sprite sp = LoadSprite($"Art/PixelMyth/UI/Events/event_{code}.png", 32)
                     ?? LoadSprite("Art/PixelMyth/UI/Events/event_E01.png", 32);
            if (sp != null && sp.texture != null)
            {
                sp.texture.filterMode = FilterMode.Bilinear;
            }
            return sp;
        }

        private static readonly (string prefix, string baseId)[] BackdropBaseRules =
        {
            ("bg_nen_tinh_ngay_mat", "bg_ngay_mat"), ("bg_nen_den", "bg_ngay_mat"), ("bg_the_chuong", "bg_ngay_mat"),
            ("bg_nha_cu_bep", "bg_nha_cu_bep_2018"), ("bg_bep_me", "bg_nha_cu_bep_2018"),
            ("bg_nha_cu_nha_tam", "bg_phong_tro_guong"), ("bg_guong", "bg_phong_tro_guong"), ("bg_phong_tro_guong", "bg_phong_tro_guong"),
            ("bg_nha_cu_cau_thang", "bg_nha_cu_cau_thang"), ("bg_cau_thang", "bg_nha_cu_cau_thang"), ("bg_bau_cua", "bg_nha_cu_cau_thang"),
            ("bg_nha_cu_hien", "bg_nha_cu_hien"), ("bg_hien_nha", "bg_nha_cu_hien"), ("bg_nha_cu_cong", "bg_nha_cu_hien"), ("bg_gieng", "bg_nha_cu_hien"),
            ("bg_nha_cu", "bg_nha_cu_phong_khach"),
            ("bg_hem", "bg_hem_nha_cu"), ("bg_tiem", "bg_hem_nha_cu"),
            ("bg_chua", "bg_chua"), ("bg_san_chua", "bg_chua"), ("bg_bac_da_chua", "bg_chua"),
            ("bg_goc_pho", "bg_goc_pho"), ("bg_nga_tu", "bg_goc_pho"), ("bg_tru_so", "bg_goc_pho"),
            ("bg_so_sau_ngay_mat", "bg_nghia_trang"), ("bg_phieu_thu_suat_mo", "bg_nghia_trang"), ("bg_nghia_trang", "bg_nghia_trang"),
            ("bg_so_bia_vai", "bg_hop_thiec"), ("bg_so_", "bg_so_tay"), ("bg_bien_ban", "bg_so_tay"), ("bg_vi_giay", "bg_so_tay"),
            ("bg_la_thu", "bg_so_tay"), ("bg_ho_so_ban_giao", "bg_so_tay"), ("bg_giay_bao", "bg_so_tay"),
            ("bg_phong_ngu", "bg_phong_ngu"),
            ("bg_benh_vien", "bg_benh_vien"), ("bg_phong_benh", "bg_benh_vien"), ("bg_ho_so_benh_an", "bg_benh_vien"), ("bg_hanh_lang", "bg_benh_vien"),
            ("bg_phong_tro", "bg_phong_tro"), ("bg_van_phong", "bg_phong_tro"),
            ("bg_tranh_gia_dinh_em", "bg_tranh_gia_dinh_em"),
            ("bg_hop_thiec", "bg_hop_thiec"), ("bg_manh_da", "bg_hop_thiec"), ("bg_vong_co", "bg_hop_thiec"), ("bg_anh_", "bg_hop_thiec"),
            ("bg_co_tay", "bg_hop_thiec"), ("bg_tui_ao", "bg_hop_thiec"),
            ("bg_xe_", "bg_xe_buyt"), ("bg_duong_", "bg_xe_buyt"),
            ("bg_bien_bo_ria", "bg_bien_bo_ria"),
            ("bg_trai_cong", "bg_cong_trai"), ("bg_cong_trai", "bg_cong_trai"),
            ("bg_dong_", "bg_trai_chang_vang"), ("bg_trai_", "bg_trai_chang_vang"),
            ("bg_phong_canh_bep_lo", "bg_phong_bep_lo"), ("bg_phong_bep_lo", "bg_phong_bep_lo"), ("bg_thanh_lo", "bg_phong_bep_lo"),
            ("bg_hoc_tuong", "bg_hoc_tuong_bay_cot"),
        };

        public static string ResolveBaseBackdropId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "";
            string clean = id.Trim().ToLowerInvariant();
            string bestBase = clean;
            int bestLen = -1;
            for (int i = 0; i < BackdropBaseRules.Length; i++)
            {
                var rule = BackdropBaseRules[i];
                if (clean.StartsWith(rule.prefix, System.StringComparison.Ordinal) && rule.prefix.Length > bestLen)
                {
                    bestLen = rule.prefix.Length;
                    bestBase = rule.baseId;
                }
            }
            return bestBase;
        }

        public static Sprite GetBackdrop(string backdropId)
        {
            if (string.IsNullOrWhiteSpace(backdropId)) return null;
            string clean = backdropId.Trim().ToLowerInvariant();
            string baseId = ResolveBaseBackdropId(clean);
            string[] candidates =
            {
                $"Art/PixelMyth/UI/Backdrops/{clean}.png",
                $"Art/PixelMyth/UI/Backdrops/{baseId}.png",
                "Art/PixelMyth/UI/Backdrops/bg_trai_chang_vang.png",
            };
            foreach (string rel in candidates)
            {
                Sprite sp = LoadSprite(rel, 32);
                if (sp != null && sp.texture != null)
                {
                    sp.texture.filterMode = FilterMode.Bilinear;
                    return sp;
                }
            }
            return null;
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

        public static Sprite LoadCharacterCellZero(string charFolder)
        {
            if (string.IsNullOrEmpty(charFolder)) return null;
            string cacheKey = "char_cell0:" + charFolder;
            if (spriteCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;

            string[] candidates =
            {
                $"Art/PixelMyth/Characters/{charFolder}/{charFolder}_idle.png",
                $"Art/PixelMyth/Characters/argon_{charFolder}/argon_{charFolder}_idle.png",
            };
            foreach (var rel in candidates)
            {
                Sprite full = LoadSprite(rel, 32);
                if (full != null && full.texture != null && full.texture.width >= 48 && full.texture.height >= 40)
                {
                    var tex = full.texture;
                    int cw = (tex.width % 72 == 0) ? 72 : (tex.width / 4);
                    int ch = (tex.height % 84 == 0) ? 84 : ((tex.height % 68 == 0) ? 68 : (tex.height / 4));
                    var sp = Sprite.Create(tex, new Rect(0, tex.height - ch, cw, ch), new Vector2(0.5f, 0.5f), 32f);
                    sp.name = charFolder + "_portrait";
                    spriteCache[cacheKey] = sp;
                    return sp;
                }
            }
            return null;
        }

        public static Sprite GetPigAvatar(GeneLineId geneLine)
        {
            string folder = geneLine switch
            {
                GeneLineId.HongDien  => "hong_dien",
                GeneLineId.LamKhe    => "lam_khe",
                GeneLineId.MocCuoc   => "moc_cuoc",
                GeneLineId.ThoTram   => "tho_tram",
                GeneLineId.XichMao   => "xich_mao",
                GeneLineId.PhongMau  => "phong_mau",
                GeneLineId.ThietBi   => "thiet_bi",
                GeneLineId.TinhQuang => "tinh_quang",
                GeneLineId.LoiMach   => "loi_mach",
                GeneLineId.MongNhan  => "mong_nhan",
                GeneLineId.KimTho    => "kim_tho",
                GeneLineId.HuThe     => "hu_the",
                _                    => "hong_dien"
            };
            Sprite pm = LoadCharacterCellZero("pig_" + folder);
            if (pm != null) return pm;
            return LoadSprite($"Art/Sprites/Pigs/{folder}/idle.png", 16)
                ?? LoadSprite("Art/Sprites/Pigs/hong_dien/idle.png", 16);
        }

        public static Sprite GetFrameWoodPanel()
        {
            return LoadSprite("Art/UI/Frames/frame_wood_panel.png", 32, new Vector4(12, 12, 12, 12));
        }
        public static Sprite GetFrameParchment()
        {
            return LoadSprite("Art/UI/Frames/frame_parchment.png", 32, new Vector4(10, 10, 10, 10));
        }
        public static Sprite GetFrameSlotNormal()
        {
            return LoadSprite("Art/UI/Frames/frame_slot_normal.png", 32, new Vector4(8, 8, 8, 8));
        }
        public static Sprite GetFrameSlotSelected()
        {
            return LoadSprite("Art/UI/Frames/frame_slot_selected.png", 32, new Vector4(10, 10, 10, 10));
        }
        public static Sprite GetFrameSlotHighlight() => GetFrameSlotSelected();
        public static Sprite GetFrameActionButton()
        {
            return LoadSprite("Art/UI/Frames/frame_action_button.png", 32, new Vector4(10, 6, 10, 6));
        }
        public static Sprite GetJoystickBase()
        {
            return LoadSprite("Art/UI/Controls/joystick_base.png", 32);
        }
        public static Sprite GetJoystickKnob()
        {
            return LoadSprite("Art/UI/Controls/joystick_knob.png", 32);
        }

        /// <summary>
        /// Stardew Valley / 2026 AAA HD-2D Context-Sensitive HUD Auto-Hide Rule (#143 item 2):
        /// Returns true whenever any modal popup or narrative dialogue runner (`runner.InScene`) is active
        /// so floating HUD controls (Virtual Joystick, Skill Medallions, Top-Left Bars, Quest Tracker) hide 100%.
        /// </summary>
        public static bool IsAnyModalOrDialogueOpen() =>
            IsAnyModalOpenIgnoringToolWheel() || (RadialToolWheel.Instance != null && RadialToolWheel.Instance.IsOpen);

        /// <summary>
        /// Every real modal except the tool wheel. The wheel still blocks hotkeys (above), but it must not hide the HUD:
        /// 4 Nov phone report "bấm nút bên cạnh di chuyển… bay hết UI" — opening it set the whole HUD to alpha 0.
        /// </summary>
        public static bool IsAnyModalOpenIgnoringToolWheel()
        {
            var ctrl = MobileGameController.Instance;
            bool inNarrativeScene = ctrl != null && ctrl.Engine?.Narrative?.Runner != null && ctrl.Engine.Narrative.Runner.InScene;
            return VnStoryDialoguePopup.IsOpen
                || inNarrativeScene
                || BackpackPopup.InputBlockedThisFrame
                || CharacterPanel.IsOpen
                || (PigInspectPopup.Instance != null && PigInspectPopup.Instance.ContentPanel != null && PigInspectPopup.Instance.ContentPanel.activeSelf)
                || DayMarketPopup.IsOpen
                || AuctionPopup.IsOpen
                || (VillageBarterPopup.Instance != null && VillageBarterPopup.Instance.IsOpen)
                || RecruitPopup.IsOpen
                || AppraisalPopup.IsOpen
                || ForestEventPopup.IsOpen
                || IntelPopup.IsOpen
                || (FarmOverviewPopup.Instance != null && FarmOverviewPopup.Instance.IsOpen)
                || (DefenseBuildPopup.Instance != null && DefenseBuildPopup.Instance.IsOpen)
                || BuildingInteriorPopup.IsOpen
                || MinimapHudWidget.IsWorldMapModalOpen;
        }
        public static void Apply25DWindowHierarchy(Transform root)
        {
            if (root == null) return;
            if (root.GetComponent<UI25DAutoStyler>() == null)
            {
                root.gameObject.AddComponent<UI25DAutoStyler>();
            }
            Apply25DWindowHierarchyPass(root);
        }

        internal static void Apply25DWindowHierarchyPass(Transform root)
        {
            if (root == null) return;
            var woodSpr = GetFrameWoodPanel();
            var slotSpr = GetFrameSlotNormal();
            var btnSpr = GetFrameActionButton();
            var images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null || img.sprite != null || img.color.a <= 0.18f) continue;
                var rt = img.rectTransform;
                string nm = img.gameObject.name ?? "";
                if (nm.IndexOf("Backdrop", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nm.IndexOf("Overlay", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nm.IndexOf("Fill", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nm.IndexOf("Bar", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nm.IndexOf("Line", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nm.IndexOf("Divider", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.offsetMin == Vector2.zero && rt.offsetMax == Vector2.zero)
                    continue;

                var btn = img.GetComponent<UnityEngine.UI.Button>();
                if (btn != null && btnSpr != null)
                {
                    img.sprite = btnSpr;
                    img.type = UnityEngine.UI.Image.Type.Sliced;
                    img.color = Color.Lerp(img.color, Color.white, 0.38f);
                }
                else if (rt.sizeDelta.x >= 360f && rt.sizeDelta.y >= 240f && woodSpr != null)
                {
                    img.sprite = woodSpr;
                    img.type = UnityEngine.UI.Image.Type.Sliced;
                    img.color = Color.Lerp(img.color, new Color(0.95f, 0.90f, 0.82f, 0.99f), 0.45f);
                    if (img.GetComponent<UnityEngine.UI.Shadow>() == null)
                    {
                        var sh = img.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                        sh.effectColor = new Color(0f, 0f, 0f, 0.65f);
                        sh.effectDistance = new Vector2(4f, -5f);
                    }
                }
                else if (rt.sizeDelta.x >= 56f && rt.sizeDelta.y >= 48f && rt.sizeDelta.x <= 320f && slotSpr != null)
                {
                    img.sprite = slotSpr;
                    img.type = UnityEngine.UI.Image.Type.Sliced;
                    img.color = Color.Lerp(img.color, new Color(0.88f, 0.82f, 0.74f, img.color.a), 0.32f);
                }
            }
        }

        internal sealed class UI25DAutoStyler : MonoBehaviour
        {
            private int lastCount = -1;
            private void OnEnable()
            {
                lastCount = -1;
                Apply25DWindowHierarchyPass(transform);
            }
            private void LateUpdate()
            {
                int c = transform.childCount;
                if (c != lastCount)
                {
                    lastCount = c;
                    Apply25DWindowHierarchyPass(transform);
                }
            }
        }
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
            string cleanKey = itemKey.Trim();
            if (cleanKey.StartsWith("("))
            {
                int closeIdx = cleanKey.IndexOf(')');
                if (closeIdx >= 0 && closeIdx + 1 < cleanKey.Length)
                    cleanKey = cleanKey.Substring(closeIdx + 1);
            }

            // 1. Kiểm tra trực tiếp trong Art/UI/Items/ hoặc PixelMyth/UI/Icons/equip/
            Sprite eqSpr = GetEquipIcon(cleanKey, 24);
            if (eqSpr != null) return eqSpr;
            Sprite uiSpr = LoadSprite($"Art/UI/Items/{cleanKey}.png", 32);
            if (uiSpr != null) return uiSpr;

            // 2. Tra cứu theo từ khóa (bao gồm toàn bộ mã vật phẩm QualifiedItemRegistry)
            return cleanKey switch
            {
                "item_wood_plank" => GetIconWoodPlank(),
                "item_corn_feed" or "item_mutant_corn" or "CamThuong" or "CamTangTrong" or "CamThaoDuoc" or "CamTinhLuyen"
                    => LoadSprite("Art/UI/Items/item_corn_feed.png", 32) ?? LoadSprite("Art/UI/Tools/tool_cam_hat.png", 32),
                "ThaoDuocKho" or "BangGac"
                    => LoadSprite("Art/PixelMyth/UI/Icons/quick/bang_gac_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/equip/bang_gac_24.png", 32),
                "BinhHoiLuc" or "ThuocHoHap" or "ThuocTieuHoa" or "ThuocKySinh" or "ThuocHaNhiet" or "ThuocAnThan" or "VacXinTongHop"
                    => LoadSprite("Art/PixelMyth/UI/Icons/quick/binh_hoi_luc_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/equip/binh_hoi_luc_24.png", 32),
                "LuuKhoi" => LoadSprite("Art/PixelMyth/UI/Icons/quick/luu_khoi_24.png", 32),
                "BomKeo" => LoadSprite("Art/PixelMyth/UI/Icons/quick/bom_keo_24.png", 32),
                "PhaoSang" => LoadSprite("Art/PixelMyth/UI/Icons/quick/phao_sang_24.png", 32),
                "TinhNoDan" => LoadSprite("Art/PixelMyth/UI/Icons/quick/tinh_no_dan_24.png", 32),
                "item_rusty_nail" or "TenThep" => LoadSprite("Art/UI/Items/item_rusty_nail.png", 32),
                "item_broken_post" => LoadSprite("Art/UI/Items/item_broken_post.png", 32),
                "item_rat_tail" => LoadSprite("Art/UI/Items/item_rat_tail.png", 32),
                "item_fertilizer" or "PhanHeoTho" or "PhanHuuCo" or "VoiBot" => LoadSprite("Art/UI/Items/item_fertilizer.png", 32),
                "item_alloy_scrap" or "item_iron_axe_head" or "ThepCuongLuc" => LoadSprite("Art/UI/Items/item_alloy_scrap.png", 32),
                "item_quantum_fiber" => LoadSprite("Art/UI/Items/item_quantum_fiber.png", 32),
                "item_plasma_core" or "item_quantum_chip" or "TinhThach" or "XangSinhHoc" => LoadSprite("Art/UI/Items/item_plasma_core.png", 32),
                "BiKip" => LoadSprite("Art/PixelMyth/UI/Icons/items/bi_kip_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/actions/hoc_vo_24.png", 32),
                // Real Core ids: DefenseRepairs.FineComponentId = "item_fine_component", EnhancementTransactions.StoneId = "da_cuong_hoa".
                "LinhKienTinh" or "item_fine_component" => LoadSprite("Art/PixelMyth/UI/Icons/items/linh_kien_tinh_24.png", 32) ?? LoadSprite("Art/UI/Items/item_quantum_chip.png", 32),
                "DaCuongHoa" or "da_cuong_hoa" => LoadSprite("Art/PixelMyth/UI/Icons/items/da_cuong_hoa_24.png", 32) ?? LoadSprite("Art/UI/Items/item_plasma_core.png", 32),
                "bot_tu_tinh" => LoadSprite("Art/PixelMyth/UI/Icons/items/bot_tu_tinh_24.png", 32) ?? LoadSprite("Art/UI/Items/item_plasma_core.png", 32),
                "muc_an" => LoadSprite("Art/PixelMyth/UI/Icons/items/muc_an_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/actions/cuong_hoa_24.png", 32),
                "huyet_trien" => LoadSprite("Art/PixelMyth/UI/Icons/items/huyet_trien_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/actions/cuong_hoa_24.png", 32),
                "GiayLam" or "Paper_Blue" => LoadSprite("Art/PixelMyth/UI/Icons/items/giay_lam_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),
                "GiayVang" or "Paper_Yellow" => LoadSprite("Art/PixelMyth/UI/Icons/items/giay_vang_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),
                "GiayTim" or "Paper_Purple" => LoadSprite("Art/PixelMyth/UI/Icons/items/giay_tim_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),
                "GiayDo" or "Paper_Red" => LoadSprite("Art/PixelMyth/UI/Icons/items/giay_do_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),
                "XoNuoc" => LoadSprite("Art/PixelMyth/UI/Icons/items/xo_nuoc_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/actions/cho_uong_24.png", 32),
                "CamLinhChi" => LoadSprite("Art/PixelMyth/UI/Icons/items/cam_linh_chi_24.png", 32) ?? LoadSprite("Art/UI/Items/item_corn_feed.png", 32),
                "CamHaHoa" => LoadSprite("Art/PixelMyth/UI/Icons/items/cam_ha_hoa_24.png", 32) ?? LoadSprite("Art/UI/Items/item_corn_feed.png", 32),
                "ThuocAnThanHeo" => LoadSprite("Art/PixelMyth/UI/Icons/items/thuoc_an_than_heo_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/quick/binh_hoi_luc_24.png", 32),
                "HoSoGiongLoai" => LoadSprite("Art/PixelMyth/UI/Icons/items/ho_so_giong_loai_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),
                "MaiCheTam" => LoadSprite("Art/PixelMyth/UI/Icons/items/mai_che_tam_24.png", 32) ?? GetTempShadeCanopySprite(),
                "TinDonCho" => LoadSprite("Art/PixelMyth/UI/Icons/items/tin_don_cho_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),
                "LaDoSuong" => LoadSprite("Art/PixelMyth/UI/Icons/items/la_do_suong_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/status/la_do_suong_16.png", 32),
                "BanDoHuongQuai" => LoadSprite("Art/PixelMyth/UI/Icons/items/ban_do_huong_quai_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),
                "ChiaCongNguoc" => LoadSprite("Art/PixelMyth/UI/Icons/items/chia_cong_nguoc_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/actions/cuong_hoa_24.png", 32),
                "MauGenTho" or "AnTinGiaToc24" or "ManhGiayKienHang"
                    => LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32) ?? GetBlueprintSprite(),
                "gay_sat" or "dao_bau" => LoadSprite("Art/PixelMyth/UI/Icons/weapons/wpn_dao_rua_24.png", 32),
                "thuong_tam_vong" => LoadSprite("Art/PixelMyth/UI/Icons/weapons/wpn_giao_moc_24.png", 32),
                "bua_ta" => LoadSprite("Art/PixelMyth/UI/Icons/weapons/wpn_bua_ta_24.png", 32),
                "ao_vai_day" => LoadSprite("Art/PixelMyth/UI/Icons/equip/armor_ao_chi_24.png", 32),
                "giap_da_heo" => LoadSprite("Art/PixelMyth/UI/Icons/equip/armor_giap_da_thuoc_24.png", 32),
                "vong_co_bac" or "LaBanTanVu" or "BuaHoiHuong" => LoadSprite("Art/PixelMyth/UI/Icons/equip/brc_vong_huyet_nha_24.png", 32),
                "item_rope" => LoadSprite("Art/UI/Items/item_rope.png", 32),
                "item_gold_pouch" or "icon_coin_gold" => GetIconGold(),
                _ => GetIconWoodPlank()
            };
        }

        public static Sprite GetFenceDamagedOverlay() => LoadSprite("Art/Sprites/Environment/Fences/fence_damaged_overlay.png", 32);
        public static Sprite GetFenceBrokenSprite() => LoadSprite("Art/Sprites/Environment/Fences/fence_broken.png", 32);
        public static Sprite GetTractorBeamVFX() => LoadSprite("Art/Sprites/VFX/Telegraphs/vfx_tractor_beam.png", 32, new Vector2(0.5f, 0.05f));
        public static Sprite GetTorchFireVFX() => LoadSprite("Art/Sprites/VFX/Telegraphs/vfx_torch_fire.png", 32, new Vector2(0.5f, 0.1f));

        // --- Celestial Anomaly ("Thứ Trên Trời") & Mythic Cryptid Sprites ---
        public static Sprite GetUfoMothershipSprite() => LoadCharacterCellZero("ufo_mothership") ?? LoadSprite("Art/Sprites/Vehicles/ufo_mothership.png", 32);
        public static Sprite GetUfoScoutSprite() => LoadCharacterCellZero("ufo_trinh_sat") ?? LoadSprite("Art/Sprites/Vehicles/ufo_scout_down.png", 32);
        public static Sprite GetAlienGruntSprite() => LoadCharacterCellZero("mon_chim_ung_dem") ?? LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down.png", 32);
        public static Sprite GetAlienEliteSprite() => LoadCharacterCellZero("mon_doi_quy") ?? LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down.png", 32);
        public static Sprite GetBossAlienWarlordSprite() => LoadCharacterCellZero("mon_boss_thien_tai") ?? LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down.png", 32);

        // Helpers inherited from the existing member C artwork/controller contract.
        public static Sprite GetVillagerAvatar(VillagerId id)
        {
            string charKey = id switch
            {
                VillagerId.BaSau  => "ba_sau",
                VillagerId.TamVa  => "tam_va",
                VillagerId.CoDung => "dung",
                VillagerId.AnhCa  => "anh_ca",
                VillagerId.OngTon => "ong_ton",
                VillagerId.BiNhan => "bi_nhan",
                VillagerId.ChoDem => "nguoi_ban_dem",
                _                 => "ba_sau"
            };
            Sprite pm = LoadCharacterCellZero(charKey);
            if (pm != null) return pm;

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
            return LoadSprite($"Art/Sprites/Characters/NPCs/{fileName}", 32);
        }

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

        public static Sprite GetTempShadeCanopySprite() => LoadSprite("Art/PixelMyth/UI/Kit/temp_shade_canopy.png", 32) ?? GetBarnSprite();

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

        public static Sprite GetEmoteSprite(string emoteKey)
        {
            if (string.IsNullOrEmpty(emoteKey)) return null;
            string raw = emoteKey.Trim();
            string key = raw.ToLowerInvariant();

            string fileName = null;

            // 1. Tim / Yêu thương / Tình cảm (<3, , , , tim, 5 tim, love, heart, \u2764, \u2665)
            if (raw.IndexOf('\u2764') >= 0 || raw.IndexOf('\u2665') >= 0 ||
                raw.Contains("<3") ||
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
            // 7. Hoảng loạn / Cầu cứu (!, !!, sos, , panic, scared, sợ, cứu)
            else if (key == "!" || key == "!!" || key == "!!!" || key == "sos" ||
                     key.Contains("panic") || key.Contains("scared") || key.Contains("sợ") || key.Contains("cứu"))
            {
                fileName = "emote_panic.png";
            }
            // 8. Bị UFO / Alien bắt cóc (, ufo, alien, bắt cóc, bat coc)
            else if (key.Contains("ufo") || key.Contains("alien") || key.Contains("bắt cóc") || key.Contains("bat coc"))
            {
                fileName = "emote_alien.png";
            }
            // 9. Vui vẻ / No nê / Khỏe mạnh (no nê, khỏe mạnh, no, khỏe, happy, star, *, vui)
            else if (raw.Contains("*") || key.Contains("no nê") || key.Contains("no ne") || key == "no" ||
                     key.Contains("khỏe mạnh") || key.Contains("khoe manh") || key.Contains("khỏe") || key.Contains("khoe") ||
                     key.Contains("happy") || key.Contains("star") || key == "*" || key.Contains("vui"))
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

        private static string PascalToSnake(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains("_")) return s.ToLowerInvariant();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsUpper(c))
                {
                    if (i > 0 && (!char.IsUpper(s[i - 1]) || (i + 1 < s.Length && !char.IsUpper(s[i + 1]))))
                        sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static Sprite LoadBuildingRowZero(string blueprintOrFolder)
        {
            if (string.IsNullOrEmpty(blueprintOrFolder)) return null;
            string snake = PascalToSnake(blueprintOrFolder);
            if (snake == "bay_chong_go") snake = "ban_chong";
            string cacheKey = "bldg_row0:" + snake;
            if (spriteCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;

            string[] candidates =
            {
                $"Art/PixelMyth/Buildings/{snake}/{snake}_static.png",
                $"Art/PixelMyth/Buildings/argon_def_{snake}/argon_def_{snake}_static.png",
            };
            foreach (var rel in candidates)
            {
                Sprite full = LoadSprite(rel, 32);
                if (full != null && full.texture != null && full.texture.height >= 32)
                {
                    var tex = full.texture;
                    int ch = tex.height / 4;
                    var sp = Sprite.Create(tex, new Rect(0, tex.height - ch, tex.width, ch), new Vector2(0.5f, 0.5f), 32f);
                    sp.name = snake + "_icon";
                    spriteCache[cacheKey] = sp;
                    return sp;
                }
            }
            return null;
        }

        public static Sprite GetBlueprintSprite(string blueprintId = null)
        {
            if (!string.IsNullOrEmpty(blueprintId))
            {
                Sprite bldg = LoadBuildingRowZero(blueprintId);
                if (bldg != null) return bldg;
            }
            Sprite sp = LoadSprite("Art/UI/Items/item_blueprint.png", 32);
            if (sp != null) return sp;
            return GetFrameParchment();
        }

        public static Sprite GetTowerSprite(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return GetDefenseTowerSprite();
            Sprite pmBldg = LoadBuildingRowZero(blueprintId);
            if (pmBldg != null) return pmBldg;

            // Canonical GDD identifier; member C's art used the older BayChongGo name.
            if (blueprintId == "BanChong") return GetSpikeTrapIdleSprite();

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

        public static Sprite GetThiefSprite() => LoadCharacterCellZero("thief") ?? LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_down.png", 32) ?? LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_down.png", 32);

        public static Sprite GetBreakerSprite() => LoadCharacterCellZero("crawler") ?? LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_down.png", 32) ?? LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_down.png", 32);

        public static Sprite GetPestSprite() => LoadCharacterCellZero("chuot_gam_kho") ?? LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_down.png", 32) ?? LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_down.png", 32);

        public static Sprite GetDauLinhTromSprite() => LoadCharacterCellZero("dau_linh_trom") ?? LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_down.png", 32);

        public static Sprite GetBossChuaTromSprite() => LoadCharacterCellZero("chua_trom_riu_sat") ?? LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_down.png", 32);

        public static Sprite GetBanditOutpostSprite() => LoadBuildingRowZero("nha_kho") ?? LoadSprite("Art/PixelMyth/UI/Icons/places/kho_24.png", 32) ?? LoadSprite("Art/Sprites/Environment/Buildings/building_storage_lvl1.png", 32) ?? GetBarnSprite();

    }
}
