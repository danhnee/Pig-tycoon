using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class FeedSilo2DView : MonoBehaviour
    {
        [Header("Silo Storage Specs")]
        public float MaxCapacityKg = 1500f;
        public float CurrentFeedKg = 600f;

        [Header("Visual Feedback")]
        public SpriteRenderer SpriteRenderer;

        public bool HasFeed => CurrentFeedKg >= 5f;

        private void Awake()
        {
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public float ScoopFeed(float desiredKg)
        {
            float taken = Mathf.Min(CurrentFeedKg, desiredKg);
            CurrentFeedKg -= taken;
            return taken;
        }

        public void AddFeed(float amountKg)
        {
            CurrentFeedKg = Mathf.Min(MaxCapacityKg, CurrentFeedKg + amountKg);
        }
    }
}
