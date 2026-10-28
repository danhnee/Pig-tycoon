"""Expanded 120x90m DCC layout scaffold and gated art candidate builder.

Only one central pasture pond. Building geometry is kept at authored size (scale 1,1,1);
spatial separation is native metres, not a finished-map scale transform. Never writes live Assets.
Full-world groundcover/forest/art renders are strictly gated until Root explicitly approves
the new short golden sward mini-view.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.collab/artifacts/mountain-reference-study-codex/runtime'))
sys.path.insert(0, str(ROOT / 'Tools'))
import bpy
from mathutils import Vector
from build_reference_world_art_master import configure_cuda, lighting, point_camera, mesh_object
import world_expanded_spatial_layout as L
from world_expanded_water import (
    configure_water_layout,
    build_world_expanded_water,
    shore_wetness,
    water_surface_height,
)

OUT = ROOT / '.collab/artifacts/expanded-gold-world-sol-high'
SOURCE = ROOT / 'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/Farm_Main_3D_Zone05_Candidate.blend'
REUSE = ROOT / '.collab/artifacts/reference-world-master-sol-high/reference-world-art-candidate.blend'
PARTITIONS_PATH = ROOT / '.collab/artifacts/world-expanded-layout-sol-high/source-partitions.json'


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def extract_observatory_local_signature(obs_obj) -> dict:
    """Computes real SHA-256 digests of local mesh vertices, polygon topology/materials, and UVs."""
    mesh = obs_obj.data
    co = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get('co', co)
    loops = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get('vertex_index', loops)
    p_start = np.empty(len(mesh.polygons), dtype=np.int32)
    mesh.polygons.foreach_get('loop_start', p_start)
    p_tot = np.empty(len(mesh.polygons), dtype=np.int32)
    mesh.polygons.foreach_get('loop_total', p_tot)
    p_mat = np.empty(len(mesh.polygons), dtype=np.int32)
    mesh.polygons.foreach_get('material_index', p_mat)
    uv = np.empty(len(mesh.loops) * 2, dtype=np.float32)
    if mesh.uv_layers:
        mesh.uv_layers[0].data.foreach_get('uv', uv)
    else:
        uv.fill(0.0)
    co_pts = co.reshape(-1, 3)
    lo = co_pts.min(axis=0).tolist()
    hi = co_pts.max(axis=0).tolist()
    return {
        'object_name': obs_obj.name,
        'mesh_name': mesh.name,
        'vertex_count': int(len(mesh.vertices)),
        'polygon_count': int(len(mesh.polygons)),
        'loop_count': int(len(mesh.loops)),
        'local_co_sha256': hashlib.sha256(co.tobytes()).hexdigest(),
        'local_topology_sha256': hashlib.sha256(
            loops.tobytes() + p_start.tobytes() + p_tot.tobytes() + p_mat.tobytes()
        ).hexdigest(),
        'local_uv_sha256': hashlib.sha256(uv.tobytes()).hexdigest(),
        'local_bbox_min': [round(float(x), 6) for x in lo],
        'local_bbox_max': [round(float(x), 6) for x in hi],
        'local_dimensions_m': [round(float(hi[i] - lo[i]), 6) for i in range(3)],
        'object_location': [round(float(x), 6) for x in obs_obj.location],
        'object_scale': [round(float(x), 6) for x in obs_obj.scale],
        'object_rotation_euler': [round(float(x), 6) for x in obs_obj.rotation_euler],
    }


def read_protected_source_observatory_signature() -> dict:
    """Reads the protected SOURCE blend read-only to capture the authoritative Observatory signature."""
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE), load_ui=False, use_scripts=False)
    obs = next(
        ob for ob in bpy.data.objects
        if ob.type == 'MESH' and ob.name.startswith('Zone05')
    )
    return extract_observatory_local_signature(obs)


def material(name: str, colour: tuple[float, float, float], roughness: float = 0.8):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*colour, 1.0)
    shader.inputs['Roughness'].default_value = roughness
    return mat


def living_ground():
    mat = material('Expanded_Living_Gold_Ground_ONLY', (0.48, 0.29, 0.035), 0.93)
    mat['surface_role'] = 'living_ground'
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    shader = nodes.get('Principled BSDF')
    geo = nodes.new('ShaderNodeNewGeometry')
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 0.31
    noise.inputs['Detail'].default_value = 3.0
    links.new(geo.outputs['Position'], noise.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = (0.28, 0.19, 0.025, 1.0)
    ramp.color_ramp.elements[0].position = 0.16
    ramp.color_ramp.elements[1].color = (0.68, 0.43, 0.055, 1.0)
    ramp.color_ramp.elements[1].position = 0.82
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    trail = nodes.new('ShaderNodeAttribute')
    trail.attribute_name = 'Expanded_Trail'
    mix = nodes.new('ShaderNodeMixRGB')
    mix.inputs[2].default_value = (0.30, 0.17, 0.045, 1.0)
    links.new(trail.outputs['Fac'], mix.inputs[0])
    links.new(ramp.outputs['Color'], mix.inputs[1])
    wet = nodes.new('ShaderNodeAttribute')
    wet.attribute_name = 'Expanded_WetBank'
    shore = nodes.new('ShaderNodeMixRGB')
    shore.inputs[2].default_value = (0.10, 0.105, 0.035, 1.0)
    links.new(wet.outputs['Fac'], shore.inputs[0])
    links.new(mix.outputs[0], shore.inputs[1])
    links.new(shore.outputs[0], shader.inputs['Base Color'])
    fine = nodes.new('ShaderNodeTexNoise')
    fine.inputs['Scale'].default_value = 28.0
    links.new(geo.outputs['Position'], fine.inputs['Vector'])
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.20
    bump.inputs['Distance'].default_value = 0.007
    links.new(fine.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], shader.inputs['Normal'])
    return mat


def weathered_bedrock_material():
    rock = material('Expanded_Weathered_Warm_Bedrock_ONLY', (0.27, 0.23, 0.15), 0.89)
    rock['surface_role'] = 'bedrock_cutaway'
    nodes = rock.node_tree.nodes
    links = rock.node_tree.links
    s = nodes.get('Principled BSDF')
    geo = nodes.new('ShaderNodeNewGeometry')
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 1.8
    noise.inputs['Detail'].default_value = 3.0
    links.new(geo.outputs['Position'], noise.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = (0.14, 0.13, 0.10, 1.0)
    ramp.color_ramp.elements[1].color = (0.42, 0.34, 0.22, 1.0)
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], s.inputs['Base Color'])
    return rock


def terrain(collection):
    x = np.linspace(-60.0, 60.0, 601)
    y = np.linspace(-45.0, 45.0, 451)
    xx, yy = np.meshgrid(x, y)
    zz = np.asarray(L.sample_ground_z(xx, yy), dtype=np.float32)
    vertices = np.stack((xx, yy, zz), axis=-1).reshape(-1, 3).astype(np.float32)
    a = np.arange(len(y) * len(x)).reshape(len(y), len(x))[:-1, :-1]
    cx = (xx[:-1, :-1] + xx[1:, 1:]) / 2.0
    cy = (yy[:-1, :-1] + yy[1:, 1:]) / 2.0
    keep = (np.abs(cx / 60.0) ** 4 + np.abs(cy / 45.0) ** 4) <= 1.0
    a = a[keep]
    b = a + 1
    c = b + len(x)
    d = a + len(x)
    faces = np.vstack((np.stack((a, b, c), axis=1), np.stack((a, c, d), axis=1))).astype(np.int32)
    mat = living_ground()
    uv = vertices[:, :2] * 0.05
    ob = mesh_object('Expanded_120x90_Living_Terrain', vertices, uv, faces, mat, collection)
    for p in ob.data.polygons:
        p.use_smooth = True
    for name, values in [
        ('Expanded_Trail', np.clip(1.0 - np.asarray(L.nearest_trail(vertices[:, 0], vertices[:, 1])) / 1.1, 0.0, 1.0) * 0.85),
        ('Expanded_WetBank', np.asarray(shore_wetness(vertices[:, 0], vertices[:, 1], L.LAYOUT))),
    ]:
        attribute = ob.data.attributes.new(name, 'FLOAT', 'POINT')
        attribute.data.foreach_set('value', np.asarray(values, dtype=np.float32))

    # Build the cutaway from the terrain's own directed boundary edges with shared
    # per-vertex displacements and outward-wound faces (zero cracks or flipped normals).
    directed = np.vstack((faces[:, [0, 1]], faces[:, [1, 2]], faces[:, [2, 0]]))
    undirected = np.sort(directed, axis=1)
    _, first_idx, counts = np.unique(undirected, axis=0, return_index=True, return_counts=True)
    boundary = directed[first_idx[counts == 1]]

    rim_vertices = []
    rim_faces = []
    vert_row_map: dict[tuple[int, int], int] = {}
    t_rows = (0.0, 0.32, 0.69, 1.0)

    def get_rim_vert(vid: int, row: int, t: float) -> int:
        key = (int(vid), int(row))
        if key in vert_row_map:
            return vert_row_map[key]
        v = vertices[vid].copy()
        radial = np.asarray((v[0], v[1], 0.0), dtype=np.float64)
        radial /= max(float(np.linalg.norm(radial)), 0.001)
        det_jitter = 0.038 * math.sin(float(v[0]) * 0.83 + float(v[1]) * 1.17 + row * 1.3)
        rock_pt = v.astype(np.float64) + radial * ((0.10 + det_jitter) * math.sin(t * math.pi))
        rock_pt[2] = float(v[2]) * (1.0 - t) + (-3.3 + 0.18 * math.sin(float(v[0]) * 0.47 + float(v[1]) * 0.63)) * t
        idx_new = len(rim_vertices)
        rim_vertices.append(rock_pt.astype(np.float32))
        vert_row_map[key] = idx_new
        return idx_new

    for start, end in boundary:
        for row in range(3):
            i00 = get_rim_vert(int(start), row, t_rows[row])
            i01 = get_rim_vert(int(end), row, t_rows[row])
            i10 = get_rim_vert(int(start), row + 1, t_rows[row + 1])
            i11 = get_rim_vert(int(end), row + 1, t_rows[row + 1])
            # Outward winding for CCW boundary edge (start -> end):
            rim_faces.extend(((i00, i10, i11), (i00, i11, i01)))

    rock = weathered_bedrock_material()
    vv = np.asarray(rim_vertices, dtype=np.float32)
    rim_ob = mesh_object('Expanded_Natural_Cutaway_Bedrock', vv, vv[:, :2], rim_faces, rock, collection)
    for p in rim_ob.data.polygons:
        p.use_smooth = True

    used_ter_verts = vertices[np.unique(faces)]
    ter_lo = used_ter_verts.min(axis=0).tolist()
    ter_hi = used_ter_verts.max(axis=0).tolist()
    return ob, rim_ob, {
        'envelope_m': [120.0, 90.0],
        'grid_spacing_m': 0.2,
        'triangles': int(len(faces)),
        'cutaway_rim_triangles': int(len(rim_faces)),
        'active_terrain_bbox_min': [round(float(val), 4) for val in ter_lo],
        'active_terrain_bbox_max': [round(float(val), 4) for val in ter_hi],
        'active_terrain_dimensions_m': [round(float(ter_hi[i] - ter_lo[i]), 4) for i in range(3)],
        'pond_count': int(len(L.PONDS)),
        'ground_material_name': mat.name,
        'rock_material_name': rock.name,
        'ground_material_separate_from_rock': bool(mat != rock and list(ob.data.materials) != list(rim_ob.data.materials)),
        'terrain_material_slots': [m.name for m in ob.data.materials],
        'cutaway_material_slots': [m.name for m in rim_ob.data.materials],
    }


def select_partition_triangles(geometry, names=None):
    """Selects exact union of authored part[first_triangle:first_triangle+triangles] spans.

    Never slices by min-to-max triangle index across interleaved parts.
    Returns both raw selected span bounds and nature-filtered mesh arrays.
    """
    from world_groundcover_art_pass import should_exclude_primitive_tree, should_exclude_primitive_groundcover
    v, uv, f = geometry.arrays()
    raw_keep = np.zeros(len(f), dtype=np.bool_)
    filtered_keep = np.zeros(len(f), dtype=np.bool_)
    bad = {
        'flower_gold', 'flower_magenta', 'flower_violet', 'shrub_green',
        'grass_gold', 'lotus_gold', 'pine_green', 'pine_dark',
        'oak_gold', 'oak_amber', 'oak_orange',
    }
    matched_parts = 0
    filtered_parts = 0
    for part in geometry.parts:
        if names is not None and part['name'] not in names:
            continue
        start = int(part['first_triangle'])
        end = start + int(part['triangles'])
        raw_keep[start:end] = True
        matched_parts += 1
        if (
            part.get('material') in bad
            or should_exclude_primitive_tree(part['name'])
            or should_exclude_primitive_groundcover(part['name'])
        ):
            continue
        filtered_keep[start:end] = True
        filtered_parts += 1

    raw_faces = f[raw_keep]
    raw_used = np.unique(raw_faces)
    raw_pts = v[raw_used]
    raw_lo = raw_pts.min(axis=0).tolist() if len(raw_pts) else [0.0, 0.0, 0.0]
    raw_hi = raw_pts.max(axis=0).tolist() if len(raw_pts) else [0.0, 0.0, 0.0]

    filt_faces = f[filtered_keep]
    filt_used = np.unique(filt_faces)
    remap = np.full(len(v), -1, dtype=np.int32)
    remap[filt_used] = np.arange(len(filt_used))
    filt_pts = v[filt_used]
    filt_lo = filt_pts.min(axis=0).tolist() if len(filt_pts) else [0.0, 0.0, 0.0]
    filt_hi = filt_pts.max(axis=0).tolist() if len(filt_pts) else [0.0, 0.0, 0.0]

    span_meta = {
        'selection_method': 'exact union of each authored part[first_triangle:first_triangle+triangles] span; never min-to-max slice',
        'matched_part_records': int(matched_parts),
        'kept_architecture_part_records': int(filtered_parts),
        'raw_selected_triangles': int(len(raw_faces)),
        'filtered_architecture_triangles': int(len(filt_faces)),
        'raw_source_bounds': [[round(float(x), 6) for x in raw_lo], [round(float(x), 6) for x in raw_hi]],
        'raw_source_center': [round(float(0.5 * (raw_lo[i] + raw_hi[i])), 6) for i in range(3)],
        'raw_source_half_extents': [round(float(0.5 * (raw_hi[i] - raw_lo[i])), 6) for i in range(3)],
        'filtered_source_bounds': [[round(float(x), 6) for x in filt_lo], [round(float(x), 6) for x in filt_hi]],
        'filtered_source_center': [round(float(0.5 * (filt_lo[i] + filt_hi[i])), 6) for i in range(3)],
        'filtered_source_half_extents': [round(float(0.5 * (filt_hi[i] - filt_lo[i])), 6) for i in range(3)],
    }
    return (v[filt_used], uv[filt_used], remap[filt_faces]), span_meta


def measure_world_object_bounds(obj) -> dict:
    mesh = obj.data
    co = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get('co', co)
    pts = co.reshape(-1, 3).astype(np.float64)
    loc = np.asarray(obj.location, dtype=np.float64)
    sc = np.asarray(obj.scale, dtype=np.float64)
    world_pts = pts * sc[None, :] + loc[None, :]
    lo = world_pts.min(axis=0).tolist()
    hi = world_pts.max(axis=0).tolist()
    return {
        'object_name': obj.name,
        'vertices': int(len(mesh.vertices)),
        'triangles': int(len(mesh.polygons)),
        'location': [round(float(x), 6) for x in obj.location],
        'scale': [round(float(x), 6) for x in obj.scale],
        'world_bounds': [[round(float(x), 6) for x in lo], [round(float(x), 6) for x in hi]],
        'world_center': [round(float(0.5 * (lo[i] + hi[i])), 6) for i in range(3)],
        'world_dimensions_m': [round(float(hi[i] - lo[i]), 6) for i in range(3)],
    }


def evaluate_bridge_contacts(dx: float, dy: float, dz_applied: float) -> dict:
    """Evaluates exact bridge pier, cutwater, arch, deck, and bank contacts at (28, 13)."""
    target_water_z = float(water_surface_height(28.0, 13.0, L.LAYOUT))
    target_bed_z = float(L.sample_ground_z(28.0, 13.0))
    source_waterline_z = 2.04
    piers_x_source = [9.8 + i * (9.4 / 4.0) for i in range(5)]
    arches_x_source = [9.8 + (i + 0.5) * (9.4 / 4.0) for i in range(4)]
    pier_contacts = []
    for idx, sx in enumerate(piers_x_source):
        wx = sx + dx
        wy = 6.2 + dy
        gz = float(L.sample_ground_z(wx, wy))
        wz = float(water_surface_height(wx, wy, L.LAYOUT))
        footing_bottom = 1.14 + dz_applied
        footing_top = 1.42 + dz_applied
        cutwater_bottom = 1.26 + dz_applied
        cutwater_hood_top = 2.98 + dz_applied
        pier_contacts.append({
            'pier_index': idx,
            'world_xy': [round(wx, 4), round(wy, 4)],
            'ground_or_bed_z': round(gz, 4),
            'water_z': round(wz, 4) if math.isfinite(wz) else None,
            'footing_bottom_z': round(footing_bottom, 4),
            'footing_embed_below_ground_m': round(gz - footing_bottom, 4),
            'cutwater_z_span': [round(cutwater_bottom, 4), round(cutwater_hood_top, 4)],
        })
    arch_clearances = []
    for idx, sx in enumerate(arches_x_source):
        wx = sx + dx
        wy = 6.2 + dy
        gz = float(L.sample_ground_z(wx, wy))
        wz = float(water_surface_height(wx, wy, L.LAYOUT))
        surface_z = wz if math.isfinite(wz) else gz
        springline_z = 1.90 + dz_applied
        crown_intrados_z = 2.74 + dz_applied
        arch_clearances.append({
            'arch_index': idx,
            'world_xy': [round(wx, 4), round(wy, 4)],
            'under_arch_surface_z': round(surface_z, 4),
            'is_over_water': bool(math.isfinite(wz)),
            'springline_z': round(springline_z, 4),
            'crown_intrados_z': round(crown_intrados_z, 4),
            'crown_clearance_above_surface_m': round(crown_intrados_z - surface_z, 4),
        })
    west_abutment_z = float(L.sample_ground_z(9.8 + dx, 6.2 + dy))
    east_abutment_z = float(L.sample_ground_z(19.2 + dx, 6.2 + dy))
    deck_core_bottom = 2.80 + dz_applied
    deck_core_top = 2.98 + dz_applied
    flagstone_top = 3.0575 + dz_applied
    return {
        'dz_derivation': 'target_river_water_z(28,13) - source_water_baseline_m(2.04)',
        'source_water_baseline_m': source_waterline_z,
        'target_river_water_z_m': round(target_water_z, 6),
        'target_river_bed_z_m': round(target_bed_z, 6),
        'applied_dz_m': round(dz_applied, 6),
        'legacy_hardcoded_dz_062_comparison': {
            'legacy_dz_m': 0.62,
            'legacy_waterline_deficit_m': round((2.04 + 0.62) - target_water_z, 4),
            'legacy_arch0_crown_clearance_above_west_bank_m': round((2.74 + 0.62) - float(L.sample_ground_z(arches_x_source[0] + dx, 6.2 + dy)), 4),
            'waterline_contact_arch0_crown_clearance_above_west_bank_m': round((2.74 + dz_applied) - float(L.sample_ground_z(arches_x_source[0] + dx, 6.2 + dy)), 4),
        },
        'deck_core_z_span_m': [round(deck_core_bottom, 4), round(deck_core_top, 4)],
        'road_flagstone_top_z_m': round(flagstone_top, 4),
        'west_abutment_ground_z_m': round(west_abutment_z, 4),
        'east_abutment_ground_z_m': round(east_abutment_z, 4),
        'deck_clearance_above_west_abutment_m': round(deck_core_bottom - west_abutment_z, 4),
        'deck_clearance_above_east_abutment_m': round(deck_core_bottom - east_abutment_z, 4),
        'deck_clearance_above_river_water_m': round(deck_core_bottom - target_water_z, 4),
        'pier_contacts': pier_contacts,
        'arch_clearances': arch_clearances,
    }


def evaluate_harbor_contacts(zone08_geom, dx: float, dy: float, dz_applied: float) -> dict:
    """Measures exact triangle-span bounds and waterline contacts for harbor quays, jetties, and boats."""
    v, _, f = zone08_geom.arrays()
    categories = {}
    for label, prefixes in [
        ('docks_quays', ('dock_', 'quay_', 'pier_', 'piling_', 'wharf_', 'jetty_')),
        ('boats', ('boat_',)),
    ]:
        selected = [
            p for p in zone08_geom.parts
            if not p['name'].startswith(('bridge_', 'pine_', 'oak_')) and p['name'].startswith(prefixes)
        ]
        if not selected:
            continue
        tri_ids = np.concatenate([
            np.arange(int(p['first_triangle']), int(p['first_triangle']) + int(p['triangles']))
            for p in selected
        ])
        pts = v[np.unique(f[tri_ids].ravel())]
        lo = pts.min(axis=0)
        hi = pts.max(axis=0)
        t_lo = lo + np.array([dx, dy, dz_applied])
        t_hi = hi + np.array([dx, dy, dz_applied])
        categories[label] = {
            'part_count': int(len(selected)),
            'triangle_count': int(len(tri_ids)),
            'source_bounds': [[round(float(x), 6) for x in lo], [round(float(x), 6) for x in hi]],
            'translated_xyz_bounds': [[round(float(x), 6) for x in t_lo], [round(float(x), 6) for x in t_hi]],
        }

    pier_pts = [(18.2, 4.4), (19.6, 2.8), (21.2, 1.2), (22.8, -0.4), (24.4, -2.0), (25.8, -3.4)]
    intended_water_z = 2.04 + dz_applied
    segment_probes = []
    for i in range(len(pier_pts) - 1):
        x0, y0 = pier_pts[i]
        x1, y1 = pier_pts[i + 1]
        pyaw = math.atan2(y1 - y0, x1 - x0)
        ca, sa = math.cos(pyaw), math.sin(pyaw)
        nx_r, ny_r = sa, -ca
        mx, my = 0.5 * (x0 + x1), 0.5 * (y0 + y1)
        jx0, jy0 = mx + nx_r * (1.38 * 0.46), my + ny_r * (1.38 * 0.46)
        j_yaw = pyaw - math.pi * 0.5
        cj, sj = math.cos(j_yaw), math.sin(j_yaw)
        qx, qy = mx + dx, my + dy
        j_tip_x, j_tip_y = jx0 + 1.58 * cj + dx, jy0 + 1.58 * sj + dy
        bx, by = jx0 + (1.58 * 0.62) * cj - 0.66 * sj + dx, jy0 + (1.58 * 0.62) * sj + 0.66 * cj + dy
        gz_boat = float(L.sample_ground_z(bx, by))
        wz_boat = float(water_surface_height(bx, by, L.LAYOUT))
        r_dist, r_level, _ = L.pond_distance(bx, by, L.PONDS[0]), None, None
        from world_expanded_water import river_section
        riv_d, riv_lvl, _ = river_section(bx, by, L.LAYOUT['rivers'][0])
        segment_probes.append({
            'segment_index': i,
            'quay_world_xyz': [round(qx, 4), round(qy, 4), round(2.16 + dz_applied, 4)],
            'jetty_tip_world_xyz': [round(j_tip_x, 4), round(j_tip_y, 4), round(2.04 + dz_applied, 4)],
            'boat_origin_world_xyz': [round(bx, 4), round(by, 4), round(1.96 + dz_applied, 4)],
            'intended_waterline_z_m': round(intended_water_z, 6),
            'terrain_ground_z_at_boat_m': round(gz_boat, 4),
            'configured_water_z_at_boat_m': round(wz_boat, 4) if math.isfinite(wz_boat) else None,
            'distance_to_eastern_river_shore_m': round(float(riv_d), 4),
            'boat_hull_minus_terrain_m': round((1.96 + dz_applied) - gz_boat, 4),
        })

    any_boat_in_water = any(p['configured_water_z_at_boat_m'] is not None for p in segment_probes)
    return {
        'source_water_baseline_m': 2.04,
        'source_foundation_override_m': 2.35,
        'target_land_foundation_m': round(float(L.sample_ground_z(43.0, 14.0)), 6),
        'uniform_town_offset_z_m': round(dz_applied, 6),
        'intended_harbor_water_level_m': round(intended_water_z, 6),
        'water_contact_categories': categories,
        'quayside_and_boat_probes': segment_probes,
        'harbor_water_contact_status': (
            'PASS' if any_boat_in_water else
            'BLOCKER_REPORTED_FOR_WATER_MODULE: harbor_town at (43,14) places quays/jetties/boats at X=[37.76,47.09], Y=[7.23,17.10] (intended water Z=3.037508m), which is 8.0m-9.9m east of eastern-meandering-river (X=28..32 at Y=13..8); boats currently sit on dry ground Z=3.28..3.34m until world_expanded_water routes harbor water along the SW quay face.'
        ),
    }


def architecture(collection, reused):
    from village_buildings_geometry import build_zone06_npc_village
    from harbor_beacon_geometry import build_zone08_harbor_and_bridge

    partitions = json.loads(PARTITIONS_PATH.read_text(encoding='utf-8'))
    atlas = reused['farmstead'].data.materials[0]
    records = {}
    placed_objects = {}

    def place(key: str, obj, span_meta: dict | None = None):
        placement = L.building_placement(key)
        dx, dy = placement['translation_xy']
        dz = float(placement['foundation_offset_z'])
        dz_reason = 'layout_contract_foundation_offset_z (target_ground_z - source_ground_z)'
        if key == 'bridge':
            # Derive bridge Z offset from exact river waterline contact at (28, 13)
            # rather than raw riverbed (0.65m) or a guessed constant (0.62m).
            target_water_z = float(water_surface_height(*placement['target_xy'], L.LAYOUT))
            dz = target_water_z - 2.04
            dz_reason = 'measured_waterline_contact (water_surface_height(28,13)=3.050333m - source_water_level=2.04m)'

        src_bounds_info = measure_world_object_bounds(obj)
        obj.location += Vector((dx, dy, dz))
        obj.hide_render = False
        placed_objects[key] = obj
        world_bounds_info = measure_world_object_bounds(obj)
        tx, ty = placement['target_xy']
        target_pad_ground_z = float(L.sample_ground_z(tx, ty))
        lowest_placed_vertex_z = float(world_bounds_info['world_bounds'][0][2])
        rec = {
            'placement': placement,
            'applied_translation_xyz': [round(float(dx), 6), round(float(dy), 6), round(float(dz), 6)],
            'dz_derivation': dz_reason,
            'asset_scale_preserved': [round(float(s), 6) for s in obj.scale],
            'scale_is_unit_1_1_1': bool(np.allclose(obj.scale, (1.0, 1.0, 1.0), atol=1e-6)),
            'source_mesh_bounds': src_bounds_info['world_bounds'],
            'placed_world_bounds': world_bounds_info['world_bounds'],
            'placed_world_center': world_bounds_info['world_center'],
            'placed_dimensions_m': world_bounds_info['world_dimensions_m'],
            'vertex_count': world_bounds_info['vertices'],
            'triangle_count': world_bounds_info['triangles'],
            'target_anchor_ground_z_m': round(target_pad_ground_z, 6),
            'lowest_placed_vertex_z_m': round(lowest_placed_vertex_z, 6),
            'footing_embed_below_anchor_ground_m': round(target_pad_ground_z - lowest_placed_vertex_z, 6),
        }
        if span_meta is not None:
            rec['partition_span_metrics'] = span_meta
        records[key] = rec

    for key, obj in reused.items():
        place(key, obj)

    zone08_geom_cache = None
    for section, builder in [
        ('zone06', build_zone06_npc_village),
        ('zone08', build_zone08_harbor_and_bridge),
    ]:
        geom = builder()
        if section == 'zone08':
            zone08_geom_cache = geom
        for key, record in partitions[section].items():
            names = set(record['parts'])
            arrays, span_meta = select_partition_triangles(geom, names)
            if not len(arrays[2]):
                continue
            obj = mesh_object('ExpandedArchitecture_' + key, *arrays, atlas, collection)
            place(key, obj, span_meta=span_meta)

    bridge_rec = records['bridge']
    b_dx, b_dy, b_dz = bridge_rec['applied_translation_xyz']
    bridge_rec['bridge_waterline_and_bank_contact_audit'] = evaluate_bridge_contacts(b_dx, b_dy, b_dz)

    harbor_rec = records['harbor_town']
    h_dx, h_dy, h_dz = harbor_rec['applied_translation_xyz']
    harbor_rec['harbor_waterline_and_quay_contact_audit'] = evaluate_harbor_contacts(
        zone08_geom_cache, h_dx, h_dy, h_dz
    )
    return records, placed_objects


def parse_args():
    parser = argparse.ArgumentParser(description='Expanded 120x90m layout scaffold and gated art builder.')
    parser.add_argument(
        '--scaffold-only', '--layout-only',
        dest='scaffold_only',
        action='store_true',
        default=True,
        help='Build only the 120x90m terrain, 9 placed buildings, and native 1-pond water scaffold (default).',
    )
    parser.add_argument(
        '--render-scaffold-proof',
        action='store_true',
        help='Render a small 1200x900 12-sample native layout/contact proof of the scaffold (not a full art render).',
    )
    parser.add_argument(
        '--full-art-approved',
        action='store_true',
        help='Explicitly gated: only run full forest/meadow/world art render after Root approves the new short golden sward mini-view.',
    )
    parser.add_argument('--preview', action='store_true', help='Preview resolution for full-art mode.')
    return parser.parse_args()


def main():
    args = parse_args()
    if args.full_art_approved:
        raise RuntimeError(
            'GATED: Full-world grass/forest/art render is blocked until Root explicitly approves '
            'the new short golden sward actual mini-view and releases the full-world build gate.'
        )

    OUT.mkdir(parents=True, exist_ok=True)
    source_sha_before = sha(SOURCE)
    reuse_sha_before = sha(REUSE)
    partitions_sha = sha(PARTITIONS_PATH)

    print('VERIFY_PROTECTED_SOURCE_OBSERVATORY_SIGNATURE', flush=True)
    source_obs_sig = read_protected_source_observatory_signature()

    configure_water_layout(L.LAYOUT, L.base_ground_z)
    assert len(L.PONDS) == 1, f'Expected exactly 1 pond, got {len(L.PONDS)}'

    # Reuse only the detailed architecture and exact approved observatory.
    bpy.ops.wm.open_mainfile(filepath=str(REUSE), load_ui=False, use_scripts=False)
    scene = bpy.context.scene
    mapping = {
        'farmstead': 'Zone04_ReferenceDetail',
        'information_beacon': 'Zone07_ReferenceDetail',
        'castle': 'Zone09_ReferenceDetail',
    }
    reused = {key: bpy.data.objects[name] for key, name in mapping.items()}
    reused['observatory'] = next(
        ob for ob in scene.objects if ob.type == 'MESH' and ob.name.startswith('Zone05')
    )
    reuse_obs_sig_before = extract_observatory_local_signature(reused['observatory'])

    keep = set(reused.values())
    for ob in list(bpy.data.objects):
        if ob not in keep:
            bpy.data.objects.remove(ob, do_unlink=True)

    art = bpy.data.collections.new('Expanded_Golden_Mythic_World_Scaffold')
    scene.collection.children.link(art)
    for ob in reused.values():
        for c in list(ob.users_collection):
            c.objects.unlink(ob)
        art.objects.link(ob)

    print('BUILD_EXPANDED_TERRAIN_120X90', flush=True)
    ground, rim_ob, ground_report = terrain(art)
    print('PLACE_AUTHORED_BUILDINGS_9_ANCHORS', flush=True)
    buildings, placed_objects = architecture(art, reused)
    print('BUILD_NATIVE_ONE_POND_AND_RIVER_WATER', flush=True)
    water = build_world_expanded_water(art, L.LAYOUT)

    # Configure explicit CUDA and 4 threads
    scene.render.engine = 'CYCLES'
    configure_cuda(scene)
    scene.cycles.samples = 12
    scene.cycles.use_denoising = True
    scene.cycles.transparent_max_bounces = 32
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 4
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'

    data = bpy.data.cameras.new('ExpandedWorld_ScaffoldOverviewCamera')
    camera = bpy.data.objects.new(data.name, data)
    art.objects.link(camera)
    scene.camera = camera
    data.lens = 49
    data.clip_end = 1200
    point_camera(camera, (0, -155, 111), (0, 7, 4))
    lighting(scene, 'day')

    bpy.context.preferences.filepaths.save_version = 0
    for image in bpy.data.images:
        if image.source == 'FILE' and image.has_data:
            try:
                image.pack()
            except RuntimeError:
                pass

    scaffold_blend = OUT / 'expanded-layout-scaffold.blend'
    bpy.ops.wm.save_as_mainfile(filepath=str(scaffold_blend), compress=True)
    print('SAVED_SCAFFOLD_BLEND', scaffold_blend, flush=True)

    proof_paths = []
    if args.render_scaffold_proof:
        overview_proof = OUT / 'expanded-layout-scaffold-proof.png'
        scene.render.filepath = str(overview_proof)
        print('RENDER_SCAFFOLD_PROOF', overview_proof, flush=True)
        bpy.ops.render.render(write_still=True)
        proof_paths.append(str(overview_proof))

        # Also render a close contact proof of Bridge (28,13) + Harbor Town (43,14)
        point_camera(camera, (24, -6, 20), (36, 13, 3.2))
        data.lens = 42
        harbor_bridge_proof = OUT / 'expanded-harbor-bridge-contact-proof.png'
        scene.render.filepath = str(harbor_bridge_proof)
        print('RENDER_HARBOR_BRIDGE_CONTACT_PROOF', harbor_bridge_proof, flush=True)
        bpy.ops.render.render(write_still=True)
        proof_paths.append(str(harbor_bridge_proof))

    # Reopen saved scaffold blend to verify persisted Observatory local hashes and object scales
    bpy.ops.wm.open_mainfile(filepath=str(scaffold_blend), load_ui=False, use_scripts=False)
    saved_obs = next(
        ob for ob in bpy.data.objects if ob.type == 'MESH' and ob.name.startswith('Zone05')
    )
    saved_obs_sig = extract_observatory_local_signature(saved_obs)

    source_sha_after = sha(SOURCE)
    reuse_sha_after = sha(REUSE)
    assert source_sha_before == source_sha_after, 'SOURCE blend was modified!'
    assert reuse_sha_before == reuse_sha_after, 'REUSE blend was modified!'
    assert saved_obs_sig['local_co_sha256'] == source_obs_sig['local_co_sha256'], 'Observatory local vertices changed!'
    assert saved_obs_sig['local_topology_sha256'] == source_obs_sig['local_topology_sha256'], 'Observatory topology changed!'
    assert saved_obs_sig['local_uv_sha256'] == source_obs_sig['local_uv_sha256'], 'Observatory UVs changed!'

    pond = L.PONDS[0]
    pond_xy = np.asarray(L.pond_outline(pond))
    pond_area_m2 = float(
        abs(np.dot(pond_xy[:, 0], np.roll(pond_xy[:, 1], -1)) - np.dot(pond_xy[:, 1], np.roll(pond_xy[:, 0], -1))) / 2.0
    )

    report = {
        'status': 'LAYOUT_SCAFFOLD_ONLY_FULL_ART_RENDER_GATED',
        'mode': 'scaffold_only',
        'full_world_art_render_executed': False,
        'lush_groundcover_executed': False,
        'forest_pass_executed': False,
        'scaffold_blend': str(scaffold_blend),
        'scaffold_blend_bytes': int(scaffold_blend.stat().st_size),
        'scaffold_proof_renders': proof_paths,
        'protected_file_integrity': {
            'source_blend_path': str(SOURCE),
            'source_sha256_before': source_sha_before,
            'source_sha256_after': source_sha_after,
            'source_unmodified_verified': bool(source_sha_before == source_sha_after),
            'reuse_blend_path': str(REUSE),
            'reuse_sha256_before': reuse_sha_before,
            'reuse_sha256_after': reuse_sha_after,
            'reuse_unmodified_verified': bool(reuse_sha_before == reuse_sha_after),
            'source_partitions_sha256': partitions_sha,
            'live_assets_modified': False,
        },
        'approved_observatory_preservation': {
            'source_observatory': source_obs_sig,
            'reuse_observatory_before': reuse_obs_sig_before,
            'saved_scaffold_observatory_after_reopen': saved_obs_sig,
            'local_co_sha256_matches_source': bool(saved_obs_sig['local_co_sha256'] == source_obs_sig['local_co_sha256']),
            'local_topology_sha256_matches_source': bool(saved_obs_sig['local_topology_sha256'] == source_obs_sig['local_topology_sha256']),
            'local_uv_sha256_matches_source': bool(saved_obs_sig['local_uv_sha256'] == source_obs_sig['local_uv_sha256']),
            'scale_is_unit_1_1_1': bool(saved_obs_sig['object_scale'] == [1.0, 1.0, 1.0]),
            'intentional_world_translation': saved_obs_sig['object_location'],
        },
        'single_pond_verification': {
            'pond_count': int(len(L.PONDS)),
            'pond_id': pond['id'],
            'center_xy': list(pond['center']),
            'radii_xy': list(pond['radii']),
            'angle_deg': float(pond['angle_deg']),
            'water_level_m': round(float(pond['level']), 6),
            'carved_bed_center_z_m': round(float(L.sample_ground_z(*pond['center'])), 6),
            'exact_polygon_area_m2': round(pond_area_m2, 6),
            'envelope_fraction': round(pond_area_m2 / (L.WIDTH * L.HEIGHT), 8),
        },
        'ground': ground_report,
        'architecture': buildings,
        'water': water['report'],
        'all_9_buildings_unit_scale_verified': bool(
            all(rec['scale_is_unit_1_1_1'] for rec in buildings.values()) and len(buildings) == 9
        ),
        'blender': bpy.app.version_string,
    }
    report_path = OUT / 'scaffold-report.json'
    report_path.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('SCAFFOLD_REPORT_WRITTEN', report_path, flush=True)


if __name__ == '__main__':
    main()
