# Tutorial Environment

The production kit follows the user-approved sample: broad flat shapes, restrained texture and **14 exact Hearth & Meadow colors**. It contains **31 objects**, **229 native 16×16 tile pieces**, **28 static prefabs**, and **three practice-art prefabs**, all at **16 pixels per unit**.

| Group | Included |
| --- | --- |
| Village | Clay-roof cottage, thatched cottage, woodshed, roofed stone well |
| Vegetation | Oak, birch, apple tree, hedgerow, fallen log |
| Stone | Boulder, boulder cluster, small stones, broken pillar, ruined wall, old arch, dilapidated stone bridge |
| Fencing | Horizontal/vertical sections, four corners, open/closed gates, repaired section, end post |
| Everyday props | Empty notched chopping stump, firewood rack, worn/new target stands, practice dummy |

The cottages, oak, boulder and horizontal fence preserve the approved sample pixels exactly. The stump has an empty hatchet notch; the pickup remains separate. Targets and dummy contain art only. Lighting, fog, shadows, camera visibility effects and the URP migration remain deferred.

## Place complete objects

Use **Assets/Prefabs/Environment/Tutorial**. Each prefab has an **Environment** Tilemap, an **Above Player** Tilemap, and separate invisible **Collision**. Origins are the lower-left of the sprite canvas; use whole units normally, or 1/16-unit increments for matching fence posts.

- Trees use a single 1×1 trunk collision cell. Their upper trunk/canopy is overhead art without collision, so the player can walk behind it. Canopy fading/cutaways are not implemented yet.
- Buildings block their lower footprint; roof portions are overhead. Cottages have even-width footprints, so center approaches with two path columns. Extend the last path cells beneath the threshold to remove the grass gap. Buildings are closed exterior art; interiors/door interactions are a later task.
- The stone bridge occupies 10×4 cells. Its middle two rows are walkable deck; top/bottom rows block movement. Clear any existing water collision beneath the deck when placing it. A prefab cannot erase the underlying world collision.
- Open gates and the arch have a clear center passage. Closed gates remain static solid art.
- Small stones are walkable. Other rocks, log piles and structures block their lower footprint.
- Keep objects as independent prefab instances when their silhouettes overlap. Painting a whole rectangular stamp into the same Tilemap as another object can overwrite its cells.

Fence sections are complete modules, not automatic RuleTiles. Join matching end posts by overlapping separate prefabs: horizontal repeat spacing is **38 pixels / 2.375 units**; vertical spacing is **34 pixels / 2.125 units**. Corners and gates need their posts aligned visually. DemoTutorial contains a joined enclosure built this way, retaining each object's own collision.

**Assets/Prefabs/WorldArt/Tutorial** holds the two targets and dummy as visual-only GameObjects. They have no health, hit detection, rewards or colliders and do not replace the prototype's working gameplay prefabs.

## Paint with tiles

Select **TutorialEnvironment** in the Tile Palette. It contains the 28 static objects assembled from their individual pieces; copy a whole stamp or use individual tiles. The three practice props are excluded to keep them as GameObjects.

Palette tiles contain visuals only. In **TutorialZoneTemplate**, author **Collision** separately and put upper tree/roof pieces in **Above Player**. Complete prefabs already include these parts and footprints.

## Presentation and rebuilding

Open **Assets/Scenes/DemoTutorial.unity** for the one full art presentation. It has three village homes, a well/work area, a kitchen garden, orchard, practice yard, river crossing and old stonework. It remains excluded from gameplay builds. The actual tutorial layout and gameplay wiring are separate authoring work.

**Tools > The Lost Shrine > Build Tutorial Environment Kit** rebuilds the atlas, tiles, palette, static prefabs, practice visuals and existing blank zone template. It removes obsolete generated tile pieces and static prefab names. It does not repaint scenes.

**Rebuild Demo Tutorial Presentation** explicitly reconstructs only DemoTutorial and refuses to overwrite unsaved edits. It places reusable prefab instances, preserves overlapping art, and sets foreground canopy ordering for the presentation.

## Sources and verification

[Sources and exact prompts](../../../../../../Docs/Art/Tutorial/Sources/Environment/README.md) are in the art docs. New artwork was generated with the built-in image tool using the approved sample as its style reference. The importer crops/resamples new sources, restricts each object to its approved material swatches and makes alpha binary. The approved four sprites are copied directly from their native reference.

`EnvironmentManifest.json` records crop regions, dimensions, swatches, overhead splits and collision rectangles. The atlas uses point filtering, no mipmaps and no compression. Surviving sprite names/identifiers and asset GUIDs are preserved on rebuild.

`Tools/Verification/TutorialEnvironmentKitChecks.cs.txt` checks exact palette/alpha, unchanged approved pixels, tile/prefab references, trunk footprints, canopy sorting against the real player renderer, and player-sized openings. `DemoTutorialChecks.cs.txt` also checks connected routes against all scene colliders.

[Current art plan](../../../../../../Docs/Art/Tutorial/Plan.md) · [Full demo](../../../../../../Docs/Art/Tutorial/Previews/demo-tutorial.png) · [Village close-up](../../../../../../Docs/Art/Tutorial/Previews/tutorial-environment-detail.png)
