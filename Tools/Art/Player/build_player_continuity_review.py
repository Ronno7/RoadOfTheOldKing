"""Compare representative current cels at one native scale, without fitting poses.

This is visual triage, not approval of every cel or of animation in motion.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from build_player_style_review import originals

ROOT = Path(__file__).resolve().parents[3]
ART = ROOT / 'TheLostShrine/Assets/Art/Sprites/Player/Actions'
OUT = ROOT / 'Docs/Art/Player/Previews/Current-Continuity.png'
FONT = ImageFont.truetype('C:/Windows/Fonts/consola.ttf',18)


def action(family, direction, index, columns, equipped=True, unarmed=False):
    path = ART / family / f'{family}-{direction}-'
    suffix = 'Unarmed' if unarmed else 'Body'
    body_path = Path(str(path)+suffix+'.png')
    if not body_path.exists():
        return None
    x,y = index%columns*640,index//columns*640
    rect = (x,y,x+640,y+640)
    body = Image.open(body_path).convert('RGBA').crop(rect)
    if equipped:
        weapon = Image.open(str(path)+'Weapon.png').convert('RGBA').crop(rect)
        body = Image.alpha_composite(body,weapon)
    return body


def main():
    references = originals()
    panel_w,panel_h = 640,552
    page_w,page_h = panel_w*3,panel_h*3+60
    atlas = Image.new('RGB',(page_w*2,page_h*2),(40,48,44))
    for index,direction in enumerate(('East','North','South','West')):
        page = Image.new('RGBA',(page_w,page_h),(40,48,44,255))
        draw = ImageDraw.Draw(page)
        draw.text((16,10),direction.upper()+' / CURRENT CONTINUITY / 128 PPU',font=FONT,fill=(242,228,201))
        draw.text((16,34),'Representative poses, fixed foot registration. Inspect full motion in the native GIFs.',font=FONT,fill=(193,207,192))
        original,anchor = references[direction+'_00']
        cel=Image.new('RGBA',(640,640));cel.paste(original,(320-anchor[0],544-anchor[1]))
        entries=[('Original empty-hand walk',cel),
            ('Forehand: set',action('Forehand',direction,0,3)),
            ('Forehand: contact',action('Forehand',direction,3,3)),
            ('Throw: held aim',action('Throw',direction,2,4)),
            ('Throw: released',action('Throw',direction,5,4,False)),
            ('Catch: confirmed grip',action('Catch',direction,1,2)),
            ('Dash: drive',action('Dash',direction,1,3)),
            ('Sprint: carrying contact',action('Sprint',direction,0,4)),
            ('Sprint: empty contact',action('Sprint',direction,0,4,False, direction!='North'))]
        for j,(label,cel) in enumerate(entries):
            x,y=j%3*panel_w,60+j//3*panel_h
            draw.rectangle((x+1,y,x+panel_w-2,y+panel_h-2),fill=(52,61,55))
            draw.text((x+12,y+8),label,font=FONT,fill=(242,228,201))
            if cel is None:
                draw.text((x+12,y+55),'Art deferred / existing fallback',font=FONT,fill=(193,207,192))
                continue
            # The identical viewport retains native pixels and foot position.
            page.alpha_composite(cel.crop((0,128,640,640)),(x,y+30))
            draw.line((x+12,y+446,x+628,y+446),fill=(91,108,92))
        atlas.paste(page.convert('RGB'),((index%2)*page_w,(index//2)*page_h))
    atlas.save(OUT)
    print('Saved representative continuity comparison; no sprite pixels resized or edited.')


if __name__=='__main__':
    main()
