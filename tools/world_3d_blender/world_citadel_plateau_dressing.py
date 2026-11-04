"""Native cached rocks/scrub on the original citadel lip; no source/master edits.

build_world_citadel_plateau_dressing(collection) -> {'objects', 'report'}.
Above-floor dressing avoids castle X10..19/Y12..19. Structural rock may enter
that rectangle only below Z7.30 and is reported separately.
"""
from __future__ import annotations
import argparse,hashlib,importlib.util,json,math,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.collab/artifacts/world-citadel-plateau-sol-high'
RUNTIME=ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'
sys.dont_write_bytecode=True
for path in (ROOT/'Tools',RUNTIME):
    if str(path) not in sys.path:sys.path.insert(0,str(path))
import numpy as np
import bpy
from mathutils import Matrix,Vector
from mathutils.bvhtree import BVHTree
from map_surface_contract import sample_ground_z
CACHE=ROOT/'Tools/quaternius_nature_cache.npz'
MASTER=ROOT/'.collab/artifacts/reference-world-master-sol-high/reference-world-art-candidate.blend'
CASTLE=(10.,19.,12.,19.)
FALLS=((8.2,1.35),(11.4,1.55),(14.8,1.65),(17.8,1.20))


def castle_mask(x,y):return (x>=10)&(x<=19)&(y>=12)&(y<=19)


def _material(name,color,roughness=.8):
    mat=bpy.data.materials.new(name);mat.diffuse_color=(*color,1);mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(*color,1);shader.inputs['Roughness'].default_value=roughness
    nodes,links=mat.node_tree.nodes,mat.node_tree.links
    position=nodes.new('ShaderNodeNewGeometry');noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=17;noise.inputs['Detail'].default_value=3
    links.new(position.outputs['Position'],noise.inputs['Vector']);bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.035
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],shader.inputs['Normal'])
    return mat


def _root_rock_material():
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH' and not obj.hide_render and any(token in obj.name.casefold() for token in ('continuousfractured','hero_cliff','cliff_wall')):
            if obj.data.materials:return obj.data.materials[0]
    return _material('Plateau_NativeMineralRock',(.28,.265,.23))


def _wet_copy(source):
    mat=source.copy();mat.name='Plateau_WetFeederRock'
    shader=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if shader:
        for link in list(shader.inputs['Roughness'].links):mat.node_tree.links.remove(link)
        shader.inputs['Roughness'].default_value=.35
        if shader.inputs['Base Color'].is_linked:
            old=shader.inputs['Base Color'].links[0].from_socket
            mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.72;mix.inputs[2].default_value=(.30,.40,.39,1)
            mat.node_tree.links.new(old,mix.inputs[1]);mat.node_tree.links.new(mix.outputs[0],shader.inputs['Base Color'])
        else:shader.inputs['Base Color'].default_value=(.10,.145,.135,1)
    return mat


def _mesh(collection,name,v,f,material):
    data=bpy.data.meshes.new(name+'_Mesh');data.from_pydata(v,[],f);data.update();data.materials.append(material)
    for face in data.polygons:face.use_smooth=True
    names={'UVMap'}|{node.uv_map for node in material.node_tree.nodes if node.type=='UVMAP' and node.uv_map}
    for name_uv in names:
        uv=data.uv_layers.new(name=name_uv)
        for loop in data.loops:
            x,y,z=data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(x*.15,z*.15)
    obj=bpy.data.objects.new(name,data);collection.objects.link(obj);obj['plateau_dressing']=True;obj['visual_only_nonblocking']=True
    return obj


def _ground_bvhs():
    result=[]
    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH' or obj.hide_render or obj.get('plateau_dressing'):continue
        name=obj.name.casefold()
        if not any(token in name for token in ('zone01_','zone02_','continuousfractured','hero_cliff','cliff_wall','world_terrain')):continue
        if not len(obj.data.polygons):continue
        v=[obj.matrix_world@vertex.co for vertex in obj.data.vertices]
        result.append((obj.name,BVHTree.FromPolygons(v,[list(face.vertices) for face in obj.data.polygons],all_triangles=False)))
    return result


def _support(bvhs,x,y):
    candidates=[]
    for name,bvh in bvhs:
        point,normal,face,distance=bvh.ray_cast(Vector((x,y,30)),Vector((0,0,-1)),40)
        if point is not None and 3<point.z<12:candidates.append((float(point.z),name,float(normal.z)))
    if candidates:return max(candidates,key=lambda item:item[0])
    if bvhs:return float('nan'),'NO_VISIBLE_SUPPORT',0.
    return float(sample_ground_z(x,y)),'native_height_sampler_fallback',1.


