"""Procedural, deterministic decor sprites for Pig Tycoon (32 px = 1 m, ADR-0005).

  python tools/pixelart/gen_decor.py            # writes Assets/Art/PixelMyth/Decor/*.png + decor.json + preview

Method: shapes are built as small height fields (ellipsoid lumps, cylinders, extruded masks), lit from the
upper left, quantised to 2-6 flat shades, then given a selective outline (darkest neighbouring colour, never
black).  Moss, gold lichen, cracks and gold leaf are applied as decals.  No anti-aliasing, no image copying.
Numpy + PIL only.
"""
import argparse
import json
import zlib
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT_DEFAULT = ROOT / "Assets" / "Art" / "PixelMyth" / "Decor"
TERRAIN = ROOT / "Assets" / "Art" / "PixelMyth" / "Terrain"


# ---------------------------------------------------------------- colour helpers
def hx(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def mix(a, b, t):
    return tuple(int(round(a[i] * (1 - t) + b[i] * t)) for i in range(3))


def dk(c, k=0.5):
    """Darker, warm-shifted ink derived from a neighbouring colour (never pure black)."""
    return mix(tuple(int(v * k) for v in c), (44, 26, 14), 0.22)


def lum(c):
    return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


def ramp(*hs):
    return [hx(h) for h in hs]


STONE = ramp("#3b332b", "#5f5446", "#82755f", "#a3967b", "#c8bb9c", "#e8dcb4")
MOSS = ramp("#2c3716", "#47561f", "#6b7a2c", "#93973f")
GOLD_L = ramp("#a56f1c", "#d9a53a", "#f0c95a", "#fbe596")
WOOD = ramp("#3d2618", "#5e3d22", "#7f5a32", "#a47a45", "#c9a066")
CON = ramp("#0d140a", "#16200e", "#212c13", "#2f3d1a", "#415024", "#586a2e")
CON_RIM = ramp("#8e8a32", "#c0a73e", "#e6c862")
SHRUB_O = ramp("#252a10", "#3a441c", "#566022", "#7c8430", "#b09e3c", "#e6cc64")
SHRUB_D = ramp("#141b0f", "#202a14", "#2f3c1b", "#445426", "#6b7430", "#b49c3c")
GRASS_F = ramp("#6f4c1c", "#8f6420", "#c58a2c", "#e4b044", "#f8d474", "#fff0b4")
GRASS_B = ramp("#5f4018", "#7a5520", "#a5782a", "#c08a30")
GRASS_O = ramp("#3f4a1c", "#58632e", "#7d8a3a", "#a7a04a")
FLAME = ramp("#5a1e0e", "#9a3a16", "#d0602a", "#ee8f30", "#f8bf48", "#fde48a", "#fff7c8")
LEAF = ramp("#2c3716", "#47561f", "#6b7a2c", "#93973f", "#c0b04e")

BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0
LIGHT = np.array([-0.55, -0.62, 0.56])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


# ---------------------------------------------------------------- canvas
class Spr:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.a = np.zeros((h, w, 4), np.uint8)

    def put(self, x, y, c, a=255):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.a[y, x] = (c[0], c[1], c[2], a)

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h and self.a[y, x, 3]:
            return tuple(int(v) for v in self.a[y, x, :3])
        return None

    def mask(self):
        return self.a[..., 3] > 0

    def blit(self, o, x=0, y=0):
        for yy in range(o.h):
            for xx in range(o.w):
                if o.a[yy, xx, 3]:
                    self.put(x + xx, y + yy, o.a[yy, xx, :3], int(o.a[yy, xx, 3]))

    def image(self):
        return Image.fromarray(self.a, "RGBA")


def vnoise(w, h, cell, rng):
    gw, gh = w // cell + 3, h // cell + 3
    lat = rng.random((gh, gw))
    ys, xs = np.arange(h) / cell, np.arange(w) / cell
    y0, x0 = ys.astype(int), xs.astype(int)
    fy, fx = ys - y0, xs - x0
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    a, b = lat[np.ix_(y0, x0)], lat[np.ix_(y0, x0 + 1)]
    c, d = lat[np.ix_(y0 + 1, x0)], lat[np.ix_(y0 + 1, x0 + 1)]
    FX, FY = fx[None, :], fy[:, None]
    return (a * (1 - FX) + b * FX) * (1 - FY) + (c * (1 - FX) + d * FX) * FY


def shade(z, n, dither=0.0, gain=1.0, lo=-0.12, hi=1.0, bias=0.0):
    """Quantised Lambert from a height field. Returns (shade index, intensity, normal-z)."""
    h, w = z.shape
    zc = np.maximum(z, 0)
    gy, gx = np.gradient(zc)
    nx, ny, nz = -gx * gain, -gy * gain, np.ones_like(zc)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    I = (nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]) / ln
    t = (I - lo) / (hi - lo) + bias
    if dither:
        ys, xs = np.mgrid[0:h, 0:w]
        t = t + (BAYER[ys % 4, xs % 4] - 0.5) * dither
    return np.clip(np.floor(t * n), 0, n - 1).astype(int), I, nz / ln


def outline_inner(L, k_dark=0.42, k_light=0.66):
    m = L.mask()
    pad = np.pad(m, 1, constant_values=False)
    up, dn = ~pad[:-2, 1:-1], ~pad[2:, 1:-1]
    lf, rt = ~pad[1:-1, :-2], ~pad[1:-1, 2:]
    edge = m & (up | dn | lf | rt)
    light = (up | lf) & ~(dn | rt)
    src = L.a.copy()
    for y, x in zip(*np.nonzero(edge)):
        L.a[y, x, :3] = dk(tuple(int(v) for v in src[y, x, :3]), k_light if light[y, x] else k_dark)


def outline_outer(L, k=0.5):
    m = L.mask()
    src = L.a.copy()
    for y in range(L.h):
        for x in range(L.w):
            if m[y, x]:
                continue
            cs = []
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < L.h and 0 <= xx < L.w and src[yy, xx, 3]:
                    cs.append(tuple(int(v) for v in src[yy, xx, :3]))
            if cs:
                L.put(x, y, dk(min(cs, key=lum), k))


def ground_blades(L, rng, n, x0, x1, y, hmax=3):
    """Golden grass blades overlapping the foot of a prop so it sits in the meadow."""
    for _ in range(n):
        x = int(rng.integers(x0, x1 + 1))
        hgt = int(rng.integers(1, hmax + 1))
        lean = int(rng.choice([-1, 0, 0, 1]))
        for k in range(hgt):
            L.put(x + (lean if k == hgt - 1 and hgt > 1 else 0), y - k, GRASS_F[min(5, 2 + k + (0 if k else 0))] if k else GRASS_F[1])
    return L


# ---------------------------------------------------------------- stone primitives
def stone_pix(idx, ramp_, moss_i=None, gold=True):
    c = ramp_[idx]
    if gold and idx >= len(ramp_) - 2:
        c = mix(c, (255, 232, 150), 0.2)
    return c


