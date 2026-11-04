"""Extra decor families for the rich-map pass (called from gen_decor.build; never run on its own).

Same method as gen_decor: tiny height fields lit from the upper left, quantised ramps, selective outlines, moss /
gold-leaf decals, no anti-aliasing, deterministic seeds. East-West mythic mood: weathered statues, sun-disc steles,
banners; lived-in farm props; forest glow flowers, ferns, mist.
"""
import numpy as np

import gen_decor as G
from gen_decor import (Spr, ramp, hx, mix, dk, shade, vnoise, outline_inner, outline_outer, seeded, ground_blades,
                       WOOD, STONE, MOSS, GOLD_L, FLAME, LEAF, GRASS_F, GRASS_B, CON, CON_RIM)

HAY = ramp("#7a5a1e", "#a8802c", "#d0a640", "#ecc860", "#f8e08c", "#fff2b8")
RED = ramp("#4a1410", "#7a1f18", "#a8321f", "#cf5030", "#e8804a")
BLUE = ramp("#142040", "#1f3566", "#2f5190", "#4a74b8", "#7aa0d6")
GREEN = ramp("#1a3018", "#2a4a22", "#42682e", "#648a40", "#8aaa58")
IRON = ramp("#1c1a1c", "#34323a", "#524f58", "#7a7682")
GLOW = ramp("#0f4a5c", "#1f8aa0", "#4cc8d8", "#9af0f0", "#e8ffff")
AUTUMN = ramp("#4a2410", "#7a3414", "#b0541a", "#d87a24", "#f0a436", "#fcd26a", "#fff0a8")
AUTUMN2 = ramp("#2e2c10", "#4c4414", "#7a6a1c", "#a89028", "#d4b43c", "#f0d462", "#fff0a8")
BARK = ramp("#2a1a10", "#46301c", "#684a2a", "#8a6a3c", "#b08c52")
CLAY = ramp("#4a2414", "#7a3e22", "#a85c32", "#cc8048", "#e8a868")
SACK = ramp("#6a5230", "#907048", "#b89864", "#d8bc88", "#f0dcaa")
WATER = ramp("#1f4466", "#2b5d86", "#3a78a6", "#5b9cc6", "#a9d6e6", "#ffffff")


# ---------------------------------------------------------------- primitives
def fill(L, inside, z, pal, dither=0.18, gain=1.1, lo=-0.1, hi=1.05, bias=0.0):
    idx, _, _ = shade(np.where(inside, z, 0), len(pal), dither, gain, lo, hi, bias)
    for y, x in zip(*np.nonzero(inside)):
        L.put(x, y, pal[int(idx[y, x])])


def box(L, x0, y0, w, hf, dt, fr, tp, planks=0):
    """3/4-view box: top face dt rows, front face hf rows; lit left, shaded right."""
    for y in range(dt):
        for x in range(w):
            c = tp[4] if (x < 2 or y == 0) else (tp[2] if x >= w - 2 else tp[3])
            if planks and (x % planks == planks - 1):
                c = tp[2]
            L.put(x0 + x, y0 + y, c)
    for y in range(hf):
        for x in range(w):
            c = fr[3] if x < 2 else (fr[1] if x >= w - 2 else fr[2])
            if y == hf - 1:
                c = fr[0]
            if planks and (x % planks == planks - 1) and 0 < x < w - 2:
                c = fr[1]
            L.put(x0 + x, y0 + dt + y, c)


def ellipse_mask(w, h, cx, cy, rx, ry):
    ys, xs = np.mgrid[0:h, 0:w]
    return ((xs + 0.5 - cx) / rx) ** 2 + ((ys + 0.5 - cy) / ry) ** 2 <= 1.0


def lumps_field(w, h, lumps, hbias=0.12):
    ys, xs = np.mgrid[0:h, 0:w]
    zz = np.full((h, w), -1.0)
    for lx, ly, lr in lumps:
        nx, ny = (xs + 0.5 - lx) / lr, (ys + 0.5 - ly) / (lr * 0.9)
        d = 1 - nx * nx - ny * ny
        z = np.where(d > 0, np.sqrt(np.clip(d, 0, 1)) * lr * 0.9 + (h - ly) * hbias, -1)
        zz = np.maximum(zz, z)
    return zz


def moss_dots(L, rng, n, x0, x1, y0, y1, only_on=True):
    for _ in range(n):
        x, y = int(rng.integers(x0, x1)), int(rng.integers(y0, y1))
        if not only_on or L.get(x, y):
            L.put(x, y, MOSS[2 if rng.random() < 0.6 else 1])
            if rng.random() < 0.5 and (not only_on or L.get(x + 1, y)):
                L.put(x + 1, y, MOSS[3])


def gold_flecks(L, rng, n, x0, x1, y0, y1):
    for _ in range(n):
        x, y = int(rng.integers(x0, x1)), int(rng.integers(y0, y1))
        if L.get(x, y):
            L.put(x, y, GOLD_L[3 if rng.random() < 0.5 else 2])


def halo(L, cx, cy, r, pal, pa=0.5, rng=None):
    """soft glow: concentric alpha pixels (additive look via bright colour)."""
    for y in range(int(cy - r), int(cy + r) + 1):
        for x in range(int(cx - r), int(cx + r) + 1):
            d = np.hypot(x - cx, y - cy) / r
            if d < 1 and not L.get(x, y):
                a = int(255 * pa * (1 - d) ** 1.6)
                if (x + y) % 2 == 0 or d < 0.5:
                    L.put(x, y, pal, a)


# ---------------------------------------------------------------- flora
def tall_grass(w, h, v, f):
    return G.make_grass_tuft(w, h, v, f, f"grass_tuft_{w}x{h}")


def wildflowers(v):
    rng = seeded("wildflowers", v)
    L = Spr(34, 18)
    cols = [("#9a6ab8", "#e2c8ee"), ("#fffaf0", "#ffffff"), ("#e2b44c", "#fff0b4"), ("#d8693a", "#f8bf48"), ("#c94f6a", "#f2a0b0")]
    pts = []
    for _ in range(11):
        pts.append((int(rng.integers(2, 32)), int(rng.integers(3, 13))))
    pts.sort(key=lambda p: p[1])
    for (fx, fy) in pts:
        for y in range(fy + 1, 17):
            L.put(fx, y, LEAF[2] if (y + fx) % 2 else LEAF[1])
    for x in range(1, 33):
        if rng.random() < 0.85:
            L.put(x, 17, LEAF[1])
        if rng.random() < 0.55:
            L.put(x, 16, LEAF[2])
        if rng.random() < 0.25:
            L.put(x, 15, LEAF[3])
    for (fx, fy) in pts:
        c0, c1 = cols[int(rng.integers(len(cols)))]
        c0, c1 = hx(c0), hx(c1)
        L.put(fx, fy, hx("#e2b44c") if c0 != hx("#e2b44c") else hx("#a5782a"))
        for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
            L.put(fx + dx, fy + dy, c1 if (dx < 0 or dy < 0) else c0)
    outline_outer(L, 0.5)
    return L


def reeds(v, f):
    rng = seeded("reeds", v)
    w, h = 22, 44
    L = Spr(w, h)
    sw = 1.4 if f else -0.6
    n = int(rng.integers(7, 11))
    stems = sorted([(float(rng.normal(0, 4.2)), 20 + rng.random() * 20, i) for i in range(n)], key=lambda s: s[1])
    for off, hgt, i in stems:
        x0 = w / 2 + off
        lean = off / 12.0
        pal = GRASS_B if i % 3 == 0 else (GREEN if i % 2 else LEAF)
        cat = (i % 3 == 1)
        last = None
        for s in range(int(hgt * 2)):
            t = s / (hgt * 2)
            x = x0 + lean * t * hgt * 0.7 + sw * t * t * (1.0 + hgt / 25)
            y = h - 3 - t * hgt
            L.put(round(x), round(y), pal[min(len(pal) - 1, 1 + int(t * 3))])
            if t < 0.5 and not cat:
                L.put(round(x) + 1, round(y), pal[1])
            last = (round(x), round(y))
        if cat and last:
            lx, ly = last
            for k in range(5):                                   # cattail capsule
                L.put(lx, ly - k, hx("#5e3d22"))
                L.put(lx + 1, ly - k, hx("#7f5a32"))
            L.put(lx, ly - 5, hx("#d9b070"))
            L.put(lx - 1, ly - 1, hx("#a47a45"))
    for x in range(int(w / 2 - 6), int(w / 2 + 7)):
        if rng.random() < 0.6:
            L.put(x, h - 2, GRASS_B[0])
    outline_outer(L, 0.45)
    return L


