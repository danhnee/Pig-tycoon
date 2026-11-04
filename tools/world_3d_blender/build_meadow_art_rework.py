"""Author explicit meadow prototype replacements in a COPY of the pinned world.

CPU: python tools/build_meadow_art_rework.py --mode plan
Native (only via headless_runner and the shared lease): --mode build [--render]
Writes source .blend, PMB3 overlay and actual renders under the meadow artifact root.
Never edits Assets, the original .blend, terrain, instance matrices or visibility policy.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import random
import shutil
import struct
import sys

ROOT = Path(__file__).resolve().parents[1]
ART_ROOT = ROOT / '.collab/artifacts/art-rework-20261029/meadow'
BASE_BUNDLE = ROOT / '.collab/artifacts/original-map-unity-20261026/post-nature-5d4-source-bundle-v1'
SOURCE = Path(r'F:/PigTycoon-ArtStaging/map-v7-20261024/runs/sol-integrated-20261024-map33/post-water-forest/post-nature.blend')
SOURCE_SHA = '5d4fa1190ed4a688c8f8a817d81a1dfb9f148f5d388493ca4b67a50d808d6cd8'
MANIFEST_SHA = '2f14b0f259203a4eaad2a4ef8af2edafb95d1a958ce86ffe029751ddabcf7cf7'
SCHEMA = 'pigtycoon-meadow-prototype-overlay-v1'
GRASS_IDS = (4526, 4527, 4528, 4529)
FLOWER_IDS = tuple(range(4531, 4539))
BOTANICAL_IDS = (819, 820, 822, 824, 1024)
ROCK_IDS = (*range(1025, 1029), 1031, *range(1034, 1039), *range(1097, 1109))
TARGET_IDS = (*GRASS_IDS, *FLOWER_IDS, *BOTANICAL_IDS, *ROCK_IDS)

# sRGB design colors. Blender receives their scene-linear conversion; Unity
# receives the declared sRGB properties. No global material is overwritten.
PALETTE = {
    94: ((.24,.27,.22), (.44,.46,.38), (.62,.61,.51)),
    98: ((.16,.19,.09), (.27,.32,.14), (.39,.43,.21)),
    99: ((.13,.23,.10), (.27,.40,.17), (.47,.56,.28)),
    100: ((.18,.29,.12), (.38,.50,.22), (.56,.63,.34)),
    101: ((.22,.34,.14), (.43,.55,.25), (.63,.68,.40)),
    102: ((.31,.25,.40), (.56,.45,.66), (.74,.66,.79)),
    103: ((.29,.30,.12), (.61,.56,.23), (.80,.72,.37)),
    104: ((.32,.33,.14), (.68,.61,.29), (.86,.78,.45)),
    105: ((.22,.28,.10), (.44,.47,.18), (.65,.63,.30)),
    110: ((.20,.29,.12), (.37,.46,.20), (.58,.61,.35)),
    163: ((.21,.24,.21), (.34,.38,.33), (.49,.51,.43)),
    164: ((.28,.29,.24), (.47,.47,.39), (.64,.62,.53)),
    165: ((.31,.30,.25), (.51,.49,.41), (.70,.67,.58)),
    225: ((.34,.35,.13), (.68,.58,.24), (.88,.76,.38)),
    226: ((.28,.33,.12), (.60,.57,.23), (.81,.73,.36)),
    227: ((.40,.38,.16), (.74,.64,.30), (.93,.83,.48)),
    228: ((.22,.26,.10), (.42,.44,.18), (.65,.59,.28)),
    229: ((.59,.45,.20), (.83,.73,.43), (.94,.87,.64)),
    230: ((.25,.20,.10), (.45,.35,.16), (.65,.54,.29)),
    231: ((.48,.25,.15), (.72,.43,.27), (.88,.66,.46)),
    232: ((.30,.25,.39), (.54,.43,.63), (.74,.66,.78)),
    233: ((.58,.59,.44), (.83,.83,.68), (.97,.95,.82)),
}


def digest(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()


def dump(path, value):
    Path(path).write_text(json.dumps(value, indent=2), encoding='utf-8')


def add(a,b): return tuple(a[i]+b[i] for i in range(3))
def mul(a,t): return tuple(x*t for x in a)
def sub(a,b): return tuple(a[i]-b[i] for i in range(3))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a):
    length=math.sqrt(sum(x*x for x in a))
    if length<1e-10: raise ValueError('Degenerate authoring frame')
    return mul(a,1/length)


def bezier(points,t):
    a,b,c,d=points; u=1-t
    return add(add(mul(a,u**3),mul(b,3*u*u*t)),add(mul(c,3*u*t*t),mul(d,t**3)))


class Geometry:
    def __init__(self): self.vertices=[]; self.uv=[]; self.faces=[]; self.slots=[]; self.face_uv=[]
    def vertex(self,p,uv):
        if not all(math.isfinite(x) for x in (*p,*uv)): raise ValueError('Nonfinite authored vertex')
        self.vertices.append(tuple(p));self.uv.append(tuple(uv));return len(self.vertices)-1
    def face(self,indices,slot,uv=None):
        self.faces.append(tuple(indices));self.slots.append(slot);self.face_uv.append(uv)
    @property
    def triangles(self): return sum(len(face)-2 for face in self.faces)


def curved_leaf(g,base,angle,height,reach,width,slot,segments=6,ridge=True,solid=False,tip=.66,twist=0):
    """A swept cubic leaf, rooted upright, with a curved midrib and rolled edge.

    Grass uses narrow ribbons with six or seven longitudinal segments. Broad
    plants use five-point curved cross-sections with thin closed backs/edges.
    """
    direction=(math.cos(angle),math.sin(angle),0);side=(-direction[1],direction[0],0)
    controls=(base,add(base,add(mul(direction,reach*.05),(0,0,height*.70))),
              add(base,add(mul(direction,reach*.55),(0,0,height*1.05))),
              add(base,add(mul(direction,reach),(0,0,height*tip))))
    if solid:segments=max(segments,7)
    across=(-1,-.5,0,.5,1) if ridge else (-1,1)
    front=[];frames=[]
    for i in range(segments+1):
        t=i/segments;p=bezier(controls,t)
        tangent=unit(sub(bezier(controls,min(1,t+.002)),bezier(controls,max(0,t-.002))))
        normal=unit(cross(side,tangent));frames.append(normal)
        breadth=max(width*.025,width*(math.sin(math.pi*(.04+.95*t))**.72))
        ring=[]
        for j,s in enumerate(across):
            roll=width*(.13*(1-s*s)+twist*s*math.sin(t*math.pi))
            point=add(add(p,mul(side,s*breadth*.5)),mul(normal,roll))
            ring.append(g.vertex(point,((s+1)*.5,t)))
        front.append(ring)
    for i in range(segments):
        for j in range(len(across)-1):g.face((front[i][j],front[i][j+1],front[i+1][j+1],front[i+1][j]),slot)
    if solid:
        thickness=min(.00055,width*.012)
        back=[[g.vertex(sub(g.vertices[v],mul(frames[i],thickness)),g.uv[v]) for v in ring] for i,ring in enumerate(front)]
        for i in range(segments):
            for j in range(len(across)-1):g.face((back[i][j],back[i+1][j],back[i+1][j+1],back[i][j+1]),slot)
            for j in (0,len(across)-1):g.face((front[i][j],front[i+1][j],back[i+1][j],back[i][j]),slot,((0,0),(1,0),(1,1),(0,1)))
        for i in (0,segments):
            for j in range(len(across)-1):g.face((front[i][j],back[i][j],back[i][j+1],front[i][j+1]),slot,((0,0),(1,0),(1,1),(0,1)))


def stem(g,points,radius,slot,sides=5,segments=5):
    rings=[]
    for i in range(segments+1):
        t=i/segments;p=bezier(points,t)
        tangent=unit(sub(bezier(points,min(1,t+.002)),bezier(points,max(0,t-.002))))
        axis=unit(cross(tangent,(0,0,1) if abs(tangent[2])<.9 else (1,0,0)))
        other=cross(tangent,axis);ring=[]
        for j in range(sides+1):
            a=j/sides*math.tau;r=radius*(1-.55*t)
            ring.append(g.vertex(add(p,add(mul(axis,math.cos(a)*r),mul(other,math.sin(a)*r))),(j/sides,t)))
        rings.append(ring)
    for i in range(segments):
        for j in range(sides):g.face((rings[i][j],rings[i][j+1],rings[i+1][j+1],rings[i+1][j]),slot)


def grass(variant):
    rng=random.Random(4100+variant);g=Geometry()
    count=(10,11,12,9)[variant]
    basal=count-2
    prevailing=variant*.63
    for j in range(count):
        accent=j>=basal
        a=prevailing+rng.uniform(-.55,.55) if accent else prevailing+j*2.399+rng.uniform(-.25,.25)
        radial=rng.uniform(.003,.022);base=(math.cos(a)*radial,math.sin(a)*radial,-.003)
        curved_leaf(g,base,a,rng.uniform(.14,.18) if accent else rng.uniform(.07,.13),
                    rng.uniform(.05,.085) if accent else rng.uniform(.035,.065),
                    rng.uniform(.0037,.006) if accent else rng.uniform(.004,.007),
                    j%3,segments=7 if accent else 6,ridge=False,
                    tip=rng.uniform(.90,.98) if accent else rng.uniform(.72,.90),twist=rng.uniform(-.07,.07))
    assert 96<=g.triangles<=160
    return g


def fern(variant=0):
    rng=random.Random(6200+variant);g=Geometry()
    for frond in range(7 if variant==0 else 6):
        angle=frond*2.399+rng.uniform(-.13,.13);direction=(math.cos(angle),math.sin(angle),0)
        reach=rng.uniform(.26,.39);height=rng.uniform(.25,.37)
        path=((0,0,-.006),(0,0,height*.7),add(mul(direction,reach*.45),(0,0,height*1.1)),add(mul(direction,reach),(0,0,height*.55)))
        stem(g,path,.0021,0,4,6)
        for k in range(1,8):
            t=k/9;p=bezier(path,t);length=(math.sin(t*math.pi)**.7)*rng.uniform(.055,.10)
            for sign in (-1,1):
                curved_leaf(g,p,angle+sign*(1.17-.2*t),length*.28,length,rng.uniform(.019,.029),
                            1,segments=4,ridge=True,solid=True,tip=.22,twist=sign*.10)
        curved_leaf(g,bezier(path,.8),angle,.024,.055,.016,1,segments=4,solid=True,tip=.3)
    return g


def fan_shrub():
    rng=random.Random(835);g=Geometry()
    # Individual arching shoots, with air between compound leaves, rather than
    # tessellated upright fans. Its maximum local height is intentionally~0.4m.
    for j in range(8):
        a=j*2.399;direction=(math.cos(a),math.sin(a),0)
        h=rng.uniform(.29,.45);reach=rng.uniform(.21,.34)
        path=((0,0,-.008),(0,0,h*.65),add(mul(direction,reach*.55),(0,0,h)),add(mul(direction,reach),(0,0,h*.6)))
        stem(g,path,.0033,0,5,5)
        for k in range(2,6):
            p=bezier(path,k/6)
            for sign in (-1,1):
                curved_leaf(g,p,a+sign*1.0,rng.uniform(.025,.055),rng.uniform(.07,.12),rng.uniform(.030,.050),
                            1+(j+k)%3,segments=5,solid=True,tip=.4,twist=sign*.12)
    return g


def rosette():
    rng=random.Random(8241);g=Geometry()
    for j in range(13):
        inner=j>=8;a=j*2.399+.22
        curved_leaf(g,(0,0,-.006),a,rng.uniform(.18,.26) if inner else rng.uniform(.12,.17),
                    rng.uniform(.07,.13) if inner else rng.uniform(.13,.19),rng.uniform(.025,.047),
                    j%3,segments=7,solid=True,tip=.48 if inner else .35,twist=rng.uniform(-.2,.2))
    return g


def sedge():
    rng=random.Random(10241);g=Geometry()
    for j in range(13):
        angle=j*2.399+rng.uniform(-.15,.15)
        curved_leaf(g,(rng.uniform(-.055,.055),rng.uniform(-.055,.055),-.008),angle,
                    rng.uniform(.27,.45),rng.uniform(.22,.40),rng.uniform(.012,.025),0,
                    segments=7,solid=True,tip=rng.uniform(.42,.65),twist=rng.uniform(-.2,.2))
    return g


def cupped_petal(g,center,angle,radius,slot):
    """A rounded petal with seven longitudinal and five transverse stations."""
    direction=(math.cos(angle),math.sin(angle),0);side=(-direction[1],direction[0],0)
    rings=[];rows=(-1,-.5,0,.5,1)
    for k in range(7):
        t=k/6;width=radius*.72*(math.sin(math.pi*(.055+.89*t))**.57)
        ring=[]
        for s in rows:
            reach=radius*(.12+.88*t)-radius*.14*s*s*(t**3)
            lift=radius*(.025+.07*t+.12*t*t+.095*s*s)
            ring.append(g.vertex(add(center,add(add(mul(direction,reach),mul(side,s*width*.5)),(0,0,lift))),((s+1)*.5,t)))
        rings.append(ring)
    for k in range(6):
        for j in range(4):g.face((rings[k][j],rings[k][j+1],rings[k+1][j+1],rings[k+1][j]),slot)
    back=[[g.vertex(sub(g.vertices[v],(0,0,.00035)),g.uv[v]) for v in ring] for ring in rings]
    for k in range(6):
        for j in range(4):g.face((back[k][j],back[k+1][j],back[k+1][j+1],back[k][j+1]),slot)
        for j in (0,4):g.face((rings[k][j],rings[k+1][j],back[k+1][j],back[k][j]),slot,((0,0),(1,0),(1,1),(0,1)))
    for k in (0,6):
        for j in range(4):g.face((rings[k][j],back[k][j],back[k][j+1],rings[k][j+1]),slot,((0,0),(1,0),(1,1),(0,1)))


def flower_head(g,center,radius,petal_slot,seed_slot,petals=8):
    for j in range(petals):
        a=j/petals*math.tau+.08*math.sin(j*4.7)
        # Cupped, round-ended individual petals, not a star or a colored block.
        cupped_petal(g,center,a,radius,petal_slot)
    rings=[];steps=12
    for k,(r,z) in enumerate(((radius*.24,0),(radius*.22,radius*.13),(radius*.08,radius*.20))):
        ring=[]
        for j in range(steps+1):
            a=j/steps*math.tau
            ring.append(g.vertex(add(center,(math.cos(a)*r,math.sin(a)*r,z)),(.5+math.cos(a)*r/radius,.5+math.sin(a)*r/radius)))
        rings.append(ring)
    for k in range(2):
        for j in range(steps):g.face((rings[k][j],rings[k][j+1],rings[k+1][j+1],rings[k+1][j]),seed_slot)
    top=g.vertex(add(center,(0,0,radius*.215)),(.5,.5))
    for j in range(steps):g.face((rings[-1][j],rings[-1][j+1],top),seed_slot)


def flowers(variant,large=False):
    rng=random.Random(7300+variant);g=Geometry();height=rng.uniform(.17,.23) if not large else .38
    # Source LD slots are stem0/petal1/seed2; BOT violet is stem0/petal1/null2/leaf3.
    leaf_slot=3 if large else 0;seed_slot=1 if large else 2
    tips=[(.018,-.008,height)]
    if variant%3==1:
        tips=[(-.022,-.012,height*.93),(.021,.008,height),(0,.023,height*.86)]
    for index,tip in enumerate(tips):
        path=((0,0,-.004),(.006,0,height*.35),(tip[0]*.6,tip[1],height*.85),tip)
        stem(g,path,.0015 if not large else .0027,0,5,5)
        for j in range(2):
            curved_leaf(g,bezier(path,.28+j*.23),index+j*2.3+.5,height*.09,height*.26,height*.10,
                        leaf_slot,segments=4,solid=True,tip=.4)
        flower_head(g,tip,(.025 if len(tips)>1 else .037)*(1.3 if large else 1),1,seed_slot,petals=7 if large else 8)
    return g


def layered_stone(variant,bounds,slot_count):
    rng=random.Random(9400+variant);g=Geometry();n=24
    # Connected slab volume with a shared, low-frequency outline. Recessed
    # bedding planes and asymmetric chipped shoulders are geometry, not noise.
    sections=((0,.72),(.055,.91),(.19,1),(.37,.98),(.405,.89),(.63,.91),(.665,.83),(.84,.81),(.955,.68),(1,.55))
    phase=rng.uniform(0,math.tau);rings=[]
    for k,(t,radius) in enumerate(sections):
        ring=[]
        for j in range(n+1):
            a=j/n*math.tau;ca=math.cos(a);sa=math.sin(a)
            coherent=1+.055*math.sin(2*a+phase)+.025*math.cos(5*a-phase)
            # One broad erosion notch, retained coherently across nearby strata.
            notch=1-.15*max(0,math.cos(a-phase))**12*(.4+.6*math.sin(t*math.pi))
            x=math.copysign(abs(ca)**.62,ca)*radius*coherent*notch+.12*t
            y=math.copysign(abs(sa)**.72,sa)*radius*coherent-.07*t
            z=t+.018*math.sin(2*a+phase)*math.sin(t*math.pi)
            ring.append(g.vertex((x,y,z),(j/n,t)))
        rings.append(ring)
    for k in range(len(sections)-1):
        slot=0 if slot_count==1 else (2 if k in (3,5,8) else (0 if k<2 else 1))
        for j in range(n):g.face((rings[k][j],rings[k][j+1],rings[k+1][j+1],rings[k+1][j]),slot)
    for ring,reverse in ((rings[0],True),(rings[-1],False)):
        center=tuple(sum(g.vertices[v][axis] for v in ring[:-1])/n for axis in range(3))
        center_id=g.vertex(center,(.5,.5))
        for j in range(n):g.face((ring[j+1],ring[j],center_id) if reverse else (ring[j],ring[j+1],center_id),0 if slot_count==1 else 1)
    # Preserve the original stone's local extent, anchor and buried portion.
    old_min,old_max=bounds
    low=[min(v[i] for v in g.vertices) for i in range(3)];high=[max(v[i] for v in g.vertices) for i in range(3)]
    g.vertices=[tuple(old_min[i]+(p[i]-low[i])/(high[i]-low[i])*(old_max[i]-old_min[i]) for i in range(3)) for p in g.vertices]
    return g


def original_bounds(bundle,entry):
    import numpy as np
    with (bundle/entry['file']).open('rb') as stream:
        magic,nv,ni,ns=struct.unpack('<4sIII',stream.read(16))
        if magic!=b'PMB3':raise ValueError('Wrong source PMB magic')
        stream.seek(ns*12,1);v=np.fromfile(stream,dtype='<f4',count=nv*3).reshape(-1,3)
    # Undo the explicit Unity basis. The supported selected prototypes are not
    # mirrored; fail rather than silently choose a different source convention.
    if entry['mirror_x']:v[:,0]*=-1
    blender=v[:,[0,2,1]]
    return blender.min(0).tolist(),blender.max(0).tolist()


def authored_geometry(mesh_id,entry,bundle):
    if mesh_id in GRASS_IDS:return grass(GRASS_IDS.index(mesh_id))
    if mesh_id in FLOWER_IDS:return flowers(FLOWER_IDS.index(mesh_id))
    if mesh_id==819:return fan_shrub()
    if mesh_id==820:return fern()
    if mesh_id==822:return flowers(8,large=True)
    if mesh_id==824:return rosette()
    if mesh_id==1024:return sedge()
    return layered_stone(mesh_id,original_bounds(bundle,entry),len(entry['material_ids']))


def material_definition(source_id,source_name):
    ramp=PALETTE[source_id];key=f'MeadowRework_Mat_{source_id:03d}'
    rock=source_id in (94,163,164,165)
    grass_material=source_id in (225,226,227)
    shader='PigTycoon/URP/ProceduralRockCoast' if rock else ('PigTycoon/URP/ElasticMeadowGrass' if grass_material else 'PigTycoon/URP/FoliageBough')
    colors={f'_RampColor{i}':[*c,1] for i,c in enumerate(ramp)}
    floats={'_RampPos0':0,'_RampPos1':.48,'_RampPos2':1,'_Smoothness':.14 if rock else .24,'_Metallic':0}
    if rock:floats.update(_StrataFrequency=4.2,_NoiseFrequency=2.3,_StrataDistortion=.24,_BumpStrength=.38)
    elif grass_material:
        colors.update(_BaseColor=[*ramp[2],1],_RootEarthColor=[*ramp[0],1],_MidOliveColor=[*ramp[1],1])
        floats={'_WindSpeed':1.25,'_WindStrength':.035,'_ElasticStiffness':1.65}
    else:floats.update(_RampCoordinate=1,_Translucency=.18,_Cull=0)
    return {'key':key,'sourceMaterialId':source_id,'sourceMaterialName':source_name,
            'candidateBlenderName':key,'recommendedUnityShader':shader,
            'roughness':.86 if rock else .72,'colorsAre':'SRGB_DESIGN_COLORS_CONVERTED_TO_LINEAR_IN_BLENDER',
            'unityColorsSRGB':colors,'unityFloats':floats,
            'scope':'CLONE_FOR_REPLACED_PROTOTYPES_ONLY_NOT_GLOBAL_PALETTE_CHANGE'}


def srgb_to_linear(c):return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4


def create_material(bpy,definition):
    material=bpy.data.materials.new(definition['key']);material.use_nodes=True
    nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
    output=nodes.new('ShaderNodeOutputMaterial');bsdf=nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value=definition['roughness']
    bsdf.inputs['Metallic'].default_value=0
    tex=nodes.new('ShaderNodeTexCoord');separate=nodes.new('ShaderNodeSeparateXYZ')
    links.new(tex.outputs['Generated'],separate.inputs[0])
    ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
    for i,t in enumerate((0,.48,1)):
        element=ramp.color_ramp.elements[0] if i==0 else ramp.color_ramp.elements.new(t)
        element.position=t;color=definition['unityColorsSRGB'][f'_RampColor{i}']
        element.color=(*[srgb_to_linear(c) for c in color[:3]],1)
    links.new(separate.outputs['Z'],ramp.inputs[0]);links.new(ramp.outputs['Color'],bsdf.inputs['Base Color'])
    noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=9 if definition['sourceMaterialId'] in (94,163,164,165) else 28
    noise.inputs['Detail'].default_value=2;links.new(tex.outputs['Generated'],noise.inputs['Vector'])
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22 if definition['sourceMaterialId'] in (94,163,164,165) else .06
    bump.inputs['Distance'].default_value=.025 if definition['sourceMaterialId'] in (94,163,164,165) else .002
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
    links.new(bsdf.outputs[0],output.inputs[0]);material.diffuse_color=ramp.color_ramp.elements[1].color
    material['meadow_rework_source_material_id']=definition['sourceMaterialId']
    return material


def create_mesh(bpy,name,g,materials,rock=False):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(g.vertices,[],g.faces);mesh.update()
    if mesh.validate(verbose=True):raise ValueError('Authored mesh required repairs: '+name)
    first_material=next(material for material in materials if material is not None)
    for index,material in enumerate(materials):
        mesh.materials.append(material if material is not None else first_material)
        if material is None:mesh.materials[index]=None
    for polygon,slot in zip(mesh.polygons,g.slots):
        if slot>=len(materials) or materials[slot] is None:raise ValueError('Invalid material slot in '+name)
        polygon.material_index=slot;polygon.use_smooth=True
    uv=mesh.uv_layers.new(name='MeadowAuthoredUV')
    for polygon,custom_uv in zip(mesh.polygons,g.face_uv):
        for corner,loop_id in enumerate(polygon.loop_indices):
            uv.data[loop_id].uv=custom_uv[corner] if custom_uv is not None else g.uv[mesh.loops[loop_id].vertex_index]
    if rock:mesh.set_sharp_from_angle(angle=.55)
    return mesh


def encode_pmb(mesh,path,mirror):
    import numpy as np
    bridge=ROOT/'.agents/skills/blender-unity-evaluated-export/scripts'
    sys.path.insert(0,str(bridge))
    from map_contract import write_mesh_arrays
    mesh.calc_loop_triangles();tris=list(mesh.loop_triangles);n=len(tris)*3
    positions=np.empty((n,3),dtype='<f4');normals=np.empty_like(positions);uv=np.empty((n,2),dtype='<f4')
    slots=np.empty(len(tris),dtype=np.int32);corners=mesh.corner_normals
    for i,tri in enumerate(tris):
        slots[i]=mesh.polygons[tri.polygon_index].material_index
        for j,loop_id in enumerate(tri.loops):
            k=i*3+j;positions[k]=mesh.vertices[mesh.loops[loop_id].vertex_index].co
            normals[k]=corners[loop_id].vector;uv[k]=mesh.uv_layers.active.data[loop_id].uv
    with path.open('wb') as stream:
        result=write_mesh_arrays(stream,positions,normals,uv,np.arange(n,dtype=np.int32).reshape(-1,3),slots,mirror)
    result.update(sha256=digest(path),bytes=path.stat().st_size,hasUV=True)
    result['localBoundsBlender']={'min':positions.min(0).tolist(),'max':positions.max(0).tolist()}
    return result


def source_state(bpy,target_names):
    result={}
    for obj in bpy.data.objects:
        result[obj.name]={'matrix':tuple(x for row in obj.matrix_world for x in row),
                          'hideRender':obj.hide_render,'hideViewport':obj.hide_viewport,
                          'data':obj.data.as_pointer() if obj.data else 0,'replaced':obj.name in target_names}
    return result


def verify_retained_state(bpy,before):
    for name,state in before.items():
        obj=bpy.data.objects.get(name)
        if obj is None:raise ValueError('Removed source object: '+name)
        if tuple(x for row in obj.matrix_world for x in row)!=state['matrix']:raise ValueError('Changed source matrix: '+name)
        if obj.hide_render!=state['hideRender'] or obj.hide_viewport!=state['hideViewport']:raise ValueError('Changed source visibility: '+name)
        if not state['replaced'] and (obj.data.as_pointer() if obj.data else 0)!=state['data']:raise ValueError('Changed retained data: '+name)


def load_base(args):
    if digest(args.bundle/'manifest.json')!=MANIFEST_SHA:raise ValueError('Pinned manifest hash mismatch')
    manifest=json.loads((args.bundle/'manifest.json').read_text(encoding='utf-8'))
    if manifest['source_sha256']!=SOURCE_SHA:raise ValueError('Pinned source identity mismatch')
    for i in TARGET_IDS:
        entry=manifest['meshes'][i]
        if digest(args.bundle/entry['file'])!=entry['sha256']:raise ValueError(f'Source mesh{i} hash mismatch')
    return manifest


def write_plan(args,manifest):
    import numpy as np
    count=manifest['counters']['exported']
    records=np.memmap(args.bundle/'instances.pmi',dtype=np.dtype([('mesh','<u4'),('binding','<u4'),('matrix','<f4',(4,4))]),mode='r',offset=12,shape=(count,))
    counts=np.bincount(records['mesh'],minlength=len(manifest['meshes']))
    entries=[]
    for i in TARGET_IDS:
        old=manifest['meshes'][i];g=authored_geometry(i,old,args.bundle)
        entries.append({'meshId':i,'sourceObject':old['source_object'],'instancesAffected':int(counts[i]),
                        'oldTriangles':old['triangles'],'authoredTriangles':g.triangles,
                        'sourceMaterialIds':old['material_ids'],'originalPMBSHA256':old['sha256']})
    plan={'schema':SCHEMA,'status':'SOURCE_AUTHORING_PLAN_NOT_ART_APPROVAL','baseManifestSHA256':MANIFEST_SHA,
          'baseSourceSHA256':SOURCE_SHA,'replacements':entries,'allSourceRecordsRetained':count,
          'instanceMatricesChanged':0,'sourceInstancesSHA256':manifest['instances_sha256'],
          'sourceLedgerSHA256':manifest['sources_sha256'],'protected':'Terrain, pond, buildings, observatory, non-target prototype data and original visibility policy unchanged.',
          'grassBudget':'9–12 blades with a low7–13cm basalcluster and only2 flowing14–18cm accents;112–148tri, all1,032,406 source grass instances retained.',
          'nativeBuildOrRenderPerformed':False}
    dump(args.out/'authoring-plan.json',plan)
    return entries


def inspect_native(bpy,args,manifest):
    bpy.ops.wm.open_mainfile(filepath=str(args.source))
    objects=[]
    for i in TARGET_IDS:
        entry=manifest['meshes'][i];obj=bpy.data.objects.get(entry['source_object'])
        if obj is None:raise ValueError('Missing exact source object '+entry['source_object'])
        objects.append({'meshId':i,'object':obj.name,'type':obj.type,'data':obj.data.name,
                        'vertices':len(obj.data.vertices),'polygons':len(obj.data.polygons),
                        'materials':[mat.name if mat else None for mat in obj.data.materials],
                        'modifiers':[{'name':mod.name,'type':mod.type,'viewport':mod.show_viewport,'render':mod.show_render} for mod in obj.modifiers],
                        'hideRender':obj.hide_render,'hideViewport':obj.hide_viewport})
    dump(args.out/'native-source-inspection.json',{'sourceSHA256':digest(args.source),'objects':objects})


def render_views(bpy,args,replacements):
    from mathutils import Vector
    scene=bpy.context.scene
    render_asset_contact(bpy,args,replacements)
    bpy.context.window.scene=scene
    if not args.render_world_crops:return
    scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=args.samples
    scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=args.threads
    scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
    # Actual source-world crops. No substitute ground/pond or cloned buildings.
    views=(('meadow_gameplay',(0,-25.6,16.6),(0,-12,7.9),46),
           ('meadow_reverse',(10,-1,13),(0,-14,7.7),47),
           ('meadow_ground_close',(1.4,-18.0,8.3),(0,-15.8,7.35),48),
           ('meadow_botanical',(20,-24,10.2),(17,-19,7.7),50))
    camera_data=bpy.data.cameras.new('MeadowRework_EvidenceCamera');camera=bpy.data.objects.new(camera_data.name,camera_data)
    scene.collection.objects.link(camera);scene.camera=camera
    render_records=[]
    for name,position,target,lens in views:
        camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.lens=lens
        scene.render.filepath=str(args.out/(name+'.png'));bpy.ops.render.render(write_still=True)
        render_records.append({'name':name,'path':name+'.png','cameraBlender':position,'targetBlender':target,'lensMm':lens})
    dump(args.out/'native-render-evidence.json',{'scope':'ACTUAL_MODIFIED_SOURCE_WORLD_CROPS_NOT_VISUAL_ACCEPTANCE','views':render_records,
                                               'artApproved':False,'geometryReplacementIds':[r['meshId'] for r in replacements]})


def render_asset_contact(bpy,args,replacements):
    """Actual replacement meshes on a named contact board, never a world substitute."""
    from mathutils import Vector
    scene=bpy.data.scenes.new('MeadowPrototypeContact_NOT_WORLD')
    bpy.context.window.scene=scene;scene.render.engine='CYCLES';scene.cycles.device='CPU'
    scene.cycles.samples=args.samples;scene.cycles.use_denoising=True
    scene.render.threads_mode='FIXED';scene.render.threads=args.threads
    scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
    world=bpy.data.worlds.new('MeadowContactNeutralWorld');world.use_nodes=True
    world.node_tree.nodes['Background'].inputs['Color'].default_value=(.36,.40,.46,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value=.35;scene.world=world
    sun_data=bpy.data.lights.new('MeadowContactSun','SUN');sun_data.energy=2.2;sun_data.angle=math.radians(12)
    sun=bpy.data.objects.new(sun_data.name,sun_data);scene.collection.objects.link(sun);sun.rotation_euler=(.52,-.28,-.58)
    fill_data=bpy.data.lights.new('MeadowContactFill','AREA');fill_data.energy=180;fill_data.shape='DISK';fill_data.size=5
    fill=bpy.data.objects.new(fill_data.name,fill_data);scene.collection.objects.link(fill);fill.location=(-2,-1,4)
    fill.rotation_euler=(Vector((0,0,0))-fill.location).to_track_quat('-Z','Y').to_euler()
    ground=bpy.data.materials.new('MeadowContactSoil_ONLY_REVIEW');ground.diffuse_color=(.13,.11,.075,1);ground.use_nodes=True
    ground.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.13,.11,.075,1)
    ground.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.94
    mesh=bpy.data.meshes.new('ContactBoardGround_NOT_WORLD')
    mesh.from_pydata([(-4,-3,-.08),(4,-3,-.08),(4,3,-.08),(-4,3,-.08)],[],[(0,1,2,3)]);mesh.materials.append(ground)
    obj=bpy.data.objects.new(mesh.name,mesh);scene.collection.objects.link(obj)
    source={r['meshId']:bpy.data.objects[r['sourceObject']] for r in replacements}
    # Preserve the original source density/relative transforms for the grass
    # contact patch. The first board accidentally overpacked300 tufts into1.26m²;
    # the actual source has102 tufts in this2×2m gameplay section.
    import numpy as np
    from mathutils import Matrix
    base=json.loads((args.bundle/'manifest.json').read_text(encoding='utf-8'))
    records=np.memmap(args.bundle/'instances.pmi',dtype=np.dtype([('mesh','<u4'),('binding','<u4'),('matrix','<f4',(4,4))]),
                      mode='r',offset=12,shape=(base['counters']['exported'],))
    points=records['matrix'][:,:3,3]
    mask=(records['mesh']>=4526)&(records['mesh']<=4529)&(np.abs(points[:,0])<=1)&(np.abs(points[:,2]+15)<=1)
    patch=records[mask].copy();ground_record=records[records['mesh']==362][0].copy()
    del points,mask,records
    # Match the actual source surface beneath those exact matrices, instead of
    # making real slope offsets appear as floating roots on a flat review board.
    ground_entry=base['meshes'][362];ground_path=args.bundle/ground_entry['file']
    if digest(ground_path)!=ground_entry['sha256']:raise ValueError('Source contact-ground hash mismatch')
    with ground_path.open('rb') as stream:
        _,nv,ni,ns=struct.unpack('<4sIII',stream.read(16));stream.seek(ns*12,1)
        local=np.fromfile(stream,dtype='<f4',count=nv*3).reshape(-1,3)
        normal=np.fromfile(stream,dtype='<f4',count=nv*3).reshape(-1,3)
        stream.seek(nv*8,1);triangles=np.fromfile(stream,dtype='<u4',count=ni).reshape(-1,3)
    transform=ground_record['matrix'];world=local@transform[:3,:3].T+transform[:3,3]
    world=world[:,[0,2,1]]-np.array((0,-15,7.2172899),dtype=np.float32)
    tri_positions=world[triangles]
    inside=(tri_positions[:,:,0].min(1)<3)&(tri_positions[:,:,0].max(1)>-3)&(tri_positions[:,:,1].min(1)<2)&(tri_positions[:,:,1].max(1)>-2)
    selected=triangles[inside][:,[0,2,1]];used=np.unique(selected)
    mapping=np.full(nv,-1,dtype=np.int32);mapping[used]=np.arange(len(used),dtype=np.int32)
    patch_mesh=bpy.data.meshes.new('ACTUAL_SOURCE_TERRAIN_362_CONTACT_CROP')
    patch_mesh.from_pydata(world[used].tolist(),[],mapping[selected].tolist());patch_mesh.materials.append(ground);patch_mesh.update()
    patch_object=bpy.data.objects.new(patch_mesh.name,patch_mesh);scene.collection.objects.link(patch_object)
    for polygon in patch_mesh.polygons:polygon.use_smooth=True
    transformed_normals=(normal@np.linalg.inv(transform[:3,:3]))[:,[0,2,1]]
    transformed_normals/=np.maximum(np.linalg.norm(transformed_normals,axis=1,keepdims=True),1e-12)
    subset_normals=transformed_normals[used]
    patch_mesh.normals_split_custom_set([tuple(subset_normals[loop.vertex_index]) for loop in patch_mesh.loops])
    for j,record in enumerate(patch):
        i=int(record['mesh']);clone=bpy.data.objects.new(f'CONTACT_SOURCE_GRASS_{i}_{j}',source[i].data);scene.collection.objects.link(clone)
        matrix=record['matrix'][[0,2,1,3]][:,[0,2,1,3]].copy()
        # One documented review translation; all source rotations/scales and
        # relative positions remain unchanged. This board is not a new world.
        matrix[:3,3]-=np.array((0,-15,7.2172899),dtype=np.float32)
        clone.matrix_world=Matrix(matrix.tolist())
    for i,location,scale in ((819,(-.75,.35,0),1.72),(820,(.05,.65,0),1.70),
                             (824,(.25,-.15,0),1.32),(1024,(-1.6,.8,0),1.35)):
        clone=bpy.data.objects.new(f'CONTACT_BOTANICAL_{i}',source[i].data);scene.collection.objects.link(clone)
        clone.location=location;clone.scale=(scale,scale,scale)
    for j,i in enumerate(FLOWER_IDS):
        clone=bpy.data.objects.new(f'CONTACT_FLOWER_{i}',source[i].data);scene.collection.objects.link(clone)
        clone.location=(-.68+j*.19,-.20+(.08 if j%2 else 0),0)
    rock=bpy.data.objects.new('CONTACT_LAYERED_STONE',source[1097].data);scene.collection.objects.link(rock);rock.location=(1.8,.4,-.20)
    camera_data=bpy.data.cameras.new('MeadowContactCamera');camera=bpy.data.objects.new(camera_data.name,camera_data)
    scene.collection.objects.link(camera);scene.camera=camera
    views=(('contact_grass_flowers',(0,-2.35,.55),(0,-.1,.13),58),
           ('contact_botanical_front',(-.1,-2.5,1.05),(-.50,.35,.30),55),
           ('contact_botanical_rear',(-.5,2.9,1.1),(-.5,.35,.30),55),
           ('contact_stone_front',(4.1,-2.3,1.35),(1.8,.4,.35),55),
           ('contact_stone_side',(3.9,2.7,1.2),(1.8,.4,.35),55))
    records=[]
    for name,position,target,lens in views:
        if args.contact_view!='all' and name!=args.contact_view:continue
        camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.lens=lens
        scene.render.filepath=str(args.out/(name+'.png'));bpy.ops.render.render(write_still=True)
        records.append({'name':name,'path':name+'.png','cameraBlender':position,'targetBlender':target,'lensMm':lens})
    dump(args.out/'asset-contact-evidence.json',{'scope':'ACTUAL_REPLACEMENT_PROTOTYPE_CONTACT_BOARD_NOT_WORLD','artApproved':False,
                                               'sourceMeshesIdenticalToOverlay':True,'views':records,
                                               'grassPlacement':'Original source102-instance2×2m gameplay patch, common review translation only.',
                                               'grassInstanceCount':len(patch),'groundSourcePrototype':362,
                                               'groundGeometry':'Actual transformed source triangles, cropped for this contact view.'})


def render_contact_from_overlay(bpy,args):
    """New process: do not keep the365MB original world in RAM for an asset view."""
    import numpy as np
    sys.path.insert(0,str(ROOT/'.agents/skills/blender-unity-evaluated-export/scripts'))
    from map_contract import decode_mesh
    overlay=json.loads(args.overlay.read_text(encoding='utf-8'))
    if overlay['schema']!=SCHEMA or overlay['baseBundle']['manifestSHA256']!=MANIFEST_SHA:
        raise ValueError('Wrong source overlay identity')
    materials={key:create_material(bpy,definition) for key,definition in overlay['materials'].items()}
    for replacement in overlay['replacements']:
        path=args.overlay.parent/replacement['pmbRelativePath']
        if digest(path)!=replacement['sha256']:raise ValueError('Replacement PMB hash mismatch: '+str(path))
        decoded=decode_mesh(path.read_bytes());mirror=replacement['mirrorX']
        positions=np.asarray(decoded['positions'],dtype=np.float32)[:,[0,2,1]]
        normals=np.asarray(decoded['normals'],dtype=np.float32)[:,[0,2,1]]
        if mirror:positions[:,0]*=-1;normals[:,0]*=-1
        indices=decoded['indices'];faces=[];slots=[]
        for slot,start,count in decoded['submeshes']:
            for first in range(start,start+count,3):
                a,b,c=indices[first:first+3];faces.append((a,b,c) if mirror else (a,c,b));slots.append(slot)
        mesh=bpy.data.meshes.new('ContactPMB_'+replacement['sourceObject'])
        mesh.from_pydata(positions.tolist(),[],faces);mesh.update()
        max_slot=max((item['slot'] for item in replacement['materialSlotOverrides']),default=0)
        keyed={item['slot']:materials[item['candidateMaterialKey']] for item in replacement['materialSlotOverrides']}
        for slot in range(max_slot+1):
            mesh.materials.append(keyed.get(slot,next(iter(keyed.values()))))
            if slot not in keyed:mesh.materials[slot]=None
        for polygon,slot in zip(mesh.polygons,slots):polygon.material_index=slot;polygon.use_smooth=True
        uv=mesh.uv_layers.new(name='SourcePMB_UV')
        loop_normals=[]
        for loop in mesh.loops:
            uv.data[loop.index].uv=decoded['uvs'][loop.vertex_index]
            loop_normals.append(tuple(normals[loop.vertex_index]))
        mesh.normals_split_custom_set(loop_normals)
        bpy.data.objects.new(replacement['sourceObject'],mesh)
    render_asset_contact(bpy,args,overlay['replacements'])
    dump(args.out/'render-source.json',{'overlay':str(args.overlay),'overlaySHA256':digest(args.overlay),
                                       'originalSourceLoaded':False,'geometrySource':'ACTUAL_AUTHORED_PMB_REPLACEMENTS',
                                       'artApproved':False})


def build_native(bpy,args,manifest,plan,prototype_only=False):
    inspection={}
    if prototype_only:
        inspected=json.loads((ART_ROOT/'native-inspect-v1/native-source-inspection.json').read_text(encoding='utf-8'))
        if inspected['sourceSHA256']!=SOURCE_SHA:raise ValueError('Prior native inspection does not bind the pinned source')
        inspection={item['object']:item for item in inspected['objects']}
        bpy.ops.wm.read_factory_settings(use_empty=True)
        before=None
    else:
        if digest(args.source)!=SOURCE_SHA:raise ValueError('Actual source .blend hash mismatch')
        bpy.ops.wm.open_mainfile(filepath=str(args.source))
        names={manifest['meshes'][i]['source_object'] for i in TARGET_IDS};before=source_state(bpy,names)
    materials={}
    used_material_ids={mid for i in TARGET_IDS for mid in manifest['meshes'][i]['material_ids'] if mid>=0}
    for mid in sorted(used_material_ids):
        definition=material_definition(mid,manifest['materials'][mid]['name'])
        materials[mid]=(definition,create_material(bpy,definition))
    replacements=[];mesh_dir=args.out/'meshes';mesh_dir.mkdir()
    for item in plan:
        i=item['meshId'];entry=manifest['meshes'][i];obj=bpy.data.objects.get(entry['source_object'])
        if prototype_only:
            if entry['source_object'] not in inspection:raise ValueError('Exact object was not natively inspected')
            old_data_name=inspection[entry['source_object']]['data'];original_slot_links=[];modifier_ledger=[]
        else:
            if obj is None or obj.type!='MESH':raise ValueError('Exact mesh source missing: '+entry['source_object'])
            old_data_name=obj.data.name
            original_slot_links=[{'slot':slot,'link':binding.link,'material':binding.material.name if binding.material else None}
                                 for slot,binding in enumerate(obj.material_slots)]
            modifier_ledger=[{'name':mod.name,'type':mod.type,'viewport':mod.show_viewport,'render':mod.show_render} for mod in obj.modifiers]
        g=authored_geometry(i,entry,args.bundle)
        candidate_mats=[materials[mid][1] if mid>=0 else None for mid in entry['material_ids']]
        mesh=create_mesh(bpy,'MeadowRework_'+entry['source_object'],g,candidate_mats,i in ROCK_IDS)
        # Modifiers belong to the explicitly replaced silhouette. Disable only
        # these modifiers, retaining the source object/collections/transforms.
        if prototype_only:
            obj=bpy.data.objects.new(entry['source_object'],mesh);bpy.context.scene.collection.objects.link(obj)
            library_index=len(replacements)
            obj.location=((library_index%7)*3.2,(library_index//7)*3.2,0)
            obj['meadow_rework_library_layout_only']=True
        else:
            for mod in obj.modifiers:mod.show_viewport=False;mod.show_render=False
            obj.data=mesh
        obj['meadow_rework_base_pmb_sha256']=entry['sha256'];obj['meadow_rework_original_data']=old_data_name
        for binding in obj.material_slots:binding.link='DATA'
        path=mesh_dir/f'{i:06d}.pmb';metrics=encode_pmb(mesh,path,entry['mirror_x'])
        replacements.append({**item,'originalDataName':old_data_name,'originalMaterialSlotLinks':original_slot_links,
            'disabledTargetModifiers':modifier_ledger,
            'pmbRelativePath':str(path.relative_to(args.out)).replace('\\','/'),**metrics,
            'mirrorX':entry['mirror_x'],'originalLocalBoundsBlender':dict(zip(('min','max'),original_bounds(args.bundle,entry))),
            'materialSlotOverrides':[{'slot':slot,'sourceMaterialId':mid,'candidateMaterialKey':materials[mid][0]['key']} for slot,mid in enumerate(entry['material_ids']) if mid>=0],
            'intent':'Explicit replacement geometry/material, not an omitted, culled or rescaled instance.'})
    if before is not None:verify_retained_state(bpy,before)
    candidate=args.out/('MeadowPrototypeSource.blend' if prototype_only else 'MeadowSourceRework.blend')
    bpy.ops.wm.save_as_mainfile(filepath=str(candidate))
    if not prototype_only and digest(args.source)!=SOURCE_SHA:raise ValueError('Original source changed during candidate authoring')
    overlay={'schema':SCHEMA,'status':'CANDIDATE_NOT_VISUALLY_APPROVED','artApproved':False,
        'baseBundle':{'directory':str(args.bundle),'manifestSHA256':MANIFEST_SHA,'sourceBlendSHA256':SOURCE_SHA,
                      'runId':manifest['run_id'],'instancesSHA256':manifest['instances_sha256'],'sourceLedgerSHA256':manifest['sources_sha256']},
        'candidateSource':{'path':str(candidate),'sha256':digest(candidate),
                           'scope':'EXPLICIT_PROTOTYPE_LIBRARY_OVERLAY_NOT_WORLD' if prototype_only else 'MODIFIED_ORIGINAL_WORLD_COPY'},
        'replacements':replacements,'materials':{definition['key']:definition for definition,_ in materials.values()},
        'materialDefinitions':[array_material_definition(definition) for definition,_ in materials.values()],
        'coverage':{'baseRecordCount':manifest['counters']['exported'],'instanceMatricesChanged':0,'instanceRecordsOmitted':0,
                    'originalSourceUnchanged':True,'originalObjectMatricesAndVisibilityUnchanged':True,
                    'originalWorldLoadedInThisJob':not prototype_only,
                    'originalObjectStateComparedInThisJob':before is not None,
                    'nonTargetDataBindingsUnchanged':True,'sourceMaterialDatablocksOverwritten':0,
                    'worldOrTerrainSubstituteCreated':False,'approvedBuildingsModified':False},
        'limits':['PMB overlay contains explicit replacements only; unchanged geometry is read from the pinned bundle.',
                  'Unity must clone materials only for replaced prototype slots; do not globally overwrite source material IDs.',
                  ('Saved source is an explicit39-prototype authoring library, not a replacement world; original PMI transforms compose the full world.'
                   if prototype_only else 'Actual new geometry is authored in the saved original-world copy; full evaluated GN export parity still needs checking.'),
                  'Native Blender and Unity images require visual review; no automatic art promotion.']}
    dump(args.out/'meadow-prototype-overlay.json',overlay)
    if args.render:
        if prototype_only:
            if args.render_world_crops:raise ValueError('A prototype library cannot stand in for actual world crops')
            render_asset_contact(bpy,args,replacements)
        else:render_views(bpy,args,replacements)


def array_material_definition(definition):
    return {**definition,
            'colorProperties':[{'property':key,'rgba':value} for key,value in definition['unityColorsSRGB'].items()],
            'floatProperties':[{'property':key,'value':value} for key,value in definition['unityFloats'].items()]}


def adapt_overlay(args):
    overlay=json.loads(args.overlay.read_text(encoding='utf-8'))
    if overlay['schema']!=SCHEMA:raise ValueError('Wrong source overlay schema')
    overlay['materialDefinitions']=[array_material_definition(definition) for definition in overlay['materials'].values()]
    overlay['arraySchemaAdaptation']={'inputOverlay':str(args.overlay),'inputSHA256':digest(args.overlay),
                                    'geometryOrSourceChanged':False}
    for replacement in overlay['replacements']:
        source=args.overlay.parent/replacement['pmbRelativePath'];destination=args.out/replacement['pmbRelativePath']
        if digest(source)!=replacement['sha256']:raise ValueError('PMB source changed')
        destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,destination)
        if digest(destination)!=replacement['sha256']:raise ValueError('PMB copy changed')
    dump(args.out/'meadow-prototype-overlay.json',overlay)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode',choices=('plan','inspect','build','prototypes','contact','adapt-overlay'),default='plan')
    parser.add_argument('--source',type=Path,default=SOURCE)
    parser.add_argument('--bundle',type=Path,default=BASE_BUNDLE)
    parser.add_argument('--out',type=Path,default=ART_ROOT/'revision1')
    parser.add_argument('--render',action='store_true');parser.add_argument('--samples',type=int,default=24)
    parser.add_argument('--threads',type=int,default=2)
    parser.add_argument('--overlay',type=Path)
    parser.add_argument('--contact-view',choices=('all','contact_grass_flowers','contact_botanical_front','contact_botanical_rear','contact_stone_front','contact_stone_side'),default='contact_grass_flowers')
    parser.add_argument('--render-world-crops',action='store_true')
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    args=parser.parse_args(argv);args.out=args.out.resolve();args.bundle=args.bundle.resolve();args.source=args.source.resolve()
    if not args.out.is_relative_to(ART_ROOT.resolve()):raise ValueError('Output must stay inside the owned meadow artifact root')
    if args.out.exists():raise FileExistsError('Use a new candidate directory; never overwrite a previous result: '+str(args.out))
    args.out.mkdir(parents=True)
    if args.mode=='adapt-overlay':
        if args.overlay is None:raise ValueError('Array adaptation requires an existing source overlay')
        args.overlay=args.overlay.resolve();adapt_overlay(args);return
    if args.mode=='contact':
        if args.overlay is None:raise ValueError('Contact rendering requires the actual built source overlay')
        import bpy
        args.overlay=args.overlay.resolve();render_contact_from_overlay(bpy,args);return
    manifest=load_base(args);plan=write_plan(args,manifest)
    if args.mode=='plan':print(f'Prepared{len(plan)} explicit source prototype replacements; no native work performed.');return
    import bpy
    if args.mode=='inspect':inspect_native(bpy,args,manifest)
    else:build_native(bpy,args,manifest,plan,prototype_only=args.mode=='prototypes')


if __name__=='__main__':main()
