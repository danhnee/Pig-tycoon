"""Independent functional-building study; never edits Unity or shared world sources."""
from pathlib import Path
import sys
sys.stdout.reconfigure(encoding='utf-8')

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.collab/artifacts/functional-buildings-codex'
DOC = Path('D:/CodexHome/visualizations/2026/10/18/01a14cfe-2d81-7d83-a693-615ea95ab2a4/source-documents')

def inspect_sources():
    import re
    for kind in ('mechanics', 'technical'):
        lines = (DOC / (kind + '.txt')).read_text(encoding='utf-8').splitlines()
        for i, line in enumerate(lines):
            if re.search(r'Tháp Thông Tin|Kho Cấp|Kho cấp|Nhà Kho|nhà kho|Nhà Chính|Phòng Xây Dựng|Phòng Nghiên Cứu|Cường Hóa|Cường hóa|Warehouse|kho chứa', line):
                print(f'{kind}:{i+1} {line}')
    for base in (ROOT/'Assets/Scripts', ROOT/'Assets/Data'):
        if not base.exists():
            continue
        for path in base.rglob('*'):
            if path.is_file() and path.suffix in ('.cs', '.json', '.asset'):
                content = path.read_text(encoding='utf-8', errors='replace')
                if re.search(r'Warehouse|warehouse|StorageLevel|StorageCapacity|InformationTower|Enhancement', content):
                    print('CODE', path.relative_to(ROOT))
                    for i, line in enumerate(content.splitlines()):
                        if re.search(r'Warehouse|warehouse|StorageLevel|StorageCapacity|InformationTower|Enhancement', line):
                            print(f'  {i+1}: {line}')