def lilypad(v):
    rng = seeded("lilypad", v)
    L = Spr(20, 10)
    for k, (cx, cy, rx, ry) in enumerate(((6, 5, 5, 3), (14, 6, 4, 2.4)) if v < 2 else ((10, 5, 6, 3.4),)):
        m = ellipse_mask(20, 10, cx, cy, rx, ry)
        for y, x in zip(*np.nonzero(m)):
            L.put(x, y, GREEN[3] if (x + y) % 5 else GREEN[2])
        for x in range(int(cx), int(cx + rx) + 1):
            L.put(x, int(cy), WATER[1])                           # notch
        for y, x in zip(*np.nonzero(m & ~np.roll(m, 1, 0))):
            L.put(x, y, GREEN[4])
        for y, x in zip(*np.nonzero(m & ~np.roll(m, -1, 0))):
            L.put(x, y, GREEN[1])
    if v == 1:
        for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1)):
            L.put(14 + dx, 4 + dy, hx("#f2a0b0") if dy < 0 or dx < 0 else hx("#c94f6a"))
        L.put(14, 4, hx("#fde48a"))
    return L


def fern(v):
    rng = seeded("fern", v)
    L = Spr(30, 22)
    cx, base = 15, 19
    for k in range(9):
        ang = -1.45 + k * 0.36 + rng.normal(0, 0.06)
        ln = 9 + rng.random() * 5
        for s in range(int(ln * 2.2)):
            t = s / (ln * 2.2)
            x = cx + np.cos(ang + 3.14159) * 0 + np.sin(ang) * t * ln
            y = base - np.cos(ang) * t * ln * 0.85 + t * t * ln * 0.55
            L.put(round(x), round(y), LEAF[2 if t < 0.7 else 3])
            if s % 2 == 0 and 0.15 < t < 0.95:
                for d in (-1, 1):                                # pinnae
                    L.put(round(x + d), round(y + 1), LEAF[1 if d < 0 else 2])
    outline_outer(L, 0.5)
    return L


def glow_flower(v, f):
    rng = seeded("glow_flower", v)
    w, h = 22, 30
    L = Spr(w, h)
    sw = 1.0 if f else 0.0
    cx = 11
    for y in range(14, h - 1):
        t = (h - 2 - y) / (h - 16)
        L.put(round(cx + sw * t * t), y, LEAF[2])
        L.put(round(cx + sw * t * t) + 1, y, LEAF[1])
    for side in (-1, 1):
        for s in range(6):
            L.put(cx + side * (1 + s), h - 3 - s // 2 + (s // 4), LEAF[3 if side < 0 else 2])
    hx0, hy0 = cx + int(sw), 12
    halo(L, hx0, hy0, 9.5, hx("#9af0f0"), 0.55)
    for a in (-1.0, -0.5, 0.0, 0.5, 1.0):
        for s in range(5):
            x = hx0 + np.sin(a) * (s * 1.2)
            y = hy0 - np.cos(a) * (s * 1.4) + 2
            for o in (-1, 0, 1):
                L.put(round(x + o * np.cos(a)), round(y), GLOW[min(4, 2 + (s > 2) + (o == 0))] if abs(o) < 2 else GLOW[1])
    L.put(hx0, hy0, GLOW[4]); L.put(hx0, hy0 + 1, GLOW[4]); L.put(hx0 - 1, hy0 + 1, GLOW[3])
    for _ in range(3):
        L.put(int(hx0 + rng.integers(-8, 9)), int(rng.integers(0, 6)), GLOW[3])
    return L


def glow_mushroom(v):
    rng = seeded("glow_mushroom", v)
    L = Spr(24, 16)
    halo(L, 12, 9, 11, hx("#7ae0e8"), 0.4)
    for (cx, cy, r) in ((7, 11, 4), (14, 9, 5), (19, 12, 3)):
        for y in range(cy + 4, 15):
            L.put(cx, y, hx("#d8d0c0")); L.put(cx + 1, y, hx("#a8a090"))
        for y in range(cy - r, cy + 1):
            hw = int(np.sqrt(max(0, r * r - (y - cy) ** 2)) * 1.3)
            for x in range(cx - hw, cx + hw + 1):
                L.put(x, y, GLOW[3] if x < cx else GLOW[2])
                if (x + y) % 5 == 0:
                    L.put(x, y, GLOW[4])
        L.put(cx - 1, cy - r + 1, GLOW[4])
    outline_outer(L, 0.5)
    return L


def mist(v):
    rng = seeded("mist", v)
    w, h = 96, 40
    L = Spr(w, h)
    n = vnoise(w, h, 7, rng) * 0.6 + vnoise(w, h, 3, rng) * 0.4
    ys, xs = np.mgrid[0:h, 0:w]
    d = np.hypot((xs - w / 2) / (w / 2), (ys - h / 2) / (h / 2))
    a = np.clip((1 - d) * 1.6, 0, 1) * (0.35 + 0.9 * n)
    bay = G.BAYER[ys % 4, xs % 4]
    for y in range(h):
        for x in range(w):
            q = a[y, x] * 0.8
            if q > 0.04 and q > bay[y, x] * 0.3:
                L.put(x, y, hx("#fff6dc"), int(min(255, q * 255)))
    return L


# ---------------------------------------------------------------- natural props
def log_fallen(v):
    rng = seeded("log", v)
    w, h = 58, 22
    L = Spr(w, h)
    r = 7.5 + (v % 2)
    cy = h - 3 - r
    x0, x1 = 6, w - 6
    ys, xs = np.mgrid[0:h, 0:w]
    inside = (xs >= x0) & (xs <= x1) & (np.abs(ys + 0.5 - cy) <= r)
    # slight bend / taper
    z = np.sqrt(np.clip(1 - ((ys + 0.5 - cy) / r) ** 2, 0, 1)) * r
    idx, _, _ = shade(np.where(inside, z, 0), len(BARK), 0.15, 1.0, -0.1, 1.05)
    gl = vnoise(w, h, 2, rng)
    for y, x in zip(*np.nonzero(inside)):
        i = int(idx[y, x])
        if gl[y, x] > 0.72:
            i = max(0, i - 1)
        if (x + y // 2) % 6 == 0 and rng.random() < 0.6:
            i = max(0, i - 1)                                    # bark furrows
        L.put(x, y, BARK[i])
    # cut end (left): wood rings
    ex = x0
    for y in range(int(cy - r), int(cy + r) + 1):
        for x in range(ex - 3, ex + 4):
            dd = ((x - ex) / 3.4) ** 2 + ((y - cy) / r) ** 2
            if dd <= 1:
                ring = int(np.sqrt(dd) * 4.5)
                L.put(x, y, WOOD[4 if ring % 2 == 0 else 3] if ring < 4 else WOOD[2])
    L.put(ex, int(cy), WOOD[1])
    # branch stub + moss on top, knots
    for k in range(3):
        L.put(x0 + 18 + k, int(cy - r) - 1 - k // 2, BARK[2]); L.put(x0 + 19 + k, int(cy - r) - 1 - k // 2, BARK[1])
    for _ in range(26):
        x = int(rng.integers(x0 + 6, x1))
        y = int(cy - r + rng.integers(0, 4))
        if L.get(x, y):
            L.put(x, y, MOSS[2 if rng.random() < 0.6 else 3])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 14, 3, w - 4, h - 1, 3)
    return L


def stump(v):
    rng = seeded("stump", v)
    w, h = 28, 24
    L = Spr(w, h)
    cx, r = 14, 8
    top = 7
    for y in range(top, h - 2):                                   # trunk
        flare = 2.5 * max(0, (y - (h - 8)) / 6.0)
        for x in range(int(cx - r - flare), int(cx + r + flare) + 1):
            t = (x - cx) / (r + flare)
            i = 3 if t < -0.45 else (2 if t < 0.2 else (1 if t < 0.65 else 0))
            if (x + y) % 5 == 0:
                i = max(0, i - 1)
            L.put(x, y, BARK[i])
    for y in range(top - 3, top + 4):                             # cut top
        for x in range(cx - r - 1, cx + r + 2):
            dd = ((x - cx) / (r + 1)) ** 2 + ((y - top) / 3.3) ** 2
            if dd <= 1:
                ring = int(np.sqrt(dd) * 5)
                L.put(x, y, WOOD[4 if ring % 2 == 0 else 3] if ring < 4 else WOOD[2])
    L.put(cx, top, WOOD[1])
    for _ in range(8):
        x, y = int(rng.integers(cx - r, cx + r)), int(rng.integers(top + 3, top + 9))
        if L.get(x, y):
            L.put(x, y, MOSS[2])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 8, 2, w - 3, h - 1, 3)
    return L


def woodpile(v):
    rng = seeded("woodpile", v)
    w, h = 36, 24
    L = Spr(w, h)
    rows = [(10, 5, 4), (11, 4, 3), (12, 3, 2)] if v == 0 else [(9, 6, 4), (10, 5, 3), (12, 3, 2)]
    y = h - 4
    for (cy_off, n, _) in rows:
        for i in range(n):
            cx = int(w / 2 + (i - (n - 1) / 2) * 6.2)
            for yy in range(-3, 4):
                for xx in range(-3, 4):
                    dd = np.hypot(xx, yy)
                    if dd <= 3.2:
                        ring = int(dd)
                        L.put(cx + xx, y + yy, WOOD[4 if ring == 0 else (3 if ring == 1 else (2 if ring == 2 else 1))] if dd < 3 else BARK[1])
        y -= 5
    outline_outer(L, 0.45)
    ground_blades(L, rng, 6, 3, w - 4, h - 1, 2)
    return L


def hay_bale_sq(v):
    rng = seeded("hay_sq", v)
    w, h = 28, 20
    L = Spr(w, h)
    box(L, 1, 3, 26, 12, 5, HAY, HAY)
    for _ in range(70):                                           # straw strands
        x, y = int(rng.integers(2, 26)), int(rng.integers(3, 20))
        for k in range(int(rng.integers(2, 5))):
            if L.get(x + k, y):
                L.put(x + k, y, HAY[5 if rng.random() < 0.4 else 1])
    for bx in (8, 19):                                            # twine bands
        for y in range(3, 17):
            L.put(bx, y, hx("#6a4a22")); L.put(bx + 1, y, hx("#8a6a34"))
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 6, 1, w - 2, h - 1, 2)
    return L


