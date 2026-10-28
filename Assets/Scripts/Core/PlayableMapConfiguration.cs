using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public sealed class FarmServicePoint
    {
        public string Id = "";
        public float X, Y;
        public bool Feed, Water, Shelter, Cooling;
    }
    [Serializable]
    public sealed class FarmApproachPoint
    {
        public ApproachSector Sector;
        public float X, Y;
    }
    /// <summary>Explicit scene contract in meters. Null on legacy scenes preserves their existing layout.</summary>
    [Serializable]
    public sealed class PlayableMapConfiguration
    {
        public string ConfigurationId = "";
        // Explicit persisted owner; unconfigured and older aggregate maps stay legacy by default.
        public bool InteractiveHerdOwnership;
        public float MapX = -45f, MapY = -32f, MapWidth = 90f, MapHeight = 64f;
        public float PastureX = -20f, PastureY = -14f, PastureWidth = 40f, PastureHeight = 28f;
        public float RefugeX = 0f, RefugeY = 6f;
        public List<PlacedFarmStructure> InitialServices = new List<PlacedFarmStructure>();
        public List<FarmServicePoint> ServicePoints = new List<FarmServicePoint>();
        public List<FarmApproachPoint> MonsterEntries = new List<FarmApproachPoint>();
        public MapRect MapBounds => new MapRect(MapX, MapY, MapWidth, MapHeight);
        public MapRect PastureBounds => new MapRect(PastureX, PastureY, PastureWidth, PastureHeight);

        public void Validate()
        {
            foreach (float v in new[] { MapX, MapY, MapWidth, MapHeight, PastureX, PastureY, PastureWidth, PastureHeight, RefugeX, RefugeY })
                if (float.IsNaN(v) || float.IsInfinity(v)) throw new ArgumentException("Map coordinates must be finite.");
            if (string.IsNullOrWhiteSpace(ConfigurationId) || MapWidth < 10 || MapHeight < 10 || MapWidth > 1024 || MapHeight > 1024 ||
                PastureWidth <= 0 || PastureHeight <= 0 || PastureX < MapX || PastureY < MapY ||
                PastureX + PastureWidth > MapX + MapWidth || PastureY + PastureHeight > MapY + MapHeight ||
                !PastureBounds.Contains(RefugeX, RefugeY)) throw new ArgumentException("Invalid explicit map/pasture/refuge configuration.");
            if (MapWidth % FarmGridAndZones.BuildCellMeters != 0 || MapHeight % FarmGridAndZones.BuildCellMeters != 0)
                throw new ArgumentException("Map size must fit the existing2m build grid.");
            if (MapX != (float)Math.Floor(MapX) || MapY != (float)Math.Floor(MapY))
                throw new ArgumentException("Map origin must align with the existing1m flow grid.");
            if (ServicePoints == null || InitialServices == null) throw new ArgumentException("Map service collections cannot be null.");
            foreach (var point in ServicePoints)
                if (point == null || string.IsNullOrWhiteSpace(point.Id) || !MapBounds.Contains(point.X, point.Y))
                    throw new ArgumentException("Service points must have stable IDs and lie within the map.");
        }
    }
}
