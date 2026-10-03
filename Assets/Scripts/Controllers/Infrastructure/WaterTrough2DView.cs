using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class WaterTrough2DView : MonoBehaviour
    {
        [Header("Water Capacity & Level")]
        public float MaxWaterLiters = 100f;
        public float CurrentWaterLiters = 100f;

        [Header("Visual Feedback")]
        public SpriteRenderer SpriteRenderer;
        public Color FullWaterColor = new Color(0.2f, 0.6f, 0.9f, 0.9f);
        public Color EmptyWaterColor = new Color(0.35f, 0.45f, 0.5f, 0.5f);

        public bool HasWater => CurrentWaterLiters > 0.5f;

        private void Awake()
        {
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            UpdateVisual();
        }

        public float DrinkWater(float desiredLiters)
        {
            float drunk = Mathf.Min(CurrentWaterLiters, desiredLiters);
            CurrentWaterLiters -= drunk;
            UpdateVisual();
            return drunk;
        }

        public void Refill(float amountLiters)
        {
            CurrentWaterLiters = Mathf.Min(MaxWaterLiters, CurrentWaterLiters + amountLiters);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (SpriteRenderer != null)
            {
                float ratio = CurrentWaterLiters / Mathf.Max(1f, MaxWaterLiters);
                SpriteRenderer.color = Color.Lerp(EmptyWaterColor, FullWaterColor, ratio);
            }
        }
    }
}
