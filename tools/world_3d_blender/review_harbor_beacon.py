#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Validate and render Zone07 (`Trạm Thông Tin`) and Zone08 (`Stone Bridge & Harbor Town`) in Blender 5.3.0.

Renders:
- `01-zone07-beacon-pbr.png`
- `02-zone07-beacon-wireframe-clay.png`
- `03-zone08-harbor-bridge-pbr.png`
- `04-zone08-harbor-bridge-wireframe-clay.png`
and writes `validation.json` + `.meshbin` files to `D:/Pig-tycoon/.collab/artifacts/zone07-08-20261018/`.
Does NOT modify `Farm_Main_3D_Master.blend` or any live project files.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

from canonical_map_helpers import sample_ground_z, validate_closed_parts
from harbor_beacon_geometry import build_zone07_beacon, build_zone08_harbor_and_bridge
from observatory_blender import create_observatory_material
from observatory_geometry import meshbin_bytes

ZONE07_NAME = "Zone07_Landmark_TramThongTin_InfoBeacon"
ZONE08_NAME = "Zone08_Vista_StoneBridge_And_HarborTown"


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def create_wireframe_clay_material() -> bpy.types.Material:
    mat = bpy.data.materials.new("Mat_Review_Wireframe_Clay")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out_node = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.78
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.22

    wire = nt.nodes.new("ShaderNodeWireframe")
    wire.use_pixel_size = True
    wire.inputs["Size"].default_value = 1.15

    mix_node = nt.nodes.new("ShaderNodeMix")
    mix_node.data_type = "RGBA"
    mix_node.inputs[6].default_value = (0.85, 0.81, 0.75, 1.0)  # Warm architectural clay
    mix_node.inputs[7].default_value = (0.09, 0.10, 0.13, 1.0)  # Crisp charcoal wireframe

    nt.links.new(wire.outputs["Fac"], mix_node.inputs["Factor"])
    nt.links.new(mix_node.outputs[2], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])
    return mat


def build_blender_mesh(name: str, geom, pbr_mat: bpy.types.Material):
    v, uv, f = geom.arrays()
    assert np.isfinite(v).all() and np.isfinite(uv).all(), f"Non-finite arrays in {name}"
    assert ((uv >= 0.0) & (uv <= 1.0)).all(), f"UV out of bounds in {name}"
    tri = v[f]
    normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    areas = np.linalg.norm(normals, axis=1)
    assert areas.min() > 1e-9, f"Degenerate triangles in {name}: {int((areas <= 1e-9).sum())}"

    mesh = bpy.data.meshes.new(name + "_DetailMesh")
    n_v = len(v)
    n_f = len(f)
    mesh.vertices.add(n_v)
    mesh.vertices.foreach_set("co", v.astype(np.float32).reshape(-1))
    mesh.loops.add(n_f * 3)
    mesh.loops.foreach_set("vertex_index", f.astype(np.int32).reshape(-1))
    mesh.polygons.add(n_f)
    mesh.polygons.foreach_set("loop_start", (np.arange(n_f, dtype=np.int32) * 3))
    mesh.polygons.foreach_set("loop_total", np.full(n_f, 3, dtype=np.int32))
    uv_layer = mesh.uv_layers.new(name="UVMap")
    uv_layer.data.foreach_set("uv", uv[f.reshape(-1)].astype(np.float32).reshape(-1))
    assert not mesh.validate(verbose=False), f"Blender repaired invalid geometry in {name}"
    mesh.update()
    mesh.materials.append(pbr_mat)
    return mesh, v, uv, f, float(areas.min())


