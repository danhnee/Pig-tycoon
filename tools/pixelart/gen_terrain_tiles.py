"""Generate the Pig Tycoon terrain tilesets (32 px tiles, ADR-0005) procedurally and deterministically.

  python tools/pixelart/gen_terrain_tiles.py --out Assets/Art/PixelMyth/Terrain

Palettes are sampled by eye from the user's target images (gold meadow diorama, sunset world):
warm sun-gold grass, ochre packed earth, weathered limestone, deep lake blue, dark moss for the forest.

Materials (layer_order bottom -> top): water, path, stone, dirt, meadow, meadow_dry, meadow_lush, forest.
meadow_dry / meadow_lush / dirt are painted by the map composer with world-space noise so the pasture gets large soft
colour drifts; their masks use a wide dithered edge (SOFT) instead of a hard lip.

Each material gets 4 seamless full-tile variants. Each ordered pair (upper over lower) gets a 16-tile dual-grid
transition set indexed by corner bits TL=1 TR=2 BL=4 BR=8 (bit set = upper material) and every material gets a
mask_<mat> overlay sheet. Seams match because the noise is periodic over one tile and the corner mask on a shared
edge depends only on that edge's two corners.
"""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

T = 32

PAL = {
    "meadow": ["#7a5520", "#a5782a", "#c99a32", "#e0b448", "#f2d06a"],
    "path":   ["#7d5634", "#9c7146", "#b98e5a", "#d2ab74", "#e6c896"],
    "stone":  ["#4a4036", "#6c6050", "#8d7f67", "#ab9d80", "#c8bb9c"],
    "water":  ["#1c3b47", "#25525c", "#2f6a72", "#4b8d8c", "#b9d8c4"],   # deep teal, warm sky glint (AE-0002: no plastic blue)
    "forest": ["#232c14", "#313d1b", "#434f24", "#58632e", "#76773a"],
    "meadow_lush": ["#6a5a22", "#867428", "#a08c32", "#bca43c", "#d4bc54"],   # olive-leaning gold, gentle step from meadow
    "meadow_dry":  ["#b08838", "#c8a244", "#dcb45a", "#ebcb7c", "#f6e2a2"],   # pale straw, close to meadow
    "dirt":   ["#6a4a28", "#80592f", "#98703c", "#ae8650", "#c49e68"],        # worn earth, lighter / softer
    "rampart": ["#2f3a16", "#45531f", "#5b6a28", "#768232", "#97a044"],     # deep olive turf crowning the ramparts: reads raised against the gold
}
EDGE_INK = {  # 1 px lip the upper material casts onto the lower one
    "meadow": "#6f4c1c", "path": "#6b4a2e", "stone": "#3f362c", "water": "#16303a", "forest": "#1b2210",
    "meadow_lush": "#3a3a16", "meadow_dry": "#8a6a30", "dirt": "#33231a", "rampart": "#22180c",
}
GRASSY = ("meadow", "meadow_lush", "meadow_dry", "rampart")
SOFT = ("meadow_lush", "meadow_dry")   # ragged dithered edges: large soft colour drifts across the pasture
PAIRS = [("meadow", "path"), ("meadow", "water"), ("path", "water"), ("meadow", "stone"),
         ("stone", "water"), ("forest", "path"), ("forest", "meadow"), ("stone", "path")]
LAYER_ORDER = ["water", "path", "stone", "dirt", "meadow", "meadow_dry", "meadow_lush", "forest", "rampart"]  # bottom -> top
LEGEND = {"m": "meadow", "p": "path", "s": "stone", "w": "water", "f": "forest",
          "g": "meadow_lush", "d": "meadow_dry", "t": "dirt", "r": "rampart"}


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.uint8)


