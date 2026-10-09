using System.Collections;
using UnityEngine;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Bù nhìn luyện công (Training Dummy) trong Đấu Trường Combat.
    /// Giúp người chơi thử nghiệm tầm đánh (Reach), góc chém (Arc), đòn chí mạng (Crits) và công thức kháng cự (Resilience).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrainingDummy2DView : MonoBehaviour, IDamageable
    {
        [Header("Dummy Configuration")]
        public string DummyName = "Bù Nhìn Rơm";
        public float MaxHp = 9999f;
        public float CurrentHp = 9999f;
        public int Resilience = 0; // Điểm kháng cự phòng thủ
        public bool AllowKnockback = true;

        [Header("Visuals")]
        public SpriteRenderer SpriteRenderer;
        public Color HitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

        private Vector3 initialPosition;
        private Coroutine flashRoutine;
        private Coroutine returnRoutine;
        private Rigidbody2D rb;

        public bool IsAlive => true; // Luôn bất tử để luyện tập không giới hạn
        public Vector2 Position => transform.position;

        private void Awake()
        {
            initialPosition = transform.position;
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.linearDamping = 8f;
            }
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            // Áp dụng giảm trừ theo Resilience
            float mitigatedDamage = Mathf.Max(1f, amount - Resilience);
            CurrentHp = Mathf.Max(1f, CurrentHp - mitigatedDamage);

            // 1. Nhấp nháy màu đỏ báo hiệu trúng đòn
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());

            // 2. Độ nảy lùi và tự động hồi về vị trí gốc (Spring Back)
            if (AllowKnockback && rb != null && knockbackForce > 0f)
            {
                rb.AddForce(knockbackDirection.normalized * knockbackForce * 0.7f, ForceMode2D.Impulse);

                if (returnRoutine != null) StopCoroutine(returnRoutine);
                returnRoutine = StartCoroutine(SpringBackRoutine());
            }

            // 3. Hiển thị chữ sát thương nổi
            SpawnDamagePopup(mitigatedDamage);

            // 4. Báo cáo lên CombatArenaController nếu có
            if (CombatArenaController.Instance != null)
            {
                CombatArenaController.Instance.RecordHit(DummyName, mitigatedDamage);
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = HitFlashColor;
                yield return new WaitForSeconds(0.12f);
                SpriteRenderer.color = Color.white;
            }
        }

        private IEnumerator SpringBackRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            float elapsed = 0f;
            Vector3 fromPos = transform.position;

            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(fromPos, initialPosition, elapsed / 0.25f);
                yield return null;
            }
            transform.position = initialPosition;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        private void SpawnDamagePopup(float damage)
        {
            GameObject popupObj = new GameObject("DummyDamagePopup");
            popupObj.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), 0.85f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            textMesh.text = $"-{Mathf.RoundToInt(damage)}";
            textMesh.fontSize = 5f;
            textMesh.color = new Color(1f, 0.9f, 0.2f, 1f); // Vàng tươi
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private IEnumerator FloatingTextRoutine(GameObject obj, TextMeshPro textMesh)
        {
            float duration = 0.55f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                obj.transform.position = startPos + new Vector3(0f, progress * 0.6f, 0f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }
    }
}
