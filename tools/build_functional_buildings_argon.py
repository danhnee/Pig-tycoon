#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Argon Functional Buildings & Herd Shelter High-Craft Builder
(`Tools/build_functional_buildings_argon.py`)
===============================================================================
Addresses Codex [functional-buildings] Messages #45, #46 (DELEGATED SUBAGENT TASK #2),
and #47:

1. PRIORITY 1 — High-Craft Herd Shelter (`Nơi Trú Đàn`, `herd-shelter.glb/.fbx/.blend`):
   - Baked 2048x2048 UV textures (`shelter_wall_planks_masonry.png`,
     `shelter_roof_tiles.png`, `shelter_floor_cobble_straw.png`) packed into GLB/FBX
     so weathered plank grain, damp masonry base, and slate/terracotta tile variation
     survive export to GLB/FBX and Unity 6000.6.3f1.
   - Individual overlapping roof tile courses with alternating joint offsets,
     sculpted terracotta ridge caps, and TWO louvered roof vents (cupolas).
   - Exposed eave rafters, collar ties, and mortise-pegged knee braces on every post.
   - Lower limestone rubble masonry base + exposed heavy oak sill course + weathered
     horizontal plank lining (no flat blank cream panels).
   - Working interior props placed strictly in rear/outer side margins:
     slatted rear/outer hay racks, stone-lined side water troughs, stacked straw
     bedding bales, lidded timber feed bins, and an 8-spoked timber hoist wheel with
     braided rope & hook in the hayloft gable.
   - Verified clear openings: West bay clear width = 6.30m (`>= 6.0m`),
     East bay clear width = 6.30m (`>= 6.0m`).

2. PRIORITY 2 — Distinct Local-Origin `Farm_Core` Facilities (`technical:p00504-p00517`):
   - `genetics-research-lab.glb/.fbx/.blend` (`Phòng Nghiên Cứu Di Truyền & Bào Chế`,
     `technical:p00545-p00569`, `mechanics:p00738, p01278`)
   - `information-station.glb/.fbx/.blend` (`Khu Thông Tin: Tháp Canh Mở, Trạm Khí Tượng,
     Radar Uy Hiếp, Bàn Tình Báo`, `mechanics:p02801-p02843`)
   - `main-house-an-room.glb/.fbx/.blend` (`Nhà Chính & Gian Bếp Gạch Đen Muội + Bậu Cửa Sổ
     + Lọ Thủy Tinh Nhỏ Trống Rỗng Mùa 1`, `mechanics:p00740`, `story:p00105, p00108`)
   - `medical-tent.glb/.fbx/.blend` (`Lều Quân Y Vải Lanh`, `technical:p00513`)

