using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class DefenseTower2DView : MonoBehaviour, IDamageable
    {
        public DefenseBuildingInstance BuildingModel { get; private set; }

        [Header("Health & Durability")]
        public float MaxHp = 1500f;
        public float CurrentHp = 1500f;
        public bool IsAlive => CurrentHp > 0f && (BuildingModel == null || !BuildingModel.IsBroken);
        public Vector2 Position => transform.position;

        [Header("2D Visual Elements")]
        public SpriteRenderer BaseSprite;
        public SpriteRenderer TurretSprite;
        public Transform MuzzlePoint;

        [Header("Floating Info Bars")]
        private GameObject healthBarRoot;
        private Transform healthBarFill;
        private Transform ammoBarFill;

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
            if (model != null)
            {
                MaxHp = model.MaxHp;
                CurrentHp = model.CurrentHp;
                RangeRadius = model.AttackRange > 0f ? model.AttackRange : 7.0f;

                // Nếu là Thần Khí Bạch Vân Cửu Tiêu Lôi Pháo
                if (model.BlueprintId == "CuuTieuLoiPhao")
                {
                    if (BaseSprite != null)
                    {
                        BaseSprite.sprite = UISpriteLoader.GetCuuTieuLoiPhaoSprite();
                    }
                    if (TurretSprite != null)
                    {
                        TurretSprite.gameObject.SetActive(false); // Cột pháo sấm đứng yên
                    }
                }
            }

            EnsureFloatingBars();
            UpdateBarVisuals();
            UpdateDamageVisual();
        }

        private void Start()
        {
            EnsureFloatingBars();
            UpdateBarVisuals();
            UpdateDamageVisual();
        }

        private void Update()
        {
            if (BuildingModel == null || BuildingModel.IsBroken) return;

            // Xử lý nạp năng lượng của Bạch Vân (Cửu Tiêu Lôi Pháo)
            if (BuildingModel.IsBachVan)
            {
                if (BuildingModel.CurrentCooldownRemaining > 0f)
                {
                    BuildingModel.CurrentCooldownRemaining -= Time.deltaTime;
                }

                if (ChargingAura2D != null)
                {
                    ChargingAura2D.SetActive(BuildingModel.CurrentCooldownRemaining > 0f);
                }
                UpdateBarVisuals();
                return; // Bạch Vân không bắn đạn thường mà chờ kích hoạt thủ công
            }

            // Đếm ngược cooldown bắn thường
            if (shootCooldownTimer > 0f)
            {
                shootCooldownTimer -= Time.deltaTime;
            }

            // Tìm mục tiêu quái gần nhất trong bán kính 2D
            FindNearestTarget2D();

            if (currentTarget != null && TurretSprite != null)
            {
                // Xoay nòng tháp theo hướng mục tiêu
                Vector2 dir = currentTarget.position - transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                TurretSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

                // Tự động bắn nếu còn đạn và hết cooldown
                if (shootCooldownTimer <= 0f && BuildingModel.AmmoCurrent > 0)
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
                if (col.GetComponent<TrainingDummy2DView>() != null) continue; // Không bắn bù nhìn thao trường
                if (col.GetComponent<Fence2DView>() != null) continue; // Không bắn hàng rào

                // Chỉ nhắm quái vật còn sống
                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
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
            BuildingModel.AmmoCurrent = Mathf.Max(0, BuildingModel.AmmoCurrent - BuildingModel.AmmoCostPerShot);
            shootCooldownTimer = BuildingModel.CooldownSeconds;

            if (currentTarget != null)
            {
                var dmg = currentTarget.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    float damageAmount = BuildingModel.Damage > 0f ? BuildingModel.Damage : 40f;
                    dmg.TakeDamage(damageAmount, direction, 2.5f);
                }
            }

            UpdateBarVisuals();
            Debug.Log($"<color=cyan>[Tháp 2D]</color> {BuildingModel.Name} khai hỏa! Đạn còn: {BuildingModel.AmmoCurrent}/{BuildingModel.AmmoCapacity}");
        }

        /// <summary>
        /// Kích hoạt Thần Khí Bạch Vân Cửu Tiêu Lôi Pháo:
        /// Giáng sấm sét toàn trại, gây 250 damage diện rộng cho tất cả quái vật và làm choáng 3s!
        /// </summary>
        public void ExecuteBachVanMegaLightning()
        {
            Debug.Log($"<color=yellow><b>⚡ [BẠCH VÂN - CỬU TIÊU LÔI PHÁO]</b> SẤM SÉT THẦN KHÍ KHỞI ĐỘNG!</color>");

            // Camera rung chấn dữ dội
            CameraShake2D.Instance?.Shake(0.6f, 0.6f);

            var enemies = FindObjectsByType<EnemyAgentView>();
            int hitCount = 0;

            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    // Sinh VFX tia sét giáng xuống
                    SpawnLightningVFX(enemy.transform.position);

                    // Gây 250 sát thương và làm chậm/choáng
                    enemy.TakeDamage(250f, Vector2.up, 2.5f);
                    enemy.ApplySlow(0.2f, 3.5f);
                    hitCount++;
                }
            }

            Debug.Log($"<color=green>⚡ [Lôi Pháo]</color> Đã giáng sấm sét trúng {hitCount} kẻ địch trên toàn bản đồ!");
            UpdateBarVisuals();
        }

        private void SpawnLightningVFX(Vector3 targetPos)
        {
            var vfxObj = new GameObject("VFX_LightningStrike");
            vfxObj.transform.position = targetPos + new Vector3(0f, 1.0f, 0f);

            var sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetVfxLightningStrikeSprite();
            sr.sortingOrder = 350;
            vfxObj.transform.localScale = new Vector3(1.5f, 2.0f, 1f);

            StartCoroutine(FadeAndDestroyVFX(vfxObj, sr, 0.45f));
        }

        private IEnumerator FadeAndDestroyVFX(GameObject obj, SpriteRenderer sr, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (sr != null)
                {
                    float alpha = 1f - (elapsed / duration);
                    sr.color = new Color(1f, 1f, 1f, alpha);
                }
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        public void ReloadAmmo(int amount)
        {
            if (BuildingModel == null) return;
            int space = BuildingModel.AmmoCapacity - BuildingModel.AmmoCurrent;
            int add = Mathf.Min(space, amount);
            BuildingModel.AmmoCurrent += add;
            UpdateBarVisuals();
            Debug.Log($"<color=green>[Nạp Đạn]</color> Đã nạp {add} {BuildingModel.AmmoName}! Hiện có: {BuildingModel.AmmoCurrent}/{BuildingModel.AmmoCapacity}");
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection = default, float knockbackForce = 0f)
        {
            if (!IsAlive) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            if (BuildingModel != null)
            {
                BuildingModel.CurrentHp = (int)CurrentHp;
                if (CurrentHp <= 0f)
                {
                    BuildingModel.IsBroken = true;
                }
            }

            UpdateDamageVisual();
            UpdateBarVisuals();
        }

        public void Repair(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            if (BuildingModel != null)
            {
                BuildingModel.CurrentHp = (int)CurrentHp;
                if (CurrentHp > 0f)
                {
                    BuildingModel.IsBroken = false;
                }
            }
            UpdateDamageVisual();
            UpdateBarVisuals();
        }

        private void EnsureFloatingBars()
        {
            if (healthBarRoot != null) return;

            healthBarRoot = new GameObject("TowerFloatingBars");
            healthBarRoot.transform.SetParent(transform, false);
            healthBarRoot.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            // Nền đen
            var bgObj = new GameObject("BarBg");
            bgObj.transform.SetParent(healthBarRoot.transform, false);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            bgSr.sortingOrder = 250;
            bgObj.transform.localScale = new Vector3(1.2f, 0.16f, 1f);

            // Thanh Máu (Xanh lá)
            var hpFillObj = new GameObject("HpFill");
            hpFillObj.transform.SetParent(healthBarRoot.transform, false);
            hpFillObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            var hpSr = hpFillObj.AddComponent<SpriteRenderer>();
            hpSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            hpSr.color = new Color(0.2f, 0.9f, 0.3f, 0.95f);
            hpSr.sortingOrder = 251;
            healthBarFill = hpFillObj.transform;

            // Thanh Đạn (Vàng cam)
            var ammoFillObj = new GameObject("AmmoFill");
            ammoFillObj.transform.SetParent(healthBarRoot.transform, false);
            ammoFillObj.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            var ammoSr = ammoFillObj.AddComponent<SpriteRenderer>();
            ammoSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            ammoSr.color = new Color(0.95f, 0.75f, 0.2f, 0.95f);
            ammoSr.sortingOrder = 251;
            ammoBarFill = ammoFillObj.transform;
        }

        private void UpdateBarVisuals()
        {
            if (healthBarFill != null && MaxHp > 0f)
            {
                float hpRatio = Mathf.Clamp01(CurrentHp / MaxHp);
                healthBarFill.localScale = new Vector3(1.15f * hpRatio, 0.05f, 1f);
            }

            if (ammoBarFill != null && BuildingModel != null && BuildingModel.AmmoCapacity > 0)
            {
                float ammoRatio = Mathf.Clamp01((float)BuildingModel.AmmoCurrent / BuildingModel.AmmoCapacity);
                ammoBarFill.localScale = new Vector3(1.15f * ammoRatio, 0.05f, 1f);
            }
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
                smokeSr.sortingOrder = (BaseSprite != null) ? BaseSprite.sortingOrder + 1 : 220;
                smokeEffectObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                smokeEffectObj.SetActive(false);
            }

            if (!IsAlive)
            {
                // Tháp sụp đổ
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
                if (BaseSprite != null) BaseSprite.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
                if (TurretSprite != null) TurretSprite.gameObject.SetActive(false);
            }
            else if (CurrentHp < MaxHp * 0.5f)
            {
                // Tháp bị hư hại
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.4f, 0.4f, 0.4f, 0.75f);
                if (BaseSprite != null) BaseSprite.color = new Color(0.9f, 0.6f, 0.45f, 1f);
                if (TurretSprite != null && BuildingModel?.BlueprintId != "CuuTieuLoiPhao") TurretSprite.gameObject.SetActive(true);
            }
            else
            {
                // Tháp nguyên vẹn
                smokeEffectObj.SetActive(false);
                if (BaseSprite != null) BaseSprite.color = Color.white;
                if (TurretSprite != null && BuildingModel?.BlueprintId != "CuuTieuLoiPhao") TurretSprite.gameObject.SetActive(true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, RangeRadius);
        }
    }
}
