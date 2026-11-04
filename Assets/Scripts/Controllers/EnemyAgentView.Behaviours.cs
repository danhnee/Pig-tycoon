using System;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD 19.1–19.2 monster and Boss behaviours, structure targeting, stealth, stun/glue/reveal from structures,
    /// and the combat hooks (forced target, armour break). Numbers without a GDD source are PROVISIONAL (ADR-0020).
    /// </summary>
    public partial class EnemyAgentView
    {
        // ---------------- registry & events ----------------
        private static readonly List<EnemyAgentView> registry = new List<EnemyAgentView>();
        public static IReadOnlyList<EnemyAgentView> Registry => registry;
        public static event Action<EnemyAgentView> AnySpawned;
        public static event Action<EnemyAgentView> AnyDied;
        /// <summary>True only while a monster's blow resolves: pigs ignore every other IDamageable source.</summary>
        public static bool IsResolvingMonsterHit { get; private set; }

        // PROVISIONAL (ADR-0020)
        public const float ChoSuongFogSpeedFactor = 1.3f;
        public const float ScoutMarkRadiusM = 6f;
        public const float HarvesterExtraGrabRadiusM = 3f;
        public const int HarvesterMaxPigs = 3;            // GDD: 1–3 heo
        public const float HarvesterBeamSeconds = 4f;     // GDD: chùm kéo 4 giây
        public const float ScoutCallSeconds = 30f;        // GDD: sống quá 30 giây
        public const float ThiefLordAuraRadiusM = 10f;    // GDD: quanh 10 m
        public const float ThiefLordAuraSpeed = 1.2f;     // GDD: nhanh +20%
        public const float StormBeastLightningEveryS = 10f; // GDD: mỗi 10 giây
        public const float IronLordSummonEveryS = 30f;    // GDD: gọi Kẻ Bắt Heo mỗi 30 giây
        public const float IronLordGateDamageFactor = 2f; // PROVISIONAL: "phá cổng rất nhanh"
        public const float MothershipDropEveryS = 30f;    // PROVISIONAL: GDD says "thả UFO Thu Hoạch" without a period
        public const float MothershipBeamSeconds = 6f;    // GDD: chùm kéo lớn 6 giây
        public const float MothershipBeamRadiusM = 4f;    // PROVISIONAL: "hút cả nhóm heo"
        public const float MothershipBeamEveryS = 30f;    // PROVISIONAL
        public const float MothershipHoverM = 3f;         // PROVISIONAL (visual offset)

        private float revealedUntil, gluedUntil, stunnedUntil, forcedUntil, armorBreakUntil, armorBreakFraction;
        private Transform forcedTarget;
        private float bornAt, specialTimer, lightningTimer, beamCooldown, beamElapsed;
        private int lastBossPhase = 1;
        private bool harvesterCalled, mothershipBeaming;
        private PigAgentView markedPig;
        private readonly List<PigAgentView> extraCaptured = new List<PigAgentView>();
        private readonly List<PigAgentView> mothershipBeamPigs = new List<PigAgentView>();
        private GameObject mothershipBeamObj;

        /// <summary>UFO Thu Hoạch called by a scout goes for the scout's marked group.</summary>
        public PigAgentView PreferredPigTarget { get; set; }

        public bool IsStealthed => Model != null && Model.TypeId == EnemyTypeId.KeCatDay && Time.time >= revealedUntil;
        public bool IsRevealed => Time.time < revealedUntil;
        public void Reveal(float seconds) { if (seconds > 0f) revealedUntil = Mathf.Max(revealedUntil, Time.time + seconds); }
        public bool IsGlued => Time.time < gluedUntil;
        public void Glue(float seconds) { if (seconds > 0f) gluedUntil = Mathf.Max(gluedUntil, Time.time + seconds); }
        public bool IsStunned => Time.time < stunnedUntil;
        public bool IsMoving => rb != null && rb.linearVelocity.sqrMagnitude > 0.01f;
        public bool IsAir => Model != null && DefenseStructureSpecs.IsAirMonster(Model.TypeId);
        /// <summary>GDD 16.1 Móc Trời: a hooked UFO flies low for a while, so ground weapons can hit it.</summary>
        public bool IsPulledLow => Time.time < pulledLowUntil;
        public void PullLow(float seconds) { if (seconds > 0f && IsAir) pulledLowUntil = Mathf.Max(pulledLowUntil, Time.time + seconds); }
        private float pulledLowUntil;
        /// <summary>Zones a monster may not enter (Hỏa Tiễn fire for ground, Skyhook debris screen for air): put it back on the edge.</summary>
        public void KeepOutOf(Vector2 centre, float radius)
        {
            Vector2 p = transform.position;
            Vector2 d = p - centre;
            float dist = d.magnitude;
            if (dist >= radius) return;
            Vector2 edge = centre + (dist < 0.01f ? Vector2.right : d / dist) * radius;
            if (rb != null) rb.position = edge;
            transform.position = new Vector3(edge.x, edge.y, transform.position.z);
        }
        public float HpCurrent => Model != null ? Model.CurrentHp : 0f;

        /// <summary>GDD 17 Tấm Sốc Điện: stun, cancelling the blow being swung (even on a gate).</summary>
        public void Stun(float seconds)
        {
            if (seconds <= 0f || !IsAlive) return;
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
            ApplySlow(0f, seconds);
            if (Model != null) attackTimer = Mathf.Max(attackTimer, seconds);
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // ---------------- combat hooks (coordinator) ----------------
        /// <summary>Khiêu Chiến: for <paramref name="seconds"/> the monster drops pigs/structures and goes for <paramref name="t"/>.</summary>
        public void ForceTarget(Transform t, float seconds)
        {
            if (t == null || seconds <= 0f) return;
            forcedTarget = t;
            forcedUntil = Time.time + seconds;
            currentTarget = t;
            approachPath.Clear();
        }

        /// <summary>Phá Giáp: EnemyData has no armour stat, so every source deals ×1/(1−fraction) for <paramref name="seconds"/>.</summary>
        public void ApplyArmorBreak(float fraction, float seconds)
        {
            if (seconds <= 0f || float.IsNaN(fraction)) return;
            armorBreakFraction = Mathf.Clamp(fraction, 0f, 0.9f);
            armorBreakUntil = Time.time + seconds;
        }

        /// <summary>ISkillReactive: a skill knocks the pig out of the thief's/UFO's hold.</summary>
        public void ReleaseCapturedPig() => ReleaseCapturedPig(rescuedByPlayer: true);

        private float IncomingDamageMultiplier => Time.time < armorBreakUntil ? 1f / (1f - armorBreakFraction) : 1f;
        private bool ForcedTargetActive => forcedTarget != null && Time.time < forcedUntil && forcedTarget.gameObject.activeInHierarchy;

        /// <summary>
        /// A structure's hit (GDD 13.3 rule 9: no resistance, full damage). Unlike <see cref="TakeDamage"/> there are no
        /// i-frames and the monster does not turn on the player.
        /// </summary>
        public void ApplyStructureHit(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            float dmg = amount * IncomingDamageMultiplier;
            Model.CurrentHp = Mathf.Max(0f, Model.CurrentHp - dmg);
            if (capturedPig != null || activeTractorBeamObj != null || extraCaptured.Count > 0) ReleaseCapturedPig(rescuedByPlayer: true);
            RefreshHealthBar();
            OnHitTaken?.Invoke();
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());
            SpawnDamagePopup(dmg);
            if (!Model.IsAlive) Die();
        }

        private void RefreshHealthBar()
        {
            if (healthBarRoot == null) return;
            healthBarRoot.SetActive(true);
            if (healthBarFill == null) return;
            float ratio = Mathf.Clamp01(Model.CurrentHp / Model.MaxHp);
            bool isBigBoss = Model.TypeId == EnemyTypeId.BossAlienWarlord || Model.TypeId == EnemyTypeId.UfoMothership;
            healthBarFill.localScale = new Vector3((isBigBoss ? 1.16f : 0.58f) * ratio, isBigBoss ? 0.10f : 0.06f, 1f);
        }

        /// <summary>Wave time cap: the survivors withdraw (no reward, no death effects).</summary>
        public void Retreat()
        {
            ReleaseCapturedPig(rescuedByPlayer: false);
            Destroy(gameObject);
        }

        /// <summary>GDD 17 Lưới EMP: a UFO loses its lock — the beam drops and the pigs fall back.</summary>
        public void BreakLock()
        {
            if (!IsAir) return;
            ReleaseCapturedPig(rescuedByPlayer: false);
            EndMothershipBeam(stolen: false);
            currentTarget = null;
            attackTimer = Mathf.Max(attackTimer, 1f);
        }

        private float MoveFactor
        {
            get
            {
                if (IsStunned) return 0f;
                float f = ActiveSlow;
                if (Model == null) return f;
                if (Model.TypeId == EnemyTypeId.KeBatHeo && NearThiefLord()) f *= ThiefLordAuraSpeed;
                if (Model.TypeId == EnemyTypeId.ChoSuong)
                {
                    var e = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
                    if (e != null && e.CurrentWeather == WeatherType.SuongMu) f *= ChoSuongFogSpeedFactor;
                }
                return f;
            }
        }

        private bool NearThiefLord()
        {
            for (int i = 0; i < registry.Count; i++)
            {
                var o = registry[i];
                if (o == null || o == this || !o.IsAlive || o.Model.TypeId != EnemyTypeId.DauLinhTrom) continue;
                if (Vector2.Distance(o.transform.position, transform.position) <= ThiefLordAuraRadiusM) return true;
            }
            return false;
        }

        private static GameEngine EngineOrNull => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        // ---------------- lifecycle ----------------
        private void OnBoundBehaviours()
        {
            if (!registry.Contains(this)) registry.Add(this);
            bornAt = Time.time;
            AnySpawned?.Invoke(this);
            var e = EngineOrNull;
            if (Model.TypeId == EnemyTypeId.ThucTheSuong && e != null && e.CurrentWeather != WeatherType.SuongMu)
            {
                e.ChangeWeather(WeatherType.SuongMu);   // GDD 19.2: phủ sương toàn trại
                DefenseFx.Feedback("<color=#94a3b8>THỰC THỂ SƯƠNG phủ sương toàn trại!</color>");
            }
        }

        private void OnRemovedBehaviours()
        {
            registry.Remove(this);
            RestoreCutWires(announce: false);
            EndMothershipBeam(stolen: false);
        }

        private void OnDiedBehaviours()
        {
            AnyDied?.Invoke(this);
            RestoreCutWires(announce: true);
            EndMothershipBeam(stolen: false);
            var e = EngineOrNull;
            if (e != null && Model.TypeId == EnemyTypeId.KeMangDich)
            {
                e.Defense.AddContaminationZone(transform.position.x, transform.position.y);
                DefenseFx.Ring(transform.position, 3f, new Color(0.6f, 0.9f, 0.2f, 0.9f), 3f);
                DefenseFx.Feedback("<color=#a3e635>Kẻ Mang Dịch để lại vùng ô nhiễm: rải vôi trong 12 giờ, nếu không chuồng sẽ nhiễm!</color>");
            }
        }

        private void RestoreCutWires(bool announce)
        {
            var e = EngineOrNull;
            if (e == null || Model == null || Model.TypeId != EnemyTypeId.KeCatDay) return;
            int n = e.Defense.RestoreWiresCutBy(Model.Id);
            if (n > 0 && announce) DefenseFx.Feedback($"<color=#4ade80>Đã hạ Kẻ Cắt Dây: {n} công trình có điện trở lại.</color>");
        }

        private static void CountPigLost()
        {
            var e = EngineOrNull;
            if (e != null) e.Defense.PigsLostThisWave += 1;
        }

        // ---------------- per-frame specials ----------------
        /// <summary>Returns true when the special behaviour consumed this frame's movement.</summary>
        private bool UpdateSpecialBehaviour()
        {
            if (Model == null) return false;
            if (IsStunned)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return true;
            }
            float dt = Time.deltaTime;
            if (Model.TypeId == EnemyTypeId.KeCatDay && SpriteRenderer != null)
            {
                var c = SpriteRenderer.color;
                SpriteRenderer.color = new Color(c.r, c.g, c.b, IsStealthed ? 0.3f : 1f);
            }
            switch (Model.TypeId)
            {
                case EnemyTypeId.ChuaTromRiuSat:
                    specialTimer += dt;
                    if (specialTimer >= IronLordSummonEveryS)
                    {
                        specialTimer = 0f;
                        var spawner = EnemyWaveSpawner.Instance;
                        if (spawner != null)
                        {
                            spawner.SpawnEnemyAt(EnemyTypeId.KeBatHeo, (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 2f, EngineOrNull?.Clock.CurrentDay ?? 1);
                            DefenseFx.Feedback("<color=#ef4444>Chúa Trộm Rìu Sắt gọi thêm Kẻ Bắt Heo!</color>");
                        }
                    }
                    break;
                case EnemyTypeId.ThucTheSuong:
                {
                    int phase = Model.GetBossPhase();
                    if (phase > lastBossPhase)
                    {
                        lastBossPhase = phase;
                        TeleportInsideFarm();
                        DefenseFx.Feedback($"<color=#94a3b8>Thực Thể Sương sang pha {phase} và dịch chuyển!</color>");
                    }
                    break;
                }
                case EnemyTypeId.ThuBao:
                    lightningTimer += dt;
                    if (lightningTimer >= StormBeastLightningEveryS) { lightningTimer = 0f; CallLightning(); }
                    break;
                case EnemyTypeId.UfoTrinhSat:
                    if (!harvesterCalled && Time.time - bornAt >= ScoutCallSeconds && markedPig != null) CallHarvester();
                    break;
                case EnemyTypeId.UfoMothership:
                    UpdateMothership(dt);
                    return true;
            }
            return false;
        }

        private void TeleportInsideFarm()
        {
            var farm = PixelMythFarmGameplay.Instance;
            Vector2 p = farm != null
                ? new Vector2(UnityEngine.Random.Range(farm.FenceRect.xMin + 4f, farm.FenceRect.xMax - 4f), UnityEngine.Random.Range(farm.FenceRect.yMin + 4f, farm.FenceRect.yMax - 4f))
                : (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 8f;
            DefenseFx.Ring(transform.position, 1.5f, new Color(0.7f, 0.75f, 0.85f, 0.9f), 0.6f);
            if (rb != null) rb.position = p;
            transform.position = p;
            currentTarget = null;
            DefenseFx.Ring(p, 1.5f, new Color(0.7f, 0.75f, 0.85f, 0.9f), 0.6f);
        }

        private void CallLightning()
        {
            DefenseTower2DView best = null;
            float bestD = float.MaxValue;
            foreach (var t in DefenseTower2DView.Registry)
            {
                if (t == null || t.BuildingModel == null || t.BuildingModel.IsBroken) continue;
                float d = Vector2.Distance(t.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = t; }
            }
            if (best == null) return;
            Vector2 top = (Vector2)best.transform.position + new Vector2(0f, 9f);
            DefenseFx.Line(top, best.transform.position, new Color(0.85f, 0.9f, 1f, 1f), 0.25f, 0.14f);
            if (DefenseTower2DView.IsUnderLightningRod(best.transform.position))
            {
                DefenseFx.Feedback("<color=#93c5fd>Cột Thu Lôi hứng sét của Thú Bão!</color>");
                return;
            }
            best.StrikeByLightning();
            DefenseFx.Feedback($"<color=#facc15>Thú Bão gọi sét đánh {best.BuildingModel.Name} (−10 độ bền)!</color>");
        }

        private void CallHarvester()
        {
            harvesterCalled = true;
            var spawner = EnemyWaveSpawner.Instance;
            if (spawner == null || markedPig == null) return;
            Vector2 pigPos = markedPig.transform.position;
            Vector2 edge = pigPos + new Vector2(0f, 14f);
            var farm = PixelMythFarmGameplay.Instance;
            if (farm != null)
            {
                var r = farm.MapRect;
                float dl = pigPos.x - r.xMin, dr = r.xMax - pigPos.x, db = pigPos.y - r.yMin, dtp = r.yMax - pigPos.y;
                float m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dtp));
                edge = m == dl ? new Vector2(r.xMin + 2f, pigPos.y) : m == dr ? new Vector2(r.xMax - 2f, pigPos.y)
                     : m == db ? new Vector2(pigPos.x, r.yMin + 2f) : new Vector2(pigPos.x, r.yMax - 2f);
            }
            var harvester = spawner.SpawnEnemyAt(EnemyTypeId.UfoThuHoach, edge, EngineOrNull?.Clock.CurrentDay ?? 1);
            if (harvester != null) harvester.PreferredPigTarget = markedPig;
            DefenseFx.Feedback($"<color=#c084fc>UFO Trinh Sát sống quá 30 giây: UFO Thu Hoạch đang tới nhóm heo của {markedPig.PigModel?.Name ?? "đàn"}!</color>");
        }

        // ---------------- Mẫu Hạm UFO (GDD 19.2) ----------------
        private void UpdateMothership(float dt)
        {
            Vector2 pos = transform.position;
            Vector2 groundPoint = pos - new Vector2(0f, MothershipHoverM);
            if (mothershipBeaming)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                beamElapsed += dt;
                if (beamElapsed >= MothershipBeamSeconds) EndMothershipBeam(stolen: true);
                return;
            }
            // it moves: over the herd's centre (the player if no pig is left)
            Vector2 sum = Vector2.zero; int n = 0;
            PigAgentView nearest = null; float nearestD = float.MaxValue;
            foreach (var p in FindObjectsByType<PigAgentView>())
            {
                if (p == null || p.PigModel == null || !p.PigModel.IsAlive || p.IsCaptured) continue;
                sum += (Vector2)p.transform.position; n++;
                float d = Vector2.Distance(groundPoint, p.transform.position);
                if (d < nearestD) { nearestD = d; nearest = p; }
            }
            Vector2 goal = n > 0 ? sum / n : (PlayerCombatController.Instance != null ? (Vector2)PlayerCombatController.Instance.transform.position : pos);
            goal += new Vector2(0f, MothershipHoverM);
            Vector2 to = goal - pos;
            if (rb != null) rb.linearVelocity = to.magnitude > 0.6f ? to.normalized * Model.MoveSpeed * MoveFactor : Vector2.zero;
            UpdateFacingSprite(to);

            specialTimer += dt;
            if (specialTimer >= MothershipDropEveryS)
            {
                specialTimer = 0f;
                var spawner = EnemyWaveSpawner.Instance;
                if (spawner != null)
                {
                    spawner.SpawnEnemyAt(EnemyTypeId.UfoThuHoach, pos + new Vector2(UnityEngine.Random.Range(-2f, 2f), -1f), EngineOrNull?.Clock.CurrentDay ?? 1);
                    DefenseFx.Feedback("<color=#c084fc>Mẫu Hạm thả UFO Thu Hoạch!</color>");
                }
            }

            beamCooldown += dt;
            if (beamCooldown >= MothershipBeamEveryS && nearest != null && nearestD <= MothershipBeamRadiusM)
            {
                beamCooldown = 0f;
                StartMothershipBeam(groundPoint);
                return;
            }

            // plasma at the nearest pig when it is in reach
            if (nearest != null && nearestD <= Model.AttackRange + MothershipHoverM && attackTimer <= 0f)
            {
                currentTarget = nearest.transform;
                ExecuteAttackOnTarget();
            }
        }

        private void StartMothershipBeam(Vector2 groundPoint)
        {
            mothershipBeamPigs.Clear();
            foreach (var p in FindObjectsByType<PigAgentView>())
            {
                if (p == null || p.PigModel == null || !p.PigModel.IsAlive || p.IsCaptured) continue;
                if (Vector2.Distance(groundPoint, p.transform.position) > MothershipBeamRadiusM) continue;
                p.AbductByUfo(transform);
                if (p.IsAbductedByUfo) mothershipBeamPigs.Add(p);
            }
            if (mothershipBeamPigs.Count == 0) return;
            mothershipBeaming = true;
            beamElapsed = 0f;
            mothershipBeamObj = new GameObject("Mothership_GroupBeam");
            mothershipBeamObj.transform.SetParent(transform, false);
            mothershipBeamObj.transform.position = (Vector2)transform.position + new Vector2(0f, -0.6f);
            var sr = mothershipBeamObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetTractorBeamVFX();
            sr.sortingOrder = 280;
            sr.color = new Color(0.95f, 0.2f, 0.85f, 0.85f);
            mothershipBeamObj.transform.localScale = new Vector3(0.9f, 0.7f, 1f);
            DefenseFx.Feedback($"<color=#f43f5e>MẪU HẠM bật chùm kéo lớn 6 giây: {mothershipBeamPigs.Count} heo đang bị hút! Dùng Skyhook / Lưới EMP!</color>");
        }

        private void EndMothershipBeam(bool stolen)
        {
            if (mothershipBeamObj != null) Destroy(mothershipBeamObj);
            mothershipBeamObj = null;
            if (!mothershipBeaming && mothershipBeamPigs.Count == 0) return;
            mothershipBeaming = false;
            foreach (var p in mothershipBeamPigs)
            {
                if (p == null) continue;
                if (stolen && p.IsAbductedByUfo)
                {
                    p.OnPigStolenAndLost();
                    CountPigLost();
                    Destroy(p.gameObject);
                }
                else if (p.IsCaptured) p.ReleaseFromCapture(false);
            }
            mothershipBeamPigs.Clear();
        }

        // ---------------- targeting ----------------
        private bool TargetStillValid()
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy) return false;
            var t = currentTarget.GetComponent<DefenseTower2DView>();
            if (t != null && (t.BuildingModel == null || t.BuildingModel.IsBroken)) return false;
            var seg = currentTarget.GetComponent<PixelFenceSegment>();
            if (seg != null && seg.IsBroken) return false;
            var pig = currentTarget.GetComponent<PigAgentView>();
            if (pig != null && (pig.PigModel == null || !pig.PigModel.IsAlive)) return false;
            return true;
        }

        private static DefenseTower2DView NearestStructure(Vector2 from, Func<DefenseTower2DView, bool> filter, float maxDist = float.MaxValue)
        {
            DefenseTower2DView best = null;
            float bestD = maxDist;
            foreach (var t in DefenseTower2DView.Registry)
            {
                if (t == null || t.BuildingModel == null || t.BuildingModel.IsBroken || !filter(t)) continue;
                float d = Vector2.Distance(from, t.transform.position);
                if (d < bestD) { bestD = d; best = t; }
            }
            return best;
        }

        /// <summary>Returns true when this monster picked (or kept) its own GDD 19 target.</summary>
        private bool SelectSpecialTarget(PlayerCombatController player)
        {
            if (ForcedTargetActive) { currentTarget = forcedTarget; return true; }
            if (forcedTarget != null) { forcedTarget = null; currentTarget = null; }
            Vector2 me = transform.position;
            var e = EngineOrNull;
            switch (Model.TypeId)
            {
                case EnemyTypeId.KeCatDay:
                {
                    if (TargetStillValid() && currentTarget.GetComponent<DefenseTower2DView>() != null) return true;
                    if (e == null) return false;
                    var t = NearestStructure(me, v => e.Defense.NeedsPower(v.BuildingModel) && string.IsNullOrEmpty(v.BuildingModel.WireCutBy));
                    if (t == null) return false;
                    currentTarget = t.transform;
                    return true;
                }
                case EnemyTypeId.ThietGiapCongThanh:
                case EnemyTypeId.ChuaTromRiuSat:
                {
                    if (TargetStillValid() && (currentTarget.GetComponent<DefenseTower2DView>() != null || currentTarget.GetComponent<PixelFenceSegment>() != null)) return true;
                    var gate = NearestStructure(me, v => v.BuildingModel.BlueprintId == "CongGiaCo");
                    if (gate == null) gate = NearestStructure(me, v => v.Spec != null && v.Spec.Role == StructureRole.Barrier);
                    if (gate != null) { currentTarget = gate.transform; return true; }
                    var seg = NearestFenceSegment(me, 400f);
                    if (seg != null) { currentTarget = seg.transform; return true; }
                    return Model.TypeId == EnemyTypeId.ThietGiapCongThanh; // siege ram: nothing else interests it
                }
                case EnemyTypeId.KeCaoPha:
                {
                    // GDD 19.1: cào rào — built fences (Rào gỗ/thép) count as well as the layout fence
                    if (TargetStillValid() && (currentTarget.GetComponent<DefenseTower2DView>() != null || currentTarget.GetComponent<PixelFenceSegment>() != null)) return true;
                    var rao = NearestStructure(me, v => v.BuildingModel.BlueprintId == "RaoGo" || v.BuildingModel.BlueprintId == "RaoThep");
                    var seg = NearestFenceSegment(me, 400f);
                    if (rao == null) return false;   // the existing fence logic handles the layout fence
                    if (seg != null && Vector2.Distance(me, seg.transform.position) < Vector2.Distance(me, rao.transform.position)) return false;
                    currentTarget = rao.transform;
                    return true;
                }
                case EnemyTypeId.UfoPhaoHam:
                {
                    if (TargetStillValid() && currentTarget.GetComponent<DefenseTower2DView>() != null) return true;
                    var t = NearestStructure(me, v => v.BuildingModel.BlueprintId == "MayPhatDien") ?? NearestStructure(me, v => true);
                    if (t == null) return false;
                    currentTarget = t.transform;
                    return true;
                }
                case EnemyTypeId.ChoSuong:
                {
                    if (player != null && player.IsAlive && DefenseTower2DView.IsInDeployZone(player.transform.position))
                    { currentTarget = player.transform; return true; }
                    if (TargetStillValid() && currentTarget.GetComponent<DefenseTower2DView>() != null) return true;
                    var t = NearestStructure(me, v => v.Spec != null && v.Spec.Role == StructureRole.Deploy);
                    if (t == null) return false;
                    currentTarget = t.transform;
                    return true;
                }
                case EnemyTypeId.DauLinhTrom:
                {
                    if (TargetStillValid() && currentTarget.GetComponent<PigAgentView>() is PigAgentView kept && !kept.IsCaptured) return true;
                    PigAgentView best = null; int bestValue = -1;
                    foreach (var p in FindObjectsByType<PigAgentView>())
                    {
                        if (p == null || p.PigModel == null || !p.PigModel.IsAlive || p.IsCaptured) continue;
                        int v = e != null ? e.Economy.CalculatePigSalePrice(p.PigModel, e.Farm.FarmLevel).priceGold : (int)p.PigModel.WeightKg;
                        if (v > bestValue) { bestValue = v; best = p; }
                    }
                    if (best == null) return false;
                    currentTarget = best.transform;
                    best.ShowThought("►", 3f);   // GDD: target shown on the map
                    return true;
                }
                case EnemyTypeId.UfoTrinhSat:
                {
                    if (markedPig == null || markedPig.PigModel == null || !markedPig.PigModel.IsAlive || markedPig.IsCaptured)
                    {
                        markedPig = null; float bestD = float.MaxValue;
                        foreach (var p in FindObjectsByType<PigAgentView>())
                        {
                            if (p == null || p.PigModel == null || !p.PigModel.IsAlive || p.IsCaptured) continue;
                            float d = Vector2.Distance(me, p.transform.position);
                            if (d < bestD) { bestD = d; markedPig = p; }
                        }
                        if (markedPig != null)
                        {
                            int group = 0;
                            foreach (var p in FindObjectsByType<PigAgentView>())
                                if (p != null && Vector2.Distance(p.transform.position, markedPig.transform.position) <= ScoutMarkRadiusM) { group++; p.ShowThought("►", 2f); }
                            DefenseFx.Feedback($"<color=#c084fc>UFO Trinh Sát đánh dấu nhóm {group} heo. Hạ nó trong 30 giây!</color>");
                        }
                    }
                    if (markedPig == null) return false;
                    currentTarget = markedPig.transform;
                    return true;
                }
                case EnemyTypeId.UfoThuHoach:
                {
                    var pref = PreferredPigTarget;
                    if (pref != null && pref.PigModel != null && pref.PigModel.IsAlive && !pref.IsCaptured) { currentTarget = pref.transform; return true; }
                    return false;
                }
            }
            return false;
        }

        /// <summary>Stuck next to something: Bầy Nhảy leaps a wooden fence; everyone else attacks a blocking barrier.</summary>
        private bool TryHandleBlocked(Vector2 myPos, Vector2 toTarget)
        {
            if (Model.TypeId == EnemyTypeId.BayNhay)
            {
                var seg = NearestFenceSegment(myPos, 1.8f);   // PixelMyth fence = wooden (300 HP)
                var wood = NearestStructure(myPos, v => v.BuildingModel.BlueprintId == "RaoGo", 1.8f);
                if (seg != null || wood != null)
                {
                    Vector2 leap = (toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector2.up) * 2.6f;
                    if (rb != null) rb.position = myPos + leap;
                    transform.position = myPos + leap;
                    StartCoroutine(LungeVisualRoutine(leap.normalized));
                    return true;
                }
            }
            var wall = NearestStructure(myPos, v => v.Spec != null && v.Spec.Role == StructureRole.Barrier, 1.8f);
            if (wall != null) { currentTarget = wall.transform; return true; }
            return false;
        }

        private bool IsHeavyStructureHitter =>
            Model.TypeId == EnemyTypeId.ThietGiapCongThanh || Model.TypeId == EnemyTypeId.UfoPhaoHam ||
            WaveRosterPlanner.IsElite(Model.TypeId) || DefenseStructureSpecs.IsBossOrElite(Model.TypeId);

        /// <summary>Hits on structures (and the wire cut). Returns true when handled.</summary>
        private bool TryExecuteSpecialAttack()
        {
            if (currentTarget == null) return false;
            var tower = currentTarget.GetComponent<DefenseTower2DView>();
            if (tower == null || tower.BuildingModel == null) return false;
            Vector2 dir = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
            if (Model.TypeId == EnemyTypeId.KeCatDay)
            {
                var e = EngineOrNull;
                int n = e != null ? e.Defense.CutWireBranch(Model.Id, tower.BuildingModel) : 0;
                if (n > 0) DefenseFx.Feedback($"<color=#f97316>Kẻ Cắt Dây cắt dây điện: {n} công trình mất điện cho đến khi hạ nó!</color>");
                Reveal(1.5f);
                currentTarget = null;
                StartCoroutine(LungeVisualRoutine(dir));
                return true;
            }
            float dmg = Model.AttackDamage;
            if (Model.TypeId == EnemyTypeId.ChuaTromRiuSat && tower.BuildingModel.BlueprintId == "CongGiaCo") dmg *= IronLordGateDamageFactor;
            if (Model.TypeId == EnemyTypeId.UfoPhaoHam)
                DefenseFx.Line(transform.position, tower.transform.position, new Color(0.9f, 0.2f, 1f, 1f), 0.18f, 0.12f);
            if (MartialSkillRunner.TryInterceptForAlly(tower.transform.position, dmg, dir)) { StartCoroutine(LungeVisualRoutine(dir)); return true; }   // Chắn Mạng
            tower.TakeStructureDamage(dmg, IsHeavyStructureHitter);
            StartCoroutine(LungeVisualRoutine(dir));
            if (!tower.IsAlive || tower.BuildingModel.IsBroken) currentTarget = null;
            return true;
        }

        /// <summary>UFO Thu Hoạch: after the first pig, the beam grabs up to two more within 3 m (GDD "1–3 heo").</summary>
        private void GrabExtraPigs(PigAgentView first)
        {
            extraCaptured.Clear();
            foreach (var p in FindObjectsByType<PigAgentView>())
            {
                if (extraCaptured.Count >= HarvesterMaxPigs - 1) break;
                if (p == null || p == first || p.PigModel == null || !p.PigModel.IsAlive || p.IsCaptured) continue;
                if (Vector2.Distance(p.transform.position, first.transform.position) > HarvesterExtraGrabRadiusM) continue;
                p.AbductByUfo(transform);
                if (p.IsAbductedByUfo) extraCaptured.Add(p);
            }
        }

        private void FinishExtraPigs(bool stolen, bool rescuedByPlayer)
        {
            foreach (var p in extraCaptured)
            {
                if (p == null) continue;
                if (stolen && p.IsAbductedByUfo) { p.OnPigStolenAndLost(); CountPigLost(); Destroy(p.gameObject); }
                else if (p.IsCaptured) p.ReleaseFromCapture(rescuedByPlayer);
            }
            extraCaptured.Clear();
        }

        /// <summary>Resolves a monster blow on an IDamageable with the pig gate open.</summary>
        private static void DealMonsterHit(IDamageable target, float amount, Vector2 dir, float knock)
        {
            if (target is PigAgentView && MartialSkillRunner.TryInterceptForAlly(target.Position, amount, dir)) return;   // Chắn Mạng
            IsResolvingMonsterHit = true;
            try { target.TakeDamage(amount, dir, knock); }
            finally { IsResolvingMonsterHit = false; }
        }

        private void HurtPig(PigAgentView pig, float amount)
        {
            if (pig == null || pig.PigModel == null) return;
            if (MartialSkillRunner.TryInterceptForAlly(pig.transform.position, amount, ((Vector2)pig.transform.position - (Vector2)transform.position).normalized)) return;   // Chắn Mạng
            bool died = MonsterPigDamage.Apply(EngineOrNull, pig.PigModel, amount, Model.Name);
            if (died) pig.ShowThought("†", 3f);
        }
    }
}
