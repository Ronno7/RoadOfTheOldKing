# Tutorial Terrain 16

Terrain companion to **TutorialGround16**, using the same native 16x16 pixels and one-unit grid. Warm grass, earth and stone use Hearth & Meadow; water adds the approved Green Lowlands stream shades. Point filtering, full-rectangle sprites, uncompressed color and no mipmaps are configured.

## Start painting

1. Drag **../Templates/TutorialZoneTemplate.prefab** into your new scene. It includes the Ground kit and all eight zone categories. Unpack your own copy if you want to change its hierarchy.
2. Fill **Ground/Grass** using **TutorialGround_Paint** as before.
3. Choose **TutorialTerrain_Paint** in **Window > 2D > Tile Palette**. Its six top-row brushes, left to right, are **MeadowWater, StoneWater, DeepWater, GrassLedge, StoneLedge, StoneWall**. Select the matching Tilemap under **Terrain** before painting. Connections update automatically as you paint or erase.
4. Use **TutorialTerrain_Structures** for taller cliffs, stairs and ramps. Paint faces on **Terrain/CliffFaces**, steps on **Terrain/Stairs and Ramps**. The four 3x3 blocks are **EarthCliff, StoneCliff, StoneStairs, EarthRamp**, left to right; each block has Top/Middle/Foot rows and Left/Center/Right columns.
5. Paint the red collision brush on **Collision** for water, walls and inaccessible cliff boundaries. Leave stairways, ramps and gates clear. This reuses the Ground kit's collision asset. The renderer is hidden; the collider remains active.

Build your actual zone in your own scene. Kit rebuilds update art and palettes; they do not repaint DemoTutorial or your scene.

## Brush guide

| Brush | Use |
| --- | --- |
| MeadowWater | Ponds, streams and river shapes with a grassy lip and warm earth bank already attached. |
| StoneWater | Rock-edged pools or village channels. Keep separate from MeadowWater; different RuleTiles do not join each other. |
| DeepWater | Dark centers inside larger bodies of water. Paint over existing water and keep at least one cell away from the bank. This is visual depth, not swimming behavior. |
| GrassLedge | Raised meadow patches: a lit upper rim, thin side boundaries and a shaded south-facing drop. |
| StoneLedge | Low rock shelves and stone terraces, with the same complete connection set. |
| StoneWall | One-cell-thick low masonry: posts, ends, straights, four corners, four T junctions and a cross. |

The five water/ledge families each contain **47 eight-neighbor shapes plus three center variations**: convex and concave corners, combined inner corners, ends, isolated cells, narrow connectors, bends and junctions. Walls use all **16 cardinal shapes**; diagonal neighbors do not connect walls. Every direction has explicit artwork, so lighting is consistent without rotating sprites.

Water includes its bank inside the painted area. A single cell is a tiny puddle; use two or more cells for readable streams and three or more for a river. Keep the base Ground filled beneath transparent edges. DeepWater and water details layer above the water. Water maps render above ledges, allowing a stream on a plateau.

## Height and structures

**Low ledges:** paint GrassLedge or StoneLedge as a complete filled footprint. Their automatic lower edge already includes a short cliff face.

**Tall cliffs:** extend the south edge with Middle face tiles, then a Foot row. Use Left and Right pieces at the ends and repeat Center pieces for width. Repeat Middle rows for height. The Top face row supplies its own cap for fully manual construction; omit it beneath an automatic ledge to avoid a doubled cap. These extensions are authored manually, not added by the RuleTile.

**Stairs and ramps:** combine the matching 3x3 pieces; repeat Center columns and Middle rows to widen or lengthen them. The art is oriented for walking north/south. Place it over the drop and remove collision beneath the entire opening. The showcase demonstrates a broad staircase and a short earth ramp.

**Low walls:** draw connected lines with Paint_StoneWall and leave cells empty for gates. These low borders render below the player; use the Above Player category for future tall wall/arch artwork that needs occlusion.

Elevation is represented visually. This art kit does not add jumping, falling, swimming, water damage or a height simulation. The separate collision map establishes the walkable layout using existing 2D movement. Collision is cell-sized, so allow generous paths beside banks and use openings at least two cells wide.

## Moving water

The Paint palette's second row contains **Animated_Ripple**, **Animated_FallBody**, **Animated_FallFoam**, then the static **FallLip**. Place these on **Terrain/WaterDetails**, set to 4 frames per second in the template.

- Use ripples sparingly inside water.
- Stack FallLip at the plateau edge, repeat FallBody down the face, then put FallFoam in the receiving pool.
- The three animations have four frames each and use stock Unity AnimatedTile assets. No custom runtime animation script.

The Structures palette also includes a ready-stacked waterfall column. Its width is one cell; leave water beneath the transparent margins. These are decorative water effects.

## Contents and preview

- **315 individual visual tiles:** 250 water/ledge pieces, 16 wall connections, 36 cliff/stair/ramp pieces and 13 water-effect frames.
- **6 RuleTile brushes**, **3 AnimatedTiles**, and **3 registered Tile Palettes** (Paint, Structures, IndividualPieces).
- A combined Ground/Terrain template in `../Templates/` and the shared **Assets/Scenes/DemoTutorial.unity** presentation, excluded from build settings.
- The original PrototypeLoop is not repainted.

**TutorialTerrain_IndividualPieces** exposes every sprite, grouped by material with a blank row between groups. Ordinary Tile assets do not count as matching neighbors of a RuleTile. Use a consistent automatic brush on its named map, or place manual pieces on a separate map.

The shared DemoTutorial landscape includes a river and island pond, raised spring feeding a waterfall, garden plots within low walls, a cobbled square, and a stone terrace reached by a ramp. It demonstrates every available material family and all three water animations. The scene contains only world tiles, a camera and light. The actual tutorial is for you to build separately.

## Source and rebuilding

`TutorialTerrain16.png` is a **320x400** atlas. Its native 16px sprites have **2px extrusion on each side** (20px pitch). Use the imported sprites; do not slice the atlas at a plain 16px interval. `TileManifest.json` and `TileIndex.csv` record the exact rectangles and connection masks.

Production pixels are authored deterministically in `Tools/Art/build_tutorial_terrain.py`, extending the Ground generator's tested geometry. Run it with Python and Pillow, then use **Tools > The Lost Shrine > Build Tutorial Terrain Kit**. Build the Ground kit first if starting from scratch. Rebuilding the Terrain kit updates the shared empty template but never creates or overwrites a scene. Rebuilds preserve sprite IDs for unchanged names. `TileKitAssets` shares import and palette creation with the Ground builder; all builder code is Editor-only.

Verification covers native scale, alpha/palette geometry, neighbor rule selection and erase refresh, separate collision, open sample routes, animation frames, registered palettes and stable sprite IDs. See `Tools/Verification/TutorialTerrainKitChecks.cs.txt` and `GenerationReport.json`.
