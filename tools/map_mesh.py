"""Small array-based mesh container and the project's Y-up binary exporter."""
import struct
import numpy as np


class MapMesh:
    def __init__(self):
        self.vertices=[];self.uvs=[];self.faces=[];self.parts=[]
        self.vertex_count=0;self.triangle_count=0

    def add(self,name,vertices,uvs,faces,flat=False):
        v=np.asarray(vertices,dtype=np.float32);uv=np.asarray(uvs,dtype=np.float32);f=np.asarray(faces,dtype=np.int32)
        if not len(f):return
        used=np.unique(f);inverse=np.full(len(v),-1,dtype=np.int32);inverse[used]=np.arange(len(used))
        v,uv,f=v[used],uv[used],inverse[f]
        if flat:
            v,uv=v[f.reshape(-1)],uv[f.reshape(-1)];f=np.arange(len(v),dtype=np.int32).reshape(-1,3)
        self.vertices.append(v);self.uvs.append(uv);self.faces.append(f+self.vertex_count)
        self.parts.append({'name':name,'first_triangle':self.triangle_count,'triangles':len(f)})
        self.vertex_count+=len(v);self.triangle_count+=len(f)

    def arrays(self):
        return np.vstack(self.vertices),np.vstack(self.uvs),np.vstack(self.faces)

    build_arrays=arrays


def meshbin_bytes(vertices,uvs,faces):
    """Area-weighted normals; split input vertices retain hard component edges."""
    vertices=np.asarray(vertices,dtype=np.float32);uvs=np.asarray(uvs,dtype=np.float32);faces=np.asarray(faces,dtype=np.int32)
    if not np.isfinite(vertices).all() or not np.isfinite(uvs).all():raise ValueError('Non-finite mesh data')
    if faces.min()<0 or faces.max()>=len(vertices):raise ValueError('Mesh index out of range')
    tri=vertices[faces];face_normals=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
    if np.any(np.linalg.norm(face_normals,axis=1)<=1e-9):raise ValueError('Degenerate triangle')
    normals=np.zeros_like(vertices)
    for i in range(3):np.add.at(normals,faces[:,i],face_normals)
    length=np.linalg.norm(normals,axis=1)
    if np.any(length<=1e-10):raise ValueError('Unused vertex or canceled normal')
    normals=(normals/length[:,None])[:,[0,2,1]].astype('<f4')
    positions=vertices[:,[0,2,1]].astype('<f4');indices=faces[:,[0,2,1]].astype('<i4')
    return struct.pack('<ii',len(vertices),faces.size)+positions.tobytes()+normals.tobytes()+uvs.astype('<f4').tobytes()+indices.tobytes()
