"""Package real Tutorial tap/held throws without rescaling gameplay pixels."""
import argparse
import csv
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--direction", choices=("South", "West"), default="South")
direction = parser.parse_args().direction
source = ROOT / f"tmp/throw-proof/native-{direction.lower()}"
dest = ROOT / "Docs/Art/Player/Previews"
rows = list(csv.reader((source / "timeline.csv").open()))
if len(rows) != 160:
    raise ValueError("Run CaptureLiveThrow.cs.txt in fresh Tutorial Play Mode first")
font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 17)
small = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 14)
frames = []
for index, mode, state, action, cel, flight in rows:
    crop = (170,270,1170,950) if direction == "West" else (530,270,1530,950)
    shot = Image.open(source / f"{int(index):03}.png").convert("RGB").crop(crop)
    frame = Image.new("RGB", (1000,740), (44,57,45)); frame.paste(shot,(0,60))
    draw = ImageDraw.Draw(frame)
    draw.text((16,10), direction.upper()+" THROW / " + ("Quick tap E" if mode == "Tap" else "Hold E, aim, release"), font=font, fill=(244,224,181))
    draw.text((16,35), f"Native Tutorial / 1x speed / {action} / {state}", font=small, fill=(206,213,193))
    frames.append(frame)
frames[0].save(dest/f"Throw-{direction}.gif", save_all=True, append_images=frames[1:], duration=20, loop=0, optimize=True)
indices = [0] + [int(next(r[0] for r in rows if r[1] == "Hold" and r[3] == "Throw" and int(r[4]) == cel)) for cel in range(8)]
labels = ["Original carry", "1 Ready", "2 Load", "3 Aim A", "4 Aim B", "5 Drive", "6 Release / complete spin", "7 Follow through", "8 Recover"]
sheet = Image.new("RGB", (1200,1590), (44,57,45)); draw = ImageDraw.Draw(sheet)
for i, (index, label) in enumerate(zip(indices, labels)):
    crop = (540,270,940,770) if direction == "West" else (620,270,1020,770)
    shot = Image.open(source/f"{index:03}.png").convert("RGB").crop(crop)
    x,y = i%3*400, i//3*530
    sheet.paste(shot,(x,y+30)); draw.text((x+12,y+7),label,font=font,fill=(244,224,181))
sheet.save(dest/f"Throw-{direction}-Cels.png")
print(f"Saved native {direction} quick/held throw GIF and original-carry/eight-cel comparison.")
