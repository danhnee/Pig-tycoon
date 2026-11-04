"""Forest_Primeval (edge of the primeval forest) - event-site map.

  python tools/pixelart/compose_forest.py            # writes Assets/Art/PixelMyth/Maps/forest_layout.json + previews
  python tools/pixelart/compose_forest.py --no-render

The player only ever sees the EDGE of the forest (a space outside the world), reached through the occult event gate
(MysticGate draws it at the portal; no gate/relic building is placed here). 12 glades, one per in-forest event
template, joined by narrow dirt game trails (no paved road, no walking way out). Each glade holds a landmark
(role "site:<id>", interact DocBangTinLore = placeholder for the event start) and a zone "event_site:<id>".
All site art goes through SITE_ART until the artist delivers dedicated sprites. Deterministic; numpy + Pillow.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import compose_map as cm  # noqa: E402
from compose_map import MapBuilder, chamfer, fbm, flat, place, poisson  # noqa: E402
from compose_maps_other import write, finish_keep, tm_at, wood_edge, rim_boulders, reachable  # noqa: E402

# site id -> (existing sprite, is_placeholder). Value is a Buildings/<name> sheet, or "decor:<file>" for a decor sprite.
# Replace these values when dedicated art arrives; nothing else in the file needs to change.
SITE_ART = {  # Argon A4 art (candidates pending the user's visual approval)
    "kho_lanh_bo_quen": ("argon_site_forgotten_cold_store", False),
    "vuon_muoi_hai_qua": ("argon_site_twelve_fruit_garden", False),
    "bi_nhan_mua_gia": ("argon_site_old_buyer_lantern_stall", False),
    "giam_dinh_vo_danh": ("argon_site_nameless_appraiser_booth", False),
    "giu_cong_nguoc": ("argon_site_reverse_gate_guard_post", False),
    "lo_mo_hu_khong": ("argon_site_void_slaughterhouse", False),
    "doi_mot_lay_mot": ("argon_site_one_for_one_shrine", False),
    "cho_khong_dong_ho": ("argon_site_clockless_market_stall", False),
    "thu_vien_muc_an": ("argon_site_ink_seal_library_ruin", False),
    "hat_giong_trang": ("argon_site_white_seed_field_marker", False),
    "dong_co_khong_bong": ("argon_site_shadowless_meadow_marker", False),
    "ba_ngay_khoa": ("argon_site_three_day_locked_camp", False),
}
# collider footprint (w, h) m of the building landmarks (centred on the pivot)
SITE_FOOT = {"argon_site_forgotten_cold_store": (4.2, 2.2), "argon_site_twelve_fruit_garden": (4.0, 2.2), "argon_site_old_buyer_lantern_stall": (3.1, 2.2), "argon_site_nameless_appraiser_booth": (3.1, 2.2), "argon_site_reverse_gate_guard_post": (3.7, 2.2), "argon_site_void_slaughterhouse": (4.5, 2.2), "argon_site_one_for_one_shrine": (2.8, 2.2), "argon_site_clockless_market_stall": (3.7, 2.2), "argon_site_ink_seal_library_ruin": (4.5, 2.2), "argon_site_white_seed_field_marker": (2.8, 2.2), "argon_site_shadowless_meadow_marker": (1.7, 1.0), "argon_site_three_day_locked_camp": (4.0, 2.2)}

W, H = 72, 64
PORTAL = (36.0, 11.6)
SPAWN = (36.0, 9.1)
ARRIVAL = (36.0, 10.0, 9.5, 5.6)       # cx, cy, rx, ry
# ONE event glade (user 31 Oct: exactly 4 maps; the forest is a single map reached only through the gate/fog). Every
# event template's landmark sits at this same glade; ForestEventSites keeps only the running event's set active.
EVENT = (36.0, 38.0, 11.0, 8.0, "mix")
GLADES = {k: EVENT for k in ("kho_lanh_bo_quen", "vuon_muoi_hai_qua", "bi_nhan_mua_gia", "giam_dinh_vo_danh",
                              "giu_cong_nguoc", "lo_mo_hu_khong", "doi_mot_lay_mot", "cho_khong_dong_ho",
                              "thu_vien_muc_an", "hat_giong_trang", "dong_co_khong_bong", "ba_ngay_khoa")}
# one winding game trail from the arrival glade to the event glade
EDGES = [("A", "E")]
# sealed relic stones ('24 anchors of the great boundary'), each in a tiny clearing: x, y, sprite
RELICS = [(19.0, 33.5, "sun_stele_a.png"), (53.5, 48.0, "statue_weathered_a.png"), (36.0, 48.5, "sun_stele_c.png"),
          (3.8, 48.5, "statue_weathered_b.png")]
LM_DY = 0.6          # landmark sits slightly north of glade centre


def centre(k):
    if k == "A":
        return ARRIVAL[0], ARRIVAL[1] + 1.0
    if k == "E":
        return EVENT[0], EVENT[1]
    g = GLADES[k]
    return g[0], g[1]


def site_zone(k):
    cx, cy, rx, ry, _ = GLADES[k]
    return [round(cx - rx, 1), round(cy - ry, 1), round(2 * rx, 1), round(2 * ry, 1)]


# --------------------------------------------------------------------------- site dressing (role site_prop:*)
# (family, dx, dy) relative to the landmark pivot; all at least 1.2 m clear of the footprint
DRESS = {k: [] for k in SITE_ART}   # Argon's landmarks carry their own props
FILES = {("banner", "bi_nhan_mua_gia"): "banner_blue_f0.png"}


def make_art(mb, sid, x, y):
    art, ph = SITE_ART[sid]
    interact = {"function": "DocBangTinLore", "id": "site_" + sid}
    role = "site:" + sid
    if art.startswith("decor:"):
        f = art[6:]
        fam = f.rsplit("_", 1)[0]
        col = dict(cm.COL[fam][1])
        sh = cm.COL[fam][2]
        if sh and fam != "scarecrow":          # the shadowless meadow gets no shadow
            mb.objects.append({"sprite": f"Assets/Art/PixelMyth/Decor/shadow_ellipse_{sh}.png", "sub": "",
                               "x": round(x, 2), "y": round(y, 2), "layer": "ground", "collider": None,
                               "role": "decor", "interact": None})
        mb.add("Assets/Art/PixelMyth/Decor/" + f, x, y, cm.COL[fam][0], col, role, interact)
        foot = (col["w"], col["h"])
        cy = y + col["oy"]
    else:
        foot = SITE_FOOT[art]
        if not mb.bld.has(art):
            mb.placeholders.append(art)
        mb.add(f"Assets/Art/PixelMyth/Buildings/{art}/{art}_static.png", x, y, "actors",
               {"w": foot[0], "h": foot[1], "ox": 0.0, "oy": 0.0}, role, interact, sub=f"{art}_static_down",
               _meta={"name": art, "row": "down"})
        cy = y
    if ph:
        mb.placeholders.append(f"{role}->{art}")
    return (x - foot[0] / 2, cy - foot[1] / 2, x + foot[0] / 2, cy + foot[1] / 2)


def glade_mask(mb, g, wob_seed):
    cx, cy, rx, ry = g
    n = fbm(mb.W + 1, mb.H + 1, 5, wob_seed)
    return (((mb.cx - cx) / (rx * 0.9)) ** 2 + ((mb.cy - cy) / (ry * 0.9)) ** 2 + (n - 0.5) * 0.42) < 1


def prop_file(mb, fam, k):
    fl = FILES.get((fam, k))
    if fl:
        return fl
    for suf in ("_a_f0.png", "_a.png"):
        if f"{fam}{suf}" in mb.decor.m:
            return f"{fam}{suf}"
    return mb.d_sprite(fam)


def forest_primeval():
    mb = MapBuilder("Forest_Primeval", "Forest_Primeval", "Forest_Primeval", W, H, seed=20261031)
    rng = np.random.default_rng(3131)
    cx, cy = mb.cx, mb.cy
    mb.paint(np.ones_like(cx, bool), "f")
    allg = {"A": ARRIVAL, "E": EVENT[:4]}
    # ---- trails first (narrow dirt game trails), glades on top
    for i, (a, b) in enumerate(EDGES):
        (ax, ay), (bx, by) = centre(a), centre(b)
        mx, my = (ax + bx) / 2, (ay + by) / 2
        nx, ny = -(by - ay), (bx - ax)
        L = math.hypot(nx, ny) or 1.0
        off = rng.uniform(-2.2, 2.2)
        mid = (mx + nx / L * off, my + ny / L * off)
        mb.stroke("t", [(ax, ay), mid, (bx, by)], 1.7, wobble=0.35, seed=70 + i)
    glade_ground = {}
    for i, (k, g) in enumerate(allg.items()):
        m = glade_mask(mb, g, 300 + i)
        mb.paint(m, "m")
        glade_ground[k] = m
    for i, (rx_, ry_, _f) in enumerate(RELICS):          # relic mini-clearings + a short trail to the nearest glade
        mb.paint((((cx - rx_) / 3.3) ** 2 + ((cy - ry_) / 2.8) ** 2) < 1, "m")
        near = min(allg, key=lambda k: (centre(k)[0] - rx_) ** 2 + (centre(k)[1] - ry_) ** 2)
        mb.stroke("t", [(rx_, ry_), centre(near)], 2.2, wobble=0.25, seed=120 + i, smooth=False)
    # ground colour: golden-dry drifts in the arrival glade, lush and gold mixed in the event glade
    n1 = fbm(W + 1, H + 1, 6, 901, 2)
    m_ = mb.c == "m"
    mb.c[m_ & (n1 > 0.62)] = "g"
    mb.c[m_ & glade_ground["E"] & (n1 < 0.42)] = "d"
    mb.c[(mb.c == "g") & glade_ground["A"]] = "d"

    tm = finish_keep(mb)
    c_ = mb.c
    tcorner = (c_[1:, :-1] == "t") | (c_[1:, 1:] == "t") | (c_[:-1, :-1] == "t") | (c_[:-1, 1:] == "t")
    mb.keep |= tcorner                           # trails are lanes: no blocking decor
    # ---- portal + spawn
    mb.portals = [{"id": "Forest_Primeval->Farm", "target": "Farm", "target_portal": "Farm->Forest_Primeval",
                   "x": PORTAL[0], "y": PORTAL[1]}]
    mb.spawn = {"x": SPAWN[0], "y": SPAWN[1]}
    mb.keep[int(PORTAL[1] - 2):int(PORTAL[1] + 2), int(PORTAL[0] - 3):int(PORTAL[0] + 3)] = True   # the gate rises here
    mb.zones = [{"name": "arrival_glade", "rect": [round(ARRIVAL[0] - ARRIVAL[2], 1), round(ARRIVAL[1] - ARRIVAL[3], 1),
                                                   2 * ARRIVAL[2], 2 * ARRIVAL[3]]}]
    # ---- landmarks + dressing
    sites = {}
    for k, g in GLADES.items():
        x, y = g[0], g[1] + LM_DY
        fp = make_art(mb, k, x, y)
        sites[k] = {"pos": (x, y), "foot": fp}
        mb.zones.append({"name": "event_site:" + k, "rect": site_zone(k)})
        for fam, dx, dy in DRESS[k]:
            layer, col, sh = cm.COL[fam]
            mb.put_decor(prop_file(mb, fam, k), x + dx, y + dy, layer, col, "site_prop:" + k, shadow=sh)
    ix, iy = sites["vuon_muoi_hai_qua"]["pos"]
    for x in (-3.6, -1.2, 1.2, 3.6):
        cm.deco(mb, "glow_flower", ix + x, iy - 4.2, check=False, role="site_prop:vuon_muoi_hai_qua")
    # ---- landmark exclusion pads (>= 1 m): no tree/decor inside the padded footprint
    for k, s in sites.items():
        x0, y0, x1, y1 = s["foot"]
        mb.keep[max(int(math.floor(y0 - 1.3)), 0):int(math.ceil(y1 + 1.3)), max(int(math.floor(x0 - 1.3)), 0):int(math.ceil(x1 + 1.3))] = True
    # ---- relic stones
    for i, (rx_, ry_, f) in enumerate(RELICS):
        fam = f.rsplit("_", 1)[0]
        layer, col, sh = cm.COL[fam]
        mb.put_decor(f, rx_, ry_, layer, col, f"relic_stone:{i + 1}", shadow=sh)
        for dx, dy, ff in ((-1.8, -0.8, "ruin_cairn"), (1.9, -0.5, "boulder_m"), (0.6, -1.7, "glow_mushroom"), (-1.1, 0.9, "glow_flower")):
            cm.deco(mb, ff, rx_ + dx, ry_ + dy, check=False)
        mb.keep[int(ry_ - 1.5):int(ry_ + 1.5), int(rx_ - 2.2):int(rx_ + 2.2)] = True
    # ---- forest: dense old stand
    glades_any = np.zeros_like(cx, bool)
    for m in glade_ground.values():
        glades_any |= m
    dpath = chamfer(mb.keep)

    def keep_out(x, y):
        yi, xi = min(int(y), H - 1), min(int(x), W - 1)
        return bool(mb.keep[yi, xi] or mb.blocked[yi, xi])
    tm = mb.tile_mat()
    wood_edge(mb, tm, rng, step=1.2, keep_out=keep_out, p_skip=0.02, tall_bias=0.5, giant_p=0.34)
    wood_edge(mb, tm, rng, step=1.2, keep_out=keep_out, p_skip=0.40, tall_bias=0.3, giant_p=0.34)
    okf = lambda x, y: tm_at(tm, x, y) == "f" and not keep_out(x, y)
    for x, y in poisson(rng, okf, (0, 0, W, H), 1.6, 40000):
        k = rng.random()
        # Ecological forest zoning: West = ancient banyan/oak & ferns; East = amber canopy & mossy boulders; North = luminous fungi
        if k < 0.22:
            cm.deco(mb, "fern", x, y)
        elif k < 0.34:
            cm.deco(mb, "glow_mushroom" if y > H * 0.45 else "fern", x, y)
        elif k < 0.42:
            cm.deco(mb, "log_fallen", x, y)
        elif k < 0.50:
            cm.deco(mb, "stump", x, y)
        elif k < 0.55:
            place(mb, "boulder_l" if x > W * 0.5 else "boulder_m", x, y)
        elif k < 0.62:
            place(mb, "oak_gold" if x < W * 0.5 else "tree_amber", x, y, shadow=True)
        elif k < 0.70:
            place(mb, "shrub_dark", x, y, shadow=False)
        elif k < 0.78:
            cm.deco(mb, "glow_flower", x, y)
        elif k < 0.84:
            cm.deco(mb, "grass_tall", x, y)
        else:
            flat(mb, str(rng.choice(["grass_tuft_24x20", "grass_tuft_16x16"])), x, y, anim=True)
    # trail verges: golden grass leaning along the trails, glowing flowers
    def okv(x, y):
        yi, xi = min(int(y), H - 1), min(int(x), W - 1)
        return tm_at(tm, x, y) in "fm" and 0.9 < dpath[yi, xi] < 2.4 and not glades_any[yi, xi]
    for x, y in poisson(rng, okv, (0, 0, W, H), 1.5, 20000):
        k = rng.random()
        if k < 0.45:
            cm.deco(mb, "grass_tall", x, y)
        elif k < 0.7:
            cm.deco(mb, "glow_flower", x, y)
        else:
            flat(mb, str(rng.choice(["flower_white", "flower_purple", "flame_flower"])), x, y, anim=True)
    # glades: tufts, flowers, mist, gold trees at the rim
    for k, m in glade_ground.items():
        g = allg[k]
        inside = lambda x, y, m=m: bool(m[min(int(y), H), min(int(x), W)])
        mind = 0.85 if k in ("hat_giong_trang", "dong_co_khong_bong") else 1.25
        bnds = (g[0] - g[2] - 1, g[1] - g[3] - 1, g[0] + g[2] + 1, g[1] + g[3] + 1)
        for x, y in poisson(rng, lambda x, y: inside(x, y) and tm_at(tm, x, y) in "m", bnds, mind, 6000):
            r = rng.random()
            if k == "hat_giong_trang":
                if r < 0.8:
                    flat(mb, "flower_white", x, y)
                else:
                    flat(mb, "grass_tuft_16x16", x, y, anim=True)
            elif k == "dong_co_khong_bong":
                if (x - g[0]) ** 2 / 2.6 ** 2 + (y - g[1]) ** 2 / 2.0 ** 2 < 1:
                    continue           # the untrodden patch in the middle stays bare
                cm.deco(mb, "grass_tall" if r < 0.6 else "grass_wide", x, y)
            elif r < 0.3:
                cm.deco(mb, "grass_tall", x, y)
            elif r < 0.5:
                cm.deco(mb, "wildflowers", x, y)
            elif r < 0.65:
                cm.deco(mb, "glow_flower", x, y)
            else:
                flat(mb, str(rng.choice(["grass_tuft_24x20", "flame_flower", "flower_white", "flower_purple"])), x, y, anim=True)
        for j in range(3):
            a = rng.uniform(0, 6.28)
            cm.deco(mb, "mist", g[0] + math.cos(a) * g[2] * 0.55, g[1] + math.sin(a) * g[3] * 0.55)
        for j in range(2):
            a = rng.uniform(0, 6.28) if k != "A" else (3.6 + 2.0 * j)
            px, py = g[0] + math.cos(a) * g[2] * 0.92, g[1] + math.sin(a) * g[3] * 0.92
            place(mb, "oak_gold" if rng.random() < 0.55 else "tree_amber", px, py, shadow=True)
    for fam, x, y in (("ruin_pillar", PORTAL[0] - 7.0, PORTAL[1] + 1.8), ("ruin_cairn", PORTAL[0] + 6.5, PORTAL[1] + 2.0),
                      ("boulder_l", PORTAL[0] + 8.0, PORTAL[1] - 2.0)):
        place(mb, fam, x, y)
    rim_boulders(mb, tm, rng, depth=1.0, step=1.5, skip=[PORTAL])
    forest_wave2(mb, rng)
    # shadows belong to their site: a disabled site must not leave its shadow ellipses on the glade
    site_xy = [((o["x"], o["y"]), o["role"].split(":", 1)[1]) for o in mb.objects
               if o["role"].startswith("site:") or o["role"].startswith("site_prop:")]
    for o in mb.objects:
        if "shadow_ellipse" in o["sprite"] and o["role"] == "decor":
            for (sx, sy), sid in site_xy:
                if abs(o["x"] - sx) < 0.05 and abs(o["y"] - sy) < 0.6:
                    o["role"] = "site_prop:" + sid
                    break
    mb.sites = sites
    mb.relic_xy = [(r[0], r[1]) for r in RELICS]
    mb.glades = allg
    # Ensure 4-connected walkability along each relic trail from nearest glade to relic approach point
    for rx_, ry_, _f in RELICS:
        near = min(allg, key=lambda k: (centre(k)[0] - rx_) ** 2 + (centre(k)[1] - ry_) ** 2)
        gx, gy = centre(near)
        tx, ty = rx_, ry_ - 1.5
        steps = int(max(abs(tx - gx), abs(ty - gy)) * 3) + 1
        for s_i in range(steps + 1):
            t_u = s_i / float(steps)
            px = int(gx + (tx - gx) * t_u)
            py = int(gy + (ty - gy) * t_u)
            mb.blocked[max(0, py - 1):min(H, py + 2), max(0, px - 1):min(W, px + 2)] = False
    return mb


def forest_wave2(mb, rng):
    """Argon wave-2 forest pack: primeval giants framing the trail and the event glade, the story's golden grass
    that bows to show the way (S929) along the trail, glowing mushroom clusters and mythic herb patches."""
    def free_for(x, y, r):
        yi, xi = int(y), int(x)
        if not (2 < x < W - 2 and 2 < y < H - 2):
            return False
        y0, y1, x0, x1 = max(0, int(y - r)), min(H, int(y + r) + 1), max(0, int(x - r)), min(W, int(x + r) + 1)
        return not (mb.keep[y0:y1, x0:x1].any() or mb.blocked[y0:y1, x0:x1].any())

    def drop_near(x, y, r):          # clear smaller decor where a giant stands
        mb.objects[:] = [o for o in mb.objects if not (o["role"] == "decor" and (o["x"] - x) ** 2 + (o["y"] - y) ** 2 < r * r)]

    n = {"giant": 0, "grass": 0, "mush": 0, "herb": 0}
    # giants: just outside the arrival glade, along the trail, ringing the event glade
    ex, ey, erx, ery = EVENT[0], EVENT[1], EVENT[2], EVENT[3]
    spots = [(ARRIVAL[0] - 11.5, ARRIVAL[1] + 2.5), (ARRIVAL[0] + 11.5, ARRIVAL[1] + 3.0),
             (EVENT[0] - 9.5, (ARRIVAL[1] + EVENT[1]) / 2), (EVENT[0] + 5.5, (ARRIVAL[1] + EVENT[1]) / 2 + 2.5)]
    for a in (0.35, 1.25, 2.0, 2.9, 3.9, 5.3):
        spots.append((ex + math.cos(a) * (erx + 3.4), ey + math.sin(a) * (ery + 3.0)))
    for i, (x, y) in enumerate(spots):
        yi, xi = int(y), int(x)
        if 3 < x < W - 3 and 3 < y < H - 3 and not mb.keep[max(0, yi - 1):yi + 2, max(0, xi - 1):xi + 2].any():
            drop_near(x, y, 3.2)            # the giant replaces the conifers around it
            cm.add_building(mb, "argon_w2_forest_ancient_tree_" + "ab"[i % 2], x, y, "down", f"ancient_tree_{i + 1}", "prop",
                            foot=(1.6, 0.9))
            mb.block_rect(x - 0.8, y - 0.2, x + 0.8, y + 0.7)
            n["giant"] += 1
    # bowed golden grass beside the trail, pointing the way in
    for k in range(7):
        t = (k + 0.5) / 7
        x = ARRIVAL[0] + (EVENT[0] - ARRIVAL[0]) * t + (2.4 if k % 2 else -2.4)
        y = ARRIVAL[1] + 3 + (EVENT[1] - ery - ARRIVAL[1] - 3) * t
        mb.add("Assets/Art/PixelMyth/Buildings/argon_w2_forest_bowed_golden_grass/argon_w2_forest_bowed_golden_grass_static.png",
               x, y, "ground", None, "decor:bowed_grass", None, sub="argon_w2_forest_bowed_golden_grass_static_down",
               _meta={"name": "argon_w2_forest_bowed_golden_grass", "row": "down"})
        n["grass"] += 1
    # glowing mushrooms and mythic herbs: gatherable-looking accents on the forest floor near clearings
    for x, y in poisson(rng, lambda x, y: free_for(x, y, 0.8) and tm_at(mb.tile_mat(), x, y) in "fm", (2, 2, W - 2, H - 2), 7.0, 3000):
        name = "argon_w2_forest_luminous_mushroom_cluster" if rng.random() < 0.55 else "argon_w2_forest_mythic_herb_patch"
        mb.add(f"Assets/Art/PixelMyth/Buildings/{name}/{name}_static.png", x, y, "actors", None, "decor:forage", None,
               sub=f"{name}_static_down", _meta={"name": name, "row": "down"})
        n["mush" if "mushroom" in name else "herb"] += 1
    print("forest_wave2:", n)


# --------------------------------------------------------------------------- validation
def validate(mb):
    viol = []
    for k, s in mb.sites.items():
        x0, y0, x1, y1 = s["foot"]
        for o in mb.objects:
            r = o["role"]
            if r.startswith("site:") or r.startswith("site_prop:") or "shadow_ellipse" in o["sprite"]:
                continue
            c = o["collider"]
            if c:
                ox0, ox1 = o["x"] + c["ox"] - c["w"] / 2, o["x"] + c["ox"] + c["w"] / 2
                oy0, oy1 = o["y"] + c["oy"] - c["h"] / 2, o["y"] + c["oy"] + c["h"] / 2
            else:
                ox0 = ox1 = o["x"]
                oy0 = oy1 = o["y"]
            if ox1 > x0 - 1.0 and ox0 < x1 + 1.0 and oy1 > y0 - 1.0 and oy0 < y1 + 1.0:
                viol.append((k, o["sprite"].split("/")[-1], o["x"], o["y"]))
    cells = {}
    for o in mb.objects:
        if "shadow_ellipse" in o["sprite"]:
            continue
        key = (int(o["x"] // 8), int(o["y"] // 8))
        cells[key] = cells.get(key, 0) + 1
    tot = empty = 0
    for j in range(math.ceil(mb.H / 8)):
        for i in range(math.ceil(mb.W / 8)):
            x, y = i * 8 + 4, j * 8 + 4
            if any(((x - g[0]) / g[2]) ** 2 + ((y - g[1]) / g[3]) ** 2 < 1.2 for g in mb.glades.values()):
                continue
            tot += 1
            if cells.get((i, j), 0) == 0:
                empty += 1
    return viol, empty, tot


def main():
    mb = forest_primeval()
    write(mb, "forest", "--no-render" not in sys.argv)
    targets = {"portal": (PORTAL[0], PORTAL[1] - 1.0)}
    for k, s in mb.sites.items():
        x0, y0, x1, y1 = s["foot"]
        targets[k] = (s["pos"][0], y0 - 1.5)
    for i, (x, y) in enumerate(mb.relic_xy):
        targets[f"relic{i + 1}"] = (x, y - 1.5)
    # a target is the nearest free tile (<= 2.5 m) to its approach point, so a stray boulder cannot fake a failure
    for k, (x, y) in list(targets.items()):
        best = None
        for dy in range(-2, 3):
            for dx in range(-2, 3):
                tx, ty = int(x) + dx, int(y) + dy
                if 0 <= tx < mb.W and 0 <= ty < mb.H and not mb.blocked[ty, tx]:
                    d = dx * dx + dy * dy
                    if best is None or d < best[0]:
                        best = (d, tx + 0.5, ty + 0.5)
        if best:
            targets[k] = (best[1], best[2])
    rch, nfree, ntot = reachable(mb, (SPAWN[0], SPAWN[1]), targets)
    viol, empty, tot = validate(mb)
    print("objects", len(mb.objects), "placeholders", len(mb.placeholders))
    print("unreachable:", [k for k, v in rch.items() if not v] or "none", "| walkable %d/%d" % (nfree, ntot))
    print("overlap violations:", len(viol), viol[:8])
    print("empty 8x8 cells (non-glade): %d/%d = %.1f%%" % (empty, tot, 100.0 * empty / max(tot, 1)))
    for k, s in mb.sites.items():
        print("  site:%s (%.1f, %.1f) art=%s" % (k, s["pos"][0], s["pos"][1], SITE_ART[k][0]))


if __name__ == "__main__":
    main()
