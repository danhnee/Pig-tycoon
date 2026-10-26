using System.Collections;
using UnityEngine;

namespace PigTycoon.Presentation
{
    public class CameraShake2D : MonoBehaviour
    {
        public static CameraShake2D Instance { get; private set; }

        [Header("Global Shake Tuning (Êm dịu & Tinh tế)")]
        [Tooltip("Hệ số suy giảm biên độ rung toàn cục (mặc định 0.20 = giảm 80% lực rung)")]
        public float GlobalIntensity = 0.20f;

        [Tooltip("Biên độ rung tối đa (đơn vị thế giới, tối đa ~2-3 pixel để không gây hoa mắt)")]
        public float MaxMagnitude = 0.08f;

        [Tooltip("Thời lượng rung tối đa (giây)")]
        public float MaxDuration = 0.18f;

        public Vector3 CurrentOffset { get; private set; } = Vector3.zero;

        private Coroutine shakeCoroutine;
        private Camera targetCamera;
        private CameraFollow2D cameraFollow;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            targetCamera = GetComponent<Camera>() ?? Camera.main;
            cameraFollow = GetComponent<CameraFollow2D>() ?? (targetCamera != null ? targetCamera.GetComponent<CameraFollow2D>() : null);
        }

        private void OnDisable()
        {
            ResetOffset();
        }

        public void Shake(float duration = 0.06f, float magnitude = 0.08f)
        {
            // Tinh chỉnh lực rung nhẹ nhàng, êm ái
            float scaledDuration = Mathf.Clamp(duration * 0.5f, 0.03f, MaxDuration);
            float scaledMagnitude = Mathf.Clamp(magnitude * GlobalIntensity, 0.01f, MaxMagnitude);

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(scaledDuration, scaledMagnitude));
        }

        private void ResetOffset()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;
            if (targetCamera != null && cameraFollow == null && CurrentOffset != Vector3.zero)
            {
                targetCamera.transform.localPosition -= CurrentOffset;
            }
            CurrentOffset = Vector3.zero;
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;
            if (cameraFollow == null && targetCamera != null)
            {
                cameraFollow = targetCamera.GetComponent<CameraFollow2D>();
            }

            // Nếu không có CameraFollow2D thì hoàn trả offset cũ trước khi bắt đầu
            if (cameraFollow == null && targetCamera != null && CurrentOffset != Vector3.zero)
            {
                targetCamera.transform.localPosition -= CurrentOffset;
                CurrentOffset = Vector3.zero;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Suy giảm mượt theo hàm bậc 3 (Smooth cubic falloff)
                float damp = (1f - progress) * (1f - progress) * (1f - progress);
                float curMag = magnitude * damp;

                Vector2 randomUnit = Random.insideUnitCircle;
                Vector3 newOffset = new Vector3(randomUnit.x * curMag, randomUnit.y * curMag, 0f);

                if (cameraFollow == null && targetCamera != null)
                {
                    targetCamera.transform.localPosition = targetCamera.transform.localPosition - CurrentOffset + newOffset;
                }
                CurrentOffset = newOffset;

                yield return null;
            }

            if (cameraFollow == null && targetCamera != null && CurrentOffset != Vector3.zero)
            {
                targetCamera.transform.localPosition -= CurrentOffset;
            }
            CurrentOffset = Vector3.zero;
            shakeCoroutine = null;
        }
    }
}
