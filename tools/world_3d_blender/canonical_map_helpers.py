"""Canonical terrain samplers and high-detail closed-manifold architectural helpers.

100% aligned with `D:/Pig-tycoon/Tools/build_true360_glb_masterpiece.py` and
`D:/Pig-tycoon/Tools/observatory_geometry.py`. Every component emitted by `MapGeometry`
is a water-tight outward-wound 3D manifold solid (`bad_edges == 0`).
"""
import math
import struct
import numpy as np
from observatory_geometry import Geometry, frame, unit, meshbin_bytes

from map_surface_contract import (
    WORLD_X_MAX,
    WORLD_Y_MAX,
    ISLAND_RX,
    ISLAND_RY,
    strip_uv,
    eval_river_center_and_width,
    eval_road_mask,
    eval_pond_distances,
    sample_ground_z,
    island_signed_distance,
)

SUN_DIR = np.array([0.68, -0.38, 0.62], dtype=np.float32)
SUN_DIR /= np.linalg.norm(SUN_DIR)

EXTENDED_PALETTE = {
    # Original observatory_geometry.py palette
    'stone': (4, .57, .16),
    'mortar': (4, .32, .10),
    'trim': (4, .76, .22),
    'plaster': (1, .20, .78),
    'wood': (1, .83, .60),
    'door': (0, .32, .48),
    'roof': (5, .51, .58),
    'dome': (5, .57, .42),
    'brass': (0, .45, .85),
    'glass': (1, .48, .70),
    'iron': (5, .72, .12),
    'lens': (5, .48, .20),
    # Extended map materials on the 8 atlas strips
    'terracotta': (5, .18, .62),
    'slate_blue': (5, .52, .60),
    'spire_dark': (5, .84, .56),
    'sandstone_warm': (0, .26, .58),
    'sandstone_ochre': (0, .16, .42),
    'sandstone_shadow': (0, .08, .25),
    'ruin_ashlar': (0, .74, .54),
    'ruin_carved': (0, .86, .68),
    'limestone': (6, .86, .62),
    'orb_sapphire': (6, .18, .56),
    'lotus_gold': (6, .54, .68),
    'water_river': (2, .22, .52),
    'water_pond': (2, .62, .55),
    'water_foam': (2, .90, .75),
    'pine_green': (3, .14, .62),
    'pine_dark': (3, .08, .36),
    'oak_gold': (3, .38, .72),
    'oak_amber': (3, .62, .68),
    'oak_orange': (3, .88, .66),
    'grass_gold': (7, .18, .66),
    'shrub_green': (7, .45, .58),
    'flower_gold': (7, .62, .74),
    'flower_magenta': (7, .78, .72),
    'flower_violet': (7, .92, .72),
}


class MapGeometry(Geometry):
    """Extends `observatory_geometry.Geometry` with `EXTENDED_PALETTE` and world placement."""

    def add(self, name, vertices, polygons, material, convex_center=None):
        vertices = np.asarray(vertices, dtype=float)
        triangles = []
        for poly in polygons:
            for i in range(1, len(poly) - 1):
                ids = [poly[0], poly[i], poly[i + 1]]
                tri = vertices[ids]
                normal = np.cross(tri[1] - tri[0], tri[2] - tri[0])
                if np.linalg.norm(normal) < 1e-10:
                    raise ValueError(f'Degenerate triangle in {name}')
                if convex_center is not None and np.dot(normal, tri.mean(axis=0) - convex_center) < 0:
                    ids[1], ids[2] = ids[2], ids[1]
                triangles.append(ids)
        start_face = len(self.faces)
        strip, u, v = EXTENDED_PALETTE[material]
        v = np.clip(v + self.rng.uniform(-.06, .06), .06, .92)
        patch = np.array([[u - .008, v - .012], [u + .008, v - .012], [u, v + .012]])
        patch[:, 0] = (strip + .03 + .94 * patch[:, 0]) / 8
        patch[:, 1] = .006 + .238 * patch[:, 1]
        for ids in triangles:
            start = len(self.vertices)
            self.vertices.extend(vertices[ids])
            self.uvs.extend(patch)
            self.faces.append((start, start + 1, start + 2))
        self.parts.append({
            'name': name,
            'material': material,
            'first_triangle': start_face,
            'triangles': len(triangles),
        })

    def merge_from(self, other_geom, origin=(0.0, 0.0, 0.0), yaw=0.0):
        """Appends all closed parts from `other_geom` rotated by `yaw` and translated by `origin`."""
        if not other_geom.faces:
            return
        v_arr, uv_arr, f_arr = other_geom.arrays(origin=origin, yaw=yaw)
        v_offset = len(self.vertices)
        f_offset = len(self.faces)
        self.vertices.extend(v_arr.tolist())
        self.uvs.extend(uv_arr.tolist())
        for tri in f_arr:
            self.faces.append((int(tri[0]) + v_offset, int(tri[1]) + v_offset, int(tri[2]) + v_offset))
        for p in other_geom.parts:
            self.parts.append({
                'name': p['name'],
                'material': p['material'],
                'first_triangle': p['first_triangle'] + f_offset,
                'triangles': p['triangles'],
            })


