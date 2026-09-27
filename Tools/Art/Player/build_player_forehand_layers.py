"""Split approved forehands with user-authorized precise masks; never repaint visible poses."""
import json
from player_contours import normalize_layers, normalize_prop
from pathlib import Path
from PIL import Image, ImageChops, ImageFilter
from build_player_throw_layers import polygon_mask, isolate, body_support

ROOT = Path(__file__).resolve().parents[3]
CONFIG = Path(__file__).with_name("player_forehand_layers.json")
OUTPUT = ROOT / "TheLostShrine/Assets/Art/Sprites/Player/Actions/Forehand"
REVIEW = ROOT / "ArtSource/Player/Registered/Combat"


def main():
    config = json.loads(CONFIG.read_text())
    size, origin, ppu, columns = config["canvas"], config["origin"], config["pixelsPerUnit"], config["columns"]
    OUTPUT.mkdir(parents=True, exist_ok=True)
    manifest = {k: config[k] for k in ("canvas", "origin", "pixelsPerUnit", "columns", "exposures", "contactCel", "recoveryCel")}
    manifest["views"] = {}
    for name, view in config["views"].items():
        master = Image.open(ROOT / view["source"]).convert("RGBA")
        authored_mask = Image.open(ROOT / view["weaponMask"]).convert("L") if view.get("weaponMask") else None
        if authored_mask is not None and authored_mask.size != master.size:
            raise ValueError(f"Forehand {name}: weapon mask must match the source canvas")
        underpaint = Image.open(ROOT / view["underpaint"]).convert("RGBA") if view.get("underpaint") else Image.new("RGBA", master.size)
        if master.size != underpaint.size:
            raise ValueError("Underpaint registration does not match master")
        sheets = {key: Image.new("RGBA", (size*columns, size*2)) for key in ("Body", "Weapon", "Reveal", "Master")}
        scale = ppu / view["density"]
        for index, frame in enumerate(view["frames"]):
            x, y, w, h = frame["rect"]
            source = master.crop((x, y, x+w, y+h))
            if authored_mask is not None:
                mask = authored_mask.crop((x,y,x+w,y+h))
            else:
                mask = polygon_mask(source.size, frame["polygons"])
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
                mask = ImageChops.subtract(mask, hand.filter(ImageFilter.MaxFilter(9)))
                if "keepClothRegion" in frame:
                    x0, y0, x1, y1 = frame["keepClothRegion"]
                    for py in range(y0, y1):
                        for px in range(x0, x1):
                            r, g, b, a = source.getpixel((px, py))
                            if (b >= g*.98 and r < g*1.2) or (r > g*1.45 and b > g*.75) or min(r,g,b) > 180:
                                mask.putpixel((px, py), 0)
                if frame.get("bodyPolygons"):
                    mask = ImageChops.subtract(mask, polygon_mask(source.size, frame["bodyPolygons"]))
                body = isolate(source, ImageChops.invert(mask))
                mask = ImageChops.lighter(mask, ImageChops.invert(body_support(body)))
            body, weapon = isolate(source, ImageChops.invert(mask)), isolate(source, mask)
            canonical = isolate(source, Image.new("L", source.size, 255))
            if ImageChops.difference(canonical, Image.alpha_composite(body, weapon)).getbbox(alpha_only=False):
                raise AssertionError(f"Noncomplementary split: {name} cel {index+1}")
            region = polygon_mask(source.size, [frame["reveal"]] if frame["reveal"] else [])
            reveal = isolate(underpaint.crop((x,y,x+w,y+h)), ImageChops.multiply(mask,region))
            affine = (1/scale, 0, frame["anchor"][0]-origin[0]/scale, 0, 1/scale, frame["anchor"][1]-origin[1]/scale)
            for key, layer in (("Body", body), ("Weapon", weapon), ("Reveal", reveal), ("Master", canonical)):
                registered = layer.transform((size,size), Image.Transform.AFFINE, affine, Image.Resampling.NEAREST)
                box = registered.getbbox()
                if box and (box[0] == 0 or box[1] == 0 or box[2] == size or box[3] == size):
                    raise ValueError(f"Canvas clips {name} {index+1} {key}")
                sheets[key].paste(registered, (index%columns*size,index//columns*size))
        normalize_layers(sheets)
        paths = {}
        for key, sheet in sheets.items():
            path = (REVIEW if key in ("Master", "Reveal") else OUTPUT) / f"Forehand-{name.title()}-{key}.png"
            sheet.save(path)
            paths[key.lower()] = path.relative_to(ROOT).as_posix()
        if ImageChops.difference(Image.alpha_composite(sheets["Body"], sheets["Weapon"]), sheets["Master"]).getbbox(alpha_only=False):
            raise AssertionError("Registered layers no longer reconstruct the master")
        manifest["views"][name] = {"files": paths, "sourceDensity": view["density"], "scale": scale}
        print(f"{name}: 6 registered forehand cels; exact RGBA recomposition; 640x640, 128 PPU; no clipping")
    (REVIEW / "forehand-layers.json").write_text(json.dumps(manifest, indent=2)+"\n")


if __name__ == "__main__":
    main()
