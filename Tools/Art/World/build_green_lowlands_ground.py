"""Native Green Lowlands meadow, farm soil and buried fieldstone.

Run from the repository root; uses the existing native exporter without changing
Tutorial assets. Regional textures use Tutorial's clean contour geometry and
material rims; asset names stay stable for existing palette references.
"""
import build_tutorial_ground as ground
import json
import random
import re
from PIL import Image, ImageDraw

PALETTE = dict(ground.P)
PALETTE.update({k: tuple(bytes.fromhex(v)) + (255,) for k, v in {
    'grass': '7F995A', 'leafshade': '4F7050', 'leaflight': 'B5C97C',
    'deepgreen': '304C42', 'stone': 'A6AA8A', 'stoneshade': '7C8370',
    'rockshadow': '585F58',
}.items()})
OUTPUT = ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/GreenLowlands/Ground'
PREVIEW = ground.ROOT / 'Docs/Art/GreenLowlands'


def lowlands_tile(material, mask, variant, palette):
    p = palette
    base = {'Grass':'grass','DryGrass':'leaflight','Earth':'earth',
            'DampEarth':'soil','WornStone':'stoneshade','Cobbles':'rockshadow',
            'TilledSoil':'soil'}[material]
    im = Image.new('RGBA',(16,16),p[base]); d = ImageDraw.Draw(im)
    rng = random.Random(1400 + variant*137 + (['Grass']+ground.MATERIALS).index(material)*49)

    def patch(points, color):
        d.polygon(points,fill=p[color])

    if material == 'Grass':
        if variant:
            # Compact blade groups with definite base/shade/light separation.
            x,y = rng.randint(2,6),rng.randint(4,8)
            if variant in (1,4,7):
                d.line((x,y+2,x+4,y+2),fill=p['leafshade'])
                d.line((x+1,y+1,x+1,y-1),fill=p['leafshade'])
                d.line((x+3,y+1,x+4,y),fill=p['leafshade'])
            elif variant in (2,5):
                d.line((x,y+1,x+3,y+1),fill=p['leaflight'])
                d.point((x+3,y),fill=p['leaflight'])
            else:
                d.line((x,y+1,x+3,y+1),fill=p['leafshade'])
                d.line((x+2,y,x+2,y-1),fill=p['leafshade'])
            if variant in (1,7):
                d.point((x+2,y+1),fill=p['leaflight'])
            if variant >= 5:
                d.line((11,12,13,12),fill=p['leafshade'])
    elif material == 'DryGrass':
        if variant != 2:
            x,y=2+variant,4+variant
            d.line((x,y,x+3,y),fill=p['grass'])
            d.point((x+3,y-1),fill=p['grass'])
        if variant in (1,3):
            d.line((9,12,13,11),fill=p['grass'])
            d.line((4,10,6,9),fill=p['sand'])
    elif material == 'Earth':
        # Compact rounded marks with varied positions. Avoid stair-step pairs
        # and repeated diagonal crescents, which read as directional hatching.
        def stamp(x,y,rows):
            colors = {'s':'soil','h':'sand','r':'stone'}
            for dy,row in enumerate(rows):
                for dx,c in enumerate(row):
                    if c != '.': im.putpixel((x+dx,y+dy),p[colors[c]])
        if variant == 0:
            stamp(4,6,('h',))
            stamp(12,5,('s',))
            stamp(8,12,('ss',))
        elif variant == 1:
            stamp(9,8,('hh','rr','ss'))
            stamp(3,4,('s',))
        elif variant == 2:
            stamp(4,11,('.h.','sss'))
            stamp(11,3,('h',))
            stamp(12,11,('s',))
        else:
            stamp(3,6,('s.s','sss'))
            stamp(12,10,('h',))
            stamp(9,3,('s',))
    elif material == 'DampEarth':
        x,y = 2+variant,4+variant
        patch([(x,y),(x+4,y-1),(x+7,y),(x+5,y+2),(x+1,y+2)],'deepsoil')
        if variant in (1,3):
            d.line((3,11,6,11),fill=p['earth'])
        if variant == 2:
            patch([(9,11),(12,10),(14,11),(13,12)],'deepsoil')
    elif material == 'WornStone':
        # Broad embedded slabs, sparse recesses and grass in the broken joins.
        patch([(2,2),(10,1),(13,3),(12,8),(8,9),(2,7)],'stone')
        d.line((3,2,9,2),fill=p['sand'])
        patch([(5,12),(9,10),(14,11),(13,14),(6,14)],'stone')
        d.line((1,10,3,10),fill=p['grass'])
        if variant: d.line((8,4,8+variant,6),fill=p['stoneshade'])
    elif material == 'Cobbles':
        # Uneven fieldstones, larger and less regular than the village's masonry.
        patch([(1,2),(5,1),(8,3),(7,7),(2,8),(1,6)],'stoneshade')
        patch([(9,1),(14,1),(15,4),(13,7),(10,6)],'stone')
        patch([(1,10),(6,9),(8,11),(6,15),(2,14)],'stone')
        patch([(10,9),(14,8),(15,12),(13,15),(9,14),(9,11)],'stoneshade')
        d.line((2,2,5,2),fill=p['stone'])
        d.line((10,10,13,9),fill=p['stone'])
        if variant: patch([(6,8),(9,7),(10,8),(8,10)],'grass')
    elif material == 'TilledSoil':
        # Broad clean ridges, two furrows per native tile (Tutorial has four).
        for y in (3,11):
            d.line((0,y,15,y),fill=p['deepsoil'],width=2)
            d.line((0,y-2,15,y-2),fill=p['earth'])
    if material != 'Grass' and mask != 255:
        alpha = ground.coverage(mask)
        im.putalpha(alpha)
        # Match Tutorial's uninterrupted, one-native-pixel material rim.
        # Only actual exposed contours receive a rim; shared tile edges do not.
        dark = dict(DryGrass='grass',Earth='soil',DampEarth='deepsoil',
                    WornStone='stoneshade',Cobbles='rockshadow',TilledSoil='deepsoil')[material]
        light = dict(DryGrass='leaflight',Earth='sand',DampEarth='earth',
                     WornStone='stone',Cobbles='stone',TilledSoil='earth')[material]
        for y in range(16):
            for x in range(16):
                if not alpha.getpixel((x,y)): continue
                exposed = [(dx,dy) for dx,dy in ground.OFFSETS[:4]
                           if 0 <= x+dx < 16 and 0 <= y+dy < 16
                           and not alpha.getpixel((x+dx,y+dy))]
                if exposed:
                    im.putpixel((x,y),p[light if (0,-1) in exposed else dark])
    return im


