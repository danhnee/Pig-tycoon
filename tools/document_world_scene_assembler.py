"""Assemble independent Farm_Core and Hub_Village candidates from reviewed assets.

Uses the D-drive isolated bpy runtime. Never opens Unity or modifies live scenes.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
from pathlib import Path
import random
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / ".collab/artifacts/world-scene-layout-codex/assembly"
RUNTIME = ROOT / ".collab/artifacts/mountain-reference-study-codex/runtime"
FACILITIES = ROOT / ".collab/artifacts/argon-facilities-and-scenes-codex"
FUNCTIONAL = ROOT / ".collab/artifacts/functional-buildings-codex"
sys.path.insert(0, str(RUNTIME))
sys.stdout.reconfigure(encoding="utf-8")
import bpy
from mathutils import Vector

ASSETS = {name: FACILITIES / f"{name}.glb" for name in ("main_house", "research_laboratory", "medical_tent", "information_complex", "hub_village_sanctum")}
ASSETS.update({name: FUNCTIONAL / f"{name}.glb" for name in ("warehouse_A", "warehouse_B", "warehouse_C", "construction_workshop", "enhancement_atelier")})


def bounds(objects):
    points = [obj.matrix_world @ Vector(p) for obj in objects if obj.type == "MESH" for p in obj.bound_box]
    return [[min(p[i] for p in points) for i in range(3)], [max(p[i] for p in points) for i in range(3)]]


def import_asset(name):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(ASSETS[name]))
    return [obj for obj in bpy.data.objects if obj not in before]


def inspect():
    records = []
    for name, path in ASSETS.items():
        bpy.ops.wm.read_factory_settings(use_empty=True)
        objects = import_asset(name)
        box = bounds(objects)
        records.append({"asset": name, "path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                        "bounds_xyz_m": box, "dimensions_m": [box[1][i]-box[0][i] for i in range(3)],
                        "objects": [obj.name for obj in objects if obj.type == "MESH"]})
    (OUT / "input-asset-inspection.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
    print(json.dumps(records), flush=True)


def material(name, color, roughness=.8, metallic=0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = (*color, 1)
    node.inputs["Roughness"].default_value = roughness
    node.inputs["Metallic"].default_value = metallic
    mat.diffuse_color = (*color, 1)
    return mat


class Landscape:
    """Batch detailed environment meshes by material to keep assembly efficient."""
    def __init__(self, materials):
        self.materials, self.parts = materials, {}

    def mesh(self, name, vertices, faces):
        vv, ff = self.parts.setdefault(name, ([], []))
        offset = len(vv)
        vv.extend(vertices)
        ff.extend(tuple(i+offset for i in face) for face in faces)

    def box(self, center, size, name, rotation=0):
        x, y, z = center
        a, b, c = (v/2 for v in size)
        points = [(-a,-b,-c),(a,-b,-c),(a,b,-c),(-a,b,-c),(-a,-b,c),(a,-b,c),(a,b,c),(-a,b,c)]
        cos, sin = math.cos(rotation), math.sin(rotation)
        self.mesh(name, [(x+u*cos-v*sin,y+u*sin+v*cos,z+w) for u,v,w in points],
                  [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])

    def cylinder(self, center, radius, height, name, sides=10, top_radius=None):
        x, y, z = center
        top = radius if top_radius is None else top_radius
        vv = [(x+r*math.cos(i*math.tau/sides),y+r*math.sin(i*math.tau/sides),z+h)
              for r,h in ((radius,-height/2),(top,height/2)) for i in range(sides)]
        ff = [tuple(reversed(range(sides))), tuple(range(sides,sides*2))]
        ff += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
        self.mesh(name, vv, ff)

    def beam(self, a, b, width, name):
        av, bv = Vector(a), Vector(b)
        delta = bv-av
        u = delta.cross(Vector((0,0,1)))
        if u.length < .001:
            u = delta.cross(Vector((0,1,0)))
        u.normalize()
        v = delta.normalized().cross(u)
        u *= width/2
        v *= width/2
        vv = [av-u-v,av+u-v,av+u+v,av-u+v,bv-u-v,bv+u-v,bv+u+v,bv-u+v]
        self.mesh(name, [tuple(p) for p in vv], [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])

    def foliage(self, center, radius, name, seed):
        rng = random.Random(seed)
        x,y,z = center
        sides, rings = 9, 5
        vv = [(x,y,z-radius*.75)]
        for r in range(1,rings):
            phi = -math.pi/2 + math.pi*r/rings
            for i in range(sides):
                angle = i*math.tau/sides + (r%2)*.18
                rr = radius * math.cos(phi) * rng.uniform(.82,1.15)
                vv.append((x+rr*math.cos(angle),y+rr*math.sin(angle),z+radius*.85*math.sin(phi)))
        vv.append((x,y,z+radius*.8))
        ff = [(0,1+(i+1)%sides,1+i) for i in range(sides)]
        for r in range(rings-2):
            a,b = 1+r*sides,1+(r+1)*sides
            ff += [(a+i,a+(i+1)%sides,b+(i+1)%sides,b+i) for i in range(sides)]
        top = len(vv)-1
        ff += [(top,1+(rings-2)*sides+i,1+(rings-2)*sides+(i+1)%sides) for i in range(sides)]
        self.mesh(name,vv,ff)

    def finish(self, collection):
        objects=[]
        for name,(vv,ff) in self.parts.items():
            me=bpy.data.meshes.new("landscape_"+name)
            me.from_pydata(vv,[],ff)
            me.update()
            ob=bpy.data.objects.new("landscape_"+name,me)
            collection.objects.link(ob)
            me.materials.append(self.materials[name])
            objects.append(ob)
        return objects


def source_place(name, center, collection, semantic, rotation=0):
    objects=import_asset(name)
    mesh_objects=[ob for ob in objects if ob.type=="MESH"]
    # Importer restores source-local Blender axes. Keep native dimensions, no scale.
    for ob in objects:
        for parent_collection in list(ob.users_collection):
            parent_collection.objects.unlink(ob)
        collection.objects.link(ob)
        if ob.parent is None:
            ob.rotation_euler.z += rotation
            ob.location += Vector((center[0],center[1],0))
        ob["semantic_role"]=semantic
        ob["source_asset"]=str(ASSETS[name])
        ob.name=semantic+"__"+ob.name
    bpy.context.view_layer.update()
    box=bounds(mesh_objects)
    return {"semantic":semantic,"source_asset":name,"source_path":str(ASSETS[name]),
            "sha256":hashlib.sha256(ASSETS[name].read_bytes()).hexdigest(),
            "placement_xy_m":list(center),"rotation_z_degrees":math.degrees(rotation),
            "scale":[1,1,1],"bounds_xyz_m":box,"mesh_count":len(mesh_objects)},mesh_objects


def road_network(batch,roads):
    """Exact rectilinear union: no overlapping coplanar road surfaces at junctions."""
    rectangles=[route_rect(a,b,r["width_m"]) for r in roads for a,b in zip(r["points"],r["points"][1:]) if a!=b]
    xs=sorted({value for x0,y0,x1,y1 in rectangles for value in (x0,x1)})
    ys=sorted({value for x0,y0,x1,y1 in rectangles for value in (y0,y1)})
    occupied=set()
    for i,(x0,x1) in enumerate(zip(xs,xs[1:])):
        for j,(y0,y1) in enumerate(zip(ys,ys[1:])):
            x,y=(x0+x1)/2,(y0+y1)/2
            if any(a<=x<=c and b<=y<=d for a,b,c,d in rectangles):
                occupied.add((i,j))
                batch.box((x,y,.035),(x1-x0,y1-y0,.06),"path")
    for i,j in occupied:
        x0,x1,y0,y1=xs[i],xs[i+1],ys[j],ys[j+1]
        edges=[((i-1,j),(x0,y0),(x0,y1)),((i+1,j),(x1,y0),(x1,y1)),
               ((i,j-1),(x0,y0),(x1,y0)),((i,j+1),(x0,y1),(x1,y1))]
        for neighbour,a,b in edges:
            if neighbour in occupied:continue
            length=math.dist(a,b)
            count=max(1,math.ceil(length/.85))
            angle=math.atan2(b[1]-a[1],b[0]-a[0])
            for k in range(count):
                t=(k+.5)/count
                batch.box((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t,.075),
                          (length/count*.93,.19,.10),"stone",angle)


def gate(batch,center,width,rotation=0):
    x,y=center
    def p(u,v,z):return (x+u*math.cos(rotation)-v*math.sin(rotation),y+u*math.sin(rotation)+v*math.cos(rotation),z)
    for side in (-1,1):
        for level in range(5):
            batch.box(p(side*(width/2+.45),0,.32+level*.60),(.9,.95,.56),"stone",rotation)
        batch.box(p(side*(width/2+.45),0,3.18),(1.15,1.15,.26),"stone_light",rotation)
        batch.cylinder(p(side*(width/2+.45),0,3.55),.18,.55,"brass",8)
    batch.beam(p(-width/2-.7,0,3.38),p(width/2+.7,0,3.38),.32,"oak")
    batch.beam(p(-width/2-.5,0,3.65),p(0,0,4.15),.20,"oak")
    batch.beam(p(width/2+.5,0,3.65),p(0,0,4.15),.20,"oak")


def well(batch,center):
    x,y=center
    for row in range(3):
        for i in range(12):
            angle=(i+.5*(row%2))*math.tau/12
            batch.box((x+1.1*math.cos(angle),y+1.1*math.sin(angle),.18+row*.34),(.62,.42,.31),"stone",angle+math.pi/2)
    batch.cylinder((x,y,.12),.88,.08,"water",24)
    for side in (-1,1):
        batch.box((x+side*1.4,y,1.6),(.18,.18,3.2),"oak")
    batch.beam((x-1.6,y,3.0),(x+1.6,y,3.0),.24,"oak")
    batch.cylinder((x,y,2.5),.04,1.2,"oak",8)
    batch.cylinder((x,y,1.8),.26,.45,"oak",12)


def village_noticeboard(batch,center):
    x,y=center
    for side in (-1,1):batch.box((x+side*.95,y,1.1),(.14,.14,2.2),"oak")
    batch.box((x,y,1.7),(2.1,.18,1.1),"oak")
    for dx in (-.65,0,.65):batch.box((x+dx,y-.11,1.75),(.5,.03,.70),"linen")
    batch.beam((x-1.2,y,2.4),(x,y,2.8),.17,"oak")
    batch.beam((x+1.2,y,2.4),(x,y,2.8),.17,"oak")


def bench(batch,center,rotation=0):
    x,y=center
    batch.box((x,y,.65),(1.9,.52,.16),"oak",rotation)
    batch.box((x-math.sin(rotation)*.25,y+math.cos(rotation)*.25,1.05),(1.9,.12,.62),"oak",rotation)
    for side in (-1,1):batch.box((x+side*.65*math.cos(rotation),y+side*.65*math.sin(rotation),.32),(.14,.42,.64),"oak",rotation)


def landscape(scene_id,size,roads,records,collection):
    colors={"grass":(.20,.31,.13),"soil":(.32,.24,.15),"path":(.51,.43,.30),"stone":(.37,.40,.33),
            "stone_light":(.57,.56,.43),"oak":(.23,.12,.045),"brass":(.56,.38,.14),
            "foliage_green":(.20,.33,.11),"foliage_gold":(.65,.43,.12),"foliage_olive":(.36,.43,.15),
            "flowers":(.78,.58,.18),"linen":(.8,.73,.57),"water":(.12,.32,.32)}
    materials={name:material(name,rgb) for name,rgb in colors.items()}
    batch=Landscape(materials)
    w,h=size
    batch.box((w/2,h/2,-.30),(w,h,.6),"grass")
    batch.box((w/2,h/2,-.75),(w+.1,h+.1,.35),"soil")
    paved_routes=list(roads)
    if scene_id=="Farm_Core":paved_routes.append({"points":[[30,-20],[30,0]],"width_m":8})
    road_network(batch,paved_routes)
    if scene_id=="Farm_Core":
        batch.box((30,-10,-.30),(8,20,.60),"grass")
        gate(batch,(30,-.2),8)
        for y in range(-19,0,3):
            for x in (25.8,34.2):
                batch.box((x,y,1.1),(.24,.24,2.2),"oak")
        for side in (-1,1):
            for z in (.65,1.4):batch.beam((30+side*4.2,-20,z),(30+side*4.2,0,z),.13,"oak")
    else:
        gate(batch,(40,15),4,math.pi/2)
        well(batch,(21,20))
        village_noticeboard(batch,(23,11))
        bench(batch,(18,20),math.pi/2)
        bench(batch,(25,20),-math.pi/2)
        bench(batch,(20,24),math.pi)
    rng=random.Random(20261018+(scene_id=="Hub_Village"))
    for x in range(0,int(w),2):
        for y in (.35,h-.35):
            if scene_id=="Farm_Core" and y<1 and 25<x<35:continue
            batch.box((x+.85,y,.35),(1.65,.65,.7),"stone")
    for y in range(2,int(h)-2,2):
        for x in (.35,w-.35):
            if scene_id=="Hub_Village" and x>w-1 and 12<y<18:continue
            batch.box((x,y+.85,.35),(.65,1.65,.7),"stone")
    # Trees and planted shrubs occupy perimeter gardens, never road rectangles.
    tree_locations=[(2.8,5),(2.8,h-4),(w-3,4),(w-3,h-4),(w*.30,h-2),(w*.63,h-2),(w-3,h*.57)]
    if scene_id=="Farm_Core":tree_locations += [(3,22),(57,22),(22,36),(39,36)]
    else:tree_locations += [(3,13),(20,28),(37,7)]
    for index,(x,y) in enumerate(tree_locations):
        height=rng.uniform(4.7,6.7)
        batch.cylinder((x,y,height*.35),.21,height*.70,"oak",10,.12)
        for k in range(4):
            angle=k*math.tau/4+.3
            q=(x+math.cos(angle)*.9,y+math.sin(angle)*.9,height*.72)
            batch.beam((x,y,height*.43),q,.13,"oak")
            batch.foliage((q[0],q[1],q[2]+.6),rng.uniform(1.2,1.7),"foliage_gold" if index%3==0 else "foliage_green",index*11+k)
    for index in range(220):
        x,y=rng.uniform(1,w-1),rng.uniform(1,h-1)
        near_building=any(r["bounds_xyz_m"][0][0]-1<x<r["bounds_xyz_m"][1][0]+1 and r["bounds_xyz_m"][0][1]-1<y<r["bounds_xyz_m"][1][1]+1 for r in records)
        if near_building or any(point_near_route(x,y,route) for route in roads):continue
        if index%4==0:batch.foliage((x,y,.35),rng.uniform(.25,.55),"foliage_olive",500+index)
        else:
            batch.cylinder((x,y,.16),.035,.32,"foliage_green",6)
            batch.foliage((x,y,.33),.10,"flowers",800+index)
    return batch.finish(collection)


def point_near_route(x,y,route):
    point=Vector((x,y))
    for a,b in zip(route["points"],route["points"][1:]):
        av,bv=Vector(a),Vector(b)
        delta=bv-av
        t=max(0,min(1,(point-av).dot(delta)/max(delta.length_squared,1e-9)))
        if (point-(av+delta*t)).length<route["width_m"]/2+.5:return True
    return False


def route_rect(a,b,width):
    if abs(a[0]-b[0])<1e-6:return (a[0]-width/2,min(a[1],b[1]),a[0]+width/2,max(a[1],b[1]))
    if abs(a[1]-b[1])<1e-6:return (min(a[0],b[0]),a[1]-width/2,max(a[0],b[0]),a[1]+width/2)
    raise ValueError("Acceptance routes must be axis-aligned")


def validation(scene_id,size,records,roads):
    collisions=[]
    for route in roads:
        if route["kind"]!="primary":continue
        for a,b in zip(route["points"],route["points"][1:]):
            x0,y0,x1,y1=route_rect(a,b,route["width_m"])
            for r in records:
                lo,hi=r["bounds_xyz_m"]
                overlap=min(x1,hi[0])-max(x0,lo[0])>.015 and min(y1,hi[1])-max(y0,lo[1])>.015
                if overlap:collisions.append({"route":route["name"],"asset":r["semantic"],"segment":[a,b]})
    checks={"native_asset_scale":all(r["scale"]==[1,1,1] for r in records),
            "all_structures_grounded":all(abs(r["bounds_xyz_m"][0][2])<.005 for r in records),
            "all_structures_inside_scene":all(r["bounds_xyz_m"][0][0]>=0 and r["bounds_xyz_m"][0][1]>=0 and r["bounds_xyz_m"][1][0]<=size[0] and r["bounds_xyz_m"][1][1]<=size[1] for r in records),
            "primary_routes_clear_of_structure_AABBs":not collisions,
            "every_facade_spur_connected_to_primary_lane":all(any(point_near_route(*r["points"][0],p) for p in roads if p["kind"]=="primary") for r in roads if r["kind"]=="facade_access")}
    if scene_id=="Farm_Core":checks["seven_core_roles"]=len(records)==7
    return {"status":"STATIC_PASS" if all(checks.values()) else "STATIC_FAIL","checks":checks,"route_structure_collisions":collisions,
            "limitations":["AABB check excludes access spurs terminating at facades", "Not a collider or navigation simulation", "No Unity integration acceptance", "Combined farm boundary and defense area ADR remain open"]}


def camera(name,location,target,ortho=None,lens=40):
    data=bpy.data.cameras.new(name)
    ob=bpy.data.objects.new(name,data)
    bpy.context.scene.collection.objects.link(ob)
    ob.location=location
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
    if ortho is not None:data.type="ORTHO";data.ortho_scale=ortho
    else:data.lens=lens
    data.clip_end=500
    return ob


def light_scene():
    world=bpy.data.worlds.new("warm_air")
    world.use_nodes=True
    world.node_tree.nodes["Background"].inputs[0].default_value=(.43,.55,.66,1)
    world.node_tree.nodes["Background"].inputs[1].default_value=.55
    bpy.context.scene.world=world
    sun=bpy.data.lights.new("late_afternoon_sun","SUN")
    sun.energy=2.3
    sun.angle=math.radians(12)
    sun.color=(1,.80,.56)
    obj=bpy.data.objects.new("late_afternoon_sun",sun)
    bpy.context.scene.collection.objects.link(obj)
    obj.rotation_euler=(math.radians(30),math.radians(-25),math.radians(-35))
    fill=bpy.data.lights.new("sky_fill","AREA")
    fill.energy=1800
    fill.shape="DISK"
    fill.size=35
    obj=bpy.data.objects.new("sky_fill",fill)
    bpy.context.scene.collection.objects.link(obj)
    obj.location=(30,5,25)


def render_scene(scene_id,directory,cameras,samples):
    scene=bpy.context.scene
    scene.render.engine="CYCLES"
    scene.cycles.samples=samples
    scene.cycles.use_denoising=True
    scene.render.threads_mode="FIXED"
    scene.render.threads=6
    scene.render.resolution_x=1600
    scene.render.resolution_y=1000
    scene.render.resolution_percentage=100
    scene.view_settings.view_transform="AgX"
    scene.render.image_settings.file_format="PNG"
    scene.render.film_transparent=False
    for name,cam in cameras.items():
        scene.camera=cam
        scene.render.filepath=str(directory/f"{scene_id}-{name}.png")
        print("Rendering",scene.render.filepath,flush=True)
        bpy.ops.render.render(write_still=True)
    clay=material("review_clay",(.58,.58,.54))
    scene.view_layers[0].material_override=clay
    scene.camera=cameras["overview"]
    scene.render.filepath=str(directory/f"{scene_id}-clay.png")
    bpy.ops.render.render(write_still=True)
    scene.view_layers[0].material_override=None


def export_fbx(scene_id,directory):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in bpy.context.scene.objects:
        if ob.type in {"MESH","EMPTY"}:ob.select_set(True)
    destination=directory/f"{scene_id}.fbx"
    bpy.ops.export_scene.fbx(filepath=str(destination),use_selection=True,object_types={"MESH","EMPTY"},
                             axis_forward="-Z",axis_up="Y",apply_unit_scale=True,global_scale=1.0,
                             path_mode="COPY",embed_textures=True,bake_anim=False,add_leaf_bones=False)
    if not destination.is_file() or destination.stat().st_size<1000:raise RuntimeError("FBX export missing or empty")
    receipt={"file":str(destination),"bytes":destination.stat().st_size,"sha256":hashlib.sha256(destination.read_bytes()).hexdigest(),
             "axis_forward":"-Z","axis_up":"Y","apply_unit_scale":True,"object_types":["MESH","EMPTY"],
             "cameras_exported":False,"lights_exported":False,"Unity_import_verified":False,
             "source_mesh_count":sum(ob.type=="MESH" for ob in bpy.context.selected_objects),
             "source_bounds_xyz_m":bounds(bpy.context.selected_objects)}
    (directory/"fbx-export.json").write_text(json.dumps(receipt,indent=2),encoding="utf-8")
    print(json.dumps(receipt),flush=True)


def verify_fbx(scene_id):
    directory=OUT/scene_id
    receipt=json.loads((directory/"fbx-export.json").read_text(encoding="utf-8"))
    manifest=json.loads((directory/"placement-manifest.json").read_text(encoding="utf-8"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(directory/f"{scene_id}.fbx"))
    meshes=[ob for ob in bpy.context.scene.objects if ob.type=="MESH"]
    imported_bounds=bounds(meshes)
    source_bounds=receipt.get("source_bounds_xyz_m")
    checks={"mesh_count_preserved":len(meshes)==receipt.get("source_mesh_count",len(meshes)),
            "bounds_preserved":source_bounds is not None and all(abs(imported_bounds[a][b]-source_bounds[a][b])<.01 for a in range(2) for b in range(3)),
            "no_cameras_lights":not any(ob.type in {"CAMERA","LIGHT"} for ob in bpy.context.scene.objects)}
    for name,position in manifest["spawn_points_blender_xyz"].items():
        ob=bpy.data.objects.get(name)
        checks["spawn_"+name]=ob is not None and (ob.location-Vector(position)).length<.01
    result={"status":"PASS" if all(checks.values()) else "FAIL","checks":checks,"roundtrip_bounds_xyz_m":imported_bounds,
            "roundtrip_mesh_count":len(meshes),"Unity_import_verified":False}
    (directory/"fbx-roundtrip.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
    print(json.dumps({"scene":scene_id,"fbx_roundtrip":result}),flush=True)
    if result["status"]!="PASS":raise RuntimeError("FBX roundtrip validation failed")


def markers(spawns):
    for name,position in spawns.items():
        ob=bpy.data.objects.get(name)
        if ob is None:
            ob=bpy.data.objects.new(name,None)
            bpy.context.scene.collection.objects.link(ob)
        ob.location=position
        ob.empty_display_type="ARROWS"
        ob.empty_display_size=.6
        ob["role"]="scene_transition_marker"


def assemble(scene_id,samples=24,skip_renders=False):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.name=scene_id
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1
    directory=OUT/scene_id
    directory.mkdir(parents=True,exist_ok=True)
    structures=bpy.data.collections.new(scene_id+"_Structures")
    environment=bpy.data.collections.new(scene_id+"_Environment")
    bpy.context.scene.collection.children.link(structures)
    bpy.context.scene.collection.children.link(environment)
    if scene_id=="Farm_Core":
        size=(60,40)
        placements=[("warehouse_C",(8,32),"warehouse_visual_C",0),("main_house",(30,32),"main_house",0),
                    ("research_laboratory",(50,32),"genetics_research",0),("construction_workshop",(8,12),"construction",0),
                    ("enhancement_atelier",(50,12),"enhancement",0),("medical_tent",(30,11),"medical",0),
                    ("information_complex",(30,21),"information_family",0)]
        roads=[{"name":"entry","kind":"primary","width_m":6,"points":[[30,0],[30,4]]},
               {"name":"southern_lane","kind":"primary","width_m":4,"points":[[17,4],[43,4]]},
               {"name":"west_lane","kind":"primary","width_m":4,"points":[[17,4],[17,25.8]]},
               {"name":"east_lane","kind":"primary","width_m":4,"points":[[43,4],[43,25.8]]},
               {"name":"north_lane","kind":"primary","width_m":4,"points":[[17,25.8],[43,25.8]]}]
        spawns={"spawn.core.from_farm":[30,0,0],"transition_to_farm":[30,-20,0]}
    else:
        size=(40,30)
        placements=[("hub_village_sanctum",(12,22),"BaSau_TamVa_household_cluster",0),
                    ("main_house",(30,23),"resident_house_visual",0),
                    ("warehouse_A",(9,6),"village_storage_visual",0),
                    ("construction_workshop",(29,7),"village_workshop_visual",math.pi)]
        roads=[{"name":"east_gate_village_lane","kind":"primary","width_m":4,"points":[[40,15],[12,15]]},
               {"name":"south_village_lane","kind":"primary","width_m":3,"points":[[20,15],[20,4]]}]
        spawns={"spawn.hub.from_farm":[40,15,0],"transition_to_farm":[40,15,0]}
    records=[]
    for asset,center,semantic,rotation in placements:
        record,objects=source_place(asset,center,structures,semantic,rotation)
        records.append(record)
    # Facade access spurs are explicit separate geometry, not claimed free of roofs.
    for r in records:
        lo,hi=r["bounds_xyz_m"]
        x=(lo[0]+hi[0])/2
        if scene_id=="Farm_Core":
            target=(x,lo[1]-1.2)
            candidates=[]
            for road in roads:
                if road["kind"]!="primary":continue
                for a,b in zip(road["points"],road["points"][1:]):
                    if min(a[1],b[1])>target[1]:continue
                    aa,bb=Vector((a[0],min(a[1],target[1]))),Vector((b[0],min(b[1],target[1])))
                    delta=bb-aa
                    t=max(0,min(1,(Vector(target)-aa).dot(delta)/max(delta.length_squared,1e-9)))
                    candidates.append(aa+delta*t)
            anchor=min(candidates,key=lambda p:(p-Vector(target)).length)
            roads.append({"name":"access_"+r["semantic"],"kind":"facade_access","width_m":2,
                          "points":[list(anchor),[x,anchor.y],[x,target[1]],[x,lo[1]]]})
        elif r["placement_xy_m"][1]>17:
            roads.append({"name":"access_"+r["semantic"],"kind":"facade_access","width_m":2,"points":[[x,15],[x,lo[1]]]})
        elif r["semantic"]=="village_workshop_visual":
            roads.append({"name":"access_workshop","kind":"facade_access","width_m":2,"points":[[x,15],[x,hi[1]]]})
        else:
            roads.append({"name":"access_storage","kind":"facade_access","width_m":2,"points":[[20,4],[15,4],[15,1],[x,1],[x,lo[1]]]})
    landscape(scene_id,size,roads,records,environment)
    markers(spawns)
    check=validation(scene_id,size,records,roads)
    manifest={"scene":scene_id,"status":"candidate_not_Unity_integrated","units":"metres","Blender_axes":"X=east,Y=north,Z=up",
              "Unity_mapping":"X=east,Y=up,Z=north; glTF importer axis conversion required", "bounds_playable_xy":[[0,0],list(size)],
              "asset_records":records,"routes":roads,"spawn_points_blender_xyz":spawns,
              "sources":["technical:word/document.xml:p00511","technical:word/document.xml:p00513","technical:word/document.xml:p00515","technical:word/document.xml:p00695","technical:word/document.xml:p00703"],
              "interpretation":"village is separate rear/NW vista with west Farm_Main gate; no same-diorama gameplay",
              "warehouse_visual_variant":"C is art proposal, not invented gameplay tier", "validation":check}
    (directory/"placement-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    if check["status"]!="STATIC_PASS":raise RuntimeError(json.dumps(check))
    bpy.ops.object.select_all(action="DESELECT")
    for collection in (structures,environment):
        for ob in collection.objects:ob.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(directory/f"{scene_id}.glb"),export_format="GLB",use_selection=True,export_apply=True)
    export_fbx(scene_id,directory)
    light_scene()
    if scene_id=="Farm_Core":
        cameras={"overview":camera("core_overview",(78,-66,62),(30,15,0),ortho=84),
                 "near-entry":camera("core_near",(36,4,7),(30,27,2.5),lens=31)}
    else:
        cameras={"overview":camera("village_overview",(56,-46,45),(20,15,0),ortho=55),
                 "near-square":camera("village_near",(23,12,4.7),(12,23,2),lens=31)}
    bpy.context.scene.camera=cameras["overview"]
    manifest["camera_poses_blender"]={name:{"location":list(cam.location),"rotation_euler_rad":list(cam.rotation_euler),
                                              "type":cam.data.type,"lens_mm":cam.data.lens,"ortho_scale":cam.data.ortho_scale}
                                     for name,cam in cameras.items()}
    (directory/"placement-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(directory/f"{scene_id}.blend"))
    if not skip_renders:render_scene(scene_id,directory,cameras,samples)
    check["outputs"]={path.name:path.stat().st_size for path in directory.iterdir() if path.is_file()}
    (directory/"validation.json").write_text(json.dumps(check,indent=2),encoding="utf-8")
    print(json.dumps({"scene":scene_id,"validation":check}),flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--inspect", action="store_true")
    parser.add_argument("--scene",choices=("Farm_Core","Hub_Village","both"),default="both")
    parser.add_argument("--samples",type=int,default=24)
    parser.add_argument("--skip-renders",action="store_true")
    parser.add_argument("--export-existing-fbx",action="store_true")
    parser.add_argument("--verify-fbx",action="store_true")
    parser.add_argument("--refresh-village-near",action="store_true")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    process_names = subprocess.check_output(["tasklist", "/FO", "CSV"], text=True, errors="replace")
    if any(line.startswith('"Unity.exe"') for line in process_names.splitlines()):
        raise SystemExit("Unity must stay closed during assembly")
    bpy.context.preferences.filepaths.use_scripts_auto_execute = False
    if args.refresh_village_near:
        directory=OUT/"Hub_Village"
        bpy.ops.wm.open_mainfile(filepath=str(directory/"Hub_Village.blend"),load_ui=False,use_scripts=False)
        cam=bpy.data.objects["village_near"]
        cam.location=(23,12,4.7)
        cam.rotation_euler=(Vector((12,23,2))-cam.location).to_track_quat('-Z','Y').to_euler()
        scene=bpy.context.scene
        scene.camera=cam
        scene.render.engine="CYCLES"
        scene.cycles.samples=args.samples
        scene.cycles.use_denoising=True
        scene.render.threads_mode="FIXED"
        scene.render.threads=6
        scene.render.resolution_x=1600
        scene.render.resolution_y=1000
        scene.render.resolution_percentage=100
        scene.view_settings.view_transform="AgX"
        scene.render.filepath=str(directory/"Hub_Village-near-square.png")
        bpy.ops.render.render(write_still=True)
        manifest=json.loads((directory/"placement-manifest.json").read_text(encoding="utf-8"))
        manifest["camera_poses_blender"]["near-square"]["location"]=list(cam.location)
        manifest["camera_poses_blender"]["near-square"]["rotation_euler_rad"]=list(cam.rotation_euler)
        (directory/"placement-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
        bpy.ops.wm.save_as_mainfile(filepath=str(directory/"Hub_Village.blend"))
    elif args.verify_fbx:
        for scene_id in ("Farm_Core","Hub_Village") if args.scene=="both" else (args.scene,):verify_fbx(scene_id)
    elif args.export_existing_fbx:
        for scene_id in ("Farm_Core","Hub_Village") if args.scene=="both" else (args.scene,):
            directory=OUT/scene_id
            bpy.ops.wm.open_mainfile(filepath=str(directory/f"{scene_id}.blend"),load_ui=False,use_scripts=False)
            source_manifest=json.loads((directory/"placement-manifest.json").read_text(encoding="utf-8"))
            markers(source_manifest["spawn_points_blender_xyz"])
            export_fbx(scene_id,directory)
    elif args.inspect:
        inspect()
    else:
        for scene in ("Farm_Core","Hub_Village") if args.scene=="both" else (args.scene,):
            assemble(scene,args.samples,args.skip_renders)


if __name__ == "__main__":
    main()
