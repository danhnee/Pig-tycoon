using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Illustrated 2.5D Forest Mystery Event Popup (E01..E14).
    /// Displays the 320x112 2.5D event scene banner at the top, full canon narrative text, and interactive choices.
    /// </summary>
    public sealed class ForestEventPopup : MonoBehaviour
    {
        public static ForestEventPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

        private GameObject panel;
        private Image bannerImage;
        private TextMeshProUGUI title, body, footer;
        private Button choiceA, choiceB;
        private TextMeshProUGUI choiceAText, choiceBText;

        public static void Open()
        {
            if (Instance == null)
            {
                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                if (canvas == null) return;
                Instance = canvas.gameObject.AddComponent<ForestEventPopup>();
                Instance.Build(canvas.transform);
            }
            Instance.panel.SetActive(true);
            Instance.panel.transform.SetAsLastSibling();
            Instance.Refresh(null);
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        private void Build(Transform root)
        {
            panel = DayMarketPopup.BuildStyledPopupFrame(root, "ForestEventPopup", new Vector2(680f, 470f),
                out _, out var headerBanner, out var footerBanner);

            title = DayMarketPopup.Text(headerBanner, new Vector2(-24f, 0f), new Vector2(540f, 38f), 19, TextAlignmentOptions.Left);
            title.color = new Color(1f, 0.94f, 0.68f);

            var close = DayMarketPopup.Button(headerBanner, new Vector2(284f, 0f), new Vector2(68f, 32f), out var ct);
            ct.text = "Đóng";
            close.onClick.AddListener(Close);

            // 2.5D Illustrated Scene Banner (320x112 scaled to 612x168)
            var bannerFrame = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, 96f), new Vector2(620f, 174f));
            var bannerGo = new GameObject("EventBannerImg", typeof(RectTransform), typeof(Image));
            bannerGo.transform.SetParent(bannerFrame.transform, false);
            var brt = (RectTransform)bannerGo.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(604f, 160f);
            bannerImage = bannerGo.GetComponent<Image>();
            bannerImage.preserveAspect = false;

            var storyCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, -42f), new Vector2(620f, 94f));
            body = DayMarketPopup.Text(storyCard.transform, Vector2.zero, new Vector2(592f, 82f), 14.5f, TextAlignmentOptions.Center);
            body.textWrappingMode = TextWrappingModes.Normal;

            choiceA = DayMarketPopup.Button(panel.transform, new Vector2(-156f, -124f), new Vector2(286f, 40f), out choiceAText);
            choiceB = DayMarketPopup.Button(panel.transform, new Vector2(156f, -124f), new Vector2(286f, 40f), out choiceBText);
            choiceA.onClick.AddListener(() => Choose(0));
            choiceB.onClick.AddListener(() => Choose(1));

            footer = DayMarketPopup.Text(footerBanner, Vector2.zero, new Vector2(620f, 32f), 13, TextAlignmentOptions.Center);
            footer.color = new Color(0.78f, 0.94f, 0.80f);
            footer.text = "Ranh giới Rừng Nguyên Sinh — mọi lựa chọn đều ghi dấu lên đàn và trại.";
        }

        public static (string title, string a, string b) Labels(MysteryEventId id) => id switch
        {
            MysteryEventId.E01_VuonMuoiHaiQua => ("Vườn Mười Hai Quả", "Hái 3 quả lành", "Hái cạn cả vườn"),
            MysteryEventId.E02_DongCoKhongBong => ("Đồng Cỏ Không Bóng", "Thắp sáng đồng cỏ", "Để một con ở lại"),
            MysteryEventId.E03_KhoLanhBoQuen => ("Kho Lạnh Bỏ Quên", "Mở hầm lạnh", ""),
            MysteryEventId.E04_HatGiongTrang => ("Hạt Giống Trắng", "Gieo bên giếng cạn", ""),
            MysteryEventId.E05_BiNhanMuaGia => ("Bí Nhân Mua Già", "Đổi một heo già", "Từ chối"),
            MysteryEventId.E06_ChoKhongDongHo => ("Chợ Không Đồng Hồ", "Đổi vật tư lúc 02:00", ""),
            MysteryEventId.E07_GiamDinhVoDanh => ("Giám Định Vô Danh", "Nhờ giám định", ""),
            MysteryEventId.E08_DoiMotLayMot => ("Đổi Một Lấy Một", "Đổi", "Giữ nguyên đàn"),
            MysteryEventId.E09_ThuVienMucAn => ("Thư Viện Mực Ấn", "Đọc", ""),
            MysteryEventId.E12_GiuCongNguoc => ("Giữ Cổng Ngược", "Giữ cổng", "Buông"),
            MysteryEventId.E13_BaNgayKhoa => ("Ba Ngày Khóa", "Chờ", ""),
            MysteryEventId.E14_LoMoHuKhong => ("Lò Mổ Hư Không", "Bước vào", "Rời đi"),
            _ => (id.ToString(), "Thực hiện", ""),
        };

        public static string StoryPrompt(MysteryEventId id) => id switch
        {
            MysteryEventId.E01_VuonMuoiHaiQua => "Giữa tán rừng nguyên sinh hiện ra mười hai gốc cây kết trái vàng ấm áp. Hái vừa đủ 3 quả lành để bồi bổ đàn heo, hay tận thu cả vườn bất chấp lời cảnh báo cổ xưa?",
            MysteryEventId.E02_DongCoKhongBong => "Trảng cỏ xanh mướt dưới ánh trăng nhưng tuyệt nhiên không một ngọn cỏ nào đổ bóng. Một luồng khí lạnh rình rập quanh đàn heo đi cùng bạn.",
            MysteryEventId.E03_KhoLanhBoQuen => "Cánh cửa sắt rỉ sét dẫn xuống hầm lạnh thời trước biến cố. Bên trong còn lưu giữ các lọ huyết thanh và khẩu phần nén đặc biệt.",
            MysteryEventId.E04_HatGiongTrang => "Một túi hạt giống trắng ngần phát sáng dịu bên thành giếng cạn. Gieo xuống đất ẩm có thể đánh thức mạch nước ngầm tinh khiết.",
            MysteryEventId.E05_BiNhanMuaGia => "Người đàn ông khoác áo choàng đen dừng xe kéo giữa sương mù, ngỏ ý đổi một con heo già trong đàn lấy tín vật hiếm.",
            MysteryEventId.E06_ChoKhongDongHo => "Phiên chợ chập chờn dưới ánh đèn lồng tím nơi kim đồng hồ đứng yên ở 02:00 sáng. Mọi giao dịch chỉ diễn ra bằng vật tư thực.",
            MysteryEventId.E07_GiamDinhVoDanh => "Lão học giả mù ngồi bên bàn đá phủ rêu, chỉ cần chạm tay vào lưng heo là đọc thấu dòng gen ẩn chưa từng thức tỉnh.",
            MysteryEventId.E08_DoiMotLayMot => "Chiếc lồng gỗ chạm khắc cổ đặt giữa ngã ba rừng. Nếu để lại một cá thể trong đàn, chiếc lồng bên cạnh sẽ mở ra một giống heo lạ.",
            MysteryEventId.E09_ThuVienMucAn => "Những cuộn trúc và sổ cái đóng dấu mực đỏ nằm rải rác trong ngôi đền đổ nát, ghi chép bí quyết phối giống và hộ vệ trang trại.",
            MysteryEventId.E12_GiuCongNguoc => "Cánh cổng đá cổ rung chuyển trước áp lực từ bên kia khe nứt. Giữ chặt chốt cổng để bảo toàn đường rút hay buông tay rút lui sớm?",
            MysteryEventId.E13_BaNgayKhoa => "Phong ấn ba tầng khóa chặt kho tàng cổ giữa rừng sâu. Kiên nhẫn giải khóa sẽ thu về nguyên liệu quý.",
            MysteryEventId.E14_LoMoHuKhong => "Cấu trúc cơ khí dị giới lơ lửng giữa sương đỏ. Bước vào chấp nhận thử thách sinh tử để đoạt lõi năng lượng hay rút lui an toàn?",
            _ => "Một dị tượng bí ẩn xuất hiện giữa Rừng Nguyên Sinh. Hãy quyết định hành động của bạn."
        };

        private void Refresh(string result)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var exp = engine.ForestAndEvents.Expedition;
            if (!exp.IsActive)
            {
                title.text = "<b>ĐIỂM SỰ KIỆN RỪNG NGUYÊN SINH</b>";
                body.text = "Không có sự kiện nào đang diễn ra ở đây.";
                if (bannerImage != null) bannerImage.sprite = UISpriteLoader.GetEventBanner(MysteryEventId.E01_VuonMuoiHaiQua);
                choiceA.gameObject.SetActive(false);
                choiceB.gameObject.SetActive(false);
                return;
            }
            var (t, a, b) = Labels(exp.EventId);
            title.text = $"<b>DỊ TƯỢNG RỪNG — {t.ToUpperInvariant()}</b>";
            if (bannerImage != null)
            {
                bannerImage.sprite = UISpriteLoader.GetEventBanner(exp.EventId);
                bannerImage.enabled = bannerImage.sprite != null;
            }
            bool done = exp.CompletionPercent >= 100;
            body.text = result ?? (done
                ? "Sự kiện đã hoàn tất. Hãy quay lại cổng chắp vá để dẫn đàn trở về trang trại."
                : StoryPrompt(exp.EventId));
            choiceA.gameObject.SetActive(!done && a.Length > 0);
            choiceB.gameObject.SetActive(!done && b.Length > 0);
            choiceAText.text = a;
            choiceBText.text = b;
        }

        public void Choose(int choice)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var res = engine.ForestAndEvents.Director.ResolveActiveEvent(engine, choice);
            Debug.Log($"[ForestEvent] {res.message}");
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh(res.message);
        }
    }
}
