using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using PigTycoon.Presentation;

namespace PigTycoon.EditorTools
{
    /// <summary>
    /// Công cụ Editor Nâng Cấp Nghệ Thuật & Level Design Làng Bờ Rìa (BoRiaVillageArtUpgrade):
    /// Thực hiện 4 nâng cấp chiến lược chuẩn Technical Artist & Level Designer 2D Top-down (PPU: 32):
    /// 1. Chuẩn hóa hệ thống Y-Sorting dựa trên chân đế và Collider Offset khớp với SortingPrecision = 100.
    /// 2. De-grid Foliage & Rocks: Thêm Position Jitter, Scale Variation, FlipX ngẫu nhiên và Color Tint theo tầng sâu.
    /// 3. Bổ sung 4 Lore Props tương tác môi trường đậm chất bí ẩn dân gian.
    /// 4. Tinh chỉnh ánh sáng Chiaroscuro: Giữ ấm trung tâm giếng/quán Bà Sáu, làm tối u ám khu nhà Trưởng Thôn và Rừng Cấm Bí Nhân.
    /// </summary>
    public static class BoRiaVillageArtUpgrade
    {
        public const string VillageScenePath = "Assets/Scenes/BoRiaVillage2D.unity";
        private const int SortingPrecision = 100;

        [MenuItem("PigTycoon/Level Design/★ Nâng Cấp Toàn Diện Làng Bờ Rìa (4 Trong 1)")]
        public static void RunFullArtUpgrade()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cảnh Báo", "Vui lòng dừng Play Mode trước khi chạy nâng cấp Level Design!", "Đã hiểu");
                return;
            }

            var envRoot = GetEnvironmentRoot();
            if (envRoot == null) return;

            Undo.RegisterFullObjectHierarchyUndo(envRoot, "BoRia Village Full Art Upgrade");

            Debug.Log("<color=cyan><b>[ArtUpgrade] Bắt đầu nâng cấp toàn diện Làng Bờ Rìa (Level Design & Tech Art)...</b></color>");

            // 1. Chuẩn hóa hệ thống Y-Sorting
            int sortedCount = UpgradeYSorting(envRoot);

            // 2. De-grid Foliage & Rocks (Xóa cảm giác xếp lưới)
            int degridCount = UpgradeDeGridFoliage(envRoot);

            // 3. Bổ sung 4 Lore Props tương tác
            int propsCount = UpgradeLoreProps(envRoot);

            // 4. Tinh chỉnh Ánh Sáng & Khí Quyển Đèn Lồng
            int lightsCount = UpgradeAtmosphereLighting(envRoot);

            // 5. Thông Cầu Thôn Giáp (Gỡ cản bước & phục hồi sorting 2.5D mặt sàn)
            int bridgeFixed = UpgradeBridgeWalkability(envRoot);

            SaveActiveVillageScene();

            Debug.Log($"<color=green><b>[ArtUpgrade] NÂNG CẤP HOÀN TẤT!</b>\n" +
                      $"- Y-Sorting chuẩn hóa: {sortedCount} SpriteRenderers\n" +
                      $"- De-grid Cây & Đá: {degridCount} objects\n" +
                      $"- Lore Props bổ sung: {propsCount} điểm tương tác\n" +
                      $"- Tối ưu hóa đèn lồng & khí quyển: {lightsCount} Avenue_Lanterns\n" +
                      $"- Thông cầu Thôn Giáp: {bridgeFixed} ô sông đã xóa collider</color>");

