"""Shared terrain atlas, banks, water and native cliff cells.

The legacy cliff export remains required by BarrowApproach."""
import json
import sys
from PIL import Image, ImageDraw
import ground_tile_helpers as ground
import terrain_tile_helpers as terrain
import build_shared_ground as shared_ground

import shared_palette as zone

PALETTE = dict(shared_ground.PALETTE)
PALETTE['cream'] = zone.COLORS['stonelight']
OUTPUT = ground.ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/Shared/Terrain'
PREVIEW = ground.ROOT / 'DeveloperNotes/Previews/Shared'


def blob(material, mask, variant=0, palette=None):
    p = palette or PALETTE
    if variant and material in ('GrassLedge','StoneLedge'):
        return shared_ground.material_tile('Grass' if material=='GrassLedge' else 'WornStone',255,variant,p)
    image = Image.new('RGBA',(16,16))
    ledge = material in ('GrassLedge','StoneLedge')
    field = terrain.extended(mask,ledge)
    for y in range(16):
        for x in range(16):
            if not field.getpixel((x+16,y+16)): continue
            north = next((i for i in range(1,9) if not field.getpixel((x+16,y+16-i))),9)
            south = next((i for i in range(1,8) if not field.getpixel((x+16,y+16+i))),8)
            side = min(next((i for i in range(1,4) if not field.getpixel((x+16-i,y+16))),4),
                       next((i for i in range(1,4) if not field.getpixel((x+16+i,y+16))),4))
            if material == 'DeepWater':
                color = 'watershade' if terrain.distance(field,x,y,1)==1 else 'waterdeep'
            elif not ledge:
                stone = material == 'StoneWater'
                # Far shore exposes a small inner face; near shore overlaps as turf.
                # Never rotate the whole north wall around the pond perimeter.
                color = 'water'
                if north <= 7:
                    color = ('rockshadow' if stone else 'deepgreen') if north==1 else \
                            ('stoneshade' if stone else 'soilmid') if north<=5 else \
                            ('stonejoint' if stone else 'soil') if north==6 else 'watershade'
                elif south <= 2:
                    color = ('stoneshade' if stone else 'grass') if south==1 else 'watershade'
                elif side <= 2:
                    color = ('stoneshade' if stone else 'soilmid') if side==1 else 'watershade'
            else:
                stone = material=='StoneLedge'
                color = 'stone' if stone else 'grass'
                if south<=7:
                    color = ('rockshadow' if stone else 'deepgreen') if south==7 else \
                            ('stoneshade' if stone else 'soilmid')
                    if side==1 and south<6: color='stoneshade' if stone else 'soil'
                elif north==1 or side==1: color='stonejoint' if stone else 'leafshade'
            image.putpixel((x,y),p[color])
    if variant and 'Water' in material:
        d=ImageDraw.Draw(image)
        d.line((3+variant,5,6+variant,5),fill=p['watershade' if material=='DeepWater' else 'waterlight'])
    return image


def face(material, row, column, variant=0, palette=None):
    p = palette or PALETTE
    stone = material == 'StoneCliff'
    base = p['stoneshade' if stone else 'soilmid']
    im = Image.new('RGBA',(16,16),base)
    d = ImageDraw.Draw(im)
    # Broad masses carry height; small contrast is reserved for one recessed join.
    # The native study's clusters are extended to the authored three-row flights.
    if stone:
        shift=5 if row=='Middle' else 0
        for x in (-16+shift,shift,16+shift):
            d.polygon([(x+1,2),(x+13,2),(x+15,4),(x+14,11),(x+10,14),(x+1,13)],fill=p['stonejoint'])
            d.polygon([(x+2,2),(x+12,2),(x+14,4),(x+12,7),(x+7,6),(x+2,8)],fill=p['stone'])
            d.line((x+3,3,x+9,3),fill=p['cream'])
    else:
        shift=7 if row=='Middle' else 0
        for x in (-16+shift,shift,16+shift):
            d.polygon([(x+2,1),(x+11,1),(x+14,3),(x+12,6),(x+8,5),(x+5,7),(x,5)],fill=p['soillight'])
            # Open shadow wedges, not a dark ring enclosing an oval per cell.
            d.polygon([(x+14,7),(x+16,8),(x+16,13),(x+11,15),
                       (x+5,15),(x+6,13),(x+11,12)],fill=p['soil'])
    # Ledge-to-face join stays continuous; it is not another horizontal outline.
    d.line((0,0,15,0),fill=base)
    if row == 'Top':
        d.rectangle((0,0,15,2),fill=p['stone' if stone else 'grass'])
        d.line((0,3,15,3),fill=p['rockshadow' if stone else 'deepgreen'])
        if not stone:
            for x in (2,8,13):d.point((x,4),fill=p['deepgreen'])
    if row == 'Foot':
        d.rectangle((0,14,15,15),fill=p['rockshadow' if stone else 'soilcontact'])
    if column in ('Left','Right'):
        x = 0 if column == 'Left' else 15
        if row == 'Top': d.line((x,0,x,2),fill=p['stoneshade' if stone else 'leafshade'])
        d.line((x,5 if row=='Top' else 0,x,13 if row=='Foot' else 15),
               fill=p['stoneshade' if stone else 'soil'])
    return im


