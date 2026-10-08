namespace PigTycoon.Core
{
    /// <summary>
    /// Bốn cổng trên chuồng Prototype. Heo chỉ ra khỏi chuồng khi đang hoảng và khe hở rộng từ 6,2 m.
    /// </summary>
    public readonly struct BoundaryGap
    {
        public ApproachSector Side { get; }
        public float Center { get; }
        public float WidthMeters { get; }

        public BoundaryGap(ApproachSector side, float center, float widthMeters)
        {
            Side = side;
            Center = center;
            WidthMeters = widthMeters;
        }
    }

    public static class PastureBoundary
    {
        public static BoundaryGap[] FourGates()
        {
            float width = FarmMainLayout.MainGateWidthMeters;
            return new[]
            {
                new BoundaryGap(ApproachSector.North, 0f, width),
                new BoundaryGap(ApproachSector.East, 0f, width),
                new BoundaryGap(ApproachSector.South, 0f, width),
                new BoundaryGap(ApproachSector.West, 0f, width)
            };
        }

        public static bool OnEdge(ApproachSector side, float x, float y)
        {
            MapRect pasture = FarmMainLayout.PastureBounds;
            float thickness = FarmMainLayout.MainGateThicknessMeters;
            if (side == ApproachSector.South)
            {
                return y >= pasture.YMin && y < pasture.YMin + thickness;
            }

            if (side == ApproachSector.North)
            {
                return y < pasture.YMax && y >= pasture.YMax - thickness;
            }

            if (side == ApproachSector.East)
            {
                return x < pasture.XMax && x >= pasture.XMax - thickness;
            }

            return x >= pasture.XMin && x < pasture.XMin + thickness;
        }

        public static bool PanicEscapes(float x, float y, bool panicking, BoundaryGap gap)
        {
            if (!panicking || gap.WidthMeters < FarmMainLayout.MainGateWidthMeters)
            {
                return false;
            }

            if (!OnEdge(gap.Side, x, y))
            {
                return false;
            }

            float along = gap.Side == ApproachSector.North || gap.Side == ApproachSector.South ? x : y;
            float half = gap.WidthMeters * 0.5f;
            return along >= gap.Center - half && along < gap.Center + half;
        }
    }
}
