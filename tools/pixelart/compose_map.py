"""Compose a Pig Tycoon map (layout data + preview images) from the PixelMyth terrain, decor and building sprites.

  python tools/pixelart/compose_map.py farm            # writes Assets/Art/PixelMyth/Maps/farm_*.json|png
  python tools/pixelart/compose_map.py farm --no-render  # data only

Everything is deterministic (fixed seeds, no clocks). Only numpy + Pillow.

Structure: one generic engine (Terrain painter, ObjectSet with collision occupancy, sprite libraries, dual-grid
compositor identical to the Unity cell rule, y-sorted renderer, zone diagram) and one layout function per map
(`farm`; `market_town`, `hub_village`, `forest_primeval` register themselves in LAYOUTS the same way).

Conventions
  * coordinates: tiles (= metres, 32 px), origin bottom-left, y up. Object (x, y) = sprite pivot (base centre).
  * terrain.corner_rows: (H+1) strings of (W+1) chars, FIRST string = TOP row (y = H). Dual grid: the tile at
    (x, y) owns corners TL=(x, y+1) TR=(x+1, y+1) BL=(x, y) BR=(x+1, y).
  * collider {w,h,ox,oy}: metres; (ox, oy) is the CENTRE of the box relative to the sprite pivot (y up).
  * layers: ground (flat things, shadows), actors (y-sorted props/buildings/fences), canopy (tree crowns).
"""
import argparse
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "Art" / "PixelMyth"
TERRAIN_DIR = ART / "Terrain"
DECOR_DIR = ART / "Decor"
BLDG_DIR = ART / "Buildings"
OUT_DIR = ART / "Maps"
T = 32
LEGEND = {"m": "meadow", "p": "path", "s": "stone", "w": "water", "f": "forest",
          "g": "meadow_lush", "d": "meadow_dry", "t": "dirt", "r": "rampart"}
LAYER_ORDER = ["w", "p", "s", "t", "m", "d", "g", "f", "r"]          # bottom -> top, same as terrain.json layer_order
LAYER_RANK = {c: i for i, c in enumerate(LAYER_ORDER)}
LAYERS = {"under": -1, "ground": 0, "actors": 1, "canopy": 2}


# --------------------------------------------------------------------------- noise / geometry helpers
def fbm(w, h, scale, seed, octaves=3):
    """Smooth value noise in [0,1], shape (h, w); lattice upsampled with bicubic."""
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        s = max(1.0, scale / (2 ** o))
        gw, gh = int(w / s) + 3, int(h / s) + 3
        lat = rng.random((gh, gw)).astype(np.float32)
        im = Image.fromarray(lat, "F").resize((int(gw * s), int(gh * s)), Image.BICUBIC)
        out += amp * np.asarray(im)[:h, :w]
        tot += amp
        amp *= 0.5
    out /= tot
    lo, hi = np.percentile(out, 1), np.percentile(out, 99)
    return np.clip((out - lo) / (hi - lo + 1e-6), 0, 1)


def catmull(pts, n=14):
    pts = [tuple(map(float, p)) for p in pts]
    if len(pts) < 3:
        return pts
    p = [pts[0]] + pts + [pts[-1]]
    out = []
    for i in range(1, len(p) - 2):
        p0, p1, p2, p3 = p[i - 1], p[i], p[i + 1], p[i + 2]
        for k in range(n):
            t = k / n
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t * t
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t ** 3) for j in (0, 1)))
    out.append(pts[-1])
    return out


def seg_dist(px, py, a, b):
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    ll = dx * dx + dy * dy or 1.0
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / ll, 0, 1)
    return np.hypot(px - (ax + t * dx), py - (ay + t * dy))


def poly_dist(px, py, pts):
    d = np.full(px.shape, 1e9, np.float32)
    for a, b in zip(pts[:-1], pts[1:]):
        d = np.minimum(d, seg_dist(px, py, a, b))
    return d


def chamfer(mask, iters=40):
    """Approximate euclidean distance (tiles) to the nearest True cell."""
    d = np.where(mask, 0.0, 1e4).astype(np.float32)
    for _ in range(iters):
        p = np.pad(d, 1, constant_values=1e4)
        n = np.minimum.reduce([p[:-2, 1:-1] + 1, p[2:, 1:-1] + 1, p[1:-1, :-2] + 1, p[1:-1, 2:] + 1,
                               p[:-2, :-2] + 1.414, p[:-2, 2:] + 1.414, p[2:, :-2] + 1.414, p[2:, 2:] + 1.414])
        nd = np.minimum(d, n)
        if np.array_equal(nd, d):
            break
        d = nd
    return d


def rect_dist(x, y, r):
    """Distance from points to rect r=(x0,y0,x1,y1) (0 inside)."""
    dx = np.maximum(np.maximum(r[0] - x, 0), x - r[2])
    dy = np.maximum(np.maximum(r[1] - y, 0), y - r[3])
    return np.hypot(dx, dy)


# --------------------------------------------------------------------------- sprite libraries
class Decor:
    def __init__(self):
        meta = json.loads((DECOR_DIR / "decor.json").read_text())
        self.m = {s["file"]: s for s in meta["sprites"]}
        self.cache = {}

    def img(self, f):
        if f not in self.cache:
            self.cache[f] = Image.open(DECOR_DIR / f).convert("RGBA")
        return self.cache[f]

    def pivot(self, f):  # px from top-left
        return tuple(self.m[f]["pivot_px"])

    def size_m(self, f):
        w, h = self.m[f]["size"]
        return w / T, h / T


class Buildings:
    """Sheets Buildings/<name>/<name>_static.png: rows [down,left,up,right], one frame per row."""
    ROWS = ["down", "left", "up", "right"]

    def __init__(self):
        self.cache = {}

    def has(self, name):
        return (BLDG_DIR / name / f"{name}_static.png").exists() and (BLDG_DIR / name / f"{name}_static.json").exists()

    def meta(self, name):
        key = ("meta", name)
        if key not in self.cache:
            p = BLDG_DIR / name / f"{name}_static.json"
            self.cache[key] = json.loads(p.read_text()) if p.exists() else None
        return self.cache[key]

    def row_img(self, name, row):
        """Returns (RGBA crop, pivot_px_from_top_left) or (None, None) when the sheet does not exist yet."""
        key = (name, row)
        if key not in self.cache:
            m = self.meta(name)
            if m is None or not self.has(name):
                self.cache[key] = (None, None)
            else:
                w, h = m["cell"]
                sheet = Image.open(BLDG_DIR / name / f"{name}_static.png").convert("RGBA")
                ri = m["rows"].index(row)
                crop = sheet.crop((0, ri * h, w, (ri + 1) * h))
                px, py = m["pivot_px_from_bottom_left"]
                self.cache[key] = (crop, (px, h - py))
        return self.cache[key]

    def size_m(self, name, default=(6.0, 6.0)):
        m = self.meta(name)
        if m is None:
            return default
        return m["cell"][0] / T, m["cell"][1] / T


