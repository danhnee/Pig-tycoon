using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    public class FarmEnvironment2D : MonoBehaviour
    {
        public static FarmEnvironment2D Instance { get; private set; }

        [Header("Prairie Map & Pasture Bounds")]
        [Tooltip("Rìa toàn bộ bản đồ thảo nguyên (Camera follow bị giới hạn tại đây)")]
        public Rect MapBounds = new Rect(-45f, -32f, 90f, 64f);

        [Tooltip("Khu vực hàng rào chăn thả heo (Heo chỉ đi dạo trong khu vực này)")]
        public Rect PigPastureBounds = new Rect(-20f, -14f, 40f, 28f);

        public Rect FarmBounds
        {
            get => MapBounds;
            set => MapBounds = value;
        }

        [Header("Registered Infrastructure")]
        public List<Fence2DView> Fences = new List<Fence2DView>();
        public List<Feeder2DView> Feeders = new List<Feeder2DView>();
        public List<WaterTrough2DView> WaterTroughs = new List<WaterTrough2DView>();
        public List<MudPit2DView> MudPits = new List<MudPit2DView>();
        public List<Shelter2DView> Shelters = new List<Shelter2DView>();
        public CorpseLot2DView CorpseLot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Tự động điều chỉnh Sorting Order của Tilemap nếu scene cũ có Ground >= -100
            // Đảm bảo nhân vật và thú nuôi không bao giờ bị chìm dưới nền cỏ ở nửa trên map
            var tilemapRenderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (var tr in tilemapRenderers)
            {
                if (tr.sortingOrder >= -100)
                {
                    if (tr.gameObject.name.Contains("Ground")) tr.sortingOrder = -10000;
                    else if (tr.gameObject.name.Contains("Path")) tr.sortingOrder = -9900;
                    else if (tr.gameObject.name.Contains("Water")) tr.sortingOrder = -9800;
                    else if (tr.gameObject.name.Contains("Mud")) tr.sortingOrder = -9700;
                    else tr.sortingOrder = -9600;
                }
            }

            RefreshInfrastructureRegistries();
        }

        public void RefreshInfrastructureRegistries()
        {
            Fences.Clear();
            Fences.AddRange(FindObjectsByType<Fence2DView>());

            Feeders.Clear();
            Feeders.AddRange(FindObjectsByType<Feeder2DView>());

            WaterTroughs.Clear();
            WaterTroughs.AddRange(FindObjectsByType<WaterTrough2DView>());

            MudPits.Clear();
            MudPits.AddRange(FindObjectsByType<MudPit2DView>());

            Shelters.Clear();
            Shelters.AddRange(FindObjectsByType<Shelter2DView>());

            if (CorpseLot == null)
            {
                CorpseLot = FindAnyObjectByType<CorpseLot2DView>();
            }
        }

        public Feeder2DView GetNearestFeeder(Vector2 position, bool requireFood = true)
        {
            Feeder2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < Feeders.Count; i++)
            {
                var feeder = Feeders[i];
                if (feeder == null || (requireFood && !feeder.HasFood)) continue;

                float distSqr = ((Vector2)feeder.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = feeder;
                }
            }

            return nearest;
        }

        public WaterTrough2DView GetNearestWaterTrough(Vector2 position, bool requireWater = true)
        {
            WaterTrough2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < WaterTroughs.Count; i++)
            {
                var trough = WaterTroughs[i];
                if (trough == null || (requireWater && !trough.HasWater)) continue;

                float distSqr = ((Vector2)trough.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = trough;
                }
            }

            return nearest;
        }

        public MudPit2DView GetNearestMudPit(Vector2 position)
        {
            MudPit2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < MudPits.Count; i++)
            {
                var pit = MudPits[i];
                if (pit == null) continue;

                float distSqr = ((Vector2)pit.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = pit;
                }
            }

            return nearest;
        }

        public Shelter2DView GetNearestShelter(Vector2 position, bool requireCapacity = true)
        {
            Shelter2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < Shelters.Count; i++)
            {
                var shelter = Shelters[i];
                if (shelter == null || (requireCapacity && !shelter.HasCapacity)) continue;

                float distSqr = ((Vector2)shelter.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = shelter;
                }
            }

            return nearest;
        }

        public bool IsInsideFarm(Vector2 position)
        {
            return MapBounds.Contains(position);
        }

        public bool IsInsidePasture(Vector2 position)
        {
            return PigPastureBounds.Contains(position);
        }

        // Đàn heo chỉ đi dạo trong khuôn viên rào chăn thả (PigPastureBounds)
        public Vector2 ClampInsideFarm(Vector2 position, float padding = 1f)
        {
            float clampedX = Mathf.Clamp(position.x, PigPastureBounds.xMin + padding, PigPastureBounds.xMax - padding);
            float clampedY = Mathf.Clamp(position.y, PigPastureBounds.yMin + padding, PigPastureBounds.yMax - padding);
            return new Vector2(clampedX, clampedY);
        }

        // Toàn bộ bản đồ thảo nguyên rộng lớn
        public Vector2 ClampInsideMap(Vector2 position, float padding = 1f)
        {
            float clampedX = Mathf.Clamp(position.x, MapBounds.xMin + padding, MapBounds.xMax - padding);
            float clampedY = Mathf.Clamp(position.y, MapBounds.yMin + padding, MapBounds.yMax - padding);
            return new Vector2(clampedX, clampedY);
        }

        public Vector2 GetRandomPositionInsideFarm(float padding = 2f)
        {
            float randX = Random.Range(PigPastureBounds.xMin + padding, PigPastureBounds.xMax - padding);
            float randY = Random.Range(PigPastureBounds.yMin + padding, PigPastureBounds.yMax - padding);
            return new Vector2(randX, randY);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(MapBounds.center, MapBounds.size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(PigPastureBounds.center, PigPastureBounds.size);
        }
    }
}
