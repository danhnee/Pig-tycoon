#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Validate and render Zone09, Zone02, Zone03, and Zone11 in Blender 5.3.0 without modifying live assets.

Outputs to `D:/Pig-tycoon/.collab/artifacts/zone02-09-11-20261018/`:
  - `01-zone09-castle-cliff-pbr.png`
  - `02-zone09-castle-cliff-wireframe-clay.png`
  - `03-zone02-11-diorama-ruins-pbr.png`
  - `04-zone02-11-diorama-ruins-wireframe-clay.png`
  - `Zone09_Vista_TwilightCastle_And_WaterfallCliff.meshbin`
  - `Zone02_Diorama_Cutaway_RockRim_And_Base.meshbin`
  - `Zone03_Water_River_Waterfalls_And_3Ponds.meshbin`
  - `Zone11_Props_AncientRuins_And_GoldenFlora.meshbin`
  - `validation.json`
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

from canonical_map_helpers import validate_closed_parts
from citadel_diorama_ruins_geometry import build_all_citadel_diorama_ruins_zones
from observatory_blender import create_observatory_material
from observatory_geometry import meshbin_bytes


TARGET_ZONES = [
    "Zone09_Vista_TwilightCastle_And_WaterfallCliff",
    "Zone02_Diorama_Cutaway_RockRim_And_Base",
    "Zone11_Props_AncientRuins_And_GoldenFlora",
]


