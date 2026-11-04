"""Compose the three non-farm Pig Tycoon maps (MarketTown, Hub_Village, Forest_Primeval).

  python tools/pixelart/compose_maps_other.py            # all three
  python tools/pixelart/compose_maps_other.py town       # one of: town village forest

Reuses the generic engine of compose_map.py (imported, never edited): MapBuilder, dual-grid compositor, decor
libraries, y-sorted renderer. Same JSON schema as farm_layout.json. Deterministic (fixed seeds). numpy + Pillow.
Buildings whose sheet is not rendered yet (town_market_hall, village_sanctum, forest_relic) are drawn as magenta
placeholder boxes in the preview; re-run this script once the sheets exist (footprints are read from sheet size).
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import compose_map as cm  # noqa: E402
from compose_map import (MapBuilder, catmull, chamfer, fbm, flat, place, poisson, poly_dist, T)  # noqa: E402

OUT = cm.OUT_DIR

# ground footprint (w, h) in tiles for building sheets; None = no collider
FOOT = {
    "approved_main_house": (8.4, 3.6), "approved_research_lab": (7.2, 3.2),
    "bldg_market_counter": (5.4, 2.0), "bldg_night_stall": (3.8, 1.8),
    "prop_capacity_board": (1.0, 0.5), "prop_water_basin": (2.2, 0.8),
    "fence_gate_arch": None, "forest_relic": None,
}
RELIC_DEFAULT_CELL = (8.0, 8.0)


ARGON_FOOT = {"argon_village_tree_banyan": (1.6, 0.9), "argon_village_tree_tamarind": (0.9, 0.6),
              "argon_town_square_fountain": (3.6, 1.8), "argon_village_gieng_chung": (1.8, 1.0),
              "argon_village_bia_bo_ria": (1.6, 0.5), "argon_village_xe_om_stop": (2.6, 1.0),
              "argon_vdecor_buffalo_shed": (4.6, 2.2), "argon_vdecor_rice_store": (3.0, 2.2),
              "argon_town_chapel_bell_tower": (3.0, 2.6), "argon_town_quarantine_post": (4.6, 2.2)}


def foot_of(mb, name):
    if name in ARGON_FOOT:
        return ARGON_FOOT[name]
    if name in FOOT:
        return FOOT[name]
    m = mb.bld.meta(name) if mb.bld.has(name) else None
    cw = (m["cell"][0] / T) if m else {"town_market_hall": 14.0, "village_sanctum": 11.0}.get(name, 8.0)
    return (round(cw * 0.62, 1), 3.6 if name == "town_market_hall" else 3.2)


def bld(mb, name, x, y, row, role_id, kind="building", interact=None, foot="auto"):
    f = foot_of(mb, name) if foot == "auto" else foot
    return cm.add_building(mb, name, x, y, row, role_id, kind, interact, f)


def gate_arch(mb, x, y, row):
    cm.add_fence_piece(mb, "fence_gate_arch", x, y, row, "gate", "gate_arch")


# --------------------------------------------------------------------------- terrain helpers
def rrect(mb, x0, y0, x1, y1, cut=2.0):
    cx, cy = mb.cx, mb.cy
    m = (cx >= x0) & (cx <= x1) & (cy >= y0) & (cy <= y1)
    # chamfer the corners
    dx = np.minimum(cx - x0, x1 - cx)
    dy = np.minimum(cy - y0, y1 - cy)
    return m & (dx + dy >= cut)


def wob(mb, seed, amp=0.5, scale=5):
    return (fbm(mb.W + 1, mb.H + 1, scale, seed) - 0.5) * 2 * amp


def finish_keep(mb):
    tm = mb.tile_mat()
    mb.keep |= (tm == "p") | (tm == "s")
    mb.blocked |= (tm == "w")
    return tm


def tm_at(tm, x, y):
    H, W = tm.shape
    return tm[min(max(int(y), 0), H - 1), min(max(int(x), 0), W - 1)]


def decor_pass(mb, tm, rng, *, region, min_d, kinds, bounds=None, tries=30000, mats="m"):
    """Scatter clusters of small decor on tiles of `mats`; kinds = list of cluster names."""
    W, H = mb.W, mb.H
    bounds = bounds or (0, 0, W, H)
    ok = lambda x, y: tm_at(tm, x, y) in mats and region(x, y)
    for x, y in poisson(rng, ok, bounds, min_d, tries):
        cluster(mb, tm, rng, x, y, str(rng.choice(kinds)))


def cluster(mb, tm, rng, x, y, kind):
    def g(sx, sy):
        return x + rng.normal(0, sx), y + rng.normal(0, sy)

    def fl(fam, anim=True):
        px, py = g(1.7, 1.1)
        if tm_at(tm, px, py) in "mf":
            flat(mb, fam, px, py, anim=anim)
    if kind == "bush":
        for _ in range(rng.integers(1, 3)):
            px, py = g(1.2, 0.8)
            if tm_at(tm, px, py) == "m":
                place(mb, str(rng.choice(["shrub_round", "shrub_dark"])), px, py)
        for _ in range(rng.integers(3, 7)):
            fl("flame_flower")
        for _ in range(rng.integers(4, 9)):
            fl(str(rng.choice(["flower_purple", "flower_white", "grass_tuft_24x20", "grass_tuft_16x16"])))
    elif kind == "rocks":
        px, py = g(0.5, 0.5)
        place(mb, str(rng.choice(["boulder_l", "boulder_m"])), px, py)
        for _ in range(rng.integers(1, 3)):
            px, py = g(1.5, 1.0)
            place(mb, str(rng.choice(["boulder_m", "boulder_s", "ruin_cairn"])), px, py)
        for _ in range(rng.integers(3, 7)):
            fl(str(rng.choice(["flame_flower", "grass_tuft_24x20", "flower_white"])))
    elif kind == "grove":
        for _ in range(rng.integers(3, 6)):
            px, py = g(1.6, 1.2)
            if tm_at(tm, px, py) in "mf":
                place(mb, str(rng.choice(["conifer_tall", "conifer_small", "conifer_small"])), px, py)
        for _ in range(rng.integers(2, 5)):
            fl(str(rng.choice(["flame_flower", "flower_purple", "grass_tuft_16x16"])))
    elif kind == "meadow":
        for _ in range(rng.integers(3, 7)):
            fl(str(rng.choice(["grass_tuft_24x20", "grass_tuft_16x16", "flame_flower"])))
        for _ in range(rng.integers(0, 3)):
            fl(str(rng.choice(["flower_purple", "flower_white"])), anim=False)
    elif kind == "flame":
        for _ in range(rng.integers(6, 12)):
            fl("flame_flower")
        for _ in range(rng.integers(2, 5)):
            fl("grass_tuft_24x20")


def verges(mb, tm, rng, dpath, lo=0.9, hi=2.6, min_d=2.2):
    ok = lambda x, y: tm_at(tm, x, y) == "m" and lo < dpath[min(int(y), mb.H - 1), min(int(x), mb.W - 1)] < hi
    for x, y in poisson(rng, ok, (0, 0, mb.W, mb.H), min_d, 30000):
        flat(mb, str(rng.choice(["grass_tuft_24x20", "flame_flower", "flower_white", "flower_purple", "grass_tuft_16x16"])),
             x, y, anim=True)


def wood_edge(mb, tm, rng, step=1.5, keep_out=None, p_skip=0.05, tall_bias=0.5, giant_p=0.16):
    """Jittered-grid conifers on forest terrain (canopy layer, collider on the trunk)."""
    W, H = mb.W, mb.H
    for gy in np.arange(1.2, H - 0.8, step):
        for gx in np.arange(1.2, W - 0.8, step):
            x = gx + rng.uniform(-0.6, 0.6) + (0.7 if int(gy / step) % 2 else 0)
            y = gy + rng.uniform(-0.5, 0.5)
            if not (0 < x < W and 0 < y < H) or tm_at(tm, x, y) != "f":
                continue
            if keep_out and keep_out(x, y):
                continue
            if rng.random() < p_skip:
                continue
            if rng.random() < 0.10:
                place(mb, "shrub_dark", x, y, shadow=False)
            else:
                k = rng.random()
                if k < giant_p:
                    fam = "conifer_giant"
                elif k < giant_p + 0.22:
                    fam = "conifer_mid"
                else:
                    fam = "conifer_tall" if rng.random() < tall_bias else "conifer_small"
                place(mb, fam, x, y, shadow=rng.random() < 0.2)


def rim_boulders(mb, tm, rng, depth=1.2, step=1.6, skip=()):
    W, H = mb.W, mb.H
    for side in range(4):
        n = int((W if side < 2 else H) / step)
        for i in range(n):
            t = 0.8 + i * step + rng.uniform(-0.2, 0.2)
            x, y = {0: (t, depth), 1: (t, H - depth), 2: (depth, t), 3: (W - depth, t)}[side]
            if any((x - px) ** 2 + (y - py) ** 2 < 4.5 ** 2 for px, py in skip) or tm_at(tm, x, y) in "wps":
                continue
            place(mb, str(rng.choice(["boulder_l", "boulder_m"])), x, y, shadow=False, check=False)


def reachable(mb, spawn, targets):
    from collections import deque
    W, H = mb.W, mb.H
    free = ~mb.blocked
    seen = np.zeros_like(free)
    sx, sy = int(spawn[0]), int(spawn[1])
    q = deque([(sx, sy)])
    seen[sy, sx] = True
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < W and 0 <= ny < H and free[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                q.append((nx, ny))
    return {k: bool(seen[min(int(y), H - 1), min(int(x), W - 1)]) for k, (x, y) in targets.items()}, int(seen.sum()), int(free.sum())


# --------------------------------------------------------------------------- rich passes (town / village / forest)
def oak_accents(mb, tm, rng, ok, min_d=7.0, p_oak=0.55):
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) == "m" and ok(x, y), (0, 0, mb.W, mb.H), min_d, 12000):
        place(mb, "oak_gold" if rng.random() < p_oak else "tree_amber", x, y, shadow=True)


def meadow_life(mb, tm, rng, min_d=5.0, margin=2):
    """Tall golden grass, wide clumps and wildflower patches over open meadow."""
    W, H = mb.W, mb.H
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) == "m" and margin < min(x, y, W - x, H - y), (0, 0, W, H), min_d, 20000):
        k = rng.random()
        cm.deco(mb, "grass_tall" if k < 0.42 else ("grass_wide" if k < 0.62 else "wildflowers"), x, y)


# --------------------------------------------------------------------------- ART MAPPING (single place to swap placeholders)
# key -> (source, placeholder). source = building sheet name in Assets/Art/PixelMyth/Buildings/, or
# "decor:<family>:<file>" for a Decor sprite. When the artist delivers real sheets, ONLY change this dict.
ART = {
    # ---- market town (old-European)
    "market_hall": ("town_market_hall", False),
    "stall_day": ("bldg_market_counter", False),
    "stall_night": ("bldg_night_stall", False),
    "board": ("prop_capacity_board", False),
    "town_house_a": ("argon_town_town_house_a", False),
    "town_house_b": ("argon_town_town_house_b", False),
    "town_house_c": ("argon_town_town_house_c", False),
    "town_house_d": ("argon_town_town_house_d", False),
    "town_house_e": ("argon_town_town_house_e", False),
    "town_shop_small": ("argon_town_town_house_f", False),
    "quarantine_post": ("argon_town_quarantine_post", False),
    "appraisal_house": ("argon_town_appraisal_house", False),
    "supply_store": ("argon_town_supply_store", False),
    "recruit_hall": ("argon_town_recruit_hall", False),
    "mercenary_stable": ("argon_town_mercenary_stable", False),
    "auction_yard": ("argon_town_auction_yard", False),
    "post_office": ("argon_town_post_office", False),
    "chapel": ("argon_town_chapel_bell_tower", False),          # art decision (no gameplay), not a GDD requirement
    "bell_tower": ("bldg_observatory", False),    # art decision (no gameplay)
    # ---- Bo Ria village (Vietnamese rural)
    "cong_thanh": ("argon_village_cong_thanh", False),
    "trong_dong_dai": ("argon_village_trong_dong_dai", False),
    "mieu_co": ("argon_village_mieu_co", False),
    "bia_rua": ("argon_village_bia_rua", False),
    "dinh_lang": ("argon_village_dinh_lang", False),
    "bia_sign": ("argon_village_bia_bo_ria", False),
    "xe_om_stop": ("argon_village_xe_om_stop", False),
    "quan_ba_sau": ("argon_village_quan_ba_sau", False),
    "nha_van_hoa": ("argon_village_nha_van_hoa", False),
    "gieng_chung": ("argon_village_gieng_chung", False),
    "nha_man": ("argon_village_village_house_a", False),
    "nha_tam_va": ("argon_village_village_house_b", False),
    "nha_dung": ("argon_village_village_house_c", False),
    "nha_cuc": ("argon_village_village_house_d", False),
    "nha_le": ("argon_village_village_house_e", False),
    "nha_anh_ca": ("argon_village_village_house_f", False),
    "nha_ong_ton": ("argon_village_village_house_g", False),
    "notice_board": ("prop_capacity_board", False),
    "kho_lua": ("argon_vdecor_rice_store", False),
    "square_fountain": ("argon_town_square_fountain", False),
    "tree_banyan": ("argon_village_tree_banyan", False),
    "tree_tamarind": ("argon_village_tree_tamarind", False),
    "chuong_trau": ("argon_vdecor_buffalo_shed", False),
}
DOOR_PAD = (1.5, 2.6, 0.2)     # half-width, below, above the door anchor: kept free of decor (>= 1 m clearance)


def _sheet_bbox(mb, name, row):
    key = ("bb", name, row)
    if key not in mb.bld.cache:
        crop, piv = mb.bld.row_img(name, row)
        if crop is None:
            mb.bld.cache[key] = None
        else:
            bb = crop.getbbox() or (0, 0, crop.width, crop.height)
            mb.bld.cache[key] = (bb, piv)
    return mb.bld.cache[key]


def fp_rect(mb, name, row, x, y):
    """Footprint = sprite width x ~0.85 of its height above the base (the area a tree must never stand in)."""
    v = _sheet_bbox(mb, name, row)
    if v is None:
        return (x - 4.0, y - 0.3, x + 4.0, y + 4.0)
    bb, piv = v
    top = (piv[1] - bb[1]) / T
    return (x + (bb[0] - piv[0]) / T, y - 0.3, x + (bb[2] - piv[0]) / T, y + 0.85 * max(top, 0.5))


def row_extent(mb, name, row="down"):
    v = _sheet_bbox(mb, name, row)
    if v is None:
        return -4.0, 4.0
    bb, piv = v
    return (bb[0] - piv[0]) / T, (bb[2] - piv[0]) / T


def _rects_hit(a, b, m=0.0):
    return a[0] < b[2] + m and b[0] < a[2] + m and a[1] < b[3] + m and b[1] < a[3] + m


def init_plan(mb, apron="s"):
    mb.fps, mb.problems, mb.npcs, mb.apron = [], [], [], apron


def put(mb, key, x, y, role_id, kind="building", interact=None, row="down", apron=True):
    """Place an ART-mapped building/prop, register its footprint, block it for decor, keep the door clear."""
    src, ph = ART[key]
    if src.startswith("decor:"):
        _, fam, f = src.split(":")
        cm.deco(mb, fam, x, y, check=False, file=f, role=f"prop:{role_id}")
        r = (x - 0.7, y - 0.2, x + 0.7, y + 0.8)
        mb.fps.append({"role": role_id, "key": key, "rect": r, "door": (x, y), "decor": True})
        if ph:
            mb.placeholders.append(f"{key}={src}")
        return r
    foot = foot_of(mb, src)
    if kind == "prop" and foot is None:
        foot = (0.8, 0.4)
    bld(mb, src, x, y, row, role_id, kind, interact, foot=foot)
    r = fp_rect(mb, src, row, x, y)
    for f in mb.fps:
        if _rects_hit(r, f["rect"], 0.25):
            mb.problems.append(f"building overlap {role_id} x {f['role']}")
    mb.fps.append({"role": role_id, "key": key, "rect": r, "door": (x, y), "decor": kind == "prop"})
    mb.block_rect(*r)
    if ph:
        mb.placeholders.append(f"{key}={src}")
    if apron and kind == "building":
        w = max(2.4, min(4.4, (r[2] - r[0]) * 0.4))
        mb.paint(cm._stone_blob(mb, x, y - 1.3, w, 1.5, int(x * 7 + y * 3), 0.4), mb.apron)
        mb.keep[max(int(y - DOOR_PAD[1]), 0):int(y + DOOR_PAD[2]) + 1, max(int(x - DOOR_PAD[0]), 0):int(x + DOOR_PAD[0]) + 1] = True
    # no forest growing through / right beside the building
    m = (mb.cx >= r[0] - 1.2) & (mb.cx <= r[2] + 1.2) & (mb.cy >= r[1] - 3.2) & (mb.cy <= r[3] + 1.2) & (mb.c == "f")
    mb.c[m] = "m"
    return r


def lay_row(mb, y, x, items, jit=(0.0, 0.5, -0.3, 0.4, -0.5, 0.2, 0.0)):
    """items: (art_key, role_id, gap_after, interact|None). Left to right from x. Returns the x where the row ends."""
    for i, (key, rid, gap, inter) in enumerate(items):
        l, r = row_extent(mb, ART[key][0])
        ax = x - l
        put(mb, key, ax, y + jit[i % len(jit)], rid, interact=inter)
        x = ax + r + gap
    return x


def in_fp(mb, x, y, m=0.0):
    for f in mb.fps:
        r = f["rect"]
        if r[0] - m <= x <= r[2] + m and r[1] - m <= y <= r[3] + m:
            return True
        dx, dy = f["door"]
        if not f["decor"] and abs(x - dx) <= DOOR_PAD[0] + 1.0 and -DOOR_PAD[1] - 1.0 <= y - dy <= DOOR_PAD[2] + 1.0:
            return True
    return False


def D(mb, fam, x, y, margin=0.0, water=False, **k):
    """Overlap-safe decor placement (never inside a building footprint / door pad, never stacked on a collider)."""
    if not (0.4 < x < mb.W - 0.4 and 0.4 < y < mb.H - 0.4) or in_fp(mb, x, y, margin):
        return False
    layer, col, _ = cm.COL[fam]
    xi, yi = int(x), int(y)
    if col is not None and mb.blocked[yi, xi]:
        return False
    if not water and col is not None and (mb.c[yi:yi + 2, xi:xi + 2] == "w").any():
        return False
    k.setdefault("check", False)
    return cm.deco(mb, fam, x, y, **k)


def walk_snap(mb, x, y, rmax=6):
    """Nearest walkable tile centre to (x,y) (not blocked, not inside a footprint)."""
    best = None
    for dy in range(-rmax, rmax + 1):
        for dx in range(-rmax, rmax + 1):
            xi, yi = int(x) + dx, int(y) + dy
            if 0 <= xi < mb.W and 0 <= yi < mb.H and not mb.blocked[yi, xi] and not in_fp(mb, xi + .5, yi + .5):
                d = math.hypot(xi + .5 - x, yi + .5 - y)
                if best is None or d < best[0]:
                    best = (d, xi + .5, yi + .5)
    return (round(best[1], 2), round(best[2], 2)) if best else (x, y)


def add_npc(mb, nid, name, near, home, route_pts, src, gdd=None):
    """near / route_pts are wished positions; all get snapped to walkable tiles after decor is final."""
    mb.npcs.append({"id": nid, "name": name, "_near": near, "home": home, "_route": route_pts, "greeting_src": src,
                    **({"gdd_src": gdd} if gdd else {})})


def finalize_npcs(mb):
    out = []
    for n in mb.npcs:
        x, y = walk_snap(mb, *n.pop("_near"))
        route = [list(walk_snap(mb, *p)) for p in n.pop("_route")]
        n["x"], n["y"], n["route"] = x, y, route
        out.append(n)
    mb.npcs = out


def door_of(mb, role_id):
    for f in mb.fps:
        if f["role"] == role_id:
            return f["door"]
    raise KeyError(role_id)


# --------------------------------------------------------------------------- validation
def _decor_size(mb, o):
    m = mb.decor.m.get(o["sprite"].split("/")[-1])
    return (m["size"][0] / T, m["size"][1] / T) if m else None


def violations(mb):
    """Every non-ground, non-building object vs every building footprint (+ 1 m door pad). Returns list of indices."""
    bad = []
    for i, o in enumerate(mb.objects):
        role = o["role"]
        if o["layer"] == "ground" or role.startswith(("building:", "prop:", "fence", "gate", "attached")):
            continue
        x, y = o["x"], o["y"]
        sz = _decor_size(mb, o)
        lat = 0.3 + (0.35 * sz[0] if (sz and o["layer"] == "canopy") else 0.0)
        for f in mb.fps:
            r = f["rect"]
            if r[0] - lat <= x <= r[2] + lat and r[1] <= y <= r[3] + (0.5 if o["layer"] == "canopy" else 0.0):
                bad.append(i)
                break
            dx, dy = f["door"]
            if not f["decor"] and abs(x - dx) <= DOOR_PAD[0] + 0.5 and -DOOR_PAD[1] - 0.2 <= y - dy <= DOOR_PAD[2] + 1.0:
                bad.append(i)
                break
    return bad


def purge(mb):
    bad = set(violations(mb))
    drop = set(bad)
    for i in bad:   # remove the shadow object that put_decor adds right before the sprite
        if i > 0 and "shadow_ellipse" in mb.objects[i - 1]["sprite"] and mb.objects[i - 1]["x"] == mb.objects[i]["x"]:
            drop.add(i - 1)
    mb.objects = [o for i, o in enumerate(mb.objects) if i not in drop]
    return len(bad)


def building_overlaps(mb):
    n = 0
    for i, a in enumerate(mb.fps):
        for b in mb.fps[i + 1:]:
            if not a["decor"] and not b["decor"] and _rects_hit(a["rect"], b["rect"]):
                n += 1
    return n


def empty_share(mb, cell=8.0):
    """8x8 m cells that contain at least one walkable tile; share with no non-ground object inside."""
    free = ~mb.blocked
    cnt = tot = 0
    for cy in np.arange(0, mb.H, cell):
        for cx in np.arange(0, mb.W, cell):
            sub = free[int(cy):int(min(cy + cell, mb.H)), int(cx):int(min(cx + cell, mb.W))]
            if sub.size == 0 or sub.mean() < 0.15:
                continue
            tot += 1
            has = any(o["layer"] != "ground" and cx <= o["x"] < cx + cell and cy <= o["y"] < cy + cell for o in mb.objects)
            cnt += 0 if has else 1
    return round(100.0 * cnt / max(tot, 1), 1), cnt, tot


def lamps_line(mb, p0, p1, step, off=0.0, fam="lantern_post", margin=0.6):
    n = max(int(math.hypot(p1[0] - p0[0], p1[1] - p0[1]) / step), 1)
    dx, dy = (p1[0] - p0[0]) / n, (p1[1] - p0[1]) / n
    for i in range(n + 1):
        D(mb, fam, p0[0] + dx * i, p0[1] + dy * i + off, margin=margin, allow_keep=True)


def side_clutter(mb, rng, roles, flower_boxes=True):
    """Barrels / crates / baskets beside buildings, flower boxes by the door (marked 'attached')."""
    for b in mb.fps:
        if b["decor"] or not (b["role"] in roles or roles == "all"):
            continue
        r = b["rect"]
        for sx in (r[0] - 0.9, r[2] + 0.9):
            if rng.random() < 0.7:
                fam = str(rng.choice(["barrel", "crate", "crate_stack", "sack_pile", "basket", "woodpile"]))
                D(mb, fam, sx, r[1] + 0.6 + rng.uniform(0, 0.9), allow_keep=True)
        if flower_boxes:
            for sx in (b["door"][0] - 2.7, b["door"][0] + 2.7):
                if r[0] < sx < r[2]:
                    D(mb, "flower_box", sx, b["door"][1] - 0.6, role="attached", allow_keep=True)


def forest_rich(mb, tm, rng, relic_at, CX, CY, stream):
    D = lambda fam, x, y, **k: cm.deco(mb, fam, x, y, **k)
    W, H = mb.W, mb.H
    # mist-lit clearing around the relic gate
    for x, y in ((CX - 6.5, CY - 3.2), (CX + 6.5, CY - 2.4), (CX, CY + 3.2), (CX - 4.0, CY + 6.0), (CX + 4.4, CY + 5.2), (CX, CY - 6.4),
                 (relic_at[0] - 4.4, relic_at[1] - 2.4), (relic_at[0] + 4.4, relic_at[1] - 2.0), (CX - 9.5, CY + 1.0), (CX + 9.5, CY + 1.4)):
        D("mist", x, y, check=False)
    # luminous flowers and mushrooms ring the glade, crowd the relic
    for a in np.linspace(0, 6.283, 40, endpoint=False):
        px, py = CX + math.cos(a) * (10.2 + rng.uniform(-0.6, 0.9)), CY + math.sin(a) * (7.8 + rng.uniform(-0.5, 0.7))
        D("glow_flower" if rng.random() < 0.65 else "glow_mushroom", px, py, check=False)
    for _ in range(26):
        a, r = rng.uniform(0, 6.283), rng.uniform(2.2, 7.0)
        D("glow_flower" if rng.random() < 0.6 else "glow_mushroom", relic_at[0] + math.cos(a) * r * 1.3, relic_at[1] - 1.5 + math.sin(a) * r * 0.6)
    D("sun_stele", relic_at[0] - 3.4, relic_at[1] - 0.2, check=False, file="sun_stele_a.png")
    D("sun_stele", relic_at[0] + 3.4, relic_at[1] - 0.2, check=False, file="sun_stele_c.png")
    D("statue_weathered", relic_at[0] - 7.4, relic_at[1] - 3.4, check=False, file="statue_weathered_b.png")
    D("statue_weathered", relic_at[0] + 7.4, relic_at[1] - 3.4, check=False, file="statue_weathered_a.png")
    for x, y in ((CX - 2.8, CY - 4.0), (CX + 2.8, CY - 4.0)):
        D("torch_post", x, y, check=False, file="torch_post_b_f0.png")
    # forest floor: ferns, glow mushrooms, fallen logs and stumps, mossy sun-steles
    ok = lambda x, y: tm_at(tm, x, y) == "f" and not mb.keep[min(int(y), H - 1), min(int(x), W - 1)]
    for x, y in poisson(rng, ok, (0, 0, W, H), 3.3, 12000):
        k = rng.random()
        if k < 0.50:
            D("fern", x, y)
        elif k < 0.62:
            D("glow_mushroom", x, y)
        elif k < 0.74:
            D("log_fallen", x, y)
        elif k < 0.86:
            D("stump", x, y)
        elif k < 0.90:
            D("sun_stele", x, y)
        else:
            D("grass_tall", x, y)
    # stream: reeds and lily pads
    pts = catmull(stream, 10)
    for t in np.linspace(0, 1, 70):
        a = pts[min(int(t * (len(pts) - 1)), len(pts) - 1)]
        D("reeds", a[0] + rng.choice([-1, 1]) * rng.uniform(1.6, 2.4), a[1] + rng.uniform(-0.5, 0.5), check=False)
        if rng.random() < 0.3:
            D("lilypad", a[0] + rng.uniform(-0.8, 0.8), a[1], check=False)
    for x, y, kind in ((12, 48, "arch"), (49, 52, "mini"), (51, 17, "court"), (10, 12, "colonnade")):
        if kind in ("court", "arch"):
            D("statue_weathered" if kind == "court" else "sun_stele", x + 0.6, y - 3.0, check=True)


# --------------------------------------------------------------------------- MARKET TOWN
# Stardew-style spacing: 130 x 110 m, ~27 buildings, wide streets, a big square, yards and gardens (1 tile = 1 m = 32 px).
# y grows to the NORTH (the image is drawn with y flipped): Farm portal at the south edge, Hub_Village portal at the west edge.
TOWN_W, TOWN_H = 130, 110
TOWN_AVE_X = 65.0
TOWN_CANAL = [(-2, 12.6), (18, 13.6), (40, 11.8), (65, 12.6), (90, 13.8), (110, 12.2), (132, 12.9)]
TOWN_SQUARE = (45.0, 18.0, 85.0, 47.0)
TOWN_SQUARE_CENTRE = (65.0, 32.5)


def _canal_y(x):
    p = np.array(TOWN_CANAL)
    return float(np.interp(x, p[:, 0], p[:, 1]))


def t_ok(mb, x, y, road=False):
    """Dressing guard: inside the map, not in a building yard, optionally off roads."""
    xi, yi = int(x), int(y)
    if not (0 <= xi < mb.W and 0 <= yi < mb.H):
        return False
    if mb.yard[yi, xi]:
        return False
    if not road and mb.keep[yi, xi]:
        return False
    return True


def TD(mb, fam, x, y, margin=0.0, road=False, **k):
    """D() + yard / road guard. Never places trees, shrubs, or blocking props onto dirt roads or main street centerlines."""
    xi, yi = int(x), int(y)
    if not (0 <= xi < mb.W and 0 <= yi < mb.H):
        return False
    # Never place trees or shrubs onto any paved/road tile ('p' or 's')
    if fam in ("shrub_round", "shrub_dark", "tree_amber", "oak_gold", "conifer_tall", "conifer_small", "conifer_mid", "conifer_giant"):
        road = False
    # Never place props onto dirt roads ('p') or main street centerlines
    if (mb.c[yi:yi + 2, xi:xi + 2] == "p").any():
        return False
    if road and (abs(x - TOWN_AVE_X) < 2.2 or abs(y - 38.0) < 2.0 or abs(y - 53.0) < 2.0):
        return False
    if not t_ok(mb, x, y, road):
        return False
    return D(mb, fam, x, y, margin, **k)


def TF(mb, rng, x, y, fams=("flower_white", "flame_flower", "flower_purple", "grass_tuft_16x16"), road=False):
    """Ground-layer flower / tuft (never in a footprint or yard, never on water)."""
    if not t_ok(mb, x, y, road) or in_fp(mb, x, y) or (mb.c[int(y):int(y) + 2, int(x):int(x) + 2] == "w").any():
        return False
    flat(mb, str(rng.choice(fams)), x, y, anim=True, check=False)
    return True


def t_bed(mb, rng, x, y, rx, ry, shrubs=1):
    """Oval flower bed (ground flowers + a shrub or two)."""
    n = int(rx * ry * 3.2) + 3
    for _ in range(n):
        a, r = rng.uniform(0, 6.283), math.sqrt(rng.uniform(0, 1))
        TF(mb, rng, x + math.cos(a) * rx * r, y + math.sin(a) * ry * r,
           ("flower_white", "flame_flower", "flower_purple", "flame_flower", "wildflowers"))
    for _ in range(shrubs):
        TD(mb, str(rng.choice(["shrub_round", "shrub_round", "shrub_dark"])), x + rng.uniform(-rx, rx) * 0.5, y + rng.uniform(-ry, ry) * 0.4,
           margin=0.4)


def t_fence(mb, p0, p1, gap=None, post_every=2.0):
    """Decorative wooden fence from existing fence_wood_* sprites; non-blocking so lanes stay walkable. gap = (lo, hi) along the run."""
    horiz = abs(p1[1] - p0[1]) < abs(p1[0] - p0[0])
    a, b = (p0[0], p1[0]) if horiz else (p0[1], p1[1])
    fixed = p0[1] if horiz else p0[0]
    t = a
    while t < b - 0.01:
        if not (gap and gap[0] <= t < gap[1]):
            x, y = (t + 0.5, fixed) if horiz else (fixed, t + 1.0)
            if not in_fp(mb, x, y, 0.2):
                if horiz:
                    mb.put_decor(mb.d_sprite("fence_wood_rail_h"), x, y, "actors", None, "fence")
                else:
                    mb.put_decor("fence_wood_rail_v_a.png", x, y, "actors", None, "fence")
        t += 1.0
    t = a
    while t <= b + 0.01:
        if not (gap and gap[0] <= t <= gap[1]):
            x, y = (t, fixed) if horiz else (fixed, t)
            if not in_fp(mb, x, y, 0.2):
                mb.put_decor(mb.d_sprite("fence_wood_post"), x, y, "actors", None, "fence")
        t += post_every


def t_hedge(mb, rng, p0, p1, step=1.1, gaps=(), fam="shrub_dark"):
    n = max(int(math.hypot(p1[0] - p0[0], p1[1] - p0[1]) / step), 1)
    for i in range(n + 1):
        x = p0[0] + (p1[0] - p0[0]) * i / n
        y = p0[1] + (p1[1] - p0[1]) * i / n
        if any(g[0] <= x <= g[1] for g in gaps):
            continue
        TD(mb, fam if rng.random() < 0.8 else "shrub_round", x, y, margin=0.3, road=False)


def t_trees(mb, rng, rect, groups, per=(3, 5), spread=2.0, kinds=("tree_amber", "oak_gold", "conifer_tall"), min_d=11.0):
    """Tree groves: Poisson group centres, 3-5 trees each, coherent single-species stands per grove."""
    x0, y0, x1, y1 = rect
    cs = poisson(rng, lambda x, y: t_ok(mb, x, y), (x0, y0, x1, y1), min_d, 4000)
    for idx, (cx_, cy_) in enumerate(cs[:groups]):
        grove_fam = kinds[idx % len(kinds)]
        for _ in range(int(rng.integers(per[0], per[1] + 1))):
            TD(mb, grove_fam, cx_ + rng.normal(0, spread), cy_ + rng.normal(0, spread * 0.8), margin=2.4)


def t_scatter(mb, rng, rect, min_d, fams, margin=0.6, tries=5000):
    x0, y0, x1, y1 = rect
    for x, y in poisson(rng, lambda x, y: t_ok(mb, x, y), (x0, y0, x1, y1), min_d, tries):
        TD(mb, str(rng.choice(fams)), x, y, margin=margin)


def t_yard(mb, fp, depth=4.5):
    """Mark the forecourt in front of a door as yard: no dressing (flowers/props/trees) may land there."""
    dx, dy = fp["door"]
    r = fp["rect"]
    hw = max(1.8, min(3.2, (r[2] - r[0]) * 0.32))
    x0, x1 = int(max(dx - hw, 0)), int(min(dx + hw, mb.W - 1)) + 1
    y0, y1 = int(max(dy - depth, 0)), int(min(dy + 0.5, mb.H - 1)) + 1
    mb.yard[y0:y1, x0:x1] = True


def market_town():
    W, H = TOWN_W, TOWN_H
    mb = MapBuilder("MarketTown", "MarketTown", "Market_Town", W, H, seed=20261101)
    init_plan(mb, "s")
    mb.yard = np.zeros((H, W), bool)
    rng = np.random.default_rng(777)
    cx, cy = mb.cx, mb.cy
    AX = TOWN_AVE_X
    n_f = fbm(W + 1, H + 1, 9, 31, 3)
    edge = np.minimum.reduce([cx, cy, W - cx, H - cy])
    mb.paint((edge < 3.6 + 5 * (n_f - 0.4)) & (n_f > 0.42), "f")
    # ---- lanes (gold-dust path) first, then framed stone streets and garden parterres (road+stone <= 16%)
    for pts, wd, sd in (
            ([(5, 20.5), (44.0, 20.5)], 2.0, 1), ([(44.0, 20.5), (44.0, 38.0)], 2.0, 2),            # guild lane
            ([(22, 15.0), (22, 20.5)], 2.0, 3),                                                        # footbridge approach
            ([(42.5, 38.0), (42.5, 53.0)], 2.0, 4),                                                    # west connector
            ([(8, 63.5), (62, 63.5)], 2.0, 5), ([(50, 53.0), (50, 63.5)], 2.0, 6), ([(50, 63.5), (50, 81.0)], 2.0, 7),
            ([(10, 81.0), (60, 81.0)], 2.0, 8),                                                        # west residential lanes
            ([(68, 63.5), (124, 63.5)], 2.0, 9), ([(78, 53.0), (78, 63.5)], 2.0, 10),                # east residential lane
            ([(109, 53.0), (109, 85.0)], 2.0, 11)):                                                    # lane to the stable yard
        mb.stroke("p", pts, wd, 0.12, sd, smooth=False)
    for pts, wd, sd in (([(AX, -1), (AX, 92.0)], 3.4, 20),            # the great avenue: Farm gate -> square -> chapel
                        ([(-1, 38.0), (46, 38.0)], 3.2, 21),         # west street (Hub_Village portal)
                        ([(80, 33.0), (96, 33.0)], 3.0, 22),         # east street into the night market
                        ([(4, 53.0), (92, 53.0)], 3.2, 23)):         # north street
        mb.stroke("s", pts, wd, 0.10, sd, smooth=False)
    # Lush garden parterres ('g') flanking the central square, chapel avenue, and night market
    mb.paint(rrect(mb, 46.0, 18.0, 84.0, 46.0, 3.0), "g")
    mb.paint(rrect(mb, 92.0, 22.0, 123.0, 52.0, 2.5), "g")
    mb.paint(rrect(mb, 54.0, 56.0, 76.0, 94.0, 2.5), "g")
    # Framed stone walkways and fountain courtyard inside the square (instead of 1160 m2 solid stone)
    mb.paint(rrect(mb, 58.0, 20.0, 72.0, 31.5, 2.0), "s")             # central fountain court
    mb.paint(rrect(mb, 58.0, 36.0, 72.0, 42.0, 1.5), "s")             # market hall forecourt
    mb.paint(rrect(mb, 50.0, 19.0, 54.5, 39.0, 1.0), "s")             # west day-stall colonnade walk
    mb.paint(rrect(mb, 75.5, 19.0, 80.0, 39.0, 1.0), "s")             # east day-stall colonnade walk
    mb.paint(rrect(mb, 54.0, 24.0, 76.0, 27.5, 0.8), "s")             # cross promenade
    # Framed stone walkways for the night market & auction court (instead of 1122 m2 solid stone)
    mb.paint(rrect(mb, 96.0, 23.5, 118.0, 27.5, 1.2), "s")            # south night-stall walkway
    mb.paint(rrect(mb, 96.0, 35.0, 118.0, 39.0, 1.2), "s")            # north night-stall walkway
    mb.paint(rrect(mb, 104.5, 25.0, 109.5, 48.0, 1.0), "s")           # auction spine walkway
    mb.paint(rrect(mb, 60.0, 85.0, 70.0, 93.5, 2.0), "s")             # chapel forecourt
    mb.paint(rrect(mb, 103.0, 83.5, 115.0, 88.5, 1.2), "p")           # compact stable forecourt
    # ---- canal with two bridges and narrow stone quays
    mb.stroke("s", TOWN_CANAL, 5.2, 0.2, 5)
    mb.stroke("w", TOWN_CANAL, 3.6, 0.25, 6)
    mb.paint((cx > AX - 2.4) & (cx < AX + 2.4) & (cy > 8.2) & (cy < 17.2), "s")     # stone bridge on the avenue
    mb.paint((cx > 20.4) & (cx < 23.6) & (cy > 8.2) & (cy < 17.2), "s")             # west footbridge
    # ---- the market core: hall, 6 day stalls, 4 night stalls, board
    put(mb, "market_hall", AX, 41.0, "market_hall", interact={"function": "VaoNhaScene", "id": "hoi_cho"})
    day = [(51.5, 20.5), (51.5, 28.9), (51.5, 37.3), (78.5, 20.5), (78.5, 28.9), (78.5, 37.3)]
    for i, (x, y) in enumerate(day, 1):
        put(mb, "stall_day", x, y, f"stall_day_{i}", interact={"function": "GiaoDichDanLang", "id": f"cho_ngay_{i}"})
    night = [(99.0, 26.5), (115.0, 26.5), (99.0, 38.0), (115.0, 38.0)]
    for i, (x, y) in enumerate(night, 1):
        put(mb, "stall_night", x, y, f"stall_night_{i}", interact={"function": "GiaoDichDanLang", "id": f"cho_dem_{i}"})
    put(mb, "board", 65.0, 15.6, "board", "prop", {"function": "DocBangTinLore", "id": "bang_tin_cho"}, apron=False)
    # ---- the 7 GDD services, each standing alone with a forecourt
    put(mb, "quarantine_post", 14.0, 43.5, "quarantine_post", interact={"function": "GiaoDichDanLang", "id": "kiem_dich_sang_loc"})
    put(mb, "recruit_hall", 34.0, 44.0, "recruit_hall", interact={"function": "GiaoDichDanLang", "id": "tuyen_npc"})
    put(mb, "appraisal_house", 22.0, 25.0, "appraisal_house", interact={"function": "GiaoDichDanLang", "id": "giam_dinh"})
    put(mb, "supply_store", 39.0, 25.0, "supply_store", interact={"function": "GiaoDichDanLang", "id": "vat_tu"})
    put(mb, "post_office", 53.0, 58.5, "post_office", interact={"function": "DocBangTinLore", "id": "buu_dien"})
    put(mb, "auction_yard", 108.0, 48.0, "auction_yard", interact={"function": "GiaoDichDanLang", "id": "dau_gia_dem"})
    put(mb, "mercenary_stable", 109.0, 87.0, "mercenary_stable", interact={"function": "GiaoDichDanLang", "id": "thue_linh_doi_thu_cung"})
    # ---- chapel / bell tower closing the avenue
    put(mb, "chapel", AX, 94.0, "chapel")
    # ---- 8 townhouses in three groups along lanes
    houses = [("town_house_a", 14, 68), ("town_house_b", 27, 68), ("town_house_c", 40, 68),      # west lane group
              ("town_house_d", 22, 86), ("town_house_e", 38, 86),                               # north-west pair
              ("town_shop_small", 90, 68), ("town_house_a", 101, 68), ("town_house_b", 120, 68)]  # east lane group
    for i, (k, x, y) in enumerate(houses, 1):
        put(mb, k, x, y, f"house_{i}")
    mb.row_ends = ()
    for f in mb.fps:
        if not f["decor"]:
            t_yard(mb, f)
    cm.add_fence_piece(mb, "fence_gate_arch", AX, 1.8, "down", "gate", "gate_arch")
    cm.add_fence_piece(mb, "fence_gate_arch", 1.8, 38.0, "left", "gate", "gate_arch")
    mb.portals = [
        {"id": "MarketTown->Farm", "target": "Farm", "target_portal": "Farm->MarketTown", "x": AX, "y": 1.2},
        {"id": "MarketTown->Hub_Village", "target": "Hub_Village", "target_portal": "Hub_Village->MarketTown", "x": 1.2, "y": 38.0},
    ]
    mb.spawn = {"x": AX, "y": 6.5}
    mb.landmarks = {"square_centre": [TOWN_SQUARE_CENTRE[0], TOWN_SQUARE_CENTRE[1]]}
    mb.keep[int(TOWN_SQUARE_CENTRE[1]) - 1:int(TOWN_SQUARE_CENTRE[1]) + 2, int(TOWN_SQUARE_CENTRE[0]) - 1:int(TOWN_SQUARE_CENTRE[0]) + 2] = True
    mb.zones = [{"name": "square", "rect": [45, 18, 40, 29]}, {"name": "night_market", "rect": [91, 22, 34, 33]},
                {"name": "guild_quarter", "rect": [4, 16, 42, 34]}, {"name": "residential", "rect": [8, 60, 114, 34]},
                {"name": "canal", "rect": [0, 8, 130, 9]}, {"name": "park", "rect": [4, 1, 120, 8]},
                {"name": "chapel_green", "rect": [50, 56, 30, 38]}]
    mb.core_center = TOWN_SQUARE_CENTRE
    cm.worn_paths(mb, 811)
    cm.ground_variation(mb, 821, moist=0.18 * np.exp(-chamfer(mb.c == "w") / 8.0))
    tm = finish_keep(mb)
    dpath = chamfer(mb.keep)
    town_rich(mb, rng, tm, day, night, houses)
    # ---- fringe of nature (placement is overlap-safe through mb.blocked / keep / purge)
    wood_edge(mb, tm, rng, step=1.6, keep_out=lambda x, y: mb.keep[min(int(y), H - 1), min(int(x), W - 1)], p_skip=0.1)
    verges(mb, tm, rng, dpath, 0.9, 2.8, 2.6)
    decor_pass(mb, tm, rng, region=lambda x, y: t_ok(mb, x, y), min_d=4.2, kinds=["bush", "bush", "meadow", "rocks", "grove"], tries=40000)
    cm_fringe(mb, tm, rng)
    rim_boulders(mb, tm, rng, skip=[(AX, 1.2), (1.2, 38.0)])
    meadow_life(mb, tm, rng, 4.2)
    oak_accents(mb, tm, rng, lambda x, y: not mb.keep[min(int(y), H - 1), min(int(x), W - 1)] and not in_fp(mb, x, y, 3.0), 10.0)
    cm.edge_dress(mb, rng, 0.2, 0.08)
    t_fill(mb, tm, rng)
    town_npcs(mb)
    mb.extra_report = town_report
    return mb


def town_rich(mb, rng, tm, day, night, houses):
    """Roomy but dressed: fountain square, market clutter, guild quarter, residential gardens, night market, park, canal quay."""
    AX = TOWN_AVE_X
    # ---- square: fountain on a sun disc, ring of lamps, benches, planters, statues, steles, banners
    fx, fy = AX, 25.5
    D(mb, "sun_inlay", fx, fy - 0.4, check=False, file="sun_inlay_b.png")
    D(mb, "sun_inlay", 107.0, 32.0, check=False, file="sun_inlay_a.png")       # night plaza
    D(mb, "sun_inlay", AX, 88.0, check=False, file="sun_inlay_a.png")          # chapel plaza
    put(mb, "square_fountain", fx, fy, "fountain", "prop", apron=False)
    for a in np.linspace(0, 6.283, 8, endpoint=False):
        TD(mb, "lantern_post", fx + math.cos(a) * 6.4, fy + 0.5 + math.sin(a) * 4.2, road=True)
    for x, y in ((fx - 5.2, fy + 0.2), (fx + 5.2, fy + 0.2), (fx - 3.4, fy - 4.2), (fx + 3.4, fy - 4.2), (fx - 3.4, fy + 4.6), (fx + 3.4, fy + 4.6)):
        TD(mb, "bench", x, y, road=True)
    for sx in (-1, 1):                                           # planted strips beside the promenade
        for y in (31.5, 34.0, 36.5):
            TD(mb, "shrub_round", AX + sx * 9.0, y, road=True, margin=0.2)
            t_bed(mb, rng, AX + sx * 11.0, y, 1.0, 0.8, shrubs=0)
    for x, y, f in ((46.8, 45.2, "statue_weathered_a.png"), (83.2, 45.2, "statue_weathered_b.png"), (46.8, 17.6, "sun_stele_a.png"),
                    (83.2, 17.6, "sun_stele_c.png"), (60.0, 18.6, "sun_stele_b.png"), (70.0, 18.6, "sun_stele_a.png")):
        TD(mb, f.rsplit("_", 1)[0], x, y, file=f, road=True)
    for x in (58.0, 72.0):
        for y in (22.5, 33.0):
            TD(mb, "torch_post", x, y, road=True)
    for x, f in ((49.0, "banner_red_f0.png"), (58.5, "banner_blue_f0.png"), (71.5, "banner_green_f0.png"), (81.0, "banner_red_f0.png")):
        TD(mb, "banner", x, 45.4, file=f, road=True)
    for x, y in ((47.0, 33.5), (83.0, 33.5), (47.0, 25.0), (83.0, 25.0)):
        TD(mb, "hay_cart", x, y, road=True)
    # ---- stalls: crates / barrels at the sides, baskets and flower boxes in front
    fam6 = ("crate_stack", "barrel", "sack_pile", "crate", "basket", "hay_bale_round")
    for i, (x, y) in enumerate(day):
        s = -1 if x < AX else 1
        TD(mb, fam6[i % 6], x + s * 4.9, y + 0.5, road=True)
        TD(mb, fam6[(i + 2) % 6], x + s * 4.9, y + 2.0, road=True)
        TD(mb, "basket", x - 2.0, y - 3.4, road=True)
        TD(mb, "basket", x + 2.0, y - 3.6, road=True)
        TD(mb, "flower_box", x - 3.0, y - 0.6, role="attached", road=True)
        TD(mb, "flower_box", x + 3.0, y - 0.6, role="attached", road=True)
    # ---- hall forecourt
    for x in (59.0, 71.0):
        TD(mb, "flower_box", x, 40.0, role="attached", road=True)
    for x, y in ((57.5, 43.0), (72.5, 43.0), (57.5, 47.0), (72.5, 47.0)):
        TD(mb, "barrel" if x < AX else "crate_stack", x, y, road=True)
    # ---- lamp lines along streets and the avenue
    lamps_line(mb, (AX - 3.4, 20.0), (AX - 3.4, 4.0), 6.0)
    lamps_line(mb, (AX + 3.4, 20.0), (AX + 3.4, 4.0), 6.0)
    lamps_line(mb, (AX - 3.4, 56.0), (AX - 3.4, 82.0), 6.5)
    lamps_line(mb, (AX + 3.4, 56.0), (AX + 3.4, 82.0), 6.5)
    lamps_line(mb, (3.0, 36.0), (44.0, 36.0), 6.5)
    lamps_line(mb, (3.0, 40.2), (44.0, 40.2), 9.0, fam="torch_post")
    lamps_line(mb, (5.0, 51.0), (90.0, 51.0), 7.0)
    lamps_line(mb, (5.0, 55.2), (90.0, 55.2), 10.5, fam="torch_post")
    # ---- benches / barrels at the kerb along the long streets
    for x in np.arange(6.0, 44.0, 5.5):
        if rng.random() < 0.35:
            TD(mb, "bench", x, 40.9, road=True)
        if rng.random() < 0.3:
            TD(mb, str(rng.choice(["barrel", "crate", "basket"])), x + 1.6, 35.7, road=True)
    for x in np.arange(6.0, 90.0, 6.0):
        if rng.random() < 0.4:
            TD(mb, str(rng.choice(["bench", "barrel", "crate", "basket", "woodpile"])), x, 55.9, road=True)
        if rng.random() < 0.3:
            TD(mb, str(rng.choice(["barrel", "crate_stack"])), x + 2.2, 50.4, road=True)
    for y in np.arange(58.0, 82.0, 6.5):
        TD(mb, "bench", AX - 4.6, y + 2.0, road=True)
        TD(mb, "bench", AX + 4.6, y + 2.0, road=True)
    # ---- service yards (guild quarter etc.): clutter at the sides, flower boxes, beds, torches
    for f in mb.fps:
        if f["role"] in ("quarantine_post", "recruit_hall", "appraisal_house", "supply_store", "post_office", "auction_yard", "mercenary_stable"):
            r = f["rect"]
            dx, dy = f["door"]
            for sx in (r[0] - 1.1, r[2] + 1.1):
                TD(mb, str(rng.choice(["barrel", "crate", "crate_stack", "woodpile", "sack_pile"])), sx, dy + 0.4 + rng.uniform(0, 1.0), margin=0.2, road=True)
            for sx in (dx - 2.6, dx + 2.6):
                if r[0] + 0.4 < sx < r[2] - 0.4:
                    TD(mb, "flower_box", sx, dy - 0.7, role="attached", road=True)
            for sx in (r[0] - 2.4, r[2] + 2.4):
                t_bed(mb, rng, sx, dy - 1.4, 1.3, 0.9, shrubs=1)
            TD(mb, "torch_post", dx - 3.6, dy - 2.4, road=True)
            TD(mb, "torch_post", dx + 3.6, dy - 2.4, road=True)
    TD(mb, "well", 30.5, 31.8, file="well_b.png")
    for x, y in ((9.0, 32.0), (14.0, 30.0), (46.0, 29.0)):
        TD(mb, "bench", x, y, road=True)
    t_bed(mb, rng, 30.5, 29.0, 2.2, 1.2, shrubs=1)
    t_bed(mb, rng, 31.0, 34.2, 2.4, 0.9, shrubs=0)
    t_trees(mb, rng, (5, 28, 44, 36), 6, spread=2.2, min_d=8.0)
    t_trees(mb, rng, (5, 14, 42, 19), 4, spread=2.0, min_d=9.0)
    t_scatter(mb, rng, (4, 21, 44, 37), 6.0, ["shrub_round", "stump", "shrub_dark", "basket", "crate"], margin=1.2)
    TD(mb, "statue_weathered", 11.0, 50.0, file="statue_weathered_a.png", road=True)
    # ---- residential groups: fenced front gardens, back gardens with beds, trees between
    for i, (k, x, y) in enumerate(houses, 1):
        fp = next(f for f in mb.fps if f["role"] == f"house_{i}")
        r = fp["rect"]
        dx = fp["door"][0]
        hw = (r[2] - r[0]) / 2
        fy_ = y - 3.1
        t_fence(mb, (dx - hw - 1.2, fy_), (dx + hw + 1.2, fy_), gap=(dx - 1.0, dx + 1.0))
        t_fence(mb, (dx - hw - 1.2, fy_), (dx - hw - 1.2, y + 1.0))
        t_fence(mb, (dx + hw + 1.2, fy_), (dx + hw + 1.2, y + 1.0))
        t_bed(mb, rng, dx - hw * 0.55, y - 1.9, hw * 0.5, 0.7, shrubs=0)
        t_bed(mb, rng, dx + hw * 0.55, y - 1.9, hw * 0.5, 0.7, shrubs=0)
        for s in (-1, 1):
            TD(mb, "shrub_round", dx + s * (hw + 0.2), y + 0.8, margin=0.2)
            TD(mb, str(rng.choice(["barrel", "woodpile", "crate"])), dx + s * (hw + 1.6), y + 1.2, margin=0.2)
        TD(mb, "flower_box", dx - 1.6, y - 0.7, role="attached", road=True)
        TD(mb, "flower_box", dx + 1.6, y - 0.7, role="attached", road=True)
        by0 = r[3] + 0.8
        bx0, bx1 = dx - hw - 0.5, dx + hw + 0.5
        if by0 + 3.6 < mb.H - 5:
            t_fence(mb, (bx0, by0 + 3.4), (bx1, by0 + 3.4))
            for gx in np.arange(bx0 + 0.8, bx1 - 0.3, 1.3):
                for gy in np.arange(by0 + 0.6, by0 + 3.0, 1.2):
                    TF(mb, rng, gx, gy)
            TD(mb, "scarecrow" if i % 3 == 0 else "hay_bale_round", dx, by0 + 1.6, margin=0.2)
        for s in (-1, 1):
            TD(mb, str(rng.choice(["tree_amber", "oak_gold"])), dx + s * (hw + 3.6), y + 2.0 + rng.uniform(-0.5, 1.5), margin=2.4)
    TD(mb, "well", 20.5, 75.8, file="well_a.png")
    TD(mb, "well", 96.0, 75.8, file="well_b.png")
    for x, y in ((33.5, 75.8), (110.0, 75.8), (60.0, 66.0), (73.0, 66.0)):
        TD(mb, "bench", x, y)
    t_trees(mb, rng, (6, 60, 48, 100), 7, spread=2.4, min_d=9.0)
    t_trees(mb, rng, (80, 56, 125, 100), 7, spread=2.4, min_d=9.0)
    # ---- chapel avenue + plaza + green
    for y in (58.5, 64.5, 70.5, 76.5):
        for s in (-1, 1):
            TD(mb, "tree_amber", AX + s * 6.8, y, margin=2.0)
    for x, y in ((58.5, 91.2), (71.5, 91.2), (58.5, 86.0), (71.5, 86.0)):
        TD(mb, "lantern_post", x, y, road=True)
    for x, y, f in ((60.5, 84.0, "statue_weathered_b.png"), (69.5, 84.0, "statue_weathered_a.png"), (60.0, 90.0, "sun_stele_a.png"), (70.0, 90.0, "sun_stele_c.png")):
        TD(mb, f.rsplit("_", 1)[0], x, y, file=f, road=True)
    for x in (62.0, 68.0):
        TD(mb, "banner", x, 85.2, file="banner_blue_f0.png", road=True)
    for sx in (-1, 1):
        t_hedge(mb, rng, (AX + sx * 4.4, 57.0), (AX + sx * 4.4, 80.5), step=1.2)
    t_bed(mb, rng, 56.0, 70.0, 3.0, 4.0, shrubs=2)
    t_bed(mb, rng, 74.0, 70.0, 3.0, 4.0, shrubs=2)
    t_bed(mb, rng, 56.0, 61.0, 2.4, 1.8, shrubs=1)
    t_bed(mb, rng, 74.0, 61.0, 2.4, 1.8, shrubs=1)
    TD(mb, "well", 79.0, 75.0, file="well_a.png")
    # ---- night-market quarter: lantern strings, seating, carts
    for x in np.arange(93.0, 124.0, 4.0):
        TD(mb, "torch_post", x, 31.4, road=True)
        TD(mb, "lantern_post", x, 22.8, road=True)
        if x < 104 or x > 112:
            TD(mb, "lantern_post", x, 54.0, road=True)
    for x, f in ((95.0, "banner_red_f0.png"), (103.0, "banner_blue_f0.png"), (111.0, "banner_red_f0.png"), (119.0, "banner_blue_f0.png")):
        TD(mb, "banner", x, 31.4, file=f, road=True)
    for x, y in ((92.5, 24.0), (92.5, 52.0), (124.0, 24.0), (124.0, 52.0)):
        TD(mb, "torch_post", x, y, road=True)
    for i, (x, y) in enumerate(night):
        TD(mb, "crate" if i % 2 else "barrel", x - 3.3, y + 0.4, road=True)
        TD(mb, "basket", x + 3.3, y + 0.3, road=True)
        TD(mb, "torch_post", x - 2.8, y - 1.5, road=True)
        TD(mb, "torch_post", x + 2.8, y - 1.5, road=True)
        if i < 2:
            TD(mb, "market_table", x, y + 4.4, road=True)
    for x in (103.0, 106.0, 110.0, 113.0):
        TD(mb, "bench", x, 44.2, road=True)
    for x, y in ((94.0, 45.0), (122.0, 45.0)):
        TD(mb, "hay_cart", x, y, road=True)
    for x, y in ((94.0, 48.0), (122.0, 48.0), (106.0, 34.0)):
        TD(mb, "crate_stack", x, y, road=True)
    for sx in (97.0, 119.0):
        TD(mb, "statue_weathered", sx, 51.5, file="statue_weathered_a.png", road=True)
    # ---- stable yard: paddock fence, hay, troughs + architectural compound fences for Guild Quarter & Night Market
    t_fence(mb, (92.0, 22.0), (123.0, 22.0), gap=(104.0, 110.0))
    t_fence(mb, (92.0, 22.0), (92.0, 52.0), gap=(30.0, 36.0))
    t_fence(mb, (123.0, 22.0), (123.0, 52.0))
    t_fence(mb, (7.0, 22.0), (43.0, 22.0), gap=(20.0, 24.0))
    t_fence(mb, (7.0, 22.0), (7.0, 36.0))
    t_fence(mb, (114.0, 83.0), (126.0, 83.0))
    t_fence(mb, (114.0, 83.0), (114.0, 98.0))
    t_fence(mb, (114.0, 98.0), (126.0, 98.0))
    for x, y in ((117.0, 86.0), (121.0, 90.0), (117.0, 94.0), (122.0, 94.5)):
        TD(mb, str(rng.choice(["hay_bale_round", "feed_trough", "hay_bale_sq", "hay_pile"])), x, y, road=True)
    TD(mb, "hay_cart", 103.0, 82.0, road=True)
    TD(mb, "woodpile", 103.0, 90.8, road=True)
    TD(mb, "barrel", 115.2, 84.4, road=True)
    # ---- canal quay: crates, barrels, lamps; reeds + boulders on the banks, lily pads
    for x in np.arange(6.0, 126.0, 6.5):
        yq = _canal_y(x)
        if abs(x - AX) < 5.0 or abs(x - 22.0) < 3.5:
            continue
        if rng.random() < 0.55:
            TD(mb, str(rng.choice(["crate_stack", "barrel", "sack_pile", "crate", "woodpile"])), x, yq + 4.1, road=True)
        if rng.random() < 0.4:
            TD(mb, str(rng.choice(["barrel", "crate", "basket"])), x + 1.8, yq - 4.1, road=True)
        TD(mb, "lantern_post", x + 3.2, yq + 4.1, road=True)
    for x in (AX - 3.2, AX + 3.2, 20.0, 24.0):
        for yy in (_canal_y(x) - 4.2, _canal_y(x) + 4.2):
            TD(mb, "lantern_post", x, yy, road=True)
    for t in np.linspace(0, 1, 160):
        xx = -2 + 134 * t
        yy0 = _canal_y(xx)
        if abs(xx - AX) < 4.0 or abs(xx - 22.0) < 3.0:
            continue
        for side in (-1, 1):
            k = rng.random()
            if k < 0.22:
                D(mb, "reeds", xx, yy0 + side * 3.0)
            elif k < 0.30:
                D(mb, str(rng.choice(["boulder_s", "boulder_m"])), xx, yy0 + side * 4.3)
        if rng.random() < 0.28:
            D(mb, "lilypad", xx, yy0 + rng.uniform(-0.9, 0.9), water=True)
    # ---- park / green strip south of the canal (west and east of the Farm road)
    for rect in ((6, 1.6, 58, 9.5), (72, 1.6, 124, 9.5)):
        x0, y0, x1, y1 = rect
        t_trees(mb, rng, rect, 7, per=(3, 5), spread=2.0, min_d=8.0)
        for bx in np.arange(x0 + 6, x1 - 4, 11.0):
            t_bed(mb, rng, bx, 5.0 + 2.0 * math.sin(bx), 2.6, 1.4, shrubs=1)
            TD(mb, "bench", bx + 2.8, 5.6, road=True)
            TD(mb, "lantern_post", bx - 2.2, 5.0, road=True)
        TD(mb, "statue_weathered", (x0 + x1) / 2 + 0.5, 4.0, file="statue_weathered_b.png")
        t_hedge(mb, rng, (x0, 9.7), (x1, 9.7), step=1.3, gaps=[(AX - 4, AX + 4), (20.5, 23.5), (x0 + 24, x0 + 26)])
        t_scatter(mb, rng, rect, 5.5, ["shrub_round", "stump", "shrub_dark", "boulder_m"], margin=0.8)
    TD(mb, "well", 90.0, 6.0, file="well_a.png")
    TD(mb, "signpost", AX - 4.2, 9.0, road=True)
    TD(mb, "signpost", AX + 4.2, 9.0, road=True)
    # ---- far-north edge: orchard rows
    for gx in np.arange(8.0, 52.0, 5.5):
        for gy in (98.5, 103.5):
            TD(mb, "tree_amber", gx + rng.uniform(-0.6, 0.6), gy + rng.uniform(-0.5, 0.5), margin=2.0)
    for gx in np.arange(80.0, 124.0, 5.5):
        for gy in (98.5, 103.5):
            TD(mb, "oak_gold" if rng.random() < 0.4 else "tree_amber", gx + rng.uniform(-0.6, 0.6), gy + rng.uniform(-0.5, 0.5), margin=2.0)


def t_fill(mb, tm, rng, cell=4.0):
    """Last pass: any walkable 4x4 cell that still has no standing object gets one fitting prop (shrubs/stumps/trees on
    lawn, kerb props on stone/path), so open ground stays dressed without cramming."""
    free = ~mb.blocked
    for cy in np.arange(0, mb.H, cell):
        for cx in np.arange(0, mb.W, cell):
            sub = free[int(cy):int(min(cy + cell, mb.H)), int(cx):int(min(cx + cell, mb.W))]
            if sub.size == 0 or sub.mean() < 0.15:
                continue
            if any(o["layer"] != "ground" and cx <= o["x"] < cx + cell and cy <= o["y"] < cy + cell for o in mb.objects):
                continue
            for _ in range(14):
                x, y = cx + rng.uniform(0.4, cell - 0.4), cy + rng.uniform(0.4, cell - 0.4)
                t = tm_at(tm, x, y)
                if t == "w" or mb.yard[min(int(y), mb.H - 1), min(int(x), mb.W - 1)] or in_fp(mb, x, y, 0.5):
                    continue
                if t in "sp":
                    # Never drop random baskets/sacks/flower boxes onto road or stone tiles
                    continue
                else:
                    fam = str(rng.choice(["shrub_round", "shrub_dark", "stump", "boulder_m", "hay_bale_round", "tree_amber", "shrub_round"]))
                    if D(mb, fam, x, y, margin=2.4 if fam == "tree_amber" else 0.5):
                        break


def town_report(mb):
    """Spacing + dressing metrics for the market town (prints)."""
    b = [f for f in mb.fps if not f["decor"]]
    area = mb.W * mb.H
    gaps = []
    for i, a in enumerate(b):
        for c in b[i + 1:]:
            ra, rb = a["rect"], c["rect"]
            dx = max(ra[0] - rb[2], rb[0] - ra[2], 0.0)
            dy = max(ra[1] - rb[3], rb[1] - ra[3], 0.0)
            gaps.append((math.hypot(dx, dy), a["role"], c["role"]))
    gaps.sort()
    cells = hit = 0
    for cy in np.arange(0, mb.H, 8.0):
        for cx in np.arange(0, mb.W, 8.0):
            cells += 1
            if any(f["rect"][0] < cx + 8 and f["rect"][2] > cx and f["rect"][1] < cy + 8 and f["rect"][3] > cy for f in b):
                hit += 1
    e4 = empty_share(mb, 4.0)
    print("town size %dx%d | buildings %d | area per building %.0f m2 (target >=500)" % (mb.W, mb.H, len(b), area / len(b)))
    print("town min gap between building footprints %.1f m (%s x %s) (target >=3)" % gaps[0])
    print("town 8x8 cells containing a building footprint: %.1f%% (%d/%d) (target <=35%%)" % (100.0 * hit / cells, hit, cells))
    print("town walkable 4x4 cells with no object: %.1f%% (%d/%d) (target <=30%%)" % e4)
    sq = mb.landmarks["square_centre"]
    print("town square_centre", sq, "walkable:", not mb.blocked[int(sq[1]), int(sq[0])])


def town_npcs(mb):
    d = lambda r: door_of(mb, r)
    sd = d("stall_day_2")
    add_npc(mb, "ba_ban_rau", "Bà bán rau", (sd[0], sd[1] - 2.4), "stall_day_2",
            [(sd[0], sd[1] - 2.4), (sd[0] + 2.4, sd[1] - 3.4), (sd[0] + 2.4, sd[1] - 1.6), (sd[0] - 1.6, sd[1] - 3.0)], "S1136")
    bd = d("stall_day_3")
    add_npc(mb, "nguoi_ban_tin", "Người bán tin", (bd[0] + 4.0, bd[1] - 2.0), "stall_day_3",
            [(bd[0] + 4.0, bd[1] - 2.0), (62.0, 22.0), (bd[0] + 6.0, bd[1] - 3.2)], "S1067")
    nd = d("stall_night_1")
    add_npc(mb, "nguoi_ban_dem", "Người bán chợ đêm", (nd[0], nd[1] - 2.2), "stall_night_1",
            [(nd[0], nd[1] - 2.2), (nd[0] + 3.0, nd[1] - 3.0), (nd[0] - 3.0, nd[1] - 3.0)], "S1058")
    for rid, nid, nm, gdd in (("quarantine_post", "npc_quarantine", "Nhân viên kiểm dịch", "G1172"),
                              ("appraisal_house", "npc_appraisal", "Giám định viên", "G1172"),
                              ("supply_store", "npc_supply", "Chủ gian vật tư", "G1173"),
                              ("recruit_hall", "npc_recruit", "Người tuyển dụng", "G1174"),
                              ("mercenary_stable", "npc_mercenary", "Chủ chuồng lính thuê", "G434"),
                              ("auction_yard", "npc_auction", "Người điều hành đấu giá", "G1174"),
                              ("post_office", "npc_post", "Nhân viên bưu điện", "S1101")):
        x, y = d(rid)
        add_npc(mb, nid, nm, (x, y - 2.2), rid, [(x, y - 2.2), (x + 2.6, y - 3.2), (x - 2.6, y - 3.2)], None, gdd)
    finalize_npcs(mb)


H_ = 50   # legacy name kept: older helpers import it


def cm_fringe(mb, tm, rng):
    dforest = chamfer(tm == "f")
    ok = lambda x, y: tm_at(tm, x, y) == "m" and 0.8 < dforest[min(int(y), mb.H - 1), min(int(x), mb.W - 1)] < 4.0
    for x, y in poisson(rng, ok, (0, 0, mb.W, mb.H), 2.1, 30000):
        k = rng.random()
        if k < 0.3:
            place(mb, str(rng.choice(["shrub_round", "shrub_dark"])), x, y)
        elif k < 0.5:
            place(mb, "conifer_small", x, y, shadow=True)
        else:
            flat(mb, str(rng.choice(["flame_flower", "flower_purple", "grass_tuft_24x20", "flower_white"])), x, y, anim=True)


# --------------------------------------------------------------------------- HUB VILLAGE (Bo Ria)
VIL_W, VIL_H = 76, 56


def hub_village():
    W, H = VIL_W, VIL_H
    mb = MapBuilder("Hub_Village", "Hub_Village", "Hub_Village", W, H, seed=20261102)
    init_plan(mb, "p")
    rng = np.random.default_rng(888)
    cx, cy = mb.cx, mb.cy
    n_f = fbm(W + 1, H + 1, 8, 41, 3)
    edge = np.minimum.reduce([cx, cy, W - cx, H - cy])
    mb.paint((edge < 3.0 + 6 * (n_f - 0.35)) & (n_f > 0.4), "f")
    # ---- dirt roads: entrance + cross street (y=28), the village loop (x 10..62, y 16.8..46), north road to the town
    RW = 3.2
    for pts in ([(77, 28), (10, 28)], [(10, 16.8), (10, 46)], [(10, 46), (62, 46)], [(62, 16.8), (62, 46)], [(10, 16.8), (62, 16.8)],
                [(38, 46), (38, 57)], [(62, 38.6), (74, 38.6)]):
        mb.stroke("p", pts, RW, 0.2, int(pts[0][0] + pts[0][1]), smooth=False)
    mb.paint(cm._stone_blob(mb, 70.0, 37.2, 6.0, 2.4, 601, 0.5), "s")          # forecourt of the eatery
    mb.paint(cm._stone_blob(mb, 36.0, 38.0, 3.6, 2.9, 612, 0.5), "s")          # well square
    mb.paint(cm._stone_blob(mb, 70.0, 27.6, 4.0, 3.0, 613, 0.5), "s")          # entrance + bus stop
    # rice-drying yard (packed earth)
    mb.paint(rrect(mb, 45, 31.0, 60.5, 43.5, 2.5), "t")
    # ---- buildings
    put(mb, "nha_van_hoa", 22.0, 31.4, "nha_van_hoa", interact={"function": "VaoNhaScene", "id": "nha_van_hoa"})
    put(mb, "quan_ba_sau", 70.0, 41.0, "quan_ba_sau", interact={"function": "GiaoDichDanLang", "id": "quan_ba_sau"})
    # 7 resident houses, one per resident (VaoNhaScene); north row faces the loop road y=46, south row faces the lane y=16.8
    lay_row(mb, 49.0, 4.0, [("nha_man", "nha_man", 1.8, {"function": "VaoNhaScene", "id": "nha_man"}),
                            ("nha_tam_va", "nha_tam_va", 1.8, {"function": "VaoNhaScene", "id": "nha_tam_va"}),
                            ("nha_dung", "nha_dung", 1.8, {"function": "VaoNhaScene", "id": "nha_dung"})])
    lay_row(mb, 49.0, 43.5, [("nha_cuc", "nha_cuc", 1.8, {"function": "VaoNhaScene", "id": "nha_cuc"}),
                             ("nha_le", "nha_le", 1.8, {"function": "VaoNhaScene", "id": "nha_le"})])
    lay_row(mb, 19.8, 13.5, [("nha_anh_ca", "nha_anh_ca", 2.0, {"function": "VaoNhaScene", "id": "nha_anh_ca"})])
    lay_row(mb, 19.8, 36.0, [("nha_ong_ton", "nha_ong_ton", 2.0, {"function": "VaoNhaScene", "id": "nha_ong_ton"})])
    put(mb, "chuong_trau", 28.5, 19.8, "chuong_trau")
    put(mb, "kho_lua", 50.0, 19.8, "kho_lua")
    put(mb, "notice_board", 44.0, 30.6, "notice_board", "prop", {"function": "DocBangTinLore", "id": "bang_tin_lang"})
    put(mb, "gieng_chung", 36.0, 38.0, "gieng_chung", "prop")
    put(mb, "bia_sign", 71.0, 30.4, "bia_sign", "prop")
    put(mb, "xe_om_stop", 71.5, 25.4, "xe_om_stop", "prop")
    cm.add_fence_piece(mb, "fence_gate_arch", 74.6, 28.0, "right", "gate", "gate_arch")
    cm.add_fence_piece(mb, "fence_gate_arch", 38.0, 55.0, "up", "gate", "gate_arch")
    mb.portals = [
        {"id": "Hub_Village->Farm", "target": "Farm", "target_portal": "Farm->Hub_Village", "x": 74.6, "y": 28.0},
        {"id": "Hub_Village->MarketTown", "target": "MarketTown", "target_portal": "MarketTown->Hub_Village", "x": 38.0, "y": 55.4},
    ]
    mb.spawn = {"x": 66.0, "y": 25.0}
    mb.zones = [{"name": "village_green", "rect": [12, 30, 48, 15]}, {"name": "drying_yard", "rect": [45, 31, 15, 12]},
                {"name": "garden", "rect": [12, 3, 50, 11]}]
    mb.core_center = (36.0, 30.0)
    cm.worn_paths(mb, 812)
    cm.ground_variation(mb, 822, lush=0.58, moist=0.2 * np.exp(-chamfer(mb.c == "f") / 6.0) + 0.14)
    tm = finish_keep(mb)
    dpath = chamfer(mb.keep)
    village_rich(mb, rng, tm)
    wood_edge(mb, tm, rng, step=1.6, keep_out=lambda x, y: mb.keep[min(int(y), H - 1), min(int(x), W - 1)], p_skip=0.12, tall_bias=0.45)
    verges(mb, tm, rng, dpath)
    decor_pass(mb, tm, rng, region=lambda x, y: True, min_d=3.6, kinds=["bush", "meadow", "meadow", "rocks"])
    cm_fringe(mb, tm, rng)
    decor_pass(mb, tm, rng, region=lambda x, y: True, min_d=7, kinds=["flame"], tries=8000)
    meadow_life(mb, tm, rng, 5.0)
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) == "m" and (x < 6 or x > W - 6 or y > H - 7 or y < 6)
                        and not mb.keep[min(int(y), H - 1), min(int(x), W - 1)] and not in_fp(mb, x, y, 3.0), (0, 0, W, H), 6.0, 12000):
        vveg(mb, rng, x, y, 1.6)     # rim: bamboo / banana / areca instead of round amber oaks
    print("village placed:", dict(sorted(mb.vd_n.items())))
    cm.edge_dress(mb, rng, 0.2, 0.08)
    village_npcs(mb)
    return mb


# ---- Vietnamese hamlet vegetation / props (Argon village decor sheets). key -> (sheet, collider (w, h) | None, visual width m)
VDEC = {
    "bamboo_a": ("argon_vdecor_bamboo_clump_a", (0.8, 0.45), 2.2), "bamboo_b": ("argon_vdecor_bamboo_clump_b", (0.8, 0.45), 2.2),
    "bamboo_c": ("argon_vdecor_bamboo_clump_c", (0.7, 0.4), 1.8),
    "banana_a": ("argon_vdecor_banana_plant_a", (0.6, 0.4), 2.2), "banana_b": ("argon_vdecor_banana_plant_b", (0.6, 0.4), 2.0),
    "areca_a": ("argon_vdecor_areca_palm_a", (0.4, 0.3), 1.8), "areca_b": ("argon_vdecor_areca_palm_b", (0.4, 0.3), 2.2),
    "jar_a": ("argon_vdecor_water_jar_a", (0.5, 0.4), 1.0), "jar_b": ("argon_vdecor_water_jar_b", (0.5, 0.4), 1.3),
    "haystack": ("argon_vdecor_haystack_round_vn", (1.0, 0.7), 2.7),
    "fence": ("argon_vdecor_woven_bamboo_fence_h", (1.8, 0.6), 1.8),
}
VEG_BED = "argon_vdecor_vegetable_bed"
# rectangles that must stay free of plants: well square, rice-drying yard
V_EXCL = [(31.0, 33.5, 41.5, 42.0), (44.0, 30.2, 61.5, 44.2)]


def _vstat(mb):
    if not hasattr(mb, "vd"):
        mb.vd, mb.vd_n = [], {}
    return mb.vd, mb.vd_n


def vok(mb, key, x, y, margin=0.5, space=0.42, free_keep=True):
    sheet, col, vw = VDEC[key]
    w, h = col
    if not (1.0 < x < mb.W - 1.0 and 1.0 < y < mb.H - 1.0):
        return False
    for ex in V_EXCL:
        if ex[0] - 0.3 <= x <= ex[2] + 0.3 and ex[1] - 0.3 <= y <= ex[3] + 0.3:
            return False
    for px in (x - w / 2, x, x + w / 2):
        if in_fp(mb, px, y, margin):
            return False
    xa, xb = int(math.floor(x - w / 2 - 0.5)), int(math.floor(x + w / 2 + 0.5))
    ya, yb = int(math.floor(y - 0.7)), int(math.floor(y + h / 2 + 0.5))
    if xa < 0 or ya < 0 or xb >= mb.W or yb >= mb.H:
        return False
    if mb.blocked[ya:yb + 1, xa:xb + 1].any() or (free_keep and mb.keep[ya:yb + 1, xa:xb + 1].any()):
        return False
    if (mb.c[ya:yb + 1, xa:xb + 1] == "w").any():
        return False
    if key != "fence":
        for ox, oy, ow in mb.vd:
            if math.hypot(ox - x, (oy - y) * 1.6) < 0.5 * (ow + vw) * space * 2:
                return False
    return True


def vplace(mb, key, x, y, margin=0.5, space=0.42, free_keep=True):
    vd, n = _vstat(mb)
    if not vok(mb, key, x, y, margin, space, free_keep):
        return False
    sheet, col, vw = VDEC[key]
    cm.add_building(mb, sheet, x, y, "down", f"vd_{key}", "prop", None, col)
    if key != "fence":
        vd.append((x, y, vw))
    kind = key.split("_")[0]
    n[kind] = n.get(kind, 0) + 1
    return True


def vveg(mb, rng, x, y, margin=1.0, w=(0.4, 0.35, 0.25)):
    """one plant of the hamlet mix (bamboo / banana / areca)"""
    r = rng.random()
    if r < w[0]:
        k = str(rng.choice(["bamboo_a", "bamboo_b", "bamboo_c"]))
    elif r < w[0] + w[1]:
        k = str(rng.choice(["banana_a", "banana_b"]))
    else:
        k = str(rng.choice(["areca_a", "areca_b"]))
    return vplace(mb, k, x, y, margin)


def hedge(mb, rng, p0, p1, step=2.0, skip=0.08, kinds=("bamboo_a", "bamboo_b", "bamboo_c", "bamboo_b"), jit=0.35, margin=0.6):
    n = max(int(math.hypot(p1[0] - p0[0], p1[1] - p0[1]) / step), 1)
    for i in range(n + 1):
        t = i / n
        x = p0[0] + (p1[0] - p0[0]) * t + rng.normal(0, jit)
        y = p0[1] + (p1[1] - p0[1]) * t + rng.normal(0, jit * 0.6)
        if rng.random() < skip:
            continue
        vplace(mb, str(rng.choice(kinds)), x, y, margin, 0.34)


def fence_run(mb, x0, x1, y, gap=None):
    """woven bamboo fence, 1.8 m segments along x from x0..x1 (optional gap (gx0, gx1) left open)"""
    n = max(int(round((x1 - x0) / 1.8)), 1)
    seg = (x1 - x0) / n
    for i in range(n):
        cx = x0 + seg * (i + 0.5)
        if gap and gap[0] <= cx <= gap[1]:
            continue
        vplace(mb, "fence", cx, y, 0.2, free_keep=True)


def veg_plot(mb, x0, x1, y0, y1, cols, rows, fence=True):
    """vegetable beds (ground layer, walkable) inside a plot + bamboo fences along the long sides"""
    for i in range(cols):
        for j in range(rows):
            bx = x0 + (x1 - x0) * (i + 0.5) / cols
            by = y0 + 0.6 + j * (y1 - y0 - 2.4) / max(rows - 1, 1)
            if in_fp(mb, bx, by + 0.9, 0.5) or any(e[0] - 1 < bx < e[2] + 1 and e[1] - 1 < by < e[3] + 1 for e in V_EXCL):
                continue
            mb.add(f"Assets/Art/PixelMyth/Buildings/{VEG_BED}/{VEG_BED}_static.png", bx, by, "ground", None, "decor:veg_bed", None,
                   sub=f"{VEG_BED}_static_down", _meta={"name": VEG_BED, "row": "down"})
            _vstat(mb)[1]["vegetable_bed"] = _vstat(mb)[1].get("vegetable_bed", 0) + 1
    if fence:
        fence_run(mb, x0, x1, y1 + 0.1)
        fence_run(mb, x0, x1, y0 - 0.1)


def village_rich(mb, rng, tm):
    _vstat(mb)
    # --- entrance: BO RIA sign, xe-om stop with bench + lamp, banners
    D(mb, "bench", 69.0, 25.2, margin=0.2, allow_keep=True)
    D(mb, "lantern_post", 73.0, 30.6, allow_keep=True)
    D(mb, "lantern_post", 73.0, 25.6, allow_keep=True)
    D(mb, "signpost", 67.4, 30.8, allow_keep=True)
    D(mb, "banner", 68.6, 30.8, file="banner_green_f0.png", allow_keep=True)
    # --- eatery: outdoor tables with benches, lanterns
    for i, x in enumerate((64.6, 68.0, 72.0)):
        D(mb, "market_table", x, 35.6, allow_keep=True)
        D(mb, "bench", x - 1.2, 34.7, allow_keep=True)
        D(mb, "bench", x + 1.2, 34.7, allow_keep=True)
        D(mb, "basket", x + 1.0, 36.2, role="attached", allow_keep=True)
    for x, y in ((66.0, 37.6), (75.0, 40.0), (63.5, 41.0), (74.6, 36.0)):
        D(mb, "lantern_post", x, y, allow_keep=True)
    for x, y in ((74.6, 42.4), (63.6, 43.5), (66.0, 44.4)):
        D(mb, str(rng.choice(["barrel", "crate", "woodpile"])), x, y, allow_keep=True)
    # --- well square: common well + benches + lamps (no sun inlay mat: a European plaza motif)
    for x, y in ((32.4, 36.6), (39.8, 36.6), (36.0, 34.6)):
        D(mb, "bench", x, y, allow_keep=True)
    for x, y in ((31.0, 38.0), (41.0, 38.0)):
        D(mb, "lantern_post", x, y, allow_keep=True)
    t1 = put(mb, "tree_banyan", 15.5, 40.0, "cay_mua_mua_dung_duoi", "prop", apron=False)     # the huge banyan
    t2 = put(mb, "tree_tamarind", 65.5, 46.4, "cay_mua_nao_cung_dung_duoi", "prop", apron=False)
    if not (t1 and t2):
        mb.problems.append("named trees not placed")
    # --- rice drying yard: golden mats in rows, baskets, bales, drying racks
    for x, y in ((49.4, 32.6), (56.4, 32.6), (49.4, 36.8), (56.4, 36.8)):   # flat golden mats: ground layer, walkable
        mb.add("Assets/Art/PixelMyth/Buildings/argon_village_rice_drying_yard/argon_village_rice_drying_yard_static.png", x, y,
               "ground", None, "decor:rice_mat", None, sub="argon_village_rice_drying_yard_static_down",
               _meta={"name": "argon_village_rice_drying_yard", "row": "down"})
    for x, y in ((46.0, 32.6), (59.0, 34.0), (46.0, 41.6), (59.0, 42.0), (53.0, 44.2), (53.0, 31.4)):
        D(mb, str(rng.choice(["basket", "sack_pile", "hay_bale_sq"])), x, y, allow_keep=True)
    D(mb, "laundry_line", 56.0, 40.6, file="laundry_line_a.png", allow_keep=True)
    D(mb, "laundry_line", 49.0, 40.6, file="laundry_line_b.png", allow_keep=True)
    # --- culture house: banners, benches, lamp
    D(mb, "banner", 15.6, 29.8, file="banner_red_f0.png", allow_keep=True)
    D(mb, "banner", 28.6, 29.8, file="banner_red_f0.png", allow_keep=True)
    D(mb, "bench", 22.0, 29.2, allow_keep=True)
    # --- along the roads
    lamps_line(mb, (12, 26.2), (60, 26.2), 8.0)
    lamps_line(mb, (12, 44.4), (60, 44.4), 8.0)
    lamps_line(mb, (36.0, 47.5), (36.0, 54.0), 4.0)
    lamps_line(mb, (40.2, 47.5), (40.2, 54.0), 4.0)
    # --- vegetable plots: woven-bamboo fences + vegetable beds
    for x0, x1, y0, y1 in ((12.0, 22.0, 4.5, 12.5), (24.0, 34.0, 4.5, 12.5), (36.0, 46.0, 4.5, 12.5), (48.0, 58.0, 4.5, 12.5)):
        veg_plot(mb, x0, x1, y0, y1, 3, 3)
        D(mb, "scarecrow", (x0 + x1) / 2 + 0.2, y0 + 3.7, collider=False)
    for x0, x1, y0, y1 in ((12.5, 18.5, 33.0, 38.0), (12.5, 18.5, 39.6, 44.0)):   # family gardens on the west green
        veg_plot(mb, x0, x1, y0, y1, 2, 2)
    # --- livestock yards: buffalo / pigs behind woven-bamboo fences, hay inside
    for x0, x1, y0, y1 in ((19.0, 25.0, 20.4, 25.0), (54.0, 60.0, 20.0, 25.0), (30.4, 35.0, 22.0, 25.0)):
        fence_run(mb, x0, x1, y1)
        fence_run(mb, x0, x1, y0, gap=((x0 + x1) / 2 - 1.2, (x0 + x1) / 2 + 1.2))
        D(mb, "hay_pile", x0 + 1.6, y1 - 1.5)
        D(mb, "feed_trough", (x0 + x1) / 2 + 0.6, y1 - 1.6) if "feed_trough" in cm.COL else None
    # --- water jars at the doors (resident houses + eatery), banana / areca at the house sides
    homes = [b for b in mb.fps if (b["role"].startswith("nha_") and b["role"] != "nha_van_hoa") or b["role"] == "quan_ba_sau"]
    for b in homes:
        dx, dy = b["door"]
        for sgn in (-1, 1):
            if sgn == -1 or rng.random() < 0.55:     # always one jar, often a second one on the other side
                for off in (2.9, 3.4, 3.9, 4.6):
                    if vplace(mb, str(rng.choice(["jar_a", "jar_b"])), dx + sgn * off, dy - 0.5, 0.2, free_keep=False):
                        break
    for b in homes:
        r = b["rect"]
        for sgn, xx in ((-1, r[0] - 1.1), (1, r[2] + 1.1)):
            for k in range(3):
                kind = str(rng.choice(["banana_a", "banana_b", "areca_a", "areca_b"]))
                if vplace(mb, kind, xx + sgn * 0.8 * k, b["door"][1] + 0.4 + rng.uniform(-0.3, 1.2), 0.2):
                    break
    # --- haystacks: by the buffalo shed / rice store and the drying yard
    for x, y in ((24.0, 19.0), (33.4, 19.4), (62.6, 34.0), (43.0, 43.8), (54.0, 23.0)):
        for ddx, ddy in ((0, 0), (1.2, 0.5), (-1.2, 0.5), (0, -1.0), (2.2, 0), (-2.2, 0), (0, 1.2), (1.5, -1.0), (-1.5, -1.0)):
            if vplace(mb, "haystack", x + ddx, y + ddy, 0.3, 0.3, free_keep=False):
                break
    # --- yards of the 7 houses: flowers, hay, woodpile, laundry
    for b in mb.fps:
        if b["role"].startswith("nha_") and b["role"] != "nha_van_hoa":
            r = b["rect"]
            for gx in np.arange(r[0], r[2], 1.4):
                D(mb, "wildflowers", gx, b["door"][1] - 2.4, allow_keep=False)
            D(mb, str(rng.choice(["woodpile", "hay_bale_sq"])), r[2] + 1.0, r[1] + 1.2, allow_keep=True)
            D(mb, "laundry_line", r[0] + (r[2] - r[0]) * 0.5, r[3] + 1.6, file="laundry_line_a.png", margin=0.3, allow_keep=False)
            D(mb, "flower_box", b["door"][0] - 2.5, b["door"][1] - 0.6, role="attached", allow_keep=True)
            D(mb, "flower_box", b["door"][0] + 2.5, b["door"][1] - 0.6, role="attached", allow_keep=True)
            D(mb, "barrel", r[0] - 0.8, r[1] + 1.0, allow_keep=True)
    # --- bamboo hedgerows: village edge, along the loop lanes, behind the houses, between the garden plots
    hedge(mb, rng, (6.4, 17.5), (6.4, 46.0), 2.1)                        # west lane
    hedge(mb, rng, (65.6, 18.0), (65.6, 26.5), 2.1)                      # east lane (north part)
    hedge(mb, rng, (65.4, 31.0), (65.4, 36.0), 2.1)
    hedge(mb, rng, (3.5, 54.4), (34.0, 54.4), 2.0)                       # behind the north house row
    hedge(mb, rng, (42.0, 54.4), (72.0, 54.4), 2.0)
    hedge(mb, rng, (12.0, 25.4), (60.0, 25.4), 2.4, skip=0.3)            # behind the south house row / pens
    hedge(mb, rng, (8.4, 14.8), (63.0, 14.8), 2.2, skip=0.2)             # south edge of the lane toward the plots
    hedge(mb, rng, (12.0, 2.8), (60.0, 2.8), 2.1)                        # back of the plots
    for x in (23.0, 35.0, 47.0, 59.2):
        hedge(mb, rng, (x, 4.0), (x, 13.0), 2.6, skip=0.1, kinds=("bamboo_c", "banana_a", "areca_a", "banana_b"))
    # --- fill: banana / areca / bamboo instead of round amber trees (around the hamlet, never the forest belt)
    for x, y in poisson(rng, lambda x, y: (y < 15 or y > 52 or x < 8 or x > 66 or 30 < y < 45 and x < 20) and not (11 < x < 59 and 3.5 < y < 13.5) and not mb.keep[min(int(y), mb.H - 1), min(int(x), mb.W - 1)]
                        and not in_fp(mb, x, y, 2.4) and tm_at(tm, x, y) in "mg", (0, 0, mb.W, mb.H), 4.4, 9000):
        vveg(mb, rng, x, y, 1.6)
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) in "mgd" and not mb.keep[min(int(y), mb.H - 1), min(int(x), mb.W - 1)]
                        and not in_fp(mb, x, y, 2.4) and 3 < x < mb.W - 3 and 3 < y < mb.H - 3
                        and not (44 < x < 62 and 30 < y < 45) and not (31 < x < 41 and 34 < y < 42) and not (11 < x < 59 and 3.5 < y < 13.5), (0, 0, mb.W, mb.H), 3.6, 9000):
        k = rng.random()
        if k < 0.75:
            vveg(mb, rng, x, y, 1.6)
            if rng.random() < 0.08:
                vplace(mb, "haystack", x + 2.4, y - 0.4, 0.8, 0.3)
        else:
            D(mb, "hay_pile", x, y)
            D(mb, "woodpile", x + 2.0, y + 0.4)
    # the green itself: a few shade plants
    for x, y in ((18.0, 36.6), (28.0, 43.0), (26.0, 36.0), (42.0, 42.6), (60.0, 30.4), (13.4, 32.0)):
        vveg(mb, rng, x, y, 1.4, w=(0.2, 0.5, 0.3))


def village_npcs(mb):
    d = lambda r: door_of(mb, r)
    qx, qy = d("quan_ba_sau")
    add_npc(mb, "ba_sau", "Bà Sáu", (qx, qy - 1.6), "quan_ba_sau", [(qx, qy - 1.6), (qx + 3.0, qy - 2.8), (qx - 3.0, qy - 2.8)], "S436")
    well = d("gieng_chung")
    nb = d("notice_board")
    vh = d("nha_van_hoa")
    stop = d("xe_om_stop")
    for rid, nid, nm, src, route in (
            ("nha_man", "man", "Mận", "S1051", lambda h: [(h[0], h[1] - 2.2), (stop[0] - 2.0, stop[1]), (well[0] - 2.5, well[1] - 2.0)]),
            ("nha_tam_va", "tam_va", "Tám Vá", "S1034", lambda h: [(h[0], h[1] - 2.2), (vh[0] + 3.0, vh[1] - 2.2), (nb[0], nb[1] - 1.6)]),
            ("nha_dung", "dung", "Bác sĩ Dung", "S1035", lambda h: [(h[0], h[1] - 2.2), (well[0] + 2.5, well[1] - 2.0), (h[0] + 6.0, h[1] - 3.0)]),
            ("nha_cuc", "cuc", "Cúc", "S1036", lambda h: [(h[0], h[1] - 2.2), (52.0, 37.0), (h[0] - 5.0, h[1] - 3.2)]),
            ("nha_le", "le", "Lệ", "S1037", lambda h: [(h[0], h[1] - 2.2), (qx - 3.0, qy - 3.0), (nb[0] + 2.0, nb[1] - 1.5)]),
            ("nha_anh_ca", "anh_ca", "Anh Ca", "S1038", lambda h: [(h[0], h[1] - 2.0), (vh[0], vh[1] - 3.0), (well[0] - 3.0, well[1] - 2.5)]),
            ("nha_ong_ton", "ong_ton", "Ông Tồn", "S1039", lambda h: [(h[0], h[1] - 2.0), (h[0] + 5.0, h[1] - 1.8), (h[0] - 4.0, h[1] - 1.8)])):
        h = d(rid)
        pts = route(h)
        add_npc(mb, nid, nm, pts[0], rid, pts, src)
    add_npc(mb, "anh_tu", "Anh Tư", (stop[0] + 1.6, stop[1] - 0.6), None, [(stop[0] + 1.6, stop[1] - 0.6), (stop[0] + 1.6, stop[1] + 1.2)], "S188")
    finalize_npcs(mb)


# --------------------------------------------------------------------------- FOREST PRIMEVAL
def forest_primeval():
    W, H = 60, 60
    mb = MapBuilder("Forest_Primeval", "Forest_Primeval", "Forest_Primeval", W, H, seed=20261103)
    rng = np.random.default_rng(999)
    cx, cy = mb.cx, mb.cy
    mb.paint(np.ones_like(cx, bool), "f")
    CX, CY = 30.0, 31.0
    clear = (((cx - CX) / 10.5) ** 2 + ((cy - CY) / 8.0) ** 2 + (fbm(W + 1, H + 1, 5, 51) - 0.5) * 0.35) < 1
    mb.paint(clear, "m")

    def ring(rx, ry, n=16, ph=0.0, jit=1.2, seed=0):
        r = np.random.default_rng(seed)
        pts = [(CX + (rx + r.uniform(-jit, jit)) * math.cos(ph + 2 * math.pi * i / n),
                CY + (ry + r.uniform(-jit, jit)) * math.sin(ph + 2 * math.pi * i / n)) for i in range(n)]
        return pts + pts[:3]

    def loop(pts, width, seed):
        c = catmull(pts, 10)
        d = poly_dist(cx, cy, c) + wob(mb, seed, 0.5, 5)
        return d <= width / 2

    inner = loop(ring(16, 13.5, 14, 0.3, 1.0, 1), 2.6, 11)
    outer = loop(ring(25, 24.5, 20, 0.1, 1.4, 2), 2.6, 12)
    spokes = np.zeros_like(inner)
    for pts in ([(30, 22), (31, 17), (29, 11), (30, 6.5)], [(30, 40), (29, 46), (31, 52), (30, 57)],
                [(40, 30), (45, 31), (50, 30), (55, 31)], [(20, 32), (14, 31), (9, 32), (4, 31)]):
        spokes |= loop(pts, 2.6, 13)
    stream = [(46, -2), (44, 8), (47, 18), (54, 24), (62, 27)]
    sd = poly_dist(cx, cy, catmull(stream, 10)) + wob(mb, 14, 0.6, 5)
    water = sd <= 1.7
    road = inner | outer | spokes
    mb.paint(road, "p")
    mb.paint(clear, "m")
    mb.paint(water, "w")
    ford = (sd <= 2.5) & road
    mb.paint(ford, "s")
    # relic apron: path to the gate
    relic_at = (30.0, 33.5)
    mb.stroke("p", [(30, 22.5), (30, 30)], 3.0, smooth=False)
    # no fixed relic building: the patched event gate (MysticGate) rises here on arrival and is the way back
    mb.portals = [{"id": "Forest_Primeval->Farm", "target": "Farm", "target_portal": "Farm->Forest_Primeval",
                   "x": relic_at[0], "y": relic_at[1]}]
    mb.spawn = {"x": 30.0, "y": 27.5}
    mb.zones = [{"name": "clearing", "rect": [20, 24, 21, 16]}, {"name": "inner_loop", "rect": [14, 17, 32, 28]},
                {"name": "outer_loop", "rect": [5, 6, 50, 49]}]
    mb.core_center = (30.0, 28.0)
    # ruin glades (small clearings) before trees
    ruins = [(12, 48, "arch"), (49, 52, "mini"), (51, 17, "court"), (10, 12, "colonnade"), (45, 33.5, "mini"), (14, 30, "mini")]
    for rx, ry, _k in ruins:
        mb.paint((((cx - rx) / 6.0) ** 2 + ((cy - ry) / 4.6) ** 2) < 1, "m")
    mb.paint(water, "w")
    mb.paint(road & ~water, "p")
    mb.paint(ford, "s")
    cm.worn_paths(mb, 813, thr=0.7)
    cm.ground_variation(mb, 823, lush=0.4, dry=0.9, dirt=0.93, moist=0.2 * np.exp(-chamfer(mb.c == "f") / 5.0))
    mb.c[(mb.c == "m") & (((cx - CX) / 8.0) ** 2 + ((cy - CY) / 6.2) ** 2 < 1)] = "g"       # lush mist-lit glade core
    tm = finish_keep(mb)
    dpath = chamfer(mb.keep)
    # relic surroundings: flanking ruins and standing stones
    for fam, dx, dy in [("ruin_pillar", -5.5, -0.5), ("ruin_pillar", 5.5, -0.3), ("ruin_wall", -7.5, 1.6), ("ruin_wall", 7.5, 1.8),
                        ("ruin_cairn", -3.5, -3.2), ("ruin_cairn", 3.6, -3.3), ("boulder_m", -9.0, -1.5), ("boulder_m", 9.0, -1.0),
                        ("ruin_arch_fragment", -8.5, 6.0), ("ruin_pillar", 8.5, 5.5)]:
        place(mb, fam, relic_at[0] + dx, relic_at[1] + dy, check=False)
    for rx, ry, k in ruins:
        cm.stamp_ruin(mb, rx, ry, k, np.random.default_rng(int(rx * 100 + ry)))
    # mist-lit clearing: pale flowers and tufts crowd the glade, flame flowers glow at the edge
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) == "m" and ((x - CX) / 10.5) ** 2 + ((y - CY) / 8) ** 2 < 1.1, (CX - 12, CY - 10, CX + 12, CY + 10), 1.1, 9000):
        flat(mb, str(rng.choice(["flower_white", "flower_white", "flower_purple", "grass_tuft_24x20", "flame_flower", "grass_tuft_16x16"])), x, y, anim=True)
    # stream: reeds, boulders, stepping stones
    for t in np.linspace(0, 1, 160):
        pts = catmull(stream, 10)
        a = pts[min(int(t * (len(pts) - 1)), len(pts) - 1)]
        for side in (-1, 1):
            px, py = a[0] + side * rng.uniform(1.8, 2.6), a[1] + rng.uniform(-0.6, 0.6)
            k = rng.random()
            if tm_at(tm, px, py) in "fm":
                if k < 0.25:
                    place(mb, str(rng.choice(["boulder_m", "boulder_s"])), px, py)
                elif k < 0.6:
                    flat(mb, "grass_tuft_24x20", px, py, anim=True)
    # forest floor
    def keep_out(x, y):
        yi, xi = min(int(y), H - 1), min(int(x), W - 1)
        return bool(mb.keep[yi, xi]) or bool(mb.blocked[yi, xi]) or ((x - CX) ** 2 / 10.5 ** 2 + (y - CY) ** 2 / 8 ** 2 < 0.9)
    wood_edge(mb, tm, rng, step=1.05, keep_out=keep_out, p_skip=0.03, tall_bias=0.55, giant_p=0.3)
    wood_edge(mb, tm, rng, step=1.05, keep_out=keep_out, p_skip=0.35, tall_bias=0.3, giant_p=0.3)   # second, offset pass: denser
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) == "f" and not keep_out(x, y), (0, 0, W, H), 2.4, 30000):
        k = rng.random()
        if k < 0.35:
            place(mb, str(rng.choice(["boulder_m", "boulder_s", "boulder_l"])), x, y)
        elif k < 0.7:
            flat(mb, str(rng.choice(["grass_tuft_24x20", "grass_tuft_16x16"])), x, y, anim=True)
        else:
            place(mb, "shrub_dark", x, y, shadow=False)
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) in "m" and not mb.keep[min(int(y), H - 1), min(int(x), W - 1)], (0, 0, W, H), 3.0, 20000):
        cluster(mb, tm, rng, x, y, str(rng.choice(["bush", "meadow", "rocks", "flame"])))
    verges(mb, tm, rng, dpath, 0.9, 2.4, 2.0)
    for x, y in poisson(rng, lambda x, y: tm_at(tm, x, y) == "f" and 0.9 < dpath[min(int(y), H - 1), min(int(x), W - 1)] < 2.2, (0, 0, W, H), 2.6, 20000):
        flat(mb, str(rng.choice(["grass_tuft_24x20", "flame_flower", "flower_purple", "flower_white"])), x, y, anim=True)
    rim_boulders(mb, tm, rng, depth=1.0, step=1.5)
    forest_rich(mb, tm, rng, relic_at, CX, CY, stream)
    cm.edge_dress(mb, rng, 0.2, 0.06)
    return mb


# --------------------------------------------------------------------------- output
# the forest is generated by compose_forest.py (single event-edge map); forest_primeval() below is the retired version
MAPS = {"town": (market_town, "town"), "village": (hub_village, "village")}


def write(mb, tag, render=True):
    OUT.mkdir(parents=True, exist_ok=True)
    data = {
        "map": mb.name, "location_id": mb.location_id, "scene_name": mb.scene, "size": [mb.W, mb.H], "tile_px": T,
        "terrain": {"legend": cm.LEGEND, "corner_rows": mb.corner_rows()},
        "objects": [{k: v for k, v in o.items() if k != "_meta"} for o in mb.objects],
        "portals": mb.portals, "npcs": getattr(mb, "npcs", []), "spawn": mb.spawn, **({"landmarks": mb.landmarks} if hasattr(mb, "landmarks") else {}), "zones": mb.zones, "gate_event_zones": [], "enemy_entries": [],
    }
    (OUT / f"{tag}_layout.json").write_text(json.dumps(data, separators=(",", ":")), encoding="utf-8")
    if not render:
        return
    saved_objs = mb.objects
    if tag == "forest":
        # JSON keeps all 12 site:<id> objects for runtime ForestEventSites switching;
        # preview PNG shows only the active site ('kho_lanh_bo_quen') so 12 landmarks aren't stacked in the preview image.
        mb.objects = [
            o for o in saved_objs
            if not ((o["role"].startswith("site:") or o["role"].startswith("site_prop:"))
                    and not o["role"].endswith(":kho_lanh_bo_quen"))
        ]
    img = cm.render(mb)
    mb.objects = saved_objs
    img.save(OUT / f"{tag}_preview.png")
    px, py = mb.spawn["x"] * T, (mb.H - mb.spawn["y"]) * T
    x0 = int(max(0, min(img.width - 640, px - 320)))
    y0 = int(max(0, min(img.height - 360, py - 180)))
    img.crop((x0, y0, x0 + 640, y0 + 360)).resize((1920, 1080), Image.NEAREST).save(OUT / f"{tag}_preview_crop_x3.png")


def main():
    which = [a for a in sys.argv[1:] if not a.startswith("--")] or list(MAPS)
    for k in which:
        fn, tag = MAPS[k]
        mb = fn()
        if hasattr(mb, "fps"):
            pre = purge(mb)       # remove any decor that landed on a footprint / door pad, then re-measure
            print(k, "overlap violations: purged", pre, "-> remaining", len(violations(mb)), "| building-vs-building overlaps",
                  building_overlaps(mb), "| layout problems", mb.problems)
            e = empty_share(mb)
            e4 = empty_share(mb, 4.0)
            print(k, "empty 4x4 cells (stricter, info only): %.1f%% (%d/%d)" % e4)
            print(k, "empty 8x8 cells: %.1f%% (%d/%d)" % e, "| buildings", sum(1 for f in mb.fps if not f["decor"]), "| npcs", len(mb.npcs))
            bad_npc = [n["id"] for n in mb.npcs if mb.blocked[int(n["y"]), int(n["x"])] or any(mb.blocked[int(q[1]), int(q[0])] for q in n["route"])]
            print(k, "npcs on blocked tiles:", bad_npc)
            if hasattr(mb, "extra_report"):
                mb.extra_report(mb)
        write(mb, tag, "--no-render" not in sys.argv)
        tg = {p["id"]: (p["x"], p["y"]) for p in mb.portals}
        rch, nfree, ntot = reachable(mb, (mb.spawn["x"], mb.spawn["y"]), tg)
        print(k, "objects", len(mb.objects), "placeholders", sorted(set(mb.placeholders)), "portal reach", rch,
              "walkable reachable %d/%d" % (nfree, ntot))


if __name__ == "__main__":
    main()
