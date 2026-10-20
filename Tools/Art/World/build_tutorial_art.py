"""Tutorial materials. Reuse the shared native cliff recipe and exact palette.

Original Tutorial sources and Lowlands output remain untouched. This exports art,
not scene layout; import with TutorialArtAssets in stopped Unity.
"""
import argparse
import json
from PIL import Image, ImageDraw
import shared_palette as zone
import build_shared_terrain as terrain

TERRAIN_OUT = terrain.ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/Tutorial/Terrain'
OUT = terrain.ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/Tutorial/Environment'


BUILDING_SOURCE = zone.ROOT / 'ArtSource/Tutorial/Buildings'
BUILDING_RECIPE = json.loads((BUILDING_SOURCE / 'NativeRecipe.json').read_text(encoding='utf-8'))


def building_source(kind):
    """Register the depth-rebuilt sources on the existing native tile canvases."""
    spec = BUILDING_RECIPE['pieces'][kind]
    colors = zone.ramp(*spec.get('colors', BUILDING_RECIPE['buildingColors']))
    source = Image.open(BUILDING_SOURCE / spec['source']).convert('RGBA')
    alpha = source.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    source.putalpha(alpha)
    left, top, right, bottom = spec['bounds']
    # Registered native edits keep their canvas; high-resolution sources use alpha bounds.
    source = source.crop(spec.get('sourceBounds', alpha.getbbox())).resize(
        (right-left, bottom-top), Image.Resampling.NEAREST)
    # Hard palette mapping without dithering or interpolated source pixels.
    cache = {}
    pixels = []
    for pixel in source.get_flattened_data():
        if pixel[3] == 0:
            pixels.append((0, 0, 0, 0))
            continue
        if pixel not in cache:
            cache[pixel] = min(colors, key=lambda c: sum((pixel[i]-c[i])**2 for i in range(3)))
        pixels.append(cache[pixel])
    source.putdata(pixels)
    native = Image.new('RGBA', tuple(spec['canvas']))
    native.paste(source, (left, top))
    assert zone.validate(native) <= zone.SPEC['pieceBudgets']['building']
    return native


def woodshed():
    """Native 4x3 shed: original left door, fuel bay and sloping timber roof."""
    return building_source('Woodshed')


def cliffs(name, filename, manifest, height, material=None):
    cell_height = 16 + height
    atlas_height = 12 * cell_height
    atlas = Image.new('RGBA', (256, atlas_height))
    entries = []
    for mask in terrain.ground.MASKS:
        for variant in range(4):
            i = len(entries); x = i % 16 * 16; top = i // 16 * cell_height
            tile = terrain.cliff_volume(mask, variant, height=height)
            if material:
                tile.putdata([material.get(pixel, pixel) for pixel in tile.get_flattened_data()])
            atlas.paste(tile, (x, top))
            entries.append(dict(name=f'{name}_{mask:03d}_{variant}', material=name,
                                mask=mask, variant=variant, index=i, x=x,
                                y=atlas_height-top-cell_height, width=16, height=cell_height))
    zone.validate(atlas)
    TERRAIN_OUT.mkdir(parents=True, exist_ok=True)
    atlas.save(TERRAIN_OUT/filename)
    (TERRAIN_OUT/manifest).write_text(json.dumps(dict(entries=entries), indent=2)+'\n')


def village_building(kind):
    """Keep the workshop and longhouse registration and existing atlas slices."""
    return building_source(kind)


