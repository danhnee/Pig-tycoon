import bpy
import bmesh
import json
import os
import time
import math
import struct
import sys
import mathutils
import numpy as np
import cv2
from PIL import Image

_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
if _SCRIPT_DIR not in sys.path:
    sys.path.insert(0, _SCRIPT_DIR)
from observatory_geometry import build_observatory, meshbin_bytes
from observatory_blender import create_observatory_material
from map_surface_contract import (WORLD_X_MAX, WORLD_Y_MAX, ISLAND_RX, ISLAND_RY,
                                  strip_uv, eval_river_center_and_width, eval_road_mask,
                                  eval_pond_distances, sample_ground_z, island_signed_distance)
OUT_DIR = r"C:\Users\Admin\.gemini\antigravity\brain\2526dd2a-9e91-4dbf-a3dc-12d822a774b4\scratch"
ARTIFACT_DIR = r"C:\Users\Admin\.gemini\antigravity\brain\2526dd2a-9e91-4dbf-a3dc-12d822a774b4"
KIT_ROOT = r"C:\Users\Admin\.gemini\antigravity\brain\2526dd2a-9e91-4dbf-a3dc-12d822a774b4\scratch\external_3d_kits"
_LOCAL_NPZ = os.path.join(_SCRIPT_DIR, "quaternius_nature_cache.npz")
QUAT_NPZ = _LOCAL_NPZ if os.path.exists(_LOCAL_NPZ) else os.path.join(OUT_DIR, "quaternius_nature_cache.npz")
_TARGET_ROOT = os.environ.get("PIGTYCOON_TARGET_ROOT", r"F:\Unity\Pig tycoon")
UNITY_TEX_DIR = os.path.join(_TARGET_ROOT, r"Assets\Textures\FarmMain3D")
UNITY_MODEL_DIR = os.path.join(_TARGET_ROOT, r"Assets\Models\FarmMain3D")
BLEND_DIR = os.path.join(_TARGET_ROOT, r"ArtSource\Blender_3D_Map")

os.makedirs(UNITY_TEX_DIR, exist_ok=True)
os.makedirs(UNITY_MODEL_DIR, exist_ok=True)
os.makedirs(BLEND_DIR, exist_ok=True)

ZONE_NAMES = {
    1: "Zone01_Terrain_GoldenMeadow_And_Paths",
    2: "Zone02_Diorama_Cutaway_RockRim_And_Base",
    3: "Zone03_Water_River_Waterfalls_And_3Ponds",
    4: "Zone04_Building_KhuChuongTrai_Farmstead",
    5: "Zone05_Building_NhaNghienCuu_ResearchObservatory",
    6: "Zone06_Building_NhaCuongHoa_And_KhuNPC",
    7: "Zone07_Landmark_TramThongTin_InfoBeacon",
    8: "Zone08_Vista_StoneBridge_And_HarborTown",
    9: "Zone09_Vista_TwilightCastle_And_WaterfallCliff",
    10: "Zone10_Vista_AlpineMountains_And_ForestRidge",
    11: "Zone11_Props_AncientRuins_And_GoldenFlora",
}


SUN_DIR = np.array([0.68, -0.38, 0.62], dtype=np.float32)
SUN_DIR /= np.linalg.norm(SUN_DIR)

_QUAT_CACHE = np.load(QUAT_NPZ) if os.path.exists(QUAT_NPZ) else None
_KIT_PALETTES = {}
_OBJ_TEMPLATE_CACHE = {}




