using System.Collections.Generic;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Night auction house (Core NightAuction) and the night-market pet stall (Core PetMarket).
    /// Supports blueprint lots, gear lots (lot.IsGear with Quality I–IV), and rare item/paper lots (lot.IsItem × count).
    /// </summary>
    public sealed class AuctionPopup : MonoBehaviour
    {
        public enum Mode { Auction, Pets }

        public static AuctionPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

        private GameObject panel;
        private TextMeshProUGUI header, footer;
        private readonly List<(GameObject rowCard, Image icon, TextMeshProUGUI label, Button[] acts, TextMeshProUGUI[] actTexts)> rows =
            new List<(GameObject, Image, TextMeshProUGUI, Button[], TextMeshProUGUI[])>();
        private Mode mode;
        private string lastMessage = "";
        private static readonly float[] BidSteps = { 1f, 1.25f, 1.5f };

        public static void Open(Mode m)
        {
            if (Instance == null)
            {
                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                if (canvas == null) return;
                Instance = canvas.gameObject.AddComponent<AuctionPopup>();
                Instance.Build(canvas.transform);
            }
            Instance.mode = m;
            Instance.lastMessage = "";
            Instance.panel.SetActive(true);
            Instance.panel.transform.SetAsLastSibling();
            Instance.Refresh();
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        private void Build(Transform root)
        {
            panel = DayMarketPopup.BuildStyledPopupFrame(root, "AuctionPopup", new Vector2(760f, 488f),
                out _, out var headerBanner, out var footerBanner);

            header = DayMarketPopup.Text(headerBanner, new Vector2(-24f, 0f), new Vector2(620f, 38f), 18, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.94f, 0.68f);

            var close = DayMarketPopup.Button(headerBanner, new Vector2(326f, 0f), new Vector2(68f, 32f), out var ct);
            ct.text = "Đóng";
            close.onClick.AddListener(Close);

            for (int i = 0; i < 7; i++)
            {
                float y = 144f - i * 44f;
                var rowCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, y), new Vector2(708f, 40f));
                var icon = DayMarketPopup.MakePortraitIcon(rowCard.transform, new Vector2(-328f, 0f), new Vector2(32f, 32f));
                var label = DayMarketPopup.Text(rowCard.transform, new Vector2(-108f, 0f), new Vector2(390f, 36f), 13.5f, TextAlignmentOptions.Left);
                label.textWrappingMode = TextWrappingModes.Normal;
                var acts = new Button[3];
                var texts = new TextMeshProUGUI[3];
                for (int k = 0; k < 3; k++)
                {
                    int row = i, step = k;
                    acts[k] = DayMarketPopup.Button(rowCard.transform, new Vector2(130f + k * 92f, 0f), new Vector2(86f, 30f), out texts[k]);
                    acts[k].onClick.AddListener(() => Act(row, step));
                }
                rows.Add((rowCard, icon, label, acts, texts));
            }
            footer = DayMarketPopup.Text(footerBanner, Vector2.zero, new Vector2(700f, 34f), 13, TextAlignmentOptions.Center);
            footer.color = new Color(0.96f, 0.90f, 0.74f);
            footer.textWrappingMode = TextWrappingModes.Normal;
        }

        private static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        private static string PetPortraitKey(PetId id) => id switch
        {
            PetId.ChoChanDan => "pet_co",
            PetId.MeoKho => "pet_phieu",
            PetId.HeoSanNam => "pet_tron",
            PetId.QuaTin => "pet_tin",
            PetId.ChoNgaoCanh => "pet_mop",
            PetId.NgongBaoDong => "pet_giam_doc",
            PetId.ChoSanChien => "pet_rang",
            _ => "pet_co"
        };

        private static Sprite ResolveLotIcon(NightAuction.Lot lot)
        {
            if (lot == null) return UISpriteLoader.GetBlueprintSprite();
            // Lot API (Core NightAuction.Lot): gear lots carry a quality-qualified GearItemId ("id#III"),
            // item lots ItemId × ItemCount, blueprint lots BlueprintId.
            if (lot.IsGear)
                return UISpriteLoader.GetEquipIcon(lot.GearItemId) ?? UISpriteLoader.GetLootSprite(lot.GearItemId);
            if (lot.IsItem)
                return UISpriteLoader.GetLootSprite(lot.ItemId);
            return UISpriteLoader.GetBlueprintSprite(lot.BlueprintId);
        }

        private static string FormatLotTitle(NightAuction.Lot lot)
        {
            if (lot == null) return "";
            if (lot.IsGear)
            {
                var q = EquipmentQuality.QualityOf(lot.GearItemId);
                var qHex = ColorUtility.ToHtmlStringRGB(UISpriteLoader.GetQualityFrameColor(q));
                return $"<color=#{qHex}><b>[Trang bị · Phẩm {UISpriteLoader.QualityRoman(q)}] {lot.Name}</b></color>";
            }
            if (lot.IsItem)
            {
                string countTag = lot.ItemCount > 1 ? $" ×{lot.ItemCount}" : "";
                return $"<color=#FDE047><b>[Vật phẩm hiếm]</b></color> <b>{lot.Name}{countTag}</b>";
            }
            return $"<color=#93C5FD><b>[Bản vẽ]</b></color> <b>{lot.Name}</b>";
        }

        private void Refresh()
        {
            var engine = Engine;
            if (engine == null) return;
            if (mode == Mode.Auction)
            {
                engine.Auction.EnsureLots(engine);
                header.text = $"<b>ĐẤU GIÁ ĐÊM — BẢN VẼ, TRANG BỊ & BẢO VẬT</b>   •   <color=#FFE270>{engine.Farm.Gold:N0} G</color>";
                for (int i = 0; i < rows.Count; i++)
                {
                    var (rowCard, icon, label, acts, texts) = rows[i];
                    bool has = i < engine.Auction.Lots.Count;
                    rowCard.SetActive(has);
                    if (!has) continue;
                    var lot = engine.Auction.Lots[i];
                    if (icon != null)
                    {
                        icon.sprite = ResolveLotIcon(lot);
                        icon.enabled = icon.sprite != null;
                    }
                    var cardImg = rowCard.GetComponent<Image>();
                    if (cardImg != null)
                    {
                        cardImg.color = lot.IsGear
                            ? Color.Lerp(new Color(0.16f, 0.20f, 0.27f, 0.96f), UISpriteLoader.GetQualityFrameColor(EquipmentQuality.QualityOf(lot.GearItemId)), 0.22f)
                            : new Color(0.16f, 0.20f, 0.27f, 0.96f);
                    }
                    string title = FormatLotTitle(lot);
                    label.text = lot.Sold
                        ? $"{title} · <color=#9BE37B>{(lot.WonByPlayer ? "đã về trại" : "đã có chủ")}</color>"
                        : $"{title} · khởi điểm <color=#FFE270>{lot.ReserveGold} G</color>";
                    for (int k = 0; k < 3; k++)
                    {
                        acts[k].gameObject.SetActive(true);
                        texts[k].text = $"{Mathf.RoundToInt(lot.ReserveGold * BidSteps[k])} G";
                        acts[k].interactable = !lot.Sold;
                    }
                }
                footer.text = lastMessage.Length > 0 ? lastMessage
                    : engine.Farm.FarmLevel < NightAuction.UnlockFarmLevel ? $"Đấu giá đêm mở từ cấp trại {NightAuction.UnlockFarmLevel}."
                    : NightAuction.IsOpenHour(engine.Clock.CurrentHour) ? "Bỏ giá kín một lần cho mỗi lô. Thắng thì trả đúng giá đã bỏ." : "Đấu giá mở 18:00–06:00.";
            }
            else
            {
                header.text = "<b>CHỢ ĐÊM — LINH THÚ ĐỒNG HÀNH</b>";
                for (int i = 0; i < rows.Count; i++)
                {
                    var (rowCard, icon, label, acts, texts) = rows[i];
                    bool has = i < PetMarket.All.Length;
                    rowCard.SetActive(has);
                    if (!has) continue;
                    for (int k = 0; k < 3; k++) acts[k].gameObject.SetActive(k == 0);
                    var id = PetMarket.All[i];
                    bool owned = engine.PetsAndNpcs.HasPet(id);
                    if (icon != null)
                    {
                        icon.sprite = UISpriteLoader.LoadCharacterCellZero(PetPortraitKey(id));
                        icon.enabled = icon.sprite != null;
                    }
                    label.text = $"<b>{PetNpcAndOccultManager.GetPetDisplayName(id)}</b> · <color=#D8C89E>{(owned ? "đã ở trại" : PetMarket.Terms(id))}</color>";
                    texts[0].text = owned ? "Đã có" : "Đổi";
                    acts[0].interactable = !owned;
                }
                int slots = PetNpcAndOccultManager.PetSlotsFor(engine.Farm.FarmLevel);
                footer.text = lastMessage.Length > 0 ? lastMessage : $"Ô thú cưng: {engine.PetsAndNpcs.ActivePets.Count}/{slots}. Chợ đêm không nhận vàng cho thú cưng.";
            }
        }

        public void Act(int row, int step)
        {
            var engine = Engine;
            if (engine == null) return;
            if (mode == Mode.Auction)
            {
                if (row >= engine.Auction.Lots.Count) return;
                int bid = Mathf.RoundToInt(engine.Auction.Lots[row].ReserveGold * BidSteps[step]);
                lastMessage = engine.Auction.Bid(engine, row, bid).message;
            }
            else
            {
                if (row >= PetMarket.All.Length) return;
                lastMessage = PetMarket.Adopt(engine, PetMarket.All[row]).message;
            }
            Debug.Log($"[NightStall] {lastMessage}");
            PlayerInteractionController.Instance?.ShowFeedback(lastMessage);
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh();
        }
    }
}
