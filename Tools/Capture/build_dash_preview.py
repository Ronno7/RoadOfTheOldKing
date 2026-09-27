"""Package the native Tutorial capture without scaling gameplay pixels."""
import argparse
import csv
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--direction", choices=("East", "South", "West"), default="East")
direction = parser.parse_args().direction
SOURCE = ROOT / f"tmp/dash-proof/native-{direction.lower()}"
DEST = ROOT / "Docs/Art/Player/Previews"
font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 17)
small = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 14)
rows = list(csv.reader((SOURCE / "timeline.csv").open()))
frames = []
for row in rows:
    index, action, cel, state, position = row
    index = int(index)
    shot = Image.open(SOURCE / f"{index:03}.png").convert("RGB").crop((530,310,1530,750))
    frame = Image.new("RGB", (1000,500), (44,57,45));frame.paste(shot,(0,60))
    draw = ImageDraw.Draw(frame)
    title = "Before pickup" if index < 70 else "Carrying the hatchet" if index < 135 else "Axe away: empty-hand dash"
    draw.text((16,10),direction.upper()+" DASH / "+title,font=font,fill=(244,224,181))
    draw.text((16,35),f"Native Tutorial / 1x speed / {action} / cel {int(cel)+1 if int(cel)>=0 else '-'}",font=small,fill=(206,213,193))
    frames.append(frame)
gif_name = "Dash.gif" if direction == "East" else f"Dash-{direction}.gif"
frames[0].save(DEST / gif_name,save_all=True,append_images=frames[1:],duration=20,loop=0,optimize=True)

# Actual scene cels beside the original carry; no resampling.
indices=[75]+[int(next(row[0] for row in rows if 85<=int(row[0])<120 and row[1]=="Dash" and int(row[2])==cel)) for cel in range(5)]
labels=["Original carry","Push-off","Drive","Travel","Land","Recover"]
sheet=Image.new("RGB",(1200,740),(44,57,45));draw=ImageDraw.Draw(sheet)
for i,(index,label) in enumerate(zip(indices,labels)):
    shot=Image.open(SOURCE/f"{index:03}.png").convert("RGB").crop((650,430,1050,760))
    x,y=(i%3)*400,(i//3)*370
    sheet.paste(shot,(x,y+35));draw.text((x+12,y+10),label,font=font,fill=(244,224,181))
cel_name = "Dash-Cels.png" if direction == "East" else f"{direction}-Dash-Cels.png"
sheet.save(DEST / cel_name)
print(f"Saved native {gif_name} and six-panel carry/dash comparison.")
