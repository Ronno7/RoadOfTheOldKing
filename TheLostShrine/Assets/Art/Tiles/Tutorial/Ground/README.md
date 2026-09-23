# Tutorial Ground 16

Native 16x16 ground kit using the approved Hearth & Meadow palette. One tile is one Unity unit. Point filtering, uncompressed RGBA, full-rectangle sprites and no mipmaps are already configured.

## Start painting

1. Drag `../Templates/TutorialZoneTemplate.prefab` into your new scene, then unpack your copy if you want to change its structure.
2. Open **Window > 2D > Tile Palette** and choose **TutorialGround_Paint**.
3. Select **Ground/Grass** as the active Tilemap, choose the leftmost brush, and fill your playable area.
4. Select the corresponding material Tilemap under **Ground** before painting that material. The seven brushes, left to right, are **Grass, DryGrass, Earth, DampEarth, WornStone, Cobbles, TilledSoil**. Hover or select an asset to see its name.
5. Use the **TutorialPaths** palette and **Paths/DirtLane** for shaped village routes. Use **Ground/Earth** for bare-earth yards. Corners and connections update as you paint or erase.
6. Paint solid areas on **Collision** using the separate red brush below the material brushes (stored in `../Collision/`). Its renderer is disabled, while its TilemapCollider2D remains active. Ground and path brushes never generate collision.

Use the automatic brushes for normal building. **TutorialGround_IndividualPieces** exposes every shape for deliberate manual placement. Its rows are grouped by material in the same order, with a blank row between groups. Manual Tile assets do not count as matching neighbors of a RuleTile: use a consistent automatic brush on a map, or author a separate manual map.

## Contents

- **308 visual Tile assets:** 8 grass fills, plus 50 pieces for each of six overlay materials.
- Each overlay includes all **47 valid eight-neighbor blob shapes**, plus 3 extra full-center variations: straight edges, outer and inner corners, bends, ends, narrow connectors, isolated patches, T/cross junctions and combined concave corners.
- **7 stock Unity RuleTiles** choose shapes and fill variations automatically. No custom runtime scripts.
- **2 registered Tile Palettes**, a separate collision tile, a shared blank zone template, and the combined DemoTutorial presentation scene.
- `TileIndex.csv` identifies every sprite rectangle and shape mask. `TileManifest.json` is the machine-readable equivalent.

## Material layering

Grass fills the base. Other materials have transparent transition edges, so the lower surface shows through. Keep different materials on their named submaps: dry grass over meadow, mud over earth, worn stone over a yard, and so on. This also lets a material work over different substrates without needing a new pair-specific atlas.

The template keeps the eight agreed categories: **Ground**, **Terrain**, **Paths**, **Environment**, **Detail Decoration**, **Collision**, **Above Player**, and **Interactive Objects**. Ground contains seven material Tilemaps. Interactive Objects is a regular GameObject container for prefabs. Environment uses Individual rendering; tall objects still need appropriate foot pivots and sorting when their art is added.

This kit covers ground surfaces. The companion [Terrain kit](../Terrain/README.md) now supplies water, cliffs, riverbanks, walls and elevation pieces. Buildings, trees and loose decorations remain separate art passes. The cobbles are ordinary village paving, not the monumental ancient roads reserved for later zones. Start with the Terrain kit's combined template when you want both kits ready to paint.

## Presentation and rebuilding

Open `Assets/Scenes/DemoTutorial.unity` for the combined world-only presentation. The former per-layer preview scenes and showcase prefabs have been removed. The reusable combined template is `../Templates/TutorialZoneTemplate.prefab`.

The production atlas remains `TutorialGround16.png`: 320x400 pixels, with native 16px cells and 2px extrusion on each side. Use the imported sprite rectangles, not plain 16px grid slicing.

Run `Tools/Art/build_tutorial_ground.py` with Python and Pillow, then choose **Tools > The Lost Shrine > Build Tutorial Ground Kit**. This updates generated art, Tiles, RuleTiles and palettes, preserving sprite IDs. It does not create or repaint scenes. The builder and shared `TileKitAssets` helpers are Editor-only.

The native production pixels use exact palette colors and tested connection geometry. Connection checks live in `Tools/Verification/TutorialGroundKitChecks.cs.txt`; presentation checks live in `DemoTutorialChecks.cs.txt`.
