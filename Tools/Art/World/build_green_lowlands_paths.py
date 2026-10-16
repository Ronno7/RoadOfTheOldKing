"""Quiet Lowlands farm routes with unchanged native path masks/connections."""
import json
import random
import re
from PIL import Image, ImageDraw
import build_tutorial_ground as ground
import build_tutorial_paths as paths
import build_green_lowlands_ground as lowlands
import build_green_lowlands_terrain as terrain

OUTPUT = ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/GreenLowlands/Paths'
PREVIEW = lowlands.PREVIEW


def tile(material, mask, variant=0, palette=None):
    p = palette or lowlands.PALETTE
    alpha = paths.coverage(mask, paths.MATERIALS[material])
    if material == 'DirtLane':
        im = lowlands.lowlands_tile('Earth',255,variant,p)
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
    source=alpha.copy()
    for y in range(1,15):
        for x in range(1,15):
            if not source.getpixel((x,y)):continue
            distance=min((abs(dx)+abs(dy) for dx,dy in
                [(0,-1),(0,1),(-1,0),(1,0),(0,-2),(0,2),(-2,0),(2,0)]
                if 0<=x+dx<16 and 0<=y+dy<16 and not source.getpixel((x+dx,y+dy))),default=9)
            tongue=(x*3+y*5)%11
            if distance<=2 and tongue in (0,1,3):alpha.putpixel((x,y),0)
            elif distance==1 and tongue in (5,6):im.putpixel((x,y),p['pathshade'])
    if material=='DirtLane' and variant==2:
        d=ImageDraw.Draw(im)
        d.polygon([(5,9),(7,8),(9,9),(8,11),(5,11)],fill=p['stone'])
        d.line((5,9,7,9),fill=p['sand'])
    im.putalpha(alpha)
    # Level ground has no black perimeter; grass interruptions and occasional
    # embedded flint break the ribbon without obscuring its walking direction.
    return im


def review_sheet():
    """Show actual exported route pieces with the retained regional layers."""
    assets = ground.ROOT / 'RoadOfTheOldKing/Assets'
    def read_tiles(folder, filename):
        atlas = Image.open(folder/filename).convert('RGBA')
        entries = json.loads((folder/'TileManifest.json').read_text())['entries']
        return {(e['material'],e['mask'],e['variant']):atlas.crop(
                (e['x'],atlas.height-e['y']-16,e['x']+16,atlas.height-e['y'])) for e in entries}
    gt = read_tiles(lowlands.OUTPUT,'GreenLowlandsGround16.png')
    tt = read_tiles(terrain.OUTPUT,'GreenLowlandsTerrain16.png')
    pt = read_tiles(OUTPUT,'GreenLowlandsPaths16.png')
    rng = random.Random(511)
    im = Image.new('RGBA',(384,256))
    for y in range(16):
        for x in range(24):
            im.alpha_composite(gt['Grass',255,rng.choice([0]*8+list(range(1,8)))],(16*x,16*y))

    def rect(x0,y0,x1,y1):
        return {(x,y) for x in range(x0,x1+1) for y in range(y0,y1+1)}

    def paint(material,cells,tiles):
        for x,y in sorted(cells):
            mask = ground.normalize(sum(1<<i for i,(dx,dy) in enumerate(ground.OFFSETS)
                         if (x+dx,y+dy) in cells))
            variant = rng.randrange(4) if mask==255 else 0
            im.alpha_composite(tiles[material,mask,variant],(16*x,16*y))

    # Fields stay visibly in use. Old paving appears only in a small worn patch.
    paint('DryGrass',rect(1,0,5,2)|rect(18,12,23,16)-{(18,12),(23,12)},gt)
    paint('Earth',rect(17,3,22,7)-{(17,3),(22,3),(22,7)},gt)
    paint('TilledSoil',rect(1,11,2,14)|rect(4,11,5,14),gt)
    paint('MeadowWater',rect(-1,5,2,7)|rect(0,6,4,8)-{(4,6),(4,8)},tt)

    lane = rect(12,-1,14,5)|rect(10,5,14,7)|rect(9,7,11,16)
    branch = rect(3,4,12,4)|rect(3,2,3,4)|rect(11,9,20,9)|rect(20,6,20,9)
    branch |= rect(7,9,10,9)|rect(7,9,7,15)
    # Match the template: Footpath below DirtLane, Paving above both.
    paint('Footpath',branch,pt)
    paint('DirtLane',lane,pt)
    paint('Cobbles',rect(19,6,21,7),gt)
    paint('WornStone',{(12,2),(13,2),(13,3)},gt)

    env_path = assets/'Art/Tiles/Tutorial/Environment/TutorialEnvironment16.png'
    env = Image.open(env_path).convert('RGBA')
    meta = env_path.with_suffix('.png.meta').read_text()
    def prop(name):
        match = re.search(r'      name: '+re.escape(name)+
                r'\n      rect:\n        serializedVersion: \d+\n'
                r'        x: (\d+)\n        y: (\d+)\n'
                r'        width: (\d+)\n        height: (\d+)',meta)
        x,y,w,h = map(int,match.groups())
        return env.crop((x,env.height-y-h,x+w,env.height-y))
    props = [('Oak',(0,0)),('Birch',(83,0)),('Birch',(249,0)),
             ('Fence_Horizontal',(268,113)),('Fence_Horizontal',(344,113)),
             ('Fence_Horizontal',(15,158)),('Fence_Horizontal',(53,158)),
             ('Boulder',(48,145)),('Oak',(274,200))]
    for name,xy in props: im.alpha_composite(prop(name),xy)
    hero = Image.open(assets/'Art/Sprites/Player/Rotations/south.png').convert('RGBA')
    im.alpha_composite(hero,(157,161))
    im.convert('RGB').resize((1536,1024),Image.Resampling.NEAREST).save(PREVIEW/'green-lowlands-paths-detail.png')


if __name__ == '__main__':
    paths.main(OUTPUT,PREVIEW,'GreenLowlandsPaths16','green-lowlands-paths-atlas',
               lowlands.PALETTE,render_tile=tile,
               reused_brushes=['Ground/Paint_Cobbles','Ground/Paint_WornStone'])
    lowlands.zone.validate(Image.open(OUTPUT/'GreenLowlandsPaths16.png').convert('RGBA'))
    review_sheet()
