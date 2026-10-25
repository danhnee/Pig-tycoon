using System;
using System.Collections;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public enum ProjectileArchetype
    {
        LinearBallistic,
        ParabolicMortar,
        SpinningSaw,
        BouncingAcid,
        HarpoonChain,
        HeavyShell
    }

    /// <summary>
    /// Lớp hiển thị đạn bay Top-Down 2D chuyên sâu (Combat Juice / Technical Art) tích hợp Object Pooling.
    /// Hỗ trợ tách biệt 2 lớp: ShadowTransform (bóng trượt trên mặt đất) và VisualTransform (hình ảnh bay bổng trục Y theo Parabol),
    /// cùng 7 Archetype: Đạn nỏ sắt B1 (kèm vệt gió), Rìu cưa xoay tròn văng 8 mảnh đinh B2, Bọt axit nảy 2 nhịp C3, Lao xích sắt B4, Đại pháo khói đặc B5, Cầu lượng tử A2, Lôi cầu A4, Gai nhầy C1...
    /// Giữ 100% tương thích cơ chế phản đòn kiếm chém (Deflect) chuẩn Stardew Valley 1.6.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class CombatProjectile2D : MonoBehaviour
    {
        [Header("Object Pooling")]
        public bool IsPooled = true;

        [Header("Projectile Properties")]
        public float Speed = 5.0f;
        public float Damage = 15f;
        public float Lifetime = 4.0f;
        public bool IsFromPlayer = false;
        public bool IsDeflected = false;
        public ProjectileArchetype Archetype = ProjectileArchetype.LinearBallistic;

        [Header("Debuff Effects")]
        public bool CausesBleed = false;
        public float BleedDps = 15f;
        public float BleedDuration = 3.5f;

        public bool CausesSlimeSlow = false;
        public float SlimeSlowAmount = 0.30f;
        public float SlimeSlowDuration = 3.0f;

        [Header("Trail Effects")]
        public bool HasTrail = false;
        public string TrailType = null; // "wind", "quantum", "smoke", "slime"
        public Color TrailColor = Color.white;
        private float trailTimer = 0f;

        [Header("Multi-Layer Rigging")]
        public Transform VisualTransform;
        public Transform ShadowTransform;
        public SpriteRenderer VisualRenderer;
        public SpriteRenderer ShadowRenderer;

        [Header("Parabolic & Bounce State")]
        public float ArcHeight = 1.8f;
        public float TotalDuration = 0.65f;
        public Action OnLandCallback;

        private Vector2 moveDirection = Vector2.down;
        private CircleCollider2D col;
        private SpriteRenderer legacySr;
        private float lifeTimer = 0f;
        private bool isLaunched = false;

        // Harpoon chain state
        private Transform chainLauncher;
        private LineRenderer chainLr;
        private Transform harpoonTargetTransform;
        private Action<Transform, Vector2> onHarpoonHit;

        // Spinning state
        private float spinSpeedDegPerSec = 0f;

        public Vector2 Direction => moveDirection;

        private void Awake()
        {
            col = GetComponent<CircleCollider2D>();
            if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.22f;

            legacySr = GetComponent<SpriteRenderer>();

            EnsureRigging();
        }

        public void EnsureRigging()
        {
            if (VisualTransform == null)
            {
                var vObj = transform.Find("Visual");
                if (vObj == null)
                {
                    var newV = new GameObject("Visual");
                    newV.transform.SetParent(transform, false);
                    VisualTransform = newV.transform;
                }
                else
                {
                    VisualTransform = vObj;
                }
            }

            if (VisualRenderer == null && VisualTransform != null)
            {
                VisualRenderer = VisualTransform.GetComponent<SpriteRenderer>();
                if (VisualRenderer == null)
                {
                    VisualRenderer = VisualTransform.gameObject.AddComponent<SpriteRenderer>();
                }
                VisualRenderer.sortingOrder = 330;
            }

            if (ShadowTransform == null)
            {
                var sObj = transform.Find("Shadow");
                if (sObj == null)
                {
                    var newS = new GameObject("Shadow");
                    newS.transform.SetParent(transform, false);
                    ShadowTransform = newS.transform;
                }
                else
                {
                    ShadowTransform = sObj;
                }
            }

            if (ShadowRenderer == null && ShadowTransform != null)
            {
                ShadowRenderer = ShadowTransform.GetComponent<SpriteRenderer>();
                if (ShadowRenderer == null)
                {
                    ShadowRenderer = ShadowTransform.gameObject.AddComponent<SpriteRenderer>();
                }
                ShadowRenderer.sprite = UISpriteLoader.GetSoftShadowSprite();
                ShadowRenderer.color = new Color(0f, 0f, 0f, 0.45f);
                ShadowRenderer.sortingOrder = 180;
                ShadowTransform.localScale = new Vector3(0.7f, 0.45f, 1f);
            }

            // Nếu script legacy lấy SpriteRenderer gốc
            if (legacySr != null && legacySr.sprite != null && VisualRenderer.sprite == null)
            {
                VisualRenderer.sprite = legacySr.sprite;
                VisualRenderer.color = legacySr.color;
                legacySr.enabled = false;
            }
        }

        public void ResetState()
        {
            StopAllCoroutines();
            EnsureRigging();

            Speed = 5.0f;
            Damage = 15f;
            Lifetime = 4.0f;
            IsFromPlayer = false;
            IsDeflected = false;
            Archetype = ProjectileArchetype.LinearBallistic;

            CausesBleed = false;
            BleedDps = 15f;
            BleedDuration = 3.5f;

            CausesSlimeSlow = false;
            SlimeSlowAmount = 0.30f;
            SlimeSlowDuration = 3.0f;

            HasTrail = false;
            TrailType = null;
            TrailColor = Color.white;
            trailTimer = 0f;

            ArcHeight = 1.8f;
            TotalDuration = 0.65f;
            OnLandCallback = null;

            moveDirection = Vector2.down;
            lifeTimer = 0f;
            isLaunched = false;
            spinSpeedDegPerSec = 0f;

            harpoonTargetTransform = null;
            chainLauncher = null;
            onHarpoonHit = null;

            transform.rotation = Quaternion.identity;

            if (VisualTransform != null)
            {
                VisualTransform.localPosition = Vector3.zero;
                VisualTransform.localRotation = Quaternion.identity;
                VisualTransform.localScale = Vector3.one;
            }

            if (VisualRenderer != null)
            {
                VisualRenderer.sprite = null;
                VisualRenderer.color = Color.white;
            }

            if (ShadowTransform != null)
            {
                ShadowTransform.localPosition = new Vector3(0f, -0.1f, 0f);
                ShadowTransform.localScale = new Vector3(0.7f, 0.45f, 1f);
            }

            if (chainLr != null)
            {
                chainLr.enabled = false;
            }
        }

        public void Despawn()
        {
            if (CombatObjectPool.Instance != null && IsPooled)
            {
                CombatObjectPool.Instance.RecycleProjectile(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // =========================================================================
        // INITIALIZERS
        // =========================================================================

        public void Initialize(Vector2 startPos, Vector2 direction, float damage, float speed = 5.0f, bool isPlayer = false)
        {
            EnsureRigging();
            Archetype = ProjectileArchetype.LinearBallistic;
            transform.position = startPos;
            moveDirection = direction.normalized;
            Damage = damage;
            Speed = speed;
            IsFromPlayer = isPlayer;
            lifeTimer = 0f;
            isLaunched = true;

            if (VisualRenderer.sprite == null)
            {
                VisualRenderer.sprite = UISpriteLoader.GetIronBoltSprite();
                VisualRenderer.color = isPlayer ? new Color(0.2f, 0.9f, 1f) : Color.white;
            }

            if (ShadowTransform != null)
            {
                ShadowTransform.localPosition = new Vector3(0f, -0.1f, 0f);
                ShadowTransform.localScale = new Vector3(0.5f, 0.3f, 1f);
            }

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        public void InitializeLinear(Vector2 startPos, Vector2 direction, float damage, float speed, float lifetime, Color tint, Sprite customSprite = null, Vector3? scale = null, string trailType = null, Color? trailColor = null)
        {
            EnsureRigging();
            Archetype = ProjectileArchetype.LinearBallistic;
            transform.position = startPos;
            moveDirection = direction.normalized;
            Damage = damage;
            Speed = speed;
            Lifetime = lifetime;
            IsFromPlayer = true;
            lifeTimer = 0f;
            isLaunched = true;

            if (!string.IsNullOrEmpty(trailType))
            {
                HasTrail = true;
                TrailType = trailType;
                TrailColor = trailColor ?? Color.white;
            }

            if (VisualRenderer != null)
            {
                VisualRenderer.sprite = customSprite ?? UISpriteLoader.GetIronBoltSprite();
                VisualRenderer.color = tint;
                VisualTransform.localScale = scale ?? new Vector3(0.55f, 0.55f, 1f);
            }

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        public void InitializeParabolic(Vector3 startPos, Vector3 targetPos, float arcHeight, float duration, float damage, Action onLand, Color color, Sprite customSprite = null, Vector3? visualScale = null, string trailType = null, Color? trailColor = null)
        {
            EnsureRigging();
            Archetype = ProjectileArchetype.ParabolicMortar;
            ArcHeight = arcHeight;
            TotalDuration = duration;
            Damage = damage;
            OnLandCallback = onLand;
            isLaunched = true;
            lifeTimer = 0f;
            Lifetime = duration + 0.15f;

            if (!string.IsNullOrEmpty(trailType))
            {
                HasTrail = true;
                TrailType = trailType;
                TrailColor = trailColor ?? color;
            }

            if (VisualRenderer != null)
            {
                VisualRenderer.sprite = customSprite ?? UISpriteLoader.GetQuantumOrbSprite();
                VisualRenderer.color = color;
                VisualTransform.localScale = visualScale ?? new Vector3(0.7f, 0.7f, 1f);
            }

            StartCoroutine(ParabolicRoutine(startPos, targetPos, arcHeight, duration, onLand));
        }

        public void InitializeSpinningSaw(Vector3 startPos, Vector3 targetPos, float arcHeight, float duration, float damage, Action onLand)
        {
            EnsureRigging();
            Archetype = ProjectileArchetype.SpinningSaw;
            ArcHeight = arcHeight;
            TotalDuration = duration;
            Damage = damage;
            OnLandCallback = onLand;
            spinSpeedDegPerSec = 1080f;
            isLaunched = true;
            Lifetime = duration + 0.15f;

            if (VisualRenderer != null)
            {
                VisualRenderer.sprite = UISpriteLoader.GetSawbladeSprite();
                VisualRenderer.color = Color.white;
                VisualTransform.localScale = new Vector3(0.95f, 0.95f, 1f);
            }

            StartCoroutine(ParabolicRoutine(startPos, targetPos, arcHeight, duration, () =>
            {
                // Gây sát thương và Bleed cho quái vật trong tâm chạm
                var hits = Physics2D.OverlapCircleAll(targetPos, 2.2f);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<EnemyAgentView>();
                    if (enemy != null && enemy.IsAlive)
                    {
                        enemy.TakeDamage(damage, (hit.transform.position - targetPos).normalized, 2.2f);
                        enemy.ApplyBleed(30f, 4.0f);
                    }
                }

                // Khi chạm đất: Bắn tung 8 mảnh đinh rỉ tỏa đều 8 hướng
                SpawnNailShrapnels(transform.position, 8, 30f);
                onLand?.Invoke();
            }));
        }

        public void InitializeBouncingAcid(Vector3 startPos, Vector3 targetPos, float arcHeight, float duration, float damage, Action onLand)
        {
            EnsureRigging();
            Archetype = ProjectileArchetype.BouncingAcid;
            Damage = damage;
            OnLandCallback = onLand;
            isLaunched = true;
            Lifetime = duration * 1.6f;

            if (VisualRenderer != null)
            {
                VisualRenderer.sprite = UISpriteLoader.GetAcidGlobSprite();
                VisualRenderer.color = Color.white;
                VisualTransform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }

            StartCoroutine(BouncingAcidRoutine(startPos, targetPos, arcHeight, duration, onLand));
        }

        public void InitializeHarpoon(Vector3 startPos, Transform target, float speed, float damage, Transform launcher, Action<Transform, Vector2> onHitPull)
        {
            EnsureRigging();
            Archetype = ProjectileArchetype.HarpoonChain;
            transform.position = startPos;
            Damage = damage;
            Speed = speed;
            harpoonTargetTransform = target;
            chainLauncher = launcher;
            onHarpoonHit = onHitPull;
            isLaunched = true;
            lifeTimer = 0f;
            Lifetime = 2.5f;

            if (VisualRenderer != null)
            {
                VisualRenderer.sprite = UISpriteLoader.GetHarpoonSpearSprite();
                VisualRenderer.color = Color.white;
                VisualTransform.localScale = new Vector3(0.85f, 0.85f, 1f);
            }

            EnsureChainLineRenderer();
            if (chainLr != null)
            {
                chainLr.enabled = true;
                chainLr.startWidth = 0.08f;
                chainLr.endWidth = 0.08f;
                chainLr.startColor = new Color(0.75f, 0.75f, 0.75f, 0.95f);
                chainLr.endColor = new Color(0.45f, 0.45f, 0.5f, 0.95f);
                chainLr.positionCount = 2;
                chainLr.SetPosition(0, launcher != null ? launcher.position : startPos);
                chainLr.SetPosition(1, startPos);
            }
        }

        private void EnsureChainLineRenderer()
        {
            if (chainLr != null) return;
            var chainObj = transform.Find("HarpoonChain_Line");
            if (chainObj == null)
            {
                var newChain = new GameObject("HarpoonChain_Line");
                newChain.transform.SetParent(transform, false);
                chainLr = newChain.AddComponent<LineRenderer>();
            }
            else
            {
                chainLr = chainObj.GetComponent<LineRenderer>();
            }

            if (chainLr != null)
            {
                chainLr.material = new Material(Shader.Find("Sprites/Default"));
                chainLr.sortingOrder = 310;
            }
        }

        // =========================================================================
        // UPDATE & TICK LOGIC
        // =========================================================================

        private void Update()
        {
            if (!isLaunched) return;

            // Xoay lưỡi cưa Z-axis nếu là B2
            if (spinSpeedDegPerSec > 0f && VisualTransform != null)
            {
                VisualTransform.Rotate(0f, 0f, -spinSpeedDegPerSec * Time.deltaTime);
            }

            // Tạo vệt hiệu ứng TrailRenderer / Pooled particle trail
            if (HasTrail)
            {
                trailTimer += Time.deltaTime;
                if (trailTimer >= 0.045f)
                {
                    trailTimer = 0f;
                    SpawnTrailingVFX();
                }
            }

            // Cập nhật xích móc B4
            if (Archetype == ProjectileArchetype.HarpoonChain && chainLr != null && chainLauncher != null)
            {
                chainLr.SetPosition(0, chainLauncher.position);
                chainLr.SetPosition(1, transform.position);
            }

            if (Archetype == ProjectileArchetype.LinearBallistic)
            {
                lifeTimer += Time.deltaTime;
                if (lifeTimer >= Lifetime)
                {
                    Despawn();
                    return;
                }

                transform.position += (Vector3)(moveDirection * (Speed * Time.deltaTime));
                CheckLinearCollisions();
            }
            else if (Archetype == ProjectileArchetype.HarpoonChain)
            {
                lifeTimer += Time.deltaTime;
                if (lifeTimer >= Lifetime || harpoonTargetTransform == null)
                {
                    Despawn();
                    return;
                }

                Vector2 toTarget = ((Vector2)harpoonTargetTransform.position - (Vector2)transform.position);
                float dist = toTarget.magnitude;
                float step = Speed * Time.deltaTime;

                if (dist <= step || dist < 0.35f)
                {
                    transform.position = harpoonTargetTransform.position;
                    onHarpoonHit?.Invoke(harpoonTargetTransform, toTarget.normalized);
                    StartCoroutine(TenseAndFadeChain(0.2f));
                    isLaunched = false;
                    return;
                }
                else
                {
                    transform.position += (Vector3)(toTarget.normalized * step);
                    float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }
            }
        }

        private void SpawnTrailingVFX()
        {
            if (CombatObjectPool.Instance == null) return;

            Vector3 spawnPos = VisualTransform != null ? VisualTransform.position : transform.position;

            switch (TrailType)
            {
                case "quantum": // A2: Hạt lượng tử tím
                    CombatObjectPool.Instance.SpawnVFX(spawnPos + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.15f), UISpriteLoader.GetSoftCircleSprite(), new Color(0.72f, 0.25f, 1f, 0.75f), new Vector3(0.22f, 0.22f, 1f), 0.25f, -moveDirection * 0.8f);
                    break;
                case "smoke": // B5: Đuôi khói xám đặc
                    CombatObjectPool.Instance.SpawnVFX(spawnPos + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f), UISpriteLoader.GetSoftCircleSprite(), new Color(0.25f, 0.25f, 0.28f, 0.75f), new Vector3(0.38f, 0.38f, 1f), 0.45f, -moveDirection * 1.2f, UnityEngine.Random.Range(-90f, 90f));
                    break;
                case "wind": // B1: Vệt gió trắng ngắn
                    CombatObjectPool.Instance.SpawnVFX(spawnPos, UISpriteLoader.GetSolidWhiteSprite(), new Color(1f, 1f, 1f, 0.55f), new Vector3(0.10f, 0.35f, 1f), 0.15f, -moveDirection * 2.0f);
                    break;
                case "slime": // C1: Giọt nhầy tím
                    CombatObjectPool.Instance.SpawnVFX(spawnPos, UISpriteLoader.GetSoftCircleSprite(), new Color(0.68f, 0.15f, 0.85f, 0.65f), new Vector3(0.18f, 0.18f, 1f), 0.3f, Vector2.down * 0.6f);
                    break;
            }
        }

        private void CheckLinearCollisions()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.28f);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    // Đạn quái vật -> trúng người chơi
                    if (!IsFromPlayer && hit.GetComponent<PlayerCombatController>() != null)
                    {
                        damageable.TakeDamage(Damage, moveDirection, 2.0f);
                        DestroyWithSpark();
                        return;
                    }
                    // Đạn tháp / người chơi / phản xạ -> trúng quái vật
                    else if (IsFromPlayer && hit.GetComponent<EnemyAgentView>() != null)
                    {
                        var enemy = hit.GetComponent<EnemyAgentView>();
                        damageable.TakeDamage(Damage, moveDirection, 2.5f);

                        if (CausesBleed && enemy != null)
                        {
                            enemy.ApplyBleed(BleedDps, BleedDuration);
                        }

                        if (CausesSlimeSlow && enemy != null)
                        {
                            enemy.ApplySlow(SlimeSlowAmount, SlimeSlowDuration);
                            CombatObjectPool.Instance?.SpawnVFX(transform.position, UISpriteLoader.GetSoftCircleSprite(), new Color(0.75f, 0.2f, 0.88f, 0.85f), new Vector3(0.4f, 0.4f, 1f), 0.35f);
                        }

                        // Lóe tia lửa vàng cam kim loại khi trúng quái (B1)
                        if (Archetype == ProjectileArchetype.LinearBallistic)
                        {
                            CombatObjectPool.Instance?.SpawnVFX(transform.position, UISpriteLoader.GetSoftCircleSprite(), new Color(1f, 0.65f, 0.15f, 0.95f), new Vector3(0.35f, 0.35f, 1f), 0.10f);
                        }

                        DestroyWithSpark();
                        return;
                    }
                    // Trúng bia tập đánh TrainingDummy
                    else if (IsFromPlayer && hit.GetComponent<TrainingDummy2DView>() != null)
                    {
                        damageable.TakeDamage(Damage, moveDirection, 2.5f);
                        DestroyWithSpark();
                        return;
                    }
                }
            }
        }

        // =========================================================================
        // PROCEDURAL COROUTINES & PARABOLIC CALCULATIONS
        // =========================================================================

        private IEnumerator ParabolicRoutine(Vector3 start, Vector3 end, float arcHeight, float duration, Action onLand)
        {
            float elapsed = 0f;
            transform.position = start;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Bóng trượt tuyến tính theo mặt đất
                Vector3 groundPos = Vector3.Lerp(start, end, t);
                transform.position = groundPos;

                // Chiều cao Parabol giả lập 2.5D: H(t) = 4 * h * t * (1 - t)
                float currentHeight = 4f * arcHeight * t * (1f - t);

                if (VisualTransform != null)
                {
                    VisualTransform.localPosition = new Vector3(0f, currentHeight, 0f);
                }

                if (ShadowTransform != null && ShadowRenderer != null)
                {
                    float shadowRatio = Mathf.Clamp(1f - (currentHeight / (arcHeight * 1.8f)), 0.45f, 1.0f);
                    ShadowTransform.localScale = new Vector3(0.7f * shadowRatio, 0.45f * shadowRatio, 1f);
                    ShadowRenderer.color = new Color(0f, 0f, 0f, 0.45f * shadowRatio);
                }

                yield return null;
            }

            transform.position = end;
            if (VisualTransform != null) VisualTransform.localPosition = Vector3.zero;
            onLand?.Invoke();
            Despawn();
        }

        private IEnumerator BouncingAcidRoutine(Vector3 start, Vector3 target, float arcHeight, float duration, Action onLand)
        {
            // Nhịp 1: Bay tới điểm đích (65% tổng thời gian)
            float t1Dur = duration * 0.65f;
            float elapsed = 0f;

            while (elapsed < t1Dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / t1Dur);
                transform.position = Vector3.Lerp(start, target, t);
                float h = 4f * arcHeight * t * (1f - t);

                if (VisualTransform != null) VisualTransform.localPosition = new Vector3(0f, h, 0f);
                if (ShadowTransform != null && ShadowRenderer != null)
                {
                    float sr = Mathf.Clamp(1f - (h / (arcHeight * 1.8f)), 0.45f, 1.0f);
                    ShadowTransform.localScale = new Vector3(0.7f * sr, 0.45f * sr, 1f);
                }
                yield return null;
            }

            // Nhịp chạm 1 (Impact bounce): để lại đốm axit nhỏ dưới đất
            CombatObjectPool.Instance?.SpawnGroundZone(target, 0.8f, 3.0f, UISpriteLoader.GetSoftCircleSprite(), new Color(0.2f, 0.9f, 0.15f, 0.45f), (e) =>
            {
                e.TakeDamage(12f * Time.deltaTime, Vector2.zero, 0f);
                e.ApplyVulnerability(1.2f, 1.5f);
            }, 0.25f, 165);

            Vector2 bounceDir = ((Vector2)target - (Vector2)start).normalized;
            Vector3 bounceTarget = target + (Vector3)(bounceDir * 1.1f);
            float t2Dur = duration * 0.35f;
            float bounceHeight = arcHeight * 0.35f;
            elapsed = 0f;

            while (elapsed < t2Dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / t2Dur);
                transform.position = Vector3.Lerp(target, bounceTarget, t);
                float h = 4f * bounceHeight * t * (1f - t);

                if (VisualTransform != null) VisualTransform.localPosition = new Vector3(0f, h, 0f);
                if (ShadowTransform != null)
                {
                    float sr = Mathf.Clamp(1f - (h / (bounceHeight * 2f)), 0.6f, 1.0f);
                    ShadowTransform.localScale = new Vector3(0.7f * sr, 0.45f * sr, 1f);
                }
                yield return null;
            }

            // Chạm nhịp 2 -> Vỡ toang thành vũng axit sủi bọt làm mòn giáp trong 5s
            transform.position = bounceTarget;
            SpawnAcidPuddle(bounceTarget, 2.4f, 5.0f, Damage * 0.45f);
            onLand?.Invoke();
            Despawn();
        }

        private IEnumerator TenseAndFadeChain(float duration)
        {
            if (chainLr != null)
            {
                chainLr.startColor = Color.white;
                chainLr.endColor = Color.white;
                chainLr.startWidth = 0.12f;
                chainLr.endWidth = 0.12f;
            }

            yield return new WaitForSeconds(duration);

            float fade = 0.15f;
            float e = 0f;
            while (e < fade)
            {
                e += Time.deltaTime;
                float a = 1f - (e / fade);
                if (chainLr != null)
                {
                    chainLr.startColor = new Color(0.7f, 0.7f, 0.7f, a);
                    chainLr.endColor = new Color(0.4f, 0.4f, 0.4f, a);
                }
                yield return null;
            }

            Despawn();
        }

        // =========================================================================
        // SPECIALIZED SHRAPNEL & SUB-EFFECTS (POOLED)
        // =========================================================================

        private void SpawnNailShrapnels(Vector3 center, int count, float shrapnelDamage)
        {
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                CombatProjectile2D proj = CombatObjectPool.Instance != null 
                    ? CombatObjectPool.Instance.SpawnProjectile() 
                    : new GameObject($"Shrapnel_Nail_{i}").AddComponent<CombatProjectile2D>();

                proj.InitializeLinear(center, dir, shrapnelDamage, 8.5f, 0.45f, new Color(0.95f, 0.6f, 0.25f), UISpriteLoader.GetNailSprite(), new Vector3(0.55f, 0.55f, 1f));
                proj.CausesBleed = true;
                proj.BleedDps = 15f;
                proj.BleedDuration = 3.5f;
            }
        }

        private void SpawnAcidPuddle(Vector3 center, float radius, float duration, float dps)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(center, radius, duration, UISpriteLoader.GetSoftCircleSprite(), new Color(0.2f, 0.85f, 0.15f, 0.55f), (enemy) =>
                {
                    enemy.TakeDamage(dps * Time.deltaTime, Vector2.zero, 0f);
                    enemy.ApplySlow(0.35f, 0.8f);
                    enemy.ApplyVulnerability(1.35f, 2.5f); // Làm mòn giáp (+35% sát thương nhận vào)
                }, 0.2f, 170);
            }
            else
            {
                var puddleObj = new GameObject("AcidPuddle_Lingering");
                puddleObj.transform.position = center;
                var sr = puddleObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetSoftCircleSprite();
                sr.color = new Color(0.2f, 0.85f, 0.15f, 0.55f);
                sr.sortingOrder = 170;
                puddleObj.transform.localScale = new Vector3(radius * 1.8f, radius * 1.2f, 1f);
                var controller = puddleObj.AddComponent<AcidPuddleController>();
                controller.Initialize(radius, duration, dps);
            }
        }

        // =========================================================================
        // STARDEW 1.6 DEFLECTION & SPARK DESTRUCTION
        // =========================================================================

        public void Deflect(Vector2 newDirection)
        {
            if (IsDeflected) return;

            IsDeflected = true;
            IsFromPlayer = true;
            moveDirection = newDirection.normalized;
            Speed *= 1.4f;
            lifeTimer = 0f;

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            if (VisualRenderer != null)
            {
                VisualRenderer.color = new Color(0.2f, 0.95f, 1f, 1f); // Xanh ngọc phản kích
            }
        }

        public void DestroyWithSpark()
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnVFX(transform.position, UISpriteLoader.GetSoftCircleSprite(), new Color(1f, 0.9f, 0.4f, 0.9f), new Vector3(0.35f, 0.35f, 1f), 0.08f);
            }
            Despawn();
        }
    }

    /// <summary>
    /// Vùng vũng axit sủi bọt (C3) ăn mòn quái vật và làm chậm
    /// </summary>
    public class AcidPuddleController : MonoBehaviour
    {
        private float radius;
        private float duration;
        private float dps;
        private float elapsed = 0f;
        private SpriteRenderer sr;
        private Vector3 baseScale;

        public void Initialize(float r, float dur, float damagePerSec)
        {
            radius = r;
            duration = dur;
            dps = damagePerSec;
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            float pulse = Mathf.Sin(Time.time * 6.0f) * 0.06f;
            transform.localScale = new Vector3(baseScale.x * (1f + pulse), baseScale.y * (1f - pulse), 1f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var col in hits)
            {
                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(dps * Time.deltaTime, Vector2.zero, 0f);
                    enemy.ApplySlow(0.35f, 0.8f);
                    enemy.ApplyVulnerability(1.35f, 2.5f);
                }
            }

            if (elapsed > duration - 1.0f && sr != null)
            {
                float remainRatio = (duration - elapsed) / 1.0f;
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.55f * remainRatio);
            }
        }
    }
}
