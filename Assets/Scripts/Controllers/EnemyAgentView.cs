using System;
using System.Collections;
using UnityEngine;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EnemyAgentView : MonoBehaviour, IDamageable
    {
        public EnemyData Model { get; private set; }

        [Header("Visuals & Sprites")]
        public SpriteRenderer SpriteRenderer;
        public Sprite SpriteDown;
        public Sprite SpriteSide;
        public Sprite SpriteUp;
        public Sprite SpriteHurt;

        [Header("Feedback")]
        public Color NormalColor = Color.white;
        public Color FlashColor = new Color(1f, 0.25f, 0.25f, 1f);

        private Rigidbody2D rb;
        private Collider2D col;
        private Coroutine flashCoroutine;
        private float attackTimer = 0f;
        private Transform currentTarget;
        private GameObject healthBarRoot;
        private Transform healthBarFill;
        private bool isDying = false;

        public bool IsAlive => Model != null && Model.IsAlive && !isDying;
        public Vector2 Position => transform.position;

        public event Action<EnemyAgentView> OnDefeated;

        public void Bind(EnemyData model)
        {
            Model = model;
            LoadSpritesForType(model.TypeId);
            CreateFloatingHealthBar();
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.linearDamping = 6f;
            }
        }

        private void Start()
        {
            if (Model == null)
            {
                Bind(EnemyData.CreateKeBatHeo());
            }
        }

        private void LoadSpritesForType(EnemyTypeId type)
        {
            if (type == EnemyTypeId.ChuotGamKho)
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Sprites/Characters/enemy_pest_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/enemy_pest_side.png", 32);
                SpriteUp = SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Sprites/Characters/enemy_thief_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/enemy_thief_side.png", 32);
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/enemy_thief_up.png", 32);
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/enemy_thief_hurt.png", 32);
            }

            if (SpriteRenderer != null && SpriteDown != null)
            {
                SpriteRenderer.sprite = SpriteDown;
            }
        }

        private void CreateFloatingHealthBar()
        {
            if (healthBarRoot != null) return;

            healthBarRoot = new GameObject("Enemy_HealthBar");
            healthBarRoot.transform.SetParent(transform, false);
            healthBarRoot.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            var bg = new GameObject("Bg");
            bg.transform.SetParent(healthBarRoot.transform, false);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
            bgSr.sortingOrder = 300;
            bg.transform.localScale = new Vector3(0.6f, 0.08f, 1f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(healthBarRoot.transform, false);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            fillSr.color = new Color(0.95f, 0.25f, 0.25f, 0.95f);
            fillSr.sortingOrder = 301;
            fill.transform.localScale = new Vector3(0.58f, 0.06f, 1f);
            healthBarFill = fill.transform;

            healthBarRoot.SetActive(false); // Chỉ hiện khi nhận sát thương
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
            }

            // Cập nhật Y-Sorting Order cho 2D Top-Down
            if (SpriteRenderer != null)
            {
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
            }

            DecideTargetAndMove();
        }

        private void DecideTargetAndMove()
        {
            // 1. Kiểm tra người chơi có lại gần không (nếu người chơi trong 3.2m -> ưu tiên đánh người chơi)
            var player = PlayerCombatController.Instance;
            if (player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) < 3.2f)
            {
                currentTarget = player.transform;
            }
            else if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                // Chọn mục tiêu phù hợp với hành vi quái vật theo GDD
                if (Model.TypeId == EnemyTypeId.ChuotGamKho)
                {
                    var silo = FindAnyObjectByType<FeedSilo2DView>();
                    if (silo != null) currentTarget = silo.transform;
                }
                else if (Model.TypeId == EnemyTypeId.KeCaoPha)
                {
                    // Tìm hàng rào gần nhất còn nguyên
                    float minDist = float.MaxValue;
                    Fence2DView bestFence = null;
                    foreach (var f in FindObjectsByType<Fence2DView>())
                    {
                        if (f != null && !f.IsBroken)
                        {
                            float d = Vector2.Distance(transform.position, f.transform.position);
                            if (d < minDist)
                            {
                                minDist = d;
                                bestFence = f;
                            }
                        }
                    }
                    if (bestFence != null) currentTarget = bestFence.transform;
                }
                else // Kẻ Bắt Heo (KeBatHeo) / Đầu Lĩnh
                {
                    // Tìm heo gần nhất
                    float minDist = float.MaxValue;
                    PigAgentView bestPig = null;
                    foreach (var p in FindObjectsByType<PigAgentView>())
                    {
                        if (p != null && p.PigModel != null && p.PigModel.IsAlive)
                        {
                            float d = Vector2.Distance(transform.position, p.transform.position);
                            if (d < minDist)
                            {
                                minDist = d;
                                bestPig = p;
                            }
                        }
                    }
                    if (bestPig != null) currentTarget = bestPig.transform;
                    else if (player != null) currentTarget = player.transform;
                }
            }

            if (currentTarget == null) return;

            Vector2 myPos = transform.position;
            Vector2 targetPos = currentTarget.position;
            Vector2 toTarget = targetPos - myPos;
            float dist = toTarget.magnitude;

            // 2. Di chuyển về phía mục tiêu nếu ngoài AttackRange
            if (dist > Model.AttackRange)
            {
                Vector2 moveDir = toTarget.normalized;
                if (rb != null)
                {
                    rb.linearVelocity = moveDir * Model.MoveSpeed;
                }
                UpdateFacingSprite(moveDir);
            }
            else
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;

                // 3. Tấn công mục tiêu khi trong tầm
                if (attackTimer <= 0f)
                {
                    ExecuteAttackOnTarget();
                }
            }
        }

        private void UpdateFacingSprite(Vector2 moveDir)
        {
            if (SpriteRenderer == null) return;

            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y) * 0.8f)
            {
                if (SpriteSide != null) SpriteRenderer.sprite = SpriteSide;
                SpriteRenderer.flipX = moveDir.x < 0f;
            }
            else if (moveDir.y > 0f)
            {
                if (SpriteUp != null) SpriteRenderer.sprite = SpriteUp;
                SpriteRenderer.flipX = false;
            }
            else
            {
                if (SpriteDown != null) SpriteRenderer.sprite = SpriteDown;
                SpriteRenderer.flipX = false;
            }
        }

        private void ExecuteAttackOnTarget()
        {
            attackTimer = Model.AttackCooldown;

            // Đòn đánh trúng người chơi
            if (currentTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 knockDir = (currentTarget.position - transform.position).normalized;
                damageable.TakeDamage(Model.AttackDamage, knockDir, 1.8f);
                StartCoroutine(LungeVisualRoutine(knockDir));
                return;
            }

            // Đòn cào phá hàng rào
            var fence = currentTarget.GetComponent<Fence2DView>();
            if (fence != null)
            {
                fence.TakeDamage(Model.AttackDamage);
                StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                return;
            }

            // Đòn bắt heo
            var pig = currentTarget.GetComponent<PigAgentView>();
            if (pig != null && pig.PigModel != null)
            {
                pig.PigModel.Mood = Mathf.Max(0, pig.PigModel.Mood - 15);
                pig.PigModel.Bonding = Mathf.Max(0f, pig.PigModel.Bonding - 8f);
                pig.ShowThought("!", 2.0f);
                StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
            }
        }

        private IEnumerator LungeVisualRoutine(Vector2 dir)
        {
            Vector3 startPos = transform.position;
            Vector3 lungePos = startPos + (Vector3)(dir * 0.25f);
            float elapsed = 0f;

            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, lungePos, elapsed / 0.08f);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(lungePos, startPos, elapsed / 0.1f);
                yield return null;
            }
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!IsAlive) return;

            float actualDamage = Model.TakeDamage(amount);

            // 1. Cập nhật thanh máu trên đầu
            if (healthBarRoot != null)
            {
                healthBarRoot.SetActive(true);
                if (healthBarFill != null)
                {
                    float ratio = Mathf.Clamp01(Model.CurrentHp / Model.MaxHp);
                    healthBarFill.localScale = new Vector3(0.58f * ratio, 0.06f, 1f);
                }
            }

            // 2. Nhấp nháy màu đỏ & Sprite Hurt
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());

            // 3. Đẩy lùi (Knockback)
            if (rb != null && knockbackForce > 0f)
            {
                rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
            }

            // 4. Floating Damage Text
            SpawnDamagePopup(actualDamage);

            // 5. Nếu chết
            if (!Model.IsAlive)
            {
                Die();
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            if (SpriteRenderer != null)
            {
                if (SpriteHurt != null) SpriteRenderer.sprite = SpriteHurt;
                SpriteRenderer.color = FlashColor;
            }
            yield return new WaitForSeconds(0.12f);
            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = NormalColor;
                if (SpriteDown != null) SpriteRenderer.sprite = SpriteDown;
            }
        }

        private void SpawnDamagePopup(float damage)
        {
            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0.7f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            textMesh.text = $"-{Mathf.RoundToInt(damage)}";
            textMesh.fontSize = 4.8f;
            textMesh.color = new Color(1f, 0.85f, 0.15f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 700;

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private IEnumerator FloatingTextRoutine(GameObject obj, TextMeshPro textMesh)
        {
            float duration = 0.55f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                obj.transform.position = startPos + new Vector3(0f, progress * 0.6f, 0f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        private void Die()
        {
            if (isDying) return;
            isDying = true;

            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (col != null) col.enabled = false;

            // Thưởng Vàng và Kinh Nghiệm cho nông trại
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.Farm.AddGold(Model.GoldReward);
                engine.Farm.AddFarmExp(Model.ExpReward);
                engine.Character.Rage = Mathf.Min(100, engine.Character.Rage + 15); // Hạ quái tăng nộ
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
            }

            // Sinh thông báo rớt vàng
            SpawnRewardPopup($"+{Model.GoldReward}G");

            OnDefeated?.Invoke(this);

            StartCoroutine(DeathFadeRoutine());
        }

        private void SpawnRewardPopup(string text)
        {
            GameObject rewardObj = new GameObject("RewardPopup");
            rewardObj.transform.position = transform.position + new Vector3(0f, 0.9f, 0f);

            var textMesh = rewardObj.AddComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.fontSize = 5.2f;
            textMesh.color = new Color(0.2f, 1f, 0.4f, 1f); // Xanh lá ngọc
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;

            StartCoroutine(FloatingTextRoutine(rewardObj, textMesh));
        }

        private IEnumerator DeathFadeRoutine()
        {
            float elapsed = 0f;
            float duration = 0.4f;
            Vector3 origScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
                if (SpriteRenderer != null)
                {
                    SpriteRenderer.color = new Color(1f, 1f, 1f, 1f - t);
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
