using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class VillageBarterPopup : MonoBehaviour
    {
        public static VillageBarterPopup Instance { get; private set; }

        public GameObject PopupPanel;
        public VillagerId CurrentVillagerId = VillagerId.BaSau;

        // UI References
        private TextMeshProUGUI villagerNameText;
        private TextMeshProUGUI villagerTitleText;
        private TextMeshProUGUI villagerLocationText;
        private TextMeshProUGUI atmosphereQuoteText;
        private TextMeshProUGUI statusBadgeText;
        private Transform listingsContainer;
        private TextMeshProUGUI feedbackText;

        // Barter Offering Selection State
        private NightMarketListing selectedListing = null;
        private GameObject barterOfferingPanel;
        private TextMeshProUGUI barterRequirementText;
        private TextMeshProUGUI barterOfferingTallyText;
        private Transform backpackItemsContainer;
        private Transform farmPigsContainer;
        private List<GameItem> selectedItemsToOffer = new List<GameItem>();
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
                Hide();
            }
        }

        public void OpenForVillager(VillagerId id)
        {
            CurrentVillagerId = id;
            EnsureUIExists();
            selectedListing = null;
            if (barterOfferingPanel != null) barterOfferingPanel.SetActive(false);
            if (PopupPanel != null) PopupPanel.SetActive(true);
            Refresh();
        }

        public void Show()
        {
            OpenForVillager(CurrentVillagerId);
        }

        public void Hide()
        {
            if (PopupPanel != null) PopupPanel.SetActive(false);
        }

        public void Refresh()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var profile = engine.Village.GetVillager(CurrentVillagerId);
            if (profile == null) return;

            // 1. Cập nhật thông tin dân làng
            if (villagerNameText != null) villagerNameText.text = profile.Name;
            if (villagerTitleText != null) villagerTitleText.text = profile.Title;
            if (villagerLocationText != null) villagerLocationText.text = $"<i>Vị trí: {profile.LocationDescription}</i>";
            if (atmosphereQuoteText != null) atmosphereQuoteText.text = $"\"{profile.AtmosphereQuote}\"";

            bool isNight = engine.Clock.GetTimeOfDay() == TimeOfDay.Toi;
            bool isOpen = isNight ? profile.IsOpenNight : profile.IsOpenDay;
            if (statusBadgeText != null)
            {
                statusBadgeText.text = isOpen ? "<color=#2e7d32>● Đang mở cửa</color>" : "<color=#c62828>● Đã đóng cửa then cài</color>";
            }

            // 2. Tái tạo danh sách hàng hóa
            if (listingsContainer != null)
            {
                foreach (Transform child in listingsContainer)
                {
                    Destroy(child.gameObject);
                }

                if (profile.Listings.Count == 0)
                {
                    var emptyObj = new GameObject("EmptyMsg");
                    emptyObj.transform.SetParent(listingsContainer, false);
                    var t = emptyObj.AddComponent<TextMeshProUGUI>();
                    t.text = "<i>(Hiện không có món nào sẵn sàng để trao đổi)</i>";
                    t.fontSize = 15f;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.4f, 0.35f, 0.3f);
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
            cRt.sizeDelta = new Vector2(720f, 76f);

            var img = cardObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;
            img.color = listing.IsSoldOut ? new Color(0.7f, 0.7f, 0.7f, 0.6f) : Color.white;

            // Info column
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var iRt = infoObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0f);
            iRt.anchorMax = new Vector2(0.68f, 1f);
            iRt.offsetMin = new Vector2(15f, 6f);
            iRt.offsetMax = new Vector2(-5f, -6f);

            var iTmp = infoObj.AddComponent<TextMeshProUGUI>();
            string soldOutTag = listing.IsSoldOut ? " <color=red>[ĐÃ ĐỔI HẾT]</color>" : "";
            iTmp.text = $"<b>{listing.OfferedItem.Name}</b> (x{listing.Quantity}){soldOutTag}\n" +
                        $"<size=12><color=#4e342e>{listing.OfferedItem.Description}</color></size>\n" +
                        $"<size=11.5><color=#1b5e20><b>Yêu cầu:</b> {listing.RequestedItemDescription}</color></size>";
            iTmp.fontSize = 13.5f;
            iTmp.color = new Color(0.2f, 0.12f, 0.05f);

            // Action Buttons
            if (!listing.IsSoldOut && isOpen)
            {
                float btnX = 640f;

                // Nút Đổi Barter
                var barterBtnObj = new GameObject("Btn_Barter");
                barterBtnObj.transform.SetParent(cardObj.transform, false);
                var bRt = barterBtnObj.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 0.5f);
                bRt.anchorMax = new Vector2(1f, 0.5f);
                bRt.pivot = new Vector2(1f, 0.5f);
                bRt.anchoredPosition = new Vector2(-15f, 0f);
                bRt.sizeDelta = new Vector2(105f, 38f);

                var bImg = barterBtnObj.AddComponent<Image>();
                bImg.sprite = UISpriteLoader.GetFrameActionButton();
                bImg.type = Image.Type.Sliced;
                bImg.color = new Color(0.85f, 0.55f, 0.25f);

                var bBtn = barterBtnObj.AddComponent<Button>();
                bBtn.onClick.AddListener(() => OpenBarterOfferingPanel(listing));

                var bLbl = new GameObject("Text");
                bLbl.transform.SetParent(barterBtnObj.transform, false);
                var blRt = bLbl.AddComponent<RectTransform>();
                blRt.anchorMin = Vector2.zero;
                blRt.anchorMax = Vector2.one;
                var blTmp = bLbl.AddComponent<TextMeshProUGUI>();
                blTmp.text = "<b>TRAO ĐỔI</b>";
                blTmp.fontSize = 12f;
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
                    gRt.anchoredPosition = new Vector2(-128f, 0f);
                    gRt.sizeDelta = new Vector2(105f, 38f);

                    var gImg = goldBtnObj.AddComponent<Image>();
                    gImg.sprite = UISpriteLoader.GetFrameActionButton();
                    gImg.type = Image.Type.Sliced;
                    gImg.color = new Color(0.95f, 0.75f, 0.15f);

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
                    glTmp.color = new Color(0.3f, 0.15f, 0.05f);
                    glTmp.alignment = TextAlignmentOptions.Center;
                }
            }
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
                Refresh();
            }
        }

        private void OpenBarterOfferingPanel(NightMarketListing listing)
        {
            selectedListing = listing;
            selectedItemsToOffer.Clear();
            selectedPigIdToOffer = null;

            if (barterOfferingPanel != null)
            {
                barterOfferingPanel.SetActive(true);
            }

            if (barterRequirementText != null)
            {
                barterRequirementText.text = $"<b>Đang đổi món:</b> {listing.OfferedItem.Name}\n" +
                                             $"<b>Yêu cầu tối thiểu:</b> {listing.MinimumReferenceValue}G GTTC ({listing.RequestedCategory})\n" +
                                             $"<color=#c62828><i>* Chú ý: Dư GTTC không thối lại. Không nhận heo dị biến.</i></color>";
            }

            RefreshBarterOfferingTally();
        }

        private void RefreshBarterOfferingTally()
        {
            if (selectedListing == null || barterOfferingTallyText == null) return;

            int totalOffered = 0;
            foreach (var item in selectedItemsToOffer)
            {
                totalOffered += item.ReferenceValueGold * item.Quantity;
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
            string status = totalOffered >= needed
                ? $"<color=#2e7d32><b>ĐỦ ĐIỀU KIỆN TRAO ĐỔI!</b> (Đã chào: {totalOffered}G / Cần: {needed}G GTTC)</color>" +
                  (excess > 0 ? $"\n<size=12><color=#e65100>Dư {excess}G GTTC: Theo tục lệ Bờ Rìa sẽ không hoàn lại!</color></size>" : "")
                : $"<color=#c62828><b>CHƯA ĐỦ GTTC!</b> (Đã chào: {totalOffered}G / Cần: {needed}G GTTC, thiếu {needed - totalOffered}G)</color>";

            barterOfferingTallyText.text = status;
        }

        public void ConfirmBarter()
        {
            if (selectedListing == null) return;

            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var res = engine.TradeWithVillager(
                CurrentVillagerId,
                selectedListing.SlotId,
                selectedItemsToOffer.Count > 0 ? selectedItemsToOffer : null,
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
                if (barterOfferingPanel != null) barterOfferingPanel.SetActive(false);
                selectedListing = null;
                Refresh();
            }
        }

        public void CloseBarterOfferingPanel()
        {
            if (barterOfferingPanel != null) barterOfferingPanel.SetActive(false);
            selectedListing = null;
        }

        public void EnsureUIExists()
        {
            if (PopupPanel != null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("Village_Barter_Popup");
            if (existing != null)
            {
                PopupPanel = existing.gameObject;
                return;
            }

            // Outer Popup Panel
            var popupObj = new GameObject("Village_Barter_Popup");
            popupObj.transform.SetParent(canvas.transform, false);
            var pRt = popupObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(780f, 620f);
            PopupPanel = popupObj;

            // Wood / Parchment Background
            var bgImg = popupObj.AddComponent<Image>();
            bgImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // Inner Parchment
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
            pImg.color = new Color(0.96f, 0.93f, 0.88f);

            // Close Button [X]
            var closeBtnObj = new GameObject("Btn_Close");
            closeBtnObj.transform.SetParent(parchObj.transform, false);
            var cRt = closeBtnObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(1f, 1f);
            cRt.anchoredPosition = new Vector2(-12f, -12f);
            cRt.sizeDelta = new Vector2(40f, 40f);
            var cImg = closeBtnObj.AddComponent<Image>();
            cImg.sprite = UISpriteLoader.GetIconCloseCross();
            cImg.color = Color.white;
            cImg.raycastTarget = true;
            var cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.RemoveAllListeners();
            cBtn.onClick.AddListener(Hide);

            var cLblObj = new GameObject("Label");
            cLblObj.transform.SetParent(closeBtnObj.transform, false);
            var clRt = cLblObj.AddComponent<RectTransform>();
            clRt.anchorMin = Vector2.zero;
            clRt.anchorMax = Vector2.one;
            clRt.offsetMin = Vector2.zero;
            clRt.offsetMax = Vector2.zero;
            var clTmp = cLblObj.AddComponent<TextMeshProUGUI>();
            clTmp.text = "<b>✕</b>";
            clTmp.fontSize = 20f;
            clTmp.fontStyle = FontStyles.Bold;
            clTmp.color = Color.white;
            clTmp.alignment = TextAlignmentOptions.Center;
            clTmp.raycastTarget = false;
            closeBtnObj.transform.SetAsLastSibling();

            // Villager Tabs Bar (Bà Sáu, Tám Vá, Cô Dung, Anh Ca, Ông Tồn)
            var tabContainer = new GameObject("Villager_Tabs");
            tabContainer.transform.SetParent(parchObj.transform, false);
            var tRt = tabContainer.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -14f);
            tRt.sizeDelta = new Vector2(-80f, 34f);

            var vList = new (VillagerId id, string name)[]
            {
                (VillagerId.BaSau, "Bà Sáu"),
                (VillagerId.TamVa, "Tám Vá"),
                (VillagerId.CoDung, "Cô Dung"),
                (VillagerId.AnhCa, "Anh Ca"),
                (VillagerId.OngTon, "Ông Tồn")
            };

            float tabW = 120f;
            float startX = -((vList.Length * tabW) / 2f) + (tabW / 2f);
            for (int i = 0; i < vList.Length; i++)
            {
                var v = vList[i];
                var tabBtn = new GameObject($"Tab_{v.name}");
                tabBtn.transform.SetParent(tabContainer.transform, false);
                var tbRt = tabBtn.AddComponent<RectTransform>();
                tbRt.anchorMin = new Vector2(0.5f, 0.5f);
                tbRt.anchorMax = new Vector2(0.5f, 0.5f);
                tbRt.anchoredPosition = new Vector2(startX + (i * tabW), 0f);
                tbRt.sizeDelta = new Vector2(tabW - 6f, 30f);

                var img = tabBtn.AddComponent<Image>();
                img.sprite = UISpriteLoader.GetFrameSlotNormal();
                img.type = Image.Type.Sliced;

                var btn = tabBtn.AddComponent<Button>();
                btn.onClick.AddListener(() => OpenForVillager(v.id));

                var lbl = new GameObject("Text");
                lbl.transform.SetParent(tabBtn.transform, false);
                var lblRt = lbl.AddComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                tmp.text = v.name;
                tmp.fontSize = 13f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.3f, 0.15f, 0.05f);
            }

            // Top Profile Section
            var profileBox = new GameObject("Profile_Box");
            profileBox.transform.SetParent(parchObj.transform, false);
            var prRt = profileBox.AddComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0f, 1f);
            prRt.anchorMax = new Vector2(1f, 1f);
            prRt.pivot = new Vector2(0.5f, 1f);
            prRt.anchoredPosition = new Vector2(0f, -54f);
            prRt.sizeDelta = new Vector2(-40f, 110f);

            var prImg = profileBox.AddComponent<Image>();
            prImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            prImg.type = Image.Type.Sliced;
            prImg.color = new Color(1f, 0.98f, 0.92f, 0.9f);

            // Name & Title
            var nameObj = new GameObject("Name_Title");
            nameObj.transform.SetParent(profileBox.transform, false);
            var nRt = nameObj.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.anchoredPosition = new Vector2(14f, -8f);
            nRt.sizeDelta = new Vector2(-28f, 24f);

            villagerNameText = nameObj.AddComponent<TextMeshProUGUI>();
            villagerNameText.text = "<b>Bà Sáu</b>";
            villagerNameText.fontSize = 17f;
            villagerNameText.fontStyle = FontStyles.Bold;
            villagerNameText.color = new Color(0.45f, 0.15f, 0.08f);

            // Status Badge
            var statusObj = new GameObject("Status_Badge");
            statusObj.transform.SetParent(profileBox.transform, false);
            var sRt = statusObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 1f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot = new Vector2(1f, 1f);
            sRt.anchoredPosition = new Vector2(-14f, -8f);
            sRt.sizeDelta = new Vector2(180f, 24f);
            statusBadgeText = statusObj.AddComponent<TextMeshProUGUI>();
            statusBadgeText.fontSize = 13f;
            statusBadgeText.alignment = TextAlignmentOptions.Right;

            // Quote Text Box
            var quoteObj = new GameObject("Quote_Text");
            quoteObj.transform.SetParent(profileBox.transform, false);
            var qRt = quoteObj.AddComponent<RectTransform>();
            qRt.anchorMin = Vector2.zero;
            qRt.anchorMax = Vector2.one;
            qRt.offsetMin = new Vector2(14f, 8f);
            qRt.offsetMax = new Vector2(-14f, -34f);
            atmosphereQuoteText = quoteObj.AddComponent<TextMeshProUGUI>();
            atmosphereQuoteText.fontSize = 13.5f;
            atmosphereQuoteText.fontStyle = FontStyles.Italic;
            atmosphereQuoteText.color = new Color(0.2f, 0.16f, 0.12f);
            atmosphereQuoteText.alignment = TextAlignmentOptions.TopLeft;

            // Center Listings Container (Scroll / Vertical List)
            var listObj = new GameObject("Listings_Container");
            listObj.transform.SetParent(parchObj.transform, false);
            var listRt = listObj.AddComponent<RectTransform>();
            listRt.anchorMin = Vector2.zero;
            listRt.anchorMax = Vector2.one;
            listRt.offsetMin = new Vector2(20f, 50f);
            listRt.offsetMax = new Vector2(-20f, -172f);

            var vlg = listObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            listingsContainer = listObj.transform;

            // Bottom Feedback Text
            var feedObj = new GameObject("Feedback_Text");
            feedObj.transform.SetParent(parchObj.transform, false);
            var fRt = feedObj.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0f, 0f);
            fRt.anchorMax = new Vector2(1f, 0f);
            fRt.pivot = new Vector2(0.5f, 0f);
            fRt.anchoredPosition = new Vector2(0f, 12f);
            fRt.sizeDelta = new Vector2(-40f, 30f);
            feedbackText = feedObj.AddComponent<TextMeshProUGUI>();
            feedbackText.fontSize = 13.5f;
            feedbackText.alignment = TextAlignmentOptions.Center;

            // Sub-Panel: Barter Offering
            BuildBarterOfferingSubPanel(parchObj.transform);
        }

        private void BuildBarterOfferingSubPanel(Transform parent)
        {
            var pnl = new GameObject("Barter_Offering_SubPanel");
            pnl.transform.SetParent(parent, false);
            var pRt = pnl.AddComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero;
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(10f, 10f);
            pRt.offsetMax = new Vector2(-10f, -10f);

            var img = pnl.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameWoodPanel();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.98f, 0.96f, 0.92f);
            barterOfferingPanel = pnl;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(pnl.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -22f);
            tRt.sizeDelta = new Vector2(-30f, 30f);
            var tmp = titleObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<b>— KHU VỰC ĐẶT HÀNG TRAO ĐỔI (BARTER) —</b>";
            tmp.fontSize = 16f;
            tmp.color = new Color(0.4f, 0.15f, 0.05f);
            tmp.alignment = TextAlignmentOptions.Center;

            // Requirement info
            var reqObj = new GameObject("Requirement_Info");
            reqObj.transform.SetParent(pnl.transform, false);
            var rRt = reqObj.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 1f);
            rRt.anchorMax = new Vector2(1f, 1f);
            rRt.anchoredPosition = new Vector2(0f, -65f);
            rRt.sizeDelta = new Vector2(-40f, 55f);
            barterRequirementText = reqObj.AddComponent<TextMeshProUGUI>();
            barterRequirementText.fontSize = 13.5f;
            barterRequirementText.alignment = TextAlignmentOptions.Center;

            // Offering Tally
            var talObj = new GameObject("Tally_Info");
            talObj.transform.SetParent(pnl.transform, false);
            var talRt = talObj.AddComponent<RectTransform>();
            talRt.anchorMin = new Vector2(0f, 0f);
            talRt.anchorMax = new Vector2(1f, 0f);
            talRt.anchoredPosition = new Vector2(0f, 75f);
            talRt.sizeDelta = new Vector2(-40f, 40f);
            barterOfferingTallyText = talObj.AddComponent<TextMeshProUGUI>();
            barterOfferingTallyText.fontSize = 13.5f;
            barterOfferingTallyText.alignment = TextAlignmentOptions.Center;

            // Action Buttons Container
            var btnBar = new GameObject("Btn_Bar");
            btnBar.transform.SetParent(pnl.transform, false);
            var bRt = btnBar.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 25f);
            bRt.sizeDelta = new Vector2(-40f, 42f);

            // Confirm Button
            var confBtn = new GameObject("Btn_Confirm");
            confBtn.transform.SetParent(btnBar.transform, false);
            var cfRt = confBtn.AddComponent<RectTransform>();
            cfRt.anchorMin = new Vector2(0.5f, 0.5f);
            cfRt.anchorMax = new Vector2(0.5f, 0.5f);
            cfRt.anchoredPosition = new Vector2(-80f, 0f);
            cfRt.sizeDelta = new Vector2(140f, 38f);
            var cfImg = confBtn.AddComponent<Image>();
            cfImg.sprite = UISpriteLoader.GetFrameActionButton();
            cfImg.type = Image.Type.Sliced;
            cfImg.color = new Color(0.2f, 0.6f, 0.2f);
            var cfB = confBtn.AddComponent<Button>();
            cfB.onClick.AddListener(ConfirmBarter);
            var cfLbl = new GameObject("Text");
            cfLbl.transform.SetParent(confBtn.transform, false);
            var cflRt = cfLbl.AddComponent<RectTransform>();
            cflRt.anchorMin = Vector2.zero;
            cflRt.anchorMax = Vector2.one;
            var cflTmp = cfLbl.AddComponent<TextMeshProUGUI>();
            cflTmp.text = "<b>XÁC NHẬN ĐỔI</b>";
            cflTmp.fontSize = 13f;
            cflTmp.color = Color.white;
            cflTmp.alignment = TextAlignmentOptions.Center;

            // Cancel Button
            var canBtn = new GameObject("Btn_Cancel");
            canBtn.transform.SetParent(btnBar.transform, false);
            var cnRt = canBtn.AddComponent<RectTransform>();
            cnRt.anchorMin = new Vector2(0.5f, 0.5f);
            cnRt.anchorMax = new Vector2(0.5f, 0.5f);
            cnRt.anchoredPosition = new Vector2(80f, 0f);
            cnRt.sizeDelta = new Vector2(140f, 38f);
            var cnImg = canBtn.AddComponent<Image>();
            cnImg.sprite = UISpriteLoader.GetFrameActionButton();
            cnImg.type = Image.Type.Sliced;
            cnImg.color = new Color(0.6f, 0.3f, 0.25f);
            var cnB = canBtn.AddComponent<Button>();
            cnB.onClick.AddListener(CloseBarterOfferingPanel);
            var cnLbl = new GameObject("Text");
            cnLbl.transform.SetParent(canBtn.transform, false);
            var cnlRt = cnLbl.AddComponent<RectTransform>();
            cnlRt.anchorMin = Vector2.zero;
            cnlRt.anchorMax = Vector2.one;
            var cnlTmp = cnLbl.AddComponent<TextMeshProUGUI>();
            cnlTmp.text = "<b>HỦY BỎ</b>";
            cnlTmp.fontSize = 13f;
            cnlTmp.color = Color.white;
            cnlTmp.alignment = TextAlignmentOptions.Center;

            pnl.SetActive(false);
        }
    }
}
