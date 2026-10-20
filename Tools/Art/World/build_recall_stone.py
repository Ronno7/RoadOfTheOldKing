"""Reproduce the approved Recall stone: five native 24x48 animation states.

Usage: python Tools/Art/World/build_recall_stone.py --export
Preserves the production atlas layout and existing importer slices.
"""
import os, sys
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))

def hx(s): return tuple(int(s[i:i+2], 16) for i in (1, 3, 5)) + (255,)
P = {
    'K': hx("#171C1B"),  # shared world ink: one-pixel outer contour
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
                if (x + dx, y + dy) not in carve_set and inside(g, x + dx, y + dy) and g[y + dy][x + dx] not in 'OK':
                    halo.add((x + dx, y + dy))
    for y in range(h):
        for x in range(w):
            ch = g[y][x]
            if ch == '.': continue
            if ch == 'X':
                ch = {'dormant': 'D', 'glint': 'c', 'charging': 'c', 'awakening': 'W', 'awakened': 'C'}[state]
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
    # Outline the complete body before splitting Top/Footing. Recolor its existing
    # edge inward so its silhouette, moss detail and shadow-casting mask stay intact.
    edge = [(x, y) for y in range(len(g)) for x in range(len(g[0])) if inside(g, x, y) and
            any(not inside(g, x+dx, y+dy) for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)))]
    for x, y in edge: g[y][x] = 'K'
    return g, {(11, 16), (8, 13), (14, 19)}

STATES = ["dormant", "glint", "charging", "awakening", "awakened"]

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
        raise SystemExit("Use --export to reproduce the approved Recall stone atlas.")