class TerrainTiles:
    def __init__(self):
        self.mat = {}
        self.mask = {}
        for c, name in LEGEND.items():
            sheet = np.asarray(Image.open(TERRAIN_DIR / f"terrain_{name}.png").convert("RGBA"))
            self.mat[c] = [sheet[:, i * T:(i + 1) * T] for i in range(4)]
            ms = np.asarray(Image.open(TERRAIN_DIR / f"mask_{name}.png").convert("RGBA"))
            self.mask[c] = [ms[(i // 4) * T:(i // 4 + 1) * T, (i % 4) * T:(i % 4 + 1) * T] for i in range(16)]
        self._cache = {}

    def cell(self, x, y, tl, tr, bl, br):
        """Unity rule: base = full tile (variant (x*7+y*3)%4) of the lowest-order corner material, then each higher
        material present is alpha-composited as mask_<mat>[bits], bits = corners with order >= that material."""
        var = (x * 7 + y * 3) % 4
        key = (var, tl, tr, bl, br)
        if key in self._cache:
            return self._cache[key]
        corners = (tl, tr, bl, br)
        mats = sorted(set(corners), key=LAYER_RANK.get)
        out = self.mat[mats[0]][var][..., :3].astype(np.float32)
        for m in mats[1:]:
            bits = sum((1 << i) for i, c in enumerate(corners) if LAYER_RANK[c] >= LAYER_RANK[m])
            ov = self.mask[m][bits].astype(np.float32)
            a = ov[..., 3:4] / 255.0
            out = out * (1 - a) + ov[..., :3] * a
        res = np.clip(out + 0.5, 0, 255).astype(np.uint8)
        self._cache[key] = res
        return res


# --------------------------------------------------------------------------- the map under construction
class MapBuilder:
    def __init__(self, name, location_id, scene, W, H, seed):
        self.name, self.location_id, self.scene = name, location_id, scene
        self.W, self.H, self.seed = W, H, seed
        self.rng = np.random.default_rng(seed)
        self.c = np.full((H + 1, W + 1), "m", dtype="<U1")           # corner materials, row index = y
        self.cx, self.cy = np.meshgrid(np.arange(W + 1, dtype=np.float32), np.arange(H + 1, dtype=np.float32))
        self.objects = []
        self.blocked = np.zeros((H, W), bool)       # collider footprints (+ water) -> no decor / not walkable
        self.keep = np.zeros((H, W), bool)          # lanes / roads: no blocking decor
        self.decor = Decor()
        self.bld = Buildings()
        self.portals = []
        self.gate_event_zones = []
        self.spawn = None
        self.zones = []
        self.enemy_entries = []
        self.herd_lanes = []                        # (x0,y0,x1,y1) kept free of blockers, for validation only
        self.missing_sheets = []
        self.placeholders = []                      # buildings whose sheet is not rendered yet

    # ---- terrain
    def paint(self, mask, ch):
        self.c[mask] = ch

    def stroke(self, ch, pts, width, wobble=0.0, seed=0, smooth=True):
        pts = catmull(pts) if smooth and len(pts) > 2 else pts
        d = poly_dist(self.cx, self.cy, pts)
        if wobble:
            d = d + (fbm(self.W + 1, self.H + 1, 5, seed + 11) - 0.5) * 2 * wobble
        self.c[d <= width / 2] = ch
        return d

    def corner_rows(self):
        return ["".join(self.c[y]) for y in range(self.H, -1, -1)]

    def tile_mat(self):
        """Per tile dominant material char from its 4 corners (for decor rules)."""
        c = self.c
        stack = np.stack([c[1:, :-1], c[1:, 1:], c[:-1, :-1], c[:-1, 1:]])
        out = np.full((self.H, self.W), "m", dtype="<U1")
        for ch in "fspw":  # higher priority later
            cnt = (stack == ch).sum(0)
            out[cnt >= (3 if ch in "fm" else 1)] = ch
        return out

    # ---- objects
    def add(self, sprite, x, y, layer, collider=None, role="decor", interact=None, sub="", _meta=None):
        o = {"sprite": sprite, "sub": sub, "x": round(float(x), 2), "y": round(float(y), 2), "layer": layer,
             "collider": collider, "role": role, "interact": interact}
        if _meta:
            o["_meta"] = _meta
        self.objects.append(o)
        if collider:
            x0 = x + collider["ox"] - collider["w"] / 2
            x1 = x + collider["ox"] + collider["w"] / 2
            y0 = y + collider["oy"] - collider["h"] / 2
            y1 = y + collider["oy"] + collider["h"] / 2
            self.block_rect(x0, y0, x1, y1)
        return o

    def block_rect(self, x0, y0, x1, y1, margin=0.0):
        xa, xb = int(math.floor(x0 - margin)), int(math.ceil(x1 + margin))
        ya, yb = int(math.floor(y0 - margin)), int(math.ceil(y1 + margin))
        self.blocked[max(ya, 0):max(min(yb, self.H), 0), max(xa, 0):max(min(xb, self.W), 0)] = True

    def free(self, x, y, r=0.5, allow_keep=False):
        xa, xb = int(math.floor(x - r)), int(math.floor(x + r))
        ya, yb = int(math.floor(y - r)), int(math.floor(y + r))
        if xa < 0 or ya < 0 or xb >= self.W or yb >= self.H:
            return False
        if self.blocked[ya:yb + 1, xa:xb + 1].any():
            return False
        if not allow_keep and self.keep[ya:yb + 1, xa:xb + 1].any():
            return False
        return True

    # decor helpers --------------------------------------------------------
    def d_sprite(self, base, v=None, anim=False):
        suf = "_f0.png" if anim else ".png"
        if v is None:
            vs = [c for c in "abcd" if f"{base}_{c}{suf}" in self.decor.m]
            v = vs[int(self.rng.integers(len(vs)))]
        return f"{base}_{v}{suf}"

    def put_decor(self, f, x, y, layer, collider=None, role="decor", shadow=None):
        if shadow:
            sf = f"shadow_ellipse_{shadow}.png"
            self.add(f"Assets/Art/PixelMyth/Decor/{sf}", x, y, "ground")
        return self.add(f"Assets/Art/PixelMyth/Decor/{f}", x, y, layer, collider, role)


# --------------------------------------------------------------------------- decor kinds
COL = {  # sprite family -> (layer, collider or None, shadow size)
    "conifer_tall": ("canopy", {"w": 0.5, "h": 0.4, "ox": 0, "oy": 0.2}, "m"),
    "conifer_small": ("canopy", {"w": 0.45, "h": 0.35, "ox": 0, "oy": 0.18}, "s"),
    "shrub_round": ("actors", {"w": 0.8, "h": 0.5, "ox": 0, "oy": 0.25}, "m"),
    "shrub_dark": ("actors", {"w": 0.8, "h": 0.5, "ox": 0, "oy": 0.25}, "m"),
    "boulder_s": ("ground", None, "s"),
    "boulder_m": ("actors", {"w": 0.9, "h": 0.6, "ox": 0, "oy": 0.3}, "m"),
    "boulder_l": ("actors", {"w": 1.4, "h": 0.7, "ox": 0, "oy": 0.35}, "l"),
    "ruin_pillar": ("actors", {"w": 0.7, "h": 0.5, "ox": 0, "oy": 0.25}, "m"),
    "ruin_cairn": ("actors", {"w": 0.9, "h": 0.6, "ox": 0, "oy": 0.3}, "m"),
    "ruin_wall": ("actors", {"w": 1.8, "h": 0.6, "ox": 0, "oy": 0.3}, "l"),
    "ruin_arch_fragment": ("actors", {"w": 1.5, "h": 0.6, "ox": 0, "oy": 0.3}, "l"),
}


def place(mb, fam, x, y, v=None, shadow=True, role="decor", allow_keep=False, check=True):
    """Place a decor family member with its standard layer/collider/shadow. Returns True if placed."""
    layer, col, sh = COL[fam]
    r = (col["w"] / 2 + 0.15) if col else 0.2
    if check and not mb.free(x, y, r if col else 0.3, allow_keep=allow_keep and not col):
        return False
    f = mb.d_sprite(fam, v)
    mb.put_decor(f, x, y, layer, col, role, shadow=sh if shadow else None)
    return True


def flat(mb, fam, x, y, anim=False, v=None, allow_keep=False, check=True):
    if check and not mb.free(x, y, 0.3, allow_keep=allow_keep):
        return False
    if fam == "stepping_stone":
        f = mb.d_sprite(fam, "abc"[int(mb.rng.integers(3))])
    elif fam.startswith("grass_tuft"):
        f = mb.d_sprite(fam, v, anim=True)
    elif fam == "flame_flower":
        f = mb.d_sprite(fam, v, anim=True)
    else:
        f = mb.d_sprite(fam, v)
    mb.put_decor(f, x, y, "ground")
    return True


# --------------------------------------------------------------------------- rich-map helpers (families from gen_decor_extra)
COL.update({
    "conifer_giant": ("canopy", {"w": 0.6, "h": 0.5, "ox": 0, "oy": 0.25}, "l"),
    "conifer_mid": ("canopy", {"w": 0.5, "h": 0.4, "ox": 0, "oy": 0.2}, "m"),
    "oak_gold": ("canopy", {"w": 0.9, "h": 0.5, "ox": 0, "oy": 0.25}, "l"),
    "tree_amber": ("canopy", {"w": 0.7, "h": 0.4, "ox": 0, "oy": 0.2}, "m"),
    "log_fallen": ("actors", {"w": 1.6, "h": 0.35, "ox": 0, "oy": 0.2}, "m"),
    "stump": ("actors", {"w": 0.5, "h": 0.4, "ox": 0, "oy": 0.2}, "s"),
    "hay_bale_sq": ("actors", {"w": 0.8, "h": 0.5, "ox": 0, "oy": 0.25}, "s"),
    "hay_bale_round": ("actors", {"w": 0.8, "h": 0.5, "ox": 0, "oy": 0.25}, "s"),
    "hay_pile": ("actors", {"w": 1.3, "h": 0.6, "ox": 0, "oy": 0.3}, "m"),
    "crate": ("actors", {"w": 0.7, "h": 0.5, "ox": 0, "oy": 0.25}, "s"),
    "crate_stack": ("actors", {"w": 0.9, "h": 0.5, "ox": 0, "oy": 0.25}, "m"),
    "barrel": ("actors", {"w": 0.55, "h": 0.45, "ox": 0, "oy": 0.22}, "s"),
    "sack_pile": ("actors", {"w": 0.9, "h": 0.5, "ox": 0, "oy": 0.25}, "s"),
    "woodpile": ("actors", {"w": 1.0, "h": 0.5, "ox": 0, "oy": 0.25}, "s"),
    "market_table": ("actors", {"w": 1.3, "h": 0.5, "ox": 0, "oy": 0.25}, "m"),
    "feed_trough": ("actors", {"w": 1.4, "h": 0.5, "ox": 0, "oy": 0.25}, "s"),
    "statue_weathered": ("actors", {"w": 0.9, "h": 0.6, "ox": 0, "oy": 0.3}, "m"),
    "sun_stele": ("actors", {"w": 0.7, "h": 0.5, "ox": 0, "oy": 0.25}, "m"),
    "well": ("actors", {"w": 1.3, "h": 0.8, "ox": 0, "oy": 0.4}, "m"),
    "hay_cart": ("actors", {"w": 2.0, "h": 0.8, "ox": 0, "oy": 0.4}, "l"),
    "bench": ("actors", {"w": 1.2, "h": 0.4, "ox": 0, "oy": 0.2}, "s"),
    "fountain": ("actors", {"w": 2.4, "h": 1.2, "ox": 0, "oy": 0.6}, "l"),
    "lantern_post": ("actors", {"w": 0.3, "h": 0.3, "ox": 0, "oy": 0.15}, None),
    "torch_post": ("actors", {"w": 0.3, "h": 0.3, "ox": 0, "oy": 0.15}, None),
    "scarecrow": ("actors", {"w": 0.4, "h": 0.3, "ox": 0, "oy": 0.15}, "s"),
    "signpost": ("actors", {"w": 0.4, "h": 0.3, "ox": 0, "oy": 0.15}, "s"),
    # non-blocking dressing
    "grass_tall": ("ground", None, None), "grass_wide": ("ground", None, None), "wildflowers": ("ground", None, None),
    "reeds": ("actors", None, None), "lilypad": ("ground", None, None), "fern": ("ground", None, None),
    "glow_flower": ("actors", None, None), "glow_mushroom": ("ground", None, None), "mist": ("ground", None, None),
    "water_deep": ("under", None, None), "water_ripple": ("ground", None, None),
    "sun_inlay": ("ground", None, None), "inlay_line": ("ground", None, None), "rubble": ("ground", None, None),
    "flagstone_loose": ("ground", None, None), "sack": ("actors", None, "s"), "basket": ("actors", None, None),
    "flower_box": ("actors", None, None), "laundry_line": ("actors", None, "s"),
    "banner": ("actors", {"w": 0.3, "h": 0.3, "ox": 0, "oy": 0.15}, None),
})


def deco(mb, fam, x, y, v=None, check=True, collider=True, allow_keep=False, shadow=True, file=None, role="decor"):
    """Generic placement for any family in COL (auto-detects animated families). collider=False strips the blocker."""
    layer, col, sh = COL[fam]
    if not collider:
        col = None
    if check:
        r = (col["w"] / 2 + 0.15) if col else 0.3
        if not mb.free(x, y, r, allow_keep=allow_keep and not col):
            return False
    anim = f"{fam}_a_f0.png" in mb.decor.m
    f = file or mb.d_sprite(fam, v, anim=anim)
    mb.put_decor(f, x, y, layer, col, role, shadow=sh if (shadow and (col or sh)) else None)
    return True


def ground_variation(mb, seed, lush=0.64, dry=0.70, dirt=0.95, region=None, moist=None, dirt_extra=None):
    """Paint meadow_lush (g) / meadow_dry (d) / dirt (t) over the plain-meadow corners with world-space noise: large
    soft colour drifts (lush near water/forest, dry in the open middle) plus small worn-earth patches."""
    W, H = mb.W, mb.H
    a = fbm(W + 1, H + 1, 15, seed, 3)
    b = fbm(W + 1, H + 1, 21, seed + 1, 3)
    c = fbm(W + 1, H + 1, 5, seed + 2, 2)
    if moist is not None:
        a = a + moist
    m = mb.c == "m"
    if region is not None:
        m &= region
    g = m & (a > lush)
    d = m & ~g & (b > dry)
    t = m & ~g & ~d & (c > dirt)
    if dirt_extra is not None:
        t |= m & dirt_extra
    mb.c[g] = "g"
    mb.c[d] = "d"
    mb.c[t] = "t"


def worn_paths(mb, seed, inner=1.5, thr=0.92):
    """Darker worn-earth ribbon along the middle of wide paths (cart ruts), broken up by noise."""
    pm = mb.c == "p"
    d = chamfer(~pm)
    n = fbm(mb.W + 1, mb.H + 1, 6, seed, 2)
    mb.c[pm & (d >= inner) & (n > thr)] = "t"


def edge_dress(mb, rng, prob_stone=0.12, prob_path=0.06, margin=4):
    """Broken plaza edges and path verges: rubble / loose flagstones, pebbles, tufts, flowers on tiles at borders."""
    tm = mb.tile_mat()
    H, W = tm.shape
    pad = np.pad(tm, 1, constant_values="m")
    for y in range(H):
        for x in range(W):
            t = tm[y, x]
            if t not in "sp" or min(x, y, W - 1 - x, H - 1 - y) < margin:
                continue
            nb = {pad[y + 1 + dy, x + 1 + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))}
            if t == "s" and (nb - {"s", "w"}):
                if rng.random() < prob_stone:
                    deco(mb, str(rng.choice(["rubble", "flagstone_loose", "rubble"])), x + 0.5 + rng.uniform(-0.5, 0.5),
                         y + 0.5 + rng.uniform(-0.5, 0.5), check=False)
            elif t == "p" and ("m" in nb) and rng.random() < prob_path:
                k = rng.random()
                px, py = x + 0.5 + rng.uniform(-0.5, 0.5), y + 0.5 + rng.uniform(-0.5, 0.5)
                if k < 0.4:
                    flat(mb, "stepping_stone", px, py, check=False)
                elif k < 0.7:
                    flat(mb, "grass_tuft_16x16", px, py, anim=True, check=False)
                else:
                    deco(mb, "wildflowers", px, py, check=False)


def pen(mb, x0, y0, x1, y1, gap=None, collide=False):
    """Decorative wooden pen from the existing fence_wood_* sprites (non-blocking unless collide=True)."""
    col_r = {"w": 0.9, "h": 0.25, "ox": 0, "oy": 0.12} if collide else None
    col_p = {"w": 0.25, "h": 0.25, "ox": 0, "oy": 0.12} if collide else None
    col_v = {"w": 0.25, "h": 0.9, "ox": 0, "oy": 0.45} if collide else None
    for xx in np.arange(x0, x1, 1.0):
        for yy in (y0, y1):
            if gap and yy == gap[0] and gap[1] <= xx < gap[2]:
                continue
            mb.put_decor(mb.d_sprite("fence_wood_rail_h"), xx + 0.5, yy, "actors", col_r)
    for yy in np.arange(y0, y1, 1.0):
        for xx in (x0, x1):
            mb.put_decor("fence_wood_rail_v_a.png", xx, yy + 1.0, "actors", col_v)
    for xx in np.arange(x0, x1 + 0.1, 1.0):
        for yy in (y0, y1):
            if gap and yy == gap[0] and gap[1] <= xx <= gap[2]:
                continue
            mb.put_decor(mb.d_sprite("fence_wood_post"), xx, yy, "actors", col_p)
    for yy in np.arange(y0 + 1.0, y1, 1.0):
        for xx in (x0, x1):
            mb.put_decor(mb.d_sprite("fence_wood_post"), xx, yy, "actors", col_p)


def lamp_line(mb, pts, fam="lantern_post", v=None, check=True, allow_keep=True):
    for x, y in pts:
        deco(mb, fam, x, y, v=v, check=check, allow_keep=allow_keep)


def poisson(rng, region_fn, bounds, min_d, tries=14000, k=None):
    """Dart-throwing Poisson points inside bounds=(x0,y0,x1,y1) satisfying region_fn(x,y)."""
    pts = []
    cell = min_d / math.sqrt(2)
    gw = int((bounds[2] - bounds[0]) / cell) + 2
    gh = int((bounds[3] - bounds[1]) / cell) + 2
    grid = {}
    for _ in range(tries):
        x = rng.uniform(bounds[0], bounds[2])
        y = rng.uniform(bounds[1], bounds[3])
        if not region_fn(x, y):
            continue
        gx, gy = int((x - bounds[0]) / cell), int((y - bounds[1]) / cell)
        ok = True
        for ix in range(gx - 2, gx + 3):
            for iy in range(gy - 2, gy + 3):
                q = grid.get((ix, iy))
                if q is not None and (q[0] - x) ** 2 + (q[1] - y) ** 2 < min_d * min_d:
                    ok = False
                    break
            if not ok:
                break
        if ok:
            grid[(gx, gy)] = (x, y)
            pts.append((x, y))
            if k and len(pts) >= k:
                break
    return pts


# --------------------------------------------------------------------------- renderer
def render(mb, scale_note=""):
    W, H = mb.W, mb.H
    tt = TerrainTiles()
    can = np.zeros((H * T, W * T, 3), np.uint8)
    c = mb.c
    for y in range(H):
        ry = (H - 1 - y) * T
        for x in range(W):
            can[ry:ry + T, x * T:(x + 1) * T] = tt.cell(x, y, c[y + 1, x], c[y + 1, x + 1], c[y, x], c[y, x + 1])
    img = Image.fromarray(can, "RGB")
    rank = lambda o: LAYERS[o["layer"]]
    ground = [o for o in mb.objects if o["layer"] == "ground"]
    rest = [o for o in mb.objects if o["layer"] != "ground"]
    ground.sort(key=lambda o: (-o["y"], o["x"]))
    rest.sort(key=lambda o: (-o["y"], o["x"]))
    d = ImageDraw.Draw(img)
    for o in ground + rest:
        spr, piv = sprite_for(mb, o)
        px, py = o["x"] * T, (H - o["y"]) * T
        if spr is None:
            w = 6 * T
            d.rectangle([px - w / 2, py - w * 0.6, px + w / 2, py], outline=(255, 0, 255), width=2)
            d.text((px - w / 2 + 4, py - w * 0.6 + 4), "MISSING " + o["sprite"].split("/")[-1], fill=(255, 0, 255))
            continue
        img.paste(spr, (int(round(px - piv[0])), int(round(py - piv[1]))), spr)
    return img


def sprite_for(mb, o):
    path = o["sprite"]
    if "/Buildings/" in path:
        meta = o.get("_meta") or {}
        name = meta.get("name")
        row = meta.get("row", "down")
        return mb.bld.row_img(name, row)
    f = path.split("/")[-1]
    return mb.decor.img(f), mb.decor.pivot(f)


def zone_diagram(mb, base_img, path):
    s = 0.25
    img = base_img.resize((int(base_img.width * s), int(base_img.height * s)), Image.LANCZOS).convert("RGBA")
    ov = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    H = mb.H
    colors = {"service_core": (230, 70, 60), "pasture": (240, 220, 60), "defense_strip": (70, 120, 255),
              "outer_ring": (60, 200, 90), "quarantine": (200, 60, 220), "processing": (255, 140, 30)}
    try:
        font = ImageFont.load_default(size=14)
    except TypeError:
        font = ImageFont.load_default()
    rects = lambda r: [r[0] * T * s, (H - r[1] - r[3]) * T * s, (r[0] + r[2]) * T * s, (H - r[1]) * T * s]
    for z in mb.zones:
        col = colors.get(z["name"], (255, 255, 255))
        d.rectangle(rects(z["rect"]), fill=col + (60,), outline=col + (255,), width=2)
    for z in mb.zones:
        col = colors.get(z["name"], (255, 255, 255))
        rr = rects(z["rect"])
        if z["rect"][2] * z["rect"][3] > 150:
            d.text((rr[0] + 6, rr[1] + 4), z["name"], fill=(255, 255, 255, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0, 255))
    for g in mb.gate_event_zones:
        d.rectangle(rects(g), outline=(255, 255, 255, 255), width=2)
        d.text((rects(g)[0] + 2, rects(g)[1] + 2), "gate-ev", fill=(255, 255, 255, 255), font=font, stroke_width=1, stroke_fill=(0, 0, 0, 255))
    for ln in mb.herd_lanes:
        d.rectangle(rects((ln[0], ln[1], ln[2] - ln[0], ln[3] - ln[1])), outline=(0, 255, 255, 255), width=1)
    for p in mb.portals:
        x, y = p["x"] * T * s, (H - p["y"]) * T * s
        d.ellipse([x - 9, y - 9, x + 9, y + 9], fill=(255, 0, 255, 255), outline=(255, 255, 255, 255), width=2)
        d.text((x - 60, y + 12), p["id"], fill=(255, 255, 255, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0, 255))
    for e in mb.enemy_entries:
        x, y = e["x"] * T * s, (H - e["y"]) * T * s
        d.polygon([(x, y - 8), (x + 8, y + 6), (x - 8, y + 6)], fill=(255, 40, 40, 255), outline=(255, 255, 255, 255))
        d.text((x + 9, y - 8), e["sector"], fill=(255, 255, 255, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0, 255))
    sx, sy = mb.spawn["x"] * T * s, (H - mb.spawn["y"]) * T * s
    d.ellipse([sx - 7, sy - 7, sx + 7, sy + 7], fill=(0, 255, 120, 255), outline=(0, 0, 0, 255), width=2)
    d.text((sx + 9, sy - 7), "spawn", fill=(255, 255, 255, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0, 255))
    Image.alpha_composite(img, ov).convert("RGB").save(path)


# --------------------------------------------------------------------------- validation
def clearance_ok(mb, region, radius):
    """Boolean grid of tiles whose disk of `radius` tiles (inside region rect) is free of blockers."""
    free = ~mb.blocked
    d = chamfer(~free)  # distance to nearest blocked tile
    return free & (d >= radius)


def validate(mb):
    rep = {}
    W, H = mb.W, mb.H
    wide = clearance_ok(mb, None, 3.0)           # lanes >= 6 m wide <=> disk radius 3 free
    # reachability of the wide-clearance region from the spawn
    from collections import deque
    sx, sy = int(mb.spawn["x"]), int(mb.spawn["y"])
    seen = np.zeros_like(wide)
    q = deque()
    if wide[sy, sx]:
        seen[sy, sx] = True
        q.append((sx, sy))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < W and 0 <= ny < H and wide[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                q.append((nx, ny))
    rep["wide_region_from_spawn_tiles"] = int(seen.sum())
    rep["spawn_in_wide_region"] = bool(wide[sy, sx])
    return rep, seen, wide


LAYOUTS = {}


def layout(name):
    def deco(fn):
        LAYOUTS[name] = fn
        return fn
    return deco


# =========================================================================== FARM
FARM_W, FARM_H = 184, 160
FENCE = (32, 34, 152, 124)           # x0,y0,x1,y1  (120 x 90 m, GDD start size)
MAIN_GATE = (92.0, 124.0, 6.0)       # centre x, y on the fence line, width
SIDE_GATE = (152.0, 62.0, 4.0)       # east fence, 4 m
WEST_GATE = (32.0, 100.0, 4.0)       # west fence, 4 m, toward the village road
# building plan: id -> (sprite name, x, y, facing row, role id, interact)
BUILDING_PLAN = {}  # filled by farm()


PINNED_GATE_ZONES = [[48.0, 64.0, 8.0, 8.0], [50.0, 76.0, 8.0, 8.0], [52.0, 48.0, 8.0, 8.0], [60.0, 84.0, 8.0, 8.0],
                     [88.0, 48.0, 8.0, 8.0], [100.0, 48.0, 8.0, 8.0], [102.0, 62.0, 8.0, 8.0]]
DOOR_PADS = [  # x, y, rx, ry : small stone aprons in front of the doors + a modest courtyard (plaza area cut ~60%)
    (92.0, 107.4, 4.4, 1.9), (82.5, 110.2, 2.3, 1.0), (101.0, 110.8, 2.0, 0.95), (113.0, 105.0, 1.7, 0.85),
    (122.0, 112.4, 2.0, 0.95), (69.0, 107.4, 1.6, 0.85), (76.0, 96.4, 1.6, 0.8), (92.0, 95.4, 1.7, 0.8),
    (52.0, 109.4, 3.0, 1.1),
]
DOOR_PATHS = [  # polyline, width
    ([(87, 106.6), (85, 108.8), (83, 110.0)], 2.2), ([(97, 106.6), (99, 109), (101, 110.6)], 2.2),
    ([(99, 105), (106, 105.4), (112, 105.0)], 2.2), ([(116, 105.5), (119.5, 108.5), (121.5, 112.2)], 2.2),
    ([(86, 106.5), (78, 107.4), (70, 107.4)], 2.2), ([(88, 104.5), (82, 100.5), (77, 96.8)], 2.2),
    ([(92, 104), (92, 97)], 2.6), ([(57, 109.4), (64, 108.6), (70, 107.6)], 2.2),
]


def _stone_blob(mb, cx, cy, rx, ry, seed, rough=0.25):
    n = fbm(mb.W + 1, mb.H + 1, 6, seed)
    v = ((mb.cx - cx) / rx) ** 2 + ((mb.cy - cy) / ry) ** 2 + (n - 0.5) * rough
    return v < 1


def add_building(mb, name, x, y, row, role_id, kind, interact=None, foot=None):
    """kind 'building' or 'prop'. foot = (w,h) metres of the base footprint for the collider (default from sprite)."""
    col = None if foot is None else {"w": round(foot[0], 2), "h": round(foot[1], 2), "ox": 0.0, "oy": 0.0}
    sub = f"{name}_static_{row}"
    if not mb.bld.has(name):
        mb.placeholders.append(name)
    o = mb.add(f"Assets/Art/PixelMyth/Buildings/{name}/{name}_static.png", x, y, "actors", col,
               f"{kind}:{role_id}", interact, sub=sub, _meta={"name": name, "row": row})
    return o


@layout("farm")
def farm(args=None):
    W, H = FARM_W, FARM_H
    mb = MapBuilder("Farm", "Farm", "Farm_Main", W, H, seed=20261030)
    rng = mb.rng
    fx0, fy0, fx1, fy1 = FENCE
    cx, cy = mb.cx, mb.cy
    dfence = rect_dist(cx, cy, FENCE)
    edge = np.minimum.reduce([cx, cy, W - cx, H - cy])

    # ---------------- 1. forest belt in the outer ring (dense toward the rim, open near the lane)
    n_f = fbm(W + 1, H + 1, 14, 101, 3)
    forest = (dfence > 12) & ((n_f + 0.03 * (dfence - 12)) > 0.5)
    forest |= (dfence > 20) & (n_f > 0.2)
    mb.paint(forest, "f")
    for rx_, ry_, _k in RUIN_SITES:   # clearings for the ruin focal points
        mb.paint((((cx - rx_) / 10.5) ** 2 + ((cy - ry_) / 7.5) ** 2) < 1, "m")

    # ---------------- 2. stone rim at the map edge (low wall of the diorama)
    rim = edge <= 2.0
    mb.paint(rim, "s")

    # ---------------- 3. the single pond (pasture, off-centre) and the NE stream
    POND = (76.0, 66.0, 11.5, 6.2)
    pond = _stone_blob(mb, POND[0], POND[1], POND[2], POND[3], 303, 0.55)
    mb.paint(pond, "w")
    stream_pts = [(176, 161), (170, 152), (166, 143), (169, 134), (176, 126), (185, 121)]
    stream_d = poly_dist(cx, cy, catmull(stream_pts)) + (fbm(W + 1, H + 1, 6, 77) - 0.5) * 2.0
    stream = stream_d <= 2.7
    mb.paint(stream, "w")

    # ---------------- 4. outer lane (rounded rectangle ring ~9 m outside the fence), roads, trails
    lane_d = np.abs(dfence - 9.0)
    lane = lane_d <= 1.0 + (fbm(W + 1, H + 1, 7, 55) - 0.5) * 0.6   # a trail (GDD 3.6), not a ring road
    # keep woods off the lane/roads: clear forest within lane
    mb.paint(lane & (edge > 2.5), "p")
    gx = MAIN_GATE[0]
    J1 = (gx, 133.0)
    road_main = [(gx, 124.0), (gx, 133.0)]
    mb.stroke("p", road_main, 5.0)
    # NE road: junction -> over the stone bridge -> portal on the east rim
    # portals sit on the north rim a short walk from the main gate (they were 90 m away in the far corners)
    # user 02/11: the town and the village lie in two different directions - town north, village west
    ne_pts = [J1, (104, 141), (116, 151), (122, 160.5)]
    w_pts = [J1, (78, 137), (52, 136.5), (30, 130), (14, 116), (-0.5, 112)]
    mb.stroke("p", ne_pts, 3.6, 0.4, 5)
    mb.stroke("p", w_pts, 3.6, 0.4, 6)
    mb.stroke("p", [(32.0, 100.0), (24.0, 100.0), (21.0, 108.0), (16.5, 114.0)], 3.0, 0.3, 61)   # west gate -> village road
    # side gate road (east, 4 m) joining the lane
    sgx, sgy, sgw = SIDE_GATE
    mb.stroke("p", [(sgx - 4, sgy), (sgx + 4, sgy), (160.5, sgy)], 4.5, smooth=False)
    # bridge: stone deck across the stream along the NE road (road at y~144.5 where stream x~166)
    deck = np.zeros_like(stream)          # no road crosses the stream any more

    # ---------------- 5. inside the fence: plazas + service core ground
    core_x0, core_x1, core_y0, core_y1 = 56, 128, 94, 124
    PLAZA = (92.0, 107.0)
    # purposeful courtyards: worn path lanes to every door, small flagstone aprons in front of the doors, one
    # central courtyard around the notice board, a narrow stone avenue from the gate (no more grey field)
    for i, (pts, wd) in enumerate(DOOR_PATHS):
        mb.stroke("p", pts, wd, 0.25, 40 + i)
    mb.stroke("s", [(gx, 124.0), (gx, 111.0)], 2.6, 0.25, 77, smooth=False)
    for i, (bx, by, brx, bry) in enumerate(DOOR_PADS):
        mb.paint(_stone_blob(mb, bx, by, brx, bry, 700 + i * 13, 0.55), "s")
    # the pasture is an open field (user 31 Oct): no service road or herd spine is painted across it; the side gate
    # only gets a short apron, and trampled earth near the shelter comes from ground_variation's wear patch
    mb.stroke("p", [(sgx - 1, sgy), (sgx - 6, sgy)], 3.2, 0.3, 9, smooth=False)
    # processing pad (stone) east, quarantine pad (path)
    # processing yard: trodden earth with a flagstone working apron, not a grey slab
    # (no fixed processing pad: GDD 3.7 lets the player place Khu Xử Lý / Khu Cách Ly)

    # trails from enemy entries to the lane (also the visible 'mouth' of each approach)
    # (entries are decided below; painted after)

    # ---------------- 6. enemy entries on the outer ring (8-12, four sectors)
    entries = [
        (46, 154, "N"), (92, 156, "N"), (148, 154, "N"),
        (178, 112, "E"), (178, 76, "E"), (178, 36, "E"),
        (146, 6, "S"), (96, 5, "S"), (46, 6, "S"),
        (5, 44, "W"), (5, 84, "W"), (5, 140, "W"),
    ]
    entries = [e for e in entries if not (e[2] == "N" and e[0] == 92)]   # keep the main road clear
    for ex, ey, sec in entries:
        # nearest lane point
        d = np.hypot(cx - ex, cy - ey)
        lane_pts = np.where(lane & (edge > 3))
        k = np.argmin((lane_pts[1] - ex) ** 2 + (lane_pts[0] - ey) ** 2)
        tx, ty = lane_pts[1][k], lane_pts[0][k]
        mid = ((ex + tx) / 2 + rng.uniform(-3, 3), (ey + ty) / 2 + rng.uniform(-3, 3))
        sd = poly_dist(cx, cy, catmull([(ex, ey), mid, (tx, ty)])) + (fbm(W + 1, H + 1, 5, 88) - 0.5) * 1.2
        # GDD 3.8: the sector is intel (Radar / scouting / Mộng Nhãn) - no painted trail gives it away
        mb.enemy_entries.append({"x": float(ex), "y": float(ey), "sector": sec})

    # nothing may grow on forest near paths: repaint meadow halo where path touches forest
    path_c = (mb.c == "p")
    halo = chamfer_corner(path_c, 2.5)
    mb.paint(halo & (mb.c == "f"), "m")
    # rim must not cover the road exits: re-assert path at portal mouths
    mb.stroke("p", [(122, 157), (122, 160.5)], 3.6, smooth=False)
    mb.stroke("p", [(4, 112), (-0.5, 112)], 3.6, smooth=False)
    # re-assert the pond/stream after the roads (no path over water, except the stone bridge deck)
    mb.paint(pond, "w")
    mb.paint(stream & ~deck, "w")
    mb.paint(deck, "s")

    # ---------------- 6b. ground richness: lush / dry / dirt drifts + worn cart ruts + South battlefront at (92, 31)
    worn_paths(mb, 910)
    moist = 0.20 * np.exp(-chamfer(mb.c == "w") / 9.0) + 0.14 * np.exp(-chamfer(mb.c == "f") / 7.0)
    moist = moist + 0.16 * np.exp(-(((cx - 92) / 34.0) ** 2 + ((cy - 108) / 12.0) ** 2))   # greener courtyard meadow
    wear = (((cx - 92) / 3.0) ** 2 + ((cy - 91.5) / 1.6) ** 2 + (fbm(W + 1, H + 1, 4, 931) - 0.5) * 0.9 < 1)
    battlefront_wear = (((cx - 92.0) / 8.5) ** 2 + ((cy - 30.5) / 3.4) ** 2 + (fbm(W + 1, H + 1, 4, 937) - 0.5) * 0.65 < 1)
    wear = wear | battlefront_wear
    ground_variation(mb, 920, lush=0.57, moist=moist, dirt_extra=wear)

    # ---------------- 7. fence ring, gates (objects)
    place_fence(mb)

    # ---------------- 8. buildings (service core) + starter props + towers
    place_service_core(mb, PLAZA)

    # keep mask from path/stone corners (decor must not litter roads)
    pm = mb.tile_mat()
    mb.keep |= (pm == "p") | (pm == "s")
    # water tiles block
    mb.blocked |= (pm == "w")
    # bridge is walkable
    # ---------------- 9. decor
    decorate(mb, dfence, edge, forest, POND, lane, pond)
    farm_richness(mb, POND)
    farm_rhythm(mb, POND)
    mb.herd_lanes = [(86.0, 78.0, 98.0, 98.0), (40.0, 96.0, 44.0, 112.0)]
    pasture_meadow(mb, POND)

    # ---------------- 10. data: portals, spawn, zones, gate-event zones
    mb.portals = [
        {"id": "Farm->MarketTown", "target": "MarketTown", "target_portal": "MarketTown->Farm", "x": 122.0, "y": 158.5},
        {"id": "Farm->Hub_Village", "target": "Hub_Village", "target_portal": "Hub_Village->Farm", "x": 1.5, "y": 112.0},
    ]
    mb.spawn = {"x": 92.0, "y": 101.0}
    mb.zones = [
        {"name": "service_core", "rect": [56, 94, 72, 30]},
        {"name": "pasture", "rect": [44, 46, 96, 48]},
        # GDD 3.6: a 12-20 m strip inside the fence, 20-25% of the interior: W + E full, S centre = 2700 m2 = 25%
        {"name": "defense_strip", "rect": [32, 34, 12, 90]},
        {"name": "defense_strip", "rect": [140, 34, 12, 90]},
        {"name": "defense_strip", "rect": [69.5, 34, 45, 12]},
        # GDD 3.7: Nơi Trú Đàn = a zone next to the core with >= 2 entrances >= 6 m (open on three sides here)
        {"name": "refuge", "rect": [82, 84, 20, 12]},
        {"name": "outer_ring", "rect": [0, 124, W, H - 124]},
        {"name": "outer_ring", "rect": [0, 0, W, 34]},
        {"name": "outer_ring", "rect": [0, 34, 32, 90]},
        {"name": "outer_ring", "rect": [152, 34, W - 152, 90]},
    ]
    # GDD 3.6 / B2: 12 Ô Đất (10 x 10 m grazing plots) at the start, inside the pasture, clear of pond and refuge
    pond_box = (62, 57, 92, 75)
    plots = [(x, y) for y in (47, 59, 71, 83) for x in (46, 58, 94, 106, 118, 128)
             if not (x < pond_box[2] and x + 10 > pond_box[0] and y < pond_box[3] and y + 10 > pond_box[1])
             and not (x < 102 and x + 10 > 82 and y + 10 > 84) and not in_fence_notch(x, y, 10, 10)][:12]
    mb.zones += [{"name": f"land_plot_{i + 1:02d}", "rect": [float(x), float(y), 10.0, 10.0]} for i, (x, y) in enumerate(plots)]
    mb.herd_lanes = [(86.0, 78.0, 98.0, 98.0), (40.0, 96.0, 44.0, 112.0)]
    choose_gate_zones(mb, POND)
    # pinned: Unity and the gate events were built against these rectangles; decor changes must not move them
    mb.gate_event_zones = [list(z) for z in PINNED_GATE_ZONES]
    # Unity reads them as zones: MysticGate rises at a random free spot inside one of these when an event opens
    mb.zones += [{"name": "gate_event", "rect": list(z)} for z in PINNED_GATE_ZONES]
    gz = mb.gate_event_zones[len(mb.gate_event_zones) // 2]
    mb.portals.append({"id": "Farm->Forest_Primeval", "target": "Forest_Primeval", "target_portal": "Forest_Primeval->Farm",
                       "x": gz[0] + gz[2] / 2, "y": gz[1] + gz[3] / 2})   # hidden until a mystic event opens it
    return mb


def tile_keep(mb, corner_mask):
    """Tile-level boolean (H,W) true if any of its 4 corners is in corner_mask."""
    c = corner_mask
    return c[1:, :-1] | c[1:, 1:] | c[:-1, :-1] | c[:-1, 1:]


def chamfer_corner(mask, r):
    return chamfer(mask) <= r


# ---------------------------------------------------------------- fence
# Sheet facts (measured on the rendered sheets, camera elevation 30 deg => depth is foreshortened by 0.5):
#   fence_segment  down/up = run along x, 4.06 m wide, pivot = segment centre on the ground.
#                  left/right = run along y, covers only +-1.0 m of tile-y on screen (4 m of 3-D length), so step 2.0.
#   fence_corner   down=SW left=SE up=NE right=NW (arms: x 1.9 m, y ~1.0 m)
#   fence_gate_arch down/up = gate in an x-run, posts +-3.5 m ; left/right = gate in a y-run, posts +-1.7 m
FENCE_ROWS = {"seg_h": "down", "seg_v": "left", "SW": "down", "SE": "left", "NE": "up", "NW": "right",
              "gate_h": "down", "gate_v": "left"}
CORNER_ARM = (1.9, 1.0)       # x arm, y arm in tile units
SEG_STEP = (4.0, 2.0)         # nominal step along x runs and y runs
GATE_HALF = {"h": 3.55, "v": 2.2}
QUARANTINE = (118, 72, 20, 16)


def fence_rect(mb, rect, gates, seed, patched=True, role="fence"):
    """Rectangle of fence_segment pieces with corners and gate arches. gates: list of (side 'N|S|E|W', centre)."""
    x0, y0, x1, y1 = rect
    ax, ay = CORNER_ARM
    r = np.random.default_rng(seed)

    def runs(a, b, gaps):
        out, cur = [], a
        for g0, g1 in sorted(gaps):
            if g0 > cur:
                out.append((cur, g0))
            cur = max(cur, g1)
        if cur < b:
            out.append((cur, b))
        return out

    def seg_name():
        return "fence_patched" if (patched and r.random() < 0.07) else "fence_segment"

    for side in "NSEW":
        horiz = side in "NS"
        fixed = y1 if side == "N" else y0 if side == "S" else x1 if side == "E" else x0
        a, b = (x0 + ax, x1 - ax) if horiz else (y0 + ay, y1 - ay)
        kind = "h" if horiz else "v"
        gaps = [(c - GATE_HALF[kind], c + GATE_HALF[kind]) for s, c in gates if s == side]
        for ra, rb in runs(a, b, gaps):
            n = max(1, round((rb - ra) / SEG_STEP[0 if horiz else 1]))
            st = (rb - ra) / n
            for i in range(n):
                c = ra + st * (i + 0.5)
                name = seg_name()
                if horiz:
                    add_fence_piece(mb, name, c, fixed, FENCE_ROWS["seg_h"], "h", role)
                else:
                    add_fence_piece(mb, name, fixed, c, FENCE_ROWS["seg_v"], "v", role)
        for s, c in gates:
            if s == side:
                if horiz:
                    add_fence_piece(mb, "fence_gate_arch", c, fixed, FENCE_ROWS["gate_h"], "gate", role)
                else:
                    add_fence_piece(mb, "fence_gate_arch", fixed, c, FENCE_ROWS["gate_v"], "gate", role)
    for (cx_, cy_), key in (((x0, y0), "SW"), ((x1, y0), "SE"), ((x0, y1), "NW"), ((x1, y1), "NE")):
        add_fence_piece(mb, "fence_corner", cx_, cy_, FENCE_ROWS[key], "c", role)


# user 02/11: the pasture is not a box. Rectilinear outline inside FENCE's bounding box, counter-clockwise from the
# south-west: a 24 x 18 m bite at the SW (pond side), a 16 x 18 m bite at the NE, a 12 x 10 m step at the NW. The north
# gate (main), east gate (side) and the south breach front (FarmApproachRoute) stay on long straight runs.
FENCE_POLY = [(56, 34), (152, 34), (152, 106), (136, 106), (136, 124), (44, 124), (44, 114), (32, 114), (32, 52), (56, 52)]
FENCE_NOTCHES = [(32, 34, 56, 52), (136, 106, 152, 124), (32, 114, 44, 124)]   # x0,y0,x1,y1 cut out of the box


def in_fence_notch(x, y, w=0.0, h=0.0):
    return any(x < nx1 and x + w > nx0 and y < ny1 and y + h > ny0 for nx0, ny0, nx1, ny1 in FENCE_NOTCHES)


def fence_poly(mb, pts, gates, seed, patched=True, role="fence"):
    """Closed rectilinear polygon of fence pieces. gates: list of (x, y) points lying on an edge."""
    ax, ay = CORNER_ARM
    r = np.random.default_rng(seed)
    corner_by_arms = {frozenset("EN"): "SW", frozenset("WN"): "SE", frozenset("WS"): "NE", frozenset("ES"): "NW"}

    def arm(a, b):
        return ("E" if b[0] > a[0] else "W") if b[1] == a[1] else ("N" if b[1] > a[1] else "S")

    n = len(pts)
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        horiz = a[1] == b[1]
        kind = "h" if horiz else "v"
        lo, hi = (min(a[0], b[0]), max(a[0], b[0])) if horiz else (min(a[1], b[1]), max(a[1], b[1]))
        fixed = a[1] if horiz else a[0]
        lo, hi = lo + (ax if horiz else ay), hi - (ax if horiz else ay)
        on_edge = [g for g in gates if (horiz and abs(g[1] - fixed) < 0.01 and lo < g[0] < hi)
                   or (not horiz and abs(g[0] - fixed) < 0.01 and lo < g[1] < hi)]
        cuts = sorted(((g[0] if horiz else g[1]) - GATE_HALF[kind], (g[0] if horiz else g[1]) + GATE_HALF[kind]) for g in on_edge)
        cur, runs_ = lo, []
        for g0, g1 in cuts:
            if g0 > cur:
                runs_.append((cur, g0))
            cur = max(cur, g1)
        if cur < hi:
            runs_.append((cur, hi))
        for ra, rb in runs_:
            k = max(1, round((rb - ra) / SEG_STEP[0 if horiz else 1]))
            st = (rb - ra) / k
            for j in range(k):
                c = ra + st * (j + 0.5)
                name = "fence_patched" if (patched and r.random() < 0.07) else "fence_segment"
                if horiz:
                    add_fence_piece(mb, name, c, fixed, FENCE_ROWS["seg_h"], "h", role)
                else:
                    add_fence_piece(mb, name, fixed, c, FENCE_ROWS["seg_v"], "v", role)
        for g in on_edge:
            if horiz:
                add_fence_piece(mb, "fence_gate_arch", g[0], fixed, FENCE_ROWS["gate_h"], "gate", role)
            else:
                add_fence_piece(mb, "fence_gate_arch", fixed, g[1], FENCE_ROWS["gate_v"], "gate", role)
    for i in range(n):
        p, v, q = pts[i - 1], pts[i], pts[(i + 1) % n]
        key = corner_by_arms[frozenset(arm(v, p) + arm(v, q))]
        add_fence_piece(mb, "fence_corner", v[0], v[1], FENCE_ROWS[key], "c", role)


def place_fence(mb):
    # west gate: the village road comes in from the west, so the farm has a gate on that side too
    fence_poly(mb, FENCE_POLY, [(MAIN_GATE[0], MAIN_GATE[1]), (SIDE_GATE[0], SIDE_GATE[1]), (WEST_GATE[0], WEST_GATE[1])], 777)
    mb.fence_polygon = [{"x": float(x), "y": float(y)} for x, y in FENCE_POLY]
    # (no fixed quarantine pen: GDD 3.7 "Khu Cách Ly không cố định trên bản đồ: người chơi chỉ định")


def add_fence_piece(mb, name, x, y, row, orient, role="fence"):
    if orient == "h":
        col = {"w": 3.9, "h": 0.4, "ox": 0.0, "oy": 0.0}
    elif orient == "v":
        col = {"w": 0.4, "h": 1.9, "ox": 0.0, "oy": 0.0}
    elif orient == "c":
        col = {"w": 0.6, "h": 0.6, "ox": 0.0, "oy": 0.0}
    else:
        col = None            # a gate opening stays passable; its posts are cosmetic
    if not mb.bld.has(name) and name not in mb.placeholders:
        mb.placeholders.append(name)
    return mb.add(f"Assets/Art/PixelMyth/Buildings/{name}/{name}_static.png", x, y, "actors", col,
                  role, None, sub=f"{name}_static_{row}", _meta={"name": name, "row": row})


# ---------------------------------------------------------------- service core
CORE = {}
# facing rows chosen by looking at every sheet: the row that shows the front/door/open side toward the camera.
FACING = {
    "approved_main_house": "down",       # porch, door, lit window
    "approved_research_lab": "down",     # porch with benches
    "approved_medical_tent": "down",     # open tent front with cots
    "approved_information_complex": "down", "approved_power_recovery": "down",
    "bldg_farmhouse": "down",            # porch + posts (the other rows are gable ends / blank back)
    "bldg_warehouse": "down",            # open double doors
    "bldg_construction_hall": "down",    # open workshop front with anvil
    "bldg_open_shelter": "down",         # open front with rail + hay, faces the pasture (south)
    "bldg_noxuyenvan_tower": "down",     # banner + stair side
    "bldg_observatory": "down",          # arched door, lit windows
    "prop_capacity_board": "down", "prop_care_post": "down", "prop_feeder": "down",
    "prop_water_basin": "down", "prop_mud_wallow": "down",
}
# collider footprint in tile units (w, h): ground footprint, not the roof. Screen depth is 0.5 of 3-D depth.
FOOT = {
    "approved_main_house": (8.4, 3.6), "approved_research_lab": (7.2, 3.2), "approved_medical_tent": (4.2, 2.0),
    "approved_information_complex": (18.0, 2.6), "approved_power_recovery": (4.0, 2.4),
    "bldg_farmhouse": (6.6, 3.0), "bldg_warehouse": (6.8, 3.0), "bldg_construction_hall": (4.8, 2.4),
    "bldg_open_shelter": (3.4, 1.8), "bldg_noxuyenvan_tower": (2.6, 1.6), "bldg_observatory": (3.2, 1.8),
    "prop_capacity_board": (1.0, 0.5), "prop_care_post": (0.7, 0.5), "prop_feeder": (1.8, 0.6),
    "prop_water_basin": (2.2, 0.8), "prop_mud_wallow": None,   # pigs wade in: no collider
    "argon_vdecor_rice_store": (3.4, 1.4), "argon_village_gieng_chung": (2.2, 1.2),
}
CORE_PLAN = [  # id, sprite, x, y, kind, interact
    ("main_house", "approved_main_house", 82.5, 114.0, "building", {"function": "VaoNhaScene", "id": "main_house"}),
    ("warehouse", "bldg_warehouse", 101.0, 114.0, "building", {"function": "MoKhoLuuTru", "id": "warehouse"}),
    ("construction_hall", "bldg_construction_hall", 113.0, 108.0, "building", {"function": "VaoNhaScene", "id": "construction_hall"}),
    ("herd_shelter", "bldg_open_shelter", 92.0, 97.5, "building", {"function": "VaoNhaScene", "id": "herd_shelter"}),
    ("research_lab", "approved_research_lab", 120.0, 116.0, "building", {"function": "VaoNhaScene", "id": "research_lab"}),
    ("medical_tent", "approved_medical_tent", 76.0, 99.0, "building", None),
    ("observatory", "bldg_observatory", 69.0, 110.0, "building", {"function": "VaoNhaScene", "id": "research_house"}),
    ("info_board", "prop_capacity_board", 87.0, 107.0, "prop", {"function": "DocBangTinLore", "id": "capacity_board"}),
    ("care_post", "prop_care_post", 100.0, 100.0, "prop", None),
    ("info_complex", "approved_information_complex", 56.0, 117.0, "building", {"function": "DocBangTinLore", "id": "info_station"}),
    # power room joins the service ring (it stood alone in the middle of the pasture)
    ("power_recovery", "approved_power_recovery", 124.0, 104.0, "building", None),
    # the only defense a new farm owns: Core builds one Nỏ Xuyên Vân at start (MobileGameController); the rest
    # are built by the player from blueprints in the defense strip (GDD 3.6 / 3.8)
    ("tower_w", "bldg_noxuyenvan_tower", 40.0, 96.0, "building", None),
    # feeding loop: feed store beside the warehouse, a well beside the west basin (bag -> trough, bucket -> basin)
    ("feed_silo", "argon_vdecor_rice_store", 107.5, 116.6, "prop", None),
    ("well", "argon_village_gieng_chung", 68.0, 92.0, "prop", None),
    ("feeder_1", "prop_feeder", 80.0, 90.0, "prop", None),
    ("feeder_2", "prop_feeder", 104.0, 90.0, "prop", None),
    ("feeder_3", "prop_feeder", 108.0, 82.0, "prop", None),
    ("basin_1", "prop_water_basin", 76.0, 84.0, "prop", None),
    ("basin_2", "prop_water_basin", 106.0, 96.0, "prop", None),
    ("wallow_1", "prop_mud_wallow", 66.0, 78.0, "prop", None),
    # pigs wallow in several spots, not one (GDD G576/G617: troughs, basins, wallows in many clusters)
    ("wallow_2", "prop_mud_wallow", 56.0, 63.0, "prop", None),
    ("wallow_3", "prop_mud_wallow", 90.0, 55.0, "prop", None),
    ("wallow_4", "prop_mud_wallow", 124.0, 92.0, "prop", None),
    ("basin_3", "prop_water_basin", 60.0, 52.0, "prop", None),
    ("feeder_4", "prop_feeder", 96.0, 64.0, "prop", None),
]
SHADOW_W = {"approved_main_house": 8.4, "approved_research_lab": 7.2, "approved_medical_tent": 4.2,
            "approved_power_recovery": 4.0, "bldg_farmhouse": 6.4, "bldg_warehouse": 6.4, "bldg_construction_hall": 4.8, "bldg_open_shelter": 3.4,
            "bldg_observatory": 3.4, "bldg_noxuyenvan_tower": 2.8}


def place_service_core(mb, plaza):
    for pid, spr, x, y, kind, inter in CORE_PLAN:
        if spr.startswith("approved_") and not mb.bld.has(spr):
            mb.missing_sheets.append(spr)
            continue
        row = FACING.get(spr, "down")
        foot = FOOT.get(spr)
        if spr in SHADOW_W:   # soft ground shadow: a row of ellipses at the base, pushed to the lower right (light = upper left)
            n = max(2, int(SHADOW_W[spr] / 1.1))
            for i in range(n):
                sx = x - SHADOW_W[spr] / 2 + (i + 0.5) * SHADOW_W[spr] / n + 0.5
                mb.add("Assets/Art/PixelMyth/Decor/shadow_ellipse_l.png", sx, y - 0.9, "ground")
        o = add_building(mb, spr, x, y, row, pid, kind, inter, foot)
        CORE[pid] = o
    mb.core_center = (92.0, 113.0)
    mb.keep_rects = [(86, 70, 98, 98)]


# ---------------------------------------------------------------- ruins
RUIN_SITES = [  # x, y, kind  (clearings are cut into the woods in farm())
    (14, 104, "court"), (16, 56, "arch"), (56, 15, "colonnade"), (118, 15, "court"), (168, 98, "arch"),
    (168, 50, "colonnade"), (112, 152, "colonnade"), (68, 152, "court"), (172, 20, "arch"), (12, 14, "court"),
    (40, 70, "mini"), (146, 90, "mini"),
]


def stamp_ruin(mb, x, y, kind, rng):
    pieces = {
        "arch": [("ruin_arch_fragment", 0, 0), ("ruin_wall", -3.4, 0.5), ("ruin_pillar", 2.5, 0.2), ("ruin_pillar", -6.0, -0.4),
                 ("ruin_cairn", 4.4, 1.2), ("boulder_m", -1.6, -1.8), ("boulder_l", 6.2, -0.6), ("shrub_dark", 1.0, -1.6),
                 ("ruin_cairn", -4.4, -1.5)],
        "colonnade": [("ruin_pillar", -4.5, 0), ("ruin_pillar", -1.5, 0.2), ("ruin_pillar", 1.5, -0.1), ("ruin_pillar", 4.5, 0.3),
                      ("ruin_wall", -3.0, 2.8), ("ruin_wall", 3.0, 3.0), ("ruin_cairn", 0.0, -1.7), ("boulder_m", 6.0, 1.0),
                      ("shrub_round", -6.2, 1.2)],
        "court": [("ruin_wall", -3, 3), ("ruin_wall", 3, 3.2), ("ruin_pillar", -5.8, 1.0), ("ruin_pillar", 5.9, 1.2),
                  ("ruin_cairn", 0.0, 0.2), ("ruin_cairn", -2.8, -2.0), ("boulder_l", 3.2, -1.6), ("boulder_m", -6.2, -1.2),
                  ("ruin_arch_fragment", 0.0, 5.4), ("shrub_dark", 6.6, -0.8)],
        "mini": [("ruin_pillar", 0, 0), ("ruin_cairn", 2.0, 0.6), ("boulder_m", -2.0, 0.4), ("shrub_round", 0.6, -1.5)],
    }[kind]
    for fam, dx, dy in pieces:
        jx, jy = rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3)
        place(mb, fam, x + dx + jx, y + dy + jy, check=True, allow_keep=False)
    # weeds, flame flowers and tufts crowd the stones
    for _ in range(14 if kind != "mini" else 6):
        a, r = rng.uniform(0, 2 * math.pi), rng.uniform(1.2, 7.0)
        px, py = x + math.cos(a) * r * 1.2, y + math.sin(a) * r * 0.7
        k = rng.random()
        if k < 0.35:
            flat(mb, "flame_flower", px, py, anim=True)
        elif k < 0.6:
            flat(mb, "grass_tuft_24x20", px, py, anim=True)
        elif k < 0.8:
            flat(mb, rng.choice(["flower_purple", "flower_white"]), px, py)
        else:
            flat(mb, "grass_tuft_16x16", px, py, anim=True)


# ---------------------------------------------------------------- decor passes
def decorate(mb, dfence, edge, forest, pond_par, lane, pond):
    rng = np.random.default_rng(4242)
    W, H = mb.W, mb.H
    tm = mb.tile_mat()
    fx0, fy0, fx1, fy1 = FENCE
    inside = lambda x, y: fx0 < x < fx1 and fy0 < y < fy1 and not in_fence_notch(x, y)
    ccx, ccy, rx, ry = pond_par
    dwater = chamfer(tm == "w")
    dpath = chamfer(mb.keep)

    def tmat(x, y):
        return tm[min(max(int(y), 0), H - 1), min(max(int(x), 0), W - 1)]

    def dfen(x, y):
        return float(rect_dist(np.float32(x), np.float32(y), FENCE))

    # ---- A. rim: two rows of big boulders form the low stone wall at the map edge
    portal_pts = [(122.0, 158.5), (1.5, 112.0)]
    def near_portal(x, y):
        return any((x - px) ** 2 + (y - py) ** 2 < 4.2 ** 2 for px, py in portal_pts)
    def rim_row(depth, fam, step, off):
        for side in range(4):
            n = int((W if side in (0, 1) else H) / step)
            for i in range(n):
                t = off + i * step + rng.uniform(-0.15, 0.15)
                if side == 0: x, y = t, depth
                elif side == 1: x, y = t, H - depth
                elif side == 2: x, y = depth, t
                else: x, y = W - depth, t
                if x < 1 or y < 1 or x > W - 1 or y > H - 1 or near_portal(x, y):
                    continue
                if tmat(x, y) in "wp":
                    continue
                place(mb, fam, x, y, shadow=False, check=False)
    rim_row(1.15, "boulder_l", 2.3, 0.8)
    rim_row(2.55, "boulder_m", 2.0, 1.3)
    # ---- B. ruin sites (focal points) -- before trees so the woods respect them
    for x, y, kind in RUIN_SITES:
        stamp_ruin(mb, x, y, kind, np.random.default_rng(int(x * 100 + y)))
    # ---- C. woods: ecological tree stands on forest terrain (broadleaf oak/amber West toward Bo Ria, tall pine/conifer East highlands)
    step = 2.3
    for gy in np.arange(1.6, H - 1.2, step):
        for gx in np.arange(1.6, W - 1.2, step):
            x = gx + rng.uniform(-0.6, 0.6) + (0.7 if int(gy / step) % 2 else 0)
            y = gy + rng.uniform(-0.5, 0.5)
            if not (0 < x < W and 0 < y < H) or tmat(x, y) != "f":
                continue
            if mb.keep[min(int(y), H - 1), min(int(x), W - 1)]:
                continue
            if rng.random() > 0.95:
                continue
            e = min(x, y, W - x, H - y)
            tall = rng.random() < (0.35 + 0.4 * min(e / 24, 1.0))
            if rng.random() < 0.10:
                place(mb, "shrub_dark" if x > 92 else "shrub_round", x, y, shadow=False)
            elif x < 72 and y > 45:
                # West / NW ecological zone (broadleaf woods leading toward Bo Ria village)
                fam = "oak_gold" if rng.random() < 0.58 else "tree_amber"
                place(mb, fam, x, y, shadow=rng.random() < 0.25)
            else:
                k = rng.random()
                if e > 5 and k < 0.32:
                    fam = "conifer_giant"
                elif k < 0.46:
                    fam = "conifer_mid"
                else:
                    fam = "conifer_tall" if tall else "conifer_small"
                place(mb, fam, x, y, shadow=rng.random() < 0.2)
    # ---- D. pond: shore boulders (north/east/west, leaving the south shore open for drinking), reeds, stepping stones
    ang = np.linspace(0, 2 * math.pi, 120, endpoint=False)
    for a in ang:
        r = 1.0 + 0.4 * rng.random()
        # find the water boundary along this ray
        for rr in np.arange(0.4, 1.6, 0.05):
            px, py = ccx + math.cos(a) * rx * rr, ccy + math.sin(a) * ry * rr
            if tmat(px, py) != "w":
                break
        px, py = px + math.cos(a) * 0.9, py + math.sin(a) * 0.7
        south = math.sin(a) < -0.55
        k = rng.random()
        if not south and k < 0.30:
            place(mb, rng.choice(["boulder_m", "boulder_s", "boulder_s"]), px, py, shadow=True)
        elif k < 0.75:
            deco(mb, "reeds", px, py - 0.2, check=False)   # reeds / cattails
        elif k < 0.9:
            flat(mb, "flame_flower", px, py, anim=True)
    for i, t in enumerate(np.linspace(0, 1, 6)):  # stepping stones across the NE bay
        sx, sy = ccx - 2.0 + t * 7.5, ccy + ry * 0.95 - t * 3.8 + math.sin(i) * 0.25
        flat(mb, "stepping_stone", sx, sy, check=False)
    # ---- E. clusters
    z_ring = lambda x, y: not inside(x, y)
    def cluster(x, y, kind):
        n = 0
        if kind == "bush":
            for _ in range(rng.integers(2, 4)):
                place(mb, rng.choice(["shrub_round", "shrub_dark"]), x + rng.normal(0, 1.3), y + rng.normal(0, 0.9))
            for _ in range(rng.integers(3, 7)):
                flat(mb, "flame_flower", x + rng.normal(0, 1.6), y + rng.normal(0, 1.1), anim=True)
            for _ in range(rng.integers(4, 9)):
                flat(mb, rng.choice(["flower_purple", "flower_white", "grass_tuft_24x20", "grass_tuft_16x16"]), x + rng.normal(0, 2.0), y + rng.normal(0, 1.4), anim=True)
        elif kind == "rocks":
            place(mb, rng.choice(["boulder_l", "boulder_m"]), x, y)
            for _ in range(rng.integers(1, 3)):
                place(mb, rng.choice(["boulder_m", "boulder_s", "ruin_cairn"]), x + rng.normal(0, 1.6), y + rng.normal(0, 1.0))
            place(mb, "shrub_dark", x + rng.normal(0, 1.5), y + rng.normal(0, 1.0))
            for _ in range(rng.integers(3, 8)):
                flat(mb, rng.choice(["flame_flower", "grass_tuft_24x20", "flower_white", "flower_purple"]), x + rng.normal(0, 1.8), y + rng.normal(0, 1.2), anim=True)
        elif kind == "grove":
            for _ in range(rng.integers(3, 6)):
                place(mb, rng.choice(["conifer_tall", "conifer_small", "conifer_small"]), x + rng.normal(0, 1.6), y + rng.normal(0, 1.2), shadow=True)
            for _ in range(rng.integers(2, 5)):
                flat(mb, rng.choice(["flame_flower", "flower_purple", "grass_tuft_16x16"]), x + rng.normal(0, 2.0), y + rng.normal(0, 1.4), anim=True)
        elif kind == "meadow":  # pasture / strip: only non-blocking things, buildable
            for _ in range(rng.integers(3, 6)):
                flat(mb, rng.choice(["grass_tuft_24x20", "grass_tuft_16x16", "grass_tuft_24x20"]), x + rng.normal(0, 1.5), y + rng.normal(0, 1.0), anim=True)
            for _ in range(rng.integers(0, 3)):
                flat(mb, rng.choice(["flower_purple", "flower_white", "flame_flower"]), x + rng.normal(0, 1.8), y + rng.normal(0, 1.2), anim=True)
            if rng.random() < 0.18:
                place(mb, "boulder_s", x + rng.normal(0, 1.5), y + rng.normal(0, 1.0))

    def zone_of(x, y):
        if not inside(x, y):
            return "ring"
        if 44 <= x <= 140 and 46 <= y <= 94:
            return "pasture"
        return "strip"
    # outer ring (non-forest): dense mixed clusters
    for x, y in poisson(rng, lambda x, y: z_ring(x, y) and tmat(x, y) in "m" and 3 < min(x, y, W - x, H - y), (0, 0, W, H), 5.6, 30000):
        if mb.keep[min(int(y), H - 1), min(int(x), W - 1)]:
            continue
        cluster(x, y, rng.choice(["bush", "bush", "rocks", "grove"]))
    # forest fringe: shrubs, young conifers and flame flowers hug the wood edge (density gradient into the woods)
    dforest = chamfer(tm == "f")
    for x, y in poisson(rng, lambda x, y: z_ring(x, y) and tmat(x, y) == "m" and 0.8 < dforest[min(int(y), H - 1), min(int(x), W - 1)] < 4.5, (0, 0, W, H), 2.5, 40000):
        k = rng.random()
        if k < 0.3:
            place(mb, rng.choice(["shrub_round", "shrub_dark"]), x, y)
        elif k < 0.42:
            place(mb, "conifer_small", x, y, shadow=True)
        elif k < 0.5:
            place(mb, str(rng.choice(["oak_gold", "tree_amber", "tree_amber"])), x, y, shadow=True)
        elif k < 0.55:
            place(mb, rng.choice(["boulder_m", "boulder_s"]), x, y)
        else:
            flat(mb, rng.choice(["flame_flower", "flower_purple", "grass_tuft_24x20", "flower_white"]), x, y, anim=True)
    # processing pad edge: boulders and shrubs mark the corners
    for bx, by in ((118, 48), (138, 48), (118, 64), (138, 64), (128, 48), (128, 64)):
        place(mb, rng.choice(["boulder_m", "shrub_dark"]), bx + rng.uniform(-0.5, 0.5), by + rng.uniform(-0.5, 0.5), check=False)
    # lane verges: tufts + flowers hugging the path
    for x, y in poisson(rng, lambda x, y: z_ring(x, y) and 0.9 < dpath[min(int(y), H - 1), min(int(x), W - 1)] < 2.6 and tmat(x, y) == "m", (0, 0, W, H), 2.3, 30000):
        flat(mb, rng.choice(["grass_tuft_24x20", "flame_flower", "flower_white", "flower_purple", "grass_tuft_16x16"]), x, y, anim=True)
    # band just outside / inside the fence
    for x, y in poisson(rng, lambda x, y: (0.8 < dfen(x, y) < 3.2 and z_ring(x, y)) or (inside(x, y) and dfen(x, y) < 0.0001 and False), (0, 0, W, H), 2.6, 12000):
        flat(mb, rng.choice(["flame_flower", "grass_tuft_24x20", "flower_purple"]), x, y, anim=True)
    # defense strip (buildable, calm): sparse non-blocking clusters; pasture: sparser still
    for x, y in poisson(rng, lambda x, y: inside(x, y) and zone_of(x, y) == "strip" and not (56 < x < 128 and y > 94), (fx0, fy0, fx1, fy1), 5.5, 20000):
        cluster(x, y, "meadow")
    for x, y in poisson(rng, lambda x, y: zone_of(x, y) == "pasture", (44, 46, 140, 94), 6.0, 20000):
        cluster(x, y, "meadow")
    # a few small ruin tufts at the very edge of the pasture are left out on purpose: pasture stays buildable
    # gentle drifts of flame flowers inside pasture along the herd lanes' flanks
    for x, y in poisson(rng, lambda x, y: zone_of(x, y) == "pasture" and tmat(x, y) == "m", (44, 46, 140, 94), 11, 8000):
        for _ in range(9):
            flat(mb, "flame_flower", x + rng.normal(0, 2.2), y + rng.normal(0, 1.4), anim=True)
    # core: tufts and flowers along building skirts
    for x, y in poisson(rng, lambda x, y: 56 < x < 128 and 94 < y < 123 and tmat(x, y) == "m", (56, 94, 128, 124), 3.2, 8000):
        flat(mb, rng.choice(["flower_white", "flower_purple", "grass_tuft_16x16", "flame_flower"]), x, y, anim=True)


def farm_richness(mb, pond):
    """Second composition pass for the farm: focal points, lived-in yards, framing, soft colour and dressing."""
    rng = np.random.default_rng(5151)
    W, H = mb.W, mb.H
    ccx, ccy, rx, ry = pond
    D = lambda fam, x, y, **k: deco(mb, fam, x, y, **k)
    # ---- service core: sun-disc inlay in the courtyard, avenue of lanterns, steles and banners at the gate
    D("sun_inlay", 92.0, 106.2, check=False, file="sun_inlay_a.png")
    for x, y, v in ((89.6, 122.6, "a"), (94.4, 122.6, "a"), (89.6, 117.2, "a"), (94.4, 117.2, "a"), (89.8, 112.2, "a"), (94.2, 112.2, "a")):
        D("lantern_post", x, y, check=False, file=f"lantern_post_{v}.png")
    D("sun_stele", 88.4, 119.9, check=False, file="sun_stele_a.png")
    D("statue_weathered", 95.6, 119.9, check=False, file="statue_weathered_a.png")
    D("banner", 87.6, 123.6, check=False, file="banner_red_f0.png")
    D("banner", 96.4, 123.6, check=False, file="banner_red_f0.png")
    for x in (88.2, 95.8):
        D("lantern_post", x, 127.4, check=False, file="lantern_post_b.png")
    for x, f in ((84.2, "oak_gold_a.png"), (100.2, "oak_gold_c.png")):
        D("oak_gold", x, 129.6, check=False, file=f)
    D("tree_amber", 80.4, 131.4, check=False, file="tree_amber_b.png")
    D("tree_amber", 104.0, 131.8, check=False, file="tree_amber_d.png")
    D("signpost", 97.4, 131.2, check=False, file="signpost_a.png")
    # warehouse yard: crates, barrels, sacks, a hay cart
    for fam, x, y, f in (("crate_stack", 96.2, 111.6, "crate_stack_a.png"), ("barrel", 105.5, 111.9, "barrel_a.png"),
                         ("barrel", 106.3, 111.3, "barrel_b.png"), ("crate", 106.0, 110.2, "crate_b.png"),
                         ("sack_pile", 108.0, 111.4, "sack_pile_a.png"), ("hay_cart", 109.4, 114.6, "hay_cart_a.png"),
                         ("woodpile", 109.0, 106.2, "woodpile_a.png"), ("barrel", 116.6, 105.4, "barrel_c.png"),
                         ("crate", 117.4, 106.0, "crate_a.png")):
        D(fam, x, y, check=False, file=f)
    # house fronts: flower boxes, a well, a bench
    for x, y, f in ((79.4, 110.8, "flower_box_a.png"), (85.6, 110.8, "flower_box_b.png"), (118.6, 112.6, "flower_box_c.png"),
                    (125.4, 112.6, "flower_box_a.png"), (72.2, 107.2, "flower_box_b.png"), (47.2, 109.2, "flower_box_c.png"),
                    (56.8, 109.2, "flower_box_a.png")):
        D("flower_box", x, y, check=False, file=f)
    D("well", 98.2, 104.4, check=False, file="well_a.png")
    D("bench", 86.4, 103.2, check=False, file="bench_a.png")
    D("sun_stele", 65.4, 105.6, check=False, file="sun_stele_b.png")
    D("banner", 47.4, 105.4, check=False, file="banner_blue_f0.png")
    D("banner", 56.6, 105.4, check=False, file="banner_blue_f0.png")
    # herd shelter: hay piles, round bales, feed troughs in a row (kept off the herd lane x 86..98)
    for fam, x, y, f in (("hay_pile", 83.6, 95.4, "hay_pile_a.png"), ("hay_pile", 101.6, 94.6, "hay_pile_b.png"),
                         ("hay_bale_round", 82.0, 93.4, "hay_bale_round_a.png"), ("hay_bale_round", 103.4, 92.6, "hay_bale_round_b.png"),
                         ("hay_bale_sq", 84.4, 92.8, "hay_bale_sq_a.png"), ("hay_bale_sq", 100.2, 91.6, "hay_bale_sq_c.png")):
        D(fam, x, y, check=True, file=f)
    for i, x in enumerate((70.0, 74.4, 78.8)):
        D("feed_trough", x, 88.6, check=True, file=f"feed_trough_{'abc'[i]}.png")
    for i, x in enumerate((108.0, 112.4, 116.8)):
        D("feed_trough", x, 86.6, check=True, file=f"feed_trough_{'abc'[(i + 1) % 3]}.png")
    # farmyard south of the fence: two decorative pens (non-blocking), troughs, hay, a scarecrow
    # Plus South breach battlefront dressing around (92, 31): non-blocking rubble/loose stone inside the breach,
    # and war-banners / torches / splintered stakes flanking outside the x=86..98 breach lane.
    pen(mb, 66, 28.0, 82, 33.0, gap=(33.0, 72, 76))
    for i, x in enumerate((68.4, 72.4, 76.4, 80.0)):
        D("feed_trough", x, 29.6, check=False, collider=False, file=f"feed_trough_{'abc'[i % 3]}.png")
    D("hay_bale_round", 67.8, 31.8, check=False, collider=False, file="hay_bale_round_a.png")
    D("hay_pile", 79.4, 32.0, check=False, collider=False, file="hay_pile_c.png")
    pen(mb, 102, 28.0, 116, 33.0, gap=(28.0, 106, 110))
    for x, f in ((104.4, "hay_bale_sq_a.png"), (111.6, "hay_bale_sq_b.png")):
        D("hay_bale_sq", x, 30.2, check=False, collider=False, file=f)
    D("hay_pile", 108.4, 31.6, check=False, collider=False, file="hay_pile_a.png")
    for x, y, f in ((88.2, 30.6, "rubble_a.png"), (95.8, 30.8, "rubble_b.png"),
                    (89.6, 27.6, "rubble_c.png"), (94.4, 27.4, "rubble_a.png")):
        D("rubble", x, y, check=False, collider=False, file=f)
    for x, y in ((91.2, 32.4), (93.0, 32.2), (90.4, 29.2), (93.8, 29.4)):
        flat(mb, "stepping_stone", x, y, allow_keep=True, check=False)
    for x, y in ((83.8, 33.6), (100.2, 33.6)):
        D("banner", x, y, check=False, collider=False, file="banner_red_f0.png")
    for x, y in ((83.8, 27.2), (100.2, 27.2)):
        D("torch_post", x, y, check=False, collider=False)
    D("hay_cart", 122.0, 29.6, check=False, collider=False, file="hay_cart_b.png")
    D("woodpile", 60.6, 30.0, check=False, collider=False, file="woodpile_b.png")
    D("signpost", 84.2, 24.2, check=False, collider=False, file="signpost_b.png")
    # pasture scarecrows (non-blocking: the pasture stays buildable)
    for x, y, f in ((52.0, 52.0, "scarecrow_b.png"), (131.0, 60.0, "scarecrow_a.png")):
        D("scarecrow", x, y, check=False, collider=False, file=f)
    # ---- pond: lily pads and a little shore life
    tm = mb.tile_mat()
    for _ in range(26):
        a = rng.uniform(0, 6.283)
        rr = np.sqrt(rng.random()) * 0.78
        px, py = ccx + math.cos(a) * rx * rr, ccy + math.sin(a) * ry * rr
        if tm[min(int(py), H - 1), min(int(px), W - 1)] == "w":
            D("lilypad", px, py, check=False)
    # depth: dithered dark bowls under the middle, light ripple arcs and sun glints on top (AE-0002: not a flat sheet)
    D("water_deep", ccx - 0.6, ccy + 0.2, check=False, file="water_deep_a.png")
    D("water_deep", ccx + 3.4, ccy - 1.0, check=False, file="water_deep_b.png")
    D("water_deep", ccx - 4.6, ccy + 1.3, check=False, file="water_deep_c.png")
    for _ in range(14):
        a = rng.uniform(0, 6.283)
        rr = np.sqrt(rng.random()) * 0.8
        px, py = ccx + math.cos(a) * rx * rr, ccy + math.sin(a) * ry * rr
        if tm[min(int(py), H - 1), min(int(px), W - 1)] == "w":
            D("water_ripple", px, py, check=False, file=f"water_ripple_{'abc'[int(rng.integers(3))]}.png")
    D("log_fallen", ccx + rx + 1.6, ccy - 3.6, check=True)
    D("stump", ccx - rx - 1.5, ccy - 1.2, check=True)
    # ---- ruin focal points get a weathered statue / sun-stele
    for x, y, kind in RUIN_SITES:
        if kind in ("court", "arch"):
            D("sun_stele" if kind == "arch" else "statue_weathered", x + 0.5, y - 3.0, check=True)
    # ---- dressing: tall golden grass, wildflower patches, logs and stumps, hay near the woods
    tm = mb.tile_mat()
    dforest = chamfer(tm == "f")
    ok_m = lambda x, y: tm[min(int(y), H - 1), min(int(x), W - 1)] == "m"
    for x, y in poisson(rng, lambda x, y: ok_m(x, y) and 2 < min(x, y, W - x, H - y), (0, 0, W, H), 8.0, 26000):
        k = rng.random()
        if k < 0.42:
            D("grass_tall", x, y)
        elif k < 0.62:
            D("grass_wide", x, y)
        else:
            D("wildflowers", x, y)
    for x, y in poisson(rng, lambda x, y: ok_m(x, y) and 1.0 < dforest[min(int(y), H - 1), min(int(x), W - 1)] < 7
                        and not (FENCE[0] - 1 < x < FENCE[2] + 1 and FENCE[1] - 1 < y < FENCE[3] + 1), (0, 0, W, H), 9.0, 12000):
        D(str(rng.choice(["log_fallen", "stump", "stump", "woodpile", "hay_bale_round"])), x, y)
    edge_dress(mb, rng)
    # flower drifts in the lush patches (colour accents), forest floor life (ferns, tufts, fallen logs)
    cm_ = mb.c
    lush_t = lambda x, y: cm_[min(int(y), H - 1), min(int(x), W - 1)] == "g"
    for x, y in poisson(rng, lambda x, y: lush_t(x, y) and 2 < min(x, y, W - x, H - y), (0, 0, W, H), 10.0, 12000):
        for _ in range(4):
            D("wildflowers", x + rng.normal(0, 1.6), y + rng.normal(0, 1.0))
        for _ in range(2):
            flat(mb, "flame_flower", x + rng.normal(0, 1.6), y + rng.normal(0, 1.0), anim=True)
    ok_f = lambda x, y: tm[min(int(y), H - 1), min(int(x), W - 1)] == "f"
    for x, y in poisson(rng, lambda x, y: ok_f(x, y), (0, 0, W, H), 5.2, 9000):
        k = rng.random()
        D("fern" if k < 0.6 else ("log_fallen" if k < 0.68 else "grass_tall"), x, y)


def pasture_meadow(mb, pond):
    """Living gold field inside the fence (user 31 Oct: 'map trống trơn'): dense drifts of tall grass and wildflowers
    following a noise field, a scatter of tufts between them, shade-tree groves where pigs rest, moss boulders, hay and
    sunken fragments of the old myth (fallen pillars, weathered statues) half-swallowed by the meadow. Everything in
    the pasture is non-blocking dressing except tree trunks and boulders, so pigs and building placement still work."""
    rng = np.random.default_rng(7373)
    W, H = mb.W, mb.H
    fx0, fy0, fx1, fy1 = FENCE
    tm = mb.tile_mat()
    keep = mb.keep
    drift = fbm(W + 1, H + 1, 9, 7401, 3)
    flowers = fbm(W + 1, H + 1, 6, 7402, 2)
    ccx, ccy, prx, pry = pond

    def inside(x, y, m=1.5):
        return fx0 + m < x < fx1 - m and fy0 + m < y < fy1 - m and not in_fence_notch(x - m, y - m, 2 * m, 2 * m)

    lanes = getattr(mb, "herd_lanes", [])

    def in_lane(x, y, m=4.0):   # herd lanes stay >= 6 m wide (GDD G582/G617): no trunks or rocks in or near them
        return any(a - m < x < c + m and b - m < y < d + m for a, b, c, d in lanes)

    def meadow(x, y):
        ix, iy = min(int(x), W - 1), min(int(y), H - 1)
        return tm[iy, ix] in ("m",) and not keep[iy, ix] and not mb.blocked[iy, ix] and mb.c[iy, ix] != "w"

    near_pond = lambda x, y: ((x - ccx) / (prx + 2.5)) ** 2 + ((y - ccy) / (pry + 2.0)) ** 2 < 1
    n = {"drift": 0, "tuft": 0, "tree": 0, "rock": 0, "ruin": 0, "hay": 0}
    # 1. dense drifts (noise > 0.55): tall grass + wide grass, wildflower carpets where the second noise is high
    for x, y in poisson(rng, lambda x, y: inside(x, y) and meadow(x, y) and not near_pond(x, y)
                        and drift[min(int(y), H), min(int(x), W)] > 0.58, (fx0, fy0, fx1, fy1), 0.85, 90000):
        f = flowers[min(int(y), H), min(int(x), W)]
        fam = "wildflowers" if f > 0.6 and rng.random() < 0.7 else ("grass_tall" if rng.random() < 0.62 else "grass_wide")
        if deco(mb, fam, x, y, check=False, collider=False):
            n["drift"] += 1
    # 2. sparse tufts everywhere else so no stretch reads as bare paint
    for x, y in poisson(rng, lambda x, y: inside(x, y) and meadow(x, y) and drift[min(int(y), H), min(int(x), W)] <= 0.58,
                        (fx0, fy0, fx1, fy1), 3.6, 40000):
        if rng.random() < 0.75:
            flat(mb, "grass_tuft_24x20" if rng.random() < 0.5 else "grass_tuft_16x16", x, y, anim=True, check=False)
        else:
            deco(mb, "wildflowers", x, y, check=False, collider=False)
        n["tuft"] += 1
    # 3. shade groves: 2-4 old trees with a ring of tall grass and a hay bale or two (pigs rest in the shade)
    groves = [(50.0, 86.0), (70.0, 50.0), (112.0, 70.0), (134.0, 52.0)]
    for gx, gy in groves:
        for k in range(int(rng.integers(2, 5))):
            a, r = rng.uniform(0, 6.283), rng.uniform(0.0, 3.2)
            x, y = gx + math.cos(a) * r, gy + math.sin(a) * r * 0.7
            if inside(x, y, 3) and meadow(x, y) and not in_lane(x, y) and deco(mb, "oak_gold" if rng.random() < 0.6 else "tree_amber", x, y, check=True):
                n["tree"] += 1
        for k in range(2):
            x, y = gx + rng.normal(0, 2.6), gy - 2.5 + rng.normal(0, 0.8)
            if inside(x, y, 2) and meadow(x, y) and deco(mb, "hay_bale_round", x, y, check=True, collider=False):
                n["hay"] += 1
    # 4. moss boulder clusters and myth fragments sunk in the field
    for x, y in poisson(rng, lambda x, y: inside(x, y, 3) and meadow(x, y) and not near_pond(x, y) and not in_lane(x, y),
                        (fx0, fy0, fx1, fy1), 16.0, 4000):
        if rng.random() < 0.45:
            for k in range(int(rng.integers(1, 4))):
                if deco(mb, str(rng.choice(["boulder_s", "boulder_m", "boulder_m", "boulder_l"])), x + rng.normal(0, 1.0),
                        y + rng.normal(0, 0.6), check=True):
                    n["rock"] += 1
    for x, y, parts in ((62.0, 70.0, ("ruin_pillar", "rubble", "statue_weathered")),
                        (118.0, 58.0, ("ruin_pillar", "ruin_cairn", "rubble")),
                        (84.0, 76.0, ("sun_stele", "rubble", "boulder_m"))):
        for i, fam in enumerate(parts):
            if deco(mb, fam, x + (i - 1) * 1.8 + rng.normal(0, 0.3), y + rng.normal(0, 0.4), check=True):
                n["ruin"] += 1
    # Argon wave-2 accents: big wildflower / tall-sward cushions (non-blocking), story props in the service ring
    for x, y in poisson(rng, lambda x, y: inside(x, y, 3) and meadow(x, y) and not near_pond(x, y) and not in_lane(x, y, 1.0),
                        (fx0, fy0, fx1, fy1), 9.0, 6000):
        name = "argon_w2_pasture_wildflower_clump" if rng.random() < 0.5 else "argon_w2_pasture_tall_sward_clump"
        mb.add(f"Assets/Art/PixelMyth/Buildings/{name}/{name}_static.png", x, y, "actors", None, "decor:meadow_clump", None,
               sub=f"{name}_static_down", _meta={"name": name, "row": "down"})
        n["clump"] = n.get("clump", 0) + 1
    for name, x, y, foot, rid in (("argon_w2_ferment_kiln", 72.6, 113.6, (2.6, 1.6), "ferment_kiln"),      # S196/S383
                                  ):   # processing yard removed: Khu Xử Lý is player-placed (GDD 3.7)
        if mb.free(x, y, 1.2, allow_keep=True):
            add_building(mb, name, x, y, "down", rid, "prop", foot=foot)
            mb.block_rect(x - foot[0] / 2, y - 0.2, x + foot[0] / 2, y + foot[1])
            n[rid] = 1
    print("pasture_meadow:", n)


def sample_path(pts, step=2.0):
    """Points every `step` m along a polyline with unit tangents."""
    out = []
    carry = 0.0
    for a, b in zip(pts[:-1], pts[1:]):
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dy)
        if L < 1e-6:
            continue
        tx, ty = dx / L, dy / L
        t = carry
        while t < L:
            out.append((a[0] + tx * t, a[1] + ty * t, tx, ty))
            t += step
        carry = t - L
    return out


def post_runs(mb, rng, pts, offset, step=2.0, run=(4, 8), gap=(4, 9), rails=True, tm=None):
    """Rows of small non-blocking fence posts (with rails on near-horizontal runs) beside a road, in runs with gaps."""
    H, W = mb.H, mb.W
    samples = sample_path(pts, step)
    n = 0
    stretches, cur = [], []
    for x, y, tx, ty in samples:
        px, py = x - ty * offset, y + tx * offset
        ok = (1.5 < px < W - 1.5 and 1.5 < py < H - 1.5)
        if ok:
            t = tm[int(py), int(px)] if tm is not None else "m"
            ok = t == "m" and not mb.blocked[int(py), int(px)] and not mb.keep[int(py), int(px)] and mb.c[int(py), int(px)] != "f"
        if ok:
            cur.append((px, py, tx, ty))
        elif cur:
            stretches.append(cur)
            cur = []
    if cur:
        stretches.append(cur)
    for st in stretches:
        i = int(rng.integers(0, 2))
        while i < len(st):
            rl = int(rng.integers(run[0], run[1] + 1))
            seg = st[i:i + rl]
            if len(seg) >= 3:
                prev = None
                for px, py, tx, ty in seg:
                    mb.put_decor(mb.d_sprite("fence_wood_post"), px, py, "actors", None)
                    n += 1
                    if rails and prev is not None and abs(ty) < 0.45 * abs(tx) + 0.2:
                        mb.put_decor(mb.d_sprite("fence_wood_rail_h"), (px + prev[0]) / 2, (py + prev[1]) / 2, "actors", None)
                        n += 1
                    prev = (px, py)
            i += rl + max(1, int(rng.integers(gap[0], gap[1] + 1)) // int(step))
    return n


def farm_rhythm(mb, pond):
    """Visual rhythm for the empty gold: fence-post rows along roads, tall-grass and wildflower clusters, loose rocks,
    occasional trees. The pasture centre (pigs, building) stays open: only non-blocking dressing goes there."""
    rng = np.random.default_rng(6262)
    W, H = mb.W, mb.H
    tm = mb.tile_mat()
    keep = mb.keep
    D = lambda fam, x, y, **k: deco(mb, fam, x, y, **k)
    inpen = lambda x, y: 58 < x < 126 and 48 < y < 94                      # open pasture centre
    ok_m = lambda x, y: 1 < x < W - 1 and 1 < y < H - 1 and tm[int(y), int(x)] == "m" and mb.c[int(y), int(x)] != "f"
    ftw = FENCE
    # ---- 1. fence-post rows along the roads and the outer lane (matching North MarketTown & West Hub_Village roads)
    roads = [([(92, 124.0), (92, 133.0)], 4.6),
             (catmull([(92, 133.0), (104, 141), (116, 151), (122, 158.0)]), 3.4),
             (catmull([(92, 133.0), (78, 137), (52, 136.5), (30, 130), (14, 116), (3.0, 112)]), 3.4)]
    for pts, off in roads:
        for side in (1, -1):
            post_runs(mb, rng, pts, side * off, tm=tm)
    # outer lane ring: posts on the far side of the lane
    x0, y0, x1, y1 = ftw[0] - 9, ftw[1] - 9, ftw[2] + 9, ftw[3] + 9
    ring = [(x0 + 4, y0), (x1 - 4, y0), (x1, y0 + 4), (x1, y1 - 4), (x1 - 4, y1), (x0 + 4, y1), (x0, y1 - 4), (x0, y0 + 4), (x0 + 4, y0)]
    for (a, b, off) in ((ring[0], ring[1], -2.9), (ring[1], ring[2], -2.9), (ring[2], ring[3], -2.9), (ring[4], ring[5], 2.9),
                        (ring[6], ring[7], -2.9), (ring[0], ring[1], 2.9), (ring[4], ring[5], -2.9)):
        post_runs(mb, rng, [a, b], off, step=2.0, run=(4, 7), gap=(8, 18), tm=tm)
    # inside the fence: posts along the herd spine and the service road
    post_runs(mb, rng, [(95, 98), (92, 88), (95, 78), (100, 72)], 2.0, run=(3, 5), gap=(6, 10), rails=False, tm=tm)
    post_runs(mb, rng, [(95, 98), (92, 88), (95, 78), (100, 72)], -2.0, run=(3, 5), gap=(6, 10), rails=False, tm=tm)
    post_runs(mb, rng, [(130, 66), (112, 70), (104, 80), (100, 92)], 2.4, run=(3, 6), gap=(6, 10), rails=False, tm=tm)
    # ---- 2. tall grass + wildflower clusters (non-blocking) on open meadow, lighter in the pasture centre
    def grass_cluster(x, y):
        for _ in range(int(rng.integers(2, 5))):
            D(str(rng.choice(["grass_tall", "grass_tall", "grass_wide"])), x + rng.normal(0, 0.9), y + rng.normal(0, 0.6), check=True)
        for _ in range(int(rng.integers(1, 4))):
            D("wildflowers", x + rng.normal(0, 1.1), y + rng.normal(0, 0.8), check=True)
    def flower_cluster(x, y):
        for _ in range(int(rng.integers(3, 6))):
            flat(mb, str(rng.choice(["flame_flower", "flower_purple", "flower_white", "flame_flower"])), x + rng.normal(0, 1.1), y + rng.normal(0, 0.7), anim=True)
    for x, y in poisson(rng, lambda x, y: ok_m(x, y) and not inpen(x, y) and 2 < min(x, y, W - x, H - y), (0, 0, W, H), 6.8, 20000):
        (grass_cluster if rng.random() < 0.55 else flower_cluster)(x, y)
    for x, y in poisson(rng, lambda x, y: ok_m(x, y) and inpen(x, y), (58, 48, 126, 94), 10.0, 6000):
        (grass_cluster if rng.random() < 0.5 else flower_cluster)(x, y)
    # service-core meadow (former plaza): small grass / flower clusters so the courtyard reads as cosy meadow, not bare gold
    for x, y in poisson(rng, lambda x, y: 54 < x < 130 and 92 < y < 123 and ok_m(x, y), (54, 92, 130, 123), 3.4, 12000):
        (grass_cluster if rng.random() < 0.5 else flower_cluster)(x, y)
    # pasture margins inside the fence (non-blocking): flower drifts, tufts and loose pebbles
    for x, y in poisson(rng, lambda x, y: ftw[0] < x < ftw[2] and ftw[1] < y < ftw[3] and not inpen(x, y) and ok_m(x, y) and not (54 < x < 130 and y > 92), (ftw[0], ftw[1], ftw[2], ftw[3]), 6.5, 12000):
        (grass_cluster if rng.random() < 0.5 else flower_cluster)(x, y)
        if rng.random() < 0.3:
            flat(mb, "stepping_stone", x + rng.normal(0, 1.0), y + rng.normal(0, 0.7))
    # ---- 3. loose rocks and occasional trees in the empty gold (never in the pasture centre)
    dpath = chamfer(keep)
    for x, y in poisson(rng, lambda x, y: ok_m(x, y) and not (ftw[0] - 2 < x < ftw[2] + 2 and ftw[1] - 2 < y < ftw[3] + 2) and 1.5 < dpath[int(y), int(x)] < 14 and 3 < min(x, y, W - x, H - y), (0, 0, W, H), 13.0, 12000):
        k = rng.random()
        if k < 0.45:
            place(mb, "boulder_s", x, y)
            flat(mb, "stepping_stone", x + 1.2, y - 0.4)
        elif k < 0.7:
            place(mb, "boulder_m", x, y, shadow=True)
        else:
            D(str(rng.choice(["oak_gold", "tree_amber", "tree_amber"])), x, y)


def choose_gate_zones(mb, pond):
    """8x8 m rectangles inside the pasture, >= 6 m from every building collider, clear of herd lanes, pond, props."""
    rng = np.random.default_rng(99)
    ccx, ccy, rx, ry = pond
    bld = []
    for o in mb.objects:
        if o["role"].startswith(("building:", "prop:")) and o["collider"]:
            c = o["collider"]
            bld.append((o["x"] + c["ox"] - c["w"] / 2, o["y"] + c["oy"] - c["h"] / 2, o["x"] + c["ox"] + c["w"] / 2, o["y"] + c["oy"] + c["h"] / 2))
    lanes = list(mb.herd_lanes) + list(getattr(mb, "keep_rects", []))
    def ok(x0, y0):
        r = (x0, y0, x0 + 8, y0 + 8)
        if not (46 <= x0 and x0 + 8 <= 138 and 48 <= y0 and y0 + 8 <= 92):
            return False
        for b in bld:
            dx = max(b[0] - r[2], r[0] - b[2], 0)
            dy = max(b[1] - r[3], r[1] - b[3], 0)
            if math.hypot(dx, dy) < 6:
                return False
        for l in lanes:
            if not (r[2] < l[0] or r[0] > l[2] or r[3] < l[1] or r[1] > l[3]):
                return False
        # pond clearance (rect vs ellipse, generous)
        px = min(max(ccx, r[0]), r[2]); py = min(max(ccy, r[1]), r[3])
        if ((px - ccx) / (rx + 6)) ** 2 + ((py - ccy) / (ry + 6)) ** 2 < 1:
            return False
        if mb.blocked[int(y0):int(y0) + 8, int(x0):int(x0) + 8].any():
            return False
        # quarantine / processing rectangles
        for q in ((118, 72, 138, 88), (118, 48, 138, 64)):
            if not (r[2] < q[0] - 2 or r[0] > q[2] + 2 or r[3] < q[1] - 2 or r[1] > q[3] + 2):
                return False
        return True
    cands = [(x, y) for x in np.arange(46, 130, 2.0) for y in np.arange(48, 85, 2.0) if ok(x, y)]
    rng.shuffle(cands)
    chosen = []
    for x, y in cands:
        if all(math.hypot(x - a, y - b) >= 12 for a, b in chosen):
            chosen.append((x, y))
        if len(chosen) >= 8:
            break
    mb.gate_event_zones = [[float(x), float(y), 8.0, 8.0] for x, y in sorted(chosen)]


# --------------------------------------------------------------------------- output
def write_outputs(mb, out_dir, do_render=True):
    out_dir.mkdir(parents=True, exist_ok=True)
    objs = []
    for o in mb.objects:
        o2 = {k: v for k, v in o.items() if k != "_meta"}
        objs.append(o2)
    data = {
        "map": mb.name, "location_id": mb.location_id, "scene_name": mb.scene, "size": [mb.W, mb.H], "tile_px": T,
        "terrain": {"legend": LEGEND, "corner_rows": mb.corner_rows()},
        "objects": objs, "portals": mb.portals, "gate_event_zones": mb.gate_event_zones, "spawn": mb.spawn,
        "zones": mb.zones, "enemy_entries": mb.enemy_entries, "fence_polygon": getattr(mb, "fence_polygon", None),
    }
    tag = mb.name.lower()
    (out_dir / f"{tag}_layout.json").write_text(json.dumps(data, separators=(",", ":")), encoding="utf-8")
    if do_render:
        img = render(mb)
        img.save(out_dir / f"{tag}_preview.png", optimize=False)
        cx, cy = mb.core_center
        px, py = cx * T, (mb.H - cy) * T
        x0 = int(max(0, min(img.width - 640, px - 320)))
        y0 = int(max(0, min(img.height - 360, py - 180)))
        img.crop((x0, y0, x0 + 640, y0 + 360)).resize((1920, 1080), Image.NEAREST).save(out_dir / f"{tag}_preview_crop_x3.png")
        zone_diagram(mb, img, out_dir / f"{tag}_zones.png")
        return img
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("map", choices=sorted(LAYOUTS))
    ap.add_argument("--out-dir", default=str(OUT_DIR))
    ap.add_argument("--no-render", action="store_true")
    a = ap.parse_args()
    mb = LAYOUTS[a.map]()
    write_outputs(mb, Path(a.out_dir), not a.no_render)
    rep, _, _ = validate(mb)
    print("missing approved sheets:", mb.missing_sheets)
    print("objects:", len(mb.objects), "placeholders:", sorted(set(mb.placeholders)))
    print("validation:", json.dumps(rep))


if __name__ == "__main__":
    main()
