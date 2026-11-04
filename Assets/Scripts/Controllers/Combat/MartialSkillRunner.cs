using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Optional hooks an enemy can implement so skills reach behaviour that lives inside the enemy AI
    /// (Khiêu Chiến taunt, Phá Giáp armour break, Cứu Nguy / Trói Lưới freeing a carried pig).
    /// </summary>
    public interface ISkillReactive
    {
        void ForceTarget(Transform target, float seconds);
        void ApplyArmorBreak(float fraction, float seconds);
        void ReleaseCapturedPig();
    }

    /// <summary>
    /// Plays the slotted martial skill in the world (GDD 12.7). Core (<see cref="MartialProgression.Cast"/>) checks the slot
    /// pair, cost, cooldown and hợp kích; this component turns the skill's shape into hits, pulls, pushes, roots and zones.
    /// Every shape is aimed: the aim (nearest enemy in reach, else facing) is snapped to one of 8 directions, so a Line
    /// skill sweeps that line and a cone opens along it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MartialSkillRunner : MonoBehaviour
    {
        PlayerMobileController mobile;
        PlayerCombatController combat;
        CharacterSpriteAnimator anim;
        Rigidbody2D rb;

        /// <summary>Chắn Mạng window: a big hit on a pig or structure within <see cref="interceptRadius"/> m is taken by the player.</summary>
        static float interceptUntil = -1f;
        static float interceptRadius;

        void Awake()
        {
            mobile = GetComponent<PlayerMobileController>();
            combat = GetComponent<PlayerCombatController>();
            anim = GetComponent<CharacterSpriteAnimator>();
            rb = GetComponent<Rigidbody2D>();
        }

        static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        public void Cast(CombatSlotType slot)
        {
            if (BackpackPopup.InputBlockedThisFrame) return;
            var e = Engine;
            if (e == null) return;
            var c = e.Character;
            var skill = MartialProgression.Slotted(c, slot);

            // Skills that need a target refuse before paying (no cost spent on nothing).
            if (skill != null && c.IsMartialSlotUnlocked(slot) && MartialProgression.CooldownRemaining(c, slot, Time.time) <= 0f)
            {
                string need = NeedsTarget(skill);
                if (need != null) { Say(need); return; }
            }

            var r = MartialProgression.Cast(c, slot, Time.time);
            Say(r.Message);
            if (!r.Ok || r.Skill == null) return;
            combat?.MarkCombatAction();
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Apply(r, r.Skill, Aim(r.Skill.Range > 0f ? r.Skill.Range * r.RangeMultiplier : 3f));
        }

        static void Say(string msg)
        {
            if (!string.IsNullOrEmpty(msg)) PlayerInteractionController.Instance?.ShowFeedback(msg);
        }

        // ------------------------------------------------------------------ aim
        /// <summary>Snap to 8 directions (45° steps).</summary>
        public static Vector2 Snap8(Vector2 v)
        {
            if (v.sqrMagnitude < 0.0001f) return Vector2.down;
            float a = Mathf.Round(Mathf.Atan2(v.y, v.x) / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        Vector2 Aim(float reach)
        {
            Vector2 dir = mobile != null ? mobile.FacingDirection : Vector2.down;
            var target = NearestEnemy(transform.position, Mathf.Max(reach, 2.5f), null);
            if (target != null) dir = (Vector2)target.transform.position - (Vector2)transform.position;
            dir = Snap8(dir);
            if (mobile != null) mobile.SetFacingDirection(dir);
            return dir;
        }

        // ------------------------------------------------------------------ enemy queries
        public static KillClass Classify(EnemyTypeId t) => CombatRewards.Classify(t);

        /// <summary>"Quái nhỏ": not elite or boss and not a heavy or flying type (GDD 12.7 pushes and pulls only move these).</summary>
        public static bool IsSmall(EnemyAgentView e)
        {
            if (e == null || e.Model == null) return false;
            var t = e.Model.TypeId;
            if (Classify(t) != KillClass.Normal) return false;
            return t != EnemyTypeId.ThietGiapCongThanh && t != EnemyTypeId.ThuBao && !EnemyAgentView.IsFlyingOrOccult(t);
        }

        static EnemyAgentView NearestEnemy(Vector2 from, float radius, System.Func<EnemyAgentView, bool> filter)
        {
            EnemyAgentView best = null;
            float bestD = float.MaxValue;
            foreach (var col in Physics2D.OverlapCircleAll(from, radius))
            {
                var e = col.GetComponent<EnemyAgentView>();
                if (e == null || !e.IsAlive || (filter != null && !filter(e))) continue;
                float d = ((Vector2)e.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        static List<EnemyAgentView> EnemiesIn(Vector2 centre, float radius)
        {
            var list = new List<EnemyAgentView>();
            foreach (var col in Physics2D.OverlapCircleAll(centre, radius))
            {
                var e = col.GetComponent<EnemyAgentView>();
                if (e != null && e.IsAlive && !list.Contains(e)) list.Add(e);
            }
            return list;
        }

        static bool InCone(Vector2 from, Vector2 dir, Vector2 p, float range, float halfAngle)
        {
            Vector2 to = p - from;
            return to.magnitude <= range && Vector2.Angle(dir, to) <= halfAngle;
        }

        static bool OnLine(Vector2 from, Vector2 dir, Vector2 p, float length, float halfWidth)
        {
            Vector2 to = p - from;
            float along = Vector2.Dot(to, dir);
            return along >= -0.3f && along <= length && (to - dir * along).magnitude <= halfWidth;
        }

        /// <summary>Moves an enemy by <paramref name="distance"/> along <paramref name="dir"/>, stopping at walls.</summary>
        public static void Drag(EnemyAgentView e, Vector2 dir, float distance)
        {
            if (e == null || distance <= 0f) return;
            Vector2 from = e.transform.position;
            dir = dir.normalized;
            var hit = Physics2D.Raycast(from, dir, distance, ~0);
            float d = distance;
            if (hit.collider != null && hit.collider.GetComponent<EnemyAgentView>() == null && hit.collider.GetComponent<PlayerCombatController>() == null
                && !hit.collider.isTrigger && hit.distance > 0.05f) d = Mathf.Max(0f, hit.distance - 0.3f);
            var body = e.GetComponent<Rigidbody2D>();
            Vector2 to = from + dir * d;
            if (body != null) body.position = to; else e.transform.position = to;
        }

        float Hit(float mult) => Engine?.Character is CharacterData c ? MartialProgression.WeaponHitDamage(c) * mult : 5f * mult;

        void Strike(EnemyAgentView e, float mult, Vector2 dir, float knock = 2f)
        {
            if (e == null || !e.IsAlive) return;
            e.TakeDamage(Hit(mult), dir, knock);
            if (CombatMarks.IsRooted(e) && lastRootedBonus > 1f) e.TakeDamage(Hit(mult) * (lastRootedBonus - 1f), dir, 0f);
        }

        float lastRootedBonus = 1f;

        void Vfx(CombatActionType type, Vector2 dir)
        {
            if (SlashVFXController.Instance != null)
                SlashVFXController.Instance.SpawnVFX(type, transform.position, dir, anim != null ? anim.CurrentFacing : CharacterFacing.Down);
        }

        void Swing(CombatActionType type, Vector2 dir)
        {
            if (anim != null)
            {
                if (performing != null) anim.PlaySkill(performing, dir, 1.2f, null, null);
                else anim.PlayCombatAction(type, dir, 1.2f, null, null, combat != null && combat.CurrentWeapon != null ? combat.CurrentWeapon.Id : "dao_go");
            }
            Vfx(type, dir);
        }

        // ------------------------------------------------------------------ preconditions
        string NeedsTarget(MartialSkillDefinition s)
        {
            Vector2 me = transform.position;
            switch (s.Shape)
            {
                case SkillShape.Rescue:
                    return NearestEnemy(me, s.Range, e => e.IsCarryingPig) == null ? "Cứu Nguy: không có heo bị bắt trong 10 m." : null;
                case SkillShape.Teleport:
                    return DeployPoint(me, s.Range) == null ? "Tiếp Viện Tốc Hành: không có vùng Triển khai trong 25 m." : null;
                case SkillShape.Pin:
                    return NearestEnemy(me, s.Range + 1f, null) == null ? "Neo Kích: không có tinh anh trong tầm." : null;
                case SkillShape.Pull:
                    return NearestEnemy(me, s.Range * 2f, IsSmall) == null ? "Móc Kéo: không có quái nhỏ trong 6 m." : null;
                case SkillShape.Root:
                case SkillShape.Mark:
                case SkillShape.Execute:
                    return NearestEnemy(me, s.Range + 1f, null) == null ? $"{s.Name}: không có quái trong tầm." : null;
            }
            return null;
        }

        /// <summary>Vùng Triển khai = a deploy-type structure (GDD ch.17 type 6); any built structure stands in until those exist.</summary>
        static Transform DeployPoint(Vector2 from, float radius)
        {
            Transform best = null;
            float bestD = radius * radius;
            foreach (var t in FindObjectsByType<DefenseTower2DView>(FindObjectsSortMode.None))
            {
                float d = ((Vector2)t.transform.position - from).sqrMagnitude;
                if (d <= bestD) { bestD = d; best = t.transform; }
            }
            return best;
        }

        // ------------------------------------------------------------------ shapes
        /// <summary>The skill being performed; Swing plays its own clip (Core AnimationClips.Skill) instead of a plain attack.</summary>
        MartialSkillDefinition performing;

        void Apply(SkillCastResult r, MartialSkillDefinition s, Vector2 dir)
        {
            performing = s;
            Vector2 me = transform.position;
            lastRootedBonus = r.RootedTargetMultiplier;
            switch (s.Shape)
            {
                case SkillShape.Dash:
                    combat?.GrantInvincibility(s.Duration);
                    StartCoroutine(DashRoutine(dir, s.Range, null));
                    Vfx(CombatActionType.Thrust, dir);
                    break;

                case SkillShape.Flurry:
                    StartCoroutine(FlurryRoutine(dir, s.Range, (int)s.Magnitude, r.GuaranteedCritLastHit));
                    break;

                case SkillShape.DashThrough:
                    StartCoroutine(DashRoutine(dir, s.Range, e => { Strike(e, 1f, dir, 1f); e.ApplySlow(1f - s.Magnitude, s.Duration); }));
                    break;

                case SkillShape.Rescue:
                {
                    var carrier = NearestEnemy(me, s.Range, e => e.IsCarryingPig);
                    if (carrier == null) break;
                    Vector2 to = (Vector2)carrier.transform.position - me;
                    StartCoroutine(DashRoutine(to.normalized, Mathf.Max(0f, to.magnitude - 0.8f), null));
                    carrier.GetComponent<ISkillReactive>()?.ReleaseCapturedPig();
                    carrier.ApplySlow(0f, 1f);
                    Strike(carrier, 1f, to.normalized, 3f);
                    break;
                }

                case SkillShape.Teleport:
                {
                    var p = DeployPoint(me, s.Range);
                    if (p == null) break;
                    Vector2 dest = (Vector2)p.position + Vector2.down * 1.2f;
                    if (rb != null) rb.position = dest; else transform.position = dest;
                    SlashVFXController.Instance?.SpawnParryVFX(dest, true);
                    break;
                }

                case SkillShape.Decoy:
                {
                    if (combat != null) combat.FreeDodges = (int)s.Magnitude;
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    var a = SpawnDecoy(me + side * 2f, s.Duration);
                    var b = SpawnDecoy(me - side * 2f, s.Duration);
                    int k = 0;
                    foreach (var e in EnemiesIn(me, 8f))
                        e.GetComponent<ISkillReactive>()?.ForceTarget((k++ % 2 == 0 ? a : b).transform, s.Duration);
                    break;
                }

                case SkillShape.ArmorBreak:
                    Swing(CombatActionType.Smash, dir);
                    foreach (var e in EnemiesIn(me, s.Range + 0.4f))
                        if (InCone(me, dir, e.transform.position, s.Range + 0.4f, 50f))
                        {
                            Strike(e, 1f, dir);
                            e.GetComponent<ISkillReactive>()?.ApplyArmorBreak(s.Magnitude, s.Duration);
                            CombatMarks.Flash(e, new Color(1f, 0.55f, 0.2f), s.Duration);
                        }
                    break;

                case SkillShape.PushCircle:
                    Swing(CombatActionType.Smash, dir);
                    CameraShake2D.Instance?.Shake(0.15f, 0.2f);
                    foreach (var e in EnemiesIn(me, s.Range))
                    {
                        Vector2 out1 = ((Vector2)e.transform.position - me).normalized;
                        Strike(e, 1f, out1, 0f);
                        if (IsSmall(e)) Drag(e, out1, s.Magnitude);
                    }
                    break;

                case SkillShape.LineSweep:
                    Swing(CombatActionType.Slash, dir);
                    foreach (var e in EnemiesIn(me + dir * s.Range * 0.5f, s.Range * 0.5f + 1f))
                        if (OnLine(me, dir, e.transform.position, s.Range, 0.9f))
                        {
                            Strike(e, 1f, dir, 0f);
                            if (IsSmall(e)) Drag(e, dir, s.Magnitude);
                        }
                    break;

                case SkillShape.Counter:
                    SlashVFXController.Instance?.SpawnParryVFX(me, false);
                    break;

                case SkillShape.Pin:
                {
                    var t = NearestEnemy(me, s.Range + 1f, e => CombatRewards.Classify(e.Model.TypeId) != KillClass.Normal)
                            ?? NearestEnemy(me, s.Range + 1f, null);
                    if (t == null) break;
                    bool boss = CombatRewards.Classify(t.Model.TypeId) == KillClass.Boss;
                    float secs = MartialProgression.ControlSeconds(Engine.Character, boss ? s.Magnitude : s.Duration);
                    t.ApplySlow(0f, secs);
                    CombatMarks.Root(t, secs);
                    CombatMarks.Flash(t, new Color(0.6f, 0.8f, 1f), secs);
                    Swing(CombatActionType.Smash, dir);
                    break;
                }

                case SkillShape.Execute:
                {
                    var t = NearestEnemy(me, s.Range + 1f, null);
                    Swing(CombatActionType.Smash, dir);
                    CameraShake2D.Instance?.Shake(0.25f, 0.3f);
                    if (t != null) Strike(t, s.DamageMultiplier, dir, 6f);
                    break;
                }

                case SkillShape.Pull:
                {
                    float reach = s.Range * r.RangeMultiplier;
                    var t = NearestEnemy(me, reach + 0.5f, IsSmall);
                    if (t == null) break;
                    Vector2 to = (Vector2)t.transform.position - me;
                    Drag(t, -to.normalized, Mathf.Max(0f, to.magnitude - 1f));
                    t.ApplySlow(0.3f, 0.6f);
                    Vfx(CombatActionType.Thrust, to.normalized);
                    break;
                }

                case SkillShape.Mark:
                {
                    var t = NearestEnemy(me, s.Range + 1f, null);
                    if (t == null) break;
                    CombatMarks.Mark(t, s.Duration);
                    break;
                }

                case SkillShape.ConeTurn:
                    Vfx(CombatActionType.Slash, dir);
                    foreach (var e in EnemiesIn(me, s.Range))
                        if (InCone(me, dir, e.transform.position, s.Range, s.Magnitude * 0.5f))
                        {
                            Drag(e, dir, 2.5f);
                            e.ApplySlow(0.5f, s.Duration);
                        }
                    break;

                case SkillShape.Root:
                {
                    var t = NearestEnemy(me, s.Range + 1f, null);
                    if (t == null) break;
                    float rootSecs = MartialProgression.ControlSeconds(Engine.Character, s.Duration);
                    t.ApplySlow(0f, rootSecs);
                    CombatMarks.Root(t, rootSecs);
                    CombatMarks.Flash(t, new Color(0.7f, 1f, 0.6f), rootSecs);
                    if (t.Model != null && t.Model.TypeId == EnemyTypeId.UfoThuHoach) t.GetComponent<ISkillReactive>()?.ReleaseCapturedPig();
                    break;
                }

                case SkillShape.Stakes:
                {
                    Vector2 centre = me + dir * s.Range;
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    for (int i = -1; i <= 1; i++) SpawnStake(centre + side * (i * 1.3f), s.Duration);
                    break;
                }

                case SkillShape.SlowField:
                    StartCoroutine(SlowFieldRoutine(me, s.Range, s.Magnitude, s.Duration));
                    break;

                case SkillShape.Stance:
                case SkillShape.Shield:
                case SkillShape.Fortify:
                    SlashVFXController.Instance?.SpawnParryVFX(me, true);
                    break;

                case SkillShape.Taunt:
                    foreach (var e in EnemiesIn(me, s.Range))
                        e.GetComponent<ISkillReactive>()?.ForceTarget(transform, s.Duration);
                    SlashVFXController.Instance?.SpawnParryVFX(me, false);
                    break;

                case SkillShape.Intercept:
                    interceptUntil = Time.time + s.Duration;
                    interceptRadius = s.Range;
                    SlashVFXController.Instance?.SpawnParryVFX(me, true);
                    break;
            }
        }

        // ------------------------------------------------------------------ túi nhanh (GDD 12.5)
        /// <summary>Uses the item in bag position <paramref name="index"/> (F5–F8). Area effects are played here.</summary>
        public void UseQuickItem(int index)
        {
            var e = Engine;
            if (e == null || BackpackPopup.InputBlockedThisFrame) return;
            var kinds = QuickBag.Kinds(e.Character);
            if (index < 0 || index >= kinds.Count) { Say($"Ô túi nhanh {index + 1} trống."); return; }
            var id = kinds[index];
            if (id == QuickItemId.BinhHoiLuc)
            {
                // GDD 12.5: "uống 1 giây, trúng đòn bị ngắt": the bottle is spent only when the drink finishes (C-13)
                if (drinking == null) drinking = StartCoroutine(DrinkRoutine(e));
                return;
            }
            var (ok, msg) = QuickBag.Use(e.Character, id);
            Say(msg);
            if (!ok) return;
            Vector2 me = transform.position;
            switch (id)
            {
                case QuickItemId.BangGac:
                    StartCoroutine(BandageRoutine(e.Character));
                    break;
                case QuickItemId.LuuKhoi:
                {
                    var decoy = new GameObject("LuuKhoi_Lure");
                    decoy.transform.position = me + Random.insideUnitCircle.normalized * 6f;
                    Destroy(decoy, 3.2f);
                    foreach (var en in EnemiesIn(me, 4f)) en.GetComponent<ISkillReactive>()?.ForceTarget(decoy.transform, 3f);
                    SpawnRing(me, 4f, 3f);
                    break;
                }
                case QuickItemId.BomKeo:
                    StartCoroutine(SlowFieldRoutine(me + (mobile != null ? mobile.FacingDirection : Vector2.down) * 2f, 3f, 0.4f, 5f));
                    break;
                case QuickItemId.PhaoSang:
                    foreach (var en in EnemiesIn(me, 12f)) en.Reveal(10f);
                    SlashVFXController.Instance?.SpawnParryVFX(me, true);
                    break;
            }
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
        }

        Coroutine drinking;
        /// <summary>True while Bình Hồi Lực is being drunk (UI can show a 1 s bar).</summary>
        public bool IsDrinking => drinking != null;

        IEnumerator DrinkRoutine(GameEngine e)
        {
            var c = e.Character;
            float guardAtStart = c.CurrentGuard;
            Say("Đang uống Bình Hồi Lực…");
            float t = 0f;
            while (t < QuickBag.DrinkSeconds)
            {
                if (c.CurrentGuard < guardAtStart - 0.01f || c.IsFainted)
                {
                    drinking = null;
                    Say("Trúng đòn: ngắt uống, bình vẫn còn.");
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            drinking = null;
            var (ok, msg) = QuickBag.Use(c, QuickItemId.BinhHoiLuc);
            Say(msg);
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
        }

        IEnumerator BandageRoutine(CharacterData c)
        {
            for (int i = 0; i < 5; i++)
            {
                float mult = Engine != null ? Engine.WeatherStatus.HpRegenMultiplier : 1f; // Nhiễm Xạ ≥ 3 tầng: hồi HP −50% (GDD 7.2)
                c.CurrentGuard = Mathf.Min(c.MaxGuard, c.CurrentGuard + Mathf.Max(1f, 6f * mult)); // không có HP: hồi Chống Chịu
                c.SyncLowGuardStamina();
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                yield return new WaitForSeconds(1f);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) UseQuickItem(0);
            if (Input.GetKeyDown(KeyCode.F6)) UseQuickItem(1);
            if (Input.GetKeyDown(KeyCode.F7)) UseQuickItem(2);
            if (Input.GetKeyDown(KeyCode.F8)) UseQuickItem(3);
        }

        /// <summary>
        /// Chắn Mạng (GDD 12.7): while active, one big hit aimed at a pig or structure within 4 m of the player is taken by
        /// the player instead. Returns true when the caller must drop its own damage.
        /// </summary>
        public static bool TryInterceptForAlly(Vector2 allyPosition, float damage, Vector2 hitDirection)
        {
            if (Time.time > interceptUntil) return false;
            var p = PlayerCombatController.Instance;
            if (p == null || Vector2.Distance(p.transform.position, allyPosition) > interceptRadius) return false;
            interceptUntil = -1f;
            p.TakeDamage(damage, hitDirection, 0f);
            PlayerInteractionController.Instance?.ShowFeedback("Chắn Mạng: đỡ thay một đòn!");
            return true;
        }

        IEnumerator DashRoutine(Vector2 dir, float distance, System.Action<EnemyAgentView> onPass)
        {
            if (mobile != null) mobile.SpeedMultiplier = 0f;
            var hitOnce = new HashSet<EnemyAgentView>();
            float travelled = 0f, speed = 30f;
            Vector2 start = transform.position;
            // stop at solid scenery (fence, building), pass through monsters
            float maxD = distance;
            foreach (var h in Physics2D.RaycastAll(start, dir, distance))
            {
                if (h.collider == null || h.collider.isTrigger || h.collider.gameObject == gameObject) continue;
                if (h.collider.GetComponent<EnemyAgentView>() != null || h.collider.GetComponent<PigAgentView>() != null) continue;
                maxD = Mathf.Min(maxD, Mathf.Max(0f, h.distance - 0.4f));
            }
            while (travelled < maxD)
            {
                float step = Mathf.Min(speed * Time.deltaTime, maxD - travelled);
                travelled += step;
                Vector2 pos = start + dir * travelled;
                if (rb != null) rb.position = pos; else transform.position = pos;
                if (onPass != null)
                    foreach (var e in EnemiesIn(pos, 1f))
                        if (hitOnce.Add(e)) onPass(e);
                yield return null;
            }
            if (mobile != null) mobile.SpeedMultiplier = 1f;
        }

        IEnumerator FlurryRoutine(Vector2 dir, float reach, int hits, bool critLast)
        {
            for (int i = 0; i < hits; i++)
            {
                Swing(CombatActionType.Slash, dir);
                Vector2 me = transform.position;
                foreach (var e in EnemiesIn(me, reach + 0.4f))
                {
                    if (!InCone(me, dir, e.transform.position, reach + 0.4f, 65f)) continue;
                    bool last = i == hits - 1;
                    float mult = last && critLast ? 2f : 1f; // Thủ Chuyển Công: nhát cuối chắc chắn chí mạng
                    Strike(e, mult, dir, last ? 4f : 0.5f);
                    if (last && IsSmall(e)) Drag(e, dir, 1.5f); // nhát 3 hất quái nhỏ khỏi heo
                }
                yield return new WaitForSeconds(0.12f);
            }
        }

        IEnumerator SlowFieldRoutine(Vector2 centre, float radius, float slow, float seconds)
        {
            var ring = SpawnRing(centre, radius, seconds);
            float until = Time.time + seconds;
            var inside = new HashSet<EnemyAgentView>();
            while (Time.time < until)
            {
                foreach (var e in EnemiesIn(centre, radius + 1.5f))
                {
                    Vector2 off = (Vector2)e.transform.position - centre;
                    if (off.magnitude <= radius) { inside.Add(e); e.ApplySlow(slow, 0.3f); }
                    else if (inside.Contains(e)) Drag(e, -off.normalized, off.magnitude - radius + 0.1f); // không rời vùng
                }
                yield return new WaitForSeconds(0.2f);
            }
            if (ring != null) Destroy(ring);
        }

        GameObject SpawnDecoy(Vector2 at, float seconds)
        {
            var go = new GameObject("VoAnhBo_Decoy");
            go.transform.position = at;
            var mine = GetComponent<SpriteRenderer>();
            var sr = go.AddComponent<SpriteRenderer>();
            if (mine != null) { sr.sprite = mine.sprite; sr.sortingLayerID = mine.sortingLayerID; sr.sortingOrder = mine.sortingOrder; }
            sr.color = new Color(0.7f, 0.85f, 1f, 0.55f);
            Destroy(go, seconds);
            return go;
        }

        static Sprite square;
        static Sprite Square()
        {
            if (square == null) square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return square;
        }

        static void SpawnStake(Vector2 at, float seconds)
        {
            var go = new GameObject("DungChot_Stake");
            go.transform.position = at;
            go.transform.localScale = new Vector3(0.35f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square();
            sr.color = new Color(0.45f, 0.3f, 0.16f, 1f);
            sr.sortingOrder = Mathf.RoundToInt(-at.y * 100f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            Destroy(go, seconds);
        }

        static GameObject SpawnRing(Vector2 centre, float radius, float seconds)
        {
            var go = new GameObject("ThienLa_Zone");
            go.transform.position = centre;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 40;
            lr.widthMultiplier = 0.12f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(0.55f, 0.85f, 1f, 0.8f);
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
            Destroy(go, seconds + 0.1f);
            return go;
        }
    }
}
