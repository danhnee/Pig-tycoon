"""Native expanded-world water. No old-world scaling or source scene writes.

Integration: build_world_expanded_water(collection, layout).
Pure shared terrain helpers: pond_signed_distance, water_surface_height,
carve_water_ground. Locations/levels come only from the supplied layout.
"""
from __future__ import annotations
import math,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.collab/artifacts/world-expanded-water-sol-high'
RUNTIME=ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'
sys.dont_write_bytecode=True
if str(RUNTIME) not in sys.path:sys.path.insert(0,str(RUNTIME))
import numpy as np


def configure_water_layout(layout,base_ground_z):
    """Freeze native water paths BEFORE terrain/asset construction; returns layout.

    Coordinates follow the agreed expanded contract: castle(29,39), bridge(28,13).
    Heights are sampled from that contract's PRE-water terrain, never fixed Z2.
    """
    if not layout.get('ponds'):
        raise ValueError('Latest user requires one explicit central pond; never recreate historical3-pond layout')
    if len(layout['ponds'])!=1:raise ValueError('Latest user override permits exactly ONE central meadow pond')
    def native_points(xy):
        result=[];last=float('inf')
        for x,y in xy:
            z=min(float(base_ground_z(x,y))-.10,last-.025);result.append([x,y,z]);last=z
        return result
    a=native_points([(30.6,36.8),(31.0,34.4),(31.7,31.8),(32.4,29.5),(33.0,27.6)])
    b=native_points([(37.1,34.0),(36.0,31.5),(34.6,29.3),(33.4,27.6)])
    layout['cascades']=[{'id':'western-stepped-chute','points':a,'width':1.20,'half_width':.88,'depth':.06,'bank_width':1.8,'contact_indices':[2,3,4]}, {'id':'eastern-split-chute','points':b,'width':.72,'half_width':.60,'depth':.06,'bank_width':1.55,'contact_indices':[1,2,3]}]
    # The steep upper reaches are split liquid cores. A broad river body begins
    # only after the last contact, so it cannot form a fullwidth vertical sheet.
    river=native_points([(33.2,27.6),(33.0,20.0),(30.0,16.0),(28.0,13.0),(32.0,8.0),(40.0,3.0),(49.0,-6.0),(57.0,-16.0)])
    toe_level=min(a[-1][2],b[-1][2],river[0][2])
    a[-1][2]=b[-1][2]=river[0][2]=toe_level
    layout['rivers']=[{'id':'eastern-meandering-river','points':river,'half_width':1.65,'depth':.62,'bank_width':2.0,'bridge_crossing':[28.,13.]}]
    layout['water_contract_version']='expanded-native120x90-organic-water-v1'
    return layout


def _smooth(value):
    t=np.clip(value,0,1)
    return t*t*(3-2*t)


def pond_signed_distance(x,y,pond):
    """Shared irregular shoreline, approximate signed metres; negative in water."""
    x,y=np.asarray(x,dtype=float),np.asarray(y,dtype=float)
    cx,cy=pond['center'];rx,ry=pond['radii']
    angle=math.radians(pond.get('angle_deg',0));ca,sa=math.cos(angle),math.sin(angle)
    dx,dy=x-cx,y-cy;u=(dx*ca+dy*sa)/rx;v=(-dx*sa+dy*ca)/ry
    theta=np.arctan2(v,u);phase=float(pond.get('seed',1))*.73
    shore=1+.13*np.sin(theta*3+phase)+.075*np.cos(theta*5-phase*.7)+.045*np.sin(theta*8+phase*1.7)
    # A shallow inlet breaks the long smooth oval silhouette.
    inlet=np.exp(-((np.arctan2(np.sin(theta-phase),np.cos(theta-phase)))/.40)**2)
    shore-=.15*inlet
    return (np.sqrt(u*u+v*v)/shore-1)*min(rx,ry)


def pond_outline(pond,count=128):
    cx,cy=pond['center'];rx,ry=pond['radii'];angle=math.radians(pond.get('angle_deg',0));ca,sa=math.cos(angle),math.sin(angle)
    t=np.linspace(0,math.tau,count,endpoint=False);phase=float(pond.get('seed',1))*.73
    inlet=np.exp(-(np.arctan2(np.sin(t-phase),np.cos(t-phase))/.40)**2)
    r=1+.13*np.sin(t*3+phase)+.075*np.cos(t*5-phase*.7)+.045*np.sin(t*8+phase*1.7)-.15*inlet
    u,v=rx*r*np.cos(t),ry*r*np.sin(t)
    return np.column_stack((cx+u*ca-v*sa,cy+u*sa+v*ca))


