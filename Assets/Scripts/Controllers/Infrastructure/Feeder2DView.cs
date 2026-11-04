using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>A feeder in the world. Its stock lives in Core (TroughCommands / FarmServicePoint.FeedKg), so filling it
    /// is what feeds the herd; the view only shows the level. Unbound (no farm map yet) it keeps a local level.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Feeder2DView : MonoBehaviour
    {
        [Header("Feeder Capacity & Level")]
        public float MaxFoodKg = TroughCommands.FeederCapacityKg;
        [SerializeField] float localFoodKg = TroughCommands.FeederCapacityKg;
        /// <summary>Core service point id; bound lazily to the nearest feeder point (or registered as a new one).</summary>
        public string PointId;

        [Header("Visual Feedback")]
        public SpriteRenderer SpriteRenderer;
        public Color FullColor = new Color(0.82f, 0.70f, 0.44f);
        public Color EmptyColor = new Color(0.4f, 0.35f, 0.3f, 0.6f);

        static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        FarmServicePoint Bound()
        {
            var e = Engine;
            if (e == null || e.PlayableMap == null) return null;
            var p = string.IsNullOrEmpty(PointId) ? null : TroughCommands.Point(e, PointId);
            if (p != null) return p;
            p = TroughCommands.Nearest(e, transform.position.x, transform.position.y, true, 1.0f);
            if (p == null)
            {
                PointId = TroughCommands.RegisterPoint(e, transform.position.x, transform.position.y, feed: true, water: false);
                p = TroughCommands.Point(e, PointId);
                if (p != null) p.FeedKg = Mathf.Clamp(localFoodKg, 0f, MaxFoodKg);
            }
            else PointId = p.Id;
            return p;
        }

        public float CurrentFoodKg
        {
            get { var p = Bound(); return p != null ? p.FeedKg : localFoodKg; }
            set { var p = Bound(); float v = Mathf.Clamp(value, 0f, MaxFoodKg); if (p != null) p.FeedKg = v; else localFoodKg = v; UpdateVisual(); }
        }

        public bool HasFood => CurrentFoodKg > 0.1f;

        private void Awake()
        {
            if (SpriteRenderer == null) SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private float nextVisual;
        private void Update()
        {
            if (Time.unscaledTime < nextVisual) return;
            nextVisual = Time.unscaledTime + 0.5f;
            UpdateVisual();
        }

        public float EatFood(float desiredKg)
        {
            float eaten = Mathf.Min(CurrentFoodKg, desiredKg);
            CurrentFoodKg -= eaten;
            return eaten;
        }

        /// <summary>Pour feed in; returns the kilograms that fit (the rest stays in the bag).</summary>
        public float Refill(float amountKg)
        {
            var e = Engine;
            float put;
            if (Bound() != null && e != null) put = TroughCommands.FillFeeder(e, PointId, amountKg);
            else { put = Mathf.Clamp(amountKg, 0f, MaxFoodKg - localFoodKg); localFoodKg += put; }
            if (put > 0f)
            {
                e?.SignalQuest(QuestSignal.FeedPoured);
                PlayerMobileController.Instance?.GetComponent<CharacterSpriteAnimator>()?.TriggerWork("feed");
            }
            UpdateVisual();
            return put;
        }

        /// <summary>The player took the feeder down: the Core point goes with it.</summary>
        public void Unregister()
        {
            var e = Engine;
            if (e != null && !string.IsNullOrEmpty(PointId)) TroughCommands.RemovePoint(e, PointId);
        }

        private void UpdateVisual()
        {
            if (SpriteRenderer == null) return;
            float ratio = CurrentFoodKg / Mathf.Max(1f, MaxFoodKg);
            SpriteRenderer.color = Color.Lerp(EmptyColor, FullColor, ratio);
        }
    }
}
