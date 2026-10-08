using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Lưới rào 1 m, mặt nạ 16 hướng giống Fence2DView: Bắc 1, Đông 2, Nam 4, Tây 8.
    /// PlaceRun chỉ đi theo một trục của tường, không xoay 15 mặt còn lại.
    /// </summary>
    public enum FenceHeading
    {
        East,
        North
    }

    public sealed class FenceCourse
    {
        private readonly HashSet<(int x, int y)> cells = new HashSet<(int x, int y)>();

        public int Count => cells.Count;

        public bool Contains(int x, int y)
        {
            return cells.Contains((x, y));
        }

        public int MaskAt(int x, int y)
        {
            if (!cells.Contains((x, y)))
            {
                return -1;
            }

            bool north = cells.Contains((x, y + 1));
            bool east = cells.Contains((x + 1, y));
            bool south = cells.Contains((x, y - 1));
            bool west = cells.Contains((x - 1, y));
            return (north ? 1 : 0) | (east ? 2 : 0) | (south ? 4 : 0) | (west ? 8 : 0);
        }

        public void Place(int x, int y)
        {
            cells.Add((x, y));
        }

        public int PlaceRun(int x, int y, FenceHeading heading, int length)
        {
            if (length < 1)
            {
                return 0;
            }

            for (int i = 0; i < length; i++)
            {
                if (heading == FenceHeading.East)
                {
                    cells.Add((x + i, y));
                }
                else
                {
                    cells.Add((x, y + i));
                }
            }

            return length;
        }
    }
}
