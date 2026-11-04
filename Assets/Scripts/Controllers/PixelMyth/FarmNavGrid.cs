using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Coarse walkability grid of the farm map (1 m cells) and A* over it, for herd recall: a pig walking straight
    /// at the refuge bounced off the pond, buildings or the fence and never arrived (playtest 2 Nov: 9–10/12).
    /// Blocked = any static, non-trigger collider (buildings, fence segments, water, rocks); animals, the player and
    /// monsters (collider on a non-static Rigidbody2D) are ignored. Gates have no collider, so paths use them.
    /// Rebuilt on demand when older than <see cref="MaxAgeSeconds"/> (new buildings, broken or patched fence).
    /// </summary>
    public static class FarmNavGrid
    {
        public const float Cell = 1f;
        public const float MaxAgeSeconds = 10f;

        private static bool[,] blocked;
        private static Rect area;
        private static int w, h;
        private static float builtAt = -999f;
        private static readonly Collider2D[] hits = new Collider2D[16];

        private static void EnsureBuilt()
        {
            var farm = PixelMythFarmGameplay.Instance;
            if (farm == null) { blocked = null; return; }
            if (blocked != null && Time.time - builtAt < MaxAgeSeconds && area == farm.MapRect) return;
            area = farm.MapRect;
            w = Mathf.CeilToInt(area.width / Cell);
            h = Mathf.CeilToInt(area.height / Cell);
            blocked = new bool[w, h];
            var filter = new ContactFilter2D { useTriggers = false };
            var size = new Vector2(Cell * 0.7f, Cell * 0.7f);
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                var c = new Vector2(area.xMin + (x + 0.5f) * Cell, area.yMin + (y + 0.5f) * Cell);
                int n = Physics2D.OverlapBox(c, size, 0f, filter, hits);
                for (int i = 0; i < n; i++)
                {
                    var col = hits[i];
                    if (col == null || col.isTrigger) continue;
                    var body = col.attachedRigidbody;
                    if (body != null && body.bodyType != RigidbodyType2D.Static) continue;
                    blocked[x, y] = true;
                    break;
                }
            }
            builtAt = Time.time;
        }

        private static bool Cell2(Vector2 p, out int x, out int y)
        {
            x = Mathf.FloorToInt((p.x - area.xMin) / Cell);
            y = Mathf.FloorToInt((p.y - area.yMin) / Cell);
            return x >= 0 && y >= 0 && x < w && y < h;
        }

        private static Vector2 Centre(int x, int y) => new Vector2(area.xMin + (x + 0.5f) * Cell, area.yMin + (y + 0.5f) * Cell);

        /// <summary>Waypoints from <paramref name="from"/> to <paramref name="to"/> (last = to), thinned to turns; null when no farm grid or no path.</summary>
        public static List<Vector2> FindPath(Vector2 from, Vector2 to)
        {
            EnsureBuilt();
            if (blocked == null || !Cell2(from, out int sx, out int sy) || !Cell2(to, out int tx, out int ty)) return null;
            if (blocked[tx, ty] && !NearestFree(ref tx, ref ty)) return null;
            bool startBlocked = blocked[sx, sy];   // a pig pressed against a wall starts "inside" it: allow leaving
            var open = new SortedSet<(float f, int id)>();
            var g = new Dictionary<int, float>();
            var came = new Dictionary<int, int>();
            int Id(int x, int y) => y * w + x;
            int start = Id(sx, sy), goal = Id(tx, ty);
            g[start] = 0f;
            open.Add((Heur(sx, sy, tx, ty), start));
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
            int expanded = 0;
            while (open.Count > 0 && expanded++ < 40000)
            {
                var cur = open.Min;
                open.Remove(cur);
                if (cur.id == goal) return Build(came, goal, to);
                int cx = cur.id % w, cy = cur.id / w;
                float gc = g[cur.id];
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + dx[k], ny = cy + dy[k];
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (blocked[nx, ny] && !(startBlocked && cur.id == start)) continue;
                    if (k >= 4 && (blocked[cx + dx[k], cy] || blocked[cx, cy + dy[k]])) continue;   // no corner cutting
                    float ng = gc + (k >= 4 ? 1.4142f : 1f);
                    int nid = Id(nx, ny);
                    if (g.TryGetValue(nid, out var old) && old <= ng) continue;
                    if (g.ContainsKey(nid)) open.Remove((old + Heur(nx, ny, tx, ty), nid));
                    g[nid] = ng;
                    came[nid] = cur.id;
                    open.Add((ng + Heur(nx, ny, tx, ty), nid));
                }
            }
            return null;
        }

        private static float Heur(int x, int y, int tx, int ty)
        {
            int ax = Mathf.Abs(x - tx), ay = Mathf.Abs(y - ty);
            return (ax + ay) + (1.4142f - 2f) * Mathf.Min(ax, ay);
        }

        private static bool NearestFree(ref int x, ref int y)
        {
            for (int r = 1; r < 6; r++)
            for (int ox = -r; ox <= r; ox++)
            for (int oy = -r; oy <= r; oy++)
            {
                int nx = x + ox, ny = y + oy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || blocked[nx, ny]) continue;
                x = nx; y = ny;
                return true;
            }
            return false;
        }

        private static List<Vector2> Build(Dictionary<int, int> came, int goal, Vector2 to)
        {
            var cells = new List<int> { goal };
            while (came.TryGetValue(cells[cells.Count - 1], out var prev)) cells.Add(prev);
            cells.Reverse();
            var pts = new List<Vector2>();
            int lastDx = 99, lastDy = 99;
            for (int i = 1; i < cells.Count; i++)
            {
                int ddx = cells[i] % w - cells[i - 1] % w, ddy = cells[i] / w - cells[i - 1] / w;
                if ((ddx != lastDx || ddy != lastDy) && i > 1) pts.Add(Centre(cells[i - 1] % w, cells[i - 1] / w));
                lastDx = ddx; lastDy = ddy;
            }
            pts.Add(to);
            return pts;
        }
    }
}
