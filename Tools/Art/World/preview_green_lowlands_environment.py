"""Offline Environment review from imported native sprites; no source-image edits."""
import json
import random
import re
from PIL import Image, ImageDraw
import build_tutorial_ground as ground
import build_green_lowlands_ground as lowlands
import build_green_lowlands_terrain as terrain
import build_green_lowlands_paths as paths

ASSETS = ground.ROOT/'RoadOfTheOldKing/Assets'
PREVIEW = lowlands.PREVIEW
ENV = ASSETS/'Art/Tiles/GreenLowlands/Environment'


def sprites(path):
    atlas = Image.open(path).convert('RGBA')
    meta = path.with_suffix('.png.meta').read_text()
    pattern = (r'      name: ([^\n]+)\n      rect:\n        serializedVersion: \d+\n'
               r'        x: (\d+)\n        y: (\d+)\n        width: (\d+)\n        height: (\d+)')
    result = {}
    for name,x,y,w,h in re.findall(pattern,meta):
        x,y,w,h = map(int,(x,y,w,h))
        result[name] = atlas.crop((x,atlas.height-y-h,x+w,atlas.height-y))
    return result


def tiles(folder,filename):
    atlas = Image.open(folder/filename).convert('RGBA')
    entries = json.loads((folder/'TileManifest.json').read_text())['entries']
    return {(e['material'],e['mask'],e['variant']):atlas.crop(
            (e['x'],atlas.height-e['y']-16,e['x']+16,atlas.height-e['y'])) for e in entries}


def environment_scene(decorate=None):
    gt = tiles(lowlands.OUTPUT,'GreenLowlandsGround16.png')
    tt = tiles(terrain.OUTPUT,'GreenLowlandsTerrain16.png')
    pt = tiles(paths.OUTPUT,'GreenLowlandsPaths16.png')
    reused = sprites(ASSETS/'Art/Tiles/Tutorial/Environment/TutorialEnvironment16.png')
    new = sprites(ENV/'GreenLowlandsEnvironment16.png')
    hero = Image.open(ASSETS/'Art/Sprites/Player/Rotations/south.png').convert('RGBA')
    rng = random.Random(513)
    im = Image.new('RGBA',(640,320))
    for y in range(20):
        for x in range(40):
            im.alpha_composite(gt['Grass',255,rng.choice([0]*8+list(range(1,8)))],(16*x,16*y))

    def rect(x0,y0,x1,y1):
        return {(x,y) for x in range(x0,x1+1) for y in range(y0,y1+1)}

    def paint(material,cells,library):
        for x,y in sorted(cells):
            mask = ground.normalize(sum(1<<i for i,(dx,dy) in enumerate(ground.OFFSETS)
                         if (x+dx,y+dy) in cells))
            variant = rng.randrange(4) if mask==255 else 0
            im.alpha_composite(library[material,mask,variant],(16*x,16*y))

    paint('DryGrass',rect(0,0,3,3)|rect(25,14,30,18)-{(25,14),(30,18)},gt)
    paint('Earth',rect(2,5,11,10)|rect(13,6,19,9),gt)
    # Fallow plots: dry turf and small remaining soil patches, using accepted Ground.
    paint('DryGrass',rect(0,15,8,19)-{(0,15),(8,15),(8,19)},gt)
    paint('TilledSoil',rect(1,16,2,17)|rect(4,17,5,18)|rect(7,15,8,16),gt)
    paint('MeadowWater',rect(20,-1,23,15)|rect(21,15,24,20),tt)
    paint('Footpath',rect(7,7,17,7)|rect(11,8,11,18)|rect(8,18,11,18),pt)
    paint('DirtLane',rect(16,6,18,20)|rect(17,11,40,12)|rect(33,9,34,11),pt)
    paint('Cobbles',rect(16,6,17,7),gt)
    paint('WornStone',{(16,17),(16,18)},gt)

    props = [('Farmhouse_Weathered',(16,16)),('Watermill',(208,16)),
             ('Woodshed_Weathered',(24,126)),('Well_Disused',(153,71)),
             ('Farm_Cart',(124,133)),('Plank_Bridge',(304,160)),
             ('Reused_Stone_Post',(108,186)),('Fence_Weathered',(132,186)),
             ('Fence_Weathered',(172,186)),('Fence_Weathered',(9,212)),
             ('Fence_Weathered',(49,212)),('Oak',(0,257)),
             ('Birch',(417,14)),('Birch',(164,0)),('Fallen_Log',(82,151)),
             ('Boulder',(420,261)),('Hedgerow',(396,78)),
             ('Workshop_Weathered',(488,82)),('Oak',(551,229))]
    library = dict(reused,**new)
    if decorate:
        decorate(im,props,library)
    for name,xy in sorted(props,key=lambda item:item[1][1]+library[item[0]].height):
        im.alpha_composite(library[name],xy)
    im.alpha_composite(hero,(261,220))
    return im


def main():
    im = environment_scene()
    im.convert('RGB').resize((1920,960),Image.Resampling.NEAREST).save(PREVIEW/'green-lowlands-environment-detail.png')
    reused = sprites(ASSETS/'Art/Tiles/Tutorial/Environment/TutorialEnvironment16.png')
    new = sprites(ENV/'GreenLowlandsEnvironment16.png')
    hero = Image.open(ASSETS/'Art/Sprites/Player/Rotations/south.png').convert('RGBA')

    # One common native scale, exact source pixels, labels outside the artwork.
    sheet = Image.new('RGBA',(480,354),'#252C26')
    draw = ImageDraw.Draw(sheet)
    draw.text((12,8),'TUTORIAL - reference',fill='#DFC291')
    draw.text((212,8),'LOWLANDS - original architecture',fill='#DFC291')
    sheet.alpha_composite(reused['Cottage_Thatch'],(12,27))
    sheet.alpha_composite(new['Farmhouse_Weathered'],(212,27))
    sheet.alpha_composite(hero,(164,91));sheet.alpha_composite(hero,(367,91))
    for name,label,xy in [
        ('Watermill','Idle mill',(12,152)),
        ('Workshop_Weathered','Threshing barn',(176,168)),
        ('Woodshed_Weathered','Drying shed',(292,200)),
        ('Well_Disused','Windlass well',(396,200)),
        ('Farm_Cart','Cart',(12,280)),
        ('Plank_Bridge','Bridge',(108,264)),
        ('Reused_Stone_Post','Reused old post',(246,280)),
        ('Fence_Weathered','Weathered fence',(372,280))]:
        draw.text((xy[0],xy[1]-15),label,fill='#DFC291')
        sheet.alpha_composite(new[name],xy)
    draw.text((12,337),'16 PPU / small object palettes / Tutorial originals preserved',fill='#DFC291')
    sheet.convert('RGB').resize((1440,1062),Image.Resampling.NEAREST).save(PREVIEW/'green-lowlands-environment-comparison.png')
    catalog = json.loads((ENV/'EnvironmentManifest.json').read_text())
    colors={name:len({p[:3] for p in new[name].get_flattened_data() if p[3]})
            for name in [e['name'] for e in catalog['entries']]}
    print(json.dumps({'colors_by_object':colors,'regional_objects':len(catalog['entries']),
                      'reused_objects':len(catalog['reusedPrefabs'])}))


if __name__ == '__main__':main()
