"""Research observatory exterior, in the existing map atlas and Z-up coordinates.

The model is assembled from closed solid components. No reference-image planes,
camera projection, external asset downloads or random global state are involved.
"""
import math
import struct
import numpy as np

# (atlas strip, local u, local v): small patches avoid painted mullions/runes.
PALETTE = {
    'stone': (4, .57, .16), 'mortar': (4, .32, .10),
    'trim': (4, .76, .22), 'plaster': (1, .20, .78),
    'wood': (1, .83, .60), 'door': (0, .32, .48),
    'roof': (5, .51, .58), 'dome': (5, .57, .42),
    'brass': (0, .45, .85), 'glass': (1, .48, .70),
    'iron': (5, .72, .12), 'lens': (5, .48, .20),
}


def meshbin_bytes(vertices, uvs, faces):
    """Existing Unity layout with explicit flat normals and Y-up winding."""
    tri=vertices[faces]
    normals=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
    lengths=np.linalg.norm(normals,axis=1)
    if np.any(lengths<=1e-9): raise ValueError('Cannot export degenerate triangles')
    if not np.array_equal(faces.reshape(-1),np.arange(len(vertices))):
        raise ValueError('Flat-normal export expects split triangle vertices')
    positions=vertices[:,[0,2,1]].astype('<f4')
    normals=np.repeat(normals/lengths[:,None],3,axis=0)[:,[0,2,1]].astype('<f4')
    indices=faces[:,[0,2,1]].astype('<i4')
    return struct.pack('<ii',len(vertices),faces.size)+positions.tobytes()+normals.tobytes()+uvs.astype('<f4').tobytes()+indices.tobytes()


def unit(v):
    v = np.asarray(v, dtype=float)
    length = np.linalg.norm(v)
    if length < 1e-10:
        raise ValueError('A geometry direction cannot be zero')
    return v / length


def frame(axis):
    z = unit(axis)
    helper = (0, 0, 1) if abs(z[2]) < .9 else (0, 1, 0)
    x = unit(np.cross(helper, z))
    return np.column_stack((x, np.cross(z, x), z))


