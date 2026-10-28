"""Irregular middle-scale botanical communities on the original terrain.

Integration: build_world_meadow_hero_flora(collection) -> objects/report.
No terrain/material/source changes; no colliders or changes to stable v4 grass.
"""
from pathlib import Path
import math,json,sys
import numpy as np
from world_groundcover_art_pass import Batch,PALETTE,valid_point,_material
from map_surface_contract import sample_ground_z,eval_road_mask,eval_pond_distances,island_signed_distance

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.collab/artifacts/world-meadow-hero-flora-sol-high'


def build_world_meadow_hero_flora(collection,seed=260519,count=60):
    import bpy
    rng=np.random.default_rng(seed)
    sedges,herbs,flowers,rocks=Batch(),Batch(),Batch(),Batch()
    palette=[*PALETTE,(.83,.79,.60,1),(.69,.59,.72,1),(.28,.38,.12,1),(.27,.25,.18,1)]
    anchors=[];boulder_anchors=[];rock_records=[]
    # Exact implicit island shape, including the diagonal front corners.
    for attempt in range(4000):
        if len(boulder_anchors)>=40:break
        if len(boulder_anchors)<27:
            theta=float(rng.uniform(math.pi*.90,math.pi*2.04))
            point=np.array([29.2*math.cos(theta),21.4*math.sin(theta)])
            point*=float(rng.uniform(.76,.90))/float(island_signed_distance(*point))
            x,y=point
        else:
            pond=int(rng.integers(3));theta=float(rng.uniform(0,math.tau));radius=float(rng.uniform(1.65,2.15))
            cx,cy,rx,ry,angle=[(-3.4,-5.2,5.65,1.78,-.541),(-.5,.6,3.55,1.40,.314),(9.6,-7.4,3.35,1.38,-.332)][pond]
            lx=rx*radius*math.cos(theta);ly=ry*radius*math.sin(theta)
            x=cx+lx*math.cos(angle)-ly*math.sin(angle);y=cy+lx*math.sin(angle)+ly*math.cos(angle)
        if not valid_point(x,y) or float(eval_road_mask(x,y))>.08:continue
        if any((x-p[0])**2+(y-p[1])**2<2.1**2 for p in boulder_anchors):continue
        if not all(valid_point(x+dx,y+dy) and float(eval_road_mask(x+dx,y+dy))<.13 for dx,dy in [(-.85,0),(.85,0),(0,-.85),(0,.85)]):continue
        boulder_anchors.append((float(x),float(y)))
    anchors.extend([(x,y,float(rng.uniform(1.0,1.5)),float(rng.uniform(.65,1.0))) for x,y in boulder_anchors])
    # Broad, irregular communities with gaps rather than an evenly spread carpet.
    centres=[(-20,-8),(-17,-14),(-12,-16),(-6,-16),(1,-17),(8,-15),(17,-12),
             (-17,-3),(-12,-6),(-5,-8),(3,-5),(10,-7),(15,-3),(-4,1),(2,1)]
    for attempt in range(count*80):
        if len(anchors)>=count:break
        cx,cy=centres[int(rng.integers(len(centres)))];x=cx+rng.normal(0,2.2);y=cy+rng.normal(0,1.6)
        if not valid_point(x,y) or float(eval_road_mask(x,y))>.14:continue
        if any((x-p[0])**2+(y-p[1])**2<1.0 for p in anchors):continue
        anchors.append((float(x),float(y),float(rng.uniform(.48,1.05)),float(rng.uniform(.33,.76))))

    def stem(base,end,material=17,radius=.003):
        v=[];f=[];n=7;sides=5
        base=np.asarray(base);end=np.asarray(end)
        for k in range(n):
            t=k/(n-1);p=base*(1-t)+end*t+[.025*math.sin(t*math.pi),0,0]
            for j in range(sides):v.append(p+[radius*math.cos(j*math.tau/sides),radius*math.sin(j*math.tau/sides),0])
        for k in range(n-1):
            for j in range(sides):
                a=k*sides+j;b=k*sides+(j+1)%sides;c=a+sides;d=b+sides
                f.extend([[a,b,c],[b,d,c]])
        flowers.add(v,f,material)

    def flower_head(centre,scale,colour,petals=9):
        # Segmented cupped petals and a shallow raised disc, never a cube/star crystal.
        centre=np.asarray(centre)
        for j in range(petals):
            angle=j*math.tau/petals+rng.uniform(-.08,.08)
            flowers.leaf(centre,angle,scale*.22,scale*.42,scale,colour)
        v=[centre+[0,0,scale*.13]]
        v.extend([centre+[scale*.20*math.cos(t),scale*.20*math.sin(t),scale*.10] for t in np.linspace(0,math.tau,13)[:-1]])
        flowers.add(v,[[0,j+1,(j+1)%12+1] for j in range(12)],9)

    native=np.load(ROOT/'Tools/quaternius_nature_cache.npz',allow_pickle=False)
    counts={'sedge_tussocks':0,'flower_stems':0,'fern_fronds':0,'mossy_herbs':0,'native_boulders':0}
    for index,(x,y,sx,sy) in enumerate(anchors):
        yaw=float(rng.uniform(0,math.tau));c,s=math.cos(yaw),math.sin(yaw)
        mood=index%5
        for tuft in range(int(rng.integers(21,38))):
            a,b=np.clip(rng.normal(0,.45,2),-1,1)*[sx,sy]
            px=x+a*c-b*s;py=y+a*s+b*c
            if not valid_point(px,py) or float(eval_road_mask(px,py))>.23:continue
            z=float(sample_ground_z(px,py))-.012
            height=float(rng.uniform(.50,.90));colour=int(rng.choice([1,2,3,4,5,6,7],p=[.14,.10,.14,.19,.20,.14,.09]))
            for blade in range(int(rng.integers(7,12))):
                sedges.leaf((px+rng.normal(0,.04),py+rng.normal(0,.04),z),rng.uniform(0,math.tau),height*rng.uniform(.75,1),rng.uniform(.010,.020),height*rng.uniform(.48,1.03),colour)
            counts['sedge_tussocks']+=1
            if tuft%7==0:
                # Broad, low, overlapping curved leaves form a moss/herb mound.
                for leaf in range(15):
                    herbs.leaf((px,py,z),rng.uniform(0,math.tau),rng.uniform(.12,.25),rng.uniform(.035,.075),rng.uniform(.10,.26),int(rng.choice([0,1,6,7,17])))
                counts['mossy_herbs']+=1
            if tuft%9==0 and mood in [1,3,4]:
                for frond in range(5):
                    angle=float(rng.uniform(0,math.tau));reach=float(rng.uniform(.48,.85));frond_height=float(rng.uniform(.45,.85));direction=np.array([math.cos(angle),math.sin(angle),0])
                    for step in range(1,11):
                        t=step/11;centre=np.array([px,py,z])+direction*reach*t+[0,0,frond_height*math.sin(t*math.pi*.78)]
                        for sign in [-1,1]:herbs.leaf(centre,angle+sign*1.23,.07*math.sin(t*math.pi)+.015,.022+math.sin(t*math.pi)*.028,.16*math.sin(t*math.pi)+.02,int(rng.choice([0,1,6,7])))
                    counts['fern_fronds']+=1
        # Flower clouds have enough area/height to read from the whole-world camera.
        for flower in range(int(rng.integers(17,30))):
            px=x+rng.normal(0,sx*.54);py=y+rng.normal(0,sy*.54)
            if not valid_point(px,py) or float(eval_road_mask(px,py))>.18:continue
            z=float(sample_ground_z(px,py));h=float(rng.uniform(.35,.65));end=np.array([px+rng.uniform(-.07,.07),py,z+h])
            stem((px,py,z),end)
            for leaf in range(2):herbs.leaf((px,py,z+h*(.25+leaf*.22)),rng.uniform(0,math.tau),.075,.025,.085,17)
            if mood in [1,4]:
                # Fine compound yarrow corymb, uneven domed crown of small heads.
                for floret in range(7):
                    angle=floret*2.399;radius=.060*math.sqrt(floret/6)
                    flower_head(end+[radius*math.cos(angle),radius*math.sin(angle),.015*(1-radius/.065)],.021,15,5)
            else:flower_head(end,float(rng.uniform(.055,.085)),15 if mood!=3 else 16,11)
            counts['flower_stems']+=1
    for index,(x,y) in enumerate(boulder_anchors):
        # Unequal companion masses are half buried, never an evenly spaced bead ring.
        for part in range(int(rng.integers(3,7))):
            angle=float(rng.uniform(0,math.tau));distance=0 if part==0 else float(rng.uniform(.45,1.05))
            rx=x+distance*math.cos(angle);ry=y+distance*math.sin(angle)
            width=float(rng.uniform(1.25,1.90) if part==0 else rng.uniform(.70,1.35));depth=width*float(rng.uniform(.62,.90))
            exposed=float(rng.uniform(.70,1.10) if part==0 else rng.uniform(.40,.75));burial=float(rng.uniform(.45,.58));height=exposed/(1-burial)
            if not all(valid_point(rx+dx,ry+dy) and float(eval_road_mask(rx+dx,ry+dy))<.13 for dx,dy in [(0,0),(-width/2,0),(width/2,0),(0,-depth/2),(0,depth/2)]):continue
            name='Rock_'+str(int(rng.integers(1,6)));v=native[name+'_v'].copy();f=native[name+'_f'];lo=v.min(axis=0);span=np.maximum(v.max(axis=0)-lo,.001)
            v=(v-lo)/span;v=(v-[.5,.5,0])*[width,depth,height]
            angle=float(rng.uniform(0,math.tau));ca,sa=math.cos(angle),math.sin(angle);v[:,:2]=v[:,:2]@np.array([[ca,sa],[-sa,ca]])
            ground=float(sample_ground_z(rx,ry));rocks.add(v+[rx,ry,ground-height*burial],f,18);counts['native_boulders']+=1
            rock_records.append({'xy':[rx,ry],'width':width,'exposed_height':exposed,'buried_fraction':burial})
            # Low curved moss leaves grow from seams near the lower exposed flank.
            for leaf in range(14):
                a=float(rng.uniform(0,math.tau));base=(rx+math.cos(a)*width*.33,ry+math.sin(a)*depth*.33,ground+.04)
                herbs.leaf(base,a,rng.uniform(.10,.19),rng.uniform(.025,.060),rng.uniform(.12,.24),int(rng.choice([0,1,6,7,17])))
    native.close()
    materials=[_material(bpy,'World_HeroFlora_'+str(i),colour) for i,colour in enumerate(palette)]
    objects=[];stone_objects=[];triangles={}
    for name,batch in [('Arching_Sedge_Communities',sedges),('Curved_Ferns_Moss_Herbs',herbs),('Daisy_Yarrow_Flower_Clouds',flowers),('Selective_Native_Boulders',rocks)]:
        arrays=batch.arrays();mesh=bpy.data.meshes.new('World_HeroFlora_'+name)
        mesh.from_pydata(arrays['positions'].tolist(),[],arrays['triangles'].tolist());mesh.update();assert not mesh.validate(verbose=True)
        for material in materials:mesh.materials.append(material)
        mesh.polygons.foreach_set('material_index',arrays['material_indices'].astype(np.int32));mesh.polygons.foreach_set('use_smooth',np.ones(len(mesh.polygons),dtype=np.bool_))
        obj=bpy.data.objects.new(mesh.name,mesh);collection.objects.link(obj);obj['visual_only_nonblocking']=True;objects.append(obj)
        if name=='Selective_Native_Boulders':stone_objects.append(obj)
        triangles[name]=len(arrays['triangles'])
    return {'objects':objects,'stone_objects':stone_objects,'report':{'cluster_count':len(anchors),'boulder_cluster_count':len(boulder_anchors),'boulder_anchors':boulder_anchors,'rock_records':rock_records,'anchors':anchors,'counts':counts,'triangles':triangles,'roads_excluded':True,'stable_v4_grass_modified':False,'original_terrain_unchanged':True}}


