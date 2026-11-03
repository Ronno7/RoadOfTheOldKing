"""Fit downloaded enemy frames, in both local copies, and record the new hashes in the manifest.

  shrink <character> <slot> <factor>   Redraw a slot's frames smaller, keeping their palette, with a clean rebuilt outline.
  plant <character> <slot>             Shift a slot's frames so the feet stand where the standing view's do, so poses don't slide.

Run the downloader first, then once on the newly downloaded slot: shrinking a slot twice shrinks it twice.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "ArtSource/Enemies/LowlandsManifest.json"
ROOTS = (ROOT / "ArtSource/Enemies", ROOT / "RoadOfTheOldKing/Assets/Art/Sprites/Enemies")
DARK = 48
SAMPLES = 6
FEET_ROWS = 5


def luma(color):
    return .299 * color[0] + .587 * color[1] + .114 * color[2]


def shrink(image, factor):
    """Area-samples each pixel: empty below half coverage, otherwise the commonest colour, ignoring dark ones unless they
    dominate, so detail doesn't clot. A one-pixel outline in the frame's darkest colour then rebuilds the silhouette."""
    source = image.convert("RGBA")
    width, height = source.size
    opaque = [c for c in source.getdata() if c[3] > 127]
    if not opaque:
        return Image.new("RGBA", (round(width * factor), round(height * factor)), (0, 0, 0, 0))
    outline = min(opaque, key=luma)
    result = Image.new("RGBA", (round(width * factor), round(height * factor)), (0, 0, 0, 0))
    read, write = source.load(), result.load()
    for y in range(result.height):
        for x in range(result.width):
            counts = {}
            for sy in range(SAMPLES):
                for sx in range(SAMPLES):
                    pixel = read[min(width - 1, int((x + (sx + .5) / SAMPLES) / factor)),
                                 min(height - 1, int((y + (sy + .5) / SAMPLES) / factor))]
                    if pixel[3] > 127:
                        counts[pixel] = counts.get(pixel, 0) + 1
            solid = sum(counts.values())
            if solid * 2 < SAMPLES * SAMPLES:
                continue
            light = {c: n for c, n in counts.items() if luma(c) >= DARK}
            dominated = (solid - sum(light.values())) * 5 >= solid * 3
            choices = counts if dominated or not light else light
            write[x, y] = max(choices, key=choices.get)
    edges = [(x, y) for y in range(result.height) for x in range(result.width) if write[x, y][3] and any(
        not (0 <= x + dx < result.width and 0 <= y + dy < result.height) or not write[x + dx, y + dy][3]
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for x, y in edges:
        write[x, y] = outline
    return result


def feet(image):
    """Lowest opaque row and the middle of what stands on it, over the bottom few rows."""
    alpha = image.convert("RGBA").getchannel("A")
    box = alpha.getbbox()
    bottom = box[3]
    columns = [x for x in range(alpha.width) for y in range(max(0, bottom - FEET_ROWS), bottom) if alpha.getpixel((x, y)) > 127]
    return (min(columns) + max(columns)) // 2, bottom


def planted(image, standing):
    """The frame moved so its feet land on the standing view's."""
    fx, fy = feet(image)
    sx, sy = feet(standing)
    result = Image.new("RGBA", image.size, (0, 0, 0, 0))
    result.paste(image.convert("RGBA"), (sx - fx, sy - fy))
    return result


def rewrite(character, frame, image):
    relative = Path(character["name"]) / frame["slot"] / frame["file"]
    paths = [base / relative for base in ROOTS]
    for path in paths:
        image.save(path)
    # Both copies must be byte-identical for the manifest audit.
    data = paths[0].read_bytes()
    paths[1].write_bytes(data)
    frame["sha256"] = hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("shrink", "plant"))
    parser.add_argument("character")
    parser.add_argument("slot")
    parser.add_argument("factor", nargs="?", type=float, help="for shrink")
    args = parser.parse_args()
    if args.mode == "shrink" and args.factor is None:
        parser.error("shrink needs a factor")
    manifest = json.loads(MANIFEST.read_bytes())
    character = next(c for c in manifest["characters"] if c["name"] == args.character)
    count = 0
    for frame in character["files"]:
        if frame["slot"] != args.slot:
            continue
        path = ROOTS[0] / character["name"] / frame["slot"] / frame["file"]
        if args.mode == "shrink":
            rewrite(character, frame, shrink(Image.open(path), args.factor))
        else:
            standing = Image.open(ROOTS[0] / character["name"] / "Rotations" / (frame["direction"] + ".png"))
            rewrite(character, frame, planted(Image.open(path), standing))
        count += 1
    MANIFEST.write_bytes((json.dumps(manifest, indent=1) + "\n").encode("utf-8"))
    print(f"{args.mode}: {count} frames of {args.character}")


if __name__ == "__main__":
    main()
