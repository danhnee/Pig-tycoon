using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PigTycoon.Presentation;
using PigTycoon.Core;

namespace PigTycoon.EditorTools
{
    public class BuildingDesignerWindow : UnityEditor.EditorWindow
    {
        private int selectedTab = 0;
        private readonly string[] tabTitles = new string[]
        {
            "🎨 Tạo Công Trình Mới",
            "📋 Danh Sách Trong Scene",
            "🏛️ Thư Viện Mẫu Có Sẵn"
        };

        // Tab 1: Tạo mới
        private Sprite newBuildingSprite;
        private string newBuildingName = "Nhà Dân Cư";
        private string newBuildingId = "building_house_01";
        private Vector2 newSpawnPosition = Vector2.zero;
        private bool placeAtSceneViewCenter = true;

        private BuildingInteractionFunction newFunction = BuildingInteractionFunction.VaoNhaScene;
        private string newActionPrompt = "Vào Nhà";
        private Vector2 newDoorOffset = new Vector2(0f, -1.2f);
        private float newInteractionRadius = 1.8f;
        private bool newAutoCreateWallCollider = true;

        // Cấu hình theo chức năng
        private string newTargetScene = "MainFarm2D";
        private Vector2 newTargetArrivalPos = new Vector2(0f, -27.5f);
        private string newLoreQuote = "\"Bạn đẩy nhẹ cánh cửa bước vào trong, ánh sáng ấm áp chào đón bạn.\"";
        private VillagerId newTargetVillager = VillagerId.BaSau;
        private string newLoreTitle = "Thông Báo Làng";
        private string newLoreText = "Khu vực làng Bờ Rìa cấm người lạ xâm nhập vào ban đêm sau hồi chuông thứ ba.";
        private Vector2 newInteriorTeleport = Vector2.zero;

        // Scroll positions
        private Vector2 scrollTab1;
        private Vector2 scrollTab2;
        private Vector2 scrollTab3;