if __name__=='__main__':
    sys.path.insert(0,str(ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'))
    import bpy
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'.collab/artifacts/reference-world-master-sol-high/reference-world-art-candidate.blend'))
    for obj in bpy.context.scene.objects:
        if obj.name.startswith('World_HeroFlora_'):obj.hide_render=True
    collection=bpy.data.collections.new('World_Meadow_Hero_Flora');bpy.context.scene.collection.children.link(collection)
    result=build_world_meadow_hero_flora(collection)
    rock_shader=bpy.data.materials.get('Natural_bank_stratified_stone')
    if rock_shader:
        for obj in result['stone_objects']:
            obj.data.materials.clear();obj.data.materials.append(rock_shader)
            obj.data.polygons.foreach_set('material_index',np.zeros(len(obj.data.polygons),dtype=np.int32))
    bpy.data.libraries.write(str(OUT/'meadow-hero-flora.blend'),{collection},compress=True)
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='CUDA';prefs.get_devices();cuda=[d for d in prefs.devices if d.type=='CUDA'];assert cuda
    for d in prefs.devices:d.use=d.type=='CUDA'
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='GPU';scene.cycles.samples=16;scene.cycles.use_denoising=True
    scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
    scene.render.filepath=str(OUT/'01-native-whole-hero-flora.png');bpy.ops.render.render(write_still=True)
    result['report']['compute_backend']='CUDA';result['report']['cuda_devices']=[d.name for d in cuda]
    (OUT/'hero-flora-evidence.json').write_text(json.dumps(result['report'],indent=2),encoding='utf8');print(json.dumps(result['report'],indent=2))