def multi_scale_organic_noise(h, w, seed=42, scales=(64, 24, 9, 3), weights=(0.45, 0.30, 0.16, 0.09)):
    """
    Generates zero-moiré, non-periodic painterly organic texture noise using multi-octave
    Gaussian-blurred random fields.
    """
    rng = np.random.RandomState(seed)
    acc = np.zeros((h, w), dtype=np.float32)
    for sc, wt in zip(scales, weights):
        gh = max(4, h // sc)
        gw = max(4, w // sc)
        base = rng.randn(gh, gw).astype(np.float32)
        up = cv2.resize(base, (w, h), interpolation=cv2.INTER_CUBIC)
        ksize = int(max(3, (sc // 2) * 2 + 1))
        up = cv2.GaussianBlur(up, (ksize, ksize), 0)
        std = float(np.std(up)) + 1e-6
        acc += wt * (up / std)
    return np.clip(acc * 0.5, -1.0, 1.0)












# =============================================================================
# OPEN-SOURCE DESIGNER 3D KIT LOADER (`Quaternius` FBX Cache + `KayKit` / `Kenney` OBJ)
# =============================================================================
def place_quaternius_asset(mb, key_name, cx, cy, z0, target_size, yaw=0.0, strip_idx=0, u0=0.03, u1=0.49, v0=0.05, v1=0.86, sink_z=0.08):
    if _QUAT_CACHE is None or f"{key_name}_v" not in _QUAT_CACHE:
        return
    v_arr = _QUAT_CACHE[f"{key_name}_v"]
    size_xyz = _QUAT_CACHE[f"{key_name}_size"]
    f_arr = _QUAT_CACHE[f"{key_name}_f"]

    sc = np.array([
        target_size[0] / max(float(size_xyz[0]), 1e-4),
        target_size[1] / max(float(size_xyz[1]), 1e-4),
        target_size[2] / max(float(size_xyz[2]), 1e-4),
    ], dtype=np.float32)
    v_world = rot_z(v_arr * sc[None, :], yaw) + np.array([cx, cy, z0 - sink_z], dtype=np.float32)
    z_min_w = float(z0 - sink_z)
    z_max_w = float(z0 - sink_z + target_size[2])
    mb.add_faceted_normal_mapped(v_world, f_arr, strip_idx, u0=u0, u1=u1, v0=v0, v1=v1, z_min=z_min_w, z_max=z_max_w)


def _get_kit_palette(kit_type):
    if kit_type in _KIT_PALETTES:
        return _KIT_PALETTES[kit_type]
    if kit_type == "kaykit":
        p = os.path.join(KIT_ROOT, r"KayKit-Medieval-Hexagon-Pack\addons\kaykit_medieval_hexagon_pack\Textures\hexagons_medieval.png")
    elif kit_type == "fantasy_town":
        p = os.path.join(KIT_ROOT, r"fantasy_town_kit\Models\OBJ format\Textures\colormap.png")
    elif kit_type == "castle":
        p = os.path.join(KIT_ROOT, r"castle_kit\Models\OBJ format\Textures\colormap.png")
    else:
        return None
    if os.path.exists(p):
        img = np.asarray(Image.open(p).convert("RGB"), dtype=np.float32)
        _KIT_PALETTES[kit_type] = img
        return img
    return None


def _parse_mtl_colors(mtl_path):
    mats = {}
    if not os.path.exists(mtl_path):
        return mats
    cur_mat = None
    with open(mtl_path, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            s = line.strip()
            if s.startswith("newmtl "):
                cur_mat = s[7:].strip()
            elif s.startswith("Kd ") and cur_mat is not None:
                parts = s.split()
                if len(parts) >= 4:
                    mats[cur_mat] = (
                        float(parts[1]) * 255.0,
                        float(parts[2]) * 255.0,
                        float(parts[3]) * 255.0,
                    )
    return mats


def _classify_triangle_to_strip(r, g, b, mtl_name, kit_type, roof_override=None):
    m_low = (mtl_name or "").lower()
    if "flower_yellow" in m_low or "yellow" in m_low:
        return (7, 0.56, 0.69, 0.25, 0.98)
    if "flower_red" in m_low or ("red" in m_low and kit_type == "nature"):
        return (7, 0.71, 0.84, 0.25, 0.98)
    if "flower_purple" in m_low or "purple" in m_low:
        return (7, 0.86, 0.98, 0.25, 0.98)
    if "leaf" in m_low or "grass" in m_low or "bush" in m_low:
        return (7, 0.36, 0.53, 0.12, 0.92)
    if "trunk" in m_low or "wood" in m_low or "bark" in m_low or "plank" in m_low or "brown" in m_low:
        return (1, 0.70, 0.96, 0.08, 0.92)
    if "statue" in m_low or "obelisk" in m_low or "column" in m_low or roof_override == "ruin":
        return (0, 0.54, 0.96, 0.10, 0.94)
    if "stone" in m_low or "rock" in m_low or "cliff" in m_low or roof_override == "cliff_rock":
        return (0, 0.03, 0.49, 0.05, 0.86)
    if roof_override == "limestone":
        return (6, 0.74, 0.98, 0.12, 0.96)
    if r > 205 and g > 155 and b < 115:
        return (1, 0.40, 0.66, 0.12, 0.94)
    if r > 155 and r > g * 1.42 and r > b * 1.45:
        if roof_override == "slate_blue":
            return (5, 0.38, 0.68, 0.08, 0.96)
        return (5, 0.03, 0.34, 0.08, 0.96)
    if b > 105 and b > r * 1.18:
        return (5, 0.38, 0.68, 0.08, 0.96)
    if r > 175 and g > 160 and b > 135:
        return (1, 0.03, 0.35, 0.22, 0.94)
    if abs(r - g) < 28 and abs(g - b) < 35:
        return (0, 0.54, 0.96, 0.10, 0.92)
    return (1, 0.70, 0.96, 0.08, 0.92)


def load_obj_template(rel_path, roof_override=None):
    cache_key = (rel_path, roof_override)
    if cache_key in _OBJ_TEMPLATE_CACHE:
        return _OBJ_TEMPLATE_CACHE[cache_key]

    full_path = os.path.join(KIT_ROOT, rel_path)
    if not os.path.exists(full_path):
        raise FileNotFoundError(f"Missing kit asset: {full_path}")

    rel_low = rel_path.lower()
    if "kaykit" in rel_low:
        kit_type = "kaykit"
    elif "fantasy_town" in rel_low:
        kit_type = "fantasy_town"
    elif "castle_kit" in rel_low:
        kit_type = "castle"
    else:
        kit_type = "nature"

    pal_img = _get_kit_palette(kit_type)
    mtl_colors = _parse_mtl_colors(full_path[:-4] + ".mtl")

    raw_v = []
    raw_vt = []
    groups = {}
    cur_mtl = "default"

    with open(full_path, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            if line.startswith("v "):
                p = line.split()
                vx, vy, vz = float(p[1]), float(p[2]), float(p[3])
                raw_v.append((vx, -vz, vy))
            elif line.startswith("vt "):
                p = line.split()
                raw_vt.append((float(p[1]), float(p[2])))
            elif line.startswith("usemtl "):
                cur_mtl = line[7:].strip()
            elif line.startswith("f "):
                parts = line.split()[1:]
                v_ids = []
                vt_ids = []
                for tok in parts:
                    sp = tok.split("/")
                    v_ids.append(int(sp[0]) - 1)
                    if len(sp) > 1 and sp[1]:
                        vt_ids.append(int(sp[1]) - 1)
                r, g, b = 180.0, 165.0, 140.0
                if pal_img is not None and vt_ids and len(raw_vt) > 0:
                    us = [raw_vt[i][0] for i in vt_ids if 0 <= i < len(raw_vt)]
                    vs = [raw_vt[i][1] for i in vt_ids if 0 <= i < len(raw_vt)]
                    if us and vs:
                        u_m = float(np.clip(sum(us) / len(us), 0.0, 1.0))
                        v_m = float(np.clip(1.0 - (sum(vs) / len(vs)), 0.0, 1.0))
                        ph, pw = pal_img.shape[:2]
                        px = min(pw - 1, max(0, int(round(u_m * (pw - 1)))))
                        py = min(ph - 1, max(0, int(round(v_m * (ph - 1)))))
                        rgb = pal_img[py, px]
                        r, g, b = float(rgb[0]), float(rgb[1]), float(rgb[2])
                elif cur_mtl in mtl_colors:
                    r, g, b = mtl_colors[cur_mtl]

                mat_key = _classify_triangle_to_strip(r, g, b, cur_mtl, kit_type, roof_override=roof_override)
                if mat_key not in groups:
                    groups[mat_key] = []
                for i in range(1, len(v_ids) - 1):
                    groups[mat_key].append((v_ids[0], v_ids[i], v_ids[i + 1]))

    v_arr = np.array(raw_v, dtype=np.float32)
    if len(v_arr) > 0:
        min_xyz = np.min(v_arr, axis=0)
        max_xyz = np.max(v_arr, axis=0)
        center_xy = 0.5 * (min_xyz[:2] + max_xyz[:2])
        v_arr[:, 0] -= center_xy[0]
        v_arr[:, 1] -= center_xy[1]
        v_arr[:, 2] -= min_xyz[2]
        size_xyz = max_xyz - min_xyz
    else:
        size_xyz = np.ones(3, dtype=np.float32)

    compiled_groups = [
        (mat_key, np.array(tris, dtype=np.int32))
        for mat_key, tris in groups.items()
        if len(tris) > 0
    ]
    res = (v_arr, size_xyz, compiled_groups)
    _OBJ_TEMPLATE_CACHE[cache_key] = res
    return res


def place_kit_asset(mb, rel_path, cx, cy, z0, target_size, yaw=0.0, roof_override=None, sink_z=0.08):
    v_arr, size_xyz, compiled_groups = load_obj_template(rel_path, roof_override=roof_override)
    if len(v_arr) == 0:
        return
    sc = np.array([
        target_size[0] / max(float(size_xyz[0]), 1e-4),
        target_size[1] / max(float(size_xyz[1]), 1e-4),
        target_size[2] / max(float(size_xyz[2]), 1e-4),
    ], dtype=np.float32)
    v_world = rot_z(v_arr * sc[None, :], yaw) + np.array([cx, cy, z0 - sink_z], dtype=np.float32)
    z_min_w = float(z0 - sink_z)
    z_max_w = float(z0 - sink_z + target_size[2])

    for (strip_idx, u0, u1, v0, v1), f_arr in compiled_groups:
        mb.add_faceted_normal_mapped(v_world, f_arr, strip_idx, u0=u0, u1=u1, v0=v0, v1=v1, z_min=z_min_w, z_max=z_max_w)


def build_4k_master_atlas_and_terrain_data(ao_stamps, light_stamps):
    print("[2/6] Generating 4K Albedo, 4K Normal & 4K Selective Emission Maps...")
    W, H = 4096, 4096
    H_TERRAIN = 3072
    H_STRIPS = 1024

    atlas = np.zeros((H, W, 3), dtype=np.uint8)
    emission = np.zeros((H, W, 3), dtype=np.uint8)
    height_for_normal = np.zeros((H, W), dtype=np.float32)

    # -------------------------------------------------------------------------
    # A. TOP 75% (4096 x 3072): Golden-Hour Meadow Terrain with Baked Contact AO & Light Spill
    # -------------------------------------------------------------------------
    gy, gx = np.mgrid[0:H_TERRAIN, 0:W].astype(np.float32)
    u_norm = gx / float(W - 1)
    v_norm = 1.0 - (gy / float(H_TERRAIN - 1))

    xw = (u_norm * 2.0 - 1.0) * WORLD_X_MAX
    yw = (v_norm * 2.0 - 1.0) * WORLD_Y_MAX

    n_macro = multi_scale_organic_noise(H_TERRAIN, W, seed=101, scales=(128, 48, 18), weights=(0.55, 0.30, 0.15))
    n_micro = multi_scale_organic_noise(H_TERRAIN, W, seed=202, scales=(12, 5, 2), weights=(0.50, 0.35, 0.15))
    diag_sun = np.sin((xw * 0.26 + yw * 0.44)) * 0.5 + 0.5

    r_grass = 224.0 + 26.0 * diag_sun + 24.0 * n_macro + 14.0 * n_micro
    g_grass = 158.0 + 32.0 * diag_sun + 20.0 * n_macro + 10.0 * n_micro
    b_grass = 32.0 + 14.0 * diag_sun + 8.0 * n_macro + 4.0 * n_micro

    forest_prox = np.clip((yw - 5.8) / 9.5, 0.0, 1.0)
    r_grass = r_grass * (1.0 - 0.34 * forest_prox) + 84.0 * (0.34 * forest_prox)
    g_grass = g_grass * (1.0 - 0.22 * forest_prox) + 114.0 * (0.22 * forest_prox)
    b_grass = b_grass * (1.0 - 0.18 * forest_prox) + 38.0 * (0.18 * forest_prox)

    road_w = eval_road_mask(xw, yw)
    r_road = 208.0 + 18.0 * n_micro + 10.0 * n_macro
    g_road = 152.0 + 14.0 * n_micro + 8.0 * n_macro
    b_road = 88.0 + 10.0 * n_micro + 6.0 * n_macro

    road_blend = np.clip(road_w * 0.84, 0.0, 0.84)
    r_ter = r_grass * (1.0 - road_blend) + r_road * road_blend
    g_ter = g_grass * (1.0 - road_blend) + g_road * road_blend
    b_ter = b_grass * (1.0 - road_blend) + b_road * road_blend

    p1_d, p2_d, p3_d = eval_pond_distances(xw, yw)
    riv_cx, riv_hw = eval_river_center_and_width(yw)
    riv_d = np.where((yw <= 12.4) & (yw >= -7.8), np.abs(xw - riv_cx) / np.maximum(riv_hw, 1.0), 99.0)
    water_dist = np.minimum.reduce([p1_d, p2_d, p3_d, riv_d])
    bank_mask = np.clip((1.12 - water_dist) / 0.14, 0.0, 1.0)
    r_ter = r_ter * (1.0 - 0.55 * bank_mask) + 148.0 * (0.55 * bank_mask)
    g_ter = g_ter * (1.0 - 0.50 * bank_mask) + 122.0 * (0.50 * bank_mask)
    b_ter = b_ter * (1.0 - 0.45 * bank_mask) + 72.0 * (0.45 * bank_mask)

    GW_AO, GH_AO = 512, 384
    ao_grid = np.zeros((GH_AO, GW_AO), dtype=np.float32)
    lt_grid = np.zeros((GH_AO, GW_AO), dtype=np.float32)
    gy_a, gx_a = np.mgrid[0:GH_AO, 0:GW_AO].astype(np.float32)
    xw_a = ((gx_a / float(GW_AO - 1)) * 2.0 - 1.0) * WORLD_X_MAX
    yw_a = ((1.0 - gy_a / float(GH_AO - 1)) * 2.0 - 1.0) * WORLD_Y_MAX

    for sx_o, sy_o, s_rad, s_str in ao_stamps:
        d2 = ((xw_a - sx_o) ** 2 + (yw_a - sy_o) ** 2) / max(0.15, s_rad * s_rad)
        ao_grid = np.maximum(ao_grid, s_str * np.exp(-d2 * 1.45))

    for lx_o, ly_o, l_rad, l_str in light_stamps:
        d2 = ((xw_a - lx_o) ** 2 + (yw_a - ly_o) ** 2) / max(0.15, l_rad * l_rad)
        lt_grid = np.maximum(lt_grid, l_str * np.exp(-d2 * 1.35))

    ao_full = cv2.resize(ao_grid, (W, H_TERRAIN), interpolation=cv2.INTER_LINEAR)
    lt_full = cv2.resize(lt_grid, (W, H_TERRAIN), interpolation=cv2.INTER_LINEAR)

    ao_f = np.clip(ao_full, 0.0, 0.72)
    r_ter = r_ter * (1.0 - ao_f) + 46.0 * ao_f
    g_ter = g_ter * (1.0 - ao_f) + 40.0 * ao_f
    b_ter = b_ter * (1.0 - ao_f) + 14.0 * ao_f

    lt_f = np.clip(lt_full, 0.0, 0.68)
    r_ter = np.clip(r_ter + 68.0 * lt_f, 0, 255)
    g_ter = np.clip(g_ter + 44.0 * lt_f, 0, 255)
    b_ter = np.clip(b_ter + 12.0 * lt_f, 0, 255)

    atlas[0:H_TERRAIN, :, 0] = np.clip(r_ter, 0, 255).astype(np.uint8)
    atlas[0:H_TERRAIN, :, 1] = np.clip(g_ter, 0, 255).astype(np.uint8)
    atlas[0:H_TERRAIN, :, 2] = np.clip(b_ter, 0, 255).astype(np.uint8)

    emission[0:H_TERRAIN, :, 0] = np.clip(lt_f * 85.0, 0, 255).astype(np.uint8)
    emission[0:H_TERRAIN, :, 1] = np.clip(lt_f * 52.0, 0, 255).astype(np.uint8)
    emission[0:H_TERRAIN, :, 2] = np.clip(lt_f * 10.0, 0, 255).astype(np.uint8)

    height_for_normal[0:H_TERRAIN, :] = 0.5 + 0.28 * n_micro + 0.18 * n_macro - 0.20 * road_blend

    # -------------------------------------------------------------------------
    # B. BOTTOM 25% (Rows 3072..4095, 8 Painterly Strips of 512 x 1024):
    # -------------------------------------------------------------------------
    sy, sx = np.mgrid[0:H_STRIPS, 0:512].astype(np.float32)
    u_s = sx / 511.0
    v_s = 1.0 - (sy / float(H_STRIPS - 1))

    n_strip_a = multi_scale_organic_noise(H_STRIPS, 512, seed=303, scales=(48, 18, 7, 3), weights=(0.45, 0.30, 0.16, 0.09))
    n_strip_b = multi_scale_organic_noise(H_STRIPS, 512, seed=404, scales=(32, 12, 5, 2), weights=(0.40, 0.35, 0.16, 0.09))

    # Strip 0 (0..512): Natural Sculpted Sandstone Cliff Boulders (`u_s < 0.52`) & Carved Spiral-Rune Ruin Stone (`u_s >= 0.52`)
    s0 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    cliff_u = np.clip(u_s / 0.52, 0.0, 1.0)
    cliff_lit = np.clip(0.18 + 0.58 * cliff_u + 0.24 * v_s + 0.14 * n_strip_a, 0.0, 1.0)
    r_cliff = 48.0 + 174.0 * (cliff_lit ** 0.84)
    g_cliff = 30.0 + 124.0 * (cliff_lit ** 0.92)
    b_cliff = 16.0 + 72.0 * (cliff_lit ** 1.05)

    ruin_u = np.clip((u_s - 0.52) / 0.48, 0.0, 1.0)
    spiral_r = np.sqrt((ruin_u - 0.5) ** 2 + ((v_s - 0.55) * 1.35) ** 2)
    spiral_ang = np.arctan2((v_s - 0.55) * 1.35, ruin_u - 0.5)
    spiral_groove = np.where(spiral_r < 0.34, np.exp(-((np.sin(spiral_r * 34.0 + spiral_ang * 2.0)) ** 2) / 0.09), 0.0)
    ruin_lit = np.clip(0.36 + 0.38 * ruin_u + 0.26 * v_s + 0.12 * n_strip_b - 0.32 * spiral_groove, 0.0, 1.0)
    r_ruin = 78.0 + 146.0 * (ruin_lit ** 0.85)
    g_ruin = 62.0 + 126.0 * (ruin_lit ** 0.90)
    b_ruin = 44.0 + 92.0 * (ruin_lit ** 0.95)

    is_ruin = u_s >= 0.52
    s0[:, :, 0] = np.clip(np.where(is_ruin, r_ruin, r_cliff), 0, 255).astype(np.uint8)
    s0[:, :, 1] = np.clip(np.where(is_ruin, g_ruin, g_cliff), 0, 255).astype(np.uint8)
    s0[:, :, 2] = np.clip(np.where(is_ruin, b_ruin, b_cliff), 0, 255).astype(np.uint8)
    atlas[H_TERRAIN:H, 0:512] = s0
    height_for_normal[H_TERRAIN:H, 0:512] = np.where(is_ruin, ruin_lit, cliff_lit)

    # Strip 1 (512..1024): Warm Medieval Cottage Plaster/Stone Walls (`u < 0.38`), Radiant Golden Windows (`0.38..0.68`), Dark Oak Timber (`u >= 0.68`)
    s1 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    e1 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    wall_lit = np.clip(0.58 + 0.36 * v_s + 0.09 * n_strip_a, 0.0, 1.0)
    plaster_r = 228.0 * wall_lit
    plaster_g = 188.0 * wall_lit
    plaster_b = 140.0 * wall_lit
    is_base = v_s < 0.28
    plaster_r = np.where(is_base, plaster_r * 0.74, plaster_r)
    plaster_g = np.where(is_base, plaster_g * 0.72, plaster_g)
    plaster_b = np.where(is_base, plaster_b * 0.70, plaster_b)

    win_u = np.clip((u_s - 0.38) / 0.30, 0.0, 1.0)
    mullion = (
        (np.abs(win_u - 0.5) < 0.045)
        | (np.abs(v_s - 0.5) < 0.035)
        | (win_u < 0.07)
        | (win_u > 0.93)
        | (v_s < 0.06)
        | (v_s > 0.94)
    )
    glow_rad = np.sqrt((win_u - 0.5) ** 2 + (v_s - 0.55) ** 2)
    win_r = np.where(mullion, 62.0, 255.0)
    win_g = np.where(mullion, 38.0, np.clip(244.0 - 48.0 * glow_rad, 185.0, 255.0))
    win_b = np.where(mullion, 22.0, np.clip(160.0 - 95.0 * glow_rad, 55.0, 185.0))

    wood_lit = np.clip(0.54 + 0.38 * v_s + 0.12 * n_strip_b, 0.0, 1.0)
    wood_r = 104.0 * wood_lit
    wood_g = 64.0 * wood_lit
    wood_b = 36.0 * wood_lit

    is_win = (u_s >= 0.38) & (u_s < 0.68)
    s1[:, :, 0] = np.clip(np.where(u_s < 0.38, plaster_r, np.where(u_s < 0.68, win_r, wood_r)), 0, 255).astype(np.uint8)
    s1[:, :, 1] = np.clip(np.where(u_s < 0.38, plaster_g, np.where(u_s < 0.68, win_g, wood_g)), 0, 255).astype(np.uint8)
    s1[:, :, 2] = np.clip(np.where(u_s < 0.38, plaster_b, np.where(u_s < 0.68, win_b, wood_b)), 0, 255).astype(np.uint8)
    atlas[H_TERRAIN:H, 512:1024] = s1

    win_emit_mask = is_win & (~mullion)
    e1[:, :, 0] = np.where(win_emit_mask, 255, 0).astype(np.uint8)
    e1[:, :, 1] = np.where(win_emit_mask, np.clip(215.0 - 40.0 * glow_rad, 150.0, 235.0), 0).astype(np.uint8)
    e1[:, :, 2] = np.where(win_emit_mask, np.clip(95.0 - 50.0 * glow_rad, 35.0, 110.0), 0).astype(np.uint8)
    emission[H_TERRAIN:H, 512:1024] = e1
    height_for_normal[H_TERRAIN:H, 512:1024] = np.where(is_win, np.where(mullion, 0.7, 0.2), wall_lit)

    # Strip 2 (1024..1536): Deep Cerulean River with Golden Harbor Reflections (`u < 0.45`), Mirror Meadow Ponds (`0.45..0.78`), Foamy Waterfalls (`u >= 0.78`)
    s2 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    e2 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    wave_rip = np.sin(sy * 0.36 + n_strip_a * 4.5) * 0.5 + 0.5
    riv_u = np.clip(u_s / 0.45, 0.0, 1.0)
    gold_refl = np.clip((riv_u - 0.44) / 0.52, 0.0, 1.0) * (0.30 + 0.70 * wave_rip) * np.sin(v_s * math.pi)
    bridge_refl = np.exp(-((v_s - 0.66) ** 2) / 0.018) * (0.35 + 0.65 * wave_rip) * 0.75
    tot_gold = np.clip(gold_refl + bridge_refl, 0.0, 1.0)
    foam_edge = np.clip((0.10 - np.minimum(riv_u, 1.0 - riv_u)) / 0.10, 0.0, 1.0) * 0.48

    r_riv = (24.0 + 42.0 * v_s + 16.0 * wave_rip) * (1.0 - tot_gold) + 255.0 * tot_gold + 185.0 * foam_edge
    g_riv = (102.0 + 58.0 * v_s + 20.0 * wave_rip) * (1.0 - tot_gold) + 194.0 * tot_gold + 215.0 * foam_edge
    b_riv = (182.0 + 52.0 * v_s + 18.0 * wave_rip) * (1.0 - tot_gold) + 82.0 * tot_gold + 238.0 * foam_edge

    pond_u = (u_s - 0.45) / 0.33
    pond_rad = np.sqrt((pond_u - 0.5) ** 2 + (v_s - 0.5) ** 2)
    sky_refl = np.clip(1.0 - pond_rad / 0.46, 0.0, 1.0) ** 1.35
    gold_spec = np.exp(-(pond_rad ** 2) / 0.045) * (0.45 + 0.55 * wave_rip)
    r_pond = 36.0 + 118.0 * sky_refl + 102.0 * gold_spec
    g_pond = 92.0 + 98.0 * sky_refl + 56.0 * gold_spec
    b_pond = 156.0 + 78.0 * sky_refl + 14.0 * gold_spec

    wf_u = (u_s - 0.78) / 0.22
    streak = np.clip(0.58 + 0.28 * n_strip_b + 0.35 * np.exp(-((v_s - 0.10) ** 2) / 0.025) + 0.30 * np.exp(-((v_s - 0.90) ** 2) / 0.025), 0.0, 1.0)
    r_wf = 152.0 + 102.0 * streak
    g_wf = 204.0 + 50.0 * streak
    b_wf = 240.0 + 15.0 * streak

    s2[:, :, 0] = np.clip(np.where(u_s < 0.45, r_riv, np.where(u_s < 0.78, r_pond, r_wf)), 0, 255).astype(np.uint8)
    s2[:, :, 1] = np.clip(np.where(u_s < 0.45, g_riv, np.where(u_s < 0.78, g_pond, g_wf)), 0, 255).astype(np.uint8)
    s2[:, :, 2] = np.clip(np.where(u_s < 0.45, b_riv, np.where(u_s < 0.78, b_pond, b_wf)), 0, 255).astype(np.uint8)
    atlas[H_TERRAIN:H, 1024:1536] = s2

    e2[:, :, 0] = np.clip(np.where(u_s < 0.45, tot_gold * 125.0, np.where(u_s < 0.78, gold_spec * 90.0, streak * 55.0)), 0, 255).astype(np.uint8)
    e2[:, :, 1] = np.clip(np.where(u_s < 0.45, tot_gold * 85.0, np.where(u_s < 0.78, gold_spec * 60.0, streak * 70.0)), 0, 255).astype(np.uint8)
    e2[:, :, 2] = np.clip(np.where(u_s < 0.45, tot_gold * 20.0, np.where(u_s < 0.78, gold_spec * 25.0, streak * 85.0)), 0, 255).astype(np.uint8)
    emission[H_TERRAIN:H, 1024:1536] = e2
    height_for_normal[H_TERRAIN:H, 1024:1536] = 0.5 + 0.12 * wave_rip

    # Strip 3 (1536..2048): Lush Painterly 3D Foliage (4 Palettes: Emerald Spruce, Golden Oak, Amber-Russet Maple, Olive-Gold)
    s3 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    u_sub = (u_s * 4.0) - np.floor(u_s * 4.0)
    lit = np.clip(0.46 * u_sub + 0.54 * v_s + 0.14 * n_strip_b, 0.0, 1.0)

    r_p0 = 14.0 + 56.0 * (lit ** 1.10)
    g_p0 = 38.0 + 84.0 * (lit ** 1.00)
    b_p0 = 22.0 + 34.0 * (lit ** 1.25)

    r_p1 = 104.0 + 151.0 * (lit ** 0.68)
    g_p1 = 56.0 + 174.0 * (lit ** 0.84)
    b_p1 = 12.0 + 62.0 * (lit ** 1.35)

    r_p2 = 86.0 + 168.0 * (lit ** 0.68)
    g_p2 = 28.0 + 138.0 * (lit ** 0.98)
    b_p2 = 12.0 + 44.0 * (lit ** 1.35)

    r_p3 = 38.0 + 172.0 * (lit ** 0.88)
    g_p3 = 56.0 + 148.0 * (lit ** 0.85)
    b_p3 = 18.0 + 40.0 * (lit ** 1.20)

    s3[:, :, 0] = np.clip(np.where(u_s < 0.25, r_p0, np.where(u_s < 0.50, r_p1, np.where(u_s < 0.75, r_p2, r_p3))), 0, 255).astype(np.uint8)
    s3[:, :, 1] = np.clip(np.where(u_s < 0.25, g_p0, np.where(u_s < 0.50, g_p1, np.where(u_s < 0.75, g_p2, g_p3))), 0, 255).astype(np.uint8)
    s3[:, :, 2] = np.clip(np.where(u_s < 0.25, b_p0, np.where(u_s < 0.50, b_p1, np.where(u_s < 0.75, b_p2, b_p3))), 0, 255).astype(np.uint8)
    atlas[H_TERRAIN:H, 1536:2048] = s3
    height_for_normal[H_TERRAIN:H, 1536:2048] = lit

    # Strip 4 (2048..2560): Crisp Alpine Slate-Blue Western Shadow Faces & Sunlit Ivory-Snow Eastern Couloirs
    #   Matches Target Images 1 & 2: craggy alpine granite rock pokes through even on upper ridges,
    #   while snow streaks fill the gullies/couloirs and sunlit eastern arêtes!
    s4 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    sun_face = np.clip(u_s + 0.08 * n_strip_a, 0.0, 1.0)
    couloir_streak = np.sin(u_s * 32.0 + n_strip_b * 4.2) * 0.5 + 0.5
    ridge_snow = (v_s - 0.44) * 1.45 + 0.34 * (couloir_streak - 0.35) + 0.24 * n_strip_b + 0.22 * (sun_face - 0.45)
    snow_mask = np.clip((ridge_snow - 0.24) / 0.26, 0.0, 1.0) * np.clip((v_s - 0.28) / 0.18, 0.0, 1.0)

    # Cool alpine slate-blue shadow rock (#3E5580) <-> Warm sunlit granite (#C69E80)
    r_mtn = 60.0 * (1.0 - sun_face) + 198.0 * sun_face + 12.0 * n_strip_a
    g_mtn = 84.0 * (1.0 - sun_face) + 158.0 * sun_face + 10.0 * n_strip_a
    b_mtn = 130.0 * (1.0 - sun_face) + 132.0 * sun_face + 8.0 * n_strip_a

    # Shadow snow (#8AA6D2 cool slate-blue snow) <-> Sunlit snow (#FFF6EA warm ivory-white snow)
    r_snow = 138.0 * (1.0 - sun_face) + 255.0 * sun_face
    g_snow = 166.0 * (1.0 - sun_face) + 245.0 * sun_face
    b_snow = 212.0 * (1.0 - sun_face) + 232.0 * sun_face

    s4[:, :, 0] = np.clip(r_mtn * (1.0 - snow_mask) + r_snow * snow_mask, 0, 255).astype(np.uint8)
    s4[:, :, 1] = np.clip(g_mtn * (1.0 - snow_mask) + g_snow * snow_mask, 0, 255).astype(np.uint8)
    s4[:, :, 2] = np.clip(b_mtn * (1.0 - snow_mask) + b_snow * snow_mask, 0, 255).astype(np.uint8)
    atlas[H_TERRAIN:H, 2048:2560] = s4
    height_for_normal[H_TERRAIN:H, 2048:2560] = 0.5 * sun_face + 0.3 * snow_mask + 0.2 * n_strip_a

    # Strip 5 (2560..3072): Steep-Gabled Shingle Roofs & Gothic Castle Spires
    s5 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    shingle_wave = np.sin(sy * 0.45) * 0.04
    roof_lit = np.clip((0.55 + 0.42 * v_s) + 0.08 * n_strip_b + shingle_wave, 0.0, 1.0)
    r_terra = 224.0 * roof_lit
    g_terra = 90.0 * roof_lit
    b_terra = 44.0 * roof_lit

    r_slate = 58.0 * roof_lit
    g_slate = 98.0 * roof_lit
    b_slate = 168.0 * roof_lit

    spire_u = np.clip((u_s - 0.70) / 0.30, 0.0, 1.0)
    spire_rim = np.clip((spire_u - 0.52) / 0.48, 0.0, 1.0) * (0.45 + 0.55 * v_s)
    r_spire = (54.0 * roof_lit) * (1.0 - spire_rim) + 228.0 * spire_rim
    g_spire = (58.0 * roof_lit) * (1.0 - spire_rim) + 158.0 * spire_rim
    b_spire = (84.0 * roof_lit) * (1.0 - spire_rim) + 92.0 * spire_rim

    s5[:, :, 0] = np.clip(np.where(u_s < 0.36, r_terra, np.where(u_s < 0.70, r_slate, r_spire)), 0, 255).astype(np.uint8)
    s5[:, :, 1] = np.clip(np.where(u_s < 0.36, g_terra, np.where(u_s < 0.70, g_slate, g_spire)), 0, 255).astype(np.uint8)
    s5[:, :, 2] = np.clip(np.where(u_s < 0.36, b_terra, np.where(u_s < 0.70, b_slate, b_spire)), 0, 255).astype(np.uint8)
    atlas[H_TERRAIN:H, 2560:3072] = s5
    height_for_normal[H_TERRAIN:H, 2560:3072] = roof_lit

    # Strip 6 (3072..3584): Cyan-Sapphire Beacon Orb (`u < 0.36`), Golden Crystal-Lotus (`0.36..0.72`), White Limestone & Mist (`u >= 0.72`)
    s6 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    e6 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    orb_u = np.clip(u_s / 0.36, 0.0, 1.0)
    orb_core = np.exp(-((orb_u - 0.5) ** 2 + (v_s - 0.55) ** 2) / 0.12)
    r_orb = 24.0 + 231.0 * orb_core
    g_orb = 195.0 + 60.0 * orb_core
    b_orb = 255.0

    lot_u = np.clip((u_s - 0.36) / 0.36, 0.0, 1.0)
    blade_ridge = 1.0 - np.abs(lot_u - 0.5) * 1.15
    r_lot = 244.0 + 11.0 * v_s
    g_lot = 148.0 + 106.0 * (v_s ** 0.78) * blade_ridge
    b_lot = 28.0 + 192.0 * (v_s ** 1.45) * blade_ridge

    lh_lit = np.clip(0.72 + 0.28 * v_s + 0.06 * n_strip_a, 0.0, 1.0)
    r_lh = 238.0 * lh_lit
    g_lh = 224.0 * lh_lit
    b_lh = 202.0 * lh_lit

    s6[:, :, 0] = np.clip(np.where(u_s < 0.36, r_orb, np.where(u_s < 0.72, r_lot, r_lh)), 0, 255).astype(np.uint8)
    s6[:, :, 1] = np.clip(np.where(u_s < 0.36, g_orb, np.where(u_s < 0.72, g_lot, g_lh)), 0, 255).astype(np.uint8)
    s6[:, :, 2] = np.clip(np.where(u_s < 0.36, b_orb, np.where(u_s < 0.72, b_lot, b_lh)), 0, 255).astype(np.uint8)
    atlas[H_TERRAIN:H, 3072:3584] = s6

    e6[:, :, 0] = np.clip(np.where(u_s < 0.36, 45.0 + 210.0 * orb_core, np.where(u_s < 0.72, 235.0 * (0.4 + 0.6 * v_s), 0.0)), 0, 255).astype(np.uint8)
    e6[:, :, 1] = np.clip(np.where(u_s < 0.36, 200.0 + 55.0 * orb_core, np.where(u_s < 0.72, 145.0 * (0.3 + 0.7 * v_s), 0.0)), 0, 255).astype(np.uint8)
    e6[:, :, 2] = np.clip(np.where(u_s < 0.36, 255.0, np.where(u_s < 0.72, 35.0 * v_s, 0.0)), 0, 255).astype(np.uint8)
    emission[H_TERRAIN:H, 3072:3584] = e6
    height_for_normal[H_TERRAIN:H, 3072:3584] = lh_lit

    # Strip 7 (3584..4096): 3D Meadow Grass Blades & Multi-Color Wildflowers (Gold, Magenta-Crimson, Royal Violet)
    s7 = np.zeros((H_STRIPS, 512, 3), dtype=np.uint8)
    r_gr = 142.0 + 112.0 * (v_s ** 0.75)
    g_gr = 96.0 + 114.0 * (v_s ** 0.82)
    b_gr = 18.0 + 38.0 * (v_s ** 1.10)

    r_sh = 32.0 + 56.0 * v_s
    g_sh = 60.0 + 72.0 * v_s
    b_sh = 20.0 + 22.0 * v_s

    r_fl_gold = 248.0 + 7.0 * v_s
    g_fl_gold = 160.0 + 88.0 * v_s
    b_fl_gold = 18.0 + 68.0 * v_s

    r_fl_mag = 188.0 + 66.0 * v_s
    g_fl_mag = 26.0 + 68.0 * v_s
    b_fl_mag = 78.0 + 74.0 * v_s

    r_fl_vio = 98.0 + 108.0 * v_s
    g_fl_vio = 40.0 + 88.0 * v_s
    b_fl_vio = 168.0 + 86.0 * v_s

    s7[:, :, 0] = np.clip(
        np.where(u_s < 0.35, r_gr, np.where(u_s < 0.55, r_sh, np.where(u_s < 0.70, r_fl_gold, np.where(u_s < 0.85, r_fl_mag, r_fl_vio)))),
        0, 255
    ).astype(np.uint8)
    s7[:, :, 1] = np.clip(
        np.where(u_s < 0.35, g_gr, np.where(u_s < 0.55, g_sh, np.where(u_s < 0.70, g_fl_gold, np.where(u_s < 0.85, g_fl_mag, g_fl_vio)))),
        0, 255
    ).astype(np.uint8)
    s7[:, :, 2] = np.clip(
        np.where(u_s < 0.35, b_gr, np.where(u_s < 0.55, b_sh, np.where(u_s < 0.70, b_fl_gold, np.where(u_s < 0.85, g_fl_mag, g_fl_vio)))),
        0, 255
    ).astype(np.uint8)
    atlas[H_TERRAIN:H, 3584:4096] = s7
    height_for_normal[H_TERRAIN:H, 3584:4096] = v_s

    atlas_path = os.path.join(UNITY_TEX_DIR, "FarmMain_3D_Albedo_4K.png")
    emission_path = os.path.join(UNITY_TEX_DIR, "FarmMain_3D_Emission_4K.png")
    Image.fromarray(atlas).save(atlas_path, optimize=False)
    Image.fromarray(emission).save(emission_path, optimize=False)

    gx_n = cv2.Sobel(height_for_normal, cv2.CV_32F, 1, 0, ksize=3) * 2.2
    gy_n = cv2.Sobel(height_for_normal, cv2.CV_32F, 0, 1, ksize=3) * 2.2
    gz_n = np.ones_like(height_for_normal)
    norm_len = np.sqrt(gx_n ** 2 + gy_n ** 2 + gz_n ** 2) + 1e-6
    nx_u8 = np.clip((-gx_n / norm_len * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
    ny_u8 = np.clip((gy_n / norm_len * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
    nz_u8 = np.clip((gz_n / norm_len * 0.5 + 0.5) * 255.0, 128, 255).astype(np.uint8)
    normal_map = np.stack([nx_u8, ny_u8, nz_u8], axis=-1)
    normal_path = os.path.join(UNITY_TEX_DIR, "FarmMain_3D_Normal_4K.png")
    Image.fromarray(normal_map).save(normal_path, optimize=False)

    # -------------------------------------------------------------------------
    # C. Painterly Sunset Sky Backdrop (`FarmMain_Sunset_Sky_Backdrop.png`)
    # -------------------------------------------------------------------------
    sky_w, sky_h = 2048, 1536
    sy_k, sx_k = np.mgrid[0:sky_h, 0:sky_w].astype(np.float32)
    uk = sx_k / float(sky_w - 1)
    vk = 1.0 - (sy_k / float(sky_h - 1))

    n_sky = multi_scale_organic_noise(sky_h, sky_w, seed=505, scales=(96, 36, 14), weights=(0.55, 0.30, 0.15))
    t_sky = np.clip((vk - 0.25) / 0.75, 0.0, 1.0)
    r_sky = np.where(vk > 0.25, 252.0 * (1.0 - t_sky ** 0.72) + 62.0 * (t_sky ** 0.72), 74.0 + 178.0 * (vk / 0.25))
    g_sky = np.where(vk > 0.25, 168.0 * (1.0 - t_sky ** 0.86) + 50.0 * (t_sky ** 0.86), 50.0 + 118.0 * (vk / 0.25))
    b_sky = np.where(vk > 0.25, 92.0 * (1.0 - t_sky ** 1.10) + 114.0 * (t_sky ** 1.10), 74.0 + 18.0 * (vk / 0.25))

    sun_dist = np.sqrt(((uk - 0.815) * 1.25) ** 2 + ((vk - 0.865) * 1.0) ** 2)
    cloud_gold = np.exp(-sun_dist * 2.6)
    cloud_band = np.exp(-((vk - 0.80) ** 2) / 0.042) * np.clip(n_sky + 0.38, 0.0, 1.0)

    r_cl = 122.0 + 133.0 * np.clip(cloud_gold + 0.45 * (1.0 - vk), 0.0, 1.0)
    g_cl = 74.0 + 144.0 * np.clip(cloud_gold + 0.25 * (1.0 - vk), 0.0, 1.0)
    b_cl = 114.0 - 38.0 * cloud_gold

    r_sky = r_sky * (1.0 - 0.72 * cloud_band) + r_cl * (0.72 * cloud_band)
    g_sky = g_sky * (1.0 - 0.72 * cloud_band) + g_cl * (0.72 * cloud_band)
    b_sky = b_sky * (1.0 - 0.72 * cloud_band) + b_cl * (0.72 * cloud_band)

    sun_core = np.exp(-(sun_dist ** 2) / 0.00085)
    sun_halo = np.exp(-(sun_dist ** 2) / 0.020) * 0.80 + np.exp(-sun_dist * 4.0) * 0.42
    r_sky = np.clip(r_sky + 255.0 * sun_core + 148.0 * sun_halo, 0, 255)
    g_sky = np.clip(g_sky + 250.0 * sun_core + 102.0 * sun_halo, 0, 255)
    b_sky = np.clip(b_sky + 215.0 * sun_core + 28.0 * sun_halo, 0, 255)

    mist_band = np.exp(-((vk - 0.50) ** 2) / 0.055) * np.clip(0.42 + 0.30 * n_sky, 0.0, 1.0)
    r_sky = r_sky * (1.0 - 0.36 * mist_band) + 238.0 * (0.36 * mist_band)
    g_sky = g_sky * (1.0 - 0.36 * mist_band) + 190.0 * (0.36 * mist_band)
    b_sky = b_sky * (1.0 - 0.36 * mist_band) + 174.0 * (0.36 * mist_band)

    sky_img = np.stack([r_sky, g_sky, b_sky], axis=-1).astype(np.uint8)
    sky_backdrop_path = os.path.join(UNITY_TEX_DIR, "FarmMain_Sunset_Sky_Backdrop.png")
    Image.fromarray(sky_img).save(sky_backdrop_path)
    Image.fromarray(sky_img).save(os.path.join(UNITY_TEX_DIR, "FarmMain_360_Equirectangular_Skybox.png"))

    return atlas_path, normal_path, emission_path, sky_backdrop_path


class MeshBuilder:
    def __init__(self):
        self.v_list = []
        self.uv_list = []
        self.f_list = []
        self.v_count = 0

    def add(self, verts, uvs, faces):
        if len(verts) == 0 or len(faces) == 0:
            return
        v_arr = np.asarray(verts, dtype=np.float32)
        uv_arr = np.asarray(uvs, dtype=np.float32)
        f_arr = np.asarray(faces, dtype=np.int32) + self.v_count
        self.v_list.append(v_arr)
        self.uv_list.append(uv_arr)
        self.f_list.append(f_arr)
        self.v_count += len(v_arr)

    def add_faceted_normal_mapped(self, verts, faces, strip_idx, u0=0.04, u1=0.96, v0=0.05, v1=0.96, z_min=None, z_max=None, custom_v_per_vert=None):
        v_arr = np.asarray(verts, dtype=np.float32)
        f_arr = np.asarray(faces, dtype=np.int32)
        if len(f_arr) == 0:
            return

        tri_v = v_arr[f_arr]  # (T, 3, 3)
        e1 = tri_v[:, 1] - tri_v[:, 0]
        e2 = tri_v[:, 2] - tri_v[:, 0]
        norms = np.cross(e1, e2)
        n_len = np.linalg.norm(norms, axis=-1, keepdims=True) + 1e-8
        norms = norms / n_len

        sun_dot = np.clip(np.sum(norms * SUN_DIR[None, :], axis=-1), -1.0, 1.0)
        u_face = u0 + (u1 - u0) * (0.5 + 0.48 * sun_dot)

        if custom_v_per_vert is not None:
            v_arr_norm = np.asarray(custom_v_per_vert, dtype=np.float32)
            v_vert = v0 + (v1 - v0) * np.clip(v_arr_norm[f_arr], 0.0, 1.0)
        else:
            z0_val = float(np.min(v_arr[:, 2])) if z_min is None else float(z_min)
            z1_val = float(np.max(v_arr[:, 2])) if z_max is None else float(z_max)
            dz_span = max(0.25, z1_val - z0_val)
            v_vert = v0 + (v1 - v0) * np.clip((tri_v[:, :, 2] - z0_val) / dz_span, 0.0, 1.0)

        u_vert = np.stack([
            np.clip(u_face - 0.015, u0, u1),
            np.clip(u_face + 0.015, u0, u1),
            u_face,
        ], axis=-1)

        flat_v = tri_v.reshape(-1, 3)
        flat_uv = strip_uv(strip_idx, u_vert.reshape(-1), v_vert.reshape(-1))
        flat_f = np.arange(len(flat_v), dtype=np.int32).reshape(-1, 3)
        self.add(flat_v, flat_uv, flat_f)

    def build_arrays(self):
        if not self.v_list:
            return (
                np.zeros((0, 3), dtype=np.float32),
                np.zeros((0, 2), dtype=np.float32),
                np.zeros((0, 3), dtype=np.int32),
            )
        return (
            np.vstack(self.v_list),
            np.vstack(self.uv_list),
            np.vstack(self.f_list),
        )


def rot_z(verts, yaw):
    if abs(yaw) < 1e-6:
        return verts
    c, s = math.cos(yaw), math.sin(yaw)
    x = verts[:, 0] * c - verts[:, 1] * s
    y = verts[:, 0] * s + verts[:, 1] * c
    return np.stack([x, y, verts[:, 2]], axis=-1)


def make_box(cx, cy, z0, sx, sy, sz, strip_idx, u0=0.05, u1=0.95, v0=0.05, v1=0.95, yaw=0.0, top_u0=None, top_u1=None, top_v0=None, top_v1=None):
    hx, hy = sx * 0.5, sy * 0.5
    corners = np.array([
        [-hx, -hy, 0.0], [hx, -hy, 0.0], [hx, hy, 0.0], [-hx, hy, 0.0],
        [-hx, -hy, sz],  [hx, -hy, sz],  [hx, hy, sz],  [-hx, hy, sz],
    ], dtype=np.float32)
    corners = rot_z(corners, yaw) + np.array([cx, cy, z0], dtype=np.float32)

    tu0 = top_u0 if top_u0 is not None else u0
    tu1 = top_u1 if top_u1 is not None else u1
    tv0 = top_v0 if top_v0 is not None else v0
    tv1 = top_v1 if top_v1 is not None else v1

    quads = [
        (0, 1, 5, 4, u0, u1, v0, v1),
        (1, 2, 6, 5, u0, u1, v0, v1),
        (2, 3, 7, 6, u0, u1, v0, v1),
        (3, 0, 4, 7, u0, u1, v0, v1),
        (4, 5, 6, 7, tu0, tu1, tv0, tv1),
    ]
    verts, uvs, faces = [], [], []
    for i, (a, b, c, d, qa, qb, qc, qd) in enumerate(quads):
        base = i * 4
        verts.extend([corners[a], corners[b], corners[c], corners[d]])
        uvs.extend([[qa, qc], [qb, qc], [qb, qd], [qa, qd]])
        faces.extend([[base, base + 1, base + 2], [base, base + 2, base + 3]])
    return np.array(verts, dtype=np.float32), strip_uv(strip_idx, np.array(uvs)[:, 0], np.array(uvs)[:, 1]), np.array(faces, dtype=np.int32)


def make_frustum(cx, cy, z0, sx0, sy0, sx1, sy1, sz, strip_idx, u0=0.05, u1=0.95, v0=0.05, v1=0.95, yaw=0.0, top_u0=None, top_u1=None, top_v0=None, top_v1=None):
    hx0, hy0 = sx0 * 0.5, sy0 * 0.5
    hx1, hy1 = sx1 * 0.5, sy1 * 0.5
    corners = np.array([
        [-hx0, -hy0, 0.0], [hx0, -hy0, 0.0], [hx0, hy0, 0.0], [-hx0, hy0, 0.0],
        [-hx1, -hy1, sz],  [hx1, -hy1, sz],  [hx1, hy1, sz],  [-hx1, hy1, sz],
    ], dtype=np.float32)
    corners = rot_z(corners, yaw) + np.array([cx, cy, z0], dtype=np.float32)

    tu0 = top_u0 if top_u0 is not None else u0
    tu1 = top_u1 if top_u1 is not None else u1
    tv0 = top_v0 if top_v0 is not None else v0
    tv1 = top_v1 if top_v1 is not None else v1

    quads = [
        (0, 1, 5, 4, u0, u1, v0, v1),
        (1, 2, 6, 5, u0, u1, v0, v1),
        (2, 3, 7, 6, u0, u1, v0, v1),
        (3, 0, 4, 7, u0, u1, v0, v1),
        (4, 5, 6, 7, tu0, tu1, tv0, tv1),
    ]
    verts, uvs, faces = [], [], []
    for i, (a, b, c, d, qa, qb, qc, qd) in enumerate(quads):
        base = i * 4
        verts.extend([corners[a], corners[b], corners[c], corners[d]])
        uvs.extend([[qa, qc], [qb, qc], [qb, qd], [qa, qd]])
        faces.extend([[base, base + 1, base + 2], [base, base + 2, base + 3]])
    return np.array(verts, dtype=np.float32), strip_uv(strip_idx, np.array(uvs)[:, 0], np.array(uvs)[:, 1]), np.array(faces, dtype=np.int32)


def make_pitched_roof(cx, cy, z0, sx, sy, sz, strip_idx, u0=0.04, u1=0.34, v0=0.10, v1=0.95, yaw=0.0, ridge_along_x=True):
    hx, hy = sx * 0.5, sy * 0.5
    if ridge_along_x:
        pts = np.array([
            [-hx, -hy, 0.0], [hx, -hy, 0.0], [hx, hy, 0.0], [-hx, hy, 0.0],
            [-hx * 0.96, 0.0, sz], [hx * 0.96, 0.0, sz],
        ], dtype=np.float32)
        pts = rot_z(pts, yaw) + np.array([cx, cy, z0], dtype=np.float32)
        verts = [
            pts[0], pts[1], pts[5], pts[4],
            pts[2], pts[3], pts[4], pts[5],
            pts[1], pts[2], pts[5],
            pts[3], pts[0], pts[4],
        ]
    else:
        pts = np.array([
            [-hx, -hy, 0.0], [hx, -hy, 0.0], [hx, hy, 0.0], [-hx, hy, 0.0],
            [0.0, -hy * 0.96, sz], [0.0, hy * 0.96, sz],
        ], dtype=np.float32)
        pts = rot_z(pts, yaw) + np.array([cx, cy, z0], dtype=np.float32)
        verts = [
            pts[1], pts[2], pts[5], pts[4],
            pts[3], pts[0], pts[4], pts[5],
            pts[0], pts[1], pts[4],
            pts[2], pts[3], pts[5],
        ]
    uvs = [
        [u0, v0], [u1, v0], [u1, v1], [u0, v1],
        [u0, v0], [u1, v0], [u1, v1], [u0, v1],
        [u0, v0], [u1, v0], [0.5 * (u0 + u1), v1],
        [u0, v0], [u1, v0], [0.5 * (u0 + u1), v1],
    ]
    faces = [
        [0, 1, 2], [0, 2, 3],
        [4, 5, 6], [4, 6, 7],
        [8, 9, 10],
        [11, 12, 13],
    ]
    return np.array(verts, dtype=np.float32), strip_uv(strip_idx, np.array(uvs)[:, 0], np.array(uvs)[:, 1]), np.array(faces, dtype=np.int32)


def make_cylinder_or_cone(cx, cy, z0, r_bot, r_top, height, segs, strip_idx, u0=0.05, u1=0.95, v0=0.05, v1=0.95, cap_top=True):
    ang = np.linspace(0.0, 2.0 * math.pi, segs + 1, dtype=np.float32)
    ca, sa = np.cos(ang), np.sin(ang)
    verts, uvs, faces = [], [], []
    for i in range(segs + 1):
        u_i = u0 + (u1 - u0) * (i / float(segs))
        verts.append([cx + r_bot * ca[i], cy + r_bot * sa[i], z0])
        uvs.append([u_i, v0])
        verts.append([cx + r_top * ca[i], cy + r_top * sa[i], z0 + height])
        uvs.append([u_i, v1])
    for i in range(segs):
        b0 = i * 2
        faces.append([b0, b0 + 2, b0 + 3])
        faces.append([b0, b0 + 3, b0 + 1])
    if cap_top and r_top > 1e-3:
        c_idx = len(verts)
        verts.append([cx, cy, z0 + height])
        uvs.append([0.5 * (u0 + u1), v1])
        for i in range(segs):
            faces.append([i * 2 + 1, (i + 1) * 2 + 1, c_idx])
    return np.array(verts, dtype=np.float32), strip_uv(strip_idx, np.array(uvs)[:, 0], np.array(uvs)[:, 1]), np.array(faces, dtype=np.int32)


def make_dome_puff(cx, cy, cz, rx, ry, rz, segs, rings, strip_idx, u0=0.05, u1=0.95, v0=0.10, v1=0.95, seed=0.0):
    verts, uvs, faces = [], [], []
    for r_i in range(rings + 1):
        v_frac = r_i / float(rings)
        phi = (v_frac - 0.5) * math.pi
        cp, sp = math.cos(phi), math.sin(phi)
        v_uv = v0 + (v1 - v0) * v_frac
        for s_i in range(segs + 1):
            u_frac = s_i / float(segs)
            theta = u_frac * 2.0 * math.pi
            ct, st = math.cos(theta), math.sin(theta)
            wob = 1.0 + 0.14 * math.sin(theta * 3.0 + phi * 2.0 + seed) * math.cos(theta * 2.0 - phi * 3.0)
            px = cx + rx * cp * ct * wob
            py = cy + ry * cp * st * wob
            pz = cz + rz * sp * wob
            verts.append([px, py, pz])
            uvs.append([u0 + (u1 - u0) * u_frac, v_uv])
    stride = segs + 1
    for r_i in range(rings):
        for s_i in range(segs):
            a = r_i * stride + s_i
            b = a + 1
            c = (r_i + 1) * stride + s_i + 1
            d = (r_i + 1) * stride + s_i
            faces.append([a, b, c])
            faces.append([a, c, d])
    return np.array(verts, dtype=np.float32), strip_uv(strip_idx, np.array(uvs)[:, 0], np.array(uvs)[:, 1]), np.array(faces, dtype=np.int32)


def add_chiseled_sandstone_boulder(mb, cx, cy, z_bot, sx, sy, sz, yaw=0.0, seed=0.0):
    rings = 4
    segs = 8
    verts = []
    faces = []
    for r_i in range(rings + 1):
        vf = r_i / float(rings)
        z_cur = vf * sz
        profile = 0.82 + 0.24 * math.sin(vf * math.pi) - 0.12 * (vf ** 2)
        for s_i in range(segs):
            th = s_i * (2.0 * math.pi / segs)
            crag = 1.0 + 0.14 * math.sin(th * 3.0 + vf * 4.0 + seed) + 0.08 * math.cos(th * 5.0 - seed * 1.7)
            lx = 0.5 * sx * profile * crag * math.cos(th)
            ly = 0.5 * sy * profile * crag * math.sin(th)
            verts.append([lx, ly, z_cur])
    top_center_idx = len(verts)
    verts.append([0.0, 0.0, sz * 1.02])
    verts_arr = rot_z(np.array(verts, dtype=np.float32), yaw) + np.array([cx, cy, z_bot], dtype=np.float32)

    for r_i in range(rings):
        for s_i in range(segs):
            s_next = (s_i + 1) % segs
            a = r_i * segs + s_i
            b = r_i * segs + s_next
            c = (r_i + 1) * segs + s_next
            d = (r_i + 1) * segs + s_i
            faces.append([a, b, c])
            faces.append([a, c, d])
    for s_i in range(segs):
        s_next = (s_i + 1) % segs
        faces.append([rings * segs + s_i, rings * segs + s_next, top_center_idx])

    mb.add_faceted_normal_mapped(verts_arr, faces, 0, u0=0.03, u1=0.49, v0=0.05, v1=0.86, z_min=z_bot, z_max=z_bot + sz)


def add_steep_gabled_fantasy_house(mb, ao_stamps, light_stamps, cx, cy, z0, sx, sy, wall_h, roof_h, yaw=0.0, roof_style="terracotta", has_cross_wing=True, has_chimney=True, has_dormers=True, has_porch=True):
    """
    Delicate diorama-scale steep-gabled medieval fantasy cottage (`Zone 04, 05, 06, 08`):
    Flush mullioned glowing golden windows, timber framing, overhanging shingle roofs, dormers, chimney, and porch!
    """
    ao_stamps.append((cx, cy, max(sx, sy) * 0.78, 0.58))
    light_stamps.append((cx, cy - sy * 0.65, max(sx, sy) * 0.65, 0.48))

    ru0, ru1 = (0.03, 0.34) if roof_style == "terracotta" else (0.38, 0.68)

    # 1. Stone foundation plinth
    mb.add(*make_frustum(cx, cy, z0 - 0.30, sx * 1.08, sy * 1.08, sx * 1.03, sy * 1.03, wall_h * 0.30 + 0.30, 1, 0.03, 0.35, 0.04, 0.26, yaw=yaw))
    # 2. Main plaster & stone wall body
    mb.add(*make_box(cx, cy, z0 + wall_h * 0.20, sx, sy, wall_h * 0.82, 1, 0.03, 0.35, 0.32, 0.94, yaw=yaw))
    # 3. Dark oak corner posts & wall-plate beams
    for dx_s in (-0.5, 0.5):
        for dy_s in (-0.5, 0.5):
            pt = rot_z(np.array([[dx_s * sx, dy_s * sy, 0.0]], dtype=np.float32), yaw)[0]
            mb.add(*make_box(cx + pt[0], cy + pt[1], z0 + wall_h * 0.18, 0.16, 0.16, wall_h * 0.84, 1, 0.72, 0.96, 0.10, 0.90, yaw=yaw))
    mb.add(*make_box(cx, cy, z0 + wall_h * 0.56, sx * 1.03, sy * 1.03, 0.12, 1, 0.72, 0.96, 0.15, 0.85, yaw=yaw))
    mb.add(*make_box(cx, cy, z0 + wall_h * 0.96, sx * 1.04, sy * 1.04, 0.14, 1, 0.72, 0.96, 0.15, 0.85, yaw=yaw))

    # 4. Steep overhanging pitched roof + wooden ridge beam
    mb.add(*make_pitched_roof(cx, cy, z0 + wall_h * 0.98, sx * 1.22, sy * 1.24, roof_h, 5, ru0, ru1, 0.08, 0.95, yaw=yaw, ridge_along_x=True))
    mb.add(*make_box(cx, cy, z0 + wall_h * 0.98 + roof_h - 0.08, sx * 1.24, 0.15, 0.15, 1, 0.72, 0.96, 0.20, 0.85, yaw=yaw))

    # 5. Flush Glowing Warm Golden Windows on all 4 facades
    n_win_x = max(2, int(sx / 1.15))
    for i in range(n_win_x):
        lx = -sx * 0.30 + (sx * 0.60) * (i / float(max(1, n_win_x - 1))) if n_win_x > 1 else 0.0
        for side_y in (-sy * 0.505, sy * 0.505):
            for wz in (z0 + wall_h * 0.30, z0 + wall_h * 0.64):
                p_w = rot_z(np.array([[lx, side_y, 0.0]], dtype=np.float32), yaw)[0]
                mb.add(*make_box(cx + p_w[0], cy + p_w[1], wz, 0.44, 0.08, 0.36, 1, 0.40, 0.66, 0.12, 0.92, yaw=yaw))
    for side_x in (-sx * 0.505, sx * 0.505):
        for wz in (z0 + wall_h * 0.34, z0 + wall_h * 0.68, z0 + wall_h * 1.10):
            p_w = rot_z(np.array([[side_x, 0.0, 0.0]], dtype=np.float32), yaw)[0]
            mb.add(*make_box(cx + p_w[0], cy + p_w[1], wz, 0.08, 0.48, 0.38, 1, 0.40, 0.66, 0.12, 0.92, yaw=yaw))

    # 6. Cross-wing gable
    if has_cross_wing:
        wx_off = sx * 0.20
        wy_off = -sy * 0.36
        pw = rot_z(np.array([[wx_off, wy_off, 0.0]], dtype=np.float32), yaw)[0]
        w_sx, w_sy = sx * 0.48, sy * 0.72
        mb.add(*make_box(cx + pw[0], cy + pw[1], z0 - 0.18, w_sx, w_sy, wall_h * 0.96 + 0.18, 1, 0.03, 0.35, 0.15, 0.92, yaw=yaw))
        mb.add(*make_pitched_roof(cx + pw[0], cy + pw[1], z0 + wall_h * 0.94, w_sx * 1.22, w_sy * 1.22, roof_h * 0.86, 5, ru0, ru1, 0.10, 0.95, yaw=yaw, ridge_along_x=False))
        pw_front = rot_z(np.array([[wx_off, wy_off - w_sy * 0.505, 0.0]], dtype=np.float32), yaw)[0]
        mb.add(*make_box(cx + pw_front[0], cy + pw_front[1], z0 + wall_h * 0.38, 0.52, 0.09, 0.44, 1, 0.40, 0.66, 0.10, 0.92, yaw=yaw))
        mb.add(*make_box(cx + pw_front[0], cy + pw_front[1], z0 + wall_h * 0.98, 0.40, 0.09, 0.36, 1, 0.40, 0.66, 0.10, 0.92, yaw=yaw))

    # 7. Front Dormer Gables
    if has_dormers:
        for dx_d in (-sx * 0.24, sx * 0.24):
            pd = rot_z(np.array([[dx_d, -sy * 0.28, 0.0]], dtype=np.float32), yaw)[0]
            dz = z0 + wall_h * 0.98 + roof_h * 0.20
            mb.add(*make_box(cx + pd[0], cy + pd[1], dz, 0.62, 0.60, roof_h * 0.40, 1, 0.04, 0.34, 0.35, 0.90, yaw=yaw))
            mb.add(*make_pitched_roof(cx + pd[0], cy + pd[1], dz + roof_h * 0.38, 0.76, 0.74, roof_h * 0.35, 5, ru0, ru1, 0.15, 0.92, yaw=yaw, ridge_along_x=False))
            pd_win = rot_z(np.array([[dx_d, -sy * 0.28 - 0.31, 0.0]], dtype=np.float32), yaw)[0]
            mb.add(*make_box(cx + pd_win[0], cy + pd_win[1], dz + 0.06, 0.38, 0.08, roof_h * 0.28, 1, 0.40, 0.66, 0.12, 0.92, yaw=yaw))

    # 8. Stone Chimney
    if has_chimney:
        pc = rot_z(np.array([[-sx * 0.32, sy * 0.18, 0.0]], dtype=np.float32), yaw)[0]
        cz_top = z0 + wall_h + roof_h * 1.06
        mb.add(*make_box(cx + pc[0], cy + pc[1], z0 + wall_h * 0.4, 0.44, 0.44, roof_h * 1.06 + wall_h * 0.6, 0, 0.55, 0.92, 0.15, 0.85, yaw=yaw))
        mb.add(*make_box(cx + pc[0], cy + pc[1], cz_top, 0.54, 0.54, 0.15, 0, 0.55, 0.92, 0.60, 0.95, yaw=yaw))

    # 9. Wooden Porch & Lantern
    if has_porch:
        pp = rot_z(np.array([[-sx * 0.18, -sy * 0.60, 0.0]], dtype=np.float32), yaw)[0]
        mb.add(*make_box(cx + pp[0], cy + pp[1], z0 + wall_h * 0.46, sx * 0.46, 0.72, 0.12, 5, ru0, ru1, 0.20, 0.80, yaw=yaw))
        for px_col in (-sx * 0.36, 0.0):
            pcol = rot_z(np.array([[px_col, -sy * 0.88, 0.0]], dtype=np.float32), yaw)[0]
            mb.add(*make_box(cx + pcol[0], cy + pcol[1], z0 - 0.12, 0.12, 0.12, wall_h * 0.62, 1, 0.72, 0.96, 0.15, 0.85, yaw=yaw))
        plan = rot_z(np.array([[-sx * 0.18, -sy * 0.88, 0.0]], dtype=np.float32), yaw)[0]
        mb.add(*make_box(cx + plan[0], cy + plan[1], z0 + wall_h * 0.34, 0.18, 0.18, 0.24, 1, 0.42, 0.62, 0.25, 0.85, yaw=yaw))


def add_chiseled_broadleaf_tree(mb, ao_stamps, cx, cy, z0, scale=1.0, palette_idx=1, seed=0.0):
    """
    Chiseled, normal-mapped 11-bough volumetric Golden-Hour Autumn Tree (`Oak` / `Maple` / `Olive-Gold`).
    Every bough cluster is unwelded and shaded by `dot(N, SUN_DIR)` + altitude via `add_faceted_normal_mapped`!
    """
    ao_stamps.append((cx, cy, 1.45 * scale, 0.48))
    u_min = 0.26 if palette_idx == 1 else (0.51 if palette_idx == 2 else 0.76)
    u_max = u_min + 0.22

    # Gnarled trunk + root flare + 3 main branches
    trunk_h = 1.05 * scale
    mb.add(*make_cylinder_or_cone(cx, cy, z0 - 0.18, 0.26 * scale, 0.15 * scale, trunk_h + 0.18, 7, 1, 0.72, 0.95, 0.08, 0.85, cap_top=False))

    bough_specs = [
        (-0.52, -0.36, 0.94, 0.84, 0.78, 0.62, 0.08, 0.52),
        (-0.46,  0.44, 0.98, 0.82, 0.80, 0.64, 0.10, 0.55),
        ( 0.04, -0.56, 1.00, 0.82, 0.78, 0.64, 0.16, 0.60),
        ( 0.06,  0.54, 1.04, 0.80, 0.82, 0.66, 0.20, 0.64),
        ( 0.52, -0.28, 1.08, 0.84, 0.80, 0.68, 0.36, 0.84),
        ( 0.48,  0.34, 1.12, 0.82, 0.80, 0.68, 0.40, 0.86),
        (-0.34,  0.02, 1.44, 0.86, 0.82, 0.70, 0.38, 0.86),
        ( 0.34, -0.04, 1.50, 0.86, 0.82, 0.72, 0.52, 0.94),
        ( 0.00, -0.28, 1.54, 0.78, 0.76, 0.66, 0.50, 0.92),
        ( 0.02,  0.28, 1.58, 0.78, 0.76, 0.66, 0.54, 0.94),
        ( 0.04,  0.02, 1.92, 0.74, 0.72, 0.62, 0.68, 0.99),
    ]
    segs, rings = 9, 5
    for i, (dx, dy, dz, rx, ry, rz, pv0, pv1) in enumerate(bough_specs):
        ang = seed * 1.7 + i * 0.6
        ca, sa = math.cos(ang), math.sin(ang)
        rx_rot = dx * ca - dy * sa
        ry_rot = dx * sa + dy * ca
        bcx = cx + rx_rot * scale
        bcy = cy + ry_rot * scale
        bcz = z0 + dz * scale

        verts = []
        faces = []
        for r_i in range(rings + 1):
            v_frac = r_i / float(rings)
            phi = (v_frac - 0.5) * math.pi
            cp, sp = math.cos(phi), math.sin(phi)
            for s_i in range(segs):
                theta = (s_i / float(segs)) * 2.0 * math.pi
                ct, st = math.cos(theta), math.sin(theta)
                wob = 1.0 + 0.16 * math.sin(theta * 3.0 + phi * 2.0 + seed + i) * math.cos(theta * 2.0 - phi * 3.0)
                verts.append([
                    bcx + rx * scale * cp * ct * wob,
                    bcy + ry * scale * cp * st * wob,
                    bcz + rz * scale * sp * wob,
                ])
        for r_i in range(rings):
            for s_i in range(segs):
                s_next = (s_i + 1) % segs
                a = r_i * segs + s_i
                b = r_i * segs + s_next
                c = (r_i + 1) * segs + s_next
                d = (r_i + 1) * segs + s_i
                faces.append([a, b, c])
                faces.append([a, c, d])
        mb.add_faceted_normal_mapped(
            verts,
            faces,
            3,
            u0=u_min,
            u1=u_max,
            v0=pv0,
            v1=pv1,
            z_min=bcz - rz * scale,
            z_max=bcz + rz * scale,
        )


def add_tiered_spruce_pine(mb, ao_stamps, cx, cy, z0, scale=1.0, tiers=6, seed=0.0):
    """
    6-Tiered, drooping, scalloped 3D Highland Spruce / Pine tree (`Strip 3`, `u in [0.02..0.23]`)
    normal-mapped via `add_faceted_normal_mapped`!
    """
    ao_stamps.append((cx, cy, 1.05 * scale, 0.44))
    mb.add(*make_cylinder_or_cone(cx, cy, z0 - 0.15, 0.18 * scale, 0.08 * scale, 0.65 * scale, 6, 1, 0.72, 0.95, 0.10, 0.80, cap_top=False))
    segs = 12
    ang = np.linspace(0.0, 2.0 * math.pi, segs, endpoint=False, dtype=np.float32) + seed
    ca, sa = np.cos(ang), np.sin(ang)

    for t_i in range(tiers):
        frac = t_i / float(max(1, tiers - 1))
        tz0 = z0 + (0.20 + t_i * 0.40) * scale
        th = (0.92 - 0.09 * t_i) * scale
        r_bot = (1.06 - 0.14 * t_i) * scale
        r_top = 0.04 * scale if t_i == tiers - 1 else (0.22 - 0.03 * t_i) * scale
        v_bot = 0.06 + 0.14 * frac
        v_top = 0.55 + 0.42 * frac

        verts = []
        faces = []
        for i in range(segs):
            scallop = 1.0 + 0.18 * math.cos(i * math.pi)
            droop = -0.06 * scale * (1.0 if i % 2 == 0 else 0.0)
            verts.append([cx + r_bot * scallop * ca[i], cy + r_bot * scallop * sa[i], tz0 + droop])
            verts.append([cx + r_top * ca[i], cy + r_top * sa[i], tz0 + th])
        for i in range(segs):
            i_next = (i + 1) % segs
            b0 = i * 2
            b1 = i_next * 2
            faces.append([b0, b1, b1 + 1])
            faces.append([b0, b1 + 1, b0 + 1])
        mb.add_faceted_normal_mapped(verts, faces, 3, u0=0.02, u1=0.23, v0=v_bot, v1=v_top, z_min=tz0 - 0.1, z_max=tz0 + th)


def add_golden_crystal_lotus(mb, light_stamps, cx, cy, z0, scale=1.0, seed=0.0):
    light_stamps.append((cx, cy, 1.45 * scale, 0.56))
    mb.add(*make_dome_puff(cx, cy, z0 + 0.22 * scale, 0.24 * scale, 0.24 * scale, 0.28 * scale, 6, 4, 6, 0.42, 0.68, 0.65, 0.98, seed=seed))

    for ring_i, (n_blades, r_base, r_tip, h_tip, z_mid) in enumerate([
        (8, 0.16, 0.82, 0.96, 0.52),
        (6, 0.09, 0.46, 1.24, 0.68),
    ]):
        for b_i in range(n_blades):
            ang = seed + b_i * (2.0 * math.pi / n_blades) + ring_i * 0.45
            ca, sa = math.cos(ang), math.sin(ang)
            pa, qa = -sa, ca
            w_b = 0.16 * scale
            w_m = 0.21 * scale
            r_m = 0.52 * (r_base + r_tip) * scale

            v0 = [cx + (r_base * scale) * ca - w_b * pa, cy + (r_base * scale) * sa - w_b * qa, z0 - 0.05]
            v1 = [cx + (r_base * scale) * ca + w_b * pa, cy + (r_base * scale) * sa + w_b * qa, z0 - 0.05]
            v2 = [cx + r_m * ca + w_m * pa, cy + r_m * sa + w_m * qa, z0 + z_mid * scale]
            v3 = [cx + r_m * ca - w_m * pa, cy + r_m * sa - w_m * qa, z0 + z_mid * scale]
            v4 = [cx + (r_tip * scale) * ca, cy + (r_tip * scale) * sa, z0 + h_tip * scale]

            verts = np.array([v0, v1, v2, v3, v4], dtype=np.float32)
            uv_loc = np.array([
                [0.40, 0.10], [0.68, 0.10], [0.68, 0.62], [0.40, 0.62], [0.54, 0.98]
            ], dtype=np.float32)
            faces = np.array([
                [0, 1, 2], [0, 2, 3], [3, 2, 4],
                [0, 2, 1], [0, 3, 2], [3, 4, 2],
            ], dtype=np.int32)
            mb.add(verts, strip_uv(6, uv_loc[:, 0], uv_loc[:, 1]), faces)


def add_wildflower_clump(mb, cx, cy, z0, scale=1.0, color_type=0, seed=0.0):
    fu0 = 0.56 if color_type == 0 else (0.71 if color_type == 1 else 0.86)
    fu1 = fu0 + 0.12
    if _QUAT_CACHE is not None and int(abs(seed * 10)) % 2 == 0:
        fl_key = f"Flower_{(int(abs(seed * 7)) % 3) + 1}_Clump"
        place_quaternius_asset(mb, fl_key, cx, cy, z0, (0.48 * scale, 0.48 * scale, 0.34 * scale), yaw=seed * 2.1, strip_idx=7, u0=fu0, u1=fu1, v0=0.25, v1=0.98, sink_z=0.04)
    else:
        mb.add(*make_dome_puff(cx, cy, z0 + 0.07 * scale, 0.34 * scale, 0.32 * scale, 0.18 * scale, 6, 3, 7, 0.36, 0.53, 0.15, 0.85, seed=seed))
        for f_i in range(4):
            ang = seed * 2.1 + f_i * 1.57
            fx = cx + 0.15 * scale * math.cos(ang)
            fy = cy + 0.15 * scale * math.sin(ang)
            fz = z0 + (0.18 + 0.04 * (f_i % 2)) * scale
            mb.add(*make_dome_puff(fx, fy, fz, 0.12 * scale, 0.12 * scale, 0.09 * scale, 5, 3, 7, fu0, fu1, 0.35, 0.98, seed=seed + f_i))


_RUIN_STATUES = [
    r"nature_kit\Models\OBJ format\statue_obelisk.obj",
    r"nature_kit\Models\OBJ format\statue_columnDamaged.obj",
    r"nature_kit\Models\OBJ format\statue_block.obj",
]


def add_botanical_ruin_cluster(mb, ao_stamps, light_stamps, cx, cy, z0, scale=1.0, has_tall_pillar=True, has_crystal=True, n_pines=2, seed=0.0):
    ao_stamps.append((cx, cy, 2.05 * scale, 0.56))
    yaw = (seed * 1.37) % (2.0 * math.pi)

    # 1. Stepped Ancient Carved Spiral-Rune Sandstone Ruin + Designer Obelisk/Column
    mb.add(*make_frustum(cx, cy, z0 - 0.15, 1.40 * scale, 1.22 * scale, 1.24 * scale, 1.08 * scale, 0.48 * scale, 0, 0.55, 0.96, 0.10, 0.55, yaw=yaw))
    mb.add(*make_frustum(cx + 0.06 * scale, cy - 0.04 * scale, z0 + 0.30 * scale, 1.00 * scale, 0.86 * scale, 0.86 * scale, 0.74 * scale, 0.44 * scale, 0, 0.55, 0.96, 0.25, 0.78, yaw=yaw + 0.08))
    if has_tall_pillar:
        mb.add(*make_frustum(cx - 0.10 * scale, cy + 0.05 * scale, z0 + 0.70 * scale, 0.68 * scale, 0.60 * scale, 0.56 * scale, 0.50 * scale, 0.54 * scale, 0, 0.55, 0.96, 0.35, 0.95, yaw=yaw - 0.06))
        st_path = _RUIN_STATUES[int(abs(seed * 7)) % len(_RUIN_STATUES)]
        ang_st = yaw + 0.85
        sx_st = cx + 0.72 * scale * math.cos(ang_st)
        sy_st = cy + 0.72 * scale * math.sin(ang_st)
        place_kit_asset(mb, st_path, sx_st, sy_st, z0, (0.64 * scale, 0.52 * scale, 0.88 * scale), yaw=ang_st, roof_override="ruin", sink_z=0.08)

    # 2. Ring of 7 Quaternius High-Detail Sculpted Rocks + Chiseled Boulders around base
    for b_i in range(7):
        b_ang = seed + b_i * (2.0 * math.pi / 7.0) + 0.22 * math.sin(b_i * 2.3)
        b_dist = (0.76 + 0.36 * ((b_i * 7) % 3) * 0.5) * scale
        bx = cx + b_dist * math.cos(b_ang)
        by = cy + b_dist * math.sin(b_ang)
        br = (0.52 + 0.22 * ((b_i * 3) % 3) * 0.5) * scale
        if b_i % 2 == 0 and _QUAT_CACHE is not None:
            rk_key = f"Rock_{((int(abs(seed * 5)) + b_i) % 5) + 1}"
            place_quaternius_asset(mb, rk_key, bx, by, z0, (br * 1.18, br * 1.02, br * 0.72), yaw=b_ang, strip_idx=0, u0=0.03, u1=0.49, v0=0.05, v1=0.86, sink_z=0.08)
        else:
            add_chiseled_sandstone_boulder(mb, bx, by, z0 - 0.08, br * 1.15, br * 0.95, br * 0.68, yaw=b_ang, seed=seed + b_i)

    # 3. Compact low-hugging dark-emerald dwarf spruce pines
    for p_i in range(n_pines):
        p_ang = seed + 1.2 + p_i * 2.15
        p_dist = (0.90 + 0.22 * p_i) * scale
        px = cx + p_dist * math.cos(p_ang)
        py = cy + p_dist * math.sin(p_ang)
        p_sc = (0.38 + 0.12 * ((p_i + int(seed * 3)) % 2)) * scale
        add_tiered_spruce_pine(mb, ao_stamps, px, py, z0, scale=p_sc, tiers=5, seed=seed + p_i)

    # 4. Radiant Golden Crystal-Lotus Plant
    if has_crystal:
        c_ang = seed - 0.95
        gx = cx + 0.86 * scale * math.cos(c_ang)
        gy = cy + 0.86 * scale * math.sin(c_ang)
        add_golden_crystal_lotus(mb, light_stamps, gx, gy, z0, scale=0.70 * scale, seed=seed)

    # 5. Vibrant Multi-Color Wildflower Garland around the cluster
    n_fl = int(10 * scale) + 4
    for f_i in range(n_fl):
        f_ang = seed * 0.7 + f_i * (2.0 * math.pi / n_fl) + 0.2 * math.cos(f_i * 3.1)
        f_dist = (0.85 + 0.60 * ((f_i * 5) % 4) / 3.0) * scale
        fx = cx + f_dist * math.cos(f_ang)
        fy = cy + f_dist * math.sin(f_ang)
        c_type = f_i % 3
        add_wildflower_clump(mb, fx, fy, z0, scale=0.68 + 0.20 * (f_i % 2), color_type=c_type, seed=seed + f_i * 0.9)


def build_continuous_eroded_alpine_mountain_range(mb):
    """
    Builds a continuous, domain-warped Ridged-Multifractal + V-Valley Eroded Alpine Mountain Range
    (`180 x 56` grid, `X in [-25.5..+9.5], Y in [11.6..19.2]`) with 6 jagged, serrated Alpine
    massifs across the northern horizon.
    - Uses domain-warped coordinates `(wx, wy)` so the knife-edge arêtes and glacial couloirs are
      100% organic and non-periodic in both perspective and top-down (`Numpad 7`) views.
    - Maps protruding 3D rock ribs to craggy granite/slate rock UVs and 3D gullies/couloirs + summit
      crests to snow UVs across all 6 peaks.
    """
    NX_M, NY_M = 180, 56
    xs = np.linspace(-25.5, 9.5, NX_M, dtype=np.float32)
    ys = np.linspace(11.6, 19.2, NY_M, dtype=np.float32)
    gx, gy = np.meshgrid(xs, ys)

    # Non-periodic domain warp so arêtes wind organically (ZERO repeating grid in top view!)
    warp_x = multi_scale_organic_noise(NY_M, NX_M, seed=711, scales=(22, 11, 5), weights=(0.55, 0.30, 0.15))
    warp_y = multi_scale_organic_noise(NY_M, NX_M, seed=712, scales=(22, 11, 5), weights=(0.55, 0.30, 0.15))
    n_org  = multi_scale_organic_noise(NY_M, NX_M, seed=713, scales=(14, 7, 3),  weights=(0.50, 0.35, 0.15))

    wx = gx + 1.35 * warp_x
    wy = gy + 1.05 * warp_y

    # Continuous sinuous main Alpine divide crest line along the northern horizon
    y_crest = 15.65 + 0.78 * np.sin(wx * 0.23 + 0.45) - 0.48 * np.cos(wx * 0.41 - 0.25)
    dy_c = np.abs(wy - y_crest)

    # Sharp tent-ridge cross-section (steep knife-edge crest, sweeping glacial flanks)
    ridge_env = np.clip(1.0 - (dy_c / 3.95) ** 1.18, 0.0, 1.0)

    # 6 jagged Alpine massifs along the crest line (with sharp pyramidal + Gaussian hybrid profile)
    peak_list = [
        (-20.4,  9.4, 2.55),
        (-15.0, 12.4, 2.85),
        ( -9.8, 11.2, 2.65),
        ( -4.6, 10.0, 2.50),
        (  0.6, 10.4, 2.55),
        (  5.6,  8.8, 2.35),
    ]
    skyline_h = np.full_like(gx, 3.2)
    for px, ph, pw in peak_list:
        dx_p = np.abs(wx - px) / pw
        pyr = np.clip(1.0 - dx_p * 0.62, 0.0, 1.0) ** 1.35
        gau = np.exp(-(dx_p ** 2))
        skyline_h = np.maximum(skyline_h, ph * (0.55 * pyr + 0.45 * gau))

    # Carve V-shaped glacial passes/cols between the 6 peaks
    col_xs = [-17.7, -12.4, -7.2, -2.0, 3.1]
    valley_carve = np.ones_like(gx)
    for cx_v in col_xs:
        valley_carve *= 1.0 - 0.34 * np.exp(-(((wx - cx_v) / 1.35) ** 2))

    # Multi-octave domain-warped diagonal knife-edge arêtes & couloirs
    r_oct1 = 1.0 - np.abs(np.sin(wx * 0.72 + wy * 0.54 + 0.4) * np.cos(wx * 0.48 - wy * 0.68 - 0.2))
    r_oct2 = 1.0 - np.abs(np.sin(wx * 1.48 - wy * 1.12 + 1.1) * np.cos(wx * 1.15 + wy * 1.34 - 0.7))
    r_oct3 = 1.0 - np.abs(np.sin(wx * 2.85 + wy * 2.25 - 0.5))
    r_org  = 1.0 - np.abs(n_org)

    ridged_detail = (
        0.42 * (r_oct1 ** 1.45)
        + 0.28 * (r_oct2 ** 1.55)
        + 0.14 * (r_oct3 ** 1.65)
        + 0.16 * (r_org ** 1.40)
    )

    # Clip cleanly to the island's northern arc so Numpad 7 top view has a natural curved silhouette
    arc_d = island_signed_distance(gx, gy * 0.89)
    arc_taper = np.clip((1.03 - arc_d) / 0.12, 0.0, 1.0)
    east_taper = np.clip((9.2 - gx) / 2.2, 0.0, 1.0)
    west_taper = np.clip((gx + 25.2) / 2.0, 0.0, 1.0)

    z_base = sample_ground_z(gx, np.minimum(gy, 14.0)) - 0.35
    mtn_h = skyline_h * valley_carve * ridge_env * (0.64 + 0.52 * ridged_detail) * arc_taper * east_taper * west_taper
    gz = z_base + mtn_h

    # Local altitude normalized per peak so all 6 peaks get snow summits + couloirs, while protruding
    # 3D arête ribs (`rock_rib`) expose dark slate-blue / warm granite rock just like Target Images 1 & 2!
    alt_frac = np.clip(mtn_h / (skyline_h * valley_carve + 0.45), 0.0, 1.0)
    rock_rib = 0.58 * (r_oct2 ** 1.55) + 0.42 * (r_oct3 ** 1.65)
    couloir_gully = 1.0 - (r_oct1 ** 1.35)
    v_norm_local = np.clip(
        alt_frac * 0.82 - 0.34 * rock_rib * (1.0 - 0.35 * (alt_frac ** 3)) + 0.22 * couloir_gully * alt_frac,
        0.04,
        0.98,
    )

    verts = np.stack([gx, gy, gz], axis=-1).reshape(-1, 3)
    v_custom = v_norm_local.reshape(-1)
    faces = []
    for j in range(NY_M - 1):
        for i in range(NX_M - 1):
            if mtn_h[j, i] < 0.12 and mtn_h[j + 1, i + 1] < 0.12:
                continue
            a = j * NX_M + i
            if (i + j) % 2 == 0:
                faces.append([a, a + 1, a + NX_M + 1])
                faces.append([a, a + NX_M + 1, a + NX_M])
            else:
                faces.append([a, a + 1, a + NX_M])
                faces.append([a + 1, a + NX_M + 1, a + NX_M])

    mb.add_faceted_normal_mapped(
        verts,
        np.array(faces, dtype=np.int32),
        4,
        u0=0.03,
        u1=0.97,
        v0=0.04,
        v1=0.98,
        custom_v_per_vert=v_custom,
    )


def build_all_11_zones():
    print("[1/6] Assembling All 11 Semantic 3D Zones from Open-Source Designer Kits & Eroded Terrain...")
    builders = {k: MeshBuilder() for k in ZONE_NAMES}
    ao_stamps = []
    light_stamps = []

    # =========================================================================
    # ZONE 01: Continuous Quad-Grid Golden Meadow Terrain (`Zone01_Terrain_GoldenMeadow_And_Paths`)
    # =========================================================================
    NX, NY = 240, 180
    xs = np.linspace(-WORLD_X_MAX, WORLD_X_MAX, NX, dtype=np.float32)
    ys = np.linspace(-WORLD_Y_MAX, WORLD_Y_MAX, NY, dtype=np.float32)
    gx, gy = np.meshgrid(xs, ys)
    gz = sample_ground_z(gx, gy)
    gd = island_signed_distance(gx, gy)

    u_grid = (gx / (2.0 * WORLD_X_MAX)) + 0.5
    v_grid = 0.255 + 0.740 * ((gy / (2.0 * WORLD_Y_MAX)) + 0.5)

    v_ter = np.stack([gx, gy, gz], axis=-1).reshape(-1, 3)
    uv_ter = np.stack([u_grid, v_grid], axis=-1).reshape(-1, 2)

    f_ter = []
    for j in range(NY - 1):
        for i in range(NX - 1):
            if gd[j, i] <= 0.962 and gd[j + 1, i + 1] <= 0.962 and gd[j, i + 1] <= 0.962 and gd[j + 1, i] <= 0.962:
                if gy[j + 1, i] > 14.8 and gx[j, i] < 10.5:
                    continue
                if gy[j + 1, i] > 15.6:
                    continue
                idx = j * NX + i
                f_ter.append([idx, idx + 1, idx + NX + 1])
                f_ter.append([idx, idx + NX + 1, idx + NX])
    builders[1].add(v_ter, uv_ter, np.array(f_ter, dtype=np.int32))

    # =========================================================================
    # ZONE 02: Interlocking Quaternius Sculpted Cliff Boulders + Chiseled Sandstone Cutaway Rim (`Zone02_Diorama_Cutaway_RockRim_And_Base`)
    # =========================================================================
    N_RIM = 148
    angles = np.linspace(0.0, 2.0 * math.pi, N_RIM, endpoint=False)
    for idx_r, ang in enumerate(angles):
        wob = 0.032 * math.sin(ang * 3.0 + 0.4) + 0.022 * math.cos(ang * 5.0 - 0.7) + 0.012 * math.sin(ang * 9.0 + 1.2)
        rx = ISLAND_RX * (1.0 + wob) * 0.958
        ry = ISLAND_RY * (1.0 + wob) * 0.958
        ca, sa = math.cos(ang), math.sin(ang)
        denom = (abs(ca) ** 2.35 + abs(sa) ** 2.35) ** (1.0 / 2.35)
        px = rx * ca / denom
        py = ry * sa / denom

        riv_cx_r, riv_hw_r = eval_river_center_and_width(py)
        if py > 13.8 or (py < 5.0 and py > -8.6 and px > 17.5 and abs(px - riv_cx_r) < riv_hw_r * 1.05):
            continue

        z_meadow = float(sample_ground_z(px * 0.92, py * 0.92))
        z_top = min(z_meadow + 0.04, 3.05)
        h_block = max(2.05, z_top + 1.05 + 0.35 * math.sin(idx_r * 2.1))
        z_bot = z_top - h_block

        tangent_yaw = ang + math.pi * 0.5
        bw = 2.35 + 0.55 * ((idx_r * 5) % 3) * 0.5
        bd = 1.82 + 0.38 * ((idx_r * 7) % 3) * 0.5

        add_chiseled_sandstone_boulder(builders[2], px, py, z_bot, bw, bd, h_block, yaw=tangent_yaw, seed=idx_r * 1.3)
        if py < 8.8:
            out_x = px + 0.50 * ca
            out_y = py + 0.50 * sa
            if idx_r % 2 == 0 and _QUAT_CACHE is not None:
                rk_key = f"Rock_{(idx_r % 5) + 1}"
                place_quaternius_asset(builders[2], rk_key, out_x, out_y, z_bot - 0.10, (bw * 0.92, bd * 0.85, h_block * 0.68), yaw=tangent_yaw + 0.15, strip_idx=0, u0=0.03, u1=0.49, v0=0.05, v1=0.84, sink_z=0.0)
            else:
                add_chiseled_sandstone_boulder(builders[2], out_x, out_y, z_bot - 0.12, bw * 0.88, bd * 0.82, h_block * 0.62, yaw=tangent_yaw + 0.12, seed=idx_r * 2.1 + 3.0)

    # Super-ellipse underside seal plate
    seal_segs = 64
    s_ang = np.linspace(0.0, 2.0 * math.pi, seal_segs, endpoint=False)
    seal_v = [[0.0, 0.0, -0.95]]
    seal_uv = [[0.25, 0.12]]
    seal_f = []
    for i, a_s in enumerate(s_ang):
        ca, sa = math.cos(a_s), math.sin(a_s)
        denom = (abs(ca) ** 2.35 + abs(sa) ** 2.35) ** (1.0 / 2.35)
        sx_p = ISLAND_RX * 0.93 * ca / denom
        sy_p = ISLAND_RY * 0.93 * sa / denom
        seal_v.append([sx_p, sy_p, -0.95])
        seal_uv.append([0.25 + 0.15 * ca, 0.12 + 0.08 * sa])
        seal_f.append([0, 1 + ((i + 1) % seal_segs), 1 + i])
    builders[2].add(np.array(seal_v, dtype=np.float32), strip_uv(0, np.array(seal_uv)[:, 0], np.array(seal_uv)[:, 1]), np.array(seal_f, dtype=np.int32))

    # =========================================================================
    # ZONE 03: Mirror-Reflective S-Curve River, 4 Foamy Waterfalls & 3 Meadow Ponds (`Zone03_Water_River_Waterfalls_And_3Ponds`)
    # =========================================================================
    RIV_NY, RIV_NX = 72, 28
    ry_vals = np.linspace(-7.4, 12.2, RIV_NY, dtype=np.float32)
    rv_verts, rv_uvs, rv_faces = [], [], []
    for j, y_val in enumerate(ry_vals):
        rcx, rhw = eval_river_center_and_width(y_val)
        v_frac = j / float(RIV_NY - 1)
        for i in range(RIV_NX):
            u_frac = i / float(RIV_NX - 1)
            x_val = rcx + (u_frac * 2.0 - 1.0) * (rhw * 0.96)
            z_val = 2.00 + 0.08 * v_frac
            rv_verts.append([x_val, y_val, z_val])
            rv_uvs.append([0.02 + 0.41 * u_frac, 0.05 + 0.90 * v_frac])
    for j in range(RIV_NY - 1):
        for i in range(RIV_NX - 1):
            a = j * RIV_NX + i
            cx_q = 0.5 * (rv_verts[a][0] + rv_verts[a + RIV_NX + 1][0])
            cy_q = 0.5 * (rv_verts[a][1] + rv_verts[a + RIV_NX + 1][1])
            if island_signed_distance(cx_q, cy_q) <= 0.958:
                rv_faces.append([a, a + 1, a + RIV_NX + 1])
                rv_faces.append([a, a + RIV_NX + 1, a + RIV_NX])
    builders[3].add(np.array(rv_verts, dtype=np.float32), strip_uv(2, np.array(rv_uvs)[:, 0], np.array(rv_uvs)[:, 1]), np.array(rv_faces, dtype=np.int32))

    wf_xs = [8.2, 11.4, 14.8, 17.8]
    wf_ws = [1.55, 1.85, 1.95, 1.45]
    for w_i, (wx, ww) in enumerate(zip(wf_xs, wf_ws)):
        wy_top = 12.6 - 0.15 * abs(w_i - 1.5)
        wy_bot = 11.2 - 0.15 * abs(w_i - 1.5)
        z_top = 7.38
        z_bot = 2.02
        n_seg_wf = 10
        w_verts, w_uvs, w_faces = [], [], []
        for s_i in range(n_seg_wf + 1):
            vf = s_i / float(n_seg_wf)
            y_cur = wy_bot + (wy_top - wy_bot) * (vf ** 0.75)
            z_cur = z_bot + (z_top - z_bot) * vf + 0.22 * math.sin(vf * math.pi)
            w_cur = ww * (1.22 - 0.22 * vf)
            w_verts.append([wx - w_cur * 0.5, y_cur, z_cur])
            w_uvs.append([0.80, 0.06 + 0.88 * vf])
            w_verts.append([wx + w_cur * 0.5, y_cur, z_cur])
            w_uvs.append([0.98, 0.06 + 0.88 * vf])
        for s_i in range(n_seg_wf):
            b0 = s_i * 2
            w_faces.extend([[b0, b0 + 1, b0 + 3], [b0, b0 + 3, b0 + 2]])
        builders[3].add(np.array(w_verts, dtype=np.float32), strip_uv(2, np.array(w_uvs)[:, 0], np.array(w_uvs)[:, 1]), np.array(w_faces, dtype=np.int32))
        builders[3].add(*make_dome_puff(wx, wy_bot - 0.25, 2.04, ww * 0.95, 0.88, 0.26, 8, 3, 2, 0.80, 0.98, 0.06, 0.32, seed=w_i))
        builders[3].add(*make_dome_puff(wx - ww * 0.35, wy_bot - 0.35, 2.04, ww * 0.55, 0.55, 0.18, 8, 3, 2, 0.80, 0.98, 0.06, 0.32, seed=w_i + 10))
        builders[3].add(*make_dome_puff(wx + ww * 0.35, wy_bot - 0.35, 2.04, ww * 0.55, 0.55, 0.18, 8, 3, 2, 0.80, 0.98, 0.06, 0.32, seed=w_i + 20))

    pond_specs = [
        (-3.4, -5.2, 5.6, 1.72, -31.0, 2.34),
        (-0.5,  0.6, 3.5, 1.35,  18.0, 2.56),
        ( 9.6, -7.4, 3.3, 1.32, -19.0, 2.14),
    ]
    for p_i, (pcx, pcy, prx, pry, pang_deg, pz) in enumerate(pond_specs):
        pang = math.radians(pang_deg)
        ca, sa = math.cos(pang), math.sin(pang)
        p_rings, p_segs = 6, 24
        pv, puv, pf = [], [], []
        for r_i in range(p_rings + 1):
            rf = r_i / float(p_rings)
            for s_i in range(p_segs + 1):
                sf = s_i / float(p_segs)
                th = sf * 2.0 * math.pi
                wob = 1.0 + 0.10 * math.sin(th * 3.0 + p_i)
                lx = prx * rf * wob * math.cos(th)
                ly = pry * rf * wob * math.sin(th)
                wx = pcx + lx * ca - ly * sa
                wy = pcy + lx * sa + ly * ca
                pv.append([wx, wy, pz])
                u_p = 0.61 + 0.14 * rf * math.cos(th)
                v_p = 0.50 + 0.42 * rf * math.sin(th)
                puv.append([u_p, v_p])
        stride = p_segs + 1
        for r_i in range(p_rings):
            for s_i in range(p_segs):
                a = r_i * stride + s_i
                pf.extend([[a, a + 1, a + stride + 1], [a, a + stride + 1, a + stride]])
        builders[3].add(np.array(pv, dtype=np.float32), strip_uv(2, np.array(puv)[:, 0], np.array(puv)[:, 1]), np.array(pf, dtype=np.int32))

        for b_k in range(22):
            th_b = b_k * (2.0 * math.pi / 22.0)
            wob = 1.05 + 0.10 * math.sin(th_b * 3.0 + p_i)
            lx = prx * wob * math.cos(th_b)
            ly = pry * wob * math.sin(th_b)
            bx = pcx + lx * ca - ly * sa
            by = pcy + lx * sa + ly * ca
            bz = max(pz - 0.04, float(sample_ground_z(bx, by)))
            if _QUAT_CACHE is not None and b_k % 2 == 0:
                place_quaternius_asset(builders[11], f"Rock_{((p_i + b_k) % 5) + 1}", bx, by, bz, (0.64, 0.54, 0.36), yaw=th_b, strip_idx=0, u0=0.03, u1=0.49, v0=0.05, v1=0.85, sink_z=0.06)
            else:
                add_chiseled_sandstone_boulder(builders[11], bx, by, bz - 0.08, 0.62, 0.52, 0.36, yaw=th_b, seed=p_i * 10 + b_k)
            if b_k % 2 == 0:
                add_wildflower_clump(builders[11], bx * 0.99, by * 0.99, bz, scale=0.72, color_type=(b_k // 2) % 3, seed=b_k)
            if b_k % 5 == 0:
                add_tiered_spruce_pine(builders[11], ao_stamps, bx + 0.52 * math.cos(th_b), by + 0.52 * math.sin(th_b), bz, scale=0.42, tiers=5, seed=b_k)
        add_golden_crystal_lotus(builders[11], light_stamps, pcx + prx * 0.85 * ca, pcy + prx * 0.85 * sa, max(pz + 0.02, float(sample_ground_z(pcx + prx * 0.85 * ca, pcy + prx * 0.85 * sa))), scale=0.80, seed=p_i)

    # =========================================================================
    # ZONE 04: Farmstead Village (`Zone04_Building_KhuChuongTrai_Farmstead`)
    #   Delicate miniature diorama cottage proportions nestled among golden trees!
    # =========================================================================
    farm_houses = [
        (-17.8, 5.4, 2.25, 1.70, 1.18, 1.28,  0.14, "terracotta"),
        (-17.0, 9.2, 2.50, 1.90, 1.32, 1.42,  0.08, "terracotta"),
        (-13.4, 9.4, 2.35, 1.78, 1.25, 1.35, -0.06, "slate_blue"),
    ]
    for hx, hy, hsx, hsy, hwh, hrh, hyaw, hrstyle in farm_houses:
        hz = float(sample_ground_z(hx, hy))
        add_steep_gabled_fantasy_house(builders[4], ao_stamps, light_stamps, hx, hy, hz, sx=hsx, sy=hsy, wall_h=hwh, roof_h=hrh, yaw=hyaw, roof_style=hrstyle, has_cross_wing=True, has_chimney=True, has_dormers=True, has_porch=True)

    for prop_rel, px, py, p_sz, pyaw in [
        (r"KayKit-Medieval-Hexagon-Pack\addons\kaykit_medieval_hexagon_pack\Assets\obj\decoration\props\wheelbarrow.obj", -16.0, 4.8, (0.68, 0.40, 0.40), 0.5),
        (r"KayKit-Medieval-Hexagon-Pack\addons\kaykit_medieval_hexagon_pack\Assets\obj\decoration\props\barrel.obj", -16.6, 5.1, (0.38, 0.38, 0.46), 0.0),
        (r"KayKit-Medieval-Hexagon-Pack\addons\kaykit_medieval_hexagon_pack\Assets\obj\decoration\props\sack.obj", -16.2, 5.2, (0.40, 0.34, 0.36), 0.8),
        (r"fantasy_town_kit\Models\OBJ format\cart-high.obj", -14.5, 6.2, (0.95, 0.65, 0.72), -0.3),
    ]:
        pz = float(sample_ground_z(px, py))
        place_kit_asset(builders[4], prop_rel, px, py, pz, target_size=p_sz, yaw=pyaw, sink_z=0.04)

    fence_runs = [
        (-20.2, 3.6, -14.2, 3.8, 7),
        (-10.8, 4.2,  -4.8, 4.2, 7),
    ]
    for fx0, fy0, fx1, fy1, n_posts in fence_runs:
        f_yaw = math.atan2(fy1 - fy0, fx1 - fx0)
        seg_len = math.hypot(fx1 - fx0, fy1 - fy0) / float(n_posts - 1)
        for p_i in range(n_posts):
            t_p = p_i / float(n_posts - 1)
            px = fx0 + (fx1 - fx0) * t_p
            py = fy0 + (fy1 - fy0) * t_p
            pz = float(sample_ground_z(px, py))
            builders[4].add(*make_box(px, py, pz - 0.08, 0.13, 0.13, 0.62, 1, 0.72, 0.95, 0.15, 0.85, yaw=f_yaw))
            if p_i % 2 == 0:
                builders[4].add(*make_box(px, py, pz + 0.54, 0.15, 0.15, 0.15, 1, 0.42, 0.62, 0.25, 0.90, yaw=f_yaw))
            if p_i < n_posts - 1:
                mx = px + (fx1 - fx0) / float(n_posts - 1) * 0.5
                my = py + (fy1 - fy0) / float(n_posts - 1) * 0.5
                mz = float(sample_ground_z(mx, my))
                builders[4].add(*make_box(mx, my, mz + 0.16, seg_len, 0.06, 0.06, 1, 0.72, 0.95, 0.20, 0.80, yaw=f_yaw))
                builders[4].add(*make_box(mx, my, mz + 0.36, seg_len, 0.06, 0.06, 1, 0.72, 0.95, 0.20, 0.80, yaw=f_yaw))

    # =========================================================================
    # ZONE 05: Research Observatory Manor (`Zone05_Building_NhaNghienCuu_ResearchObservatory`)
    # =========================================================================
    obs_x, obs_y = -7.8, 6.4
    obs_z = float(sample_ground_z(obs_x, obs_y))
    observatory = build_observatory()
    builders[5].add(*observatory.arrays((obs_x, obs_y, obs_z), yaw=-0.05))
    ao_stamps.append((obs_x, obs_y, 2.85 * 0.78, 0.58))
    light_stamps.append((obs_x, obs_y - 2.05 * 0.65, 2.85 * 0.65, 0.48))

    # =========================================================================
    # ZONE 06: NPC Village Cluster (`Zone06_Building_NhaCuongHoa_And_KhuNPC`)
    # =========================================================================
    npc_houses = [
        (-0.6, 8.0, 2.15, 1.62, 1.18, 1.28, -0.18, "terracotta"),
        ( 2.4, 7.4, 2.05, 1.54, 1.14, 1.24, -0.24, "terracotta"),
        ( 1.0, 9.6, 2.22, 1.68, 1.24, 1.34, -0.10, "terracotta"),
    ]
    for hx, hy, hsx, hsy, hwh, hrh, hyaw, hrstyle in npc_houses:
        hz = float(sample_ground_z(hx, hy))
        add_steep_gabled_fantasy_house(builders[6], ao_stamps, light_stamps, hx, hy, hz, sx=hsx, sy=hsy, wall_h=hwh, roof_h=hrh, yaw=hyaw, roof_style=hrstyle, has_cross_wing=True, has_chimney=True, has_dormers=True, has_porch=True)

    # =========================================================================
    # ZONE 07: Slender Information Beacon Lighthouse (`Zone07_Landmark_TramThongTin_InfoBeacon`)
    # =========================================================================
    bx_lh, by_lh = 8.6, 4.6
    bz_lh = float(sample_ground_z(bx_lh, by_lh))
    ao_stamps.append((bx_lh, by_lh, 2.5, 0.58))
    light_stamps.append((bx_lh, by_lh, 2.4, 0.55))

    if _QUAT_CACHE is not None:
        for r_i in range(7):
            ang_r = r_i * (2.0 * math.pi / 7.0)
            rx_h = bx_lh + 1.45 * math.cos(ang_r)
            ry_h = by_lh + 1.25 * math.sin(ang_r)
            rz_h = float(sample_ground_z(rx_h, ry_h))
            place_quaternius_asset(builders[7], f"Rock_{(r_i % 5) + 1}", rx_h, ry_h, rz_h, (1.02, 0.86, 0.62), yaw=ang_r, strip_idx=0, u0=0.03, u1=0.49, v0=0.05, v1=0.85, sink_z=0.10)

    builders[7].add(*make_cylinder_or_cone(bx_lh, by_lh, bz_lh - 0.30, 1.85, 1.68, 0.52, 12, 6, 0.74, 0.98, 0.08, 0.45, cap_top=True))
    builders[7].add(*make_cylinder_or_cone(bx_lh, by_lh, bz_lh + 0.20, 1.38, 1.25, 0.45, 12, 6, 0.74, 0.98, 0.20, 0.58, cap_top=True))
    builders[7].add(*make_cylinder_or_cone(bx_lh, by_lh, bz_lh + 0.64, 0.96, 0.70, 3.05, 12, 6, 0.74, 0.98, 0.25, 0.96, cap_top=True))
    for wz_off in (1.10, 2.02):
        builders[7].add(*make_cylinder_or_cone(bx_lh, by_lh, bz_lh + wz_off, 0.92 if wz_off < 1.8 else 0.80, 0.88 if wz_off < 1.8 else 0.76, 0.40, 12, 1, 0.40, 0.66, 0.15, 0.90, cap_top=False))
    builders[7].add(*make_cylinder_or_cone(bx_lh, by_lh, bz_lh + 3.62, 0.92, 0.98, 0.28, 12, 6, 0.74, 0.98, 0.60, 0.98, cap_top=True))
    for c_i in range(6):
        c_ang = c_i * (math.pi / 3.0)
        builders[7].add(*make_cylinder_or_cone(bx_lh + 0.60 * math.cos(c_ang), by_lh + 0.60 * math.sin(c_ang), bz_lh + 3.88, 0.08, 0.08, 0.78, 6, 6, 0.74, 0.98, 0.50, 0.95, cap_top=False))
    builders[7].add(*make_dome_puff(bx_lh, by_lh, bz_lh + 4.35, 0.60, 0.60, 0.62, 14, 8, 6, 0.02, 0.34, 0.15, 0.99, seed=0.0))

    # =========================================================================
    # ZONE 08: 4-Arch Stone Bridge & Medieval Harbor Town (`Zone08_Vista_StoneBridge_And_HarborTown`)
    # =========================================================================
    br_y = 6.2
    br_x0, br_x1 = 9.8, 19.2
    br_len = br_x1 - br_x0
    builders[8].add(*make_box(0.5 * (br_x0 + br_x1), br_y, 2.85, br_len, 2.12, 0.36, 6, 0.74, 0.98, 0.25, 0.88))
    for py_off in (-0.96, 0.96):
        builders[8].add(*make_box(0.5 * (br_x0 + br_x1), br_y + py_off, 3.20, br_len, 0.22, 0.40, 6, 0.74, 0.98, 0.40, 0.95))
    for p_i in range(5):
        px_b = br_x0 + p_i * (br_len / 4.0)
        place_kit_asset(builders[8], r"castle_kit\Models\OBJ format\bridge-straight-pillar.obj", px_b, br_y, 1.15, (1.08, 2.45, 1.82), yaw=0.0, roof_override="ruin", sink_z=0.0)
        for py_off in (-0.96, 0.96):
            builders[8].add(*make_box(px_b, br_y + py_off, 3.60, 0.30, 0.30, 0.45, 6, 0.74, 0.98, 0.45, 0.95))
            builders[8].add(*make_dome_puff(px_b, br_y + py_off, 4.18, 0.20, 0.20, 0.24, 6, 4, 1, 0.42, 0.64, 0.35, 0.98))
        if p_i < 4:
            arch_cx = px_b + (br_len / 8.0)
            for a_s in (-0.62, 0.62):
                builders[8].add(*make_box(arch_cx + a_s, br_y, 2.38, 0.62, 2.10, 0.50, 0, 0.55, 0.95, 0.25, 0.85))

    harbor_houses = [
        (20.2, 4.4, 2.25, 1.72, 1.42, 1.52, -0.32, "terracotta"),
        (22.2, 3.0, 2.32, 1.78, 1.40, 1.50, -0.35, "slate_blue"),
        (24.2, 1.6, 2.18, 1.65, 1.35, 1.45, -0.38, "terracotta"),
        (26.0, 0.2, 2.10, 1.58, 1.28, 1.38, -0.40, "terracotta"),
        (21.6, 6.2, 2.35, 1.80, 1.48, 1.58, -0.25, "terracotta"),
        (23.6, 4.6, 2.42, 1.85, 1.52, 1.62, -0.30, "slate_blue"),
        (25.4, 3.2, 2.18, 1.66, 1.38, 1.48, -0.32, "terracotta"),
    ]
    for hx, hy, hsx, hsy, hwh, hrh, hyaw, hrstyle in harbor_houses:
        hz = max(2.35, float(sample_ground_z(hx, hy)))
        add_steep_gabled_fantasy_house(builders[8], ao_stamps, light_stamps, hx, hy, hz, sx=hsx, sy=hsy, wall_h=hwh, roof_h=hrh, yaw=hyaw, roof_style=hrstyle, has_cross_wing=True, has_chimney=True, has_dormers=True, has_porch=True)

    for tx_h, ty_h, th_base, th_spire in [(24.6, 5.6, 3.8, 2.7), (22.4, 7.6, 3.5, 2.5)]:
        tz_h = max(2.55, float(sample_ground_z(tx_h, ty_h)))
        builders[8].add(*make_box(tx_h, ty_h, tz_h - 0.20, 1.32, 1.32, th_base, 1, 0.03, 0.35, 0.15, 0.94, yaw=-0.28))
        for wz_t in (tz_h + th_base * 0.45, tz_h + th_base * 0.78):
            builders[8].add(*make_box(tx_h, ty_h, wz_t, 1.36, 1.36, 0.38, 1, 0.40, 0.66, 0.15, 0.92, yaw=-0.28))
        builders[8].add(*make_cylinder_or_cone(tx_h, ty_h, tz_h + th_base - 0.20, 1.02, 0.04, th_spire, 8, 5, 0.38, 0.68, 0.10, 0.98, cap_top=True))

    fz_p = max(2.25, float(sample_ground_z(23.2, 0.4)))
    place_kit_asset(builders[8], r"fantasy_town_kit\Models\OBJ format\fountain-round-detail.obj", 23.2, 0.4, fz_p, (1.65, 1.65, 0.88), yaw=0.0, roof_override="limestone", sink_z=0.05)

    pier_pts = [(18.2, 4.4), (19.6, 2.8), (21.2, 1.2), (22.8, -0.4), (24.4, -2.0), (25.8, -3.4)]
    for p_i in range(len(pier_pts) - 1):
        x0_p, y0_p = pier_pts[p_i]
        x1_p, y1_p = pier_pts[p_i + 1]
        mx_p, my_p = 0.5 * (x0_p + x1_p), 0.5 * (y0_p + y1_p)
        plen = math.hypot(x1_p - x0_p, y1_p - y0_p) + 0.35
        pyaw = math.atan2(y1_p - y0_p, x1_p - x0_p)
        builders[8].add(*make_box(mx_p, my_p, 2.12, plen, 1.35, 0.20, 1, 0.70, 0.96, 0.15, 0.88, yaw=pyaw))
        builders[8].add(*make_cylinder_or_cone(x0_p - 0.50, y0_p - 0.42, 1.45, 0.11, 0.11, 1.02, 6, 1, 0.72, 0.95, 0.10, 0.85, cap_top=True))
        builders[8].add(*make_dome_puff(x0_p - 0.50, y0_p - 0.42, 2.56, 0.14, 0.14, 0.16, 6, 4, 1, 0.42, 0.64, 0.35, 0.98))
        jx, jy = mx_p - 1.25, my_p - 0.78
        builders[8].add(*make_box(jx + 0.42, jy + 0.28, 2.02, 1.45, 0.58, 0.15, 1, 0.70, 0.96, 0.20, 0.85, yaw=pyaw + 1.57))
        bx_r, by_r = jx - 0.55, jy - 0.35
        place_kit_asset(builders[8], r"nature_kit\Models\OBJ format\canoe.obj", bx_r, by_r, 1.96, (1.62, 0.66, 0.38), yaw=pyaw + 0.15, sink_z=0.0)

    # =========================================================================
    # ZONE 09: Waterfall Cliff Escarpment & Soaring Fairytale Gothic Citadel (`Zone09_Vista_TwilightCastle_And_WaterfallCliff`)
    # =========================================================================
    cliff_crag_xs = [6.2, 9.6, 13.0, 16.2, 19.3, 21.2, 22.8, 24.2]
    for c_i, ccx in enumerate(cliff_crag_xs):
        ccy = 12.2 - 0.35 * max(0, c_i - 3)
        ch = 6.1 - 0.35 * max(0, c_i - 4)
        add_chiseled_sandstone_boulder(builders[9], ccx, ccy, 1.45, 3.15, 2.95, ch, yaw=0.15 * c_i, seed=c_i * 2.7)
        if _QUAT_CACHE is not None:
            place_quaternius_asset(builders[9], f"Rock_{(c_i % 5) + 1}", ccx + 0.42, ccy - 0.72, 1.45, (2.55, 2.25, ch * 0.72), yaw=-0.20 * c_i, strip_idx=0, u0=0.03, u1=0.49, v0=0.05, v1=0.86, sink_z=0.0)
        else:
            add_chiseled_sandstone_boulder(builders[9], ccx + 0.42, ccy - 0.72, 1.45, 2.35, 2.05, ch * 0.68, yaw=-0.20 * c_i, seed=c_i * 3.1 + 1.0)

    for e_i in range(6):
        ex = 24.2 + 0.45 * (e_i % 3)
        ey = 7.2 + 0.85 * e_i
        add_chiseled_sandstone_boulder(builders[9], ex, ey, 2.05, 2.85, 2.45, 4.1 + 0.4 * (e_i % 2), yaw=0.25 * e_i, seed=e_i * 4.1)

    cas_cx, cas_cy, cas_z = 14.4, 15.6, 7.35
    # Sculpted rocky crag foundation instead of a flat rectangular box under the castle
    for cr_i, cr_dx in enumerate([-3.4, -1.7, 0.0, 1.7, 3.4]):
        add_chiseled_sandstone_boulder(builders[9], cas_cx + cr_dx, cas_cy - 0.75, cas_z - 0.55, 2.45, 2.15, 1.35, yaw=0.18 * cr_i, seed=40.0 + cr_i)
    builders[9].add(*make_box(cas_cx, cas_cy + 0.55, cas_z + 0.55, 4.2, 2.1, 3.85, 1, 0.04, 0.36, 0.20, 0.96))
    builders[9].add(*make_pitched_roof(cas_cx, cas_cy + 0.55, cas_z + 4.35, 4.6, 2.4, 2.10, 5, 0.72, 0.98, 0.15, 0.98, ridge_along_x=True))

    castle_towers = [
        ( 0.0,  0.4, 0.82, 7.4, 5.1),
        (-1.4,  0.1, 0.68, 6.1, 4.2),
        ( 1.4,  0.1, 0.70, 6.3, 4.3),
        (-0.7, -0.7, 0.60, 5.2, 3.6),
        ( 0.7, -0.7, 0.60, 5.3, 3.7),
        (-2.4, -0.4, 0.58, 4.6, 3.2),
        ( 2.4, -0.4, 0.58, 4.7, 3.3),
        (-3.4, -0.8, 0.50, 3.7, 2.7),
        ( 3.4, -0.8, 0.50, 3.8, 2.8),
        (-4.3, -1.1, 0.44, 2.9, 2.2),
        ( 4.3, -1.1, 0.44, 3.0, 2.3),
        (-1.8, -1.2, 0.48, 3.6, 2.5),
        ( 1.8, -1.2, 0.48, 3.6, 2.5),
        (-2.8, -1.3, 0.42, 3.0, 2.1),
        ( 2.8, -1.3, 0.42, 3.0, 2.1),
        ( 0.0, -1.3, 0.48, 3.9, 2.7),
        (-2.9,  0.5, 0.50, 4.1, 2.8),
        ( 2.9,  0.5, 0.50, 4.2, 2.9),
        (-1.1,  1.1, 0.54, 5.0, 3.4),
        ( 1.1,  1.1, 0.54, 5.1, 3.5),
    ]
    for t_idx, (tdx, tdy, tr, tsh, tsp) in enumerate(castle_towers):
        tx = cas_cx + tdx
        ty = cas_cy + tdy
        builders[9].add(*make_cylinder_or_cone(tx, ty, cas_z + 0.35, tr, tr * 0.90, tsh, 10, 1, 0.04, 0.35, 0.15, 0.95, cap_top=False))
        for w_frac in (0.28, 0.52, 0.76):
            wz = cas_z + 0.35 + tsh * w_frac
            builders[9].add(*make_box(tx, ty - tr * 0.94, wz, tr * 0.52, 0.10, 0.28, 1, 0.42, 0.64, 0.18, 0.90))
        z_sp = cas_z + 0.35 + tsh - 0.10
        builders[9].add(*make_cylinder_or_cone(tx, ty, z_sp, tr * 1.18, tr * 0.46, tsp * 0.36, 10, 5, 0.72, 0.98, 0.10, 0.55, cap_top=False))
        builders[9].add(*make_cylinder_or_cone(tx, ty, z_sp + tsp * 0.34, tr * 0.47, 0.03, tsp * 0.68, 10, 5, 0.72, 0.98, 0.50, 0.99, cap_top=True))

    cliff_tree_coords = [
        (5.8, 12.8, 0.78, False),  (6.9, 13.4, 0.66, True),   (8.4, 13.1, 0.72, False),
        (9.8, 13.6, 0.64, True),   (10.8, 13.8, 0.74, False), (11.8, 13.6, 0.68, False),
        (12.8, 13.9, 0.62, True),  (13.8, 13.7, 0.72, False), (15.0, 13.7, 0.70, False),
        (16.0, 13.8, 0.64, True),  (17.0, 13.6, 0.74, False), (18.2, 13.3, 0.72, False),
        (19.4, 13.4, 0.76, False), (20.8, 12.6, 0.64, True),  (22.0, 12.2, 0.80, False),
        (23.4, 11.5, 0.72, False), (23.8, 9.8,  0.76, False), (24.4, 8.4,  0.66, True),
    ]
    for idx_ct, (ctx, cty, ct_sc, is_pine) in enumerate(cliff_tree_coords):
        ctz = 7.35 if cty > 12.0 else 5.85
        if is_pine:
            add_tiered_spruce_pine(builders[9], ao_stamps, ctx, cty, ctz, scale=ct_sc, tiers=6, seed=idx_ct * 1.7)
        else:
            add_chiseled_broadleaf_tree(builders[9], ao_stamps, ctx, cty, ctz, scale=ct_sc, palette_idx=(idx_ct % 3), seed=idx_ct * 2.3)

    # =========================================================================
    # ZONE 10: Continuous Snow-Streaked Alpine Mountain Range & Foothill Forest (`Zone10_Vista_AlpineMountains_And_ForestRidge`)
    # =========================================================================
    build_continuous_eroded_alpine_mountain_range(builders[10])

    # Chunky Sandstone Foothill Crags (`Quaternius Rock_1..5` + `add_chiseled_sandstone_boulder`) in front of the snow range!
    foothill_crag_spots = [
        (-20.2, 11.8, 3.6, 2.8, 3.2),
        (-15.5, 11.5, 3.8, 3.0, 3.5),
        (-10.8, 11.4, 3.4, 2.8, 3.0),
        ( -5.2, 11.6, 3.2, 2.6, 2.8),
        (  2.5, 11.6, 3.0, 2.4, 2.6),
    ]
    for c_idx, (fcx, fcy, fsx, fsy, fsz) in enumerate(foothill_crag_spots):
        fcz = float(sample_ground_z(fcx, fcy))
        add_chiseled_sandstone_boulder(builders[10], fcx, fcy, fcz - 0.30, fsx, fsy, fsz, yaw=c_idx * 0.4, seed=c_idx * 3.7)
        if _QUAT_CACHE is not None:
            place_quaternius_asset(builders[10], f"Rock_{(c_idx % 5) + 1}", fcx + 1.1, fcy - 0.5, fcz, (fsx * 0.72, fsy * 0.72, fsz * 0.68), yaw=c_idx * 0.9, strip_idx=0, u0=0.03, u1=0.49, v0=0.05, v1=0.86, sink_z=0.15)

    placed_trees = []
    building_keepouts = [
        (-17.8, 5.4, 2.7), (-17.0, 9.2, 3.0), (-13.4, 9.4, 2.8),
        (-7.8, 6.4, 3.2), (-0.6, 8.0, 2.4), (2.4, 7.4, 2.4), (1.0, 9.6, 2.5),
        (8.6, 4.6, 2.4),
    ]

    def can_place_tree(tx, ty, min_d=1.28):
        if island_signed_distance(tx, ty) > 0.93:
            return False
        if eval_road_mask(tx, ty) > 0.25:
            return False
        rcx, rhw = eval_river_center_and_width(ty)
        if ty < 12.5 and ty > -7.2 and abs(tx - rcx) < rhw + 1.05:
            return False
        if tx > 17.5 and ty > -2.5 and ty < 8.8:
            return False
        for bx, by, br in building_keepouts:
            if math.hypot(tx - bx, ty - by) < br:
                return False
        for px, py in placed_trees:
            if math.hypot(tx - px, ty - py) < min_d:
                return False
        return True

    rng = np.random.RandomState(42)
    for idx_t in range(580):
        tx = rng.uniform(-25.8, 10.8)
        ty = rng.uniform(-2.5, 14.2)
        if ty < 4.2 and tx > -18.2:
            continue
        if ty < 6.4 and tx > -15.5 and abs(tx + 11.6) > 2.8 and abs(tx + 3.4) > 2.8 and abs(tx - 5.2) > 2.6:
            continue
        if not can_place_tree(tx, ty, min_d=1.30):
            continue
        placed_trees.append((tx, ty))
        tz = float(sample_ground_z(tx, ty))
        target_zone = 10 if ty > 10.2 else 4
        pine_prob = 0.55 if ty > 12.0 else 0.30
        if rng.rand() < pine_prob:
            sc = rng.uniform(0.75, 1.15)
            add_tiered_spruce_pine(builders[target_zone], ao_stamps, tx, ty, tz, scale=sc, tiers=6, seed=idx_t * 0.7)
        else:
            sc = rng.uniform(0.82, 1.18)
            pal = 1 if rng.rand() < 0.58 else (2 if rng.rand() < 0.68 else 3)
            add_chiseled_broadleaf_tree(builders[target_zone], ao_stamps, tx, ty, tz, scale=sc, palette_idx=pal, seed=idx_t * 1.1)

    for idx_r in range(115):
        ty = rng.uniform(-7.2, 11.2)
        rcx, rhw = eval_river_center_and_width(ty)
        side = -1.0 if rng.rand() < 0.68 else 1.0
        tx = rcx + side * (rhw + rng.uniform(0.95, 2.7))
        if abs(ty - 6.2) < 1.7:
            continue
        if not can_place_tree(tx, ty, min_d=1.25):
            continue
        placed_trees.append((tx, ty))
        tz = float(sample_ground_z(tx, ty))
        if rng.rand() < 0.42:
            add_tiered_spruce_pine(builders[8], ao_stamps, tx, ty, tz, scale=rng.uniform(0.62, 0.94), tiers=5, seed=idx_r)
        else:
            add_chiseled_broadleaf_tree(builders[8], ao_stamps, tx, ty, tz, scale=rng.uniform(0.75, 1.08), palette_idx=1 + (idx_r % 2), seed=idx_r)

    # =========================================================================
    # ZONE 11: Botanical Stone-Ruin Garden Clusters, Cliff-Rim Garland & Dense 3D Meadow Carpet (`Zone11_Props_AncientRuins_And_GoldenFlora`)
    # =========================================================================
    interior_ruin_coords = [
        (-16.2, -3.2, 1.05, True,  True,  2),
        (-11.8, -8.4, 1.15, True,  True,  3),
        (-15.5, -9.8, 0.92, False, True,  2),
        (-19.5, -6.2, 0.98, True,  True,  2),
        (-18.8,  0.4, 1.00, False, True,  2),
        (-13.4,  1.2, 0.88, True,  False, 2),
        ( -8.6,  0.8, 0.92, True,  True,  2),
        ( -8.2, -3.6, 0.85, False, False, 2),
        ( -4.2,  3.2, 0.88, False, True,  2),
        (  1.8,  3.8, 0.90, True,  True,  2),
        (  4.8,  1.6, 0.98, True,  True,  2),
        (  6.8, -2.4, 1.05, True,  True,  3),
        ( 12.4, -2.8, 1.10, True,  True,  3),
        ( 15.2, -5.8, 1.04, True,  False, 2),
        (  5.2, -8.8, 0.92, True,  True,  2),
        ( -0.8, -9.6, 0.90, False, True,  2),
        ( -6.4,-11.2, 0.96, True,  True,  2),
        (  2.8,-13.4, 0.94, True,  False, 2),
        ( 10.6,-12.6, 1.00, True,  True,  2),
        ( 16.8,-10.2, 1.02, True,  True,  3),
        ( 21.4, -7.8, 0.98, False, True,  2),
    ]
    for r_i, (rcx, rcy, rsc, r_tall, r_crys, r_pines) in enumerate(interior_ruin_coords):
        if eval_road_mask(rcx, rcy) < 0.35:
            rz = float(sample_ground_z(rcx, rcy))
            add_botanical_ruin_cluster(builders[11], ao_stamps, light_stamps, rcx, rcy, rz, scale=rsc, has_tall_pillar=r_tall, has_crystal=r_crys, n_pines=r_pines, seed=r_i * 1.9 + 0.5)

    N_BORDER = 38
    for b_i in range(N_BORDER):
        ang = math.pi * 0.80 + b_i * (math.pi * 1.44 / float(N_BORDER - 1))
        wob = 0.032 * math.sin(ang * 3.0 + 0.4) + 0.022 * math.cos(ang * 5.0 - 0.7) + 0.012 * math.sin(ang * 9.0 + 1.2)
        rx = ISLAND_RX * (1.0 + wob) * 0.915
        ry = ISLAND_RY * (1.0 + wob) * 0.915
        ca, sa = math.cos(ang), math.sin(ang)
        denom = (abs(ca) ** 2.35 + abs(sa) ** 2.35) ** (1.0 / 2.35)
        bx = rx * ca / denom
        by = ry * sa / denom
        if eval_road_mask(bx, by) > 0.35:
            continue
        bz = float(sample_ground_z(bx, by))
        has_crys = (b_i % 3 == 0)
        add_botanical_ruin_cluster(builders[11], ao_stamps, light_stamps, bx, by, bz, scale=0.84 + 0.16 * (b_i % 2), has_tall_pillar=(b_i % 2 == 0), has_crystal=has_crys, n_pines=2, seed=b_i * 2.3 + 7.0)

    for w_i in range(280):
        wx = rng.uniform(-24.5, 23.5)
        wy = rng.uniform(-17.5, 6.5)
        if island_signed_distance(wx, wy) > 0.90:
            continue
        if float(eval_road_mask(wx, wy)) > 0.25:
            continue
        rcx, rhw = eval_river_center_and_width(wy)
        if wy > -7.0 and abs(wx - rcx) < rhw + 0.8:
            continue
        p1_d, p2_d, p3_d = eval_pond_distances(wx, wy)
        if min(p1_d, p2_d, p3_d) < 1.15:
            continue
        wz = float(sample_ground_z(wx, wy))
        add_wildflower_clump(builders[11], wx, wy, wz, scale=rng.uniform(0.65, 0.98), color_type=w_i % 3, seed=w_i * 0.6)
        if w_i % 12 == 0:
            add_tiered_spruce_pine(builders[11], ao_stamps, wx + 0.32, wy + 0.22, wz, scale=rng.uniform(0.32, 0.46), tiers=4, seed=w_i)

    # Dense Vectorized 3D Clumped Golden Meadow Grass Carpet (`11,500` clumps)
    N_CLUMPS = 11500
    cx_arr = rng.uniform(-26.8, 25.8, N_CLUMPS).astype(np.float32)
    cy_arr = rng.uniform(-19.5, 11.5, N_CLUMPS).astype(np.float32)
    d_arr = island_signed_distance(cx_arr, cy_arr)
    r_arr = eval_road_mask(cx_arr, cy_arr)
    rcx_arr, rhw_arr = eval_river_center_and_width(cy_arr)
    riv_ok = (cy_arr < -7.2) | (np.abs(cx_arr - rcx_arr) > rhw_arr + 0.6)

    p1_d_a, p2_d_a, p3_d_a = eval_pond_distances(cx_arr, cy_arr)
    pond_ok = (p1_d_a > 1.08) & (p2_d_a > 1.08) & (p3_d_a > 1.08)
    town_ok = ~((cx_arr > 18.0) & (cy_arr > -2.5) & (cy_arr < 8.5))

    valid_mask = (d_arr < 0.935) & (r_arr < 0.18) & riv_ok & pond_ok & town_ok
    cx_v = cx_arr[valid_mask]
    cy_v = cy_arr[valid_mask]
    cz_v = sample_ground_z(cx_v, cy_v)
    M = len(cx_v)

    g_verts = np.zeros((M, 9, 3), dtype=np.float32)
    g_uvs = np.zeros((M, 9, 2), dtype=np.float32)
    base_ang = rng.uniform(0.0, math.pi * 2.0, M).astype(np.float32)
    h_blade = rng.uniform(0.26, 0.44, M).astype(np.float32)
    w_blade = rng.uniform(0.08, 0.13, M).astype(np.float32)

    for b_idx in range(3):
        ang_b = base_ang + b_idx * (math.pi * 2.0 / 3.0)
        ca_b, sa_b = np.cos(ang_b), np.sin(ang_b)
        v0_i = b_idx * 3
        g_verts[:, v0_i + 0, 0] = cx_v - w_blade * ca_b
        g_verts[:, v0_i + 0, 1] = cy_v - w_blade * sa_b
        g_verts[:, v0_i + 0, 2] = cz_v - 0.03

        g_verts[:, v0_i + 1, 0] = cx_v + w_blade * ca_b
        g_verts[:, v0_i + 1, 1] = cy_v + w_blade * sa_b
        g_verts[:, v0_i + 1, 2] = cz_v - 0.03

        g_verts[:, v0_i + 2, 0] = cx_v + 0.13 * (-sa_b)
        g_verts[:, v0_i + 2, 1] = cy_v + 0.13 * ca_b
        g_verts[:, v0_i + 2, 2] = cz_v + h_blade

        g_uvs[:, v0_i + 0] = [0.06, 0.08]
        g_uvs[:, v0_i + 1] = [0.30, 0.08]
        g_uvs[:, v0_i + 2] = [0.18, 0.96]

    g_verts_flat = g_verts.reshape(-1, 3)
    g_uvs_flat = strip_uv(7, g_uvs.reshape(-1, 2)[:, 0], g_uvs.reshape(-1, 2)[:, 1])

    base_ids = (np.arange(M, dtype=np.int32) * 9)[:, None]
    local_tris = np.array([
        [0, 1, 2], [0, 2, 1],
        [3, 4, 5], [3, 5, 4],
        [6, 7, 8], [6, 8, 7],
    ], dtype=np.int32)
    g_faces = (base_ids[:, :, None] + local_tris[None, :, :]).reshape(-1, 3)
    builders[11].add(g_verts_flat, g_uvs_flat, g_faces)

    return builders, ao_stamps, light_stamps


def export_meshbin(path, verts, uvs, faces):
    v_cnt = len(verts)
    u_verts = np.stack([verts[:, 0], verts[:, 2], verts[:, 1]], axis=-1).astype(np.float32)
    u_uvs = uvs.astype(np.float32)
    u_faces = np.stack([faces[:, 0], faces[:, 2], faces[:, 1]], axis=-1).astype(np.int32).reshape(-1)

    with open(path, "wb") as f:
        f.write(struct.pack("<ii", v_cnt, len(u_faces)))
        f.write(u_verts.tobytes())
        f.write(u_uvs.tobytes())
        f.write(u_faces.tobytes())


def main():
    t0 = time.time()
    builders, ao_stamps, light_stamps = build_all_11_zones()
    atlas_path, normal_path, emission_path, sky_backdrop_path = build_4k_master_atlas_and_terrain_data(ao_stamps, light_stamps)

    print("[3/6] Populating Blender 5.3.0 Scene & Exporting Binary `.meshbin` Meshes for Unity 6...")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    mat_atlas = bpy.data.materials.new(name="Mat_FarmMain_True3D_Master")
    mat_atlas.use_nodes = True
    nt = mat_atlas.node_tree
    nt.nodes.clear()

    node_out = nt.nodes.new("ShaderNodeOutputMaterial")

    node_tex = nt.nodes.new("ShaderNodeTexImage")
    node_tex.image = bpy.data.images.load(atlas_path)
    node_tex.extension = 'CLIP'

    node_norm_tex = nt.nodes.new("ShaderNodeTexImage")
    img_norm = bpy.data.images.load(normal_path)
    img_norm.colorspace_settings.name = 'Non-Color'
    node_norm_tex.image = img_norm
    node_norm_tex.extension = 'CLIP'

    node_norm_map = nt.nodes.new("ShaderNodeNormalMap")
    node_norm_map.inputs["Strength"].default_value = 0.65

    node_emit_tex = nt.nodes.new("ShaderNodeTexImage")
    node_emit_tex.image = bpy.data.images.load(emission_path)
    node_emit_tex.extension = 'CLIP'

    node_bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    node_bsdf.inputs["Roughness"].default_value = 0.72
    if "Specular IOR Level" in node_bsdf.inputs:
        node_bsdf.inputs["Specular IOR Level"].default_value = 0.22
    if "Emission Strength" in node_bsdf.inputs:
        node_bsdf.inputs["Emission Strength"].default_value = 2.40

    node_ambient = nt.nodes.new("ShaderNodeEmission")
    node_ambient.inputs["Strength"].default_value = 0.24
    node_mix = nt.nodes.new("ShaderNodeMixShader")
    node_mix.inputs["Fac"].default_value = 0.18

    nt.links.new(node_tex.outputs["Color"], node_bsdf.inputs["Base Color"])
    nt.links.new(node_norm_tex.outputs["Color"], node_norm_map.inputs["Color"])
    nt.links.new(node_norm_map.outputs["Normal"], node_bsdf.inputs["Normal"])
    if "Emission Color" in node_bsdf.inputs:
        nt.links.new(node_emit_tex.outputs["Color"], node_bsdf.inputs["Emission Color"])
    elif "Emission" in node_bsdf.inputs:
        nt.links.new(node_emit_tex.outputs["Color"], node_bsdf.inputs["Emission"])

    nt.links.new(node_tex.outputs["Color"], node_ambient.inputs["Color"])
    nt.links.new(node_bsdf.outputs["BSDF"], node_mix.inputs[1])
    nt.links.new(node_ambient.outputs["Emission"], node_mix.inputs[2])
    nt.links.new(node_mix.outputs["Shader"], node_out.inputs["Surface"])

    col_zones = bpy.data.collections.new("FarmMain_11_Semantic_3D_Zones")
    scene.collection.children.link(col_zones)

    tot_verts = 0
    tot_tris = 0
    mat_observatory = create_observatory_material(atlas_path, emission_path)

    for z_id, z_name in ZONE_NAMES.items():
        verts, uvs, faces = builders[z_id].build_arrays()
        tot_verts += len(verts)
        tot_tris += len(faces)
        print(f"  -> {z_name}: {len(verts):,} verts | {len(faces):,} tris")

        mesh_path = os.path.join(UNITY_MODEL_DIR, f"{z_name}.meshbin")
        if z_id == 5:
            with open(mesh_path, "wb") as mesh_file:
                mesh_file.write(meshbin_bytes(verts, uvs, faces))
        else:
            export_meshbin(mesh_path, verts, uvs, faces)

        me = bpy.data.meshes.new(z_name)
        me.from_pydata(verts.tolist(), [], faces.tolist())
        uv_layer = me.uv_layers.new(name="UVMap")
        loop_indices = np.empty(len(me.loops), dtype=np.int32)
        me.loops.foreach_get("vertex_index", loop_indices)
        loop_uvs = uvs[loop_indices].reshape(-1)
        uv_layer.data.foreach_set("uv", loop_uvs)
        me.update()

        for poly in me.polygons:
            poly.use_smooth = (z_id in (1, 3))

        obj = bpy.data.objects.new(z_name, me)
        obj.data.materials.append(mat_observatory if z_id == 5 else mat_atlas)
        col_zones.objects.link(obj)

    print(f"  [TOTAL 3D GEOMETRY]: {tot_verts:,} vertices | {tot_tris:,} triangles across 11 semantic 3D zones!")

    cam_pos = (0.0, -68.5, 42.5)
    cam_target = (0.0, -0.5, 3.1)
    cam_data = bpy.data.cameras.new("Master_Isometric_Camera")
    cam_data.type = 'PERSP'
    cam_data.lens = 42.5
    cam_data.sensor_width = 36.0
    cam_data.clip_start = 0.2
    cam_data.clip_end = 500.0
    cam_obj = bpy.data.objects.new("Master_Isometric_Camera", cam_data)
    scene.collection.objects.link(cam_obj)
    cam_obj.location = cam_pos
    direction = mathutils.Vector(cam_target) - mathutils.Vector(cam_pos)
    cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam_obj

    fov_deg = 2.0 * math.degrees(math.atan(18.0 / cam_data.lens))
    calib = {
        "cam_pos_blender": list(cam_pos),
        "cam_target_blender": list(cam_target),
        "fov_vertical_deg": fov_deg * (3.0 / 4.0),
        "lens_mm": cam_data.lens,
    }
    with open(os.path.join(UNITY_MODEL_DIR, "unity_camera_calib.json"), "w") as f:
        json.dump(calib, f, indent=2)

    col_mode1 = bpy.data.collections.new("Mode1_Studio_Backdrop")
    scene.collection.children.link(col_mode1)

    me_floor = bpy.data.meshes.new("Studio_White_Shadow_Floor")
    s_v = [[-110.0, -110.0, -1.02], [110.0, -110.0, -1.02], [110.0, 110.0, -1.02], [-110.0, 110.0, -1.02]]
    me_floor.from_pydata(s_v, [], [[0, 1, 2], [0, 2, 3]])
    obj_floor = bpy.data.objects.new("Studio_White_Shadow_Floor", me_floor)
    mat_floor = bpy.data.materials.new("Mat_Studio_White_Floor")
    mat_floor.use_nodes = True
    f_nt = mat_floor.node_tree
    f_bsdf = f_nt.nodes.get("Principled BSDF")
    if f_bsdf:
        f_bsdf.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)
        f_bsdf.inputs["Roughness"].default_value = 0.95
    obj_floor.data.materials.append(mat_floor)
    col_mode1.objects.link(obj_floor)

    col_mode2 = bpy.data.collections.new("Mode2_Sunset_Atmosphere")
    scene.collection.children.link(col_mode2)

    cam_quat = direction.to_track_quat('-Z', 'Y')
    fwd = direction.normalized()
    right = (cam_quat @ mathutils.Vector((1.0, 0.0, 0.0))).normalized()
    up = (cam_quat @ mathutils.Vector((0.0, 1.0, 0.0))).normalized()
    dist_sky = 108.0
    half_w = dist_sky * (18.0 / cam_data.lens) * 1.04
    half_h = half_w * (3.0 / 4.0)
    sky_center = mathutils.Vector(cam_pos) + fwd * dist_sky

    sv0 = sky_center - right * half_w - up * half_h
    sv1 = sky_center + right * half_w - up * half_h
    sv2 = sky_center + right * half_w + up * half_h
    sv3 = sky_center - right * half_w + up * half_h

    sky_verts = np.array([list(sv0), list(sv1), list(sv2), list(sv3)], dtype=np.float32)
    sky_uvs = np.array([[0.0, 0.0], [1.0, 0.0], [1.0, 1.0], [0.0, 1.0]], dtype=np.float32)
    sky_faces = np.array([[0, 1, 2], [0, 2, 3]], dtype=np.int32)
    export_meshbin(os.path.join(UNITY_MODEL_DIR, "Mode2_Sunset_Atmosphere.meshbin"), sky_verts, sky_uvs, sky_faces)

    me_sky = bpy.data.meshes.new("Mode2_Sunset_Atmosphere")
    me_sky.from_pydata(sky_verts.tolist(), [], sky_faces.tolist())
    suv = me_sky.uv_layers.new(name="UVMap")
    for li, lp in enumerate(me_sky.loops):
        suv.data[li].uv = sky_uvs[lp.vertex_index]
    me_sky.update()

    mat_sky = bpy.data.materials.new("Mat_Mode2_Sunset_Sky")
    mat_sky.use_nodes = True
    s_nt = mat_sky.node_tree
    s_nt.nodes.clear()
    s_out = s_nt.nodes.new("ShaderNodeOutputMaterial")
    s_emit = s_nt.nodes.new("ShaderNodeEmission")
    s_tex = s_nt.nodes.new("ShaderNodeTexImage")
    s_tex.image = bpy.data.images.load(sky_backdrop_path)
    s_tex.extension = 'CLIP'
    s_emit.inputs["Strength"].default_value = 1.02
    s_nt.links.new(s_tex.outputs["Color"], s_emit.inputs["Color"])
    s_nt.links.new(s_emit.outputs["Emission"], s_out.inputs["Surface"])

    obj_sky = bpy.data.objects.new("Mode2_Sunset_Atmosphere", me_sky)
    obj_sky.data.materials.append(mat_sky)
    col_mode2.objects.link(obj_sky)

    sun_data = bpy.data.lights.new(name="GoldenHour_KeySun", type='SUN')
    sun_data.energy = 3.25
    sun_data.color = (1.0, 0.88, 0.70)
    sun_data.angle = math.radians(10.0)
    sun_obj = bpy.data.objects.new(name="GoldenHour_KeySun", object_data=sun_data)
    scene.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = (math.radians(46.0), math.radians(-24.0), math.radians(32.0))

    fill_data = bpy.data.lights.new(name="Skylight_Fill", type='SUN')
    fill_data.energy = 1.25
    fill_data.color = (0.92, 0.94, 1.0)
    fill_obj = bpy.data.objects.new(name="Skylight_Fill", object_data=fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(35.0), math.radians(28.0), math.radians(-145.0))

    world = bpy.data.worlds.new("FarmMain_World")
    scene.world = world
    world.use_nodes = True
    w_bg = world.node_tree.nodes.get("Background")
    if w_bg:
        w_bg.inputs["Color"].default_value = (1.0, 1.0, 1.0, 1.0)
        w_bg.inputs["Strength"].default_value = 0.95

    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = 'Standard'

    print("[4/6] Rendering All 5 Blender 5.3.0 Proof Screenshots...")
    col_mode1.hide_render = False
    col_mode2.hide_render = True
    if w_bg:
        w_bg.inputs["Color"].default_value = (1.0, 1.0, 1.0, 1.0)
    scene.render.filepath = os.path.join(ARTIFACT_DIR, "TRUE3D_Blender53_CameraView_Mode1_Studio.png")
    bpy.ops.render.render(write_still=True)

    col_mode1.hide_render = True
    col_mode2.hide_render = False
    if w_bg:
        w_bg.inputs["Color"].default_value = (0.85, 0.55, 0.42, 1.0)
    scene.render.filepath = os.path.join(ARTIFACT_DIR, "TRUE3D_Blender53_CameraView_Mode2_Sunset.png")
    bpy.ops.render.render(write_still=True)

    col_mode1.hide_render = True
    col_mode2.hide_render = True
    if w_bg:
        w_bg.inputs["Color"].default_value = (0.18, 0.19, 0.22, 1.0)
    cam_top_data = bpy.data.cameras.new("Top_Ortho_Camera")
    cam_top_data.type = 'ORTHO'
    cam_top_data.ortho_scale = 64.0
    cam_top_obj = bpy.data.objects.new("Top_Ortho_Camera", cam_top_data)
    scene.collection.objects.link(cam_top_obj)
    cam_top_obj.location = (0.0, 0.0, 65.0)
    cam_top_obj.rotation_euler = (0.0, 0.0, 0.0)
    scene.camera = cam_top_obj
    scene.render.filepath = os.path.join(ARTIFACT_DIR, "TRUE3D_Blender53_TopOrthographic_Numpad7.png")
    bpy.ops.render.render(write_still=True)

    cam_orb_data = bpy.data.cameras.new("Orbit_Persp_Camera")
    cam_orb_data.type = 'PERSP'
    cam_orb_data.lens = 42.0
    cam_orb_obj = bpy.data.objects.new("Orbit_Persp_Camera", cam_orb_data)
    scene.collection.objects.link(cam_orb_obj)
    orb_pos = (-26.0, -60.0, 39.5)
    cam_orb_obj.location = orb_pos
    cam_orb_obj.rotation_euler = (mathutils.Vector((0.0, 0.0, 3.1)) - mathutils.Vector(orb_pos)).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam_orb_obj
    scene.render.filepath = os.path.join(ARTIFACT_DIR, "TRUE3D_Blender53_PerspectiveOrbit_Wireframe.png")
    bpy.ops.render.render(write_still=True)

    cam_zoom_data = bpy.data.cameras.new("CloseUp_Ground_Camera")
    cam_zoom_data.type = 'PERSP'
    cam_zoom_data.lens = 35.0
    cam_zoom_obj = bpy.data.objects.new("CloseUp_Ground_Camera", cam_zoom_data)
    scene.collection.objects.link(cam_zoom_obj)
    z_pos = (-4.5, -5.8, 5.25)
    z_tgt = (-7.8, 6.4, 4.85)
    cam_zoom_obj.location = z_pos
    cam_zoom_obj.rotation_euler = (mathutils.Vector(z_tgt) - mathutils.Vector(z_pos)).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam_zoom_obj
    if w_bg:
        w_bg.inputs["Color"].default_value = (0.94, 0.66, 0.42, 1.0)
    scene.render.filepath = os.path.join(ARTIFACT_DIR, "TRUE3D_Blender53_CloseUp_GroundLevel_ZoomCheck.png")
    bpy.ops.render.render(write_still=True)

    scene.camera = cam_obj
    col_mode1.hide_viewport = True
    col_mode1.hide_render = True
    col_mode2.hide_viewport = False
    col_mode2.hide_render = False
    bpy.data.objects.remove(cam_top_obj, do_unlink=True)
    bpy.data.objects.remove(cam_orb_obj, do_unlink=True)
    bpy.data.objects.remove(cam_zoom_obj, do_unlink=True)

    print("[5/6] Saving `.blend` Master Files...")
    blend_p1 = os.path.join(BLEND_DIR, "Farm_Main_3D_Master.blend")
    blend_p2 = os.path.join(BLEND_DIR, "PigTycoon_FarmMain_True3D_Masterpiece.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_p1)
    bpy.ops.wm.save_as_mainfile(filepath=blend_p2)
    print(f"[6/6] Completed in {time.time() - t0:.1f}s!")


if __name__ == "__main__":
    main()