def stairs(material, row, column, palette=None):
    p = palette or PALETTE
    if material == 'EarthRamp':
        im = shared_ground.material_tile('Earth',255,{'Top':0,'Middle':1,'Foot':2}[row],p)
    else:
        im = Image.new('RGBA',(16,16),p['rockshadow'])
        d = ImageDraw.Draw(im)
        # The same tread language as the approved study, extended over the full rise.
        for y in (0,4,8,12):
            d.rectangle((0,y,15,y+1),fill=p['stone'])
            d.line((0,y+2,15,y+2),fill=p['stonejoint'])
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


def water_detail(kind, frame, palette=None):
    p=palette or PALETTE
    if kind!='Ripple': return terrain.water_detail(kind,frame,p)
    im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
    # Four small poses, no diagonal scrolling sheet or crawling surface noise.
    x=3+(frame%2)
    d.line((x,5,x+[4,5,3,4][frame],5),fill=p['waterlight'])
    if frame in (1,2):d.line((9,11,11,11),fill=p['watershade'])
    return im




def _legacy_depth_edge(stone, edge):
    """Native directional edge with identical opposite seams, never rotated."""
    im = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(im)
    top = PALETTE['stone' if stone else 'grass']
    light = PALETTE['cream' if stone else 'grasspatch']
    dark = PALETTE['rockshadow' if stone else 'deepgreen']
    mid = PALETTE['stonejoint' if stone else 'soilmid']
    # Long banks stay clean and cardinal. Shape changes belong to authored
    # corners, not a repeating lateral wave inside each sixteen-pixel tile.
    if edge == 'Back':
        for x in range(16):
            crest = 6
            d.line((x, crest, x, 15), fill=top)
            d.point((x, crest-3), fill=mid)
            d.line((x, crest-2, x, crest-1), fill=dark)
        d.line((3, 7, 5, 7), fill=light)
        d.line((11, 6, 13, 6), fill=light)
    elif edge in ('West', 'East'):
        for y in range(16):
            if edge == 'West':
                crest = 8
                d.line((0, y, crest, y), fill=top)
                d.point((crest+1, y), fill=dark)
                d.line((crest+2, y, crest+4, y), fill=mid)
                d.line((crest+5, y, min(15, crest+6), y), fill=dark)
            else:
                crest = 7
                d.line((crest, y, 15, y), fill=top)
                d.point((crest-1, y), fill=dark)
                d.line((crest-4, y, crest-2, y), fill=mid)
                d.line((crest-6, y, crest-5, y), fill=dark)
    elif edge in ('WestBackOuter', 'EastBackOuter'):
        # Convex far corner: high ground is the INTERSECTION of west/east
        # and south. The older Back corner is concave (their union), with
        # opposite entry ports. Reusing it here disconnects side from crest.
        west = edge == 'WestBackOuter'
        side = _legacy_depth_edge(stone, 'West' if west else 'East')
        back = _legacy_depth_edge(stone, 'Back')
        for y in range(16):
            for x in range(16):
                u = x if west else 15-x
                if u <= 8 and y >= 6:
                    color = top
                elif u <= 8:
                    color = back.getpixel((x,y))
                elif y >= 6:
                    color = side.getpixel((x,y))
                else:
                    distance = ((u-8)**2/36 + (6-y)**2/9)**.5
                    color = (dark if distance<.4 or distance>.8 else mid) if distance<=1 else (0,0,0,0)
                im.putpixel((x,y),color)
        # Explicit native ports retain the directional profiles exactly.
        if west:im.paste(back.crop((0,0,2,16)),(0,0))
        else:im.paste(back.crop((14,0,16,16)),(14,0))
        im.paste(side.crop((0,14,16,16)),(0,14))
    elif edge in ('WestBack', 'EastBack'):
        west = edge == 'WestBack'
        side = _legacy_depth_edge(stone, 'West' if west else 'East')
        back = _legacy_depth_edge(stone, 'Back')
        # A single L-shaped mass: high to the west/east AND south; low in the
        # opposite northern quadrant. Inner shoulder fills the former rail gap.
        for y in range(16):
            for x in range(16):
                u = x if west else 15-x
                if u <= 8 or y >= 6 or (u <= 10 and y >= 4) or (u <= 12 and y >= 5):
                    color = top
                elif (u-8)/6.0 <= (6-y)/3.0:
                    color = side.getpixel((x, y))
                else:
                    color = back.getpixel((x, y))
                im.putpixel((x, y), color)
        # Guarantee exact joins to unchanged side/back border profiles.
        if west:
            im.paste(back.crop((14, 0, 16, 16)), (14, 0))
        else:
            im.paste(back.crop((0, 0, 2, 16)), (0, 0))
        im.paste(side.crop((0, 0, 16, 2)), (0, 0))
        # Continuous dark concave turn, with restrained soil/stone exposure.
        points = [(9, 2), (9, 3), (11, 3), (11, 4), (13, 4), (13, 5)]
        if not west: points = [(15-x, y) for x, y in points]
        d.line(points, fill=dark, width=1)
    else:
        d.rectangle((0, 0, 15, 1), fill=dark)
        d.line((0, 2, 15, 2), fill=mid)
    return im



