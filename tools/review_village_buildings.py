r"""Build, validate, and render isolated Zone04 (Farm_Core), Zone06 (Enhancement Sanctum + Elevated Hub_Village),
and the 3-Stage Storage Granary Upgrade Showcase (Cấp 1 -> Cấp 2 -> Cấp 3) in Blender 5.3.0.

Run with:
  F:\Tools\blender-5.3.0-windows-x64\blender.exe --background --python-exit-code 1 --python D:\Pig-tycoon\Tools\review_village_buildings.py
"""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'Tools'))

from canonical_map_helpers import (
    MapGeometry,
    WORLD_X_MAX,
    WORLD_Y_MAX,
    sample_ground_z,
    validate_closed_parts,
)
from observatory_geometry import meshbin_bytes
from observatory_blender import create_observatory_material
from village_buildings_geometry import (
    build_zone04_farmstead,
    build_zone06_npc_village,
    build_storage_granary_tier1,
    build_storage_granary_tier2,
    build_storage_granary_tier3,
)

ZONE04_NAME = 'Zone04_Building_KhuChuongTrai_Farmstead'
ZONE06_NAME = 'Zone06_Building_NhaCuongHoa_And_KhuNPC'


def find_texture_dir() -> Path:
    candidates = [
        ROOT / 'Assets/Textures/FarmMain3D',
        Path(r'F:/Unity/Pig tycoon/Assets/Textures/FarmMain3D'),
    ]
    for c in candidates:
        if (c / 'FarmMain_3D_Albedo_4K.png').exists():
            return c
    raise FileNotFoundError('Could not locate FarmMain_3D_Albedo_4K.png in Assets/Textures/FarmMain3D')


