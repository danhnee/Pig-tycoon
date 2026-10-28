#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Blender 5.2.2 Cycles + bmesh Closed-Manifold Study for the Remaining Functional Facilities
and 5-Scene Architectural Kits (`Farm_Core`, `Hub_Village`, `Market_Town`) from Codex's
`functional-building-catalog.md` and `world-scene-layout.md`.

Uses the exact same material palette, `MeshBatch` closed-cuboid masonry/slate/timber rules,
`bmesh` manifold + signed-volume validation (`nonmanifold == 0`, `volume > 0`), and AgX Cycles
lighting as `Tools/functional_building_study.py` and `Tools/mountain_reference_study.py`.
Writes exclusively to `.collab/artifacts/argon-facilities-and-scenes-codex/` without touching
any file owned by Codex.
"""
import json
import math
from pathlib import Path
import random
import struct
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")
ROOT = Path(__file__).resolve().parent.parent
RUNTIME = ROOT / ".collab/artifacts/mountain-reference-study-codex/runtime"
OUT = ROOT / ".collab/artifacts/argon-facilities-and-scenes-codex"
MTN_GLB = ROOT / ".collab/artifacts/mountain-reference-study-codex/mountain-foothill-study.glb"
BLD_DIR = ROOT / ".collab/artifacts/functional-buildings-codex"

if str(RUNTIME) not in sys.path:
    sys.path.insert(0, str(RUNTIME))

import bpy
import bmesh
from mathutils import Vector


def build_and_render_all():
    processes = subprocess.check_output(["tasklist", "/FO", "CSV"], text=True, errors="replace")
    assert not any(row.startswith('"Unity.exe"') for row in processes.splitlines()), "Close Unity first"

    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.use_scripts_auto_execute = False
    rng = random.Random(20261018)

    def material(name, rgb, metallic=0.0, roughness=None, emission_rgb=None, emission_strength=0.0):
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*rgb, 1.0)
        mat.use_nodes = True
        shader = mat.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = (*rgb, 1.0)
        shader.inputs["Roughness"].default_value = (0.80 if metallic == 0 else 0.44) if roughness is None else roughness
        shader.inputs["Metallic"].default_value = metallic
        if emission_rgb is not None and "Emission Color" in shader.inputs:
            shader.inputs["Emission Color"].default_value = (*emission_rgb, 1.0)
            shader.inputs["Emission Strength"].default_value = emission_strength
        return mat

    colors = {
        "oak": (0.25, 0.13, 0.065),
        "oak_light": (0.40, 0.24, 0.12),
        "oak_dark": (0.13, 0.07, 0.039),
        "ochre": (0.55, 0.37, 0.18),
        "plaster": (0.78, 0.72, 0.57),
        "plaster_warm": (0.82, 0.74, 0.59),
        "stone": (0.43, 0.44, 0.40),
        "stone_light": (0.58, 0.57, 0.49),
        "stone_dark": (0.31, 0.33, 0.31),
        "brick": (0.52, 0.23, 0.14),
        "slate": (0.115, 0.19, 0.23),
        "slate_light": (0.19, 0.28, 0.31),
        "slate_dark": (0.07, 0.13, 0.16),
        "terracotta": (0.56, 0.25, 0.14),
        "terracotta_dark": (0.42, 0.18, 0.10),
        "iron": (0.10, 0.12, 0.115),
        "brass": (0.56, 0.37, 0.12),
        "copper": (0.25, 0.42, 0.36),
        "linen": (0.76, 0.69, 0.54),
        "linen_light": (0.84, 0.78, 0.64),
        "paper": (0.83, 0.76, 0.57),
        "glass_green": (0.22, 0.46, 0.40),
        "sapphire": (0.16, 0.58, 0.78),
        "warmglass": (0.94, 0.56, 0.15),
        "blue": (0.13, 0.28, 0.32),
        "dark": (0.04, 0.05, 0.045),
    }
    palette = {}
    for k, rgb in colors.items():
        if k in ("iron", "brass", "copper"):
            palette[k] = material(k, rgb, metallic=0.68)
        elif k == "sapphire":
            palette[k] = material(k, rgb, metallic=0.1, roughness=0.22, emission_rgb=(0.20, 0.68, 0.92), emission_strength=1.4)
        elif k == "warmglass":
            palette[k] = material(k, rgb, metallic=0.0, roughness=0.28, emission_rgb=(0.95, 0.58, 0.18), emission_strength=1.1)
        elif k == "glass_green":
            palette[k] = material(k, rgb, metallic=0.08, roughness=0.25)
        else:
            palette[k] = material(k, rgb, metallic=0.0)

    all_records = []

    class MeshBatch:
        def __init__(self, name):
            self.name = name
            self.parts = {}

        def mesh(self, vertices, faces, mat):
            data = self.parts.setdefault(mat, [[], []])
            offset = len(data[0])
            data[0].extend(vertices)
            data[1].extend([tuple(offset + j for j in face) for face in faces])

        def box(self, center, size, mat, rotation=0.0):
            x, y, z = center
            w, d, h = (v / 2.0 for v in size)
            points = [
                (-w, -d, -h), (w, -d, -h), (w, d, -h), (-w, d, -h),
                (-w, -d,  h), (w, -d,  h), (w, d,  h), (-w, d,  h),
            ]
            c, s = math.cos(rotation), math.sin(rotation)
            self.mesh(
                [(x + a * c - b * s, y + a * s + b * c, z + q) for a, b, q in points],
                [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)],
                mat,
            )

        def beam(self, a, b, width, depth, mat):
            av, bv = Vector(a), Vector(b)
            delta = bv - av
            if delta.length < 1e-6:
                return
            u = delta.cross(Vector((0, 0, 1)))
            if u.length < 0.001:
                u = delta.cross(Vector((0, 1, 0)))
            u.normalize()
            u *= width / 2.0
            v = delta.normalized().cross(u.normalized()) * (depth / 2.0)
            vv = [av - u - v, av + u - v, av + u + v, av - u + v, bv - u - v, bv + u - v, bv + u + v, bv - u + v]
            self.mesh(
                [tuple(p) for p in vv],
                [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)],
                mat,
            )

        def cylinder(self, center, radius, height, mat, sides=16, top=None):
            x, y, z = center
            rr = radius if top is None else top
            vv = [
                (x + math.cos(i * math.tau / sides) * r, y + math.sin(i * math.tau / sides) * r, z + h)
                for r, h in ((radius, -height / 2.0), (rr, height / 2.0))
                for i in range(sides)
            ]
            ff = [tuple(reversed(range(sides))), tuple(range(sides, sides * 2))]
            ff += [(i, (i + 1) % sides, (i + 1) % sides + sides, i + sides) for i in range(sides)]
            self.mesh(vv, ff, mat)

        def finish(self):
            objects = []
            for key, (verts, faces) in self.parts.items():
                me = bpy.data.meshes.new(self.name + "_" + key)
                me.from_pydata(verts, [], faces)
                me.update()
                ob = bpy.data.objects.new(self.name + "_" + key, me)
                bpy.context.collection.objects.link(ob)
                me.materials.append(palette[key])
                objects.append(ob)
            return objects

    def masonry(batch, cx, cy, w, d, z, rows, stone_pool=("stone", "stone", "stone_light", "stone_dark")):
        for row in range(rows):
            zz = z + row * 0.28 + 0.14
            for side in (-1, 1):
                n = max(2, round(w / 0.56))
                step = w / n
                for i in range(n):
                    x = cx - w / 2.0 + (i + 0.5) * step
                    batch.box((x, cy + side * (d / 2.0 - 0.13), zz), (step - 0.025, 0.27, 0.25), rng.choice(stone_pool))
                n = max(2, round((d - 0.55) / 0.56))
                step = (d - 0.55) / n
                for i in range(n):
                    yy = cy - (d - 0.55) / 2.0 + (i + 0.5) * step
                    batch.box((cx + side * (w / 2.0 - 0.13), yy, zz), (0.27, step - 0.025, 0.25), rng.choice(stone_pool))
        batch.box((cx, cy, z + rows * 0.28 + 0.015), (w, d, 0.12), "stone_light")

    def roof(batch, cx, cy, w, d, eave, rise, roof_style="slate"):
        half = w / 2.0 + 0.32
        length = d + 0.68
        ridge = eave + rise
        tile_pool = ["slate"] * 5 + ["slate_light", "slate_dark"] if roof_style == "slate" else ["terracotta"] * 5 + ["terracotta_dark", "ochre"]
        for side in (-1, 1):
            rows = max(5, round(half / 0.33))
            cols = max(7, round(length / 0.44))
            for r in range(rows):
                x0 = half * r / rows
                x1 = min(half + 0.025, half * (r + 1) / rows + 0.10)
                z0 = ridge - rise * x0 / half
                z1 = ridge - rise * x1 / half
                for col in range(cols):
                    y0 = cy - length / 2.0 + col * length / cols
                    y1 = y0 + length / cols - 0.016
                    top = [
                        (cx + side * x0, y0, z0 + 0.025),
                        (cx + side * x1, y0, z1 + 0.025),
                        (cx + side * x1, y1, z1 + 0.025),
                        (cx + side * x0, y1, z0 + 0.025),
                    ]
                    if side < 0:
                        top.reverse()
                    vv = top + [(x, y, z - 0.075) for x, y, z in top]
                    batch.mesh(vv, [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], rng.choice(tile_pool))
            for y in (cy - length / 2.0 - 0.01, cy + length / 2.0 + 0.01):
                batch.beam((cx, y, ridge + 0.07), (cx + side * half, y, eave + 0.04), 0.16, 0.16, "oak_dark")
            batch.beam((cx + side * half, cy - length / 2.0, eave), (cx + side * half, cy + length / 2.0, eave), 0.16, 0.20, "oak_dark")
        batch.beam((cx, cy - length / 2.0 - 0.05, ridge + 0.10), (cx, cy + length / 2.0 + 0.05, ridge + 0.10), 0.22, 0.16, "slate_dark")

    def gable(batch, cx, cy, w, d, z, h, rise, wall="plaster", roof_style="slate"):
        y0, y1 = cy - d / 2.0, cy + d / 2.0
        x0, x1 = cx - w / 2.0, cx + w / 2.0
        vv = [
            (x0, y0, z), (x1, y0, z), (x1, y0, z + h), (cx, y0, z + h + rise), (x0, y0, z + h),
            (x0, y1, z), (x1, y1, z), (x1, y1, z + h), (cx, y1, z + h + rise), (x0, y1, z + h),
        ]
        batch.mesh(vv, [(4, 3, 2, 1, 0), (5, 6, 7, 8, 9), (0, 1, 6, 5), (1, 2, 7, 6), (2, 3, 8, 7), (3, 4, 9, 8), (4, 0, 5, 9)], wall)
        for side in (-1, 1):
            yy = cy + side * (d / 2.0 + 0.035)
            for i in range(round(w / 0.24) + 1):
                x = x0 + 0.06 + i * (w - 0.12) / max(1, round(w / 0.24))
                top = z + h + rise * (1.0 - abs(x - cx) / (w / 2.0))
                batch.box((x, yy, (z + top) / 2.0), (0.025, 0.028, top - z), "oak")
            for x in (x0 + 0.06, cx, x1 - 0.06):
                top = z + h if x != cx else z + h + rise - 0.15
                batch.box((x, yy + side * 0.025, (z + top) / 2.0), (0.18, 0.16, top - z), "oak_dark")
            for zz in (z + 0.12, z + h - 0.08):
                batch.box((cx, yy + side * 0.03, zz), (w, 0.18, 0.18), "oak_dark")
            for sign in (-1, 1):
                batch.beam((cx, yy + side * 0.045, z + h + rise - 0.12), (cx + sign * (w / 2.0 - 0.10), yy + side * 0.045, z + h), 0.14, 0.14, "oak_dark")
                batch.beam((cx + sign * 0.25, yy + side * 0.07, z + h - 0.18), (cx + sign * (w / 2.0 - 0.25), yy + side * 0.07, z + 0.45), 0.12, 0.14, "oak")
        for side in (-1, 1):
            xx = cx + side * (w / 2.0 + 0.04)
            for j in range(5):
                y = cy - d / 2.0 + j * d / 4.0
                batch.box((xx, y, z + h / 2.0), (0.18, 0.18, h), "oak_dark")
            for zz in (z + 0.12, z + h - 0.08):
                batch.box((xx, cy, zz), (0.18, d, 0.18), "oak_dark")
        roof(batch, cx, cy, w, d, z + h, rise + 0.12, roof_style=roof_style)

    def door(batch, x, y, z, width=1.6, height=2.0):
        side = -1 if y < 0 else 1
        batch.box((x, y, z + height / 2.0), (width + 0.20, 0.13, height + 0.12), "oak_dark")
        for i in range(8):
            batch.box((x - width / 2.0 + (i + 0.5) * width / 8.0, y + side * 0.09, z + height / 2.0), (width / 8.0 - 0.014, 0.10, height), "oak_light" if i % 2 else "oak")
        for dx in (-width / 4.0, width / 4.0):
            for zz in (z + 0.3, z + height - 0.3):
                batch.box((x + dx, y + side * 0.16, zz), (width * 0.38, 0.04, 0.09), "iron")
        batch.box((x, y + side * 0.28, z + 0.03), (width + 0.38, 0.62, 0.13), "stone_light")

    def window(batch, x, y, z, w=0.75, h=0.85, pane_mat="glass_green"):
        side = -1 if y < 0 else 1
        batch.box((x, y, z), (w + 0.18, 0.12, h + 0.18), "oak_dark")
        batch.box((x, y + side * 0.08, z), (w, 0.04, h), pane_mat)
        for dx in (-w / 2.0, 0.0, w / 2.0):
            batch.box((x + dx, y + side * 0.12, z), (0.06, 0.045, h + 0.10), "oak_light")
        for dz in (-h / 2.0, 0.0, h / 2.0):
            batch.box((x, y + side * 0.12, z + dz), (w + 0.10, 0.045, 0.06), "oak_light")
        batch.box((x, y + side * 0.18, z - h / 2.0 - 0.12), (w + 0.35, 0.42, 0.13), "stone_light")

    # -------------------------------------------------------------------------
    # 1. MAIN HOUSE / NHÀ CHÍNH (with Brick Feed Stove, Window Sill Empty Bottle & Bạch Vân Control Board)
    # -------------------------------------------------------------------------
    def build_main_house():
        batch = MeshBatch("main_house")
        # Broad domestic two-gable European cottage
        masonry(batch, -0.6, 0.4, 5.4, 5.6, 0.0, 2)
        gable(batch, -0.6, 0.4, 5.2, 5.4, 0.62, 2.75, 1.65, "plaster_warm", roof_style="terracotta")
        # Cross wing on East side
        masonry(batch, 2.5, -0.3, 3.2, 4.4, 0.0, 2)
        gable(batch, 2.5, -0.3, 3.0, 4.2, 0.62, 2.55, 1.35, "plaster", roof_style="terracotta")
        # Prominent inhabited stone hearth & chimney on West side
        batch.box((-3.35, 0.6, 2.4), (0.95, 1.15, 4.8), "stone")
        batch.box((-3.35, 0.6, 4.9), (1.12, 1.30, 0.22), "stone_light")
        batch.box((-3.35, 0.6, 5.25), (0.72, 0.85, 0.48), "brick")
        # Front low timber porch & sleep/save entrance
        batch.box((-0.6, -2.95, 0.30), (3.4, 1.45, 0.60), "stone_light")
        for px in (-2.0, 0.8):
            batch.box((px, -3.50, 1.55), (0.18, 0.18, 1.95), "oak_dark")
        roof(batch, -0.6, -3.05, 3.2, 1.35, 2.45, 0.55, roof_style="terracotta")
        door(batch, -0.6, -2.32, 0.62, width=1.45, height=2.0)
        window(batch, -2.1, -2.32, 1.85, 0.72, 0.82, "warmglass")
        window(batch, 0.9, -2.32, 1.85, 0.72, 0.82, "warmglass")
        window(batch, -0.6, -2.32, 3.85, 0.68, 0.68, "warmglass")
        # Remote Bạch Vân Control-Board Area under East Porch (`Bảng Điều Khiển Bạch Vân`)
        batch.box((2.5, -2.85, 0.28), (2.6, 1.25, 0.56), "stone")
        for px in (1.4, 3.6):
            batch.box((px, -3.32, 1.48), (0.16, 0.16, 1.85), "oak_dark")
        batch.beam((1.4, -3.32, 2.38), (3.6, -3.32, 2.38), 0.16, 0.16, "oak_dark")
        batch.box((2.5, -2.55, 1.35), (1.45, 0.22, 1.05), "oak_dark")
        batch.box((2.5, -2.68, 1.38), (1.22, 0.05, 0.82), "blue")
        for ix, gx in enumerate((2.05, 2.50, 2.95)):
            batch.cylinder((gx, -2.72, 1.45), 0.12, 0.06, "brass", sides=12)
            batch.box((gx, -2.72, 1.12), (0.14, 0.08, 0.18), "copper" if ix == 1 else "iron")
        # Brick Feed Stove (`Lò Ủ Cám xây bằng gạch sát cửa sổ`) + Window Sill + Empty Season-1 Bottle (`story:p00105, p00304`)
        window(batch, 4.02, -0.20, 1.82, 0.78, 0.86, "warmglass")
        # Broad stone window sill with the empty glass bottle sitting on the sill
        batch.box((4.18, -0.20, 1.28), (0.36, 1.12, 0.14), "stone_light")
        batch.cylinder((4.24, -0.42, 1.44), 0.055, 0.18, "glass_green", sides=10)
        batch.cylinder((4.24, -0.42, 1.56), 0.024, 0.08, "glass_green", sides=10)
        # Brick feed-fermentation stove right beside the window
        batch.box((4.42, 0.82, 0.58), (0.96, 1.22, 1.16), "brick")
        batch.box((4.42, 0.82, 1.20), (1.06, 1.30, 0.12), "stone_light")
        batch.cylinder((4.46, 0.65, 1.42), 0.34, 0.38, "iron", sides=16)
        batch.cylinder((4.46, 0.65, 1.62), 0.36, 0.06, "brass", sides=16)
        batch.box((4.32, 1.22, 2.25), (0.42, 0.42, 2.10), "brick")
        batch.box((4.32, 1.22, 3.35), (0.50, 0.50, 0.14), "stone_light")
        return batch.finish()

    # -------------------------------------------------------------------------
    # 2. RESEARCH LABORATORY / PHÒNG NGHIÊN CỨU (Glazed Clerestory + Conservatory + Ventilated Porch + 2 Benches)
    # -------------------------------------------------------------------------
    def build_research_laboratory():
        batch = MeshBatch("research_laboratory")
        masonry(batch, -0.6, 0.2, 5.2, 4.8, 0.0, 3)
        gable(batch, -0.6, 0.2, 5.0, 4.6, 0.84, 2.55, 1.55, "plaster", roof_style="slate")
        # Glazed Clerestory / Raised Roof Monitor
        batch.box((-0.6, 0.2, 4.45), (2.8, 2.6, 0.72), "oak_dark")
        for side in (-1, 1):
            for gx in (-1.3, -0.6, 0.1):
                window(batch, gx, 0.2 + side * 1.32, 4.45, 0.52, 0.46, "glass_green")
        roof(batch, -0.6, 0.2, 2.9, 2.7, 4.82, 0.62, roof_style="slate")
        # Glazed Conservatory Side Wing on East (`pale glass-green` & iron frames)
        batch.box((2.95, 0.2, 0.42), (2.2, 3.8, 0.84), "stone")
        for gx in (2.0, 2.95, 3.9):
            for gy in (-1.55, 0.2, 1.95):
                batch.box((gx, gy, 1.65), (0.14, 0.14, 1.65), "iron")
        for gy in (-0.68, 1.08):
            batch.box((3.92, gy, 1.65), (0.06, 1.55, 1.48), "glass_green")
            batch.box((2.95, gy, 2.58), (1.95, 1.55, 0.06), "glass_green", rotation=0.0)
        for side in (-1, 1):
            batch.box((2.95, 0.2 + side * 1.86, 1.65), (1.85, 0.06, 1.48), "glass_green")
        roof(batch, 2.95, 0.2, 2.1, 3.7, 2.48, 0.65, roof_style="slate")
        # Reagent exhaust stack (`ceramic & copper`)
        batch.cylinder((-2.5, 1.6, 3.6), 0.24, 3.2, "stone_light", sides=14)
        batch.cylinder((-2.5, 1.6, 5.3), 0.30, 0.28, "copper", sides=14, top=0.16)
        # Ventilated sampling porch on Front (`South`) with separate Queue/Identification Bench vs Crafting Bench
        batch.box((-0.6, -2.85, 0.28), (4.8, 1.4, 0.56), "stone_light")
        for px in (-2.7, -0.6, 1.5):
            batch.box((px, -3.42, 1.52), (0.16, 0.16, 1.92), "oak_dark")
        roof(batch, -0.6, -2.92, 4.6, 1.3, 2.48, 0.48, roof_style="slate")
        door(batch, -0.6, -2.18, 0.84, width=1.35, height=1.95)
        # Bench 1 (West porch): Gene Identification & Sample Queue Bench (`Bàn Giám Định Gen`)
        batch.box((-1.95, -2.85, 0.92), (1.35, 0.68, 0.14), "oak_light")
        for lx in (-2.5, -1.4):
            batch.box((lx, -2.85, 0.72), (0.12, 0.58, 0.32), "oak_dark")
        batch.cylinder((-2.15, -2.85, 1.12), 0.11, 0.26, "brass", sides=12)
        batch.box((-1.65, -2.85, 1.03), (0.42, 0.32, 0.06), "paper")
        # Bench 2 (East porch): Cám Hạ Hỏa & Domestication Crafting Bench (`Bàn Chế Tạo`)
        batch.box((0.75, -2.85, 0.92), (1.35, 0.68, 0.14), "stone_light")
        for lx in (0.2, 1.3):
            batch.box((lx, -2.85, 0.72), (0.14, 0.58, 0.32), "stone_dark")
        batch.cylinder((0.55, -2.85, 1.14), 0.16, 0.30, "copper", sides=14)
        batch.cylinder((1.02, -2.85, 1.10), 0.12, 0.22, "glass_green", sides=12)
        return batch.finish()

    # -------------------------------------------------------------------------
    # 3. MEDICAL TENT / LỀU QUÂN Y (Stretched Pale-Linen Roof, Open Treatment Canopy, Cots & Supply Cabinet)
    # -------------------------------------------------------------------------
    def build_medical_tent():
        batch = MeshBatch("medical_tent")
        # Clean stone threshold platform
        batch.box((0.0, 0.0, 0.16), (4.6, 4.2, 0.32), "stone_light")
        batch.box((0.0, 0.0, 0.34), (4.2, 3.8, 0.08), "oak_light")
        # Timber poles & ridge frame
        for x in (-1.9, 0.0, 1.9):
            for y in (-1.7, 1.7):
                batch.box((x, y, 1.35), (0.14, 0.14, 1.95), "oak_dark")
            batch.box((x, 0.0, 1.85), (0.15, 0.15, 2.95), "oak_dark")
        batch.beam((-2.1, 0.0, 3.28), (2.1, 0.0, 3.28), 0.16, 0.16, "oak_dark")
        # Stretched pale-linen canopy slopes
        for side in (-1, 1):
            for col in range(6):
                x0 = -2.1 + col * 0.70
                x1 = x0 + 0.68
                top = [
                    (x0, 0.0, 3.32),
                    (x1, 0.0, 3.32),
                    (x1, side * 1.95, 2.18),
                    (x0, side * 1.95, 2.18),
                ]
                if side > 0:
                    top.reverse()
                vv = top + [(px, py, pz - 0.06) for px, py, pz in top]
                batch.mesh(vv, [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], "linen_light" if col % 2 == 0 else "linen")
            # Guy ropes & pegs
            for x in (-1.8, 0.0, 1.8):
                batch.beam((x, side * 1.92, 2.15), (x, side * 2.45, 0.18), 0.035, 0.035, "oak")
                batch.box((x, side * 2.45, 0.14), (0.12, 0.12, 0.28), "oak_dark")
        # Rear canvas curtain wall
        batch.box((0.0, 1.68, 1.32), (3.6, 0.08, 1.88), "linen")
        # 2 Treatment cots (`Giường Quân Y`) & Medical Supply Cabinet (`Tủ Quân Y`)
        for cx in (-1.15, 0.35):
            batch.box((cx, 0.15, 0.68), (0.92, 1.85, 0.12), "linen_light")
            batch.box((cx, 0.82, 0.78), (0.76, 0.36, 0.10), "paper")
            for lx in (cx - 0.38, cx + 0.38):
                for ly in (-0.68, 0.98):
                    batch.box((lx, ly, 0.50), (0.09, 0.09, 0.28), "oak_dark")
        # Medical Supply Cabinet on East side
        batch.box((1.45, 0.65, 1.15), (0.68, 1.15, 1.55), "oak")
        batch.box((1.09, 0.65, 1.35), (0.05, 0.42, 0.42), "paper")
        return batch.finish()

    # -------------------------------------------------------------------------
    # 4. INFORMATION & SURVEILLANCE FAMILY (`Khu Thông Tin`: Intel Desk + Watchtower + Threat Radar + Weather Station + Bạch Vân Sky-Eye)
    # -------------------------------------------------------------------------
    def build_information_complex():
        batch = MeshBatch("information_complex")
        # 4A. Center-West (`x = -3.8`): Bàn Tình Báo (`Intelligence Desk Pavilion`)
        ix, iy = -3.8, 0.0
        masonry(batch, ix, iy, 3.2, 2.8, 0.0, 1)
        for px in (ix - 1.35, ix + 1.35):
            for py in (iy - 1.15, iy + 1.15):
                batch.box((px, py, 1.45), (0.16, 0.16, 2.25), "oak_dark")
        roof(batch, ix, iy, 3.1, 2.7, 2.55, 0.75, roof_style="slate")
        # Broad map table + archive drawers + pinned intelligence sheets
        batch.box((ix, iy, 0.98), (1.95, 1.25, 0.16), "oak_light")
        batch.box((ix - 0.45, iy, 0.64), (0.75, 1.05, 0.52), "oak")
        batch.box((ix + 0.55, iy, 0.64), (0.65, 1.05, 0.52), "oak_dark")
        batch.box((ix - 0.25, iy - 0.10, 1.08), (0.85, 0.62, 0.04), "paper")
        batch.box((ix + 0.48, iy + 0.12, 1.08), (0.48, 0.42, 0.04), "blue")

        # 4B. Far-West (`x = -7.8`): Tháp Canh Quan Sát (`Slender Braced Timber Watchtower with Ladder`)
        wx, wy = -7.8, 0.4
        masonry(batch, wx, wy, 2.8, 2.8, 0.0, 1)
        for sx in (-1.05, 1.05):
            for sy in (-1.05, 1.05):
                batch.beam((wx + sx, wy + sy, 0.32), (wx + sx * 0.78, wy + sy * 0.78, 4.45), 0.18, 0.18, "oak_dark")
        for z0, z1 in ((0.45, 1.85), (1.85, 3.15), (3.15, 4.40)):
            for side in (-1, 1):
                batch.beam((wx - 0.92, wy + side * 0.92, z0), (wx + 0.92, wy + side * 0.92, z1), 0.11, 0.11, "oak")
                batch.beam((wx + 0.92, wy + side * 0.92, z0), (wx - 0.92, wy + side * 0.92, z1), 0.11, 0.11, "oak")
        # Open elevated lookout/crossbow deck & narrow weather roof
        batch.box((wx, wy, 4.52), (2.45, 2.45, 0.18), "oak_light")
        for side in (-1, 1):
            batch.box((wx, wy + side * 1.12, 5.02), (2.45, 0.10, 0.82), "oak")
            batch.box((wx + side * 1.12, wy, 5.02), (0.10, 2.25, 0.82), "oak")
        for sx in (-1.02, 1.02):
            for sy in (-1.02, 1.02):
                batch.box((wx + sx, wy + sy, 5.45), (0.14, 0.14, 1.75), "oak_dark")
        roof(batch, wx, wy, 2.35, 2.35, 6.28, 0.82, roof_style="slate")
        # Physical access ladder on South face
        for lx in (wx - 0.28, wx + 0.28):
            batch.beam((lx, wy - 1.38, 0.25), (lx, wy - 0.96, 4.52), 0.08, 0.08, "oak_dark")
        for rung in range(11):
            t = (rung + 0.5) / 11.0
            ry = (wy - 1.38) * (1.0 - t) + (wy - 0.96) * t
            rz = 0.25 * (1.0 - t) + 4.52 * t
            batch.box((wx, ry, rz), (0.56, 0.06, 0.06), "oak_light")

        # 4C. Center (`x = 0.0`): Trạm Khí Tượng (`Weather Station Mast & Louvered Cabinet`)
        mx, my = 0.0, -0.2
        batch.box((mx, my, 0.22), (2.2, 2.2, 0.44), "stone_light")
        # Louvered instrument cabinet (`Stevenson screen`)
        batch.box((mx - 0.52, my - 0.25, 1.18), (0.78, 0.68, 0.88), "plaster")
        batch.box((mx - 0.52, my - 0.25, 1.68), (0.88, 0.78, 0.12), "slate")
        for lx in (mx - 0.82, mx - 0.22):
            for ly in (my - 0.52, my + 0.02):
                batch.box((lx, ly, 0.60), (0.08, 0.08, 0.48), "oak_dark")
        # Slender measuring mast + cup anemometer + rain gauge
        batch.cylinder((mx + 0.42, my + 0.20, 2.35), 0.07, 4.10, "iron", sides=12)
        for arm_a in (0.0, 2.094, 4.188):
            ax = mx + 0.42 + 0.52 * math.cos(arm_a)
            ay = my + 0.20 + 0.52 * math.sin(arm_a)
            batch.beam((mx + 0.42, my + 0.20, 4.25), (ax, ay, 4.25), 0.04, 0.04, "brass")
            batch.cylinder((ax, ay, 4.25), 0.10, 0.12, "copper", sides=10)
        batch.cylinder((mx + 0.42, my - 0.48, 0.68), 0.16, 0.48, "copper", sides=14)

        # 4D. Center-East (`x = 3.8`): Radar Uy Hiếp (`Threat Radar Instrument Room & Tilted Receiver`)
        rx, ry = 3.8, 0.2
        masonry(batch, rx, ry, 3.0, 2.8, 0.0, 2)
        batch.box((rx, ry, 1.45), (2.8, 2.6, 1.55), "stone")
        batch.box((rx, ry, 2.28), (2.96, 2.76, 0.18), "stone_light")
        door(batch, rx - 0.55, ry - 1.32, 0.62, width=1.1, height=1.55)
        window(batch, rx + 0.65, ry - 1.32, 1.45, 0.58, 0.58, "warmglass")
        # Tilted directional receiver array on roof
        batch.cylinder((rx, ry, 2.65), 0.42, 0.58, "iron", sides=16)
        batch.box((rx, ry - 0.12, 3.35), (1.85, 0.22, 1.35), "brass", rotation=0.35)
        batch.box((rx, ry - 0.22, 3.38), (1.55, 0.08, 1.05), "copper", rotation=0.35)
        batch.beam((rx, ry - 0.22, 3.38), (rx + 0.28, ry - 0.92, 3.72), 0.07, 0.07, "iron")
        batch.cylinder((rx + 0.28, ry - 0.92, 3.72), 0.14, 0.14, "sapphire", sides=12)
        # Exposed cable drum on East side
        batch.cylinder((rx + 1.75, ry - 0.35, 0.58), 0.42, 0.62, "oak_dark", sides=14)
        batch.cylinder((rx + 1.75, ry - 0.35, 0.58), 0.32, 0.68, "copper", sides=14)

        # 4E. Far-East (`x = 8.2`): Bạch Vân · Thiên Nhãn Vọng Đài (`High-Craft Optical/Receiver Tower`)
        bx, by = 8.2, 0.4
        batch.cylinder((bx, by, 0.28), 2.15, 0.56, "stone_light", sides=16, top=1.95)
        batch.cylinder((bx, by, 0.82), 1.82, 0.54, "stone", sides=16, top=1.68)
        batch.cylinder((bx, by, 2.65), 1.48, 3.20, "stone_light", sides=16, top=1.22)
        for z_band in (1.55, 2.65, 3.75):
            batch.cylinder((bx, by, z_band), 1.44, 0.14, "stone_dark", sides=16)
        batch.cylinder((bx, by, 4.42), 1.56, 0.36, "stone_light", sides=16, top=1.64)
        # Precision brass optical/receiver cage & protected Sapphire core
        for k in range(8):
            ang = k * math.tau / 8.0
            px0 = bx + 1.32 * math.cos(ang)
            py0 = by + 1.32 * math.sin(ang)
            px1 = bx + 0.62 * math.cos(ang)
            py1 = by + 0.62 * math.sin(ang)
            batch.beam((px0, py0, 4.58), (px1, py1, 6.15), 0.10, 0.10, "brass")
        batch.cylinder((bx, by, 5.35), 0.68, 0.95, "sapphire", sides=16, top=0.52)
        batch.cylinder((bx, by, 6.32), 0.72, 0.24, "brass", sides=16, top=0.12)
        door(batch, bx, by - 1.48, 0.56, width=1.15, height=1.75)
        return batch.finish()

    # -------------------------------------------------------------------------
    # 5. POWER GENERATORS (A/B/C), RECOVERY STATION & LIGHTNING ROD
    # -------------------------------------------------------------------------
    def build_power_and_recovery():
        batch = MeshBatch("power_and_recovery")
        # Generator A (`x = -7.5`), Generator B (`x = -2.2`), Generator C (`x = 3.6`)
        for state, gx in (("A", -7.5), ("B", -2.2), ("C", 3.6)):
            w = 2.6 if state == "A" else (3.4 if state == "B" else 4.4)
            d = 2.4 if state == "A" else (2.8 if state == "B" else 3.2)
            masonry(batch, gx, 0.0, w, d, 0.0, 1 if state == "A" else 2)
            gable(batch, gx, 0.0, w - 0.15, d - 0.15, 0.34 if state == "A" else 0.62, 1.65 if state == "A" else 2.05, 0.75, "oak_light" if state == "A" else "plaster", roof_style="slate")
            # Exposed motor, brass flywheel & exhaust pipe on front/side
            n_cyl = 1 if state == "A" else (2 if state == "B" else 3)
            for c_i in range(n_cyl):
                cx_off = gx - 0.55 + c_i * 0.55
                batch.cylinder((cx_off, -d / 2.0 - 0.42, 0.68), 0.24, 0.72, "iron", sides=14)
                batch.cylinder((cx_off, -d / 2.0 - 0.42, 1.10), 0.28, 0.14, "brass", sides=14)
            # Flywheel & exhaust stack
            batch.cylinder((gx + w / 2.0 + 0.18, -0.35, 0.85), 0.52, 0.18, "brass", sides=16)
            batch.cylinder((gx - w / 2.0 - 0.22, 0.45, 1.85), 0.14, 2.65 if state != "C" else 3.35, "iron", sides=12)
            if state in ("B", "C"):
                # Fuel tank & switchgear cabinet
                batch.cylinder((gx + w / 2.0 + 0.55, 0.55, 0.75), 0.42, 1.15, "copper", sides=14)
                batch.box((gx + 0.85, -d / 2.0 - 0.25, 1.15), (0.68, 0.28, 0.95), "iron")
            if state == "C":
                batch.cylinder((gx - w / 2.0 - 0.22, -0.35, 1.85), 0.14, 3.35, "iron", sides=12)
                batch.box((gx - 1.15, -d / 2.0 - 0.35, 0.95), (0.85, 0.45, 1.12), "brass")

        # Recovery Station (`Trạm Hồi Sức` at `x = 8.4`) + Lightning Rod (`Cột Thu Lôi` at `x = 11.4`)
        rx, ry = 8.4, 0.0
        batch.box((rx, ry, 0.22), (3.2, 2.6, 0.44), "stone_light")
        for px in (rx - 1.35, rx + 1.35):
            for py in (ry - 1.05, ry + 1.05):
                batch.box((px, py, 1.35), (0.15, 0.15, 1.85), "oak_dark")
        roof(batch, rx, ry, 3.1, 2.5, 2.25, 0.65, roof_style="terracotta")
        # Rest benches & consumable cabinet
        batch.box((rx - 0.55, ry + 0.25, 0.68), (1.35, 0.55, 0.12), "oak_light")
        batch.box((rx + 0.82, ry + 0.35, 1.05), (0.65, 0.55, 1.22), "oak")
        batch.cylinder((rx - 0.55, ry - 0.55, 0.72), 0.24, 0.52, "copper", sides=12)

        # Lightning Rod (`Cột Thu Lôi`)
        lx, ly = 11.4, 0.2
        batch.box((lx, ly, 0.32), (1.35, 1.35, 0.64), "stone")
        batch.cylinder((lx, ly, 2.85), 0.11, 4.60, "iron", sides=12, top=0.04)
        for z_ins in (1.45, 2.85, 4.25):
            batch.cylinder((lx, ly, z_ins), 0.22, 0.14, "copper", sides=12)
        batch.cylinder((lx, ly, 5.35), 0.08, 0.55, "brass", sides=10, top=0.01)
        batch.box((lx + 0.42, ly - 0.35, 0.82), (0.48, 0.42, 0.58), "brass")
        return batch.finish()

    # -------------------------------------------------------------------------
    # 6. ANCIENT EUROPEAN MARKET TOWN (`Market_Town` — Romanesque Arcades, Jettied Timber Upper Floors, Crow-Step Gables, Belfry Clock Tower & Plaza Fountain)
    # -------------------------------------------------------------------------
    def build_market_town_hall_and_stalls():
        batch = MeshBatch("market_town_european")
        # Grand Ancient European Guild & Auction Hall (`Đấu Giá / Hội Quán Âu Cổ`)
        cx, cy = 0.0, 1.2
        w, d = 7.2, 5.4
        front_y = cy - d / 2.0
        # Stepped cobblestone plinth
        batch.box((cx, cy, 0.12), (w + 0.6, d + 0.6, 0.24), "stone_dark")
        batch.box((cx, cy, 0.28), (w + 0.3, d + 0.3, 0.16), "stone_light")
        # Ground floor recessed stone hall behind the open Romanesque arcade loggia
        batch.box((cx, cy + 0.42, 1.28), (w - 0.35, d - 0.95, 1.88), "stone")
        # 5 Romanesque stone colonnade piers + bases + capitals
        pier_xs = (-3.2, -1.6, 0.0, 1.6, 3.2)
        for px in pier_xs:
            batch.box((cx + px, front_y + 0.18, 0.44), (0.46, 0.48, 0.18), "stone_dark")
            batch.box((cx + px, front_y + 0.18, 1.15), (0.36, 0.38, 1.32), "stone_light")
            batch.box((cx + px, front_y + 0.18, 1.88), (0.48, 0.48, 0.18), "stone_dark")
        # True segmented Romanesque stone arches (`vòm đá cuốn Âu cổ`) across all 4 bays
        for ax in (-2.4, -0.8, 0.8, 2.4):
            for seg_i in range(7):
                ang0 = math.pi * (0.12 + 0.76 * (seg_i / 7.0))
                ang1 = math.pi * (0.12 + 0.76 * ((seg_i + 1) / 7.0))
                rx, rz = 0.62, 0.34
                p0 = (cx + ax - math.cos(ang0) * rx, front_y + 0.18, 1.78 + math.sin(ang0) * rz)
                p1 = (cx + ax - math.cos(ang1) * rx, front_y + 0.18, 1.78 + math.sin(ang1) * rz)
                mat_arch = "stone_dark" if seg_i == 3 else "stone_light"
                batch.beam(p0, p1, 0.38, 0.22 if seg_i == 3 else 0.18, mat_arch)
            # Spandrel wall fill above the arch and carved oak corbel brackets supporting the jettied floor
            batch.box((cx + ax, front_y + 0.22, 2.08), (1.26, 0.32, 0.22), "stone")
            batch.beam((cx + ax, front_y + 0.08, 1.82), (cx + ax, front_y - 0.32, 2.22), 0.16, 0.16, "oak_dark")

        # Jettied Overhanging Half-Timbered Upper Floor (`Tầng Lầu Đua Ra Ngoài Khung Gỗ Sồi`)
        gable(batch, cx, cy - 0.12, w + 0.45, d + 0.45, 2.18, 2.48, 1.92, "plaster_warm", roof_style="terracotta")

        # Stepped Hanseatic Crow-Step Front Gable Projection (`Đỉnh Tường Bậc Thang Âu Cổ`)
        gx, gy = cx, front_y - 0.38
        batch.box((gx, gy + 0.38, 3.38), (2.95, 0.96, 2.38), "plaster")
        roof(batch, gx, gy + 0.48, 2.85, 1.25, 4.48, 1.38, roof_style="terracotta")
        # Exposed European Fachwerk half-timbering on the front gable projection (`y = gy - 0.11`)
        fy = gy - 0.11
        for tx in (-1.42, -0.78, 0.78, 1.42):
            batch.box((gx + tx, fy, 3.38), (0.11, 0.08, 2.36), "oak_dark")
        for tz in (2.25, 2.95, 4.45):
            batch.box((gx, fy, tz), (2.96, 0.09, 0.11), "oak_dark")
        # St. Andrew's crosses (diagonal braces) in side bays of the front projection
        for side_s in (-1.0, 1.0):
            bx0, bx1 = side_s * 0.82, side_s * 1.38
            batch.beam((bx0, fy - 0.01, 2.30), (bx1, fy - 0.01, 2.90), 0.08, 0.08, "oak")
            batch.beam((bx1, fy - 0.01, 2.30), (bx0, fy - 0.01, 2.90), 0.08, 0.08, "oak")
        # Crow-step limestone gable steps (`Bậc Thang Đá Hanseatic`)
        for step_i in range(6):
            sw = 3.00 - step_i * 0.46
            sz = 4.55 + step_i * 0.26
            batch.box((gx, gy - 0.08, sz), (sw, 0.30, 0.24), "stone_light")
            batch.box((gx, gy - 0.09, sz + 0.11), (sw + 0.06, 0.34, 0.05), "stone_dark")
        batch.cylinder((gx, gy - 0.08, 6.18), 0.14, 0.45, "brass", sides=10, top=0.03)

        # TRUE Y-Axis Gothic Rose Window (`Cửa Sổ Hoa Hồng Gothic Kính Màu`) on front gable face (`z = 3.68`)
        rcx, rcy, rcz = gx, fy - 0.03, 3.68
        for seg_i in range(12):
            a0 = 2.0 * math.pi * (seg_i / 12.0)
            a1 = 2.0 * math.pi * ((seg_i + 1) / 12.0)
            am = 0.5 * (a0 + a1)
            p0 = (rcx + math.cos(a0) * 0.52, rcy - 0.02, rcz + math.sin(a0) * 0.52)
            p1 = (rcx + math.cos(a1) * 0.52, rcy - 0.02, rcz + math.sin(a1) * 0.52)
            batch.beam(p0, p1, 0.12, 0.13, "stone_dark")
            glass_mat = "warmglass" if seg_i % 2 == 0 else ("ochre" if seg_i % 4 == 1 else "blue")
            pg0 = (rcx + math.cos(am) * 0.10, rcy + 0.01, rcz + math.sin(am) * 0.10)
            pg1 = (rcx + math.cos(am) * 0.44, rcy + 0.01, rcz + math.sin(am) * 0.44)
            batch.beam(pg0, pg1, 0.06, 0.18, glass_mat)
        for spoke_i in range(8):
            ang = 2.0 * math.pi * (spoke_i / 8.0)
            ps0 = (rcx, rcy - 0.03, rcz)
            ps1 = (rcx + math.cos(ang) * 0.48, rcy - 0.03, rcz + math.sin(ang) * 0.48)
            batch.beam(ps0, ps1, 0.06, 0.05, "brass")
        batch.box((rcx, rcy - 0.04, rcz), (0.18, 0.08, 0.18), "brass")

        # Gothic Belfry & Clock Turret (`Tháp Đồng Hồ & Tháp Chuông Âu Cổ`) rising above main roof ridge
        tx, ty = cx, cy - 0.25
        batch.box((tx, ty, 5.45), (1.55, 1.55, 1.85), "stone_light")
        batch.box((tx, ty, 6.42), (1.72, 1.72, 0.18), "stone_dark")
        batch.box((tx, ty, 7.05), (1.35, 1.35, 1.15), "oak_dark")
        batch.box((tx, ty, 7.22), (0.95, 1.42, 0.65), "warmglass")
        batch.cylinder((tx, ty, 7.62), 0.98, 1.85, "copper", sides=8, top=0.04)
        batch.cylinder((tx, ty, 9.45), 0.05, 0.55, "brass", sides=8, top=0.02)
        clk_y, clk_z = ty - 0.80, 5.75
        for seg_i in range(12):
            a0 = 2.0 * math.pi * (seg_i / 12.0)
            a1 = 2.0 * math.pi * ((seg_i + 1) / 12.0)
            batch.beam(
                (tx + math.cos(a0) * 0.36, clk_y, clk_z + math.sin(a0) * 0.36),
                (tx + math.cos(a1) * 0.36, clk_y, clk_z + math.sin(a1) * 0.36),
                0.08,
                0.08,
                "brass",
            )
        batch.box((tx, clk_y + 0.02, clk_z), (0.58, 0.04, 0.58), "paper")
        batch.beam((tx, clk_y - 0.02, clk_z), (tx, clk_y - 0.02, clk_z + 0.26), 0.05, 0.05, "iron")
        batch.beam((tx, clk_y - 0.02, clk_z), (tx + 0.20, clk_y - 0.02, clk_z - 0.08), 0.05, 0.05, "iron")

        for wx in (-2.25, 2.25):
            window(batch, wx, front_y - 0.32, 3.28, 0.76, 0.88, "warmglass")
        door(batch, cx, front_y + 0.55, 0.36, width=1.60, height=1.82)

        # Central Carved Stone European Market Plaza Fountain (`Đài Phun Nước Đá Quảng Trường Âu Cổ`)
        fx, fy_plaza = 0.0, -3.15
        batch.cylinder((fx, fy_plaza, 0.11), 1.35, 0.22, "stone_dark", sides=8)
        batch.cylinder((fx, fy_plaza, 0.36), 1.12, 0.32, "stone_light", sides=8)
        batch.cylinder((fx, fy_plaza, 0.54), 0.92, 0.10, "blue", sides=8)
        batch.cylinder((fx, fy_plaza, 0.88), 0.24, 1.15, "stone_light", sides=8, top=0.18)
        batch.cylinder((fx, fy_plaza, 1.48), 0.52, 0.14, "stone_dark", sides=8, top=0.38)
        batch.cylinder((fx, fy_plaza, 1.72), 0.12, 0.38, "brass", sides=8, top=0.03)

        # 2 Covered European Market Stall Pavilions (`Quầy Ngày` & `Quầy Kiểm Dịch / Giám Định`)
        for side, mat_roof in ((-1, "terracotta"), (1, "slate")):
            sx, sy = side * 4.8, -2.65
            batch.box((sx, sy, 0.16), (2.85, 2.25, 0.32), "stone_light")
            for px in (sx - 1.18, sx + 1.18):
                for py in (sy - 0.88, sy + 0.88):
                    batch.box((px, py, 1.25), (0.15, 0.15, 1.85), "oak_dark")
                    batch.beam((px, py, 1.75), (px - side * 0.28, py, 2.08), 0.10, 0.10, "oak")
            roof(batch, sx, sy, 2.75, 2.15, 2.15, 0.62, roof_style=mat_roof)
            batch.box((sx, sy - 0.45, 0.78), (2.18, 0.70, 0.66), "oak_light")
            batch.box((sx - 0.48, sy - 0.45, 1.16), (0.58, 0.46, 0.12), "paper" if side > 0 else "linen")
            batch.cylinder((sx + 0.55, sy - 0.45, 1.12), 0.16, 0.28, "brass" if side > 0 else "copper", sides=12)
        return batch.finish()

    # -------------------------------------------------------------------------
    # 7. HUB VILLAGE (`Hub_Village` 40x30m — Elevated Highland NPC Village: Nhà Bà Sáu, Nhà Tám Vá / Ông Lẫm, Sân Làng & Bảng Tin)
    # -------------------------------------------------------------------------
    def build_hub_village_sanctum():
        batch = MeshBatch("hub_village_sanctum")
        # Elevated terraced stone plateau (`Triền Hậu Cảnh Tây-Bắc` — visually elevated behind Farm_Main)
        batch.box((0.0, 0.4, 0.22), (11.6, 7.8, 0.44), "stone_dark")
        batch.box((0.0, 0.5, 0.54), (10.8, 7.0, 0.24), "stone_light")
        # Stepped stone stair approach from the West Gate trail
        for st_i in range(4):
            batch.box((0.0, -3.15 - st_i * 0.36, 0.10 + (3 - st_i) * 0.12), (2.6, 0.38, 0.20), "stone_light")

        # Left Cottage: `Nhà Bà Sáu` (Warm terracotta-roofed highland stone-and-timber cottage with kitchen hearth & veranda)
        lx, ly = -3.1, 1.05
        gable(batch, lx, ly, 4.2, 3.6, 0.66, 2.25, 1.45, "plaster_warm", roof_style="terracotta")
        door(batch, lx + 0.2, ly - 1.80, 0.66, width=1.12, height=1.65)
        window(batch, lx - 1.05, ly - 1.80, 1.65, 0.68, 0.72, "warmglass")
        # Stone hearth chimney on West wall of Bà Sáu's house
        batch.box((lx - 2.22, ly + 0.35, 2.35), (0.62, 0.72, 3.35), "stone")
        batch.box((lx - 2.22, ly + 0.35, 4.12), (0.72, 0.82, 0.20), "stone_dark")
        # Front porch awning
        roof(batch, lx + 0.15, ly - 2.15, 2.4, 1.05, 2.18, 0.42, roof_style="terracotta")
        for px in (lx - 0.92, lx + 1.22):
            batch.box((px, ly - 2.52, 1.42), (0.13, 0.13, 1.52), "oak_dark")

        # Right Cottage: `Nhà Tám Vá & Ông Lẫm` (Slate-roofed craftsman & village elder house with tool rack & attic dormer)
        rx, ry = 3.15, 1.15
        gable(batch, rx, ry, 4.0, 3.5, 0.66, 2.35, 1.52, "plaster", roof_style="slate")
        door(batch, rx - 0.25, ry - 1.75, 0.66, width=1.10, height=1.65)
        window(batch, rx + 0.98, ry - 1.75, 1.68, 0.68, 0.72, "warmglass")
        # Small gable dormer on front roof slope
        batch.box((rx, ry - 0.95, 3.42), (1.15, 0.95, 0.78), "plaster_warm")
        roof(batch, rx, ry - 0.95, 1.28, 1.10, 3.82, 0.45, roof_style="slate")
        window(batch, rx, ry - 1.43, 3.42, 0.48, 0.48, "warmglass")

        # Central Village Courtyard (`Sân Làng`) + Notice Board (`Bảng Tin Làng`) + Stone Village Lantern
        bx, by = 0.0, -1.25
        batch.box((bx, by, 0.74), (1.85, 0.55, 0.16), "stone_dark")
        for px in (-0.72, 0.72):
            batch.box((bx + px, by, 1.58), (0.14, 0.14, 1.55), "oak_dark")
        batch.box((bx, by, 1.68), (1.58, 0.16, 0.92), "oak_light")
        batch.box((bx - 0.35, by - 0.10, 1.72), (0.48, 0.05, 0.52), "paper")
        batch.box((bx + 0.32, by - 0.10, 1.65), (0.44, 0.05, 0.42), "ochre")
        roof(batch, bx, by, 2.05, 0.95, 2.35, 0.42, roof_style="terracotta")
        # Stone lantern post beside courtyard
        batch.cylinder((0.0, 1.65, 0.95), 0.28, 0.58, "stone", sides=8)
        batch.cylinder((0.0, 1.65, 1.52), 0.18, 0.62, "stone_light", sides=8)
        batch.box((0.0, 1.65, 1.98), (0.34, 0.34, 0.32), "warmglass")
        batch.cylinder((0.0, 1.65, 2.28), 0.32, 0.28, "stone_dark", sides=8, top=0.06)
        return batch.finish()

    # -------------------------------------------------------------------------
    # 8. FOREST PRIMEVAL (`Forest_Primeval` 80x80m — Ancient Sun-Moon Rune Monolith Shrine, Golden Mythic Flora & Sacred Pool)
    # -------------------------------------------------------------------------
    def build_forest_primeval_relic():
        batch = MeshBatch("forest_primeval_relic")
        # Mossy tiered ancient limestone sanctuary terrace & sacred reflective pool (`Hồ Rừng Sâu & Di Tích Cổ`)
        batch.cylinder((0.0, 0.0, 0.18), 4.85, 0.36, "stone_dark", sides=12)
        batch.cylinder((0.0, 0.25, 0.48), 4.15, 0.26, "stone", sides=12)
        # Sacred forest pool basin in the foreground (`Hồ Nước Rừng Nguyên Sinh`)
        batch.cylinder((0.0, -1.55, 0.58), 1.95, 0.16, "stone_light", sides=10)
        batch.cylinder((0.0, -1.55, 0.65), 1.68, 0.08, "blue", sides=10)

        # Central Carved Ancient Sun-and-Moon Rune Monolith Arch (`Di Tích Cổ Khắc Phù Nhật Nguyệt`)
        for side_s in (-1.0, 1.0):
            px = side_s * 1.65
            batch.box((px, 1.15, 0.85), (0.88, 0.88, 0.48), "stone_dark")
            batch.box((px, 1.15, 2.35), (0.68, 0.68, 2.55), "stone_light")
            batch.box((px, 1.15, 3.72), (0.82, 0.82, 0.24), "stone_dark")
            # Glowing sapphire/golden rune inlays on monolith pillars
            for rz in (1.65, 2.35, 3.05):
                batch.box((px, 0.78, rz), (0.28, 0.08, 0.28), "sapphire" if rz == 2.35 else "ochre")
        # Segmented carved stone trilithon arch & sun-disc keystone crowning the relic
        for seg_i in range(7):
            a0 = math.pi * (0.08 + 0.84 * (seg_i / 7.0))
            a1 = math.pi * (0.08 + 0.84 * ((seg_i + 1) / 7.0))
            p0 = (-math.cos(a0) * 1.65, 1.15, 3.75 + math.sin(a0) * 1.05)
            p1 = (-math.cos(a1) * 1.65, 1.15, 3.75 + math.sin(a1) * 1.05)
            batch.beam(p0, p1, 0.72, 0.36 if seg_i == 3 else 0.28, "stone_dark" if seg_i == 3 else "stone_light")
        # Levitating Sun-Moon Rune Core (`Lõi Cổ Ngữ Nhật Nguyệt`) inside the monolith arch
        batch.cylinder((0.0, 1.15, 2.55), 0.42, 0.42, "sapphire", sides=12, top=0.65)
        for ring_i in range(8):
            ang0 = 2.0 * math.pi * (ring_i / 8.0)
            ang1 = 2.0 * math.pi * ((ring_i + 1) / 8.0)
            batch.beam(
                (math.cos(ang0) * 0.68, 1.15, 2.75 + math.sin(ang0) * 0.68),
                (math.cos(ang1) * 0.68, 1.15, 2.75 + math.sin(ang1) * 0.68),
                0.09,
                0.09,
                "brass",
            )

        # Mythological Golden-Amber Crystal-Leaf Flora (`Thực Vật Hoàng Kim Thần Thoại`) flanking the relic
        for fx_side in (-2.95, 2.95):
            fy_pos = -0.35
            batch.cylinder((fx_side, fy_pos, 0.78), 0.55, 0.36, "stone_dark", sides=8, top=0.78)
            batch.cylinder((fx_side, fy_pos, 1.18), 0.22, 0.45, "ochre", sides=8, top=0.65)
            for petal_i in range(6):
                pa = 2.0 * math.pi * (petal_i / 6.0)
                p_start = (fx_side + math.cos(pa) * 0.14, fy_pos + math.sin(pa) * 0.14, 1.05)
                p_end = (fx_side + math.cos(pa) * 0.78, fy_pos + math.sin(pa) * 0.78, 1.68)
                batch.beam(p_start, p_end, 0.18, 0.12, "warmglass" if petal_i % 2 == 0 else "ochre")
        return batch.finish()

    def validate(name, objects):
        vertices = []
        triangles = 0
        records = []
        for ob in objects:
            mesh = ob.data
            mesh.calc_loop_triangles()
            assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
            bm = bmesh.new()
            bm.from_mesh(mesh)
            bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
            bm.to_mesh(mesh)
            nonmanifold = sum(not edge.is_manifold for edge in bm.edges)
            assert nonmanifold == 0, (name, ob.name, nonmanifold)
            volume = bm.calc_volume(signed=True)
            assert volume > 0, (name, ob.name, volume)
            bm.free()
            mesh.calc_loop_triangles()
            minimum = min(
                (mesh.vertices[t.vertices[1]].co - mesh.vertices[t.vertices[0]].co)
                .cross(mesh.vertices[t.vertices[2]].co - mesh.vertices[t.vertices[0]].co)
                .length
                / 2.0
                for t in mesh.loop_triangles
            )
            assert minimum > 1e-10, (name, ob.name, minimum)
            assert tuple(ob.location) == (0.0, 0.0, 0.0) and tuple(ob.scale) == (1.0, 1.0, 1.0)
            vertices.extend(tuple(v.co) for v in mesh.vertices)
            triangles += len(mesh.loop_triangles)
            records.append(dict(mesh=ob.name, nonmanifold_edges=nonmanifold, minimum_triangle_area=minimum, signed_volume=volume))
        bounds = [[min(v[a] for v in vertices) for a in range(3)], [max(v[a] for v in vertices) for a in range(3)]]
        assert bounds[0][2] >= -0.001, (name, bounds)
        result = dict(
            asset=name,
            status="PASS",
            local_origin=True,
            vertices=len(vertices),
            triangles=triangles,
            bounds=bounds,
            meshes=records,
        )
        all_records.append(result)
        print("VALIDATED", name, len(vertices), triangles, flush=True)

    # Setup Cycles + AgX Scene
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 20
    scene.cycles.use_denoising = True
    scene.cycles.device = "CPU"
    scene.render.threads_mode = "FIXED"
    scene.render.threads = 8
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Study world")
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.65, 0.72, 0.78, 1.0)
    scene.world.node_tree.nodes["Background"].inputs[1].default_value = 0.52
    scene.view_settings.view_transform = "AgX"

    bpy.ops.object.light_add(type="AREA", location=(-14, -18, 25))
    light = bpy.context.object
    light.data.energy = 2600
    light.data.size = 12
    light.rotation_euler = (Vector((0, 0, 0)) - light.location).to_track_quat("-Z", "Y").to_euler()

    bpy.ops.object.light_add(type="SUN", location=(-12, -14, 22))
    sun = bpy.context.object
    sun.data.energy = 2.2
    sun.data.angle = 0.14
    sun.data.color = (1.0, 0.86, 0.67)
    sun.rotation_euler = (Vector((0, 0, 0)) - sun.location).to_track_quat("-Z", "Y").to_euler()

    bpy.ops.mesh.primitive_plane_add(size=260, location=(0, 0, -0.055))
    floor = bpy.context.object
    floor.name = "STUDIO_ONLY"
    floor.data.materials.append(material("Studio ivory", (0.66, 0.67, 0.62)))

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    scene.camera = camera

    factories = [
        ("main_house", build_main_house),
        ("research_laboratory", build_research_laboratory),
        ("medical_tent", build_medical_tent),
        ("information_complex", build_information_complex),
        ("power_and_recovery", build_power_and_recovery),
        ("market_town_european", build_market_town_hall_and_stalls),
        ("hub_village_sanctum", build_hub_village_sanctum),
        ("forest_primeval_relic", build_forest_primeval_relic),
    ]

    models = {}
    for name, fn in factories:
        objs = fn()
        models[name] = objs
        validate(name, objs)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in objs:
            ob.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.export_scene.gltf(filepath=str(OUT / f"{name}.glb"), export_format="GLB", use_selection=True)
        for ob in objs:
            ob.hide_render = True

    (OUT / "geometry-validation.json").write_text(
        json.dumps(
            dict(
                status="PASS",
                blender_version=bpy.app.version_string,
                assets=all_records,
            ),
            indent=2,
        ),
        encoding="utf-8",
    )

    def show(names, offsets=None):
        for name, objs in models.items():
            for ob in objs:
                ob.hide_render = name not in names
                ob.location = (offsets or {}).get(name, (0.0, 0.0, 0.0))

    def render(filename, position, target, scale):
        camera.location = position
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.ortho_scale = scale
        scene.render.filepath = str(OUT / filename)
        bpy.ops.render.render(write_still=True)
        print("RENDERED", filename, flush=True)

    clay = material("Neutral clay", (0.5, 0.51, 0.49))
    market_only = "--market-only" in sys.argv
    village_forest_only = "--village-forest-only" in sys.argv

    if not market_only and not village_forest_only:
        # Render 1: Main House + Research Lab + Medical Tent family lineup
        show(
            ["main_house", "research_laboratory", "medical_tent"],
            {"main_house": (-11.5, 0.0, 0.0), "research_laboratory": (0.5, 0.0, 0.0), "medical_tent": (11.5, 0.0, 0.0)},
        )
        render("01-mainhouse-research-medtent-front.png", (18, -36, 23), (0, 0, 2.2), 34)
        scene.view_layers[0].material_override = clay
        render("02-mainhouse-research-medtent-clay.png", (18, -36, 23), (0, 0, 2.2), 34)
        scene.view_layers[0].material_override = None

        # Render 3: Information & Surveillance Family (Watchtower + Intel Desk + Weather Station + Threat Radar + Sky-Eye Beacon)
        show(["information_complex"])
        render("03-information-surveillance-family.png", (16, -34, 21), (0.2, 0.2, 2.6), 27)

        # Render 4: Power Generators (A/B/C) + Recovery Station + Lightning Rod
        show(["power_and_recovery"])
        render("04-generators-recovery-lightning-family.png", (16, -36, 22), (1.8, 0.0, 1.9), 30)

    if not village_forest_only:
        # Render 5: Ancient European Market Town (`Market_Town` — Romanesque Arcades, Jettied Floors, Stepped Gables, Belfry Clock Tower & Market Stalls)
        show(["market_town_european"])
        render("05-market-town-ancient-european-front.png", (15, -28, 19), (0.0, -0.3, 3.3), 21.5)
        scene.view_layers[0].material_override = clay
        render("06-market-town-ancient-european-clay.png", (15, -28, 19), (0.0, -0.3, 3.3), 21.5)
        scene.view_layers[0].material_override = None

    # Render 7 & 8: Hub_Village (Elevated NPC Highland Village) & Forest_Primeval (Ancient Sun-Moon Rune Monolith & Golden Flora)
    show(
        ["hub_village_sanctum", "forest_primeval_relic"],
        {"hub_village_sanctum": (-7.2, 0.0, 0.0), "forest_primeval_relic": (7.2, 0.0, 0.0)},
    )
    render("07-hub-village-and-forest-primeval-front.png", (16, -34, 21), (0.0, 0.0, 2.1), 29.5)
    scene.view_layers[0].material_override = clay
    render("08-hub-village-and-forest-primeval-clay.png", (16, -34, 21), (0.0, 0.0, 2.1), 29.5)
    scene.view_layers[0].material_override = None

    # Save local-origin .blend
    for name, objs in models.items():
        coll = bpy.data.collections.new(name)
        scene.collection.children.link(coll)
        for ob in objs:
            ob.location = (0.0, 0.0, 0.0)
            ob.hide_render = name != "market_town_european"
            for curr in list(ob.users_collection):
                curr.objects.unlink(ob)
            coll.objects.link(ob)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "argon-facilities-and-scenes.blend"))
    print("ARGON_STUDY_COMPLETE", str(OUT), flush=True)


if __name__ == "__main__":
    build_and_render_all()