def _depth_edge(stone, edge):
    """Illustrated earth shoulders with the existing directional alpha/ports."""
    if stone:
        return _legacy_depth_edge(stone, edge)
    im = _legacy_depth_edge(False, edge)
    p = PALETTE
    if edge == 'Foot':
        for y in range(16):
            for x in range(16):
                if im.getpixel((x, y))[3]:
                    im.putpixel((x, y), zone.COLORS['ink'] if y == 0 else p['soilcontact'])
        return im
    west = edge.startswith('West')
    side_edge = edge in ('West', 'East')
    back_edge = edge == 'Back'
    outer = edge.endswith('Outer')
    for y in range(16):
        for x in range(16):
            if not im.getpixel((x, y))[3]:
                continue
            u = x if west else 15-x
            side_crest = 6 if y % 8 in (0, 1, 6, 7) else 5
            back_crest = 8 if x % 8 < 4 else 9
            side_turf = u <= side_crest
            back_turf = y >= back_crest
            turf = side_turf if side_edge else back_turf if back_edge else \
                   (side_turf and back_turf if outer else side_turf or back_turf)
            if turf:
                edge_turf = (u == side_crest and not back_edge) or (y == back_crest and not side_edge)
                color = p['leafshade' if edge_turf else 'grass']
            else:
                side = side_edge or (not back_edge and 14-u < y-3)
                color = p['soilmid']
                if side:
                    color = p['soil' if west else 'soilmid']
                    if 8 <= u <= 12 and y % 8 < 6:
                        color = p['soilmid' if west else 'soillight']
                    if u == 14:
                        color = zone.COLORS['ink']
                else:
                    if y == 3: color = zone.COLORS['ink']
                    elif y >= 5 and (x+y*2) % 16 in range(3, 10): color = p['soillight']
            im.putpixel((x, y), color)
    if not side_edge and not back_edge:
        side = _depth_edge(False, 'West' if west else 'East')
        back = _depth_edge(False, 'Back')
        # The corner's shared ports match their neighboring runs exactly.
        x = 0 if (west == outer) else 14
        im.paste(back.crop((x, 0, x+2, 16)), (x, 0))
        y = 14 if outer else 0
        im.paste(side.crop((0, y, 16, y+2)), (0, y))
    return im



def _depth_door():
    """Registered 64px recessed arch, shared by the exterior and interior."""
    from build_lowlands_architecture import building_source
    return building_source('Storehouse_Door')


def _shore_face(variant):
    """The same broad earth as automatic cliffs, sliced into retained wet faces."""
    im = _cliff_earth().crop((variant*32, 0, variant*32+32, 48))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 46, 31, 47), fill=PALETTE['soilcontact'])
    d.line((0, 47, 31, 47), fill=zone.COLORS['ink'])
    # Retained manual variants may be painted in any horizontal sequence.
    # Their two-column ports share a quiet earth profile, not a dark seam.
    for y in range(46):
        for x in (0, 1, 30, 31):
            im.putpixel((x, y), PALETTE['soilmid'])
    return im


def _shore_return(side):
    """Opaque earth end, 16x48. W = high west/low east; E = high east/low west.

    Turf is only at the top of the rise. These pieces terminate an exposed
    south face or support an apron; they are not a grass strip on the low land.
    """
    p = PALETTE
    im = _shore_face(0).crop((0, 0, 16, 48))
    d = ImageDraw.Draw(im)
    # West-high has a darker east-facing return; its mirror receives a broader
    # upper-left plane. The inner edge keeps the face's exact shared profile.
    if side == 'W':
        d.rectangle((8, 4, 13, 45), fill=p['soil'])
        d.rectangle((10, 10, 12, 23), fill=p['soilmid'])
        d.rectangle((9, 29, 11, 41), fill=p['soilmid'])
        outer, inner = 15, 14
    else:
        im = im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        d = ImageDraw.Draw(im)
        d.rectangle((2, 4, 7, 45), fill=p['soilmid'])
        d.rectangle((4, 10, 6, 23), fill=p['soillight'])
        d.rectangle((5, 29, 7, 41), fill=p['soillight'])
        outer, inner = 0, 1
    d.line((inner, 4, inner, 45), fill=p['deepsoil'])
    d.line((outer, 0, outer, 45), fill=zone.COLORS['ink'])
    d.point((inner, 0), fill=p['soilmid'])
    d.point((inner, 1), fill=p['soilmid'])
    d.rectangle((0, 46, 15, 47), fill=p['soilcontact'])
    # The upper two rows meet the directional side bank exactly. Previously
    # the full-width turf cap left a square protrusion at every shore shoulder.
    side_edge = _depth_edge(False, 'West' if side == 'W' else 'East')
    im.paste(side_edge.crop((0, 0, 16, 2)), (0, 0))
    return im


def _shore_inside(side):
    """Three-cell re-entrant corner: high side meets a front, then turns below.

    The front-facing port spans the whole rise. A plain side bank at this
    junction leaves the front floating a pixel away from the upper surface.
    """
    im = _shore_face(0).crop((16, 0, 32, 48))
    p = PALETTE
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 6, 47), fill=p['grass'])
    d.line((7, 3, 7, 45), fill=p['leafshade'])
    d.rectangle((8, 3, 10, 44), fill=p['soilmid'])
    # Connected inner foot, continuing into the ordinary narrow side below.
    d.line((9, 46, 11, 46), fill=p['deepgreen'])
    d.line((12, 47, 15, 47), fill=zone.COLORS['ink'])
    if side == 'E':
        im = im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return im