def build_study():
    """Native mesh craft study. Geometry and material palettes export to glTF."""
    if '--bpy-runtime' in sys.argv:
        sys.path.insert(0,sys.argv[sys.argv.index('--bpy-runtime')+1])
    import bpy
    import bmesh
    import math
    import random
    import json
    import subprocess
    import numpy as np
    from mathutils import Vector

    processes = subprocess.check_output(['tasklist', '/FO', 'CSV'], text=True, errors='replace')
    assert not any(row.startswith('"Unity.exe"') for row in processes.splitlines()), 'Close Unity first'
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.use_scripts_auto_execute=False
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    rng = random.Random(18068)

    def material(name, rgb, metallic=0):
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*rgb, 1)
        mat.use_nodes = True
        shader = mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = (*rgb, 1)
        shader.inputs['Roughness'].default_value = .80 if metallic == 0 else .46
        shader.inputs['Metallic'].default_value = metallic
        if name.startswith(('oak','plaster','stone','slate','linen','sack','clay')):
            # Exportable local albedo; UV grain follows each face's long edge.
            size=384
            uu,vv=np.meshgrid(np.arange(size)/size,np.arange(size)/size)
            noise=np.random.default_rng(sum(map(ord,name))).random((size,size))-.5
            if name.startswith('oak'):
                waves=vv+.013*np.sin(uu*math.tau*3)+.005*np.sin(uu*math.tau*9)
                grain=np.sin(waves*math.tau*61)*.025+np.sin(waves*math.tau*17)*.045
                knot=np.sqrt(((uu-.55)*.38)**2+((vv-.52)*1.25)**2)
                pattern=grain+.025*np.sin(knot*math.tau*55)*np.exp(-knot*14)+noise*.045
            else:
                pattern=.020*np.sin(uu*math.tau*4+np.sin(vv*math.tau*3))+.018*np.sin(vv*math.tau*17+uu*12)+noise*.055
            rgba=np.ones((size,size,4),dtype=np.float32)
            linear=np.clip(np.array(rgb)[None,None,:]*(1+pattern[:,:,None]*2.7)+pattern[:,:,None]*.09,.01,.95)
            # PNG base-color data is sRGB; direct shader colors above are linear.
            rgba[:,:,:3]=np.where(linear<=.0031308,linear*12.92,1.055*linear**(1/2.4)-.055)
            tex=bpy.data.images.new(name+' baked craft albedo',width=size,height=size)
            tex.pixels.foreach_set(rgba.ravel());tex.filepath_raw=str(OUT/(name+'-albedo.png'))
            tex.file_format='PNG';tex.save();tex.pack()
            node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=tex
            mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
        return mat

    colors = {
        'oak': (.25,.13,.065), 'oak_light': (.40,.24,.12), 'oak_dark': (.13,.07,.039),
        'ochre': (.55,.37,.18), 'plaster': (.78,.72,.57), 'stone': (.43,.44,.40),
        'stone_light': (.58,.57,.49), 'stone_dark': (.31,.33,.31),
        'slate': (.115,.19,.23), 'slate_light': (.19,.28,.31), 'slate_dark': (.07,.13,.16),
        'iron': (.10,.12,.115), 'brass': (.53,.34,.105), 'copper': (.25,.40,.35),
        'linen': (.69,.58,.38), 'sack': (.56,.45,.28), 'dark': (.035,.046,.038),
        'glass': (.22,.43,.39), 'clay': (.47,.22,.12), 'paper': (.82,.75,.55),
        'warmglass': (.92,.53,.13), 'blue': (.13,.28,.32),
    }
    palette = {key: material(key, rgb, .65 if key in ('iron','brass','copper') else 0)
               for key,rgb in colors.items()}
    all_records=[]

    class MeshBatch:
        def __init__(self, name):
            self.name=name
            self.parts={}

        def mesh(self, vertices, faces, mat):
            data=self.parts.setdefault(mat, [[],[]])
            offset=len(data[0]);data[0].extend(vertices)
            data[1].extend([tuple(offset+j for j in face) for face in faces])

        def box(self, center, size, mat, rotation=0):
            x,y,z=center;w,d,h=(v/2 for v in size)
            points=[(-w,-d,-h),(w,-d,-h),(w,d,-h),(-w,d,-h),
                    (-w,-d,h),(w,-d,h),(w,d,h),(-w,d,h)]
            c,s=math.cos(rotation),math.sin(rotation)
            self.mesh([(x+a*c-b*s,y+a*s+b*c,z+q) for a,b,q in points],
                      [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)

        def beam(self, a, b, width, depth, mat):
            av,bv=Vector(a),Vector(b);delta=bv-av
            u=delta.cross(Vector((0,0,1)))
            if u.length < .001:u=delta.cross(Vector((0,1,0)))
            u.normalize();u*=width/2
            v=delta.normalized().cross(u.normalized())*depth/2
            vv=[av-u-v,av+u-v,av+u+v,av-u+v,bv-u-v,bv+u-v,bv+u+v,bv-u+v]
            self.mesh([tuple(p) for p in vv],[(0,3,2,1),(4,5,6,7),(0,1,5,4),
                       (1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)

        def cylinder(self, center, radius, height, mat, sides=16, top=None):
            x,y,z=center;rr=radius if top is None else top
            vv=[(x+math.cos(i*math.tau/sides)*r,y+math.sin(i*math.tau/sides)*r,z+h)
                for r,h in ((radius,-height/2),(rr,height/2)) for i in range(sides)]
            ff=[tuple(reversed(range(sides))),tuple(range(sides,sides*2))]
            ff += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
            self.mesh(vv,ff,mat)

        def finish(self):
            objects=[]
            for key,(verts,faces) in self.parts.items():
                me=bpy.data.meshes.new(self.name+'_'+key)
                me.from_pydata(verts,[],faces);me.update()
                ob=bpy.data.objects.new(self.name+'_'+key,me)
                bpy.context.collection.objects.link(ob);me.materials.append(palette[key])
                uv=me.uv_layers.new(name='Craft grain projection')
                for poly in me.polygons:
                    p=[me.vertices[i].co for i in poly.vertices]
                    edges=[p[(i+1)%len(p)]-p[i] for i in range(len(p))]
                    u=max(edges,key=lambda e:e.length_squared).normalized()
                    v=poly.normal.cross(u).normalized()
                    for loop in poly.loop_indices:
                        pos=me.vertices[me.loops[loop].vertex_index].co
                        uv.data[loop].uv=(pos.dot(u)*.65,pos.dot(v)*.65)
                objects.append(ob)
            return objects

    def masonry(batch,cx,cy,w,d,z,rows):
        # Bonded masonry is actual individual cuboids, including all four faces.
        for row in range(rows):
            zz=z+row*.28+.14
            for side in (-1,1):
                n=max(2,round(w/.56));step=w/n
                for i in range(n+1):
                    start=max(-w/2,-w/2+(i-.5*(row%2))*step)
                    end=min(w/2,-w/2+(i+1-.5*(row%2))*step)
                    if end-start<.04:continue
                    x=cx+(start+end)/2
                    batch.box((x,cy+side*(d/2-.13),zz),(end-start-.025,.27,.25),
                              rng.choice(['stone','stone','stone_light','stone_dark']))
                n=max(2,round((d-.55)/.56));step=(d-.55)/n
                for i in range(n):
                    yy=cy-(d-.55)/2+(i+.5)*step
                    batch.box((cx+side*(w/2-.13),yy,zz),(.27,step-.025,.25),
                              rng.choice(['stone','stone','stone_light']))
        # Continuous stone cap carries the timber sill; no unsupported air gap.
        batch.box((cx,cy,z+rows*.28+.015),(w,d,.12),'stone_light')

    def roof(batch,cx,cy,w,d,eave,rise):
        """Two pitch slate roof with solid substrate, overlapping individual courses."""
        half=w/2+.33; length=d+.72
        ridge=eave+rise
        for side in (-1,1):
            # Each quad is extruded down to remain a watertight component.
            rows=max(5,round(half/.33));cols=max(7,round(length/.44))
            for r in range(rows):
                x0=half*r/rows;x1=min(half+.025,half*(r+1)/rows+.10)
                z0=ridge-rise*x0/half;z1=ridge-rise*x1/half
                for col in range(cols):
                    y0=cy-length/2+col*length/cols
                    y1=y0+length/cols-.016
                    top=[(cx+side*x0,y0,z0+.025),(cx+side*x1,y0,z1+.025),
                         (cx+side*x1,y1,z1+.025),(cx+side*x0,y1,z0+.025)]
                    if side<0:top.reverse()
                    vv=top+[(x,y,z-.075) for x,y,z in top]
                    batch.mesh(vv,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),
                                   (2,6,7,3),(3,7,4,0)],rng.choice(['slate']*5+['slate_light','slate_dark']))
            for y in (cy-length/2-.01,cy+length/2+.01):
                batch.beam((cx,y,ridge+.07),(cx+side*half,y,eave+.04),.16,.16,'oak_dark')
            batch.beam((cx+side*half,cy-length/2,eave),(cx+side*half,cy+length/2,eave),.16,.20,'oak_dark')
        batch.beam((cx,cy-length/2-.05,ridge+.10),(cx,cy+length/2+.05,ridge+.10),.22,.16,'slate_dark')

    def gable(batch,cx,cy,w,d,z,h,rise,wall='oak_light'):
        # Solid gable volume, framed surface, with a constant finished floor origin.
        y0,y1=cy-d/2,cy+d/2;x0,x1=cx-w/2,cx+w/2
        vv=[(x0,y0,z),(x1,y0,z),(x1,y0,z+h),(cx,y0,z+h+rise),(x0,y0,z+h),
            (x0,y1,z),(x1,y1,z),(x1,y1,z+h),(cx,y1,z+h+rise),(x0,y1,z+h)]
        batch.mesh(vv,[(4,3,2,1,0),(5,6,7,8,9),(0,1,6,5),(1,2,7,6),
                       (2,3,8,7),(3,4,9,8),(4,0,5,9)],wall)
        for side in (-1,1):
            yy=cy+side*(d/2+.035)
            # Vertical cladding and two exposed tie beams prevent flat rear facades.
            for i in range(round(w/.22)+1):
                x=x0+.06+i*(w-.12)/round(w/.22)
                top=z+h+rise*(1-abs(x-cx)/(w/2))
                batch.box((x,yy,(z+top)/2),(.025,.028,top-z),'oak')
            for x in (x0+.06,cx,x1-.06):
                top=z+h if x!=cx else z+h+rise-.15
                batch.box((x,yy+side*.025,(z+top)/2),(.19,.17,top-z),'oak_dark')
            for zz in (z+.12,z+h-.08):
                batch.box((cx,yy+side*.03,zz),(w,.20,.20),'oak_dark')
            for sign in (-1,1):
                batch.beam((cx,yy+side*.045,z+h+rise-.12),(cx+sign*(w/2-.10),yy+side*.045,z+h),.14,.14,'oak_dark')
                batch.beam((cx+sign*.25,yy+side*.07,z+h-.18),(cx+sign*(w/2-.25),yy+side*.07,z+.45),.12,.14,'oak')
        for side in (-1,1):
            xx=cx+side*(w/2+.04)
            # Stone wainscot and braces articulate long side-wall planes.
            for row in range(2):
                count=max(3,round(d/.58))
                for j in range(count):
                    yy=cy-d/2+(j+.5)*d/count
                    batch.box((xx,yy,z+.14+row*.24),(.12,d/count-.025,.22),
                              rng.choice(['stone','stone_dark','stone_light']))
            for yy in [cy-d*.30,cy+d*.30]:
                batch.beam((xx+side*.035,yy-d*.14,z+.65),(xx+side*.035,yy+d*.14,z+h-.22),.12,.13,'oak')
            for j in range(5):
                y=cy-d/2+j*d/4
                batch.box((xx,y,z+h/2),(.18,.19,h),'oak_dark')
            for zz in (z+.12,z+h-.08):batch.box((xx,cy,zz),(.18,d,.20),'oak_dark')
        roof(batch,cx,cy,w,d,z+h,rise+.12)

    def door(batch,x,y,z,width=1.7,height=2.0):
        side=-1 if y<0 else 1
        batch.box((x,y,z+height/2),(width+.20,.13,height+.12),'oak_dark')
        for i in range(10):
            batch.box((x-width/2+(i+.5)*width/10,y+side*.09,z+height/2),
                      (width/10-.014,.10,height),'oak_light' if i%3 else 'oak')
        for dx in (-width/4,width/4):
            for zz in (z+.3,z+height-.3):batch.box((x+dx,y+side*.16,zz),(width*.40,.04,.095),'iron')
        batch.box((x,y+side*.19,z+.95),(.14,.08,.22),'iron')
        batch.box((x,y+side*.28,z+.03),(width+.4,.65,.13),'stone_light')

    def window(batch,x,y,z,w=.75,h=.85):
        side=-1 if y<0 else 1
        batch.box((x,y,z),(w+.18,.12,h+.18),'oak_dark')
        batch.box((x,y+side*.08,z),(w,.04,h),'glass')
        for dx in (-w/2,0,w/2):batch.box((x+dx,y+side*.12,z),(.06,.045,h+.1),'oak_light')
        for dz in (-h/2,0,h/2):batch.box((x,y+side*.12,z+dz),(w+.1,.045,.06),'oak_light')
        batch.box((x,y+side*.18,z-h/2-.12),(w+.35,.42,.13),'stone_light')

    def barrel(batch,x,y,z,size=.65):
        # Faceted stave shell with strong silhouette rings, cap and feet.
        batch.cylinder((x,y,z+size*.60),size*.39,size*1.12,'oak_light',14,top=size*.36)
        for zz in (.12,.42,.92,1.12):
            batch.cylinder((x,y,z+size*zz),size*.40,size*.055,'iron',14)
        batch.cylinder((x,y,z+size*1.18),size*.34,.05,'oak',14)

    def crate(batch,x,y,z,size=.65):
        batch.box((x,y,z+size/2),(size,size,size),'oak_light')
        for side in (-1,1):
            for zz in (.12,.85):
                batch.box((x,y+side*(size/2+.02),z+size*zz),(size+.05,.07,.09),'oak_dark')
            batch.beam((x-size*.38,y+side*(size/2+.04),z+.1),
                       (x+size*.38,y+side*(size/2+.04),z+size-.1),.065,.08,'oak')

    def hoist(batch,x,y,z):
        batch.box((x,y,z+.65),(.20,.20,1.55),'oak_dark')
        batch.beam((x,y,z+1.35),(x,y-1.15,z+1.35),.18,.18,'oak')
        batch.beam((x,y,z+.55),(x,y-.90,z+1.30),.12,.12,'oak_dark')
        batch.beam((x,y-1.02,z+1.32),(x,y-1.02,.55),.03,.03,'linen')
        batch.box((x,y-1.02,.50),(.33,.35,.12),'iron')
        cy=y-1.02;cz=z+1.30;n=16;outer=.23;inner=.17
        verts=[]
        for yy,rr in ((cy-.055,outer),(cy-.055,inner),(cy+.055,outer),(cy+.055,inner)):
            verts.extend([(x+math.cos(i*math.tau/n)*rr,yy,cz+math.sin(i*math.tau/n)*rr) for i in range(n)])
        faces=[]
        for i in range(n):
            j=(i+1)%n
            faces.extend([(i,j,j+n,i+n),(i+2*n,i+3*n,j+3*n,j+2*n),
                          (i,i+2*n,j+2*n,j),(i+n,j+n,j+3*n,i+3*n)])
        batch.mesh(verts,faces,'iron')
        for angle in range(0,360,60):
            a=math.radians(angle)
            batch.beam((x,cy,cz),(x+math.cos(a)*inner,cy,cz+math.sin(a)*inner),.032,.035,'brass')

    def dormer(batch):
        detail=MeshBatch('Dormer')
        gable(detail,0,0,1.35,1.45,4.55,.85,.56,'plaster')
        window(detail,0,-.84,5.01,.80,.62)
        for key,(vertices,faces) in detail.parts.items():
            batch.mesh([(2.0-y,.35+x,z) for x,y,z in vertices],faces,key)

    def warehouse(state):
        batch=MeshBatch('warehouse_'+state)
        width=4.8;depth={'A':5.2,'B':6.6,'C':7.6}[state]
        loft=state!='A';height=2.6 if not loft else 3.8
        base=.35 if state=='A' else .62
        masonry(batch,0,0,width+.1,depth+.1,0,1 if state=='A' else 2)
        gable(batch,0,0,width,depth,base,height,1.75,'oak_light' if state=='A' else 'plaster')
        door(batch,0,-depth/2-.12,base)
        door(batch,0,depth/2+.12,base,width=1.3,height=1.9)
        window(batch,-1.65,-depth/2-.12,base+1.6,.60,.70)
        window(batch,1.65,-depth/2-.12,base+1.6,.60,.70)
        window(batch,1.60,depth/2+.12,base+1.6,.60,.70)
        if loft:
            dormer(batch)
            door(batch,0,-depth/2-.13,base+2.65,1.1,1.15)
            hoist(batch,0,-depth/2-.25,base+3.45)
            window(batch,-1.5,depth/2+.12,base+3.0,.70,.70)
            # External dry-feed gallery makes B structurally broader than A.
            annex_x=-3.2
            masonry(batch,annex_x,.7,1.75,depth-1.3,0,2)
            gable(batch,annex_x,.7,1.65,depth-1.4,base,1.65,.60,'oak')
            door(batch,annex_x,-(depth-1.4)/2+.7-.12,base,.95,1.55)
            for j in range(3):barrel(batch,annex_x-1.15,-1+j*.9,0,.7)
        if state=='C':
            # Transverse elevated granary wing + stone silo, no uniform scaling.
            masonry(batch,3.7,1.15,2.7,4.7,0,2)
            gable(batch,3.7,1.15,2.6,4.6,.62,2.95,1.18,'plaster')
            door(batch,3.7,-1.15-.12,.62,1.5,2.1)
            window(batch,3.7,3.45+.12,2.7,1.0,1.0)
            silo_x,silo_y=3.9,3.3
            batch.cylinder((silo_x,silo_y,2.4),1.15,4.8,'stone',24)
            for z in (.25,1.5,3.0,4.7):batch.cylinder((silo_x,silo_y,z),1.18,.12,'iron',24)
            batch.cylinder((silo_x,silo_y,5.2),1.38,.9,'slate',24,.06)
            # Loading deck and stairs are visibly navigable physical structures.
            batch.box((2.4,-3.9,.52),(5.8,1.5,.25),'oak')
            for x in (.3,2.3,4.8):batch.box((x,-4.3,.22),(.23,.23,.44),'oak_dark')
            for i in range(3):batch.box((4.7,-4.75-i*.32,.36-i*.10),(1.1,.34,.2),'stone_light')
            for j in range(3):crate(batch,3+j*.68,-3.9,.66,.6)
        # Human-scale loading artifacts retain identity through every state.
        for j in range(3):crate(batch,-1.5+j*.65,-depth/2-1,0,.58)
        barrel(batch,2.85,-depth/2+.6,0,.75)
        barrel(batch,2.9,-depth/2+1.6,0,.62)
        batch.box((1.75,-depth/2-.2,base+2.4),(.42,.18,.55),'iron')
        batch.box((1.75,-depth/2-.32,base+2.4),(.28,.08,.36),'warmglass')
        # Rear stock ledger board, cat ledge, and a side service entrance.
        batch.box((-1.55,depth/2+.15,base+1.55),(.8,.14,.75),'oak_dark')
        batch.box((-1.55,depth/2+.24,base+1.55),(.65,.035,.59),'paper')
        batch.box((width/2+.18,.8,.95),(.55,1.0,.11),'oak')
        return batch.finish()

    def construction():
        batch=MeshBatch('construction_workshop')
        masonry(batch,-1,.8,4.5,5.1,0,2)
        gable(batch,-1,.8,4.4,5,.62,2.65,1.4,'plaster')
        door(batch,-1,-1.82,.62,1.75,2.1)
        window(batch,-2.45,3.43,2.05,.8,.9)
        # Large open trussed assembly bay, unmistakably different from a barn.
        for x in (1.6,4.8):
            for y in (-2.2,1.0,3.4):batch.box((x,y,1.65),(.25,.25,3.3),'oak_dark')
        roof(batch,3.2,.6,3.5,6.0,3.3,.55)
        for y in (-2.2,1.0,3.4):
            batch.beam((1.6,y,3.2),(4.8,y,3.2),.23,.23,'oak')
            batch.beam((1.6,y,2.3),(2.5,y,3.2),.16,.16,'oak_dark')
            batch.beam((4.8,y,2.3),(3.9,y,3.2),.16,.16,'oak_dark')
        batch.box((3.1,.7,1.10),(2.3,1.1,.22),'oak_light')
        for x in (2.15,4.05):
            for y in (.3,1.1):batch.box((x,y,.55),(.16,.16,1.1),'oak_dark')
        batch.box((3.1,.65,1.23),(1.1,.7,.025),'blue')
        for j in range(5):
            batch.box((2.4+j*.28,2.6,.24+j*.06),(.24,2.2,.20),'oak_light')
        # Gantry rail and pulley lift; blueprint bureau at open work frontage.
        batch.beam((1.9,-1.2,3.15),(4.5,-1.2,3.15),.16,.20,'iron')
        batch.beam((3.4,-1.2,3.15),(3.4,-1.2,1.65),.035,.035,'iron')
        batch.box((3.4,-1.2,1.65),(.25,.18,.24),'brass')
        batch.box((-2.8,-2.15,1.25),(1.1,.12,.95),'oak_dark')
        batch.box((-2.8,-2.23,1.25),(.94,.025,.77),'blue')
        for j in range(3):crate(batch,-3.65,.5+j*.8,0,.65)
        return batch.finish()

    def enhancement():
        batch=MeshBatch('enhancement_atelier')
        masonry(batch,0,0,5.2,4.4,0,3)
        gable(batch,0,0,5.0,4.2,.84,2.6,1.95,'plaster')
        door(batch,-1.4,-2.22,.84,1.25,2.05)
        window(batch,1.0,-2.24,2.25,1.65,1.4)
        window(batch,0,2.24,2.6,1.4,1.15)
        # Masonry chimney, copper hood, paper drying veranda: craft not magic temple.
        batch.box((2.0,.9,3.2),(.95,.95,4.8),'stone')
        for z in (4.5,5.6):batch.box((2,.9,z),(1.12,1.12,.20),'stone_light')
        batch.box((2,.9,5.76),(.66,.66,.08),'dark')
        for x in (-2.1,2.1):batch.box((x,-3.55,1.75),(.18,.18,3.5),'oak_dark')
        batch.beam((-2.1,-3.55,3.35),(2.1,-3.55,3.35),.17,.17,'oak_dark')
        for j in range(6):
            batch.box((-1.6+j*.63,-3.55,2.85),(.44,.035,.63),'paper')
            batch.box((-1.6+j*.63,-3.55,3.22),(.05,.08,.1),'oak_dark')
        # Exterior demonstration engraving station; blank paper, no late-saga glyph.
        batch.box((.7,-3.05,1.0),(1.6,.85,.22),'oak_dark')
        for x in (.05,1.35):batch.box((x,-3.05,.5),(.22,.65,1),'stone')
        batch.box((.7,-3.05,1.14),(.95,.55,.05),'stone_light')
        batch.cylinder((1.22,-2.9,1.32),.12,.26,'brass',12)
        batch.box((-.6,-3.0,1.2),(.75,.70,.3),'copper')
        barrel(batch,3.05,1,0,.75)
        return batch.finish()

    def validate(name,objects):
        vertices=[];triangles=0;records=[]
        for ob in objects:
            mesh=ob.data;mesh.calc_loop_triangles()
            assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
            bm=bmesh.new();bm.from_mesh(mesh)
            # Recalculate winding of disconnected solids consistently before export.
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh)
            nonmanifold=sum(not edge.is_manifold for edge in bm.edges)
            assert nonmanifold==0,(name,ob.name,nonmanifold)
            volume=bm.calc_volume(signed=True);assert volume>0,(name,ob.name,volume)
            bm.free();mesh.calc_loop_triangles()
            minimum=min((mesh.vertices[t.vertices[1]].co-mesh.vertices[t.vertices[0]].co).cross(
                        mesh.vertices[t.vertices[2]].co-mesh.vertices[t.vertices[0]].co).length/2
                        for t in mesh.loop_triangles)
            assert minimum>1e-10,(name,ob.name,minimum)
            assert tuple(ob.location)==(0,0,0) and tuple(ob.scale)==(1,1,1)
            vertices.extend(tuple(v.co) for v in mesh.vertices);triangles+=len(mesh.loop_triangles)
            records.append(dict(mesh=ob.name,nonmanifold_edges=nonmanifold,minimum_triangle_area=minimum))
        bounds=[[min(v[a] for v in vertices) for a in range(3)],
                [max(v[a] for v in vertices) for a in range(3)]]
        assert bounds[0][2]>=-.001
        result=dict(asset=name,status='PASS',local_origin=True,origin='Ground center of original central module',
                    vertices=len(vertices),triangles=triangles,bounds=bounds,meshes=records,
                    caveat='Intersecting closed craft components; render study, not optimized collision or LOD geometry.')
        all_records.append(result)
        print('VALIDATED',name,len(vertices),triangles,flush=True)

    scene=bpy.context.scene
    scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
    scene.cycles.device='CPU';scene.render.threads_mode='FIXED';scene.render.threads=6
    scene.render.resolution_x=1800;scene.render.resolution_y=1000
    scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
    if scene.world is None:scene.world=bpy.data.worlds.new('Study world')
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.72,.78,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
    scene.view_settings.view_transform='AgX'
    bpy.ops.object.light_add(type='AREA',location=(-12,-17,23))
    light=bpy.context.object;light.data.energy=2400;light.data.size=11
    light.rotation_euler=(Vector((0,0,0))-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.light_add(type='SUN',location=(-10,-12,20))
    sun=bpy.context.object;sun.data.energy=2;sun.data.angle=.16;sun.data.color=(1,.86,.67)
    sun.rotation_euler=(Vector((0,0,0))-sun.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=250,location=(0,0,-.055))
    floor=bpy.context.object;floor.name='STUDIO_ONLY';floor.data.materials.append(material('Studio ivory',(.66,.67,.62)))
    bpy.ops.object.camera_add();camera=bpy.context.object;camera.data.type='ORTHO';scene.camera=camera

    models={}
    for name,factory in [('warehouse_A',lambda:warehouse('A')),('warehouse_B',lambda:warehouse('B')),
                         ('warehouse_C',lambda:warehouse('C')),('construction_workshop',construction),
                         ('enhancement_atelier',enhancement)]:
        objects=factory();models[name]=objects;validate(name,objects)
        bpy.ops.object.select_all(action='DESELECT')
        for ob in objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True)
        for ob in objects:ob.hide_render=True
    (OUT/'geometry-validation.json').write_text(json.dumps(dict(
        status='PASS',blender_version=bpy.app.version_string,warehouse_states='Proposals A/B/C; not game levels',
        assets=all_records),indent=2),encoding='utf-8')

    def show(names,offsets=None):
        for name,objects in models.items():
            for ob in objects:
                ob.hide_render=name not in names
                ob.location=(offsets or {}).get(name,(0,0,0))

    def render(filename,position,target,scale):
        camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.ortho_scale=scale;scene.render.filepath=str(OUT/filename)
        bpy.ops.render.render(write_still=True);print('RENDERED',filename,flush=True)

    warehouses=['warehouse_A','warehouse_B','warehouse_C']
    offsets={'warehouse_A':(-12,0,0),'warehouse_B':(0,0,0),'warehouse_C':(13,0,0)}
    show(warehouses,offsets)
    render('01-warehouse-progression-front.png',(22,-42,27),(1,0,2.1),43)
    render('02-warehouse-progression-rear.png',(-22,42,25),(1,0,2.1),43)
    clay=material('Neutral clay',(.5,.51,.49));scene.view_layers[0].material_override=clay
    render('03-warehouse-progression-clay.png',(22,-42,27),(1,0,2.1),43)
    scene.view_layers[0].material_override=None
    show(['warehouse_C'])
    render('04-warehouse-C-front-detail.png',(12,-20,13),(1,0,2.6),16.5)
    render('05-warehouse-C-rear-detail.png',(-12,20,13),(1,0,2.6),16.5)
    show(['construction_workshop'])
    render('06-construction-open-assembly.png',(12,-19,12),(.5,.5,2),16)
    show(['enhancement_atelier'])
    render('07-enhancement-paper-and-engraving.png',(12,-19,12),(0,0,2.4),15)
    # A .blend with every component centered locally; collections separate states.
    for name,objects in models.items():
        collection=bpy.data.collections.new(name);scene.collection.children.link(collection)
        for ob in objects:
            ob.location=(0,0,0);ob.hide_render=name!='warehouse_C'
            ob.hide_set(name!='warehouse_C')
            for current in list(ob.users_collection):current.objects.unlink(ob)
            collection.objects.link(ob)
    camera.location=(12,-20,13);camera.rotation_euler=(Vector((1,0,2.6))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=16.5
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'functional-buildings-local-origin.blend'))
    print('STUDY_COMPLETE',str(OUT),flush=True)

