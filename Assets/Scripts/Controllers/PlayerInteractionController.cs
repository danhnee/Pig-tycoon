using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class PlayerInteractionController : MonoBehaviour
    {
        public static PlayerInteractionController Instance { get; private set; }

        [Header("Interaction Settings")]
        public float InteractionRadius = 2.5f;

        [Header("Context Action Button UI")]
        public GameObject ActionButtonRoot;
        public Button ActionButton;
        public TextMeshProUGUI ActionButtonText;
        public TextMeshProUGUI FeedbackFloatingText;

        [Header("Player Carrying Resources")]
        private float currentBucketWaterLitersFallback = 0f;
        private float currentBagFeedKgFallback = 0f;
        private int carriedFencesFallback = 10;
        private int carriedFeedersFallback = 2;
        private int carriedWaterTroughsFallback = 2;

        public float CurrentBucketWaterLiters
        {
            get => MobileGameController.Instance?.Engine?.Inventory.GetQuantity("xo_nuoc") ?? currentBucketWaterLitersFallback;
            set
            {
                currentBucketWaterLitersFallback = value;
                var inventory = MobileGameController.Instance?.Engine?.Inventory;
                if (inventory != null)
                    inventory.SetQuantity("xo_nuoc", value, MaxBucketWaterLiters);
            }
        }
        public float MaxBucketWaterLiters = 50f;
        public float CurrentBagFeedKg
        {
            get => MobileGameController.Instance?.Engine?.Inventory.GetQuantity("cam_heo") ?? currentBagFeedKgFallback;
            set
            {
                currentBagFeedKgFallback = value;
                var inventory = MobileGameController.Instance?.Engine?.Inventory;
                if (inventory != null)
                    inventory.SetQuantity("cam_heo", value, MaxBagFeedKg);
            }
        }
        public float MaxBagFeedKg = 20f;
        public int CarriedFences
        {
            get => (int)(MobileGameController.Instance?.Engine?.Inventory.GetQuantity("coc_go") ?? carriedFencesFallback);
            set
            {
                carriedFencesFallback = value;
                MobileGameController.Instance?.Engine?.Inventory.SetQuantity("coc_go", Math.Max(0, value));
            }
        }
        public int CarriedFeeders
        {
            get => (int)(MobileGameController.Instance?.Engine?.Inventory.GetQuantity("mang_an") ?? carriedFeedersFallback);
            set
            {
                carriedFeedersFallback = value;
                MobileGameController.Instance?.Engine?.Inventory.SetQuantity("mang_an", Math.Max(0, value));
            }
        }
        public int CarriedWaterTroughs
        {
            get => (int)(MobileGameController.Instance?.Engine?.Inventory.GetQuantity("mang_nuoc") ?? carriedWaterTroughsFallback);
            set
            {
                carriedWaterTroughsFallback = value;
                MobileGameController.Instance?.Engine?.Inventory.SetQuantity("mang_nuoc", Math.Max(0, value));
            }
        }

        public int CarriedWoodPlanks
        {
            get => CarriedFences;
            set => CarriedFences = value;
        }

        private Component currentTarget;
        private string currentPrompt = "";
        private float feedbackTimer = 0f;

        private GameObject gridCursorObj;
        private SpriteRenderer gridCursorRenderer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (ActionButton != null)
            {
                ActionButton.onClick.AddListener(PerformAction);
            }
        }

        private void Start()
        {
            if (ActionButtonRoot != null)
            {
                var btnImg = ActionButtonRoot.GetComponent<Image>();
                if (btnImg != null)
                {
                    var spr = UISpriteLoader.GetFrameActionButton();
                    if (spr != null)
                    {
                        btnImg.sprite = spr;
                        btnImg.type = Image.Type.Sliced;
                        btnImg.color = Color.white;
                    }
                }
                var outline = ActionButtonRoot.GetComponent<Outline>();
                if (outline != null) outline.enabled = false;
            }
        }

        private void Update()
        {
            DetectNearestInteractable();

            // Phím Space hoặc E trên bàn phím cũng kích hoạt tương tác
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
            {
                PerformAction();
            }

            // Chạm hoặc Click chuột để đóng rào hoặc gỡ rào tại ô vuông chỉ định
            HandleWorldPointerInput();

            // Hiển thị khung ô vuông chỉ định trên bản đồ khi cầm Búa
            UpdateGridCursor();

            // Xử lý ẩn feedback text
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f && FeedbackFloatingText != null)
                {
                    FeedbackFloatingText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateGridCursor()
        {
            var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;
            bool isPlacementTool = (tool == StardewToolType.HangRao || tool == StardewToolType.MangAn || tool == StardewToolType.MangNuoc);
            bool isHammerTool = (tool == StardewToolType.BuaGo);

            if (!isPlacementTool && !isHammerTool)
            {
                if (gridCursorObj != null && gridCursorObj.activeSelf)
                {
                    gridCursorObj.SetActive(false);
                }
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            var env = FarmEnvironment2D.Instance;
            Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int targetTile = env != null ? env.WorldToGrid(mouseWorld) : new Vector2Int(Mathf.FloorToInt(mouseWorld.x), Mathf.FloorToInt(mouseWorld.y));
            Vector2 tileCenter = env != null ? env.GridToWorldCenter(targetTile) : new Vector2(targetTile.x + 0.5f, targetTile.y + 0.5f);

            // Kiểm tra các công trình tại vị trí con trỏ chuột
            var hitFence = env != null ? env.GetFenceAtGrid(targetTile) : null;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(tileCenter, 0.45f);
            Feeder2DView hitFeeder = null;
            WaterTrough2DView hitTrough = null;
            DefenseTower2DView hitTower = null;

            foreach (var c in colliders)
            {
                if (c == null) continue;
                if (hitFeeder == null) hitFeeder = c.GetComponentInParent<Feeder2DView>();
                if (hitTrough == null) hitTrough = c.GetComponentInParent<WaterTrough2DView>();
                if (hitTower == null) hitTower = c.GetComponentInParent<DefenseTower2DView>();
            }

            // Với Búa Gỗ: CHỈ hiển thị con trỏ khi rê chuột trúng công trình cần tháo dỡ/sửa chữa! Rê vào đất trống thì ẩn
            if (isHammerTool)
            {
                bool hasTargetStructure = (hitFence != null || hitFeeder != null || hitTrough != null || hitTower != null);
                if (!hasTargetStructure)
                {
                    if (gridCursorObj != null && gridCursorObj.activeSelf) gridCursorObj.SetActive(false);
                    return;
                }
            }

            if (gridCursorObj == null)
            {
                gridCursorObj = new GameObject("Grid_Placement_Cursor");
                gridCursorRenderer = gridCursorObj.AddComponent<SpriteRenderer>();
                var spr = UISpriteLoader.GetGridSelector();
                if (spr != null) gridCursorRenderer.sprite = spr;
                gridCursorRenderer.sortingOrder = -9000;
            }

            if (!gridCursorObj.activeSelf) gridCursorObj.SetActive(true);
            gridCursorObj.transform.position = new Vector3(tileCenter.x, tileCenter.y, 0f);

            float dist = Vector2.Distance(transform.position, tileCenter);
            const float MaxReachDistance = 3.8f;

            if (dist > MaxReachDistance)
            {
                gridCursorRenderer.color = new Color(1f, 0.3f, 0.3f, 0.35f); // Đỏ mờ: quá tầm với
                return;
            }

            // 1. Phản hồi cho Búa Gỗ (Tháo dỡ / Sửa chữa)
            if (isHammerTool)
            {
                if (hitFence != null)
                {
                    if (hitFence.CurrentHp < hitFence.MaxHp)
                        gridCursorRenderer.color = new Color(0.3f, 0.8f, 1f, 0.85f); // Xanh dương: Sửa chữa rào
                    else
                        gridCursorRenderer.color = new Color(1f, 0.8f, 0.2f, 0.85f); // Vàng hổ phách: Tháo dỡ rào
                }
                else if (hitFeeder != null || hitTrough != null || hitTower != null)
                {
                    gridCursorRenderer.color = new Color(1f, 0.8f, 0.2f, 0.85f); // Vàng hổ phách: Tháo dỡ máng / tháp
                }
                return;
            }

            // 2. Phản hồi cho Item Hàng Rào (Đặt rào 1m x 1m)
            if (tool == StardewToolType.HangRao)
            {
                if (env != null && env.HasFenceAt(targetTile))
                {
                    gridCursorRenderer.color = new Color(1f, 0.2f, 0.2f, 0.75f); // Đỏ: Đã có rào
                }
                else
                {
                    Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.35f);
                    if ((occ != null && !occ.isTrigger) || (env != null && !env.IsInsideFarm(tileCenter)))
                        gridCursorRenderer.color = new Color(1f, 0.2f, 0.2f, 0.75f); // Đỏ: Vướng vật cản / ngoài rìa
                    else
                        gridCursorRenderer.color = new Color(0.2f, 1f, 0.4f, 0.85f); // Xanh lá: Ô trống sẵn sàng đặt rào
                }
                return;
            }

            // 3. Phản hồi cho Item Máng Ăn & Item Máng Nước
            if (tool == StardewToolType.MangAn || tool == StardewToolType.MangNuoc)
            {
                Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.55f);
                if ((occ != null && !occ.isTrigger) || (env != null && !env.IsInsideFarm(tileCenter)))
                    gridCursorRenderer.color = new Color(1f, 0.2f, 0.2f, 0.75f); // Đỏ: Vướng vật cản / ngoài rìa
                else
                    gridCursorRenderer.color = new Color(0.2f, 1f, 0.4f, 0.85f); // Xanh lá: Ô trống sẵn sàng đặt máng
            }
        }

        private void HandleWorldPointerInput()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // Bỏ qua nếu chạm vào giao diện UI (Hotbar, Nút tương tác, Popup...)
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;
            if (tool != StardewToolType.BuaGo && 
                tool != StardewToolType.HangRao && 
                tool != StardewToolType.MangAn && 
                tool != StardewToolType.MangNuoc)
            {
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector3 mouseScreen = Input.mousePosition;
            Vector2 clickWorldPos = mainCam.ScreenToWorldPoint(mouseScreen);

            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var stamina = engine?.Character?.Stamina;

            Vector2Int targetTile = env != null ? env.WorldToGrid(clickWorldPos) : new Vector2Int(Mathf.FloorToInt(clickWorldPos.x), Mathf.FloorToInt(clickWorldPos.y));
            Vector2 tileCenter = env != null ? env.GridToWorldCenter(targetTile) : new Vector2(targetTile.x + 0.5f, targetTile.y + 0.5f);
            float dist = Vector2.Distance(transform.position, tileCenter);
            const float MaxReachDistance = 3.8f;

            // =================================================================
            // TRƯỜNG HỢP 1: CẦM BÚA GỖ (CHUYÊN DÙNG THÁO DỠ & SỬA CHỮA CÔNG TRÌNH)
            // =================================================================
            if (tool == StardewToolType.BuaGo)
            {
                // A. Kiểm tra click trúng Hàng Rào
                var hitFence = env != null ? env.GetFenceAtGrid(targetTile) : null;
                if (hitFence != null)
                {
                    if (dist > MaxReachDistance)
                    {
                        ShowFeedback($"Hàng rào ở quá xa tầm với! ({dist:0.0}m > {MaxReachDistance:0.0}m). Hãy bước lại gần.");
                        return;
                    }

                    if (stamina != null && stamina.CurrentStamina < 5f)
                    {
                        ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }

                    if (hitFence.CurrentHp < hitFence.MaxHp)
                    {
                        hitFence.Repair(75f);
                        if (stamina != null) stamina.CurrentStamina -= 5f;
                        ShowFeedback($"Đã sửa chữa rào tại ({targetTile.x}, {targetTile.y})! HP: {hitFence.CurrentHp:0}/{hitFence.MaxHp:0}");
                    }
                    else
                    {
                        env.RemoveFence(hitFence);
                        CarriedFences++;
                        currentTarget = null;
                        if (stamina != null) stamina.CurrentStamina -= 5f;
                        ShowFeedback($"Đã tháo dỡ Hàng Rào tại ({targetTile.x}, {targetTile.y})! Thu hồi 1 Item Hàng Rào (Hiện có: {CarriedFences} Rào)");
                    }

                    GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    UpdateActionButtonVisual();
                    return;
                }

                // B. Kiểm tra click trúng Máng Ăn hoặc Máng Nước hoặc Tháp Canh
                Collider2D[] hits = Physics2D.OverlapCircleAll(tileCenter, 0.6f);
                foreach (var col in hits)
                {
                    if (col == null) continue;
                    var feeder = col.GetComponentInParent<Feeder2DView>();
                    if (feeder != null)
                    {
                        if (dist > MaxReachDistance)
                        {
                            ShowFeedback("Máng Ăn ở quá xa tầm với! Hãy bước lại gần.");
                            return;
                        }
                        if (stamina != null && stamina.CurrentStamina < 8f)
                        {
                            ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                            return;
                        }
                        env?.RemoveFeeder(feeder);
                        if (engine?.Farm?.Infrastructure != null)
                        {
                            engine.Farm.Infrastructure.Feeders = Mathf.Max(0, engine.Farm.Infrastructure.Feeders - 1);
                        }
                        CarriedFeeders++;
                        currentTarget = null;
                        if (stamina != null) stamina.CurrentStamina -= 8f;
                        ShowFeedback($"Đã tháo dỡ Máng Ăn! Thu hồi 1 Item Máng Ăn (Hiện có: {CarriedFeeders} Máng)");

                        GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                        UpdateActionButtonVisual();
                        return;
                    }

                    var trough = col.GetComponentInParent<WaterTrough2DView>();
                    if (trough != null)
                    {
                        if (dist > MaxReachDistance)
                        {
                            ShowFeedback("Máng Nước ở quá xa tầm với! Hãy bước lại gần.");
                            return;
                        }
                        if (stamina != null && stamina.CurrentStamina < 8f)
                        {
                            ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                            return;
                        }
                        env?.RemoveWaterTrough(trough);
                        if (engine?.Farm?.Infrastructure != null)
                        {
                            engine.Farm.Infrastructure.WaterTroughs = Mathf.Max(0, engine.Farm.Infrastructure.WaterTroughs - 1);
                        }
                        CarriedWaterTroughs++;
                        currentTarget = null;
                        if (stamina != null) stamina.CurrentStamina -= 8f;
                        ShowFeedback($"Đã tháo dỡ Máng Nước! Thu hồi 1 Item Máng Nước (Hiện có: {CarriedWaterTroughs} Máng)");

                        GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                        UpdateActionButtonVisual();
                        return;
                    }

                    var tower = col.GetComponentInParent<DefenseTower2DView>();
                    if (tower != null)
                    {
                        if (dist > MaxReachDistance)
                        {
                            ShowFeedback("Tháp Canh ở quá xa tầm với! Hãy bước lại gần.");
                            return;
                        }
                        if (stamina != null && stamina.CurrentStamina < 12f)
                        {
                            ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                            return;
                        }
                        env?.RemoveDefenseTower(tower);
                        CarriedFences += 4;
                        if (engine != null) engine.Farm.Gold += 100;
                        currentTarget = null;
                        if (stamina != null) stamina.CurrentStamina -= 12f;
                        ShowFeedback("Đã tháo dỡ Tháp Canh! (Thu hồi 4 Rào & 100 Vàng)");

                        GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                        UpdateActionButtonVisual();
                        return;
                    }
                }

                // C. Click vào đất trống khi cầm Búa Gỗ: TUYỆT ĐỐI KHÔNG ĐẶT RÀO! Hướng dẫn người chơi
                ShowFeedback("Búa Gỗ chỉ dùng để tháo dỡ hoặc sửa chữa công trình. Chọn Item Hàng Rào hoặc Máng Ăn/Nước để đặt mới!");
                return;
            }

            // =================================================================
            // TRƯỜNG HỢP 2: CẦM ITEM HÀNG RÀO (ĐẶT HÀNG RÀO THEO Ô VUÔNG)
            // =================================================================
            if (tool == StardewToolType.HangRao)
            {
                if (dist > MaxReachDistance)
                {
                    ShowFeedback($"Quá xa tầm với để đặt rào! ({dist:0.0}m > {MaxReachDistance:0.0}m). Hãy bước lại gần.");
                    return;
                }

                if (CarriedFences <= 0)
                {
                    ShowFeedback("Hết Hàng Rào! Hãy dùng Búa Gỗ tháo dỡ rào cũ để thu hồi hoặc chế tạo thêm.");
                    return;
                }

                if (stamina != null && stamina.CurrentStamina < 5f)
                {
                    ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }

                if (env != null && env.HasFenceAt(targetTile))
                {
                    ShowFeedback("Ô vuông này đã có hàng rào! Dùng Búa Gỗ nếu muốn tháo dỡ.");
                    return;
                }

                if (env != null && !env.IsInsideFarm(tileCenter))
                {
                    ShowFeedback("Không thể đặt rào ngoài rìa nông trại!");
                    return;
                }

                Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.35f);
                if (occ != null && !occ.isTrigger)
                {
                    ShowFeedback("Ô vuông này đã bị vướng vật cản!");
                    return;
                }

                var newFence = env?.BuildFence(targetTile);
                if (newFence != null)
                {
                    CarriedFences--;
                    if (stamina != null) stamina.CurrentStamina -= 5f;
                    ShowFeedback($"Đã đặt Hàng Rào tại ({targetTile.x}, {targetTile.y})! Tự động nối với rào xung quanh. (Còn {CarriedFences} Rào)");
                    GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    UpdateActionButtonVisual();
                }
                return;
            }

            // =================================================================
            // TRƯỜNG HỢP 3: CẦM ITEM MÁNG ĂN (ĐẶT MÁNG ĂN HEO THEO Ô VUÔNG)
            // =================================================================
            if (tool == StardewToolType.MangAn)
            {
                if (dist > MaxReachDistance)
                {
                    ShowFeedback("Quá xa tầm với để đặt Máng Ăn! Hãy bước lại gần hơn.");
                    return;
                }

                if (CarriedFeeders <= 0)
                {
                    ShowFeedback("Hết Máng Ăn trong túi đồ! Dùng Búa Gỗ tháo dỡ máng cũ nếu muốn di dời.");
                    return;
                }

                if (stamina != null && stamina.CurrentStamina < 8f)
                {
                    ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }

                if (env != null && !env.IsInsideFarm(tileCenter))
                {
                    ShowFeedback("Không thể đặt Máng Ăn ngoài rìa nông trại!");
                    return;
                }

                Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.55f);
                if (occ != null && !occ.isTrigger)
                {
                    ShowFeedback("Vị trí này đã bị vướng vật cản, không thể đặt Máng Ăn!");
                    return;
                }

                var newFeeder = env?.BuildFeeder(targetTile);
                if (newFeeder != null)
                {
                    CarriedFeeders--;
                    if (engine?.Farm?.Infrastructure != null)
                    {
                        engine.Farm.Infrastructure.Feeders++;
                    }
                    if (stamina != null) stamina.CurrentStamina -= 8f;
                    ShowFeedback($"Đã đặt Máng Ăn mới tại ({targetTile.x}, {targetTile.y})! (Còn {CarriedFeeders} Máng)");
                    GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    UpdateActionButtonVisual();
                }
                return;
            }

            // =================================================================
            // TRƯỜNG HỢP 4: CẦM ITEM MÁNG NƯỚC (ĐẶT MÁNG NƯỚC SẠCH THEO Ô VUÔNG)
            // =================================================================
            if (tool == StardewToolType.MangNuoc)
            {
                if (dist > MaxReachDistance)
                {
                    ShowFeedback("Quá xa tầm với để đặt Máng Nước! Hãy bước lại gần hơn.");
                    return;
                }

                if (CarriedWaterTroughs <= 0)
                {
                    ShowFeedback("Hết Máng Nước trong túi đồ! Dùng Búa Gỗ tháo dỡ máng cũ nếu muốn di dời.");
                    return;
                }

                if (stamina != null && stamina.CurrentStamina < 8f)
                {
                    ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }

                if (env != null && !env.IsInsideFarm(tileCenter))
                {
                    ShowFeedback("Không thể đặt Máng Nước ngoài rìa nông trại!");
                    return;
                }

                Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.55f);
                if (occ != null && !occ.isTrigger)
                {
                    ShowFeedback("Vị trí này đã bị vướng vật cản, không thể đặt Máng Nước!");
                    return;
                }

                var newTrough = env?.BuildWaterTrough(targetTile);
                if (newTrough != null)
                {
                    CarriedWaterTroughs--;
                    if (engine?.Farm?.Infrastructure != null)
                    {
                        engine.Farm.Infrastructure.WaterTroughs++;
                    }
                    if (stamina != null) stamina.CurrentStamina -= 8f;
                    ShowFeedback($"Đã đặt Máng Nước mới tại ({targetTile.x}, {targetTile.y})! (Còn {CarriedWaterTroughs} Máng)");
                    GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    UpdateActionButtonVisual();
                }
            }
        }

        private void DetectNearestInteractable()
        {
            Vector2 playerPos = transform.position;
            Component bestTarget = null;
            float minDistSqr = InteractionRadius * InteractionRadius;

            // 1. Kiểm tra Heo gần nhất
            var pigs = FindObjectsByType<PigAgentView>();
            foreach (var p in pigs)
            {
                if (p == null || p.PigModel == null || !p.PigModel.IsAlive) continue;
                float dSqr = ((Vector2)p.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 4.8f)
                {
                    minDistSqr = dSqr;
                    bestTarget = p;
                }
            }

            // 2. Kiểm tra Máng Ăn
            var feeders = FindObjectsByType<Feeder2DView>();
            foreach (var f in feeders)
            {
                if (f == null) continue;
                float dSqr = ((Vector2)f.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 7.5f)
                {
                    minDistSqr = dSqr;
                    bestTarget = f;
                }
            }

            // 3. Kiểm tra Bồn Nước
            var troughs = FindObjectsByType<WaterTrough2DView>();
            foreach (var w in troughs)
            {
                if (w == null) continue;
                float dSqr = ((Vector2)w.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 7.5f)
                {
                    minDistSqr = dSqr;
                    bestTarget = w;
                }
            }

            // 4. Kiểm tra Giếng Nước
            var wells = FindObjectsByType<WaterWell2DView>();
            foreach (var well in wells)
            {
                if (well == null) continue;
                float dSqr = ((Vector2)well.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 8.0f)
                {
                    minDistSqr = dSqr;
                    bestTarget = well;
                }
            }

            // 5. Kiểm tra Kho Cám (Feed Silo)
            var silos = FindObjectsByType<FeedSilo2DView>();
            foreach (var silo in silos)
            {
                if (silo == null) continue;
                float dSqr = ((Vector2)silo.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 8.0f)
                {
                    minDistSqr = dSqr;
                    bestTarget = silo;
                }
            }

            // 6. Kiểm tra Hàng Rào
            var fences = FindObjectsByType<Fence2DView>();
            foreach (var fence in fences)
            {
                if (fence == null) continue;
                float dSqr = ((Vector2)fence.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && (fence.CurrentHp < fence.MaxHp || dSqr < 3.8f))
                {
                    minDistSqr = dSqr;
                    bestTarget = fence;
                }
            }

            // 7. Kiểm tra Khu Xử Lý
            var corpseLot = FindAnyObjectByType<CorpseLot2DView>();
            if (corpseLot != null)
            {
                float dSqr = ((Vector2)corpseLot.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 9f)
                {
                    minDistSqr = dSqr;
                    bestTarget = corpseLot;
                }
            }

            // 8. Kiểm tra Tháp Canh (Defense Tower)
            var towers = FindObjectsByType<DefenseTower2DView>();
            foreach (var tower in towers)
            {
                if (tower == null) continue;
                float dSqr = ((Vector2)tower.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 9f)
                {
                    minDistSqr = dSqr;
                    bestTarget = tower;
                }
            }

            // 9. Cửa nhà Khoa: dùng nút tương tác hiện tại để vào/ra.
            foreach (var door in FindObjectsByType<KhoaHouseDoor2DView>())
            {
                if (door == null) continue;
                if (door.IsInside)
                {
                    float dSqr = ((Vector2)door.transform.position - playerPos).sqrMagnitude;
                    if (dSqr < 16.0f && (dSqr < minDistSqr || bestTarget == null))
                    {
                        minDistSqr = dSqr;
                        bestTarget = door;
                    }
                }
                else
                {
                    Vector2 housePos = door.HouseRoot != null ? (Vector2)door.HouseRoot.position : (Vector2)door.transform.position;
                    float dx = Mathf.Abs(playerPos.x - housePos.x);
                    float dy = playerPos.y - housePos.y;
                    // Rong rai toan bo khu vuc truoc hiÃªn nha Khoa
                    bool inFrontOfHouse = (dx <= 4.8f && dy >= -4.5f && dy <= -0.2f);
                    float dSqr = ((Vector2)door.transform.position - playerPos).sqrMagnitude;
                    if ((inFrontOfHouse || dSqr < 36.0f) && (dSqr < minDistSqr || bestTarget == null))
                    {
                        minDistSqr = dSqr;
                        bestTarget = door;
                    }
                }
            }

            // 10. Giường trong nhà Khoa
            foreach (var bed in FindObjectsByType<KhoaBed2DView>())
            {
                if (bed == null) continue;
                float dSqr = ((Vector2)bed.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr)
                {
                    minDistSqr = dSqr;
                    bestTarget = bed;
                }
            }

            // 11. Luong hoa hoang gia (FlowerPlotView)
            foreach (var flower in FindObjectsByType<FlowerPlotView>())
            {
                if (flower == null) continue;
                float dSqr = ((Vector2)flower.transform.position - playerPos).sqrMagnitude;
                if (dSqr < 4.0f && (dSqr < minDistSqr || bestTarget == null))
                {
                    minDistSqr = dSqr;
                    bestTarget = flower;
                }
            }

            // 12. Cong bien bao qua Lang NPC / Ve Nong Trai (VillageTravelTrigger)
            foreach (var portal in FindObjectsByType<VillageTravelTrigger>())
            {
                if (portal == null) continue;
                float dSqr = ((Vector2)portal.transform.position - playerPos).sqrMagnitude;
                if (dSqr < 9.0f && (dSqr < minDistSqr || bestTarget == null))
                {
                    minDistSqr = dSqr;
                    bestTarget = portal;
                }
            }

            // 13. Dan lang NPC (VillageNPCView)
            foreach (var npc in FindObjectsByType<VillageNPCView>())
            {
                if (npc == null) continue;
                float dSqr = ((Vector2)npc.transform.position - playerPos).sqrMagnitude;
                if (dSqr < 5.0f && (dSqr < minDistSqr || bestTarget == null))
                {
                    minDistSqr = dSqr;
                    bestTarget = npc;
                }
            }

            currentTarget = bestTarget;
            UpdateActionButtonVisual();
        }

        private void UpdateActionButtonVisual()
        {
            var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;

            // Nếu không có đối tượng cụ thể nhưng đang cầm Item Đặt Công Trình -> Cho phép đặt tại chỗ
            if (currentTarget == null)
            {
                if (tool == StardewToolType.HangRao)
                {
                    if (ActionButtonRoot != null) ActionButtonRoot.SetActive(true);
                    currentPrompt = $"Đặt Hàng Rào ({CarriedFences})";
                    if (ActionButtonText != null) ActionButtonText.text = currentPrompt;
                    return;
                }
                if (tool == StardewToolType.MangAn)
                {
                    if (ActionButtonRoot != null) ActionButtonRoot.SetActive(true);
                    currentPrompt = $"Đặt Máng Ăn ({CarriedFeeders})";
                    if (ActionButtonText != null) ActionButtonText.text = currentPrompt;
                    return;
                }
                if (tool == StardewToolType.MangNuoc)
                {
                    if (ActionButtonRoot != null) ActionButtonRoot.SetActive(true);
                    currentPrompt = $"Đặt Máng Nước ({CarriedWaterTroughs})";
                    if (ActionButtonText != null) ActionButtonText.text = currentPrompt;
                    return;
                }

                // Cầm Búa Gỗ hoặc công cụ khác trên đất trống thì ẩn nút
                if (ActionButtonRoot != null) ActionButtonRoot.SetActive(false);
                return;
            }

            if (ActionButtonRoot != null) ActionButtonRoot.SetActive(true);

            if (currentTarget is WaterWell2DView well)
            {
                if (tool == StardewToolType.XoNuoc)
                {
                    currentPrompt = CurrentBucketWaterLiters < MaxBucketWaterLiters
                        ? "Múc Nước Đầy Xô (50L)"
                        : "Xô Đã Đầy Nước (50L)";
                }
                else
                {
                    currentPrompt = "Giếng Nước (Cần Xô Nước)";
                }
            }
            else if (currentTarget is FeedSilo2DView silo)
            {
                if (tool == StardewToolType.CamHat)
                {
                    currentPrompt = CurrentBagFeedKg < MaxBagFeedKg
                        ? "Xúc Cám Vào Bao (20kg)"
                        : "Bao Cám Đã Đầy (20kg)";
                }
                else
                {
                    currentPrompt = "Kho Cám (Cần Bao Cám)";
                }
            }
            else if (currentTarget is Feeder2DView feeder)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    currentPrompt = "Tháo Dỡ Máng Ăn (Thu hồi 1 Máng)";
                }
                else if (tool == StardewToolType.CamHat)
                {
                    currentPrompt = CurrentBagFeedKg > 0f
                        ? $"Đổ Cám Vào Máng ({CurrentBagFeedKg:0}kg)"
                        : "Bao Cám Rỗng! Lại Kho Xúc";
                }
                else
                {
                    currentPrompt = "Máng Ăn (Cần Bao Cám / Búa)";
                }
            }
            else if (currentTarget is WaterTrough2DView water)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    currentPrompt = "Tháo Dỡ Bồn Nước (Thu hồi 1 Máng)";
                }
                else if (tool == StardewToolType.XoNuoc)
                {
                    currentPrompt = CurrentBucketWaterLiters > 0f
                        ? $"Đổ Nước Vào Bồn ({CurrentBucketWaterLiters:0}L)"
                        : "Xô Rỗng! Lại Giếng Múc";
                }
                else
                {
                    currentPrompt = "Bồn Nước (Cần Xô Nước / Búa)";
                }
            }
            else if (currentTarget is Fence2DView fence)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    currentPrompt = fence.CurrentHp < fence.MaxHp
                        ? $"Sửa Rào (+75 HP) [{fence.CurrentHp:0}/{fence.MaxHp:0}]"
                        : "Tháo Dỡ Rào (Thu hồi 1 Rào)";
                }
                else if (tool == StardewToolType.HangRao)
                {
                    currentPrompt = "Ô Này Đã Có Rào (Dùng Búa để tháo dỡ)";
                }
                else
                {
                    currentPrompt = $"Hàng Rào [{fence.CurrentHp:0}/{fence.MaxHp:0}]";
                }
            }
            else if (currentTarget is PigAgentView pig)
            {
                pig.EnsurePigModel();
                string pName = pig.PigModel != null ? pig.PigModel.Name : "Heo";
                if (tool == StardewToolType.BanChai)
                {
                    currentPrompt = $"Vuốt Ve {pName}";
                }
                else if (tool == StardewToolType.KinhLup)
                {
                    currentPrompt = $"Soi Gen {pName}";
                }
                else
                {
                    currentPrompt = $"Xem {pName}";
                }
            }
            else if (currentTarget is CorpseLot2DView)
            {
                currentPrompt = "Vệ Sinh Khu Xử Lý";
            }
            else if (currentTarget is DefenseTower2DView tower)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    currentPrompt = "Tháo Dỡ Tháp Canh (Thu hồi 4 Rào & 100g)";
                }
                else
                {
                    currentPrompt = "Tháp Canh (Cần Búa Gỗ)";
                }
            }
            else if (currentTarget is KhoaHouseDoor2DView door)
            {
                currentPrompt = door.IsInside ? "Ra khỏi nhà" : "Vào nhà";
            }
            else if (currentTarget is KhoaBed2DView)
            {
                currentPrompt = "Ngủ đến 06:00 sáng";
            }
            else if (currentTarget is FlowerPlotView flowerPlot)
            {
                currentPrompt = flowerPlot.IsFullyBloomed
                    ? $"Ng\u1EAFm {flowerPlot.FlowerName} (N\u1EDF R\u1ED9)"
                    : $"Xem {flowerPlot.FlowerName} (Ng\u00E0y {flowerPlot.CurrentDayAge + 1}/{flowerPlot.DaysToMature})";
            }
            else if (currentTarget is VillageTravelTrigger portal)
            {
                currentPrompt = portal.PromptText;
            }
            else if (currentTarget is VillageNPCView npc)
            {
                currentPrompt = $"Tr\u00F2 chuy\u1EC7n v\u1EDBi {npc.NPCName}";
            }

            if (ActionButtonText != null)
            {
                ActionButtonText.text = currentPrompt;
            }
        }

        public void PerformAction()
        {
            var engine = MobileGameController.Instance?.Engine;
            var stamina = engine?.Character?.Stamina;
            var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;

            // 0. Trường hợp tương tác khi đứng trên đất trống
            if (currentTarget == null)
            {
                if (tool == StardewToolType.HangRao)
                {
                    BuildFenceOnGround(stamina);
                }
                else if (tool == StardewToolType.MangAn)
                {
                    BuildFeederOnGround(stamina);
                }
                else if (tool == StardewToolType.MangNuoc)
                {
                    BuildWaterTroughOnGround(stamina);
                }
                else if (tool == StardewToolType.BuaGo)
                {
                    ShowFeedback("Búa Gỗ chỉ dùng để tháo dỡ hoặc sửa chữa công trình. Chọn Item Hàng Rào hoặc Máng Ăn/Nước để đặt mới!");
                }
                return;
            }

            // 1. Tương tác với Giếng Nước
            if (currentTarget is WaterWell2DView well)
            {
                if (tool != StardewToolType.XoNuoc)
                {
                    ShowFeedback("Hãy chọn Xô Nước trên thanh Hotbar để múc nước!");
                    return;
                }

                if (CurrentBucketWaterLiters >= MaxBucketWaterLiters)
                {
                    ShowFeedback("Xô đã đầy 50L nước! Hãy lại Bồn Nước để đổ vào bồn.");
                    return;
                }

                if (!well.HasWater)
                {
                    ShowFeedback("Giếng đang cạn! Vui lòng chờ mạch nước ngầm hồi phục.");
                    return;
                }

                if (stamina != null && stamina.CurrentStamina < 3f)
                {
                    ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }

                float need = MaxBucketWaterLiters - CurrentBucketWaterLiters;
                float drawn = well.DrawWater(need);
                CurrentBucketWaterLiters += drawn;
                if (stamina != null) stamina.CurrentStamina -= 3f;
                ShowFeedback($"Đã múc đầy {CurrentBucketWaterLiters:0}L nước từ Giếng!");
            }
            // 2. Tương tác với Kho Cám (Feed Silo)
            else if (currentTarget is FeedSilo2DView silo)
            {
                if (tool != StardewToolType.CamHat)
                {
                    ShowFeedback("Hãy chọn Bao Cám trên thanh Hotbar để xúc cám!");
                    return;
                }

                if (CurrentBagFeedKg >= MaxBagFeedKg)
                {
                    ShowFeedback("Bao cám đã đầy 20kg! Hãy lại Máng Ăn để đổ cám.");
                    return;
                }

                if (!silo.HasFeed)
                {
                    ShowFeedback("Kho Cám đã cạn thức ăn! Cần bổ sung nguồn cung nông trại.");
                    return;
                }

                if (stamina != null && stamina.CurrentStamina < 3f)
                {
                    ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }

                float need = MaxBagFeedKg - CurrentBagFeedKg;
                float taken = silo.ScoopFeed(need);
                CurrentBagFeedKg += taken;
                if (stamina != null) stamina.CurrentStamina -= 3f;
                ShowFeedback($"Đã xúc {CurrentBagFeedKg:0}kg Cám từ Kho! (Kho còn: {silo.CurrentFeedKg:0}kg)");
            }
            // 3. Tương tác với Máng Ăn (Feeder)
            else if (currentTarget is Feeder2DView feeder)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    if (stamina != null && stamina.CurrentStamina < 8f)
                    {
                        ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }
                    FarmEnvironment2D.Instance?.RemoveFeeder(feeder);
                    if (engine?.Farm?.Infrastructure != null)
                    {
                        engine.Farm.Infrastructure.Feeders = Mathf.Max(0, engine.Farm.Infrastructure.Feeders - 1);
                    }
                    CarriedFeeders++;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 8f;
                    ShowFeedback($"Đã tháo dỡ Máng Ăn và thu hồi 1 Item Máng Ăn! (Hiện có: {CarriedFeeders} Máng)");
                }
                else if (tool == StardewToolType.CamHat)
                {
                    if (CurrentBagFeedKg <= 0f)
                    {
                        ShowFeedback("Bao cám đang rỗng! Hãy lại Kho Cám để xúc cám trước.");
                        return;
                    }

                    if (stamina != null && stamina.CurrentStamina < 4f)
                    {
                        ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }

                    feeder.Refill(CurrentBagFeedKg);
                    ShowFeedback($"Đã đổ {CurrentBagFeedKg:0}kg Cám vào máng! Máng hiện có: {feeder.CurrentFoodKg:0}/{feeder.MaxFoodKg:0}kg");
                    CurrentBagFeedKg = 0f;
                    if (stamina != null) stamina.CurrentStamina -= 4f;
                }
                else
                {
                    ShowFeedback("Cần cầm Bao Cám để đổ thức ăn hoặc Búa Gỗ để tháo dỡ máng!");
                    return;
                }
            }
            // 4. Tương tác với Bồn Nước (Water Trough)
            else if (currentTarget is WaterTrough2DView water)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    if (stamina != null && stamina.CurrentStamina < 8f)
                    {
                        ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }
                    FarmEnvironment2D.Instance?.RemoveWaterTrough(water);
                    if (engine?.Farm?.Infrastructure != null)
                    {
                        engine.Farm.Infrastructure.WaterTroughs = Mathf.Max(0, engine.Farm.Infrastructure.WaterTroughs - 1);
                    }
                    CarriedWaterTroughs++;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 8f;
                    ShowFeedback($"Đã tháo dỡ Bồn Nước và thu hồi 1 Item Máng Nước! (Hiện có: {CarriedWaterTroughs} Máng)");
                }
                else if (tool == StardewToolType.XoNuoc)
                {
                    if (CurrentBucketWaterLiters <= 0f)
                    {
                        ShowFeedback("Xô nước đang rỗng! Hãy lại Giếng Nước để múc nước trước.");
                        return;
                    }

                    if (stamina != null && stamina.CurrentStamina < 4f)
                    {
                        ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }

                    water.Refill(CurrentBucketWaterLiters);
                    ShowFeedback($"Đã đổ {CurrentBucketWaterLiters:0}L Nước vào bồn! Bồn hiện có: {water.CurrentWaterLiters:0}/{water.MaxWaterLiters:0}L");
                    CurrentBucketWaterLiters = 0f;
                    if (stamina != null) stamina.CurrentStamina -= 4f;
                }
                else
                {
                    ShowFeedback("Cần cầm Xô Nước để đổ nước hoặc Búa Gỗ để tháo dỡ bồn!");
                    return;
                }
            }
            // 5. Tương tác với Hàng Rào (Fence)
            else if (currentTarget is Fence2DView fence)
            {
                if (tool != StardewToolType.BuaGo)
                {
                    ShowFeedback("Cần cầm Búa Gỗ trên thanh Hotbar để sửa hoặc tháo dỡ rào!");
                    return;
                }

                if (stamina != null && stamina.CurrentStamina < 5f)
                {
                    ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }

                if (fence.CurrentHp < fence.MaxHp)
                {
                    fence.Repair(75f);
                    if (stamina != null) stamina.CurrentStamina -= 5f;
                    ShowFeedback($"Đã sửa chữa rào! HP: {fence.CurrentHp:0}/{fence.MaxHp:0}");
                }
                else
                {
                    // Tháo dỡ rào và thu hồi 1 Item Hàng Rào
                    FarmEnvironment2D.Instance?.RemoveFence(fence);
                    CarriedFences++;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 5f;
                    ShowFeedback($"Đã tháo dỡ rào và thu hồi 1 Item Hàng Rào! (Hiện có: {CarriedFences} Rào)");
                }
            }
            // 6. Tương tác với Heo
            else if (currentTarget is PigAgentView pigAgent)
            {
                pigAgent.EnsurePigModel();
                if (pigAgent.PigModel == null) return;

                if (tool == StardewToolType.BanChai)
                {
                    if (stamina != null && stamina.CurrentStamina < 4f)
                    {
                        ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }
                    pigAgent.PigModel.Bonding = Mathf.Min(100f, pigAgent.PigModel.Bonding + 12f);
                    pigAgent.PigModel.Mood = Mathf.Min(100, pigAgent.PigModel.Mood + 15);
                    pigAgent.ShowThought("<3", 3.0f);
                    if (stamina != null) stamina.CurrentStamina -= 4f;
                    ShowFeedback($"{pigAgent.PigModel.Name} thích thú! (Bonding +12%)");
                }
                else
                {
                    if (PigInspectPopup.Instance != null)
                    {
                        PigInspectPopup.Instance.Show(pigAgent.PigModel);
                    }
                }
            }
            // 7. Tương tác với Khu Xử Lý
            else if (currentTarget is CorpseLot2DView)
            {
                if (stamina != null && stamina.CurrentStamina < 10f)
                {
                    ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }
                if (engine != null)
                {
                    engine.Farm.EventPressureIndex = Mathf.Max(0, engine.Farm.EventPressureIndex - 8);
                }
                if (stamina != null) stamina.CurrentStamina -= 10f;
                ShowFeedback("Đã vệ sinh tiêu độc! (Ám khí -8)");
            }
            // 8. Tương tác với Tháp Canh (Defense Tower)
            else if (currentTarget is DefenseTower2DView tower)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    if (stamina != null && stamina.CurrentStamina < 12f)
                    {
                        ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }
                    FarmEnvironment2D.Instance?.RemoveDefenseTower(tower);
                    CarriedFences += 4;
                    if (engine != null) engine.Farm.Gold += 100;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 12f;
                    ShowFeedback("Đã tháo dỡ Tháp Canh! (Thu hồi 4 Rào & 100 Vàng)");
                }
                else
                {
                    ShowFeedback("Cần cầm Búa Gỗ trên thanh Hotbar để tháo dỡ Tháp Canh!");
                    return;
                }
            }
            else if (currentTarget is KhoaHouseDoor2DView door)
            {
                door.Interact(transform);
                ShowFeedback(door.IsInside ? "Đã vào nhà của Khoa." : "Đã ra khỏi nhà.");
            }
            else if (currentTarget is KhoaBed2DView)
            {
                var sleepResult = engine?.PerformStandardSleep();
                ShowFeedback(sleepResult?.message ?? "Hệ thống game chưa sẵn sàng.");
            }
            else if (currentTarget is FlowerPlotView flowerPlot)
            {
                flowerPlot.Interact();
            }
            else if (currentTarget is VillageTravelTrigger portal)
            {
                portal.Travel();
            }
            else if (currentTarget is VillageNPCView npc)
            {
                npc.Talk();
            }

            GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            UpdateActionButtonVisual();
        }

        private void BuildFenceOnGround(StaminaSystem stamina)
        {
            if (CarriedFences <= 0)
            {
                ShowFeedback("Hết Hàng Rào! Hãy dùng Búa Gỗ tháo dỡ rào cũ để thu hồi hoặc chế tạo thêm.");
                return;
            }

            if (stamina != null && stamina.CurrentStamina < 5f)
            {
                ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                return;
            }

            var pCtrl = GetComponent<PlayerMobileController>();
            Vector2 facing = pCtrl != null ? pCtrl.FacingDirection : Vector2.down;
            if (facing.sqrMagnitude < 0.05f) facing = Vector2.down;

            Vector2 buildPos = (Vector2)transform.position + facing.normalized * 1.2f;
            var env = FarmEnvironment2D.Instance;
            Vector2Int gridPos = env != null ? env.WorldToGrid(buildPos) : new Vector2Int(Mathf.FloorToInt(buildPos.x), Mathf.FloorToInt(buildPos.y));
            Vector2 tileCenter = env != null ? env.GridToWorldCenter(gridPos) : new Vector2(gridPos.x + 0.5f, gridPos.y + 0.5f);

            if (env != null && env.HasFenceAt(gridPos))
            {
                ShowFeedback("Ô vuông này đã có hàng rào!");
                return;
            }

            if (env != null && !env.IsInsideFarm(tileCenter))
            {
                ShowFeedback("Không thể đóng rào ngoài rìa nông trại!");
                return;
            }

            Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.35f);
            if (occ != null && !occ.isTrigger)
            {
                ShowFeedback("Vị trí này đã bị vướng vật cản, không thể đóng rào!");
                return;
            }

            var newFence = env?.BuildFence(gridPos);
            if (newFence != null)
            {
                CarriedFences--;
                if (stamina != null) stamina.CurrentStamina -= 5f;
                ShowFeedback($"Đã đặt Hàng Rào mới tại ({gridPos.x}, {gridPos.y})! Tự động nối với rào xung quanh. (Còn {CarriedFences} Rào)");
                GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                UpdateActionButtonVisual();
            }
        }

        private void BuildFeederOnGround(StaminaSystem stamina)
        {
            if (CarriedFeeders <= 0)
            {
                ShowFeedback("Hết Máng Ăn trong túi đồ! Dùng Búa Gỗ tháo dỡ máng cũ nếu muốn di dời.");
                return;
            }

            if (stamina != null && stamina.CurrentStamina < 8f)
            {
                ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                return;
            }

            var pCtrl = GetComponent<PlayerMobileController>();
            Vector2 facing = pCtrl != null ? pCtrl.FacingDirection : Vector2.down;
            if (facing.sqrMagnitude < 0.05f) facing = Vector2.down;

            Vector2 buildPos = (Vector2)transform.position + facing.normalized * 1.5f;
            var env = FarmEnvironment2D.Instance;
            Vector2Int gridPos = env != null ? env.WorldToGrid(buildPos) : new Vector2Int(Mathf.FloorToInt(buildPos.x), Mathf.FloorToInt(buildPos.y));
            Vector2 tileCenter = env != null ? env.GridToWorldCenter(gridPos) : new Vector2(gridPos.x + 0.5f, gridPos.y + 0.5f);

            if (env != null && !env.IsInsideFarm(tileCenter))
            {
                ShowFeedback("Không thể đặt Máng Ăn ngoài rìa nông trại!");
                return;
            }

            Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.55f);
            if (occ != null && !occ.isTrigger)
            {
                ShowFeedback("Vị trí này đã bị vướng vật cản, không thể đặt Máng Ăn!");
                return;
            }

            var newFeeder = env?.BuildFeeder(gridPos);
            if (newFeeder != null)
            {
                CarriedFeeders--;
                var engine = MobileGameController.Instance?.Engine;
                if (engine?.Farm?.Infrastructure != null)
                {
                    engine.Farm.Infrastructure.Feeders++;
                }
                if (stamina != null) stamina.CurrentStamina -= 8f;
                ShowFeedback($"Đã đặt Máng Ăn mới tại ({gridPos.x}, {gridPos.y})! (Còn {CarriedFeeders} Máng)");
                GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                UpdateActionButtonVisual();
            }
        }

        private void BuildWaterTroughOnGround(StaminaSystem stamina)
        {
            if (CarriedWaterTroughs <= 0)
            {
                ShowFeedback("Hết Máng Nước trong túi đồ! Dùng Búa Gỗ tháo dỡ bồn cũ nếu muốn di dời.");
                return;
            }

            if (stamina != null && stamina.CurrentStamina < 8f)
            {
                ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                return;
            }

            var pCtrl = GetComponent<PlayerMobileController>();
            Vector2 facing = pCtrl != null ? pCtrl.FacingDirection : Vector2.down;
            if (facing.sqrMagnitude < 0.05f) facing = Vector2.down;

            Vector2 buildPos = (Vector2)transform.position + facing.normalized * 1.5f;
            var env = FarmEnvironment2D.Instance;
            Vector2Int gridPos = env != null ? env.WorldToGrid(buildPos) : new Vector2Int(Mathf.FloorToInt(buildPos.x), Mathf.FloorToInt(buildPos.y));
            Vector2 tileCenter = env != null ? env.GridToWorldCenter(gridPos) : new Vector2(gridPos.x + 0.5f, gridPos.y + 0.5f);

            if (env != null && !env.IsInsideFarm(tileCenter))
            {
                ShowFeedback("Không thể đặt Máng Nước ngoài rìa nông trại!");
                return;
            }

            Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.55f);
            if (occ != null && !occ.isTrigger)
            {
                ShowFeedback("Vị trí này đã bị vướng vật cản, không thể đặt Máng Nước!");
                return;
            }

            var newTrough = env?.BuildWaterTrough(gridPos);
            if (newTrough != null)
            {
                CarriedWaterTroughs--;
                var engine = MobileGameController.Instance?.Engine;
                if (engine?.Farm?.Infrastructure != null)
                {
                    engine.Farm.Infrastructure.WaterTroughs++;
                }
                if (stamina != null) stamina.CurrentStamina -= 8f;
                ShowFeedback($"Đã đặt Máng Nước mới tại ({gridPos.x}, {gridPos.y})! (Còn {CarriedWaterTroughs} Máng)");
                GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                UpdateActionButtonVisual();
            }
        }

        public void ShowFeedback(string message)
        {
            if (FeedbackFloatingText != null)
            {
                FeedbackFloatingText.text = message;
                FeedbackFloatingText.gameObject.SetActive(true);
                feedbackTimer = 2.5f;
            }
        }
    }
}
