using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Quản lý Bụi Hoa / Cỏ Dại trên thảo nguyên:
    /// 1. Cho phép người chơi, đàn heo, thú cưng đi xuyên qua (Trigger Collider hoặc không cản trở vật lý).
    /// 2. Áp dụng Dynamic Y-Sorting tại gốc bụi hoa: khi thực thể đứng sau bụi hoa, bụi hoa có sortingOrder cao hơn
    ///    và tự nhiên che khuất phần chân / nửa thân dưới của nhân vật, tạo chiều sâu 2.5D chân thực.
    /// 3. Hiệu ứng rung nhẹ (Rustle) khi thực thể bước vào bụi hoa.
    /// </summary>
    public class FlowerBush2D : MonoBehaviour
    {
        [Header("Y-Sorting Settings")]
        [Tooltip("Độ lệch Y từ tâm bụi hoa xuống gốc tiếp đất. Mặc định -0.25f để hoa che được chân thực thể khi đi vào")]
        public float BaseYOffset = -0.25f;

        [Header("Rustle Effect")]
        public bool EnableRustle = true;
        public float RustleAmount = 6f;
        public float RustleDuration = 0.25f;

        private SpriteRenderer spriteRenderer;
        private Vector3 originalScale;
        private Quaternion originalRotation;
        private float rustleTimer = 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalScale = transform.localScale;
            originalRotation = transform.localRotation;

            // Đảm bảo Y-sorting chuẩn xác tại gốc tiếp đất
            ApplyYSorting();

            // Đảm bảo Collider nếu có thì phải là Trigger để cho phép đi xuyên qua 100%
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                // Thêm một trigger nhẹ để đón sự kiện đi xuyên qua tạo hiệu ứng rung cỏ
                var trigger = gameObject.AddComponent<CircleCollider2D>();
                trigger.radius = 0.5f;
                trigger.offset = new Vector2(0f, 0f);
                trigger.isTrigger = true;
            }
        }

        private void Start()
        {
            ApplyYSorting();
        }

        public void ApplyYSorting()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Y-Sort tại gốc chân hoa:
                // Nếu Y_entity > Y_bush_base -> Entity ở phía sau -> hoa có sortingOrder cao hơn -> che khuất chân/thân dưới
                // Nếu Y_entity < Y_bush_base -> Entity ở phía trước -> entity có sortingOrder cao hơn -> đứng trước hoa
                spriteRenderer.sortingOrder = Mathf.RoundToInt(-(transform.position.y + BaseYOffset) * 100);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!EnableRustle) return;

            // Kích hoạt rung lắc nhẹ khi người chơi, heo hoặc thú cưng bước qua
            if (other.CompareTag("Player") || other.GetComponent<PigAgentView>() != null || other.name.Contains("Player") || other.name.Contains("Pig"))
            {
                TriggerRustle();
            }
        }

        private void TriggerRustle()
        {
            rustleTimer = RustleDuration;
        }

        private void Update()
        {
            if (rustleTimer > 0f)
            {
                rustleTimer -= Time.deltaTime;
                float progress = rustleTimer / RustleDuration;
                float angle = Mathf.Sin(progress * Mathf.PI * 4f) * RustleAmount * progress;
                transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);

                if (rustleTimer <= 0f)
                {
                    transform.localRotation = originalRotation;
                    transform.localScale = originalScale;
                }
            }
        }
    }
}
