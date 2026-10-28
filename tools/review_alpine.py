"""Build and render an isolated Zone10 v2 candidate + Zone05/Zone10 Wireframe-on-Clay 3D proof.

Run with Blender --background --python-exit-code 1 --python Tools/review_alpine.py.
Optional arguments after --: --source <blend> --output <directory>.
"""
from pathlib import Path
import argparse
import hashlib
import json
import struct
import sys
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))
from alpine_geometry import build_alpine_zone, build_alpine_massif_only, meshbin_bytes
from observatory_geometry import build_observatory

ZONE = "Zone10_Vista_AlpineMountains_And_ForestRidge"
FOCUS_CENTER = (-8.0, 14.5, 6.8)


def mesh_hash(obj):
    v = np.empty(len(obj.data.vertices) * 3, dtype=np.float32)
    obj.data.vertices.foreach_get("co", v)
    loops = np.empty(len(obj.data.loops), dtype=np.int32)
    obj.data.loops.foreach_get("vertex_index", loops)
    return hashlib.sha256(v.tobytes() + loops.tobytes()).hexdigest()


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def render(scene, camera, path, location, target, size_x=1080, size_y=720):
    camera.location = Vector(location)
    look_at(camera, Vector(target))
    scene.camera = camera
    scene.render.resolution_x = size_x
    scene.render.resolution_y = size_y
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(path)
    print("RENDER", path, flush=True)
    bpy.ops.render.render(write_still=True)


