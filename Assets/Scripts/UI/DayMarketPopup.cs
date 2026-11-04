using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD 20 "Chợ Ngày (06:00–18:00) — vàng": the player sells pigs for gold at the market counters of Market_Town.
    /// Styled with 32-PPU East–West Mythic-Epic wood/parchment/gold 9-slice frames, row slot cards, and breed portraits.
    /// </summary>
    public sealed class DayMarketPopup : MonoBehaviour
    {
        public static DayMarketPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

        private const int RowsPerPage = 8;
        private GameObject panel;
        private TextMeshProUGUI header, footer;
        private readonly List<(GameObject rowCard, Image icon, TextMeshProUGUI label, Button sell, TextMeshProUGUI sellText)> rows =
            new List<(GameObject, Image, TextMeshProUGUI, Button, TextMeshProUGUI)>();
        private Button quarantineToggle, prevPage, nextPage, feedButton, modeButton;
        private TextMeshProUGUI quarantineText, feedText, modeText;
        private bool buyMode; // C-02: "Mua heo" tab next to selling
        private bool quarantine = true;
        private int page;
        private string lastMessage = "";

        public static void Open()
        {
            if (Instance == null)
            {
                var go = new GameObject("DayMarketCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = go.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 115;
                var sc = go.GetComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1280f, 720f);
                sc.matchWidthOrHeight = 0.5f;
                Instance = go.AddComponent<DayMarketPopup>();
                Instance.Build(go.transform);
            }
            Instance.page = 0;
            Instance.lastMessage = "";
            Instance.panel.SetActive(true);
            Instance.panel.transform.SetAsLastSibling();
            Instance.Refresh();
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        private void Build(Transform root)
        {
            panel = BuildStyledPopupFrame(root, "DayMarketPopup", new Vector2(680f, 510f), out var bodyRect, out var headerBanner, out var footerBanner);

            header = Text(headerBanner, new Vector2(-24f, 0f), new Vector2(560f, 38f), 20, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.94f, 0.68f);

            var close = Button(headerBanner, new Vector2(286f, 0f), new Vector2(68f, 32f), out var closeText);
            closeText.text = "Đóng";
            close.onClick.AddListener(Close);

            modeButton = Button(headerBanner, new Vector2(186f, 0f), new Vector2(112f, 32f), out modeText);
            modeButton.onClick.AddListener(() => { buyMode = !buyMode; page = 0; lastMessage = ""; Refresh(); });

            quarantineToggle = Button(panel.transform, new Vector2(-170f, 178f), new Vector2(300f, 32f), out quarantineText);
            quarantineToggle.onClick.AddListener(() => { quarantine = !quarantine; Refresh(); });

            // Bao cám: the farm silo only refills here (Core TroughCommands.BuyFeedSack)
            feedButton = Button(panel.transform, new Vector2(170f, 178f), new Vector2(300f, 32f), out feedText);
            feedButton.onClick.AddListener(() =>
            {
                var eng = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
                if (eng == null) return;
                var r = TroughCommands.BuyFeedSack(eng);
                PlayerInteractionController.Instance?.ShowFeedback(r.message);
                Refresh();
            });

            for (int i = 0; i < RowsPerPage; i++)
            {
                float y = 132f - i * 38f;
                var rowCard = MakeSlotCard(panel.transform, new Vector2(0f, y), new Vector2(632f, 34f));
                var icon = MakePortraitIcon(rowCard.transform, new Vector2(-292f, 0f), new Vector2(28f, 28f));
                var label = Text(rowCard.transform, new Vector2(-42f, 0f), new Vector2(440f, 32f), 15, TextAlignmentOptions.Left);
                var sell = Button(rowCard.transform, new Vector2(242f, 0f), new Vector2(128f, 28f), out var st);
                int idx = i;
                sell.onClick.AddListener(() => SellRow(idx));
                rows.Add((rowCard, icon, label, sell, st));
            }

            prevPage = Button(footerBanner, new Vector2(-280f, 0f), new Vector2(56f, 28f), out var pt); pt.text = "<";
            nextPage = Button(footerBanner, new Vector2(280f, 0f), new Vector2(56f, 28f), out var nt); nt.text = ">";
            prevPage.onClick.AddListener(() => { page = Mathf.Max(0, page - 1); Refresh(); });
            nextPage.onClick.AddListener(() => { page++; Refresh(); });
            footer = Text(footerBanner, Vector2.zero, new Vector2(480f, 32f), 14, TextAlignmentOptions.Center);
            footer.color = new Color(0.96f, 0.90f, 0.74f);
        }

        // C-01: on the real farm only the pigs driven to the market can be sold here
        private List<(Pig pig, int price)> Sellable(GameEngine engine) =>
            engine.Pigs.Where(p => p.IsAlive && (!engine.InteractiveHerdOwned || engine.Drove.IsAway(p.Id)))
                .Select(p => (p, engine.Economy.CalculatePigSalePrice(p, engine.Farm.FarmLevel).priceGold))
                .OrderByDescending(t => t.Item2).ToList();

        private void Refresh()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null || panel == null) return;
            int hour = engine.Clock.CurrentHour;
            bool open = hour >= 6 && hour < 18;
            var dm = engine.Economy.DayMarket;
            // GDD 20.2: the running cycle theme (and the post-raid scarcity) set today's pig price and buying power
            string scarcity = dm.PostRaidScarcityDaysRemaining > 0 ? $" <color=#FF9A6B>· heo khan hiếm +{dm.PostRaidScarcityBonus:P0}</color>" : "";
            header.text = $"<b>CHỢ NGÀY</b> · Chủ đề: <color=#9FE0FF>{MarketCycleThemes.NameVi(dm.Theme)}</color>{scarcity}   •   <color=#FFE270>{engine.Farm.Gold:N0} G</color>   •   {hour:00}:{engine.Clock.CurrentMinute:00}";
            if (feedText != null) feedText.text = $"Mua bao cám {TroughCommands.FeedSackKg:0}kg · {TroughCommands.FeedSackPrice(engine)}G (kho {engine.Farm.FeedStockKg:0}kg)";
            quarantineText.text = quarantine ? $"Kiểm dịch trước khi bán: CÓ (-{dm.QuarantineFeePerPig} G)" : "Kiểm dịch trước khi bán: KHÔNG";
            if (modeText != null) modeText.text = buyMode ? "Bán heo" : "Mua heo";
            quarantineToggle.gameObject.SetActive(!buyMode);
            if (buyMode) { RefreshBuy(engine, open); return; }
            var list = Sellable(engine);
            int pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)RowsPerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < RowsPerPage; i++)
            {
                int k = page * RowsPerPage + i;
                var (rowCard, icon, label, sell, st) = rows[i];
                bool has = k < list.Count;
                rowCard.SetActive(has);
                if (!has) continue;
                var (pig, price) = list[k];
                // GDD 8.1 Mắt Nhà Nghề: con An đã kiểm miễn phí thì không mất phí kiểm dịch
                int net = !quarantine || ProtagonistTraits.IsQuarantineFeeWaived(engine, pig.Id) ? price : Mathf.Max(10, price - dm.QuarantineFeePerPig);
                if (icon != null)
                {
                    icon.sprite = UISpriteLoader.GetPigAvatar(pig.GeneLine);
                    icon.enabled = icon.sprite != null;
                }
                label.text = $"<b>{pig.Name}</b>  ·  <color=#D8C89E>{PigText.Line(pig)}</color>  ·  {PigText.Stage(pig.Stage)}  ·  {pig.WeightKg:0} kg";
                st.text = price > 0 ? $"Bán {net} G" : "Không bán";
                sell.interactable = open && price > 0;
            }
            prevPage.gameObject.SetActive(page > 0);
            nextPage.gameObject.SetActive(page < pages - 1);
            footer.text = !open ? "Chợ ngày mở 06:00–18:00. Ban đêm ra chợ đêm (trao đổi)."
                : list.Count == 0 && engine.InteractiveHerdOwned ? "Chưa có heo để bán: ở trại, mở thẻ heo → \"Đưa vào đoàn đi chợ\", rồi dẫn đoàn ra chợ."
                : string.IsNullOrEmpty(lastMessage) ? $"Đã bán hôm nay: {dm.CurrentPigsSoldToday}/{dm.DailyPurchasingPowerCap} con · trang {page + 1}/{pages}"
                : lastMessage;
        }

        private void RefreshBuy(GameEngine engine, bool open)
        {
            var offers = MarketPigShop.TodayOffers(engine).Where(o => !o.Sold).ToList();
            int pages = Mathf.Max(1, Mathf.CeilToInt(offers.Count / (float)RowsPerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < RowsPerPage; i++)
            {
                int k = page * RowsPerPage + i;
                var (rowCard, icon, label, sell, st) = rows[i];
                bool has = k < offers.Count;
                rowCard.SetActive(has);
                if (!has) continue;
                var o = offers[k];
                if (icon != null) { icon.sprite = UISpriteLoader.GetPigAvatar(o.Line); icon.enabled = icon.sprite != null; }
                label.text = $"<b>{PigText.Line(o.Line, o.Rarity, false)}</b>  ·  {PigText.Rarity(o.Rarity)}  ·  {PigText.Stage(o.Stage)}  ·  {PigText.Gender(o.Gender)}  ·  {o.WeightKg:0} kg";
                st.text = $"Mua {o.PriceGold} G";
                sell.interactable = open;
            }
            prevPage.gameObject.SetActive(page > 0);
            nextPage.gameObject.SetActive(page < pages - 1);
            footer.text = !open ? "Chợ ngày bán heo 06:00–18:00."
                : string.IsNullOrEmpty(lastMessage) ? $"Heo mua theo đoàn về trại · đoàn {engine.Drove.PigIds.Count}/{MarketDrove.MaxDroveSize}"
                : lastMessage;
        }

        public void SellRow(int i)
        {
            if (buyMode) { BuyRow(i); return; }
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var list = Sellable(engine);
            int k = page * RowsPerPage + i;
            if (k >= list.Count) return;
            var pig = list[k].pig;
            var res = engine.SellPigAtDayMarket(pig.Id, quarantine);
            lastMessage = res.success ? $"Đã bán {pig.Name}: +{res.goldEarned} G" : res.message;
            if (res.success)
            {
                foreach (var v in FindObjectsByType<PigAgentView>())
                    if (v != null && v.PigModel == pig) Destroy(v.gameObject);
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
            }
            Debug.Log($"[DayMarket] {lastMessage}");
            PlayerInteractionController.Instance?.ShowFeedback(lastMessage);
            Refresh();
        }

        private void BuyRow(int i)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var offers = MarketPigShop.TodayOffers(engine).Where(o => !o.Sold).ToList();
            int k = page * RowsPerPage + i;
            if (k >= offers.Count) return;
            var r = MarketPigShop.Buy(engine, offers[k].OfferId);
            lastMessage = r.message;
            PlayerInteractionController.Instance?.ShowFeedback(r.message);
            if (r.ok) MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh();
        }

        // ------------------------------------------------------------------
        // Shared Mythic-Epic 32-PPU Popup Builders (Used across Town/Forest/Quest Popups)
        // ------------------------------------------------------------------
        internal static GameObject BuildStyledPopupFrame(
            Transform root,
            string name,
            Vector2 size,
            out RectTransform bodyParchment,
            out RectTransform headerBanner,
            out RectTransform footerBanner)
        {
            var p = new GameObject(name, typeof(RectTransform), typeof(Image));
            p.transform.SetParent(root, false);
            var rt = (RectTransform)p.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var bg = p.GetComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.92f, 0.86f, 0.78f, 0.99f);

            // Inner recessed parchment body
            var bodyGo = new GameObject("InnerParchment", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(p.transform, false);
            bodyParchment = (RectTransform)bodyGo.transform;
            bodyParchment.anchorMin = Vector2.zero;
            bodyParchment.anchorMax = Vector2.one;
            bodyParchment.offsetMin = new Vector2(14f, 54f);
            bodyParchment.offsetMax = new Vector2(-14f, -56f);
            var bodyImg = bodyGo.GetComponent<Image>();
            bodyImg.sprite = UISpriteLoader.GetFrameParchment();
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = new Color(0.08f, 0.11f, 0.16f, 0.97f); // Master Spec #121925 Deep Slate

            // Top Gold/Crimson Header Banner
            var hdrGo = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
            hdrGo.transform.SetParent(p.transform, false);
            headerBanner = (RectTransform)hdrGo.transform;
            headerBanner.anchorMin = new Vector2(0.5f, 1f);
            headerBanner.anchorMax = new Vector2(0.5f, 1f);
            headerBanner.pivot = new Vector2(0.5f, 1f);
            headerBanner.anchoredPosition = new Vector2(0f, -8f);
            headerBanner.sizeDelta = new Vector2(size.x - 24f, 44f);
            var hdrImg = hdrGo.GetComponent<Image>();
            hdrImg.sprite = UISpriteLoader.GetFrameSlotSelected();
            hdrImg.type = Image.Type.Sliced;
            hdrImg.color = new Color(0.15f, 0.20f, 0.28f, 0.98f); // Master Spec #253246 Elevated Header

            // Bottom Footer Strip
            var ftrGo = new GameObject("FooterBanner", typeof(RectTransform), typeof(Image));
            ftrGo.transform.SetParent(p.transform, false);
            footerBanner = (RectTransform)ftrGo.transform;
            footerBanner.anchorMin = new Vector2(0.5f, 0f);
            footerBanner.anchorMax = new Vector2(0.5f, 0f);
            footerBanner.pivot = new Vector2(0.5f, 0f);
            footerBanner.anchoredPosition = new Vector2(0f, 10f);
            footerBanner.sizeDelta = new Vector2(size.x - 28f, 38f);
            var ftrImg = ftrGo.GetComponent<Image>();
            ftrImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            ftrImg.type = Image.Type.Sliced;
            ftrImg.color = new Color(0.09f, 0.13f, 0.19f, 0.96f);

            UISpriteLoader.Apply25DWindowHierarchy(p.transform);
            return p;
        }

        internal static GameObject MakeSlotCard(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("SlotCard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.15f, 0.20f, 0.28f, 0.96f); // Master Spec #253246 Card Surface
            return go;
        }

        internal static Image MakePortraitIcon(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("PortraitIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        internal static TextMeshProUGUI Text(Transform parent, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = new Color(0.98f, 0.92f, 0.78f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        internal static Button Button(Transform parent, Vector2 pos, Vector2 size, out TextMeshProUGUI label)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameActionButton();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.96f, 0.92f, 0.84f, 1f);
            label = Text(go.transform, Vector2.zero, size, 14, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.22f, 0.13f, 0.06f);
            return go.GetComponent<Button>();
        }
    }
}
