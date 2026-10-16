"""Shared source palette and export guard for new Lowlands environment art."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[3]
SPEC = json.loads((ROOT / 'ArtSource/GreenLowlands/ZonePalette.json').read_text(encoding='utf-8'))
COLORS = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in SPEC['colors'].items()}
ACCENTS = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in SPEC['rewardAccents'].items()}
assert len(set(COLORS.values())) == SPEC['environmentColorLimit'] == 32

def ramp(*names):
    return [COLORS[n] for n in names]

def validate(image, rewards=False):
    pixels = image.get_flattened_data() if hasattr(image, 'get_flattened_data') else image.getdata()
    used = {p for p in pixels if p[3]}
    allowed = set(COLORS.values()) | (set(ACCENTS.values()) if rewards else set())
    assert used <= allowed, 'Colors outside the Lowlands palette: ' + repr(used - allowed)
    return len(used)

def preview():
    from PIL import Image, ImageDraw, ImageFont
    image = Image.new('RGB', (1120, 690), '#F1EEE4'); draw = ImageDraw.Draw(image)
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 18)
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 26)
    draw.text((28, 18), 'GREEN LOWLANDS  /  32 ENVIRONMENT COLORS', fill='#323F3E', font=title)
    draw.text((28, 56), 'Sage foliage · warm earth · chalk stone · quiet blue water', fill='#53635A', font=font)
    for row, (label, names) in enumerate(SPEC['ramps'].items()):
        y = 104 + row * 66
        draw.text((28, y + 11), label, fill='#323F3E', font=font)
        for col, name in enumerate(names):
            x = 288 + col * 130
            draw.rectangle((x, y, x+114, y+33), fill=COLORS[name][:3])
            draw.text((x, y+36), '#'+SPEC['colors'][name], fill='#323F3E', font=font)
    draw.text((28, 646), 'Per piece: ground 3–5 · bushes 5 · trees 5–8 · buildings 8–12. Lighting comes later.', fill='#53635A', font=font)
    image.save(ROOT / 'Docs/Art/GreenLowlands/zone-palette.png')
    lines=['GIMP Palette','Name: Green Lowlands','Columns: 8','# 32 environment colors']
    lines += [f'{r:3} {g:3} {b:3} {name}' for name,(r,g,b,a) in COLORS.items()]
    (ROOT/'ArtSource/GreenLowlands/ZonePalette.gpl').write_text('\n'.join(lines)+'\n')

if __name__ == '__main__':
    preview()
