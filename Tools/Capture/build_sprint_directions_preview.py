"""Compare native east/north or east/west sprint captures at game zoom/speed."""
import argparse
import csv
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Docs/Art/Player/Previews"
font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 17)
small = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 14)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--second',choices=('North','West'),default='North')
second = parser.parse_args().second
directions = ('East',second)
sources = {name: ROOT / f"tmp/sprint-proof/native-{name.lower()}" for name in directions}
rows = {name: list(csv.reader((path / "timeline.csv").open())) for name, path in sources.items()}
if len(rows["East"]) != len(rows[second]):
    raise AssertionError("Compare matching capture timelines")
frames = []
for index in range(len(rows["East"])):
    frame = Image.new("RGB", (1000,470), (44,57,45));draw = ImageDraw.Draw(frame)
    title = ("Before pickup", "Carrying the hatchet", "Axe away: empty hands")[index//100]
    draw.text((16,8),f"SPRINT / {title} / native Tutorial / 1x speed",font=font,fill=(244,224,181))
    for column, name in enumerate(directions):
        action, cel = rows[name][index][1:3]
        shot = Image.open(sources[name] / f"{index:03}.png").convert("RGB").crop((580,350,1080,750))
        frame.paste(shot,(column*500,70))
        draw.text((column*500+16,40),f"{name} / {action} / cel {int(cel)+1 if int(cel)>=0 else '-'}",font=small,fill=(206,213,193))
    frames.append(frame)
gif_name = 'Sprint-Directions.gif' if second == 'North' else 'Sprint-East-West.gif'
frames[0].save(DEST / gif_name,save_all=True,append_images=frames[1:],duration=20,loop=0,optimize=True)

if second == 'North':
    indices=[105]+[int(next(row[0] for row in rows["North"] if 122<=int(row[0])<168 and row[1]=="Sprint" and int(row[2])==cel)) for cel in range(8)]
    sheet=Image.new("RGB",(1200,960),(44,57,45));draw=ImageDraw.Draw(sheet)
    for i,(index,label) in enumerate(zip(indices,["Original north carry","1 Right contact","2 Right passing","3 Right push","4 Left reach","5 Left contact","6 Left passing","7 Left push","8 Right reach"])):
        shot=Image.open(sources["North"]/f"{index:03}.png").convert("RGB").crop((650,430,1050,710))
        x,y=(i%3)*400,(i//3)*320
        sheet.paste(shot,(x,y+35));draw.text((x+12,y+10),label,font=font,fill=(244,224,181))
    sheet.save(DEST / "North-Sprint-Cels.png")
print(f'Saved native {gif_name}.')