def water_surface_height(x,y,layout):
    """NaN on dry land; constant actual metre level inside each organic pond."""
    x,y=np.broadcast_arrays(np.asarray(x,dtype=float),np.asarray(y,dtype=float))
    height=np.full(x.shape,np.nan,dtype=float)
    for pond in layout.get('ponds',[]):height=np.where(pond_signed_distance(x,y,pond)<=0,float(pond['level']),height)
    for river in layout.get('cascades',[])+layout.get('rivers',[]):
        d,level,width=river_section(x,y,river)
        height=np.where(d<=0,level,height)
    return height


def shore_wetness(x,y,layout):
    """Native terrain material helper: soft narrow wet soil, no painted oval."""
    x,y=np.broadcast_arrays(np.asarray(x,dtype=float),np.asarray(y,dtype=float));wet=np.zeros(x.shape)
    for pond in layout.get('ponds',[]):wet=np.maximum(wet,np.exp(-np.abs(pond_signed_distance(x,y,pond))/.34))
    for river in layout.get('rivers',[])+layout.get('cascades',[]):
        d,_,_=river_section(x,y,river);wet=np.maximum(wet,np.exp(-np.abs(d)/.30))
    return wet


def channel_path(channel,subdivisions=10):
    points=np.asarray(channel['points'],dtype=float)
    if len(points)<2 or points.shape[1]!=3:raise ValueError('Channel needs ordered native XYZ points')
    result=[]
    for a,b in zip(points[:-1],points[1:]):
        for t in np.linspace(0,1,subdivisions,endpoint=False):result.append(a*(1-t)+b*t)
    result.append(points[-1])
    return np.asarray(result)


def river_section(x,y,channel):
    """Signed shore metres, level and half-width at nearest native path segment."""
    x,y=np.broadcast_arrays(np.asarray(x,dtype=float),np.asarray(y,dtype=float))
    best=np.full(x.shape,np.inf);height=np.zeros(x.shape);half=float(channel.get('half_width',channel.get('width',1.)/2))
    points=np.asarray(channel['points'],dtype=float)
    for a,b in zip(points[:-1],points[1:]):
        delta=b[:2]-a[:2];denom=float(delta@delta)
        if denom<1e-10:continue
        t=np.clip(((x-a[0])*delta[0]+(y-a[1])*delta[1])/denom,0,1)
        distance=np.hypot(x-(a[0]+t*delta[0]),y-(a[1]+t*delta[1]))
        choose=distance<best;best=np.minimum(best,distance);height=np.where(choose,a[2]+t*(b[2]-a[2]),height)
    return best-half,height,np.full(x.shape,half)


def carve_water_ground(x,y,base_z,layout):
    """Only shallow basins and soft banks; same shoreline as the actual surface.

    Pond levels must be picked from the new native landscape, near its local
    surface. These helpers never invoke the old62x47 sampler.
    """
    x,y,z=np.broadcast_arrays(np.asarray(x,dtype=float),np.asarray(y,dtype=float),np.asarray(base_z,dtype=float))
    z=z.copy()
    for pond in layout.get('ponds',[]):
        d=pond_signed_distance(x,y,pond);level=float(pond['level']);depth=float(pond.get('depth',.60));bank=float(pond.get('bank_width',1.5))
        inside=level-depth*_smooth(-d/max(.5,min(pond['radii'])*.82))
        # Outside lip is only centimetres above the water and eases into the
        # unmodified native ground over an explicit broad bank, never a wall.
        shelf=level+.16*np.maximum(d,0)
        target=np.where(d<=0,inside,shelf)
        weight=np.where(d<=0,1.,1-_smooth(d/bank))
        z=z*(1-weight)+target*weight
    for river in layout.get('cascades',[])+layout.get('rivers',[]):
        d,level,width=river_section(x,y,river);bank=float(river.get('bank_width',1.5));depth=float(river.get('depth',.50))
        across=np.clip((d+width)/width,0,1)
        inside=level-depth*(1-across*across)**1.35
        target=np.where(d<=0,inside,level+.16*np.maximum(d,0))
        weight=np.where(d<=0,1.,1-_smooth(d/bank));z=np.minimum(z,z*(1-weight)+target*weight)
    return z


