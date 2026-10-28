"""Zone10 Multi-Layered Alpine Mountain Ranges & Tiered Rocky Highland Outcrops (`Tools/alpine_geometry.py` v3).

Addresses:
1. User Visual Inspection of Zoomed Crops (`crop1_zone10_mountains_and_left_highland.png` & `crop6_img2_mountains_and_left_highland.png`):
   - **Multi-Layered Cluster of Mountain Ranges (`tập hợp các dãy núi xếp lớp xa - trung - gần`)**:
     Built as 3 distinct depth ranges (Distant Horizon Range `y ~ 17.8..21.5`, Midground Grand Cordillera `y ~ 14.2..18.6`,
     and Foreground Diagonal Rock Spurs `y ~ 12.0..15.2`) composed of chiseled, flat-faceted pyramidal/prismatic
     geological massifs with sharp diagonal arête ridges, sunlit warm granite east faces, deep slate-blue shadow
     west faces, and snow-filled V-couloirs.
   - **What Stands in Front of the Mountains (`phần đứng trước núi`)**:
     Replaces the old row of round brown boulders with the authentic **3-Tier Stepped Rocky Highland Hill / Butte
     (`Gò Đồi Đá Phân Tầng Góc Tây-Bắc` at `x in [-23.5..-10.2], y in [8.6..13.6]`)** and **Central-East Stepped
     Cliff Terrace (`x in [-6.8..+5.8], y in [10.4..13.8]`)**, built from vertical-walled beveled cliff strata blocks
     and crowned with dense, cascading golden-yellow/amber autumn oak trees and emerald alpine pines growing directly
     on top of and down the stepped rock ledges.
2. Codex Contract (`Tools/map_surface_contract.py`):
   - Imports `sample_ground_z`, `island_signed_distance`, `eval_road_mask`, `eval_river_center_and_width`, `strip_uv`
     directly from `map_surface_contract.py` (via `canonical_map_helpers.py`).
   - Provides both `build_alpine_massif_only()` and `build_alpine_zone()`, with 100% closed outward-wound 2-manifold
     components (`bad_edges == 0`, `vol6 > 0`).
"""
import math
import struct
import numpy as np
from canonical_map_helpers import (
    ISLAND_RX,
    ISLAND_RY,
    SUN_DIR,
    eval_river_center_and_width,
    eval_road_mask,
    island_signed_distance,
    sample_ground_z,
    strip_uv,
)


def meshbin_bytes(vertices: np.ndarray, uvs: np.ndarray, faces: np.ndarray) -> bytes:
    """Unity `.meshbin` binary layout with explicit flat normals and Y-up winding."""
    tri = vertices[faces]
    normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    lengths = np.linalg.norm(normals, axis=1)
    if np.any(lengths <= 1e-9):
        raise ValueError("Cannot export degenerate triangles in Zone10")
    if not np.array_equal(faces.reshape(-1), np.arange(len(vertices))):
        raise ValueError("Flat-normal export expects split triangle vertices")
    positions = vertices[:, [0, 2, 1]].astype("<f4")
    normals_up = np.repeat(normals / lengths[:, None], 3, axis=0)[:, [0, 2, 1]].astype("<f4")
    indices = faces[:, [0, 2, 1]].astype("<i4")
    return (
        struct.pack("<ii", len(vertices), faces.size)
        + positions.tobytes()
        + normals_up.tobytes()
        + uvs.astype("<f4").tobytes()
        + indices.tobytes()
    )