if __name__ == '__main__' and '--blender-build' in sys.argv:
    build_study()
elif __name__ == '__main__' and '--repair-viewport' in sys.argv:
    sys.path.insert(0,sys.argv[sys.argv.index('--bpy-runtime')+1])
    import bpy
    bpy.context.preferences.filepaths.use_scripts_auto_execute=False
    path=OUT/'functional-buildings-local-origin.blend'
    bpy.ops.wm.open_mainfile(filepath=str(path),use_scripts=False)
    for ob in bpy.data.objects:
        if ob.type=='MESH' and ob.name!='STUDIO_ONLY':
            ob.hide_set(not ob.name.startswith('warehouse_C_'))
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
    print('Saved editable BLEND with only warehouse_C visible in viewport')
elif __name__ == '__main__' and '--build' in sys.argv:
    import subprocess
    OUT.mkdir(parents=True,exist_ok=True)
    # An explicitly supplied verified bpy Python avoids touching an offline drive.
    if '--bpy-runtime' in sys.argv:
        runtime=Path(sys.argv[sys.argv.index('--bpy-runtime')+1]).resolve()
        assert (runtime/'bpy').exists(), runtime
        command=[sys.executable,str(Path(__file__).resolve()),'--blender-build','--bpy-runtime',str(runtime)]
    elif '--bpy-python' in sys.argv:
        python=Path(sys.argv[sys.argv.index('--bpy-python')+1])
        assert python.is_file(), python
        command=[str(python),str(Path(__file__).resolve()),'--blender-build']
    else:
        blender=Path('F:/Tools/blender-5.3.0-windows-x64/blender.exe')
        assert blender.is_file(), blender
        command=[str(blender),'--background','--factory-startup','--python',str(Path(__file__).resolve()),
                 '--','--blender-build']
    with (OUT/'build.log').open('w',encoding='utf-8') as log:
        result=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT)
    print('Blender exit:',result.returncode,'Log:',OUT/'build.log')
    raise SystemExit(result.returncode)
