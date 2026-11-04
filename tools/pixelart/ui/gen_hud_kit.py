#!/usr/bin/env python
"""Pig Tycoon 2D HUD kit generator (procedural pixel art, deterministic).

Writes RGBA PNGs at 1x, no anti-aliasing, to Assets/Art/PixelMyth/UI/Kit/ plus kit.json.
Style: Docs/art-direction/2d-ui-style-guide.md (sun-bronze on umber lacquer, upper-left light).
Run:  python tools/pixelart/ui/gen_hud_kit.py
"""
import json
import math
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "PixelMyth", "UI", "Kit")


def H(s, a=255):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


# ---- palette (style guide) -------------------------------------------------
RIM, GOLD, BRZ, DEEP = H("f4d27a"), H("e2b44c"), H("b07a2c"), H("6e4518")
GLINT = H("fff1c4")
U0, U1, U2 = H("2a1a12"), H("3d2618"), H("55361f")
INK = H("1a0f0a")
OUTB = H("2e1a0e")            # darkest bronze-neighbour ink (never pure black)
LS = [H("ddd2b6"), H("c2b597"), H("a3967b"), H("82755f")]
CRACK = H("5f5446")
STONE_OUT = H("3f372c")
LAP, LAP2, JADE, CIN = H("2b5d86"), H("5b9cc6"), H("3f8a6a"), H("a8382a")
JADE_HI, JADE_D, JADE_OUT = H("7fd1a3"), H("2a5c46"), H("16322a")
CIN_HI, CIN_D, CIN_OUT = H("d9604a"), H("6b241c"), H("3a1410")
LAP_D = H("1c3d5c")
ROSE, ROSE_D = H("e8a58e"), H("c9795f")
AMBER, AMBER_D = H("d0622f"), H("8e3a22")
ORANGE = H("e0802c")
P0, P1, P2, PINK = H("efe0bb"), H("e2cc98"), H("c9ad74"), H("3a2414")
LEATH_H, LEATH, LEATH_D = H("c28f52"), H("9b6430"), H("6a4020")

KIT = []     # manifest entries


# ---- tiny raster toolkit ---------------------------------------------------
class Img:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.a = np.zeros((h, w, 4), np.uint8)

    def p(s, x, y, c):
        x, y = int(x), int(y)
        if 0 <= x < s.w and 0 <= y < s.h:
            s.a[y, x] = c

    def mask(s):
        return s.a[..., 3] > 0

    def paint(s, m, c):
        s.a[m] = c

    def outline(s, c):
        m = s.mask()
        s.a[dilate(m) & ~m] = c


def shift(m, dx, dy):
    r = np.zeros_like(m)
    h, w = m.shape
    xs0, xs1 = max(dx, 0), w + min(dx, 0)
    ys0, ys1 = max(dy, 0), h + min(dy, 0)
    r[ys0:ys1, xs0:xs1] = m[ys0 - dy:ys1 - dy, xs0 - dx:xs1 - dx]
    return r


def dilate(m):
    return m | shift(m, 1, 0) | shift(m, -1, 0) | shift(m, 0, 1) | shift(m, 0, -1)


def layers(mask):
    d = np.full(mask.shape, -1, int)
    cur = mask.copy()
    k = 0
    while cur.any():
        er = cur & shift(cur, 1, 0) & shift(cur, -1, 0) & shift(cur, 0, 1) & shift(cur, 0, -1)
        d[cur & ~er] = k
        cur = er
        k += 1
    return d


def bevel_shape(im, mask, base, hi, sh, outline=None):
    top = mask & ~shift(mask, 0, 1)
    left = mask & ~shift(mask, 1, 0)
    bot = mask & ~shift(mask, 0, -1)
    right = mask & ~shift(mask, -1, 0)
    lit = top | left
    dark = bot | right
    im.paint(mask, base)
    im.paint(mask & lit & ~dark, hi)
    im.paint(mask & dark & ~lit, sh)
    if outline is not None:
        im.paint(dilate(mask) & ~mask & ~im.mask() | (dilate(mask) & ~mask & (im.a[..., 3] == 0)), outline)


def polar(w, h, cx, cy):
    Y, X = np.mgrid[0:h, 0:w]
    dx, dy = X + 0.5 - cx, Y + 0.5 - cy
    r = np.hypot(dx, dy)
    ang = (np.degrees(np.arctan2(dx, -dy))) % 360.0   # clockwise from top
    return dx, dy, r, ang


def ring_cls(dx, dy, r0, r1, convex=True, edge=1.15):
    d = math.hypot(dx, dy)
    if d < r0 or d >= r1:
        return None
    l = -(dx + dy) / (d * math.sqrt(2) + 1e-9)
    if not convex:
        l = -l
    if r1 - d < edge:
        return "hi" if l > 0.25 else ("lo" if l < -0.25 else "mid")
    if d - r0 < edge:
        return "lo" if l > 0.25 else ("hi" if l < -0.25 else "mid")
    return "mid" if l > -0.1 else "mid2"


def ring(im, cx, cy, r0, r1, pal, convex=True, edge=1.15):
    for y in range(im.h):
        for x in range(im.w):
            c = ring_cls(x + 0.5 - cx, y + 0.5 - cy, r0, r1, convex, edge)
            if c:
                im.p(x, y, pal[c])


def disc_shaded(im, cx, cy, r, base, hi, sh, outline=None):
    dx, dy, rr, _ = polar(im.w, im.h, cx, cy)
    m = rr <= r
    before = im.mask().copy()
    bevel_shape(im, m, base, hi, sh)
    if outline is not None:
        o = dilate(m) & ~m
        im.paint(o, outline)


def blit(dst, src, x, y):
    """alpha-over src (h,w,4 uint8) onto dst (float or uint8 array) at x,y with clipping."""
    sh, sw = src.shape[:2]
    x0, y0 = max(x, 0), max(y, 0)
    x1, y1 = min(x + sw, dst.shape[1]), min(y + sh, dst.shape[0])
    if x0 >= x1 or y0 >= y1:
        return
    s = src[y0 - y:y1 - y, x0 - x:x1 - x].astype(float)
    a = s[..., 3:4] / 255.0
    d = dst[y0:y1, x0:x1].astype(float)
    da = d[..., 3:4] / 255.0
    oa = a + da * (1 - a)
    d[..., :3] = (s[..., :3] * a + d[..., :3] * da * (1 - a)) / np.maximum(oa, 1e-6)
    d[..., 3:4] = oa * 255
    dst[y0:y1, x0:x1] = np.clip(d, 0, 255).astype(np.uint8)


def art(rows, pal):
    h, w = len(rows), len(rows[0])
    im = Img(w, h)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in pal:
                im.p(x, y, pal[ch])
    return im


def save(name, im, **meta):
    os.makedirs(OUT, exist_ok=True)
    Image.fromarray(im.a, "RGBA").save(os.path.join(OUT, name))
    e = {"file": name, "size": [im.w, im.h]}
    e.update(meta)
    KIT.append(e)
    return im


def darken(c, f):
    return (int(c[0] * f), int(c[1] * f), int(c[2] * f), c[3])


def tint_img(im, f):
    o = Img(im.w, im.h)
    o.a = im.a.copy()
    o.a[..., :3] = (o.a[..., :3] * f).astype(np.uint8)
    return o


# ---- 1-3 panels (9-slice, border 8) ---------------------------------------
def rivet(im, cx, cy):
    disc_shaded(im, cx, cy, 2.6, GOLD, RIM, BRZ, outline=OUTB)
    im.p(cx - 0.5, cy - 0.5, GLINT)
    im.p(cx + 0.5, cy + 0.5, DEEP)


def panel_bronze():
    rng = np.random.default_rng(101)
    im = Img(48, 48)
    m = np.ones((48, 48), bool)
    for cx, cy in ((0, 0), (47, 0), (0, 47), (47, 47)):
        for dx, dy in ((0, 0), (1, 0), (0, 1)):
            m[cy + (dy if cy == 0 else -dy), cx + (dx if cx == 0 else -dx)] = False
    d = layers(m)
    for y in range(48):
        for x in range(48):
            if not m[y, x]:
                continue
            lit = min(x, y) <= min(47 - x, 47 - y)
            k = d[y, x]
            if k == 0:
                c = OUTB
            elif k == 1:
                c = RIM if lit else GOLD
            elif k == 2:
                c = GOLD if lit else BRZ
            elif k == 3:
                c = GOLD if lit else BRZ
            elif k == 4:
                c = BRZ if lit else DEEP
            elif k == 5:
                c = INK if lit else U2
            elif k == 6:
                c = U0 if lit else U1
            else:
                c = U0
            im.p(x, y, c)
    # chipped / missing gold leaf (corner zones only: edges are stretched by the 9-slice)
    for (ox, oy) in ((0, 0), (40, 0), (0, 40), (40, 40)):
        for _ in range(7):
            x, y = ox + int(rng.integers(0, 8)), oy + int(rng.integers(0, 8))
            if m[y, x] and 1 <= d[y, x] <= 3:
                im.p(x, y, DEEP if d[y, x] > 1 else BRZ)
    # hairline scratches in the rim
    for (x, y, dx, dy) in ((6, 7, -1, -1), (41, 6, 1, -1), (7, 40, -1, 1), (40, 41, 1, 1)):
        for i in range(3):
            if im.a[y + dy * i, x + dx * i][3]:
                im.p(x + dx * i, y + dy * i, BRZ if i % 2 == 0 else DEEP)
    for cx, cy in ((4, 4), (43, 4), (4, 43), (43, 43)):
        rivet(im, cx, cy)
    # re-assert straight edge bands of the 9-slice mid area (cols/rows 8..39 uniform): nothing to do
    return save("panel_bronze.png", im, nineSlice=[8, 8, 8, 8], note="border L,B,R,T in px; umber lacquer fill, sun-disc rivets in corners")