def tutorial_prop(kind):
    """Native replacements on the original canvases, without cast shadows."""
    p = zone.COLORS
    sizes = {'Stone_Bridge':(160,64), 'Old_Arch':(48,64), 'Broken_Pillar':(32,48),
             'Ruined_Wall':(48,32), 'Hedgerow':(48,32), 'Firewood_Rack':(48,32),
             'Fence_Vertical':(16,48), 'Fence_Repaired':(48,32), 'Fence_Post':(16,32),
             'Gate_Closed':(48,32), 'Target_New':(32,32), 'Target_Worn':(32,32),
             'Practice_Dummy':(32,32)}
    im = Image.new('RGBA', sizes[kind]); d = ImageDraw.Draw(im)
    def rect(box, color): d.rectangle(box, fill=p[color])
    def line(points, color): d.line(points, fill=p[color])
    def poly(points, color): d.polygon(points, fill=p[color])
    def post(x, top, bottom):
        poly(((x+2,top),(x+5,top),(x+7,top+2),(x+7,bottom),(x,bottom),(x,top+2)),'ink')
        rect((x+1,top+4,x+5,bottom-2),'soil')
        line((x+1,top+4,x+1,bottom-3),'soilmid')
        line((x+2,top+2,x+5,top+2),'soillight')
    if kind == 'Stone_Bridge':
        # Unbroken middle two rows are the actual walkable deck.
        rect((0,12,159,52),'stonejoint'); rect((0,16,159,47),'stoneshade')
        for row,y in enumerate((16,32)):
            for x in range(-16 if row else 0,160,32):
                rect((max(0,x+1),y,min(159,x+30),y+14),'stone' if (x//32+row)%4==1 else 'stoneshade')
                line((max(0,x+2),y,min(159,x+29),y),'stone')
        for y in (6,50):
            rect((0,y,159,y+12),'ink'); rect((1,y+1,158,y+10),'stonejoint')
            for x in range(2,158,20):
                rect((x,y+1,min(x+17,158),y+6),'stone')
                line((x,y+1,min(x+16,158),y+1),'stonelight')
                line((x,y+7,min(x+17,158),y+7),'stoneshade')
            # Two chipped coping stones, no holes through the deck.
            rect((57,y+1,59,y+2),'stonejoint'); rect((120,y+1,122,y+1),'stonejoint')
        for x in (0,150):
            for y in (0,48):
                rect((x,y+2,x+9,y+15),'ink'); rect((x+1,y+4,x+8,y+13),'stoneshade')
                rect((x+1,y+2,x+8,y+5),'stone'); line((x+2,y+2,x+7,y+2),'stonelight')
        assert all(im.getpixel((x,y))[3]==255 for x in range(160) for y in range(16,48))
    elif kind == 'Old_Arch':
        # The middle 16px repeat across the scene's existing five-cell opening.
        poly(((2,63),(2,39),(5,32),(13,26),(16,24),(31,24),(39,27),(45,34),(46,63),
              (35,63),(35,39),(31,35),(16,35),(12,39),(12,63)),'ink')
        poly(((4,61),(4,39),(7,33),(15,27),(32,26),(39,30),(43,35),(44,61),
              (37,61),(37,39),(32,33),(15,33),(10,38),(10,61)),'stoneshade')
        rect((16,25,31,32),'stone'); line((16,25,31,25),'stonelight')
        poly(((5,37),(8,32),(14,28),(15,32),(10,36),(9,40)),'stone')
        poly(((33,27),(39,31),(42,36),(38,38),(33,33)),'stone')
        for x in (4,37):
            for y in (43,54): line((x,y,x+6,y),'stonejoint')
            line((x,41,x,60),'stone'); rect((x-1,60,x+8,62),'stonejoint')
        line((8,34,11,36),'stonejoint'); line((38,32,35,35),'stonejoint')
    elif kind == 'Broken_Pillar':
        poly(((8,0),(13,4),(14,10),(23,15),(24,40),(28,43),(28,47),(4,47),(4,42),(7,40)),'ink')
        poly(((9,3),(11,6),(12,12),(21,17),(22,41),(9,42)),'stone')
        poly(((18,16),(22,18),(23,41),(17,41)),'stoneshade')
        line((10,15,10,39),'stonelight'); line((11,26,17,26),'stonejoint')
        line((17,26,17,29,20,31),'stonejoint')
        rect((6,43,26,46),'stoneshade'); line((7,43,24,43),'stone')
    elif kind == 'Ruined_Wall':
        poly(((0,31),(0,24),(4,21),(4,17),(10,15),(19,17),(24,13),(32,14),(36,19),(43,19),(47,25),(47,31)),'ink')
        poly(((2,29),(2,25),(6,22),(6,18),(11,17),(19,19),(25,15),(31,16),(35,21),(42,21),(45,26),(45,29)),'stoneshade')
        for x,y,w in ((6,18,10),(24,16,7),(2,26,14),(21,24,11),(36,24,7)):
            rect((x,y,x+w,y+4),'stone'); line((x,y,x+w-1,y),'stonelight')
        line((17,25,20,21),'stonejoint'); line((32,24,34,29),'stonejoint')
    elif kind == 'Hedgerow':
        # Three restrained sage leaf masses fit clipped border paintings too.
        for x,top in ((0,13),(16,15),(32,12)):
            poly(((x,24),(x+2,19),(x+4,top+2),(x+9,top),(x+13,top+3),
                  (x+15,22),(x+15,28),(x+11,31),(x+4,30)),'deepgreen')
            poly(((x+2,23),(x+4,top+3),(x+9,top+1),(x+12,top+4),
                  (x+13,24),(x+9,27),(x+4,26)),'leafshade')
            poly(((x+4,top+4),(x+9,top+2),(x+12,top+5),(x+9,top+9),(x+4,top+8)),'grasspatch')
            line((x+5,top+3,x+9,top+2),'leaflight')
    elif kind == 'Firewood_Rack':
        rect((4,12,43,29),'deepsoil')
        for x,y in ((11,24),(22,24),(33,24),(16,15),(28,15)):
            poly(((x-4,y-4),(x+3,y-4),(x+5,y-1),(x+4,y+4),(x-3,y+4),(x-5,y)),'soilcontact')
            poly(((x-3,y-3),(x+2,y-3),(x+3,y),(x+2,y+3),(x-3,y+2)),'soillight')
            line((x,y-1,x+1,y+1),'soil')
        post(0,5,31); post(40,5,31); line((7,30,39,30),'soilmid')
    elif kind in ('Fence_Repaired','Gate_Closed'):
        for y in (12,23):
            rect((4,y-1,43,y+4),'ink'); rect((5,y,42,y+2),'soil')
            line((6,y,41,y),'soillight')
        poly(((10,14),(13,13),(38,23),(38,26)),'deepsoil')
        line((12,14,37,24),'soilmid')
        post(0,3,31); post(40,3,31)
        if kind == 'Fence_Repaired':
            rect((23,9,27,28),'deepsoil'); line((24,10,24,27),'soillight')
    elif kind == 'Fence_Post': post(4,3,31)
    elif kind == 'Fence_Vertical':
        rect((6,3,11,45),'ink'); rect((7,4,9,44),'soil')
        line((7,4,7,43),'soillight')
        post(4,0,14); post(4,33,47)
    elif kind.startswith('Target_'):
        poly(((8,31),(10,20),(13,20),(11,31)),'deepsoil')
        poly(((21,31),(19,20),(22,20),(24,31)),'deepsoil')
        line((10,25,21,25),'soilmid')
        for box,color in (((5,0,26,23),'ink'),((6,1,25,22),'deepsoil'),
                          ((7,2,24,21),'soillight'),((9,4,22,19),'soil'),
                          ((11,6,20,17),'soilshine'),((13,8,18,15),'deepsoil')):
            d.ellipse(box,fill=p[color])
        if kind == 'Target_Worn':
            line((9,5,14,10),'deepsoil'); line((19,16,22,19),'deepsoil')
            line((15,12,17,12),'soilcontact')
    else:
        rect((14,11,17,31),'deepsoil'); line((14,12,14,30),'soilmid')
        line((4,14,27,14),'ink'); line((5,15,26,15),'soilmid')
        d.ellipse((11,0,20,9),fill=p['ink']);d.ellipse((12,1,19,8),fill=p['soilshine'])
        poly(((11,10),(20,10),(22,20),(19,25),(12,25),(9,20)),'deepsoil')
        poly(((12,11),(19,11),(20,20),(18,23),(13,23),(11,20)),'soillight')
        line((11,17,20,17),'soil'); line((15,11,15,15),'soilshine')
        for x in (5,24): rect((x,12,x+2,17),'soillight')
    if kind in ('Broken_Pillar', 'Ruined_Wall'):
        # Keep a single native-pixel contour. Fill the old broad ink band with
        # stone instead of eroding the silhouette, foot or shadow-casting mask.
        original = im.copy()
        w, h = im.size
        for y in range(1, h-1):
            for x in range(1, w-1):
                if original.getpixel((x,y)) == p['ink'] and all(
                    original.getpixel((x+dx,y+dy))[3] == 255
                    for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))
                ):
                    im.putpixel((x,y), p['stoneshade'])
    # Attached wear preserves the intact walking deck and existing silhouettes.
    if kind == 'Stone_Bridge':
        for x, y in ((19,8),(73,10),(117,51),(34,54),(140,52)):
            poly(((x,y),(x+5,y),(x+7,y+2),(x+4,y+3),(x,y+2)), 'leafshade')
            line((x+1,y,x+4,y), 'grasspatch')
        for x,y in ((42,23),(109,38)):
            line((x,y,x+2,y+2,x+1,y+4,x+4,y+6), 'stonejoint')
        rect((78,7,81,8), 'stonejoint')
        rect((122,51,125,52), 'stoneshade')
    elif kind == 'Broken_Pillar':
        poly(((9,35),(12,34),(13,38),(11,41),(16,42),(16,44),(7,44),(7,41)), 'leafshade')
        line((8,42,11,41,11,38), 'grasspatch')
        line((17,18,16,20,18,23), 'stonejoint')
        line((11,9,12,11), 'stoneshade')
    elif kind == 'Ruined_Wall':
        for x,y in ((7,18),(25,16),(36,25)):
            poly(((x,y),(x+4,y),(x+5,y+2),(x+3,y+2),(x+2,y+4),(x,y+3)), 'leafshade')
            line((x+1,y,x+3,y), 'grasspatch')
    elif kind == 'Hedgerow':
        original = im.copy()
        for y in range(im.height):
            for x in range(im.width):
                if original.getpixel((x,y))[3] and any(
                    not (0 <= x+dx < im.width and 0 <= y+dy < im.height)
                    or original.getpixel((x+dx,y+dy))[3] == 0
                    for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))
                ):
                    im.putpixel((x,y), p['ink'])
    zone.validate(im)
    return im


