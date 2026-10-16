using System.Collections;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Đại diện cho một vật thể đạn bay (gai nhọn, bọt độc, cầu lửa...) trong không gian Top-Down 2D.
    /// Có cơ chế Phản xạ / Chém tan (Projectile Deflection) chuẩn Stardew Valley khi trúng đòn cận chiến.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class CombatProjectile2D : MonoBehaviour
    {
        [Header("Projectile Properties")]
        public float Speed = 5.0f;
        public float Damage = 15f;
        public float Lifetime = 4.0f;
        public bool IsFromPlayer = false;
        public bool IsDeflected = false;

        private Vector2 moveDirection = Vector2.down;
        private CircleCollider2D col;
        private SpriteRenderer sr;
        private float lifeTimer = 0f;

        public Vector2 Direction => moveDirection;

        private void Awake()
        {
            col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        public void Initialize(Vector2 startPos, Vector2 direction, float damage, float speed = 5.0f, bool isPlayer = false)
        {
            transform.position = startPos;
            moveDirection = direction.normalized;
            Damage = damage;
            Speed = speed;
            IsFromPlayer = isPlayer;
            lifeTimer = 0f;

            // Xoay góc mũi đạn theo hướng bay
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(moveDirection * (Speed * Time.deltaTime));

            // Quét va chạm theo bán kính 2D
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.25f);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    // Đạn quái vật -> chỉ đánh trúng người chơi
                    if (!IsFromPlayer && hit.GetComponent<PlayerCombatController>() != null)
                    {
                        damageable.TakeDamage(Damage, moveDirection, 2.0f);
                        DestroyWithSpark();
                        return;
                    }
                    // Đạn đã bị phản xạ (Deflected) -> đánh trúng quái vật
                    else if (IsFromPlayer && hit.GetComponent<EnemyAgentView>() != null)
                    {
                        damageable.TakeDamage(Damage * 1.5f, moveDirection, 4.0f);
                        DestroyWithSpark();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Phản đòn đạn bay khi người chơi vung vũ khí trúng quỹ đạo đạn (Melee Deflection)
        /// </summary>
        public void Deflect(Vector2 newDirection)
        {
            if (IsDeflected) return;

            IsDeflected = true;
            IsFromPlayer = true;
            moveDirection = newDirection.normalized;
            Speed *= 1.4f; // Tăng tốc đạn khi phản kích
            lifeTimer = 0f; // Reset thời gian sống

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            if (sr != null)
            {
                sr.color = new Color(0.2f, 0.9f, 1f, 1f); // Đổi sang màu xanh ngọc phản đòn
            }
        }

        /// <summary>
        /// Phá hủy đạn kèm hiệu ứng lóe sáng
        /// </summary>
        public void DestroyWithSpark()
        {
            Destroy(gameObject);
        }
    }
}
