using System.Collections;
using UnityEngine;

namespace PigTycoon.Presentation
{
    public class CameraShake2D : MonoBehaviour
    {
        public static CameraShake2D Instance { get; private set; }

        private Vector3 originalPos;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Shake(float duration = 0.08f, float magnitude = 0.12f)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3 camOriginal = cam.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = (Random.value * 2f - 1f) * magnitude;
                float y = (Random.value * 2f - 1f) * magnitude;

                cam.transform.localPosition = new Vector3(camOriginal.x + x, camOriginal.y + y, camOriginal.z);
                yield return null;
            }

            cam.transform.localPosition = camOriginal;
            shakeCoroutine = null;
        }
    }
}
