"""Road of the Old King UI kit: shared pixel-art frames and a mockup sheet of real screens.

Usage (repository root): PYTHONPATH=tmp/art-python python Tools/Art/UI/ui_kit.py [--out DIR] [--assets]
Writes kit sprites (kit/*.png) and mockups (mockup-*.png) to DeveloperNotes/Previews/UIKit by default.

Contract (user, 17 Oct): every screen UI element uses one pixel size, 2 reference px per UI pixel on the
960x540 reference canvas (4 screen px at 1080p), so the UI is drawn on a 480x270 grid. Text is RotOK Pixel at
font size 20 (one font pixel = one UI pixel); display text (banners, defeat, logo) is font size 40. Spatial UI
(world prompts) uses the same art at the same UI pixel size (user, 17 Oct). Colors come from the shared zone palette
(ArtSource/Shared/Palettes/Meadow.json) plus the existing sun-gold accent. Light comes from the upper left.
"""
import argparse
import json
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
ASSETS = os.path.join(ROOT, "RoadOfTheOldKing", "Assets")
FONT = os.path.join(ASSETS, "UI", "Fonts", "RotOKPixel.ttf")
UI = 4          # screen px per UI pixel at 1920x1080


def hexrgb(value, alpha=255):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4)) + (alpha,)


palette = {k: v for k, v in json.load(open(os.path.join(ROOT, "ArtSource", "Shared", "Palettes", "Meadow.json"), encoding="utf-8"))["colors"].items()}
C = {
    "ink": hexrgb(palette["ink"]),
    "fill": hexrgb(palette["rockshadow"]),      # panel body
    "inset": hexrgb(palette["slatedark"]),      # descriptions, slots, selected rows
    "trimDark": hexrgb(palette["soil"]),        # bronze trim, lower/right
    "trim": hexrgb(palette["soilmid"]),
    "trimLight": hexrgb(palette["soillight"]),  # bronze trim, upper/left
    "text": hexrgb(palette["stonelight"]),
    "muted": hexrgb(palette["stone"]),
    "disabled": hexrgb(palette["stoneshade"]),
    "gold": hexrgb("E7B85C"),                   # sun gold: selection, headings, key names
    "red": hexrgb("CA6E57"),
    "timberDark": hexrgb(palette["deepsoil"]),
    "timber": hexrgb(palette["soil"]),
    "timberLight": hexrgb(palette["soilmid"]),
    "rope": hexrgb(palette["sand"]),
    "ropeDark": hexrgb(palette["pathshade"]),
}
CLEAR = (0, 0, 0, 0)


def notched(w, h, notch):
    """Inside mask of a rectangle whose corners lose a stepped triangle of `notch` pixels."""
    def inside(x, y):
        dx = min(x, w - 1 - x)
        dy = min(y, h - 1 - y)
        return dx + dy >= notch
    return inside


