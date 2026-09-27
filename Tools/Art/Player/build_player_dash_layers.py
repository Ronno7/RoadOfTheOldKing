"""Register whole dash drawings and authorized complementary weapon cutouts.

No limbs are transplanted, rotated or rescaled independently. One source density
and explicit ground anchors preserve proportions through compression and landing.
"""
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageFilter
from build_player_throw_layers import polygon_mask, isolate
from build_player_return_layers import register
from player_contours import normalize_layers

ROOT = Path(__file__).resolve().parents[3]
REVIEW = ROOT / "ArtSource/Player/Registered/Dash"
OUTPUT = ROOT / "RoadOfTheOldKing/Assets/Art/Sprites/Player/Actions/Dash"


def main():
    config = json.loads(Path(__file__).with_name("player_dash_layers.json").read_text())
    for name, view in config["views"].items():
        master = Image.open(ROOT / view["source"]).convert("RGBA")
        authored_mask = Image.open(ROOT / view["weaponMask"]).convert("L") if view.get("weaponMask") else None
        if authored_mask is not None and authored_mask.size != master.size:
            raise ValueError(f"Dash {name}: weapon mask must match the source canvas")
        size, ppu = config["canvas"], config["pixelsPerUnit"]
        sheets = {key: Image.new("RGBA", (size*3, size*2)) for key in ("Body", "Weapon", "Reveal", "Master")}
        for index, frame in enumerate(view["frames"]):
            x, y = index % 3 * 512, index // 3 * 512
            # Explicit bounds can include a boot crossing the nominal cell edge
            # into empty padding without picking up the neighbouring row.
            x,y,width,height = frame.get("rect",[x,y,512,512])
            source = master.crop((x, y, x+width, y+height))
            # Discard the generated low-alpha background haze, preserving drawn pixels.
            source = isolate(source, source.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
            if authored_mask is not None:
                mask = authored_mask.crop((x, y, x+width, y+height))
            else:
                mask = polygon_mask(source.size, frame["polygons"])
                hand = Image.new("L", source.size)
                x0, y0, x1, y1 = frame["handRegion"]
                for py in range(y0, y1):
                    for px in range(x0, x1):
                        r, g, b, a = source.getpixel((px, py))
                        if a >= 128 and r > 235 and 90 < g < 230 and b < 180 and r > g*1.15:
                            hand.putpixel((px, py), 255)
                mask = ImageChops.subtract(mask, hand.filter(ImageFilter.MaxFilter(view.get("handPadding", 9))))
            body, weapon = isolate(source, ImageChops.invert(mask)), isolate(source, mask)
            if ImageChops.difference(Image.alpha_composite(body, weapon), source).getbbox(alpha_only=False):
                raise AssertionError(f"Noncomplementary source layers: {index}")
            for key, pixels in (("Body", body), ("Weapon", weapon), ("Master", source)):
                cel = register(pixels, size, ppu/view["density"], frame["anchor"], config["origin"])
                sheets[key].paste(cel, (index%3*size, index//3*size))
        normalize_layers(sheets)
        if ImageChops.difference(Image.alpha_composite(sheets["Body"], sheets["Weapon"]), sheets["Master"]).getbbox(alpha_only=False):
            raise AssertionError("Registered layers do not reconstruct the drawing")
        OUTPUT.mkdir(parents=True, exist_ok=True)
        for key, sheet in sheets.items():
            sheet.save((REVIEW if key in ("Master", "Reveal") else OUTPUT) / f"Dash-{name}-{key}.png")
    (REVIEW / "dash.json").write_text(json.dumps({key:config[key] for key in
        ("canvas", "pixelsPerUnit", "origin", "columns", "exposures")}, indent=2)+"\n")
    print("Dash: five whole poses per view, matched body/weapon, exact RGBA reconstruction, no clipping.")


if __name__ == "__main__":
    main()
