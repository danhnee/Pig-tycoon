using UnityEngine;
using UnityEditor;
using PigTycoon.Presentation;

namespace PigTycoon.EditorTools
{
    [CustomEditor(typeof(CustomBuildingController))]
    [CanEditMultipleObjects]
    public class CustomBuildingEditor : UnityEditor.Editor
    {
        private SerializedProperty propBuildingId;
        private SerializedProperty propBuildingDisplayName;
        private SerializedProperty propBuildingRenderer;
        private SerializedProperty propAutoDynamicSorting;
        private SerializedProperty propSortingPrecision;
        private SerializedProperty propSortingOffset;

        private SerializedProperty propWallObstacleCollider;

        private SerializedProperty propDoorLocalOffset;
        private SerializedProperty propInteractionRadius;
        private SerializedProperty propUseBoxTrigger;
        private SerializedProperty propInteractionBoxSize;

        private SerializedProperty propActionPromptText;
        private SerializedProperty propSubPromptInfo;
        private SerializedProperty propShowWorldSpacePrompt;

        private SerializedProperty propAssignedFunction;
        private SerializedProperty propTargetSceneName;
        private SerializedProperty propTargetArrivalPosition;
        private SerializedProperty propTransitionLoreQuote;

        private SerializedProperty propInteriorTeleportTarget;
        private SerializedProperty propTargetVillager;
        private SerializedProperty propLoreTitle;
        private SerializedProperty propLoreContent;
        private SerializedProperty propOnCustomAction;

