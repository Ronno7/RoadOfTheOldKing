# World and tilemaps

[Systems overview](SystemsOverview.md) · [Player animation](PlayerAnimation.md)

Tutorial moves from a sheltered village and practice terrace through woodland, a river crossing and old stone ruins. The world uses native 16 x 16 tiles at 16 pixels per unit, with muted green foliage, warm earth and timber, and restrained surface detail.

![Tutorial world overview](Art/Tutorial/Previews/tutorial-populated.png)

[Village and work yard](Art/Tutorial/Previews/tutorial-populated-home.png) · [Practice terrace](Art/Tutorial/Previews/tutorial-populated-practice.png) · [Forest road](Art/Tutorial/Previews/tutorial-forest-road.png) · [Ruined courtyard](Art/Tutorial/Previews/tutorial-recall-courtyard.png)

The Tutorial combines this environment with its full teaching route. Gameplay objects (axe pickup, target stands, practice dummy, Recall stone and drill posts, wolf, Sun Shard and bonfire) are prefab instances placed over the tilemaps; the [systems overview](SystemsOverview.md#tutorial) describes how they work. Static ruin artwork alone does not imply an interaction. The transition to the overworld is in development.

## Tilemap composition

Visual layers and physical boundaries are independent. A water or cliff tile does not automatically become an obstacle; a separate collision map defines where the player can travel.

| Layer | Responsibility |
| --- | --- |
| Ground | Grass, earth, stone and other base surfaces |
| Terrain | Riverbanks, water, cliffs, low walls and elevation edges |
| Paths | Dirt lanes, narrow footpaths and paving |
| Environment | Trees, buildings, fences, rocks and ruins |
| Detail / Decoration | Sparse ground accents and small props |
| Collision | Invisible solid boundaries and object footprints |
| Above Player | Canopies, roofs and other overhead portions |
| Interactive Objects | GameObjects for pickups, targets, enemies, puzzles and bonfires |

Trees and buildings use separate base, overhead and collision elements. Trunk-sized footprints allow movement behind canopies, while solid lower building footprints remain blocked. Independent object instances preserve overlapping silhouettes. Elevation is visual: ramps and terraces share the same 2D physics plane.

## RuleTiles and terrain connections

Unity RuleTiles inspect neighboring cells and choose the matching edge, corner or junction sprite. Separate material maps allow paths and ground treatments to overlap without replacing the underlying surface.

| Kit | Contents |
| --- | --- |
| Ground | 308 native tiles across seven materials |
| Terrain | 315 tiles for banks, water, cliffs, walls, stairs and ramps |
| Paths | 100 dirt-lane and footpath tiles; paving reuses ground cobbles |
| Environment | 31 objects represented by 275 tile pieces, including buildings, vegetation and ruins |
| Detail / Decoration | 12 designs for ground accents and small village props |

Five terrain materials have all 47 valid edge/corner combinations; low walls use 16 cardinal connections. Taller cliff faces and stairs combine repeatable pieces. Atlas borders include pixel extrusion to prevent neighboring sprites bleeding into tile edges.

[Ground atlas](Art/Tutorial/Previews/tutorial-ground-production-atlas.png) · [Terrain atlas](Art/Tutorial/Previews/tutorial-terrain-production-atlas.png) · [Path atlas](Art/Tutorial/Previews/tutorial-paths-production-atlas.png)

## Animated water

![Waterfall and landing foam](Art/Tutorial/Previews/waterfall-motion.gif)

Ripples, falling water and landing foam each use a four-frame Unity AnimatedTile. Ripples run at 4 fps; waterfall body and foam use a 1.25 multiplier for 5 fps. Repeating waterfall-body tiles connect a static lip to the animated landing. No custom runtime water-animation script is required.

## Assets and runtime

Python generators define ground, terrain and path pixels. Editor-only builders create the sliced sprites, Tile/RuleTile/AnimatedTile assets, palettes and prefab structures. Runtime scenes consume ordinary Unity assets; generation tools do not run in the game.

The Tutorial environment uses the [Hearth & Meadow palette](Art/Palettes/tutorial-hearth-and-meadow-v1.gpl). Source drawings and manifests live in [ArtSource](../ArtSource/README.md), with generated runtime assets under `Assets/Art/Tiles/Tutorial`.

[Environment presentation](Art/Tutorial/Previews/demo-tutorial.png) · [Village detail](Art/Tutorial/Previews/tutorial-decoration-village.png) · [Practice-area detail](Art/Tutorial/Previews/tutorial-decoration-practice.png)
