"""Cleanup helpers for PixelLab UI experiments (Assets/UI/Experiments/PixelLab).

Generated frames vary slightly pixel to pixel, which shows as seams once 9-sliced. `clean_frame` keeps the
corners verbatim, sets every edge row/column to its most common colour and flattens the fill; `slice9`
exports the compact corners-plus-one-pixel version used with -unity-slice-*. `split_columns` cuts a sheet
of separate pieces, and `lift_circle` removes a round crest from a frame so the frame can stretch while the
crest stays a fixed-size overlay.

Run with PYTHONPATH=tmp/art-python:  python Tools/Art/UI/pixellab_ui_cleanup.py
"""
from collections import Counter
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[3] / "RoadOfTheOldKing/Assets/UI/Experiments/PixelLab"


def _mode(values):
    return Counter(values).most_common(1)[0][0]


def border_depth(img):
    """Pixels from the left edge, along the middle row, to the first fill-coloured pixel."""
    px = img.load()
    w, h = img.size
    fill = px[w // 2, h // 2]
    return next(x for x in range(w) if px[x, h // 2] == fill)


def clean_frame(img, corner, depth, fill=None):
    """Corners kept verbatim; edges uniform along their length; interior flattened."""
    src = img.load()
    w, h = img.size
    fill = fill or _mode([src[x, y] for x in range(depth, w - depth) for y in range(depth, h - depth)])
    out = img.copy()
    px = out.load()
    for y in range(h):
        for x in range(w):
            if (x < corner or x >= w - corner) and (y < corner or y >= h - corner):
                # Corner flourishes reaching into the fill are dark outline or bright metal; any other
                # colour there is fill noise or a stain.
                r, g, b, a = src[x, y]
                inside = depth <= x < w - depth and depth <= y < h - depth
                if inside and a and not (r + g + b < 60 or r > 150):
                    px[x, y] = fill
                continue
            if y < depth or y >= h - depth:
                px[x, y] = _mode([src[t, y] for t in range(corner, w - corner)])
            elif x < depth or x >= w - depth:
                px[x, y] = _mode([src[x, t] for t in range(corner, h - corner)])
            else:
                px[x, y] = fill
    return out


def slice9(img, corner):
    """Corners plus a two-pixel stretch band: (2 * corner + 2) square."""
    w, h = img.size
    cols = list(range(corner)) + [w // 2, w // 2] + list(range(w - corner, w))
    rows = list(range(corner)) + [h // 2, h // 2] + list(range(h - corner, h))
    out = Image.new("RGBA", (len(cols), len(rows)))
    src, px = img.load(), out.load()
    for j, y in enumerate(rows):
        for i, x in enumerate(cols):
            px[i, j] = src[x, y]
    return out


def split_columns(img, top=0):
    """Pieces separated by empty columns (below `top`), each cropped to its content."""
    alpha = img.getchannel("A")
    w, h = img.size
    occupied = [any(alpha.getpixel((x, y)) for y in range(top, h)) for x in range(w)]
    pieces, start = [], None
    for x, used in enumerate(occupied + [False]):
        if used and start is None:
            start = x
        if not used and start is not None:
            sub = img.crop((start, top, x, h))
            pieces.append(sub.crop(sub.getbbox()))
            start = None
    return pieces


def lift_circle(img, cx, cy, r, patch_from_x):
    """Returns (crest, frame_without_crest). The crest is every opaque pixel within r of (cx, cy); in the
    frame those pixels are replaced from column `patch_from_x` of the same row, or cleared above the frame."""
    w, h = img.size
    src = img.load()
    crest = Image.new("RGBA", (w, h))
    frame = img.copy()
    cp, fp = crest.load(), frame.load()
    for y in range(h):
        for x in range(w):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r * r and src[x, y][3]:
                cp[x, y] = src[x, y]
                fp[x, y] = src[patch_from_x, y]
    return crest.crop(crest.getbbox()), frame


def bronze_lining(img, ember=((230, 130, 61), (188, 103, 50)), fill=(68, 58, 52, 255), width=4):
    """Replaces the hearth frame's ragged orange ember edge with a straight bevelled bronze band just inside the
    beams (user, 3 Oct: metallic bronze instead of orange): lit on the top/left, shaded on the bottom/right."""
    w, h = img.size
    src = img.load()
    mx, my = w // 2, h // 2
    left = next(x for x in range(w) if src[x, my][:3] in ember)
    right = max(x for x in range(w) if src[x, my][:3] in ember) + 1
    top = next(y for y in range(h) if src[mx, y][:3] in ember)
    bottom = max(y for y in range(h) if src[mx, y][:3] in ember) + 1
    lit = [(232, 204, 140), (196, 162, 93), (158, 126, 70), (84, 64, 40)]
    shade = [(196, 162, 93), (158, 126, 70), (118, 92, 52), (64, 48, 32)]
    out = img.copy()
    px = out.load()
    for y in range(top, bottom):
        for x in range(left, right):
            d = min(x - left, right - 1 - x, y - top, bottom - 1 - y)
            if d >= width:
                if src[x, y][:3] != fill[:3] and d < 12:
                    px[x, y] = fill  # ember spill and dark specks
                continue
            side = min((y - top, "t"), (bottom - 1 - y, "b"), (x - left, "l"), (right - 1 - x, "r"))[1]
            px[x, y] = (lit if side in "tl" else shade)[d] + (255,)
    return out


def main():
    # 02: plain bronze frame.
    d = ROOT / "02 Menu Frame"
    raw = Image.open(d / "frame-02.png").convert("RGBA")
    raw = raw.crop(raw.getbbox())
    clean = clean_frame(raw, 16, 10, fill=(48, 50, 41, 255))
    clean.save(d / "frame-02-clean.png")
    slice9(clean, 16).save(d / "frame-02-slice.png")

    # 03: buttons, normal above selected.
    d = ROOT / "03 Menu Buttons"
    sheet = Image.open(d / "buttons-03.png").convert("RGBA")
    for name, (y0, y1) in (("button", (48, 83)), ("button-selected", (109, 140))):
        btn = sheet.crop((0, y0, sheet.width, y1))
        btn = btn.crop(btn.getbbox())
        depth = border_depth(btn)
        clean = clean_frame(btn, depth + 3, depth)
        clean.save(d / f"{name}-clean.png")
        slice9(clean, depth + 3).save(d / f"{name}-slice.png")

    # 04: sun-wheel crest frame; the crest becomes a separate overlay.
    d = ROOT / "04 Sun-Wheel Crest Frame"
    raw = Image.open(d / "crest-frame-04.png").convert("RGBA")
    crest, frame = lift_circle(raw, 95.5, 44, 29.5, patch_from_x=40)
    crest.save(d / "crest.png")
    frame = frame.crop(frame.getbbox())
    # Its corner flourishes reach 20 px; the middle row is noisy, so the border depth is set by hand.
    clean = clean_frame(frame, 20, 11)
    clean.save(d / "crest-frame-04-clean.png")
    slice9(clean, 20).save(d / "crest-frame-04-slice.png")

    # 06: ornament kit: divider on top, cursor / emblem / flame below.
    d = ROOT / "06 Ornament Kit"
    sheet = Image.open(d / "ornaments-06.png").convert("RGBA")
    top = sheet.crop((0, 0, sheet.width, 90))
    top.crop(top.getbbox()).save(d / "divider.png")
    for name, piece in zip(("cursor", "emblem", "flame"), split_columns(sheet, top=90)):
        piece.save(d / f"{name}.png")
    # The divider stretches as a bar (rows 6-14) with its medallion (columns 86-105) as a centred overlay.
    divider = Image.open(d / "divider.png").convert("RGBA")
    divider.crop((86, 0, 106, divider.height)).save(d / "divider-medallion.png")
    bar = divider.crop((0, 6, divider.width, 15))
    bar_px = bar.load()
    for x in range(80, 112):
        for y in range(bar.height):
            bar_px[x, y] = bar_px[60, y]
    bar.save(d / "divider-bar.png")

    # 05: hearth timber frame, used whole (tiled 34 px slices).
    d = ROOT / "05 Hearth Timber Frame"
    raw = Image.open(d / "timber-frame-05.png").convert("RGBA")
    crop = raw.crop(raw.getbbox())
    crop.save(d / "timber-frame-05-crop.png")
    bronze_lining(crop).save(d / "timber-frame-05-bronze.png")

    # Adopted menu set (3 Oct): Hearth frame for the bonfire, Bronze frame for the title, shared ornaments.
    menus = ROOT.parents[2] / "Art/Sprites/UI/Menus"
    menus.mkdir(parents=True, exist_ok=True)
    for source, target in (
            ("05 Hearth Timber Frame/timber-frame-05-bronze.png", "HearthFrame.png"),
            ("02 Menu Frame/frame-02-slice.png", "BronzeFrame.png"),
            ("03 Menu Buttons/button-slice.png", "MenuButton.png"),
            ("03 Menu Buttons/button-selected-slice.png", "MenuButtonSelected.png"),
            ("06 Ornament Kit/cursor.png", "MenuCursor.png"),
            ("06 Ornament Kit/divider-bar.png", "DividerBar.png"),
            ("06 Ornament Kit/divider-medallion.png", "DividerMedallion.png"),
            ("06 Ornament Kit/flame.png", "HearthFlame.png"),
            ("06 Ornament Kit/emblem.png", "SunWheelEmblem.png")):
        Image.open(ROOT / source).save(menus / target)


if __name__ == "__main__":
    main()
