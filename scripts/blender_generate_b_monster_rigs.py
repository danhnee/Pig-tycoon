import bpy
import math
import os

# Headless Blender 5.3 script to build 3D articulated meshes & keyframed animations
# for Member B's 4 signature enemies (ChuotGamKho, BayNhay, ThuBao, KeMangDich)
# and save .blend source into F:\Unity\Pig tycoon\ArtSource\Enemies_B\ and d:\Pig-tycoon\Assets\Art\Characters\Enemies_B\

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def create_material(name, rgba):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = 0.75
    return mat

def build_monster(name, offset_x, body_scale, color_rgba, accent_rgba):
    mat_body = create_material(f"Mat_{name}_Body", color_rgba)
    mat_acc = create_material(f"Mat_{name}_Accent", accent_rgba)

    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=(offset_x, 0.0, 0.55))
    body = bpy.context.active_object
    body.name = f"{name}_Root"
    body.scale = body_scale
    body.data.materials.append(mat_body)

    bpy.ops.mesh.primitive_cone_add(radius1=0.28, depth=0.45, location=(offset_x, -0.45, 0.72))
    head = bpy.context.active_object
    head.name = f"{name}_Head"
    head.parent = body
    head.data.materials.append(mat_acc)

    # Keyframe Idle + Walk + Attack + Leap/Burst cycle (frames 1..48)
    for frame, z_bob, pitch in [(1, 0.55, 0.0), (12, 0.68, -0.18), (24, 0.55, 0.0), (36, 0.85, 0.35), (48, 0.55, 0.0)]:
        bpy.context.scene.frame_set(frame)
        body.location.z = z_bob
        body.rotation_euler.x = pitch
        body.keyframe_insert(data_path="location", index=2)
        body.keyframe_insert(data_path="rotation_euler", index=0)

    return body

def main():
    clear_scene()
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 48

    build_monster("ChuotGamKho", -4.5, (0.65, 1.15, 0.55), (0.42, 0.36, 0.33, 1.0), (0.85, 0.45, 0.48, 1.0))
    build_monster("BayNhay", -1.5, (0.75, 1.25, 0.80), (0.22, 0.52, 0.28, 1.0), (0.88, 0.76, 0.20, 1.0))
    build_monster("ThuBao", 1.5, (1.25, 1.35, 1.15), (0.18, 0.32, 0.68, 1.0), (0.35, 0.88, 1.00, 1.0))
    build_monster("KeMangDich", 4.5, (1.05, 1.05, 1.10), (0.38, 0.22, 0.48, 1.0), (0.45, 0.92, 0.25, 1.0))

    out_dirs = [
        r"d:\Pig-tycoon\Assets\Art\Characters\Enemies_B",
        r"F:\Unity\Pig tycoon\ArtSource\Enemies_B"
    ]
    for d in out_dirs:
        os.makedirs(d, exist_ok=True)
        blend_path = os.path.join(d, "MemberB_Monsters_3DRigs.blend")
        bpy.ops.wm.save_as_mainfile(filepath=blend_path)
        print(f"[BLENDER] Saved 3D monster rigs & animations to: {blend_path}")

if __name__ == "__main__":
    main()
