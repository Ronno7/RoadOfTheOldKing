"""Register the drawn catch and spin studies using authorized pixel masks.

Catch uses complementary body/weapon cutouts; no pose is repainted or rotated.
One source-density scale is shared by every cel in each view.
"""
import json
from player_contours import normalize_layers, normalize_prop
from pathlib import Path
from PIL import Image, ImageChops, ImageFilter
from build_player_throw_layers import polygon_mask, isolate, body_support

ROOT = Path(__file__).resolve().parents[3]
CONFIG = Path(__file__).with_name("player_return_layers.json")
REVIEW = ROOT / "ArtSource/Player/Registered/Return"
OUTPUT = ROOT / "TheLostShrine/Assets/Art/Sprites/Player/Actions/Catch"
SPIN = ROOT / "TheLostShrine/Assets/Art/Sprites/Weapons/Spin"


def register(image, size, scale, anchor, origin):
    affine = (1/scale, 0, anchor[0]-origin[0]/scale,
              0, 1/scale, anchor[1]-origin[1]/scale)
    result = image.transform((size, size), Image.Transform.AFFINE, affine, Image.Resampling.NEAREST)
    box = result.getbbox()
    if box and (box[0] == 0 or box[1] == 0 or box[2] == size or box[3] == size):
        raise ValueError("Registered cel touches canvas boundary")
    return result


def main():
    config = json.loads(CONFIG.read_text())
    size, ppu, origin = config["canvas"], config["pixelsPerUnit"], config["origin"]
    OUTPUT.mkdir(parents=True, exist_ok=True)
    SPIN.mkdir(parents=True, exist_ok=True)
    manifest = {key: config[key] for key in ("canvas", "pixelsPerUnit", "origin", "columns", "exposures", "possessionCel")}
    manifest.update(contactCel=-1, recoveryCel=-1, views={})
    for name, view in config["views"].items():
        master = Image.open(ROOT / view["source"]).convert("RGBA")
        authored_mask = Image.open(ROOT / view["weaponMask"]).convert("L") if view.get("weaponMask") else None
        if authored_mask is not None and authored_mask.size != master.size:
            raise ValueError(f"Catch {name}: weapon mask must match the source canvas")
        sheets = {key: Image.new("RGBA", (size*2, size*2)) for key in ("Body", "Weapon", "Reveal", "Master")}
        scale = ppu / view["density"]
        for index, frame in enumerate(view["frames"]):
            x, y, w, h = frame["rect"]
            source = master.crop((x, y, x+w, y+h))
            support = body_support(source)
            opaque = source.getchannel("A").point(lambda a: 255 if a >= 128 else 0)
            if ImageChops.subtract(opaque, support).getbbox():
                raise AssertionError(f"Catch {name} {index}: disconnected opaque artwork")
            source = isolate(source, support)
            mask = authored_mask.crop((x,y,x+w,y+h)) if authored_mask is not None else polygon_mask(source.size, frame["polygons"])
            if authored_mask is None and "handRegion" in frame:
                mask = mask.filter(ImageFilter.MaxFilter(9))
                hand = Image.new("L", source.size)
                hx0, hy0, hx1, hy1 = frame["handRegion"]
                for hy in range(hy0, hy1):
                    for hx in range(hx0, hx1):
                        r, g, b, a = source.getpixel((hx, hy))
                        if a > 180 and r > 235 and 90 < g < 230 and b < 180 and r > g*1.15:
                            hand.putpixel((hx, hy), 255)
                mask = ImageChops.subtract(mask, hand.filter(ImageFilter.MaxFilter(9)))
            body = isolate(source, ImageChops.invert(mask))
            if index and authored_mask is None:
                mask = ImageChops.lighter(mask, ImageChops.invert(body_support(body)))
                body = isolate(source, ImageChops.invert(mask))
            weapon = isolate(source, mask)
            canonical = isolate(source, Image.new("L", source.size, 255))
            if ImageChops.difference(Image.alpha_composite(body, weapon), canonical).getbbox(alpha_only=False):
                raise AssertionError(f"Noncomplementary split: {name} {index}")
            if "weaponGrip" in frame:
                # Correct the oversized catch prop around its own gripping hand.
                # Preserve every original body/hand pixel; never transplant anatomy.
                factor = view["weaponScale"]
                gx, gy = frame["weaponGrip"]
                weapon = weapon.transform(source.size, Image.Transform.AFFINE,
                    (1/factor, 0, gx-gx/factor, 0, 1/factor, gy-gy/factor), Image.Resampling.NEAREST)
                body_pixels = body.getchannel("A").point(lambda a: 255 if a else 0)
                weapon = isolate(weapon, ImageChops.invert(body_pixels))
                canonical = Image.alpha_composite(body, weapon)
            for key, image in (("Body", body), ("Weapon", weapon), ("Master", canonical)):
                sheets[key].paste(register(image, size, scale, frame["anchor"], origin), ((index%2)*size, (index//2)*size))
        normalize_layers(sheets)
        paths = {}
        for key, sheet in sheets.items():
            path = (REVIEW if key in ("Master", "Reveal") else OUTPUT) / f"Catch-{name.title()}-{key}.png"
            sheet.save(path)
            paths[key.lower()] = path.relative_to(ROOT).as_posix()
        if ImageChops.difference(Image.alpha_composite(sheets["Body"], sheets["Weapon"]), sheets["Master"]).getbbox(alpha_only=False):
            raise AssertionError("Registered layers do not reconstruct master")
        manifest["views"][name] = {"files": paths, "sourceDensity": view["density"], "scale": scale, "spinScale": view["spinScale"], "weaponScale": view.get("weaponScale", 1)}
        print(f"{name}: 4 catch cels, exact RGBA reconstruction, no clipping")
    (REVIEW / "catch-layers.json").write_text(json.dumps(manifest, indent=2)+"\n")

    spin = config["spin"]
    master = Image.open(ROOT / spin["source"]).convert("RGBA")
    size = spin["canvas"]
    sheet = Image.new("RGBA", (size*4, size*2))
    for index, anchor in enumerate(spin["anchors"]):
        x, y = index%4, index//4
        source = master.crop((round(x*master.width/4), round(y*master.height/2), round((x+1)*master.width/4), round((y+1)*master.height/2)))
        support = body_support(source)
        opaque = source.getchannel("A").point(lambda a: 255 if a >= 128 else 0)
        if ImageChops.subtract(opaque, support).getbbox():
            raise AssertionError(f"Spin {index}: disconnected opaque artwork would be removed")
        cel = register(isolate(source, support), size, spin["pixelsPerUnit"]/spin["density"], anchor, [size/2, size/2])
        if not cel.getbbox(): raise AssertionError("Empty spin cel")
        sheet.paste(normalize_prop(cel), (x*size, y*size))
    path = SPIN / "Axe-Spin.png"
    sheet.save(path)
    (REVIEW / "spin.json").write_text(json.dumps({"canvas": size, "pixelsPerUnit": spin["pixelsPerUnit"], "columns": 4, "celCount": 8, "rotationsPerSecond": spin["rotationsPerSecond"], "sprite": path.relative_to(ROOT).as_posix()}, indent=2)+"\n")
    print("spin: 8 drawn cels, fixed scale, no clipping; 4 rotations/second")


if __name__ == "__main__":
    main()
