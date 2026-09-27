"""Register whole sprint poses and split authorized complementary axe masks.

One fixed source density, whole-pose registration, no anatomy transplants or
independent limb transforms. The body keeps its original blob hands and wrists.
"""
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageFilter
from build_player_throw_layers import polygon_mask, isolate, body_support
from build_player_return_layers import register
from player_contours import normalize_layers, normalize_prop

ROOT = Path(__file__).resolve().parents[3]
REVIEW = ROOT / "ArtSource/Player/Registered/Sprint"
OUTPUT = ROOT / "TheLostShrine/Assets/Art/Sprites/Player/Actions/Sprint"


def build_mirrored_view(name, view, config):
    """Bake the user's requested exact reflection, retaining cel order/pivots."""
    source_name = view['mirrorOf']
    if source_name == name or source_name not in config['views'] or 'mirrorOf' in config['views'][source_name]:
        raise ValueError('A mirrored sprint must reference a directly authored view')
    size = config['canvas']
    if config['origin'][0] != size/2:
        raise ValueError('Exact cell reflection requires a centered horizontal foot pivot')
    sheets = {}
    for key in ('Body','Weapon','Reveal','Master','Unarmed'):
        folder = REVIEW if key == 'Master' else OUTPUT
        source = Image.open(folder/f'Sprint-{source_name}-{key}.png').convert('RGBA')
        if source.size != (size*4,size*2):
            raise ValueError('Expected eight registered sprint cels')
        result = Image.new('RGBA',source.size)
        for index in range(8):
            x,y = index%4*size,index//4*size
            cel = source.crop((x,y,x+size,y+size))
            reflected = cel.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            # Mirror each cel, not the atlas; gait order and matching layers stay fixed.
            assert not ImageChops.difference(reflected.transpose(Image.Transpose.FLIP_LEFT_RIGHT),cel).getbbox(alpha_only=False)
            box = reflected.getbbox()
            if box and (min(box[:2]) == 0 or max(box[2:]) == size):
                raise ValueError('Mirrored sprint touches its canvas boundary')
            result.paste(reflected,(x,y))
        sheets[key] = result
    if ImageChops.difference(Image.alpha_composite(sheets['Body'],sheets['Weapon']),sheets['Master']).getbbox(alpha_only=False):
        raise AssertionError('Mirrored body/weapon layers do not reconstruct the drawing')
    for key,sheet in sheets.items():
        sheet.save((REVIEW if key == 'Master' else OUTPUT)/f'Sprint-{name}-{key}.png')
    print(f'{name}: eight exact mirrored {source_name} cels per track; no resampling, color changes or phase reordering.')


