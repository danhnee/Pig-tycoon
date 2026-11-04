"""Lush botanical communities and crafted masonry on an explicitly supplied layout.

No legacy map coordinates, master edits, terrain shader edits or gameplay changes.
"""
from pathlib import Path
import importlib.util
import math
import numpy as np
from world_groundcover_art_pass import Batch,_material

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.collab/artifacts/world-lush-meadow-ruins-sol-high'
LUSH_PALETTE=[(.34,.36,.07,1),(.48,.43,.065,1),(.61,.48,.045,1),(.73,.54,.075,1),
              (.39,.48,.11,1),(.27,.38,.085,1),(.78,.58,.085,1),(.87,.70,.20,1),
              (.38,.18,.52,1),(.53,.28,.65,1),(.31,.30,.15,1)]


def _anchor(value,index=0):
    if isinstance(value,dict):
        return float(value['x']),float(value['y']),float(value.get('scale',1)),float(value.get('yaw',index*2.399)),int(value.get('type',index%4))
    return float(value[0]),float(value[1]),float(value[2] if len(value)>2 else 1),index*2.399,index%4


def _safe(surface,x,y):
    # The explicit expanded surface contract owns its exact water/pad/trail masks.
    valid=np.asarray(surface.is_walkable(x,y),dtype=np.bool_)
    return bool(valid) if valid.ndim==0 else valid


def build_rejected_olive_patch_for_evidence_only(seed=260719):
    """REJECTED by the user. Preserved for evidence only; never an art fallback."""
    rng=np.random.default_rng(seed);batch=Batch()
    # Soft overlapping curved clover foliage, not a base of dry upright wires.
    for index in range(430):
        x,y=rng.uniform([-2,-1.5],[2,1.5])
        if (x/2)**2+(y/1.5)**2>rng.uniform(.75,1.25):continue
        colour=int(rng.choice([0,1,2,3,4,5],p=[.12,.16,.21,.17,.19,.15]))
        base=(x,y,rng.uniform(0,.025));reach=float(rng.uniform(.10,.22));h=float(rng.uniform(.065,.16))
        for leaf in range(int(rng.integers(7,12))):
            batch.leaf(base,rng.uniform(0,math.tau),h*rng.uniform(.75,1.15),rng.uniform(.045,.095),reach*rng.uniform(.6,1.3),colour)
        if index%8==0:
            for blade in range(5):batch.leaf((x,y,.015),rng.uniform(0,math.tau),rng.uniform(.22,.42),rng.uniform(.009,.018),rng.uniform(.15,.32),colour)
        if index%3==0:
            height=float(rng.uniform(.14,.32));centre=np.array([x,y,height]);purple=index%15==0
            batch.leaf((x,y,0),rng.uniform(0,math.tau),height*1.05,.008,.018,5)
            for petal in range(8):batch.leaf(centre,petal*math.tau/8+rng.uniform(-.1,.1),.023,.027,.055,int(rng.choice([8,9])) if purple else int(rng.choice([6,7])))
            for leaf in range(3):batch.leaf((x,y,height*.4),rng.uniform(0,math.tau),.06,.028,.09,4)
    arrays=batch.arrays();arrays['palette']=LUSH_PALETTE;arrays['report']={'source':'local_dense_curved_clover_fallback','seed':seed}
    return arrays