        private void OnEnable()
        {
            propBuildingId = serializedObject.FindProperty("BuildingId");
            propBuildingDisplayName = serializedObject.FindProperty("BuildingDisplayName");
            propBuildingRenderer = serializedObject.FindProperty("BuildingRenderer");
            propAutoDynamicSorting = serializedObject.FindProperty("AutoDynamicSorting");
            propSortingPrecision = serializedObject.FindProperty("SortingPrecision");
            propSortingOffset = serializedObject.FindProperty("SortingOffset");

            propWallObstacleCollider = serializedObject.FindProperty("WallObstacleCollider");

            propDoorLocalOffset = serializedObject.FindProperty("DoorLocalOffset");
            propInteractionRadius = serializedObject.FindProperty("InteractionRadius");
            propUseBoxTrigger = serializedObject.FindProperty("UseBoxTrigger");
            propInteractionBoxSize = serializedObject.FindProperty("InteractionBoxSize");

            propActionPromptText = serializedObject.FindProperty("ActionPromptText");
            propSubPromptInfo = serializedObject.FindProperty("SubPromptInfo");
            propShowWorldSpacePrompt = serializedObject.FindProperty("ShowWorldSpacePrompt");

            propAssignedFunction = serializedObject.FindProperty("AssignedFunction");
            propTargetSceneName = serializedObject.FindProperty("TargetSceneName");
            propTargetArrivalPosition = serializedObject.FindProperty("TargetArrivalPosition");
            propTransitionLoreQuote = serializedObject.FindProperty("TransitionLoreQuote");

            propInteriorTeleportTarget = serializedObject.FindProperty("InteriorTeleportTarget");
            propTargetVillager = serializedObject.FindProperty("TargetVillager");
            propLoreTitle = serializedObject.FindProperty("LoreTitle");
            propLoreContent = serializedObject.FindProperty("LoreContent");
            propOnCustomAction = serializedObject.FindProperty("OnCustomAction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var bldg = (CustomBuildingController)target;

            // Banner hướng dẫn
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏠 CÔNG CỤ THIẾT KẾ CÔNG TRÌNH TỰ DO (CUSTOM BUILDING)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Bạn có thể dùng chuột kéo thả trực tiếp 'Door Handle' màu vàng trong Scene View.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Vùng màu xanh lá là bán kính kích hoạt nút tương tác khi nhân vật bước tới.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 1. Thông tin chung
            EditorGUILayout.LabelField("1. Định Danh & Hình Ảnh (Art)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propBuildingId, new GUIContent("Mã Công Trình (ID)"));
            EditorGUILayout.PropertyField(propBuildingDisplayName, new GUIContent("Tên Hiển Thị (Display Name)"));
            EditorGUILayout.PropertyField(propBuildingRenderer, new GUIContent("Sprite Renderer"));
            EditorGUILayout.PropertyField(propAutoDynamicSorting, new GUIContent("Tự Động Dynamic Sorting"));
            if (propAutoDynamicSorting.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(propSortingPrecision, new GUIContent("Độ Chính Xác Y (Precision)"));
                EditorGUILayout.PropertyField(propSortingOffset, new GUIContent("Sorting Offset"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);

            // 2. Tường va chạm
            EditorGUILayout.LabelField("2. Tường Va Chạm (Wall Collider)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propWallObstacleCollider, new GUIContent("Collider Chặn Bước Đi"));
            if (GUILayout.Button("🛠️ Tự Động Khớp Tường Va Chạm (Fit Wall Collider)", GUILayout.Height(24)))
            {
                FitWallCollider(bldg);
            }

            EditorGUILayout.Space(6);

            // 3. Vị trí cửa & vùng phát hiện
            EditorGUILayout.LabelField("3. Vị Trí Cửa & Vùng Kích Hoạt Nút", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propDoorLocalOffset, new GUIContent("Tọa Độ Cửa (Door Local Offset)"));
            EditorGUILayout.PropertyField(propUseBoxTrigger, new GUIContent("Dùng Vùng Hình Hộp (Box Trigger)"));
            if (propUseBoxTrigger.boolValue)
            {
                EditorGUILayout.PropertyField(propInteractionBoxSize, new GUIContent("Kích Thước Vùng Hộp (W, H)"));
            }
            else
            {
                EditorGUILayout.PropertyField(propInteractionRadius, new GUIContent("Bán Kính Nhận Diện (Radius)"));
            }

            // Các nút căn vị trí nhanh
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Căn Cửa Giữa - Đáy", GUILayout.Height(22)))
            {
                AlignDoor(bldg, 0f);
            }
            if (GUILayout.Button("Căn Cửa Trái", GUILayout.Height(22)))
            {
                AlignDoor(bldg, -0.6f);
            }
            if (GUILayout.Button("Căn Cửa Phải", GUILayout.Height(22)))
            {
                AlignDoor(bldg, 0.6f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // 4. Nút tương tác
            EditorGUILayout.LabelField("4. Nút Tương Tác Hiển Thị", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propActionPromptText, new GUIContent("Chữ Trên Nút (Action Prompt)"));
            EditorGUILayout.PropertyField(propSubPromptInfo, new GUIContent("Ghi Chú Phụ"));
            EditorGUILayout.PropertyField(propShowWorldSpacePrompt, new GUIContent("Hiện Nút Bay Trên Cửa (In-World)"));

            EditorGUILayout.Space(6);

            // 5. Gán chức năng
            EditorGUILayout.LabelField("5. Chức Năng Hoạt Động (Assigned Function)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propAssignedFunction, new GUIContent("Chọn Chức Năng"));

            var func = (BuildingInteractionFunction)propAssignedFunction.enumValueIndex;
            EditorGUI.indentLevel++;
            switch (func)
            {
                case BuildingInteractionFunction.VaoNhaScene:
                    EditorGUILayout.HelpBox("Chức năng Vào Nhà: Khi bấm nút sẽ chuyển cảnh sang Scene nội thất hoặc Scene khác với hiệu ứng chuyển cảnh mượt mà.", MessageType.Info);
                    EditorGUILayout.PropertyField(propTargetSceneName, new GUIContent("Tên Scene Đích"));
                    EditorGUILayout.PropertyField(propTargetArrivalPosition, new GUIContent("Tọa Độ Xuất Hiện (Target Pos)"));
                    EditorGUILayout.PropertyField(propTransitionLoreQuote, new GUIContent("Lời Dẫn Khi Vào Cửa (Quote)"));
                    break;

                case BuildingInteractionFunction.TeleportNoiThat:
                    EditorGUILayout.HelpBox("Dịch chuyển nhân vật đến một phòng hoặc vị trí khác trong cùng một Scene.", MessageType.Info);
                    EditorGUILayout.PropertyField(propInteriorTeleportTarget, new GUIContent("Tọa Độ Đích Đến"));
                    break;

                case BuildingInteractionFunction.GiaoDichDanLang:
                    EditorGUILayout.HelpBox("Mở giao diện trao đổi / Barter với NPC dân làng tương ứng.", MessageType.Info);
                    EditorGUILayout.PropertyField(propTargetVillager, new GUIContent("Dân Làng Bờ Rìa"));
                    break;

                case BuildingInteractionFunction.MoKhoLuuTru:
                    EditorGUILayout.HelpBox("Mở rương chứa đồ / balo nông trại để cất và lấy vật phẩm.", MessageType.Info);
                    break;

                case BuildingInteractionFunction.NghiNgoiGiuong:
                    EditorGUILayout.HelpBox("Giường nghỉ ngơi: Hồi phục 100% thể lực (Stamina) và qua ngày mới nếu cần.", MessageType.Info);
                    break;

                case BuildingInteractionFunction.DocBangTinLore:
                    EditorGUILayout.HelpBox("Đọc thông báo, cốt truyện, hoặc ghi chú gắn trên tường / trước cửa.", MessageType.Info);
                    EditorGUILayout.PropertyField(propLoreTitle, new GUIContent("Tiêu Đề"));
                    EditorGUILayout.PropertyField(propLoreContent, new GUIContent("Nội Dung Ghi Chép"));
                    break;

                case BuildingInteractionFunction.SuKienTuyBien:
                    EditorGUILayout.HelpBox("Tùy biến tự do bằng UnityEvent - Cho phép gọi bất kỳ hàm C# nào trong dự án.", MessageType.Info);
                    EditorGUILayout.PropertyField(propOnCustomAction, new GUIContent("Sự Kiện UnityEvent"));
                    break;
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var bldg = (CustomBuildingController)target;
            if (bldg == null) return;

            Vector3 currentDoorWorld = bldg.DoorWorldPosition;

            // 1. Draggable handle cho vị trí Cửa Nhà
            EditorGUI.BeginChangeCheck();
            Vector3 newDoorWorld = Handles.PositionHandle(currentDoorWorld, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(bldg, "Move Building Door Offset");
                Vector2 newOffset = newDoorWorld - bldg.transform.position;
                bldg.DoorLocalOffset = newOffset;
                EditorUtility.SetDirty(bldg);
            }

            // 2. Vẽ nhãn Scene View
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(1f, 0.95f, 0.4f) },
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };

            Handles.Label(currentDoorWorld + Vector3.up * 0.4f, $"🚪 CỬA: {bldg.BuildingDisplayName}\n[{bldg.ActionPromptText}]", labelStyle);

            // 3. Vẽ đĩa bán kính tương tác
            Handles.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            if (!bldg.UseBoxTrigger)
            {
                Handles.DrawSolidDisc(currentDoorWorld, Vector3.forward, bldg.InteractionRadius);
                Handles.color = Color.green;
                Handles.DrawWireDisc(currentDoorWorld, Vector3.forward, bldg.InteractionRadius);
            }
            else
            {
                Vector3 sz = new Vector3(bldg.InteractionBoxSize.x, bldg.InteractionBoxSize.y, 0.05f);
                Handles.DrawWireCube(currentDoorWorld, sz);
            }
        }

        private void AlignDoor(CustomBuildingController bldg, float xRatio)
        {
            Undo.RecordObject(bldg, "Align Door Preset");
            var sr = bldg.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Bounds b = sr.sprite.bounds;
                float bottomY = sr.transform.localPosition.y - b.extents.y;
                float posX = sr.transform.localPosition.x + b.extents.x * xRatio;
                bldg.DoorLocalOffset = new Vector2(posX, bottomY);
            }
            else
            {
                bldg.DoorLocalOffset = new Vector2(xRatio * 1.5f, -1.2f);
            }
            EditorUtility.SetDirty(bldg);
        }

        private void FitWallCollider(CustomBuildingController bldg)
        {
            Undo.RecordObject(bldg, "Fit Wall Collider");
            var boxCol = bldg.GetComponent<BoxCollider2D>();
            if (boxCol == null)
            {
                boxCol = bldg.gameObject.AddComponent<BoxCollider2D>();
            }

            var sr = bldg.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Bounds b = sr.sprite.bounds;
                // Collider che phần mái và tường (phần trên 65%), chừa 35% chân tường để đi lại và đặt cửa
                float h = b.size.y * 0.65f;
                float offsetY = sr.transform.localPosition.y + (b.extents.y - h * 0.5f);
                boxCol.size = new Vector2(b.size.x * 0.95f, h);
                boxCol.offset = new Vector2(sr.transform.localPosition.x, offsetY);
            }
            else
            {
                boxCol.size = new Vector2(3f, 2f);
                boxCol.offset = new Vector2(0f, 0.5f);
            }

            bldg.WallObstacleCollider = boxCol;
            EditorUtility.SetDirty(bldg);
        }
    }
}

