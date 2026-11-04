#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""High-detail, closed-manifold 3D geometry generator for Zone07 and Zone08.

Builds:
- `Zone07` (`Zone07_Landmark_TramThongTin_InfoBeacon`): `build_zone07_beacon() -> MapGeometry`
- `Zone08` (`Zone08_Vista_StoneBridge_And_HarborTown`): `build_zone08_harbor_and_bridge() -> MapGeometry`

Every single architectural and natural component is constructed via `MapGeometry`
from `canonical_map_helpers.py` as an outward-wound, watertight 3D manifold solid
(`bad_edges == 0`, `vol6 > 0`, verified by `validate_closed_parts`).
"""
import math
import numpy as np

from canonical_map_helpers import (
    MapGeometry,
    eval_river_center_and_width,
    eval_road_mask,
    island_signed_distance,
    sample_ground_z,
)
from observatory_geometry import arch_outline, frame, prism, unit


# =============================================================================
# Core Closed-Manifold Solid Primitives
# =============================================================================

def add_hexahedron(g: MapGeometry, name: str, verts8, material: str):
    """Adds a closed 8-vertex convex hexahedron with outward-wound faces."""
    v = np.asarray(verts8, dtype=float)
    faces = [
        [3, 2, 1, 0],
        [4, 5, 6, 7],
        [0, 1, 5, 4],
        [1, 2, 6, 5],
        [2, 3, 7, 6],
        [3, 0, 4, 7],
    ]
    g.add(name, v, faces, material, np.mean(v, axis=0))


def add_curved_wedge_block(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    z1: float,
    r0_in: float,
    r0_out: float,
    r1_in: float,
    r1_out: float,
    a0: float,
    a1: float,
    material: str,
):
    """Adds an individual radial ashlar stone block (supporting tapered radii)."""
    vv = [
        (cx + r0_in * math.cos(a0), cy + r0_in * math.sin(a0), z0),
        (cx + r0_out * math.cos(a0), cy + r0_out * math.sin(a0), z0),
        (cx + r0_out * math.cos(a1), cy + r0_out * math.sin(a1), z0),
        (cx + r0_in * math.cos(a1), cy + r0_in * math.sin(a1), z0),
        (cx + r1_in * math.cos(a0), cy + r1_in * math.sin(a0), z1),
        (cx + r1_out * math.cos(a0), cy + r1_out * math.sin(a0), z1),
        (cx + r1_out * math.cos(a1), cy + r1_out * math.sin(a1), z1),
        (cx + r1_in * math.cos(a1), cy + r1_in * math.sin(a1), z1),
    ]
    add_hexahedron(g, name, vv, material)


def remap_last_part_uv(g: MapGeometry, strip_idx: int, u_loc: float, v_loc: float):
    """Remaps the UV patch of the most recently added part in `g`."""
    part = g.parts[-1]
    t0 = part["first_triangle"]
    tc = part["triangles"]
    patch = np.array(
        [[u_loc - 0.006, v_loc - 0.008], [u_loc + 0.006, v_loc - 0.008], [u_loc, v_loc + 0.008]],
        dtype=float,
    )
    patch[:, 0] = (float(strip_idx) + 0.03 + 0.94 * patch[:, 0]) / 8.0
    patch[:, 1] = 0.006 + 0.238 * patch[:, 1]
    patch_list = patch.tolist()
    for t_i in range(t0, t0 + tc):
        g.uvs[t_i * 3 : (t_i + 1) * 3] = patch_list


def curved_tube(
    g: MapGeometry,
    name: str,
    points,
    radius: float,
    material: str,
    segments: int = 8,
    closed: bool = False,
):
    """Parallel-transported closed tube (avoids frame flips on steep 3D ribs/rings)."""
    pts = np.asarray(points, dtype=float)
    n_pts = len(pts)
    if n_pts < 2:
        raise ValueError(f"curved_tube '{name}' requires at least 2 points")

    tangents = []
    for i in range(n_pts):
        if closed:
            t = pts[(i + 1) % n_pts] - pts[(i - 1) % n_pts]
        else:
            t = pts[min(i + 1, n_pts - 1)] - pts[max(0, i - 1)]
        tangents.append(unit(t))

    basis = frame(tangents[0])
    x_dir = basis[:, 0].copy()

    frames = []
    for i in range(n_pts):
        t_cur = tangents[i]
        x_proj = x_dir - np.dot(x_dir, t_cur) * t_cur
        if np.linalg.norm(x_proj) < 1e-6:
            x_proj = frame(t_cur)[:, 0]
        else:
            x_proj = unit(x_proj)
        y_proj = unit(np.cross(t_cur, x_proj))
        x_dir = x_proj
        frames.append((x_proj, y_proj))

    if closed:
        x0, y0 = frames[0]
        x_last, _ = frames[-1]
        t_end = unit(pts[0] - pts[-1])
        x_end = x_last - np.dot(x_last, t_end) * t_end
        x_end = unit(x_end) if np.linalg.norm(x_end) > 1e-6 else x0
        angle_err = math.atan2(float(np.dot(x_end, y0)), float(np.dot(x_end, x0)))
        for i in range(n_pts):
            frac = i / float(n_pts)
            ang = -angle_err * frac
            ca, sa = math.cos(ang), math.sin(ang)
            xi, yi = frames[i]
            frames[i] = (ca * xi + sa * yi, -sa * xi + ca * yi)

    verts = []
    for i, p in enumerate(pts):
        xi, yi = frames[i]
        for j in range(segments):
            th = j * math.tau / segments
            verts.append(p + radius * (math.cos(th) * xi + math.sin(th) * yi))

    faces = []
    ring_count = n_pts if closed else (n_pts - 1)
    for i in range(ring_count):
        i_next = (i + 1) % n_pts
        for j in range(segments):
            j_next = (j + 1) % segments
            a = i * segments + j
            b = i * segments + j_next
            c = i_next * segments + j_next
            d = i_next * segments + j
            faces.append([a, b, c, d])

    if not closed:
        faces.append(list(range(segments - 1, -1, -1)))
        faces.append(list(range((n_pts - 1) * segments, n_pts * segments)))

    g.add(name, verts, faces, material, convex_center=None)


def harbor_arch_window(
    g: MapGeometry,
    name: str,
    origin,
    width: float = 0.38,
    height: float = 0.66,
    yaw: float = 0.0,
    door: bool = False,
):
    """High-detail arched doorway or glowing mullioned window with stone voussoirs.

    Uses diorama-grade member thicknesses so float32 volume integration far from
    the world origin (`x > 25m`) is always strictly positive (`vol6 >> 1e-4`).
    """
    r = width / 2.0
    spring = height - r
    c, s = math.cos(yaw), math.sin(yaw)
    rot_z_mat = np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]])

    def pt(x, y, z):
        return (origin[0] + c * x - s * y, origin[1] + s * x + c * y, origin[2] + z)

    prism(g, name + "_recess", arch_outline(r + 0.04, spring), 0.065, pt(0.0, 0.0, 0.0), "iron", yaw)
    prism(
        g,
        name + "_inset",
        arch_outline(r - 0.015, spring),
        0.048,
        pt(0.0, -0.028, 0.015),
        "door" if door else "glass",
        yaw,
    )
    count = max(2, int(spring / 0.16))
    for side in (-1, 1):
        for row in range(count):
            z = (row + 0.5) * spring / count
            g.box(
                name + "_jamb",
                pt(side * (r + 0.048), -0.008, z),
                (0.11, 0.11, spring / count - 0.01),
                "trim",
                0.01,
                rot_z_mat,
                top_bevel_only=True,
            )
    for i in range(11):
        lo = i * math.pi / 11.0 + 0.016
        hi = (i + 1) * math.pi / 11.0 - 0.016
        outline = [
            ((r + 0.105) * math.cos(lo), spring + (r + 0.105) * math.sin(lo)),
            ((r + 0.105) * math.cos(hi), spring + (r + 0.105) * math.sin(hi)),
            (r * math.cos(hi), spring + r * math.sin(hi)),
            (r * math.cos(lo), spring + r * math.sin(lo)),
        ]
        prism(g, name + "_voussoir", outline, 0.125, origin, "trim", yaw)
    g.beam(name + "_sill", pt(-r - 0.10, -0.028, -0.028), pt(r + 0.10, -0.028, -0.028), 0.10, "trim")

    if door:
        for i in range(5):
            x = -r + 0.055 + i * (2.0 * r - 0.11) / 4.0
            top = spring + math.sqrt(max(0.0, (r - 0.025) ** 2 - x * x)) - 0.02
            g.beam(name + "_plank_joint", pt(x, -0.048, 0.04), pt(x, -0.048, top), 0.036, "wood")
        for z in (0.22, 0.54):
            for side in (-1, 1):
                g.beam(
                    name + "_hinge",
                    pt(side * 0.035, -0.065, z),
                    pt(side * (r - 0.025), -0.065, z),
                    0.042,
                    "brass",
                )
        for side in (-1, 1):
            knocker_c = pt(side * 0.08, -0.078, 0.42)
            g.cylinder(
                name + "_door_ring",
                (knocker_c[0] - s * 0.02, knocker_c[1] + c * 0.02, knocker_c[2]),
                (knocker_c[0] + s * 0.035, knocker_c[1] - c * 0.035, knocker_c[2]),
                0.045,
                "brass",
                segments=10,
            )
    else:
        g.beam(name + "_mullion", pt(0.0, -0.048, 0.03), pt(0.0, -0.048, height - 0.03), 0.038, "wood")
        for z in (spring * 0.36, spring * 0.72):
            g.beam(
                name + "_transom",
                pt(-r + 0.02, -0.048, z),
                pt(r - 0.02, -0.048, z),
                0.038,
                "wood",
            )


def add_faceted_boulder(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z_bot: float,
    sx: float,
    sy: float,
    sz: float,
    material: str,
    yaw: float = 0.0,
    seed: float = 0.0,
    rings: int = 5,
    segs: int = 9,
):
    """Adds a closed, outward-wound chiseled rock boulder solid."""
    c, s = math.cos(yaw), math.sin(yaw)

    def to_world(lx, ly, lz):
        return (cx + c * lx - s * ly, cy + s * lx + c * ly, z_bot + lz)

    verts = []
    for r_i in range(1, rings):
        vf = r_i / float(rings)
        z_cur = vf * sz
        profile = 0.84 + 0.25 * math.sin(vf * math.pi) - 0.14 * (vf ** 2)
        for s_i in range(segs):
            th = s_i * math.tau / segs
            crag = (
                1.0
                + 0.13 * math.sin(th * 3.0 + vf * 4.0 + seed)
                + 0.07 * math.cos(th * 4.0 - seed * 1.7)
            )
            lx = 0.5 * sx * profile * crag * math.cos(th)
            ly = 0.5 * sy * profile * crag * math.sin(th)
            verts.append(to_world(lx, ly, z_cur))

    bot_idx = len(verts)
    verts.append(to_world(0.0, 0.0, 0.0))
    top_idx = len(verts)
    verts.append(to_world(0.02 * sx * math.cos(seed), 0.02 * sy * math.sin(seed), sz * 1.02))

    n_lat = rings - 1
    faces = []
    for s_i in range(segs):
        s_next = (s_i + 1) % segs
        faces.append([bot_idx, s_next, s_i])

    for r_i in range(n_lat - 1):
        for s_i in range(segs):
            s_next = (s_i + 1) % segs
            a = r_i * segs + s_i
            b = r_i * segs + s_next
            c_v = (r_i + 1) * segs + s_next
            d = (r_i + 1) * segs + s_i
            faces.append([a, b, c_v, d])

    top_ring = (n_lat - 1) * segs
    for s_i in range(segs):
        s_next = (s_i + 1) % segs
        faces.append([top_ring + s_i, top_ring + s_next, top_idx])

    g.add(name, verts, faces, material, convex_center=None)


def add_geodesic_crystal_orb(
    g: MapGeometry,
    name: str,
    center,
    radius: float,
    material: str = "orb_sapphire",
    subdivisions: int = 2,
    z_stretch: float = 1.10,
):
    """Builds a closed, multi-faceted geodesic/icosahedral energy crystal orb."""
    phi = (1.0 + math.sqrt(5.0)) * 0.5
    raw_verts = [
        (-1,  phi, 0), ( 1,  phi, 0), (-1, -phi, 0), ( 1, -phi, 0),
        ( 0, -1,  phi), ( 0,  1,  phi), ( 0, -1, -phi), ( 0,  1, -phi),
        ( phi, 0, -1), ( phi, 0,  1), (-phi, 0, -1), (-phi, 0,  1),
    ]
    verts = [unit(v) for v in raw_verts]
    tris = [
        (0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11),
        (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
        (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9),
        (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1),
    ]

    for _ in range(subdivisions):
        mid_cache = {}

        def get_mid(i1, i2):
            key = (min(i1, i2), max(i1, i2))
            if key not in mid_cache:
                mid_cache[key] = len(verts)
                verts.append(unit(verts[i1] + verts[i2]))
            return mid_cache[key]

        new_tris = []
        for a, b, c in tris:
            ab = get_mid(a, b)
            bc = get_mid(b, c)
            ca = get_mid(c, a)
            new_tris.extend([
                (a, ab, ca),
                (b, bc, ab),
                (c, ca, bc),
                (ab, bc, ca),
            ])
        tris = new_tris

    cx, cy, cz = center
    final_verts = []
    for vx, vy, vz in verts:
        th = math.atan2(vy, vx)
        el = math.asin(max(-1.0, min(1.0, vz)))
        facet_mod = 1.0 + 0.032 * math.cos(th * 6.0) * math.cos(el * 4.0)
        r_loc = radius * facet_mod
        final_verts.append((
            cx + r_loc * vx,
            cy + r_loc * vy,
            cz + r_loc * z_stretch * vz,
        ))

    g.add(name, final_verts, [list(t) for t in tris], material, convex_center=None)
    if material == "orb_sapphire":
        # Map facets across the deep royal-sapphire to electric-cyan gradient of Strip 6 (u in [0.02..0.11], v in [0.05..0.28])
        # avoiding the white-hot Gaussian center at (u=0.18, v=0.56).
        start_uv = len(g.uvs) - len(tris) * 3
        for t_i, (a, b, c_idx) in enumerate(tris):
            tc = (np.asarray(verts[a]) + np.asarray(verts[b]) + np.asarray(verts[c_idx])) / 3.0
            radial = float(np.clip(1.0 - abs(tc[2]) * 0.65, 0.0, 1.0))
            u_loc = 0.02 + 0.075 * (1.0 - radial)
            v_loc = 0.05 + 0.18 * (1.0 - radial)
            patch = np.array([[u_loc - 0.005, v_loc - 0.006], [u_loc + 0.005, v_loc - 0.006], [u_loc, v_loc + 0.006]], dtype=float)
            patch[:, 0] = (6.0 + 0.03 + 0.94 * patch[:, 0]) / 8.0
            patch[:, 1] = 0.006 + 0.238 * patch[:, 1]
            g.uvs[start_uv + t_i * 3 : start_uv + (t_i + 1) * 3] = patch.tolist()


def add_shrub_and_flowers(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    seed: int = 0,
    with_flowers: bool = True,
):
    """Adds a closed-manifold golden/green shrub cluster with optional blossoms."""
    g.cylinder(
        name + "_stem",
        (cx, cy, z0 - 0.08),
        (cx, cy, z0 + 0.22 * scale),
        0.068 * scale,
        "wood",
        end_radius=0.045 * scale,
        segments=8,
    )
    puff_specs = [
        (0.0, 0.0, 0.16, 0.44, 0.40, 0.30, "shrub_green" if seed % 2 == 0 else "oak_gold"),
        (0.18, -0.12, 0.12, 0.34, 0.32, 0.24, "oak_gold"),
        (-0.17, 0.14, 0.13, 0.35, 0.32, 0.25, "shrub_green"),
        (0.10, 0.16, 0.20, 0.30, 0.28, 0.22, "oak_amber"),
    ]
    for idx, (dx, dy, dz, sx, sy, sz, mat) in enumerate(puff_specs):
        ang = seed * 1.1 + idx * 0.8
        ca, sa = math.cos(ang), math.sin(ang)
        px = cx + (dx * ca - dy * sa) * scale
        py = cy + (dx * sa + dy * ca) * scale
        pz = z0 + dz * scale
        add_faceted_boulder(
            g,
            f"{name}_foliage_{idx}",
            px,
            py,
            pz - 0.04 * scale,
            sx * scale,
            sy * scale,
            sz * scale,
            mat,
            yaw=ang,
            seed=seed + idx * 2.3,
            rings=4,
            segs=8,
        )
    if with_flowers:
        flower_mats = ["flower_gold", "flower_magenta", "flower_violet"]
        f_mat = flower_mats[seed % len(flower_mats)]
        for f_i in range(4):
            f_ang = seed * 0.7 + f_i * (math.tau / 4.0)
            fx = cx + 0.18 * scale * math.cos(f_ang)
            fy = cy + 0.18 * scale * math.sin(f_ang)
            fz = z0 + (0.28 + 0.03 * (f_i % 2)) * scale
            g.box(
                f"{name}_blossom_{f_i}",
                (fx, fy, fz),
                (0.10 * scale, 0.10 * scale, 0.075 * scale),
                f_mat,
                bevel=0.015 * scale,
                top_bevel_only=True,
            )


def add_street_or_dock_lantern(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z_base: float,
    scale: float = 1.0,
):
    """Adds a wrought-iron & brass lantern with glowing warm glass core."""
    s = scale
    g.cylinder(
        name + "_pedestal",
        (cx, cy, z_base),
        (cx, cy, z_base + 0.13 * s),
        0.10 * s,
        "iron",
        end_radius=0.072 * s,
        segments=10,
    )
    g.cylinder(
        name + "_post",
        (cx, cy, z_base + 0.12 * s),
        (cx, cy, z_base + 0.45 * s),
        0.048 * s,
        "iron",
        end_radius=0.040 * s,
        segments=10,
    )
    for side in (-1, 1):
        g.beam(
            name + "_scroll",
            (cx, cy, z_base + 0.32 * s),
            (cx + side * 0.11 * s, cy, z_base + 0.45 * s),
            0.036 * s,
            "iron",
        )
    z_lan = z_base + 0.44 * s
    g.box(
        name + "_tray",
        (cx, cy, z_lan),
        (0.20 * s, 0.20 * s, 0.048 * s),
        "brass",
        bevel=0.008 * s,
        top_bevel_only=True,
    )
    g.box(
        name + "_glass",
        (cx, cy, z_lan + 0.13 * s),
        (0.15 * s, 0.15 * s, 0.22 * s),
        "glass",
        bevel=0.012 * s,
        top_bevel_only=True,
    )
    for xx in (-0.08 * s, 0.08 * s):
        for yy in (-0.08 * s, 0.08 * s):
            g.beam(
                name + "_frame",
                (cx + xx, cy + yy, z_lan + 0.01 * s),
                (cx + xx, cy + yy, z_lan + 0.25 * s),
                0.034 * s,
                "brass",
            )
    g.box(
        name + "_cap_lower",
        (cx, cy, z_lan + 0.255 * s),
        (0.22 * s, 0.22 * s, 0.048 * s),
        "brass",
        bevel=0.008 * s,
        top_bevel_only=True,
    )
    g.cylinder(
        name + "_cap_dome",
        (cx, cy, z_lan + 0.27 * s),
        (cx, cy, z_lan + 0.36 * s),
        0.11 * s,
        "brass",
        end_radius=0.028 * s,
        segments=8,
    )


# =============================================================================
# ZONE 07: Slender Information Beacon Lighthouse (`Trạm Thông Tin`)
# =============================================================================

def build_zone07_beacon() -> MapGeometry:
    """Builds `Zone07_Landmark_TramThongTin_InfoBeacon` at `(8.6, 4.6)`."""
    g = MapGeometry()
    bx, by = 8.6, 4.6
    bz = float(sample_ground_z(bx, by))

    # 1. Base rocky mound: 12 chunky beveled sandstone & mossy cliff boulders + 4 shrubs
    boulder_specs = [
        (0.00, 1.68, 1.15, 0.92, 0.68, "sandstone_warm"),
        (0.52, 1.56, 1.05, 0.88, 0.62, "stone"),
        (1.05, 1.72, 1.18, 0.94, 0.72, "sandstone_warm"),
        (1.58, 1.52, 1.02, 0.86, 0.64, "sandstone_ochre"),
        (2.10, 1.66, 1.12, 0.90, 0.68, "stone"),
        (2.62, 1.58, 1.08, 0.88, 0.62, "sandstone_warm"),
        (3.15, 1.74, 1.22, 0.96, 0.70, "sandstone_warm"),
        (3.68, 1.50, 0.98, 0.82, 0.56, "stone"),
        (4.42, 1.54, 1.02, 0.84, 0.58, "sandstone_ochre"),
        (4.95, 1.70, 1.16, 0.92, 0.68, "sandstone_warm"),
        (5.48, 1.58, 1.06, 0.86, 0.62, "stone"),
        (5.92, 1.44, 0.96, 0.80, 0.56, "sandstone_warm"),
    ]
    for idx, (ang, rad, sx, sy, sz, mat) in enumerate(boulder_specs):
        rx = bx + rad * math.cos(ang)
        ry = by + rad * math.sin(ang)
        rz = min(bz - 0.12, float(sample_ground_z(rx, ry)) - 0.14)
        add_faceted_boulder(
            g,
            f"beacon_mound_boulder_{idx}",
            rx,
            ry,
            rz,
            sx,
            sy,
            sz,
            mat,
            yaw=ang + 0.35,
            seed=idx * 1.9 + 0.4,
            rings=5,
            segs=9,
        )

    shrub_angles = [-0.95, -2.85, 0.35, 3.65]
    for s_idx, s_ang in enumerate(shrub_angles):
        sx_p = bx + 1.66 * math.cos(s_ang)
        sy_p = by + 1.66 * math.sin(s_ang)
        sz_p = bz + 0.08
        add_shrub_and_flowers(
            g,
            f"beacon_shrub_{s_idx}",
            sx_p,
            sy_p,
            sz_p,
            scale=1.02 + 0.08 * (s_idx % 2),
            seed=s_idx + 3,
            with_flowers=True,
        )

    # 2. Stepped Octagonal/Cylindrical Ashlar Stone Plinth (`radius 1.75m -> 1.28m`)
    door_angle = -2.32
    g.cylinder(
        "plinth_core_lower",
        (bx, by, bz - 0.35),
        (bx, by, bz + 0.24),
        1.68,
        "mortar",
        end_radius=1.58,
        segments=32,
    )
    g.cylinder(
        "plinth_core_upper",
        (bx, by, bz + 0.22),
        (bx, by, bz + 0.68),
        1.34,
        "mortar",
        end_radius=1.22,
        segments=32,
    )
    g.cylinder(
        "plinth_base_molding",
        (bx, by, bz - 0.34),
        (bx, by, bz - 0.18),
        1.78,
        "trim",
        end_radius=1.74,
        segments=32,
    )

    for row in range(2):
        z0 = bz - 0.18 + row * 0.19
        z1 = z0 + 0.178
        r0 = 1.75 - row * 0.05
        r1 = r0 - 0.04
        for i in range(16):
            lo = (i + 0.5 * (row % 2)) * math.tau / 16.0 + 0.008
            hi = lo + math.tau / 16.0 - 0.016
            mat = "limestone" if (i + row) % 3 != 0 else "stone"
            add_curved_wedge_block(
                g,
                "plinth_lower_ashlar",
                bx,
                by,
                z0,
                z1,
                r0 - 0.12,
                r0,
                r1 - 0.12,
                r1,
                lo,
                hi,
                mat,
            )

    g.cylinder(
        "plinth_mid_coping",
        (bx, by, bz + 0.19),
        (bx, by, bz + 0.26),
        1.66,
        "trim",
        end_radius=1.62,
        segments=32,
    )

    for row in range(2):
        z0 = bz + 0.26 + row * 0.19
        z1 = z0 + 0.178
        r0 = 1.40 - row * 0.06
        r1 = r0 - 0.05
        for i in range(16):
            lo = (i + 0.5 * (row % 2)) * math.tau / 16.0 + 0.009
            hi = lo + math.tau / 16.0 - 0.018
            mat = "limestone" if (i + row) % 4 != 0 else "trim"
            add_curved_wedge_block(
                g,
                "plinth_upper_ashlar",
                bx,
                by,
                z0,
                z1,
                r0 - 0.12,
                r0,
                r1 - 0.12,
                r1,
                lo,
                hi,
                mat,
            )

    g.cylinder(
        "plinth_top_cornice",
        (bx, by, bz + 0.63),
        (bx, by, bz + 0.70),
        1.33,
        "trim",
        end_radius=1.28,
        segments=32,
    )

    for k in range(8):
        ang_k = door_angle + (k + 0.5) * (math.tau / 8.0)
        ca, sa = math.cos(ang_k), math.sin(ang_k)
        rot_k = np.array([[ca, -sa, 0], [sa, ca, 0], [0, 0, 1]])
        g.box(
            "plinth_buttress_pier",
            (bx + 1.34 * ca, by + 1.34 * sa, bz + 0.44),
            (0.22, 0.26, 0.48),
            "trim",
            bevel=0.018,
            rotation=rot_k,
        )

    cd, sd = math.cos(door_angle), math.sin(door_angle)
    rot_door = np.array([[cd, -sd, 0], [sd, cd, 0], [0, 0, 1]])
    for step_i in range(5):
        r_step = 1.18 + step_i * 0.18
        z_step = bz + 0.56 - step_i * 0.13
        w_step = 0.84 + step_i * 0.06
        g.box(
            "beacon_entrance_step",
            (bx + r_step * cd, by + r_step * sd, z_step),
            (0.24, w_step, 0.15),
            "trim" if step_i % 2 == 0 else "limestone",
            bevel=0.016,
            rotation=rot_door,
        )
    for side in (-1, 1):
        lat = side * 0.50
        p0 = (
            bx + 1.16 * cd - lat * sd,
            by + 1.16 * sd + lat * cd,
            bz + 0.68,
        )
        p1 = (
            bx + 1.98 * cd - lat * sd,
            by + 1.98 * sd + lat * cd,
            bz + 0.12,
        )
        g.beam("beacon_stair_cheek", p0, p1, 0.14, "limestone", depth=0.22)
        g.box(
            "beacon_stair_newel",
            (bx + 1.98 * cd - lat * sd, by + 1.98 * sd + lat * cd, bz + 0.08),
            (0.20, 0.20, 0.36),
            "trim",
            bevel=0.018,
            rotation=rot_door,
        )

    # 3. Tapering Cylindrical/Octagonal Tower Shaft (`height 3.1m`, `radius 0.98m -> 0.74m`)
    z_shaft_base = bz + 0.68
    shaft_h = 3.10
    r_shaft_bot = 0.98
    r_shaft_top = 0.74

    g.cylinder(
        "beacon_shaft_core",
        (bx, by, z_shaft_base - 0.04),
        (bx, by, z_shaft_base + shaft_h),
        r_shaft_bot - 0.035,
        "mortar",
        end_radius=r_shaft_top - 0.035,
        segments=32,
    )

    n_rows = 16
    n_blocks = 16
    row_dz = (shaft_h - 0.12) / float(n_rows)
    for row in range(n_rows):
        t0 = row / float(n_rows)
        t1 = (row + 0.91) / float(n_rows)
        z0 = z_shaft_base + 0.04 + row * row_dz
        z1 = z_shaft_base + 0.04 + (row + 0.91) * row_dz
        r0 = r_shaft_bot + (r_shaft_top - r_shaft_bot) * t0
        r1 = r_shaft_bot + (r_shaft_top - r_shaft_bot) * t1
        for i in range(n_blocks):
            lo = (i + 0.5 * (row % 2)) * (math.tau / n_blocks) + 0.009
            hi = lo + (math.tau / n_blocks) - 0.018
            mat = "limestone" if (i + row * 3) % 5 != 0 else "trim"
            add_curved_wedge_block(
                g,
                "beacon_shaft_masonry",
                bx,
                by,
                z0,
                z1,
                r0 - 0.055,
                r0 + 0.018,
                r1 - 0.055,
                r1 + 0.018,
                lo,
                hi,
                mat,
            )

    for c_idx, (z_rel, extra_r, c_h) in enumerate([
        (0.02, 0.075, 0.085),
        (0.92, 0.055, 0.068),
        (1.88, 0.055, 0.068),
        (2.88, 0.065, 0.075),
    ]):
        t_c = z_rel / shaft_h
        r_c = r_shaft_bot + (r_shaft_top - r_shaft_bot) * t_c + extra_r
        z_c = z_shaft_base + z_rel
        g.cylinder(
            f"beacon_cornice_ring_{c_idx}",
            (bx, by, z_c),
            (bx, by, z_c + c_h),
            r_c,
            "trim",
            end_radius=r_c - 0.01,
            segments=32,
        )

    yaw_door_win = door_angle + math.pi * 0.5
    door_r = r_shaft_bot + 0.01
    harbor_arch_window(
        g,
        "beacon_entrance_door",
        (bx + door_r * cd, by + door_r * sd, z_shaft_base + 0.06),
        width=0.52,
        height=0.84,
        yaw=yaw_door_win,
        door=True,
    )
    for side in (-1, 1):
        ang_l = door_angle + side * 0.42
        rl = r_shaft_bot + 0.10
        lx = bx + rl * math.cos(ang_l)
        ly = by + rl * math.sin(ang_l)
        lz = z_shaft_base + 0.62
        g.beam(
            "beacon_door_lantern_bracket",
            (bx + (r_shaft_bot - 0.02) * math.cos(ang_l), by + (r_shaft_bot - 0.02) * math.sin(ang_l), lz + 0.16),
            (lx, ly, lz + 0.16),
            0.036,
            "iron",
        )
        g.box("beacon_door_lantern_glass", (lx, ly, lz + 0.04), (0.12, 0.12, 0.17), "glass", bevel=0.01)
        g.box("beacon_door_lantern_cap", (lx, ly, lz + 0.14), (0.15, 0.15, 0.04), "brass", bevel=0.006)
        g.box("beacon_door_lantern_base", (lx, ly, lz - 0.05), (0.14, 0.14, 0.036), "brass", bevel=0.005)

    for w_i in range(4):
        ang_mid = door_angle + (w_i + 0.5) * (math.tau / 4.0)
        z_mid = z_shaft_base + 1.04
        r_mid = r_shaft_bot + (r_shaft_top - r_shaft_bot) * (1.04 / shaft_h) + 0.015
        harbor_arch_window(
            g,
            f"beacon_mid_window_{w_i}",
            (bx + r_mid * math.cos(ang_mid), by + r_mid * math.sin(ang_mid), z_mid),
            width=0.28,
            height=0.56,
            yaw=ang_mid + math.pi * 0.5,
            door=False,
        )

        ang_top = door_angle + w_i * (math.tau / 4.0)
        z_top = z_shaft_base + 1.98
        r_top = r_shaft_bot + (r_shaft_top - r_shaft_bot) * (1.98 / shaft_h) + 0.015
        harbor_arch_window(
            g,
            f"beacon_upper_window_{w_i}",
            (bx + r_top * math.cos(ang_top), by + r_top * math.sin(ang_top), z_top),
            width=0.34,
            height=0.72,
            yaw=ang_top + math.pi * 0.5,
            door=False,
        )

    # 4. Corbeled Stone Lantern Gallery at the Top (`z = bz + 3.65`)
    z_gal = bz + 3.65
    for i in range(16):
        ang_c = i * (math.tau / 16.0)
        cc, sc = math.cos(ang_c), math.sin(ang_c)
        rot_c = np.array([[cc, -sc, 0], [sc, cc, 0], [0, 0, 1]])
        g.box(
            "beacon_gallery_corbel_lower",
            (bx + 0.80 * cc, by + 0.80 * sc, z_gal - 0.08),
            (0.18, 0.09, 0.14),
            "trim",
            bevel=0.012,
            rotation=rot_c,
        )
        g.box(
            "beacon_gallery_corbel_upper",
            (bx + 0.88 * cc, by + 0.88 * sc, z_gal + 0.02),
            (0.24, 0.10, 0.12),
            "trim",
            bevel=0.012,
            rotation=rot_c,
        )

    g.cylinder(
        "beacon_gallery_deck",
        (bx, by, z_gal + 0.06),
        (bx, by, z_gal + 0.24),
        0.92,
        "limestone",
        end_radius=1.02,
        segments=32,
    )
    g.cylinder(
        "beacon_gallery_rim",
        (bx, by, z_gal + 0.22),
        (bx, by, z_gal + 0.29),
        1.05,
        "trim",
        end_radius=1.04,
        segments=32,
    )

    for i in range(16):
        lo = i * (math.tau / 16.0) + 0.012
        hi = (i + 1) * (math.tau / 16.0) - 0.012
        add_curved_wedge_block(
            g,
            "beacon_parapet_base",
            bx,
            by,
            z_gal + 0.28,
            z_gal + 0.44,
            0.90,
            1.02,
            0.90,
            1.02,
            lo,
            hi,
            "limestone",
        )
        lo_m = i * (math.tau / 16.0) + 0.055
        hi_m = (i + 1) * (math.tau / 16.0) - 0.055
        add_curved_wedge_block(
            g,
            "beacon_parapet_merlon",
            bx,
            by,
            z_gal + 0.44,
            z_gal + 0.58,
            0.91,
            1.02,
            0.91,
            1.01,
            lo_m,
            hi_m,
            "trim",
        )

    g.cylinder(
        "beacon_orb_pedestal_stone",
        (bx, by, z_gal + 0.28),
        (bx, by, z_gal + 0.50),
        0.58,
        "trim",
        end_radius=0.44,
        segments=16,
    )
    g.cylinder(
        "beacon_orb_pedestal_brass",
        (bx, by, z_gal + 0.49),
        (bx, by, z_gal + 0.62),
        0.46,
        "brass",
        end_radius=0.34,
        segments=16,
    )

    for k in range(8):
        ang_r = k * (math.tau / 8.0)
        cr, sr = math.cos(ang_r), math.sin(ang_r)
        rot_r = np.array([[cr, -sr, 0], [sr, cr, 0], [0, 0, 1]])
        g.box(
            "beacon_cage_pier",
            (bx + 0.84 * cr, by + 0.84 * sr, z_gal + 0.44),
            (0.16, 0.14, 0.34),
            "trim",
            bevel=0.014,
            rotation=rot_r,
        )
        g.box(
            "beacon_cage_pier_cap",
            (bx + 0.84 * cr, by + 0.84 * sr, z_gal + 0.63),
            (0.18, 0.16, 0.06),
            "brass",
            bevel=0.008,
            rotation=rot_r,
        )
        rib_profile = [
            (0.84, z_gal + 0.64),
            (0.90, z_gal + 0.86),
            (0.92, z_gal + 1.12),
            (0.84, z_gal + 1.38),
            (0.64, z_gal + 1.60),
            (0.38, z_gal + 1.76),
            (0.18, z_gal + 1.84),
        ]
        rib_pts = [(bx + rr * cr, by + rr * sr, zz) for rr, zz in rib_profile]
        curved_tube(g, f"beacon_crown_rib_{k}", rib_pts, 0.038, "brass", segments=8, closed=False)

    for r_ring, z_ring, rad_tube in [
        (0.85, z_gal + 0.68, 0.028),
        (0.86, z_gal + 1.34, 0.026),
        (0.22, z_gal + 1.83, 0.028),
    ]:
        ring_pts = [
            (bx + r_ring * math.cos(a), by + r_ring * math.sin(a), z_ring)
            for a in np.linspace(0.0, math.tau, 24, endpoint=False)
        ]
        curved_tube(g, "beacon_crown_ring", ring_pts, rad_tube, "brass", segments=8, closed=True)

    g.cylinder(
        "beacon_crown_cupola",
        (bx, by, z_gal + 1.80),
        (bx, by, z_gal + 1.96),
        0.24,
        "brass",
        end_radius=0.06,
        segments=16,
    )
    g.cylinder(
        "beacon_crown_finial",
        (bx, by, z_gal + 1.94),
        (bx, by, z_gal + 2.28),
        0.055,
        "brass",
        end_radius=0.02,
        segments=12,
    )

    orb_center = (bx, by, z_gal + 1.12)
    add_geodesic_crystal_orb(
        g,
        "beacon_sapphire_energy_orb",
        orb_center,
        radius=0.52,
        material="orb_sapphire",
        subdivisions=2,
        z_stretch=1.08,
    )

    # Intersecting Astrological Brass Armillary Rings (`Vòng Tinh Bàn Thiên Nhãn`) orbiting the Sapphire Orb
    for r_idx, (tilt_x, tilt_y, r_arm) in enumerate([
        (0.55, 0.20, 0.68),
        (-0.25, 0.62, 0.63),
    ]):
        n_ax = unit((tilt_x, tilt_y, 0.78))
        basis_a = frame(n_ax)
        u_a, v_a = basis_a[:, 0], basis_a[:, 1]
        arm_pts = [
            tuple(np.asarray(orb_center, dtype=float) + r_arm * (math.cos(a) * u_a + math.sin(a) * v_a))
            for a in np.linspace(0.0, math.tau, 28, endpoint=False)
        ]
        curved_tube(g, f"beacon_armillary_ring_{r_idx}", arm_pts, 0.026, "brass", segments=8, closed=True)

    for s_i in range(8):
        ang_s = (s_i + 0.5) * (math.tau / 8.0)
        sx_c = bx + 0.56 * math.cos(ang_s)
        sy_c = by + 0.56 * math.sin(ang_s)
        sz_c = z_gal + 0.68 + 0.04 * (s_i % 2)
        g.cylinder(
            f"beacon_crystal_shard_{s_i}",
            (sx_c, sy_c, sz_c - 0.08),
            (sx_c + 0.04 * math.cos(ang_s), sy_c + 0.04 * math.sin(ang_s), sz_c + 0.14),
            0.058,
            "orb_sapphire",
            end_radius=0.02,
            segments=6,
        )
        remap_last_part_uv(g, 6, 0.035, 0.12)

    return g


# =============================================================================
# ZONE 08 Helpers: Medieval Harbor Houses, Clock Spire Towers, Fountain,
# Timber Docks, Ribbed Wooden Rowboats & Riverbank Autumn Trees
# =============================================================================

def tiled_roof_custom(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    width: float,
    depth: float,
    eave: float,
    rise: float,
    roof_mat: str = "terracotta",
    ridge_x: bool = True,
    occluders=(),
    tile_col_step: float = 0.27,
    tile_row_step: float = 0.19,
):
    """Closed roof underlay + individual overlapping beveled roof tiles (`'terracotta'` or `'slate_blue'`)."""
    def p(u, v, z):
        return np.array((cx + u, cy + v, z) if ridge_x else (cx + v, cy + u, z), dtype=float)

    length = width if ridge_x else depth
    run = (depth if ridge_x else width) / 2.0

    verts = [
        p(u, v, z)
        for u in (-length / 2.0, length / 2.0)
        for v, z in ((-run, eave), (0.0, eave + rise), (run, eave))
    ]
    g.add(
        name + "_underlay",
        verts,
        [[0, 1, 2], [5, 4, 3], [0, 3, 4, 1], [1, 4, 5, 2], [2, 5, 3, 0]],
        roof_mat,
        np.mean(verts, axis=0),
    )

    columns = max(4, math.ceil(length / tile_col_step))
    rows = max(5, math.ceil(math.hypot(run, rise) / tile_row_step))
    for side in (-1, 1):
        for row in range(rows):
            t = 1.0 - (row + 0.5) / float(rows)
            z = eave + rise * (1.0 - t) + 0.032 * t * t
            slope = unit(p(0.0, side * run, -rise + 0.065 * t) - p(0.0, 0.0, 0.0))
            along = unit(p(1.0, 0.0, 0.0) - p(0.0, 0.0, 0.0))
            normal = unit(np.cross(along, slope))
            if normal[2] < 0:
                normal = -normal
            rotation = np.column_stack((along, slope, normal))
            edges = np.linspace(-length / 2.0, length / 2.0, columns + 1)
            if row % 2:
                edges = np.unique(
                    np.r_[-length / 2.0, edges[:-1] + length / columns / 2.0, length / 2.0]
                )
            for left, right in zip(edges[:-1], edges[1:]):
                if right - left < 0.05:
                    continue
                center = p((left + right) / 2.0, side * run * t, z) + normal * 0.024
                if any(test(center) for test in occluders):
                    continue
                g.box(
                    name + "_tile",
                    center,
                    (right - left - 0.008, math.hypot(run, rise) / rows * 1.16, 0.042),
                    roof_mat,
                    0.006,
                    rotation,
                    top_bevel_only=True,
                )
        for u in (-length / 2.0, length / 2.0):
            g.beam(
                name + "_bargeboard",
                p(u, 0.0, eave + rise + 0.025),
                p(u, side * run, eave + 0.025),
                0.08,
                "wood",
            )
        g.beam(
            name + "_fascia",
            p(-length / 2.0, side * run, eave),
            p(length / 2.0, side * run, eave),
            0.085,
            "wood",
        )
    g.beam(
        name + "_ridge",
        p(-length / 2.0 - 0.025, 0.0, eave + rise + 0.035),
        p(length / 2.0 + 0.025, 0.0, eave + rise + 0.035),
        0.09,
        "wood",
    )

    for side in (-1, 1):
        u = side * (length / 2.0 + 0.035)
        points = [
            p(u, -run * 0.87, eave - 0.03),
            p(u, run * 0.87, eave - 0.03),
            p(u, 0.0, eave + rise - 0.07),
        ]
        axis = unit(p(1.0, 0.0, 0.0) - p(0.0, 0.0, 0.0)) * 0.03
        vv = [q + axis for q in points] + [q - axis for q in points]
        g.add(
            name + "_gable",
            vv,
            [[0, 1, 2], [5, 4, 3], [0, 3, 4, 1], [1, 4, 5, 2], [2, 5, 3, 0]],
            "plaster",
            np.mean(vv, axis=0),
        )
        for v in (-run * 0.82, run * 0.82):
            g.beam(
                name + "_gable_brace",
                p(u + side * 0.045, v, eave + 0.02),
                p(u + side * 0.045, 0.0, eave + rise * 0.70),
                0.07,
                "wood",
            )
        g.beam(
            name + "_gable_kingpost",
            p(u + side * 0.045, 0.0, eave),
            p(u + side * 0.045, 0.0, eave + rise - 0.04),
            0.07,
            "wood",
        )


def add_detailed_masonry_and_timber_block(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    width: float,
    depth: float,
    eave: float = 1.45,
):
    """Builds a stone-and-half-timbered building volume with courses, quoins & diagonal braces."""
    stone_top = eave * 0.56
    plaster_h = eave - stone_top + 0.04
    g.box(name + "_plinth", (cx, cy, -0.18), (width + 0.16, depth + 0.16, 0.38), "stone", 0.024, top_bevel_only=True)
    g.box(name + "_mortar", (cx, cy, eave * 0.46), (width, depth, eave + 0.10), "mortar", 0.015, top_bevel_only=True)
    g.box(
        name + "_plaster",
        (cx, cy, stone_top + plaster_h * 0.5 - 0.02),
        (width + 0.012, depth + 0.012, plaster_h),
        "plaster",
        0.014,
        top_bevel_only=True,
    )

    n_stone_rows = 5
    row_h = (stone_top + 0.04) / float(n_stone_rows)
    for row in range(n_stone_rows):
        z = -0.02 + (row + 0.5) * row_h
        for horizontal, length, offset in ((True, width, depth / 2.0), (False, depth, width / 2.0)):
            count = max(3, math.ceil(length / 0.38))
            edges = np.linspace(-length / 2.0, length / 2.0, count + 1)
            if row % 2:
                edges = np.unique(
                    np.r_[-length / 2.0, edges[:-1] + length / count / 2.0, length / 2.0]
                )
            for left, right in zip(edges[:-1], edges[1:]):
                if right - left < 0.06:
                    continue
                for side in (-1, 1):
                    center = (
                        (cx + (left + right) / 2.0, cy + side * offset, z)
                        if horizontal
                        else (cx + side * offset, cy + (left + right) / 2.0, z)
                    )
                    size = (
                        (right - left - 0.014, 0.072, row_h - 0.012)
                        if horizontal
                        else (0.072, right - left - 0.014, row_h - 0.012)
                    )
                    g.box(name + "_stone_course", center, size, "stone", 0.012, top_bevel_only=True)

    for x in (-width / 2.0, width / 2.0):
        for y in (-depth / 2.0, depth / 2.0):
            g.box(
                name + "_corner_timber",
                (cx + x, cy + y, stone_top + plaster_h * 0.5),
                (0.11, 0.11, plaster_h),
                "wood",
                top_bevel_only=True,
            )
            for row in range(4):
                g.box(
                    name + "_quoin",
                    (cx + x, cy + y, 0.04 + row * (stone_top / 4.0)),
                    (0.17, 0.17, stone_top / 4.0 - 0.015),
                    "trim",
                    top_bevel_only=True,
                )

    for z in (stone_top, eave - 0.035):
        g.box(name + "_belt", (cx, cy, z), (width + 0.12, depth + 0.12, 0.095), "wood", top_bevel_only=True)

    z_bot_t = stone_top + 0.04
    z_top_t = eave - 0.06
    for side in (-1, 1):
        y_f = cy + side * (depth / 2.0 + 0.025)
        for sx_stud in (-width * 0.22, width * 0.22):
            g.beam(name + "_stud_y", (cx + sx_stud, y_f, z_bot_t), (cx + sx_stud, y_f, z_top_t), 0.068, "wood")
        g.beam(
            name + "_diag_brace_y1",
            (cx - width * 0.46, y_f, z_bot_t),
            (cx - width * 0.23, y_f, z_top_t),
            0.062,
            "wood",
        )
        g.beam(
            name + "_diag_brace_y2",
            (cx + width * 0.46, y_f, z_bot_t),
            (cx + width * 0.23, y_f, z_top_t),
            0.062,
            "wood",
        )

        x_f = cx + side * (width / 2.0 + 0.025)
        g.beam(name + "_stud_x", (x_f, cy, z_bot_t), (x_f, cy, z_top_t), 0.068, "wood")
        g.beam(
            name + "_diag_brace_x1",
            (x_f, cy - depth * 0.44, z_bot_t),
            (x_f, cy - depth * 0.05, z_top_t),
            0.062,
            "wood",
        )
        g.beam(
            name + "_diag_brace_x2",
            (x_f, cy + depth * 0.44, z_bot_t),
            (x_f, cy + depth * 0.05, z_top_t),
            0.062,
            "wood",
        )


def build_harbor_house(
    width: float,
    depth: float,
    wall_h: float,
    roof_h: float,
    roof_mat: str = "terracotta",
    variant: int = 0,
) -> MapGeometry:
    """Builds a full `observatory_geometry.py`-grade medieval harbor house."""
    g = MapGeometry()
    add_detailed_masonry_and_timber_block(g, f"house_{variant}_main", 0.0, 0.0, width, depth, eave=wall_h)

    wing_x = width * (0.20 if variant % 2 == 0 else -0.20)
    wing_y = -depth * 0.28
    wing_w = width * 0.48
    wing_d = depth * 0.76
    add_detailed_masonry_and_timber_block(
        g,
        f"house_{variant}_wing",
        wing_x,
        wing_y,
        wing_w,
        wing_d,
        eave=wall_h * 0.96,
    )

    def under_wing(pt):
        return (
            abs(pt[0] - wing_x) < (wing_w * 0.56)
            and pt[1] < (wing_y + wing_d * 0.52)
            and pt[2] < (wall_h * 0.96 + roof_h * 0.85 * (1.0 - abs(pt[0] - wing_x) / (wing_w * 0.60)))
        )

    tiled_roof_custom(
        g,
        f"house_{variant}_main_roof",
        0.0,
        0.0,
        width + 0.24,
        depth + 0.24,
        wall_h,
        roof_h,
        roof_mat=roof_mat,
        ridge_x=True,
        occluders=(under_wing,),
    )
    tiled_roof_custom(
        g,
        f"house_{variant}_wing_roof",
        wing_x,
        wing_y,
        wing_w + 0.22,
        wing_d + 0.22,
        wall_h * 0.96,
        roof_h * 0.82,
        roof_mat=roof_mat,
        ridge_x=False,
    )

    dorm_x = -wing_x * 0.95
    dorm_y = -depth * 0.26
    dorm_z = wall_h + roof_h * 0.28
    g.box(f"house_{variant}_dormer_body", (dorm_x, dorm_y, dorm_z), (0.56, 0.54, 0.44), "plaster", 0.012, top_bevel_only=True)
    tiled_roof_custom(
        g,
        f"house_{variant}_dormer_roof",
        dorm_x,
        dorm_y,
        0.68,
        0.64,
        dorm_z + 0.20,
        0.32,
        roof_mat=roof_mat,
        ridge_x=False,
        tile_col_step=0.25,
        tile_row_step=0.18,
    )
    harbor_arch_window(
        g,
        f"house_{variant}_dormer_win",
        (dorm_x, dorm_y - 0.29, dorm_z - 0.16),
        width=0.24,
        height=0.36,
        yaw=0.0,
        door=False,
    )

    door_x = -wing_x * 0.92
    door_y = -depth * 0.50 - 0.02
    harbor_arch_window(
        g,
        f"house_{variant}_door",
        (door_x, door_y, 0.04),
        width=0.46,
        height=0.78,
        yaw=0.0,
        door=True,
    )
    for step_i in range(2):
        g.box(
            f"house_{variant}_step_{step_i}",
            (door_x, door_y - 0.12 - step_i * 0.12, -0.06 - step_i * 0.08),
            (0.66 + step_i * 0.08, 0.26, 0.14),
            "trim",
            0.015,
            top_bevel_only=True,
        )

    lan_x = door_x + (0.36 if wing_x < 0 else -0.36)
    lan_y = door_y - 0.12
    g.beam(
        f"house_{variant}_lantern_arm",
        (lan_x, door_y + 0.02, 0.88),
        (lan_x, lan_y, 0.88),
        0.042,
        "iron",
    )
    g.box(f"house_{variant}_lantern_glass", (lan_x, lan_y, 0.74), (0.12, 0.12, 0.18), "glass", 0.01, top_bevel_only=True)
    g.box(f"house_{variant}_lantern_cap", (lan_x, lan_y, 0.84), (0.15, 0.15, 0.042), "brass", 0.005, top_bevel_only=True)
    g.box(f"house_{variant}_lantern_base", (lan_x, lan_y, 0.64), (0.14, 0.14, 0.038), "brass", 0.005, top_bevel_only=True)

    wing_front_y = wing_y - wing_d * 0.50 - 0.02
    harbor_arch_window(
        g,
        f"house_{variant}_wing_win_lower",
        (wing_x, wing_front_y, 0.22),
        width=0.34,
        height=0.56,
        yaw=0.0,
        door=False,
    )
    harbor_arch_window(
        g,
        f"house_{variant}_wing_win_upper",
        (wing_x, wing_front_y, wall_h * 0.68),
        width=0.30,
        height=0.48,
        yaw=0.0,
        door=False,
    )
    for rx_w in (-width * 0.28, width * 0.28):
        harbor_arch_window(
            g,
            f"house_{variant}_rear_win",
            (rx_w, depth * 0.50 + 0.02, 0.24),
            width=0.32,
            height=0.56,
            yaw=math.pi,
            door=False,
        )
    for side_s, yaw_s in ((-1, -math.pi * 0.5), (1, math.pi * 0.5)):
        sx_w = side_s * (width * 0.50 + 0.02)
        harbor_arch_window(
            g,
            f"house_{variant}_side_win_low",
            (sx_w, 0.0, 0.24),
            width=0.32,
            height=0.56,
            yaw=yaw_s,
            door=False,
        )
        harbor_arch_window(
            g,
            f"house_{variant}_side_win_gable",
            (sx_w, 0.0, wall_h + 0.12),
            width=0.28,
            height=0.46,
            yaw=yaw_s,
            door=False,
        )

    chim_x = -wing_x * 0.75
    chim_y = depth * 0.20
    chim_z0 = wall_h + roof_h * 0.45
    for c_row in range(5):
        g.box(
            f"house_{variant}_chimney_course",
            (chim_x, chim_y, chim_z0 + c_row * 0.14),
            (0.30, 0.30, 0.132),
            "stone",
            0.012,
            top_bevel_only=True,
        )
    g.box(
        f"house_{variant}_chimney_cap",
        (chim_x, chim_y, chim_z0 + 0.72),
        (0.38, 0.38, 0.075),
        "trim",
        0.012,
        top_bevel_only=True,
    )
    g.cylinder(
        f"house_{variant}_chimney_pot",
        (chim_x, chim_y, chim_z0 + 0.75),
        (chim_x, chim_y, chim_z0 + 0.94),
        0.09,
        "iron",
        end_radius=0.075,
        segments=10,
    )

    # -------------------------------------------------------------------------
    # AUTHENTIC ANCIENT/MEDIEVAL EUROPEAN (`ÂU CỔ`) ARCHITECTURAL UPGRADES:
    # 1) Ground-Floor Romanesque Stone Colonnade Arcade (`Hành Lang Vòm Đá Cổ Điển`)
    # 2) Jettied Overhanging Upper Floor on Carved Corbels (`Tầng Lầu Đua Ra Ngoài & Dầm Console`)
    # 3) Stepped Hanseatic/Gothic Crow-Step Gable (`Đỉnh Tường Bậc Thang Âu Cổ`) + Pinnacle Finial
    # 4) Circular Gothic Rose Window (`Cửa Sổ Hoa Hồng Gothic`) in the Front Gable
    # -------------------------------------------------------------------------
    arcade_y = wing_y - wing_d * 0.50 - 0.22
    # 1. Ground-floor Romanesque stone colonnade loggia in front of the wing
    for col_idx, cx_col in enumerate((wing_x - wing_w * 0.44, wing_x, wing_x + wing_w * 0.44)):
        g.box(
            f"house_{variant}_arcade_base_{col_idx}",
            (cx_col, arcade_y, 0.04),
            (0.20, 0.20, 0.22),
            "stone",
            0.014,
            top_bevel_only=True,
        )
        g.cylinder(
            f"house_{variant}_arcade_col_{col_idx}",
            (cx_col, arcade_y, 0.14),
            (cx_col, arcade_y, wall_h * 0.50),
            0.078,
            "limestone",
            end_radius=0.072,
            segments=10,
        )
        g.box(
            f"house_{variant}_arcade_cap_{col_idx}",
            (cx_col, arcade_y, wall_h * 0.52),
            (0.20, 0.20, 0.10),
            "trim",
            0.012,
            top_bevel_only=True,
        )
    # Stone arcade lintel & twin Romanesque archivolts
    g.box(
        f"house_{variant}_arcade_entablature",
        (wing_x, arcade_y + 0.08, wall_h * 0.58),
        (wing_w + 0.16, 0.28, 0.12),
        "trim",
        0.014,
        top_bevel_only=True,
    )
    for b_idx, arch_cx in enumerate((wing_x - wing_w * 0.22, wing_x + wing_w * 0.22)):
        for side_a in (-1, 1):
            g.beam(
                f"house_{variant}_arcade_arch_{b_idx}_{side_a}",
                (arch_cx + side_a * (wing_w * 0.20), arcade_y, wall_h * 0.44),
                (arch_cx, arcade_y, wall_h * 0.56),
                0.085,
                "stone",
            )

    # 2. Jettied Overhanging Upper Floor (`Tầng Lầu Đua Ra Ngoài`) supported by Carved Corbel Brackets
    jetty_h = wall_h * 0.44
    jetty_z = wall_h * 0.58 + jetty_h * 0.5
    g.box(
        f"house_{variant}_jetty_bay",
        (wing_x, arcade_y + 0.08, jetty_z),
        (wing_w + 0.10, 0.26, jetty_h),
        "plaster",
        0.012,
        top_bevel_only=True,
    )
    for cb_idx, cx_cb in enumerate(np.linspace(wing_x - wing_w * 0.44, wing_x + wing_w * 0.44, 4)):
        g.beam(
            f"house_{variant}_corbel_{cb_idx}",
            (float(cx_cb), wing_front_y, wall_h * 0.40),
            (float(cx_cb), arcade_y - 0.02, wall_h * 0.58),
            0.075,
            "wood" if variant % 2 == 0 else "trim",
        )

    # 3. Stepped Hanseatic/Gothic Crow-Step Gable (`Đỉnh Tường Bậc Thang Âu Cổ`) on the Front Wing Gable
    gable_y = arcade_y - 0.02
    gable_base_z = wall_h * 0.96
    gable_apex_z = gable_base_z + roof_h * 0.88
    n_crow_steps = 5
    for st_i in range(n_crow_steps):
        frac = st_i / float(n_crow_steps)
        step_w = (wing_w + 0.16) * (1.0 - frac * 0.78)
        step_z0 = gable_base_z + frac * (gable_apex_z - gable_base_z)
        step_h = (gable_apex_z - gable_base_z) / float(n_crow_steps) + 0.04
        g.box(
            f"house_{variant}_crowstep_wall_{st_i}",
            (wing_x, gable_y, step_z0 + step_h * 0.45),
            (step_w, 0.14, step_h),
            "stone" if variant % 2 == 1 else "terracotta",
            0.012,
            top_bevel_only=True,
        )
        g.box(
            f"house_{variant}_crowstep_cap_{st_i}",
            (wing_x, gable_y, step_z0 + step_h * 0.92),
            (step_w + 0.08, 0.18, 0.065),
            "trim",
            0.010,
            top_bevel_only=True,
        )
    # Gothic Pinnacle Finial on the Crow-Step Gable Apex
    g.cylinder(
        f"house_{variant}_gable_pinnacle",
        (wing_x, gable_y, gable_apex_z - 0.02),
        (wing_x, gable_y, gable_apex_z + 0.34),
        0.075,
        "trim",
        end_radius=0.018,
        segments=8,
    )

    # 4. Circular Gothic Rose Window (`Cửa Sổ Hoa Hồng Gothic`) in the Upper Gable
    rose_z = gable_base_z + roof_h * 0.36
    rose_r = 0.22
    g.cylinder(
        f"house_{variant}_rose_frame",
        (wing_x, gable_y - 0.05, rose_z),
        (wing_x, gable_y + 0.04, rose_z),
        rose_r + 0.04,
        "trim",
        segments=16,
    )
    g.cylinder(
        f"house_{variant}_rose_glass",
        (wing_x, gable_y - 0.065, rose_z),
        (wing_x, gable_y + 0.02, rose_z),
        rose_r,
        "glass",
        segments=16,
    )
    for sp_k in range(4):
        ang_sp = sp_k * (math.pi / 4.0)
        dx_sp = rose_r * math.cos(ang_sp)
        dz_sp = rose_r * math.sin(ang_sp)
        g.beam(
            f"house_{variant}_rose_spoke_{sp_k}",
            (wing_x - dx_sp, gable_y - 0.068, rose_z - dz_sp),
            (wing_x + dx_sp, gable_y - 0.068, rose_z + dz_sp),
            0.032,
            "brass",
        )

    return g


def build_clock_spire_tower(
    width: float,
    th_base: float,
    th_spire: float,
    variant: int = 0,
) -> MapGeometry:
    """Builds a square medieval stone clock/bell tower with 8-sided slate-blue spire."""
    g = MapGeometry()
    hw = width * 0.5

    g.box(f"tower_{variant}_plinth", (0.0, 0.0, -0.18), (width + 0.22, width + 0.22, 0.42), "stone", 0.025, top_bevel_only=True)
    g.box(f"tower_{variant}_base_mold", (0.0, 0.0, 0.06), (width + 0.14, width + 0.14, 0.10), "trim", 0.015, top_bevel_only=True)
    g.box(
        f"tower_{variant}_core",
        (0.0, 0.0, th_base * 0.48),
        (width, width, th_base),
        "mortar",
        0.016,
        top_bevel_only=True,
    )
    g.box(
        f"tower_{variant}_upper_plaster",
        (0.0, 0.0, th_base * 0.68),
        (width + 0.01, width + 0.01, th_base * 0.58),
        "plaster",
        0.014,
        top_bevel_only=True,
    )

    stone_h = th_base * 0.40
    n_rows = 7
    rh = stone_h / float(n_rows)
    for row in range(n_rows):
        z = 0.10 + (row + 0.5) * rh
        for horiz in (True, False):
            edges = np.linspace(-hw, hw, 5)
            if row % 2:
                edges = np.unique(np.r_[-hw, edges[:-1] + width / 8.0, hw])
            for left, right in zip(edges[:-1], edges[1:]):
                if right - left < 0.05:
                    continue
                for side in (-1, 1):
                    c_pt = (
                        ((left + right) * 0.5, side * hw, z)
                        if horiz
                        else (side * hw, (left + right) * 0.5, z)
                    )
                    sz_pt = (
                        (right - left - 0.014, 0.072, rh - 0.012)
                        if horiz
                        else (0.072, right - left - 0.014, rh - 0.012)
                    )
                    g.box(f"tower_{variant}_stone_course", c_pt, sz_pt, "stone", 0.012, top_bevel_only=True)

    n_quoin = int(th_base / 0.24)
    for sx_q in (-hw, hw):
        for sy_q in (-hw, hw):
            for q_i in range(n_quoin):
                qz = 0.12 + (q_i + 0.5) * (th_base - 0.16) / float(n_quoin)
                q_w = 0.20 if q_i % 2 == 0 else 0.15
                g.box(
                    f"tower_{variant}_quoin",
                    (sx_q, sy_q, qz),
                    (q_w, q_w, (th_base - 0.16) / float(n_quoin) - 0.014),
                    "trim",
                    0.012,
                    top_bevel_only=True,
                )

    for z_belt, extra in (
        (th_base * 0.40, 0.12),
        (th_base * 0.65, 0.12),
        (th_base - 0.04, 0.18),
    ):
        g.box(
            f"tower_{variant}_belt",
            (0.0, 0.0, z_belt),
            (width + extra, width + extra, 0.095),
            "trim",
            0.015,
            top_bevel_only=True,
        )

    harbor_arch_window(
        g,
        f"tower_{variant}_door",
        (0.0, -hw - 0.02, 0.10),
        width=0.46,
        height=0.80,
        yaw=0.0,
        door=True,
    )

    # Arched belfry windows at mid-stage & circular brass-rimmed clock faces high on the upper stage
    z_belfry = th_base * 0.43
    z_clock = th_base * 0.81
    r_clock = 0.34
    for f_idx, (fx, fy, yaw_f) in enumerate([
        (0.0, -hw - 0.025, 0.0),
        (hw + 0.025, 0.0, math.pi * 0.5),
        (0.0, hw + 0.025, math.pi),
        (-hw - 0.025, 0.0, -math.pi * 0.5),
    ]):
        harbor_arch_window(
            g,
            f"tower_{variant}_belfry_{f_idx}",
            (fx, fy, z_belfry),
            width=0.38,
            height=0.66,
            yaw=yaw_f,
            door=False,
        )
        nx = math.sin(yaw_f)
        ny = -math.cos(yaw_f)
        tx_v = math.cos(yaw_f)
        ty_v = math.sin(yaw_f)
        dial_center = np.array([fx, fy, z_clock], dtype=float)
        n_vec = np.array([nx, ny, 0.0], dtype=float)
        t_vec = np.array([tx_v, ty_v, 0.0], dtype=float)
        up_vec = np.array([0.0, 0.0, 1.0], dtype=float)

        g.cylinder(
            f"tower_{variant}_clock_frame_{f_idx}",
            dial_center - n_vec * 0.02,
            dial_center + n_vec * 0.035,
            r_clock + 0.05,
            "brass",
            segments=16,
        )
        g.cylinder(
            f"tower_{variant}_clock_dial_{f_idx}",
            dial_center - n_vec * 0.005,
            dial_center + n_vec * 0.045,
            r_clock,
            "plaster",
            segments=16,
        )
        rim_pts = [
            dial_center + n_vec * 0.048 + r_clock * (math.cos(a) * t_vec + math.sin(a) * up_vec)
            for a in np.linspace(0.0, math.tau, 16, endpoint=False)
        ]
        curved_tube(g, f"tower_{variant}_clock_rim_{f_idx}", rim_pts, 0.036, "brass", segments=8, closed=True)

        for h_i in range(4):
            ha = h_i * (math.tau / 4.0)
            p_in = dial_center + n_vec * 0.045 + (r_clock * 0.58) * (math.cos(ha) * t_vec + math.sin(ha) * up_vec)
            p_out = dial_center + n_vec * 0.045 + (r_clock * 0.88) * (math.cos(ha) * t_vec + math.sin(ha) * up_vec)
            g.beam(f"tower_{variant}_clock_tick_{f_idx}_{h_i}", p_in, p_out, 0.048, "brass")

        g.cylinder(
            f"tower_{variant}_clock_boss_{f_idx}",
            dial_center + n_vec * 0.03,
            dial_center + n_vec * 0.088,
            0.056,
            "brass",
            segments=10,
        )
        hr_ang = math.radians(105.0)
        mn_ang = math.radians(30.0)
        g.beam(
            f"tower_{variant}_clock_hour_{f_idx}",
            dial_center + n_vec * 0.058,
            dial_center + n_vec * 0.058 + (r_clock * 0.56) * (math.cos(hr_ang) * t_vec + math.sin(hr_ang) * up_vec),
            0.048,
            "iron",
        )
        g.beam(
            f"tower_{variant}_clock_min_{f_idx}",
            dial_center + n_vec * 0.064,
            dial_center + n_vec * 0.064 + (r_clock * 0.78) * (math.cos(mn_ang) * t_vec + math.sin(mn_ang) * up_vec),
            0.044,
            "iron",
        )

    for sx_p in (-hw + 0.06, hw - 0.06):
        for sy_p in (-hw + 0.06, hw - 0.06):
            g.box(
                f"tower_{variant}_pinnacle_base",
                (sx_p, sy_p, th_base + 0.14),
                (0.22, 0.22, 0.28),
                "trim",
                0.014,
                top_bevel_only=True,
            )
            g.cylinder(
                f"tower_{variant}_pinnacle_cap",
                (sx_p, sy_p, th_base + 0.27),
                (sx_p, sy_p, th_base + 0.56),
                0.12,
                "trim",
                end_radius=0.025,
                segments=8,
            )

    z_spire0 = th_base - 0.02
    r_spire0 = width * 0.56
    g.cylinder(
        f"tower_{variant}_spire_core",
        (0.0, 0.0, z_spire0),
        (0.0, 0.0, z_spire0 + th_spire),
        r_spire0 * 0.94,
        "slate_blue",
        end_radius=0.04,
        segments=8,
    )

    n_spire_tiers = 8
    for t_i in range(n_spire_tiers):
        f0 = t_i / float(n_spire_tiers)
        f1 = (t_i + 1.04) / float(n_spire_tiers)
        zt0 = z_spire0 + f0 * (th_spire * 0.90)
        zt1 = min(z_spire0 + th_spire * 0.94, z_spire0 + f1 * (th_spire * 0.90))
        rt0 = max(0.08, r_spire0 * (1.0 - f0 * 0.88))
        rt1 = max(0.06, r_spire0 * (1.0 - f1 * 0.88))
        for s_oct in range(8):
            a0 = s_oct * (math.tau / 8.0) + 0.012
            a1 = (s_oct + 1) * (math.tau / 8.0) - 0.012
            add_curved_wedge_block(
                g,
                f"tower_{variant}_spire_shingle",
                0.0,
                0.0,
                zt0,
                zt1,
                max(0.02, rt0 - 0.05),
                rt0 + 0.025,
                max(0.018, rt1 - 0.045),
                rt1 + 0.015,
                a0,
                a1,
                "slate_blue",
            )

    for s_oct in range(8):
        ang_oct = s_oct * (math.tau / 8.0)
        p_bot = (r_spire0 * math.cos(ang_oct), r_spire0 * math.sin(ang_oct), z_spire0)
        p_top = (0.04 * math.cos(ang_oct), 0.04 * math.sin(ang_oct), z_spire0 + th_spire)
        g.beam(f"tower_{variant}_spire_rib_{s_oct}", p_bot, p_top, 0.052, "wood")

    for d_i in range(4):
        ang_d = d_i * (math.tau / 4.0)
        cd, sd = math.cos(ang_d), math.sin(ang_d)
        rot_d = np.array([[cd, -sd, 0], [sd, cd, 0], [0, 0, 1]])
        rd = r_spire0 * 0.72
        zd = z_spire0 + 0.36
        g.box(
            f"tower_{variant}_spire_dormer",
            (rd * cd, rd * sd, zd),
            (0.28, 0.28, 0.36),
            "plaster",
            0.012,
            rotation=rot_d,
            top_bevel_only=True,
        )
        g.box(
            f"tower_{variant}_spire_dormer_win",
            ((rd + 0.13) * cd, (rd + 0.13) * sd, zd - 0.02),
            (0.08, 0.16, 0.22),
            "glass",
            0.008,
            rotation=rot_d,
            top_bevel_only=True,
        )
        g.cylinder(
            f"tower_{variant}_spire_dormer_cap",
            (rd * cd, rd * sd, zd + 0.17),
            (rd * cd, rd * sd, zd + 0.42),
            0.20,
            "slate_blue",
            end_radius=0.025,
            segments=6,
        )

    z_apex = z_spire0 + th_spire
    g.cylinder(
        f"tower_{variant}_finial_shaft",
        (0.0, 0.0, z_apex - 0.05),
        (0.0, 0.0, z_apex + 0.52),
        0.042,
        "brass",
        end_radius=0.022,
        segments=10,
    )
    g.cylinder(
        f"tower_{variant}_finial_orb",
        (0.0, 0.0, z_apex + 0.12),
        (0.0, 0.0, z_apex + 0.28),
        0.095,
        "brass",
        end_radius=0.075,
        segments=12,
    )
    g.beam(
        f"tower_{variant}_weathervane",
        (-0.22, 0.0, z_apex + 0.42),
        (0.26, 0.0, z_apex + 0.42),
        0.045,
        "brass",
    )
    return g


def build_plaza_fountain(cx: float, cy: float, fz: float) -> MapGeometry:
    """Builds the tiered octagonal carved stone harbor plaza fountain at `(23.2, 0.4)`."""
    g = MapGeometry()

    for i in range(16):
        lo = i * (math.tau / 16.0) + 0.012
        hi = (i + 1) * (math.tau / 16.0) - 0.012
        add_curved_wedge_block(
            g,
            "fountain_plaza_paving",
            cx,
            cy,
            fz - 0.22,
            fz + 0.04,
            0.94,
            1.48,
            0.94,
            1.46,
            lo,
            hi,
            "stone" if i % 2 == 0 else "limestone",
        )

    g.cylinder(
        "fountain_basin_floor",
        (cx, cy, fz - 0.24),
        (cx, cy, fz + 0.12),
        0.92,
        "stone",
        segments=16,
    )
    for s_oct in range(8):
        lo = s_oct * (math.tau / 8.0) + 0.018
        hi = (s_oct + 1) * (math.tau / 8.0) - 0.018
        add_curved_wedge_block(
            g,
            "fountain_basin_wall",
            cx,
            cy,
            fz + 0.02,
            fz + 0.38,
            0.74,
            0.94,
            0.75,
            0.93,
            lo,
            hi,
            "limestone",
        )
        add_curved_wedge_block(
            g,
            "fountain_basin_coping",
            cx,
            cy,
            fz + 0.37,
            fz + 0.45,
            0.71,
            0.98,
            0.72,
            0.97,
            lo - 0.008,
            hi + 0.008,
            "trim",
        )
        ang_p = s_oct * (math.tau / 8.0)
        cp, sp = math.cos(ang_p), math.sin(ang_p)
        rot_p = np.array([[cp, -sp, 0], [sp, cp, 0], [0, 0, 1]])
        g.box(
            "fountain_corner_pier",
            (cx + 0.86 * cp, cy + 0.86 * sp, fz + 0.22),
            (0.22, 0.20, 0.46),
            "trim",
            0.016,
            rotation=rot_p,
            top_bevel_only=True,
        )

    g.cylinder(
        "fountain_water_lower",
        (cx, cy, fz + 0.12),
        (cx, cy, fz + 0.34),
        0.74,
        "water_pond",
        segments=16,
    )
    foam_pts = [
        (cx + 0.46 * math.cos(a), cy + 0.46 * math.sin(a), fz + 0.35)
        for a in np.linspace(0.0, math.tau, 20, endpoint=False)
    ]
    curved_tube(g, "fountain_water_ripple", foam_pts, 0.032, "water_foam", segments=6, closed=True)

    g.cylinder(
        "fountain_pedestal_lower",
        (cx, cy, fz + 0.12),
        (cx, cy, fz + 0.64),
        0.28,
        "limestone",
        end_radius=0.20,
        segments=8,
    )
    g.cylinder(
        "fountain_bowl_mid",
        (cx, cy, fz + 0.60),
        (cx, cy, fz + 0.74),
        0.24,
        "trim",
        end_radius=0.50,
        segments=12,
    )
    g.cylinder(
        "fountain_water_upper",
        (cx, cy, fz + 0.68),
        (cx, cy, fz + 0.76),
        0.44,
        "water_pond",
        segments=12,
    )

    g.cylinder(
        "fountain_pedestal_upper",
        (cx, cy, fz + 0.72),
        (cx, cy, fz + 1.06),
        0.15,
        "limestone",
        end_radius=0.09,
        segments=8,
    )
    g.cylinder(
        "fountain_finial_nozzle",
        (cx, cy, fz + 1.04),
        (cx, cy, fz + 1.20),
        0.11,
        "brass",
        end_radius=0.035,
        segments=8,
    )
    for k in range(4):
        ang_j = k * (math.tau / 4.0) + 0.4
        cj, sj = math.cos(ang_j), math.sin(ang_j)
        g.cylinder(
            f"fountain_brass_spout_{k}",
            (cx + 0.12 * cj, cy + 0.12 * sj, fz + 0.90),
            (cx + 0.25 * cj, cy + 0.25 * sj, fz + 0.88),
            0.036,
            "brass",
            end_radius=0.026,
            segments=8,
        )
        jet_pts = [
            (cx + (0.22 + 0.24 * t) * cj, cy + (0.22 + 0.24 * t) * sj, fz + 0.88 - 0.52 * (t ** 1.6))
            for t in np.linspace(0.0, 1.0, 6)
        ]
        curved_tube(g, f"fountain_water_jet_{k}", jet_pts, 0.026, "water_foam", segments=6, closed=False)

    return g


def build_wooden_rowboat(
    length: float = 1.68,
    beam: float = 0.68,
    depth: float = 0.34,
    seed: int = 0,
) -> MapGeometry:
    """Builds a detailed clinker-planked wooden rowboat with ribs, thwarts, gunwales & 2 oars."""
    g = MapGeometry()
    hl = length * 0.5
    hb = beam * 0.5

    n_stat = 8
    us = np.linspace(-1.0, 1.0, n_stat)

    def station_geom(u):
        x = u * hl
        shape = 0.18 + 0.82 * (math.sin(max(0.0, min(1.0, (u + 1.0) * 0.5)) * math.pi) ** 0.78)
        w_bot = hb * 0.52 * shape
        w_mid = hb * 0.80 * shape
        w_top = hb * 0.96 * shape
        z_keel = 0.05 * (u * u)
        z_mid = z_keel + depth * 0.46
        z_top = z_keel + depth * (0.92 + 0.16 * (u * u))
        return x, w_bot, w_mid, w_top, z_keel, z_mid, z_top

    stations = [station_geom(float(u)) for u in us]

    for i in range(n_stat - 1):
        x0, _, _, _, zk0, _, _ = stations[i]
        x1, _, _, _, zk1, _, _ = stations[i + 1]
        g.beam(f"boat_keel_{i}", (x0, 0.0, zk0 - 0.02), (x1, 0.0, zk1 - 0.02), 0.072, "wood", depth=0.075)

    x_stern, _, _, _, zk_s, _, zt_s = stations[0]
    x_bow, _, _, _, zk_b, _, zt_b = stations[-1]
    g.beam("boat_stern_post", (x_stern + 0.04, 0.0, zk_s - 0.02), (x_stern - 0.05, 0.0, zt_s + 0.06), 0.082, "wood")
    g.beam("boat_bow_stem", (x_bow - 0.04, 0.0, zk_b - 0.02), (x_bow + 0.06, 0.0, zt_b + 0.08), 0.082, "wood")

    def add_clinker_strake(name_s, p00, p10, p01, p11, thick=0.042):
        p00, p10, p01, p11 = (
            np.asarray(p00, dtype=float),
            np.asarray(p10, dtype=float),
            np.asarray(p01, dtype=float),
            np.asarray(p11, dtype=float),
        )
        center = 0.25 * (p00 + p10 + p01 + p11)
        v_along = 0.5 * ((p10 - p00) + (p11 - p01))
        v_up = 0.5 * ((p01 - p00) + (p11 - p10))
        len_along = float(np.linalg.norm(v_along)) + 0.025
        len_up = float(np.linalg.norm(v_up)) + 0.022
        ax_x = unit(v_along)
        ax_z = unit(np.cross(ax_x, v_up))
        ax_y = unit(np.cross(ax_z, ax_x))
        rot = np.column_stack((ax_x, ax_y, ax_z))
        g.box(name_s, center, (len_along, len_up, thick), "wood", bevel=0.006, rotation=rot, top_bevel_only=True)

    for i in range(n_stat - 1):
        x0, wb0, wm0, wt0, zk0, zm0, zt0 = stations[i]
        x1, wb1, wm1, wt1, zk1, zm1, zt1 = stations[i + 1]
        for side in (-1, 1):
            add_clinker_strake(
                f"boat_strake_bot_{i}_{side}",
                (x0, side * 0.02, zk0),
                (x1, side * 0.02, zk1),
                (x0, side * wb0, zk0 + 0.04),
                (x1, side * wb1, zk1 + 0.04),
            )
            add_clinker_strake(
                f"boat_strake_mid_{i}_{side}",
                (x0, side * wb0, zk0 + 0.03),
                (x1, side * wb1, zk1 + 0.03),
                (x0, side * wm0, zm0),
                (x1, side * wm1, zm1),
            )
            add_clinker_strake(
                f"boat_strake_top_{i}_{side}",
                (x0, side * wm0, zm0 - 0.01),
                (x1, side * wm1, zm1 - 0.01),
                (x0, side * wt0, zt0),
                (x1, side * wt1, zt1),
            )
            g.beam(
                f"boat_gunwale_{i}_{side}",
                (x0, side * wt0, zt0),
                (x1, side * wt1, zt1),
                0.056,
                "wood",
                depth=0.052,
            )

    for i in range(1, n_stat - 1):
        xr, wbr, wmr, wtr, zkr, zmr, ztr = stations[i]
        g.beam(
            f"boat_rib_floor_{i}",
            (xr, -wbr + 0.02, zkr + 0.045),
            (xr, wbr - 0.02, zkr + 0.045),
            0.044,
            "wood",
        )
        for side in (-1, 1):
            g.beam(
                f"boat_rib_side_{i}_{side}",
                (xr, side * (wbr - 0.02), zkr + 0.045),
                (xr, side * (wtr - 0.03), ztr - 0.02),
                0.042,
                "wood",
            )

    for b_idx, s_idx in enumerate([2, 3, 5]):
        xb, _, wmb, wtb, _, zmb, ztb = stations[s_idx]
        w_bench = (wmb + wtb) * 0.88
        z_bench = ztb - 0.075
        g.box(
            f"boat_thwart_bench_{b_idx}",
            (xb, 0.0, z_bench),
            (0.20, w_bench * 2.0, 0.046),
            "wood",
            bevel=0.008,
            top_bevel_only=True,
        )
        for side in (-1, 1):
            g.box(
                f"boat_thwart_knee_{b_idx}_{side}",
                (xb, side * (w_bench - 0.04), z_bench + 0.03),
                (0.14, 0.07, 0.06),
                "iron",
                bevel=0.006,
                top_bevel_only=True,
            )

    g.box("boat_bow_knee", (x_bow - 0.14, 0.0, zt_b - 0.03), (0.22, 0.18, 0.048), "wood", bevel=0.008, top_bevel_only=True)
    g.box("boat_stern_knee", (x_stern + 0.14, 0.0, zt_s - 0.03), (0.22, 0.20, 0.048), "wood", bevel=0.008, top_bevel_only=True)
    g.cylinder(
        "boat_mooring_post",
        (x_bow - 0.10, 0.0, zt_b - 0.02),
        (x_bow - 0.10, 0.0, zt_b + 0.08),
        0.036,
        "iron",
        segments=8,
    )

    xm, _, _, wtm, _, _, ztm = stations[4]
    for side in (-1, 1):
        g.cylinder(
            f"boat_oarlock_{side}",
            (xm, side * wtm, ztm + 0.01),
            (xm, side * wtm, ztm + 0.095),
            0.034,
            "brass",
            segments=8,
        )
        oar_p0 = np.array([xm + 0.36, side * 0.08, ztm - 0.02], dtype=float)
        oar_p1 = np.array([xm - 0.48, side * (wtm + 0.24), ztm + 0.06], dtype=float)
        g.beam(f"boat_oar_shaft_{side}", oar_p0, oar_p1, 0.044, "wood")
        oar_dir = unit(oar_p1 - oar_p0)
        collar_c = oar_p0 + oar_dir * 0.42
        g.cylinder(
            f"boat_oar_collar_{side}",
            collar_c - oar_dir * 0.05,
            collar_c + oar_dir * 0.05,
            0.036,
            "brass",
            segments=8,
        )
        blade_c = oar_p1 + oar_dir * 0.16
        g.box(
            f"boat_oar_blade_{side}",
            blade_c,
            (0.11, 0.036, 0.34),
            "wood",
            bevel=0.006,
            rotation=frame(oar_dir),
            top_bevel_only=True,
        )

    # Medieval Merchant Boat Mast, Yardarm Spar & Furled Canvas Sail (`Thuyền Buôn Âu Cổ`)
    if seed % 2 == 0:
        mast_x = 0.08 * hl
        mast_h = 1.46
        g.cylinder(
            "boat_mast",
            (mast_x, 0.0, 0.04),
            (mast_x, 0.0, mast_h),
            0.046,
            "wood",
            end_radius=0.032,
            segments=10,
        )
        g.cylinder(
            "boat_mast_crow_cap",
            (mast_x, 0.0, mast_h - 0.04),
            (mast_x, 0.0, mast_h + 0.08),
            0.068,
            "brass",
            end_radius=0.025,
            segments=10,
        )
        yard_z = mast_h * 0.76
        g.beam(
            "boat_yardarm",
            (mast_x - 0.04, -beam * 0.68, yard_z),
            (mast_x - 0.04, beam * 0.68, yard_z),
            0.042,
            "wood",
        )
        g.cylinder(
            "boat_furled_sail",
            (mast_x - 0.08, -beam * 0.62, yard_z - 0.05),
            (mast_x - 0.08, beam * 0.62, yard_z - 0.05),
            0.072,
            "plaster",
            segments=10,
        )

    return g


def build_broadleaf_tree(scale: float = 1.0, palette_idx: int = 1, seed: float = 0.0) -> MapGeometry:
    """Builds a closed-manifold autumn oak/maple broadleaf tree (`'oak_gold'`, `'oak_amber'`, `'oak_orange'`)."""
    g = MapGeometry()
    trunk_h = 1.06 * scale
    g.cylinder(
        "oak_trunk",
        (0.0, 0.0, -0.18 * scale),
        (0.0, 0.0, trunk_h),
        0.24 * scale,
        "wood",
        end_radius=0.14 * scale,
        segments=8,
    )
    for r_i in range(3):
        ang = seed + r_i * (math.tau / 3.0)
        ca, sa = math.cos(ang), math.sin(ang)
        g.beam(
            f"oak_root_{r_i}",
            (0.08 * scale * ca, 0.08 * scale * sa, 0.18 * scale),
            (0.28 * scale * ca, 0.28 * scale * sa, -0.12 * scale),
            0.11 * scale,
            "wood",
        )
        g.beam(
            f"oak_branch_{r_i}",
            (0.0, 0.0, trunk_h * 0.72),
            (0.42 * scale * ca, 0.42 * scale * sa, trunk_h * 1.12),
            0.10 * scale,
            "wood",
        )

    mats = (
        ["oak_gold", "oak_amber", "oak_gold"]
        if palette_idx == 1
        else (["oak_amber", "oak_orange", "oak_gold"] if palette_idx == 2 else ["oak_orange", "oak_amber", "oak_gold"])
    )
    bough_specs = [
        (-0.48, -0.34, 0.94, 0.80, 0.74, 0.58),
        (-0.44,  0.40, 0.98, 0.78, 0.76, 0.60),
        ( 0.04, -0.52, 1.00, 0.78, 0.74, 0.60),
        ( 0.06,  0.50, 1.04, 0.76, 0.78, 0.62),
        ( 0.48, -0.26, 1.08, 0.80, 0.76, 0.64),
        ( 0.44,  0.32, 1.12, 0.78, 0.76, 0.64),
        (-0.30,  0.02, 1.42, 0.82, 0.78, 0.66),
        ( 0.30, -0.04, 1.48, 0.82, 0.78, 0.68),
        ( 0.02,  0.02, 1.84, 0.72, 0.70, 0.60),
    ]
    for i, (dx, dy, dz, rx, ry, rz) in enumerate(bough_specs):
        ang = seed * 1.7 + i * 0.6
        ca, sa = math.cos(ang), math.sin(ang)
        bcx = (dx * ca - dy * sa) * scale
        bcy = (dx * sa + dy * ca) * scale
        bcz = (dz - 0.26 * rz) * scale
        add_faceted_boulder(
            g,
            f"oak_bough_{i}",
            bcx,
            bcy,
            bcz,
            rx * 1.65 * scale,
            ry * 1.65 * scale,
            rz * 1.65 * scale,
            mats[i % len(mats)],
            yaw=ang,
            seed=seed + i * 1.3,
            rings=5,
            segs=9,
        )
    return g


def build_spruce_pine(scale: float = 1.0, tiers: int = 5, seed: float = 0.0) -> MapGeometry:
    """Builds a closed-manifold multi-tiered spruce pine tree (`'pine_green'`, `'pine_dark'`)."""
    g = MapGeometry()
    g.cylinder(
        "pine_trunk",
        (0.0, 0.0, -0.16 * scale),
        (0.0, 0.0, 0.65 * scale),
        0.16 * scale,
        "wood",
        end_radius=0.09 * scale,
        segments=8,
    )
    for t_i in range(tiers):
        tf = t_i / float(max(1, tiers - 1))
        z_bot = (0.38 + t_i * 0.36) * scale
        h_tier = (0.72 - 0.28 * tf) * scale
        r_bot = (0.78 - 0.50 * tf) * scale
        r_top = max(0.04 * scale, r_bot * 0.18)
        mat = "pine_dark" if t_i < 2 else "pine_green"
        g.cylinder(
            f"pine_tier_{t_i}",
            (0.0, 0.0, z_bot),
            (0.0, 0.0, z_bot + h_tier),
            r_bot,
            mat,
            end_radius=r_top,
            segments=9,
        )
    return g


# =============================================================================
# ZONE 08: 4-Arch Stone Bridge & Medieval Harbor Town
# =============================================================================

def build_zone08_harbor_and_bridge() -> MapGeometry:
    """Builds `Zone08_Vista_StoneBridge_And_HarborTown`."""
    g = MapGeometry()

    # -------------------------------------------------------------------------
    # 1. 4-Arch Ashlar Stone Bridge (`x in [9.8..19.2], y = 6.2, deck z = 2.95`)
    # -------------------------------------------------------------------------
    br_y = 6.2
    br_x0, br_x1 = 9.8, 19.2
    br_len = br_x1 - br_x0
    n_arches = 4
    bay_len = br_len / float(n_arches)
    deck_z = 2.95
    bridge_w = 2.12
    parapet_y = 0.98

    for p_i in range(n_arches + 1):
        px = br_x0 + p_i * bay_len
        g.box(
            f"bridge_pier_footing_{p_i}",
            (px, br_y, 1.28),
            (0.86, bridge_w + 0.28, 0.28),
            "stone",
            bevel=0.025,
            top_bevel_only=True,
        )
        for c_row in range(6):
            zc = 1.42 + (c_row + 0.5) * 0.255
            cw = 0.76 - 0.015 * c_row
            cd = bridge_w + 0.16 - 0.015 * c_row
            g.box(
                f"bridge_pier_course_{p_i}_{c_row}",
                (px, br_y, zc),
                (cw, cd, 0.24),
                "limestone" if c_row % 2 == 0 else "stone",
                bevel=0.018,
                top_bevel_only=True,
            )

        for side_y in (-1, 1):
            cy_cut = br_y + side_y * (bridge_w * 0.5 + 0.18)
            for cw_row in range(5):
                z_cw = 1.25 + (cw_row + 0.5) * 0.30
                s_scale = 1.0 - 0.05 * cw_row
                g.cylinder(
                    f"bridge_cutwater_{p_i}_{side_y}_{cw_row}",
                    (px, cy_cut, z_cw - 0.14),
                    (px, cy_cut, z_cw + 0.14),
                    0.40 * s_scale,
                    "limestone" if cw_row % 2 == 0 else "trim",
                    end_radius=0.38 * s_scale,
                    segments=6,
                )
            g.cylinder(
                f"bridge_cutwater_hood_{p_i}_{side_y}",
                (px, cy_cut, 2.75),
                (px, cy_cut, 2.98),
                0.38,
                "trim",
                end_radius=0.06,
                segments=6,
            )

        for side_y in (-1, 1):
            py_pil = br_y + side_y * (parapet_y + 0.05)
            g.box(
                f"bridge_pilaster_shaft_{p_i}_{side_y}",
                (px, py_pil, 3.22),
                (0.40, 0.36, 0.58),
                "limestone",
                bevel=0.018,
                top_bevel_only=True,
            )
            g.box(
                f"bridge_pilaster_cap_{p_i}_{side_y}",
                (px, py_pil, 3.53),
                (0.46, 0.42, 0.08),
                "trim",
                bevel=0.014,
                top_bevel_only=True,
            )
            add_street_or_dock_lantern(
                g,
                f"bridge_street_lantern_{p_i}_{side_y}",
                px,
                py_pil,
                3.56,
                scale=1.08,
            )

    for a_i in range(n_arches):
        acx = br_x0 + (a_i + 0.5) * bay_len
        z_spring = 1.90
        r_in = 0.84
        r_out = 1.04
        n_voussoirs = 13

        for v_i in range(n_voussoirs):
            lo = v_i * (math.pi / n_voussoirs) + 0.012
            hi = (v_i + 1) * (math.pi / n_voussoirs) - 0.012
            is_keystone = (v_i == n_voussoirs // 2)
            r_ext = r_out + (0.055 if is_keystone else 0.0)

            vault_outline = [
                (r_in * math.cos(lo), r_in * math.sin(lo)),
                ((r_out - 0.02) * math.cos(lo), (r_out - 0.02) * math.sin(lo)),
                ((r_out - 0.02) * math.cos(hi), (r_out - 0.02) * math.sin(hi)),
                (r_in * math.cos(hi), r_in * math.sin(hi)),
            ]
            prism(
                g,
                f"bridge_arch_vault_{a_i}_{v_i}",
                vault_outline,
                bridge_w - 0.08,
                (acx, br_y, z_spring),
                "stone" if v_i % 2 == 0 else "limestone",
                yaw=0.0,
            )

            voussoir_outline = [
                (r_in * math.cos(lo), r_in * math.sin(lo)),
                (r_ext * math.cos(lo), r_ext * math.sin(lo)),
                (r_ext * math.cos(hi), r_ext * math.sin(hi)),
                (r_in * math.cos(hi), r_in * math.sin(hi)),
            ]
            for side_y in (-1, 1):
                prism(
                    g,
                    f"bridge_arch_voussoir_{a_i}_{v_i}_{side_y}",
                    voussoir_outline,
                    0.14,
                    (acx, br_y + side_y * (bridge_w * 0.5 - 0.02), z_spring),
                    "trim",
                    yaw=0.0,
                )

        for side_x in (-1, 1):
            for sp_row, (dx_sp, w_sp, z_sp, h_sp) in enumerate([
                (0.76, 0.44, 2.38, 0.34),
                (0.68, 0.58, 2.72, 0.26),
            ]):
                for side_y in (-1, 1):
                    g.box(
                        f"bridge_spandrel_{a_i}_{side_x}_{sp_row}_{side_y}",
                        (acx + side_x * dx_sp, br_y + side_y * (bridge_w * 0.5 - 0.08), z_sp),
                        (w_sp, 0.22, h_sp),
                        "limestone" if sp_row % 2 == 0 else "stone",
                        bevel=0.015,
                        top_bevel_only=True,
                    )

        n_par_blocks = 5
        span_par = bay_len - 0.38
        for side_y in (-1, 1):
            py_w = br_y + side_y * parapet_y
            for row_p in range(2):
                zp = deck_z + 0.11 + row_p * 0.17
                for b_k in range(n_par_blocks):
                    bx_p = acx - span_par * 0.5 + (b_k + 0.5) * (span_par / n_par_blocks)
                    g.box(
                        f"bridge_parapet_stone_{a_i}_{side_y}_{row_p}_{b_k}",
                        (bx_p, py_w, zp),
                        (span_par / n_par_blocks - 0.016, 0.22, 0.158),
                        "limestone",
                        bevel=0.012,
                        top_bevel_only=True,
                    )
            for c_k in range(n_par_blocks):
                bx_c = acx - span_par * 0.5 + (c_k + 0.5) * (span_par / n_par_blocks)
                g.box(
                    f"bridge_parapet_coping_{a_i}_{side_y}_{c_k}",
                    (bx_c, py_w, deck_z + 0.42),
                    (span_par / n_par_blocks - 0.012, 0.28, 0.082),
                    "trim",
                    bevel=0.014,
                    top_bevel_only=True,
                )

    g.box(
        "bridge_deck_core",
        (0.5 * (br_x0 + br_x1), br_y, deck_z - 0.06),
        (br_len + 0.70, bridge_w, 0.18),
        "mortar",
        bevel=0.018,
        top_bevel_only=True,
    )
    for side_y in (-1, 1):
        g.box(
            f"bridge_stringcourse_{side_y}",
            (0.5 * (br_x0 + br_x1), br_y + side_y * (bridge_w * 0.5 + 0.03), deck_z - 0.02),
            (br_len + 0.76, 0.16, 0.10),
            "trim",
            bevel=0.014,
            top_bevel_only=True,
        )

    n_pave = 20
    pave_dx = (br_len + 0.40) / float(n_pave)
    for p_k in range(n_pave):
        tf = p_k / float(n_pave - 1)
        camber = 0.045 * math.sin(tf * math.pi)
        px_s = (br_x0 - 0.20) + (p_k + 0.5) * pave_dx
        for lane_y in (-0.42, 0.42):
            g.box(
                f"bridge_road_flagstone_{p_k}_{int(lane_y*10)}",
                (px_s, br_y + lane_y, deck_z + 0.03 + camber),
                (pave_dx - 0.018, 0.78, 0.065),
                "stone" if (p_k + (0 if lane_y < 0 else 1)) % 2 == 0 else "limestone",
                bevel=0.01,
                top_bevel_only=True,
            )

    for end_name, x_outer, x_inner in (("west", 9.05, br_x0 - 0.10), ("east", 19.95, br_x1 + 0.10)):
        z_ground_end = float(sample_ground_z(x_outer, br_y))
        g.beam(
            f"bridge_approach_ramp_{end_name}",
            (x_outer, br_y, z_ground_end - 0.04),
            (x_inner, br_y, deck_z - 0.02),
            bridge_w - 0.12,
            "stone",
            depth=0.22,
        )

    # -------------------------------------------------------------------------
    # 2. 7 Harbor Town Medieval European (`Âu Cổ`) Guildhalls + 2 Cathedral Clock/Bell Towers
    # -------------------------------------------------------------------------
    harbor_houses = [
        (20.2, 4.4, 2.28, 1.76, -0.32, "terracotta"),
        (22.2, 3.0, 2.38, 1.82, -0.35, "slate_blue"),
        (24.2, 1.6, 2.22, 1.68, -0.38, "terracotta"),
        (26.0, 0.2, 2.14, 1.62, -0.40, "terracotta"),
        (21.6, 6.2, 2.42, 1.84, -0.25, "terracotta"),
        (23.6, 4.6, 2.48, 1.88, -0.30, "slate_blue"),
        (25.4, 3.2, 2.24, 1.70, -0.32, "terracotta"),
    ]
    for h_idx, (hx, hy, hsx, hsy, hyaw, hrstyle) in enumerate(harbor_houses):
        hz = max(2.35, float(sample_ground_z(hx, hy)))
        wall_h = 1.48 + 0.12 * (h_idx % 3)
        roof_h = 1.46 + 0.10 * ((h_idx + 1) % 3)
        house_geom = build_harbor_house(
            width=hsx,
            depth=hsy,
            wall_h=wall_h,
            roof_h=roof_h,
            roof_mat=hrstyle,
            variant=h_idx,
        )
        g.merge_from(house_geom, origin=(hx, hy, hz), yaw=hyaw)

    clock_towers = [
        (24.6, 5.6, 4.05, 2.85),
        (22.4, 7.6, 3.75, 2.65),
    ]
    for t_idx, (tx_h, ty_h, th_base, th_spire) in enumerate(clock_towers):
        tz_h = max(2.55, float(sample_ground_z(tx_h, ty_h)))
        tower_geom = build_clock_spire_tower(
            width=1.38,
            th_base=th_base,
            th_spire=th_spire,
            variant=t_idx,
        )
        g.merge_from(tower_geom, origin=(tx_h, ty_h, tz_h), yaw=-0.28)

    # -------------------------------------------------------------------------
    # 3. Harbor Stone Plaza Fountain & 3 Covered Medieval Market Pavilions (`Quầy Chợ Âu Cổ`)
    # -------------------------------------------------------------------------
    fz_p = max(2.25, float(sample_ground_z(23.2, 0.4)))
    g.merge_from(build_plaza_fountain(23.2, 0.4, fz_p))

    market_stalls = [
        (21.5, 0.9, -0.34, "terracotta"),
        (24.6, -0.7, -0.42, "slate_blue"),
        (22.7, -1.3, 0.22, "terracotta"),
    ]
    for st_idx, (st_x, st_y, st_yaw, st_mat) in enumerate(market_stalls):
        st_z = max(2.24, float(sample_ground_z(st_x, st_y)))
        st_g = MapGeometry()
        st_g.box(f"stall_{st_idx}_plinth", (0.0, 0.0, 0.04), (1.16, 0.78, 0.18), "stone", 0.014, top_bevel_only=True)
        st_g.box(f"stall_{st_idx}_counter", (0.0, -0.18, 0.28), (1.02, 0.34, 0.34), "wood", 0.012, top_bevel_only=True)
        for sx_p in (-0.48, 0.48):
            for sy_p in (-0.30, 0.30):
                st_g.box(f"stall_{st_idx}_post_{sx_p:+.2f}_{sy_p:+.2f}", (sx_p, sy_p, 0.56), (0.08, 0.08, 0.88), "wood", 0.008, top_bevel_only=True)
        tiled_roof_custom(
            st_g,
            f"stall_{st_idx}_canopy",
            0.0,
            0.0,
            1.28,
            0.92,
            0.98,
            0.34,
            roof_mat=st_mat,
            ridge_x=True,
            tile_col_step=0.26,
            tile_row_step=0.18,
        )
        g.merge_from(st_g, origin=(st_x, st_y, st_z), yaw=st_yaw)

    # -------------------------------------------------------------------------
    # 4. Timber Plank Docks, Waterfront Quay, 5 Finger Jetties & 5 Rowboats
    # -------------------------------------------------------------------------
    pier_pts = [
        (18.2, 4.4),
        (19.6, 2.8),
        (21.2, 1.2),
        (22.8, -0.4),
        (24.4, -2.0),
        (25.8, -3.4),
    ]
    z_quay = 2.16
    quay_w = 1.38

    for p_i in range(len(pier_pts) - 1):
        x0_p, y0_p = pier_pts[p_i]
        x1_p, y1_p = pier_pts[p_i + 1]
        dx_p, dy_p = x1_p - x0_p, y1_p - y0_p
        seg_len = math.hypot(dx_p, dy_p)
        pyaw = math.atan2(dy_p, dx_p)
        ca, sa = math.cos(pyaw), math.sin(pyaw)
        nx_r, ny_r = sa, -ca
        rot_q = np.array([[ca, -sa, 0], [sa, ca, 0], [0, 0, 1]])
        mx_p, my_p = 0.5 * (x0_p + x1_p), 0.5 * (y0_p + y1_p)

        for sw_k in range(4):
            ts = (sw_k + 0.5) / 4.0
            sx_w = x0_p + dx_p * ts - nx_r * (quay_w * 0.46)
            sy_w = y0_p + dy_p * ts - ny_r * (quay_w * 0.46)
            g.box(
                f"quay_seawall_{p_i}_{sw_k}",
                (sx_w, sy_w, 1.92),
                (seg_len / 4.0 - 0.02, 0.34, 0.56),
                "stone" if sw_k % 2 == 0 else "limestone",
                bevel=0.018,
                rotation=rot_q,
                top_bevel_only=True,
            )

        for lat_off in (-quay_w * 0.38, 0.0, quay_w * 0.38):
            p_start = (x0_p + nx_r * lat_off - 0.10 * ca, y0_p + ny_r * lat_off - 0.10 * sa, z_quay - 0.08)
            p_end = (x1_p + nx_r * lat_off + 0.10 * ca, y1_p + ny_r * lat_off + 0.10 * sa, z_quay - 0.08)
            g.beam(f"quay_stringer_{p_i}_{int(lat_off*10)}", p_start, p_end, 0.12, "wood", depth=0.14)

        n_planks = 14
        plank_pitch = (seg_len + 0.16) / float(n_planks)
        for k_pl in range(n_planks):
            u_pl = -0.08 + (k_pl + 0.5) * plank_pitch
            px_pl = x0_p + u_pl * ca
            py_pl = y0_p + u_pl * sa
            g.box(
                f"quay_plank_{p_i}_{k_pl}",
                (px_pl, py_pl, z_quay),
                (plank_pitch - 0.018, quay_w, 0.068),
                "wood",
                bevel=0.008,
                rotation=rot_q,
                top_bevel_only=True,
            )

        for t_pile in (0.15, 0.85):
            for side_pile in (-quay_w * 0.44, quay_w * 0.44):
                ppx = x0_p + dx_p * t_pile + nx_r * side_pile
                ppy = y0_p + dy_p * t_pile + ny_r * side_pile
                g.cylinder(
                    f"quay_piling_{p_i}_{int(t_pile*10)}_{int(side_pile*10)}",
                    (ppx, ppy, 1.35),
                    (ppx, ppy, 2.34),
                    0.105,
                    "wood",
                    end_radius=0.098,
                    segments=8,
                )
                g.cylinder(
                    f"quay_piling_band_{p_i}_{int(t_pile*10)}_{int(side_pile*10)}",
                    (ppx, ppy, 2.22),
                    (ppx, ppy, 2.30),
                    0.114,
                    "iron",
                    segments=8,
                )

        j_len = 1.58
        j_w = 0.64
        j_yaw = pyaw - math.pi * 0.5
        cj, sj = math.cos(j_yaw), math.sin(j_yaw)
        rot_j = np.array([[cj, -sj, 0], [sj, cj, 0], [0, 0, 1]])
        jx0 = mx_p + nx_r * (quay_w * 0.46)
        jy0 = my_p + ny_r * (quay_w * 0.46)
        z_jetty = 2.04

        for s_lat in (-j_w * 0.36, j_w * 0.36):
            ja = (jx0 - s_lat * sj, jy0 + s_lat * cj, z_jetty - 0.06)
            jb = (jx0 + j_len * cj - s_lat * sj, jy0 + j_len * sj + s_lat * cj, z_jetty - 0.06)
            g.beam(f"jetty_stringer_{p_i}_{int(s_lat*10)}", ja, jb, 0.095, "wood")

        n_j_planks = 12
        j_pitch = j_len / float(n_j_planks)
        for jk in range(n_j_planks):
            dist_j = (jk + 0.5) * j_pitch
            g.box(
                f"jetty_plank_{p_i}_{jk}",
                (jx0 + dist_j * cj, jy0 + dist_j * sj, z_jetty),
                (j_pitch - 0.016, j_w, 0.056),
                "wood",
                bevel=0.007,
                rotation=rot_j,
                top_bevel_only=True,
            )

        for d_pile in (0.45, j_len - 0.14):
            for s_lat in (-j_w * 0.44, j_w * 0.44):
                jpx = jx0 + d_pile * cj - s_lat * sj
                jpy = jy0 + d_pile * sj + s_lat * cj
                g.cylinder(
                    f"jetty_piling_{p_i}_{int(d_pile*10)}_{int(s_lat*10)}",
                    (jpx, jpy, 1.32),
                    (jpx, jpy, 2.26),
                    0.088,
                    "wood",
                    segments=8,
                )
                g.cylinder(
                    f"jetty_piling_ring_{p_i}_{int(d_pile*10)}_{int(s_lat*10)}",
                    (jpx, jpy, 2.16),
                    (jpx, jpy, 2.24),
                    0.098,
                    "iron",
                    segments=8,
                )

        boat_geom = build_wooden_rowboat(length=1.68, beam=0.68, depth=0.34, seed=p_i)
        bx_r = jx0 + (j_len * 0.62) * cj - 0.66 * sj
        by_r = jy0 + (j_len * 0.62) * sj + 0.66 * cj
        boat_yaw = pyaw + 0.10 * (1 if p_i % 2 == 0 else -1)
        g.merge_from(boat_geom, origin=(bx_r, by_r, 1.96), yaw=boat_yaw)

    for idx_pt, (px_d, py_d) in enumerate(pier_pts):
        lx_d = px_d - 0.48
        ly_d = py_d - 0.40
        g.cylinder(
            f"dock_lantern_pile_{idx_pt}",
            (lx_d, ly_d, 1.35),
            (lx_d, ly_d, 2.42),
            0.12,
            "wood",
            end_radius=0.105,
            segments=8,
        )
        add_street_or_dock_lantern(
            g,
            f"dock_lantern_{idx_pt}",
            lx_d,
            ly_d,
            2.42,
            scale=0.96,
        )
        g.cylinder(
            f"dock_bollard_{idx_pt}",
            (px_d + 0.22, py_d + 0.16, z_quay + 0.02),
            (px_d + 0.22, py_d + 0.16, z_quay + 0.26),
            0.075,
            "iron",
            end_radius=0.092,
            segments=8,
        )
        if idx_pt < 5:
            g.cylinder(
                f"dock_barrel_{idx_pt}",
                (px_d + 0.38, py_d - 0.12, z_quay + 0.02),
                (px_d + 0.38, py_d - 0.12, z_quay + 0.44),
                0.17,
                "wood",
                end_radius=0.16,
                segments=10,
            )
            for z_hoop in (z_quay + 0.10, z_quay + 0.34):
                g.cylinder(
                    f"dock_barrel_hoop_{idx_pt}_{int(z_hoop*100)}",
                    (px_d + 0.38, py_d - 0.12, z_hoop),
                    (px_d + 0.38, py_d - 0.12, z_hoop + 0.05),
                    0.18,
                    "iron",
                    segments=10,
                )

    # -------------------------------------------------------------------------
    # 5. Riverbank Autumn Oak & Pine Trees (`Zone08` trees)
    # -------------------------------------------------------------------------
    placed_trees = []
    building_keepouts = [
        (-17.8, 5.4, 2.7), (-17.0, 9.2, 3.0), (-13.4, 9.4, 2.8),
        (-7.8, 6.4, 3.2), (-0.6, 8.0, 2.4), (2.4, 7.4, 2.4), (1.0, 9.6, 2.5),
        (8.6, 4.6, 2.6),
    ]

    def can_place_tree(tx, ty, min_d=1.28):
        if float(island_signed_distance(tx, ty)) > 0.93:
            return False
        if float(eval_road_mask(tx, ty)) > 0.25:
            return False
        rcx, rhw = eval_river_center_and_width(ty)
        if ty < 12.5 and ty > -7.2 and abs(tx - float(rcx)) < float(rhw) + 1.05:
            return False
        if tx > 17.5 and ty > -2.5 and ty < 8.8:
            return False
        for bx_k, by_k, br_k in building_keepouts:
            if math.hypot(tx - bx_k, ty - by_k) < br_k:
                return False
        for px_t, py_t in placed_trees:
            if math.hypot(tx - px_t, ty - py_t) < min_d:
                return False
        return True

    rng = np.random.RandomState(42)
    for _ in range(580):
        tx = rng.uniform(-25.8, 10.8)
        ty = rng.uniform(-2.5, 14.2)
        if ty < 4.2 and tx > -18.2:
            continue
        if ty < 6.4 and tx > -15.5 and abs(tx + 11.6) > 2.8 and abs(tx + 3.4) > 2.8 and abs(tx - 5.2) > 2.6:
            continue
        if not can_place_tree(tx, ty, min_d=1.30):
            continue
        placed_trees.append((tx, ty))
        pine_prob = 0.55 if ty > 12.0 else 0.30
        if rng.rand() < pine_prob:
            _ = rng.uniform(0.75, 1.15)
        else:
            _ = rng.uniform(0.82, 1.18)
            if rng.rand() >= 0.58:
                _ = rng.rand()

    for idx_r in range(115):
        ty = rng.uniform(-7.2, 11.2)
        rcx, rhw = eval_river_center_and_width(ty)
        side = -1.0 if rng.rand() < 0.68 else 1.0
        tx = float(rcx) + side * (float(rhw) + rng.uniform(0.95, 2.7))
        # Keep the 4-arch stone bridge view corridor (`y in [1.8, 8.2]`) and steep northern cliff (`y > 7.2, x > 16.5`) clear
        if 1.8 < ty < 8.2:
            continue
        if ty > 7.2 and tx > 16.5:
            continue
        if not can_place_tree(tx, ty, min_d=1.25):
            continue
        placed_trees.append((tx, ty))
        tz = float(sample_ground_z(tx, ty))
        if rng.rand() < 0.42:
            sc = rng.uniform(0.62, 0.94)
            pine_geom = build_spruce_pine(scale=sc, tiers=5, seed=float(idx_r))
            g.merge_from(pine_geom, origin=(tx, ty, tz), yaw=idx_r * 0.4)
        else:
            sc = rng.uniform(0.75, 1.08)
            tree_geom = build_broadleaf_tree(scale=sc, palette_idx=1 + (idx_r % 2), seed=float(idx_r))
            g.merge_from(tree_geom, origin=(tx, ty, tz), yaw=idx_r * 0.5)

    return g
