"""Road of the Old King pixel font: hand-placed glyphs compiled to a TrueType font.

Usage (repository root): PYTHONPATH=tmp/art-python python Tools/Art/UI/pixel_font.py
Writes RoadOfTheOldKing/Assets/UI/Fonts/RotOKPixel.ttf and the review sheet DeveloperNotes/Previews/PixelFont.png.

Grid: capitals and digits are 5x7, lowercase has a 5-row x-height and 2-row descenders, punctuation is
narrow where it reads better (proportional advance = glyph width + 1). One font pixel is 1/10 em, so font
sizes 10, 20, 30... keep every glyph on whole pixels. Rows are listed top (cap height) to bottom; row 6 sits on
the baseline and rows 7-8 are descenders. Pillow and fontTools come from tmp/art-python locally.
"""
import os
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "RoadOfTheOldKing", "Assets", "UI", "Fonts", "RotOKPixel.ttf")
PREVIEW = os.path.join(ROOT, "DeveloperNotes", "Previews", "PixelFont.png")

PX = 128              # font units per pixel
EM = 10 * PX          # font-size N => N/10 screen px per font pixel
ASCENT, DESCENT = 8 * PX, 2 * PX

def g(*rows):
    return list(rows)

GLYPHS = {
    # ---- capitals (5x7) ----
    "A": g(".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"),
    "B": g("####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."),
    "C": g(".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."),
    "D": g("####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."),
    "E": g("#####", "#....", "#....", "####.", "#....", "#....", "#####"),
    "F": g("#####", "#....", "#....", "####.", "#....", "#....", "#...."),
    "G": g(".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####"),
    "H": g("#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"),
    "I": g("###", ".#.", ".#.", ".#.", ".#.", ".#.", "###"),
    "J": g("..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.."),
    "K": g("#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"),
    "L": g("#....", "#....", "#....", "#....", "#....", "#....", "#####"),
    "M": g("#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"),
    "N": g("#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#"),
    "O": g(".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."),
    "P": g("####.", "#...#", "#...#", "####.", "#....", "#....", "#...."),
    "Q": g(".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"),
    "R": g("####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"),
    "S": g(".####", "#....", "#....", ".###.", "....#", "....#", "####."),
    "T": g("#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."),
    "U": g("#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."),
    "V": g("#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."),
    "W": g("#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#."),
    "X": g("#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"),
    "Y": g("#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."),
    "Z": g("#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"),
    # ---- lowercase (x-height 5, descenders 2) ----
    "a": g(".....", ".....", ".###.", "....#", ".####", "#...#", ".####"),
    "b": g("#....", "#....", "####.", "#...#", "#...#", "#...#", "####."),
    "c": g("....", "....", ".###", "#...", "#...", "#...", ".###"),
    "d": g("....#", "....#", ".####", "#...#", "#...#", "#...#", ".####"),
    "e": g(".....", ".....", ".###.", "#...#", "#####", "#....", ".###."),
    "f": g("..##", ".#..", "####", ".#..", ".#..", ".#..", ".#.."),
    "g": g(".....", ".....", ".####", "#...#", "#...#", "#...#", ".####", "....#", ".###."),
    "h": g("#....", "#....", "####.", "#...#", "#...#", "#...#", "#...#"),
    "i": g("#", ".", "#", "#", "#", "#", "#"),
    "j": g("..#", "...", "..#", "..#", "..#", "..#", "..#", "#.#", ".#."),
    "k": g("#...", "#...", "#..#", "#.#.", "##..", "#.#.", "#..#"),
    "l": g("#.", "#.", "#.", "#.", "#.", "#.", ".#"),
    "m": g(".....", ".....", "##.#.", "#.#.#", "#.#.#", "#.#.#", "#...#"),
    "n": g(".....", ".....", "####.", "#...#", "#...#", "#...#", "#...#"),
    "o": g(".....", ".....", ".###.", "#...#", "#...#", "#...#", ".###."),
    "p": g(".....", ".....", "####.", "#...#", "#...#", "#...#", "####.", "#....", "#...."),
    "q": g(".....", ".....", ".####", "#...#", "#...#", "#...#", ".####", "....#", "....#"),
    "r": g("....", "....", "#.##", "##..", "#...", "#...", "#..."),
    "s": g(".....", ".....", ".####", "#....", ".###.", "....#", "####."),
    "t": g(".#..", ".#..", "####", ".#..", ".#..", ".#..", "..##"),
    "u": g(".....", ".....", "#...#", "#...#", "#...#", "#...#", ".####"),
    "v": g(".....", ".....", "#...#", "#...#", "#...#", ".#.#.", "..#.."),
    "w": g(".....", ".....", "#...#", "#...#", "#.#.#", "#.#.#", ".#.#."),
    "x": g(".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#"),
    "y": g(".....", ".....", "#...#", "#...#", "#...#", "#...#", ".####", "....#", ".###."),
    "z": g(".....", ".....", "#####", "...#.", "..#..", ".#...", "#####"),
    # ---- digits (5x7; 1 is narrow) ----
    "0": g(".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."),
    "1": g(".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###"),
    "2": g(".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"),
    "3": g("####.", "....#", "....#", ".###.", "....#", "....#", "####."),
    "4": g("...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."),
    "5": g("#####", "#....", "####.", "....#", "....#", "#...#", ".###."),
    "6": g(".###.", "#....", "#....", "####.", "#...#", "#...#", ".###."),
    "7": g("#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."),
    "8": g(".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."),
    "9": g(".###.", "#...#", "#...#", ".####", "....#", "....#", ".###."),
    # ---- punctuation and symbols ----
    ".": g(".", ".", ".", ".", ".", ".", "#"),
    ",": g("..", "..", "..", "..", "..", "..", ".#", "#."),
    ":": g(".", ".", "#", ".", ".", ".", "#"),
    ";": g("..", "..", ".#", "..", "..", "..", ".#", "#."),
    "!": g("#", "#", "#", "#", "#", ".", "#"),
    "?": g(".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."),
    "'": g("#", "#"),
    '"': g("#.#", "#.#"),
    "-": g("...", "...", "...", "###"),
    "+": g("...", "...", ".#.", "###", ".#."),
    "=": g("...", "...", "###", "...", "###"),
    "/": g("..#", "..#", ".#.", ".#.", ".#.", "#..", "#.."),
    "(": g(".#", "#.", "#.", "#.", "#.", "#.", ".#"),
    ")": g("#.", ".#", ".#", ".#", ".#", ".#", "#."),
    "[": g("##", "#.", "#.", "#.", "#.", "#.", "##"),
    "]": g("##", ".#", ".#", ".#", ".#", ".#", "##"),
    "<": g("...", "..#", ".#.", "#..", ".#.", "..#"),
    ">": g("...", "#..", ".#.", "..#", ".#.", "#.."),
    "_": g("....", "....", "....", "....", "....", "....", "....", "####"),
    "%": g("##..#", "##.#.", "...#.", "..#..", ".#...", ".#.##", "#..##"),
    "&": g(".##..", "#..#.", ".##..", ".#...", "#.#.#", "#..#.", ".##.#"),
    "#": g(".#.#.", "#####", ".#.#.", ".#.#.", "#####", ".#.#."),
    "*": g("...", "#.#", ".#.", "#.#"),
    "·": g(".", ".", ".", "#"),
    "…": g(".....", ".....", ".....", ".....", ".....", ".....", "#.#.#"),
    "×": g("...", "...", "#.#", ".#.", "#.#"),
    "↑": g("..#..", ".###.", "#.#.#", "..#..", "..#..", "..#.."),
    "↓": g("..#..", "..#..", "..#..", "#.#.#", ".###.", "..#.."),
    "←": g(".....", "..#..", ".#...", "#####", ".#...", "..#.."),
    "→": g(".....", "..#..", "...#.", "#####", "...#.", "..#.."),
}
# Lookalikes that should render rather than fall back.
ALIASES = {"’": "'", "‘": "'", "“": '"', "”": '"', "–": "-", "—": "-", "•": "·"}
SPACE_ADVANCE = 3