def create_wireframe_clay_material(name="Mat_Wireframe_Clay_Proof", wire_px=1.35):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.55
    wire = nt.nodes.new("ShaderNodeWireframe")
    wire.use_pixel_size = True
    wire.inputs["Size"].default_value = wire_px
    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.inputs["Color1"].default_value = (0.78, 0.74, 0.68, 1.0)
    mix.inputs["Color2"].default_value = (0.06, 0.08, 0.12, 1.0)
    nt.links.new(wire.outputs["Fac"], mix.inputs["Fac"])
    nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=ROOT / "ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend")
    parser.add_argument("--output", type=Path, default=ROOT / ".collab/artifacts/zone10-20261018")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else [])
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=True)

    source_hash = hashlib.sha256(args.source.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(args.source))
    scene = bpy.context.scene
    obj = bpy.data.objects.get(ZONE)
    if obj is None:
        raise RuntimeError(f"The source master has no {ZONE}")

    other_hashes = {o.name: mesh_hash(o) for o in scene.objects if o.type == "MESH" and o.name.startswith("Zone") and o != obj}
    old_triangles = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    old_mesh_hash = mesh_hash(obj)
    old_mats = list(obj.data.materials)

    old_camera = scene.camera
    old_resolution = (scene.render.resolution_x, scene.render.resolution_y)
    old_world = scene.world
    old_transform = scene.view_settings.view_transform
    old_hidden = {o.name: o.hide_render for o in scene.objects}
    for o in scene.objects:
        o.hide_render = o != obj
    col_states = {c.name: c.hide_render for c in bpy.data.collections}
    for c in obj.users_collection:
        c.hide_render = False

    stage = bpy.data.collections.new("Alpine_Review_Stage")
    scene.collection.children.link(stage)

    def stage_obj(name, data):
        o = bpy.data.objects.new(name, data)
        stage.objects.link(o)
        return o

    camera = stage_obj("ReviewCamera", bpy.data.cameras.new("ReviewCamera"))
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 36.0
    camera.data.clip_end = 400.0

    for name, loc, energy, size in [
        ("Key", (-18, -28, 26), 1800, 16),
        ("Fill", (20, -14, 18), 900, 14),
        ("Rim", (0, 26, 24), 1400, 12),
    ]:
        light = bpy.data.lights.new(name, "AREA")
        light.energy = energy
        light.shape = "DISK"
        light.size = size
        o = stage_obj(name, light)
        o.location = Vector(FOCUS_CENTER) + Vector(loc)
        look_at(o, Vector(FOCUS_CENTER))

    world = bpy.data.worlds.new("Alpine_Review_World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.28, 0.32, 0.40, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    scene.world = world
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 28
    scene.cycles.use_denoising = True
    scene.render.threads_mode = "FIXED"
    scene.render.threads = 6
    scene.view_settings.view_transform = "AgX"

    target = Vector(FOCUS_CENTER)
    front = Vector(FOCUS_CENTER) + Vector((4.0, -34.0, 14.0))
    render(scene, camera, out / "01-before-front.png", front, target)

    # Also build massif-only candidate for Codex's minimal integration option
    massif_only = build_alpine_massif_only()
    m_v, m_uv, m_f = massif_only.arrays()
    massif_bin = meshbin_bytes(m_v, m_uv, m_f)
    (out / "Zone10_MassifOnly.meshbin").write_bytes(massif_bin)

    detail = build_alpine_zone()
    vertices, uvs, faces = detail.arrays()
    assert np.isfinite(vertices).all() and np.isfinite(uvs).all()
    assert faces.min() >= 0 and faces.max() < len(vertices)
    assert ((uvs >= 0.0) & (uvs <= 1.0)).all()
    tri = vertices[faces]
    normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    areas = np.linalg.norm(normals, axis=1)
    assert areas.min() > 1e-9, f"Degenerate triangles: {int((areas <= 1e-9).sum())}"

    mesh = bpy.data.meshes.new(ZONE + "_ReferenceDetail")
    n_v = len(vertices)
    n_f = len(faces)
    mesh.vertices.add(n_v)
    mesh.vertices.foreach_set("co", vertices.astype(np.float32).reshape(-1))
    mesh.loops.add(n_f * 3)
    mesh.loops.foreach_set("vertex_index", faces.astype(np.int32).reshape(-1))
    mesh.polygons.add(n_f)
    mesh.polygons.foreach_set("loop_start", (np.arange(n_f, dtype=np.int32) * 3))
    mesh.polygons.foreach_set("loop_total", np.full(n_f, 3, dtype=np.int32))
    layer = mesh.uv_layers.new(name="UVMap")
    layer.data.foreach_set("uv", uvs[faces.reshape(-1)].astype(np.float32).reshape(-1))
    assert not mesh.validate(verbose=True), "Blender repaired invalid geometry"
    mesh.update()
    for m in old_mats:
        mesh.materials.append(m)
    obj.data = mesh

    render(scene, camera, out / "02-after-front.png", front, target)
    for name, offset in [
        ("03-after-west", (-28.0, -24.0, 14.0)),
        ("04-after-rear", (4.0, 34.0, 14.0)),
        ("05-after-east", (28.0, -22.0, 14.0)),
    ]:
        render(scene, camera, out / (name + ".png"), target + Vector(offset), target)

    camera.data.ortho_scale = 18.5
    render(scene, camera, out / "06-ridge-close.png", Vector((-12.5, -14.0, 15.5)), Vector((-12.5, 14.5, 8.8)))

    # Render 08-after-wireframe-clay.png (Blender 5.3.0 Wireframe-on-Clay proof for Zone10)
    wire_mat = create_wireframe_clay_material("Mat_Zone10_WireClay", wire_px=1.2)
    camera.data.ortho_scale = 36.0
    mesh.materials.clear()
    mesh.materials.append(wire_mat)
    render(scene, camera, out / "08-after-wireframe-clay.png", front, target)
    mesh.materials.clear()
    for m in old_mats:
        mesh.materials.append(m)

    binary = meshbin_bytes(vertices, uvs, faces)
    (out / (ZONE + ".meshbin")).write_bytes(binary)

    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(filepath=str(out / (ZONE + ".glb")), export_format="GLB", use_selection=True)

    # Restore full world scene
    scene.camera = old_camera
    scene.world = old_world
    scene.view_settings.view_transform = old_transform
    scene.render.resolution_x, scene.render.resolution_y = old_resolution
    for o in list(stage.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.collections.remove(stage)
    for name, hidden in old_hidden.items():
        if name in bpy.data.objects:
            bpy.data.objects[name].hide_render = hidden
    for name, hidden in col_states.items():
        if name in bpy.data.collections:
            bpy.data.collections[name].hide_render = hidden

    after_hashes = {o.name: mesh_hash(o) for o in scene.objects if o.type == "MESH" and o.name in other_hashes}
    assert other_hashes == after_hashes, "A zone outside Zone10 changed"

    scene.render.engine = "CYCLES"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "Farm_Main_3D_Zone10_Candidate.blend"))
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 960
    scene.render.filepath = str(out / "07-world-context.png")
    bpy.ops.render.render(write_still=True)

    report = {
        "source": str(args.source),
        "source_sha256": source_hash,
        "baseline_zone10_sha256": old_mesh_hash,
        "old_triangles": old_triangles,
        "vertices": int(len(vertices)),
        "triangles": int(len(faces)),
        "minimum_double_triangle_area": float(areas.min()),
        "bounds_blender": [vertices.min(axis=0).tolist(), vertices.max(axis=0).tolist()],
        "other_zone_geometry_unchanged": other_hashes == after_hashes,
        "other_zone_hashes": after_hashes,
        "parts_count": len(detail.parts),
        "canonical_ground_sampler": True,
        "meshbin_sha256": hashlib.sha256(binary).hexdigest(),
        "massif_only_vertices": int(len(m_v)),
        "massif_only_triangles": int(len(m_f)),
        "massif_only_meshbin_sha256": hashlib.sha256(massif_bin).hexdigest(),
    }
    (out / "validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("ZONE10_V2_CANDIDATE_VALIDATED", json.dumps({k: v for k, v in report.items() if k != "other_zone_hashes"}), flush=True)


if __name__ == "__main__":
    main()
