"""Register accepted throw cels and split authored weapon masks without repainting them.

User authorized precise masks/cutouts on 25 Sep 2026 after generative extraction
changed the accepted poses. Pillow is the only dependency. Run from any directory.
Hidden-body patches are generated artwork, limited to explicit reveal regions.
"""
import json
from player_contours import normalize_layers, normalize_prop
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[3]
CONFIG = Path(__file__).with_name("player_throw_layers.json")
OUTPUT = ROOT / "RoadOfTheOldKing/Assets/Art/Sprites/Player/Actions/Throw"
REVIEW = ROOT / "ArtSource/Player/Registered/Combat"


def polygon_mask(size, polygons):
    mask = Image.new("L", size)
    draw = ImageDraw.Draw(mask)
    for polygon in polygons:
        draw.polygon([tuple(point) for point in polygon], fill=255)
    return mask


def isolate(image, mask):
    result = image.copy()
    result.putalpha(ImageChops.multiply(image.getchannel("A"), mask))
    # Transparent pixels have no meaningful RGB; canonicalize for reproducible checks.
    result.paste((0, 0, 0, 0), mask=result.getchannel("A").point(lambda a: 255 if a == 0 else 0))
    return result


def body_support(body):
    # Ignore low-alpha sheet haze while finding the body island. Keep its edge fringe.
    alpha = body.getchannel("A")
    remaining = {(x, y) for y in range(body.height) for x in range(body.width) if alpha.getpixel((x, y)) >= 128}
    largest = set()
    while remaining:
        seed = remaining.pop()
        component, todo = {seed}, [seed]
        while todo:
            x, y = todo.pop()
            for dx, dy in ((-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)):
                point = (x+dx, y+dy)
                if point in remaining:
                    remaining.remove(point); component.add(point); todo.append(point)
        if len(component) > len(largest): largest = component
    support = Image.new("L", body.size)
    for point in largest: support.putpixel(point, 255)
    return support.filter(ImageFilter.MaxFilter(7))


