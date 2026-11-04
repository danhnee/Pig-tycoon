using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// 8-direction flow field (multi-source Dijkstra on a grid, 10/14 step costs, no corner cutting) used by
    /// <see cref="HerdFlowField"/>. Extracted on 3 Nov 2026 from the former NativePolyglotAccelerator, whose Rust/C++
    /// P/Invoke paths never had a shipped library (the managed path was the only one that ever ran).
    /// </summary>
    public static class FlowField8Dir
    {
        /// <summary>Fills distances (10 per straight step) and unit vectors toward the nearest target; returns reachable cells, -1 on bad input.</summary>
        public static int Compute(int width, int height, byte[] obstacles, int[] targetsX, int[] targetsY, int targetCount,
            int[] outDist, float[] outVx, float[] outVy)
        {
            int n = width * height;
            if (width <= 0 || height <= 0 || targetCount <= 0 ||
                obstacles == null || obstacles.Length < n ||
                targetsX == null || targetsX.Length < targetCount ||
                targetsY == null || targetsY.Length < targetCount ||
                outDist == null || outDist.Length < n ||
                outVx == null || outVx.Length < n ||
                outVy == null || outVy.Length < n)
                return -1;

            const int UNREACHABLE = 999999;
            for (int i = 0; i < n; i++)
            {
                outDist[i] = UNREACHABLE;
                outVx[i] = 0f;
                outVy[i] = 0f;
            }

            int[] queue = new int[n * 4];
            int head = 0;
            int tail = 0;

            for (int k = 0; k < targetCount; k++)
            {
                int tx = targetsX[k];
                int ty = targetsY[k];
                if (tx >= 0 && tx < width && ty >= 0 && ty < height)
                {
                    int idx = ty * width + tx;
                    if (outDist[idx] != 0)
                    {
                        outDist[idx] = 0;
                        queue[tail++] = idx;
                    }
                }
            }

            int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
            int[] dy = { 1, 0, -1, 0, 1, -1, 1, -1 };
            int[] stepCost = { 10, 10, 10, 10, 14, 14, 14, 14 };

            while (head < tail)
            {
                int cur = queue[head++];
                int cx = cur % width;
                int cy = cur / width;
                int currentDist = outDist[cur];

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = cx + dx[dir];
                    int ny = cy + dy[dir];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int nidx = ny * width + nx;
                    if (obstacles[nidx] != 0) continue;

                    if (dir >= 4 && (obstacles[cy * width + nx] != 0 || obstacles[ny * width + cx] != 0))
                    {
                        continue;
                    }

                    int cand = currentDist + stepCost[dir];
                    if (cand < outDist[nidx])
                    {
                        outDist[nidx] = cand;
                        if (tail < queue.Length)
                        {
                            queue[tail++] = nidx;
                        }
                    }
                }
            }

            int reachableCells = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    int d = outDist[idx];
                    if (d < UNREACHABLE) reachableCells++;
                    if (obstacles[idx] != 0 || d == 0 || d >= UNREACHABLE) continue;

                    int bestDist = d;
                    float bvx = 0f;
                    float bvy = 0f;
                    for (int dir = 0; dir < 8; dir++)
                    {
                        int nx = x + dx[dir];
                        int ny = y + dy[dir];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int nd = outDist[ny * width + nx];
                        if (nd < bestDist)
                        {
                            bestDist = nd;
                            bvx = dx[dir];
                            bvy = dy[dir];
                        }
                    }

                    float mag = (float)Math.Sqrt(bvx * bvx + bvy * bvy);
                    if (mag > 0.0001f)
                    {
                        outVx[idx] = bvx / mag;
                        outVy[idx] = bvy / mag;
                    }
                }
            }

            return reachableCells;
        }
    }
}