class ClosedAlpineGeometry:
    """Collects closed, outward-wound 3D solid components with split flat-normal triangles."""

    def __init__(self):
        self.v_chunks = []
        self.uv_chunks = []
        self.parts = []
        self.total_tris = 0

    def add_closed_component(
        self,
        name: str,
        vertices: np.ndarray,
        faces: np.ndarray,
        strip_idx: int,
        u_range=(0.06, 0.94),
        v_range=(0.08, 0.92),
        per_face_uv: np.ndarray = None,
        enforce_outward_center: np.ndarray = None,
    ):
        v_arr = np.asarray(vertices, dtype=np.float64)
        f_arr = np.asarray(faces, dtype=np.int32)
        if len(f_arr) == 0:
            return

        if enforce_outward_center is not None:
            c = np.asarray(enforce_outward_center, dtype=np.float64)
            tri0 = v_arr[f_arr]
            norms0 = np.cross(tri0[:, 1] - tri0[:, 0], tri0[:, 2] - tri0[:, 0])
            centers0 = tri0.mean(axis=1)
            flip_mask = np.einsum("ij,ij->i", norms0, centers0 - c[None, :]) < 0.0
            if np.any(flip_mask):
                f_arr = f_arr.copy()
                f_arr[flip_mask, 1], f_arr[flip_mask, 2] = f_arr[flip_mask, 2].copy(), f_arr[flip_mask, 1].copy()

        tri = v_arr[f_arr]
        norms = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
        n_len = np.linalg.norm(norms, axis=1)
        if np.any(n_len <= 1e-10):
            raise ValueError(f"Degenerate triangle in component '{name}'")
        n_unit = norms / n_len[:, None]

        if per_face_uv is not None:
            uv_tri = np.asarray(per_face_uv, dtype=np.float32)
        else:
            sun_dot = np.clip(np.sum(n_unit * SUN_DIR[None, :], axis=1), -1.0, 1.0)
            u0, u1 = u_range
            v0, v1 = v_range
            u_mid = u0 + (u1 - u0) * (0.5 + 0.44 * sun_dot)
            z_min = float(np.min(v_arr[:, 2]))
            z_max = float(np.max(v_arr[:, 2]))
            dz = max(0.15, z_max - z_min)
            v_vert = v0 + (v1 - v0) * np.clip((tri[:, :, 2] - z_min) / dz, 0.0, 1.0)
            u_vert = np.stack(
                [
                    np.clip(u_mid - 0.012, u0, u1),
                    np.clip(u_mid + 0.012, u0, u1),
                    u_mid,
                ],
                axis=-1,
            )
            uv_tri = strip_uv(strip_idx, u_vert.reshape(-1), v_vert.reshape(-1)).reshape(-1, 3, 2)

        n_t = len(f_arr)
        self.v_chunks.append(tri.reshape(-1, 3).astype(np.float32))
        self.uv_chunks.append(uv_tri.reshape(-1, 2).astype(np.float32))
        self.parts.append(
            {
                "name": name,
                "strip": int(strip_idx),
                "first_triangle": self.total_tris,
                "triangles": int(n_t),
            }
        )
        self.total_tris += n_t

    def arrays(self):
        if not self.v_chunks:
            return (
                np.zeros((0, 3), dtype=np.float32),
                np.zeros((0, 2), dtype=np.float32),
                np.zeros((0, 3), dtype=np.int32),
            )
        v_all = np.vstack(self.v_chunks).astype(np.float32)
        uv_all = np.vstack(self.uv_chunks).astype(np.float32)
        f_all = np.arange(len(v_all), dtype=np.int32).reshape(-1, 3)
        return v_all, uv_all, f_all


