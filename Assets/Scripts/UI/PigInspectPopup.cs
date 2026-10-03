using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class PigInspectPopup : MonoBehaviour
    {
        public static PigInspectPopup Instance { get; private set; }

        [Header("Root Panel")]
        public GameObject ContentPanel;

        [Header("Text Fields")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI GeneText;
        public TextMeshProUGUI MoodText;
        public TextMeshProUGUI BondingHeartsText;
        public TextMeshProUGUI WeightText;
        public TextMeshProUGUI StageText;
        public TextMeshProUGUI MeatQualityText;
        public TextMeshProUGUI TraitsText;

        [Header("Visual Avatar")]
        public Image PigAvatarImage;
        public Button CloseButton;

        private Pig currentPig;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        public void Show(Pig pig)
        {
            if (pig == null) return;
            currentPig = pig;

            if (ContentPanel != null) ContentPanel.SetActive(true);

            // 1. Tên & Dòng Gen
            if (TitleText != null)
            {
                TitleText.text = $"{pig.Name} (ID: {pig.Id})";
            }
            if (GeneText != null)
            {
                string rColor = pig.Rarity == GeneRarity.HuyenThoai ? "#FFD700" : (pig.Rarity == GeneRarity.Hiem ? "#A335EE" : "#FFFFFF");
                GeneText.text = $"Dòng: <color={rColor}>{pig.GeneLine} [{pig.Rarity}]</color> | Giới tính: {pig.Gender}";
            }

            // 2. Avatar Heo
            if (PigAvatarImage != null)
            {
                PigAvatarImage.color = GetPigColor(pig.GeneLine);
            }

            // 3. Tâm Trạng
            if (MoodText != null)
            {
                string moodEmoji = pig.MoodState switch
                {
                    MoodState.BinhOn => "😊 Bình Ổn",
                    MoodState.BatAn => "😟 Bất An",
                    MoodState.HoangSo => "😨 Hoảng Sợ",
                    _ => "😱 Hoảng Loạn (Húc rào!)"
                };
                MoodText.text = $"Tâm trạng: {moodEmoji} ({pig.Mood}/100)";
            }

            // 4. Trái Tim Thân Thiết (Bonding Hearts phong cách Stardew Valley)
            if (BondingHeartsText != null)
            {
                int fullHearts = Mathf.Clamp(Mathf.RoundToInt(pig.Bonding / 20f), 0, 5);
                string hearts = "";
                for (int i = 0; i < 5; i++)
                {
                    hearts += (i < fullHearts) ? "❤️ " : "🖤 ";
                }
                BondingHeartsText.text = $"Thân thiết: {hearts} ({pig.Bonding:0.0}%)";
            }

            // 5. Cân nặng & Giai đoạn
            if (WeightText != null)
            {
                WeightText.text = $"Cân nặng: <b>{pig.WeightKg:0.0} kg</b> (Tối đa: {pig.MaxWeightKg:0.0} kg)";
            }
            if (StageText != null)
            {
                StageText.text = $"Giai đoạn: <b>{pig.Stage}</b> ({pig.AgeDays} ngày tuổi)";
            }

            // 6. Phẩm chất thịt
            if (MeatQualityText != null)
            {
                MeatQualityText.text = $"Phẩm chất thịt: <color=#FFA500>Hạng {pig.MeatQuality}</color>";
            }

            // 7. Đặc tính Gen (Traits)
            if (TraitsText != null)
            {
                if (pig.Traits != null && pig.Traits.Count > 0)
                {
                    string tStr = "Đặc tính:";
                    foreach (var t in pig.Traits)
                    {
                        tStr += $"\n• <b>{t.Name}</b>: {t.Description}";
                    }
                    TraitsText.text = tStr;
                }
                else
                {
                    TraitsText.text = "Đặc tính: Thuần chủng, không có dị biến.";
                }
            }
        }

        public void Hide()
        {
            if (ContentPanel != null)
            {
                ContentPanel.SetActive(false);
            }
        }

        private Color GetPigColor(GeneLineId geneLine)
        {
            return geneLine switch
            {
                GeneLineId.HongDien => new Color(1f, 0.72f, 0.78f),
                GeneLineId.LamKhe => new Color(0.55f, 0.85f, 0.95f),
                GeneLineId.KimTho => new Color(1f, 0.85f, 0.25f),
                GeneLineId.HuThe => new Color(0.72f, 0.45f, 0.95f),
                _ => new Color(0.9f, 0.8f, 0.7f)
            };
        }
    }
}
