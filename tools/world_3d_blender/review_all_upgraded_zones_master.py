#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Master validation and Blender 5.3.0 Cycles PBR + Wireframe-Clay rendering pipeline
for ALL upgraded 3D Map zones (`Zone02, Zone04, Zone05, Zone06, Zone07, Zone08, Zone09, Zone10, Zone11`)
plus the 3-Stage Storage Granary Upgrade Showcase (`Cấp 1 -> Cấp 2 -> Cấp 3`).

Runs in a SINGLE Blender 5.3.0 session using fast `foreach_set` mesh construction.
Does NOT modify `ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend` or any file locked by Codex.
"""
import hashlib
import json
import math
import sys
import time
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

from canonical_map_helpers import MapGeometry, sample_ground_z, validate_closed_parts
from observatory_geometry import build_observatory, meshbin_bytes
from observatory_blender import create_observatory_material
from alpine_geometry import build_alpine_zone, build_alpine_massif_only
from village_buildings_geometry import (
    build_zone04_farmstead,
    build_zone06_npc_village,
    build_storage_granary_tier1,
    build_storage_granary_tier2,
    build_storage_granary_tier3,
)
from harbor_beacon_geometry import build_zone07_beacon, build_zone08_harbor_and_bridge
from citadel_diorama_ruins_geometry import (
    build_zone09_twilight_citadel,
    build_zone02_diorama_rim,
    build_zone11_ancient_ruins_and_flora,
)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def mesh_hash(obj) -> str:
    v = np.empty(len(obj.data.vertices) * 3, dtype=np.float32)
    obj.data.vertices.foreach_get("co", v)
    loops = np.empty(len(obj.data.loops), dtype=np.int32)
    obj.data.loops.foreach_get("vertex_index", loops)
    return hashlib.sha256(v.tobytes() + loops.tobytes()).hexdigest()


def create_wireframe_clay_material(name="Mat_Master_WireClay", clay_rgb=(0.86, 0.81, 0.74), wire_rgb=(0.07, 0.09, 0.12), wire_px=1.15):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.72

    wire = nt.nodes.new("ShaderNodeWireframe")
    wire.use_pixel_size = True
    wire.inputs["Size"].default_value = wire_px

    mix_node = nt.nodes.new("ShaderNodeMix")
    mix_node.data_type = "RGBA"
    mix_node.inputs[6].default_value = (*clay_rgb, 1.0)
    mix_node.inputs[7].default_value = (*wire_rgb, 1.0)

    nt.links.new(wire.outputs["Fac"], mix_node.inputs["Factor"])
    nt.links.new(mix_node.outputs[2], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def fast_blender_mesh(name: str, v: np.ndarray, uv: np.ndarray, f: np.ndarray, mat: bpy.types.Material):
    assert np.isfinite(v).all() and np.isfinite(uv).all(), f"Non-finite arrays in {name}"
    assert ((uv >= 0.0) & (uv <= 1.0)).all(), f"UVs out of [0,1] in {name}"
    tri = v[f]
    normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    areas = np.linalg.norm(normals, axis=1)
    assert areas.min() > 1e-9, f"Degenerate triangles in {name}"

    me = bpy.data.meshes.new(name)
    n_v = len(v)
    n_f = len(f)
    me.vertices.add(n_v)
    me.vertices.foreach_set("co", v.astype(np.float32).reshape(-1))
    me.loops.add(n_f * 3)
    me.loops.foreach_set("vertex_index", f.astype(np.int32).reshape(-1))
    me.polygons.add(n_f)
    me.polygons.foreach_set("loop_start", (np.arange(n_f, dtype=np.int32) * 3))
    me.polygons.foreach_set("loop_total", np.full(n_f, 3, dtype=np.int32))
    uv_layer = me.uv_layers.new(name="UVMap")
    uv_layer.data.foreach_set("uv", uv[f.reshape(-1)].astype(np.float32).reshape(-1))
    assert not me.validate(verbose=False), f"Blender mesh.validate repaired {name}"
    me.update()
    me.materials.append(mat)
    return me, float(areas.min())


def render_shot(scene, cam_obj, filepath: Path, loc, tgt, ortho_scale=None, persp_lens=None, res_x=1280, res_y=900):
    filepath.parent.mkdir(parents=True, exist_ok=True)
    if ortho_scale is not None:
        cam_obj.data.type = "ORTHO"
        cam_obj.data.ortho_scale = ortho_scale
    elif persp_lens is not None:
        cam_obj.data.type = "PERSP"
        cam_obj.data.lens = persp_lens
    cam_obj.location = Vector(loc)
    look_at(cam_obj, Vector(tgt))
    scene.camera = cam_obj
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(filepath)
    t0 = time.time()
    print(f"[MASTER RENDER] -> {filepath.name} ...", flush=True)
    bpy.ops.render.render(write_still=True)
    print(f"[MASTER RENDER DONE] {filepath.name} ({time.time() - t0:.1f}s)", flush=True)


def main():
    t_start = time.time()
    source_blend = ROOT / "ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend"
    art_root = ROOT / ".collab/artifacts"
    out_z10 = art_root / "zone10-20261018"
    out_z04_06 = art_root / "zone04-06-20261018"
    out_z07_08 = art_root / "zone07-08-20261018"
    out_z02_09_11 = art_root / "zone02-09-11-20261018"
    out_world = art_root / "world-master-20261018"
    for d in (out_z10, out_z04_06, out_z07_08, out_z02_09_11, out_world):
        d.mkdir(parents=True, exist_ok=True)

    source_sha_before = hashlib.sha256(source_blend.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(source_blend))
    scene = bpy.context.scene

    tex_dir = ROOT / "Assets/Textures/FarmMain3D"
    pbr_mat = create_observatory_material(
        tex_dir / "FarmMain_3D_Albedo_4K.png",
        tex_dir / "FarmMain_3D_Emission_4K.png",
    )
    clay_mat = create_wireframe_clay_material("Mat_Master_WireClay", (0.86, 0.81, 0.74), (0.07, 0.09, 0.12), wire_px=1.15)
    clay_floor_mat = create_wireframe_clay_material("Mat_Master_FloorClay", (0.58, 0.55, 0.50), (0.20, 0.22, 0.25), wire_px=0.65)
    clay_water_mat = create_wireframe_clay_material("Mat_Master_WaterClay", (0.64, 0.76, 0.86), (0.08, 0.14, 0.22), wire_px=0.95)

    # 1. Build and validate all candidate zones
    print("[1/4] Building & validating all closed-manifold zones...", flush=True)
    builders = {
        "Zone10_Vista_AlpineMountains_And_ForestRidge": (build_alpine_zone, out_z10),
        "Zone04_Building_KhuChuongTrai_Farmstead": (build_zone04_farmstead, out_z04_06),
        "Zone06_Building_NhaCuongHoa_And_KhuNPC": (build_zone06_npc_village, out_z04_06),
        "Zone07_Landmark_TramThongTin_InfoBeacon": (build_zone07_beacon, out_z07_08),
        "Zone08_Vista_StoneBridge_And_HarborTown": (build_zone08_harbor_and_bridge, out_z07_08),
        "Zone09_Vista_TwilightCastle_And_WaterfallCliff": (build_zone09_twilight_citadel, out_z02_09_11),
        "Zone02_Diorama_Cutaway_RockRim_And_Base": (build_zone02_diorama_rim, out_z02_09_11),
        "Zone11_Props_AncientRuins_And_GoldenFlora": (build_zone11_ancient_ruins_and_flora, out_z02_09_11),
    }

    summary_metrics = {}
    for z_name, (fn, out_dir) in builders.items():
        geom = fn()
        rep = validate_closed_parts(geom)
        assert rep["closed_outward_components"] and not rep["failures"], f"{z_name} failed manifold validation: {rep['failures']}"
        v, uv, f = geom.arrays()
        binary = meshbin_bytes(v, uv, f)
        bin_path = out_dir / f"{z_name}.meshbin"
        bin_path.write_bytes(binary)

        obj = bpy.data.objects[z_name]
        old_tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
        me, min_area = fast_blender_mesh(z_name + "_Upgraded", v, uv, f, pbr_mat)
        obj.data = me

        summary_metrics[z_name] = {
            "old_triangles": old_tris,
            "parts": rep["parts"],
            "vertices": int(len(v)),
            "triangles": int(len(f)),
            "closed_outward_components": True,
            "failures": [],
            "min_double_triangle_area": min_area,
            "bounds_min": [round(float(x), 4) for x in v.min(axis=0)],
            "bounds_max": [round(float(x), 4) for x in v.max(axis=0)],
            "meshbin_path": str(bin_path),
            "meshbin_sha256": hashlib.sha256(binary).hexdigest(),
        }
        print(f"  [OK] {z_name}: {rep['parts']} closed parts, {len(f)} tris", flush=True)

    # Also build Zone10 Massif-Only option
    m_only = build_alpine_massif_only()
    mv, muv, mf = m_only.arrays()
    mbin = meshbin_bytes(mv, muv, mf)
    (out_z10 / "Zone10_MassifOnly.meshbin").write_bytes(mbin)

    # Also swap Zone05 (`Phòng Nghiên Cứu`) in memory so the full-world preview has all 9 upgraded zones
    obs_geom = build_observatory()
    ov, ouv, of = obs_geom.arrays((-7.8, 6.4, 3.2412), yaw=-0.05)
    obs_me, _ = fast_blender_mesh("Zone05_Observatory_Upgraded", ov, ouv, of, pbr_mat)
    bpy.data.objects["Zone05_Building_NhaNghienCuu_Observatory"].data = obs_me

    # Also build the 3 Storage Granary Upgrade Tiers (`Cấp 1, Cấp 2, Cấp 3`)
    g_t1 = build_storage_granary_tier1()
    g_t2 = build_storage_granary_tier2()
    g_t3 = build_storage_granary_tier3()
    gran_metrics = {}
    for idx_t, gt in enumerate((g_t1, g_t2, g_t3), start=1):
        rep_t = validate_closed_parts(gt)
        assert rep_t["closed_outward_components"] and not rep_t["failures"], f"Granary Tier {idx_t} failed: {rep_t['failures']}"
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
    gran_me, _ = fast_blender_mesh("StorageGranary_3Tiers_Mesh", gv, guv, gf, pbr_mat)
    gran_obj = bpy.data.objects.new("StorageGranary_3Tiers_Obj", gran_me)
    scene.collection.objects.link(gran_obj)
    gran_obj.hide_render = True

    # Configure Cycles rendering
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.threads_mode = "FIXED"
    scene.render.threads = 6
    scene.view_settings.view_transform = "Standard"

    for c in bpy.data.collections:
        if "Zone" in c.name or "Mode1" in c.name:
            c.hide_render = False
        if "Mode2" in c.name:
            c.hide_render = True

    cam_data = bpy.data.cameras.new("MasterReviewCam")
    cam_data.clip_start = 0.1
    cam_data.clip_end = 400.0
    cam_obj = bpy.data.objects.new("MasterReviewCam", cam_data)
    scene.collection.objects.link(cam_obj)

    all_zone_objs = [o for o in scene.objects if o.type == "MESH" and o.name.startswith("Zone")]
    orig_z01_mat = bpy.data.objects["Zone01_Terrain_GoldenMeadow_And_Paths"].data.materials[0]
    orig_z03_mat = bpy.data.objects["Zone03_Water_River_Waterfalls_And_3Ponds"].data.materials[0]

    def set_visible_zones(visible_names, wire_mode=False):
        for o in all_zone_objs:
            o.hide_render = o.name not in visible_names
            if not o.hide_render:
                if wire_mode:
                    if "Zone01" in o.name:
                        o.data.materials[0] = clay_floor_mat
                    elif "Zone03" in o.name:
                        o.data.materials[0] = clay_water_mat
                    else:
                        o.data.materials[0] = clay_mat
                else:
                    if "Zone01" in o.name:
                        o.data.materials[0] = orig_z01_mat
                    elif "Zone03" in o.name:
                        o.data.materials[0] = orig_z03_mat
                    else:
                        o.data.materials[0] = pbr_mat

    # -------------------------------------------------------------------------
    # A. Render Zone10 v3 (Multi-Layered Alpine Cordillera + Stepped NW Rocky Highland Cliff)
    # -------------------------------------------------------------------------
    z10_vis = {"Zone10_Vista_AlpineMountains_And_ForestRidge", "Zone01_Terrain_GoldenMeadow_And_Paths"}
    set_visible_zones({ "Zone10_Vista_AlpineMountains_And_ForestRidge" }, wire_mode=False)
    render_shot(scene, cam_obj, out_z10 / "02-after-front.png", (-4.0, -19.5, 20.8), (-8.0, 14.5, 6.8), ortho_scale=36.0, res_x=1280, res_y=840)

    set_visible_zones(z10_vis, wire_mode=False)
    render_shot(scene, cam_obj, out_z10 / "06-ridge-close.png", (-13.0, -14.0, 15.8), (-14.2, 13.5, 7.6), ortho_scale=19.5, res_x=1280, res_y=840)

    set_visible_zones({ "Zone10_Vista_AlpineMountains_And_ForestRidge" }, wire_mode=True)
    render_shot(scene, cam_obj, out_z10 / "08-after-wireframe-clay.png", (-4.0, -19.5, 20.8), (-8.0, 14.5, 6.8), ortho_scale=36.0, res_x=1280, res_y=840)

    # -------------------------------------------------------------------------
    # B. Render Zone04 v2 (Farm_Core: Main Homestead + Bạch Vân, Tier 3 Granary, Construction Guild + A-Frame Crane)
    # -------------------------------------------------------------------------
    z04_vis = {"Zone04_Building_KhuChuongTrai_Farmstead", "Zone01_Terrain_GoldenMeadow_And_Paths"}
    set_visible_zones(z04_vis, wire_mode=False)
    render_shot(scene, cam_obj, out_z04_06 / "01-zone04-front-pbr.png", (-11.2, -1.8, 10.2), (-16.1, 7.6, 4.75), ortho_scale=11.4, res_x=1280, res_y=900)

    set_visible_zones(z04_vis, wire_mode=True)
    render_shot(scene, cam_obj, out_z04_06 / "02-zone04-wireframe-clay-proof.png", (-11.2, -1.8, 10.2), (-16.1, 7.6, 4.75), ortho_scale=11.4, res_x=1280, res_y=900)

    # -------------------------------------------------------------------------
    # C. Render Zone06 v2 (Octagonal Enhancement Sanctum at y=7.6 + Transition Gate + Elevated Rear Hub_Village at y=11.2..14.6)
    # -------------------------------------------------------------------------
    z06_vis = {
        "Zone06_Building_NhaCuongHoa_And_KhuNPC",
        "Zone05_Building_NhaNghienCuu_Observatory",
        "Zone01_Terrain_GoldenMeadow_And_Paths",
    }
    set_visible_zones(z06_vis, wire_mode=False)
    render_shot(scene, cam_obj, out_z04_06 / "03-zone06-front-pbr.png", (-4.8, -1.2, 12.2), (0.0, 10.4, 5.15), ortho_scale=12.6, res_x=1280, res_y=900)

    set_visible_zones(z06_vis, wire_mode=True)
    render_shot(scene, cam_obj, out_z04_06 / "04-zone06-wireframe-clay-proof.png", (-4.8, -1.2, 12.2), (0.0, 10.4, 5.15), ortho_scale=12.6, res_x=1280, res_y=900)

    # -------------------------------------------------------------------------
    # D. Render 3-Stage Storage Granary Upgrade Showcase (`Cấp 1 -> Cấp 2 -> Cấp 3`)
    # -------------------------------------------------------------------------
    set_visible_zones(set(), wire_mode=False)
    gran_obj.hide_render = False
    gran_obj.data.materials[0] = pbr_mat
    render_shot(scene, cam_obj, out_z04_06 / "05-storage-granary-3-upgrade-tiers-pbr.png", (0.0, -9.5, 4.8), (0.1, 0.1, 1.35), ortho_scale=14.8, res_x=1440, res_y=720)

    gran_obj.data.materials[0] = clay_mat
    render_shot(scene, cam_obj, out_z04_06 / "06-storage-granary-3-upgrade-tiers-wireframe.png", (0.0, -9.5, 4.8), (0.1, 0.1, 1.35), ortho_scale=14.8, res_x=1440, res_y=720)
    gran_obj.hide_render = True

    # -------------------------------------------------------------------------
    # E. Render Zone07 v2 (Astrological Armillary Beacon) & Zone08 v2 (Ancient European `Âu Cổ` Harbor Town)
    # -------------------------------------------------------------------------
    bz07 = float(sample_ground_z(8.6, 4.6))
    z07_vis = {"Zone07_Landmark_TramThongTin_InfoBeacon", "Zone01_Terrain_GoldenMeadow_And_Paths"}
    set_visible_zones(z07_vis, wire_mode=False)
    render_shot(scene, cam_obj, out_z07_08 / "01-zone07-beacon-pbr.png", (1.1, -4.9, bz07 + 6.4), (8.6, 4.6, bz07 + 2.35), ortho_scale=7.2, res_x=1080, res_y=1080)

    set_visible_zones(z07_vis, wire_mode=True)
    render_shot(scene, cam_obj, out_z07_08 / "02-zone07-beacon-wireframe-clay.png", (1.1, -4.9, bz07 + 6.4), (8.6, 4.6, bz07 + 2.35), ortho_scale=7.2, res_x=1080, res_y=1080)

    z08_vis = {
        "Zone08_Vista_StoneBridge_And_HarborTown",
        "Zone01_Terrain_GoldenMeadow_And_Paths",
        "Zone03_Water_River_Waterfalls_And_3Ponds",
    }
    set_visible_zones(z08_vis, wire_mode=False)
    render_shot(scene, cam_obj, out_z07_08 / "03-zone08-harbor-bridge-pbr.png", (5.5, -17.5, 18.2), (19.6, 3.2, 4.4), ortho_scale=21.2, res_x=1440, res_y=960)

    set_visible_zones(z08_vis, wire_mode=True)
    render_shot(scene, cam_obj, out_z07_08 / "04-zone08-harbor-bridge-wireframe-clay.png", (5.5, -17.5, 18.2), (19.6, 3.2, 4.4), ortho_scale=21.2, res_x=1440, res_y=960)

    # -------------------------------------------------------------------------
    # F. Render Zone09 (Twilight Citadel & Waterfall Cliff) + Zone02 & Zone11 (Cutaway Rim & Ruins/Flora)
    # -------------------------------------------------------------------------
    z09_vis = {"Zone09_Vista_TwilightCastle_And_WaterfallCliff", "Zone03_Water_River_Waterfalls_And_3Ponds"}
    set_visible_zones(z09_vis, wire_mode=False)
    render_shot(scene, cam_obj, out_z02_09_11 / "01-zone09-castle-cliff-pbr.png", (13.6, -4.8, 15.8), (14.4, 13.8, 7.6), persp_lens=48.0, res_x=1280, res_y=960)

    set_visible_zones(z09_vis, wire_mode=True)
    render_shot(scene, cam_obj, out_z02_09_11 / "02-zone09-castle-cliff-wireframe-clay.png", (13.6, -4.8, 15.8), (14.4, 13.8, 7.6), persp_lens=48.0, res_x=1280, res_y=960)

    z02_11_vis = {
        "Zone02_Diorama_Cutaway_RockRim_And_Base",
        "Zone11_Props_AncientRuins_And_GoldenFlora",
        "Zone01_Terrain_GoldenMeadow_And_Paths",
        "Zone03_Water_River_Waterfalls_And_3Ponds",
    }
    set_visible_zones(z02_11_vis, wire_mode=False)
    render_shot(scene, cam_obj, out_z02_09_11 / "03-zone02-11-diorama-ruins-pbr.png", (-2.8, -50.0, 26.5), (0.0, -4.5, 2.2), persp_lens=46.0, res_x=1280, res_y=900)

    set_visible_zones(z02_11_vis, wire_mode=True)
    render_shot(scene, cam_obj, out_z02_09_11 / "04-zone02-11-diorama-ruins-wireframe-clay.png", (-2.8, -50.0, 26.5), (0.0, -4.5, 2.2), persp_lens=46.0, res_x=1280, res_y=900)

    # -------------------------------------------------------------------------
    # G. Render Full-World Master Preview (ALL 11 Zones Visible with All 9 Upgraded Zones Swapped In!)
    # -------------------------------------------------------------------------
    all_names = {o.name for o in all_zone_objs}
    set_visible_zones(all_names, wire_mode=False)
    render_shot(scene, cam_obj, out_world / "01-full-world-upgraded-pbr.png", (-3.2, -55.5, 31.5), (0.5, -3.2, 2.3), persp_lens=43.0, res_x=1600, res_y=1120)
    render_shot(scene, cam_obj, out_z10 / "07-world-context.png", (-3.2, -55.5, 31.5), (0.5, -3.2, 2.3), persp_lens=43.0, res_x=1440, res_y=1020)

    set_visible_zones(all_names, wire_mode=True)
    render_shot(scene, cam_obj, out_world / "02-full-world-upgraded-wireframe-clay.png", (-3.2, -55.5, 31.5), (0.5, -3.2, 2.3), persp_lens=43.0, res_x=1600, res_y=1120)

    source_sha_after = hashlib.sha256(source_blend.read_bytes()).hexdigest()
    assert source_sha_before == source_sha_after, "Source master .blend on disk was modified!"

    # Write validation JSONs
    (out_z10 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha_after,
        "source_unmodified_on_disk": True,
        "zone10": summary_metrics["Zone10_Vista_AlpineMountains_And_ForestRidge"],
        "massif_only_meshbin_sha256": hashlib.sha256(mbin).hexdigest(),
    }, indent=2), encoding="utf-8")

    (out_z04_06 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha_after,
        "source_unmodified_on_disk": True,
        "zone04": summary_metrics["Zone04_Building_KhuChuongTrai_Farmstead"],
        "zone06": summary_metrics["Zone06_Building_NhaCuongHoa_And_KhuNPC"],
        "storage_granary_tiers": gran_metrics,
    }, indent=2), encoding="utf-8")

    (out_z07_08 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha_after,
        "source_unmodified_on_disk": True,
        "zone07": summary_metrics["Zone07_Landmark_TramThongTin_InfoBeacon"],
        "zone08": summary_metrics["Zone08_Vista_StoneBridge_And_HarborTown"],
    }, indent=2), encoding="utf-8")

    (out_z02_09_11 / "validation.json").write_text(json.dumps({
        "source_sha256": source_sha_after,
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
        "source_sha256": source_sha_after,
        "source_unmodified_on_disk": True,
        "elapsed_seconds": round(time.time() - t_start, 2),
        "zones": summary_metrics,
        "storage_granary_tiers": gran_metrics,
    }
    (out_world / "validation.json").write_text(json.dumps(master_report, indent=2), encoding="utf-8")
    print("MASTER_ALL_ZONES_VALIDATED_AND_RENDERED", json.dumps({"elapsed": master_report["elapsed_seconds"]}), flush=True)


if __name__ == "__main__":
    main()
