using System;
using System.Collections.Generic;
using PigTycoon.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PigTycoon.PlayableDemo3D
{
    public readonly struct FarmRaidAnimationFrame
    {
        public bool Telegraph { get; }
        public bool Attack { get; }
        public bool Hit { get; }
        public bool Bind { get; }
        public bool Death { get; }
        public bool IsDead { get; }
        public bool IsDragging { get; }
        public FarmRaidAnimationFrame(bool telegraph, bool attack, bool hit, bool bind, bool death, bool dead, bool dragging)
        { Telegraph = telegraph; Attack = attack; Hit = hit; Bind = bind; Death = death; IsDead = dead; IsDragging = dragging; }
    }
    public sealed class FarmRaidAnimationCursor
    {
        private int telegraph, attack, hit, bind;
        private bool deathSeen;
        public FarmRaidAnimationFrame Read(FarmRaidEnemy enemy)
        {
            bool dead = !enemy.Model.IsAlive;
            var frame = new FarmRaidAnimationFrame(enemy.TelegraphSequence > telegraph, enemy.AttackSequence > attack,
                enemy.HitSequence > hit, enemy.BindSequence > bind, dead && !deathSeen, dead,
                !dead && !string.IsNullOrEmpty(enemy.CapturedPigId));
            telegraph = enemy.TelegraphSequence; attack = enemy.AttackSequence; hit = enemy.HitSequence; bind = enemy.BindSequence;
            deathSeen |= dead;
            return frame;
        }
    }

    /// <summary>Authored enemy prefabs render the real raid model. No invisible or primitive enemies are spawned.</summary>
    public sealed class PlayableFarmRaidView : MonoBehaviour
    {
        public PlayableFarmController Farm;
        public GameObject ThiefPrefab, CrawlerPrefab;
        public bool ShowCombatHud = true;
        public PlayableFarmRaid Model { get; private set; }
        public IReadOnlyDictionary<string, Transform> EnemyViews => views;
        private readonly Dictionary<string, Transform> views = new Dictionary<string, Transform>();
        private readonly Dictionary<string, FarmRaidAnimationCursor> animationCursors = new Dictionary<string, FarmRaidAnimationCursor>();
        private sealed class DeathVisual { public Transform View; public float Remaining; }
        private readonly List<DeathVisual> deathVisuals = new List<DeathVisual>();
        public int DeathVisualCount => deathVisuals.Count;
        private readonly List<string> removed = new List<string>();
        private readonly RaycastHit[] shotHits = new RaycastHit[32];
        private readonly List<string> piercedIds = new List<string>();
        private static readonly IComparer<RaycastHit> HitOrder = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
        private string message = "";
        private int lastReportNumber;

        private void Start()
        {
            if (Farm == null) Farm = FindAnyObjectByType<PlayableFarmController>();
            if (Farm == null) return;
            Farm.SessionReady += Bind;
            Farm.NavigationRebuilt += Rebuild;
            Farm.PauseChanged += OnPauseChanged;
            if (Farm.Session != null) Bind(Farm.Session);
        }
        private void Bind(PlayableFarmSession session)
        {
            if (Model != null) return;
            Model = new PlayableFarmRaid(session);
            Model.Paused = Farm.IsPaused;
            Model.TowerPosition = id =>
            {
                foreach (var station in Farm.Stations)
                    if (station != null && station.Kind == FarmInteractionKind.Defense && station.CoreInstanceId == id)
                        return (station.transform.position.x, station.transform.position.z);
                return null;
            };
            Model.PlayerRescued += Farm.RescuePlayerToHouse;
            Model.TowerFired += PlayTowerAttack;
            Model.TowerPierceTargets = RaycastTowerShot;
        }
        private void Rebuild() { Model?.RebuildObstacles(); }
        private void OnPauseChanged(bool paused) { if (Model != null) Model.Paused = paused; }
        private sealed class SlashVfxItem
        {
            public GameObject Root;
            public Material Mat;
            public Color BaseColor;
            public float Elapsed;
            public float Duration;
            public float SpinSpeed;
            public Vector3 StartScale;
            public Vector3 EndScale;
        }

        private sealed class FloatingPopupItem
        {
            public Vector3 WorldPos;
            public string Text;
            public Color Color;
            public float Remaining;
            public float Duration;
            public bool IsCritOrMajor;
        }

        private readonly List<SlashVfxItem> activeVfx = new List<SlashVfxItem>();
        private readonly List<FloatingPopupItem> activePopups = new List<FloatingPopupItem>();
        private readonly Dictionary<string, float> lastKnownEnemyHp = new Dictionary<string, float>();
        private readonly Dictionary<string, float> enemyHitFlashTimers = new Dictionary<string, float>();
        private GameObject lockReticleObj;
        private string lockedEnemyId = "";
        private int localComboStep;
        private float dashCooldownTimer;
        private static Texture2D flatWhiteTex;

        public string LockedEnemyId => lockedEnemyId;
        public int ActivePopupCount => activePopups.Count;
        public int ActiveSlashVfxCount => activeVfx.Count;

        private void OnDestroy()
        {
            if (Farm != null)
            {
                Farm.SessionReady -= Bind; Farm.NavigationRebuilt -= Rebuild; Farm.PauseChanged -= OnPauseChanged;
                if (Model != null) Model.PlayerRescued -= Farm.RescuePlayerToHouse;
            }
            if (Model != null) Model.TowerFired -= PlayTowerAttack;
            for (int i = activeVfx.Count - 1; i >= 0; i--)
                if (activeVfx[i].Root != null) Destroy(activeVfx[i].Root);
            activeVfx.Clear();
            if (lockReticleObj != null) Destroy(lockReticleObj);
        }
        private void Update()
        {
            if (Model == null || Farm == null || Farm.Session == null) return;
            Model.Paused = Farm.IsPaused;
            float dt = Time.deltaTime;
            if (dashCooldownTimer > 0f) dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - dt);
            AdvanceCombatVfx(dt);
            if (Farm.IsPaused) return;
            if (!Model.Active && (Farm.Session.State.PendingRaid || Farm.Session.Engine.Clock.IsMinorWaveTriggered) &&
                ThiefPrefab != null && CrawlerPrefab != null)
            {
                foreach (var entry in Farm.Session.Engine.GridZones.MonsterSpawnPoints)
                {
                    var started = Model.TryStartScheduledMinor(entry.x, entry.y, IsVisible);
                    if (started.Success) { message = started.Message; break; }
                }
            }
            Model.Advance(dt, (ax, ay, bx, by) => Farm.CanTraverseActorSegment(ax, ay, bx, by, false));
            AdvanceDeathVisuals(dt);
            RefreshEnemyViews();
            UpdateLockReticle(dt);
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.jKey.wasPressedThisFrame) PracticeOrAttackCombo();
                if (keyboard.kKey.wasPressedThisFrame) TriggerMartialSkill();
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.leftAltKey.wasPressedThisFrame) TriggerParry();
                if (keyboard.cKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasPressedThisFrame) TriggerDashRoll();
                if (keyboard.rKey.wasPressedThisFrame) ToggleTargetLock();
                if (keyboard.vKey.wasPressedThisFrame) SpawnImmediateSkirmish();
            }
            if (Mouse.current != null && !Farm.MouseOverHud() && !MouseOverCombatHud())
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) PracticeOrAttackCombo();
                if (Mouse.current.rightButton.wasPressedThisFrame) TriggerParry();
                if (Mouse.current.middleButton.wasPressedThisFrame) ToggleTargetLock();
            }