def create_wireframe_clay_material(name: str = 'Mat_Wireframe_Clay_Proof'):
    """Cycles material combining warm Ambient-Occlusion clay with crisp pixel-width 3D wireframe edges."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.78

    ao = nt.nodes.new('ShaderNodeAmbientOcclusion')
    ao.samples = 12
    ao.inputs['Distance'].default_value = 0.42
    ao.inputs['Color'].default_value = (0.90, 0.85, 0.78, 1.0)

    wire = nt.nodes.new('ShaderNodeWireframe')
    wire.use_pixel_size = True
    wire.inputs['Size'].default_value = 1.15

    mix_col = nt.nodes.new('ShaderNodeMix')
    mix_col.data_type = 'RGBA'
    mix_col.inputs[6].default_value = (0.90, 0.85, 0.78, 1.0)
    mix_col.inputs[7].default_value = (0.05, 0.06, 0.08, 1.0)

    nt.links.new(wire.outputs['Fac'], mix_col.inputs[0])
    nt.links.new(ao.outputs['Color'], mix_col.inputs[6])
    nt.links.new(mix_col.outputs[2], bsdf.inputs['Base Color'])
    nt.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return mat


def create_clay_ground_material(name: str = 'Mat_Clay_Floor'):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (0.34, 0.33, 0.31, 1.0)
        bsdf.inputs['Roughness'].default_value = 0.92
    return mat


def build_terrain_patch_object(name: str, x0: float, x1: float, y0: float, y1: float, nx: int = 80, ny: int = 80):
    xs = np.linspace(x0, x1, nx, dtype=np.float32)
    ys = np.linspace(y0, y1, ny, dtype=np.float32)
    gx, gy = np.meshgrid(xs, ys)
    gz = sample_ground_z(gx, gy) - 0.015

    u_grid = (gx / (2.0 * WORLD_X_MAX)) + 0.5
    v_grid = 0.255 + 0.740 * ((gy / (2.0 * WORLD_Y_MAX)) + 0.5)

    verts = np.stack([gx, gy, gz], axis=-1).reshape(-1, 3)
    uvs = np.stack([u_grid, v_grid], axis=-1).reshape(-1, 2)
    faces = []
    for j in range(ny - 1):
        for i in range(nx - 1):
            a = j * nx + i
            faces.append((a, a + 1, a + nx + 1, a + nx))

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts.tolist(), [], faces)
    uv_layer = mesh.uv_layers.new(name='UVMap')
    loop_vi = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get('vertex_index', loop_vi)
    uv_layer.data.foreach_set('uv', uvs[loop_vi].reshape(-1))
    for poly in mesh.polygons:
        poly.use_smooth = True
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def build_blender_zone_object(name: str, geom):
    v_arr, uv_arr, f_arr = geom.arrays()
    assert np.isfinite(v_arr).all() and np.isfinite(uv_arr).all()
    assert f_arr.min() >= 0 and f_arr.max() < len(v_arr)
    assert ((uv_arr >= 0.0) & (uv_arr <= 1.0)).all()

    tri = v_arr[f_arr]
    normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    areas = np.linalg.norm(normals, axis=1)
    assert areas.min() > 1e-9, f'{name} has degenerate triangles'

    mesh = bpy.data.meshes.new(name)
    n_v = len(v_arr)
    n_f = len(f_arr)
    mesh.vertices.add(n_v)
    mesh.vertices.foreach_set('co', v_arr.astype(np.float32).reshape(-1))
    mesh.loops.add(n_f * 3)
    mesh.loops.foreach_set('vertex_index', f_arr.astype(np.int32).reshape(-1))
    mesh.polygons.add(n_f)
    mesh.polygons.foreach_set('loop_start', (np.arange(n_f, dtype=np.int32) * 3))
    mesh.polygons.foreach_set('loop_total', np.full(n_f, 3, dtype=np.int32))
    uv_layer = mesh.uv_layers.new(name='UVMap')
    uv_layer.data.foreach_set('uv', uv_arr[f_arr.reshape(-1)].astype(np.float32).reshape(-1))
    assert not mesh.validate(verbose=False), f'Blender repaired invalid geometry in {name}'
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj, v_arr, uv_arr, f_arr, float(areas.min())


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def render_view(scene, camera, filepath: Path, cam_loc, target_loc, ortho_scale: float, res_x: int = 1280, res_y: int = 920):
    camera.location = Vector(cam_loc)
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = ortho_scale
    look_at(camera, Vector(target_loc))
    scene.camera = camera
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(filepath)
    print('RENDER', filepath, flush=True)
    bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, default=ROOT / '.collab/artifacts/zone04-06-20261018')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    out_dir = args.output.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    # 1. Build & validate closed outward manifold components for Zone04, Zone06, and all 3 Storage Granary Tiers
    z04 = build_zone04_farmstead()
    rep04 = validate_closed_parts(z04)
    assert rep04['closed_outward_components'] is True and rep04['failures'] == [], f'Zone04 failed: {rep04}'

    z06 = build_zone06_npc_village()
    rep06 = validate_closed_parts(z06)
    assert rep06['closed_outward_components'] is True and rep06['failures'] == [], f'Zone06 failed: {rep06}'

    g_t1 = build_storage_granary_tier1()
    g_t2 = build_storage_granary_tier2()
    g_t3 = build_storage_granary_tier3()
    for t_idx, gt in enumerate((g_t1, g_t2, g_t3), start=1):
        rep_t = validate_closed_parts(gt)
        assert rep_t['closed_outward_components'] is True and rep_t['failures'] == [], f'Granary Tier {t_idx} failed: {rep_t}'

    granary_lineup = MapGeometry()
    granary_lineup.merge_from(g_t1, origin=(-5.2, 0.0, 0.0), yaw=0.12)
    granary_lineup.merge_from(g_t2, origin=(-0.2, 0.0, 0.0), yaw=0.10)
    granary_lineup.merge_from(g_t3, origin=( 5.4, 0.0, 0.0), yaw=0.08)

    # 2. Setup clean Blender 5.3.0 review stage
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 28
    scene.cycles.use_denoising = True
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 6
    scene.view_settings.view_transform = 'AgX'

    world = bpy.data.worlds.new('Village_Review_World')
    world.use_nodes = True
    bg = world.node_tree.nodes.get('Background')
    if bg:
        bg.inputs[0].default_value = (0.34, 0.38, 0.46, 1.0)
        bg.inputs[1].default_value = 0.55
    scene.world = world

    sun_key = bpy.data.lights.new('SunKey', 'SUN')
    sun_key.energy = 3.2
    sun_key.color = (1.0, 0.92, 0.80)
    sun_key.angle = math.radians(8.0)
    sun_key_obj = bpy.data.objects.new('SunKey', sun_key)
    scene.collection.objects.link(sun_key_obj)
    sun_key_obj.rotation_euler = (math.radians(48.0), math.radians(-22.0), math.radians(30.0))

    sun_fill = bpy.data.lights.new('SunFill', 'SUN')
    sun_fill.energy = 1.15
    sun_fill.color = (0.85, 0.90, 1.0)
    sun_fill.angle = math.radians(25.0)
    sun_fill_obj = bpy.data.objects.new('SunFill', sun_fill)
    scene.collection.objects.link(sun_fill_obj)
    sun_fill_obj.rotation_euler = (math.radians(38.0), math.radians(26.0), math.radians(-140.0))

    cam_data = bpy.data.cameras.new('ReviewCamera')
    cam_data.clip_start = 0.1
    cam_data.clip_end = 250.0
    camera = bpy.data.objects.new('ReviewCamera', cam_data)
    scene.collection.objects.link(camera)

    tex_dir = find_texture_dir()
    mat_pbr = create_observatory_material(
        tex_dir / 'FarmMain_3D_Albedo_4K.png',
        tex_dir / 'FarmMain_3D_Emission_4K.png',
    )
    mat_wire_clay = create_wireframe_clay_material()
    mat_clay_floor = create_clay_ground_material()

    # 3. Create Zone04, Zone06 & Granary Lineup Blender objects + export .meshbin files
    obj04, v04, uv04, f04, min_area04 = build_blender_zone_object(ZONE04_NAME, z04)
    obj06, v06, uv06, f06, min_area06 = build_blender_zone_object(ZONE06_NAME, z06)
    obj_gran, vg, uvg, fg, _ = build_blender_zone_object('StorageGranary_3Tiers_Lineup', granary_lineup)

    bin04 = meshbin_bytes(v04, uv04, f04)
    bin06 = meshbin_bytes(v06, uv06, f06)
    (out_dir / f'{ZONE04_NAME}.meshbin').write_bytes(bin04)
    (out_dir / f'{ZONE06_NAME}.meshbin').write_bytes(bin06)

    for t_idx, gt in enumerate((g_t1, g_t2, g_t3), start=1):
        vt, uvt, ft = gt.arrays()
        (out_dir / f'StorageGranary_Tier{t_idx}.meshbin').write_bytes(meshbin_bytes(vt, uvt, ft))

    terrain04 = build_terrain_patch_object('TerrainPatch_Zone04', -27.5, -3.5, -3.0, 12.5, nx=96, ny=64)
    terrain06 = build_terrain_patch_object('TerrainPatch_Zone06', -6.0, 7.5, 4.0, 16.5, nx=72, ny=64)

    # 4. Render Zone04 Front PBR & Wireframe-Clay Proof
    obj04.hide_render = False
    terrain04.hide_render = False
    obj06.hide_render = True
    terrain06.hide_render = True
    obj_gran.hide_render = True

    z04_cam_loc = (-11.2, -1.8, 10.2)
    z04_target = (-16.1, 7.6, 4.75)
    z04_ortho = 11.4

    obj04.data.materials.clear()
    obj04.data.materials.append(mat_pbr)
    terrain04.data.materials.clear()
    terrain04.data.materials.append(mat_pbr)
    render_view(scene, camera, out_dir / '01-zone04-front-pbr.png', z04_cam_loc, z04_target, z04_ortho)

    obj04.data.materials.clear()
    obj04.data.materials.append(mat_wire_clay)
    terrain04.data.materials.clear()
    terrain04.data.materials.append(mat_clay_floor)
    render_view(scene, camera, out_dir / '02-zone04-wireframe-clay-proof.png', z04_cam_loc, z04_target, z04_ortho)

    # 5. Render Zone06 (Enhancement Sanctum at y=7.6 + Transition Gate + Elevated Hub_Village at y=11.2..14.6)
    obj04.hide_render = True
    terrain04.hide_render = True
    obj06.hide_render = False
    terrain06.hide_render = False
    obj_gran.hide_render = True

    z06_cam_loc = (-5.2, -0.5, 11.8)
    z06_target = (0.2, 10.6, 5.10)
    z06_ortho = 11.8

    obj06.data.materials.clear()
    obj06.data.materials.append(mat_pbr)
    terrain06.data.materials.clear()
    terrain06.data.materials.append(mat_pbr)
    render_view(scene, camera, out_dir / '03-zone06-front-pbr.png', z06_cam_loc, z06_target, z06_ortho)

    obj06.data.materials.clear()
    obj06.data.materials.append(mat_wire_clay)
    terrain06.data.materials.clear()
    terrain06.data.materials.append(mat_clay_floor)
    render_view(scene, camera, out_dir / '04-zone06-wireframe-clay-proof.png', z06_cam_loc, z06_target, z06_ortho)

    # 6. Render 3-Stage Storage Granary Upgrade Showcase (Cấp 1 -> Cấp 2 -> Cấp 3)
    obj06.hide_render = True
    terrain06.hide_render = True
    obj_gran.hide_render = False

    gran_cam_loc = (0.0, -9.5, 4.8)
    gran_target = (0.1, 0.1, 1.35)
    gran_ortho = 14.8

    obj_gran.data.materials.clear()
    obj_gran.data.materials.append(mat_pbr)
    render_view(scene, camera, out_dir / '05-storage-granary-3-upgrade-tiers-pbr.png', gran_cam_loc, gran_target, gran_ortho, res_x=1440, res_y=720)

    obj_gran.data.materials.clear()
    obj_gran.data.materials.append(mat_wire_clay)
    render_view(scene, camera, out_dir / '06-storage-granary-3-upgrade-tiers-wireframe.png', gran_cam_loc, gran_target, gran_ortho, res_x=1440, res_y=720)

    report = {
        'zone04': {
            'name': ZONE04_NAME,
            'parts': rep04['parts'],
            'vertices': int(len(v04)),
            'triangles': int(len(f04)),
            'closed_outward_components': rep04['closed_outward_components'],
            'failures': rep04['failures'],
            'min_double_triangle_area': min_area04,
            'bounds_min': v04.min(axis=0).tolist(),
            'bounds_max': v04.max(axis=0).tolist(),
            'meshbin_sha256': hashlib.sha256(bin04).hexdigest(),
        },
        'zone06': {
            'name': ZONE06_NAME,
            'parts': rep06['parts'],
            'vertices': int(len(v06)),
            'triangles': int(len(f06)),
            'closed_outward_components': rep06['closed_outward_components'],
            'failures': rep06['failures'],
            'min_double_triangle_area': min_area06,
            'bounds_min': v06.min(axis=0).tolist(),
            'bounds_max': v06.max(axis=0).tolist(),
            'meshbin_sha256': hashlib.sha256(bin06).hexdigest(),
        },
        'storage_granary_tiers': {
            f'tier_{i}': {
                'parts': len(gt.parts),
                'vertices': int(len(gt.vertices)),
                'triangles': int(len(gt.faces)),
            }
            for i, gt in enumerate((g_t1, g_t2, g_t3), start=1)
        },
    }
    (out_dir / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('VILLAGE_BUILDINGS_VALIDATED', json.dumps(report), flush=True)


if __name__ == '__main__':
    main()
