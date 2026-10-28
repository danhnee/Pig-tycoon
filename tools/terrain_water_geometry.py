"""Terrain and water candidates using the unchanged canonical world coordinates."""
import math
import numpy as np
from map_mesh import MapMesh
from map_surface_contract import (WORLD_X_MAX,WORLD_Y_MAX,strip_uv,sample_ground_z,
                                  eval_road_mask,eval_pond_distances,
                                  eval_river_center_and_width,island_signed_distance)

PONDS=((-3.4,-5.2,5.65,1.78,-31.,2.34),(-.5,.6,3.55,1.40,18.,2.56),(9.6,-7.4,3.35,1.38,-19.,2.14))


def patch_uv(strip,u,v,count):
    return strip_uv(strip,np.full(count,u),np.full(count,v))


def ellipsoid(mesh,name,center,radii,strip=0,u=.30,v=.55,seed=0,flat=False):
    center=np.asarray(center);radii=np.asarray(radii);segments=12;rings=6
    vertices=[center+np.array((0,0,radii[2]))]
    for j in range(1,rings):
        theta=math.pi*j/rings
        for i in range(segments):
            a=math.tau*i/segments
            wobble=1+.07*math.sin(a*3+seed)*math.sin(theta)
            vertices.append(center+radii*np.array((wobble*math.sin(theta)*math.cos(a),wobble*math.sin(theta)*math.sin(a),math.cos(theta))))
    bottom=len(vertices);vertices.append(center-np.array((0,0,radii[2])))
    faces=[]
    for i in range(segments):faces.append((0,1+i,1+(i+1)%segments))
    for j in range(rings-2):
        for i in range(segments):
            a=1+j*segments+i;b=1+j*segments+(i+1)%segments;c=b+segments;d=a+segments
            faces.extend(((a,d,c),(a,c,b)))
    for i in range(segments):faces.append((bottom,1+(rings-2)*segments+(i+1)%segments,1+(rings-2)*segments+i))
    vertices=np.asarray(vertices);faces=np.asarray(faces)
    # The cap and belt use one consistent outward convention.
    tri=vertices[faces];normal=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
    reverse=np.einsum('ij,ij->i',normal,tri.mean(axis=1)-center)<0
    faces[reverse]=faces[reverse][:,[0,2,1]]
    mesh.add(name,vertices,patch_uv(strip,u,v,len(vertices)),faces,flat=flat)


def ribbon(mesh,name,points,width,strip=2,u=.88,v=.35,thickness=.008):
    points=np.asarray(points);vertices=[]
    for i,p in enumerate(points):
        tangent=points[min(i+1,len(points)-1)]-points[max(0,i-1)]
        side=np.cross(tangent,(0,0,1))
        if np.linalg.norm(side)<1e-8:side=np.array((1.,0,0))
        side=side/np.linalg.norm(side)*width/2
        vertices.extend((p-side,p+side,p-side-(0,0,thickness),p+side-(0,0,thickness)))
    faces=[]
    for i in range(len(points)-1):
        a=i*4;b=a+4
        faces.extend(((a,b,b+1),(a,b+1,a+1),(a+2,a+3,b+3),(a+2,b+3,b+2),
                      (a,a+2,b+2),(a,b+2,b),(a+1,b+1,b+3),(a+1,b+3,a+3)))
    a=(len(points)-1)*4
    faces.extend(((0,1,3),(0,3,2),(a,a+2,a+3),(a,a+3,a+1)))
    vv=np.asarray(vertices);ff=np.asarray(faces)
    q=vv[ff]-vv.mean(axis=0)
    if np.einsum('ij,ij->i',q[:,0],np.cross(q[:,1],q[:,2])).sum()<0:
        faces=ff[:,[0,2,1]]
    mesh.add(name,vertices,patch_uv(strip,u,v,len(vertices)),faces)


