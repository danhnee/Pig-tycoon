"""Render an animated FBX into per-direction RGBA frames for the 3D->pixel sprite pipeline.

Run through the project runner (hidden, RAM-capped):
  python tools/headless_runner.py blender --script tools/pixelart/render_fbx_sprites.py -- \
      --fbx <file.fbx> --out <dir> [--dirs 4] [--frames 8] [--size 256] [--elev 30]

Output: <out>/<dir>_<frame>.png (transparent) + <out>/manifest.json.
Pixelization (downsample, palette, outline) is a separate step: tools/pixelart/pixelize.py.
The camera framing is computed once over the whole clip and all directions so every frame of a
character shares one pixel grid; that is what keeps the downsampled animation from shimmering.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

DIR_NAMES_4 = ["down", "left", "up", "right"]          # camera looks from +Y toward -Y; "down" = facing camera
DIR_NAMES_8 = ["down", "down_left", "left", "up_left", "up", "up_right", "right", "down_right"]


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--fbx", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--dirs", type=int, default=4, choices=(1, 4, 8))
    p.add_argument("--frames", type=int, default=8, help="frames sampled evenly over the clip; 1 = static prop")
    p.add_argument("--size", type=int, default=256, help="square render size in px (ignored when --px-per-m is set)")
    p.add_argument("--px-per-m", type=float, default=0.0, help="final sprite pixels per metre; render size then follows from it")
    p.add_argument("--supersample", type=int, default=4, help="render this many px per final px (integer box downsample)")
    p.add_argument("--elev", type=float, default=30.0, help="camera elevation in degrees")
    p.add_argument("--facing", type=float, default=0.0, help="extra Z rotation so the model's front faces the camera")
    p.add_argument("--pad", type=float, default=1.12)
    p.add_argument("--sun", default="1.0,0.78,0.52", help="key light colour (warm gold)")
    p.add_argument("--texdir", action="append", default=[], help="folders searched for textures the FBX points at but cannot find")
    p.add_argument("--samples", type=int, default=24)
    p.add_argument("--head-scale", type=float, default=1.0,
                   help="stylise: scale the Head pose bone (and Neck by sqrt) so faces stay readable at sprite size")
    p.add_argument("--no-denoise", action="store_true", help="skip OIDN (it ran out of RAM on 600+ px building renders)")
    return p.parse_args(argv)


def relink_textures(texdirs):
    """FBX exported from Unity reference <name>.fbm/<tex>.png; the textures actually live in a sibling Textures/ folder."""
    missing = []
    for img in bpy.data.images:
        if img.source != "FILE" or not img.filepath:
            continue
        if Path(bpy.path.abspath(img.filepath)).exists():
            continue
        name = Path(img.filepath).name
        hit = next((Path(d) / name for d in texdirs if (Path(d) / name).exists()), None)
        if hit:
            img.filepath = str(hit)
            img.reload()
        else:
            missing.append(name)
    return missing


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(path):
    before = set(bpy.data.objects)
    if path.lower().endswith((".glb", ".gltf")):
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        bpy.ops.import_scene.fbx(filepath=path, automatic_bone_orientation=True)
    return [o for o in bpy.data.objects if o not in before]


def clip_range(objs):
    for o in objs:
        ad = o.animation_data
        if ad and ad.action:
            s, e = ad.action.frame_range
            return int(round(s)), int(round(e))
    return 1, 1


HEAD_SCALE = 1.0


def apply_overrides():
    """Big-head stylisation on top of the clip's bone animation. Done with Limit Scale constraints:
    setting pose_bone.scale directly was overwritten when the render re-evaluated the keyed action."""
    if HEAD_SCALE == 1.0:
        return
    for ob in bpy.data.objects:
        if ob.type != "ARMATURE" or ob.pose is None:
            continue
        for pb in ob.pose.bones:
            n = pb.name.split(":")[-1].lower()
            k = HEAD_SCALE if n == "head" else (HEAD_SCALE ** 0.5 if n == "neck" else None)
            if k is None or any(c.name == "BigHead" for c in pb.constraints):
                continue
            c = pb.constraints.new("LIMIT_SCALE")
            c.name = "BigHead"
            c.owner_space = "LOCAL"
            for ax in "xyz":
                setattr(c, f"use_min_{ax}", True)
                setattr(c, f"use_max_{ax}", True)
                setattr(c, f"min_{ax}", k)
                setattr(c, f"max_{ax}", k)
    bpy.context.view_layer.update()


def set_frame(f):
    bpy.context.scene.frame_set(f)
    apply_overrides()


def world_bbox(objs, frames):
    dg = bpy.context.evaluated_depsgraph_get()
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for f in frames:
        set_frame(f)
        dg.update()
        for o in objs:
            if o.type != "MESH":
                continue
            ev = o.evaluated_get(dg)
            for c in ev.bound_box:
                w = ev.matrix_world @ Vector(c)
                lo = Vector(map(min, lo, w))
                hi = Vector(map(max, hi, w))
    return lo, hi


def setup_render(size, samples, denoise=True):
    # Cycles on CPU: Eevee peaked above 2 GB private commit on this 8 GB machine for one character
    # (runner report 20261030-224506); a 256 px Cycles render of the same mesh stays far below that.
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = denoise
    sc.cycles.max_bounces = 4
    sc.render.resolution_x = size
    sc.render.resolution_y = size
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"


def setup_lights(sun_rgb):
    world = bpy.data.worlds.new("SpriteWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.62, 0.52, 0.40, 1.0)   # warm ambient bounce
    bg.inputs[1].default_value = 0.55
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 3.2
    key.color = sun_rgb
    key.angle = math.radians(8)
    ko = bpy.data.objects.new("Key", key)
    ko.rotation_euler = (math.radians(50), 0, math.radians(-35))   # upper-left sun, consistent across all sprites
    bpy.context.scene.collection.objects.link(ko)
    rim = bpy.data.lights.new("Rim", "SUN")
    rim.energy = 1.1
    rim.color = (0.55, 0.62, 0.85)                                 # cool rim keeps silhouettes readable
    ro = bpy.data.objects.new("Rim", rim)
    ro.rotation_euler = (math.radians(60), 0, math.radians(150))
    bpy.context.scene.collection.objects.link(ro)


def main():
    a = parse_args()
    global HEAD_SCALE
    HEAD_SCALE = a.head_scale
    out = Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    reset_scene()
    objs = import_fbx(a.fbx)
    meshes = [o for o in objs if o.type == "MESH"]
    if not meshes:
        raise SystemExit(f"no mesh in {a.fbx}")

    pivot = bpy.data.objects.new("Pivot", None)
    bpy.context.scene.collection.objects.link(pivot)
    for o in objs:
        if o.parent is None:
            o.parent = pivot

    s, e = clip_range(objs)
    n = max(1, a.frames)
    frames = [s] if n == 1 or e <= s else [s + round(i * (e - s) / n) for i in range(n)]
    names = {1: ["down"], 4: DIR_NAMES_4, 8: DIR_NAMES_8}[a.dirs]

    # Frame once over every direction and frame so all sprites of this clip share one grid.
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for k in range(len(names)):
        pivot.rotation_euler = (0, 0, math.radians(a.facing + k * 360.0 / len(names)))
        l, h = world_bbox(objs, frames)
        lo = Vector(map(min, lo, l))
        hi = Vector(map(max, hi, h))
    center = (lo + hi) / 2
    radius = max((hi - lo).length / 2, 0.01)

    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    if a.px_per_m > 0:
        # Integer supersampling: each final pixel is exactly ss x ss render pixels, so the box filter
        # in pixelize.py never straddles pixel boundaries and every clip of a character shares one scale.
        ss = a.supersample
        final_px = math.ceil(radius * 2 * a.pad * a.px_per_m)
        a.size = final_px * ss
        cam_data.ortho_scale = final_px / a.px_per_m
    else:
        cam_data.ortho_scale = radius * 2 * a.pad
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    el = math.radians(a.elev)
    dist = radius * 6
    cam.location = center + Vector((0, -math.cos(el) * dist, math.sin(el) * dist))
    cam.rotation_euler = (math.radians(90) - el, 0, 0)
    bpy.context.scene.camera = cam
    cam_data.clip_end = dist * 4

    setup_render(a.size, a.samples, not a.no_denoise)
    setup_lights(tuple(float(x) for x in a.sun.split(",")))
    texdirs = a.texdir or [str(Path(a.fbx).parent.parent / "Textures")]
    missing = relink_textures(texdirs)
    if missing:
        print("MISSING TEXTURES", missing)

    # Where the model's root (origin, ground plane z = 0) lands in the image: the sprite pivot.
    # Per-clip lowest-mesh z made pig idle/eat pivots differ by 5 px (codex R2), so use the rig root.
    # Using the bbox centre instead made attack/whistle clips jump sideways against walk.
    bpy.context.view_layer.update()
    ruv = world_to_camera_view(bpy.context.scene, cam, Vector((pivot.location.x, pivot.location.y, 0.0)))
    written = []
    for k, dname in enumerate(names):
        pivot.rotation_euler = (0, 0, math.radians(a.facing + k * 360.0 / len(names)))
        for i, f in enumerate(frames):
            set_frame(f)
            path = out / f"{dname}_{i:02d}.png"
            bpy.context.scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            written.append(path.name)

    # Pixels per metre lets every sprite of a family (all clips of one character) be scaled consistently.
    manifest = {
        "fbx": str(Path(a.fbx)),
        "clip_frames": [s, e],
        "sampled_frames": frames,
        "directions": names,
        "render_size": a.size,
        "supersample": a.supersample if a.px_per_m > 0 else None,
        "ortho_scale_m": cam_data.ortho_scale,
        "px_per_m": a.size / cam_data.ortho_scale,
        "ground_z_m": lo.z,
        "root_uv": [ruv.x, ruv.y],
        "elevation_deg": a.elev,
        "missing_textures": missing,
        "files": written,
    }
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print("RENDERED", len(written), "frames ->", out)


main()
