using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class Feeder2DView : MonoBehaviour
    {
        [Header("Feeder Capacity & Level")]
        public float MaxFoodKg = 50f;
        public float CurrentFoodKg = 50f;

        [Header("Visual Feedback")]
        public SpriteRenderer SpriteRenderer;
        public Color FullColor = new Color(0.82f, 0.70f, 0.44f);
        public Color EmptyColor = new Color(0.4f, 0.35f, 0.3f, 0.6f);

        public bool HasFood => CurrentFoodKg > 0.1f;

        private void Awake()
        {
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            UpdateVisual();
        }

        public float EatFood(float desiredKg)
        {
            float eaten = Mathf.Min(CurrentFoodKg, desiredKg);
            CurrentFoodKg -= eaten;
            UpdateVisual();
            return eaten;
        }

        public void Refill(float amountKg)
        {
            CurrentFoodKg = Mathf.Min(MaxFoodKg, CurrentFoodKg + amountKg);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (SpriteRenderer != null)
            {
                float ratio = CurrentFoodKg / Mathf.Max(1f, MaxFoodKg);
                SpriteRenderer.color = Color.Lerp(EmptyColor, FullColor, ratio);
            }
        }
    }
}
