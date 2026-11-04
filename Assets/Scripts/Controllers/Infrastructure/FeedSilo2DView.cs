using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class FeedSilo2DView : MonoBehaviour
    {
        [Header("Silo Storage Specs")]
        public float MaxCapacityKg = 1500f;
        [SerializeField] float localFeedKg = 600f;
        static PigTycoon.Core.GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
        /// <summary>The farm's feed stock in Core (Farm.FeedStockKg); the silo is its visible face.</summary>
        public float CurrentFeedKg
        {
            get { var e = Engine; return e != null ? e.Farm.FeedStockKg : localFeedKg; }
            set { var e = Engine; if (e != null) e.Farm.FeedStockKg = Mathf.Max(0f, value); else localFeedKg = Mathf.Max(0f, value); }
        }

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