def _mesh(collection,name,v,f,material,smooth=True):
    import bpy
    mesh=bpy.data.meshes.new(name+'_Mesh');mesh.from_pydata(v,[],f);mesh.update()
    if material:mesh.materials.append(material)
    for face in mesh.polygons:face.use_smooth=smooth
    uv=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        x,y,z=mesh.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(x*.18,y*.18)
    obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj);obj['expanded_water']=True
    return obj


def _liquid_material(name,fall=False):
    import bpy
    mat=bpy.data.materials.new(name);mat.use_nodes=True;nodes,links=mat.node_tree.nodes,mat.node_tree.links
    # Water itself is clear. Blue belongs to depth absorption/reflection, not a
    # dark diffuse surface tint that turns centimetre-thin flows into plastic.
    shader=nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(.86,.96,.985,1)
    shader.inputs['Roughness'].default_value=.075 if fall else .065;shader.inputs['IOR'].default_value=1.333
    shader.inputs['Transmission Weight'].default_value=.92 if fall else 1.;shader.inputs['Coat Weight'].default_value=.10
    tex=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(1.1,1.1,6.0) if fall else (1,1,1)
    links.new(tex.outputs['Object'],mapping.inputs[0]);noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=11 if fall else 3.2;noise.inputs['Detail'].default_value=3;noise.inputs['Roughness'].default_value=.6
    links.new(mapping.outputs[0],noise.inputs['Vector']);bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.012 if fall else .018
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],shader.inputs['Normal'])
    volume=nodes.new('ShaderNodeVolumeAbsorption');volume.inputs['Color'].default_value=(.06,.52,.68,1) if fall else (.06,.32,.72,1);volume.inputs['Density'].default_value=4.0 if fall else 2.2
    links.new(volume.outputs[0],nodes.get('Material Output').inputs['Volume'])
    if fall:
        # A single physical glass surface avoids the diffuse grey/waxy response
        # of overlapping layered Principled/scatter closures in the fall mass.
        glass=nodes.new('ShaderNodeBsdfGlass');glass.inputs['Color'].default_value=(.83,.96,.985,1);glass.inputs['Roughness'].default_value=.055;glass.inputs['IOR'].default_value=1.333
        links.new(bump.outputs['Normal'],glass.inputs['Normal']);links.new(glass.outputs[0],nodes.get('Material Output').inputs['Surface']);nodes.remove(shader)
        volume.inputs['Density'].default_value=.75
        mask_path=OUT/'textures/native-whitewater-flow-mask-v1.png'
        if mask_path.is_file():
            uv=nodes.new('ShaderNodeUVMap');uv.uv_map='NativeFlowUV';image=nodes.new('ShaderNodeTexImage');image.image=bpy.data.images.load(str(mask_path),check_existing=True);image.image.colorspace_settings.name='Non-Color';image.extension='REPEAT';links.new(uv.outputs['UV'],image.inputs['Vector'])
            gray=nodes.new('ShaderNodeRGBToBW');links.new(image.outputs['Color'],gray.inputs[0]);power=nodes.new('ShaderNodeMath');power.operation='POWER';power.inputs[1].default_value=2.5;links.new(gray.outputs[0],power.inputs[0]);gain=nodes.new('ShaderNodeMath');gain.operation='MULTIPLY';gain.inputs[1].default_value=.72;links.new(power.outputs[0],gain.inputs[0])
            foam=nodes.new('ShaderNodeBsdfDiffuse');foam.inputs['Color'].default_value=(.72,.86,.88,1);foam.inputs['Roughness'].default_value=.25;links.new(bump.outputs['Normal'],foam.inputs['Normal'])
            mix=nodes.new('ShaderNodeMixShader');links.new(gain.outputs[0],mix.inputs[0]);links.new(glass.outputs[0],mix.inputs[1]);links.new(foam.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],nodes.get('Material Output').inputs['Surface'])
    return mat


