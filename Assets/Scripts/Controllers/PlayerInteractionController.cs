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
        public float CurrentBucketWaterLiters = 0f;
        public float MaxBucketWaterLiters = 50f;
        public float CurrentBagFeedKg = 0f;
        public float MaxBagFeedKg = 20f;
        public int CarriedWoodPlanks = 10;

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
            if (tool != StardewToolType.BuaGo)
            {
                if (gridCursorObj != null && gridCursorObj.activeSelf)
                {
                    gridCursorObj.SetActive(false);
                }
                return;
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

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            var env = FarmEnvironment2D.Instance;
            Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int targetTile = env != null ? env.WorldToGrid(mouseWorld) : new Vector2Int(Mathf.FloorToInt(mouseWorld.x), Mathf.FloorToInt(mouseWorld.y));
            Vector2 tileCenter = env != null ? env.GridToWorldCenter(targetTile) : new Vector2(targetTile.x + 0.5f, targetTile.y + 0.5f);
            gridCursorObj.transform.position = new Vector3(tileCenter.x, tileCenter.y, 0f);

            float dist = Vector2.Distance(transform.position, tileCenter);
            const float MaxReachDistance = 3.8f;

            if (dist > MaxReachDistance)
            {
                gridCursorRenderer.color = new Color(1f, 0.3f, 0.3f, 0.35f); // Đỏ mờ: quá tầm với
            }
            else if (env != null && env.HasFenceAt(targetTile))
            {
                var fence = env.GetFenceAtGrid(targetTile);
                if (fence != null && fence.CurrentHp < fence.MaxHp)
                {
                    gridCursorRenderer.color = new Color(0.3f, 0.8f, 1f, 0.85f); // Xanh dương: sửa chữa rào
                }
                else
                {
                    gridCursorRenderer.color = new Color(1f, 0.8f, 0.2f, 0.85f); // Vàng hổ phách: tháo dỡ rào
                }
            }
            else
            {
                Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.35f);
                if ((occ != null && !occ.isTrigger) || (env != null && !env.IsInsideFarm(tileCenter)))
                {
                    gridCursorRenderer.color = new Color(1f, 0.2f, 0.2f, 0.75f); // Đỏ: vướng vật cản / ngoài rìa
                }
                else
                {
                    gridCursorRenderer.color = new Color(0.2f, 1f, 0.4f, 0.85f); // Xanh lá: ô đất trống sẵn sàng đóng rào
                }
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
            if (tool != StardewToolType.BuaGo) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector3 mouseScreen = Input.mousePosition;
            Vector2 clickWorldPos = mainCam.ScreenToWorldPoint(mouseScreen);

            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var stamina = engine?.Character?.Stamina;

            // Cố định vào ô vuông Tilemap trên map
            Vector2Int targetTile = env != null ? env.WorldToGrid(clickWorldPos) : new Vector2Int(Mathf.FloorToInt(clickWorldPos.x), Mathf.FloorToInt(clickWorldPos.y));
            Vector2 tileCenter = env != null ? env.GridToWorldCenter(targetTile) : new Vector2(targetTile.x + 0.5f, targetTile.y + 0.5f);
            float dist = Vector2.Distance(transform.position, tileCenter);
            const float MaxReachDistance = 3.8f; // Giới hạn tầm với đóng / tháo dỡ rào (3.8m)

            // 1. Kiểm tra xem ô vuông có công trình rào nào không (tháo dỡ hoặc sửa chữa)
            var hitFence = env != null ? env.GetFenceAtGrid(targetTile) : null;
            if (hitFence != null)
            {
                if (dist > MaxReachDistance)
                {
                    ShowFeedback($"Rào ở quá xa tầm với! ({dist:0.0}m > {MaxReachDistance:0.0}m). Hãy bước lại gần hơn.");
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
                    // Tháo dỡ đúng 1 cọc rào tại ô này, các cọc lân cận tự động ngắt kết nối
                    env.RemoveFence(hitFence);
                    CarriedWoodPlanks++;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 5f;
                    ShowFeedback($"Đã tháo dỡ Cọc Gỗ tại ({targetTile.x}, {targetTile.y})! Thu hồi 1 Gỗ (Hiện có: {CarriedWoodPlanks} Gỗ)");
                }

                GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                UpdateActionButtonVisual();
                return;
            }

            // 2. Nếu ô vuông là đất trống -> Đóng 1 cọc rào mới tại ô vuông đó
            if (dist > MaxReachDistance)
            {
                ShowFeedback($"Quá xa tầm với để đóng rào! ({dist:0.0}m > {MaxReachDistance:0.0}m). Hãy bước lại gần hơn.");
                return;
            }

            if (CarriedWoodPlanks <= 0)
            {
                ShowFeedback("Hết Gỗ! Hãy dùng Búa tháo dỡ cọc rào cũ để thu hồi gỗ.");
                return;
            }

            if (stamina != null && stamina.CurrentStamina < 5f)
            {
                ShowFeedback("Kiệt sức! Cần nghỉ ngơi.");
                return;
            }

            if (env != null && !env.IsInsideFarm(tileCenter))
            {
                ShowFeedback("Không thể đóng rào ngoài rìa nông trại!");
                return;
            }

            // Kiểm tra vật cản cứng tại ô vuông
            Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.35f);
            if (occ != null && !occ.isTrigger)
            {
                ShowFeedback("Ô vuông này đã bị vướng vật cản!");
                return;
            }

            // Đóng cọc rào tại ô vuông: Tự động kiểm tra và nối với các hàng rào ở ô vuông bên cạnh (Bắc, Đông, Nam, Tây)
            var newFence = env?.BuildFence(targetTile);
            if (newFence != null)
            {
                CarriedWoodPlanks--;
                if (stamina != null) stamina.CurrentStamina -= 5f;
                ShowFeedback($"Đã đóng rào mới tại ({targetTile.x}, {targetTile.y})! Tự động nối với rào xung quanh. (Còn {CarriedWoodPlanks} Gỗ)");
                GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                UpdateActionButtonVisual();
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

            currentTarget = bestTarget;
            UpdateActionButtonVisual();
        }

        private void UpdateActionButtonVisual()
        {
            var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;

            // Nếu không có đối tượng cụ thể nhưng đang cầm Búa Gỗ -> Cho phép đóng cọc rào mới trên mặt đất
            if (currentTarget == null)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    if (ActionButtonRoot != null) ActionButtonRoot.SetActive(true);
                    currentPrompt = $"Đóng Rào Mới ({CarriedWoodPlanks} Gỗ)";
                    if (ActionButtonText != null) ActionButtonText.text = currentPrompt;
                    return;
                }

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
                    currentPrompt = "Tháo Dỡ Máng Ăn (Thu hồi 2 Gỗ)";
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
                    currentPrompt = "Tháo Dỡ Bồn Nước (Thu hồi 2 Gỗ)";
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
                        : "Tháo Dỡ Rào (Thu hồi 1 Gỗ)";
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
                    currentPrompt = "Tháo Dỡ Tháp Canh (Thu hồi 4 Gỗ & 100g)";
                }
                else
                {
                    currentPrompt = "Tháp Canh (Cần Búa Gỗ)";
                }
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

            // 0. Trường hợp đóng cọc rào mới khi đứng trên đất trống
            if (currentTarget == null)
            {
                if (tool == StardewToolType.BuaGo)
                {
                    BuildFenceOnGround(stamina);
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
                    CarriedWoodPlanks += 2;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 8f;
                    ShowFeedback($"Đã tháo dỡ Máng Ăn và thu hồi 2 Cọc Gỗ! (Hiện có: {CarriedWoodPlanks} Gỗ)");
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
                    CarriedWoodPlanks += 2;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 8f;
                    ShowFeedback($"Đã tháo dỡ Bồn Nước và thu hồi 2 Cọc Gỗ! (Hiện có: {CarriedWoodPlanks} Gỗ)");
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
                    // Tháo dỡ rào và thu hồi gỗ
                    FarmEnvironment2D.Instance?.RemoveFence(fence);
                    CarriedWoodPlanks++;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 5f;
                    ShowFeedback($"Đã tháo dỡ rào và thu hồi 1 Cọc Gỗ! (Hiện có: {CarriedWoodPlanks} Gỗ)");
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
                    CarriedWoodPlanks += 4;
                    if (engine != null) engine.Farm.Gold += 100;
                    currentTarget = null;
                    if (stamina != null) stamina.CurrentStamina -= 12f;
                    ShowFeedback($"Đã tháo dỡ Tháp Canh! (Thu hồi 4 Gỗ & 100 Vàng)");
                }
                else
                {
                    ShowFeedback("Cần cầm Búa Gỗ trên thanh Hotbar để tháo dỡ Tháp Canh!");
                    return;
                }
            }

            GetComponent<CharacterSpriteAnimator>()?.TriggerAction();
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            UpdateActionButtonVisual();
        }

        private void BuildFenceOnGround(StaminaSystem stamina)
        {
            if (CarriedWoodPlanks <= 0)
            {
                ShowFeedback("Hết Gỗ! Hãy dùng Búa tháo dỡ cọc rào cũ để thu hồi gỗ.");
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

            // Kiểm tra vật cản tại vị trí đóng cọc
            Collider2D occ = Physics2D.OverlapCircle(tileCenter, 0.35f);
            if (occ != null && !occ.isTrigger)
            {
                ShowFeedback("Vị trí này đã bị vướng vật cản, không thể đóng rào!");
                return;
            }

            var newFence = env?.BuildFence(gridPos);
            if (newFence != null)
            {
                CarriedWoodPlanks--;
                if (stamina != null) stamina.CurrentStamina -= 5f;
                ShowFeedback($"Đã đóng rào mới tại ({gridPos.x}, {gridPos.y})! Tự động nối với rào xung quanh. (Còn {CarriedWoodPlanks} Gỗ)");
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
