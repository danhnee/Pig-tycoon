using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD §5.4 "Giám định" at the town's appraisal house (day market channel: Hiếm 70%, Quý 40%).
    /// Styled with 32-PPU East–West Mythic-Epic wood/parchment/gold 9-slice frames, row slot cards, and breed portraits.
    /// </summary>
    public sealed class AppraisalPopup : MonoBehaviour
    {
        public static AppraisalPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

        private GameObject panel;
        private TextMeshProUGUI header, footer;
        private readonly List<(GameObject rowCard, Image icon, TextMeshProUGUI label, Button act, TextMeshProUGUI actText)> rows =
            new List<(GameObject, Image, TextMeshProUGUI, Button, TextMeshProUGUI)>();
        private string lastMessage = "";

        public static void Open()
        {
            if (Instance == null)
            {
                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                if (canvas == null) return;
                Instance = canvas.gameObject.AddComponent<AppraisalPopup>();
                Instance.Build(canvas.transform);
            }
            Instance.lastMessage = "";
            Instance.panel.SetActive(true);
            Instance.panel.transform.SetAsLastSibling();
            Instance.Refresh();
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        private void Build(Transform root)
        {
            panel = DayMarketPopup.BuildStyledPopupFrame(root, "AppraisalPopup", new Vector2(650f, 440f),
                out _, out var headerBanner, out var footerBanner);

            header = DayMarketPopup.Text(headerBanner, new Vector2(-24f, 0f), new Vector2(520f, 38f), 19, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.94f, 0.68f);

            var close = DayMarketPopup.Button(headerBanner, new Vector2(270f, 0f), new Vector2(68f, 32f), out var ct);
            ct.text = "Đóng";
            close.onClick.AddListener(Close);

            for (int i = 0; i < 6; i++)
            {
                float y = 120f - i * 44f;
                var rowCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, y), new Vector2(604f, 38f));
                var icon = DayMarketPopup.MakePortraitIcon(rowCard.transform, new Vector2(-278f, 0f), new Vector2(30f, 30f));
                var label = DayMarketPopup.Text(rowCard.transform, new Vector2(-34f, 0f), new Vector2(416f, 34f), 15, TextAlignmentOptions.Left);
                var act = DayMarketPopup.Button(rowCard.transform, new Vector2(226f, 0f), new Vector2(134f, 30f), out var at);
                int idx = i;
                act.onClick.AddListener(() => AppraiseRow(idx));
                rows.Add((rowCard, icon, label, act, at));
            }
            footer = DayMarketPopup.Text(footerBanner, Vector2.zero, new Vector2(580f, 34f), 13, TextAlignmentOptions.Center);
            footer.color = new Color(0.96f, 0.90f, 0.74f);
            footer.textWrappingMode = TextWrappingModes.Normal;
        }

        private static List<Pig> Unknown(GameEngine engine) => engine.Pigs.Where(p => p.IsAlive && !p.IsIdentified).ToList();

        private void Refresh()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            bool day = engine.Clock.CurrentHour >= 6 && engine.Clock.CurrentHour < 18;
            header.text = $"<b>NHÀ GIÁM ĐỊNH — CHỢ NGÀY</b>   •   <color=#FFE270>{engine.Farm.Gold:N0} G</color>";
            var list = Unknown(engine);
            for (int i = 0; i < rows.Count; i++)
            {
                var (rowCard, icon, label, act, at) = rows[i];
                bool has = i < list.Count;
                rowCard.SetActive(has);
                if (!has) continue;
                var pig = list[i];
                bool allowed = pig.Rarity <= GeneRarity.Quy;
                if (icon != null)
                {
                    icon.sprite = UISpriteLoader.GetPigAvatar(pig.GeneLine);
                    icon.enabled = icon.sprite != null;
                }
                label.text = $"<b>Heo \"?\" ({pig.Name})</b> · <color=#D8C89E>{PigText.Stage(pig.Stage)}</color> · {pig.WeightKg:0} kg";
                at.text = allowed ? "Giám định" : "Không thể ở đây";
                act.interactable = allowed && day;
            }
            footer.text = !string.IsNullOrEmpty(lastMessage) ? lastMessage
                : list.Count == 0 ? "Đàn không có heo mang dấu \"?\"."
                : day ? "Chợ ngày: Hiếm 70%, Quý 40%. Huyền Thoại / Dị Biến cần kênh khác." : "Nhà giám định mở 06:00–18:00.";
        }

        public void AppraiseRow(int i)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var list = Unknown(engine);
            if (i >= list.Count) return;
            var res = engine.AppraisePigGeneAtChannel(list[i].Id, AppraisalChannel.ChoNgay);
            lastMessage = res.message;
            Debug.Log($"[Appraisal] {res.message}");
            PlayerInteractionController.Instance?.ShowFeedback(lastMessage);
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh();
        }
    }
}
