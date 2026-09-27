"""Build the original character reference board from unchanged source pixels."""
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Docs/Art/Player/Previews"
ORIGINAL = ROOT / "TheLostShrine/Assets/Art/Sprites/Player/PlayerSheet.png"
FONT = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 18)
SMALL = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 15)
BG = (49, 59, 55)
PANEL = (73, 83, 74)
INK = (238, 226, 204)
MUTED = (196, 206, 190)


def originals():
    sheet = Image.open(ORIGINAL).convert("RGBA")
    metadata = ORIGINAL.with_suffix(".png.meta").read_text()
    frames = {}
    pattern = (r"name: ((?:South|East|West|North)_\d+)\s+rect:\s+"
               r"serializedVersion: \d+\s+x: (\d+)\s+y: (\d+)\s+"
               r"width: (\d+)\s+height: (\d+)\s+alignment: \d+\s+"
               r"pivot: \{x: ([\d.]+), y: ([\d.]+)\}")
    for name, x, y, w, h, px, py in re.findall(pattern, metadata):
        x, y, w, h = map(int, (x, y, w, h))
        frames[name] = (sheet.crop((x, sheet.height-y-h, x+w, sheet.height-y)),
                        (round(float(px)*w), round(h-float(py)*h)))
    if len(frames) != 16:
        raise ValueError("Expected all sixteen original imported cels")
    return frames


def text(draw, xy, value, small=False, color=INK):
    draw.text(xy, value, font=SMALL if small else FONT, fill=color)


def place(board, cel, anchor, origin):
    board.paste(cel, (origin[0]-anchor[0], origin[1]-anchor[1]), cel)


def reference(frames):
    board = Image.new("RGB", (1200, 930), BG)
    draw = ImageDraw.Draw(board)
    text(draw, (24, 18), "PLAYER / ORIGINAL VISUAL STANDARD")
    text(draw, (24, 48), "Unchanged source pixels / native texture scale / original Unity foot pivots", True)
    for i, direction in enumerate(("South", "East", "West", "North")):
        x = 20+i*295
        draw.rectangle((x, 85, x+275, 420), fill=PANEL)
        text(draw, (x+14, 99), direction)
        draw.line((x+12, 399, x+263, 399), fill=(105, 116, 99))
        cel, anchor = frames[direction+"_00"]
        place(board, cel, anchor, (x+137, 399))
    east, _ = frames["East_00"]
    details = [
        ("CAP / repeating stepped red + cream", (0, 0, 182, 108),
         "Keep motif spacing, blue shading and one tip."),
        ("BOOTS / short, muted gray bands", (60, 190, 182, 265),
         "Keep compact feet and low-contrast highlights."),
        ("HANDS / rounded blobs with wrists", (74, 145, 182, 221),
         "Draw each pose; no fingers or pasted anatomy."),
    ]
    for i, (title, rect, note) in enumerate(details):
        x = 20+i*395
        text(draw, (x, 451), title, True)
        crop = east.crop(rect)
        crop = crop.resize((crop.width*2, crop.height*2), Image.Resampling.NEAREST)
        board.paste(crop, (x, 487), crop)
        text(draw, (x, 721), "2x nearest-neighbour detail", True, MUTED)
        # Fixed short lines keep this a compact visual reference.
        words, lines, current = note.split(), [], ""
        for word in words:
            if len(current)+len(word)+1 > 39:
                lines.append(current); current = word
            else:
                current = (current+" "+word).strip()
        lines.append(current)
        for j, line in enumerate(lines):
            text(draw, (x, 750+j*22), line, True)
    text(draw, (24, 835), "Match compact proportions, pixel-cluster size, restrained shading and soft charcoal edges.")
    text(draw, (24, 864), "Whole drawn poses / right weapon hand / asymmetric satchel / body-led action and weight.", True)
    text(draw, (24, 889), "128 PPU describes world scale; it does not establish a pixel-art style by itself.", True, MUTED)
    board.save(OUT / "Original-Reference.png")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    frames = originals()
    reference(frames)
    print("Built original reference board.")


if __name__ == "__main__":
    main()