def _pond_mesh(collection,pond,material):
    outline=pond_outline(pond);n=len(outline);nr=22;cx,cy=pond['center'];level=float(pond['level']);depth=float(pond.get('depth',.6))
    v=[(cx,cy,level)]
    for ring in range(1,nr+1):
        q=ring/nr
        for x,y in outline:
            px,py=cx+(x-cx)*q,cy+(y-cy)*q
            wave=.005*(math.sin(px*2.1+py*1.7)+.4*math.sin(px*5.3-py*3.6))*math.sin(q*math.pi)
            v.append((px,py,level+wave))
    faces=[(0,1+i,1+(i+1)%n) for i in range(n)]
    for ring in range(nr-1):
        for i in range(n):
            a=1+ring*n+i;b=1+ring*n+(i+1)%n;c=b+n;d=a+n
            faces.extend(((a,d,c),(a,c,b)))
    top_count=len(v);bottom=[]
    for x,y,z in v:
        signed=float(pond_signed_distance(x,y,pond));bed=level-depth*float(_smooth(-signed/max(.5,min(pond['radii'])*.82)))
        bottom.append((x,y,min(level-.008,bed-.035)))
    v+=bottom;faces+=[tuple(top_count+i for i in reversed(f)) for f in list(faces)]
    edge=1+(nr-1)*n
    for i in range(n):
        a=edge+i;b=edge+(i+1)%n;faces.append((a,a+top_count,b+top_count,b))
    obj=_mesh(collection,'ExpandedWater_OrganicPond_'+str(pond.get('id','pond')),v,faces,material)
    obj['level_m']=level;obj['shallow_depth_m']=depth;obj['shore_geometry']='Shared native organic signed distance; positive winding; closed refractive water body'
    return obj


def _channel_mesh(collection,channel,material):
    path=channel_path(channel,10);half=float(channel.get('half_width',channel.get('width',1.)/2));depth=float(channel.get('depth',.5));n=17;v=[]
    for index,p in enumerate(path):
        tangent=path[min(index+1,len(path)-1),:2]-path[max(0,index-1),:2]
        tangent/=max(1e-8,np.linalg.norm(tangent));side=np.array((-tangent[1],tangent[0]))
        for q in np.linspace(-1,1,n):
            xy=p[:2]+side*half*q;v.append((xy[0],xy[1],p[2]+.003*math.sin(xy[0]*3+xy[1]*2)*math.sin(q*math.pi)))
    f=[]
    for row in range(len(path)-1):
        for col in range(n-1):
            a=row*n+col;f.extend(((a,a+1,a+n+1),(a,a+n+1,a+n)))
    # Ensure open water faces point skyward independently of path orientation.
    for i,face in enumerate(f):
        a,b,c=(np.asarray(v[k]) for k in face)
        if np.cross(b-a,c-a)[2]<0:f[i]=tuple(reversed(face))
    nv=len(v)
    for row,p in enumerate(path):
        for col,q in enumerate(np.linspace(-1,1,n)):
            x,y,z=v[row*n+col];v.append((x,y,z-max(.008,depth*(1-q*q)**1.35+.035)))
    f+=[tuple(nv+i for i in reversed(face)) for face in list(f)]
    edge=list(range(n))+[row*n+n-1 for row in range(1,len(path))]+list(range((len(path)-1)*n+n-2,(len(path)-1)*n-1,-1))+[row*n for row in range(len(path)-2,0,-1)]
    for i,a in enumerate(edge):b=edge[(i+1)%len(edge)];f.append((a,b,b+nv,a+nv))
    return _mesh(collection,'ExpandedWater_ReflectiveStream_'+str(channel.get('id','stream')),v,f,material)


def _wet_material():
    import bpy
    # Latest user rejects photo-quarry albedo and grain. This neutral material
    # is intentionally texture-free until the isolated reference-led helper is
    # reviewed; it cannot silently reload a historical photo texture.
    mat=bpy.data.materials.new('ExpandedWater_StylizedWetStone_NoPhoto');mat.use_nodes=True
    s=mat.node_tree.nodes.get('Principled BSDF')
    s.inputs['Base Color'].default_value=(.22,.29,.31,1)
    s.inputs['Roughness'].default_value=.55;s.inputs['Specular IOR Level'].default_value=.25
    s.inputs['Coat Weight'].default_value=.06;s.inputs['Coat Roughness'].default_value=.32
    mat['style_status']='Neutral clean planes; awaiting reference-led Argon helper review'
    mat['photo_albedo_forbidden']=True
    return mat


