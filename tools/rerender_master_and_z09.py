#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Fast 15-second re-render of Zone09, Zone02+11, and Full-World Master views from exported .meshbin files
using the elliptical island-clipped terrain mesh.
"""
import struct
from pathlib import Path
import numpy as np
from validate_and_render_all_master import ROOT, True3DMeshRenderer, build_terrain_mesh
from citadel_diorama_ruins_geometry import build_all_citadel_diorama_ruins_zones
from observatory_geometry import build_observatory


def load_meshbin(path: Path):
    data = path.read_bytes()
    v_cnt, idx_cnt = struct.unpack_from("<ii", data, 0)
    off = 8
    pos_y_up = np.frombuffer(data, dtype="<f4", count=v_cnt * 3, offset=off).reshape(v_cnt, 3)
    off += v_cnt * 12
    off += v_cnt * 12  # skip normals
    uvs = np.frombuffer(data, dtype="<f4", count=v_cnt * 2, offset=off).reshape(v_cnt, 2).copy()
    off += v_cnt * 8
    idx_y_up = np.frombuffer(data, dtype="<i4", count=idx_cnt, offset=off).reshape(-1, 3)
    verts = pos_y_up[:, [0, 2, 1]].copy()
    faces = idx_y_up[:, [0, 2, 1]].copy()
    return verts, uvs, faces


def main():
    art = ROOT / ".collab/artifacts"
    out_z10 = art / "zone10-20261018"
    out_z04_06 = art / "zone04-06-20261018"
    out_z07_08 = art / "zone07-08-20261018"
    out_z02_09_11 = art / "zone02-09-11-20261018"
    out_world = art / "world-master-20261018"

    tex_dir = ROOT / "Assets/Textures/FarmMain3D"
    renderer = True3DMeshRenderer(
        tex_dir / "FarmMain_3D_Albedo_4K.png",
        tex_dir / "FarmMain_3D_Emission_4K.png",
    )

    tv, tuv, tf = build_terrain_mesh()
    cdr = build_all_citadel_diorama_ruins_zones()
    wv, wuv, wf = cdr["Zone03_Water_River_Waterfalls_And_3Ponds"].arrays()

    z10_v, z10_uv, z10_f = load_meshbin(out_z10 / "Zone10_Vista_AlpineMountains_And_ForestRidge.meshbin")
    z04_v, z04_uv, z04_f = load_meshbin(out_z04_06 / "Zone04_Building_KhuChuongTrai_Farmstead.meshbin")
    z06_v, z06_uv, z06_f = load_meshbin(out_z04_06 / "Zone06_Building_NhaCuongHoa_And_KhuNPC.meshbin")
    z07_v, z07_uv, z07_f = load_meshbin(out_z07_08 / "Zone07_Landmark_TramThongTin_InfoBeacon.meshbin")
    z08_v, z08_uv, z08_f = load_meshbin(out_z07_08 / "Zone08_Vista_StoneBridge_And_HarborTown.meshbin")
    z09_v, z09_uv, z09_f = load_meshbin(out_z02_09_11 / "Zone09_Vista_TwilightCastle_And_WaterfallCliff.meshbin")
    z02_v, z02_uv, z02_f = load_meshbin(out_z02_09_11 / "Zone02_Diorama_Cutaway_RockRim_And_Base.meshbin")
    z11_v, z11_uv, z11_f = load_meshbin(out_z02_09_11 / "Zone11_Props_AncientRuins_And_GoldenFlora.meshbin")
    ov, ouv, of = build_observatory().arrays((-7.8, 6.4, 3.2412), yaw=-0.05)

    renderer.render_scene([(wv, wuv, wf, 2), (z09_v, z09_uv, z09_f, 0)], out_z02_09_11 / "01-zone09-castle-cliff-pbr.png", (12.8, -11.5, 18.2), (14.4, 14.2, 10.6), ortho_scale=21.5, res_x=1280, res_y=960, wire_clay_mode=False)
    renderer.render_scene([(wv, wuv, wf, 2), (z09_v, z09_uv, z09_f, 0)], out_z02_09_11 / "02-zone09-castle-cliff-wireframe-clay.png", (12.8, -11.5, 18.2), (14.4, 14.2, 10.6), ortho_scale=21.5, res_x=1280, res_y=960, wire_clay_mode=True)

    renderer.render_scene([(z02_v, z02_uv, z02_f, 0), (tv, tuv, tf, 1), (wv, wuv, wf, 2), (z11_v, z11_uv, z11_f, 0)], out_z02_09_11 / "03-zone02-11-diorama-ruins-pbr.png", (-2.8, -50.0, 26.5), (0.0, -4.5, 2.2), persp_fov_deg=42.0, res_x=1280, res_y=900, wire_clay_mode=False)
    renderer.render_scene([(z02_v, z02_uv, z02_f, 0), (tv, tuv, tf, 1), (wv, wuv, wf, 2), (z11_v, z11_uv, z11_f, 0)], out_z02_09_11 / "04-zone02-11-diorama-ruins-wireframe-clay.png", (-2.8, -50.0, 26.5), (0.0, -4.5, 2.2), persp_fov_deg=42.0, res_x=1280, res_y=900, wire_clay_mode=True)

    full_world_meshes = [
        (z02_v, z02_uv, z02_f, 0),
        (tv, tuv, tf, 1),
        (wv, wuv, wf, 2),
        (z10_v, z10_uv, z10_f, 0),
        (z09_v, z09_uv, z09_f, 0),
        (z08_v, z08_uv, z08_f, 0),
        (z07_v, z07_uv, z07_f, 0),
        (z06_v, z06_uv, z06_f, 0),
        (ov, ouv, of, 0),
        (z04_v, z04_uv, z04_f, 0),
        (z11_v, z11_uv, z11_f, 0),
    ]
    renderer.render_scene(full_world_meshes, out_world / "01-full-world-upgraded-pbr.png", (-3.2, -55.5, 31.5), (0.5, -1.8, 3.2), persp_fov_deg=43.0, res_x=1600, res_y=1120, wire_clay_mode=False)
    renderer.render_scene(full_world_meshes, out_z10 / "07-world-context.png", (-3.2, -55.5, 31.5), (0.5, -1.8, 3.2), persp_fov_deg=43.0, res_x=1440, res_y=1020, wire_clay_mode=False)
    renderer.render_scene(full_world_meshes, out_world / "02-full-world-upgraded-wireframe-clay.png", (-3.2, -55.5, 31.5), (0.5, -1.8, 3.2), persp_fov_deg=43.0, res_x=1600, res_y=1120, wire_clay_mode=True)


if __name__ == "__main__":
    main()
