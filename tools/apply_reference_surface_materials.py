"""Apply generated albedo materials to the real candidate, never approved sources."""
from pathlib import Path
import hashlib
import json
import shutil
import sys

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.collab/artifacts/reference-world-master-sol-high'
TEXTURES=OUT/'textures'
ORIGINALS=Path('D:/CodexHome/generated_images/01a14cfe-2d81-7d83-a693-615ea95ab2a4')
SOURCE_IMAGES={
 'weathered-rock-v1.png':'exec-f3007da2-9ac6-45e3-87cb-3360cdcedf9f.png',
 'golden-meadow-ground-v1.png':'exec-de46aa39-fb10-4380-8c07-de1501534f9c.png'}

def prepare_textures():
    TEXTURES.mkdir(parents=True,exist_ok=True)
    records=[]
    for name,original in SOURCE_IMAGES.items():
        source=ORIGINALS/original;destination=TEXTURES/name
        if not destination.exists():shutil.copy2(source,destination)
        assert destination.read_bytes()==source.read_bytes()
        records.append({'file':str(destination),'sha256':hashlib.sha256(destination.read_bytes()).hexdigest(),
                        'source':str(source),'tool':'built-in imagegen','purpose':'Albedo texture mapped on real 3D geometry; not a generated map render'})
    (TEXTURES/'provenance.json').write_text(json.dumps(records,indent=2),encoding='utf-8')

def image_node(nodes,links,path,scale=.3):
    import bpy
    coordinate=nodes.new('ShaderNodeNewGeometry')
    mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='SCALE';mapping.inputs[3].default_value=scale
    links.new(coordinate.outputs['Position'],mapping.inputs[0])
    image=nodes.new('ShaderNodeTexImage');image.image=bpy.data.images.load(str(path),check_existing=True)
    image.image.pack();image.projection='BOX';image.projection_blend=.25;image.extension='REPEAT'
    links.new(mapping.outputs['Vector'],image.inputs['Vector']);return image

def apply_materials(collection,terrain):
    import bpy
    import numpy as np
    from map_surface_contract import eval_road_mask
    prepare_textures()
    names=[]
    ground_materials=set(terrain.data.materials)
    for ob in collection.objects:
        if ob.type!='MESH' or not (ob.name.startswith('WCF_Zone09_Continuous') or ob.name.startswith('WCF_CliffToe') or ob.name.startswith('Zone02_Natural') or ob.get('surface_role')=='stone'):continue
        used_slots={polygon.material_index for polygon in ob.data.polygons}
        for slot in sorted(used_slots):
            material=ob.data.materials[slot]
            # Rim caps deliberately share the meadow shader. Never reclassify
            # that shared ground material as rock when refreshing components.
            if material in ground_materials or (ob.name.startswith('Zone02_Natural') and slot!=0):continue
            if material is None or material.get('generated_rock_albedo_applied'):continue
            if material.name.startswith('Natural_Rim_Top_Living_Ground'):continue
            material.use_nodes=True;nodes=material.node_tree.nodes;links=material.node_tree.links
            shader=nodes.get('Principled BSDF');texture=image_node(nodes,links,TEXTURES/'weathered-rock-v1.png',.34)
            links.new(texture.outputs['Color'],shader.inputs['Base Color'])
            bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.24;bump.inputs['Distance'].default_value=.036
            links.new(texture.outputs['Color'],bump.inputs['Height']);links.new(bump.outputs['Normal'],shader.inputs['Normal'])
            shader.inputs['Roughness'].default_value=.84;material['generated_rock_albedo_applied']=True;names.append(material.name)
    mesh=terrain.data;material=mesh.materials[0]
    if material.get('generated_ground_albedo_applied'):
        return {'rock_materials':names,'terrain_material':material.name,'already_applied':True,'approved_observatory_modified':False}
    nodes=material.node_tree.nodes;links=material.node_tree.links
    shader=nodes.get('Principled BSDF');old=shader.inputs['Base Color'].links[0].from_socket
    texture=image_node(nodes,links,TEXTURES/'golden-meadow-ground-v1.png',.28)
    # Real blades carry the silhouette. Restrain the underlay's high contrast so
    # it reads as living ground beneath them rather than a noisy straw photograph.
    underlay=nodes.new('ShaderNodeMixRGB');underlay.blend_type='MIX'
    underlay.inputs[0].default_value=.42;underlay.inputs[1].default_value=(.38,.26,.055,1)
    links.new(texture.outputs['Color'],underlay.inputs[2])
    mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX'
    positions=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',positions)
    positions=positions.reshape((-1,3));mask=mesh.attributes.get('Reference_WornTrail') or mesh.attributes.new('Reference_WornTrail','FLOAT','POINT')
    mask.data.foreach_set('value',np.asarray(eval_road_mask(positions[:,0],positions[:,1]),dtype=np.float32))
    attribute=nodes.new('ShaderNodeAttribute');attribute.attribute_name=mask.name
    links.new(attribute.outputs['Fac'],mix.inputs[0]);links.new(underlay.outputs['Color'],mix.inputs[1])
    links.new(old,mix.inputs[2]);links.new(mix.outputs['Color'],shader.inputs['Base Color'])
    material['generated_ground_albedo_applied']=True
    return {'rock_materials':names,'terrain_material':material.name,'approved_observatory_modified':False}

def main():
    sys.path.insert(0,str(ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'))
    sys.path.insert(0,str(ROOT/'Tools'))
    import bpy
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'),load_ui=False,use_scripts=False)
    collection=bpy.data.collections['ReferenceWorld_ArtPass']
    terrain=bpy.data.objects['Zone01_Terrain_GoldenMeadow_And_Paths']
    report=apply_materials(collection,terrain)
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'))
    (TEXTURES/'application.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report),flush=True)

if __name__=='__main__':main()
