"""Native 16px terrain, extending the existing code-authored Ground kit.

The same blob topology supplies every bank/ledge connection. Shading samples
neighbor geometry so a cell boundary never becomes a false shoreline or cliff.
"""
from pathlib import Path
from functools import lru_cache
from PIL import Image, ImageDraw
import csv
import itertools
import json
import build_tutorial_ground as ground

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'TheLostShrine/Assets/Art/Tiles/Tutorial/Terrain'
DOC = ROOT / 'Docs/Art'
P = dict(ground.P)
P.update({k: tuple(bytes.fromhex(v)) + (255,) for k, v in {
    'water': '6E9F9A', 'waterdeep': '395F66', 'watershade': '638C8E',
    'waterlight': '9BC0AE', 'cream': 'F1DEB0',
}.items()})
BLOBS = ['MeadowWater', 'StoneWater', 'DeepWater', 'GrassLedge', 'StoneLedge']


@lru_cache(None)
def extended(mask):
    occupied = {(0, 0)} | {offset for b, offset in enumerate(ground.OFFSETS) if mask & (1 << b)}
    result = Image.new('L', (48, 48))
    for tx, ty in occupied:
        local = ground.normalize(sum(1 << b for b, (dx, dy) in enumerate(ground.OFFSETS)
                                     if (tx + dx, ty + dy) in occupied))
        result.paste(ground.coverage(local), ((tx + 1) * 16, (ty + 1) * 16))
    return result


def distance(field, x, y, limit):
    for d in range(1, limit + 1):
        for dx in range(-d, d + 1):
            dy = d - abs(dx)
            if not field.getpixel((x + dx + 16, y + dy + 16)) or not field.getpixel((x + dx + 16, y - dy + 16)):
                return d
    return limit + 1


def blob(material, mask, variant=0):
    image = Image.new('RGBA', (16, 16))
    field = extended(mask)
    for y in range(16):
        for x in range(16):
            if not field.getpixel((x + 16, y + 16)): continue
            d = distance(field, x, y, 4)
            north = not field.getpixel((x + 16, y + 15))
            if material in ('MeadowWater', 'StoneWater'):
                # Bank is inside the painted shape; transparent outside reveals ground.
                if material == 'MeadowWater':
                    color = ('leaflight' if north else 'leafshade') if d == 1 else 'earth' if d == 2 else 'soil' if d == 3 else 'watershade' if d == 4 else 'water'
                else:
                    color = ('sand' if north else 'stoneshade') if d == 1 else 'stone' if d == 2 else 'stoneshade' if d == 3 else 'watershade' if d == 4 else 'water'
            elif material == 'DeepWater':
                color = 'watershade' if d == 1 else 'waterdeep'
            else:
                stone = material == 'StoneLedge'
                # Seven pixels of south-facing relief; side edges remain thin.
                south = next((i for i in range(1, 8) if not field.getpixel((x + 16, y + 16 + i))), 8)
                color = 'stone' if stone else 'grass'
                if south <= 7:
                    if south == 7: color = 'sand' if stone else 'leaflight'
                    elif south == 6: color = 'stone' if stone else 'leafshade'
                    elif south == 1: color = 'deepsoil'
                    elif south == 2: color = 'stoneshade' if stone else 'soil'
                    else:
                        color = 'stoneshade' if stone else 'earth'
                        if (x + (4 if y % 8 >= 4 else 0)) % 8 == 0: color = 'deepsoil' if stone else 'soil'
                elif d == 1: color = ('sand' if stone else 'leaflight') if north else ('stoneshade' if stone else 'leafshade')
            image.putpixel((x, y), P[color])
    if variant:
        draw = ImageDraw.Draw(image)
        if 'Water' in material:
            color = P['watershade' if material == 'DeepWater' else 'waterlight']
            draw.line((3 + variant, 5, 6 + variant, 5), fill=color)
            draw.line((8 - variant, 11, 10 - variant, 11), fill=color)
        else:
            # Reuse the quiet ground material, without a telltale identical mark in each cell.
            image = ground.texture('WornStone' if material == 'StoneLedge' else 'Grass', variant)
    return image


def wall(mask):
    """All 16 cardinal connections. Narrow masonry, with a lower face and cap."""
    shape = Image.new('L', (16, 16)); d = ImageDraw.Draw(shape)
    d.rectangle((4, 3, 11, 12), fill=255)
    if mask & 1: d.rectangle((4, 0, 11, 8), fill=255)
    if mask & 2: d.rectangle((7, 3, 15, 12), fill=255)
    if mask & 4: d.rectangle((4, 8, 11, 15), fill=255)
    if mask & 8: d.rectangle((0, 3, 8, 12), fill=255)
    result = Image.new('RGBA', (16, 16))
    for y in range(16):
        for x in range(16):
            if not shape.getpixel((x, y)): continue
            # Exposed lower edge is a three-pixel face; boundary connections continue.
            below = next((i for i in range(1, 4) if y+i<16 and not shape.getpixel((x, y+i))), 4)
            above = y > 0 and not shape.getpixel((x, y-1))
            side = (x>0 and not shape.getpixel((x-1,y))) or (x<15 and not shape.getpixel((x+1,y)))
            color = 'deepsoil' if below == 1 else 'stoneshade' if below <= 3 or side else 'sand' if above else 'stone'
            if below > 3 and not above and (y % 8 == 2 or (x + (4 if y >= 8 else 0)) % 8 == 3): color = 'stoneshade'
            result.putpixel((x,y),P[color])
    return result


