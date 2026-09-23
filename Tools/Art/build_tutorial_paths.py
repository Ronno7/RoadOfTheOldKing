"""Native village routes using the existing production palette and blob topology."""
from pathlib import Path
from PIL import Image
import csv
import itertools
import json
import random
import build_tutorial_ground as ground

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'TheLostShrine/Assets/Art/Tiles/Tutorial/Paths'
MATERIALS = {'DirtLane': 2, 'Footpath': 5}


def coverage(mask, inset):
    im = Image.new('L', (16,16))
    for y in range(16):
        for x in range(16):
            right, bottom = x >= 8, y >= 8
            u,v = (15-x if right else x),(15-y if bottom else y)
            h,vert = (2 if right else 8),(4 if bottom else 1)
            diag = (32 if bottom else 16) if right else (64 if bottom else 128)
            if mask & h and mask & vert: inside = bool(mask & diag) or u+v >= inset
            elif mask & h: inside = v >= inset
            elif mask & vert: inside = u >= inset
            else: inside = u>=inset and v>=inset and u+v>=2*inset+2
            if inside: im.putpixel((x,y),255)
    return im


def tile(material, mask, variant=0):
    alpha = coverage(mask,MATERIALS[material])
    im = Image.new('RGBA',(16,16),ground.P['earth']);im.putalpha(alpha)
    rng = random.Random(391+variant*71)
    # Sparse grains and light scuff marks, leaving the walking surface calm.
    for i in range(4 if material=='DirtLane' else 2):
        x,y=rng.randint(5,10),rng.randint(5,10)
        if alpha.getpixel((x,y)):
            im.putpixel((x,y),ground.P['sand' if i%2==0 else 'soil'])
            if i==0 and alpha.getpixel((x+1,y)):im.putpixel((x+1,y),ground.P['sand'])
    # Interrupted dark grains suggest a worn edge, without the ground kit's hard outline.
    for y in range(16):
        for x in range(16):
            if alpha.getpixel((x,y)) and (x*3+y*5)%7==0 and any(
                0<=x+dx<16 and 0<=y+dy<16 and not alpha.getpixel((x+dx,y+dy))
                for dx,dy in ground.OFFSETS[:4]):im.putpixel((x,y),ground.P['soil'])
    return im


def verify():
    checks=0
    for material,inset in MATERIALS.items():
        masks={m:coverage(m,inset) for m in ground.MASKS}
        for vertical in (False,True):
            centers=((0,0),(0,1) if vertical else (1,0))
            coords=list(itertools.product(range(-1,2 if vertical else 3),range(-1,3 if vertical else 2)))
            optional=[p for p in coords if p not in centers]
            for bits in range(1<<len(optional)):
                occupied=set(centers)|{p for i,p in enumerate(optional) if bits&(1<<i)}
                def at(p):
                    return masks[ground.normalize(sum(1<<b for b,(dx,dy) in enumerate(ground.OFFSETS) if (p[0]+dx,p[1]+dy) in occupied))]
                a,b=[at(p) for p in centers]
                for n in range(16):assert a.getpixel((n,15) if vertical else (15,n))==b.getpixel((n,0) if vertical else (0,n)),(material,bits)
                checks+=1
    return checks


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    (ROOT/'Docs/Art/Tutorial/Previews').mkdir(parents=True,exist_ok=True)
    entries=[];images=[]
    for material in MATERIALS:
        for mask,v in [(m,0) for m in ground.MASKS]+[(255,v) for v in range(1,4)]:
            name=f'{material}_{ground.shape_name(mask)}' if not v else f'{material}_Center_Variant_{v:02}'
            entries.append(dict(name=name,material=material,mask=mask,variant=v));images.append(tile(material,mask,v))
    cols=10;rows=10;pitch=20;atlas=Image.new('RGBA',(200,200))
    sheet=Image.new('RGB',(cols*64,rows*64),ground.P['grass'][:3])
    for i,(e,im) in enumerate(zip(entries,images)):
        x=i%cols*pitch+2;y=i//cols*pitch+2
        for dy in range(-2,18):
            for dx in range(-2,18):atlas.putpixel((x+dx,y+dy),im.getpixel((max(0,min(15,dx)),max(0,min(15,dy)))))
        e.update(index=i,x=x,y=200-y-16,width=16,height=16)
        sample=Image.new('RGBA',(16,16),ground.P['grass']);sample.alpha_composite(im)
        sheet.paste(sample.resize((64,64),Image.Resampling.NEAREST),(i%cols*64,i//cols*64))
    atlas.save(OUT/'TutorialPaths16.png')
    sheet.save(ROOT/'Docs/Art/Tutorial/Previews/tutorial-paths-production-atlas.png')
    (OUT/'TileManifest.json').write_text(json.dumps(dict(tileSize=16,columns=cols,padding=2,entries=entries),indent=2)+'\n')
    with (OUT/'TileIndex.csv').open('w',newline='') as f:
        w=csv.DictWriter(f,fieldnames=entries[0].keys());w.writeheader();w.writerows(entries)
    assert len(entries)==len({e['name'] for e in entries})==100
    assert {p for im in images for p in im.get_flattened_data() if p[3]}<=set(ground.P.values())
    report=dict(visual_tiles=100,new_brushes=2,reused_brushes=['Ground/Paint_Cobbles'],shapes_per_brush=47,
                seam_neighborhoods_checked=verify(),tile_size=16,atlas_size=[200,200])
    (OUT/'GenerationReport.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))


if __name__=='__main__':main()
