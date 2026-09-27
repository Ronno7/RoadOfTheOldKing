# Animation assets

Playback definitions are grouped by character and action, independently of playback method.

| Folder | Contents |
| --- | --- |
| Player/Locomotion | Idle/walk clips and controller; sprint/dash frame-sequence assets |
| Player/Combat | Opening forehand, throw/aim and catch frame-sequence assets |
| Weapons | Detached hatchet spin sequence |

`.anim` files use Unity's Animator. The custom `.asset` sequences are sampled by gameplay-driven presentation code so body and weapon frames stay synchronized. Both belong here; sprite-sheet textures stay under `Assets/Art/Sprites`.

See [player animation](../../../Docs/PlayerAnimation.md) for timing, layering and previews.