def depth_edges():
    """Directional structural overlays, joined corners and a recessed bank door.

    Existing eight sprite names/rects stay at bottom row y=0. Appended artwork
    uses bottom-origin manifest coordinates, including Door_x_y (y=0 is floor).
    North remains the back of a foreground bank, never a rotated front cliff.
    """
    names = ['Bank_Back', 'Bank_West', 'Bank_East', 'Bank_Foot',
             'Stone_Back', 'Stone_West', 'Stone_East', 'Stone_Foot']
    atlas = Image.new('RGBA', (128, 176)); entries = []

    # Convex and concave corners have different ports. Keep both directional
    # joins exact so an art revision cannot bring back disconnected bank rails.
    for stone in (False, True):
        for side in ('West', 'East'):
            outer = _depth_edge(stone, side+'BackOuter')
            straight, back = _depth_edge(stone, side), _depth_edge(stone, 'Back')
            port = (0,0,2,16) if side=='West' else (14,0,16,16)
            assert outer.crop(port).tobytes() == back.crop(port).tobytes()
            assert outer.crop((0,14,16,16)).tobytes() == straight.crop((0,14,16,16)).tobytes()
    for side in ('W', 'E'):
        assert _shore_return(side).crop((0,0,16,2)).tobytes() == \
            _depth_edge(False, 'West' if side=='W' else 'East').crop((0,0,16,2)).tobytes()
        inside_port = (14, 0, 16, 48) if side == 'W' else (0, 0, 2, 48)
        assert _shore_inside(side).crop(inside_port).tobytes() == \
            _shore_face(0).crop((0, 0, 2, 48)).tobytes()

    def append(name, im, x, y):
        atlas.alpha_composite(im, (x, atlas.height-y-16))
        entries.append(dict(name=name, material=name, mask=0, variant=0,
                            index=len(entries), x=x, y=y, width=16, height=16))

    for i, name in enumerate(names):
        append(name, _depth_edge(name.startswith('Stone'), name.split('_')[1]), i*16, 0)
    for i, name in enumerate(('Bank_WestBack', 'Bank_EastBack',
                              'Stone_WestBack', 'Stone_EastBack')):
        append(name, _depth_edge(name.startswith('Stone'), name.split('_')[1]), i*16, 16)
    for i, name in enumerate(('Bank_WestBackOuter', 'Bank_EastBackOuter',
                              'Stone_WestBackOuter', 'Stone_EastBackOuter')):
        append(name, _depth_edge(name.startswith('Stone'), name.split('_')[1]), i*16, 32)
    door = _depth_door()
    for y in range(4):
        for x in range(4):
            tile = door.crop((x*16, 64-(y+1)*16, (x+1)*16, 64-y*16))
            append('Door_{}_{}'.format(x, y), tile, 64+x*16, 16+y*16)
    # Append above the existing 80px atlas. All older names/rectangles/pixels
    # remain fixed in Unity's bottom-origin coordinates.
    for variant in range(2):
        face_image = _shore_face(variant)
        for y in range(3):
            for x in range(2):
                tile = face_image.crop((x*16, 48-(y+1)*16, (x+1)*16, 48-y*16))
                append('ShoreFace_{}_{}_{}'.format(variant, x, y), tile,
                       variant*32+x*16, 80+y*16)
    for i, side in enumerate(('W', 'E')):
        return_image = _shore_return(side)
        for y in range(3):
            tile = return_image.crop((0, 48-(y+1)*16, 16, 48-y*16))
            append('ShoreReturn_{}_{}'.format(side, y), tile, 64+i*16, 80+y*16)
    for i, side in enumerate(('W', 'E')):
        inside = _shore_inside(side)
        for y in range(3):
            tile = inside.crop((0, 48-(y+1)*16, 16, 48-y*16))
            append('ShoreInside_{}_{}'.format(side, y), tile, i*16, 128+y*16)
    # Wet shores keep their crest above water and the full rise below it.
    # These matching seven-pixel shoulders bridge the side rim to the exposed
    # front strip without drawing the whole submerged wall over high water.
    for i, (name, source) in enumerate((('ShoreLip_W', _shore_return('W')),
                                      ('ShoreLip_E', _shore_return('E')),
                                      ('ShoreLip_Inside_E', _shore_inside('E')))):
        lip = Image.new('RGBA', (16,16))
        lip.paste(source.crop((0,0,16,7)), (0,0))
        if name != 'ShoreLip_Inside_E':
            for y in range(2,7):
                for x in range(1 + (y >= 5)):
                    lip.putpixel((x if name.endswith('_E') else 15-x,y),(0,0,0,0))
        append(name, lip, 32+i*16, 128)
    zone.validate(atlas)
    atlas.save(OUTPUT/'BankJoins16.png')
    (OUTPUT/'DepthManifest.json').write_text(json.dumps(dict(entries=entries), indent=2)+'\n')