def panel_parchment():
    rng = np.random.default_rng(202)
    im = Img(48, 48)
    m = np.ones((48, 48), bool)
    # torn corners: randomly bite the 8x8 corner zones
    for (ox, oy, sx, sy) in ((0, 0, 1, 1), (40, 0, -1, 1), (0, 40, 1, -1), (40, 40, -1, -1)):
        for j in range(8):
            for i in range(8):
                x = ox + i if sx == 1 else ox + 7 - i
                y = oy + j if sy == 1 else oy + 7 - j
                depth = (7 - i) + (7 - j)  # near corner -> large
                dd = (i + j)
                if dd < 3 and rng.random() < 0.95:
                    m[y, x] = False
                elif dd < 6 and rng.random() < 0.35:
                    m[y, x] = False
    d = layers(m)
    burn = [H("3a2414"), H("7a4a22"), H("a8702f")]
    for y in range(48):
        for x in range(48):
            if not m[y, x]:
                continue
            k = d[y, x]
            lit = min(x, y) <= min(47 - x, 47 - y)
            if k == 0:
                c = burn[0]
            elif k == 1:
                c = burn[1]
            elif k == 2:
                c = burn[2] if not lit else P2
            elif k == 3:
                c = P2 if not lit else P1
            elif k == 4:
                c = P1
            elif k == 5:
                c = PINK                  # ink rule
            elif k == 6:
                c = P1 if not lit else P0
            else:
                c = P0
            im.p(x, y, c)
    # scorch + stains only in corners
    for (ox, oy) in ((0, 0), (40, 0), (0, 40), (40, 40)):
        for _ in range(9):
            x, y = ox + int(rng.integers(0, 8)), oy + int(rng.integers(0, 8))
            if m[y, x] and d[y, x] in (1, 2, 3):
                im.p(x, y, burn[int(rng.integers(0, 2))])
        for _ in range(4):
            x, y = ox + int(rng.integers(2, 8)), oy + int(rng.integers(2, 8))
            if m[y, x] and d[y, x] >= 6:
                im.p(x, y, P1)
    # rule-corner flourish: tiny diamond where the ink line turns
    for (x, y) in ((3, 3), (44, 3), (3, 44), (44, 44)):
        if im.a[y, x][3]:
            im.p(x, y, PINK)
    return save("panel_parchment.png", im, nineSlice=[8, 8, 8, 8], note="notebooks / dialogue; ink rule 5 px inside edge")


