# Player source art

PixelLab exports of the hero, kept as delivered. Each export folder holds `metadata.json` (character prompt, canvas size, directions) and one folder per PixelLab state, with `rotations/<direction>.png` standing views and `animations/<Name>/<direction>/frame_###.png` sequences.

| Export | Contents |
| --- | --- |
| A_small_hooded_wanderer_for_a_top-down_16-bit_acti | State `Idle`: eight standing views (32 x 32) and an eight-frame walk (40 x 40) |

Runtime copies live under `RoadOfTheOldKing/Assets/Art/Sprites/Player/<Slot>/`; the import pipeline is described in [player animation](../../Docs/PlayerAnimation.md#assets-and-import). The previous hand-assembled character was retired on 28 September 2026; samples are in [Docs/Art/Player/Legacy](../../Docs/Art/Player/Legacy) and the full sources remain in the repository history.
