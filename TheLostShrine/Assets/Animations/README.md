# Animation assets

Organize playback definitions by character and action, regardless of playback method.

| Folder | Contents |
| --- | --- |
| Player/Locomotion | Idle/walk clips and controller; sprint/dash frame-sequence assets |
| Player/Combat | Opening forehand, throw/aim and catch frame-sequence assets |
| Weapons | Detached hatchet spin sequence |

`.anim` files use Unity's Animator. The custom `.asset` sequences are sampled by gameplay-driven presentation code so body and weapon frames stay synchronized. Both belong here; sprite-sheet textures stay under `Assets/Art/Sprites`.

Future player interactions can get an `Interactions` folder when those assets exist. Keep gameplay tuning under `Assets/Settings`, reusable object setups under `Assets/Prefabs`, and external rebuild inputs in the workspace's `ArtSource` folder.