def periodic_noise(rng, cells, octaves=3):
    """Value noise that wraps every T pixels."""
    out = np.zeros((T, T))
    amp, total = 1.0, 0.0
    for o in range(octaves):
        n = cells * (2 ** o)
        lat = rng.random((n, n))
        ys = np.arange(T) * n / T
        y0 = np.floor(ys).astype(int)
        fy = ys - y0
        fy = fy * fy * (3 - 2 * fy)
        a = lat[y0 % n][:, y0 % n]
        b = lat[y0 % n][:, (y0 + 1) % n]
        c = lat[(y0 + 1) % n][:, y0 % n]
        d = lat[(y0 + 1) % n][:, (y0 + 1) % n]
        fx = fy[None, :]
        fyy = fy[:, None]
        out += amp * ((a * (1 - fx) + b * fx) * (1 - fyy) + (c * (1 - fx) + d * fx) * fyy)
        total += amp
        amp *= 0.5
    return out / total


def _put(img, y, x, c):
    img[int(y) % T, int(x) % T] = c


def _blades(img, rng, pal, n, tall=(1, 3), tip=4, root=1, body=3):
    """Blade clusters wrapped over the tile: root shade, body, lit tip; leaning left or right."""
    for _ in range(n):
        x, y = int(rng.integers(T)), int(rng.integers(T))
        lean = int(rng.choice([-1, 0, 1]))
        h = int(rng.integers(tall[0], tall[1] + 1))
        _put(img, y + 1, x, pal[root])
        for k in range(h):
            _put(img, y - k, x + (lean if k == h - 1 and h > 1 else 0), pal[body] if k < h - 1 else pal[tip])
        if rng.random() < 0.35:
            _put(img, y, x + 1, pal[body])


def flagstone(pal, rng):
    """Irregular worn flagstones: periodic jittered-grid Voronoi, joints, lit upper-left bevel, cracks, moss in the
    joints, rare gold-leaf flecks. ~0.35 m stones, wider than tall (3/4 view foreshortening)."""
    n = 3
    cell = T / n
    seeds = []
    for gy in range(n):
        for gx in range(n):
            seeds.append(((gx + 0.5) * cell + rng.uniform(-3.4, 3.4), (gy + 0.5) * cell + rng.uniform(-3.0, 3.0)))
    seeds = np.array(seeds)
    ys, xs = np.mgrid[0:T, 0:T]
    px, py = xs + 0.5, ys + 0.5
    dist = np.empty((len(seeds), T, T))
    for i, (sx, sy) in enumerate(seeds):
        dx = np.abs(px - sx)
        dx = np.minimum(dx, T - dx)
        dy = np.abs(py - sy)
        dy = np.minimum(dy, T - dy)
        dist[i] = np.hypot(dx * 0.86, dy * 1.22)
    order = np.argsort(dist, axis=0)
    d1 = np.take_along_axis(dist, order[:1], 0)[0]
    d2 = np.take_along_axis(dist, order[1:2], 0)[0]
    sid = order[0]
    gap = d2 - d1
    tone = rng.choice([1.8, 2.1, 2.4, 2.7, 3.0], len(seeds))
    tint = rng.choice([0, 0, 1, 2], len(seeds))               # 1 = warm gold-leaning, 2 = cool grey
    img = np.zeros((T, T, 3), np.uint8)
    gold_stone = int(rng.integers(len(seeds))) if rng.random() < 0.6 else -1
    for y in range(T):
        for x in range(T):
            i = sid[y, x]
            sx0, sy0 = seeds[i]
            t = tone[i] + 0.22 * np.clip(-(px[y, x] - sx0 + (py[y, x] - sy0) * 1.2) / 6.0, -1, 1)
            c = np.array(pal[int(np.clip(round(t), 0, 4))], np.float32)
            if tint[i] == 1:
                c = c * 0.88 + np.array([232, 196, 110], np.float32) * 0.12
            elif tint[i] == 2:
                c = c * 0.9 + np.array([140, 150, 150], np.float32) * 0.1
            sx, sy = seeds[i]
            vx, vy = px[y, x] - sx, py[y, x] - sy
            if gap[y, x] < 1.15:
                c = pal[0] if rng.random() < 0.75 else pal[1]
            elif gap[y, x] < 2.4:
                c = pal[4] if (vx + vy * 1.2) < 0 else pal[1]            # lit bevel / shaded rim
            img[y, x] = np.clip(c, 0, 255)
    moss, moss2 = hexrgb("#6b7a2c"), hexrgb("#47561f")
    for y in range(T):
        for x in range(T):
            if gap[y, x] < 1.3 and rng.random() < 0.05:
                for k in range(int(rng.integers(2, 4))):     # moss run along a joint
                    if gap[(y + k) % T, x] < 1.9:
                        img[(y + k) % T, x] = moss2 if k else moss
    crack = hexrgb("#3a3128")
    for i in range(len(seeds)):                                    # hairline cracks
        if rng.random() < 0.25:
            x, y = seeds[i][0] + rng.uniform(-3, 3), seeds[i][1] + rng.uniform(-2, 2)
            ang = rng.uniform(0, 6.283)
            for _ in range(int(rng.integers(4, 9))):
                ang += rng.normal(0, 0.55)
                x += np.cos(ang)
                y += np.sin(ang) * 0.8
                if sid[int(y) % T, int(x) % T] == i and gap[int(y) % T, int(x) % T] > 1.5:
                    img[int(y) % T, int(x) % T] = crack
    if gold_stone >= 0:                                             # gold-leaf flecks flaking off one stone
        sx, sy = seeds[gold_stone]
        for _ in range(7):
            gx_, gy_ = int(sx + rng.integers(-3, 4)), int(sy + rng.integers(-2, 3))
            if sid[gy_ % T, gx_ % T] == gold_stone and gap[gy_ % T, gx_ % T] > 1.6:
                img[gy_ % T, gx_ % T] = hexrgb("#f0c95a") if rng.random() < 0.6 else hexrgb("#d9a53a")
    return img


