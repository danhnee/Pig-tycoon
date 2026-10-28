"""Build and render an isolated Zone05 candidate without writing live assets.

Run with Blender --background --python-exit-code 1 --python this_file.py.
Optional arguments after --: --source <blend> --output <directory>.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import sys
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'Tools'))
from observatory_geometry import build_observatory, meshbin_bytes
from observatory_blender import create_observatory_material

ZONE = 'Zone05_Building_NhaNghienCuu_ResearchObservatory'
ORIGIN = (-7.8, 6.4, 3.323293447494507)


def mesh_hash(obj):
    v = np.empty(len(obj.data.vertices)*3, dtype=np.float32)
    obj.data.vertices.foreach_get('co', v)
    loops = np.empty(len(obj.data.loops), dtype=np.int32)
    obj.data.loops.foreach_get('vertex_index', loops)
    return hashlib.sha256(v.tobytes()+loops.tobytes()).hexdigest()


def look_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()


def render(scene, camera, path, location, target, size=900):
    camera.location=location
    look_at(camera,target)
    scene.camera=camera
    scene.render.resolution_x=size
    scene.render.resolution_y=size
    scene.render.resolution_percentage=100
    scene.render.filepath=str(path)
    print('RENDER',path,flush=True)
    bpy.ops.render.render(write_still=True)


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',type=Path,default=ROOT/'ArtSource/Blender_3D_Map/Farm_Main_3D_Master.blend')
    parser.add_argument('--output',type=Path,default=ROOT/'.collab/artifacts/zone05-20261018')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    out=args.output.resolve(); out.mkdir(parents=True,exist_ok=True)
    source_hash=hashlib.sha256(args.source.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(args.source))
    scene=bpy.context.scene
    obj=bpy.data.objects.get(ZONE)
    if obj is None: raise RuntimeError('The source master has no Zone05')
    other_hashes={o.name:mesh_hash(o) for o in scene.objects if o.type=='MESH' and o.name.startswith('Zone') and o!=obj}
    old_triangles=sum(len(p.vertices)-2 for p in obj.data.polygons)
    old_mesh_hash=mesh_hash(obj)
    old_camera=scene.camera
    old_resolution=(scene.render.resolution_x,scene.render.resolution_y)
    old_world=scene.world
    old_transform=scene.view_settings.view_transform
    old_hidden={o.name:o.hide_render for o in scene.objects}
    for o in scene.objects: o.hide_render=(o!=obj)
    # Collections may hide the object even if hide_render is false on the object.
    col_states={c.name:c.hide_render for c in bpy.data.collections}
    for c in obj.users_collection: c.hide_render=False
    stage=bpy.data.collections.new('Observatory_Review_Stage');scene.collection.children.link(stage)
    def stage_obj(name,data):
        o=bpy.data.objects.new(name,data);stage.objects.link(o);return o
    camera=stage_obj('ReviewCamera',bpy.data.cameras.new('ReviewCamera'))
    camera.data.type='ORTHO';camera.data.ortho_scale=5.6
    camera.data.clip_end=200
    for name,loc,energy,size in [('Key',(-4,-6,8),650,5),('Fill',(5,-1,5),280,4),('Rim',(1,5,7),500,3)]:
        light=bpy.data.lights.new(name,'AREA');light.energy=energy;light.shape='DISK';light.size=size
        o=stage_obj(name,light);o.location=Vector(ORIGIN)+Vector(loc);look_at(o,Vector(ORIGIN)+Vector((0,0,1.5)))
    world=bpy.data.worlds.new('Observatory_Review_World');world.use_nodes=True
    world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.32,.40,1)
    world.node_tree.nodes['Background'].inputs[1].default_value=.45
    scene.world=world
    scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.render.threads_mode='FIXED';scene.render.threads=6
    scene.view_settings.view_transform='AgX'
    floor=bpy.data.meshes.new('ReviewFloor');floor.from_pydata([(-200,-200,ORIGIN[2]-.25),(200,-200,ORIGIN[2]-.25),(200,200,ORIGIN[2]-.25),(-200,200,ORIGIN[2]-.25)],[],[(0,1,2,3)])
    floor_obj=stage_obj('ReviewFloor',floor)
    floor_mat=bpy.data.materials.new('ReviewFloor');floor_mat.diffuse_color=(.30,.28,.24,1);floor_obj.data.materials.append(floor_mat)
    target=Vector(ORIGIN)+Vector((0,0,1.65))
    front=Vector(ORIGIN)+Vector((6,-9,6.0))
    render(scene,camera,out/'01-before-front.png',front,target)
    detail=build_observatory()
    vertices,uvs,faces=detail.arrays(ORIGIN,yaw=-.05)
    assert np.isfinite(vertices).all() and np.isfinite(uvs).all()
    assert faces.min()>=0 and faces.max()<len(vertices)
    assert ((uvs>=0)&(uvs<=1)).all()
    tri=vertices[faces]; normals=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
    areas=np.linalg.norm(normals,axis=1)
    assert areas.min()>1e-9, f'Degenerate triangles: {int((areas<=1e-9).sum())}'
    local=vertices-np.asarray(ORIGIN)
    assert np.abs(local[:,0]).max()<1.9 and local[:,1].min()>-1.9 and local[:,1].max()<1.5
    assert local[:,2].min()>-.35 and local[:,2].max()<4.15
    mesh=bpy.data.meshes.new(ZONE+'_ReferenceDetail')
    mesh.from_pydata(vertices.tolist(),[],faces.tolist())
    layer=mesh.uv_layers.new(name='UVMap')
    layer.data.foreach_set('uv',uvs[faces.reshape(-1)].reshape(-1))
    assert not mesh.validate(verbose=True), 'Blender repaired invalid geometry'
    mesh.update();obj.data=mesh
    tex_dir=ROOT/'Assets/Textures/FarmMain3D'
    obj.data.materials.append(create_observatory_material(tex_dir/'FarmMain_3D_Albedo_4K.png',tex_dir/'FarmMain_3D_Emission_4K.png'))
    render(scene,camera,out/'02-after-front.png',front,target)
    for name,position in [('03-after-west',(-7,-6,5)),('04-after-rear',(6,9,6)),('05-after-rear-west',(-7,7,5))]:
        render(scene,camera,out/(name+'.png'),Vector(ORIGIN)+Vector(position),target)
    camera.data.ortho_scale=2.3
    render(scene,camera,out/'06-dome-close.png',Vector(ORIGIN)+Vector((3,-5,5.3)),Vector(ORIGIN)+Vector((0,.0,3.0)))
    # Persist the original atlas contract, with explicit face normals for Unity.
    binary=meshbin_bytes(vertices,uvs,faces)
    (out/(ZONE+'.meshbin')).write_bytes(binary)
    bpy.ops.object.select_all(action='DESELECT');obj.hide_set(False);obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.export_scene.gltf(filepath=str(out/'ResearchObservatory.glb'),export_format='GLB',use_selection=True)
    # Restore the real world before saving the candidate; stage never enters it.
    scene.camera=old_camera;scene.world=old_world
    scene.view_settings.view_transform=old_transform
    scene.render.resolution_x,scene.render.resolution_y=old_resolution
    for o in list(stage.objects): bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(stage)
    for name,hidden in old_hidden.items():
        if name in bpy.data.objects: bpy.data.objects[name].hide_render=hidden
    for name,hidden in col_states.items():
        if name in bpy.data.collections: bpy.data.collections[name].hide_render=hidden
    after_hashes={o.name:mesh_hash(o) for o in scene.objects if o.type=='MESH' and o.name in other_hashes}
    assert other_hashes==after_hashes, 'A zone outside Zone05 changed'
    scene.render.engine='BLENDER_EEVEE'
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'Farm_Main_3D_Zone05_Candidate.blend'))
    scene.render.resolution_x=1280;scene.render.resolution_y=960
    scene.render.filepath=str(out/'07-world-context.png');bpy.ops.render.render(write_still=True)
    report={'source':str(args.source),'source_sha256':source_hash,'baseline_zone05_sha256':old_mesh_hash,
            'old_triangles':old_triangles,'vertices':len(vertices),'triangles':len(faces),'minimum_double_triangle_area':float(areas.min()),
            'bounds_blender':[vertices.min(axis=0).tolist(),vertices.max(axis=0).tolist()],
            'other_zone_geometry_unchanged':other_hashes==after_hashes,'other_zone_hashes':after_hashes,
            'unity_runtime_verified':False,'parts':detail.parts,'meshbin_sha256':hashlib.sha256(binary).hexdigest()}
    (out/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('ZONE05_CANDIDATE_VALIDATED',json.dumps({k:v for k,v in report.items() if k not in ['parts','other_zone_hashes']}),flush=True)


if __name__=='__main__': main()
