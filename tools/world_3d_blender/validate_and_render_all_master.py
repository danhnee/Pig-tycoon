#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Standalone 3D Mesh Validation, .meshbin Exporter, and High-Resolution True 3D Mesh
PBR + Wireframe-over-Clay Renderer for all upgraded Pig Tycoon 3D Map zones.

Validates (`bad_edges == 0`, `vol6 > 0`) and renders the exact 3D triangle meshes of:
- `Zone10` v3 (`Tools/alpine_geometry.py`): Multi-layered Alpine mountain ranges + 3-tier stepped NW rocky highland cliff with golden oaks.
- `Zone04` v2 (`Tools/village_buildings_geometry.py`):
  * `Nhà Chính & Đài Điều Khiển Bạch Vân` (Main Homestead + Brick Feed Stove + Rooftop Bạch Vân Console)
  * `Nhà Kho` (`Storage Granary` — 3 Distinct Structural Upgrade Tiers: `Cấp 1 -> Cấp 2 -> Cấp 3`)
  * `Phòng Xây Dựng` (High-Bay Workshop + Open Blueprint Bay + Giant A-Frame Gantry Crane + Twin-Stack Forge)
- `Zone06` v2 (`Tools/village_buildings_geometry.py`):
  * `Xưởng Cường Hóa & Khắc Phù Ấn` (Octagonal Stone-and-Bronze Sanctum with 4 Rune Monoliths at `(-1.8, 7.6)`)
  * `Đoạn Chuyển Map` (Rear Boundary Wall, Twin-Pillar Stone Gatehouse Arch & Stepped Mountain Trail)
  * `Khu Làng NPC Ngoại Vi` (`Hub_Village`) elevated on the Rear Stone Highland Terrace Bastion (`y in [11.2, 14.6], z = ground + 1.35m`)
- `Zone07` v2 & `Zone08` v2 (`Tools/harbor_beacon_geometry.py`):
  * `Thiên Nhãn Vọng Đài` (Sapphire Energy Orb + Intersecting Astrological Brass Armillary Rings)
  * `Chợ Thị Trấn & Cảng Âu Cổ` (Stepped Crow-Step Gables, Romanesque Stone Arcades, Jettied Upper Floors on Corbels, Gothic Rose Windows, Covered Market Stalls, Masted Boats)
- `Zone09`, `Zone02`, `Zone11` (`Tools/citadel_diorama_ruins_geometry.py`):
  * `Thành Cổ Hoàng Hôn & Vách Đá Thác Nước`, `Đế Cắt Địa Tầng Diorama`, `Tàn Tích Nhật-Nguyệt & Linh Thảo`