def clipped_surface(mesh,name,vertices,uvs,faces,field):
    """Clip triangles at sampled terrain/shore intersections; no center pole duplicates."""
    result_v=[];result_uv=[];result_f=[]
    for ids in faces:
        polygon=[(vertices[i],uvs[i],field[i]) for i in ids]
        clipped=[]
        for i,current in enumerate(polygon):
            previous=polygon[i-1]
            inside=current[2]>=0;previous_inside=previous[2]>=0
            if inside!=previous_inside:
                t=previous[2]/(previous[2]-current[2])
                clipped.append((previous[0]+t*(current[0]-previous[0]),previous[1]+t*(current[1]-previous[1])))
            if inside:clipped.append((current[0],current[1]))
        for i in range(1,len(clipped)-1):
            points=[clipped[0],clipped[i],clipped[i+1]]
            if np.linalg.norm(np.cross(points[1][0]-points[0][0],points[2][0]-points[0][0]))<=1e-8:continue
            base=len(result_v);result_v.extend(p[0] for p in points);result_uv.extend(p[1] for p in points);result_f.append((base,base+1,base+2))
    mesh.add(name,result_v,result_uv,result_f)


def build_terrain():
    mesh=MapMesh();nx,ny=240,180
    gx,gy=np.meshgrid(np.linspace(-WORLD_X_MAX,WORLD_X_MAX,nx,dtype=np.float32),np.linspace(-WORLD_Y_MAX,WORLD_Y_MAX,ny,dtype=np.float32))
    gz=sample_ground_z(gx,gy);distance=island_signed_distance(gx,gy)
    vertices=np.stack((gx,gy,gz),axis=-1).reshape(-1,3)
    uvs=np.stack((gx/(2*WORLD_X_MAX)+.5,.255+.740*(gy/(2*WORLD_Y_MAX)+.5)),axis=-1).reshape(-1,2)
    faces=[]
    for j in range(ny-1):
        for i in range(nx-1):
            if np.max(distance[j:j+2,i:i+2])>.962:continue
            if gy[j+1,i]>15.6 or (gy[j+1,i]>14.8 and gx[j,i]<10.5):continue
            a=j*nx+i;faces.extend(((a,a+1,a+nx+1),(a,a+nx+1,a+nx)))
    mesh.add('canonical_heightfield',vertices,uvs,faces)
    # Small embedded gravel, concentrated beside worn paths instead of uniform scatter.
    rng=np.random.default_rng(260118)
    keepouts=((-17.8,5.4,2.1),(-17,9.2,2.1),(-13.4,9.4,2.0),(-7.8,6.4,2.3),(1.,8.5,3.7),(8.6,4.6,2.0))
    for i in range(2800):
        x,y=rng.uniform((-25,-17),(20,12))
        road=float(eval_road_mask(x,y))
        if not .20<road<.65 or float(island_signed_distance(x,y))>.90:continue
        if any(math.hypot(x-a,y-b)<r for a,b,r in keepouts):continue
        if min(float(d) for d in eval_pond_distances(x,y))<1.2:continue
        rc,rw=eval_river_center_and_width(y)
        if -8.4<y<12.5 and abs(x-float(rc))<float(rw)*1.18:continue
        z=float(sample_ground_z(x,y));size=float(rng.uniform(.035,.10))
        ellipsoid(mesh,'path_gravel',(x,y,z+.012),(size,size*.65,.025),strip=0,u=float(rng.uniform(.25,.47)),v=.58,seed=i,flat=True)
    return mesh


