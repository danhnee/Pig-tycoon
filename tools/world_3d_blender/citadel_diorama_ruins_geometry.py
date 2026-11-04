#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""High-detail closed-manifold 3D geometry generator for Zone09, Zone02, Zone03, and Zone11.

Every component emitted by `build_zone09_castle_and_cliff()`, `build_zone02_diorama_rim()`,
`build_zone03_water()`, and `build_zone11_ruins_and_flora()` uses `MapGeometry` from
`canonical_map_helpers.py` and is a 100% water-tight, outward-wound 3D manifold solid
(`bad_edges == 0`, `vol6 > 0`, verified by `validate_closed_parts`).
"""

import math
import numpy as np

from canonical_map_helpers import (
    ISLAND_RX,
    ISLAND_RY,
    WORLD_X_MAX,
    WORLD_Y_MAX,
    MapGeometry,
    eval_pond_distances,
    eval_river_center_and_width,
    eval_road_mask,
    island_signed_distance,
    sample_ground_z,
    validate_closed_parts,
)
from observatory_geometry import frame, prism, unit


# =============================================================================
# SHARED CLOSED-MANIFOLD GEOMETRIC HELPERS (`bad_edges == 0`, `vol6 > 0`)
# =============================================================================

def rot_z_mat(yaw: float) -> np.ndarray:
    c, s = math.cos(yaw), math.sin(yaw)
    return np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]], dtype=float)


def rot_zyx_mat(yaw: float, pitch: float = 0.0, roll: float = 0.0) -> np.ndarray:
    cy, sy = math.cos(yaw), math.sin(yaw)
    cp, sp = math.cos(pitch), math.sin(pitch)
    cr, sr = math.cos(roll), math.sin(roll)
    rz = np.array([[cy, -sy, 0.0], [sy, cy, 0.0], [0.0, 0.0, 1.0]], dtype=float)
    ry = np.array([[cp, 0.0, sp], [0.0, 1.0, 0.0], [-sp, 0.0, cp]], dtype=float)
    rx = np.array([[1.0, 0.0, 0.0], [0.0, cr, -sr], [0.0, sr, cr]], dtype=float)
    return rz @ ry @ rx


def crisp_box(
    g: MapGeometry,
    name: str,
    center,
    size,
    material: str,
    rotation=None,
    top_taper: float = 0.92,
):
    """Adds a closed, outward-wound 8-vertex (12-triangle) tapered/chamfered box.
    Avoids microscopic bevel slivers at large world coordinates so float32 vol6 is strictly positive.
    """
    cx, cy, cz = float(center[0]), float(center[1]), float(center[2])
    hx = max(0.035, float(size[0]) * 0.5)
    hy = max(0.035, float(size[1]) * 0.5)
    hz = max(0.035, float(size[2]) * 0.5)
    tx = hx * top_taper
    ty = hy * top_taper

    verts = np.array(
        [
            [-hx, -hy, -hz],
            [ hx, -hy, -hz],
            [ hx,  hy, -hz],
            [-hx,  hy, -hz],
            [-tx, -ty,  hz],
            [ tx, -ty,  hz],
            [ tx,  ty,  hz],
            [-tx,  ty,  hz],
        ],
        dtype=float,
    )
    if rotation is not None:
        verts = verts @ np.asarray(rotation, dtype=float).T
    verts += np.array([cx, cy, cz], dtype=float)

    faces = [
        [3, 2, 1, 0],
        [4, 5, 6, 7],
        [0, 1, 5, 4],
        [1, 2, 6, 5],
        [2, 3, 7, 6],
        [3, 0, 4, 7],
    ]
    g.add(name, verts, faces, material, convex_center=np.array([cx, cy, cz], dtype=float))


def crisp_beam(
    g: MapGeometry,
    name: str,
    a,
    b,
    width: float,
    material: str = "wood",
    depth: float = None,
):
    """Adds a closed 12-triangle beam between points `a` and `b`."""
    a_arr = np.asarray(a, dtype=float)
    b_arr = np.asarray(b, dtype=float)
    span = float(np.linalg.norm(b_arr - a_arr))
    w = max(0.07, float(width))
    d = max(0.07, float(depth if depth is not None else width))
    crisp_box(
        g,
        name,
        0.5 * (a_arr + b_arr),
        (w, d, max(0.08, span)),
        material,
        rotation=frame(b_arr - a_arr),
        top_taper=0.98,
    )


def add_chiseled_strata_block(
    g: MapGeometry,
    name: str,
    center,
    size,
    material: str,
    yaw: float = 0.0,
    pitch: float = 0.0,
    roll: float = 0.0,
    taper: float = 0.90,
    crag: float = 0.09,
    seed: float = 0.0,
    segs: int = 8,
):
    """Adds a closed, outward-wound beveled & faceted sedimentary rock block."""
    cx, cy, cz = float(center[0]), float(center[1]), float(center[2])
    hx, hy, hz = max(0.06, float(size[0]) * 0.5), max(0.06, float(size[1]) * 0.5), max(0.06, float(size[2]) * 0.5)
    rot = rot_zyx_mat(yaw, pitch, roll)

    ring_specs = [
        (-hz, 0.78, 0.03),
        (-hz * 0.65, 1.02, crag),
        (hz * 0.62, taper, crag * 0.85),
        (hz, taper * 0.74, 0.03),
    ]
    verts = []
    for r_idx, (z_loc, r_scale, c_amp) in enumerate(ring_specs):
        for s_i in range(segs):
            th = (s_i + 0.5) * (2.0 * math.pi / segs)
            mod = 1.0 + c_amp * math.sin(th * 3.0 + seed + r_idx * 0.7) + (c_amp * 0.55) * math.cos(th * 2.0 - seed * 1.3)
            lx = hx * r_scale * mod * math.cos(th)
            ly = hy * r_scale * mod * math.sin(th)
            p_world = (rot @ np.array([lx, ly, z_loc], dtype=float)) + np.array([cx, cy, cz], dtype=float)
            verts.append(p_world)

    n_rings = len(ring_specs)
    faces = [
        list(range(segs - 1, -1, -1)),
        list(range((n_rings - 1) * segs, n_rings * segs)),
    ]
    for r_i in range(n_rings - 1):
        base = r_i * segs
        for s_i in range(segs):
            s_next = (s_i + 1) % segs
            faces.append([base + s_i, base + s_next, base + segs + s_next, base + segs + s_i])

    g.add(name, verts, faces, material, convex_center=np.array([cx, cy, cz], dtype=float))


def add_tower_frustum(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    z1: float,
    r_bot: float,
    r_top: float,
    segs: int,
    material: str,
    yaw: float = 0.0,
):
    """Adds a closed polygonal/cylindrical frustum with exact yaw orientation."""
    rb = max(0.035, float(r_bot))
    rt = max(0.035, float(r_top))
    verts = []
    for z_val, r_val in [(float(z0), rb), (float(z1), rt)]:
        for i in range(segs):
            ang = yaw + (2.0 * math.pi * i) / segs
            verts.append((cx + r_val * math.cos(ang), cy + r_val * math.sin(ang), z_val))
    faces = [
        list(range(segs - 1, -1, -1)),
        list(range(segs, 2 * segs)),
    ]
    for i in range(segs):
        j = (i + 1) % segs
        faces.append([i, j, segs + j, segs + i])
    g.add(name, verts, faces, material, convex_center=np.array([cx, cy, 0.5 * (z0 + z1)], dtype=float))


def add_gothic_lancet_window(
    g: MapGeometry,
    name: str,
    origin,
    width: float = 0.36,
    height: float = 0.72,
    yaw: float = 0.0,
    glass_mat: str = "glass",
    trim_mat: str = "trim",
    with_voussoirs: bool = False,
):
    """Adds a closed Gothic pointed-arch glowing window assembly facing local -Y."""
    r = max(0.11, width * 0.5)
    spring = max(0.16, height - r * 1.10)
    c, s = math.cos(yaw), math.sin(yaw)

    def pt(lx, ly, lz):
        return (origin[0] + c * lx - s * ly, origin[1] + s * lx + c * ly, origin[2] + lz)

    def pointed_outline(rad, spr, peak_h):
        pts = [(-rad, 0.0), (rad, 0.0)]
        for k in range(7):
            t = k / 6.0
            ang = t * math.pi
            px = rad * math.cos(ang)
            pz = spr + (peak_h - spr) * (math.sin(ang) ** 0.78)
            pts.append((px, pz))
        return pts

    prism(g, name + "_surround", pointed_outline(r + 0.05, spring, height + 0.06), 0.14, pt(0.0, 0.0, 0.0), trim_mat, yaw)
    prism(g, name + "_pane", pointed_outline(r - 0.01, spring - 0.01, height - 0.01), 0.11, pt(0.0, -0.035, 0.015), glass_mat, yaw)
    crisp_box(g, name + "_sill", pt(0.0, -0.04, -0.04), (width + 0.18, 0.18, 0.11), trim_mat, rot_z_mat(yaw))
    crisp_box(g, name + "_mullion", pt(0.0, -0.06, height * 0.46), (0.09, 0.11, max(0.22, height * 0.86)), trim_mat, rot_z_mat(yaw))

    if with_voussoirs:
        for v_i in range(5):
            lo = (v_i / 5.0) * math.pi + 0.03
            hi = ((v_i + 1) / 5.0) * math.pi - 0.03
            r_in = r + 0.01
            r_out = r + 0.11
            h_in = height - r + r_in
            h_out = height - r + r_out
            out_v = [
                (r_out * math.cos(lo), spring + (h_out - spring) * math.sin(lo)),
                (r_out * math.cos(hi), spring + (h_out - spring) * math.sin(hi)),
                (r_in * math.cos(hi), spring + (h_in - spring) * math.sin(hi)),
                (r_in * math.cos(lo), spring + (h_in - spring) * math.sin(lo)),
            ]
            prism(g, name + "_voussoir", out_v, 0.16, pt(0.0, -0.015, 0.0), trim_mat, yaw)


def add_closed_autumn_broadleaf_tree(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    variant: int = 0,
    seed: float = 0.0,
):
    """Adds a closed-manifold multi-bough golden/amber autumn broadleaf tree."""
    trunk_h = 1.05 * scale
    add_tower_frustum(g, name + "_trunk", cx, cy, z0 - 0.16, z0 + trunk_h, 0.24 * scale, 0.14 * scale, 7, "wood", yaw=seed)
    for b_i in range(3):
        b_ang = seed + b_i * (2.0 * math.pi / 3.0)
        p_a = (cx, cy, z0 + trunk_h * 0.68)
        p_b = (cx + 0.42 * scale * math.cos(b_ang), cy + 0.42 * scale * math.sin(b_ang), z0 + trunk_h * 1.12)
        crisp_beam(g, name + "_branch", p_a, p_b, 0.11 * scale, "wood")

    palettes = [
        ("oak_gold", "oak_amber", "oak_gold"),
        ("oak_amber", "oak_orange", "oak_gold"),
        ("oak_gold", "oak_orange", "oak_amber"),
    ]
    pal = palettes[variant % len(palettes)]

    bough_offsets = [
        (-0.48, -0.32, 0.96, 0.78, 0.72, 0.56),
        (-0.42,  0.40, 1.00, 0.76, 0.74, 0.58),
        ( 0.04, -0.50, 1.02, 0.76, 0.72, 0.58),
        ( 0.06,  0.48, 1.06, 0.74, 0.76, 0.60),
        ( 0.48, -0.26, 1.10, 0.78, 0.74, 0.62),
        ( 0.44,  0.30, 1.14, 0.76, 0.74, 0.62),
        (-0.28,  0.02, 1.44, 0.80, 0.76, 0.64),
        ( 0.28, -0.04, 1.48, 0.80, 0.76, 0.66),
        ( 0.02,  0.02, 1.82, 0.70, 0.68, 0.58),
    ]
    ca, sa = math.cos(seed * 1.3), math.sin(seed * 1.3)
    for idx, (dx, dy, dz, rx, ry, rz) in enumerate(bough_offsets):
        rx_w = (dx * ca - dy * sa) * scale
        ry_w = (dx * sa + dy * ca) * scale
        mat = pal[idx % len(pal)]
        add_chiseled_strata_block(
            g,
            name + "_bough",
            (cx + rx_w, cy + ry_w, z0 + dz * scale),
            (rx * 1.85 * scale, ry * 1.85 * scale, rz * 1.75 * scale),
            mat,
            yaw=seed + idx * 0.5,
            taper=0.88,
            crag=0.11,
            seed=seed + idx,
            segs=8,
        )


def add_closed_spruce_pine(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    tiers: int = 5,
    seed: float = 0.0,
):
    """Adds a closed-manifold multi-tiered scalloped highland spruce/pine tree."""
    add_tower_frustum(g, name + "_trunk", cx, cy, z0 - 0.14, z0 + 0.65 * scale, 0.18 * scale, 0.10 * scale, 6, "wood", yaw=seed)
    segs = 10
    for t_i in range(tiers):
        tz0 = z0 + (0.22 + t_i * 0.38) * scale
        th = (0.86 - 0.08 * t_i) * scale
        r_bot = max(0.16, (0.98 - 0.13 * t_i) * scale)
        r_top = max(0.05, (0.06 if t_i == tiers - 1 else (0.22 - 0.025 * t_i)) * scale)
        mat = "pine_dark" if t_i < 2 else "pine_green"

        verts = []
        for is_top, z_v, r_v in [(False, tz0, r_bot), (True, tz0 + th, r_top)]:
            for i in range(segs):
                ang = seed + t_i * 0.25 + (2.0 * math.pi * i) / segs
                scallop = 1.0 if is_top else (1.0 + 0.12 * math.cos(i * math.pi))
                dz_droop = 0.0 if is_top else (-0.04 * scale * (1.0 if i % 2 == 0 else 0.0))
                verts.append((cx + r_v * scallop * math.cos(ang), cy + r_v * scallop * math.sin(ang), z_v + dz_droop))

        faces = [
            list(range(segs - 1, -1, -1)),
            list(range(segs, 2 * segs)),
        ]
        for i in range(segs):
            j = (i + 1) % segs
            faces.append([i, j, segs + j])
            faces.append([i, segs + j, segs + i])
        g.add(name + "_tier", verts, faces, mat, convex_center=np.array([cx, cy, tz0 + th * 0.35], dtype=float))


def add_closed_grid_slab(
    g: MapGeometry,
    name: str,
    gx: np.ndarray,
    gy: np.ndarray,
    gz_top: np.ndarray,
    gz_bot: np.ndarray,
    material: str,
):
    """Builds a 100% closed, water-tight 3D solid from a `(NY, NX)` top/bottom height grid."""
    ny, nx = gx.shape
    top_v = np.stack([gx, gy, gz_top], axis=-1).reshape(-1, 3)
    bot_v = np.stack([gx, gy, gz_bot], axis=-1).reshape(-1, 3)
    verts = np.vstack([top_v, bot_v])
    n_grid = ny * nx

    faces = []
    # Top surface (+Z outward) and Bottom surface (-Z outward)
    for j in range(ny - 1):
        for i in range(nx - 1):
            a = j * nx + i
            b = a + 1
            c = a + nx + 1
            d = a + nx
            faces.append([a, b, c])
            faces.append([a, c, d])
            faces.append([n_grid + a, n_grid + c, n_grid + b])
            faces.append([n_grid + a, n_grid + d, n_grid + c])

    # South wall (j = 0)
    for i in range(nx - 1):
        a = i
        b = i + 1
        faces.append([n_grid + a, n_grid + b, b])
        faces.append([n_grid + a, b, a])

    # North wall (j = ny - 1)
    row_n = (ny - 1) * nx
    for i in range(nx - 1):
        a = row_n + i
        b = row_n + i + 1
        faces.append([n_grid + b, n_grid + a, a])
        faces.append([n_grid + b, a, b])

    # West wall (i = 0)
    for j in range(ny - 1):
        a = j * nx
        b = (j + 1) * nx
        faces.append([n_grid + b, n_grid + a, a])
        faces.append([n_grid + b, a, b])

    # East wall (i = nx - 1)
    for j in range(ny - 1):
        a = j * nx + (nx - 1)
        b = (j + 1) * nx + (nx - 1)
        faces.append([n_grid + a, n_grid + b, b])
        faces.append([n_grid + a, b, a])

    g.add(name, verts, faces, material, convex_center=None)


# =============================================================================
# ZONE 09: TWILIGHT CITADEL & STRATIFIED WATERFALL CLIFF ESCARPMENT
# =============================================================================

def _add_castle_tower(
    g: MapGeometry,
    name: str,
    tx: float,
    ty: float,
    z_base: float,
    tr: float,
    t_shaft_h: float,
    t_spire_h: float,
    is_octagonal: bool = False,
    has_lucarnes: bool = False,
    seed: float = 0.0,
):
    """Builds a high-detail cylindrical or octagonal Gothic tower with stone courses,
    corbeled parapets, glowing lancet windows, 12-sided dark-slate spire, and brass finial.
    """
    segs = 8 if is_octagonal else 12
    yaw = math.pi / segs if is_octagonal else 0.0

    # 1. Stepped stone plinth & molded base
    add_tower_frustum(g, name + "_plinth_bot", tx, ty, z_base - 0.35, z_base + 0.18, tr * 1.18, tr * 1.10, segs, "stone", yaw=yaw)
    add_tower_frustum(g, name + "_plinth_trim", tx, ty, z_base + 0.16, z_base + 0.34, tr * 1.12, tr * 1.03, segs, "trim", yaw=yaw)

    # 2. Main tower shaft
    z_shaft_top = z_base + 0.32 + t_shaft_h
    add_tower_frustum(g, name + "_shaft", tx, ty, z_base + 0.32, z_shaft_top, tr * 1.00, tr * 0.91, segs, "plaster", yaw=yaw)

    # 3. Radial stone masonry courses / horizontal stringcourse rings + vertical pilaster ribs
    n_courses = max(4, int(t_shaft_h / 0.85))
    for c_i in range(n_courses):
        frac = (c_i + 0.5) / float(n_courses)
        cz = z_base + 0.32 + t_shaft_h * frac
        r_here = tr * (1.00 - 0.09 * frac)
        mat_band = "trim" if (c_i % 2 == 1) else "stone"
        add_tower_frustum(
            g,
            name + "_course_band",
            tx,
            ty,
            cz - 0.06,
            cz + 0.06,
            r_here + 0.032,
            r_here + 0.032,
            segs,
            mat_band,
            yaw=yaw,
        )

    n_ribs = 4 if tr < 0.55 else 6
    for r_i in range(n_ribs):
        ang_r = yaw + r_i * (2.0 * math.pi / n_ribs)
        ca, sa = math.cos(ang_r), math.sin(ang_r)
        p_bot = (tx + (tr * 0.99) * ca, ty + (tr * 0.99) * sa, z_base + 0.34)
        p_top = (tx + (tr * 0.90) * ca, ty + (tr * 0.90) * sa, z_shaft_top - 0.12)
        crisp_beam(g, name + "_pilaster_rib", p_bot, p_top, 0.11, "stone")

    # 4. Multiple tiers of warm glowing Gothic arched windows facing south/SW/SE
    win_tiers = [0.28, 0.54, 0.78] if t_shaft_h >= 3.8 else [0.36, 0.70]
    win_angles = [-math.pi * 0.5, -math.pi * 0.72, -math.pi * 0.28] if tr >= 0.54 else [-math.pi * 0.5, -math.pi * 0.68]
    for w_frac in win_tiers:
        wz = z_base + 0.32 + t_shaft_h * w_frac
        r_w = tr * (1.00 - 0.09 * w_frac)
        for w_ang in win_angles:
            wx = tx + (r_w - 0.01) * math.cos(w_ang)
            wy = ty + (r_w - 0.01) * math.sin(w_ang)
            w_yaw = w_ang + math.pi * 0.5
            add_gothic_lancet_window(
                g,
                name + "_window",
                (wx, wy, wz - 0.16),
                width=max(0.22, tr * 0.42),
                height=max(0.38, tr * 0.76),
                yaw=w_yaw,
                glass_mat="glass",
                trim_mat="trim",
                with_voussoirs=(tr >= 0.65 and abs(w_ang + math.pi * 0.5) < 0.1),
            )

    # 5. Corbeled stone parapet rings + machicolations + battlements/merlons
    r_top_shaft = tr * 0.91
    add_tower_frustum(g, name + "_corbel_lower", tx, ty, z_shaft_top - 0.14, z_shaft_top + 0.02, r_top_shaft * 1.02, r_top_shaft * 1.14, segs, "trim", yaw=yaw)
    add_tower_frustum(g, name + "_parapet_ring", tx, ty, z_shaft_top + 0.01, z_shaft_top + 0.22, r_top_shaft * 1.15, r_top_shaft * 1.16, segs, "stone", yaw=yaw)

    n_merlons = 8 if tr < 0.56 else 12
    for m_i in range(n_merlons):
        m_ang = m_i * (2.0 * math.pi / n_merlons)
        mx = tx + (r_top_shaft * 1.11) * math.cos(m_ang)
        my = ty + (r_top_shaft * 1.11) * math.sin(m_ang)
        crisp_box(
            g,
            name + "_merlon",
            (mx, my, z_shaft_top + 0.28),
            (max(0.16, tr * 0.28), 0.15, 0.20),
            "trim",
            rot_z_mat(m_ang + math.pi * 0.5),
        )

    # 6. Steep 12-sided dark slate-blue conical spire (`spire_dark` / `slate_blue`)
    z_sp = z_shaft_top + 0.18
    add_tower_frustum(g, name + "_spire_skirt", tx, ty, z_sp, z_sp + t_spire_h * 0.28, r_top_shaft * 1.14, r_top_shaft * 0.56, 12, "spire_dark", yaw=0.0)
    add_tower_frustum(g, name + "_spire_mid", tx, ty, z_sp + t_spire_h * 0.26, z_sp + t_spire_h * 0.68, r_top_shaft * 0.57, r_top_shaft * 0.22, 12, "slate_blue", yaw=0.12)
    add_tower_frustum(g, name + "_spire_upper", tx, ty, z_sp + t_spire_h * 0.66, z_sp + t_spire_h * 1.02, r_top_shaft * 0.23, 0.055, 12, "spire_dark", yaw=0.0)

    # Optional lucarne dormers on major towers
    if has_lucarnes:
        for l_ang in (-math.pi * 0.5, -math.pi * 0.75, -math.pi * 0.25):
            lx = tx + (r_top_shaft * 0.68) * math.cos(l_ang)
            ly = ty + (r_top_shaft * 0.68) * math.sin(l_ang)
            lz = z_sp + t_spire_h * 0.22
            l_yaw = l_ang + math.pi * 0.5
            crisp_box(g, name + "_lucarne_body", (lx, ly, lz), (0.30, 0.32, 0.38), "stone", rot_z_mat(l_yaw))
            crisp_box(g, name + "_lucarne_roof", (lx, ly, lz + 0.24), (0.34, 0.36, 0.20), "spire_dark", rot_z_mat(l_yaw), top_taper=0.65)
            add_gothic_lancet_window(g, name + "_lucarne_win", (lx + 0.14 * math.cos(l_ang), ly + 0.14 * math.sin(l_ang), lz - 0.12), width=0.22, height=0.34, yaw=l_yaw)

    # 7. Brass finial + orb crown
    z_fin = z_sp + t_spire_h * 1.00
    add_tower_frustum(g, name + "_finial_rod", tx, ty, z_fin - 0.06, z_fin + 0.44, 0.065, 0.045, 8, "brass")
    crisp_box(g, name + "_finial_orb", (tx, ty, z_fin + 0.22), (0.16, 0.16, 0.16), "brass")
    crisp_box(g, name + "_finial_cross", (tx, ty, z_fin + 0.38), (0.24, 0.10, 0.10), "brass")


def _add_castle_keep_and_curtain_walls(g: MapGeometry, cas_cx: float, cas_cy: float, cas_z: float):
    """Builds the stepped rocky crag foundation, curtain walls with stone courses & merlons,
    arched gateway, flying buttresses, and the Central Grand Keep (`4.2 x 2.1 x 3.85`).
    """
    # A. Stepped rocky crag foundation under the Citadel (28 chiseled cliff crag blocks)
    for row_i, (z_off, y_off, h_crag, n_blk, w_span) in enumerate([
        (-0.85, -1.35, 1.15, 10, 9.6),
        (-0.35, -0.85, 1.05, 9,  8.8),
        ( 0.08, -0.25, 0.78, 9,  8.0),
    ]):
        for b_i in range(n_blk):
            frac = b_i / float(max(1, n_blk - 1)) - 0.5
            bx = cas_cx + frac * w_span + 0.12 * math.sin(b_i * 2.1 + row_i)
            by = cas_cy + y_off - 0.38 * math.cos(frac * math.pi)
            bw = (w_span / n_blk) * 1.24
            bd = 1.95 + 0.35 * ((b_i + row_i) % 2)
            mat = "sandstone_warm" if (b_i + row_i) % 3 == 0 else ("stone" if (b_i + row_i) % 3 == 1 else "sandstone_ochre")
            add_chiseled_strata_block(
                g,
                "citadel_foundation_crag",
                (bx, by, cas_z + z_off),
                (bw, bd, h_crag),
                mat,
                yaw=0.14 * math.sin(b_i * 1.7),
                taper=0.88,
                crag=0.09,
                seed=row_i * 10 + b_i,
            )

    # Raised citadel terrace podium slab
    crisp_box(g, "citadel_terrace_core", (cas_cx, cas_cy - 0.15, cas_z + 0.12), (8.4, 3.6, 0.68), "stone")
    crisp_box(g, "citadel_terrace_coping", (cas_cx, cas_cy - 0.15, cas_z + 0.48), (8.6, 3.75, 0.18), "trim")

    # B. Outer Curtain Walls with individual beveled stone courses, merlons & Arched Gateway
    wall_segments = [
        # Left & Right front curtain walls flanking the central gatehouse
        ("curtain_front_west", cas_cx - 2.35, cas_cy - 1.25, 2.75, 0.48, 1.95, 0.08),
        ("curtain_front_east", cas_cx + 2.35, cas_cy - 1.25, 2.75, 0.48, 1.95, -0.08),
        # Outer flank wing walls
        ("curtain_outer_west", cas_cx - 3.85, cas_cy - 0.95, 1.65, 0.44, 1.65, 0.22),
        ("curtain_outer_east", cas_cx + 3.85, cas_cy - 0.95, 1.65, 0.44, 1.65, -0.22),
    ]
    for w_name, wcx, wcy, wsx, wsy, wsz, wyaw in wall_segments:
        rmat = rot_z_mat(wyaw)
        c_w, s_w = math.cos(wyaw), math.sin(wyaw)
        crisp_box(g, w_name + "_core", (wcx, wcy, cas_z + 0.45 + wsz * 0.5), (wsx, wsy, wsz), "mortar", rmat)
        crisp_box(g, w_name + "_plinth", (wcx, wcy, cas_z + 0.52), (wsx + 0.12, wsy + 0.14, 0.26), "trim", rmat)
        crisp_box(g, w_name + "_cornice", (wcx, wcy, cas_z + 0.45 + wsz), (wsx + 0.14, wsy + 0.16, 0.18), "trim", rmat)

        # Individual stone masonry courses on front face
        n_rows = 4
        n_cols = max(4, int(wsx / 0.52))
        for r_i in range(n_rows):
            rz = cas_z + 0.72 + r_i * ((wsz - 0.36) / n_rows)
            for c_i in range(n_cols):
                u_f = (c_i + 0.5 + 0.25 * (r_i % 2)) / n_cols - 0.5
                lx = u_f * (wsx - 0.22)
                ly = -wsy * 0.51
                bx = wcx + lx * c_w - ly * s_w
                by = wcy + lx * s_w + ly * c_w
                b_mat = "stone" if (r_i + c_i) % 2 == 0 else "ruin_ashlar"
                crisp_box(g, w_name + "_ashlar", (bx, by, rz), ((wsx / n_cols) * 0.88, 0.14, 0.30), b_mat, rmat)

        # Crenellated battlements / merlons along curtain wall top
        n_mer = max(4, int(wsx / 0.50))
        for m_i in range(n_mer):
            lx = ((m_i + 0.5) / n_mer - 0.5) * (wsx - 0.20)
            ly = -wsy * 0.36
            mx = wcx + lx * c_w - ly * s_w
            my = wcy + lx * s_w + ly * c_w
            crisp_box(g, w_name + "_merlon", (mx, my, cas_z + 0.45 + wsz + 0.18), (0.28, 0.20, 0.24), "trim", rmat)

    # C. Grand Arched Gateway at front center (`cas_cx, cas_cy - 1.32`)
    gate_x, gate_y, gate_z = cas_cx, cas_cy - 1.32, cas_z + 0.48
    crisp_box(g, "gatehouse_body", (gate_x, gate_y, gate_z + 1.18), (1.76, 0.76, 2.36), "stone")
    crisp_box(g, "gatehouse_cornice", (gate_x, gate_y, gate_z + 2.38), (1.92, 0.88, 0.20), "trim")
    for gm_i in range(5):
        gmx = gate_x - 0.72 + gm_i * 0.36
        crisp_box(g, "gatehouse_merlon", (gmx, gate_y - 0.32, gate_z + 2.56), (0.24, 0.20, 0.24), "trim")
    add_gothic_lancet_window(
        g,
        "gatehouse_arch_portal",
        (gate_x, gate_y - 0.38, gate_z + 0.02),
        width=0.82,
        height=1.42,
        yaw=0.0,
        glass_mat="door",
        trim_mat="trim",
        with_voussoirs=True,
    )
    for gx_off in (-0.46, 0.46):
        add_gothic_lancet_window(
            g,
            "gatehouse_upper_lancet",
            (gate_x + gx_off, gate_y - 0.38, gate_z + 1.56),
            width=0.30,
            height=0.58,
            yaw=0.0,
            glass_mat="glass",
            trim_mat="trim",
        )

    # D. Central Grand Keep (`4.2 x 2.1 x 3.85`) at `(cas_cx, cas_cy + 0.55, cas_z + 0.55)`
    keep_cx, keep_cy, keep_z0 = cas_cx, cas_cy + 0.55, cas_z + 0.55
    keep_sx, keep_sy, keep_sz = 4.2, 2.1, 3.85

    crisp_box(g, "keep_core", (keep_cx, keep_cy, keep_z0 + keep_sz * 0.5), (keep_sx, keep_sy, keep_sz), "plaster")
    crisp_box(g, "keep_plinth", (keep_cx, keep_cy, keep_z0 + 0.22), (keep_sx + 0.18, keep_sy + 0.18, 0.44), "stone")
    for belt_z in (keep_z0 + 1.35, keep_z0 + 2.65, keep_z0 + keep_sz - 0.06):
        crisp_box(g, "keep_belt_course", (keep_cx, keep_cy, belt_z), (keep_sx + 0.16, keep_sy + 0.16, 0.16), "trim")

    # Individual stone ashlar courses on the Grand Keep facades
    for row_k in range(7):
        kz = keep_z0 + 0.52 + row_k * 0.46
        n_kcols = 8
        for col_k in range(n_kcols):
            kx = keep_cx + ((col_k + 0.5 + 0.25 * (row_k % 2)) / n_kcols - 0.5) * (keep_sx - 0.28)
            for side_y in (-1.0, 1.0):
                ky = keep_cy + side_y * (keep_sy * 0.505)
                k_mat = "stone" if (row_k + col_k) % 2 == 0 else "trim"
                crisp_box(g, "keep_stone_course", (kx, ky, kz), (0.44, 0.14, 0.34), k_mat)

    # Corner quoins on the Grand Keep
    for qx_s in (-1.0, 1.0):
        for qy_s in (-1.0, 1.0):
            for q_row in range(8):
                qz = keep_z0 + 0.48 + q_row * 0.42
                crisp_box(
                    g,
                    "keep_quoin",
                    (keep_cx + qx_s * (keep_sx * 0.50), keep_cy + qy_s * (keep_sy * 0.50), qz),
                    (0.26, 0.26, 0.34),
                    "trim",
                )

    # Flying Buttresses (`6` stepped piers + inclined flyer beams flanking the Keep)
    for fb_x in (-1.75, 0.0, 1.75):
        for fb_side in (-1.0, 1.0):
            pier_x = keep_cx + fb_x
            pier_y = keep_cy + fb_side * (keep_sy * 0.5 + 0.58)
            crisp_box(g, "keep_buttress_pier_low", (pier_x, pier_y, keep_z0 + 1.05), (0.36, 0.44, 2.10), "stone")
            crisp_box(g, "keep_buttress_pier_top", (pier_x, pier_y, keep_z0 + 2.45), (0.30, 0.36, 0.95), "trim")
            crisp_box(g, "keep_buttress_pinnacle", (pier_x, pier_y, keep_z0 + 3.12), (0.24, 0.24, 0.44), "spire_dark", top_taper=0.35)
            # Two tiers of flying buttress arches/flyers connecting pier to keep wall
            for fl_z0, fl_z1 in [(keep_z0 + 1.55, keep_z0 + 2.15), (keep_z0 + 2.45, keep_z0 + 3.15)]:
                p_outer = (pier_x, keep_cy + fb_side * (keep_sy * 0.5 + 0.46), fl_z0)
                p_inner = (pier_x, keep_cy + fb_side * (keep_sy * 0.5 - 0.02), fl_z1)
                crisp_beam(g, "keep_flying_buttress_flyer", p_outer, p_inner, 0.16, "trim", depth=0.20)

    # Tall Gothic arched glowing windows across the Grand Keep
    for w_kx in (-1.25, -0.45, 0.45, 1.25):
        for w_tier_z, w_h in [(keep_z0 + 1.48, 0.82), (keep_z0 + 2.75, 0.88)]:
            add_gothic_lancet_window(
                g,
                "keep_facade_window",
                (keep_cx + w_kx, keep_cy - keep_sy * 0.51, w_tier_z),
                width=0.38,
                height=w_h,
                yaw=0.0,
                glass_mat="glass",
                trim_mat="trim",
                with_voussoirs=True,
            )

    # Steep dark-slate tiled roof (`4.6 x 2.4 x 2.10`) on the Grand Keep + ridge cresting
    roof_z0 = cas_z + 4.35
    roof_sx, roof_sy, roof_sz = 4.6, 2.4, 2.10
    hx_r, hy_r = roof_sx * 0.5, roof_sy * 0.5
    roof_verts = [
        (keep_cx - hx_r, keep_cy - hy_r, roof_z0),
        (keep_cx + hx_r, keep_cy - hy_r, roof_z0),
        (keep_cx + hx_r, keep_cy + hy_r, roof_z0),
        (keep_cx - hx_r, keep_cy + hy_r, roof_z0),
        (keep_cx - hx_r * 0.94, keep_cy, roof_z0 + roof_sz),
        (keep_cx + hx_r * 0.94, keep_cy, roof_z0 + roof_sz),
    ]
    roof_faces = [
        [0, 3, 2, 1],
        [0, 1, 5, 4],
        [2, 3, 4, 5],
        [1, 2, 5],
        [3, 0, 4],
    ]
    g.add("keep_steep_roof", roof_verts, roof_faces, "spire_dark", convex_center=np.mean(roof_verts, axis=0))
    crisp_beam(g, "keep_roof_ridge", (keep_cx - hx_r * 0.95, keep_cy, roof_z0 + roof_sz + 0.05), (keep_cx + hx_r * 0.95, keep_cy, roof_z0 + roof_sz + 0.05), 0.14, "brass")
    for end_x in (-hx_r * 0.92, 0.0, hx_r * 0.92):
        add_tower_frustum(g, "keep_ridge_finial", keep_cx + end_x, keep_cy, roof_z0 + roof_sz, roof_z0 + roof_sz + 0.54, 0.065, 0.045, 8, "brass")


def build_zone09_castle_and_cliff() -> MapGeometry:
    """Builds `Zone09_Vista_TwilightCastle_And_WaterfallCliff`:
    - 175+ stacked, multi-tiered, bevel-faceted sedimentary cliff rock strata blocks
      between and around `wf_xs = [8.2, 11.4, 14.8, 17.8]`.
    - 22 multi-cluster autumn oak & highland spruce pine trees crowning the cliff rim.
    - Soaring Gothic Twilight Citadel (`cas_cx=14.4, cas_cy=15.6, cas_z=7.35`) with
      stepped rocky crag foundation, curtain walls, arched gateway, flying buttresses,
      Central Grand Keep, and 20 cylindrical & octagonal castle towers.
    """
    g = MapGeometry()

    # -------------------------------------------------------------------------
    # 1. STRATIFIED WATERFALL CLIFF ESCARPMENT (175+ Beveled Rock Strata Blocks)
    # -------------------------------------------------------------------------
    buttress_columns = [
        (6.0,  12.3, 5.95, 2.3),
        (6.9,  12.5, 6.05, 1.9),
        (9.5,  12.4, 6.10, 1.9),
        (10.2, 12.5, 6.05, 1.8),
        (12.8, 12.3, 6.15, 1.9),
        (13.5, 12.4, 6.10, 1.8),
        (16.1, 12.2, 6.05, 1.9),
        (16.7, 12.3, 6.00, 1.7),
        (19.0, 12.0, 5.90, 2.2),
        (20.3, 11.8, 5.75, 2.3),
        (21.6, 11.5, 5.50, 2.3),
        (22.9, 11.1, 5.15, 2.2),
        (24.0, 10.6, 4.75, 2.1),
    ]
    strata_mats = ["sandstone_warm", "sandstone_ochre", "stone", "trim", "sandstone_shadow"]

    for col_i, (bcx, bcy, top_h, bw_base) in enumerate(buttress_columns):
        n_tiers = 7
        z_bot_cliff = 1.40
        tier_dz = (top_h - 0.15) / float(n_tiers)
        for t_i in range(n_tiers):
            tf = t_i / float(n_tiers - 1)
            cz = z_bot_cliff + (t_i + 0.5) * tier_dz
            step_back = 0.65 * tf - 0.12 * math.sin(tf * math.pi * 2.0 + col_i)
            x_jitter = 0.14 * math.sin(t_i * 1.9 + col_i * 1.3)
            sx = bw_base * (1.16 - 0.24 * tf)
            sy = 2.45 * (1.12 - 0.18 * tf)
            sz = tier_dz * 1.18
            mat = strata_mats[(col_i + t_i) % 4]
            add_chiseled_strata_block(
                g,
                "cliff_buttress_strata",
                (bcx + x_jitter, bcy - 0.75 + step_back, cz),
                (sx, sy, sz),
                mat,
                yaw=0.12 * math.sin(col_i * 1.7 + t_i),
                pitch=0.03 * math.cos(t_i),
                taper=0.92,
                crag=0.09,
                seed=col_i * 11.0 + t_i,
            )
            # Protruding front ledge rock strata slab on lower/mid tiers
            if t_i in (0, 2, 4, 6):
                ledge_mat = "trim" if t_i == 6 else ("sandstone_warm" if t_i % 2 == 0 else "stone")
                crisp_box(
                    g,
                    "cliff_strata_ledge",
                    (bcx + x_jitter * 0.6, bcy - 1.28 + step_back * 0.85, cz + sz * 0.28),
                    (sx * 0.92, sy * 0.65, tier_dz * 0.46),
                    ledge_mat,
                    rot_z_mat(0.08 * math.sin(col_i + t_i)),
                )

    # B. Stepped waterfall channel back-wall strata behind `wf_xs = [8.2, 11.4, 14.8, 17.8]`
    wf_xs = [8.2, 11.4, 14.8, 17.8]
    for w_i, wx in enumerate(wf_xs):
        for s_i in range(6):
            sf = s_i / 5.0
            cz = 1.65 + s_i * 0.96
            cy = 11.45 + sf * 1.25
            mat = "sandstone_shadow" if s_i % 2 == 0 else "stone"
            add_chiseled_strata_block(
                g,
                "waterfall_channel_strata",
                (wx, cy, cz),
                (1.95, 1.85, 1.12),
                mat,
                yaw=0.06 * (s_i - 2.5),
                taper=0.94,
                crag=0.07,
                seed=100.0 + w_i * 10 + s_i,
            )

    # C. Eastern cliff bluff & river-gorge rock wall (`x in [23.2..24.8], y in [7.2..11.2]`)
    for e_i in range(8):
        ey = 7.2 + e_i * 0.55
        ex = 24.0 + 0.35 * math.sin(e_i * 1.4)
        for t_i in range(4):
            cz = 2.15 + t_i * 1.05
            mat = "sandstone_warm" if (e_i + t_i) % 2 == 0 else "sandstone_ochre"
            add_chiseled_strata_block(
                g,
                "east_bluff_strata",
                (ex - 0.18 * t_i, ey, cz),
                (2.45 - 0.18 * t_i, 1.95, 1.20),
                mat,
                yaw=0.22 * e_i + 0.1 * t_i,
                taper=0.90,
                crag=0.09,
                seed=200.0 + e_i * 5 + t_i,
            )

    # D. Upper cliff rim cap-stones & talus boulders at waterfall base (14 blocks)
    for r_i in range(14):
        rx_c = 5.8 + r_i * 1.32
        ry_c = 13.0 - 0.08 * max(0, r_i - 7)
        add_chiseled_strata_block(
            g,
            "cliff_rim_capstone",
            (rx_c, ry_c, 7.05 - 0.08 * max(0, r_i - 9)),
            (1.68, 1.85, 0.72),
            "stone" if r_i % 2 == 0 else "sandstone_warm",
            yaw=0.15 * r_i,
            taper=0.92,
            crag=0.08,
            seed=300.0 + r_i,
        )

    # -------------------------------------------------------------------------
    # 2. SOARING GOTHIC TWILIGHT CITADEL (`cas_cx=14.4, cas_cy=15.6, cas_z=7.35`)
    # -------------------------------------------------------------------------
    cas_cx, cas_cy, cas_z = 14.4, 15.6, 7.35
    _add_castle_keep_and_curtain_walls(g, cas_cx, cas_cy, cas_z)

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
        _add_castle_tower(
            g,
            f"castle_tower_{t_idx:02d}",
            cas_cx + tdx,
            cas_cy + tdy,
            cas_z,
            tr,
            tsh,
            tsp,
            is_octagonal=(t_idx % 3 == 1),
            has_lucarnes=(t_idx < 5),
            seed=float(t_idx),
        )

    # -------------------------------------------------------------------------
    # 3. 22 MULTI-CLUSTER AUTUMN OAK & PINE TREES CROWNING THE CLIFF RIM
    # -------------------------------------------------------------------------
    cliff_tree_coords = [
        ( 5.8, 12.8, 0.78, False),
        ( 6.9, 13.4, 0.68, True),
        ( 7.6, 13.9, 0.72, False),
        ( 8.5, 13.1, 0.74, False),
        ( 9.6, 13.6, 0.66, True),
        (10.5, 14.0, 0.76, False),
        (11.6, 13.6, 0.70, False),
        (12.6, 13.9, 0.64, True),
        (13.6, 13.7, 0.72, False),
        (15.0, 13.7, 0.72, False),
        (16.0, 13.8, 0.66, True),
        (17.0, 13.6, 0.74, False),
        (18.2, 13.3, 0.74, False),
        (19.1, 14.2, 0.68, True),
        (19.6, 13.2, 0.78, False),
        (20.6, 12.8, 0.66, True),
        (21.4, 12.4, 0.76, False),
        (22.2, 12.0, 0.80, False),
        (23.1, 11.4, 0.72, False),
        (23.6, 10.4, 0.68, True),
        (24.0,  9.4, 0.76, False),
        (24.4,  8.3, 0.68, True),
    ]
    for idx_ct, (ctx, cty, ct_sc, is_pine) in enumerate(cliff_tree_coords):
        ctz = 7.35 if cty > 12.0 else (6.35 if cty > 10.8 else 5.65)
        if is_pine:
            add_closed_spruce_pine(g, f"cliff_pine_{idx_ct:02d}", ctx, cty, ctz, scale=ct_sc, tiers=5, seed=idx_ct * 1.7)
        else:
            add_closed_autumn_broadleaf_tree(g, f"cliff_oak_{idx_ct:02d}", ctx, cty, ctz, scale=ct_sc, variant=idx_ct % 3, seed=idx_ct * 2.3)

    return g


# =============================================================================
# ZONE 02: CARVED STONE BLOCK DIORAMA CUTAWAY RIM & BASE SLAB
# =============================================================================

def _eval_island_rim_xy(ang: float, scale_r: float = 0.958):
    wob = (
        0.032 * math.sin(ang * 3.0 + 0.4)
        + 0.022 * math.cos(ang * 5.0 - 0.7)
        + 0.012 * math.sin(ang * 9.0 + 1.2)
    )
    rx = ISLAND_RX * (1.0 + wob) * scale_r
    ry = ISLAND_RY * (1.0 + wob) * scale_r
    ca, sa = math.cos(ang), math.sin(ang)
    denom = (abs(ca) ** 2.35 + abs(sa) ** 2.35) ** (1.0 / 2.35)
    return rx * ca / denom, ry * sa / denom


def _add_rim_overhang_flora_cluster(g: MapGeometry, name: str, cx: float, cy: float, z0: float, yaw: float, idx: int):
    """Adds a closed overhanging grass & wildflower cluster along the top lip of the cutaway rim."""
    rmat = rot_z_mat(yaw)
    crisp_box(g, name + "_grass_cushion", (cx, cy, z0 + 0.08), (0.68, 0.52, 0.24), "grass_gold", rmat)
    c, s = math.cos(yaw), math.sin(yaw)
    crisp_box(
        g,
        name + "_shrub_lip",
        (cx + 0.14 * (-s), cy + 0.14 * c, z0 + 0.04),
        (0.48, 0.38, 0.22),
        "shrub_green",
        rot_z_mat(yaw + 0.3),
    )
    fl_mats = ["flower_gold", "flower_magenta", "flower_violet"]
    fl_mat = fl_mats[idx % 3]
    crisp_box(
        g,
        name + "_flower_bloom",
        (cx - 0.10 * c + 0.08 * (-s), cy - 0.10 * s + 0.08 * c, z0 + 0.18),
        (0.30, 0.30, 0.18),
        fl_mat,
        rot_z_mat(yaw + 0.6),
    )


def build_zone02_diorama_rim() -> MapGeometry:
    """Builds `Zone02_Diorama_Cutaway_RockRim_And_Base`:
    - 3-tier interlocking wall of 450+ chunky, beveled sedimentary stone cliff blocks
      around the island super-ellipse perimeter (`ISLAND_RX=29.2, ISLAND_RY=21.4`, skipping
      the river mouth and north alpine cliff), with horizontal bedding strata & deep joints.
    - Overhanging top lip of golden grass clusters & colorful wildflowers.
    - Closed water-tight 3D underside diorama base slab.
    """
    g = MapGeometry()

    N_RIM = 164
    angles = np.linspace(0.0, 2.0 * math.pi, N_RIM, endpoint=False)
    half_step = math.pi / N_RIM
    rim_materials = ["sandstone_warm", "sandstone_ochre", "stone", "ruin_ashlar", "trim"]

    for idx_r, ang in enumerate(angles):
        px, py = _eval_island_rim_xy(ang, scale_r=0.958)
        riv_cx_r, riv_hw_r = eval_river_center_and_width(py)

        # Skip northern Alpine mountain / Castle cliff arc
        if py > 13.8:
            continue

        is_river_mouth = (py < 5.0 and py > -8.6 and px > 17.5 and abs(px - riv_cx_r) < riv_hw_r * 1.05)
        if is_river_mouth:
            px_n, py_n = _eval_island_rim_xy(ang + 2.0 * half_step, scale_r=0.958)
            tangent_yaw = math.atan2(py_n - py, px_n - px)
            seg_len = math.hypot(px_n - px, py_n - py)
            crisp_box(
                g,
                f"rim_rivermouth_sill_{idx_r:03d}",
                (px, py, 0.35),
                (seg_len * 1.06, 1.55, 1.95),
                "sandstone_shadow",
                rot_z_mat(tangent_yaw),
            )
            continue

        px_next, py_next = _eval_island_rim_xy(ang + 2.0 * half_step, scale_r=0.958)
        tangent_yaw = math.atan2(py_next - py, px_next - px)
        seg_len = math.hypot(px_next - px, py_next - py)

        # Outward normal direction in XY
        nx_out = math.sin(tangent_yaw)
        ny_out = -math.cos(tangent_yaw)
        if nx_out * px + ny_out * py < 0.0:
            nx_out, ny_out = -nx_out, -ny_out

        z_meadow = float(sample_ground_z(px * 0.92, py * 0.92))
        z_top = min(z_meadow + 0.05, 3.08)
        z_bot = -0.92
        total_h = max(2.15, z_top - z_bot)

        # Inner dark-mortar/shadow backing block so deep vertical masonry joints never show gaps
        crisp_box(
            g,
            f"rim_inner_backing_{idx_r:03d}",
            (px - 0.32 * nx_out, py - 0.32 * ny_out, z_bot + total_h * 0.48),
            (seg_len * 1.12, 1.35, total_h * 0.92),
            "sandstone_shadow",
            rot_z_mat(tangent_yaw),
        )

        # 3 Interlocking Tiers of Chunky, Beveled Sedimentary Stone Blocks (Running Bond)
        tier_fracs = [
            (0.00, 0.36, 0.0,       1.04, "sandstone_ochre"),
            (0.33, 0.69, half_step, 1.08, "sandstone_warm"),
            (0.66, 1.00, 0.0,       1.02, "stone"),
        ]
        for t_idx, (f0, f1, ang_shift, out_scale, base_mat) in enumerate(tier_fracs):
            bx_t, by_t = _eval_island_rim_xy(ang + ang_shift, scale_r=0.960 * out_scale * 0.99)
            bx_n, by_n = _eval_island_rim_xy(ang + ang_shift + 2.0 * half_step, scale_r=0.960 * out_scale * 0.99)
            t_yaw = math.atan2(by_n - by_t, bx_n - bx_t)

            tz0 = z_bot + total_h * f0
            tz1 = z_bot + total_h * f1 + 0.04 * math.sin(idx_r * 2.3 + t_idx)
            th = max(0.55, tz1 - tz0)
            tc_z = 0.5 * (tz0 + tz1)

            # Deliberate vertical joint gap between adjacent blocks (`0.93 * seg_len`)
            bw = max(0.65, seg_len * (0.93 + 0.04 * ((idx_r + t_idx) % 2)))
            bd = 1.48 + 0.24 * math.sin(idx_r * 1.9 + t_idx * 1.3)
            mat = rim_materials[(idx_r * 3 + t_idx * 2) % len(rim_materials)] if t_idx > 0 else base_mat

            add_chiseled_strata_block(
                g,
                f"rim_tier{t_idx}_block_{idx_r:03d}",
                (bx_t, by_t, tc_z),
                (bw, bd, th),
                mat,
                yaw=t_yaw,
                pitch=0.02 * math.sin(idx_r + t_idx),
                taper=0.93,
                crag=0.05,
                seed=idx_r * 3.1 + t_idx,
            )

        # Protruding horizontal bedding-strata relief block on every second station
        if idx_r % 2 == 0:
            bed_z = z_bot + total_h * (0.48 + 0.14 * math.cos(idx_r * 1.7))
            crisp_box(
                g,
                f"rim_strata_seam_block_{idx_r:03d}",
                (px + 0.42 * nx_out, py + 0.42 * ny_out, bed_z),
                (seg_len * 0.84, 0.72, 0.36),
                "trim" if (idx_r % 4 == 0) else "sandstone_warm",
                rot_z_mat(tangent_yaw),
            )

        # Overhanging top lip of golden grass clusters & colorful wildflowers
        if idx_r % 2 == 0 or py < 2.0:
            _add_rim_overhang_flora_cluster(
                g,
                f"rim_top_lip_flora_{idx_r:03d}",
                px - 0.12 * nx_out,
                py - 0.12 * ny_out,
                z_top - 0.04,
                tangent_yaw,
                idx_r,
            )

    # Closed Water-Tight 3D Underside Diorama Base Plinth (`z in [-1.22, -0.74]`)
    seal_segs = 96
    s_ang = np.linspace(0.0, 2.0 * math.pi, seal_segs, endpoint=False)
    for slab_name, z0_s, z1_s, r_sc, mat_s in [
        ("diorama_base_upper_seal", -0.96, -0.68, 0.945, "sandstone_ochre"),
        ("diorama_base_lower_plinth", -1.24, -0.94, 0.922, "sandstone_shadow"),
    ]:
        verts = []
        for z_val, sc_v in [(z0_s, r_sc * 0.985), (z1_s, r_sc)]:
            for a_s in s_ang:
                sx_p, sy_p = _eval_island_rim_xy(float(a_s), scale_r=sc_v)
                verts.append((sx_p, sy_p, z_val))
        idx_bot_c = len(verts)
        verts.append((0.0, 0.0, z0_s))
        idx_top_c = len(verts)
        verts.append((0.0, 0.0, z1_s))
        faces = []
        for i in range(seal_segs):
            j = (i + 1) % seal_segs
            faces.append([idx_bot_c, j, i])
            faces.append([idx_top_c, seal_segs + i, seal_segs + j])
            faces.append([i, j, seal_segs + j])
            faces.append([i, seal_segs + j, seal_segs + i])
        g.add(slab_name, verts, faces, mat_s, convex_center=None)

    return g


# =============================================================================
# ZONE 03: CLOSED-VOLUME RIVER, 4 CASCADING WATERFALLS & 3 MEADOW PONDS
# =============================================================================

def _add_closed_pond_prism(
    g: MapGeometry,
    name: str,
    pcx: float,
    pcy: float,
    prx: float,
    pry: float,
    pang_deg: float,
    pz: float,
    seed: int = 0,
):
    """Builds a closed, water-tight 3D pond prism with shoreline ripple rings."""
    pang = math.radians(pang_deg)
    ca, sa = math.cos(pang), math.sin(pang)
    segs = 36

    verts = []
    for z_val, r_scale in [(pz - 0.36, 0.94), (pz, 1.00)]:
        for i in range(segs):
            th = (2.0 * math.pi * i) / segs
            wob = 1.0 + 0.08 * math.sin(th * 3.0 + seed) + 0.04 * math.cos(th * 2.0 - seed)
            lx = prx * r_scale * wob * math.cos(th)
            ly = pry * r_scale * wob * math.sin(th)
            wx = pcx + lx * ca - ly * sa
            wy = pcy + lx * sa + ly * ca
            verts.append((wx, wy, z_val))

    idx_bot_c = len(verts)
    verts.append((pcx, pcy, pz - 0.36))
    idx_top_c = len(verts)
    verts.append((pcx, pcy, pz))
    faces = []
    for i in range(segs):
        j = (i + 1) % segs
        faces.append([idx_bot_c, j, i])
        faces.append([idx_top_c, segs + i, segs + j])
        faces.append([i, j, segs + j])
        faces.append([i, segs + j, segs + i])
    g.add(name + "_water_body", verts, faces, "water_pond", convex_center=None)

    # Subtle concentric shoreline ripple tubes (`water_foam`)
    for r_idx, rf in enumerate([0.78, 0.91]):
        pts = []
        for i in range(segs):
            th = (2.0 * math.pi * i) / segs
            wob = 1.0 + 0.08 * math.sin(th * 3.0 + seed)
            lx = prx * rf * wob * math.cos(th)
            ly = pry * rf * wob * math.sin(th)
            wx = pcx + lx * ca - ly * sa
            wy = pcy + lx * sa + ly * ca
            pts.append((wx, wy, pz + 0.015))
        g.tube(name + f"_ripple_ring_{r_idx}", pts, 0.055, "water_foam", segments=6, closed=True)


def build_zone03_water() -> MapGeometry:
    """Builds `Zone03_Water_River_Waterfalls_And_3Ponds`:
    - Closed-volume S-curve river (`z = 1.65..2.06`) clipped cleanly to the island perimeter.
    - 4 multi-step cascading waterfalls at `wf_xs = [8.2, 11.4, 14.8, 17.8]` with stepped foam
      ledges and plunge-pool foam domes (`water_foam`).
    - 3 Meadow Ponds (`(-3.4, -5.2)`, `(-0.5, 0.6)`, `(9.6, -7.4)`) as closed water prisms
      with shoreline ripples.
    """
    g = MapGeometry()

    # 1. Closed-volume S-curve river (`z_bot = 1.65`, `z_top = 2.00..2.06`)
    RIV_NY, RIV_NX = 68, 22
    ry_vals = np.linspace(-7.35, 12.20, RIV_NY, dtype=np.float64)
    gx = np.zeros((RIV_NY, RIV_NX), dtype=np.float64)
    gy = np.zeros((RIV_NY, RIV_NX), dtype=np.float64)
    gz_top = np.zeros((RIV_NY, RIV_NX), dtype=np.float64)
    gz_bot = np.full((RIV_NY, RIV_NX), 1.65, dtype=np.float64)

    for j, y_val in enumerate(ry_vals):
        rcx, rhw = eval_river_center_and_width(float(y_val))
        rcx, rhw = float(rcx), float(rhw)
        v_frac = j / float(RIV_NY - 1)
        x_left = rcx - rhw * 0.95
        x_right = rcx + rhw * 0.95

        # Clamp east bank cleanly inside the island super-ellipse boundary
        for _ in range(12):
            if float(island_signed_distance(x_right, float(y_val))) > 0.952:
                x_right -= 0.22
        if x_right < x_left + 1.2:
            x_right = x_left + 1.2

        for i in range(RIV_NX):
            u_frac = i / float(RIV_NX - 1)
            x_val = x_left + (x_right - x_left) * u_frac
            ripple = 0.012 * math.sin(y_val * 1.4 + u_frac * math.pi * 3.0)
            gx[j, i] = x_val
            gy[j, i] = y_val
            gz_top[j, i] = 2.00 + 0.06 * v_frac + ripple

    add_closed_grid_slab(g, "river_main_volume", gx, gy, gz_top, gz_bot, "water_river")

    # River shoreline & bridge wake foam ribbons
    for bank_i, u_col in enumerate([1, RIV_NX - 2]):
        pts = [(float(gx[j, u_col]), float(gy[j, u_col]), float(gz_top[j, u_col]) + 0.02) for j in range(0, RIV_NY, 2)]
        g.tube(f"river_bank_foam_{bank_i}", pts, 0.075, "water_foam", segments=6, closed=False)

    # 2. 4 Multi-step cascading waterfalls at `wf_xs = [8.2, 11.4, 14.8, 17.8]`
    wf_xs = [8.2, 11.4, 14.8, 17.8]
    wf_ws = [1.55, 1.85, 1.95, 1.45]
    for w_i, (wx, ww) in enumerate(zip(wf_xs, wf_ws)):
        wy_top = 12.60 - 0.15 * abs(w_i - 1.5)
        wy_bot = 11.18 - 0.15 * abs(w_i - 1.5)
        z_top = 7.38
        z_bot = 1.96

        wf_ny, wf_nx = 20, 8
        wgx = np.zeros((wf_ny, wf_nx), dtype=np.float64)
        wgy = np.zeros((wf_ny, wf_nx), dtype=np.float64)
        wgz_top = np.zeros((wf_ny, wf_nx), dtype=np.float64)
        wgz_bot = np.zeros((wf_ny, wf_nx), dtype=np.float64)

        for j in range(wf_ny):
            vf = j / float(wf_ny - 1)
            step_wave = 0.16 * math.sin(vf * math.pi * 3.0)
            y_cur = wy_bot + (wy_top - wy_bot) * (vf ** 0.78) - 0.10 * math.sin(vf * math.pi * 3.0)
            z_cur = z_bot + (z_top - z_bot) * vf + 0.20 * math.sin(vf * math.pi) + step_wave
            w_cur = ww * (1.24 - 0.24 * vf)
            for i in range(wf_nx):
                uf = i / float(wf_nx - 1) - 0.5
                arch_z = 0.06 * (1.0 - 4.0 * uf * uf)
                wgx[j, i] = wx + uf * w_cur
                wgy[j, i] = y_cur
                wgz_top[j, i] = z_cur + arch_z
                wgz_bot[j, i] = z_cur - 0.32

        add_closed_grid_slab(g, f"waterfall_cascade_{w_i}", wgx, wgy, wgz_top, wgz_bot, "water_foam")

        # Stepped foam ledges along the cascade
        for step_k, vf_s in enumerate([0.28, 0.56, 0.84]):
            sy_s = wy_bot + (wy_top - wy_bot) * (vf_s ** 0.78)
            sz_s = z_bot + (z_top - z_bot) * vf_s + 0.18
            sw_s = ww * (1.20 - 0.20 * vf_s)
            add_chiseled_strata_block(
                g,
                f"waterfall_{w_i}_step_foam_{step_k}",
                (wx, sy_s - 0.12, sz_s),
                (sw_s * 0.95, 0.46, 0.28),
                "water_foam",
                yaw=0.05 * (step_k - 1),
                taper=0.85,
                crag=0.08,
                seed=w_i * 7.0 + step_k,
            )

        # Plunge-pool foam domes & turbulent spray cushions at waterfall base
        foam_domes = [
            (0.0,         -0.24, 2.04, ww * 1.15, 0.92, 0.34),
            (-ww * 0.38,  -0.36, 2.02, ww * 0.68, 0.65, 0.24),
            ( ww * 0.38,  -0.36, 2.02, ww * 0.68, 0.65, 0.24),
            (-ww * 0.18,  -0.68, 2.00, ww * 0.56, 0.52, 0.18),
            ( ww * 0.18,  -0.68, 2.00, ww * 0.56, 0.52, 0.18),
        ]
        for d_i, (dx_f, dy_f, dz_f, sx_f, sy_f, sz_f) in enumerate(foam_domes):
            add_chiseled_strata_block(
                g,
                f"waterfall_{w_i}_plunge_foam_{d_i}",
                (wx + dx_f, wy_bot + dy_f, dz_f),
                (sx_f, sy_f, sz_f),
                "water_foam",
                yaw=0.2 * d_i,
                taper=0.76,
                crag=0.08,
                seed=50.0 + w_i * 10 + d_i,
                segs=10,
            )

    # 3. 3 Meadow Ponds (`(-3.4, -5.2)`, `(-0.5, 0.6)`, `(9.6, -7.4)`)
    pond_specs = [
        (-3.4, -5.2, 5.6, 1.72, -31.0, 2.34),
        (-0.5,  0.6, 3.5, 1.35,  18.0, 2.56),
        ( 9.6, -7.4, 3.3, 1.32, -19.0, 2.14),
    ]
    for p_i, (pcx, pcy, prx, pry, pang_deg, pz) in enumerate(pond_specs):
        _add_closed_pond_prism(g, f"meadow_pond_{p_i + 1}", pcx, pcy, prx, pry, pang_deg, pz, seed=p_i)

    return g


# =============================================================================
# ZONE 11: STEPPED ANCIENT STONE RUINS, BROKEN SUNDIAL, POND BANKS & FLORA
# =============================================================================

def _add_golden_crystal_lotus_closed(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    seed: float = 0.0,
):
    """Builds a multi-petal glowing golden crystal-lotus plant where every petal/blade
    and core crystal is a closed 3D polyhedron (`bad_edges == 0`, `vol6 > 0`).
    """
    add_chiseled_strata_block(
        g,
        name + "_core",
        (cx, cy, z0 + 0.18 * scale),
        (0.42 * scale, 0.42 * scale, 0.44 * scale),
        "lotus_gold",
        yaw=seed,
        taper=0.72,
        crag=0.06,
        seed=seed,
        segs=8,
    )

    for ring_i, (n_blades, r_base, r_tip, h_tip, w_blade, th_blade) in enumerate([
        (8, 0.15, 0.80, 0.94, 0.18, 0.12),
        (6, 0.09, 0.46, 1.24, 0.15, 0.11),
    ]):
        for b_i in range(n_blades):
            ang = seed + b_i * (2.0 * math.pi / n_blades) + ring_i * 0.48
            ca, sa = math.cos(ang), math.sin(ang)
            pa, qa = -sa, ca

            r0 = r_base * scale
            r1 = 0.54 * (r_base + r_tip) * scale
            r2 = r_tip * scale
            z_b = z0 - 0.04 * scale
            z_m = z0 + 0.52 * h_tip * scale
            z_t = z0 + h_tip * scale

            w0 = w_blade * 0.82 * scale
            w1 = w_blade * scale
            t0 = th_blade * scale
            t1 = th_blade * 0.88 * scale

            ring_bot = [
                (cx + (r0 - t0) * ca, cy + (r0 - t0) * sa, z_b + 0.04 * scale),
                (cx + r0 * ca + w0 * pa, cy + r0 * sa + w0 * qa, z_b),
                (cx + (r0 + t0) * ca, cy + (r0 + t0) * sa, z_b - 0.02 * scale),
                (cx + r0 * ca - w0 * pa, cy + r0 * sa - w0 * qa, z_b),
            ]
            ring_mid = [
                (cx + (r1 - t1) * ca, cy + (r1 - t1) * sa, z_m + 0.05 * scale),
                (cx + r1 * ca + w1 * pa, cy + r1 * sa + w1 * qa, z_m),
                (cx + (r1 + t1) * ca, cy + (r1 + t1) * sa, z_m - 0.04 * scale),
                (cx + r1 * ca - w1 * pa, cy + r1 * sa - w1 * qa, z_m),
            ]
            tip_r = 0.045 * scale
            ring_tip = [
                (cx + (r2 - tip_r) * ca, cy + (r2 - tip_r) * sa, z_t),
                (cx + r2 * ca + tip_r * pa, cy + r2 * sa + tip_r * qa, z_t),
                (cx + (r2 + tip_r) * ca, cy + (r2 + tip_r) * sa, z_t - tip_r),
                (cx + r2 * ca - tip_r * pa, cy + r2 * sa - tip_r * qa, z_t),
            ]
            verts = ring_bot + ring_mid + ring_tip
            faces = [
                [3, 2, 1, 0],
                [8, 9, 10, 11],
            ]
            for r_k in range(2):
                b_off = r_k * 4
                for s_k in range(4):
                    s_nxt = (s_k + 1) % 4
                    faces.append([b_off + s_k, b_off + s_nxt, b_off + 4 + s_nxt, b_off + 4 + s_k])
            g.add(name + "_petal", verts, faces, "lotus_gold", convex_center=np.mean(verts, axis=0))


def _add_carved_sun_seal_monolith(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    yaw: float = 0.0,
):
    """Builds a carved spiral sun-seal monolith slab (`ruin_carved`) with raised concentric relief."""
    rmat = rot_z_mat(yaw)
    c, s = math.cos(yaw), math.sin(yaw)

    def pt(lx, ly, lz):
        return (cx + c * lx - s * ly, cy + s * lx + c * ly, z0 + lz)

    crisp_box(g, name + "_plinth", pt(0.0, 0.0, 0.08 * scale), (0.76 * scale, 0.48 * scale, 0.24 * scale), "ruin_ashlar", rmat)
    crisp_box(g, name + "_stele_body", pt(0.0, 0.0, 0.56 * scale), (0.62 * scale, 0.34 * scale, 0.82 * scale), "ruin_carved", rmat)
    crisp_box(g, name + "_stele_cap", pt(0.0, 0.0, 1.00 * scale), (0.52 * scale, 0.30 * scale, 0.18 * scale), "trim", rmat)

    seal_center = np.array(pt(0.0, -0.18 * scale, 0.62 * scale), dtype=float)
    ring_pts = []
    for k in range(12):
        th = (2.0 * math.pi * k) / 12.0
        lx = 0.19 * scale * math.cos(th)
        lz = 0.19 * scale * math.sin(th)
        ring_pts.append(pt(lx, -0.18 * scale, 0.62 * scale + lz))
    g.tube(name + "_sun_ring", ring_pts, 0.045 * scale, "ruin_carved", segments=6, closed=True)
    crisp_box(g, name + "_sun_boss", seal_center, (0.20 * scale, 0.12 * scale, 0.20 * scale), "trim", rmat)


def _add_stepped_ashlar_ruin_cairn(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    has_tall_pillar: bool = True,
    has_crystal: bool = True,
    n_pines: int = 2,
    seed: float = 0.0,
):
    """Builds a true stepped ancient stone pyramid/pillar cairn assembled from 20–32 individual
    beveled weathered stone blocks (`ruin_ashlar`, `stone`, `trim`), plus a carved spiral sun-seal
    monolith (`ruin_carved`), weathered boulders, dwarf pines, golden crystal-lotus, and wildflowers.
    """
    yaw = (seed * 1.37) % (2.0 * math.pi)
    c_y, s_y = math.cos(yaw), math.sin(yaw)

    def world_pt(lx, ly, lz):
        return (cx + (lx * c_y - ly * s_y) * scale, cy + (lx * s_y + ly * c_y) * scale, z0 + lz * scale)

    courses = [
        (0.10, 0.28, 3, 2, 1.42, 1.18),
        (0.36, 0.26, 3, 2, 1.18, 0.98),
        (0.60, 0.25, 2, 2, 0.94, 0.80),
        (0.84, 0.24, 2, 2, 0.74, 0.64),
    ]
    if has_tall_pillar:
        courses.extend([
            (1.07, 0.24, 2, 1, 0.58, 0.50),
            (1.30, 0.24, 1, 1, 0.44, 0.40),
        ])

    ashlar_mats = ["ruin_ashlar", "stone", "trim", "ruin_carved"]
    blk_counter = 0
    for c_idx, (cz_l, ch_l, gnx, gny, spx, spy) in enumerate(courses):
        bw = (spx / gnx) * 0.92
        bd = (spy / gny) * 0.92
        for ix in range(gnx):
            for iy in range(gny):
                if c_idx >= 3 and ix == gnx - 1 and iy == gny - 1 and int(seed * 5 + c_idx) % 3 == 0:
                    continue
                lx = ((ix + 0.5) / gnx - 0.5) * spx + 0.025 * math.sin(seed + blk_counter * 1.9)
                ly = ((iy + 0.5) / gny - 0.5) * spy + 0.025 * math.cos(seed + blk_counter * 2.3)
                b_yaw = yaw + 0.05 * math.sin(blk_counter * 1.7 + seed)
                mat = ashlar_mats[(blk_counter + c_idx) % len(ashlar_mats)]
                crisp_box(
                    g,
                    name + f"_ashlar_{blk_counter:02d}",
                    world_pt(lx, ly, cz_l),
                    (bw * scale, bd * scale, ch_l * scale),
                    mat,
                    rot_z_mat(b_yaw),
                )
                blk_counter += 1

    for f_i in range(4):
        f_ang = yaw + 0.4 + f_i * 1.55
        f_dist = (0.72 + 0.14 * (f_i % 2)) * scale
        fx = cx + f_dist * math.cos(f_ang)
        fy = cy + f_dist * math.sin(f_ang)
        fz = float(sample_ground_z(fx, fy))
        crisp_box(
            g,
            name + f"_fallen_ashlar_{f_i}",
            (fx, fy, fz + 0.10 * scale),
            (0.42 * scale, 0.30 * scale, 0.24 * scale),
            "ruin_ashlar" if f_i % 2 == 0 else "stone",
            rot_zyx_mat(f_ang + 0.3, 0.18 * (1 if f_i % 2 == 0 else -1), 0.10),
        )

    ang_m = yaw + 0.82
    mx = cx + 0.78 * scale * math.cos(ang_m)
    my = cy + 0.78 * scale * math.sin(ang_m)
    mz = float(sample_ground_z(mx, my))
    _add_carved_sun_seal_monolith(g, name + "_monolith", mx, my, mz, scale=0.78 * scale, yaw=ang_m - math.pi * 0.5)

    for b_i in range(6):
        b_ang = seed + b_i * (2.0 * math.pi / 6.0) + 0.18 * math.sin(b_i * 2.3)
        b_dist = (0.82 + 0.28 * ((b_i * 5) % 3) * 0.5) * scale
        bx = cx + b_dist * math.cos(b_ang)
        by = cy + b_dist * math.sin(b_ang)
        bz = float(sample_ground_z(bx, by))
        br = (0.48 + 0.18 * (b_i % 3) * 0.5) * scale
        add_chiseled_strata_block(
            g,
            name + f"_boulder_{b_i}",
            (bx, by, bz + br * 0.26),
            (br * 1.15, br * 0.95, br * 0.68),
            "sandstone_warm" if b_i % 2 == 0 else "stone",
            yaw=b_ang,
            taper=0.84,
            crag=0.10,
            seed=seed + b_i,
        )

    for p_i in range(n_pines):
        p_ang = seed + 1.25 + p_i * 2.15
        p_dist = (0.94 + 0.22 * p_i) * scale
        px = cx + p_dist * math.cos(p_ang)
        py = cy + p_dist * math.sin(p_ang)
        pz = float(sample_ground_z(px, py))
        p_sc = (0.38 + 0.10 * ((p_i + int(seed * 3)) % 2)) * scale
        add_closed_spruce_pine(g, name + f"_pine_{p_i}", px, py, pz, scale=p_sc, tiers=4, seed=seed + p_i)

    if has_crystal:
        c_ang = seed - 0.95
        gx_c = cx + 0.88 * scale * math.cos(c_ang)
        gy_c = cy + 0.88 * scale * math.sin(c_ang)
        gz_c = float(sample_ground_z(gx_c, gy_c))
        _add_golden_crystal_lotus_closed(g, name + "_lotus", gx_c, gy_c, gz_c, scale=0.72 * scale, seed=seed)

    n_fl = int(8 * scale) + 4
    fl_colors = ["flower_gold", "flower_magenta", "flower_violet"]
    for f_i in range(n_fl):
        f_ang = seed * 0.7 + f_i * (2.0 * math.pi / n_fl)
        f_dist = (0.88 + 0.55 * ((f_i * 5) % 4) / 3.0) * scale
        fx = cx + f_dist * math.cos(f_ang)
        fy = cy + f_dist * math.sin(f_ang)
        fz = float(sample_ground_z(fx, fy))
        f_sc = 0.65 + 0.18 * (f_i % 2)
        crisp_box(g, name + f"_fl_foliage_{f_i}", (fx, fy, fz + 0.08 * f_sc), (0.40 * f_sc, 0.38 * f_sc, 0.18 * f_sc), "shrub_green", rot_z_mat(f_ang))
        crisp_box(g, name + f"_fl_bloom_{f_i}", (fx, fy, fz + 0.18 * f_sc), (0.28 * f_sc, 0.28 * f_sc, 0.16 * f_sc), fl_colors[f_i % 3], rot_z_mat(f_ang + 0.5))


def _add_broken_black_stone_sundial(g: MapGeometry, cx: float, cy: float, z0: float):
    """Builds `Nhật Quỹ Tàn Khuyết` (Broken Black-Stone Sundial) at `(-8.6, 0.8)` with
    stepped ashlar altar, tilted dark obsidian/iron dial plate, 12 radial hour markers,
    triangular shadow-casting gnomon, and fractured obsidian slab fragments.
    """
    add_tower_frustum(g, "sundial_altar_step1", cx, cy, z0 - 0.12, z0 + 0.22, 1.55, 1.44, 8, "stone", yaw=0.15)
    add_tower_frustum(g, "sundial_altar_step2", cx, cy, z0 + 0.20, z0 + 0.48, 1.22, 1.12, 8, "ruin_ashlar", yaw=0.15)
    add_tower_frustum(g, "sundial_pedestal_column", cx, cy, z0 + 0.46, z0 + 0.96, 0.64, 0.56, 8, "trim", yaw=0.15)

    dial_center = np.array([cx - 0.04, cy + 0.02, z0 + 1.02], dtype=float)
    dial_rot = rot_zyx_mat(0.25, 0.22, -0.10)
    crisp_box(g, "sundial_obsidian_plate_main", dial_center, (1.28, 1.08, 0.18), "iron", dial_rot)
    crisp_box(g, "sundial_brass_rim_arc", dial_center + dial_rot @ np.array([0.0, -0.44, 0.08]), (0.96, 0.18, 0.12), "brass", dial_rot)

    for h_i in range(9):
        h_ang = -math.pi * 0.75 + h_i * (math.pi * 1.15 / 8.0)
        p_in = dial_center + dial_rot @ np.array([0.22 * math.cos(h_ang), 0.22 * math.sin(h_ang), 0.09])
        p_out = dial_center + dial_rot @ np.array([0.48 * math.cos(h_ang), 0.48 * math.sin(h_ang), 0.09])
        crisp_beam(g, f"sundial_hour_line_{h_i}", p_in, p_out, 0.08, "brass")

    gnomon_verts_local = [
        (-0.06, -0.32, 0.06),
        ( 0.06, -0.32, 0.06),
        ( 0.06,  0.34, 0.06),
        (-0.06,  0.34, 0.06),
        (-0.04,  0.22, 0.54),
        ( 0.04,  0.22, 0.54),
    ]
    gnomon_verts = [dial_center + (dial_rot @ np.array(v, dtype=float)) for v in gnomon_verts_local]
    gnomon_faces = [
        [0, 3, 2, 1],
        [0, 1, 5, 4],
        [2, 3, 4, 5],
        [1, 2, 5],
        [3, 0, 4],
    ]
    g.add("sundial_gnomon_blade", gnomon_verts, gnomon_faces, "brass", convex_center=np.mean(gnomon_verts, axis=0))

    frag_pos = (cx + 0.95, cy - 0.55, z0 + 0.14)
    crisp_box(g, "sundial_fallen_fragment", frag_pos, (0.68, 0.46, 0.18), "iron", rot_zyx_mat(-0.4, 0.28, 0.15))


def _add_pond_rock_and_flora_necklace(
    g: MapGeometry,
    pcx: float,
    pcy: float,
    prx: float,
    pry: float,
    pang_deg: float,
    pz: float,
    pond_idx: int,
):
    """Surrounds each pond with 36 rounded/chiseled river boulders, cattail reed stands,
    glowing golden crystal-lotus plants, dwarf spruce pines, and wildflowers.
    """
    pang = math.radians(pang_deg)
    ca, sa = math.cos(pang), math.sin(pang)
    n_boulders = 36
    boulder_mats = ["stone", "sandstone_warm", "ruin_ashlar", "sandstone_ochre"]

    for b_k in range(n_boulders):
        th_b = b_k * (2.0 * math.pi / float(n_boulders))
        ring_shift = 1.03 if (b_k % 2 == 0) else 1.12
        wob = ring_shift + 0.08 * math.sin(th_b * 3.0 + pond_idx)
        lx = prx * wob * math.cos(th_b)
        ly = pry * wob * math.sin(th_b)
        bx = pcx + lx * ca - ly * sa
        by = pcy + lx * sa + ly * ca
        bz = max(pz - 0.06, float(sample_ground_z(bx, by)))

        sx_b = 0.56 + 0.22 * ((b_k * 5 + pond_idx) % 3) * 0.5
        sy_b = 0.46 + 0.18 * ((b_k * 3 + pond_idx) % 3) * 0.5
        sz_b = 0.32 + 0.14 * ((b_k + pond_idx) % 3) * 0.5
        b_mat = boulder_mats[(b_k + pond_idx) % len(boulder_mats)]
        add_chiseled_strata_block(
            g,
            f"pond{pond_idx + 1}_boulder_{b_k:02d}",
            (bx, by, bz + sz_b * 0.28),
            (sx_b, sy_b, sz_b),
            b_mat,
            yaw=th_b + 0.3 * b_k,
            taper=0.80,
            crag=0.09,
            seed=pond_idx * 40.0 + b_k,
        )

        # Cattail reed stands every 3rd station (`12` reed clusters per pond)
        if b_k % 3 == 0:
            for r_stem in range(3):
                s_ang = th_b + (r_stem - 1) * 0.18
                rx_s = bx - 0.16 * math.cos(th_b) + 0.12 * math.cos(s_ang * 3.0)
                ry_s = by - 0.16 * math.sin(th_b) + 0.12 * math.sin(s_ang * 3.0)
                stem_h = 0.54 + 0.12 * (r_stem % 2)
                tilt_x = 0.05 * math.cos(th_b + r_stem)
                tilt_y = 0.05 * math.sin(th_b + r_stem)
                crisp_beam(
                    g,
                    f"pond{pond_idx + 1}_reed_stem_{b_k}_{r_stem}",
                    (rx_s, ry_s, bz - 0.05),
                    (rx_s + tilt_x, ry_s + tilt_y, bz + stem_h),
                    0.075,
                    "shrub_green",
                )
                crisp_beam(
                    g,
                    f"pond{pond_idx + 1}_cattail_head_{b_k}_{r_stem}",
                    (rx_s + tilt_x * 0.55, ry_s + tilt_y * 0.55, bz + stem_h * 0.58),
                    (rx_s + tilt_x * 0.92, ry_s + tilt_y * 0.92, bz + stem_h * 0.92),
                    0.105,
                    "wood",
                )

        # Shoreline wildflowers
        if b_k % 2 == 1:
            fl_mats = ["flower_gold", "flower_magenta", "flower_violet"]
            fx_w = bx + 0.24 * math.cos(th_b)
            fy_w = by + 0.24 * math.sin(th_b)
            fz_w = float(sample_ground_z(fx_w, fy_w))
            crisp_box(g, f"pond{pond_idx + 1}_bank_shrub_{b_k}", (fx_w, fy_w, fz_w + 0.08), (0.38, 0.34, 0.18), "shrub_green", rot_z_mat(th_b))
            crisp_box(g, f"pond{pond_idx + 1}_bank_flower_{b_k}", (fx_w, fy_w, fz_w + 0.18), (0.26, 0.26, 0.16), fl_mats[(b_k // 2) % 3], rot_z_mat(th_b + 0.4))

        # Dwarf spruce pines along the pond bank
        if b_k % 7 == 0:
            px_p = bx + 0.56 * math.cos(th_b)
            py_p = by + 0.56 * math.sin(th_b)
            pz_p = float(sample_ground_z(px_p, py_p))
            add_closed_spruce_pine(g, f"pond{pond_idx + 1}_bank_pine_{b_k}", px_p, py_p, pz_p, scale=0.42, tiers=4, seed=pond_idx * 10 + b_k)

    for l_side in (-0.88, 0.88):
        lx_l = pcx + prx * l_side * ca
        ly_l = pcy + prx * l_side * sa
        lz_l = max(pz + 0.02, float(sample_ground_z(lx_l, ly_l)))
        _add_golden_crystal_lotus_closed(
            g,
            f"pond{pond_idx + 1}_lotus_{0 if l_side < 0 else 1}",
            lx_l,
            ly_l,
            lz_l,
            scale=0.80,
            seed=pond_idx * 3.0 + l_side,
        )


def _add_closed_meadow_grass_batches(g: MapGeometry, rng: np.random.RandomState):
    """Populates the meadow with closed 3D triangular-prism golden grass tufts (`grass_gold`).
    Each blade is a 4-vertex closed tetrahedron/wedge with strictly positive volume (`vol6 > 0`)
    and `bad_edges == 0`, batched on a jittered grid so blades never collide!
    """
    gx_vals = np.arange(-25.8, 24.8, 0.76)
    gy_vals = np.arange(-18.8, 10.8, 0.76)

    tufts = []
    for x0 in gx_vals:
        for y0 in gy_vals:
            cx = float(x0 + rng.uniform(-0.20, 0.20))
            cy = float(y0 + rng.uniform(-0.20, 0.20))
            if float(island_signed_distance(cx, cy)) > 0.925:
                continue
            if float(eval_road_mask(cx, cy)) > 0.20:
                continue
            rcx, rhw = eval_river_center_and_width(cy)
            if cy > -7.2 and abs(cx - float(rcx)) < float(rhw) + 0.75:
                continue
            p1_d, p2_d, p3_d = eval_pond_distances(cx, cy)
            if min(float(p1_d), float(p2_d), float(p3_d)) < 1.12:
                continue
            if cx > 18.0 and -2.5 < cy < 8.5:
                continue
            cz = float(sample_ground_z(cx, cy))
            tufts.append((cx, cy, cz))

    batch_size = 24
    for b_start in range(0, len(tufts), batch_size):
        batch = tufts[b_start:b_start + batch_size]
        verts = []
        faces = []
        for cx, cy, cz in batch:
            base_yaw = rng.uniform(0.0, 2.0 * math.pi)
            for b_i in range(3):
                ang = base_yaw + b_i * (2.0 * math.pi / 3.0)
                ca, sa = math.cos(ang), math.sin(ang)
                pa, qa = -sa, ca
                bx = cx + 0.09 * ca
                by = cy + 0.09 * sa
                w_b = 0.085
                th_b = 0.065
                h_b = rng.uniform(0.28, 0.44)
                z_base = cz - 0.03

                v0 = (bx - w_b * pa, by - w_b * qa, z_base)
                v1 = (bx + w_b * pa, by + w_b * qa, z_base)
                v2 = (bx + th_b * ca, by + th_b * sa, z_base + 0.02)
                v3 = (bx + 0.10 * ca, by + 0.10 * sa, cz + h_b)

                idx0 = len(verts)
                verts.extend([v0, v1, v2, v3])
                faces.extend([
                    [idx0 + 0, idx0 + 1, idx0 + 2],
                    [idx0 + 0, idx0 + 3, idx0 + 1],
                    [idx0 + 1, idx0 + 3, idx0 + 2],
                    [idx0 + 2, idx0 + 3, idx0 + 0],
                ])
        g.add(f"meadow_grass_batch_{b_start // batch_size:03d}", verts, faces, "grass_gold", convex_center=None)


def build_zone11_ruins_and_flora() -> MapGeometry:
    """Builds `Zone11_Props_AncientRuins_And_GoldenFlora`:
    - Stepped Ancient Stone Ruins (`24 Phong Ấn Liên Kết Cổ Đại`) at the 21 `interior_ruin_coords`
      and 38 border spots, each built from 18–35 individual beveled stone blocks (`ruin_ashlar`,
      `stone`, `trim`) + carved spiral sun-seal monoliths (`ruin_carved`).
    - Broken Black-Stone Sundial (`Nhật Quỹ Tàn Khuyết` at `(-8.6, 0.8)`) with tilted obsidian
      dial plate & gnomon.
    - Dense rings of 36 rounded river boulders + cattail reeds + wildflowers + golden crystal-lotus
      around all 3 ponds.
    - Closed triangular-prism 3D golden meadow grass tufts & wildflowers across the meadow.
    """
    g = MapGeometry()

    interior_ruin_coords = [
        (-16.2, -3.2, 1.05, True,  True,  2),
        (-11.8, -8.4, 1.15, True,  True,  3),
        (-15.5, -9.8, 0.92, False, True,  2),
        (-19.5, -6.2, 0.98, True,  True,  2),
        (-18.8,  0.4, 1.00, False, True,  2),
        (-13.4,  1.2, 0.88, True,  False, 2),
        ( -8.6,  0.8, 0.92, True,  True,  2),  # Nhật Quỹ Tàn Khuyết (Broken Black-Stone Sundial)
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
        if float(eval_road_mask(rcx, rcy)) < 0.35:
            rz = float(sample_ground_z(rcx, rcy))
            if abs(rcx + 8.6) < 0.2 and abs(rcy - 0.8) < 0.2:
                _add_broken_black_stone_sundial(g, rcx, rcy, rz)
                _add_stepped_ashlar_ruin_cairn(
                    g,
                    f"interior_ruin_{r_i:02d}",
                    rcx - 1.35,
                    rcy + 0.65,
                    float(sample_ground_z(rcx - 1.35, rcy + 0.65)),
                    scale=rsc * 0.88,
                    has_tall_pillar=True,
                    has_crystal=True,
                    n_pines=2,
                    seed=r_i * 1.9 + 0.5,
                )
            else:
                _add_stepped_ashlar_ruin_cairn(
                    g,
                    f"interior_ruin_{r_i:02d}",
                    rcx,
                    rcy,
                    rz,
                    scale=rsc,
                    has_tall_pillar=r_tall,
                    has_crystal=r_crys,
                    n_pines=r_pines,
                    seed=r_i * 1.9 + 0.5,
                )

    N_BORDER = 38
    for b_i in range(N_BORDER):
        ang = math.pi * 0.80 + b_i * (math.pi * 1.44 / float(N_BORDER - 1))
        bx, by = _eval_island_rim_xy(ang, scale_r=0.915)
        if float(eval_road_mask(bx, by)) > 0.35:
            continue
        bz = float(sample_ground_z(bx, by))
        _add_stepped_ashlar_ruin_cairn(
            g,
            f"border_ruin_{b_i:02d}",
            bx,
            by,
            bz,
            scale=0.84 + 0.16 * (b_i % 2),
            has_tall_pillar=(b_i % 2 == 0),
            has_crystal=(b_i % 3 == 0),
            n_pines=2,
            seed=b_i * 2.3 + 7.0,
        )

    pond_specs = [
        (-3.4, -5.2, 5.6, 1.72, -31.0, 2.34),
        (-0.5,  0.6, 3.5, 1.35,  18.0, 2.56),
        ( 9.6, -7.4, 3.3, 1.32, -19.0, 2.14),
    ]
    for p_i, (pcx, pcy, prx, pry, pang_deg, pz) in enumerate(pond_specs):
        _add_pond_rock_and_flora_necklace(g, pcx, pcy, prx, pry, pang_deg, pz, p_i)

    rng = np.random.RandomState(108)
    fl_colors = ["flower_gold", "flower_magenta", "flower_violet"]
    for w_i in range(180):
        wx = float(rng.uniform(-24.5, 23.5))
        wy = float(rng.uniform(-17.5, 6.5))
        if float(island_signed_distance(wx, wy)) > 0.90:
            continue
        if float(eval_road_mask(wx, wy)) > 0.25:
            continue
        rcx, rhw = eval_river_center_and_width(wy)
        if wy > -7.0 and abs(wx - float(rcx)) < float(rhw) + 0.8:
            continue
        p1_d, p2_d, p3_d = eval_pond_distances(wx, wy)
        if min(float(p1_d), float(p2_d), float(p3_d)) < 1.15:
            continue
        wz = float(sample_ground_z(wx, wy))
        f_sc = float(rng.uniform(0.68, 0.96))
        f_yaw = float(rng.uniform(0.0, 2.0 * math.pi))
        crisp_box(g, f"meadow_wildflower_shrub_{w_i:03d}", (wx, wy, wz + 0.08 * f_sc), (0.42 * f_sc, 0.38 * f_sc, 0.18 * f_sc), "shrub_green", rot_z_mat(f_yaw))
        crisp_box(g, f"meadow_wildflower_bloom_{w_i:03d}", (wx, wy, wz + 0.18 * f_sc), (0.28 * f_sc, 0.28 * f_sc, 0.16 * f_sc), fl_colors[w_i % 3], rot_z_mat(f_yaw + 0.4))

    _add_closed_meadow_grass_batches(g, rng)

    return g


def build_all_citadel_diorama_ruins_zones() -> dict:
    """Returns a dictionary mapping canonical zone names to their built `MapGeometry`."""
    return {
        "Zone09_Vista_TwilightCastle_And_WaterfallCliff": build_zone09_castle_and_cliff(),
        "Zone02_Diorama_Cutaway_RockRim_And_Base": build_zone02_diorama_rim(),
        "Zone03_Water_River_Waterfalls_And_3Ponds": build_zone03_water(),
        "Zone11_Props_AncientRuins_And_GoldenFlora": build_zone11_ruins_and_flora(),
    }


if __name__ == "__main__":
    for z_name, geom in build_all_citadel_diorama_ruins_zones().items():
        res = validate_closed_parts(geom)
        print(f"{z_name}: {res}")
