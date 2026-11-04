using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Drives an EnemyAgentView with a PixelMyth sprite library (idle/move/attack/hit/death, 4 facings) when one
    /// exists for its type. Runs in LateUpdate so it wins over the legacy single-sprite facing code; the legacy
    /// colour flash and health bar keep working.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMythAnimator : MonoBehaviour
    {
        public PixelMythSpriteLibrary Library;
        public float MoveThreshold = 0.15f;
        public float DeathHoldSeconds = 1.1f;

        private EnemyAgentView view;
        private PixelMythPlayer player;
        private CharacterFacing facing = CharacterFacing.Down;
        private Vector3 lastPos;
        private bool deathStarted;
        private bool frozen;
        private Sprite frozenSprite;

        /// <summary>Library family per enemy type; null = keep the legacy sprites.</summary>
        public static string FamilyFor(EnemyTypeId type)
        {
            switch (type)
            {
                case EnemyTypeId.KeCaoPha: return "crawler";   // Kẻ Cào Phá: the clawing beast
                case EnemyTypeId.KeBatHeo: return "thief";     // Kẻ Bắt Heo: rain-cloaked thief (art pending)
                default: return null;
            }
        }

        public static void AttachIfAvailable(EnemyAgentView view)
        {
            if (view == null || view.Model == null) return;
            var family = FamilyFor(view.Model.TypeId);
            var library = family != null ? PixelMythRegistry.Find(family) : null;
            if (library == null) return;
            var anim = view.GetComponent<EnemyMythAnimator>() ?? view.gameObject.AddComponent<EnemyMythAnimator>();
            anim.Library = library;
            // the clips are drawn at the world's 32 PPU: no legacy per-type scaling, and a low wide body
            view.transform.localScale = Vector3.one;
            var capsule = view.GetComponent<CapsuleCollider2D>();
            if (capsule != null && view.Model.TypeId == EnemyTypeId.KeCaoPha)
            {
                capsule.direction = CapsuleDirection2D.Horizontal;
                capsule.size = new Vector2(1.3f, 0.6f);
                capsule.offset = new Vector2(0f, 0.3f);
            }
        }

        private void Start()
        {
            view = GetComponent<EnemyAgentView>();
            if (view == null || Library == null || view.SpriteRenderer == null) { enabled = false; return; }
            player = new PixelMythPlayer(Library, view.SpriteRenderer);
            player.SetBase("idle");
            view.OnHitTaken += HandleHit;
            view.OnAttackStarted += HandleAttack;
            lastPos = transform.position;
        }

        private void OnDestroy()
        {
            if (view == null) return;
            view.OnHitTaken -= HandleHit;
            view.OnAttackStarted -= HandleAttack;
        }

        private void HandleHit()
        {
            if (player == null || deathStarted || view.IsDying) return;
            player.PlayOnce("hit");
        }

        private void HandleAttack()
        {
            if (player == null || deathStarted) return;
            FaceTowardsPlayerIfClose();
            player.PlayOnce("attack");
        }

        private void FaceTowardsPlayerIfClose()
        {
            var pc = PlayerCombatController.Instance;
            if (pc == null) return;
            Vector2 d = pc.transform.position - transform.position;
            if (d.sqrMagnitude < 9f) facing = FacingOf(d);
        }

        private Sprite LastFrame(string clip)
        {
            var frames = Library.Get(clip)?.For(facing);
            return frames != null && frames.Length > 0 ? frames[frames.Length - 1] : view.SpriteRenderer.sprite;
        }

        private static CharacterFacing FacingOf(Vector2 d)
        {
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x < 0f ? CharacterFacing.Left : CharacterFacing.Right;
            return d.y > 0f ? CharacterFacing.Up : CharacterFacing.Down;
        }

        private void LateUpdate()
        {
            if (player == null) return;
            // legacy hit-flash coroutine resets the sprite after 0.12 s; keep the last death frame on screen
            if (frozen) { if (frozenSprite != null) view.SpriteRenderer.sprite = frozenSprite; return; }
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector2 delta = pos - lastPos;
            lastPos = pos;

            if (view.IsDying && !deathStarted)
            {
                deathStarted = true;
                player.PlayOnce("death", null, () => { frozen = true; frozenSprite = LastFrame("death"); });
            }

            if (!deathStarted && dt > 0f)
            {
                float speed = delta.magnitude / dt;
                if (speed > MoveThreshold)
                {
                    facing = CharacterSpriteAnimator.ResolveFacing(delta, facing, 1.3f);
                    player.SetBase("move");
                }
                else
                {
                    player.SetBase("idle");
                }
            }
            player.Tick(dt, facing);
            // Y-sort with pigs, props and buildings (PixelMythWorldBuilder: order = -y * 100)
            view.SpriteRenderer.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
        }
    }
}