class Geometry:
    def __init__(self):
        self.vertices, self.uvs, self.faces, self.parts = [], [], [], []
        self.rng = np.random.default_rng(180526)

    def add(self, name, vertices, polygons, material, convex_center=None):
        vertices = np.asarray(vertices, dtype=float)
        triangles = []
        for poly in polygons:
            for i in range(1, len(poly) - 1):
                ids = [poly[0], poly[i], poly[i + 1]]
                tri = vertices[ids]
                normal = np.cross(tri[1] - tri[0], tri[2] - tri[0])
                if np.linalg.norm(normal) < 1e-10:
                    raise ValueError(f'Degenerate triangle in {name}')
                if convex_center is not None and np.dot(normal, tri.mean(axis=0) - convex_center) < 0:
                    ids[1], ids[2] = ids[2], ids[1]
                triangles.append(ids)
        start_face = len(self.faces)
        # Split at triangle boundaries for identical flat normals in Blender/Unity.
        strip, u, v = PALETTE[material]
        v = np.clip(v + self.rng.uniform(-.07, .07), .07, .9)
        patch = np.array([[u-.008, v-.012], [u+.008, v-.012], [u, v+.012]])
        patch[:, 0] = (strip + .03 + .94 * patch[:, 0]) / 8
        patch[:, 1] = .006 + .238 * patch[:, 1]
        for ids in triangles:
            start = len(self.vertices)
            self.vertices.extend(vertices[ids])
            self.uvs.extend(patch)
            self.faces.append((start, start+1, start+2))
        self.parts.append({'name': name, 'material': material,
                           'first_triangle': start_face, 'triangles': len(triangles)})

    def box(self, name, center, size, material, bevel=.012, rotation=None, top_bevel_only=False):
        half = np.asarray(size, dtype=float) / 2
        b = min(bevel, float(min(half)) * .35)
        hx, hy, hz = half
        def octagon(x, y, c):
            return [(-x+c,-y), (x-c,-y), (x,-y+c), (x,y-c),
                    (x-c,y), (-x+c,y), (-x,y-c), (-x,-y+c)]
        verts = []
        if top_bevel_only:
            for inset,z in [(0,-hz),(0,hz-b),(b,hz)]:
                verts.extend((x,y,z) for x,y in [(-hx+inset,-hy+inset),(hx-inset,-hy+inset),(hx-inset,hy-inset),(-hx+inset,hy-inset)])
            sides,rings=4,3
        else:
            for inset, z in [(b,-hz), (0,-hz+b), (0,hz-b), (b,hz)]:
                verts.extend((x,y,z) for x,y in octagon(hx-inset,hy-inset,b*.5))
            sides,rings=8,4
        verts = np.asarray(verts)
        if rotation is not None:
            verts = verts @ np.asarray(rotation).T
        verts += center
        faces = [list(range(sides-1,-1,-1)), list(range((rings-1)*sides,rings*sides))]
        for ring in range(rings-1):
            for i in range(sides):
                j=(i+1)%sides; a=ring*sides
                faces.append([a+i,a+j,a+sides+j,a+sides+i])
        self.add(name,verts,faces,material,np.asarray(center))

    def beam(self, name, a, b, width, material='wood', depth=None):
        a,b=np.asarray(a),np.asarray(b)
        self.box(name,(a+b)/2,(width,depth or width,np.linalg.norm(b-a)),material,
                 bevel=min(.008,width*.15),rotation=frame(b-a))

    def cylinder(self, name, a, b, radius, material, end_radius=None, segments=24):
        a,b=np.asarray(a),np.asarray(b)
        basis=frame(b-a)
        verts=[]
        for center,r in [(a,radius),(b,radius if end_radius is None else end_radius)]:
            for i in range(segments):
                t=2*math.pi*i/segments
                verts.append(center+basis[:,0]*r*math.cos(t)+basis[:,1]*r*math.sin(t))
        faces=[list(range(segments-1,-1,-1)),list(range(segments,segments*2))]
        faces += [[i,(i+1)%segments,(i+1)%segments+segments,i+segments] for i in range(segments)]
        self.add(name,verts,faces,material,(a+b)/2)

    def tube(self, name, points, radius, material, segments=6, closed=False):
        points=np.asarray(points); verts=[]
        for i,p in enumerate(points):
            tangent=points[(i+1)%len(points)]-points[(i-1)%len(points)] if closed else points[min(i+1,len(points)-1)]-points[max(0,i-1)]
            basis=frame(tangent)
            verts.extend(p+radius*(math.cos(j*2*math.pi/segments)*basis[:,0]+math.sin(j*2*math.pi/segments)*basis[:,1]) for j in range(segments))
        faces=[]
        for i in range(len(points) if closed else len(points)-1):
            for j in range(segments):
                a=i*segments+j; b=i*segments+(j+1)%segments
                c=((i+1)%len(points))*segments+(j+1)%segments
                d=((i+1)%len(points))*segments+j
                faces.append([a,b,c,d])
        if not closed:
            faces.extend([list(range(segments-1,-1,-1)),list(range((len(points)-1)*segments,len(points)*segments))])
        self.add(name,verts,faces,material)

    def arrays(self, origin=(0,0,0), yaw=0):
        vertices=np.asarray(self.vertices,dtype=np.float32)
        c,s=math.cos(yaw),math.sin(yaw)
        vertices=vertices @ np.array([[c,-s,0],[s,c,0],[0,0,1]],dtype=np.float32).T
        vertices+=np.asarray(origin,dtype=np.float32)
        return vertices,np.asarray(self.uvs,dtype=np.float32),np.asarray(self.faces,dtype=np.int32)


def prism(g,name,outline,depth,origin,material,yaw=0):
    """Convex outline in local XZ, facade looking toward negative local Y."""
    c,s=math.cos(yaw),math.sin(yaw)
    verts=[]
    for y in [-depth/2,depth/2]:
        for x,z in outline:
            verts.append((origin[0]+c*x-s*y,origin[1]+s*x+c*y,origin[2]+z))
    n=len(outline)
    faces=[list(range(n)),list(range(n,2*n))]
    faces += [[i,(i+1)%n,(i+1)%n+n,i+n] for i in range(n)]
    g.add(name,verts,faces,material,np.mean(verts,axis=0))


def arch_outline(radius,spring):
    return [(-radius,0),(radius,0)]+[(radius*math.cos(t),spring+radius*math.sin(t)) for t in np.linspace(0,math.pi,13)]


