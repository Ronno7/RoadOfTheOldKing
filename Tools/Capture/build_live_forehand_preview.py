"""Package real Tutorial opening swings, retaining native camera pixel scale."""
import argparse
import csv
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--direction", choices=("East", "North", "South", "West"), default="South")
direction = parser.parse_args().direction
source = ROOT / f"tmp/forehand-proof/native-{direction.lower()}"
dest = ROOT / "Docs/Art/Player/Previews"
rows = list(csv.reader((source / "timeline.csv").open()))
if len(rows) != 150:
    raise ValueError("Run CaptureLiveForehand.cs.txt first")
font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 17)
small = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 14)
frames = []
for index, action, cel, paused, hp in rows:
    index = int(index)
    shot = Image.open(source / f"{index:03}.png").convert("RGB").crop((530,300,1530,870))
    frame = Image.new("RGB", (1000,630), (44,57,45));frame.paste(shot,(0,60))
    draw = ImageDraw.Draw(frame)
    draw.text((16,10),f"{direction.upper()} FOREHAND / "+("Miss and recover" if index<70 else "Actual target hit"),font=font,fill=(244,224,181))
    draw.text((16,35),f"Native Tutorial / 1x speed / {action} / "+("contact pause" if paused=="True" else "gameplay clock"),font=small,fill=(206,213,193))
    frames.append(frame)
frames[0].save(dest/f"Forehand-{direction}.gif",save_all=True,append_images=frames[1:],duration=20,loop=0,optimize=True)
indices=[5]+[int(next(r[0] for r in rows if 20<=int(r[0])<60 and r[1]=="Forehand" and int(r[2])==cel)) for cel in range(6)]
indices.append(int(next(r[0] for r in rows if int(r[0])>90 and r[3]=="True")))
labels=["Original carry","1 Set","2 Load","3 Drive","4 Contact","5 Absorb","6 Recover","Actual contact pause"]
sheet=Image.new("RGB",(1520,1040),(44,57,45));draw=ImageDraw.Draw(sheet)
for i,(index,label) in enumerate(zip(indices,labels)):
    shot=Image.open(source/f"{index:03}.png").convert("RGB").crop((620,340,1000,830))
    x,y=i%4*380,i//4*520;sheet.paste(shot,(x,y+30));draw.text((x+12,y+7),label,font=font,fill=(244,224,181))
sheet.save(dest/f"Forehand-{direction}-Cels.png")
print(f"Saved native {direction} forehand GIF and carry/cel/contact comparison.")
