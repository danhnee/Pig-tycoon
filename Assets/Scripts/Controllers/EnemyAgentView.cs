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
        public Sprite SpriteLeft;
        public Sprite SpriteRight;
        public Sprite SpriteUp;
        public Sprite SpriteHurt;
        public Sprite SpriteAttack;
        public Sprite SpriteDie;

        [Header("Walk Animation Frames (Authentic Sprites)")]
        public Sprite[] SideWalkFrames;
        public Sprite[] UpWalkFrames;
        public Sprite[] DownWalkFrames;
        public float WalkFPS = 6.0f;
        private float walkAnimTimer = 0f;

        [Header("Combat & Death Animation Frames")]
        public Sprite[] AttackFrames;
        public Sprite[] DieFrames;

        [Header("Feedback")]
        public Color NormalColor = Color.white;
        public Color FlashColor = new Color(1f, 0.25f, 0.25f, 1f);

        private Rigidbody2D rb;
        private Collider2D col;
        private CapsuleCollider2D hurtboxCol;
        private Coroutine flashCoroutine;
        private float attackTimer = 0f;
        private Transform currentTarget;
        private GameObject healthBarRoot;
        private Transform healthBarFill;
        private SpriteRenderer healthBarBgSr;
        private SpriteRenderer healthBarFillSr;
        private bool isDying = false;
        private float hitRecoveryTimer = 0f;

        [Header("Pig Snatching & Abduction State")]
        private PigAgentView capturedPig = null;
        private Vector2 escapeTargetPos = Vector2.zero;
        private GameObject activeTractorBeamObj = null;
        private float ufoAbductionTimer = 0f;

        public bool IsCarryingPig => capturedPig != null;
        public PigAgentView CarriedPig => capturedPig;

        [Header("Bandit Leader (Đầu Lĩnh Trộm) Special State")]
        public float SpeedMultiplier = 1.0f;
        private float speedBuffDuration = 0f;
        private float commandAuraTimer = 0f;
        private bool hasCalledBackup = false;
        private bool hasDroppedSmokeBomb = false;

        [Header("Bandit Lord (Chúa Trộm Rìu Sắt) Boss State")]
        public bool IsEnraged => isEnraged;
        private bool isEnraged = false;
        private float bossWhirlwindCooldown = 4.0f;
        private float bossGroundSlamCooldown = 8.0f;
        private float bossSummonTimer = 30.0f;
        private bool isCastingSpecialSkill = false;

        [Header("Alien Warlord (Chúa Tể Alien) Boss State")]
        public float PlasmaShieldHp { get; private set; }
        public float MaxPlasmaShieldHp { get; private set; }
        public bool IsAlienOverloaded => isAlienOverloaded;
        private bool isAlienOverloaded = false;
        private GameObject plasmaShieldVisualObj = null;
        private SpriteRenderer plasmaShieldSr = null;
        private GameObject plasmaShieldBarFill = null;
        private float alienBeamCooldown = 5.0f;
        private float alienTeleportCooldown = 7.0f;

        [Header("UFO Mothership (Mẫu Hạm UFO) Boss State")]
        private float mothershipOrbitalCooldown = 6.0f;
        private float mothershipDropPodTimer = 22.0f;
        private float mothershipMegaBeamTimer = 30.0f;
        private GameObject activeMegaTractorBeamObj = null;
        private Coroutine megaTractorBeamCoroutine = null;
        private readonly System.Collections.Generic.List<PigAgentView> megaBeamPulledPigs = new System.Collections.Generic.List<PigAgentView>();

        [Header("Defense Debuffs (Bẫy Chông / Lôi Trận)")]
        private float slowDebuffDuration = 0f;
        private float slowMultiplier = 1.0f;

        public void ApplySlow(float multiplier, float duration)
        {
            slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
            slowDebuffDuration = Mathf.Max(slowDebuffDuration, duration);
            SpeedMultiplier = slowMultiplier;
        }

        public void ApplySpeedBuff(float multiplier, float duration)
        {
            SpeedMultiplier = Mathf.Max(SpeedMultiplier, multiplier);
            speedBuffDuration = Mathf.Max(speedBuffDuration, duration);
        }

        public bool IsAlive => Model != null && Model.IsAlive && !isDying;
        public Vector2 Position => transform.position;

        public event Action<EnemyAgentView> OnDefeated;

        public void Bind(EnemyData model)
        {
            Model = model;
            LoadSpritesForType(model.TypeId);
            CreateFloatingHealthBar();

            if (model.TypeId == EnemyTypeId.BossAlienWarlord)
            {
                MaxPlasmaShieldHp = CombatFormulas.CalculateAlienPlasmaShield(model.MaxHp);
                PlasmaShieldHp = MaxPlasmaShieldHp;
                CreateAlienPlasmaShieldVisual();
            }
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
                SpriteDown = UISpriteLoader.GetPestSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_down.png", 32) ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_pest_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_hurt.png", 32) ?? SpriteDown;
                SpriteAttack = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_attack.png", 32) ?? SpriteDown;
                SpriteDie = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_die.png", 32) ?? SpriteDown;

                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_down_walk4.png", 32)
                };
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_up_walk4.png", 32)
                };
                AttackFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_attack1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_attack2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_attack3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_attack4.png", 32)
                };
                DieFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_die1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_die2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_die3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Pest/pest_die4.png", 32)
                };
            }
            else if (type == EnemyTypeId.KeCaoPha)
            {
                SpriteDown = UISpriteLoader.GetBreakerSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_down.png", 32) ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_breaker_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_hurt.png", 32) ?? SpriteDown;
                SpriteAttack = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_attack.png", 32) ?? SpriteDown;
                SpriteDie = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_die.png", 32) ?? SpriteDown;

                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_down_walk4.png", 32)
                };
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_up_walk4.png", 32)
                };
                AttackFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_attack1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_attack2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_attack3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_attack4.png", 32)
                };
                DieFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_die1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_die2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_die3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Breaker/breaker_die4.png", 32)
                };
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
                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienGrunt/alien_grunt_down_walk4.png", 32)
                };
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
                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/AlienElite/alien_elite_down_walk4.png", 32)
                };
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
                SpriteAttack = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_attack.png", 32);
                SpriteDie = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_die.png", 32);
                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_down_walk4.png", 32)
                };
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_up_walk4.png", 32)
                };
                AttackFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_attack1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_attack2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_attack3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_attack4.png", 32)
                };
                DieFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_die1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_die2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_die3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossAlienWarlord/boss_alien_warlord_die4.png", 32)
                };
            }
            else if (type == EnemyTypeId.UfoMothership)
            {
                SpriteDown = UISpriteLoader.GetUfoMothershipSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Vehicles/ufo_mothership.png", 32);
                SpriteSide = SpriteDown;
                SpriteUp = SpriteDown;
                SpriteHurt = SpriteDown;
            }
            else if (type == EnemyTypeId.DauLinhTrom)
            {
                SpriteDown = UISpriteLoader.GetDauLinhTromSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_hurt.png", 32) ?? SpriteDown;
                SpriteAttack = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_attack.png", 32) ?? SpriteDown;
                SpriteDie = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_die.png", 32) ?? SpriteDown;

                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_down_walk4.png", 32)
                };
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_up_walk4.png", 32)
                };
                AttackFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_attack1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_attack2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_attack3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_attack4.png", 32)
                };
                DieFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_die1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_die2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_die3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/DauLinhTrom/dau_linh_die4.png", 32)
                };
            }
            else if (type == EnemyTypeId.ChuaTromRiuSat)
            {
                SpriteDown = UISpriteLoader.GetBossChuaTromSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_hurt.png", 32) ?? SpriteDown;
                SpriteAttack = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_attack.png", 32) ?? SpriteDown;
                SpriteDie = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_die.png", 32) ?? SpriteDown;

                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_down_walk4.png", 32)
                };
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_up_walk4.png", 32)
                };
                AttackFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_attack1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_attack2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_attack3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_attack4.png", 32)
                };
                DieFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_die1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_die2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_die3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/BossChuaTromRiuSat/boss_chua_trom_die4.png", 32)
                };
            }
            else
            {
                SpriteDown = UISpriteLoader.GetThiefSprite() ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_down.png", 32) ?? UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/enemy_thief_down.png", 32);
                SpriteSide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_side.png", 32) ?? SpriteDown;
                SpriteLeft = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_left.png", 32) ?? SpriteSide;
                SpriteRight = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_right.png", 32) ?? SpriteSide;
                SpriteUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_up.png", 32) ?? SpriteDown;
                SpriteHurt = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_hurt.png", 32) ?? SpriteDown;
                SpriteAttack = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_attack.png", 32) ?? SpriteDown;
                SpriteDie = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_die.png", 32) ?? SpriteDown;

                DownWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_down_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_down_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_down_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_down_walk4.png", 32)
                };
                SideWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_side_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_side_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_side_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_side_walk4.png", 32)
                };
                UpWalkFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_up_walk1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_up_walk2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_up_walk3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_up_walk4.png", 32)
                };
                AttackFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_attack1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_attack2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_attack3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_attack4.png", 32)
                };
                DieFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_die1.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_die2.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_die3.png", 32),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Enemies/Thief/thief_die4.png", 32)
                };
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
                    transform.localScale = new Vector3(1.85f, 1.85f, 1f);
                    break;
                case EnemyTypeId.ChuaTromRiuSat:
                    transform.localScale = new Vector3(2.45f, 2.45f, 1f);
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

            // 1. Cấu hình Movement Footprint Collider (Va chạm vật lý chân tiếp đất - Solid/isTrigger=false)
            // Đặt tại chân, hướng ngang (Horizontal) dẹt để quái lướt mượt qua góc tường/hàng rào,
            // không để phần đầu/thân trên bị va quẹt cản trở di chuyển.
            if (col is CapsuleCollider2D footCapsule)
            {
                footCapsule.isTrigger = false;
                footCapsule.direction = CapsuleDirection2D.Horizontal;

                switch (type)
                {
                    case EnemyTypeId.AlienGrunt:
                        // scale 1.55 -> world (0.56m x 0.28m), chiếm ~0.55 ô ngang
                        footCapsule.size = new Vector2(0.36f, 0.18f);
                        footCapsule.offset = new Vector2(0f, -0.75f);
                        break;

                    case EnemyTypeId.AlienElite:
                        // scale 1.95 -> world (0.78m x 0.37m), chiếm ~0.78 ô ngang
                        footCapsule.size = new Vector2(0.40f, 0.19f);
                        footCapsule.offset = new Vector2(0f, -0.75f);
                        break;

                    case EnemyTypeId.BossAlienWarlord:
                        // scale 2.85 -> world (1.20m x 0.57m), chiếm ~1.2 ô ngang
                        footCapsule.size = new Vector2(0.42f, 0.20f);
                        footCapsule.offset = new Vector2(0f, -0.74f);
                        break;

                    case EnemyTypeId.ChuotGamKho:
                        // scale 1.15 -> world (0.52m x 0.29m), chiếm ~0.5 ô
                        footCapsule.size = new Vector2(0.45f, 0.25f);
                        footCapsule.offset = new Vector2(0f, -0.25f);
                        break;

                    case EnemyTypeId.KeBatHeo:
                        // scale 1.35 -> world (0.51m x 0.24m)
                        footCapsule.size = new Vector2(0.38f, 0.18f);
                        footCapsule.offset = new Vector2(0f, -0.75f);
                        break;

                    case EnemyTypeId.KeCaoPha:
                        // scale 1.50 -> world (0.57m x 0.27m)
                        footCapsule.size = new Vector2(0.38f, 0.18f);
                        footCapsule.offset = new Vector2(0f, -0.75f);
                        break;

                    case EnemyTypeId.DauLinhTrom:
                        // scale 1.85 -> world (0.74m x 0.35m)
                        footCapsule.size = new Vector2(0.40f, 0.19f);
                        footCapsule.offset = new Vector2(0f, -0.75f);
                        break;

                    case EnemyTypeId.ChuaTromRiuSat:
                        // scale 2.45 -> world (1.03m x 0.49m), chiếm ~1.0 ô
                        footCapsule.size = new Vector2(0.42f, 0.20f);
                        footCapsule.offset = new Vector2(0f, -0.75f);
                        break;

                    case EnemyTypeId.UfoMothership:
                        footCapsule.size = new Vector2(0.85f, 0.38f);
                        footCapsule.offset = new Vector2(0f, -0.35f);
                        break;

                    case EnemyTypeId.UfoTrinhSat:
                    case EnemyTypeId.UfoThuHoach:
                        footCapsule.size = new Vector2(0.75f, 0.38f);
                        footCapsule.offset = new Vector2(0f, 0f);
                        break;

                    default:
                        footCapsule.size = new Vector2(0.38f, 0.18f);
                        footCapsule.offset = new Vector2(0f, -0.75f);
                        break;
                }
            }

            // 2. Cấu hình Combat Hurtbox (Hitbox nhận sát thương toàn thân - Trigger/isTrigger=true)
            // Bao phủ toàn bộ cơ thể (chân, bụng, ngực, đầu) để người chơi chém trúng bất kỳ bộ phận nào
            // cũng nhận sát thương, nhưng là Trigger nên KHÔNG BAO GIỜ bị hàng rào hay đá cản trở di chuyển!
            if (hurtboxCol == null)
            {
                var capsules = GetComponents<CapsuleCollider2D>();
                if (capsules.Length > 1)
                {
                    hurtboxCol = capsules[1];
                }
                else
                {
                    hurtboxCol = gameObject.AddComponent<CapsuleCollider2D>();
                }
            }

            if (hurtboxCol != null)
            {
                hurtboxCol.isTrigger = true;
                hurtboxCol.direction = CapsuleDirection2D.Vertical;

                switch (type)
                {
                    case EnemyTypeId.AlienGrunt:
                        // scale 1.55 -> world (0.65m x 1.63m), bao phủ từ chân đến đầu
                        hurtboxCol.size = new Vector2(0.42f, 1.05f);
                        hurtboxCol.offset = new Vector2(0f, -0.29f);
                        break;

                    case EnemyTypeId.AlienElite:
                        // scale 1.95 -> world (0.86m x 1.95m), bao phủ từ chân đến đầu
                        hurtboxCol.size = new Vector2(0.44f, 1.00f);
                        hurtboxCol.offset = new Vector2(0f, -0.33f);
                        break;

                    case EnemyTypeId.BossAlienWarlord:
                        // scale 2.85 -> world (1.25m x 2.85m), bao phủ từ chân đến đỉnh sừng
                        hurtboxCol.size = new Vector2(0.44f, 1.00f);
                        hurtboxCol.offset = new Vector2(0f, -0.33f);
                        break;

                    case EnemyTypeId.ChuotGamKho:
                        hurtboxCol.direction = CapsuleDirection2D.Horizontal;
                        hurtboxCol.size = new Vector2(0.55f, 0.40f);
                        hurtboxCol.offset = new Vector2(0f, -0.20f);
                        break;

                    case EnemyTypeId.KeBatHeo:
                        hurtboxCol.size = new Vector2(0.79f, 1.25f);
                        hurtboxCol.offset = new Vector2(0f, -0.20f);
                        break;

                    case EnemyTypeId.KeCaoPha:
                        hurtboxCol.size = new Vector2(0.77f, 1.15f);
                        hurtboxCol.offset = new Vector2(0f, -0.20f);
                        break;

                    case EnemyTypeId.DauLinhTrom:
                        hurtboxCol.size = new Vector2(0.55f, 0.95f);
                        hurtboxCol.offset = new Vector2(0f, -0.25f);
                        break;

                    case EnemyTypeId.ChuaTromRiuSat:
                        hurtboxCol.size = new Vector2(0.48f, 0.90f);
                        hurtboxCol.offset = new Vector2(0f, -0.30f);
                        break;

                    case EnemyTypeId.UfoMothership:
                        hurtboxCol.direction = CapsuleDirection2D.Horizontal;
                        hurtboxCol.size = new Vector2(0.95f, 0.55f);
                        hurtboxCol.offset = new Vector2(0f, 0f);
                        break;

                    case EnemyTypeId.UfoTrinhSat:
                    case EnemyTypeId.UfoThuHoach:
                        hurtboxCol.direction = CapsuleDirection2D.Horizontal;
                        hurtboxCol.size = new Vector2(0.80f, 0.52f);
                        hurtboxCol.offset = new Vector2(0f, 0f);
                        break;

                    default:
                        hurtboxCol.size = new Vector2(0.50f, 1.05f);
                        hurtboxCol.offset = new Vector2(0f, -0.25f);
                        break;
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
                else if (Model.TypeId == EnemyTypeId.ChuaTromRiuSat) barY = 1.45f;
                else if (Model.TypeId == EnemyTypeId.BossAlienWarlord) barY = 1.65f;
                else if (Model.TypeId == EnemyTypeId.DauLinhTrom) barY = 1.15f;
            }
            healthBarRoot.transform.localPosition = new Vector3(0f, barY, 0f);

            bool isBigBoss = Model != null && (Model.TypeId == EnemyTypeId.BossAlienWarlord || Model.TypeId == EnemyTypeId.UfoMothership || Model.TypeId == EnemyTypeId.ChuaTromRiuSat);
            bool isElite = Model != null && (Model.IsElite || Model.TypeId == EnemyTypeId.DauLinhTrom || Model.TypeId == EnemyTypeId.AlienElite);
            float bgWidth = isBigBoss ? 1.2f : (isElite ? 0.85f : 0.6f);
            float bgHeight = isBigBoss ? 0.12f : (isElite ? 0.10f : 0.08f);
            float fillWidth = isBigBoss ? 1.16f : (isElite ? 0.81f : 0.58f);
            float fillHeight = isBigBoss ? 0.10f : (isElite ? 0.08f : 0.06f);

            var bg = new GameObject("Bg");
            bg.transform.SetParent(healthBarRoot.transform, false);
            healthBarBgSr = bg.AddComponent<SpriteRenderer>();
            healthBarBgSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            healthBarBgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
            healthBarBgSr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 2 : 300;
            bg.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(healthBarRoot.transform, false);
            healthBarFillSr = fill.AddComponent<SpriteRenderer>();
            healthBarFillSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            healthBarFillSr.color = isBigBoss ? new Color(0.95f, 0.15f, 0.15f, 0.95f) : (isElite ? new Color(0.95f, 0.35f, 0.75f, 0.95f) : new Color(0.95f, 0.25f, 0.25f, 0.95f));
            healthBarFillSr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 3 : 301;
            fill.transform.localScale = new Vector3(fillWidth, fillHeight, 1f);
            healthBarFill = fill.transform;

            // Thanh khiên Plasma màu xanh ngọc hiển thị ngay trên thanh HP đỏ của Chúa Tể Alien Warlord
            if (Model != null && Model.TypeId == EnemyTypeId.BossAlienWarlord)
            {
                var shieldBar = new GameObject("ShieldFill");
                shieldBar.transform.SetParent(healthBarRoot.transform, false);
                shieldBar.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                var shieldSr = shieldBar.AddComponent<SpriteRenderer>();
                shieldSr.sprite = UISpriteLoader.GetFrameSlotNormal();
                shieldSr.color = new Color(0.2f, 0.85f, 1f, 0.95f);
                shieldSr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 4 : 302;
                shieldBar.transform.localScale = new Vector3(fillWidth, 0.045f, 1f);
                plasmaShieldBarFill = shieldBar;
            }

            if (isElite || isBigBoss)
            {
                var labelObj = new GameObject("NameLabel");
                labelObj.transform.SetParent(healthBarRoot.transform, false);
                float labelY = (Model != null && Model.TypeId == EnemyTypeId.BossAlienWarlord) ? 0.24f : 0.16f;
                labelObj.transform.localPosition = new Vector3(0f, labelY, 0f);
                var tmp = labelObj.AddComponent<TextMeshPro>();
                tmp.text = $"★ {Model?.Name} ★";
                tmp.fontSize = 2.6f;
                tmp.color = isBigBoss ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(1f, 0.85f, 0.2f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 5 : 303;
                tmp.rectTransform.sizeDelta = new Vector2(3.5f, 0.4f);
            }

            healthBarRoot.SetActive(false); // Chỉ hiện khi nhận sát thương
        }

        /// <summary>
        /// Tính toán độ lệch Y từ tâm transform xuống điểm tiếp xúc mặt đất (chân nhân vật)
        /// để đảm bảo cơ chế 2D Top-Down Y-Sorting hoạt động chuẩn xác theo phối cảnh Stardew Valley.
        /// </summary>
        public float GetFootYOffset()
        {
            if (Model == null) return -0.5f;
            return GetFootYOffsetForType(Model.TypeId);
        }

        public static float GetFootYOffsetForType(EnemyTypeId type)
        {
            // Điểm tiếp xúc chân trên canvas 64x64 (PPU 32) nằm ở hàng pixel ~5 -> (5 - 32)/32 = -27/32 = -0.84375f
            switch (type)
            {
                case EnemyTypeId.BossAlienWarlord:
                    return -2.40f; // Boss to lớn scale 2.85 -> chân cách tâm -2.40m
                case EnemyTypeId.ChuaTromRiuSat:
                    return -2.07f; // scale 2.45 -> chân cách tâm -2.07m
                case EnemyTypeId.AlienElite:
                    return -1.65f; // scale 1.95 -> chân cách tâm -1.65m
                case EnemyTypeId.DauLinhTrom:
                    return -1.56f; // scale 1.85 -> chân cách tâm -1.56m
                case EnemyTypeId.AlienGrunt:
                    return -1.31f; // scale 1.55 -> chân cách tâm -1.31m
                case EnemyTypeId.KeCaoPha:
                    return -1.27f; // scale 1.50 -> chân cách tâm -1.27m
                case EnemyTypeId.KeBatHeo:
                    return -1.14f; // scale 1.35 -> chân cách tâm -1.14m
                case EnemyTypeId.ChuotGamKho:
                    return -0.43f; // scale 1.15, sprite 32x32 chân tại y=4 -> (4 - 16)/32 * 1.15
                case EnemyTypeId.UfoMothership:
                    return -2.50f; // Điểm tiếp giáp mặt đất của pháo đài đổ bộ
                case EnemyTypeId.UfoTrinhSat:
                case EnemyTypeId.UfoThuHoach:
                    return -1.20f; // Điểm tham chiếu đáy phi thuyền
                default:
                    return -0.5f;
            }
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

            if (speedBuffDuration > 0f)
            {
                speedBuffDuration -= Time.deltaTime;
                if (speedBuffDuration <= 0f)
                {
                    SpeedMultiplier = 1.0f;
                }
            }

            if (slowDebuffDuration > 0f)
            {
                slowDebuffDuration -= Time.deltaTime;
                if (slowDebuffDuration <= 0f)
                {
                    slowMultiplier = 1.0f;
                    SpeedMultiplier = 1.0f;
                }
                else
                {
                    SpeedMultiplier = slowMultiplier;
                }
            }

            // GDD 19.1: Aura Chỉ Huy của Đầu Lĩnh Trộm (quét 10m mỗi 0.5s buff quái trộm +20% speed)
            if (Model != null && Model.TypeId == EnemyTypeId.DauLinhTrom && IsAlive)
            {
                commandAuraTimer += Time.deltaTime;
                if (commandAuraTimer >= 0.5f)
                {
                    commandAuraTimer = 0f;
                    PulseCommandAura();
                }
            }

            // GDD 19.1 & Q05: Cơ chế kỹ năng Boss Chúa Trộm Rìu Sắt
            if (Model != null && Model.TypeId == EnemyTypeId.ChuaTromRiuSat && IsAlive)
            {
                if (bossWhirlwindCooldown > 0f) bossWhirlwindCooldown -= Time.deltaTime;
                if (bossGroundSlamCooldown > 0f) bossGroundSlamCooldown -= Time.deltaTime;

                bossSummonTimer -= Time.deltaTime;
                if (bossSummonTimer <= 0f)
                {
                    bossSummonTimer = 30.0f;
                    StartCoroutine(TriggerBanditReinforcementsRoutine());
                }

                if (!isCastingSpecialSkill)
                {
                    var player = PlayerCombatController.Instance;
                    if (player != null && player.IsAlive)
                    {
                        float distToPlayer = Vector2.Distance(transform.position, player.Position);

                        // Ưu tiên 1: Càn quét rìu xoay nếu người chơi áp sát cận chiến (<= 2.5m)
                        if (distToPlayer <= 2.5f && bossWhirlwindCooldown <= 0f)
                        {
                            StartCoroutine(ExecuteWhirlwindSlashRoutine());
                        }
                        // Ưu tiên 2: Dậm đất địa chấn nếu người chơi ở cự ly 2.5m - 4.5m
                        else if (distToPlayer <= 4.5f && distToPlayer >= 1.8f && bossGroundSlamCooldown <= 0f)
                        {
                            StartCoroutine(ExecuteGroundSlamRoutine());
                        }
                    }
                }
            }

            // GDD 19.2 & Q10: Cơ chế kỹ năng Boss Ngoài Hành Tinh (Alien Warlord & UFO Mothership)
            if (Model != null && IsAlive)
            {
                if (Model.TypeId == EnemyTypeId.BossAlienWarlord)
                {
                    if (alienBeamCooldown > 0f) alienBeamCooldown -= Time.deltaTime;
                    if (alienTeleportCooldown > 0f) alienTeleportCooldown -= Time.deltaTime;

                    if (!isCastingSpecialSkill)
                    {
                        var player = PlayerCombatController.Instance;
                        if (player != null && player.IsAlive)
                        {
                            float distToPlayer = Vector2.Distance(transform.position, player.Position);

                            // Cận chiến < 1.8m -> Tốc biến chiến thuật giữ cự ly bắn
                            if (distToPlayer < 1.8f && alienTeleportCooldown <= 0f)
                            {
                                StartCoroutine(ExecuteAlienTeleportRoutine());
                            }
                            // Tầm xa 1.5m - 7.5m -> Bắn tia phân rã laser xuyên thấu
                            else if (distToPlayer >= 1.5f && distToPlayer <= 7.5f && alienBeamCooldown <= 0f)
                            {
                                StartCoroutine(ExecuteDisintegrationBeamRoutine());
                            }
                        }
                    }

                    // Hiệu ứng phát sáng lõi khi quá tải Phase 2
                    if (isAlienOverloaded && SpriteRenderer != null)
                    {
                        float pulse = Mathf.PingPong(Time.time * 6f, 0.4f);
                        SpriteRenderer.color = new Color(0.5f + pulse, 0.9f, 1f, 1f);
                    }
                }
                else if (Model.TypeId == EnemyTypeId.UfoMothership)
                {
                    mothershipOrbitalCooldown -= Time.deltaTime;
                    mothershipDropPodTimer -= Time.deltaTime;
                    mothershipMegaBeamTimer -= Time.deltaTime;

                    // Kỹ năng 1: Đại Pháo Quỹ Đạo Plasma mỗi 8s
                    if (mothershipOrbitalCooldown <= 0f && !isCastingSpecialSkill)
                    {
                        mothershipOrbitalCooldown = 8.0f;
                        StartCoroutine(ExecuteOrbitalPlasmaBarrageRoutine());
                    }

                    // Kỹ năng 2: Thả Kén Tiếp Viện mỗi 25s
                    if (mothershipDropPodTimer <= 0f)
                    {
                        mothershipDropPodTimer = 25.0f;
                        StartCoroutine(TriggerAlienDropPodRoutine());
                    }

                    // Kỹ năng 3: Chùm Kéo Lớn (Mega Tractor Beam) mỗi 35s
                    if (mothershipMegaBeamTimer <= 0f && !isCastingSpecialSkill && activeMegaTractorBeamObj == null)
                    {
                        mothershipMegaBeamTimer = 35.0f;
                        megaTractorBeamCoroutine = StartCoroutine(ExecuteMegaTractorBeamRoutine());
                    }
                }
            }

            // Cập nhật Y-Sorting Order cho 2D Top-Down chuẩn theo điểm tiếp đất (Foot / Ground Contact Point)
            if (SpriteRenderer != null)
            {
                float footY = transform.position.y + GetFootYOffset();
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-footY * 100);

                // Đồng bộ layer thanh máu luôn nằm trên cơ thể kẻ địch chính nó
                if (healthBarRoot != null && healthBarRoot.activeSelf)
                {
                    if (healthBarBgSr != null) healthBarBgSr.sortingOrder = SpriteRenderer.sortingOrder + 2;
                    if (healthBarFillSr != null) healthBarFillSr.sortingOrder = SpriteRenderer.sortingOrder + 3;
                }
            }

            DecideTargetAndMove();
        }

        private void DecideTargetAndMove()
        {
            if (isCastingSpecialSkill)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

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
                    rb.linearVelocity = escapeDir * (Model.MoveSpeed * SpeedMultiplier * 1.25f);
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
            if (Model.TypeId == EnemyTypeId.BossAlienWarlord || Model.TypeId == EnemyTypeId.ChuaTromRiuSat)
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
            else if (Model.TypeId == EnemyTypeId.DauLinhTrom)
            {
                // GDD 19.1: Đầu Lĩnh Trộm nhắm bắt chú heo có giá trị / độ hiếm cao nhất toàn trại!
                if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || currentTarget == player?.transform)
                {
                    float bestScore = -1f;
                    PigAgentView highestValPig = null;
                    foreach (var p in FindObjectsByType<PigAgentView>())
                    {
                        if (p != null && p.PigModel != null && p.PigModel.IsAlive && !p.IsCaptured)
                        {
                            float score = CombatFormulas.CalculatePigTheftValue(p.PigModel);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                highestValPig = p;
                            }
                        }
                    }

                    if (highestValPig != null)
                    {
                        currentTarget = highestValPig.transform;
                    }
                    else if (player != null && player.IsAlive)
                    {
                        currentTarget = player.transform;
                    }
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
                    // Ưu tiên tấn công Tháp Canh nếu có trong bán kính 14m
                    DefenseTower2DView bestTower = null;
                    foreach (var t in FindObjectsByType<DefenseTower2DView>())
                    {
                        if (t != null && t.IsAlive)
                        {
                            float d = Vector2.Distance(transform.position, t.transform.position);
                            if (d < 14f && d < minDist) { minDist = d; bestTower = t; }
                        }
                    }

                    if (bestTower != null)
                    {
                        currentTarget = bestTower.transform;
                    }
                    else
                    {
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
                    rb.linearVelocity = moveDir * (Model.MoveSpeed * SpeedMultiplier);
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

            if (currentTarget != null)
            {
                Vector2 dirToTarget = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
                UpdateFacingSprite(dirToTarget);
            }

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
                    sr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder - 1 : 100;
                    sr.color = new Color(0.3f, 0.9f, 1f, 0.85f);

                    ufoAbductionTimer = 0f;
                    PlayerInteractionController.Instance?.ShowFeedback("<color=#c084fc>CẢNH BÁO: UFO đang chiếu Tractor Beam hút heo! Hãy tấn công UFO để cứu heo!</color>");
                    return;
                }
            }

            // 2. Kẻ Bắt Heo & Đầu Lĩnh Trộm tóm heo và đổi hướng tẩu thoát
            if (Model.TypeId == EnemyTypeId.KeBatHeo || Model.TypeId == EnemyTypeId.DauLinhTrom)
            {
                var targetPig = currentTarget.GetComponent<PigAgentView>();
                if (targetPig != null && !targetPig.IsCaptured && capturedPig == null)
                {
                    capturedPig = targetPig;
                    capturedPig.CaptureByThief(this.transform);
                    escapeTargetPos = CalculateEscapeEdge(transform.position);

                    if (Model.TypeId == EnemyTypeId.DauLinhTrom && !hasDroppedSmokeBomb)
                    {
                        hasDroppedSmokeBomb = true;
                        DeploySmokeBomb(transform.position);
                    }
                    else
                    {
                        PlayerInteractionController.Instance?.ShowFeedback("<color=#ef4444>BÁO ĐỘNG: Kẻ trộm đã tóm được Heo! Hãy đuổi theo tấn công hắn để cứu heo!</color>");
                    }
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
                float dmg = (Model != null && Model.TypeId == EnemyTypeId.ChuaTromRiuSat)
                    ? CombatFormulas.CalculateSiegeDamage(Model.AttackDamage, Model.SiegeDamageMultiplier)
                    : Model.AttackDamage;
                fence.TakeDamage(dmg);
                StartCoroutine(LungeVisualRoutine((currentTarget.position - transform.position).normalized));
                return;
            }
        }

        private IEnumerator LungeVisualRoutine(Vector2 dir)
        {
            Vector3 startPos = transform.position;
            Vector3 lungePos = startPos + (Vector3)(dir * 0.25f);
            float elapsed = 0f;
            Sprite prevSprite = SpriteRenderer != null ? SpriteRenderer.sprite : null;

            if (AttackFrames != null && AttackFrames.Length > 0 && SpriteRenderer != null)
            {
                SpriteRenderer.sprite = AttackFrames[0] ?? SpriteAttack ?? prevSprite;
            }
            else if (SpriteAttack != null && SpriteRenderer != null)
            {
                SpriteRenderer.sprite = SpriteAttack;
            }

            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, lungePos, elapsed / 0.08f);
                if (AttackFrames != null && AttackFrames.Length >= 2 && SpriteRenderer != null)
                {
                    int fIdx = Mathf.Min(AttackFrames.Length - 1, (int)((elapsed / 0.08f) * 2));
                    if (AttackFrames[fIdx] != null) SpriteRenderer.sprite = AttackFrames[fIdx];
                }
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(lungePos, startPos, elapsed / 0.1f);
                if (AttackFrames != null && AttackFrames.Length >= 4 && SpriteRenderer != null)
                {
                    int fIdx = Mathf.Min(AttackFrames.Length - 1, 2 + (int)((elapsed / 0.1f) * 2));
                    if (AttackFrames[fIdx] != null) SpriteRenderer.sprite = AttackFrames[fIdx];
                }
                yield return null;
            }
            if (SpriteRenderer != null && prevSprite != null)
            {
                SpriteRenderer.sprite = prevSprite;
            }
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!IsAlive) return;
            if (hitRecoveryTimer > 0f) return; // Bảo vệ miễn nhiễm va chạm tức thời (0.15s)
            hitRecoveryTimer = 0.15f;

            // GDD 19.1: Bom khói tẩu thoát nếu bị tấn công khi đang cướp heo
            if (Model != null && Model.TypeId == EnemyTypeId.DauLinhTrom && capturedPig != null && !hasDroppedSmokeBomb)
            {
                hasDroppedSmokeBomb = true;
                DeploySmokeBomb(transform.position);
            }

            // GDD 19.2 / Q10: Nếu Mẫu Hạm đang chiếu Chùm Kéo Lớn mà bị tấn công -> Ngắt chùm kéo ngay lập tức!
            if (Model != null && Model.TypeId == EnemyTypeId.UfoMothership && activeMegaTractorBeamObj != null)
            {
                InterruptMegaTractorBeam();
            }

            // Đang cướp heo hoặc đang chiếu tia bắt cóc mà bị đánh trúng -> Đánh rơi heo ra!
            if (capturedPig != null || activeTractorBeamObj != null)
            {
                ReleaseCapturedPig(rescuedByPlayer: true);
            }

            // GDD 19.2: Khiên Năng Lượng Plasma của Chúa Tể Alien Warlord (hấp thụ 15% Max HP)
            if (Model != null && Model.TypeId == EnemyTypeId.BossAlienWarlord && PlasmaShieldHp > 0f)
            {
                float absorbed = Mathf.Min(amount, PlasmaShieldHp);
                PlasmaShieldHp -= absorbed;
                amount -= absorbed;

                if (healthBarRoot != null) healthBarRoot.SetActive(true);
                if (plasmaShieldBarFill != null && MaxPlasmaShieldHp > 0f)
                {
                    float shieldRatio = Mathf.Clamp01(PlasmaShieldHp / MaxPlasmaShieldHp);
                    plasmaShieldBarFill.transform.localScale = new Vector3(1.16f * shieldRatio, 0.045f, 1f);
                }

                SpawnShieldDamagePopup(absorbed);

                if (plasmaShieldSr != null)
                {
                    plasmaShieldSr.color = new Color(0.6f, 1f, 1f, 0.85f);
                }

                if (PlasmaShieldHp <= 0f)
                {
                    PlasmaShieldHp = 0f;
                    if (plasmaShieldVisualObj != null) plasmaShieldVisualObj.SetActive(false);
                    if (plasmaShieldBarFill != null) plasmaShieldBarFill.SetActive(false);

                    PlayerInteractionController.Instance?.ShowFeedback("<color=#38bdf8><b>[VỠ KHIÊN PLASMA!]</b> Khiên năng lượng của Chúa Tể Alien bị phá vỡ! Hắn kích hoạt Tốc Biến khẩn cấp!</color>");

                    TriggerAlienShieldBreakShockwave();

                    if (!isCastingSpecialSkill && IsAlive)
                    {
                        StartCoroutine(ExecuteAlienTeleportRoutine());
                    }
                }

                if (amount <= 0f)
                {
                    // Toàn bộ sát thương đã bị khiên hấp thụ hoàn toàn
                    return;
                }
            }

            float actualDamage = Model.TakeDamage(amount);

            // 1. Cập nhật thanh máu trên đầu
            if (healthBarRoot != null)
            {
                healthBarRoot.SetActive(true);
                if (healthBarFill != null)
                {
                    float ratio = Mathf.Clamp01(Model.CurrentHp / Model.MaxHp);
                    bool isBigBoss = Model != null && (Model.TypeId == EnemyTypeId.BossAlienWarlord || Model.TypeId == EnemyTypeId.UfoMothership || Model.TypeId == EnemyTypeId.ChuaTromRiuSat);
                    bool isElite = Model != null && (Model.IsElite || Model.TypeId == EnemyTypeId.DauLinhTrom || Model.TypeId == EnemyTypeId.AlienElite);
                    float maxFillWidth = isBigBoss ? 1.16f : (isElite ? 0.81f : 0.58f);
                    float fillHeight = isBigBoss ? 0.10f : (isElite ? 0.08f : 0.06f);
                    healthBarFill.localScale = new Vector3(maxFillWidth * ratio, fillHeight, 1f);
                }
            }

            // GDD 19.1: Khi HP <= 50%, Đầu Lĩnh Trộm thổi tù và triệu hồi 2 Kẻ Bắt Heo tiếp viện
            if (Model != null && Model.TypeId == EnemyTypeId.DauLinhTrom && !hasCalledBackup && Model.CurrentHp <= Model.MaxHp * 0.5f && Model.IsAlive)
            {
                hasCalledBackup = true;
                StartCoroutine(TriggerBackupHornRoutine());
            }

            // GDD 19.1 & KY_THUAT: Khi HP <= 30%, Chúa Trộm Rìu Sắt kích hoạt Cuồng Bạo Phase 2
            if (Model != null && Model.TypeId == EnemyTypeId.ChuaTromRiuSat && !isEnraged && Model.CurrentHp <= Model.MaxHp * 0.30f && Model.IsAlive)
            {
                isEnraged = true;
                StartCoroutine(TriggerBossEnrageRoutine());
            }

            // GDD 19.2: Khi HP <= 30%, Chúa Tể Alien kích hoạt Quá Tải Lõi Năng Lượng Phase 2
            if (Model != null && Model.TypeId == EnemyTypeId.BossAlienWarlord && !isAlienOverloaded && Model.CurrentHp <= Model.MaxHp * 0.30f && Model.IsAlive)
            {
                isAlienOverloaded = true;
                StartCoroutine(TriggerAlienOverloadRoutine());
            }

            // 2. Nhấp nháy màu đỏ & Sprite Hurt
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());

            // 3. Đẩy lùi (Knockback) - Miễn nhiễm nếu là Boss đang Cuồng Bạo/Quá Tải hoặc Tàu Mẹ (Super Armor)
            bool hasSuperArmor = (Model != null && ((Model.TypeId == EnemyTypeId.ChuaTromRiuSat && isEnraged) || (Model.TypeId == EnemyTypeId.BossAlienWarlord && isAlienOverloaded) || Model.TypeId == EnemyTypeId.UfoMothership));
            if (rb != null && knockbackForce > 0f && !hasSuperArmor)
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
            textMesh.rectTransform.sizeDelta = new Vector2(1.5f, 0.8f);

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
        public static int BanditFailureCount = 0;
        private bool banditFailureCounted = false;

        private void RegisterBanditFailure()
        {
            if (banditFailureCounted) return;
            banditFailureCounted = true;

            BanditFailureCount++;
            if (BanditFailureCount >= 2)
            {
                PlayerInteractionController.Instance?.ShowFeedback("<color=#f59e0b>ĐẠO TẶC BỊ ĐẨY LÙI NHIỀU LẦN! Tín hiệu khẩn cấp: Toán cướp cảm tử tràn vào!</color>");
                EnemyWaveSpawner.Instance?.TriggerBanditAmbush3(transform.position);
            }
            else
            {
                PlayerInteractionController.Instance?.ShowFeedback($"<color=#f59e0b>Đạo tặc bị ngăn chặn ({BanditFailureCount}/2 lần)! Cẩn thận đợt phục kích trả đũa!</color>");
            }
        }

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
            if (Model != null && (Model.TypeId == EnemyTypeId.KeBatHeo || Model.TypeId == EnemyTypeId.KeCaoPha))
            {
                RegisterBanditFailure();
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
            if (activeMegaTractorBeamObj != null)
            {
                InterruptMegaTractorBeam();
            }
            ufoAbductionTimer = 0f;

            if (rescuedByPlayer && Model != null && (Model.TypeId == EnemyTypeId.UfoTrinhSat || Model.TypeId == EnemyTypeId.UfoThuHoach))
            {
                RegisterUfoFailure();
            }
            if (rescuedByPlayer && Model != null && (Model.TypeId == EnemyTypeId.KeBatHeo || Model.TypeId == EnemyTypeId.KeCaoPha))
            {
                RegisterBanditFailure();
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
            textMesh.rectTransform.sizeDelta = new Vector2(2.5f, 0.8f);

            StartCoroutine(FloatingTextRoutine(rewardObj, textMesh));
        }

        private IEnumerator DeathFadeRoutine()
        {
            float elapsed = 0f;
            float duration = 0.55f;
            Vector3 origScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (DieFrames != null && DieFrames.Length > 0 && SpriteRenderer != null)
                {
                    int dIdx = Mathf.Min(DieFrames.Length - 1, (int)(t * DieFrames.Length));
                    if (DieFrames[dIdx] != null) SpriteRenderer.sprite = DieFrames[dIdx];
                }
                else if (SpriteDie != null && SpriteRenderer != null)
                {
                    SpriteRenderer.sprite = SpriteDie;
                }
                transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
                if (SpriteRenderer != null)
                {
                    SpriteRenderer.color = new Color(1f, 1f, 1f, 1f - t);
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        #region DauLinhTrom (Bandit Leader) GDD 19.1 Mechanics

        /// <summary>
        /// Aura Chỉ Huy: Phát sóng buff tốc độ chạy +20% cho tất cả quái trộm trong bán kính 10m mỗi 0.5s.
        /// </summary>
        private void PulseCommandAura()
        {
            float radius = Model?.CommandAuraRadius > 0f ? Model.CommandAuraRadius : 10f;
            float buff = Model?.CommandAuraSpeedBuff > 0f ? (1f + Model.CommandAuraSpeedBuff) : 1.20f;

            var allEnemies = FindObjectsByType<EnemyAgentView>();
            foreach (var ally in allEnemies)
            {
                if (ally != null && ally != this && ally.IsAlive)
                {
                    if (ally.Model != null && (ally.Model.TypeId == EnemyTypeId.KeBatHeo || ally.Model.TypeId == EnemyTypeId.KeCaoPha || ally.Model.TypeId == EnemyTypeId.ChuotGamKho))
                    {
                        float dist = Vector2.Distance(transform.position, ally.transform.position);
                        if (dist <= radius)
                        {
                            ally.ApplySpeedBuff(buff, 0.8f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tù Và Gọi Viện Binh: Khi HP <= 50%, triệu hồi 2 Kẻ Bắt Heo tiếp viện.
        /// </summary>
        private IEnumerator TriggerBackupHornRoutine()
        {
            PlayerInteractionController.Instance?.ShowFeedback("<color=#e11d48><b>[📯 TÙ VÀ VIỆN BINH!]</b> Đầu Lĩnh Trộm thổi tù và triệu hồi 2 Kẻ Bắt Heo tiếp viện!</color>");

            GameObject hornPopup = new GameObject("HornPopup");
            hornPopup.transform.position = transform.position + new Vector3(0f, 1.3f, 0f);
            var textMesh = hornPopup.AddComponent<TextMeshPro>();
            textMesh.text = "📯 VIỆN BINH!";
            textMesh.fontSize = 5.2f;
            textMesh.color = new Color(1f, 0.25f, 0.25f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(3.5f, 1f);
            StartCoroutine(FloatingTextRoutine(hornPopup, textMesh));

            yield return new WaitForSeconds(0.4f);

            int day = MobileGameController.Instance?.Engine?.Clock?.CurrentDay ?? 1;
            Vector2 leftSpawn = (Vector2)transform.position + new Vector2(-1.8f, UnityEngine.Random.Range(-0.4f, 0.4f));
            Vector2 rightSpawn = (Vector2)transform.position + new Vector2(1.8f, UnityEngine.Random.Range(-0.4f, 0.4f));

            if (EnemyWaveSpawner.Instance != null)
            {
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.KeBatHeo, leftSpawn, day);
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.KeBatHeo, rightSpawn, day);
            }
            else
            {
                var leftObj = new GameObject("Backup_KeBatHeo_Left");
                leftObj.transform.position = leftSpawn;
                var a1 = leftObj.AddComponent<EnemyAgentView>();
                a1.Bind(EnemyData.CreateKeBatHeo(day));

                var rightObj = new GameObject("Backup_KeBatHeo_Right");
                rightObj.transform.position = rightSpawn;
                var a2 = rightObj.AddComponent<EnemyAgentView>();
                a2.Bind(EnemyData.CreateKeBatHeo(day));
            }
        }

        /// <summary>
        /// Bom Khói Tẩu Thoát: Thả khói làm chậm người chơi 50% trong 2s, tăng tốc tẩu thoát cho Đầu Lĩnh Trộm.
        /// </summary>
        private void DeploySmokeBomb(Vector2 position)
        {
            PlayerInteractionController.Instance?.ShowFeedback("<color=#94a3b8><b>[💨 BOM KHÓI TẨU THOÁT!]</b> Đầu Lĩnh ném bom khói làm chậm người chơi 50% trong 2 giây!</color>");

            GameObject smokeCloud = new GameObject("SmokeBombCloud");
            smokeCloud.transform.position = position;
            var sr = smokeCloud.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameSlotNormal();
            sr.color = new Color(0.25f, 0.25f, 0.30f, 0.85f);
            sr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 1 : 250;
            smokeCloud.transform.localScale = new Vector3(3.0f, 3.0f, 1f);

            // Gây hiệu ứng làm chậm cho người chơi nếu đứng trong vùng khói (bán kính 6.0m)
            var player = PlayerMobileController.Instance;
            if (player != null && Vector2.Distance(position, player.transform.position) <= 6.0f)
            {
                player.ApplySlow(0.5f, 2.0f);
            }

            // Buff tốc độ tẩu thoát cho bản thân (+35% trong 2.5s)
            ApplySpeedBuff(1.35f, 2.5f);

            Destroy(smokeCloud, 2.5f);
        }

        #endregion

        #region Iron-Axe Bandit Lord (Chúa Trộm Rìu Sắt) GDD 19.1 Mechanics

        /// <summary>
        /// Kỹ năng 1: Càn Quét Rìu Xoay (Whirlwind Axe Slash).
        /// Vung chiếc rìu sắt khổng lồ xoay 360 độ (bán kính 2.8m), sát thương x1.35 và phá hủy hàng rào.
        /// </summary>
        private IEnumerator ExecuteWhirlwindSlashRoutine()
        {
            isCastingSpecialSkill = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            PlayerInteractionController.Instance?.ShowFeedback("<color=#ef4444><b>[CẢNH BÁO!]</b> Chúa Trộm đang giương rìu sắt chuẩn bị CÀN QUÉT!</color>");

            // Vòng tròn cảnh báo đỏ (Telegraph) bán kính 2.8m
            GameObject telegraph = new GameObject("Boss_WhirlwindTelegraph");
            telegraph.transform.position = transform.position;
            var sr = telegraph.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameSlotNormal();
            sr.color = new Color(0.95f, 0.15f, 0.15f, 0.35f);
            sr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder - 1 : 150;
            telegraph.transform.localScale = new Vector3(5.6f, 5.6f, 1f);

            // Chữ cảnh báo nổi
            GameObject popup = new GameObject("WhirlwindTelegraphText");
            popup.transform.position = transform.position + new Vector3(0f, 1.4f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "⚠️ CÀN QUÉT RÌU!";
            textMesh.fontSize = 5.0f;
            textMesh.color = new Color(1f, 0.2f, 0.2f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4f, 1f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            // Thời gian gồng (Windup telegraph) 0.55s
            float elapsed = 0f;
            while (elapsed < 0.55f)
            {
                elapsed += Time.deltaTime;
                if (sr != null)
                {
                    float alpha = Mathf.PingPong(elapsed * 6f, 0.5f) + 0.2f;
                    sr.color = new Color(0.95f, 0.15f, 0.15f, alpha);
                }
                yield return null;
            }
            if (telegraph != null) Destroy(telegraph);

            // Vung rìu xoay 360 độ chém quét
            SlashVFXController.Instance?.PlaySlash(transform.position, Vector2.right, Color.red);
            SlashVFXController.Instance?.PlaySlash(transform.position, Vector2.left, Color.red);

            // Animation chém 4 frame liên hoàn
            if (AttackFrames != null && AttackFrames.Length > 0 && SpriteRenderer != null)
            {
                foreach (var frame in AttackFrames)
                {
                    if (frame != null) SpriteRenderer.sprite = frame;
                    yield return new WaitForSeconds(0.04f);
                }
            }

            float whirlwindDmg = CombatFormulas.CalculateWhirlwindDamage(Model.AttackDamage);

            // Sát thương lên người chơi trong bán kính 2.8m
            var player = PlayerCombatController.Instance;
            if (player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) <= 2.8f)
            {
                Vector2 knock = (player.Position - (Vector2)transform.position).normalized;
                if (knock.sqrMagnitude < 0.01f) knock = Vector2.up;
                player.TakeDamage(whirlwindDmg, knock, 4.5f);
                PlayerInteractionController.Instance?.ShowFeedback("<color=#ef4444><b>[CÀN QUÉT RÌU XOAY!]</b> Bạn bị chém trúng và chịu sát thương nặng nề!</color>");
            }

            // Quét bay hàng rào xung quanh
            foreach (var f in FindObjectsByType<Fence2DView>())
            {
                if (f != null && !f.IsBroken && Vector2.Distance(transform.position, f.transform.position) <= 2.8f)
                {
                    f.TakeDamage(whirlwindDmg * 2.5f);
                }
            }

            if (SpriteRenderer != null && SpriteDown != null) SpriteRenderer.sprite = SpriteDown;
            bossWhirlwindCooldown = isEnraged ? 5.0f : 8.0f;
            isCastingSpecialSkill = false;
        }

        /// <summary>
        /// Kỹ năng 2: Dậm Đất Địa Chấn (Ground Slam Shockwave).
        /// Nhảy lên dậm mạnh rìu sắt xuống đất, tạo sóng chấn động 4m làm chậm 60% và gây sát thương.
        /// </summary>
        private IEnumerator ExecuteGroundSlamRoutine()
        {
            isCastingSpecialSkill = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            PlayerInteractionController.Instance?.ShowFeedback("<color=#f59e0b><b>[CẢNH BÁO!]</b> Chúa Trộm nhảy lên chuẩn bị DẬM ĐẤT ĐỊA CHẤN!</color>");

            GameObject popup = new GameObject("GroundSlamText");
            popup.transform.position = transform.position + new Vector3(0f, 1.4f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "⚡ DẬM ĐẤT!";
            textMesh.fontSize = 5.0f;
            textMesh.color = new Color(1f, 0.6f, 0.1f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4f, 1f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            // Nhảy lên cao trong 0.45s
            Vector3 startPos = transform.position;
            Vector3 peakPos = startPos + new Vector3(0f, 0.6f, 0f);
            float elapsed = 0f;
            while (elapsed < 0.45f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, peakPos, elapsed / 0.45f);
                yield return null;
            }

            // Dậm mạnh xuống đất
            transform.position = startPos;

            // Hiệu ứng sóng chấn động đất
            GameObject shockwave = new GameObject("Boss_GroundShockwave");
            shockwave.transform.position = transform.position;
            var sr = shockwave.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameSlotNormal();
            sr.color = new Color(0.95f, 0.55f, 0.15f, 0.65f);
            sr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder - 1 : 150;
            shockwave.transform.localScale = new Vector3(8.0f, 8.0f, 1f);
            Destroy(shockwave, 0.4f);

            float slamDmg = CombatFormulas.CalculateGroundSlamDamage(Model.AttackDamage);

            // Gây sát thương và làm chậm 60% người chơi trong bán kính 4.2m
            var player = PlayerCombatController.Instance;
            if (player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) <= 4.2f)
            {
                Vector2 knock = (player.Position - (Vector2)transform.position).normalized;
                if (knock.sqrMagnitude < 0.01f) knock = Vector2.down;
                player.TakeDamage(slamDmg, knock, 3.0f);
                PlayerMobileController.Instance?.ApplySlow(0.40f, 2.5f);
                PlayerInteractionController.Instance?.ShowFeedback("<color=#f59e0b><b>[DẬM ĐẤT ĐỊA CHẤN!]</b> Sóng chấn động làm bạn choáng váng và giảm 60% tốc độ chạy!</color>");
            }

            // Phá hủy rào trong phạm vi
            foreach (var f in FindObjectsByType<Fence2DView>())
            {
                if (f != null && !f.IsBroken && Vector2.Distance(transform.position, f.transform.position) <= 3.8f)
                {
                    f.TakeDamage(slamDmg);
                }
            }

            bossGroundSlamCooldown = isEnraged ? 8.5f : 14.0f;
            isCastingSpecialSkill = false;
        }

        /// <summary>
        /// Kỹ năng 3: Triệu Tập Tiếp Viện (Summon Bandit Reinforcements).
        /// Cứ mỗi 30s gọi 2 Kẻ Bắt Heo + 1 Kẻ Cào Phá tràn vào hỗ trợ.
        /// </summary>
        private IEnumerator TriggerBanditReinforcementsRoutine()
        {
            PlayerInteractionController.Instance?.ShowFeedback("<color=#e11d48><b>[TRIỆU TẬP TIẾP VIỆN!]</b> Chúa Trộm hú vang gọi toán cướp tràn vào tiếp ứng!</color>");

            GameObject popup = new GameObject("BossSummonPopup");
            popup.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "📯 TẬP HỢP TOÁN CƯỚP!";
            textMesh.fontSize = 5.2f;
            textMesh.color = new Color(1f, 0.2f, 0.2f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4.5f, 1f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            yield return new WaitForSeconds(0.4f);

            int day = MobileGameController.Instance?.Engine?.Clock?.CurrentDay ?? 1;
            Vector2 p1 = (Vector2)transform.position + new Vector2(-2.2f, UnityEngine.Random.Range(-0.8f, 0.8f));
            Vector2 p2 = (Vector2)transform.position + new Vector2(2.2f, UnityEngine.Random.Range(-0.8f, 0.8f));
            Vector2 p3 = (Vector2)transform.position + new Vector2(0f, -2.0f);

            if (EnemyWaveSpawner.Instance != null)
            {
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.KeBatHeo, p1, day);
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.KeBatHeo, p2, day);
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.KeCaoPha, p3, day);
            }
            else
            {
                var o1 = new GameObject("Reinforce_KeBatHeo_1"); o1.transform.position = p1;
                o1.AddComponent<EnemyAgentView>().Bind(EnemyData.CreateKeBatHeo(day));

                var o2 = new GameObject("Reinforce_KeBatHeo_2"); o2.transform.position = p2;
                o2.AddComponent<EnemyAgentView>().Bind(EnemyData.CreateKeBatHeo(day));

                var o3 = new GameObject("Reinforce_KeCaoPha_1"); o3.transform.position = p3;
                o3.AddComponent<EnemyAgentView>().Bind(EnemyData.CreateKeCaoPha(day));
            }
        }

        /// <summary>
        /// Kỹ năng 4: Cuồng Bạo Phase 2 (Enrage Mode: HP <= 30%).
        /// Tăng 30% tốc độ, giảm thời gian hồi đòn, miễn nhiễm hoàn toàn hiệu ứng đẩy lùi (Super Armor).
        /// </summary>
        private IEnumerator TriggerBossEnrageRoutine()
        {
            PlayerInteractionController.Instance?.ShowFeedback("<color=#dc2626><b>[CẢNH BÁO: CHÚA TRỘM CUỒNG BẠO!]</b> HP dưới 30%, Chúa Trộm hóa điên: Tăng 30% tốc độ và miễn nhiễm đẩy lùi!</color>");

            GameObject popup = new GameObject("BossEnragePopup");
            popup.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "🔥 CUỒNG BẠO!";
            textMesh.fontSize = 5.6f;
            textMesh.color = new Color(1f, 0.1f, 0.1f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4.5f, 1.2f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            // Tăng tốc độ vĩnh viễn cho đến khi bị hạ
            ApplySpeedBuff(1.30f, 9999f);
            Model.AttackCooldown = 1.0f; // Đánh dồn dập

            // Hiệu ứng đỏ rực
            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
            }

            yield return new WaitForSeconds(0.5f);
        }

        #endregion

        #region Alien Bosses (BossAlienWarlord & UfoMothership) GDD 19.2 Mechanics

        /// <summary>
        /// Khởi tạo vòng bảo vệ năng lượng Plasma (Plasma Shield Visual) quanh Chúa Tể Alien Warlord.
        /// </summary>
        private void CreateAlienPlasmaShieldVisual()
        {
            if (plasmaShieldVisualObj != null) return;
            plasmaShieldVisualObj = new GameObject("Alien_PlasmaShield");
            plasmaShieldVisualObj.transform.SetParent(transform, false);
            plasmaShieldVisualObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            plasmaShieldSr = plasmaShieldVisualObj.AddComponent<SpriteRenderer>();
            plasmaShieldSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            plasmaShieldSr.color = new Color(0.2f, 0.85f, 1f, 0.40f);
            plasmaShieldSr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 1 : 250;
            plasmaShieldVisualObj.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
        }

        private void SpawnShieldDamagePopup(float damage)
        {
            GameObject popupObj = new GameObject("ShieldDamagePopup");
            popupObj.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0.95f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            textMesh.text = $"-{Mathf.RoundToInt(damage)} (Khiên)";
            textMesh.fontSize = 4.4f;
            textMesh.color = new Color(0.3f, 0.9f, 1f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 710;
            textMesh.rectTransform.sizeDelta = new Vector2(2.5f, 0.8f);

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        /// <summary>
        /// Sóng xung kích khi khiên Plasma vỡ: Hất văng người chơi nếu đứng quá gần (3.5m).
        /// </summary>
        private void TriggerAlienShieldBreakShockwave()
        {
            GameObject shockwave = new GameObject("Alien_ShieldBreak_Shockwave");
            shockwave.transform.position = transform.position;
            var sr = shockwave.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameSlotNormal();
            sr.color = new Color(0.3f, 0.95f, 1f, 0.75f);
            sr.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 2 : 255;
            shockwave.transform.localScale = new Vector3(7.0f, 7.0f, 1f);
            Destroy(shockwave, 0.35f);

            var player = PlayerCombatController.Instance;
            if (player != null && player.IsAlive && Vector2.Distance(transform.position, player.Position) <= 3.5f)
            {
                Vector2 knock = (player.Position - (Vector2)transform.position).normalized;
                if (knock.sqrMagnitude < 0.01f) knock = Vector2.up;
                player.TakeDamage(18f, knock, 5.0f);
            }
        }

        /// <summary>
        /// Kỹ năng 1 của Alien Warlord: Tia Phân Rã Laser (Disintegration Beam).
        /// Tụ năng lượng 0.6s, bắn tia laser xuyên thấu gây 160% sát thương và làm chậm 30%.
        /// </summary>
        private IEnumerator ExecuteDisintegrationBeamRoutine()
        {
            isCastingSpecialSkill = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            PlayerInteractionController.Instance?.ShowFeedback("<color=#06b6d4><b>[CẢNH BÁO: TIA PHÂN RÃ LASER!]</b> Chúa Tể Alien đang tụ năng lượng!</color>");

            // Chữ cảnh báo nổi
            GameObject popup = new GameObject("BeamTelegraphText");
            popup.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "⚠️ TIA PHÂN RÃ!";
            textMesh.fontSize = 5.0f;
            textMesh.color = new Color(0.2f, 0.95f, 1f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4f, 1f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            // Tia telegraph cảnh báo nhấp nháy 0.6s
            var player = PlayerCombatController.Instance;
            Vector2 targetPos = (player != null) ? player.Position : (Vector2)transform.position + Vector2.down * 4f;
            Vector2 beamDir = (targetPos - (Vector2)transform.position).normalized;

            GameObject telegraphLine = new GameObject("Alien_BeamTelegraph");
            telegraphLine.transform.position = transform.position;
            var srLine = telegraphLine.AddComponent<SpriteRenderer>();
            srLine.sprite = UISpriteLoader.GetFrameSlotNormal();
            srLine.color = new Color(0.2f, 0.95f, 1f, 0.35f);
            srLine.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder - 1 : 150;
            float angle = Mathf.Atan2(beamDir.y, beamDir.x) * Mathf.Rad2Deg;
            telegraphLine.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            telegraphLine.transform.localScale = new Vector3(10f, 0.15f, 1f);

            float elapsed = 0f;
            while (elapsed < 0.60f)
            {
                elapsed += Time.deltaTime;
                if (player != null && player.IsAlive)
                {
                    beamDir = (player.Position - (Vector2)transform.position).normalized;
                    angle = Mathf.Atan2(beamDir.y, beamDir.x) * Mathf.Rad2Deg;
                    if (telegraphLine != null)
                    {
                        telegraphLine.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        float alpha = Mathf.PingPong(elapsed * 8f, 0.4f) + 0.3f;
                        srLine.color = new Color(0.2f, 0.95f, 1f, alpha);
                    }
                }
                yield return null;
            }
            if (telegraphLine != null) Destroy(telegraphLine);

            // Bắn tia laser phân rã thực thụ
            float beamDamage = CombatFormulas.CalculateDisintegrationBeamDamage(Model.AttackDamage);

            GameObject beamVisual = new GameObject("Alien_LaserBeam");
            beamVisual.transform.position = (Vector2)transform.position + beamDir * 5.0f;
            var srBeam = beamVisual.AddComponent<SpriteRenderer>();
            srBeam.sprite = UISpriteLoader.GetIconEnergyBolt() ?? UISpriteLoader.GetFrameSlotNormal();
            srBeam.color = new Color(0.3f, 0.95f, 1f, 0.95f);
            srBeam.sortingOrder = 550;
            beamVisual.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            beamVisual.transform.localScale = new Vector3(10f, 0.65f, 1f);
            Destroy(beamVisual, 0.25f);

            // Kiểm tra trúng người chơi bằng khoảng cách tới đoạn thẳng
            if (player != null && player.IsAlive)
            {
                Vector2 toPlayer = player.Position - (Vector2)transform.position;
                float proj = Vector2.Dot(toPlayer, beamDir);
                if (proj > 0f && proj <= 10.0f)
                {
                    Vector2 closestPt = (Vector2)transform.position + beamDir * proj;
                    if (Vector2.Distance(player.Position, closestPt) <= 0.95f)
                    {
                        player.TakeDamage(beamDamage, beamDir, 4.0f);
                        PlayerMobileController.Instance?.ApplySlow(0.30f, 2.0f);
                        PlayerInteractionController.Instance?.ShowFeedback("<color=#06b6d4><b>[TRÚNG TIA PHÂN RÃ!]</b> Bạn bị thiêu đốt và giảm 30% tốc độ di chuyển!</color>");
                    }
                }
            }

            // Quét tia qua hàng rào
            foreach (var f in FindObjectsByType<Fence2DView>())
            {
                if (f != null && !f.IsBroken)
                {
                    Vector2 toFence = (Vector2)f.transform.position - (Vector2)transform.position;
                    float proj = Vector2.Dot(toFence, beamDir);
                    if (proj > 0f && proj <= 10.0f)
                    {
                        Vector2 closestPt = (Vector2)transform.position + beamDir * proj;
                        if (Vector2.Distance(f.transform.position, closestPt) <= 0.8f)
                        {
                            f.TakeDamage(beamDamage * 1.5f);
                        }
                    }
                }
            }

            alienBeamCooldown = isAlienOverloaded ? 3.5f : 7.0f;
            isCastingSpecialSkill = false;
        }

        /// <summary>
        /// Kỹ năng 2 của Alien Warlord: Tốc Biến Không Gian (Blink / Tactical Teleport).
        /// Dịch chuyển 4.5m - 6.0m để giữ cự ly bắn tia khi người chơi áp sát (< 1.8m) hoặc khi khiên vỡ.
        /// </summary>
        private IEnumerator ExecuteAlienTeleportRoutine()
        {
            isCastingSpecialSkill = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            GameObject popup = new GameObject("TeleportText");
            popup.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "🌀 TỐC BIẾN!";
            textMesh.fontSize = 5.2f;
            textMesh.color = new Color(0.3f, 0.9f, 1f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4f, 1f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            // Sóng xung kích biến mất
            GameObject warpOut = new GameObject("Alien_WarpOut");
            warpOut.transform.position = transform.position;
            var srOut = warpOut.AddComponent<SpriteRenderer>();
            srOut.sprite = UISpriteLoader.GetFrameSlotNormal();
            srOut.color = new Color(0.2f, 0.9f, 1f, 0.7f);
            srOut.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 1 : 200;
            warpOut.transform.localScale = new Vector3(3.5f, 3.5f, 1f);
            Destroy(warpOut, 0.25f);

            // Mờ biến hình
            if (SpriteRenderer != null) SpriteRenderer.color = new Color(1f, 1f, 1f, 0.15f);

            yield return new WaitForSeconds(0.15f);

            // Tính vị trí tốc biến mới (4.5m - 6.0m so với người chơi)
            var player = PlayerCombatController.Instance;
            Vector2 playerPos = (player != null) ? player.Position : (Vector2)transform.position;
            Vector2 awayDir = ((Vector2)transform.position - playerPos).normalized;
            if (awayDir.sqrMagnitude < 0.01f) awayDir = UnityEngine.Random.insideUnitCircle.normalized;

            // Xoay góc ngẫu nhiên một chút (+/- 35 độ) để tránh đường thẳng nhàm chán
            float rotAngle = UnityEngine.Random.Range(-35f, 35f);
            awayDir = Quaternion.Euler(0f, 0f, rotAngle) * awayDir;

            float blinkDist = UnityEngine.Random.Range(4.5f, 6.0f);
            Vector2 newPos = playerPos + awayDir * blinkDist;
            newPos.x = Mathf.Clamp(newPos.x, -14.0f, 14.0f);
            newPos.y = Mathf.Clamp(newPos.y, -8.0f, 6.5f);

            transform.position = newPos;

            // Sóng xung kích xuất hiện lại
            GameObject warpIn = new GameObject("Alien_WarpIn");
            warpIn.transform.position = newPos;
            var srIn = warpIn.AddComponent<SpriteRenderer>();
            srIn.sprite = UISpriteLoader.GetFrameSlotNormal();
            srIn.color = new Color(0.2f, 0.9f, 1f, 0.7f);
            srIn.sortingOrder = (SpriteRenderer != null) ? SpriteRenderer.sortingOrder + 1 : 200;
            warpIn.transform.localScale = new Vector3(4.0f, 4.0f, 1f);
            Destroy(warpIn, 0.25f);

            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = isAlienOverloaded ? new Color(0.6f, 0.95f, 1f, 1f) : Color.white;
            }

            alienTeleportCooldown = isAlienOverloaded ? 4.0f : 8.0f;
            isCastingSpecialSkill = false;
        }

        /// <summary>
        /// Kỹ năng 3 của Alien Warlord: Quá Tải Lõi Năng Lượng Phase 2 (HP <= 30%).
        /// Tăng +35% tốc độ, hồi khiên plasma 50%, giảm 50% hồi chiêu Tia Phân Rã & Tốc Biến, miễn nhiễm đẩy lùi.
        /// </summary>
        private IEnumerator TriggerAlienOverloadRoutine()
        {
            PlayerInteractionController.Instance?.ShowFeedback("<color=#a855f7><b>[QUÁ TẢI LÕI NĂNG LƯỢNG!]</b> Lõi của Chúa Tể Alien quá tải: Tăng +35% tốc độ, hồi 50% Khiên Plasma và giảm 50% hồi chiêu!</color>");

            GameObject popup = new GameObject("AlienOverloadPopup");
            popup.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "⚡ QUÁ TẢI LÕI!";
            textMesh.fontSize = 5.6f;
            textMesh.color = new Color(0.8f, 0.3f, 1f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4.5f, 1.2f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            // Tăng tốc độ vĩnh viễn
            ApplySpeedBuff(1.35f, 9999f);
            Model.AttackCooldown = 1.0f;

            // Hồi 50% khiên plasma
            PlasmaShieldHp = MaxPlasmaShieldHp * 0.50f;
            if (plasmaShieldVisualObj != null)
            {
                plasmaShieldVisualObj.SetActive(true);
                if (plasmaShieldSr != null) plasmaShieldSr.color = new Color(0.8f, 0.35f, 1f, 0.65f); // Tím neon quá tải
            }
            if (plasmaShieldBarFill != null)
            {
                plasmaShieldBarFill.SetActive(true);
                plasmaShieldBarFill.transform.localScale = new Vector3(1.16f * 0.50f, 0.045f, 1f);
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Kỹ năng 1 của Mẫu Hạm UFO: Đại Pháo Quỹ Đạo Plasma (Orbital Plasma Barrage).
        /// Bắn 3 quả cầu plasma quỹ đạo rơi từ trên cao xuống vị trí người chơi với vòng telegraph 1.0s, nổ AoE và tạo vũng lửa plasma 2.0s.
        /// </summary>
        private IEnumerator ExecuteOrbitalPlasmaBarrageRoutine()
        {
            isCastingSpecialSkill = true;
            PlayerInteractionController.Instance?.ShowFeedback("<color=#d946ef><b>[CẢNH BÁO: ĐẠI PHÁO QUỸ ĐẠO!]</b> Mẫu Hạm UFO đang nạp loạt 3 quả cầu plasma oanh tạc!</color>");

            float baseDamage = Model != null ? Model.AttackDamage : 40f;
            float barrageDmg = CombatFormulas.CalculateOrbitalPlasmaBarrageDamage(baseDamage);

            var player = PlayerCombatController.Instance;

            for (int i = 0; i < 3; i++)
            {
                if (!IsAlive) break;

                Vector2 strikeTarget = (player != null && player.IsAlive)
                    ? player.Position + (player.Velocity * (0.35f * (i + 1)))
                    : (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-4f, 2f));

                strikeTarget.x = Mathf.Clamp(strikeTarget.x, -13.5f, 13.5f);
                strikeTarget.y = Mathf.Clamp(strikeTarget.y, -7.5f, 5.0f);

                StartCoroutine(SingleOrbitalStrikeRoutine(strikeTarget, barrageDmg));

                yield return new WaitForSeconds(0.6f);
            }

            mothershipOrbitalCooldown = 8.0f;
            isCastingSpecialSkill = false;
        }

        private IEnumerator SingleOrbitalStrikeRoutine(Vector2 targetPos, float damage)
        {
            // Telegraph đỏ-tím trên mặt đất
            GameObject telegraph = new GameObject("OrbitalTelegraph");
            telegraph.transform.position = targetPos;
            var sr = telegraph.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameSlotNormal();
            sr.color = new Color(0.85f, 0.15f, 0.95f, 0.35f);
            sr.sortingOrder = 90;
            telegraph.transform.localScale = new Vector3(3.6f, 3.6f, 1f);

            float elapsed = 0f;
            while (elapsed < 1.0f)
            {
                elapsed += Time.deltaTime;
                if (sr != null)
                {
                    float alpha = Mathf.PingPong(elapsed * 8f, 0.45f) + 0.25f;
                    sr.color = new Color(0.85f, 0.15f, 0.95f, alpha);
                }
                yield return null;
            }
            if (telegraph != null) Destroy(telegraph);

            // Quả cầu plasma rơi từ trên cao xuống
            GameObject bolt = new GameObject("OrbitalPlasmaBolt");
            bolt.transform.position = targetPos + new Vector2(0f, 6.0f);
            var srBolt = bolt.AddComponent<SpriteRenderer>();
            srBolt.sprite = UISpriteLoader.GetIconEnergyBolt() ?? UISpriteLoader.GetFrameSlotNormal();
            srBolt.color = new Color(0.95f, 0.2f, 1f, 1f);
            srBolt.sortingOrder = 600;
            bolt.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

            float fallElapsed = 0f;
            Vector3 startFall = bolt.transform.position;
            Vector3 endFall = targetPos;
            while (fallElapsed < 0.16f)
            {
                fallElapsed += Time.deltaTime;
                if (bolt != null) bolt.transform.position = Vector3.Lerp(startFall, endFall, fallElapsed / 0.16f);
                yield return null;
            }
            if (bolt != null) Destroy(bolt);

            // Vụ nổ Plasma AoE bán kính 1.8m
            GameObject explosion = new GameObject("PlasmaExplosion");
            explosion.transform.position = targetPos;
            var srExp = explosion.AddComponent<SpriteRenderer>();
            srExp.sprite = UISpriteLoader.GetFrameSlotNormal();
            srExp.color = new Color(0.95f, 0.25f, 1f, 0.85f);
            srExp.sortingOrder = 550;
            explosion.transform.localScale = new Vector3(4.0f, 4.0f, 1f);
            Destroy(explosion, 0.3f);

            var player = PlayerCombatController.Instance;
            if (player != null && player.IsAlive && Vector2.Distance(targetPos, player.Position) <= 1.8f)
            {
                Vector2 knock = (player.Position - targetPos).normalized;
                if (knock.sqrMagnitude < 0.01f) knock = Vector2.up;
                player.TakeDamage(damage, knock, 4.0f);
                PlayerInteractionController.Instance?.ShowFeedback("<color=#d946ef><b>[TRÚNG ĐẠI PHÁO QUỸ ĐẠO!]</b> Sóng xung kích plasma hất văng bạn!</color>");
            }

            // Quét bay hàng rào trong tâm nổ
            foreach (var f in FindObjectsByType<Fence2DView>())
            {
                if (f != null && !f.IsBroken && Vector2.Distance(targetPos, f.transform.position) <= 1.8f)
                {
                    f.TakeDamage(damage * 1.5f);
                }
            }

            // Để lại vũng plasma 2.0s gây sát thương duy trì nếu người chơi bước vào
            GameObject pool = new GameObject("PlasmaFirePool");
            pool.transform.position = targetPos;
            var srPool = pool.AddComponent<SpriteRenderer>();
            srPool.sprite = UISpriteLoader.GetFrameSlotNormal();
            srPool.color = new Color(0.85f, 0.2f, 1f, 0.45f);
            srPool.sortingOrder = 95;
            pool.transform.localScale = new Vector3(2.8f, 2.8f, 1f);

            float poolTimer = 2.0f;
            float tickTimer = 0f;
            while (poolTimer > 0f)
            {
                poolTimer -= Time.deltaTime;
                tickTimer -= Time.deltaTime;
                if (tickTimer <= 0f && player != null && player.IsAlive && Vector2.Distance(targetPos, player.Position) <= 1.4f)
                {
                    tickTimer = 0.5f;
                    player.TakeDamage(6f, Vector2.zero, 0f);
                }
                yield return null;
            }
            if (pool != null) Destroy(pool);
        }

        /// <summary>
        /// Kỹ năng 2 của Mẫu Hạm UFO: Thả Kén Tiếp Viện (Alien Drop Pod Spawner).
        /// Mỗi 25s phóng kén đổ bộ xuống đồng cỏ sinh ra 1 UFO Thu Hoạch + 2 Alien Grunt.
        /// </summary>
        private IEnumerator TriggerAlienDropPodRoutine()
        {
            PlayerInteractionController.Instance?.ShowFeedback("<color=#c084fc><b>[THẢ KÉN TIẾP VIỆN!]</b> Mẫu Hạm UFO phóng kén đổ bộ xuống đồng cỏ!</color>");

            GameObject popup = new GameObject("DropPodPopup");
            popup.transform.position = transform.position + new Vector3(0f, 1.6f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "🛸 PHÓNG KÉN ĐỔ BỘ!";
            textMesh.fontSize = 5.2f;
            textMesh.color = new Color(0.8f, 0.3f, 1f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(4.5f, 1f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            yield return new WaitForSeconds(0.4f);

            int day = MobileGameController.Instance?.Engine?.Clock?.CurrentDay ?? 1;
            Vector2 pCenter = new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-1.5f, 1.5f));
            Vector2 p1 = pCenter;
            Vector2 p2 = pCenter + new Vector2(-2.5f, UnityEngine.Random.Range(-0.8f, 0.8f));
            Vector2 p3 = pCenter + new Vector2(2.5f, UnityEngine.Random.Range(-0.8f, 0.8f));

            if (EnemyWaveSpawner.Instance != null)
            {
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.UfoThuHoach, p1, day);
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.AlienGrunt, p2, day);
                EnemyWaveSpawner.Instance.SpawnEnemyAt(EnemyTypeId.AlienGrunt, p3, day);
            }
            else
            {
                var o1 = new GameObject("Pod_UfoThuHoach"); o1.transform.position = p1;
                o1.AddComponent<EnemyAgentView>().Bind(EnemyData.CreateUfoThuHoach(day));

                var o2 = new GameObject("Pod_AlienGrunt_1"); o2.transform.position = p2;
                o2.AddComponent<EnemyAgentView>().Bind(EnemyData.CreateAlienGrunt(day));

                var o3 = new GameObject("Pod_AlienGrunt_2"); o3.transform.position = p3;
                o3.AddComponent<EnemyAgentView>().Bind(EnemyData.CreateAlienGrunt(day));
            }
        }

        /// <summary>
        /// Kỹ năng 3 của Mẫu Hạm UFO: Chùm Kéo Lớn (Mega Tractor Beam - GDD 19.2 / Quest Q10).
        /// Mỗi 35s chiếu chùm kéo khổng lồ bán kính 4.0m hút đàn heo lên trời trong 6.0s. Tấn công Mẫu Hạm sẽ ngắt chùm tia!
        /// </summary>
        private IEnumerator ExecuteMegaTractorBeamRoutine()
        {
            PlayerInteractionController.Instance?.ShowFeedback("<color=#ec4899><b>[CẢNH BÁO: CHÙM KÉO LỚN!]</b> Mẫu Hạm đang chiếu chùm kéo khổng lồ hút đàn heo! Hãy TẤN CÔNG MẪU HẠM để ngắt chùm tia!</color>");

            GameObject popup = new GameObject("MegaBeamPopup");
            popup.transform.position = transform.position + new Vector3(0f, 1.8f, 0f);
            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = "🛸 CHÙM KÉO KHỔNG LỒ!";
            textMesh.fontSize = 5.2f;
            textMesh.color = new Color(1f, 0.2f, 0.8f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;
            textMesh.rectTransform.sizeDelta = new Vector2(5.5f, 1.2f);
            StartCoroutine(FloatingTextRoutine(popup, textMesh));

            // Xác định vị trí chiếu beam (tập trung vào đàn heo)
            Vector2 beamCenter = Vector2.zero;
            int pigCount = 0;
            foreach (var p in FindObjectsByType<PigAgentView>())
            {
                if (p != null && p.PigModel != null && p.PigModel.IsAlive && !p.IsCaptured)
                {
                    beamCenter += (Vector2)p.transform.position;
                    pigCount++;
                }
            }
            if (pigCount > 0) beamCenter /= pigCount;

            activeMegaTractorBeamObj = new GameObject("Mothership_MegaTractorBeam");
            activeMegaTractorBeamObj.transform.position = new Vector3(beamCenter.x, beamCenter.y + 4.5f, 0f);
            var srBeam = activeMegaTractorBeamObj.AddComponent<SpriteRenderer>();
            srBeam.sprite = UISpriteLoader.GetTractorBeamVFX();
            srBeam.sortingOrder = 110;
            srBeam.color = new Color(0.95f, 0.2f, 0.85f, 0.85f);
            activeMegaTractorBeamObj.transform.localScale = new Vector3(8.0f, 9.0f, 1f);

            // Bắt đầu kéo các chú heo trong phạm vi bán kính 4.0m
            megaBeamPulledPigs.Clear();
            foreach (var p in FindObjectsByType<PigAgentView>())
            {
                if (p != null && p.PigModel != null && p.PigModel.IsAlive && !p.IsCaptured)
                {
                    if (Vector2.Distance(beamCenter, p.transform.position) <= 4.0f)
                    {
                        p.AbductByUfo(this.transform);
                        megaBeamPulledPigs.Add(p);
                    }
                }
            }

            // Kéo dài trong 6.0s (nếu không bị người chơi tấn công ngắt)
            float elapsed = 0f;
            while (elapsed < 6.0f)
            {
                elapsed += Time.deltaTime;
                if (activeMegaTractorBeamObj != null)
                {
                    float pulse = 7.5f + Mathf.PingPong(elapsed * 5f, 1.0f);
                    activeMegaTractorBeamObj.transform.localScale = new Vector3(pulse, 9.0f, 1f);
                }
                yield return null;
            }

            // Nếu kéo xong 6.0s mà không bị ngắt -> Đàn heo bị bắt mất vào khoang lái!
            if (megaBeamPulledPigs.Count > 0)
            {
                foreach (var pig in megaBeamPulledPigs)
                {
                    if (pig != null)
                    {
                        pig.OnPigStolenAndLost();
                        Destroy(pig.gameObject);
                    }
                }
                megaBeamPulledPigs.Clear();
                PlayerInteractionController.Instance?.ShowFeedback("<color=#ef4444><b>[MẪU HẠM ĐÃ HÚT HEO!]</b> Chùm kéo lớn đã hoàn tất và bắt cóc đàn heo lên vũ trụ!</color>");
            }

            if (activeMegaTractorBeamObj != null) Destroy(activeMegaTractorBeamObj);
            activeMegaTractorBeamObj = null;
            megaTractorBeamCoroutine = null;
            mothershipMegaBeamTimer = 35.0f;
        }

        /// <summary>
        /// Ngắt Chùm Kéo Lớn khi Mẫu Hạm bị tấn công (GDD 19.2 / Quest Q10).
        /// </summary>
        public void InterruptMegaTractorBeam()
        {
            if (megaTractorBeamCoroutine != null)
            {
                StopCoroutine(megaTractorBeamCoroutine);
                megaTractorBeamCoroutine = null;
            }

            if (activeMegaTractorBeamObj != null)
            {
                Destroy(activeMegaTractorBeamObj);
                activeMegaTractorBeamObj = null;
            }

            foreach (var pig in megaBeamPulledPigs)
            {
                if (pig != null)
                {
                    pig.ReleaseFromCapture(rescuedByPlayer: true);
                }
            }
            megaBeamPulledPigs.Clear();

            PlayerInteractionController.Instance?.ShowFeedback("<color=#22c55e><b>[ĐÃ NGẮT CHÙM KÉO!]</b> Đòn tấn công của bạn đã làm gián đoạn chùm kéo của Mẫu Hạm và giải cứu đàn heo!</color>");
        }

        #endregion
    }
}
