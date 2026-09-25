# Player and hatchet

The [character sheet](../../../TheLostShrine/Assets/Art/Sprites/Player/PlayerSheet.png) defines the player style: blue cap, cream fur, brown leather and soft charcoal outlines. The weapon is a separate sprite. Milestones: [Unarmed locomotion](Locomotion.gif) · [Idle and weapon scale](Idle.gif) · [First chop in-game](Chop.gif).

## Current setup

| Asset | Setup |
| --- | --- |
| PlayerSheet.png | 16 frames: South, East, West and North, four each. 128 PPU, foot pivots, point filtering, no compression or mipmaps. |
| TutorialPlayer.prefab | Gameplay player in the home yard, using the existing movement, stamina, dodge and camera systems. Body offset: (0, -0.25); collider: 0.75 units square. |
| PlayerLocomotion.controller | Four standing clips and four walk loops. One walk cycle per 2.25 units travelled; turns preserve phase and walls stop the gait. |
| Idle breathing | 0.35-second settle, 3.2-second cycle, up to 1.2% vertical expansion at the foot pivot. Root and collision stay fixed. Movement, dash, stagger and attacks restore normal scale. |
| Hatchet.png | Transparent source imported at 43x64, point filtered and uncompressed. Grip pivot preserved; 1.3x visual scale gives a length of about 1.42 world units, 70% of player height. |
| TutorialHatchet.prefab | Equips on approach to the stump. PlayerWeaponGrip maps each character frame to the drawn hand; HatchetView applies the pose after player animation and sorts the hand over the grip. |
| ChopEast.png | Four weapon-free action cels: anticipation, raised backswing, contact and follow-through. 256 source PPU, 1024 import limit; runtime density is close to the existing character. Foot pivots and individual fist anchors preserve attachment. |
| PlayerChopAnimation | Right-facing light-chop sample, including repeated combo swings using the same cels. Locomotion yields body ownership during the action, then resumes. Movement is planted until 78% progress and eases back during recovery. |
| TutorialPracticeDummy.prefab | Existing dummy artwork over the reusable practice mechanics. Visual recoil, pooled wood chips, a synthesized wood-impact placeholder and a dim/reset state; collision does not shake. |

Controls: WASD/arrows to move, Shift to sprint, Space to dodge, scroll to zoom. Carrying follows the hand rather than mouse aim. Left-click toward the right to try the new chop against the practice dummy at (48.93, 58.10). Recall is locked in Tutorial. Other attack directions, distinct combo poses, charged cleave, throwing and dedicated dodge art remain provisional.

Production sources stay in `Assets/Art/Sprites/Player` and `Assets/Art/Sprites/Weapons`. [Player edit prompt](Edit-prompt.txt) · [Hatchet source prompt](Hatchet-prompt.txt). DemoTutorial uses PlayerSheetPreview as its static art reference; PrototypeLoop retains its reference visuals.

## Combat and interaction plan

1. Build one side-facing light chop against a practice target: anticipation, planted lead foot, torso turn, fast cut and recovery. Start from the existing roughly 0.3-second attack and synchronize the visible blade with its damage window.
2. Add impact-only hit pause, target recoil, a compact slash accent and material-specific sound. Reuse combo buffering and stamina; combat owns facing and pose during the strike, then returns to locomotion.
3. Expand to four directions and the combo, then add throw wind-up, release, follow-through and a reaching catch with recoil.
4. Add pickup/reach, stopping/turning, surface footfalls, coat/satchel follow-through and bonfire/rest poses. Add dedicated blinks and upper-body idle frames as needed.

### First chop milestone — 25 September 2026

Established the action with a small side-facing sample before producing the remaining directions. Generated four empty-hand poses from the approved character reference, aligned the feet, measured each fist, then attached the existing hatchet in Unity. [Art source](../../../TheLostShrine/Assets/Art/Sprites/Player/ChopEast.png) · [Pose brief](Chop-prompt.txt).

TutorialHatchetSettings preserves the 0.30-second base swing: 32% anticipation, contact through 53%, then recovery. Its 1.8-unit reach fits the extended arm and enlarged axe. Successful damage adds 45 ms of local action pause; misses and blocked hits do not. The weapon clock drives both the body and blade, with a compact slash accent. PrototypeLoop keeps its existing settings.

The GIF records actual Unity rendering at normal speed. Keep this and the locomotion/idle captures as development milestones; replace redundant working previews instead of accumulating variants. Next: review the sample's feel, then produce the other directions and distinct combo/throw/catch poses. Verification commands and recorded results belong in [SystemsOverview](../../SystemsOverview.md#verification).
