"""An - cutout/puppet pixel animation (deterministic).

  python tools/pixelart/an_puppet.py [--debug] [--clips idle,walk,...]

Pipeline: hi-res cutouts (.agent_scratch/an/cut_*.png) -> clean + head stylisation -> split into rig layers
(torso+hair+dress, 2 legs each with a boot piece, arms) -> pose every frame with affine/warp at hi-res
(20 hi-res px = 1 final px) -> premultiplied box downsample -> hard alpha -> shared k-means palette
(tools/pixelart/palettes/an.json) -> palette-bound umber outline -> sheets + json in Characters/an.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import pixelize as px  # noqa: E402  (kmeans / grade / quantize reuse)

CUT = ROOT / ".agent_scratch" / "an"
OUT = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters" / "an"
PAL = HERE / "palettes" / "an.json"

S = 16                       # hi-res px per final px
CELL = (56, 80)              # final cell (An ~67 px tall, Khoa ~72)
TARGET_H = 67                # soles -> top of hair, final px
MOT = 1.4                    # px-motion scale (taller sprite -> bigger strides/bobs)
SM = S * MOT
CW, CH = CELL[0] * S, CELL[1] * S
GROUND_MARGIN = 4            # final px under the soles (pivot y)
PIVOT = (CELL[0] // 2, GROUND_MARGIN)
PAD = 100                     # top padding for the stylised head
HEAD_GAIN = 1.45
HEAD_GAIN_X = 1.45
XSTRETCH = 1.0               # hero stylisation: slightly fuller figure so lace/hair read at 46 px
DIRS = ["down", "left", "up", "right"]
FX = {"down": 0, "left": -1, "up": 0, "right": 1}      # screen direction of "forward"
DEPTH = {"down": 1, "left": 0, "up": -1, "right": 0}   # frontal: +1 = forward foot lower on screen

# ---- per-view rig description, in the ORIGINAL cutout coordinates -----------------------------
VIEWS = {
    "down": dict(
        eyemid=(128.5, 88.5), hem=665, flare=34,
        neck=(127, 150), root_x=105, yc=458, yank=800,
        bounds=[(440, 80, 176), (560, 85, 172), (600, 75, 180), (640, 50, 190), (700, 42, 190), (770, 40, 185), (900, 38, 180)],
        sep=[(440, 125), (700, 122), (760, 112), (800, 106), (900, 106)],
        arms=[dict(poly=[(0, 170), (60, 170), (58, 250), (62, 345), (0, 345)], piv=(60, 195), tip=(28, 330)),
              dict(poly=[(194, 170), (254, 170), (254, 345), (196, 345)], piv=(194, 195), tip=(226, 330))],
        inpaint=False),
    "up": dict(
        eyemid=None, hem=665, flare=34,
        neck=(123, 150), root_x=122, yc=468, yank=800,
        bounds=[(440, 88, 168), (640, 92, 168), (660, 50, 190), (900, 50, 190)],
        sep=[(440, 126), (900, 126)],
        arms=[dict(poly=[(0, 170), (55, 170), (52, 250), (58, 345), (0, 345)], piv=(55, 195), tip=(25, 330)),
              dict(poly=[(192, 170), (247, 170), (247, 345), (189, 345)], piv=(192, 195), tip=(222, 330))],
        inpaint=False),
    "left": dict(
        eyemid=(111, 84), hem=665, flare=34,
        neck=(150, 150), root_x=120, yc=470, yank=800,
        bounds=[(440, 92, 176), (630, 100, 172), (650, 95, 200), (740, 90, 200), (790, 45, 205), (900, 45, 205)],
        sep=[(440, 135), (640, 140), (700, 150), (770, 150), (800, 132), (900, 132)],
        arms=[dict(poly=[(100, 190), (192, 190), (196, 304), (100, 308)], piv=(150, 200), tip=(130, 300)),
              dict(poly=[(45, 345), (100, 338), (96, 420), (76, 450), (72, 482), (38, 480), (34, 440)], piv=(78, 352), tip=(56, 470), parent=0)],
        inpaint=True),
    "right": dict(
        eyemid=(137, 85), hem=665, flare=34,
        neck=(112, 150), root_x=120, yc=466, yank=800,
        bounds=[(440, 85, 168), (600, 95, 160), (640, 55, 180), (900, 55, 185)],
        sep=[(440, 120), (900, 115)],
        arms=[dict(poly=[(78, 190), (172, 190), (176, 304), (82, 308)], piv=(118, 200), tip=(138, 300)),
              dict(poly=[(148, 345), (180, 342), (202, 400), (222, 445), (222, 480), (170, 480), (150, 420)], piv=(165, 352), tip=(195, 460), parent=0)],
        inpaint=True),
}


# ---- matrices ---------------------------------------------------------------------------------
def T(dx, dy):
    return np.array([[1, 0, dx], [0, 1, dy], [0, 0, 1.0]])


def R(deg, pivot):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    m = np.array([[c, -s, 0], [s, c, 0], [0, 0, 1.0]])
    return T(*pivot) @ m @ T(-pivot[0], -pivot[1])


def Sc(sx, sy, pivot):
    m = np.array([[sx, 0, 0], [0, sy, 0], [0, 0, 1.0]])
    return T(*pivot) @ m @ T(-pivot[0], -pivot[1])


def smooth(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


NL = 11
HAIR, SKIN, BLOUSE, SKIRT, LAPIS, BOOT, BAG, CORD, GOLDL, EYE, BELT = range(NL)
TORSO = {'down': (127, 70, 48), 'up': (123, 70, 48), 'left': (135, 48, 38), 'right': (118, 48, 38)}   # cx, shoulder half, waist half (orig px)
ARMS = {   # fitted sleeve S->E (blouse), forearm E->H (skin); orig coords
    'down': [dict(S=(51, 200), E=(47, 330), H=(47, 445)), dict(S=(203, 200), E=(207, 330), H=(207, 445))],
    'up': [dict(S=(47, 200), E=(43, 330), H=(43, 445)), dict(S=(199, 200), E=(203, 330), H=(203, 445))],
    'left': [dict(S=(140, 205), E=(125, 335), H=(108, 445))],
    'right': [dict(S=(113, 205), E=(126, 335), H=(142, 445))],
}
BAGC = {'down': (67, 485), 'left': (150, 485), 'right': (118, 485), 'up': None}
STRAP = {'down': [(185, 192)], 'left': [(125, 195)], 'right': [(112, 195)], 'up': [None]}
for _v, _sp in ARMS.items():
    if FX[_v] == 0:
        VIEWS[_v]["arms"] = [dict(piv=a["S"], tip=a["H"]) for a in _sp]
    else:
        VIEWS[_v]["arms"] = [dict(piv=_sp[0]["S"], tip=_sp[0]["E"]), dict(piv=_sp[0]["E"], tip=_sp[0]["H"], parent=0)]
YW = 400                      # waist row (orig cutout coords): sash + belt
PONY = {   # low ponytail strand to keep (orig coords); all other hair below the head is removed
    'down': [(168, 150), (238, 150), (240, 385), (178, 385)],
    'left': [(172, 140), (240, 140), (238, 385), (180, 385)],
    'up': [(98, 150), (150, 150), (148, 385), (100, 385)],
    'right': [(18, 140), (76, 140), (74, 385), (22, 385)],
}
NCH = NL + 2        # one-hot labels..., luminance, alpha (alpha is always the last channel)


def place(canvas, layer, M):
    """canvas: (CH,CW,NCH) premultiplied float32; layer: (arr, ox, oy); M maps layer image coords -> canvas."""
    arr, ox, oy = layer
    if arr is None:
        return
    h, w = arr.shape[:2]
    M2 = M @ T(ox, oy)
    corners = np.array([[0, 0, 1], [w, 0, 1], [0, h, 1], [w, h, 1]]).T
    pts = M2 @ corners
    x0 = max(int(math.floor(pts[0].min())) - 1, 0)
    x1 = min(int(math.ceil(pts[0].max())) + 1, CW)
    y0 = max(int(math.floor(pts[1].min())) - 1, 0)
    y1 = min(int(math.ceil(pts[1].max())) + 1, CH)
    if x1 <= x0 or y1 <= y0:
        return
    inv = np.linalg.inv(M2)
    A = np.array([[inv[1, 1], inv[1, 0]], [inv[0, 1], inv[0, 0]]])
    off = A @ np.array([y0, x0]) + np.array([inv[1, 2], inv[0, 2]])
    out = np.empty((y1 - y0, x1 - x0, arr.shape[2]), np.float32)
    for c in range(arr.shape[2]):
        out[..., c] = ndi.affine_transform(arr[..., c], A, offset=off, output_shape=(y1 - y0, x1 - x0), order=1, mode="constant", cval=0.0)
    reg = canvas[y0:y1, x0:x1]
    reg[:] = out + reg * (1 - out[..., -1:])


# ---- rig building -----------------------------------------------------------------------------
def to_layer(feat, mask):
    """feat (H,W,NL+1) straight + bool mask -> cropped premultiplied layer (arr with alpha last, ox, oy)."""
    ys, xs = np.nonzero(mask)
    if len(ys) == 0:
        return (None, 0, 0)
    y0, y1, x0, x1 = ys.min() - 2, ys.max() + 3, xs.min() - 2, xs.max() + 3
    y0, x0 = max(y0, 0), max(x0, 0)
    y1, x1 = min(y1, mask.shape[0]), min(x1, mask.shape[1])
    m = mask[y0:y1, x0:x1].astype(np.float32)
    out = np.concatenate([feat[y0:y1, x0:x1] * m[..., None], m[..., None]], axis=2).astype(np.float32)
    return (out, x0, y0)


def clean_small(mask, minpx):
    lab, n = ndi.label(mask, structure=np.ones((3, 3)))
    if n == 0:
        return mask
    sizes = ndi.sum(mask, lab, range(1, n + 1))
    return np.isin(lab, 1 + np.nonzero(sizes >= minpx)[0])


def flatten(rgb):
    """edge-preserving mean-shift: lace/print texture collapses into flat colour regions."""
    import cv2
    u8 = (np.clip(rgb, 0, 1) * 255).astype(np.uint8)
    f = cv2.pyrMeanShiftFiltering(u8, FLAT_SP, FLAT_SR, maxLevel=2)
    return f.astype(np.float32) / 255.0


FLAT_SP, FLAT_SR = 14, 28
# face feature anchors in ORIGINAL cutout coords (eyes, mouth)
EYES = {"down": [], "left": [], "up": [], "right": []}     # eyes are hand-painted at final res (see paint)
EYEMID = {"down": (128.5, 88.5), "left": (111, 84), "up": None, "right": (137, 85)}
LIPSP = {"down": (127, 106), "left": None, "up": None, "right": None}


class Rig:
    def __init__(self, view):
        self.view = view
        cfg = VIEWS[view]
        self.cfg = cfg
        im = Image.open(CUT / f"cut_{view}.png").convert("RGBA")
        k = 900.0 / im.height * (TARGET_H * S / (900.0 + (HEAD_GAIN - 1) * cfg["neck"][1]))
        self.k = k
        self.kx = k * XSTRETCH
        im = im.resize((round(im.width * self.kx), round(im.height * k)), Image.LANCZOS)
        a = np.asarray(im)
        rgb = a[..., :3].astype(np.float32) / 255
        mask = a[..., 3] > 127
        core = ndi.binary_erosion(mask, iterations=4)
        idx = ndi.distance_transform_edt(~core, return_distances=False, return_indices=True)
        rgb = rgb[idx[0], idx[1]]
        mask = ndi.binary_erosion(mask, iterations=2)
        rgb = flatten(rgb)
        rgb, mask = self.head_scale(rgb, mask)
        H, W = mask.shape
        self.H, self.W = H, W
        self.rgb, self.mask = rgb, mask
        kx = self.kx
        self.G = lambda x, y: (x * kx, y * k + PAD)
        yy, xx = np.mgrid[0:H, 0:W]
        self.yy, self.xx = yy, xx
        self.yo = (yy - PAD) / k
        self.build_layers()

    def head_scale(self, rgb, mask):
        k = self.k
        cx, cy = self.cfg["neck"][0] * self.kx, self.cfg["neck"][1] * k
        H, W = mask.shape
        blend = 150.0
        Ho = H + PAD
        yy, xx = np.mgrid[0:Ho, 0:W].astype(np.float32)
        y = yy - PAD
        w = np.where(y <= cy, 1.0, np.clip(1 - (y - cy) / blend, 0, 1))
        g = 1 + (HEAD_GAIN - 1) * w
        gx = 1 + (HEAD_GAIN_X - 1) * w
        sx = cx + (xx - cx) / gx
        sy = cy + (y - cy) / g
        out_rgb = np.empty((Ho, W, 3), np.float32)
        for c in range(3):
            out_rgb[..., c] = ndi.map_coordinates(rgb[..., c], [sy, sx], order=1, mode="nearest")
        m = ndi.map_coordinates(mask.astype(np.float32), [sy, sx], order=1, mode="constant") > 0.5
        return out_rgb, m

    def lerp_rows(self, table, y):
        t = np.array(table, np.float32)
        return [np.interp(y, t[:, 0], t[:, i]) for i in range(1, t.shape[1])]

    def classify(self, legs_mask, forearm_mask):
        """semantic label map from colour clusters + position priors (hi-res, cleaned cutout)."""
        rgb, yo, xo = self.rgb, self.yo, self.xx / self.kx
        r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
        lum = rgb @ np.array([0.299, 0.587, 0.114], np.float32)
        mx, mn = rgb.max(-1), rgb.min(-1)
        sat = (mx - mn) / np.maximum(mx, 1e-3)
        lab = np.full(lum.shape, BLOUSE, np.int8)
        dressy = np.ones(lum.shape, bool)
        nx = self.cfg["neck"][0]
        em = EYEMID[self.view]
        if em is None:
            lab[(yo < 135) & dressy] = HAIR
            lab[(yo >= 135) & (yo < 150) & dressy] = SKIN
        else:
            fw = 52 if self.view == 'down' else 36
            faceb = (np.abs(xo - em[0]) < fw) & (yo > 40)
            lab[(yo < 150) & dressy & faceb] = SKIN
            lab[(yo < 150) & dressy & ~faceb] = HAIR
        lab[(yo >= 150) & (yo < 196) & (np.abs(xo - nx) < 45) & (lum > 0.85)] = SKIN
        lab[legs_mask & (yo > 440) & (yo < 735)] = SKIN
        lab[forearm_mask & (lum > 0.8)] = SKIN
        # pink head ribbons / flowers become hair (the outfit has no pink)
        ribm = (yo < 215) & ((r - g) > 0.13) & (r > 0.8) & (lum > 0.6) & (np.abs(xo - nx) > 30)
        lab[ribm] = HAIR
        lab[(lum < 0.30)] = HAIR
        brown = (sat > 0.30) & ((r - b) > 0.16) & (lum > 0.24) & (lum < 0.56) & (yo > 270) & (r >= g) & (g >= b)
        lab[brown] = BAG
        gold = ((r - b) > 0.40) & ((g - b) > 0.25) & (r > 0.75) & ndi.binary_dilation(brown, iterations=25)
        lab[gold] = GOLDL
        lab[legs_mask & (yo >= 735)] = BOOT
        for (ex, ey) in EYES[self.view]:
            cx, cy = headpt(self, ex, ey)
            sel = (np.abs(self.xx - cx) < 15 * self.kx) & (np.abs(self.yy - cy) < 11) & (lum < 0.52)
            lab[sel] = EYE
        oh = np.stack([ndi.gaussian_filter((lab == l).astype(np.float32), 3.0) for l in range(NL)], -1)
        lab2 = oh.argmax(-1).astype(np.int8)
        keep = (lab == EYE) | (lab == GOLDL)
        lab2[keep] = lab[keep]
        l2 = ndi.gaussian_filter(lum, 5)
        l2 = l2 + 0.7 * (l2 - ndi.gaussian_filter(lum, 25))
        l2 = l2 + 0.05 * ((0.5 - self.xx / self.W) + (0.5 - self.yy / self.H))
        lb = ndi.gaussian_filter(lum, 14)
        l2 = np.where((lab2 == BOOT) | (lab2 == SKIN), lb + 0.5 * (lb - ndi.gaussian_filter(lum, 40)), l2)
        l2 = np.where(ribm | ((yo < 150) & (lab2 == HAIR) & (l2 > 0.55)), 0.22, l2)
        return lab2, l2.astype(np.float32)

    def capsule(self, p0, p1, w0, w1):
        """soft-edged capsule mask (orig coords -> hi-res), returns (mask, axis_x, radius)."""
        k, kx = self.k, self.kx
        a = np.array([p0[0] * kx, p0[1] * k + PAD], np.float32)
        b = np.array([p1[0] * kx, p1[1] * k + PAD], np.float32)
        d = b - a
        L2 = max(float(d @ d), 1.0)
        px_ = self.xx.astype(np.float32) - a[0]
        py_ = self.yy.astype(np.float32) - a[1]
        t = np.clip((px_ * d[0] + py_ * d[1]) / L2, 0, 1)
        cx_ = a[0] + t * d[0]
        cy_ = a[1] + t * d[1]
        dist = np.hypot(self.xx - cx_, self.yy - cy_)
        rad = (w0 + (w1 - w0) * t) / 2 * k
        return dist <= rad, cx_, rad

    def build_layers(self):
        cfg, k, kx = self.cfg, self.k, self.kx
        yo, xo = self.yo, self.xx / self.kx
        mask0 = self.mask
        yc, yank = cfg["yc"], cfg["yank"]
        xlo, xhi = self.lerp_rows(cfg["bounds"], yo)
        (xsep,) = self.lerp_rows(cfg["sep"], yo)
        inb = (xo >= xlo) & (xo <= xhi) & (yo >= 440)
        legA = mask0 & inb & (xo < xsep)
        legB = mask0 & inb & (xo >= xsep)
        legA = ndi.gaussian_filter(legA.astype(np.float32), 5) > 0.5
        legB = ndi.gaussian_filter(legB.astype(np.float32), 5) > 0.5
        labels, lum = self.classify(legA | legB, np.zeros_like(mask0))
        # ---- hair -> low ponytail
        pimg = Image.new("L", (self.W, self.H), 0)
        ImageDraw.Draw(pimg).polygon([self.G(*p) for p in PONY[self.view]], fill=255)
        pony = np.asarray(pimg) > 0
        hairm = labels == HAIR
        kill = hairm & (yo > 158) & ~pony
        kill |= hairm & pony & (yo > 385)
        valid = mask0 & ~kill
        body = ndi.binary_closing(valid, structure=np.ones((3, 3)), iterations=18)
        body = ndi.binary_fill_holes(body)
        body = ndi.gaussian_filter(body.astype(np.float32), 5) > 0.5
        body &= (yo < 740)
        idx = ndi.distance_transform_edt(~valid, return_distances=False, return_indices=True)
        lab2 = np.where(valid, labels, labels[idx[0], idx[1]]).astype(np.int8)
        lum2 = np.where(valid, lum, lum[idx[0], idx[1]])
        lab2[(~valid) & (lab2 == HAIR)] = BLOUSE
        yw, hemY = YW, cfg["hem"]
        cx, hs, hw = TORSO[self.view]
        cxs, halfw = cx, hw
        # ---- upper torso: fitted blouse (synthetic trapezoid, no frills)
        half = np.where(yo < 195, 24 + (hs - 24) * np.clip((yo - 165) / 30, 0, 1), hs + (hw - hs) * np.clip((yo - 195) / (yw - 195), 0, 1))
        upper = (np.abs(xo - cx) <= half) & (yo >= 165) & (yo <= yw + 6)
        upper = ndi.gaussian_filter(upper.astype(np.float32), 3) > 0.5
        head = body & (yo < 165)
        ux = np.clip((xo - cx) / max(hs, 1), -1, 1)
        blum = 0.62 - 0.26 * ux + 0.07 * np.cos(2 * math.pi * (xo - cx) / 46.0)
        # ---- skirt: A-line, knee to mid-calf
        t = np.clip((yo - yw) / (hemY - yw), 0, 1.3)
        fl = cfg.get("flare", 34) * np.power(np.clip(t, 0, 1), 0.9)
        sk_l, sk_r = cx - hw - fl, cx + hw + fl
        u = (xo - cx) / np.maximum((sk_r - sk_l) / 2, 1)
        hem_curve = hemY + 8 * np.clip(1 - u * u, 0, 1) * (1 if FX[self.view] == 0 else 0.4)
        skirt = (yo >= yw - 6) & (yo <= hem_curve) & (xo >= sk_l) & (xo <= sk_r)
        skirt = ndi.gaussian_filter(skirt.astype(np.float32), 3) > 0.5
        pleat = 0.5 + 0.27 * np.cos(2 * math.pi * 3.0 * u * 0.5 + 0.6)
        tier = np.where((t > 0.52) & (t < 0.60), -0.28, 0.0)
        hemd = -0.30 * np.clip((t - 0.80) / 0.2, 0, 1)
        slum = pleat + tier + hemd - 0.12 * u
        sash = upper & (yo > yw - 30) & (yo <= yw + 6)
        belt = skirt & (yo > yw + 6) & (yo <= yw + 20)
        ponyh = (lab2 == HAIR) & pony & body & (yo >= 165)
        lab_f = lab2.copy()
        lum_f = lum2.copy()
        torso_core = head | upper | skirt | ponyh
        ub = upper & ~head
        lab_f[ub] = BLOUSE
        lum_f[ub] = blum[ub]
        if self.view != "up":
            if FX[self.view] == 0:
                v = upper & (yo < 225) & (np.abs(xo - cx) < 34 * (1 - np.clip((yo - 165) / 60, 0, 1)))
            else:
                v = upper & (yo < 185) & (np.abs(xo - cx) < 20)
            lab_f[v] = SKIN
            lum_f[v] = (0.9 - 0.2 * ux)[v]
        sb = skirt & ~upper
        lab_f[sb] = SKIRT
        lum_f[sb] = slum[sb]
        lab_f[ponyh] = HAIR
        lum_f[ponyh] = lum2[ponyh]
        lab_f[sash] = LAPIS
        lum_f[sash] = (0.65 - 0.5 * np.clip((yo - (yw - 30)) / 36, 0, 1))[sash]
        lab_f[belt] = BELT
        lum_f[belt] = (0.5 - 0.2 * np.clip((yo - (yw + 6)) / 14, 0, 1))[belt]
        # ---- satchel: 4x4 px bag on her right hip, 1 px strap, gold clasp
        if self.view != "up":
            bc = BAGC[self.view]
            bx, by = bc[0] * kx, bc[1] * k + PAD
            sq = (np.abs(self.xx - bx) <= 32) & (np.abs(self.yy - by) <= 32)
            sp = STRAP[self.view][0]
            sp0 = (sp[0] * kx, sp[1] * k + PAD)
            d = np.array([bx - sp0[0], by - 30 - sp0[1]], np.float32)
            L2 = max(float(d @ d), 1.0)
            tt = np.clip(((self.xx - sp0[0]) * d[0] + (self.yy - sp0[1]) * d[1]) / L2, 0, 1)
            sd = np.hypot(self.xx - (sp0[0] + tt * d[0]), self.yy - (sp0[1] + tt * d[1]))
            strap = (sd <= 9) & (yo < 520) & (upper | skirt)
            bag_mask = sq | strap
            lab_f[bag_mask] = BAG
            lum_f[bag_mask] = np.where(sq, np.where(self.yy < by - 6, 0.62, 0.35), 0.32)[bag_mask]
            clasp = (np.abs(self.xx - bx) <= 9) & (np.abs(self.yy - (by - 4)) <= 9)
            lab_f[clasp] = GOLDL
            torso_core = torso_core | bag_mask
        torso_all = torso_core
        rootm = (lab_f == HAIR) & pony & (yo > 160) & (yo < 195) & body
        if rootm.any():
            ys_, xs2 = np.nonzero(rootm)
            cy_, cx_ = ys_.mean(), xs2.mean()
            disc = ((self.yy - cy_) ** 2 + ((self.xx - cx_) * 0.8) ** 2) < (20 ** 2)
            lab_f[disc & (lab_f == HAIR)] = CORD
        # ---- fitted elbow-length sleeves + forearms (synthetic capsules)
        amasks = []
        for spec in ARMS[self.view]:
            S0, E0, H0 = spec["S"], spec["E"], spec["H"]
            sm, scx, srad = self.capsule(S0, E0, 46, 40)
            fm, fcx, frad = self.capsule(E0, H0, 28, 22)
            hm, hcx, hrad = self.capsule(H0, H0, 30, 30)
            fm = fm | hm
            if FX[self.view] != 0:
                amasks.append((sm, [(sm, BLOUSE, scx, srad)]))
                amasks.append((fm & ~sm, [(fm & ~sm, SKIN, fcx, frad)]))
            else:
                amasks.append((sm | fm, [(sm, BLOUSE, scx, srad), (fm & ~sm, SKIN, fcx, frad)]))
        arm_masks, arm_lab, arm_lum = [], [], []
        for m_, parts in amasks:
            la = np.full(m_.shape, BLOUSE, np.int8)
            lu = np.zeros(m_.shape, np.float32)
            for (pm, lb, ax_, rad) in parts:
                across = np.clip((self.xx - ax_) / np.maximum(rad, 1), -1, 1)
                la[pm] = lb
                base = 0.62 if lb == BLOUSE else 0.9
                lu[pm] = (base - 0.26 * across)[pm]
            arm_masks.append(m_)
            arm_lab.append(la)
            arm_lum.append(lu)
        self.labels = lab_f
        feat = np.concatenate([np.eye(NL, dtype=np.float32)[lab_f], lum_f[..., None]], axis=2)
        mask = torso_all | body
        self.mask = mask
        torso = torso_all & ~(inb & (yo >= hemY + 10))
        torso = clean_small(torso, 150)
        ank = yank * k + PAD
        legs = []
        for lm in (legA, legB):
            lm = clean_small(lm, 400)
            up = lm & (self.yy < ank + 14)
            boot = lm & (self.yy >= ank)
            legs.append(dict(up=to_layer(feat, up), boot=to_layer(feat, boot), mask=lm))
        self.torso = to_layer(feat, torso)
        self.torso_mask = torso
        self.legs = legs
        self.arms = []
        for m_, la, lu in zip(arm_masks, arm_lab, arm_lum):
            f2 = np.concatenate([np.eye(NL, dtype=np.float32)[la], lu[..., None]], axis=2)
            self.arms.append(to_layer(f2, m_))
        self.hip = []
        for lm in (legA, legB):
            ys, xs = np.nonzero(lm & (np.abs(yo - yc) < 6))
            self.hip.append((xs.mean() if len(xs) else self.W / 2, (yc - 30) * k + PAD))
        self.ank = []
        for lm in (legA, legB):
            ys, xs = np.nonzero(lm & (np.abs(self.yy - ank) < 6))
            self.ank.append((xs.mean() if len(xs) else self.W / 2, ank + 14))
        self.hipc = ((self.hip[0][0] + self.hip[1][0]) / 2, (yc - 30) * k + PAD)
        self.Lleg = self.H - self.hip[0][1]
        self.root_img = (cfg["root_x"] * self.kx, float(self.H))
        hair = (lab_f == HAIR) & torso
        hair_w = np.clip(ndi.gaussian_filter(hair.astype(np.float32), 9) * 1.6, 0, 1)
        ramp_h = np.clip((yo - 150) / 160, 0, 1)
        self.hair_wt = (hair_w * ramp_h).astype(np.float32)
        hem = np.clip((yo - yw) / (hemY - yw), 0, 1) ** 1.2 * (0.5 + 0.5 * np.clip(np.abs(xo - cxs) / max(halfw + 20, 1), 0, 1))
        self.hem_wt = (np.where(yo > yw, hem, 0) * skirt).astype(np.float32)
        self.skin = np.concatenate([np.eye(NL, dtype=np.float32)[SKIN], [0.85]])
        self.shade_q = {}
        for l, qs in SHADE_Q.items():
            vs = [lum_f[mask & (lab_f == l)]]
            for m_, la, lu in zip(arm_masks, arm_lab, arm_lum):
                vs.append(lu[m_ & (la == l)])
            v = np.concatenate(vs)
            self.shade_q[l] = np.quantile(v, qs) if len(v) > 50 else np.array([0.5] * len(qs))

    def warp_torso(self, hair_dx, hair_dy, hem_dx, phase_h, phase_m):
        arr, ox, oy = self.torso
        h, w = arr.shape[:2]
        sl = (slice(oy, oy + h), slice(ox, ox + w))
        yy = self.yy[sl].astype(np.float32)
        xx = self.xx[sl].astype(np.float32)
        hw, mw = self.hair_wt[sl], self.hem_wt[sl]
        yo = self.yo[sl]
        wave_h = np.sin(phase_h - yo * 0.011)
        wave_m = np.sin(phase_m - yo * 0.018 + xx * 0.02)
        dx = hair_dx[0] * hw + hair_dx[1] * hw * wave_h + hem_dx[0] * mw + hem_dx[1] * mw * wave_m
        dy = hair_dy * hw * wave_h
        if not np.any(dx) and not np.any(dy):
            return self.torso
        out = np.empty_like(arr)
        for c in range(arr.shape[2]):
            out[..., c] = ndi.map_coordinates(arr[..., c], [np.arange(h)[:, None] - dy, np.arange(w)[None, :] - dx], order=1, mode="constant")
        return (out, ox, oy)



# ---- posing -----------------------------------------------------------------------------------
MOUTH = {"down": (127, 107), "left": (111, 111), "up": (124, 110), "right": (152, 108)}


def headpt(rig, x, y):
    k = rig.k
    cx, cy = rig.cfg["neck"]
    if y <= cy:
        x = cx + (x - cx) * HEAD_GAIN_X
        y = cy + (y - cy) * HEAD_GAIN
    return (x * rig.kx, y * k + PAD)


def ang_of(v):
    return math.degrees(math.atan2(v[1], v[0]))


def aim(piv, tip, target, maxs=(0.9, 1.15)):
    """matrix-degrees (+stretch) so that the arm vector piv->tip points at target."""
    v0 = (tip[0] - piv[0], tip[1] - piv[1])
    v1 = (target[0] - piv[0], target[1] - piv[1])
    d = ang_of(v1) - ang_of(v0)
    st = math.hypot(*v1) / max(math.hypot(*v0), 1e-6)
    return d, float(np.clip(st, *maxs))


def draw_blob(canvas, shape, color, alpha):
    """alpha-over of a PIL-drawn mask (hi-res) with a feature vector (labels+lum) onto the premultiplied canvas."""
    if alpha <= 0.01:
        return
    m = np.asarray(shape, np.float32) / 255.0 * alpha
    col = np.array(color, np.float32)
    canvas[:] = np.concatenate([col * m[..., None], m[..., None]], axis=2) + canvas * (1 - m[..., None])


def render(rig, P):
    """P: pose dict -> (CH,CW,4) premultiplied hi-res canvas."""
    view = rig.view
    fx = FX[view]
    side = fx != 0
    canvas = np.zeros((CH, CW, NCH), np.float32)
    Tc = T(CW / 2 - rig.root_img[0], CH - GROUND_MARGIN * S - rig.root_img[1])
    rdx, rdy = P.get("root_dx", 0) * SM, P.get("root_dy", 0) * SM
    L = rig.Lleg
    aleg, lift, foff = P["aleg"], P["lift"], P.get("foff", [0, 0])
    if side:
        dyh = min(L * (1 - math.cos(math.radians(aleg[i]))) + lift[i] * SM for i in range(2))
    else:
        dyh = P.get("bob", 0) * SM
    legM = []
    for i in range(2):
        if side:
            rot = -fx * aleg[i]
            M = Tc @ T(rdx, rdy + dyh - lift[i] * SM) @ R(rot, rig.hip[i])
            Mb = M @ R(-P.get("boot_comp", 0.85) * rot, rig.ank[i])
        else:
            sq = 1 - 0.018 * lift[i]
            M = Tc @ T(rdx, rdy + dyh + (foff[i] - lift[i]) * SM) @ Sc(1, sq, rig.hip[i]) @ R(P.get("splay", 0) * (1 if i == 0 else -1), rig.hip[i])
            Mb = M
        legM.append((M, Mb))
    order = [1, 0] if side else [0, 1]
    for i in order:
        place(canvas, rig.legs[i]["up"], legM[i][0])
        place(canvas, rig.legs[i]["boot"], legM[i][1])
    # torso group
    up_dy = (P.get("breath", 0) + P.get("crouch", 0)) * SM
    lean = P.get("lean", 0)
    sqz = P.get("squash", 1.0)
    Mg = Tc @ T(rdx, rdy + dyh + up_dy) @ R(fx * lean, rig.hipc) @ Sc(1, sqz, rig.hipc) @ R(P.get("roll", 0), rig.hipc)
    hd = P.get("hair", {})
    torso = rig.warp_torso((hd.get("dx0", 0) * SM, hd.get("dx", 0) * SM), hd.get("dy", 0) * SM,
                           (P.get("hem0", 0) * SM, P.get("hem", 0) * SM), hd.get("ph", 0), P.get("hem_ph", 0))
    behind = P.get("arm_behind", False)
    arms = P.get("arms", [])

    def draw_arms():
        cfg = rig.cfg
        Mu = [None] * len(rig.arms)
        for j, ad in enumerate(arms):
            a = cfg["arms"][j]
            par = a.get("parent")
            base = Mg if par is None else Mu[par]
            piv = a["piv"]
            pv = (piv[0] * rig.kx, piv[1] * rig.k + PAD)
            Mj = base @ T(ad.get("dx", 0) * SM, ad.get("dy", 0) * SM) @ R(ad.get("rot", 0), pv) @ Sc(ad.get("st", 1), ad.get("st", 1), pv)
            Mu[j] = Mj
        # skin bridge between sleeve and forearm (side views, raised arms)
        if False:
            img = Image.new("L", (CW, CH), 0)
            d = ImageDraw.Draw(img)
            a0 = cfg["arms"][0]
            p0 = np.array([a0["tip"][0] * rig.kx, a0["tip"][1] * rig.k + PAD, 1.0])
            p0[0] += -18 if fx < 0 else 18
            p0[1] -= 40
            a1 = cfg["arms"][1]
            p1 = np.array([a1["piv"][0] * rig.kx, a1["piv"][1] * rig.k + PAD, 1.0])
            q0, q1 = (Mu[0] @ p0)[:2], (Mu[1] @ p1)[:2]
            d.line([tuple(q0), tuple(q1)], fill=255, width=34)
            for q in (q0, q1):
                d.ellipse([q[0] - 17, q[1] - 17, q[0] + 17, q[1] + 17], fill=255)
            draw_blob(canvas, img, rig.skin, 1.0)
        for j in range(len(rig.arms)):
            place(canvas, rig.arms[j], Mu[j] if j < len(arms) else Mg)
        for j, ad in enumerate(arms):
            v = ad.get("hand", 0)
            if v > 0.02 and not (side and j == 1):
                tip = cfg["arms"][j]["tip"]
                q = (Mu[j] @ np.array([tip[0] * rig.kx, tip[1] * rig.k + PAD, 1.0]))[:2]
                img = Image.new("L", (CW, CH), 0)
                r = 40 * ad.get("hs", 1.0)
                ImageDraw.Draw(img).ellipse([q[0] - r, q[1] - r * 0.9, q[0] + r, q[1] + r * 0.9], fill=255)
                draw_blob(canvas, img, rig.skin, v)

    if behind:
        draw_arms()
    place(canvas, torso, Mg)
    if not behind:
        draw_arms()
    anc = None
    if rig.cfg.get("eyemid") is not None:
        ex, ey = headpt(rig, *rig.cfg["eyemid"])
        q = Mg @ np.array([ex, ey, 1.0])
        anc = (q[0] / S, q[1] / S)
    return canvas, anc



# ---- clip definitions ---------------------------------------------------------------------------
def base_pose():
    return dict(aleg=[0, 0], lift=[0, 0], foff=[0, 0], arms=[dict(), dict()], hair={}, hem=0, hem0=0)


def cosp(t):
    return math.cos(2 * math.pi * t)


def side_arms(view, a1, a2):
    fx = FX[view]
    return [dict(rot=-fx * a1), dict(rot=-fx * a2)]


def pose_idle(view, i, n, rig):
    t = i / n
    P = base_pose()
    fx = FX[view]
    br = 0.5 - 0.5 * cosp(t)
    P["breath"] = -1.0 * br
    P["hair"] = dict(dx0=0, dx=0.30, dy=0.25 if not fx else 0.0, ph=2 * math.pi * t)
    P["hem"] = 0.30
    P["hem_ph"] = 2 * math.pi * t + 1.0
    if fx:
        P["arms"] = [dict(rot=-fx * (2.0 * br)), dict(rot=-fx * (2.0 * br))]
    else:
        P["arms"] = [dict(rot=2.0 * br, dy=-0.2 * br), dict(rot=-2.0 * br, dy=-0.2 * br)]
    return P


def gait(view, P, ph, amp, lift, stride, bob, arm_sw, arm_bend=0, splay=0):
    fx = FX[view]
    s = math.sin(ph)
    P["lift"] = [lift * max(0, math.cos(ph)), lift * max(0, -math.cos(ph))]
    if fx:
        P["aleg"] = [amp * s, -amp * s]
        P["arms"] = [dict(rot=-fx * (-arm_sw * s * 0.9 + arm_bend * 0.4)), dict(rot=-fx * (-arm_sw * s * 0.9 + arm_bend))]
    else:
        d = DEPTH[view]
        P["foff"] = [d * stride * s, -d * stride * s]
        P["bob"] = bob * s * s
        P["splay"] = splay
        P["roll"] = 1.2 * s
        P["arms"] = [dict(rot=3, dy=-d * 0.9 * s * arm_sw / 12), dict(rot=-3, dy=d * 0.9 * s * arm_sw / 12)]


def pose_walk(view, i, n, rig):
    ph = 2 * math.pi * i / n
    P = base_pose()
    fx = FX[view]
    gait(view, P, ph, 15, 1.7, 1.0, 0.8, 9)
    P["hair"] = dict(dx0=-fx * 0.25, dx=0.55, dy=0.45 if not fx else 0.0, ph=ph - 0.8)
    P["hem"] = 1.0
    P["hem_ph"] = ph + 0.4
    P["breath"] = -0.25 * math.cos(2 * ph)
    return P


def pose_run(view, i, n, rig):
    ph = 2 * math.pi * i / n
    P = base_pose()
    fx = FX[view]
    gait(view, P, ph, 36, 3.6, 1.7, 1.4, 30, arm_bend=48, splay=2)
    P["lean"] = 9 if fx else 0
    if not fx:
        P["squash"] = 0.985
    P["hair"] = dict(dx0=-fx * 1.7, dx=0.9, dy=0.9 if not fx else 0.3, ph=ph - 1.0)
    P["hem0"] = -fx * 0.9
    P["hem"] = 0.9
    P["hem_ph"] = ph
    P["breath"] = -0.4 * math.cos(2 * ph)
    return P


PROF_INTERACT = [0.2, 0.65, 1.0, 1.0, 0.6, 0.2]


def pose_interact(view, i, n, rig):
    p = PROF_INTERACT[i]
    P = base_pose()
    fx = FX[view]
    P["hair"] = dict(dx0=0, dx=0.4, dy=0.4, ph=2 * math.pi * i / n)
    P["hem"] = 0.4
    P["hem_ph"] = 1.0 + i
    P["crouch"] = 1.5 * p
    if fx:
        P["aleg"] = [6 * p, -6 * p]
        P["lean"] = 30 * p
        P["arms"] = side_arms(view, 38 * p, 62 * p)
        P["bridge"] = p
    else:
        P["squash"] = 1 - 0.07 * p
        P["crouch"] = 2.2 * p
        d = DEPTH[view]
        P["foff"] = [d * 0.8 * p, d * 0.8 * p]
        P["arms"] = [dict(rot=-30 * p, dy=0.5 * p, hand=p), dict(rot=30 * p, dy=0.5 * p, hand=p)]
    return P


PROF_WHISTLE = [0.0, 0.45, 0.85, 1.0, 1.0, 0.9, 0.45, 0.0]


def pose_whistle(view, i, n, rig):
    p = PROF_WHISTLE[i]
    P = base_pose()
    fx = FX[view]
    P["hair"] = dict(dx0=0, dx=0.35, dy=0.3, ph=2 * math.pi * i / n)
    P["hem"] = 0.3
    P["hem_ph"] = 0.5 + i
    P["breath"] = -0.5 * (0.5 - 0.5 * cosp(i / n))
    mx, my = headpt(rig, *MOUTH[view])
    if fx:
        P["arms"] = [dict(rot=-fx * 50 * p), dict(rot=-fx * (50 + 118) * p)]
        P["bridge"] = min(1.0, p * 1.5)
    else:
        j = 0 if view == "down" else 1
        cfg = rig.cfg["arms"][j]
        pv = (cfg["piv"][0] * rig.kx, cfg["piv"][1] * rig.k + PAD)
        tp = (cfg["tip"][0] * rig.kx, cfg["tip"][1] * rig.k + PAD)
        d, st = aim(pv, tp, (mx, my + 10))
        P["arms"] = [dict(), dict()]
        P["arms"][j] = dict(rot=d * p, st=1 + (st - 1) * p, hand=min(1, p * 1.6))
    return P


def pose_attack(view, i, n, rig):
    P = base_pose()
    fx = FX[view]
    # (legA, legB, lift A, lean, upper arm, forearm, root dx, crouch)
    tab = [
        (10, -8, 0, 0, 12, 14, 0.0, 0.0),
        (0, 0, 0, -6, -30, 70, -0.3, 1.0),
        (-4, 4, 0, -9, -42, 80, -0.6, 1.6),
        (30, -24, 2.0, 7, 30, 40, 0.5, 0.6),
        (34, -26, 0, 12, 88, 2, 1.2, 0.2),
        (33, -26, 0, 11, 90, 0, 1.2, 0.2),
        (22, -16, 0, 5, 48, 40, 0.5, 0.2),
        (12, -9, 0, 1, 14, 20, 0.0, 0.0),
    ]
    la, lb, lf, ln, a1, a2, rdx, cr = tab[i]
    P["hair"] = dict(dx0=-fx * (0.9 if i in (3, 4, 5) else 0.3), dx=0.7, dy=0.5, ph=1.2 * i)
    P["hem0"] = -fx * (0.6 if i in (3, 4, 5, 6) else 0)
    P["hem"] = 0.7
    P["hem_ph"] = 1.0 * i
    P["crouch"] = cr
    if fx:
        P["aleg"] = [la, lb]
        P["lift"] = [lf, 0]
        P["lean"] = ln
        P["root_dx"] = fx * rdx
        P["arms"] = side_arms(view, a1, a1 + a2)
        P["bridge"] = float(np.clip(a1 / 40.0, 0, 1)) if a1 > 15 else 0.0
    else:
        d = DEPTH[view]
        P["foff"] = [d * la / 14.0, d * lb / 14.0]
        P["lift"] = [lf, 0]
        P["squash"] = 1 - 0.015 * ln
        e = float(np.clip((a1 - 10) / 80.0, 0, 1))
        cfg = rig.cfg["arms"][0]
        pv = (cfg["piv"][0] * rig.kx, cfg["piv"][1] * rig.k + PAD)
        tp = (cfg["tip"][0] * rig.kx, cfg["tip"][1] * rig.k + PAD)
        tgt = (rig.cfg["root_x"] * rig.kx - 5, pv[1] + 60)
        dd, st = aim(pv, tp, tgt)
        back = float(np.clip(-a1 / 40.0, 0, 1))
        if view == "down":
            P["arms"] = [dict(rot=dd * e - 12 * back, st=1 + (st - 1) * e, hand=e, hs=1.0 + 0.55 * e, dy=-0.3 * back), dict(rot=-2, dy=0.3 * e)]
        else:
            P["arms"] = [dict(rot=dd * e * 0.8 - 12 * back, hand=e * 0.7), dict(rot=-2)]
    return P


CLIPS = {
    "idle": dict(n=6, fps=6.0, fn=pose_idle),
    "walk": dict(n=8, fps=10.0, fn=pose_walk),
    "run": dict(n=8, fps=14.0, fn=pose_run),
    "interact": dict(n=6, fps=10.0, fn=pose_interact),
    "whistle": dict(n=8, fps=8.0, fn=pose_whistle),
    "attack": dict(n=8, fps=14.0, fn=pose_attack),
}



# ---- paint-by-numbers ------------------------------------------------------------------------------
GOLD = (201, 154, 50, 255)


def hx(s):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


# dark -> light ramps (hand-picked)
RAMPS = {
    HAIR: ["1c1214", "2e1f22", "4a3436", "7a5a5c"],
    SKIN: ["c98f7c", "e8b9a4", "f6d9c8"],
    BLOUSE: ["8a7455", "a8906c", "d6c3a0", "efe3c8"],
    SKIRT: ["5c3d18", "7e5420", "b07a2c", "d9a441"],
    LAPIS: ["26406a", "3a5f8f"],
    BOOT: ["3a2414", "523520", "7a5234"],
    BAG: ["5e3a24", "8a5a3a"],
    CORD: ["a8382a"],
    GOLDL: ["e2b44c"],
    EYE: ["1c1214"],
    BELT: ["4a301c", "6b4a2e"],
}
EXTRA = ["9c6a4e", "b4604f", "efb59e", "fbf2ec", "f9e3d2"]   # warm rim strand, mouth, cheek, eye highlight, skin highlight
SHADE_Q = {HAIR: [0.35, 0.75, 0.96], SKIN: [0.22, 0.62], BLOUSE: [0.12, 0.4, 0.75], SKIRT: [0.12, 0.4, 0.72], LAPIS: [0.5],
           BOOT: [0.25, 0.6], BAG: [0.5], BELT: [0.5]}
PRIORITY = [(BAG, 0.40), (LAPIS, 0.25), (BELT, 0.30), (GOLDL, 0.10), (EYE, 0.10)]
DRESS = (BLOUSE, SKIRT, LAPIS)


def palette_rgb():
    cols = []
    for l in range(NL):
        for h in RAMPS[l]:
            c = hx(h)
            if c not in cols:
                cols.append(c)
    for h in EXTRA:
        cols.append(hx(h))
    return np.array(cols, np.float32) / 255.0


def down_to_cell(canvas):
    c = canvas.reshape(CELL[1], S, CELL[0], S, NCH).mean(axis=(1, 3))
    return c


def nb4(a):
    """number of opaque 4-neighbours"""
    n = np.zeros(a.shape, int)
    n[1:] += a[:-1]
    n[:-1] += a[1:]
    n[:, 1:] += a[:, :-1]
    n[:, :-1] += a[:, 1:]
    return n


def paint(cell, rig, anc=None):
    """premultiplied downsampled cell (CELL[1],CELL[0],NCH) -> RGBA float with palette colours only."""
    A = cell[..., -1]
    opaque = A >= 0.5
    opaque &= nb4(opaque) >= 1
    cov = cell[..., :NL] / np.maximum(A[..., None], 1e-6)
    lum = cell[..., NL] / np.maximum(A, 1e-6)
    lab = cov.argmax(-1)
    for l, thr in PRIORITY:
        if l == EYE:
            continue
        if l == GOLDL:
            sel = (cell[..., l] >= 0.08) & (A > 0.25)
        else:
            sel = cov[..., l] >= thr
        lab = np.where(sel & opaque, l, lab)
    lab = np.where(opaque, lab, -1)
    H, W = lab.shape
    colors = np.zeros((H, W, 3), np.float32)
    shade = np.zeros((H, W), int)
    for l, qs in rig.shade_q.items():
        m = lab == l
        shade[m] = (lum[m][:, None] > np.array(qs)[None, :]).sum(1)
    col = lambda hexs: np.array(hx(hexs), np.float32) / 255
    # light from the upper left: left side one band lighter, right side one band darker
    cols_ = np.arange(W)[None, :] + 0 * np.arange(H)[:, None]
    side = -np.clip(np.round((cols_ - W / 2.0) / 4.5), -1, 1).astype(int)
    lit = np.isin(lab, (BLOUSE, SKIRT, LAPIS, BELT, BOOT, SKIN))
    for l in range(NL):
        m = lab == l
        if m.any():
            ramp = [col(h) for h in RAMPS[l]]
            sh = shade + (side if l in (BLOUSE, SKIRT, LAPIS, BELT, BOOT, SKIN) else 0)
            sh = np.clip(sh, 0, len(ramp) - 1)
            for s_ in range(len(ramp)):
                colors[m & (sh == s_)] = ramp[s_]
    left = np.full((H, W), -1)
    left[:, 1:] = lab[:, :-1]
    below = np.full((H, W), -1)
    below[:-1] = lab[1:]
    above = np.full((H, W), -1)
    above[1:] = lab[:-1]
    rows = np.arange(H)[:, None]
    # warm rim-light strand on the lit (left) edge of the hair
    rim = (lab == HAIR) & (shade >= 2) & (left != HAIR)
    colors[rim] = col(EXTRA[0])
    # skirt hem: darkest ochre on the lowest skirt row
    hem = (lab == SKIRT) & np.isin(below, (-1, SKIN, BOOT)) & (rows > H * 0.4)
    colors[hem] = col(RAMPS[SKIRT][0])
    # umber inner seam lines: blouse/sash and sash/belt, skirt top, belt/skirt
    seam = (lab == BLOUSE) & (below == LAPIS)
    colors[seam] = col(RAMPS[BELT][0])
    seam2 = (lab == SKIRT) & (above == BELT)
    colors[seam2] = col(RAMPS[BELT][0])
    sole = (lab == BOOT) & (below == -1)
    colors[sole] = col(RAMPS[BOOT][0])
    # cord tie: small deep-red mark at the ponytail root
    cc = cov[..., CORD] * opaque
    if cc.sum() > 0.12:
        yy, xx = np.unravel_index(int(np.argmax(cc)), cc.shape)
        colors[yy, xx] = col(RAMPS[CORD][0])
        lab[yy, xx] = CORD
        nb = [(yy, xx + 1), (yy + 1, xx), (yy, xx - 1), (yy - 1, xx)]
        nb = [p for p in nb if 0 <= p[0] < H and 0 <= p[1] < W and opaque[p] and cc[p] > 0.04]
        if nb:
            p = max(nb, key=lambda q: cc[q])
            colors[p] = col(RAMPS[CORD][0])
    # satchel: at least 3x3 and a gold clasp pixel (front/side)
    if rig.view != "up" and (lab == BAG).sum() >= 2:
        bag = lab == BAG
        for _ in range(2):
            if bag.sum() >= 9:
                break
            grow = ndi.binary_dilation(bag, structure=np.ones((3, 3))) & opaque & ~np.isin(lab, (HAIR, EYE, SKIN))
            lab = np.where(grow, BAG, lab)
            colors[grow & ~bag] = col(RAMPS[BAG][1])
            bag = lab == BAG
        ys, xs = np.nonzero(bag)
        j = int(np.argmax(ys * 100 + xs * (-1 if rig.view == "left" else 1)))
        colors[ys[j] - 1 if ys[j] > 0 else ys[j], xs[j]] = col(RAMPS[GOLDL][0])

    # ---- hand-designed face (pixel rules anchored on the posed eye midpoint) ----
    def put(y, x, hexs):
        if 0 <= y < H and 0 <= x < W and opaque[y, x]:
            colors[y, x] = col(hexs)
            lab[y, x] = SKIN if hexs not in ("1c1214",) else EYE

    if anc is not None:
        c = int(round(anc[0])) - 1
        cy = int(round(anc[1])) - 1
        SK = RAMPS[SKIN]
        if rig.view == "down":
            for y in range(cy - 2, cy + 5):
                for x in range(c - 4, c + 6):
                    base = SK[2] if x <= c else SK[1]
                    if x == c + 5:
                        base = SK[0]
                    if y == cy + 4:
                        base = SK[1] if x <= c else SK[0]
                    put(y, x, base)
            for ex in (c - 3, c + 3):
                for dy in (0, 1):
                    for dx in (0, 1):
                        put(cy + dy, ex + dx, "1c1214")
                put(cy, ex, EXTRA[3])
                put(cy - 2, ex, "4a3436")
                put(cy - 2, ex + 1, "4a3436")
            put(cy + 2, c + 1, SK[0])
            put(cy + 3, c, EXTRA[1])
            put(cy + 3, c + 1, EXTRA[1])
            put(cy + 2, c - 3, EXTRA[2])
            put(cy + 2, c + 4, EXTRA[2])
        elif rig.view in ("left", "right"):
            f = FX[rig.view]
            for y in range(cy - 2, cy + 5):
                for x in range(c - 1, c + 3):
                    put(y, x, SK[2] if x <= c else SK[1])
            put(cy, c, "1c1214"); put(cy, c + 1, "1c1214")
            put(cy + 1, c, "1c1214"); put(cy + 1, c + 1, "1c1214")
            put(cy, c if f < 0 else c + 1, EXTRA[3])
            put(cy - 2, c, "4a3436"); put(cy - 2, c + 1, "4a3436")
            nx = c - 1 if f < 0 else c + 2
            put(cy + 2, nx, SK[0])
            put(cy + 3, nx, EXTRA[1])
            put(cy + 2, c + 1 if f < 0 else c, EXTRA[2])
    out = np.zeros((H, W, 4), np.float32)
    out[..., :3] = np.where(opaque[..., None], colors, 0)
    out[..., 3] = opaque
    return out


def build_frames(clips):
    frames = {}
    rigs = {}
    for view in DIRS:
        rig = Rig(view)
        rigs[view] = rig
        for cname in clips:
            c = CLIPS[cname]
            for i in range(c["n"]):
                P = c["fn"](view, i, c["n"], rig)
                cv, anc = render(rig, P)
                frames[(cname, view, i)] = paint(down_to_cell(cv), rig, anc)
        print("rendered", view, flush=True)
    return frames


def finish(frames, pal):
    return {key: px.outline(img, pal) for key, img in frames.items()}


def to_u8(img):
    return (np.clip(img, 0, 1) * 255).round().astype(np.uint8)


def write_palette(pal):
    PAL.parent.mkdir(parents=True, exist_ok=True)
    inks = pal * 0.32 + px.OUTLINE_TINT / 255.0 * 0.68
    PAL.write_text(json.dumps({
        "rgb": (pal * 255).round().astype(int).tolist(),
        "inks": (inks * 255).round().astype(int).tolist(),
        "ramps": RAMPS,
        "note": "An hand-picked material ramps (dark->light); inks = umber-shifted copies used by the selective outline"}))


def write_sheets(done, pal):
    OUT.mkdir(parents=True, exist_ok=True)
    for cname, c in CLIPS.items():
        if not any(k[0] == cname for k in done):
            continue
        n = c["n"]
        sheet = Image.new("RGBA", (CELL[0] * n, CELL[1] * 4), (0, 0, 0, 0))
        for r, d in enumerate(DIRS):
            for i in range(n):
                sheet.paste(Image.fromarray(to_u8(done[(cname, d, i)]), "RGBA"), (i * CELL[0], r * CELL[1]))
        sheet.save(OUT / f"an_{cname}.png")
        meta = {"source": "tools/pixelart/an_puppet.py (label-map puppet from ArtSource/Characters/An turnaround)",
                "cell": list(CELL), "rows": DIRS, "frames_per_row": n,
                "pivot_px_from_bottom_left": list(PIVOT), "fps": c["fps"], "colors": int(len(pal))}
        (OUT / f"an_{cname}.json").write_text(json.dumps(meta, indent=2))
        print("SHEET", cname, sheet.size, flush=True)


def previews(done):
    x = 4
    blocks = []
    for cname, c in CLIPS.items():
        if not any(k[0] == cname for k in done):
            continue
        n = c["n"]
        im = Image.new("RGBA", (CELL[0] * n * x + 8, CELL[1] * 4 * x), GOLD)
        for r, d in enumerate(DIRS):
            for i in range(n):
                f = Image.fromarray(to_u8(done[(cname, d, i)]), "RGBA").resize((CELL[0] * x, CELL[1] * x), Image.NEAREST)
                im.alpha_composite(f, (i * CELL[0] * x, r * CELL[1] * x))
        im.convert("RGB").save(CUT / f"preview_{cname}.png")
        blocks.append(im)
    if blocks:
        W = max(b.width for b in blocks)
        H = sum(b.height + 8 for b in blocks)
        big = Image.new("RGB", (W, H), (60, 60, 60))
        y = 0
        for b in blocks:
            big.paste(b.convert("RGB"), (0, y))
            y += b.height + 8
        big.save(CUT / "preview_x4.png")
    for cname in ("walk", "run"):
        if not any(k[0] == cname for k in done):
            continue
        c = CLIPS[cname]
        gs = 3
        frames = []
        for i in range(c["n"]):
            im = Image.new("RGBA", (CELL[0] * 4 * gs, CELL[1] * gs), GOLD)
            for r, d in enumerate(DIRS):
                f = Image.fromarray(to_u8(done[(cname, d, i)]), "RGBA").resize((CELL[0] * gs, CELL[1] * gs), Image.NEAREST)
                im.alpha_composite(f, (r * CELL[0] * gs, 0))
            frames.append(im.convert("RGB"))
        frames[0].save(CUT / f"{cname}_preview.gif", save_all=True, append_images=frames[1:], duration=int(1000 / c["fps"]), loop=0)
        if cname == "walk":
            frames[0].save(CUT / "walk_preview.gif", save_all=True, append_images=frames[1:], duration=int(1000 / c["fps"]), loop=0)
    # v4 vs Khoa (style_test2): per direction Khoa f0 | An idle | An walk 1 | An walk 5, 4x
    ksh = Image.open(ROOT / ".agent_scratch/pixel-pipeline/style_test2/sheet.png").convert("RGBA")
    kj = json.loads((ROOT / ".agent_scratch/pixel-pipeline/style_test2/sheet.json").read_text())
    kw, kh_ = kj["cell"]
    g = 4
    if all(any(k[0] == c for k in done) for c in ("idle", "walk")):
        cw = max(kw, CELL[0])
        hh = max(kh_, CELL[1])
        sc = Image.new("RGBA", (cw * 4 * 4 * g, hh * g), GOLD)
        for r, d in enumerate(DIRS):
            kf = ksh.crop((0, r * kh_, kw, (r + 1) * kh_)).resize((kw * g, kh_ * g), Image.NEAREST)
            xo = r * cw * 4 * g
            sc.alpha_composite(kf, (xo, (hh - kh_) * g))
            for j, (cl, fr) in enumerate((("idle", 0), ("walk", 1), ("walk", 5))):
                af = Image.fromarray(to_u8(done[(cl, d, fr)]), "RGBA").resize((CELL[0] * g, CELL[1] * g), Image.NEAREST)
                sc.alpha_composite(af, (xo + (j + 1) * cw * g, (hh - CELL[1]) * g))
        sc.convert("RGB").save(CUT / "v4_vs_khoa.png")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--clips", default=",".join(CLIPS))
    ap.add_argument("--no-write", action="store_true")
    a = ap.parse_args()
    clips = [c for c in a.clips.split(",") if c]
    pal = palette_rgb()
    write_palette(pal)
    frames = build_frames(clips)
    done = finish(frames, pal)
    if not a.no_write:
        write_sheets(done, pal)
    previews(done)


if __name__ == "__main__":
    main()
