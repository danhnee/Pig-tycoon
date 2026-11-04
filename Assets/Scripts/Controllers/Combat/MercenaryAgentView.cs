using System.Collections.Generic;
using PigTycoon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Body of a hired mercenary during a wave (GDD 11.2). Stats and target rules come from Core
    /// <see cref="MercenarySpecs"/>; monsters attack it like any other IDamageable (melee mercenaries draw aggro).
    /// Out of Stamina → falls back to the rally point (Cờ Tập Kết if built, else the player) for 8 s.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MercenaryAgentView : MonoBehaviour, IDamageable
    {
        public static readonly List<MercenaryAgentView> Active = new List<MercenaryAgentView>();

        public MercenarySpec Spec { get; private set; }
        float hp, stamina, nextAttackAt, retreatUntil;
        SpriteRenderer sr;
        PixelMythPlayer anim;
        CharacterFacing facing = CharacterFacing.Down;
        EnemyAgentView target;
        const float MoveSpeed = 3.2f;

        public bool IsAlive => hp > 0f;
        public Vector2 Position => transform.position;

        public static MercenaryAgentView Spawn(MercenarySpec spec, Vector2 at)
        {
            var go = new GameObject("Merc_" + spec.Role, typeof(SpriteRenderer));
            go.transform.position = at;
            var v = go.AddComponent<MercenaryAgentView>();
            v.Init(spec);
            return v;
        }

        float bonus; // ADR-0026: hảo cảm ≥ 50/80/100 → +10/20/30% HP và sát thương (không đổi giá)

        void Init(MercenarySpec spec)
        {
            Spec = spec;
            var eng = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            bonus = eng != null ? NpcContracts.EfficiencyBonus(eng, spec.Id) : 0f;
            hp = spec.MaxHp * (1f + bonus);
            stamina = MercenarySpecs.MaxStamina;
            sr = GetComponent<SpriteRenderer>();
            var lib = PixelMythRegistry.Find(spec.SpriteFamily) ?? PixelMythRegistry.Find("npc_mercenary");
            if (lib != null) { anim = new PixelMythPlayer(lib, sr); anim.SetBase("idle"); }
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
            col.isTrigger = true;
            Active.Add(this);
        }

        void OnDestroy() => Active.Remove(this);

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!IsAlive) return;
            hp -= amount;
            transform.position += (Vector3)(knockbackDirection.normalized * Mathf.Min(0.4f, knockbackForce * 0.05f));
            if (hp <= 0f)
            {
                PlayerInteractionController.Instance?.ShowFeedback($"{Spec.Role} gục — rút khỏi trận đợt này.");
                Destroy(gameObject, 0.1f);
            }
        }

        void Update()
        {
            if (Spec == null || !IsAlive) return;
            float dt = Time.deltaTime;
            Vector2 me = transform.position;
            bool moving = false;

            if (Time.time < retreatUntil)
            {
                moving = MoveTo(RallyPoint(), 0.8f, dt);
            }
            else
            {
                if (stamina <= 0f)
                {
                    retreatUntil = Time.time + MercenarySpecs.RetreatSeconds;
                    stamina = MercenarySpecs.MaxStamina; // hồi đầy sau khi rút về (Cờ Tập Kết: hồi +30%)
                    PlayerInteractionController.Instance?.ShowFeedback($"{Spec.Role} hết sức — rút về điểm tập kết {MercenarySpecs.RetreatSeconds:0} giây.");
                }
                if (Spec.Targeting == MercenaryTargeting.StealthAndThieves)
                    foreach (var m in EnemyAgentView.Registry)
                        if (m != null && m.IsAlive && Vector2.Distance(me, m.transform.position) <= MercenarySpecs.RevealMeters) m.Reveal(0.5f);

                if (target == null || !target.IsAlive) target = PickTarget(me);
                if (target != null)
                {
                    Vector2 tp = target.transform.position;
                    float d = Vector2.Distance(me, tp);
                    if (d > Spec.RangeMeters) moving = MoveTo(tp, Spec.RangeMeters * 0.9f, dt);
                    else if (Time.time >= nextAttackAt) Attack(target, tp - me);
                }
                else moving = MoveTo(RallyPoint(), 2.5f, dt);
            }

            if (anim != null)
            {
                anim.SetBase(moving ? "walk" : "idle");
                anim.Tick(dt, facing);
            }
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }

        EnemyAgentView PickTarget(Vector2 me)
        {
            EnemyAgentView best = null, bestPreferred = null;
            float bestD = float.MaxValue, bestPD = float.MaxValue;
            float leash = Mathf.Max(Spec.RangeMeters, 12f);
            foreach (var m in EnemyAgentView.Registry)
            {
                if (m == null || !m.IsAlive || m.Model == null) continue;
                var t = m.Model.TypeId;
                float d = Vector2.Distance(me, m.transform.position);
                if (d > leash) continue;
                bool air = MercenarySpecs.IsAirTarget(t);
                if (Spec.Targeting == MercenaryTargeting.AirOnly && !air) continue;
                if (Spec.Melee && air) continue; // cận chiến không với tới quái bay
                bool preferred = Spec.Targeting switch
                {
                    MercenaryTargeting.PreferElite => CombatRewards.Classify(t) != KillClass.Normal,
                    MercenaryTargeting.StealthAndThieves => MercenarySpecs.IsThief(t),
                    _ => false,
                };
                if (preferred && d < bestPD) { bestPD = d; bestPreferred = m; }
                if (d < bestD) { bestD = d; best = m; }
            }
            return bestPreferred != null ? bestPreferred : best;
        }

        void Attack(EnemyAgentView t, Vector2 dir)
        {
            nextAttackAt = Time.time + MercenarySpecs.AttackIntervalSeconds;
            stamina -= MercenarySpecs.StaminaPerAttack;
            Face(dir);
            anim?.PlayOnce("attack");
            float dmg = MercenarySpecs.HitDamage(Spec, CombatMarks.IsMarked(t)) * (1f + bonus);
            if (!Spec.Melee) DefenseFx.Line((Vector2)transform.position + new Vector2(0f, 0.5f), t.transform.position, new Color(1f, 0.9f, 0.6f));
            t.TakeDamage(dmg, dir.normalized, Spec.Melee ? 1.5f : 0.5f);
            // Thợ Săn UFO "cắt chùm kéo": trúng UFO đang kéo heo → heo rơi xuống an toàn
            if (Spec.Id == NpcSpecialistId.ThoSanUfo && t.Model != null && t.Model.TypeId == EnemyTypeId.UfoThuHoach)
                t.GetComponent<ISkillReactive>()?.ReleaseCapturedPig();
            // cận chiến giữ quái lại: quái quay sang đánh lính (Vệ Binh Khiên chặn cửa)
            if (Spec.Melee && t.IsAlive) t.ForceTarget(transform, Spec.Id == NpcSpecialistId.VeBinhKhien ? 3f : 1.5f);
        }

        bool MoveTo(Vector2 p, float stopAt, float dt)
        {
            Vector2 me = transform.position;
            Vector2 d = p - me;
            if (d.magnitude <= stopAt) return false;
            Face(d);
            transform.position = me + d.normalized * Mathf.Min(d.magnitude - stopAt, MoveSpeed * dt);
            return true;
        }

        void Face(Vector2 d)
        {
            if (d.sqrMagnitude < 0.0001f) return;
            facing = CharacterSpriteAnimator.ResolveFacing(d, facing, 1.3f);
        }

        static Vector2 RallyPoint()
        {
            foreach (var t in DefenseTower2DView.Registry)
                if (t != null && t.BuildingModel != null && t.BuildingModel.BlueprintId == "CoTapKet") return t.transform.position;
            var p = PlayerMobileController.Instance;
            return p != null ? (Vector2)p.transform.position : Vector2.zero;
        }
    }

    /// <summary>Spawns the hired mercenaries when a wave starts on the farm and dismisses them when it ends.</summary>
    public sealed class MercenaryRoster : MonoBehaviour
    {
        static MercenaryRoster instance;
        float scan;
        bool deployed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
            Ensure();
        }

        static void OnScene(Scene s, LoadSceneMode m) => Ensure();

        static void Ensure()
        {
            if (instance != null) return;
            instance = new GameObject("MercenaryRoster").AddComponent<MercenaryRoster>();
            DontDestroyOnLoad(instance.gameObject);
            EnemyAgentView.AnyDied -= CountKill;
            EnemyAgentView.AnyDied += CountKill;
        }

        /// <summary>ADR-0026: quái bị hạ (bởi bất kỳ ai) đếm cho nhiệm vụ riêng của lính (tinh anh / UFO / quái trộm).</summary>
        static void CountKill(EnemyAgentView v)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null || v == null || v.Model == null) return;
            NpcContracts.RecordKill(engine, v.Model.TypeId, CombatRewards.Classify(v.Model.TypeId) != KillClass.Normal);
        }

        void Update()
        {
            if ((scan -= Time.deltaTime) > 0f) return;
            scan = 0.5f;
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            bool onFarm = PixelMythFarmGameplay.Instance != null;
            bool wave = engine != null && onFarm && engine.Clock.IsDuringCombatWave;
            if (wave && !deployed)
            {
                deployed = true;
                var player = PlayerMobileController.Instance;
                Vector2 at = player != null ? (Vector2)player.transform.position : Vector2.zero;
                var duty = MercenarySpecs.OnDuty(engine);
                for (int i = 0; i < duty.Count; i++)
                    MercenaryAgentView.Spawn(duty[i], at + new Vector2(Mathf.Cos(i * 1.3f), Mathf.Sin(i * 1.3f)) * 1.5f);
                if (duty.Count > 0) PlayerInteractionController.Instance?.ShowFeedback($"{duty.Count} lính đánh thuê vào vị trí.");
            }
            else if (!wave && deployed)
            {
                deployed = false;
                foreach (var m in MercenaryAgentView.Active.ToArray()) if (m != null) Destroy(m.gameObject);
            }
        }
    }
}
