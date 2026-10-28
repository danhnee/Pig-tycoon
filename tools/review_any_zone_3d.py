#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Universal True-3D Multi-Angle Module Renderer & Anti-Cheat Verifier (`review_any_zone_3d.py`)
=============================================================================================
Renders any Zone (`Zone01`..`Zone11`) from `Farm_Main_3D_Master.blend` (or an updated `.meshbin`)
in Blender 5.3.0 from 5 orthographic/perspective angles + full world context, and verifies:
1. Non-zero 3D volumetric thickness across X, Y, Z (rejects flat 2D card projections).
2. Zero degenerate triangles (`min_double_area > 1e-10`).
3. Valid UV coordinates in `[0, 1]`.
"""

import argparse
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def render_view(scene, camera, path: Path, location, target, size=900):
    camera.location = Vector(location)
    look_at(camera, Vector(target))
    scene.camera = camera
    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--zone", type=str, required=True, help="Zone prefix or full object name (e.g. Zone04 or Zone05_Building_...)")
    parser.add_argument("--source", type=Path, default=ROOT / "ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend")
    parser.add_argument("--meshbin", type=Path, default=None, help="Optional candidate .meshbin to swap into the zone before rendering")
    parser.add_argument("--output", type=Path, default=None)
    parser.add_argument("--engine", type=str, default="CYCLES", choices=["BLENDER_EEVEE", "CYCLES"])
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else [])

    bpy.ops.wm.open_mainfile(filepath=str(args.source))
    scene = bpy.context.scene

    target_obj = None
    for o in scene.objects:
        if o.type == "MESH" and (o.name == args.zone or o.name.startswith(args.zone)):
            target_obj = o
            break
    if target_obj is None:
        raise RuntimeError(f"Zone object matching '{args.zone}' not found in {args.source}")

    zone_name = target_obj.name
    out_dir = (args.output or (ROOT / ".collab" / "artifacts" / f"review_{zone_name}")).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    if args.meshbin and args.meshbin.exists():
        raw = args.meshbin.read_bytes()
        nv, n_idx = struct.unpack("<ii", raw[:8])
        off = 8
        verts_u = np.frombuffer(raw, dtype="<f4", count=nv * 3, offset=off).reshape(nv, 3)
        off += nv * 12
        has_norm = len(raw) >= (8 + nv * 32 + n_idx * 4)
        if has_norm:
            off += nv * 12
        uvs_u = np.frombuffer(raw, dtype="<f4", count=nv * 2, offset=off).reshape(nv, 2)
        off += nv * 8
        faces_u = np.frombuffer(raw, dtype="<i4", count=n_idx, offset=off).reshape(-1, 3)
        # Convert Unity (x, y_up, z) back to Blender (x, z, y_up)
        verts_b = verts_u[:, [0, 2, 1]]
        faces_b = faces_u[:, [0, 2, 1]]
        new_mesh = bpy.data.meshes.new(zone_name + "_Candidate")
        new_mesh.from_pydata(verts_b.tolist(), [], faces_b.tolist())
        uv_layer = new_mesh.uv_layers.new(name="UVMap")
        uv_layer.data.foreach_set("uv", uvs_u[faces_b.reshape(-1)].reshape(-1))
        new_mesh.update()
        for mat in target_obj.data.materials:
            new_mesh.materials.append(mat)
        target_obj.data = new_mesh

    mesh = target_obj.data
    mesh.calc_loop_triangles()
    n_verts = len(mesh.vertices)
    v_arr = np.empty(n_verts * 3, dtype=np.float32)
    mesh.vertices.foreach_get("co", v_arr)
    v_arr = v_arr.reshape(-1, 3)
    # Apply world matrix
    mat_w = np.array(target_obj.matrix_world, dtype=np.float32)
    v_world = (v_arr @ mat_w[:3, :3].T) + mat_w[:3, 3]

    b_min = v_world.min(axis=0)
    b_max = v_world.max(axis=0)
    extents = b_max - b_min
    center = (b_min + b_max) * 0.5
    radius = float(np.linalg.norm(extents) * 0.5)

    n_tris = len(mesh.loop_triangles)
    tri_indices = np.empty(n_tris * 3, dtype=np.int32)
    mesh.loop_triangles.foreach_get("vertices", tri_indices)
    tri_indices = tri_indices.reshape(-1, 3)
    tri_pts = v_world[tri_indices]
    double_areas = np.linalg.norm(np.cross(tri_pts[:, 1] - tri_pts[:, 0], tri_pts[:, 2] - tri_pts[:, 0]), axis=1)
    min_double_area = float(double_areas.min()) if n_tris > 0 else 0.0
    degenerate_count = int(np.sum(double_areas <= 1e-10))

    if mesh.uv_layers.active and len(mesh.uv_layers.active.data) > 0:
        uv_raw = np.empty(len(mesh.uv_layers.active.data) * 2, dtype=np.float32)
        mesh.uv_layers.active.data.foreach_get("uv", uv_raw)
        uv_raw = uv_raw.reshape(-1, 2)
        uv_min = [round(float(uv_raw[:, 0].min()), 6), round(float(uv_raw[:, 1].min()), 6)]
        uv_max = [round(float(uv_raw[:, 0].max()), 6), round(float(uv_raw[:, 1].max()), 6)]
        uv_in_unit_square = bool(np.isfinite(uv_raw).all() and (uv_raw >= 0.0).all() and (uv_raw <= 1.0).all())
    else:
        uv_min = [0.0, 0.0]
        uv_max = [0.0, 0.0]
        uv_in_unit_square = False

    # Save scene state
    old_hidden = {o.name: o.hide_render for o in scene.objects}
    col_states = {c.name: c.hide_render for c in bpy.data.collections}
    old_cam = scene.camera
    old_world = scene.world

    for o in scene.objects:
        o.hide_render = o != target_obj
    for c in target_obj.users_collection:
        c.hide_render = False

    stage = bpy.data.collections.new("Zone_Review_Stage")
    scene.collection.children.link(stage)

    def stage_obj(name, data):
        o = bpy.data.objects.new(name, data)
        stage.objects.link(o)
        return o

    cam = stage_obj("ZoneReviewCam", bpy.data.cameras.new("ZoneReviewCam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = max(4.5, float(max(extents[0], extents[1], extents[2]) * 1.35))
    cam.data.clip_end = 400.0

    dist = max(8.0, radius * 1.8)
    for lname, offset, energy in [
        ("Key", (-dist * 0.6, -dist * 0.8, dist * 0.9), 700),
        ("Fill", (dist * 0.7, -dist * 0.3, dist * 0.6), 300),
        ("Rim", (dist * 0.2, dist * 0.8, dist * 0.8), 500),
    ]:
        lt = bpy.data.lights.new(lname, "AREA")
        lt.energy = energy
        lt.shape = "DISK"
        lt.size = max(4.0, radius * 0.5)
        lo = stage_obj(lname, lt)
        lo.location = Vector(center) + Vector(offset)
        look_at(lo, Vector(center))

    world = bpy.data.worlds.new("ZoneReviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.26, 0.30, 0.38, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    scene.world = world
    scene.render.engine = args.engine
    if args.engine == "CYCLES":
        scene.cycles.samples = 24
        scene.cycles.use_denoising = True

    renders = []
    for rname, off_vec in [
        ("01-front.png", (dist * 0.55, -dist * 0.85, dist * 0.55)),
        ("02-west.png", (-dist * 0.85, -dist * 0.55, dist * 0.50)),
        ("03-rear.png", (dist * 0.55, dist * 0.85, dist * 0.55)),
        ("04-east.png", (dist * 0.85, dist * 0.55, dist * 0.50)),
        ("05-top-ortho.png", (0.0, -0.01, dist * 1.4)),
    ]:
        rp = out_dir / rname
        render_view(scene, cam, rp, center + np.array(off_vec), center)
        renders.append(str(rp))

    # Restore full world context and render 06-world-context.png
    scene.camera = old_cam
    scene.world = old_world
    for o in list(stage.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.collections.remove(stage)
    for name, hidden in old_hidden.items():
        if name in bpy.data.objects:
            bpy.data.objects[name].hide_render = hidden
    for name, hidden in col_states.items():
        if name in bpy.data.collections:
            bpy.data.collections[name].hide_render = hidden

    scene.render.engine = args.engine
    if args.engine == "CYCLES":
        scene.cycles.samples = 24
        scene.cycles.use_denoising = True
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 960
    ctx_path = out_dir / "06-world-context.png"
    scene.render.filepath = str(ctx_path)
    bpy.ops.render.render(write_still=True)
    renders.append(str(ctx_path))

    is_true_3d = bool(
        extents[0] > 0.25
        and extents[1] > 0.25
        and extents[2] > 0.25
        and degenerate_count == 0
        and uv_in_unit_square
    )
    report = {
        "zone": zone_name,
        "vertices": int(n_verts),
        "triangles": int(n_tris),
        "bounds_min": [round(float(x), 4) for x in b_min],
        "bounds_max": [round(float(x), 4) for x in b_max],
        "extents_xyz": [round(float(x), 4) for x in extents],
        "min_double_triangle_area": min_double_area,
        "degenerate_triangles": degenerate_count,
        "uv_min": uv_min,
        "uv_max": uv_max,
        "uv_in_unit_square": uv_in_unit_square,
        "bbox_nonzero_xyz": bool(extents[0] > 0.25 and extents[1] > 0.25 and extents[2] > 0.25),
        "is_true_3d_volumetric": is_true_3d,
        "renders": renders,
    }
    (out_dir / "zone_3d_verification.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("ZONE_3D_VERIFIED", json.dumps(report), flush=True)


if __name__ == "__main__":
    main()
