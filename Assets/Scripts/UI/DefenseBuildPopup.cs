using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Giao diện Quản Lý & Chế Tạo Phòng Thủ Trang Trại (Defense Workshop Popup):
    /// Cho phép xem bản vẽ, xây mới tháp nỏ, bẫy chông, thần khí Bạch Vân,
    /// nạp đạn nhanh, sửa chữa tháp và nâng cấp hệ thống phòng thủ trang trại.
    /// </summary>
    public class DefenseBuildPopup : MonoBehaviour
    {
        public static DefenseBuildPopup Instance { get; private set; }

        public bool IsOpen => contentPanel != null && contentPanel.activeSelf;

        private GameObject modalBackdropObj;
        private GameObject contentPanel;
        private Transform blueprintListContent;
        private Transform activeListContent;

        private TextMeshProUGUI resourceInfoText;
        private TextMeshProUGUI hallLevelText;
        private GameObject hudDefenseButtonObj;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnsureUIExists();
            Hide();
        }

        private void Update()
        {
            // Phím P hoặc H để bật/tắt menu phòng thủ
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.H))
            {
                Toggle();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                Hide();
            }
        }

        public void Toggle()
        {
            EnsureUIExists();
            if (IsOpen) Hide();
            else Show();
        }

        public void Show()
        {
            EnsureUIExists();

            if (modalBackdropObj != null)
            {
                modalBackdropObj.transform.SetAsLastSibling();
                modalBackdropObj.SetActive(true);
            }

            if (contentPanel != null)
            {
                contentPanel.transform.SetAsLastSibling();
                contentPanel.SetActive(true);
            }

            RefreshUI();
        }

        public void Hide()
        {
            if (contentPanel != null) contentPanel.SetActive(false);
            if (modalBackdropObj != null) modalBackdropObj.SetActive(false);
        }

        private void EnsureUIExists()
        {
            if (contentPanel != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var cObj = new GameObject("Defense_Canvas");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cObj.AddComponent<CanvasScaler>();
                cObj.AddComponent<GraphicRaycaster>();
            }

            EnsureHudButton(canvas);

            // 1. Modal Backdrop mờ
            modalBackdropObj = new GameObject("Defense_Backdrop");
            modalBackdropObj.transform.SetParent(canvas.transform, false);
            var bdRt = modalBackdropObj.AddComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.sizeDelta = Vector2.zero;

            var bdImg = modalBackdropObj.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            var bdBtn = modalBackdropObj.AddComponent<Button>();
            bdBtn.onClick.AddListener(Hide);

            // 2. Main Content Panel Gỗ
            contentPanel = new GameObject("Defense_Panel");
            contentPanel.transform.SetParent(canvas.transform, false);
            var cpRt = contentPanel.AddComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.5f, 0.5f);
            cpRt.anchorMax = new Vector2(0.5f, 0.5f);
            cpRt.pivot = new Vector2(0.5f, 0.5f);
            cpRt.sizeDelta = new Vector2(860f, 540f);

            var cpImg = contentPanel.AddComponent<Image>();
            cpImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            cpImg.type = Image.Type.Sliced;
            cpImg.color = new Color(0.92f, 0.88f, 0.82f, 1f);

            // Header Banner
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(contentPanel.transform, false);
            var hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.05f, 0.90f);
            hRt.anchorMax = new Vector2(0.95f, 0.98f);
            hRt.sizeDelta = Vector2.zero;

            var titleText = headerObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "<b>🛡️ XƯỞNG QUỐC PHÒNG & THÁP CANH NÔNG TRẠI</b>";
            titleText.fontSize = 24f;
            titleText.color = new Color(0.35f, 0.20f, 0.08f);
            titleText.alignment = TextAlignmentOptions.Left;

            // Nút đóng [X]
            var closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(contentPanel.transform, false);
            var cRt = closeBtnObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.93f, 0.90f);
            cRt.anchorMax = new Vector2(0.98f, 0.98f);
            cRt.sizeDelta = Vector2.zero;

            var cImg = closeBtnObj.AddComponent<Image>();
            cImg.sprite = UISpriteLoader.GetIconCloseCross();
            var closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            // Sub Header: Thông tin tài nguyên & Cấp Phòng
            var subHeaderObj = new GameObject("SubHeader");
            subHeaderObj.transform.SetParent(contentPanel.transform, false);
            var shRt = subHeaderObj.AddComponent<RectTransform>();
            shRt.anchorMin = new Vector2(0.05f, 0.82f);
            shRt.anchorMax = new Vector2(0.95f, 0.89f);
            shRt.sizeDelta = Vector2.zero;

            resourceInfoText = subHeaderObj.AddComponent<TextMeshProUGUI>();
            resourceInfoText.fontSize = 17f;
            resourceInfoText.color = new Color(0.2f, 0.35f, 0.2f);
            resourceInfoText.alignment = TextAlignmentOptions.Left;

            // Nút Nạp Đạn Nhanh Toàn Trại
            var quickReloadBtnObj = new GameObject("QuickReloadBtn");
            quickReloadBtnObj.transform.SetParent(contentPanel.transform, false);
            var qrRt = quickReloadBtnObj.AddComponent<RectTransform>();
            qrRt.anchorMin = new Vector2(0.68f, 0.82f);
            qrRt.anchorMax = new Vector2(0.95f, 0.89f);
            qrRt.sizeDelta = Vector2.zero;

            var qrImg = quickReloadBtnObj.AddComponent<Image>();
            qrImg.sprite = UISpriteLoader.GetFrameActionButton();
            qrImg.type = Image.Type.Sliced;
            qrImg.color = new Color(0.35f, 0.65f, 0.85f);
            var qrBtn = quickReloadBtnObj.AddComponent<Button>();
            qrBtn.onClick.AddListener(OnQuickReloadAllClicked);

            var qrTextObj = new GameObject("Text");
            qrTextObj.transform.SetParent(quickReloadBtnObj.transform, false);
            var qrTRt = qrTextObj.AddComponent<RectTransform>();
            qrTRt.anchorMin = Vector2.zero;
            qrTRt.anchorMax = Vector2.one;
            qrTRt.sizeDelta = Vector2.zero;
            var qrText = qrTextObj.AddComponent<TextMeshProUGUI>();
            qrText.text = "<b>⚡ NẠP ĐẦY ĐẠN TẤT CẢ THÁP</b>";
            qrText.fontSize = 14f;
            qrText.color = Color.white;
            qrText.alignment = TextAlignmentOptions.Center;

            // Cột Trái (60%): Danh sách bản vẽ phòng thủ có thể xây
            CreateBlueprintScrollView();

            // Cột Phải (35%): Danh sách công trình phòng thủ đang hoạt động trong trại
            CreateActiveBuildingsScrollView();
        }

        private void CreateBlueprintScrollView()
        {
            var svObj = new GameObject("Blueprint_ScrollView");
            svObj.transform.SetParent(contentPanel.transform, false);
            var svRt = svObj.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.04f, 0.05f);
            svRt.anchorMax = new Vector2(0.58f, 0.80f);
            svRt.sizeDelta = Vector2.zero;

            var sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            var viewObj = new GameObject("Viewport");
            viewObj.transform.SetParent(svObj.transform, false);
            var vRt = viewObj.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            viewObj.AddComponent<RectMask2D>();
            sv.viewport = vRt;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewObj.transform, false);
            var cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 600f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.content = cRt;
            blueprintListContent = contentObj.transform;
        }

        private void CreateActiveBuildingsScrollView()
        {
            var svObj = new GameObject("Active_ScrollView");
            svObj.transform.SetParent(contentPanel.transform, false);
            var svRt = svObj.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.60f, 0.05f);
            svRt.anchorMax = new Vector2(0.96f, 0.80f);
            svRt.sizeDelta = Vector2.zero;

            var sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            var viewObj = new GameObject("Viewport");
            viewObj.transform.SetParent(svObj.transform, false);
            var vRt = viewObj.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            viewObj.AddComponent<RectMask2D>();
            sv.viewport = vRt;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewObj.transform, false);
            var cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 600f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.content = cRt;
            activeListContent = contentObj.transform;
        }

        public void RefreshUI()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            int carriedWood = PlayerInteractionController.Instance != null ? PlayerInteractionController.Instance.CarriedFences : 0;
            if (resourceInfoText != null)
            {
                resourceInfoText.text = $"💰 <b>Vàng:</b> {engine.Farm.Gold:N0} G  |  🪵 <b>Gỗ Rào:</b> {carriedWood}  |  🏛️ <b>Phòng Xây Dựng:</b> Cấp {engine.Defense.HallLevel}/3";
            }

            // 1. Populate Blueprints
            if (blueprintListContent != null)
            {
                foreach (Transform child in blueprintListContent)
                {
                    Destroy(child.gameObject);
                }

                var blueprints = engine.Defense.GetAllBlueprints();
                foreach (var bp in blueprints)
                {
                    CreateBlueprintCard(bp);
                }
            }

            // 2. Populate Active Buildings
            if (activeListContent != null)
            {
                foreach (Transform child in activeListContent)
                {
                    Destroy(child.gameObject);
                }

                var buildings = engine.Defense.Buildings;
                if (buildings.Count == 0)
                {
                    var emptyObj = new GameObject("EmptyMsg");
                    emptyObj.transform.SetParent(activeListContent, false);
                    var t = emptyObj.AddComponent<TextMeshProUGUI>();
                    t.text = "<i>Chưa có công trình phòng thủ nào được xây trong trại. Hãy chọn bản vẽ bên trái để xây dựng!</i>";
                    t.fontSize = 14f;
                    t.color = Color.gray;
                }
                else
                {
                    foreach (var b in buildings)
                    {
                        CreateActiveBuildingCard(b);
                    }
                }
            }
        }

        private void CreateBlueprintCard(DefenseBlueprint bp)
        {
            var engine = MobileGameController.Instance?.Engine;
            bool isUnlocked = engine != null && engine.Defense.UnlockedBlueprints.Contains(bp.Id);
            bool canCraft = engine != null && engine.Defense.CanCraftBlueprint(bp.Id);

            var cardObj = new GameObject($"Card_{bp.Id}");
            cardObj.transform.SetParent(blueprintListContent, false);
            var le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 98f;
            le.preferredHeight = 98f;

            var bg = cardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;

            if (!isUnlocked)
            {
                bg.color = new Color(0.85f, 0.85f, 0.85f, 0.75f);
            }
            else if (bp.IsBachVan)
            {
                bg.color = new Color(0.95f, 0.90f, 0.75f);
            }
            else if (bp.Faction == "Alien")
            {
                bg.color = new Color(0.88f, 0.94f, 0.98f);
            }
            else if (bp.Faction == "Bandit")
            {
                bg.color = new Color(0.96f, 0.92f, 0.88f);
            }
            else if (bp.Faction == "BioMystic")
            {
                bg.color = new Color(0.92f, 0.96f, 0.90f);
            }
            else
            {
                bg.color = new Color(0.96f, 0.94f, 0.90f);
            }

            // Icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);
            var iRt = iconObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.02f, 0.15f);
            iRt.anchorMax = new Vector2(0.18f, 0.85f);
            iRt.sizeDelta = Vector2.zero;
            var iImg = iconObj.AddComponent<Image>();
            iImg.sprite = GetSpriteForBlueprint(bp.Id);
            iImg.preserveAspect = true;
            if (!isUnlocked) iImg.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);

            // Info Text
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var ifRt = infoObj.AddComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.20f, 0.05f);
            ifRt.anchorMax = new Vector2(0.72f, 0.95f);
            ifRt.sizeDelta = Vector2.zero;

            var ifText = infoObj.AddComponent<TextMeshProUGUI>();
            string tag = bp.IsBachVan ? "<color=#eab308>[BẠCH VÂN]</color> " :
                (bp.Faction == "Alien" ? $"<color=#0284c7>[ALIEN T{bp.Tier}]</color> " :
                (bp.Faction == "Bandit" ? $"<color=#b45309>[BĂNG CƯỚP T{bp.Tier}]</color> " :
                (bp.Faction == "BioMystic" ? $"<color=#15803d>[DỊCH HỌA T{bp.Tier}]</color> " : "")));

            string woodStr = bp.BuildCostWood > 0 ? $" + {bp.BuildCostWood} Gỗ" : "";

            // Nguyên liệu quái vật
            string matStr = "";
            if (bp.CraftingMaterials != null && bp.CraftingMaterials.Count > 0 && engine != null)
            {
                List<string> mats = new List<string>();
                foreach (var req in bp.CraftingMaterials)
                {
                    int cur = engine.Defense.GetMonsterMaterialCount(req.Key);
                    string col = cur >= req.Value ? "#16a34a" : "#dc2626";
                    mats.Add($"<color={col}>{req.Key}: {cur}/{req.Value}</color>");
                }
                matStr = "\n<size=75%>NVL: " + string.Join(" | ", mats) + "</size>";
            }

            string lockNotice = !isUnlocked ? "\n<size=80%><color=#dc2626>🔒 Chưa học Bản Vẽ (Tỉ lệ rớt từ quái vật)</color></size>" : "";

            ifText.text = $"<b>{tag}{bp.Name}</b> (Cấp {bp.RequiredHallLevel})\n" +
                          $"<size=80%><color=#4b5563>{bp.Description}</color></size>\n" +
                          $"<size=85%><b>Giá:</b> <color=#b45309>{bp.BuildCostGold:N0} Vàng{woodStr}</color> | <b>Tầm:</b> {bp.AttackRange:0}m | <b>Dame:</b> {bp.Damage:0}</size>" +
                          matStr + lockNotice;
            ifText.fontSize = 13.5f;
            ifText.color = new Color(0.2f, 0.2f, 0.2f);

            // Nút Xây Dựng
            var btnObj = new GameObject("BuildBtn");
            btnObj.transform.SetParent(cardObj.transform, false);
            var bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.74f, 0.25f);
            bRt.anchorMax = new Vector2(0.97f, 0.75f);
            bRt.sizeDelta = Vector2.zero;

            var bImg = btnObj.AddComponent<Image>();
            bImg.sprite = UISpriteLoader.GetFrameActionButton();
            bImg.type = Image.Type.Sliced;

            if (!isUnlocked)
            {
                bImg.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            }
            else if (!canCraft)
            {
                bImg.color = new Color(0.85f, 0.5f, 0.3f);
            }
            else if (bp.IsBachVan)
            {
                bImg.color = new Color(0.9f, 0.6f, 0.2f);
            }
            else
            {
                bImg.color = new Color(0.3f, 0.75f, 0.4f);
            }

            var btn = btnObj.AddComponent<Button>();
            string bId = bp.Id;
            btn.onClick.AddListener(() => OnBuildBlueprintClicked(bId));

            var btTextObj = new GameObject("Text");
            btTextObj.transform.SetParent(btnObj.transform, false);
            var btTRt = btTextObj.AddComponent<RectTransform>();
            btTRt.anchorMin = Vector2.zero;
            btTRt.anchorMax = Vector2.one;
            btTRt.sizeDelta = Vector2.zero;

            var btText = btTextObj.AddComponent<TextMeshProUGUI>();
            btText.text = !isUnlocked ? "<b>🔒 KHÓA</b>" : (!canCraft ? "<b>🔨 THIẾU</b>" : "<b>🔨 ĐẶT</b>");
            btText.fontSize = 14f;
            btText.color = Color.white;
            btText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateActiveBuildingCard(DefenseBuildingInstance b)
        {
            var cardObj = new GameObject($"Active_{b.InstanceId}");
            cardObj.transform.SetParent(activeListContent, false);
            var le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 78f;
            le.preferredHeight = 78f;

            var bg = cardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;
            bg.color = b.IsBroken ? new Color(0.95f, 0.75f, 0.75f) : new Color(0.92f, 0.94f, 0.96f);

            // Info
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var ifRt = infoObj.AddComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.04f, 0.1f);
            ifRt.anchorMax = new Vector2(0.68f, 0.9f);
            ifRt.sizeDelta = Vector2.zero;

            var ifText = infoObj.AddComponent<TextMeshProUGUI>();
            string statusStr = b.IsBroken ? "<color=red>SỤP ĐỔ</color>" : "<color=green>HOẠT ĐỘNG</color>";
            ifText.text = $"<b>{b.Name}</b> [{statusStr}]\n" +
                          $"<size=85%>Máu: <b>{b.CurrentHp}/{b.MaxHp}</b> | Đạn: <b>{b.AmmoCurrent}/{b.AmmoCapacity}</b></size>";
            ifText.fontSize = 13f;
            ifText.color = Color.black;

            // Nút Nạp Đạn / Sửa Chữa
            var actionBtnObj = new GameObject("ActionBtn");
            actionBtnObj.transform.SetParent(cardObj.transform, false);
            var abRt = actionBtnObj.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0.70f, 0.2f);
            abRt.anchorMax = new Vector2(0.96f, 0.8f);
            abRt.sizeDelta = Vector2.zero;

            var abImg = actionBtnObj.AddComponent<Image>();
            abImg.sprite = UISpriteLoader.GetFrameActionButton();
            abImg.type = Image.Type.Sliced;
            abImg.color = b.IsBroken ? new Color(0.85f, 0.4f, 0.3f) : new Color(0.3f, 0.65f, 0.85f);

            var abBtn = actionBtnObj.AddComponent<Button>();
            string instId = b.InstanceId;
            bool isBroken = b.IsBroken;
            abBtn.onClick.AddListener(() =>
            {
                if (isBroken) OnRepairBuildingClicked(instId);
                else OnReloadBuildingClicked(instId);
            });

            var abTextObj = new GameObject("Text");
            abTextObj.transform.SetParent(actionBtnObj.transform, false);
            var abTRt = abTextObj.AddComponent<RectTransform>();
            abTRt.anchorMin = Vector2.zero;
            abTRt.anchorMax = Vector2.one;
            abTRt.sizeDelta = Vector2.zero;
            var abText = abTextObj.AddComponent<TextMeshProUGUI>();
            abText.text = isBroken ? "<b>SỬA</b>" : "<b>NẠP</b>";
            abText.fontSize = 14f;
            abText.color = Color.white;
            abText.alignment = TextAlignmentOptions.Center;
        }

        private void OnBuildBlueprintClicked(string blueprintId)
        {
            Hide();
            if (DefenseBuildController.Instance != null)
            {
                DefenseBuildController.Instance.StartPlacement(blueprintId);
            }
        }

        private void OnQuickReloadAllClicked()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            if (engine.Clock != null && engine.Clock.IsDuringCombatWave)
            {
                PlayerInteractionController.Instance?.ShowFeedback("Không được nạp đạn trong khi có đợt quái tấn công!");
                return;
            }

            int reloadedTowers = 0;
            foreach (var b in engine.Defense.Buildings)
            {
                if (!b.IsBroken && b.AmmoCurrent < b.AmmoCapacity)
                {
                    int need = b.AmmoCapacity - b.AmmoCurrent;
                    engine.Defense.PreloadAmmo(b.InstanceId, need, false);
                    reloadedTowers++;
                }
            }

            // Đồng bộ sang Scene Views
            foreach (var tv in FindObjectsByType<DefenseTower2DView>())
            {
                if (tv.BuildingModel != null) tv.ReloadAmmo(tv.BuildingModel.AmmoCapacity);
            }

            PlayerInteractionController.Instance?.ShowFeedback($"Đã nạp đầy đạn cho {reloadedTowers} công trình phòng thủ!");
            RefreshUI();
        }

        private void OnReloadBuildingClicked(string instanceId)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var res = engine.Defense.PreloadAmmo(instanceId, 30, engine.Clock != null && engine.Clock.IsDuringCombatWave);
            PlayerInteractionController.Instance?.ShowFeedback(res.message);

            foreach (var tv in FindObjectsByType<DefenseTower2DView>())
            {
                if (tv.BuildingModel != null && tv.BuildingModel.InstanceId == instanceId)
                {
                    tv.ReloadAmmo(30);
                    break;
                }
            }

            RefreshUI();
        }

        private void OnRepairBuildingClicked(string instanceId)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var res = engine.Defense.RepairBuilding(instanceId, 800);
            PlayerInteractionController.Instance?.ShowFeedback(res.message);

            foreach (var tv in FindObjectsByType<DefenseTower2DView>())
            {
                if (tv.BuildingModel != null && tv.BuildingModel.InstanceId == instanceId)
                {
                    tv.Repair(800f);
                    break;
                }
            }
            foreach (var st in FindObjectsByType<SpikeTrap2DView>())
            {
                if (st.BuildingModel != null && st.BuildingModel.InstanceId == instanceId)
                {
                    st.Repair(10f);
                    break;
                }
            }

            RefreshUI();
        }

        private Sprite GetSpriteForBlueprint(string blueprintId)
        {
            return UISpriteLoader.GetTowerSprite(blueprintId);
        }

        private void EnsureHudButton(Canvas canvas)
        {
            if (hudDefenseButtonObj != null || canvas == null) return;
            hudDefenseButtonObj = new GameObject("HUD_Defense_Button");
            hudDefenseButtonObj.transform.SetParent(canvas.transform, false);

            var rt = hudDefenseButtonObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -175f);
            rt.sizeDelta = new Vector2(140f, 40f);

            var img = hudDefenseButtonObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameActionButton();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.35f, 0.55f, 0.75f, 0.95f);

            var btn = hudDefenseButtonObj.AddComponent<Button>();
            btn.onClick.AddListener(Toggle);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(hudDefenseButtonObj.transform, false);
            var tRt = textObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            var t = textObj.AddComponent<TextMeshProUGUI>();
            t.text = "<b>🛡️ Phòng Thủ [P]</b>";
            t.fontSize = 13.5f;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
        }
    }
}
