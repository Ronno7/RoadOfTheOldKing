"""Recall stone concepts: hand-placed native 16 PPU pixel art, three designs x awakening states.

Usage (repository root): python Tools/Art/World/recall_stone_concepts.py [out_dir]
Writes per-state native PNGs to out_dir (default tmp/recall-stone) and the review sheet to
DeveloperNotes/Previews/RecallStoneConcepts.png.
`--export` writes the approved design A sheet to Assets/Art/Sprites/World/RecallStone.png
(24x48 frames: dormant, glint, charging, awakening, awakened; the importer's slices are kept).
"""
import os, shutil, sys
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("--") else os.path.join(ROOT, "tmp", "recall-stone")
PREVIEW = os.path.join(ROOT, "DeveloperNotes", "Previews", "RecallStoneConcepts.png")

def hx(s): return tuple(int(s[i:i+2], 16) for i in (1, 3, 5)) + (255,)
P = {
    'O': hx("#60724D"),  # moss outline (matches existing ruins)
    'M': hx("#8B9B5E"),  # moss
    'S': hx("#585F58"),  # ruins shadow
    'B': hx("#7C8370"),  # weathered stone
    'L': hx("#A6AA8A"),  # stone light / lichen
    'H': hx("#D4CCAA"),  # sunlit rim
    'D': hx("#434944"),  # carving recess (new neutral, darker than ruins shadow)
    'G': hx("#E7B85C"),  # sun gold fleck
    'c': hx("#2A7F95"),  # Recall cyan deep  (new)
    'C': hx("#4CCBDD"),  # Recall cyan mid   (new)
    'W': hx("#CFF8FF"),  # Recall cyan core  (new)
}

def blank(w, h): return [['.'] * w for _ in range(h)]

def silhouette(spans):
    """spans: list of (left, right) per row, inclusive. Returns grid with body fill 'B'."""
    w = max(r for _, r in spans) + 1
    g = blank(w, len(spans))
    for y, (l, r) in enumerate(spans):
        for x in range(l, r + 1): g[y][x] = 'B'
    return g

def inside(g, x, y): return 0 <= y < len(g) and 0 <= x < len(g[0]) and g[y][x] != '.'