def main():
    config = json.loads(CONFIG.read_text())
    size, origin, ppu = config["canvas"], config["origin"], config["pixelsPerUnit"]
    OUTPUT.mkdir(parents=True, exist_ok=True)
    REVIEW.mkdir(parents=True, exist_ok=True)
    manifest = {"canvas": size, "origin": origin, "pixelsPerUnit": ppu, "views": {}}
    for name, view in config["views"].items():
        master = Image.open(ROOT / view["source"]).convert("RGBA")
        authored_mask = Image.open(ROOT / view["weaponMask"]).convert("L") if view.get("weaponMask") else None
        if authored_mask is not None and authored_mask.size != master.size:
            raise ValueError(f"Throw {name}: weapon mask must match the source canvas")
        underpaint = Image.open(ROOT / view["underpaint"]).convert("RGBA") if view.get("underpaint") else Image.new("RGBA", master.size)
        if master.size != underpaint.size:
            raise ValueError("Underpaint registration does not match master")
        sheets = {key: Image.new("RGBA", (size*4, size*2)) for key in ("Body", "Weapon", "Reveal", "Master")}
        # ONE scale per source view. Never fit individual silhouettes.
        scale = ppu / view["density"]
        for index, frame in enumerate(view["frames"]):
            x, y, w, h = frame["rect"]
            source = master.crop((x, y, x+w, y+h))
            # Uneven source layout can put a neighboring cel inside a crop. Keep
            # only this complete pose; these masks never move or redraw anatomy.
            if frame.get("sourceRegion"):
                source = isolate(source, polygon_mask(source.size, [frame["sourceRegion"]]))
            if authored_mask is not None:
                mask = authored_mask.crop((x,y,x+w,y+h))
            else:
                pattern = view["frames"][frame["copy"]] if "copy" in frame else frame
                dx, dy = frame.get("shift", [0, 0])
                polygons = [[[px+dx, py+dy] for px, py in polygon] for polygon in pattern.get("polygons", [])]
                mask = polygon_mask(source.size, polygons)
                if "handRegion" in frame:
                    padding = view.get("maskPadding", 9)
                    if padding > 1:
                        mask = mask.filter(ImageFilter.MaxFilter(padding))
                    hand = Image.new("L", source.size)
                    hx0, hy0, hx1, hy1 = frame["handRegion"]
                    for hy in range(hy0, hy1):
                        for hx in range(hx0, hx1):
                            r, g, b, a = source.getpixel((hx, hy))
                            if a > 180 and r > 235 and 90 < g < 230 and b < 180 and r > g*1.15:
                                hand.putpixel((hx, hy), 255)
                    # Protect the skin island plus its existing dark outline. This classifies
                    # source pixels only; it never paints skin or changes the hand silhouette.
                    hand = hand.filter(ImageFilter.MaxFilter(9))
                    mask = ImageChops.subtract(mask, hand)
                    if name == "east" and index in (1, 2, 3):
                        # The small butt crosses the cap's blue cloth/red band. Retain those
                        # original costume pixels; only the brown haft belongs to this layer.
                        for py in range(178, 250):
                            for px in range(153, 202):
                                r, g, b, a = source.getpixel((px, py))
                                cloth = b >= g*.98 and r < g*1.2
                                band = r > g*1.6 and g < b*1.5
                                if cloth or band: mask.putpixel((px, py), 0)
            body = isolate(source, ImageChops.invert(mask))
            if index < 6 and authored_mask is None:
                # Reassign isolated axe-edge specks to the weapon instead of erasing them.
                mask = ImageChops.lighter(mask, ImageChops.invert(body_support(body)))
                body = isolate(source, ImageChops.invert(mask))
            weapon = isolate(source, mask)
            # Complementary masks must reconstruct every visible source pixel exactly.
            canonical = isolate(source, Image.new("L", source.size, 255))
            joined = Image.alpha_composite(body, weapon)
            if ImageChops.difference(canonical, joined).getbbox(alpha_only=False):
                raise AssertionError(f"Noncomplementary split: {name} cel {index+1}")
            reveal_polygon = (view["frames"][frame["copy"]] if "copy" in frame else frame).get("reveal", [])
            dx, dy = frame.get("shift", [0, 0])
            reveal_region = polygon_mask(source.size, [[[px+dx, py+dy] for px, py in reveal_polygon]] if reveal_polygon else [])
            reveal_mask = ImageChops.multiply(mask, reveal_region)
            reveal = isolate(underpaint.crop((x, y, x+w, y+h)), reveal_mask)
            # Convert to a shared fixed canvas/pivot with nearest-neighbour sampling.
            affine = (1/scale, 0, frame["anchor"][0]-origin[0]/scale,
                      0, 1/scale, frame["anchor"][1]-origin[1]/scale)
            cel_origin = ((index % 4)*size, (index//4)*size)
            for key, image in (("Body", body), ("Weapon", weapon), ("Reveal", reveal), ("Master", canonical)):
                registered = image.transform((size, size), Image.Transform.AFFINE, affine, Image.Resampling.NEAREST)
                box = registered.getbbox()
                if box and (box[0] == 0 or box[1] == 0 or box[2] == size or box[3] == size):
                    raise ValueError(f"Canvas clips {name} {index+1} {key}")
                sheets[key].paste(registered, cel_origin)
        normalize_layers(sheets)
        paths = {}
        for key, sheet in sheets.items():
            path = (REVIEW if key in ("Master", "Reveal") else OUTPUT) / f"Throw-{name.title()}-{key}.png"
            sheet.save(path)
            paths[key.lower()] = path.relative_to(ROOT).as_posix()
        # Validate the exported, normalized layers too (RGBA, not only RGB or bounds).
        if ImageChops.difference(Image.alpha_composite(sheets["Body"], sheets["Weapon"]), sheets["Master"]).getbbox(alpha_only=False):
            raise AssertionError("Registered layers no longer reconstruct the master")
        manifest["views"][name] = {"files": paths, "sourceDensity": view["density"], "scale": scale}
        print(f"{name}: 8 registered cels; exact RGBA recomposition; 640x640, 128 PPU; no clipping")
    (REVIEW / "layers.json").write_text(json.dumps(manifest, indent=2)+"\n")


if __name__ == "__main__":
    main()