def rock(L, cx, cy, rx, ry, rz, rng, ramp_=STONE, p=2.5, wob=0.09, moss=0.5, lichen=0.025,
         cracks=1, dither=0.10, gain=1.0, flatten=0.0):
    h, w = L.h, L.w
    ys, xs = np.mgrid[0:h, 0:w]
    nx, ny = (xs + 0.5 - cx) / rx, (ys + 0.5 - cy) / ry
    ang = np.arctan2(ny, nx)
    ph = rng.random(4) * 6.283
    rm = 1 + wob * np.sin(2 * ang + ph[0]) + wob * .7 * np.sin(3 * ang + ph[1]) + wob * .5 * np.sin(5 * ang + ph[2])
    d = ((np.abs(nx) ** p + np.abs(ny) ** p) ** (1 / p)) / rm
    inside = (d < 1.0)
    z = rz * np.clip(1 - d ** 3, 0, 1) ** (0.55 - flatten)
    z = z + (vnoise(w, h, 3, rng) - 0.5) * rz * 0.45 * (z > 0)
    z = np.where(inside, z, 0)
    n = len(ramp_)
    idx, I, nzz = shade(z, n, dither, gain)
    mn = vnoise(w, h, 3, rng)
    mn2 = vnoise(w, h, 2, rng)
    topness = np.clip(-ny, 0, 1)
    mossm = inside & (mn * 0.65 + 0.55 * topness + 0.25 * nzz > 0.98 - moss * 0.55)
    mossm |= inside & (ny > 0.5) & (mn2 > 0.62) & (moss > 0.2)
    lm = inside & (rng.random((h, w)) < lichen) & (idx >= 2)
    for y, x in zip(*np.nonzero(inside)):
        i = int(idx[y, x])
        if mossm[y, x]:
            mi = min(3, i * 4 // n)
            c = MOSS[mi] if i < n - 1 else (176, 170, 72)
        else:
            c = stone_pix(i, ramp_)
        L.put(x, y, c)
    for y, x in zip(*np.nonzero(lm)):
        gi = 1 + int(rng.integers(0, 2))
        L.put(x, y, GOLD_L[gi])
        if rng.random() < 0.5:
            L.put(x + 1, y, GOLD_L[0])
    # cracks: random walk downward, dark line + 1 px lit lip on the right
    for _ in range(cracks):
        xs0 = np.nonzero(inside[int(max(cy - ry * 0.4, 0))])[0]
        if len(xs0) < 4:
            continue
        x = int(rng.choice(xs0[len(xs0) // 4: -len(xs0) // 4 or None]))
        y = int(max(cy - ry * 0.4, 0))
        for _s in range(int(rng.integers(5, 10))):
            if 0 <= y < h and 0 <= x < w and inside[y, x]:
                L.put(x, y, ramp_[0])
                if x + 1 < w and inside[y, x + 1]:
                    L.put(x + 1, y, ramp_[min(n - 1, 4)])
            y += 1
            x += int(rng.choice([-1, 0, 0, 1]))
    sub = Spr(w, h)
    sub.a[inside] = L.a[inside]
    outline_inner(sub)
    L.a[inside] = sub.a[inside]
    return inside


def stone_block(L, x0, y0, x1, y1, topH, rng, ramp_=STONE, chip=2, speck=0.14, moss=0.0, tone=0):
    """3/4-view block: lit top face, front face with left bevel and right/bottom shade."""
    t = Spr(L.w, L.h)
    n = len(ramp_)
    fy0 = y0 + topH
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if y < fy0:
                i = n - 2 + tone
                if rng.random() < speck * 1.5:
                    i -= 1
                if x == x0:
                    i = n - 1
            else:
                i = 2 + tone
                r = rng.random()
                if r < speck * 0.5:
                    i -= 1
                elif r < speck:
                    i += 1
                if x == x0 or y == fy0:
                    i = 3 + tone
                if x == x1 or y == y1:
                    i = 1 + tone
            t.put(x, y, stone_pix(int(np.clip(i, 0, n - 1)), ramp_))
    # chipped corners
    for cxp, cyp, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        if chip and rng.random() < 0.55:
            k = int(rng.integers(1, chip + 1))
            for dy in range(k):
                for dx in range(k - dy):
                    t.a[cyp + sy * dy, cxp + sx * dx] = 0
    if moss > 0:
        mn = vnoise(L.w, L.h, 3, rng)
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if t.a[y, x, 3] and (y < fy0 + 2 or y > y1 - 2) and mn[y, x] > 1 - moss:
                    t.put(x, y, MOSS[2 if y < fy0 else 1])
    outline_inner(t)
    L.blit(t)


def sun_disc(L, cx, cy, r, rng, erode=0.25, leaf=True):
    """Eroded sun-disc carving: engraved ring + short rays, a few gold-leaf specks left."""
    for yy in range(int(cy - r - 3), int(cy + r + 4)):
        for xx in range(int(cx - r - 3), int(cx + r + 4)):
            d = np.hypot(xx + 0.5 - cx, yy + 0.5 - cy)
            a = np.arctan2(yy + 0.5 - cy, xx + 0.5 - cx)
            keep = rng.random() > erode
            if r - 0.9 < d < r + 0.2 and keep:               # ring groove
                L.put(xx, yy, STONE[1])
                L.put(xx - 1, yy - 1, STONE[4]) if not L.get(xx - 1, yy - 1) == STONE[1] and rng.random() < 0.3 else None
            elif d < r * 0.45 and keep:                        # boss
                L.put(xx, yy, STONE[3] if d > r * 0.2 else STONE[1])
            elif r + 1 < d < r + 3.2 and keep:                 # rays every 30 deg
                k = (a / (np.pi / 6)) % 1
                if k < 0.17 or k > 0.83:
                    L.put(xx, yy, STONE[1])
    if leaf:
        for _ in range(5):
            a = rng.random() * 6.283
            rr = rng.random() * r * 0.45
            L.put(cx + np.cos(a) * rr, cy + np.sin(a) * rr, GOLD_L[2])
        L.put(cx - 1, cy - 1, GOLD_L[3])


# ---------------------------------------------------------------- sprites
def seeded(name, v):
    return np.random.default_rng(zlib.crc32(name.encode()) + 977 * v)


def make_grass_tuft(w, h, v, frame, name):
    rng = seeded(name, v)
    L = Spr(w, h)
    base = h - 2
    nb = int(rng.integers(13, 18)) if w < 20 else int(rng.integers(20, 27))
    cx = (w - 1) / 2
    spread = w * 0.40
    blades = []
    for i in range(nb):
        off = rng.normal(0, spread * 0.55)
        off = float(np.clip(off, -spread, spread))
        hgt = (h - 4) * (0.5 + 0.5 * (1 - abs(off) / (spread + 1))) * (0.8 + 0.35 * rng.random())
        blades.append((off, hgt, i))
    blades.sort(key=lambda b: b[1])   # short first -> tall ones drawn on top
    seed_n = int(rng.integers(1, 3)) if v != 3 else 0
    tallest = sorted(blades, key=lambda b: -b[1])[:seed_n]
    sw = 1.6 if frame else -1.0
    for off, hgt, i in blades:
        back = (i % 3 == 0)
        pal = GRASS_B if back else (GRASS_O if i % 7 == 0 else GRASS_F)
        lean = off / spread * 0.55 + (rng.random() - 0.5) * 0.3
        curve = (rng.random() - 0.5) * 0.9
        x0 = cx + off * 0.45
        n = int(hgt * 2) + 1
        last = None
        for s in range(n + 1):
            t = s / n
            x = x0 + lean * t * hgt + curve * t * t * hgt * 0.5 + sw * (t ** 2) * (0.6 + hgt / 14)
            y = base - t * hgt
            ci = int(round(t * (len(pal) - 1) * 0.95 + (0.5 if (off < 0) else 0)))
            ci = min(len(pal) - 1, ci)
            L.put(round(x), round(y), pal[ci])
            if t < 0.28 and hgt > 8:
                L.put(round(x) + 1, round(y), pal[max(0, ci - 1)])
            last = (round(x), round(y), t)
        if (off, hgt, i) in tallest and hgt > 7:
            hx_, hy_, _ = last
            for dy in range(0, 4):                      # seed head: oval with awns
                for dx in (0, 1):
                    L.put(hx_ + dx - (0 if dy < 3 else 0), hy_ - dy, hx("#b8741f") if dx else hx("#d89a30"))
            L.put(hx_, hy_ - 4, hx("#fff0b4"))
            L.put(hx_ - 1, hy_ - 2, hx("#fbe596"))
            L.put(hx_ + 2, hy_ - 3, hx("#fbe596"))
    for x in range(int(cx - spread), int(cx + spread) + 1):           # root shadow
        if rng.random() < 0.7:
            L.put(x, base + 1, GRASS_B[0])
        if rng.random() < 0.4:
            L.put(x, base, GRASS_B[1])
    return L


def make_flame_flower(v, frame):
    name = "flame_flower"
    rng = seeded(name, v)
    w, h = 16, 24
    L = Spr(w, h)
    sw = (1 if frame else 0) * 1.2
    cx = 8
    # base leaves: two long pointed leaves, gold-rimmed olive
    for side, ln in ((-1, 7 + (v % 2)), (1, 6 + ((v + 1) % 2)), (-1, 4)):
        for s in range(ln * 2):
            t = s / (ln * 2)
            x = cx + side * (1 + t * ln * 0.75)
            y = h - 2 - t * ln * 0.9 + t * t * ln * 0.45
            L.put(round(x), round(y), LEAF[3 if side < 0 else 2] if t > 0.3 else LEAF[1])
            if t < 0.7:
                L.put(round(x), round(y) + 1, LEAF[1])
    # stem
    stem_top = 12
    for y in range(stem_top, h - 1):
        t = (h - 2 - y) / (h - 2 - stem_top)
        x = cx + sw * t * t * 1.5 + (0.6 * np.sin(v + y * 0.4))
        L.put(round(x), y, LEAF[2])
        L.put(round(x) + 1, y, LEAF[1])
    hx0 = cx + sw * 1.5
    hy0 = stem_top + 1

    def petal(ang, length, wb, bright):
        steps = int(length * 3)
        for s in range(steps + 1):
            t = s / steps
            px = hx0 + np.sin(ang) * t * length
            py = hy0 - np.cos(ang) * t * length
            half = wb * (1 - t) ** 0.85
            o = -half
            while o <= half + 1e-6:
                qx, qy = px + np.cos(ang) * o, py + np.sin(ang) * o
                e = abs(o) / (half + 1e-6) if half > 0.05 else 0
                ci = 5 - (e > 0.55) - (e > 0.9) * 1 - (t > 0.8) * 1 + (t < 0.35) * 1 + bright
                if o < 0 and e > 0.5:
                    ci += 1
                L.put(round(qx), round(qy), FLAME[int(np.clip(ci, 0, 6))])
                o += 0.5
    angs = [-1.25, 1.2, -0.85, 0.82, -0.45, 0.42, 0.0]
    lens = [6.0, 6.0, 7.5, 7.5, 8.8, 8.5, 9.5]
    for k, (a, ln) in enumerate(zip(angs, lens)):
        a2 = a + (rng.random() - 0.5) * 0.18 + sw * 0.08
        petal(a2, ln + (rng.random() - 0.5) * 1.5, 1.7 if k < 6 else 1.9, -1 if a > 0 else (0 if k < 6 else 0))
    for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1)):
        L.put(round(hx0) + dx, hy0 + 1 + dy, FLAME[6])
    # glow sparks
    for _ in range(2):
        L.put(int(hx0 + rng.integers(-6, 7)), int(rng.integers(0, 4)), FLAME[5])
    outline_outer(L, 0.42)
    return L


def make_flower_cluster(kind, v):
    rng = seeded("flower_" + kind, v)
    L = Spr(12, 12)
    if kind == "purple":
        pal = ramp("#4f2f6e", "#7a4e9c", "#9a6ab8", "#bd90d6", "#e2c8ee")
    else:
        pal = ramp("#8c7f96", "#bdb2bc", "#ede4d2", "#fffaf0", "#ffffff")
    cenc = hx("#e2b44c")
    cnt = 3 + (v % 2)
    pts = [(2 + (i * 3 + int(rng.integers(0, 2))) % 9, 4 + int(rng.integers(0, 4)) - (i % 2) * 2) for i in range(cnt)]
    pts = sorted(set(pts), key=lambda p: p[1])
    for (fx, fy) in pts:
        for y in range(fy + 2, 11):                           # stalk
            L.put(fx, y, LEAF[2] if (y + fx) % 2 else LEAF[1])
        if rng.random() < 0.7:
            L.put(fx - 1, 9, LEAF[3]); L.put(fx + 1, 8, LEAF[3]) if rng.random() < 0.5 else None
    for (fx, fy) in sorted(pts, key=lambda p: p[1]):
        for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
            L.put(fx + dx, fy + dy, pal[3] if (dx < 0 or dy < 0) else pal[2])
        L.put(fx, fy, cenc)
        L.put(fx + 1, fy + 1, pal[1]) if rng.random() < 0.5 else None
    # leaf rosette at the foot
    for x in range(1, 11):
        if rng.random() < 0.8:
            L.put(x, 11, LEAF[1])
        if rng.random() < 0.5:
            L.put(x, 10, LEAF[2])
    outline_outer(L, 0.5)
    return L


def make_shrub(kind, v):
    name = "shrub_" + kind
    rng = seeded(name, v)
    w, h = 32, 28
    L = Spr(w, h)
    pal = SHRUB_O if kind == "round" else SHRUB_D
    zz = np.full((h, w), -1.0)
    ys, xs = np.mgrid[0:h, 0:w]
    cxe, cye, rxe, rye = 15.5, 17.0, 13.5, 8.5
    lumps = []
    for i in range(14):
        a = rng.random() * 6.283
        rr = np.sqrt(rng.random()) * 0.85
        lx, ly = cxe + np.cos(a) * rr * rxe, cye + np.sin(a) * rr * rye * 0.9 - 1.0
        lr = 4.2 + rng.random() * 3.6
        lumps.append((lx, ly, lr))
    lumps.append((cxe, cye - 5, 7.5))
    lumps.append((cxe - 8, cye + 1, 6))
    lumps.append((cxe + 8, cye + 2, 6))
    lumps.sort(key=lambda t: t[1])
    for lx, ly, lr in lumps:
        nx, ny = (xs + 0.5 - lx) / lr, (ys + 0.5 - ly) / (lr * 0.9)
        d = 1 - nx * nx - ny * ny
        z = np.where(d > 0, np.sqrt(np.clip(d, 0, 1)) * lr * 0.9 + (h - ly) * 0.12, -1)
        zz = np.maximum(zz, z)
    inside = zz > 0
    inside &= ys <= h - 3
    z = np.where(inside, zz, 0)
    idx, I, nzz = shade(z, len(pal), dither=0.28, gain=1.3, lo=-0.1, hi=1.1)
    gl = vnoise(w, h, 2, rng)
    for y, x in zip(*np.nonzero(inside)):
        i = int(idx[y, x])
        if gl[y, x] > 0.8 and i < len(pal) - 1:
            i += 1                                           # leaf glints
        if (y > h - 7) and gl[y, x] < 0.3:
            i = max(0, i - 1)                                # dark undergrowth
        L.put(x, y, pal[i])
    # ground contact + flowers/berries
    if v % 2 == 0:
        col = hx("#d8693a") if kind == "round" else hx("#9a6ab8")
        for _ in range(5):
            x, y = int(rng.integers(6, 26)), int(rng.integers(12, 22))
            if inside[y, x]:
                L.put(x, y, col); L.put(x - 1, y - 1, mix(col, (255, 245, 200), 0.5))
    outline_inner(L, 0.45, 0.7)
    ground_blades(L, rng, 6, 3, 28, h - 1, 3)
    return L


def make_conifer(w, h, ntiers, v, name, maxw_frac=1.0):
    rng = seeded(name, v)
    L = Spr(w, h)
    cx = (w - 1) / 2
    trunk_h = 5 if h < 48 else 7
    ground = h - 2
    for y in range(ground - trunk_h + 1, ground + 1):
        tw = 3 if w < 28 else 4
        x0 = int(round(cx - tw / 2 + 0.5))
        for k in range(tw):
            L.put(x0 + k, y, WOOD[3 - min(k, 2)] if k == 0 else WOOD[1 + (k == 1)])
    body_bot = ground - trunk_h + 2
    top = 2
    span = body_bot - top
    step = span / ntiers
    maxw = (w - 3) / 2 * maxw_frac
    for i in range(ntiers - 1, -1, -1):
        bot = top + (i + 1) * step
        apex = bot - step * 1.9
        if i == 0:
            apex = top
        bw = maxw * (0.30 + 0.70 * (i + 1) / ntiers) * (0.80 + 0.28 * rng.random())
        cxo = cx + (rng.random() - 0.5) * 2.2 * (1 if i < ntiers - 1 else 0)
        prev = L.mask().copy()
        tier = Spr(w, h)
        for y in range(int(round(apex)), int(round(bot)) + 1):
            t = (y - apex) / max(1, bot - apex)
            hw = bw * (max(t, 0) ** 0.9) + (rng.random() - 0.5) * 1.6
            if y >= int(round(bot)) - 1:
                hw += 1.0 if ((y + int(rng.integers(0, 2))) % 2) else 0.2
            hw = max(hw, 0.6)
            for x in range(int(round(cxo - hw)), int(round(cxo + hw)) + 1):
                nx = (x - cxo) / max(hw, 0.8)
                I = 0.52 - 0.50 * nx - 0.12 * t
                if y >= int(round(bot)) - 1:
                    I -= 0.25
                tier.put(x, y, (int(np.clip(I, 0, 0.999) * 100), 0, 0))
        # shade index -> colour with needle texture
        tex = vnoise(w, h, 2, rng)
        for y in range(h):
            for x in range(w):
                if tier.a[y, x, 3]:
                    I = tier.a[y, x, 0] / 100.0
                    I += 0.09 * (int(tex[y, x] > 0.68) - int(tex[y, x] < 0.30))
                    ci = int(np.clip(I * len(CON), 0, len(CON) - 1))
                    tier.a[y, x, :3] = CON[ci]
        # gold rim on the left edge of lit tiers
        m = tier.mask()
        for y in range(h):
            xs_ = np.nonzero(m[y])[0]
            if len(xs_) > 2:
                x0 = xs_[0]
                yy_t = (y - apex) / max(1, bot - apex)
                if y < int(round(bot)) - 1:
                    tier.put(x0, y, CON_RIM[1] if rng.random() < 0.8 else CON_RIM[2])
                    if len(xs_) > 5 and rng.random() < 0.55:
                        tier.put(x0 + 1, y, CON_RIM[0])
        # shadow this tier casts on what is below it
        sh = np.zeros_like(prev)
        sh[2:, :] = tier.mask()[:-2, :]
        sh &= prev & ~tier.mask()
        for y, x in zip(*np.nonzero(sh)):
            L.a[y, x, :3] = dk(tuple(int(v_) for v_ in L.a[y, x, :3]), 0.62)
        L.blit(tier)
    outline_outer(L, 0.5)
    return L


def make_boulder(kind, v):
    sizes = {"s": (20, 16), "m": (32, 24), "l": (48, 36)}
    w, h = sizes[kind]
    rng = seeded("boulder_" + kind, v)
    L = Spr(w, h)
    if kind == "s":
        rock(L, w * 0.58, h * 0.62, 8.2, 6.2, 7, rng, moss=0.3 + 0.2 * (v % 2), lichen=0.03, cracks=0)
        rock(L, w * 0.25, h * 0.78, 4.0, 3.0, 3.2, rng, moss=0.2, lichen=0.0, cracks=0)
        gb = 4
    elif kind == "m":
        rock(L, w * 0.42, h * 0.52, 12.5, 9.5, 11, rng, moss=0.55, lichen=0.03, cracks=1, wob=0.1)
        rock(L, w * 0.76, h * 0.72, 7, 5.5, 6, rng, moss=0.4, lichen=0.03, cracks=0)
        rock(L, w * 0.12, h * 0.80, 4.2, 3.2, 3.5, rng, moss=0.3, lichen=0.0, cracks=0)
        gb = 7
    else:
        rock(L, w * 0.44, h * 0.46, 19, 14, 17, rng, moss=0.65, lichen=0.03, cracks=2, wob=0.1, p=2.8)
        rock(L, w * 0.20, h * 0.60, 9, 7, 8, rng, moss=0.4, lichen=0.03, cracks=1)
        rock(L, w * 0.80, h * 0.70, 10, 8, 8, rng, moss=0.5, lichen=0.03, cracks=1)
        rock(L, w * 0.60, h * 0.88, 6, 4, 4, rng, moss=0.3, lichen=0.0, cracks=0)
        rock(L, w * 0.08, h * 0.84, 4.5, 3.4, 3.4, rng, moss=0.3, lichen=0.0, cracks=0)
        gb = 11
    ground_blades(L, rng, gb, 2, w - 3, h - 1, 3 + (kind != "s"))
    return L


def make_pillar(v):
    rng = seeded("ruin_pillar", v)
    w, h = 24, 48
    L = Spr(w, h)
    # plinth
    stone_block(L, 1, 38, 22, 47, 3, rng, moss=0.5, chip=3)
    # fallen chunk
    rock(L, 19.5, 45, 3.8, 2.8, 3.5, rng, p=3.4, moss=0.3, lichen=0.0, cracks=0, wob=0.04)
    # shaft: analytic cylinder, broken jagged top
    x0, x1 = 5, 18
    cxs = (x0 + x1 + 1) / 2
    r = (x1 - x0 + 1) / 2
    base_top = 7 + (v % 3) * 2
    tilt = (-0.5 + 0.3 * v) * 0.7
    shaft = Spr(w, h)
    jag = [int(round(rng.random() * 2.2)) for _ in range(w)]
    for x in range(x0, x1 + 1):
        top = int(round(base_top + (x - cxs) * tilt + jag[x]))
        for y in range(top, 39):
            nx = (x + 0.5 - cxs) / r
            I = 0.58 - 0.62 * nx - 0.12 * nx * nx
            fl = (x - x0) % 4
            if fl == 0:
                I -= 0.14
            elif fl == 1:
                I += 0.05
            I += (rng.random() - 0.5) * 0.10
            ci = int(np.clip(I * len(STONE), 0, len(STONE) - 1))
            shaft.put(x, y, stone_pix(ci, STONE))
        # fracture face
        shaft.put(x, top, STONE[5]); shaft.put(x, top + 1, STONE[4] if rng.random() < 0.8 else STONE[3])
    # drum joints (curved)
    for jy in (19, 29):
        for x in range(x0, x1 + 1):
            yy = jy + int(round(((x - cxs) / r) ** 2 * -1.0 + 1))
            if shaft.get(x, yy):
                shaft.put(x, yy, STONE[0])
                shaft.put(x, yy + 1, STONE[4] if x < cxs else STONE[3])
    # carved band with eroded gold leaf wedge marks
    for x in range(x0, x1 + 1):
        shaft.put(x, 23, STONE[1]); shaft.put(x, 27, STONE[1])
        if shaft.get(x, 24):
            shaft.put(x, 24, STONE[4] if x < cxs else STONE[2])
    for x in range(x0 + 1, x1, 3):
        for y in (25, 26):
            if rng.random() < 0.8:
                shaft.put(x, y, GOLD_L[2] if y == 25 else GOLD_L[0])
        shaft.put(x + 1, 25, STONE[0]) if rng.random() < 0.8 else None
    # crack
    cxk, cyk = int(rng.integers(8, 15)), 11 + (v % 3)
    for k in range(14):
        if shaft.get(cxk, cyk):
            shaft.put(cxk, cyk, STONE[0])
        cyk += 1
        cxk += int(rng.choice([-1, 0, 0, 1]))
    # moss creeping up the shaft base and on the break
    mn = vnoise(w, h, 3, rng)
    for y in range(h):
        for x in range(w):
            if shaft.a[y, x, 3]:
                cm = (y > 30 and mn[y, x] > 0.62 - (y - 30) * 0.03) or (y < base_top + 4 and mn[y, x] > 0.72)
                if cm:
                    i = int(np.clip(shaft.a[y, x, 0] / 255 * 4, 0, 3))
                    shaft.put(x, y, MOSS[min(3, 1 + (1 if x < cxs else 0))])
    outline_inner(shaft)
    L.blit(shaft)
    # plinth top face moss tufts, grass at foot
    ground_blades(L, rng, 8, 2, 22, 47, 3)
    return L


def make_cairn(v):
    rng = seeded("ruin_cairn", v)
    w, h = 32, 32
    L = Spr(w, h)
    spec = [  # cx, cy, rx, ry, rz
        (16, 7.5, 4.6, 3.4, 4.2),
        (11, 15, 6.4, 4.6, 5.4), (21.5, 14.5, 5.6, 4.4, 5.2),
        (8, 25, 7.0, 5.0, 5.8), (17.5, 25.5, 7.6, 5.4, 6.2), (26, 25, 5.4, 4.2, 4.6),
    ]
    if v % 2:
        spec[0] = (14, 8, 5.2, 3.6, 4.4)
        spec.append((26, 14.5, 3.6, 2.8, 3.0))
    spec.sort(key=lambda s: s[1])
    for i, (cx, cy, rx, ry, rz) in enumerate(spec):
        rock(L, cx + rng.normal(0, .4), cy, rx, ry, rz, rng, p=2.9, wob=0.08,
             moss=0.55 if i > 2 else 0.25, lichen=0.04, cracks=int(i % 2 == 0), dither=0.08)
    ground_blades(L, rng, 9, 2, 29, h - 1, 3)
    return L


def make_wall(v):
    rng = seeded("ruin_wall", v)
    w, h = 64, 40
    L = Spr(w, h)
    wall = Spr(w, h)
    base_y = 36
    # profile: tall at left, stepping down to the right, with bites
    prof = []
    for x in range(2, 50):
        t = (x - 2) / 47
        top = 8 + t * 20 + rng.normal(0, 0.8)
        prof.append(top)
    prof = np.array(prof)
    for bite in range(3):                                  # missing blocks
        bx = int(rng.integers(4, 40))
        bw = int(rng.integers(3, 7))
        prof[bx:bx + bw] += rng.integers(3, 6)
    prof = np.round(np.minimum(prof, base_y - 4)).astype(int)
    course = 7
    offs = {}
    for x in range(2, 50):
        top = prof[x - 2]
        for y in range(top - 3, base_y + 1):
            if y < top:                                      # top face
                wall.put(x, y, STONE[4] if rng.random() > 0.2 else STONE[3])
                continue
            cy_ = (y - base_y) // course
            row = (base_y - y) // course
            yin = (base_y - y) % course
            bwid = 11 + (row * 5) % 7
            xo = (x - 2 + (row * 6) % bwid) % bwid
            bid = ((x - 2 + (row * 6) % bwid) // bwid, row)
            if bid not in offs:
                offs[bid] = int(rng.integers(-1, 2))
            i = 2 + offs[bid]
            if xo == 0:
                i = 3
            if xo == bwid - 1 or yin == 0:
                i = 1
            if yin == course - 1:
                i = 3
            if rng.random() < 0.08:
                i += int(rng.choice([-1, 1]))
            wall.put(x, y, stone_pix(int(np.clip(i, 0, 5)), STONE))
        # right end bevel shading near edge handled by outline
    # carved sun-disc slab on the tall part
    sx0, sy0, sx1, sy1 = 9, 14, 29, 33
    for y in range(sy0, sy1 + 1):
        for x in range(sx0, sx1 + 1):
            if wall.get(x, y) is not None and y > prof[x - 2] + 1:
                wall.put(x, y, STONE[2] if (x + y) % 5 else STONE[3])
                if x == sx0 or y == sy0:
                    wall.put(x, y, STONE[4])
                if x == sx1 or y == sy1:
                    wall.put(x, y, STONE[1])
    sun_disc(wall, 19, 24, 5.2, rng, erode=0.22)
    # cracks
    for cx_ in (12, 31, 40):
        y = int(prof[cx_ - 2]) + 2
        x = cx_
        for _ in range(int(rng.integers(6, 12))):
            if wall.get(x, y):
                wall.put(x, y, STONE[0])
            y += 1
            x += int(rng.choice([-1, 0, 0, 1]))
    # moss on top and base
    mn = vnoise(w, h, 3, rng)
    for y in range(h):
        for x in range(w):
            if wall.a[y, x, 3]:
                top_zone = (x >= 2 and x < 50 and y < prof[min(max(x - 2, 0), 47)] + 3)
                if (top_zone and mn[y, x] > 0.45) or (y > base_y - 3 and mn[y, x] > 0.5):
                    wall.put(x, y, MOSS[2 if y < base_y - 4 else 1] if mn[y, x] < 0.8 else MOSS[3])
    for _ in range(18):                                     # gold lichen
        x, y = int(rng.integers(3, 49)), int(rng.integers(12, 34))
        if wall.get(x, y) and y > prof[x - 2] + 2 and wall.get(x, y) not in MOSS:
            wall.put(x, y, GOLD_L[1])
    outline_inner(wall)
    L.blit(wall)
    # rubble
    rock(L, 53, 33, 6.5, 4.5, 5, rng, p=3.0, moss=0.4, lichen=0.04, cracks=0)
    rock(L, 58.5, 36, 4.2, 3.0, 3.2, rng, p=3.0, moss=0.3, lichen=0.0, cracks=0)
    rock(L, 45, 36, 4.0, 2.8, 3, rng, p=3.0, moss=0.2, lichen=0.0, cracks=0)
    ground_blades(L, rng, 12, 2, 62, h - 1, 3)
    return L


def make_arch(v):
    rng = seeded("ruin_arch_fragment", v)
    w, h = 48, 56
    L = Spr(w, h)
    cx, cy, ri, ro = 28.0, 26.0, 11.0, 19.0
    depth = 4
    ys, xs = np.mgrid[0:h, 0:w]
    dx, dy = xs + 0.5 - cx, ys + 0.5 - cy
    r = np.hypot(dx, dy)
    ang = np.degrees(np.arctan2(-dy, dx)) % 360       # 0=east, 90=up, 180=west
    cut = 28 + 14 * (v % 2) + int(rng.integers(0, 8))  # arch broken on the right at this angle
    ring = (r >= ri) & (r <= ro) & (dy <= 0.5) & (ang >= cut) & (ang <= 180.5)
    pier = (xs >= 3) & (xs <= 17) & (ys >= int(cy)) & (ys <= 47)
    front = ring | pier
    # extruded top face (visible because the camera looks down)
    top = np.zeros_like(front)
    for k in range(1, depth + 1):
        sh = np.zeros_like(front)
        sh[:-k, :] = front[k:, :]
        top |= sh
    top &= ~front
    fl = Spr(w, h)
    # top face
    for y, x in zip(*np.nonzero(top)):
        fl.put(x, y, STONE[4] if rng.random() > 0.25 else STONE[3])
    # front face, voussoir joints
    for y, x in zip(*np.nonzero(front)):
        i = 2
        if ring[y, x]:
            rn = (r[y, x] - ri) / (ro - ri)
            seg = int(ang[y, x] // 20)
            i = 2 + (1 if seg % 2 else 0) - (1 if (seg % 3 == 0) else 0)
            if (ang[y, x] % 20) < 1.4:
                i = 1
            elif (ang[y, x] % 20) > 18.6:
                i = 3
            if rn < 0.14:
                i = 1
            elif rn > 0.88:
                i = 3
        else:
            nxp = (x - 3) / 14.0
            i = 3 - int(nxp * 2.2) + int(rng.random() < 0.1) - int(rng.random() < 0.1)
            # capital band + base plinth
            if int(cy) <= y <= int(cy) + 2:
                i = 4 if y == int(cy) else 3
            if y in (int(cy) + 3,):
                i = 1
            if y >= 44:
                i = 2
            if x == 3:
                i = 4 if y < 44 else 3
            if x == 17:
                i = 1
        fl.put(x, y, stone_pix(int(np.clip(i, 0, 5)), STONE))
    # pier top above capital: the springing stones that continue the arch downwards already; fluting
    for x in (7, 11, 14):
        for y in range(int(cy) + 5, 44):
            if fl.get(x, y):
                fl.put(x, y, STONE[1] if (x == 7 or x == 14) else STONE[3])
    # capital and base slab protrusions
    for x in range(2, 19):
        fl.put(x, int(cy) + 3, STONE[1] if x > 3 else STONE[2])
    for x in range(1, 20):
        for y in range(44, 48):
            fl.put(x, y, STONE[3] if y == 44 else (STONE[2] if y < 47 else STONE[1]))
    for x in range(1, 20):
        fl.put(x, 43, STONE[4])
    # carved band + gold leaf along the keystone ring
    for a in range(cut + 12, 170, 11):
        rr = (ri + ro) / 2
        px, py = cx + rr * np.cos(np.radians(a)), cy - rr * np.sin(np.radians(a))
        if rng.random() < 0.75:
            fl.put(px, py, GOLD_L[2]); fl.put(px + 1, py, GOLD_L[0])
    # gold sun hint on the pier
    fl.put(10, 33, GOLD_L[3]); fl.put(9, 33, GOLD_L[2]); fl.put(11, 33, GOLD_L[2]); fl.put(10, 32, GOLD_L[2]); fl.put(10, 34, GOLD_L[1])
    # cracks and moss
    for sx in (9, 15):
        x, y = sx, 30
        for _ in range(int(rng.integers(8, 13))):
            if fl.get(x, y):
                fl.put(x, y, STONE[0])
            y += 1
            x += int(rng.choice([-1, 0, 0, 1]))
    mn = vnoise(w, h, 3, rng)
    for y in range(h):
        for x in range(w):
            if fl.a[y, x, 3] and top[y, x] and mn[y, x] > 0.42:
                fl.put(x, y, MOSS[2] if mn[y, x] < 0.75 else MOSS[3])
            elif fl.a[y, x, 3] and (y > 40 and mn[y, x] > 0.55):
                fl.put(x, y, MOSS[1])
            elif fl.a[y, x, 3] and ring[y, x] and mn[y, x] > 0.82:
                fl.put(x, y, MOSS[1])
    outline_inner(fl)
    L.blit(fl)
    # fallen voussoirs
    rock(L, 33, 50.5, 5.6, 3.8, 4.2, rng, p=3.3, moss=0.3, lichen=0.04, cracks=0, wob=0.04)
    rock(L, 42, 52, 3.8, 2.8, 3, rng, p=3.3, moss=0.2, lichen=0.0, cracks=0, wob=0.04)
    ground_blades(L, rng, 10, 1, 46, h - 1, 3)
    return L


def make_stepping_stone(v):
    rng = seeded("stepping_stone", v)
    w, h = 16, 10
    L = Spr(w, h)
    rx, ry = 7.0 + 0.4 * (v % 2), 3.2 + 0.3 * (v // 2)
    cx, cy = 7.8 + 0.3 * v, 4.2
    ys, xs = np.mgrid[0:h, 0:w]
    wob = 1 + 0.12 * np.sin(np.arctan2(ys - cy, xs - cx) * 2 + v) + 0.08 * np.sin(np.arctan2(ys - cy, xs - cx) * 3 + 2 * v)
    d = np.hypot((xs + 0.5 - cx) / rx, (ys + 0.5 - cy) / ry) / wob
    topm = d < 1
    sh = np.zeros_like(topm)
    sh[2:, :] = topm[:-2, :]
    sh &= ~topm
    for y, x in zip(*np.nonzero(sh)):
        L.put(x, y, STONE[1] if x > cx - 2 else STONE[2])
    mn = vnoise(w, h, 3, rng)
    for y, x in zip(*np.nonzero(topm)):
        dd = d[y, x]
        i = 4 if (x + 0.5 < cx and y + 0.5 < cy) else 3
        if rng.random() < 0.18:
            i -= 1
        if dd > 0.82 and x > cx:
            i = 2
        c = stone_pix(i, STONE)
        if mn[y, x] > 0.72:
            c = MOSS[2]
        L.put(x, y, c)
    for _ in range(2):
        x, y = int(rng.integers(3, 13)), int(rng.integers(2, 6))
        if topm[y, x]:
            L.put(x, y, GOLD_L[1])
    outline_inner(L)
    return L


def make_post(v):
    rng = seeded("fence_wood_post", v)
    w, h = 8, 24
    L = Spr(w, h)
    cx0 = 1
    top = 2 + (v == 2) * 3
    # front face 6 wide, top cap 2 high, base mound
    for y in range(top, h - 2):
        for x in range(cx0, cx0 + 6):
            i = 2
            if x == cx0:
                i = 3
            if x >= cx0 + 4:
                i = 1
            if (x + y * 3) % 7 == 0 and rng.random() < 0.8:
                i -= 1
            L.put(x, y, WOOD[int(np.clip(i, 0, 4))])
    for x in range(cx0, cx0 + 6):
        L.put(x, top, WOOD[4]); L.put(x, top - 1, WOOD[4] if x < cx0 + 5 else WOOD[3])
    if v == 1:
        L.a[top - 1, cx0 + 5] = 0; L.a[top, cx0 + 5] = 0
    if v == 2:                                           # snapped, slanted top
        for k in range(6):
            for y in range(top - 2, top + (5 - k) // 2):
                L.a[y, cx0 + k] = 0
        for k in range(6):
            L.put(cx0 + k, top + (5 - k) // 2, WOOD[4])
    for y in (7, 14):                                     # grain nicks
        L.put(cx0 + 2, y, WOOD[0]); L.put(cx0 + 2, y + 1, WOOD[1])
    outline_inner(L, 0.45, 0.62)
    for x in range(0, 8):                                 # dirt mound + blades
        L.put(x, h - 2, hx("#7d5634")) if 1 <= x <= 6 else None
        L.put(x, h - 1, hx("#6b4a2e")) if 2 <= x <= 5 else None
    ground_blades(L, rng, 3, 0, 7, h - 2, 3)
    return L


def make_rail(v):
    rng = seeded("fence_wood_rail", v)
    w, h = 32, 16
    L = Spr(w, h)
    for ry, th in ((2, 3), (8, 3)):                       # two rails: top face 1 + front 3
        if v == 1 and ry == 8:
            xs_ = range(0, 18)
        elif v == 2 and ry == 2:
            xs_ = range(0, 32)
        else:
            xs_ = range(0, 32)
        for x in xs_:
            yoff = 0
            if v == 2 and ry == 2 and x > 20:
                yoff = (x - 20) // 4                      # sagging rail
            L.put(x, ry + yoff, WOOD[4])
            for k in range(1, th + 1):
                i = 2 if k < th else 1
                if (x * 5 + k * 3) % 11 == 0:
                    i -= 1
                L.put(x, ry + yoff + k, WOOD[i])
            if x % 13 == 7:
                L.put(x, ry + yoff + 2, WOOD[0])
    sub = Spr(w, h)
    sub.a = L.a.copy()
    outline_outer(L, 0.5)
    # nails
    L.put(1, 4, hx("#c0b090")); L.put(1, 10, hx("#c0b090")) if v != 1 else None
    L.put(30, 4, hx("#c0b090"))
    return L


def make_rail_v(v):
    rng = seeded("fence_wood_rail_v", v)
    w, h = 10, 44
    L = Spr(w, h)
    # two rails running away from the camera: boxes with top face (lit) and front-end cap
    for hgt, wx in ((5, 6), (13, 6)):
        yb = 42 - 13 + (13 - hgt)                          # end of rail on screen
        for y in range(yb - 30, yb):                      # top face
            for x in range(2, 2 + wx):
                i = 4 if x == 2 else (2 if x == 2 + wx - 1 else 3)
                if (y * 7 + x) % 9 == 0:
                    i -= 1
                L.put(x, y, WOOD[i])
        for y in range(yb, yb + 3):                       # front end face
            for x in range(2, 2 + wx):
                L.put(x, y, WOOD[2 if y == yb else 1])
        if hgt == 13:
            pass
    # shaded ground strip beneath
    outline_outer(L, 0.5)
    for y in range(40, 44):
        pass
    return L


def make_shadow(kind):
    w, h = {"s": (16, 6), "m": (28, 9), "l": (44, 13)}[kind]
    L = Spr(w, h)
    cx, cy = (w - 1) / 2, (h - 1) / 2
    for y in range(h):
        for x in range(w):
            d = np.hypot((x - cx) / (w / 2), (y - cy) / (h / 2))
            if d <= 0.78:
                L.put(x, y, hx("#2a1a12"), 128)
            elif d <= 1.0:
                L.put(x, y, hx("#2a1a12"), 64 if (x + y) % 2 == 0 or d < 0.9 else 0)
    return L


# ---------------------------------------------------------------- catalogue
def build(out):
    out.mkdir(parents=True, exist_ok=True)
    for f in out.glob("*.png"):
        f.unlink()
    entries = []

    def save(name, L, fp, block, sway=False, frames=None, extra=None):
        fn = f"{name}.png"
        L.image().save(out / fn)
        e = {"file": fn, "size": [L.w, L.h], "pivot_px": [L.w // 2, L.h - 1],
             "pivot_note": "px from top-left, y down; base-centre of the prop. Unity pivot = [x/w, 1 - y/h]",
             "footprint_tiles": fp, "blocks_movement": block, "sway": sway}
        if frames:
            e["frames"] = frames
        if extra:
            e.update(extra)
        entries.append(e)

    sprites = {}
    for (w, h, tag) in ((16, 16, "16x16"), (24, 20, "24x20")):
        for v in range(4):
            frs = []
            for fr in (0, 1):
                nm = f"grass_tuft_{tag}_{'abcd'[v]}_f{fr}"
                L = make_grass_tuft(w, h, v, fr, f"grass_tuft_{tag}")
                sprites[nm] = L
                save(nm, L, [1, 1], False, True)
                frs.append(nm + ".png")
            entries[-2]["frames"] = frs
            entries[-1]["frames"] = frs
    for v in range(4):
        frs = []
        for fr in (0, 1):
            nm = f"flame_flower_{'abcd'[v]}_f{fr}"
            L = make_flame_flower(v, fr)
            sprites[nm] = L
            save(nm, L, [1, 1], False, True)
            frs.append(nm + ".png")
        entries[-2]["frames"] = frs
        entries[-1]["frames"] = frs
    for kind in ("purple", "white"):
        for v in range(4):
            nm = f"flower_{kind}_{'abcd'[v]}"
            L = make_flower_cluster(kind, v)
            sprites[nm] = L
            save(nm, L, [1, 1], False, True)
    for kind in ("round", "dark"):
        for v in range(4):
            nm = f"shrub_{kind}_{'abcd'[v]}"
            L = make_shrub(kind, v)
            sprites[nm] = L
            save(nm, L, [1, 1], True)
    for v in range(4):
        nm = f"conifer_small_{'abcd'[v]}"
        L = make_conifer(24, 40, 4, v, "conifer_small", 0.9 + 0.1 * (v % 2))
        sprites[nm] = L
        save(nm, L, [1, 1], True)
    for v in range(4):
        nm = f"conifer_tall_{'abcd'[v]}"
        L = make_conifer(32, 64, 6 + (v % 2), v, "conifer_tall", 0.9 + 0.1 * (v % 2))
        sprites[nm] = L
        save(nm, L, [1, 1], True)
    for kind, fp, blk in (("s", [1, 1], False), ("m", [1, 1], True), ("l", [2, 1], True)):
        for v in range(4):
            nm = f"boulder_{kind}_{'abcd'[v]}"
            L = make_boulder(kind, v)
            sprites[nm] = L
            save(nm, L, fp, blk)
    for v in range(3):
        nm = f"ruin_pillar_{'abc'[v]}"; L = make_pillar(v); sprites[nm] = L; save(nm, L, [1, 1], True)
    for v in range(3):
        nm = f"ruin_cairn_{'abc'[v]}"; L = make_cairn(v); sprites[nm] = L; save(nm, L, [1, 1], True)
    for v in range(3):
        nm = f"ruin_wall_{'abc'[v]}"; L = make_wall(v); sprites[nm] = L; save(nm, L, [2, 1], True)
    for v in range(3):
        nm = f"ruin_arch_fragment_{'abc'[v]}"; L = make_arch(v); sprites[nm] = L; save(nm, L, [2, 1], True)
    for v in range(3):
        nm = f"stepping_stone_{'abc'[v]}"; L = make_stepping_stone(v); sprites[nm] = L; save(nm, L, [1, 1], False)
    for v in range(3):
        nm = f"fence_wood_post_{'abc'[v]}"; L = make_post(v); sprites[nm] = L
        save(nm, L, [1, 1], True, extra={"note": "stand on tile corner; pivot is the post foot"})
    for v in range(3):
        nm = f"fence_wood_rail_h_{'abc'[v]}"; L = make_rail(v); sprites[nm] = L
        save(nm, L, [1, 1], True, extra={"note": "spans one tile east-west between two posts; b/c = damaged"})
    nm = "fence_wood_rail_v_a"; L = make_rail_v(0); sprites[nm] = L
    save(nm, L, [1, 1], True, extra={"note": "vertical (north-south) run of one tile; bottom end at pivot"})
    for kind in ("s", "m", "l"):
        nm = f"shadow_ellipse_{kind}"; L = make_shadow(kind); sprites[nm] = L; save(nm, L, [1, 1], False)
    import gen_decor_extra   # rich-map families (grass clumps, props, statues, trees, ...)
    gen_decor_extra.build_extra(save, sprites)
    (out / "decor.json").write_text(json.dumps({"tile_px": 32, "ppu": 32, "light": "upper-left",
                                                "sprites": entries}, indent=2), encoding="utf-8")
    return sprites


# ---------------------------------------------------------------- preview scene
def preview(out, sp):
    W, H = 320, 200
    rng = np.random.default_rng(7)
    mead = Image.open(TERRAIN / "terrain_meadow.png").convert("RGBA")
    path = Image.open(TERRAIN / "terrain_path.png").convert("RGBA")
    canvas = Image.new("RGBA", (W, H))
    pm = np.zeros((H, W), dtype=int)
    ys, xs = np.mgrid[0:H, 0:W]
    cy = 128 + 14 * np.sin(xs / 42.0) + 0.04 * (xs - 160)
    half = 9 + 2 * np.sin(xs / 17.0)
    dd = np.abs(ys - cy)
    for ty in range((H + 31) // 32):
        for tx in range((W + 31) // 32):
            vi = int(rng.integers(0, 4))
            canvas.alpha_composite(mead.crop((vi * 32, 0, vi * 32 + 32, 32)), (tx * 32, ty * 32))
    pc = Image.new("RGBA", (W, H))
    for ty in range((H + 31) // 32):
        for tx in range((W + 31) // 32):
            vi = int(rng.integers(0, 4))
            pc.alpha_composite(path.crop((vi * 32, 0, vi * 32 + 32, 32)), (tx * 32, ty * 32))
    ca, pa = np.array(canvas), np.array(pc)
    inside = dd < half
    lip = (dd >= half) & (dd < half + 1)
    ca[inside] = pa[inside]
    ca[lip] = (107, 74, 46, 255)
    canvas = Image.fromarray(ca, "RGBA")
    items = []   # (name, x_foot_centre, y_foot)

    def P(name, x, y):
        items.append((name, x, y))
    # backdrop forest
    for i, x in enumerate(range(10, 320, 38)):
        P(f"conifer_tall_{'abcd'[i % 4]}", x + int(rng.integers(-5, 6)), 44 + int(rng.integers(-3, 8)))
    for i, x in enumerate(range(28, 320, 52)):
        P(f"conifer_small_{'abcd'[i % 4]}", x + int(rng.integers(-6, 6)), 64 + int(rng.integers(-3, 6)))
    # ruin cluster left
    P("ruin_wall_a", 56, 96); P("ruin_pillar_a", 20, 100); P("ruin_cairn_b", 104, 98)
    P("boulder_l_a", 150, 88); P("ruin_arch_fragment_b", 232, 98); P("ruin_pillar_c", 280, 86)
    P("shrub_round_a", 84, 84); P("shrub_dark_b", 196, 84); P("shrub_round_c", 262, 100)
    P("boulder_m_b", 120, 84); P("boulder_s_a", 180, 98)
    # lower-left rocks and shrubs
    P("boulder_m_c", 30, 176); P("shrub_dark_a", 60, 186); P("boulder_s_c", 96, 182)
    P("conifer_small_c", 206, 168); P("conifer_small_a", 16, 150); P("shrub_round_d", 220, 188)
    P("boulder_l_c", 130, 190)
    # stepping stones across path
    for k, x in enumerate((176, 194, 206)):
        P(f"stepping_stone_{'abc'[k]}", x, int(128 + 14 * np.sin(x / 42.0) + 0.04 * (x - 160) + 3 + k))
    # fence pasture lower-right
    fx0, fy0, fx1, fy1 = 238, 150, 302, 182
    for x in range(fx0, fx1, 32):
        P(f"fence_wood_rail_h_{'abc'[(x // 32) % 3]}", x + 16, fy0 + 2)
        P(f"fence_wood_rail_h_{'abc'[(x // 32 + 1) % 3]}", x + 16, fy1 + 2)
    P("fence_wood_rail_v_a", fx0, fy1 - 0); P("fence_wood_rail_v_a", fx1, fy1 - 0)
    for x in range(fx0, fx1 + 1, 32):
        P("fence_wood_post_a" if x != fx1 else "fence_wood_post_b", x, fy0 + 4)
        P("fence_wood_post_c" if x == fx0 else "fence_wood_post_a", x, fy1 + 4)
    # flowers, flame flowers and grass tufts scattered, avoiding the path
    for _ in range(260):
        x, y = int(rng.integers(6, W - 6)), int(rng.integers(20, H - 2))
        c = 128 + 14 * np.sin(x / 42.0) + 0.04 * (x - 160)
        if abs(y - c) < 14 and rng.random() < 0.95:
            continue
        if fx0 - 4 < x < fx1 + 4 and fy0 - 2 < y < fy1 + 8:
            continue
        r = rng.random()
        if r < 0.50:
            P(f"grass_tuft_{'16x16' if rng.random() < .55 else '24x20'}_{'abcd'[int(rng.integers(0, 4))]}_f0", x, y)
        elif r < 0.62:
            P(f"flame_flower_{'abcd'[int(rng.integers(0, 4))]}_f{int(rng.integers(0, 2))}", x, y)
        elif r < 0.82:
            P(f"flower_{'purple' if rng.random() < .5 else 'white'}_{'abcd'[int(rng.integers(0, 4))]}", x, y)
        else:
            P(f"grass_tuft_16x16_{'abcd'[int(rng.integers(0, 4))]}_f1", x, y)
    shadow_for = {"conifer_tall": "m", "conifer_small": "m", "boulder_l": "l", "boulder_m": "m", "boulder_s": "s",
                  "ruin_wall": "l", "ruin_pillar": "m", "ruin_cairn": "m", "ruin_arch_fragment": "l",
                  "shrub_round": "m", "shrub_dark": "m", "fence_wood_post": "s"}
    for name, x, y in items:
        key = next((k for k in shadow_for if name.startswith(k)), None)
        if key:
            s = sp[f"shadow_ellipse_{shadow_for[key]}"].image()
            canvas.alpha_composite(s, (x - s.width // 2 + 1, y - s.height + 3)) if 0 <= x < W and 0 <= y < H + 10 else None
    for name, x, y in sorted(items, key=lambda t: t[2]):
        im = sp[name].image()
        px, py = x - im.width // 2, y - im.height + 1
        if px < -im.width or px > W or py < -im.height or py > H:
            continue
        # clip manually so partially visible sprites work
        tmp = Image.new("RGBA", (W, H))
        tmp.paste(im, (px, py))
        canvas.alpha_composite(tmp)
    canvas.resize((W * 3, H * 3), Image.NEAREST).save(out / "preview_decor_x3.png")


def sheet(out, sp):
    """Debug contact sheet (not part of the deliverable list, but handy for review)."""
    names = [n for n in sp if not n.startswith("shadow") and not n.endswith("_f1")]
    mead = Image.open(TERRAIN / "terrain_meadow.png").convert("RGBA").crop((0, 0, 32, 32))
    cw = 100
    cols = 14
    rows = (len(names) + cols - 1) // cols
    im = Image.new("RGBA", (cols * cw, rows * 124))
    for ty in range(rows * 124 // 32 + 1):
        for tx in range(cols * cw // 32 + 1):
            im.alpha_composite(mead, (tx * 32, ty * 32))
    for i, n in enumerate(names):
        s = sp[n].image()
        r, c = divmod(i, cols)
        im.alpha_composite(s, (c * cw + (cw - s.width) // 2, r * 124 + 122 - s.height))
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=str(OUT_DEFAULT))
    ap.add_argument("--sheet", default="", help="optional debug contact sheet path (x3)")
    a = ap.parse_args()
    out = Path(a.out)
    sp = build(out)
    preview(out, sp)
    ex = {k: v for k, v in sp.items() if k.split("_f")[0] not in ()}
    sh = sheet(out, ex)
    sh.resize((sh.width * 2, sh.height * 2), Image.NEAREST).save(out / "preview_decor_all_x2.png")
    if a.sheet:
        s = sheet(out, sp)
        s.resize((s.width * 3, s.height * 3), Image.NEAREST).save(a.sheet)
    print("decor ->", out, len(sp), "sprites")


if __name__ == "__main__":
    main()