def shade(g, spans, light_frac=0.34, shadow_frac=0.74, block=3, jitter=None):
    """Upper-left light: left band light, right band shadow, stepped in chunky row blocks."""
    jitter = jitter or {}
    for y, (l, r) in enumerate(spans):
        w = r - l + 1
        j = jitter.get(y // block, 0)
        lx = l + int(w * light_frac) + j
        sx = l + int(w * shadow_frac) + j
        for x in range(l, r + 1):
            g[y][x] = 'L' if x < lx else 'S' if x >= sx else 'B'

def outline(g):
    h, w = len(g), len(g[0])
    edge = [(x, y) for y in range(h) for x in range(w) if g[y][x] != '.' and
            any(not inside(g, x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for x, y in edge: g[y][x] = 'O'

def put(g, x0, y0, art, keep='.'):
    for dy, row in enumerate(art):
        for dx, ch in enumerate(row):
            if ch != keep and inside(g, x0 + dx, y0 + dy) or ch in 'OM' and ch != keep:
                if 0 <= y0 + dy < len(g) and 0 <= x0 + dx < len(g[0]): g[y0 + dy][x0 + dx] = ch

FOOT = ["OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO",   # bottom row first
        "OMMMOMMMMMOOMMMMOMMMMMMOMMMOMMMO",
        ".OMM.MMOMMM.OMMMMM.OMMMM.MMOMM..",
        "..O...MM..O...MMO....MMO...O....",
        "......O............O..........O."]

MOUND = [5, 6, 5, 4, 3, 3, 2, 2, 3, 2, 2, 2, 3, 3, 2, 2, 2, 3, 4, 3, 3, 4, 5, 6, 5, 4, 5, 6, 5, 4, 3, 4]

def footing(g, rows=5, side=4, phase=0):
    """Hand-profiled moss mound at the base: taller tufts at the sides, moss-outlined like the ruins."""
    h, w = len(g), len(g[0])
    height = [min(rows + 1, MOUND[(x + phase) % len(MOUND)] + (1 if x < side or x >= w - side else 0)) for x in range(w)]
    mound = {(x, y) for x in range(w) for y in range(h - height[x], h) if g[y][x] != '.'}
    for x, y in mound:
        top = (x, y - 1) not in mound
        g[y][x] = 'O' if top or y == h - 1 or (x, y) in {(1, h - 3), (w - 3, h - 2)} else 'M'
    for x in range(w):  # a few lighter blades inside the mound
        y = h - 2
        if (x, y) in mound and (x, y - 1) in mound and x % 5 == 2: g[y][x] = 'L'

def carve(g, x0, y0, glyph):
    """'#' = carved line (state colour later, stored as 'X'); a light lip is added below each line."""
    marks = []
    for dy, row in enumerate(glyph):
        for dx, ch in enumerate(row):
            if ch == '#': marks.append((x0 + dx, y0 + dy))
    ms = set(marks)
    for x, y in marks:
        if (x, y + 1) not in ms and inside(g, x, y + 1) and g[y + 1][x] in 'BLS': g[y + 1][x] = 'H' if g[y + 1][x] == 'L' else 'L'
    for x, y in marks: g[y][x] = 'X'
    return marks

def render(g, state, glints=()):
    """state: dormant | glint | charging | awakening | awakened"""
    h, w = len(g), len(g[0])
    pad = 4 if state == 'awakening' else 0
    img = Image.new('RGBA', (w, h + pad), (0, 0, 0, 0))
    px = img.load()
    carve_set = {(x, y) for y in range(h) for x in range(w) if g[y][x] == 'X'}
    halo = set()
    if state == 'awakening':
        for x, y in carve_set:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if (x + dx, y + dy) not in carve_set and inside(g, x + dx, y + dy) and g[y + dy][x + dx] != 'O':
                    halo.add((x + dx, y + dy))
    for y in range(h):
        for x in range(w):
            ch = g[y][x]
            if ch == '.': continue
            if ch == 'X':
                ch = {'dormant': 'D', 'glint': 'D', 'charging': 'c', 'awakening': 'W', 'awakened': 'C'}[state]
                if state == 'glint' and (x, y) in glints: ch = 'C'
            elif (x, y) in halo: ch = 'C'
            elif state == 'awakened' and ch == 'H' and any((x, y - k) in carve_set for k in (1,)): ch = 'c'
            px[x, y + pad] = P[ch]
    if state == 'awakening':  # a few native-pixel motes rising off the stone
        top = next(y for y in range(h) if any(g[y][x] != '.' for x in range(w)))
        for mx, my, col in ((w // 2 - 3, 1, 'C'), (w // 2 + 4, 0, 'W'), (w // 2, 3, 'C'), (2, top + 10, 'C'), (w - 2, top + 16, 'W')):
            if 0 <= mx < w and 0 <= my < h + pad and px[mx, my][3] == 0: px[mx, my] = P[col]
    return img

# ---------------- A: Sun-Wheel Menhir (24 x 44) ----------------
def menhir():
    spans = [(9, 14), (7, 16), (5, 17), (4, 18), (4, 19), (3, 19)] + [(3, 19)] * 8 + [(2, 19)] * 10 + [(2, 20)] * 12 + [(1, 21)] * 4 + [(0, 22)] * 4
    g = silhouette(spans)
    shade(g, spans, jitter={2: -1, 5: 1, 9: -1, 11: 1})
    g[1][9] = g[1][10] = g[2][7] = g[2][8] = 'H'  # sunlit crown
    for y in range(3, 10): g[y][4 + (y > 4)] = 'H'
    wheel = ["...#####...",
             "..#.....#..",
             ".#...#...#.",
             "#....#....#",
             "#....#....#",
             "###########",
             "#....#....#",
             "#....#....#",
             ".#...#...#.",
             "..#.....#..",
             "...#####..."]
    carve(g, 6, 11, wheel)
    for x, y in ((14, 26), (15, 27), (15, 28), (6, 31), (7, 32), (16, 35), (17, 36)): g[y][x] = 'S'  # weathering
    outline(g)
    # moss: crown cap, a creeping patch, and a mossy footing
    put(g, 12, 1, ["MM", "MMM", ".MO"])
    put(g, 16, 18, ["OO", "MO", "MMO", "OM"])
    footing(g)
    return g, {(11, 16), (8, 13), (14, 19)}

# ---------------- B: Cup-and-Ring Boulder (30 x 30) ----------------
def boulder():
    spans = [(10, 19), (7, 22), (5, 24), (4, 25), (3, 26), (2, 27), (2, 27)] + [(1, 28)] * 14 + [(1, 29)] * 4 + [(0, 29)] * 3 + [(1, 28)]
    g = silhouette(spans)
    shade(g, spans, light_frac=0.3, shadow_frac=0.72, jitter={1: 1, 3: -1, 6: 1})
    for x in range(9, 16): g[1][x] = 'H'
    for x in range(6, 10): g[2][x] = 'H'
    rings = ["....#####....",
             "..##.....##..",
             ".#...###...#.",
             ".#..#...#..#.",
             "#..#.....#..#",
             "#..#..#..#..#",
             "#..#.....#..#",
             ".#..#...#..#.",
             ".#...###...#.",
             "..##.....##..",
             "....##.##....",
             "......#......",
             "......#......",
             "......#......"]
    carve(g, 8, 6, rings)
    carve(g, 3, 9, ["##", "##"])      # small cup marks
    carve(g, 23, 20, ["##", "##"])
    outline(g)
    put(g, 20, 2, ["MMO", "MMMO", ".OM"])
    footing(g, rows=4)
    return g, {(14, 11), (4, 10), (14, 19)}

# ---------------- C: Old Road Stela (20 x 46) ----------------
def stela():
    spans = [(3, 12), (3, 14), (3, 15), (3, 16)] + [(3, 16)] * 30 + [(2, 17)] * 6 + [(1, 18)] * 3 + [(0, 19)] * 3
    g = silhouette(spans)
    shade(g, spans, light_frac=0.36, shadow_frac=0.72, block=4, jitter={3: 1, 6: -1})
    for x in range(3, 12): g[0][x] = 'H'
    for y in range(1, 14): g[y][4] = 'H'
    glyphs = [(6, ("..###..", ".#...#.", "#..#..#", ".#...#.", "..###..")),   # the sun
              (16, ("#...#..", ".#.#.#.", "..#...#")),                        # the winding road
              (24, ("#..#..#", "#.###.#", "#######"))]                         # the old king's crown
    for y0, glyph in glyphs:
        carve(g, 6, y0, glyph)
    for x, y in ((13, 12), (14, 13), (14, 30), (13, 31), (6, 34)): g[y][x] = 'S'
    outline(g)
    put(g, 12, 1, ["MO", "MMO"])
    footing(g, side=3)
    return g, {(9, 8), (8, 17), (9, 25)}

DESIGNS = [("A  Sun-Wheel Menhir", menhir), ("B  Cup-and-Ring Boulder", boulder), ("C  Old Road Stela", stela)]
STATES = ["dormant", "glint", "charging", "awakening", "awakened"]
LABELS = ["Dormant", "Dormant glint", "Hit / charging", "Awakening peak", "Awakened"]

def main():
    os.makedirs(OUT, exist_ok=True)
    ground = Image.open(os.path.join(ROOT, "RoadOfTheOldKing/Assets/Art/Tiles/Tutorial/Environment/TutorialEnvironment16.png")).convert("RGBA")
    crop = ground.crop((216, 320, 252, 376)); pillar = crop.crop(crop.getbbox())
    hero = Image.open(os.path.join(ROOT, "ArtSource/Player/Idle/rotations/south.png")).convert("RGBA")
    paving, grass = hx("#B2AE91"), hx("#8B9B5E")
    S = 5                         # state previews at 5x
    CW, CH = 52 * S // 1, 56 * S  # cell size
    sheet_w = 20 + len(STATES) * (34 * S + 12) + 16 + 110 * 3
    sheet = Image.new("RGBA", (sheet_w, 40 + len(DESIGNS) * (54 * S + 40)), hx("#1E1E22"))
    d = ImageDraw.Draw(sheet)
    d.text((20, 12), "Recall stone concepts - native 16 PPU pixels shown at 5x (states) and 3x (in context with hero + existing broken pillar)", fill=(230, 225, 210, 255))
    y = 40
    for title, fn in DESIGNS:
        g, glints = fn()
        d.text((20, y), title + f"   ({len(g[0])} x {len(g)} px)", fill=(231, 184, 92, 255))
        x = 20
        for state, label in zip(STATES, LABELS):
            img = render(g, state, glints)
            cell = Image.new("RGBA", (34 * S, 50 * S), paving)
            big = img.resize((img.width * S, img.height * S), Image.NEAREST)
            cell.alpha_composite(big, ((cell.width - big.width) // 2, cell.height - big.height - S * 2))
            sheet.alpha_composite(cell, (x, y + 16))
            d.text((x, y + 16 + cell.height + 4), label, fill=(210, 210, 200, 255))
            img.save(os.path.join(OUT, f"RecallStone-{title[0]}-{state}.png"))
            x += 34 * S + 12
        # context: paving strip, broken pillar, stone (dormant + awakened), hero
        ctx = Image.new("RGBA", (104, 60), paving)
        for yy in range(52, 60):
            for xx in range(104): ctx.putpixel((xx, yy), grass if (xx // 16 + yy // 8) % 2 else paving)
        ctx.alpha_composite(pillar, (0, 56 - pillar.height))
        dorm, awake = render(g, "dormant"), render(g, "awakened")
        ctx.alpha_composite(dorm, (25, 56 - dorm.height))
        ctx.alpha_composite(awake, (104 - awake.width, 56 - awake.height))
        hx0 = 25 + dorm.width + (104 - awake.width - 25 - dorm.width - 18) // 2 - 7
        ctx.alpha_composite(hero, (hx0, 58 - 32))
        big = ctx.resize((ctx.width * 3, ctx.height * 3), Image.NEAREST)
        sheet.alpha_composite(big, (x + 4, y + 16 + 50 * S - big.height))
        d.text((x + 4, y + 16 + 50 * S + 4), "in context: pillar | dormant + hero | awakened", fill=(210, 210, 200, 255))
        y += 54 * S + 40
    sheet.convert("RGB").save(os.path.join(OUT, "RecallStoneConcepts.png"))
    if os.path.isdir(os.path.dirname(PREVIEW)):
        shutil.copyfile(os.path.join(OUT, "RecallStoneConcepts.png"), PREVIEW)
    used = set()
    for _, fn in DESIGNS:
        g, gl = fn()
        for st in STATES:
            im = render(g, st, gl); used |= {c for c in im.getdata() if c[3] == 255}
    print("sheet", sheet.size, "opaque colours across all states", len(used))

SHEET = os.path.join(ROOT, "RoadOfTheOldKing", "Assets", "Art", "Sprites", "World", "RecallStone.png")
FRAME_W, FRAME_H = 24, 48  # even width keeps the bottom-centre pivot on the pixel grid

def export():
    """Approved design A (29 Sep): five states left to right, bottom-aligned 24x48 frames."""
    g, glints = menhir()
    sheet = Image.new("RGBA", (FRAME_W * len(STATES), FRAME_H), (0, 0, 0, 0))
    for i, state in enumerate(STATES):
        img = render(g, state, glints)
        sheet.alpha_composite(img, (i * FRAME_W, FRAME_H - img.height))
    os.makedirs(os.path.dirname(SHEET), exist_ok=True)
    sheet.save(SHEET)
    print("exported", SHEET, sheet.size, "states:", ", ".join(STATES))

if __name__ == "__main__":
    if "--export" in sys.argv:
        export()
    else:
        main()
