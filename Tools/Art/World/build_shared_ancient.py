"""Native, reusable flat remnants; original four-spoke motif from the sluice crest.

Authoritative pixel recipe for this small shared atlas. No scene construction,
collision, palette generation, or per-prop menu commands.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw
import shared_palette as palette

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/Shared/DetailDecoration/Ancient'
C = palette.COLORS


def slab():
    im = Image.new('RGBA', (32, 16))
    d = ImageDraw.Draw(im)
    # Low, partially buried masonry: no black upright-object silhouette.
    d.polygon([(3,5),(8,3),(20,3),(22,4),(27,4),(29,6),
               (28,11),(25,13),(9,13),(8,12),(3,12),(2,9)], fill=C['stonejoint'])
    d.polygon([(4,5),(9,4),(20,4),(21,5),(27,5),(27,10),
               (24,11),(8,11),(7,10),(3,10)], fill=C['stoneshade'])
    d.polygon([(5,5),(9,4),(19,4),(18,6),(11,6),(9,8),(4,8)], fill=C['stone'])
    # Broken four-spoke sun-wheel, kept subdued instead of pickup gold.
    d.line([(14,5),(18,5),(20,7),(20,9),(18,11),(14,11),(12,9),(12,7),(14,5)],
           fill=C['stonejoint'])
    d.line((16,5,16,11), fill=C['stonejoint'])
    d.line((12,8,20,8), fill=C['stonejoint'])
    d.point((17,6), fill=C['stone'])
    d.line([(22,5),(21,7),(23,8),(22,10)], fill=C['stonejoint'])
    # Missing lower edge opens onto the real ground below, not a painted grass tint.
    d.polygon([(5,12),(8,10),(10,11),(10,15),(5,15)], fill=(0,0,0,0))
    d.point((18,11), fill=C['stoneshade'])
    return im


def paving(broken=False):
    im = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(im)
    outline = ([(3,2),(12,2),(14,4),(12,6),(13,10),(10,13),(3,12),(1,9)]
               if broken else [(3,2),(12,3),(14,6),(13,11),(11,13),(3,12),(2,9)])
    d.polygon(outline, fill=C['stoneshade'])
    d.polygon([(4,3),(11,4),(12,6),(11,10),(8,11),(3,10)], fill=C['stone'])
    d.line((5,4,10,4), fill=C['stonelight'])
    if broken:
        d.polygon([(10,2),(9,5),(11,7),(9,9),(10,13),(13,15),(16,15),(16,0)],
                  fill=(0,0,0,0))
    else:
        d.line([(5,7),(5,9),(8,9)], fill=C['stoneshade'])
    return im


def inlay():
    im = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(im)
    # A broken service-channel fitting, flat and unlit.
    d.polygon([(3,3),(11,2),(13,5),(12,11),(9,13),(3,11)], fill=C['stoneshade'])
    d.polygon([(4,4),(10,3),(11,5),(10,11),(4,10)], fill=C['stone'])
    d.line([(6,3),(6,8),(8,10),(8,12)], fill=C['soil'])
    d.line([(7,3),(7,7),(9,9),(9,11)], fill=C['soillight'])
    d.line((6,6,7,6), fill=C['stoneshade'])
    d.point((8,9), fill=C['stoneshade'])
    return im


def main():
    pieces = [('Sunwheel_Slab', slab()), ('Ancient_Paving', paving()),
              ('Ancient_Paving_Broken', paving(True)), ('Bronze_Channel_Fragment', inlay())]
    atlas = Image.new('RGBA', (80, 16)); entries = []; x = 0
    for name, im in pieces:
        assert palette.validate(im) <= 6
        assert set(im.getchannel('A').get_flattened_data()) <= {0,255}
        atlas.alpha_composite(im, (x,0))
        entries.append(dict(name=name,x=x,y=0,width=im.width,height=16))
        if im.width > 16:
            for cell in range(im.width//16):
                entries.append(dict(name=f'{name}_{cell}',x=x+cell*16,y=0,width=16,height=16))
        x += im.width
    OUT.mkdir(parents=True,exist_ok=True)
    atlas.save(OUT/'Ancient16.png')
    (OUT/'AncientManifest.json').write_text(json.dumps(dict(entries=entries),indent=2)+'\n')
    print(f'{len(pieces)} shared flat pieces, {len(entries)} sprite registrations; {palette.validate(atlas)} shared colors.')


if __name__ == '__main__':
    main()
