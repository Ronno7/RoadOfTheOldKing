# World and tilemaps

[Systems overview](SystemsOverview.md) · [Player animation](PlayerAnimation.md)

Tutorial moves from a sheltered village and practice terrace through woodland, a river crossing and old stone ruins. The world uses native 16 x 16 tiles at 16 pixels per unit, with muted green foliage, warm earth and timber, and restrained surface detail.

![Tutorial world overview](Art/Tutorial/Previews/tutorial-populated.png)

[Village and work yard](Art/Tutorial/Previews/tutorial-populated-home.png) · [Practice terrace](Art/Tutorial/Previews/tutorial-populated-practice.png) · [Forest road](Art/Tutorial/Previews/tutorial-forest-road.png) · [Ruined courtyard](Art/Tutorial/Previews/tutorial-recall-courtyard.png)

The Tutorial combines this environment with its full teaching route. Gameplay objects (axe pickup, target stands, practice dummy, Recall stone and seal gate, wolves, Sun Shard and bonfire) are prefab instances placed over the tilemaps; the [systems overview](SystemsOverview.md#tutorial) describes how they work. Static ruin artwork alone does not imply an interaction. Its exit connects to the Green Lowlands arrival pocket.

## Interactive vegetation

Tutorial and Green Lowlands meadows now contain grass, flowers and occasional waterside reeds that sway, bend away from the player's feet and settle after passage. Actual axe swings, throws and Recall cut them with spinning airborne leaf fragments and a burst of green motes. Fragments arc, drift, bounce lightly and fade; their ground position controls overlap with the player. These smooth effects intentionally extend beyond the pixel constraint and use a bounded pool with no individual particle objects or physics. Cut plants return on rest or scene reload; they do not affect movement, combat rewards or saved progression. Existing painted decoration remains static.

Grass uses two separate leaf sprites with staggered wind timing, each rotating smoothly around its bottom pivot. The leaves remain intact as they move: no row deformation, stepped animation or UV distortion. This is an intentional modern treatment of pixel art. New 18/14-pixel grass leaves use four Tutorial greens at native 16 PPU; flowers and reeds retain their existing artwork as intact bottom-pivoted sprites. Tutorial's editable density map places 967 plants in patches, excluding dirt, paths, water, solid footprints and interaction clearings.

Grass depth follows its stationary roots and the player's feet: nearby plants south of the feet draw over the player; plants north of the feet draw behind. The player wins equal-height ties. Ground and foreground draws share each chunk's mesh and cut indices, and only nearby chunks enable the extra draw. This keeps distant grass under existing props and overheads without sorting every plant on the CPU.

One field owns small spatial mesh groups, with continuous shader rotation and no per-plant objects or colliders. Cutting queries nearby groups during the weapon's real damage interval and removes their triangle indices; melee respects solid terrain. Per-plant root/phase data follows [GPU Gems' grass animation approach](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-7-rendering-countless-blades-waving-grass); local groups balance draw calls against the shared culling described in [Unity's mesh combination guidance](https://docs.unity3d.com/6000.3/Documentation/Manual/combining-meshes.html).

[Motion preview](Art/Tutorial/Previews/tutorial-interactive-vegetation.gif) (768x480 renders at 30 fps; staged player positions and simulation steps, not a recorded keyboard playthrough). Green Lowlands adds a separate 1,726-plant field: 1,627 grass clumps, 96 flowers and three reeds, with patchy meadow edges and sparse combat interiors. Paths, water, machinery and interaction clearings remain open; original static decoration is preserved. Its editable density compiles to 108 culled mesh chunks and shares the accepted atlas. A configurable ground order places the field above each scene's terrain without changing player-relative depth. Sixteen native leaf-litter clusters add woodland detail.

[Green Lowlands vegetation and cutting](Art/GreenLowlands/green-lowlands-vegetation.png) shows four regional views and a closer before/after pair from an actual axe swing. Camera/player positions were staged. Automated checks cover both fields, real cutting/depth/debris, regional bonfire regrowth and unchanged route access; human density review and browser performance remain pending.

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

Trees and buildings use separate base, overhead and collision elements. Trunk-sized footprints allow movement behind canopies, while solid lower building footprints remain blocked. Independent object instances preserve overlapping silhouettes. Elevation is visual: ramps and terraces share the same 2D physics plane. Green Lowlands distinguishes flat woodland barriers with hedgerow foliage, reserves cliff faces for height changes, and marks the orchard ascent with native stone steps. Prop feet are checked against painted cliff faces. [Current terrain views](Art/GreenLowlands/green-lowlands-height-review.png) show staged Edit Mode camera/player positions. Live movement tests are separate.

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

Regional chests and breakable pots use native timber/bronze and clay artwork. Three chest poses share a stationary base; a brief lid opening accompanies the immediate reward, while reloading shows its final open state. Fixed ground anchors control player overlap independently of animation. The 16-PPU atlas uses six colors total, 5–6 per piece, with Point filtering, binary alpha and unit-scale placement. Sources and import recipe are in [art provenance](../ArtSource/GreenLowlands/Provenance.json).

[Reward-prop views](Art/GreenLowlands/green-lowlands-reward-props.png) show staged live closed/half/open poses, a farm pot and north/south player overlaps. Existing pot collision, drop behavior and chest access gates are preserved.

Shared pickup prefabs now use an asymmetric amber Sun Shard, round bronze coin and cracked red heart fragment. Each uses 4–5 colors at native 16 PPU, with crisp alpha and no baked glow. Tutorial and Green Lowlands share the same shard; availability, collection and saved exhaustion retain their existing behavior. [Pickup views](Art/GreenLowlands/green-lowlands-pickups.png) stage player positions, water states and a pot drop to show contrast at gameplay scale. A traveling trader and inventory now present the saved economy.

Stone-and-bronze rope anchors, a shallow hemp span and a weathered smiths' carving also use native 16-PPU art with 4–6 colors per piece. The maintenance pickup reuses the bronze sluice fitting. Existing traversal, reading, collection and save behavior is preserved. [Interaction views](Art/GreenLowlands/green-lowlands-traversal-props.png) show the rope before/after securing, the carving and the tool at gameplay scale.

The **GreenLowlands** scene connects to Tutorial through a winding southern road with two separated, easier instances of the existing wolf before the regional bonfire. This road is part of the same scene; entering carries current health/flasks and does not rest the player. The scene combines the original farm artwork with a larger Sleeping River blockout. The barn now stands on the widened pasture approach. It includes the shared player/HUD, session and follow camera, plus a Lowlands Bonfire for rest, travel and respawn. Upgrade purchases remain disabled at this fire. The farm's former rectangular boundary is being integrated into the regional landscape.

[Interactive arrival](Art/GreenLowlands/green-lowlands-interactive.png) | [Resting at the bonfire](Art/GreenLowlands/green-lowlands-bonfire.png) | [Initial art layout](Art/GreenLowlands/green-lowlands-integration.png) | [Player overlap checks](Art/GreenLowlands/green-lowlands-overlap-checks.png). The earlier layout/overlap captures used a temporary scale reference; the current scene contains the real player. Automated checks cover route movement, both exit triggers, rest/travel, checkpoint respawn and Continue across a fresh session. A human keyboard/full Tutorial playthrough and browser test remain pending.

All bonfires inherit a shared four-frame flame at 6 fps and a default stone fire ring. Regional variants can change the base while retaining the same flame. Both the flame and defeated enemy sprites render below the player. Its 16x24 frames use five colors, binary alpha and native 16 PPU. `BonfireSpriteView` displays the flame only after the fire is discovered; the existing Bonfire and checkpoint systems continue to own rest, travel and saving. Initial art production is complete through this interactive arrival. The broader region and Puzzle 1 now have playable placeholder geometry; final environmental/reward art, core regional encounters and the first upgrade remain later work.

The first regional Ground kit is available as **GreenLowlandsGround_Paint** and **GreenLowlandsGround_IndividualPieces**: 308 native tiles and seven RuleTile brushes. The regional drawings use meadow clusters, sun-dried grass, compacted soil with grit, embedded stones and wear marks, silt pockets, buried slabs, rough fieldstones and broad plough furrows. They retain Tutorial pixel scale, clean transition silhouettes and one-pixel material rims, with distinct regional texture designs. The [Field and Old Road palette](Art/Palettes/green-lowlands-field-and-old-road-v1.gpl) supplies all ground colors; the atlas uses 10 opaque colors, 3-4 per material.

[Country art preview](Art/GreenLowlands/green-lowlands-ground-detail.png) | [Side-by-side comparison](Art/GreenLowlands/green-lowlands-ground-comparison.png) | [Ground atlas](Art/GreenLowlands/green-lowlands-ground-atlas.png).

The Terrain kit adds clean meadow and stone banks, muted water, earth terraces, broad stone shelves and stairs, soil ramps and low walls. Its 315 native tiles, six RuleTiles and three water animations are available through **GreenLowlandsTerrain_Paint**, **GreenLowlandsTerrain_Structures** and **GreenLowlandsTerrain_IndividualPieces**. Terrain uses 14 palette colors in total, 2-8 per material family. Both regional kits use the existing blank zone template and separate collision map.

[Terrain preview](Art/GreenLowlands/green-lowlands-terrain-detail.png) | [Water motion](Art/GreenLowlands/green-lowlands-terrain-motion.gif) | [Terrain atlas](Art/GreenLowlands/green-lowlands-terrain-atlas.png). These individual-layer previews are offline compositions of exported tiles and unchanged Tutorial props/hero.

**GreenLowlandsPaths** supplies broad dirt lanes and narrow field tracks: 100 new native tiles, two RuleTiles and direct references to Ground's fieldstones and buried slabs. Lanes use four colors and trails three, matching the regional soil. The palette uses the existing three Paths maps. [Paths preview](Art/GreenLowlands/green-lowlands-paths-detail.png) | [Paths atlas](Art/GreenLowlands/green-lowlands-paths-atlas.png).

**GreenLowlandsEnvironment** combines 19 retained Tutorial nature/fencing/prop objects with nine regional prefabs: idle watermill, empty cart, plank bridge, old stone fence post, hipped-roof farmhouse, gambrel threshing barn, open drying shed, roofless windlass well and weathered fence. The architecture has original regional silhouettes while sharing Tutorial rendering style. The farmstead suggests abandonment a few generations ago: intact structures, closed openings, small wear and sparse moss/ivy. Individual pieces use 6–10 colors from the regional palette, with separate base, overhead and collision maps. The bridge has a two-cell deck; the mill is a static exterior. Tutorial originals remain unchanged. [Country preview](Art/GreenLowlands/green-lowlands-environment-detail.png) | [Native-scale comparison](Art/GreenLowlands/green-lowlands-environment-comparison.png) | [Sources and prompts](../ArtSource/GreenLowlands/Provenance.json).

**GreenLowlandsDecoration** adds six separate dressing designs: upright, swept and dry grass, bank reeds, field weeds and a collapsed crate. Each uses 2–5 colors at 16 PPU. The palette contains eight new tile pieces and direct references to nine Tutorial pieces for flowers, a small tuft, leaves, wood chips, split logs and a low carved stone. Six regional visual prefabs and the original Tutorial bucket support independent placement. All decoration is visual-only and walk-through; existing Ground and architecture remain unchanged. [Dressed countryside preview](Art/GreenLowlands/green-lowlands-decoration-detail.png) | [Native-scale pieces](Art/GreenLowlands/green-lowlands-decoration-comparison.png).

## Sleeping River blockout

The authoritative [GreenLowlands scene](../RoadOfTheOldKing/Assets/Scenes/GreenLowlands.unity) now extends through a broad pasture, winding field lanes, two connected routes around an eastern copse, an orchard terrace and a winding high route to the working two-bay sluice. Its bent reservoir has coves, a reed island and residual pools. Lowering the pond permanently opens the basin, a shorter return beside the mill and the northern road-cut; the main river stays wet. The first landscape pass applies the accepted regional ground, banks, lanes, water and basin soil, with reused oak/birch/apple clusters, hedges, weathered fences and boulders. Native earth/stone cliff faces now dress the outer terrain; the sluice has modest stone-and-bronze headwalls, tiled masonry, quiet paving and native fork/vane/crank sprites. New drawings use 4–9 colors per piece at 16 PPU with Point filtering and binary alpha; working collision and mechanism geometry are preserved. Design labels identify planned content and grant nothing; they are not final presentation.

Automated real-collider checks cover access in all three water states. Isolated live checks exercise the translated axe/carriage mechanism, contextual controls, actual player-motor traversal, saving, rest and checkpoint reload. Geometry checks also reserve clear fighting footprints and reachable viewpoints toward future rewards/connections. Human clue/aiming/pacing review and browser performance remain pending. The regional sluice now requires a maintenance crank collected in its courtyard. Further detail/interaction art, drainage animation, interiors and destination connections remain unfinished.

The expanded core spans roughly 162×105 world units including the farm, with a southern connecting road extending another 67 units. Wooded-bank collision divides the previous open field into winding lanes, branches and combat clearings. A narrow northern approach and wider southern hollow reconnect before the orchard climb; the high ridge bends past pond viewpoints. A distant entrance to the upper spur gives the nearby visible reward a substantially longer walking route. Added props occupy previously blocked terrain, preserving the routes, fighting spaces and reward viewpoints. The high-water art shares a continuous shoreline with the permanent river; draining hides the pond visual while exposing basin soil. Puzzle rooms retain their local dimensions; native sprite scale and rope crossing distance are unchanged. A saved exact position obstructed by revised terrain falls back to normal arrival/checkpoint footing while retaining saved vitals.

## Coins, tools and rewards

The regional blockout has a guaranteed-coin practice pot, four ordinary pots, two loose coin sources, three fixed-reward chests and exposed shard/fragment pickups. Pots roll once per save, including empty results; they remain broken, and uncollected coins reappear after reload. Six bounded ceramic flecks accompany a break. Coins collect by proximity through clear physical access.

The traveling trader sells a permanent rope kit for 30 Bronze Coins and one heart fragment for 60. Purchases save the debit, reward and finite stock together. Tab opens a paused inventory of owned equipment, tools and progression. The kit now secures a persistent two-way rope from (127,77) to the workshop landing at (131,77). Both ends use F; repeated trips cost nothing. Combat/action restrictions and body/landing checks protect traversal, and a mid-climb resume stores departure footing. Orchard/workshop gates have no guards assigned, so those rewards remain locked until encounters are supplied. Rope anchors and the secured span now have native art; the trader has a native static sprite and shared stock menu, while climbing animation remains provisional.

Save v3 retains coins, tools and source results; older saves migrate with prior progress intact. Pots, coins, chests and the maintenance tool use native art; the two stock markers are hidden behind one F trader interaction. Purchases require a confirmation that defaults to Back, and sold-out offers remain readable. Further intermittent sources, enemy/vegetation payouts, NPC animation and human economy/pacing review remain pending.

## Readable inscriptions

The regional blockout includes one reusable inscription interaction with a native weathered smiths? carving and draft text. Carvings remain in the world; knowledge persists through rest, death and reload. Reading uses a framed, paused menu with Escape/Enter/Leave controls. Tab shows forge lore only after discovery. Physical access and an encounter milestone gate the current carving; empty guard assignments keep it unavailable in normal play. Other sites, interior illumination and the final forge upgrade remain future work.

## Sluice mechanism prototype

The separate [LowlandsSluiceProof scene](../RoadOfTheOldKing/Assets/Scenes/LowlandsSluiceProof.unity) contains a playable two-bay Recall puzzle with placeholder geometry. Open it from stopped Edit Mode and Play. It uses the standard player, axe, HUD and bonfire, with its own prototype save and no legacy migration. It is excluded from the normal build and has no production-scene exit.

The first bay requires lodging the axe through a narrow slot, repositioning and recalling through a concealed vane. Turning the released crank drains the service passage. In the second bay, the crank carries the embedded axe to a new position; its return must strike the brake and catch vanes in that order within the same flight. The controller binds a confirmed lodging to the owned axe and its flight sequence, so failed attempts and unrelated later throws cannot accumulate a solution. Permanent milestones restore the service passage and low-water collision after rest, reload and respawn. Drainage is an immediate geometry swap in this prototype.

`RewardChest` uses F and the reward service to grant fixed shard, fragment, coin or tool contents. It checks distance and intervening solids, and derives its persistent open state from the awarded reward ID. The proof contains one shard chest behind the drained passage. Regional chests and owned-crank acquisition are now implemented; regional chest art and lid animation are implemented, while the proof keeps its fallback chest and audio remains pending. Automated checks exercise real weapon sweeps and saved-state lifecycles; human clue/aiming review and browser testing remain pending.

## Animated water

![Waterfall and landing foam](Art/Tutorial/Previews/waterfall-motion.gif)

Ripples, falling water and landing foam each use a four-frame Unity AnimatedTile. Ripples run at 4 fps; waterfall body and foam use a 1.25 multiplier for 5 fps. Repeating waterfall-body tiles connect a static lip to the animated landing. No custom runtime water-animation script is required.

## Assets and runtime

Python generators define ground, terrain and path pixels. Editor-only builders create the sliced sprites, Tile/RuleTile/AnimatedTile assets, palettes and prefab structures. Runtime scenes consume ordinary Unity assets; generation tools do not run in the game.

The Tutorial environment uses the [Hearth & Meadow palette](Art/Palettes/tutorial-hearth-and-meadow-v1.gpl). Source drawings and manifests live in [ArtSource](../ArtSource/README.md), with generated runtime assets under `Assets/Art/Tiles/Tutorial`.

[Environment presentation](Art/Tutorial/Previews/demo-tutorial.png) · [Village detail](Art/Tutorial/Previews/tutorial-decoration-village.png) · [Practice-area detail](Art/Tutorial/Previews/tutorial-decoration-practice.png)