def _add_chiseled_alpine_peak_massif(
    g: ClosedAlpineGeometry,
    name: str,
    cx: float,
    cy: float,
    z_base: float,
    rx: float,
    ry: float,
    height: float,
    yaw: float,
    num_ridges: int = 7,
    ridge_sharpness: float = 0.28,
    summit_lean_x: float = 0.12,
    summit_lean_y: float = 0.08,
    snow_threshold: float = 0.52,
    seed: float = 0.0,
):
    """Builds a chiseled, flat-faceted pyramidal Alpine peak massif (`khối núi đa diện vát cạnh sắc nét`).

    Circumferential stations alternate between protruding sharp arête ridges (`sống núi`) and recessed
    V-shaped glacial gullies (`rãnh tuyết`), producing crisp planar rock faces with strong sunlit/shadow
    contrast and snow couloirs matching Target Images 1 & 2.
    """
    segs = num_ridges * 2  # Alternating ridge crest (even) and V-couloir gully (odd)
    rings = 5
    ring_fracs = [0.0, 0.22, 0.46, 0.70, 0.88, 1.0]

    verts = [[0.0, 0.0, 0.0]]  # 0: bottom center
    is_gully_station = []

    for s_i in range(segs):
        is_gully_station.append((s_i % 2) == 1)

    for r_idx, vf in enumerate(ring_fracs[:-1]):
        # Pyramidal taper with broad rocky base and sharp upper arêtes
        base_taper = (1.0 - vf) ** 0.88
        z_ring = vf * height
        shift_x = summit_lean_x * rx * (vf ** 1.15)
        shift_y = summit_lean_y * ry * (vf ** 1.15)

        for s_i in range(segs):
            base_th = s_i * (2.0 * math.pi / segs)
            # Slight angular jitter so facets look hand-chiseled rather than mechanical
            th_jit = 0.07 * math.sin(s_i * 2.3 + seed + r_idx * 0.6)
            th = base_th + th_jit
            is_gul = is_gully_station[s_i]

            # Diagonal twist along altitude so arêtes sweep diagonally down the mountain face
            twist = -0.18 * (1.0 - vf) * math.sin(base_th)
            th_eff = th + twist

            if not is_gul:
                # Protruding sharp arête ridge crest
                r_mod = 1.0 + ridge_sharpness * (0.65 + 0.55 * math.sin(vf * math.pi))
                z_mod = z_ring + (0.06 * height * math.sin(vf * math.pi)) * (1.0 if r_idx > 0 else 0.0)
            else:
                # Recessed V-couloir gully between ridges
                r_mod = 1.0 - ridge_sharpness * (0.72 + 0.35 * math.sin(vf * math.pi))
                z_mod = z_ring - (0.04 * height * math.sin(vf * math.pi)) * (1.0 if r_idx > 0 else 0.0)

            asym = 1.0 + 0.14 * math.cos(th_eff * 2.0 + seed) + 0.09 * math.sin(th_eff * 3.0 - seed * 0.7)
            lx = shift_x + rx * base_taper * r_mod * asym * math.cos(th_eff)
            ly = shift_y + ry * base_taper * r_mod * asym * math.sin(th_eff)
            verts.append([lx, ly, max(0.0, z_mod)])

    top_idx = len(verts)
    verts.append([summit_lean_x * rx, summit_lean_y * ry, height])

    v_arr = np.asarray(verts, dtype=np.float64)
    c, s = math.cos(yaw), math.sin(yaw)
    rot = np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]], dtype=np.float64)
    v_arr = v_arr @ rot.T + np.array([cx, cy, z_base], dtype=np.float64)

    faces = []
    face_meta = []  # (alt_frac, gully_weight, is_top_surface)

    # Bottom cap
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([0, 1 + sn, 1 + s_i])
        face_meta.append((0.0, 0.0, 0.0))

    # Side chiseled facets across rings
    for r_idx in range(rings - 1):
        b0 = 1 + r_idx * segs
        b1 = 1 + (r_idx + 1) * segs
        af = 0.5 * (ring_fracs[r_idx] + ring_fracs[r_idx + 1])
        for s_i in range(segs):
            sn = (s_i + 1) % segs
            a = b0 + s_i
            b = b0 + sn
            c_v = b1 + sn
            d = b1 + s_i
            # Triangulate along the ridge/gully diagonal to preserve crisp arête lines
            if (s_i % 2) == 0:
                faces.append([a, b, c_v])
                faces.append([a, c_v, d])
                # First triangle touches gully station `sn` twice (`b, c_v`)
                face_meta.append((af, 0.75, 1.0))
                face_meta.append((af, 0.25, 1.0))
            else:
                faces.append([a, b, d])
                faces.append([b, c_v, d])
                # First triangle touches gully station `s_i` twice (`a, d`)
                face_meta.append((af, 0.75, 1.0))
                face_meta.append((af, 0.25, 1.0))

    # Summit pyramid fan
    b_last = 1 + (rings - 1) * segs
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([b_last + s_i, b_last + sn, top_idx])
        face_meta.append((0.94, 0.55, 1.0))

    f_arr = np.asarray(faces, dtype=np.int32)
    center = np.array([cx, cy, z_base + 0.35 * height], dtype=np.float64)
    tri0 = v_arr[f_arr]
    norms0 = np.cross(tri0[:, 1] - tri0[:, 0], tri0[:, 2] - tri0[:, 0])
    centers0 = tri0.mean(axis=1)
    flip_mask = np.einsum("ij,ij->i", norms0, centers0 - center[None, :]) < 0.0
    if np.any(flip_mask):
        f_arr[flip_mask, 1], f_arr[flip_mask, 2] = f_arr[flip_mask, 2].copy(), f_arr[flip_mask, 1].copy()

    tri = v_arr[f_arr]
    norms = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    n_len = np.linalg.norm(norms, axis=1) + 1e-12
    n_unit = norms / n_len[:, None]

    meta_arr = np.asarray(face_meta, dtype=np.float32)
    af_arr = meta_arr[:, 0]
    gul_arr = meta_arr[:, 1]
    top_mask = meta_arr[:, 2] > 0.5

    # Strong directional Alpine lighting: sunlit warm granite on right/south-east faces (`n_x > 0`),
    # deep slate-blue shadow on left/north-west faces (`n_x < 0`), snow streaks in gullies & summit crests
    east_sun = np.clip(0.74 * n_unit[:, 0] - 0.28 * n_unit[:, 1] + 0.18 * n_unit[:, 2], -1.0, 1.0)
    u_mid = np.where(
        east_sun >= 0.0,
        0.58 + 0.36 * (np.maximum(0.0, east_sun) ** 0.75),
        0.42 - 0.36 * (np.maximum(0.0, -east_sun) ** 0.75),
    )

    snow_score = af_arr * 0.72 + gul_arr * 0.38 * (af_arr ** 0.65) + 0.14 * np.maximum(0.0, n_unit[:, 2])
    is_snow = top_mask & (snow_score >= snow_threshold)

    v_mid = np.where(
        ~top_mask,
        0.12,
        np.where(
            is_snow,
            0.72 + 0.23 * np.clip((snow_score - snow_threshold) / 0.45, 0.0, 1.0),
            0.08 + 0.30 * np.clip(af_arr, 0.0, 1.0),
        ),
    )

    u_tri = np.stack([np.clip(u_mid - 0.009, 0.04, 0.96), np.clip(u_mid + 0.009, 0.04, 0.96), u_mid], axis=-1)
    v_tri = np.stack([np.clip(v_mid - 0.009, 0.04, 0.96), np.clip(v_mid + 0.009, 0.04, 0.96), v_mid], axis=-1)
    uv_tri = strip_uv(4, u_tri.reshape(-1), v_tri.reshape(-1)).reshape(-1, 3, 2)

    g.add_closed_component(
        name,
        v_arr,
        f_arr,
        strip_idx=4,
        per_face_uv=uv_tri,
    )


