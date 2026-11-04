using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public partial class EnemyAgentView : MonoBehaviour, IDamageable, ISkillReactive
    {
        public EnemyData Model { get; private set; }

        [Header("Visuals & Sprites")]
        public SpriteRenderer SpriteRenderer;
        public Sprite SpriteDown;
        public Sprite SpriteSide;
        public Sprite SpriteLeft;
        public Sprite SpriteRight;
        public Sprite SpriteUp;
        public Sprite SpriteHurt;

        [Header("Walk Animation Frames (Authentic Sprites)")]
        public Sprite[] SideWalkFrames;
        public Sprite[] UpWalkFrames;
        public Sprite[] DownWalkFrames;
        public float WalkFPS = 6.0f;
        private float walkAnimTimer = 0f;

        [Header("Feedback")]
        public Color NormalColor = Color.white;
        public Color FlashColor = new Color(1f, 0.25f, 0.25f, 1f);

        private Rigidbody2D rb;
        private Collider2D col;
        private Coroutine flashCoroutine;
        private float attackTimer = 0f;
        private float slowMultiplier = 1f;
        private float slowExpiresAt;
        private float ActiveSlow => Time.time < slowExpiresAt ? slowMultiplier : 1f;

        public void ApplySlow(float multiplier, float duration)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) ||
                float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f) return;
            slowMultiplier = Mathf.Min(ActiveSlow, Mathf.Clamp01(multiplier));
            slowExpiresAt = Mathf.Max(slowExpiresAt, Time.time + duration);
        }

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
        /// <summary>Going for a pig right now (carrying one, or its current target is a pig): Tiết kiệm towers fire at it.</summary>
        public bool IsThreateningPig => capturedPig != null || (currentTarget != null && currentTarget.GetComponent<PigAgentView>() != null);

        // Ground monsters walk the outer lane to the farm's single breach before choosing a target (FarmApproachRoute).
        private readonly List<Vector2> approachPath = new List<Vector2>();
        public bool IsOnApproach => approachPath.Count > 0;
        public void SetApproachPath(IEnumerable<Vector2> points)
        {
            approachPath.Clear();
            if (Model != null && IsFlyingOrOccult(Model.TypeId)) return;
            approachPath.AddRange(points);
        }
        public PigAgentView CarriedPig => capturedPig;

        public bool IsAlive => Model != null && Model.IsAlive && !isDying;
        public Vector2 Position => transform.position;

        public event Action<EnemyAgentView> OnDefeated;
        /// <summary>Presentation hooks for EnemyMythAnimator (PixelMyth sprite clips).</summary>
        public event Action OnHitTaken;
        public event Action OnAttackStarted;
        public bool IsDying => isDying;

        public void Bind(EnemyData model)
        {
            Model = model;
            LoadSpritesForType(model.TypeId);
            CreateFloatingHealthBar();
            EnemyMythAnimator.AttachIfAvailable(this);
            // GDD 3.8: flying and occult monsters do not follow ground paths; a kinematic body passes over fences
            if (IsFlyingOrOccult(model.TypeId) && rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
            OnBoundBehaviours();
        }

        public static bool IsFlyingOrOccult(EnemyTypeId t) =>
            t == EnemyTypeId.UfoTrinhSat || t == EnemyTypeId.UfoThuHoach || t == EnemyTypeId.UfoPhaoHam ||
            t == EnemyTypeId.UfoMothership || t == EnemyTypeId.BongHuyen || t == EnemyTypeId.ThucTheSuong;

        // ground monsters that stop making progress next to a PixelMyth fence break through it (GDD 3.8)
        private Vector2 probePos;
        private float probeTimer;
        private Vector2 sidestepDir;
        private float sidestepTimer;

        // a thief or UFO that escapes with a pig is destroyed without dying: the wave must still learn it is gone
        private bool removalReported;
        private void OnDestroy()
        {
            OnRemovedBehaviours();
            if (removalReported || !gameObject.scene.isLoaded) return;   // scene unload is not a defeat
            removalReported = true;
            OnDefeated?.Invoke(this);
        }

        private static PixelFenceSegment NearestFenceSegment(Vector2 pos, float maxDist)
        {
            PixelFenceSegment best = null;
            float bestD = maxDist;
            foreach (var f in FindObjectsByType<PixelFenceSegment>())
            {
                if (f == null || f.IsBroken) continue;
                var c = f.GetComponent<Collider2D>();
                float d = c != null ? Vector2.Distance(pos, c.ClosestPoint(pos)) : Vector2.Distance(pos, f.transform.position);
                if (d < bestD) { bestD = d; best = f; }
            }
            return best;
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
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_scout_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_scout_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_scout_up.png", 32) ?? SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else if (type == EnemyTypeId.AlienGrunt)
            {
                SpriteDown = UISpriteLoader.GetAlienGruntSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_hurt.png", 32) ?? SpriteDown;
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_up_walk4.png", 32)
                };
            }
            else if (type == EnemyTypeId.AlienElite)
            {
                SpriteDown = UISpriteLoader.GetAlienEliteSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_hurt.png", 32) ?? SpriteDown;
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_up_walk4.png", 32)
                };
            }
            else if (type == EnemyTypeId.BossAlienWarlord)
            {
                SpriteDown = UISpriteLoader.GetBossAlienWarlordSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_hurt.png", 32) ?? SpriteDown;
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side_walk3.png", 32)
                };
            }
            else if (type == EnemyTypeId.UfoMothership)
            {
                SpriteDown = UISpriteLoader.GetUfoMothershipSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_mothership.png", 32);
                SpriteSide = SpriteDown;
                SpriteUp = SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else if (type == EnemyTypeId.BayNhay)
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Characters/Enemies_B/Enemy_BayNhay_Sheet.png", 32) ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_down.png", 32);
                SpriteSide = SpriteDown;
                SpriteUp = SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else if (type == EnemyTypeId.ThuBao)
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Characters/Enemies_B/Enemy_ThuBao_Sheet.png", 32) ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_down.png", 32);
                SpriteSide = SpriteDown;
                SpriteUp = SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else if (type == EnemyTypeId.KeMangDich)
            {
                SpriteDown = UISpriteLoader.LoadSprite("Art/Characters/Enemies_B/Enemy_KeMangDich_Sheet.png", 32) ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_down.png", 32);
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

            // Collider size phải chia cho localScale để bù lại việc Unity tự nhân collider theo transform scale.
            // Công thức: size_local = visual_pixel / PPU / localScale  (PPU = 32 cho sprite 64×64)
            // Dữ liệu đo bbox visual thực (đã loại vùng transparent):
            //   AlienGrunt:       20×53 px, scale 1.55  → (0.40, 1.07)
            //   AlienElite:       27×55 px, scale 1.95  → (0.43, 0.88)
            //   BossAlienWarlord: 33×58 px, scale 2.85  → (0.36, 0.64)
            //   UfoTrinhSat:      56×37 px, scale 2.2   → (0.795, 0.525)
            //   UfoMothership:    120×67 px (canvas 128, PPU 32), scale 4.2 → (0.893, 0.498)
            //   KeBatHeo:         34×54 px, scale 1.35  → (0.79, 1.25)
            //   KeCaoPha:         37×55 px, scale 1.5   → (0.77, 1.15)
            //   DauLinhTrom:      33×58 px, scale 2.0   → (0.52, 0.91)

            if (col is CapsuleCollider2D capsule)
            {
                if (type == EnemyTypeId.ChuotGamKho)
                {
                    capsule.direction = CapsuleDirection2D.Horizontal;
                    capsule.size = new Vector2(0.55f, 0.4f);
                    capsule.offset = new Vector2(0f, -0.2f);
                }
                else if (type == EnemyTypeId.AlienGrunt)
                {
                    capsule.direction = CapsuleDirection2D.Vertical;
                    capsule.size = new Vector2(0.40f, 1.07f);
                    capsule.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.AlienElite)
                {
                    capsule.direction = CapsuleDirection2D.Vertical;
                    capsule.size = new Vector2(0.43f, 0.88f);
                    capsule.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.BossAlienWarlord)
                {
                    capsule.direction = CapsuleDirection2D.Vertical;
                    capsule.size = new Vector2(0.36f, 0.64f);
                    capsule.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.UfoTrinhSat || type == EnemyTypeId.UfoThuHoach)
                {
                    capsule.direction = CapsuleDirection2D.Horizontal;
                    capsule.size = new Vector2(0.795f, 0.525f);
                    capsule.offset = new Vector2(0f, 0f);
                }
                else if (type == EnemyTypeId.UfoMothership)
                {
                    capsule.direction = CapsuleDirection2D.Horizontal;
                    capsule.size = new Vector2(0.893f, 0.498f);
                    capsule.offset = new Vector2(0f, 0f);
                }
                else if (type == EnemyTypeId.KeBatHeo)
                {
                    capsule.direction = CapsuleDirection2D.Vertical;
                    capsule.size = new Vector2(0.79f, 1.25f);
                    capsule.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.KeCaoPha)
                {
                    capsule.direction = CapsuleDirection2D.Vertical;
                    capsule.size = new Vector2(0.77f, 1.15f);
                    capsule.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.DauLinhTrom)
                {
                    capsule.direction = CapsuleDirection2D.Vertical;
                    capsule.size = new Vector2(0.52f, 0.91f);
                    capsule.offset = new Vector2(0f, 0.02f);
                }
                else
                {
                    capsule.direction = CapsuleDirection2D.Vertical;
                    capsule.size = new Vector2(0.50f, 1.07f);
                    capsule.offset = new Vector2(0f, 0.02f);
                }
            }
            else if (col is BoxCollider2D box)
            {
                if (type == EnemyTypeId.ChuotGamKho)
                {
                    box.size = new Vector2(0.55f, 0.4f);
                    box.offset = new Vector2(0f, -0.2f);
                }
                else if (type == EnemyTypeId.AlienGrunt)
                {
                    box.size = new Vector2(0.40f, 1.07f);
                    box.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.AlienElite)
                {
                    box.size = new Vector2(0.43f, 0.88f);
                    box.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.BossAlienWarlord)
                {
                    box.size = new Vector2(0.36f, 0.64f);
                    box.offset = new Vector2(0f, 0.02f);
                }
                else if (type == EnemyTypeId.UfoTrinhSat || type == EnemyTypeId.UfoThuHoach)
                {
                    box.size = new Vector2(0.795f, 0.525f);
                    box.offset = new Vector2(0f, 0f);
                }
                else if (type == EnemyTypeId.UfoMothership)
                {
                    box.size = new Vector2(0.893f, 0.498f);
                    box.offset = new Vector2(0f, 0f);
                }
                else
                {
                    box.size = new Vector2(0.50f, 1.07f);
                    box.offset = new Vector2(0f, 0.02f);
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
                if (distToEscape < 1.0f || HasLeftMap(transform.position))
                {
                    capturedPig.OnPigStolenAndLost();
                    CountPigLost();
                    Destroy(capturedPig.gameObject);
                    capturedPig = null;
                    Destroy(gameObject);
                    return;
                }

                Vector2 escapeDir = toEscape.normalized;
                if (rb != null)
                {
                    rb.linearVelocity = escapeDir * (Model.MoveSpeed * 1.25f * MoveFactor);
                }
                UpdateFacingSprite(escapeDir);
                return;
            }

            // B. UFO ĐANG CHIẾU TRACTOR BEAM HÚT HEO LÊN TRỜI
            if ((Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach) && activeTractorBeamObj != null)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                ufoAbductionTimer += Time.deltaTime;

                if (ufoAbductionTimer >= HarvesterBeamSeconds)   // GDD 19.1: chùm kéo 4 giây
                {
                    // Hoàn tất bắt cóc
                    if (capturedPig != null)
                    {
                        capturedPig.OnPigStolenAndLost();
                        CountPigLost();
                        Destroy(capturedPig.gameObject);
                        capturedPig = null;
                    }
                    FinishExtraPigs(stolen: true, rescuedByPlayer: false);
                    if (activeTractorBeamObj != null) Destroy(activeTractorBeamObj);
                    PlayerInteractionController.Instance?.ShowFeedback("<color=#c084fc>UFO đã hút heo vào khoang lái và biến mất!</color>");
                    Destroy(gameObject);
                }
                return;
            }

            // GDD 19.1–19.2 per-monster timers (Boss summons, lightning, scout call, Mẫu Hạm movement)
            if (UpdateSpecialBehaviour()) return;

            // B2. ĐI THEO LỐI VÒNG NGOÀI VỀ CỬA PHÁ (trừ khi người chơi chặn ngay trước mặt)
            var player = PlayerCombatController.Instance;
            if (approachPath.Count > 0)
            {
                bool blockedByPlayer = player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) < 4.8f;
                if (!blockedByPlayer)
                {
                    Vector2 wp = approachPath[0];
                    Vector2 toWp = wp - (Vector2)transform.position;
                    if (toWp.magnitude < 1.6f) { approachPath.RemoveAt(0); return; }
                    Vector2 dirWp = toWp.normalized;
                    probeTimer += Time.deltaTime;
                    if (probeTimer >= 0.8f)
                    {
                        // stuck on a tree or rock on the lane: slide past it
                        if (((Vector2)transform.position - probePos).sqrMagnitude < 0.04f)
                        {
                            sidestepDir = Vector2.Perpendicular(dirWp) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                            sidestepTimer = 0.7f;
                        }
                        probePos = transform.position;
                        probeTimer = 0f;
                    }
                    if (sidestepTimer > 0f) { sidestepTimer -= Time.deltaTime; dirWp = (dirWp + sidestepDir * 1.4f).normalized; }
                    if (rb != null) rb.linearVelocity = dirWp * Model.MoveSpeed * MoveFactor;
                    UpdateFacingSprite(dirWp);
                    return;
                }
                approachPath.Clear();
            }

            // C. CHỌN MỤC TIÊU TIÊU CHUẨN (GDD 19.1 riêng từng quái trước)
            if (SelectSpecialTarget(player)) { }
            else if (Model.TypeId == EnemyTypeId.BossAlienWarlord)
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
            else if (player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) < 4.8f && Model.TypeId != EnemyTypeId.UfoTrinhSat && Model.TypeId != EnemyTypeId.UfoThuHoach && Model.TypeId != EnemyTypeId.UfoMothership)
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
                    else
                    {
                        var seg = NearestFenceSegment(transform.position, 400f);
                        if (seg != null) currentTarget = seg.transform;
                    }
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
            // a fence target is reached at its nearest point, not at the pivot of a 4 m segment
            var targetSeg = currentTarget.GetComponent<PixelFenceSegment>();
            if (targetSeg != null)
            {
                var segCol = targetSeg.GetComponent<Collider2D>();
                if (segCol != null) { toTarget = (Vector2)segCol.ClosestPoint(myPos) - myPos; dist = toTarget.magnitude; }
                if (targetSeg.IsBroken) { currentTarget = null; return; }
            }

            // Di chuyển về phía mục tiêu
            if (dist > Model.AttackRange)
            {
                probeTimer += Time.deltaTime;
                if (probeTimer >= 0.8f)
                {
                    if ((myPos - probePos).sqrMagnitude < 0.04f && !IsFlyingOrOccult(Model.TypeId) && targetSeg == null)
                    {
                        var blocking = NearestFenceSegment(myPos, 1.8f);
                        if (TryHandleBlocked(myPos, toTarget)) { }
                        else if (blocking != null) currentTarget = blocking.transform;
                        else
                        {
                            // a tree trunk or a wall corner: slide around it for a moment
                            sidestepDir = Vector2.Perpendicular(toTarget.normalized) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                            sidestepTimer = 0.7f;
                        }
                    }
                    probePos = myPos;
                    probeTimer = 0f;
                }
                Vector2 moveDir = toTarget.normalized;
                if (sidestepTimer > 0f)
                {
                    sidestepTimer -= Time.deltaTime;
                    moveDir = (moveDir + sidestepDir * 1.4f).normalized;
                }
                if (rb != null)
                {
                    rb.linearVelocity = moveDir * Model.MoveSpeed * MoveFactor;
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

            bool isMoving = moveDir.sqrMagnitude > 0.05f;
            if (isMoving)
            {
                walkAnimTimer += Time.deltaTime;
            }
            else
            {
                walkAnimTimer = 0f;
            }

            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y) * 0.8f)
            {
                Sprite sideSprite = SpriteSide;
                if (isMoving && SideWalkFrames != null && SideWalkFrames.Length > 0)
                {
                    int idx = (int)(walkAnimTimer * WalkFPS) % SideWalkFrames.Length;
                    if (SideWalkFrames[idx] != null) sideSprite = SideWalkFrames[idx];
                }

                if (moveDir.x < 0f)
                {
                    // Di chuyển sang trái: nếu đứng yên dùng SpriteLeft, nếu bước đi lật frame bước đi ngang
                    if (!isMoving && SpriteLeft != null)
                    {
                        SpriteRenderer.sprite = SpriteLeft;
                        SpriteRenderer.flipX = false;
                    }
                    else
                    {
                        SpriteRenderer.sprite = sideSprite ?? SpriteLeft;
                        SpriteRenderer.flipX = true; // SideWalk chuẩn quay mặt sang phải, nên lật khi đi sang trái
                    }
                }
                else
                {
                    // Di chuyển sang phải
                    if (!isMoving && SpriteRight != null)
                    {
                        SpriteRenderer.sprite = SpriteRight;
                        SpriteRenderer.flipX = false;
                    }
                    else
                    {
                        SpriteRenderer.sprite = sideSprite ?? SpriteRight;
                        SpriteRenderer.flipX = false;
                    }
                }
            }
            else if (moveDir.y > 0f)
            {
                Sprite upSprite = SpriteUp;
                if (isMoving && UpWalkFrames != null && UpWalkFrames.Length > 0)
                {
                    int idx = (int)(walkAnimTimer * WalkFPS) % UpWalkFrames.Length;
                    if (UpWalkFrames[idx] != null) upSprite = UpWalkFrames[idx];
                }
                if (upSprite != null) SpriteRenderer.sprite = upSprite;
                SpriteRenderer.flipX = false;
            }
            else
            {
                Sprite downSprite = SpriteDown;
                if (isMoving && DownWalkFrames != null && DownWalkFrames.Length > 0)
                {
                    int idx = (int)(walkAnimTimer * WalkFPS) % DownWalkFrames.Length;
                    if (DownWalkFrames[idx] != null) downSprite = DownWalkFrames[idx];
                }
                if (downSprite != null) SpriteRenderer.sprite = downSprite;
                SpriteRenderer.flipX = false;
            }
        }

        private void ExecuteAttackOnTarget()
        {
            attackTimer = Model.AttackCooldown;
            OnAttackStarted?.Invoke();

            if (currentTarget != null)
            {
                Vector2 dirToTarget = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
                UpdateFacingSprite(dirToTarget);
            }

            // 0a. Đòn vào công trình (Thiết Giáp, Pháo Hạm, Kẻ Cắt Dây, rào chặn đường…) — GDD 18/19.1
            if (TryExecuteSpecialAttack()) return;

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
            // GDD 19.1: UFO Trinh Sát only marks the group (it hovers); the harvester beams 1–3 pigs
            if (Model.TypeId == EnemyTypeId.UfoTrinhSat) return;
            if (Model.TypeId == EnemyTypeId.UfoThuHoach)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && !targetPig.IsCaptured && activeTractorBeamObj == null)
                {
                    capturedPig = targetPig;
                    capturedPig.AbductByUfo(this.transform);
                    if (!capturedPig.IsAbductedByUfo) { capturedPig = null; return; }
                    GrabExtraPigs(capturedPig);

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

            // 2. Kẻ Bắt Heo tóm heo và đổi hướng tẩu thoát (có kiểm tra Chó Ngao Canh chặn kẻ trộm)
            if (Model.TypeId == EnemyTypeId.KeBatHeo || Model.TypeId == EnemyTypeId.DauLinhTrom)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && !targetPig.IsCaptured && capturedPig == null)
                {
                    var engine = MobileGameController.Instance?.Engine;
                    if (engine != null && engine.PetsAndNpcs.HasPet(PetId.ChoNgaoCanh))
                    {
                        TakeDamage(45f, ((Vector2)transform.position - (Vector2)targetPig.transform.position).normalized, 3.0f);
                        PlayerInteractionController.Instance?.ShowFeedback("<color=#4ade80>Chó Ngao Canh đã lao ra cắn chặn kẻ trộm (-45 HP)!</color>");
                        return;
                    }

                    capturedPig = targetPig;
                    capturedPig.CaptureByThief(this.transform);
                    if (!targetPig.IsCaptured)
                    {
                        capturedPig = null;
                        return;
                    }
                    escapeTargetPos = CalculateEscapeEdge(transform.position);

                    PlayerInteractionController.Instance?.ShowFeedback("<color=#ef4444>BÁO ĐỘNG: Kẻ trộm đã tóm được Heo! Hãy đuổi theo tấn công hắn để cứu heo!</color>");
                    StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                    return;
                }
            }

            // 2.5. Hành vi riêng của 4 quái thuộc phạm vi Người B khi đánh mục tiêu KHÔNG phải người chơi
            bool targetIsPlayer = currentTarget != null && currentTarget.GetComponent<PlayerCombatController>() != null;

            if (Model.TypeId == EnemyTypeId.ChuotGamKho && !targetIsPlayer)
            {
                var engine = MobileGameController.Instance?.Engine;
                if (engine != null)
                {
                    var (_, ratLossMult, _, _, _) = engine.PetsAndNpcs.GetActivePetBonuses();
                    float stolenKg = 4.0f * ratLossMult; // Mèo Kho giảm 80% mất cám (còn 0.8 kg)
                    engine.Farm.FeedStockKg = Mathf.Max(0f, engine.Farm.FeedStockKg - stolenKg);
                    SpawnRewardPopup($"-{stolenKg:0.0}kg Cám");
                }
                StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                return;
            }

            if (Model.TypeId == EnemyTypeId.BayNhay)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && targetPig.PigModel != null)
                {
                    HurtPig(targetPig, 8f);
                    MonsterPigDamage.Morale(targetPig.PigModel, 10);   // GDD 19.1: mỗi lần cắn Tinh thần −10
                    targetPig.ShowThought("!! Bầy Nhảy!", 2.0f);
                    StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                    return;
                }
            }

            if (Model.TypeId == EnemyTypeId.ThuBao)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && targetPig.PigModel != null)
                {
                    HurtPig(targetPig, 18f);
                    targetPig.PigModel.Mood = Mathf.Max(0, targetPig.PigModel.Mood - 15);
                    if (targetPig.PigModel.GeneLine == GeneLineId.LoiMach && !targetPig.PigModel.ActiveDisease.HasValue)
                    {
                        targetPig.PigModel.ActiveDisease = DiseaseId.LoiTam;
                        targetPig.PigModel.SeizurePending = true;
                    }
                    targetPig.ShowThought("! Giật sét!", 2.2f);
                    StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                    return;
                }
            }

            if (Model.TypeId == EnemyTypeId.KeMangDich)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && targetPig.PigModel != null)
                {
                    if (!targetPig.PigModel.ActiveDisease.HasValue && targetPig.PigModel.ImmuneDays <= 0)
                    {
                        targetPig.PigModel.ActiveDisease = DiseaseId.DichTaHeo;
                    }
                    HurtPig(targetPig, 10f);
                    targetPig.ShowThought("! Nhiễm dịch!", 2.5f);
                    PlayerInteractionController.Instance?.ShowFeedback($"<color=#a3e635>Kẻ Mang Dịch đã phát tán mầm bệnh lên [{targetPig.PigModel.Name}]!</color>");
                    StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                    return;
                }
            }

            // 3. Đòn đánh trúng người chơi (có nhịp Telegraph cảnh báo đỏ/cam trước khi tung đòn để người chơi kịp Đỡ Chủ Động hoặc Đỡ Tự Động)
            if (currentTarget != null && currentTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                StartCoroutine(TelegraphedPlayerAttackRoutine(damageable));
                return;
            }

            // 4. Đòn cào phá hàng rào
            var pixelFence = currentTarget != null ? currentTarget.GetComponent<PixelFenceSegment>() : null;
            if (pixelFence != null)
            {
                pixelFence.TakeDamage(Model.AttackDamage);
                attackTimer = Model.AttackCooldown;
                StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                if (pixelFence.IsBroken) currentTarget = null;
                return;
            }
            var fence = currentTarget != null ? currentTarget.GetComponent<Fence2DView>() : null;
            if (fence != null)
            {
                fence.TakeDamage(Model.AttackDamage);
                StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                return;
            }
        }

        private double specialMoveReadyAt;

        private IEnumerator TelegraphedPlayerAttackRoutine(IDamageable playerTarget)
        {
            // Bộ đòn của loại quái (MonsterMovesets): đòn cơ bản báo 0,18 giây (nháy cam, né được);
            // đòn đặc biệt báo lâu hơn (nháy tím + tên đòn, không né được, chỉ phản kích được — phím Q).
            var player = playerTarget as PlayerCombatController;
            var move = MonsterMovesets.Pick(Model.TypeId, Time.time, ref specialMoveReadyAt, UnityEngine.Random.value);
            bool special = move.Kind == MonsterMoveKind.Special;
            if (SpriteRenderer != null)
                SpriteRenderer.color = special ? new Color(0.85f, 0.25f, 1f, 1f) : new Color(1f, 0.55f, 0.15f, 1f);
            if (special) SpawnRewardPopup($"!! {move.Name}");
            if (player != null) player.NotifyIncoming(this, move, Time.time + move.TelegraphSeconds);
            // Chỉ báo nhịp (CounterRhythmIndicator): tiến độ thật của đòn; vùng vàng = khung +45% của CounterResolver (≤ 0,08 giây trước khi chạm)
            float windStart = Time.time, T = Mathf.Max(0.05f, move.TelegraphSeconds);
            bool near = player != null && Vector2.Distance(player.transform.position, transform.position) < 14f;
            while (Time.time - windStart < T)
            {
                if (near && IsAlive)
                {
                    float prog = (Time.time - windStart) / T;
                    if (special)
                    {
                        float chance = player.PreviewCounterChance(this, out bool spam, out int streak);
                        CounterRhythmIndicator.ShowSpecialTelegraph(transform, move.Name, prog, chance, streak, spam,
                            1f - 0.25f / T, 1f - 0.08f / T);
                    }
                    else CounterRhythmIndicator.ShowBasicTelegraph(transform, move.Name, prog);
                }
                yield return null;
            }
            if (!special && near) CounterRhythmIndicator.HideTelegraph(transform);
            if (!IsAlive || currentTarget == null || IsStunned) yield break;   // Tấm Sốc Điện cancels the swing

            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = NormalColor;
            }

            float distNow = Vector2.Distance(transform.position, currentTarget.position);
            if (distNow <= Model.AttackRange + 0.85f)
            {
                Vector2 knockDir = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
                if (player != null) player.ResolveEnemyMove(this, move, Model.AttackDamage * move.DamageMultiplier, knockDir, special ? 3.5f : 1.8f);
                else DealMonsterHit(playerTarget, Model.AttackDamage, knockDir, 1.8f);
                StartCoroutine(LungeVisualRoutine(knockDir));
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

            // Khi bị người chơi đánh trúng, quái mặt đất lập tức chuyển mục tiêu quay sang đánh trả người chơi!
            if (PlayerCombatController.Instance != null &&
                PlayerCombatController.Instance.IsAlive &&
                Model != null &&
                !ForcedTargetActive &&
                Model.TypeId != EnemyTypeId.ThietGiapCongThanh &&   // GDD 19.1: bỏ mọi mục tiêu, chỉ đập cổng
                Model.TypeId != EnemyTypeId.UfoPhaoHam &&
                Model.TypeId != EnemyTypeId.UfoTrinhSat &&
                Model.TypeId != EnemyTypeId.UfoThuHoach &&
                Model.TypeId != EnemyTypeId.UfoMothership)
            {
                currentTarget = PlayerCombatController.Instance.transform;
            }

            float actualDamage = Model.TakeDamage(amount * IncomingDamageMultiplier);

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

            OnHitTaken?.Invoke();

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

            // Thưởng Kinh Nghiệm, Nộ Khí và xử lý đám mây ô nhiễm khi Kẻ Mang Dịch chết (GDD 12.2 / P3.11)
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.Farm.AddFarmExp(Model.ExpReward);
                CombatRewards.OnEnemyKilled(Model.TypeId);

                if (Model != null && Model.LeavesContaminationOnDeath)
                {
                    engine.Farm.HabitatIndex = Mathf.Max(0, engine.Farm.HabitatIndex - 3);
                    PlayerInteractionController.Instance?.ShowFeedback("<color=#a3e635>Kẻ Mang Dịch phát nổ để lại vệt bẩn dịch (-3 Sinh Cảnh)! Hãy dùng Xử Lý Môi Trường để khử trùng!</color>");
                }

                MobileGameController.Instance?.OnStateUpdated?.Invoke();
            }

            // Sinh các vật phẩm rơi vật lý (Loot Drops) theo chủng loại quái
            var droppedItems = LootDatabase.RollLoot(Model.TypeId);
            foreach (var lootItem in droppedItems)
            {
                LootDrop2DView.Spawn(lootItem, transform.position);
            }

            OnDiedBehaviours();
            removalReported = true;
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
            FinishExtraPigs(stolen: false, rescuedByPlayer: rescuedByPlayer);

            if (rescuedByPlayer && Model != null && (Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach))
            {
                RegisterUfoFailure();
            }
        }

        // PixelMyth maps run from (0,0) to (W,H); the legacy farm was centred on (0,0) at 36 x 26 m.
        private static bool HasLeftMap(Vector2 p)
        {
            var farm = PixelMythFarmGameplay.Instance;
            if (farm != null) return !farm.MapRect.Contains(p) || (p.x < farm.MapRect.xMin + 1.5f || p.y < farm.MapRect.yMin + 1.5f || p.x > farm.MapRect.xMax - 1.5f || p.y > farm.MapRect.yMax - 1.5f);
            return Mathf.Abs(p.x) >= 16.5f || Mathf.Abs(p.y) >= 11.5f;
        }

        private Vector2 CalculateEscapeEdge(Vector2 currentPos)
        {
            var farm = PixelMythFarmGameplay.Instance;
            if (farm != null)
            {
                // run back out the way the herd thieves come: the nearest map edge
                var r = farm.MapRect;
                float dl = currentPos.x - r.xMin, dr = r.xMax - currentPos.x, db = currentPos.y - r.yMin, dt = r.yMax - currentPos.y;
                float m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
                if (m == dl) return new Vector2(r.xMin, currentPos.y);
                if (m == dr) return new Vector2(r.xMax, currentPos.y);
                if (m == db) return new Vector2(currentPos.x, r.yMin);
                return new Vector2(currentPos.x, r.yMax);
            }
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
            // PixelMyth enemies play their death clip and lie still for a beat, then fade without shrinking
            var myth = GetComponent<EnemyMythAnimator>();
            if (myth != null)
            {
                yield return new WaitForSeconds(myth.DeathHoldSeconds);
                duration = 0.6f;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (myth == null) transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
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
