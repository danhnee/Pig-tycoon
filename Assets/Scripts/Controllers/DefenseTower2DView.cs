using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// One placed structure (every family except Bàn Chông, which has <see cref="SpikeTrap2DView"/>). Behaviour comes
    /// from its GDD 14–17 data row (<see cref="DefenseStructureSpecs"/>): info structures warn/reveal, generators power
    /// towers within 12 m, attack towers use their own damage/cadence/range (Nỏ pierce + pin, Hồ Quang chain,
    /// Skyhook flak, Thiên Lôi charge), barriers block and absorb, traps fire their own effect, deploy zones only mark.
    /// Durability follows GDD 18 (wear per use, per 100 HP taken, Mòn/Nguy kịch).
    /// </summary>
    public class DefenseTower2DView : MonoBehaviour, IDamageable
    {
        private static readonly List<DefenseTower2DView> registry = new List<DefenseTower2DView>();
        public static IReadOnlyList<DefenseTower2DView> Registry => registry;

        public DefenseBuildingInstance BuildingModel { get; private set; }
        public StructureSpec Spec { get; private set; }

        [Header("Health & Durability")]
        public float MaxHp = 500f;
        public float CurrentHp = 500f;
        public bool IsAlive => CurrentHp > 0f && (BuildingModel == null || !BuildingModel.IsBroken);
        public Vector2 Position => transform.position;

        [Header("2D Visual Elements")]
        public SpriteRenderer BaseSprite;
        public SpriteRenderer TurretSprite;
        public Transform MuzzlePoint;

        [Header("Damage Visuals")]
        private GameObject smokeEffectObj;
        private SpriteRenderer smokeSr;

        [Header("Targeting & Range")]
        public float RangeRadius = 7.0f;
        public LayerMask MonsterLayer;

        [Header("Bạch Vân Charge Effect")]
        public GameObject ChargingAura2D;

        // Bạch Vân area effects shared by all structures
        private static float thienNhanUntil;
        private static float vanLinhUntil;
        private static Vector2 vanLinhCenter;
        private static float vanLinhRadius;

        private float shootCooldownTimer;
        private int shotsSinceReload;
        private float fireStreak;          // Hồ Quang continuous-fire time
        private float lastShotAt = -99f;
        private float overheatRestUntil;
        private int thunderCharge;         // Đài Thiên Lôi
        private float trapRearm;
        private float rechargeUntil;       // Máy Cộng Hưởng busy
        private bool alarmRaised;
        private float charging = -1f;      // Cửu Tiêu Lôi Pháo countdown
        private Vector2 chargeZone;
        private float lastCooldownSeen;
        private Collider2D bodyCollider;

        private static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
        private static bool WaveActive => EnemyWaveSpawner.Instance != null && EnemyWaveSpawner.Instance.IsWaveActive;

        private void OnEnable()
        {
            if (!registry.Contains(this)) registry.Add(this);
            EnemyAgentView.AnyDied += OnMonsterDied;
            EnemyAgentView.AnySpawned += OnMonsterSpawned;
        }

        private void OnDisable()
        {
            registry.Remove(this);
            EnemyAgentView.AnyDied -= OnMonsterDied;
            EnemyAgentView.AnySpawned -= OnMonsterSpawned;
        }

        public void Bind(DefenseBuildingInstance model)
        {
            BuildingModel = model;
            Spec = model != null ? DefenseBranches.For(model.BlueprintId, model.ChosenUpgradeBranch) : null;
            appliedBranch = model?.ChosenUpgradeBranch ?? "";
            bodyCollider = GetComponent<Collider2D>();
            if (model != null)
            {
                CurrentHp = model.CurrentHp;
                MaxHp = model.MaxHp;
                if (!model.HasPosition)
                {
                    model.PosX = transform.position.x;
                    model.PosY = transform.position.y;
                    model.HasPosition = true;
                }
                lastCooldownSeen = model.CurrentCooldownRemaining;
            }
            if (Spec != null && Spec.RangeM > 0f) RangeRadius = Spec.RangeM;
            UpdateDamageVisual();
        }

        // Domain commands own changes to ammunition and repairs; refresh presentation after they succeed.
        public void SynchronizeFromModel()
        {
            if (BuildingModel == null) return;
            if (BuildingModel.ChosenUpgradeBranch != appliedBranch)
            {
                // GDD 16.1: the chosen branch changes how the tower fights from now on
                appliedBranch = BuildingModel.ChosenUpgradeBranch ?? "";
                Spec = DefenseBranches.For(BuildingModel.BlueprintId, appliedBranch);
                if (Spec != null && Spec.RangeM > 0f) RangeRadius = Spec.RangeM;
                if (Spec != null && Spec.AmmoCapacity > 0) BuildingModel.AmmoCurrent = Mathf.Min(BuildingModel.AmmoCurrent, Spec.AmmoCapacity);
            }
            CurrentHp = BuildingModel.CurrentHp;
            MaxHp = BuildingModel.MaxHp;
            UpdateDamageVisual();
        }

        // ------------------------------------------------------------------ queries used by monsters
        public static bool IsUnderLightningRod(Vector2 pos)
        {
            var e = Engine;
            foreach (var t in registry)
            {
                if (t == null || t.BuildingModel == null || t.BuildingModel.BlueprintId != "CotThuLoi" || t.Spec == null) continue;
                if (e != null && !e.Defense.IsOperational(t.BuildingModel)) continue;
                if (Vector2.Distance(pos, t.transform.position) <= t.Spec.RangeM) return true;
            }
            return false;
        }

        public static bool IsInDeployZone(Vector2 pos)
        {
            foreach (var t in registry)
            {
                if (t == null || t.Spec == null || t.Spec.Role != StructureRole.Deploy || t.BuildingModel == null || t.BuildingModel.IsBroken) continue;
                if (Vector2.Distance(pos, t.transform.position) <= t.Spec.RangeM) return true;
            }
            return false;
        }

        private static bool CooledBy(Vector2 pos)
        {
            var e = Engine;
            foreach (var t in registry)
            {
                if (t == null || t.BuildingModel == null || t.BuildingModel.BlueprintId != "BomLamMat" || t.Spec == null) continue;
                if (e != null && !e.Defense.IsOperational(t.BuildingModel)) continue;
                if (Vector2.Distance(pos, t.transform.position) <= t.Spec.RangeM) return true;
            }
            return false;
        }

        private bool InVanLinh => Time.time < vanLinhUntil && Vector2.Distance(transform.position, vanLinhCenter) <= vanLinhRadius;
        private float EffectiveRange => RangeRadius * (Time.time < thienNhanUntil ? 1f + DefenseStructureSpecs.ThienNhanRangeBonus : 1f);

        // ------------------------------------------------------------------ frame
        private void Update()
        {
            if (BuildingModel == null) return;
            float dt = Time.deltaTime;

            // GDD 13.3 rule 7: every structure has its own cadence; Vạn Linh doubles cooldown recovery.
            // A jump of the cooldown means TriggerBachVanManual just fired (HUD button / dev tools).
            if (BuildingModel.CurrentCooldownRemaining > lastCooldownSeen + 0.5f) OnManualActivated();
            if (BuildingModel.CurrentCooldownRemaining > 0f)
                BuildingModel.CurrentCooldownRemaining = Mathf.Max(0f, BuildingModel.CurrentCooldownRemaining - dt * (InVanLinh ? 2f : 1f));
            lastCooldownSeen = BuildingModel.CurrentCooldownRemaining;
            if (ChargingAura2D != null) ChargingAura2D.SetActive(BuildingModel.IsBachVan && BuildingModel.CurrentCooldownRemaining > 0f);

            if (charging >= 0f) TickCuuTieuCharge(dt);
            if (BuildingModel.IsBroken) { UpdateDamageVisual(); return; }

            var engine = Engine;
            bool operational = engine == null || engine.Defense.IsOperational(BuildingModel);
            TintPower(operational);
            if (Spec == null) return;
            if (!WaveActive) { alarmRaised = false; thunderCharge = 0; }
            if (BuildingModel.BlueprintId == "CongGiaCo") UpdateGate();
            if (!operational) return;

            switch (Spec.Role)
            {
                case StructureRole.Attack:
                    if (!Spec.Manual) UpdateAttack(dt);
                    break;
                case StructureRole.ScoutInfo:
                    UpdateScout();
                    break;
                case StructureRole.Support:
                    if (BuildingModel.BlueprintId == "DenPhaCongNghiep") RevealAround(Spec.RangeM, 0.25f, stealthOnly: true);
                    break;
                case StructureRole.Trap:
                    UpdateTrap(dt);
                    break;
            }
        }

        private void TintPower(bool operational)
        {
            if (BaseSprite == null || CurrentHp < MaxHp * 0.5f) return;
            var state = DefenseDurability.StateOf(BuildingModel.Durability);
            Color c = !operational ? new Color(0.55f, 0.6f, 0.75f, 1f)
                    : state == DurabilityState.NguyKich ? new Color(1f, 0.6f, 0.5f, 1f)
                    : state == DurabilityState.Mon ? new Color(1f, 0.9f, 0.75f, 1f)
                    : Color.white;
            if (BuildingModel.BlueprintId == "CongGiaCo" && bodyCollider != null && !bodyCollider.enabled) c.a = 0.55f;
            BaseSprite.color = c;
        }

        // ------------------------------------------------------------------ targeting
        private bool Targetable(EnemyAgentView e)
        {
            if (e == null || !e.IsAlive || e.Model == null || e.IsStealthed) return false;
            if (Spec != null && !DefenseStructureSpecs.CanTarget(Spec, e.Model.TypeId) && !(Spec.Targets == StructureTargets.Ground && e.IsPulledLow)) return false;
            // Chế độ "Chỉ tinh anh": chỉ bắn Quái Tinh Anh, UFO hoặc Boss
            if (BuildingModel != null && BuildingModel.FireMode == "ChiTinhAnh")
                return CombatRewards.Classify(e.Model.TypeId) != KillClass.Normal || e.Model.TypeId.ToString().StartsWith("Ufo");
            return true;
        }

        /// <summary>GDD 13 "Tiết kiệm": fire only when at least 3 targets are in range, or the target is attacking / carrying a pig.</summary>
        private bool EconomyModeAllows(EnemyAgentView target, float range)
        {
            if (BuildingModel == null || BuildingModel.FireMode != "TietKiem") return true;
            if (target.IsThreateningPig) return true;
            Vector2 me = transform.position;
            int inRange = 0;
            foreach (var m in EnemyAgentView.Registry)
                if (m != null && m.IsAlive && ((Vector2)m.transform.position - me).sqrMagnitude <= range * range) inRange++;
            return inRange >= 3;
        }

        /// <summary>Nearest valid monster in range; monsters marked by the player's skills come first (GDD 12: 5 s priority).</summary>
        private EnemyAgentView FindTarget(float range)
        {
            EnemyAgentView best = null, bestMarked = null;
            float bestD = range * range, bestMarkedD = range * range;
            Vector2 me = transform.position;
            foreach (var e in EnemyAgentView.Registry)
            {
                if (!Targetable(e)) continue;
                float d = ((Vector2)e.transform.position - me).sqrMagnitude;
                if (d > range * range) continue;
                if (CombatMarks.IsMarked(e) && d <= bestMarkedD) { bestMarkedD = d; bestMarked = e; }
                if (d < bestD) { bestD = d; best = e; }
            }
            return bestMarked != null ? bestMarked : best;
        }

        // ------------------------------------------------------------------ attack towers (GDD 16)
        private void UpdateAttack(float dt)
        {
            if (zones.Count > 0) TickZones();
            if ((BuildingModel.ChosenUpgradeBranch ?? "") != appliedBranch) SynchronizeFromModel();
            if (shootCooldownTimer > 0f) shootCooldownTimer -= dt * (InVanLinh ? 2f : 1f);
            if (Time.time < overheatRestUntil) return;

            if (BuildingModel.BlueprintId == "DaiThienLoi") { UpdateThunder(); return; }

            var target = FindTarget(EffectiveRange);
            if (target == null || !EconomyModeAllows(target, EffectiveRange)) return;
            Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            if (TurretSprite != null) TurretSprite.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            if (shootCooldownTimer > 0f || BuildingModel.AmmoCurrent < Mathf.Max(1, Spec.AmmoPerUse)) return;

            var state = DefenseDurability.StateOf(BuildingModel.Durability);
            shootCooldownTimer = Spec.CooldownS * DefenseDurability.CooldownMultiplier(state);
            if (Random.value < DefenseDurability.JamChance(state)) return;   // GDD 18 Mòn: 3% bắn kẹt
            float dmg = Spec.Damage * DefenseDurability.DamageMultiplier(state);

            switch (BuildingModel.BlueprintId)
            {
                case "NoXuyenVan": FireCrossbow(target, dir, dmg); break;
                case "ThapHoQuang": FireArc(target, dmg); break;
                case "ThapPhongKhongSkyhook": FireFlak(target, dmg); break;
                default:
                    DefenseFx.Line(MuzzlePos, target.transform.position, Color.white);
                    target.ApplyStructureHit(dmg);
                    break;
            }
            SpendUse();

            if (Spec.MagazineShots > 0 && ++shotsSinceReload >= Spec.MagazineShots)
            {
                shotsSinceReload = 0;
                shootCooldownTimer += Spec.MagazineReloadS;   // Nỏ: mỗi 10 mũi lên dây 4 giây
            }
            if (Spec.OverheatAfterS > 0f)
            {
                // Hồ Quang: 20 s of continuous fire → 6 s rest; a Bơm Làm Mát within 15 m cuts the heat by 40%
                float gap = Time.time - lastShotAt;
                if (gap > Spec.CooldownS * 1.5f + 0.5f) fireStreak = 0f;
                else fireStreak += gap * (CooledBy(transform.position) ? 0.6f : 1f);
                if (fireStreak >= Spec.OverheatAfterS)
                {
                    fireStreak = 0f;
                    overheatRestUntil = Time.time + Spec.OverheatRestS;
                    DefenseFx.Feedback($"<color=#fb923c>{BuildingModel.Name} quá nhiệt — nghỉ {Spec.OverheatRestS:0} giây.</color>");
                }
            }
            lastShotAt = Time.time;
        }

        private Vector2 MuzzlePos => MuzzlePoint != null ? (Vector2)MuzzlePoint.position : (Vector2)transform.position + new Vector2(0f, 0.6f);

        private void SpendUse()
        {
            int use = Mathf.Max(1, Spec.AmmoPerUse);
            BuildingModel.AmmoCurrent = Mathf.Max(0, BuildingModel.AmmoCurrent - use);
            BuildingModel.AmmoUsedThisWave += use;
            if (!InVanLinh) DefenseDurability.ApplyUseWear(BuildingModel, Spec.WearPerUse);
            if (BuildingModel.IsBroken) UpdateDamageVisual();
        }

        /// <summary>Nỏ Xuyên Vân: 40/mũi, pierces 2 along the line; a running monster is pinned 0,5 s.</summary>
        private void FireCrossbow(EnemyAgentView first, Vector2 dir, float dmg)
        {
            Vector2 from = MuzzlePos;
            var hits = new List<EnemyAgentView> { first };
            float firstDist = Vector2.Distance(from, first.transform.position);
            EnemyAgentView second = null; float secondDist = float.MaxValue;
            foreach (var e in EnemyAgentView.Registry)
            {
                if (e == first || !Targetable(e)) continue;
                Vector2 rel = (Vector2)e.transform.position - from;
                float along = Vector2.Dot(rel, dir);
                if (along <= firstDist || along > EffectiveRange) continue;
                if (Mathf.Abs(rel.x * dir.y - rel.y * dir.x) > 0.6f) continue;   // PROVISIONAL line width (ADR-0020)
                if (along < secondDist) { secondDist = along; second = e; }
            }
            if (second != null && Spec.Pierce >= 2) hits.Add(second);
            if (Spec.ChainTargets > 1)
            {
                // Xích Liên: the chained bolt links up to ChainTargets monsters, each within ChainHopM of the last
                var cur = first;
                while (hits.Count < Spec.ChainTargets)
                {
                    EnemyAgentView next = null; float bestD = Spec.ChainHopM;
                    foreach (var e in EnemyAgentView.Registry)
                    {
                        if (hits.Contains(e) || !Targetable(e)) continue;
                        float d = Vector2.Distance(cur.transform.position, e.transform.position);
                        if (d <= bestD) { bestD = d; next = e; }
                    }
                    if (next == null) break;
                    DefenseFx.Line(cur.transform.position, next.transform.position, new Color(0.75f, 0.7f, 0.6f, 1f), 0.1f, 0.04f);
                    hits.Add(next);
                    cur = next;
                }
            }
            DefenseFx.Line(from, hits[hits.Count - 1].transform.position, new Color(0.85f, 0.75f, 0.5f, 1f), 0.1f, 0.05f);
            bool bind = Spec.BindEveryNth > 0 && ++boltsFired % Spec.BindEveryNth == 0;
            foreach (var e in hits)
            {
                bool running = e.IsMoving;
                e.ApplyStructureHit(dmg);
                if (!e.IsAlive) continue;
                if (bind) e.Stun(Spec.BindSeconds);
                else if (running && Spec.PinSeconds > 0f) e.ApplySlow(0f, Spec.PinSeconds);
            }
            if (Spec.FireZoneM > 0f) AddZone(first.transform.position, Spec.FireZoneM, Spec.FireZoneS, air: false, new Color(1f, 0.45f, 0.1f, 0.9f));
        }

        // ---- GDD 16 / 16.1 area zones: Hỏa Tiễn fire (ground cannot cross), Skyhook debris screen (UFOs fly around)
        private sealed class Zone { public Vector2 C; public float R; public float Until; public bool Air; public float NextRing; public Color Col; }
        private readonly List<Zone> zones = new List<Zone>();
        private int boltsFired;
        private string appliedBranch = "";

        private void AddZone(Vector2 c, float r, float seconds, bool air, Color col)
        {
            if (r <= 0f || seconds <= 0f) return;
            zones.Add(new Zone { C = c, R = r, Until = Time.time + seconds, Air = air, Col = col });
        }

        private void TickZones()
        {
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                var z = zones[i];
                if (Time.time >= z.Until) { zones.RemoveAt(i); continue; }
                if (Time.time >= z.NextRing) { z.NextRing = Time.time + 0.5f; DefenseFx.Ring(z.C, z.R, z.Col, 0.45f); }
                foreach (var e in EnemyAgentView.Registry)
                    if (e != null && e.IsAlive && e.IsAir == z.Air && !(z.Air && e.IsPulledLow)) e.KeepOutOf(z.C, z.R);
            }
        }

        /// <summary>Tháp Hồ Quang: 70, bounces to 4 monsters ≤ 4 m apart (+2 hops when a glued monster is in the chain).</summary>
        private void FireArc(EnemyAgentView first, float dmg)
        {
            var hit = new HashSet<EnemyAgentView>();
            Vector2 from = MuzzlePos;
            var cur = first;
            int maxHops = Spec.ChainTargets;
            while (cur != null && hit.Count < maxHops)
            {
                hit.Add(cur);
                if (cur.IsGlued) maxHops = Spec.ChainTargets + Spec.ChainBonusOnGlued;
                DefenseFx.Line(from, cur.transform.position, new Color(0.5f, 0.85f, 1f, 1f), 0.12f, 0.07f);
                from = cur.transform.position;
                cur.ApplyStructureHit(dmg);
                if (Spec.StunChance > 0f && cur.IsAlive && Random.value < Spec.StunChance) cur.Stun(Spec.StunSeconds);
                EnemyAgentView next = null; float bestD = Spec.ChainHopM;
                foreach (var e in EnemyAgentView.Registry)
                {
                    if (hit.Contains(e) || !Targetable(e)) continue;
                    float d = Vector2.Distance(from, e.transform.position);
                    if (d <= bestD) { bestD = d; next = e; }
                }
                cur = next;
            }
        }

        /// <summary>Skyhook: 180 per volley, 4 m burst, air only.</summary>
        private void FireFlak(EnemyAgentView target, float dmg)
        {
            Vector2 c = target.transform.position;
            DefenseFx.Line(MuzzlePos, c, new Color(1f, 0.9f, 0.4f, 1f), 0.12f, 0.06f);
            DefenseFx.Ring(c, Spec.SplashM, new Color(1f, 0.6f, 0.2f, 0.9f), 0.3f);
            foreach (var e in new List<EnemyAgentView>(EnemyAgentView.Registry))
                if (Targetable(e) && Vector2.Distance(c, e.transform.position) <= Spec.SplashM)
                {
                    e.ApplyStructureHit(dmg);
                    if (e.IsAlive && Spec.GroundedSeconds > 0f) e.PullLow(Spec.GroundedSeconds);
                }
            // GDD 16: "Màn mảnh" — a debris screen UFOs must fly around (Mảnh Vụn: 6 m / 6 s)
            AddZone(c, Spec.ScreenM, Spec.ScreenS, air: true, new Color(0.85f, 0.8f, 0.6f, 0.7f));
        }

        private void OnMonsterDied(EnemyAgentView e)
        {
            if (BuildingModel == null || BuildingModel.BlueprintId != "DaiThienLoi" || e == null) return;
            if (Vector2.Distance(e.transform.position, transform.position) <= EffectiveRange) thunderCharge++;
        }

        /// <summary>Đài Thiên Lôi: +1 charge per kill in 30 m; at 40 charge and ≥ 6 targets, 8 bolts × 450 on the highest HP.</summary>
        private void UpdateThunder()
        {
            if (BuildingModel.CurrentCooldownRemaining > 0f || thunderCharge < Spec.ChargeToFire || BuildingModel.AmmoCurrent < 1) return;
            var targets = new List<EnemyAgentView>();
            foreach (var e in EnemyAgentView.Registry)
                if (Targetable(e) && Vector2.Distance(transform.position, e.transform.position) <= EffectiveRange) targets.Add(e);
            if (targets.Count < Spec.MinTargets) return;
            targets.Sort((a, b) => b.HpCurrent.CompareTo(a.HpCurrent));
            var state = DefenseDurability.StateOf(BuildingModel.Durability);
            float dmg = Spec.Damage * DefenseDurability.DamageMultiplier(state);
            for (int i = 0; i < Mathf.Min(Spec.Bolts, targets.Count); i++)
            {
                DefenseFx.Line((Vector2)targets[i].transform.position + new Vector2(0f, 8f), targets[i].transform.position, new Color(0.8f, 0.85f, 1f, 1f), 0.25f, 0.12f);
                targets[i].ApplyStructureHit(dmg);
            }
            thunderCharge = 0;
            BuildingModel.CurrentCooldownRemaining = Spec.CooldownS * DefenseDurability.CooldownMultiplier(state);
            lastCooldownSeen = BuildingModel.CurrentCooldownRemaining;
            SpendUse();
            DefenseFx.Feedback("<color=#a5b4fc>ĐÀI THIÊN LÔI phóng 8 tia sét!</color>");
        }

        // ------------------------------------------------------------------ Bạch Vân (manual, GDD 13.1/14.1/15/16.2)
        private void OnManualActivated()
        {
            if (Spec == null) return;
            var e = Engine;
            if (e != null && e.Defense.LastBachVanActivatedId != BuildingModel.InstanceId) return;   // failed: fuel lost only
            if (e != null) e.Defense.LastBachVanActivatedId = "";
            switch (BuildingModel.BlueprintId)
            {
                case "ThienNhanVongDai":
                    thienNhanUntil = Time.time + Spec.RevealSeconds;
                    foreach (var m in EnemyAgentView.Registry) if (m != null) m.Reveal(Spec.RevealSeconds);
                    DefenseFx.Feedback("<color=#fde68a>THIÊN NHÃN: lộ toàn bộ quái 60 giây, công trình +20% tầm.</color>");
                    break;
                case "VanLinhHoiNguyenTran":
                    vanLinhUntil = Time.time + Spec.EffectSeconds;
                    vanLinhCenter = transform.position;
                    vanLinhRadius = Spec.RangeM;
                    DefenseFx.Ring(transform.position, Spec.RangeM, new Color(0.6f, 1f, 0.7f, 0.8f), 1.5f);
                    DefenseFx.Feedback("<color=#86efac>VẠN LINH HỒI NGUYÊN TRẬN: 20 giây công trình không mất độ bền, hồi chiêu gấp đôi.</color>");
                    break;
                case "CuuTieuLoiPhao":
                {
                    // GDD 16.2: the zone is chosen now and the shot lands 20 s later — aim where the Boss will be
                    EnemyAgentView best = null;
                    foreach (var m in EnemyAgentView.Registry)
                        if (m != null && m.IsAlive && (best == null || m.HpCurrent > best.HpCurrent)) best = m;
                    Vector2 zone = best != null ? (Vector2)best.transform.position : (Vector2)transform.position + new Vector2(0f, 10f);
                    Vector2 off = zone - (Vector2)transform.position;
                    if (off.magnitude > Spec.RangeM) zone = (Vector2)transform.position + off.normalized * Spec.RangeM;
                    chargeZone = zone;
                    charging = Spec.ManualChargeS;
                    DefenseFx.Feedback("<color=#fbbf24>CỬU TIÊU LÔI PHÁO truyền năng 20 giây vào vùng đã chọn!</color>");
                    break;
                }
            }
        }

        private void TickCuuTieuCharge(float dt)
        {
            float before = charging;
            charging -= dt;
            if (Mathf.Floor(before) != Mathf.Floor(charging)) DefenseFx.Ring(chargeZone, Spec != null ? Spec.SplashM : 5f, new Color(1f, 0.8f, 0.2f, 0.9f), 0.5f);
            if (charging > 0f) return;
            charging = -1f;
            if (Spec == null) return;
            EnemyAgentView main = null;
            var inZone = new List<EnemyAgentView>();
            Vector2 from = transform.position;
            Vector2 lineDir = (chargeZone - from).sqrMagnitude > 0.01f ? (chargeZone - from).normalized : Vector2.up;
            foreach (var m in EnemyAgentView.Registry)
            {
                if (m == null || !m.IsAlive) continue;
                if (Spec.LinePierce)
                {
                    // Xuyên Tâm: a 60 m line from the cannon through the zone; width = the normal blast radius
                    Vector2 rel = (Vector2)m.transform.position - from;
                    float along = Vector2.Dot(rel, lineDir);
                    if (along < 0f || along > Spec.RangeM || Mathf.Abs(rel.x * lineDir.y - rel.y * lineDir.x) > Spec.SplashM) continue;
                }
                else if (Vector2.Distance(chargeZone, m.transform.position) > Spec.SplashM) continue;
                inZone.Add(m);
                if (main == null || m.HpCurrent > main.HpCurrent) main = m;
            }
            float mult = DefenseDurability.DamageMultiplier(DefenseDurability.StateOf(BuildingModel.Durability));
            DefenseFx.Line(chargeZone + new Vector2(0f, 12f), chargeZone, new Color(1f, 0.9f, 0.3f, 1f), 0.4f, 0.4f);
            foreach (var m in inZone) m.ApplyStructureHit((Spec.LinePierce || m == main ? Spec.Damage : Spec.SplashDamage) * mult);
            DefenseFx.Feedback(inZone.Count > 0 ? $"<color=#fbbf24>CỬU TIÊU LÔI PHÁO khai hỏa trúng {inZone.Count} mục tiêu!</color>"
                                                : "<color=#fbbf24>Cửu Tiêu Lôi Pháo khai hỏa — vùng trống!</color>");
            // GDD 16.2: pigs within 25 m lose 12 Tinh thần
            foreach (var p in FindObjectsByType<PigAgentView>())
                if (p != null && p.PigModel != null && Vector2.Distance(chargeZone, p.transform.position) <= 25f) MonsterPigDamage.Morale(p.PigModel, 12);
        }

        // ------------------------------------------------------------------ information (GDD 14.1)
        private void OnMonsterSpawned(EnemyAgentView e)
        {
            if (BuildingModel == null || Spec == null || BuildingModel.IsBroken || e == null || e.Model == null) return;
            var engine = Engine;
            if (engine != null && !engine.Defense.IsOperational(BuildingModel)) return;
            if (BuildingModel.BlueprintId == "RadarUyHiep")
            {
                if (BuildingModel.CurrentCooldownRemaining > 0f || BuildingModel.AmmoCurrent < Spec.AmmoPerUse) return;
                BuildingModel.CurrentCooldownRemaining = Spec.CooldownS;
                lastCooldownSeen = Spec.CooldownS;
                SpendUse();
                // a new sub-wave: reveal every monster for 45 s and report direction + count
                int n = 0; Vector2 sum = Vector2.zero;
                foreach (var m in EnemyAgentView.Registry) if (m != null && m.IsAlive) { m.Reveal(Spec.RevealSeconds); n++; sum += (Vector2)m.transform.position; }
                Vector2 dir = n > 0 ? sum / n - (Vector2)transform.position : Vector2.zero;
                DefenseFx.Feedback($"<color=#38bdf8>RADAR: {n} quái hướng {Compass(dir)} (hiện 45 giây).</color>");
            }
            else if (BuildingModel.BlueprintId == "MayCongHuongHuyenBi")
            {
                var t = e.Model.TypeId;
                bool occult = t == EnemyTypeId.BongHuyen || t == EnemyTypeId.ChoSuong || t == EnemyTypeId.ThucTheSuong;
                if (!occult || Time.time < rechargeUntil || BuildingModel.AmmoCurrent < Spec.AmmoPerUse) return;
                rechargeUntil = Time.time + Spec.CooldownS;
                SpendUse();
                e.Reveal(Spec.RevealSeconds);
                DefenseFx.Ring(e.transform.position, 2f, new Color(0.7f, 0.5f, 1f, 0.9f), 2f);
                DefenseFx.Feedback($"<color=#c4b5fd>MÁY CỘNG HƯỞNG: {e.Model.Name} xuất hiện hướng {Compass((Vector2)e.transform.position - (Vector2)transform.position)} (90 giây).</color>");
            }
        }

        private void UpdateScout()
        {
            if (BuildingModel.BlueprintId != "ThapCanh" || alarmRaised || !WaveActive) return;
            int n = 0; Vector2 sum = Vector2.zero;
            foreach (var m in EnemyAgentView.Registry)
            {
                if (m == null || !m.IsAlive || Vector2.Distance(transform.position, m.transform.position) > Spec.RangeM) continue;
                n++; sum += (Vector2)m.transform.position;
            }
            if (n == 0) return;
            alarmRaised = true;
            DefenseFx.Ring(transform.position, Spec.RangeM, new Color(1f, 0.3f, 0.2f, 0.7f), 1f);
            DefenseFx.Feedback($"<color=#f87171>THÁP CANH BÁO ĐỘNG: {n} quái trong 40 m, hướng {Compass(sum / n - (Vector2)transform.position)}!</color>");
        }

        private void RevealAround(float radius, float seconds, bool stealthOnly)
        {
            foreach (var m in EnemyAgentView.Registry)
            {
                if (m == null || !m.IsAlive) continue;
                if (stealthOnly && m.Model.TypeId != EnemyTypeId.KeCatDay) continue;
                if (Vector2.Distance(transform.position, m.transform.position) <= radius) m.Reveal(seconds);
            }
        }

        private static string Compass(Vector2 d)
        {
            if (d.sqrMagnitude < 0.01f) return "trung tâm";
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (a >= -22.5f && a < 22.5f) return "Đông";
            if (a >= 22.5f && a < 67.5f) return "Đông Bắc";
            if (a >= 67.5f && a < 112.5f) return "Bắc";
            if (a >= 112.5f && a < 157.5f) return "Tây Bắc";
            if (a >= -67.5f && a < -22.5f) return "Đông Nam";
            if (a >= -112.5f && a < -67.5f) return "Nam";
            if (a >= -157.5f && a < -112.5f) return "Tây Nam";
            return "Tây";
        }

        // ------------------------------------------------------------------ barriers (GDD 17 type 4)
        /// <summary>Cổng Gia Cố: closes when a monster is within 15 m, opens for people otherwise.</summary>
        private void UpdateGate()
        {
            if (bodyCollider == null || Spec == null) return;
            bool monsterNear = false;
            foreach (var m in EnemyAgentView.Registry)
                if (m != null && m.IsAlive && Vector2.Distance(transform.position, m.transform.position) <= Spec.RangeM) { monsterNear = true; break; }
            bool shouldClose = monsterNear && !BuildingModel.IsBroken;
            if (bodyCollider.enabled != shouldClose) bodyCollider.enabled = shouldClose;
        }

        // ------------------------------------------------------------------ traps (GDD 17 type 5)
        private void UpdateTrap(float dt)
        {
            if (trapRearm > 0f) { trapRearm -= dt; return; }
            if (BuildingModel.AmmoCurrent < Mathf.Max(1, Spec.AmmoPerUse)) return;
            Vector2 c = transform.position;
            var near = new List<EnemyAgentView>();
            foreach (var m in EnemyAgentView.Registry)
                if (Targetable(m) && Vector2.Distance(c, m.transform.position) <= Spec.TriggerRadiusM) near.Add(m);
            if (near.Count == 0) return;

            switch (BuildingModel.BlueprintId)
            {
                case "TamSocDien":
                    near[0].Stun(Spec.StunSeconds);
                    DefenseFx.Ring(c, 1f, new Color(0.6f, 0.9f, 1f, 1f), 0.3f);
                    break;
                case "HoKeo":
                    foreach (var m in new List<EnemyAgentView>(EnemyAgentView.Registry))
                    {
                        if (!Targetable(m) || Vector2.Distance(c, m.transform.position) > Spec.PullRadiusM) continue;
                        m.ApplySlow(Spec.SlowMultiplier, Spec.SlowSeconds);
                        m.Glue(Spec.SlowSeconds);
                        var body = m.GetComponent<Rigidbody2D>();
                        Vector2 pulled = Vector2.Lerp(m.transform.position, c, 0.8f);
                        if (body != null) body.position = pulled; else m.transform.position = pulled;
                    }
                    DefenseFx.Ring(c, Spec.PullRadiusM, new Color(0.9f, 0.8f, 0.3f, 0.9f), 0.5f);
                    break;
                case "LuoiEmp":
                {
                    bool worth = false;
                    foreach (var m in near) if (m.IsStealthed || m.IsAir) { worth = true; break; }
                    if (!worth)
                    {
                        // stealthed cutters are not "Targetable": look for them directly
                        foreach (var m in EnemyAgentView.Registry)
                            if (m != null && m.IsAlive && m.IsStealthed && Vector2.Distance(c, m.transform.position) <= Spec.RangeM) { worth = true; break; }
                    }
                    if (!worth) return;
                    foreach (var m in new List<EnemyAgentView>(EnemyAgentView.Registry))
                    {
                        if (m == null || !m.IsAlive || Vector2.Distance(c, m.transform.position) > Spec.RangeM) continue;
                        m.Reveal(Spec.RevealSeconds);
                        m.BreakLock();
                    }
                    DefenseFx.Ring(c, Spec.RangeM, new Color(0.4f, 0.8f, 1f, 0.9f), 0.6f);
                    DefenseFx.Feedback("<color=#7dd3fc>LƯỚI EMP: phá tàng hình, UFO mất khóa mục tiêu!</color>");
                    break;
                }
                case "MinNo":
                {
                    if (near.Count < Spec.MinTargets) return;
                    foreach (var m in near) m.ApplyStructureHit(Spec.SplashDamage);
                    foreach (var p in FindObjectsByType<PigAgentView>())
                        if (p != null && p.PigModel != null && Vector2.Distance(c, p.transform.position) <= Spec.PigMoraleRadiusM)
                            MonsterPigDamage.Morale(p.PigModel, Spec.PigMoraleLossInRadius);
                    DefenseFx.Ring(c, Spec.SplashM, new Color(1f, 0.45f, 0.1f, 1f), 0.5f);
                    DefenseFx.Feedback($"<color=#fb923c>MÌN NỔ: {near.Count} quái trúng 500 sát thương!</color>");
                    break;
                }
                default:
                    if (Spec.Damage > 0f) near[0].ApplyStructureHit(Spec.Damage);
                    break;
            }
            trapRearm = DefenseStructureSpecs.TrapRearmSeconds;
            SpendUse();
        }

        // ------------------------------------------------------------------ damage (GDD 18)
        public void TakeDamage(float amount, Vector2 knockbackDirection = default, float knockbackForce = 0f) => TakeStructureDamage(amount, false);

        /// <summary>A monster blow: shield first, 100 HP = −1 durability, heavy blows (elite/Boss/siege) −3.</summary>
        public void TakeStructureDamage(float amount, bool heavyBlow)
        {
            if (!IsAlive || amount <= 0f) return;
            var e = Engine;
            if (BuildingModel != null)
            {
                DefenseDurability.ApplyHit(e?.Defense, BuildingModel, amount, heavyBlow);
                CurrentHp = BuildingModel.CurrentHp;
            }
            else CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            UpdateDamageVisual();
            if (BuildingModel != null && BuildingModel.IsBroken && e != null)
            {
                e.EventBus.Publish(new BuildingDestroyed(BuildingModel.InstanceId, BuildingModel.BlueprintId, e.Clock.CurrentDay));
                DefenseFx.Feedback($"<color=#ef4444>{BuildingModel.Name} đã HỎNG!</color>");
            }
        }

        public void StrikeByLightning()
        {
            if (BuildingModel == null) return;
            DefenseDurability.ApplyLightning(BuildingModel);
            UpdateDamageVisual();
        }

        public void Repair(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            if (BuildingModel != null) BuildingModel.CurrentHp = Mathf.RoundToInt(CurrentHp);
            if (BuildingModel != null && CurrentHp > 0f && BuildingModel.Durability > 0) BuildingModel.IsBroken = false;
            UpdateDamageVisual();
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
                smokeSr.sortingOrder = 220;
                smokeEffectObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                smokeEffectObj.SetActive(false);
            }

            bool barrier = Spec != null && Spec.Role == StructureRole.Barrier;
            if (!IsAlive)
            {
                // Hỏng: stops until repaired; ammo in the structure is kept (GDD 18). A broken barrier no longer blocks.
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                if (BaseSprite != null) BaseSprite.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
                if (TurretSprite != null) TurretSprite.gameObject.SetActive(false);
                if (bodyCollider != null && barrier) bodyCollider.enabled = false;
            }
            else if (CurrentHp < MaxHp * 0.5f)
            {
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.3f, 0.3f, 0.3f, 0.75f);
                if (BaseSprite != null) BaseSprite.color = new Color(0.85f, 0.5f, 0.35f, 1f);
                if (TurretSprite != null) TurretSprite.gameObject.SetActive(true);
            }
            else
            {
                smokeEffectObj.SetActive(false);
                if (BaseSprite != null) BaseSprite.color = Color.white;
                if (TurretSprite != null) TurretSprite.gameObject.SetActive(true);
                if (bodyCollider != null && barrier && BuildingModel != null && BuildingModel.BlueprintId != "CongGiaCo") bodyCollider.enabled = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, RangeRadius);
        }
    }
}
