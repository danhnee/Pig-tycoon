import bpy
import os
import numpy as np

KIT_ROOT = r"C:\Users\Admin\.gemini\antigravity\brain\2526dd2a-9e91-4dbf-a3dc-12d822a774b4\scratch\external_3d_kits"
FBX_DIR = os.path.join(KIT_ROOT, r"Quaternius-Stylized-Nature\ultimate-stylized-nature\sources\FBX")
OUT_NPZ = r"C:\Users\Admin\.gemini\antigravity\brain\2526dd2a-9e91-4dbf-a3dc-12d822a774b4\scratch\quaternius_nature_cache.npz"

ASSETS = [
    "Bush_Large_Flowers.fbx",
    "Flower_1_Clump.fbx", "Flower_2_Clump.fbx", "Flower_3_Clump.fbx",
    "Rock_1.fbx", "Rock_2.fbx", "Rock_3.fbx", "Rock_4.fbx", "Rock_5.fbx",
]

data_dict = {}

for fname in ASSETS:
    fpath = os.path.join(FBX_DIR, fname)
    if not os.path.exists(fpath):
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fpath)
    bpy.context.view_layer.update()
    mesh_objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not mesh_objs:
        continue

    all_v = []
    all_f = []
    v_offset = 0

    for obj in mesh_objs:
        me = obj.data
        me.calc_loop_triangles()
        mw = obj.matrix_world
        verts = np.array([list(mw @ v.co) for v in me.vertices], dtype=np.float32)
        all_v.append(verts)
        for tri in me.loop_triangles:
            all_f.append([tri.vertices[0] + v_offset, tri.vertices[1] + v_offset, tri.vertices[2] + v_offset])
        v_offset += len(verts)

    v_arr = np.vstack(all_v)
    min_xyz = np.min(v_arr, axis=0)
    max_xyz = np.max(v_arr, axis=0)
    c_xy = 0.5 * (min_xyz[:2] + max_xyz[:2])
    v_arr[:, 0] -= c_xy[0]
    v_arr[:, 1] -= c_xy[1]
    v_arr[:, 2] -= min_xyz[2]
    size_xyz = max_xyz - min_xyz

    key = fname[:-4]
    data_dict[f"{key}_v"] = v_arr
    data_dict[f"{key}_size"] = size_xyz
    data_dict[f"{key}_f"] = np.array(all_f, dtype=np.int32).reshape(-1, 3)
    print(f"Cached {key}: verts={len(v_arr)} tris={len(all_f)} size_Z_up={np.round(size_xyz, 2)}")

np.savez_compressed(OUT_NPZ, **data_dict)
print("Saved Quaternius Z-up cache to:", OUT_NPZ)
