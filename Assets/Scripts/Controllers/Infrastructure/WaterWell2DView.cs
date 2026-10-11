using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class WaterWell2DView : MonoBehaviour
    {
        [Header("Well Reservoir Specs")]
        public float MaxWaterLiters = 2000f;
        public float CurrentWaterLiters = 2000f;
        public float RefillRatePerSecond = 5f; // Giếng nước ngầm tự hồi phục

        [Header("Visual Feedback")]
        public SpriteRenderer SpriteRenderer;

        public bool HasWater => CurrentWaterLiters >= 10f;

        private void Awake()
        {
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            // Nước ngầm tự nhiên thẩm thấu hồi đầy dần
            if (CurrentWaterLiters < MaxWaterLiters)
            {
                CurrentWaterLiters = Mathf.Min(MaxWaterLiters, CurrentWaterLiters + RefillRatePerSecond * Time.deltaTime);
            }
        }

        public float DrawWater(float desiredLiters)
        {
            float drawn = Mathf.Min(CurrentWaterLiters, desiredLiters);
            CurrentWaterLiters -= drawn;
            return drawn;
        }

        public void RefillInstantly()
        {
            CurrentWaterLiters = MaxWaterLiters;
        }
    }
}
