namespace PigTycoon.Core
{
    /// <summary>
    /// Khung Farm_Main đang có trên Prototype (thảo nguyên 90×64 m, chuồng 40×28 m).
    /// Đây chưa phải kích thước khởi đầu 120×90 m của GDD 3.6. Số đo lấy từ scene đã chơi được.
    /// </summary>
    public readonly struct MapRect
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public MapRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float XMin => X;
        public float YMin => Y;
        public float XMax => X + Width;
        public float YMax => Y + Height;

        public bool Contains(float x, float y)
        {
            return x >= XMin && x < XMax && y >= YMin && y < YMax;
        }
    }

    public static class FarmMainLayout
    {
        public static MapRect MapBounds => new MapRect(-45f, -32f, 90f, 64f);
        public static MapRect PastureBounds => new MapRect(-20f, -14f, 40f, 28f);

        public const float MainGateWidthMeters = 6.2f;
        public const float MainGateThicknessMeters = 0.8f;
        public const float MainGateCenterX = 0f;

        public static bool IsInsidePasture(float x, float y)
        {
            return PastureBounds.Contains(x, y);
        }

        public static bool IsInsideMap(float x, float y)
        {
            return MapBounds.Contains(x, y);
        }

        public static ApproachSector SectorFor(float x, float y)
        {
            float centerX = PastureBounds.X + (PastureBounds.Width * 0.5f);
            float centerY = PastureBounds.Y + (PastureBounds.Height * 0.5f);
            float dx = x - centerX;
            float dy = y - centerY;
            float absoluteX = dx < 0f ? -dx : dx;
            float absoluteY = dy < 0f ? -dy : dy;
            if (absoluteX > absoluteY)
            {
                return dx >= 0f ? ApproachSector.East : ApproachSector.West;
            }

            return dy >= 0f ? ApproachSector.North : ApproachSector.South;
        }
    }

    public enum ApproachSector
    {
        North,
        East,
        South,
        West
    }

    public static class FarmSorting
    {
        public static readonly string[] LayerOrder =
        {
            "Ground",
            "Decor",
            "Actors",
            "Canopy",
            "UI_World"
        };
    }

    /// <summary>
    /// Ba khung camera GDD 3.6. Ortho size của Unity là một nửa chiều cao nhìn thấy.
    /// </summary>
    public static class CameraFrames
    {
        public const float CloseWidthMeters = 40f;
        public const float CloseHeightMeters = 22f;
        public const float DefaultWidthMeters = 64f;
        public const float DefaultHeightMeters = 36f;
        public const float OverviewWidthMeters = 128f;
        public const float OverviewHeightMeters = 72f;

        public static float OrthoSizeForHeight(float heightMeters)
        {
            return heightMeters * 0.5f;
        }
    }
}