def review_sheet():
    """Compose the actual exported tiles with unchanged Tutorial/hero sprites."""
    assets = ground.ROOT / 'RoadOfTheOldKing/Assets'
    env_path = assets / 'Art/Tiles/Tutorial/Environment/TutorialEnvironment16.png'
    env = Image.open(env_path).convert('RGBA')
    meta = env_path.with_suffix('.png.meta').read_text()

    def prop(name):
        match = re.search(r'      name: ' + re.escape(name) +
                          r'\n      rect:\n        serializedVersion: \d+\n'
                          r'        x: (\d+)\n        y: (\d+)\n'
                          r'        width: (\d+)\n        height: (\d+)', meta)
        x, y, w, h = map(int, match.groups())
        return env.crop((x, env.height-y-h, x+w, env.height-y))

    def compose(folder, atlas_name, landscape=False):
        atlas = Image.open(folder / atlas_name).convert('RGBA')
        entries = json.loads((folder / 'TileManifest.json').read_text())['entries']
        tiles = {(e['material'], e['mask'], e['variant']): atlas.crop(
            (e['x'], atlas.height-e['y']-16, e['x']+16, atlas.height-e['y'])) for e in entries}
        im = Image.new('RGBA', (320 if landscape else 256, 192))
        rng = random.Random(507)
        for y in range(12):
            for x in range(im.width//16):
                quiet_weight = 8 if folder == OUTPUT else 12
                variant = rng.choice([0]*quiet_weight + list(range(1, 8)))
                im.alpha_composite(tiles['Grass', 255, variant], (x*16, y*16))

        patches = {
            'DryGrass': {(x,y) for x in range(2,6) for y in range(3,6)} - {(2,3),(5,5)},
            'Earth': {(x,y) for x in range(6,11) for y in range(3,12)} |
                     {(x,y) for x in range(10,16) for y in range(5,12)} |
                     {(5,y) for y in range(6,9)},
            'DampEarth': {(x,y) for x in range(1,5) for y in range(8,11)} - {(1,8),(4,10)},
            'WornStone': {(x,y) for x in range(12,15) for y in range(2,4)} - {(14,2)},
            'Cobbles': {(x,y) for x in range(7,10) for y in range(2,5)},
            'TilledSoil': {(x,y) for x in (11,12,14) for y in range(7,11)},
        }
        if landscape:
            def oval(cx,cy,rx,ry):
                return {(x,y) for x in range(20) for y in range(12)
                        if ((x-cx)/rx)**2+((y-cy)/ry)**2 <= 1}
            patches = {
                'DryGrass': oval(4,4,3.5,2.2) | {(8,1),(9,1),(10,1),(9,2),(10,2),(11,2)} | oval(7,10,2,1.5),
                'Earth': oval(15,6,5,3) | {(x,y) for y in range(7,12)
                         for x in range(18-y,21-y)},
                'DampEarth': oval(2,10,2.5,1.2),
                'WornStone': {(12,2),(13,2),(13,3),(14,3)},
                'Cobbles': {(11,5),(12,5),(12,6)},
                'TilledSoil': {(x,y) for x in (15,16,18) for y in range(5,9)},
            }
        for material, cells in patches.items():
            for x,y in sorted(cells):
                mask = ground.normalize(sum(1 << i for i,(dx,dy) in enumerate(ground.OFFSETS)
                                            if (x+dx,y+dy) in cells))
                variant = rng.randrange(4) if mask == 255 else 0
                im.alpha_composite(tiles[material, mask, variant], (x*16,y*16))
        props = [('Oak',(1,0)), ('Birch',(48,0)),
                 ('Fence_Horizontal',(170,67)), ('Fence_Horizontal',(208,67)),
                 ('Boulder',(9,87))]
        if landscape:
            props = [('Oak',(0,0)),('Birch',(42,2)),('Birch',(258,0)),
                     ('Fence_Horizontal',(228,52)),('Fence_Horizontal',(266,52)),
                     ('Boulder',(58,115)),('Oak',(0,147))]
        for name, xy in props:
            im.alpha_composite(prop(name), xy)
        hero = Image.open(assets / 'Art/Sprites/Player/Rotations/south.png').convert('RGBA')
        im.alpha_composite(hero,(153,108) if landscape else (113,102))
        return im.convert('RGB'), tiles

    tutorial, _ = compose(assets / 'Art/Tiles/Tutorial/Ground', 'TutorialGround16.png')
    lowlands, tiles = compose(OUTPUT, 'GreenLowlandsGround16.png')
    # Labels sit outside the native art; scene panels are exact 3x enlargements.
    sheet = Image.new('RGB',(532,252),'#252823')
    draw = ImageDraw.Draw(sheet)
    draw.text((6,4),'TUTORIAL - reference',fill='#F1DEB0')
    draw.text((270,4),'GREEN LOWLANDS - Ground',fill='#F1DEB0')
    sheet.paste(tutorial,(6,20)); sheet.paste(lowlands,(270,20))
    for i, material in enumerate(['Grass']+ground.MATERIALS):
        x = 6+i*75
        sheet.paste(tiles[material,255,0].convert('RGB'),(x,220))
        draw.text((x,239),material,fill='#F1DEB0')
    sheet.resize((1596,756),Image.Resampling.NEAREST).save(PREVIEW/'green-lowlands-ground-comparison.png')
    landscape, _ = compose(OUTPUT, 'GreenLowlandsGround16.png', landscape=True)
    landscape.resize((1280,768),Image.Resampling.NEAREST).save(PREVIEW/'green-lowlands-ground-detail.png')


if __name__ == '__main__':
    ground.main(OUTPUT, PREVIEW, 'GreenLowlandsGround16',
                'green-lowlands-ground-atlas', PALETTE,
                render_tile=lowlands_tile)
    review_sheet()
