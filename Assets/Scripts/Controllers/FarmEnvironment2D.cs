using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    public class FarmEnvironment2D : MonoBehaviour
    {
        public static FarmEnvironment2D Instance { get; private set; }

        [Header("Farm Boundaries (Top-down 2D)")]
        public Rect FarmBounds = new Rect(-16f, -12f, 32f, 24f);

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
                CorpseLot = FindFirstObjectByType<CorpseLot2DView>();
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
            return FarmBounds.Contains(position);
        }

        public Vector2 ClampInsideFarm(Vector2 position, float padding = 1f)
        {
            float clampedX = Mathf.Clamp(position.x, FarmBounds.xMin + padding, FarmBounds.xMax - padding);
            float clampedY = Mathf.Clamp(position.y, FarmBounds.yMin + padding, FarmBounds.yMax - padding);
            return new Vector2(clampedX, clampedY);
        }

        public Vector2 GetRandomPositionInsideFarm(float padding = 2f)
        {
            float randX = Random.Range(FarmBounds.xMin + padding, FarmBounds.xMax - padding);
            float randY = Random.Range(FarmBounds.yMin + padding, FarmBounds.yMax - padding);
            return new Vector2(randX, randY);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(FarmBounds.center, FarmBounds.size);
        }
    }
}
