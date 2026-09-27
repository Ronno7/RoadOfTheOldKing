"""Keep local, same-cel arm drawings over the registered carrying body.

The registered source is rebuilt by the sprint exporter first. This recipe never
uses a previous output as its input, moves a limb, or copies reference anatomy.
"""
import json
from pathlib import Path
from PIL import Image, ImageChops
from build_player_throw_layers import polygon_mask, isolate

ROOT = Path(__file__).resolve().parents[3]


def build_unarmed_view(name, body, recipe_path, config, output):
    recipe = json.loads((ROOT / recipe_path).read_text())
    size = config['canvas']
    x0, y0, x1, y1 = recipe['workingCrop']
    ratio = recipe['workingScale']
    if ratio != int(ratio) or len(recipe['frames']) != config['celCount']:
        raise ValueError('Use an integer editing scale and one redraw per cel')
    result = Image.new('RGBA', body.size)
    for index, frame in enumerate(recipe['frames']):
        x, y = index % 4 * size, index // 4 * size
        base = body.crop((x, y, x + size, y + size))
        working = base.crop((x0, y0, x1, y1)).resize(
            ((x1-x0)*ratio, (y1-y0)*ratio), Image.Resampling.NEAREST)
        drawing = Image.open(ROOT / frame['source']).convert('RGBA')
        if drawing.width != drawing.height:
            raise ValueError('Arm edits must retain the square source layout')
        drawing = drawing.resize(working.size, Image.Resampling.NEAREST)
        drawing = isolate(drawing, drawing.getchannel('A').point(lambda a: 255 if a >= 128 else 0))
        mask = polygon_mask(working.size, frame['regions'])
        # Preserve the low hem/leg silhouette even where an edit window passes
        # nearby. Forward hands are above this region, so no hand is clipped.
        mask = ImageChops.subtract(mask, polygon_mask(working.size, frame['protectedRegions']))
        revised = Image.composite(drawing, working, mask)
        cel = base.copy()
        cel.paste(revised.resize((x1-x0, y1-y0), Image.Resampling.NEAREST), (x0, y0))
        allowed = Image.new('L', base.size)
        allowed.paste(mask.resize((x1-x0, y1-y0), Image.Resampling.NEAREST), (x0, y0))
        difference = ImageChops.difference(cel, base)
        assert not isolate(difference, ImageChops.invert(allowed)).getbbox(alpha_only=False), index
        for box in recipe['protectedBoxes']:
            assert not ImageChops.difference(cel.crop(box), base.crop(box)).getbbox(alpha_only=False), (index, box)
        box = cel.getbbox()
        if not box or min(box[:2]) == 0 or max(box[2:]) == size:
            raise ValueError('Empty or clipped unarmed sprint cel')
        result.paste(cel, (x, y))
    result.save(output / f'Sprint-{name}-Unarmed.png')
    print(f'{name}: eight local empty-arm drawings; protected head/legs exact; no limb transforms.')
