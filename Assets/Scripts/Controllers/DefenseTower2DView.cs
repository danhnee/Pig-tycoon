using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class DefenseTower2DView : MonoBehaviour, IDamageable
    {
        public DefenseBuildingInstance BuildingModel { get; private set; }

        [Header("Health & Durability")]
        public float MaxHp = 500f;
        public float CurrentHp = 500f;
        public bool IsAlive => CurrentHp > 0f;
        public Vector2 Position => transform.position;

        [Header("2D Visual Elements")]
        public SpriteRenderer BaseSprite;
        public SpriteRenderer TurretSprite;
        public Transform MuzzlePoint;

        [Header("Damage Visuals")]
        private GameObject smokeEffectObj;
        private SpriteRenderer smokeSr;

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
            Collider2D[] hits = (MonsterLayer.value != 0) 
                ? Physics2D.OverlapCircleAll(transform.position, RangeRadius, MonsterLayer)
                : Physics2D.OverlapCircleAll(transform.position, RangeRadius);

            float nearestDistSqr = float.MaxValue;
            Transform best = null;

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                if (col.GetComponent<PlayerCombatController>() != null) continue; // Không bắn người chơi

                var damageable = col.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    float dSqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
                    if (dSqr < nearestDistSqr)
                    {
                        nearestDistSqr = dSqr;
                        best = col.transform;
                    }
                }
            }
            currentTarget = best;
        }

        private void Shoot2D(Vector2 direction)
        {
            BuildingModel.AmmoCurrent -= BuildingModel.AmmoCostPerShot;
            shootCooldownTimer = BuildingModel.CooldownSeconds;

            if (currentTarget != null)
            {
                var dmg = currentTarget.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    dmg.TakeDamage(40f, direction, 2.5f);
                }
            }

            Debug.Log($"[Tháp 2D] {BuildingModel.Name} khai hỏa! Đạn còn: {BuildingModel.AmmoCurrent}/{BuildingModel.AmmoCapacity}");
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection = default, float knockbackForce = 0f)
        {
            if (!IsAlive) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            UpdateDamageVisual();

            if (!IsAlive && BuildingModel != null)
            {
                BuildingModel.IsBroken = true;
            }
        }

        public void Repair(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            if (BuildingModel != null && CurrentHp > 0f)
            {
                BuildingModel.IsBroken = false;
            }
            UpdateDamageVisual();
        }

        private void UpdateDamageVisual()
        {
            if (smokeEffectObj == null)
            {
                smokeEffectObj = new GameObject("TowerDamageSmoke");
                smokeEffectObj.transform.SetParent(transform, false);
                smokeEffectObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);

                smokeSr = smokeEffectObj.AddComponent<SpriteRenderer>();
                smokeSr.sprite = UISpriteLoader.GetFrameSlotNormal();
                smokeSr.sortingOrder = 220;
                smokeEffectObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                smokeEffectObj.SetActive(false);
            }

            if (!IsAlive)
            {
                // Tháp sụp đổ: Ngừng bắn, đổi màu xám đen
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                if (BaseSprite != null) BaseSprite.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
                if (TurretSprite != null) TurretSprite.gameObject.SetActive(false);
            }
            else if (CurrentHp < MaxHp * 0.5f)
            {
                // Tháp bị hư hại: Bốc khói đen và tia lửa
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.3f, 0.3f, 0.3f, 0.75f);
                if (BaseSprite != null) BaseSprite.color = new Color(0.85f, 0.5f, 0.35f, 1f);
                if (TurretSprite != null) TurretSprite.gameObject.SetActive(true);
            }
            else
            {
                // Tháp nguyên vẹn
                smokeEffectObj.SetActive(false);
                if (BaseSprite != null) BaseSprite.color = Color.white;
                if (TurretSprite != null) TurretSprite.gameObject.SetActive(true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, RangeRadius);
        }
    }
}