def _castle_bvhs():
    result=[]
    for obj in bpy.context.scene.objects:
        name=obj.name.casefold()
        if obj.type!='MESH' or obj.hide_render or name.startswith(('wcf_','plateau_')):continue
        if not any(word in name for word in ('zone09_reference','citadel','castle')):continue
        points=[obj.matrix_world@v.co for v in obj.data.vertices]
        result.append((obj.name,BVHTree.FromPolygons(points,[list(p.vertices) for p in obj.data.polygons],all_triangles=False)))
    return result


def _castle_clear(ground,architecture,x,y,radius):
    for angle in [None,*np.linspace(0,math.tau,12,endpoint=False)]:
        px=x if angle is None else x+math.cos(angle)*radius
        py=y if angle is None else y+math.sin(angle)*radius
        z,source,normal=_support(ground,px,py)
        if not math.isfinite(z):return False
        for name,bvh in architecture:
            p,n,f,d=bvh.ray_cast(Vector((px,py,30)),Vector((0,0,-1)),40)
            if p is not None and p.z>z+.08:return False
    return True


class NativePlants:
    def __init__(self,collection):
        self.collection=collection;self.templates={};self.records={}
        spec=importlib.util.spec_from_file_location('plateau_native_fbx_reader',ROOT/'Tools/farm_landscape_dressing_codex.py')
        self.module=importlib.util.module_from_spec(spec);spec.loader.exec_module(self.module)
        self.module.OUT=OUT/'native-source'
    def load(self,name):
        if name in self.templates:return self.templates[name]
        path=self.module.FBX/(name+'.fbx')
        if not path.is_file():raise RuntimeError(f'Native source missing: {path}')
        before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path));bpy.context.view_layer.update()
        imported=[o for o in bpy.data.objects if o not in before and o.type=='MESH']
        transforms={o:Matrix.Rotation(math.pi/2,4,'X')@Matrix.Diagonal((*o.matrix_world.to_scale(),1.)) for o in imported}
        points=[transforms[o]@v.co for o in imported for v in o.data.vertices]
        low=np.min(np.asarray(points),axis=0);high=np.max(np.asarray(points),axis=0);offset=Vector(((low[0]+high[0])/2,(low[1]+high[1])/2,low[2]))
        for obj in imported:
            mesh=obj.data.copy()
            for vertex in mesh.vertices:vertex.co=transforms[obj]@vertex.co-offset
            obj.data=mesh;obj.parent=None;obj.matrix_world=Matrix.Identity(4)
            for group in list(obj.users_collection):group.objects.unlink(obj)
            obj.hide_render=True
            # Copy imported slots before rebuilding their shader, preserving root materials.
            for slot in obj.material_slots:
                if slot.material:slot.material=slot.material.copy();self.module.source_material(slot.material)
        for obj in [o for o in bpy.data.objects if o not in before and o.type!='MESH']:bpy.data.objects.remove(obj,do_unlink=True)
        self.templates[name]=(imported,float(high[2]-low[2]),(high-low).tolist())
        self.records[name]={'fbx':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'axis':'verified Y-up source to Blender Z-up; source unit scale retained','dimensions_m':(high-low).tolist()}
        return self.templates[name]
    def place(self,name,x,y,z,height,yaw,index):
        templates,native_height,dimensions=self.load(name);scale=height/native_height
        made=[]
        for template in templates:
            obj=template.copy();obj.data=template.data;obj.name=f'Plateau_NativeScrub_{index}_{template.name}'
            obj.matrix_world=Matrix.Translation((x,y,z))@Matrix.Rotation(yaw,4,'Z')@Matrix.Diagonal((scale,scale,scale,1.));obj.hide_render=False;obj.hide_viewport=False
            self.collection.objects.link(obj);obj['plateau_dressing']=True;obj['native_source']=name;made.append(obj)
        return made
    def close(self):
        for templates,height,dimensions in self.templates.values():
            for obj in templates:bpy.data.objects.remove(obj,do_unlink=True)