def arch_window(g,name,origin,width=.38,height=.66,yaw=0,door=False):
    r=width/2; spring=height-r
    c,s=math.cos(yaw),math.sin(yaw)
    def pt(x,y,z):
        return (origin[0]+c*x-s*y,origin[1]+s*x+c*y,origin[2]+z)
    prism(g,name+'_recess',arch_outline(r+.035,spring),.045,pt(0,0,0),'iron',yaw)
    prism(g,name+'_inset',arch_outline(r-.018,spring),.025,pt(0,-.029,.015),'door' if door else 'glass',yaw)
    for side in [-1,1]:
        for row in range(max(2,int(spring/.15))):
            count=max(2,int(spring/.15)); z=(row+.5)*spring/count
            g.box(name+'_jamb',pt(side*(r+.045),-.008,z),(.10,.10,spring/count-.008),'trim',.01,
                  np.array([[c,-s,0],[s,c,0],[0,0,1]]))
    for i in range(11):
        lo=i*math.pi/11+.018; hi=(i+1)*math.pi/11-.018
        outline=[((r+.10)*math.cos(lo),spring+(r+.10)*math.sin(lo)),
                 ((r+.10)*math.cos(hi),spring+(r+.10)*math.sin(hi)),
                 (r*math.cos(hi),spring+r*math.sin(hi)),
                 (r*math.cos(lo),spring+r*math.sin(lo))]
        prism(g,name+'_voussoir',outline,.12,origin,'trim',yaw)
    g.beam(name+'_sill',pt(-r-.10,-.025,-.025),pt(r+.10,-.025,-.025),.095,'trim')
    if door:
        for i in range(7):
            x=-r+.045+i*(2*r-.09)/6
            top=spring+math.sqrt(max(0,(r-.025)**2-x*x))-.02
            g.beam(name+'_plank_joint',pt(x,-.048,.04),pt(x,-.048,top),.012,'wood')
        for z in [.22,.57]:
            for side in [-1,1]:
                g.beam(name+'_hinge',pt(side*.04,-.068,z),pt(side*(r-.03),-.068,z),.025,'brass')
        for side in [-1,1]:
            center=np.array(pt(side*.075,-.085,.46))
            points=[center+np.array([c*.037*math.cos(t),s*.037*math.cos(t),.037*math.sin(t)]) for t in np.linspace(0,2*math.pi,20,endpoint=False)]
            g.tube(name+'_door_ring',points,.009,'brass',closed=True)
    else:
        g.beam(name+'_mullion',pt(0,-.05,.03),pt(0,-.05,height-.03),.022)
        for z in [spring*.34,spring*.72]:
            g.beam(name+'_transom',pt(-r+.025,-.05,z),pt(r-.025,-.05,z),.022)


def masonry_block(g,name,cx,cy,width,depth,eave=1.5):
    g.box(name+'_mortar',(cx,cy,.65),(width,depth,1.6),'mortar',.015)
    g.box(name+'_plaster',(cx,cy,1.20),(width+.005,depth+.005,.60),'plaster',.014)
    # Individually beveled courses: deliberate joints and real relief at every facade.
    for row in range(6):
        z=-.09+(row+.5)*.18
        for horizontal,length,offset in [(True,width,depth/2),(False,depth,width/2)]:
            count=math.ceil(length/.32)
            edges=np.linspace(-length/2,length/2,count+1)
            if row%2: edges=np.unique(np.r_[-length/2,edges[:-1]+length/count/2,length/2])
            for left,right in zip(edges[:-1],edges[1:]):
                for side in [-1,1]:
                    center=(cx+(left+right)/2,cy+side*offset,z) if horizontal else (cx+side*offset,cy+(left+right)/2,z)
                    size=(right-left-.012,.065,.168) if horizontal else (.065,right-left-.012,.168)
                    g.box(name+'_stone_course',center,size,'stone',.012)
    for x in [-width/2,width/2]:
        for y in [-depth/2,depth/2]:
            g.box(name+'_corner_timber',(cx+x,cy+y,1.24),(.10,.10,.56),'wood')
            for row in range(4):
                g.box(name+'_quoin',(cx+x,cy+y,.03+row*.25),(.17,.17,.235),'trim')
    for z in [1.02,eave-.035]:
        g.box(name+'_belt',(cx,cy,z),(width+.12,depth+.12,.095),'wood')
    g.box(name+'_plinth',(cx,cy,-.08),(width+.13,depth+.13,.20),'stone',.025)