def mesh_hash(obj) -> str:
    v = np.empty(len(obj.data.vertices) * 3, dtype=np.float32)
    obj.data.vertices.foreach_get("co", v)
    loops = np.empty(len(obj.data.loops), dtype=np.int32)
    obj.data.loops.foreach_get("vertex_index", loops)
    return hashlib.sha256(v.tobytes() + loops.tobytes()).hexdigest()


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def create_wireframe_clay_material(name="Mat_Review_Wireframe_Clay", clay_rgb=(0.80, 0.74, 0.66), wire_rgb=(0.10, 0.12, 0.15)):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.68

    wire = nt.nodes.new("ShaderNodeWireframe")
    wire.use_pixel_size = True
    wire.inputs["Size"].default_value = 1.25

    mix_rgb = nt.nodes.new("ShaderNodeMix")
    mix_rgb.data_type = "RGBA"
    mix_rgb.inputs[6].default_value = (*clay_rgb, 1.0)
    mix_rgb.inputs[7].default_value = (*wire_rgb, 1.0)

    nt.links.new(wire.outputs["Fac"], mix_rgb.inputs["Factor"])
    nt.links.new(mix_rgb.outputs[2], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def render_shot(scene, camera, filepath: Path, location, target, res_x=1280, res_y=960):
    camera.location = Vector(location)
    look_at(camera, Vector(target))
    scene.camera = camera
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(filepath)
    print(f"RENDER -> {filepath}", flush=True)
    bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=ROOT / "ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend")
    parser.add_argument("--output", type=Path, default=ROOT / ".collab/artifacts/zone02-09-11-20261018")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else [])

    out_dir = args.output.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    source_sha_before = hashlib.sha256(args.source.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(args.source))
    scene = bpy.context.scene

    untouched_hashes_before = {
        o.name: mesh_hash(o)
        for o in scene.objects
        if o.type == "MESH" and o.name.startswith("Zone") and o.name not in TARGET_ZONES
    }
    baseline_stats = {}
    for z_name in TARGET_ZONES:
        obj = bpy.data.objects.get(z_name)
        if obj is None:
            raise RuntimeError(f"Missing {z_name} in {args.source}")
        baseline_stats[z_name] = {
            "old_vertices": len(obj.data.vertices),
            "old_triangles": sum(len(p.vertices) - 2 for p in obj.data.polygons),
            "old_sha256": mesh_hash(obj),
        }

    tex_dir = ROOT / "Assets/Textures/FarmMain3D"
    pbr_mat = create_observatory_material(
        tex_dir / "FarmMain_3D_Albedo_4K.png",
        tex_dir / "FarmMain_3D_Emission_4K.png",
    )
    clay_mat = create_wireframe_clay_material("Mat_WireClay_Main", (0.82, 0.76, 0.68), (0.11, 0.13, 0.16))
    clay_water_mat = create_wireframe_clay_material("Mat_WireClay_Water", (0.64, 0.76, 0.86), (0.08, 0.14, 0.22))

    # Build and validate target zones
    built_all = build_all_citadel_diorama_ruins_zones()
    zone_reports = {}

    for z_name in TARGET_ZONES:
        geom = built_all[z_name]
        v, uv, f = geom.arrays()
        assert np.isfinite(v).all() and np.isfinite(uv).all(), f"Non-finite arrays in {z_name}"
        assert ((uv >= 0.0) & (uv <= 1.0)).all(), f"UVs out of [0,1] in {z_name}"
        tri = v[f]
        normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
        areas = np.linalg.norm(normals, axis=1)
        assert areas.min() > 1e-9, f"Degenerate triangle in {z_name}"

        closed_info = validate_closed_parts(geom)
        assert closed_info["closed_outward_components"] and not closed_info["failures"], (
            f"Unclosed component in {z_name}: {closed_info['failures']}"
        )

        # Write Unity .meshbin artifact
        binary = meshbin_bytes(v, uv, f)
        meshbin_path = out_dir / f"{z_name}.meshbin"
        meshbin_path.write_bytes(binary)

        # Replace Blender mesh data in memory using fast foreach_set
        obj = bpy.data.objects[z_name]
        new_me = bpy.data.meshes.new(z_name + "_HighDetail")
        n_v = len(v)
        n_f = len(f)
        new_me.vertices.add(n_v)
        new_me.vertices.foreach_set("co", v.astype(np.float32).reshape(-1))
        new_me.loops.add(n_f * 3)
        new_me.loops.foreach_set("vertex_index", f.astype(np.int32).reshape(-1))
        new_me.polygons.add(n_f)
        new_me.polygons.foreach_set("loop_start", (np.arange(n_f, dtype=np.int32) * 3))
        new_me.polygons.foreach_set("loop_total", np.full(n_f, 3, dtype=np.int32))
        uv_layer = new_me.uv_layers.new(name="UVMap")
        uv_layer.data.foreach_set("uv", uv[f.reshape(-1)].astype(np.float32).reshape(-1))
        assert not new_me.validate(verbose=False), f"Blender mesh.validate failed on {z_name}"
        new_me.update()
        new_me.materials.append(pbr_mat)
        obj.data = new_me

        zone_reports[z_name] = {
            **baseline_stats[z_name],
            "parts": closed_info["parts"],
            "vertices": int(len(v)),
            "triangles": int(len(f)),
            "closed_outward_components": closed_info["closed_outward_components"],
            "failures": closed_info["failures"],
            "min_double_triangle_area": float(areas.min()),
            "bounds_min": [round(float(x), 4) for x in v.min(axis=0)],
            "bounds_max": [round(float(x), 4) for x in v.max(axis=0)],
            "meshbin_path": str(meshbin_path),
            "meshbin_sha256": hashlib.sha256(binary).hexdigest(),
        }

    # Verify untouched zones remained 100% identical
    untouched_hashes_after = {
        o.name: mesh_hash(o)
        for o in scene.objects
        if o.type == "MESH" and o.name in untouched_hashes_before
    }
    assert untouched_hashes_before == untouched_hashes_after, "An untouched zone was modified!"

    # Set up review camera & lighting stage
    stage = bpy.data.collections.new("Citadel_Diorama_Review_Stage")
    scene.collection.children.link(stage)

    cam_data = bpy.data.cameras.new("ReviewPerspCam")
    cam_data.type = "PERSP"
    cam_data.lens = 46.0
    cam_data.sensor_width = 36.0
    cam_data.clip_start = 0.2
    cam_data.clip_end = 400.0
    cam_obj = bpy.data.objects.new("ReviewPerspCam", cam_data)
    stage.objects.link(cam_obj)

    scene.render.engine = "CYCLES"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Standard"

    # Ensure zone collection is visible
    for c in bpy.data.collections:
        if "Zone" in c.name or "Mode1" in c.name:
            c.hide_render = False
        if "Mode2" in c.name:
            c.hide_render = True

    # -------------------------------------------------------------------------
    # SHOT 1 & 2: Zone09 (Twilight Citadel & Stratified Waterfall Cliff) + Zone03 Waterfalls
    # -------------------------------------------------------------------------
    visible_z09 = {"Zone09_Vista_TwilightCastle_And_WaterfallCliff", "Zone03_Water_River_Waterfalls_And_3Ponds"}
    for o in scene.objects:
        if o.type == "MESH" and o.name.startswith("Zone"):
            o.hide_render = o.name not in visible_z09

    z09_cam_loc = (13.6, -4.8, 15.8)
    z09_cam_tgt = (14.4, 13.8, 7.6)
    cam_data.lens = 48.0

    # 01: PBR
    orig_z03_mat = bpy.data.objects["Zone03_Water_River_Waterfalls_And_3Ponds"].data.materials[0]
    bpy.data.objects["Zone09_Vista_TwilightCastle_And_WaterfallCliff"].data.materials[0] = pbr_mat
    render_shot(scene, cam_obj, out_dir / "01-zone09-castle-cliff-pbr.png", z09_cam_loc, z09_cam_tgt)

    # 02: Wireframe-over-Clay
    bpy.data.objects["Zone09_Vista_TwilightCastle_And_WaterfallCliff"].data.materials[0] = clay_mat
    bpy.data.objects["Zone03_Water_River_Waterfalls_And_3Ponds"].data.materials[0] = clay_water_mat
    render_shot(scene, cam_obj, out_dir / "02-zone09-castle-cliff-wireframe-clay.png", z09_cam_loc, z09_cam_tgt)
    bpy.data.objects["Zone03_Water_River_Waterfalls_And_3Ponds"].data.materials[0] = orig_z03_mat

    # -------------------------------------------------------------------------
    # SHOT 3 & 4: Zone02 (Cutaway Rim) + Zone03 (Water/Ponds) + Zone11 (Ruins/Flora) + Full Diorama
    # -------------------------------------------------------------------------
    for o in scene.objects:
        if o.type == "MESH" and o.name.startswith("Zone"):
            o.hide_render = False

    diorama_cam_loc = (-3.2, -55.5, 31.5)
    diorama_cam_tgt = (0.5, -3.2, 2.3)
    cam_data.lens = 44.0

    # 03: PBR
    for z_n in TARGET_ZONES:
        bpy.data.objects[z_n].data.materials[0] = pbr_mat
    render_shot(scene, cam_obj, out_dir / "03-zone02-11-diorama-ruins-pbr.png", diorama_cam_loc, diorama_cam_tgt)

    # 04: Wireframe-over-Clay on Zone02, Zone03, Zone11, Zone09 (and Zone01 terrain for context)
    orig_z01_mat = bpy.data.objects["Zone01_Terrain_GoldenMeadow_And_Paths"].data.materials[0]
    for o in scene.objects:
        if o.type == "MESH" and o.name.startswith("Zone"):
            o.hide_render = o.name not in (set(TARGET_ZONES) | {"Zone01_Terrain_GoldenMeadow_And_Paths"})
    for z_n in TARGET_ZONES:
        bpy.data.objects[z_n].data.materials[0] = clay_water_mat if "Zone03" in z_n else clay_mat
    bpy.data.objects["Zone01_Terrain_GoldenMeadow_And_Paths"].data.materials[0] = clay_mat
    render_shot(scene, cam_obj, out_dir / "04-zone02-11-diorama-ruins-wireframe-clay.png", diorama_cam_loc, diorama_cam_tgt)

    # Restore materials
    bpy.data.objects["Zone01_Terrain_GoldenMeadow_And_Paths"].data.materials[0] = orig_z01_mat
    for z_n in TARGET_ZONES:
        bpy.data.objects[z_n].data.materials[0] = pbr_mat

    source_sha_after = hashlib.sha256(args.source.read_bytes()).hexdigest()
    assert source_sha_before == source_sha_after, "Source .blend file on disk was modified!"

    validation_payload = {
        "source_blend": str(args.source),
        "source_sha256": source_sha_after,
        "source_unmodified_on_disk": source_sha_before == source_sha_after,
        "other_zones_unchanged": untouched_hashes_before == untouched_hashes_after,
        "all_closed_outward_components": True,
        "zones": zone_reports,
        "renders": [
            str(out_dir / "01-zone09-castle-cliff-pbr.png"),
            str(out_dir / "02-zone09-castle-cliff-wireframe-clay.png"),
            str(out_dir / "03-zone02-11-diorama-ruins-pbr.png"),
            str(out_dir / "04-zone02-11-diorama-ruins-wireframe-clay.png"),
        ],
    }
    val_path = out_dir / "validation.json"
    val_path.write_text(json.dumps(validation_payload, indent=2), encoding="utf-8")
    print("CITADEL_DIORAMA_RUINS_VALIDATED", json.dumps(validation_payload, indent=2), flush=True)


if __name__ == "__main__":
    main()
