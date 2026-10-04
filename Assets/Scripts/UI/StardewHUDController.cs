using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class StardewHUDController : MonoBehaviour
    {
        public static StardewHUDController Instance { get; private set; }

        [Header("Top-Right Stardew Clock & Calendar")]
        public TextMeshProUGUI TimeText;
        public TextMeshProUGUI DayPartWeatherText;
        public TextMeshProUGUI GoldText;
        public TextMeshProUGUI FarmStatusText;

        [Header("Bottom-Right Stardew Energy Bar")]
        public Image EnergyFillBar;
        public TextMeshProUGUI EnergyText;

        [Header("Player & Controls")]
        public PlayerMobileController PlayerController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeVisuals();

            // Khởi tạo Dev Mode Button & Popup nếu chưa có
            var dev = DevModePopup.Instance;
            if (dev == null)
            {
                dev = gameObject.AddComponent<DevModePopup>();
            }
            dev.EnsureUIExists();

            if (MobileGameController.Instance != null)
            {
                MobileGameController.Instance.OnStateUpdated += RefreshHUD;
            }
            RefreshHUD();
        }

        private void InitializeVisuals()
        {
            // 1. Cấu hình bảng đồng hồ góc trên bên phải
            Transform clockPanelTr = transform.Find("Stardew_Clock_Panel");
            if (clockPanelTr != null)
            {
                var panelImg = clockPanelTr.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.sprite = UISpriteLoader.GetFrameWoodPanel();
                    panelImg.type = Image.Type.Sliced;
                    panelImg.color = Color.white;
                }
                var panelRt = clockPanelTr.GetComponent<RectTransform>();
                if (panelRt != null)
                {
                    panelRt.sizeDelta = new Vector2(310f, 162f);
                }

                // Gỡ bỏ outline thô nếu có
                var outline = clockPanelTr.GetComponent<Outline>();
                if (outline != null) outline.enabled = false;

                // Căn chỉnh DateWeatherText
                if (DayPartWeatherText != null)
                {
                    var rt = DayPartWeatherText.rectTransform;
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -8f);
                    rt.sizeDelta = new Vector2(290f, 26f);
                    DayPartWeatherText.fontSize = 18f;
                    DayPartWeatherText.fontStyle = FontStyles.Bold;
                    DayPartWeatherText.color = new Color(0.25f, 0.12f, 0.03f);
                }

                // Căn chỉnh TimeText
                if (TimeText != null)
                {
                    var rt = TimeText.rectTransform;
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -36f);
                    rt.sizeDelta = new Vector2(290f, 34f);
                    TimeText.fontSize = 28f;
                    TimeText.fontStyle = FontStyles.Bold;
                    TimeText.color = new Color(0.18f, 0.08f, 0.02f);
                }

                // Căn chỉnh GoldBox
                Transform goldBoxTr = clockPanelTr.Find("GoldBox");
                if (goldBoxTr != null)
                {
                    var gRt = goldBoxTr.GetComponent<RectTransform>();
                    if (gRt != null)
                    {
                        gRt.anchorMin = new Vector2(0.5f, 1f);
                        gRt.anchorMax = new Vector2(0.5f, 1f);
                        gRt.pivot = new Vector2(0.5f, 1f);
                        gRt.anchoredPosition = new Vector2(0f, -76f);
                        gRt.sizeDelta = new Vector2(275f, 32f);
                    }
                    var gImg = goldBoxTr.GetComponent<Image>();
                    if (gImg != null)
                    {
                        gImg.sprite = UISpriteLoader.GetFrameSlotNormal();
                        gImg.type = Image.Type.Sliced;
                        gImg.color = Color.white;
                    }
                    var gOutline = goldBoxTr.GetComponent<Outline>();
                    if (gOutline != null) gOutline.enabled = false;

                    // Thêm icon đồng tiền vàng vào bên trái hộp vàng nếu chưa có
                    Transform iconTr = goldBoxTr.Find("GoldIcon");
                    if (iconTr == null)
                    {
                        var iconObj = new GameObject("GoldIcon");
                        iconObj.transform.SetParent(goldBoxTr, false);
                        var iconRt = iconObj.AddComponent<RectTransform>();
                        iconRt.anchorMin = new Vector2(0, 0.5f);
                        iconRt.anchorMax = new Vector2(0, 0.5f);
                        iconRt.pivot = new Vector2(0, 0.5f);
                        iconRt.anchoredPosition = new Vector2(10f, 0f);
                        iconRt.sizeDelta = new Vector2(24f, 24f);
                        var iconImg = iconObj.AddComponent<Image>();
                        iconImg.sprite = UISpriteLoader.GetIconGold();
                        iconImg.preserveAspect = true;
                        iconImg.raycastTarget = false;
                    }
                }

                if (GoldText != null)
                {
                    GoldText.fontSize = 20f;
                    GoldText.fontStyle = FontStyles.Bold;
                    GoldText.color = new Color(1f, 0.95f, 0.65f);
                    var gTxtRt = GoldText.rectTransform;
                    gTxtRt.anchoredPosition = new Vector2(12f, 0f);
                }

                // Căn chỉnh FarmStatusText ở đáy bảng không bị đè chữ
                if (FarmStatusText != null)
                {
                    var rt = FarmStatusText.rectTransform;
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -118f);
                    rt.sizeDelta = new Vector2(290f, 26f);
                    FarmStatusText.fontSize = 12.5f;
                    FarmStatusText.fontStyle = FontStyles.Bold;
                    FarmStatusText.color = new Color(0.32f, 0.16f, 0.05f);
                }
            }

            // 2. Cấu hình thanh năng lượng (Energy Bar)
            Transform energyRootTr = transform.Find("Stardew_Energy_Bar");
            if (energyRootTr != null)
            {
                var eImg = energyRootTr.GetComponent<Image>();
                if (eImg != null)
                {
                    eImg.sprite = UISpriteLoader.GetFrameWoodPanel();
                    eImg.type = Image.Type.Sliced;
                    eImg.color = Color.white;
                }
                var eOutline = energyRootTr.GetComponent<Outline>();
                if (eOutline != null) eOutline.enabled = false;

                Transform badgeTr = energyRootTr.Find("E_Badge");
                if (badgeTr != null)
                {
                    var bImg = badgeTr.GetComponent<Image>();
                    if (bImg != null)
                    {
                        bImg.sprite = UISpriteLoader.GetIconEnergyBolt();
                        bImg.color = Color.white;
                    }
                    var bOutline = badgeTr.GetComponent<Outline>();
                    if (bOutline != null) bOutline.enabled = false;
                    var bText = badgeTr.GetComponentInChildren<TextMeshProUGUI>();
                    if (bText != null) bText.enabled = false; // Icon đã có biểu tượng sấm sét
                }
            }

            // 3. Cấu hình Joystick
            Transform joyBgTr = transform.Find("Mobile_Joystick_Background");
            if (joyBgTr != null)
            {
                var bgImg = joyBgTr.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = UISpriteLoader.GetJoystickBase();
                    bgImg.color = Color.white;
                }
                Transform handleTr = joyBgTr.Find("Mobile_Joystick_Handle");
                if (handleTr != null)
                {
                    var handleImg = handleTr.GetComponent<Image>();
                    if (handleImg != null)
                    {
                        handleImg.sprite = UISpriteLoader.GetJoystickKnob();
                        handleImg.color = Color.white;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (MobileGameController.Instance != null)
            {
                MobileGameController.Instance.OnStateUpdated -= RefreshHUD;
            }
        }

        private void Update()
        {
            UpdateEnergyBar();
        }

        public void RefreshHUD()
        {
            if (MobileGameController.Instance == null || MobileGameController.Instance.Engine == null) return;

            var engine = MobileGameController.Instance.Engine;
            var clock = engine.Clock;
            var farm = engine.Farm;

            // 1. Đồng hồ phong cách Stardew Valley (AM/PM)
            if (TimeText != null)
            {
                int hour12 = clock.CurrentHour % 12;
                if (hour12 == 0) hour12 = 12;
                string ampm = clock.CurrentHour >= 12 ? "PM" : "AM";
                TimeText.text = $"{hour12:00}:{clock.CurrentMinute:00} <size=70%>{ampm}</size>";
            }

            // 2. Ngày, Chu kỳ và Thời tiết
            if (DayPartWeatherText != null)
            {
                string weatherName = engine.CurrentWeather switch
                {
                    WeatherType.QuangDang => "Nắng",
                    WeatherType.NhieuMay => "Nhiều Mây",
                    WeatherType.NangGat => "Nắng Gắt",
                    WeatherType.Mua => "Mưa",
                    WeatherType.GiongBao => "Giông Bão",
                    WeatherType.RetDam => "Rét Đậm",
                    _ => "Sương Mù"
                };

                string partName = clock.GetTimeOfDay() switch
                {
                    TimeOfDay.Sang => "Sáng",
                    TimeOfDay.Trua => "Trưa",
                    TimeOfDay.Chieu => "Chiều",
                    _ => "Tối"
                };

                DayPartWeatherText.text = $"Ngày {clock.CurrentDay} ({partName}) • {weatherName}";
            }

            // 3. Hộp Vàng (Gold Box)
            if (GoldText != null)
            {
                GoldText.text = $"{farm.Gold:N0}g";
            }

            // 4. Chỉ số môi trường nông trại
            if (FarmStatusText != null)
            {
                var living = engine.Pigs.FindAll(p => p.IsAlive);
                var cap = farm.CalculateCapacityReport(living.Count, 0);
                FarmStatusText.text = $"Heo: {living.Count}/{cap.EffectiveCapacity} | SC: {farm.HabitatIndex}% | ÁLSK: {farm.EventPressureIndex}";
            }
        }

        private void UpdateEnergyBar()
        {
            var engine = MobileGameController.Instance?.Engine;
            var stamina = engine?.Character?.Stamina;
            if (stamina == null) return;

            float ratio = Mathf.Clamp01(stamina.CurrentStamina / Mathf.Max(1f, stamina.MaxStamina));

            if (EnergyFillBar != null)
            {
                EnergyFillBar.fillAmount = ratio;

                // Gradient màu Stardew: Xanh lá (khỏe) -> Vàng (mệt) -> Đỏ (kiệt sức)
                if (ratio > 0.5f)
                {
                    EnergyFillBar.color = Color.Lerp(new Color(0.95f, 0.85f, 0.2f), new Color(0.3f, 0.82f, 0.35f), (ratio - 0.5f) * 2f);
                }
                else
                {
                    EnergyFillBar.color = Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.95f, 0.85f, 0.2f), ratio * 2f);
                }
            }

            if (EnergyText != null)
            {
                EnergyText.text = $"{stamina.CurrentStamina:0}/{stamina.MaxStamina:0}";
            }
        }
    }
}
