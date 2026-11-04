"""Textured landscape dressing for Argon's Farm_Main art pass.

Only writes .collab/artifacts/farm-landscape-codex/. Existing FBX/PNG source kit
is read-only. Import this module and call DressKit.place/build_shoreline against
the receiving scene's actual terrain sampler; do not scale a finished diorama.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import random
import shutil
import sys

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/".collab/artifacts/farm-landscape-codex"
RUNTIME=ROOT/".collab/artifacts/mountain-reference-study-codex/runtime"
KIT=Path("C:/Users/Admin/.gemini/antigravity/brain/2526dd2a-9e91-4dbf-a3dc-12d822a774b4/scratch/external_3d_kits/Quaternius-Stylized-Nature")
FBX=KIT/"ultimate-stylized-nature/sources/FBX"
TEX=KIT/"ultimate-stylized-nature/sources/Textures"
sys.path.insert(0,str(RUNTIME))
sys.stdout.reconfigure(encoding="utf-8")
import bpy
from mathutils import Matrix, Vector


def bounds(objects):
    points=[ob.matrix_world@Vector(v) for ob in objects if ob.type=="MESH" for v in ob.bound_box]
    return [[min(v[a] for v in points) for a in range(3)],[max(v[a] for v in points) for a in range(3)]]


def texture_for(name):
    clean=name.split(".")[0]
    exact=TEX/f"{clean}.png"
    if exact.exists():return exact
    lower=clean.lower()
    if "rock" in lower:return TEX/"Rocks.png"
    if "grass" in lower:return TEX/"Grass.png"
    if "flower" in lower:return TEX/"Flowers.png"
    if "bush" in lower:return TEX/"Bush_Leaves.png"
    if "leave" in lower:return TEX/"NormalTree_Leaves.png"
    if "bark" in lower or "trunk" in lower:return TEX/"NormalTree_Bark.png"
    return None


def source_material(material):
    material.use_nodes=True
    nodes,links=material.node_tree.nodes,material.node_tree.links
    nodes.clear()
    out=nodes.new("ShaderNodeOutputMaterial")
    shader=nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Roughness"].default_value=.78
    links.new(shader.outputs["BSDF"],out.inputs["Surface"])
    path=texture_for(material.name)
    if path:
        texture_dir=OUT/"textures"
        texture_dir.mkdir(parents=True,exist_ok=True)
        local=texture_dir/path.name
        if not local.exists():shutil.copyfile(path,local)
        image=bpy.data.images.load(str(local),check_existing=True)
        image.pack()
        tex=nodes.new("ShaderNodeTexImage")
        tex.image=image
        links.new(tex.outputs["Color"],shader.inputs["Base Color"])
        if image.channels==4:
            links.new(tex.outputs["Alpha"],shader.inputs["Alpha"])
            material.surface_render_method="DITHERED"
        normal=path.with_name(path.stem+"_Normal.png")
        if normal.exists():
            local_normal=texture_dir/normal.name
            if not local_normal.exists():shutil.copyfile(normal,local_normal)
            normal_image=bpy.data.images.load(str(local_normal),check_existing=True)
            normal_image.colorspace_settings.name="Non-Color"
            normal_image.pack()
            normal_tex=nodes.new("ShaderNodeTexImage")
            normal_tex.image=normal_image
            normal_node=nodes.new("ShaderNodeNormalMap")
            normal_node.inputs["Strength"].default_value=.45
            links.new(normal_tex.outputs["Color"],normal_node.inputs["Color"])
            links.new(normal_node.outputs["Normal"],shader.inputs["Normal"])
    return str(path) if path else None


class DressKit:
    """Linked native source meshes; explicit per-asset uniform scale and provenance."""
    def __init__(self):
        self.templates={}
        self.records=[]
        self.sources={}
        self.template_collection=bpy.data.collections.new("SourceTemplates_DoNotExport")
        bpy.context.scene.collection.children.link(self.template_collection)
        self.template_collection.hide_render=True

    def load(self,name):
        if name in self.templates:return self.templates[name]
        path=FBX/f"{name}.fbx"
        before=set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(path))
        bpy.context.view_layer.update()
        imported=[ob for ob in bpy.data.objects if ob not in before and ob.type=="MESH"]
        # This local source pack stores vertices in Y-up centimetres. Maple's FBX
        # node rotates +90X correctly; Birch/Normal/grass nodes rotate 180X, which
        # Blender imports lying sideways. Normalize the verified source axis,
        # retaining each node's unit scale instead of trusting inconsistent nodes.
        source_matrices={ob:Matrix.Rotation(math.pi/2,4,"X") @ Matrix.Diagonal((*ob.matrix_world.to_scale(),1.0)) for ob in imported}
        source_points=[source_matrices[ob]@vertex.co for ob in imported for vertex in ob.data.vertices]
        bbox=[[min(p[a] for p in source_points) for a in range(3)],[max(p[a] for p in source_points) for a in range(3)]]
        offset=Vector(((bbox[0][0]+bbox[1][0])/2,(bbox[0][1]+bbox[1][1])/2,bbox[0][2]))
        material_paths=set()
        for ob in imported:
            mesh=ob.data.copy()
            matrix=source_matrices[ob]
            for vertex in mesh.vertices:vertex.co=matrix@vertex.co-offset
            ob.data=mesh
            ob.parent=None
            ob.rotation_mode="XYZ"
            ob.matrix_world=Matrix.Identity(4)
            for collection in list(ob.users_collection):collection.objects.unlink(ob)
            self.template_collection.objects.link(ob)
            for slot in ob.material_slots:
                if slot.material:
                    value=source_material(slot.material)
                    if value:material_paths.add(value)
        for ob in [ob for ob in bpy.data.objects if ob not in before and ob.type!="MESH"]:
            bpy.data.objects.remove(ob,do_unlink=True)
        height=max(.001,bbox[1][2]-bbox[0][2])
        self.templates[name]=(imported,height)
        self.sources[name]={"fbx":str(path),"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),
                            "original_dimensions_m":[bbox[1][a]-bbox[0][a] for a in range(3)],"textures":sorted(material_paths),
                            "axis_conversion":"verified local Y-up -> Blender Z-up; unit scale retained; inconsistent FBX node rotation normalized"}
        return self.templates[name]

    def place(self,name,xy,height_sampler,target_height,collection,seed=0,yaw=0,sink=.02):
        templates,native_height=self.load(name)
        scale=target_height/native_height
        z=float(height_sampler(*xy))-sink
        made=[]
        for index,template in enumerate(templates):
            ob=template.copy()
            ob.data=template.data
            ob.name=f"{name}_{seed:04d}_{index}"
            ob.rotation_mode="XYZ"
            ob.location=(xy[0],xy[1],z)
            ob.rotation_euler=(0,0,yaw)
            ob.scale=(scale,scale,scale)
            ob.hide_render=False
            ob.hide_viewport=False
            collection.objects.link(ob)
            ob["source_asset"]=name
            ob["visual_only_nonblocking"]=name.startswith(("Grass_","Flower_"))
            made.append(ob)
        self.records.append({"source":name,"xy_m":list(xy),"base_z_m":z,"uniform_scale":scale,
                             "target_height_m":target_height,"seed":seed,"collection":collection.name,
                             "visual_only_nonblocking":name.startswith(("Grass_","Flower_"))})
        return made


def collection(name):
    coll=bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)
    return coll


def build_shoreline(kit,points,height_sampler,target,seed=260118,excluded=lambda x,y:False,low_excluded=None):
    """Points are a closed shoreline; positive radial offsets point away from pond.

    Vegetation/rocks are grouped in patches, with deliberate open bank stretches.
    The receiving scene provides metres, shore points and terrain heights.
    """
    rng=random.Random(seed)
    cx=sum(p[0] for p in points)/len(points)
    cy=sum(p[1] for p in points)/len(points)
    made=[]
    for i,p in enumerate(points):
        radial=Vector((p[0]-cx,p[1]-cy)).normalized()
        # Unequal clusters create a shoreline rhythm rather than bead-like stones.
        cluster=(math.sin(i*.53)+math.cos(i*.17))*.5
        if cluster>.18 and not excluded(*p):
            xy=(p[0]+radial.x*rng.uniform(-.12,.42),p[1]+radial.y*rng.uniform(-.12,.42))
            made+=kit.place(f"Rock_{i%5+1}",xy,height_sampler,rng.uniform(.25,.8)*(1+.25*cluster),target,i,yaw=rng.random()*math.tau,sink=.06)
        for j in range(4 if cluster>-.2 else 2):
            distance=rng.uniform(.45,2.1)
            xy=(p[0]+radial.x*distance+rng.uniform(-.45,.45),p[1]+radial.y*distance+rng.uniform(-.45,.45))
            name="Grass_Large_Extruded" if j<2 else "Plant_1" if i%3==0 else "Grass_Small"
            skip=(low_excluded or excluded)(*xy) if name.startswith("Grass_") else excluded(*xy)
            if skip:continue
            height=rng.uniform(.13,.28) if excluded(*xy) else rng.uniform(.28,.68)
            made+=kit.place(name,xy,height_sampler,height,target,1000+i*5+j,yaw=rng.random()*math.tau,sink=.025)
        if i%7==2:
            xy=(p[0]+radial.x*1.8,p[1]+radial.y*1.8)
            if not (low_excluded or excluded)(*xy):
                made+=kit.place("Flower_2_Clump",xy,height_sampler,.22 if excluded(*xy) else .42,target,3000+i,yaw=rng.random()*math.tau)
        if i%11==5:
            xy=(p[0]+radial.x*.40,p[1]+radial.y*.40)
            if not excluded(*xy):made+=kit.place("Plant_2",xy,height_sampler,.85,target,4000+i,yaw=rng.random()*math.tau)
    return made


def build_meadow_groundcover(kit,surface,target,excluded=lambda x,y:False,seed=20261018,attempts=2400):
    """Clustered short, explicitly nonblocking source grass; art density only.

    Keep worn trail centres and water clear, not all of the six-metre navigable
    grass corridor. The caller still supplies doors/pads/critical-readability gaps.
    """
    rng=random.Random(seed)
    made=[]
    for i in range(attempts):
        x,y=rng.uniform(2,118),rng.uniform(2,88)
        if excluded(x,y) or float(surface.nearest_trail(x,y))<.8:continue
        if any(float(surface.pond_distance(x,y,p))<1.05 for p in surface.PONDS):continue
        cover=.5+.24*math.sin(x*.19+y*.11)+.18*math.cos(y*.31-x*.08)
        if rng.random()>cover:continue
        name="Flower_1_Clump" if i%17==0 else "Grass_Small" if i%3 else "Grass_Large_Extruded"
        made+=kit.place(name,(x,y),surface.sample_ground_z,rng.uniform(.13,.30),target,
                        20000+i,yaw=rng.random()*math.tau,sink=.018)
    return made


def shape(theta):return 1+.075*math.sin(3*theta+.4)+.035*math.cos(5*theta-.2)


def sample_height(x,y):
    theta=math.atan2(y/3.7,x/6.2)
    radial=math.hypot(x/6.2,y/3.7)/shape(theta)
    return .48*math.tanh((radial-1)*3)+.10*math.sin(x*.7)*math.cos(y*.8)*min(1,max(0,radial-1))


def preview_ground(coll):
    vv,faces=[],[]
    nx,ny=200,180
    for j in range(ny):
        v=-1+2*j/(ny-1)
        y=60*math.copysign(abs(v)**1.8,v)
        for i in range(nx):
            u=-1+2*i/(nx-1)
            x=60*math.copysign(abs(u)**1.8,u)
            vv.append((x,y,sample_height(x,y)))
    for j in range(ny-1):
        for i in range(nx-1):
            a=j*nx+i
            faces.append((a,a+1,a+1+nx,a+nx))
    me=bpy.data.meshes.new("continuous_preview_terrain")
    me.from_pydata(vv,[],faces)
    me.update()
    ob=bpy.data.objects.new("PreviewOnly_ContinuousTerrain",me)
    coll.objects.link(ob)
    color=me.color_attributes.new(name="Col",type="FLOAT_COLOR",domain="POINT")
    for i,(x,y,z) in enumerate(vv):
        t=max(0,min(1,(z+.02)/.28))
        mud=(.25,.20,.115)
        green=(.20,.30,.095)
        color.data[i].color=tuple(mud[a]*(1-t)+green[a]*t for a in range(3))+(1,)
    mat=bpy.data.materials.new("bank_soil_grass_transition")
    mat.use_nodes=True
    nodes,links=mat.node_tree.nodes,mat.node_tree.links
    bsdf=nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value=.95
    vc=nodes.new("ShaderNodeVertexColor")
    vc.layer_name="Col"
    links.new(vc.outputs["Color"],bsdf.inputs["Base Color"])
    tex=nodes.new("ShaderNodeTexNoise")
    tex.inputs["Scale"].default_value=7
    tex.inputs["Detail"].default_value=3
    bump=nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value=.22
    bump.inputs["Distance"].default_value=.05
    links.new(tex.outputs["Fac"],bump.inputs["Height"])
    links.new(bump.outputs["Normal"],bsdf.inputs["Normal"])
    me.materials.append(mat)
    return ob


def water(coll):
    vertices=[(0,0,-.005)]
    count=128
    vertices += [(6.2*shape(t)*math.cos(t),3.7*shape(t)*math.sin(t),-.005) for t in (i*math.tau/count for i in range(count))]
    faces=[(0,1+i,1+(i+1)%count) for i in range(count)]
    me=bpy.data.meshes.new("pond_water")
    me.from_pydata(vertices,[],faces)
    me.update()
    ob=bpy.data.objects.new("PreviewOnly_PondWater",me)
    coll.objects.link(ob)
    mat=bpy.data.materials.new("teal_shallow_water")
    mat.use_nodes=True
    nodes,links=mat.node_tree.nodes,mat.node_tree.links
    bsdf=nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value=(.045,.20,.18,1)
    bsdf.inputs["Roughness"].default_value=.15
    bsdf.inputs["Metallic"].default_value=.15
    bsdf.inputs["Transmission Weight"].default_value=.22
    bsdf.inputs["IOR"].default_value=1.333
    noise=nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value=2.4
    noise.inputs["Detail"].default_value=2
    bump=nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value=.17
    bump.inputs["Distance"].default_value=.035
    links.new(noise.outputs["Fac"],bump.inputs["Height"])
    links.new(bump.outputs["Normal"],bsdf.inputs["Normal"])
    me.materials.append(mat)
    return ob


def export(coll,name):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in coll.objects:ob.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/f"{name}.glb"),export_format="GLB",use_selection=True,export_apply=True,export_extras=True)
    originals={ob:ob.data for ob in coll.objects if ob.type=="MESH"}
    copies=[]
    try:
        for ob,mesh in originals.items():
            slot_materials=[slot.material for slot in ob.material_slots]
            ob.data=mesh.copy()
            copies.append(ob.data)
            for slot,mat in zip(ob.material_slots,slot_materials):
                slot.link="DATA"
                slot.material=mat
        bpy.ops.export_scene.fbx(filepath=str(OUT/f"{name}.fbx"),use_selection=True,object_types={"MESH"},axis_forward="-Z",axis_up="Y",
                                 apply_unit_scale=True,path_mode="COPY",embed_textures=True,bake_anim=False,use_custom_props=True)
    finally:
        for ob,mesh in originals.items():ob.data=mesh
        for mesh in copies:bpy.data.meshes.remove(mesh)


def setup_render(samples):
    scene=bpy.context.scene
    scene.render.engine="CYCLES"
    scene.cycles.samples=samples
    scene.cycles.use_denoising=True
    scene.cycles.transparent_max_bounces=16
    scene.render.threads_mode="FIXED"
    scene.render.threads=6
    scene.render.resolution_x=1800
    scene.render.resolution_y=1100
    scene.render.resolution_percentage=100
    scene.view_settings.view_transform="AgX"
    scene.world=bpy.data.worlds.new("warm_clear_sky")
    scene.world.use_nodes=True
    scene.world.node_tree.nodes["Background"].inputs[0].default_value=(.55,.69,.85,1)
    scene.world.node_tree.nodes["Background"].inputs[1].default_value=.5
    sun=bpy.data.lights.new("sunset_sun","SUN")
    sun.energy=2.8
    sun.angle=.15
    sun.color=(1,.76,.45)
    ob=bpy.data.objects.new("sunset_sun",sun)
    scene.collection.objects.link(ob)
    ob.rotation_euler=(.40,-.65,-.4)
    cam=bpy.data.objects.new("shore_review_camera",bpy.data.cameras.new("shore_review_camera"))
    scene.collection.objects.link(cam)
    scene.camera=cam
    return cam


def render(cam,name,eye,target,lens):
    cam.location=eye
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.lens=lens
    cam.data.clip_end=500
    bpy.context.scene.render.filepath=str(OUT/name)
    bpy.ops.render.render(write_still=True)


def verify_artifacts():
    manifest=json.loads((OUT/"dressing-manifest.json").read_text(encoding="utf-8"))
    records=[]
    for name,expected in manifest["exports"].items():
        bpy.ops.wm.read_factory_settings(use_empty=True)
        path=OUT/f"{name}.fbx"
        bpy.ops.import_scene.fbx(filepath=str(path))
        bpy.context.view_layer.update()
        meshes=[ob for ob in bpy.context.scene.objects if ob.type=="MESH"]
        actual=bounds(meshes)
        materials={slot.material for ob in meshes for slot in ob.material_slots if slot.material}
        without_image=[mat.name for mat in materials if not any(node.type=="TEX_IMAGE" and node.image for node in mat.node_tree.nodes)]
        checks={"nonempty_meshes":bool(meshes),"bounds_preserved":all(abs(actual[a][b]-expected[a][b])<.015 for a in range(2) for b in range(3)),
                "all_meshes_keep_uv":all(ob.data.uv_layers for ob in meshes),"all_materials_keep_images":not without_image}
        records.append({"asset":name,"checks":checks,"mesh_count":len(meshes),"material_count":len(materials),
                        "materials_without_image":without_image,"roundtrip_bounds_m":actual,
                        "fbx_sha256":hashlib.sha256(path.read_bytes()).hexdigest()})
    result={"status":"EXPORT_PASS" if all(all(row["checks"].values()) for row in records) else "EXPORT_FAIL",
            "assets":records,"full_Farm_Main_visual_acceptance":False,"Unity_runtime_verified":False}
    (OUT/"export-verification.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
    print(json.dumps(result),flush=True)
    if result["status"]!="EXPORT_PASS":raise RuntimeError("Dressing export verification failed")


def build_farm_banks(surface,kit=None,target=None):
    """Use the live root-owned surface API; no duplicate pond constants."""
    kit=kit or DressKit()
    target=target or collection("Dressing_FarmMain_ActualPondBanks")
    def excluded(x,y):
        return float(surface.nearest_trail(x,y))<3 or not (18<x<102 and 18<y<78)
    for i,pond in enumerate(surface.PONDS):
        points=surface.shoreline(pond,72).tolist()
        build_shoreline(kit,points,surface.sample_ground_z,target,seed=260118+i*173,excluded=excluded)
    return kit,target


def farm_banks(samples,with_groundcover=False):
    path=ROOT/"Tools/document_farm_surface.py"
    spec=importlib.util.spec_from_file_location("farm_surface_owner",path)
    surface=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(surface)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.use_scripts_auto_execute=False
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1
    kit,target=build_farm_banks(surface)
    groundcover=None
    if with_groundcover:
        groundcover=collection("Dressing_Meadow_Nonblocking")
        def keepout(x,y):return not(18<x<102 and 18<y<64)
        build_meadow_groundcover(kit,surface,groundcover,excluded=keepout,attempts=4500)
        bpy.ops.object.select_all(action="DESELECT")
        objects=list(groundcover.objects)
        for ob in objects:ob.select_set(True)
        if objects:
            bpy.context.view_layer.objects.active=objects[0]
            bpy.ops.object.join()
            joined=bpy.context.object
            joined.name="Meadow_Nonblocking_Groundcover"
            joined["visual_only_nonblocking"]=True
            joined["source_asset"]="multiple native grass/flower meshes; see manifest"
            export(groundcover,"farm-meadow-groundcover")
    bpy.context.view_layer.update()
    export(target,"farm-pond-banks")
    manifest={"status":"world_positioned_component_not_full_Farm_Main_visual_acceptance",
              "surface_module":str(path),"surface_sha256":hashlib.sha256(path.read_bytes()).hexdigest(),
              "ponds":list(surface.PONDS),"sources":kit.sources,"placements":kit.records,
              "bounds_world_m":bounds(list(target.objects)),"mesh_count":len(target.objects),
              "excluded":"nearest_trail<3m and outside pasture18<x<102,18<y<78",
              "base_contact":"positionZ=actual surface.sample_ground_z(x,y)-small root embed",
              "import_transform":"identity; these exports already use Farm_Main world metres, do not rescale or recenter"}
    if groundcover:
        manifest["groundcover"]={"file":"farm-meadow-groundcover.glb","mesh_count_after_batch":len(groundcover.objects),
                                 "collision_policy":"visual_only_nonblocking; no colliders",
                                 "placements":sum(r["collection"]==groundcover.name for r in kit.records)}
    (OUT/"farm-banks-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    preview=ROOT/".collab/artifacts/farm-surface-codex/organic-farm-surface.glb"
    if preview.exists():
        before=set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=str(preview))
        for ob in bpy.data.objects:
            if ob not in before:ob.name="PreviewOnly_"+ob.name
    cam=setup_render(samples)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"farm-pond-banks.blend"))
    render(cam,"03-farm-bank-integration-detail.png",(83,12,21),(59,41,1.2),44)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"farm-pond-banks.blend"))
    print(json.dumps({"output":str(OUT/"farm-pond-banks.glb"),"mesh_count":len(target.objects),
                      "surface_sha256":manifest["surface_sha256"]}),flush=True)


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--samples",type=int,default=32)
    parser.add_argument("--verify",action="store_true")
    parser.add_argument("--inspect-sources",action="store_true")
    parser.add_argument("--farm-banks",action="store_true")
    parser.add_argument("--with-groundcover",action="store_true")
    args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    if args.farm_banks:
        farm_banks(args.samples,args.with_groundcover)
        return
    if args.inspect_sources:
        manifest=json.loads((OUT/"dressing-manifest.json").read_text(encoding="utf-8"))
        print(json.dumps({name:row["original_dimensions_m"] for name,row in manifest["sources"].items()}),flush=True)
        for name in ("MapleTree_2","MapleTree_4","Plant_1","Rock_1","Bush_Small"):
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.fbx(filepath=str(FBX/f"{name}.fbx"))
            bpy.context.view_layer.update()
            records=[]
            for ob in bpy.context.scene.objects:
                if ob.type=="MESH":records.append({"name":ob.name,"matrix":list(map(list,ob.matrix_world)),
                                                  "local_bounds":list(map(list,ob.bound_box)),"mode":ob.rotation_mode})
            print(json.dumps({"source":name,"objects":records}),flush=True)
        return
    if args.verify:
        verify_artifacts()
        return
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.use_scripts_auto_execute=False
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1
    kit=DressKit()
    shore=collection("Dressing_Shoreline_Rocks_Grass_Reeds")
    grove=collection("Dressing_Native_Broadleaf_Grove")
    demo=collection("PreviewOnly_Ground_And_Water")
    points=[(6.2*shape(t)*math.cos(t),3.7*shape(t)*math.sin(t)) for t in (i*math.tau/72 for i in range(72))]
    build_shoreline(kit,points,sample_height,shore)
    for i,(name,x,y,height) in enumerate([("MapleTree_2",-8,4.5,7.8),("MapleTree_4",-6.5,8.2,8.8),
                                        ("NormalTree_3",8.6,6.6,7.5),("BirchTree_2",5.2,9.0,8.6),
                                        ("Bush_Large_Flowers",-7.1,1.5,1.2),("Bush_Small",7.8,2,1.1)]):
        kit.place(name,(x,y),sample_height,height,grove,8000+i,yaw=i*.7)
    preview_ground(demo)
    water(demo)
    for image in list(bpy.data.images):
        if image.users==0:bpy.data.images.remove(image)
    bpy.context.view_layer.update()
    export(shore,"pond-bank-dressing")
    export(grove,"broadleaf-grove-dressing")
    cam=setup_render(args.samples)
    manifest={"status":"dressing_asset_candidate_not_full_Farm_Main_acceptance","source_kit":str(KIT),
              "sources":kit.sources,"placements":kit.records,"shoreline_points_local_m":points,
              "height_contract":"Caller must supply actual terrain height_sampler; all source FBXs normalized at baseZ0, uniform element scaling only.",
              "exports":{"pond-bank-dressing":bounds(list(shore.objects)),"broadleaf-grove-dressing":bounds(list(grove.objects))},
              "preview_only":["PreviewOnly_ContinuousTerrain","PreviewOnly_PondWater"],
              "forbidden_promotion":"Do not present this component study as a completed Farm_Main scene.",
              "attribution":"Existing local Quaternius Stylized Nature source pack; bundled repository LICENSE preserved verbatim."}
    shutil.copyfile(KIT/"LICENSE",OUT/"SOURCE_LICENSE.txt")
    (OUT/"dressing-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"pond-bank-dressing-study.blend"))
    render(cam,"01-shoreline-beauty.png",(14,-17,10.5),(0,1.2,.5),42)
    render(cam,"02-bank-closeup.png",(4,-9,3.0),(0,-2.4,.4),48)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"pond-bank-dressing-study.blend"))
    print(json.dumps({"output":str(OUT),"source_assets":len(kit.sources),"instances":len(kit.records),
                      "shore_meshes":len(shore.objects),"grove_meshes":len(grove.objects)}),flush=True)


if __name__=="__main__":main()