def ring_depth(w, h, notch):
    """Pixels' distance inward from the outline (0 = outline), following the notched shape."""
    inside = notched(w, h, notch)
    depth = [[-1] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if not inside(x, y):
                continue
            d = 0
            while True:
                ok = all(0 <= x + ox < w and 0 <= y + oy < h and inside(x + ox, y + oy)
                         for ox, oy in ((-d - 1, 0), (d + 1, 0), (0, -d - 1), (0, d + 1)))
                if not ok:
                    break
                d += 1
            depth[y][x] = d
    return depth


def frame(w, h, notch, layers, rivets=False):
    """layers[d] gives the color for depth d; a callable gets (x, y, w, h) for lit/shaded bevels."""
    img = Image.new("RGBA", (w, h), CLEAR)
    depth = ring_depth(w, h, notch)
    for y in range(h):
        for x in range(w):
            d = depth[y][x]
            if d < 0:
                continue
            layer = layers[min(d, len(layers) - 1)]
            img.putpixel((x, y), layer(x, y, w, h) if callable(layer) else layer)
    if rivets:
        for x, y in ((3, 3), (w - 4, 3), (3, h - 4), (w - 4, h - 4)):
            img.putpixel((x, y), C["trimLight"])
    return img


def bevel(light, mid, dark):
    def color(x, y, w, h):
        top_left = x + y
        bottom_right = (w - 1 - x) + (h - 1 - y)
        if top_left < bottom_right - 2:
            return light
        if bottom_right < top_left - 2:
            return dark
        return mid
    return color


def build_kit():
    kit = {}
    trim = bevel(C["trimLight"], C["trim"], C["trimDark"])
    kit["Panel"] = frame(16, 16, 2, [C["ink"], trim, C["ink"], C["fill"]], rivets=True)       # slice 6
    kit["Inset"] = frame(8, 8, 1, [C["ink"], C["inset"]])                                      # slice 2
    kit["Selected"] = frame(8, 8, 1, [C["trim"], C["inset"]])                                  # slice 2
    kit["Chip"] = frame(8, 8, 1, [C["ink"], C["fill"]])                                        # slice 2
    kit["ChipEdge"] = kit["Chip"].copy()
    for x in range(2, 6):
        kit["ChipEdge"].putpixel((x, 6), C["trimDark"])                                      # bronze ledge
    kit["HearthPanel"] = hearth_panel()                                                        # slice 12
    cursor = Image.new("RGBA", (5, 7), CLEAR)
    for y in range(7):
        for x in range(5):
            reach = 3 - abs(y - 3)
            if x <= reach + 1:
                edge = x == reach + 1 or x == 0 or y in (0, 6)
                cursor.putpixel((x, y), C["ink"] if edge else C["gold"])
    kit["Cursor"] = cursor
    gem = Image.new("RGBA", (5, 5), CLEAR)
    for y in range(5):
        for x in range(5):
            d = abs(x - 2) + abs(y - 2)
            if d <= 2:
                gem.putpixel((x, y), C["ink"] if d == 2 else C["gold"])
    kit["DividerGem"] = gem
    kit["HearthCrest"] = hearth_crest()
    return kit


def hearth_panel():
    """Lowlands bonfire skin: a frame of squared oak beams lashed with rope at each corner.

    32x32, 9-slice border 12. Beam = 4 px (lit top/left face, two body rows with sparse grain, shaded edge)."""
    size, beam = 32, 4
    img = Image.new("RGBA", (size, size), CLEAR)
    for y in range(size):
        for x in range(size):
            sides = {"top": y, "left": x, "right": size - 1 - x, "bottom": size - 1 - y}
            side = min(sides, key=sides.get)
            edge = sides[side]
            if edge == 0:
                if (x in (0, size - 1)) and (y in (0, size - 1)):
                    continue
                img.putpixel((x, y), C["ink"])
            elif edge <= beam:
                if edge == 1:
                    color = C["timberLight"] if side in ("top", "left") else C["timber"]
                elif edge == beam:
                    color = C["timberDark"]
                else:
                    along = x if side in ("top", "bottom") else y
                    color = C["timberDark"] if (along * 7 + edge * 3) % 11 == 0 else C["timber"]
                img.putpixel((x, y), color)
            elif edge == beam + 1:
                img.putpixel((x, y), C["ink"])
            else:
                img.putpixel((x, y), C["fill"])
    # Rope lashing: two crossing hemp bands over each corner joint, with an ink rim so they sit on the beams.
    lash = [
        "..##....##..",
        ".#oo#..#oo#.",
        "#oo-o##o-oo#",
        "#o-oo--oo-o#",
        ".#o-oooo-o#.",
        "..#o-oo-o#..",
        "..#o-oo-o#..",
        ".#o-oooo-o#.",
        "#o-oo--oo-o#",
        "#oo-o##o-oo#",
        ".#oo#..#oo#.",
        "..##....##..",
    ]
    for ox, oy in ((0, 0), (size - 12, 0), (0, size - 12), (size - 12, size - 12)):
        for y, row in enumerate(lash):
            for x, ch in enumerate(row):
                if ch == "#":
                    img.putpixel((ox + x, oy + y), C["ink"])
                elif ch == "o":
                    img.putpixel((ox + x, oy + y), C["rope"])
                elif ch == "-":
                    img.putpixel((ox + x, oy + y), C["ropeDark"])
    return img


def hearth_crest():
    """Flame medallion that sits on the hearth frame's top beam."""
    size = 21
    img = Image.new("RGBA", (size, size), CLEAR)
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            r = ((x - c) ** 2 + (y - c) ** 2) ** 0.5
            if r <= c + .3:
                img.putpixel((x, y), C["ink"] if r > c - .8 else C["timberLight"] if r > c - 1.8 else
                             C["timber"] if r > c - 2.8 else C["ink"] if r > c - 3.6 else C["inset"])
    flame = [
        "....r....",
        "...rr....",
        "...rrr...",
        "..rrrr.r.",
        "..rrgrrr.",
        ".rrggrrr.",
        ".rrgggrr.",
        ".rggpggr.",
        "..rgppgr.",
        "...rggr..",
        "....rr...",
    ]
    colors = {"r": C["red"], "g": C["gold"], "p": hexrgb("FFF0B3")}
    for y, row in enumerate(flame):
        for x, ch in enumerate(row):
            if ch in colors:
                img.putpixel((6 + x, 5 + y), colors[ch])
    return img


def nine(img, w, h, s):
    """Stretch a 9-slice sprite with border s to w x h (edges stretch; pixel art stays crisp)."""
    out = Image.new("RGBA", (w, h), CLEAR)
    sw, sh = img.size
    xs = [(0, s, 0, s), (s, sw - s, s, w - s), (sw - s, sw, w - s, w)]
    ys = [(0, s, 0, s), (s, sh - s, s, h - s), (sh - s, sh, h - s, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 <= dx0 or dy1 <= dy0:
                continue
            part = img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST)
            out.alpha_composite(part, (dx0, dy0))
    return out


class Canvas:
    """A layer on the UI pixel grid (480x270 at 1 UI px per pixel)."""

    def __init__(self, w=480, h=270):
        self.img = Image.new("RGBA", (w, h), CLEAR)
        self.draw = ImageDraw.Draw(self.img)
        self.draw.fontmode = "1"
        self.font = ImageFont.truetype(FONT, 10)          # font size 10 at 1 px = one font pixel per pixel
        self.display = ImageFont.truetype(FONT, 20)       # display text: two pixels per font pixel

    def width(self, text, display=False):
        return int(self.draw.textlength(text, font=self.display if display else self.font))

    def text(self, x, y, text, color, display=False, center=False):
        if center:
            x -= self.width(text, display) // 2
        # Baseline: RotOK Pixel's cap height is 7 font px with an 8 px ascent; nudge so caps start at y.
        self.draw.text((x, y - (2 if display else 1)), text, font=self.display if display else self.font, fill=color)

    def paste(self, sprite, x, y):
        self.img.alpha_composite(sprite, (int(x), int(y)))

    def veil(self, rgba):
        layer = Image.new("RGBA", self.img.size, rgba)
        self.img.alpha_composite(layer)


def composite(background, ui_layer, world_layers=()):
    out = background.convert("RGBA").copy()
    for layer, scale, pos in world_layers:
        big = layer.resize((layer.width * scale, layer.height * scale), Image.NEAREST)
        out.alpha_composite(big, pos)
    big = ui_layer.resize((ui_layer.width * UI, ui_layer.height * UI), Image.NEAREST)
    out.alpha_composite(big)
    return out.convert("RGB")


def icon(path, rect=None):
    img = Image.open(path).convert("RGBA")
    if rect:
        x, y, w, h = rect
        img = img.crop((x, img.height - y - h, x + w, img.height - y))
        img = img.crop(img.getbbox())
    return img


def menu_list(c, kit, x, y, w, rows, selected, disabled=()):
    """Rows of 14 UI px; the selected one sits in the bronze Selected bar with the cursor beside it."""
    for i, label in enumerate(rows):
        ry = y + i * 16
        if i == selected:
            c.paste(nine(kit["Selected"], w, 14, 2), x, ry)
            c.paste(kit["Cursor"], x - 8, ry + 4)
        color = C["disabled"] if i in disabled else C["gold"] if i == selected else C["text"]
        c.text(x + w // 2, ry + 4, label, color, center=True)
    return y + len(rows) * 16


def heading(c, kit, cx, y, title, inner_w):
    c.text(cx, y, title.upper(), C["gold"], center=True)
    line_y = y + 12
    c.draw.line((cx - inner_w // 2, line_y, cx + inner_w // 2 - 1, line_y), fill=C["trimDark"])
    c.draw.line((cx - inner_w // 2, line_y + 1, cx + inner_w // 2 - 1, line_y + 1), fill=C["ink"])
    c.paste(kit["DividerGem"], cx - 2, line_y - 2)
    return line_y + 8


def vitals(c, health=0.78):
    heart = icon(os.path.join(ASSETS, "Art", "Sprites", "UI", "HUD", "Heart.png"))
    flask = icon(os.path.join(ASSETS, "Art", "Sprites", "UI", "HUD", "Flask.png"))
    c.paste(heart, 6, 6)
    bar = nine(icon(os.path.join(ASSETS, "Art", "Sprites", "UI", "HUD", "BarFrame.png")), 70, 8, 2)
    c.paste(bar, 17, 6)
    c.draw.rectangle((19, 8, 19 + int(65 * health), 11), fill=hexrgb("B24840"))
    c.draw.line((19, 8, 19 + int(65 * health), 8), fill=hexrgb("D67060"))
    c.draw.line((19, 11, 19 + int(65 * health), 11), fill=hexrgb("7E2B2B"))
    for i in range(3):
        c.paste(flask, 6 + i * 11, 17)


def world_prompt(kit, key, verb):
    """The spatial interaction prompt. Drawn on the UI pixel grid and shown at UI size (user, 17 Oct)."""
    world = Canvas(200, 20)
    label = [("[" + key + "]", C["gold"]), (" " + verb, C["text"])]
    pw = sum(world.width(t) for t, _ in label) + 8
    world.paste(nine(kit["Chip"], pw, 12, 2), 0, 0)
    x = 4
    for t, color in label:
        world.text(x, 3, t, color)
        x += world.width(t)
    return world.img.crop((0, 0, pw, 12))


FIRE_PROMPT = (860, 452)   # screen anchor above the fire ring in the gameplay captures


def screen_gameplay_lit(kit, bg):
    c = Canvas()
    vitals(c)
    band_y = 48            # upper third: the hero and the fire stay clear at screen centre
    c.draw.rectangle((0, band_y, 479, band_y + 31), fill=C["ink"][:3] + (200,))
    c.draw.line((0, band_y, 479, band_y), fill=C["trim"])
    c.draw.line((0, band_y + 31, 479, band_y + 31), fill=C["trim"])
    c.text(240, band_y + 9, "BONFIRE LIT", C["gold"], display=True, center=True)
    prompt = world_prompt(kit, "F", "Rest")
    return composite(bg, c.img, [(prompt, UI, (FIRE_PROMPT[0] - prompt.width * UI // 2, FIRE_PROMPT[1] - prompt.height * UI))])


def screen_gameplay(kit, bg):
    c = Canvas()
    vitals(c)
    # Teaching hint chip, lower centre
    hint = "Hold [Alt] to look around"
    w = c.width(hint) + 16
    c.paste(nine(kit["ChipEdge"], w, 16, 2), 240 - w // 2, 244)
    x = 240 - w // 2 + 8
    for part, color in (("Hold ", C["text"]), ("[Alt]", C["gold"]), (" to look around", C["text"])):
        c.text(x, 249, part, color)
        x += c.width(part)
    # Receipt chip, lower left
    c.paste(nine(kit["Chip"], c.width("Sun Shard +1 · 2") + 12, 14, 2), 6, 246)
    c.text(12, 250, "Sun Shard +1 · 2", C["text"])
    prompt = world_prompt(kit, "F", "Light Bonfire")
    return composite(bg, c.img, [(prompt, UI, (FIRE_PROMPT[0] - prompt.width * UI // 2, FIRE_PROMPT[1] - prompt.height * UI))])


def screen_pause(kit, bg):
    c = Canvas()
    vitals(c)
    c.veil((15, 19, 18, 166))
    w, h = 168, 136
    x0, y0 = 240 - w // 2, 135 - h // 2
    c.paste(nine(kit["Panel"], w, h, 6), x0, y0)
    y = heading(c, kit, 240, y0 + 12, "Paused", w - 28)
    y = menu_list(c, kit, x0 + 24, y, w - 48, ["Resume", "Settings", "Main Menu", "Quit"], 0)
    c.text(240, y + 4, "Back to the road.", C["muted"], center=True)
    c.text(240, y0 + h - 16, "Esc Resume", C["muted"], center=True)
    return composite(bg, c.img)


def screen_bonfire(kit, bg):
    c = Canvas()
    vitals(c, 1.0)
    c.veil((15, 19, 18, 50))
    w, h = 192, 164
    x0, y0 = 480 - 34 - w, 135 - h // 2
    c.paste(nine(kit["HearthPanel"], w, h, 12), x0, y0)
    c.paste(kit["HearthCrest"], x0 + w // 2 - 10, y0 - 9)
    cx = x0 + w // 2
    y = heading(c, kit, cx, y0 + 20, "Lowlands Bonfire", w - 44)
    c.text(cx, y, "Rested. Saved.", C["text"], center=True)
    c.text(cx, y + 12, "Sun Shards 2", C["gold"], center=True)
    y = menu_list(c, kit, x0 + 32, y + 28, w - 64, ["Upgrades", "Roadside Bonfire", "Leave"], 0)
    c.text(cx, y + 4, "View the next choices.", C["muted"], center=True)
    c.text(cx, y0 + h - 22, "Esc Back · F Leave", C["muted"], center=True)
    return composite(bg, c.img)


def screen_inventory(kit, bg):
    c = Canvas()
    vitals(c)
    c.veil((15, 19, 18, 122))
    w, h = 220, 126
    x0, y0 = 240 - w // 2, 135 - h // 2
    c.paste(nine(kit["Panel"], w, h, 6), x0, y0)
    y = heading(c, kit, 240, y0 + 12, "Inventory", w - 28)
    manifest = json.load(open(os.path.join(ASSETS, "Art", "Sprites", "World", "Shared", "Interactables", "InteractableManifest.json"), encoding="utf-8"))
    rects = {e["name"]: (e["x"], e["y"], e["width"], e["height"]) for e in manifest["entries"]}
    sheet = os.path.join(ASSETS, "Art", "Sprites", "World", "Shared", "Interactables", "Interactables16.png")
    wallet = [("BronzeCoin", "125"), ("SunShard", "12"), ("HeartFragment", "0/3")]
    for i, (name, count) in enumerate(wallet):
        ix = x0 + 22 + i * 64
        art = icon(sheet, rects.get(name)) if name in rects else None
        if art:
            c.paste(art, ix, y + 6 - art.height // 2 + 4)
        c.text(ix + 16, y + 6, count, C["text"])
    y += 22
    slots = [os.path.join(ASSETS, "Art", "Sprites", "UI", "HUD", "Flask.png"), (sheet, rects.get("Machine_Crank")), (sheet, rects.get("RopeAnchor")), None, None]
    for i, art in enumerate(slots):
        sx = x0 + 22 + i * 36
        c.paste(nine(kit["Selected" if i == 0 else "Inset"], 30, 30, 2), sx, y)
        if art:
            img = icon(art) if isinstance(art, str) else icon(*art)
            img = img.resize((img.width, img.height), Image.NEAREST)
            c.paste(img, sx + 15 - img.width // 2, y + 15 - img.height // 2)
        if i == 0:
            c.text(sx + 18, y + 20, "3", C["text"])
    c.text(240, y + 38, "Flasks", C["gold"], center=True)
    c.text(x0 + 16, y0 + h - 18, "Tab / Esc", C["muted"])
    close_w = c.width("Close") + 14
    c.paste(nine(kit["Chip"], close_w, 14, 2), x0 + w - 16 - close_w, y0 + h - 22)
    c.text(x0 + w - 16 - close_w // 2, y0 + h - 18, "Close", C["text"], center=True)
    return composite(bg, c.img)


def kit_sheet(kit):
    c = Canvas(240, 120)
    c.img.paste(Image.new("RGBA", (240, 120), hexrgb(palette["grass"])), (0, 0))
    c.text(8, 6, "ARDAN UI KIT", C["gold"])
    c.paste(nine(kit["Panel"], 64, 40, 6), 8, 18)
    c.text(40, 34, "Panel", C["text"], center=True)
    c.paste(nine(kit["HearthPanel"], 64, 40, 12), 80, 18)
    c.paste(kit["HearthCrest"], 80 + 22, 10)
    c.text(112, 36, "Hearth", C["text"], center=True)
    c.paste(nine(kit["Inset"], 40, 18, 2), 152, 18); c.text(172, 24, "Inset", C["text"], center=True)
    c.paste(nine(kit["Selected"], 56, 14, 2), 162, 40); c.paste(kit["Cursor"], 154, 44); c.text(190, 44, "Selected", C["gold"], center=True)
    c.paste(nine(kit["Chip"], 40, 14, 2), 8, 66); c.text(28, 70, "Chip", C["text"], center=True)
    c.paste(nine(kit["ChipEdge"], 48, 14, 2), 56, 66); c.text(80, 70, "Hint", C["text"], center=True)
    c.text(112, 70, "Disabled", C["disabled"])
    for i, key in enumerate(["ink", "fill", "inset", "trimDark", "trim", "trimLight", "text", "muted", "disabled", "gold", "red"]):
        c.draw.rectangle((8 + i * 20, 92, 8 + i * 20 + 15, 107), fill=C[key], outline=C["ink"])
    big = c.img.resize((c.img.width * UI, c.img.height * UI), Image.NEAREST)
    return big.convert("RGB")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=os.path.join(ROOT, "DeveloperNotes", "Previews", "UIKit"))
    parser.add_argument("--assets", action="store_true",
                        help="also write the kit sprites to Assets/Art/Sprites/UI/Kit (import: 50 PPU, Point, uncompressed)")
    args = parser.parse_args()
    os.makedirs(os.path.join(args.out, "kit"), exist_ok=True)
    kit = build_kit()
    for name, img in kit.items():
        img.save(os.path.join(args.out, "kit", name + ".png"))
    if args.assets:
        target = os.path.join(ASSETS, "Art", "Sprites", "UI", "Kit")
        os.makedirs(target, exist_ok=True)
        for name, img in kit.items():
            img.save(os.path.join(target, name + ".png"))
    gameplay = Image.open(os.path.join(args.out, "bg-gameplay.png"))
    lit = Image.open(os.path.join(args.out, "bg-gameplay-lit.png"))
    bonfire = Image.open(os.path.join(args.out, "bg-bonfire.png"))
    shots = {
        "mockup-1a-light-prompt.png": screen_gameplay(kit, gameplay),
        "mockup-1b-bonfire-lit.png": screen_gameplay_lit(kit, lit),
        "mockup-2-pause.png": screen_pause(kit, gameplay),
        "mockup-3-bonfire.png": screen_bonfire(kit, bonfire),
        "mockup-4-inventory.png": screen_inventory(kit, gameplay),
        "mockup-0-kit.png": kit_sheet(kit),
    }
    for stale in ("mockup-1-gameplay.png", "mockup-1c-prompt-at-ui-size.png"):
        if os.path.exists(os.path.join(args.out, stale)):
            os.remove(os.path.join(args.out, stale))
    for name, img in shots.items():
        img.save(os.path.join(args.out, name))
    order = ["mockup-1a-light-prompt.png", "mockup-1b-bonfire-lit.png", "mockup-2-pause.png",
             "mockup-3-bonfire.png", "mockup-4-inventory.png"]
    sheet = Image.new("RGB", (1920, 3 * 540 + 32), (12, 14, 13))
    for i, name in enumerate(order):
        thumb = shots[name].resize((960, 540), Image.NEAREST)
        sheet.paste(thumb, ((i % 2) * 960, (i // 2) * (540 + 16)))
    kit_thumb = shots["mockup-0-kit.png"].resize((960, 480), Image.NEAREST)
    sheet.paste(kit_thumb, (960, 2 * (540 + 16) + 30))
    sheet.save(os.path.join(args.out, "mockup-overview.png"))
    print("wrote", len(shots) + 1, "images and", len(kit), "kit sprites to", args.out)


if __name__ == "__main__":
    main()
