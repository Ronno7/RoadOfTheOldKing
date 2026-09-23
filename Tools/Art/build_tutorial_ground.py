"""Reproducible native-pixel production assets for the approved tutorial palette.

Creates exact 16px cells, transparent 47-shape overlays and a Unity slicing manifest.
No generated preview PNG is resized into gameplay art. Run from the repository root.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import csv
import itertools
import json
import random

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'TheLostShrine/Assets/Art/Tiles/Tutorial/Ground'
DOC = ROOT / 'Docs/Art'
SIZE, PAD, COLS = 16, 2, 16
P = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in {
    'grass': '8B9B5E', 'leafshade': '60724D', 'leaflight': 'BECA82',
    'deepgreen': '384B3C', 'earth': 'B58C61', 'soil': '826047',
    'deepsoil': '543F36', 'sand': 'DFC291', 'stone': 'B2AE91',
    'stoneshade': '77766A',
}.items()}
MATERIALS = ['DryGrass', 'Earth', 'DampEarth', 'WornStone', 'Cobbles', 'TilledSoil']
BASE = dict(Grass='grass', DryGrass='leaflight', Earth='earth', DampEarth='soil',
            WornStone='stone', Cobbles='stoneshade', TilledSoil='soil')
# Bits N E S W NE SE SW NW; diagonals matter only when both adjoining sides connect.
OFFSETS = [(0,-1),(1,0),(0,1),(-1,0),(1,-1),(1,1),(-1,1),(-1,-1)]
DIAGONALS = [(16,1,2),(32,2,4),(64,4,8),(128,8,1)]


def normalize(mask):
    for bit, a, b in DIAGONALS:
        if not (mask & a and mask & b): mask &= ~bit
    return mask


MASKS = sorted({normalize(i) for i in range(256)})
assert len(MASKS) == 47


def coverage(mask):
    """Corner-local blob geometry, identical at every shared occupied-cell edge."""
    image = Image.new('L', (SIZE,SIZE), 0)
    for y in range(SIZE):
        for x in range(SIZE):
            right, bottom = x >= 8, y >= 8
            u, v = (15-x if right else x), (15-y if bottom else y)
            h, vert = (2 if right else 8), (4 if bottom else 1)
            diag = (32 if bottom else 16) if right else (64 if bottom else 128)
            if mask & h and mask & vert:
                inside = bool(mask & diag) or u+v >= 3
            elif mask & h:
                inside = v >= 3
            elif mask & vert:
                inside = u >= 3
            else:
                inside = u >= 3 and v >= 3 and u+v >= 8
            if inside: image.putpixel((x,y),255)
    return image


def texture(material, variant=0):
    im = Image.new('RGBA', (16,16), P[BASE[material]])
    draw = ImageDraw.Draw(im)
    rng = random.Random(9000 + (['Grass']+MATERIALS).index(material)*71 + variant*13)
    if material == 'Cobbles':
        # Periodic staggered stonework. A two-pixel mortar frame continues across cells.
        for y in range(16):
            for x in range(16):
                ly = y % 8; lx = (x + (4 if y >= 8 else 0)) % 8
                if 1 <= lx <= 6 and 1 <= ly <= 6 and not (lx in (1,6) and ly in (1,6)):
                    color = 'sand' if ly == 1 else 'stone'
                    im.putpixel((x,y),P[color])
        # A few natural worn marks keep the designed masonry readable.
        if variant > 0:
            im.putpixel((3+variant,4),P['stoneshade'])
        return im
    if material == 'TilledSoil':
        for y in (3,7,11,15):
            draw.line((0,y,15,y),fill=P['deepsoil'])
            draw.line((0,y-1,15,y-1),fill=P['earth'])
        if variant:
            im.putpixel((4+variant,5),P['deepsoil'])
        return im
    if material == 'Grass' and variant == 0: return im
    shades = dict(Grass=('leafshade','leaflight'), DryGrass=('grass','sand'),
                  Earth=('soil','sand'), DampEarth=('deepsoil','earth'),
                  WornStone=('stoneshade','sand'))[material]
    # Keep noisy marks inside the tile; edge color is stable between fill variants.
    for i in range(3 if variant == 0 else 4):
        x, y = rng.randint(4,10), rng.randint(4,10)
        color = P[shades[i % 2]]
        draw.point((x,y),fill=color)
        if i % 2 == 0: draw.point((x+1,y),fill=color)
        if i == 2: draw.point((x+1,y-1),fill=color)
    return im


def tile(material, mask=255, variant=0):
    im = texture(material,variant)
    if mask == 255: return im
    alpha = coverage(mask)
    im.putalpha(alpha)
    # Flat one-pixel material rim follows actual exposed contours, never cell borders.
    dark = dict(DryGrass='grass', Earth='soil', DampEarth='deepsoil',
                WornStone='stoneshade', Cobbles='stoneshade', TilledSoil='deepsoil')[material]
    light = dict(DryGrass='leaflight', Earth='sand', DampEarth='earth',
                 WornStone='stone', Cobbles='stone', TilledSoil='earth')[material]
    for y in range(16):
        for x in range(16):
            if not alpha.getpixel((x,y)): continue
            exposed = [(dx,dy) for dx,dy in OFFSETS[:4]
                       if 0 <= x+dx < 16 and 0 <= y+dy < 16 and not alpha.getpixel((x+dx,y+dy))]
            if exposed: im.putpixel((x,y),P[light if (0,-1) in exposed else dark])
    return im


def shape_name(mask):
    labels={0:'Island',1:'End_N',2:'End_E',4:'End_S',8:'End_W',
            5:'Straight_NS',10:'Straight_EW',255:'Center',
            19:'Bend_NE',38:'Bend_SE',76:'Bend_SW',137:'Bend_NW',
            239:'Inner_NE',223:'Inner_SE',191:'Inner_SW',127:'Inner_NW',
            110:'Edge_N',205:'Edge_E',155:'Edge_S',55:'Edge_W'}
    return labels.get(mask,'Junction') + f'_{mask:03d}'


def validate_edges():
    # Exhaust all local arrangements around two touching occupied tiles, both axes.
    checks = 0
    for vertical in (False,True):
        coords=list(itertools.product(range(-1,2 if vertical else 3),range(-1,3 if vertical else 2)))
        centers=((0,0),(0,1) if vertical else (1,0))
        optional=[p for p in coords if p not in centers]
        for bits in range(1 << len(optional)):
            occupied=set(centers)|{p for i,p in enumerate(optional) if bits & (1<<i)}
            def mask_at(p):
                return normalize(sum(1<<i for i,(x,y) in enumerate(OFFSETS) if (p[0]+x,p[1]+y) in occupied))
            a,b=[coverage(mask_at(p)) for p in centers]
            for n in range(16):
                assert a.getpixel((n,15) if vertical else (15,n)) == b.getpixel((n,0) if vertical else (0,n))
            checks += 1
    return checks


def main():
    OUT.mkdir(parents=True,exist_ok=True); DOC.mkdir(parents=True,exist_ok=True)
    entries=[]; images=[]
    def add(name,material,mask,variant,image):
        entries.append(dict(name=name,material=material,mask=mask,variant=variant))
        images.append(image)
    for i in range(8): add(f'Grass_Fill_{i:02}', 'Grass',255,i,texture('Grass',i))
    for material in MATERIALS:
        for mask in MASKS: add(f'{material}_{shape_name(mask)}', material,mask,0,tile(material,mask))
        for i in range(1,4): add(f'{material}_Center_Variant_{i:02}',material,255,i,tile(material,255,i))
    pitch=SIZE+2*PAD; rows=(len(entries)+COLS-1)//COLS
    atlas=Image.new('RGBA',(COLS*pitch,rows*pitch),(0,0,0,0))
    for index,(entry,im) in enumerate(zip(entries,images)):
        x=(index%COLS)*pitch+PAD; top=(index//COLS)*pitch+PAD
        atlas.paste(im,(x,top))
        entry.update(index=index,x=x,y=atlas.height-top-SIZE,width=SIZE,height=SIZE)
        # Two-pixel extrusion outside each sprite rectangle for safe sampling.
        for oy in range(-PAD,SIZE+PAD):
            for ox in range(-PAD,SIZE+PAD):
                if 0<=ox<SIZE and 0<=oy<SIZE: continue
                atlas.putpixel((x+ox,top+oy),im.getpixel((max(0,min(15,ox)),max(0,min(15,oy)))))
    atlas.save(OUT/'TutorialGround16.png')
    (OUT/'TileManifest.json').write_text(json.dumps(dict(tileSize=16,columns=COLS,padding=PAD,entries=entries),indent=2)+'\n')
    with (OUT/'TileIndex.csv').open('w',newline='') as f:
        writer=csv.DictWriter(f,fieldnames=list(entries[0]));writer.writeheader();writer.writerows(entries)
    # Inspection sheet shows transparent rims over the normal meadow substrate.
    sheet=Image.new('RGB',(16*COLS,16*rows),P['grass'][:3])
    for i,im in enumerate(images): sheet.paste(im,((i%COLS)*16,(i//COLS)*16),im)
    sheet.resize((sheet.width*4,sheet.height*4),Image.Resampling.NEAREST).save(DOC/'tutorial-ground-production-atlas.png')
    assert len(entries)==308 and len({e['name'] for e in entries})==308
    pixels = list(atlas.get_flattened_data())
    assert set(pixels) <= set(P.values())|{(0,0,0,0)}|{c[:3]+(0,) for c in P.values()}
    seam_checks=validate_edges()
    report=dict(tiles=len(entries),rule_materials=7,overlay_shapes=47,materials=6,
                seam_neighborhoods_checked=seam_checks,atlas_size=list(atlas.size),tile_size=16,
                unique_opaque_colors=len({c for c in pixels if c[3]}))
    (OUT/'GenerationReport.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report))


if __name__=='__main__': main()