def _clump(img, rng, cx, cy, col, n):
    """Soft pixel clump: random walk of n px, wider than tall. Reads as one shape, never as single-pixel noise."""
    x, y = int(cx), int(cy)
    for _ in range(n):
        _put(img, y, x, col)
        r = rng.random()
        if r < 0.45:
            x += 1
        elif r < 0.7:
            x -= 1
        elif r < 0.85:
            y += 1
        else:
            y -= 1


def _tuft(img, rng, x, y, pal, h=3):
    """Little blade cluster 3 px wide: shaded root row, 2-3 blades of body shade, bright tips. One shape."""
    x, y = int(x), int(y)
    lean = int(rng.choice([-1, 0, 1]))
    for dx in (-1, 0, 1):
        _put(img, y, x + dx, pal[1])
    for dx, hh in ((-1, h - 1), (0, h), (1, h - 1)):
        for k in range(1, hh + 1):
            xx = x + dx + (lean if k == hh and hh > 1 else 0)
            _put(img, y - k, xx, pal[4] if k == hh and dx != -1 else pal[3] if k > 1 else pal[2])


def _pebble(img, rng, x, y, pal, big=False):
    x, y = int(x), int(y)
    _put(img, y, x, pal[4]); _put(img, y, x + 1, pal[3]); _put(img, y + 1, x, pal[3]); _put(img, y + 1, x + 1, pal[0])
    if big:
        _put(img, y, x + 2, pal[3]); _put(img, y + 1, x + 2, pal[1]); _put(img, y + 2, x + 1, pal[1])


def _grass_tile(img, rng, pal, tufts, lights, darks, sprigs=0):
    for _ in range(darks):                       # soft shaded undergrowth
        _clump(img, rng, rng.integers(T), rng.integers(T), pal[1], int(rng.integers(8, 16)))
    for _ in range(lights):                      # sun-lit patches
        _clump(img, rng, rng.integers(T), rng.integers(T), pal[3], int(rng.integers(6, 13)))
    for _ in range(tufts):                       # blade clusters, calm ground between them
        _tuft(img, rng, rng.integers(T), rng.integers(T), pal, int(rng.choice([2, 3, 3])))
    for _ in range(sprigs):                      # tiny two-pixel seed heads
        x, y = int(rng.integers(T)), int(rng.integers(T))
        _put(img, y, x, hexrgb("#fff0b4")); _put(img, y + 1, x, pal[3])