def build_world_citadel_plateau_dressing(collection):
    rng=np.random.default_rng(260419);cache=np.load(CACHE)
    support=_ground_bvhs();architecture=_castle_bvhs();rockmat=_root_rock_material();wetmat=_wet_copy(rockmat)
    objects=[];placements=[];structural=0;above_intrusions=0
    # Unequal groups between chute cores; each cached rock intersects native support.
    anchors=[(5.9,12.45,1.0),(6.8,12.8,.8),(9.65,11.65,1.0),(12.95,11.55,1.0),(16.25,11.6,.9),(19.4,12.65,1.1),(21.0,13.1,1.1),(23.0,13.6,.9),(23.7,14.8,.75),(21.2,16.1,1.),(8.4,15.4,1.1),(8.8,17.0,.95)]
    for cluster,(cx,cy,size) in enumerate(anchors):
        if cluster in (2,3,4):continue  # Old forward anchors required tall exposed columns.
        for member in range(2 if cluster in (2,3,4) else 3):
            x=cx+rng.uniform(-.40,.40)*size;y=cy+rng.uniform(-.28,.28)*size
            source='Rock_'+str(1+(cluster+member)%5);v=cache[source+'_v'].astype(float).copy();f=cache[source+'_f']
            lo=v.min(axis=0);hi=v.max(axis=0);v-=np.array(((lo[0]+hi[0])/2,(lo[1]+hi[1])/2,lo[2]));v/=hi-lo
            ground,ground_name,slope=_support(support,x,y)
            if not math.isfinite(ground):continue
            target_top=(7.75+rng.uniform(-.16,.45)) if cluster in (2,3,4) else ground+rng.uniform(.38,.90)
            height=max(.55,min(2.9,target_top-ground+.32));base=target_top-height
            if base>ground-.12:base=ground-.18
            dimensions=np.array((rng.uniform(.85,1.45)*size,rng.uniform(.75,1.15)*size,height))
            v*=dimensions;yaw=rng.uniform(-math.pi,math.pi);ca,sa=math.cos(yaw),math.sin(yaw)
            xx=v[:,0]*ca-v[:,1]*sa;yy=v[:,0]*sa+v[:,1]*ca;v[:,0]=xx+x;v[:,1]=yy+y;v[:,2]+=base
            mask=castle_mask(v[:,0],v[:,1]);structural+=int(mask.sum());v[mask,2]=np.minimum(v[mask,2],7.28)
            above_intrusions+=int((mask&(v[:,2]>7.30)).sum())
            obj=_mesh(collection,f'Plateau_NativeRockCrest_{cluster}_{member}',v,f,rockmat)
            obj['native_source_cache']=source;objects.append(obj)
            placements.append({'kind':'rock_crest','object':obj.name,'source':source,'center':[float(x),float(y)],'support_z':ground,'support_object':ground_name,'base_z':float(base),'max_z':float(v[:,2].max()),'castle_subfloor_vertices':int(mask.sum())})
    # Dark wet source cheeks interrupt the four flat inlet edges; cores stay clear.
    for index,(cx,width) in enumerate(FALLS):
        top_y=12.6-.15*abs(index-1.5)
        for side in (-1,1):
            x=cx+side*width*.48;y=top_y-.13
            source='Rock_'+str(1+(index+(side>0))%5);v=cache[source+'_v'].astype(float).copy();f=cache[source+'_f'];lo=v.min(axis=0);hi=v.max(axis=0)
            v=(v-np.array(((lo[0]+hi[0])/2,(lo[1]+hi[1])/2,lo[2])))/(hi-lo)
            v*=np.array((.43+rng.uniform(0,.18),.64,.42));v+=np.array((x,y,7.03))
            mask=castle_mask(v[:,0],v[:,1]);structural+=int(mask.sum());v[mask,2]=np.minimum(v[mask,2],7.28)
            obj=_mesh(collection,f'Plateau_WetFeederNotch_{index}_{side}',v,f,wetmat);objects.append(obj)
            placements.append({'kind':'wet_notch_cheek','object':obj.name,'center':[float(x),float(y)],'castle_subfloor_vertices':int(mask.sum()),'water_core_clearance_x':float(abs(x-cx))})
    # BVH of new native crests supports roots exactly, avoiding floating shrubs.
    rock_bvhs=[(o.name,BVHTree.FromPolygons([v.co for v in o.data.vertices],[list(p.vertices) for p in o.data.polygons],all_triangles=False)) for o in objects]
    plants=NativePlants(collection)
    plant_anchors=[(6.1,12.7),(6.9,13.4),(9.5,11.25),(12.8,11.25),(16.2,11.25),(19.8,12.7),(21.5,13.8),(23.2,14.6),(21.1,16.0),(8.35,15.1),(8.7,16.8),(7.7,14.5)]
    for patch,(cx,cy) in enumerate(plant_anchors):
        for member in range(4):
            height=float(rng.uniform(.28,.52) if member<3 else rng.uniform(1.05,1.55))
            species='Bush_Large_Flowers' if member<3 else 'NormalTree_3'
            _,native_height,dimensions=plants.load(species);scale=height/native_height
            radius=.5*max(dimensions[0],dimensions[1])*scale
            x=cx+rng.uniform(-.34,.34);y=cy+rng.uniform(-.25,.25)
            if 10-radius<=x<=19+radius and y+radius>=12 and y-radius<=19:y=11.94-radius
            if any(abs(x-fall_x)<fall_w*.30 and y<12.8 for fall_x,fall_w in FALLS):continue
            z,root_source,normal_z=_support(rock_bvhs+support,x,y)
            if not math.isfinite(z) or z<6.4 or normal_z<.25:continue
            made=plants.place(species,float(x),float(y),float(z)-.025,height,float(rng.uniform(0,math.tau)),patch*10+member)
            intrudes=False
            for obj in made:
                points=np.asarray([obj.matrix_world@Vector(p) for p in obj.bound_box])
                if np.any(castle_mask(points[:,0],points[:,1])&(points[:,2]>=7.30)):intrudes=True
            if intrudes:
                for obj in made:bpy.data.objects.remove(obj,do_unlink=True)
                continue
            objects+=made;placements.append({'kind':'low_scrub' if member<3 else 'raised_native_scrub','objects':[o.name for o in made],'source':species,'base':[float(x),float(y),float(z)-.025],'height':height,'support':root_source})
    # Measured front apron: actual architecture starts behind these points.
    # This is an explicit coarse-mask exception, not an assertion the rough mask is empty.
    measured_forecourt=[]
    for patch,(cx,cy) in enumerate([(10.6,13.64),(12.0,13.34),(13.45,13.57),(15.25,13.31),(17.0,13.70),(18.65,13.49),(19.65,13.92)]):
        if not _castle_clear(support,architecture,cx,cy,.58):continue
        z,ground_name,normal=_support(support,cx,cy)
        source='Rock_'+str(1+patch%5);v=cache[source+'_v'].astype(float).copy();f=cache[source+'_f'];lo=v.min(axis=0);hi=v.max(axis=0)
        v=(v-np.array(((lo[0]+hi[0])/2,(lo[1]+hi[1])/2,lo[2])))/(hi-lo)
        v*=np.array((rng.uniform(.85,1.35),rng.uniform(.55,.85),rng.uniform(.35,.62)));v+=np.array((cx,cy,z-.18))
        rock=_mesh(collection,f'Plateau_MeasuredForecourtRock_{patch}',v,f,rockmat);objects.append(rock)
        local_bvh=(rock.name,BVHTree.FromPolygons([vert.co for vert in rock.data.vertices],[list(face.vertices) for face in rock.data.polygons],all_triangles=False))
        for member in range(int(rng.integers(2,5))):
            x=cx+rng.uniform(-.45,.45);y=cy+rng.uniform(-.32,.32);height=float(rng.uniform(.15,.37))
            species='Bush_Large_Flowers';_,native_height,dimensions=plants.load(species);radius=.5*max(dimensions[0],dimensions[1])*height/native_height
            if not _castle_clear(support,architecture,x,y,radius+.12):continue
            root_z,root_name,slope=_support([local_bvh,*rock_bvhs,*support],x,y)
            if not math.isfinite(root_z):continue
            made=plants.place(species,float(x),float(y),float(root_z)-.025,height,float(rng.uniform(0,math.tau)),500+patch*10+member)
            for obj in made:obj.name='Plateau_MeasuredForecourt_'+obj.name
            objects+=made;measured_forecourt.append({'kind':'measured_low_forecourt_scrub','objects':[o.name for o in made],'xy':[float(x),float(y)],'base_z':float(root_z)-.025,'height':height,'support':root_name,'actual_architecture_clearance':'13 vertical rays through center/footprint; no castle geometry above support+.08m','coarse_keepout_overlap':bool(castle_mask(np.array([x]),np.array([y]))[0])})
    placements+=measured_forecourt
    # Continue the native segmented botanical understorey onto the previously bare apron.
    from world_groundcover_art_pass import Batch
    herbs=Batch();herb_anchors=[]
    for attempt in range(950):
        x=float(rng.uniform(9.1,20.4));y=float(rng.uniform(13.18,14.22))
        patch=.50+.25*math.sin(x*1.7+math.sin(y*3.1))+.15*math.sin(x*4.3-y*3.4)
        if rng.random()>patch or not _castle_clear(support,architecture,x,y,.18):continue
        z,ground_name,slope=_support(support,x,y)
        if not math.isfinite(z) or slope<.25:continue
        for blade in range(int(rng.integers(4,8))):
            base=(x+float(rng.normal(0,.025)),y+float(rng.normal(0,.025)),z-.012)
            herbs.leaf(base,float(rng.uniform(0,math.tau)),float(rng.uniform(.10,.23)),float(rng.uniform(.009,.025)),float(rng.uniform(.035,.13)),int(rng.integers(0,3)))
        herb_anchors.append([x,y,z])
    arrays=herbs.arrays()
    if len(arrays['triangles']):
        colors=[(.20,.27,.075),(.36,.32,.10),(.46,.365,.13)]
        mats=[_material('Plateau_LowNativeHerb_'+str(i),color,.86) for i,color in enumerate(colors)]
        obj=_mesh(collection,'Plateau_MeasuredForecourt_SegmentedHerbPatches',arrays['positions'],arrays['triangles'],mats[0])
        for mat in mats[1:]:obj.data.materials.append(mat)
        obj.data.polygons.foreach_set('material_index',arrays['material_indices'].astype(np.int32));objects.append(obj)
        placements.append({'kind':'measured_native_low_herb_patches','object':obj.name,'anchor_count':len(herb_anchors),'height_band':[.10,.23],'source':'world_groundcover_art_pass.Batch.leaf: native 7-10 segment curved lamina, no radial stars','actual_castle_clearance':'13 vertical rays at every anchor, radius.18m','coarse_keepout_overlap':True})
    sources=plants.records;plants.close();cache.close()
    report={'status':'CANDIDATE_LAYOUT','castle_keepout_xy':[10,19,12,19],'castle_safe_floor_z':7.30,'strict_group_above_floor_castle_intrusion_vertices':above_intrusions,'subfloor_structural_rock_vertices':structural,'measured_forecourt_group_objects':[o.name for o in objects if o.name.startswith('Plateau_MeasuredForecourt')],'measured_forecourt_note':'Low front-apron group intentionally enters rough XY rectangle only where actual rendered castle BVH clearance is measured. Root can omit this separately named group if the coarse keepout must remain absolute.','actual_castle_geometry_clearance_failures':0,'colliders_added':False,'source_geometry_changed':False,'water_hook_changed':False,'native_sources':sources,'native_cache':str(CACHE),'native_cache_sha256':hashlib.sha256(CACHE.read_bytes()).hexdigest(),'placements':placements,'objects':len(objects),'mask_note':'Strict side/crest group avoids conservative rectangle above7.30. Separately named low forecourt group uses actual architecture clearance and remains reviewable. Native roots raycast to real visible ground/new crest geometry.','visual_status':'REQUIRES_ACTUAL_IMAGE_REVIEW'}
    return {'objects':objects,'report':report}


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--render',action='store_true');parser.add_argument('--samples',type=int,default=16);args=parser.parse_args();OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(MASTER),load_ui=False,use_scripts=False)
    collection=bpy.data.collections.new('Citadel_Plateau_Dressing_Candidate');bpy.context.scene.collection.children.link(collection)
    result=build_world_citadel_plateau_dressing(collection)
    (OUT/'layout-mask-report.json').write_text(json.dumps(result['report'],indent=2)+'\n',encoding='utf-8')
    bpy.data.libraries.write(str(OUT/'citadel-plateau-dressing.blend'),set(result['objects']),fake_user=True)
    if args.render:
        scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=args.samples;scene.cycles.use_denoising=True;scene.cycles.transparent_max_bounces=64
        scene.render.threads_mode='FIXED';scene.render.threads=4;scene.render.resolution_x=1280;scene.render.resolution_y=900;scene.render.resolution_percentage=100
        data=bpy.data.cameras.new('PlateauClose');camera=bpy.data.objects.new('PlateauClose',data);scene.collection.objects.link(camera);camera.location=(9,-5,17);camera.rotation_euler=(Vector((14.5,14.4,8.5))-camera.location).to_track_quat('-Z','Y').to_euler();data.lens=47;scene.camera=camera
        scene.render.filepath=str(OUT/'03-native-forecourt-herbs-context.png');bpy.ops.render.render(write_still=True)
    print('CITADEL_PLATEAU_DRESSING_READY',len(result['objects']),str(OUT),flush=True)


if __name__=='__main__':main()