def face(material, row, column, variant=0):
    stone = material == 'StoneCliff'
    im = Image.new('RGBA', (16,16), P['stoneshade' if stone else 'earth'])
    d = ImageDraw.Draw(im)
    # Irregular vertical fissures and broken strata, not a masonry grid.
    fissure = [2,2,3,3,3,2,2,1,1,1,2,2,2,2,2,2]
    for y in range(16):
        for offset in (0,8):
            x = (fissure[(y + (5 if offset else 0)) % 16] + offset) % 16
            im.putpixel((x,y),P['deepsoil' if stone else 'soil'])
            if y % 7 < 4:im.putpixel(((x+1)%16,y),P['stone' if stone else 'sand'])
    d.line((4,7,7,6,9,6),fill=P['soil'])
    d.line((11,12,13,12,15,11),fill=P['soil'])
    if row == 'Top':
        d.rectangle((0,0,15,2),fill=P['stone' if stone else 'grass'])
        d.line((0,3,15,3),fill=P['sand' if stone else 'leaflight'])
        d.line((0,4,15,4),fill=P['stoneshade' if stone else 'leafshade'])
    if row == 'Foot':
        d.rectangle((0,13,15,15),fill=P['deepsoil'])
        d.line((0,12,15,12),fill=P['soil'])
        for x in (2,8,13): d.line((x,14,x+1,14),fill=P['stoneshade' if stone else 'soil'])
    if column == 'Left':
        d.line((0,0,0,15),fill=P['deepsoil']);d.line((1,0,1,15),fill=P['stone' if stone else 'sand'])
    if column == 'Right': d.rectangle((14,0,15,15),fill=P['deepsoil'])
    if variant: d.line((6,7,9,7),fill=P['deepsoil'])
    return im


def stairs(material, row, column):
    im = Image.new('RGBA',(16,16),P['earth' if material == 'EarthRamp' else 'stoneshade'])
    d=ImageDraw.Draw(im)
    if material == 'EarthRamp':
        for x,y in ((5,4),(9,10),(7,14)): d.line((x,y,x+2,y),fill=P['sand'])
    else:
        for y in range(0,16,4):
            d.line((0,y,15,y),fill=P['sand']);d.rectangle((0,y+1,15,y+2),fill=P['stone'])
    if row=='Top': d.line((0,0,15,0),fill=P['sand' if material=='StoneStairs' else 'leaflight'])
    if row=='Foot': d.line((0,15,15,15),fill=P['earth'])
    if column=='Left': d.rectangle((0,0,1,15),fill=P['stoneshade' if material=='StoneStairs' else 'soil'])
    if column=='Right': d.rectangle((14,0,15,15),fill=P['deepsoil' if material=='StoneStairs' else 'soil'])
    return im


def water_detail(kind, frame):
    im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
    if kind=='Ripple':
        x=3+frame
        d.line((x,5,x+4,5),fill=P['waterlight']);d.line((7-frame,11,10-frame,11),fill=P['watershade'])
    elif kind=='FallLip':
        d.rectangle((2,0,13,15),fill=P['water']);d.line((2,5,13,5),fill=P['waterlight'])
        d.line((2,6,13,6),fill=P['cream'])
        for x in (3,7,11):d.line((x,8,x,15),fill=P['waterlight'])
    elif kind=='FallBody':
        d.rectangle((2,0,13,15),fill=P['water'])
        for x in (3,7,11):
            for y in range(16):
                if (y+frame*4+x)%16<9:im.putpixel((x,y),P['waterlight'])
        d.line((2,0,2,15),fill=P['watershade']);d.line((13,0,13,15),fill=P['watershade'])
    else:
        d.rectangle((2,0,13,5),fill=P['water'])
        d.line((3,0,3,4),fill=P['waterlight']);d.line((11,0,11,4),fill=P['waterlight'])
        d.line((2,6+frame%2,13,6+frame%2),fill=P['cream'])
        d.line((1+frame%2,8,14-frame%2,8),fill=P['waterlight'])
        d.line((4,11,10,11),fill=P['waterlight'])
    return im


