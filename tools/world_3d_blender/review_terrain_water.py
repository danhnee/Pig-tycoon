"""Render and export the terrain/water candidate atop the reviewed observatory master."""
from pathlib import Path
import hashlib,json,sys
import bpy
import numpy as np
from mathutils import Vector

ROOT=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(ROOT/'Tools'))
from terrain_water_geometry import build_terrain,build_water
from map_mesh import meshbin_bytes
from map_materials import create_atlas_material
from review_observatory import mesh_hash
from validate_terrain_water_geometry import validate


def main():
    validation=validate()
    source=ROOT/'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/Farm_Main_3D_Zone05_Candidate.blend'
    out=ROOT/'.collab/artifacts/zone01-03-20261018';out.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene;original_camera=scene.camera
    names={1:'Zone01_Terrain_GoldenMeadow_And_Paths',3:'Zone03_Water_River_Waterfalls_And_3Ponds'}
    unrelated={o.name:mesh_hash(o) for o in scene.objects if o.type=='MESH' and o.name.startswith('Zone') and o.name not in names.values()}
    camera=bpy.data.objects.new('SurfaceReviewCamera',bpy.data.cameras.new('SurfaceReviewCamera'));scene.collection.objects.link(camera)
    camera.data.type='PERSP';camera.data.lens=42;camera.data.clip_end=300
    scene.render.engine='BLENDER_EEVEE'
    scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
    def render(filename,position,target,override=None):
        scene.camera=camera;camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.view_layers[0].material_override=override
        scene.render.filepath=str(out/filename);print('RENDER',filename,flush=True);bpy.ops.render.render(write_still=True)
    pond_position=(5,-15,11);pond_target=(-3.4,-5.2,2.34)
    fall_position=(23,1,12);fall_target=(13,11.5,4.3)
    render('01-before-pond.png',pond_position,pond_target)
    render('02-before-waterfalls.png',fall_position,fall_target)
    texture=ROOT/'Assets/Textures/FarmMain3D';albedo=texture/'FarmMain_3D_Albedo_4K.png';emission=texture/'FarmMain_3D_Emission_4K.png'
    reports={}
    for zone,builder in [(1,build_terrain),(3,build_water)]:
        model=builder();vertices,uvs,faces=model.arrays();obj=bpy.data.objects[names[zone]]
        mesh=bpy.data.meshes.new(names[zone]+'_ReferenceDetail');mesh.from_pydata(vertices.tolist(),[],faces.tolist())
        uv=mesh.uv_layers.new(name='UVMap');uv.data.foreach_set('uv',uvs[faces.reshape(-1)].reshape(-1))
        assert not mesh.validate(verbose=True)
        for polygon in mesh.polygons:polygon.use_smooth=True
        mesh.update();obj.data=mesh
        material=create_atlas_material('Mat_Reference_Terrain' if zone==1 else 'Mat_Reference_Water',albedo,emission,
                                       roughness=.85 if zone==1 else .20,emission_strength=.03 if zone==1 else .04,metallic=0 if zone==1 else .12)
        mesh.materials.append(material)
        binary=meshbin_bytes(vertices,uvs,faces);(out/(names[zone]+'.meshbin')).write_bytes(binary)
        reports[str(zone)]={'vertices':len(vertices),'triangles':len(faces),'parts':len(model.parts),'meshbin_sha256':hashlib.sha256(binary).hexdigest()}
    render('03-after-pond.png',pond_position,pond_target)
    render('04-after-waterfalls.png',fall_position,fall_target)
    render('05-after-harbor.png',(36,-5,11),(19,4,2.))
    clay=bpy.data.materials.new('SurfaceProofClay');clay.use_nodes=True
    clay.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.42,.44,.48,1)
    clay.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8
    render('06-clay-pond.png',pond_position,pond_target,clay)
    wire=clay.copy();node=wire.node_tree.nodes.new('ShaderNodeWireframe');node.use_pixel_size=True;node.inputs['Size'].default_value=.6
    mix=wire.node_tree.nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.65,.68,.72,1);mix.inputs[2].default_value=(.015,.02,.03,1)
    wire.node_tree.links.new(node.outputs['Fac'],mix.inputs[0]);wire.node_tree.links.new(mix.outputs[0],wire.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    render('07-wireframe-waterfalls.png',fall_position,fall_target,wire)
    scene.view_layers[0].material_override=None;scene.camera=original_camera
    bpy.data.objects.remove(camera,do_unlink=True)
    after={o.name:mesh_hash(o) for o in scene.objects if o.name in unrelated}
    assert after==unrelated,'An unrelated zone changed'
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'Farm_Main_Surface_Candidate.blend'))
    scene.render.filepath=str(out/'08-world-context.png');bpy.ops.render.render(write_still=True)
    reports['validation']=validation;reports['other_zone_geometry_unchanged']=True;reports['source']=str(source)
    reports['source_sha256']=hashlib.sha256(source.read_bytes()).hexdigest();reports['unity_runtime_verified']=False
    (out/'validation.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
    print('TERRAIN_WATER_CANDIDATE_VALIDATED',json.dumps(reports),flush=True)


if __name__=='__main__':main()
