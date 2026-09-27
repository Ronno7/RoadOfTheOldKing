"""Compare native east/north Tutorial dash captures at actual game zoom/speed."""
import csv
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Docs/Art/Player/Previews"
font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 17)
small = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 14)
sources = {name: ROOT / f"tmp/dash-proof/native-{name.lower()}" for name in ("East", "North")}
rows = {name: list(csv.reader((path / "timeline.csv").open())) for name, path in sources.items()}
if len(rows["East"]) != len(rows["North"]):
    raise AssertionError("Compare matching capture timelines")
frames = []
for index in range(len(rows["East"])):
    frame = Image.new("RGB", (1000,520), (44,57,45));draw = ImageDraw.Draw(frame)
    title = "Before pickup" if index < 70 else "Carrying the hatchet" if index < 135 else "Axe away: empty-hand dash"
    draw.text((16,8),f"DASH / {title} / native Tutorial / 1x speed",font=font,fill=(244,224,181))
    for column, name in enumerate(("East", "North")):
        action, cel = rows[name][index][1:3]
        shot = Image.open(sources[name] / f"{index:03}.png").convert("RGB").crop((580,350,1080,800))
        frame.paste(shot,(column*500,70))
        draw.text((column*500+16,40),f"{name} / {action} / cel {int(cel)+1 if int(cel)>=0 else '-'}",font=small,fill=(206,213,193))
    frames.append(frame)
frames[0].save(DEST / "Dash-Directions.gif",save_all=True,append_images=frames[1:],duration=20,loop=0,optimize=True)

indices=[75]+[int(next(row[0] for row in rows["North"] if 85<=int(row[0])<120 and row[1]=="Dash" and int(row[2])==cel)) for cel in range(5)]
sheet=Image.new("RGB",(1200,740),(44,57,45));draw=ImageDraw.Draw(sheet)
for i,(index,label) in enumerate(zip(indices,["Original north carry","Push-off","Drive","Travel","Land","Recover"])):
    shot=Image.open(sources["North"]/f"{index:03}.png").convert("RGB").crop((650,430,1050,760))
    x,y=(i%3)*400,(i//3)*370
    sheet.paste(shot,(x,y+35));draw.text((x+12,y+10),label,font=font,fill=(244,224,181))
sheet.save(DEST / "North-Dash-Cels.png")
print("Saved native Dash-Directions.gif and north carry/dash comparison.")