def _contact_rock(collection,name,point,direction,width,material,index):
    with np.load(ROOT/'Tools/quaternius_nature_cache.npz') as cache:
        key='Rock_'+str(1+index%5);v=cache[key+'_v'].astype(float).copy();f=cache[key+'_f']
    lo=v.min(axis=0);hi=v.max(axis=0);v=(v-(lo+hi)/2)/(hi-lo)
    v*=np.array((width*1.8+.4,width*1.3+.4,.72+.12*(index%3)))
    yaw=math.atan2(direction[1],direction[0]);ca,sa=math.cos(yaw),math.sin(yaw);xx=v[:,0]*ca-v[:,1]*sa;yy=v[:,0]*sa+v[:,1]*ca;v[:,0]=xx;v[:,1]=yy
    v+=np.asarray(point)+np.array((direction[0]*.10,direction[1]*.10,-.34))
    obj=_mesh(collection,name,v,f,material);obj['native_rock_cache']=key;obj['water_contact']=list(point)
    return obj


def _rejected_loft_core(collection,channel,material):
    """Three unequal thin refractive liquid cores, short linked drops, not a wall."""
    from mathutils import Vector
    full_path=channel_path(channel,16);width=float(channel.get('width',.9));objects=[]
    for branch,(flow,offset,phase) in enumerate(((.61,-.06,.2),(.25,.32,2.7),(.14,-.40,4.8))):
        start=0 if branch!=1 else channel.get('contact_indices',[1])[0]*16
        end=len(full_path) if branch!=2 else channel.get('contact_indices',[len(channel['points'])-1])[-1]*16+1
        path=full_path[start:end]
        if len(path)<2:continue
        v=[];faces=[];ns=10
        for row,p in enumerate(path):
            tangent=Vector(path[min(row+1,len(path)-1)]-path[max(0,row-1)]).normalized()
            side=tangent.cross(Vector((0,0,1)))
            if side.length<1e-5:side=Vector((1,0,0))
            side.normalize();depth_axis=tangent.cross(side).normalized();t=row/(len(path)-1)
            center=Vector(p)+side*(offset*width+.035*math.sin(t*19+phase))
            # Real millimetre/centimetre liquid thickness. Width is irregular,
            # gently varying; no opaque fullheight panel or tubular round rope.
            previous=[i*16-start for i in channel.get('contact_indices',[]) if i*16-start<=row and i*16-start>=0]
            source_height=path[max(previous) if previous else 0,2]
            speed=math.sqrt(1.5**2+19.62*max(0,source_height-p[2]))
            gravity_neck=math.sqrt(1.5/speed)
            radius=width*flow*.5*gravity_neck*(.83+.12*math.sin(t*23+phase)+.05*math.sin(t*57-phase))
            thick=.012+.010*flow
            for k in range(ns):
                a=math.tau*k/ns;shape=1+.08*math.sin(k*2.1+t*31+phase)
                v.append(tuple(center+side*(math.cos(a)*radius*shape)+depth_axis*(math.sin(a)*thick)))
            if row:
                for k in range(ns):
                    a=(row-1)*ns+k;b=(row-1)*ns+(k+1)%ns;faces.append((a,b,b+ns,a+ns))
        faces.extend((tuple(range(ns-1,-1,-1)),tuple(range((len(path)-1)*ns,len(path)*ns))))
        obj=_mesh(collection,f'ExpandedWater_TranslucentCore_{channel.get("id","chute")}_{branch}',v,faces,material)
        obj['liquid_core_thickness_m']=.024+.020*flow;objects.append(obj)
    return objects