def _add_stepped_cliff_block(
    g: ClosedAlpineGeometry,
    name: str,
    cx: float,
    cy: float,
    z_bot: float,
    sx: float,
    sy: float,
    sz: float,
    yaw: float,
    seed: float,
    strip_idx: int = 0,
    u_range=(0.08, 0.48),
    v_range=(0.14, 0.82),
):
    """Builds a steep, flat-topped, vertical-ribbed sedimentary/granite cliff block (`khối vách đá phân tầng`).

    Used to construct the tiered rocky highland hills standing in front of the mountains so trees can grow
    directly on top of its flat/stepped upper terrace!
    """
    segs = 10
    rings = 3
    ring_zs = [0.0, 0.15 * sz, 0.88 * sz, sz]
    ring_tapers = [1.02, 1.00, 0.90, 0.76]

    verts = [[0.0, 0.0, 0.0]]
    for r_i in range(rings + 1):
        z_cur = ring_zs[r_i]
        tap = ring_tapers[r_i]
        for s_i in range(segs):
            th = s_i * (2.0 * math.pi / segs)
            # Crisp vertical rock ribs along the cliff wall
            rib = 1.0 + 0.12 * math.cos(th * 4.0 + seed) + 0.08 * math.sin(th * 3.0 - seed * 1.3)
            lx = 0.5 * sx * tap * rib * math.cos(th)
            ly = 0.5 * sy * tap * rib * math.sin(th)
            verts.append([lx, ly, z_cur])
    top_idx = len(verts)
    verts.append([0.0, 0.0, sz])

    v_arr = np.asarray(verts, dtype=np.float64)
    c, s = math.cos(yaw), math.sin(yaw)
    rot = np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]], dtype=np.float64)
    v_arr = v_arr @ rot.T + np.array([cx, cy, z_bot], dtype=np.float64)

    faces = []
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([0, 1 + sn, 1 + s_i])
    for r_i in range(rings):
        b0 = 1 + r_i * segs
        b1 = 1 + (r_i + 1) * segs
        for s_i in range(segs):
            sn = (s_i + 1) % segs
            faces.append([b0 + s_i, b0 + sn, b1 + sn])
            faces.append([b0 + s_i, b1 + sn, b1 + s_i])
    b_top = 1 + rings * segs
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([b_top + s_i, b_top + sn, top_idx])

    center = np.array([cx, cy, z_bot + 0.5 * sz], dtype=np.float64)
    g.add_closed_component(
        name,
        v_arr,
        np.asarray(faces, dtype=np.int32),
        strip_idx=strip_idx,
        u_range=u_range,
        v_range=v_range,
        enforce_outward_center=center,
    )


def _build_multi_layered_alpine_ranges(g: ClosedAlpineGeometry):
    """Builds the 3-Layer Receding Cluster of Alpine Mountain Ranges (`tập hợp các dãy núi xếp lớp xa - trung - gần`)."""
    # -------------------------------------------------------------------------
    # LAYER 1: DISTANT HORIZON RANGE (`y in [17.4 .. 20.2]` — 7 distant peaks peeking through passes)
    # -------------------------------------------------------------------------
    distant_peaks = [
        # (cx, cy, rx, ry, height, yaw, num_ridges, lean_x, snow_thresh)
        (-20.4, 17.6, 3.8, 2.8,  8.8,  0.12, 7,  0.10, 0.48),
        (-16.8, 18.4, 4.4, 3.1, 11.4, -0.08, 8,  0.14, 0.44),
        (-12.6, 18.6, 4.2, 3.0, 10.8,  0.15, 8, -0.10, 0.45),
        ( -8.2, 18.2, 3.9, 2.9,  9.2, -0.14, 7,  0.08, 0.48),
        ( -3.4, 18.4, 4.2, 3.0,  9.8,  0.10, 8,  0.12, 0.46),
        (  1.2, 18.0, 3.8, 2.8,  8.6, -0.12, 7, -0.08, 0.48),
        (  5.2, 17.5, 3.4, 2.6,  7.4,  0.08, 7,  0.06, 0.52),
    ]
    for idx, (cx, cy, rx, ry, h, yaw, nr, lx, st) in enumerate(distant_peaks):
        zb = float(sample_ground_z(cx, min(cy, 14.0))) - 0.35
        _add_chiseled_alpine_peak_massif(
            g,
            f"alpine_range_distant_{idx}",
            cx=cx,
            cy=cy,
            z_base=zb,
            rx=rx,
            ry=ry,
            height=h,
            yaw=yaw,
            num_ridges=nr,
            ridge_sharpness=0.30,
            summit_lean_x=lx,
            summit_lean_y=0.10,
            snow_threshold=st,
            seed=100.0 + idx * 7.3,
        )

    # -------------------------------------------------------------------------
    # LAYER 2: MIDGROUND MAIN CORDILLERA (`y in [14.8 .. 17.4]` — Grand Twin Massif on Left-Center + Central-East Range)
    # -------------------------------------------------------------------------
    midground_peaks = [
        # West flank stepping up to the Grand Twin-Peak Massif
        (-22.6, 14.8, 3.2, 2.5,  6.2,  0.22, 6,  0.14, 0.56),
        (-19.2, 15.6, 3.9, 2.9,  8.8,  0.14, 7,  0.16, 0.50),
        # GRAND WESTERN TWIN-PEAK MASSIF (Dominant highest twin summits in Target Images 1 & 2)
        (-15.0, 16.4, 4.8, 3.6, 12.2,  0.06, 8,  0.15, 0.44),
        (-13.0, 16.0, 3.5, 2.7,  9.4, -0.05, 7,  0.04, 0.48),
        (-10.8, 16.1, 4.5, 3.4, 11.0, -0.12, 8, -0.12, 0.45),
        # High Pass Crag between West Massif and Central Range
        ( -7.8, 15.6, 3.4, 2.6,  7.4,  0.18, 7,  0.08, 0.52),
        # CENTRAL-EAST TWIN MASSIF CLUSTER
        ( -5.0, 16.0, 4.2, 3.2,  9.6,  0.08, 8,  0.12, 0.47),
        ( -1.2, 16.1, 4.4, 3.3, 10.0, -0.10, 8, -0.10, 0.46),
        (  2.6, 15.6, 3.8, 2.9,  8.4,  0.12, 7,  0.10, 0.50),
        (  5.8, 15.0, 3.3, 2.5,  6.8, -0.15, 7, -0.08, 0.54),
    ]
    for idx, (cx, cy, rx, ry, h, yaw, nr, lx, st) in enumerate(midground_peaks):
        zb = float(sample_ground_z(cx, min(cy, 14.0))) - 0.30
        _add_chiseled_alpine_peak_massif(
            g,
            f"alpine_range_midground_{idx}",
            cx=cx,
            cy=cy,
            z_base=zb,
            rx=rx,
            ry=ry,
            height=h,
            yaw=yaw,
            num_ridges=nr,
            ridge_sharpness=0.32,
            summit_lean_x=lx,
            summit_lean_y=0.06,
            snow_threshold=st,
            seed=200.0 + idx * 11.1,
        )

    # -------------------------------------------------------------------------
    # LAYER 3: FOREGROUND ROCKY SPURS & ARÊTE RIDGES (`y in [12.8 .. 14.8]`)
    # Sweeping down from the main peaks into the highland forest & cliff terraces
    # -------------------------------------------------------------------------
    foreground_spurs = [
        (-20.8, 13.4, 2.8, 2.4, 4.8,  0.30, 6,  0.18, 0.62),
        (-17.4, 14.0, 3.2, 2.7, 6.4,  0.22, 7,  0.15, 0.56),
        (-14.2, 14.2, 3.4, 2.8, 7.0, -0.18, 7, -0.14, 0.55),
        (-10.4, 13.9, 3.1, 2.6, 6.2,  0.24, 7,  0.12, 0.56),
        ( -6.8, 13.6, 2.8, 2.3, 5.0, -0.20, 6, -0.10, 0.60),
        ( -3.4, 13.8, 3.1, 2.6, 5.8,  0.16, 7,  0.12, 0.58),
        (  0.4, 13.7, 3.0, 2.5, 5.6, -0.22, 7, -0.12, 0.58),
        (  4.0, 13.4, 2.7, 2.2, 4.8,  0.18, 6,  0.10, 0.62),
    ]
    for idx, (cx, cy, rx, ry, h, yaw, nr, lx, st) in enumerate(foreground_spurs):
        zb = float(sample_ground_z(cx, min(cy, 13.8))) - 0.25
        _add_chiseled_alpine_peak_massif(
            g,
            f"alpine_range_foreground_spur_{idx}",
            cx=cx,
            cy=cy,
            z_base=zb,
            rx=rx,
            ry=ry,
            height=h,
            yaw=yaw,
            num_ridges=nr,
            ridge_sharpness=0.29,
            summit_lean_x=lx,
            summit_lean_y=-0.12,
            snow_threshold=st,
            seed=300.0 + idx * 9.7,
        )


