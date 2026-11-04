using System;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Bẫy Chông Gỗ (Spike Trap):
    /// Khi kẻ địch (EnemyAgentView) bước vào vùng va chạm ngầm:
    /// - Bung cọc chông nhọn gây 60 sát thương
    /// - Áp dụng hiệu ứng làm chậm 20% trong 2 giây
    /// - Tiêu hao 1 độ bền (tối đa 6 lần kích hoạt)
    /// - Khi hết độ bền, bẫy bị hỏng và cần được người chơi sửa chữa bằng búa gỗ
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SpikeTrap2DView : MonoBehaviour, IDamageable
    {
        public DefenseBuildingInstance BuildingModel { get; private set; }

        [Header("Trap Parameters")]
        public float Damage = SpikeTrapSpec.Damage;
        public float SlowMultiplier = SpikeTrapSpec.SlowMultiplier;
        public float SlowDuration = SpikeTrapSpec.SlowSeconds;
        public float SpringCooldown = 2.0f;

        [Header("Durability & Health")]
        public int Durability = SpikeTrapSpec.Charges;
        public int MaxDurability = SpikeTrapSpec.Charges;
        public float MaxHp = 120f;
        public float CurrentHp = 120f;
        public bool IsAlive => CurrentHp > 0f && Durability > 0;
        public Vector2 Position => transform.position;

        [Header("State")]
        public bool IsSprung = false;
        public bool IsBroken => Durability <= 0 || CurrentHp <= 0f;

        private SpriteRenderer spriteRenderer;
        private Collider2D triggerCol;
        private float cooldownTimer = 0f;

        public void Bind(DefenseBuildingInstance model)
        {
            BuildingModel = model;
            if (model != null)
            {
                Durability = model.Durability;
                MaxDurability = SpikeTrapSpec.Charges;
                CurrentHp = model.CurrentHp;
                MaxHp = model.MaxHp;
                Damage = SpikeTrapSpec.Damage;
                SlowMultiplier = SpikeTrapSpec.SlowMultiplier;
                SlowDuration = SpikeTrapSpec.SlowSeconds;
                if (!model.HasPosition)
                {
                    model.PosX = transform.position.x;
                    model.PosY = transform.position.y;
                    model.HasPosition = true;
                }
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            triggerCol = GetComponent<Collider2D>();
            if (triggerCol != null)
            {
                triggerCol.isTrigger = true;
            }

            UpdateVisualState();
        }

        private void Start()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) - 5;
            }
        }

        private void Update()
        {
            if (IsBroken) return;

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f && IsSprung)
                {
                    // Thu hồi chông về trạng thái rình mồi
                    IsSprung = false;
                    UpdateVisualState();
                }
            }
            else if (!IsSprung)
            {
                // Quét phát hiện quái vật giẫm lên bẫy
                CheckEnemiesOnTrap();
            }
        }

        private void CheckEnemiesOnTrap()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.8f);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    TriggerTrap(enemy);
                    break;
                }
            }
        }

        public void TriggerTrap(EnemyAgentView enemy)
        {
            if (IsBroken || IsSprung) return;

            IsSprung = true;
            cooldownTimer = SpringCooldown;

            // 1. Gây sát thương và hiệu ứng làm chậm cho quái
            if (enemy != null && enemy.IsAlive)
            {
                enemy.ApplyStructureHit(Damage);   // GDD 13.3 rule 9: full damage, the monster keeps its target
                enemy.ApplySlow(SlowMultiplier, SlowDuration);
                Debug.Log($"<color=orange>[Bẫy Chông]</color> Kích hoạt! Đâm trúng {enemy.name} gây {Damage} sát thương & làm chậm 20%!");
            }

            // 2. Tiêu hao độ bền
            Durability = Mathf.Max(0, Durability - 1);
            if (BuildingModel != null)
            {
                BuildingModel.Durability = Durability;
                BuildingModel.AmmoCurrent = Durability;
                BuildingModel.AmmoUsedThisWave += 1;
                if (Durability <= 0)
                {
                    BuildingModel.IsBroken = true;
                }
            }

            UpdateVisualState();
        }

        public void Repair(float amount = 10f)
        {
            Durability = MaxDurability;
            CurrentHp = MaxHp;
            IsSprung = false;
            cooldownTimer = 0f;

            if (BuildingModel != null)
            {
                BuildingModel.Durability = Durability;
                BuildingModel.AmmoCurrent = Durability;
                BuildingModel.CurrentHp = (int)CurrentHp;
                BuildingModel.IsBroken = false;
            }

            UpdateVisualState();
            Debug.Log($"<color=green>[Bẫy Chông]</color> Đã sửa chữa hoàn toàn bẫy chông! Độ bền: {Durability}/{MaxDurability}");
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection = default, float knockbackForce = 0f)
        {
            if (IsBroken) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            if (CurrentHp <= 0f)
            {
                Durability = 0;
                if (BuildingModel != null) BuildingModel.IsBroken = true;
            }
            if (BuildingModel != null) BuildingModel.CurrentHp = Mathf.RoundToInt(CurrentHp);
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (spriteRenderer == null) return;

            if (IsBroken)
            {
                spriteRenderer.sprite = UISpriteLoader.GetSpikeTrapSprungSprite();
                spriteRenderer.color = new Color(0.45f, 0.45f, 0.45f, 0.7f); // Gãy nát
            }
            else if (IsSprung)
            {
                spriteRenderer.sprite = UISpriteLoader.GetSpikeTrapSprungSprite();
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.sprite = UISpriteLoader.GetSpikeTrapIdleSprite();
                spriteRenderer.color = Color.white;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsBroken ? Color.red : (IsSprung ? Color.yellow : Color.green);
            Gizmos.DrawWireCube(transform.position, new Vector3(1.2f, 0.8f, 0.1f));
        }
    }
}