def _flow_core(collection,channel,material):
    """Actual particle-derived native VDB liquid volume, warped into shared chute.

    The cached organic surface comes from real descending Newton positions and
    short velocity exposure. It is not a new fluid simulation. Each world build
    transforms this mesh only; no voxel bake or simulation is rerun.
    """
    import bpy
    source=OUT/'physical-volume-study/actual-descending-liquid-core.blend'
    if not source.is_file():raise RuntimeError('Native reconstructed liquid volume missing; refusing rejected loft fallback')
    with bpy.data.libraries.load(str(source),link=False) as (available,loaded):
        loaded.objects=[n for n in available.objects if n=='ActualDescendingLiquidVolume']
    if len(loaded.objects)!=1:raise RuntimeError('Actual native liquid volume object missing')
    template=loaded.objects[0];raw=np.empty(len(template.data.vertices)*3,dtype=np.float32);template.data.vertices.foreach_get('co',raw);local=raw.reshape(-1,3)
    u=np.clip((local[:,2].max()-local[:,2])/max(1e-6,np.ptp(local[:,2])),0,1);points=np.asarray(channel['points'],dtype=float);objects=[]
    for step,(a,b) in enumerate(zip(points[:-1],points[1:])):
        drop=a[2]-b[2]
        # Every physical drop gets its own short source volume. No15m
        # longitudinal stretching: fluid lobes remain small native liquid forms.
        center=a[None,:]*(1-u[:,None])+b[None,:]*u[:,None]
        center[:,2]=a[2]*(1-u)+b[2]*u+.12*drop*u*(1-u)
        derivative=np.tile(b-a,(len(u),1));derivative[:,2]=-drop+.12*drop*(1-2*u)
        tangent=derivative/np.linalg.norm(derivative,axis=1)[:,None]
        side=np.column_stack((tangent[:,1],-tangent[:,0],np.zeros(len(u))));side/=np.linalg.norm(side,axis=1)[:,None]
        depth_axis=np.cross(side,tangent)
        neck=.76-.14*u+.24*u**4
        arch=.04+.12*4*u*(1-u);width=float(channel.get('width',1.))
        branches=((.50,0.),(.27,-.55),(.12,.50)) if width>1. else ((.64,0.),(.24,.64))
        for branch,(branch_scale,branch_offset) in enumerate(branches):
            split=width*branch_offset*_smooth(u/.28)
            coords=center+side*(local[:,0]*width/.65*neck*branch_scale+split)[:,None]+depth_axis*(local[:,1]*.20*neck*branch_scale+arch)[:,None]
            mesh=template.data.copy();mesh.name=f'ExpandedPhysicalLiquid_{channel["id"]}_{step}_{branch}_Mesh';mesh.vertices.foreach_set('co',coords.astype(np.float32).reshape(-1));mesh.update();mesh.materials.clear();mesh.materials.append(material)
            uv=mesh.uv_layers.new(name='NativeFlowUV');loop_ids=np.empty(len(mesh.loops),dtype=np.int32);mesh.loops.foreach_get('vertex_index',loop_ids)
            uv_vertices=np.column_stack(((local[:,0]-local[:,0].min())/max(1e-6,np.ptp(local[:,0]))+branch*.27,(1-u)*max(.6,np.linalg.norm(b-a)/2)+step*.19))
            uv.data.foreach_set('uv',uv_vertices[loop_ids].astype(np.float32).reshape(-1))
            obj=bpy.data.objects.new(f'ExpandedWater_PhysicalVolumeCore_{channel["id"]}_{step}_{branch}',mesh);collection.objects.link(obj)
            for p in mesh.polygons:p.use_smooth=True
            obj['expanded_water']=True;obj['physical_core_source']=str(source);obj['construction']='Per-step native VDB actual particle mass; ballistic downward arc, gravity neck and anchored impact broadening; no opaque loft panels';objects.append(obj)
    bpy.data.objects.remove(template,do_unlink=True)
    return objects


def _pond_shore_rocks(collection,pond,material,offset):
    from world_expanded_spatial_layout import base_ground_z
    outline=pond_outline(pond);made=[];rng=np.random.default_rng(int(pond.get('seed',offset)))
    with np.load(ROOT/'Tools/quaternius_nature_cache.npz') as cache:
        for cluster,anchor in enumerate((18,83)):
            for member in range(2+cluster):
                index=(anchor+member*3)%len(outline);xy=outline[index].copy();radial=xy-np.asarray(pond['center']);radial/=np.linalg.norm(radial);xy+=radial*rng.uniform(.06,.22)
                z=float(carve_water_ground(*xy,base_ground_z(*xy),{'ponds':[pond]}));key='Rock_'+str(1+(offset+member+cluster)%5)
                v=cache[key+'_v'].astype(float).copy();f=cache[key+'_f'];lo=v.min(axis=0);hi=v.max(axis=0);v=(v-(lo+hi)/2)/(hi-lo)
                v*=np.array((rng.uniform(.32,.65),rng.uniform(.25,.42),rng.uniform(.14,.25)));v+=np.array((xy[0],xy[1],z-.045))
                obj=_mesh(collection,f'ExpandedWater_HalfBuriedShoreStone_{offset}_{cluster}_{member}',v,f,material);obj['bank_contact_z']=z;made.append(obj)
    return made


