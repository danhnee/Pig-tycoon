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
            if (MobileGameController.Instance != null)
            {
                MobileGameController.Instance.OnStateUpdated += RefreshHUD;
            }
            RefreshHUD();
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
