"""HUD icons: hand-placed native-pixel art for the corner vitals (heart, stamina, flask) and the bar frame.

Usage (repository root): PYTHONPATH=tmp/art-python python Tools/Art/UI/hud_icons.py
Writes Assets/Art/Sprites/UI/{Heart,Stamina,Flask,FlaskEmpty,BarFrame}.png (1 px = 1 art pixel; the
HUD draws them at 3 reference px per pixel, the world's pixel density at camera size 5.5) and a 6x review
sheet to DeveloperNotes/Previews/HudIcons.png. Masks are interiors; the charcoal outline is added around them.
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "RoadOfTheOldKing", "Assets", "Art", "Sprites", "UI")
PREVIEW = os.path.join(ROOT, "DeveloperNotes", "Previews", "HudIcons.png")

def hx(s): return tuple(int(s[i:i+2], 16) for i in (1, 3, 5)) + (255,)
P = {
    'O': hx("#282323"),  # charcoal outline (shared with the awareness "!")
    # health red (GameHud placeholder ramp)
    'r': hx("#D67060"), 'R': hx("#B24840"), 'q': hx("#7E2B2B"),
    # stamina ochre-gold (user, 2 Oct)
    'y': hx("#EBC35F"), 'Y': hx("#CF9535"), 'u': hx("#93611F"),
    # flask: red-orange draught, pale glass, cork, empty glass
    'h': hx("#F08A5D"), 'L': hx("#D9573F"), 'd': hx("#9C3A2C"),
    'g': hx("#B9C4BE"), 'K': hx("#8A5A3C"), 'e': hx("#3A3434"),
    # panels: dark fill (slightly translucent), olive bevel, sun-gold outline for the selected button
    'F': (0x25, 0x28, 0x23, 0xEB), 'H': hx("#525341"), 'G': hx("#E7B85C"), 'b': hx("#6E6038"),
}

def outlined(rows):
    """rows: interior mask (non-'.' = filled). Adds a 1 px charcoal outline (8-neighbour) around it."""
    h, w = len(rows), len(rows[0])
    grid = [['.'] * (w + 2) for _ in range(h + 2)]
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            if c != '.': grid[y + 1][x + 1] = c
    out = [r[:] for r in grid]
    for y in range(h + 2):
        for x in range(w + 2):
            if grid[y][x] != '.': continue
            if any(0 <= y + dy < h + 2 and 0 <= x + dx < w + 2 and grid[y + dy][x + dx] != '.'
                   for dy in (-1, 0, 1) for dx in (-1, 0, 1) if (dx or dy) and (dx == 0 or dy == 0)):
                out[y][x] = 'O'
    return ["".join(r) for r in out]

def shade(rows, light, base, dark):
    """Top-left light: a cell with empty space above or left gets light, below or right gets dark."""
    h, w = len(rows), len(rows[0])
    filled = lambda x, y: 0 <= x < w and 0 <= y < h and rows[y][x] != '.'
    out = []
    for y in range(h):
        line = ""
        for x in range(w):
            if rows[y][x] == '.': line += '.'; continue
            if not filled(x, y - 1) or not filled(x - 1, y): line += light
            elif not filled(x, y + 1) or not filled(x + 1, y): line += dark
            else: line += base
        out.append(line)
    return out

HEART = outlined(shade([
    ".XX.XX.",
    "XXXXXXX",
    "XXXXXXX",
    ".XXXXX.",
    "..XXX..",
    "...X...",
], 'r', 'R', 'q'))

STAMINA = outlined(shade([
    "..X..",
    ".XXX.",
    "XXXXX",
    ".XXX.",
    "..X..",
], 'y', 'Y', 'u'))

FLASK = outlined([
    "..KKK..",
    "...g...",
    "..ghg..",
    ".ghLLg.",
    "ghLLLdg",
    "gLLLLdg",
    "gLLLddg",
    ".gLddg.",
    "..ggg..",
])
FLASK_EMPTY = [row.translate(str.maketrans("hLd", "eee")) for row in FLASK]
FLASK_EMPTY[4] = FLASK_EMPTY[4][:2] + 'g' + FLASK_EMPTY[4][3:]  # one glint keeps it reading as glass

# 9-slice frame: notched corners, transparent centre (the track is a child inset by 1 px).
BAR_FRAME = [
    ".OOO.",
    "O...O",
    "O...O",
    "O...O",
    ".OOO.",
]

# 9-slice panels (2 px border): notched charcoal outline, a lighter bevel row on top, dark fill.
PANEL = [
    ".OOOOO.",
    "OHHHHHO",
    "OFFFFFO",
    "OFFFFFO",
    "OFFFFFO",
    "OFFFFFO",
    ".OOOOO.",
]
PANEL_SELECTED = [row.replace("O", "G").replace("H", "b") for row in PANEL]

ICONS = {"Heart": HEART, "Stamina": STAMINA, "Flask": FLASK, "FlaskEmpty": FLASK_EMPTY, "BarFrame": BAR_FRAME,
         "Panel": PANEL, "PanelSelected": PANEL_SELECTED,
         # Same pixels for world-space panels (imported at 50 PPU: 2 panel px per pixel = the art grid).
         "PanelWorld": PANEL}

def image(rows):
    img = Image.new("RGBA", (len(rows[0]), len(rows)), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            if c != '.': img.putpixel((x, y), P[c])
    return img

def main():
    os.makedirs(OUT, exist_ok=True)
    images = {name: image(rows) for name, rows in ICONS.items()}
    for name, img in images.items():
        img.save(os.path.join(OUT, name + ".png"))
    scale, pad = 6, 4
    width = sum(i.width for i in images.values()) + pad * (len(images) + 1)
    height = max(i.height for i in images.values()) + pad * 2
    sheet = Image.new("RGBA", (width * scale, height * scale), hx("#5E7A45"))  # grass-like backdrop
    x = pad
    for img in images.values():
        sheet.alpha_composite(img.resize((img.width * scale, img.height * scale), Image.NEAREST), (x * scale, pad * scale))
        x += img.width + pad
    os.makedirs(os.path.dirname(PREVIEW), exist_ok=True)
    sheet.save(PREVIEW)
    print("wrote", ", ".join(images), "and", PREVIEW)

if __name__ == "__main__":
    main()