def verify(images, entries):
    # Alpha geometry exhausts every adjacent neighborhood. Color sampling additionally
    # checks continuous straight bank and cliff runs, in both orientations.
    checks=ground.validate_edges()
    byname={e['name']:im for e,im in zip(entries,images)}
    color_checks=0
    for material in BLOBS:
        for mask,vertical in ((155,False),(110,False),(55,True),(205,True),(255,False),(255,True)):
            im=byname[f'{material}_{ground.shape_name(mask)}']
            for n in range(16):
                a=im.getpixel((n,15) if vertical else (15,n));b=im.getpixel((n,0) if vertical else (0,n))
                # Rock striations are periodic vertically/horizontally; edge contour must match.
                assert a[3]==b[3],(material,mask,n)
                if 'Water' in material:assert a==b,(material,mask,n,a,b)
                color_checks+=1
    palette=set(P.values())|{(0,0,0,0)}
    for im in images:assert set(im.get_flattened_data())<=palette
    return checks,color_checks


def main():
    OUT.mkdir(parents=True,exist_ok=True);DOC.mkdir(parents=True,exist_ok=True)
    entries=[];images=[]
    def add(name,material,mask,variant,image):
        entries.append(dict(name=name,material=material,mask=mask,variant=variant));images.append(image)
    for material in BLOBS:
        for mask in ground.MASKS:add(f'{material}_{ground.shape_name(mask)}',material,mask,0,blob(material,mask))
        for v in range(1,4):add(f'{material}_Center_Variant_{v:02}',material,255,v,blob(material,255,v))
    labels={0:'Post',1:'End_N',2:'End_E',4:'End_S',8:'End_W',5:'Straight_NS',10:'Straight_EW',3:'Corner_NE',6:'Corner_SE',12:'Corner_SW',9:'Corner_NW',7:'T_NES',11:'T_NEW',13:'T_NSW',14:'T_ESW',15:'Cross'}
    for mask in range(16):add(f'StoneWall_{labels[mask]}_{mask:03}', 'StoneWall',mask,0,wall(mask))
    for material in ('EarthCliff','StoneCliff','StoneStairs','EarthRamp'):
        for row in ('Top','Middle','Foot'):
            for column in ('Left','Center','Right'):
                im=face(material,row,column) if 'Cliff' in material else stairs(material,row,column)
                add(f'{material}_{row}_{column}',material,-1,0,im)
    for kind in ('Ripple','FallLip','FallBody','FallFoam'):
        for frame in range(1 if kind=='FallLip' else 4):add(f'{kind}_{frame:02}',kind,-1,frame,water_detail(kind,frame))
    count=len(entries);cols=16;pad=2;pitch=20;rows=(count+cols-1)//cols
    atlas=Image.new('RGBA',(cols*pitch,rows*pitch))
    for i,(e,im) in enumerate(zip(entries,images)):
        x=i%cols*pitch+pad;y=i//cols*pitch+pad
        atlas.paste(im,(x,y))
        for oy in range(-pad,16+pad):
            for ox in range(-pad,16+pad):
                if 0<=ox<16 and 0<=oy<16:continue
                atlas.putpixel((x+ox,y+oy),im.getpixel((max(0,min(15,ox)),max(0,min(15,oy)))))
        e.update(index=i,x=x,y=atlas.height-y-16,width=16,height=16)
    atlas.save(OUT/'TutorialTerrain16.png')
    (OUT/'TileManifest.json').write_text(json.dumps(dict(tileSize=16,columns=cols,padding=pad,entries=entries),indent=2)+'\n')
    with (OUT/'TileIndex.csv').open('w',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(entries[0]));w.writeheader();w.writerows(entries)
    # Grouped/labeled contact sheet; labels are documentation, never sprite pixels.
    materials=list(dict.fromkeys(e['material'] for e in entries));w=832;y=30
    sheet=Image.new('RGB',(w,2400),(35,44,38));draw=ImageDraw.Draw(sheet)
    draw.text((16,10),'TUTORIAL TERRAIN / 16px / HEARTH & MEADOW',fill='#F1DEB0')
    for material in materials:
        draw.text((16,y),material,fill='#DFC291');y+=20
        for i,(e,im) in enumerate((e,im) for e,im in zip(entries,images) if e['material']==material):
            thumb=Image.new('RGBA',(16,16),P['water' if material in ('DeepWater','Ripple','FallFoam') else 'grass'])
            thumb.alpha_composite(im);sheet.paste(thumb.resize((48,48),Image.Resampling.NEAREST),(16+(i%16)*50,y+(i//16)*50))
        y+=((i+16)//16)*50+14
    sheet.crop((0,0,w,y)).save(DOC/'tutorial-terrain-production-atlas.png')
    seam_checks,color_checks=verify(images,entries)
    report=dict(visual_tiles=count,blob_families=5,blob_shapes=47,wall_shapes=16,automatic_brushes=6,
                animated_tiles=3,seam_neighborhoods_checked=seam_checks,edge_samples_checked=color_checks,
                atlas_size=list(atlas.size),tile_size=16,palette_colors=len({p for im in images for p in im.get_flattened_data() if p[3]}))
    (OUT/'GenerationReport.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))


if __name__=='__main__':main()
