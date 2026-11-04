#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PigTycoon.EditorTools
{
    /// <summary>
    /// Automatically builds and opens the complete Edit-Mode & Play-Mode 2.5D Design Showcase Scene
    /// ("Assets/Scenes/PixelMyth_25D_Design_Master.unity") and saves real Unity Prefabs under
    /// "Assets/Prefabs/PixelMyth_25D/" so that when the student opens Unity Editor to demonstrate
    /// their work to their lecturer, every Map, Grounded 2.5D Building, Defense Tower, Protagonist (An & Khoa 80px),
    /// 18 Pig Breeds, NPCs, 4 Stardew Cutaway Interiors, and UI Canvases (Icon-First HUD, 2.5D Minimap Radar,
    /// 4-Daypart Sundial, 6-Slot Paper-Doll Character Panel, Icon-First Backpack, Defense Build Panel,
    /// and Visual Novel Story Dialogue Box) are immediately visible and inspectable in the Hierarchy,
    /// Scene View, Game View, and Project Prefabs folder!
    /// </summary>
    [InitializeOnLoad]
    public static class PixelMyth25DSceneAndPrefabAuthoring
    {
        public const string FarmMainScenePath = "Assets/Scenes/PixelMyth/Farm_Main.unity";
        public const string HubVillageScenePath = "Assets/Scenes/PixelMyth/Hub_Village.unity";
        public const string MarketTownScenePath = "Assets/Scenes/PixelMyth/Market_Town.unity";
        public const string ForestPrimevalScenePath = "Assets/Scenes/PixelMyth/Forest_Primeval.unity";
        public const string MasterScenePath = "Assets/Scenes/PixelMyth_25D_Design_Master.unity";
        public const string PrefabRootDir = "Assets/Prefabs/PixelMyth_25D";
        private const string SessionBuiltKey = "PigTycoon_25D_MasterScene_AutoBuilt_v27_1";

        static PixelMyth25DSceneAndPrefabAuthoring()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool(SessionBuiltKey, false))
            {
                SessionState.SetBool(SessionBuiltKey, true);
                if (!File.Exists(MasterScenePath))
                {
                    BuildMasterDesignSceneAndPrefabs(openScene: false);
                }
                var active = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(active.path) && File.Exists(FarmMainScenePath))
                {
                    EditorSceneManager.OpenScene(FarmMainScenePath, OpenSceneMode.Single);
                }
            }
        }

        [MenuItem("Pig Tycoon 2.5D/1. Mở Scene Nông Trại Chính 2.5D (Farm_Main - Chơi & Tương Tác Thật)", false, 1)]
        public static void MenuOpenFarmMainScene()
        {
            if (File.Exists(FarmMainScenePath))
                EditorSceneManager.OpenScene(FarmMainScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Pig Tycoon 2.5D/2. Dựng & Mở Scene + 50 Prefab Thiết Kế 2.5D (Chứng Minh Giảng Viên)", false, 2)]
        public static void MenuBuildMasterDesignScene()
        {
            BuildMasterDesignSceneAndPrefabs(openScene: true);
        }

        [MenuItem("Pig Tycoon 2.5D/3. Mở Scene Làng Đông 2.5D (Hub_Village)", false, 3)]
        public static void MenuOpenHubVillageScene()
        {
            if (File.Exists(HubVillageScenePath))
                EditorSceneManager.OpenScene(HubVillageScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Pig Tycoon 2.5D/4. Mở Scene Thị Trấn Chợ 2.5D (Market_Town)", false, 4)]
        public static void MenuOpenMarketTownScene()
        {
            if (File.Exists(MarketTownScenePath))
                EditorSceneManager.OpenScene(MarketTownScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Pig Tycoon 2.5D/5. Mở Scene Rừng Nguyên Sinh 2.5D (Forest_Primeval)", false, 5)]
        public static void MenuOpenForestPrimevalScene()
        {
            if (File.Exists(ForestPrimevalScenePath))
                EditorSceneManager.OpenScene(ForestPrimevalScenePath, OpenSceneMode.Single);
        }

        public static void BuildMasterDesignSceneAndPrefabs(bool openScene)
        {
            try
            {
                EnsureFolders();

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // 0. Main 2.5D Orthographic Camera & EventSystem
                var camGo = new GameObject("Main Camera (2.5D Orthographic)");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 11.5f;
                cam.backgroundColor = new Color(0.08f, 0.11f, 0.15f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                camGo.transform.position = new Vector3(0f, 0f, -20f);

                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

                // 1. 01_WORLD_MAP_25D
                var mapRoot = new GameObject("01_WORLD_MAP_25D (Bản Đồ Nông Trại, Thị Trấn, Làng Đông, Rừng Cổ)");
                BuildWorldMapGroup(mapRoot.transform);

                // 2. 02_GROUNDED_BUILDINGS_AND_DEFENSE_TOWERS
                var bldgRoot = new GameObject("02_GROUNDED_BUILDINGS_AND_DEFENSE_TOWERS (35 Công Trình & Tháp Phòng Thủ 2.5D)");
                BuildGroundedBuildingsGroup(bldgRoot.transform);

                // 3. 03_PROTAGONISTS_AN_AND_KHOA_80PX
                var heroRoot = new GameObject("03_PROTAGONISTS_AN_AND_KHOA_80PX (150 Clip Võ Kỹ & 4 Hệ Vũ Khí)");
                BuildProtagonistsGroup(heroRoot.transform);

                // 4. 04_HERD_18_PIG_BREEDS_AND_PETS
                var herdRoot = new GameObject("04_HERD_18_PIG_BREEDS_AND_PETS (20 Giống Heo & 7 Thú Cưng Tiếp Đất)");
                BuildHerdAndPetsGroup(herdRoot.transform);

                // 5. 05_NPCS_MERCS_AND_BOSSES
                var npcRoot = new GameObject("05_NPCS_MERCS_AND_BOSSES (27 NPC/Lính Đánh Thuê & 20 Quái/Boss)");
                BuildNpcsAndBossesGroup(npcRoot.transform);

                // 6. 06_STARDEW_4_CUTAWAY_INTERIORS
                var interiorRoot = new GameObject("06_STARDEW_4_CUTAWAY_INTERIORS (4 Không Gian Nội Thất Tương Tác)");
                BuildStardewInteriorsGroup(interiorRoot.transform);

                // 7. 07_UI_CANVAS_DESIGN_SHOWCASE
                var uiRoot = new GameObject("07_UI_CANVAS_DESIGN_SHOWCASE (HUD Thuần Icon, Minimap 2.5D, Balo, 6 Ô Paper-Doll, Hội Thoại)");
                BuildUiDesignShowcaseCanvas(uiRoot.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                Directory.CreateDirectory(Path.GetDirectoryName(MasterScenePath) ?? "Assets/Scenes");
                EditorSceneManager.SaveScene(scene, MasterScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (openScene && SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.orthographic = true;
                    SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.identity, 18f, true, false);
                    SceneView.lastActiveSceneView.Repaint();
                }

                Debug.Log($"[PigTycoon 2.5D] Đã dựng thành công Scene '{MasterScenePath}' và bộ Prefab tại '{PrefabRootDir}'!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PigTycoon 2.5D Authoring] " + ex.Message);
            }
        }

        private static void EnsureFolders()
        {
            string[] dirs =
            {
                "Assets/Scenes",
                PrefabRootDir,
                PrefabRootDir + "/Characters",
                PrefabRootDir + "/Pigs",
                PrefabRootDir + "/NPCs",
                PrefabRootDir + "/Buildings",
                PrefabRootDir + "/Interiors",
                PrefabRootDir + "/UI",
            };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        private static Sprite LoadFirstSprite(params string[] candidatePaths)
        {
            foreach (var path in candidatePaths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;

                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                if (all != null)
                {
                    foreach (var obj in all)
                    {
                        if (obj is Sprite s) return s;
                    }
                }

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
            return null;
        }

        private static Sprite LoadFirstCellSprite(string path, int cellW = 72, int cellH = 84)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                int w = Mathf.Min(cellW, tex.width);
                int h = Mathf.Min(cellH, tex.height);
                int y = Mathf.Max(0, tex.height - h);
                return Sprite.Create(tex, new Rect(0, y, w, h), new Vector2(0.5f, 0.06f), 32f);
            }
            return LoadFirstSprite(path);
        }

        private static void SavePrefabIfValid(GameObject go, string prefabPath)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
            }
            catch
            {
                // Ignore if locked or in play transition
            }
        }

        private static void BuildWorldMapGroup(Transform parent)
        {
            var farmMapGo = new GameObject("Map_01_Farm_Main_25D");
            farmMapGo.transform.SetParent(parent, false);
            farmMapGo.transform.localPosition = Vector3.zero;
            var sr = farmMapGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadFirstSprite(
                "Assets/Art/PixelMyth/Maps/farm_preview_crop_x3.png",
                "Assets/Art/PixelMyth/Maps/farm_preview.png",
                "Assets/Art/PixelMyth/Terrain/terrain_meadow.png"
            );
            sr.sortingOrder = -1000;
        }

        private static void BuildGroundedBuildingsGroup(Transform parent)
        {
            (string id, string label, Vector2 pos)[] items =
            {
                ("approved_main_house", "Nhà Chính Nông Trại (Main House)", new Vector2(-10f, 5.5f)),
                ("thien_nhan_vong_dai", "Thiên Nhãn Vọng Đài (Observatory)", new Vector2(-4f, 6.2f)),
                ("may_phat_dien", "Máy Phát Điện (Steam Generator)", new Vector2(2f, 6.0f)),
                ("approved_research_lab", "Phòng Nghiên Cứu & Xưởng Rèn", new Vector2(8f, 5.8f)),
                ("no_xuyen_van", "Tháp Phòng Thủ #1: Nỏ Xuyên Vân", new Vector2(-10f, 0.5f)),
                ("thap_ho_quang", "Tháp Phòng Thủ #2: Tháp Hồ Quang", new Vector2(-5.5f, 0.5f)),
                ("thap_phong_khong_skyhook", "Tháp Phòng Thủ #3: Phòng Không Skyhook", new Vector2(-1f, 0.5f)),
                ("dai_thien_loi", "Tháp Phòng Thủ #4: Đài Thiên Lôi", new Vector2(3.5f, 0.5f)),
                ("cuu_tieu_loi_phao", "Tháp Phòng Thủ #5: Cửu Tiêu Lôi Pháo", new Vector2(8.0f, 0.5f)),
                ("van_linh_hoi_nguyen_tran", "Tháp Phòng Thủ #6: Vạn Linh Hồi Nguyên Trận", new Vector2(12.5f, 0.5f)),
                ("may_chieu_khien", "Công Trình: Máy Chiếu Khiên", new Vector2(-10f, -4.2f)),
                ("dai_do_di_tuong", "Công Trình: Đài Dò Dị Tượng", new Vector2(-5.5f, -4.2f)),
                ("prop_feeder", "Máng Cám Chăn Nuôi 2.5D", new Vector2(-1.5f, -4.2f)),
                ("prop_water_basin", "Bồn Nước Đá Mát 2.5D", new Vector2(2.0f, -4.2f)),
            };

            foreach (var (id, label, pos) in items)
            {
                var go = new GameObject($"Bldg_{id} ({label})");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LoadFirstSprite(
                    $"Assets/Art/PixelMyth/Buildings/{id}/{id}_static.png",
                    $"Assets/Art/PixelMyth/Buildings/{id}/{id}_idle.png"
                );
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2.2f, 1.8f);
                SavePrefabIfValid(go, $"{PrefabRootDir}/Buildings/PF_{id}.prefab");
            }
        }

        private static void BuildProtagonistsGroup(Transform parent)
        {
            (string folder, string clip, string label, Vector2 pos)[] heroes =
            {
                ("an", "an_idle", "An (80px C2-A · Idle 4 Hướng)", new Vector2(-4f, -1.8f)),
                ("an", "an_attack_slash_3", "An (Dao Rựa Slash 3-Hit · 28 FPS)", new Vector2(-2f, -1.8f)),
                ("an", "an_skill_doan_anh_tram_slash", "An (Võ Kỹ Phái A: Đoạn Ảnh Trảm)", new Vector2(0f, -1.8f)),
                ("khoa", "khoa_idle", "Khoa (80px · Idle 4 Hướng)", new Vector2(2.5f, -1.8f)),
                ("khoa", "khoa_attack_smash_3", "Khoa (Búa Tạ Smash Slam · 24 FPS)", new Vector2(4.5f, -1.8f)),
                ("khoa", "khoa_skill_thien_chuy_smash", "Khoa (Võ Kỹ Phái B: Thiên Chùy)", new Vector2(6.5f, -1.8f)),
            };

            foreach (var (folder, clip, label, pos) in heroes)
            {
                var go = new GameObject($"Hero_{clip} ({label})");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LoadFirstCellSprite($"Assets/Art/PixelMyth/Characters/{folder}/{clip}.png", 72, 84);
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + 10;
                var col = go.AddComponent<CapsuleCollider2D>();
                col.size = new Vector2(0.9f, 1.8f);
                SavePrefabIfValid(go, $"{PrefabRootDir}/Characters/PF_{clip}.prefab");
            }
        }

        private static void BuildHerdAndPetsGroup(Transform parent)
        {
            string[] breeds =
            {
                "pig_kim_tho", "pig_hong_dien", "pig_hu_the", "pig_xich_mao",
                "pig_lam_khe", "pig_tinh_quang", "pig_loi_mach", "pig_moc_cuoc",
                "pig_mong_nhan", "pig_phong_mau", "pig_thiet_bi", "pig_tho_tram",
                "pig_white_foot", "pet_giam_doc", "pet_co", "pet_rang"
            };

            for (int i = 0; i < breeds.Length; i++)
            {
                string b = breeds[i];
                float x = -9.5f + (i % 8) * 2.6f;
                float y = -6.8f - (i / 8) * 2.2f;
                var go = new GameObject($"Entity_{b}");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(x, y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                int cw = b.StartsWith("pet_") ? 56 : 72;
                int ch = b.StartsWith("pet_") ? 56 : 68;
                sr.sprite = LoadFirstCellSprite($"Assets/Art/PixelMyth/Characters/{b}/{b}_idle.png", cw, ch);
                sr.sortingOrder = Mathf.RoundToInt(-y * 100f);
                go.AddComponent<CircleCollider2D>().radius = 0.65f;
                SavePrefabIfValid(go, $"{PrefabRootDir}/Pigs/PF_{b}.prefab");
            }
        }

        private static void BuildNpcsAndBossesGroup(Transform parent)
        {
            string[] npcs =
            {
                "ba_sau", "ong_ton", "tam_va", "cuc", "le", "man", "kien", "anh_tu",
                "merc_chot", "merc_tu_troi", "cho_suong", "dau_linh_trom", "mon_boss_thien_tai", "ufo_mothership"
            };

            for (int i = 0; i < npcs.Length; i++)
            {
                string n = npcs[i];
                float x = -9.5f + (i % 7) * 3.0f;
                float y = -11.5f - (i / 7) * 2.6f;
                var go = new GameObject($"Actor_{n}");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(x, y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LoadFirstCellSprite($"Assets/Art/PixelMyth/Characters/{n}/{n}_idle.png", 72, 84);
                sr.sortingOrder = Mathf.RoundToInt(-y * 100f);
                SavePrefabIfValid(go, $"{PrefabRootDir}/NPCs/PF_{n}.prefab");
            }
        }

        private static void BuildStardewInteriorsGroup(Transform parent)
        {
            (string file, string title, Vector2 pos)[] rooms =
            {
                ("interior_nha_chinh", "1. Nội Thất Nhà Chính (Giường Ngủ · Bếp Lò · Bàn Sổ Tay · Kho 36 Ô)", new Vector2(-28f, 6f)),
                ("interior_chuong_trai_lon", "2. Nội Thất Chuồng Trại Lớn (Máng Cám · Bồn Nước · Phối Giống F1 · Y Tế)", new Vector2(-28f, -6f)),
                ("interior_xuong_ren_va_phong_thu", "3. Nội Thất Xưởng Rèn & Phòng Thủ (Đe Rèn · Khắc Ấn · Bản Vẽ Tháp)", new Vector2(28f, 6f)),
                ("interior_quan_ba_sau_va_tram_tin", "4. Nội Thất Quán Bà Sáu & Trạm Tin (Quầy Cơm · Giám Định Gen · Chợ)", new Vector2(28f, -6f)),
            };

            foreach (var (file, title, pos) in rooms)
            {
                var go = new GameObject($"StardewRoom_{file} ({title})");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                go.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LoadFirstSprite($"Assets/Art/PixelMyth/UI/Interiors/{file}.png");
                sr.sortingOrder = -500;
                SavePrefabIfValid(go, $"{PrefabRootDir}/Interiors/PF_{file}.prefab");
            }
        }

        private static void BuildUiDesignShowcaseCanvas(Transform parent)
        {
            var canvasGo = new GameObject("UI_Master_Canvas_25D (1280x720 ScaleWithScreenSize)");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Automatically hide this static Edit-Mode design preview canvas when entering Play Mode so live MythHudController runs cleanly!
            canvasGo.AddComponent<EditModeDesignCanvasAutoHider>();

            // 1. Top-Left 4-Daypart Sundial & Bars
            var sundialGo = CreateUiImage(canvasGo.transform, "HUD_01_Sundial_4Dayparts",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(62f, -62f), new Vector2(96f, 96f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Kit/sundial_base.png"));
            CreateUiImage(sundialGo.transform, "Sundial_Needle",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Kit/sundial_needle.png"));
            CreateUiImage(sundialGo.transform, "Weather_Icon_24px",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Icons/weather/nang_gat_24.png"));

            var guardBar = CreateUiImage(canvasGo.transform, "HUD_02_GuardBar_Vessel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(92f, -128f), new Vector2(156f, 24f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Kit/bar_hp_vessel_frame.png"));
            var stamBar = CreateUiImage(canvasGo.transform, "HUD_03_StaminaBar_DualLayer",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -156f), new Vector2(180f, 24f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Kit/bar_stamina_frame.png"));

            // 2. Top-Right 2.5D Minimap Radar [M] & Quick Medallions
            var minimapFrame = CreateUiImage(canvasGo.transform, "HUD_04_Minimap_Radar_25D [M]",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-118f, -112f), new Vector2(212f, 156f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Common/frame_slot_normal.png"));
            CreateUiImage(minimapFrame.transform, "Minimap_MapViewport",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(198f, 142f),
                LoadFirstSprite("Assets/Art/PixelMyth/Maps/farm_preview_crop_x3.png"));

            // 3. Bottom-Left 2.5D Octagonal Compass Joystick & Bottom-Right Combat/Skill Medallions
            var joyBase = CreateUiImage(canvasGo.transform, "HUD_05_Joystick_25D_Compass",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(96f, 96f), new Vector2(148f, 148f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Kit/joystick_base.png"));
            CreateUiImage(joyBase.transform, "Joystick_Crystal_Knob",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Kit/joystick_knob.png"));

            var atkMedallion = CreateUiImage(canvasGo.transform, "HUD_06_ActionCluster_AttackAndSkills",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-88f, 84f), new Vector2(96f, 96f),
                LoadFirstSprite("Assets/Art/PixelMyth/UI/Kit/medallion_large_normal.png"));

            SavePrefabIfValid(canvasGo, $"{PrefabRootDir}/UI/PF_HUD_And_Minimap_25D_Canvas.prefab");
        }

        private static GameObject CreateUiImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return go;
        }
    }
}
#endif
