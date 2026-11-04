"""Check exterior solid winding, closed edges, finite coordinates and reproducibility."""
import json
import sys
from pathlib import Path
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent))
from observatory_geometry import build_observatory


def validate():
    model=build_observatory(); v,uv,f=model.arrays()
    v2,uv2,f2=build_observatory().arrays()
    assert np.array_equal(v,v2) and np.array_equal(uv,uv2) and np.array_equal(f,f2), 'Nondeterministic geometry'
    assert np.isfinite(v).all() and np.isfinite(uv).all()
    assert ((uv>=0)&(uv<=1)).all()
    failures=[]
    for part in model.parts:
        start=part['first_triangle'];end=start+part['triangles']
        triangles=v[f[start:end]].astype(np.float64)
        center=triangles.reshape(-1,3).mean(axis=0)
        q=triangles-center
        volume=np.einsum('ij,ij->i',q[:,0],np.cross(q[:,1],q[:,2])).sum()/6
        _,indices=np.unique(np.round(triangles.reshape(-1,3),6),axis=0,return_inverse=True)
        ids=indices.reshape(-1,3)
        edges=np.concatenate([ids[:,[0,1]],ids[:,[1,2]],ids[:,[2,0]]])
        directed={}
        for a,b in edges:
            key=(min(a,b),max(a,b)); count,winding=directed.get(key,(0,0))
            directed[key]=(count+1,winding+(1 if a<b else -1))
        bad_edges=sum(count!=2 or winding!=0 for count,winding in directed.values())
        if volume<=1e-12 or bad_edges:
            failures.append({'part':part['name'],'volume':float(volume),'bad_edges':bad_edges})
    report={'parts':len(model.parts),'vertices':len(v),'triangles':len(f),'closed_outward_components':not failures,'failures':failures}
    print(json.dumps(report,indent=2))
    assert not failures, 'Component topology/winding failed'
    return report


if __name__=='__main__': validate()
