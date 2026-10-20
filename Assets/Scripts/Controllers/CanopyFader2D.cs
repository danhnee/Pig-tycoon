using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class CanopyFader2D : MonoBehaviour
    {
        [Header("Target Canopy")]
        [SerializeField] private Tilemap canopyTilemap;
        [SerializeField] private SpriteRenderer treeSprite;

        [Header("Fade Settings")]
        [Range(0.1f, 1f)] [SerializeField] private float fadedAlpha = 0.35f;
        [SerializeField] private float fadeDuration = 0.2f;

        private Coroutine activeFadeRoutine;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.name.Contains("Player") || other.name.Contains("An"))
            {
                StartFade(fadedAlpha);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.name.Contains("Player") || other.name.Contains("An"))
            {
                StartFade(1.0f);
            }
        }

        private void StartFade(float targetAlpha)
        {
            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            float elapsed = 0f;
            Color currentColor = canopyTilemap != null ? canopyTilemap.color : (treeSprite != null ? treeSprite.color : Color.white);
            float startAlpha = currentColor.a;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float currentA = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                SetAlpha(currentA);
                yield return null;
            }
            SetAlpha(targetAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (canopyTilemap != null)
            {
                Color c = canopyTilemap.color;
                c.a = alpha;
                canopyTilemap.color = c;
            }
            if (treeSprite != null)
            {
                Color c = treeSprite.color;
                c.a = alpha;
                treeSprite.color = c;
            }
        }
    }
}