def _add_closed_frustum(
    g: ClosedAlpineGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    r_bot: float,
    r_top: float,
    height: float,
    segs: int,
    strip_idx: int,
    u_range,
    v_range,
    wobble_amp: float = 0.0,
    seed: float = 0.0,
):
    verts = [[cx, cy, z0]]
    for ring_i, (r_val, z_val) in enumerate([(r_bot, z0), (r_top, z0 + height)]):
        for s_i in range(segs):
            th = s_i * (2.0 * math.pi / segs)
            wob = 1.0 + wobble_amp * math.sin(th * 3.0 + seed + ring_i * 1.4) if wobble_amp > 0.0 else 1.0
            verts.append([cx + r_val * wob * math.cos(th), cy + r_val * wob * math.sin(th), z_val])
    top_center = len(verts)
    verts.append([cx, cy, z0 + height])

    faces = []
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([0, 1 + sn, 1 + s_i])
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        a = 1 + s_i
        b = 1 + sn
        c = 1 + segs + sn
        d = 1 + segs + s_i
        faces.append([a, b, c])
        faces.append([a, c, d])
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([1 + segs + s_i, 1 + segs + sn, top_center])

    center = np.array([cx, cy, z0 + 0.5 * height], dtype=np.float64)
    g.add_closed_component(
        name,
        np.asarray(verts, dtype=np.float64),
        np.asarray(faces, dtype=np.int32),
        strip_idx=strip_idx,
        u_range=u_range,
        v_range=v_range,
        enforce_outward_center=center,
    )


