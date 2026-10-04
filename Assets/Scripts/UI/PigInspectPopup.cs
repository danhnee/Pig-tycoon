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

        private readonly List<Image> heartImages = new List<Image>();
        private GameObject heartsContainer;

        public void Show(Pig pig)
        {
            if (pig == null) return;
            currentPig = pig;

            if (ContentPanel != null)
            {
                ContentPanel.SetActive(true);

                // Nâng cấp khung gỗ bên ngoài
                var panelImg = ContentPanel.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.sprite = UISpriteLoader.GetFrameWoodPanel();
                    panelImg.type = Image.Type.Sliced;
                    panelImg.color = Color.white;
                }
                var pOutline = ContentPanel.GetComponent<Outline>();
                if (pOutline != null) pOutline.enabled = false;

                // Nâng cấp nền giấy cuộn bên trong
                Transform parchTr = ContentPanel.transform.Find("Parchment");
                if (parchTr != null)
                {
                    var parchImg = parchTr.GetComponent<Image>();
                    if (parchImg != null)
                    {
                        parchImg.sprite = UISpriteLoader.GetFrameParchment();
                        parchImg.type = Image.Type.Sliced;
                        parchImg.color = Color.white;
                    }
                    var parchOutline = parchTr.GetComponent<Outline>();
                    if (parchOutline != null) parchOutline.enabled = false;
                }
            }

            // Nâng cấp nút đóng cửa sổ
            if (CloseButton != null)
            {
                var cbImg = CloseButton.GetComponent<Image>();
                if (cbImg != null)
                {
                    cbImg.sprite = UISpriteLoader.GetIconCloseCross();
                    cbImg.color = Color.white;
                }
                var cbOutline = CloseButton.GetComponent<Outline>();
                if (cbOutline != null) cbOutline.enabled = false;
                var cbTxt = CloseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (cbTxt != null) cbTxt.enabled = false;
            }

            // 1. Tên & Dòng Gen
            if (TitleText != null)
            {
                TitleText.text = $"<b>{pig.Name}</b> <size=70%><color=#8C5E32>(ID: #{pig.Id})</color></size>";
            }
            if (GeneText != null)
            {
                string rColor = pig.Rarity == GeneRarity.HuyenThoai ? "#B45309" : (pig.Rarity == GeneRarity.Hiem ? "#7E22CE" : "#451A03");
                string genderStr = pig.Gender == "Duc" ? "Đực ♂" : "Cái ♀";
                GeneText.text = $"Dòng: <color={rColor}><b>{pig.GeneLine}</b> [{pig.Rarity}]</color>  |  Giới tính: <b>{genderStr}</b>";
            }

            // 2. Avatar Heo Pixel Art thật từ spritesheet
            if (PigAvatarImage != null)
            {
                var pigSprite = UISpriteLoader.GetPigAvatar(pig.GeneLine);
                if (pigSprite != null)
                {
                    PigAvatarImage.sprite = pigSprite;
                    PigAvatarImage.color = Color.white;
                    PigAvatarImage.preserveAspect = true;
                }
                var avatarBoxImg = PigAvatarImage.transform.parent?.GetComponent<Image>();
                if (avatarBoxImg != null && avatarBoxImg.gameObject != PigAvatarImage.gameObject)
                {
                    avatarBoxImg.sprite = UISpriteLoader.GetFrameSlotNormal();
                    avatarBoxImg.type = Image.Type.Sliced;
                    avatarBoxImg.color = Color.white;
                }
            }

            // 3. Trái Tim Thân Thiết (Dãy 5 trái tim pixel art Stardew Valley)
            int fullHearts = Mathf.Clamp(Mathf.RoundToInt(pig.Bonding / 20f), 0, 5);
            UpdateBondingHeartsVisual(fullHearts, pig.Bonding);

            // 4. Tâm Trạng
            if (MoodText != null)
            {
                string moodStateName = pig.MoodState switch
                {
                    MoodState.BinhOn => "Bình Ổn",
                    MoodState.BatAn => "Bất An",
                    MoodState.HoangSo => "Hoảng Sợ",
                    _ => "Hoảng Loạn (Húc rào!)"
                };
                string moodColor = pig.MoodState switch
                {
                    MoodState.BinhOn => "#15803D",
                    MoodState.BatAn => "#D97706",
                    _ => "#DC2626"
                };
                MoodText.text = $"Tâm trạng: <color={moodColor}><b>[{moodStateName}]</b></color> ({pig.Mood}/100)";
            }

            // 5. Cân nặng & Giai đoạn
            if (WeightText != null)
            {
                WeightText.text = $"Cân nặng: <b>{pig.WeightKg:0.0} kg</b> <size=85%><color=#78350F>(Tối đa: {pig.MaxWeightKg:0.0} kg)</color></size>";
            }
            if (StageText != null)
            {
                StageText.text = $"Giai đoạn: <b>{pig.Stage}</b> <size=85%><color=#78350F>({pig.AgeDays} ngày tuổi)</color></size>";
            }

            // 6. Phẩm chất thịt
            if (MeatQualityText != null)
            {
                MeatQualityText.text = $"Phẩm chất thịt: <color=#D97706><b>Hạng {pig.MeatQuality}</b></color>  |  Sức khỏe: <color=#15803D><b>Khỏe mạnh</b></color>";
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

        private void UpdateBondingHeartsVisual(int fullHearts, float bondingPercent)
        {
            if (BondingHeartsText == null) return;

            Transform parent = BondingHeartsText.transform.parent;
            if (parent == null) return;

            if (heartsContainer == null)
            {
                heartsContainer = new GameObject("Hearts_Container");
                heartsContainer.transform.SetParent(parent, false);
                var hRt = heartsContainer.AddComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0, 1);
                hRt.anchorMax = new Vector2(0, 1);
                hRt.pivot = new Vector2(0, 1);
                hRt.anchoredPosition = new Vector2(105, -73);
                hRt.sizeDelta = new Vector2(110, 20);

                heartImages.Clear();
                var fullSpr = UISpriteLoader.GetIconHeartFull();
                var emptySpr = UISpriteLoader.GetIconHeartEmpty();

                for (int i = 0; i < 5; i++)
                {
                    var hObj = new GameObject($"Heart_{i}");
                    hObj.transform.SetParent(heartsContainer.transform, false);
                    var rt = hObj.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = new Vector2(i * 21f, 0);
                    rt.sizeDelta = new Vector2(18f, 18f);

                    var img = hObj.AddComponent<Image>();
                    img.sprite = (i < fullHearts) ? fullSpr : emptySpr;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    heartImages.Add(img);
                }

                // Dời text phần trăm sang bên phải dãy trái tim
                var txtRt = BondingHeartsText.rectTransform;
                txtRt.anchoredPosition = new Vector2(215, -73);
                txtRt.sizeDelta = new Vector2(150, 22);
            }
            else
            {
                var fullSpr = UISpriteLoader.GetIconHeartFull();
                var emptySpr = UISpriteLoader.GetIconHeartEmpty();
                for (int i = 0; i < heartImages.Count; i++)
                {
                    if (heartImages[i] != null)
                    {
                        heartImages[i].sprite = (i < fullHearts) ? fullSpr : emptySpr;
                    }
                }
            }

            BondingHeartsText.text = $"<b>{bondingPercent:0.0}%</b> <size=85%>(Gắn kết)</size>";
            BondingHeartsText.color = new Color(0.75f, 0.15f, 0.22f);
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
