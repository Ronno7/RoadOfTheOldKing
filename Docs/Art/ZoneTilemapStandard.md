# Zone tilemap standard

Use this structure for every zone. The tutorial environment uses [Hearth & Meadow](Palettes/OpeningZonePalettes-v1.md).

The [tutorial village plan](Tutorial/Plan.md) records the route, placed environment and remaining interactions.

| Layer | Content | Notes |
| --- | --- | --- |
| Ground | Grass, bare earth, stone, mud, dungeon floor | Quiet material fills beneath other layers. Walkable by default. |
| Terrain | Cliffs, riverbanks, water, walls, elevation boundaries | Visible terrain structure; collision is authored separately. |
| Paths | Dirt paths through ancient stone roads | Region 1 paths gradually give way to ancient roads by Region 3. Keep shaped paths and their transitions separate from bare-earth fill. |
| Environment | Trees, rocks, fences, ruins, pillars, buildings | Place static visual structures here. Large structures may span multiple tiles. |
| Detail / Decoration | Flowers, grass tufts, leaves, bones, pottery, bronze fragments, carved stones | Keep movable or breakable versions as interactive GameObjects. |
| Collision | Terrain boundaries and solid objects | Separate invisible Tilemap/Collider layer; no baked collision in ordinary Ground tiles. |
| Above Player | Canopies, arches, roof edges, banners, tall ruins | Existing tutorial overhead parts reuse Environment art, with separate collision. [Authoring guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/AbovePlayer/README.md). |
| Interactive Objects | Breakables, switches, targets, chests, puzzle pieces, bonfires, Recall objects | A GameObject parent/container, not a baked Tilemap. Use reusable prefabs. |

This is an authoring convention. Draw order and object sorting must also accommodate the player's feet and tall-object bases; the list alone does not establish every object's sorting behavior.

## Folder layout

Store art under `Assets/Art/Tiles/<Zone>/<Layer>/`. The current tutorial has **Ground**, **Terrain**, **Paths**, **Environment**, **Collision**, **DetailDecoration**, and an **AbovePlayer** authoring guide. Each production kit owns its atlas, tiles and palettes. Terrain owns its water animations. The Environment atlas supplies both base and overhead portions of its objects. Interactive objects stay under `Assets/Prefabs/`.

One reusable blank template, `Tutorial/Templates/TutorialZoneTemplate.prefab`, combines Ground, Terrain and Paths. The original prototype kit is in `Prototype/Ground/`, with its asset references preserved.

## One presentation scene

Open **Assets/Scenes/DemoTutorial.unity** for the full world-only art presentation: homes, work areas, a garden, orchard, practice enclosure, bridge and modest ruins. Static Environment objects are reusable prefab instances with independent visual/overhead/collision maps. Practice props remain visual-only GameObjects. There are no gameplay scripts or inactive sample worlds in this scene.

Layer builders update assets and palettes without generating scenes. The explicit **Rebuild Demo Tutorial Presentation** menu command recreates this demonstration only. The scene is excluded from gameplay build settings.

[Full presentation](Tutorial/Previews/demo-tutorial.png) · [Village close-up](Tutorial/Previews/tutorial-environment-detail.png). The [village plan](Tutorial/Plan.md) tracks art direction, world layout and deferred atmosphere/occlusion work.

## Production kits

- **Ground:** 308 native 16x16 visual tiles, seven automatic RuleTile brushes and two palettes. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/Ground/README.md) · [Contact sheet](Tutorial/Previews/tutorial-ground-production-atlas.png).
- **Terrain:** 315 native tiles, six automatic brushes, three animated water tiles and three palettes. Five full 47-shape sets cover banks and ledges; walls have 16 cardinal shapes. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/Terrain/README.md) · [Contact sheet](Tutorial/Previews/tutorial-terrain-production-atlas.png).

- **Paths:** 100 native dirt-lane/footpath tiles, two automatic brushes and one palette that also reuses Ground cobbles for paving. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/Paths/README.md).

- **Environment:** 31 objects, 275 tile pieces, one palette, 28 static prefabs and three visual-only practice props. Separate trunk/base collision and overhead art; 14 exact Tutorial colors and binary transparency. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/Environment/README.md).

The environment kits use exact colors from the [opening-zone palette](Palettes/OpeningZonePalettes-v1.md). Native oak, boulder and fence pixels in `Tutorial/Sources/Environment/ApprovedSample16.png` remain production inputs; the cottages use the larger workshop and longhouse designs.

**Detail / Decoration:** 12 designs, eight ground stamps made from 12 collision-free tiles, one palette and four visual-only props. Ground details use Ground sorting order 30; the unlit fire ring uses order 31. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/DetailDecoration/README.md) · [Village detail](Tutorial/Previews/tutorial-decoration-village.png) · [Practice/rest detail](Tutorial/Previews/tutorial-decoration-practice.png). The six approved sample designs are preserved exactly. Atmospheric work remains deferred.
