"""Slightly trim heavy external ink using the user's authorized precise pixel masks.

Run on the registered canvas, once per rebuild from unchanged sources. Interior
colors, internal lines, pose registration and the walking reference are untouched.
"""
from PIL import Image, ImageChops, ImageFilter


def contour_keep_mask(master):
    alpha = master.getchannel("A")
    opaque = alpha.point(lambda a: 255 if a >= 128 else 0)
    inner = opaque.filter(ImageFilter.MinFilter(3))
    border = ImageChops.subtract(opaque, inner)
    red, green, blue, _ = master.split()
    darkest = ImageChops.lighter(ImageChops.lighter(red, green), blue).point(lambda c: 255 if c <= 64 else 0)
    # One registered pixel of dark external ink, never a full silhouette erosion.
    trim = ImageChops.multiply(border, darkest)
    # Remove the now-disconnected dark antialias fringe outside the old solid edge.
    fringe = ImageChops.multiply(ImageChops.invert(opaque), darkest)
    trim = ImageChops.lighter(trim, fringe)
    return ImageChops.invert(trim)


def apply_contour_mask(image, mask):
    result = image.copy()
    result.putalpha(ImageChops.multiply(image.getchannel("A"), mask))
    result.paste((0, 0, 0, 0), mask=result.getchannel("A").point(lambda a: 255 if a == 0 else 0))
    return result


def normalize_layers(sheets):
    # All matched layers receive ONE mask from their combined drawing. Never erode
    # the hand/weapon occlusion cutouts separately, which would create seams.
    mask = contour_keep_mask(sheets["Master"])
    for key in sheets:
        sheets[key] = apply_contour_mask(sheets[key], mask)
    return sheets


def normalize_prop(image):
    return apply_contour_mask(image, contour_keep_mask(image))
