# Player source art

PixelLab export of the hero, kept as delivered. It holds `metadata.json` (character prompt, canvas size, directions) and one folder per PixelLab state, with `rotations/<direction>.png` standing views and `animations/<Name>/<direction>/frame_###.png` sequences.

| Export | Contents |
| --- | --- |
| `Idle/` and `metadata.json` | State `Idle`: eight standing views (32 x 32); Walking and Running (8 frames) and Quick_Dash (4 frames), 40 x 40; Taking_Punch (6 frames), Drinking (6 frames) and Throw_Object (7 frames), 48 x 48 |
| `sitting_down/` | PixelLab state `7ddc3425-25cc-4c08-a05e-c59c05cc6862`: eight seated rotations and `animations/Idle` (8 frames per direction, 40 x 40), downloaded as delivered on 3 October 2026. Seated breathing group `bfd2bf24-500b-41a8-ad90-64ca366725bb` fills the runtime Rest slot |

Runtime copies live under `RoadOfTheOldKing/Assets/Art/Sprites/Player/<Slot>/`; the import pipeline is described in [player animation](../../Docs/PlayerAnimation.md#assets-and-import). The previous hand-assembled character was retired on 28 September 2026; samples are in [Docs/Art/Player/Legacy](../../Docs/Art/Player/Legacy) and the full sources remain in the repository history.