def tiled_roof(g,name,cx,cy,width,depth,eave,rise,ridge_x=True,occluders=()):
    def p(u,v,z): return np.array((cx+u,cy+v,z) if ridge_x else (cx+v,cy+u,z))
    length=width if ridge_x else depth
    run=(depth if ridge_x else width)/2
    # Closed roof volume, with gable faces made separately below.
    verts=[p(u,v,z) for u in [-length/2,length/2] for v,z in [(-run,eave),(0,eave+rise),(run,eave)]]
    g.add(name+'_underlay',verts,[[0,1,2],[5,4,3],[0,3,4,1],[1,4,5,2],[2,5,3,0]],'roof',np.mean(verts,axis=0))
    columns=math.ceil(length/.19); rows=max(7,math.ceil(math.hypot(run,rise)/.145))
    for side in [-1,1]:
        for row in range(rows):
            # Start at eaves; the next row overlaps the head of the previous one.
            t=1-(row+.5)/rows
            z=eave+rise*(1-t)+.035*t*t
            slope=unit(p(0,side*run, -rise+.07*t)-p(0,0,0))
            along=unit(p(1,0,0)-p(0,0,0))
            normal=unit(np.cross(along,slope))
            if normal[2]<0: normal=-normal
            rotation=np.column_stack((along,slope,normal))
            edges=np.linspace(-length/2,length/2,columns+1)
            if row%2: edges=np.unique(np.r_[-length/2,edges[:-1]+length/columns/2,length/2])
            for left,right in zip(edges[:-1],edges[1:]):
                center=p((left+right)/2,side*run*t,z)+normal*.023
                if any(test(center) for test in occluders): continue
                g.box(name+'_slate_tile',center,(right-left-.007,math.hypot(run,rise)/rows*1.18,.038),'roof',.006,rotation,top_bevel_only=True)
        for u in [-length/2,length/2]:
            g.beam(name+'_bargeboard',p(u,0,eave+rise+.025),p(u,side*run,eave+.025),.08,'wood')
        g.beam(name+'_fascia',p(-length/2,side*run,eave),p(length/2,side*run,eave),.085,'wood')
    g.beam(name+'_ridge',p(-length/2-.025,0,eave+rise+.035),p(length/2+.025,0,eave+rise+.035),.09,'wood')
    # Plaster/timber gables close both ends, so a rear orbit retains detail.
    for side in [-1,1]:
        u=side*(length/2+.035)
        points=[p(u,-run*.87,eave-.03),p(u,run*.87,eave-.03),p(u,0,eave+rise-.07)]
        axis=unit(p(1,0,0)-p(0,0,0))*.025
        vv=[q+axis for q in points]+[q-axis for q in points]
        g.add(name+'_gable',vv,[[0,1,2],[5,4,3],[0,3,4,1],[1,4,5,2],[2,5,3,0]],'plaster',np.mean(vv,axis=0))
        for v in [-run*.83,run*.83]:
            g.beam(name+'_gable_brace',p(u+side*.045,v,eave+.02),p(u+side*.045,0,eave+rise*.70),.07)
        g.beam(name+'_gable_kingpost',p(u,0,eave),p(u,0,eave+rise-.04),.07)


