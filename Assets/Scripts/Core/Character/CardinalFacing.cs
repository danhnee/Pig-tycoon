namespace PigTycoon.Core
{
    /// <summary>
    /// 4 hướng N/S/E/W. Đông và Tây dùng chung sprite nhìn ngang; Tây lật ngang.
    /// Ngưỡng trục ngang giữ đúng luật đã chạy trên Prototype: |x| > |y| * 0.8.
    /// </summary>
    public enum CardinalDirection
    {
        South,
        North,
        East,
        West
    }

    public static class CardinalFacing
    {
        public const float MoveEpsilonSqr = 0.05f;
        public const float HorizontalDominance = 0.8f;

        public static CardinalDirection Resolve(float x, float y, CardinalDirection hold)
        {
            if ((x * x) + (y * y) <= MoveEpsilonSqr)
            {
                return hold;
            }

            float absoluteX = Abs(x);
            float absoluteY = Abs(y);
            if (absoluteX > absoluteY * HorizontalDominance)
            {
                return x < 0f ? CardinalDirection.West : CardinalDirection.East;
            }

            return y > 0f ? CardinalDirection.North : CardinalDirection.South;
        }

        public static bool MirrorSideSprite(CardinalDirection direction)
        {
            return direction == CardinalDirection.West;
        }

        private static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }
    }
}