def _add_closed_foliage_polyhedron(
    g: ClosedAlpineGeometry,
    name: str,
    cx: float,
    cy: float,
    cz: float,
    rx: float,
    ry: float,
    rz: float,
    segs: int,
    rings: int,
    strip_idx: int,
    u_range,
    v_range,
    seed: float,
):
    verts = [[cx, cy, cz - rz]]
    for r_i in range(1, rings):
        vf = r_i / float(rings)
        phi = (vf - 0.5) * math.pi
        cp, sp = math.cos(phi), math.sin(phi)
        for s_i in range(segs):
            th = s_i * (2.0 * math.pi / segs) + 0.22 * (r_i % 2)
            wob = 1.0 + 0.14 * math.sin(th * 3.0 + phi * 2.0 + seed) * math.cos(th * 2.0 - phi * 3.0)
            verts.append([cx + rx * cp * math.cos(th) * wob, cy + ry * cp * math.sin(th) * wob, cz + rz * sp * wob])
    top_idx = len(verts)
    verts.append([cx, cy, cz + rz])

    faces = []
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([0, 1 + sn, 1 + s_i])
    for r_i in range(rings - 2):
        b0 = 1 + r_i * segs
        b1 = 1 + (r_i + 1) * segs
        for s_i in range(segs):
            sn = (s_i + 1) % segs
            faces.append([b0 + s_i, b0 + sn, b1 + sn])
            faces.append([b0 + s_i, b1 + sn, b1 + s_i])
    b_last = 1 + (rings - 2) * segs
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append([b_last + s_i, b_last + sn, top_idx])

    center = np.array([cx, cy, cz], dtype=np.float64)
    g.add_closed_component(
        name,
        np.asarray(verts, dtype=np.float64),
        np.asarray(faces, dtype=np.int32),
        strip_idx=strip_idx,
        u_range=u_range,
        v_range=v_range,
        enforce_outward_center=center,
    )


def _add_layered_autumn_oak_tree(
    g: ClosedAlpineGeometry,
    tx: float,
    ty: float,
    tz: float,
    scale: float,
    palette_idx: int,
    seed: float,
):
    """Multi-cluster faceted autumn oak tree using exact Strip 3 sub-palettes (p1=Golden, p2=Amber-Orange, p3=Olive-Gold)."""
    _add_closed_frustum(
        g,
        "oak_trunk",
        tx,
        ty,
        tz - 0.12,
        0.19 * scale,
        0.11 * scale,
        1.18 * scale,
        7,
        strip_idx=1,
        u_range=(0.72, 0.95),
        v_range=(0.12, 0.85),
    )
    if palette_idx == 1:
        u_rng = (0.27, 0.48)   # Strip 3 p1: Pure Golden Oak
    elif palette_idx == 2:
        u_rng = (0.52, 0.73)   # Strip 3 p2: Pure Amber-Russet Maple
    else:
        u_rng = (0.77, 0.97)   # Strip 3 p3: Pure Olive-Gold

    puffs = [
        ( 0.00,  0.00, 1.48, 0.66, 0.62, 0.52),
        (-0.38, -0.24, 1.24, 0.52, 0.48, 0.42),
        ( 0.40, -0.22, 1.26, 0.50, 0.48, 0.42),
        (-0.34,  0.32, 1.28, 0.52, 0.50, 0.42),
        ( 0.36,  0.30, 1.26, 0.50, 0.48, 0.42),
        ( 0.00, -0.42, 1.18, 0.48, 0.46, 0.38),
        (-0.22, -0.12, 1.78, 0.50, 0.48, 0.40),
        ( 0.24,  0.14, 1.76, 0.48, 0.46, 0.40),
        ( 0.00,  0.04, 2.04, 0.44, 0.42, 0.36),
    ]
    c, s = math.cos(seed), math.sin(seed)
    for idx_p, (dx, dy, dz, rx, ry, rz) in enumerate(puffs):
        rx_w = (dx * c - dy * s) * scale
        ry_w = (dx * s + dy * c) * scale
        _add_closed_foliage_polyhedron(
            g,
            "oak_canopy_cluster",
            tx + rx_w,
            ty + ry_w,
            tz + dz * scale,
            rx * scale,
            ry * scale,
            rz * scale,
            segs=9,
            rings=5,
            strip_idx=3,
            u_range=u_rng,
            v_range=(0.22, 0.95),
            seed=seed + idx_p * 1.7,
        )


def _add_layered_spruce_pine(
    g: ClosedAlpineGeometry,
    tx: float,
    ty: float,
    tz: float,
    scale: float,
    tiers: int,
    seed: float,
):
    """Multi-tier highland spruce pine using Strip 3 p0 (`u in [0.03, 0.23]`: pure Emerald Spruce)."""
    _add_closed_frustum(
        g,
        "pine_trunk",
        tx,
        ty,
        tz - 0.12,
        0.14 * scale,
        0.08 * scale,
        0.82 * scale,
        6,
        strip_idx=1,
        u_range=(0.72, 0.95),
        v_range=(0.12, 0.85),
    )
    for t_i in range(tiers):
        tf = t_i / float(max(1, tiers - 1))
        z0 = tz + (0.48 + tf * 1.68) * scale
        r_b = (0.98 - tf * 0.66) * scale
        r_t = 0.04 * scale if t_i == tiers - 1 else (0.26 - tf * 0.18) * scale
        h_t = (0.64 - tf * 0.20) * scale
        _add_closed_frustum(
            g,
            "pine_tier",
            tx,
            ty,
            z0,
            r_b,
            r_t,
            h_t,
            10,
            strip_idx=3,
            u_range=(0.03, 0.22),
            v_range=(0.18, 0.94),
            wobble_amp=0.14,
            seed=seed + t_i * 1.1,
        )


