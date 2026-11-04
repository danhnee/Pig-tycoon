using UnityEngine;
using UnityEngine.Rendering.Universal;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class DayNight2DController : MonoBehaviour
    {
        [Header("2D Global Light (URP)")]
        public Light2D GlobalLight2D;

        [Header("Day Part Ambient Colors")]
        public Color DawnColor = new Color(1.0f, 0.90f, 0.80f, 1.0f);     // Sáng (06-10)
        public float DawnIntensity = 0.95f;

        public Color NoonColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);         // Trưa (10-14)
        public float NoonIntensity = 1.15f;

        public Color DuskColor = new Color(1.0f, 0.72f, 0.52f, 1.0f);     // Chiều (14-18)
        public float DuskIntensity = 0.85f;

        public Color NightColor = new Color(0.16f, 0.20f, 0.38f, 1.0f);    // Tối (18-06, -15% tầm nhìn)
        public float NightIntensity = 0.35f;

        [Header("Transition Settings")]
        public float BlendSpeed = 2.0f;

        [Header("Bí Nhân Green Lantern Light 2D")]
        public Light2D MysticLanternLight2D;

        private Color targetColor;
        private float targetIntensity;

        private void Start()
        {
            if (MobileGameController.Instance != null)
            {
                MobileGameController.Instance.OnStateUpdated += OnStateUpdated;
            }
            UpdateTargetAmbient();
        }

        private void OnDestroy()
        {
            if (MobileGameController.Instance != null)
            {
                MobileGameController.Instance.OnStateUpdated -= OnStateUpdated;
            }
        }

        private void Update()
        {
            if (GlobalLight2D == null) return;

            GlobalLight2D.color = Color.Lerp(GlobalLight2D.color, targetColor, Time.deltaTime * BlendSpeed);
            GlobalLight2D.intensity = Mathf.Lerp(GlobalLight2D.intensity, targetIntensity, Time.deltaTime * BlendSpeed);

            // Kiểm tra trạng thái Bí nhân (ngọn đèn lồng xanh trong đêm)
            if (MysticLanternLight2D != null && MobileGameController.Instance != null)
            {
                bool isMysticActive = MobileGameController.Instance.Engine.Economy.MysticMerchant.IsActive;
                MysticLanternLight2D.gameObject.SetActive(isMysticActive);
            }
        }

        private void OnStateUpdated()
        {
            UpdateTargetAmbient();
        }

        private void UpdateTargetAmbient()
        {
            if (MobileGameController.Instance == null || MobileGameController.Instance.Engine == null) return;

            var timeOfDay = MobileGameController.Instance.Engine.Clock.GetTimeOfDay();
            switch (timeOfDay)
            {
                case TimeOfDay.Sang:
                    targetColor = DawnColor;
                    targetIntensity = DawnIntensity;
                    break;
                case TimeOfDay.Trua:
                    targetColor = NoonColor;
                    targetIntensity = NoonIntensity;
                    break;
                case TimeOfDay.Chieu:
                    targetColor = DuskColor;
                    targetIntensity = DuskIntensity;
                    break;
                case TimeOfDay.Toi:
                    // GDD7: night reduces sight, but must preserve readability of actors and interactables.
                    targetColor = new Color(Mathf.Max(.48f, NightColor.r), Mathf.Max(.56f, NightColor.g), Mathf.Max(.76f, NightColor.b), 1);
                    targetIntensity = Mathf.Max(.72f, NightIntensity);
                    break;
            }

            // GDD 7.2: the weather tints the light on top of the time of day (moving parts: WeatherFx2D)
            var (tint, k) = WeatherLight(MobileGameController.Instance.Engine.CurrentWeather);
            targetColor = new Color(targetColor.r * tint.r, targetColor.g * tint.g, targetColor.b * tint.b, 1f);
            targetIntensity *= k;
        }

        public static (Color tint, float intensity) WeatherLight(WeatherType w) => w switch
        {
            WeatherType.NhieuMay => (new Color(0.93f, 0.94f, 0.97f), 0.92f),
            WeatherType.NangGat => (new Color(1.0f, 0.95f, 0.84f), 1.08f),
            WeatherType.Mua => (new Color(0.84f, 0.88f, 0.96f), 0.82f),
            WeatherType.GiongBao => (new Color(0.74f, 0.78f, 0.9f), 0.68f),
            WeatherType.SuongMu => (new Color(0.9f, 0.92f, 0.95f), 0.86f),
            WeatherType.RetDam => (new Color(0.86f, 0.92f, 1.0f), 0.9f),
            WeatherType.NomAm => (new Color(0.92f, 0.96f, 0.9f), 0.9f),
            WeatherType.GioKho => (new Color(1.0f, 0.94f, 0.82f), 1.0f),
            WeatherType.MuaBucXa => (new Color(0.82f, 0.96f, 0.8f), 0.8f),
            _ => (Color.white, 1f),
        };
    }
}
