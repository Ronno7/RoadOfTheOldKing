# Art source files

Source artwork and reconstruction inputs are kept outside Unity's imported runtime assets.

- [Player](Player/README.md): earlier PixelLab explorations for the hero; runtime sprites are imported directly.
- [Tutorial](Tutorial/README.md): environment and decoration source sheets.
- [Vegetation provenance](Vegetation/Provenance.json): [two separate grass leaves](Vegetation/GrassLeaves_Source.png) generated with built-in imagegen; exact [initial prompt](Vegetation/GrassLeaves.prompt.txt) and [solid-leaf refinement](Vegetation/GrassLeaves.edit.prompt.txt). `TutorialVegetationBuilder.BuildLeafArt` prepares native 18/14-pixel leaves with four Tutorial greens, hard alpha and bottom pivots at 16 PPU. The runtime `Assets/Art/Vegetation/GrassLeaves16.png` atlas also copies accepted flowers/reeds unchanged. Intact leaves rotate smoothly with separate timing; `RefreshSpecies` changes species art without rebaking density or placements.
- [Green Lowlands provenance](GreenLowlands/Provenance.json): source files and exact built-in imagegen prompts for original regional architecture, the mill, cart, bridge, old stone post and weathered fencing. Superseded building derivatives are marked in the provenance. Source PNGs are in `GreenLowlands/Environment`; native dimensions and material swatches are set by the runtime Environment manifest.
  Separate grass, reeds, field weeds and collapsed-crate sources are in `GreenLowlands/Decoration`, using the same provenance file and the regional Decoration manifest for native sizes and swatches.
  `GreenLowlands/Interactive/BonfireFlame_Source.png` supplies the accepted four-frame shared bonfire flame, with its prompt in the same provenance file. `BonfireFlameManifest.json` under `Assets/Art/Sprites/World/Bonfire` sets the fixed 16x24 frame size and five-color palette. The shared bonfire reuses the original Tutorial fire ring directly. Regional bases may change; the flame remains the same across the world.
  [Arrival layout seed](GreenLowlands/ArrivalLayout.json) records the initial composition used to start the regional art scene. The authored `Assets/Scenes/GreenLowlands.unity` is now authoritative; this seed must not overwrite later scene work.

Runtime sprites and tile assets live under `RoadOfTheOldKing/Assets/Art`. [Player animation](../Docs/PlayerAnimation.md) and [world presentation](../Docs/World.md) explain how the assets are used.
