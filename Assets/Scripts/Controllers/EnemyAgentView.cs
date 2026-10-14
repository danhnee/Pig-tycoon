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
        private float hitRecoveryTimer = 0f;

        [Header("Pig Snatching & Abduction State")]
        private PigAgentView capturedPig = null;
        private Vector2 escapeTargetPos = Vector2.zero;
        private GameObject activeTractorBeamObj = null;
        private float ufoAbductionTimer = 0f;

        public bool IsCarryingPig => capturedPig != null;
        public PigAgentView CarriedPig => capturedPig;

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

        public static int UfoFailureCount = 0;

        private void LoadSpritesForType(EnemyTypeId type)
        {
            if (type == EnemyTypeId.ChuotGamKho)
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_side.png", 32);
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_hurt.png", 32) ?? SpriteDown;
            }
            else if (type == EnemyTypeId.KeCaoPha)
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_side.png", 32);
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_up.png", 32);
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_hurt.png", 32);
            }
            else if (type == EnemyTypeId.UfoTrinhSat || type == EnemyTypeId.UfoThuHoach)
            {
                SpriteDown = UISpriteLoader.GetUfoScoutSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_scout_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_scout_right.png", 32) ?? SpriteDown;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_scout_up.png", 32) ?? SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else if (type == EnemyTypeId.AlienGrunt)
            {
                SpriteDown = UISpriteLoader.GetAlienGruntSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_side.png", 32) ?? SpriteDown;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_hurt.png", 32) ?? SpriteDown;
            }
            else if (type == EnemyTypeId.AlienElite)
            {
                SpriteDown = UISpriteLoader.GetAlienEliteSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_side.png", 32) ?? SpriteDown;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_hurt.png", 32) ?? SpriteDown;
            }
            else if (type == EnemyTypeId.BossAlienWarlord)
            {
                SpriteDown = UISpriteLoader.GetBossAlienWarlordSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side.png", 32) ?? SpriteDown;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_hurt.png", 32) ?? SpriteDown;
            }
            else if (type == EnemyTypeId.UfoMothership)
            {
                SpriteDown = UISpriteLoader.GetUfoMothershipSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_mothership.png", 32);
                SpriteSide = SpriteDown;
                SpriteUp = SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_side.png", 32);
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_up.png", 32);
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_hurt.png", 32);
            }

            if (SpriteRenderer != null && SpriteDown != null)
            {
                SpriteRenderer.sprite = SpriteDown;
            }

            AdjustColliderForType(type);
            ApplyScaleForType(type);
        }

        public void ApplyScaleForType(EnemyTypeId type)
        {
            switch (type)
            {
                case EnemyTypeId.AlienGrunt:
                    transform.localScale = new Vector3(1.55f, 1.55f, 1f);
                    break;
                case EnemyTypeId.AlienElite:
                    transform.localScale = new Vector3(1.95f, 1.95f, 1f);
                    break;
                case EnemyTypeId.BossAlienWarlord:
                    transform.localScale = new Vector3(2.85f, 2.85f, 1f);
                    break;
                case EnemyTypeId.UfoTrinhSat:
                case EnemyTypeId.UfoThuHoach:
                    transform.localScale = new Vector3(2.2f, 2.2f, 1f);
                    break;
                case EnemyTypeId.UfoMothership:
                    transform.localScale = new Vector3(4.2f, 4.2f, 1f);
                    break;
                case EnemyTypeId.KeBatHeo:
                    transform.localScale = new Vector3(1.35f, 1.35f, 1f);
                    break;
                case EnemyTypeId.KeCaoPha:
                    transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                    break;
                case EnemyTypeId.DauLinhTrom:
                    transform.localScale = new Vector3(2.0f, 2.0f, 1f);
                    break;
                case EnemyTypeId.ChuotGamKho:
                    transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                    break;
                default:
                    transform.localScale = new Vector3(1.35f, 1.35f, 1f);
                    break;
            }
        }

        private void AdjustColliderForType(EnemyTypeId type)
        {
            if (col == null) col = GetComponent<Collider2D>();

            if (col is CapsuleCollider2D capsule)
            {
                if (type == EnemyTypeId.ChuotGamKho)
                {
                    capsule.size = new Vector2(0.55f, 0.4f);
                    capsule.offset = new Vector2(0f, -0.2f);
                }
                else if (type == EnemyTypeId.UfoTrinhSat || type == EnemyTypeId.UfoThuHoach)
                {
                    capsule.size = new Vector2(1.6f, 0.85f);
                    capsule.offset = new Vector2(0f, 0f);
                }
                else if (type == EnemyTypeId.BossAlienWarlord)
                {
                    capsule.size = new Vector2(0.9f, 1.6f);
                    capsule.offset = new Vector2(0f, 0.05f);
                }
                else if (type == EnemyTypeId.AlienElite)
                {
                    capsule.size = new Vector2(0.75f, 1.55f);
                    capsule.offset = new Vector2(0f, 0.05f);
                }
                else if (type == EnemyTypeId.UfoMothership)
                {
                    capsule.size = new Vector2(3.6f, 1.8f);
                    capsule.offset = new Vector2(0f, 0f);
                }
                else
                {
                    capsule.size = new Vector2(0.65f, 1.45f);
                    capsule.offset = new Vector2(0f, 0.05f);
                }
            }
            else if (col is BoxCollider2D box)
            {
                if (type == EnemyTypeId.ChuotGamKho)
                {
                    box.size = new Vector2(0.55f, 0.4f);
                    box.offset = new Vector2(0f, -0.2f);
                }
                else if (type == EnemyTypeId.UfoMothership)
                {
                    box.size = new Vector2(3.6f, 1.8f);
                    box.offset = new Vector2(0f, 0f);
                }
                else
                {
                    box.size = new Vector2(0.65f, 1.45f);
                    box.offset = new Vector2(0f, 0.05f);
                }
            }
        }

        private void CreateFloatingHealthBar()
        {
            if (healthBarRoot != null) return;

            healthBarRoot = new GameObject("Enemy_HealthBar");
            healthBarRoot.transform.SetParent(transform, false);
            float barY = 0.98f;
            if (Model != null)
            {
                if (Model.TypeId == EnemyTypeId.ChuotGamKho) barY = 0.35f;
                else if (Model.TypeId == EnemyTypeId.UfoMothership) barY = 1.35f;
                else if (Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach) barY = 0.65f;
            }
            healthBarRoot.transform.localPosition = new Vector3(0f, barY, 0f);

            bool isBigBoss = Model != null && (Model.TypeId == EnemyTypeId.BossAlienWarlord || Model.TypeId == EnemyTypeId.UfoMothership);
            float bgWidth = isBigBoss ? 1.2f : 0.6f;
            float bgHeight = isBigBoss ? 0.12f : 0.08f;
            float fillWidth = isBigBoss ? 1.16f : 0.58f;
            float fillHeight = isBigBoss ? 0.10f : 0.06f;

            var bg = new GameObject("Bg");
            bg.transform.SetParent(healthBarRoot.transform, false);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
            bgSr.sortingOrder = 300;
            bg.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(healthBarRoot.transform, false);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            fillSr.color = new Color(0.95f, 0.25f, 0.25f, 0.95f);
            fillSr.sortingOrder = 301;
            fill.transform.localScale = new Vector3(fillWidth, fillHeight, 1f);
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

            if (hitRecoveryTimer > 0f)
            {
                hitRecoveryTimer -= Time.deltaTime;
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
            // A. KẺ TRỘM ĐANG THA HEO CHẠY TRỐN VỀ RÌA MAP
            if (capturedPig != null)
            {
                if (escapeTargetPos == Vector2.zero)
                {
                    escapeTargetPos = CalculateEscapeEdge(transform.position);
                }

                Vector2 toEscape = escapeTargetPos - (Vector2)transform.position;
                float distToEscape = toEscape.magnitude;

                // Nếu đã thoát ra rìa map -> Cướp heo thành công!
                if (distToEscape < 1.0f || Mathf.Abs(transform.position.x) >= 16.5f || Mathf.Abs(transform.position.y) >= 11.5f)
                {
                    capturedPig.OnPigStolenAndLost();
                    Destroy(capturedPig.gameObject);
                    capturedPig = null;
                    Destroy(gameObject);
                    return;
                }

                Vector2 escapeDir = toEscape.normalized;
                if (rb != null)
                {
                    rb.linearVelocity = escapeDir * (Model.MoveSpeed * 1.25f);
                }
                UpdateFacingSprite(escapeDir);
                return;
            }

            // B. UFO ĐANG CHIẾU TRACTOR BEAM HÚT HEO LÊN TRỜI
            if ((Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach) && activeTractorBeamObj != null)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                ufoAbductionTimer += Time.deltaTime;

                if (ufoAbductionTimer >= 3.5f)
                {
                    // Hoàn tất bắt cóc
                    if (capturedPig != null)
                    {
                        capturedPig.OnPigStolenAndLost();
                        Destroy(capturedPig.gameObject);
                        capturedPig = null;
                    }
                    if (activeTractorBeamObj != null) Destroy(activeTractorBeamObj);
                    PlayerInteractionController.Instance?.ShowFeedback("<color=#c084fc>UFO đã hút heo vào khoang lái và biến mất!</color>");
                    Destroy(gameObject);
                }
                return;
            }

            // C. CHỌN MỤC TIÊU TIÊU CHUẨN
            var player = PlayerCombatController.Instance;
            if (Model.TypeId == EnemyTypeId.BossAlienWarlord)
            {
                if (player != null && player.IsAlive) currentTarget = player.transform;
            }
            else if (Model.TypeId == EnemyTypeId.AlienGrunt || Model.TypeId == EnemyTypeId.AlienElite)
            {
                if (player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) < 8.0f)
                {
                    currentTarget = player.transform;
                }
                else if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
                {
                    float minDist = float.MaxValue;
                    PigAgentView bestPig = null;
                    foreach (var p in FindObjectsByType<PigAgentView>())
                    {
                        if (p != null && p.PigModel != null && p.PigModel.IsAlive)
                        {
                            float d = Vector2.Distance(transform.position, p.transform.position);
                            if (d < minDist) { minDist = d; bestPig = p; }
                        }
                    }
                    if (bestPig != null) currentTarget = bestPig.transform;
                    else if (player != null) currentTarget = player.transform;
                }
            }
            else if (player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) < 3.2f && Model.TypeId != EnemyTypeId.UfoTrinhSat && Model.TypeId != EnemyTypeId.UfoThuHoach && Model.TypeId != EnemyTypeId.UfoMothership)
            {
                currentTarget = player.transform;
            }
            else if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                if (Model.TypeId == EnemyTypeId.ChuotGamKho)
                {
                    var silo = FindAnyObjectByType<FeedSilo2DView>();
                    if (silo != null) currentTarget = silo.transform;
                }
                else if (Model.TypeId == EnemyTypeId.KeCaoPha)
                {
                    float minDist = float.MaxValue;
                    Fence2DView bestFence = null;
                    foreach (var f in FindObjectsByType<Fence2DView>())
                    {
                        if (f != null && !f.IsBroken)
                        {
                            float d = Vector2.Distance(transform.position, f.transform.position);
                            if (d < minDist) { minDist = d; bestFence = f; }
                        }
                    }
                    if (bestFence != null) currentTarget = bestFence.transform;
                }
                else // Kẻ Bắt Heo & UFO: Săn tìm heo tự do gần nhất
                {
                    float minDist = float.MaxValue;
                    PigAgentView bestPig = null;
                    foreach (var p in FindObjectsByType<PigAgentView>())
                    {
                        if (p != null && p.PigModel != null && p.PigModel.IsAlive && !p.IsCaptured)
                        {
                            float d = Vector2.Distance(transform.position, p.transform.position);
                            if (d < minDist) { minDist = d; bestPig = p; }
                        }
                    }
                    if (bestPig != null) currentTarget = bestPig.transform;
                    else if (player != null) currentTarget = player.transform;
                }
            }

            if (currentTarget == null) return;

            Vector2 myPos = transform.position;
            Vector2 targetPos = currentTarget.position;

            // Nếu là Tàu Mẹ ở rìa map: Tàu Mẹ neo đậu tại chỗ (rìa map), bắn đại pháo plasma
            if (Model.TypeId == EnemyTypeId.UfoMothership)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                float dToTarget = Vector2.Distance(myPos, targetPos);
                if (dToTarget <= Model.AttackRange && attackTimer <= 0f)
                {
                    ExecuteAttackOnTarget();
                }
                return;
            }

            // Nếu là UFO thì neo vị trí phía trên đầu heo 2.0m (vừa vặn với kích thước UFO to)
            if (Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach)
            {
                targetPos += new Vector2(0f, 2.0f);
            }

            Vector2 toTarget = targetPos - myPos;
            float dist = toTarget.magnitude;

            // Di chuyển về phía mục tiêu
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

            // 0. Tàu Mẹ tại rìa map phóng chùm đại pháo plasma cực lớn
            if (Model.TypeId == EnemyTypeId.UfoMothership)
            {
                Vector2 fireOrigin = (Vector2)transform.position + new Vector2(0f, -2.0f);
                Vector2 fireDir = ((Vector2)currentTarget.position - fireOrigin).normalized;
                var projObj = new GameObject("Mothership_PlasmaBolt");
                var sr = projObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetIconEnergyBolt() ?? UISpriteLoader.GetFrameActionButton();
                sr.color = new Color(0.9f, 0.2f, 1f, 1f);
                sr.sortingOrder = 550;
                projObj.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
                var proj = projObj.AddComponent<CombatProjectile2D>();
                proj.Initialize(fireOrigin, fireDir, damage: Model.AttackDamage, speed: 5.2f, isPlayer: false);
                return;
            }

            // 0.5. Alien Elite bắn tia plasma viễn chiến nếu mục tiêu ở xa (> 1.2m)
            if (Model.TypeId == EnemyTypeId.AlienElite && Vector2.Distance(transform.position, currentTarget.position) > 1.2f)
            {
                Vector2 fireOrigin = transform.position;
                Vector2 fireDir = ((Vector2)currentTarget.position - fireOrigin).normalized;
                var projObj = new GameObject("AlienElite_PlasmaShot");
                var sr = projObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetIconEnergyBolt() ?? UISpriteLoader.GetFrameActionButton();
                sr.color = new Color(0.3f, 0.85f, 1f, 1f);
                sr.sortingOrder = 550;
                projObj.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                var proj = projObj.AddComponent<CombatProjectile2D>();
                proj.Initialize(fireOrigin, fireDir, damage: Model.AttackDamage, speed: 6.2f, isPlayer: false);
                return;
            }

            // 1. UFO bắt đầu chiếu Tractor Beam hút heo
            if (Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && !targetPig.IsCaptured && activeTractorBeamObj == null)
                {
                    capturedPig = targetPig;
                    capturedPig.AbductByUfo(this.transform);

                    activeTractorBeamObj = new GameObject("Ufo_TractorBeam");
                    activeTractorBeamObj.transform.position = transform.position + new Vector3(0f, -0.6f, 0f);
                    activeTractorBeamObj.transform.SetParent(transform, true);
                    var sr = activeTractorBeamObj.AddComponent<SpriteRenderer>();
                    sr.sprite = UISpriteLoader.GetTractorBeamVFX();
                    sr.sortingOrder = 280;
                    sr.color = new Color(0.3f, 0.9f, 1f, 0.85f);

                    ufoAbductionTimer = 0f;
                    PlayerInteractionController.Instance?.ShowFeedback("<color=#c084fc>CẢNH BÁO: UFO đang chiếu Tractor Beam hút heo! Hãy tấn công UFO để cứu heo!</color>");
                    return;
                }
            }

            // 2. Kẻ Bắt Heo tóm heo và đổi hướng tẩu thoát
            if (Model.TypeId == EnemyTypeId.KeBatHeo || Model.TypeId == EnemyTypeId.DauLinhTrom)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && !targetPig.IsCaptured && capturedPig == null)
                {
                    capturedPig = targetPig;
                    capturedPig.CaptureByThief(this.transform);
                    escapeTargetPos = CalculateEscapeEdge(transform.position);

                    PlayerInteractionController.Instance?.ShowFeedback("<color=#ef4444>BÁO ĐỘNG: Kẻ trộm đã tóm được Heo! Hãy đuổi theo tấn công hắn để cứu heo!</color>");
                    StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                    return;
                }
            }

            // 3. Đòn đánh trúng người chơi
            if (currentTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 knockDir = (currentTarget.position - transform.position).normalized;
                damageable.TakeDamage(Model.AttackDamage, knockDir, 1.8f);
                StartCoroutine(LungeVisualRoutine(knockDir));
                return;
            }

            // 4. Đòn cào phá hàng rào
            var fence = currentTarget.GetComponent<Fence2DView>();
            if (fence != null)
            {
                fence.TakeDamage(Model.AttackDamage);
                StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                return;
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
            if (hitRecoveryTimer > 0f) return; // Bảo vệ miễn nhiễm va chạm tức thời (0.15s)
            hitRecoveryTimer = 0.15f;

            // Đang cướp heo hoặc đang chiếu tia bắt cóc mà bị đánh trúng -> Đánh rơi heo ra!
            if (capturedPig != null || activeTractorBeamObj != null)
            {
                ReleaseCapturedPig(rescuedByPlayer: true);
            }

            float actualDamage = Model.TakeDamage(amount);

            // 1. Cập nhật thanh máu trên đầu
            if (healthBarRoot != null)
            {
                healthBarRoot.SetActive(true);
                if (healthBarFill != null)
                {
                    float ratio = Mathf.Clamp01(Model.CurrentHp / Model.MaxHp);
                    bool isBigBoss = Model != null && (Model.TypeId == EnemyTypeId.BossAlienWarlord || Model.TypeId == EnemyTypeId.UfoMothership);
                    float maxFillWidth = isBigBoss ? 1.16f : 0.58f;
                    float fillHeight = isBigBoss ? 0.10f : 0.06f;
                    healthBarFill.localScale = new Vector3(maxFillWidth * ratio, fillHeight, 1f);
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

            // Ghi nhận sát thương vào Đấu trường nếu đang trong map Arena
            if (CombatArenaController.Instance != null)
            {
                CombatArenaController.Instance.RecordHit(Model.TypeId.ToString(), actualDamage);
            }

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

        private bool ufoFailureCounted = false;

        private void RegisterUfoFailure()
        {
            if (ufoFailureCounted) return;
            ufoFailureCounted = true;

            UfoFailureCount++;
            if (UfoFailureCount >= 2)
            {
                PlayerInteractionController.Instance?.ShowFeedback("<color=#c084fc>UFO BỊ ĐẨY LÙI NHIỀU LẦN! Tín hiệu khẩn cấp: Thả 3 lính ngoài hành tinh!</color>");
                EnemyWaveSpawner.Instance?.TriggerAlienDrop3(transform.position);
            }
            else
            {
                PlayerInteractionController.Instance?.ShowFeedback($"<color=#c084fc>UFO bị ngăn chặn ({UfoFailureCount}/2 lần)! Cẩn thận với đợt đổ bộ kế tiếp!</color>");
            }
        }

        private void Die()
        {
            if (isDying) return;
            isDying = true;

            if (Model != null && (Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach))
            {
                RegisterUfoFailure();
            }

            ReleaseCapturedPig(rescuedByPlayer: true);

            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (col != null) col.enabled = false;

            // Thưởng Kinh Nghiệm và Nộ Khí
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.Farm.AddFarmExp(Model.ExpReward);
                engine.Character.Rage = Mathf.Min(100, engine.Character.Rage + 15); // Hạ quái tăng nộ
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
            }

            // Sinh các vật phẩm rơi vật lý (Loot Drops) theo chủng loại quái
            var droppedItems = LootDatabase.RollLoot(Model.TypeId);
            foreach (var lootItem in droppedItems)
            {
                LootDrop2DView.Spawn(lootItem, transform.position);
            }

            OnDefeated?.Invoke(this);

            StartCoroutine(DeathFadeRoutine());
        }

        private void ReleaseCapturedPig(bool rescuedByPlayer)
        {
            if (capturedPig != null)
            {
                capturedPig.ReleaseFromCapture(rescuedByPlayer);
                capturedPig = null;
            }
            if (activeTractorBeamObj != null)
            {
                Destroy(activeTractorBeamObj);
                activeTractorBeamObj = null;
            }
            ufoAbductionTimer = 0f;

            if (rescuedByPlayer && Model != null && (Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach))
            {
                RegisterUfoFailure();
            }
        }

        private Vector2 CalculateEscapeEdge(Vector2 currentPos)
        {
            float targetX = currentPos.x >= 0 ? 18f : -18f;
            float targetY = currentPos.y >= 0 ? 13f : -13f;
            return (Mathf.Abs(currentPos.x) > Mathf.Abs(currentPos.y))
                ? new Vector2(targetX, currentPos.y)
                : new Vector2(currentPos.x, targetY);
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
