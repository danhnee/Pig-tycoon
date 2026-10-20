using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum FlowDestinationGroup
    {
        Feeder,
        WaterTrough,
        MudPit,
        Shelter,
        HerdRefuge,
        IsolationZone,
        WhistlePoint,
        GateExit
    }

    public readonly struct FlowVector2D
    {
        public float X { get; }
        public float Y { get; }

        public FlowVector2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y);

        public FlowVector2D Normalized()
        {
            float m = Magnitude;
            if (m < 0.0001f) return new FlowVector2D(0f, 0f);
            return new FlowVector2D(X / m, Y / m);
        }
    }

    [Serializable]
    public sealed class PigSteeringAgent
    {
        public string PigId = "";
        public float X;
        public float Y;
        public float BaseSpeedMetersPerSec = 2.6f;
        public GeneLineId GeneLine = GeneLineId.HongDien;
        public bool IsPanicking;
        public bool IsIsolated;
        public bool IsAlive = true;
        public int Mood = 70;
    }

    /// <summary>
    /// Hệ thống điều hướng Flow-Field + Steering theo nhóm đích (GDD 3.6–3.7, P1.03, P2.23).
    /// Chạy ở 1 Hz cho tối đa 300 heo trên lưới 1m mà không cần A* riêng cho từng cá thể.
    /// Bao gồm lệnh "Lùa về Nơi Trú" (≥90% đàn về trong ≤2 giờ game) và "Tiếng Huýt Chăn Đàn" của Khoa.
    /// </summary>
    public sealed class HerdFlowField
    {
        public int OriginX { get; }
        public int OriginY { get; }
        public int Width { get; }
        public int Height { get; }

        private readonly bool[,] blockedCells;
        private readonly Dictionary<FlowDestinationGroup, int[,]> distanceMaps = new Dictionary<FlowDestinationGroup, int[,]>();
        private readonly Dictionary<FlowDestinationGroup, FlowVector2D[,]> vectorMaps = new Dictionary<FlowDestinationGroup, FlowVector2D[,]>();
        private readonly Dictionary<FlowDestinationGroup, List<(int gx, int gy)>> groupTargets = new Dictionary<FlowDestinationGroup, List<(int gx, int gy)>>();

        // Bộ đệm tái sử dụng Zero-GC cho RebuildFlowField
        private readonly byte[] _obsFlatScratch;
        private readonly int[] _outDistFlatScratch;
        private readonly float[] _outVxFlatScratch;
        private readonly float[] _outVyFlatScratch;
        private int[] _txScratch = new int[16];
        private int[] _tyScratch = new int[16];

        private const int UnreachableDistance = 999999;

        public HerdFlowField(int originX = -45, int originY = -32, int width = 90, int height = 64)
        {
            OriginX = originX;
            OriginY = originY;
            Width = Math.Max(10, width);
            Height = Math.Max(10, height);
            blockedCells = new bool[Width, Height];

            int n = Width * Height;
            _obsFlatScratch = new byte[n];
            _outDistFlatScratch = new int[n];
            _outVxFlatScratch = new float[n];
            _outVyFlatScratch = new float[n];

            foreach (FlowDestinationGroup group in Enum.GetValues(typeof(FlowDestinationGroup)))
            {
                groupTargets[group] = new List<(int gx, int gy)>();
                distanceMaps[group] = new int[Width, Height];
                vectorMaps[group] = new FlowVector2D[Width, Height];
            }
        }

        public (int gx, int gy) WorldToGrid(float worldX, float worldY)
        {
            int gx = (int)Math.Floor(worldX - OriginX);
            int gy = (int)Math.Floor(worldY - OriginY);
            gx = Math.Max(0, Math.Min(Width - 1, gx));
            gy = Math.Max(0, Math.Min(Height - 1, gy));
            return (gx, gy);
        }

        public (float wx, float wy) GridToWorldCenter(int gx, int gy)
        {
            return (OriginX + gx + 0.5f, OriginY + gy + 0.5f);
        }

        public bool IsValidCell(int gx, int gy)
        {
            return gx >= 0 && gx < Width && gy >= 0 && gy < Height;
        }

        public void SetBlocked(int gx, int gy, bool blocked)
        {
            if (IsValidCell(gx, gy))
            {
                blockedCells[gx, gy] = blocked;
            }
        }

        public void SetWorldRectBlocked(float xMin, float yMin, float width, float height, bool blocked)
        {
            int x0 = (int)Math.Floor(xMin - OriginX);
            int y0 = (int)Math.Floor(yMin - OriginY);
            int x1 = (int)Math.Ceiling(xMin + width - OriginX);
            int y1 = (int)Math.Ceiling(yMin + height - OriginY);

            for (int gx = x0; gx < x1; gx++)
            {
                for (int gy = y0; gy < y1; gy++)
                {
                    SetBlocked(gx, gy, blocked);
                }
            }
        }

        public bool IsBlocked(int gx, int gy)
        {
            return !IsValidCell(gx, gy) || blockedCells[gx, gy];
        }

        /// <summary>
        /// Dựng rào chuồng chuẩn với 4 cổng rộng 6.2m (có thể đóng hoặc mở từng cổng).
        /// </summary>
        public void ConfigurePastureFenceWithGates(MapRect pasture, BoundaryGap[] gates, bool[] gateOpenStates)
        {
            int xMin = (int)Math.Floor(pasture.XMin - OriginX);
            int xMax = (int)Math.Floor(pasture.XMax - OriginX);
            int yMin = (int)Math.Floor(pasture.YMin - OriginY);
            int yMax = (int)Math.Floor(pasture.YMax - OriginY);

            for (int gx = xMin; gx <= xMax; gx++)
            {
                SetBlocked(gx, yMin, true);
                SetBlocked(gx, yMax, true);
            }
            for (int gy = yMin; gy <= yMax; gy++)
            {
                SetBlocked(xMin, gy, true);
                SetBlocked(xMax, gy, true);
            }

            if (gates == null) return;
            for (int i = 0; i < gates.Length; i++)
            {
                bool isOpen = gateOpenStates == null || i >= gateOpenStates.Length || gateOpenStates[i];
                if (!isOpen) continue;

                var g = gates[i];
                float half = g.WidthMeters * 0.5f;
                if (g.Side == ApproachSector.North || g.Side == ApproachSector.South)
                {
                    int gy = g.Side == ApproachSector.North ? yMax : yMin;
                    int gxStart = (int)Math.Floor((g.Center - half) - OriginX);
                    int gxEnd = (int)Math.Ceiling((g.Center + half) - OriginX);
                    for (int gx = gxStart; gx <= gxEnd; gx++)
                    {
                        SetBlocked(gx, gy, false);
                    }
                }
                else
                {
                    int gx = g.Side == ApproachSector.East ? xMax : xMin;
                    int gyStart = (int)Math.Floor((g.Center - half) - OriginY);
                    int gyEnd = (int)Math.Ceiling((g.Center + half) - OriginY);
                    for (int gy = gyStart; gy <= gyEnd; gy++)
                    {
                        SetBlocked(gx, gy, false);
                    }
                }
            }
        }

        public void SetTargets(FlowDestinationGroup group, IEnumerable<(float worldX, float worldY)> worldTargets)
        {
            var list = groupTargets[group];
            list.Clear();
            foreach (var (wx, wy) in worldTargets)
            {
                var cell = WorldToGrid(wx, wy);
                SetBlocked(cell.gx, cell.gy, false);
                list.Add(cell);
            }
            RebuildFlowField(group);
        }

        public string LastUsedNativeBackend { get; private set; } = "None";
        public int RebuildCount { get; private set; }

        public void RebuildFlowField(FlowDestinationGroup group)
        {
            if (!distanceMaps.TryGetValue(group, out var dist))
            {
                dist = new int[Width, Height];
                distanceMaps[group] = dist;
            }
            if (!vectorMaps.TryGetValue(group, out var vecs))
            {
                vecs = new FlowVector2D[Width, Height];
                vectorMaps[group] = vecs;
            }

            var targets = groupTargets[group];
            if (targets.Count == 0)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        dist[x, y] = UnreachableDistance;
                        vecs[x, y] = new FlowVector2D(0f, 0f);
                    }
                }
                return;
            }

            for (int y = 0; y < Height; y++)
            {
                int rowOffset = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    _obsFlatScratch[rowOffset + x] = blockedCells[x, y] ? (byte)1 : (byte)0;
                }
            }

            if (_txScratch.Length < targets.Count)
            {
                _txScratch = new int[targets.Count];
                _tyScratch = new int[targets.Count];
            }
            for (int i = 0; i < targets.Count; i++)
            {
                _txScratch[i] = targets[i].gx;
                _tyScratch[i] = targets[i].gy;
            }

            NativePolyglotAccelerator.ComputeFlowField8DirNativeOrManaged(
                Width,
                Height,
                _obsFlatScratch,
                _txScratch,
                _tyScratch,
                targets.Count,
                _outDistFlatScratch,
                _outVxFlatScratch,
                _outVyFlatScratch,
                out string backend);

            LastUsedNativeBackend = backend;
            RebuildCount++;

            for (int y = 0; y < Height; y++)
            {
                int rowOffset = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    int idx = rowOffset + x;
                    dist[x, y] = _outDistFlatScratch[idx];
                    vecs[x, y] = new FlowVector2D(_outVxFlatScratch[idx], _outVyFlatScratch[idx]);
                }
            }
        }

        public FlowVector2D SampleFlow(FlowDestinationGroup group, float worldX, float worldY)
        {
            if (!vectorMaps.TryGetValue(group, out var vecs))
            {
                return new FlowVector2D(0f, 0f);
            }
            var (gx, gy) = WorldToGrid(worldX, worldY);
            return vecs[gx, gy];
        }

        public float SampleDistanceMeters(FlowDestinationGroup group, float worldX, float worldY)
        {
            if (!distanceMaps.TryGetValue(group, out var dist))
            {
                return UnreachableDistance;
            }
            var (gx, gy) = WorldToGrid(worldX, worldY);
            int raw = dist[gx, gy];
            return raw >= UnreachableDistance ? UnreachableDistance : raw * 0.1f;
        }

        /// <summary>
        /// Tính vector di chuyển kết hợp Flow-Field + lực tách đàn (Separation) để 300 heo không chồng lên nhau.
        /// </summary>
        public FlowVector2D ComputeSteeringWithSeparation(
            FlowDestinationGroup group,
            PigSteeringAgent self,
            IReadOnlyList<PigSteeringAgent> neighbors,
            float separationRadius = 1.2f,
            float separationWeight = 0.45f)
        {
            FlowVector2D flow = SampleFlow(group, self.X, self.Y);
            float sepX = 0f;
            float sepY = 0f;
            float rSq = separationRadius * separationRadius;

            if (neighbors != null)
            {
                for (int i = 0; i < neighbors.Count; i++)
                {
                    var other = neighbors[i];
                    if (other == null || !other.IsAlive || other.PigId == self.PigId) continue;

                    float dx = self.X - other.X;
                    float dy = self.Y - other.Y;
                    float dSq = dx * dx + dy * dy;
                    if (dSq > 0.0001f && dSq < rSq)
                    {
                        float inv = 1f / (float)Math.Sqrt(dSq);
                        sepX += dx * inv;
                        sepY += dy * inv;
                    }
                }
            }

            FlowVector2D sep = new FlowVector2D(sepX, sepY).Normalized();
            return new FlowVector2D(
                flow.X * (1f - separationWeight) + sep.X * separationWeight,
                flow.Y * (1f - separationWeight) + sep.Y * separationWeight).Normalized();
        }

        /// <summary>
        /// Lệnh "Lùa về Nơi Trú" (GDD 3.8 / P2.23):
        /// Khi Nơi Trú Đàn có ≥ 2 lối vào rộng ≥ 6m, tối thiểu 90% heo khỏe không hoảng loạn về đến Nơi Trú trong ≤ 2 giờ game.
        /// </summary>
        public (int recalledCount, int eligibleCount, float ratioRecalled, float estimatedGameHours) ExecuteRecallToRefuge(
            IList<PigSteeringAgent> agents,
            (float x, float y) refugeCenter,
            int entranceCount = 2,
            float minEntranceWidthMeters = 6.2f,
            bool hasShepherdDog = false)
        {
            SetTargets(FlowDestinationGroup.HerdRefuge, new[] { refugeCenter });

            int eligible = 0;
            int recalled = 0;
            float maxTravelSeconds = 0f;

            float entranceEfficiency = (entranceCount >= 2 && minEntranceWidthMeters >= 6.0f) ? 1.0f : 0.65f;
            float dogSpeedBonus = hasShepherdDog ? 1.20f : 1.0f;

            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                if (a == null || !a.IsAlive || a.IsIsolated || a.IsPanicking) continue;
                eligible++;

                float dist = SampleDistanceMeters(FlowDestinationGroup.HerdRefuge, a.X, a.Y);
                if (dist >= UnreachableDistance * 0.5f) continue;

                float geneSpeedMult = a.GeneLine == GeneLineId.MocCuoc ? 1.15f : 1.0f;
                float effectiveSpeed = a.BaseSpeedMetersPerSec * geneSpeedMult * dogSpeedBonus * entranceEfficiency;
                float travelSeconds = dist / Math.Max(0.5f, effectiveSpeed);

                // 1 giờ game = 40 giây thực (16 phút = 960s / 24h = 40s/giờ) -> 2 giờ game = 80s thực
                if (travelSeconds <= 80f)
                {
                    recalled++;
                    a.X = refugeCenter.x;
                    a.Y = refugeCenter.y;
                    if (travelSeconds > maxTravelSeconds) maxTravelSeconds = travelSeconds;
                }
            }

            float ratio = eligible > 0 ? (float)recalled / eligible : 1f;
            float gameHours = maxTravelSeconds / 40f;
            return (recalled, eligible, ratio, gameHours);
        }

        /// <summary>
        /// Kỹ năng đặc thù của Khoa: "Tiếng Huýt Chăn Đàn" (GDD 8.2 / P1.03):
        /// Thu hút heo trong bán kính 25m (30m nếu có Chó Chăn Đàn), làm dịu heo Bất an / Hoảng sợ (+8 Tinh thần).
        /// </summary>
        public int ExecuteKhoaWhistle(
            IList<PigSteeringAgent> agents,
            float khoaX,
            float khoaY,
            bool hasShepherdDog = false)
        {
            float radius = hasShepherdDog ? 30f : 25f;
            float rSq = radius * radius;
            SetTargets(FlowDestinationGroup.WhistlePoint, new[] { (khoaX, khoaY) });

            int affected = 0;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                if (a == null || !a.IsAlive || a.IsIsolated) continue;

                float dx = a.X - khoaX;
                float dy = a.Y - khoaY;
                if (dx * dx + dy * dy <= rSq)
                {
                    affected++;
                    a.IsPanicking = false;
                    a.Mood = Math.Min(100, a.Mood + 8);
                    FlowVector2D step = SampleFlow(FlowDestinationGroup.WhistlePoint, a.X, a.Y);
                    float speed = a.BaseSpeedMetersPerSec * (hasShepherdDog ? 1.20f : 1.0f);
                    a.X += step.X * speed;
                    a.Y += step.Y * speed;
                }
            }

            return affected;
        }
    }
}
