"""Native Lowlands terrain: clean banks, exposed soil and broad stone shelves."""
import json
import random
import re
from PIL import Image, ImageDraw
import build_tutorial_ground as ground
import build_tutorial_terrain as terrain
import build_green_lowlands_ground as lowlands

PALETTE = dict(lowlands.PALETTE)
PALETTE.update({k: tuple(bytes.fromhex(v))+(255,) for k,v in {
    'water':'638C8E', 'watershade':'638C8E', 'waterdeep':'395F66',
    'waterlight':'9BC0AE', 'cream':'D4CCAA',
}.items()})
OUTPUT = ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/GreenLowlands/Terrain'
PREVIEW = ground.ROOT / 'Docs/Art/GreenLowlands'


def blob(material, mask, variant=0, palette=None):
    p = palette or PALETTE
    if variant and material in ('GrassLedge','StoneLedge'):
        return lowlands.lowlands_tile('Grass' if material=='GrassLedge' else 'WornStone',255,variant,p)
    return terrain.blob(material,mask,variant,p)


def face(material, row, column, variant=0, palette=None):
    p = palette or PALETTE
    stone = material == 'StoneCliff'
    im = Image.new('RGBA',(16,16),p['stoneshade' if stone else 'earth'])
    d = ImageDraw.Draw(im)
    # Broad strata, sparse inset stones. Shared top/bottom rows remain plain
    # so the face joins the automatic ledge and repeated middle rows cleanly.
    if stone:
        d.line((1,9,14,9),fill=p['rockshadow'])
        d.line((1,8,9,8),fill=p['stone'])
        x = 11 if row == 'Middle' else 5
        d.line((x,3,x,8),fill=p['rockshadow'])
        d.line((4,13,8,13),fill=p['stone'])
    else:
        d.line((1,8,14,8),fill=p['soil'])
        d.line((2,7,9,7),fill=p['sand'])
        d.rectangle((11,11,12,12),fill=p['soil'])
        d.line((11,11,12,11),fill=p['stone'])
        if row != 'Foot': d.line((5,3,5,5),fill=p['soil'])
    if row == 'Top':
        d.rectangle((0,0,15,2),fill=p['stone' if stone else 'grass'])
        d.line((0,3,15,3),fill=p['sand' if stone else 'leaflight'])
        d.line((0,4,15,4),fill=p['stoneshade' if stone else 'leafshade'])
    if row == 'Foot':
        d.line((0,12,15,12),fill=p['rockshadow' if stone else 'soil'])
        d.rectangle((0,13,15,15),fill=p['rockshadow' if stone else 'deepsoil'])
    if column in ('Left','Right'):
        x = 0 if column == 'Left' else 15
        if row == 'Top': d.line((x,0,x,2),fill=p['stoneshade' if stone else 'leafshade'])
        d.line((x,5 if row=='Top' else 0,x,12 if row=='Foot' else 15),
               fill=p['stoneshade' if stone else 'soil'])
    return im


def stairs(material, row, column, palette=None):
    p = palette or PALETTE
    if material == 'EarthRamp':
        im = lowlands.lowlands_tile('Earth',255,{'Top':0,'Middle':1,'Foot':2}[row],p)
    else:
        im = Image.new('RGBA',(16,16),p['rockshadow'])
        d = ImageDraw.Draw(im)
        # Wide, shallow stone treads; heavier than the village's close-set steps.
        for y in (0,8):
            d.line((0,y,15,y),fill=p['cream'])
            d.rectangle((0,y+1,15,y+4),fill=p['stone'])
            d.rectangle((0,y+5,15,y+6),fill=p['stoneshade'])
    d = ImageDraw.Draw(im)
    if row == 'Top': d.line((0,0,15,0),fill=p['cream' if material=='StoneStairs' else 'leaflight'])
    if row == 'Foot': d.line((0,15,15,15),fill=p['earth'])
    if column == 'Left': d.rectangle((0,0,1,15),fill=p['stoneshade' if material=='StoneStairs' else 'soil'])
    if column == 'Right': d.rectangle((14,0,15,15),fill=p['rockshadow' if material=='StoneStairs' else 'soil'])
    return im


def wall(mask, palette=None):
    p = dict(palette or PALETTE)
    p['sand'],p['deepsoil'] = p['cream'],p['rockshadow']
    return terrain.wall(mask,p)


