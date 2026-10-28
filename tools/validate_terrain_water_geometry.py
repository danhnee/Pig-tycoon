"""Validate canonical terrain height and actual shoreline contact, not polygon counts."""
import json
import numpy as np
from terrain_water_geometry import build_terrain,build_water
from map_surface_contract import sample_ground_z
from map_mesh import meshbin_bytes


def validate():
    report={}
    for name,builder in [('terrain',build_terrain),('water',build_water)]:
        model=builder();v,uv,f=model.arrays()
        assert np.isfinite(v).all() and np.isfinite(uv).all()
        assert f.min()>=0 and f.max()<len(v)
        assert ((uv>=0)&(uv<=1)).all()
        area=np.linalg.norm(np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]]),axis=1)
        assert area.min()>1e-9,(name,'degenerate triangle')
        encoded=meshbin_bytes(v,uv,f)
        contacts=[]
        for part in model.parts:
            ids=np.unique(f[part['first_triangle']:part['first_triangle']+part['triangles']])
            if part['name']=='canonical_heightfield':
                assert np.array_equal(v[ids,2],sample_ground_z(v[ids,0],v[ids,1]))
            if part['name'].endswith('_surface'):
                delta=v[ids,2]-sample_ground_z(v[ids,0],v[ids,1])
                contacts.append({'part':part['name'],'minimum_depth':float(delta.min())})
                assert delta.min()>-.12,(part['name'],'water penetrates bank by more than 12 cm',float(delta.min()))
        report[name]={'vertices':len(v),'triangles':len(f),'parts':len(model.parts),'minimum_double_area':float(area.min()),'binary_bytes':len(encoded),'surface_contact':contacts}
    print(json.dumps(report,indent=2))
    return report


if __name__=='__main__':validate()
