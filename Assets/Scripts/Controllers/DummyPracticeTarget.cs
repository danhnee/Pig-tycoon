using System.Collections;
using UnityEngine;
using TMPro;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class DummyPracticeTarget : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        public float MaxHealth = 200f;
        public float CurrentHealth = 200f;

        [Header("Feedback")]
        public SpriteRenderer TargetRenderer;
        public Color FlashColor = new Color(1f, 0.3f, 0.3f, 1f); // Đỏ nhấp nháy

        private Rigidbody2D rb;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;

        public bool IsAlive => CurrentHealth > 0;
        public Vector2 Position => transform.position;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (TargetRenderer == null)
            {
                TargetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (TargetRenderer != null)
            {
                originalColor = TargetRenderer.color;
            }
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            // 1. Hit Flash
            if (TargetRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(HitFlashRoutine());
            }

            // 2. Knockback
            if (rb != null && knockbackForce > 0)
            {
                rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
            }
            else
            {
                transform.position += (Vector3)(knockbackDirection.normalized * (knockbackForce * 0.05f));
            }

            // 3. Floating Damage Popup
            SpawnDamagePopup(amount);

            // 4. Tự hồi phục nếu bị hạ gục (để người chơi tập đánh liên tục)
            if (CurrentHealth <= 0)
            {
                Invoke(nameof(ResetHealth), 1.5f);
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            TargetRenderer.color = FlashColor;
            yield return new WaitForSeconds(0.08f);
            TargetRenderer.color = originalColor;
        }

        private void SpawnDamagePopup(float damage)
        {
            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), 0.6f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            textMesh.text = $"-{Mathf.RoundToInt(damage)}";
            textMesh.fontSize = 4.5f;
            textMesh.color = new Color(1f, 0.9f, 0.2f, 1f); // Vàng cam nổi bật
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 600;

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private IEnumerator FloatingTextRoutine(GameObject obj, TextMeshPro textMesh)
        {
            float duration = 0.6f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                obj.transform.position = startPos + new Vector3(0f, progress * 0.5f, 0f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }

            if (obj != null) Destroy(obj);
        }

        private void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
