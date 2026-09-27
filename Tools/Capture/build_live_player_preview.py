"""Package native Tutorial Play Mode captures without resizing sprite pixels.

Run CaptureLivePlayerLoop.cs.txt for the requested direction first. Requires Pillow.
Default retains the east/north milestone; --direction builds a south or west action/catch review.
"""
from pathlib import Path
import argparse
import csv
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "tmp/live-player-capture"
OUTPUT = ROOT / "Docs/Art/Player/Previews/Live-Action-Loop.gif"
FONT = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 18)
CROP = (620, 110, 1300, 750)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--direction", choices=("South", "West"))
args = parser.parse_args()
if args.direction:
    name = args.direction
    source = SOURCE / name.lower()
    rows = list(csv.reader((source / "timeline.csv").open()))
    if len(rows) != 110:
        raise ValueError("Capture all 110 live action frames first")
    frames = []
    for index, state, action, cel, flight in rows:
        frame = Image.new("RGB", (680,706), "#303e32")
        frame.paste(Image.open(source / f"{int(index):03}.png").convert("RGB").crop(CROP), (0,34))
        draw = ImageDraw.Draw(frame)
        draw.text((14,8),f"{name} / live Tutorial / {action} / cel {int(cel)+1 if int(cel)>=0 else '-'}",font=FONT,fill="#eddfbf")
        draw.text((14,681),"Native game zoom / actual throw and Recall arrival",font=FONT,fill="#eddfbf")
        frames.append(frame)
    frames[0].save(OUTPUT.parent/f"Live-Action-Loop-{name}.gif",save_all=True,append_images=frames[1:],duration=20,disposal=2,loop=0)
    caught = next(int(row[0]) for row in rows if row[2]=="Catch" and row[3]=="1")
    indices = ["carry"]+[f"{int(next(row[0] for row in rows if row[2]=='Catch' and int(row[3])==cel)):03}" for cel in range(4)]
    indices.insert(2,f"{caught-1:03}")
    labels = ["Original carry", "1 Reach / no possession", "Last incoming prop", "2 Confirmed grip", "3 Absorb", "4 Settle"]
    sheet = Image.new("RGB",(1200,700),"#303e32")
    for i,(index,label) in enumerate(zip(indices,labels)):
        x,y = i%3*400,i//3*350
        shot=Image.open(source/f"{index}.png").convert("RGB").crop((650,430,1050,740))
        sheet.paste(shot,(x,y+35))
        ImageDraw.Draw(sheet).text((x+10,y+10),label,font=FONT,fill="#eddfbf")
    sheet.save(OUTPUT.parent/f"Catch-{name}-Cels.png")
    print(f"Saved native {name} action loop and catch/handoff comparison.")
    raise SystemExit(0)
frames = []
for index in range(110):
    frame = Image.new("RGB", (1360, 706), "#303e32")
    for column, name in enumerate(("east", "north")):
        with Image.open(SOURCE / name / f"{index:03}.png") as raw:
            frame.paste(raw.convert("RGB").crop(CROP), (column * 680, 34))
    draw = ImageDraw.Draw(frame)
    draw.text((14, 8), "East | live Tutorial", font=FONT, fill="#eddfbf")
    draw.text((694, 8), "North | live Tutorial", font=FONT, fill="#eddfbf")
    draw.text((14, 681), "Actual weapon clocks / native game zoom / Recall enabled temporarily for capture", font=FONT, fill="#eddfbf")
    frames.append(frame)
frames[0].save(OUTPUT, save_all=True, append_images=frames[1:], duration=20, disposal=2, loop=0)
print(f"Saved {OUTPUT.relative_to(ROOT)} ({OUTPUT.stat().st_size / 1024:.0f} KiB).")

# One at-scale comparison, from the same camera density throughout.
review = Image.new("RGB", (1380, 796), "#303e32")
panels = [
    ("pickup", "Pickup", (750, 400, 1210, 760)),
    ("carry", "Carry / walking reference", (660, 400, 1120, 760)),
    ("005", "Opening swing", (660, 400, 1120, 760)),
    ("051", "Release", (660, 400, 1120, 760)),
    ("056", "Free spin", (660, 400, 1120, 760)),
    ("079", "Confirmed catch", (660, 400, 1120, 760)),
]
for index, (name, label, crop) in enumerate(panels):
    x, y = index % 3 * 460, index // 3 * 398
    with Image.open(SOURCE / "east" / (name + ".png")) as raw:
        review.paste(raw.convert("RGB").crop(crop), (x, y+38))
    ImageDraw.Draw(review).text((x+12, y+10), label, font=FONT, fill="#eddfbf")
path = OUTPUT.parent / "Axe-Continuity.png"
review.save(path)
print(f"Saved {path.relative_to(ROOT)} (native pixels; no per-panel scaling).")