#else
            if (Input.GetKeyDown(KeyCode.J)) PracticeOrAttackCombo();
            if (Input.GetKeyDown(KeyCode.K)) TriggerMartialSkill();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftAlt)) TriggerParry();
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl)) TriggerDashRoll();
            if (Input.GetKeyDown(KeyCode.R)) ToggleTargetLock();
            if (Input.GetKeyDown(KeyCode.V)) SpawnImmediateSkirmish();
            if (Input.GetMouseButtonDown(0) && !Farm.MouseOverHud() && !MouseOverCombatHud())
                PracticeOrAttackCombo();
            if (Input.GetMouseButtonDown(1) && !Farm.MouseOverHud() && !MouseOverCombatHud())
                TriggerParry();
            if (Input.GetMouseButtonDown(2))
                ToggleTargetLock();
#endif
            if (Model.LastReport != null && Model.LastReport.WaveNumber != lastReportNumber)
            { lastReportNumber = Model.LastReport.WaveNumber; message = Model.LastReport.SummaryText; Farm.RefreshViews(); }
        }
        private bool MouseOverCombatHud()
        {
            if (!ShowCombatHud) return false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            Vector2 position = Mouse.current.position.ReadValue();
#else
            Vector2 position = Input.mousePosition;
#endif
            return position.x >= Screen.width - 420f && Screen.height - position.y <= 340f;
        }

        public FarmActionResult SpawnImmediateSkirmish()
        {
            if (Model == null || Farm == null || Farm.Session == null)
                return new FarmActionResult(false, "Chưa sẵn sàng triệu hồi.");
            Vector3 fwd = Farm.PlayerRoot != null ? Farm.PlayerRoot.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            float sx = Mathf.Clamp(Farm.Session.PlayerX + fwd.x * 4.5f, -35f, 35f);
            float sy = Mathf.Clamp(Farm.Session.PlayerY + fwd.z * 4.5f, -32f, 28f);
            var res = Model.ForceStartOpenWorldSkirmish(sx, sy);
            message = res.Message;
            RefreshEnemyViews();
            FarmRaidEnemy nearest = FindNearestActiveEnemy();
            if (nearest != null) lockedEnemyId = nearest.Model.Id;
            Farm.ReportExternalAction(res);
            return res;
        }

        public FarmActionResult ToggleTargetLock()
        {
            if (Model == null || !Model.Active)
            {
                lockedEnemyId = "";
                return new FarmActionResult(false, "Không có mục tiêu để khóa tâm.");
            }
            if (!string.IsNullOrEmpty(lockedEnemyId))
            {
                lockedEnemyId = "";
                var unlockRes = new FarmActionResult(true, "Đã hủy khóa mục tiêu (Free Aim).");
                Farm.ReportExternalAction(unlockRes);
                return unlockRes;
            }
            var nearest = FindNearestActiveEnemy();
            if (nearest == null) return new FarmActionResult(false, "Không có mục tiêu trong tầm.");
            lockedEnemyId = nearest.Model.Id;
            var lockRes = new FarmActionResult(true, $"Đã khóa mục tiêu: {nearest.Model.Name} ({nearest.Model.CurrentHp:0} HP)!");
            Farm.ReportExternalAction(lockRes);
            return lockRes;
        }

        public FarmActionResult TriggerDashRoll()
        {
            if (Farm == null || Farm.Session == null || Farm.IsPaused)
                return new FarmActionResult(false, "Chưa thể lướt né.");
            if (dashCooldownTimer > 0f)
                return new FarmActionResult(false, "Đang hồi nhịp lướt né.");
            dashCooldownTimer = 0.55f;
            Vector3 dir = Farm.PlayerRoot != null ? Farm.PlayerRoot.forward : Vector3.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            float stepDist = 2.2f;
            float nx = Farm.Session.PlayerX + dir.x * stepDist;
            float ny = Farm.Session.PlayerY + dir.z * stepDist;
            if (Farm.CanTraverseActorSegment(Farm.Session.PlayerX, Farm.Session.PlayerY, nx, ny, true))
            {
                Farm.Session.SetPlayerPosition(nx, ny);
                if (Farm.PlayerRoot != null)
                    Farm.PlayerRoot.position = new Vector3(nx, Farm.GroundHeight(nx, ny), ny);
            }
            if (Farm.PlayerRoot != null)
            {
                Farm.PlayerRoot.GetComponentInChildren<PlayableFarmActor>()?.TriggerDashVisual(0.28f);
                SpawnSlashVfx(Farm.PlayerRoot.position + Vector3.up * 0.6f, Farm.PlayerRoot.rotation,
                    new Color(0.55f, 0.92f, 1.0f, 0.78f), new Vector3(0.75f, 1.0f, 0.75f), new Vector3(1.25f, 0.65f, 1.25f), 0.24f, 360f);
            }
            Model?.TriggerParryStance();
            var res = new FarmActionResult(true, "Thân Pháp Lướt Né (I-Frames 0.35s) · Tách khỏi vòng vây!");
            message = res.Message;
            Farm.ReportExternalAction(res);
            return res;
        }

        public FarmActionResult PracticeOrAttackCombo()
        {
            if (Model == null || Farm.IsPaused) return new FarmActionResult(false, "Chưa thể ra đòn.");
            localComboStep = (localComboStep % 3) + 1;
            if (Model.Active)
            {
                var atkRes = AttackNearest(Model.HeavyWeaponEquipped);
                if (atkRes.Success) return atkRes;
            }
            if (Farm.PlayerRoot != null)
            {
                var actor = Farm.PlayerRoot.GetComponentInChildren<PlayableFarmActor>();
                actor?.PlayComboAttack(localComboStep - 1, false);
                SpawnWeaponSlashArc(localComboStep - 1, false);
            }
            string wpn = Farm.Session?.EquippedWeaponDisplayName ?? "Dao Rựa";
            var res = new FarmActionResult(true, $"Luyện đòn {wpn} · Chuỗi cận chiến mượt.");
            message = res.Message;
            Farm.ReportExternalAction(res);
            return res;
        }
        public FarmActionResult TriggerMartialSkill()
        {
            if (Model == null || Farm.IsPaused) return new FarmActionResult(false, "Chưa thể thi triển võ kỹ.");
            FarmRaidEnemy nearest = GetLockedOrNearestEnemy();
            if (nearest != null) LungePlayerToward(nearest, 1.55f);
            var res = Model.ExecuteMartialSkillSlash(nearest?.Model.Id);
            message = res.Message;
            if (Farm.PlayerRoot != null)
            {
                if (nearest != null)
                {
                    Vector3 facing = new Vector3(nearest.X - Farm.Session.PlayerX, 0f, nearest.Y - Farm.Session.PlayerY);
                    if (facing.sqrMagnitude > 0.001f) Farm.PlayerRoot.rotation = Quaternion.LookRotation(facing);
                }
                var actor = Farm.PlayerRoot.GetComponentInChildren<PlayableFarmActor>();
                actor?.PlayComboAttack(2, true);
                SpawnWeaponSlashArc(2, true);
                SpawnDamagePopup(Farm.PlayerRoot.position + Vector3.up * 2.05f, "SÁT GIA TRẢM!", new Color(1.0f, 0.78f, 0.22f), true);
            }
            Farm.ReportExternalAction(res);
            return res;
        }
        public FarmActionResult TriggerParry()
        {
            if (Model == null || Farm.IsPaused) return new FarmActionResult(false, "Chưa thể dựng thế thủ.");
            var res = Model.TriggerParryStance();
            message = res.Message;
            if (res.Success && Farm.PlayerRoot != null)
            {
                var actor = Farm.PlayerRoot.GetComponentInChildren<PlayableFarmActor>();
                actor?.TriggerParryVisual(0.48f);
                actor?.PlayInteraction();
                SpawnSlashVfx(Farm.PlayerRoot.position + Vector3.up * 0.95f, Quaternion.identity,
                    new Color(1.0f, 0.85f, 0.24f, 0.85f), new Vector3(0.75f, 1.0f, 0.75f), new Vector3(1.35f, 0.65f, 1.35f), 0.30f, 420f);
                if (res.Message.IndexOf("PHẢN ĐÒN HOÀN HẢO", StringComparison.OrdinalIgnoreCase) >= 0)
                    SpawnDamagePopup(Farm.PlayerRoot.position + Vector3.up * 2.1f, "⚡ PHẢN ĐÒN HOÀN HẢO!", new Color(1.0f, 0.92f, 0.28f), true);
            }
            Farm.ReportExternalAction(res);
            return res;
        }
        private FarmRaidEnemy GetLockedOrNearestEnemy()
        {
            if (Model == null) return null;
            if (!string.IsNullOrEmpty(lockedEnemyId))
            {
                foreach (var enemy in Model.Enemies)
                    if (!enemy.Resolved && enemy.Model.Id == lockedEnemyId) return enemy;
            }
            return FindNearestActiveEnemy();
        }
        private FarmRaidEnemy FindNearestActiveEnemy()
        {
            if (Model == null) return null;
            FarmRaidEnemy nearest = null; float distance = float.PositiveInfinity;
            foreach (var enemy in Model.Enemies)
            {
                if (enemy.Resolved) continue;
                float d = Vector2.Distance(new Vector2(enemy.X, enemy.Y), new Vector2(Farm.Session.PlayerX, Farm.Session.PlayerY));
                if (d < distance) { nearest = enemy; distance = d; }
            }
            return nearest;
        }
        private void LungePlayerToward(FarmRaidEnemy target, float desiredStopDist = 1.45f)
        {
            if (target == null || Farm == null || Farm.Session == null) return;
            Vector2 p = new Vector2(Farm.Session.PlayerX, Farm.Session.PlayerY);
            Vector2 e = new Vector2(target.X, target.Y);
            float dist = Vector2.Distance(p, e);
            if (dist > desiredStopDist && dist <= 5.2f)
            {
                Vector2 dir = (e - p) / dist;
                float step = Mathf.Min(dist - desiredStopDist, 1.45f);
                float nx = p.x + dir.x * step;
                float ny = p.y + dir.y * step;
                if (Farm.CanTraverseActorSegment(p.x, p.y, nx, ny, true))
                {
                    Farm.Session.SetPlayerPosition(nx, ny);
                    if (Farm.PlayerRoot != null)
                        Farm.PlayerRoot.position = new Vector3(nx, Farm.GroundHeight(nx, ny), ny);
                }
            }
        }
        public FarmActionResult AttackNearest(bool heavy)
        {
            if (Model == null || Farm.IsPaused) return new FarmActionResult(false, "Chưa thể ra đòn.");
            FarmRaidEnemy nearest = GetLockedOrNearestEnemy();
            if (nearest == null) return new FarmActionResult(false, "Không có mục tiêu.");
            LungePlayerToward(nearest, heavy ? 1.55f : 1.40f);
            var result = Model.AttackEnemy(nearest.Model.Id, heavy); message = result.Message;
            if (result.Success && Farm.PlayerRoot != null)
            {
                Vector3 facing = new Vector3(nearest.X - Farm.Session.PlayerX, 0f, nearest.Y - Farm.Session.PlayerY);
                if (facing.sqrMagnitude > 0.001f) Farm.PlayerRoot.rotation = Quaternion.LookRotation(facing);
                int step = Mathf.Max(0, Model.CurrentComboStep - 1);
                var actor = Farm.PlayerRoot.GetComponentInChildren<PlayableFarmActor>();
                actor?.PlayComboAttack(step, false);
                SpawnWeaponSlashArc(step, false);
            }
            return result;
        }

        private void SpawnWeaponSlashArc(int step, bool skillSpin)
        {
            if (Farm == null || Farm.PlayerRoot == null) return;
            int wpnIdx = Farm.EquippedWeaponSlotIndex;
            Color col = skillSpin
                ? new Color(1.0f, 0.52f, 0.18f, 0.92f)
                : (wpnIdx == 1
                    ? new Color(1.0f, 0.78f, 0.22f, 0.90f)
                    : (wpnIdx == 2 ? new Color(0.35f, 0.96f, 0.88f, 0.90f) : new Color(0.58f, 0.92f, 1.0f, 0.90f)));

            Vector3 origin = Farm.PlayerRoot.position + Vector3.up * 0.92f + Farm.PlayerRoot.forward * (skillSpin ? 0.15f : 0.45f);
            float tilt = skillSpin ? 0f : (step == 0 ? 14f : (step == 1 ? -18f : 38f));
            Quaternion rot = Farm.PlayerRoot.rotation * Quaternion.Euler(tilt, 0f, tilt);
            Vector3 s0 = skillSpin ? new Vector3(0.95f, 1.0f, 0.95f) : new Vector3(0.85f, 1.0f, 0.85f);
            Vector3 s1 = skillSpin ? new Vector3(1.55f, 0.65f, 1.55f) : new Vector3(1.28f, 0.65f, 1.28f);
            SpawnSlashVfx(origin, rot, col, s0, s1, skillSpin ? 0.36f : 0.24f, skillSpin ? 540f : 280f);
        }

        private void SpawnSlashVfx(Vector3 pos, Quaternion rot, Color col, Vector3 startScale, Vector3 endScale, float duration, float spinSpeed)
        {
            var go = new GameObject("CombatSlashArcVfx");
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = startScale;

            Shader sh = Shader.Find("PigTycoon/OriginalWorld/SurfaceLit3D")
                        ?? Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard");
            var mat = new Material(sh);
            mat.enableInstancing = true;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);

            const int segCount = 7;
            const float arcDeg = 140f;
            const float radius = 0.95f;
            for (int i = 0; i < segCount; i++)
            {
                float u = (float)i / (segCount - 1) - 0.5f;
                float ang = u * arcDeg;
                float rad = ang * Mathf.Deg2Rad;
                float taper = 1f - Mathf.Abs(u) * 1.15f;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = "SlashSeg_" + i;
                var c = seg.GetComponent<Collider>();
                if (c != null) Destroy(c);
                seg.transform.SetParent(go.transform, false);
                seg.transform.localPosition = new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
                seg.transform.localEulerAngles = new Vector3(0f, ang + 90f, 0f);
                seg.transform.localScale = new Vector3(0.055f * taper, 0.024f * taper, 0.26f);
                var r = seg.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            activeVfx.Add(new SlashVfxItem
            {
                Root = go,
                Mat = mat,
                BaseColor = col,
                Elapsed = 0f,
                Duration = Mathf.Max(0.08f, duration),
                SpinSpeed = spinSpeed,
                StartScale = startScale,
                EndScale = endScale
            });
        }

        public void SpawnDamagePopup(Vector3 worldPos, string text, Color color, bool isCritOrMajor = false)
        {
            activePopups.Add(new FloatingPopupItem
            {
                WorldPos = worldPos,
                Text = text,
                Color = color,
                Remaining = isCritOrMajor ? 1.25f : 0.95f,
                Duration = isCritOrMajor ? 1.25f : 0.95f,
                IsCritOrMajor = isCritOrMajor
            });
        }

        private void AdvanceCombatVfx(float dt)
        {
            for (int i = activeVfx.Count - 1; i >= 0; i--)
            {
                var item = activeVfx[i];
                item.Elapsed += dt;
                if (item.Root == null || item.Elapsed >= item.Duration)
                {
                    if (item.Root != null) Destroy(item.Root);
                    activeVfx.RemoveAt(i);
                    continue;
                }
                float t = Mathf.Clamp01(item.Elapsed / item.Duration);
                item.Root.transform.localScale = Vector3.Lerp(item.StartScale, item.EndScale, t);
                item.Root.transform.Rotate(0f, item.SpinSpeed * dt, 0f, Space.Self);
                Color c = Color.Lerp(item.BaseColor, Color.white, (1f - t) * 0.35f);
                if (item.Mat != null)
                {
                    if (item.Mat.HasProperty("_BaseColor")) item.Mat.SetColor("_BaseColor", c);
                    if (item.Mat.HasProperty("_Color")) item.Mat.SetColor("_Color", c);
                }
            }

            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                var p = activePopups[i];
                p.Remaining -= dt;
                p.WorldPos += Vector3.up * (dt * 1.15f);
                if (p.Remaining <= 0f) activePopups.RemoveAt(i);
            }
        }

        private void UpdateLockReticle(float dt)
        {
            FarmRaidEnemy target = null;
            if (Model != null && Model.Active)
                target = GetLockedOrNearestEnemy();

            if (target == null || !views.TryGetValue(target.Model.Id, out var targetTr) || targetTr == null)
            {
                if (lockReticleObj != null && lockReticleObj.activeSelf)
                    lockReticleObj.SetActive(false);
                return;
            }

            if (lockReticleObj == null)
            {
                lockReticleObj = new GameObject("TargetLockReticle");
                Shader sh = Shader.Find("PigTycoon/OriginalWorld/SurfaceLit3D") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var m = new Material(sh);
                Color goldRed = new Color(1.0f, 0.36f, 0.18f, 1f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", goldRed);
                if (m.HasProperty("_Color")) m.SetColor("_Color", goldRed);
                const float r0 = 0.56f;
                for (int i = 0; i < 4; i++)
                {
                    float ang = i * 90f + 45f;
                    float rad = ang * Mathf.Deg2Rad;
                    var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seg.name = "ReticleSeg_" + i;
                    var col = seg.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    seg.transform.SetParent(lockReticleObj.transform, false);
                    seg.transform.localPosition = new Vector3(Mathf.Sin(rad) * r0, 0f, Mathf.Cos(rad) * r0);
                    seg.transform.localEulerAngles = new Vector3(0f, ang + 90f, 0f);
                    seg.transform.localScale = new Vector3(0.05f, 0.02f, 0.32f);
                    var r = seg.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial = m;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
            lockReticleObj.SetActive(true);
            float pulse = 1.0f + 0.10f * Mathf.Sin(Time.time * 7.5f);
            lockReticleObj.transform.position = targetTr.position + Vector3.up * 0.08f;
            lockReticleObj.transform.localEulerAngles = new Vector3(0f, Time.time * 90f, 0f);
            lockReticleObj.transform.localScale = new Vector3(pulse, 1f, pulse);
        }
        private void PlayTowerAttack(string id)
        {
            foreach (var station in Farm.Stations)
                if (station != null && station.CoreInstanceId == id) station.GetComponentInChildren<PlayableFarmActor>()?.PlayAttack();
        }
        private bool IsVisible(float x, float y)
        {
            foreach (var station in Farm.Stations)
                if (station != null && Vector2.Distance(new Vector2(x, y), new Vector2(station.transform.position.x, station.transform.position.z)) < 25f)
                    return true;
            var camera = Farm.GameplayCamera;
            if (camera == null) return true;
            Vector3 screen = camera.WorldToViewportPoint(new Vector3(x, Farm.GroundHeight(x, y), y));
            return screen.z > 0 && screen.x >= 0 && screen.x <= 1 && screen.y >= 0 && screen.y <= 1;
        }
        private void RefreshEnemyViews()
        {
            removed.Clear();
            foreach (var pair in views)
            {
                bool present = false;
                foreach (var enemy in Model.Enemies) if (enemy.Model.Id == pair.Key) { present = true; break; }
                if (!present) removed.Add(pair.Key);
            }
            foreach (string id in removed)
            {
                if (views[id] != null) Destroy(views[id].gameObject);
                views.Remove(id); animationCursors.Remove(id);
                lastKnownEnemyHp.Remove(id); enemyHitFlashTimers.Remove(id);
            }
            foreach (var enemy in Model.Enemies)
            {
                float curHp = enemy.Model.CurrentHp;
                if (lastKnownEnemyHp.TryGetValue(enemy.Model.Id, out float prevHp) && curHp < prevHp - 0.1f)
                {
                    float dmg = prevHp - curHp;
                    Vector3 popPos = new Vector3(enemy.X, Farm.GroundHeight(enemy.X, enemy.Y) + 1.95f, enemy.Y);
                    bool bigHit = dmg >= 35f || !enemy.Model.IsAlive;
                    string txt = !enemy.Model.IsAlive
                        ? $"💥 HẠ GỤC -{dmg:0}! (+{enemy.Model.GoldReward} Vàng)"
                        : (bigHit ? $"🔥 BẠO KÍCH -{dmg:0}!" : $"-{dmg:0}");
                    Color col = !enemy.Model.IsAlive
                        ? new Color(1.0f, 0.90f, 0.25f)
                        : (bigHit ? new Color(1.0f, 0.48f, 0.15f) : new Color(1.0f, 0.28f, 0.28f));
                    SpawnDamagePopup(popPos, txt, col, bigHit);
                    enemyHitFlashTimers[enemy.Model.Id] = 0.18f;
                }
                lastKnownEnemyHp[enemy.Model.Id] = curHp;

                if (enemy.Resolved)
                {
                    if (views.TryGetValue(enemy.Model.Id, out var resolvedView))
                    {
                        float deathDuration = 0f;
                        if (resolvedView != null)
                        {
                            foreach (var collider in resolvedView.GetComponentsInChildren<Collider>()) collider.enabled = false;
                            foreach (var body in resolvedView.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
                            if (!enemy.Model.IsAlive && animationCursors.TryGetValue(enemy.Model.Id, out var cursor))
                                deathDuration = resolvedView.GetComponent<PlayableFarmActor>()?.ApplyRaidAnimation(cursor.Read(enemy)) ?? 0f;
                            if (deathDuration > 0f) deathVisuals.Add(new DeathVisual { View = resolvedView, Remaining = deathDuration });
                            else Destroy(resolvedView.gameObject);
                        }
                        views.Remove(enemy.Model.Id); animationCursors.Remove(enemy.Model.Id);
                    }
                    continue;
                }
                if (!views.TryGetValue(enemy.Model.Id, out var view))
                {
                    var prefab = enemy.Model.TypeId == EnemyTypeId.KeBatHeo ? ThiefPrefab : CrawlerPrefab;
                    if (prefab == null) continue;
                    view = Instantiate(prefab, transform).transform; views.Add(enemy.Model.Id, view);
                    if (view.GetComponent<PlayableFarmActor>() == null) view.gameObject.AddComponent<PlayableFarmActor>();
                    view.GetComponent<PlayableFarmActor>().CoreEnemyId = enemy.Model.Id;
                    animationCursors[enemy.Model.Id] = new FarmRaidAnimationCursor();
                }
                Vector3 next = new Vector3(enemy.X, Farm.GroundHeight(enemy.X, enemy.Y), enemy.Y);
                Vector3 movement = next - view.position; movement.y = 0;
                if (movement.sqrMagnitude > 0.0001f) view.rotation = Quaternion.LookRotation(movement);
                view.position = next;

                // Hit-jolt scale pulse on damage
                if (enemyHitFlashTimers.TryGetValue(enemy.Model.Id, out float flashRem) && flashRem > 0f)
                {
                    flashRem = Mathf.Max(0f, flashRem - Time.deltaTime);
                    enemyHitFlashTimers[enemy.Model.Id] = flashRem;
                    float s = 1.0f + 0.16f * Mathf.Sin((flashRem / 0.18f) * Mathf.PI);
                    view.localScale = new Vector3(s, 1.0f / Mathf.Sqrt(s), s);
                }
                else
                {
                    view.localScale = Vector3.one;
                }

                if (animationCursors.TryGetValue(enemy.Model.Id, out var animation))
                    view.GetComponent<PlayableFarmActor>()?.ApplyRaidAnimation(animation.Read(enemy));
            }
        }
        private void AdvanceDeathVisuals(float seconds)
        {
            for (int i = deathVisuals.Count - 1; i >= 0; i--)
            {
                var visual = deathVisuals[i]; visual.Remaining -= seconds;
                if (visual.View != null && visual.Remaining > 0f) continue;
                if (visual.View != null) Destroy(visual.View.gameObject);
                deathVisuals.RemoveAt(i);
            }
        }
        private IReadOnlyList<string> RaycastTowerShot(string towerId, string targetId)
        {
            piercedIds.Clear();
            if (!views.TryGetValue(targetId, out var target)) return piercedIds;
            PlayableFarmStation station = null;
            foreach (var item in Farm.Stations) if (item != null && item.CoreInstanceId == towerId) { station = item; break; }
            if (station == null) return piercedIds;
            Vector3 origin = station.ProjectileOrigin != null ? station.ProjectileOrigin.position : station.transform.position + Vector3.up;
            var body = target.GetComponentInChildren<Collider>();
            Vector3 aim = body != null ? body.bounds.center : target.position + Vector3.up * 0.5f;
            int count = Physics.RaycastNonAlloc(origin, (aim - origin).normalized, shotHits, 22f, Farm.ObstacleMask, QueryTriggerInteraction.Collide);
            Array.Sort(shotHits, 0, count, HitOrder);
            for (int i = 0; i < count; i++)
            {
                var hit = shotHits[i].collider;
                if (hit == null || hit.GetComponentInParent<PlayableFarmStation>() == station) continue;
                var actor = hit.GetComponentInParent<PlayableFarmActor>();
                if (actor == null) break;
                if (string.IsNullOrEmpty(actor.CoreEnemyId)) continue;
                if (!piercedIds.Contains(actor.CoreEnemyId)) piercedIds.Add(actor.CoreEnemyId);
                if (piercedIds.Count == 2) break;
            }
            return piercedIds;
        }

        private static void DrawRect(Rect r, Color col)
        {
            if (flatWhiteTex == null)
            {
                flatWhiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                flatWhiteTex.SetPixel(0, 0, Color.white);
                flatWhiteTex.Apply();
            }
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(r, flatWhiteTex);
            GUI.color = prev;
        }

        private void OnGUI()
        {
            if (Model == null || Farm == null || Farm.Session == null) return;
            Camera cam = Farm.GameplayCamera != null ? Farm.GameplayCamera : Camera.main;

            // 1. World-Space Floating Enemy HP Bars, Lock Indicators & Telegraph Warnings
            if (cam != null && Model.Active)
            {
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                nameStyle.normal.textColor = new Color(1.0f, 0.96f, 0.86f);

                foreach (var enemy in Model.Enemies)
                {
                    if (enemy.Resolved) continue;
                    Vector3 headWS = new Vector3(enemy.X, Farm.GroundHeight(enemy.X, enemy.Y) + 1.92f, enemy.Y);
                    Vector3 sp = cam.WorldToScreenPoint(headWS);
                    if (sp.z <= 0.2f) continue;
                    float sx = sp.x;
                    float sy = Screen.height - sp.y;
                    bool isLocked = enemy.Model.Id == lockedEnemyId;
                    float bw = 148f;
                    float bx = sx - bw * 0.5f;
                    float by = sy - 36f;

                    DrawRect(new Rect(bx - 2f, by - 2f, bw + 4f, 38f), isLocked ? new Color(1.0f, 0.78f, 0.22f, 0.92f) : new Color(0.08f, 0.08f, 0.12f, 0.85f));
                    DrawRect(new Rect(bx, by, bw, 34f), new Color(0.11f, 0.10f, 0.14f, 0.94f));

                    string prefix = isLocked ? "🎯 " : "";
                    string stateTag = enemy.PinSeconds > 0f ? " [⚡CHOÁNG]" : (!string.IsNullOrEmpty(enemy.CapturedPigId) ? " [⚠KÉO HEO]" : "");
                    GUI.Label(new Rect(bx, by + 1f, bw, 16f), $"{prefix}{enemy.Model.Name}{stateTag}", nameStyle);

                    // HP Bar
                    float hpRatio = Mathf.Clamp01(enemy.Model.CurrentHp / Mathf.Max(1f, enemy.Model.MaxHp));
                    DrawRect(new Rect(bx + 6f, by + 18f, bw - 12f, 8f), new Color(0.22f, 0.08f, 0.08f, 0.95f));
                    DrawRect(new Rect(bx + 6f, by + 18f, (bw - 12f) * hpRatio, 8f), new Color(0.92f, 0.24f, 0.22f, 1.0f));

                    // Telegraph / Stun Bar
                    if (enemy.Telegraph > 0f)
                    {
                        float teleRatio = Mathf.Clamp01(enemy.Telegraph / 0.18f);
                        DrawRect(new Rect(bx + 6f, by + 28f, (bw - 12f) * (1f - teleRatio), 4f), new Color(1.0f, 0.85f, 0.18f, 1.0f));
                        GUI.Label(new Rect(bx - 20f, by - 18f, bw + 40f, 18f), "⚠ ĐÒN ĐỎ — [SPACE] PHẢN ĐÒN!", nameStyle);
                    }
                    else if (enemy.PinSeconds > 0f)
                    {
                        float stunRatio = Mathf.Clamp01(enemy.PinSeconds / 1.5f);
                        DrawRect(new Rect(bx + 6f, by + 28f, (bw - 12f) * stunRatio, 4f), new Color(0.38f, 0.88f, 1.0f, 1.0f));
                    }
                }
            }

            // 2. World-Space Floating Damage & Combat Popups
            if (cam != null && activePopups.Count > 0)
            {
                GUIStyle popStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                for (int i = 0; i < activePopups.Count; i++)
                {
                    var p = activePopups[i];
                    Vector3 sp = cam.WorldToScreenPoint(p.WorldPos);
                    if (sp.z <= 0.2f) continue;
                    float alpha = Mathf.Clamp01(p.Remaining / Mathf.Max(0.2f, p.Duration * 0.45f));
                    popStyle.fontSize = p.IsCritOrMajor ? 18 : 15;
                    float sx = sp.x - 110f;
                    float sy = (Screen.height - sp.y) - 14f;

                    popStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.9f);
                    GUI.Label(new Rect(sx + 1.5f, sy + 1.5f, 220f, 28f), p.Text, popStyle);
                    popStyle.normal.textColor = new Color(p.Color.r, p.Color.g, p.Color.b, alpha);
                    GUI.Label(new Rect(sx, sy, 220f, 28f), p.Text, popStyle);
                }
            }

            // 3. Top-Right Tactical Combat Status Card
            if (!ShowCombatHud) return;
            bool warning = Farm.Session.Engine.Clock.MinorWavesRemainingInCycle > 0 &&
                Farm.Session.Engine.Clock.HoursUntilNextMinorWave <= 2;
            if (!Model.Active && !warning && string.IsNullOrEmpty(message) && !Farm.Session.State.PendingRaid) return;

            Rect cardRect = new Rect(Screen.width - 406f, 14f, 392f, Model.Active ? 210f : 112f);
            DrawRect(new Rect(cardRect.x - 2f, cardRect.y - 2f, cardRect.width + 4f, cardRect.height + 4f),
                Model.Active ? new Color(0.92f, 0.32f, 0.22f, 0.90f) : new Color(0.78f, 0.62f, 0.26f, 0.85f));
            DrawRect(cardRect, new Color(0.09f, 0.10f, 0.14f, 0.94f));

            GUILayout.BeginArea(new Rect(cardRect.x + 12f, cardRect.y + 8f, cardRect.width - 24f, cardRect.height - 16f));
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            headerStyle.normal.textColor = Model.Active ? new Color(1.0f, 0.46f, 0.32f) : new Color(1.0f, 0.84f, 0.36f);
            GUILayout.Label(Model.Active ? "⚔ CHIẾN ĐẤU BẢO VỆ TRẠI · LIÊN HOÀN THỨC " + Model.CurrentComboStep + "/3" : "🛡 CẢNH BÁO AN NINH TRẠI HEO", headerStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            subStyle.normal.textColor = new Color(0.92f, 0.90f, 0.84f);
            GUILayout.Label(message, subStyle);

            if (Model.Active)
            {
                GUILayout.Space(2f);
                GUILayout.Label("[J/LMB] Liên hoàn 3 thức · [K] Sát Gia Trảm · [Space/RMB] Đỡ/Phản Đòn · [C/Ctrl] Lướt Né · [R] Khóa mục tiêu", subStyle);
                foreach (var enemy in Model.Enemies)
                    if (!enemy.Resolved)
                        GUILayout.Label($"• {enemy.Model.Name}: {enemy.Model.CurrentHp:0}/{enemy.Model.MaxHp:0} HP" +
                            (enemy.Telegraph > 0 ? " · ⚠ ĐANG RA ĐÒN" : "") +
                            (enemy.PinSeconds > 0 ? " · ⚡ CHOÁNG" : "") +
                            (string.IsNullOrEmpty(enemy.CapturedPigId) ? "" : " · 🐖 ĐANG KÉO HEO"), subStyle);
            }
            GUILayout.EndArea();
        }
    }
}
