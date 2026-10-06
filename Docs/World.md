# World and tilemaps

[Systems overview](SystemsOverview.md) · [Player animation](PlayerAnimation.md)

Tutorial moves from a sheltered village and practice terrace through woodland, a river crossing and old stone ruins. The world uses native 16 x 16 tiles at 16 pixels per unit, with muted green foliage, warm earth and timber, and restrained surface detail.

![Tutorial world overview](Art/Tutorial/Previews/tutorial-populated.png)

[Village and work yard](Art/Tutorial/Previews/tutorial-populated-home.png) · [Practice terrace](Art/Tutorial/Previews/tutorial-populated-practice.png) · [Forest road](Art/Tutorial/Previews/tutorial-forest-road.png) · [Ruined courtyard](Art/Tutorial/Previews/tutorial-recall-courtyard.png)

The Tutorial combines this environment with its full teaching route. Gameplay objects (axe pickup, target stands, practice dummy, Recall stone and seal gate, wolves, Sun Shard and bonfire) are prefab instances placed over the tilemaps; the [systems overview](SystemsOverview.md#tutorial) describes how they work. Static ruin artwork alone does not imply an interaction. Its exit connects to the Green Lowlands arrival pocket.

## Interactive vegetation

Tutorial meadows now contain grass, flowers and occasional waterside reeds that sway, bend away from the player's feet and settle after passage. Actual axe swings, throws and Recall cut them with short leaf flecks. Cut plants return on rest or scene reload; they do not affect movement, combat rewards or saved progression. Existing painted decoration remains static.

Grass uses two separate leaf sprites with staggered wind timing, each rotating smoothly around its bottom pivot. The leaves remain intact as they move: no row deformation, stepped animation or UV distortion. This is an intentional modern treatment of pixel art. New 18/14-pixel grass leaves use four Tutorial greens at native 16 PPU; flowers and reeds retain their existing artwork as intact bottom-pivoted sprites. An editable density map places 967 plants in patches, excluding dirt, paths, water, solid footprints and interaction clearings.

Grass depth follows its stationary roots and the player's feet: nearby plants south of the feet draw over the player; plants north of the feet draw behind. The player wins equal-height ties. Ground and foreground draws share each chunk's mesh and cut indices, and only nearby chunks enable the extra draw. This keeps distant grass under existing props and overheads without sorting every plant on the CPU.

One field owns small spatial mesh groups, with continuous shader rotation and no per-plant objects or colliders. Cutting queries nearby groups during the weapon's real damage interval and removes their triangle indices; melee respects solid terrain. Per-plant root/phase data follows [GPU Gems' grass animation approach](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-7-rendering-countless-blades-waving-grass); local groups balance draw calls against the shared culling described in [Unity's mesh combination guidance](https://docs.unity3d.com/6000.3/Documentation/Manual/combining-meshes.html).

[Motion preview](Art/Tutorial/Previews/tutorial-interactive-vegetation.gif) (768x480 renders at 30 fps; staged player positions and simulation steps, not a recorded keyboard playthrough). The reusable system is first placed in Tutorial; existing Green Lowlands decoration has not been converted.

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

## Green Lowlands art

The first **GreenLowlands** scene is a playable 40-by-20-cell arrival pocket connected to the Tutorial in both directions. It includes the shared player/HUD, session and follow camera, plus a Lowlands Bonfire for rest, travel and respawn. Upgrade purchases remain disabled at this fire. Render-only meadow, river and road continuation beyond the playable bounds keep the camera view filled without expanding the walkable area.

[Interactive arrival](Art/GreenLowlands/green-lowlands-interactive.png) | [Resting at the bonfire](Art/GreenLowlands/green-lowlands-bonfire.png) | [Initial art layout](Art/GreenLowlands/green-lowlands-integration.png) | [Player overlap checks](Art/GreenLowlands/green-lowlands-overlap-checks.png). The earlier layout/overlap captures used a temporary scale reference; the current scene contains the real player. Automated checks cover route movement, both exit triggers, rest/travel, checkpoint respawn and Continue across a fresh session. A human keyboard/full Tutorial playthrough and browser test remain pending.

All bonfires inherit a shared four-frame flame at 6 fps and a default stone fire ring. Regional variants can change the base while retaining the same flame. Both the flame and defeated enemy sprites render below the player. Its 16x24 frames use five colors, binary alpha and native 16 PPU. `BonfireSpriteView` displays the flame only after the fire is discovered; the existing Bonfire and checkpoint systems continue to own rest, travel and saving. Initial art production is complete through this interactive arrival; the broader overworld, Puzzle 1, enemies and first upgrade remain later work.

The first regional Ground kit is available as **GreenLowlandsGround_Paint** and **GreenLowlandsGround_IndividualPieces**: 308 native tiles and seven RuleTile brushes. The regional drawings use meadow clusters, sun-dried grass, compacted soil with grit, embedded stones and wear marks, silt pockets, buried slabs, rough fieldstones and broad plough furrows. They retain Tutorial pixel scale, clean transition silhouettes and one-pixel material rims, with distinct regional texture designs. The [Field and Old Road palette](Art/Palettes/green-lowlands-field-and-old-road-v1.gpl) supplies all ground colors; the atlas uses 10 opaque colors, 3-4 per material.

[Country art preview](Art/GreenLowlands/green-lowlands-ground-detail.png) | [Side-by-side comparison](Art/GreenLowlands/green-lowlands-ground-comparison.png) | [Ground atlas](Art/GreenLowlands/green-lowlands-ground-atlas.png).

The Terrain kit adds clean meadow and stone banks, muted water, earth terraces, broad stone shelves and stairs, soil ramps and low walls. Its 315 native tiles, six RuleTiles and three water animations are available through **GreenLowlandsTerrain_Paint**, **GreenLowlandsTerrain_Structures** and **GreenLowlandsTerrain_IndividualPieces**. Terrain uses 14 palette colors in total, 2-8 per material family. Both regional kits use the existing blank zone template and separate collision map.

[Terrain preview](Art/GreenLowlands/green-lowlands-terrain-detail.png) | [Water motion](Art/GreenLowlands/green-lowlands-terrain-motion.gif) | [Terrain atlas](Art/GreenLowlands/green-lowlands-terrain-atlas.png). These individual-layer previews are offline compositions of exported tiles and unchanged Tutorial props/hero.

**GreenLowlandsPaths** supplies broad dirt lanes and narrow field tracks: 100 new native tiles, two RuleTiles and direct references to Ground's fieldstones and buried slabs. Lanes use four colors and trails three, matching the regional soil. The palette uses the existing three Paths maps. [Paths preview](Art/GreenLowlands/green-lowlands-paths-detail.png) | [Paths atlas](Art/GreenLowlands/green-lowlands-paths-atlas.png).

**GreenLowlandsEnvironment** combines 19 retained Tutorial nature/fencing/prop objects with nine regional prefabs: idle watermill, empty cart, plank bridge, old stone fence post, hipped-roof farmhouse, gambrel threshing barn, open drying shed, roofless windlass well and weathered fence. The architecture has original regional silhouettes while sharing Tutorial rendering style. The farmstead suggests abandonment a few generations ago: intact structures, closed openings, small wear and sparse moss/ivy. Individual pieces use 6–10 colors from the regional palette, with separate base, overhead and collision maps. The bridge has a two-cell deck; the mill is a static exterior. Tutorial originals remain unchanged. [Country preview](Art/GreenLowlands/green-lowlands-environment-detail.png) | [Native-scale comparison](Art/GreenLowlands/green-lowlands-environment-comparison.png) | [Sources and prompts](../ArtSource/GreenLowlands/Provenance.json).

**GreenLowlandsDecoration** adds six separate dressing designs: upright, swept and dry grass, bank reeds, field weeds and a collapsed crate. Each uses 2–5 colors at 16 PPU. The palette contains eight new tile pieces and direct references to nine Tutorial pieces for flowers, a small tuft, leaves, wood chips, split logs and a low carved stone. Six regional visual prefabs and the original Tutorial bucket support independent placement. All decoration is visual-only and walk-through; existing Ground and architecture remain unchanged. [Dressed countryside preview](Art/GreenLowlands/green-lowlands-decoration-detail.png) | [Native-scale pieces](Art/GreenLowlands/green-lowlands-decoration-comparison.png).

## Animated water

![Waterfall and landing foam](Art/Tutorial/Previews/waterfall-motion.gif)

Ripples, falling water and landing foam each use a four-frame Unity AnimatedTile. Ripples run at 4 fps; waterfall body and foam use a 1.25 multiplier for 5 fps. Repeating waterfall-body tiles connect a static lip to the animated landing. No custom runtime water-animation script is required.

## Assets and runtime

Python generators define ground, terrain and path pixels. Editor-only builders create the sliced sprites, Tile/RuleTile/AnimatedTile assets, palettes and prefab structures. Runtime scenes consume ordinary Unity assets; generation tools do not run in the game.

The Tutorial environment uses the [Hearth & Meadow palette](Art/Palettes/tutorial-hearth-and-meadow-v1.gpl). Source drawings and manifests live in [ArtSource](../ArtSource/README.md), with generated runtime assets under `Assets/Art/Tiles/Tutorial`.

[Environment presentation](Art/Tutorial/Previews/demo-tutorial.png) · [Village detail](Art/Tutorial/Previews/tutorial-decoration-village.png) · [Practice-area detail](Art/Tutorial/Previews/tutorial-decoration-practice.png)