        [MenuItem("PigTycoon/5. Công Cụ Thiết Kế Công Trình & Gán Chức Năng (Building Designer)", false, 50)]
        public static void OpenWindow()
        {
            var window = GetWindow<BuildingDesignerWindow>("Building Designer");
            window.minSize = new Vector2(500, 620);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();

            selectedTab = GUILayout.Toolbar(selectedTab, tabTitles, GUILayout.Height(32));
            EditorGUILayout.Space(8);

            switch (selectedTab)
            {
                case 0:
                    DrawTabCreateNew();
                    break;
                case 1:
                    DrawTabSceneList();
                    break;
                case 2:
                    DrawTabPresets();
                    break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🏛️ PIG TYCOON — BỘ CÔNG CỤ THIẾT KẾ CÔNG TRÌNH & GÁN CHỨC NĂNG", EditorStyles.boldLabel);
            GUILayout.Label("Cho phép bạn tự chọn ảnh vẽ công trình bất kỳ, tự định vị cửa (Door Point) xuất hiện nút vào nhà,", EditorStyles.miniLabel);
            GUILayout.Label("và gán các chức năng có sẵn (Vào nhà / Chuyển Scene, Barter Dân Làng, Kho Rương, Ngủ Nghỉ, Lore).", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // =========================================================================
        // TAB 1: TẠO CÔNG TRÌNH MỚI
        // =========================================================================
        private void DrawTabCreateNew()
        {
            scrollTab1 = EditorGUILayout.BeginScrollView(scrollTab1);

            EditorGUILayout.LabelField("1. Hình Ảnh Công Trình (Sprite)", EditorStyles.boldLabel);
            newBuildingSprite = (Sprite)EditorGUILayout.ObjectField("Ảnh Vẽ Công Trình:", newBuildingSprite, typeof(Sprite), false, GUILayout.Height(20));
            if (newBuildingSprite == null)
            {
                EditorGUILayout.HelpBox("Kéo thả file Sprite công trình bạn tự vẽ (hoặc chọn từ Project) vào ô trên.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Xem trước:", GUILayout.Width(80));
                Rect previewRect = GUILayoutUtility.GetRect(80, 80, GUILayout.Width(80), GUILayout.Height(80));
                if (newBuildingSprite.texture != null)
                {
                    GUI.DrawTexture(previewRect, newBuildingSprite.texture, ScaleMode.ScaleToFit);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("2. Thông Tin Cơ Bản", EditorStyles.boldLabel);
            newBuildingName = EditorGUILayout.TextField("Tên Công Trình:", newBuildingName);
            newBuildingId = EditorGUILayout.TextField("Mã Công Trình (ID):", newBuildingId);

            EditorGUILayout.BeginHorizontal();
            placeAtSceneViewCenter = EditorGUILayout.Toggle("Đặt Tại Giữa Màn Hình Scene", placeAtSceneViewCenter);
            if (!placeAtSceneViewCenter)
            {
                newSpawnPosition = EditorGUILayout.Vector2Field("Tọa Độ Đặt:", newSpawnPosition);
            }
            EditorGUILayout.EndHorizontal();

            newAutoCreateWallCollider = EditorGUILayout.Toggle("Tự Động Tạo Tường Va Chạm", newAutoCreateWallCollider);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("3. Vị Trí Cửa & Vùng Kích Hoạt Nút", EditorStyles.boldLabel);
            newDoorOffset = EditorGUILayout.Vector2Field("Tọa Độ Cửa Tương Đối (Offset):", newDoorOffset);
            newInteractionRadius = EditorGUILayout.Slider("Bán Kính Nhận Diện (Radius):", newInteractionRadius, 0.8f, 5.0f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Căn Cửa Giữa - Đáy"))
            {
                if (newBuildingSprite != null)
                    newDoorOffset = new Vector2(0f, -newBuildingSprite.bounds.extents.y);
                else
                    newDoorOffset = new Vector2(0f, -1.2f);
            }
            if (GUILayout.Button("Căn Cửa Trái"))
            {
                if (newBuildingSprite != null)
                    newDoorOffset = new Vector2(-newBuildingSprite.bounds.extents.x * 0.6f, -newBuildingSprite.bounds.extents.y);
                else
                    newDoorOffset = new Vector2(-1.2f, -1.2f);
            }
            if (GUILayout.Button("Căn Cửa Phải"))
            {
                if (newBuildingSprite != null)
                    newDoorOffset = new Vector2(newBuildingSprite.bounds.extents.x * 0.6f, -newBuildingSprite.bounds.extents.y);
                else
                    newDoorOffset = new Vector2(1.2f, -1.2f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("4. Chức Năng Gán Cho Công Trình", EditorStyles.boldLabel);
            newFunction = (BuildingInteractionFunction)EditorGUILayout.EnumPopup("Chọn Chức Năng:", newFunction);
            newActionPrompt = EditorGUILayout.TextField("Chữ Hiển Thị Trên Nút:", newActionPrompt);

            // Cấu hình chi tiết chức năng
            EditorGUI.indentLevel++;
            switch (newFunction)
            {
                case BuildingInteractionFunction.VaoNhaScene:
                    EditorGUILayout.HelpBox("Khi người chơi tới cửa bấm nút, game sẽ chuyển mượt sang Scene đích.", MessageType.None);
                    newTargetScene = EditorGUILayout.TextField("Tên Scene Đích:", newTargetScene);
                    newTargetArrivalPos = EditorGUILayout.Vector2Field("Tọa Độ Xuất Hiện Tại Scene Mới:", newTargetArrivalPos);
                    newLoreQuote = EditorGUILayout.TextField("Lời Dẫn Chuyển Cảnh:", newLoreQuote);
                    break;

                case BuildingInteractionFunction.TeleportNoiThat:
                    EditorGUILayout.HelpBox("Dịch chuyển nhân vật đến tọa độ phòng trong cùng Scene.", MessageType.None);
                    newInteriorTeleport = EditorGUILayout.Vector2Field("Tọa Độ Phòng Nội Thất:", newInteriorTeleport);
                    break;

                case BuildingInteractionFunction.GiaoDichDanLang:
                    EditorGUILayout.HelpBox("Khi bấm nút sẽ mở popup trao đổi Barter với Dân Làng Bờ Rìa.", MessageType.None);
                    newTargetVillager = (VillagerId)EditorGUILayout.EnumPopup("Dân Làng Bờ Rìa:", newTargetVillager);
                    break;

                case BuildingInteractionFunction.MoKhoLuuTru:
                    EditorGUILayout.HelpBox("Mở Balo / Hòm trữ đồ để gửi hoặc nhận vật phẩm.", MessageType.None);
                    break;

                case BuildingInteractionFunction.NghiNgoiGiuong:
                    EditorGUILayout.HelpBox("Hồi phục 100% thể lực (Stamina) và chuyển sang chu kỳ thời gian tiếp theo.", MessageType.None);
                    break;

                case BuildingInteractionFunction.DocBangTinLore:
                    EditorGUILayout.HelpBox("Hiển thị thông báo, cốt truyện hoặc ghi chú khi đọc.", MessageType.None);
                    newLoreTitle = EditorGUILayout.TextField("Tiêu Đề Bảng Tin:", newLoreTitle);
                    newLoreText = EditorGUILayout.TextArea(newLoreText, GUILayout.Height(50));
                    break;

                case BuildingInteractionFunction.SuKienTuyBien:
                    EditorGUILayout.HelpBox("Sau khi tạo, bạn có thể gán hàm C# bất kỳ trong Unity Inspector.", MessageType.None);
                    break;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(14);
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("🏠 TẠO CÔNG TRÌNH VÀO SCENE HIỆN TẠI", GUILayout.Height(42)))
            {
                CreateBuildingInScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        private void CreateBuildingInScene()
        {
            Vector3 spawnWorldPos = Vector3.zero;
            if (placeAtSceneViewCenter && SceneView.lastActiveSceneView != null)
            {
                var pivot = SceneView.lastActiveSceneView.pivot;
                spawnWorldPos = new Vector3(Mathf.Round(pivot.x), Mathf.Round(pivot.y), 0f);
            }
            else
            {
                spawnWorldPos = new Vector3(newSpawnPosition.x, newSpawnPosition.y, 0f);
            }

            var rootObj = new GameObject(string.IsNullOrEmpty(newBuildingName) ? "CustomBuilding" : newBuildingName);
            rootObj.transform.position = spawnWorldPos;
            Undo.RegisterCreatedObjectUndo(rootObj, "Create Custom Building");

            // 1. SpriteRenderer
            var sr = rootObj.AddComponent<SpriteRenderer>();
            sr.sprite = newBuildingSprite;
            sr.sortingLayerName = "Default";

            // 2. BoxCollider2D (Wall Obstacle)
            BoxCollider2D boxCol = null;
            if (newAutoCreateWallCollider)
            {
                boxCol = rootObj.AddComponent<BoxCollider2D>();
                if (newBuildingSprite != null)
                {
                    Bounds b = newBuildingSprite.bounds;
                    float h = b.size.y * 0.65f;
                    float offsetY = (b.extents.y - h * 0.5f);
                    boxCol.size = new Vector2(b.size.x * 0.95f, h);
                    boxCol.offset = new Vector2(0f, offsetY);
                }
                else
                {
                    boxCol.size = new Vector2(3f, 2f);
                    boxCol.offset = new Vector2(0f, 0.5f);
                }
            }

            // 3. CustomBuildingController
            var ctrl = rootObj.AddComponent<CustomBuildingController>();
            ctrl.BuildingId = string.IsNullOrEmpty(newBuildingId) ? $"bldg_{rootObj.GetInstanceID()}" : newBuildingId;
            ctrl.BuildingDisplayName = string.IsNullOrEmpty(newBuildingName) ? "Công Trình" : newBuildingName;
            ctrl.BuildingRenderer = sr;
            ctrl.WallObstacleCollider = boxCol;

            ctrl.DoorLocalOffset = newDoorOffset;
            ctrl.InteractionRadius = newInteractionRadius;
            ctrl.ActionPromptText = string.IsNullOrEmpty(newActionPrompt) ? "Tương Tác" : newActionPrompt;

            ctrl.AssignedFunction = newFunction;
            ctrl.TargetSceneName = newTargetScene;
            ctrl.TargetArrivalPosition = newTargetArrivalPos;
            ctrl.TransitionLoreQuote = newLoreQuote;
            ctrl.TargetVillager = newTargetVillager;
            ctrl.LoreTitle = newLoreTitle;
            ctrl.LoreContent = newLoreText;
            ctrl.InteriorTeleportTarget = newInteriorTeleport;

            // Đánh dấu Scene Dirty
            EditorSceneManager.MarkSceneDirty(rootObj.scene);

            // Focus SceneView vào công trình vừa tạo
            Selection.activeGameObject = rootObj;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            Debug.Log($"<color=green>[BuildingDesigner]</color> Đã tạo thành công công trình '{ctrl.BuildingDisplayName}' tại {spawnWorldPos}!");
        }

        // =========================================================================
        // TAB 2: QUẢN LÝ DANH SÁCH TRONG SCENE
        // =========================================================================
        private void DrawTabSceneList()
        {
            scrollTab2 = EditorGUILayout.BeginScrollView(scrollTab2);

            var buildings = UnityEngine.Object.FindObjectsByType<CustomBuildingController>(FindObjectsSortMode.None);
            EditorGUILayout.LabelField($"Tổng số công trình tùy biến trong Scene: {buildings.Length}", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            if (buildings.Length == 0)
            {
                EditorGUILayout.HelpBox("Chưa có công trình tùy biến nào trong Scene. Hãy sang Tab 1 hoặc Tab 3 để tạo công trình!", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < buildings.Length; i++)
                {
                    var b = buildings[i];
                    if (b == null) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label($"#{i + 1}. {b.BuildingDisplayName}", EditorStyles.boldLabel, GUILayout.Width(180));
                    GUILayout.Label($"Chức năng: {b.AssignedFunction}", GUILayout.Width(160));

                    if (GUILayout.Button("🔍 Chọn & Xem", GUILayout.Width(100)))
                    {
                        Selection.activeGameObject = b.gameObject;
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }

                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("Xóa", GUILayout.Width(50)))
                    {
                        if (EditorUtility.DisplayDialog("Xác nhận xóa", $"Bạn có chắc muốn xóa công trình {b.BuildingDisplayName}?", "Xóa", "Hủy"))
                        {
                            Undo.DestroyObjectImmediate(b.gameObject);
                            EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
                            GUIUtility.ExitGUI();
                        }
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();

                    // Thông số nhanh
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Vị trí: {b.transform.position.x:F1}, {b.transform.position.y:F1}", GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Cửa Offset: {b.DoorLocalOffset.x:F1}, {b.DoorLocalOffset.y:F1}", GUILayout.Width(160));
                    EditorGUILayout.LabelField($"Prompt: [{b.ActionPromptText}]");
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // =========================================================================
        // TAB 3: THƯ VIỆN MẪU CÓ SẴN (PRESETS)
        // =========================================================================
        private void DrawTabPresets()
        {
            scrollTab3 = EditorGUILayout.BeginScrollView(scrollTab3);

            EditorGUILayout.LabelField("Chọn nhanh các mẫu công trình hoàn chỉnh với Sprite và Chức Năng được cấu hình sẵn:", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8);

            DrawPresetItem(
                title: "1. Nhà Dân / Nhà Trưởng Làng (Vào Nhà Chuyển Scene)",
                desc: "Ngôi nhà gỗ truyền thống. Khi bước tới cửa sẽ hiện nút [E] Vào Nhà và chuyển sang Scene nội thất / trang trại.",
                spritePath: "Assets/Art/Sprites/Environment/Village/building_tam_va_workshop.png",
                func: BuildingInteractionFunction.VaoNhaScene,
                prompt: "Vào Nhà",
                doorOffset: new Vector2(0f, -1.8f),
                radius: 2.0f,
                targetScene: "MainFarm2D",
                arrivalPos: new Vector2(0f, -27.5f),
                quote: "\"Bạn đẩy nhẹ cánh cửa gỗ, bước vào gian nhà ấm cúng phủ ánh đèn dầu.\""
            );

            DrawPresetItem(
                title: "2. Xưởng Rèn Tám Vá (Giao Dịch Barter Đồ Rèn)",
                desc: "Xưởng cơ khí sắt vụn của Tám Vá. Bước tới cửa hiện nút [E] Gặp Tám Vá để trao đổi máy ép, búa đập xỉ, cào sắt.",
                spritePath: "Assets/Art/Sprites/Environment/Village/building_tam_va_workshop.png",
                func: BuildingInteractionFunction.GiaoDichDanLang,
                prompt: "Gặp Tám Vá (Xưởng Rèn)",
                doorOffset: new Vector2(0.5f, -1.8f),
                radius: 2.2f,
                villager: VillagerId.TamVa
            );

            DrawPresetItem(
                title: "3. Tiệm Thuốc Nam Cô Dung (Giao Dịch Thảo Dược)",
                desc: "Tiệm bốc thuốc phơi thảo dược khô. Bước tới cửa hiện nút [E] Mua Thuốc Cô Dung để trao đổi thuốc cầm máu, bột ngải cứu.",
                spritePath: "Assets/Art/Sprites/Environment/Village/building_co_dung_apothecary.png",
                func: BuildingInteractionFunction.GiaoDichDanLang,
                prompt: "Gặp Cô Dung (Tiệm Thuốc)",
                doorOffset: new Vector2(0f, -1.5f),
                radius: 2.0f,
                villager: VillagerId.CoDung
            );

            DrawPresetItem(
                title: "4. Trạm Cân Trọng Trâu Điên (Đấu Giá Cân Trọng)",
                desc: "Trạm thu mua và cân heo của Lái Buôn Anh Ca. Bước tới hiện nút [E] Đấu Giá Heo để bán heo thịt và xem giá cả.",
                spritePath: "Assets/Art/Sprites/Environment/Village/building_scale_station.png",
                func: BuildingInteractionFunction.GiaoDichDanLang,
                prompt: "Gặp Anh Ca (Trạm Cân)",
                doorOffset: new Vector2(0f, -1.6f),
                radius: 2.2f,
                villager: VillagerId.AnhCa
            );

            DrawPresetItem(
                title: "5. Chòi Lá Thảo Mộc Bà Sáu (Bùa Chú & Giải Trừ)",
                desc: "Chòi lá cổ kính của Bà Sáu. Bước tới hiện nút [E] Gặp Bà Sáu để thanh tẩy Hư Thể và mua bùa hộ mệnh.",
                spritePath: "Assets/Art/Sprites/Environment/Village/building_ba_sau_hut.png",
                func: BuildingInteractionFunction.GiaoDichDanLang,
                prompt: "Gặp Bà Sáu (Chòi Lá)",
                doorOffset: new Vector2(0f, -1.4f),
                radius: 2.0f,
                villager: VillagerId.BaSau
            );

            DrawPresetItem(
                title: "6. Bảng Tin Cốt Truyện Làng Bờ Rìa (Đọc Lore)",
                desc: "Bảng gỗ cắm ven đường ghi chép luật lệ và bí mật làng. Bước tới hiện nút [E] Đọc Bảng Tin.",
                spritePath: "Assets/Art/Sprites/Environment/Village/prop_village_notice_board.png",
                func: BuildingInteractionFunction.DocBangTinLore,
                prompt: "Đọc Bảng Tin",
                doorOffset: new Vector2(0f, -0.6f),
                radius: 1.5f,
                loreTitle: "Luật Lệ Làng Bờ Rìa",
                loreContent: "1. Đêm xuống không bước qua ranh giới đá cổ.\n2. Gia súc nhiễm khí đen phải báo ngay cho Bà Sáu xử lý.\n3. Tiền vàng của Lái Buôn Ca không hoàn lại sau khi ký sổ."
            );

            DrawPresetItem(
                title: "7. Giường Ngủ Nông Trại (Hồi 100% Thể Lực)",
                desc: "Giường ngủ nghỉ ngơi. Khi kiệt sức bước tới bấm nút sẽ ngủ hồi đầy bình Stamina và chuyển ngày mới.",
                spritePath: "Assets/Art/Sprites/Environment/Village/prop_carpenter_workbench.png",
                func: BuildingInteractionFunction.NghiNgoiGiuong,
                prompt: "Nghỉ Ngơi / Ngủ",
                doorOffset: new Vector2(0f, -0.6f),
                radius: 1.6f
            );

            EditorGUILayout.EndScrollView();
        }

        private void DrawPresetItem(
            string title, string desc, string spritePath,
            BuildingInteractionFunction func, string prompt,
            Vector2 doorOffset, float radius,
            string targetScene = "", Vector2 arrivalPos = default, string quote = "",
            VillagerId villager = VillagerId.BaSau,
            string loreTitle = "", string loreContent = "")
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("➕ Đặt Mẫu Này Vào Scene", GUILayout.Width(200), GUILayout.Height(28)))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                newBuildingSprite = sprite;
                newBuildingName = title.Substring(title.IndexOf('.') + 2);
                newBuildingId = $"preset_{func.ToString().ToLower()}_{Environment.TickCount % 1000}";
                newFunction = func;
                newActionPrompt = prompt;
                newDoorOffset = doorOffset;
                newInteractionRadius = radius;
                newTargetScene = targetScene;
                newTargetArrivalPos = arrivalPos;
                newLoreQuote = quote;
                newTargetVillager = villager;
                newLoreTitle = loreTitle;
                newLoreText = loreContent;

                CreateBuildingInScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }
}
