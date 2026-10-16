"""Reproduce the foliage source atlas; import only the retained Bramble B.

Historical wall pixels remain stable in the atlas, but their unused tile assets
and slices are retired. Whole small bushes use --small-bush-only.
"""
from pathlib import Path
import json
import random
import sys
from PIL import Image, ImageDraw
import lowlands_palette as zone

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/GreenLowlands/Thickets'
COLORS = zone.ramp('ink', 'leafshade', 'grass', 'grasspatch', 'leaflight')


def motif():
    image = Image.new('RGBA', (64, 64), COLORS[2])
    draw = ImageDraw.Draw(image)
    rng=random.Random(111026)
    # Broad attached leaf planes, with shallow steps instead of sawtooth tips.
    # Values share the meadow ramp; quiet gaps connect unevenly sized clusters.
    sprays=[(x+rng.randrange(-3,4),y+rng.randrange(-3,4),
             rng.randrange(12,22),rng.randrange(6,10),rng.randrange(4))
            for y in range(-2,64,14) for x in range(-3,64,19)]
    for ox in (-64,0,64):
        for oy in (-64,0,64):
            for sx,sy,w,h,v in sprays:
                x,y=sx+ox,sy+oy
                shape=[(x,y+3),(x+3,y+3),(x+3,y+1),(x+8,y+1),
                       (x+8,y),(x+w-5,y),(x+w-5,y+2),(x+w-2,y+2),
                       (x+w-2,y+4),(x+w,y+4),(x+w,y+h-2),
                       (x+w-4,y+h-2),(x+w-4,y+h),(x+4,y+h),
                       (x+4,y+h-1),(x,y+h-1)]
                draw.polygon([(a,b+2) for a,b in shape],fill=COLORS[1])
                draw.polygon(shape,fill=COLORS[3] if v!=2 else COLORS[2])
                # Small connected highlights, never a bright cap on every mass.
                if v==0:
                    draw.polygon([(x+3,y+3),(x+6,y+3),(x+6,y+1),(x+11,y+1),
                                  (x+11,y+3),(x+8,y+3),(x+8,y+4),(x+3,y+4)],fill=COLORS[4])
                if v==1:draw.rectangle((x+w-7,y+h-2,x+w-3,y+h-1),fill=COLORS[2])
    return image


def canonical(mask):
    """N/E/S/W, followed by NE/SE/SW/NW; impossible diagonals are removed."""
    for bit,a,b in [(16,1,2),(32,2,4),(64,4,8),(128,8,1)]:
        if not (mask&a and mask&b):mask &= ~bit
    return mask


