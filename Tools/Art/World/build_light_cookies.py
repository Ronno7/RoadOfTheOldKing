"""Native 16-PPU light shapes for URP 2D Sprite lights.

Pixel-art light pools: brightness falls off in a few flat steps with ordered (Bayer) dithering between them, so a
light reads as painted pixels instead of a smooth gradient. Pools are flattened vertically to lie on the ground in
the three-quarter view. White on transparent; the Light2D supplies colour and intensity.

Run with the project's Pillow environment:
    $env:PYTHONPATH='tmp/art-python'; python Tools/Art/World/build_light_cookies.py
"""
from pathlib import Path
from PIL import Image

OUT = Path(__file__).resolve().parents[3] / "RoadOfTheOldKing/Assets/Art/Lighting"
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# name: (radius in pixels, vertical squash, falloff exponent, brightness steps from the edge inwards)
POOLS = {
    "FirePool": (56, 0.78, 1.15, [0.18, 0.4, 0.68, 1.0]),
    "WindowPool": (28, 0.7, 1.0, [0.25, 0.55, 1.0]),
}


def pool(radius, squash, exponent, steps):
    size = radius * 2
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    pixels = image.load()
    levels = [0.0] + steps
    for y in range(size):
        for x in range(size):
            dx = (x + 0.5 - radius) / radius
            dy = (y + 0.5 - radius) / (radius * squash)
            d = (dx * dx + dy * dy) ** 0.5
            if d >= 1:
                continue
            # Scaled past the top step and clamped, so the brightest step is a small pool rather than one pixel.
            v = min((1 - d) ** exponent * (len(levels) - 1) * 1.25, len(levels) - 1)
            low = int(v)
            frac = v - low
            # Flat bands; only the outer quarter of each band dithers into the next, along a 4x4 ordered pattern.
            seam = (frac - 0.75) * 4
            level = levels[min(low + (1 if seam > 0 and seam * 16 > BAYER[y % 4][x % 4] + 0.5 else 0), len(levels) - 1)]
            a = round(level * 255)
            if a:
                pixels[x, y] = (255, 255, 255, a)
    return image


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, spec in POOLS.items():
        pool(*spec).save(OUT / f"{name}.png")
        print(OUT / f"{name}.png")


if __name__ == "__main__":
    main()
