"""Export the approved geometry candidate as editable components at local origin."""
from pathlib import Path
from collections import defaultdict
import hashlib,json,sys,shutil
import bpy
import numpy as np
from mathutils import Vector

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Tools'))
from observatory_geometry import build_observatory, meshbin_bytes
from observatory_blender import create_observatory_material


def main():
    candidate=ROOT/'.collab/artifacts/zone05-20261018'
    report=json.loads((candidate/'validation.json').read_text())
    name='Zone05_Building_NhaNghienCuu_ResearchObservatory.meshbin'
    assert hashlib.sha256((candidate/name).read_bytes()).hexdigest()==report['meshbin_sha256']
    out=ROOT/'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail';out.mkdir(parents=True,exist_ok=True)
    model=build_observatory();verts,uvs,faces=model.arrays()
    world_arrays=model.arrays((-7.8,6.4,3.323293447494507),yaw=-.05)
    assert hashlib.sha256(meshbin_bytes(*world_arrays)).hexdigest()==report['meshbin_sha256'], 'Source geometry changed after candidate validation'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    tex=ROOT/'Assets/Textures/FarmMain3D'
    material=create_observatory_material(tex/'FarmMain_3D_Albedo_4K.png',tex/'FarmMain_3D_Emission_4K.png')
    collection=bpy.data.collections.new('ResearchObservatory_EditableParts');bpy.context.scene.collection.children.link(collection)
    groups=defaultdict(list)
    for part in model.parts:
        start=part['first_triangle'];end=start+part['triangles']
        groups[part['name']].extend(faces[start:end].reshape(-1))
    for name_key,indices in groups.items():
        indices=np.asarray(indices,dtype=np.int32)
        mesh=bpy.data.meshes.new(name_key)
        mesh.from_pydata(verts[indices].tolist(),[],np.arange(len(indices)).reshape(-1,3).tolist())
        layer=mesh.uv_layers.new(name='UVMap');layer.data.foreach_set('uv',uvs[indices].reshape(-1))
        mesh.update();obj=bpy.data.objects.new(name_key,mesh);collection.objects.link(obj);obj.data.materials.append(material)
    scene=bpy.context.scene
    cam=bpy.data.objects.new('ReviewCamera',bpy.data.cameras.new('ReviewCamera'));scene.collection.objects.link(cam)
    cam.location=(6,-9,6);cam.rotation_euler=(Vector((0,0,1.65))-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=5.6;scene.camera=cam
    for name_key,position,energy,size in [('Key',(-4,-6,8),650,5),('Fill',(5,-1,5),280,4),('Rim',(1,5,7),500,3)]:
        light=bpy.data.lights.new(name_key,'AREA');light.energy=energy;light.size=size
        obj=bpy.data.objects.new(name_key,light);scene.collection.objects.link(obj);obj.location=position
        obj.rotation_euler=(Vector((0,0,1.5))-obj.location).to_track_quat('-Z','Y').to_euler()
    scene.world=bpy.data.worlds.new('ReviewWorld');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.32,.40,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
    scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.view_settings.view_transform='AgX';scene.render.resolution_x=900;scene.render.resolution_y=900
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=next(iter(collection.objects))
    bpy.ops.export_scene.gltf(filepath=str(out/'ResearchObservatory.glb'),export_format='GLB',use_selection=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'ResearchObservatory_Editable.blend'))
    for file in [name,'validation.json','Farm_Main_3D_Zone05_Candidate.blend']:
        shutil.copy2(candidate/file,out/file)
    report['editable_object_groups']=len(groups)
    report['source_geometry_sha256']=hashlib.sha256(verts.tobytes()+uvs.tobytes()+faces.tobytes()).hexdigest()
    report['files']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in out.iterdir() if p.is_file() and p.name!='package-manifest.json'}
    (out/'package-manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('SOURCE_PACKAGE_EXPORTED',json.dumps({'path':str(out),'editable_groups':len(groups),'triangles':len(faces)}),flush=True)


if __name__=='__main__':main()
