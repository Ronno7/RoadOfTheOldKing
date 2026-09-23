# Zone tilemap standard

Use this structure for every zone. Build the tutorial art first using [Hearth & Meadow](OpeningZonePalettes-v1.md).

The [tutorial village plan](TutorialVillagePlan.md) records the intended route and upcoming environment assets.

| Layer | Content | Notes |
| --- | --- | --- |
| Ground | Grass, bare earth, stone, mud, dungeon floor | Quiet material fills beneath other layers. Walkable by default. |
| Terrain | Cliffs, riverbanks, water, walls, elevation boundaries | Visible terrain structure; collision is authored separately. |
| Paths | Dirt paths through ancient stone roads | Region 1 paths gradually give way to ancient roads by Region 3. Keep shaped paths and their transitions separate from bare-earth fill. |
| Environment | Trees, rocks, fences, ruins, pillars, buildings | Place static visual structures here. Large structures may span multiple tiles. |
| Detail / Decoration | Flowers, grass tufts, leaves, bones, pottery, bronze fragments, carved stones | Keep movable or breakable versions as interactive GameObjects. |
| Collision | Terrain boundaries and solid objects | Separate invisible Tilemap/Collider layer; no baked collision in ordinary Ground tiles. |
| Above Player | Canopies, arches, roof edges, banners, tall ruins | Only the parts that should cover the player. |
| Interactive Objects | Breakables, switches, targets, chests, puzzle pieces, bonfires, Recall objects | A GameObject parent/container, not a baked Tilemap. Use reusable prefabs. |

This is an authoring convention. Draw order and object sorting must also accommodate the player's feet and tall-object bases; the list alone does not establish every object's sorting behavior.

## Folder layout

Store art under `Assets/Art/Tiles/<Zone>/<Layer>/`. The current tutorial has **Ground**, **Terrain**, **Paths**, **Collision**, and reserved **Environment**, **DetailDecoration**, and **AbovePlayer** folders. Ground/Terrain/Paths own their atlases, tiles, brushes and palettes. Terrain owns its water animations. Interactive objects stay under `Assets/Prefabs/`.

One reusable blank template, `Tutorial/Templates/TutorialZoneTemplate.prefab`, combines Ground, Terrain and Paths. The original prototype kit is in `Prototype/Ground/`, with its asset references preserved.

## One presentation scene

Open **Assets/Scenes/DemoTutorial.unity** for the combined world-only art presentation. It shows the available materials, garden plots, a village square, river/island pond, cliff waterfall, stairs and a stone terrace. Play Mode animates the water. It contains only world tiles, a camera and light; author the actual tutorial separately.

Layer builders update assets and palettes without generating scenes. The explicit **Rebuild Demo Tutorial Presentation** menu command recreates this demonstration only. The scene is excluded from gameplay build settings.

[Presentation image](demo-tutorial.png)

## Production kits

- **Ground:** 308 native 16x16 visual tiles, seven automatic RuleTile brushes and two palettes. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/Ground/README.md) · [Contact sheet](tutorial-ground-production-atlas.png).
- **Terrain:** 315 native tiles, six automatic brushes, three animated water tiles and three palettes. Five full 47-shape sets cover banks and ledges; walls have 16 cardinal shapes. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/Terrain/README.md) · [Contact sheet](tutorial-terrain-production-atlas.png).

- **Paths:** 100 native dirt-lane/footpath tiles, two automatic brushes and one palette that also reuses Ground cobbles for paving. [Painting guide](../../TheLostShrine/Assets/Art/Tiles/Tutorial/Paths/README.md).

All production atlases use exact native pixels and the approved [opening-zone palette](OpeningZonePalettes-v1.md). The older [generated ground draft](tutorial-ground-v1.png) remains a visual study, not the production atlas.
