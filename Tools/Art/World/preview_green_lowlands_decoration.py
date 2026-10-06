"""Compose imported Detail sprites at native scale; never paint or modify source artwork."""
import json
from PIL import Image, ImageDraw
import preview_green_lowlands_environment as env

ROOT = env.ASSETS/'Art/Tiles/GreenLowlands/DetailDecoration'


def main():
    new = env.sprites(ROOT/'GreenLowlandsDecoration16.png')
    reused = env.sprites(env.ASSETS/'Art/Tiles/Tutorial/DetailDecoration/TutorialDecoration16.png')
    catalog = json.loads((ROOT/'DecorationManifest.json').read_text())
    colors = {e['name']:len({p[:3] for p in new[e['name']].get_flattened_data() if p[3]})
              for e in catalog['items']}

    def decorate(im,props,library):
        library.update(reused)
        library.update(new)
        # Flat marks sit below structures; upright clumps share the existing foot-sort preview.
        for name,xy in [
            ('FallenLeaves',(424,78)),('FallenLeaves',(552,287)),
            ('FallenLeaves',(150,61)),('WoodChips',(71,170)),
            ('SplitLogs',(504,160)),('CarvedStone',(210,258)),
            ('MeadowFlowers',(438,113)),('MeadowFlowers',(459,104)),
            ('MeadowFlowers',(482,222)),('MeadowFlowers',(474,238)),
            ('MeadowFlowers',(77,275)),('GrassTuft',(203,176))]:
            im.alpha_composite(reused[name],xy)
        patches = {
            'Grass_Fan': [(8,103),(34,113),(21,165),(89,189),(126,204),
                          (196,204),(212,88),(240,158),(411,132),(429,124),
                          (565,151),(575,264),(440,282),(454,270)],
            'Grass_Swept': [(18,114),(117,210),(204,216),(94,178),(13,234),
                            (91,234),(234,166),(309,250),(393,223),(492,163),
                            (566,159),(414,133)],
            'Grass_Dry': [(18,250),(61,256),(87,291),(121,265),(100,224),
                          (488,154),(516,163),(454,292)],
            'Field_Weeds': [(29,277),(68,291),(121,243),(102,267),(406,265)],
            'Meadow_Reeds': [(307,75),(305,123),(372,38),(372,114),
                             (324,279),(384,251),(385,275)],
            'Collapsed_Crate': [(91,146),(564,168)],
            'WoodenBucket': [(171,108)]
        }
        for name,positions in patches.items():
            props.extend((name,xy) for xy in positions)

    scene = env.environment_scene(decorate)
    scene.convert('RGB').resize((1920,960),Image.Resampling.NEAREST).save(
        env.PREVIEW/'green-lowlands-decoration-detail.png')
    # Small selection at the same pixel density as the actual hero; 4x integer display.
    sheet = Image.new('RGBA',(448,244),'#252C26')
    draw = ImageDraw.Draw(sheet)
    ink = '#DFC291'
    draw.text((12,10),'LOWLANDS DETAIL / 16 PPU',fill=ink)
    labels = ['Grass fan','Swept grass','Dry grass','Bank reeds','Field weeds','Old crate']
    for i,(item,label) in enumerate(zip(catalog['items'],labels)):
        sprite = new[item['name']]
        x = 12+i*72
        draw.text((x,36),label,fill=ink)
        sheet.alpha_composite(sprite,(x+16,88-sprite.height))
    draw.text((12,105),'DIRECT TUTORIAL REUSE',fill=ink)
    for i,name in enumerate(catalog['reusedStamps']):
        sprite = reused[name]
        x = 12+i*72
        label = {'MeadowFlowers':'Flowers','GrassTuft':'Small tuft','FallenLeaves':'Leaves',
                 'WoodChips':'Wood chips','SplitLogs':'Split logs','CarvedStone':'Old stone'}[name]
        draw.text((x,127),label,fill=ink)
        sheet.alpha_composite(sprite,(x+12,163-sprite.height))
    hero = Image.open(env.ASSETS/'Art/Sprites/Player/Rotations/south.png').convert('RGBA')
    sheet.alpha_composite(hero,(17,181))
    sheet.alpha_composite(reused['WoodenBucket'],(65,197))
    sheet.alpha_composite(new['Grass_Fan'],(88,197))
    sheet.alpha_composite(new['Meadow_Reeds'],(116,181))
    draw.text((166,186),f'{min(colors.values())}-{max(colors.values())} colors per new piece',fill=ink)
    draw.text((166,201),'Separate walk-through art',fill=ink)
    draw.text((12,227),'Sparse boundaries / clear paths and thresholds',fill=ink)
    sheet.convert('RGB').resize((1792,976),Image.Resampling.NEAREST).save(
        env.PREVIEW/'green-lowlands-decoration-comparison.png')
    print(json.dumps({'colors_by_piece':colors,'new_designs':len(catalog['items']),
                      'retained_stamps':len(catalog['reusedStamps']),
                      'retained_visuals':catalog['reusedVisuals']}))


if __name__ == '__main__':main()