def build_view(name, view, config):
    if 'mirrorOf' in view:
        build_mirrored_view(name,view,config)
        return
    if 'carryRecipe' in view:
        from build_player_carry_layers import build_carry_view
        build_carry_view(name,view,config,OUTPUT,REVIEW)
        return
    master = Image.open(ROOT / view["source"]).convert("RGBA")
    if "localRedrawSource" in view:
        # Keep only the requested same-cel leg redraws at their original coordinates.
        # No reference anatomy, moving, resizing or mirroring; preserve all other pixels.
        revision = Image.open(ROOT / view["localRedrawSource"]).convert("RGBA")
        if revision.size != master.size:
            raise ValueError("Local redraw must retain the original sheet dimensions")
        for index in view["redrawFrames"]:
            x, y = index%4*384, index//4*512
            x0, y0, x1, y1 = view["frames"][index]["redrawnRegion"]
            rect = (x+x0, y+y0, x+x1, y+y1)
            master.paste(revision.crop(rect), rect)
        master.save(ROOT / view["packedSource"])
    silhouette = Image.open(ROOT / view["silhouetteSource"]).convert("RGBA") if "silhouetteSource" in view else None
    size, ppu = config["canvas"], config["pixelsPerUnit"]
    if len(view["frames"]) != 8 or (master.size != (1536,1024) and
                                    any("rect" not in frame for frame in view["frames"])):
        raise ValueError("Sprint requires eight cels, with explicit rectangles for a custom source layout")
    sheets = {key: Image.new("RGBA", (size*4,size*2)) for key in ("Body","Weapon","Reveal","Master")}
    for index, frame in enumerate(view["frames"]):
        x,y = index%4*384,index//4*512
        # A few blade tips cross a nominal source-cell edge into empty padding.
        # Take the full character island, discarding any neighboring-cell fragment.
        if "rect" in frame:
            x,y,width,height = frame["rect"]
            if min(x,y) < 0 or min(width,height) <= 0 or x+width > master.width or y+height > master.height:
                raise ValueError("Sprint source rectangle is outside its sheet")
            source = master.crop((x,y,x+width,y+height))
        else:
            source = master.crop((x,y,x+416,y+512))
        if silhouette is not None:
            # The targeted redraw retained the silhouette but introduced opaque haze.
            # Reuse only the study's transparency, never its character pixels.
            original = silhouette.crop((x,y,x+416,y+512))
            support = original.getchannel("A").point(lambda a:255 if a>=128 else 0)
            support = support.filter(ImageFilter.MaxFilter(3))
            if "redrawnRegion" in frame:
                rect = tuple(frame["redrawnRegion"])
                changed = source.crop(rect).getchannel("A").point(lambda a:255 if a>=245 else 0)
                support.paste(changed,rect)
            source = isolate(source,support)
        source = isolate(source,body_support(source))
        source = isolate(source,source.getchannel("A").point(lambda a:255 if a>=128 else 0))
        mask = polygon_mask(source.size,frame["polygons"])
        hand = Image.new("L",source.size)
        cuff = Image.new("L",source.size)
        x0,y0,x1,y1 = frame["handRegion"]
        for py in range(y0,y1):
            for px in range(x0,x1):
                r,g,b,a = source.getpixel((px,py))
                if a>=128 and r>235 and 90<g<230 and b<180 and r>g*1.15:
                    hand.putpixel((px,py),255)
                if a>=128 and r>110 and r>g*1.6 and b>g*.7:
                    cuff.putpixel((px,py),255)
        mask = ImageChops.subtract(mask,hand.filter(ImageFilter.MaxFilter(view.get("handPadding",9))))
        cuff_padding = view.get("cuffPadding",5)
        # Size 1 means keep the exact cuff pixels; Pillow's native rank filter
        # does not need to run for this identity operation.
        if cuff_padding>0:
            mask = ImageChops.subtract(mask,cuff.filter(ImageFilter.MaxFilter(cuff_padding)) if cuff_padding>1 else cuff)
        if "gripOutline" in frame:
            # Override the colour-based guard only around this hand. Brown shaft
            # pixels resemble the red cuff and must not remain in the unarmed body.
            x0, y0, x1, y1 = frame["gripBounds"]
            window = polygon_mask(source.size, [[[x0,y0],[x1,y0],[x1,y1],[x0,y1]]])
            fist = polygon_mask(source.size, [frame["gripOutline"]])
            mask = ImageChops.lighter(mask, ImageChops.subtract(window, fist))
        if "weaponEdgePolygons" in frame:
            # Explicit exposed axe-edge pixels override colour guards that can
            # mistake warm shaft highlights for skin. Never redraw the fist.
            mask = ImageChops.lighter(mask,polygon_mask(source.size,frame["weaponEdgePolygons"]))
        body,weapon = isolate(source,ImageChops.invert(mask)),isolate(source,mask)
        if view.get("cleanDetachedWeapon", False):
            # Detached outline islands belong to the weapon, not the character.
            # Reassign original pixels rather than erasing them from the master.
            mask = ImageChops.lighter(mask, ImageChops.invert(body_support(body)))
            body,weapon = isolate(source,ImageChops.invert(mask)),isolate(source,mask)
        if ImageChops.difference(Image.alpha_composite(body,weapon),source).getbbox(alpha_only=False):
            raise AssertionError(f"Noncomplementary source layers: {index}")
        for key,pixels in (("Body",body),("Weapon",weapon),("Master",source)):
            cel=register(pixels,size,ppu/view["density"],frame["anchor"],config["origin"])
            sheets[key].paste(cel,(index%4*size,index//4*size))
    if view.get("trimContour", True):
        normalize_layers(sheets)
    if ImageChops.difference(Image.alpha_composite(sheets["Body"],sheets["Weapon"]),sheets["Master"]).getbbox(alpha_only=False):
        raise AssertionError("Registered sprint layers do not reconstruct the drawing")
    OUTPUT.mkdir(parents=True,exist_ok=True)
    for key,sheet in sheets.items():
        sheet.save((REVIEW if key=="Master" else OUTPUT)/f"Sprint-{name}-{key}.png")
    if "unarmedRecipe" in view:
        from build_player_unarmed_layers import build_unarmed_view
        build_unarmed_view(name, sheets['Body'], view['unarmedRecipe'], config, OUTPUT)
    elif "unarmedSource" in view:
        # A distinct drawn arm cycle, not the carrying pose with its weapon hidden.
        # Share the registered canvas and distance phase with the equipped track.
        # An independently drawn source can have its own uniform working scale
        # and whole-pose anchors, without stretching individual frames to fit.
        unarmed_source = Image.open(ROOT / view["unarmedSource"]).convert("RGBA")
        layout = view.get("unarmedLayout", {})
        width, height = layout.get("cellSize", [384, 512])
        anchors = layout.get("anchors", [frame["anchor"] for frame in view["frames"]])
        density = layout.get("density", view["density"])
        if unarmed_source.size != (width*4, height*2) or len(anchors) != 8 or density <= 0:
            raise ValueError("Unarmed sprint requires eight cels at one positive source density")
        unarmed = Image.new("RGBA", sheets["Body"].size)
        for index, anchor in enumerate(anchors):
            x,y = index%4*width,index//4*height
            source = unarmed_source.crop((x,y,x+width,y+height))
            source = isolate(source,body_support(source))
            source = isolate(source,source.getchannel("A").point(lambda a:255 if a>=128 else 0))
            cel = register(source,size,ppu/density,anchor,config["origin"])
            unarmed.paste(cel,(index%4*size,index//4*size))
        if layout.get("trimContour", True):
            unarmed = normalize_prop(unarmed)
        unarmed.save(OUTPUT/f"Sprint-{name}-Unarmed.png")
    print(f"{name} sprint: eight whole poses, exact RGBA reconstruction, fixed source density, no clipping.")


def main():
    config = json.loads(Path(__file__).with_name("player_sprint_layers.json").read_text())
    # Build authored sources before their reflected exports.
    for name, view in sorted(config["views"].items(),key=lambda item:'mirrorOf' in item[1]):
        build_view(name, view, config)
    (REVIEW/"sprint.json").write_text(json.dumps({key:config[key] for key in
        ("canvas","pixelsPerUnit","origin","columns","celCount")},indent=2)+"\n")


if __name__=="__main__":
    main()