def wedge(im, x, y, flipx, flipy, hi, lo, vertical=False):
    pts = [(0, 0), (1, 0), (0, 1), (1, 1), (2, 0), (3, 0)] if not vertical else [(0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (0, 3)]
    for (dx, dy) in pts:
        px = x + (-dx if flipx else dx)
        py = y + (-dy if flipy else dy)
        if im.a[py, px][3]:
            im.p(px, py, lo)
    # highlight below/right of the head
    hx = x + (-1 if flipx else 1)
    hy = y + (-2 if flipy else 2)
    if im.a[hy, hx][3] and not vertical:
        im.p(hx, hy, hi)


def panel_stone():
    rng = np.random.default_rng(303)
    im = Img(48, 48)
    m = np.ones((48, 48), bool)
    for (ox, oy, sx, sy) in ((0, 0, 1, 1), (40, 0, -1, 1), (0, 40, 1, -1), (40, 40, -1, -1)):
        for j in range(8):
            for i in range(8):
                x = ox + i if sx == 1 else ox + 7 - i
                y = oy + j if sy == 1 else oy + 7 - j
                if i + j < 2:
                    m[y, x] = False
                elif i + j < 5 and rng.random() < 0.25:
                    m[y, x] = False
    d = layers(m)
    for y in range(48):
        for x in range(48):
            if not m[y, x]:
                continue
            k = d[y, x]
            lit = min(x, y) <= min(47 - x, 47 - y)
            if k == 0:
                c = STONE_OUT
            elif k == 1:
                c = LS[0] if lit else LS[2]
            elif k == 2:
                c = LS[1] if lit else LS[3]
            elif k == 3:
                c = LS[1] if lit else LS[2]
            elif k == 4:
                c = LS[3] if lit else LS[0]     # engraved groove: shadow top-left, lit bottom-right
            elif k == 5:
                c = LS[2] if lit else LS[1]
            else:
                c = LS[1]
            im.p(x, y, c)
    # stone grain in corner zones only
    for (ox, oy) in ((0, 0), (40, 0), (0, 40), (40, 40)):
        for _ in range(8):
            x, y = ox + int(rng.integers(0, 8)), oy + int(rng.integers(0, 8))
            if m[y, x] and d[y, x] >= 2:
                im.p(x, y, LS[int(rng.integers(0, 3))])
    # wedge-mark ticks (carved) in each corner pocket
    wedge(im, 2, 5, False, False, LS[0], LS[3])
    wedge(im, 2, 2, False, False, LS[0], LS[3]) if False else None
    wedge(im, 45, 5, True, False, LS[0], LS[3])
    wedge(im, 5, 42, False, True, LS[0], LS[3], vertical=False)
    wedge(im, 42, 42, True, True, LS[0], LS[3])
    wedge(im, 5, 2, False, False, LS[0], LS[3], vertical=True) if False else None
    # cracks: diagonal fissures from the corners
    for (x, y, dx, dy) in ((7, 7, -1, 0), (40, 7, 1, 0), (7, 40, -1, 0), (40, 40, 1, 0)):
        pass
    def crack(pts):
        for (x, y) in pts:
            if im.a[y, x][3] and d[y, x] >= 3:
                im.p(x, y, CRACK)
                if im.a[y + 1, x][3] and d[y + 1, x] >= 3:
                    im.p(x, y + 1, LS[0])
    crack([(7, 3), (6, 4), (6, 5), (5, 6), (4, 6), (3, 7)])
    crack([(40, 2), (41, 3), (41, 4), (42, 5), (43, 5)])
    crack([(6, 40), (5, 41), (5, 42), (4, 43)])
    crack([(41, 39), (42, 40), (42, 41), (43, 42), (43, 43)])
    return save("panel_stone_tablet.png", im, nineSlice=[8, 8, 8, 8], note="weathered limestone, engraved inner groove, wedge ticks and cracks in corner pockets")


# ---- 4 sundial -------------------------------------------------------------
SECTORS = [  # (name, centre angle clockwise from top, light, dark)
    ("noon", 0, RIM, GOLD),
    ("dusk", 90, AMBER, AMBER_D),
    ("night", 180, LAP, LAP_D),
    ("dawn", 270, ROSE, ROSE_D),
]


def sector_of(ang):
    a = (ang + 45) % 360
    return SECTORS[int(a // 90)]


BRONZE_PAL = dict(hi=RIM, mid=GOLD, mid2=BRZ, lo=DEEP)


def sundial_base():
    rng = np.random.default_rng(404)
    im = Img(64, 64)
    cx = cy = 32.0
    for y in range(64):
        for x in range(64):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            r = math.hypot(dx, dy)
            if r > 31.5:
                continue
            ang = math.degrees(math.atan2(dx, -dy)) % 360
            if r > 30.5:
                im.p(x, y, OUTB)
                continue
            if r >= 27.5:
                c = ring_cls(dx, dy, 27.5, 30.5, True)
                im.p(x, y, BRONZE_PAL[c])
                continue
            if r >= 17.5:
                name, _, lt, dk = sector_of(ang)
                c = ring_cls(dx, dy, 17.5, 27.5, False, edge=1.1)
                col = {"hi": lt, "mid": lt, "mid2": lt, "lo": dk}[c]
                if c == "hi":
                    col = lt
                # soft inner dither: second shade on the lower-right half of each sector
                if c in ("mid", "mid2") and r < 23 and (x + y) % 2 == 0 and c == "mid2":
                    col = dk
                im.p(x, y, col)
                continue
            if r >= 12.5:
                c = ring_cls(dx, dy, 12.5, 17.5, True)
                im.p(x, y, BRONZE_PAL[c])
                continue
            c = ring_cls(dx, dy, 0, 12.5, False, edge=1.6)
            im.p(x, y, {"hi": U2, "lo": INK, "mid": U0, "mid2": U0}[c])
    # dark grooves separating band from rim and from cavity ring
    for y in range(64):
        for x in range(64):
            r = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
            if 26.6 <= r < 27.5 or 17.5 <= r < 18.3:
                im.p(x, y, DEEP if (x + 0.5 - cx + y + 0.5 - cy) > 0 else BRZ)
    # sector boundaries (hair lines)
    for k in range(4):
        a0 = math.radians(45 + 90 * k)
        for t in np.arange(17.8, 27.4, 0.25):
            px, py = cx + t * math.sin(a0), cy - t * math.cos(a0)
            im.p(px, py, DEEP)
    # 24 hour notches carved into the rim, wedge-shaped; every 6th is deeper
    for k in range(24):
        a = math.radians(15 * k)
        deep = (k % 6 == 0)
        r_in = 27.3 if deep else 28.6
        for t in np.arange(r_in, 30.6, 0.2):
            hw = 0.35 + 0.18 * (t - r_in)
            for s in (-1, 0, 1):
                off = s * 0.5
                if abs(off) <= hw + 0.2:
                    px = cx + t * math.sin(a) + off * math.cos(a)
                    py = cy - t * math.cos(a) + off * math.sin(a)
                    im.p(px, py, INK if deep else OUTB)
    # night sector stars, dawn rays, noon sunflecks, dusk embers (tiny, not noisy)
    for (r_, a_, col) in ((24, 160, P0), (21, 185, LAP2), (25, 205, P0), (22, 222, LAP2)):
        im.p(cx + r_ * math.sin(math.radians(a_)), cy - r_ * math.cos(math.radians(a_)), col)
    for a_ in (255, 270, 285):
        for t in (20, 21):
            im.p(cx + t * math.sin(math.radians(a_)), cy - t * math.cos(math.radians(a_)), P0)
    for a_ in (-12, 0, 12):
        im.p(cx + 24 * math.sin(math.radians(a_)), cy - 24 * math.cos(math.radians(a_)), GLINT)
    for a_ in (78, 92, 104):
        im.p(cx + 22 * math.sin(math.radians(a_)), cy - 22 * math.cos(math.radians(a_)), ORANGE)
    # worn gold leaf on the rim
    for _ in range(14):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(28.2, 30.2)
        x, y = int(cx + r * math.sin(a)), int(cy - r * math.cos(a))
        if im.a[y, x][3] and tuple(im.a[y, x]) in (RIM, GOLD):
            im.p(x, y, BRZ)
    return save("sundial_base.png", im, note=(
        "top=noon(12h), needle step k (0..23) rotates clockwise 15deg*k, hour=(12+k)%24; quadrants: noon top, dusk right, night bottom, dawn left; "
        "centre cavity r=12.5 hosts 24x24 weather icon centred at (32,32)"),
        cavityCentre=[32, 32], cavityRadius=12.5, needleStepDeg=15, sectors=["noon:top", "dusk:right", "night:bottom", "dawn:left"])


def sundial_needle():
    im = Img(64, 64)
    widths = {2: 2, 3: 2, 4: 4, 5: 4, 6: 4, 7: 6, 8: 6, 9: 6, 10: 8, 11: 8, 12: 8, 13: 6, 14: 6, 15: 4, 16: 4, 17: 2}
    for y, w in widths.items():
        half = w // 2
        for i in range(half):
            im.p(31 - i, y, U2)
            im.p(32 + i, y, U0)
        im.p(32 - half, y, RIM)          # lit left edge
        im.p(31 + half, y, BRZ)          # shaded right edge
    for x in range(30, 34):
        im.p(x, 2, RIM)
        im.p(x, 17, BRZ)
    for y in range(3, 17):
        im.p(31, y, RIM) if im.a[y, 31][3] and im.a[y, 30][3] == 0 else None
    # cinnabar jewel
    for (x, y, c) in ((31, 10, CIN_HI), (32, 10, CIN), (31, 11, CIN), (32, 11, CIN_D)):
        im.p(x, y, c)
    # outer ink then a thin pale halo so it reads on gold, rose and lapis alike
    im.outline(OUTB)
    m = im.mask()
    halo = dilate(m) & ~m
    im.paint(halo, (255, 241, 196, 255))
    return save("sundial_needle.png", im, note="needle points up (=noon); pivot = image centre (32,32); rotate clockwise in whole 15 deg steps only; covers the rim band, leaves the weather cavity free",
                pivot=[32, 32], stepDeg=15)


def pips():
    on = Img(6, 6)
    m = polar(6, 6, 3, 3)[2] <= 2.7
    on.a[..., :] = 0
    bevel_shape(on, m, GOLD, RIM, BRZ)
    on.paint(dilate(m) & ~m, OUTB)
    on.p(2, 2, GLINT)
    save("sundial_day_pip_on.png", on)
    off = Img(6, 6)
    off.paint(m, U0)
    off.paint(m & ~shift(m, 0, 1), INK)
    off.paint(m & ~shift(m, 1, 0), INK)
    off.paint(m & ~shift(m, 0, -1), U2)
    off.paint(dilate(m) & ~m, DEEP)
    save("sundial_day_pip_off.png", off)
    sk = art([
        "..OOOO..",
        ".OwwwwO.",
        "OwwwwwsO",
        "OEEwwEEO",
        "OEEwwEEO",
        ".OwKKsO.",
        ".OwOwsO.",
        "..OOOO..",
    ], {"O": H("4a1a14"), "w": LS[0], "s": LS[2], "E": CIN_D, "K": U0})
    sk.p(1, 3, CIN)
    sk.p(6, 3, CIN)
    save("raid_skull_pip.png", sk, note="raid countdown pip; tint darker in engine for spent pips")


# ---- 5 bars ----------------------------------------------------------------
def tube_frame(w, wx0, wx1, pal, outline, seed, wedge_caps=True, ticks=None):
    rng = np.random.default_rng(seed)
    hh = 12
    im = Img(w, hh)
    m = np.ones((hh, w), bool)
    for (x, y) in ((0, 0), (1, 0), (0, 1), (w - 1, 0), (w - 2, 0), (w - 1, 1),
                   (0, hh - 1), (1, hh - 1), (0, hh - 2), (w - 1, hh - 1), (w - 2, hh - 1), (w - 1, hh - 2)):
        m[y, x] = False
    d = layers(m)
    win = np.zeros_like(m)
    win[2:10, wx0:wx1 + 1] = True
    for y in range(hh):
        for x in range(w):
            if not m[y, x] or win[y, x]:
                continue
            k = d[y, x]
            lit = y < 6
            cap = x < wx0 or x > wx1
            if k == 0:
                c = outline
            elif y == 1 and not cap:
                c = pal["hi"]
            elif y == 10 and not cap:
                c = pal["lo"]
            elif cap:
                c = pal["hi"] if (k == 1 and lit) else (pal["mid"] if y < 7 else pal["mid2"])
                if k == 1 and not lit:
                    c = pal["mid2"]
                if x in (wx0 - 1, wx1 + 1):
                    c = pal["lo"]
            else:
                c = pal["mid"]
            im.p(x, y, c)
    # window inner shade (translucent, drawn over the fill by the engine)
    for x in range(wx0, wx1 + 1):
        im.p(x, 2, (26, 15, 10, 150))
        im.p(x, 3, (26, 15, 10, 60))
    for y in range(2, 10):
        im.p(wx0, y, (26, 15, 10, 120))
    # scale ticks on the rim every 10 %
    if ticks:
        n = ticks
        step = (wx1 - wx0 + 1) / n
        for k in range(1, n):
            x = int(wx0 + step * k)
            im.p(x, 1, pal["lo"])
            im.p(x, 10, pal["lo"] if False else outline)
    # wedge marks on caps
    if wedge_caps:
        for cx0 in (2, w - 6):
            for j, yy in enumerate((3, 5, 7)):
                xx = cx0 + (j % 2)
                for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1), (2, 0)):
                    if cx0 + dx + (j % 2) < w and im.a[yy + dy, xx + dx][3]:
                        im.p(xx + dx, yy + dy, outline)
                im.p(xx + 1, yy + 2, pal["hi"])
    # gold leaf chips
    for _ in range(5):
        x = int(rng.choice([rng.integers(1, wx0), rng.integers(wx1 + 1, w - 1)]))
        y = int(rng.integers(1, 11))
        if im.a[y, x][3] and tuple(im.a[y, x]) in (pal["hi"], pal["mid"]):
            im.p(x, y, pal["lo"])
    return im


def fill_col(rows, hatch=None, glint=None, w=4):
    im = Img(w, 8)
    for y in range(8):
        for x in range(w):
            im.p(x, y, rows[y])
    if hatch:
        for x in range(w):
            for y in range(8):
                if hatch[0](x, y):
                    im.p(x, y, hatch[1])
    if glint:
        for (x, y, c) in glint:
            im.p(x, y, c)
    return im


def bars():
    f = tube_frame(96, 8, 87, dict(hi=RIM, mid=GOLD, mid2=BRZ, lo=DEEP), OUTB, 501, True, ticks=10)
    save("bar_stamina_frame.png", f, window=[8, 2, 80, 8],
         note="window x8..87,y2..9 is transparent: draw bar_trough then fill (scaled horizontally from left) then this frame on top; 9 ticks = 10% steps")
    tr = Img(96, 12)
    tr.a[2:10, 8:88] = U0
    tr.a[2:4, 8:88] = INK
    tr.a[9, 8:88] = U2
    save("bar_stamina_trough.png", tr, note="dark lacquer trough behind stamina fill, same window")
    bright = fill_col([RIM, RIM, GOLD, GOLD, GOLD, GOLD, BRZ, BRZ],
                      glint=[(1, 1, GLINT), (2, 1, GLINT), (3, 4, RIM), (0, 5, BRZ)])
    save("bar_stamina_fill_bright.png", bright, tileX=True, note="recoverable stamina; 4x8 tileable horizontally")
    load = fill_col([BRZ, BRZ, H("8a5a24"), H("8a5a24"), DEEP, DEEP, U2, U2],
                    hatch=(lambda x, y: 2 <= y <= 5 and (x + y) % 4 == 0, BRZ))
    save("bar_stamina_fill_load.png", load, tileX=True, note="daily-load layer, darker umber-gold; 4x8 tileable horizontally")

    jade = dict(hi=JADE_HI, mid=JADE, mid2=JADE_D, lo=JADE_D)
    hf = tube_frame(80, 12, 73, jade, JADE_OUT, 502, False, ticks=None)
    # heart seal in the left cap
    heart = art([
        ".OO.OO.",
        "OaaOHHO",
        "OaHHHHO",
        ".OHHHO.",
        "..OHO..",
        "...O...",
    ], {"O": CIN_OUT, "H": CIN, "a": CIN_HI})
    # shift-down shading
    heart.p(2, 2, CIN_D) if False else None
    hf.a[2:8, 2:9] = np.where(heart.a[..., 3:4] > 0, heart.a, hf.a[2:8, 2:9])
    # gold ring bracket around heart ends
    for y in (9, ):
        for x in range(3, 8):
            hf.p(x, y, BRZ)
    for (x, y) in ((1, 5), (10, 5)):
        hf.p(x, y, RIM if x < 5 else GOLD)
    save("bar_hp_vessel_frame.png", hf, window=[12, 2, 62, 8], note="blood-jade vessel with cinnabar heart seal; window x12..73,y2..9 transparent")
    tr = Img(80, 12)
    tr.a[2:10, 12:74] = U0
    tr.a[2:4, 12:74] = INK
    tr.a[9, 12:74] = U2
    save("bar_hp_trough.png", tr)
    hp = fill_col([H("e8806a"), CIN_HI, CIN, CIN, CIN, CIN, CIN_D, JADE_D], glint=[(1, 1, H("f5b09c")), (3, 3, CIN_HI)])
    save("bar_hp_fill.png", hp, tileX=True, note="cinnabar blood with a dark jade bottom line; 4x8 tileable")


def rage_flames():
    for i in range(4):
        im = Img(16, 24)
        phase = i * math.pi / 2
        Hf = 20.5 + [0, 1.2, -0.6, 0.8][i]
        layers_def = [(1.0, 1.0, H("a8382a")), (0.72, 0.82, ORANGE), (0.46, 0.6, GOLD), (0.22, 0.36, RIM)]
        mm = np.zeros((24, 16), bool)
        for (s, hf_, col) in layers_def:
            hgt = Hf * hf_
            for y in range(24):
                u = (22 - y) / hgt
                if u < 0 or u > 1:
                    continue
                w = s * 6.6 * (1 - u ** 1.5) * (0.85 + 0.15 * math.sin(u * 9 + phase))
                sway = 2.6 * math.sin(phase + u * 3.0) * u + (1.8 if i % 2 else -1.8) * u ** 3
                for x in range(16):
                    if abs(x + 0.5 - (8 + sway)) <= w:
                        im.p(x, y, col)
                        if s == 1.0:
                            mm[y, x] = True
        # left tongue flicker
        ty = int(5 + (i * 2) % 5)
        for (x, y) in ((3 + (i % 2), ty), (3 + (i % 2), ty + 1)):
            if not im.a[y, x][3]:
                im.p(x, y, ORANGE)
        im.outline(CIN_OUT)
        for (x, y) in [((11, 3), (4, 6), (12, 8), (3, 2))[i], ((4, 9), (12, 5), (3, 4), (11, 10))[i]]:
            if not im.a[y, x][3]:
                im.p(x, y, RIM)
        save("bar_rage_flame_%d.png" % i, im, note="rage flame frame %d of 4, loop at ~8 fps, shown only during raids" % i)


# ---- 6 coin ----------------------------------------------------------------
def coin():
    im = Img(16, 16)
    dx, dy, r, ang = polar(16, 16, 8, 8)
    m = r <= 7.6
    bevel_shape(im, m, GOLD, RIM, BRZ)
    im.paint(dilate(m) & ~m, OUTB)
    # milled inner ring
    for y in range(16):
        for x in range(16):
            c = ring_cls(x + 0.5 - 8, y + 0.5 - 8, 6.0, 7.0, False, edge=0.9)
            if c in ("lo", "hi") and r[y, x] <= 7.0:
                im.p(x, y, DEEP if c == "lo" else RIM)
    # tiny radiant sun (West) around the square hole (East): 4 straight rays + 4 diagonal sparks
    for (x, y) in ((7, 2), (8, 2), (7, 3), (8, 3), (7, 12), (8, 12), (7, 13), (8, 13),
                   (2, 7), (2, 8), (3, 7), (3, 8), (12, 7), (12, 8), (13, 7), (13, 8)):
        im.p(x, y, BRZ)
    for (x, y) in ((7, 3), (8, 3), (3, 7), (3, 8), (7, 12), (8, 12), (12, 7), (12, 8)):
        im.p(x, y, DEEP)
    for (x, y) in ((4, 4), (11, 4), (4, 11), (11, 11)):
        im.p(x, y, DEEP)
    # square hole with a rim
    for x in range(5, 11):
        im.p(x, 5, RIM if x < 10 else GOLD)
        im.p(x, 10, DEEP)
    for y in range(5, 11):
        im.p(5, y, RIM if y < 10 else GOLD)
        im.p(10, y, DEEP)
    for y in range(6, 10):
        for x in range(6, 10):
            im.p(x, y, (0, 0, 0, 0))
    for x in range(6, 10):
        im.p(x, 6, OUTB) if False else None
    im.p(3, 3, GLINT)
    save("coin_icon.png", im, note="round coin, square hole (transparent), 8-ray sun stamp")


# ---- 8 pig faces -----------------------------------------------------------
def pig(pal, mood, gem=None):
    im = Img(16, 16)
    head = polar(16, 16, 8, 8.7)[2]
    Y, X = np.mgrid[0:16, 0:16]
    hm = (((X + 0.5 - 8) / 6.4) ** 2 + ((Y + 0.5 - 8.7) / 5.2) ** 2) <= 1.0
    ears = np.zeros_like(hm)
    for (x, y) in ((3, 2), (4, 2), (2, 3), (3, 3), (4, 3), (5, 3), (1, 4), (2, 4), (3, 4)):
        ears[y, x] = True
        ears[y, 15 - x] = True
    m = hm | ears
    bevel_shape(im, m, pal["base"], pal["hi"], pal["sh"])
    # ear inner shade
    for (x, y) in ((2, 4), (3, 4), (13, 4), (12, 4)):
        im.p(x, y, pal["sh"])
    sm = (((X + 0.5 - 8) / 3.4) ** 2 + ((Y + 0.5 - 9.8) / 2.1) ** 2) <= 1.0
    bevel_shape(im, sm, pal["snout"], pal["hi"], pal["base"])
    im.paint(dilate(m) & ~m, pal["out"])
    for (x, y) in ((6, 10), (9, 10)):
        im.p(x, y, pal["nos"])
    e = pal["eye"]
    if mood == "happy":
        for (x, y) in ((3, 7), (4, 6), (5, 7), (10, 7), (11, 6), (12, 7)):
            im.p(x, y, e)
        for (x, y) in ((5, 12), (6, 13), (7, 13), (8, 13), (9, 13), (10, 12)):
            im.p(x, y, e)
    elif mood == "calm":
        for (x, y) in ((4, 6), (4, 7), (5, 6), (5, 7), (10, 6), (10, 7), (11, 6), (11, 7)):
            im.p(x, y, e)
        im.p(4, 6, GLINT if False else e)
        for x in range(6, 10):
            im.p(x, 13, e)
    elif mood == "worried":
        for (x, y) in ((4, 7), (5, 7), (4, 8), (5, 8), (10, 7), (11, 7), (10, 8), (11, 8)):
            im.p(x, y, e)
        for (x, y) in ((3, 6), (4, 5), (5, 5), (10, 5), (11, 5), (12, 6)):
            im.p(x, y, e)
        for (x, y) in ((5, 13), (6, 12), (7, 13), (8, 13), (9, 12), (10, 13)):
            im.p(x, y, e)
    elif mood == "panic":
        for (ox) in (3, 10):
            for yy in range(5, 9):
                for xx in range(ox, ox + 3):
                    im.p(xx, yy, P0)
            for (xx, yy) in ((ox, 5), (ox + 2, 5), (ox, 8), (ox + 2, 8)):
                im.p(xx, yy, pal["base"])
            im.p(ox + 1, 6, CIN_D)
            im.p(ox + 1, 7, e)
        for (x, y) in ((3, 4), (4, 4), (11, 4), (12, 4)):
            im.p(x, y, e)
        for xx in range(6, 10):
            im.p(xx, 12, e)
            im.p(xx, 13, e)
        im.p(7, 13, CIN)
        im.p(8, 13, CIN)
        im.p(14, 5, LAP2)
        im.p(14, 6, LAP2)
        im.p(14, 7, H("2b5d86"))
    if gem:
        im.p(7, 4, gem)
        im.p(8, 4, gem)
    return im


def faces():
    bronze = dict(base=GOLD, hi=RIM, sh=BRZ, snout=RIM, out=OUTB, nos=DEEP, eye=U0)
    gems = [JADE, GOLD, ORANGE, CIN]
    moods = ["happy", "calm", "worried", "panic"]
    for i in range(4):
        pal = dict(bronze)
        if i == 3:
            pal["base"] = H("d9904a")
            pal["sh"] = H("a8542c")
        save("morale_face_%d.png" % i, pig(pal, moods[i], gems[i]),
             note="carved pig mask, mood %d of 3 (0 content ... 3 panic)" % i)
    rose = dict(base=ROSE, hi=H("f5c9b6"), sh=ROSE_D, snout=H("f5c9b6"), out=H("5a2a22"), nos=H("8f4a3a"), eye=U0)
    save("herd_pighead_16.png", pig(rose, "calm"), note="centre icon for the herd ring gauge")


# ---- 7 herd ring -----------------------------------------------------------
BANDS = [("jade", 70, JADE, JADE_D, JADE_HI), ("gold", 100, GOLD, BRZ, RIM), ("amber", 120, AMBER, AMBER_D, H("f0a05a")),
         ("orange", 150, ORANGE, H("a85a1e"), H("f5b060")), ("cinnabar", 200, CIN, CIN_D, CIN_HI)]


def band_of_angle(a):
    v = a / 360.0 * 200.0
    prev = 0
    for i, b in enumerate(BANDS):
        if v < b[1]:
            return i, (v - prev), b[1] - prev
        prev = b[1]
    return 4, 0, 1


def herd_ring(dim=False):
    im = Img(32, 32)
    cx = cy = 16.0
    start = [0] + [b[1] for b in BANDS]
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            r = math.hypot(dx, dy)
            if r > 15.9:
                continue
            ang = math.degrees(math.atan2(dx, -dy)) % 360
            if r > 14.9:
                im.p(x, y, OUTB)
            elif r >= 13.6:
                c = ring_cls(dx, dy, 13.6, 14.9, True, 0.9)
                im.p(x, y, BRONZE_PAL[c])
            elif r >= 9.8:
                bi, _, _ = band_of_angle(ang)
                b = BANDS[bi]
                c = ring_cls(dx, dy, 9.8, 13.6, False, 0.8)
                col = {"hi": b[2], "mid": b[2], "mid2": b[2], "lo": b[3]}[c]
                if c == "hi":
                    col = b[4]
                # divider notches between bands
                for sv in start[1:-1]:
                    da = abs(((ang - sv / 200.0 * 360.0 + 180) % 360) - 180)
                    if da * math.pi / 180 * r < 0.75:
                        col = OUTB
                if dim:
                    col = (int(col[0] * 0.45 + 14), int(col[1] * 0.45 + 9), int(col[2] * 0.45 + 6), 255)
                im.p(x, y, col)
            elif r >= 8.9:
                c = ring_cls(dx, dy, 8.9, 9.8, True, 0.9)
                im.p(x, y, BRONZE_PAL[c])
            else:
                c = ring_cls(dx, dy, 0, 8.9, False, 1.4)
                im.p(x, y, {"hi": U2, "lo": INK, "mid": U0, "mid2": U0}[c])
    return im


def herd():
    full = herd_ring(False)
    spans = {b[0]: [([0] + [x[1] for x in BANDS])[i], b[1]] for i, b in enumerate(BANDS)}
    save("herd_ring_gauge.png", full, socket={"centre": [16, 16], "radius": 8.9, "icon": 16},
         bandsPigsRange={"jade": "<=70", "gold": "71-100", "amber": "101-120", "orange": "121-150", "cinnabar": ">150"},
         bandArcDegClockwiseFromTop={k: [round(v[0] / 200 * 360, 1), round(v[1] / 200 * 360, 1)] for k, v in spans.items()},
         note="ring maps 0..200 pigs to 360 deg clockwise from top; engine shows lit ring up to current angle over herd_ring_gauge_dim")
    save("herd_ring_gauge_dim.png", herd_ring(True), note="dimmed ring for the unfilled part of the gauge")


# ---- 9 joystick ------------------------------------------------------------
def sun_disc(im, cx, cy, r_core, r_tip, n_rays, pal, curl=True, ray_w=0.30):
    """radiant sun: alternating straight (West) and flame-curled (East) rays around a core disc."""
    dx_, dy_, r, ang = polar(im.w, im.h, cx, cy)
    ray = np.zeros((im.h, im.w), bool)
    for k in range(n_rays):
        a0 = k * 360.0 / n_rays
        curled = curl and (k % 2 == 1)
        for y in range(im.h):
            for x in range(im.w):
                rr = r[y, x]
                if rr < r_core - 0.5 or rr > r_tip:
                    continue
                t = (rr - r_core) / max(r_tip - r_core, 1e-6)
                ac = a0 + (22.0 * t if curled else 0.0)
                da = abs(((ang[y, x] - ac + 180) % 360) - 180)
                hw = math.degrees(ray_w * (1 - t) ** 0.9 * 2.0 / max(rr, 1))
                hw = max(hw, 0) * (1.0 if not curled else 0.85) * 2.2
                if da <= hw:
                    ray[y, x] = True
    bevel_shape(im, ray, pal["mid"], pal["hi"], pal["lo"])
    core = r <= r_core
    bevel_shape(im, core, pal["mid"], pal["hi"], pal["lo"])


CROW = [
    "................",
    "................",
    "...XXX..........",
    "..XXXXX.........",
    "XXXXXXXXX.......",
    ".XXXXXXXXXXX....",
    "..XXXXXXXXXXXX..",
    "..XXXXXXXXXXXXXX",
    "...XXXXXXXXXXXX.",
    "....XXXXXXXXXX..",
    ".....XXXXXXXX...",
    "......X.X.X.....",
    "......X.X.X.....",
    ".....XX.XX.XX...",
    "................",
    "................",
]


def stamp_crow(im, ox, oy, col, hi=None):
    for y, row in enumerate(CROW):
        for x, ch in enumerate(row):
            if ch == "X":
                im.p(ox + x, oy + y, col)
    # eye
    if hi is not None:
        im.p(ox + 2, oy + 4, hi)


def joystick():
    rng = np.random.default_rng(909)
    im = Img(96, 96)
    cx = cy = 48.0
    # value-noise for weathering
    blocks = rng.random((32, 32))
    pix = rng.random((96, 96))
    for y in range(96):
        for x in range(96):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            r = math.hypot(dx, dy)
            if r > 47.8:
                continue
            if r > 46.8:
                im.p(x, y, STONE_OUT)
                continue
            if r >= 30.0:
                c = ring_cls(dx, dy, 30.0, 46.8, True, 2.2)
                n = blocks[y // 3, x // 3] * 0.55 + pix[y, x] * 0.45
                base = {"hi": 0, "mid": 1, "mid2": 2, "lo": 3}[c]
                if c in ("mid", "mid2"):
                    base = 1 if n < 0.55 else (2 if n < 0.9 else 3)
                    if c == "mid" and n < 0.2:
                        base = 0
                elif c == "hi":
                    base = 0 if n > 0.12 else 1
                elif c == "lo":
                    base = 3 if n > 0.2 else 2
                im.p(x, y, LS[base])
            else:
                c = ring_cls(dx, dy, 0, 30.0, False, 2.4)
                col = {"hi": U2, "lo": INK, "mid": U0, "mid2": U0}[c]
                if c in ("mid", "mid2") and pix[y, x] > 0.985:
                    col = U1
                im.p(x, y, col)
    # inner bronze lip around the recess
    ring(im, cx, cy, 29.0, 31.0, BRONZE_PAL, True, 0.8)
    # rune dots on the stone ring (24, echoing the day dial)
    for k in range(24):
        a = math.radians(k * 15 + 7.5)
        im.p(cx + 33.4 * math.sin(a), cy - 33.4 * math.cos(a), LS[3])
    # 8 sun-ray compass inlays, N a little larger
    for k in range(8):
        a = math.radians(k * 45)
        big = (k == 0)
        r_base, r_tip = (34.5, 45.5) if big else (35.5, 43.5)
        hw0 = 4.2 if big else 3.2
        ray = Img(96, 96)
        for y in range(96):
            for x in range(96):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                t = dx * math.sin(a) - dy * math.cos(a)      # along the ray
                s = dx * math.cos(a) + dy * math.sin(a)      # across
                if r_base <= t <= r_tip:
                    u = (t - r_base) / (r_tip - r_base)
                    if abs(s) <= hw0 * (1 - u) + 0.3:
                        ray.p(x, y, GOLD)
        mm = ray.mask()
        bevel_shape(ray, mm, GOLD, RIM, BRZ)
        o = dilate(mm) & ~mm
        im.a[mm] = ray.a[mm]
        im.paint(o & (im.a[..., 3] > 0), OUTB)
    # N marker: cinnabar bead above the big ray
    for (x, y) in ((47, 4), (48, 4), (47, 5), (48, 5)):
        pass
    # cracks
    def crack(x, y, steps, seed):
        rr = np.random.default_rng(seed)
        for i in range(steps):
            if 0 <= x < 96 and 0 <= y < 96 and im.a[y, x][3] and math.hypot(x + .5 - cx, y + .5 - cy) >= 30.5:
                im.p(x, y, CRACK)
                if im.a[y + 1, x][3]:
                    pass
            x += int(rr.choice([-1, 0, 1, 1]))
            y += int(rr.choice([-1, 0, 1, 1]))
    crack(14, 22, 16, 1)
    crack(84, 70, 16, 2)
    crack(30, 86, 14, 3)
    crack(80, 20, 12, 4)
    # moss/erosion chips in the outer edge
    for _ in range(20):
        a = rng.uniform(0, 2 * math.pi)
        x, y = int(cx + 46.5 * math.sin(a)), int(cy - 46.5 * math.cos(a))
        if im.a[y, x][3]:
            im.p(x, y, (0, 0, 0, 0))
    return save("joystick_base.png", im, note="stone ring r=30..47, recess r<30 for the knob; compass rays N,NE,E,... 45 deg apart",
                recessCentre=[48, 48], recessRadius=30, rays=8)


def joystick_knob(pressed=False):
    im = Img(40, 40)
    cx = cy = 20.0
    pal = dict(hi=RIM, mid=GOLD, lo=BRZ)
    if pressed:
        pal = dict(hi=GOLD, mid=BRZ, lo=DEEP)
    sun_disc(im, cx, cy, 12.5, 19.3, 16, pal, True, ray_w=0.42)
    dx, dy, r, _ = polar(40, 40, cx, cy)
    face = r < 11.3
    rimc = dict(hi=pal["hi"], mid=pal["mid"], mid2=pal["lo"], lo=DEEP if not pressed else INK)
    ring(im, cx, cy, 11.3, 12.6, dict(hi=pal["lo"], mid=pal["lo"], mid2=pal["lo"], lo=pal["lo"]), True, 0.9)
    im.paint(face, pal["mid"] if not pressed else pal["mid"])
    bevel_shape(im, face, H("e9c25a") if not pressed else pal["mid"], pal["hi"], pal["lo"])
    stamp_crow(im, 12, 11, U0 if not pressed else INK, hi=RIM)
    im.outline(OUTB)
    if pressed:
        sh = Img(40, 40)
        sh.a[1:, :] = im.a[:-1, :] if False else 0
    return im


def knobs():
    k = joystick_knob(False)
    save("joystick_knob.png", k, note="gold sun disc, alternating straight/curled rays, three-legged crow; centre (20,20)")
    p = joystick_knob(True)
    q = Img(40, 40)
    q.a[1:, :] = p.a[:-1, :]
    save("joystick_knob_pressed.png", q, note="pressed: shifted 1 px down, darker rim")


# ---- 10 medallions ---------------------------------------------------------
GREY_PAL = dict(hi=H("b9ad92"), mid=H("8f8570"), mid2=H("6b6254"), lo=H("463e33"))
PRESSED_PAL = dict(hi=GOLD, mid=BRZ, mid2=H("8a5a24"), lo=DEEP)


def medallion(size, state, seed):
    rng = np.random.default_rng(seed)
    im = Img(size, size)
    R = size / 2 - 1.6
    rimw = 5.0 if size >= 48 else 3.5
    cx = size / 2
    cy = size / 2 + (1.0 if state == "pressed" else 0.0) - 0.0
    if state != "pressed":
        cy = size / 2 - 0.0
    pal = {"normal": BRONZE_PAL, "pressed": PRESSED_PAL, "disabled": GREY_PAL}[state]
    outl = OUTB if state != "disabled" else H("2a2420")
    cav_r = R - 1 - rimw
    cav_fill = U0 if state != "pressed" else H("221410")
    if state == "disabled":
        cav_fill = H("24201c")
    for y in range(size):
        for x in range(size):
            dx, dy = x + 0.5 - cx, y + 0.5 - (cy - (0.5 if state != "pressed" else -0.0))
            dy = y + 0.5 - cy
            r = math.hypot(dx, dy)
            if r > R + 0.3:
                continue
            if r > R - 1:
                im.p(x, y, outl)
            elif r >= cav_r:
                c = ring_cls(dx, dy, cav_r, R - 1, True, 1.1)
                im.p(x, y, pal[c])
            else:
                c = ring_cls(dx, dy, 0, cav_r, False, 1.5)
                col = {"hi": U2 if state != "disabled" else H("3a342e"), "lo": INK, "mid": cav_fill, "mid2": cav_fill}[c]
                im.p(x, y, col)
    # wedge-mark notches on the rim
    n = 16 if size >= 48 else 12
    for k in range(n):
        a = math.radians(k * 360 / n + 360 / n / 2)
        rr = cav_r + rimw * 0.5 + 0.5
        px, py = cx + rr * math.sin(a), cy - rr * math.cos(a)
        if state == "disabled":
            im.p(px, py, pal["lo"])
        else:
            im.p(px, py, DEEP if state == "normal" else INK)
    # chips of lost gold leaf
    for _ in range(4 if size >= 48 else 3):
        a = rng.uniform(0, 2 * math.pi)
        rr = cav_r + rng.uniform(0.6, rimw)
        px, py = int(cx + rr * math.sin(a)), int(cy - rr * math.cos(a))
        if im.a[py, px][3] and tuple(im.a[py, px]) in (RIM, GOLD, pal["hi"], pal["mid"]):
            im.p(px, py, pal["lo"])
    if state == "disabled":
        # cracked gold: a fissure through the rim and across the socket edge
        x, y = int(cx - R * 0.55), int(cy - R * 0.7)
        for i in range(int(R * 0.9)):
            if 0 <= x < size and 0 <= y < size and im.a[y, x][3]:
                im.p(x, y, H("1a1612"))
            x += int(rng.choice([0, 1, 1]))
            y += int(rng.choice([1, 1, 0]))
    return im


def medallions():
    for size, nm in ((48, "large"), (32, "small")):
        for st in ("normal", "pressed", "disabled"):
            save("medallion_%s_%s.png" % (nm, st), medallion(size, st, 1000 + size + len(st)),
                 note="round bronze medallion with empty centre; centre cavity r=%s at (%s,%s)%s" % (
                     (size / 2 - 1.6 - 1 - (5.0 if size >= 48 else 3.5)), size // 2, size // 2,
                     "; art shifted 1 px down" if st == "pressed" else ""))
        # cooldown wedges, 8 frames; frame i darkens the clockwise sector from i*45deg back to 12 o'clock
        R = size / 2 - 2.0
        for i in range(8):
            im = Img(size, size)
            dx, dy, r, ang = polar(size, size, size / 2, size / 2)
            cover = (r <= R) & (ang >= i * 45.0)
            im.a[cover] = (26, 15, 10, 150)
            if i > 0:
                a0 = math.radians(i * 45)
                for t in np.arange(1.0, R, 0.5):
                    im.p(size / 2 + t * math.sin(a0), size / 2 - t * math.cos(a0), (244, 210, 122, 150))
            save("cooldown_wedge_%s_%d.png" % (nm, i), im, note="frame %d/8: dark wedge from %d deg clockwise to 360; play 0..7 as time passes" % (i, i * 45))
        for f in range(2):
            im = Img(size, size)
            Rr = size / 2 - 1.6
            for y in range(size):
                for x in range(size):
                    r = math.hypot(x + 0.5 - size / 2, y + 0.5 - size / 2)
                    if f == 0 and Rr - 2.1 < r <= Rr - 1.1:
                        im.p(x, y, JADE)
                    if f == 1:
                        if Rr - 3.1 < r <= Rr - 2.1:
                            im.p(x, y, JADE)
                        elif Rr - 2.1 < r <= Rr - 1.1:
                            im.p(x, y, JADE_HI)
            if f == 1:
                for (ax, ay) in ((size // 2 - 1, 0), (size // 2, 0), (size - 1, size // 2), (0, size // 2 - 1)):
                    pass
                # sparkles at top-left rim
                for (ax, ay) in ((int(size * 0.2), int(size * 0.2)), (int(size * 0.8), int(size * 0.8))):
                    im.p(ax, ay, GLINT)
            save("ready_pulse_%s_%d.png" % (nm, f), im, note="jade rim overlay, alternate frames 0/1 at ~4 fps")


# ---- 11 bag ----------------------------------------------------------------
def satchel():
    im = Img(32, 32)
    # handle
    hm = np.zeros((32, 32), bool)
    Y, X = np.mgrid[0:32, 0:32]
    rr = np.hypot(X + 0.5 - 16, Y + 0.5 - 8)
    hm = (rr <= 6.5) & (rr >= 4.0) & (Y <= 9)
    bevel_shape(im, hm, LEATH_D, LEATH, H("4a2a14"))
    # body
    bm = np.zeros((32, 32), bool)
    for y in range(12, 30):
        hw = 10.0 + (y - 12) * 0.12
        if y >= 27:
            hw -= {27: 0.7, 28: 1.7, 29: 3.2}[y]
        for x in range(32):
            if abs(x + 0.5 - 16) <= hw:
                bm[y, x] = True
    bevel_shape(im, bm, LEATH, LEATH_H, LEATH_D)
    # flap
    fm = np.zeros((32, 32), bool)
    for y in range(7, 21):
        if y == 7:
            hw = 6
        elif y == 8:
            hw = 9
        elif y <= 16:
            hw = 10.4
        else:
            hw = {17: 10, 18: 8.4, 19: 6, 20: 3}[y]
        for x in range(32):
            if abs(x + 0.5 - 16) <= hw:
                fm[y, x] = True
    # shadow cast by flap on body
    cast = dilate(fm) & ~fm & bm
    im.paint(cast, LEATH_D)
    bevel_shape(im, fm, LEATH_H, H("dcae72"), LEATH)
    # stitching along flap border
    lay = layers(fm)
    for y in range(32):
        for x in range(32):
            if fm[y, x] and lay[y, x] == 2 and (x + y) % 2 == 0 and y >= 10:
                im.p(x, y, P1)
    im.outline(H("2e1a0e"))
    # side buckles / straps shading on body
    for y in range(22, 28):
        im.p(7, y, LEATH_D)
        im.p(24, y, LEATH_D)
    # sun clasp
    disc_shaded(im, 16.0, 19.0, 2.8, GOLD, RIM, BRZ, outline=OUTB)
    for (x, y) in ((15, 18), (16, 19)):
        im.p(x, y, DEEP)
    im.p(14, 17, GLINT)
    for (x, y) in ((15, 15), (16, 15), (15, 24 - 1 + 0), (16, 22)):
        pass
    save("bag_satchel.png", im, note="leather satchel with sun clasp at (16,19)")


def socket(kind):
    im = Img(14, 14)
    dx, dy, r, _ = polar(14, 14, 7, 7)
    for y in range(14):
        for x in range(14):
            d = math.hypot(x + 0.5 - 7, y + 0.5 - 7)
            if d > 7.0:
                continue
            if d > 6.0:
                im.p(x, y, OUTB)
            elif d >= 4.6:
                c = ring_cls(x + 0.5 - 7, y + 0.5 - 7, 4.6, 6.0, True, 0.8)
                im.p(x, y, BRONZE_PAL[c] if kind == "empty" else {"hi": GLINT, "mid": RIM, "mid2": GOLD, "lo": BRZ}[c])
            elif kind == "empty":
                c = ring_cls(x + 0.5 - 7, y + 0.5 - 7, 0, 4.6, False, 1.2)
                im.p(x, y, {"hi": U2, "lo": INK, "mid": U0, "mid2": U0}[c])
    return im


BAG_C = (40, 40)


def bag_layout():
    out = {}
    for n, angs, rad in ((3, [-52, 0, 52], 30), (8, [k * 45 for k in range(8)], 30)):
        pts = []
        for a in angs:
            a_ = math.radians(a)
            cx_ = int(round(BAG_C[0] + rad * math.sin(a_)))
            cy_ = int(round(BAG_C[1] - rad * math.cos(a_)))
            pts.append([cx_ - 7, cy_ - 7])
        out[n] = pts
    return out


def bag_rings():
    lay = bag_layout()
    se, sf = socket("empty"), socket("filled")
    save("bag_socket_empty.png", se, note="empty socket, dark")
    save("bag_socket_filled_rim.png", sf, note="lit rim overlay; centre transparent so the item sprite shows through")
    sat = Img(32, 32)
    sat.a = np.array(Image.open(os.path.join(OUT, "bag_satchel.png")).convert("RGBA"))
    for n in (3, 8):
        im = Img(80, 80)
        blit(im.a, sat.a, BAG_C[0] - 16, BAG_C[1] - 16)
        for (x, y) in lay[n]:
            blit(im.a, se.a, x, y)
        # layout images keep transparency: re-key alpha
        a = np.zeros((80, 80, 4), np.uint8)
        blit_a = im.a
        save("bag_ring_%d.png" % n, Img(80, 80) if False else _fix(im), socketTopLeft=lay[n], socketSize=14, satchelTopLeft=[24, 24],
             note="layout reference: %d sockets around the satchel; positions are top-left corners of 14x14 sockets in this 80x80 canvas" % n)


def _fix(im):
    # blit() forces alpha 255 on touched pixels; restore transparency where nothing was drawn
    return im


# ---- 12 preview ------------------------------------------------------------
DIG = {
    "0": "111101101101111", "1": "010110010010111", "2": "111001111100111", "3": "111001111001111",
    "4": "101101111001001", "5": "111100111001111", "6": "111100111101111", "7": "111001001001001",
    "8": "111101111101111", "9": "111101111001111", ",": "000000000010100", ".": "000000000000010",
}


def draw_text(base, s, x, y, col=RIM):
    X = x
    for ch in s:
        g = DIG[ch]
        for j in range(5):
            for i in range(3):
                if g[j * 3 + i] == "1":
                    for (ox, oy, c) in ((1, 1, OUTB), (-1, 0, OUTB), (1, 0, OUTB), (0, -1, OUTB), (0, 1, OUTB), (0, 0, col)):
                        if ox == 0 and oy == 0 or True:
                            pass
        X += 4
    # second pass for correct layering
    X = x
    for layer in ("o", "t"):
        X = x
        for ch in s:
            g = DIG[ch]
            for j in range(5):
                for i in range(3):
                    if g[j * 3 + i] == "1":
                        if layer == "o":
                            for ox in (-1, 0, 1):
                                for oy in (-1, 0, 1):
                                    xx, yy = X + i + ox, y + j + oy
                                    if 0 <= xx < base.shape[1] and 0 <= yy < base.shape[0]:
                                        base[yy, xx] = OUTB
                        else:
                            base[y + j, X + i] = col
            X += 4


def nine_slice(src, w, h, b=8):
    s = np.array(src)
    out = np.zeros((h, w, 4), np.uint8)
    sh, sw = s.shape[:2]
    xs = [0, b, sw - b, sw]
    ys = [0, b, sh - b, sh]
    xd = [0, b, w - b, w]
    yd = [0, b, h - b, h]
    for j in range(3):
        for i in range(3):
            tile = s[ys[j]:ys[j + 1], xs[i]:xs[i + 1]]
            tw, th = xd[i + 1] - xd[i], yd[j + 1] - yd[j]
            ix = (np.arange(tw) * tile.shape[1] // max(tw, 1)).clip(0, tile.shape[1] - 1)
            iy = (np.arange(th) * tile.shape[0] // max(th, 1)).clip(0, tile.shape[0] - 1)
            out[yd[j]:yd[j + 1], xd[i]:xd[i + 1]] = tile[iy][:, ix]
    return out


def load(name):
    return np.array(Image.open(os.path.join(OUT, name)).convert("RGBA"))


def icon(rows, extra=None):
    pal = {"o": OUTB, "a": RIM, "b": GOLD, "c": BRZ, "w": LS[0], "g": LS[2], "r": CIN, "t": LEATH, "u": LEATH_D, "j": JADE_HI, "k": JADE}
    return art(rows, pal).a


ICON_HAMMER = icon([
    "................",
    "...ooooooo......",
    "..owwwggggo.....",
    "..owwgggggco....",
    "..owgggggcco....",
    "..ooooggcooo....",
    "......otuo......",
    "......otuo......",
    "......otuo......",
    "......otuo......",
    "......otuo......",
    "......otuo......",
    "......ootuo.....",
    ".......oooo.....",
    "................",
    "................",
])
ICON_SWORD = icon([
    "............oo..",
    "...........owwo.",
    "..........owwgo.",
    ".........owwgo..",
    "........owwgo...",
    ".......owwgo....",
    "..o...owwgo.....",
    "..oo.owwgo......",
    "...ooowgo.......",
    "....oacao.......",
    "....oaaco.......",
    "...otocao.......",
    "..otoo.oo.......",
    "..ooo...........",
    "................",
    "................",
])
def _shield():
    im = Img(16, 16)
    m = np.zeros((16, 16), bool)
    for y in range(1, 15):
        hw = 6.4 if y <= 8 else 6.4 - (y - 8) * 1.05
        for x in range(16):
            if abs(x + 0.5 - 8) <= hw:
                m[y, x] = True
    bevel_shape(im, m, GOLD, RIM, BRZ)
    inner = layers(m) >= 2
    bevel_shape(im, inner, LAP, LAP2, LAP_D)
    for y in range(3, 12):
        im.p(7, y, GOLD)
        im.p(8, y, BRZ)
    for x in (5, 6, 7, 8, 9, 10):
        im.p(x, 5, GOLD if x < 8 else BRZ)
    im.paint(dilate(m) & ~m, OUTB)
    return im.a


ICON_SHIELD = _shield()


def _hammer24():
    im = Img(24, 24)
    Y, X = np.mgrid[0:24, 0:24]
    px, py = X + 0.5, Y + 0.5
    # handle: diagonal bar from (5,19) to (15,9)
    t = ((px - 5) * 10 + (py - 19) * -10) / 200.0
    dist = np.abs((px - 5) * 10 + (py - 19) * 10) / math.hypot(10, 10)   # perpendicular to the (1,-1) axis
    handle = (t >= -0.02) & (t <= 1.0) & (dist <= 1.8)
    bevel_shape(im, handle, LEATH, LEATH_H, LEATH_D)
    # head: rotated block centred (15,9)
    u = ((px - 15) + (py - 9) * -1) / math.sqrt(2)      # along the handle
    v = ((px - 15) + (py - 9)) / math.sqrt(2)            # across
    head = (np.abs(u) <= 3.4) & (v >= -7.0) & (v <= 7.0)
    # note: head spans across the handle direction
    head = (np.abs(u) <= 3.6) & (np.abs(v) <= 6.2)
    bevel_shape(im, head, LS[1], LS[0], LS[3])
    im.paint(head & (np.abs(u) <= 1.0), LS[2])
    im.paint(handle & ~head & False, LEATH)
    im.outline(STONE_OUT)
    return im.a


ICON_HAMMER24 = _hammer24()

ICON_HORN = icon([
    "................",
    "................",
    "...........oo...",
    "..........oaco..",
    ".........oabco..",
    "........oabbco..",
    ".......oabbco...",
    ".....ooabbco....",
    "...ooaabbco.....",
    "..oaaabbcco.....",
    "..oabbbcco......",
    "..oooccoo.......",
    "....oooo........",
    "................",
    "................",
    "................",
])
ICON_FIST = icon([
    "................",
    "...oooooooo.....",
    "..owwwwwwwwo....",
    "..owgowgowgoo...",
    "..owgowgowgwo...",
    "..owwwwwwwwwo...",
    "..owgggggggwo...",
    "..owwwwwwwwwo...",
    "...owwwwwwwo....",
    "....owgggwo.....",
    "....oggggo......",
    "....oooooo......",
    "................",
    "................",
    "................",
    "................",
])


def weather_icon():
    im = Img(24, 24)
    sun = Img(24, 24)
    sun_disc(sun, 9.0, 9.0, 4.5, 8.8, 8, dict(hi=RIM, mid=GOLD, lo=BRZ), True)
    sun.outline(OUTB)
    im.a = sun.a.copy()
    Y, X = np.mgrid[0:24, 0:24]
    cl = (((X + .5 - 14) / 7) ** 2 + ((Y + .5 - 16) / 4) ** 2 <= 1) | (((X + .5 - 10) / 4.5) ** 2 + ((Y + .5 - 17) / 3.5) ** 2 <= 1) | (((X + .5 - 16) / 4) ** 2 + ((Y + .5 - 13) / 3.5) ** 2 <= 1)
    cl_img = Img(24, 24)
    bevel_shape(cl_img, cl, H("e6eef2"), H("ffffff"), LAP2)
    cl_img.outline(LAP_D)
    blit(im.a, cl_img.a, 0, 0)
    a = im.a
    return a


def preview():
    rng = np.random.default_rng(1212)
    W, Hh = 640, 360
    bg = np.zeros((Hh, W, 4), np.uint8)
    base = np.array(H("c99a32"), np.int16)
    n = rng.integers(-7, 8, (Hh, W, 1))
    bg[..., :3] = np.clip(base[:3] + n, 0, 255)
    bg[..., 3] = 255
    for _ in range(900):
        x, y = int(rng.integers(0, W)), int(rng.integers(0, Hh))
        c = np.array(base[:3]) + rng.choice([-16, -10, 12, 16])
        bg[y, x, :3] = np.clip(c, 0, 255)
        if rng.random() < 0.5 and y > 0:
            bg[y - 1, x, :3] = np.clip(c + 6, 0, 255)

    # --- top-left: sun dial
    blit(bg, load("sundial_base.png"), 6, 6)
    blit(bg, weather_icon(), 6 + 20, 6 + 20)
    nd = Image.fromarray(load("sundial_needle.png"), "RGBA").rotate(-30, resample=Image.NEAREST)
    blit(bg, np.array(nd), 6, 6)
    on, off = load("sundial_day_pip_on.png"), load("sundial_day_pip_off.png")
    for i in range(7):
        blit(bg, on if i < 3 else off, 76 + i * 8, 12)
    sk = load("raid_skull_pip.png")
    for i in range(3):
        a = sk.copy()
        if i == 2:
            a[..., :3] = (a[..., :3] * 0.35).astype(np.uint8)
        blit(bg, a, 76 + i * 11, 24)
    # --- bars under dial
    def bar(frame, trough, x, y, wx0, wx1, parts):
        blit(bg, load(trough), x, y)
        for (name, frac) in parts:
            col = load(name)
            wpx = int((wx1 - wx0 + 1) * frac)
            for i in range(wpx):
                blit(bg, col[:, i % col.shape[1]:i % col.shape[1] + 1], x + wx0 + i, y + 2)
        blit(bg, load(frame), x, y)
    bar("bar_hp_vessel_frame.png", "bar_hp_trough.png", 6, 76, 12, 73, [("bar_hp_fill.png", 0.62)])
    bar("bar_stamina_frame.png", "bar_stamina_trough.png", 6, 92, 8, 87, [("bar_stamina_fill_load.png", 0.8), ("bar_stamina_fill_bright.png", 0.52)])
    blit(bg, load("bar_rage_flame_1.png"), 106, 68)
    # status chips (16px) with stack dots
    for i, (c1, c2) in enumerate(((JADE_HI, JADE), (LAP2, LAP), (CIN_HI, CIN))):
        chip = Img(16, 16)
        disc_shaded(chip, 8, 8, 6.4, c2, c1, darken(c2, 0.6), outline=OUTB)
        glyph = (["..X..", ".XXX.", "XXXXX", "XXXXX", ".XXX."], ["X...X", ".X.X.", "..X..", ".X.X.", "X...X"], ["..X..", ".XXX.", "XXXXX", ".XXX.", "..X.."])[i]
        for gy, row in enumerate(glyph):
            for gx, ch in enumerate(row):
                if ch == "X":
                    chip.p(5 + gx, 5 + gy, P0 if i != 1 else GLINT)
        blit(bg, chip.a, 6 + i * 18, 108)
        for d in range(i + 1):
            blit(bg, on[1:5, 1:5] if False else on, 8 + i * 18 + d * 4, 126) if False else None
            bg[126, 9 + i * 18 + d * 3, :3] = GLINT[:3]
            bg[127, 9 + i * 18 + d * 3, :3] = BRZ[:3]
    # --- top-right: coin bar, herd ring, morale
    blit(bg, nine_slice(load("panel_bronze.png"), 86, 28), 456, 6)
    blit(bg, load("coin_icon.png"), 463, 12)
    draw_text(bg, "12,450", 483, 15, RIM)
    ring_a = load("herd_ring_gauge_dim.png").copy()
    full = load("herd_ring_gauge.png")
    dx, dy, r, ang = polar(32, 32, 16, 16)
    lit = (ang <= 112 / 200 * 360) & (r > 9.8) & (r < 13.6)
    ring_a[lit] = full[lit]
    blit(bg, ring_a, 550, 6)
    blit(bg, load("herd_pighead_16.png"), 558, 14)
    blit(bg, load("medallion_small_normal.png"), 590, 6)
    blit(bg, load("morale_face_2.png"), 598, 14)
    # --- bottom-left: joystick
    blit(bg, load("joystick_base.png"), 12, 250)
    blit(bg, load("joystick_knob.png"), 12 + 28 + 9, 250 + 28 - 7)
    # --- bottom-right: context medallion + skills + horn
    cx0, cy0 = 576, 300
    blit(bg, load("medallion_large_normal.png"), cx0, cy0)
    blit(bg, ICON_HAMMER24, cx0 + 12, cy0 + 12)
    skills = [((520, 304), "normal", ICON_SWORD), ((538, 262), "normal", ICON_SHIELD), ((584, 244), "normal", ICON_FIST)]
    for (p, st, ic) in skills:
        blit(bg, load("medallion_small_%s.png" % st), p[0], p[1])
        blit(bg, ic, p[0] + 8, p[1] + 8)
    # cooldown on skill 2 (frame 3), ready pulse on skill 3, whistle disabled
    blit(bg, load("cooldown_wedge_small_3.png"), 538, 262)
    blit(bg, load("ready_pulse_small_1.png"), 584, 244)
    blit(bg, load("medallion_small_disabled.png"), 482, 316)
    hg = ICON_HORN.copy()
    gr = (hg[..., :3].astype(int).sum(axis=2) // 3)[..., None]
    hg[..., :3] = (gr * 0.8).astype(np.uint8)
    blit(bg, hg, 490, 324)
    # --- space bag on the right (3 sockets, two filled with item sprites)
    lay = bag_layout()[3]
    bx, by = 548, 124
    blit(bg, load("bag_satchel.png"), bx + 24, by + 24)
    items = [(JADE_HI, JADE), None, (CIN_HI, CIN)]
    for i, (sx, sy) in enumerate(lay):
        if items[i] is None:
            blit(bg, load("bag_socket_empty.png"), bx + sx, by + sy)
        else:
            blit(bg, load("bag_socket_empty.png"), bx + sx, by + sy)
            it = Img(14, 14)
            disc_shaded(it, 7, 7, 3.6, items[i][1], items[i][0], darken(items[i][1], 0.6), outline=OUTB)
            blit(bg, it.a, bx + sx, by + sy)
            blit(bg, load("bag_socket_filled_rim.png"), bx + sx, by + sy)
    # --- bottom-centre examples of the panels
    blit(bg, nine_slice(load("panel_parchment.png"), 112, 48), 236, 304)
    for i in range(3):
        bg[320 + i * 6, 252:330, :3] = PINK[:3]
    blit(bg, nine_slice(load("panel_stone_tablet.png"), 64, 48), 356, 304)
    blit(bg, nine_slice(load("panel_bronze.png"), 56, 48), 426, 304)
    img = Image.fromarray(bg, "RGBA")
    big = img.resize((W * 3, Hh * 3), Image.NEAREST)
    big.save(os.path.join(OUT, "preview_hud_x3.png"))
    KIT.append({"file": "preview_hud_x3.png", "size": [W * 3, Hh * 3], "note": "640x360 logical mock HUD at 3x NEAREST; preview only, not an engine asset"})


def main():
    os.makedirs(OUT, exist_ok=True)
    panel_bronze()
    panel_parchment()
    panel_stone()
    sundial_base()
    sundial_needle()
    pips()
    bars()
    rage_flames()
    coin()
    faces()
    herd()
    joystick()
    knobs()
    medallions()
    satchel()
    bag_rings()
    preview()
    manifest = {
        "version": 1,
        "generator": "tools/pixelart/ui/gen_hud_kit.py",
        "scale": "author at 1x, display at integer scale only, filter=Point, no compression",
        "bagLayouts": {"3": bag_layout()[3], "8": bag_layout()[8]},
        "files": sorted(KIT, key=lambda e: e["file"]),
    }
    with open(os.path.join(OUT, "kit.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=1)
    print("wrote %d files to %s" % (len(KIT), OUT))


if __name__ == "__main__":
    main()
