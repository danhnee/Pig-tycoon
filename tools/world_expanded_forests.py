"""Native mixed forests for the expanded120x90 art candidate, never old sampling."""
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

OUT=ROOT/'.collab/artifacts/world-expanded-forests-sol-high'


def _get(surface,name):
    value=surface.get(name) if isinstance(surface,dict) else getattr(surface,name,None)
    if not callable(value):raise ValueError('Expanded forest requires callable surface.'+name)
    return value


def _native_texture(name,family):
    clean=name.split('.')[0]
    exact=dressing.TEX/(clean+'.png')
    if clean.startswith(family+'_') and exact.exists():return exact
    lower=clean.lower()
    suffix='Leaves' if any(t in lower for t in ('leave','needle','foliage')) else 'Bark' if any(t in lower for t in ('bark','trunk','wood')) else None
    path=dressing.TEX/(family+'_'+suffix+'.png') if suffix else None
    return path if path and path.exists() else None


def _tone_templates(templates,family,tinted):
    for mat in {slot.material for ob in templates for slot in ob.material_slots if slot.material}:
        if mat.as_pointer() in tinted:continue
        tinted.add(mat.as_pointer())
        if not any(n.type=='TEX_IMAGE' and n.image and family+'_Leaves' in n.image.name for n in mat.node_tree.nodes):continue
        shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        link=next((l for l in mat.node_tree.links if l.to_socket==shader.inputs['Base Color']),None)
        if not link:continue
        mix=mat.node_tree.nodes.new('ShaderNodeMixRGB')
        if family=='PineTree':
            mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.43,.65,.48,1)
        else:
            mix.blend_type='COLOR';mix.inputs[0].default_value=.48 if family=='BirchTree' else .64
            mix.inputs[2].default_value=(.72,.51,.12,1) if family=='BirchTree' else (.80,.28,.055,1)
        mat.node_tree.links.new(link.from_socket,mix.inputs[1])
        mat.node_tree.links.new(mix.outputs[0],shader.inputs['Base Color'])
        shader.inputs['Roughness'].default_value=.88


def build_world_expanded_forests(collection,surface):
    """Return objects/report; surface must provide expanded height and art guard."""
    sample=_get(surface,'sample_ground_z');guard=_get(surface,'is_walkable')
    OUT.mkdir(parents=True,exist_ok=True)
    old_out,old_texture=dressing.OUT,dressing.texture_for
    dressing.OUT=OUT
    kit=DressKit();rng=random.Random(260719);accepted=[];objects=[];tinted=set()
    # Unequal, overlapping forest lobes; only ten small edge accents, no meadow carpet.
    lobes=[(-49,15,6.,13.,60,False),(-33,36,12.,5.,55,False),
           (-9,36,11.,5.,45,False),(-46,-10,4.5,11.,20,False),
           (-42,-32,4.,4.,5,True),(24,-36,5.,3.,5,True)]
    try:
        for cx,cy,rx,ry,target,accent in lobes:
            placed=0
            for attempt in range(2800):
                if placed>=target:break
                x,y=cx+rng.gauss(0,rx),cy+rng.gauss(0,ry)
                if not (-57<x<56 and -42<y<43):continue
                # Keep a broad continuous central pasture, even if all else is legal.
                if not accent and x>-38 and y<22:continue
                if not bool(guard(x,y)):continue
                if any((x-px)**2+(y-py)**2<2.65**2 for px,py in accepted):continue
                family=rng.choices(('PineTree','BirchTree','MapleTree'),weights=(.48,.34,.18))[0]
                name=family+'_'+str(rng.randint(1,5))
                dressing.texture_for=lambda material_name,kind=family:_native_texture(material_name,kind)
                templates,_=kit.load(name)
                _tone_templates(templates,family,tinted)
                height=rng.uniform(3.2,4.8) if accent else rng.uniform(4.1,7.0) if family=='PineTree' else rng.uniform(5.0,7.7)
                index=len(accepted)
                made=kit.place(name,(x,y),sample,height,collection,seed=88000+index,
                               yaw=rng.random()*math.tau,sink=.065)
                for ob in made:ob['expanded_forest']=True
                objects+=made;accepted.append((x,y));placed+=1
    finally:
        dressing.OUT=old_out;dressing.texture_for=old_texture
    if len(accepted)<150:raise RuntimeError(f'Expanded keepouts leave only{len(accepted)} trees; revise forest lobes')
    bpy.context.view_layer.update()
    images=sorted({n.image.name for ob in objects for slot in ob.material_slots if slot.material
                   for n in slot.material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image})
    assert not any('NormalTree' in name for name in images),images
    for family in ('PineTree','BirchTree','MapleTree'):
        assert any(family+'_Leaves' in name for name in images),family
    report={'status':'expanded_native_forest_candidate','trees':len(accepted),'sources':kit.sources,
            'placements':kit.records,'images':images,'bounds':bounds(objects),
            'surface':'Caller-supplied expanded surface.sample_ground_z/is_walkable only; no old62x47 sampler',
            'root_contact':'Actual expanded terrain minus0.065m trunk embed; source normalized atbaseZ0',
            'composition':'Irregular left/north pine-birch-maple forest lobes, sparse edge accents, central pasture kept clear',
            'approval':'No user visual or runtime acceptance claimed'}
    return {'objects':objects,'report':report}


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--verify',action='store_true');args=parser.parse_args()
    import world_expanded_spatial_layout as surface
    bpy.ops.wm.read_factory_settings(use_empty=True)
    collection=bpy.data.collections.new('Expanded_Native_Mixed_Forests');bpy.context.scene.collection.children.link(collection)
    result=build_world_expanded_forests(collection,surface)
    bpy.data.libraries.write(str(OUT/'expanded-native-forests.blend'),{collection},fake_user=True)
    report=result['report']
    if args.verify:
        checks=[]
        for record in report['placements']:
            x,y=record['xy_m'];meshes=[ob for ob in result['objects'] if abs(ob.location.x-x)<1e-5 and abs(ob.location.y-y)<1e-5]
            box=bounds(meshes);error=abs(box[0][2]-(surface.sample_ground_z(x,y)-.065))
            assert error<1e-4 and abs(box[1][2]-box[0][2]-record['target_height_m'])<1e-4
            assert surface.is_walkable(x,y)
            checks.append(error)
        report['verification']={'actual_root_and_height_checks':'PASS','max_root_error_m':max(checks),
                                'all_trunks_obey_expanded_water_road_pad_guard':True,'native_texture_families_only':True}
    (OUT/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps({'status':'READY','trees':report['trees'],'images':report['images'],'verification':report.get('verification')}),flush=True)


if __name__=='__main__':main()
