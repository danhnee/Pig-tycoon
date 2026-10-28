"""Varied ancient architectural fragments on verified original-world safe sites.

Hook: build_world_ancient_ruins(collection) -> objects/report/stone_objects.
No lore seal counts, gameplay, source writes, building moves or ground shader edits.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import sys
import numpy as np
from map_surface_contract import sample_ground_z, eval_road_mask, eval_pond_distances, eval_river_center_and_width

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.collab/artifacts/world-ancient-ruins-sol-high'
SITES=[(-16.2,-3.2),(-11.8,-8.4),(-15.5,-9.8),(-19.5,-6.2),(-18.8,.4),(-13.4,1.2),
       (-4.2,3.2),(1.8,3.8),(4.8,1.6),(6.8,-2.4),(-6.4,-11.2),(2.8,-13.4),
       (10.6,-12.6),(16.8,-10.2)]


def _safe(x,y):
    if float(eval_road_mask(x,y))>.30 or min(float(d) for d in eval_pond_distances(x,y))<1.23:return False
    if -8.8<y<12.9:
        cx,w=eval_river_center_and_width(y)
        if abs(x-float(cx))<float(w)*1.16:return False
    for cx,cy,r in [(-17.8,5.4,3),(-17,9.2,4),(-13.4,9.4,3.3),(-7.8,6.4,4),(-1.8,7.6,3),(8.6,4.6,3)]:
        if (x-cx)**2+(y-cy)**2<r*r:return False
    return True


def build_world_ancient_ruins(collection,seed=260419):
    import bpy
    rng=np.random.default_rng(seed);objects=[];stone_objects=[];records=[]
    def material(name,colour):
        m=bpy.data.materials.new(name);m.diffuse_color=(*colour,1);m.use_nodes=True
        p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*colour,1);p.inputs['Roughness'].default_value=.88
        return m
    stone=material('AncientRuins_Stone_Placeholder_For_Root_Albedo',(.24,.22,.18))
    moss=material('AncientRuins_Moss_Seams',(.055,.115,.025));dark=material('AncientRuins_Deep_Carved_Stone',(.13,.125,.105))
    native=np.load(ROOT/'Tools/quaternius_nature_cache.npz',allow_pickle=False)

    def mesh_object(name,v,f,origin,yaw=0,mat=stone,bevel=.012,smooth=False):
        v=np.asarray(v,dtype=np.float32);f=[list(face) for face in f]
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(v.tolist(),[],f);mesh.update()
        assert not mesh.validate(verbose=True),name
        mesh.materials.append(mat)
        if smooth:mesh.polygons.foreach_set('use_smooth',np.ones(len(mesh.polygons),dtype=np.bool_))
        obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj);obj.location=origin;obj.rotation_euler.z=yaw
        obj['surface_role']='moss' if mat==moss else 'stone';obj['visual_art_only']=True
        if bevel:
            mod=obj.modifiers.new('Worn_Stone_Edge_Rounding','BEVEL');mod.width=bevel;mod.segments=3;mod.affect='EDGES'
            mod=obj.modifiers.new('Weighted_Weathered_Normals','WEIGHTED_NORMAL');mod.keep_sharp=True
        objects.append(obj)
        if mat!=moss:stone_objects.append(obj)
        return obj

    def tube(name,points,radius,origin,yaw,mat=moss):
        points=np.asarray(points);v=[];f=[];sides=6
        for i,p in enumerate(points):
            tangent=points[min(i+1,len(points)-1)]-points[max(i-1,0)];tangent=tangent/max(np.linalg.norm(tangent),1e-9)
            side=np.cross(tangent,[0,0,1])
            if np.linalg.norm(side)<.1:side=np.cross(tangent,[0,1,0])
            side/=np.linalg.norm(side);up=np.cross(tangent,side)
            for j in range(sides):v.append(p+radius*(side*math.cos(j*math.tau/sides)+up*math.sin(j*math.tau/sides)))
        for i in range(len(points)-1):
            for j in range(sides):f.append([i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j])
        return mesh_object(name,v,f,origin,yaw,mat,0,True)

    def shaft(name,origin,yaw,height,radius,lean=.0):
        sides=48;rings=8;v=[];f=[]
        broken=rng.uniform(-.075,.045,sides)
        for row in range(rings):
            t=row/(rings-1)
            for j in range(sides):
                a=j*math.tau/sides;r=radius*(1-.10*math.cos(a*12))*(1-.09*t)
                v.append((r*math.cos(a)+lean*t,r*math.sin(a),height*t+(broken[j] if row==rings-1 else 0)))
        for row in range(rings-1):
            for j in range(sides):f.append([row*sides+j,row*sides+(j+1)%sides,(row+1)*sides+(j+1)%sides,(row+1)*sides+j])
        f.extend([list(range(sides-1,-1,-1)),list(range((rings-1)*sides,rings*sides))])
        return mesh_object(name,v,f,origin,yaw,bevel=.006,smooth=True)

    def arch_stone(name,origin,yaw,radius,theta0,theta1,depth=.22):
        # True curved voussoir wedge: empty centre is never filled by a wall/core.
        angles=np.linspace(theta0,theta1,9);v=[]
        for y in [-depth/2,depth/2]:
            for r in [radius,radius+.16]:
                v.extend([((r+float(rng.uniform(-.004,.004)))*math.cos(a),y+float(rng.uniform(-.003,.003)),(r+float(rng.uniform(-.004,.004)))*math.sin(a)) for a in angles])
        n=len(angles);f=[]
        for i in range(n-1):
            f.extend([[i,i+1,n+i+1,n+i],[2*n+i,3*n+i,3*n+i+1,2*n+i+1],
                      [i,2*n+i,2*n+i+1,i+1],[n+i,n+i+1,3*n+i+1,3*n+i]])
        f.extend([[0,n,3*n,2*n],[n-1,3*n-1,4*n-1,2*n-1]])
        return mesh_object(name,v,f,origin,yaw,bevel=.009)

    def carved_panel(name,origin,yaw,width,height,depth=.19,fallen=False):
        nx,nz=34,48;v=[];f=[]
        for j,z in enumerate(np.linspace(0,height,nz)):
            for i,x in enumerate(np.linspace(-width/2,width/2,nx)):
                # Abstract flowing/meander incisions only, with actual recessed surface geometry.
                lines=[height*.22+.035*math.sin(x*14),height*.58+.045*math.cos(x*11),height*.84+.025*math.sin(x*19)]
                distance=min(abs(z-a) for a in lines)
                edge=min(abs(x-width*.36),abs(x+width*.36))
                cut=.014*math.exp(-(min(distance,edge)/.009)**2)
                zz=z+(rng.uniform(-.035,.025) if j==nz-1 else 0)
                v.append((x,-depth/2+cut,zz))
        for j in range(nz-1):
            for i in range(nx-1):
                a=j*nx+i;f.append([a,a+nx,a+nx+1,a+1])
        back=len(v);v.extend([(-width/2,depth/2,0),(width/2,depth/2,0),(width/2,depth/2,height),(-width/2,depth/2,height)])
        f.extend([[back,back+1,back+2,back+3],list(range(nx-1,-1,-1))+[back,back+1],
                  [j*nx for j in range(nz)]+[back+3,back],[(j+1)*nx-1 for j in range(nz-1,-1,-1)]+[back+1,back+2],
                  list(range((nz-1)*nx,nz*nx))+[back+2,back+3]])
        obj=mesh_object(name,v,f,origin,yaw,bevel=.007,smooth=True)
        if fallen:obj.rotation_euler.x=math.radians(75+rng.uniform(-8,8))
        return obj

    sites=[]
    for x,y in SITES:
        if _safe(x,y) and all(_safe(x+dx,y+dy) for dx,dy in [(-.5,0),(.5,0),(0,-.5),(0,.5)]):sites.append((x,y))
        if len(sites)==12:break
    for index,(x,y) in enumerate(sites):
        z=float(sample_ground_z(x,y))-.10;yaw=float(rng.uniform(-.8,.8));origin=(x,y,z);kind=index%4
        if kind==0:
            height=float(rng.uniform(.62,.92));radius=float(rng.uniform(.42,.56))
            # Rotate both piers with the arch, retaining only its supported half.
            offset=radius+.065
            shaft(f'WorldAncientRuins_Arch_{index}_LeftFlutedPier',(x-offset*math.cos(yaw),y-offset*math.sin(yaw),z),yaw,height,.12)
            shaft(f'WorldAncientRuins_Arch_{index}_BrokenRightPier',(x+offset*math.cos(yaw),y+offset*math.sin(yaw),z),yaw,height*.65,.13,.06)
            for segment in range(9):
                if segment < (4 if index%8==0 else 5):continue
                lo=segment*math.pi/9+.025;hi=(segment+1)*math.pi/9-.025
                arch_stone(f'WorldAncientRuins_Arch_{index}_Voussoir_{segment}',(x,y,z+height),yaw,radius,lo,hi)
        elif kind==1:
            shaft(f'WorldAncientRuins_FracturedFlutedPost_{index}',origin,yaw,float(rng.uniform(.85,1.35)),float(rng.uniform(.17,.24)),.075)
            drum=shaft(f'WorldAncientRuins_FallenColumnDrum_{index}',(x+.40,y-.24,z+.13),yaw,.48,.16)
            drum.rotation_euler.x=1.40
        elif kind==2:
            panel=carved_panel(f'WorldAncientRuins_HalfBuriedFallenLintel_{index}',(x,y,z+.09),yaw,1.12,.48,.23,True)
            shaft(f'WorldAncientRuins_BrokenSocket_{index}',(x-.42,y+.22,z),yaw,.28,.19)
        else:
            carved_panel(f'WorldAncientRuins_WornCarvedStele_{index}',origin,yaw,.48+rng.uniform(0,.17),.75+rng.uniform(0,.30))
            carved_panel(f'WorldAncientRuins_FallenReliefChip_{index}',(x+.34,y-.18,z+.04),yaw+.7,.48,.30,.15,True)
        # Ground-hugging native stone companions; unequal forms/scales, no stacked cube piles.
        for k in range(int(rng.integers(2,5))):
            name='Rock_'+str(int(rng.integers(1,6)));v=native[name+'_v'].copy();f=native[name+'_f']
            scale=np.array([rng.uniform(.26,.52),rng.uniform(.21,.44),rng.uniform(.15,.31)])
            v*=scale;angle=float(rng.uniform(0,math.tau));distance=float(rng.uniform(.40,.80));rx=x+distance*math.cos(angle);ry=y+distance*math.sin(angle)
            if not _safe(rx,ry):continue
            mesh_object(f'WorldAncientRuins_NativeWeatheredBoulder_{index}_{k}',v,f,(rx,ry,float(sample_ground_z(rx,ry))-.055),angle,bevel=0,smooth=True)
        for seam in range(2):
            points=[(-.30+t*.60,-.16+.018*math.sin(t*11+index),.06+.025*math.sin(t*7)) for t in np.linspace(0,1,11)]
            tube(f'WorldAncientRuins_MossSeam_{index}_{seam}',points,.008+seam*.002,(x,y+seam*.11,z+.09),yaw)
        records.append({'xy':[x,y],'kind':['broken_open_voussoir_arch','fractured_fluted_post','half_buried_fallen_lintel','incised_relief_stele'][kind],
                        'original_safe_site':True,'ground_z':z+.10,'road_mask':float(eval_road_mask(x,y))})
    native.close()
    report={'status':'native_context_art_candidate_requires_visual_review','fragment_sites':records,'site_count':len(records),
            'site_count_is_art_scatter_only_not_lore':True,'no_canonical_seals_or_sundials':True,'colliders_added':False,
            'actual_open_arches':True,'source_cache_sha256':hashlib.sha256((ROOT/'Tools/quaternius_nature_cache.npz').read_bytes()).hexdigest(),
            'stone_material_policy':'Root may replace stone material using surface_role; moss seams stay separate.',
            'approved_observatory_or_buildings_changed':False}
    return {'objects':objects,'stone_objects':stone_objects,'report':report}


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--proof',action='store_true');args=parser.parse_args()
    sys.path.insert(0,str(ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'))
    import bpy
    from mathutils import Vector
    OUT.mkdir(parents=True,exist_ok=True)
    source=ROOT/'.collab/artifacts/reference-world-master-sol-high/reference-world-art-candidate.blend'
    if args.proof:bpy.ops.wm.open_mainfile(filepath=str(source))
    else:bpy.ops.wm.read_factory_settings(use_empty=True)
    for obj in bpy.context.scene.objects:
        if 'Zone11' in obj.name:obj.hide_render=True
    collection=bpy.data.collections.new('World_Ancient_Ruins_Candidate');bpy.context.scene.collection.children.link(collection)
    result=build_world_ancient_ruins(collection);report=result['report']
    # Reuse root-painted rock shader in this read-only native-context proof when available.
    stone_material=next((m for m in bpy.data.materials if any(token in m.name.lower() for token in ['paintedrock','painted_rock','world_rock_surface'])),None)
    if stone_material:
        for obj in result['stone_objects']:obj.data.materials.clear();obj.data.materials.append(stone_material)
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='CUDA';prefs.get_devices()
    cuda=[d for d in prefs.devices if d.type=='CUDA'];assert cuda,'CUDA device is required for this proof'
    for device in prefs.devices:device.use=device.type=='CUDA'
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='GPU';scene.cycles.samples=24;scene.cycles.use_denoising=True
    report['explicit_compute_backend']='CUDA';report['cuda_devices']=[d.name for d in cuda]
    # Keep the editable deliverable confined to this collection, not a duplicate master.
    bpy.data.libraries.write(str(OUT/'ancient-ruins-editable.blend'),{collection},path_remap='RELATIVE_ALL',fake_user=True,compress=True)
    if args.proof:
        original_camera=scene.camera
        data=bpy.data.cameras.new('AncientRuins_Close_Camera');camera=bpy.data.objects.new(data.name,data);collection.objects.link(camera)
        x,y=report['fragment_sites'][0]['xy'];z=float(sample_ground_z(x,y));camera.location=(x+2.25,y-3.2,z+1.65)
        camera.rotation_euler=(Vector((x,y,z+.55))-camera.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=2.7
        scene.camera=camera;scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
        scene.render.filepath=str(OUT/'01-native-ruins-close.png');bpy.ops.render.render(write_still=True)
        scene.camera=original_camera;scene.render.resolution_x=1600;scene.render.resolution_y=1200
        scene.render.filepath=str(OUT/'02-native-whole-context.png');bpy.ops.render.render(write_still=True)
    (OUT/'ruins-evidence.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report,indent=2))


if __name__=='__main__':main()
