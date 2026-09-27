"""Bake local carrying-arm drawings with the walking prop at its fixed scale.

Same-cel masks retain approved character pixels. Every output is a complete
registered drawing; no runtime limb transforms or reference anatomy transplants.
"""
import json
import math
from pathlib import Path
from PIL import Image, ImageChops
from build_player_throw_layers import polygon_mask, isolate, body_support
from build_player_return_layers import register

ROOT = Path(__file__).resolve().parents[3]


def build_carry_view(name, view, config, output, review):
    recipe = json.loads((ROOT/view['carryRecipe']).read_text())
    unarmed = Image.open(ROOT/view['unarmedSource']).convert('RGBA')
    layout = view['unarmedLayout']
    width,height = layout['cellSize']
    working = recipe['workingCanvas']
    ox,oy = recipe['workingOrigin']
    size = config['canvas']
    ppu = config['pixelsPerUnit']
    scale = ppu/recipe['workingDensity']
    ratio = recipe['workingDensity']/layout['density']
    if ratio != int(ratio) or len(recipe['frames']) != 8:
        raise ValueError('Use whole-pixel working scale and eight matching cels')
    ratio = int(ratio)
    sheets = {k:Image.new('RGBA',(size*4,size*2)) for k in ('Body','Weapon','Reveal','Master','Unarmed')}
    axe = Image.open(ROOT/recipe['prop']).convert('RGBA')
    if recipe['propPixelsPerUnit'] != ppu:
        raise ValueError('Carry must use walking weapon scale without resizing')
    px,py = recipe['propPivot']
    for i,frame in enumerate(recipe['frames']):
        x,y = i%4*width,i//4*height
        source = unarmed.crop((x,y,x+width,y+height))
        source = isolate(source,body_support(source))
        source = isolate(source,source.getchannel('A').point(lambda a:255 if a>=128 else 0))
        anchor = layout['anchors'][i]
        base = Image.new('RGBA',(working,working))
        base.paste(source.resize((width*ratio,height*ratio),Image.Resampling.NEAREST),
                   (ox-anchor[0]*ratio,oy-anchor[1]*ratio))
        drawing = Image.open(ROOT/frame['source']).convert('RGBA')
        if drawing.width != drawing.height:
            raise ValueError('Local edit must retain square working layout')
        drawing = drawing.resize(base.size,Image.Resampling.NEAREST)
        drawing = isolate(drawing,drawing.getchannel('A').point(lambda a:255 if a>=128 else 0))
        for patch in frame.get('patches',[]):
            x,y,w,h = patch['rect']
            pixels = Image.open(ROOT/patch['source']).convert('RGBA').resize((w,h),Image.Resampling.NEAREST)
            pixels = isolate(pixels,pixels.getchannel('A').point(lambda a:255 if a>=128 else 0))
            canvas = Image.new('RGBA',base.size)
            canvas.paste(pixels,(x,y))
            drawing = Image.composite(canvas,drawing,polygon_mask(base.size,patch['regions']))
        if frame['erase']:
            drawing = isolate(drawing,ImageChops.invert(polygon_mask(base.size,frame['erase'])))
        mask = polygon_mask(base.size,frame['regions'])
        body = Image.composite(drawing,base,mask)
        # No generated head/cap/face/legs can leak out of the local edit.
        difference = ImageChops.difference(body,base)
        assert not isolate(difference,ImageChops.invert(mask)).getbbox(alpha_only=False), i
        # Remove detached remnants of the superseded arm silhouettes, retaining
        # the connected body's alpha fringe. No outline erosion or recoloring.
        body = isolate(body,body_support(body))
        registered = register(body,size,scale,recipe['workingOrigin'],config['origin'])
        empty = register(source,size,ppu/layout['density'],anchor,config['origin'])
        assert not ImageChops.difference(register(base,size,scale,recipe['workingOrigin'],config['origin']),empty).getbbox(alpha_only=False), i
        for box in ((0,0,size,416),(0,500,size,size)):
            assert not ImageChops.difference(registered.crop(box),empty.crop(box)).getbbox(alpha_only=False), (i,box)
        gx = config['origin'][0]+(frame['grip'][0]-ox)*scale
        gy = config['origin'][1]+(frame['grip'][1]-oy)*scale
        angle = math.radians(frame['angle'])
        c,s = math.cos(angle),math.sin(angle)
        # Inverse mapping: same east flip and rigid rotation as walking.
        prop = axe.transform((size,size),Image.Transform.AFFINE,
            (-c,-s,px+c*gx+s*gy,-s,c,py+s*gx-c*gy),Image.Resampling.NEAREST)
        coverage = registered.getchannel('A').point(lambda a:255 if a else 0)
        # Keep even semitransparent character-edge colors unchanged. Bake the
        # occlusion into the weapon cutout, never weapon colors into the body.
        body = registered
        weapon = isolate(prop,ImageChops.invert(coverage))
        master = Image.alpha_composite(body,weapon)
        assert not ImageChops.difference(Image.alpha_composite(body,weapon),master).getbbox(alpha_only=False), i
        for key,cel in (('Body',body),('Weapon',weapon),('Master',master),('Unarmed',empty)):
            box = cel.getbbox()
            assert box and min(box[:2])>0 and max(box[2:])<size, (i,key,'clipping')
            sheets[key].paste(cel,(i%4*size,i//4*size))
    output.mkdir(parents=True,exist_ok=True)
    for key,sheet in sheets.items():
        sheet.save((review if key in ("Master", "Reveal") else output)/f'Sprint-{name}-{key}.png')
    print(f'{name}:8 local carry drawings; unchanged head/cap/legs; fixed walking axe scale; exact layer reconstruction.')