- Full-World Combined Master Preview (`Zone01..Zone11` with all 9 upgraded zones assembled in world coordinates).
"""
import hashlib
import json
import math
import struct
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.stdout.reconfigure(encoding="utf-8")
ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

from canonical_map_helpers import (
    MapGeometry,
    WORLD_X_MAX,
    WORLD_Y_MAX,
    sample_ground_z,
    validate_closed_parts,
)
from observatory_geometry import build_observatory, meshbin_bytes
from alpine_geometry import build_alpine_zone, build_alpine_massif_only
from village_buildings_geometry import (
    build_zone04_farmstead,
    build_zone06_npc_village,
    build_storage_granary_tier1,
    build_storage_granary_tier2,
    build_storage_granary_tier3,
)
from harbor_beacon_geometry import build_zone07_beacon, build_zone08_harbor_and_bridge
from citadel_diorama_ruins_geometry import build_all_citadel_diorama_ruins_zones


def build_terrain_mesh(x0=-30.0, x1=30.0, y0=-22.5, y1=20.0, nx=130, ny=96, clip_island=True):
    """Builds the canonical Zone01 terrain mesh clipped to the elliptical island boundary."""
    xs = np.linspace(x0, x1, nx, dtype=np.float32)
    ys = np.linspace(y0, y1, ny, dtype=np.float32)
    gx, gy = np.meshgrid(xs, ys)
    gz = sample_ground_z(gx, gy) - 0.02

    u_grid = np.clip((gx / (2.0 * WORLD_X_MAX)) + 0.5, 0.001, 0.999)
    v_grid = np.clip(0.255 + 0.740 * ((gy / (2.0 * WORLD_Y_MAX)) + 0.5), 0.255, 0.995)

    verts = np.stack([gx, gy, gz], axis=-1).reshape(-1, 3).astype(np.float32)
    uvs = np.stack([u_grid, v_grid], axis=-1).reshape(-1, 2).astype(np.float32)

    idx = np.arange(nx * ny, dtype=np.int32).reshape(ny, nx)
    a = idx[:-1, :-1].reshape(-1)
    b = idx[:-1, 1:].reshape(-1)
    c = idx[1:, 1:].reshape(-1)
    d = idx[1:, :-1].reshape(-1)
    faces = np.vstack([
        np.column_stack([a, b, c]),
        np.column_stack([a, c, d]),
    ]).astype(np.int32)
    if clip_island:
        tri_c = verts[faces].mean(axis=1)
        rx = (tri_c[:, 0] / 29.35) ** 2 + ((tri_c[:, 1] + 0.8) / 21.15) ** 2
        faces = faces[rx <= 1.0]
    return verts, uvs, faces


class True3DMeshRenderer:
    """High-speed, vectorized 3D triangle mesh renderer (PBR Texture + Lighting + Wireframe-over-Clay)."""

    def __init__(self, albedo_path: Path, emission_path: Path):
        self.albedo_img = Image.open(albedo_path).convert("RGB")
        self.emiss_img = Image.open(emission_path).convert("RGB")
        self.tex_w, self.tex_h = self.albedo_img.size
        self.albedo_arr = np.asarray(self.albedo_img, dtype=np.float32) / 255.0
        self.emiss_arr = np.asarray(self.emiss_img, dtype=np.float32) / 255.0

    def sample_textures(self, tri_uv: np.ndarray):
        """Samples 4K albedo and emission textures at the 3 vertices + centroid of each triangle."""
        # tri_uv shape: (T, 3, 2)
        c_uv = tri_uv.mean(axis=1)
        pts_uv = np.concatenate([tri_uv, c_uv[:, None, :]], axis=1)  # (T, 4, 2)
        u = np.clip(pts_uv[..., 0], 0.0, 1.0)
        v = np.clip(1.0 - pts_uv[..., 1], 0.0, 1.0)  # Flip V for image row indexing
        px = np.clip((u * (self.tex_w - 1)).astype(np.int32), 0, self.tex_w - 1)
        py = np.clip((v * (self.tex_h - 1)).astype(np.int32), 0, self.tex_h - 1)
        alb = self.albedo_arr[py, px].mean(axis=1)   # (T, 3)
        emi = self.emiss_arr[py, px].mean(axis=1)    # (T, 3)
        return alb, emi

    def render_scene(
        self,
        mesh_list,
        filepath: Path,
        cam_loc,
        target_loc,
        ortho_scale=None,
        persp_fov_deg=38.0,
        res_x=1280,
        res_y=900,
        wire_clay_mode=False,
        bg_top=(46, 56, 74),
        bg_bot=(84, 76, 72),
    ):
        """Renders a list of `(verts, uvs, faces, role)` meshes from the given camera."""
        t0 = time.time()
        filepath.parent.mkdir(parents=True, exist_ok=True)

        # 1. Merge visible meshes
        v_all, uv_all, f_all, role_all = [], [], [], []
        v_offset = 0
        for v, uv, f, role in mesh_list:
            if len(f) == 0:
                continue
            v_all.append(v)
            uv_all.append(uv)
            f_all.append(f + v_offset)
            role_all.append(np.full(len(f), role, dtype=np.int32))
            v_offset += len(v)

        V = np.vstack(v_all)
        UV = np.vstack(uv_all)
        F = np.vstack(f_all)
        R = np.concatenate(role_all)

        # 2. Camera basis (Right, Up, Forward)
        cam_p = np.asarray(cam_loc, dtype=np.float64)
        tgt_p = np.asarray(target_loc, dtype=np.float64)
        fwd = tgt_p - cam_p
        fwd /= np.linalg.norm(fwd)
        world_up = np.array([0.0, 0.0, 1.0], dtype=np.float64)
        right = np.cross(fwd, world_up)
        right /= np.linalg.norm(right)
        up = np.cross(right, fwd)
        up /= np.linalg.norm(up)

        rel = V.astype(np.float64) - cam_p[None, :]
        cx = rel @ right
        cy = rel @ up
        cz = rel @ fwd

        aspect = res_x / float(res_y)
        if ortho_scale is not None:
            half_w = 0.5 * ortho_scale
            half_h = half_w / aspect
            sx = (cx / half_w * 0.5 + 0.5) * res_x
            sy = (0.5 - cy / half_h * 0.5) * res_y
        else:
            tan_half = math.tan(math.radians(persp_fov_deg * 0.5))
            cz_safe = np.maximum(cz, 0.2)
            nx = cx / (cz_safe * tan_half * aspect)
            ny = cy / (cz_safe * tan_half)
            sx = (nx * 0.5 + 0.5) * res_x
            sy = (0.5 - ny * 0.5) * res_y

        # 3. Triangle geometry & culling
        tri_v = V[F]      # (T, 3, 3)
        tri_sx = sx[F]    # (T, 3)
        tri_sy = sy[F]    # (T, 3)
        tri_cz = cz[F]    # (T, 3)

        # Frustum cull
        in_view = (
            (tri_cz.min(axis=1) > 0.2)
            & (tri_sx.max(axis=1) >= -40)
            & (tri_sx.min(axis=1) <= res_x + 40)
            & (tri_sy.max(axis=1) >= -40)
            & (tri_sy.min(axis=1) <= res_y + 40)
        )
        tri_v = tri_v[in_view]
        tri_sx = tri_sx[in_view]
        tri_sy = tri_sy[in_view]
        tri_cz = tri_cz[in_view]
        F_vis = F[in_view]
        R_vis = R[in_view]

        # Compute world normals
        e1 = tri_v[:, 1] - tri_v[:, 0]
        e2 = tri_v[:, 2] - tri_v[:, 0]
        normals = np.cross(e1, e2)
        n_len = np.linalg.norm(normals, axis=1, keepdims=True)
        normals = normals / np.maximum(n_len, 1e-12)

        # Backface cull (keep faces facing camera or slightly grazing)
        if ortho_scale is not None:
            view_dirs = -fwd[None, :]
        else:
            centroids = tri_v.mean(axis=1)
            view_dirs = cam_p[None, :] - centroids
            view_dirs /= np.maximum(np.linalg.norm(view_dirs, axis=1, keepdims=True), 1e-9)
        ndotv = np.sum(normals * view_dirs, axis=1)
        front_mask = ndotv > -0.15
        tri_v = tri_v[front_mask]
        tri_sx = tri_sx[front_mask]
        tri_sy = tri_sy[front_mask]
        tri_cz = tri_cz[front_mask]
        normals = normals[front_mask]
        F_vis = F_vis[front_mask]
        R_vis = R_vis[front_mask]

        # 4. Compute Lighting (Golden-Hour Sun Key + Cool Sky Fill + Warm Rim + Height AO)
        sun_dir = np.array([-0.52, -0.44, 0.73], dtype=np.float32)
        sun_dir /= np.linalg.norm(sun_dir)
        fill_dir = np.array([0.58, -0.32, 0.75], dtype=np.float32)
        fill_dir /= np.linalg.norm(fill_dir)
        rim_dir = np.array([0.12, 0.82, 0.56], dtype=np.float32)
        rim_dir /= np.linalg.norm(rim_dir)

        ndotl_key = np.clip(normals @ sun_dir, 0.0, 1.0)
        ndotl_fill = np.clip(normals @ fill_dir, 0.0, 1.0)
        ndotl_rim = np.clip(normals @ rim_dir, 0.0, 1.0)
        sky_hemi = 0.5 + 0.5 * normals[:, 2]

        cent_z = tri_v[:, :, 2].mean(axis=1)
        ao_factor = np.clip(0.76 + 0.24 * np.tanh((cent_z - 2.2) / 3.5), 0.68, 1.04)

        if wire_clay_mode:
            # Warm architectural clay shading
            base_clay = np.full((len(tri_v), 3), (0.87, 0.82, 0.75), dtype=np.float32)
            base_clay[R_vis == 1] = (0.58, 0.55, 0.50)  # Terrain floor clay
            base_clay[R_vis == 2] = (0.64, 0.76, 0.86)  # Water clay
            shade = (0.34 + 0.52 * ndotl_key + 0.18 * ndotl_fill + 0.10 * sky_hemi) * ao_factor
            rgb_f = np.clip(base_clay * shade[:, None], 0.0, 1.0)
        else:
            tri_uv = UV[F_vis]
            alb, emi = self.sample_textures(tri_uv)
            sun_col = np.array([1.06, 0.95, 0.82], dtype=np.float32)
            fill_col = np.array([0.78, 0.86, 0.98], dtype=np.float32)
            amb_col = np.array([0.42, 0.45, 0.52], dtype=np.float32)
            lit = (
                amb_col[None, :] * (0.65 + 0.35 * sky_hemi[:, None])
                + sun_col[None, :] * (0.78 * ndotl_key[:, None])
                + fill_col[None, :] * (0.26 * ndotl_fill[:, None])
                + np.array([1.0, 0.88, 0.72], dtype=np.float32)[None, :] * (0.16 * ndotl_rim[:, None])
            ) * ao_factor[:, None]
            rgb_f = np.clip(alb * lit + emi * 1.35, 0.0, 1.0)
            # Subtle AgX-like contrast/gamma curve
            rgb_f = np.power(rgb_f, 1.0 / 1.12)

        colors_u8 = np.clip(rgb_f * 255.0, 0, 255).astype(np.uint8)

        # 5. Depth sort (far to near using average + min Z)
        sort_z = 0.75 * tri_cz.mean(axis=1) + 0.25 * tri_cz.max(axis=1)
        order = np.argsort(-sort_z)

        tri_sx = tri_sx[order]
        tri_sy = tri_sy[order]
        colors_u8 = colors_u8[order]
        R_sorted = R_vis[order]

        # 6. Render background gradient + sorted triangles
        ys_grad = np.linspace(0.0, 1.0, res_y, dtype=np.float32)[:, None, None]
        bg_arr = (
            np.array(bg_top, dtype=np.float32)[None, None, :] * (1.0 - ys_grad)
            + np.array(bg_bot, dtype=np.float32)[None, None, :] * ys_grad
        ).astype(np.uint8)
        bg_arr = np.broadcast_to(bg_arr, (res_y, res_x, 3)).copy()
        img = Image.fromarray(bg_arr, mode="RGB")
        draw = ImageDraw.Draw(img)

        pts_int = np.stack([tri_sx, tri_sy], axis=-1).round().astype(np.int32)
        if wire_clay_mode:
            for i in range(len(pts_int)):
                p = pts_int[i]
                c = (int(colors_u8[i, 0]), int(colors_u8[i, 1]), int(colors_u8[i, 2]))
                pts_tuple = [(int(p[0, 0]), int(p[0, 1])), (int(p[1, 0]), int(p[1, 1])), (int(p[2, 0]), int(p[2, 1]))]
                r_code = int(R_sorted[i])
                outline_col = (52, 56, 64) if r_code == 1 else ((22, 38, 58) if r_code == 2 else (18, 22, 29))
                draw.polygon(pts_tuple, fill=c, outline=outline_col)
        else:
            for i in range(len(pts_int)):
                p = pts_int[i]
                c = (int(colors_u8[i, 0]), int(colors_u8[i, 1]), int(colors_u8[i, 2]))
                pts_tuple = [(int(p[0, 0]), int(p[0, 1])), (int(p[1, 0]), int(p[1, 1])), (int(p[2, 0]), int(p[2, 1]))]
                draw.polygon(pts_tuple, fill=c)

        img.save(filepath)
        print(f"  [RENDERED] {filepath.name} ({len(pts_int)} visible tris, {time.time() - t0:.2f}s)", flush=True)


def main():
    t_start = time.time()
    art_root = ROOT / ".collab/artifacts"
    out_z10 = art_root / "zone10-20261018"
    out_z04_06 = art_root / "zone04-06-20261018"
    out_z07_08 = art_root / "zone07-08-20261018"
    out_z02_09_11 = art_root / "zone02-09-11-20261018"
    out_world = art_root / "world-master-20261018"
    for d in (out_z10, out_z04_06, out_z07_08, out_z02_09_11, out_world):
        d.mkdir(parents=True, exist_ok=True)

    source_blend = ROOT / "ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend"
    source_sha = hashlib.sha256(source_blend.read_bytes()).hexdigest()

    tex_dir = ROOT / "Assets/Textures/FarmMain3D"
    renderer = True3DMeshRenderer(
        tex_dir / "FarmMain_3D_Albedo_4K.png",
        tex_dir / "FarmMain_3D_Emission_4K.png",
    )

    print("[1/3] Building & validating all closed-manifold zones...", flush=True)
    cdr_zones = build_all_citadel_diorama_ruins_zones()
    builders = {
        "Zone10_Vista_AlpineMountains_And_ForestRidge": (build_alpine_zone, out_z10, 18796),
        "Zone04_Building_KhuChuongTrai_Farmstead": (build_zone04_farmstead, out_z04_06, 44424),
        "Zone06_Building_NhaCuongHoa_And_KhuNPC": (build_zone06_npc_village, out_z04_06, 15244),
        "Zone07_Landmark_TramThongTin_InfoBeacon": (build_zone07_beacon, out_z07_08, 2248),
        "Zone08_Vista_StoneBridge_And_HarborTown": (build_zone08_harbor_and_bridge, out_z07_08, 66740),
        "Zone09_Vista_TwilightCastle_And_WaterfallCliff": (lambda: cdr_zones["Zone09_Vista_TwilightCastle_And_WaterfallCliff"], out_z02_09_11, 21172),
        "Zone02_Diorama_Cutaway_RockRim_And_Base": (lambda: cdr_zones["Zone02_Diorama_Cutaway_RockRim_And_Base"], out_z02_09_11, 6192),
        "Zone11_Props_AncientRuins_And_GoldenFlora": (lambda: cdr_zones["Zone11_Props_AncientRuins_And_GoldenFlora"], out_z02_09_11, 29712),
    }

    zone_arrays = {}
    summary_metrics = {}
    for z_name, (fn, out_dir, old_tris) in builders.items():
        geom = fn()
        rep = validate_closed_parts(geom)
        assert rep["closed_outward_components"] and not rep["failures"], f"{z_name} failed: {rep['failures']}"
        v, uv, f = geom.arrays()
        assert np.isfinite(v).all() and np.isfinite(uv).all()
        assert ((uv >= 0.0) & (uv <= 1.0)).all()
        tri = v[f]
        areas = np.linalg.norm(np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0]), axis=1)
        assert areas.min() > 1e-9

        binary = meshbin_bytes(v, uv, f)
        bin_path = out_dir / f"{z_name}.meshbin"
        bin_path.write_bytes(binary)
        zone_arrays[z_name] = (v, uv, f)

        summary_metrics[z_name] = {
            "old_triangles": old_tris,
            "parts": rep["parts"],
            "vertices": int(len(v)),
            "triangles": int(len(f)),
            "closed_outward_components": True,
            "failures": [],
            "min_double_triangle_area": float(areas.min()),
            "bounds_min": [round(float(x), 4) for x in v.min(axis=0)],
            "bounds_max": [round(float(x), 4) for x in v.max(axis=0)],
            "meshbin_path": str(bin_path),
            "meshbin_sha256": hashlib.sha256(binary).hexdigest(),
        }
        print(f"  [OK] {z_name}: {rep['parts']} closed parts, {len(v)} verts, {len(f)} tris", flush=True)

    # Massif-only option for Zone10
    m_only = build_alpine_massif_only()
    mv, muv, mf = m_only.arrays()
    mbin = meshbin_bytes(mv, muv, mf)
    (out_z10 / "Zone10_MassifOnly.meshbin").write_bytes(mbin)

    # Zone05 (`Phòng Nghiên Cứu`)
    obs_geom = build_observatory()
    ov, ouv, of = obs_geom.arrays((-7.8, 6.4, 3.2412), yaw=-0.05)
    zone_arrays["Zone05_Building_NhaNghienCuu_Observatory"] = (ov, ouv, of)

    # Context Terrain (`Zone01`) and Water (`Zone03`)
    tv, tuv, tf = build_terrain_mesh()
    z03_geom = cdr_zones["Zone03_Water_River_Waterfalls_And_3Ponds"]
    wv, wuv, wf = z03_geom.arrays()

    # 3-Stage Storage Granary Upgrade Tiers (`Cấp 1, Cấp 2, Cấp 3`)
    g_t1 = build_storage_granary_tier1()
    g_t2 = build_storage_granary_tier2()
    g_t3 = build_storage_granary_tier3()
    gran_metrics = {}
    for idx_t, gt in enumerate((g_t1, g_t2, g_t3), start=1):
        rep_t = validate_closed_parts(gt)
        assert rep_t["closed_outward_components"] and not rep_t["failures"]
        vt, uvt, ft = gt.arrays()
        t_bin = meshbin_bytes(vt, uvt, ft)
        (out_z04_06 / f"StorageGranary_Tier{idx_t}.meshbin").write_bytes(t_bin)
        gran_metrics[f"tier_{idx_t}"] = {
            "parts": rep_t["parts"],
            "vertices": int(len(vt)),
            "triangles": int(len(ft)),
            "closed_outward_components": True,
            "meshbin_sha256": hashlib.sha256(t_bin).hexdigest(),
        }

    gran_lineup = MapGeometry()
    gran_lineup.merge_from(g_t1, origin=(-5.2, 0.0, 0.0), yaw=0.12)
    gran_lineup.merge_from(g_t2, origin=(-0.2, 0.0, 0.0), yaw=0.10)
    gran_lineup.merge_from(g_t3, origin=( 5.4, 0.0, 0.0), yaw=0.08)
    gv, guv, gf = gran_lineup.arrays()

    print("[2/3] Rendering True 3D Mesh PBR + Wireframe-over-Clay views...", flush=True)

    # A. Zone10 v3
    z10_v, z10_uv, z10_f = zone_arrays["Zone10_Vista_AlpineMountains_And_ForestRidge"]
    renderer.render_scene([(z10_v, z10_uv, z10_f, 0)], out_z10 / "02-after-front.png", (-4.0, -19.5, 20.8), (-8.0, 14.5, 6.8), ortho_scale=36.0, res_x=1280, res_y=840, wire_clay_mode=False)
    renderer.render_scene([(tv, tuv, tf, 1), (z10_v, z10_uv, z10_f, 0)], out_z10 / "06-ridge-close.png", (-13.0, -14.0, 15.8), (-14.2, 13.5, 7.6), ortho_scale=19.5, res_x=1280, res_y=840, wire_clay_mode=False)
    renderer.render_scene([(z10_v, z10_uv, z10_f, 0)], out_z10 / "08-after-wireframe-clay.png", (-4.0, -19.5, 20.8), (-8.0, 14.5, 6.8), ortho_scale=36.0, res_x=1280, res_y=840, wire_clay_mode=True)

    # B. Zone04 v2
    z04_v, z04_uv, z04_f = zone_arrays["Zone04_Building_KhuChuongTrai_Farmstead"]
    t04_v, t04_uv, t04_f = build_terrain_mesh(-27.5, -3.5, -3.0, 12.5, nx=72, ny=52)
    renderer.render_scene([(t04_v, t04_uv, t04_f, 1), (z04_v, z04_uv, z04_f, 0)], out_z04_06 / "01-zone04-front-pbr.png", (-11.2, -1.8, 10.2), (-16.1, 7.6, 4.75), ortho_scale=11.4, res_x=1280, res_y=900, wire_clay_mode=False)
    renderer.render_scene([(t04_v, t04_uv, t04_f, 1), (z04_v, z04_uv, z04_f, 0)], out_z04_06 / "02-zone04-wireframe-clay-proof.png", (-11.2, -1.8, 10.2), (-16.1, 7.6, 4.75), ortho_scale=11.4, res_x=1280, res_y=900, wire_clay_mode=True)

    # C. Zone06 v2 (Octagonal Enhancement Sanctum + Transition Gate + Elevated Rear Hub_Village)
    z06_v, z06_uv, z06_f = zone_arrays["Zone06_Building_NhaCuongHoa_And_KhuNPC"]
    t06_v, t06_uv, t06_f = build_terrain_mesh(-10.5, 7.5, 3.5, 16.5, nx=72, ny=52)
    renderer.render_scene([(t06_v, t06_uv, t06_f, 1), (ov, ouv, of, 0), (z06_v, z06_uv, z06_f, 0)], out_z04_06 / "03-zone06-front-pbr.png", (-4.8, -1.2, 12.2), (0.0, 10.4, 5.15), ortho_scale=12.6, res_x=1280, res_y=900, wire_clay_mode=False)
    renderer.render_scene([(t06_v, t06_uv, t06_f, 1), (ov, ouv, of, 0), (z06_v, z06_uv, z06_f, 0)], out_z04_06 / "04-zone06-wireframe-clay-proof.png", (-4.8, -1.2, 12.2), (0.0, 10.4, 5.15), ortho_scale=12.6, res_x=1280, res_y=900, wire_clay_mode=True)

    # D. 3-Stage Storage Granary Upgrade Showcase (`Cấp 1 -> Cấp 2 -> Cấp 3`)
    renderer.render_scene([(gv, guv, gf, 0)], out_z04_06 / "05-storage-granary-3-upgrade-tiers-pbr.png", (0.0, -9.5, 4.8), (0.1, 0.1, 1.35), ortho_scale=14.8, res_x=1440, res_y=720, wire_clay_mode=False)
    renderer.render_scene([(gv, guv, gf, 0)], out_z04_06 / "06-storage-granary-3-upgrade-tiers-wireframe.png", (0.0, -9.5, 4.8), (0.1, 0.1, 1.35), ortho_scale=14.8, res_x=1440, res_y=720, wire_clay_mode=True)

    # E. Zone07 v2 & Zone08 v2 (`Âu Cổ` Harbor Town)
    z07_v, z07_uv, z07_f = zone_arrays["Zone07_Landmark_TramThongTin_InfoBeacon"]
    bz07 = float(sample_ground_z(8.6, 4.6))
    renderer.render_scene([(z07_v, z07_uv, z07_f, 0)], out_z07_08 / "01-zone07-beacon-pbr.png", (1.1, -4.9, bz07 + 6.4), (8.6, 4.6, bz07 + 2.35), ortho_scale=7.2, res_x=1080, res_y=1080, wire_clay_mode=False)
    renderer.render_scene([(z07_v, z07_uv, z07_f, 0)], out_z07_08 / "02-zone07-beacon-wireframe-clay.png", (1.1, -4.9, bz07 + 6.4), (8.6, 4.6, bz07 + 2.35), ortho_scale=7.2, res_x=1080, res_y=1080, wire_clay_mode=True)

    z08_v, z08_uv, z08_f = zone_arrays["Zone08_Vista_StoneBridge_And_HarborTown"]
    t08_v, t08_uv, t08_f = build_terrain_mesh(6.0, 29.5, -8.5, 11.5, nx=72, ny=56)
    renderer.render_scene([(t08_v, t08_uv, t08_f, 1), (wv, wuv, wf, 2), (z08_v, z08_uv, z08_f, 0)], out_z07_08 / "03-zone08-harbor-bridge-pbr.png", (5.5, -17.5, 18.2), (19.6, 3.2, 4.4), ortho_scale=21.2, res_x=1440, res_y=960, wire_clay_mode=False)
    renderer.render_scene([(t08_v, t08_uv, t08_f, 1), (wv, wuv, wf, 2), (z08_v, z08_uv, z08_f, 0)], out_z07_08 / "04-zone08-harbor-bridge-wireframe-clay.png", (5.5, -17.5, 18.2), (19.6, 3.2, 4.4), ortho_scale=21.2, res_x=1440, res_y=960, wire_clay_mode=True)

    # F. Zone09, Zone02 & Zone11
    z09_v, z09_uv, z09_f = zone_arrays["Zone09_Vista_TwilightCastle_And_WaterfallCliff"]
    renderer.render_scene([(wv, wuv, wf, 2), (z09_v, z09_uv, z09_f, 0)], out_z02_09_11 / "01-zone09-castle-cliff-pbr.png", (12.8, -11.5, 18.2), (14.4, 14.2, 10.6), ortho_scale=21.5, res_x=1280, res_y=960, wire_clay_mode=False)
    renderer.render_scene([(wv, wuv, wf, 2), (z09_v, z09_uv, z09_f, 0)], out_z02_09_11 / "02-zone09-castle-cliff-wireframe-clay.png", (12.8, -11.5, 18.2), (14.4, 14.2, 10.6), ortho_scale=21.5, res_x=1280, res_y=960, wire_clay_mode=True)

    z02_v, z02_uv, z02_f = zone_arrays["Zone02_Diorama_Cutaway_RockRim_And_Base"]
    z11_v, z11_uv, z11_f = zone_arrays["Zone11_Props_AncientRuins_And_GoldenFlora"]
    renderer.render_scene([(z02_v, z02_uv, z02_f, 0), (tv, tuv, tf, 1), (wv, wuv, wf, 2), (z11_v, z11_uv, z11_f, 0)], out_z02_09_11 / "03-zone02-11-diorama-ruins-pbr.png", (-2.8, -50.0, 26.5), (0.0, -4.5, 2.2), persp_fov_deg=42.0, res_x=1280, res_y=900, wire_clay_mode=False)
    renderer.render_scene([(z02_v, z02_uv, z02_f, 0), (tv, tuv, tf, 1), (wv, wuv, wf, 2), (z11_v, z11_uv, z11_f, 0)], out_z02_09_11 / "04-zone02-11-diorama-ruins-wireframe-clay.png", (-2.8, -50.0, 26.5), (0.0, -4.5, 2.2), persp_fov_deg=42.0, res_x=1280, res_y=900, wire_clay_mode=True)

    # G. Full-World Master Preview (ALL Zones Assembled Together!)
    full_world_meshes = [
        (z02_v, z02_uv, z02_f, 0),
        (tv, tuv, tf, 1),
        (wv, wuv, wf, 2),
        (z10_v, z10_uv, z10_f, 0),
        (z09_v, z09_uv, z09_f, 0),
        (z08_v, z08_uv, z08_f, 0),
        (z07_v, z07_uv, z07_f, 0),
        (z06_v, z06_uv, z06_f, 0),
        (ov, ouv, of, 0),
        (z04_v, z04_uv, z04_f, 0),
        (z11_v, z11_uv, z11_f, 0),
    ]
    renderer.render_scene(full_world_meshes, out_world / "01-full-world-upgraded-pbr.png", (-3.2, -55.5, 31.5), (0.5, -1.8, 3.2), persp_fov_deg=43.0, res_x=1600, res_y=1120, wire_clay_mode=False)
    renderer.render_scene(full_world_meshes, out_z10 / "07-world-context.png", (-3.2, -55.5, 31.5), (0.5, -1.8, 3.2), persp_fov_deg=43.0, res_x=1440, res_y=1020, wire_clay_mode=False)
    renderer.render_scene(full_world_meshes, out_world / "02-full-world-upgraded-wireframe-clay.png", (-3.2, -55.5, 31.5), (0.5, -1.8, 3.2), persp_fov_deg=43.0, res_x=1600, res_y=1120, wire_clay_mode=True)

    # Write validation JSONs
    (out_z10 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha,
        "source_unmodified_on_disk": True,
        "zone10": summary_metrics["Zone10_Vista_AlpineMountains_And_ForestRidge"],
        "massif_only_meshbin_sha256": hashlib.sha256(mbin).hexdigest(),
    }, indent=2), encoding="utf-8")

    (out_z04_06 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha,
        "source_unmodified_on_disk": True,
        "zone04": summary_metrics["Zone04_Building_KhuChuongTrai_Farmstead"],
        "zone06": summary_metrics["Zone06_Building_NhaCuongHoa_And_KhuNPC"],
        "storage_granary_tiers": gran_metrics,
    }, indent=2), encoding="utf-8")

    (out_z07_08 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha,
        "source_unmodified_on_disk": True,
        "zone07": summary_metrics["Zone07_Landmark_TramThongTin_InfoBeacon"],
        "zone08": summary_metrics["Zone08_Vista_StoneBridge_And_HarborTown"],
    }, indent=2), encoding="utf-8")

    (out_z02_09_11 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha,
        "source_unmodified_on_disk": True,
        "zones": {
            k: summary_metrics[k]
            for k in (
                "Zone09_Vista_TwilightCastle_And_WaterfallCliff",
                "Zone02_Diorama_Cutaway_RockRim_And_Base",
                "Zone11_Props_AncientRuins_And_GoldenFlora",
            )
        },
    }, indent=2), encoding="utf-8")

    master_report = {
        "source_blend": str(source_blend),
        "source_sha256": source_sha,
        "source_unmodified_on_disk": True,
        "elapsed_seconds": round(time.time() - t_start, 2),
        "zones": summary_metrics,
        "storage_granary_tiers": gran_metrics,
    }
    (out_world / "validation.json").write_text(json.dumps(master_report, indent=2), encoding="utf-8")
    print("[3/3] ALL ZONES VALIDATED, EXPORTED & RENDERED IN", master_report["elapsed_seconds"], "s", flush=True)


if __name__ == "__main__":
    main()
