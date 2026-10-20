"""Shared ground materials for Tutorial and Green Lowlands."""
import ground_tile_helpers as ground
from PIL import Image, ImageDraw

import shared_palette as zone

PALETTE = dict(ground.P)
PALETTE.update(zone.COLORS)
OUTPUT = ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/Shared/Ground'
PREVIEW = ground.ROOT / 'DeveloperNotes/Previews/Shared'


def blend_margin(im, alpha, material, palette):
    """Two-pixel broken material margins; native alpha and joining ports stay crisp."""
    source = alpha.copy()
    for y in range(1, 15):
        for x in range(1, 15):
            if not source.getpixel((x, y)):
                continue
            distance = min((abs(dx)+abs(dy) for dx,dy in
                ((0,-1),(0,1),(-1,0),(1,0),(0,-2),(0,2),(-2,0),(2,0))
                if 0 <= x+dx < 16 and 0 <= y+dy < 16
                and not source.getpixel((x+dx,y+dy))), default=9)
            cluster = (x//2*3 + y//2*5) % 13
            if distance == 1 and cluster in (0,1,4):
                alpha.putpixel((x,y),0)
            elif distance == 2 and cluster == 0 and (x+y)%2 == 0:
                alpha.putpixel((x,y),0)
            elif distance == 1 and cluster in (7,8):
                key = 'basinsilt' if material == 'DampEarth' else (
                    'stoneshade' if material in ('WornStone','Cobbles') else 'pathshade')
                im.putpixel((x,y),palette[key])
    im.putalpha(alpha)
    return im


def material_tile(material, mask, variant, palette):
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
        # Keep connected ports intact while breaking the interior of a flat
        # material margin. The underlying grass/dirt supplies the transition.
        im = blend_margin(im, ground.coverage(mask), material, p)
    return im


if __name__ == '__main__':
    ground.main(OUTPUT, PREVIEW, 'Ground16',
                'shared-ground-atlas', PALETTE,
                render_tile=material_tile)
    zone.validate(Image.open(OUTPUT/'Ground16.png').convert('RGBA'))