def observatory_dome(g,cx,cy,base):
    r=.53
    drum_height=1.20
    g.cylinder('drum_core',(cx,cy,base-drum_height),(cx,cy,base),r,'mortar',segments=48)
    for row in range(10):
        for i in range(16):
            lo=(i+row*.5)*math.tau/16+.008; hi=lo+math.tau/16-.016
            z0=base-drum_height+row*.12; z1=z0+.109
            vv=[(cx+rr*math.cos(a),cy+rr*math.sin(a),z) for z in [z0,z1] for rr,a in [(r-.02,lo),(r+.025,lo),(r+.025,hi),(r-.02,hi)]]
            g.add('drum_masonry',vv,[[3,2,1,0],[4,5,6,7],[0,1,5,4],[1,2,6,5],[2,3,7,6],[3,0,4,7]],'stone',np.mean(vv,axis=0))
    for z in [base-drum_height,base-.07,base]:
        g.cylinder('drum_cornice',(cx,cy,z),(cx,cy,z+.045),r+.06,'trim',segments=48)
    arch_window(g,'drum_front_window',(cx,cy-r-.065,base-.47),.23,.36)
    # Open south-facing slit. Each shell panel is a closed curved solid.
    for sector in range(14):
        a0=-math.pi/2+.32+sector*(math.tau-.64)/14
        a1=-math.pi/2+.32+(sector+1)*(math.tau-.64)/14
        az=np.linspace(a0,a1,5); polar=np.linspace(.04,math.pi/2,10)
        vv=[]
        for rr in [r+.045,r+.025]:
            for t in polar:
                for a in az: vv.append((cx+rr*math.sin(t)*math.cos(a),cy+rr*math.sin(t)*math.sin(a),base+rr*math.cos(t)))
        n=len(az); layer=n*len(polar); ff=[]
        for j in range(len(polar)-1):
            for i in range(n-1):
                k=j*n+i
                ff.extend([[k,k+n,k+n+1,k+1],[layer+k+1,layer+k+n+1,layer+k+n,layer+k]])
        perimeter=list(range(n))+[j*n+n-1 for j in range(1,len(polar))]+list(range(layer-2,layer-n-1,-1))+[j*n for j in range(len(polar)-2,0,-1)]
        for i,a in enumerate(perimeter):
            b=perimeter[(i+1)%len(perimeter)]; ff.append([a,b,b+layer,a+layer])
        g.add('dome_shell',vv,ff,'dome')
        points=[(cx+(r+.057)*math.sin(t)*math.cos(a0),cy+(r+.057)*math.sin(t)*math.sin(a0),base+(r+.057)*math.cos(t)) for t in polar]
        g.tube('dome_rib',points,.009,'brass')
    g.cylinder('dome_pole',(cx,cy,base+r+.02),(cx,cy,base+r+.055),.045,'brass')
    # Outward-open curved shutter at the bottom of the observing slit.
    angles=np.linspace(-math.pi/2-.30,-math.pi/2+.30,9)
    radii=np.linspace(r-.02,r+.32,5)
    shutter=[]
    for down in [0,.025]:
        for rr in radii:
            for a in angles:
                shutter.append((cx+rr*math.cos(a),cy+rr*math.sin(a),base+.04+.12*((rr-r)/.32)**2-down))
    n=len(angles);layer=n*len(radii);faces=[]
    for j in range(len(radii)-1):
        for i in range(n-1):
            k=j*n+i
            faces.extend([[k,k+n,k+n+1,k+1],[layer+k+1,layer+k+n+1,layer+k+n,layer+k]])
    perimeter=list(range(n))+[j*n+n-1 for j in range(1,len(radii))]+list(range(layer-2,layer-n-1,-1))+[j*n for j in range(len(radii)-2,0,-1)]
    for i,a in enumerate(perimeter):
        b=perimeter[(i+1)%len(perimeter)];faces.append([a,b,b+layer,a+layer])
    g.add('dome_open_shutter',shutter,faces,'dome')
    g.tube('shutter_lip',[(cx+(r+.32)*math.cos(a),cy+(r+.32)*math.sin(a),base+.16) for a in angles],.012,'brass')
    for theta in np.linspace(0,math.tau,40,endpoint=False):
        p=np.array((cx+(r+.058)*math.cos(theta),cy+(r+.058)*math.sin(theta),base+.03))
        g.cylinder('dome_rivet',p,p+np.array((.012*math.cos(theta),.012*math.sin(theta),0)),.009,'brass',segments=8)
    a=np.array((cx,cy-.06,base+.20)); direction=unit((.28,-1,.68))
    for lo,hi,rr,mat in [(0,.35,.105,'brass'),(.35,.80,.12,'brass'),(.80,1.03,.14,'brass')]:
        g.cylinder('telescope_barrel',a+direction*lo,a+direction*hi,rr,mat,segments=32)
    for t,rr in [(0,.13),(.34,.14),(.78,.15),(1.02,.165)]:
        g.cylinder('telescope_collar',a+direction*t,a+direction*(t+.04),rr,'brass',segments=32)
    g.cylinder('telescope_lens_recess',a+direction*1.056,a+direction*1.060,.137,'iron',segments=32)
    g.cylinder('telescope_optic',a+direction*1.061,a+direction*1.063,.118,'lens',segments=32)
    g.cylinder('telescope_mount',(cx,cy,base-.08),a,.085,'brass')