def _impact_foam(collection,name,point,material,seed,width):
    rng=np.random.default_rng(seed);v=[];f=[]
    for i in range(95):
        theta=rng.uniform(0,math.tau);radius=math.sqrt(rng.uniform(0,1))*width*.52
        airborne=i<18
        center=np.asarray(point)+np.array((math.cos(theta)*radius,math.sin(theta)*radius*.60,rng.uniform(.04,.18) if airborne else .008+rng.uniform(0,.025)))
        r=rng.uniform(.003,.010) if airborne else rng.uniform(.008,.030);base=len(v)
        for offset in ((1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,.55),(0,0,-.25)):v.append(tuple(center+np.asarray(offset)*r))
        for tri in ((0,2,4),(2,1,4),(1,3,4),(3,0,4),(2,0,5),(1,2,5),(3,1,5),(0,3,5)):f.append(tuple(base+k for k in tri))
    return _mesh(collection,name,v,f,material)


def build_world_expanded_water(collection,layout):
    """New native world only. Geometry placement is supplied by landscape layout."""
    if collection is None:raise ValueError('Explicit destination collection required')
    if not isinstance(layout,dict):raise ValueError('Pass shared expanded landscape layout dict')
    if not layout.get('ponds'):raise ValueError('Expanded layout needs explicit native pond centers/radii/levels')
    if len(layout['ponds'])!=1:raise ValueError('Latest user override: build exactly one central meadow pond')
    material=_liquid_material('ExpandedWater_DeepReflectiveLiquid')
    water=[_pond_mesh(collection,p,material) for p in layout['ponds']]
    water+=[_channel_mesh(collection,r,material) for r in layout.get('rivers',[])]
    rocks=[];fallmat=_liquid_material('ExpandedWater_ThinRefractiveCascadeCore',True);wetmat=_wet_material()
    for i,pond in enumerate(layout['ponds']):rocks+=_pond_shore_rocks(collection,pond,wetmat,i*11)
    import bpy
    foam=bpy.data.materials.new('ExpandedWater_LocalContactMicrofoam');foam.use_nodes=True;shader=foam.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(.72,.84,.81,1);shader.inputs['Roughness'].default_value=.40
    for cindex,cascade in enumerate(layout.get('cascades',[])):
        water+=_flow_core(collection,cascade,fallmat)
        points=np.asarray(cascade['points'],dtype=float)
        for n,index in enumerate(cascade.get('contact_indices',list(range(1,len(points))))):
            point=points[index];direction=points[index]-points[max(0,index-1)];direction[2]=0;direction/=max(1e-8,np.linalg.norm(direction))
            rocks.append(_contact_rock(collection,f'ExpandedWater_WetRockContact_{cindex}_{n}',point,direction,float(cascade.get('width',.9)),wetmat,cindex*3+n))
            water.append(_impact_foam(collection,f'ExpandedWater_AnchoredContactFoam_{cindex}_{n}',point,foam,7210+cindex*17+n,float(cascade.get('width',.9))))
    return {'objects':water+rocks,'water_objects':water,'rock_objects':rocks,'report':{'version':'expanded-native-water-v1','coordinate_contract':'native metres; landscape supplied expanded120x90 layout; no old sampler/scaling','ponds':layout['ponds'],'rivers':layout.get('rivers',[]),'cascades':layout.get('cascades',[]),'terrain_carve_api':'carve_water_ground(x,y,base_z,layout)','surface_height_api':'water_surface_height(x,y,layout)','closed_refractive_liquid_bodies':True,'opaque_panels':False,'visual_status':'REQUIRES_NATIVE_IMAGE_REVIEW'}}
