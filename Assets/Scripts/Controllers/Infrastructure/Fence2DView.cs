using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Fence2DView : MonoBehaviour
    {
        [Header("GDD v6.0 Specs: Hàng rào gỗ 300 HP")]
        public float MaxHp = 300f;
        public float CurrentHp = 300f;
        public bool IsBroken => CurrentHp <= 0f;

        [Header("Visuals")]
        public SpriteRenderer SpriteRenderer;
        public Color NormalColor = new Color(0.6f, 0.4f, 0.2f, 1f);
        public Color DamagedColor = new Color(0.85f, 0.35f, 0.2f, 0.9f);
        public Color BrokenColor = new Color(0.4f, 0.2f, 0.1f, 0.35f);

        private BoxCollider2D boxCollider;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            UpdateVisual();
        }

        public void TakeDamage(float amount)
        {
            if (IsBroken) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            UpdateVisual();

            if (IsBroken && boxCollider != null)
            {
                boxCollider.isTrigger = true; // Heo/quái có thể lọt qua khi rào vỡ
            }
        }

        public void Repair(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            if (boxCollider != null && CurrentHp > 0f)
            {
                boxCollider.isTrigger = false;
            }
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (SpriteRenderer == null) return;

            if (IsBroken)
            {
                SpriteRenderer.color = BrokenColor;
            }
            else if (CurrentHp < MaxHp * 0.5f)
            {
                SpriteRenderer.color = DamagedColor;
            }
            else
            {
                SpriteRenderer.color = NormalColor;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Nếu heo đang hoảng loạn đâm vào hàng rào
            var pigAgent = collision.gameObject.GetComponent<PigAgentView>();
            if (pigAgent != null && pigAgent.PigModel != null)
            {
                if (pigAgent.PigModel.MoodState == Core.MoodState.HoangLoan)
                {
                    TakeDamage(15f);
                    Debug.Log($"[Fence2D] Heo {pigAgent.PigModel.Name} húc rào! HP rào còn: {CurrentHp}/{MaxHp}");
                }
            }
        }
    }
}
