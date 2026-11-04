using System;
using System.Collections;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public enum CharacterFacing
    {
        Down,
        Up,
        Left,
        Right,
        Side
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        [Header("Idle Animation Frames (Authentic Artist Sprites)")]
        public Sprite[] DownIdleFrames;
        public Sprite[] UpIdleFrames;
        public Sprite[] LeftIdleFrames;
        public Sprite[] RightIdleFrames;
        public Sprite[] SideIdleFrames;

        [Header("Single Idle Fallbacks")]
        public Sprite DownIdle;
        public Sprite UpIdle;
        public Sprite LeftIdle;
        public Sprite RightIdle;
        public Sprite SideIdle;

        [Header("Walk Animation Frames (4 frames each)")]
        public Sprite[] DownWalk;
        public Sprite[] UpWalk;
        public Sprite[] LeftWalk;
        public Sprite[] RightWalk;
        public Sprite[] SideWalk;


        [Header("Happy / Level-Up Emote Frames (4 frames)")]
        public Sprite[] HappyFrames;

        [Header("Action Sprite (Rải cám / Chải lông / Sửa rào)")]
        public Sprite ActionSprite;

        [Header("Animation Settings")]
        [Header("Pixel Myth (3D-rendered sheets; overrides the single-PNG frames when set)")]
        public PixelMythSpriteLibrary MythLibrary;
        private PixelMythPlayer myth;

        public float IdleFPS = 4.0f;
        public float WalkFPS = 7.5f;
        public bool PreserveAssignedSprites;

        [Header("Backpack Visual Variation (Từ Sprite Sheet gốc)")]
        [SerializeField] private bool hasBackpack = false;
        public bool HasBackpack
        {
            get => hasBackpack;
            set
            {
                if (hasBackpack != value)
                {
                    hasBackpack = value;
                    ReloadSprites();
                }
            }
        }

        private SpriteRenderer spriteRenderer;
        private PlayerMobileController playerController;
        private Rigidbody2D rb;
        private WeaponSocketController weaponSocketController;
        private CharacterFacing currentFacing = CharacterFacing.Down;
        private float actionTimer = 0f;
        private float happyTimer = 0f;
        private bool isAttacking = false;

        public CharacterFacing CurrentFacing => currentFacing;
        /// <summary>The 3D-rendered sheet player (null on the legacy single-PNG path); gear layers follow its frame.</summary>
        public PixelMythPlayer Myth => myth;
        /// <summary>Story staging (ADR-0027): turn without moving.</summary>
        public void FaceDirection(CharacterFacing facing) => currentFacing = facing;
        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerController = GetComponent<PlayerMobileController>();
            rb = GetComponent<Rigidbody2D>();
            weaponSocketController = GetComponent<WeaponSocketController>();
            if (weaponSocketController == null)
            {
                weaponSocketController = gameObject.AddComponent<WeaponSocketController>();
            }
            if (playerController != null && GetComponent<CharacterGearLayers>() == null) gameObject.AddComponent<CharacterGearLayers>();
            if (MythLibrary != null)
            {
                myth = new PixelMythPlayer(MythLibrary, spriteRenderer);
                return;
            }
            LoadSpritesIfNeeded();
        }

        [Tooltip("A new facing must beat the current axis by this ratio; stops left/up flicker on diagonals.")]
        public float FacingHysteresis = 1.3f;
        /// <summary>Below this travelled speed (m/s) the body is idle even if the stick is pushed (pushing into a wall).</summary>
        public float MinTravelSpeed = 0.35f;
        [Tooltip("Upper limit on speeding up the walk/run cycle; beyond it the legs would blur, so the feet slide instead.")]
        public float MaxStrideRate = 2.5f;

        private void UpdateMyth()
        {
            Vector2 input = playerController != null ? playerController.MoveInput : Vector2.zero;
            Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
            float speed = velocity.magnitude;
            bool moving = speed >= MinTravelSpeed && !isAttacking;
            // Face where the body actually goes; while blocked, face the stick so the player sees the intent.
            Vector2 dirSource = moving ? velocity : input;
            if (dirSource.sqrMagnitude > 0.01f && !isAttacking) currentFacing = ResolveFacing(dirSource, currentFacing, FacingHysteresis);
            if (moving)
            {
                bool run = (playerController != null ? playerController.IsRunning : speed > MovementSpec.WalkMetersPerSecond + 0.2f) && MythLibrary.Has("run");
                myth.SetBase(run ? "run" : "walk");
                // Stride lock: play the cycle at the rate where one loop covers the clip's stride. Sheets that carry
                // stride_m_per_cycle use it; older sheets assume they were drawn for the nominal walk/run speed.
                float natural = myth.BaseClip != null ? myth.BaseClip.NaturalSpeed : 0f;
                if (natural <= 0f) natural = run ? MovementSpec.RunMetersPerSecond : MovementSpec.WalkMetersPerSecond;
                myth.BaseRate = Mathf.Clamp(speed / natural, 0.5f, MaxStrideRate);
            }
            else
            {
                myth.SetBase("idle");
                myth.BaseRate = 1f;
            }
            myth.Tick(Time.deltaTime, currentFacing);
        }

        /// <summary>4-way facing with hysteresis: switch axis only when the other axis clearly dominates.</summary>
        public static CharacterFacing ResolveFacing(Vector2 v, CharacterFacing current, float hysteresis)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y);
            bool currentHorizontal = current == CharacterFacing.Left || current == CharacterFacing.Right || current == CharacterFacing.Side;
            bool horizontal = currentHorizontal ? ax * hysteresis >= ay : ax > ay * hysteresis;
            if (horizontal) return v.x < 0f ? CharacterFacing.Left : CharacterFacing.Right;
            return v.y > 0f ? CharacterFacing.Up : CharacterFacing.Down;
        }

        /// <summary>Swaps the 3D-rendered sheet family at runtime (start-screen An/Khoa choice); facing is kept.</summary>
        public void SetMythLibrary(PixelMythSpriteLibrary library)
        {
            if (library == null || library == MythLibrary) return;
            MythLibrary = library;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            isAttacking = false;
            myth = new PixelMythPlayer(library, spriteRenderer);
        }

        /// <summary>Whistle to the herd (Khoa/An "Huýt sáo"): plays the whistle clip when the family has one.</summary>
        public void TriggerWhistle(Action onEffect = null)
        {
            if (myth != null) myth.PlayOnce("whistle", onEffect);
            else onEffect?.Invoke();
        }

        public void ReloadSprites()
        {
            if (PreserveAssignedSprites) return;
            string folder = hasBackpack ? "PlayerBackpack" : "Player";
            string prefix = hasBackpack ? "player_backpack_" : "player_";

            DownIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_idle_4.png")
            };

            UpIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_idle_4.png")
            };

            LeftIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_idle_4.png")
            };

            RightIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_idle_4.png")
            };

            SideIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_idle_4.png")
            };

            DownIdle = DownIdleFrames[0];
            UpIdle = UpIdleFrames[0];
            LeftIdle = LeftIdleFrames[0];
            RightIdle = RightIdleFrames[0];
            SideIdle = SideIdleFrames[0];

            DownWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}down_walk4.png")
            };

            UpWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}up_walk4.png")
            };

            LeftWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}left_walk4.png")
            };

            RightWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}right_walk4.png")
            };

            SideWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{folder}/{prefix}side_walk4.png")
            };
        }

        private void LoadSpritesIfNeeded()
        {
            if ((DownIdleFrames == null || DownIdleFrames.Length == 0 || DownIdleFrames[0] == null) && DownIdle == null)
            {
                ReloadSprites();
            }

            // 3. Tải Happy Emote Frames
            if (HappyFrames == null || HappyFrames.Length == 0 || HappyFrames[0] == null)
            {
                HappyFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Player/player_happy_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Player/player_happy_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Player/player_happy_3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/Player/player_happy_4.png")
                };
            }

            // 4. Action Sprite
            if (ActionSprite == null) ActionSprite = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Player/player_action.png");
        }

        private void Update()
        {
            if (spriteRenderer == null) return;
            if (myth == null && MythLibrary != null) myth = new PixelMythPlayer(MythLibrary, spriteRenderer);
            if (myth != null)
            {
                UpdateMyth();
                return;
            }

            // 1. Đang tấn công
            if (isAttacking) return;

            // 2. Đang phát Happy Emote
            if (happyTimer > 0f)
            {
                happyTimer -= Time.deltaTime;
                PlayFrames(HappyFrames, 6f);
                return;
            }

            // 3. Đang phát Action tương tác (rải cám, tưới nước)
            if (actionTimer > 0f)
            {
                actionTimer -= Time.deltaTime;
                if (ActionSprite != null)
                {
                    spriteRenderer.sprite = ActionSprite;
                    return;
                }
            }

            // 4. Đang trong tư thế tấn công (giữ nguyên thế đứng hướng tấn công)
            if (isAttacking)
            {
                return;
            }

            // 5. Lấy vector di chuyển
            Vector2 moveVec = Vector2.zero;
            if (playerController != null && playerController.MoveInput.sqrMagnitude > 0.01f)
            {
                moveVec = playerController.MoveInput;
            }
            else if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                moveVec = rb.linearVelocity;
            }

            bool isMoving = moveVec.sqrMagnitude > 0.05f;

            if (isMoving)
            {
                walkAnimTimer += Time.deltaTime;

                spriteRenderer.flipX = false;

                if (Mathf.Abs(moveVec.x) > Mathf.Abs(moveVec.y) * 0.8f)
                {
                    if (moveVec.x < 0f)
                    {
                        currentFacing = CharacterFacing.Left;
                        bool hasLeft = LeftWalk != null && LeftWalk.Length > 0 && LeftWalk[0] != null;
                        spriteRenderer.flipX = !hasLeft && CardinalFacing.MirrorSideSprite(CardinalDirection.West);
                        PlayFramesWithTimer(hasLeft ? LeftWalk : SideWalk, WalkFPS, walkAnimTimer);
                    }
                    else
                    {
                        currentFacing = CharacterFacing.Right;
                        bool hasRight = RightWalk != null && RightWalk.Length > 0 && RightWalk[0] != null;
                        PlayFramesWithTimer(hasRight ? RightWalk : SideWalk, WalkFPS, walkAnimTimer);
                    }
                }
                else if (moveVec.y > 0f)
                {
                    currentFacing = CharacterFacing.Up;
                    PlayFramesWithTimer(UpWalk, WalkFPS, walkAnimTimer);
                }
                else
                {
                    currentFacing = CharacterFacing.Down;
                    PlayFramesWithTimer(DownWalk, WalkFPS, walkAnimTimer);
                }
            }
            else
            {
                walkAnimTimer = 0f;
                spriteRenderer.flipX = false;

                // Khi đứng yên (Idle): Trả về sprite đứng yên chuẩn xác từ Sprite Sheet gốc
                switch (currentFacing)
                {
                    case CharacterFacing.Up:
                        spriteRenderer.sprite = UpIdle != null ? UpIdle : (UpIdleFrames != null && UpIdleFrames.Length > 0 ? UpIdleFrames[0] : null);
                        break;
                    case CharacterFacing.Left:
                        {
                            Sprite left = LeftIdle != null ? LeftIdle : (LeftIdleFrames != null && LeftIdleFrames.Length > 0 ? LeftIdleFrames[0] : null);
                            if (left != null)
                            {
                                spriteRenderer.sprite = left;
                            }
                            else
                            {
                                spriteRenderer.flipX = CardinalFacing.MirrorSideSprite(CardinalDirection.West);
                                spriteRenderer.sprite = SideIdle != null ? SideIdle : (SideIdleFrames != null && SideIdleFrames.Length > 0 ? SideIdleFrames[0] : null);
                            }
                        }
                        break;
                    case CharacterFacing.Right:
                        {
                            Sprite right = RightIdle != null ? RightIdle : (RightIdleFrames != null && RightIdleFrames.Length > 0 ? RightIdleFrames[0] : null);
                            spriteRenderer.sprite = right != null ? right : (SideIdle != null ? SideIdle : (SideIdleFrames != null && SideIdleFrames.Length > 0 ? SideIdleFrames[0] : null));
                        }
                        break;
                    case CharacterFacing.Side:
                        spriteRenderer.sprite = SideIdle != null ? SideIdle : (SideIdleFrames != null && SideIdleFrames.Length > 0 ? SideIdleFrames[0] : null);
                        break;
                    default:
                        spriteRenderer.sprite = DownIdle != null ? DownIdle : (DownIdleFrames != null && DownIdleFrames.Length > 0 ? DownIdleFrames[0] : null);
                        break;
                }
            }
        }

        private float walkAnimTimer = 0f;

        private void PlayFramesWithTimer(Sprite[] frames, float fps, float timer)
        {
            if (frames == null || frames.Length == 0) return;
            int index = (int)(timer * fps) % frames.Length;
            if (frames[index] != null)
            {
                spriteRenderer.sprite = frames[index];
            }
        }

        private void PlayFrames(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return;
            int index = (int)(Time.time * fps) % frames.Length;
            if (frames[index] != null)
            {
                spriteRenderer.sprite = frames[index];
            }
        }

        public void TriggerHappy(float duration = 1.0f)
        {
            happyTimer = duration;
        }

        public void TriggerAction(float duration = 0.35f)
        {
            if (myth != null)
            {
                // The selected wheel tool picks the action clip (whistle for Bạch Vân, otherwise the work gesture).
                var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;
                myth.PlayOnce(tool == StardewToolType.BachVan && MythLibrary.Has("whistle") ? "whistle" : "interact");
                return;
            }
            actionTimer = duration;
            if (ActionSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = ActionSprite;
            }
        }

        private Coroutine combatActionRoutine;

        /// <summary>
        /// Kích hoạt đòn tấn công: Nhân vật quay về hướng tấn công, giữ vững thế thủ và kích hoạt va chạm + VFX tại điểm chạm kinetic.
        /// Bỏ qua animation cơ thể phức tạp, tập trung hoàn toàn vào cơ chế combat chính xác cho nhân vật 64x64.
        /// </summary>
        private int comboStep;
        private float lastBasicAttackAt = -10f;
        /// <summary>Seconds between basic attacks that still continue the 3-hit string.</summary>
        public float ComboWindowSeconds = 1.1f;

        /// <summary>The GDD weapon in the weapon slot (paper-doll / animation lookups), null when unarmed.</summary>
        private static EquipmentItem EquippedWeapon()
        {
            var c = MobileGameController.Instance != null ? MobileGameController.Instance.Engine?.Character : null;
            return c != null ? MartialProgression.Weapon(c) : null;
        }

        /// <summary>Farm work: "feed", "water", "clean", "repair", "gather", "carry", "observe" (falls back to "interact").</summary>
        public void TriggerWork(string kind)
        {
            if (myth != null) { myth.PlayFirst(AnimationClips.Work(kind)); return; }
            TriggerAction();
        }

        /// <summary>One-shot reaction or move from a Core chain (dodge, counter, hit, faint…).</summary>
        public void PlayChain(System.Collections.Generic.IReadOnlyList<string> chain, Action done = null)
        {
            if (myth != null) myth.PlayFirst(chain, null, done);
            else done?.Invoke();
        }

        /// <summary>A martial skill: its own clip, varied by the equipped weapon (Core AnimationClips.Skill).</summary>
        public void PlaySkill(MartialSkillDefinition skill, Vector2 aimDirection, float speedMultiplier, Action onHitFrame, Action onComplete)
        {
            var w = EquippedWeapon();
            var chain = AnimationClips.Skill(skill?.Id, w?.BaseId, w != null ? w.Archetype : WeaponArchetype.None, skill != null ? skill.Shape : SkillShape.Dash);
            PlayCombatChain(chain, aimDirection, speedMultiplier, onHitFrame, onComplete);
        }

        public void PlayCombatAction(CombatActionType actionType, Vector2 aimDirection, float speedMultiplier, Action onHitFrame, Action onComplete, string weaponId = "dao_go")
        {
            if (myth != null)
            {
                // basic attack: weapon-specific 3-hit string (attack_<weapon>_<n> → attack_<class>_<n> → … → attack)
                comboStep = Time.time - lastBasicAttackAt <= ComboWindowSeconds ? comboStep % 3 + 1 : 1;
                lastBasicAttackAt = Time.time;
                var w = EquippedWeapon();
                PlayCombatChain(AnimationClips.BasicAttack(w?.BaseId, w != null ? w.Archetype : WeaponArchetype.None, comboStep),
                    aimDirection, speedMultiplier, onHitFrame, onComplete);
                return;
            }
            if (combatActionRoutine != null)
            {
                StopCoroutine(combatActionRoutine);
            }
            combatActionRoutine = StartCoroutine(CombatActionRoutine(actionType, aimDirection, speedMultiplier, onHitFrame, onComplete));
        }

        private void PlayCombatChain(System.Collections.Generic.IReadOnlyList<string> chain, Vector2 aimDirection, float speedMultiplier, Action onHitFrame, Action onComplete)
        {
            if (combatActionRoutine != null) { StopCoroutine(combatActionRoutine); combatActionRoutine = null; }
            if (myth == null) { onHitFrame?.Invoke(); onComplete?.Invoke(); return; }
            if (aimDirection.sqrMagnitude > 0.0001f) currentFacing = ResolveFacing(aimDirection, currentFacing, 1f);
            isAttacking = true;
            weaponSocketController?.HideWeapon();
            // The hit lands on the clip's effect frame, so damage and the swing are in sync.
            myth.PlayFirst(chain, onHitFrame, () => { isAttacking = false; onComplete?.Invoke(); }, speedMultiplier);
        }

        private IEnumerator CombatActionRoutine(CombatActionType actionType, Vector2 aimDirection, float speedMultiplier, Action onHitFrame, Action onComplete)
        {
            isAttacking = true;

            // 1. Xác định hướng tấn công (Down, Up, Left, Right)
            if (Mathf.Abs(aimDirection.x) > Mathf.Abs(aimDirection.y) * 0.8f)
            {
                currentFacing = aimDirection.x < 0f ? CharacterFacing.Left : CharacterFacing.Right;
            }
            else
            {
                currentFacing = aimDirection.y > 0f ? CharacterFacing.Up : CharacterFacing.Down;
            }

            // 2. Đặt sprite đứng yên theo hướng tấn công (Thế thủ sẵn sàng chuẩn Stardew Valley)
            switch (currentFacing)
            {
                case CharacterFacing.Up:    if (UpIdle != null) spriteRenderer.sprite = UpIdle; break;
                case CharacterFacing.Left:  if (LeftIdle != null) spriteRenderer.sprite = LeftIdle; break;
                case CharacterFacing.Right: if (RightIdle != null) spriteRenderer.sprite = RightIdle; break;
                default:                    if (DownIdle != null) spriteRenderer.sprite = DownIdle; break;
            }

            // Ẩn vũ khí socket nếu có
            if (weaponSocketController != null)
            {
                weaponSocketController.HideWeapon();
            }

            float speed = Mathf.Max(0.2f, speedMultiplier);

            // Phase 1: Micro-windup (0.05s)
            float windupTime = 0.05f / speed;
            yield return new WaitForSeconds(windupTime);

            // Phase 2: Active Hit Frame (Chạm mục tiêu, kích hoạt va chạm + VFX)
            onHitFrame?.Invoke();

            // Phase 3: Recovery / Follow-through (0.12s)
            float recoveryTime = 0.12f / speed;
            yield return new WaitForSeconds(recoveryTime);

            isAttacking = false;
            combatActionRoutine = null;
            onComplete?.Invoke();
        }
    }
}