def _patch_source(seed):
    # Olive foliage and the later uniform large-flower carpet were both rejected.
    # Field tiles contain low sward only; taller ornaments belong at ruin anchors.
    path=OUT/'argon-short-sward-v5/short_sward_module.py'
    if path.exists():
        spec=importlib.util.spec_from_file_location('owned_argon_short_sward_patch',path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        result=module.build_short_sward_patch_arrays(seed=seed)
        for key in ['positions','triangles','material_indices','palette']:assert key in result,(path,key)
        return result,str(path)
    raise RuntimeError('Rejected olive and large-flower sources are disabled. Build and visually review the new short-sward candidate before integration.')


def suggest_lush_anchors(surface,seed=260719,meadow_count=1400,ruin_count=16):
    """Initial anchors from the supplied world extents, fully overridable by the assembler."""
    rng=np.random.default_rng(seed)
    bounds=getattr(surface,'BOUNDS',None)
    if bounds is None:
        width=getattr(surface,'WORLD_WIDTH',getattr(surface,'WIDTH',None));height=getattr(surface,'WORLD_HEIGHT',getattr(surface,'HEIGHT',None))
        if width is None or height is None:raise ValueError('Pass explicit anchors or expose surface BOUNDS/WORLD_WIDTH/WORLD_HEIGHT.')
        bounds=(-float(width)/2,float(width)/2,-float(height)/2,float(height)/2)
    if isinstance(bounds,dict):xmin,xmax,ymin,ymax=[bounds[k] for k in ['xmin','xmax','ymin','ymax']]
    else:xmin,xmax,ymin,ymax=bounds
    meadow=[];ruins=[];grid={};spacing=1.15 if meadow_count>1400 else (1.45 if meadow_count>800 else 2.2)
    points=rng.uniform((xmin+3,ymin+3),(xmax-3,ymax-3),size=(meadow_count*50,2))
    try:
        allowed=np.asarray(_safe(surface,points[:,0],points[:,1]),dtype=bool)
        for dx,dy in [(-2,0),(2,0),(0,-1.5),(0,1.5)]:allowed &= np.asarray(_safe(surface,points[:,0]+dx,points[:,1]+dy),dtype=bool)
        points=points[allowed]
    except (TypeError,ValueError):points=np.array([(x,y) for x,y in points if all(_safe(surface,x+dx,y+dy) for dx,dy in [(0,0),(-2,0),(2,0),(0,-1.5),(0,1.5)])])
    for x,y in points:
        x,y=float(x),float(y)
        cell=(math.floor(x/spacing),math.floor(y/spacing))
        neighbours=[p for dx in [-1,0,1] for dy in [-1,0,1] for p in grid.get((cell[0]+dx,cell[1]+dy),[])]
        if any((x-p[0])**2+(y-p[1])**2<spacing**2 for p in neighbours):continue
        grid.setdefault(cell,[]).append((x,y))
        meadow.append({'x':x,'y':y,'scale':float(rng.uniform(.72,1.15)),'yaw':float(rng.uniform(0,math.tau))})
        if len(meadow)>=meadow_count:break
    for point in meadow:
        if len(ruins)>=ruin_count:break
        x,y=point['x'],point['y']
        if any((x-p['x'])**2+(y-p['y'])**2<12**2 for p in ruins):continue
        scale=float(rng.uniform(.85,1.20));c,s=math.cos(point['yaw']),math.sin(point['yaw'])
        if not all(_safe(surface,x+dx*c-dy*s,y+dx*s+dy*c) for dx,dy in [(-2.2*scale,0),(2.2*scale,0),(0,-1.3*scale),(0,1.3*scale)]):continue
        ruins.append({'x':x,'y':y,'scale':scale,'yaw':point['yaw'],'type':len(ruins)%4})
    return {'meadow_anchors':meadow,'ruin_anchors':ruins}


def build_world_lush_meadow_ruins(collection,surface,meadow_anchors=(),ruin_anchors=(),seed=260719,meadow_mode='linked_tiles'):
    """Return objects/stone_objects/report. Surface supplies sample_ground_z/is_walkable."""
    import bpy
    assert callable(surface.sample_ground_z) and callable(surface.is_walkable)
    rng=np.random.default_rng(seed);objects=[];stone_objects=[];meadow_records=[];ruin_records=[]
    source,source_path=_patch_source(seed)
    palette=[_material(bpy,'Living_Meadow_'+str(i),tuple(colour)) for i,colour in enumerate(source['palette'])]
    for material in palette:
        shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.72
        shader.inputs['Subsurface Weight'].default_value=.075
    positions=np.asarray(source['positions'],dtype=np.float32);faces=np.asarray(source['triangles'],dtype=np.int32);indices=np.asarray(source['material_indices'],dtype=np.int32)
    assert np.isfinite(positions).all() and faces.min()>=0 and faces.max()<len(positions)

    def ground(x,y):return float(surface.sample_ground_z(float(x),float(y)))
    def mesh_object(name,v,f,origin=(0,0,0),yaw=0,material=None,bevel=0,smooth=False):
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(np.asarray(v).tolist(),[],np.asarray(f).tolist() if isinstance(f,np.ndarray) else f);mesh.update();assert not mesh.validate(verbose=True),name
        if material:mesh.materials.append(material)
        if smooth:mesh.polygons.foreach_set('use_smooth',np.ones(len(mesh.polygons),dtype=np.bool_))
        obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj);obj.location=origin;obj.rotation_euler.z=yaw;obj['visual_only_nonblocking']=True;objects.append(obj)
        if bevel:
            modifier=obj.modifiers.new('Worn_Masonry_Edges','BEVEL');modifier.width=bevel;modifier.segments=3
            modifier=obj.modifiers.new('Crafted_Stone_Normals','WEIGHTED_NORMAL');modifier.keep_sharp=True
        return obj

    tile_cache={};maximum_tangent_error=0;linked_tiles=0;exact_tiles=0
    if meadow_mode=='linked_tiles':
        centres=positions[faces,:2].mean(axis=1)
        for tx in [-1,1]:
            for ty in [-1,1]:
                mask=(centres[:,0]*tx>=0)&(centres[:,1]*ty>=0);selected=faces[mask];used=np.unique(selected);mapping=np.full(len(positions),-1,dtype=np.int32);mapping[used]=np.arange(len(used))
                centre=np.array([tx*1.,ty*.75]);v=positions[used].copy();v[:,:2]-=centre
                mesh=bpy.data.meshes.new(f'LivingMeadow_SharedTile_{tx}_{ty}');mesh.from_pydata(v.tolist(),[],mapping[selected].tolist());mesh.update();assert not mesh.validate(verbose=True)
                for material in palette:mesh.materials.append(material)
                mesh.polygons.foreach_set('material_index',indices[mask]);mesh.polygons.foreach_set('use_smooth',np.ones(len(mesh.polygons),dtype=np.bool_))
                tile_cache[(tx,ty)]=(mesh,centre,v,mapping[selected])
    for index,value in enumerate(meadow_anchors):
        x,y,scale,yaw,_=_anchor(value,index)
        if not _safe(surface,x,y):continue
        if meadow_mode=='linked_tiles':
            from mathutils import Matrix
            c,s=math.cos(yaw),math.sin(yaw);triangles=0
            for (tx,ty),(shared,centre,local,local_faces) in tile_cache.items():
                px=x+scale*(centre[0]*c-centre[1]*s);py=y+scale*(centre[0]*s+centre[1]*c)
                probes=[(px+scale*(dx*c-dy*s),py+scale*(dx*s+dy*c)) for dx,dy in [(0,0),(-1,-.75),(-1,.75),(1,-.75),(1,.75)]]
                if not all(_safe(surface,qx,qy) for qx,qy in probes):continue
                z=ground(px,py)-.008;gx=(ground(px+.25,py)-ground(px-.25,py))/.5;gy=(ground(px,py+.25)-ground(px,py-.25))/.5
                error=max(abs(ground(qx,qy)-(z+.008+gx*(qx-px)+gy*(qy-py))) for qx,qy in probes);maximum_tangent_error=max(maximum_tangent_error,error)
                if error>.06:
                    exact_tiles+=1
                    v=local.copy()*scale;v[:,:2]=v[:,:2]@np.array([[c,s],[-s,c]]);v[:,:2]+=[px,py]
                    v[:,2]+=np.asarray(surface.sample_ground_z(v[:,0],v[:,1]))-.008
                    obj=mesh_object(f'LivingMeadow_DrapedTile_{index}_{tx}_{ty}',v,local_faces,smooth=True)
                    for material in palette:obj.data.materials.append(material)
                    obj.data.polygons.foreach_set('material_index',np.asarray([p.material_index for p in shared.polygons],dtype=np.int32))
                else:
                    linked_tiles+=1
                    obj=bpy.data.objects.new(f'LivingMeadow_LinkedTile_{index}_{tx}_{ty}',shared);collection.objects.link(obj);objects.append(obj);obj['visual_only_nonblocking']=True
                    obj.matrix_world=Matrix(((scale*c,-scale*s,0,px),(scale*s,scale*c,0,py),(scale*(gx*c+gy*s),scale*(-gx*s+gy*c),scale,z),(0,0,0,1)))
                triangles+=len(local_faces)
            if triangles:meadow_records.append({'xy':[x,y],'scale':scale,'triangles':triangles,'mode':'linked_local_tangent_tiles_with_exact_drape_above_6cm'})
            continue
        c,s=math.cos(yaw),math.sin(yaw);v=positions.copy()*scale;v[:,:2]=v[:,:2]@np.array([[c,s],[-s,c]]);v[:,:2]+=[x,y]
        try:
            keep=np.asarray(_safe(surface,v[:,0],v[:,1]),dtype=np.bool_)
            if keep.shape!=(len(v),):raise ValueError('Scalar-only guard')
        except (TypeError,ValueError):keep=np.fromiter((bool(_safe(surface,float(px),float(py))) for px,py in v[:,:2]),dtype=np.bool_,count=len(v))
        selected=faces[np.all(keep[faces],axis=1)]
        if not len(selected):continue
        used=np.unique(selected);mapping=np.full(len(v),-1,dtype=np.int32);mapping[used]=np.arange(len(used));v=v[used]
        try:height=np.asarray(surface.sample_ground_z(v[:,0],v[:,1]),dtype=float)
        except (TypeError,ValueError):height=np.fromiter((ground(px,py) for px,py in v[:,:2]),dtype=float,count=len(v))
        v[:,2]+=height-.008
        mask=np.all(keep[faces],axis=1);obj=mesh_object(f'LivingMeadow_Expanded_Community_{index:03d}',v,mapping[selected],smooth=True)
        for material in palette:obj.data.materials.append(material)
        obj.data.polygons.foreach_set('material_index',indices[mask]);meadow_records.append({'xy':[x,y],'scale':scale,'triangles':len(selected)})

    masonry=_material(bpy,'CraftedAncient_AgedMasonry',(.40,.355,.27,1));masonry.use_nodes=True
    nodes,links=masonry.node_tree.nodes,masonry.node_tree.links;shader=nodes.get('Principled BSDF')
    coord=nodes.new('ShaderNodeNewGeometry');noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=2.8;noise.inputs['Detail'].default_value=4
    links.new(coord.outputs['Position'],noise.inputs['Vector']);ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.29,.275,.235,1);ramp.color_ramp.elements[1].color=(.52,.46,.345,1)
    links.new(noise.outputs['Fac'],ramp.inputs['Fac']);links.new(ramp.outputs['Color'],shader.inputs['Base Color'])
    fine=nodes.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=46;fine.inputs['Detail'].default_value=3;links.new(coord.outputs['Position'],fine.inputs['Vector'])
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.28;bump.inputs['Distance'].default_value=.022;links.new(fine.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],shader.inputs['Normal'])
    moss=_material(bpy,'CraftedAncient_GoldenMoss',(.55,.38,.045,1))

    def stone(name,v,f,origin,yaw,bevel=.030,smooth=False):
        obj=mesh_object(name,v,f,origin,yaw,masonry,bevel,smooth);obj['surface_role']='crafted_masonry';stone_objects.append(obj);return obj

    def block(name,centre,size,origin,yaw,broken=False):
        sx,sy,sz=np.asarray(size)*.5;v=np.array([[-sx,-sy,-sz],[sx,-sy,-sz],[sx,sy,-sz],[-sx,sy,-sz],[-sx,-sy,sz],[sx,-sy,sz],[sx,sy,sz],[-sx,sy,sz]])
        v+=rng.uniform(-.018,.018,v.shape)
        if broken:v[4:,2]+=rng.uniform(-.045,.025,4)
        v+=centre;f=[[0,3,2,1],[4,5,6,7],[0,1,5,4],[1,2,6,5],[2,3,7,6],[3,0,4,7]]
        return stone(name,v,f,origin,yaw)

    def column(name,centre,height,radius,origin,yaw,fallen=False):
        sides=64;rings=12;v=[];f=[];fracture=rng.uniform(-.045,.025,sides)
        for row in range(rings):
            t=row/(rings-1)
            for j in range(sides):
                a=j*math.tau/sides;r=radius*(1-.095*math.cos(a*14))*(1-.085*t)
                v.append(np.array(centre)+[r*math.cos(a)+.035*t,r*math.sin(a),height*t+(fracture[j] if row==rings-1 else 0)])
        for row in range(rings-1):
            for j in range(sides):f.append([row*sides+j,row*sides+(j+1)%sides,(row+1)*sides+(j+1)%sides,(row+1)*sides+j])
        f.extend([list(range(sides-1,-1,-1)),list(range((rings-1)*sides,rings*sides))]);obj=stone(name,v,f,origin,yaw,.007,True)
        if fallen:obj.rotation_euler.x=1.45
        return obj

    def voussoir(name,radius,a0,a1,spring,origin,yaw):
        angles=np.linspace(a0,a1,12);v=[];n=len(angles)
        for depth in [-.20,.20]:
            for r in [radius,radius+.23]:v.extend([(r*math.cos(a),depth,spring+r*math.sin(a)) for a in angles])
        f=[]
        for j in range(n-1):f.extend([[j,j+1,n+j+1,n+j],[2*n+j,3*n+j,3*n+j+1,2*n+j+1],[j,2*n+j,2*n+j+1,j+1],[n+j,n+j+1,3*n+j+1,3*n+j]])
        f.extend([[0,n,3*n,2*n],[n-1,3*n-1,4*n-1,2*n-1]]);return stone(name,v,f,origin,yaw,.011)

    def fallen_arc(name,centre,origin,yaw):
        obj=voussoir(name,1.,math.pi*.36,math.pi*.48,0,origin,yaw)
        v=np.array([vertex.co[:] for vertex in obj.data.vertices]);v-=v.mean(axis=0)
        angle=1.2;c,s=math.cos(angle),math.sin(angle);v=v@np.array([[1,0,0],[0,c,s],[0,-s,c]])
        v[:,2]-=v[:,2].min()+.08;v+=centre
        obj.data.vertices.foreach_set('co',v.astype(np.float32).ravel());obj.data.update();return obj

    def relief(name,centre,width,height,origin,yaw):
        nx,nz=48,40;v=[];f=[]
        for j in range(nz):
            z=height*j/(nz-1)
            for i in range(nx):
                x=width*(i/(nx-1)-.5);line=min(abs(z-height*.23-.045*math.sin(x*8)),abs(z-height*.73-.045*math.cos(x*9)),abs(abs(x)-width*.39))
                cut=.025*math.exp(-(line/.012)**2);v.append(np.asarray(centre)+[x,-.16+cut,z+(rng.uniform(-.04,.03) if j==nz-1 else 0)])
        for j in range(nz-1):
            for i in range(nx-1):a=j*nx+i;f.append([a,a+nx,a+nx+1,a+1])
        return stone(name,v,f,origin,yaw,.004,True)

    for index,value in enumerate(ruin_anchors):
        x,y,scale,yaw,kind=_anchor(value,index)
        # Ruins have footprints; keep complete fragments off new routes and pads.
        c,s=math.cos(yaw),math.sin(yaw)
        if not all(_safe(surface,x+dx*c-dy*s,y+dx*s+dy*c) for dx,dy in [(0,0),(-2.2*scale,0),(2.2*scale,0),(0,-1.3*scale),(0,1.3*scale)]):continue
        origin=(x,y,ground(x,y)-.12);before=len(objects);prefix=f'CraftedAncient_{index:02d}'
        if kind%4==0:
            # Ragged masonry wall with a genuine open central doorway.
            for side in [-1,1]:
                for course in range(4 if side<0 else 3):
                    for piece in range(2):
                        px=side*(.82+piece*.54)+rng.uniform(-.035,.035)
                        block(prefix+f'_TornWall_{side}_{course}_{piece}',(px,0,.13+course*.26),(.50,.43,.24),origin,yaw,course>=2)
            for segment in range(12):
                if segment not in [0,1,10,11]:continue
                voussoir(prefix+f'_DoorArc_{segment}',.47,segment*math.pi/12+.01,(segment+1)*math.pi/12-.01,.62,origin,yaw)
            fallen_arc(prefix+'_FallenDoorVoussoir_A',(.25,-.68,.03),origin,yaw)
            fallen_arc(prefix+'_FallenDoorVoussoir_B',(-.45,-.61,.04),origin,yaw)
            relief(prefix+'_WallEngravedFrieze',(-1.34,-.057,.28),.65,.45,origin,yaw)
        elif kind%4==1:
            column(prefix+'_FlutedColumn_A',(-.72,0,0),1.25,.25,origin,yaw)
            column(prefix+'_FlutedColumn_B',(.78,.17,0),.73,.27,origin,yaw)
            block(prefix+'_FallenCarvedLintel',(.25,-.62,.12),(2.00,.49,.32),origin,yaw,True)
            relief(prefix+'_LintelRelief',(.25,-.707,.00),1.84,.22,origin,yaw)
            column(prefix+'_BuriedColumnDrum',(.5,.65,-.12),.58,.29,origin,yaw)
        elif kind%4==2:
            column(prefix+'_ArcFlutedPier',(-.74,0,0),.38,.24,origin,yaw)
            for segment in [14,15]:voussoir(prefix+f'_CurvedCarvedArc_{segment}',.65,segment*math.pi/16+.009,(segment+1)*math.pi/16-.009,.38,origin,yaw)
            column(prefix+'_OppositeFracturedPier',(.74,0,0),.27,.25,origin,yaw)
            for segment in [0,1]:voussoir(prefix+f'_OppositeArcSpring_{segment}',.65,segment*math.pi/16+.009,(segment+1)*math.pi/16-.009,.27,origin,yaw)
            block(prefix+'_FallenArchKeystone',(.55,-.55,.11),(.55,.52,.30),origin,yaw,True)
            fallen_arc(prefix+'_GroundBuriedVoussoir_A',(-.45,-.60,.04),origin,yaw)
            fallen_arc(prefix+'_GroundBuriedVoussoir_B',(.20,-.84,.03),origin,yaw)
        else:
            for course in range(3):
                for piece in range(3-course//2):block(prefix+f'_BrokenCorner_A_{course}_{piece}',(-.65+piece*.57,0,.13+course*.27),(.53,.43,.24),origin,yaw,course>=2)
                for piece in range(2):block(prefix+f'_BrokenCorner_B_{course}_{piece}',(-.83,.48+piece*.53,.13+course*.27),(.43,.49,.24),origin,yaw,course>=2)
            block(prefix+'_SteleBody',(1.05,-.1,.46),(.67,.32,.92),origin,yaw,True)
            relief(prefix+'_TallIncisedStele',(1.05,-.102,0),.63,.96,origin,yaw)
            block(prefix+'_BuriedFallenRelief',(.55,-.72,.09),(1.24,.52,.28),origin,yaw,True)
        # Scale each crafted assembly around its own explicit anchor.
        for obj in objects[before:]:obj.scale=(scale,scale,scale)
        moss_batch=Batch()
        for tuft in range(55):
            px=float(rng.uniform(-1.75,1.75));py=float(rng.uniform(-.90,.90))
            for leaf in range(6):moss_batch.leaf((px,py,.02),rng.uniform(0,math.tau),rng.uniform(.045,.12),rng.uniform(.035,.075),rng.uniform(.07,.16),0)
        arrays=moss_batch.arrays();obj=mesh_object(prefix+'_LivingGoldenMossSeams',arrays['positions'],arrays['triangles'],origin,yaw,moss,smooth=True);obj.scale=(scale,scale,scale)
        ruin_records.append({'xy':[x,y],'type':['torn_doorway_wall','fluted_colonnade_fallen_lintel','carved_broken_arc','broken_corner_relief_stele'][kind%4],'scale':scale,'actual_open_void':kind%4 in [0,2]})
    return {'objects':objects,'stone_objects':stone_objects,'report':{'layout_module':getattr(surface,'__name__',type(surface).__name__),'meadow_source':source_path,'meadow_mode':meadow_mode,'shared_template_meshes':len(tile_cache),'linked_tile_instances':linked_tiles,'exact_draped_tiles':exact_tiles,'maximum_pre_drape_tangent_probe_error':maximum_tangent_error,'meadow_communities':meadow_records,'ruin_clusters':ruin_records,'ruin_type_count':len({p['type'] for p in ruin_records}),'old_world_coordinates_used':False,'terrain_materials_modified':False,'colliders_added':False,'approved_buildings_modified':False}}