def bramble(variant):
    """Complete uneven leaf spray, keeping its original three-cell footing."""
    image=motif().crop((variant*9,0,48+variant*9,32))
    alpha=Image.new('L',(48,32));d=ImageDraw.Draw(alpha)
    points=[(1,24),(2,20),(5,20),(5,16),(8,16),(8,12),(12,12),
            (12,9),(17,9),(17,6),(24,6),(24,4),(29,4),(29,7),
            (34,7),(34,11),(39,11),(39,15),(42,15),(42,19),
            (45,19),(45,24),(46,24),(46,27),(41,27),(41,29),
            (33,29),(33,30),(19,30),(19,29),(9,29),(9,28),(2,28)]
    if variant:points=[(47-x,y+min(2,max(0,(x-20)//10))) for x,y in points]
    d.polygon(points,fill=255);image.putalpha(alpha)
    for y in range(32):
        for x in range(48):
            if not alpha.getpixel((x,y)):image.putpixel((x,y),(0,0,0,0));continue
            if any(not (0<=x+dx<48 and 0<=y+dy<32) or not alpha.getpixel((x+dx,y+dy))
                   for dx,dy in [(0,1),(0,-1),(1,0),(-1,0)]):image.putpixel((x,y),COLORS[0])
            elif y>=28:image.putpixel((x,y),COLORS[1])
    return image


def tile(pattern, mask, phase, blob=False):
    px, py = phase % 4, phase // 4
    result = pattern.crop((px*16,py*16,px*16+16,py*16+16))
    north,east,south,west = [bool(mask & b) for b in (1,2,4,8)]
    ne,se,sw,nw = [bool(mask&b) if blob else a and c for b,a,c in
                   [(16,north,east),(32,east,south),(64,south,west),(128,west,north)]]
    occupied={(0,0):True,(0,-1):north,(1,0):east,(0,1):south,(-1,0):west,
              (1,-1):ne,(1,1):se,(-1,1):sw,(-1,-1):nw}
    alpha=Image.new('L',(16,16),255)
    # Small attached leaf tips break the silhouette. Collision has its own
    # authored inset; the image does not pretend to define a hard square wall.
    for y in range(16):
        for x in range(16):
            outside=False
            scallop=[2,2,2,1,1,1,1,2,2,3,3,3,2,2,2,2,
                     2,2,1,1,1,1,1,2,2,2,3,3,2,2,2,2,
                     2,2,3,3,3,2,2,1,1,1,1,1,2,2,2,2,
                     2,2,2,3,3,2,2,2,1,1,1,1,1,2,2,2]
            hx=scallop[(x+px*16)%64]
            vy=scallop[(y+py*16+9)%64]
            outside |= (not north and y<hx)
            outside |= (not south and y>=16-hx)
            outside |= (not west and x<vy)
            outside |= (not east and x>=16-vy)
            for a,b,xx,yy in [(north,west,x,y),(north,east,15-x,y),
                               (south,west,x,15-y),(south,east,15-x,15-y)]:
                if not a and not b and xx<4 and yy<4:
                    outside |= (xx-3.5)**2+(yy-3.5)**2>16
            # A restrained one-pixel leaf notch breaks straight runs without
            # cutting off the canopy or leaving large empty collision margins.
            outside |= (not north and y==0 and (x+px*16)%13==4)
            outside |= (not south and y==15 and (x+px*16)%11==7)
            outside |= (not west and x==0 and (y+py*16)%17==5)
            outside |= (not east and x==15 and (y+py*16)%19==11)
            for a,b,diagonal,xx,yy in [(north,east,ne,15-x,y),(east,south,se,15-x,15-y),
                                     (south,west,sw,x,15-y),(west,north,nw,x,y)]:
                if a and b and not diagonal and xx+yy<2:outside=True
            if outside:
                alpha.putpixel((x,y),0)
    def inside(xx,yy):
        if 0<=xx<16 and 0<=yy<16:return alpha.getpixel((xx,yy))!=0
        return occupied.get((xx//16,yy//16),False)
    for y in range(16):
        for x in range(16):
            if not alpha.getpixel((x,y)):
                result.putpixel((x,y),(0,0,0,0));continue
            ds=next((d for d in range(1,7) if not inside(x,y+d)),7)
            de=next((d for d in range(1,4) if not inside(x+d,y)),4)
            dn=next((d for d in range(1,5) if not inside(x,y-d)),5)
            dw=next((d for d in range(1,3) if not inside(x-d,y)),3)
            if min(ds,de,dn,dw)==1:color=COLORS[0]
            elif ds<=3:color=COLORS[1]
            elif dn==2 and (x+px*16)%7<4:color=COLORS[4]
            elif dw==2 and (y+py*16)%9<5:color=COLORS[3]
            else:continue
            result.putpixel((x,y),color)
    return result


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    pattern=motif()
    atlas=Image.new('RGBA',(512,512))
    entries=[]
    for mask in range(16):
        for phase in range(16):
            x,y=phase*16,mask*16
            atlas.alpha_composite(tile(pattern,mask,phase),(x,256+y))
            entries.append(dict(name=f'Thicket_{mask}_{phase}',material='Thicket',
                                mask=mask,variant=phase,index=len(entries),
                                x=x,y=256-y-16,width=16,height=16))
    masks=sorted({canonical(m) for m in range(256)})
    assert len(masks)==47
    slots=[(x,y) for y in range(0,512,16) for x in range(0,512,16) if x>=256 or y>=256]
    for mask in masks:
        for phase in range(16):
            x,y=slots.pop(0)
            atlas.alpha_composite(tile(pattern,mask,phase,True),(x,512-y-16))
            entries.append(dict(name=f'ThicketBlob_{mask}_{phase}',material='Thicket',
                                mask=mask,variant=phase,index=len(entries),
                                x=x,y=y,width=16,height=16))
    for variant in range(2):
        shrub=bramble(variant)
        for row in range(2):
            for col in range(3):
                x,y=slots.pop(0)
                piece=shrub.crop((col*16,16-row*16,col*16+16,32-row*16))
                atlas.alpha_composite(piece,(x,512-y-16))
                entries.append(dict(name=f'Bramble_{"AB"[variant]}_{col}_{row}',material='Bramble',
                                    mask=0,variant=variant,index=len(entries),
                                    x=x,y=y,width=16,height=16))
    atlas.save(OUT/'LowlandsThickets16.png')
    retained = [entry for entry in entries if entry['name'].startswith('Bramble_B_')]
    (OUT/'ThicketManifest.json').write_text(json.dumps({'entries':retained},indent=2)+'\n')
    zone.validate(atlas)
    colors=set(atlas.get_flattened_data())- {(0,0,0,0)}
    assert colors == set(COLORS)
    # Adjacent fully connected phases recover the one seamless periodic motif.
    rebuilt=Image.new('RGBA',(64,64))
    for phase in range(16):rebuilt.alpha_composite(tile(pattern,15,phase),((phase%4)*16,(phase//4)*16))
    assert rebuilt.tobytes()==pattern.tobytes()
    print(f'Preserved source atlas; exported {len(retained)} retained Bramble B tiles; motif seams verified.')


def small_bush():
    """Prepare the generated whole shrub at native game scale, not wall tiles."""
    source = Image.open(ROOT/'ArtSource/GreenLowlands/SmallBush_Source.png').convert('RGBA')
    alpha = source.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError('Small bush source is empty')
    cropped = source.crop(bounds)
    width = 24
    height = round(cropped.height*width/cropped.width)
    sampled = cropped.resize((width,height), Image.Resampling.NEAREST)
    colors = zone.ramp('ink','deepgreen','leafshade','grass','leaflight','deepsoil')
    native = Image.new('RGBA',(26,height+2))
    for y in range(height):
        for x in range(width):
            pixel = sampled.getpixel((x,y))
            if pixel[3] < 128:
                continue
            color = min(colors,key=lambda c:sum((pixel[k]-c[k])**2 for k in range(3)))
            native.putpixel((x+1,y+1),color)
    zone.validate(native)
    folder = ROOT/'RoadOfTheOldKing/Assets/Art/Sprites/World/LowlandsVegetation'
    folder.mkdir(parents=True,exist_ok=True)
    native.save(folder/'SmallSageBush.png')
    shadow = Image.new('RGBA',(24,6))
    ImageDraw.Draw(shadow).ellipse((2,1,21,4),fill=zone.COLORS['ink'])
    shadow.save(folder/'SmallBushContact.png')
    print(f'Prepared whole bush {native.size}, six shared colors; separate native contact shadow.')


if __name__=='__main__':
    if '--small-bush-only' in sys.argv: small_bush()
    else: main()
