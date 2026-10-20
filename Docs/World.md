# World and tilemaps

[Vegetation](#interactive-vegetation) · [Tilemap composition](#tilemap-composition) · [Terrain rules](#ruletiles-and-terrain-connections) · [Lowlands art](#green-lowlands-art) · [Passages](#regional-passages) · [Rewards](#coins-tools-and-rewards) · [Asset pipeline](#production-asset-pipeline)

[Systems overview](SystemsOverview.md) · [Player animation](PlayerAnimation.md)

Lowlands meadow overscan uses `MeadowBackdrop`: a grass brush and rectangular extent generate a temporary native Tilemap in Edit and Play Mode. This preserves RuleTile variation and camera coverage while avoiding thousands of repeated scene records. The generated background has no collider and is excluded from vegetation placement and shadow receiver masks; edit the component bounds instead of its temporary cells.

Tutorial moves from a sheltered village and practice terrace through woodland, a river crossing and old stone ruins. The world uses native 16 x 16 tiles at 16 pixels per unit, with muted green foliage, warm earth and timber, and restrained surface detail.

The Tutorial combines this environment with its full teaching route. Gameplay objects (axe pickup, target stands, practice dummy, Recall stone and seal gate, wolves, Sun Shard and bonfire) are prefab instances placed over the tilemaps; the [systems overview](SystemsOverview.md#tutorial) describes how they work. Static ruin artwork alone does not imply an interaction. The Recall stone has a thin dark contour and a brief cyan glimmer across its carving when the tutorial is ready for the player to strike it. The cue pauses during combat and ends when awakening begins. Its exit connects to the Green Lowlands arrival pocket.

Tutorial now reuses current terrain and tree materials across the whole map, including its painted forest backdrop. The northern waterfall has joined stone cliffs, while the practice terrace retains a shallower earth face. Interactive grass uses the shorter upright Lowlands blades with the same 967 plant placements and original cutting/motion behavior. The village houses and workyard woodshed have thicker roof edges, projecting entries and recessed openings, using the shared palette on their original native canvases. Bridge, ruins, hedges and teaching props use the same restrained materials; the well, fences and bonfire base reuse fitting regional art. Gameplay, collision, the accepted flame and small decoration are preserved.

For Tutorial authoring, use the `TutorialGroundPaths`, `TutorialTerrain` and `TutorialEnvironment` palettes. Regional buildings live in `Assets/Prefabs/Environment/Tutorial`; functional targets and the dummy live in `Assets/Prefabs/Tutorial`. Common trees, fences and props live in `Assets/Prefabs/Environment/Shared`. **Road of the Old King > Advanced > Asset Maintenance > Tutorial > Refresh Painting Palettes** refreshes these references without changing placed scenes.

Tutorial, Green Lowlands and Barrow Approach share `OutdoorPalette`, a shared URP profile with contrast, saturation, daylight warmth and ACES tonemapping after world rendering. Source sprite palettes and baked shadows stay intact. Post-processing preserves crisp pixel edges, with blur, bloom and antialiasing disabled. Screen UI and the dark storehouse remain ungraded. Tune the shared profile in `Assets/Settings/Rendering`; each outdoor scene owns its Volume and camera setting. Browser performance for this finish remains unverified.

## Interactive vegetation

Tutorial and Green Lowlands meadows now contain grass, flowers and occasional waterside reeds that sway, bend away from the player's feet and settle after passage. Actual axe swings, throws and Recall cut them with spinning airborne leaf fragments and a burst of green motes. Fragments arc, drift, bounce lightly and fade; their ground position controls overlap with the player. These smooth effects intentionally extend beyond the pixel constraint and use a bounded pool with no individual particle objects or physics. Cut plants stay cleared through ordinary building visits and return on rest/death or a fresh session; they do not block movement. Existing painted decoration remains static.

Grass uses two separate leaf sprites with staggered wind timing, each rotating smoothly around its bottom pivot. The leaves remain intact as they move: no row deformation, stepped animation or UV distortion. This is an intentional modern treatment of pixel art. Tutorial and Lowlands share 16/12-pixel grass leaves with upright resting angles and 3-4 shared sage colors per blade. Both retain native 16 PPU; flowers and reeds retain their existing artwork as intact bottom-pivoted sprites. Tutorial's editable density map places 967 plants in patches, excluding dirt, paths, water, solid footprints and interaction clearings.

Grass depth follows its stationary roots and the player's feet: nearby plants south of the feet draw over the player; plants north of the feet draw behind. The player wins equal-height ties. Ground and foreground draws share each chunk's mesh and cut indices, and only nearby chunks enable the extra draw. This keeps distant grass under existing props and overheads without sorting every plant on the CPU.

One field owns small spatial mesh groups, with continuous shader rotation and no per-plant objects or colliders. Cutting queries nearby groups during the weapon's real damage interval and removes their triangle indices; melee respects solid terrain. Per-plant root/phase data follows [GPU Gems' grass animation approach](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-7-rendering-countless-blades-waving-grass); local groups balance draw calls against the shared culling described in [Unity's mesh combination guidance](https://docs.unity3d.com/6000.3/Documentation/Manual/combining-meshes.html).

Green Lowlands has its own separately baked field, with patchy meadow edges and sparse combat interiors. Paths, water, machinery and interaction clearings remain open. The baker excludes structural artwork and each leaf's full sway envelope; static foliage and props use the current terrain layout and meadow dressing. Its editable density compiles to culled mesh groups using the same current atlas as Tutorial, with unchanged copies of the accepted flowers/reeds. The grass artwork refresh retains its motion and cutting behavior; terrain rebuilds rebake the planted roots. A configurable ground order places the field above each scene's terrain without changing player-relative depth. Native leaf-litter clusters add woodland detail.

Automated checks cover both fields, real cutting/depth/debris, regional bonfire regrowth and unchanged route access; human density review and browser performance remain pending.

## Tilemap composition

Visual layers and physical boundaries are independent. A water or cliff tile does not automatically become an obstacle; a separate collision map defines where the player can travel.

Tutorial's forest scenery extends beyond the playable area to cover the widest 21:9 camera view and full look-around offset. The northern trail continues beyond the transition point without exposing the scenery edge. Buildings, trees, ruins and cliff feet share short ground shadows with a consistent light direction. Overlaps stay restrained, and water, cliff faces and roofs keep their separate surface treatment. The seal gate shadow fades as the gate lowers and remains hidden when progress restores it open.

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

Trees and buildings use separate base, overhead and collision elements. Trunk-sized footprints allow movement behind canopies, while solid lower building footprints remain blocked. Independent object instances preserve overlapping silhouettes. Elevation is visual: ramps and terraces share the same 2D physics plane. Green Lowlands uses joined terrain and spaced trees for its rebuilt boundaries, with native stone steps marking the orchard ascent. Prop feet are checked against painted cliff faces.

The regional elevation contract in [ElevationLayout.json](../ArtSource/GreenLowlands/ElevationLayout.json) names the farm, ridge and basin tiers and their stair landings. The layout fixture closes stair regions in its collision graph to detect unintended connections between different heights. The regional rebuild joins banks, replaces dense hedge walls with terrain and trees, gives the small island a turf-and-paving top, and extends the background meadow. Stairs preserve their native light direction and route gates. Broader terrain/building composition and human readability review remain unfinished.

The rebuilt southern approach uses joined rocky woodland around dry, clear tree bases. One RuleTile map owns the joined river, removing opposing banks at the former map seam. The road continues behind the unchanged return exit so the approach remains visible beyond the transition.

New and revised terrain, foliage and architecture share a 32-color environment palette, with fixed pickup accents and per-piece limits. Existing decoration and compatible natural scenery retain their artwork and colors, so this source-art contract does not describe every displayed pixel. The earlier connected foliage prototype remains reproducible, but its regional wall placement has been removed.

Regional boundaries use connected earth or stone masses with supported tree roots and clear routes through the village, pasture, woodland hollows and orchard. The river remains permanent; the basin and its worksite approach open through the sluice. The current [three-tier layout](#green-lowlands-art) supersede the earlier staged terrain studies. The Barrow entrance is a Lower passage beneath a carved header, not a stair onto the Upper ridge.

The waterworks southeast wall has a connected one-tier stone footing against the earth bank. At Barrow, the east arch pier joins the worksite retaining wall; obsolete bank strips and the detached fragment below the western shoulder are removed. The eight established stairs, reward ledges, rope crossing and progression routes are retained; a ninth short stair now connects the drained road to the Barrow doorway.

The farmstead, shed and mill, village barn, sluice houses and storehouse arch now share the Tutorial's thick eaves, recessed openings and bevelled foundations while keeping regional slate/timber/stone materials. The farm buildings and storehouse retain their original footprints, lower/overhead slices and collision. The mill's open wheel exposes actual river water; the sluice rooms use the enclosure and cutaway described below. Sources, exact prompts and native registration are retained in [the building recipe](../ArtSource/GreenLowlands/Environment/Buildings/NativeRecipe.json).

The courtyard props now share that material treatment: a roofless windlass well, empty cart, simple fences and stone remnants. The plank bridge retains both walking lanes and its separate rails; regional fence sections meet at their existing posts.

The region's trees use separate outlined art: mixed oak/birch groves, chalk-barked birches and golden-fruited orchard trees. All retain their original 48x64 canvas, placement, trunk collision and overhead rows; Tutorial sources remain unchanged. The three species use 8/8/7 shared colors. Existing decoration and compatible natural scenery are retained. The traveling trader now uses the user-selected PixelLab merchant at the original interaction point, with unchanged stock and player-relative depth. Its eight-direction standing, breathing-idle and walking sets are imported; the trader wanders near the bonfire and stops to talk. The region uses the coordinated environmental shadows described below.

One shared terrain set supplies water, banks, stairs and reusable cliff cells. Green Lowlands uses separate top/face Tilemaps for Farm (0), Ridge (+1) and Enclosure (+2). The small `TerrainRuleTile` extension joins earth/stone by Top or Face part within one map; changing material does not introduce a cliff. Higher walls cover the filled lower top beneath them. Local edges/corners update while painting, with no generated cliff PNGs or rebuild command. Collision and elevation guides remain separate. **Shared > Refresh Terrain Assets** maintains art/palettes; Scene Tools updates vegetation, shadows and basin drainage. Human review of the rebuilt terrain remains pending.

**Pickup presentation:** hearts hover with a double beat, Sun Shards float slowly, and coins hop on reveal and occasionally turn. Native chests and tools stay grounded with brief glints; the nearest eligible F target receives stronger attention. Successful collections give a short world-space payoff while rewards save immediately. Physical anchors, range and loot rolls remain unchanged, and dark interiors still control visibility.

**Scene authoring:** the Lowlands hierarchy separates regional surfaces, landforms, manual cliffs/banks, stairs/supports, stonework and ground detail. Obsolete empty layers are removed; compatible worksite paving and basin floor detail are consolidated. Collision owners, changing water groups, resolved tile art and object sorting are preserved; decoration retains its existing assets and palette.

**Environmental shadows:** buildings, all 108 trees, the two bushes, retained scenery, the scattered stones and logs, and the trader share short lower-right casts with darker foot contacts; the trader's shadow moves with him as he walks. Static overlaps merge at maximum opacity. Prop casts stay on their supporting surface; terrain casts originate only from explicit faces onto lower receivers, with higher faces/tops occluding lower casts. Flat shores and same-height material changes contribute no cliff shadow. Basin contacts sit below animated water and emerge during drainage. Pot shadows disappear with their breakables, while fitting shadows follow their assemblies. Native 16-PPU masks add no realtime lighting scripts; update shadows after moving scenery or terrain, then save normally.

Farm shores meet high water flush. Their opaque directional banks and wet stairs sit beneath the changing water and emerge during drainage; Ridge faces remain above it. Seven reusable bank pieces share the native cliff atlas. Two sparse small shrubs beside the approach use compact solid bases, player-relative crown depth and coordinated contact shadows.

The pond recedes over eight seconds according to an authored bed-depth field, exposing shallow edges before deeper channels. Thin waterline marks and quieter cool silt distinguish the emerging basin; its barrier remains until the transition finishes. Permanent water is separate. This is a shader-driven presentation over the existing progression system, not a fluid simulation.

The storehouse now has a recessed doorway and matching interior threshold. Walking inward commits entry; brushing sideways, standing still or being pushed into the trigger does not. Arrivals face into the room or back toward the exterior approach, clear of the return trigger. The shared fade, carried vitals, cleared-enemy/vegetation visit state and checkpoint remain intact. Connected masonry replaces the interior's repeated wall junctions; deeper rooms retain lantern-dependent darkness. Geometry and actual motor crossings pass focused checks; this is the first rebuild slice, not the completed region.

Green Lowlands groups authored objects under `Green Lowlands`: **Collision**, **Ground and Paths**, **Terrain**, **Scenery**, **Gameplay**, **Encounters** and **Authoring**. Terrain collision maps are together in Collision; prop colliders remain in their prefab instances, and mechanism colliders stay with their machinery. `Collision/Pond high water` owns both temporary flooded boundaries and their changing artwork. Hidden vegetation masks and editor markers live in Authoring. Player and Game Session remain scene roots; Cameras and Lighting have separate root folders.

## RuleTiles and terrain connections

Unity RuleTiles inspect neighboring cells and choose the matching edge, corner or junction sprite. Separate material maps allow paths and ground treatments to overlap without replacing the underlying surface.

| Asset group | Contents |
| --- | --- |
| Shared Ground | Seven surface materials and their RuleTile brushes |
| Shared Terrain | Banks, water, cell-sized cliff parts, retained legacy cliffs, walls, stairs and joins |
| Shared Paths | Dirt lanes and footpaths; paving reuses ground cobbles |
| Environment | Shared trees, rocks, fences and ruins; regional building prefabs |
| Detail / Decoration | Retained regional dressing atlases and reusable shared prop prefabs |

Five terrain materials have all 47 valid edge/corner combinations; low walls use 16 cardinal connections. Taller cliff faces and stairs combine repeatable pieces. Atlas borders include pixel extrusion to prevent neighboring sprites bleeding into tile edges.

## Green Lowlands art

Green Lowlands has three walkable terrain tiers: Farm 0, Ridge +1 and Basin -1. Scene-only elevation and transition Tilemaps record the implemented grid; the [elevation data](../ArtSource/GreenLowlands/ElevationLayout.json) records its floor surfaces and nine stair connections. **Road of the Old King > Height Brush** paints a separate editor-only height plan directly over the Scene grid, with brush size, erase, whole-stroke Undo and an adjustable overlay. That plan saves with the scene and records intended changes for later terrain construction; it does not immediately change art or collision. Unwalkable enclosure remains Structure in the implementation guide, while its visible top/face maps specify its actual height: southern rock above Farm uses +1, northern rock above Ridge uses +2. One elevation level has exactly one 16-pixel front row; a two-level drop has two. Side edges are four pixels wide and rear rims two, with matching static collision. The nine stairs use one tread row per level; walking transitions use those connections. Material, water state, rendering and collision remain separate concerns.

Terrain painting uses ordinary Tilemaps; the former bounded generator is retired. Farm faces/tops draw at Ground 43/44, changing water at 45, Ridge at 46/47 and Enclosure at 48/49. The shared 234-sprite atlas contains native top/face variants, repeatable wall and single-row stair pieces, retained banks and seven collision shapes. Water coverage and gated collision follow the reconstructed basin. Current player-motor checks cover both directions of all nine stairs and eight blocked cliff approaches; human art/navigation review remains separate.

Shared ground, paths and terrain use a sage meadow palette, quiet ochre routes, broad cliff shading and distinct near/far water-bank edges adapted from the presentation study. Material margins use restrained pixel clusters; cliff corners have one-pixel chamfers and bedrock favors plain cells over repeated marks. These shared ground/path changes apply to Tutorial and Lowlands. The native 16-PPU assets retain existing tile connections, sprite IDs and authored terrain heights. Tutorial and Green Lowlands reuse these materials and suitable trees/rocks; each region keeps its buildings, terrain variants and scene composition. Regional visual identity and architectural depth still need review.

The basin reuses regional WornStone and Earth brushes for sparse broken paving and deposits beneath the changing water. The two visual-only maps preserve the permanent river and collision. Paired carriage rails, ties, worn stops and a distributor sleeve use three existing Field and Old Road colors, with edges registered to the native pixel grid. Development labels are inactive and tagged EditorOnly; their named marker parents remain available for authoring.

The sluice now has enclosed rooms beneath its south-facing gables: plaster and stone back walls, solid wall returns, recessed machine beds, timber jambs and clear thresholds. Exterior stone paving meets quieter indoor boards at the doorways. The main roof is shortened at native pixel scale to clear the approach, and its facade meets the room floor; a shallow culvert fascia replaces the misleading basement-height face.

The same-scene cutaway fades roofs/front walls and reveals static interior art as the player enters, preventing machinery from showing through the closed shell. The shared camera, targets, throwing apertures and carriage travel retain their contracts. Physical timber gates control the dry service passage and main exit; each slides into the wall after its corresponding water channel or pond finishes draining, clearing collision when fully retracted. Water stays in the bank channel, intake and outlet. The separate exterior lifting sluice gate and tailrace remain visible, with their motion and flow following puzzle state. Architecture, clue readability and the full traversal still need human review.

A provisional worn stone relief on the entrance masonry suggests the general Recall rule through an axe, stone and wheel motif. It contains no text, numbered steps or solution path. The 64x32 sprite uses six colors at 16 PPU and adds no collision or interaction. generated source, exact prompt and native sampling recipe are recorded in [art provenance](../ArtSource/GreenLowlands/Provenance.json). Human clue interpretation remains to be reviewed.

Shared chests and breakable pots use the accepted outlined timber/bronze and clay artwork. Three chest poses share a stationary base; a brief lid opening accompanies the immediate reward, while reloading shows its final open state. Fixed ground anchors control player overlap independently of animation. The shared interaction atlas uses 16 PPU, Point filtering, binary alpha and unit-scale placement. Sources and historical imports are recorded in [art provenance](../ArtSource/GreenLowlands/Provenance.json).

 Existing pot collision, drop behavior and chest access gates are preserved.

Shared pickup prefabs now use an asymmetric amber Sun Shard, round bronze coin and cracked red heart fragment. Each uses 4–5 colors at native 16 PPU, with crisp alpha and no baked glow. Tutorial and Green Lowlands share the same shard; availability, collection and saved exhaustion retain their existing behavior. A traveling trader and inventory now present the saved economy.

Stone-and-bronze rope anchors, a shallow hemp span and a weathered smiths' carving also use native 16-PPU art with 4–6 colors per piece. The maintenance pickup reuses the bronze sluice fitting. Existing traversal, reading, collection and save behavior is preserved.

The **GreenLowlands** scene connects to Tutorial through a winding southern road with two separated, easier instances of the existing wolf before the regional bonfire. This road is part of the same scene; entering carries current health/flasks and does not rest the player. The scene combines the original farm artwork with a larger Sleeping River blockout. The barn now stands on the widened pasture approach. It includes the shared player/HUD, session and follow camera, plus a Lowlands Bonfire for rest, travel and respawn. The regional fire offers the first upgrade tier. The farm's former rectangular boundary is being integrated into the regional landscape.

 Automated checks cover route movement, both exit triggers, rest/travel, checkpoint respawn and Continue across a fresh session. A human keyboard/full Tutorial playthrough and browser test remain pending.

All bonfires use the shared prefab's gray-sage stone ring with charred logs and accepted four-frame flame at 6 fps. The base uses eight shared environment colors, a bottom-aligned 28x15 drawing in a 32x32 sprite at 16 PPU. Both the flame and defeated enemy sprites render below the player. The flame's 16x24 frames use five colors, binary alpha and native 16 PPU. `BonfireSpriteView` displays the flame only after the fire is discovered; the existing Bonfire and checkpoint systems own rest, travel and saving. Future custom bonfires should inherit this prefab and override their art.

The first regional Ground kit is available as **Ground_Paint** and **Ground_IndividualPieces**: 308 native tiles and seven RuleTile brushes. The regional drawings use meadow clusters, sun-dried grass, compacted soil with grit, embedded stones and wear marks, silt pockets, buried slabs, rough fieldstones and broad plough furrows. They retain Tutorial pixel scale, clean transition silhouettes and one-pixel material rims, with distinct regional texture designs. The original kit used the [Field and Old Road palette](Art/Palettes/green-lowlands-field-and-old-road-v1.gpl); current exports use the [shared zone palette](../ArtSource/Shared/Palettes/Meadow.json).

The shared Terrain set adds meadow and stone banks, water, earth terraces, stone shelves and stairs, soil ramps and low walls. **Terrain_Paint**, **Terrain_Structures** and **Terrain_IndividualPieces** cover its base pieces; the cliff and join palettes extend the same set. Current exports use the shared palette. Scene templates retain separate collision maps.

**Paths** supplies broad dirt lanes and narrow field tracks: 100 new native tiles, two RuleTiles and direct references to Ground's fieldstones and buried slabs. Lanes use four colors and trails three, matching the regional soil. The palette uses the existing three Paths maps.

**GreenLowlandsEnvironment** contains 23 visual stamps: nine regional building/prop families and fourteen reused objects. Regional prefabs live in `Assets/Prefabs/Environment/GreenLowlands`; shared trees and other common objects live in `Assets/Prefabs/Environment/Shared`. Complete objects retain base/overhead and collision maps. The buildings use broad timber/slate/stone forms; the bridge preserves its two walking rows, and the mill remains a static exterior. **Road of the Old King > Advanced > Asset Maintenance > Green Lowlands > Refresh Painting Palette** refreshes these references without repainting scenes. [Source provenance](../ArtSource/GreenLowlands/Provenance.json).

**FieldDetails** adds upright, swept and dry grass, bank reeds, field weeds and a collapsed crate, alongside directly reused **MeadowDetails** accents. Both painting libraries, plus **AncientDetails**, live under `Assets/Art/Tiles/Shared/DetailDecoration`. Reusable visual prefabs, including the bucket, live in `Assets/Prefabs/Decoration/Shared`. This decoration is visual-only and walk-through; functional pots and chests use the shared interactive prefabs instead.

## Sleeping River blockout

The authoritative [GreenLowlands scene](../RoadOfTheOldKing/Assets/Scenes/GreenLowlands.unity) now extends through a broad pasture, winding field lanes, two connected routes around an eastern copse, an orchard terrace and a winding high route to the working two-bay sluice. Its bent reservoir has coves, a reed island and residual pools. Lowering the pond permanently opens the basin, a shorter return beside the mill and the northern road-cut; the raised water over the southern river also recedes, leaving a narrow permanent channel with exposed dirt banks. Regional ground, banks, lanes, water and basin soil combine with reused oak/birch/apple art, connected hedges, weathered fences and boulders. Native cliff faces establish the terrain heights; the rebuilt sluice surrounds its original mechanism with the cutaway architecture described above. Artwork uses native 16 PPU, Point filtering and restrained palettes. Design labels remain hidden authoring aids and grant nothing.

Automated real-collider checks cover access in all three water states. Isolated live checks exercise the translated axe/carriage mechanism, contextual controls, actual player-motor traversal, saving, rest and checkpoint reload. Geometry checks also reserve clear fighting footprints and reachable viewpoints toward future rewards/connections. Human clue/aiming/pacing review and browser performance remain pending. The regional sluice now requires a maintenance crank collected in its courtyard. Animated drainage and the first separate dark interior with two-way travel are implemented. The road and storehouse now connect to a small Barrow approach scene; the full Barrow and Highlands regions remain unfinished.

The expanded core spans roughly 162×105 world units including the farm, with the southern connecting road continuing beyond its return transition. Wooded-bank collision divides the previous open field into winding lanes, branches and combat clearings. The expanded southeast loop surrounds a wooded bank and rope-gated bandit camp, then reconnects before the orchard climb; the high ridge bends past pond viewpoints. A distant entrance to the upper spur gives the nearby visible reward a substantially longer walking route. Prop placements and surrounding clearings preserve the routes, fighting spaces and reward viewpoints. The high-water art shares a continuous shoreline with the permanent river; draining reveals basin soil and the dirt banks of the remaining low river. Puzzle rooms retain their local dimensions; native sprite scale is unchanged; the southeast camp uses a nine-unit rope span. A saved exact position obstructed by revised terrain falls back to normal arrival/checkpoint footing while retaining saved vitals.

## Regional passages

`BarrowApproach` is a small playable connector reached through the drained Lowlands road and the storehouse's Old Drain. Paired named arrivals preserve the existing transition, carried-vitals and checkpoint rules. Its side court has a persistent reward chest and a gate operated from the court side. The full Barrow region, lantern acquisition and vault remain future work.

`PermanentPassage` owns local, one-time gates and bridges. It requires a nearby living player with clear access, records a stable progress ID on interaction, holds collision through the opening animation and restores an open passage immediately on load. The High Pass crossing uses the same component, with its winch on the far platform and a persistent smithing inscription nearby. That platform has an authored future Highlands arrival; no Highlands scene or transition is implemented yet.

Barrow Approach reuses current native terrain, trees and masonry, the outdoor grade and the coordinated shadow baker. Dev travel discovers its arrivals from the enabled scene catalogue.

## Coins, tools and rewards

The regional blockout has a guaranteed-coin practice pot, four ordinary pots, two loose coin sources, three fixed-reward chests and exposed shard/fragment pickups. The separate storehouse adds three ordinary pots. Pots roll once per save, including empty results; they remain broken, and uncollected coins reappear after reload. Six bounded ceramic flecks accompany a break. Coins collect by proximity through clear physical access.

The traveling trader sells a permanent rope kit for 30 Bronze Coins and one heart fragment for 60. Purchases save the debit, reward and finite stock together. Tab opens a paused inventory of owned equipment, tools and progression. The kit now secures a persistent two-way rope from (110.5,45.5) to the southeast bandit camp at (119.5,45.5). Both ends use F; repeated trips cost nothing. Combat/action restrictions and body/landing checks protect traversal, and a mid-climb resume stores departure footing. Three orchard Scavengers and two camp Scavengers now unlock their respective rewards through actual defeat events; rest revives the guards without relocking earned access. Rope anchors and the secured span have native art; the trader walks a small patch by the bonfire with the user's PixelLab idle and walk, greets passers-by and talks before trading, while climbing animation remains provisional.

**Meadow dressing:** the region has about 500 ground pieces and 70 stones/logs placed in the 17 Oct pass. Those results are ordinary editable scene content; the one-time scatter script is retired. Use **Road of the Old King > Decoration Brush** for local prefab additions with preview, spacing, exclusion maps and Undo. Use Unity's Tile Palette for tile decoration, then update vegetation and shadows through **Scene Tools**.

Save v3 retains coins, tools and source results; older saves migrate with prior progress intact. Pots, coins, chests and the maintenance tool use native art; the two stock markers are hidden behind one F trader interaction. Purchases require a confirmation that defaults to Back; bought stock leaves the list, and an emptied trader explains that nothing remains to sell. Sixteen authored vegetation patches and eligible regional enemies now supply intermittent coin drops. The two planned wolf pockets, further character animations and human economy/pacing review remain pending.

## Readable inscriptions

The regional blockout includes a reusable inscription interaction with a native weathered smiths' carving and draft text. Carvings remain in the world; knowledge persists through rest, death and reload. Reading uses a framed, paused menu with Escape/Enter/Leave controls. Tab shows forge lore only after discovery. Physical access and the implemented workshop encounter gate its carving. A second carving sits in the dark interior, with its own text and the same persistent knowledge/reader behavior. A third authored site occupies the far-side High Pass shelter; its future Highlands approach, the remaining sites and the final forge upgrade remain future work. The first dark interior masks world rendering with wall-blocked lantern visibility; shared interactions require actual light on the object.

## Sluice mechanism prototype

The separate [LowlandsSluiceProof scene](../RoadOfTheOldKing/Assets/Scenes/Development/LowlandsSluiceProof.unity) contains a playable two-bay Recall puzzle with placeholder geometry. Open it from stopped Edit Mode and Play. It uses the standard player, axe, HUD and bonfire, with its own prototype save and no legacy migration. It is excluded from the normal build and has no production-scene exit.

The first bay requires lodging the axe through a narrow slot, repositioning and recalling through a concealed vane. Turning the released crank drains the service passage. In the second bay, the crank carries the embedded axe to a new position; its return must strike the brake and catch vanes in that order within the same flight. The controller binds a confirmed lodging to the owned axe and its flight sequence, so failed attempts and unrelated later throws cannot accumulate a solution. Permanent milestones restore the service passage and low-water collision after rest, reload and respawn. Drainage is an immediate geometry swap in this prototype.

`RewardChest` uses F and the reward service to grant fixed shard, fragment, coin or tool contents. It checks distance and intervening solids, and derives its persistent open state from the awarded reward ID. The proof contains one shard chest behind the drained passage. The shared chest prefab includes the accepted chest art and lid animation; pots likewise include their shared breakable visual. Audio remains pending. Automated checks exercise real weapon sweeps and saved-state lifecycles; human clue/aiming review and browser testing remain pending.

## Animated water

Ripples, falling water and landing foam each use a four-frame Unity AnimatedTile. Ripples run at 4 fps; waterfall body and foam use a 1.25 multiplier for 5 fps. Repeating waterfall-body tiles connect a static lip to the animated landing. No custom runtime water-animation script is required.

## Assets and runtime

### Production asset pipeline

Python generators define ground, terrain and path pixels. Editor-only builders create the sliced sprites, Tile/RuleTile/AnimatedTile assets, palettes and prefab structures. Runtime scenes consume ordinary Unity assets; generation tools do not run in the game.

Environment exports use the shared [Meadow palette](../ArtSource/Shared/Palettes/Meadow.json) and the regional building recipes in [ArtSource](../ArtSource/README.md). The original [Hearth & Meadow palette](Art/Palettes/tutorial-hearth-and-meadow-v1.gpl) describes retained older pieces. Complete objects are independent prefabs; palette refreshes reference their existing art. Shared ground, terrain, paths and trees live under `Assets/Art/Tiles/Shared`; region folders own regional variants and buildings. Scene-derived outputs live under `Assets/Art/Generated`.

Common interactive prefabs live in `Assets/Prefabs/World/Shared`: `Bonfire`, `ClayPot`, `RewardChest`, pickups and traversal objects. One bonfire owns the accepted stone base, animated flame, smoke and gameplay component. Scene instances retain their own checkpoint identity and placement; future custom art should use a variant of this prefab. The shared folders support reuse across regions without dictating regional composition or future palettes.
