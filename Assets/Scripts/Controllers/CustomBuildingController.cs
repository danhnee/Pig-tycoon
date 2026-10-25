using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public enum BuildingInteractionFunction
    {
        VaoNhaScene = 0,      // Vào Nhà / Chuyển sang Scene Nội Thất hoặc Scene Đích
        TeleportNoiThat = 1,  // Dịch Chuyển Tức Thời Vào Phòng (Cùng Scene)
        GiaoDichDanLang = 2,  // Mở Giao Dịch Barter Dân Làng Bờ Rìa
        MoKhoLuuTru = 3,      // Mở Kho Chứa Đồ / Rương Nông Trại
        NghiNgoiGiuong = 4,   // Giường Ngủ / Nghỉ Ngơi Hồi Phục Thể Lực
        DocBangTinLore = 5,   // Đọc Bảng Tin / Cốt Truyện / Ghi Chú
        SuKienTuyBien = 6     // Tùy Biến Sự Kiện Tự Do (UnityEvent)
    }

    /// <summary>
    /// Component công trình tự do cho phép người chơi tự gắn Sprite công trình,
    /// tự xác định vị trí cửa nhà (Door Point), vùng xuất hiện nút tương tác,
    /// và tự gán chức năng có sẵn (Vào nhà, Barter, Mở kho, Ngủ, Đọc Lore...).
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class CustomBuildingController : MonoBehaviour
    {
        [Header("1. Định Danh & Hình Ảnh Công Trình")]
        public string BuildingId = "building_custom";
        public string BuildingDisplayName = "Công Trình";
        public SpriteRenderer BuildingRenderer;
        public bool AutoDynamicSorting = true;
        public int SortingPrecision = 100;
        public int SortingOffset = 0;

        [Header("2. Tường Va Chạm (Obstacle Collider)")]
        [Tooltip("Collider ngăn người chơi đi xuyên qua tường nhà")]
        public Collider2D WallObstacleCollider;

        [Header("3. Vị Trí Cửa Nhà & Vùng Tương Tác")]
        [Tooltip("Tọa độ cửa nhà tương đối so với tâm công trình (Local Offset)")]
        public Vector2 DoorLocalOffset = new Vector2(0f, -1.2f);
        [Tooltip("Bán kính phát hiện khi người chơi bước tới cửa sẽ hiện nút")]
        public float InteractionRadius = 1.8f;
        public bool UseBoxTrigger = false;
        public Vector2 InteractionBoxSize = new Vector2(2.0f, 1.5f);

        [Header("4. Nút Tương Tác Hiển Thị")]
        [Tooltip("Chữ hiển thị trên nút tương tác (ví dụ: 'Vào Nhà', 'Mở Cửa', 'Xem Kho')")]
        public string ActionPromptText = "Vào Nhà";
        public string SubPromptInfo = "Nhấn [E] hoặc nhấp chuột để tương tác";
        public bool ShowWorldSpacePrompt = true;

        [Header("5. Chức Năng Gán Cho Công Trình (Function Selector)")]
        public BuildingInteractionFunction AssignedFunction = BuildingInteractionFunction.VaoNhaScene;

        [Header("5.1. Cấu Hình: Vào Nhà / Chuyển Scene")]
        [Tooltip("Tên Scene đích cần nạp (ví dụ: MainFarm2D, BoRiaVillage2D, HouseInterior2D...)")]
        public string TargetSceneName = "MainFarm2D";
        public Vector2 TargetArrivalPosition = new Vector2(0f, -27.5f);
        [TextArea(2, 4)]
        public string TransitionLoreQuote = "\"Bạn đẩy nhẹ cánh cửa bước vào trong, hơi ấm xua tan sự lạnh lẽo của màn sương.\"";

        [Header("5.2. Cấu Hình: Dịch Chuyển Nội Thất (Cùng Scene)")]
        public Vector2 InteriorTeleportTarget = new Vector2(0f, 0f);

        [Header("5.3. Cấu Hình: Giao Dịch Dân Làng")]
        public VillagerId TargetVillager = VillagerId.BaSau;

        [Header("5.4. Cấu Hình: Bảng Tin / Lore")]
        public string LoreTitle = "Ghi Chép";
        [TextArea(3, 6)]
        public string LoreContent = "Cánh cửa gỗ đã khóa then cài cẩn thận. Bên trên có dán một mảnh bùa giấy đỏ.";

        [Header("5.5. Cấu Hình: Sự Kiện Tùy Biến")]
        public UnityEvent OnCustomAction;

        // Vị trí thế giới thực của cửa nhà
        public Vector2 DoorWorldPosition => (Vector2)transform.position + DoorLocalOffset;

        // In-world floating prompt widget
        private GameObject worldPromptObj;
        private Canvas worldPromptCanvas;
        private TextMeshProUGUI worldPromptText;
        private Button worldPromptButton;
        private bool isPlayerInRange = false;

        private void Awake()
        {
            if (BuildingRenderer == null)
            {
                BuildingRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (WallObstacleCollider == null)
            {
                // Tìm collider không phải trigger để làm tường
                var cols = GetComponents<Collider2D>();
                foreach (var c in cols)
                {
                    if (!c.isTrigger)
                    {
                        WallObstacleCollider = c;
                        break;
                    }
                }
            }

            EnsureWorldPromptUI();
        }

        private void Start()
        {
            UpdateSortingOrder();
        }

        private void OnValidate()
        {
            if (BuildingRenderer == null)
            {
                BuildingRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            UpdateSortingOrder();
        }

        private void Update()
        {
            if (AutoDynamicSorting && BuildingRenderer != null)
            {
                UpdateSortingOrder();
            }

            if (!Application.isPlaying) return;

            // Kiểm tra khoảng cách tới người chơi
            var player = PlayerMobileController.Instance;
            if (player != null)
            {
                bool inRange = IsPlayerInRange(player.transform.position);
                if (inRange != isPlayerInRange)
                {
                    isPlayerInRange = inRange;
                    SetWorldPromptActive(isPlayerInRange);
                }
            }
            else if (isPlayerInRange)
            {
                isPlayerInRange = false;
                SetWorldPromptActive(false);
            }
        }

        private void UpdateSortingOrder()
        {
            if (BuildingRenderer == null) return;
            // Sorting order theo điểm chân tường / đáy công trình
            float baseY = transform.position.y + DoorLocalOffset.y;
            BuildingRenderer.sortingOrder = Mathf.RoundToInt(-baseY * SortingPrecision) + SortingOffset;
        }

        public bool IsPlayerInRange(Vector2 playerPos)
        {
            Vector2 doorPos = DoorWorldPosition;
            if (UseBoxTrigger)
            {
                Vector2 diff = playerPos - doorPos;
                return Mathf.Abs(diff.x) <= InteractionBoxSize.x * 0.5f &&
                       Mathf.Abs(diff.y) <= InteractionBoxSize.y * 0.5f;
            }
            else
            {
                return (playerPos - doorPos).sqrMagnitude <= (InteractionRadius * InteractionRadius);
            }
        }

        public void ExecuteInteraction(PlayerMobileController player = null)
        {
            Debug.Log($"<color=yellow>[CustomBuilding]</color> Kích hoạt công trình: {BuildingDisplayName} | Chức năng: {AssignedFunction}");

            switch (AssignedFunction)
            {
                case BuildingInteractionFunction.VaoNhaScene:
                    ExecuteEnterHouseScene();
                    break;

                case BuildingInteractionFunction.TeleportNoiThat:
                    ExecuteTeleportInterior(player);
                    break;

                case BuildingInteractionFunction.GiaoDichDanLang:
                    ExecuteVillageBarter();
                    break;

                case BuildingInteractionFunction.MoKhoLuuTru:
                    ExecuteOpenStorage();
                    break;

                case BuildingInteractionFunction.NghiNgoiGiuong:
                    ExecuteRestSleep(player);
                    break;

                case BuildingInteractionFunction.DocBangTinLore:
                    ExecuteInspectLore();
                    break;

                case BuildingInteractionFunction.SuKienTuyBien:
                    OnCustomAction?.Invoke();
                    break;
            }
        }

        private void ExecuteEnterHouseScene()
        {
            if (string.IsNullOrEmpty(TargetSceneName))
            {
                Debug.LogWarning($"[CustomBuilding] Chưa cấu hình TargetSceneName cho công trình {BuildingDisplayName}!");
                return;
            }

            var tz = TransitionZoneController.Instance;
            if (tz != null)
            {
                tz.TravelToScene(TargetSceneName, TargetArrivalPosition, $"— {BuildingDisplayName.ToUpper()} —", TransitionLoreQuote);
            }
            else
            {
                SceneManager.LoadSceneAsync(TargetSceneName);
            }
        }

        private void ExecuteTeleportInterior(PlayerMobileController player)
        {
            if (player == null) player = PlayerMobileController.Instance;
            if (player != null)
            {
                player.transform.position = new Vector3(InteriorTeleportTarget.x, InteriorTeleportTarget.y, player.transform.position.z);
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;

                if (Camera.main != null)
                {
                    Camera.main.transform.position = new Vector3(InteriorTeleportTarget.x, InteriorTeleportTarget.y, Camera.main.transform.position.z);
                    Camera.main.GetComponent<CameraFollow2D>()?.SnapToTarget();
                }
            }
        }

        private void ExecuteVillageBarter()
        {
            var popup = VillageBarterPopup.Instance;
            if (popup != null)
            {
                popup.OpenForVillager(TargetVillager);
            }
            else
            {
                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                if (canvas != null)
                {
                    popup = canvas.gameObject.AddComponent<VillageBarterPopup>();
                    popup.OpenForVillager(TargetVillager);
                }
            }
        }

        private void ExecuteOpenStorage()
        {
            var bp = BackpackPopup.Instance;
            if (bp != null)
            {
                bp.Show();
                MobileGameController.Instance?.Engine?.LogMessage($"[Kho Lưu Trữ] Đã mở kho trữ vật phẩm tại {BuildingDisplayName}.");
            }
        }

        private void ExecuteRestSleep(PlayerMobileController player)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null && engine.Character != null)
            {
                var stamina = engine.Character.Stamina;
                if (stamina != null)
                {
                    stamina.DailyLoad = 0f;
                    stamina.PerformSleepReset();
                    stamina.CurrentStamina = stamina.MaxStamina;
                }
                engine.Character.Hp = engine.Character.MaxHp;

                var sleepResult = engine.Clock != null ? engine.Clock.PerformStandardSleep(true) : default;
                string sleepMsg = !string.IsNullOrEmpty(sleepResult.message) ? sleepResult.message : "Đã nghỉ ngơi.";
                string msg = $"[Nghỉ Ngơi] Bạn đã nghỉ ngơi tại {BuildingDisplayName}. {sleepMsg} Thể lực và sinh lực phục hồi 100%!";
                engine.LogMessage(msg);
                Debug.Log($"<color=green>{msg}</color>");
            }
        }

        private void ExecuteInspectLore()
        {
            string msg = $"<b>[{LoreTitle}]</b>\n{LoreContent}";
            MobileGameController.Instance?.Engine?.LogMessage($"[{LoreTitle}] {LoreContent}");
            Debug.Log($"<color=cyan>[Lore Inspect]</color> {msg}");
        }

        private void EnsureWorldPromptUI()
        {
            if (worldPromptObj != null || !ShowWorldSpacePrompt) return;

            worldPromptObj = new GameObject("Door_Floating_Prompt");
            worldPromptObj.transform.SetParent(transform, false);
            worldPromptObj.transform.localPosition = new Vector3(DoorLocalOffset.x, DoorLocalOffset.y + 0.9f, 0f);

            worldPromptCanvas = worldPromptObj.AddComponent<Canvas>();
            worldPromptCanvas.renderMode = RenderMode.WorldSpace;
            worldPromptCanvas.sortingOrder = 300;

            var rt = worldPromptCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2.4f, 0.75f);
            worldPromptObj.transform.localScale = Vector3.one * 0.9f;

            worldPromptObj.AddComponent<CanvasScaler>();
            worldPromptObj.AddComponent<GraphicRaycaster>();

            // Nền nút gỗ
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(worldPromptObj.transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = UISpriteLoader.GetFrameActionButton();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.88f, 0.65f, 0.35f, 0.95f);

            worldPromptButton = bgObj.AddComponent<Button>();
            worldPromptButton.onClick.AddListener(() => ExecuteInteraction(PlayerMobileController.Instance));

            // Text hiển thị
            var tObj = new GameObject("Text");
            tObj.transform.SetParent(bgObj.transform, false);
            var tRt = tObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            worldPromptText = tObj.AddComponent<TextMeshProUGUI>();
            worldPromptText.text = $"<b>[E] {ActionPromptText}</b>";
            worldPromptText.fontSize = 0.22f;
            worldPromptText.color = Color.white;
            worldPromptText.alignment = TextAlignmentOptions.Center;

            worldPromptObj.SetActive(false);
        }

        private void SetWorldPromptActive(bool active)
        {
            if (worldPromptObj != null && ShowWorldSpacePrompt)
            {
                if (active && worldPromptText != null)
                {
                    worldPromptText.text = $"<b>[E] {ActionPromptText}</b>";
                    worldPromptObj.transform.localPosition = new Vector3(DoorLocalOffset.x, DoorLocalOffset.y + 0.9f, 0f);
                }
                worldPromptObj.SetActive(active);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 doorPos = DoorWorldPosition;

            // 1. Vẽ vùng kích hoạt (Trigger Detection Zone)
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            if (UseBoxTrigger)
            {
                Gizmos.DrawCube(doorPos, new Vector3(InteractionBoxSize.x, InteractionBoxSize.y, 0.1f));
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(doorPos, new Vector3(InteractionBoxSize.x, InteractionBoxSize.y, 0.1f));
            }
            else
            {
                Gizmos.DrawSphere(doorPos, 0.15f);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(doorPos, InteractionRadius);
            }

            // 2. Vẽ đường nối từ tâm công trình tới cửa nhà
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, doorPos);

            // 3. Đánh dấu vị trí Cửa Nhà
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(doorPos, 0.3f);

            // 4. Vẽ tường va chạm (nếu có)
            if (WallObstacleCollider is BoxCollider2D boxCol)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
                Vector3 colCenter = transform.TransformPoint(boxCol.offset);
                Gizmos.DrawWireCube(colCenter, boxCol.size);
            }
        }
    }
}