def structures():
    pieces = [('Cottage_Clay', village_building('Clay')), ('Cottage_Thatch', village_building('Thatch'))]
    pieces += [(name,tutorial_prop(name)) for name in ('Stone_Bridge','Old_Arch','Broken_Pillar',
               'Ruined_Wall','Hedgerow','Firewood_Rack','Fence_Vertical','Fence_Repaired',
               'Fence_Post','Gate_Closed','Target_New','Target_Worn','Practice_Dummy')]
    atlas=Image.new('RGBA',(512,256)); entries=[]; x=2; top=2; row_height=0
    for name,im in pieces:
        w,h=im.size
        if x+w+2>512: x=2; top+=row_height+4; row_height=0
        assert top+h+2<=256
        atlas.paste(im,(x,top)); bottom=256-top-h
        entries.append(dict(name=name,x=x,y=bottom,width=w,height=h))
        if not (name.startswith('Target_') or name=='Practice_Dummy'):
            entries += [dict(name=f'{name}_{cx}_{cy}',x=x+cx*16,y=bottom+cy*16,width=16,height=16)
                        for cy in range(h//16) for cx in range(w//16)]
        x+=w+4; row_height=max(row_height,h)
    zone.validate(atlas); atlas.save(OUT/'TutorialStructures16.png')
    (OUT/'StructuresManifest.json').write_text(json.dumps(dict(entries=entries),indent=2)+'\n')
    print(f'{len(pieces)} Tutorial structures/props; {len(entries)-len(pieces)} tile slices.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--buildings-only', action='store_true', help='Refresh structures/shed without exporting cliffs.')
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    if not args.buildings_only:
        cliffs('ShallowEarth', 'TutorialCliffs16.png', 'CliffManifest.json', 16)
        # Northern spring: the same connected silhouette, with the shared stone ramp.
        # Keep its existing three-cell height and quiet bare rock top.
        p = zone.COLORS
        stone = {p[a]: p[b] for a, b in (
            ('grass', 'stone'), ('leafshade', 'stoneshade'),
            ('soilmid', 'stoneshade'), ('soillight', 'stone'),
            ('soil', 'stonejoint'), ('deepsoil', 'rockshadow'),
            ('soilcontact', 'rockshadow'))}
        cliffs('RaisedStone', 'TutorialStoneCliffs16.png', 'StoneCliffManifest.json', 48, stone)
    shed = woodshed(); shed.save(OUT/'TutorialWoodshed16.png')
    pieces = [dict(name=f'Woodshed_{x}_{y}', x=x*16, y=y*16, width=16, height=16)
              for y in range(3) for x in range(4)]
    (OUT/'WoodshedManifest.json').write_text(json.dumps(dict(entries=pieces), indent=2)+'\n')
    structures()
    print(f'Tutorial art exported; exact shared palette. Output: {OUT}')


if __name__ == '__main__':
    main()
