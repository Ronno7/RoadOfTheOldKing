# Player source art

PixelLab export of the hero, kept as delivered. It holds `metadata.json` (character prompt, canvas size, directions) and one folder per PixelLab state, with `rotations/<direction>.png` standing views and `animations/<Name>/<direction>/frame_###.png` sequences.

| Export | Contents |
| --- | --- |
| `Idle/` and `metadata.json` | State `Idle`: eight standing views (32 x 32); Walking and Running (8 frames) and Quick_Dash (4 frames), 40 x 40 |

Runtime copies live under `RoadOfTheOldKing/Assets/Art/Sprites/Player/<Slot>/`; the import pipeline is described in [player animation](../../Docs/PlayerAnimation.md#assets-and-import). The previous hand-assembled character was retired on 28 September 2026; samples are in [Docs/Art/Player/Legacy](../../Docs/Art/Player/Legacy) and the full sources remain in the repository history.
