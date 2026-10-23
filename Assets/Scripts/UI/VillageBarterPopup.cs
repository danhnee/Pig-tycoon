using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Giao diện Trao Đổi & Mua Bán Dân Làng Bờ Rìa (Village Barter & Trade Popup):
    /// - Chuẩn phong cách Stardew Valley / RPG cổ điển với khung gỗ chạm khắc và giấy da ấm.
    /// - Hỗ trợ 6 dân làng: Bà Sáu, Tám Vá, Cô Dung, Anh Ca, Ông Tồn, Bí Nhân.
    /// - Hiển thị chân dung pixel-art, danh hiệu, vị trí, lời thoại và trạng thái đóng/mở cửa.
    /// - Danh sách hàng hóa scrollable mượt mà với yêu cầu GTTC rõ ràng.
    /// - Khu vực đặt hàng trao đổi (Barter Desk) trực quan cho phép chọn Vật phẩm túi đồ hoặc Heo sống.
    /// - Tự động tính toán tổng GTTC thời gian thực và thông báo luật Bờ Rìa (không thối dư).
    /// </summary>
    public class VillageBarterPopup : MonoBehaviour
    {
        public static VillageBarterPopup Instance { get; private set; }

        public GameObject PopupPanel;
        public VillagerId CurrentVillagerId = VillagerId.BaSau;

        // UI References - Header
        private GameObject modalBackdropObj;
        private TextMeshProUGUI headerTitleText;
        private TextMeshProUGUI playerGoldText;
        private TextMeshProUGUI timeInfoText;

        // UI References - Tabs
        private readonly List<Button> villagerTabButtons = new List<Button>();
        private readonly List<Image> villagerTabImages = new List<Image>();
        private readonly List<TextMeshProUGUI> villagerTabTexts = new List<TextMeshProUGUI>();

        // UI References - Profile
        private Image villagerAvatarImg;
        private TextMeshProUGUI villagerNameText;
        private TextMeshProUGUI villagerTitleText;
        private TextMeshProUGUI villagerLocationText;
        private TextMeshProUGUI atmosphereQuoteText;
        private TextMeshProUGUI statusBadgeText;

        // UI References - Listings
        private Transform listingsContainer;
        private ScrollRect listingsScrollRect;
        private TextMeshProUGUI feedbackText;

        // Barter Offering State
        private NightMarketListing selectedListing = null;
        private GameObject barterOfferingPanel;
        private TextMeshProUGUI barterTargetItemText;
        private TextMeshProUGUI barterRequirementText;
        private TextMeshProUGUI barterOfferingTallyText;
        private Button confirmBarterBtn;
        private TextMeshProUGUI confirmBarterBtnText;

        // Offering sub-tabs (0: Items, 1: Pigs)
        private int barterSubTab = 0;
        private Transform barterOfferingContent;
        private Button tabItemsBtn;
        private Button tabPigsBtn;
        private Image tabItemsImg;
        private Image tabPigsImg;
        private TextMeshProUGUI tabItemsText;
        private TextMeshProUGUI tabPigsText;

        // Selected offers
        private readonly Dictionary<string, int> selectedItemQuantities = new Dictionary<string, int>();
        private string selectedPigIdToOffer = null;

        public bool IsOpen => PopupPanel != null && PopupPanel.activeSelf;

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
            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                if (barterOfferingPanel != null && barterOfferingPanel.activeSelf)
                {
                    CloseBarterOfferingPanel();
                }
                else
                {
                    Hide();
                }
            }
        }

        public void OpenForVillager(VillagerId id)
        {
            CurrentVillagerId = id;
            EnsureUIExists();

            selectedListing = null;
            selectedItemQuantities.Clear();
            selectedPigIdToOffer = null;

            if (barterOfferingPanel != null) barterOfferingPanel.SetActive(false);
            if (modalBackdropObj != null)
            {
                modalBackdropObj.transform.SetAsLastSibling();
                modalBackdropObj.SetActive(true);
            }
            if (PopupPanel != null)
            {
                PopupPanel.transform.SetAsLastSibling();
                PopupPanel.SetActive(true);
            }

            Refresh();
        }

        public void Show()
        {
            OpenForVillager(CurrentVillagerId);
        }

        public void Hide()
        {
            if (modalBackdropObj != null) modalBackdropObj.SetActive(false);
            if (barterOfferingPanel != null) barterOfferingPanel.SetActive(false);
            if (PopupPanel != null) PopupPanel.SetActive(false);
        }

        public void Refresh()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            // 1. Cập nhật Header: Vàng và Thời gian
            if (playerGoldText != null)
            {
                playerGoldText.text = $"{engine.Farm.Gold:N0} G";
            }
            if (timeInfoText != null)
            {
                string todStr = engine.Clock.GetTimeOfDay() switch
                {
                    TimeOfDay.Sang => "Sáng",
                    TimeOfDay.Trua => "Trưa",
                    TimeOfDay.Chieu => "Chiều",
                    _ => "Tối"
                };
                timeInfoText.text = $"Ngày {engine.Clock.CurrentDay} • {engine.Clock.CurrentHour:D2}:{engine.Clock.CurrentMinute:D2} [{todStr}]";
            }

            // 2. Cập nhật Tab Buttons trạng thái hoạt động
            var vList = new VillagerId[]
            {
                VillagerId.BaSau,
                VillagerId.TamVa,
                VillagerId.CoDung,
                VillagerId.AnhCa,
                VillagerId.OngTon,
                VillagerId.BiNhan
            };

            for (int i = 0; i < vList.Length && i < villagerTabButtons.Count; i++)
            {
                bool isSelected = vList[i] == CurrentVillagerId;
                if (villagerTabImages[i] != null)
                {
                    villagerTabImages[i].sprite = isSelected ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                    villagerTabImages[i].color = isSelected ? new Color(1f, 0.95f, 0.75f) : new Color(0.85f, 0.82f, 0.78f);
                }
                if (villagerTabTexts[i] != null)
                {
                    villagerTabTexts[i].color = isSelected ? new Color(0.42f, 0.15f, 0.05f) : new Color(0.35f, 0.28f, 0.22f);
                    villagerTabTexts[i].fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                }
            }

            // 3. Cập nhật thông tin dân làng
            var profile = engine.Village.GetVillager(CurrentVillagerId);
            if (profile == null) return;

            if (villagerAvatarImg != null)
            {
                villagerAvatarImg.sprite = UISpriteLoader.GetVillagerAvatar(CurrentVillagerId);
            }
            if (villagerNameText != null) villagerNameText.text = profile.Name;
            if (villagerTitleText != null) villagerTitleText.text = profile.Title;
            if (villagerLocationText != null) villagerLocationText.text = $"<i>Vị trí: {profile.LocationDescription}</i>";
            if (atmosphereQuoteText != null) atmosphereQuoteText.text = $"\"{profile.AtmosphereQuote}\"";

            bool isNight = engine.Clock.GetTimeOfDay() == TimeOfDay.Toi;
            bool isOpen = isNight ? profile.IsOpenNight : profile.IsOpenDay;
            if (statusBadgeText != null)
            {
                statusBadgeText.text = isOpen
                    ? "<color=#2e7d32>● Đang mở cửa tiếp khách</color>"
                    : "<color=#c62828>● Đã đóng cửa then cài</color>";
            }

            // 4. Tái tạo danh sách hàng hóa
            if (listingsContainer != null)
            {
                foreach (Transform child in listingsContainer)
                {
                    Destroy(child.gameObject);
                }

                if (profile.Listings == null || profile.Listings.Count == 0)
                {
                    var emptyObj = new GameObject("EmptyMsg");
                    emptyObj.transform.SetParent(listingsContainer, false);
                    var t = emptyObj.AddComponent<TextMeshProUGUI>();
                    t.text = "<i>(Dân làng này hiện chưa có món nào sẵn sàng để giao thương)</i>";
                    t.fontSize = 15f;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.45f, 0.38f, 0.32f);
                }
                else
                {
                    foreach (var listing in profile.Listings)
                    {
                        CreateListingCard(listing, isOpen);
                    }
                }
            }

            if (feedbackText != null) feedbackText.text = "";
        }

        private void CreateListingCard(NightMarketListing listing, bool isOpen)
        {
            var cardObj = new GameObject($"Listing_{listing.SlotId}");
            cardObj.transform.SetParent(listingsContainer, false);
            var cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(850f, 88f);

            var img = cardObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;
            img.color = listing.IsSoldOut ? new Color(0.85f, 0.85f, 0.85f, 0.7f) : Color.white;

            // 1. Icon Box bên trái (58x58)
            var iconBox = new GameObject("Icon_Box");
            iconBox.transform.SetParent(cardObj.transform, false);
            var ibRt = iconBox.AddComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(12f, 0f);
            ibRt.sizeDelta = new Vector2(62f, 62f);

            var ibImg = iconBox.AddComponent<Image>();
            ibImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            ibImg.type = Image.Type.Sliced;
            ibImg.color = new Color(0.92f, 0.90f, 0.85f);

            var iconInner = new GameObject("Icon_Sprite");
            iconInner.transform.SetParent(iconBox.transform, false);
            var iiRt = iconInner.AddComponent<RectTransform>();
            iiRt.anchorMin = Vector2.zero;
            iiRt.anchorMax = Vector2.one;
            iiRt.offsetMin = new Vector2(6f, 6f);
            iiRt.offsetMax = new Vector2(-6f, -6f);
            var iiImg = iconInner.AddComponent<Image>();
            iiImg.sprite = ResolveItemSprite(listing.OfferedItem);
            iiImg.preserveAspect = true;

            // Quantity badge
            if (listing.Quantity > 1)
            {
                var qObj = new GameObject("Quantity_Badge");
                qObj.transform.SetParent(iconBox.transform, false);
                var qRt = qObj.AddComponent<RectTransform>();
                qRt.anchorMin = new Vector2(1f, 0f);
                qRt.anchorMax = new Vector2(1f, 0f);
                qRt.pivot = new Vector2(1f, 0f);
                qRt.anchoredPosition = new Vector2(-2f, 2f);
                qRt.sizeDelta = new Vector2(30f, 18f);
                var qTmp = qObj.AddComponent<TextMeshProUGUI>();
                qTmp.text = $"x{listing.Quantity}";
                qTmp.fontSize = 11.5f;
                qTmp.fontStyle = FontStyles.Bold;
                qTmp.color = Color.white;
                qTmp.alignment = TextAlignmentOptions.BottomRight;
            }

            // 2. Info Column ở giữa
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var iRt = infoObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0f);
            iRt.anchorMax = new Vector2(1f, 1f);
            iRt.offsetMin = new Vector2(86f, 6f);
            iRt.offsetMax = new Vector2(-270f, -6f);

            var iTmp = infoObj.AddComponent<TextMeshProUGUI>();
            string soldOutTag = listing.IsSoldOut ? " <color=#c62828><b>[ĐÃ ĐỔI HẾT HÔM NAY]</b></color>" : "";
            string tierBadge = $"<color=#8d6e63>[Tier {listing.OfferedItem.Tier}]</color>";

            iTmp.text = $"<b>{listing.OfferedItem.Name}</b> (x{listing.Quantity}) {tierBadge}{soldOutTag}\n" +
                        $"<size=11.5><color=#4e342e>{listing.OfferedItem.Description}</color></size>\n" +
                        $"<size=12><color=#1b5e20><b>Yêu cầu:</b> {listing.RequestedItemDescription}</color></size>";
            iTmp.fontSize = 13.5f;
            iTmp.color = new Color(0.22f, 0.15f, 0.08f);

            // 3. Action Buttons bên phải
            if (!listing.IsSoldOut && isOpen)
            {
                // Nút Đổi Barter
                var barterBtnObj = new GameObject("Btn_Barter");
                barterBtnObj.transform.SetParent(cardObj.transform, false);
                var bRt = barterBtnObj.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 0.5f);
                bRt.anchorMax = new Vector2(1f, 0.5f);
                bRt.pivot = new Vector2(1f, 0.5f);
                bRt.anchoredPosition = new Vector2(-12f, 0f);
                bRt.sizeDelta = new Vector2(115f, 40f);

                var bImg = barterBtnObj.AddComponent<Image>();
                bImg.sprite = UISpriteLoader.GetFrameActionButton();
                bImg.type = Image.Type.Sliced;
                bImg.color = new Color(0.85f, 0.52f, 0.22f);

                var bBtn = barterBtnObj.AddComponent<Button>();
                bBtn.onClick.AddListener(() => OpenBarterOfferingPanel(listing));

                var bLbl = new GameObject("Text");
                bLbl.transform.SetParent(barterBtnObj.transform, false);
                var blRt = bLbl.AddComponent<RectTransform>();
                blRt.anchorMin = Vector2.zero;
                blRt.anchorMax = Vector2.one;
                var blTmp = bLbl.AddComponent<TextMeshProUGUI>();
                blTmp.text = "<b>TRAO ĐỔI</b>";
                blTmp.fontSize = 12.5f;
                blTmp.color = Color.white;
                blTmp.alignment = TextAlignmentOptions.Center;

                // Nút Mua Bằng Vàng (nếu hỗ trợ)
                if (listing.AcceptsGold)
                {
                    var goldBtnObj = new GameObject("Btn_Gold");
                    goldBtnObj.transform.SetParent(cardObj.transform, false);
                    var gRt = goldBtnObj.AddComponent<RectTransform>();
                    gRt.anchorMin = new Vector2(1f, 0.5f);
                    gRt.anchorMax = new Vector2(1f, 0.5f);
                    gRt.pivot = new Vector2(1f, 0.5f);
                    gRt.anchoredPosition = new Vector2(-135f, 0f);
                    gRt.sizeDelta = new Vector2(125f, 40f);

                    var gImg = goldBtnObj.AddComponent<Image>();
                    gImg.sprite = UISpriteLoader.GetFrameActionButton();
                    gImg.type = Image.Type.Sliced;
                    gImg.color = new Color(0.96f, 0.78f, 0.20f);

                    var gBtn = goldBtnObj.AddComponent<Button>();
                    gBtn.onClick.AddListener(() => BuyWithGold(listing));

                    var gLbl = new GameObject("Text");
                    gLbl.transform.SetParent(goldBtnObj.transform, false);
                    var glRt = gLbl.AddComponent<RectTransform>();
                    glRt.anchorMin = Vector2.zero;
                    glRt.anchorMax = Vector2.one;
                    var glTmp = gLbl.AddComponent<TextMeshProUGUI>();
                    glTmp.text = $"<b>{listing.GoldPrice}G VÀNG</b>";
                    glTmp.fontSize = 12f;
                    glTmp.color = new Color(0.35f, 0.18f, 0.05f);
                    glTmp.alignment = TextAlignmentOptions.Center;
                }
                else
                {
                    // Huy hiệu Thuần Barter
                    var tagObj = new GameObject("Tag_PureBarter");
                    tagObj.transform.SetParent(cardObj.transform, false);
                    var tgRt = tagObj.AddComponent<RectTransform>();
                    tgRt.anchorMin = new Vector2(1f, 0.5f);
                    tgRt.anchorMax = new Vector2(1f, 0.5f);
                    tgRt.pivot = new Vector2(1f, 0.5f);
                    tgRt.anchoredPosition = new Vector2(-135f, 0f);
                    tgRt.sizeDelta = new Vector2(110f, 30f);

                    var tgTmp = tagObj.AddComponent<TextMeshProUGUI>();
                    tgTmp.text = "<i>[Thuần Barter]</i>";
                    tgTmp.fontSize = 12f;
                    tgTmp.color = new Color(0.55f, 0.35f, 0.25f);
                    tgTmp.alignment = TextAlignmentOptions.Center;
                }
            }
            else if (!isOpen)
            {
                var closedObj = new GameObject("Tag_Closed");
                closedObj.transform.SetParent(cardObj.transform, false);
                var clRt = closedObj.AddComponent<RectTransform>();
                clRt.anchorMin = new Vector2(1f, 0.5f);
                clRt.anchorMax = new Vector2(1f, 0.5f);
                clRt.pivot = new Vector2(1f, 0.5f);
                clRt.anchoredPosition = new Vector2(-15f, 0f);
                clRt.sizeDelta = new Vector2(130f, 32f);
                var clTmp = closedObj.AddComponent<TextMeshProUGUI>();
                clTmp.text = "<color=#c62828><i>(Tiệm đã nghỉ)</i></color>";
                clTmp.fontSize = 13f;
                clTmp.alignment = TextAlignmentOptions.Right;
            }
        }

        private Sprite ResolveItemSprite(GameItem item)
        {
            if (item == null) return UISpriteLoader.GetIconWoodPlank();
            if (item.Id.Contains("Chao") || item.Id.Contains("Tra")) return UISpriteLoader.GetToolIcon(StardewToolType.CamHat);
            if (item.Id.Contains("Go") || item.Id.Contains("Plank")) return UISpriteLoader.GetIconWoodPlank();
            if (item.Id.Contains("Coc") || item.Id.Contains("Rao")) return UISpriteLoader.GetToolIcon(StardewToolType.HangRao);
            if (item.Id.Contains("Thuoc") || item.Id.Contains("KhangDich") || item.Id.Contains("AnThan")) return UISpriteLoader.GetToolIcon(StardewToolType.KhuTrung);
            if (item.Id.Contains("Dao")) return UISpriteLoader.GetToolIcon(StardewToolType.DaoGo);
            if (item.Id.Contains("Thep") || item.Id.Contains("Alloy")) return UISpriteLoader.GetLootSprite("item_alloy_scrap");
            if (item.Id.Contains("PhaLe") || item.Id.Contains("Plasma")) return UISpriteLoader.GetLootSprite("item_plasma_core");
            if (item.Id.Contains("Bua") || item.Id.Contains("Phu")) return UISpriteLoader.GetToolIcon(StardewToolType.BachVan);
            return UISpriteLoader.GetLootSprite(item.Id) ?? UISpriteLoader.GetIconWoodPlank();
        }

        private void BuyWithGold(NightMarketListing listing)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var res = engine.TradeWithVillager(CurrentVillagerId, listing.SlotId, null, null, payWithGold: true);
            if (feedbackText != null)
            {
                feedbackText.text = res.success
                    ? $"<color=#2e7d32>✔ {res.message}</color>"
                    : $"<color=#c62828>✖ {res.message}</color>";
            }

            if (res.success)
            {
                // Thêm vật phẩm vào túi đồ của người chơi
                BackpackPopup.Instance?.AddGameItem(listing.OfferedItem, listing.Quantity);
                Refresh();
            }
        }

        // =========================================================================
        // KHU VỰC ĐẶT HÀNG TRAO ĐỔI (BARTER OFFERING DESK)
        // =========================================================================

        private void OpenBarterOfferingPanel(NightMarketListing listing)
        {
            selectedListing = listing;
            selectedItemQuantities.Clear();
            selectedPigIdToOffer = null;
            barterSubTab = 0; // Default to Items tab

            if (barterOfferingPanel != null)
            {
                barterOfferingPanel.SetActive(true);
            }

            if (barterTargetItemText != null)
            {
                barterTargetItemText.text = $"Đang muốn đổi: <b>{listing.OfferedItem.Name}</b> (x{listing.Quantity})";
            }

            if (barterRequirementText != null)
            {
                string catNote = listing.RequestedCategory == "Any" ? "Bất kỳ danh mục nào" : $"Danh mục: [{listing.RequestedCategory}]";
                barterRequirementText.text = $"Yêu cầu tối thiểu: <b>{listing.MinimumReferenceValue}G GTTC</b> ({catNote})\n" +
                                             $"<color=#c62828><i>* Tục lệ Bờ Rìa: Thừa GTTC không thối lại. Không nhận heo dị biến (trừ Bí Nhân).</i></color>";
            }

            RefreshBarterSubTabsUI();
            PopulateBarterOfferingContent();
            RefreshBarterOfferingTally();
        }

        private void RefreshBarterSubTabsUI()
        {
            if (tabItemsImg != null)
            {
                tabItemsImg.sprite = barterSubTab == 0 ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                tabItemsImg.color = barterSubTab == 0 ? new Color(1f, 0.95f, 0.75f) : new Color(0.85f, 0.82f, 0.78f);
            }
            if (tabItemsText != null)
            {
                tabItemsText.fontStyle = barterSubTab == 0 ? FontStyles.Bold : FontStyles.Normal;
                tabItemsText.color = barterSubTab == 0 ? new Color(0.42f, 0.15f, 0.05f) : new Color(0.35f, 0.28f, 0.22f);
            }

            if (tabPigsImg != null)
            {
                tabPigsImg.sprite = barterSubTab == 1 ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                tabPigsImg.color = barterSubTab == 1 ? new Color(1f, 0.95f, 0.75f) : new Color(0.85f, 0.82f, 0.78f);
            }
            if (tabPigsText != null)
            {
                tabPigsText.fontStyle = barterSubTab == 1 ? FontStyles.Bold : FontStyles.Normal;
                tabPigsText.color = barterSubTab == 1 ? new Color(0.42f, 0.15f, 0.05f) : new Color(0.35f, 0.28f, 0.22f);
            }
        }

        private void SwitchBarterSubTab(int newTab)
        {
            barterSubTab = newTab;
            RefreshBarterSubTabsUI();
            PopulateBarterOfferingContent();
        }

        private void PopulateBarterOfferingContent()
        {
            if (barterOfferingContent == null || selectedListing == null) return;

            foreach (Transform child in barterOfferingContent)
            {
                Destroy(child.gameObject);
            }

            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            if (barterSubTab == 0)
            {
                // --- TAB 0: VẬT PHẨM BALO & KHO ---
                var availableItems = GetAvailableTradeItems();
                if (availableItems.Count == 0)
                {
                    var emptyMsg = new GameObject("EmptyItems");
                    emptyMsg.transform.SetParent(barterOfferingContent, false);
                    var t = emptyMsg.AddComponent<TextMeshProUGUI>();
                    t.text = "<i>(Túi đồ của bạn hiện không có vật phẩm nào để đem ra trao đổi)</i>";
                    t.fontSize = 14f;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.45f, 0.35f, 0.25f);
                    return;
                }

                foreach (var it in availableItems)
                {
                    CreateOfferingItemRow(it);
                }
            }
            else
            {
                // --- TAB 1: ĐÀN HEO SỐNG TẠI TRẠI ---
                var livingPigs = engine.Pigs.FindAll(p => p.IsAlive);
                if (livingPigs.Count == 0)
                {
                    var emptyMsg = new GameObject("EmptyPigs");
                    emptyMsg.transform.SetParent(barterOfferingContent, false);
                    var t = emptyMsg.AddComponent<TextMeshProUGUI>();
                    t.text = "<i>(Trang trại của bạn hiện chưa có heo sống nào trong chuồng)</i>";
                    t.fontSize = 14f;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.45f, 0.35f, 0.25f);
                    return;
                }

                foreach (var pig in livingPigs)
                {
                    CreateOfferingPigRow(pig);
                }
            }
        }

        private List<GameItem> GetAvailableTradeItems()
        {
            var list = new List<GameItem>();

            // 1. Lấy từ túi đồ của người chơi
            var bp = BackpackPopup.Instance;
            if (bp != null)
            {
                var bpItems = bp.GetInventoryItems();
                if (bpItems != null)
                {
                    foreach (var inv in bpItems)
                    {
                        if (inv == null || inv.Quantity <= 0) continue;
                        if (inv.Id == "balo" || inv.Id == "tien_vang") continue; // Không đổi balo hoặc tiền mặt tại bàn barter

                        // Chuyển đổi sang GameItem với GTTC ước lượng chuẩn
                        int gttc = EstimateItemReferenceValue(inv.Id);
                        string cat = EstimateItemCategory(inv.Id, inv.Category);

                        list.Add(new GameItem
                        {
                            Id = inv.Id,
                            Name = inv.Name,
                            Quantity = inv.Quantity,
                            ReferenceValueGold = gttc,
                            Category = cat,
                            Description = inv.Description
                        });
                    }
                }
            }

            // 2. Nếu túi đồ trống, bổ sung kho dự phòng tiêu chuẩn của người nuôi heo
            if (list.Count == 0)
            {
                list.Add(new GameItem { Id = "cam_heo", Name = "Bao Cám Heo", Quantity = 5, ReferenceValueGold = 60, Category = "Consumable", Description = "Cám dinh dưỡng cao" });
                list.Add(new GameItem { Id = "xo_nuoc", Name = "Xô Nước Ngọt", Quantity = 5, ReferenceValueGold = 40, Category = "Consumable", Description = "Nước ngọt thảo nguyên" });
                list.Add(new GameItem { Id = "coc_go", Name = "Cọc Rào Chuồng", Quantity = 10, ReferenceValueGold = 50, Category = "Material", Description = "Cọc gỗ rào trại" });
                list.Add(new GameItem { Id = "thao_duoc", Name = "Thảo Dược Bổ Lực", Quantity = 3, ReferenceValueGold = 120, Category = "Consumable", Description = "Thảo mộc hái ven suối" });
                list.Add(new GameItem { Id = "khu_trung", Name = "Bình Khử Trùng", Quantity = 2, ReferenceValueGold = 90, Category = "Consumable", Description = "Dược phẩm trang trại" });
            }

            return list;
        }

        private int EstimateItemReferenceValue(string id)
        {
            return id switch
            {
                "cam_heo" => 60,
                "xo_nuoc" => 40,
                "coc_go" => 50,
                "thao_duoc" => 120,
                "khu_trung" => 90,
                "bua_go" => 150,
                "dao_gam" => 200,
                "bach_van" => 1000,
                "GoTinhChe" => 30,
                "ThepCuongLuc" => 120,
                "PhaLeUFO" => 950,
                "DaHeoThietBi" => 450,
                "NanhXichMao" => 520,
                _ => 50
            };
        }

        private string EstimateItemCategory(string id, string defaultCat)
        {
            if (id.Contains("cam") || id.Contains("nuoc") || id.Contains("thao_duoc") || id.Contains("khu_trung") || id.Contains("Chao") || id.Contains("Tra"))
                return "Consumable";
            if (id.Contains("coc") || id.Contains("go") || id.Contains("thep") || id.Contains("da") || id.Contains("nanh") || id.Contains("phale"))
                return "Material";
            return "Material";
        }

        private void CreateOfferingItemRow(GameItem item)
        {
            var rowObj = new GameObject($"OfferItem_{item.Id}");
            rowObj.transform.SetParent(barterOfferingContent, false);
            var rRt = rowObj.AddComponent<RectTransform>();
            rRt.sizeDelta = new Vector2(800f, 62f);

            var img = rowObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;

            bool isMatchCategory = selectedListing.RequestedCategory == "Any" ||
                                   string.Equals(item.Category, selectedListing.RequestedCategory, StringComparison.OrdinalIgnoreCase);

            img.color = isMatchCategory ? Color.white : new Color(0.92f, 0.88f, 0.88f);

            // Icon
            var iconBox = new GameObject("Icon");
            iconBox.transform.SetParent(rowObj.transform, false);
            var ibRt = iconBox.AddComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(8f, 0f);
            ibRt.sizeDelta = new Vector2(46f, 46f);
            var iImg = iconBox.AddComponent<Image>();
            iImg.sprite = ResolveItemSprite(item);
            iImg.preserveAspect = true;

            // Info text
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(rowObj.transform, false);
            var iRt = infoObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0f);
            iRt.anchorMax = new Vector2(1f, 1f);
            iRt.offsetMin = new Vector2(64f, 4f);
            iRt.offsetMax = new Vector2(-220f, -4f);

            var iTmp = infoObj.AddComponent<TextMeshProUGUI>();
            string catTag = isMatchCategory
                ? $"<color=#1b5e20>[{item.Category}]</color>"
                : $"<color=#c62828>[Sai danh mục: cần {selectedListing.RequestedCategory}]</color>";

            iTmp.text = $"<b>{item.Name}</b> (Có: {item.Quantity}) {catTag}\n" +
                        $"<size=12><color=#b8860b><b>+{item.ReferenceValueGold}G GTTC / món</b></color></size>";
            iTmp.fontSize = 13f;
            iTmp.color = new Color(0.25f, 0.18f, 0.12f);

            // Counter Selector: [-] [Count] [+]
            selectedItemQuantities.TryGetValue(item.Id, out int currentSelected);

            var counterBar = new GameObject("CounterBar");
            counterBar.transform.SetParent(rowObj.transform, false);
            var cbRt = counterBar.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 0.5f);
            cbRt.anchorMax = new Vector2(1f, 0.5f);
            cbRt.pivot = new Vector2(1f, 0.5f);
            cbRt.anchoredPosition = new Vector2(-12f, 0f);
            cbRt.sizeDelta = new Vector2(180f, 38f);

            // Button Minus [-]
            var btnMinus = new GameObject("Btn_Minus");
            btnMinus.transform.SetParent(counterBar.transform, false);
            var bmRt = btnMinus.AddComponent<RectTransform>();
            bmRt.anchorMin = new Vector2(0f, 0.5f);
            bmRt.anchorMax = new Vector2(0f, 0.5f);
            bmRt.pivot = new Vector2(0f, 0.5f);
            bmRt.anchoredPosition = new Vector2(0f, 0f);
            bmRt.sizeDelta = new Vector2(38f, 36f);
            var bmImg = btnMinus.AddComponent<Image>();
            bmImg.sprite = UISpriteLoader.GetFrameActionButton();
            bmImg.type = Image.Type.Sliced;
            bmImg.color = currentSelected > 0 ? new Color(0.85f, 0.4f, 0.35f) : new Color(0.7f, 0.7f, 0.7f);
            var bmBtn = btnMinus.AddComponent<Button>();
            bmBtn.interactable = currentSelected > 0;
            var bmTextObj = new GameObject("Text");
            bmTextObj.transform.SetParent(btnMinus.transform, false);
            var bmtRt = bmTextObj.AddComponent<RectTransform>();
            bmtRt.anchorMin = Vector2.zero;
            bmtRt.anchorMax = Vector2.one;
            var bmtTmp = bmTextObj.AddComponent<TextMeshProUGUI>();
            bmtTmp.text = "<b>-</b>";
            bmtTmp.fontSize = 18f;
            bmtTmp.color = Color.white;
            bmtTmp.alignment = TextAlignmentOptions.Center;

            // Count Text
            var countObj = new GameObject("CountText");
            countObj.transform.SetParent(counterBar.transform, false);
            var cRt = countObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, 0f);
            cRt.sizeDelta = new Vector2(90f, 36f);
            var cTmp = countObj.AddComponent<TextMeshProUGUI>();
            cTmp.text = currentSelected > 0 ? $"<color=#2e7d32><b>Đã chọn: {currentSelected}</b></color>" : "Chưa chọn";
            cTmp.fontSize = 12f;
            cTmp.alignment = TextAlignmentOptions.Center;

            // Button Plus [+]
            var btnPlus = new GameObject("Btn_Plus");
            btnPlus.transform.SetParent(counterBar.transform, false);
            var bpRt = btnPlus.AddComponent<RectTransform>();
            bpRt.anchorMin = new Vector2(1f, 0.5f);
            bpRt.anchorMax = new Vector2(1f, 0.5f);
            bpRt.pivot = new Vector2(1f, 0.5f);
            bpRt.anchoredPosition = new Vector2(0f, 0f);
            bpRt.sizeDelta = new Vector2(38f, 36f);
            var bpImg = btnPlus.AddComponent<Image>();
            bpImg.sprite = UISpriteLoader.GetFrameActionButton();
            bpImg.type = Image.Type.Sliced;
            bpImg.color = isMatchCategory && currentSelected < item.Quantity ? new Color(0.35f, 0.75f, 0.35f) : new Color(0.7f, 0.7f, 0.7f);
            var bpBtn = btnPlus.AddComponent<Button>();
            bpBtn.interactable = isMatchCategory && currentSelected < item.Quantity;
            var bpTextObj = new GameObject("Text");
            bpTextObj.transform.SetParent(btnPlus.transform, false);
            var bptRt = bpTextObj.AddComponent<RectTransform>();
            bptRt.anchorMin = Vector2.zero;
            bptRt.anchorMax = Vector2.one;
            var bptTmp = bpTextObj.AddComponent<TextMeshProUGUI>();
            bptTmp.text = "<b>+</b>";
            bptTmp.fontSize = 18f;
            bptTmp.color = Color.white;
            bptTmp.alignment = TextAlignmentOptions.Center;

            // Action Listeners
            bmBtn.onClick.AddListener(() =>
            {
                if (selectedItemQuantities.TryGetValue(item.Id, out int val) && val > 0)
                {
                    val--;
                    if (val == 0) selectedItemQuantities.Remove(item.Id);
                    else selectedItemQuantities[item.Id] = val;
                    PopulateBarterOfferingContent();
                    RefreshBarterOfferingTally();
                }
            });

            bpBtn.onClick.AddListener(() =>
            {
                selectedItemQuantities.TryGetValue(item.Id, out int val);
                if (val < item.Quantity)
                {
                    selectedItemQuantities[item.Id] = val + 1;
                    PopulateBarterOfferingContent();
                    RefreshBarterOfferingTally();
                }
            });
        }

        private void CreateOfferingPigRow(Pig pig)
        {
            var rowObj = new GameObject($"OfferPig_{pig.Id}");
            rowObj.transform.SetParent(barterOfferingContent, false);
            var rRt = rowObj.AddComponent<RectTransform>();
            rRt.sizeDelta = new Vector2(800f, 62f);

            var img = rowObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;

            bool isSelected = selectedPigIdToOffer == pig.Id;
            bool isMutant = pig.Rarity == GeneRarity.DiBien;
            bool isEligiblePig = !isMutant || CurrentVillagerId == VillagerId.BiNhan;

            img.color = isSelected ? new Color(1f, 0.95f, 0.75f) : (isEligiblePig ? Color.white : new Color(0.92f, 0.85f, 0.85f));

            // Pig Avatar
            var iconBox = new GameObject("Icon");
            iconBox.transform.SetParent(rowObj.transform, false);
            var ibRt = iconBox.AddComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(8f, 0f);
            ibRt.sizeDelta = new Vector2(46f, 46f);
            var iImg = iconBox.AddComponent<Image>();
            iImg.sprite = UISpriteLoader.GetPigAvatar(pig.GeneLine);
            iImg.preserveAspect = true;

            // Info
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(rowObj.transform, false);
            var iRt = infoObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0f);
            iRt.anchorMax = new Vector2(1f, 1f);
            iRt.offsetMin = new Vector2(64f, 4f);
            iRt.offsetMax = new Vector2(-180f, -4f);

            var engine = MobileGameController.Instance?.Engine;
            int pigGttc = 0;
            if (engine != null)
            {
                var (pPrice, _) = engine.Economy.CalculatePigSalePrice(pig, engine.Farm.FarmLevel);
                pigGttc = (int)Math.Round(pPrice * 0.90f);
            }

            string mutantTag = isMutant
                ? (CurrentVillagerId == VillagerId.BiNhan ? " <color=#7b1fa2>[Dị Biến - Bí Nhân Chấp Nhận]</color>" : " <color=#c62828>[Dị Biến - Bị Từ Chối]</color>")
                : "";

            var iTmp = infoObj.AddComponent<TextMeshProUGUI>();
            iTmp.text = $"<b>{pig.Name}</b> (Giống: {pig.GeneLine}, {pig.WeightKg:F1}kg){mutantTag}\n" +
                        $"<size=12><color=#b8860b><b>Giá trị quy đổi Barter: +{pigGttc:N0}G GTTC</b> (90% giá bán)</color></size>";
            iTmp.fontSize = 13f;
            iTmp.color = new Color(0.25f, 0.18f, 0.12f);

            // Select Button
            var selectBtnObj = new GameObject("Btn_Select");
            selectBtnObj.transform.SetParent(rowObj.transform, false);
            var sRt = selectBtnObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 0.5f);
            sRt.anchorMax = new Vector2(1f, 0.5f);
            sRt.pivot = new Vector2(1f, 0.5f);
            sRt.anchoredPosition = new Vector2(-12f, 0f);
            sRt.sizeDelta = new Vector2(140f, 38f);

            var sImg = selectBtnObj.AddComponent<Image>();
            sImg.sprite = UISpriteLoader.GetFrameActionButton();
            sImg.type = Image.Type.Sliced;
            sImg.color = isSelected ? new Color(0.85f, 0.4f, 0.35f) : (isEligiblePig ? new Color(0.35f, 0.75f, 0.35f) : new Color(0.7f, 0.7f, 0.7f));

            var sBtn = selectBtnObj.AddComponent<Button>();
            sBtn.interactable = isEligiblePig;

            var sTextObj = new GameObject("Text");
            sTextObj.transform.SetParent(selectBtnObj.transform, false);
            var stRt = sTextObj.AddComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            var stTmp = sTextObj.AddComponent<TextMeshProUGUI>();
            stTmp.text = isSelected ? "<b>BỎ CHỌN HEO</b>" : "<b>CHỌN ĐEM CÂN</b>";
            stTmp.fontSize = 12f;
            stTmp.color = Color.white;
            stTmp.alignment = TextAlignmentOptions.Center;

            sBtn.onClick.AddListener(() =>
            {
                if (isSelected)
                {
                    selectedPigIdToOffer = null;
                }
                else
                {
                    selectedPigIdToOffer = pig.Id;
                }
                PopulateBarterOfferingContent();
                RefreshBarterOfferingTally();
            });
        }

        private void RefreshBarterOfferingTally()
        {
            if (selectedListing == null || barterOfferingTallyText == null) return;

            int totalOffered = 0;
            var availableItems = GetAvailableTradeItems();

            foreach (var kvp in selectedItemQuantities)
            {
                var match = availableItems.Find(it => it.Id == kvp.Key);
                if (match != null)
                {
                    totalOffered += match.ReferenceValueGold * kvp.Value;
                }
            }

            var engine = MobileGameController.Instance?.Engine;
            if (!string.IsNullOrEmpty(selectedPigIdToOffer) && engine != null)
            {
                var pig = engine.Pigs.Find(p => p.Id == selectedPigIdToOffer);
                if (pig != null)
                {
                    var (pPrice, _) = engine.Economy.CalculatePigSalePrice(pig, engine.Farm.FarmLevel);
                    totalOffered += (int)Math.Round(pPrice * 0.90f);
                }
            }

            int needed = selectedListing.MinimumReferenceValue;
            int excess = totalOffered - needed;
            bool isEnough = totalOffered >= needed;

            string status = isEnough
                ? $"<color=#2e7d32><b>✔ ĐỦ ĐIỀU KIỆN TRAO ĐỔI!</b> (Đã chào: {totalOffered:N0}G / Cần: {needed:N0}G GTTC)</color>" +
                  (excess > 0 ? $"\n<size=12><color=#e65100>Dư {excess:N0}G GTTC: Theo tục lệ Bờ Rìa sẽ không hoàn lại!</color></size>" : "")
                : $"<color=#c62828><b>CHƯA ĐỦ GTTC!</b> (Đã chào: {totalOffered:N0}G / Cần: {needed:N0}G GTTC, còn thiếu {needed - totalOffered:N0}G)</color>";

            barterOfferingTallyText.text = status;

            if (confirmBarterBtn != null)
            {
                confirmBarterBtn.interactable = isEnough;
            }
        }

        public void ConfirmBarter()
        {
            if (selectedListing == null) return;

            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            // Xây dựng danh sách GameItem chào đổi
            var offeredList = new List<GameItem>();
            var availableItems = GetAvailableTradeItems();
            foreach (var kvp in selectedItemQuantities)
            {
                if (kvp.Value <= 0) continue;
                var match = availableItems.Find(it => it.Id == kvp.Key);
                if (match != null)
                {
                    offeredList.Add(new GameItem
                    {
                        Id = match.Id,
                        Name = match.Name,
                        Category = match.Category,
                        ReferenceValueGold = match.ReferenceValueGold,
                        Quantity = kvp.Value
                    });
                }
            }

            var res = engine.TradeWithVillager(
                CurrentVillagerId,
                selectedListing.SlotId,
                offeredList.Count > 0 ? offeredList : null,
                selectedPigIdToOffer,
                payWithGold: false);

            if (feedbackText != null)
            {
                feedbackText.text = res.success
                    ? $"<color=#2e7d32>✔ {res.message}</color>"
                    : $"<color=#c62828>✖ {res.message}</color>";
            }

            if (res.success)
            {
                // Trừ vật phẩm trong túi đồ người chơi
                var bp = BackpackPopup.Instance;
                if (bp != null)
                {
                    foreach (var kvp in selectedItemQuantities)
                    {
                        bp.DeductItemQuantity(kvp.Key, kvp.Value);
                    }
                    bp.AddGameItem(selectedListing.OfferedItem, selectedListing.Quantity);
                }

                if (barterOfferingPanel != null) barterOfferingPanel.SetActive(false);
                selectedListing = null;
                selectedItemQuantities.Clear();
                selectedPigIdToOffer = null;

                Refresh();
            }
        }

        public void CloseBarterOfferingPanel()
        {
            if (barterOfferingPanel != null) barterOfferingPanel.SetActive(false);
            selectedListing = null;
            selectedItemQuantities.Clear();
            selectedPigIdToOffer = null;
        }

        // =========================================================================
        // KHỞI TẠO HỆ THỐNG GIAO DIỆN (UI INITIALIZATION)
        // =========================================================================

        public void EnsureUIExists()
        {
            if (PopupPanel != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("Village_Barter_Popup");
            if (existing != null)
            {
                PopupPanel = existing.gameObject;
                return;
            }

            // 1. Modal Backdrop làm tối màn hình & chặn click xuyên thấu
            EnsureModalBackdrop(canvas);

            // 2. Outer Popup Panel (Khung gỗ lớn 920x660)
            var popupObj = new GameObject("Village_Barter_Popup");
            popupObj.transform.SetParent(canvas.transform, false);
            var pRt = popupObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(920f, 660f);
            PopupPanel = popupObj;

            var bgImg = popupObj.AddComponent<Image>();
            bgImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // 3. Inner Parchment (Giấy da ấm)
            var parchObj = new GameObject("Parchment_Inner");
            parchObj.transform.SetParent(popupObj.transform, false);
            var parchRt = parchObj.AddComponent<RectTransform>();
            parchRt.anchorMin = Vector2.zero;
            parchRt.anchorMax = Vector2.one;
            parchRt.offsetMin = new Vector2(16f, 16f);
            parchRt.offsetMax = new Vector2(-16f, -16f);
            var pImg = parchObj.AddComponent<Image>();
            pImg.sprite = UISpriteLoader.GetFrameParchment();
            pImg.type = Image.Type.Sliced;
            pImg.color = new Color(0.97f, 0.94f, 0.89f);

            // 4. Header Bar: Tiêu đề + Ví Vàng + Đồng Hồ + Nút Đóng [X]
            BuildHeaderBar(parchObj.transform);

            // 5. Villager Tabs Bar (6 Dân Làng Bờ Rìa)
            BuildVillagerTabsBar(parchObj.transform);

            // 6. Villager Profile Box (Chân dung, Tên, Quote, Trạng thái)
            BuildVillagerProfileBox(parchObj.transform);

            // 7. Listings Area với ScrollView chuyên dụng
            BuildListingsScrollView(parchObj.transform);

            // 8. Feedback Banner ở đáy
            var feedObj = new GameObject("Feedback_Text");
            feedObj.transform.SetParent(parchObj.transform, false);
            var fRt = feedObj.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0f, 0f);
            fRt.anchorMax = new Vector2(1f, 0f);
            fRt.pivot = new Vector2(0.5f, 0f);
            fRt.anchoredPosition = new Vector2(0f, 8f);
            fRt.sizeDelta = new Vector2(-40f, 26f);
            feedbackText = feedObj.AddComponent<TextMeshProUGUI>();
            feedbackText.fontSize = 13.5f;
            feedbackText.alignment = TextAlignmentOptions.Center;

            // 9. Sub-Panel: Khu vực đặt hàng trao đổi (Barter Offering Desk)
            BuildBarterOfferingSubPanel(parchObj.transform);
        }

        private void EnsureModalBackdrop(Canvas canvas)
        {
            if (modalBackdropObj == null)
            {
                var existing = canvas.transform.Find("Village_Barter_Backdrop");
                if (existing != null)
                {
                    modalBackdropObj = existing.gameObject;
                }
                else
                {
                    modalBackdropObj = new GameObject("Village_Barter_Backdrop");
                    modalBackdropObj.transform.SetParent(canvas.transform, false);
                    var mbRt = modalBackdropObj.AddComponent<RectTransform>();
                    mbRt.anchorMin = Vector2.zero;
                    mbRt.anchorMax = Vector2.one;
                    mbRt.offsetMin = Vector2.zero;
                    mbRt.offsetMax = Vector2.zero;
                    var mbImg = modalBackdropObj.AddComponent<Image>();
                    mbImg.color = new Color(0f, 0f, 0f, 0.55f);
                    mbImg.raycastTarget = true;
                    var mbBtn = modalBackdropObj.AddComponent<Button>();
                    mbBtn.onClick.AddListener(Hide);
                    modalBackdropObj.SetActive(false);
                }
            }
        }

        private void BuildHeaderBar(Transform parent)
        {
            var header = new GameObject("Header_Bar");
            header.transform.SetParent(parent, false);
            var hRt = header.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -8f);
            hRt.sizeDelta = new Vector2(-20f, 38f);

            // Tiêu đề
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(0f, 0.5f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(12f, 0f);
            tRt.sizeDelta = new Vector2(360f, 32f);
            headerTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitleText.text = "🏛️ <b>GIAO THƯƠNG DÂN LÀNG BỜ RÌA</b>";
            headerTitleText.fontSize = 16f;
            headerTitleText.fontStyle = FontStyles.Bold;
            headerTitleText.color = new Color(0.38f, 0.18f, 0.08f);

            // Thời gian
            var timeObj = new GameObject("TimeBadge");
            timeObj.transform.SetParent(header.transform, false);
            var tmRt = timeObj.AddComponent<RectTransform>();
            tmRt.anchorMin = new Vector2(0.5f, 0.5f);
            tmRt.anchorMax = new Vector2(0.5f, 0.5f);
            tmRt.pivot = new Vector2(0.5f, 0.5f);
            tmRt.anchoredPosition = new Vector2(20f, 0f);
            tmRt.sizeDelta = new Vector2(220f, 30f);
            timeInfoText = timeObj.AddComponent<TextMeshProUGUI>();
            timeInfoText.fontSize = 12.5f;
            timeInfoText.color = new Color(0.48f, 0.35f, 0.25f);
            timeInfoText.alignment = TextAlignmentOptions.Center;

            // Ví Vàng
            var goldBox = new GameObject("GoldBox");
            goldBox.transform.SetParent(header.transform, false);
            var gRt = goldBox.AddComponent<RectTransform>();
            gRt.anchorMin = new Vector2(1f, 0.5f);
            gRt.anchorMax = new Vector2(1f, 0.5f);
            gRt.pivot = new Vector2(1f, 0.5f);
            gRt.anchoredPosition = new Vector2(-55f, 0f);
            gRt.sizeDelta = new Vector2(150f, 32f);

            var coinObj = new GameObject("CoinIcon");
            coinObj.transform.SetParent(goldBox.transform, false);
            var cRt = coinObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0.5f);
            cRt.anchorMax = new Vector2(0f, 0.5f);
            cRt.pivot = new Vector2(0f, 0.5f);
            cRt.anchoredPosition = new Vector2(4f, 0f);
            cRt.sizeDelta = new Vector2(22f, 22f);
            var cImg = coinObj.AddComponent<Image>();
            cImg.sprite = UISpriteLoader.GetIconGold();
            cImg.preserveAspect = true;

            var goldTextObj = new GameObject("GoldText");
            goldTextObj.transform.SetParent(goldBox.transform, false);
            var gtRt = goldTextObj.AddComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0f, 0f);
            gtRt.anchorMax = new Vector2(1f, 1f);
            gtRt.offsetMin = new Vector2(30f, 0f);
            gtRt.offsetMax = Vector2.zero;
            playerGoldText = goldTextObj.AddComponent<TextMeshProUGUI>();
            playerGoldText.text = "2,500 G";
            playerGoldText.fontSize = 14.5f;
            playerGoldText.fontStyle = FontStyles.Bold;
            playerGoldText.color = new Color(0.72f, 0.45f, 0.05f);
            playerGoldText.alignment = TextAlignmentOptions.Left;

            // Nút Đóng [X]
            var closeBtnObj = new GameObject("Btn_Close");
            closeBtnObj.transform.SetParent(header.transform, false);
            var clRt = closeBtnObj.AddComponent<RectTransform>();
            clRt.anchorMin = new Vector2(1f, 0.5f);
            clRt.anchorMax = new Vector2(1f, 0.5f);
            clRt.pivot = new Vector2(1f, 0.5f);
            clRt.anchoredPosition = new Vector2(-4f, 0f);
            clRt.sizeDelta = new Vector2(34f, 34f);

            var clImg = closeBtnObj.AddComponent<Image>();
            clImg.sprite = UISpriteLoader.GetFrameActionButton();
            clImg.type = Image.Type.Sliced;
            clImg.color = new Color(0.85f, 0.35f, 0.25f);

            var clBtn = closeBtnObj.AddComponent<Button>();
            clBtn.onClick.AddListener(Hide);

            var clLbl = new GameObject("Label");
            clLbl.transform.SetParent(closeBtnObj.transform, false);
            var cllRt = clLbl.AddComponent<RectTransform>();
            cllRt.anchorMin = Vector2.zero;
            cllRt.anchorMax = Vector2.one;
            var cllTmp = clLbl.AddComponent<TextMeshProUGUI>();
            cllTmp.text = "<b>✕</b>";
            cllTmp.fontSize = 16f;
            cllTmp.color = Color.white;
            cllTmp.alignment = TextAlignmentOptions.Center;
        }

        private void BuildVillagerTabsBar(Transform parent)
        {
            var tabContainer = new GameObject("Villager_Tabs");
            tabContainer.transform.SetParent(parent, false);
            var tRt = tabContainer.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -48f);
            tRt.sizeDelta = new Vector2(-24f, 36f);

            var vList = new (VillagerId id, string name, string role)[]
            {
                (VillagerId.BaSau, "Bà Sáu", "Quán Nước"),
                (VillagerId.TamVa, "Tám Vá", "Xưởng Mộc"),
                (VillagerId.CoDung, "Cô Dung", "Tiệm Thuốc"),
                (VillagerId.AnhCa, "Anh Ca", "Trạm Cân"),
                (VillagerId.OngTon, "Ông Tồn", "Sổ Đen"),
                (VillagerId.BiNhan, "Bí Nhân", "Ranh Giới")
            };

            villagerTabButtons.Clear();
            villagerTabImages.Clear();
            villagerTabTexts.Clear();

            float totalW = 860f;
            float tabW = totalW / vList.Length;
            float startX = -(totalW / 2f) + (tabW / 2f);

            for (int i = 0; i < vList.Length; i++)
            {
                var v = vList[i];
                var tabBtn = new GameObject($"Tab_{v.name}");
                tabBtn.transform.SetParent(tabContainer.transform, false);
                var tbRt = tabBtn.AddComponent<RectTransform>();
                tbRt.anchorMin = new Vector2(0.5f, 0.5f);
                tbRt.anchorMax = new Vector2(0.5f, 0.5f);
                tbRt.anchoredPosition = new Vector2(startX + (i * tabW), 0f);
                tbRt.sizeDelta = new Vector2(tabW - 6f, 34f);

                var img = tabBtn.AddComponent<Image>();
                img.sprite = UISpriteLoader.GetFrameSlotNormal();
                img.type = Image.Type.Sliced;
                villagerTabImages.Add(img);

                var btn = tabBtn.AddComponent<Button>();
                btn.onClick.AddListener(() => OpenForVillager(v.id));
                villagerTabButtons.Add(btn);

                var lbl = new GameObject("Text");
                lbl.transform.SetParent(tabBtn.transform, false);
                var lblRt = lbl.AddComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{v.name}</b>\n<size=9.5>{v.role}</size>";
                tmp.fontSize = 12f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.35f, 0.28f, 0.22f);
                villagerTabTexts.Add(tmp);
            }
        }

        private void BuildVillagerProfileBox(Transform parent)
        {
            var profileBox = new GameObject("Profile_Box");
            profileBox.transform.SetParent(parent, false);
            var prRt = profileBox.AddComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0f, 1f);
            prRt.anchorMax = new Vector2(1f, 1f);
            prRt.pivot = new Vector2(0.5f, 1f);
            prRt.anchoredPosition = new Vector2(0f, -90f);
            prRt.sizeDelta = new Vector2(-24f, 105f);

            var prImg = profileBox.AddComponent<Image>();
            prImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            prImg.type = Image.Type.Sliced;
            prImg.color = new Color(1f, 0.98f, 0.93f);

            // 1. Avatar Portrait Frame bên trái
            var avFrame = new GameObject("Avatar_Frame");
            avFrame.transform.SetParent(profileBox.transform, false);
            var avfRt = avFrame.AddComponent<RectTransform>();
            avfRt.anchorMin = new Vector2(0f, 0.5f);
            avfRt.anchorMax = new Vector2(0f, 0.5f);
            avfRt.pivot = new Vector2(0f, 0.5f);
            avfRt.anchoredPosition = new Vector2(14f, 0f);
            avfRt.sizeDelta = new Vector2(76f, 76f);

            var avfImg = avFrame.AddComponent<Image>();
            avfImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            avfImg.type = Image.Type.Sliced;

            var avObj = new GameObject("Avatar_Sprite");
            avObj.transform.SetParent(avFrame.transform, false);
            var avRt = avObj.AddComponent<RectTransform>();
            avRt.anchorMin = Vector2.zero;
            avRt.anchorMax = Vector2.one;
            avRt.offsetMin = new Vector2(6f, 6f);
            avRt.offsetMax = new Vector2(-6f, -6f);
            villagerAvatarImg = avObj.AddComponent<Image>();
            villagerAvatarImg.preserveAspect = true;

            // 2. Tên & Chức Danh
            var nameObj = new GameObject("Name_Title");
            nameObj.transform.SetParent(profileBox.transform, false);
            var nRt = nameObj.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.anchoredPosition = new Vector2(102f, -8f);
            nRt.sizeDelta = new Vector2(-330f, 24f);

            villagerNameText = nameObj.AddComponent<TextMeshProUGUI>();
            villagerNameText.text = "<b>Bà Sáu</b>";
            villagerNameText.fontSize = 18f;
            villagerNameText.fontStyle = FontStyles.Bold;
            villagerNameText.color = new Color(0.45f, 0.15f, 0.08f);

            var titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(profileBox.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0f, 1f);
            tRt.anchoredPosition = new Vector2(102f, -32f);
            tRt.sizeDelta = new Vector2(-330f, 18f);

            villagerTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            villagerTitleText.text = "Chủ Quán Nước & Bếp Ăn Đầu Làng";
            villagerTitleText.fontSize = 12f;
            villagerTitleText.color = new Color(0.55f, 0.35f, 0.22f);

            // 3. Status Badge bên phải
            var statusObj = new GameObject("Status_Badge");
            statusObj.transform.SetParent(profileBox.transform, false);
            var sRt = statusObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 1f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot = new Vector2(1f, 1f);
            sRt.anchoredPosition = new Vector2(-16f, -10f);
            sRt.sizeDelta = new Vector2(210f, 24f);
            statusBadgeText = statusObj.AddComponent<TextMeshProUGUI>();
            statusBadgeText.fontSize = 13f;
            statusBadgeText.alignment = TextAlignmentOptions.Right;

            // 4. Quote Text Box
            var quoteObj = new GameObject("Quote_Text");
            quoteObj.transform.SetParent(profileBox.transform, false);
            var qRt = quoteObj.AddComponent<RectTransform>();
            qRt.anchorMin = Vector2.zero;
            qRt.anchorMax = Vector2.one;
            qRt.offsetMin = new Vector2(102f, 8f);
            qRt.offsetMax = new Vector2(-16f, -52f);
            atmosphereQuoteText = quoteObj.AddComponent<TextMeshProUGUI>();
            atmosphereQuoteText.fontSize = 12.5f;
            atmosphereQuoteText.fontStyle = FontStyles.Italic;
            atmosphereQuoteText.color = new Color(0.25f, 0.20f, 0.15f);
            atmosphereQuoteText.alignment = TextAlignmentOptions.TopLeft;
        }

        private void BuildListingsScrollView(Transform parent)
        {
            var scrollObj = new GameObject("Listings_ScrollView");
            scrollObj.transform.SetParent(parent, false);
            var scRt = scrollObj.AddComponent<RectTransform>();
            scRt.anchorMin = Vector2.zero;
            scRt.anchorMax = Vector2.one;
            scRt.offsetMin = new Vector2(16f, 38f);
            scRt.offsetMax = new Vector2(-16f, -202f);

            listingsScrollRect = scrollObj.AddComponent<ScrollRect>();
            listingsScrollRect.horizontal = false;
            listingsScrollRect.vertical = true;
            listingsScrollRect.movementType = ScrollRect.MovementType.Clamped;
            listingsScrollRect.scrollSensitivity = 25f;

            // Viewport
            var viewObj = new GameObject("Viewport");
            viewObj.transform.SetParent(scrollObj.transform, false);
            var vRt = viewObj.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.offsetMin = Vector2.zero;
            vRt.offsetMax = Vector2.zero;

            var vImg = viewObj.AddComponent<Image>();
            vImg.color = Color.white;
            var mask = viewObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewObj.transform, false);
            var cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0f, 300f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            listingsScrollRect.viewport = vRt;
            listingsScrollRect.content = cRt;
            listingsContainer = contentObj.transform;
        }

        private void BuildBarterOfferingSubPanel(Transform parent)
        {
            var pnl = new GameObject("Barter_Offering_SubPanel");
            pnl.transform.SetParent(parent, false);
            var pRt = pnl.AddComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero;
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(8f, 8f);
            pRt.offsetMax = new Vector2(-8f, -8f);

            var img = pnl.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameWoodPanel();
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            barterOfferingPanel = pnl;

            // Inner Parchment
            var parch = new GameObject("Parchment");
            parch.transform.SetParent(pnl.transform, false);
            var prRt = parch.AddComponent<RectTransform>();
            prRt.anchorMin = Vector2.zero;
            prRt.anchorMax = Vector2.one;
            prRt.offsetMin = new Vector2(12f, 12f);
            prRt.offsetMax = new Vector2(-12f, -12f);
            var prImg = parch.AddComponent<Image>();
            prImg.sprite = UISpriteLoader.GetFrameParchment();
            prImg.type = Image.Type.Sliced;
            prImg.color = new Color(0.98f, 0.95f, 0.90f);

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parch.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -12f);
            tRt.sizeDelta = new Vector2(-30f, 28f);
            var tmp = titleObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<b>— KHU VỰC THƯƠNG THẢO TRAO ĐỔI (BARTER DESK) —</b>";
            tmp.fontSize = 15.5f;
            tmp.color = new Color(0.42f, 0.16f, 0.05f);
            tmp.alignment = TextAlignmentOptions.Center;

            // Target Item Text
            var tgtObj = new GameObject("Target_Item_Text");
            tgtObj.transform.SetParent(parch.transform, false);
            var tgRt = tgtObj.AddComponent<RectTransform>();
            tgRt.anchorMin = new Vector2(0f, 1f);
            tgRt.anchorMax = new Vector2(1f, 1f);
            tgRt.pivot = new Vector2(0.5f, 1f);
            tgRt.anchoredPosition = new Vector2(0f, -42f);
            tgRt.sizeDelta = new Vector2(-40f, 24f);
            barterTargetItemText = tgtObj.AddComponent<TextMeshProUGUI>();
            barterTargetItemText.fontSize = 14f;
            barterTargetItemText.color = new Color(0.35f, 0.15f, 0.05f);
            barterTargetItemText.alignment = TextAlignmentOptions.Center;

            // Requirement info
            var reqObj = new GameObject("Requirement_Info");
            reqObj.transform.SetParent(parch.transform, false);
            var rRt = reqObj.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 1f);
            rRt.anchorMax = new Vector2(1f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(0f, -68f);
            rRt.sizeDelta = new Vector2(-40f, 42f);
            barterRequirementText = reqObj.AddComponent<TextMeshProUGUI>();
            barterRequirementText.fontSize = 12.5f;
            barterRequirementText.alignment = TextAlignmentOptions.Center;

            // Sub-Tabs Bar: [🎒 Vật Phẩm Balo] [🐷 Đàn Heo Sống]
            var subTabBar = new GameObject("Sub_Tabs_Bar");
            subTabBar.transform.SetParent(parch.transform, false);
            var stbRt = subTabBar.AddComponent<RectTransform>();
            stbRt.anchorMin = new Vector2(0f, 1f);
            stbRt.anchorMax = new Vector2(1f, 1f);
            stbRt.pivot = new Vector2(0.5f, 1f);
            stbRt.anchoredPosition = new Vector2(0f, -114f);
            stbRt.sizeDelta = new Vector2(-40f, 34f);

            // Tab 1: Vật Phẩm
            var t1 = new GameObject("Tab_Items");
            t1.transform.SetParent(subTabBar.transform, false);
            var t1Rt = t1.AddComponent<RectTransform>();
            t1Rt.anchorMin = new Vector2(0.5f, 0.5f);
            t1Rt.anchorMax = new Vector2(0.5f, 0.5f);
            t1Rt.anchoredPosition = new Vector2(-110f, 0f);
            t1Rt.sizeDelta = new Vector2(210f, 32f);
            tabItemsImg = t1.AddComponent<Image>();
            tabItemsImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            tabItemsImg.type = Image.Type.Sliced;
            tabItemsBtn = t1.AddComponent<Button>();
            tabItemsBtn.onClick.AddListener(() => SwitchBarterSubTab(0));
            var t1Lbl = new GameObject("Text");
            t1Lbl.transform.SetParent(t1.transform, false);
            var t1lRt = t1Lbl.AddComponent<RectTransform>();
            t1lRt.anchorMin = Vector2.zero;
            t1lRt.anchorMax = Vector2.one;
            tabItemsText = t1Lbl.AddComponent<TextMeshProUGUI>();
            tabItemsText.text = "🎒 <b>Vật Phẩm Trong Túi Đồ</b>";
            tabItemsText.fontSize = 12.5f;
            tabItemsText.alignment = TextAlignmentOptions.Center;

            // Tab 2: Heo Sống
            var t2 = new GameObject("Tab_Pigs");
            t2.transform.SetParent(subTabBar.transform, false);
            var t2Rt = t2.AddComponent<RectTransform>();
            t2Rt.anchorMin = new Vector2(0.5f, 0.5f);
            t2Rt.anchorMax = new Vector2(0.5f, 0.5f);
            t2Rt.anchoredPosition = new Vector2(110f, 0f);
            t2Rt.sizeDelta = new Vector2(210f, 32f);
            tabPigsImg = t2.AddComponent<Image>();
            tabPigsImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            tabPigsImg.type = Image.Type.Sliced;
            tabPigsBtn = t2.AddComponent<Button>();
            tabPigsBtn.onClick.AddListener(() => SwitchBarterSubTab(1));
            var t2Lbl = new GameObject("Text");
            t2Lbl.transform.SetParent(t2.transform, false);
            var t2lRt = t2Lbl.AddComponent<RectTransform>();
            t2lRt.anchorMin = Vector2.zero;
            t2lRt.anchorMax = Vector2.one;
            tabPigsText = t2Lbl.AddComponent<TextMeshProUGUI>();
            tabPigsText.text = "🐷 <b>Đàn Heo Trang Trại</b>";
            tabPigsText.fontSize = 12.5f;
            tabPigsText.alignment = TextAlignmentOptions.Center;

            // ScrollView danh sách đồ/heo
            var scrollObj = new GameObject("Offering_ScrollView");
            scrollObj.transform.SetParent(parch.transform, false);
            var scRt = scrollObj.AddComponent<RectTransform>();
            scRt.anchorMin = Vector2.zero;
            scRt.anchorMax = Vector2.one;
            scRt.offsetMin = new Vector2(20f, 105f);
            scRt.offsetMax = new Vector2(-20f, -154f);

            var sRect = scrollObj.AddComponent<ScrollRect>();
            sRect.horizontal = false;
            sRect.vertical = true;
            sRect.movementType = ScrollRect.MovementType.Clamped;
            sRect.scrollSensitivity = 25f;

            var vObj = new GameObject("Viewport");
            vObj.transform.SetParent(scrollObj.transform, false);
            var vRt = vObj.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.offsetMin = Vector2.zero;
            vRt.offsetMax = Vector2.zero;
            vObj.AddComponent<Image>().color = Color.white;
            vObj.AddComponent<Mask>().showMaskGraphic = false;

            var cObj = new GameObject("Content");
            cObj.transform.SetParent(vObj.transform, false);
            var cRt = cObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0f, 200f);

            var vlg = cObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;

            var csf = cObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sRect.viewport = vRt;
            sRect.content = cRt;
            barterOfferingContent = cObj.transform;

            // Offering Tally
            var talObj = new GameObject("Tally_Info");
            talObj.transform.SetParent(parch.transform, false);
            var talRt = talObj.AddComponent<RectTransform>();
            talRt.anchorMin = new Vector2(0f, 0f);
            talRt.anchorMax = new Vector2(1f, 0f);
            talRt.anchoredPosition = new Vector2(0f, 62f);
            talRt.sizeDelta = new Vector2(-40f, 40f);
            barterOfferingTallyText = talObj.AddComponent<TextMeshProUGUI>();
            barterOfferingTallyText.fontSize = 13f;
            barterOfferingTallyText.alignment = TextAlignmentOptions.Center;

            // Action Buttons Container
            var btnBar = new GameObject("Btn_Bar");
            btnBar.transform.SetParent(parch.transform, false);
            var bRt = btnBar.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 18f);
            bRt.sizeDelta = new Vector2(-40f, 42f);

            // Confirm Button
            var confBtn = new GameObject("Btn_Confirm");
            confBtn.transform.SetParent(btnBar.transform, false);
            var cfRt = confBtn.AddComponent<RectTransform>();
            cfRt.anchorMin = new Vector2(0.5f, 0.5f);
            cfRt.anchorMax = new Vector2(0.5f, 0.5f);
            cfRt.anchoredPosition = new Vector2(-90f, 0f);
            cfRt.sizeDelta = new Vector2(170f, 38f);
            var cfImg = confBtn.AddComponent<Image>();
            cfImg.sprite = UISpriteLoader.GetFrameActionButton();
            cfImg.type = Image.Type.Sliced;
            cfImg.color = new Color(0.22f, 0.65f, 0.25f);
            confirmBarterBtn = confBtn.AddComponent<Button>();
            confirmBarterBtn.onClick.AddListener(ConfirmBarter);

            var cfLbl = new GameObject("Text");
            cfLbl.transform.SetParent(confBtn.transform, false);
            var cflRt = cfLbl.AddComponent<RectTransform>();
            cflRt.anchorMin = Vector2.zero;
            cflRt.anchorMax = Vector2.one;
            confirmBarterBtnText = cfLbl.AddComponent<TextMeshProUGUI>();
            confirmBarterBtnText.text = "<b>XÁC NHẬN ĐỔI</b>";
            confirmBarterBtnText.fontSize = 13.5f;
            confirmBarterBtnText.color = Color.white;
            confirmBarterBtnText.alignment = TextAlignmentOptions.Center;

            // Cancel Button
            var canBtn = new GameObject("Btn_Cancel");
            canBtn.transform.SetParent(btnBar.transform, false);
            var cnRt = canBtn.AddComponent<RectTransform>();
            cnRt.anchorMin = new Vector2(0.5f, 0.5f);
            cnRt.anchorMax = new Vector2(0.5f, 0.5f);
            cnRt.anchoredPosition = new Vector2(90f, 0f);
            cnRt.sizeDelta = new Vector2(150f, 38f);
            var cnImg = canBtn.AddComponent<Image>();
            cnImg.sprite = UISpriteLoader.GetFrameActionButton();
            cnImg.type = Image.Type.Sliced;
            cnImg.color = new Color(0.65f, 0.35f, 0.28f);
            var cnB = canBtn.AddComponent<Button>();
            cnB.onClick.AddListener(CloseBarterOfferingPanel);

            var cnLbl = new GameObject("Text");
            cnLbl.transform.SetParent(canBtn.transform, false);
            var cnlRt = cnLbl.AddComponent<RectTransform>();
            cnlRt.anchorMin = Vector2.zero;
            cnlRt.anchorMax = Vector2.one;
            var cnlTmp = cnLbl.AddComponent<TextMeshProUGUI>();
            cnlTmp.text = "<b>QUAY LẠI</b>";
            cnlTmp.fontSize = 13.5f;
            cnlTmp.color = Color.white;
            cnlTmp.alignment = TextAlignmentOptions.Center;

            pnl.SetActive(false);
        }
    }
}
