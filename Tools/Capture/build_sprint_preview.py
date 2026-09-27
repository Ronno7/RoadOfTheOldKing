"""Package the native Tutorial sprint capture without scaling gameplay pixels."""
import argparse
import csv
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--direction", choices=("East", "South", "West"), default="East")
direction = parser.parse_args().direction
SOURCE = ROOT / f"tmp/sprint-proof/native-{direction.lower()}"
DEST = ROOT / "Docs/Art/Player/Previews"
font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 17)
small = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 14)
rows = list(csv.reader((SOURCE / "timeline.csv").open()))
if len(rows) != 300:
    raise ValueError("Capture all three 100-frame walk/sprint/stop segments first")
frames = []
for index, action, cel, state, position in rows:
    index = int(index)
    shot = Image.open(SOURCE / f"{index:03}.png").convert("RGB").crop((530,310,1530,750))
    frame = Image.new("RGB", (1000,500), (44,57,45))
    frame.paste(shot,(0,60))
    draw = ImageDraw.Draw(frame)
    title = ("Before pickup", "Carrying the hatchet", "Axe away: empty hands")[index//100]
    draw.text((16,10),direction.upper()+" SPRINT / "+title,font=font,fill=(244,224,181))
    draw.text((16,35),f"Native Tutorial / 1x speed / {action} / cel {int(cel)+1 if int(cel)>=0 else '-'}",font=small,fill=(206,213,193))
    frames.append(frame)
gif_name = "Sprint.gif" if direction == "East" else f"Sprint-{direction}.gif"
frames[0].save(DEST / gif_name,save_all=True,append_images=frames[1:],duration=20,loop=0,optimize=True)

# Same native scale: original carry and all eight authored sprint cels.
indices = [105] + [int(next(row[0] for row in rows if 122<=int(row[0])<168 and row[1]=="Sprint" and int(row[2])==cel)) for cel in range(8)]
labels = ["Original carry", "1 Near contact", "2 Near passing", "3 Near push", "4 Far reach", "5 Far contact", "6 Far passing", "7 Far push", "8 Near reach"]
if direction == "South":
    labels = ["Original carry", "1 Right contact", "2 Right passing", "3 Right push", "4 Left reach", "5 Left contact", "6 Left passing", "7 Left push", "8 Right reach"]
sheet = Image.new("RGB",(1200,960),(44,57,45))
draw = ImageDraw.Draw(sheet)
for i,(index,label) in enumerate(zip(indices,labels)):
    shot = Image.open(SOURCE/f"{index:03}.png").convert("RGB").crop((650,430,1050,710))
    x,y = (i%3)*400,(i//3)*320
    sheet.paste(shot,(x,y+35))
    draw.text((x+12,y+10),label,font=font,fill=(244,224,181))
cel_name = "Sprint-Cels.png" if direction == "East" else f"{direction}-Sprint-Cels.png"
sheet.save(DEST / cel_name)
if direction in ("East", "West", "South"):
    # Show the distinct empty-hand arm cycle beside the original weapon-free walk.
    indices = [5] + [int(next(row[0] for row in rows if 22<=int(row[0])<68 and row[1]=="Sprint" and int(row[2])==cel)) for cel in range(8)]
    unarmed = Image.new("RGB",sheet.size,(44,57,45)); draw = ImageDraw.Draw(unarmed)
    for i,(index,label) in enumerate(zip(indices,["Original empty-hand walk"]+labels[1:])):
        shot = Image.open(SOURCE/f"{index:03}.png").convert("RGB").crop((650,430,1050,710))
        x,y = i%3*400,i//3*320
        unarmed.paste(shot,(x,y+35));draw.text((x+12,y+10),label,font=font,fill=(244,224,181))
    unarmed.save(DEST / f"{direction}-Unarmed-Cels.png")
print(f"Saved native {gif_name} and nine-panel carry/sprint comparison.")