            EditorUtility.DisplayDialog(
                "Nâng Cấp Thành Công",
                $"Đã nâng cấp mỹ thuật và Level Design Làng Bờ Rìa thành công!\n\n" +
                $"• {sortedCount} Sprites đã chuẩn hóa Y-Sorting (Precision: 100)\n" +
                $"• {degridCount} Cây & Đá đã De-grid tự nhiên\n" +
                $"• 4 Điểm tương tác cốt truyện (Lore Props) đã khởi tạo\n" +
                $"• {lightsCount} Đèn lồng đã tối ưu tương phản Chiaroscuro\n" +
                $"• Cầu Thôn Giáp đã thông suốt hoàn toàn (x: -28..-20)\n\n" +
                $"File Scene đã được tự động lưu: {VillageScenePath}",
                "Tuyệt vời"
            );
        }

        [MenuItem("PigTycoon/Level Design/1. Chuẩn Hóa Y-Sorting (Chân Đế)")]
        public static void RunStep1_YSorting()
        {
            var envRoot = GetEnvironmentRoot();
            if (envRoot == null) return;
            Undo.RegisterFullObjectHierarchyUndo(envRoot, "BoRia Step 1 Y-Sorting");
            int c = UpgradeYSorting(envRoot);
            SaveActiveVillageScene();
            EditorUtility.DisplayDialog("Y-Sorting Hoàn Tất", $"Đã chuẩn hóa Sorting Order cho {c} Sprites (SpriteSortPoint: Pivot, Precision: 100).", "OK");
        }

        [MenuItem("PigTycoon/Level Design/2. De-grid Cây & Đá (Tự Nhiên Hóa)")]
        public static void RunStep2_DeGrid()
        {
            var envRoot = GetEnvironmentRoot();
            if (envRoot == null) return;
            Undo.RegisterFullObjectHierarchyUndo(envRoot, "BoRia Step 2 De-Grid");
            int c = UpgradeDeGridFoliage(envRoot);
            SaveActiveVillageScene();
            EditorUtility.DisplayDialog("De-Grid Hoàn Tất", $"Đã xóa lưới công nghiệp cho {c} Cây & Khối Đá (Jitter, Scale, FlipX, Depth Tint).", "OK");
        }

        [MenuItem("PigTycoon/Level Design/3. Bổ Sung 4 Lore Props Dân Gian")]
        public static void RunStep3_LoreProps()
        {
            var envRoot = GetEnvironmentRoot();
            if (envRoot == null) return;
            Undo.RegisterFullObjectHierarchyUndo(envRoot, "BoRia Step 3 Lore Props");
            int c = UpgradeLoreProps(envRoot);
            SaveActiveVillageScene();
            EditorUtility.DisplayDialog("Lore Props Hoàn Tất", $"Đã tạo/cập nhật {c} điểm tương tác cốt truyện kèm Trigger Collider & World Space Prompt.", "OK");
        }

        [MenuItem("PigTycoon/Level Design/4. Tinh Chỉnh Khí Quyển Đèn Lồng Chiaroscuro")]
        public static void RunStep4_Atmosphere()
        {
            var envRoot = GetEnvironmentRoot();
            if (envRoot == null) return;
            Undo.RegisterFullObjectHierarchyUndo(envRoot, "BoRia Step 4 Atmosphere");
            int c = UpgradeAtmosphereLighting(envRoot);
            SaveActiveVillageScene();
            EditorUtility.DisplayDialog("Khí Quyển Hoàn Tất", $"Đã điều chỉnh ánh sáng cho {c} Đèn Lồng Avenue_Lantern theo phân vùng tương phản.", "OK");
        }

        [MenuItem("PigTycoon/Level Design/5. Thông Cầu Thôn Giáp (Xóa Cản Bước)")]
        public static void RunStep5_Bridge()
        {
            var envRoot = GetEnvironmentRoot();
            if (envRoot == null) return;
            Undo.RegisterFullObjectHierarchyUndo(envRoot, "BoRia Step 5 Bridge Walkability");
            int c = UpgradeBridgeWalkability(envRoot);
            SaveActiveVillageScene();
            EditorUtility.DisplayDialog("Thông Cầu Hoàn Tất", $"Đã gỡ bỏ toàn bộ Water Tilemap Colliders cản bước trên cầu ({c} ô sông x: -28..-20) và chuẩn hóa Depth Sorting.", "OK");
        }

        private static GameObject GetEnvironmentRoot()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != VillageScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(VillageScenePath, OpenSceneMode.Single);
            }

            var envRoot = GameObject.Find("Village_Environment_2D");
            if (envRoot == null)
            {
                Debug.LogError("[ArtUpgrade] Không tìm thấy root GameObject 'Village_Environment_2D' trong Scene!");
                return null;
            }
            return envRoot;
        }

        private static void SaveActiveVillageScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }

        #region 1. CHUẨN HÓA Y-SORTING
        /// <summary>
        /// Duyệt qua toàn bộ SpriteRenderer bên trong Village_Environment_2D (trừ TilemapRenderer và Mystic_Aura).
        /// Đặt SpriteSortPoint = Pivot và tính lại sortingOrder = Mathf.RoundToInt(-(transform.position.y + colliderOffsetY) * 100).
        /// </summary>
        public static int UpgradeYSorting(GameObject envRoot)
        {
            var renderers = envRoot.GetComponentsInChildren<SpriteRenderer>(true);
            int count = 0;

            foreach (var sr in renderers)
            {
                if (sr == null) continue;

                // Loại trừ TilemapRenderer, Mystic_Aura, và kết cấu đặc biệt của cầu (Mặt sàn và Lan can)
                if (sr.gameObject.name.IndexOf("Mystic_Aura", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sr.gameObject.name.IndexOf("Bridge_Main_Structure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sr.gameObject.name.IndexOf("Bridge_Front_Railing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (sr.transform.parent != null && sr.transform.parent.name.IndexOf("Mystic_Aura", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    continue;
                }

                // Đặt điểm sort theo Pivot
                sr.spriteSortPoint = SpriteSortPoint.Pivot;

                // Tính toán chân đế (Foot position) dựa vào Collider2D offset nếu có
                float colliderOffsetY = 0f;
                var col = sr.GetComponent<Collider2D>();
                if (col == null) col = sr.GetComponentInParent<Collider2D>();

                if (col != null)
                {
                    colliderOffsetY = col.offset.y;
                }

                float footY = sr.transform.position.y + colliderOffsetY;
                sr.sortingOrder = Mathf.RoundToInt(-footY * SortingPrecision);
                count++;
            }

            return count;
        }
        #endregion

        #region 2. DE-GRID FOLIAGE & ROCKS
        /// <summary>
        /// Xóa bỏ cảm giác xếp lưới công nghiệp cho cây và đá:
        /// - Position Jitter: ngẫu nhiên từ -0.35f đến +0.35f (X, Y).
        /// - LocalScale: biến thiên 1.15f - 1.45f.
        /// - FlipX: ngẫu nhiên 50/50.
        /// - Color Tint: giảm nhẹ độ sáng 0.88f - 1.0f cho lớp cây viền ngoài tạo chiều sâu rừng rậm.
        /// </summary>
        public static int UpgradeDeGridFoliage(GameObject envRoot)
        {
            int count = 0;
            var transforms = envRoot.GetComponentsInChildren<Transform>(true);

            // Danh sách tiền tố các object tự nhiên cần de-grid
            string[] targetPrefixes = {
                "Tree_North", "Tree_South", "East_Tree", "West_Tree", "Willow", "Perimeter_Rock",
                "Orchard_Tree", "Herb_Tree", "TamVa_Pine", "AnhCa_Tree", "Park_Tree",
                "NorthAvenue_Tree", "SouthAvenue_Tree", "Forest_Tree"
            };

            var processedRoots = new HashSet<Transform>();

            foreach (var tr in transforms)
            {
                if (tr == null || tr == envRoot.transform) continue;

                string name = tr.name;
                bool isTarget = false;
                bool isPerimeter = false;

                foreach (var prefix in targetPrefixes)
                {
                    if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                        name.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isTarget = true;
                        if (prefix.Contains("Tree_North") || prefix.Contains("Tree_South") ||
                            prefix.Contains("East_Tree") || prefix.Contains("West_Tree") ||
                            prefix.Contains("Perimeter_Rock"))
                        {
                            isPerimeter = true;
                        }
                        break;
                    }
                }

                if (!isTarget) continue;

                // Tránh can thiệp 2 lần vào cả parent lẫn child
                if (processedRoots.Contains(tr.parent)) continue;
                processedRoots.Add(tr);

                // 1. Position Jitter (-0.35f .. +0.35f)
                // Dùng Pseudo-random theo hash để kết quả đẹp và ổn định
                int hash = name.GetHashCode();
                var rand = new System.Random(hash);

                float jitterX = ((float)rand.NextDouble() * 0.70f) - 0.35f;
                float jitterY = ((float)rand.NextDouble() * 0.70f) - 0.35f;
                tr.position = new Vector3(tr.position.x + jitterX, tr.position.y + jitterY, tr.position.z);

                // 2. LocalScale Variation (1.15f - 1.45f)
                float scale = 1.15f + ((float)rand.NextDouble() * 0.30f);
                tr.localScale = new Vector3(scale, scale, 1f);

                // 3. FlipX & Color Tint trên SpriteRenderer
                var sr = tr.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.flipX = rand.NextDouble() > 0.50;

                    if (isPerimeter)
                    {
                        // Giảm nhẹ độ sáng cho lớp ngoài tạo bóng đổ chiều sâu rừng rậm
                        float brightness = 0.88f + ((float)rand.NextDouble() * 0.12f);
                        sr.color = new Color(brightness, brightness * 0.98f, brightness, 1f);
                    }
                    else
                    {
                        float subtleTint = 0.94f + ((float)rand.NextDouble() * 0.06f);
                        sr.color = new Color(subtleTint, subtleTint, subtleTint, 1f);
                    }

                    // Cập nhật lại Y-sorting sau khi jitter vị trí
                    float colliderOffsetY = 0f;
                    var col = tr.GetComponent<Collider2D>();
                    if (col != null) colliderOffsetY = col.offset.y;
                    sr.sortingOrder = Mathf.RoundToInt(-(tr.position.y + colliderOffsetY) * SortingPrecision);
                }

                count++;
            }

            return count;
        }
        #endregion

        #region 3. BỔ SUNG 4 LORE PROPS TƯƠNG TÁC
        /// <summary>
        /// Tự động khởi tạo 4 Điểm tương tác Môi trường (Lore Props) đậm chất cốt truyện dân gian Bờ Rìa.
        /// </summary>
        public static int UpgradeLoreProps(GameObject envRoot)
        {
            int count = 0;

            // Tìm hoặc tạo nhóm chứa Lore Props
            var loreGroup = envRoot.transform.Find("Village_Lore_Props");
            if (loreGroup == null)
            {
                var lgObj = new GameObject("Village_Lore_Props");
                lgObj.transform.SetParent(envRoot.transform, false);
                loreGroup = lgObj.transform;
            }

            var boardSprite = LoadSpriteAsset("Assets/Art/Sprites/Environment/Village/prop_village_notice_board.png");
            var tableSprite = LoadSpriteAsset("Assets/Art/Sprites/Environment/Village/prop_tea_table_set.png");
            var bowlSprite = LoadSpriteAsset("Assets/Art/Sprites/Environment/Village/prop_inverted_bowl.png");
            var treeFruitSprite = LoadSpriteAsset("Assets/Art/Sprites/Environment/Village/tree_village_fruit.png");

            // -------------------------------------------------------------
            // Prop 1: Prop_BoRia_Signpost (Cổng Nam x: -1.5, y: -15.5)
            // -------------------------------------------------------------
            CreateOrUpdateLoreProp(
                loreGroup,
                "Prop_BoRia_Signpost",
                new Vector3(-1.5f, -15.5f, 0f),
                boardSprite,
                "Biển Báo Cổng Nam",
                "Đọc Biển Báo Cũ",
                "BIỂN BÁO LỐI VÀO CỔNG NAM (RỈ SÉT)",
                "\"Xe quay đầu ở sân quán. Đừng quay trong ruộng. Ruộng không nhận lời xin lỗi.\"",
                new Vector2(1.2f, 0.8f),
                new Vector2(0f, -0.2f),
                new Color(0.92f, 0.85f, 0.75f, 1f), // Tông gỉ sét cũ kỹ
                0.95f
            );
            count++;

            // -------------------------------------------------------------
            // Prop 2: Prop_Forbidden_Tree (Cây dọc đại lộ trung tâm x: -1.5, y: 8.5)
            // -------------------------------------------------------------
            CreateOrUpdateLoreProp(
                loreGroup,
                "Prop_Forbidden_Tree",
                new Vector3(-1.5f, 8.5f, 0f),
                treeFruitSprite,
                "Cây Đứng Bóng Cấm Địa",
                "Nhìn Lên Tán Cây",
                "LỜI DẶN CỦA MẬN (CÂY ĐỨNG BÓNG)",
                "\"Mận từng dặn: cái cây này mùa mưa đừng đứng dưới, cái cây kia mùa nào cũng đừng đứng dưới.\"",
                new Vector2(1.4f, 1.0f),
                new Vector2(0f, -1.2f),
                new Color(0.85f, 0.72f, 0.50f, 1f), // Tán lá úa vàng bất thường
                1.35f
            );
            count++;

            // -------------------------------------------------------------
            // Prop 3: Prop_UpsideDown_Bowls (Cạnh quán Bà Sáu x: -12.0, y: 6.5)
            // -------------------------------------------------------------
            CreateOrUpdateLoreProp(
                loreGroup,
                "Prop_UpsideDown_Bowls",
                new Vector3(-12.0f, 6.5f, 0f),
                bowlSprite != null ? bowlSprite : tableSprite,
                "Bát Úp Bàn Trà",
                "Xem Bát Úp",
                "TỤC LỆ BÁT ÚP QUÁN BÀ SÁU",
                "\"Bàn nào trong quán cũng có một cái bát úp ở chiếc ghế trống. Không ai ngồi, không ai lật.\"",
                new Vector2(1.0f, 0.8f),
                new Vector2(0f, -0.1f),
                new Color(0.88f, 0.86f, 0.82f, 1f),
                1.1f
            );
            count++;

            // -------------------------------------------------------------
            // Prop 4: Prop_Scale_PriceBoard (Cạnh Station_AnhCa x: 16.0, y: -17.0)
            // -------------------------------------------------------------
            CreateOrUpdateLoreProp(
                loreGroup,
                "Prop_Scale_PriceBoard",
                new Vector3(16.0f, -17.0f, 0f),
                boardSprite,
                "Bảng Giá Trạm Cân",
                "Xem Bảng Giá Cân",
                "BẢNG GIÁ THU MUA HEO TRẠM CÂN (ĐÃ BỊ GẠCH XÓA)",
                "\"Bảng giá thu mua ghi '6+1 kg'. Mọi con số bảy đều bị gạch xóa.\"",
                new Vector2(1.2f, 0.8f),
                new Vector2(0f, -0.2f),
                new Color(0.80f, 0.75f, 0.70f, 1f),
                1.0f
            );
            count++;

            return count;
        }

        private static GameObject CreateOrUpdateLoreProp(
            Transform parent,
            string name,
            Vector3 worldPos,
            Sprite sprite,
            string displayName,
            string promptText,
            string loreTitle,
            string loreContent,
            Vector2 colSize,
            Vector2 colOffset,
            Color tint,
            float scale)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }

            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * scale;

            // 1. SpriteRenderer (Đảm bảo an toàn tuyệt đối, tránh cạm bẫy toán tử ?? với UnityEngine.Object)
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
            }
            if (sprite != null)
            {
                sr.sprite = sprite;
            }
            sr.color = tint;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            sr.sortingOrder = Mathf.RoundToInt(-(worldPos.y + colOffset.y) * SortingPrecision);

            // 2. BoxCollider2D Trigger
            var col = go.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = go.AddComponent<BoxCollider2D>();
            }
            col.isTrigger = true;
            col.size = colSize;
            col.offset = colOffset;

            // 3. CustomBuildingController (AssignedFunction = DocBangTinLore)
            var cb = go.GetComponent<CustomBuildingController>();
            if (cb == null)
            {
                cb = go.AddComponent<CustomBuildingController>();
            }
            cb.BuildingId = name.ToLowerInvariant();
            cb.BuildingDisplayName = displayName;
            cb.BuildingRenderer = sr;
            cb.UseBoxTrigger = true;
            cb.InteractionBoxSize = new Vector2(colSize.x + 0.8f, colSize.y + 0.8f);
            cb.ActionPromptText = promptText;
            cb.SubPromptInfo = "Nhấn [E] để đọc";
            cb.ShowWorldSpacePrompt = true;
            cb.AssignedFunction = BuildingInteractionFunction.DocBangTinLore;
            cb.LoreTitle = loreTitle;
            cb.LoreContent = loreContent;
            cb.SortingPrecision = SortingPrecision;

            return go;
        }
        #endregion

        #region 4. TỐI ƯU HÓA ÁNH SÁNG & KHÍ QUYỂN (CHIAROSCURO)
        /// <summary>
        /// Giữ nguyên mật độ đèn Avenue_Lantern ở khu vực trung tâm giếng (Well) và quán Bà Sáu (ấm áp, an toàn).
        /// Làm tối hoặc giảm bớt đèn ở khu vực dẫn về Building_Cottage (Nhà Trưởng Thôn dán bùa đỏ phía Bắc)
        /// và vùng có Mystic_Aura (Phía Đông) để tạo sự tương phản sáng - tối rùng rợn.
        /// </summary>
        public static int UpgradeAtmosphereLighting(GameObject envRoot)
        {
            int count = 0;
            var flora = envRoot.transform.Find("Village_Flora");
            if (flora == null) return 0;

            for (int i = 0; i < 26; i++)
            {
                var lanternTr = flora.Find($"Avenue_Lantern_{i}");
                if (lanternTr == null) continue;

                Vector3 pos = lanternTr.position;
                var sr = lanternTr.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                count++;

                // 1. Khu vực Trục Bắc dẫn vào Nhà Trưởng Thôn (Elder_Cottage: x: -0.5, y: 18.5)
                // Đèn Avenue_Lantern_8 (y: 6.5), 9 (y: 10.5), 10 (y: 14.5)
                if (pos.y > 5.0f && Mathf.Abs(pos.x) < 4.0f)
                {
                    if (pos.y > 13.0f) // Gần sát cổng nhà Trưởng Thôn
                    {
                        // Ngọn đèn cuối cùng tắt lụi hoàn toàn, lạnh lẽo bên rào
                        sr.color = new Color(0.20f, 0.18f, 0.18f, 0.55f);
                    }
                    else if (pos.y > 9.0f)
                    {
                        // Đèn chập chờn le lói, mờ tối
                        sr.color = new Color(0.42f, 0.35f, 0.25f, 0.75f);
                    }
                    else
                    {
                        // Bắt đầu nhạt dần từ quảng trường đi lên
                        sr.color = new Color(0.70f, 0.65f, 0.50f, 0.90f);
                    }
                }
                // 2. Khu vực Đại lộ Đông dẫn ra Rừng Cấm & Ranh Giới Bí Nhân (Station_BiNhan: x: 35.5, y: 0.5)
                // x = 6.5 .. 31.5
                else if (pos.x > 5.0f && Mathf.Abs(pos.y) < 3.0f)
                {
                    if (pos.x > 25.0f) // Vùng giáp ranh Rừng Cấm (Mystic_Aura)
                    {
                        // Giảm mật độ: tắt bớt đèn hàng dưới, đèn hàng trên chuyển sang ánh u ám lạnh ngắt
                        if (pos.y < 0f)
                        {
                            lanternTr.gameObject.SetActive(false); // Ẩn bớt để thưa thớt
                        }
                        else
                        {
                            sr.color = new Color(0.28f, 0.35f, 0.45f, 0.65f); // Ánh xanh u linh rợn ngợp
                        }
                    }
                    else if (pos.x > 18.0f)
                    {
                        // Vùng trung gian: làm tối và thưa bớt
                        if (pos.y > 0f && (i % 2 == 0))
                        {
                            lanternTr.gameObject.SetActive(false);
                        }
                        else
                        {
                            sr.color = new Color(0.45f, 0.45f, 0.40f, 0.80f);
                        }
                    }
                    else
                    {
                        // Đoạn đầu đại lộ Đông gần Giếng Làng: sáng vàng dịu
                        sr.color = new Color(0.95f, 0.90f, 0.75f, 1.0f);
                    }
                }
                // 3. Khu vực Trung Tâm Giếng và Quán Bà Sáu (Đại lộ Tây x = -18.5 .. -4.5)
                else
                {
                    // Giữ nguyên ánh sáng vàng ấm áp, thanh bình
                    lanternTr.gameObject.SetActive(true);
                    sr.color = new Color(1.0f, 0.95f, 0.82f, 1.0f);
                }
            }

            return count;
        }
        #endregion

        #region 5. THÔNG CẦU THÔN GIÁP (CLEAR BRIDGE COLLIDERS & RESTORE DEPTH)
        /// <summary>
        /// Khắc phục triệt để lỗi không đi qua được cầu Thôn Giáp:
        /// 1. Gỡ bỏ toàn bộ TilemapCollider2D của Tilemap_Water trong hành lang cầu (x: -28..-20, y: -1..1).
        /// 2. Khôi phục Sorting Order chuẩn 2.5D cho cầu: Mặt sàn (-20, nằm dưới chân người) và Lan can (250, che trước chân).
        /// </summary>
        public static int UpgradeBridgeWalkability(GameObject envRoot)
        {
            int count = 0;
            var waterObj = GameObject.Find("Tilemap_Water");
            if (waterObj != null)
            {
                var tm = waterObj.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                if (tm != null)
                {
                    for (int x = -28; x <= -20; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            var pos = new Vector3Int(x, y, 0);
                            if (tm.HasTile(pos))
                            {
                                tm.SetColliderType(pos, UnityEngine.Tilemaps.Tile.ColliderType.None);
                                count++;
                            }
                        }
                    }
                }
            }

            // Khôi phục chiều sâu chuẩn cho kết cấu cầu
            var bridgeMain = GameObject.Find("Bridge_Main_Structure");
            if (bridgeMain != null)
            {
                var sr = bridgeMain.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = -20;
                }
            }

            var bridgeRail = GameObject.Find("Bridge_Front_Railing");
            if (bridgeRail != null)
            {
                var sr = bridgeRail.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 250;
                }
            }

            return count;
        }
        #endregion

        private static Sprite LoadSpriteAsset(string path)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null)
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in allAssets)
                {
                    if (a is Sprite spr)
                    {
                        sp = spr;
                        break;
                    }
                }
            }
            if (sp == null)
            {
                sp = UISpriteLoader.LoadSprite(path, 32);
            }
            return sp;
        }
    }
}
