import bpy
import bmesh
import math
import os
import random

# Clear existing objects
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# Configure EEVEE Next / Render settings for fast, lush golden-hour rendering
for eng in ['BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'CYCLES']:
    try:
        scene.render.engine = eng
        break
    except Exception:
        pass

scene.render.resolution_x = 1600
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100

# Create collections matching Camera Design v1.0 architecture
col_playable = bpy.data.collections.new("01_Playable_FarmMain_3D")
col_pigs = bpy.data.collections.new("02_PigHerd_OneThird_MeadowZone")
col_vista = bpy.data.collections.new("03_DistantVista_3D_Models")
scene.collection.children.link(col_playable)
scene.collection.children.link(col_pigs)
scene.collection.children.link(col_vista)

def make_mat(name, base_rgb, roughness=0.65, metallic=0.0, emission_rgb=None, emission_strength=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        if emission_rgb and emission_strength > 0:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = (*emission_rgb, 1.0)
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = (*emission_rgb, 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

mat_meadow = make_mat("Mat_GoldenAmberMeadow", (0.85, 0.56, 0.16), roughness=0.82)
mat_path = make_mat("Mat_DirtPath", (0.68, 0.44, 0.22), roughness=0.88)
mat_water = make_mat("Mat_ReflectiveWater", (0.18, 0.46, 0.68), roughness=0.08, metallic=0.15)
mat_waterfall = make_mat("Mat_WaterfallFoam", (0.78, 0.90, 0.98), roughness=0.25, emission_rgb=(0.55, 0.78, 0.95), emission_strength=0.6)
mat_timber = make_mat("Mat_RusticTimber", (0.36, 0.22, 0.11), roughness=0.75)
mat_stone = make_mat("Mat_CarvedStone", (0.52, 0.48, 0.43), roughness=0.80)
mat_ruin_stone = make_mat("Mat_AncientRuneStone", (0.58, 0.52, 0.42), roughness=0.85)
mat_roof_warm = make_mat("Mat_WarmShingleRoof", (0.55, 0.27, 0.14), roughness=0.70)
mat_roof_red = make_mat("Mat_TownRedRoof", (0.72, 0.22, 0.14), roughness=0.65)
mat_roof_blue = make_mat("Mat_ResearchBlueRoof", (0.16, 0.29, 0.58), roughness=0.55)
mat_brass = make_mat("Mat_BrassTelescope", (0.85, 0.66, 0.24), roughness=0.25, metallic=0.88)
mat_window_glow = make_mat("Mat_WarmWindowGlow", (1.0, 0.72, 0.25), roughness=0.2, emission_rgb=(1.0, 0.68, 0.20), emission_strength=4.5)
mat_blue_crystal = make_mat("Mat_InfoBlueCrystal", (0.20, 0.72, 1.0), roughness=0.1, emission_rgb=(0.18, 0.68, 1.0), emission_strength=6.5)
mat_gold_flora = make_mat("Mat_GoldenMythFlora", (1.0, 0.82, 0.32), roughness=0.25, emission_rgb=(1.0, 0.76, 0.22), emission_strength=3.8)
mat_pig_pink = make_mat("Mat_PigRosyPink", (0.98, 0.68, 0.64), roughness=0.45)
mat_pine = make_mat("Mat_PineFoliage", (0.18, 0.34, 0.16), roughness=0.80)
mat_cliff = make_mat("Mat_CliffRock", (0.42, 0.36, 0.34), roughness=0.88)
mat_mountain = make_mat("Mat_DistantMountain", (0.38, 0.28, 0.42), roughness=0.90)
mat_portal = make_mat("Mat_PortalKhuNPC", (0.30, 0.85, 1.0), roughness=0.15, emission_rgb=(0.25, 0.80, 1.0), emission_strength=3.5)

def link_to_col(obj, col):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)

def add_box(name, loc, scale, mat, col, rot_deg=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = tuple(math.radians(a) for a in rot_deg)
    obj.data.materials.append(mat)
    link_to_col(obj, col)
    return obj

def add_cyl(name, loc, radius, depth, mat, col, rot_deg=(0, 0, 0), vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = tuple(math.radians(a) for a in rot_deg)
    obj.data.materials.append(mat)
    link_to_col(obj, col)
    return obj

def add_cone(name, loc, r1, r2, depth, mat, col, vertices=16, rot_deg=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2, depth=depth, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = tuple(math.radians(a) for a in rot_deg)
    obj.data.materials.append(mat)
    link_to_col(obj, col)
    return obj

def add_sphere(name, loc, radius, mat, col, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=18, ring_count=12, radius=radius, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    link_to_col(obj, col)
    return obj

# Note on Blender coordinate axes:
# X = East (+) / West (-)
# Y = North (+, into background) / South (-, toward foreground camera)
# Z = Up (+)

# 1. SLOPING GOLDEN-AMBER MEADOW TERRAIN (120m x 90m Playable Core)
bpy.ops.mesh.primitive_grid_add(x_subdivisions=60, y_subdivisions=45, size=1.0, location=(0, 0, 0))
terrain = bpy.context.active_object
terrain.name = "FarmMain_GoldenMeadow_Terrain"
terrain.scale = (130.0, 100.0, 1.0)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

for v in terrain.data.vertices:
    x, y, _ = v.co
    # Gentle hillside rise toward the back-left (barn & construction hall) and drop toward the river on the right (x > 36)
    slope_z = max(0.0, (y + 35.0) * 0.055) + max(0.0, (-x + 20.0) * 0.025)
    # Carve 3 shallow pond basins in the foreground/midground meadow
    for px, py, rx, ry in [(8.0, -4.0, 8.0, 4.5), (-10.0, -18.0, 10.0, 4.5), (22.0, -26.0, 6.5, 4.0)]:
        d2 = ((x - px) / rx) ** 2 + ((y - py) / ry) ** 2
        if d2 < 1.0:
            slope_z -= (1.0 - d2) * 1.15
    # Carve river gorge on the right (x > 38)
    if x > 38.0:
        slope_z -= min(4.5, (x - 38.0) * 0.35)
    v.co.z = slope_z

terrain.data.materials.append(mat_meadow)
link_to_col(terrain, col_playable)

def get_ground_z(x, y):
    return max(0.0, (y + 35.0) * 0.055) + max(0.0, (-x + 20.0) * 0.025)

# 2. THREE REFLECTIVE MEADOW PONDS (Matching Image 1)
for idx, (px, py, sx, sy) in enumerate([(8.0, -4.0, 15.0, 8.2), (-10.0, -18.0, 18.5, 8.5), (22.0, -26.0, 12.0, 7.0)], 1):
    gz = get_ground_z(px, py) - 0.25
    add_cyl(f"Pond_{idx}_Water", (px, py, gz), 1.0, 0.15, mat_water, col_playable)
    bpy.context.active_object.scale = (sx * 0.5, sy * 0.5, 1.0)

# 3. PLAYABLE FARM BUILDINGS (Matching Image 1)
def build_house(prefix, x, y, w, d, h, roof_mat, col, has_telescope=False, has_crane=False):
    gz = get_ground_z(x, y)
    # Stone foundation & timber walls
    add_box(f"{prefix}_Foundation", (x, y, gz + 0.4), (w + 0.4, d + 0.4, 0.8), mat_stone, col)
    add_box(f"{prefix}_Walls", (x, y, gz + 0.8 + h * 0.5), (w, d, h), mat_timber, col)
    # Pitched roof (rotated cylinder with 3 vertices = prism)
    add_cyl(f"{prefix}_Roof", (x, y, gz + 0.8 + h + h * 0.38), d * 0.68, w + 0.8, roof_mat, col, rot_deg=(0, 90, 0), vertices=3)
    # Glowing warm windows
    for wx in [-w * 0.28, w * 0.28]:
        add_box(f"{prefix}_WinFront_{wx:.1f}", (x + wx, y - d * 0.51, gz + 0.8 + h * 0.48), (1.1, 0.15, 1.3), mat_window_glow, col)
    # Chimney
    add_box(f"{prefix}_Chimney", (x + w * 0.32, y + d * 0.22, gz + 0.8 + h + 1.2), (1.1, 1.1, 2.6), mat_stone, col)
    if has_telescope:
        # Observatory dome & gleaming brass telescope pointing skyward
        dome_z = gz + 0.8 + h + 1.1
        add_sphere(f"{prefix}_ObservatoryDome", (x + 1.0, y, dome_z), 2.4, mat_roof_blue, col)
        add_cyl(f"{prefix}_BrassTelescope", (x + 2.4, y - 0.4, dome_z + 1.6), 0.48, 4.8, mat_brass, col, rot_deg=(48, 35, -20))
    if has_crane:
        add_box(f"{prefix}_CraneBeam", (x - w * 0.45, y - d * 0.35, gz + 0.8 + h + 0.8), (0.35, 2.8, 0.35), mat_timber, col)

# 3A. Khu chuồng trại (Main Barn + Wooden Pen Fence on Left-Midground)
build_house("KhuChuongTrai_MainBarn", -32.0, 18.0, 11.0, 8.0, 4.2, mat_roof_warm, col_playable)
build_house("KhoVaNhaBep_Granary", -43.0, 21.0, 6.5, 5.5, 3.4, mat_roof_warm, col_playable)

# Wooden Pen Fence around Khu chuồng trại (-46.. -18, 6.. 24)
pen_x0, pen_x1, pen_y0, pen_y1 = -46.0, -18.0, 6.0, 24.0
for fx in range(int(pen_x0), int(pen_x1) + 1, 3):
    for fy in [pen_y0, pen_y1]:
        gz = get_ground_z(fx, fy)
        add_box(f"FencePost_X_{fx}_{fy}", (fx, fy, gz + 0.65), (0.28, 0.28, 1.3), mat_timber, col_playable)
        if fx + 3 <= pen_x1:
            add_box(f"FenceRail_X_{fx}_{fy}", (fx + 1.5, fy, gz + 0.85), (3.0, 0.14, 0.18), mat_timber, col_playable)
for fy in range(int(pen_y0), int(pen_y1) + 1, 3):
    for fx in [pen_x0, pen_x1]:
        gz = get_ground_z(fx, fy)
        add_box(f"FencePost_Y_{fx}_{fy}", (fx, fy, gz + 0.65), (0.28, 0.28, 1.3), mat_timber, col_playable)
        if fy + 3 <= pen_y1:
            add_box(f"FenceRail_Y_{fx}_{fy}", (fx, fy + 1.5, gz + 0.85), (0.14, 3.0, 0.18), mat_timber, col_playable)

# 3B. Nhà xây dựng (Upper-left behind Khu chuồng trại)
build_house("NhaXayDung_ConstructionHall", -26.0, 34.0, 9.5, 7.2, 4.0, mat_roof_warm, col_playable, has_crane=True)

# 3C. Nhà nghiên cứu (Center-left with blue roof & brass telescope)
build_house("NhaNghienCuu_ResearchObservatory", -6.0, 22.0, 10.0, 7.8, 4.2, mat_roof_blue, col_playable, has_telescope=True)

# 3D. Trạm thông tin (Right-midground near river: carved stone obelisk + glowing blue crystal orb)
ib_x, ib_y = 28.0, 14.0
ib_z = get_ground_z(ib_x, ib_y)
add_box("TramThongTin_BaseStep1", (ib_x, ib_y, ib_z + 0.35), (4.6, 4.6, 0.7), mat_stone, col_playable)
add_box("TramThongTin_BaseStep2", (ib_x, ib_y, ib_z + 0.95), (3.4, 3.4, 0.6), mat_stone, col_playable)
add_box("TramThongTin_ObeliskPillar", (ib_x, ib_y, ib_z + 3.4), (1.7, 1.7, 4.4), mat_stone, col_playable)
add_sphere("TramThongTin_BlueCrystalOrb", (ib_x, ib_y, ib_z + 6.5), 1.15, mat_blue_crystal, col_playable)

# 4. ANCIENT STONE RUINS & GLOWING GOLDEN MYTHOLOGICAL FLORA IN THE MEADOW
ruin_coords = [
    (-22.0, -6.0), (-14.0, -24.0), (16.0, -14.0), (30.0, -18.0),
    (-34.0, -16.0), (4.0, 6.0), (18.0, 4.0), (-8.0, -32.0), (26.0, -32.0)
]
for idx, (rx, ry) in enumerate(ruin_coords, 1):
    rz = get_ground_z(rx, ry)
    add_box(f"AncientRuinBlock_{idx}", (rx, ry, rz + 0.8), (2.4, 1.2, 2.2), mat_ruin_stone, col_playable, rot_deg=(8, -12, idx * 23))
    # Glowing golden mythological low crystal-leaf plant beside each ruin
    fx, fy = rx + 1.8, ry - 1.1
    fz = get_ground_z(fx, fy)
    for leaf_i in range(5):
        ang = leaf_i * 72
        add_cone(f"GoldenFlora_{idx}_Leaf_{leaf_i}", (fx, fy, fz + 0.85), 0.45, 0.05, 1.7, mat_gold_flora, col_playable, rot_deg=(28, 0, ang))

# 5. SINGLE MAP TRANSITION PORTAL TO KHU NPC (At the end of the path behind the farm: (8.0, 43.5))
px_npc, py_npc = 8.0, 43.5
pz_npc = get_ground_z(px_npc, py_npc)
add_cyl("Portal_FarmMain_To_KhuNPC", (px_npc, py_npc, pz_npc + 0.25), 3.2, 0.35, mat_portal, col_playable)

# 6. FREE-ROAMING 3D PIGS RESTRICTED TO <= 1/3 MEADOW AREA NEAR KHU CHUONG TRAI
# Allowed 1/3 pasture box: X in [-48..+2], Y in [-6..+26] (strictly near Khu chuồng trại, both inside and outside the fence!)
pig_positions = [
    # Inside pen
    (-38.0, 12.0), (-34.0, 14.0), (-29.0, 11.5), (-25.0, 15.0), (-41.0, 16.5), (-22.0, 12.5),
    # Outside fence on the adjacent 1/3 meadow grass near barn
    (-28.0, 2.5), (-19.0, 4.0), (-14.0, 8.5), (-11.0, 1.5), (-24.0, -2.5), (-8.0, 11.0), (-16.0, -4.0), (-4.0, 3.0)
]
for idx, (pig_x, pig_y) in enumerate(pig_positions, 1):
    pz = get_ground_z(pig_x, pig_y)
    # Plump stylized 3D pig body + head + snout + ears
    body = add_sphere(f"Pig3D_{idx:02d}_Body", (pig_x, pig_y, pz + 0.55), 0.55, mat_pig_pink, col_pigs, scale=(1.35, 0.92, 0.88))
    head = add_sphere(f"Pig3D_{idx:02d}_Head", (pig_x + 0.58, pig_y, pz + 0.68), 0.36, mat_pig_pink, col_pigs)

# 7. DISTANT 3D BACKGROUND VISTA MODELS (NOT 2D FLAT BACKDROPS, NOT WALKABLE IN FARM_MAIN)
# 7A. Khu NPC Distant 3D Village Cluster (At the end of the back path: Y = 62..74)
for idx, (vx, vy, vw, vd, vh) in enumerate([
    (8.0, 64.0, 7.5, 6.0, 3.6),
    (17.5, 66.0, 8.5, 6.5, 4.0),
    (26.0, 63.0, 6.8, 5.5, 3.4),
], 1):
    build_house(f"DistantVista_KhuNPC_House_{idx}", vx, vy, vw, vd, vh, mat_roof_warm, col_vista)

# 7B. River, Waterfall Cliff & Multi-Arch Stone Bridge leading to Chợ / Thị trấn
add_box("DistantVista_RiverWater", (54.0, 22.0, -1.2), (28.0, 130.0, 0.5), mat_water, col_vista)

# Multi-Arch Stone Bridge (X = 36..66, Y = 38)
for bx in range(38, 66, 7):
    add_box(f"DistantVista_BridgePier_{bx}", (bx, 38.0, 1.5), (2.2, 4.8, 5.2), mat_stone, col_vista)
add_box("DistantVista_StoneBridgeDeck", (51.0, 38.0, 4.2), (32.0, 4.6, 1.1), mat_stone, col_vista)
add_box("DistantVista_BridgeWatchtower", (51.0, 38.0, 7.2), (4.2, 4.8, 5.2), mat_stone, col_vista)
add_cone("DistantVista_BridgeTowerRoof", (51.0, 38.0, 11.2), 3.4, 0.1, 3.2, mat_roof_warm, col_vista, vertices=4)

# 7C. Chợ / Thị trấn (Distant 3D Market Town on the right across the river: X = 68..96, Y = 8..46)
market_houses = [
    (70.0, 16.0, 7.5, 6.2, 4.2),
    (78.0, 22.0, 8.2, 6.8, 4.8),
    (72.0, 30.0, 7.8, 6.0, 4.4),
    (84.0, 14.0, 8.5, 6.5, 5.0),
    (86.0, 28.0, 9.0, 7.2, 5.4),
    (80.0, 38.0, 8.0, 6.5, 4.6),
    (92.0, 22.0, 9.5, 7.5, 6.0),
]
for idx, (mx, my, mw, md, mh) in enumerate(market_houses, 1):
    build_house(f"DistantVista_ChoThiTran_Building_{idx}", mx, my, mw, md, mh, mat_roof_red, col_vista)

# 7D. Waterfall Cliff & Majestic Multi-Spired Twilight Castle in the Distant Background (X = 48, Y = 105)
add_box("DistantVista_WaterfallCliff", (48.0, 98.0, 10.0), (54.0, 28.0, 22.0), mat_cliff, col_vista)
for wf_x in [32.0, 42.0, 52.0, 62.0]:
    add_box(f"DistantVista_WaterfallStream_{wf_x:.0f}", (wf_x, 83.5, 9.5), (3.8, 1.5, 20.0), mat_waterfall, col_vista)

castle_x, castle_y, castle_z = 48.0, 102.0, 21.0
add_box("DistantVista_CastleKeep", (castle_x, castle_y, castle_z + 7.0), (22.0, 16.0, 14.0), mat_stone, col_vista)
for tx, ty, th, tr in [
    (castle_x, castle_y, 26.0, 3.6),
    (castle_x - 8.0, castle_y - 5.0, 20.0, 2.6),
    (castle_x + 8.0, castle_y - 5.0, 21.0, 2.6),
    (castle_x - 11.0, castle_y + 4.0, 18.0, 2.2),
    (castle_x + 11.0, castle_y + 4.0, 19.0, 2.2),
]:
    add_cyl(f"DistantVista_CastleTower_{tx:.0f}_{ty:.0f}", (tx, ty, castle_z + th * 0.5), tr, th, mat_stone, col_vista)
    add_cone(f"DistantVista_CastleSpire_{tx:.0f}_{ty:.0f}", (tx, ty, castle_z + th + 4.5), tr * 1.25, 0.1, 9.0, mat_roof_blue, col_vista)

# Pine trees along the upper ridge and riverside
random.seed(42)
for i in range(45):
    tx = random.uniform(-58.0, 35.0)
    ty = random.uniform(36.0, 62.0)
    if abs(tx - 8.0) < 6.0:
        continue  # Keep the Khu NPC path clear
    tz = get_ground_z(tx, ty)
    add_cone(f"RidgePine_{i:02d}", (tx, ty, tz + 3.2), 2.1, 0.1, 6.5, mat_pine, col_vista)

# Distant mountain range backdrop
for idx, (mx, my, mw, mh) in enumerate([
    (-45.0, 125.0, 42.0, 38.0),
    (-10.0, 130.0, 48.0, 44.0),
    (25.0, 135.0, 45.0, 40.0),
    (82.0, 128.0, 50.0, 42.0),
], 1):
    add_cone(f"DistantMountainPeak_{idx}", (mx, my, mh * 0.45), mw * 0.6, 1.5, mh, mat_mountain, col_vista, vertices=7)

# 8. GOLDEN HOUR SUNSET LIGHTING & SKY
world = bpy.data.worlds.new("PigTycoon_GoldenSunsetWorld")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.96, 0.56, 0.32, 1.0)
    bg.inputs[1].default_value = 1.15

bpy.ops.object.light_add(type='SUN', location=(45.0, 90.0, 45.0))
sun = bpy.context.active_object
sun.name = "GoldenHour_Sunset_Light"
sun.data.energy = 4.5
sun.data.color = (1.0, 0.76, 0.44)
sun.rotation_euler = (math.radians(68), math.radians(15), math.radians(-155))

# 9. SAVE .BLEND AND EXPORT .GLB TO BOTH UNITY WORKSPACES
os.makedirs(r"F:\Unity\Pig tycoon\ArtSource\Blender_3D_Map", exist_ok=True)
os.makedirs(r"F:\Unity\Pig tycoon\Assets\Art\Models3D", exist_ok=True)
os.makedirs(r"d:\Pig-tycoon\Assets\Art\Models3D", exist_ok=True)

blend_path = r"F:\Unity\Pig tycoon\ArtSource\Blender_3D_Map\Farm_Main_3D_Master.blend"
bpy.ops.wm.save_as_mainfile(filepath=blend_path)

glb_path_d = r"d:\Pig-tycoon\Assets\Art\Models3D\Farm_Main_3D_Complete.glb"
glb_path_f = r"F:\Unity\Pig tycoon\Assets\Art\Models3D\Farm_Main_3D_Complete.glb"
bpy.ops.export_scene.gltf(filepath=glb_path_d, export_format='GLB')
bpy.ops.export_scene.gltf(filepath=glb_path_f, export_format='GLB')

# 10. RENDER 4 STANDARDIZED CAMERA DESIGN V1.0 VIEWS
# Note: In Blender, Euler X = 90 - pitch_from_horizontal
artifact_dir = r"C:\Users\Admin\.gemini\antigravity\brain\2526dd2a-9e91-4dbf-a3dc-12d822a774b4"
bpy.ops.object.camera_add(location=(0, -45, 22))
cam = bpy.context.active_object
cam.name = "PigTycoon_CameraDesignV1"
cam.data.type = 'PERSP'
cam.data.sensor_fit = 'VERTICAL'
cam.data.angle = math.radians(58.0)
cam.data.clip_end = 500.0
scene.camera = cam

camera_views = [
    ("Render_01_CameraMode_Farm_24deg.png", (6.0, -48.0, 26.0), 24.0, -8.0, 58.0),
    ("Render_02_CameraMode_Combat_34deg.png", (-4.0, -34.0, 24.0), 34.0, -6.0, 58.0),
    ("Render_03_CameraMode_Defense_43deg.png", (-12.0, -28.0, 32.0), 43.0, -10.0, 58.0),
    ("Render_04_CameraMode_Scenic_15deg.png", (10.0, -54.0, 19.0), 15.0, -12.0, 56.0),
]

for filename, loc, pitch_deg, yaw_deg, vfov in camera_views:
    cam.location = loc
    cam.rotation_euler = (math.radians(90.0 - pitch_deg), 0.0, math.radians(yaw_deg))
    cam.data.angle = math.radians(vfov)
    scene.render.filepath = os.path.join(artifact_dir, filename)
    bpy.ops.render.render(write_still=True)
    print("Rendered:", filename)

print("COMPLETE_3D_MAP_BUILD_AND_RENDER_SUCCESS")
