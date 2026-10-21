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

        private void Update()
        {
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

            // Kiểm tra tiền vàng
            if (engine.Farm.Gold < bp.BuildCostGold)
            {
                PlayerInteractionController.Instance?.ShowFeedback($"Không đủ Vàng! Cần {bp.BuildCostGold} Vàng (Hiện có: {engine.Farm.Gold} Vàng).");
                return;
            }

            // Kiểm tra gỗ (nếu có)
            var pInteract = PlayerInteractionController.Instance;
            if (bp.BuildCostWood > 0 && pInteract != null && pInteract.CarriedFences < bp.BuildCostWood)
            {
                PlayerInteractionController.Instance?.ShowFeedback($"Không đủ Gỗ! Cần {bp.BuildCostWood} Gỗ (Hiện có: {pInteract.CarriedFences} Gỗ).");
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

            // 1. Trừ chi phí Vàng và Gỗ
            engine.Farm.Gold -= CurrentBlueprint.BuildCostGold;
            var pInteract = PlayerInteractionController.Instance;
            if (CurrentBlueprint.BuildCostWood > 0 && pInteract != null)
            {
                pInteract.CarriedFences = Mathf.Max(0, pInteract.CarriedFences - CurrentBlueprint.BuildCostWood);
            }

            // 2. Tạo mô hình trong Core DefenseManager
            var res = engine.Defense.ConstructBuilding(CurrentBlueprint.Id);
            if (!res.success)
            {
                PlayerInteractionController.Instance?.ShowFeedback(res.message);
                CancelPlacement();
                return;
            }

            // 3. Tạo thực thể 2D trong Scene
            InstantiateBuildingInScene(CurrentBlueprint, res.building, currentPlacementPos);

            PlayerInteractionController.Instance?.ShowFeedback($"Hoàn thành xây dựng {CurrentBlueprint.Name}!");
            CancelPlacement();
        }

        private void InstantiateBuildingInScene(DefenseBlueprint bp, DefenseBuildingInstance model, Vector2 pos)
        {
            if (bp.Category == BuildingCategory.Bay)
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
                // Tháp Canh (Nỏ Xuyên Vân hoặc Cửu Tiêu Lôi Pháo)
                var towerObj = new GameObject($"DefenseTower_{bp.Id}_{model.InstanceId.Substring(0, 8)}");
                towerObj.transform.position = pos;

                // Base Sprite
                var baseSr = towerObj.AddComponent<SpriteRenderer>();
                baseSr.sprite = bp.Id == "CuuTieuLoiPhao" 
                    ? UISpriteLoader.GetCuuTieuLoiPhaoSprite() 
                    : UISpriteLoader.GetDefenseTowerSprite();
                baseSr.sortingOrder = Mathf.RoundToInt(-(pos.y - 1.12f) * 100);

                var col = towerObj.AddComponent<CircleCollider2D>();
                col.offset = new Vector2(0f, -0.35f);
                col.radius = 0.45f;
                col.isTrigger = false;

                // Turret nòng xoay (chỉ dành cho Nỏ Xuyên Vân)
                SpriteRenderer turretSr = null;
                if (bp.Id != "CuuTieuLoiPhao")
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

            // Gắn vào ô lưới Grid 1x1
            if (env != null)
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

            if (CurrentBlueprint.Category == BuildingCategory.Bay)
            {
                ghostSr.sprite = UISpriteLoader.GetSpikeTrapIdleSprite();
            }
            else if (CurrentBlueprint.Id == "CuuTieuLoiPhao")
            {
                ghostSr.sprite = UISpriteLoader.GetCuuTieuLoiPhaoSprite();
            }
            else
            {
                ghostSr.sprite = UISpriteLoader.GetDefenseTowerSprite();
            }
        }
    }
}
