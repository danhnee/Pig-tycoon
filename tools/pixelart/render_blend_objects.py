"""Render selected objects/collections of a .blend as static 3/4-view sprites (same camera and light
language as render_fbx_sprites.py), one sub-folder per target so pixelize.py can process each.

  python tools/headless_runner.py blender --script tools/pixelart/render_blend_objects.py -- \
      --blend <file.blend> --list
  python tools/headless_runner.py blender --script tools/pixelart/render_blend_objects.py -- \
      --blend <file.blend> --targets "House,Lab" --out <dir> [--px-per-m 32] [--yaw 0]

A target is a collection name or a root object name (its children come along).
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--blend", required=True)
    p.add_argument("--list", action="store_true")
    p.add_argument("--targets", default="")
    p.add_argument("--out", default="")
    p.add_argument("--px-per-m", type=float, default=32.0)
    p.add_argument("--supersample", type=int, default=4)
    p.add_argument("--elev", type=float, default=30.0)
    p.add_argument("--yaw", type=float, default=0.0, help="degrees the camera orbits around the target")
    p.add_argument("--samples", type=int, default=32)
    p.add_argument("--max-px", type=int, default=2048, help="refuse renders larger than this (RAM guard)")
    p.add_argument("--append", action="store_true",
                   help="do not open the file; list names / append only the target objects or collections "
                        "(needed for the 351 MB harbor blend, which exceeded RAM when opened whole)")
    return p.parse_args(argv)


def descend(o):
    yield o
    for c in o.children:
        yield from descend(c)


def target_objects(name):
    col = bpy.data.collections.get(name)
    if col:
        return [o for o in col.all_objects]
    ob = bpy.data.objects.get(name)
    if ob:
        return list(descend(ob))
    return []


def bbox(objs):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        if o.type not in {"MESH", "CURVE", "SURFACE", "META", "FONT"}:
            continue
        ev = o.evaluated_get(dg)
        for c in ev.bound_box:
            w = ev.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
    return lo, hi


def do_list():
    rows = []
    for col in bpy.data.collections:
        lo, hi = bbox(list(col.all_objects))
        if lo.x < 1e8:
            rows.append(("COL", col.name, len(col.all_objects), tuple(round(v, 1) for v in (hi - lo))))
    for o in bpy.data.objects:
        if o.parent is None and o.type in {"MESH", "EMPTY"}:
            lo, hi = bbox(list(descend(o)))
            if lo.x < 1e8:
                rows.append(("OBJ", o.name, len(list(descend(o))), tuple(round(v, 1) for v in (hi - lo))))
    for r in rows:
        print("LIST", *r)


def setup(a):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = a.samples
    sc.cycles.use_denoising = True
    sc.cycles.max_bounces = 4
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    for o in list(sc.objects):
        if o.type in {"LIGHT", "CAMERA"}:
            o.hide_render = True
    world = bpy.data.worlds.new("SpriteWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.62, 0.52, 0.40, 1.0)
    bg.inputs[1].default_value = 0.55
    for name, energy, col, rot in (("Key", 3.2, (1.0, 0.78, 0.52), (50, 0, -35)), ("Rim", 1.1, (0.55, 0.62, 0.85), (60, 0, 150))):
        L = bpy.data.lights.new(name, "SUN")
        L.energy = energy
        L.color = col
        L.angle = math.radians(8)
        lo = bpy.data.objects.new(name, L)
        lo.rotation_euler = tuple(math.radians(v) for v in rot)
        sc.collection.objects.link(lo)
    cam_data = bpy.data.cameras.new("SpriteCam")
    cam_data.type = "ORTHO"
    cam = bpy.data.objects.new("SpriteCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def append_targets(path, names):
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.collections = [n for n in src.collections if n in names]
        dst.objects = [n for n in src.objects if n in names]
    for col in dst.collections:
        if col is not None:
            bpy.context.scene.collection.children.link(col)
    for ob in dst.objects:
        if ob is not None and ob.name not in bpy.context.scene.collection.all_objects:
            bpy.context.scene.collection.objects.link(ob)


def main():
    a = args()
    if a.append:
        if a.list:
            with bpy.data.libraries.load(a.blend) as (src, dst):
                for n in src.collections:
                    print("LIST COLNAME", n)
                for n in src.objects:
                    print("LIST OBJNAME", n)
            return
        bpy.ops.wm.read_factory_settings(use_empty=True)
        append_targets(a.blend, {x.strip() for x in a.targets.split(",") if x.strip()})
    else:
        bpy.ops.wm.open_mainfile(filepath=a.blend)
    if a.list:
        do_list()
        return
    out = Path(a.out)
    cam = setup(a)
    renderables = [o for o in bpy.context.scene.objects if o.type not in {"LIGHT", "CAMERA"} and o.name not in {"SpriteCam"}]
    original = {o.name: o.hide_render for o in renderables}
    for t in [x.strip() for x in a.targets.split(",") if x.strip()]:
        objs = target_objects(t)
        if not objs:
            print("MISSING TARGET", t)
            continue
        keep = {o.name for o in objs}
        for o in renderables:
            o.hide_render = o.name not in keep or original[o.name]
        lo, hi = bbox(objs)
        center = (lo + hi) / 2
        radius = max((hi - lo).length / 2, 0.05)
        final_px = math.ceil(radius * 2 * 1.04 * a.px_per_m)
        if final_px * a.supersample > a.max_px:
            print("TOO LARGE", t, final_px)
            continue
        sc = bpy.context.scene
        sc.render.resolution_x = sc.render.resolution_y = final_px * a.supersample
        cam.data.ortho_scale = final_px / a.px_per_m
        el, yaw = math.radians(a.elev), math.radians(a.yaw)
        dist = radius * 6
        cam.location = center + Vector((math.sin(yaw) * math.cos(el) * dist, -math.cos(yaw) * math.cos(el) * dist, math.sin(el) * dist))
        cam.rotation_euler = (math.radians(90) - el, 0, yaw)
        cam.data.clip_end = dist * 4
        d = out / t.replace(" ", "_")
        d.mkdir(parents=True, exist_ok=True)
        sc.render.filepath = str(d / "down_00.png")
        bpy.ops.render.render(write_still=True)
        (d / "manifest.json").write_text(json.dumps({
            "blend": a.blend, "target": t, "directions": ["down"], "files": ["down_00.png"],
            "supersample": a.supersample, "px_per_m": a.px_per_m, "size_m": [round(v, 2) for v in (hi - lo)],
        }, indent=2))
        print("RENDERED", t, final_px, "px")


main()
