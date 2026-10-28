"""Original 62x47 world: turquoise water and continuous fractured citadel geology.

Integration API: build_world_water_cliffs(collection) -> object lists + metadata.
The hook creates only new objects/materials; it never hides or edits source zones.
Standalone --build/--render writes only world-water-cliffs-sol-high artifacts.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
from pathlib import Path
import random
import sys

ROOT=Path(__file__).resolve().parents[1]
BASE_OUT=ROOT/'.collab/artifacts/world-water-cliffs-sol-high'
OUT=BASE_OUT/'v8b-visible-physical-cascades'
MASTER_CONTEXT=ROOT/'.collab/artifacts/reference-world-master-sol-high/reference-world-art-candidate.blend'
SOURCE=ROOT/'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/Farm_Main_3D_Zone05_Candidate.blend'
RUNTIME=ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'
sys.dont_write_bytecode=True
for path in (ROOT/'Tools',RUNTIME):
    if str(path) not in sys.path:sys.path.insert(0,str(path))
import numpy as np
import bpy
from mathutils import Vector
from map_surface_contract import sample_ground_z,eval_river_center_and_width,island_signed_distance
from terrain_water_geometry import build_water,PONDS,clipped_surface
from map_mesh import MapMesh

WATER_ZONE='Zone03_Water_River_Waterfalls_And_3Ponds'
CASTLE_ZONE='Zone09_Vista_TwilightCastle_And_WaterfallCliff'
OLD_CLIFF_PREFIXES=('cliff_buttress_strata','cliff_strata_ledge','waterfall_channel_strata','east_bluff_strata','cliff_rim_capstone')
ART_VERSION='v8b-visible-physical-cascades'
PHYSICAL_STREAK_SOURCE=BASE_OUT/'liquid-study/particle-fallback/actual-frozen-water-streaks.blend'
PHYSICAL_PARTICLE_DATA=BASE_OUT/'liquid-study/particle-fallback/actual-frame30-particles.npz'


def _material(name,color,roughness=.5,water=False,attribute=False):
    material=bpy.data.materials.new(name)
    material.diffuse_color=(*color,1)
    material.use_nodes=True
    nodes=material.node_tree.nodes;links=material.node_tree.links
    shader=nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Roughness'].default_value=roughness
    shader.inputs['Metallic'].default_value=0
    shader.inputs['Specular IOR Level'].default_value=.26 if water else .3
    if water:
        shader.inputs['Coat Weight'].default_value=.18
        shader.inputs['Coat Roughness'].default_value=.2
        shader.inputs['Transmission Weight'].default_value=.05
        shader.inputs['IOR'].default_value=1.333
    if attribute:
        tint=nodes.new('ShaderNodeVertexColor');tint.layer_name='WCF_Tint'
        links.new(tint.outputs['Color'],shader.inputs['Base Color'])
    noise=nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value=5.0 if water else 3.5
    noise.inputs['Detail'].default_value=2.0
    tex=nodes.new('ShaderNodeTexCoord')
    mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY'
    mapping.inputs[1].default_value=(.65,3.2,.8) if water else (1.1,1.1,4.8)
    links.new(tex.outputs['Position'] if 'Position' in tex.outputs else tex.outputs['Generated'],mapping.inputs[0])
    links.new(mapping.outputs[0],noise.inputs['Vector'])
    bump=nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value=.16 if water else .22
    bump.inputs['Distance'].default_value=.035 if water else .065
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],shader.inputs['Normal'])
    return material


def _geology_material():
    """World-scale strata, mineral variation, two-scale relief, wetness and moss."""
    mat=bpy.data.materials.new('WCF_V2_MineralStrata_Moss_WetRock')
    mat.diffuse_color=(.27,.28,.245,1);mat.use_nodes=True
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;shader=nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value=.82;shader.inputs['Specular IOR Level'].default_value=.24
    coords=nodes.new('ShaderNodeTexCoord')
    broad=nodes.new('ShaderNodeTexNoise');broad.inputs['Scale'].default_value=1.7;broad.inputs['Detail'].default_value=4;broad.inputs['Roughness'].default_value=.72
    links.new(coords.outputs['Object'],broad.inputs['Vector'])
    ramp=nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.15;ramp.color_ramp.elements[0].color=(.12,.125,.12,1)
    ramp.color_ramp.elements[1].position=.84;ramp.color_ramp.elements[1].color=(.43,.405,.35,1)
    middle=ramp.color_ramp.elements.new(.49);middle.color=(.285,.275,.245,1)
    links.new(broad.outputs['Fac'],ramp.inputs[0])
    stretch=nodes.new('ShaderNodeVectorMath');stretch.operation='MULTIPLY';stretch.inputs[1].default_value=(.7,.7,4.3)
    links.new(coords.outputs['Object'],stretch.inputs[0])
    layers=nodes.new('ShaderNodeTexNoise');layers.inputs['Scale'].default_value=2.2;layers.inputs['Detail'].default_value=4;layers.inputs['Roughness'].default_value=.78
    links.new(stretch.outputs[0],layers.inputs['Vector'])
    layer_mix=nodes.new('ShaderNodeMixRGB');layer_mix.blend_type='MULTIPLY';layer_mix.inputs[0].default_value=.075
    links.new(ramp.outputs[0],layer_mix.inputs[1]);links.new(layers.outputs['Color'],layer_mix.inputs[2])
    geom=nodes.new('ShaderNodeNewGeometry');normal=nodes.new('ShaderNodeSeparateXYZ');links.new(geom.outputs['Normal'],normal.inputs[0])
    upward=nodes.new('ShaderNodeMath');upward.operation='GREATER_THAN';upward.inputs[1].default_value=.68;links.new(normal.outputs['Z'],upward.inputs[0])
    moss_noise=nodes.new('ShaderNodeTexNoise');moss_noise.inputs['Scale'].default_value=3.4;moss_noise.inputs['Detail'].default_value=2
    links.new(coords.outputs['Object'],moss_noise.inputs['Vector'])
    moss_threshold=nodes.new('ShaderNodeMath');moss_threshold.operation='GREATER_THAN';moss_threshold.inputs[1].default_value=.61;links.new(moss_noise.outputs['Fac'],moss_threshold.inputs[0])
    moss=nodes.new('ShaderNodeMath');moss.operation='MULTIPLY';links.new(upward.outputs[0],moss.inputs[0]);links.new(moss_threshold.outputs[0],moss.inputs[1])
    moss_mix=nodes.new('ShaderNodeMixRGB');moss_mix.inputs[2].default_value=(.095,.145,.055,1)
    links.new(moss.outputs[0],moss_mix.inputs[0]);links.new(layer_mix.outputs[0],moss_mix.inputs[1])
    wet=nodes.new('ShaderNodeAttribute');wet.attribute_name='WCF_Wetness'
    wet_mix=nodes.new('ShaderNodeMixRGB');wet_mix.inputs[2].default_value=(.055,.105,.10,1)
    links.new(wet.outputs['Fac'],wet_mix.inputs[0]);links.new(moss_mix.outputs[0],wet_mix.inputs[1]);links.new(wet_mix.outputs[0],shader.inputs['Base Color'])
    rough=nodes.new('ShaderNodeMapRange');rough.inputs['From Min'].default_value=0;rough.inputs['From Max'].default_value=1;rough.inputs['To Min'].default_value=.88;rough.inputs['To Max'].default_value=.40
    links.new(wet.outputs['Fac'],rough.inputs[0]);links.new(rough.outputs[0],shader.inputs['Roughness'])
    first_bump=nodes.new('ShaderNodeBump');first_bump.inputs['Strength'].default_value=.25;first_bump.inputs['Distance'].default_value=.045
    links.new(layers.outputs['Fac'],first_bump.inputs['Height'])
    grit=nodes.new('ShaderNodeTexNoise');grit.inputs['Scale'].default_value=38;grit.inputs['Detail'].default_value=3;grit.inputs['Roughness'].default_value=.8
    links.new(coords.outputs['Object'],grit.inputs['Vector'])
    second_bump=nodes.new('ShaderNodeBump');second_bump.inputs['Strength'].default_value=.24;second_bump.inputs['Distance'].default_value=.014
    links.new(grit.outputs['Fac'],second_bump.inputs['Height']);links.new(first_bump.outputs['Normal'],second_bump.inputs['Normal']);links.new(second_bump.outputs['Normal'],shader.inputs['Normal'])
    return mat


def _mist_material():
    mat=bpy.data.materials.new('WCF_V2_LocalImpactMist_Volume');mat.use_nodes=True
    mat.node_tree.nodes.clear();out=mat.node_tree.nodes.new('ShaderNodeOutputMaterial');volume=mat.node_tree.nodes.new('ShaderNodeVolumePrincipled')
    volume.inputs['Density'].default_value=.12;volume.inputs['Color'].default_value=(.72,.86,.84,1);volume.inputs['Anisotropy'].default_value=.1
    mat.node_tree.links.new(volume.outputs['Volume'],out.inputs['Volume'])
    return mat




def _mesh(collection,name,vertices,faces,materials,indices=None,colors=None,smooth=False):
    data=bpy.data.meshes.new(name+'_Mesh')
    data.from_pydata(vertices,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);collection.objects.link(obj)
    for material in materials:data.materials.append(material)
    if indices is not None:
        for face,index in zip(data.polygons,indices):face.material_index=index
    for face in data.polygons:face.use_smooth=smooth
    uv=data.uv_layers.new(name='UVMap')
    for loop in data.loops:
        x,y,z=data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=((x+31)/62,(y+23.5)/47)
    if colors is not None:
        layer=data.color_attributes.new(name='WCF_Tint',type='FLOAT_COLOR',domain='POINT')
        for entry,color in zip(layer.data,colors):entry.color=(*color,1)
        data.color_attributes.active_color=layer
    obj['art_pass']='world-water-cliffs-sol-high'
    return obj


def _ribbon(collection,name,points,width,material):
    vertices=[];faces=[]
    for index,p in enumerate(points):
        p=Vector(p)
        tangent=Vector(points[min(index+1,len(points)-1)])-Vector(points[max(0,index-1)])
        side=tangent.cross(Vector((0,0,1)))
        if side.length<1e-5:side=Vector((1,0,0))
        side.normalize()
        taper=.45+.55*math.sin(math.pi*(index+.3)/(len(points)-.4))
        vertices.extend((tuple(p-side*width*taper/2),tuple(p+side*width*taper/2)))
        if index:
            a=index*2;faces.extend(((a-2,a-1,a+1),(a-2,a+1,a)))
    return _mesh(collection,name,vertices,faces,[material],smooth=True)


def _rock(collection,name,center,scale,materials,seed):
    """Buried angular slabs with irregular shoulders, never ellipsoid beads."""
    rng=random.Random(seed);n=7;vertices=[]
    yaw=rng.uniform(-math.pi,math.pi);ca,sa=math.cos(yaw),math.sin(yaw)
    for row,(height,amount) in enumerate(((-.6,.76),(-.1,1.0),(.44,.78),(.62,.47))):
        for i in range(n):
            a=math.tau*i/n
            radius=amount*(1+rng.uniform(-.16,.12))
            x=scale[0]*radius*math.cos(a);y=scale[1]*radius*math.sin(a)
            vertices.append((center[0]+ca*x-sa*y,center[1]+sa*x+ca*y,center[2]+scale[2]*(height+rng.uniform(-.07,.07))))
    faces=[tuple(range(n-1,-1,-1)),tuple(range(3*n,4*n))]
    for row in range(3):
        for i in range(n):
            a=row*n+i;b=row*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    indices=[rng.randrange(len(materials)) for face in faces]
    return _mesh(collection,name,vertices,faces,materials,indices)


def _select_source_parts(collection,source,predicate,name,material,color_fn=None,flatten_ponds=False):
    vertices,uvs,faces=source.arrays()
    selected=[faces[p['first_triangle']:p['first_triangle']+p['triangles']] for p in source.parts if predicate(p['name'])]
    if not selected:return None
    selected=np.vstack(selected)
    used=np.unique(selected);inverse=np.full(len(vertices),-1,dtype=int);inverse[used]=np.arange(len(used))
    verts=vertices[used]
    if flatten_ponds:
        for cx,cy,rx,ry,degrees,level in PONDS:
            angle=math.radians(degrees);ca,sa=math.cos(angle),math.sin(angle)
            dx,dy=verts[:,0]-cx,verts[:,1]-cy
            distance=((dx*ca+dy*sa)/rx)**2+((-dx*sa+dy*ca)/ry)**2
            verts[(distance<1.5)&(np.abs(verts[:,2]-level)<.025),2]=level
    mapped=inverse[selected]
    corrected=0
    if flatten_ponds:
        # Source add_pond's central fan is CCW but all annular rings are CW.
        # With identical planar Z/tint, opposite normals produced a dark center
        # oval. Orient every selected open water triangle toward sky (+Z).
        tri=verts[mapped]
        down=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])[:,2]<0
        corrected=int(down.sum());mapped=mapped.copy();mapped[down]=mapped[down][:,[0,2,1]]
    colors=[color_fn(v) for v in verts] if color_fn else None
    obj=_mesh(collection,name,verts,mapped,[material],colors=colors,smooth=True)
    if flatten_ponds:obj['water_surface_winding_corrected_triangles']=corrected
    return obj


def _water_tint(v):
    x,y,z=[float(a) for a in v]
    depth=max(0,z-float(sample_ground_z(x,y)))
    deep=min(1,depth/.85)
    shallow=np.array((.035,.37,.285));dark=np.array((.015,.145,.23))
    return shallow*(1-deep)+dark*deep


def _build_water(collection,materials):
    generated=build_water()
    water=[]
    water.append(_select_source_parts(collection,generated,lambda n:n=='river_surface' or (n.startswith('pond_') and n.endswith('_surface')),'WCF_Zone03_DeepTurquoiseWater',materials['water'],_water_tint,flatten_ponds=True))
    water.append(_select_source_parts(collection,generated,lambda n:n.startswith('reed_'),'WCF_Zone03_BankReeds',materials['reeds']))
    rng=random.Random(7631)
    for pond,(cx,cy,rx,ry,degrees,level) in enumerate(PONDS):
        angle=math.radians(degrees);ca,sa=math.cos(angle),math.sin(angle)
        # A few irregular grouped shore outcrops; leave most banks uninterrupted.
        for group,theta in enumerate((.35,2.35,4.25)):
            for j in range(2+(group%2)):
                a=theta+(j-1)*.14+rng.uniform(-.06,.06)
                lo,hi=.75,1.28
                for _ in range(16):
                    r=(lo+hi)/2;lx,ly=rx*r*math.cos(a),ry*r*math.sin(a)
                    x,y=cx+lx*ca-ly*sa,cy+lx*sa+ly*ca
                    if float(sample_ground_z(x,y))<level:lo=r
                    else:hi=r
                r=hi+.018;lx,ly=rx*r*math.cos(a),ry*r*math.sin(a)
                x,y=cx+lx*ca-ly*sa,cy+lx*sa+ly*ca;z=float(sample_ground_z(x,y))
                s=rng.uniform(.15,.31)
                water.append(_rock(collection,f'WCF_BankOutcrop_P{pond}_{group}_{j}',(x,y,z+.04),(s*1.4,s,.22),materials['wetrock'],800+pond*20+group*3+j))
    for i in range(24):
        y=rng.uniform(-6.2,10.7);center,width=eval_river_center_and_width(y)
        x=float(center)+rng.uniform(-.60,.55)*float(width)
        if float(island_signed_distance(x,y))>.92 or float(sample_ground_z(x,y))>1.75:continue
        z=2+.08*(y+7.4)/19.6+.018
        points=[(x+.06*math.sin(t*math.pi),y+t*.28,z) for t in np.linspace(-1,1,9)]
        water.append(_ribbon(collection,f'WCF_RiverDirectionalGlint_{i}',points,.012,materials['glint']))
    return [o for o in water if o]


def _cliff_front(x,t):
    """Unequal eroded bays, tilted ledges and short branching joints at three scales."""
    y=float(np.interp(x,[4.7,6.5,9,12,15,18,21,24,25.6],[12.7,11.8,11.45,11.55,11.35,11.2,10.95,10.5,10.0]))
    y+=-.46*(1-t)**1.7+1.08*t*t
    y+=.18*(2/math.pi)*math.asin(math.sin(x*.50+t*3.4))+.18*math.tanh(2.5*math.sin(x*.83-t*1.8))
    y+=.11*math.sin(x*.31-t*3.4)
    for at,level,width,height,depth in [(5.8,.43,1.2,.16,.29),(10.0,.30,1.1,.13,.24),(13.0,.65,1.5,.14,.28),(16.4,.37,.9,.12,.26),(19.5,.61,1.6,.17,.32),(22.0,.31,1.7,.15,.38),(24.3,.73,.8,.13,.24)]:
        wx=max(0,1-abs((x-at)/width));wz=max(0,1-abs((t-level)/height))
        y-=depth*(wx*wz)**.65
    z=1.55+5.7*t
    y+=.045*math.sin(x*5.1+z*2.4)*math.cos(x*2.6-z*4.1)+.018*math.sin(x*16-z*11)
    for layer in (.12,.28,.46,.69,.87):
        bed=layer+.028*math.sin(x*.57+layer*13)+.0015*x
        y+=.055*math.tanh((t-bed)*72)
    fissure=0.
    for at,depth,width,low,high,slope in [(6.1,.23,.095,.02,.63,1.3),(9.55,.28,.08,.31,.97,-.9),(12.65,.22,.075,.12,.72,.8),(15.85,.24,.09,.48,.99,-1.1),(19.65,.25,.12,.02,.62,-.7),(22.6,.28,.10,.25,.87,1.2),(24.1,.16,.07,.60,.98,-.9)]:
        center=at+slope*(t-.4)+.075*math.sin(t*16+at)
        fade=max(0,min(1,(t-low)*18))*max(0,min(1,(high-t)*18))
        value=math.exp(-((x-center)/width)**2)*fade
        y+=depth*value;fissure=max(fissure,value)
    return y,fissure


def _build_cliffs(collection,materials):
    nx,nz=183,89;vertices=[];wetness=[]
    for row in range(nz):
        t=row/(nz-1)
        for col in range(nx):
            x=4.7+(25.6-4.7)*col/(nx-1)
            if col not in (0,nx-1):x+=.012*math.sin(col*5.3+row*2.9)
            zbase=1.55+.08*math.sin(x*.8)
            top=7.26+.13*math.sin(x*.55)+.07*math.cos(x*1.8)
            if x>21:top-=.22*(x-21)
            z=zbase+(top-zbase)*t
            if row not in (0,nz-1):z+=.012*math.sin(col*1.7+row*4.5)
            y,fissure=_cliff_front(x,t)
            vertices.append((x,y,z))
            wet=max(math.exp(-((x-cx)/(.47+.38*(1-t)))**2) for cx in (8.2,11.4,14.8,17.8))
            wetness.append(min(.72,wet*.65+.22*(1-t)**3))
    faces=[];indices=[]
    for row in range(nz-1):
        for col in range(nx-1):
            a=row*nx+col;b=a+1;c=a+nx+1;d=a+nx
            tris=((a,b,c),(a,c,d)) if (row+col)%2 else ((a,b,d),(b,c,d))
            for tri in tris:
                faces.append(tri)
                x=sum(vertices[i][0] for i in tri)/3;t=(row+.5)/(nz-1)
                indices.append(0)
    # Close into a solid continuous mass behind the visible face.
    edge=[col for col in range(nx)]+[row*nx+nx-1 for row in range(1,nz)]+[(nz-1)*nx+col for col in range(nx-2,-1,-1)]+[row*nx for row in range(nz-2,0,-1)]
    back=[]
    for i in edge:
        x,y,z=vertices[i];back.append(len(vertices));vertices.append((x,max(y+1.2,14.35),z));wetness.append(.08)
    for k,a in enumerate(edge):
        next_k=(k+1)%len(edge);faces.append((a,back[k],back[next_k],edge[next_k]));indices.append(0)
    faces.append(tuple(reversed(back)));indices.append(3)
    obj=_mesh(collection,'WCF_Zone09_ContinuousFracturedEscarpment',vertices,faces,[materials['geology']],smooth=True)
    wet_layer=obj.data.attributes.new('WCF_Wetness','FLOAT','POINT')
    for element,value in zip(wet_layer.data,wetness):element.value=value
    objects=[obj]
    # Unequal talus groups sit half-buried at the foot and leave waterfall mouths open.
    for i,(x,scale) in enumerate([(5.7,1.0),(6.6,.65),(9.6,.85),(12.9,1.15),(16.35,.75),(19.5,1.2),(21.5,.9),(23.8,1.15)]):
        y,_=_cliff_front(x,.04)
        rock=_rock(collection,f'WCF_CliffToeBuriedSlab_{i}',(x,y+.06,1.90),(.80*scale,.54*scale,.52*scale),[materials['geology']],910+i)
        _soften_rock(rock,.05);objects.append(rock)
    return objects


def _soften_rock(obj,width):
    for face in obj.data.polygons:face.use_smooth=True
    bevel=obj.modifiers.new('SmallErodedEdges','BEVEL');bevel.width=width;bevel.segments=3
    normal=obj.modifiers.new('AreaWeightedRockNormals','WEIGHTED_NORMAL');normal.keep_sharp=True;normal.weight=35


def _flow_position(cx,width,t,branch):
    center=cx+.12*math.sin(t*7.2+cx)+.025*math.sin(t*16.3+cx)
    if branch:
        start,end=(.16,.78) if branch<0 else (.35,.96)
        u=max(0,min(1,(t-start)/(end-start)))
        center+=branch*width*.34*math.sin(u*math.pi)
    z=7.39-5.30*t
    base=1.55+.08*math.sin(center*.8)
    top=7.26+.13*math.sin(center*.55)+.07*math.cos(center*1.8)
    front,_=_cliff_front(center,max(0,min(1,(z-base)/(top-base))))
    ideal=12.48-1.42*t**.7
    return Vector((center,min(ideal,front-.16),z))




def _splash_mesh(collection,name,center,scale,material,seed,count=10):
    rng=random.Random(seed);vertices=[];faces=[]
    for droplet in range(count):
        c=Vector(center)+Vector((rng.uniform(-scale[0],scale[0]),rng.uniform(-scale[1],scale[1]),rng.uniform(0,scale[2])))
        radius=rng.uniform(.008,.026);base=len(vertices)
        for offset in [(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1.7),(0,0,-1.1)]:vertices.append(tuple(c+Vector(offset)*radius))
        faces.extend(tuple(base+i for i in tri) for tri in [(0,2,4),(2,1,4),(1,3,4),(3,0,4),(2,0,5),(1,2,5),(3,1,5),(0,3,5)])
    return _mesh(collection,name,vertices,faces,[material],smooth=True)




def _build_aerated_cascades(collection,materials):
    """Stochastic overlapping short water volumes; no hero curtain/strap surfaces."""
    objects=[]
    palette=[_material('WCF_V5_AeratedWater_'+str(i),color,.29) for i,color in enumerate(((.18,.43,.50),(.27,.53,.59),(.41,.65,.69),(.57,.75,.76),(.74,.85,.84)))]
    for index,(cx,width) in enumerate(zip((8.2,11.4,14.8,17.8),(1.35,1.55,1.65,1.20))):
        rng=random.Random(55210+index);vertices=[];faces=[];indices=[]
        for branch,count,lo,hi in [(0,560,0,1),(-1,235,.12,.80),(1,230,.32,.99)]:
            for droplet in range(count):
                t=rng.uniform(lo,hi)
                dt=rng.uniform(.007,.025)
                p=_flow_position(cx,width,max(0,t-dt),branch)
                q=_flow_position(cx,width,min(1,t+dt),branch)
                spread=(.08 if branch else .115)*width*(.8+.4*t)
                lateral=max(-spread*2,min(spread*2,rng.gauss(0,spread)))
                toward=rng.uniform(-.085,.025)
                p.x+=lateral;q.x+=lateral;p.y+=toward;q.y+=toward
                for point in (p,q):
                    base=1.55+.08*math.sin(point.x*.8);top=7.26+.13*math.sin(point.x*.55)+.07*math.cos(point.x*1.8)
                    front,_=_cliff_front(point.x,max(0,min(1,(point.z-base)/(top-base))))
                    point.y=min(point.y,front-.085)
                direction=q-p
                if direction.length<.012:continue
                side=direction.cross(Vector((0,1,0))).normalized();normal=direction.normalized().cross(side)
                radius=rng.uniform(.013,.038)*(1+.25*t)
                first=len(vertices);vertices.append(tuple(p))
                for along,amount in ((.18,.7),(.49,1.0),(.82,.55)):
                    center=p+direction*along
                    for k in range(6):
                        a=math.tau*k/6
                        vertices.append(tuple(center+side*(math.cos(a)*radius)+normal*(math.sin(a)*radius*.60)))
                end=len(vertices);vertices.append(tuple(q))
                local=[]
                for k in range(6):local.append((first,first+1+(k+1)%6,first+1+k))
                for row in range(2):
                    for k in range(6):
                        a=first+1+row*6+k;b=first+1+row*6+(k+1)%6;local.append((a,b,b+6,a+6))
                for k in range(6):local.append((first+13+k,first+13+(k+1)%6,end))
                contact=max(math.exp(-((t-v)/.050)**2) for v in (.285,.59,.83,.965))
                shade=rng.choices([0,1,2,3,4],[1,3,4,2+contact*3,contact*7+.1])[0]
                faces.extend(local);indices.extend([shade]*len(local))
        objects.append(_mesh(collection,f'WCF_V5_AiryCascade_{index}',vertices,faces,palette,indices,smooth=True))
        for contact,t in enumerate((.285,.59,.83,.965)):
            p=_flow_position(cx,width,t,0)
            objects.append(_splash_mesh(collection,f'WCF_V5_ContactMicroSpray_{index}_{contact}',p+Vector((0,-.08,-.03)),(.26,.10,.18),materials['foam'],2310+index*6+contact,18))
        impact=_flow_position(cx,width,1,0)
        objects.append(_splash_mesh(collection,f'WCF_V5_ImpactSpray_{index}',impact+Vector((0,-.22,0)),(.45,.30,.48),materials['foam'],2500+index,36))
        mist=_rock(collection,f'WCF_V5_ImpactMist_{index}',(impact.x,impact.y-.14,2.26),(.63,.40,.30),[materials['mist']],2600+index)
        objects.append(mist)
        for arc in range(2):
            radius=.35+arc*.30
            points=[(impact.x+radius*math.cos(a),impact.y-.18+radius*.55*math.sin(a),2.10) for a in np.linspace(math.pi+.25,math.pi+2.0,19)]
            objects.append(_ribbon(collection,f'WCF_V5_ImpactRipple_{index}_{arc}',points,.018,materials['glint']))
    return objects,[]




def _build_ragged_water_web(collection,materials):
    """A closed thin aerated web with true branch gaps and irregular broken edges."""
    body=_material('WCF_V6_AeratedBlueWhiteWater',(.42,.64,.68),.43,False,True)
    shader=body.node_tree.nodes.get('Principled BSDF');shader.inputs['Specular IOR Level'].default_value=.22
    for node in body.node_tree.nodes:
        if node.type=='BUMP':node.inputs['Strength'].default_value=.12;node.inputs['Distance'].default_value=.006
    objects=[]
    for index,(cx,width) in enumerate(zip((8.2,11.4,14.8,17.8),(1.35,1.55,1.65,1.20))):
        nr,nc=167,49;raw=[];field=[]
        for row in range(nr):
            t=row/(nr-1)
            for col in range(nc):
                u=col/(nc-1)*2-1
                core=.12*math.sin(t*7.2+cx)
                centers=[(core,.29)]
                if .12<t<.81:centers.append((core-.65*math.sin((t-.12)/.69*math.pi),.18))
                if .31<t<.995:centers.append((core+.60*math.sin((t-.31)/.685*math.pi),.16))
                signal=max(math.exp(-((u-a)/b)**2) for a,b in centers)
                n=.14*math.sin(u*71+t*43+index)+.09*math.sin(u*143-t*129)+.05*math.sin(u*269+t*219)
                value=signal+n-.37
                # Sparse genuine openings are small and irregular, never rectangular cuts.
                hole=math.exp(-((u-.20*math.sin(t*31+index))/.075)**2)*max(0,math.sin(t*89+index)-.62)*1.5
                value-=hole
                x=cx+u*width*.62
                z=7.39-5.30*t
                base=1.55+.08*math.sin(x*.8);top=7.26+.13*math.sin(x*.55)+.07*math.cos(x*1.8)
                front,_=_cliff_front(x,max(0,min(1,(z-base)/(top-base))))
                y=min(12.48-1.42*t**.7,front-.12)+.004*math.sin(u*45+t*117)
                raw.append((x,y,z));field.append(value)
        triangles=[]
        for row in range(nr-1):
            for col in range(nc-1):
                a=row*nc+col;triangles.extend(((a,a+nc+1,a+1),(a,a+nc,a+nc+1)))
        clipped=MapMesh();vv=np.asarray(raw);ff=np.asarray(triangles);uv=np.zeros((len(vv),2))
        clipped_surface(clipped,'aerated_web',vv,uv,ff,np.asarray(field))
        cv,_,cf=clipped.arrays()
        front,inverse=np.unique(np.round(cv,6),axis=0,return_inverse=True);front_faces=inverse[cf]
        count=len(front);vertices=[tuple(v) for v in front]+[(float(v[0]),float(v[1]+.018),float(v[2])) for v in front]
        faces=[tuple(f) for f in front_faces]+[tuple(int(i)+count for i in reversed(f)) for f in front_faces]
        from collections import Counter
        edges=Counter(tuple(sorted((int(f[i]),int(f[(i+1)%3])))) for f in front_faces for i in range(3))
        for (a,b),uses in edges.items():
            if uses==1:faces.append((a,b,b+count,a+count))
        colors=[]
        for x,y,z in vertices:
            t=(7.39-z)/5.3;u=(x-cx)/(width*.62)
            noise=.5+.27*math.sin(u*55+t*26+index)+.14*math.sin(u*117-t*83)
            contact=max(math.exp(-((t-v)/.055)**2) for v in (.285,.59,.83,.965))
            froth=max(0,min(1,noise*.75+contact*.32))
            blue=np.array((.16,.38,.45));white=np.array((.73,.84,.83))
            colors.append(blue*(1-froth)+white*froth)
        objects.append(_mesh(collection,f'WCF_V6_BrokenAeratedWeb_{index}',vertices,faces,[body],colors=colors,smooth=True))
        impact=_flow_position(cx,width,1,0)
        objects.append(_splash_mesh(collection,f'WCF_V6_ImpactMicroSpray_{index}',impact+Vector((0,-.15,0)),(.40,.24,.34),materials['foam'],2900+index,30))
        mist=_rock(collection,f'WCF_V6_SparseImpactMist_{index}',(impact.x,impact.y-.12,2.24),(.6,.38,.28),[materials['mist']],2990+index)
        objects.append(mist)
        for arc in range(2):
            radius=.35+arc*.30
            points=[(impact.x+radius*math.cos(a),impact.y-.18+radius*.55*math.sin(a),2.10) for a in np.linspace(math.pi+.25,math.pi+2.0,19)]
            objects.append(_ribbon(collection,f'WCF_V6_ImpactRipple_{index}_{arc}',points,.018,materials['glint']))
    return objects,[]


def _build_physical_particle_flow(collection,materials):
    """Cache-independent geometry swept from actual Newton positions/velocities."""
    if not PHYSICAL_STREAK_SOURCE.is_file() or not PHYSICAL_PARTICLE_DATA.is_file():
        raise RuntimeError('Native physical particle study is missing; refusing rejected web fallback')
    with bpy.data.libraries.load(str(PHYSICAL_STREAK_SOURCE),link=False) as (available,loaded):
        loaded.objects=[name for name in available.objects if name=='FROZEN_ACTUAL_PARTICLE_MOTION_STREAKS']
    if len(loaded.objects)!=1:raise RuntimeError('Physical streak library has no frozen native object')
    template=loaded.objects[0];source=template.data
    source_coords=np.empty(len(source.vertices)*3,dtype=np.float32);source.vertices.foreach_get('co',source_coords);source_coords=source_coords.reshape(-1,3)
    particle_data=np.load(PHYSICAL_PARTICLE_DATA)
    if len(source_coords)!=len(particle_data['positions'])*20:raise RuntimeError('Physical mesh and simulated particle provenance differ')
    exposure=np.asarray([entry.value for entry in source.attributes['MotionExposure'].data],dtype=np.float32)
    # Sample the unchanged cliff once; projected optical drops maintain real air depth.
    gx=np.linspace(4,27,384);gz=np.linspace(1.5,7.6,160)
    front=np.empty((len(gz),len(gx)),dtype=np.float32)
    for row,z in enumerate(gz):
        for col,x in enumerate(gx):
            base=1.55+.08*math.sin(x*.8);top=7.26+.13*math.sin(x*.55)+.07*math.cos(x*1.8)-.22*max(0,x-21)
            front[row,col]=_cliff_front(float(x),max(0,min(1,(z-base)/(top-base))))[0]
    def front_at(x,z):
        ix=np.clip((x-gx[0])/(gx[-1]-gx[0])*(len(gx)-1),0,len(gx)-1.001)
        iz=np.clip((z-gz[0])/(gz[-1]-gz[0])*(len(gz)-1),0,len(gz)-1.001)
        a=ix.astype(int);b=iz.astype(int);fx=ix-a;fz=iz-b
        return front[b,a]*(1-fx)*(1-fz)+front[b,a+1]*fx*(1-fz)+front[b+1,a]*(1-fx)*fz+front[b+1,a+1]*fx*fz
    # Each cascade retains actual frozen simulated droplets, with a different
    # stream width/flow and advected phase. No mouth surface or opaque sheet.
    optical_materials=[]
    for original in source.materials:
        material=original.copy();material.name='WCF_V8_TransparentLiquidExposure'
        for node in material.node_tree.nodes:
            if node.type=='BSDF_PRINCIPLED':
                node.inputs['Base Color'].default_value=(.51,.73,.77,1)
                node.inputs['Roughness'].default_value=.17
        optical_materials.append(material)
    objects=[];contact_rocks=[];nparticles=len(particle_data['positions'])
    flow_scales=(.6,.9,1.3,.5)
    for index,(cx,flow) in enumerate(zip((8.2,11.4,14.8,17.8),flow_scales)):
        width=flow
        fraction=.43*math.sqrt(flow)
        rng=np.random.default_rng(97200+index)
        chosen=np.sort(rng.choice(nparticles,int(nparticles*fraction),replace=False))
        ids=(chosen[:,None]*20+np.arange(20)[None,:]).reshape(-1)
        coords=source_coords[ids].copy();local=coords.copy()
        # The study's elastic solid-particle rebound rises almost two metres.
        # Reflect that collision-derived arc into a dissipated downstream spill
        # around its real contact, retaining every swept drop and its exposure.
        # This is a static art transform of native water, not a new simulation.
        rebounds=np.abs(particle_data['velocities'][chosen,0])>1
        rebound_vertices=np.repeat(rebounds,20)
        local[rebound_vertices,2]=1.12-(local[rebound_vertices,2]-1.12)*.48
        local[rebound_vertices,0]*=.62
        t=np.clip(1-local[:,2]/5.6,0,1);mirror=-1 if index%2 else 1
        scale_x=width/.65
        phase=(.3,2.1,4.8,1.2)[index]
        lateral=.065*np.sin(t*(7+index*.9)+phase)*np.sin(t*math.pi)
        coords[:,0]=cx+mirror*local[:,0]*scale_x+lateral
        coords[:,2]=2.08+np.clip(local[:,2],0,5.6)*(5.30/5.6)
        top_y=12.6-.15*abs(index-1.5);bottom_y=11.2-.15*abs(index-1.5)
        # The upper droplets turn over a wet lip on an arcing trajectory rather
        # than emerging below an equal oval cap. The native Z endpoints stay put.
        lip=np.clip(1-t/.14,0,1)
        target_y=top_y-(top_y-bottom_y)*t**.70+local[:,1]*.22+.38*lip**2
        coords[:,1]=np.minimum(target_y,front_at(coords[:,0],coords[:,2])-.065-np.abs(local[:,1]-.04)*.10)
        # A small upstream arc remains outside the exposed rock face. Its upper
        # horizontal spread comes from the native emitter, not a filled polygon.
        coords[:,1]-=.12*np.sin(np.clip(t/.14,0,1)*math.pi)*lip
        faces=[]
        for particle in range(len(chosen)):
            base=particle*20
            for k in range(6):faces.append((base,base+1+(k+1)%6,base+1+k))
            for row in range(2):
                for k in range(6):
                    a=base+1+row*6+k;b=base+1+row*6+(k+1)%6;faces.append((a,b,b+6,a+6))
            for k in range(6):faces.append((base+13+k,base+13+(k+1)%6,base+19))
        mesh=bpy.data.meshes.new(f'WCF_V8_PhysicalCascade_{index}_Mesh');mesh.from_pydata(coords,[],faces);mesh.update()
        obj=bpy.data.objects.new(f'WCF_V8_PhysicalCascade_{index}',mesh);collection.objects.link(obj)
        for material in optical_materials:mesh.materials.append(material)
        for face in mesh.polygons:face.use_smooth=True
        # Low optical coverage leaves wet geology visible between fine streams.
        # Feathering applies to individual drops, never to a perforated curtain.
        centroids=particle_data['positions'][chosen]
        edge=np.clip(1-np.abs(centroids[:,0]-.015)/.52,.12,1)
        airborne=np.clip((centroids[:,2]-.15)/1.2,.30,1)
        density=np.repeat((.62+.15*edge)*airborne,20)
        alpha=mesh.attributes.new('MotionExposure','FLOAT','POINT');values=np.minimum(.62,exposure[ids]/fraction*density)
        alpha.data.foreach_set('value',values)
        velocity=particle_data['velocities'][chosen].copy();velocity[rebounds,0]*=.62;velocity[rebounds,2]*=-.48
        velocity[:,0]*=scale_x*mirror;velocity[:,1]*=.22;velocity[:,2]*=5.30/5.6
        motion=mesh.attributes.new('WCF_PhysicalVelocity','FLOAT_VECTOR','POINT');motion.data.foreach_set('vector',np.repeat(velocity,20,axis=0).reshape(-1))
        obj.visible_shadow=False;obj['art_pass']='world-water-cliffs-sol-high';obj['physical_source']='Actual native Newton frame30; swept geometry preserves 0.65/24s exposure across reload';obj['collision_arc_transform']='Native elastic rebound statically reflected down about local Z1.12, vertical scale-.48, lateral scale.62; velocity transformed consistently';obj['native_top_z']=7.38;obj['native_bottom_z']=2.08;obj['flow_relative']=flow;obj['phase']=phase
        objects.append(obj)
        # The physical study contains a bounce at local Z~1.05. Put an actual
        # sloping, buried rock shoulder beneath that deflection in world space.
        contact_t=1-1.05/5.6
        contact_x=cx+.065*math.sin(contact_t*(7+index*.9)+phase)*math.sin(contact_t*math.pi)
        contact_z=2.08+1.05*5.30/5.6
        face_y=float(front_at(np.array([contact_x]),np.array([contact_z]))[0])
        cache_path=ROOT/'Tools/quaternius_nature_cache.npz'
        with np.load(cache_path) as cache:
            key='Rock_'+str(1+index%5);v=cache[key+'_v'].astype(float).copy();f=cache[key+'_f']
        lo=v.min(axis=0);hi=v.max(axis=0)
        v=(v-(lo+hi)/2)/(hi-lo)
        # Front shoulder is exposed beneath the contact, rear half intersects
        # the existing mass. Different native fractured rocks avoid bead forms.
        v*=np.array((.85+width*.65,1.0,.68))
        v+=np.array((contact_x,face_y+.11,contact_z-.30))
        v[:,2]+=.18*(v[:,1]-face_y)-.08*mirror*(v[:,0]-contact_x)
        material=materials['wetrock'][1]
        # Reuse a COPY of the root's already reviewed image rock shader, keeping
        # its PBR images and darkening only this wet contact without root writes.
        context_rock=next((o for o in bpy.data.objects if o.name.startswith('WCF_Zone09_Continuous') and o.type=='MESH' and len(o.data.materials)),None)
        if context_rock:
            material=context_rock.data.materials[0].copy();material.name=f'WCF_V8_DarkWetContactRock_{index}'
            shader=next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
            if shader:
                for link in list(shader.inputs['Roughness'].links):material.node_tree.links.remove(link)
                shader.inputs['Roughness'].default_value=.41
                if shader.inputs['Base Color'].is_linked:
                    old=shader.inputs['Base Color'].links[0].from_socket
                    multiply=material.node_tree.nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=.68;multiply.inputs[2].default_value=(.31,.40,.39,1)
                    material.node_tree.links.new(old,multiply.inputs[1]);material.node_tree.links.new(multiply.outputs[0],shader.inputs['Base Color'])
        rock=_mesh(collection,f'WCF_V8_FanContactLedge_{index}',v,f,[material],smooth=True)
        uv_names={'UVMap'}|{n.uv_map for n in material.node_tree.nodes if n.type=='UVMAP' and n.uv_map}
        for uv_name in uv_names:
            uv=rock.data.uv_layers.get(uv_name) or rock.data.uv_layers.new(name=uv_name)
            for loop in rock.data.loops:
                x,y,z=rock.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(x*.16,z*.16)
        rock['native_source_cache']=key
        rock['physical_contact_local_z']=1.05;rock['supports_physical_fan']=obj.name
        contact_rocks.append(rock)
        impact=Vector((cx,bottom_y,2.08));impact.y=min(impact.y,float(front_at(np.array([cx]),np.array([2.08]))[0])-.12)
        mist=_rock(collection,f'WCF_V8_LocalImpactAir_{index}',(cx,impact.y-.18,2.27),(.45*math.sqrt(flow),.30,.20),[materials['mist']],3190+index);mist.visible_shadow=False;objects.append(mist)
        objects.append(_splash_mesh(collection,f'WCF_V8_ImpactMicrofoam_{index}',impact+Vector((0,-.12,0)),(.25*math.sqrt(flow),.18,.23),materials['foam'],3290+index,22))
    bpy.data.objects.remove(template,do_unlink=True)
    return objects,contact_rocks


def should_exclude_legacy_pond_detail(obj_or_name):
    """Pure root-assembler filter for old wire ripples/center oval props."""
    name=str(getattr(obj_or_name,'name',obj_or_name)).casefold()
    return 'pond' in name and any(token in name for token in ('ripple','wire','center_oval','centre_oval','oval_bump','center_bump'))


def build_world_water_cliffs(collection):
    """Create separate replacement-water and replacement-cliff objects only."""
    if collection is None:raise ValueError('Pass an explicit destination collection')
    materials={
        'water':_material('WCF_Water_DeepTurquoise_VertexDepth',(.025,.24,.29),.25,True,True),
        'foam':_material('WCF_V2_Foam_SmallContactOnly',(.56,.75,.71),.72),
        'mist':_mist_material(),
        'geology':_geology_material(),
        'glint':_material('WCF_Glint_MutedJade',(.12,.44,.42),.32),
        'reeds':_material('WCF_Reeds_DeepOlive',(.12,.20,.045),.78),
        'cliff':[_material('WCF_Cliff_'+name,color,.83) for name,color in [('WarmSlate',(.24,.24,.21)),('PaleStone',(.34,.32,.26)),('WeatheredOchre',(.27,.24,.185)),('DeepFissure',(.115,.14,.14))]],
        'wetrock':[_material('WCF_WetRock_'+str(i),color,.57) for i,color in enumerate(((.13,.17,.16),(.19,.23,.20),(.26,.27,.23)))]}
    water=_build_water(collection,materials)
    cliffs=_build_cliffs(collection,materials)
    cascade_water,cascade_cliffs=_build_physical_particle_flow(collection,materials)
    water+=cascade_water;cliffs+=cascade_cliffs
    return {'water_objects':water,'cliff_objects':cliffs,'all_objects':water+cliffs,'metadata':{'art_version':ART_VERSION,'coordinate_contract':'original62x47 Blender Z-up; identity world coordinates','replacement_zone03':WATER_ZONE,'cliff_only_zone09':True,'castle_geometry_modified':False,'water_pond_specs':PONDS,'fall_centers_x':[8.2,11.4,14.8,17.8],'fall_relative_width_and_flow':[.6,.9,1.3,.5],'flat_feeder_caps':False,'physical_fan_contact_rocks':4,'old_cliff_part_prefixes':OLD_CLIFF_PREFIXES,'supersedes_rejected':'v1-v6 strips; v7 equal columns/oval mouths/unbacked collision fans; do not reuse','physical_source':str(PHYSICAL_STREAK_SOURCE),'motion_retention':'Cache-independent swept geometry plus MotionExposure and WCF_PhysicalVelocity point attributes; no 36-frame simulation required','required_cycles_transparent_max_bounces':64,'preserve_materials':'Keep WCF_V8_TransparentLiquidExposure optical shader on WCF_V8_PhysicalCascade objects; do not replace with pond/PBR albedo material','pond_wire_ripples':False,'pond_center_geometry_bump':False,'visual_status':'CANDIDATE_REQUIRES_VISUAL_REVIEW'}}


def _preserve_baseline_castle_without_old_rock(source,collection):
    """Standalone preview extracts high architectural/tree components unchanged.

    Castle and trees sit above7.1; original rock primitives below7.36 are removed
    by connected-component bounds. This never runs in the integration API hook.
    """
    data=source.data;parent=list(range(len(data.vertices)))
    def find(a):
        while parent[a]!=a:parent[a]=parent[parent[a]];a=parent[a]
        return a
    def union(a,b):
        a,b=find(a),find(b)
        if a!=b:parent[b]=a
    for edge in data.edges:union(*edge.vertices)
    components={}
    for vertex in data.vertices:components.setdefault(find(vertex.index),[]).append(vertex.index)
    kept=set()
    for ids in components.values():
        coords=[data.vertices[i].co for i in ids]
        minimum=min(p.z for p in coords);maximum=max(p.z for p in coords)
        # Baseline castle walls and all tree stems/canopies start at/above7.1.
        if minimum>=7.09 and maximum>7.38:kept.update(ids)
    polygons=[p for p in data.polygons if all(i in kept for i in p.vertices)]
    used=sorted({i for p in polygons for i in p.vertices});lookup={i:n for n,i in enumerate(used)}
    vertices=[data.vertices[i].co[:] for i in used];faces=[[lookup[i] for i in p.vertices] for p in polygons]
    obj=_mesh(collection,'WCF_PreservedOriginalCitadelAndRimTrees',vertices,faces,list(data.materials),[p.material_index for p in polygons])
    # Preserve original atlas coordinates loop-for-loop.
    if data.uv_layers.active:
        for new_poly,old_poly in zip(obj.data.polygons,polygons):
            for new_loop,old_loop in zip(new_poly.loop_indices,old_poly.loop_indices):obj.data.uv_layers.active.data[new_loop].uv=data.uv_layers.active.data[old_loop].uv
    obj.matrix_world=source.matrix_world.copy()
    return obj,{'original_components':len(components),'preserved_vertices':len(used),'preserved_faces':len(polygons),'selection':'component minimumZ>=7.09 and maximumZ>7.38; original local vertices/material indices/UV unchanged'}


def _camera(name,location,target,lens=48):
    data=bpy.data.cameras.new(name);obj=bpy.data.objects.new(name,data)
    bpy.context.scene.collection.objects.link(obj);obj.location=location
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler();data.lens=lens
    return obj


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--build',action='store_true');parser.add_argument('--render',action='store_true');parser.add_argument('--samples',type=int,default=24)
    parser.add_argument('--view',choices=('all','full','pond','cliff','side','quick-cliff'),default='all');parser.add_argument('--resolution',type=int,default=1280);parser.add_argument('--threads',type=int,default=4)
    parser.add_argument('--context',choices=('master','baseline'),default='master');parser.add_argument('--cuda',action='store_true');args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    if not args.build and not args.render:raise SystemExit('Use --build and/or --render')
    if args.build:
        context_source=MASTER_CONTEXT if args.context=='master' and MASTER_CONTEXT.exists() else SOURCE
        bpy.ops.wm.open_mainfile(filepath=str(context_source),load_ui=False,use_scripts=False)
        bpy.context.preferences.filepaths.use_scripts_auto_execute=False
        collection=bpy.data.collections.new('WCF_WaterAndNaturalCitadelCliffs_Candidate');bpy.context.scene.collection.children.link(collection)
        context_cliffs={obj.name.split('.')[0]:obj for obj in bpy.data.objects if obj.name.startswith(('WCF_Zone09_Continuous','WCF_CliffToe'))}
        if context_source==MASTER_CONTEXT:
            for obj in list(bpy.data.objects):
                if obj.name.startswith('WCF_') or should_exclude_legacy_pond_detail(obj):obj.hide_render=True;obj.hide_set(True)
        result=build_world_water_cliffs(collection)
        if context_source==MASTER_CONTEXT:
            for obj in result['cliff_objects']:
                old=context_cliffs.get(obj.name.split('.')[0])
                if old and len(old.data.loops)==len(obj.data.loops):
                    obj.data.materials.clear()
                    for mat in old.data.materials:obj.data.materials.append(mat)
                    for old_uv in old.data.uv_layers:
                        new_uv=obj.data.uv_layers.get(old_uv.name) or obj.data.uv_layers.new(name=old_uv.name)
                        values=np.empty(len(old_uv.data)*2,dtype=np.float32);old_uv.data.foreach_get('uv',values);new_uv.data.foreach_set('uv',values)
        if WATER_ZONE in bpy.data.objects:
            bpy.data.objects[WATER_ZONE].hide_render=True;bpy.data.objects[WATER_ZONE].hide_set(True)
        record={'mode':'root master context copied read-only; preserve detailed root castle and all other root upgrades'}
        if context_source==SOURCE:
            castle=bpy.data.objects[CASTLE_ZONE]
            preserved,record=_preserve_baseline_castle_without_old_rock(castle,collection)
            castle.hide_render=True;castle.hide_set(True)
        metadata=result['metadata'];metadata['preserved_castle_extraction']=record;metadata['context_source']=str(context_source)
        metadata['source']=str(SOURCE);metadata['source_sha256']=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
        metadata['objects']=[{'name':obj.name,'vertices':len(obj.data.vertices),'polygons':len(obj.data.polygons),'bounds':[[min(v.co[i] for v in obj.data.vertices) for i in range(3)],[max(v.co[i] for v in obj.data.vertices) for i in range(3)]]} for obj in result['all_objects']]
        (OUT/'build-manifest.json').write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf-8')
        bpy.context.scene.render.image_settings.file_format='PNG'
        bpy.context.preferences.filepaths.save_version=0
        bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'world-water-cliffs-candidate.blend'))
    else:bpy.ops.wm.open_mainfile(filepath=str(OUT/'world-water-cliffs-candidate.blend'),load_ui=False,use_scripts=False)
    if args.render:
        scene=bpy.context.scene
        scene.render.engine='CYCLES';scene.cycles.samples=args.samples;scene.cycles.use_denoising=True
        scene.cycles.transparent_max_bounces=64
        if args.cuda:
            prefs=bpy.context.preferences.addons['cycles'].preferences
            prefs.compute_device_type='CUDA';prefs.get_devices()
            cuda_devices=[]
            for device in prefs.devices:
                device.use=device.type=='CUDA'
                if device.use:cuda_devices.append(device.name)
            if not cuda_devices:raise RuntimeError('Explicit CUDA render requested but no CUDA device is available')
            scene.cycles.device='GPU'
            (OUT/'cuda-render-device.json').write_text(json.dumps({'backend':'CUDA','devices':cuda_devices,'fresh_saved_scene':str(OUT/'world-water-cliffs-candidate.blend'),'view':args.view},indent=2)+'\n',encoding='utf-8')
            print('EXPLICIT_CUDA_DEVICE',cuda_devices,flush=True)
        scene.render.threads_mode='FIXED';scene.render.threads=args.threads
        scene.render.resolution_x=args.resolution;scene.render.resolution_y=int(args.resolution*.75);scene.render.resolution_percentage=100
        scene.camera=bpy.data.objects.get('Master_Isometric_Camera') or scene.camera
        if args.view in ('all','full'):
            scene.render.filepath=str(OUT/'01-whole-original-world.png');bpy.ops.render.render(write_still=True)
        for view,filename,location,target,lens in [('pond','02-native-pond-close.png',(-8,-14,11),(-3.4,-5.2,2.34),51),('cliff','03-cascade-natural-cliff-close.png',(10,-5,12),(14,12,5.8),47),('side','04-side-cascade-geology.png',(30,1,11),(16,12,5),49)]:
            if args.view!='all' and args.view!=view and not(args.view=='quick-cliff' and view=='cliff'):continue
            if args.view=='quick-cliff':filename='quick-cliff-proof.png'
            scene.camera=_camera(filename,location,target,lens);scene.render.filepath=str(OUT/filename);bpy.ops.render.render(write_still=True)
    print('WORLD_WATER_CLIFFS_READY',str(OUT),flush=True)


if __name__=='__main__':main()