def build_observatory():
    g=Geometry()
    masonry_block(g,'main',0,.30,3.0,1.80)
    masonry_block(g,'entry',-.57,-.63,1.28,1.42)
    masonry_block(g,'east',1.01,-.30,1.00,1.40)
    # Roof intersections are hidden beneath the taller entrance wing/drum.
    def under_entry(p):
        return abs(p[0]+.57)<.76 and p[1]<.19 and p[2]<1.55+1.08*(1-abs(p[0]+.57)/.76)+.025
    def under_drum(p):
        return math.hypot(p[0],p[1]-.43)<.56 and p[2]<2.75
    tiled_roof(g,'main_roof',0,.30,3.24,2.08,1.51,1.12,True,(under_entry,under_drum))
    tiled_roof(g,'entry_roof',-.57,-.65,1.53,1.65,1.53,1.10,False,(under_drum,))
    tiled_roof(g,'east_roof',1.01,-.25,1.22,1.63,1.52,.86,False)
    g.box('east_dormer_wall',(1.39,-.21,1.99),(.53,.42,.43),'plaster')
    tiled_roof(g,'east_dormer',1.39,-.21,.64,.56,2.16,.30,True)
    arch_window(g,'east_dormer_window',(1.77,-.21,1.86),.21,.35,math.pi/2)
    arch_window(g,'entrance',(-.57,-1.375,.04),.61,.98,door=True)
    arch_window(g,'front_gable',(-.57,-1.535,1.61),.32,.53)
    arch_window(g,'east_front',(1.01,-1.04,.28),.32,.65)
    for y in [-.65,.15,.85]: arch_window(g,'east_window',(1.545,y,.28),.32,.65,math.pi/2)
    for y in [-.20,.70]: arch_window(g,'west_window',(-1.545,y,.28),.34,.66,-math.pi/2)
    for x in [-1.05,-.25,.65,1.17]: arch_window(g,'rear_window',(x,1.24,.28),.31,.65,math.pi)
    for step in range(3):
        g.box('entrance_stair',(-.57,-1.49-step*.105,-.13-step*.04),(.88+step*.08,.30,.12),'trim',.016)
    for side in [-1,1]:
        x=-.57+side*.48; y=-1.48
        g.beam('lantern_bracket',(x,-1.35,1.12),(x,y,1.12),.026,'iron')
        g.box('lantern_glass',(x,y,.98),(.105,.105,.18),'glass',.009)
        for xx in [-.052,.052]:
            for yy in [-.052,.052]:g.beam('lantern_frame',(x+xx,y+yy,.88),(x+xx,y+yy,1.08),.012,'brass')
        for z in [.88,1.08]:g.box('lantern_cap',(x,y,z),(.14,.14,.035),'brass',.005)
    # Detailed chimney, kept clear of the observatory aperture.
    for row in range(6):
        g.box('chimney_course',(1.08,.78,2.09+row*.14),(.29,.29,.132),'stone',.012)
    g.box('chimney_cap',(1.08,.78,2.91),(.38,.38,.08),'trim')
    g.box('chimney_soot',(1.08,.78,2.955),(.22,.22,.015),'iron',.003)
    for angle in np.linspace(0,math.tau,6,endpoint=False):
        points=[(1.08+rr*math.cos(angle),.78+rr*math.sin(angle),z) for rr,z in [(.11,2.95),(.12,3.04),(.14,3.16),(.17,3.23)]]
        g.tube('chimney_cage_spike',points,.010,'iron')
    for z in [3.04,3.14]:
        points=[(1.08+.14*math.cos(a),.78+.14*math.sin(a),z) for a in np.linspace(0,math.tau,24,endpoint=False)]
        g.tube('chimney_crown_ring',points,.012,'iron',closed=True)
    observatory_dome(g,0,.43,2.83)
    # Small armillary above the front gable, with rings readable from either side.
    center=np.array((-.57,-1.14,2.87))
    g.cylinder('armillary_pedestal',center-(0,0,.23),center-(0,0,.08),.055,'brass',end_radius=.035)
    for axis in [(0,0,1),(1,0,.3),(.3,1,.6)]:
        basis=frame(axis)
        pts=[center+.14*(math.cos(t)*basis[:,0]+math.sin(t)*basis[:,1]) for t in np.linspace(0,math.tau,32,endpoint=False)]
        g.tube('armillary_ring',pts,.009,'brass',closed=True)
    g.beam('armillary_axis',center-(.03,0,.19),center+(.03,0,.19),.012,'brass')
    return g