def material_tile(mat, seed):
    """Clustered pixel art, no salt-and-pepper: every mark is a 2+ px shape (clump, tuft, pebble, flagstone).
    Large-scale variety comes from painting several meadow/dirt materials with world-space noise and from decoration
    sprites, never from low-frequency noise inside a tile (that produced a visible grid in terrain_v1)."""
    rng = np.random.default_rng(seed)
    pal = [hexrgb(c) for c in PAL[mat]]
    img = np.empty((T, T, 3), dtype=np.uint8)
    img[:] = pal[2]
    if mat == "meadow":
        _grass_tile(img, rng, pal, tufts=13, lights=7, darks=7, sprigs=2)
    elif mat == "meadow_lush":
        _grass_tile(img, rng, pal, tufts=11, lights=6, darks=7)
        for _ in range(2):                        # clover pad
            x, y = int(rng.integers(T)), int(rng.integers(T))
            _put(img, y, x, hexrgb("#a0a444")); _put(img, y, x + 1, hexrgb("#c4c264")); _put(img, y + 1, x, hexrgb("#8a9238"))
    elif mat == "rampart":
        _grass_tile(img, rng, pal, tufts=16, lights=8, darks=6, sprigs=3)   # thick wind-combed grass on the rampart crown
    elif mat == "meadow_dry":
        _grass_tile(img, rng, pal, tufts=7, lights=7, darks=5)
        for _ in range(4):                        # lying straw strands (2-3 px runs)
            x, y = int(rng.integers(T)), int(rng.integers(T))
            for k in range(int(rng.integers(3, 6))):
                _put(img, y + (k // 3), x + k, pal[4] if k % 2 else pal[3])
    elif mat == "dirt":
        for _ in range(7):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[1], int(rng.integers(10, 20)))
        for _ in range(8):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[3], int(rng.integers(8, 16)))
        for _ in range(3):
            _pebble(img, rng, rng.integers(T), rng.integers(T), pal, big=bool(rng.random() < 0.4))
        if rng.random() < 0.5:                    # one short rut / crack
            x, y = int(rng.integers(T)), int(rng.integers(T))
            for k in range(int(rng.integers(5, 9))):
                _put(img, y + int(np.sin(k * 0.7) * 1.1), x + k, pal[1])
    elif mat == "path":
        for _ in range(7):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[1], int(rng.integers(10, 20)))
        for _ in range(9):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[3], int(rng.integers(8, 16)))
        for _ in range(3):
            _pebble(img, rng, rng.integers(T), rng.integers(T), pal, big=bool(rng.random() < 0.35))
        if rng.random() < 0.5:                    # cart rut
            x, y = int(rng.integers(T)), int(rng.integers(T))
            for k in range(int(rng.integers(5, 9))):
                _put(img, y + (k // 4), x + k, pal[1])
        if rng.random() < 0.3:                    # a tuft pushing through
            _tuft(img, rng, rng.integers(T), rng.integers(T), [hexrgb(c) for c in PAL["meadow_lush"]], 2)
    elif mat == "stone":
        img = flagstone(pal, rng)
    elif mat == "water":
        for _ in range(8):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[1], int(rng.integers(8, 16)))
        for _ in range(8):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[3], int(rng.integers(5, 10)))
        for _ in range(5):  # horizontal ripple dashes
            x, y, w = int(rng.integers(T)), int(rng.integers(T)), int(rng.integers(3, 8))
            for k in range(w):
                _put(img, y, x + k, pal[3])
            _put(img, y, x + w // 2, pal[4])
            _put(img, y + 1, x + 1, pal[1]); _put(img, y + 1, x + w - 1, pal[1])
        for _ in range(3):  # warm sun glints
            x, y = int(rng.integers(T)), int(rng.integers(T))
            _put(img, y, x, hexrgb("#f6e7a6")); _put(img, y, x + 1, hexrgb("#e0b448"))
    elif mat == "forest":
        for _ in range(9):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[1], int(rng.integers(10, 20)))
        for _ in range(7):
            _clump(img, rng, rng.integers(T), rng.integers(T), pal[3], int(rng.integers(6, 12)))
        for _ in range(7):  # leaf litter: 2 px leaves
            x, y = int(rng.integers(T)), int(rng.integers(T))
            c = hexrgb(rng.choice(["#8a6a2a", "#a7703a", "#76773a", "#6b7a2c"]))
            _put(img, y, x, c); _put(img, y, x + 1, c)
        for _ in range(3):  # moss pads
            x, y = int(rng.integers(T)), int(rng.integers(T))
            for dx, dy in ((0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (1, -1)):
                _put(img, y + dy, x + dx, hexrgb("#5f6b2a") if (dx + dy) % 2 else hexrgb("#6b7a2c"))
    return img


def transition_set(upper, lower, seed):
    rng = np.random.default_rng(seed)
    up = material_tile(upper, seed + 1)
    lo = material_tile(lower, seed + 2)
    n = periodic_noise(rng, 4, 2)
    ink = hexrgb(EDGE_INK[upper])
    ys, xs = np.mgrid[0:T, 0:T]
    u, v = (xs + 0.5) / T, (ys + 0.5) / T
    tiles = []
    for bits in range(16):
        tl, tr, bl, br = (bits >> 0) & 1, (bits >> 1) & 1, (bits >> 2) & 1, (bits >> 3) & 1
        m = tl * (1 - u) * (1 - v) + tr * u * (1 - v) + bl * (1 - u) * v + br * u * v
        mask = (m + (n - 0.5) * 0.22) > 0.5
        img = np.where(mask[..., None], up, lo).copy()
        # lip: lower-material pixel directly below an upper pixel gets the upper's shadow ink
        below = np.roll(mask, 1, axis=0) & ~mask
        below[0, :] = False
        img[below] = ink
        tiles.append(img)
    return tiles


def mask_set(mat, seed):
    """16 overlay tiles for one material: its pixels where the corner mask says so, its shadow lip on the pixel row
    just below its edge, transparent elsewhere. Stacking these in LAYER_ORDER over a base tile composes any number of
    materials per cell without per-pair tiles.
      * grassy materials get blade overhang above the edge, meadow also spills pebbles just outside it
        (path / plaza / shore edges);
      * SOFT materials (lush / dry meadow) use a wide dithered edge instead of a lip, so colour drifts across the pasture."""
    rng = np.random.default_rng(seed)
    up = material_tile(mat, seed + 1)
    n = periodic_noise(rng, 4, 2)
    n2 = periodic_noise(rng, 8, 2)
    white = rng.random((T, T))
    ink = hexrgb(EDGE_INK[mat])
    ys, xs = np.mgrid[0:T, 0:T]
    u, v = (xs + 0.5) / T, (ys + 0.5) / T
    pb = [hexrgb(c) for c in ("#d8cdb0", "#a3967b", "#4a4036")]
    tiles = []
    for bits in range(16):
        tl, tr, bl, br = (bits >> 0) & 1, (bits >> 1) & 1, (bits >> 2) & 1, (bits >> 3) & 1
        m = tl * (1 - u) * (1 - v) + tr * u * (1 - v) + bl * (1 - u) * v + br * u * v
        if mat in SOFT:
            mask = (m + (n - 0.5) * 0.70 + (n2 - 0.5) * 0.34) > 0.5
        else:
            mask = (m + (n - 0.5) * 0.30 + (n2 - 0.5) * 0.10) > 0.5
        rgba = np.zeros((T, T, 4), dtype=np.uint8)
        rgba[mask, :3] = up[mask]
        rgba[mask, 3] = 255
        if mat not in SOFT:
            below = np.roll(mask, 1, axis=0) & ~mask
            below[0, :] = False
            if mat in GRASSY:
                below &= white < 0.8                     # broken lip: some pixels stay bare
            rgba[below, :3] = ink
            rgba[below, 3] = 255
        if mat in GRASSY and 0 < mask.sum() < T * T:
            tip = hexrgb(PAL[mat][4])
            mid = hexrgb(PAL[mat][3])
            above = np.roll(mask, -1, axis=0) & ~mask    # empty pixel with grass directly below it
            above[-1, :] = False
            for y, x in zip(*np.nonzero(above)):
                if rng.random() < (0.3 if mat in SOFT else 0.5):
                    hgt = int(rng.integers(1, 4))
                    lean = int(rng.choice([-1, 0, 0, 1]))
                    for k in range(hgt):
                        yy, xx = y - k, x + (lean if k == hgt - 1 else 0)
                        if 0 <= yy < T and 0 <= xx < T and not mask[yy, xx]:
                            rgba[yy, xx, :3] = tip if k == hgt - 1 else mid
                            rgba[yy, xx, 3] = 255
            if mat == "meadow":                          # pebbles and grit spilled onto the neighbour material
                near = np.zeros_like(mask)
                for dy in range(-4, 5):
                    for dx in range(-4, 5):
                        near |= np.roll(np.roll(mask, dy, 0), dx, 1)
                near &= ~mask
                for y, x in zip(*np.nonzero(near)):
                    if white[y, x] < 0.05 and 0 < y < T - 1 and 0 < x < T - 1 and rgba[y, x, 3] == 0:
                        rgba[y, x, :3] = pb[0]; rgba[y, x, 3] = 255
                        if rgba[y, x + 1, 3] == 0:
                            rgba[y, x + 1, :3] = pb[1]; rgba[y, x + 1, 3] = 255
                        if rgba[y + 1, x + 1, 3] == 0:
                            rgba[y + 1, x + 1, :3] = pb[2]; rgba[y + 1, x + 1, 3] = 255
        tiles.append(rgba)
    return tiles


def save_sheet(tiles, cols, path):
    rows = (len(tiles) + cols - 1) // cols
    sheet = np.zeros((rows * T, cols * T, 4), dtype=np.uint8)
    for i, t in enumerate(tiles):
        r, c = divmod(i, cols)
        if t.shape[2] == 4:
            sheet[r * T:(r + 1) * T, c * T:(c + 1) * T] = t
        else:
            sheet[r * T:(r + 1) * T, c * T:(c + 1) * T, :3] = t
            sheet[r * T:(r + 1) * T, c * T:(c + 1) * T, 3] = 255
    Image.fromarray(sheet, "RGBA").save(path)
    # sidecar read by PixelMythSpriteImport: slice into 32 px tiles named <sheet>_r<row>_<col>
    Path(path).with_suffix(".json").write_text(json.dumps({
        "cell": [T, T], "rows": [f"r{r}" for r in range(rows)], "frames_per_row": cols,
        "pivot_px_from_bottom_left": [T // 2, T // 2]}))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    a = ap.parse_args()
    out = Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    meta = {"tile_px": T, "materials": {}, "transitions": {}, "corner_bits": "TL=1 TR=2 BL=4 BR=8 (set = upper)"}
    for i, mat in enumerate(PAL):
        tiles = [material_tile(mat, 100 + i * 10 + k) for k in range(4)]
        save_sheet(tiles, 4, out / f"terrain_{mat}.png")
        meta["materials"][mat] = f"terrain_{mat}.png"
    for j, (up, lo) in enumerate(PAIRS):
        tiles = transition_set(up, lo, 500 + j * 17)
        save_sheet(tiles, 4, out / f"edge_{up}_over_{lo}.png")
        meta["transitions"][f"{up}>{lo}"] = f"edge_{up}_over_{lo}.png"
    meta["layer_order"] = LAYER_ORDER
    meta["legend"] = LEGEND
    meta["masks"] = {}
    for k, mat in enumerate(LAYER_ORDER):
        save_sheet(mask_set(mat, 900 + k * 13), 4, out / f"mask_{mat}.png")
        meta["masks"][mat] = f"mask_{mat}.png"
    (out / "terrain.json").write_text(json.dumps(meta, indent=2))
    print("terrain tiles ->", out)


if __name__ == "__main__":
    main()