def _build_tiered_highland_outcrops_and_cascading_forest(g: ClosedAlpineGeometry):
    """Builds the authentic 'area standing in front of the mountains' (`phần đứng trước núi`) from Images 1 & 2:

    1. **North-West Tiered Rocky Highland Hill (`Gò Đồi Đá Phân Tầng Góc Tây-Bắc`, `x in [-23.2..-10.8], y in [8.8..13.5]`)**:
       - 3 stepped tiers of vertical-ribbed sandstone/granite cliff ledges rising above the Farmstead,
       - Crowned with dense, cascading golden-yellow and amber autumn oak trees & pines growing directly on top of
         and down the stepped cliff terraces (matching `crop1_zone10_mountains_and_left_highland.png`!).
    2. **Central-East Tiered Rocky Bluff (`Đồi Đá Phân Tầng Trung-Đông`, `x in [-7.2..+5.6], y in [10.4..13.6]`)**:
       - Stepped rock ledges with golden oaks & pines framing the elevated rear NPC Village (`Zone06`) and valley.
    """
    # (cx, cy, sx, sy, height_above_ground, yaw, strip_idx, tree_on_top_type, tree_scale, tree_palette)
    # tree_on_top_type: 0=none, 1=golden_oak, 2=spruce_pine, 3=both
    highland_cliff_terraces = [
        # --- NORTH-WEST HIGHLAND BUTTE: TIER 1 (Lower outer stepped cliff rim) ---
        (-22.4,  9.4, 2.6, 2.2, 1.45,  0.18, 0, 2, 1.12, 1),
        (-20.4,  9.2, 2.8, 2.3, 1.65, -0.15, 0, 1, 1.18, 1),
        (-18.4, 10.4, 2.9, 2.4, 1.80,  0.12, 0, 1, 1.22, 2),
        (-14.6, 10.6, 2.8, 2.3, 1.75, -0.20, 0, 1, 1.20, 1),
        (-12.2, 10.2, 2.6, 2.2, 1.55,  0.24, 0, 1, 1.15, 1),
        (-10.6, 10.8, 2.5, 2.1, 1.50, -0.18, 0, 2, 1.14, 1),

        # --- NORTH-WEST HIGHLAND BUTTE: TIER 2 (Mid-level stepped sandstone/granite cliff ledges) ---
        (-21.2, 11.2, 3.0, 2.5, 2.35,  0.22, 0, 3, 1.24, 1),
        (-18.8, 11.4, 3.2, 2.6, 2.65, -0.14, 4, 1, 1.28, 1),
        (-16.4, 11.0, 3.4, 2.7, 2.85,  0.08, 0, 1, 1.32, 2),
        (-14.0, 11.4, 3.1, 2.5, 2.55, -0.16, 4, 1, 1.25, 1),
        (-11.8, 11.6, 2.8, 2.4, 2.25,  0.20, 0, 3, 1.20, 2),

        # --- NORTH-WEST HIGHLAND BUTTE: TIER 3 CROWN CRAG (High vertical-ribbed cliff summit with iconic Golden Oak crown!) ---
        (-17.6, 12.2, 3.2, 2.6, 3.45,  0.15, 4, 1, 1.38, 1),
        (-15.6, 12.0, 3.5, 2.8, 3.80, -0.08, 0, 3, 1.42, 1),
        (-13.6, 12.3, 3.0, 2.5, 3.25,  0.18, 4, 1, 1.30, 2),

        # --- CENTRAL-NORTH ROCKY PASS OUTCROPS (Framing the trail & forest between West Massif and Central Bluff) ---
        ( -9.2, 11.2, 2.6, 2.1, 1.85, -0.22, 4, 1, 1.22, 1),
        ( -7.4, 11.6, 2.8, 2.3, 2.15,  0.16, 0, 3, 1.24, 2),
        ( -5.4, 11.2, 2.7, 2.2, 1.95, -0.14, 0, 1, 1.20, 1),

        # --- NORTH-EAST / CENTRAL-EAST STEPPED ROCKY BLUFF (In front of Central-East Mountains & Waterfall Cliff) ---
        ( -3.2, 12.2, 3.0, 2.4, 2.35,  0.12, 0, 3, 1.22, 1),
        ( -0.8, 12.4, 3.2, 2.5, 2.55, -0.18, 4, 1, 1.26, 2),
        (  1.8, 12.2, 3.0, 2.4, 2.40,  0.20, 0, 3, 1.24, 1),
        (  4.2, 11.8, 2.8, 2.3, 2.10, -0.15, 0, 1, 1.18, 1),
    ]

    keepout_ledges = []
    for idx, (cx, cy, sx, sy, h_above, yaw, s_idx, t_type, t_sc, t_pal) in enumerate(highland_cliff_terraces):
        gz = float(sample_ground_z(cx, cy))
        z_bot = gz - 0.28
        sz = h_above + 0.28
        u_rng = (0.08, 0.48) if s_idx == 0 else (0.48, 0.88)
        v_rng = (0.14, 0.84) if s_idx == 0 else (0.10, 0.42)
        _add_stepped_cliff_block(
            g,
            f"highland_cliff_terrace_{idx}",
            cx=cx,
            cy=cy,
            z_bot=z_bot,
            sx=sx,
            sy=sy,
            sz=sz,
            yaw=yaw,
            seed=idx * 4.3 + 1.2,
            strip_idx=s_idx,
            u_range=u_rng,
            v_range=v_rng,
        )
        # Secondary stepped sub-ledge at the front/side of each terrace block
        sub_ang = yaw - 0.65
        sub_x = cx + 0.42 * sx * math.cos(sub_ang)
        sub_y = cy - 0.36 * sy * math.sin(sub_ang)
        sub_gz = float(sample_ground_z(sub_x, sub_y))
        _add_stepped_cliff_block(
            g,
            f"highland_subledge_{idx}",
            cx=sub_x,
            cy=sub_y,
            z_bot=sub_gz - 0.24,
            sx=sx * 0.68,
            sy=sy * 0.65,
            sz=h_above * 0.62 + 0.24,
            yaw=yaw + 0.45,
            seed=idx * 6.1 + 2.7,
            strip_idx=0,
            u_range=(0.10, 0.46),
            v_range=(0.16, 0.82),
        )

        # Plant cascading golden-autumn oaks & pines directly on top of the rocky terrace summit!
        z_top = gz + h_above - 0.08
        keepout_ledges.append((cx, cy, max(sx, sy) * 0.48))
        if t_type in (1, 3):
            _add_layered_autumn_oak_tree(
                g,
                tx=cx - 0.18 * math.cos(yaw),
                ty=cy + 0.12 * math.sin(yaw),
                tz=z_top,
                scale=t_sc,
                palette_idx=t_pal,
                seed=50.0 + idx * 3.1,
            )
        if t_type in (2, 3):
            off_x = 0.55 * math.cos(yaw + 0.8) if t_type == 3 else 0.0
            off_y = 0.55 * math.sin(yaw + 0.8) if t_type == 3 else 0.0
            _add_layered_spruce_pine(
                g,
                tx=cx + off_x,
                ty=cy + off_y,
                tz=z_top - (0.12 if t_type == 3 else 0.0),
                scale=t_sc * 0.94,
                tiers=6,
                seed=80.0 + idx * 2.9,
            )

    # Dense surrounding golden-autumn oak & alpine pine forest belt along the northern slopes
    building_keepouts = [
        (-17.8, 5.4, 2.8),
        (-17.0, 9.2, 3.1),
        (-13.4, 9.4, 2.9),
        (-7.8, 6.4, 3.3),
        (-2.5, 7.2, 2.8),   # Enhancement Sanctum keepout
        (-0.8, 11.4, 2.5),  # Elevated Rear NPC Village keepout 1
        ( 2.2, 11.2, 2.5),  # Elevated Rear NPC Village keepout 2
        ( 0.8, 12.8, 2.5),  # Elevated Rear NPC Village keepout 3
        ( 8.6, 4.6, 2.5),
    ] + keepout_ledges

    placed_trees = []

    def can_place_tree(tx: float, ty: float, min_d: float = 1.06) -> bool:
        if float(island_signed_distance(tx, ty)) > 0.93:
            return False
        if float(eval_road_mask(tx, ty)) > 0.25:
            return False
        rcx, rhw = eval_river_center_and_width(ty)
        if ty < 12.5 and ty > -7.2 and abs(tx - float(rcx)) < float(rhw) + 1.05:
            return False
        for bx, by, br in building_keepouts:
            if math.hypot(tx - bx, ty - by) < br:
                return False
        for px, py in placed_trees:
            if math.hypot(tx - px, ty - py) < min_d:
                return False
        return True

    rng = np.random.RandomState(42)
    for idx_t in range(920):
        tx = float(rng.uniform(-24.8, 7.8))
        ty = float(rng.uniform(9.85, 13.95))
        if not can_place_tree(tx, ty, min_d=1.06):
            continue
        placed_trees.append((tx, ty))
        tz = float(sample_ground_z(tx, ty))
        # More golden-yellow and amber autumn oaks on the left/center slopes, pines along the upper ridge
        pine_prob = 0.52 if ty > 12.4 else 0.24
        if rng.rand() < pine_prob:
            sc = float(rng.uniform(0.90, 1.30))
            _add_layered_spruce_pine(g, tx, ty, tz, scale=sc, tiers=6, seed=idx_t * 0.7)
        else:
            sc = float(rng.uniform(0.98, 1.36))
            pal = 1 if rng.rand() < 0.56 else (2 if rng.rand() < 0.82 else 3)
            _add_layered_autumn_oak_tree(g, tx, ty, tz, scale=sc, palette_idx=pal, seed=idx_t * 1.1)


def build_alpine_massif_only() -> ClosedAlpineGeometry:
    """Returns ONLY the 3-layer cluster of chiseled Alpine mountain ranges (`25` closed massif parts)."""
    g = ClosedAlpineGeometry()
    _build_multi_layered_alpine_ranges(g)
    return g


def build_alpine_zone() -> ClosedAlpineGeometry:
    """Builds the complete `Zone10` (3-layer Alpine mountain ranges + tiered rocky highland hills & cascading golden forest)."""
    g = ClosedAlpineGeometry()
    _build_multi_layered_alpine_ranges(g)
    _build_tiered_highland_outcrops_and_cascading_forest(g)
    return g
