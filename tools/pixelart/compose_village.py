"""Bờ Rìa village: a normal RECTANGULAR, spacious map like a Stardew village (user 31 Oct). The "ancient Vietnamese
citadel" quality the user asked for lives in the BUILDINGS' style (Argon's art via compose_maps_other.ART): grand
curved multi-tier roofs, weathered stone plinths, stone nghê, bronze sun discs — never in the map shape (a circular
rampart map was rejected: "làm gì có map nào có lũy").

Layout (1 tile = 1 m): the farm road enters from the south through the free-standing village gate (cổng làng); the main
street climbs to the đình court (well, notice board, bronze-drum platform) with a lotus pond and the banyan beside it;
a cross street and lanes reach seven homes standing apart, each with a yard and garden; Bà Sáu's eatery sits on the
main street; fields with the buffalo shed and the rice store lie north-west; the market road leaves north-west.

    python tools/pixelart/compose_village.py
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import compose_map as cm                      # noqa: E402
import compose_maps_other as cmo              # noqa: E402
from compose_map import MapBuilder, fbm       # noqa: E402

W, H = 120, 90
CX = 60.0
GATE_Y = 12.0                 # free-standing village gate on the main street
CROSS_Y = 30.0                # cross street
LANE_N = 70.0                 # north lane
PLAZA = (60.0, 48.0)          # đình court centre
POND = (42.0, 56.0, 6.0, 3.2)
EATERY = (71.0, 21.5)
MARKET_X = 22.0
HOMES = {"nha_man": (19.0, 34.5), "nha_tam_va": (41.0, 34.5), "nha_dung": (22.0, 54.0), "nha_cuc": (81.0, 34.5),
         "nha_le": (102.0, 34.5), "nha_anh_ca": (99.0, 54.0), "nha_ong_ton": (74.0, 73.5)}

# Argon's delivered Đông Sơn / Đại Việt village landmarks (zero placeholders)
cmo.ART.update({
    "cong_thanh": ("argon_village_cong_thanh", False),
    "trong_dong_dai": ("argon_village_trong_dong_dai", False),
    "mieu_co": ("argon_village_mieu_co", False),
    "bia_rua": ("argon_village_bia_rua", False),
    "dinh_lang": ("argon_village_dinh_lang", False),
})


def open_passage(mb, x, y, half_w, half_h):
    """Keep a walk-through gate: drop colliders inside the passage, unblock its tiles, keep two piers."""
    for o in mb.objects:
        if o.get("collider") and abs(o["x"] - x) < half_w + 1.0 and abs(o["y"] - y) < half_h:
            o["collider"] = None
    x0, x1 = int(math.floor(x - half_w)), int(math.ceil(x + half_w))
    y0, y1 = int(math.floor(y - half_h)), int(math.ceil(y + half_h))
    mb.blocked[max(y0, 0):y1, max(x0, 0):x1] = False
    for side in (-1, 1):
        mb.block_rect(x + side * (half_w + 0.2) - 0.6, y - 1.0, x + side * (half_w + 0.2) + 0.6, y + 1.0)


def village():
    mb = MapBuilder("Hub_Village", "Hub_Village", "Hub_Village", W, H, seed=20261103)
    cmo.init_plan(mb, "s")
    cmo.V_EXCL[:] = []
    rng = np.random.default_rng(1031)
    X, Y = mb.cx, mb.cy

    # ---- ground: golden meadow, forest belt at the rim
    edge = np.minimum.reduce([X, Y, W - X, H - Y])
    n_f = fbm(W + 1, H + 1, 9, 51, 3)
    mb.paint((edge < 3.5 + 7 * (n_f - 0.3)) & (n_f > 0.35), "f")
    cm.ground_variation(mb, 1101, lush=0.62, dry=0.66, dirt=0.97)

    # ---- streets (compact Vietnamese rural lanes: road + stone <= 8.5%)
    mb.stroke("p", [(CX, 0.5), (CX, PLAZA[1] - 5.5)], 3.2, 0.12, 3, smooth=False)          # main street
    mb.stroke("p", [(8, CROSS_Y), (W - 8, CROSS_Y)], 2.4, 0.15, 4, smooth=False)            # cross street
    mb.stroke("p", [(30, CROSS_Y), (30, LANE_N)], 2.0, 0.18, 5, smooth=False)              # west lane
    mb.stroke("p", [(92, CROSS_Y), (92, LANE_N)], 2.0, 0.18, 6, smooth=False)              # east lane
    mb.stroke("p", [(30, LANE_N), (92, LANE_N)], 2.0, 0.18, 7, smooth=False)               # north lane
    mb.stroke("p", [(30, LANE_N), (26, LANE_N + 8), (MARKET_X, H - 0.5)], 2.4, 0.22, 8)     # road to the market
    mb.stroke("p", [(CX, PLAZA[1] + 5.5), (CX, LANE_N)], 2.2, 0.15, 9, smooth=False)        # behind the đình
    mb.paint(cm._stone_blob(mb, PLAZA[0], PLAZA[1], 9.2, 5.6, 903, 0.22), "s")              # đình court
    mb.paint(cm._stone_blob(mb, CX, GATE_Y + 3.5, 4.0, 2.0, 901, 0.28), "s")                 # gate forecourt
    pond = cm._stone_blob(mb, POND[0], POND[1], POND[2], POND[3], 905, 0.4)
    mb.paint(pond, "w")                                                                      # lotus pond
    tm = mb.tile_mat()
    mb.keep |= (tm == "p") | (tm == "s")
    mb.blocked |= (tm == "w")

    P = cmo.put
    # village gate on the road, stele and xe-ôm stop just outside
    P(mb, "cong_thanh", CX, GATE_Y, "cong_thanh", "prop", apron=False)
    open_passage(mb, CX, GATE_Y + 4.5, 2.4, 6.0)
    P(mb, "bia_sign", CX - 8.0, 6.5, "bia_sign", "prop", apron=False)
    P(mb, "xe_om_stop", CX + 8.5, 6.0, "xe_om_stop", "prop", apron=False)
    # đình court + sacred stele & shrine by the lotus pond
    P(mb, "nha_van_hoa", CX, PLAZA[1] + 8.5, "nha_van_hoa", interact={"function": "VaoNhaScene", "id": "nha_van_hoa"})
    P(mb, "gieng_chung", PLAZA[0] - 6.5, PLAZA[1] - 1.5, "gieng_chung", "prop", apron=False)
    P(mb, "notice_board", PLAZA[0] + 6.5, PLAZA[1] - 2.5, "notice_board", "prop",
      {"function": "DocBangTinLore", "id": "bang_tin_lang"}, apron=False)
    P(mb, "trong_dong_dai", PLAZA[0] + 17.0, PLAZA[1] + 1.0, "trong_dong_dai", "prop", apron=False)
    P(mb, "bia_rua", PLAZA[0] - 12.5, PLAZA[1] - 2.0, "bia_rua", "prop", apron=False)
    P(mb, "mieu_co", POND[0], POND[1] + 6.8, "mieu_co", "prop", apron=False)
    P(mb, "tree_banyan", POND[0] - 10.0, POND[1] + 3.0, "cay_mua_mua_dung_duoi", "prop", apron=False)
    P(mb, "tree_tamarind", 88.0, 52.0, "cay_mua_nao_cung_dung_duoi", "prop", apron=False)
    # Bà Sáu's eatery on the main street, rice-drying mats beside it
    P(mb, "quan_ba_sau", EATERY[0], EATERY[1], "quan_ba_sau", interact={"function": "GiaoDichDanLang", "id": "quan_ba_sau"})
    for x, y in ((EATERY[0] + 12, EATERY[1] - 1), (EATERY[0] + 19, EATERY[1] - 1), (EATERY[0] + 12, EATERY[1] - 6)):
        mb.add("Assets/Art/PixelMyth/Buildings/argon_village_rice_drying_yard/argon_village_rice_drying_yard_static.png",
               x, y, "ground", None, "decor:rice_mat", None, sub="argon_village_rice_drying_yard_static_down",
               _meta={"name": "argon_village_rice_drying_yard", "row": "down"})
    # seven homes in enclosed rural courtyard compounds, each with a front gate, yard, water jar, and rear garden
    home_pos = {}
    for key, (x, y) in HOMES.items():
        P(mb, key, x, y, key, interact={"function": "VaoNhaScene", "id": key})
        home_pos[key] = (x, y)
    # fields north-west: buffalo shed, rice store, haystacks
    P(mb, "chuong_trau", 14.0, LANE_N + 6.0, "chuong_trau")
    P(mb, "kho_lua", 44.0, LANE_N + 7.0, "kho_lua")
    for _ in range(3):
        cmo.vplace(mb, "haystack", 22 + rng.uniform(-5, 5), LANE_N + 10 + rng.uniform(-2, 2), 0.6)

    # ---- enclosed rural courtyard compounds, gardens, plants, hedges
    for key, (hx, hy) in home_pos.items():
        side = 1 if rng.random() < 0.5 else -1
        cmo.vplace(mb, str(rng.choice(["banana_a", "banana_b", "areca_a", "areca_b"])), hx + side * 5.2, hy + 0.6, 0.6)
        cmo.vplace(mb, str(rng.choice(["areca_a", "areca_b", "banana_a"])), hx - side * 5.6, hy + 1.2, 0.6)
        cmo.vplace(mb, str(rng.choice(["jar_a", "jar_b"])), hx - side * 4.0, hy - 0.8, 0.3)
        cmo.veg_plot(mb, hx - 4.5, hx + 4.5, hy + 6.5, hy + 10.5, 3, 2, fence=True)
        # Enclose each family compound with a front courtyard fence & gate opening
        fy = hy - 2.8
        cmo.t_fence(mb, (hx - 6.2, fy), (hx + 6.2, fy), gap=(hx - 1.4, hx + 1.4))
        cmo.t_fence(mb, (hx - 6.2, fy), (hx - 6.2, hy + 5.8))
        cmo.t_fence(mb, (hx + 6.2, fy), (hx + 6.2, hy + 5.8))
    for p0, p1 in (((10, CROSS_Y + 3), (26, CROSS_Y + 3)), ((96, CROSS_Y + 3), (110, CROSS_Y + 3)),
                   ((33, LANE_N - 3), (54, LANE_N - 3)), ((66, LANE_N - 3), (89, LANE_N - 3)),
                   ((10, 14), (CX - 12, 14)), ((CX + 12, 14), (110, 14))):
        cmo.hedge(mb, rng, p0, p1, step=2.2, skip=0.15)
    # meadow life everywhere else: open, never bare
    tm = mb.tile_mat()
    ok = lambda x, y: tm[min(int(y), H - 1), min(int(x), W - 1)] == "m" and mb.free(x, y, 0.4)
    for x, y in cm.poisson(rng, ok, (1, 1, W - 1, H - 1), 2.0, 30000):
        r = rng.random()
        if r < 0.45:
            cm.flat(mb, "grass_tuft_24x20" if rng.random() < 0.5 else "grass_tuft_16x16", x, y, anim=True, check=False)
        elif r < 0.75:
            cm.deco(mb, "wildflowers", x, y, check=False, collider=False)
        elif r < 0.9:
            cm.deco(mb, "grass_tall", x, y, check=False, collider=False)
        else:
            cm.deco(mb, "boulder_s", x, y, check=False)
    for _ in range(18):                                       # lotus pads on the pond
        a, rr = rng.uniform(0, 6.283), np.sqrt(rng.random()) * 0.75
        cm.deco(mb, "lilypad", POND[0] + math.cos(a) * POND[2] * rr, POND[1] + math.sin(a) * POND[3] * rr, check=False)
    for x, y in cm.poisson(rng, lambda x, y: tm[min(int(y), H - 1), min(int(x), W - 1)] == "f" and mb.free(x, y, 0.6),
                           (0, 0, W, H), 2.6, 20000):
        # Ecological tree stands by quadrant (West broadleaf oak, East amber canopy, North highland conifer)
        fam = "oak_gold" if x < W * 0.42 else ("tree_amber" if x > W * 0.58 and y < H * 0.72 else "conifer_tall")
        cm.place(mb, fam, x, y)

    # ---- portals, spawn, NPCs, zones
    mb.portals = [
        {"id": "Hub_Village->Farm", "target": "Farm", "target_portal": "Farm->Hub_Village", "x": CX, "y": 1.2},
        {"id": "Hub_Village->MarketTown", "target": "MarketTown", "target_portal": "MarketTown->Hub_Village",
         "x": MARKET_X, "y": H - 1.2},
    ]
    mb.spawn = {"x": CX, "y": 3.5}
    eatery_door = (EATERY[0], EATERY[1] - 2.6)
    well = (PLAZA[0] - 6.5, PLAZA[1] - 4.0)
    npcs = [("ba_sau", "Bà Sáu", eatery_door, "quan_ba_sau", [eatery_door, (eatery_door[0] - 5, eatery_door[1]), well], "S436")]
    names = {"nha_man": ("man", "Mận", "S1051"), "nha_tam_va": ("tam_va", "Tám Vá", "S1034"), "nha_dung": ("dung", "Dung", "S1035"),
             "nha_cuc": ("cuc", "Cúc", "S1036"), "nha_le": ("le", "Lệ", "S1037"), "nha_anh_ca": ("anh_ca", "anh Ca", "S1038"),
             "nha_ong_ton": ("ong_ton", "ông Tồn", "S1039")}
    for key, (hx, hy) in home_pos.items():
        nid, nm, src = names[key]
        door = (hx, hy - 2.4)
        npcs.append((nid, nm, door, key, [door, (door[0] + 3, door[1] - 1.5), well], src))
    npcs.append(("anh_tu", "anh Tư", (CX + 8.5, 3.6), None, [(CX + 8.5, 3.6), (CX + 5.5, 4.0)], "S188"))
    mb.npcs = []
    for nid, nm, pt, home, route, src in npcs:
        route = [r for r in route if mb.free(r[0], r[1], 0.3, allow_keep=True)] or [pt]
        mb.npcs.append({"id": nid, "name": nm, "x": round(route[0][0], 2), "y": round(route[0][1], 2), "home": home,
                        "route": [[round(a, 2), round(b, 2)] for a, b in route], "greeting_src": src})
    mb.zones = [{"name": "village_green", "rect": [PLAZA[0] - 12, PLAZA[1] - 7.5, 24, 15]}]
    mb.landmarks = {"well": list(well), "eatery": list(eatery_door), "gate": [CX, GATE_Y + 3]}
    print(f"village: buildings {len(mb.fps)}, npcs {len(mb.npcs)}, problems {mb.problems}")
    return mb


def main():
    mb = village()
    cmo.write(mb, "village", "--no-render" not in sys.argv)
    tg = {p["id"]: (p["x"], p["y"]) for p in mb.portals}
    tg.update({k: tuple(v) for k, v in mb.landmarks.items()})
    rch, nfree, ntot = cmo.reachable(mb, (mb.spawn["x"], mb.spawn["y"]), tg)
    area = W * H / max(1, len([f for f in mb.fps if not f.get("decor")]))
    print("reach", rch, "walkable reachable %d/%d" % (nfree, ntot), "m² per building %.0f" % area)


if __name__ == "__main__":
    main()