def hay_bale_round(v):
    rng = seeded("hay_rd", v)
    w, h = 28, 26
    L = Spr(w, h)
    cx, cy, r = 13, 12, 11
    for y in range(h):
        for x in range(w):
            dd = np.hypot(x + 0.5 - cx, y + 0.5 - cy)
            if dd <= r:
                ring = int(dd * 0.9)
                i = 4 if ring % 3 == 0 else (3 if ring % 3 == 1 else 2)
                if x + y < cx + cy - 5:
                    i = min(5, i + 1)
                if x + y > cx + cy + 6:
                    i = max(0, i - 1)
                L.put(x, y, HAY[i])
    for y in range(cy, cy + r + 1):                                # cylinder side to the right
        for x in range(cx + r - 2, cx + r + 3):
            if np.hypot(x - cx - 2, y - cy) <= r:
                L.put(x, y, HAY[1])
    for _ in range(40):
        a = rng.random() * 6.28
        rr = rng.random() * r
        L.put(cx + np.cos(a) * rr, cy + np.sin(a) * rr, HAY[5 if rng.random() < 0.5 else 1])
    for y in range(cy - r, cy + r):                                # twine
        L.put(cx + 4, y, hx("#6a4a22")) if L.get(cx + 4, y) else None
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 6, 2, w - 3, h - 1, 2)
    return L


