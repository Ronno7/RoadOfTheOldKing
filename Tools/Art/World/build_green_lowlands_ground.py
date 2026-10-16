"""Native Green Lowlands ground using the approved presentation-study ramps.

Run from the repository root; uses the existing native exporter without changing
Tutorial assets. Quiet meadow/path motifs retain the original masks and stable
asset names. Material boundaries are flat transitions, not miniature ledges.
"""
import build_tutorial_ground as ground
import json
import random
import re
from PIL import Image, ImageDraw

import lowlands_palette as zone

PALETTE = dict(ground.P)
PALETTE.update(zone.COLORS)
OUTPUT = ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/GreenLowlands/Ground'
PREVIEW = ground.ROOT / 'Docs/Art/GreenLowlands'


def lowlands_tile(material, mask, variant, palette):
    p = palette
    base = {'Grass':'grass','DryGrass':'leaflight','Earth':'earth',
            'DampEarth':'basinmud','WornStone':'stoneshade','Cobbles':'stonejoint',
            'TilledSoil':'soil'}[material]
    im = Image.new('RGBA',(16,16),p[base]); d = ImageDraw.Draw(im)

    def patch(points, color):
        d.polygon(points,fill=p[color])

    if material == 'Grass':
        # Broad single-value patches and a few grounded tufts; variants zero
        # two and six stay empty to preserve resting space under physical plants.
        patches = {
            1: [(1,8),(3,8),(3,6),(6,6),(6,4),(12,4),(12,6),
                (14,6),(14,10),(10,10),(10,12),(4,12),(4,11),(1,11)],
            3: [(1,11),(4,11),(4,9),(10,9),(10,10),(13,10),
                (13,12),(11,12),(11,14),(4,14),(4,13),(1,13)],
        }
        if variant in patches:
            patch(patches[variant], 'grasspatch')
        elif variant in (4,7):
            x,y = (3,5) if variant == 4 else (8,9)
            d.rectangle((x,y+2,x+5,y+3),fill=p['leafshade'])
            d.line((x+1,y-1,x+1,y+2),fill=p['leaflight'])
            d.line((x+4,y,x+4,y+2),fill=p['leaflight'])
        elif variant == 5:
            d.line((4,11,7,11),fill=p['leaflight'])
            d.point((8,10),fill=p['leaflight'])
    elif material == 'DryGrass':
        # A gentle vegetation-color change, not a bright sand-colored carpet.
        if variant in (1,3):
            x,y = (3,8) if variant == 1 else (8,5)
            d.line((x,y,x+3,y),fill=p['drygrass'])
            d.point((x+4,y-1),fill=p['drygrass'])
        elif variant == 2:
            patch([(3,10),(3,8),(7,8),(7,7),(12,7),(12,10),(9,10),(9,11),(5,11)],
                  'grasspatch')
    elif material == 'Earth':
        # Leave common fill and boundaries empty. Connected, low-contrast worn
        # patches carry the texture; tiny paired chips add grit without making
        # the route a field of dots or borrowing a raised bank's dark outline.
        if variant == 1:
            patch([(2,8),(4,8),(4,7),(8,7),(8,8),(10,8),
                   (10,10),(8,10),(8,11),(3,11),(3,10),(2,10)], 'pathworn')
            d.line((4,9,6,9),fill=p['sand'])
            d.point((7,8),fill=p['sand'])
            d.line((11,4,12,4),fill=p['pathshade'])
        elif variant == 2:
            patch([(8,3),(11,3),(11,4),(13,4),(13,6),
                   (11,6),(11,7),(8,7),(8,6),(7,6),(7,4),(8,4)], 'pathworn')
            d.line((10,4,12,4),fill=p['sand'])
            d.line((9,7,11,7),fill=p['pathshade'])
            d.line((3,11,4,11),fill=p['pathshade'])
            d.point((4,10),fill=p['sand'])
        elif variant == 3:
            patch([(5,10),(7,10),(7,9),(10,9),(10,10),(12,10),
                   (12,12),(10,12),(10,13),(6,13),(6,12),(5,12)], 'pathworn')
            d.line((7,11,9,11),fill=p['sand'])
            d.line((7,13,8,13),fill=p['pathshade'])
            patch([(3,3),(5,3),(5,4),(6,4),(6,5),(3,5)],'pathworn')
    elif material == 'DampEarth':
        # Quiet cool sediment distinguishes the exposed bed from dry roads.
        # Connected, low-contrast stains replace the repeated dark gouges.
        if variant in (1,3):
            x,y = (3,8) if variant == 1 else (8,5)
            patch([(x,y),(x+2,y-1),(x+5,y-1),(x+6,y),(x+5,y+2),
                   (x+1,y+2)],'basinpatch')
        if variant == 2:
            d.line((4,10,8,10),fill=p['basinsilt'])
            d.line((7,11,10,11),fill=p['basinsilt'])
    elif material == 'WornStone':
        # Broad embedded slabs; texture stays quieter than upright cliff faces.
        patch([(2,2),(10,1),(13,3),(12,8),(8,9),(2,7)],'stone')
        patch([(5,12),(9,10),(14,11),(13,14),(6,14)],'stone')
        if variant == 1:
            d.line((1,10,3,10),fill=p['grass'])
        elif variant == 2:
            d.line((8,4,10,5),fill=p['stoneshade'])
        elif variant == 3:
            d.line((3,3,6,3),fill=p['stonelight'])
    elif material == 'Cobbles':
        # Recognizable laid fieldstone, with softer joins than structural walls.
        patch([(1,2),(5,1),(8,3),(7,7),(2,8),(1,6)],'stoneshade')
        patch([(9,1),(14,1),(15,4),(13,7),(10,6)],'stone')
        patch([(1,10),(6,9),(8,11),(6,15),(2,14)],'stone')
        patch([(10,9),(14,8),(15,12),(13,15),(9,14),(9,11)],'stoneshade')
        if variant == 1: d.line((2,2,5,2),fill=p['stone'])
        if variant == 2: d.line((10,10,12,10),fill=p['stone'])
        if variant == 3: patch([(6,8),(9,7),(10,8),(8,10)],'grass')
    elif material == 'TilledSoil':
        # Broad clean ridges, two furrows per native tile (Tutorial has four).
        for y in (3,11):
            d.line((0,y,15,y),fill=p['deepsoil'])
            d.line((0,y-1,15,y-1),fill=p['soilmid'])
    if material != 'Grass' and mask != 255:
        # Preserve every authored 47-shape footprint exactly. No lit upper rim
        # or dark lower bevel: only dedicated terrain art communicates height.
        im.putalpha(ground.coverage(mask))
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
    zone.validate(Image.open(OUTPUT/'GreenLowlandsGround16.png').convert('RGBA'))
    review_sheet()