def validate_closed_parts(geom: MapGeometry) -> dict:
    """Verifies every part in `geom` is a closed outward-wound 3D manifold solid (`bad_edges == 0`)."""
    v_arr, _, f_arr = geom.arrays()
    n_parts = len(geom.parts)
    if n_parts == 0 or len(f_arr) == 0:
        return {
            'parts': 0,
            'vertices': 0,
            'triangles': 0,
            'closed_outward_components': True,
            'failures': [],
        }

    # Fast vectorized check across all parts simultaneously (in float64 relative to each part's reference vertex)
    v_f64 = v_arr.astype(np.float64)
    part_ids = np.empty(len(f_arr), dtype=np.int64)
    part_ref_v = np.empty((n_parts, 3), dtype=np.float64)
    for p_idx, part in enumerate(geom.parts):
        t0 = part['first_triangle']
        tc = part['triangles']
        part_ids[t0:t0 + tc] = p_idx
        part_ref_v[p_idx] = v_f64[f_arr[t0, 0]]

    tri_pts = v_f64[f_arr]
    d0 = tri_pts[:, 0] - part_ref_v[part_ids]
    e1 = tri_pts[:, 1] - tri_pts[:, 0]
    e2 = tri_pts[:, 2] - tri_pts[:, 0]
    tri_vol6 = np.einsum('ij,ij->i', d0, np.cross(e1, e2))

    part_vols = np.empty(n_parts, dtype=np.float64)
    for p_idx, part in enumerate(geom.parts):
        t0 = part['first_triangle']
        tc = part['triangles']
        part_vols[p_idx] = float(np.sum(tri_vol6[t0:t0 + tc]))

    # Quantize vertex positions to 1e-5 m and combine with part_id
    vq = np.round(v_arr[f_arr.reshape(-1)] * 100000.0).astype(np.int64)
    p_vert = np.repeat(part_ids, 3)[:, None]
    vp_key = np.hstack([p_vert, vq])
    _, inv = np.unique(vp_key, axis=0, return_inverse=True)
    tris = inv.reshape(-1, 3).astype(np.int64)

    # Directed edges: (u, v) and reverse (v, u)
    u0, u1, u2 = tris[:, 0], tris[:, 1], tris[:, 2]
    e_u = np.concatenate([u0, u1, u2])
    e_v = np.concatenate([u1, u2, u0])
    max_v = int(inv.max()) + 1 if len(inv) else 1
    dir_keys = e_u * max_v + e_v
    rev_keys = e_v * max_v + e_u

    dir_sorted = np.sort(dir_keys)
    rev_sorted = np.sort(rev_keys)
    has_dup_dir = bool(np.any(dir_sorted[1:] == dir_sorted[:-1])) if len(dir_sorted) > 1 else False
    all_matched = (not has_dup_dir) and np.array_equal(dir_sorted, rev_sorted) and bool(np.all(part_vols > 1e-8))

    if all_matched:
        return {
            'parts': n_parts,
            'vertices': int(len(v_arr)),
            'triangles': int(len(f_arr)),
            'closed_outward_components': True,
            'failures': [],
        }

    failures = []
    for p_idx, part in enumerate(geom.parts):
        t0 = part['first_triangle']
        tc = part['triangles']
        sub_f = f_arr[t0:t0 + tc]
        sub_v = np.round(v_arr[sub_f.reshape(-1)], 5)
        _, p_inv = np.unique(sub_v, axis=0, return_inverse=True)
        p_tris = p_inv.reshape(-1, 3)

        edge_counts = {}
        for a, b, c in p_tris:
            for u_e, v_e in ((a, b), (b, c), (c, a)):
                edge_counts[(int(u_e), int(v_e))] = edge_counts.get((int(u_e), int(v_e)), 0) + 1

        bad_edges = 0
        for (u_e, v_e), cnt in edge_counts.items():
            if cnt != 1 or edge_counts.get((v_e, u_e), 0) != 1:
                bad_edges += 1

        vol6 = float(part_vols[p_idx])
        if bad_edges > 0 or vol6 <= 1e-8:
            failures.append({
                'part_index': p_idx,
                'name': part['name'],
                'bad_edges': bad_edges,
                'vol6': vol6,
            })
            if len(failures) >= 10:
                break

    return {
        'parts': n_parts,
        'vertices': int(len(v_arr)),
        'triangles': int(len(f_arr)),
        'closed_outward_components': len(failures) == 0,
        'failures': failures,
    }