def add_pond(mesh,index,spec):
    cx,cy,rx,ry,angle,level=spec
    ca,sa=math.cos(math.radians(angle)),math.sin(math.radians(angle))
    rings,segments=15,96
    vertices=[(cx,cy,level)];local=[(0.,0.)]
    for ring in range(1,rings+1):
        rf=1.18*ring/rings
        for i in range(segments):
            t=math.tau*i/segments
            lx,ly=rx*rf*math.cos(t),ry*rf*math.sin(t)
            x,y=cx+lx*ca-ly*sa,cy+lx*sa+ly*ca
            z=level+.007*math.sin(x*4+y*3)*(1-min(1,rf))
            vertices.append((x,y,z));local.append((rf*math.cos(t),rf*math.sin(t)))
    faces=[(0,1+i,1+(i+1)%segments) for i in range(segments)]
    for ring in range(rings-1):
        for i in range(segments):
            a=1+ring*segments+i;b=1+ring*segments+(i+1)%segments;c=b+segments;d=a+segments
            faces.extend(((a,b,c),(a,c,d)))
    vertices=np.asarray(vertices,dtype=np.float32);local=np.asarray(local)
    uvs=strip_uv(2,.61+.13*local[:,0],.50+.40*local[:,1])
    field=level-sample_ground_z(vertices[:,0],vertices[:,1])
    clipped_surface(mesh,f'pond_{index}_surface',vertices,uvs,faces,field)
    rng=np.random.default_rng(2000+index)
    # Solve the true water/terrain crossing along each radial bank section.
    for i in range(76):
        theta=math.tau*(i+rng.uniform(-.2,.2))/76
        lo,hi=.80,1.25
        for _ in range(14):
            r=(lo+hi)/2;lx,ly=rx*r*math.cos(theta),ry*r*math.sin(theta)
            x,y=cx+lx*ca-ly*sa,cy+lx*sa+ly*ca
            if float(sample_ground_z(x,y))<level:lo=r
            else:hi=r
        radial=hi+float(rng.uniform(-.012,.03))
        lx,ly=rx*radial*math.cos(theta),ry*radial*math.sin(theta)
        x,y=cx+lx*ca-ly*sa,cy+lx*sa+ly*ca;ground=float(sample_ground_z(x,y))
        size=float(rng.uniform(.10,.24));height=float(rng.uniform(.08,.18))
        ellipsoid(mesh,f'pond_{index}_shore_rock',(x,y,ground+height*.35),(size,size*.75,height),u=float(rng.uniform(.24,.46)),v=float(rng.uniform(.38,.7)),seed=i,flat=True)
        if i%7==0:
            for stem in range(4):
                sx,sy=x+rng.uniform(-.1,.1),y+rng.uniform(-.1,.1);sz=float(sample_ground_z(sx,sy))
                h=float(rng.uniform(.22,.43));lean=float(rng.uniform(-.08,.08))
                ribbon(mesh,'reed_stem',[(sx,sy,sz),(sx+lean,sy+.025,sz+h)],.014,strip=3,u=.78,v=.6,thickness=.006)
                for side in [-1,1]:
                    ribbon(mesh,'reed_leaf',[(sx,sy,sz+.08),(sx+side*.06,sy+.03,sz+h*.7),(sx+side*.12,sy+.05,sz+h*.8)],.035,strip=3,u=.79,v=.7,thickness=.004)
        if i%3==0:
            points=[]
            for t in np.linspace(theta-.026,theta+.026,6):
                lx,ly=rx*lo*.985*math.cos(t),ry*lo*.985*math.sin(t)
                points.append((cx+lx*ca-ly*sa,cy+lx*sa+ly*ca,level+.012))
            ribbon(mesh,'pond_shore_foam',points,.018)


