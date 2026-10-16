"""Native 16-PPU Ardan architecture, drawn directly as reusable pixel clusters.

The kit shares the approved Lowlands stone/timber ramps. Broad cardinal roofs,
short stone courses and functional bronze sluice hardware establish a quieter
regional building grammar. No existing art, import metadata or scene is changed.

Run with the project's Pillow environment:
    $env:PYTHONPATH='tmp/art-python'; python Tools/Art/World/build_lowlands_architecture.py
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'RoadOfTheOldKing/Assets/Art/Tiles/GreenLowlands/Architecture'
PPU = 16


def rgb(value):
    return tuple(bytes.fromhex(value)) + (255,)


import lowlands_palette as zone

STONE = zone.ramp('rockshadow', 'stonejoint', 'stoneshade', 'stone', 'stonelight')
TIMBER = zone.ramp('deepsoil', 'soilmid', 'soillight')
SERVICE_TIMBER = zone.ramp('deepsoil', 'soil', 'soilmid')
ROOF = zone.ramp('slatedark', 'slate', 'slatelight')
BRONZE = zone.ramp('soil', 'soillight', 'soilshine')
WATER = zone.ramp('waterdeep', 'water', 'waterlight', 'stonelight')
GRASS = zone.COLORS['grass']
WORK_FLOOR = zone.ramp('slate', 'slatelight', 'stoneshade')
WALK_FLOOR = zone.ramp('basinsilt', 'basinmud', 'basinpatch')
IRON = zone.ramp('slatedark', 'slate', 'slatelight', 'stone')


def blank(w=16, h=16):
    return Image.new('RGBA', (w, h))


def pixels(im):
    # Keep the exporter compatible with the project's older Pillow installs.
    return im.get_flattened_data() if hasattr(im, 'get_flattened_data') else im.getdata()


def roof(height):
    """Horizontal ridge and screen-axis eaves; not an isometric building.

    The eave projection is included in the canvas. Place above a 10x9 or 10x7
    floor; renderer visibility is controlled by the building's cutaway, never
    by changing the gameplay camera. A single thin contrasting ridge separates
    broad lit and shaded slopes. Tile marks remain short, sparse and staggered.
    """
    im = blank(160, height)
    d = ImageDraw.Draw(im)
    end = height - 1
    ridge = (height - 14) // 2
    silhouette = [(5, 0), (154, 0), (154, 5), (157, 5), (157, 11),
                  (159, 11), (159, end-9), (156, end-9), (156, end-3),
                  (151, end-3), (151, end), (8, end), (8, end-3),
                  (3, end-3), (3, end-9), (0, end-9), (0, 11),
                  (2, 11), (2, 5), (5, 5)]
    d.polygon(silhouette, fill=ROOF[0])
    # Upper slope: a quiet pale plane, warmed only by its thin timber bargeboard.
    d.rectangle((7, 3, 152, ridge-2), fill=ROOF[2])
    d.rectangle((4, 13, 155, ridge-2), fill=ROOF[2])
    # Lower slope shares the same outline and runs to a supported timber eave.
    d.rectangle((4, ridge+3, 155, end-12), fill=ROOF[1])
    d.rectangle((7, end-11, 152, end-7), fill=ROOF[1])
    # Avoid noisy all-over gridding: alternate courses contain two short seams
    # per broad shingle; substantial untouched planes remain between courses.
    for row, y in enumerate(range(12, ridge-5, 12)):
        offset = 10 if row % 2 else 0
        for col, x in enumerate(range(8-offset, 151, 22)):
            x0, x1 = max(7, x), min(152, x+19)
            if x1 <= x0:
                continue
            d.line((x0, y+7, min(x1, x0+16), y+7), fill=ROOF[1])
            if (row+col) % 3 == 0:
                d.line((x0+2, y+8, min(x1, x0+10), y+8), fill=ROOF[1])
            if (row+col) % 3 != 0:
                d.line((x1, y+2, x1, y+6), fill=ROOF[1])
    for row, y in enumerate(range(ridge+8, end-18, 12)):
        offset = 10 if row % 2 else 0
        for col, x in enumerate(range(8-offset, 151, 22)):
            x0, x1 = max(7, x), min(152, x+19)
            if x1 <= x0:
                continue
            # Low contrast highlight tells the leading edge; dark marks are
            # confined to rare joints instead of outlining every roof tile.
            d.line((x0+1, y, min(x1, x0+16), y), fill=ROOF[2])
            if (row+col) % 3 == 0:
                d.line((x0+2, y+1, min(x1, x0+7), y+1), fill=ROOF[2])
            if (row+col) % 3 == 1:
                d.line((x1, y+5, x1, y+8), fill=ROOF[0])
                d.line((max(x0+1,x1-4), y+8, x1, y+8), fill=ROOF[0])
    # Ridge stones are broad, with one offset joint per 32 native pixels.
    d.rectangle((2, ridge-2, 157, ridge+3), fill=ROOF[0])
    d.rectangle((4, ridge-2, 155, ridge), fill=ROOF[2])
    d.line((4, ridge+1, 155, ridge+1), fill=ROOF[1])
    for x in range(23, 150, 32):
        d.line((x, ridge-2, x, ridge), fill=ROOF[1])
    # Strong lower edge and short brackets describe the wall's roof projection.
    d.rectangle((7, end-6, 152, end-4), fill=TIMBER[0])
    d.line((8, end-6, 151, end-6), fill=TIMBER[1])
    for x in (15, 47, 79, 111, 143):
        d.rectangle((x, end-4, x+3, end-1), fill=TIMBER[0])
        d.line((x, end-4, x, end-2), fill=TIMBER[1])
    # Two deliberate weather patches, not diffuse noise or moss stippling.
    d.line((18, 30, 24, 30), fill=ROOF[1])
    d.line((19, 31, 22, 31), fill=ROOF[1])
    d.line((125, ridge+23, 132, ridge+23), fill=ROOF[2])
    return im


def wall(variant):
    im = Image.new('RGBA', (16, 16), STONE[1])
    d = ImageDraw.Draw(im)
    # Three courses are intentionally simple so tiled walls read as one mass.
    for row, (y0, y1) in enumerate(((0, 4), (6, 10), (12, 15))):
        offset = (variant*3 + row*7) % 16
        for x in (offset-17, offset, offset+17):
            d.rectangle((x+1, y0, x+15, y1), fill=STONE[2])
            d.line((x+2, y0, x+12, y0), fill=STONE[3])
            if row == 0 and variant == 1:
                d.line((x+3, y0, x+6, y0), fill=STONE[4])
            if row == 1 and variant == 2:
                d.point((x+14, y1), fill=STONE[0])
    return im


def floor(variant):
    im = Image.new('RGBA', (16, 16), STONE[2])
    d = ImageDraw.Draw(im)
    if variant == 1:
        d.line((3, 10, 8, 10), fill=STONE[3]); d.point((9, 9), fill=STONE[3])
    elif variant == 2:
        d.line((10, 3, 12, 3), fill=STONE[1]); d.point((13, 4), fill=STONE[1])
    elif variant == 3:
        d.polygon([(3, 4), (8, 4), (8, 5), (11, 5), (11, 7), (6, 7), (6, 6), (3, 6)], fill=STONE[3])
    return im


def timber(kind):
    im = blank(); d = ImageDraw.Draw(im)
    if kind in ('Post', 'Cross'):
        d.rectangle((5, 0, 10, 15), fill=STONE[0])
        d.rectangle((6, 0, 9, 15), fill=TIMBER[0])
        d.line((6, 0, 6, 15), fill=TIMBER[2])
        d.line((7, 2, 7, 7), fill=TIMBER[1])
    if kind in ('Beam', 'Cross'):
        d.rectangle((0, 5, 15, 10), fill=STONE[0])
        d.rectangle((0, 6, 15, 9), fill=TIMBER[0])
        d.line((0, 6, 15, 6), fill=TIMBER[2])
        d.line((2, 7, 8, 7), fill=TIMBER[1])
    if kind == 'Cross':
        d.point((7, 8), fill=STONE[0])
    return im


def foundation(kind):
    im = blank(); d = ImageDraw.Draw(im)
    if kind == 'Cap':
        d.rectangle((0, 4, 15, 15), fill=STONE[0])
        d.rectangle((0, 4, 15, 11), fill=STONE[2])
        d.rectangle((0, 4, 15, 6), fill=STONE[3])
        d.line((1, 4, 12, 4), fill=STONE[4])
    elif kind == 'Foot':
        d.rectangle((0, 0, 15, 9), fill=STONE[2])
        d.rectangle((0, 10, 15, 12), fill=STONE[1])
        d.rectangle((0, 13, 15, 15), fill=STONE[0])
        d.line((2, 3, 8, 3), fill=STONE[3])
    elif kind in ('West', 'East'):
        west = kind == 'West'
        x = 10 if west else 0
        d.rectangle((x, 0, x+5, 15), fill=STONE[0])
        d.rectangle((x+1, 0, x+4, 15), fill=STONE[2])
        d.line((x+2 if west else x+1, 0, x+2 if west else x+1, 15), fill=STONE[3])
        d.line((x+2, 8, x+4, 8), fill=STONE[1])
    else:
        west = kind.startswith('West')
        cap = kind.endswith('Cap')
        im = foundation('Cap' if cap else 'Foot'); d = ImageDraw.Draw(im)
        # A joined outer corner, with the strip's horizontal seam unchanged.
        edge = 0 if west else 15
        d.line((edge, 4 if cap else 0, edge, 15), fill=STONE[0])
        d.rectangle((0 if west else 13, 0, 2 if west else 15, 4), fill=(0, 0, 0, 0))
    return im


def throat(kind):
    im = blank(); d = ImageDraw.Draw(im)
    if kind in ('West', 'East'):
        left = kind == 'West'
        a,b = (0, 11) if left else (4, 15)
        d.rectangle((a, 0, b, 15), fill=STONE[0])
        d.rectangle((a+1, 0, b-1, 15), fill=STONE[2])
        d.rectangle((a+2, 0, b-3, 15), fill=STONE[3])
        d.line((a+1, 9, b-1, 9), fill=STONE[1])
        d.line((a+2, 10, b-4, 10), fill=STONE[4])
    elif kind == 'Lintel':
        d.rectangle((0, 0, 15, 13), fill=STONE[0])
        d.rectangle((0, 1, 15, 9), fill=STONE[2])
        d.rectangle((0, 1, 15, 3), fill=STONE[3])
        d.line((0, 1, 15, 1), fill=STONE[4])
        d.line((12, 3, 12, 8), fill=STONE[1])
    elif kind == 'Apron':
        d.rectangle((0, 0, 15, 15), fill=STONE[1])
        d.rectangle((0, 2, 15, 8), fill=STONE[2])
        d.line((0, 2, 15, 2), fill=STONE[3])
        d.line((8, 3, 8, 8), fill=STONE[1])
        d.rectangle((0, 11, 15, 15), fill=STONE[2])
        d.line((0, 11, 15, 11), fill=STONE[3])
    return im


def gate():
    im = blank(64, 48); d = ImageDraw.Draw(im)
    # Side guides are separate from the timber shutter's visible travel plane.
    for x in (2, 57):
        d.rectangle((x, 0, x+4, 47), fill=STONE[0])
        d.rectangle((x+1, 1, x+2, 45), fill=BRONZE[1])
        d.line((x+1, 1, x+1, 43), fill=BRONZE[2])
    d.rectangle((8, 3, 55, 43), fill=STONE[0])
    for n, x in enumerate(range(9, 55, 8)):
        d.rectangle((x, 4, x+6, 42), fill=TIMBER[0])
        d.line((x+1, 5, x+1, 41), fill=TIMBER[1])
        if n % 2:
            d.line((x+4, 14, x+4, 22), fill=STONE[0])
    for y in (9, 33):
        d.rectangle((7, y, 56, y+5), fill=BRONZE[0])
        d.line((8, y, 55, y), fill=BRONZE[2])
        d.rectangle((8, y+1, 55, y+3), fill=BRONZE[1])
        for x in (12, 27, 42, 51):
            d.point((x, y+2), fill=STONE[0])
    # Orthogonal central spine keeps the shutter's motion unambiguous.
    d.rectangle((29, 0, 34, 41), fill=BRONZE[0])
    d.rectangle((30, 1, 32, 40), fill=BRONZE[1])
    d.line((30, 1, 30, 40), fill=BRONZE[2])
    d.rectangle((9, 44, 54, 46), fill=STONE[0])
    return im


def hoist():
    im = blank(64, 32); d = ImageDraw.Draw(im)
    for x in (3, 56):
        d.rectangle((x, 9, x+4, 31), fill=STONE[0])
        d.rectangle((x+1, 10, x+3, 31), fill=BRONZE[0])
        d.line((x+1, 10, x+1, 31), fill=BRONZE[1])
    d.rectangle((1, 6, 62, 13), fill=STONE[0])
    d.rectangle((2, 7, 61, 10), fill=BRONZE[1])
    d.line((3, 7, 60, 7), fill=BRONZE[2])
    for x in (17, 43):
        for y in range(14, 31, 4):
            d.rectangle((x, y, x+2, y+3), fill=BRONZE[0])
            d.line((x+1, y, x+1, y+1), fill=BRONZE[2])
    d.rectangle((26, 3, 37, 16), fill=BRONZE[0])
    d.rectangle((25, 5, 38, 13), fill=BRONZE[0])
    d.rectangle((28, 5, 35, 12), fill=BRONZE[1])
    d.line((28, 5, 35, 5), fill=BRONZE[2])
    d.rectangle((30, 7, 33, 10), fill=STONE[0])
    # Crank silhouette and a shaft disappearing into the gate's central spine.
    d.rectangle((32, 0, 43, 2), fill=BRONZE[1])
    d.rectangle((41, 0, 44, 5), fill=BRONZE[0])
    d.line((32, 0, 42, 0), fill=BRONZE[2])
    d.rectangle((30, 16, 33, 31), fill=BRONZE[0])
    d.line((31, 16, 31, 31), fill=BRONZE[1])
    return im


def flow(frame):
    im = blank(48, 32); d = ImageDraw.Draw(im)
    # Downward water ribbons and small stepped crests, never soft foam particles.
    d.polygon([(5, 0), (42, 0), (42, 10), (44, 10), (44, 19), (46, 19),
               (46, 30), (1, 30), (1, 20), (3, 20), (3, 9), (5, 9)], fill=WATER[0])
    d.rectangle((6, 0, 41, 28), fill=WATER[1])
    for j, x in enumerate((8, 18, 29, 38)):
        y = (j*7 + frame*3) % 24
        length = 8 if j % 2 else 13
        y1 = min(28, y+length)
        d.rectangle((x, y, x+1, y1), fill=WATER[2])
        if j % 2 == 0:
            d.line((x, y, x, min(y1, y+3)), fill=WATER[3])
    phase = frame % 2
    d.line((3, 28+phase, 12, 28+phase), fill=WATER[2])
    d.line((16, 30-phase, 31, 30-phase), fill=WATER[2])
    d.line((35, 28+phase, 44, 28+phase), fill=WATER[2])
    return im


def crest():
    im = blank(32, 32); d = ImageDraw.Draw(im)
    d.polygon([(8, 3), (23, 3), (28, 8), (28, 23), (23, 28), (8, 28),
               (3, 23), (3, 8)], fill=BRONZE[0])
    d.polygon([(9, 5), (22, 5), (26, 9), (26, 22), (22, 26), (9, 26),
               (5, 22), (5, 9)], fill=BRONZE[1])
    d.ellipse((8, 8, 23, 23), fill=BRONZE[0])
    d.ellipse((10, 10, 21, 21), fill=BRONZE[1])
    d.rectangle((15, 7, 16, 24), fill=BRONZE[0])
    d.rectangle((7, 15, 24, 16), fill=BRONZE[0])
    d.rectangle((14, 14, 17, 17), fill=BRONZE[2])
    d.line((10, 5, 21, 5), fill=BRONZE[2])
    d.line((5, 10, 5, 21), fill=BRONZE[2])
    return im


def vent():
    im = blank(32, 32); d = ImageDraw.Draw(im)
    d.rectangle((8, 9, 23, 30), fill=STONE[0])
    d.rectangle((10, 10, 21, 27), fill=TIMBER[0])
    for y in (12, 17, 22):
        d.line((11, y, 20, y), fill=TIMBER[2])
        d.line((11, y+1, 20, y+1), fill=STONE[0])
    d.polygon([(8, 2), (23, 2), (23, 4), (26, 4), (26, 7),
               (29, 7), (29, 10), (2, 10), (2, 7), (5, 7), (5, 4), (8, 4)], fill=ROOF[0])
    d.rectangle((9, 3, 22, 5), fill=ROOF[2])
    d.rectangle((6, 6, 25, 8), fill=ROOF[1])
    return im


def service_floor(variant):
    """Flat walking planks; low contrast grain keeps them distinct from walls."""
    shades = SERVICE_TIMBER
    im = Image.new('RGBA', (16, 16), shades[1]); d = ImageDraw.Draw(im)
    for row, y in enumerate((0, 6, 11)):
        d.line((0, y, 15, y), fill=shades[0])
        joint = (row*7+variant*5+3) % 16
        if (row+variant) % 2 == 0:
            d.line((joint, y+1, joint, min(15, y+4)), fill=shades[0])
        if row != variant:
            start = (variant*4+row*3) % 10
            d.line((start, y+1, min(15, start+6), y+1), fill=shades[2])
    # A single shallow scrape suggests traffic without becoming floor noise.
    if variant == 2:
        d.line((5, 9, 9, 9), fill=shades[2])
    return im


def drive_cover(vertical=False):
    """Recessed mechanical run with raised brass collar and visible drive rod."""
    im = blank(); d = ImageDraw.Draw(im)

    def band(box, color):
        x0,y0,x1,y1 = box
        d.rectangle((y0,x0,y1,x1) if vertical else box, fill=color)

    # Opposite tile edges match; a sequence makes one continuous protected run.
    band((0,3,15,12),STONE[0])
    band((0,4,15,11),BRONZE[0])
    band((0,4,15,4),BRONZE[1])
    band((0,6,15,10),ROOF[0])
    band((0,7,15,8),BRONZE[0])
    band((0,7,15,7),BRONZE[1])
    band((6,4,9,11),BRONZE[0])
    band((6,4,8,10),BRONZE[1])
    band((6,4,6,10),BRONZE[2])
    band((7,5,7,5),STONE[0])
    band((7,10,7,10),STONE[0])
    return im


def intake_grille():
    """Deep open water mouth, wide enough between bars to remain readable."""
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle((0,0,15,15),fill=STONE[0])
    d.rectangle((1,1,14,13),fill=ROOF[0])
    d.line((1,1,14,1),fill=STONE[1])
    d.line((0,0,15,0),fill=STONE[2])
    for x in (3,7,11):
        d.rectangle((x,2,x+1,14),fill=BRONZE[0])
        d.line((x,2,x,12),fill=BRONZE[1])
    d.line((1,14,14,14),fill=BRONZE[0])
    d.line((1,13,14,13),fill=BRONZE[1])
    return im


def south_roof(height, wing=False):
    """Long cardinal gable: vertical ridge, two planes, south-facing gable.

    The diagonal lines are the roof pitch in elevation, not an oblique ground
    footprint. The lowest pixels are eave brackets; a separate facade below
    them can place its doorway over the actual walking approach.
    """
    im = blank(160, height)
    d = ImageDraw.Draw(im)
    front = height-12
    peak = front-38
    left = [(79, 1), (3, 39), (3, front), (79, peak)]
    right = [(80, 1), (156, 39), (156, front), (80, peak)]
    d.polygon([(79,0),(81,0),(159,39),(159,front+5),(153,front+5),
               (153,height-2),(6,height-2),(6,front+5),(0,front+5),
               (0,39)], fill=ROOF[0])
    d.polygon(left,fill=ROOF[2]); d.polygon(right,fill=ROOF[1])
    # Quiet, broad slate courses follow the roof's north/south ridge. Marks
    # are clipped to each plane so none crosses the gable or breaks its mass.
    for side, poly, shade in ((0,left,ROOF[1]),(1,right,ROOF[0])):
        mask=Image.new('1',im.size,0); ImageDraw.Draw(mask).polygon(poly,fill=1)
        marks=blank(*im.size); md=ImageDraw.Draw(marks)
        for col,x in enumerate(range(10 if side==0 else 93,76 if side==0 else 154,16)):
            for row,y in enumerate(range(18+((col%2)*11),front,25)):
                md.line((x,y,x,y+16),fill=shade)
                md.line((x,y+16,x+8,y+16),fill=shade)
                if (col+row+int(wing))%4==0:
                    md.line((x+3,y+5,x+3,y+10),fill=ROOF[2] if side else ROOF[1])
        marks.putalpha(Image.composite(marks.getchannel('A'),Image.new('L',im.size),mask))
        im.alpha_composite(marks)
    d=ImageDraw.Draw(im)
    # Eaves and the lifted ridge establish a clear volume with one light side.
    d.line((3,40,3,front),fill=TIMBER[1],width=2)
    d.line((157,40,157,front+2),fill=TIMBER[0],width=2)
    d.rectangle((77,2,82,peak),fill=ROOF[0])
    d.rectangle((78,3,80,peak-1),fill=ROOF[2])
    for y in range(23,peak,28): d.line((78,y,80,y),fill=ROOF[1])
    # Warm solid gable under the two roof slopes. Timber braces, not brass.
    d.polygon([(5,front),(79,peak),(155,front)],fill=TIMBER[0])
    d.polygon([(14,front-2),(79,peak+6),(145,front-2)],fill=TIMBER[1])
    d.line((7,front-1,79,peak+1,153,front-1),fill=ROOF[0],width=5)
    d.line((7,front-3,79,peak-1),fill=TIMBER[2],width=2)
    d.line((81,peak+1,153,front-1),fill=TIMBER[0],width=3)
    d.rectangle((76,peak+7,82,front+1),fill=TIMBER[0])
    d.line((77,peak+9,77,front),fill=TIMBER[2])
    for x in (42,114):
        d.rectangle((x,front-13,x+3,front),fill=TIMBER[0])
    # Recessed ventilation, deliberately neutral so it cannot read as a lever.
    if wing:
        d.rectangle((62,front-14,71,front-5),fill=ROOF[0])
        d.rectangle((89,front-14,98,front-5),fill=ROOF[0])
        for x in (64,68,91,95): d.line((x,front-12,x,front-7),fill=TIMBER[0])
    else:
        for x in (58,88):
            d.rectangle((x,front-18,x+12,front-6),fill=ROOF[0])
            for y in (front-15,front-11): d.line((x+2,y,x+10,y),fill=TIMBER[0])
    d.rectangle((6,front+1,153,front+5),fill=ROOF[0])
    d.line((7,front+1,152,front+1),fill=TIMBER[2])
    d.line((8,front+2,151,front+2),fill=TIMBER[0])
    for x in (10,43,112,145):
        d.rectangle((x,front+5,x+3,height-2),fill=TIMBER[0])
        d.line((x,front+5,x,height-3),fill=TIMBER[1])
    return im


def quiet_floor(variant, timber_floor=False):
    shades=WALK_FLOOR if timber_floor else WORK_FLOOR
    im=Image.new('RGBA',(16,16),shades[1]); d=ImageDraw.Draw(im)
    if timber_floor:
        # Full 16-pixel wide boards; weak seams let the moving mechanisms lead.
        d.line((0,0,15,0),fill=shades[0])
        if variant==1: d.line((2,5,8,5),fill=shades[2])
        if variant==2: d.line((9,11,13,11),fill=shades[0])
        if variant==3: d.line((12,1,12,4),fill=shades[0])
    elif variant:
        if variant==1: d.rectangle((3,8,6,8),fill=shades[2])
        if variant==2: d.line((11,4,13,4),fill=shades[0])
        if variant==3: d.polygon([(5,11),(8,11),(8,12),(10,12),(10,13),(5,13)],fill=shades[2])
    return im


def track(kind):
    """Continuous neutral channels. No gold strip competes with the controls."""
    im=blank();d=ImageDraw.Draw(im)
    if kind=='H':
        d.rectangle((0,4,15,11),fill=IRON[0]);d.line((0,4,15,4),fill=IRON[2])
        d.rectangle((0,6,15,9),fill=IRON[1]);d.line((0,6,15,6),fill=IRON[2])
    elif kind=='V':
        d.rectangle((4,0,11,15),fill=IRON[0]);d.line((4,0,4,15),fill=IRON[2])
        d.rectangle((6,0,9,15),fill=IRON[1]);d.line((6,0,6,15),fill=IRON[2])
    elif kind=='Corner':
        d.rectangle((4,0,11,11),fill=IRON[0]);d.rectangle((4,4,15,11),fill=IRON[0])
        d.line((4,0,4,4,15,4),fill=IRON[2]);d.rectangle((6,0,9,9),fill=IRON[1])
        d.rectangle((6,6,15,9),fill=IRON[1]);d.line((6,0,6,6,15,6),fill=IRON[2])
    else:
        d.rectangle((1,4,14,11),fill=IRON[0]);d.rectangle((3,5,12,9),fill=IRON[1])
        d.line((3,5,12,5),fill=IRON[3]);d.rectangle((6,2,9,13),fill=IRON[0])
        d.line((6,3,6,12),fill=IRON[2])
    return im


def machine(kind):
    sizes={'Fork':(16,24),'Crank':(24,24),'Brake':(16,16),
           'Catch':(16,20),'Counterweight':(16,24),'Axle':(16,16)}
    im=blank(*sizes[kind]);d=ImageDraw.Draw(im)
    if kind=='Fork':
        # Open U is recognizable at the same scale as the player's axe head.
        d.rectangle((2,16,13,22),fill=IRON[0]);d.rectangle((3,16,12,20),fill=IRON[2])
        d.line((3,16,12,16),fill=IRON[3]);d.rectangle((6,9,9,18),fill=BRONZE[0])
        d.rectangle((7,9,8,17),fill=BRONZE[1])
        d.polygon([(1,2),(4,2),(4,10),(11,10),(11,2),(14,2),(14,12),
                   (11,15),(4,15),(1,12)],fill=IRON[0])
        d.polygon([(2,2),(4,2),(4,11),(11,11),(11,2),(13,2),(13,12),
                   (10,14),(5,14),(2,11)],fill=BRONZE[1])
        d.line((2,2,2,10),fill=BRONZE[2]);d.line((12,2,12,10),fill=BRONZE[2])
        d.line((4,12,10,12),fill=BRONZE[2])
    elif kind=='Crank':
        # A real wheel, spoke and projecting wooden grip: the only round input.
        d.rectangle((8,17,17,22),fill=IRON[0]);d.rectangle((9,17,16,20),fill=IRON[1])
        d.ellipse((2,1,20,19),fill=IRON[0]);d.ellipse((3,2,19,18),fill=BRONZE[1])
        d.ellipse((5,4,17,16),fill=IRON[0]);d.ellipse((6,5,16,15),fill=(0,0,0,0))
        d.arc((3,2,19,18),180,300,fill=BRONZE[2],width=2)
        d.line((11,4,11,16),fill=BRONZE[0],width=2)
        d.line((5,10,17,10),fill=BRONZE[1],width=2)
        d.rectangle((9,8,13,12),fill=IRON[0]);d.rectangle((10,9,12,11),fill=BRONZE[2])
        d.line((12,10,18,6),fill=BRONZE[1],width=2)
        d.rectangle((17,2,22,7),fill=IRON[0]);d.rectangle((18,3,21,5),fill=TIMBER[2])
        d.line((18,3,21,3),fill=BRONZE[2])
    elif kind=='Brake':
        # Sliding horizontal shoe; flat visible grip differs from wheel/fork.
        d.rectangle((1,1,14,14),fill=IRON[0]);d.rectangle((2,2,13,12),fill=IRON[1])
        d.line((2,2,13,2),fill=IRON[2]);d.rectangle((6,0,9,15),fill=IRON[0])
        d.line((6,0,6,15),fill=IRON[2]);d.rectangle((3,5,12,10),fill=BRONZE[0])
        d.rectangle((3,5,11,8),fill=BRONZE[1]);d.line((3,5,11,5),fill=BRONZE[2])
        d.rectangle((5,7,9,8),fill=IRON[0])
    elif kind=='Catch':
        # A hooked pawl visibly bears against its toothed rack.
        d.rectangle((5,0,10,18),fill=IRON[0]);d.rectangle((6,0,9,17),fill=IRON[1])
        for y in (1,5,9,13):d.rectangle((10,y,12,y+1),fill=IRON[2])
        d.polygon([(2,4),(5,4),(5,10),(10,10),(10,8),(14,8),(14,13),(9,15),(3,13),(2,10)],fill=IRON[0])
        d.polygon([(3,5),(4,5),(4,11),(10,11),(10,9),(13,9),(13,12),(9,14),(4,12),(3,10)],fill=BRONZE[1])
        d.line((3,5,3,10,5,12,9,13),fill=BRONZE[2])
        d.rectangle((1,15,14,19),fill=IRON[0]);d.line((2,15,13,15),fill=IRON[2])
    elif kind=='Counterweight':
        # Dense iron weight, kept cooler/darker than interactive bronze inputs.
        d.rectangle((6,0,9,5),fill=IRON[0]);d.line((7,0,7,4),fill=IRON[3])
        d.polygon([(3,4),(12,4),(14,7),(14,21),(12,23),(3,23),(1,21),(1,7)],fill=IRON[0])
        d.rectangle((3,6,12,20),fill=IRON[1]);d.line((3,6,12,6),fill=IRON[3])
        d.line((3,7,3,20),fill=IRON[2]);d.rectangle((6,7,9,19),fill=IRON[0])
        for y in (10,17):d.line((2,y,13,y),fill=IRON[0])
    else:
        d.ellipse((1,1,14,14),fill=IRON[0]);d.ellipse((3,3,12,12),fill=IRON[2])
        d.ellipse((5,5,10,10),fill=IRON[0]);d.rectangle((7,6,8,9),fill=IRON[3])
    return im


def low_wall(kind):
    im=blank();d=ImageDraw.Draw(im)
    if kind=='Face':
        d.rectangle((0,0,15,15),fill=STONE[1]);d.rectangle((0,0,15,3),fill=STONE[3])
        d.line((0,0,15,0),fill=STONE[4]);d.rectangle((0,5,15,12),fill=STONE[2])
        d.line((11,5,11,12),fill=STONE[1]);d.rectangle((0,14,15,15),fill=STONE[0])
    elif kind=='Pier':
        d.rectangle((4,0,11,15),fill=STONE[0]);d.rectangle((5,1,10,13),fill=STONE[2])
        d.line((5,1,5,13),fill=STONE[3]);d.line((5,7,10,7),fill=STONE[1])
    else:
        d.rectangle((0,0,15,15),fill=STONE[2]);d.rectangle((0,0,15,3),fill=STONE[3])
        d.line((1,0,14,0),fill=STONE[4]);d.line((15,3,15,15),fill=STONE[1])
        d.line((4,11,8,11),fill=STONE[1])
    return im


def south_facade(width=160, door=True):
    """Two-and-a-half unit wall mass under a south gable, separate for cutaway."""
    im=blank(width,48);d=ImageDraw.Draw(im)
    d.rectangle((3,0,width-4,46),fill=STONE[0])
    d.rectangle((5,1,width-6,43),fill=STONE[2]);d.rectangle((5,1,width-6,24),fill=STONE[3])
    for y in (29,39):
        d.line((5,y,width-6,y),fill=STONE[1])
        for x in range(11+(y%3)*8,width-10,23):d.line((x,y+1,x,min(43,y+8)),fill=STONE[1])
    for x in (5,width-11):
        d.rectangle((x,0,x+5,43),fill=TIMBER[0]);d.line((x,1,x,41),fill=TIMBER[2])
    d.rectangle((6,0,width-7,4),fill=TIMBER[0]);d.line((7,1,width-8,1),fill=TIMBER[1])
    d.line((5,45,width-6,45),fill=STONE[1])
    if door:
        center=24 if width==48 else 44
        d.rectangle((center-13,11,center+13,44),fill=TIMBER[0])
        d.rectangle((center-10,14,center+10,43),fill=ROOF[0])
        d.line((center-12,12,center+12,12),fill=TIMBER[2])
        d.line((center-12,13,center-12,43),fill=TIMBER[1])
        d.rectangle((center-13,43,center+13,46),fill=STONE[2])
        d.line((center-13,43,center+13,43),fill=STONE[4])
    if width>64:
        for x in (94,124):
            d.rectangle((x,10,x+15,26),fill=TIMBER[0]);d.rectangle((x+2,12,x+13,24),fill=ROOF[0])
            d.line((x+7,12,x+7,24),fill=TIMBER[1]);d.line((x+2,17,x+13,17),fill=TIMBER[1])
    return im


def farm_building(kind):
    """Original farm silhouettes in the existing native architecture grammar.

    Preserve the established 8x6 cottage, 6x5 barn and 4x3 drying shed canvases.
    Their lower two/two/one rows remain the physical facade, respectively.
    Outline the whole object; quiet material planes carry only sparse wear.
    Ground shadows are intentionally left to the later scene-wide pass.
    """
    ink = zone.COLORS['ink']
    wood = zone.ramp('deepsoil', 'soil', 'soilmid', 'soillight')
    masonry = zone.ramp('stonejoint', 'stoneshade', 'stone', 'stonelight')
    width, height = {'Cottage': (128, 96), 'Barn': (96, 80),
                     'DryingShed': (64, 48)}[kind]
    im = blank(width, height)
    d = ImageDraw.Draw(im)

    def post(x, top, bottom, wide=3):
        d.rectangle((x, top, x+wide-1, bottom), fill=wood[0])
        d.line((x, top+1, x, bottom-1), fill=wood[2])

    def footing(left, top, right, bottom):
        d.rectangle((left, top, right, bottom), fill=ink)
        d.rectangle((left+1, top, right-1, bottom-1), fill=masonry[1])
        d.line((left+1, top, right-1, top), fill=masonry[2])
        # Staggered broad blocks; no moss/noise scattered over the courses.
        for x in range(left+10, right-3, 17):
            d.line((x, top+1, x, bottom-1), fill=masonry[0])
        d.line((left+2, bottom-1, right-1, bottom-1), fill=masonry[0])

    def closed_door(left, top, right, bottom, double=False):
        d.rectangle((left-2, top-2, right+2, bottom), fill=wood[0])
        d.rectangle((left, top, right, bottom), fill=ROOF[0])
        # Two-pixel recess shows the door set behind the front wall.
        d.rectangle((left+2, top+2, right-2, bottom-1), fill=wood[1])
        d.line((left+2, top+2, left+2, bottom-1), fill=wood[2])
        for x in range(left+9, right-3, 8):
            d.line((x, top+3, x, bottom-2), fill=wood[0])
        if double:
            mid=(left+right)//2
            d.line((mid, top+2, mid, bottom), fill=ROOF[0])
            for y in (top+8, bottom-7):
                d.line((left+3, y, mid-2, y), fill=wood[2])
                d.line((mid+2, y, right-3, y), fill=wood[0])
        else:
            d.rectangle((right-6, top+13, right-4, top+14), fill=ROOF[0])
        d.line((left-2, bottom, right+2, bottom), fill=masonry[2])

    if kind == 'Cottage':
        # Broad front, shuttered domestic window and the retained right porch.
        d.rectangle((7, 55, 120, 94), fill=ink)
        d.rectangle((9, 58, 118, 88), fill=masonry[2])
        d.rectangle((9, 59, 118, 64), fill=masonry[1])
        footing(7, 89, 120, 95)
        for x in (9, 35, 76, 116):
            post(x, 59, 88)
        d.line((12, 78, 32, 78), fill=wood[0])
        # A single paired shutter leaves a clear, quiet wall panel around it.
        d.rectangle((45, 68, 65, 83), fill=wood[0])
        d.rectangle((47, 70, 63, 81), fill=wood[2])
        d.line((55, 70, 55, 82), fill=wood[0])
        for x in (50, 60):
            d.line((x, 71, x, 80), fill=wood[1])
        d.line((47, 73, 53, 73), fill=wood[0])
        d.line((57, 79, 63, 79), fill=wood[0])
        d.rectangle((80, 63, 115, 88), fill=ROOF[0])
        closed_door(91, 66, 110, 89)
        d.polygon([(80, 63), (89, 63), (80, 73)], fill=wood[0])
        d.line((81, 64, 81, 70), fill=wood[2])
        # Short capped chimney interrupts the roof without decorative clutter.
        d.rectangle((85, 1, 95, 20), fill=ink)
        d.rectangle((86, 3, 94, 18), fill=masonry[1])
        d.rectangle((84, 0, 96, 4), fill=ink)
        d.line((85, 1, 95, 1), fill=masonry[2])
        d.line((87, 8, 94, 8), fill=masonry[0])
        d.line((90, 9, 90, 13), fill=masonry[0])
        # Hipped roof: long ridge, two short end hips, large lower slate plane.
        d.polygon([(38, 13), (91, 13), (127, 57), (127, 62),
                   (0, 62), (0, 57)], fill=ink)
        d.polygon([(39, 15), (90, 15), (122, 55), (5, 55)], fill=ROOF[1])
        d.polygon([(40, 15), (89, 15), (107, 37), (22, 37)], fill=ROOF[2])
        d.polygon([(39, 16), (22, 37), (6, 55), (27, 55), (48, 16)], fill=ROOF[1])
        d.polygon([(89, 16), (108, 37), (122, 55), (102, 55), (82, 16)], fill=ROOF[0])
        d.line((47, 16, 27, 55), fill=ROOF[0])
        d.line((81, 16, 102, 55), fill=ROOF[1])
        d.line((40, 15, 88, 15), fill=ROOF[2])
        for x,y,n in ((49, 27, 14), (68, 34, 10), (37, 45, 16), (76, 50, 13)):
            d.line((x, y, x+n, y), fill=ROOF[1] if y<38 else ROOF[2])
            d.point((x+n, y-1), fill=ROOF[1] if y<38 else ROOF[2])
        d.rectangle((2, 57, 125, 60), fill=wood[0])
        d.line((3, 57, 124, 57), fill=wood[2])
        d.line((6, 61, 121, 61), fill=wood[1])
    elif kind == 'Barn':
        # The threshing barn keeps its stepped gambrel and wide double doors.
        d.rectangle((5, 40, 90, 78), fill=ink)
        d.rectangle((7, 43, 88, 74), fill=wood[1])
        d.rectangle((7, 44, 88, 49), fill=wood[0])
        footing(5, 74, 90, 79)
        for x in (8, 27, 66, 85):
            post(x, 47, 74)
        for x in (17, 76):
            d.line((x, 51, x, 70), fill=wood[0])
        closed_door(33, 49, 62, 75, True)
        d.line((11, 53, 24, 65), fill=wood[2])
        d.line((71, 65, 83, 53), fill=wood[0])
        d.polygon([(36, 2), (59, 2), (85, 20), (95, 43),
                   (95, 48), (0, 48), (0, 43), (10, 20)], fill=ink)
        d.polygon([(36, 4), (59, 4), (83, 21), (12, 21)], fill=ROOF[2])
        d.polygon([(12, 23), (83, 23), (92, 43), (3, 43)], fill=ROOF[1])
        d.polygon([(59, 5), (83, 22), (92, 43), (82, 43), (73, 23), (53, 5)], fill=ROOF[0])
        d.line((13, 22, 82, 22), fill=ROOF[0])
        d.line((34, 6, 14, 20), fill=ROOF[1])
        for x,y,n in ((37, 14, 10), (24, 31, 12), (48, 37, 14)):
            d.line((x, y, x+n, y), fill=ROOF[1] if y<23 else ROOF[2])
        d.rectangle((2, 44, 93, 46), fill=wood[0])
        d.line((3, 44, 92, 44), fill=wood[2])
    else:
        # Low asymmetric mono-pitch roof distinguishes the open two-bay shed.
        d.rectangle((4, 23, 59, 46), fill=ink)
        d.rectangle((6, 26, 57, 44), fill=ROOF[0])
        d.rectangle((7, 31, 56, 42), fill=wood[0])
        # Large stored boards and bundled timber read at the normal camera.
        for x,y in ((9, 35), (14, 34), (19, 36)):
            d.rectangle((x, y, x+3, 43), fill=wood[1])
            d.line((x, y, x+2, y), fill=wood[2])
        for x,y in ((36, 40), (45, 40), (40, 36)):
            d.rectangle((x, y, x+7, y+3), fill=wood[1])
            d.line((x+1, y, x+6, y), fill=wood[2])
        for x in (5, 29, 56):
            post(x, 24, 45)
            d.rectangle((x-1, 44, x+3, 47), fill=ink)
            d.line((x, 45, x+2, 45), fill=masonry[1])
        d.polygon([(11, 3), (57, 3), (63, 24), (63, 28),
                   (0, 28), (0, 23)], fill=ink)
        d.polygon([(12, 5), (55, 5), (60, 23), (3, 23)], fill=ROOF[1])
        d.polygon([(12, 5), (55, 5), (57, 11), (9, 11)], fill=ROOF[2])
        d.line((13, 6, 4, 23), fill=ROOF[2])
        d.line((54, 6, 59, 23), fill=ROOF[0])
        d.line((17, 16, 29, 16), fill=ROOF[2])
        d.line((40, 20, 50, 20), fill=ROOF[2])
        d.rectangle((1, 25, 62, 26), fill=wood[0])
        d.line((2, 25, 61, 25), fill=wood[2])
    return im


def watermill():
    """Retain the regional mill's porch gable and eight-spoke right wheel.

    Native 9x6 canvas; two facade rows plus the existing 2x3 wheel collision
    stay owned by the scene. The abandoned wheel is static and its open spokes
    reveal the actual permanent river, never painted blue inside this asset.
    """
    im=blank(144,96); d=ImageDraw.Draw(im)
    ink=zone.COLORS['ink']
    wood=zone.ramp('deepsoil','soilmid','soillight')
    stone=zone.ramp('stonejoint','stoneshade','stone','stonelight')

    # Low plaster/timber volume; the right edge houses the wheel's bearing.
    d.rectangle((6,49,109,94),fill=ink)
    d.rectangle((8,52,107,88),fill=stone[2])
    d.rectangle((8,53,107,60),fill=stone[1])
    d.rectangle((99,55,108,89),fill=wood[0])
    d.line((99,57,99,87),fill=wood[1])
    for x in (9,28,65):
        d.rectangle((x,56,x+2,88),fill=wood[0])
        d.line((x,58,x,87),fill=wood[1])
    # One broad stone course, with a sill that does not pretend to be stairs.
    d.rectangle((6,88,109,95),fill=ink)
    d.rectangle((7,88,108,93),fill=stone[1])
    d.line((8,88,107,88),fill=stone[2])
    for x in (22,39,70,87,102):
        d.line((x,89,x,93),fill=stone[0])
    d.line((8,93,107,93),fill=stone[0])
    # Closed, slightly offset shutter; one worn slat, no all-over hatching.
    d.rectangle((77,68,92,81),fill=wood[0])
    d.rectangle((79,70,89,79),fill=wood[1])
    d.line((84,71,84,79),fill=wood[0])
    d.line((80,72,89,72),fill=wood[2])
    d.rectangle((78,81,93,82),fill=stone[1])
    # The door is visibly closed, inset behind stout porch jambs.
    d.rectangle((39,58,63,91),fill=wood[0])
    d.rectangle((42,61,60,90),fill=ROOF[0])
    d.rectangle((44,63,58,90),fill=wood[1])
    d.line((44,64,44,89),fill=wood[2])
    for x in (49,55):d.line((x,64,x,89),fill=wood[0])
    d.rectangle((45,79,47,80),fill=ROOF[0])
    d.rectangle((38,91,64,94),fill=stone[1])
    d.line((38,91,64,91),fill=stone[3])

    # Wide east/west roof with a short back slope and quiet broad front plane.
    d.polygon([(25,17),(88,17),(111,47),(111,54),(0,54),(0,48)],fill=ink)
    d.polygon([(26,19),(87,19),(107,46),(4,46)],fill=ROOF[1])
    d.polygon([(26,19),(87,19),(93,26),(20,26)],fill=ROOF[2])
    d.line((20,27,94,27),fill=ROOF[0])
    d.line((26,20,5,46),fill=ROOF[2])
    d.polygon([(87,20),(107,46),(102,46),(83,20)],fill=ROOF[0])
    for x,y,n in ((25,35,13),(74,35,12),(12,43,13),(82,44,12)):
        d.line((x,y,x+n,y),fill=ROOF[2])
    d.rectangle((2,48,109,52),fill=wood[0])
    d.line((3,48,108,48),fill=wood[1])
    d.line((4,53,107,53),fill=wood[1])

    # Familiar small south-facing porch gable, integrated with the main eave.
    d.polygon([(31,56),(31,52),(49,32),(53,32),(72,52),(72,58),
               (65,58),(51,42),(38,58)],fill=ink)
    d.polygon([(34,53),(51,35),(69,54),(65,54),(51,40),(38,54)],fill=ROOF[0])
    d.line((35,52,50,36),fill=ROOF[2])
    d.line((52,36,68,53),fill=ROOF[1])
    d.polygon([(39,54),(51,41),(63,54)],fill=wood[0])
    d.polygon([(44,51),(51,44),(57,51)],fill=stone[2])
    d.rectangle((37,56,40,90),fill=wood[0])
    d.line((38,57,38,89),fill=wood[1])
    d.rectangle((62,56,65,90),fill=wood[0])
    d.line((62,57,62,89),fill=wood[1])

    # Bearing and short shaft tie the wheel to the wall without new machinery.
    d.rectangle((101,66,121,76),fill=ink)
    d.rectangle((102,67,120,74),fill=wood[0])
    d.line((103,68,119,68),fill=wood[1])
    d.rectangle((101,75,106,88),fill=wood[0])

    # Build the wheel on its own native plane so real water shows through holes.
    wheel=blank(144,96); w=ImageDraw.Draw(wheel)
    # Broad paddle ends distinguish the mill from a plain cart wheel. All stay
    # inside the original 144x96 canvas and its existing wheel footprint.
    paddles=[[(114,49),(126,49),(126,54),(114,54)],
             [(138,66),(143,66),(143,78),(138,78)],
             [(114,90),(126,90),(126,95),(114,95)],
             [(97,66),(102,66),(102,78),(97,78)],
             [(100,56),(105,51),(110,56),(105,61)],
             [(130,56),(135,51),(140,56),(135,61)],
             [(130,88),(135,83),(140,88),(135,93)],
             [(100,88),(105,83),(110,88),(105,93)]]
    for shape in paddles:w.polygon(shape,fill=ink)
    w.ellipse((97,49,143,95),fill=ink)
    w.ellipse((98,50,142,94),fill=wood[1])
    w.ellipse((101,53,139,91),fill=wood[0])
    w.ellipse((103,55,137,89),fill=(0,0,0,0))
    # Eight broad spokes join the rim; upper edges receive one quiet highlight.
    for dx,dy in ((0,-18),(13,-13),(18,0),(13,13),(0,18),(-13,13),(-18,0),(-13,-13)):
        w.line((120,72,120+dx,72+dy),fill=wood[0],width=4)
        w.line((120,71,120+dx,71+dy),fill=wood[1],width=2)
    # A handful of broad rim highlights and paddle divisions, not tiny teeth.
    w.line((112,51,125,51),fill=wood[2],width=2)
    w.line((101,59,99,66),fill=wood[2],width=2)
    w.line((98,72,98,78),fill=wood[2])
    for box in ((115,50,125,52),(140,67,142,77),
                (115,92,125,94),(98,67,100,77)):
        w.rectangle(box,fill=wood[1])
    for xy in ((102,56,105,53),(135,53,138,56),
               (138,88,135,91),(105,91,102,88)):
        w.line(xy,fill=wood[1],width=2)
    for xy in ((119,50,119,54),(135,56,132,59),(140,71,137,71),
               (135,87,132,84),(120,94,120,90),(104,88,107,85),
               (98,73,102,73),(104,56,107,59)):
        w.line(xy,fill=wood[0])
    w.rectangle((115,67,125,77),fill=ink)
    w.rectangle((116,68,124,76),fill=wood[0])
    w.line((117,68,123,68),fill=wood[2])
    w.rectangle((119,71,121,73),fill=wood[1])
    im.alpha_composite(wheel)
    # A small attached ivy cluster keeps the lightly reclaimed farm character.
    d=ImageDraw.Draw(im); leaf=zone.COLORS['leafshade']
    d.line((8,93,8,84),fill=leaf)
    for x,y in ((5,87),(9,83),(6,91)):
        d.rectangle((x,y,x+2,y+1),fill=leaf)
    return im


def courtyard_prop(kind):
    """Simple farm joinery on the existing prop canvases and row splits.

    The scene still owns collision/depth. Fence posts repeat at a forty-pixel
    stride, and the bridge's entire middle thirty-two pixels remain solid deck.
    All surface marks are attached material wear, never cast ground shadows.
    """
    sizes={'Well':(32,48),'Cart':(64,48),'Bridge':(96,64),
           'Fence':(48,32),'StonePost':(32,32)}
    im=blank(*sizes[kind]); d=ImageDraw.Draw(im)
    ink=zone.COLORS['ink']
    wood=zone.ramp('deepsoil','soil','soilmid','soillight')
    stone=zone.ramp('stonejoint','stoneshade','stone','stonelight')

    if kind=='Well':
        # Bare windlass over a low octagonal basin; no roof or loose decoration.
        for x,y in ((3,2),(25,4)):
            d.rectangle((x,y,x+3,35),fill=ink)
            d.rectangle((x+1,y+1,x+2,33),fill=wood[0])
            d.line((x+1,y+1,x+1,32),fill=wood[2])
        d.rectangle((5,10,27,14),fill=ink)
        d.rectangle((6,11,26,13),fill=wood[0])
        d.line((7,11,24,11),fill=wood[3])
        d.rectangle((26,12,29,19),fill=wood[0])
        d.rectangle((28,18,31,20),fill=ink)
        d.line((28,18,30,18),fill=wood[2])
        # A few broad rope turns and a short snapped end above the dark opening.
        for x in (13,15,17):d.line((x,10,x,14),fill=wood[2])
        d.line((16,15,16,27),fill=wood[3])
        d.line((16,27,18,29),fill=wood[2])
        d.polygon([(7,25),(24,25),(31,31),(31,40),(25,47),
                   (6,47),(0,40),(0,31)],fill=ink)
        d.polygon([(7,27),(24,27),(29,32),(29,39),(24,45),
                   (7,45),(2,39),(2,32)],fill=stone[1])
        d.polygon([(2,34),(8,38),(24,38),(29,34),(29,39),
                   (24,45),(7,45),(2,39)],fill=stone[0])
        d.rectangle((8,39,23,44),fill=stone[1])
        d.polygon([(7,28),(24,28),(28,32),(24,37),(7,37),
                   (3,32)],fill=stone[2])
        d.polygon([(9,30),(22,30),(25,32),(22,35),(9,35),
                   (6,32)],fill=ink)
        d.line((8,28,23,28),fill=stone[3])
        d.line((7,37,24,37),fill=stone[3])
        d.line((8,39,8,44),fill=stone[0])
        d.line((18,39,18,44),fill=stone[0])
        d.line((24,39,27,37),fill=stone[1])
        d.line((16,24,16,31),fill=wood[3])
    elif kind=='Cart':
        # Open empty bed, two long shafts and one unobstructed near wheel.
        d.ellipse((11,1,25,15),fill=ink)
        d.ellipse((12,2,24,14),fill=wood[2])
        d.ellipse((14,4,22,12),fill=wood[0])
        d.line((18,4,18,8),fill=wood[2])
        for y in (20,31):
            d.rectangle((36,y-1,63,y+3),fill=ink)
            d.rectangle((38,y,61,y+2),fill=wood[1])
            d.line((39,y,61,y),fill=wood[3])
        d.rectangle((3,8,39,36),fill=ink)
        d.rectangle((5,11,37,33),fill=wood[0])
        d.rectangle((7,14,35,27),fill=wood[2])
        for y in (19,25):d.line((7,y,35,y),fill=wood[1])
        d.line((10,15,10,18),fill=wood[3])
        d.rectangle((4,8,38,12),fill=wood[0])
        d.line((5,9,37,9),fill=wood[3])
        d.rectangle((5,28,37,35),fill=wood[1])
        d.line((6,28,36,28),fill=wood[3])
        d.line((6,33,36,33),fill=wood[0])
        for x in (3,36):
            d.rectangle((x,6,x+3,36),fill=ink)
            d.rectangle((x+1,8,x+2,34),fill=wood[0])
            d.line((x+1,8,x+1,32),fill=wood[2])
        d.ellipse((9,30,25,46),fill=ink)
        d.ellipse((10,31,24,45),fill=wood[2])
        d.ellipse((12,33,22,43),fill=wood[0])
        d.line((17,33,17,43),fill=wood[2],width=2)
        d.line((12,38,22,38),fill=wood[2],width=2)
        d.rectangle((15,36,19,40),fill=ink)
        d.rectangle((16,37,18,39),fill=wood[1])
    elif kind=='Bridge':
        # No decorative holes in the two clear walking rows. Eight quiet boards
        # span both banks; sparse seams describe construction without striping.
        d.rectangle((0,12,95,51),fill=wood[0])
        for col in range(8):
            x=12*col
            d.rectangle((x,14,x+10,49),fill=wood[2] if col in (1,5) else wood[1])
            d.line((x,15,x,48),fill=wood[2])
        for x,y in ((17,23),(65,39),(86,29)):
            d.line((x,y,x+3,y),fill=wood[3])
        # Upper rail is below the actor; near rail is the separate overhead row.
        for y in (7,54):
            d.rectangle((1,y-2,94,y+4),fill=ink)
            d.rectangle((2,y-1,93,y+2),fill=wood[0])
            d.line((3,y-1,92,y-1),fill=wood[3])
            d.line((3,y,92,y),fill=wood[2])
        for x in (30,62):
            d.rectangle((x,11,x+2,15),fill=wood[0])
            d.rectangle((x,49,x+2,53),fill=wood[0])
        for x in (0,88):
            for y in (0,48):
                d.polygon([(x+2,y),(x+5,y),(x+7,y+3),(x+7,y+15),
                           (x,y+15),(x,y+3)],fill=ink)
                d.rectangle((x+1,y+4,x+5,y+13),fill=wood[1])
                d.line((x+1,y+4,x+1,y+12),fill=wood[3])
                d.line((x+2,y+2,x+4,y+2),fill=wood[2])
        assert all(im.getpixel((x,y))[3]==255 for x in range(96) for y in range(16,48))
    elif kind=='Fence':
        for y in (12,23):
            d.rectangle((4,y-1,43,y+4),fill=ink)
            d.rectangle((5,y,42,y+2),fill=wood[1])
            d.line((6,y,41,y),fill=wood[3])
        d.line((22,12,25,12),fill=wood[0])
        # Equal eight-pixel end posts make two existing 2.5-unit-spaced
        # instances meet exactly, including their shallow cap and bottom.
        for x in (0,40):
            d.polygon([(x+2,3),(x+5,3),(x+7,5),(x+7,31),
                       (x,31),(x,5)],fill=ink)
            d.rectangle((x+1,7,x+5,29),fill=wood[1])
            d.rectangle((x+1,7,x+2,28),fill=wood[2])
            d.line((x+2,5,x+5,5),fill=wood[3])
            d.line((x+6,7,x+6,29),fill=wood[0])
            d.line((x+1,30,x+5,30),fill=wood[0])
        assert im.crop((0,0,8,32)).tobytes()==im.crop((40,0,48,32)).tobytes()
    else:
        # Low ancient remnant with modest rail stubs. Only the existing left
        # cell is solid; the stubs keep their original render-only ownership.
        for y in (12,23):
            d.rectangle((10,y-1,31,y+4),fill=ink)
            d.rectangle((12,y,31,y+2),fill=wood[0])
            d.line((12,y,31,y),fill=wood[2])
        d.polygon([(5,0),(10,0),(14,4),(14,29),(11,31),
                   (4,31),(1,28),(1,5)],fill=ink)
        d.polygon([(5,2),(9,2),(12,5),(12,28),(10,29),
                   (4,29),(3,27),(3,6)],fill=stone[1])
        d.line((4,6,4,26),fill=stone[2])
        d.line((6,3,9,3),fill=stone[2])
        d.line((11,8,11,26),fill=stone[0])
        d.line((6,12,9,12),fill=stone[0])
        d.line((8,13,8,17),fill=stone[0])
    return im


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    atlas = blank(512, 512)
    entries = []
    sources = {}

    def append(name, im, x, top, pivot=(.5, .5)):
        atlas.alpha_composite(im, (x, top))
        colors = sorted({p[:3] for p in pixels(im) if p[3]})
        assert set(pixels(im.getchannel('A'))) <= {0, 255}
        entry = dict(name=name, x=x, y=512-top-im.height, width=im.width,
                     height=im.height, pivotX=pivot[0], pivotY=pivot[1],
                     pixelsPerUnit=PPU, colors=len(colors))
        entries.append(entry); sources[name] = im

    append('Roof_Main', roof(192), 0, 0)
    append('Roof_Wing', roof(144), 160, 0)
    append('Sluice_Gate', gate(), 320, 0)
    append('Sluice_Hoist', hoist(), 384, 0)
    append('Sunwheel_Crest', crest(), 448, 0)
    append('Roof_Vent', vent(), 480, 0)
    for frame in range(4):
        append('Sluice_Flow_'+str(frame), flow(frame), 320+frame*48, 64)
    small = [(f'Wall_Face_{v}', wall(v)) for v in range(3)]
    small += [(f'Floor_{v}', floor(v)) for v in range(4)]
    small += [('Timber_'+k, timber(k)) for k in ('Post', 'Beam', 'Cross')]
    small += [('Foundation_'+k, foundation(k)) for k in
              ('Cap', 'Foot', 'West', 'East', 'WestCap', 'EastCap', 'WestFoot', 'EastFoot')]
    small += [('Throat_'+k, throat(k)) for k in ('West', 'East', 'Lintel', 'Apron')]
    for i, (name, im) in enumerate(small):
        append(name, im, (i % 16)*16, 208+(i//16)*16)
    # Tile subdivisions let editor painting reuse exactly the same roof pixels.
    # Whole roof sprites remain available for efficient one-renderer cutaways.
    for name in ('Roof_Main', 'Roof_Wing'):
        entry = next(e for e in entries if e['name'] == name)
        for y in range(entry['height']//16):
            for x in range(entry['width']//16):
                entries.append(dict(name=f'{name}_{x}_{y}', x=entry['x']+16*x,
                                    y=entry['y']+16*y, width=16, height=16,
                                    pivotX=.5, pivotY=.5, pixelsPerUnit=PPU))
    # Append, never repack: established sprites/roof slices retain their exact
    # names, order, rectangles and pixels when the service-floor kit grows.
    service = [(f'Floor_Timber_{v}', service_floor(v)) for v in range(3)]
    service += [('Drive_Cover_H',drive_cover()), ('Drive_Cover_V',drive_cover(True)),
                ('Sluice_Intake_Grille',intake_grille())]
    for i,(name,im) in enumerate(service):
        append(name,im,256+i*16,224)
    # Revision two is append-only. Old footprints and tile rects remain valid
    # while scene authors opt into the south-facing architecture explicitly.
    append('Roof_Main_South',south_roof(192),0,256)
    append('Roof_Wing_South',south_roof(144,True),160,256)
    append('Facade_Main_South',south_facade(),320,288)
    append('Facade_Door_South',south_facade(48),320,352)
    append('Facade_Wall_South',south_facade(48,False),368,352)
    machine_x=320
    for kind in ('Fork','Crank','Brake','Catch','Counterweight','Axle'):
        im=machine(kind)
        append('Machine_'+kind,im,machine_x,256)
        machine_x+=im.width
    work=[(f'Floor_Work_{v}',quiet_floor(v)) for v in range(4)]
    work += [(f'Floor_Service_{v}',quiet_floor(v,True)) for v in range(4)]
    work += [('Track_'+k,track(k)) for k in ('H','V','Corner','Stop')]
    work += [('Wall_Low_'+k,low_wall(k)) for k in ('Face','Pier','Cap')]
    for i,(name,im) in enumerate(work):
        append(name,im,i*16,240)
    # Append into unused atlas space, without repacking any sluice sprites.
    for name, x, top in (('Cottage', 160, 416), ('Barn', 320, 416),
                         ('DryingShed', 432, 416)):
        sprite_name='Farm_'+name
        append(sprite_name, farm_building(name), x, top)
        entry=entries[-1]
        for y in range(entry['height']//16):
            for col in range(entry['width']//16):
                entries.append(dict(name=f'{sprite_name}_{col}_{y}',
                                    x=x+16*col, y=entry['y']+16*y,
                                    width=16, height=16, pivotX=.5, pivotY=.5,
                                    pixelsPerUnit=PPU))
    # Last unused 144x96 rectangle above the masonry strip; prior entries stay fixed.
    append('Farm_Watermill',watermill(),320,112)
    mill=entries[-1]
    for y in range(6):
        for x in range(9):
            entries.append(dict(name=f'Farm_Watermill_{x}_{y}',
                                x=mill['x']+16*x, y=mill['y']+16*y,
                                width=16,height=16,pivotX=.5,pivotY=.5,pixelsPerUnit=PPU))
    # Original courtyard props use remaining atlas gaps; no existing repacking.
    for kind,x,top in (('Well',224,144),('Cart',160,144),('Bridge',0,448),
                       ('Fence',96,448),('StonePost',96,480)):
        name='Farm_'+kind
        append(name,courtyard_prop(kind),x,top)
        entry=entries[-1]
        for y in range(entry['height']//16):
            for col in range(entry['width']//16):
                entries.append(dict(name=f'{name}_{col}_{y}',
                                    x=x+16*col,y=entry['y']+16*y,
                                    width=16,height=16,pivotX=.5,pivotY=.5,pixelsPerUnit=PPU))
    zone.validate(atlas)
    atlas.save(OUT/'LowlandsArchitecture16.png')
    manifest = dict(pixelsPerUnit=PPU, filterMode='Point', alpha='binary',
                    atlas='LowlandsArchitecture16.png', coordinateOrigin='bottom-left',
                    roofFootprints={'Roof_Main':[10,9], 'Roof_Wing':[10,7],
                                    'Roof_Main_South':[10,10], 'Roof_Wing_South':[10,7]},
                    farmCanvases={'Farm_Cottage':[128,96], 'Farm_Barn':[96,80],
                                  'Farm_DryingShed':[64,48], 'Farm_Watermill':[144,96]},
                    farmPlacementNotes='Whole original farm sprites and exact 16x16 slices share the existing canvases. Keep the lower 2/2/1/2 rows on Environment for Cottage/Barn/DryingShed/Watermill and upper rows on Above Player. Collision remains separately authored in the original prefab instance; these tiles never add colliders. The mill keeps its original 7x2 house plus 2x3 wheel footprint, static wheel and permanent river placement. Ground shadows are deferred to the coordinated scene pass.',
                    courtyardCanvases={'Farm_Well':[32,48],'Farm_Cart':[64,48],
                                       'Farm_Bridge':[96,64],'Farm_Fence':[48,32],
                                       'Farm_StonePost':[32,32]},
                    courtyardPlacementNotes='Preserve the existing placed prefab visual maps and collision. Well/Cart/Fence/StonePost use their lower row on Environment and upper rows on Above Player; Cart collision remains two rows. Bridge rows 1..3 stay Environment and its near-rail row 0 stays Above Player; only rows 1..2 are walkable. Fence end posts match at the existing 40-pixel (2.5-unit) stride. StonePost remains solid only in the lower-left cell. No ground shadow is added.',
                    southGableNotes='North/south ridge; bottom edge rests above a separate south-facing facade. Main facade opening is 44 pixels from its left edge; modular door opening is centered. Machine sprites have center pivots and require unit-scale visual children on gameplay receiver transforms.',
                    palette={key:['#'+''.join(f'{v:02x}' for v in c[:3]) for c in value]
                             for key,value in [('stone',STONE),('timber',TIMBER),
                                               ('roof',ROOF),('bronze',BRONZE),('water',WATER),
                                               ('serviceTimber',SERVICE_TIMBER),
                                               ('workFloor',WORK_FLOOR),('walkFloor',WALK_FLOOR),
                                               ('iron',IRON)]},
                    entries=entries)
    (OUT/'ArchitectureManifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    # A native assembly proof; text is outside the art and never enters the atlas.
    preview = Image.new('RGBA', (768, 592), GRASS)
    d = ImageDraw.Draw(preview)
    preview.alpha_composite(sources['Facade_Main_South'], (18, 205))
    preview.alpha_composite(sources['Roof_Main_South'], (18, 16))
    preview.alpha_composite(sources['Facade_Main_South'], (200, 157))
    preview.alpha_composite(sources['Roof_Wing_South'], (200, 16))
    # A tiny room assembly proves the hierarchy: neutral floor, dark linkage,
    # then isolated warm controls. It is not a layout or a puzzle solution.
    for x in range(10):
        for y in range(12):
            family='Floor_Service_' if x<4 else 'Floor_Work_'
            preview.alpha_composite(sources[family+str((x*3+y)%4)],(390+x*16,24+y*16))
    for x in range(10):preview.alpha_composite(sources['Wall_Low_Face'],(390+x*16,8))
    for y in range(12):
        preview.alpha_composite(sources['Wall_Low_Pier'],(384,24+y*16))
        preview.alpha_composite(sources['Wall_Low_Pier'],(544,24+y*16))
    for y in range(7):preview.alpha_composite(sources['Track_V'],(516,52+y*16))
    for x in range(5):preview.alpha_composite(sources['Track_H'],(436+x*16,164))
    preview.alpha_composite(sources['Machine_Fork'],(516,96))
    preview.alpha_composite(sources['Machine_Crank'],(420,158))
    preview.alpha_composite(sources['Machine_Brake'],(480,164))
    preview.alpha_composite(sources['Machine_Catch'],(452,162))
    preview.alpha_composite(sources['Machine_Counterweight'],(516,32))
    for x in range(4):preview.alpha_composite(sources['Throat_Lintel'],(608+x*16,48))
    for y in range(5):
        preview.alpha_composite(sources['Throat_West'],(598,48+y*16))
        preview.alpha_composite(sources['Throat_East'],(674,48+y*16))
    preview.alpha_composite(sources['Sluice_Hoist'],(608,59))
    preview.alpha_composite(sources['Sluice_Gate'],(608,87))
    preview.alpha_composite(sources['Sluice_Flow_0'],(616,134))
    for i, (name, im) in enumerate(work):
        x,y=16+i*32,284
        d.rectangle((x-1,y-1,x+16,y+16),fill=STONE[1]);preview.alpha_composite(im,(x,y))
    for i,kind in enumerate(('Fork','Crank','Brake','Catch','Counterweight','Axle')):
        preview.alpha_composite(sources['Machine_'+kind],(516+i*36,280))
    for name, x in (('Cottage', 24), ('Barn', 200), ('DryingShed', 344)):
        im=sources['Farm_'+name]
        preview.alpha_composite(im,(x,496-im.height))
    preview.alpha_composite(sources['Farm_Watermill'],(464,400))
    for name,x,y in (('Well',24,532),('Cart',96,532),('Bridge',192,516),
                      ('StonePost',332,548),('Fence',356,548),('Fence',396,548)):
        preview.alpha_composite(sources['Farm_'+name],(x,y))
    preview.resize((1536,1184),Image.Resampling.NEAREST).save(OUT/'ArchitecturePreview.png')
    # This is source-data verification, not Unity import or in-game visual approval.
    assert len(entries) == len({e['name'] for e in entries})
    assert len({p[:3] for key in ('Roof_Main','Wall_Face_1','Timber_Post')
                for p in pixels(sources[key]) if p[3]}) <= 12
    assert all(len({p[:3] for p in pixels(im) if p[3]})<=12 for im in sources.values())
    print(json.dumps({'atlas':[512,512], 'entries':len(entries),
                      'standaloneSprites':len(sources),
                      'roofColors':len({p[:3] for p in pixels(sources['Roof_Main']) if p[3]}),
                      'gateColors':len({p[:3] for p in pixels(sources['Sluice_Gate']) if p[3]}),
                      'output':str(OUT)}, indent=2))


if __name__ == '__main__':
    main()