elif __name__ == '__main__' and '--inspect' in sys.argv:
    inspect_sources()
elif __name__ == '__main__' and '--check' in sys.argv:
    import ast
    import subprocess
    ast.parse(Path(__file__).read_text(encoding='utf-8'))
    print('Python syntax: PASS')
    result=subprocess.check_output(['tasklist','/FO','CSV'],text=True,errors='replace')
    unity=any(row.startswith('"Unity.exe"') for row in result.splitlines())
    print('Unity Editor running:',unity)
    assert not unity
    print('Output files:')
    for path in sorted(OUT.glob('*')):
        if path.is_file(): print(path.name,path.stat().st_size)
elif __name__ == '__main__' and '--tail' in sys.argv:
    path=OUT/'build.log'
    print('\n'.join(path.read_text(encoding='utf-8',errors='replace').splitlines()[-28:]) if path.exists() else 'No build log yet')
elif __name__ == '__main__' and '--active' in sys.argv:
    import os
    sys.path.insert(0,str(OUT/'diagnostics-runtime'))
    import psutil
    for process in psutil.process_iter(['pid','name','cmdline']):
        try:
            cmd=process.info['cmdline'] or []
            if process.pid!=os.getpid() and any('functional_building_study.py' in arg for arg in cmd):
                print(process.info)
                if '--stop-own-renderer' in sys.argv and ('--blender-build' in cmd or ('--build' in cmd and '--bpy-runtime' not in cmd)):
                    process.terminate()
                    process.wait(timeout=15)
                    print('Terminated owned renderer',process.pid)
        except (psutil.AccessDenied,psutil.NoSuchProcess):pass
