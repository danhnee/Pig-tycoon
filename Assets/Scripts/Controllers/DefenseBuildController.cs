using System;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Điều phối viên Xây Dựng & Đặt Công Trình Phòng Thủ (Defense Placement Controller):
    /// Cho phép người chơi tự do chọn vị trí xây dựng Tháp Nỏ, Bẫy Chông, Thần Khí Bạch Vân
    /// với hình ảnh bóng mờ trực quan (Ghost Hologram) và hiển thị bán kính tầm bắn (Range Circle).
    /// </summary>
    public class DefenseBuildController : MonoBehaviour
    {
        public static DefenseBuildController Instance { get; private set; }

        [Header("State")]
        public bool IsInPlacementMode { get; private set; } = false;
        public DefenseBlueprint CurrentBlueprint { get; private set; }

        [Header("Visual Elements")]
        private GameObject ghostHologramObj;
        private SpriteRenderer ghostSr;
        private GameObject rangeIndicatorObj;
        private LineRenderer rangeLineRenderer;

        private Vector2 currentPlacementPos;
        private bool isPlacementValid = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private float viewSyncTimer = 1f;   // first sync after the layout's starter tower is bound

        private void Update()
        {
            viewSyncTimer -= Time.deltaTime;
            if (viewSyncTimer <= 0f) { viewSyncTimer = 1f; SyncViewsWithModel(); }

            if (!IsInPlacementMode || CurrentBlueprint == null) return;

            UpdatePlacementPosition();
            CheckPlacementValidity();
            UpdateVisuals();

            // Phím tắt xác nhận hoặc hủy
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmPlacement();
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        public void StartPlacement(string blueprintId)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var bp = engine.Defense.GetBlueprint(blueprintId);
            if (bp == null)
            {
                PlayerInteractionController.Instance?.ShowFeedback("Bản vẽ không tồn tại!");
                return;
            }

            // Kiểm tra điều kiện Phòng Xây Dựng
            if (engine.Defense.HallLevel < bp.RequiredHallLevel)
            {
                PlayerInteractionController.Instance?.ShowFeedback($"Cần Phòng Xây Dựng cấp {bp.RequiredHallLevel}!");
                return;
            }

            // GDD 13.2: cần NPC trực Phòng Xây Dựng và còn ô thi công
            var canStart = ConstructionQueue.CanStart(engine, blueprintId);
            if (!canStart.ok)
            {
                PlayerInteractionController.Instance?.ShowFeedback(canStart.message);
                return;
            }

            // Kiểm tra tiền vàng
            if (engine.Farm.Gold < bp.BuildCostGold)
            {
                PlayerInteractionController.Instance?.ShowFeedback($"Không đủ Vàng! Cần {bp.BuildCostGold} Vàng (Hiện có: {engine.Farm.Gold} Vàng).");
                return;
            }

            CurrentBlueprint = bp;
            IsInPlacementMode = true;

            EnsureHologramVisuals();
            UpdateHologramSprite();

            PlayerInteractionController.Instance?.ShowFeedback($"[Chế Độ Đặt] Chọn vị trí đặt {bp.Name}. [Chuột Trái/Space]: Đặt | [Chuột Phải/ESC]: Hủy.");
            Debug.Log($"<color=cyan>[Phòng Thủ]</color> Bắt đầu chế độ đặt: {bp.Name}");
        }

        public void CancelPlacement()
        {
            IsInPlacementMode = false;
            CurrentBlueprint = null;

            if (ghostHologramObj != null) ghostHologramObj.SetActive(false);
            if (rangeIndicatorObj != null) rangeIndicatorObj.SetActive(false);

            PlayerInteractionController.Instance?.ShowFeedback("Đã hủy chế độ đặt công trình.");
        }

        public void ConfirmPlacement()
        {
            if (!isPlacementValid || CurrentBlueprint == null)
            {
                PlayerInteractionController.Instance?.ShowFeedback("Vị trí không hợp lệ! Hãy chọn ô đất trống trong trang trại.");
                return;
            }

            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            // GDD 13.2: Phòng Xây Dựng nhận lệnh, trả vàng một lần; công trình hiện ra khi thi công xong (Core lưu vị trí,
            // SyncViewsWithModel vẽ nó và tín hiệu StructureBuilt phát lúc hoàn thành).
            var res = ConstructionQueue.TryStart(engine, CurrentBlueprint.Id, currentPlacementPos.x, currentPlacementPos.y, true);
            if (res.success) SyncConstructionSites(engine);
            CancelPlacement();
            PlayerInteractionController.Instance?.ShowFeedback(res.message); // after the "đã hủy" line of CancelPlacement
        }

        private void InstantiateBuildingInScene(DefenseBlueprint bp, DefenseBuildingInstance model, Vector2 pos)
        {
            if (bp.Category == BuildingCategory.Bay && bp.Id == "BanChong")
            {
                // Bẫy Chông Gỗ
                var trapObj = new GameObject($"SpikeTrap_{model.InstanceId.Substring(0, 8)}");
                trapObj.transform.position = pos;

                var sr = trapObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetSpikeTrapIdleSprite();
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100) - 5;

                var col = trapObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.9f, 0.6f);
                col.isTrigger = true;

                var trap = trapObj.AddComponent<SpikeTrap2DView>();
                trap.Bind(model);
            }
            else
            {
                // Tháp Canh / Công trình phòng thủ tự động (gắn DefenseTower2DView)
                var towerObj = new GameObject($"DefenseTower_{bp.Id}_{model.InstanceId.Substring(0, 8)}");
                towerObj.transform.position = pos;

                // Base Sprite
                var baseSr = towerObj.AddComponent<SpriteRenderer>();
                baseSr.sprite = UISpriteLoader.GetTowerSprite(bp.Id);
                baseSr.sortingOrder = Mathf.RoundToInt(-(pos.y - 1.12f) * 100);

                var col = towerObj.AddComponent<CircleCollider2D>();
                col.offset = new Vector2(0f, -0.35f);
                col.radius = 0.45f;
                // GDD 17: traps and deployment zones are walked over; barriers and buildings block
                col.isTrigger = bp.Category == BuildingCategory.Bay || bp.Category == BuildingCategory.TrienKhai;
                if (bp.Category == BuildingCategory.PhongThu) { col.offset = Vector2.zero; col.radius = 0.95f; }

                // Turret nòng xoay (cho các tháp có nòng xoay)
                SpriteRenderer turretSr = null;
                bool isStationary = bp.IsBachVan || bp.Category != BuildingCategory.TanCong;

                if (!isStationary)
                {
                    var turretObj = new GameObject("Turret");
                    turretObj.transform.SetParent(towerObj.transform, false);
                    turretObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    turretSr = turretObj.AddComponent<SpriteRenderer>();
                    turretSr.sprite = UISpriteLoader.GetDefenseTowerSprite();
                    turretSr.sortingOrder = baseSr.sortingOrder + 2;
                }

                var towerView = towerObj.AddComponent<DefenseTower2DView>();
                towerView.BaseSprite = baseSr;
                towerView.TurretSprite = turretSr;
                towerView.RangeRadius = bp.AttackRange;
                towerView.Bind(model);
            }
        }

        /// <summary>
        /// Keeps the scene in step with Core: after a load every placed structure (saved with its position) is drawn
        /// again, views re-bind to the restored instances, and views whose structure is gone are removed.
        /// </summary>
        private void SyncViewsWithModel()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;
            SyncConstructionSites(engine);
            var bound = new HashSet<string>();
            foreach (var tv in FindObjectsByType<DefenseTower2DView>())
            {
                if (tv == null || tv.BuildingModel == null) continue;
                var current = engine.Defense.Buildings.Find(b => b.InstanceId == tv.BuildingModel.InstanceId);
                if (current == null)
                {
                    if (tv.gameObject.name.StartsWith("DefenseTower_")) Destroy(tv.gameObject);
                    continue;
                }
                if (!ReferenceEquals(current, tv.BuildingModel)) tv.Bind(current);
                bound.Add(current.InstanceId);
            }
            foreach (var trap in FindObjectsByType<SpikeTrap2DView>())
            {
                if (trap == null || trap.BuildingModel == null) continue;
                var current = engine.Defense.Buildings.Find(b => b.InstanceId == trap.BuildingModel.InstanceId);
                if (current == null) { Destroy(trap.gameObject); continue; }
                if (!ReferenceEquals(current, trap.BuildingModel)) trap.Bind(current);
                bound.Add(current.InstanceId);
            }
            foreach (var model in engine.Defense.Buildings)
            {
                if (model == null || !model.HasPosition || bound.Contains(model.InstanceId)) continue;
                var bp = engine.Defense.GetBlueprint(model.BlueprintId);
                if (bp == null) continue;
                InstantiateBuildingInScene(bp, model, new Vector2(model.PosX, model.PosY));
            }
        }

        private readonly Dictionary<string, SpriteRenderer> constructionSites = new Dictionary<string, SpriteRenderer>();

        /// <summary>
        /// Translucent marker of the structure for every placed order still "đang thi công" (GDD 13.2); its alpha follows
        /// the progress. Markers of finished or dropped orders are removed (the real view is drawn by SyncViewsWithModel).
        /// </summary>
        private void SyncConstructionSites(GameEngine engine)
        {
            var live = new HashSet<string>();
            foreach (var order in engine.Defense.ConstructionOrders)
            {
                if (order == null || !order.HasPosition) continue;
                live.Add(order.OrderId);
                if (!constructionSites.TryGetValue(order.OrderId, out var sr) || sr == null)
                {
                    var go = new GameObject($"ConstructionSite_{order.BlueprintId}_{order.OrderId.Substring(0, Mathf.Min(14, order.OrderId.Length))}");
                    go.transform.position = new Vector3(order.PosX, order.PosY, 0f);
                    sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = order.BlueprintId == "BanChong" ? UISpriteLoader.GetSpikeTrapIdleSprite() : UISpriteLoader.GetTowerSprite(order.BlueprintId);
                    sr.sortingOrder = Mathf.RoundToInt(-(order.PosY - 1.12f) * 100);
                    constructionSites[order.OrderId] = sr;
                }
                sr.color = new Color(0.85f, 0.75f, 0.55f, 0.25f + 0.45f * order.Progress);
            }
            var stale = new List<string>();
            foreach (var kv in constructionSites)
                if (!live.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale)
            {
                if (constructionSites[id] != null) Destroy(constructionSites[id].gameObject);
                constructionSites.Remove(id);
            }
        }

        private void UpdatePlacementPosition()
        {
            var env = FarmEnvironment2D.Instance;
            var player = PlayerMobileController.Instance;

            Vector2 targetWorld = Vector2.zero;

            // Nếu có chuột di chuyển
            if (Camera.main != null)
            {
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                targetWorld = (Vector2)mouseWorld;
            }
            else if (player != null)
            {
                targetWorld = (Vector2)player.transform.position + player.FacingDirection * 1.5f;
            }

            // PixelMyth farm: the GDD 2 m build grid (Ô Xây, §3.8)
            if (env != null && !env.ApplyLegacyLayout)
            {
                currentPlacementPos = new Vector2(Mathf.Floor(targetWorld.x / 2f) * 2f + 1f, Mathf.Floor(targetWorld.y / 2f) * 2f + 1f);
            }
            // Gắn vào ô lưới Grid 1x1
            else if (env != null)
            {
                Vector2Int grid = env.WorldToGrid(targetWorld);
                currentPlacementPos = env.GridToWorldCenter(grid);
            }
            else
            {
                currentPlacementPos = new Vector2(Mathf.Round(targetWorld.x), Mathf.Round(targetWorld.y));
            }

            if (ghostHologramObj != null)
            {
                ghostHologramObj.transform.position = currentPlacementPos;
            }
            if (rangeIndicatorObj != null)
            {
                rangeIndicatorObj.transform.position = currentPlacementPos;
            }
        }

        private void CheckPlacementValidity()
        {
            var env = FarmEnvironment2D.Instance;

            // 1. Phải nằm trong nông trại
            if (env != null && !env.IsInsideFarm(currentPlacementPos))
            {
                isPlacementValid = false;
                return;
            }

            // GDD 13.2: a cell already under construction ("đang thi công") is taken
            var engineNow = MobileGameController.Instance?.Engine;
            if (engineNow != null && engineNow.Defense.ConstructionOrders.Exists(o => o.HasPosition &&
                Mathf.Abs(o.PosX - currentPlacementPos.x) < 0.95f && Mathf.Abs(o.PosY - currentPlacementPos.y) < 0.95f))
            {
                isPlacementValid = false;
                return;
            }

            // PixelMyth farm (GDD 3.8): inside the fence anything; outside (the outer ring) traps only; never on
            // a building, prop, fence segment or water (any solid collider in the 2 m cell)
            if (env != null && !env.ApplyLegacyLayout)
            {
                bool inside = env.IsInsidePasture(currentPlacementPos);
                if (!inside && (CurrentBlueprint == null || CurrentBlueprint.Category != BuildingCategory.Bay)) { isPlacementValid = false; return; }
                foreach (var c in Physics2D.OverlapBoxAll(currentPlacementPos, new Vector2(1.9f, 1.9f), 0f))
                {
                    if (c.isTrigger || c.GetComponent<PlayerMobileController>() != null || c.GetComponent<PigAgentView>() != null) continue;
                    isPlacementValid = false;
                    return;
                }
                isPlacementValid = true;
                return;
            }

            // 2. Không được đè lên chướng ngại vật cứng
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(currentPlacementPos, new Vector2(0.85f, 0.85f), 0f);
            foreach (var col in overlaps)
            {
                if (col.isTrigger) continue;
                if (col.GetComponent<PlayerMobileController>() != null) continue;
                if (col.GetComponent<DefenseTower2DView>() != null || col.GetComponent<Fence2DView>() != null || col.GetComponent<SpikeTrap2DView>() != null)
                {
                    isPlacementValid = false;
                    return;
                }
            }

            isPlacementValid = true;
        }

        private void UpdateVisuals()
        {
            Color holoColor = isPlacementValid 
                ? new Color(0.2f, 1f, 0.4f, 0.65f) // Xanh ngọc hợp lệ
                : new Color(1f, 0.25f, 0.25f, 0.65f); // Đỏ vướng vật cản

            if (ghostSr != null)
            {
                ghostSr.color = holoColor;
            }

            if (rangeLineRenderer != null)
            {
                rangeLineRenderer.startColor = holoColor;
                rangeLineRenderer.endColor = holoColor;
            }
        }

        private void EnsureHologramVisuals()
        {
            if (ghostHologramObj == null)
            {
                ghostHologramObj = new GameObject("Defense_Ghost_Hologram");
                ghostSr = ghostHologramObj.AddComponent<SpriteRenderer>();
                ghostSr.sortingOrder = 400;
            }
            ghostHologramObj.SetActive(true);

            if (rangeIndicatorObj == null)
            {
                rangeIndicatorObj = new GameObject("Defense_Range_Indicator");
                rangeLineRenderer = rangeIndicatorObj.AddComponent<LineRenderer>();
                rangeLineRenderer.useWorldSpace = false;
                rangeLineRenderer.startWidth = 0.06f;
                rangeLineRenderer.endWidth = 0.06f;
                rangeLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                rangeLineRenderer.sortingOrder = 399;
                rangeLineRenderer.loop = true;

                // Tạo 40 điểm tròn
                int segments = 40;
                rangeLineRenderer.positionCount = segments;
            }
            rangeIndicatorObj.SetActive(true);

            // Cập nhật bán kính
            float radius = CurrentBlueprint != null ? CurrentBlueprint.AttackRange : 7.0f;
            int count = 40;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                rangeLineRenderer.SetPosition(i, p);
            }
        }

        private void UpdateHologramSprite()
        {
            if (ghostSr == null || CurrentBlueprint == null) return;

            if (CurrentBlueprint.Category == BuildingCategory.Bay && CurrentBlueprint.Id == "BanChong")
            {
                ghostSr.sprite = UISpriteLoader.GetSpikeTrapIdleSprite();
            }
            else
            {
                ghostSr.sprite = UISpriteLoader.GetTowerSprite(CurrentBlueprint.Id);
            }
        }
    }
}