def _cliff_earth():
    p = PALETTE
    # A continuous 64px material, sampled by world-column phase. Broad open
    # planes cross several cells instead of repeating one pebble per tile.
    rock = Image.new('RGBA', (64, 48), p['soilmid'])
    d = ImageDraw.Draw(rock)
    for offset in (-64, 0, 64):
        def plane(points, color):
            d.polygon([(xx+offset, yy) for xx, yy in points], fill=p[color])
        plane([(-4,7),(18,7),(18,10),(23,10),(23,16),(19,16),
               (19,23),(13,23),(13,28),(4,28),(4,25),(-4,25)], 'soillight')
        plane([(24,8),(29,8),(29,29),(26,29),(26,40),(19,40),
               (19,43),(7,43),(7,39),(19,39),(19,34),(24,34)], 'soil')
        plane([(35,8),(51,8),(51,11),(57,11),(57,18),(53,18),
               (53,21),(43,21),(43,18),(35,18)], 'soillight')
        plane([(34,24),(40,24),(40,30),(49,30),(49,27),(58,27),
               (58,33),(62,33),(62,42),(46,42),(46,39),(34,39)], 'soil')
        plane([(35,25),(40,25),(40,31),(46,31),(46,34),(35,34)], 'soillight')

    return rock


def cliff_volume(mask, variant=0, height=48):
    """A complete native column: one top cell and its native vertical extrusion.

    All caps, sides, inside corners and feet derive from the same silhouette.
    Unity anchors the sprite to its TOP cell and draws north cells first. The
    next southern top occludes the preceding column, including short notches.
    No separate side/front tilemap or rotated lighting is involved.
    """
    shape = ground.coverage(mask)
    extended = terrain.extended(mask)
    out = Image.new('RGBA', (16, 16+height))
    p = PALETTE

    def top(x, y):
        return -16 <= x < 32 and -16 <= y < 32 and bool(extended.getpixel((x+16, y+16)))

    def body(x, y):
        return any(top(x, yy) for yy in range(max(-16, y-height), min(31, y)+1))

    def rim(x, y):
        """Expose earth around the upper rim, inside the existing footprint.

        A strict vertical extrusion hides these planes from the camera. This
        deliberately wider illustrated shoulder keeps their height readable,
        including around concave corners, without moving a collision boundary.
        Distances cross tile seams; material phases are fixed in world space.
        """
        north = next((i for i in range(1, 9) if not top(x, y-i)), 9)
        south = next((i for i in range(1, 7) if not top(x, y+i)), 7)
        west = next((i for i in range(1, 12) if not top(x-i, y)), 12)
        east = next((i for i in range(1, 12) if not top(x+i, y)), 12)
        wx = x + variant*16
        # Small stepped turf interruptions; no tooth on every native pixel.
        back_width = (5, 5, 6, 6, 5, 6, 6, 5,
                      5, 6, 7, 6, 6, 5, 6, 6)[(wx % 64)//4]
        side_width = (8, 8, 8, 8, 7, 7, 8, 8,
                      9, 9, 8, 8, 8, 7, 8, 8)[y % 16]
        front_width = (4, 4, 3, 3, 4, 4, 4, 3,
                       3, 4, 4, 4, 3, 3, 4, 4)[(wx % 64)//4]
        # The south rim continues into the face: only exposed outer sides
        # need ink. An outline here would split one earth surface in two.
        edge = min(north, west, east)
        if edge == 1:
            return zone.COLORS['ink']
        is_back = north <= back_width
        is_side = min(west, east) <= side_width
        # A shallow earth lip above the tall south face. The same distance
        # field joins it to side shoulders and concave/convex turns; no new
        # silhouette, separate tiles or extra face rows are needed.
        if south <= front_width and (not (is_back or is_side) or south < min(north, west, east)):
            return p['soillight' if south == front_width and wx % 16 in range(2, 12) else 'soilmid']
        if is_back or is_side:
            # Broad attached facets, not loose pebbles. North gets a shallow
            # earth band; east's plane is darker than the lit western return.
            if is_back and (not is_side or north < min(west, east)):
                color = 'soilmid'
                if north == 2: color = 'soil'
                elif (wx + north*2) % 16 in range(3, 10): color = 'soillight'
                if north == back_width and wx % 16 in range(8, 14): color = 'soil'
            else:
                distance = min(west, east)
                color = 'soilmid' if west < east else 'soil'
                facet = y % 8
                # Chamfered four-to-six-pixel faces, with a small underside.
                lo = 3 if facet in (0, 1, 6, 7) else 2
                hi = 5 if facet in (0, 7) else 6 if facet in (1, 6) else 7
                if lo <= distance <= hi and facet < 6:
                    color = 'soillight' if west < east else 'soilmid'
                elif facet == 6 and 3 <= distance <= 6:
                    color = 'soil' if west < east else 'deepsoil'
            return p[color]
        if north == back_width+1 or south == front_width+1 or min(west, east) == side_width+1:
            return p['leafshade']
        return p['grass']

    rock = _cliff_earth()

    for x in range(16):
        ys = [y for y in range(16) if shape.getpixel((x,y))]
        if not ys:
            continue
        first, last = min(ys), max(ys)
        for y in range(first, last+height+1):
            if y < 16 and shape.getpixel((x,y)):
                color = rim(x, y)
            else:
                depth = y-last
                sample_depth = depth-1 if height <= 48 else (depth-1)*48//height
                color = rock.getpixel((x+variant*16, min(47,max(0,sample_depth))))
                # The exposed lip meets the same earth, without the old
                # turf/underside stripe running across the full front.
                if depth >= height-1: color = p['soilcontact']
                if not body(x-1,y) or not body(x+1,y): color = zone.COLORS['ink']
                elif not body(x+2,y): color = p['soil']
                elif not body(x-2,y): color = p['soillight']
                if y == last+height: color = zone.COLORS['ink']
            out.putpixel((x,y), color)
    return out


def cliff_kit():
    """Reproduce the retained three-cell atlas still used by BarrowApproach."""
    rise = 48
    cell_height = 16 + rise
    width, height = 256, 12 * cell_height
    family = 'RaisedEarth'
    atlas = Image.new('RGBA', (width,height))
    entries = []
    for mask in ground.MASKS:
        for variant in range(4):
            index = len(entries); x = (index % 16)*16; y = height-(index//16+1)*cell_height
            im = cliff_volume(mask, variant, rise)
            atlas.paste(im, (x,height-y-cell_height))
            entries.append(dict(name=f'{family}_{mask:03d}_{variant}', material=family,
                                mask=mask, variant=variant, index=index,
                                x=x,y=y,width=16,height=cell_height))
    zone.validate(atlas)
    atlas.save(OUTPUT/'Cliffs16.png')
    (OUTPUT/'CliffManifest.json').write_text(json.dumps(dict(entries=entries),indent=2)+'\n')


def basin_water():
    """Water against authored cliffs, without a second earth bank."""
    source = json.loads((OUTPUT/'TileManifest.json').read_text())['entries']
    source = [e for e in source if e['material'] == 'MeadowWater']
    height = ((len(source)+7)//8)*16
    atlas = Image.new('RGBA', (128, height)); entries = []
    for i, entry in enumerate(source):
        # Authored shore tiles own the boundary. A bank-shaped alpha cutout
        # here would expose a second rim and submerged stair tops at high water.
        im = Image.new('RGBA', (16,16), PALETTE['water'])
        if entry['variant']:
            ImageDraw.Draw(im).line((3+entry['variant'],5,6+entry['variant'],5),fill=PALETTE['waterlight'])
        x, y = (i%8)*16, height-(i//8+1)*16
        atlas.paste(im, (x,height-y-16))
        entries.append(dict(entry, name=entry['name'].replace('MeadowWater','BasinWater'), material='BasinWater', index=i, x=x,y=y))
    zone.validate(atlas)
    atlas.save(OUTPUT/'BasinWater16.png')
    (OUTPUT/'BasinWaterManifest.json').write_text(json.dumps(dict(entries=entries),indent=2)+'\n')

def river_water():
    """A permanent low channel with sediment shores, using the existing 47 masks."""
    source = json.loads((OUTPUT/'TileManifest.json').read_text())['entries']
    source = [e for e in source if e['material'] == 'MeadowWater']
    height = ((len(source)+7)//8)*16
    atlas = Image.new('RGBA', (128, height)); entries = []
    for i, entry in enumerate(source):
        field = terrain.extended(entry['mask'])
        im = Image.new('RGBA', (16,16), PALETTE['basinmud'])
        for y in range(16):
            for x in range(16):
                if not field.getpixel((x+16,y+16)): continue
                edge = terrain.distance(field,x,y,1)
                im.putpixel((x,y), PALETTE['basinsilt'] if edge == 1 else
                            PALETTE['watershade'] if edge == 2 else PALETTE['water'])
        if entry['variant']:
            ImageDraw.Draw(im).line((3+entry['variant'],5,6+entry['variant'],5),fill=PALETTE['waterlight'])
        x, y = (i%8)*16, height-(i//8+1)*16
        atlas.paste(im, (x,height-y-16))
        entries.append(dict(entry, name=entry['name'].replace('MeadowWater','DirtWater'),
                            material='DirtWater', index=i, x=x,y=y))
    zone.validate(atlas)
    atlas.save(OUTPUT/'DirtWater16.png')
    (OUTPUT/'DirtWaterManifest.json').write_text(json.dumps(dict(entries=entries),indent=2)+'\n')


def cell_cliffs():
    """Reusable 16px tiles, independent of scene geometry or cliff height.

    Tops and faces are painted separately; TerrainRuleTile joins their materials.
    Every tile occupies exactly its cell; only exposed edges receive contact.
    """
    entries, images = [], []
    for material in ('Earth', 'Stone'):
        stone = material == 'Stone'
        for part in ('Top', 'Face'):
            for mask in ground.MASKS:
                variants = 6 if part == 'Face' and mask in (255,10) else 4 if stone and part == 'Top' and mask == 255 else 1
                for variant in range(variants):
                    if part == 'Top':
                        p = PALETTE
                        im = Image.new('RGBA', (16, 16), p['stone' if stone else 'grass'])
                        for y in range(16):
                            for x in range(16):
                                color = p['stone' if stone else 'grass']
                                # Conventional top-down projection: thin sides,
                                # a two-pixel rear rim, full front walls BELOW.
                                west = not mask & 8 and x < 4
                                east = not mask & 2 and x >= 12
                                rear = not mask & 1 and y < 2
                                if west or east:
                                    color = p['stoneshade' if stone else 'soilmid']
                                    if x in (0, 15): color = p['rockshadow' if stone else 'soilcontact']
                                    elif x in (3, 12): color = p['stone' if stone else 'leafshade']
                                    elif x == 1: color = p['stonelight' if stone else 'soillight']
                                if rear:
                                    color = p['rockshadow' if stone else 'soilcontact'] if y == 0 else p['stoneshade' if stone else 'leafshade']
                                if not mask & 4 and y == 15 and not (west or east):
                                    color = p['stonelight' if stone else 'grasspatch']
                                im.putpixel((x,y),color)
                        # Close inside turns where both cardinal neighbors exist
                        # but their diagonal is lower. These are the same 4px
                        # side / 2px rear edges, joined within this one cell.
                        d = ImageDraw.Draw(im)
                        contact = p['rockshadow' if stone else 'soilcontact']
                        rim = p['stoneshade' if stone else 'leafshade']
                        for side, cardinal, diagonal in (('East', 2, 16), ('West', 8, 128)):
                            if mask & 1 and mask & cardinal and not mask & diagonal:
                                x0, x1 = (12,15) if side == 'East' else (0,3)
                                d.rectangle((x0,0,x1,1), fill=rim)
                                d.line((x0,0,x1,0), fill=contact)
                        for side, cardinal, diagonal in (('East', 2, 32), ('West', 8, 64)):
                            if mask & 4 and mask & cardinal and not mask & diagonal:
                                x0, x1 = (12,15) if side == 'East' else (0,3)
                                d.rectangle((x0,14,x1,15), fill=rim)
                        if stone and mask == 255:
                            # Quiet bedrock planes, not a floor of outlined bricks.
                            if variant == 1:
                                d.polygon([(3,6),(6,4),(11,4),(12,6),(10,8),(5,8)],fill=p['stonelight'])
                            elif variant == 2:
                                d.line((6,11,10,11),fill=p['stoneshade'])
                                d.line((10,10,12,10),fill=p['stoneshade'])
                        # A one-pixel chamfer softens exposed rear corners without
                        # changing the grid, height or straight joining ports.
                        if not mask & 1:
                            if not mask & 8: im.putpixel((0,0),(0,0,0,0))
                            if not mask & 2: im.putpixel((15,0),(0,0,0,0))
                        # Ground/Paths own the horizontal turf inside every rim.
                        if not stone:
                            for y in range(16):
                                for x in range(16):
                                    if im.getpixel((x,y)) == p['grass']:
                                        im.putpixel((x,y),(0,0,0,0))
                    else:
                        p = PALETTE
                        base = p['stoneshade' if stone else 'soilmid']
                        shade = p['stonejoint' if stone else 'soil']
                        light = p['stone' if stone else 'soillight']
                        contact = p['rockshadow' if stone else 'soilcontact']
                        im = Image.new('RGBA', (16, 16), base)
                        d = ImageDraw.Draw(im)
                        if not mask & 1:
                            # A small irregular turf lip belongs to the wall's
                            # top row, so one level still occupies ONE cell.
                            d.line((0, 0, 15, 0), fill=p['stone' if stone else 'grass'])
                            for a, b, depth in ((0,3,2),(7,10,1),(13,15,2)):
                                d.rectangle((a,1,b,depth), fill=p['stoneshade' if stone else 'leafshade'])
                        if mask != 255 and variant in (1,4):
                            d.line((4,7,7,7),fill=light)
                            d.line((10,10,12,10),fill=shade)
                        # Quiet vertical facets, with clear edges. No outlined
                        # rock on every cell or coordinate-dependent texture phase.
                        if mask == 255 and variant == 1:
                            d.polygon([(3, 3), (10, 3), (12, 5), (10, 8), (7, 8), (5, 10), (3, 8)], fill=light)
                        elif mask == 255 and variant == 4:
                            d.polygon([(11, 3), (13, 5), (13, 11), (9, 13), (5, 13), (6, 11), (10, 10)], fill=shade)
                        elif variant == 2:
                            d.polygon([(3,5),(6,4),(9,5),(9,7),(6,9),(3,8)],fill=light)
                            d.line((5,10,8,10),fill=shade)
                        elif variant == 3 and stone:
                            d.polygon([(7,4),(12,4),(13,7),(11,10),(7,11),(5,9)],fill=light)
                            d.line((4,12,9,12),fill=shade)
                        elif variant == 5:
                            d.polygon([(8,7),(12,6),(13,8),(11,11),(7,12),(7,10)],fill=shade)
                        if not mask & 8:
                            d.line((0, 0, 0, 15), fill=contact)
                            d.line((1, 0, 1, 15), fill=light)
                        if not mask & 2:
                            d.rectangle((14, 0, 15, 15), fill=shade)
                            d.line((15, 0, 15, 15), fill=contact)
                        if not mask & 4:
                            d.rectangle((0, 14, 15, 15), fill=contact)
                        # Concave feet connect to the neighboring exposed edge.
                        if mask & 2 and mask & 4 and not mask & 32:
                            d.rectangle((14, 14, 15, 15), fill=contact)
                        if mask & 8 and mask & 4 and not mask & 64:
                            d.rectangle((0, 14, 1, 15), fill=contact)
                        if not mask & 4:
                            if not mask & 8: im.putpixel((0,15),(0,0,0,0))
                            if not mask & 2: im.putpixel((15,15),(0,0,0,0))
                    index = len(images)
                    images.append(im)
                    entries.append(dict(name=f'Cliff{material}{part}_{mask:03}_{variant}',
                                        material=material+part, mask=mask, variant=variant,
                                        index=index, x=(index % 16)*16, y=0, width=16, height=16))
    # Opaque directional shore slopes share the cliff material. Their turf
    # meets the adjacent floor; water clips the submerged part without exposing
    # the disconnected decorative strips in the older bank sprites.
    for edge in ('Back','West','East','WestBack','EastBack','WestBackOuter','EastBackOuter'):
        im = Image.new('RGBA', (16,16), PALETTE['soilmid'])
        for y in range(16):
            for x in range(16):
                side = x < 4 if edge.startswith('West') else x >= 12
                back = y >= 12
                turf = back if edge == 'Back' else side if edge in ('West','East') else (side and back if edge.endswith('Outer') else side or back)
                if turf: im.putpixel((x,y),PALETTE['grass'])
        index = len(images)
        images.append(im)
        entries.append(dict(name='CliffBank_'+edge,material='Bank',mask=0,variant=0,index=index,x=(index%16)*16,y=0,width=16,height=16))
    # Plain repeatable wall row for a face continuing into the next elevation
    # layer (e.g. the lower half of a +1 -> -1 drop submerged by pond water).
    for material in ('Earth', 'Stone'):
        stone = material == 'Stone'
        im = Image.new('RGBA',(16,16),PALETTE['stoneshade' if stone else 'soilmid'])
        d=ImageDraw.Draw(im)
        d.line((0,0,15,0),fill=PALETTE['stone' if stone else 'grass'])
        for a,b,depth in ((0,3,2),(7,10,1),(13,15,2)):
            d.rectangle((a,1,b,depth),fill=PALETTE['stoneshade' if stone else 'leafshade'])
        d.polygon([(3,5),(6,4),(10,5),(11,7),(8,9),(4,8)],fill=PALETTE['stone' if stone else 'soillight'])
        d.line((8,11,12,11),fill=PALETTE['stonejoint' if stone else 'soil'])
        index=len(images);images.append(im)
        entries.append(dict(name=f'Cliff{material}Face_Continue',material='Piece',mask=0,variant=0,index=index,x=index%16*16,y=0,width=16,height=16))
        for row in ('Middle','Foot'):
            im=Image.new('RGBA',(16,16),PALETTE['stoneshade' if stone else 'soilmid'])
            d=ImageDraw.Draw(im)
            d.polygon([(7,3),(11,4),(12,7),(9,9),(5,9),(5,7)],fill=PALETTE['stone' if stone else 'soillight'])
            d.line((3,11,7,11),fill=PALETTE['stonejoint' if stone else 'soil'])
            if row=='Foot':d.rectangle((0,14,15,15),fill=PALETTE['rockshadow' if stone else 'soilcontact'])
            index=len(images);images.append(im)
            entries.append(dict(name=f'Cliff{material}Face_{row}',material='Piece',mask=0,variant=0,index=index,x=index%16*16,y=0,width=16,height=16))
    for side in ('Left','Center','Right'):
        im=Image.new('RGBA',(16,16),PALETTE['stone'])
        d=ImageDraw.Draw(im)
        for y in (3,7,11,15):
            d.line((0,y,15,y),fill=PALETTE['stonejoint'])
            if y<15:d.line((0,y+1,15,y+1),fill=PALETTE['stonelight'])
        if side=='Left':d.rectangle((0,0,1,15),fill=PALETTE['stonejoint'])
        if side=='Right':d.rectangle((14,0,15,15),fill=PALETTE['stonejoint'])
        index=len(images);images.append(im)
        entries.append(dict(name=f'StoneStairs_Single_{side}',material='Piece',mask=0,variant=0,index=index,x=index%16*16,y=0,width=16,height=16))
    # Debug sprites also own the custom physics outlines on the separate
    # collision Tilemap. Bit 1=rear, 2=east, 4=west. Never a full-cell side wall.
    for mask in range(1,8):
        im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
        if mask&1:d.rectangle((0,0,15,1),fill=PALETTE['rockshadow'])
        if mask&2:d.rectangle((12,0,15,15),fill=PALETTE['rockshadow'])
        if mask&4:d.rectangle((0,0,3,15),fill=PALETTE['rockshadow'])
        index=len(images);images.append(im)
        entries.append(dict(name=f'CliffCollision_{mask}',material='Collision',mask=mask,variant=0,index=index,x=index%16*16,y=0,width=16,height=16))
    height = ((len(images)+15)//16)*16
    atlas = Image.new('RGBA', (256, height))
    for entry, im in zip(entries, images):
        y = (entry['index']//16)*16
        atlas.paste(im, (entry['x'], y))
        entry['y'] = height-y-16
    zone.validate(atlas)
    atlas.save(OUTPUT/'CliffTiles16.png')
    (OUTPUT/'CliffTilesManifest.json').write_text(json.dumps(dict(entries=entries), indent=2)+'\n')


if __name__ == '__main__' and '--cell-cliffs-only' in sys.argv:
    cell_cliffs()
elif __name__ == '__main__' and '--basin-water-only' in sys.argv:
    basin_water()
elif __name__ == '__main__' and '--river-water-only' in sys.argv:
    river_water()
elif __name__ == '__main__' and '--cliffs-only' in sys.argv:
    cliff_kit()
elif __name__ == '__main__' and '--depth-only' in sys.argv:
    depth_edges()
elif __name__ == '__main__':
    terrain.main(OUTPUT,PREVIEW,'Terrain16','shared-terrain-atlas',
                 PALETTE,'SHARED TERRAIN / 16px / MEADOW',
                 render_blob=blob,render_wall=wall,render_face=face,render_stairs=stairs,
                 render_water=water_detail)
    zone.validate(Image.open(OUTPUT/'Terrain16.png').convert('RGBA'))
