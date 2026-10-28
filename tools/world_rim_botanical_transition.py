"""Low irregular vegetation on the existing exact outer rim cap, with no new floor."""
from pathlib import Path
import math
import numpy as np
from citadel_diorama_ruins_geometry import _eval_island_rim_xy
from map_surface_contract import sample_ground_z,eval_road_mask,eval_pond_distances,eval_river_center_and_width
from world_groundcover_art_pass import Batch,PALETTE,_material,KEEP_OUTS

ROOT=Path(__file__).resolve().parents[1]


def build_world_rim_botanical_transition(collection,seed=260619,count=1800):
    """Populate only the supplied collection; return objects/report, no terrain writes."""
    import bpy
    rng=np.random.default_rng(seed);leaves=Batch();ferns=Batch();rocks=Batch();anchors=[]

    def cap_z(x,y):return min(float(sample_ground_z(x,y))+.018,3.52)

    def safe(x,y,clearance=.0):
        if y>11 or (x>18 and y>-8):return False
        if float(eval_road_mask(x,y))>.24:return False
        if min(float(d) for d in eval_pond_distances(x,y))<1.24+clearance:return False
        if -8.8<y<12.9:
            cx,w=eval_river_center_and_width(y)
            if abs(x-float(cx))<float(w)*1.16+clearance:return False
        return not any((x-cx)**2+(y-cy)**2<(r+clearance)**2 for cx,cy,r in KEEP_OUTS)

    # The cap intentionally extends past the old .94 scatter cutoff. No regular rows.
    for attempt in range(count*35):
        if len(anchors)>=count:break
        angle=float(rng.uniform(0,math.tau));radius=float(rng.uniform(.92,.990))
        x,y=map(float,_eval_island_rim_xy(angle,radius))
        if not safe(x,y):continue
        field=.56+.27*math.sin(angle*9.1+math.sin(angle*2.4))+.15*math.cos(angle*23.3)
        outer_falloff=1.0 if radius<.98 else .20+.80*(.99-radius)/.01
        if rng.random()>max(.08,field)*outer_falloff:continue
        z=cap_z(x,y)-.008;anchors.append((x,y,z,angle,radius))
        height=float(rng.uniform(.15,.45))*(1-.30*max(0,(radius-.98)/.01))
        colour=int(rng.choice([0,1,2,3,4,5,6,7],p=[.08,.10,.07,.17,.20,.20,.10,.08]))
        for blade in range(int(rng.integers(5,9))):
            # Outward tips arch down slightly but roots stay on the existing cap.
            direction=float(rng.uniform(0,math.tau));reach=height*float(rng.uniform(.55,1.0))
            leaves.leaf((x+rng.normal(0,.025),y+rng.normal(0,.025),z),direction,height*rng.uniform(.70,1),rng.uniform(.008,.020),reach,colour)
        if len(anchors)%37==0 and radius<.979:
            for frond in range(5):
                a=float(rng.uniform(0,math.tau));reach=float(rng.uniform(.22,.38));h=float(rng.uniform(.18,.33));direction=np.array([math.cos(a),math.sin(a),0])
                for step in range(1,9):
                    t=step/9;centre=np.array([x,y,z])+direction*reach*t+[0,0,h*math.sin(t*math.pi*.83)]
                    for sign in [-1,1]:ferns.leaf(centre,a+sign*1.2,.025+.025*math.sin(t*math.pi),.018+.014*math.sin(t*math.pi),.025+.085*math.sin(t*math.pi),int(rng.choice([0,1,6,7])))

    # A few uneven, low embedded companions, never a continuous rock necklace.
    native=np.load(ROOT/'Tools/quaternius_nature_cache.npz',allow_pickle=False);rock_records=[]
    eligible=[p for p in anchors if .935<p[4]<.971 and p[1]<7]
    rng.shuffle(eligible);groups=[]
    for x,y,z,angle,radius in eligible:
        if len(groups)>=9:break
        if any((x-px)**2+(y-py)**2<5.0**2 for px,py in groups):continue
        if not safe(x,y,.30):continue
        groups.append((x,y))
        for k in range(int(rng.integers(2,4))):
            # Tangential stagger keeps every footprint inside the existing cap.
            offset=float(rng.uniform(-.35,.35));rx=x-math.sin(angle)*offset;ry=y+math.cos(angle)*offset
            name='Rock_'+str(int(rng.integers(1,6)));v=native[name+'_v'].copy();f=native[name+'_f'];lo=v.min(axis=0);span=np.maximum(v.max(axis=0)-lo,.001)
            width=float(rng.uniform(.30,.68));depth=width*rng.uniform(.65,.90);height=float(rng.uniform(.27,.57));burial=float(rng.uniform(.48,.64))
            v=((v-lo)/span-[.5,.5,0])*[width,depth,height]
            yaw=float(rng.uniform(0,math.tau));c,s=math.cos(yaw),math.sin(yaw);v[:,:2]=v[:,:2]@np.array([[c,s],[-s,c]])
            rz=cap_z(rx,ry)-height*burial;rocks.add(v+[rx,ry,rz],f,0);rock_records.append({'xy':[rx,ry],'buried_fraction':burial})
    native.close()
    materials=[_material(bpy,'Rim_Botanical_'+str(i),colour) for i,colour in enumerate(PALETTE)]
    objects=[];triangles={}
    stone_shader=bpy.data.materials.get('Natural_bank_stratified_stone')
    for name,batch in [('Thin_Curved_Cap_Blades',leaves),('Low_Curved_Cap_Ferns',ferns),('Sparse_Embedded_Cap_Rocks',rocks)]:
        arrays=batch.arrays()
        if not len(arrays['triangles']):continue
        mesh=bpy.data.meshes.new('World_RimTransition_'+name);mesh.from_pydata(arrays['positions'].tolist(),[],arrays['triangles'].tolist());mesh.update();assert not mesh.validate(verbose=True)
        if name=='Sparse_Embedded_Cap_Rocks' and stone_shader:
            mesh.materials.append(stone_shader);indices=np.zeros(len(mesh.polygons),dtype=np.int32)
        else:
            for material in materials:mesh.materials.append(material)
            indices=arrays['material_indices'].astype(np.int32)
        mesh.polygons.foreach_set('material_index',indices);mesh.polygons.foreach_set('use_smooth',np.ones(len(mesh.polygons),dtype=np.bool_))
        obj=bpy.data.objects.new(mesh.name,mesh);collection.objects.link(obj);obj['visual_only_nonblocking']=True;objects.append(obj);triangles[name]=len(mesh.polygons)
    return {'objects':objects,'report':{'clump_count':len(anchors),'cap_radius_range':[.92,.99],'cap_height':'min(sample_ground_z(x,y)+.018,3.52)','exact_boundary':'citadel_diorama_ruins_geometry._eval_island_rim_xy','outward_density_falloff':True,'rock_cluster_count':len(groups),'rock_records':rock_records,'triangles':triangles,'terrain_or_stable_flora_changed':False,'colliders_added':False}}
