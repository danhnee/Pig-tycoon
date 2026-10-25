using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Hệ thống Object Pooling tối ưu hóa hiệu năng cực cao (Zero-allocation Combat Pooling).
    /// Quản lý tái sử dụng:
    /// 1. CombatProjectile2D: Đạn nỏ sắt B1, Cưa gai B2, Hắc ín B3, Lao xích B4, Đại pháo B5, Đạn lượng tử A2, Lôi cầu A4, Gai nhọn C1, Axit C3...
    /// 2. PooledVFX: Tia lửa kim loại, Tia plasma, Khói đen đặc, Bụi dăm gỗ, Đinh văng, Sóng chấn động EMP, Mote phản trọng lực...
    /// 3. PooledGroundZone: Vùng bức xạ lượng tử A2, Vết cháy nung chảy A1, Than hồng bốc khói B3, Hố cháy đại pháo B5, Mây bào tử C2, Vũng axit C3, Vòng ấn câm lặng C5, Khóa xích B4...
    /// </summary>
    public class CombatObjectPool : MonoBehaviour
    {
        public static CombatObjectPool Instance { get; private set; }

        [Header("Pool Capacities")]
        public int InitialProjectilePoolSize = 60;
        public int InitialVfxPoolSize = 80;
        public int InitialGroundZonePoolSize = 30;

        private readonly Queue<CombatProjectile2D> projectilePool = new Queue<CombatProjectile2D>();
        private readonly Queue<PooledVFX> vfxPool = new Queue<PooledVFX>();
        private readonly Queue<PooledGroundZone> groundZonePool = new Queue<PooledGroundZone>();

        private Transform projectileRoot;
        private Transform vfxRoot;
        private Transform groundZoneRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePoolRoots();
            WarmupPools();
        }

        public static void EnsurePoolExists()
        {
            if (Instance == null)
            {
                var poolObj = new GameObject("[CombatObjectPool]");
                Instance = poolObj.AddComponent<CombatObjectPool>();
            }
        }

        private void InitializePoolRoots()
        {
            if (projectileRoot == null)
            {
                var pRoot = new GameObject("Projectiles_Pool");
                pRoot.transform.SetParent(transform, false);
                projectileRoot = pRoot.transform;
            }

            if (vfxRoot == null)
            {
                var vRoot = new GameObject("VFX_Pool");
                vRoot.transform.SetParent(transform, false);
                vfxRoot = vRoot.transform;
            }

            if (groundZoneRoot == null)
            {
                var gzRoot = new GameObject("GroundZones_Pool");
                gzRoot.transform.SetParent(transform, false);
                groundZoneRoot = gzRoot.transform;
            }
        }

        private void WarmupPools()
        {
            for (int i = 0; i < InitialProjectilePoolSize; i++)
            {
                var proj = CreateNewProjectile();
                proj.gameObject.SetActive(false);
                projectilePool.Enqueue(proj);
            }

            for (int i = 0; i < InitialVfxPoolSize; i++)
            {
                var vfx = CreateNewVFX();
                vfx.gameObject.SetActive(false);
                vfxPool.Enqueue(vfx);
            }

            for (int i = 0; i < InitialGroundZonePoolSize; i++)
            {
                var zone = CreateNewGroundZone();
                zone.gameObject.SetActive(false);
                groundZonePool.Enqueue(zone);
            }
        }

        // =========================================================================
        // PROJECTILE POOLING
        // =========================================================================

        private CombatProjectile2D CreateNewProjectile()
        {
            var go = new GameObject("Pooled_CombatProjectile");
            go.transform.SetParent(projectileRoot, false);
            var proj = go.AddComponent<CombatProjectile2D>();
            proj.IsPooled = true;
            return proj;
        }

        public CombatProjectile2D SpawnProjectile()
        {
            CombatProjectile2D proj;
            if (projectilePool.Count > 0)
            {
                proj = projectilePool.Dequeue();
                if (proj == null) proj = CreateNewProjectile();
            }
            else
            {
                proj = CreateNewProjectile();
            }

            proj.transform.SetParent(null);
            proj.gameObject.SetActive(true);
            proj.ResetState();
            return proj;
        }

        public void RecycleProjectile(CombatProjectile2D proj)
        {
            if (proj == null) return;
            proj.StopAllCoroutines();
            proj.ResetState();
            proj.gameObject.SetActive(false);
            if (projectileRoot != null) proj.transform.SetParent(projectileRoot, false);
            projectilePool.Enqueue(proj);
        }

        // =========================================================================
        // VFX POOLING
        // =========================================================================

        private PooledVFX CreateNewVFX()
        {
            var go = new GameObject("Pooled_VFX");
            go.transform.SetParent(vfxRoot, false);
            return go.AddComponent<PooledVFX>();
        }

        public PooledVFX SpawnVFX(Vector3 pos, Sprite sprite, Color color, Vector3 scale, float duration, Vector2 velocity = default, float rotationSpeed = 0f, int sortingOrder = 350, Action onComplete = null)
        {
            PooledVFX vfx;
            if (vfxPool.Count > 0)
            {
                vfx = vfxPool.Dequeue();
                if (vfx == null) vfx = CreateNewVFX();
            }
            else
            {
                vfx = CreateNewVFX();
            }

            vfx.transform.SetParent(null);
            vfx.transform.position = pos;
            vfx.transform.rotation = Quaternion.identity;
            vfx.transform.localScale = scale;
            vfx.gameObject.SetActive(true);
            vfx.Play(sprite, color, duration, velocity, rotationSpeed, sortingOrder, () =>
            {
                onComplete?.Invoke();
                RecycleVFX(vfx);
            });
            return vfx;
        }

        public void RecycleVFX(PooledVFX vfx)
        {
            if (vfx == null) return;
            vfx.StopAllCoroutines();
            vfx.gameObject.SetActive(false);
            if (vfxRoot != null) vfx.transform.SetParent(vfxRoot, false);
            vfxPool.Enqueue(vfx);
        }

        // =========================================================================
        // GROUND ZONE POOLING
        // =========================================================================

        private PooledGroundZone CreateNewGroundZone()
        {
            var go = new GameObject("Pooled_GroundZone");
            go.transform.SetParent(groundZoneRoot, false);
            return go.AddComponent<PooledGroundZone>();
        }

        public PooledGroundZone SpawnGroundZone(Vector3 center, float radius, float duration, Sprite sprite, Color color, Action<EnemyAgentView> onTick, float tickInterval = 0.25f, int sortingOrder = 160, Action onExpire = null)
        {
            PooledGroundZone zone;
            if (groundZonePool.Count > 0)
            {
                zone = groundZonePool.Dequeue();
                if (zone == null) zone = CreateNewGroundZone();
            }
            else
            {
                zone = CreateNewGroundZone();
            }

            zone.transform.SetParent(null);
            zone.transform.position = center;
            zone.transform.rotation = Quaternion.identity;
            zone.gameObject.SetActive(true);
            zone.Activate(center, radius, duration, sprite, color, onTick, tickInterval, sortingOrder, () =>
            {
                onExpire?.Invoke();
                RecycleGroundZone(zone);
            });
            return zone;
        }

        public void RecycleGroundZone(PooledGroundZone zone)
        {
            if (zone == null) return;
            zone.StopAllCoroutines();
            zone.gameObject.SetActive(false);
            if (groundZoneRoot != null) zone.transform.SetParent(groundZoneRoot, false);
            groundZonePool.Enqueue(zone);
        }
    }

    /// <summary>
    /// Đối tượng VFX dùng chung trong Object Pool (hạt sáng, mảnh văng, khói, lóe lửa)
    /// </summary>
    public class PooledVFX : MonoBehaviour
    {
        public SpriteRenderer Sr { get; private set; }
        private Coroutine playRoutine;

        private void Awake()
        {
            Sr = GetComponent<SpriteRenderer>();
            if (Sr == null) Sr = gameObject.AddComponent<SpriteRenderer>();
        }

        public void Play(Sprite sprite, Color color, float duration, Vector2 velocity, float rotationSpeed, int sortingOrder, Action onFinished)
        {
            if (Sr == null) Sr = GetComponent<SpriteRenderer>();
            Sr.sprite = sprite ?? UISpriteLoader.GetSolidWhiteSprite();
            Sr.color = color;
            Sr.sortingOrder = sortingOrder;

            if (playRoutine != null) StopCoroutine(playRoutine);
            playRoutine = StartCoroutine(AnimateRoutine(duration, velocity, rotationSpeed, color, onFinished));
        }

        private IEnumerator AnimateRoutine(float duration, Vector2 velocity, float rotationSpeed, Color initColor, Action onFinished)
        {
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (velocity != Vector2.zero)
                {
                    transform.position += (Vector3)(velocity * (Time.deltaTime * (1f - t * 0.4f)));
                }

                if (rotationSpeed != 0f)
                {
                    transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
                }

                // Fade alpha mượt mà về cuối
                float a = Mathf.Lerp(initColor.a, 0f, t);
                Sr.color = new Color(initColor.r, initColor.g, initColor.b, a);

                yield return null;
            }

            playRoutine = null;
            onFinished?.Invoke();
        }
    }

    /// <summary>
    /// Vùng hiệu ứng mặt đất dùng chung trong Object Pool (Bức xạ A2, Vết nung A1, Than hồng B3, Bào tử C2, Axit C3...)
    /// </summary>
    public class PooledGroundZone : MonoBehaviour
    {
        public SpriteRenderer Sr { get; private set; }
        private Coroutine zoneRoutine;

        private void Awake()
        {
            Sr = GetComponent<SpriteRenderer>();
            if (Sr == null) Sr = gameObject.AddComponent<SpriteRenderer>();
        }

        public void Activate(Vector3 center, float radius, float duration, Sprite sprite, Color color, Action<EnemyAgentView> onTick, float tickInterval, int sortingOrder, Action onFinished)
        {
            if (Sr == null) Sr = GetComponent<SpriteRenderer>();
            Sr.sprite = sprite ?? UISpriteLoader.GetSoftCircleSprite();
            Sr.color = color;
            Sr.sortingOrder = sortingOrder;
            transform.position = center;
            transform.localScale = new Vector3(radius * 2.0f, radius * 1.35f, 1f);

            if (zoneRoutine != null) StopCoroutine(zoneRoutine);
            zoneRoutine = StartCoroutine(ZoneExecutionRoutine(radius, duration, color, onTick, tickInterval, onFinished));
        }

        private IEnumerator ZoneExecutionRoutine(float radius, float duration, Color initColor, Action<EnemyAgentView> onTick, float tickInterval, Action onFinished)
        {
            float elapsed = 0f;
            float tickTimer = 0f;
            Vector3 baseScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                tickTimer += Time.deltaTime;

                // Hiệu ứng dao động nhẹ nhấp nháy / sủi bọt (Pulse)
                float pulse = Mathf.Sin(Time.time * 6.5f) * 0.05f;
                transform.localScale = new Vector3(baseScale.x * (1f + pulse), baseScale.y * (1f - pulse), 1f);

                // Tick sát thương & Debuff lên kẻ địch trong vùng
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
                    foreach (var hit in hits)
                    {
                        var enemy = hit.GetComponent<EnemyAgentView>();
                        if (enemy != null && enemy.IsAlive)
                        {
                            onTick?.Invoke(enemy);
                        }
                    }
                }

                // Mờ dần ở 1 giây cuối
                if (elapsed > duration - 1.0f)
                {
                    float fadeRatio = (duration - elapsed) / 1.0f;
                    Sr.color = new Color(initColor.r, initColor.g, initColor.b, initColor.a * fadeRatio);
                }

                yield return null;
            }

            zoneRoutine = null;
            onFinished?.Invoke();
        }
    }
}