def hay_pile(v):
    rng = seeded("hay_pile", v)
    w, h = 46, 30
    L = Spr(w, h)
    cx, cy = w / 2, h - 11
    ys, xs = np.mgrid[0:h, 0:w]
    zz = np.zeros((h, w))
    for (lx, ly, lr) in ((cx, cy + 1, 15), (cx - 10, cy + 4, 9), (cx + 10, cy + 3, 10), (cx + 1, cy - 4, 9)):
        d = 1 - ((xs + 0.5 - lx) / lr) ** 2 - ((ys + 0.5 - ly) / (lr * 0.62)) ** 2
        zz = np.maximum(zz, np.where(d > 0, np.sqrt(np.clip(d, 0, 1)) * lr * 0.9, 0))
    inside = zz > 0.3
    fill(L, inside, zz, HAY, dither=0.3, gain=0.9, lo=-0.25, hi=0.95, bias=0.1)
    for _ in range(160):
        x, y = int(rng.integers(0, w)), int(rng.integers(0, h))
        if L.get(x, y):
            for k in range(int(rng.integers(2, 6))):
                if L.get(x + k, y - k // 3):
                    L.put(x + k, y - k // 3, HAY[5 if rng.random() < 0.45 else 1])
    for _ in range(14):                                            # loose straws sticking out
        x, y = int(rng.integers(8, w - 8)), int(rng.integers(h - 20, h - 8))
        if L.get(x, y + 1) and not L.get(x, y):
            L.put(x, y, HAY[4]); L.put(x + 1, y - 1, HAY[5])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 10, 2, w - 3, h - 1, 3)
    return L


def crate(v):
    rng = seeded("crate", v)
    w, h = 24, 24
    L = Spr(w, h)
    box(L, 1, 2, 22, 16, 6, WOOD, WOOD, planks=5)
    for x in range(1, 23):                                         # front frame boards
        for yy in (8, 9, 22 - 4, 22 - 3):
            L.put(x, yy, WOOD[3] if x < 3 else WOOD[2])
    for y in range(8, 21):                                         # diagonal brace
        x = int(2 + (y - 8) * 20 / 13)
        for o in (0, 1):
            L.put(x + o, y, WOOD[3])
    for (nx, ny) in ((3, 9), (20, 9), (3, 19), (20, 19)):
        L.put(nx, ny, IRON[1])
    if v == 1:                                                     # apples on top
        for _ in range(7):
            x, y = int(rng.integers(4, 20)), int(rng.integers(2, 6))
            L.put(x, y, RED[3]); L.put(x + 1, y, RED[2]); L.put(x, y - 1, RED[4])
    elif v == 2:                                                   # grain heap
        for _ in range(60):
            x, y = int(rng.integers(3, 21)), int(rng.integers(1, 6))
            L.put(x, y, HAY[int(rng.integers(2, 6))])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def crate_stack(v):
    L = Spr(34, 40)
    a, b, c = crate(0), crate(1), crate(2)
    L.blit(a, 0, 16)
    L.blit(b, 10, 17)
    L.blit(c, 5 if v == 0 else 6, 0 + (0 if v == 0 else 1))
    return L


def barrel(v):
    rng = seeded("barrel", v)
    w, h = 20, 26
    L = Spr(w, h)
    cx, r0 = 10, 7.4
    for y in range(4, h - 2):
        t = (y - 4) / (h - 7)
        r = r0 + 1.5 * np.sin(np.pi * t)
        for x in range(int(cx - r), int(cx + r) + 1):
            tt = (x + 0.5 - cx) / r
            i = 3 if tt < -0.45 else (2 if tt < 0.1 else (1 if tt < 0.6 else 0))
            if int((x - cx + 20) % 4) == 0:
                i = max(0, i - 1)
            L.put(x, y, WOOD[i])
    for hy in (7, 11, h - 8, h - 5):                               # iron hoops
        t = (hy - 4) / (h - 7)
        r = r0 + 1.5 * np.sin(np.pi * t)
        for x in range(int(cx - r), int(cx + r) + 1):
            L.put(x, hy, IRON[3 if x < cx else 1])
            L.put(x, hy + 1, IRON[1])
    for y in range(1, 8):                                          # top ellipse
        for x in range(cx - 8, cx + 9):
            dd = ((x + 0.5 - cx) / 7.6) ** 2 + ((y - 4.0) / 3.0) ** 2
            if dd <= 1:
                L.put(x, y, WOOD[4] if dd < 0.8 else WOOD[2])
    for x in range(cx - 5, cx + 5):
        L.put(x, 4, WOOD[3] if x % 3 else WOOD[2])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def sack(v):
    rng = seeded("sack", v)
    w, h = 18, 18
    L = Spr(w, h)
    cx = 9
    lumps = [(cx, 11, 7.0), (cx - 3, 12, 5), (cx + 3, 12, 5), (cx, 5, 3.0)]
    zz = lumps_field(w, h, lumps, 0.0)
    inside = (zz > 0.2) & (np.mgrid[0:h, 0:w][0] < h - 1)
    fill(L, inside, zz, SACK, dither=0.22, gain=1.1)
    for x in range(cx - 2, cx + 3):                                # tie
        L.put(x, 7, hx("#6a4a22"))
    if v == 1:
        for _ in range(5):
            L.put(int(rng.integers(5, 14)), int(rng.integers(9, 15)), hx("#7a5a2c"))
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def sack_pile(v):
    L = Spr(34, 22)
    L.blit(sack(0), 0, 4); L.blit(sack(1), 16, 4); L.blit(sack(1), 8, -2 if v else 0)
    return L


def basket(v):
    rng = seeded("basket", v)
    w, h = 22, 16
    L = Spr(w, h)
    goods = [(RED, 3), (hx("#ee8f30"), None), (GREEN, 3)][v % 3]
    for y in range(6, 15):                                         # basket body
        for x in range(3 + (y - 6) // 4, 19 - (y - 6) // 4):
            L.put(x, y, WOOD[3] if (x + y) % 3 else WOOD[2])
    for x in range(2, 20):
        L.put(x, 6, WOOD[4]); L.put(x, 14, WOOD[1])
    for _ in range(9):
        x, y = int(rng.integers(4, 17)), int(rng.integers(2, 7))
        if v % 3 == 1:
            L.put(x, y, hx("#ee8f30")); L.put(x + 1, y, hx("#d0602a")); L.put(x, y - 1, hx("#fde48a"))
        else:
            pal = goods[0]
            L.put(x, y, pal[3]); L.put(x + 1, y, pal[2]); L.put(x, y - 1, pal[4])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def market_table(v):
    rng = seeded("mtable", v)
    w, h = 44, 26
    L = Spr(w, h)
    cloth = [RED, BLUE, GREEN][v % 3]
    for x in (4, 38):                                              # legs
        for y in range(12, h - 1):
            L.put(x, y, WOOD[2]); L.put(x + 1, y, WOOD[1])
    box(L, 1, 8, 42, 6, 4, cloth, cloth)
    for x in range(1, 43, 6):                                      # stripes
        for y in range(8, 18):
            L.put(x, y, GOLD_L[1] if y > 11 else GOLD_L[2])
    for y in range(18, 21):                                        # scalloped hem
        for x in range(1, 43):
            if (x // 3) % 2 == 0 or y < 19:
                L.put(x, y, cloth[2 if x % 6 else 1])
    for _ in range(11):                                            # goods on the table
        x, y = int(rng.integers(5, 38)), int(rng.integers(3, 9))
        k = int(rng.integers(0, 3))
        c = [RED[3], hx("#ee8f30"), GRASS_F[4]][k]
        L.put(x, y, c); L.put(x + 1, y, dk(c, 0.75)); L.put(x, y - 1, mix(c, (255, 255, 220), 0.4)); L.put(x + 1, y + 1, dk(c, 0.5))
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def flower_box(v):
    rng = seeded("flowerbox", v)
    w, h = 32, 18
    L = Spr(w, h)
    box(L, 1, 8, 30, 8, 2, WOOD, WOOD, planks=6)
    cols = [("#9a6ab8", "#e2c8ee"), ("#fffaf0", "#ffffff"), ("#e2b44c", "#fff0b4"), ("#d8693a", "#f8bf48"), ("#c94f6a", "#f2a0b0")]
    for x in range(2, 30):
        if rng.random() < 0.9:
            for y in range(6, 9):
                L.put(x, y, LEAF[2 if (x + y) % 2 else 1])
    for x in range(2, 30, 3):
        c0, c1 = cols[int(rng.integers(len(cols)))]
        fy = int(rng.integers(1, 5))
        L.put(x + 1, fy, hx(c1)); L.put(x, fy + 1, hx(c0)); L.put(x + 1, fy + 1, hx("#e2b44c")); L.put(x + 2, fy + 1, hx(c0)); L.put(x + 1, fy + 2, hx(c0))
        L.put(x + 1, fy + 3, LEAF[2])
    for x in range(2, 30, 5):                                      # trailing ivy
        L.put(x, 17, LEAF[2]); L.put(x, 16, LEAF[3])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def trough(v):
    rng = seeded("trough", v)
    w, h = 48, 20
    L = Spr(w, h)
    for x in (4, 42):
        for y in range(10, h - 1):
            L.put(x, y, WOOD[2]); L.put(x + 1, y, WOOD[1])
    box(L, 1, 4, 46, 8, 5, WOOD, WOOD, planks=0)
    for y in range(5, 8):                                          # feed in the trough
        for x in range(3, 45):
            L.put(x, y, HAY[int(rng.integers(2, 6))] if v == 0 else (SACK[int(rng.integers(1, 4))] if v == 1 else WATER[int(rng.integers(1, 4))]))
    for x in range(1, 47):
        L.put(x, 4, WOOD[4])
    for x in range(1, 47, 7):                                      # plank joints
        for y in range(9, 16):
            L.put(x, y, WOOD[1])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def scarecrow(v):
    rng = seeded("scarecrow", v)
    w, h = 34, 62
    L = Spr(w, h)
    cx = 17
    for y in range(18, h - 1):                                     # post
        L.put(cx, y, WOOD[3]); L.put(cx + 1, y, WOOD[1])
    for x in range(3, 32):                                         # cross arm
        L.put(x, 24, WOOD[3]); L.put(x, 25, WOOD[1])
    for y in range(22, 42):                                        # ragged shirt
        hw = 6 if y < 34 else 5 + (y - 34) // 3
        for x in range(cx - hw, cx + hw + 1):
            c = RED[3] if x < cx - 2 else (RED[2] if x < cx + 3 else RED[1])
            if (x + y) % 7 == 0:
                c = hx("#cf9a50")                                  # patch
            L.put(x, y, c)
    for y in range(24, 33):                                        # sleeves + straw ends
        for x in (3, 4, 5, 6, 7, 27, 28, 29, 30, 31):
            L.put(x, y if y < 28 else 24 + (y - 28) // 2, RED[2]) if y < 28 else None
    for sx in (1, 2, 32, 33):
        for k in range(4):
            L.put(sx, 24 + k, HAY[4 if k % 2 else 3])
    for k in range(5):
        L.put(cx - 3 + k, 43 + (k % 2), HAY[4])
    for y in range(11, 21):                                        # sack head
        for x in range(cx - 5, cx + 6):
            if np.hypot(x - cx, (y - 16) * 1.0) <= 5.2:
                L.put(x, y, SACK[3] if x < cx else SACK[2])
    L.put(cx - 2, 15, IRON[0]); L.put(cx + 2, 15, IRON[0]); L.put(cx - 1, 18, IRON[0]); L.put(cx, 18, IRON[0]); L.put(cx + 1, 18, IRON[0])
    for x in range(cx - 9, cx + 10):                               # hat brim + crown
        L.put(x, 11, WOOD[3] if x < cx else WOOD[2]); L.put(x, 12, WOOD[1])
    for y in range(3, 11):
        for x in range(cx - 4, cx + 5):
            L.put(x, y, WOOD[3] if x < cx else WOOD[2])
    for x in range(cx - 4, cx + 5):
        L.put(x, 9, RED[2])
    if v == 1:                                                     # a crow on the arm
        for dx, dy in ((0, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (2, 1), (-2, 0), (3, 0)):
            L.put(27 + dx, 21 + dy, IRON[0])
        L.put(26, 20, IRON[0]); L.put(25, 21, hx("#e2b44c"))
    outline_outer(L, 0.45)
    ground_blades(L, rng, 6, cx - 7, cx + 8, h - 1, 3)
    return L


def signpost(v):
    rng = seeded("signpost", v)
    w, h = 30, 50
    L = Spr(w, h)
    cx = 15
    for y in range(4, h - 1):
        L.put(cx, y, WOOD[3]); L.put(cx + 1, y, WOOD[2]); L.put(cx + 2, y, WOOD[1])
    for (y, dirn, ln) in ((7, 1, 11), (16, -1, 10)):
        for k in range(ln):
            x = cx + 1 + dirn * (k + 1)
            for yy in range(y, y + 5):
                L.put(x, yy, WOOD[4] if yy == y else (WOOD[3] if yy < y + 4 else WOOD[1]))
        tipx = cx + 1 + dirn * (ln + 1)
        for k in range(3):
            L.put(tipx + dirn * k, y + 2, WOOD[3]); L.put(tipx, y + 1 + (k % 2) * 2, WOOD[2])
        for k in range(2, ln - 2, 3):
            L.put(cx + 1 + dirn * k, y + 2, GOLD_L[2])
    for y in range(2, 5):
        for x in range(cx - 1, cx + 4):
            L.put(x, y, GOLD_L[2] if (x + y) % 2 else GOLD_L[1])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 8, cx - 6, cx + 8, h - 1, 3)
    return L


def conifer(w, h, ntiers, v, name):
    return G.make_conifer(w, h, ntiers, v, name, 0.92 + 0.08 * (v % 2))


def oak(v, kind=0):
    rng = seeded("oak%d" % kind, v)
    w, h = (88, 96) if kind == 0 else (64, 76)
    L = Spr(w, h)
    cx = w // 2
    pal = AUTUMN if (v % 2 == 0) else AUTUMN2
    tr_h = 26 if kind == 0 else 20
    gy = h - 2
    for y in range(gy - tr_h, gy + 1):                              # trunk with root flare
        t = (y - (gy - tr_h)) / tr_h
        hw = 3.6 + 3.2 * t ** 3 + (1.5 if t > 0.9 else 0)
        for x in range(int(cx - hw - 0.5), int(cx + hw + 1.5)):
            tt = (x - cx) / hw
            i = 3 if tt < -0.4 else (2 if tt < 0.1 else (1 if tt < 0.6 else 0))
            if rng.random() < 0.12:
                i = max(0, i - 1)
            L.put(x, y, BARK[i])
    for side in (-1, 1):                                            # two limbs into the crown
        for s in range(14):
            L.put(cx + side * (2 + s * 0.8), gy - tr_h - s, BARK[2 if side < 0 else 1])
            L.put(cx + side * (3 + s * 0.8), gy - tr_h - s, BARK[1])
    cw, ch = (36, 27) if kind == 0 else (26, 20)
    ccy = h - tr_h - ch * 0.75
    lumps = []
    for i in range(48 if kind == 0 else 30):
        a = rng.random() * 6.283
        rr = np.sqrt(rng.random()) * 0.9
        lumps.append((cx + np.cos(a) * rr * cw, ccy + np.sin(a) * rr * ch * 0.95, 7 + rng.random() * (6 if kind == 0 else 4)))
    lumps.sort(key=lambda t: t[1])
    zz = lumps_field(w, h, lumps, 0.18)
    ys, xs = np.mgrid[0:h, 0:w]
    inside = (zz > 0.2) & (ys < gy - tr_h + 8)
    fz = np.where(inside, zz, 0)
    idx, I, _ = shade(fz, len(pal), 0.30, 1.2, -0.2, 1.0, bias=0.05)
    gl = vnoise(w, h, 2, rng)
    for y, x in zip(*np.nonzero(inside)):
        i = int(idx[y, x])
        if gl[y, x] > 0.78 and i < len(pal) - 1:
            i += 1
        if gl[y, x] < 0.2 and i > 0:
            i -= 1
        if y > ccy + ch * 0.5:                                      # shaded underside
            i = max(0, i - 1)
        L.put(x, y, pal[i])
    for _ in range(50):                                             # gaps showing sky/dark between leaf clumps
        x, y = int(rng.integers(0, w)), int(rng.integers(int(ccy - ch * 0.4), int(ccy + ch * 0.9)))
        if L.get(x, y) and y > ccy:
            L.put(x, y, pal[0])
    for _ in range(60):                                             # sun-lit leaves (gold rim)
        x, y = int(rng.integers(0, w)), int(rng.integers(0, int(ccy + ch * 0.2)))
        if L.get(x, y) and (not L.get(x - 1, y - 1) or not L.get(x - 1, y)):
            L.put(x, y, pal[-1])
    outline_inner(L, 0.45, 0.8)
    outline_outer(L, 0.45)
    for _ in range(12):                                             # fallen leaves at the foot
        L.put(int(cx + rng.normal(0, 12)), gy + int(rng.integers(-1, 1)), pal[int(rng.integers(3, len(pal)))])
    ground_blades(L, rng, 9, cx - 8, cx + 9, h - 1, 3)
    return L


# ---------------------------------------------------------------- structures
def lantern_post(v, f=0):
    rng = seeded("lantern", v)
    w, h = 22, 62
    L = Spr(w, h)
    cx = 8
    if v == 0:                                                      # wooden post, iron bracket
        for y in range(6, h - 1):
            L.put(cx, y, WOOD[3]); L.put(cx + 1, y, WOOD[2]); L.put(cx + 2, y, WOOD[1])
        for y in range(h - 4, h - 1):
            for x in range(cx - 2, cx + 5):
                L.put(x, y, WOOD[2])
    else:                                                           # old stone pillar lantern
        for y in range(10, h - 1):
            for x in range(cx - 1, cx + 4):
                L.put(x, y, STONE[3 if x < cx + 1 else 2] if x < cx + 3 else STONE[1])
        for y in range(h - 5, h - 1):
            for x in range(cx - 3, cx + 6):
                L.put(x, y, STONE[3 if x < cx + 1 else 1])
        gold_flecks(L, rng, 6, cx - 1, cx + 4, 12, h - 4)
    for k in range(9):                                              # arm
        L.put(cx + 2 + k, 7 - (k // 6), IRON[2]); L.put(cx + 2 + k, 8 - (k // 6), IRON[1])
    for y in range(8, 11):
        L.put(cx + 10, y, IRON[1])
    ox, oy = cx + 10, 11                                            # lantern body
    halo(L, ox, oy + 4, 11, hx("#ffd36a"), 0.6 if f == 0 else 0.5)
    for y in range(oy, oy + 9):
        for x in range(ox - 3, ox + 4):
            edge = x in (ox - 3, ox + 3) or y in (oy, oy + 8)
            L.put(x, y, IRON[1] if edge else (hx("#fff2b8") if abs(x - ox) < 2 and 2 < y - oy < 6 else hx("#f8bf48")))
    L.put(ox, oy - 1, IRON[2]); L.put(ox - 1, oy - 1, IRON[1]); L.put(ox + 1, oy - 1, IRON[1])
    if f:
        L.put(ox, oy + 5, hx("#fde48a"))
    outline_outer(L, 0.45)
    return L


def torch_post(v, f):
    rng = seeded("torch", v)
    w, h = 18, 46
    L = Spr(w, h)
    cx = 8
    for y in range(14, h - 1):
        L.put(cx, y, WOOD[3]); L.put(cx + 1, y, WOOD[2]); L.put(cx + 2, y, WOOD[1])
    for y in range(h - 4, h - 1):
        for x in range(cx - 2, cx + 5):
            L.put(x, y, STONE[2])
    for x in range(cx - 2, cx + 5):
        L.put(x, 14, IRON[2]); L.put(x, 15, IRON[1])
    halo(L, cx + 1, 8, 9, hx("#ffb84a"), 0.55)
    flame = [(0, 0, 4), (-1, 1, 3), (1, 1, 3), (0, 1, 5), (-1, 2, 2), (1, 2, 2), (0, 2, 4), (0, 3, 1), (-1, 3, 1), (1, 3, 1), (0, -1, 5), (0, -2, 6)]
    sway = 1 if f else 0
    for dx, dy, c in flame:
        L.put(cx + 1 + dx + (sway if dy < 0 else 0), 9 - dy + 3, FLAME[min(6, c)])
    L.put(cx + 1 + sway, 7, FLAME[5]); L.put(cx + 1 + sway, 6, FLAME[6] if f else FLAME[5])
    outline_outer(L, 0.45)
    return L


def statue(v):
    rng = seeded("statue", v)
    w, h = 34, 76
    L = Spr(w, h)
    cx = 17
    box(L, 5, h - 20, 24, 12, 7, STONE, STONE)                      # plinth
    for x in range(4, 30):
        L.put(x, h - 22, STONE[4]); L.put(x, h - 8 - 1 + 0, STONE[0]) if False else None
    top = 14
    for y in range(top + 12, h - 21):                                # robe
        t = (y - (top + 12)) / max(1, (h - 21 - (top + 12)))
        hw = 5 + 4.5 * t ** 0.8
        for x in range(int(cx - hw), int(cx + hw) + 1):
            tt = (x - cx) / hw
            i = 4 if tt < -0.55 else (3 if tt < -0.05 else (2 if tt < 0.5 else 1))
            if int(abs(x - cx) * 1.1 + y * 0.12) % 4 == 0 and t > 0.15:
                i = max(0, i - 1)                                    # fold lines
            L.put(x, y, STONE[i])
    for y in range(top + 8, top + 14):                               # shoulders / folded arms
        for x in range(cx - 7, cx + 8):
            L.put(x, y, STONE[4 if x < cx else 2])
    for y in range(top + 17, top + 21):
        for x in range(cx - 6, cx + 7):
            L.put(x, y, STONE[3 if x < cx else 1])
    for y in range(top, top + 10):                                   # head + hood
        for x in range(cx - 5, cx + 6):
            if ((x - cx) / 5.0) ** 2 + ((y - top - 4.5) / 5.4) ** 2 <= 1:
                L.put(x, y, STONE[4 if x < cx - 1 else (3 if x < cx + 2 else 2)])
    for x in range(cx - 2, cx + 3):
        L.put(x, top + 6, STONE[1])
    if v == 1:                                                       # headless, broken shoulder
        for y in range(top - 1, top + 9):
            for x in range(cx - 6, cx + 7):
                L.a[y, x, 3] = 0 if (y - top) < 7 else L.a[y, x, 3]
        for k in range(6):
            L.put(cx - 3 + k, top + 7 + (k % 2), STONE[2])
    # sun-disc halo behind the head, gold leaf flaking
    for a in np.linspace(0, 6.283, 70):
        if rng.random() < 0.7:
            x, y = cx + np.cos(a) * 9.5, top + 4 + np.sin(a) * 9.5
            if not L.get(round(x), round(y)):
                L.put(round(x), round(y), GOLD_L[2 if rng.random() < 0.5 else 1])
    for k in range(10):
        if rng.random() < 0.6:
            a = k * 0.628
            x, y = cx + np.cos(a) * 12.5, top + 4 + np.sin(a) * 12.5
            if not L.get(round(x), round(y)):
                L.put(round(x), round(y), GOLD_L[3])
    for _ in range(5):                                               # cracks
        x, y = int(rng.integers(cx - 6, cx + 6)), int(rng.integers(top + 6, h - 24))
        for k in range(int(rng.integers(5, 11))):
            if L.get(x, y):
                L.put(x, y, STONE[0])
            x += int(rng.choice([-1, 0, 1])); y += 1
    moss_dots(L, rng, 70, 4, 31, top + 14, h - 8)
    moss_dots(L, rng, 14, cx - 7, cx + 8, top + 7, top + 12)
    gold_flecks(L, rng, 9, cx - 6, cx + 7, top + 10, h - 30)
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 10, 3, 31, h - 1, 3)
    return L


def stele(v):
    rng = seeded("stele", v)
    w, h = 28, 72
    L = Spr(w, h)
    x0, x1 = 5, 20
    top = 6 + (v % 2) * 3
    for y in range(top, h - 8):
        top_r = 0 if y > top + 7 else (7 - int(np.sqrt(max(0, 49 - (7 - (y - top)) ** 2))))
        for x in range(x0 + top_r, x1 - top_r + 1):
            t = (x - x0) / (x1 - x0)
            i = 4 if t < 0.15 else (3 if t < 0.5 else (2 if t < 0.85 else 1))
            if (x * 3 + y * 7) % 11 == 0:
                i = max(0, i - 1)
            L.put(x, y, STONE[i])
    for y in range(top + 3, h - 8):                                  # right side face (depth)
        for x in range(x1 + 1, x1 + 4):
            L.put(x, y, STONE[0] if x > x1 + 1 else STONE[1])
    cxd, cyd = (x0 + x1) // 2, top + 14
    for y in range(cyd - 6, cyd + 7):                                # sun disc: gold ring + rays
        for x in range(cxd - 6, cxd + 7):
            d = np.hypot(x - cxd, (y - cyd))
            if 3.2 <= d <= 4.6 and rng.random() < 0.82:
                L.put(x, y, GOLD_L[3 if (x + y) % 3 else 2])
            elif d < 3.2:
                L.put(x, y, STONE[1] if d > 1.6 else GOLD_L[1])
    for k in range(12):
        a = k * 0.5236
        if rng.random() < 0.7:
            L.put(round(cxd + np.cos(a) * 6.3), round(cyd + np.sin(a) * 6.3), GOLD_L[2])
    for r_ in range(3):                                              # glyph lines
        for gx in range(x0 + 2, x1 - 1, 3):
            if rng.random() < 0.8:
                L.put(gx, cyd + 10 + r_ * 5, STONE[0]); L.put(gx + 1, cyd + 10 + r_ * 5 + int(rng.integers(0, 2)), STONE[0])
    x, y = x0 + 6, top + 1                                           # long crack
    for k in range(18):
        L.put(x, y, STONE[0])
        x += int(rng.choice([-1, 0, 0, 1])); y += 1
    for _ in range(8):                                               # base rubble
        bx, by = int(rng.integers(1, w - 3)), h - 8 + int(rng.integers(0, 6))
        for dx, dy, c in ((0, 0, 3), (1, 0, 2), (0, 1, 1), (1, 1, 0)):
            L.put(bx + dx, by + dy, STONE[c])
    moss_dots(L, rng, 36, x0, x1 + 1, h - 22, h - 6)
    gold_flecks(L, rng, 8, x0, x1, top + 4, h - 30)
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 10, 2, w - 3, h - 1, 3)
    return L


def banner_pole(color, f):
    rng = seeded("banner" + color, f)
    pal = {"red": RED, "blue": BLUE, "green": GREEN}[color]
    w, h = 26, 72
    L = Spr(w, h)
    cx = 5
    for y in range(4, h - 1):
        L.put(cx, y, WOOD[3]); L.put(cx + 1, y, WOOD[2]); L.put(cx + 2, y, WOOD[1])
    for y in range(h - 4, h - 1):
        for x in range(cx - 2, cx + 5):
            L.put(x, y, STONE[2])
    for k in range(3):                                               # gold sun finial
        for x in range(cx - 1 + k // 2, cx + 4 - k // 2):
            L.put(x, 2 - k + 2, GOLD_L[2 + (k == 0)])
    for x in range(cx + 3, cx + 19):                                 # crossbar
        L.put(x, 6, WOOD[3]); L.put(x, 7, WOOD[1])
    sw = 1 if f else 0
    for y in range(8, 48):
        wav = int(np.sin(y * 0.22 + (1.2 if f else 0)) * 1.2)
        tail = max(0, y - 40)
        for x in range(cx + 5 + wav + tail // 2, cx + 17 + wav - tail // 2 + sw):
            t = (x - cx - 5) / 12
            i = 3 if t < 0.25 else (2 if t < 0.7 else 1)
            if x == cx + 5 + wav + tail // 2 or y == 8:
                i = 4
            L.put(x, y, pal[i])
    cxe, cye = cx + 11 + sw, 22                                      # gold sun emblem
    for y in range(cye - 5, cye + 6):
        for x in range(cxe - 5, cxe + 6):
            d = np.hypot(x - cxe, y - cye)
            if d <= 2.4:
                L.put(x, y, GOLD_L[3])
            elif 3.2 <= d <= 4.0:
                L.put(x, y, GOLD_L[2])
    for k in range(8):
        a = k * 0.785
        L.put(round(cxe + np.cos(a) * 5.2), round(cye + np.sin(a) * 5.2), GOLD_L[2])
    for x in range(cx + 5, cx + 17):                                 # gold hem
        L.put(x, 9, GOLD_L[1])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 6, cx - 3, cx + 6, h - 1, 2)
    return L


def well(v):
    rng = seeded("well", v)
    w, h = 46, 62
    L = Spr(w, h)
    cx, cy = 23, h - 14
    # ring wall (cylinder, 3/4 view)
    for y in range(cy - 4, cy + 9):
        for x in range(cx - 15, cx + 16):
            e = ((x + 0.5 - cx) / 15.5) ** 2
            if e <= 1:
                top_edge = cy - 4 + 4 * (1 - np.sqrt(1 - e)) * -1
                yy_top = cy - 4 - 4 * np.sqrt(1 - e) + 4
                if y >= yy_top - 0.5 and y <= cy + 4 + 4 * np.sqrt(1 - e):
                    t = (x - cx) / 15.5
                    i = 4 if t < -0.5 else (3 if t < 0.0 else (2 if t < 0.55 else 1))
                    row = (y - (cy - 4)) // 4
                    if (y - (cy - 4)) % 4 == 0:
                        i = max(0, i - 2)
                    if (x + row * 5) % 9 == 0:
                        i = max(0, i - 2)
                    L.put(x, y, STONE[i])
    for y in range(cy - 8, cy + 1):                                  # rim ellipse + water inside
        for x in range(cx - 16, cx + 17):
            dd = ((x + 0.5 - cx) / 15.8) ** 2 + ((y - (cy - 3.5)) / 5.2) ** 2
            if dd <= 1:
                L.put(x, y, STONE[5] if dd > 0.62 else (WATER[1] if dd > 0.3 else WATER[2]))
                if dd < 0.45 and (x + y) % 7 == 0:
                    L.put(x, y, WATER[4])
    for x in (cx - 13, cx + 13):                                     # posts
        for y in range(cy - 38, cy - 2):
            L.put(x, y, WOOD[3]); L.put(x + 1, y, WOOD[1])
    for y in range(cy - 40, cy - 36):                                # little roof
        span = 16 + (y - (cy - 40)) * 3
        for x in range(cx - span // 2 - 2, cx + span // 2 + 3):
            c = RED[3] if (x + y) % 4 else RED[2]
            if y == cy - 37:
                c = RED[1]
            L.put(x, y, c)
    for x in range(cx - 14, cx + 15):
        L.put(x, cy - 24, WOOD[3]); L.put(x, cy - 23, WOOD[1])      # windlass beam
    for y in range(cy - 23, cy - 10):                                # rope + bucket
        L.put(cx + 2, y, hx("#a47a45"))
    for y in range(cy - 10, cy - 4):
        for x in range(cx, cx + 5):
            L.put(x, y, WOOD[2] if y > cy - 9 else WOOD[4])
    moss_dots(L, rng, 40, cx - 15, cx + 16, cy - 4, cy + 8)
    gold_flecks(L, rng, 5, cx - 14, cx + 15, cy - 3, cy + 7)
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 14, 3, w - 4, h - 1, 3)
    return L


def hay_cart(v):
    rng = seeded("cart", v)
    w, h = 78, 54
    L = Spr(w, h)
    bx0, by0 = 12, 20
    for x in range(bx0 - 8, bx0 + 8):                                # shafts to the left
        L.put(x, by0 + 14 + (x - bx0) // 6, WOOD[3]); L.put(x, by0 + 15 + (x - bx0) // 6, WOOD[1])
    # hay heap on top
    ys, xs = np.mgrid[0:h, 0:w]
    zz = np.zeros((h, w))
    for (lx, ly, lr) in ((44, 26, 22), (30, 28, 12), (58, 27, 12)):
        d = 1 - ((xs + 0.5 - lx) / lr) ** 2 - ((ys + 0.5 - ly) / (lr * 0.55)) ** 2
        zz = np.maximum(zz, np.where(d > 0, np.sqrt(np.clip(d, 0, 1)) * lr * 0.8, 0))
    inside = zz > 0.3
    fill(L, inside, zz, HAY, dither=0.3, gain=0.9, lo=-0.25, hi=0.95, bias=0.1)
    for _ in range(100):
        x, y = int(rng.integers(0, w)), int(rng.integers(0, h))
        if L.get(x, y) and L.get(x, y)[0] > 150:
            L.put(x + 1, y - 1, HAY[5]) if rng.random() < 0.5 else None
    box(L, 18, 28, 52, 10, 3, WOOD, WOOD, planks=7)                  # cart bed
    for wx in (28, 58):                                              # wheels (side-on circles with spokes)
        wy, r = 42, 10
        for y in range(wy - r, wy + r + 1):
            for x in range(wx - r, wx + r + 1):
                d = np.hypot(x - wx, y - wy)
                if r - 2 <= d <= r:
                    L.put(x, y, WOOD[1] if (x + y) % 2 else WOOD[0])
                elif r - 3.2 <= d < r - 2:
                    L.put(x, y, IRON[2])
                elif d < 2.4:
                    L.put(x, y, IRON[3])
        for k in range(8):
            a = k * 0.785 + 0.2
            for s in range(2, r - 2):
                L.put(round(wx + np.cos(a) * s), round(wy + np.sin(a) * s), WOOD[3] if s % 2 else WOOD[2])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 14, 6, w - 6, h - 1, 3)
    return L


def bench(v):
    rng = seeded("bench", v)
    w, h = 40, 24
    L = Spr(w, h)
    for x in (4, 34):
        for y in range(10, h - 1):
            L.put(x, y, WOOD[2]); L.put(x + 1, y, WOOD[1])
    for y in range(0, 10):                                           # backrest
        for x in range(2, 38):
            L.put(x, y, WOOD[4] if y in (0, 5) else (WOOD[3] if y < 5 else WOOD[2]))
    for y in (4, 5):
        pass
    box(L, 1, 11, 38, 3, 4, WOOD, WOOD, planks=0)
    for x in range(1, 39, 7):
        L.put(x, 12, WOOD[1])
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    return L


def laundry(v):
    rng = seeded("laundry", v)
    w, h = 82, 46
    L = Spr(w, h)
    for x in (5, 76):
        for y in range(4, h - 1):
            L.put(x, y, WOOD[3]); L.put(x + 1, y, WOOD[1])
    for x in range(5, 78):                                           # rope with sag
        L.put(x, 6 + int(3 * np.sin((x - 5) / 73 * 3.14159)), hx("#cfb88a"))
    cloths = [RED, BLUE, (hx("#f0e4c8"), hx("#d8c8a0"), hx("#bca878"), hx("#e8dcc0"), hx("#fffaf0")), GREEN, RED, (hx("#f0e4c8"), hx("#d8c8a0"), hx("#bca878"), hx("#e8dcc0"), hx("#fffaf0"))]
    xs0 = [11, 23, 36, 48, 59, 66]
    for k, x0 in enumerate(xs0):
        pal = cloths[k]
        wd = 9 if k < 5 else 7
        ln = int(10 + rng.integers(0, 10))
        sag = int(3 * np.sin((x0 - 5) / 73 * 3.14159))
        for y in range(7 + sag, 7 + sag + ln):
            for x in range(x0, x0 + wd):
                c = pal[3 if x < x0 + 2 else (2 if x < x0 + wd - 2 else 1)]
                if (y - 7 - sag) % 5 == 4:
                    c = pal[1]
                L.put(x + int(np.sin(y * 0.4 + k) > 0.7), y, c)
        L.put(x0 + 1, 7 + sag, WOOD[3]); L.put(x0 + wd - 2, 7 + sag, WOOD[3])
    outline_outer(L, 0.45)
    ground_blades(L, rng, 8, 3, 9, h - 1, 3)
    ground_blades(L, rng, 8, 74, 80, h - 1, 3)
    return L


def fountain(f):
    rng = seeded("fountain", 0)
    w, h = 76, 70
    L = Spr(w, h)
    cx = 38
    by = h - 18
    # outer basin (cylinder, 3/4 view)
    for y in range(by - 6, by + 10):
        for x in range(cx - 33, cx + 34):
            e = ((x + 0.5 - cx) / 33.5) ** 2
            if e <= 1:
                yt = by - 6 + 6 * (1 - np.sqrt(1 - e)) * 0 + 6 - 6 * np.sqrt(1 - e) * 1.0
                yb = by + 4 + 6 * np.sqrt(1 - e)
                if yt - 0.5 <= y <= yb:
                    t = (x - cx) / 33.5
                    i = 4 if t < -0.5 else (3 if t < 0.0 else (2 if t < 0.55 else 1))
                    if (y - by) % 5 == 0:
                        i = max(0, i - 2)
                    if (x // 7) % 3 == 1 and y < by + 6:
                        i = max(0, i - 1)
                    L.put(x, y, STONE[i])
    for y in range(by - 12, by + 4):                                 # rim ellipse + water
        for x in range(cx - 34, cx + 35):
            dd = ((x + 0.5 - cx) / 33.8) ** 2 + ((y - (by - 4)) / 8.6) ** 2
            if dd <= 1:
                if dd > 0.78:
                    L.put(x, y, STONE[5] if (x < cx + 10) else STONE[4])
                else:
                    wv = int(5 * (1 - np.hypot((x - cx) / 28, (y - by + 4) / 6.8)))
                    c = WATER[1 if dd > 0.62 else 2]
                    if (x + y * 2 + f * 3) % 9 == 0:
                        c = WATER[4]
                    L.put(x, y, c)
    # pedestal
    for y in range(by - 36, by - 3):
        for x in range(cx - 4, cx + 5):
            t = (x - cx) / 4.0
            L.put(x, y, STONE[4 if t < -0.4 else (3 if t < 0.2 else 2)] if (y - by) % 6 else STONE[1])
    for (yy, rx_) in ((by - 22, 15), (by - 36, 9)):                  # upper bowls
        for y in range(yy - 4, yy + 4):
            for x in range(cx - rx_ - 1, cx + rx_ + 2):
                dd = ((x + 0.5 - cx) / (rx_ + 0.5)) ** 2 + ((y - yy) / 3.4) ** 2
                if dd <= 1:
                    L.put(x, y, STONE[5 if dd > 0.7 else 2] if dd > 0.5 else WATER[2])
                    if dd > 0.7 and x > cx + 4:
                        L.put(x, y, STONE[3])
    for y in range(by - 46, by - 38):                                # finial: gold sun disc
        for x in range(cx - 4, cx + 5):
            d = np.hypot(x - cx, (y - (by - 42)))
            if d <= 3.5:
                L.put(x, y, GOLD_L[3 if d < 2 else 2])
    for k in range(8):
        a = k * 0.785
        L.put(round(cx + np.cos(a) * 5.5), round(by - 42 + np.sin(a) * 5.5), GOLD_L[2])
    # falling water: arcs from the finial/bowls
    for (y0, rx_, y1) in ((by - 36, 9, by - 22), (by - 22, 15, by - 5)):
        for side in (-1, 1):
            for k in range(8 + 2 * f):
                t = k / 8
                x = cx + side * (3 + t * (rx_ - 3))
                y = y0 - 2 + 4 * t - 6 * t * (1 - t) * (-1) * 0 + 3 * t * t * (y1 - y0) / 6
                L.put(round(x), round(y), WATER[5 if (k + f) % 3 == 0 else 4])
        for k in range(int(y1 - y0 - 3)):                            # curtain
            if (k + f) % 2 == 0:
                L.put(cx + rx_ - 1, y0 + 3 + k, WATER[4]); L.put(cx - rx_ + 1, y0 + 3 + k, WATER[4])
    for _ in range(6):
        L.put(cx + int(rng.integers(-10, 11)), by - 6 + int(rng.integers(-1, 3)), WATER[5])
    moss_dots(L, rng, 40, cx - 32, cx + 33, by - 2, by + 12)
    gold_flecks(L, rng, 8, cx - 30, cx + 31, by - 1, by + 8)
    outline_inner(L, 0.45, 0.7)
    outline_outer(L, 0.45)
    ground_blades(L, rng, 16, 4, w - 5, h - 1, 3)
    return L


# ---------------------------------------------------------------- ground decals (flat, sit on the terrain)
def sun_inlay(v):
    """Worn sun-disc medallion set into flagstones: gold ring + rays, flaked, cracked."""
    rng = seeded("inlay", v)
    w, h = 96, 54
    L = Spr(w, h)
    cx, cy = w / 2, h / 2
    ys, xs = np.mgrid[0:h, 0:w]
    d = np.hypot((xs + 0.5 - cx) / 1.0, (ys + 0.5 - cy) * 1.75)
    for y in range(h):
        for x in range(w):
            r = d[y, x]
            if r > 47:
                continue
            if 40 <= r <= 47:                                         # outer stone ring with dark joints
                L.put(x, y, STONE[3] if (int(np.arctan2(y - cy, x - cx) * 9) % 2) else STONE[2])
            elif 36 <= r < 40:
                L.put(x, y, GOLD_L[1 if rng.random() < 0.85 else 0] if rng.random() < 0.88 else STONE[1])
            elif r < 36:
                L.put(x, y, STONE[3] if (x + y) % 7 else STONE[4])
    for k in range(16):                                               # rays
        a = k * 6.283 / 16
        for s in np.linspace(14, 33, 40):
            x, y = cx + np.cos(a) * s, cy + np.sin(a) * s / 1.75
            L.put(round(x), round(y), GOLD_L[2 if k % 2 else 1] if rng.random() < 0.85 else STONE[1])
    for y in range(h):                                                # central gold disc
        for x in range(w):
            if d[y, x] < 13:
                L.put(x, y, GOLD_L[3 if d[y, x] < 6 else 2] if rng.random() < 0.9 else STONE[2])
    for _ in range(3):                                                # cracks across
        x, y = rng.uniform(15, 80), rng.uniform(10, 44)
        for k in range(int(rng.integers(12, 26))):
            if L.get(round(x), round(y)):
                L.put(round(x), round(y), STONE[0])
            x += rng.choice([-1, 0, 1, 1]); y += rng.choice([-1, 0, 0, 1]) * 0.7
    for y in range(h):                                                # outline ring on the stone-dark edge
        for x in range(w):
            if L.get(x, y) and (not L.get(x, y - 1) or not L.get(x - 1, y)):
                L.put(x, y, STONE[0])
    moss_dots(L, rng, 60, 0, w, 0, h)
    for _ in range(55):                                               # weathered holes: edge erosion
        x, y = int(rng.integers(0, w)), int(rng.integers(0, h))
        if d[min(y, h - 1), min(x, w - 1)] > 36:
            L.a[y, x, 3] = 0
    return L


def inlay_line(v):
    rng = seeded("inlay_line", v)
    w, h = 64, 8
    L = Spr(w, h)
    for x in range(w):
        for y in (3, 4):
            if rng.random() < 0.82:
                L.put(x, y, GOLD_L[2 if y == 3 else 1])
        if rng.random() < 0.25:
            L.put(x, 2, STONE[1]); L.put(x, 5, STONE[1])
    for x in range(2, w, 16):                                          # diamond studs
        for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1), (0, 1)):
            L.put(x + dx, 3 + dy, GOLD_L[3])
    return L


def rubble(v):
    rng = seeded("rubble", v)
    w, h = 26, 12
    L = Spr(w, h)
    for _ in range(7 + v):
        cx, cy = int(rng.integers(4, w - 4)), int(rng.integers(4, h - 3))
        rx, ry = int(rng.integers(2, 5)), int(rng.integers(1, 3))
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if ((x - cx) / (rx + 0.5)) ** 2 + ((y - cy) / (ry + 0.5)) ** 2 <= 1:
                    L.put(x, y, STONE[4 if (x < cx and y <= cy) else (3 if y <= cy else 1)])
    for _ in range(6):
        x, y = int(rng.integers(2, w - 2)), h - 2
        L.put(x, y, GRASS_F[3]); L.put(x, y - 1, GRASS_F[4]); L.put(x + 1, y, GRASS_B[2])
    moss_dots(L, rng, 6, 0, w, 0, h)
    outline_outer(L, 0.45)
    return L


def flagstone_path(v):
    """Single loose flagstone, bigger than a stepping stone, for broken plaza edges."""
    rng = seeded("flag", v)
    w, h = 28, 14
    L = Spr(w, h)
    m = ellipse_mask(w, h, w / 2, h / 2, 12 + v, 5)
    z = np.where(m, 3.0, 0)
    for y, x in zip(*np.nonzero(m)):
        L.put(x, y, STONE[3 if (x + y) % 6 else 4])
    for y, x in zip(*np.nonzero(m & ~np.roll(m, 1, 0))):
        L.put(x, y, STONE[4])
    for y, x in zip(*np.nonzero(m & ~np.roll(m, -1, 0))):
        L.put(x, y, STONE[1])
    moss_dots(L, rng, 8, 0, w, 0, h)
    outline_outer(L, 0.45)
    return L


# ---------------------------------------------------------------- catalogue
def build_extra(save, sprites):
    def reg(name, L, fp=(1, 1), block=False, sway=False, frames=None, extra=None):
        sprites[name] = L
        save(name, L, list(fp), block, sway, frames, extra)

    def anim(base, mk, fp=(1, 1), block=False):
        frs = [base + "_f0.png", base + "_f1.png"]
        for fr in (0, 1):
            reg(f"{base}_f{fr}", mk(fr), fp, block, True, frs)

    for v in range(4):
        anim(f"grass_tall_{'abcd'[v]}", lambda f, v=v: tall_grass(24, 34, v, f))
        anim(f"grass_wide_{'abcd'[v]}", lambda f, v=v: tall_grass(34, 26, v + 7, f))
        reg(f"wildflowers_{'abcd'[v]}", wildflowers(v))
        anim(f"reeds_{'abcd'[v]}", lambda f, v=v: reeds(v, f))
        anim(f"glow_flower_{'abcd'[v]}", lambda f, v=v: glow_flower(v, f))
        reg(f"glow_mushroom_{'abcd'[v]}", glow_mushroom(v))
        reg(f"conifer_giant_{'abcd'[v]}", conifer(44, 112, 9 + (v % 2), v, "conifer_giant"), block=True)
        reg(f"conifer_mid_{'abcd'[v]}", conifer(34, 80, 7, v, "conifer_mid"), block=True)
        reg(f"oak_gold_{'abcd'[v]}", oak(v, 0), block=True)
        reg(f"tree_amber_{'abcd'[v]}", oak(v, 1), block=True)
    for v in range(3):
        reg(f"lilypad_{'abc'[v]}", lilypad(v))
        reg(f"fern_{'abc'[v]}", fern(v))
        reg(f"log_fallen_{'abc'[v]}", log_fallen(v), (2, 1), True)
        reg(f"stump_{'abc'[v]}", stump(v), block=True)
        reg(f"hay_bale_sq_{'abc'[v]}", hay_bale_sq(v), block=True)
        reg(f"hay_bale_round_{'abc'[v]}", hay_bale_round(v), block=True)
        reg(f"hay_pile_{'abc'[v]}", hay_pile(v), (2, 1), True)
        reg(f"crate_{'abc'[v]}", crate(v), block=True)
        reg(f"barrel_{'abc'[v]}", barrel(v), block=True)
        reg(f"sack_{'abc'[v]}", sack(v))
        reg(f"basket_{'abc'[v]}", basket(v))
        reg(f"market_table_{'abc'[v]}", market_table(v), (2, 1), True)
        reg(f"flower_box_{'abc'[v]}", flower_box(v))
        reg(f"feed_trough_{'abc'[v]}", trough(v), (2, 1), True)
        reg(f"statue_weathered_{'abc'[v]}", statue(v % 2), block=True)
        reg(f"sun_stele_{'abc'[v]}", stele(v), block=True)
        reg(f"rubble_{'abc'[v]}", rubble(v))
        reg(f"flagstone_loose_{'abc'[v]}", flagstone_path(v))
        reg(f"mist_{'abc'[v]}", mist(v), (3, 1))
    for v in range(2):
        reg(f"crate_stack_{'ab'[v]}", crate_stack(v), (1, 1), True)
        reg(f"sack_pile_{'ab'[v]}", sack_pile(v), (1, 1), True)
        reg(f"woodpile_{'ab'[v]}", woodpile(v), (1, 1), True)
        reg(f"scarecrow_{'ab'[v]}", scarecrow(v), block=True)
        reg(f"signpost_{'ab'[v]}", signpost(v), block=True)
        reg(f"hay_cart_{'ab'[v]}", hay_cart(v), (3, 1), True)
        reg(f"bench_{'ab'[v]}", bench(v), (2, 1), True)
        reg(f"laundry_line_{'ab'[v]}", laundry(v), (3, 1), True)
        reg(f"well_{'ab'[v]}", well(v), (2, 1), True)
        reg(f"sun_inlay_{'ab'[v]}", sun_inlay(v), (3, 2))
        reg(f"inlay_line_{'ab'[v]}", inlay_line(v), (2, 1))
        reg(f"lantern_post_{'ab'[v]}", lantern_post(v, 0), block=True)
    for v in range(2):
        anim(f"torch_post_{'ab'[v]}", lambda f, v=v: torch_post(v, f), block=True)
    for c in ("red", "blue", "green"):
        frs = [f"banner_{c}_f0.png", f"banner_{c}_f1.png"]
        for fr in (0, 1):
            reg(f"banner_{c}_f{fr}", banner_pole(c, fr), (1, 1), True, True, frs)
    anim("fountain_a", fountain, (3, 2), True)
