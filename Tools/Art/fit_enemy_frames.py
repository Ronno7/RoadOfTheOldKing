"""Fit downloaded enemy frames, in both local copies, and record the new hashes in the manifest.

  shrink <character> <slot> <factor>   Redraw a slot's frames smaller, keeping their palette, with a clean rebuilt outline.
  plant <character> <slot>             Shift a slot's frames so the feet stand where the standing view's do, so poses don't slide.
  snap <character> <slot>              Move any pixel off the standing views' colours to the nearest of them.

Run the downloader first, then once on the newly downloaded slot: shrinking a slot twice shrinks it twice.
An interrupted run leaves a journal; the next run only finishes it, so nothing is applied twice.
"""
import argparse
import hashlib
import io
import json
import os
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "ArtSource/Enemies/LowlandsManifest.json"
JOURNAL = MANIFEST.with_name("fit_enemy_frames.pending.json")
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


def snapped(image, palette):
    """Every opaque pixel on one of the palette's colours: the nearest, by RGB distance."""
    result = image.convert("RGBA")
    pixels = result.load()
    for y in range(result.height):
        for x in range(result.width):
            r, g, b, a = pixels[x, y]
            if a and (r, g, b) not in palette:
                nr, ng, nb = min(palette, key=lambda c: (c[0] - r) ** 2 + (c[1] - g) ** 2 + (c[2] - b) ** 2)
                pixels[x, y] = (nr, ng, nb, 255)
    return result


def png(image):
    buffer = io.BytesIO()
    image.save(buffer, "PNG")
    return buffer.getvalue()


def staged(path):
    """The temporary file beside a target; the leading dot keeps Unity from importing it."""
    return path.with_name(f".{path.name}.tmp")


def write_synced(path, data):
    with open(path, "wb") as file:
        file.write(data)
        file.flush()
        os.fsync(file.fileno())


def commit(writes, operation):
    """Stage every new file beside its target, journal the swaps, then make them. Until the journal lands nothing has
    changed and the staged files are removed; once it has, the swaps are owed and finish() makes them, now or next run."""
    temps = [staged(path) for path, _ in writes] + [staged(JOURNAL)]
    try:
        entries = []
        for path, data in writes:
            write_synced(staged(path), data)
            entries.append([staged(path).relative_to(ROOT).as_posix(), path.relative_to(ROOT).as_posix(),
                            hashlib.sha256(data).hexdigest()])
        write_synced(staged(JOURNAL), json.dumps({"operation": operation, "replace": entries}, indent=1).encode("utf-8"))
        os.replace(staged(JOURNAL), JOURNAL)
    except BaseException:
        if not JOURNAL.exists():
            for temp in temps:
                temp.unlink(missing_ok=True)
        raise
    finish()


def finish():
    """Make the journal's swaps, skipping those already made, then drop it. Returns the journalled operation."""
    journal = json.loads(JOURNAL.read_bytes())
    for temp, target, digest in journal["replace"]:
        temp, target = ROOT / temp, ROOT / target
        if temp.exists():
            os.replace(temp, target)
        elif not target.exists() or hashlib.sha256(target.read_bytes()).hexdigest() != digest:
            sys.exit(f"Cannot finish {JOURNAL}: {target} is neither staged nor written.")
    JOURNAL.unlink()
    return journal["operation"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("shrink", "plant", "snap"))
    parser.add_argument("character")
    parser.add_argument("slot")
    parser.add_argument("factor", nargs="?", type=float, help="for shrink")
    args = parser.parse_args()
    if args.mode == "shrink" and args.factor is None:
        parser.error("shrink needs a factor")
    if JOURNAL.exists():
        sys.exit(f"Finished the interrupted '{finish()}'; nothing else was done. Run again only if still needed.")
    manifest = json.loads(MANIFEST.read_bytes())
    character = next(c for c in manifest["characters"] if c["name"] == args.character)
    count = 0
    palette = {c[:3] for view in (ROOTS[0] / character["name"] / "Rotations").glob("*.png")
               for c in Image.open(view).convert("RGBA").get_flattened_data() if c[3]} if args.mode == "snap" else None
    writes = []
    for frame in character["files"]:
        if frame["slot"] != args.slot:
            continue
        relative = Path(character["name"]) / frame["slot"] / frame["file"]
        path = ROOTS[0] / relative
        if args.mode == "shrink":
            data = png(shrink(Image.open(path), args.factor))
        elif args.mode == "snap":
            data = png(snapped(Image.open(path), palette))
        else:
            standing = Image.open(ROOTS[0] / character["name"] / "Rotations" / (frame["direction"] + ".png"))
            data = png(planted(Image.open(path), standing))
        # Both copies must be byte-identical for the manifest audit.
        writes += [(base / relative, data) for base in ROOTS]
        frame["sha256"] = hashlib.sha256(data).hexdigest()
        count += 1
    writes.append((MANIFEST, (json.dumps(manifest, indent=1) + "\n").encode("utf-8")))
    commit(writes, " ".join(sys.argv[1:]))
    print(f"{args.mode}: {count} frames of {args.character}")


if __name__ == "__main__":
    main()
