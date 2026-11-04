using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Nối trạng thái thời tiết lên người (Core PlayerWeatherStatus, GDD 7.2) với cảnh đang chơi:
    /// - báo Core ngoài trời / trong nhà và khoảng cách tới mái che (ADR-0022: ngoài trời trừ khi đứng trong vùng tương tác
    ///   của một công trình CustomBuildingController hoặc ở cảnh nội thất; mái che = công trình gần nhất);
    /// - kẹp Thể lực dưới trần Cảm Nắng sau mỗi lần hồi;
    /// - áp sét đánh: choáng 3 giây (SpeedMultiplier = 0) và báo người chơi.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWeatherStatusDriver : MonoBehaviour
    {
        const float BuildingRefreshSeconds = 2f;

        CustomBuildingController[] buildings = new CustomBuildingController[0];
        float nextRefresh;
        string cachedScene = string.Empty;
        Coroutine stunRoutine;

        void Update()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null || engine.Character == null) return;

            RefreshBuildingsIfNeeded();
            Vector2 pos = transform.position;
            bool inBuilding = false;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                if (b == null || !b.isActiveAndEnabled) continue;
                if (b.IsPlayerInRange(pos)) inBuilding = true;
                float d = ((Vector2)b.transform.position - pos).sqrMagnitude;
                if (d < nearestSqr) nearestSqr = d;
            }
            bool interiorScene = cachedScene.IndexOf("Interior", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var status = engine.WeatherStatus;
            status.IsOutdoors = !inBuilding && !interiorScene;
            float roof = PlayerWeatherStatus.LightningRoofDistanceMeters;
            status.IsFarFromRoof = nearestSqr > roof * roof;

            while (status.TryTakeLightningStrike(out var strike)) ApplyLightning(strike);
            var feedback = PlayerInteractionController.Instance;
            while (status.TryTakeMessage(out var message))
            {
                if (feedback != null) feedback.ShowFeedback(message);
                else Debug.Log($"[WeatherStatus] {message}");
            }
        }

        void LateUpdate()
        {
            // Cảm Nắng (GDD 7.2/9.3): trần −10%. Hồi Thể lực chạy trong Update của hệ combat, nên kẹp sau cùng.
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null || engine.Character == null) return;
            engine.WeatherStatus.EnforceStaminaCap(engine.Character.Stamina);
        }

        void RefreshBuildingsIfNeeded()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (Time.unscaledTime < nextRefresh && scene == cachedScene) return;
            cachedScene = scene;
            nextRefresh = Time.unscaledTime + BuildingRefreshSeconds;
            buildings = FindObjectsByType<CustomBuildingController>(FindObjectsSortMode.None);
        }

        void ApplyLightning(LightningStrike strike)
        {
            if (strike.StunSeconds <= 0f) return; // Vòng Cách Điện: miễn choáng điện
            if (stunRoutine != null) StopCoroutine(stunRoutine);
            stunRoutine = StartCoroutine(Stun(strike.StunSeconds));
        }

        IEnumerator Stun(float seconds)
        {
            var mobile = GetComponent<PlayerMobileController>();
            if (mobile != null) mobile.SpeedMultiplier = 0f;
            yield return new WaitForSeconds(seconds);
            if (mobile != null && mobile.SpeedMultiplier == 0f) mobile.SpeedMultiplier = 1f;
            stunRoutine = null;
        }

        void OnDisable()
        {
            if (stunRoutine == null) return;
            StopCoroutine(stunRoutine);
            stunRoutine = null;
            var mobile = GetComponent<PlayerMobileController>();
            if (mobile != null && mobile.SpeedMultiplier == 0f) mobile.SpeedMultiplier = 1f;
        }
    }
}
