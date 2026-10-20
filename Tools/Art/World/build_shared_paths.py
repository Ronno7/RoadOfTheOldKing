"""Shared dirt routes with native path masks and connections."""
from PIL import Image, ImageDraw
import ground_tile_helpers as ground
import path_tile_helpers as paths
import build_shared_ground as shared_ground

OUTPUT = ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/Shared/Paths'
PREVIEW = shared_ground.PREVIEW


def tile(material, mask, variant=0, palette=None):
    p = palette or shared_ground.PALETTE
    alpha = paths.coverage(mask, paths.MATERIALS[material])
    if material == 'DirtLane':
        im = shared_ground.material_tile('Earth',255,variant,p)
    else:
        # Scale the lane's worn patches down to the narrow six-pixel trail.
        # Keep the core and joins clean; all detail stays away from tile edges.
        im = Image.new('RGBA',(16,16),p['earth'])
        d = ImageDraw.Draw(im)
        if variant == 1:
            d.polygon([(6,7),(7,7),(7,6),(9,6),(9,7),(10,7),
                       (10,9),(8,9),(8,10),(6,10)],fill=p['pathworn'])
            d.line((7,7,8,7),fill=p['sand'])
        elif variant == 2:
            d.polygon([(7,8),(9,8),(9,9),(10,9),(10,11),
                       (8,11),(8,10),(7,10)],fill=p['pathworn'])
            d.line((8,9,9,9),fill=p['sand'])
            d.line((7,11,8,11),fill=p['pathshade'])
        elif variant == 3:
            d.polygon([(6,4),(8,4),(8,5),(10,5),(10,7),
                       (7,7),(7,6),(6,6)],fill=p['pathworn'])
            d.line((8,5,9,5),fill=p['sand'])
            d.point((6,10),fill=p['pathshade'])
    # Short grass tongues expose the actual underlying surface. Leave all
    # connection ports intact, so the native joins remain exact across tiles.
    im = shared_ground.blend_margin(im, alpha, 'Earth', p)
    if material=='DirtLane' and variant==2:
        d=ImageDraw.Draw(im)
        d.polygon([(5,9),(7,8),(9,9),(8,11),(5,11)],fill=p['stone'])
        d.line((5,9,7,9),fill=p['sand'])
    im.putalpha(alpha)
    # Level ground has no black perimeter; grass interruptions and occasional
    # embedded flint break the ribbon without obscuring its walking direction.
    return im




if __name__ == '__main__':
    paths.main(OUTPUT,PREVIEW,'Paths16','shared-paths-atlas',
               shared_ground.PALETTE,render_tile=tile,
               reused_brushes=['Ground/Paint_Cobbles','Ground/Paint_WornStone'])
    shared_ground.zone.validate(Image.open(OUTPUT/'Paths16.png').convert('RGBA'))
