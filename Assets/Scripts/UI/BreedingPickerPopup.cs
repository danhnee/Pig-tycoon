using System.Collections.Generic;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD 4.4 "Phối giống": mở từ thẻ heo (E). Liệt kê bạn phối khác giới đã trưởng thành (từ ngày 12, kể cả heo già),
    /// kèm tỉ lệ thụ thai GDD 4.8 bước 1; bấm "Phối" gọi GameEngine.SchedulePigBreeding.
    /// </summary>
    public sealed class BreedingPickerPopup : MonoBehaviour
    {
        public static BreedingPickerPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

        private const int RowsPerPage = 6;
        private GameObject panel;
        private TextMeshProUGUI header, footer;
        private readonly List<(GameObject rowCard, Image icon, TextMeshProUGUI label, Button act, TextMeshProUGUI actText)> rows =
            new List<(GameObject, Image, TextMeshProUGUI, Button, TextMeshProUGUI)>();
        private Button prevPage, nextPage;
        private string subjectId = "";
        private int page;
        private string lastMessage = "";

        public static void Open(Pig subject)
        {
            if (subject == null) return;
            if (Instance == null)
            {
                var go = new GameObject("BreedingPickerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = go.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 116;
                var sc = go.GetComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1280f, 720f);
                sc.matchWidthOrHeight = 0.5f;
                Instance = go.AddComponent<BreedingPickerPopup>();
                Instance.Build(go.transform);
            }
            Instance.subjectId = subject.Id;
            Instance.page = 0;
            Instance.lastMessage = "";
            Instance.panel.SetActive(true);
            Instance.panel.transform.SetAsLastSibling();
            Instance.Refresh();
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        private void Build(Transform root)
        {
            panel = DayMarketPopup.BuildStyledPopupFrame(root, "BreedingPickerPopup", new Vector2(660f, 440f),
                out _, out var headerBanner, out var footerBanner);

            header = DayMarketPopup.Text(headerBanner, new Vector2(-30f, 0f), new Vector2(540f, 38f), 18, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.94f, 0.68f);

            var close = DayMarketPopup.Button(headerBanner, new Vector2(276f, 0f), new Vector2(68f, 32f), out var ct);
            ct.text = "Đóng";
            close.onClick.AddListener(Close);

            for (int i = 0; i < RowsPerPage; i++)
            {
                float y = 120f - i * 44f;
                var rowCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, y), new Vector2(614f, 38f));
                var icon = DayMarketPopup.MakePortraitIcon(rowCard.transform, new Vector2(-284f, 0f), new Vector2(30f, 30f));
                var label = DayMarketPopup.Text(rowCard.transform, new Vector2(-40f, 0f), new Vector2(436f, 34f), 14, TextAlignmentOptions.Left);
                var act = DayMarketPopup.Button(rowCard.transform, new Vector2(236f, 0f), new Vector2(124f, 30f), out var at);
                int idx = i;
                act.onClick.AddListener(() => BreedRow(idx));
                rows.Add((rowCard, icon, label, act, at));
            }

            prevPage = DayMarketPopup.Button(footerBanner, new Vector2(-284f, 0f), new Vector2(50f, 28f), out var pt); pt.text = "<";
            nextPage = DayMarketPopup.Button(footerBanner, new Vector2(284f, 0f), new Vector2(50f, 28f), out var nt); nt.text = ">";
            prevPage.onClick.AddListener(() => { page = Mathf.Max(0, page - 1); Refresh(); });
            nextPage.onClick.AddListener(() => { page++; Refresh(); });
            footer = DayMarketPopup.Text(footerBanner, Vector2.zero, new Vector2(500f, 34f), 13, TextAlignmentOptions.Center);
            footer.color = new Color(0.96f, 0.90f, 0.74f);
            footer.textWrappingMode = TextWrappingModes.Normal;
        }

        private static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        private (Pig mother, Pig father) Pair(GameEngine engine, Pig subject, Pig partner) =>
            subject.Gender == "Cai" ? (subject, partner) : (partner, subject);

        private static string StageName(PigStage s) => s switch
        {
            PigStage.HeoNon => "Heo non",
            PigStage.DangLon => "Đang lớn",
            PigStage.TruongThanh => "Trưởng thành",
            _ => "Heo già"
        };

        private void Refresh()
        {
            var engine = Engine;
            if (engine == null || panel == null) return;
            var subject = engine.Pigs.Find(p => p.Id == subjectId);
            if (subject == null || !subject.IsAlive) { Close(); return; }

            string role = subject.Gender == "Cai" ? "nái" : "đực";
            header.text = $"<b>PHỐI GIỐNG</b> — {role} <color=#FFE270>{subject.Name}</color>";
            var partners = engine.ListBreedingPartners(subject.Id);
            int pages = Mathf.Max(1, Mathf.CeilToInt(partners.Count / (float)RowsPerPage));
            page = Mathf.Clamp(page, 0, pages - 1);

            for (int i = 0; i < RowsPerPage; i++)
            {
                int k = page * RowsPerPage + i;
                var (rowCard, icon, label, act, at) = rows[i];
                bool has = k < partners.Count;
                rowCard.SetActive(has);
                if (!has) continue;
                var partner = partners[k];
                var (mother, father) = Pair(engine, subject, partner);
                var (ok, chance, msg) = engine.PreviewPigBreeding(mother.Id, father.Id);
                if (icon != null)
                {
                    icon.sprite = UISpriteLoader.GetPigAvatar(partner.GeneLine);
                    icon.enabled = icon.sprite != null;
                }
                string line = PigText.Line(partner);
                string inbred = engine.IsInbredPair(mother, father) ? " · <color=#F87171>cận huyết (-2 con)</color>" : "";
                label.text = ok
                    ? $"<b>{partner.Name}</b> · {line} · {PigText.Stage(partner.Stage)} · thụ thai <color=#86EFAC>{chance * 100f:0.#}%</color>{inbred}"
                    : $"<b>{partner.Name}</b> · <color=#FCA5A5>{msg}</color>";
                at.text = ok ? "Phối" : "Chưa được";
                act.interactable = ok;
            }

            prevPage.gameObject.SetActive(page > 0);
            nextPage.gameObject.SetActive(page < pages - 1);
            footer.text = !string.IsNullOrEmpty(lastMessage) ? lastMessage
                : partners.Count == 0
                    ? (subject.AgeDays < BreedingLedger.MinAgeDays || (subject.Stage != PigStage.TruongThanh && subject.Stage != PigStage.HeoGia)
                        ? $"{subject.Name} chưa đủ tuổi phối (từ ngày {BreedingLedger.MinAgeDays})."
                        : "Không có bạn phối khác giới đã trưởng thành trong trại.")
                    : $"Mang thai {BreedingLedger.GestationDays} ngày · nghỉ {BreedingLedger.RestDays} ngày sau đẻ · tối đa {BreedingLedger.MaxLitters} lứa · 1 lần phối/nái/ngày";
        }

        public void BreedRow(int i)
        {
            var engine = Engine;
            if (engine == null) return;
            var subject = engine.Pigs.Find(p => p.Id == subjectId);
            if (subject == null) return;
            var partners = engine.ListBreedingPartners(subject.Id);
            int k = page * RowsPerPage + i;
            if (k >= partners.Count) return;
            var (mother, father) = Pair(engine, subject, partners[k]);
            var res = engine.SchedulePigBreeding(mother.Id, father.Id);
            lastMessage = res.message;
            Debug.Log($"[Breeding] {res.message}");
            PlayerInteractionController.Instance?.ShowFeedback(res.message);
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            if (PigInspectPopup.Instance != null) PigInspectPopup.Instance.Refresh();
            Refresh();
        }
    }
}