def glyph_name(ch):
    return "uni%04X" % ord(ch)

def build():
    order = [".notdef", "space"] + [glyph_name(c) for c in GLYPHS]
    cmap = {32: "space"}
    glyf, metrics = {}, {}

    def make(rows):
        pen = TTGlyphPen(None)
        width = max(len(r) for r in rows) if rows else 0
        for r, row in enumerate(rows):
            y0 = (6 - r) * PX
            x = 0
            while x < len(row):
                if row[x] != "#":
                    x += 1
                    continue
                start = x
                while x < len(row) and row[x] == "#":
                    x += 1
                # one rectangle per horizontal run, clockwise (TrueType outer contour)
                pen.moveTo((start * PX, y0)); pen.lineTo((start * PX, y0 + PX))
                pen.lineTo((x * PX, y0 + PX)); pen.lineTo((x * PX, y0)); pen.closePath()
        return pen.glyph(), (width + 1) * PX

    notdef = ["#####", "#...#", "#...#", "#...#", "#...#", "#...#", "#####"]
    glyf[".notdef"], adv = make(notdef); metrics[".notdef"] = (adv, 0)
    glyf["space"] = TTGlyphPen(None).glyph(); metrics["space"] = (SPACE_ADVANCE * PX, 0)
    for ch, rows in GLYPHS.items():
        name = glyph_name(ch)
        glyf[name], adv = make(rows)
        metrics[name] = (adv, 0)
        cmap[ord(ch)] = name
    for src, dst in ALIASES.items():
        cmap[ord(src)] = glyph_name(dst)

    fb = FontBuilder(EM, isTTF=True)
    fb.setupGlyphOrder(order)
    fb.setupCharacterMap(cmap)
    fb.setupGlyf(glyf)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=ASCENT, descent=-DESCENT)
    fb.setupNameTable({"familyName": "RotOK Pixel", "styleName": "Regular"})
    fb.setupOS2(sTypoAscender=ASCENT, sTypoDescender=-DESCENT, sTypoLineGap=0,
                usWinAscent=ASCENT, usWinDescent=DESCENT, sxHeight=5 * PX, sCapHeight=7 * PX)
    fb.setupPost()
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    fb.save(OUT)

def preview():
    from PIL import Image, ImageDraw, ImageFont
    lines = [
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "abcdefghijklmnopqrstuvwxyz",
        "0123456789 .,:;!?'\"-+=/()[]<>_%&#*·…×↑↓←→",
        "[F] Rest   [E] Recall   [Q] Drink",
        "Sun Shard +1 · 3 shards",
        "Roadside Bonfire",
        "Rest   Upgrades   Travel   New run   Leave",
        "A wolf hunts the ruins. Dodge its lunge.",
    ]
    font = ImageFont.truetype(OUT, 10)
    w, h = 260, 12 * len(lines) + 4
    img = Image.new("RGB", (w, h), (37, 40, 35))
    d = ImageDraw.Draw(img); d.fontmode = "1"
    for i, line in enumerate(lines):
        d.text((3, 2 + i * 12), line, font=font, fill=(242, 229, 193))
    img = img.resize((w * 4, h * 4), Image.NEAREST)
    os.makedirs(os.path.dirname(PREVIEW), exist_ok=True)
    img.save(PREVIEW)

if __name__ == "__main__":
    build()
    preview()
    print("wrote", OUT, "and", PREVIEW)