def add_river(mesh):
    nx,ny=60,170
    yy,uu=np.meshgrid(np.linspace(-7.4,12.2,ny),np.linspace(0,1,nx),indexing='ij')
    center,width=eval_river_center_and_width(yy)
    xx=center+(uu*2-1)*width*1.14
    zz=2.00+.08*(yy+7.4)/19.6+.010*np.sin(xx*3.5+yy*5.5)*np.sin(uu*math.pi)
    vertices=np.stack((xx,yy,zz),axis=-1).reshape(-1,3).astype(np.float32)
    uvs=strip_uv(2,.02+.41*uu,.05+.90*(yy+7.4)/19.6).reshape(-1,2)
    faces=[]
    for j in range(ny-1):
        for i in range(nx-1):
            a=j*nx+i;faces.extend(((a,a+1,a+nx+1),(a,a+nx+1,a+nx)))
    field=np.minimum(vertices[:,2]-sample_ground_z(vertices[:,0],vertices[:,1]),(.958-island_signed_distance(vertices[:,0],vertices[:,1]))*20)
    clipped_surface(mesh,'river_surface',vertices,uvs,faces,field)
    rng=np.random.default_rng(9003)
    for i in range(105):
        y=float(rng.uniform(-6.5,11));cx,hw=eval_river_center_and_width(y)
        x=float(cx)+float(rng.uniform(-.8,.8))*float(hw)
        if float(island_signed_distance(x,y))>.93 or float(sample_ground_z(x,y))>1.8:continue
        z=2+.08*(y+7.4)/19.6+.025;length=float(rng.uniform(.12,.5))
        points=[(x+length*t,y+.015*math.sin(t*math.pi),z) for t in np.linspace(-1,1,5)]
        ribbon(mesh,'river_flow_glint',points,.010,strip=2,u=.32,v=.7)


def add_waterfalls(mesh):
    for index,(x,width) in enumerate(zip((8.2,11.4,14.8,17.8),(1.55,1.85,1.95,1.45))):
        top_y=12.6-.15*abs(index-1.5);bottom_y=11.2-.15*abs(index-1.5)
        for band in range(9):
            vertices=[];uvs=[];n=30;band_width=width/9*1.15
            offset=(band-4)*width/9
            for back in [0,.035]:
                for row in range(n+1):
                    t=row/n
                    y=bottom_y+(top_y-bottom_y)*(t**.75)+back
                    z=2.02+(7.38-2.02)*t+.22*math.sin(t*math.pi)
                    wobble=.028*math.sin(t*19+band*1.8+index)
                    for side in [-1,1]:
                        vertices.append((x+offset+wobble+side*band_width*.5*(1.22-.22*t),y,z))
                        uvs.append((.81+.16*(band/8),.06+.88*t))
            layer=(n+1)*2;faces=[]
            for row in range(n):
                a=row*2;b=a+2
                faces.extend(((a,a+1,b+1),(a,b+1,b),(a+layer,b+layer,b+1+layer),(a+layer,b+1+layer,a+1+layer),
                              (a,b,b+layer),(a,b+layer,a+layer),(a+1,a+1+layer,b+1+layer),(a+1,b+1+layer,b+1)))
            a=n*2
            faces.extend(((0,layer,layer+1),(0,layer+1,1),(a,a+1,a+1+layer),(a,a+1+layer,a+layer)))
            mesh.add('waterfall_stream',vertices,strip_uv(2,np.array(uvs)[:,0],np.array(uvs)[:,1]),faces)
        rng=np.random.default_rng(800+index)
        for i in range(25):
            dx=float(rng.uniform(-width*.8,width*.8));dy=float(rng.uniform(-.75,.12));r=float(rng.uniform(.08,.23))
            ellipsoid(mesh,'waterfall_foam',(x+dx,bottom_y+dy,2.07+r*.15),(r*1.5,r,r*.5),strip=2,u=.9,v=float(rng.uniform(.12,.45)),seed=i)
        for arc in range(5):
            r=.35+arc*.21
            points=[(x+r*1.3*math.cos(t),bottom_y-.22+r*.6*math.sin(t),2.08+.002*arc) for t in np.linspace(math.pi,math.tau,28)]
            ribbon(mesh,'waterfall_impact_arc',points,.022,strip=2,u=.86,v=.4)


def build_water():
    mesh=MapMesh();add_river(mesh)
    for i,spec in enumerate(PONDS):add_pond(mesh,i,spec)
    add_waterfalls(mesh)
    return mesh