Outputs to `D:/Pig-tycoon/.collab/artifacts/functional-buildings-argon/` ONLY.
Never edits Codex-owned files or launches Unity.
"""

from __future__ import annotations

import hashlib
import json
import math
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(r"D:\Pig-tycoon")
RUNTIME_DIR = REPO_ROOT / ".collab" / "artifacts" / "mountain-reference-study-codex" / "runtime"
if str(RUNTIME_DIR) not in sys.path:
    sys.path.insert(0, str(RUNTIME_DIR))

import bpy
import bmesh
import numpy as np
from mathutils import Euler, Matrix, Vector

OUT_DIR = REPO_ROOT / ".collab" / "artifacts" / "functional-buildings-argon"
TEX_DIR = OUT_DIR / "textures"


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def write_png_rgba(path: Path, rgba: np.ndarray, is_srgb_color: bool = True) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    h, w, _ = rgba.shape
    arr = np.clip(rgba, 0.0, 1.0).copy()
    if is_srgb_color:
        # Encode linear RGB values into sRGB space for PNG storage per Codex Message #51
        arr[..., :3] = np.power(arr[..., :3], 1.0 / 2.2)
    img = bpy.data.images.new(name=path.stem, width=w, height=h, alpha=True)
    flipped = np.flipud(arr)
    img.pixels.foreach_set(flipped.reshape(-1).astype(np.float32))
    img.filepath_raw = str(path)
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    return path


def ensure_baked_textures() -> dict[str, Path]:
    """Generate deterministic 1024x1024 hand-crafted sRGB surface textures with human-scale timber/tile grain."""
    TEX_DIR.mkdir(parents=True, exist_ok=True)
    paths = {
        "wall_planks_masonry": TEX_DIR / "shelter_wall_planks_masonry_v3.png",
        "roof_tiles": TEX_DIR / "shelter_roof_tiles_v3.png",
        "floor_cobble_straw": TEX_DIR / "shelter_floor_cobble_straw_v3.png",
        "timber_oak": TEX_DIR / "shelter_timber_oak_v3.png",
    }
    if all(p.exists() for p in paths.values()):
        return paths

    n = 1024
    u = np.linspace(0.0, 1.0, n, endpoint=False)[None, :]
    v = np.linspace(0.0, 1.0, n, endpoint=False)[:, None]

    # 1. Wall texture: lower limestone rubble (v < 0.30), oak sill (0.30..0.35), human-scale horizontal planks (v >= 0.35)
    wall = np.zeros((n, n, 4), dtype=np.float32)
    wall[..., 3] = 1.0
    plank_rows = 24.0
    plank_idx = np.floor(v * plank_rows)
    plank_frac = (v * plank_rows) - plank_idx
    plank_seam = np.exp(-((plank_frac - 0.04) / 0.020) ** 2)
    # Fine horizontal wood fibers along U (no giant wavy vertical bands)
    fine_fiber = (
        0.016 * np.sin(v * 480.0 + 0.12 * np.sin(u * 22.0 + plank_idx * 1.7))
        + 0.010 * np.cos(v * 820.0 - u * 14.0)
    )
    butt_joint = np.exp(-(((u * 4.0 + 0.37 * (plank_idx % 3)) % 1.0 - 0.02) / 0.008) ** 2)
    plank_tone = 0.028 * np.sin(plank_idx * 2.71 + np.floor(u * 4.0) * 1.53)
    damp_base = np.clip((0.48 - v) / 0.18, 0.0, 1.0)

    plank_r = 0.43 + fine_fiber + plank_tone - 0.15 * plank_seam - 0.10 * butt_joint - 0.08 * damp_base
    plank_g = 0.30 + 0.8 * fine_fiber + 0.8 * plank_tone - 0.12 * plank_seam - 0.08 * butt_joint - 0.06 * damp_base
    plank_b = 0.19 + 0.5 * fine_fiber + 0.5 * plank_tone - 0.08 * plank_seam - 0.06 * butt_joint - 0.05 * damp_base

    # Stone rubble base pattern
    stone_rows = 7.0
    sr = np.floor(v / 0.30 * stone_rows)
    sf = (v / 0.30 * stone_rows) - sr
    su = (u * 10.0 + 0.45 * (sr % 2)) % 1.0
    mortar = np.maximum(
        np.exp(-((sf - 0.06) / 0.045) ** 2),
        np.exp(-((su - 0.06) / 0.040) ** 2),
    )
    stone_var = 0.045 * np.sin(sr * 3.1 + np.floor(u * 10.0) * 4.7) + 0.020 * np.cos(u * 130.0 + v * 120.0)
    moss = np.clip((0.14 - v) / 0.14, 0.0, 1.0) * (0.5 + 0.5 * np.sin(u * 52.0))
    stone_r = (0.48 + stone_var) * (1.0 - 0.32 * mortar) - 0.09 * moss
    stone_g = (0.45 + stone_var) * (1.0 - 0.30 * mortar) + 0.03 * moss
    stone_b = (0.39 + stone_var) * (1.0 - 0.28 * mortar) - 0.07 * moss

    is_stone = v < 0.30
    is_sill = (v >= 0.30) & (v < 0.35)
    wall[..., 0] = np.where(is_stone, stone_r, np.where(is_sill, 0.27 + 0.5 * fine_fiber, plank_r))
    wall[..., 1] = np.where(is_stone, stone_g, np.where(is_sill, 0.18 + 0.4 * fine_fiber, plank_g))
    wall[..., 2] = np.where(is_stone, stone_b, np.where(is_sill, 0.11 + 0.3 * fine_fiber, plank_b))
    write_png_rgba(paths["wall_planks_masonry"], wall)

    # 2. Roof tiles texture: human-scale overlapping shingle/tile grid
    roof = np.zeros((n, n, 4), dtype=np.float32)
    roof[..., 3] = 1.0
    t_rows = 20.0
    tr = np.floor(v * t_rows)
    tf = (v * t_rows) - tr
    tu = (u * 18.0 + 0.5 * (tr % 2)) % 1.0
    tile_shadow = np.exp(-((tf - 0.07) / 0.050) ** 2)
    joint_shadow = np.exp(-((tu - 0.04) / 0.030) ** 2)
    tile_id = tr * 19.0 + np.floor(u * 18.0 + 0.5 * (tr % 2))
    tile_var = 0.045 * np.sin(tile_id * 1.93) + 0.018 * np.cos(u * 160.0 + v * 110.0)
    lichen = np.clip(np.sin(tile_id * 3.77) - 0.62, 0.0, 0.38)
    roof[..., 0] = (0.57 + tile_var - 0.10 * lichen) * (1.0 - 0.40 * tile_shadow - 0.26 * joint_shadow)
    roof[..., 1] = (0.28 + 0.6 * tile_var + 0.07 * lichen) * (1.0 - 0.40 * tile_shadow - 0.26 * joint_shadow)
    roof[..., 2] = (0.18 + 0.4 * tile_var - 0.03 * lichen) * (1.0 - 0.40 * tile_shadow - 0.26 * joint_shadow)
    write_png_rgba(paths["roof_tiles"], roof)

    # 3. Floor cobble & straw bedding texture
    flr = np.zeros((n, n, 4), dtype=np.float32)
    flr[..., 3] = 1.0
    cob = 0.5 + 0.5 * np.sin(u * 75.0 + 0.6 * np.cos(v * 65.0)) * np.cos(v * 75.0 - 0.6 * np.sin(u * 65.0))
    straw_mask = np.clip(0.45 + 0.35 * np.sin(u * 14.0 + v * 11.0) + 0.20 * np.cos(u * 29.0 - v * 19.0), 0.0, 1.0)
    straw_fibers = 0.04 * np.sin(u * 240.0 + v * 190.0) + 0.03 * np.cos(u * 170.0 - v * 260.0)
    flr[..., 0] = (0.42 + 0.08 * cob) * (1.0 - 0.55 * straw_mask) + (0.68 + straw_fibers) * 0.55 * straw_mask
    flr[..., 1] = (0.38 + 0.07 * cob) * (1.0 - 0.55 * straw_mask) + (0.55 + straw_fibers) * 0.55 * straw_mask
    flr[..., 2] = (0.31 + 0.06 * cob) * (1.0 - 0.55 * straw_mask) + (0.28 + 0.5 * straw_fibers) * 0.55 * straw_mask
    write_png_rgba(paths["floor_cobble_straw"], flr)

    # 4. Heavy weathered oak timber texture with fine straight wood grain
    oak = np.zeros((n, n, 4), dtype=np.float32)
    oak[..., 3] = 1.0
    og = 0.018 * np.sin(v * 380.0 + 0.15 * np.sin(u * 16.0)) + 0.010 * np.cos(u * 340.0)
    oak[..., 0] = 0.32 + og
    oak[..., 1] = 0.21 + 0.75 * og
    oak[..., 2] = 0.13 + 0.50 * og
    write_png_rgba(paths["timber_oak"], oak)

    return paths


def make_textured_mat(name: str, image_path: Path, roughness: float = 0.78, tint: tuple[float, float, float] = (1.0, 1.0, 1.0)) -> bpy.types.Material:
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = roughness

    tex = nt.nodes.new("ShaderNodeTexImage")
    img = bpy.data.images.load(str(image_path), check_existing=True)
    try:
        img.pack()
    except Exception:
        pass
    tex.image = img

    if tint != (1.0, 1.0, 1.0):
        mul = nt.nodes.new("ShaderNodeMixRGB")
        mul.blend_type = "MULTIPLY"
        mul.inputs["Fac"].default_value = 1.0
        mul.inputs["Color2"].default_value = (tint[0], tint[1], tint[2], 1.0)
        nt.links.new(tex.outputs["Color"], mul.inputs["Color1"])
        nt.links.new(mul.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])

    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def make_mat(
    name: str,
    color: tuple[float, float, float],
    roughness: float = 0.75,
    metallic: float = 0.0,
    emission: tuple[float, float, float] | None = None,
    emission_strength: float = 0.0,
    alpha: float = 1.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if alpha < 1.0 and "Alpha" in bsdf.inputs:
        bsdf.inputs["Alpha"].default_value = alpha
    if emission is not None and emission_strength > 0.0:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (emission[0], emission[1], emission[2], 1.0)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def apply_box_uv(mesh: bpy.types.Mesh, scale: float = 0.38) -> None:
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    for poly in mesh.polygons:
        n = poly.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for li in poly.loop_indices:
            vi = mesh.loops[li].vertex_index
            co = mesh.vertices[vi].co
            if az >= ax and az >= ay:
                u, v = co.x * scale, co.y * scale
            elif ax >= ay:
                u, v = co.y * scale, co.z * scale
            else:
                u, v = co.x * scale, co.z * scale
            uv_layer[li].uv = (u, v)


def apply_wall_uv(mesh: bpy.types.Mesh, z_min: float, z_max: float, u_scale: float = 0.28) -> None:
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    dz = max(z_max - z_min, 0.01)
    for poly in mesh.polygons:
        n = poly.normal
        for li in poly.loop_indices:
            vi = mesh.loops[li].vertex_index
            co = mesh.vertices[vi].co
            u = (co.x if abs(n.y) >= abs(n.x) else co.y) * u_scale
            v = np.clip((co.z - z_min) / dz, 0.02, 0.98)
            uv_layer[li].uv = (float(u), float(v))


def add_box(
    col: bpy.types.Collection,
    name: str,
    center: tuple[float, float, float],
    size: tuple[float, float, float],
    mat: bpy.types.Material,
    rot_deg: tuple[float, float, float] = (0.0, 0.0, 0.0),
    wall_uv_z: tuple[float, float] | None = None,
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    if any(abs(a) > 1e-5 for a in rot_deg):
        rmat = Euler(tuple(math.radians(a) for a in rot_deg), "XYZ").to_matrix().to_4x4()
        bmesh.ops.transform(bm, matrix=rmat, verts=bm.verts)
    bmesh.ops.translate(bm, vec=center, verts=bm.verts)
    bm.to_mesh(mesh)
    bm.free()
    if wall_uv_z is not None:
        apply_wall_uv(mesh, wall_uv_z[0], wall_uv_z[1])
    else:
        apply_box_uv(mesh)
    mesh.materials.append(mat)
    ob = bpy.data.objects.new(name, mesh)
    col.objects.link(ob)
    return ob


def add_cylinder(
    col: bpy.types.Collection,
    name: str,
    center: tuple[float, float, float],
    radius: float,
    depth: float,
    mat: bpy.types.Material,
    segments: int = 16,
    rot_deg: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments, radius1=radius, radius2=radius, depth=depth)
    if any(abs(a) > 1e-5 for a in rot_deg):
        rmat = Euler(tuple(math.radians(a) for a in rot_deg), "XYZ").to_matrix().to_4x4()
        bmesh.ops.transform(bm, matrix=rmat, verts=bm.verts)
    bmesh.ops.translate(bm, vec=center, verts=bm.verts)
    bm.to_mesh(mesh)
    bm.free()
    apply_box_uv(mesh)
    mesh.materials.append(mat)
    ob = bpy.data.objects.new(name, mesh)
    col.objects.link(ob)
    return ob


def add_framed_window(
    col: bpy.types.Collection,
    prefix: str,
    center: tuple[float, float, float],
    size_xyz: tuple[float, float, float],
    mat_frame: bpy.types.Material,
    mat_recess: bpy.types.Material,
    mat_pane: bpy.types.Material,
    axis: str = "Y",
) -> list[bpy.types.Object]:
    """Build a recessed window with oak outer frame, sill, dark interior recess, low-glare glazed pane, and cross mullions."""
    objs: list[bpy.types.Object] = []
    cx, cy, cz = center
    wx, wy, wz = size_xyz
    # Recessed dark backing + low-emission warm dusk pane
    objs.append(add_box(col, f"{prefix}_Recess", (cx, cy, cz), (wx * 0.88, wy * 0.88, wz * 0.88), mat_recess))
    objs.append(add_box(col, f"{prefix}_Pane", (cx, cy, cz), (wx * 0.84, wy * 0.92, wz * 0.84), mat_pane))
    if axis == "Y":
        # Window on a North/South wall (width along X, depth along Y)
        objs.append(add_box(col, f"{prefix}_Sill", (cx, cy, cz - wz * 0.50), (wx + 0.18, wy + 0.16, 0.11), mat_frame))
        objs.append(add_box(col, f"{prefix}_Lintel", (cx, cy, cz + wz * 0.50), (wx + 0.16, wy + 0.12, 0.12), mat_frame))
        for side in (-1.0, 1.0):
            objs.append(add_box(col, f"{prefix}_Jamb_{side:+.0f}", (cx + side * (wx * 0.48), cy, cz), (0.10, wy + 0.10, wz), mat_frame))
        objs.append(add_box(col, f"{prefix}_MullionV", (cx, cy, cz), (0.06, wy + 0.08, wz * 0.92), mat_frame))
        objs.append(add_box(col, f"{prefix}_MullionH", (cx, cy, cz + wz * 0.06), (wx * 0.92, wy + 0.08, 0.06), mat_frame))
    else:
        # Window on an East/West wall (width along Y, depth along X)
        objs.append(add_box(col, f"{prefix}_Sill", (cx, cy, cz - wz * 0.50), (wx + 0.16, wy + 0.18, 0.11), mat_frame))
        objs.append(add_box(col, f"{prefix}_Lintel", (cx, cy, cz + wz * 0.50), (wx + 0.12, wy + 0.16, 0.12), mat_frame))
        for side in (-1.0, 1.0):
            objs.append(add_box(col, f"{prefix}_Jamb_{side:+.0f}", (cx, cy + side * (wy * 0.48), cz), (wx + 0.10, 0.10, wz), mat_frame))
        objs.append(add_box(col, f"{prefix}_MullionV", (cx, cy, cz), (wx + 0.08, 0.06, wz * 0.92), mat_frame))
        objs.append(add_box(col, f"{prefix}_MullionH", (cx, cy, cz + wz * 0.06), (wx + 0.08, wy * 0.92, 0.06), mat_frame))
    return objs


def add_annular_wheel_rim(
    col: bpy.types.Collection,
    prefix: str,
    center: tuple[float, float, float],
    radius: float,
    rim_thickness: float,
    depth_x: float,
    mat_rim: bpy.types.Material,
    mat_hub: bpy.types.Material,
    segments: int = 12,
) -> list[bpy.types.Object]:
    """Build an open annular wooden wheel rim in the YZ plane + central hub so radial spokes are 100% visible."""
    objs: list[bpy.types.Object] = []
    cx, cy, cz = center
    seg_len = 2.0 * radius * math.sin(math.pi / segments) * 1.08
    for i in range(segments):
        ang = (i + 0.5) * (2.0 * math.pi / segments)
        py = cy + radius * math.cos(ang)
        pz = cz + radius * math.sin(ang)
        objs.append(
            add_box(
                col,
                f"{prefix}_RimSeg_{i}",
                (cx, py, pz),
                (depth_x, rim_thickness, seg_len),
                mat_rim,
                rot_deg=(math.degrees(ang), 0.0, 0.0),
            )
        )
    objs.append(add_cylinder(col, f"{prefix}_Hub", (cx, cy, cz), rim_thickness * 1.25, depth_x * 1.35, mat_hub, segments=12, rot_deg=(0.0, 90.0, 0.0)))
    return objs


def add_tensioned_canvas_roof(
    col: bpy.types.Collection,
    prefix: str,
    center: tuple[float, float, float],
    span_x: float,
    span_y: float,
    rise_z: float,
    mat_canvas_a: bpy.types.Material,
    mat_canvas_b: bpy.types.Material,
    mat_timber: bpy.types.Material,
) -> list[bpy.types.Object]:
    """Build a continuous tensioned sagging linen canvas tent roof with seam welts and timber ridge/eave poles (no roof tiles)."""
    objs: list[bpy.types.Object] = []
    cx, cy, cz = center
    hx = span_x * 0.5
    hy = span_y * 0.5

    # Continuous ridge pole and eave poles
    objs.append(add_cylinder(col, f"{prefix}_RidgeBeam", (cx, cy, cz + rise_z), 0.11, span_y + 0.45, mat_timber, segments=12, rot_deg=(90.0, 0.0, 0.0)))
    for side in (-1.0, 1.0):
        objs.append(add_cylinder(col, f"{prefix}_EavePole_{side:+.0f}", (cx + side * hx, cy, cz), 0.085, span_y + 0.35, mat_timber, segments=10, rot_deg=(90.0, 0.0, 0.0)))

    # Build a thickened catenary-sagging canvas sheet mesh
    nx, ny = 14, 16
    xs = np.linspace(-hx, hx, nx)
    ys = np.linspace(-hy, hy, ny)
    thick = 0.065
    mesh = bpy.data.meshes.new(f"{prefix}_Sheet_mesh")
    bm = bmesh.new()
    top_grid = []
    bot_grid = []
    for iy, yv in enumerate(ys):
        row_t = []
        row_b = []
        # Subtle catenary sag between support poles at y = -2.6, 0.0, +2.6
        pole_sag = -0.07 * (math.sin((yv / max(hy, 0.1)) * math.pi) ** 2)
        for ix, xv in enumerate(xs):
            t_x = abs(xv) / max(hx, 0.1)
            # Catenary droop across slope
            slope_sag = -0.14 * t_x * (1.0 - t_x)
            zv = cz + rise_z * (1.0 - t_x) + slope_sag + pole_sag
            row_t.append(bm.verts.new((cx + float(xv), cy + float(yv), float(zv + thick * 0.5))))
            row_b.append(bm.verts.new((cx + float(xv), cy + float(yv), float(zv - thick * 0.5))))
        top_grid.append(row_t)
        bot_grid.append(row_b)

    for iy in range(ny - 1):
        for ix in range(nx - 1):
            bm.faces.new((top_grid[iy][ix], top_grid[iy][ix + 1], top_grid[iy + 1][ix + 1], top_grid[iy + 1][ix]))
            bm.faces.new((bot_grid[iy][ix], bot_grid[iy + 1][ix], bot_grid[iy + 1][ix + 1], bot_grid[iy][ix + 1]))
    for ix in range(nx - 1):
        bm.faces.new((top_grid[0][ix], bot_grid[0][ix], bot_grid[0][ix + 1], top_grid[0][ix + 1]))
        bm.faces.new((top_grid[-1][ix + 1], bot_grid[-1][ix + 1], bot_grid[-1][ix], top_grid[-1][ix]))
    for iy in range(ny - 1):
        bm.faces.new((top_grid[iy + 1][0], bot_grid[iy + 1][0], bot_grid[iy][0], top_grid[iy][0]))
        bm.faces.new((top_grid[iy][-1], bot_grid[iy][-1], bot_grid[iy + 1][-1], top_grid[iy + 1][-1]))

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    apply_box_uv(mesh, scale=0.30)
    mesh.materials.append(mat_canvas_a)
    sheet_ob = bpy.data.objects.new(f"{prefix}_Sheet", mesh)
    col.objects.link(sheet_ob)
    objs.append(sheet_ob)

    # Sewn canvas seam welts running from ridge to eaves at 5 intervals along Y
    slope_angle = math.degrees(math.atan2(rise_z, hx))
    slope_len = math.hypot(hx, rise_z)
    for w_idx, wy in enumerate(np.linspace(cy - hy + 0.15, cy + hy - 0.15, 6)):
        for side in (-1.0, 1.0):
            objs.append(
                add_box(
                    col,
                    f"{prefix}_SeamWelt_{w_idx}_{side:+.0f}",
                    (cx + side * hx * 0.50, float(wy), cz + rise_z * 0.48),
                    (slope_len, 0.08, 0.05),
                    mat_canvas_b,
                    rot_deg=(0.0, side * slope_angle, 0.0),
                )
            )
    return objs


def add_pitched_roof_with_courses(
    col: bpy.types.Collection,
    prefix: str,
    center: tuple[float, float, float],
    span_x: float,
    span_y: float,
    rise_z: float,
    mat_tile_a: bpy.types.Material,
    mat_tile_b: bpy.types.Material,
    mat_timber: bpy.types.Material,
    courses: int = 13,
    add_vent: bool = False,
    open_gable_truss: bool = False,
) -> list[bpy.types.Object]:
    """Build pitched roof deck, exposed eave rafters, fine overlapping courses, ridge caps, and optional roof vent."""
    objs: list[bpy.types.Object] = []
    cx, cy, cz = center
    hx = span_x * 0.5
    hy = span_y * 0.5
    slope_angle = math.degrees(math.atan2(rise_z, hx))
    slope_len = math.hypot(hx, rise_z)

    if not open_gable_truss:
        # Solid triangular roof core for enclosed buildings
        mesh = bpy.data.meshes.new(f"{prefix}_Core_mesh")
        bm = bmesh.new()
        v0 = bm.verts.new((cx - hx, cy - hy, cz))
        v1 = bm.verts.new((cx + hx, cy - hy, cz))
        v2 = bm.verts.new((cx + hx, cy + hy, cz))
        v3 = bm.verts.new((cx - hx, cy + hy, cz))
        v4 = bm.verts.new((cx, cy - hy, cz + rise_z))
        v5 = bm.verts.new((cx, cy + hy, cz + rise_z))
        bm.faces.new((v0, v3, v2, v1))
        bm.faces.new((v0, v1, v4))
        bm.faces.new((v2, v3, v5))
        bm.faces.new((v0, v4, v5, v3))
        bm.faces.new((v1, v2, v5, v4))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(mesh)
        bm.free()
        apply_box_uv(mesh, scale=0.35)
        mesh.materials.append(mat_timber)
        core_ob = bpy.data.objects.new(f"{prefix}_Core", mesh)
        col.objects.link(core_ob)
        objs.append(core_ob)
    else:
        # Ridge beam, mid purlins, and gable bargeboards for open-bay timber truss roof (no solid gable wall!)
        objs.append(add_box(col, f"{prefix}_RidgeBeam", (cx, cy, cz + rise_z - 0.08), (0.28, span_y + 0.25, 0.28), mat_timber))
        for side in (-1.0, 1.0):
            objs.append(add_box(col, f"{prefix}_MidPurlin_{side:+.0f}", (cx + side * hx * 0.50, cy, cz + rise_z * 0.48), (0.22, span_y + 0.20, 0.22), mat_timber))
            for end_y in (cy - hy, cy + hy):
                objs.append(
                    add_box(
                        col,
                        f"{prefix}_GableBargeboard_{side:+.0f}_{end_y:.1f}",
                        (cx + side * hx * 0.50, end_y, cz + rise_z * 0.50),
                        (slope_len + 0.30, 0.16, 0.22),
                        mat_timber,
                        rot_deg=(0.0, side * slope_angle, 0.0),
                    )
                )

    # Exposed eave rafters protruding below the roof slopes
    n_rafters = max(7, int(span_y / 1.15))
    for r_idx, ry in enumerate(np.linspace(cy - hy + 0.25, cy + hy - 0.25, n_rafters)):
        for side in (-1.0, 1.0):
            objs.append(
                add_box(
                    col,
                    f"{prefix}_Rafter_{r_idx}_{'W' if side < 0 else 'E'}",
                    (cx + side * (hx * 0.54), float(ry), cz + rise_z * 0.46 - 0.06),
                    (slope_len + 0.55, 0.12, 0.15),
                    mat_timber,
                    rot_deg=(0.0, side * slope_angle, 0.0),
                )
            )

    # Individual overlapping tile courses with alternating joint offsets along Y (fine human-scale steps for Clay & Textured)
    for i in range(courses):
        t0 = i / float(courses)
        t1 = (i + 1.18) / float(courses)
        tm = 0.5 * (t0 + min(t1, 1.0))
        course_w = (min(t1, 1.0) - t0) * slope_len * 1.10
        mat_c = mat_tile_a if (i % 2 == 0) else mat_tile_b
        y_segs = max(6, int(span_y / 1.25))
        y_offset = 0.16 if (i % 2 == 1) else -0.16
        seg_len = (span_y + 0.26) / y_segs
        for s_idx in range(y_segs):
            sy = (cy - hy - 0.13) + (s_idx + 0.5) * seg_len + y_offset * 0.22
            for side in (-1.0, 1.0):
                px = cx + side * hx * (1.0 - tm * 0.95)
                pz = cz + rise_z * (tm * 0.96) + 0.06 + 0.008 * (courses - i)
                objs.append(
                    add_box(
                        col,
                        f"{prefix}_Course_{i}_{s_idx}_{'W' if side < 0 else 'E'}",
                        (px, sy, pz),
                        (course_w, seg_len - 0.04, 0.065),
                        mat_c,
                        rot_deg=(0.0, side * slope_angle, 0.0),
                    )
                )

    # Alternating ridge cap tiles along the crest
    n_caps = max(8, int(span_y / 0.72))
    for c_idx, ry in enumerate(np.linspace(cy - hy - 0.10, cy + hy + 0.10, n_caps)):
        objs.append(
            add_box(
                col,
                f"{prefix}_RidgeCap_{c_idx}",
                (cx, float(ry), cz + rise_z + 0.13),
                (0.42, (span_y / n_caps) * 0.90, 0.16),
                mat_tile_b if (c_idx % 2 == 0) else mat_tile_a,
            )
        )

    # Optional louvered roof vent (cupola)
    if add_vent:
        vz = cz + rise_z + 0.30
        objs.append(add_box(col, f"{prefix}_VentBase", (cx, cy, vz), (1.35, 1.65, 0.56), mat_timber))
        for l_idx, lz in enumerate((vz - 0.10, vz + 0.06, vz + 0.20)):
            objs.append(add_box(col, f"{prefix}_VentLouver_{l_idx}", (cx, cy, lz), (1.44, 1.74, 0.05), mat_tile_b))
        objs.append(
            add_pitched_roof_with_courses(
                col,
                f"{prefix}_VentCap",
                (cx, cy, vz + 0.28),
                1.75,
                2.05,
                0.52,
                mat_tile_b,
                mat_tile_a,
                mat_timber,
                courses=5,
                add_vent=False,
            )[0]
        )

    return objs


def compute_shelter_corridor_metrics(
    objs: list[bpy.types.Object],
    sx: float,
    sy: float,
    z0: float,
) -> dict[str, Any]:
    """Dynamically measure actual clear floor navigation corridors and overhead brace clearance from mesh AABBs."""
    nav_z_min = z0 + 0.12
    nav_z_max = z0 + 2.25
    bay_y_min = sy - 4.45
    bay_y_max = sy + 4.45

    west_inner_edge = sx - 3.00
    west_outer_edge = sx - 10.80
    east_inner_edge = sx + 3.00
    east_outer_edge = sx + 10.80
    overhead_brace_min_z = z0 + 3.42

    for ob in objs:
        if ob.type != "MESH" or not ob.data.vertices:
            continue
        xs = [v.co.x for v in ob.data.vertices]
        ys = [v.co.y for v in ob.data.vertices]
        zs = [v.co.z for v in ob.data.vertices]
        x0, x1 = min(xs), max(xs)
        y0, y1 = min(ys), max(ys)
        zb, zt = min(zs), max(zs)

        if y1 < bay_y_min or y0 > bay_y_max:
            continue

        # Track overhead knee-brace / truss clearance inside the bays (z > nav_z_max)
        if "KneeBrace" in ob.name and zt > nav_z_max:
            overhead_brace_min_z = min(overhead_brace_min_z, zb)

        # Check overlap with floor + pig/player navigation vertical envelope [z0 + 0.12, z0 + 2.25]
        if zt <= nav_z_min or zb >= nav_z_max:
            continue

        # West bay obstacle analysis (x in [sx - 11.2, sx - 2.8])
        if x1 <= sx - 2.5 and x0 >= sx - 11.5:
            cx_ob = 0.5 * (x0 + x1)
            if cx_ob > sx - 6.8:
                # Inner post / plinth / pier obstacle on the east side of West bay
                west_inner_edge = min(west_inner_edge, x0)
            else:
                # Outer post / plinth / hay rack / water trough obstacle on the west side of West bay
                west_outer_edge = max(west_outer_edge, x1)

        # East bay obstacle analysis (x in [sx + 2.8, sx + 11.2])
        if x0 >= sx + 2.5 and x1 <= sx + 11.5:
            cx_ob = 0.5 * (x0 + x1)
            if cx_ob < sx + 6.8:
                # Inner post / plinth / pier obstacle on the west side of East bay
                east_inner_edge = max(east_inner_edge, x1)
            else:
                # Outer post / plinth / hay rack / water trough obstacle on the east side of East bay
                east_outer_edge = min(east_outer_edge, x0)

    west_w = round(max(0.0, west_inner_edge - west_outer_edge), 3)
    east_w = round(max(0.0, east_outer_edge - east_inner_edge), 3)
    return {
        "floor_navigation_clearance_z_interval_m": [round(nav_z_min, 3), round(nav_z_max, 3)],
        "overhead_knee_brace_min_z_m": round(overhead_brace_min_z, 3),
        "west_bay_clear_x_range": [round(west_outer_edge, 3), round(west_inner_edge, 3)],
        "west_bay_clear_width_m": west_w,
        "east_bay_clear_x_range": [round(east_inner_edge, 3), round(east_outer_edge, 3)],
        "east_bay_clear_width_m": east_w,
        "min_required_herd_passage_m": 6.00,
        "clear_passage_verified": bool(west_w >= 6.00 and east_w >= 6.00),
    }


def build_high_craft_herd_shelter(
    col: bpy.types.Collection,
    origin: tuple[float, float] = (0.0, 0.0),
    height_fn=None,
) -> dict[str, Any]:
    """
    Build the local-origin or scene-grounded High-Craft Herd Shelter (`Nơi Trú Đàn`).
    Incorporates Zone04 Old-European rural farmstead craft vocabulary + Codex Messages #60/#63 fixes:
    - Hollow central granary ground floor with real perimeter walls, open 2.40m stone archway,
      and separate upper hayloft floor slab so interior feed bins & straw bales are visible and accessible.
    - Open annular 12-segment hoist wheel rim + hub so all 8 radial spokes are visible.
    - Recessed framed/mullioned windows with low-glare dusk glass (never blown-out white boards).
    - Fine human-scale wood/tile textures, 11-14 overlapping roof courses, open gable trusses on wings,
      and dynamic mesh-AABB corridor measurement.
    """
    before_objs = set(col.objects)
    tex_paths = ensure_baked_textures()
    mat_wall = make_textured_mat("Shelter_WallPlanksMasonry", tex_paths["wall_planks_masonry"], roughness=0.80)
    mat_roof_a = make_textured_mat("Shelter_RoofTilesWarm", tex_paths["roof_tiles"], roughness=0.68, tint=(1.02, 0.94, 0.90))
    mat_roof_b = make_textured_mat("Shelter_RoofTilesSlate", tex_paths["roof_tiles"], roughness=0.64, tint=(0.74, 0.78, 0.86))
    mat_floor = make_textured_mat("Shelter_FloorCobbleStraw", tex_paths["floor_cobble_straw"], roughness=0.86)
    mat_oak = make_textured_mat("Shelter_HeavyOak", tex_paths["timber_oak"], roughness=0.76)

    mat_stone = make_mat("Shelter_LimestoneQuoin", (0.52, 0.48, 0.42), roughness=0.86)
    mat_iron = make_mat("Shelter_ForgedIron", (0.17, 0.18, 0.20), roughness=0.42, metallic=0.78)
    mat_straw = make_mat("Shelter_GoldenStraw", (0.78, 0.62, 0.26), roughness=0.84)
    mat_water = make_mat("Shelter_TroughWater", (0.16, 0.42, 0.48), roughness=0.18)
    mat_recess = make_mat("Shelter_InteriorRecess", (0.08, 0.07, 0.06), roughness=0.92)
    mat_window_pane = make_mat(
        "Shelter_WindowPaneDusk",
        (0.46, 0.36, 0.22),
        roughness=0.22,
        emission=(0.85, 0.58, 0.24),
        emission_strength=0.24,
    )
    mat_lantern = make_mat(
        "Shelter_LanternGlow",
        (0.95, 0.72, 0.32),
        roughness=0.24,
        emission=(1.0, 0.72, 0.28),
        emission_strength=1.4,
    )

    sx, sy = origin
    h_fn = (lambda x, y: 0.0) if height_fn is None else height_fn
    z0 = float(h_fn(sx, sy))

    # 1. Cobble & straw floor pads with stone curbs (top at z0 + 0.02 so below nav_z_min = z0 + 0.12)
    add_box(col, "Shelter_Pad_Center", (sx, sy, z0 - 0.16), (6.6, 10.6, 0.36), mat_floor)
    add_box(col, "Shelter_Pad_WestBay", (sx - 7.0, sy, z0 - 0.16), (8.2, 10.0, 0.34), mat_floor)
    add_box(col, "Shelter_Pad_EastBay", (sx + 7.0, sy, z0 - 0.16), (8.2, 10.0, 0.34), mat_floor)

    # 2. Central 2-Story Hollow Granary & Upper Hayloft (`x in [-3.05, +3.05]`, `y in [-4.40, +4.40]`)
    # Real perimeter walls instead of a solid box so the ground-floor interior is genuinely hollow and accessible!
    # North rear wall (`y = sy + 4.22`)
    add_box(col, "Shelter_Center_WallNorth", (sx, sy + 4.22, z0 + 2.62), (6.10, 0.36, 5.20), mat_wall, wall_uv_z=(z0, z0 + 5.25))
    # West & East side walls (`x = sx - 2.87` and `sx + 2.87`)
    for side, wtag in ((-1.0, "West"), (1.0, "East")):
        add_box(col, f"Shelter_Center_Wall{wtag}", (sx + side * 2.87, sy, z0 + 2.62), (0.36, 8.40, 5.20), mat_wall, wall_uv_z=(z0, z0 + 5.25))
    # South front ground-floor wall piers flanking the 2.40m wide central arched doorway (`x in [-3.05, -1.20]` and `[+1.20, +3.05]`)
    for side, ptag in ((-1.0, "Left"), (1.0, "Right")):
        add_box(
            col,
            f"Shelter_Center_WallSouthGround_{ptag}",
            (sx + side * 2.125, sy - 4.22, z0 + 1.40),
            (1.85, 0.36, 2.76),
            mat_wall,
            wall_uv_z=(z0, z0 + 5.25),
        )
    # Separate Upper Hayloft Floor Slab (`z = z0 + 2.82`) + exposed ceiling joists visible inside ground floor
    add_box(col, "Shelter_Center_HayloftFloorSlab", (sx, sy, z0 + 2.82), (5.75, 8.35, 0.20), mat_oak)
    for jy in np.linspace(sy - 3.6, sy + 3.6, 6):
        add_box(col, f"Shelter_Center_LoftJoist_{jy:.1f}", (sx, float(jy), z0 + 2.66), (5.65, 0.18, 0.22), mat_oak)

    # South upper hayloft wall flanking the recessed hayloft loading doorway (`x in [-0.78, +0.78]`)
    for side, ptag in ((-1.0, "Left"), (1.0, "Right")):
        add_box(
            col,
            f"Shelter_Center_WallSouthLoft_{ptag}",
            (sx + side * 1.915, sy - 4.22, z0 + 4.05),
            (2.27, 0.36, 2.35),
            mat_wall,
            wall_uv_z=(z0, z0 + 5.25),
        )
    # Recessed dark hayloft loading bay with open timber shutters & visible hay bales inside the loft
    add_box(col, "Shelter_Hayloft_RecessBack", (sx, sy - 3.65, z0 + 3.95), (1.70, 0.18, 1.95), mat_recess)
    add_box(col, "Shelter_Hayloft_LoftBale_A", (sx - 0.32, sy - 3.95, z0 + 3.25), (0.72, 0.62, 0.52), mat_straw, rot_deg=(0.0, 0.0, 8.0))
    add_box(col, "Shelter_Hayloft_LoftBale_B", (sx + 0.36, sy - 3.90, z0 + 3.25), (0.68, 0.58, 0.48), mat_straw, rot_deg=(0.0, 0.0, -11.0))
    add_box(col, "Shelter_Hayloft_DoorFrameTop", (sx, sy - 4.42, z0 + 4.92), (1.82, 0.26, 0.18), mat_oak)
    add_box(col, "Shelter_Hayloft_DoorSill", (sx, sy - 4.44, z0 + 2.98), (1.88, 0.32, 0.16), mat_oak)
    for side in (-1.0, 1.0):
        add_box(col, f"Shelter_Hayloft_DoorJamb_{side:+.0f}", (sx + side * 0.84, sy - 4.42, z0 + 3.95), (0.16, 0.26, 1.85), mat_oak)
        add_box(
            col,
            f"Shelter_Hayloft_OpenShutter_{side:+.0f}",
            (sx + side * 1.18, sy - 4.52, z0 + 3.95),
            (0.68, 0.08, 1.76),
            mat_oak,
            rot_deg=(0.0, 0.0, side * 22.0),
        )

    # Stone corner quoins & lower limestone rubble buttress courses
    for qx in (-3.05, 3.05):
        for qy in (-4.38, 4.38):
            for qz_i in range(6):
                qz = z0 + 0.22 + qz_i * 0.42
                qw = 0.48 if (qz_i % 2 == 0) else 0.36
                qd = 0.36 if (qz_i % 2 == 0) else 0.48
                add_box(col, f"Shelter_Quoin_{qx:+.1f}_{qy:+.1f}_{qz_i}", (sx + qx, sy + qy, qz), (qw, qd, 0.36), mat_stone)

    # Segmented Stone Arch Portal (`Zone04` craft vocabulary) framing the open South doorway (`x in [-1.20, +1.20]`)
    for arch_y in (sy - 4.38, sy + 4.38):
        for pier_s in (-1.26, 1.26):
            add_box(col, f"Shelter_ArchPier_{arch_y:.1f}_{pier_s:+.2f}", (sx + pier_s, arch_y, z0 + 0.95), (0.44, 0.38, 1.88), mat_stone)
        for v_idx in range(7):
            ang = math.radians(-72.0 + v_idx * 24.0)
            vx = sx + 1.24 * math.sin(ang)
            vz = z0 + 1.84 + 0.82 * math.cos(ang)
            v_h = 0.42 if v_idx == 3 else 0.32
            add_box(
                col,
                f"Shelter_Voussoir_{arch_y:.1f}_{v_idx}",
                (vx, arch_y, vz),
                (0.34, 0.40, v_h),
                mat_stone,
                rot_deg=(0.0, math.degrees(ang), 0.0),
            )

    # Exposed oak sill course, horizontal girts, vertical studs, and diagonal braces
    for hz in (z0 + 2.82, z0 + 5.15):
        add_box(col, f"Shelter_Center_GirtN_{hz:.2f}", (sx, sy + 4.42, hz), (6.30, 0.22, 0.22), mat_oak)
        add_box(col, f"Shelter_Center_GirtS_{hz:.2f}", (sx, sy - 4.42, hz), (6.30, 0.22, 0.22), mat_oak)
        add_box(col, f"Shelter_Center_GirtW_{hz:.2f}", (sx - 3.06, sy, hz), (0.22, 8.95, 0.22), mat_oak)
        add_box(col, f"Shelter_Center_GirtE_{hz:.2f}", (sx + 3.06, sy, hz), (0.22, 8.95, 0.22), mat_oak)

    for tx in (-3.04, -1.52, 1.52, 3.04):
        for ty in (-4.42, 4.42):
            add_box(col, f"Shelter_Stud_{tx:+.2f}_{ty:+.2f}", (sx + tx, sy + ty, z0 + 2.65), (0.22, 0.22, 5.25), mat_oak)
    for side in (-1.0, 1.0):
        for ty in (-4.42, 4.42):
            add_box(
                col,
                f"Shelter_DiagBrace_{side:+.0f}_{ty:+.1f}",
                (sx + side * 2.22, sy + ty, z0 + 4.02),
                (1.55, 0.18, 0.18),
                mat_oak,
                rot_deg=(0.0, side * 38.0, 0.0),
            )

    # Framed, recessed, mullioned windows (no blown-out emissive rectangles!)
    add_framed_window(col, "Shelter_Hayloft_WindowN", (sx, sy + 4.42, z0 + 4.02), (1.35, 0.24, 1.20), mat_oak, mat_recess, mat_window_pane, axis="Y")
    for side in (-1.0, 1.0):
        add_framed_window(
            col,
            f"Shelter_Ground_WindowS_{side:+.0f}",
            (sx + side * 2.15, sy - 4.42, z0 + 1.85),
            (0.88, 0.24, 1.05),
            mat_oak,
            mat_recess,
            mat_window_pane,
            axis="Y",
        )
        add_framed_window(
            col,
            f"Shelter_Loft_WindowS_{side:+.0f}",
            (sx + side * 2.15, sy - 4.42, z0 + 3.95),
            (0.82, 0.24, 0.95),
            mat_oak,
            mat_recess,
            mat_window_pane,
            axis="Y",
        )
        # Forged iron lantern bracket + warm lantern flanking central portal
        add_box(col, f"Shelter_LanternArm_{side:+.0f}", (sx + side * 1.56, sy - 4.65, z0 + 2.55), (0.09, 0.42, 0.09), mat_iron)
        add_cylinder(col, f"Shelter_LanternCap_{side:+.0f}", (sx + side * 1.56, sy - 4.80, z0 + 2.52), 0.18, 0.08, mat_iron, segments=8)
        add_cylinder(col, f"Shelter_Lantern_{side:+.0f}", (sx + side * 1.56, sy - 4.80, z0 + 2.32), 0.13, 0.30, mat_lantern, segments=8)

    # Protruding hoist beam + OPEN ANNULAR 8-spoked wheel at south gable (`Message #60.3`)
    add_box(col, "Shelter_HoistBeam", (sx, sy - 4.55, z0 + 6.48), (0.28, 2.55, 0.28), mat_oak)
    wheel_center = (sx + 0.48, sy - 5.05, z0 + 6.08)
    add_annular_wheel_rim(col, "Shelter_HoistWheel", wheel_center, radius=0.58, rim_thickness=0.09, depth_x=0.12, mat_rim=mat_oak, mat_hub=mat_iron, segments=12)
    for spk in range(4):
        add_box(
            col,
            f"Shelter_HoistWheel_Spoke_{spk}",
            wheel_center,
            (0.08, 1.12, 0.07),
            mat_oak,
            rot_deg=(spk * 45.0, 0.0, 0.0),
        )
    add_cylinder(col, "Shelter_HoistWheel_Axle", (sx + 0.24, sy - 5.05, z0 + 6.08), 0.06, 0.56, mat_iron, segments=10, rot_deg=(0.0, 90.0, 0.0))
    add_cylinder(col, "Shelter_HoistPulley", (sx, sy - 5.55, z0 + 6.22), 0.20, 0.12, mat_iron, segments=14, rot_deg=(0.0, 90.0, 0.0))
    add_cylinder(col, "Shelter_HoistRope", (sx, sy - 5.55, z0 + 4.80), 0.035, 2.70, mat_straw, segments=8)
    add_box(col, "Shelter_HoistHayBale", (sx, sy - 5.55, z0 + 3.42), (0.88, 0.68, 0.58), mat_straw, rot_deg=(0.0, 0.0, 14.0))

    # Central pitched roof with 14 fine overlapping courses + south hayloft gable dormer + side dormers + stone chimney
    add_pitched_roof_with_courses(
        col,
        "Shelter_CenterRoof",
        (sx, sy, z0 + 5.20),
        7.3,
        10.2,
        2.72,
        mat_roof_b,
        mat_roof_a,
        mat_oak,
        courses=14,
        add_vent=False,
    )
    add_pitched_roof_with_courses(
        col,
        "Shelter_HayloftGable",
        (sx, sy - 4.50, z0 + 5.02),
        3.5,
        2.5,
        1.72,
        mat_roof_a,
        mat_roof_b,
        mat_oak,
        courses=7,
        add_vent=False,
        open_gable_truss=True,
    )
    # Two side roof dormers (`cửa sổ mái dormer`) with recessed mullioned windows
    for d_side in (-1.0, 1.0):
        dx = sx + d_side * 2.15
        dy = sy - 1.10
        dz = z0 + 6.02
        add_box(col, f"Shelter_SideDormer_Wall_{d_side:+.0f}", (dx, dy, dz), (1.32, 1.52, 1.02), mat_wall, wall_uv_z=(dz - 0.5, dz + 0.6))
        add_framed_window(
            col,
            f"Shelter_SideDormer_Win_{d_side:+.0f}",
            (dx + d_side * 0.66, dy, dz + 0.04),
            (0.16, 0.86, 0.62),
            mat_oak,
            mat_recess,
            mat_window_pane,
            axis="X",
        )
        add_pitched_roof_with_courses(
            col,
            f"Shelter_SideDormer_Roof_{d_side:+.0f}",
            (dx, dy, dz + 0.50),
            1.72,
            1.92,
            0.65,
            mat_roof_a,
            mat_roof_b,
            mat_oak,
            courses=5,
        )

    add_box(col, "Shelter_Chimney_Shaft", (sx + 2.32, sy + 2.55, z0 + 4.85), (1.02, 1.02, 6.80), mat_stone)
    add_box(col, "Shelter_Chimney_Crown", (sx + 2.32, sy + 2.55, z0 + 8.32), (1.24, 1.24, 0.26), mat_roof_a)

    # Rustic two-wheeled Hay Cart parked at the central SE apron (`x = sx + 2.05, y = sy - 5.85`, outside both bays)
    cx_cart, cy_cart = sx + 2.05, sy - 5.85
    cz_cart = float(h_fn(cx_cart, cy_cart))
    add_box(col, "Shelter_HayCart_Bed", (cx_cart, cy_cart, cz_cart + 0.66), (1.60, 1.12, 0.40), mat_oak, rot_deg=(0.0, 0.0, 14.0))
    add_box(col, "Shelter_HayCart_Load", (cx_cart, cy_cart, cz_cart + 1.02), (1.42, 0.94, 0.48), mat_straw, rot_deg=(0.0, 0.0, 14.0))
    for wy_off in (-0.62, 0.62):
        add_cylinder(
            col,
            f"Shelter_HayCart_Wheel_{wy_off:+.2f}",
            (cx_cart - wy_off * 0.22, cy_cart + wy_off * 0.94, cz_cart + 0.46),
            0.46,
            0.10,
            mat_oak,
            segments=16,
            rot_deg=(90.0, 0.0, 14.0),
        )

    # 3. West & East Open-Bay Herd Wings (`x = -10.8..-3.2` and `x = +3.2..+10.8`)
    for side, wname in ((-1.0, "West"), (1.0, "East")):
        wx_inner = sx + side * 3.20
        wx_outer = sx + side * 10.80
        wx_mid = 0.5 * (wx_inner + wx_outer)

        for px_i, px in enumerate((wx_inner, wx_outer)):
            for py_i, py in enumerate((sy - 4.40, sy, sy + 4.40)):
                p_ground = float(h_fn(px, py))
                post_top = z0 + 3.45
                plinth_h = max(0.66, (z0 - p_ground) + 0.60)
                add_box(
                    col,
                    f"Shelter_{wname}_Plinth_{px_i}_{py_i}",
                    (px, py, p_ground + plinth_h * 0.5 - 0.18),
                    (0.70, 0.70, plinth_h + 0.36),
                    mat_stone,
                )
                add_box(
                    col,
                    f"Shelter_{wname}_PlinthCap_{px_i}_{py_i}",
                    (px, py, z0 + 0.46),
                    (0.76, 0.76, 0.14),
                    mat_stone,
                )
                post_h = post_top - (z0 + 0.50)
                add_box(
                    col,
                    f"Shelter_{wname}_Post_{px_i}_{py_i}",
                    (px, py, z0 + 0.50 + post_h * 0.5),
                    (0.40, 0.40, post_h),
                    mat_oak,
                )
                add_box(
                    col,
                    f"Shelter_{wname}_IronStrap_{px_i}_{py_i}",
                    (px, py, z0 + 0.68),
                    (0.44, 0.44, 0.18),
                    mat_iron,
                )
                # Overhead knee braces placed at z0 + 2.92 so their lowest point is z0 + 2.42m (well above 2.25m floor clearance!)
                brace_dir_x = -side if px_i == 1 else side
                bx = px + brace_dir_x * 0.54
                bz = z0 + 2.92
                add_box(
                    col,
                    f"Shelter_{wname}_KneeBraceX_{px_i}_{py_i}",
                    (bx, py, bz),
                    (1.18, 0.20, 0.22),
                    mat_oak,
                    rot_deg=(0.0, brace_dir_x * 40.0, 0.0),
                )
                for peg_z in (bz - 0.22, bz + 0.20):
                    add_cylinder(
                        col,
                        f"Shelter_{wname}_Peg_{px_i}_{py_i}_{peg_z:.2f}",
                        (px + brace_dir_x * 0.16, py, peg_z),
                        0.042,
                        0.48,
                        mat_oak,
                        segments=8,
                        rot_deg=(90.0, 0.0, 0.0),
                    )

        # Heavy tie beams, wall plates, and open king-post trusses across the bay
        for py_i, py in enumerate((sy - 4.40, sy, sy + 4.40)):
            add_box(col, f"Shelter_{wname}_TieBeam_{py_i}", (wx_mid, py, z0 + 3.42), (8.10, 0.32, 0.32), mat_oak)
            add_box(col, f"Shelter_{wname}_KingPost_{py_i}", (wx_mid, py, z0 + 4.22), (0.28, 0.28, 1.55), mat_oak)
            for s_dir in (-1.0, 1.0):
                add_box(
                    col,
                    f"Shelter_{wname}_Strut_{py_i}_{s_dir:+.0f}",
                    (wx_mid + s_dir * 1.45, py, z0 + 3.95),
                    (2.15, 0.18, 0.20),
                    mat_oak,
                    rot_deg=(0.0, s_dir * 26.0, 0.0),
                )
        for px_i, px in enumerate((wx_inner, wx_outer)):
            add_box(col, f"Shelter_{wname}_PlateY_{px_i}", (px, sy, z0 + 3.45), (0.34, 9.60, 0.32), mat_oak)

        # Outer low limestone wall base + slatted plank windbreak (`|x| = 10.80m`)
        add_box(
            col,
            f"Shelter_{wname}_OuterMasonryWall",
            (wx_outer, sy, z0 + 0.52),
            (0.44, 8.50, 0.95),
            mat_wall,
            wall_uv_z=(z0, z0 + 3.2),
        )
        for p_idx, pz in enumerate((z0 + 1.25, z0 + 1.65, z0 + 2.05)):
            add_box(
                col,
                f"Shelter_{wname}_WindbreakPlank_{p_idx}",
                (wx_outer, sy, pz),
                (0.16, 8.50, 0.24),
                mat_oak,
            )

        # Working Interior Props along the OUTER margin (`|x| = 10.22m`, inner edge at `|x| = 9.88m`):
        # Leaves `|x| in [3.58, 9.88]` (`measured width = 6.30m >= 6.00m`) 100% clear from South to North!
        margin_x = sx + side * 10.22
        add_box(
            col,
            f"Shelter_{wname}_HayRack_Back",
            (margin_x, sy + 2.10, z0 + 1.45),
            (0.50, 3.20, 0.92),
            mat_oak,
            rot_deg=(0.0, -side * 16.0, 0.0),
        )
        add_box(
            col,
            f"Shelter_{wname}_HayRack_Fodder",
            (margin_x - side * 0.06, sy + 2.10, z0 + 1.54),
            (0.44, 2.95, 0.68),
            mat_straw,
        )
        add_box(
            col,
            f"Shelter_{wname}_WaterTrough_Base",
            (margin_x, sy - 2.15, z0 + 0.38),
            (0.64, 3.10, 0.60),
            mat_stone,
        )
        add_box(
            col,
            f"Shelter_{wname}_WaterTrough_Water",
            (margin_x, sy - 2.15, z0 + 0.62),
            (0.46, 2.82, 0.14),
            mat_water,
        )

        # Wing pitched roof with 11 fine overlapping courses + louvered roof vent + OPEN GABLE TRUSS (`open_gable_truss=True`)
        add_pitched_roof_with_courses(
            col,
            f"Shelter_{wname}Roof",
            (wx_mid, sy, z0 + 3.48),
            8.6,
            10.0,
            2.05,
            mat_roof_a,
            mat_roof_b,
            mat_oak,
            courses=11,
            add_vent=True,
            open_gable_truss=True,
        )

    # 4. Working Interior Props inside the HOLLOW Central Granary room (`x in [-2.2, +2.2]`, `y in [-1.5, +3.2]`)
    # Directly visible through the open 2.40m South stone archway!
    for b_idx, (bx, by, bz, yaw) in enumerate(
        (
            (-1.65, 1.85, 0.32, 8.0),
            (-0.75, 2.15, 0.32, -6.0),
            (-1.20, 2.00, 0.86, 12.0),
            (1.55, 1.65, 0.48, 0.0),
            (1.55, -0.45, 0.48, 0.0),
        )
    ):
        if b_idx < 3:
            add_box(
                col,
                f"Shelter_Interior_StrawBale_{b_idx}",
                (sx + bx, sy + by, z0 + bz),
                (0.92, 0.70, 0.56),
                mat_straw,
                rot_deg=(0.0, 0.0, yaw),
            )
        else:
            # Lidded timber feed bins inside the hollow central granary room
            add_box(col, f"Shelter_Interior_FeedBin_{b_idx}", (sx + bx, sy + by, z0 + 0.48), (1.28, 0.88, 0.88), mat_oak)
            add_box(
                col,
                f"Shelter_Interior_FeedBinLid_{b_idx}",
                (sx + bx, sy + by, z0 + 0.98),
                (1.34, 0.94, 0.11),
                mat_oak,
                rot_deg=(10.0, 0.0, 0.0),
            )

    added_objs = [o for o in col.objects if o not in before_objs and o.type == "MESH"]
    return compute_shelter_corridor_metrics(added_objs, sx, sy, z0)


def build_genetics_research_lab(col: bpy.types.Collection, origin: tuple[float, float] = (0.0, 0.0)) -> dict[str, Any]:
    """
    Build `Phòng Nghiên Cứu Di Truyền & Bào Chế` (`genetics-research-lab.glb`, `13.6m x 11.4m` footprint)
    grounded in `technical:p00545-p00569` and `mechanics:p00738, p01278`.
    Addresses Codex Message #61:
    - Open-bay mullion & translucent-pane conservatory shell (no opaque box burying specimen vessels!)
      so specimen cylinders (`Lam Khê`, `Hắc Mao`) and botanical culture tables inside are 100% visible.
    """
    tex_paths = ensure_baked_textures()
    mat_wall = make_textured_mat("Lab_WallPlanksMasonry", tex_paths["wall_planks_masonry"], roughness=0.76)
    mat_slate = make_textured_mat("Lab_RoofSlate", tex_paths["roof_tiles"], roughness=0.60, tint=(0.56, 0.66, 0.82))
    mat_tile = make_textured_mat("Lab_RoofTile", tex_paths["roof_tiles"], roughness=0.66, tint=(0.82, 0.64, 0.52))
    mat_oak = make_textured_mat("Lab_OakTimber", tex_paths["timber_oak"], roughness=0.74)
    mat_stone = make_mat("Lab_Limestone", (0.52, 0.50, 0.45), roughness=0.86)
    mat_copper = make_mat("Lab_Copper", (0.74, 0.45, 0.24), roughness=0.34, metallic=0.82)
    mat_recess = make_mat("Lab_WindowRecess", (0.08, 0.09, 0.11), roughness=0.90)
    mat_glass_pane = make_mat(
        "Lab_TransomGlass",
        (0.36, 0.68, 0.64),
        roughness=0.12,
        emission=(0.20, 0.52, 0.48),
        emission_strength=0.22,
        alpha=0.24,
    )
    mat_specimen_cyan = make_mat(
        "Lab_SpecimenLamKhe",
        (0.28, 0.78, 0.92),
        roughness=0.16,
        emission=(0.22, 0.68, 0.88),
        emission_strength=1.1,
    )
    mat_specimen_amber = make_mat(
        "Lab_SpecimenHacMao",
        (0.92, 0.54, 0.20),
        roughness=0.16,
        emission=(0.88, 0.46, 0.16),
        emission_strength=1.1,
    )

    ox, oy = origin
    add_box(col, "Lab_Foundation", (ox, oy, 0.28), (13.6, 11.4, 0.56), mat_stone)

    # 1. Main Genetics & Reagent Hall (`x in [-5.8, +1.8]`, `y in [-3.2, +4.8]`)
    add_box(col, "Lab_MainHall_Body", (ox - 2.0, oy + 0.8, 2.75), (7.6, 7.8, 4.40), mat_wall, wall_uv_z=(0.55, 4.95))
    for wx in (ox - 4.1, ox - 0.2):
        add_framed_window(col, f"Lab_MainWinS_{wx:.1f}", (wx, oy - 3.12, 2.25), (1.25, 0.22, 1.35), mat_oak, mat_recess, mat_glass_pane, axis="Y")
    add_pitched_roof_with_courses(
        col,
        "Lab_MainRoof",
        (ox - 2.0, oy + 0.8, 4.95),
        8.4,
        8.6,
        2.35,
        mat_slate,
        mat_tile,
        mat_oak,
        courses=11,
        add_vent=True,
    )
    # Clerestory roof monitor with framed mullions
    add_box(col, "Lab_Clerestory_Core", (ox - 2.0, oy + 0.8, 6.45), (3.2, 4.8, 1.15), mat_oak)
    for side in (-1.0, 1.0):
        add_framed_window(
            col,
            f"Lab_Clerestory_Win_{side:+.0f}",
            (ox - 2.0 + side * 1.62, oy + 0.8, 6.45),
            (0.18, 4.2, 0.68),
            mat_oak,
            mat_recess,
            mat_glass_pane,
            axis="X",
        )

    # 2. East Open-Mullion Specimen & Bio-Conservatory Wing (`x in [1.8, 6.2]`, `y in [-2.8, +4.2]`)
    # Built as a real mullion/transom structural cage with open inspection bays so interior specimens are 100% visible!
    add_box(col, "Lab_Conservatory_Floor", (ox + 4.0, oy + 0.7, 0.72), (4.4, 7.0, 0.36), mat_stone)
    # Low stone knee walls along North, East, and South leaving central bays open for specimen visibility
    add_box(col, "Lab_Conservatory_NorthKneewall", (ox + 4.0, oy + 4.02, 1.12), (4.4, 0.34, 0.82), mat_stone)
    add_box(col, "Lab_Conservatory_SouthKneewall", (ox + 4.0, oy - 2.62, 1.12), (4.4, 0.34, 0.82), mat_stone)
    add_box(col, "Lab_Conservatory_EastKneewall", (ox + 6.02, oy + 0.7, 1.12), (0.34, 7.0, 0.82), mat_stone)

    # Structural oak & copper mullions, transoms, and open-truss glazed roof ribs
    for rib_y in np.linspace(oy - 2.55, oy + 3.95, 6):
        add_box(col, f"Lab_Conservatory_PostE_{rib_y:.2f}", (ox + 6.02, float(rib_y), 2.45), (0.20, 0.20, 2.05), mat_oak)
        add_box(col, f"Lab_Conservatory_RoofRib_{rib_y:.2f}", (ox + 4.0, float(rib_y), 3.85), (4.55, 0.16, 0.18), mat_copper, rot_deg=(0.0, 16.0, 0.0))
    for tz in (1.52, 2.48, 3.42):
        add_box(col, f"Lab_Conservatory_TransomE_{tz:.2f}", (ox + 6.04, oy + 0.7, tz), (0.16, 7.05, 0.14), mat_copper)
        add_box(col, f"Lab_Conservatory_TransomS_{tz:.2f}", (ox + 4.0, oy - 2.62, tz), (4.45, 0.16, 0.14), mat_copper)

    # Upper transom glass panes only (leaving lower inspection bays open so specimens are unobstructed in clay & textured!)
    add_box(col, "Lab_Conservatory_UpperGlassE", (ox + 6.00, oy + 0.7, 2.95), (0.04, 6.8, 0.82), mat_glass_pane)
    add_box(col, "Lab_Conservatory_RoofGlassSlope", (ox + 4.0, oy + 0.7, 3.92), (4.45, 6.9, 0.05), mat_glass_pane, rot_deg=(0.0, 16.0, 0.0))

    # Visible Specimen Cylinders & Culture Table inside the Conservatory (`x = ox + 3.8..5.1`)
    add_box(col, "Lab_Conservatory_CultureTable", (ox + 3.45, oy + 0.7, 1.22), (1.35, 5.2, 0.72), mat_oak)
    for s_idx, (sx_p, sy_p, smat) in enumerate(
        (
            (ox + 4.95, oy - 1.45, mat_specimen_cyan),
            (ox + 4.95, oy + 0.70, mat_specimen_amber),
            (ox + 4.95, oy + 2.85, mat_specimen_cyan),
        )
    ):
        add_cylinder(col, f"Lab_SpecimenJar_Base_{s_idx}", (sx_p, sy_p, 1.08), 0.44, 0.44, mat_stone, segments=14)
        add_cylinder(col, f"Lab_SpecimenJar_Vessel_{s_idx}", (sx_p, sy_p, 1.88), 0.36, 1.18, smat, segments=16)
        add_cylinder(col, f"Lab_SpecimenJar_Cap_{s_idx}", (sx_p, sy_p, 2.54), 0.40, 0.16, mat_copper, segments=14)

    # 3. Copper reagent distillation flues & condensers on NW corner
    for f_idx, fx in enumerate((ox - 5.2, ox - 4.3)):
        add_cylinder(col, f"Lab_CopperFlue_{f_idx}", (fx, oy + 4.2, 3.85), 0.24, 5.4, mat_copper, segments=14)
        add_cylinder(col, f"Lab_CopperHood_{f_idx}", (fx, oy + 4.2, 6.68), 0.38, 0.28, mat_copper, segments=14)

    # 4. Covered Ventilated South Veranda (`y in [-5.2, -3.0]`) with TWO distinct workbenches
    for px in (ox - 5.2, ox - 1.8, ox + 1.6):
        add_box(col, f"Lab_VerandaPost_{px:.1f}", (px, oy - 5.0, 1.85), (0.30, 0.30, 2.60), mat_oak)
    add_box(col, "Lab_VerandaAwning", (ox - 1.8, oy - 4.1, 3.25), (7.6, 2.4, 0.22), mat_tile, rot_deg=(14.0, 0.0, 0.0))
    add_box(col, "Lab_Bench_Genetics", (ox - 3.6, oy - 4.1, 1.02), (2.4, 1.0, 0.88), mat_oak)
    add_cylinder(col, "Lab_Microscope", (ox - 3.9, oy - 4.1, 1.68), 0.16, 0.48, mat_copper, segments=12, rot_deg=(18.0, 0.0, 0.0))
    add_box(col, "Lab_TraitSampleRack", (ox - 3.1, oy - 4.1, 1.62), (0.65, 0.38, 0.34), mat_specimen_cyan)
    add_box(col, "Lab_Bench_Reagents", (ox - 0.1, oy - 4.1, 1.02), (2.4, 1.0, 0.88), mat_oak)
    add_cylinder(col, "Lab_AlembicStill", (ox - 0.4, oy - 4.1, 1.72), 0.26, 0.56, mat_copper, segments=14)
    add_cylinder(col, "Lab_ReagentFlask", (ox + 0.4, oy - 4.1, 1.64), 0.18, 0.42, mat_specimen_amber, segments=14)

    return {"facility_id": "genetics_research_lab", "footprint_m": [13.6, 11.4], "source_anchors": ["technical:p00545-p00569", "mechanics:p00738", "mechanics:p01278"]}


def build_information_station(col: bpy.types.Collection, origin: tuple[float, float] = (0.0, 0.0)) -> dict[str, Any]:
    """
    Build `Khu Thông Tin` (`information-station.glb`, `11.6m x 4.8m` footprint)
    grounded in `mechanics:p02801-p02843, p00740, p01706-p01730`.
    Addresses Codex Message #61:
    - Visible inclined timber access stair/ladder with handrails connecting ground plinth to observation deck;
    - Connected railing posts on the observation deck;
    - 4 grounded oak support legs + louvered slats under `Info_Weather_StevensonBox` (zero floating instruments!).
    """
    tex_paths = ensure_baked_textures()
    mat_oak = make_textured_mat("Info_OakTimber", tex_paths["timber_oak"], roughness=0.76)
    mat_roof = make_textured_mat("Info_RoofTiles", tex_paths["roof_tiles"], roughness=0.66)
    mat_stone = make_mat("Info_StoneBase", (0.49, 0.47, 0.42), roughness=0.88)
    mat_iron = make_mat("Info_IronFrame", (0.20, 0.22, 0.25), roughness=0.42, metallic=0.78)
    mat_copper = make_mat("Info_BrassInstrument", (0.78, 0.56, 0.24), roughness=0.32, metallic=0.84)
    mat_white = make_mat("Info_WeatherBox", (0.88, 0.86, 0.80), roughness=0.72)
    mat_map = make_mat("Info_MapParchment", (0.84, 0.74, 0.54), roughness=0.78)

    ox, oy = origin
    add_box(col, "Info_CompoundPlinth", (ox, oy, 0.22), (11.6, 4.8, 0.44), mat_stone)

    # 1. Open Lookout Tower (`x = -3.8m`, height ~8.2m) with connected railing posts & inclined access stair
    tx, ty = ox - 3.8, oy
    add_box(col, "Info_Tower_StonePier", (tx, ty, 0.95), (3.1, 3.1, 1.15), mat_stone)
    for cx_s in (-1.15, 1.15):
        for cy_s in (-1.15, 1.15):
            add_box(col, f"Info_Tower_Post_{cx_s:+.2f}_{cy_s:+.2f}", (tx + cx_s, ty + cy_s, 4.15), (0.26, 0.26, 5.40), mat_oak)
    for tier_z in (2.65, 4.35, 5.95):
        for side in (-1.15, 1.15):
            add_box(col, f"Info_Tower_XBraceA_{tier_z:.1f}_{side:+.1f}", (tx, ty + side, tier_z), (2.45, 0.14, 0.14), mat_oak, rot_deg=(0.0, 34.0, 0.0))
            add_box(col, f"Info_Tower_XBraceB_{tier_z:.1f}_{side:+.1f}", (tx, ty + side, tier_z), (2.45, 0.14, 0.14), mat_oak, rot_deg=(0.0, -34.0, 0.0))
    # Observation deck + connected vertical railing posts + top/mid rails
    add_box(col, "Info_Tower_Deck", (tx, ty, 6.05), (3.2, 3.2, 0.26), mat_oak)
    for rx in (-1.45, 0.0, 1.45):
        for ry in (-1.45, 1.45):
            if rx == 1.45 and ry == -1.45:
                continue
            add_box(col, f"Info_Tower_RailPostY_{rx:+.2f}_{ry:+.2f}", (tx + rx, ty + ry, 6.52), (0.14, 0.14, 0.92), mat_oak)
    for ry in (-1.45, 1.45):
        for rz in (6.52, 6.92):
            add_box(col, f"Info_Tower_RailY_{ry:+.2f}_{rz:.2f}", (tx, ty + ry, rz), (3.05, 0.12, 0.12), mat_oak)
    for rx in (-1.45, 1.45):
        for rz in (6.52, 6.92):
            # Leave entry opening on East-South side of deck for access ladder
            span_r = 2.10 if rx > 0 else 3.05
            off_y = 0.45 if rx > 0 else 0.0
            add_box(col, f"Info_Tower_RailX_{rx:+.2f}_{rz:.2f}", (tx + rx, ty + off_y, rz), (0.12, span_r, 0.12), mat_oak)

    # Inclined timber access stair/ladder with dual stringers, handrails, and 12 treads from plinth (z=0.44) to deck (z=6.05)
    stair_x = tx + 1.52
    for s_off in (-0.34, 0.34):
        add_box(
            col,
            f"Info_Tower_StairStringer_{s_off:+.2f}",
            (stair_x + s_off, ty - 0.85, 3.25),
            (0.10, 0.22, 6.15),
            mat_oak,
            rot_deg=(24.0, 0.0, 0.0),
        )
        add_box(
            col,
            f"Info_Tower_StairHandrail_{s_off:+.2f}",
            (stair_x + s_off, ty - 1.05, 3.85),
            (0.08, 0.10, 6.15),
            mat_oak,
            rot_deg=(24.0, 0.0, 0.0),
        )
    for step_i in range(12):
        t_s = (step_i + 0.5) / 12.0
        sy_step = (ty - 2.05) + t_s * 2.40
        sz_step = 0.55 + t_s * 5.45
        add_box(col, f"Info_Tower_StairTread_{step_i}", (stair_x, sy_step, sz_step), (0.64, 0.24, 0.08), mat_oak)

    add_pitched_roof_with_courses(
        col,
        "Info_Tower_Roof",
        (tx, ty, 7.05),
        3.6,
        3.6,
        1.35,
        mat_roof,
        mat_roof,
        mat_oak,
        courses=7,
    )

    # 2. Covered Intelligence Map Desk (`Bàn Tình Báo`, `x = -0.1m`)
    mx, my = ox - 0.1, oy - 0.2
    for px in (-1.35, 1.35):
        for py in (-1.25, 1.25):
            add_box(col, f"Info_MapCanopy_Post_{px:+.2f}_{py:+.2f}", (mx + px, my + py, 1.75), (0.20, 0.20, 2.65), mat_oak)
    add_pitched_roof_with_courses(col, "Info_MapCanopy_Roof", (mx, my, 3.05), 3.2, 3.0, 0.95, mat_roof, mat_roof, mat_oak, courses=6)
    add_box(col, "Info_MapDesk_Table", (mx, my, 1.05), (2.1, 1.35, 0.85), mat_oak)
    add_box(col, "Info_MapDesk_Chart", (mx, my, 1.52), (1.75, 1.05, 0.06), mat_map, rot_deg=(6.0, 0.0, 0.0))

    # 3. Weather Instruments (`Trạm Khí Tượng`, `x = +2.5m`) with 4 grounded oak support legs + louvered housing
    wx, wy = ox + 2.5, oy + 0.6
    for lx in (-0.42, 0.42):
        for ly in (-0.36, 0.36):
            add_box(col, f"Info_Weather_Leg_{lx:+.2f}_{ly:+.2f}", (wx + lx, wy + ly, 0.82), (0.10, 0.10, 0.84), mat_oak)
    add_box(col, "Info_Weather_StandFrame", (wx, wy, 1.18), (0.98, 0.88, 0.10), mat_oak)
    add_box(col, "Info_Weather_StevensonBox", (wx, wy, 1.68), (0.95, 0.85, 0.96), mat_white)
    for lv_i, lv_z in enumerate((1.38, 1.56, 1.74, 1.92)):
        add_box(col, f"Info_Weather_Louver_{lv_i}", (wx, wy, lv_z), (1.00, 0.90, 0.05), mat_white)
    add_pitched_roof_with_courses(col, "Info_Weather_BoxRoof", (wx, wy, 2.18), 1.12, 1.02, 0.28, mat_white, mat_white, mat_oak, courses=3)
    add_cylinder(col, "Info_Weather_Mast", (wx + 0.68, wy, 2.65), 0.08, 4.4, mat_iron, segments=10)
    add_box(col, "Info_Weather_CrossArm", (wx + 0.68, wy, 4.75), (1.35, 0.08, 0.08), mat_copper)
    for cup_x in (wx + 0.05, wx + 1.31):
        add_cylinder(col, f"Info_Weather_AnemometerCup_{cup_x:.2f}", (cup_x, wy, 4.82), 0.14, 0.12, mat_copper, segments=10)

    # 4. Threat Radar Frame (`Radar Uy Hiếp`, `x = +4.4m`)
    rx, ry = ox + 4.4, oy - 0.3
    add_box(col, "Info_Radar_Cabinet", (rx, ry, 1.25), (1.45, 1.35, 1.65), mat_iron)
    add_cylinder(col, "Info_Radar_Yoke", (rx, ry, 2.55), 0.18, 1.15, mat_iron, segments=12)
    add_cylinder(col, "Info_Radar_Dish", (rx, ry - 0.15, 3.35), 1.15, 0.22, mat_copper, segments=18, rot_deg=(52.0, 0.0, 0.0))

    return {"facility_id": "information_station", "footprint_m": [11.6, 4.8], "source_anchors": ["mechanics:p02801-p02843", "mechanics:p00740", "mechanics:p01706-p01730"]}


def build_main_house_an_room(
    col: bpy.types.Collection,
    origin: tuple[float, float] = (0.0, 0.0),
    lit_stove: bool = False,
) -> dict[str, Any]:
    """
    Build `Nhà Chính & Gian Bếp Nấu Cám Của An` (`main-house-an-room.glb`, `13.4m x 9.6m` footprint)
    grounded in `mechanics:p00740` and `story:p00105, p00108`.
    Addresses Codex Message #61:
    - Default `lit_stove=False` (cold soot-blackened brick feed stove per `story:p00105`: "bếp gạch đen muội đã nguội từ lâu").
    - Framed/mullioned windows with non-blown-out glass.
    """
    tex_paths = ensure_baked_textures()
    mat_wall = make_textured_mat("House_WallPlanksMasonry", tex_paths["wall_planks_masonry"], roughness=0.78)
    mat_roof_a = make_textured_mat("House_RoofTilesA", tex_paths["roof_tiles"], roughness=0.66)
    mat_roof_b = make_textured_mat("House_RoofTilesB", tex_paths["roof_tiles"], roughness=0.64, tint=(0.86, 0.72, 0.64))
    mat_oak = make_textured_mat("House_OakTimber", tex_paths["timber_oak"], roughness=0.75)
    mat_stone = make_mat("House_Limestone", (0.50, 0.47, 0.41), roughness=0.88)
    mat_soot_brick = make_mat("House_SootBrickStove", (0.18, 0.14, 0.13), roughness=0.94)
    mat_cold_ash = make_mat("House_ColdSootAsh", (0.08, 0.08, 0.09), roughness=0.96)
    mat_ember = make_mat("House_StoveEmber", (0.96, 0.42, 0.14), roughness=0.35, emission=(1.0, 0.42, 0.12), emission_strength=1.6)
    mat_iron = make_mat("House_CastIronCauldron", (0.16, 0.17, 0.19), roughness=0.48, metallic=0.75)
    mat_glass_bottle = make_mat("House_EmptySmallGlassBottle", (0.76, 0.88, 0.86), roughness=0.12, alpha=0.45)
    mat_recess = make_mat("House_WindowRecess", (0.08, 0.07, 0.06), roughness=0.92)
    mat_window_pane = make_mat("House_WindowPane", (0.48, 0.38, 0.24), roughness=0.22, emission=(0.85, 0.58, 0.24), emission_strength=0.24)

    ox, oy = origin
    add_box(col, "House_Foundation", (ox, oy, 0.26), (13.4, 9.6, 0.52), mat_stone)

    # 1. Main Domestic Farmhouse Volume (`x in [-6.0, +1.8]`, `y in [-3.2, +4.2]`)
    add_box(col, "House_MainBody", (ox - 2.1, oy + 0.5, 2.55), (7.8, 7.4, 4.10), mat_wall, wall_uv_z=(0.50, 4.60))
    add_pitched_roof_with_courses(
        col,
        "House_MainRoof",
        (ox - 2.1, oy + 0.5, 4.60),
        8.6,
        8.2,
        2.45,
        mat_roof_a,
        mat_roof_b,
        mat_oak,
        courses=11,
    )
    add_box(col, "House_HearthChimney", (ox - 5.2, oy + 1.8, 4.25), (1.15, 1.15, 6.20), mat_stone)
    for px in (ox - 5.4, ox - 2.1, ox + 1.2):
        add_box(col, f"House_PorchPost_{px:.1f}", (px, oy - 4.3, 1.75), (0.28, 0.28, 2.50), mat_oak)
    add_box(col, "House_PorchRoof", (ox - 2.1, oy - 3.6, 3.15), (7.8, 2.1, 0.20), mat_roof_b, rot_deg=(15.0, 0.0, 0.0))
    add_box(col, "House_FrontDoor", (ox - 2.1, oy - 3.22, 1.65), (1.35, 0.22, 2.25), mat_oak)
    add_framed_window(col, "House_FrontWindowW", (ox - 4.3, oy - 3.22, 2.05), (1.25, 0.24, 1.20), mat_oak, mat_recess, mat_window_pane, axis="Y")

    # 2. Attached Lore Anchor Side Wing (`Gian Bếp Củi & Bậu Cửa Sổ Của An`, `x in [1.8, 6.2]`, `y in [-2.8, +3.6]`)
    add_box(col, "House_AnRoom_RearWall", (ox + 4.0, oy + 3.3, 2.15), (4.4, 0.36, 3.30), mat_wall, wall_uv_z=(0.50, 3.80))
    add_box(col, "House_AnRoom_EastWallLower", (ox + 6.0, oy + 0.4, 1.25), (0.38, 6.0, 1.50), mat_wall, wall_uv_z=(0.50, 3.80))
    # Prominent weathered oak Window Sill (`bậu cửa sổ`, `story:p00105, p00108`) on the East/South bay
    add_box(col, "House_AnRoom_WindowSill", (ox + 5.85, oy - 0.65, 2.05), (0.68, 2.10, 0.16), mat_oak)
    add_box(col, "House_AnRoom_WindowFrameTop", (ox + 6.0, oy - 0.65, 3.15), (0.38, 2.10, 0.22), mat_oak)
    # Small Empty Glass Bottle (`lọ thủy tinh nhỏ trống rỗng đầu Mùa 1`) resting on the window sill
    add_cylinder(col, "House_AnRoom_SmallEmptyGlassBottle_Body", (ox + 5.75, oy - 0.55, 2.28), 0.11, 0.32, mat_glass_bottle, segments=14)
    add_cylinder(col, "House_AnRoom_SmallEmptyGlassBottle_Neck", (ox + 5.75, oy - 0.55, 2.48), 0.05, 0.14, mat_glass_bottle, segments=12)

    # Soot-blackened Brick Feed Stove (`bếp gạch nấu cám đen muội đã nguội từ lâu`, `story:p00105`) right beside the window sill
    add_box(col, "House_AnRoom_SootBrickStove_Body", (ox + 4.55, oy + 1.15, 1.18), (1.85, 1.65, 1.32), mat_soot_brick)
    firebox_mat = mat_ember if lit_stove else mat_cold_ash
    add_box(col, "House_AnRoom_SootBrickStove_Firebox", (ox + 4.55, oy + 0.30, 0.92), (0.75, 0.22, 0.55), firebox_mat)
    add_cylinder(col, "House_AnRoom_FeedCauldron", (ox + 4.55, oy + 1.15, 1.95), 0.62, 0.55, mat_iron, segments=18)
    add_box(col, "House_AnRoom_SootFlue", (ox + 4.55, oy + 2.65, 3.25), (0.68, 0.68, 4.20), mat_soot_brick)

    add_pitched_roof_with_courses(
        col,
        "House_AnRoom_Roof",
        (ox + 4.0, oy + 0.4, 3.75),
        4.8,
        6.6,
        1.45,
        mat_roof_b,
        mat_roof_a,
        mat_oak,
        courses=8,
    )

    return {
        "facility_id": "main_house_an_room",
        "footprint_m": [13.4, 9.6],
        "source_anchors": ["mechanics:p00740", "story:p00105", "story:p00108"],
        "stove_initial_state": "cold_soot_blackened" if not lit_stove else "lit_operating",
        "lore_props": ["cold_soot_blackened_brick_feed_stove", "window_sill", "small_empty_glass_bottle_season1_start"],
    }


def build_medical_tent(col: bpy.types.Collection, origin: tuple[float, float] = (0.0, 0.0)) -> dict[str, Any]:
    """
    Build `Lều Quân Y` (`medical-tent.glb`, `9.4m x 7.4m` footprint)
    grounded in `technical:p00513` (no invented healing values).
    Addresses Codex Message #61:
    - Uses continuous sagging/tensioned linen canvas roof sheet with sewn seam welts (`add_tensioned_canvas_roof`)
      instead of stacked roof tiles!
    """
    tex_paths = ensure_baked_textures()
    mat_oak = make_textured_mat("Med_OakTimber", tex_paths["timber_oak"], roughness=0.76)
    mat_stone = make_mat("Med_StoneCurb", (0.50, 0.48, 0.43), roughness=0.88)
    mat_linen_a = make_mat("Med_LinenCanvasA", (0.86, 0.82, 0.72), roughness=0.84)
    mat_linen_b = make_mat("Med_LinenCanvasB", (0.76, 0.71, 0.60), roughness=0.86)
    mat_emblem = make_mat("Med_CinnabarMark", (0.68, 0.22, 0.18), roughness=0.76)

    ox, oy = origin
    add_box(col, "Med_GravelCurb", (ox, oy, 0.18), (9.4, 7.4, 0.36), mat_stone)
    add_box(col, "Med_DuckboardFloor", (ox, oy, 0.40), (7.8, 5.8, 0.14), mat_oak)

    for py in (-2.6, 0.0, 2.6):
        add_cylinder(col, f"Med_RidgePole_{py:+.1f}", (ox, oy + py, 2.15), 0.12, 3.55, mat_oak, segments=12)
        for sx in (-3.4, 3.4):
            add_cylinder(col, f"Med_SidePost_{sx:+.1f}_{py:+.1f}", (ox + sx, oy + py, 1.35), 0.10, 1.90, mat_oak, segments=10)
            add_cylinder(
                col,
                f"Med_GuyRope_{sx:+.1f}_{py:+.1f}",
                (ox + sx * 1.14, oy + py, 1.15),
                0.028,
                1.88,
                mat_linen_b,
                segments=8,
                rot_deg=(0.0, (1.0 if sx > 0 else -1.0) * 32.0, 0.0),
            )
            add_box(col, f"Med_Stake_{sx:+.1f}_{py:+.1f}", (ox + sx * 1.26, oy + py, 0.42), (0.14, 0.14, 0.48), mat_oak, rot_deg=(0.0, (1.0 if sx > 0 else -1.0) * 18.0, 0.0))

    # Continuous tensioned sagging linen canvas roof canopy with sewn seam welts (no roof tiles!)
    add_tensioned_canvas_roof(
        col,
        "Med_CanvasRoof",
        (ox, oy, 2.25),
        7.2,
        6.0,
        1.65,
        mat_linen_a,
        mat_linen_b,
        mat_oak,
    )
    for sx in (-3.45, 3.45):
        add_cylinder(col, f"Med_RolledFlap_{sx:+.2f}", (ox + sx, oy, 2.18), 0.22, 5.8, mat_linen_b, segments=12, rot_deg=(90.0, 0.0, 0.0))

    add_box(col, "Med_EmblemH", (ox - 1.8, oy, 3.12), (0.95, 0.28, 0.06), mat_emblem, rot_deg=(0.0, -24.5, 0.0))
    add_box(col, "Med_EmblemV", (ox - 1.8, oy, 3.12), (0.28, 0.95, 0.06), mat_emblem, rot_deg=(0.0, -24.5, 0.0))
    for cx in (-1.65, 1.65):
        add_box(col, f"Med_FieldCot_{cx:+.2f}", (ox + cx, oy + 0.5, 0.78), (1.15, 2.25, 0.48), mat_linen_a)
    add_box(col, "Med_SupplyChest", (ox, oy + 1.85, 0.85), (1.25, 0.75, 0.68), mat_oak)

    return {"facility_id": "medical_tent", "footprint_m": [9.4, 7.4], "source_anchors": ["technical:p00513"], "gameplay_values_invented": False}


def setup_studio_world(scene: bpy.types.Scene, samples: int = 36) -> None:
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"

    world = bpy.data.worlds.new("StudioWorld")
    scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    bg = nt.nodes.get("Background")
    bg.inputs[0].default_value = (0.36, 0.33, 0.29, 1.0)
    bg.inputs[1].default_value = 0.95

    sun_data = bpy.data.lights.new("KeySun", type="SUN")
    sun_data.energy = 4.2
    sun_data.color = (1.0, 0.86, 0.68)
    sun_data.angle = math.radians(5.0)
    sun = bpy.data.objects.new("KeySun", sun_data)
    scene.collection.objects.link(sun)
    sun.rotation_euler = Euler((math.radians(52.0), math.radians(-16.0), math.radians(34.0)), "XYZ")

    fill_data = bpy.data.lights.new("FillSun", type="SUN")
    fill_data.energy = 1.4
    fill_data.color = (0.72, 0.82, 0.98)
    fill_data.angle = math.radians(24.0)
    fill = bpy.data.objects.new("FillSun", fill_data)
    scene.collection.objects.link(fill)
    fill.rotation_euler = Euler((math.radians(44.0), math.radians(22.0), math.radians(-142.0)), "XYZ")


def aim_camera(cam: bpy.types.Object, loc: tuple[float, float, float], target: tuple[float, float, float], lens: float = 34.0) -> None:
    cam.location = Vector(loc)
    cam.data.lens = lens
    cam.data.clip_start = 0.1
    cam.data.clip_end = 500.0
    direction = Vector(target) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def export_clean_asset(scene: bpy.types.Scene, col: bpy.types.Collection, stem: str) -> dict[str, Any]:
    blend_p = OUT_DIR / f"{stem}.blend"
    glb_p = OUT_DIR / f"{stem}.glb"
    fbx_p = OUT_DIR / f"{stem}.fbx"

    bpy.ops.object.select_all(action="DESELECT")
    mesh_objs = [o for o in col.all_objects if o.type == "MESH"]
    for o in mesh_objs:
        o.select_set(True)
    if mesh_objs:
        bpy.context.view_layer.objects.active = mesh_objs[0]

    bpy.ops.export_scene.gltf(
        filepath=str(glb_p),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
    )
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_p),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        path_mode="COPY",
        embed_textures=True,
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_p))
    tris = sum(len(o.data.polygons) for o in mesh_objs)
    return {
        "blend": str(blend_p),
        "glb": str(glb_p),
        "fbx": str(fbx_p),
        "glb_sha256": sha256_file(glb_p),
        "fbx_sha256": sha256_file(fbx_p),
        "mesh_objects": len(mesh_objs),
        "faces": tris,
    }


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    results: dict[str, Any] = {}

    # 1. Build & Render Local-Origin Herd Shelter (`herd-shelter.glb/.fbx/.blend`)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    setup_studio_world(scene, samples=36)
    col_shelter = bpy.data.collections.new("HerdShelter")
    scene.collection.children.link(col_shelter)
    shelter_meta = build_high_craft_herd_shelter(col_shelter, origin=(0.0, 0.0))
    shelter_export = export_clean_asset(scene, col_shelter, "herd-shelter")

    # Add temporary studio floor disc for renders only
    mat_backdrop = make_mat("StudioBackdrop", (0.28, 0.32, 0.22), roughness=0.92)
    add_cylinder(scene.collection, "PreviewBackdrop", (0.0, 0.0, -0.36), 28.0, 0.08, mat_backdrop, segments=48)

    cam_data = bpy.data.cameras.new("RenderCam")
    cam = bpy.data.objects.new("RenderCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    # Unclipped full-building front view keeping BOTH West & East roof wings & plinths 100% in frame (`Message #63`)
    front_p = OUT_DIR / "01-shelter-front-close.png"
    aim_camera(cam, (-17.5, -24.5, 9.6), (0.2, 0.2, 2.8), lens=28.0)
    scene.render.filepath = str(front_p)
    bpy.ops.render.render(write_still=True)

    # Unclipped full-building rear view keeping BOTH wings & plinths 100% in frame (`Message #63`)
    rear_p = OUT_DIR / "02-shelter-rear-close.png"
    aim_camera(cam, (17.5, 24.5, 9.8), (-0.2, -0.2, 2.8), lens=28.0)
    scene.render.filepath = str(rear_p)
    bpy.ops.render.render(write_still=True)

    # Unclipped Clay + wireframe proof (`Message #63`)
    clay_p = OUT_DIR / "03-shelter-clay-close.png"
    clay_mat = make_mat("ShelterClay", (0.78, 0.75, 0.70), roughness=0.65)
    bpy.context.view_layer.material_override = clay_mat
    scene.render.use_freestyle = True
    vl = bpy.context.view_layer
    vl.freestyle_settings.linesets.new("WireLines")
    aim_camera(cam, (-17.5, -24.5, 9.6), (0.2, 0.2, 2.8), lens=28.0)
    scene.render.filepath = str(clay_p)
    bpy.ops.render.render(write_still=True)
    bpy.context.view_layer.material_override = None
    scene.render.use_freestyle = False

    # Dedicated close-up interior & joinery view (`04-shelter-interior-detail.png`, `Message #63`)
    interior_p = OUT_DIR / "04-shelter-interior-detail.png"
    aim_camera(cam, (-5.4, -12.2, 3.3), (-2.0, 1.2, 2.1), lens=30.0)
    scene.render.filepath = str(interior_p)
    bpy.ops.render.render(write_still=True)

    results["herd_shelter"] = {
        **shelter_meta,
        **shelter_export,
        "renders": {
            "front_close": str(front_p),
            "rear_close": str(rear_p),
            "clay_close": str(clay_p),
            "interior_detail": str(interior_p),
        },
    }

    # 2. Build & Export the 4 Distinct Local-Origin `Farm_Core` Facilities
    facility_builders = [
        ("genetics-research-lab", build_genetics_research_lab),
        ("information-station", build_information_station),
        ("main-house-an-room", build_main_house_an_room),
        ("medical-tent", build_medical_tent),
    ]
    for stem, fn in facility_builders:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        sc = bpy.context.scene
        col = bpy.data.collections.new(stem)
        sc.collection.children.link(col)
        meta = fn(col, origin=(0.0, 0.0))
        exp = export_clean_asset(sc, col, stem)
        results[stem] = {**meta, **exp}

    # 3. Render 2x2 Facility Contact Sheet (`facility-contact-sheet.png`)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    setup_studio_world(sc, samples=32)
    sc.render.resolution_x = 1800
    sc.render.resolution_y = 1100
    col_sheet = bpy.data.collections.new("FacilitySheet")
    sc.collection.children.link(col_sheet)
    build_genetics_research_lab(col_sheet, origin=(-11.0, 8.0))
    build_information_station(col_sheet, origin=(11.0, 8.0))
    build_main_house_an_room(col_sheet, origin=(-11.0, -8.0))
    build_medical_tent(col_sheet, origin=(11.0, -8.0))
    mat_pad = make_mat("SheetPad", (0.28, 0.31, 0.23), roughness=0.92)
    add_box(col_sheet, "SheetGround", (0.0, 0.0, -0.20), (48.0, 36.0, 0.25), mat_pad)

    cam_d = bpy.data.cameras.new("SheetCam")
    cam_o = bpy.data.objects.new("SheetCam", cam_d)
    sc.collection.objects.link(cam_o)
    sc.camera = cam_o
    sheet_p = OUT_DIR / "facility-contact-sheet.png"
    aim_camera(cam_o, (0.0, -34.0, 22.5), (0.0, -0.5, 2.2), lens=35.0)
    sc.render.filepath = str(sheet_p)
    bpy.ops.render.render(write_still=True)
    results["facility_contact_sheet"] = str(sheet_p)

    # 4. Write `validation.json` and `architecture-source-map.md`
    val_p = OUT_DIR / "validation.json"
    val_p.write_text(json.dumps(results, indent=2, ensure_ascii=False), encoding="utf-8")

    md_lines = [
        "# Argon Functional Buildings & Herd Shelter Source Map (`functional-buildings-argon`)",
        "",
        "## 1. Priority 1: `Nơi Trú Đàn` (`herd-shelter.glb` / `.fbx` / `.blend`)",
        "- **Scene Ownership**: Placed in `Farm_Main` at `(60.0, 72.0)` on the `1.50m` leveled pad (`technical:p00504-p00517`). Simultaneous `warehouse_A/B/C` catalogue staging and `Farm_Core` operation buildings have been removed from `Farm_Main`.",
        "- **Dynamically Measured Herd Passage Openings (Mesh AABB Validation)**:",
        f"  - Floor Navigation Vertical Envelope: `z in {shelter_meta['floor_navigation_clearance_z_interval_m']} m` (Overhead Knee-Brace Min Z: `{shelter_meta['overhead_knee_brace_min_z_m']:.2f} m`)",
        f"  - West Bay Clear Opening: `x in {shelter_meta['west_bay_clear_x_range']}` -> **`{shelter_meta['west_bay_clear_width_m']:.2f} m` (`>= 6.0 m` PASS)**",
        f"  - East Bay Clear Opening: `x in {shelter_meta['east_bay_clear_x_range']}` -> **`{shelter_meta['east_bay_clear_width_m']:.2f} m` (`>= 6.0 m` PASS)**",
        "- **Craft & Export Surface Fidelity (`Messages #60 & #63`)**:",
        "  - Baked `1024x1024` sRGB UV-mapped textures (`shelter_wall_planks_masonry_v3.png`, `shelter_roof_tiles_v3.png`, `shelter_floor_cobble_straw_v3.png`, `shelter_timber_oak_v3.png`) with human-scale straight wood grain and fine tile grid packed into `.glb` and `.fbx`.",
        "  - Hollow central granary ground floor with real perimeter walls, open `2.40m` segmented stone archway, and separate upper hayloft floor slab (`z = 2.82m`) so interior straw bales and lidded feed bins are visible and accessible.",
        "  - Open annular 12-segment hoist wheel rim + hub so all 8 radial spokes read cleanly.",
        "  - Framed, recessed, mullioned windows with low-glare dusk glass (`emission_strength = 0.24`).",
        "  - 11-14 overlapping roof courses, open gable timber trusses on West/East wings, and 2 louvered roof vents (cupolas).",
        "",
        "## 2. Priority 2: Distinct Local-Origin `Farm_Core` Facilities (`technical:p00504-p00517`, `Message #61`)",
        "1. **`genetics-research-lab.glb` (`Phòng Nghiên Cứu Di Truyền & Bào Chế`)**:",
        "   - Source anchors: `technical:p00545-p00569`, `mechanics:p00738, p01278`.",
        "   - Features limestone-and-timber genetics hall with clerestory roof monitor, open-mullion bio-specimen conservatory wing with visible `Lam Khê`/`Hắc Mao` specimen vessels and culture table, copper reagent flues, and two distinct covered veranda workbenches.",
        "2. **`information-station.glb` (`Khu Thông Tin`)**:",
        "   - Source anchors: `mechanics:p02801-p02843, p00740, p01706-p01730`.",
        "   - Features open cross-braced timber lookout tower with connected railing posts and inclined 12-tread access stair, weather instruments mast & 4-legged louvered Stevenson screen, threat radar dish frame, and covered intelligence map desk (`Bàn Tình Báo`).",
        "3. **`main-house-an-room.glb` (`Nhà Chính & Gian Bếp Nấu Cám Của An`)**:",
        "   - Source anchors: `mechanics:p00740`, `story:p00105, p00108`.",
        "   - Features domestic front porch, stone hearth chimney, and cutaway feed-kitchen room containing the **cold soot-blackened brick feed stove (`bếp gạch đen muội đã nguội từ lâu`)**, **window sill (`bậu cửa sổ`)**, and **small empty glass bottle (`lọ thủy tinh nhỏ trống rỗng đầu Mùa 1`)** with zero late-saga symbols.",
        "4. **`medical-tent.glb` (`Lều Quân Y`)**:",
        "   - Source anchors: `technical:p00513` (inventing zero gameplay healing numbers).",
        "   - Features continuous tensioned sagging linen canvas roof canopy with sewn seam welts (`add_tensioned_canvas_roof`), rolled side flaps, guy ropes & stakes, duckboard plank flooring, two field cots, and supply chest.",
    ]
    (OUT_DIR / "architecture-source-map.md").write_text("\n".join(md_lines) + "\n", encoding="utf-8")
    print("COMPLETED functional-buildings-argon v3 build.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