elif __name__ == '__main__' and '--warehouse-audit' in sys.argv:
    import re
    matches=[]
    for kind in ('mechanics','technical'):
        for line in (DOC/(kind+'.txt')).read_text(encoding='utf-8').splitlines():
            if re.search(r'\bKho\b|sức chứa kho|warehouse|storage',line,re.I):matches.append(line)
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'warehouse-source-matches.txt').write_text('\n'.join(matches),encoding='utf-8')
    print('\n'.join(matches))
elif __name__ == '__main__' and '--verify' in sys.argv:
    import json
    import struct
    import hashlib
    from PIL import Image, ImageStat
    geometry=json.loads((OUT/'geometry-validation.json').read_text(encoding='utf-8'))
    assert geometry['status']=='PASS' and len(geometry['assets'])==5
    records=[]
    for entry in geometry['assets']:
        path=OUT/(entry['asset']+'.glb');raw=path.read_bytes()
        magic,version,length=struct.unpack_from('<III',raw,0)
        assert magic==0x46546c67 and version==2 and length==len(raw)
        json_length,chunk_type=struct.unpack_from('<II',raw,12)
        assert chunk_type==0x4E4F534A
        document=json.loads(raw[20:20+json_length].decode('utf-8'))
        assert document.get('meshes') and document.get('materials')
        assert len(document.get('images',[]))>=8
        assert all('bufferView' in im and 'uri' not in im for im in document['images'])
        assert all('TEXCOORD_0' in primitive['attributes'] for mesh in document['meshes'] for primitive in mesh['primitives'])
        assert not any('STUDIO' in node.get('name','') for node in document.get('nodes',[]))
        assert all(node.get('translation',[0,0,0])==[0,0,0] for node in document.get('nodes',[]))
        assert all(node.get('scale',[1,1,1])==[1,1,1] for node in document.get('nodes',[]))
        records.append(dict(asset=path.name,bytes=len(raw),mesh_count=len(document['meshes']),
                            vertices=entry['vertices'],triangles=entry['triangles'],
                            sha256=hashlib.sha256(raw).hexdigest(),
                            embedded_images=len(document['images']),uv_check='PASS',local_transform_check='PASS'))
    images=[]
    for index in range(1,8):
        matches=list(OUT.glob(f'{index:02d}-*.png'));assert len(matches)==1,matches
        with Image.open(matches[0]) as image:
            image.load();assert image.size==(1800,1000)
            stat=ImageStat.Stat(image.convert('RGB'))
            assert max(stat.stddev)>12,(matches[0],stat.stddev)
            images.append(dict(file=matches[0].name,size=list(image.size),stddev=stat.stddev))
    blend=OUT/'functional-buildings-local-origin.blend';assert blend.stat().st_size>10000
    catalog=json.loads((OUT/'functional-building-catalog.json').read_text(encoding='utf-8'))
    assert len(catalog['facilities'])==7
    assert catalog['placement_policy']['warehouse_variants_are_mutually_exclusive']
    report=dict(status='PASS',geometry_status=geometry['status'],blender_version=geometry['blender_version'],
                glb_files=records,images=images,blend_bytes=blend.stat().st_size)
    (OUT/'artifact-verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))
elif __name__ == '__main__' and '--texture-audit' in sys.argv:
    from PIL import Image,ImageStat
    for name in ('oak','plaster','slate'):
        with Image.open(OUT/(name+'-albedo.png')) as im:
            print(name,ImageStat.Stat(im.convert('RGB')).mean)
elif __name__ == '__main__' and '--audit-shelter' in sys.argv:
    # Independent read-only check of Argon's actual exported geometry, not its flags.
    import json,struct,hashlib,itertools
    path=ROOT/'.collab/artifacts/functional-buildings-argon/herd-shelter.glb'
    raw=path.read_bytes()
    magic,version,total=struct.unpack_from('<III',raw,0)
    assert magic==0x46546c67 and version==2 and total==len(raw),'Incomplete GLB; wait for export to finish'
    digest=hashlib.sha256(raw).hexdigest()
    snapshot=OUT/'audit-snapshots'/('herd-shelter-'+digest[:12]+'.glb')
    snapshot.parent.mkdir(parents=True,exist_ok=True)
    if not snapshot.exists():snapshot.write_bytes(raw)
    size,kind=struct.unpack_from('<II',raw,12)
    assert kind==0x4E4F534A
    doc=json.loads(raw[20:20+size].decode('utf-8'))
    identity=[[1 if a==b else 0 for b in range(4)] for a in range(4)]
    def multiply(a,b):
        return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
    def transform(node):
        if 'matrix' in node:return [[node['matrix'][j*4+i] for j in range(4)] for i in range(4)]
        x,y,z,w=node.get('rotation',[0,0,0,1]);scale=node.get('scale',[1,1,1]);t=node.get('translation',[0,0,0])
        r=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
           [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
           [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
        return [[r[i][j]*scale[j] for j in range(3)]+[t[i]] for i in range(3)]+[[0,0,0,1]]
    bounds=[]
    def visit(index,parent):
        node=doc['nodes'][index];world=multiply(parent,transform(node))
        if 'mesh' in node:
            points=[]
            for primitive in doc['meshes'][node['mesh']]['primitives']:
                acc=doc['accessors'][primitive['attributes']['POSITION']]
                assert 'min' in acc and 'max' in acc
                for p in itertools.product(*zip(acc['min'],acc['max'])):
                    q=[sum(world[i][j]*(*p,1)[j] for j in range(4)) for i in range(3)]
                    points.append((q[0],-q[2],q[1]))  # glTF Y-up -> Blender Z-up.
            bounds.append(dict(name=node.get('name',''),minimum=[min(p[i] for p in points) for i in range(3)],
                               maximum=[max(p[i] for p in points) for i in range(3)]))
        for child in node.get('children',[]):visit(child,world)
    for index in doc['scenes'][doc.get('scene',0)]['nodes']:visit(index,identity)
    result={}
    for label,lo,hi in [('west',-10.5,-3.3),('east',3.3,10.5)]:
        obstacles=[]
        for b in bounds:
            a,c=b['minimum'],b['maximum']
            if c[2]<.05 or a[2]>2.0 or c[1]<-4.4 or a[1]>4.4:continue
            if c[0]<=lo or a[0]>=hi:continue
            obstacles.append((max(lo,a[0]),min(hi,c[0]),b['name']))
        merged=[]
        for start,end,name in sorted(obstacles):
            if merged and start<=merged[-1][1]:merged[-1][1]=max(merged[-1][1],end)
            else:merged.append([start,end])
        gaps=[];cursor=lo
        for start,end in merged:
            if start>cursor:gaps.append([cursor,start])
            cursor=max(cursor,end)
        if cursor<hi:gaps.append([cursor,hi])
        lane=max(gaps,key=lambda x:x[1]-x[0]) if gaps else [0,0]
        result[label]=dict(clear_x=lane,width_m=lane[1]-lane[0],meets_6m=(lane[1]-lane[0]>=6),
                           obstacles=[dict(x=[a,b],name=n) for a,b,n in obstacles])
    report=dict(status='PASS' if all(v['meets_6m'] for v in result.values()) else 'FAIL',
                source=str(path),snapshot=str(snapshot),sha256=digest,
                method='Conservative constant straight corridor from exported low-obstacle AABBs, excluding no named geometry',
                envelope_proposal=dict(z_m=[.05,2.0],longitudinal_y_m=[-4.4,4.4]),
                limitation='Art geometry audit only. Height envelope is an explicit review proposal, not a new GDD value; no runtime colliders/navmesh tested.',
                corridors=result)
    (OUT/'argon-shelter-independent-clearance.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='corridors'},indent=2))
    print(json.dumps({k:{a:b for a,b in v.items() if a!='obstacles'} for k,v in result.items()},indent=2))
elif __name__ == '__main__' and '--read' in sys.argv:
    kind, start, end = sys.argv[-3:]
    lines = (DOC / (kind + '.txt')).read_text(encoding='utf-8').splitlines()
    print('\n'.join(lines[int(start)-1:int(end)]))
elif __name__ == '__main__' and '--read-code' in sys.argv:
    relative,start,end=sys.argv[-3:]
    path=(ROOT/relative).resolve()
    assert path.is_relative_to(ROOT)
    lines=path.read_text(encoding='utf-8').splitlines()
    print('\n'.join(f'{i+1}: {lines[i]}' for i in range(int(start)-1,min(int(end),len(lines)))))
elif __name__ == '__main__' and '--search' in sys.argv:
    import re
    pattern = r'Warehouse|Storage|Storehouse|\bKho\b' if sys.argv[-1] == 'storage' else sys.argv[-1]
    for base in (ROOT/'Assets/Scripts/Core', ROOT/'Assets/Data'):
        if not base.exists(): continue
        for path in base.rglob('*'):
            if path.is_file() and path.suffix in ('.cs','.json','.asset','.md'):
                for i,line in enumerate(path.read_text(encoding='utf-8',errors='replace').splitlines()):
                    if re.search(pattern,line,re.I): print(f'{path.relative_to(ROOT)}:{i+1}: {line}')
