"""Bake the selected drawn pickup/carry axe to the same native density as actions."""
from pathlib import Path
from PIL import Image
from build_player_throw_layers import body_support, isolate
from build_player_return_layers import register
from player_contours import normalize_prop

ROOT = Path(__file__).resolve().parents[3]


def main():
    source = Image.open(ROOT / "ArtSource/Player/Weapon/Hatchet-Carry.png").convert("RGBA")
    # Fixed grip and density, not auto-fitting: 176 native pixels for the full axe.
    source = isolate(source, body_support(source))
    sprite = register(source, 256, 176 / 872, (698, 908), (140, 201))
    sprite = normalize_prop(sprite)
    sprite.save(ROOT / "RoadOfTheOldKing/Assets/Art/Sprites/Weapons/Hatchet.png")
    print("carry: 256 square, 128 PPU, grip pivot (140,55), unit renderer scale")

    source = Image.open(ROOT / "ArtSource/Player/Weapon/Hatchet-Turns.png").convert("RGBA")
    turns = Image.new("RGBA", (512, 256))
    for index, grip in enumerate(((537, 791), (300, 791))):
        cel = source.crop((index*768, 0, (index+1)*768, 1024))
        # The generator added faint surrounding haze; retain only the solid drawing.
        cel = isolate(cel, cel.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
        cel = isolate(cel, body_support(cel))
        cel = register(cel, 256, 176/768, grip, (140, 201))
        turns.paste(normalize_prop(cel), (index*256, 0))
    turns.save(ROOT / "RoadOfTheOldKing/Assets/Art/Sprites/Weapons/HatchetTurns.png")
    print("turns: oblique/edge-on, matched native length and grip, no background haze")


if __name__ == "__main__":
    main()
