using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>A water basin. Its stock lives in Core (FarmServicePoint.WaterL): the herd's water and cooling source.
    /// The view only shows the level. Unbound (no farm map yet) it keeps a local level.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterTrough2DView : MonoBehaviour
    {
        [Header("Basin Capacity & Level")]
        public float MaxWaterLiters = TroughCommands.BasinCapacityL;
        [SerializeField] float localWaterL = TroughCommands.BasinCapacityL;
        /// <summary>Core service point id; bound lazily to the nearest basin point (or registered as a new one).</summary>
        public string PointId;

        [Header("Visual Feedback")]
        public SpriteRenderer SpriteRenderer;
        public Color FullWaterColor = new Color(0.2f, 0.6f, 0.9f, 0.9f);
        public Color EmptyWaterColor = new Color(0.35f, 0.45f, 0.5f, 0.5f);

        static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        FarmServicePoint Bound()
        {
            var e = Engine;
            if (e == null || e.PlayableMap == null) return null;
            var p = string.IsNullOrEmpty(PointId) ? null : TroughCommands.Point(e, PointId);
            if (p != null) return p;
            p = TroughCommands.Nearest(e, transform.position.x, transform.position.y, false, 1.0f);
            if (p == null)
            {
                PointId = TroughCommands.RegisterPoint(e, transform.position.x, transform.position.y, feed: false, water: true);
                p = TroughCommands.Point(e, PointId);
                if (p != null) p.WaterL = Mathf.Clamp(localWaterL, 0f, MaxWaterLiters);
            }
            else PointId = p.Id;
            return p;
        }

        public float CurrentWaterLiters
        {
            get { var p = Bound(); return p != null ? p.WaterL : localWaterL; }
            set { var p = Bound(); float v = Mathf.Clamp(value, 0f, MaxWaterLiters); if (p != null) p.WaterL = v; else localWaterL = v; UpdateVisual(); }
        }

        public bool HasWater => CurrentWaterLiters > 0.5f;

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

        public float DrinkWater(float desiredLiters)
        {
            float drunk = Mathf.Min(CurrentWaterLiters, desiredLiters);
            CurrentWaterLiters -= drunk;
            return drunk;
        }

        /// <summary>Pour water in; returns the litres that fit.</summary>
        public float Refill(float amountKg)
        {
            var e = Engine;
            float put;
            if (Bound() != null && e != null) put = TroughCommands.FillBasin(e, PointId, amountKg);
            else { put = Mathf.Clamp(amountKg, 0f, MaxWaterLiters - localWaterL); localWaterL += put; }
            if (put > 0f)
            {
                e?.SignalQuest(QuestSignal.WaterRefilled); // Q01/Q02 "thay nước bồn" (was never sent: Ch1 could not finish)
                PlayerMobileController.Instance?.GetComponent<CharacterSpriteAnimator>()?.TriggerWork("water");
            }
            UpdateVisual();
            return put;
        }

        /// <summary>The player took the basin down: the Core point goes with it.</summary>
        public void Unregister()
        {
            var e = Engine;
            if (e != null && !string.IsNullOrEmpty(PointId)) TroughCommands.RemovePoint(e, PointId);
        }

        private void UpdateVisual()
        {
            if (SpriteRenderer == null) return;
            float ratio = CurrentWaterLiters / Mathf.Max(1f, MaxWaterLiters);
            SpriteRenderer.color = Color.Lerp(EmptyWaterColor, FullWaterColor, ratio);
        }
    }
}
