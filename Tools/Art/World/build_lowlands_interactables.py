"""Regional interaction art with a deliberate near-black silhouette.

Native drawing only: no source images are sampled or modified. Shared Tutorial
reward art, scene references, gameplay IDs and prefabs remain untouched. Existing
canvases and centered pivots make the new regional sprites direct visual swaps.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw
import build_lowlands_architecture as architecture

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'RoadOfTheOldKing/Assets/Art/Sprites/World/LowlandsInteractables'


def rgb(value):
    return tuple(bytes.fromhex(value)) + (255,)


import lowlands_palette as zone

INK = zone.COLORS['ink']
WOOD = zone.ramp('deepsoil', 'soil', 'soilmid', 'soillight')
BRONZE = zone.ramp('soil', 'soilmid', 'soillight', 'soilshine')
STONE = zone.ramp('slate', 'slatelight', 'stoneshade', 'stone')
CLAY = zone.ramp('soil', 'soillight', 'soilshine')
RED = [zone.ACCENTS[n] for n in ('heartShadow', 'heartMid', 'heart', 'heartLight')]


def blank(w, h):
    return Image.new('RGBA', (w, h))


def silhouette(im):
    """One native dark contour within the authored shape; never grow its bounds."""
    result = im.copy()
    width, height = im.size
    for y in range(height):
        for x in range(width):
            if not im.getpixel((x, y))[3]:
                continue
            if any(not (0 <= xx < width and 0 <= yy < height)
                   or not im.getpixel((xx, yy))[3]
                   for xx, yy in ((x-1, y), (x+1, y), (x, y-1), (x, y+1))):
                result.putpixel((x, y), INK)
    return result


def chest(pose):
    im = blank(24, 24); d = ImageDraw.Draw(im)
    # The body at y=16..21 is byte-identical in every pose. The slightly raised
    # lid exposes a dark interior without changing the feet or depth anchor.
    d.rectangle((2, 14, 21, 21), fill=INK)
    d.rectangle((3, 15, 20, 19), fill=WOOD[2])
    d.line((3, 19, 20, 19), fill=WOOD[1])
    d.line((4, 20, 19, 20), fill=WOOD[0])
    for x in (5, 17):
        d.rectangle((x, 15, x+1, 20), fill=BRONZE[1])
        d.point((x, 17), fill=BRONZE[3])
    if pose == 0:
        d.polygon([(2, 10), (4, 7), (19, 7), (21, 10), (21, 15), (2, 15)], fill=INK)
        d.rectangle((3, 10, 20, 14), fill=WOOD[2])
        d.rectangle((5, 8, 18, 9), fill=WOOD[3])
        d.line((3, 13, 20, 13), fill=WOOD[1])
        for x in (5, 17):
            d.rectangle((x, 8, x+1, 14), fill=BRONZE[1])
            d.point((x, 8), fill=BRONZE[3])
        d.rectangle((10, 13, 13, 15), fill=INK)
        d.rectangle((11, 13, 12, 14), fill=BRONZE[2])
    elif pose == 1:
        d.rectangle((3, 10, 20, 15), fill=INK)
        d.line((4, 14, 19, 14), fill=WOOD[0])
        d.polygon([(1, 8), (4, 5), (19, 5), (22, 8), (21, 11), (2, 11)], fill=INK)
        d.rectangle((3, 8, 20, 9), fill=WOOD[2])
        d.rectangle((5, 6, 18, 7), fill=WOOD[3])
        for x in (5, 17):
            d.rectangle((x, 6, x+1, 9), fill=BRONZE[1])
            d.point((x, 6), fill=BRONZE[3])
        d.rectangle((10, 9, 13, 11), fill=INK)
        d.line((11, 10, 12, 10), fill=BRONZE[2])
    else:
        d.rectangle((2, 3, 21, 13), fill=INK)
        d.rectangle((4, 1, 19, 13), fill=INK)
        d.rectangle((4, 3, 19, 10), fill=WOOD[1])
        d.rectangle((5, 2, 18, 3), fill=WOOD[3])
        d.line((3, 4, 3, 10), fill=WOOD[2])
        d.line((4, 11, 19, 11), fill=WOOD[0])
        for x in (5, 17):
            d.rectangle((x, 3, x+1, 10), fill=BRONZE[1])
        d.rectangle((3, 13, 20, 15), fill=INK)
        d.line((4, 14, 19, 14), fill=WOOD[0])
    return silhouette(im)


def pot():
    im = blank(16, 16); d = ImageDraw.Draw(im)
    d.polygon([(4, 1), (11, 1), (11, 4), (13, 6), (13, 11),
               (11, 14), (4, 14), (2, 11), (2, 6), (4, 4)], fill=CLAY[1])
    d.rectangle((4, 2, 11, 4), fill=CLAY[2])
    d.line((5, 2, 10, 2), fill=INK)
    d.line((5, 3, 10, 3), fill=CLAY[0])
    d.rectangle((3, 7, 4, 10), fill=CLAY[2])
    d.rectangle((10, 7, 12, 11), fill=CLAY[0])
    d.line((5, 12, 10, 12), fill=CLAY[0])
    return silhouette(im)


def machine(kind):
    # Re-render our existing native hardware shapes with the new interaction
    # contour. No historical atlas pixels are inputs, and architecture exports
    # stay untouched. The open fork notch and each part's canvas remain fixed.
    old_iron = architecture.IRON
    try:
        architecture.IRON = [INK] + old_iron[1:]
        return silhouette(architecture.machine(kind))
    finally:
        architecture.IRON = old_iron


def anchor():
    im = blank(16, 24); d = ImageDraw.Draw(im)
    d.polygon([(3, 8), (5, 6), (11, 6), (13, 9), (14, 20),
               (12, 21), (2, 21), (1, 19)], fill=STONE[1])
    d.polygon([(4, 8), (10, 7), (11, 10), (4, 10)], fill=STONE[3])
    d.rectangle((10, 11, 12, 19), fill=STONE[0])
    d.rectangle((4, 12, 11, 17), fill=INK)
    d.rectangle((5, 13, 10, 16), fill=BRONZE[1])
    d.rectangle((6, 14, 9, 15), fill=INK)
    d.line((5, 13, 10, 13), fill=BRONZE[3])
    d.line((3, 19, 5, 19), fill=STONE[0])
    return silhouette(im)


def carving():
    im = blank(24, 24); d = ImageDraw.Draw(im)
    d.polygon([(3, 3), (19, 3), (23, 7), (23, 17), (20, 20),
               (3, 20), (0, 17), (0, 7)], fill=STONE[1])
    d.rectangle((3, 5, 20, 16), fill=STONE[2])
    d.line((4, 4, 18, 4), fill=STONE[3])
    d.rectangle((3, 18, 20, 19), fill=STONE[0])
    # A single compact forge stamp, with no text, sequence or answer arrows.
    d.polygon([(6, 12), (16, 12), (19, 10), (19, 13), (15, 15),
               (14, 17), (9, 17), (9, 14), (6, 14)], fill=INK)
    d.line((7, 11, 15, 11), fill=STONE[3])
    d.rectangle((8, 7, 14, 9), fill=INK)
    d.rectangle((10, 9, 11, 11), fill=INK)
    d.line((9, 7, 13, 7), fill=STONE[0])
    return silhouette(im)


def pickup(kind):
    im = blank(16, 24 if kind == 'SunShard' else 16); d = ImageDraw.Draw(im)
    if kind == 'BronzeCoin':
        d.polygon([(5, 3), (9, 3), (12, 6), (12, 10), (9, 13),
                   (5, 13), (2, 10), (2, 6)], fill=BRONZE[1])
        d.line((5, 4, 9, 4), fill=BRONZE[3])
        d.line((3, 6, 3, 9), fill=BRONZE[3])
        d.rectangle((6, 6, 8, 10), fill=BRONZE[0])
        d.line((10, 7, 10, 10), fill=BRONZE[2])
    elif kind == 'HeartFragment':
        d.polygon([(1, 4), (3, 2), (5, 2), (7, 4), (10, 2), (12, 2),
                   (14, 4), (14, 7), (11, 10), (8, 13), (6, 13),
                   (3, 10), (1, 7)], fill=RED[2])
        d.polygon([(10, 4), (13, 4), (13, 7), (8, 12), (8, 8)], fill=RED[1])
        d.line((3, 4, 5, 4), fill=RED[3])
        d.line((2, 5, 2, 6), fill=RED[3])
        d.line((7, 4, 6, 6, 8, 7, 7, 9), fill=INK)
    else:
        d.polygon([(8, 3), (11, 7), (12, 13), (10, 17), (7, 21),
                   (4, 17), (3, 11), (5, 7)], fill=BRONZE[2])
        d.polygon([(8, 5), (8, 17), (7, 19), (5, 15), (5, 10)], fill=zone.ACCENTS['shardLight'])
        d.polygon([(9, 7), (11, 11), (10, 15), (9, 18)], fill=BRONZE[1])
    return silhouette(im)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sources = {'TimberChest_Closed': chest(0), 'TimberChest_HalfOpen': chest(1),
               'TimberChest_Open': chest(2), 'ClayPot': pot()}
    sources.update({'Machine_'+kind: machine(kind) for kind in
                    ('Fork', 'Crank', 'Brake', 'Catch', 'Counterweight', 'Axle')})
    sources.update({'RopeAnchor': anchor(), 'ForgeCarving': carving()})
    sources.update({kind: pickup(kind) for kind in ('SunShard', 'BronzeCoin', 'HeartFragment')})
    atlas = blank(256, 64); entries = []
    # Fixed explicit slots keep named rectangles stable as this catalog grows.
    slots = [(0, 0), (24, 0), (48, 0), (72, 0),
             (0, 24), (16, 24), (40, 24), (56, 24), (72, 24), (88, 24),
             (96, 0), (112, 0), (144, 0), (160, 0), (176, 0)]
    for (name, im), (x, y) in zip(sources.items(), slots):
        w, h = im.size
        atlas.alpha_composite(im, (x, atlas.height-y-h))
        entries.append(dict(name=name, material='Interactable', mask=0, variant=0,
                            index=len(entries), x=x, y=y, width=w, height=h))
        assert set(im.getchannel('A').get_flattened_data()) <= {0, 255}
        assert len({p for p in im.get_flattened_data() if p[3]}) <= 8
        assert silhouette(im).tobytes() == im.tobytes(), name
    lower = sources['TimberChest_Closed'].crop((0, 16, 24, 24)).tobytes()
    assert all(sources['TimberChest_'+pose].crop((0, 16, 24, 24)).tobytes() == lower
               for pose in ('HalfOpen', 'Open'))
    zone.validate(atlas, rewards=True)
    atlas.save(OUT/'LowlandsInteractables16.png')
    (OUT/'InteractableManifest.json').write_text(json.dumps(dict(
        entries=entries, outline='#171C1B', nativePPU=16,
        recipe='Original native silhouettes; centered pivots and prior canvases retained. '
               'Existing machine drawing functions reused; no image inputs or shared asset changes.'),
        indent=2)+'\n')
    print('Exported 15 regional interaction sprites; native canvases, 3-8 colors, '
          'binary alpha, complete near-black contours and stable chest base verified.')


if __name__ == '__main__':
    main()
