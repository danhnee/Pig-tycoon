using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class DefenseTower2DView : MonoBehaviour
    {
        public DefenseBuildingInstance BuildingModel { get; private set; }

        [Header("2D Visual Elements")]
        public SpriteRenderer BaseSprite;
        public SpriteRenderer TurretSprite;
        public Transform MuzzlePoint;

        [Header("Targeting & Range")]
        public float RangeRadius = 7.0f; // Tương đương 22m theo tỉ lệ game
        public LayerMask MonsterLayer;

        [Header("Bạch Vân Charge Effect")]
        public GameObject ChargingAura2D;

        private float shootCooldownTimer = 0f;
        private Transform currentTarget;

        public void Bind(DefenseBuildingInstance model)
        {
            BuildingModel = model;
        }

        private void Update()
        {
            if (BuildingModel == null || BuildingModel.IsBroken) return;

            // Xử lý nạp năng lượng của Bạch Vân (Cửu Tiêu Lôi Pháo)
            if (BuildingModel.IsBachVan && ChargingAura2D != null)
            {
                ChargingAura2D.SetActive(BuildingModel.CurrentCooldownRemaining > 0f);
            }

            // Đếm ngược cooldown
            if (shootCooldownTimer > 0f)
            {
                shootCooldownTimer -= Time.deltaTime;
            }

            // Tìm mục tiêu quái gần nhất trong bán kính 2D
            FindNearestTarget2D();

            if (currentTarget != null && TurretSprite != null)
            {
                // Xoay tháp theo hướng mục tiêu trong không gian 2D
                Vector2 dir = currentTarget.position - transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                TurretSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

                // Tự động bắn nếu còn đạn và hết cooldown (Công trình thường luôn tự động)
                if (!BuildingModel.IsBachVan && shootCooldownTimer <= 0f && BuildingModel.AmmoCurrent > 0)
                {
                    Shoot2D(dir.normalized);
                }
            }
        }

        private void FindNearestTarget2D()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, RangeRadius, MonsterLayer);
            float nearestDistSqr = float.MaxValue;
            Transform best = null;

            foreach (var col in hits)
            {
                float dSqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (dSqr < nearestDistSqr)
                {
                    nearestDistSqr = dSqr;
                    best = col.transform;
                }
            }
            currentTarget = best;
        }

        private void Shoot2D(Vector2 direction)
        {
            BuildingModel.AmmoCurrent -= BuildingModel.AmmoCostPerShot;
            shootCooldownTimer = BuildingModel.CooldownSeconds;

            Debug.Log($"[Tháp 2D] {BuildingModel.Name} khai hỏa! Đạn còn: {BuildingModel.AmmoCurrent}/{BuildingModel.AmmoCapacity}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, RangeRadius);
        }
    }
}