def render_still(scene, camera, filepath: Path, cam_loc, target_loc, ortho_scale: float, res_x=1280, res_y=960):
    camera.data.ortho_scale = ortho_scale
    camera.location = Vector(cam_loc)
    look_at(camera, Vector(target_loc))
    scene.camera = camera
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(filepath)
    print("RENDER", filepath, flush=True)
    bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=ROOT / "ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend")
    parser.add_argument("--output", type=Path, default=ROOT / ".collab/artifacts/zone07-08-20261018")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else [])
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=True)

    source_hash_before = hashlib.sha256(args.source.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(args.source))
    scene = bpy.context.scene

    obj07 = bpy.data.objects.get(ZONE07_NAME)
    obj08 = bpy.data.objects.get(ZONE08_NAME)
    if obj07 is None or obj08 is None:
        raise RuntimeError(f"Missing {ZONE07_NAME} or {ZONE08_NAME} in {args.source}")

    old_tris_07 = sum(len(p.vertices) - 2 for p in obj07.data.polygons)
    old_tris_08 = sum(len(p.vertices) - 2 for p in obj08.data.polygons)

    tex_dir = ROOT / "Assets/Textures/FarmMain3D"
    pbr_mat = create_observatory_material(
        tex_dir / "FarmMain_3D_Albedo_4K.png",
        tex_dir / "FarmMain_3D_Emission_4K.png",
    )
    clay_mat = create_wireframe_clay_material()

    # Build and validate Zone07 and Zone08 closed-manifold geometries
    geom07 = build_zone07_beacon()
    val07 = validate_closed_parts(geom07)
    assert val07["closed_outward_components"], f"Zone07 failed closed-parts check: {val07['failures']}"

    geom08 = build_zone08_harbor_and_bridge()
    val08 = validate_closed_parts(geom08)
    assert val08["closed_outward_components"], f"Zone08 failed closed-parts check: {val08['failures']}"

    mesh07, v07, uv07, f07, min_area07 = build_blender_mesh(ZONE07_NAME, geom07, pbr_mat)
    mesh08, v08, uv08, f08, min_area08 = build_blender_mesh(ZONE08_NAME, geom08, pbr_mat)

    obj07.data = mesh07
    obj08.data = mesh08

    # Persist Unity .meshbin files
    bin07 = meshbin_bytes(v07, uv07, f07)
    bin08 = meshbin_bytes(v08, uv08, f08)
    path_bin07 = out / f"{ZONE07_NAME}.meshbin"
    path_bin08 = out / f"{ZONE08_NAME}.meshbin"
    path_bin07.write_bytes(bin07)
    path_bin08.write_bytes(bin08)

    # Set up clean review stage
    for o in scene.objects:
        o.hide_render = True
    for c in bpy.data.collections:
        c.hide_render = False

    stage = bpy.data.collections.new("HarborBeacon_Review_Stage")
    scene.collection.children.link(stage)

    def stage_obj(name, data):
        o = bpy.data.objects.new(name, data)
        stage.objects.link(o)
        return o

    camera = stage_obj("ReviewCamera", bpy.data.cameras.new("ReviewCamera"))
    camera.data.type = "ORTHO"
    camera.data.clip_end = 400.0

    # Studio 3-point area lighting
    key_light_data = bpy.data.lights.new("KeyLight", "AREA")
    key_light_data.energy = 1400.0
    key_light_data.shape = "DISK"
    key_light_data.size = 10.0
    key_light_data.color = (1.0, 0.95, 0.88)
    key_light = stage_obj("KeyLight", key_light_data)

    fill_light_data = bpy.data.lights.new("FillLight", "AREA")
    fill_light_data.energy = 650.0
    fill_light_data.shape = "DISK"
    fill_light_data.size = 9.0
    fill_light_data.color = (0.85, 0.92, 1.0)
    fill_light = stage_obj("FillLight", fill_light_data)

    rim_light_data = bpy.data.lights.new("RimLight", "AREA")
    rim_light_data.energy = 950.0
    rim_light_data.shape = "DISK"
    rim_light_data.size = 8.0
    rim_light_data.color = (1.0, 0.92, 0.82)
    rim_light = stage_obj("RimLight", rim_light_data)

    world = bpy.data.worlds.new("HarborBeacon_Review_World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.24, 0.28, 0.36, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    scene.world = world

    scene.render.engine = "CYCLES"
    scene.cycles.samples = 36
    scene.cycles.use_denoising = True
    scene.render.threads_mode = "FIXED"
    scene.render.threads = 6
    scene.view_settings.view_transform = "AgX"

    # Subtle neutral studio ground plane below the models
    floor_mesh = bpy.data.meshes.new("ReviewFloor")
    floor_mesh.from_pydata(
        [(-120, -120, 1.10), (120, -120, 1.10), (120, 120, 1.10), (-120, 120, 1.10)],
        [],
        [(0, 1, 2, 3)],
    )
    floor_obj = stage_obj("ReviewFloor", floor_mesh)
    floor_mat = bpy.data.materials.new("Mat_ReviewFloor")
    floor_mat.use_nodes = True
    floor_mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.20, 0.22, 0.26, 1.0)
    floor_mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.85
    floor_obj.data.materials.append(floor_mat)

    def position_lights(focus, radius_scale=1.0):
        fc = Vector(focus)
        key_light.location = fc + Vector((-9.0 * radius_scale, -13.0 * radius_scale, 14.0 * radius_scale))
        fill_light.location = fc + Vector((11.0 * radius_scale, -7.0 * radius_scale, 9.0 * radius_scale))
        rim_light.location = fc + Vector((2.0 * radius_scale, 13.0 * radius_scale, 12.0 * radius_scale))
        key_light_data.energy = 1100.0 * (radius_scale ** 1.35)
        fill_light_data.energy = 520.0 * (radius_scale ** 1.35)
        rim_light_data.energy = 780.0 * (radius_scale ** 1.35)
        look_at(key_light, fc)
        look_at(fill_light, fc)
        look_at(rim_light, fc)

    # -------------------------------------------------------------------------
    # Render 01 & 02: Zone07 (`Trạm Thông Tin` Beacon Lighthouse)
    # -------------------------------------------------------------------------
    bz07 = float(sample_ground_z(8.6, 4.6))
    focus07 = (8.6, 4.6, bz07 + 2.35)
    cam07 = (8.6 - 7.5, 4.6 - 9.5, bz07 + 6.4)
    obj07.hide_render = False
    obj08.hide_render = True
    floor_obj.location.z = bz07 - 1.45
    position_lights(focus07, radius_scale=0.85)

    obj07.data.materials[0] = pbr_mat
    render_still(scene, camera, out / "01-zone07-beacon-pbr.png", cam07, focus07, ortho_scale=7.2, res_x=1080, res_y=1080)

    obj07.data.materials[0] = clay_mat
    render_still(scene, camera, out / "02-zone07-beacon-wireframe-clay.png", cam07, focus07, ortho_scale=7.2, res_x=1080, res_y=1080)
    obj07.data.materials[0] = pbr_mat

    # -------------------------------------------------------------------------
    # Render 03 & 04: Zone08 (`Stone Bridge & Medieval Harbor Town`)
    # -------------------------------------------------------------------------
    focus08 = (18.9, 3.4, 4.55)
    cam08 = (18.9 - 14.0, 3.4 - 22.0, 4.55 + 14.5)
    obj07.hide_render = True
    obj08.hide_render = False
    floor_obj.location.z = 0.0
    position_lights(focus08, radius_scale=2.1)

    obj08.data.materials[0] = pbr_mat
    render_still(scene, camera, out / "03-zone08-harbor-bridge-pbr.png", cam08, focus08, ortho_scale=22.6, res_x=1440, res_y=960)

    obj08.data.materials[0] = clay_mat
    render_still(scene, camera, out / "04-zone08-harbor-bridge-wireframe-clay.png", cam08, focus08, ortho_scale=22.6, res_x=1440, res_y=960)
    obj08.data.materials[0] = pbr_mat

    source_hash_after = hashlib.sha256(args.source.read_bytes()).hexdigest()
    assert source_hash_before == source_hash_after, "Source master blend file was modified!"

    report = {
        "source_blend": str(args.source),
        "source_sha256": source_hash_after,
        "source_unmodified": source_hash_before == source_hash_after,
        "closed_outward_components": bool(val07["closed_outward_components"] and val08["closed_outward_components"]),
        "failures": val07["failures"] + val08["failures"],
        "zone07": {
            "name": ZONE07_NAME,
            "old_triangles": old_tris_07,
            "parts": val07["parts"],
            "vertices": val07["vertices"],
            "triangles": val07["triangles"],
            "closed_outward_components": val07["closed_outward_components"],
            "failures": val07["failures"],
            "minimum_double_triangle_area": min_area07,
            "bounds_min": [round(float(x), 4) for x in v07.min(axis=0)],
            "bounds_max": [round(float(x), 4) for x in v07.max(axis=0)],
            "meshbin_path": str(path_bin07),
            "meshbin_sha256": hashlib.sha256(bin07).hexdigest(),
        },
        "zone08": {
            "name": ZONE08_NAME,
            "old_triangles": old_tris_08,
            "parts": val08["parts"],
            "vertices": val08["vertices"],
            "triangles": val08["triangles"],
            "closed_outward_components": val08["closed_outward_components"],
            "failures": val08["failures"],
            "minimum_double_triangle_area": min_area08,
            "bounds_min": [round(float(x), 4) for x in v08.min(axis=0)],
            "bounds_max": [round(float(x), 4) for x in v08.max(axis=0)],
            "meshbin_path": str(path_bin08),
            "meshbin_sha256": hashlib.sha256(bin08).hexdigest(),
        },
        "renders": [
            str(out / "01-zone07-beacon-pbr.png"),
            str(out / "02-zone07-beacon-wireframe-clay.png"),
            str(out / "03-zone08-harbor-bridge-pbr.png"),
            str(out / "04-zone08-harbor-bridge-wireframe-clay.png"),
        ],
    }
    (out / "validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("ZONE07_08_REVIEW_COMPLETE", json.dumps(report, indent=2), flush=True)


if __name__ == "__main__":
    main()
