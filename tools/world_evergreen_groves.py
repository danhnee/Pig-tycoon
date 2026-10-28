"""Native textured evergreen contrast; world-positioned art candidate only."""
from pathlib import Path
import argparse
import json
import math
import random
import sys

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Tools'))
import farm_landscape_dressing_codex as dressing
from farm_landscape_dressing_codex import bpy,DressKit,bounds
from map_surface_contract import sample_ground_z,island_signed_distance,eval_road_mask,eval_pond_distances,eval_river_center_and_width

OUT=ROOT/'.collab/artifacts/world-evergreen-groves-sol-high'
CONTEXT=ROOT/'.collab/artifacts/reference-world-master-sol-high/reference-world-art-candidate.blend'


def _pine_texture(name):
    clean=name.split('.')[0]
    exact=dressing.TEX/(clean+'.png')
    if exact.exists() and clean.startswith('PineTree_'):return exact
    lower=name.lower()
    if any(s in lower for s in ('leave','needle','foliage')):return dressing.TEX/'PineTree_Leaves.png'
    if any(s in lower for s in ('bark','trunk','wood')):return dressing.TEX/'PineTree_Bark.png'
    return None


def _safe(x,y):
    if float(island_signed_distance(x,y))>.92:return False
    if float(eval_road_mask(x,y))>.12:return False
    if min(float(v) for v in eval_pond_distances(x,y))<1.5:return False
    river,width=eval_river_center_and_width(y)
    if -8.8<y<13 and abs(x-float(river))<float(width)+1.4:return False
    # Existing building/door/courtyard and castle/harbor footprints stay clear.
    if -27<x<5 and -3.8<y<11.5:return False
    if x>3.6 and y>4.2:return False
    return True


def build_world_evergreen_groves(collection):
    """Return {'objects': [...], 'report': {...}}; add only native pine instances."""
    OUT.mkdir(parents=True,exist_ok=True)
    old_out,old_texture=dressing.OUT,dressing.texture_for
    dressing.OUT=OUT;dressing.texture_for=_pine_texture
    kit=DressKit();made=[];accepted=[]
    existing=set()
    for ob in bpy.context.scene.objects:
        if ob.hide_render:continue
        asset=str(ob.get('source_asset',''))
        if asset.startswith(('MapleTree','BirchTree','NormalTree','PineTree')):
            existing.add((round(ob.location.x,3),round(ob.location.y,3)))
    rng=random.Random(260119)
    groups=[(-23,12.5,6,False),(-18,14.2,6,False),(-13,15.3,6,False),
            (-7,14.8,5,False),(-1,13.8,5,False),
            (-23,-11,2,True),(-18,-16,2,True),(16,-13,2,True)]
    try:
        for cx,cy,target,front in groups:
            count=0
            for attempt in range(160):
                if count>=target:break
                x,y=cx+rng.gauss(0,1.6),cy+rng.gauss(0,.95)
                if not _safe(x,y):continue
                if any((x-px)**2+(y-py)**2<1.35**2 for px,py in accepted):continue
                if any((x-px)**2+(y-py)**2<1.05**2 for px,py in existing):continue
                index=len(accepted);name=f'PineTree_{index%5+1}'
                height=rng.uniform(1.8,2.8) if front else rng.uniform(3.15,4.85)
                made+=kit.place(name,(x,y),sample_ground_z,height,collection,seed=51000+index,
                                yaw=rng.random()*math.tau,sink=.055)
                accepted.append((x,y));count+=1
        # Keep native needle alpha/UV; tint actual pine texture for dark value contrast.
        for mat in {slot.material for ob in made for slot in ob.material_slots if slot.material}:
            shader=mat.node_tree.nodes.get('Principled BSDF')
            images=[n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
            if any('PineTree_Leaves' in image.name for image in images):
                link=next((l for l in mat.node_tree.links if l.to_socket==shader.inputs['Base Color']),None)
                if link:
                    mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY'
                    mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.42,.62,.46,1)
                    mat.node_tree.links.new(link.from_socket,mix.inputs[1])
                    mat.node_tree.links.new(mix.outputs[0],shader.inputs['Base Color'])
                shader.inputs['Roughness'].default_value=.88
    finally:
        dressing.OUT=old_out;dressing.texture_for=old_texture
    bpy.context.view_layer.update()
    textures=sorted({str(node.image.filepath) for ob in made for slot in ob.material_slots if slot.material
                     for node in slot.material.node_tree.nodes if node.type=='TEX_IMAGE' and node.image})
    assert all('NormalTree' not in path for path in textures),textures
    assert any('PineTree_Leaves' in path for path in textures),'Missing correct pine needle texture'
    report={'status':'native_evergreen_art_candidate','trees':len(accepted),'sources':kit.sources,
            'placements':kit.records,'textures':textures,'bounds':bounds(made),
            'root_contact':'sample_ground_z(x,y)-0.055m, native source base normalized at z0',
            'keepouts':'baseline roads, ponds, river, building/door/courtyard/castle footprints and existing native trunks',
            'geometry':'Native PineTree_1..5 FBX branch/needle meshes, verified source Y-up normalization',
            'approval':'No user visual approval or Unity/runtime acceptance claimed'}
    return {'objects':made,'report':report}


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--build-only',action='store_true');args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(CONTEXT),load_ui=False,use_scripts=False)
    collection=bpy.data.collections.new('World_Native_Evergreen_Groves');bpy.context.scene.collection.children.link(collection)
    result=build_world_evergreen_groves(collection)
    bpy.data.libraries.write(str(OUT/'native-evergreen-groves.blend'),{collection},fake_user=True)
    (OUT/'report.json').write_text(json.dumps(result['report'],indent=2),encoding='utf-8')
    if args.build_only:return
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=10;scene.cycles.use_denoising=True
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='CUDA';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='CUDA'
    scene.cycles.device='GPU';scene.render.threads_mode='FIXED';scene.render.threads=4
    scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'native-evergreens-whole-context.png')
    bpy.ops.render.render(write_still=True)
    print('EVERGREEN_GROVES_READY',len(result['report']['placements']),flush=True)


if __name__=='__main__':main()
