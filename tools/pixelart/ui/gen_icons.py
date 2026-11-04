#!/usr/bin/env python
"""Pig Tycoon 2D - procedural HUD icon set (24x24 primary, 16x16 hand-tuned small).

Deterministic: no randomness.  numpy + PIL only.  Style: Docs/art-direction/2d-ui-style-guide.md
(worn sun-gold / bronze, light from upper-left, 1px outline in the darkest neighbouring ink,
a gold-leaf rim highlight on every top-left edge so the whole set shares one family).

Writes   Assets/Art/PixelMyth/UI/Icons/<group>/<name>_24.png and _16.png, icons.json, preview PNGs.
Rule: no letters or digits are ever drawn inside an icon (labels exist only in the preview sheet margin).
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Art', 'PixelMyth', 'UI', 'Icons')


# ----------------------------------------------------------------------------- colour
def hx(s):
    return tuple(int(s[i:i + 2], 16) for i in (1, 3, 5))


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


RIM = hx('#f4d27a')          # sun-bronze rim language (style guide)


def ramp(base):
    """(rim-hi, light, base, shade, dark, ink) - warm shifted, shared by all materials."""
    b = hx(base)
    hi = mix(mix(b, (255, 244, 205), .55), RIM, .35)
    lt = mix(b, (255, 236, 180), .24)
    sh = mix(b, (74, 38, 54), .30)
    dk = mix(b, (46, 26, 20), .58)
    ink = mix(b, (28, 14, 12), .80)
    return (hi, lt, b, sh, dk, ink)


BASES = dict(
    gold='#e2b44c', sun='#f6cd52', orange='#e48a2c', flame='#d9622a', cinn='#a8382a', red='#c8442e',
    ruby='#c8344e', pink='#eaa89c', pinkd='#cc7f76', snout='#f2c2b4', stone='#ddd2b6', stoned='#a3967b',
    lapis='#3a6f9c', lapisl='#6aa8d0', ice='#c5e6ee', jade='#3f8a6a', leaf='#6aa04a', wood='#9a6434',
    woodd='#6a4224', steel='#a7aeb0', iron='#5f6a74', cloud='#e8e1d0', cloudg='#9fa4ac', storm='#6c6484',
    parch='#efe0bb', mud='#6f4e32', moon='#f2e7b6', toxic='#b8d63a', purple='#8c70b0', skin='#e0b27c',
    white='#f3eee2', bronze='#b07a2c', deep='#6e4518', umber='#3d2618', mist='#c8d4d8', sand='#dcb868',
    dry='#9a6a30', olive='#8a8f70', hill='#6f8038', bone='#e9e0c8', teal='#7fc4b0', straw='#d9b050',
    brick='#b4583a', lilac='#c4b4dc', night='#2d3a66', sundim='#b89a5a', sick='#8aa84a', rain='#5b9cc6',
    rainb='#8fa3b8', dusk='#e8642c', glass='#bcd8e0',
)
M = {k: ramp(v) for k, v in BASES.items()}
INK = (42, 26, 18)
HL = (255, 244, 214)


# ----------------------------------------------------------------------------- canvas
def shift(m, dx, dy):
    """out[y, x] = m[y-dy, x-dx], zero filled."""
    out = np.zeros_like(m)
    h, w = m.shape
    ys, ye = max(0, dy), min(h, h + dy)
    xs, xe = max(0, dx), min(w, w + dx)
    out[ys:ye, xs:xe] = m[ys - dy:ye - dy, xs - dx:xe - dx]
    return out


def dil4(m):
    return m | shift(m, 1, 0) | shift(m, -1, 0) | shift(m, 0, 1) | shift(m, 0, -1)


class Cv:
    def __init__(s, n):
        s.n = n
        s.a = np.zeros((n, n, 4), np.uint8)
        s.Y, s.X = np.mgrid[0:n, 0:n].astype(float)

    # ---- masks (pixel centres at integer coordinates)
    def z(s):
        return np.zeros((s.n, s.n), bool)

    def circ(s, cx, cy, r):
        return (s.X - cx) ** 2 + (s.Y - cy) ** 2 <= r * r + 1e-6

    def ell(s, cx, cy, rx, ry):
        return ((s.X - cx) / rx) ** 2 + ((s.Y - cy) / ry) ** 2 <= 1 + 1e-6

    def rect(s, x0, y0, x1, y1):
        return (s.X >= x0 - 1e-6) & (s.X <= x1 + 1e-6) & (s.Y >= y0 - 1e-6) & (s.Y <= y1 + 1e-6)

    def rrect(s, x0, y0, x1, y1):
        m = s.rect(x0, y0, x1, y1)
        for (x, y) in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
            m &= ~((abs(s.X - x) < .1) & (abs(s.Y - y) < .1))
        return m

    def line(s, pts, w=1.0):
        m = s.z()
        r = max(w / 2.0, 0.5) + 0.12
        for i in range(len(pts) - 1):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            dx, dy = x1 - x0, y1 - y0
            L2 = dx * dx + dy * dy
            if L2 < 1e-9:
                t = np.zeros_like(s.X)
            else:
                t = np.clip(((s.X - x0) * dx + (s.Y - y0) * dy) / L2, 0, 1)
            px, py = x0 + t * dx, y0 + t * dy
            m |= (s.X - px) ** 2 + (s.Y - py) ** 2 <= r * r
        return m

    def poly(s, pts):
        m = s.z()
        n = len(pts)
        for i in range(n):
            x1, y1 = pts[i]
            x2, y2 = pts[(i + 1) % n]
            cond = (y1 > s.Y) != (y2 > s.Y)
            xi = (x2 - x1) * (s.Y - y1) / ((y2 - y1) if y2 != y1 else 1e-9) + x1
            m ^= cond & (s.X < xi)
        return m | s.line(list(pts) + [pts[0]], 1.0)

    def sector(s, cx, cy, r0, r1, a0, a1):
        ang = np.degrees(np.arctan2(s.Y - cy, s.X - cx)) % 360
        d2 = (s.X - cx) ** 2 + (s.Y - cy) ** 2
        ring = (d2 >= r0 * r0) & (d2 <= r1 * r1)
        a0m, a1m = a0 % 360, a1 % 360
        if a1 - a0 >= 360:
            return ring
        sel = (ang >= a0m) & (ang <= a1m) if a0m <= a1m else (ang >= a0m) | (ang <= a1m)
        return ring & sel

    def taper(s, pts, ws):
        m = s.z()
        for i in range(len(pts) - 1):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            w0, w1 = ws[i], ws[i + 1]
            L = max(1, int(math.hypot(x1 - x0, y1 - y0) * 3))
            for t in range(L + 1):
                u = t / L
                m |= s.circ(x0 + (x1 - x0) * u, y0 + (y1 - y0) * u, (w0 + (w1 - w0) * u) / 2)
        return m

    # ---- painting
    def paint(s, m, col):
        s.a[m] = (col[0], col[1], col[2], 255)

    def p(s, x, y, col):
        xi, yi = int(math.floor(x + .5)), int(math.floor(y + .5))
        if 0 <= xi < s.n and 0 <= yi < s.n:
            s.a[yi, xi] = (col[0], col[1], col[2], 255)

    def ps(s, pts, col):
        for x, y in pts:
            s.p(x, y, col)

    def dither(s, m, col, phase=0):
        chk = ((s.X.astype(int) + s.Y.astype(int) + phase) % 2) == 0
        s.paint(m & chk, col)

    def fill(s, m, mat, sh='round', ol=True, ink=None):
        """Outlined, shaded fill.  sh: round | flat | plain.  Upper-left rim-hi, lower-right shade."""
        hi, lt, b, shd, dk, ink0 = mat
        if not m.any():
            return
        if ol:
            s.paint(dil4(m) & ~m, ink or ink0)
        ys, xs = np.nonzero(m)
        x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
        w, h = x1 - x0 + 1, y1 - y0 + 1
        s.paint(m, b)
        if sh == 'plain':
            return
        tl = m & (~shift(m, 1, 0) | ~shift(m, 0, 1))
        br = m & (~shift(m, -1, 0) | ~shift(m, 0, -1))
        if sh == 'round' and min(w, h) >= 4:
            d = (s.X - (x0 + x1) / 2) / w + (s.Y - (y0 + y1) / 2) / h
            s.paint(m & (d < -.30), lt)
            s.paint(m & (d > .30), shd)
            s.paint(br & ~tl, dk if min(w, h) >= 6 else shd)
        else:
            s.paint(br & ~tl, shd)
        s.paint(tl, hi)

    def center(s):
        """Centre the opaque bounding box on the canvas (whole pixels only)."""
        ys, xs = np.nonzero(s.a[:, :, 3])
        if not len(xs):
            return
        dx = int(round(((s.n - 1) - (xs.min() + xs.max())) / 2.0))
        dy = int(round(((s.n - 1) - (ys.min() + ys.max())) / 2.0))
        b = np.zeros_like(s.a)
        n = s.n
        b[max(0, dy):n + min(0, dy), max(0, dx):n + min(0, dx)] = \
            s.a[max(0, -dy):n - max(0, dy), max(0, -dx):n - max(0, dx)]
        s.a = b

    def out(s):
        return Image.fromarray(s.a, 'RGBA')


# ----------------------------------------------------------------------------- shared parts
def rot(pts, cx, cy, deg):
    a = math.radians(deg)
    ca, sa = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * ca - (y - cy) * sa, cy + (x - cx) * sa + (y - cy) * ca) for x, y in pts]


def ray(c, cx, cy, ang, r0, r1, hw):
    a = math.radians(ang)
    ux, uy = math.cos(a), math.sin(a)
    px, py = -uy, ux
    return c.poly([(cx + ux * r0 + px * hw, cy + uy * r0 + py * hw),
                   (cx + ux * r1, cy + uy * r1),
                   (cx + ux * r0 - px * hw, cy + uy * r0 - py * hw)])


def star4(c, cx, cy, r, mat, ol=True):
    k = max(1.0, r * .34)
    c.fill(c.poly([(cx, cy - r), (cx + k, cy - k), (cx + r, cy), (cx + k, cy + k), (cx, cy + r),
                   (cx - k, cy + k), (cx - r, cy), (cx - k, cy - k)]), mat, 'flat', ol)


def drop(c, cx, cy, h, mat, ol=True):
    r = h * .32
    m = c.circ(cx, cy + h / 2 - r - .3, r + .35) | c.poly([(cx, cy - h / 2), (cx - r, cy + h / 2 - r * 1.4),
                                                             (cx + r, cy + h / 2 - r * 1.4)])
    c.fill(m, mat, 'round', ol)
    c.p(cx - r * .45, cy + h / 2 - r * 1.3, HL)


def cloud_m(c, x, y, w, h):
    y1 = y + h - 1
    r = h * .33
    m = c.rect(x + r, y1 - 2 * r, x + w - 1 - r, y1)
    m |= c.circ(x + r, y1 - r, r + .3) | c.circ(x + w - 1 - r, y1 - r, r + .3)
    m |= c.circ(x + w * .38, y + h * .46, h * .42) | c.circ(x + w * .67, y + h * .52, h * .33)
    return m


def cloud(c, x, y, w, h, mat):
    c.fill(cloud_m(c, x, y, w, h), mat)


def bolt(c, pts, mat=None):
    c.fill(c.poly(pts), mat or M['sun'], 'flat')


def trefoil(c, cx, cy, r, bg, blade=None):
    c.fill(c.circ(cx, cy, r), bg)
    k = blade or M['umber']
    for a in (90, 210, 330):
        c.paint(c.sector(cx, cy, r * .26, r * .82, a - 25, a + 25), k[2])
    c.paint(c.circ(cx, cy, max(1.0, r * .2)), k[2])
    c.paint(c.circ(cx, cy, max(0.4, r * .2 - 1.2)), bg[1])


def pig(c, cx, cy, r, mat=None, eyes='dot', ears=True):
    mat = mat or M['pink']
    if ears:
        for sg in (-1, 1):
            c.fill(c.poly([(cx + sg * r * .30, cy - r * .85), (cx + sg * r * 1.05, cy - r * .35),
                           (cx + sg * r * 1.0, cy - r * 1.18)]), M['pinkd'], 'flat')
    c.fill(c.circ(cx, cy, r), mat)
    c.fill(c.ell(cx, cy + r * .38, r * .52, r * .38), M['snout'], 'flat', ol=r >= 5)
    ey = cy - r * .22
    ex = r * .5
    if eyes == 'dot':
        c.ps([(cx - ex, ey), (cx + ex, ey)], INK)
    elif eyes == 'wide':
        for sg in (-1, 1):
            c.fill(c.circ(cx + sg * ex, ey, max(1.0, r * .24)), M['white'], 'plain', ol=r >= 5)
            c.p(cx + sg * ex, ey, INK)
    elif eyes == 'closed':
        for sg in (-1, 1):
            c.ps([(cx + sg * ex - 1, ey + .5), (cx + sg * ex, ey + 1), (cx + sg * ex + 1, ey + .5)] if r >= 5
                 else [(cx + sg * ex, ey + .5)], INK)
    elif eyes == 'sick':
        for sg in (-1, 1):
            c.ps([(cx + sg * ex - 1, ey - .5), (cx + sg * ex, ey), (cx + sg * ex + 1, ey + .5)] if r >= 5
                 else [(cx + sg * ex, ey)], INK)
    ns = r * .2
    c.ps([(cx - ns, cy + r * .4), (cx + ns, cy + r * .4)], M['pinkd'][4])


def thermo(c, x, y0, y1, bulb, fill=None, big=True):
    w = 1.6 if big else 1.0
    tube = c.rrect(x - w - .5, y0, x + w + .5, y1) | c.circ(x, y1 + bulb * .7, bulb)
    c.fill(tube, M['glass'], 'flat')
    fm = c.rect(x - .5, y0 + (y1 - y0) * .35, x + .5, y1) | c.circ(x, y1 + bulb * .7, bulb - 1)
    c.paint(fm, (fill or M['red'])[2])
    c.p(x - 1, y1 + bulb * .7 - 1, HL)


# ----------------------------------------------------------------------------- registry
ICONS = []


def icon(group, name, vi, src):
    def deco(f):
        ICONS.append((group, name, vi, src, f))
        return f
    return deco


G = 'GDD v6.0'
W_SRC = G + ' L826-L836 (thoi tiet); checklist 8'
S_SRC = G + ' L829-L836, L881, L895, L899-L901 (trang thai nhan vat); checklist 8'


# ============================================================================= WEATHER
@icon('weather', 'quang_dang', 'Quang Đãng: trời quang, nắng dịu.', W_SRC)
def quang_dang(c):
    if c.n == 24:
        cx = cy = 11.5
        m = c.z()
        for k in range(8):
            a = k * 45
            m |= ray(c, cx, cy, a, 6.4, 10.4 if k % 2 == 0 else 9.0, 1.7 if k % 2 == 0 else 1.3)
        c.fill(m, M['gold'], 'flat')
        c.fill(c.circ(cx, cy, 5.4), M['sun'])
        c.paint(c.circ(cx - .5, cy - .5, 2.2), M['sun'][1])
    else:
        cx = cy = 7.5
        m = c.z()
        for k in range(8):
            m |= ray(c, cx, cy, k * 45, 4.8, 6.9 if k % 2 == 0 else 6.0, 1.0 if k % 2 == 0 else .7)
        c.fill(m, M['gold'], 'flat')
        c.fill(c.circ(cx, cy, 3.4), M['sun'])


@icon('weather', 'nhieu_may', 'Nhiều Mây: trời nhiều mây, không nắng.', W_SRC)
def nhieu_may(c):
    if c.n == 24:
        cloud(c, 8, 3, 15, 10, M['cloudg'])
        cloud(c, 1, 9, 18, 12, M['cloud'])
    else:
        cloud(c, 6, 2, 9, 7, M['cloudg'])
        cloud(c, 1, 6, 11, 8, M['cloud'])


@icon('weather', 'nang_gat', 'Nắng Gắt: nóng bức, không khí rung lên (tăng nhu cầu nước).', W_SRC)
def nang_gat(c):
    if c.n == 24:
        cx, cy = 11.5, 8.5
        m = c.z()
        for k in range(12):
            a = k * 30
            m |= ray(c, cx, cy, a, 5.6, 8.4, 1.1)
        c.fill(m, M['orange'], 'flat')
        c.fill(c.circ(cx, cy, 4.6), M['flame'])
        c.paint(c.circ(cx - .5, cy - .5, 2.0), M['orange'][1])
        for yy in (17, 21):
            pts = [(x, yy + 1.1 * math.sin(x * .62 + (0 if yy == 17 else 2))) for x in range(2, 22)]
            c.fill(c.line(pts, 1), M['sand'], 'plain')
    else:
        cx, cy = 7.5, 5.0
        m = c.z()
        for k in range(8):
            m |= ray(c, cx, cy, k * 45, 3.9, 5.2, .9)
        c.fill(m, M['orange'], 'flat')
        c.fill(c.circ(cx, cy, 2.9), M['flame'])
        for yy, ph in ((11, 0), (14, 2)):
            pts = [(x, yy + .9 * math.sin(x * .9 + ph)) for x in range(2, 14)]
            c.paint(c.line(pts, 1), M['sand'][2])


@icon('weather', 'mua', 'Mưa: trời mưa.', W_SRC)
def mua(c):
    if c.n == 24:
        cloud(c, 2, 2, 19, 11, M['rainb'])
        for x, y, h in ((6, 18, 6), (12, 20, 6), (18, 18, 6), (9, 16.5, 3)):
            drop(c, x, y, h, M['rain'])
    else:
        cloud(c, 1, 1, 14, 8, M['rainb'])
        for x, y in ((4, 12.5), (8, 14), (12, 12.5)):
            drop(c, x, y, 4, M['rain'])


@icon('weather', 'giong_bao', 'Giông Bão: mây đen, sấm sét (heo hoảng, choáng).', W_SRC)
def giong_bao(c):
    if c.n == 24:
        cloud(c, 1, 1, 21, 11, M['storm'])
        bolt(c, [(14, 9), (7, 15), (11, 15), (8, 23), (18, 12), (13, 12), (17, 9)])
        drop(c, 4, 18, 5, M['rain'])
        drop(c, 20, 18, 5, M['rain'])
    else:
        cloud(c, 0, 0, 16, 8, M['storm'])
        bolt(c, [(9, 7), (4, 11), (7, 11), (5, 15), (11, 9), (8, 9), (10, 7)])
        c.p(2, 12, M['rain'][1])
        c.p(14, 12, M['rain'][1])


@icon('weather', 'suong_mu', 'Sương Mù: tầm nhìn bị che (minimap giới hạn 15 m).', W_SRC)
def suong_mu(c):
    if c.n == 24:
        c.fill(c.circ(15.5, 6.5, 4.6), M['sundim'], 'round')
        for (x0, y0, x1, y1) in ((2, 10, 18, 12), (6, 14, 22, 16), (3, 18, 19, 20)):
            c.fill(c.rrect(x0, y0, x1, y1), M['mist'], 'flat')
        c.paint(c.rect(7, 10, 14, 10), M['mist'][0])
    else:
        c.fill(c.circ(10.5, 3.5, 2.8), M['sundim'], 'plain')
        for (x0, y0, x1, y1) in ((1, 6, 11, 7), (4, 9, 14, 10), (2, 12, 12, 13)):
            c.fill(c.rrect(x0, y0, x1, y1), M['mist'], 'flat')


@icon('weather', 'ret_dam', 'Rét Đậm: giá rét, tinh thể băng.', W_SRC)
def ret_dam(c):
    if c.n == 24:
        cx = cy = 11.5
        m = c.z()
        for k in range(6):
            a = math.radians(90 + 60 * k)
            ex, ey = cx + 10 * math.cos(a), cy + 10 * math.sin(a)
            m |= c.line([(cx, cy), (ex, ey)], 1)
            bx, by = cx + 6.4 * math.cos(a), cy + 6.4 * math.sin(a)
            for s2 in (-1, 1):
                b = a + s2 * math.radians(55)
                m |= c.line([(bx, by), (bx + 3.2 * math.cos(b), by + 3.2 * math.sin(b))], 1)
        c.fill(m, M['ice'], 'plain', ink=M['lapis'][4])
        c.fill(c.circ(cx, cy, 2.3), M['lapisl'])
    else:
        cx = cy = 7.5
        m = c.z()
        for k in range(6):
            a = math.radians(90 + 60 * k)
            m |= c.line([(cx, cy), (cx + 6.4 * math.cos(a), cy + 6.4 * math.sin(a))], 1)
        c.fill(m, M['ice'], 'plain', ink=M['lapis'][4])
        c.fill(c.circ(cx, cy, 1.5), M['lapisl'], 'plain', ol=False)
        for k in range(6):
            a = math.radians(90 + 60 * k + 30)
            c.p(cx + 2.8 * math.cos(a), cy + 2.8 * math.sin(a), M['ice'][1])


@icon('weather', 'nom_am', 'Nồm Ẩm: tường rịn nước, sàn trơn (trượt ngã 10%).', W_SRC)
def nom_am(c):
    if c.n == 24:
        c.fill(c.rect(1, 3, 22, 21), M['stone'], 'flat')
        for y in (8, 13, 18):
            c.paint(c.rect(2, y, 21, y), M['stoned'][3])
        for i, y in enumerate((3, 8, 13)):
            off = 5 if i % 2 else 10
            for xx in (off, off + 8, off - 8):
                if 1 < xx < 22:
                    c.paint(c.rect(xx, y + 1, xx, y + 4), M['stoned'][3])
        c.dither(c.rect(2, 16, 21, 20), M['stoned'][2], 0)
        c.paint(c.rect(7, 4, 7, 8), M['lapisl'][1])
        drop(c, 7, 11, 7, M['rain'])
        c.paint(c.rect(16, 9, 16, 12), M['lapisl'][1])
        drop(c, 16, 16, 7, M['rain'])
        drop(c, 11, 19, 4, M['rain'])
    else:
        c.fill(c.rect(1, 2, 14, 13), M['stone'], 'flat')
        for y in (6, 10):
            c.paint(c.rect(2, y, 13, y), M['stoned'][3])
        drop(c, 5, 8, 5, M['rain'])
        drop(c, 10, 11, 4, M['rain'])


@icon('weather', 'gio_kho', 'Gió Khô: gió hanh, bụi và lá khô (Khô Rát, tốn Stamina).', W_SRC)
def gio_kho(c):
    if c.n == 24:
        for pts in ([(2, 8), (13, 8), (16, 7), (17, 5), (15, 3), (13, 4), (13, 6)],
                    [(5, 13), (18, 13), (21, 12), (21, 10), (19, 9)],
                    [(2, 18), (11, 18), (14, 19), (14, 21), (12.5, 21.5), (11, 20.5)]):
            c.fill(c.line(pts, 1.8), M['sand'], 'flat')
        c.fill(c.poly([(17, 17), (21, 15.5), (22.5, 19), (18.5, 20.5)]), M['dry'], 'flat')
        c.line([(18, 19), (21, 17)], 1)
        c.paint(c.line([(18, 19), (21.5, 17.5)], 1), M['dry'][4])
        c.ps([(3, 4), (8, 3), (20, 5), (6, 22), (17, 22)], M['sand'][1])
    else:
        for pts in ([(1, 5), (8, 5), (10, 4), (10, 2), (8.5, 1.5)],
                    [(3, 10), (11, 10), (13, 9), (13, 7.5)]):
            c.fill(c.line(pts, 1), M['sand'], 'plain')
        c.fill(c.poly([(10, 12), (13.5, 11), (14.5, 14), (11, 14.5)]), M['dry'], 'flat')


@icon('weather', 'mua_buc_xa', 'Mưa Bức Xạ: mưa nhiễm xạ, vòng sáng cảnh báo (Nhiễm Xạ).', W_SRC)
def mua_buc_xa(c):
    if c.n == 24:
        cloud(c, 0, 1, 17, 10, M['olive'])
        for x, y in ((4, 15.5), (9, 18), (3, 21)):
            drop(c, x, y, 5, M['sick'])
        ring = c.circ(16.5, 16.5, 8) & ~c.circ(16.5, 16.5, 6.6)
        c.dither(ring, M['toxic'][1], 0)
        trefoil(c, 16.5, 16.5, 6.3, M['toxic'])
    else:
        cloud(c, 0, 0, 11, 6, M['olive'])
        drop(c, 3, 9.5, 4, M['sick'])
        drop(c, 7, 12.5, 4, M['sick'])
        trefoil(c, 10.5, 10.5, 4.7, M['toxic'])


# ============================================================================= TIME
T_SRC = G + ' L202, L821-L825 (buoi trong ngay); checklist 8 gio/buoi'


@icon('time', 'binh_minh', 'Bình minh: mặt trời mọc trên đồi (Sáng 06-10).', T_SRC)
def binh_minh(c):
    if c.n == 24:
        m = c.z()
        for a in (180, 202, 225, 247, 270, 292, 315, 337, 360):
            m |= ray(c, 11.5, 16.5, a, 7.2, 10.6 if a % 45 == 0 else 9.4, 1.4)
        c.fill(m & (c.Y <= 15.5), M['gold'], 'flat')
        c.fill(c.circ(11.5, 16.5, 6), M['sun'])
        c.fill(c.ell(11.5, 25, 14, 9), M['hill'])
        c.paint(c.rect(8, 18, 10, 18), M['hill'][1])
        star4(c, 3.5, 5, 2.5, M['moon'])
        star4(c, 20.5, 5, 2.5, M['moon'])
    else:
        m = c.z()
        for a in (180, 225, 270, 315, 360):
            m |= ray(c, 7.5, 11.5, a, 4.6, 7.2 if a % 90 == 0 else 6.4, 1.0)
        c.fill(m & (c.Y <= 10.5), M['gold'], 'flat')
        c.fill(c.circ(7.5, 11.5, 3.8), M['sun'])
        c.fill(c.ell(7.5, 17, 9, 6), M['hill'])


@icon('time', 'trua', 'Trưa: mặt trời đứng bóng, nắng chói (Trưa 10-14).', T_SRC)
def trua(c):
    if c.n == 24:
        cx, cy = 11.5, 8
        m = c.z()
        for k in range(8):
            m |= ray(c, cx, cy, k * 45, 5.8, 7.8 if k % 2 == 0 else 7.0, 1.2)
        c.fill(m, M['gold'], 'flat')
        c.fill(c.circ(cx, cy, 4.5), M['sun'])
        c.paint(c.circ(cx - .5, cy - .5, 2.2), M['white'][2])
        for x, ys in ((7, (16, 18)), (11, (15, 17, 19)), (12, (15, 17, 19)), (16, (16, 18))):
            for y in ys:
                c.p(x, y, M['sun'][1])
        c.fill(c.ell(11.5, 21.5, 7, 1.6), M['stoned'], 'flat')
        c.paint(c.ell(11.5, 21.5, 3.5, .8), M['stoned'][4])
    else:
        cx, cy = 7.5, 4.5
        m = c.z()
        for a in (0, 90, 180, 270):
            m |= ray(c, cx, cy, a, 3.6, 5.0, .9)
        c.fill(m, M['gold'], 'flat')
        c.fill(c.circ(cx, cy, 2.7), M['sun'])
        c.ps([(7, 9), (8, 9), (7, 11), (8, 11), (5, 10), (10, 10)], M['sun'][1])
        c.fill(c.ell(7.5, 13.5, 5, 1.2), M['stoned'], 'plain')


@icon('time', 'chieu', 'Chiều: hoàng hôn, mặt trời xuống mặt nước (Chiều 14-18).', T_SRC)
def chieu(c):
    if c.n == 24:
        m = c.z()
        for a in (180, 225, 270, 315, 360):
            m |= ray(c, 11.5, 15.5, a, 7.2, 10.4 if a % 90 == 0 else 9.2, 1.5)
        c.fill(m & (c.Y <= 14.5), M['orange'], 'flat')
        c.fill(c.circ(11.5, 15.5, 6) & (c.Y <= 16), M['dusk'])
        c.paint(c.circ(10, 13, 2) & (c.Y <= 16), M['orange'][1])
        c.fill(c.rect(0, 16, 23, 17), M['night'], 'flat')
        c.fill(c.rect(0, 19, 23, 20), M['night'], 'flat')
        c.paint(c.rect(7, 16.5, 16, 16.5), M['dusk'][1])
        c.paint(c.rect(9, 19.5, 14, 19.5), M['dusk'][1])
        c.paint(c.rect(11, 22, 12, 22), M['night'][3])
        c.paint(c.line([(17, 5), (19, 3.5), (21, 5)], 1), INK)
        c.paint(c.line([(3, 8), (4.5, 7), (6, 8)], 1), INK)
    else:
        m = c.z()
        for a in (180, 270, 360):
            m |= ray(c, 7.5, 9.5, a, 4.6, 6.8, 1.0)
        c.fill(m & (c.Y <= 8.5), M['orange'], 'flat')
        c.fill(c.circ(7.5, 9.5, 3.8) & (c.Y <= 10), M['dusk'])
        c.fill(c.rect(0, 10, 15, 11), M['night'], 'flat')
        c.fill(c.rect(0, 13, 15, 14), M['night'], 'flat')
        c.paint(c.rect(5, 10.5, 10, 10.5), M['dusk'][1])


@icon('time', 'dem', 'Đêm: trăng lưỡi liềm và sao (Tối 18-06).', T_SRC)
def dem(c):
    if c.n == 24:
        m = c.circ(11.5, 12, 9.6) & ~c.circ(16.5, 8.5, 8.2)
        c.fill(m, M['moon'])
        star4(c, 17.5, 15.5, 3, M['sun'])
        star4(c, 20, 6, 2, M['moon'])
        star4(c, 12.5, 4, 1.5, M['moon'], ol=False)
    else:
        m = c.circ(7, 8, 6.2) & ~c.circ(10.6, 5.6, 5.4)
        c.fill(m, M['moon'])
        star4(c, 12, 11, 2, M['sun'], ol=False)
        c.p(13, 4, M['moon'][1])


# ============================================================================= STATUS
@icon('status', 'cam_nang', 'Cảm Nắng: say nắng, người nóng bừng (nhiệt kế đỏ + mặt trời).', S_SRC)
def cam_nang(c):
    if c.n == 24:
        thermo(c, 8, 2, 14, 4.2)
        for y in (4, 7, 10):
            c.paint(c.rect(11, y, 12, y), M['bronze'][4])
        m = c.z()
        for k in range(8):
            m |= ray(c, 17.5, 6, k * 45, 3.2, 5.2, .9)
        c.fill(m, M['gold'], 'flat')
        c.fill(c.circ(17.5, 6, 2.5), M['sun'])
    else:
        thermo(c, 5, 1, 9, 2.6, big=False)
        m = c.z()
        for k in range(8):
            m |= ray(c, 11.5, 4.5, k * 45, 2.0, 3.6, .7)
        c.fill(m, M['gold'], 'flat')
        c.fill(c.circ(11.5, 4.5, 1.7), M['sun'], 'plain')


@icon('status', 'uot_lanh', 'Ướt Lạnh: ướt sũng và lạnh (giọt nước + tinh thể băng).', S_SRC)
def uot_lanh(c):
    if c.n == 24:
        drop(c, 11.5, 11.5, 21, M['lapis'])
        c.paint(c.line([(8, 12), (8, 15)], 1), M['lapisl'][1])
        star4(c, 12.5, 15, 3.5, M['ice'])
    else:
        drop(c, 7.5, 7.5, 14, M['lapis'])
        star4(c, 8, 10, 2.2, M['ice'], ol=False)


@icon('status', 'cam_gio', 'Cảm Gió: trúng gió, hắt hơi (gió + mặt tái xanh).', S_SRC)
def cam_gio(c):
    if c.n == 24:
        for pts in ([(1, 9), (7, 9), (9, 8), (9, 6), (7.5, 5.5)], [(1, 14), (9, 14), (11, 15), (11, 17), (9.5, 17.5)]):
            c.fill(c.line(pts, 1), M['teal'], 'plain')
        c.fill(c.circ(16, 12.5, 6), M['skin'])
        c.ps([(14, 10.5), (15, 10.5), (18, 10.5), (19, 10.5)], INK)
        c.paint(c.rect(15, 14, 17, 14), M['jade'][3])
        c.fill(c.circ(16, 13.5, 1), M['pinkd'], 'plain', ol=False)
        drop(c, 16, 18.6, 4, M['rain'])
        c.paint(c.rect(11, 10, 20, 11), M['skin'][3])
        c.ps([(14, 10.5), (15, 10.5), (18, 10.5), (19, 10.5)], INK)
    else:
        c.fill(c.line([(0, 6), (5, 6), (6.5, 5), (6.5, 3.5)], 1), M['teal'], 'plain')
        c.fill(c.circ(10.5, 8, 4), M['skin'])
        c.ps([(9, 7), (12, 7)], INK)
        drop(c, 10.5, 12.3, 3.2, M['rain'], ol=False)


@icon('status', 'nhiem_xa', 'Nhiễm Xạ: nhiễm phóng xạ (ký hiệu cánh quạt, tầng = chấm).', S_SRC)
def nhiem_xa(c):
    if c.n == 24:
        c.dither(c.circ(11.5, 11.5, 11.2) & ~c.circ(11.5, 11.5, 10), M['toxic'][1], 0)
        trefoil(c, 11.5, 11.5, 9.8, M['toxic'])
    else:
        c.fill(c.circ(7.5, 7.5, 7), M['toxic'])
        for a in (90, 210, 330):
            c.paint(c.sector(7.5, 7.5, 1.6, 5.6, a - 28, a + 28), M['umber'][2])
        c.paint(c.circ(7.5, 7.5, 1.4), M['umber'][2])


@icon('status', 'lo_do', 'Lờ Đờ: chậm chạp, ì ạch sau thuốc ngủ (ốc sên).', S_SRC)
def lo_do(c):
    if c.n == 24:
        c.fill(c.rrect(2, 16, 20, 20), M['skin'])
        c.fill(c.rrect(16, 9, 19, 19), M['skin'], 'flat')
        for x, y0 in ((17, 4), (20, 5)):
            c.fill(c.line([(x - (0 if x == 17 else 1), y0 + 3), (x - (0 if x == 17 else 1), 10)], 1), M['skin'], 'plain')
        c.ps([(17, 4), (20, 5)], INK)
        c.fill(c.circ(9, 11.5, 6.8), M['bronze'])
        for r0, r1, a0, a1 in ((4.6, 5.6, 40, 400), (2.6, 3.6, 120, 470)):
            c.paint(c.sector(9, 11.5, r0, r1, a0, a1), M['bronze'][4])
        c.p(9, 11.5, M['bronze'][4])
    else:
        c.fill(c.rrect(1, 11, 12, 14), M['skin'])
        c.fill(c.rect(11, 6, 13, 13), M['skin'], 'flat')
        c.ps([(11, 3), (13, 3)], INK)
        c.fill(c.circ(6, 7.5, 4.6), M['bronze'])
        c.paint(c.sector(6, 7.5, 2.2, 3.2, 60, 400), M['bronze'][4])


@icon('status', 'hu_hoa', 'Hư Hỏa: hỏa khí lệch, lửa hư lạnh (ngọn lửa lõi xanh rỗng).', S_SRC)
def hu_hoa(c):
    if c.n == 24:
        fl = c.poly([(12, 1), (14.5, 6), (19, 10), (20, 16), (17, 21), (11.5, 22.5), (6, 21), (3.5, 16), (5, 11),
                     (8, 12.5), (9.5, 7)])
        c.fill(fl, M['flame'])
        c.fill(c.poly([(11.5, 9), (14, 13), (15, 17), (11.5, 20), (8, 17), (9, 13)]), M['night'], 'plain', ol=False)
        c.fill(c.poly([(11.5, 12), (13, 15), (11.5, 18.5), (10, 15)]), M['lapisl'], 'plain', ol=False)
    else:
        fl = c.poly([(8, 1), (10, 5), (13, 8), (13, 12), (10, 14.5), (5, 14.5), (2.5, 11.5), (3, 8), (5.5, 9), (6.5, 5)])
        c.fill(fl, M['flame'])
        c.fill(c.poly([(8, 7), (10, 10), (8, 13), (6, 10)]), M['night'], 'plain', ol=False)
        c.p(8, 10, M['lapisl'][1])


@icon('status', 'met_doi', 'Mệt Dội: mệt mỏi chồng tầng (mặt thở dốc, mồ hôi).', S_SRC)
def met_doi(c):
    if c.n == 24:
        c.fill(c.circ(11, 13, 8.5), M['skin'])
        for x in (7.5, 14.5):
            c.fill(c.ell(x, 12.5, 2.6, 1.9), M['white'], 'plain', ol=False)
            c.paint(c.ell(x, 12.5, 2.6, 1.9) & (c.Y <= 12.2), M['woodd'][2])
            c.p(x, 13.3, INK)
            c.ps([(x - 2, 15), (x - 1, 15.5), (x, 15.5), (x + 1, 15.5), (x + 2, 15)], M['pinkd'][3])
        c.paint(c.line([(9, 19), (10.5, 18), (12.5, 18), (14, 19)], 1), M['umber'][2])
        drop(c, 19.5, 5.5, 6, M['rain'])
    else:
        c.fill(c.circ(7, 9, 5.5), M['skin'])
        for x in (4.5, 9.5):
            c.paint(c.rect(x - 1, 7, x + 1, 7), M['woodd'][2])
            c.p(x, 8, INK)
        c.paint(c.rect(6, 12, 8, 12), M['umber'][2])
        drop(c, 13, 3.5, 4, M['rain'], ol=False)


@icon('status', 'kiet_suc', 'Kiệt Sức: cạn Stamina (tia sét rỗng, nứt, mũi tên xuống).', S_SRC)
def kiet_suc(c):
    if c.n == 24:
        b = c.poly([(15, 1), (5, 13), (11, 13), (7, 22), (19, 9), (13, 9), (17, 1)])
        c.fill(b, M['umber'], 'flat', ink=M['bronze'][3])
        c.paint(c.line([(15, 4), (12, 8), (13, 10)], 1), M['bronze'][3])
        c.paint(c.line([(10, 15), (9, 18)], 1), M['bronze'][3])
        c.fill(c.poly([(18, 15), (22, 15), (22, 18), (23.5, 18), (20, 23), (16.5, 18), (18, 18)]), M['cinn'], 'flat')
    else:
        b = c.poly([(10, 1), (3, 9), (7, 9), (4, 15), (13, 6), (9, 6), (12, 1)])
        c.fill(b, M['umber'], 'flat', ink=M['bronze'][3])
        c.paint(c.line([(10, 3), (8, 6)], 1), M['bronze'][3])
        c.fill(c.poly([(12, 9), (15, 9), (13.5, 13)]), M['cinn'], 'flat', ol=False)


@icon('status', 'doi', 'Đói: bụng trống (chén rỗng) - đói hơn 8/16 giờ.', S_SRC)
def doi(c):
    if c.n == 24:
        c.fill(c.poly([(2, 12), (21, 12), (19, 18), (16, 21), (7, 21), (4, 18)]), M['wood'])
        c.paint(c.rect(6, 19, 17, 19), M['wood'][4])
        c.fill(c.ell(11.5, 12, 9.5, 3.4), M['bronze'])
        c.fill(c.ell(11.5, 12.4, 7.6, 2.3), M['umber'], 'plain', ol=False)
        c.ps([(8, 12), (14, 13)], M['bronze'][3])
        for dx in (-1, 1):
            c.paint(c.line([(11.5 + dx * 11, 5), (11.5 + dx * 9.5, 7), (11.5 + dx * 11, 9)], 1), M['sand'][2])
    else:
        c.fill(c.poly([(1, 7), (14, 7), (12, 12), (10, 14), (5, 14), (3, 12)]), M['wood'])
        c.fill(c.ell(7.5, 7, 6.5, 2.2), M['bronze'])
        c.paint(c.ell(7.5, 7.2, 5, 1.2), M['umber'][2])
        c.paint(c.line([(0, 2), (1, 4)], 1), M['sand'][2])
        c.paint(c.line([(15, 2), (14, 4)], 1), M['sand'][2])


@icon('status', 'benh', 'Bệnh: nhiễm bệnh (khuẩn gai xanh).', S_SRC)
def benh(c):
    if c.n == 24:
        m = c.z()
        for k in range(8):
            a = math.radians(k * 45)
            ex, ey = 11.5 + 9.3 * math.cos(a), 11.5 + 9.3 * math.sin(a)
            m |= c.line([(11.5 + 6 * math.cos(a), 11.5 + 6 * math.sin(a)), (ex, ey)], 1) | c.circ(ex, ey, 1.4)
        c.fill(m, M['sick'], 'flat', ink=M['sick'][5])
        c.fill(c.circ(11.5, 11.5, 6.8), M['sick'])
        c.ps([(9, 10), (14, 10)], INK)
        c.paint(c.line([(9, 14.5), (11.5, 13.5), (14, 14.5)], 1), INK)
        c.p(10, 8, M['sick'][0])
    else:
        m = c.z()
        for k in range(8):
            a = math.radians(k * 45)
            m |= c.line([(7.5 + 3 * math.cos(a), 7.5 + 3 * math.sin(a)), (7.5 + 6.3 * math.cos(a), 7.5 + 6.3 * math.sin(a))], 1)
        c.fill(m, M['sick'], 'flat', ink=M['sick'][5])
        c.fill(c.circ(7.5, 7.5, 3.9), M['sick'])
        c.ps([(6, 6.5), (9, 6.5), (6, 9), (9, 9)], INK)


@icon('status', 'bat_tinh', 'Bất tỉnh: HP về 0 - trái tim ngọc vỡ đôi.', S_SRC)
def bat_tinh(c):
    if c.n == 24:
        heart = c.circ(7.3, 8.3, 5.6) | c.circ(15.7, 8.3, 5.6) | c.poly([(2.1, 10), (21, 10), (11.5, 21.5)])
        left = heart & (c.X <= 10.5 - (c.Y > 12) * 0)
        zig = (abs(c.X - (11.5 + 1.2 * np.sin(c.Y * 1.1))) < .75)
        left = heart & (c.X < 11.5 + 1.2 * np.sin(c.Y * 1.1) - .7)
        right = heart & (c.X > 11.5 + 1.2 * np.sin(c.Y * 1.1) + .7)
        c.fill(left, M['ruby'])
        c.fill(shift(right, 1, 1), M['ruby'])
        c.p(5, 6, HL)
        c.p(6, 5, M['ruby'][0])
        c.p(16, 8, M['ruby'][0])
    else:
        heart = c.circ(4.5, 5, 3.4) | c.circ(10.5, 5, 3.4) | c.poly([(1.2, 6), (13.8, 6), (7.5, 13.5)])
        zig = np.sin(c.Y * 1.3) * .8
        left = heart & (c.X < 7 + zig - .5)
        right = heart & (c.X > 7 + zig + .5)
        c.fill(left, M['ruby'])
        c.fill(shift(right, 1, 1), M['ruby'])
        c.p(3, 3, HL)


@icon('status', 'ngu', 'Ngủ (trạng thái): đang ngủ - mắt nhắm, bong bóng mộng.', S_SRC)
def ngu_status(c):
    if c.n == 24:
        c.fill(c.circ(11.5, 14.5, 7.6), M['skin'])
        for cx in (8.5, 14.5):
            c.ps([(cx - 2, 13), (cx - 1, 14), (cx, 14), (cx + 1, 14), (cx + 2, 13)], M['woodd'][4])
        c.ps([(11, 18), (12, 18)], M['pinkd'][3])
        c.fill(c.poly([(3.8, 10.5), (7, 4), (13, 1), (19.2, 10.5)]), M['lapis'])
        c.fill(c.rrect(3.2, 9.5, 19.8, 11.5), M['white'], 'flat')
        c.fill(c.circ(13.5, 1.8, 1.7), M['white'], 'plain')
        c.fill(c.circ(20.5, 17, 2.4), M['lapisl'], 'flat')
        c.p(19.8, 16.2, HL)
    else:
        c.fill(c.circ(7, 10, 4.8), M['skin'])
        c.ps([(4.5, 9), (5.5, 10), (8.5, 10), (9.5, 9)], M['woodd'][4])
        c.fill(c.poly([(2.2, 7), (4.5, 2.5), (9, 1), (11.8, 7)]), M['lapis'], 'flat')
        c.fill(c.rrect(1.5, 6.5, 12.5, 7.5), M['white'], 'flat', ol=False)
        c.p(10, .8, M['white'][2])
        c.fill(c.circ(13, 12.5, 1.8), M['lapisl'], 'plain')


# ============================================================================= PIG
P_SRC = G + ' L806-L820 (benh), L583/L661-L665 (nhu cau, tinh than); checklist 2.2-2.4'


@icon('pig', 'heo_benh_ho', 'Heo bệnh ho (Hô Hấp Lạnh): heo xanh xao ho ra bụi hơi.', P_SRC)
def heo_benh_ho(c):
    if c.n == 24:
        pig(c, 9.5, 14, 6.4, M['pink'], 'sick')
        c.paint(c.ell(9.5, 18, 2.2, 1.4), M['sick'][3])
        for cx, cy, r in ((17, 11, 2.2), (20, 6.5, 1.8), (16, 5.5, 1.5)):
            c.fill(c.circ(cx, cy, r), M['cloud'], 'flat')
        c.paint(c.line([(15, 14), (18, 14.5), (16.5, 16.5), (20, 16)], 1), M['sick'][2])
    else:
        pig(c, 6, 10, 4.6, M['pink'], 'sick')
        for cx, cy, r in ((12, 8, 1.6), (14, 4.5, 1.2)):
            c.fill(c.circ(cx, cy, r), M['cloud'], 'flat')


@icon('pig', 'heo_benh_sot', 'Heo bệnh sốt (Sốt Bùn/Say Nắng): heo đỏ bừng, nhiệt kế.', P_SRC)
def heo_benh_sot(c):
    if c.n == 24:
        pig(c, 9.5, 14, 6.4, M['red'], 'sick')
        c.fill(c.ell(9.5, 17.4, 3.2, 2.4), M['snout'], 'flat')
        c.ps([(8, 17.2), (11, 17.2)], M['pinkd'][4])
        thermo(c, 19.5, 2, 13, 2.8, big=False)
        for x in (4, 9.5, 15):
            c.paint(c.line([(x, 1), (x - 1, 3)], 1), M['orange'][2])
    else:
        pig(c, 6, 10, 4.6, M['red'], 'sick')
        thermo(c, 13, 1, 8, 1.6, big=False)


@icon('pig', 'heo_doi', 'Heo đói: heo thèm ăn trước máng rỗng.', P_SRC)
def heo_doi(c):
    if c.n == 24:
        pig(c, 11.5, 8, 5.4, M['pink'], 'dot')
        c.fill(c.poly([(1, 15), (22, 15), (19, 22), (4, 22)]), M['wood'])
        c.fill(c.ell(11.5, 15, 10.5, 2.4), M['bronze'])
        c.paint(c.ell(11.5, 15.3, 8.4, 1.3), M['umber'][2])
        drop(c, 16.2, 12.5, 3.4, M['rain'], ol=False)
    else:
        pig(c, 7.5, 5, 3.8, M['pink'], 'dot')
        c.fill(c.poly([(1, 10), (14, 10), (12, 15), (3, 15)]), M['wood'])
        c.fill(c.ell(7.5, 10, 6.5, 1.8), M['bronze'])
        c.paint(c.ell(7.5, 10.3, 4.6, .9), M['umber'][2])


@icon('pig', 'heo_khat', 'Heo khát: heo thè lưỡi, thèm giọt nước.', P_SRC)
def heo_khat(c):
    if c.n == 24:
        pig(c, 9, 14, 6.4, M['pink'], 'dot')
        c.fill(c.rrect(7.5, 17.6, 10.5, 21.5), M['red'], 'flat')
        drop(c, 19, 6.5, 10, M['lapis'])
        c.ps([(17, 17), (20, 18), (19, 21)], M['sand'][2])
    else:
        pig(c, 6, 10, 4.6, M['pink'], 'dot')
        c.fill(c.rect(5, 12.5, 6.5, 14.5), M['red'], 'flat', ol=False)
        drop(c, 12.5, 4.5, 7, M['lapis'])


@icon('pig', 'heo_bun', 'Heo muốn lăn bùn: heo chờ vũng bùn mát.', P_SRC)
def heo_bun(c):
    if c.n == 24:
        pig(c, 11.5, 8, 5.6, M['pink'], 'closed')
        c.fill(c.ell(11.5, 19, 10.5, 3.7), M['mud'])
        c.paint(c.circ(7, 18, 1.1), M['mud'][1])
        c.paint(c.circ(15, 20, 1.1), M['mud'][1])
        c.paint(c.circ(12, 17.5, .7), M['mud'][0])
        c.paint(c.ell(11.5, 13.6, 3, 1), M['mud'][3])
    else:
        pig(c, 7.5, 5, 3.8, M['pink'], 'closed')
        c.fill(c.ell(7.5, 12.5, 7, 2.6), M['mud'])
        c.p(4, 12, M['mud'][1])
        c.p(10, 13, M['mud'][1])


@icon('pig', 'heo_hoang', 'Heo hoảng sợ/hoảng loạn: mắt tròn, miệng há, mồ hôi lạnh.', P_SRC)
def heo_hoang(c):
    if c.n == 24:
        pig(c, 11.5, 13.5, 6.6, M['pink'], 'wide')
        c.fill(c.ell(11.5, 19.6, 2.2, 1.5), M['umber'], 'plain', ol=False)
        for a in (215, 245, 270, 295, 325):
            m = ray(c, 11.5, 13.5, a, 9.3, 12.5, .8)
            c.paint(m & (c.Y >= 0), M['sun'][2])
        drop(c, 3.5, 11, 4.5, M['rain'], ol=False)
        drop(c, 20, 12, 4.5, M['rain'], ol=False)
    else:
        pig(c, 7.5, 9, 4.8, M['pink'], 'wide')
        c.paint(c.rect(7, 12, 8, 12), M['umber'][2])
        for a in (225, 270, 315):
            c.paint(ray(c, 7.5, 9, a, 6.0, 8.2, .6), M['sun'][2])


@icon('pig', 'heo_thu_linh', 'Heo thủ lĩnh: heo đội vương miện, vòng nguyệt quế (Neo tinh thần 20 m).', P_SRC)
def heo_thu_linh(c):
    if c.n == 24:
        for sg in (-1, 1):
            for a in range(110, 232, 20):
                th = math.radians(a)
                x = 11.5 + sg * 9.6 * math.cos(math.radians(a - 180 + 180)) * -1
                x = 11.5 + sg * (-9.6 * math.cos(th))
                y = 14 + 9.6 * -math.sin(th) * -1
                c.fill(c.ell(x, y, 1.8, 1.2), M['leaf'], 'flat', ol=False)
        pig(c, 11.5, 14.5, 5.8, M['pink'], 'dot')
        c.fill(c.poly([(6.5, 8.5), (7, 3.5), (9.4, 6), (11.5, 2), (13.6, 6), (16, 3.5), (16.5, 8.5)]), M['sun'], 'flat')
        c.ps([(9, 7.5), (11.5, 7.5), (14, 7.5)], M['cinn'][2])
    else:
        c.ps([(1, 11), (2, 13), (3, 14), (14, 11), (13, 13), (12, 14)], M['leaf'][2])
        pig(c, 7.5, 10, 4.4, M['pink'], 'dot')
        c.fill(c.poly([(4, 6), (4.5, 2.5), (6, 4.5), (7.5, 1.5), (9, 4.5), (10.5, 2.5), (11, 6)]), M['sun'], 'flat')


@icon('pig', 'heo_xac', 'Xác heo: sọ heo và xương bắt chéo (trừ Sinh Cảnh, ÁLSK).', P_SRC)
def heo_xac(c):
    if c.n == 24:
        for pts in ([(2, 21), (21, 13)], [(2, 13), (21, 21)]):
            c.fill(c.taper(pts, [2.2, 2.2]), M['bone'], 'flat')
        for x, y in ((2, 21), (2, 13), (21, 13), (21, 21)):
            c.fill(c.circ(x, y, 1.6), M['bone'], 'flat', ol=False)
        for sg in (-1, 1):
            c.fill(c.poly([(11.5 + sg * 4, 3.5), (11.5 + sg * 8.5, 1), (11.5 + sg * 8, 7)]), M['bone'], 'flat')
        c.fill(c.circ(11.5, 9.5, 7.2) | c.rrect(7.5, 12, 15.5, 16), M['bone'])
        c.fill(c.ell(11.5, 15.5, 4, 2.7), M['pinkd'], 'flat')
        for x in (8.5, 14.5):
            c.fill(c.circ(x, 9.5, 2.1), M['umber'], 'plain', ol=False)
        c.ps([(10, 15.5), (13, 15.5)], M['umber'][2])
    else:
        c.fill(c.taper([(1, 14), (14, 9)], [1.4, 1.4]), M['bone'], 'plain')
        c.fill(c.taper([(1, 9), (14, 14)], [1.4, 1.4]), M['bone'], 'plain')
        for sg in (-1, 1):
            c.fill(c.poly([(7.5 + sg * 2, 2), (7.5 + sg * 5, .5), (7.5 + sg * 4.8, 4.5)]), M['bone'], 'flat')
        c.fill(c.circ(7.5, 6, 4.6), M['bone'])
        c.fill(c.ell(7.5, 9.5, 2.3, 1.6), M['pinkd'], 'plain', ol=False)
        c.paint(c.rect(5, 6, 5, 6), M['umber'][2])
        c.paint(c.rect(10, 6, 10, 6), M['umber'][2])


# ============================================================================= ACTIONS
A_SRC = G + ' checklist 3 (hanh dong nguoi choi)'


@icon('actions', 'cho_an', 'Cho ăn: rải thức ăn vào máng (bông lúa + máng đầy).', A_SRC + ' #3 L859')
def cho_an(c):
    if c.n == 24:
        c.fill(c.poly([(1, 14), (22, 14), (19, 22), (4, 22)]), M['wood'])
        c.fill(c.ell(11.5, 14, 10.5, 2.6), M['bronze'])
        c.fill(c.ell(11.5, 13.2, 9, 2.5), M['sand'], 'flat', ol=False)
        for x, y in ((6, 12.5), (9, 12), (12, 12.3), (15, 12), (17.5, 12.6)):
            c.p(x, y, M['sand'][1])
        for a in (-24, 0, 24):
            st = rot([(11.5, 12), (11.5, 1.5)], 11.5, 12, a)
            c.paint(c.line(st, 1), M['leaf'][3])
            for yy in (3.2, 6, 8.8):
                (gx, gy) = rot([(11.5, yy)], 11.5, 12, a)[0]
                for s2 in (-1, 1):
                    c.fill(c.ell(gx + s2 * 1.3, gy, 1.1, 1.6), M['gold'], 'flat')
    else:
        c.fill(c.poly([(1, 9), (14, 9), (12, 14), (3, 14)]), M['wood'])
        c.fill(c.ell(7.5, 9, 6.5, 1.8), M['bronze'])
        c.fill(c.poly([(3, 9), (5.5, 6), (9.5, 6), (12, 9)]), M['sand'], 'flat', ol=False)
        c.ps([(7.5, 1), (5, 2.5), (10, 3), (7.5, 4.5)], M['gold'][1])
        c.ps([(6, 7), (9, 7.5)], M['sand'][0])


@icon('actions', 'cho_uong', 'Cho uống: thay nước, đổ xô nước (xô + giọt nước).', A_SRC + ' #4 L860')
def cho_uong(c):
    if c.n == 24:
        c.fill(c.poly([(3, 9), (18, 9), (16.5, 22), (4.5, 22)]), M['wood'])
        c.paint(c.rect(4, 13, 17, 13), M['bronze'][2])
        c.paint(c.rect(4, 18, 16, 18), M['bronze'][2])
        c.fill(c.ell(10.5, 9, 7.5, 2.2), M['lapis'], 'flat')
        c.paint(c.ell(10.5, 9, 5.5, 1.1), M['lapisl'][1])
        c.paint(c.line([(4, 9), (4, 2.5), (10.5, 1), (17, 2.5), (17, 9)], 1), M['iron'][2])
        drop(c, 20, 15, 7, M['lapis'])
        drop(c, 20.5, 6, 3, M['lapisl'])
    else:
        c.fill(c.poly([(1, 6), (10, 6), (9, 15), (2, 15)]), M['wood'])
        c.paint(c.rect(2, 10, 9, 10), M['bronze'][2])
        c.fill(c.ell(5.5, 6, 4.5, 1.4), M['lapis'], 'flat')
        c.paint(c.line([(1, 6), (1, 2), (5.5, 1), (10, 2), (10, 6)], 1), M['iron'][2])
        drop(c, 13, 11, 4.5, M['lapis'])


@icon('actions', 'lua_ve', 'Lùa về: lùa đàn về Nơi Trú bằng gậy chăn (cây gậy cong + mái nhà).', A_SRC + ' #8 L864, L586')
def lua_ve(c):
    if c.n == 24:
        m = c.rect(2.5, 8, 4, 22) | c.sector(7.5, 8, 3.2, 5.4, 180, 430)
        m = c.rect(6, 8, 7.4, 21) | c.sector(11, 8, 3.6, 5.4, 180, 440)
        c.fill(m, M['wood'], 'flat')
        c.fill(c.rect(5.4, 19, 8, 21.5), M['bronze'], 'flat')
        c.fill(c.rect(14, 17, 21, 22), M['stone'], 'flat')
        c.fill(c.poly([(12.5, 17.5), (17.5, 12), (22.5, 17.5)]), M['cinn'], 'flat')
        c.fill(c.rect(16.5, 19, 18.5, 22), M['woodd'], 'flat', ol=False)
    else:
        m = c.rect(3, 5, 4, 14) | c.sector(6, 5, 2.0, 3.4, 180, 430)
        c.fill(m, M['wood'], 'flat')
        c.fill(c.rect(9, 10, 14, 14), M['stone'], 'flat')
        c.fill(c.poly([(8, 10.5), (11.5, 6.5), (15, 10.5)]), M['cinn'], 'flat')


@icon('actions', 'huyt_sao', 'Huýt sáo: gọi heo bằng tù và (gọi tối đa 12 heo trong 20 m).', A_SRC + ' #9 L841')
def huyt_sao(c):
    if c.n == 24:
        c.fill(c.rrect(1, 11, 12, 17), M['bronze'])
        c.fill(c.circ(14.5, 14.5, 6.2), M['gold'])
        c.fill(c.rect(9, 8.5, 14, 10), M['umber'], 'plain', ol=False)
        c.paint(c.circ(15.5, 15.5, 2.2), M['umber'][2])
        c.paint(c.circ(15.5, 15.5, 1.1), M['sun'][1])
        c.paint(c.rect(1, 13, 8, 13), M['gold'][0])
        c.fill(c.sector(19.5, 7.5, 1.6, 3.2, 0, 360), M['steel'], 'flat')
        c.paint(c.line([(15, 9), (18, 8.5)], 1), M['steel'][3])
        for (x0, y0, x1, y1) in ((2, 6, 5, 3), (6, 4, 8, 1.5), (1, 9, 4, 8)):
            c.paint(c.line([(x0, y0), (x1, y1)], 1), M['sun'][2])
    else:
        c.fill(c.rrect(0, 7, 7, 10), M['bronze'])
        c.fill(c.circ(9, 9.5, 3.8), M['gold'])
        c.paint(c.rect(6, 5, 9, 5), M['umber'][2])
        c.paint(c.circ(9.5, 10, 1.3), M['umber'][2])
        c.fill(c.sector(12.5, 4, .8, 2.2, 0, 360), M['steel'], 'plain')
        c.paint(c.line([(1, 4), (3, 2)], 1), M['sun'][2])
        c.paint(c.line([(4, 3), (5, 1)], 1), M['sun'][2])


@icon('actions', 'sua', 'Sửa: sửa chữa công trình bằng búa (độ bền, chỉ ngoài đợt).', A_SRC + ' #12 L866, L1134')
def sua(c):
    if c.n == 24:
        pts = lambda p: rot(p, 11.5, 11.5, -45)
        c.fill(c.poly(pts([(10.3, 8), (12.7, 8), (12.7, 23), (10.3, 23)])), M['wood'], 'flat')
        c.fill(c.poly(pts([(4.5, 2), (18.5, 2), (18.5, 8.5), (4.5, 8.5)])), M['steel'])
        c.paint(c.poly(pts([(15.5, 2.5), (18, 2.5), (18, 8), (15.5, 8)])), M['steel'][3])
        star4(c, 19.5, 18.5, 3.2, M['sun'])
        c.ps([(15, 20.5), (21.5, 12)], M['sun'][1])
    else:
        pts = lambda p: rot(p, 7.5, 7.5, -45)
        c.fill(c.poly(pts([(6.6, 5), (8.4, 5), (8.4, 15), (6.6, 15)])), M['wood'], 'flat')
        c.fill(c.poly(pts([(3, 1), (12, 1), (12, 5.5), (3, 5.5)])), M['steel'])
        c.p(13, 12, M['sun'][1])
        star4(c, 12.5, 12.5, 2, M['sun'], ol=False)


@icon('actions', 'xay', 'Xây: thi công theo bản vẽ (bản vẽ xanh + com-pa).', A_SRC + ' #12 L866, checklist 1.1 Phong Xay Dung')
def xay(c):
    if c.n == 24:
        c.fill(c.rect(2, 5, 17, 21), M['lapis'], 'flat')
        for y in (9, 13, 17):
            c.paint(c.rect(3, y, 16, y), M['lapisl'][2])
        for x in (6, 10, 14):
            c.paint(c.rect(x, 6, x, 20), M['lapisl'][2])
        c.paint(c.rect(5, 11, 12, 11), M['white'][2])
        c.paint(c.rect(5, 11, 5, 18), M['white'][2])
        c.paint(c.rect(5, 18, 12, 18), M['white'][2])
        c.fill(c.taper([(17, 3), (12, 21)], [1.6, 1.6]), M['gold'], 'flat')
        c.fill(c.taper([(17, 3), (22, 21)], [1.6, 1.6]), M['gold'], 'flat')
        c.fill(c.circ(17, 3, 2.1), M['sun'])
        c.paint(c.line([(14.5, 13), (19.5, 13)], 1), M['bronze'][3])
    else:
        c.fill(c.rect(1, 3, 10, 14), M['lapis'], 'flat')
        c.paint(c.rect(2, 8, 9, 8), M['lapisl'][2])
        c.paint(c.rect(5, 4, 5, 13), M['lapisl'][2])
        c.fill(c.taper([(11, 1.5), (7.5, 14)], [1.2, 1.2]), M['gold'], 'flat')
        c.fill(c.taper([(11, 1.5), (14.5, 14)], [1.2, 1.2]), M['gold'], 'flat')
        c.fill(c.circ(11, 1.8, 1.5), M['sun'], 'plain')


@icon('actions', 'nap_dan', 'Nạp đạn: nạp đạn/nhiên liệu vào công trình (mũi tên xuống + viên đạn).', A_SRC + ' #13 L867')
def nap_dan(c):
    if c.n == 24:
        for x, y in ((7, 18), (16, 18), (11.5, 13.5)):
            c.fill(c.circ(x, y, 4.2), M['iron'])
        c.fill(c.poly([(10, 1), (13, 1), (13, 5), (16.5, 5), (11.5, 10.5), (6.5, 5), (10, 5)]), M['jade'], 'flat')
    else:
        for x, y in ((4.5, 11.5), (10.5, 11.5), (7.5, 8)):
            c.fill(c.circ(x, y, 3.0), M['iron'])
        c.fill(c.poly([(6.5, 0.5), (8.5, 0.5), (8.5, 3), (10.5, 3), (7.5, 6), (4.5, 3), (6.5, 3)]), M['jade'], 'flat', ol=False)


@icon('actions', 'cuong_hoa', 'Cường hóa: rèn nâng cấp vật phẩm (đe + tia lửa mặt trời).', A_SRC + ' checklist 5.3')
def cuong_hoa(c):
    if c.n == 24:
        c.fill(c.poly([(1, 11), (6, 9.5), (6, 14)]), M['iron'], 'flat')
        c.fill(c.rect(5, 9, 21, 13), M['steel'])
        c.fill(c.rect(8, 14, 17, 17), M['iron'], 'flat')
        c.fill(c.poly([(5, 18), (20, 18), (22, 22.5), (3, 22.5)]), M['iron'], 'flat')
        star4(c, 12, 4, 4, M['sun'])
        c.ps([(5, 4), (19, 5), (16, 1), (8, 7)], M['sun'][1])
    else:
        c.fill(c.poly([(0, 7), (3, 6), (3, 9)]), M['iron'], 'flat', ol=False)
        c.fill(c.rect(3, 6, 14, 9), M['steel'])
        c.fill(c.rect(5, 10, 11, 11), M['iron'], 'flat', ol=False)
        c.fill(c.poly([(3, 12), (13, 12), (14, 15), (2, 15)]), M['iron'], 'flat')
        star4(c, 8, 2.5, 2.5, M['sun'], ol=False)


@icon('actions', 'hoc_vo', 'Học võ: học võ kỹ từ cuộn giấy (cuộn + nắm đấm).', A_SRC + ' #40 L218, L292; 5.4')
def hoc_vo(c):
    if c.n == 24:
        c.fill(c.rect(4, 5, 19, 20), M['parch'], 'flat')
        for x in (2.5, 20.5):
            c.fill(c.rrect(x - 1.5, 3, x + 1.5, 22), M['wood'], 'flat')
        c.fill(c.rrect(7.5, 7.5, 16.5, 17.5), M['cinn'])
        for x in (10, 12, 14):
            c.paint(c.rect(x, 8, x, 12), M['cinn'][4])
        c.paint(c.rect(8, 13, 16, 13), M['cinn'][4])
        c.fill(c.rrect(8.5, 13.5, 12.5, 16.5), M['red'], 'flat', ol=False)
    else:
        c.fill(c.rect(3, 3, 12, 12), M['parch'], 'flat')
        for x in (2, 13):
            c.fill(c.rect(x - 1, 2, x, 13), M['wood'], 'flat', ol=False)
        c.fill(c.rrect(5, 5, 10, 10), M['cinn'], 'plain', ol=False)
        c.paint(c.rect(6, 5, 6, 7), M['cinn'][4])
        c.paint(c.rect(8, 5, 8, 7), M['cinn'][4])


@icon('actions', 'danh', 'Đánh: tấn công (kiếm + vệt chém).', A_SRC + ' #19 L872')
def danh(c):
    if c.n == 24:
        p = lambda q: rot(q, 11.5, 11.5, 45)
        c.fill(c.poly(p([(11.5, 0.5), (13.4, 3), (13.4, 14), (9.6, 14), (9.6, 3)])), M['steel'])
        c.paint(c.poly(p([(11.5, 4), (11.5, 4), (11.5, 13), (11, 13)])), M['steel'][4])
        c.fill(c.poly(p([(6, 14.3), (17, 14.3), (17, 16.4), (6, 16.4)])), M['gold'], 'flat')
        c.fill(c.poly(p([(10.4, 16.6), (12.6, 16.6), (12.6, 20.4), (10.4, 20.4)])), M['wood'], 'flat')
        (cx, cy) = p([(11.5, 22)])[0]
        c.fill(c.circ(cx, cy, 1.6), M['gold'])
        c.fill(c.sector(11.5, 11.5, 9.6, 10.9, 15, 95), M['white'], 'plain', ol=False)
    else:
        p = lambda q: rot(q, 7.5, 7.5, 45)
        c.fill(c.poly(p([(7.5, 0.5), (8.9, 2), (8.9, 9), (6.1, 9), (6.1, 2)])), M['steel'])
        c.fill(c.poly(p([(4, 9.4), (11, 9.4), (11, 10.8), (4, 10.8)])), M['gold'], 'flat')
        c.fill(c.poly(p([(6.8, 11), (8.2, 11), (8.2, 14), (6.8, 14)])), M['wood'], 'flat', ol=False)


@icon('actions', 'do_don', 'Đỡ đòn: Guard, đỡ đòn (khiên mặt trời; đỡ đúng lúc +1 Nộ Khí).', A_SRC + ' #23 L273, L873')
def do_don(c):
    if c.n == 24:
        c.fill(c.poly([(3, 3), (20, 3), (20, 12), (17, 18), (11.5, 22), (6, 18), (3, 12)]), M['bronze'])
        c.fill(c.poly([(5.5, 5.5), (17.5, 5.5), (17.5, 11.5), (15, 16), (11.5, 18.8), (8, 16), (5.5, 11.5)]), M['lapis'], 'flat', ol=False)
        c.fill(c.circ(11.5, 11, 3.8), M['sun'])
        for a in range(0, 360, 45):
            c.paint(ray(c, 11.5, 11, a, 4.4, 5.6, .4), M['sun'][2])
    else:
        c.fill(c.poly([(2, 2), (13, 2), (13, 8), (11, 12), (7.5, 14.5), (4, 12), (2, 8)]), M['bronze'])
        c.fill(c.poly([(4, 4), (11, 4), (11, 8), (7.5, 11.5), (4, 8)]), M['lapis'], 'plain', ol=False)
        c.fill(c.circ(7.5, 7, 2.2), M['sun'], 'plain', ol=False)


@icon('actions', 'ngu', 'Ngủ (hành động): nằm giường ngủ, tua giờ (hồi chiêu 6 giờ game).', A_SRC + ' #34 L892-L893')
def ngu_action(c):
    if c.n == 24:
        c.fill(c.rect(1, 6, 4, 21), M['wood'], 'flat')
        c.fill(c.rect(21, 12, 23, 21), M['wood'], 'flat')
        c.fill(c.rect(4, 14, 21, 17), M['parch'], 'flat')
        c.fill(c.rect(9, 11, 21, 16), M['lapis'], 'flat')
        c.paint(c.rect(10, 12, 20, 12), M['lapisl'][2])
        c.fill(c.rrect(4.5, 10.5, 9.5, 14), M['white'], 'flat')
        c.fill(c.rect(4, 18, 21, 19), M['woodd'], 'flat')
        c.fill(c.circ(17, 4, 3.4) & ~c.circ(18.8, 2.8, 3), M['moon'])
    else:
        c.fill(c.rect(0, 4, 2, 14), M['wood'], 'flat')
        c.fill(c.rect(14, 8, 15, 14), M['wood'], 'flat')
        c.fill(c.rect(2, 9, 14, 12), M['parch'], 'flat')
        c.fill(c.rect(6, 8, 14, 11), M['lapis'], 'flat')
        c.fill(c.rrect(2.5, 7, 6, 9.5), M['white'], 'flat')
        c.fill(c.rect(2, 12, 14, 13), M['woodd'], 'flat', ol=False)


@icon('actions', 'vao_cua', 'Vào cửa: vào nhà/công trình (cửa mở sáng + mũi tên vào).', A_SRC + ' #38 L1221')
def vao_cua(c):
    if c.n == 24:
        c.fill(c.circ(14.5, 9.5, 7.6) | c.rect(7, 9.5, 22, 22), M['stone'], 'flat')
        c.fill(c.circ(14.5, 10.5, 5) | c.rect(9.5, 10.5, 19.5, 22), M['sand'], 'plain', ol=False)
        c.paint(c.circ(16, 12, 3) | c.rect(13, 12, 19, 22), M['sun'][1])
        c.fill(c.poly([(9.5, 7.5), (14, 9), (14, 22), (9.5, 22)]), M['wood'], 'flat', ol=False)
        c.p(13, 16, M['gold'][0])
        c.fill(c.poly([(.5, 11.5), (4, 11.5), (4, 9), (8, 13), (4, 17), (4, 14.5), (.5, 14.5)]), M['jade'], 'flat')
    else:
        c.fill(c.circ(9.5, 6, 4.8) | c.rect(5, 6, 14, 14), M['stone'], 'flat')
        c.paint(c.circ(10, 7, 2.6) | c.rect(8, 7, 12, 14), M['sun'][1])
        c.fill(c.poly([(0, 7), (2, 7), (2, 5), (5, 8), (2, 11), (2, 9), (0, 9)]), M['jade'], 'flat', ol=False)


@icon('actions', 'mua_ban', 'Mua bán: giao dịch chợ ngày bằng vàng (cân hai đĩa + đồng xu).', A_SRC + ' #37 L869; 7.1')
def mua_ban(c):
    if c.n == 24:
        c.fill(c.rect(11, 4, 12, 19), M['bronze'], 'flat')
        c.fill(c.rrect(7, 19, 16, 22), M['bronze'], 'flat')
        c.fill(c.rrect(2, 4, 21, 6), M['gold'], 'flat')
        c.fill(c.circ(11.5, 3.2, 1.8), M['sun'], 'flat')
        for x, y in ((4, 15), (19, 12)):
            c.paint(c.line([(x - 2.5, y), (x - 0 if False else 3 if x == 4 else 20, 6)], 1), M['gold'][4])
            c.paint(c.line([(x + 2.5, y), (x if False else 4 if x == 4 else 19, 6)], 1), M['gold'][4])
        for x, y in ((4, 15), (19, 12)):
            c.fill(c.circ(x, y, 3.8) & (c.Y >= y), M['bronze'], 'flat')
        c.fill(c.circ(4, 13, 2.2), M['sun'], 'flat')
        c.p(3.5, 12.5, HL)
    else:
        c.fill(c.rect(7, 2, 8, 12), M['bronze'], 'flat', ol=False)
        c.fill(c.rrect(4, 12, 11, 14), M['bronze'], 'flat')
        c.fill(c.rrect(1, 2, 14, 3), M['gold'], 'flat')
        for x, y in ((2.5, 8), (12.5, 6)):
            c.fill(c.circ(x, y, 2.4) & (c.Y >= y), M['bronze'], 'flat', ol=False)
        c.fill(c.circ(2.5, 7, 1.4), M['sun'], 'plain', ol=False)


@icon('actions', 'trao_doi', 'Trao đổi: đổi hàng đổi hàng ở chợ đêm (hai mũi tên ngược chiều).', A_SRC + ' #37 L869; 7.2 L1176-L1181')
def trao_doi(c):
    if c.n == 24:
        c.fill(c.poly([(2, 4.5), (13, 4.5), (13, 1.5), (22, 7), (13, 12.5), (13, 9.5), (2, 9.5)]), M['gold'])
        c.fill(c.poly([(22, 14.5), (11, 14.5), (11, 11.5), (2, 17), (11, 22.5), (11, 19.5), (22, 19.5)]), M['lapisl'])
    else:
        c.fill(c.poly([(1, 3), (9, 3), (9, 1), (15, 4.5), (9, 8), (9, 6), (1, 6)]), M['gold'], 'flat')
        c.fill(c.poly([(15, 9), (7, 9), (7, 7.5), (1, 11.5), (7, 15.5), (7, 14), (15, 14)]), M['lapisl'], 'flat')


@icon('actions', 'giam_dinh', 'Giám định: soi giám định/kiểm dịch (kính lúp + viên ngọc).', A_SRC + ' #18 L833, L870; 2.1 L179')
def giam_dinh(c):
    if c.n == 24:
        c.fill(c.taper([(15.5, 15.5), (21, 21)], [3.4, 3.4]), M['wood'], 'flat')
        c.fill(c.circ(9.5, 9.5, 8.2), M['bronze'])
        c.fill(c.circ(9.5, 9.5, 6.1), M['glass'], 'flat', ink=M['bronze'][4])
        c.fill(c.poly([(9.5, 5.5), (12.8, 9), (9.5, 13.5), (6.2, 9)]), M['lapis'], 'flat')
        c.paint(c.line([(6.5, 9), (12.5, 9)], 1), M['lapisl'][1])
        c.p(7, 6.5, HL)
        c.p(6.5, 7.5, HL)
    else:
        c.fill(c.taper([(10, 10), (14, 14)], [2.2, 2.2]), M['wood'], 'flat')
        c.fill(c.circ(6.5, 6.5, 5.6), M['bronze'])
        c.fill(c.circ(6.5, 6.5, 3.8), M['glass'], 'plain', ol=False)
        c.fill(c.poly([(6.5, 4), (8.5, 6.5), (6.5, 9), (4.5, 6.5)]), M['lapis'], 'plain', ol=False)


# ============================================================================= PLACES
PL_SRC = G + ' checklist 1.1 (Nha chinh/Kho/Phong Xay Dung/Noi Tru Dan/Khu Xu Ly/Khu Cach Ly/Phong nghien cuu)'


@icon('places', 'nha_chinh', 'Nhà chính: nhà ở của An/Khoa (ngủ, hồi sinh, Bảng Điều Khiển).', PL_SRC + ' L892-L893, L845, L301')
def nha_chinh(c):
    if c.n == 24:
        c.fill(c.rect(4, 12, 19, 22), M['stone'], 'flat')
        c.fill(c.rect(15, 3, 17, 9), M['stoned'], 'flat')
        c.fill(c.poly([(.5, 11), (2.2, 13.8), (20.8, 13.8), (22.5, 11), (11.5, 2.5)]), M['cinn'])
        for x in (6, 9, 14, 17):
            c.paint(c.line([(11.5 + (x - 11.5) * .25, 6), (x, 12)], 1), M['cinn'][3])
        c.fill(c.circ(11.5, 9.5, 1.5), M['sun'], 'plain', ol=False)
        c.fill(c.rrect(9.5, 16, 13.5, 22), M['woodd'], 'flat', ol=False)
        c.fill(c.rect(15, 15.5, 18, 18), M['sun'], 'flat', ol=False)
        c.fill(c.rect(5, 15.5, 8, 18), M['sun'], 'flat', ol=False)
    else:
        c.fill(c.rect(3, 8, 12, 15), M['stone'], 'flat')
        c.fill(c.poly([(0, 7), (1.5, 9.5), (13.5, 9.5), (15, 7), (7.5, 1.5)]), M['cinn'])
        c.fill(c.rect(6, 11, 9, 15), M['woodd'], 'plain', ol=False)


@icon('places', 'kho', 'Kho: kho chứa heo/vật liệu/vật phẩm (kho gỗ cửa đôi).', PL_SRC + ' L365')
def kho(c):
    if c.n == 24:
        c.fill(c.rect(2, 10, 21, 22), M['wood'], 'flat')
        for x in (6, 11, 17):
            c.paint(c.rect(x, 11, x, 21), M['wood'][3])
        c.fill(c.poly([(.5, 10), (5, 3), (18, 3), (22.5, 10)]), M['woodd'])
        c.fill(c.rect(7, 12, 16, 22), M['wood'], 'flat')
        c.paint(c.rect(11, 12, 11, 22), M['woodd'][4])
        c.paint(c.line([(7, 12), (11, 22)], 1), M['woodd'][3])
        c.paint(c.line([(16, 12), (12, 22)], 1), M['woodd'][3])
        c.fill(c.circ(11.5, 7, 1.6), M['umber'], 'plain', ol=False)
    else:
        c.fill(c.rect(1, 6, 14, 15), M['wood'], 'flat')
        c.fill(c.poly([(0, 6), (3, 1), (12, 1), (15, 6)]), M['woodd'])
        c.paint(c.rect(7, 7, 7, 15), M['woodd'][4])
        c.paint(c.line([(4, 7), (7, 15)], 1), M['woodd'][3])
        c.paint(c.line([(11, 7), (8, 15)], 1), M['woodd'][3])


@icon('places', 'phong_xay_dung', 'Phòng Xây Dựng: thi công, sửa, chế tạo (tường gạch dở + giàn giáo).', PL_SRC + ' L303, L306, L1062-L1065')
def phong_xay_dung(c):
    if c.n == 24:
        c.fill(c.rect(1, 12, 12, 22), M['brick'], 'flat')
        for y in (14, 17, 20):
            c.paint(c.rect(2, y, 11, y), M['brick'][4])
        for x, y in ((5, 12), (9, 12), (3, 15), (7, 15), (11, 15), (5, 18), (9, 18), (3, 21), (7, 21)):
            c.p(x, y + 1, M['brick'][4])
        c.fill(c.rect(15, 4, 16, 22), M['wood'], 'flat')
        c.fill(c.rect(21, 4, 22, 22), M['wood'], 'flat')
        c.fill(c.rect(15, 11, 22, 12.5), M['woodd'], 'flat')
        for y in (7, 15, 18):
            c.paint(c.rect(17, y, 20, y), M['wood'][1])
        c.fill(c.rect(1, 2, 22, 3), M['woodd'], 'flat')
        c.paint(c.line([(6, 4), (6, 9)], 1), M['iron'][2])
        c.fill(c.rect(4.5, 9, 7.5, 11), M['brick'], 'flat')
    else:
        c.fill(c.rect(0, 8, 7, 15), M['brick'], 'flat')
        c.paint(c.rect(1, 11, 6, 11), M['brick'][4])
        c.fill(c.rect(10, 2, 11, 15), M['wood'], 'flat')
        c.fill(c.rect(14, 2, 15, 15), M['wood'], 'flat', ol=False)
        c.fill(c.rect(10, 7, 15, 8), M['woodd'], 'flat', ol=False)
        c.paint(c.rect(0, 1, 15, 1), M['woodd'][2])


@icon('places', 'noi_tru_dan', 'Nơi Trú Đàn: chuồng/mái trú cho đàn heo (mái tranh + heo).', PL_SRC + ' L586, L603-L618')
def noi_tru_dan(c):
    if c.n == 24:
        c.fill(c.rect(4, 14, 19, 22), M['umber'], 'plain')
        pig(c, 11.5, 18.5, 3.4, M['pink'], 'dot', ears=True)
        c.fill(c.rect(2.5, 13, 4, 22), M['wood'], 'flat')
        c.fill(c.rect(19, 13, 20.5, 22), M['wood'], 'flat')
        c.fill(c.poly([(.5, 14), (11.5, 2.5), (22.5, 14)]), M['straw'])
        for y in (7, 10, 13):
            c.paint(c.line([(11.5 - (y - 2.5) * .95, y), (11.5 + (y - 2.5) * .95, y)], 1), M['straw'][3])
        c.paint(c.rect(1, 21, 22, 22), M['sand'][2])
    else:
        c.fill(c.rect(3, 8, 12, 15), M['umber'], 'plain')
        pig(c, 7.5, 11.5, 2.6, M['pink'], 'dot', ears=False)
        c.fill(c.poly([(0, 9), (7.5, 1), (15, 9)]), M['straw'])
        c.paint(c.rect(1, 14, 14, 15), M['sand'][2])


@icon('places', 'khu_xu_ly', 'Khu Xử Lý: xử lý xác heo (lò nung, ống khói).', PL_SRC + ' L97, L725, L588')
def khu_xu_ly(c):
    if c.n == 24:
        c.fill(c.rect(14, 3, 18, 12), M['stoned'], 'flat')
        for cx, cy, r in ((16.5, 2, 1.8), (19, .5, 1.3)):
            c.fill(c.circ(cx, cy, r), M['cloudg'], 'flat', ol=False)
        c.fill(c.rect(3, 11, 20, 22), M['stoned'])
        c.fill(c.ell(11.5, 11, 8.5, 3.4), M['stone'], 'flat')
        c.fill(c.circ(11.5, 16, 3.6) | c.rect(8, 16, 15, 22), M['umber'], 'plain')
        c.fill(c.poly([(11.5, 13.5), (13.5, 17), (14, 20.5), (9, 20.5), (9.5, 17)]), M['flame'], 'flat', ol=False)
        c.paint(c.rect(10.5, 18, 12.5, 20.5), M['sun'][2])
        for y in (14, 18):
            c.paint(c.rect(4, y, 6, y), M['stoned'][3])
            c.paint(c.rect(17, y, 19, y), M['stoned'][3])
    else:
        c.fill(c.rect(9, 1, 11, 6), M['stoned'], 'flat')
        c.fill(c.circ(10, .5, 1.2), M['cloudg'], 'plain', ol=False)
        c.fill(c.rect(1, 6, 13, 15), M['stoned'])
        c.fill(c.circ(7, 11, 2.4) | c.rect(4.6, 11, 9.4, 15), M['umber'], 'plain', ol=False)
        c.fill(c.poly([(7, 9), (8.5, 12), (8, 15), (6, 15), (5.5, 12)]), M['flame'], 'plain', ol=False)


@icon('places', 'khu_cach_ly', 'Khu Cách Ly: khu phong tỏa dịch (hàng rào + biển cảnh báo).', PL_SRC + ' L587, L810, L813')
def khu_cach_ly(c):
    if c.n == 24:
        c.fill(c.rect(1, 17, 22, 18.5), M['woodd'], 'flat')
        for x in (1, 5.5, 10, 14.5, 19):
            c.fill(c.poly([(x, 22), (x, 14), (x + 1.7, 11.5), (x + 3.4, 14), (x + 3.4, 22)]), M['wood'], 'flat')
        c.fill(c.poly([(11.5, 1), (19.5, 12.5), (3.5, 12.5)]), M['sun'])
        c.paint(c.rect(11, 5, 12, 8.5), M['umber'][2])
        c.paint(c.rect(11, 10, 12, 10.5), M['umber'][2])
    else:
        c.fill(c.rect(0, 10, 15, 11), M['woodd'], 'flat', ol=False)
        for x in (0, 4, 8, 12):
            c.fill(c.poly([(x, 15), (x, 10), (x + 1.5, 8.5), (x + 3, 10), (x + 3, 15)]), M['wood'], 'flat')
        c.fill(c.poly([(7.5, .5), (12, 7.5), (3, 7.5)]), M['sun'])
        c.paint(c.rect(7, 3, 8, 5), M['umber'][2])


@icon('places', 'phong_nghien_cuu', 'Phòng nghiên cứu: giám định gen, nghiên cứu (nhà mái lam + bình thí nghiệm).', PL_SRC + ' L185, L563, L785, L941')
def phong_nghien_cuu(c):
    if c.n == 24:
        c.fill(c.rect(3, 12, 20, 22), M['stone'], 'flat')
        c.fill(c.poly([(.5, 12), (3, 12), (11.5, 4), (20, 12), (22.5, 12), (11.5, 3)]), M['lapis'])
        c.fill(c.poly([(.5, 12.5), (11.5, 3.5), (22.5, 12.5)]), M['lapis'])
        c.fill(c.circ(11.5, 15, 3.8), M['gold'])
        c.paint(c.circ(11.5, 15, 2.6), M['umber'][2])
        c.fill(c.poly([(10.5, 13), (12.5, 13), (12.5, 14), (14, 17), (9, 17), (10.5, 14)]), M['jade'], 'plain', ol=False)
        c.fill(c.rrect(10, 19, 13, 22), M['woodd'], 'flat', ol=False)
        star4(c, 11.5, 1.5, 2.2, M['sun'], ol=False)
    else:
        c.fill(c.rect(2, 7, 13, 15), M['stone'], 'flat')
        c.fill(c.poly([(0, 8), (7.5, 1), (15, 8)]), M['lapis'])
        c.fill(c.circ(7.5, 10.5, 2.4), M['gold'], 'plain', ol=False)
        c.paint(c.rect(7, 10, 8, 12), M['jade'][2])


def stripes(c, x0, y0, x1, y1, step, a, b):
    for i, x in enumerate(range(x0, x1 + 1, step)):
        c.paint(c.rect(x, y0, min(x + step - 1, x1), y1), (a if i % 2 == 0 else b)[2])


@icon('places', 'cho_ngay', 'Chợ ngày: quầy chợ ban ngày, trả bằng vàng (mái sọc, mặt trời).', PL_SRC + ' L351, L359-L360, L1170-L1175')
def cho_ngay(c):
    if c.n == 24:
        c.fill(c.rect(3, 12, 4, 22), M['wood'], 'flat')
        c.fill(c.rect(19, 12, 20, 22), M['wood'], 'flat')
        c.fill(c.rect(2, 17, 21, 21), M['wood'], 'flat')
        c.fill(c.circ(8, 15.2, 1.9), M['red'], 'round')
        c.fill(c.circ(12.5, 15.2, 1.9), M['leaf'], 'round')
        c.fill(c.circ(16.5, 15.2, 1.9), M['sun'], 'round')
        c.fill(c.rect(1, 7, 22, 12), M['white'], 'flat')
        stripes(c, 2, 8, 21, 12, 3, M['cinn'], M['white'])
        for x in range(2, 22, 3):
            c.paint(c.circ(x + 1, 12.5, 1.5), (M['cinn'] if ((x - 2) // 3) % 2 == 0 else M['white'])[2])
        c.fill(c.circ(11.5, 3.2, 2.2), M['sun'], 'plain', ol=False)
        m = c.z()
        for k in range(8):
            m |= ray(c, 11.5, 3.2, k * 45, 3.2, 4.1, .6)
        c.paint(m & (c.Y >= 0), M['gold'][2])
    else:
        c.fill(c.rect(1, 6, 14, 9), M['white'], 'flat')
        stripes(c, 1, 7, 14, 9, 2, M['cinn'], M['white'])
        c.fill(c.rect(2, 9, 2, 15), M['wood'], 'flat', ol=False)
        c.fill(c.rect(13, 9, 13, 15), M['wood'], 'flat', ol=False)
        c.fill(c.rect(1, 11, 14, 14), M['wood'], 'flat')
        c.fill(c.circ(7.5, 9.8, 1.4), M['sun'], 'plain', ol=False)
        c.fill(c.circ(7.5, 2.6, 2.0), M['sun'], 'plain')


@icon('places', 'cho_dem', 'Chợ đêm: chợ trao đổi ban đêm (quầy tối, đèn lồng, trăng).', PL_SRC + ' L1176-L1181, L234, L284')
def cho_dem(c):
    if c.n == 24:
        c.fill(c.circ(19, 4, 3) & ~c.circ(20.6, 2.8, 2.6), M['moon'])
        c.fill(c.rect(2, 8, 21, 11), M['night'], 'flat')
        stripes(c, 2, 8, 21, 11, 3, M['night'], M['lapis'])
        c.fill(c.rect(2, 17, 21, 21), M['woodd'], 'flat')
        c.fill(c.rect(3, 11, 4, 17), M['woodd'], 'flat', ol=False)
        c.fill(c.rect(19, 11, 20, 17), M['woodd'], 'flat', ol=False)
        for x in (7, 12, 17):
            c.paint(c.rect(x, 11, x, 12), INK)
            c.fill(c.ell(x, 14.7, 2.2, 2.6), M['orange'], 'round')
            c.paint(c.ell(x, 14.7, 1.1, 1.5), M['sun'][1])
    else:
        c.fill(c.circ(11.5, 2.5, 2.2) & ~c.circ(12.9, 1.6, 1.9), M['moon'], 'plain', ol=False)
        c.fill(c.rect(1, 5, 14, 7), M['night'], 'flat')
        c.fill(c.rect(1, 12, 14, 14), M['woodd'], 'flat')
        for x in (4.5, 10.5):
            c.fill(c.ell(x, 9.8, 1.6, 2.0), M['orange'], 'plain')
            c.p(x, 9.8, M['sun'][1])


@icon('places', 'bi_nhan', 'Bí Nhân: thương nhân nửa người nửa quỷ xuất hiện (đèn lồng xanh).', PL_SRC + ' L361-L362, L1191-L1198; checklist 7.3')
def bi_nhan(c):
    if c.n == 24:
        c.fill(c.circ(11.5, 2.6, 1.6) & ~c.circ(11.5, 2.6, .5), M['gold'], 'flat', ol=False)
        c.fill(c.rrect(6, 4, 17, 6), M['gold'], 'flat')
        c.fill(c.ell(11.5, 12.5, 7, 7), M['lapis'])
        c.paint(c.ell(11.5, 12.5, 3.6, 5.2), M['ice'][2])
        c.paint(c.ell(11.5, 12.5, 2.2, 3.6), M['white'][2])
        c.paint(c.ell(11.5, 12.5, 7, 7) & (abs(c.X - 11.5) < .6) & (c.Y < 8), M['lapis'][3])
        c.ps([(7, 12.5), (16, 12.5)], M['lapis'][4])
        c.fill(c.rrect(7, 19, 16, 21), M['gold'], 'flat')
        c.paint(c.line([(11.5, 21), (11.5, 23)], 1), M['cinn'][2])
        c.fill(c.poly([(6, 4), (4.5, 1), (8, 3.5)]), M['cinn'], 'flat', ol=False)
        c.fill(c.poly([(17, 4), (18.5, 1), (15, 3.5)]), M['cinn'], 'flat', ol=False)
    else:
        c.fill(c.rrect(4, 2, 11, 3), M['gold'], 'flat')
        c.fill(c.ell(7.5, 8, 4.8, 4.6), M['lapis'])
        c.paint(c.ell(7.5, 8, 2.2, 3.2), M['ice'][2])
        c.fill(c.rrect(4.5, 12, 10.5, 13), M['gold'], 'flat', ol=False)
        c.p(7.5, 14.5, M['cinn'][2])


# ============================================================================= MISC
MI_SRC = G + ' checklist 8 (HUD)'


@icon('misc', 'canh_bao_dot', 'Cảnh báo đợt: chuông báo đợt quái sắp tới (cảnh báo 1-2 ngày trước).', MI_SRC + ' L515, L1140, L1213-L1219')
def canh_bao_dot(c):
    if c.n == 24:
        c.fill(c.rrect(10, 1, 13, 4), M['iron'], 'flat')
        c.fill(c.circ(11.5, 10, 6.4) | c.poly([(5, 10), (18, 10), (21, 18), (2, 18)]), M['gold'])
        c.fill(c.rrect(1.5, 17.5, 21.5, 19.5), M['bronze'], 'flat')
        c.fill(c.circ(11.5, 21, 2), M['bronze'])
        c.paint(c.line([(7, 7), (6, 12), (4.5, 16)], 1), M['gold'][0])
        for sg in (-1, 1):
            for y0, y1 in ((4, 6), (8, 10)):
                c.paint(c.line([(11.5 + sg * 10.5, y0), (11.5 + sg * 9.5, y1)], 1), M['cinn'][2])
    else:
        c.fill(c.rrect(6.5, 0, 8.5, 2), M['iron'], 'flat', ol=False)
        c.fill(c.circ(7.5, 6, 4) | c.poly([(3.5, 6), (11.5, 6), (13.5, 11), (1.5, 11)]), M['gold'])
        c.fill(c.circ(7.5, 13.5, 1.4), M['bronze'], 'plain', ol=False)
        c.paint(c.rect(0, 3, 0, 4), M['cinn'][2])
        c.paint(c.rect(15, 3, 15, 4), M['cinn'][2])


@icon('misc', 'cong_su_kien', 'Cổng sự kiện: cổng huyền bí còn 6 giờ (vòng sáng + đồng hồ cát).', MI_SRC + ' L1221')
def cong_su_kien(c):
    if c.n == 24:
        c.fill(c.circ(11.5, 11.5, 10.8), M['gold'])
        c.fill(c.circ(11.5, 11.5, 8), M['night'], 'plain')
        c.paint(c.sector(11.5, 11.5, 7.2, 8, 0, 360), M['teal'][2])
        for a in range(0, 360, 45):
            c.p(11.5 + 9.4 * math.cos(math.radians(a)), 11.5 + 9.4 * math.sin(math.radians(a)), M['gold'][4])
        c.fill(c.rect(7.5, 4.5, 15.5, 5.5), M['gold'], 'flat')
        c.fill(c.rect(7.5, 17.5, 15.5, 18.5), M['gold'], 'flat')
        c.fill(c.poly([(8.5, 6), (14.5, 6), (11.5, 11.5)]), M['glass'], 'plain', ol=False)
        c.fill(c.poly([(11.5, 11.5), (8.5, 17), (14.5, 17)]), M['glass'], 'plain', ol=False)
        c.paint(c.poly([(9.8, 7), (13.2, 7), (11.5, 9.5)]), M['sun'][2])
        c.paint(c.poly([(11.5, 13), (9.6, 16.5), (13.4, 16.5)]), M['sun'][2])
        c.p(11.5, 12.5, M['sun'][2])
    else:
        c.fill(c.circ(7.5, 7.5, 7), M['gold'])
        c.fill(c.circ(7.5, 7.5, 5), M['night'], 'plain', ol=False)
        c.paint(c.rect(4, 3, 11, 3), M['gold'][2])
        c.paint(c.rect(4, 12, 11, 12), M['gold'][2])
        c.paint(c.poly([(5, 4.5), (10, 4.5), (7.5, 7.5)]), M['sun'][2])
        c.paint(c.poly([(7.5, 8), (5, 11), (10, 11)]), M['white'][2])


@icon('misc', 'suong_huyen_bi', 'Sương Mù Huyền Bí: vòng sương cuốn (kẹt 3 ngày).', MI_SRC + ' L1222')
def suong_huyen_bi(c):
    if c.n == 24:
        c.paint(c.circ(11.5, 11.5, 8), M['night'][2])
        c.dither(c.circ(11.5, 11.5, 6), M['night'][1], 0)
        for k in range(10):
            a = math.radians(k * 36 - 90)
            x, y = 11.5 + 8.6 * math.cos(a), 11.5 + 8.6 * math.sin(a)
            c.fill(c.circ(x, y, 3.2 if k % 2 == 0 else 2.6), M['lilac'], 'round')
        c.fill(c.circ(11.5, 11.5, 5.2), M['night'], 'flat', ol=False)
        c.paint(c.line([(8, 12), (11, 9), (15, 12)], 1), M['lilac'][3])
        c.p(11.5, 14, M['lilac'][2])
    else:
        for k in range(8):
            a = math.radians(k * 45 - 90)
            x, y = 7.5 + 5.2 * math.cos(a), 7.5 + 5.2 * math.sin(a)
            c.fill(c.circ(x, y, 2.1), M['lilac'], 'flat')
        c.fill(c.circ(7.5, 7.5, 3.2), M['night'], 'plain', ol=False)


@icon('misc', 'ap_luc_su_kien', 'Áp Lực Sự Kiện: đồng hồ khí áp 4 vùng (Yên ả / Gợn sóng / Bất ổn / Rạn nứt).', MI_SRC + ' L554-L558, L1091')
def ap_luc_su_kien(c):
    if c.n == 24:
        c.fill(c.circ(11.5, 11.5, 10.8), M['bronze'])
        c.fill(c.circ(11.5, 11.5, 8.8), M['parch'], 'flat', ol=False)
        cols = [M['jade'], M['sun'], M['orange'], M['cinn']]
        for i, k in enumerate(cols):
            c.paint(c.sector(11.5, 11.5, 4.6, 8.4, 150 + 60 * i, 207 + 60 * i), k[2])
        a = math.radians(305)
        c.paint(c.taper([(11.5, 11.5), (11.5 + 7.2 * math.cos(a), 11.5 + 7.2 * math.sin(a))], [2.4, .8]), M['umber'][2])
        c.fill(c.circ(11.5, 11.5, 2.1), M['gold'])
        c.paint(c.rect(8, 18, 15, 19), M['bronze'][4])
    else:
        c.fill(c.circ(7.5, 7.5, 7), M['bronze'])
        c.fill(c.circ(7.5, 7.5, 5.4), M['parch'], 'plain', ol=False)
        cols = [M['jade'], M['sun'], M['orange'], M['cinn']]
        for i, k in enumerate(cols):
            c.paint(c.sector(7.5, 7.5, 3.0, 5.4, 150 + 60 * i, 207 + 60 * i), k[2])
        c.paint(c.taper([(7.5, 7.5), (10.8, 4)], [1.6, .8]), M['umber'][2])
        c.fill(c.circ(7.5, 7.5, 1.1), M['gold'], 'plain', ol=False)


@icon('misc', 'sinh_canh', 'Sinh Cảnh: độ khỏe môi trường trại, 5 bậc (lá + giọt).', MI_SRC + ' L787-L792')
def sinh_canh(c):
    if c.n == 24:
        leaf = c.poly([(3, 19), (2.5, 12), (6.5, 6.5), (13, 3.5), (21, 2.5), (20, 10), (16, 16), (10, 20), (3, 19)])
        c.fill(leaf, M['leaf'])
        c.paint(c.line([(3, 19), (9, 13), (17, 6)], 1), M['leaf'][4])
        for (x, y) in ((8, 11), (11, 8), (11, 15), (14, 12)):
            c.paint(c.line([(x, y), (x + 1.5, y - 1.5)] if y < 12 else [(x, y), (x + 2, y - 2)], 1), M['leaf'][3])
        drop(c, 17.5, 17.5, 8, M['lapis'])
    else:
        leaf = c.poly([(1.5, 12), (1.5, 8), (4, 4), (9, 2), (13, 1.5), (13, 6), (10, 10), (6, 12.5)])
        c.fill(leaf, M['leaf'])
        c.paint(c.line([(2, 12), (7, 7), (12, 2.5)], 1), M['leaf'][4])
        drop(c, 12, 12, 5, M['lapis'])


@icon('misc', 'tinh_bao', 'Tình báo: mắt tình báo với 5 vạch độ tin (cấp 0-5).', MI_SRC + ' L1213-L1219, L373')
def tinh_bao(c):
    if c.n == 24:
        c.fill(c.poly([(1, 9), (5, 4), (11.5, 2), (18, 4), (22, 9), (18, 14), (11.5, 16), (5, 14)]), M['white'])
        c.fill(c.circ(11.5, 9, 4.6), M['lapis'])
        c.fill(c.circ(11.5, 9, 2.2), M['night'], 'plain', ol=False)
        c.p(10, 7.5, HL)
        for i in range(5):
            x = 3 + i * 4
            c.fill(c.rect(x, 22 - i, x + 1, 22), (M['sun'] if i < 3 else M['umber']), 'flat', ink=M['bronze'][4])
    else:
        c.fill(c.poly([(0, 5), (3, 2), (7.5, 1), (12, 2), (15, 5), (12, 8), (7.5, 9), (3, 8)]), M['white'])
        c.fill(c.circ(7.5, 5, 2.6), M['lapis'], 'plain', ol=False)
        c.p(7.5, 5, M['night'][2])
        for i in range(5):
            x = 1 + i * 3
            c.paint(c.rect(x, 14 - (i // 2), x, 14), (M['sun'] if i < 3 else M['bronze'])[2])
            c.paint(c.rect(x + 1, 14, x + 1, 14), M['bronze'][4])


@icon('misc', 'bach_van', 'Bạch Vân: con dấu mây trắng - công trình thủ công (nhãn/mây/lôi pháo).', MI_SRC + ' L301, L329-L333')
def bach_van(c):
    if c.n == 24:
        c.fill(c.circ(11.5, 11.5, 10.8), M['bronze'])
        c.fill(c.circ(11.5, 11.5, 8.6), M['stoned'], 'flat', ol=False)
        c.fill(cloud_m(c, 4, 6, 16, 10), M['white'], 'round', ink=M['lapis'][4])
        for (x, y, rr) in ((8, 11.5, 2.4), (14.5, 12.2, 2.2), (11.5, 9, 1.7)):
            c.paint(c.sector(x, y, rr - 1.1, rr, 150, 470), M['lapisl'][2])
        c.paint(c.line([(6, 15), (17, 15)], 1), M['lapisl'][1])
        c.ps([(11.5, 3.5), (11.5, 19.5), (3.5, 11.5), (19.5, 11.5)], M['gold'][0])
    else:
        c.fill(c.circ(7.5, 7.5, 7), M['bronze'])
        c.fill(c.circ(7.5, 7.5, 5.2), M['stoned'], 'plain', ol=False)
        c.fill(cloud_m(c, 3, 4, 10, 7), M['white'], 'flat', ink=M['lapis'][4])
        c.paint(c.sector(6, 7, .8, 1.6, 150, 470), M['lapisl'][2])
        c.paint(c.sector(9.5, 7.5, .8, 1.6, 150, 470), M['lapisl'][2])


@icon('misc', 'khoa', 'Khóa: ổ khóa - đặc tính chuyên biệt/Phong tỏa còn khóa.', MI_SRC + ' L842, L245, L810')
def khoa(c):
    if c.n == 24:
        sh = c.sector(11.5, 10, 4.2, 6.6, 180, 360) | c.rect(5, 10, 7.3, 11) | c.rect(15.7, 10, 18, 11)
        c.fill(sh, M['steel'], 'flat')
        c.fill(c.rrect(3, 10, 20, 22), M['gold'])
        c.fill(c.circ(11.5, 15, 2.3), M['umber'], 'plain', ol=False)
        c.fill(c.poly([(10.4, 15), (12.6, 15), (13, 19.5), (10, 19.5)]), M['umber'], 'plain', ol=False)
        c.paint(c.line([(5, 18), (6, 20)], 1), M['gold'][4])
        c.p(15, 12, M['gold'][0])
    else:
        sh = c.sector(7.5, 6, 2.6, 4.2, 180, 360) | c.rect(3, 6, 4.4, 7) | c.rect(10.6, 6, 12, 7)
        c.fill(sh, M['steel'], 'flat')
        c.fill(c.rrect(2, 6, 13, 14), M['gold'])
        c.paint(c.circ(7.5, 9.5, 1.3), M['umber'][2])
        c.paint(c.rect(7, 10, 8, 12), M['umber'][2])


# ----------------------------------------------------------------------------- build + preview
def render_all():
    res = []
    for group, name, vi, src, f in ICONS:
        pair = {}
        for n in (24, 16):
            c = Cv(n)
            f(c)
            c.center()
            pair[n] = c.out()
        res.append((group, name, vi, src, pair))
    return res


def write_all(res):
    meta = {}
    for group, name, vi, src, pair in res:
        d = os.path.join(OUT, group)
        os.makedirs(d, exist_ok=True)
        for n in (24, 16):
            pair[n].save(os.path.join(d, '%s_%d.png' % (name, n)))
        meta['%s/%s' % (group, name)] = dict(name=name, group=group, meaning_vi=vi, gdd_source=src,
                                              files=['%s/%s_24.png' % (group, name), '%s/%s_16.png' % (group, name)])
    with open(os.path.join(OUT, 'icons.json'), 'w', encoding='utf-8') as fh:
        json.dump(meta, fh, ensure_ascii=False, indent=2, sort_keys=True)


def preview(res, size, scale, fname):
    bg = (0x2a, 0x1a, 0x12)
    cell = size * scale
    pad = 14
    lab = 14
    per = 16
    groups = []
    for r in res:
        if not groups or groups[-1][0] != r[0]:
            groups.append((r[0], []))
        groups[-1][1].append(r)
    rows = sum((len(g[1]) + per - 1) // per for g in groups)
    W = per * (cell + pad) + pad
    H = rows * (cell + pad + lab) + pad + len(groups) * 14
    img = Image.new('RGBA', (W, H), bg + (255,))
    dr = ImageDraw.Draw(img)
    y = pad
    for gname, items in groups:
        dr.text((pad, y - 4), gname.upper(), fill=(244, 210, 122))
        y += 10
        for i, (_, name, _, _, pair) in enumerate(items):
            col, row = i % per, i // per
            x0 = pad + col * (cell + pad)
            y0 = y + row * (cell + pad + lab)
            dr.rectangle([x0 - 1, y0 - 1, x0 + cell, y0 + cell], fill=(0x3d, 0x26, 0x18))
            big = pair[size].resize((cell, cell), Image.NEAREST)
            img.alpha_composite(big, (x0, y0))
            dr.text((x0, y0 + cell + 2), name[:int(cell / 6)], fill=(221, 210, 182))
        y += ((len(items) + per - 1) // per) * (cell + pad + lab) + 4
    img.convert('RGB').save(os.path.join(OUT, fname))


def main():
    res = render_all()
    write_all(res)
    preview(res, 24, 4, 'preview_x4.png')
    preview(res, 16, 4, 'preview16_x4.png')
    # phone-like check: both sizes at 3x on one sheet is covered by the two previews above
    print('wrote %d icons to %s' % (len(res), OUT))
    names = [(r[0], r[1]) for r in res]
    assert len(names) == len(set(names))


if __name__ == '__main__':
    main()
