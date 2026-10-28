#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Completes the interrupted Blender 5.2.2 Cycles renders for Codex's two active studies
(`mountain-reference-study-codex` and `functional-buildings-codex`) using the isolated
`bpy 5.2.2` runtime on `D:`, without modifying Codex's source scripts.
"""
from pathlib import Path
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")
ROOT = Path(__file__).resolve().parent.parent
RUNTIME = ROOT / ".collab/artifacts/mountain-reference-study-codex/runtime"
MTN_OUT = ROOT / ".collab/artifacts/mountain-reference-study-codex"
BLD_OUT = ROOT / ".collab/artifacts/functional-buildings-codex"

sys.path.insert(0, str(RUNTIME))
import bpy
from mathutils import Vector


def complete_mountain_study():
    print("=== [1/2] Completing mountain-reference-study-codex (05-front-clay & 06-ridge-wireframe) ===", flush=True)
    blend_path = MTN_OUT / "mountain-foothill-study.blend"
    bpy.ops.wm.open_mainfile(filepath=str(blend_path))
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.cycles.device = "CPU"
    scene.render.threads_mode = "FIXED"
    scene.render.threads = 8
    cam = scene.camera

    def mat(name, color, roughness=0.9):
        m = bpy.data.materials.new(name)
        m.diffuse_color = (*color, 1)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (*color, 1)
        bsdf.inputs["Roughness"].default_value = roughness
        return m

    def render(name, position, target, scale):
        cam.location = position
        cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()
        cam.data.ortho_scale = scale
        scene.render.filepath = str(MTN_OUT / name)
        bpy.ops.render.render(write_still=True)
        print("  RENDER_READY", name, flush=True)

    if not (MTN_OUT / "05-front-clay.png").exists():
        clay = mat("Neutral clay proof", (0.48, 0.49, 0.51))
        scene.view_layers[0].material_override = clay
        render("05-front-clay.png", (27, -51, 28), (0, 3, 8), 57)
        scene.view_layers[0].material_override = None

    if not (MTN_OUT / "06-ridge-wireframe.png").exists():
        wire = mat("Mesh topology wire", (0.11, 0.15, 0.19))
        nodes = wire.node_tree.nodes
        links = wire.node_tree.links
        w = nodes.new("ShaderNodeWireframe")
        w.use_pixel_size = True
        w.inputs[0].default_value = 0.6
        mix = nodes.new("ShaderNodeMixRGB")
        mix.inputs[1].default_value = (0.60, 0.64, 0.69, 1)
        mix.inputs[2].default_value = (0.035, 0.055, 0.075, 1)
        links.new(w.outputs[0], mix.inputs[0])
        links.new(mix.outputs[0], nodes.get("Principled BSDF").inputs["Base Color"])
        scene.view_layers[0].material_override = wire
        render("06-ridge-wireframe.png", (15, -26, 22), (-5, 6, 12), 31)
        scene.view_layers[0].material_override = None

    subprocess.run([sys.executable, str(MTN_OUT / "verify_export.py")], check=True)
    print("  [OK] mountain-reference-study-codex verified!", flush=True)


def complete_functional_buildings_study():
    print("=== [2/2] Completing functional-buildings-codex (06, 07, .blend & verify) ===", flush=True)
    script = ROOT / "Tools/functional_building_study.py"
    subprocess.run([sys.executable, str(script), "--build", "--bpy-runtime", str(RUNTIME)], check=True)
    subprocess.run([sys.executable, str(script), "--verify"], check=True)
    print("  [OK] functional-buildings-codex verified!", flush=True)


if __name__ == "__main__":
    complete_mountain_study()
    complete_functional_buildings_study()