def review_sheet():
    """An offline construction preview, drawn only from exported native sprites."""
    assets = ground.ROOT / 'RoadOfTheOldKing/Assets'

    def read_tiles(folder, filename):
        atlas = Image.open(folder / filename).convert('RGBA')
        entries = json.loads((folder / 'TileManifest.json').read_text())['entries']
        return {e['name']: atlas.crop((e['x'],atlas.height-e['y']-16,
                e['x']+16,atlas.height-e['y'])) for e in entries}, entries

    gt, ge = read_tiles(lowlands.OUTPUT, 'GreenLowlandsGround16.png')
    tt, te = read_tiles(OUTPUT, 'GreenLowlandsTerrain16.png')
    ground_tiles = {(e['material'],e['mask'],e['variant']):gt[e['name']] for e in ge}
    terrain_tiles = {(e['material'],e['mask'],e['variant']):tt[e['name']] for e in te}
    rng = random.Random(509)
    im = Image.new('RGBA',(384,256))
    for y in range(16):
        for x in range(24):
            variant = rng.choice([0]*8+list(range(1,8)))
            im.alpha_composite(ground_tiles['Grass',255,variant],(16*x,16*y))

    def paint(material, cells, tiles):
        for x,y in sorted(cells):
            mask = ground.normalize(sum(1<<i for i,(dx,dy) in enumerate(ground.OFFSETS)
                         if (x+dx,y+dy) in cells))
            variant = rng.choice([0]*8+[1,2,3]) if mask==255 else 0
            im.alpha_composite(tiles[material,mask,variant],(16*x,16*y))

    def rect(x0,y0,x1,y1):
        return {(x,y) for x in range(x0,x1+1) for y in range(y0,y1+1)}

    paint('DryGrass',rect(0,12,5,15)-{(5,12),(5,15)},ground_tiles)
    paint('Earth',rect(11,-1,14,10)|rect(8,8,14,10),ground_tiles)
    paint('DampEarth',rect(1,9,4,12)-{(1,9)},ground_tiles)
    paint('WornStone',rect(18,8,20,9)-{(20,9)},ground_tiles)

    # Short earth terrace and a separate exposed stone shelf. Face rows extend
    # the automatic ledge directly, and the ramp/stairs replace the full opening.
    meadow = rect(-1,-1,10,4)|rect(-1,5,8,5)
    rock = rect(17,2,22,5)-{(17,2),(22,2)}
    for cells,ledge,cliff in ((meadow,'GrassLedge','EarthCliff'),(rock,'StoneLedge','StoneCliff')):
        paint(ledge,cells,terrain_tiles)
        bottoms = {x:max(y for xx,y in cells if xx==x) for x in {x for x,y in cells}}
        faces = {(x,bottoms[x]+i):'Middle' if i==1 else 'Foot' for x in bottoms for i in (1,2)}
        for (x,y),row in sorted(faces.items()):
            col = 'Left' if (x-1,y) not in faces else 'Right' if (x+1,y) not in faces else 'Center'
            im.alpha_composite(tt[f'{cliff}_{row}_{col}'],(16*x,16*y))
    for material,x0,y0 in (('EarthRamp',6,5),('StoneStairs',18,5)):
        for dx,col in enumerate(('Left','Right')):
            for dy,row in enumerate(('Top','Middle','Foot')):
                im.alpha_composite(tt[f'{material}_{row}_{col}'],(16*(x0+dx),16*(y0+dy)))

    source = rect(2,-1,4,4)|{(3,5)}
    pool = rect(2,8,7,11)-{(2,8),(7,8),(2,11)}
    stream = rect(6,10,12,12)|rect(11,11,17,13)|rect(16,12,24,14)
    paint('MeadowWater',source,terrain_tiles)
    paint('MeadowWater',pool|stream,terrain_tiles)
    paint('DeepWater',rect(4,9,5,10),terrain_tiles)
    # A modest worked boundary, not an ancient monument.
    wall_cells = {(14,2),(15,2),(16,2),(16,3),(16,4)}
    for x,y in sorted(wall_cells):
        mask = sum(1<<i for i,(dx,dy) in enumerate(ground.OFFSETS[:4]) if (x+dx,y+dy) in wall_cells)
        im.alpha_composite(terrain_tiles['StoneWall',mask,0],(16*x,16*y))

    env_path = assets / 'Art/Tiles/Tutorial/Environment/TutorialEnvironment16.png'
    env = Image.open(env_path).convert('RGBA')
    meta = env_path.with_suffix('.png.meta').read_text()
    def prop(name):
        match = re.search(r'      name: '+re.escape(name)+
                r'\n      rect:\n        serializedVersion: \d+\n'
                r'        x: (\d+)\n        y: (\d+)\n'
                r'        width: (\d+)\n        height: (\d+)',meta)
        x,y,w,h = map(int,match.groups())
        return env.crop((x,env.height-y-h,x+w,env.height-y))
    props = [('Oak',(0,0)),('Birch',(120,0)),('Birch',(332,0)),
             ('Boulder',(14,206)),('Oak',(0,220)),
             ('Fence_Horizontal',(272,166)),('Fence_Horizontal',(310,166))]
    hero = Image.open(assets / 'Art/Sprites/Player/Rotations/south.png').convert('RGBA')
    frames = []
    for f in range(4):
        frame = im.copy()
        for name,x,y in [('FallLip_00',3,5),(f'FallBody_{f:02d}',3,6),
                         (f'FallBody_{f:02d}',3,7),(f'FallFoam_{f:02d}',3,8),
                         (f'Ripple_{f:02d}',3,2),(f'Ripple_{f:02d}',6,9),
                         (f'Ripple_{f:02d}',14,12),(f'Ripple_{f:02d}',20,13)]:
            frame.alpha_composite(tt[name],(16*x,16*y))
        for name,xy in props: frame.alpha_composite(prop(name),xy)
        frame.alpha_composite(hero,(207,125))
        frames.append(frame.convert('RGB').resize((1536,1024),Image.Resampling.NEAREST))
    frames[0].save(PREVIEW/'green-lowlands-terrain-detail.png')
    frames[0].save(PREVIEW/'green-lowlands-terrain-motion.gif',save_all=True,
                   append_images=frames[1:],duration=200,loop=0,disposal=2)


if __name__ == '__main__':
    terrain.main(OUTPUT,PREVIEW,'GreenLowlandsTerrain16','green-lowlands-terrain-atlas',
                 PALETTE,'GREEN LOWLANDS TERRAIN / 16px / FIELD & OLD ROAD',
                 render_blob=blob,render_wall=wall,render_face=face,render_stairs=stairs)
    review_sheet()
