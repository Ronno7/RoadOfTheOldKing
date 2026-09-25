# Environment art sources

These source sheets and native references are required by the production Environment builder.

| File | Role |
| --- | --- |
| `Sample.png`, `Sample-prompt.txt` | Approved generated style reference and exact prompt |
| `ApprovedSample16.png` | Exact native oak, boulder and horizontal fence pixels reused by production; original cottage retained only in this shared reference |
| `Structures.png` | Woodshed, well, arch, pillar and ruined wall; the cottage in this sheet is unused |
| `Cottages.png`, `Cottages-prompt.txt` | Current 6x5 workshop (96x80) and 8x6 thatched longhouse (128x96) |
| `Nature.png` | Birch, apple tree, hedge, boulder cluster, fallen log, notched stump, firewood and small stones |
| `Fences.png` | Vertical segment, corners, closed gate, repaired section and post; its open-gate draft is unused |
| `OpenGate.png` | Replacement open gate with a clear player-width passage |
| `Bridge.png` | Dilapidated stone crossing with broad walkable deck |
| `Practice.png` | Worn/new targets and straw dummy |

Each generated production sheet has a matching `*-prompt.txt`. All were made with the **built-in image_gen tool**, using `Sample.png` as the style reference. These are source images, not the production tile atlas: the Editor builder prepares native dimensions, applies material-specific swatches from the existing Tutorial palette, and restricts alpha to 0/255. `ApprovedSample16.png` is copied without resampling.

The production atlas and painting guide are under `TheLostShrine/Assets/Art/Tiles/Tutorial/Environment`. The manifest there is the source of truth for crop regions, footprints and overhead splits. Rebuild via **Tools > The Lost Shrine > Build Tutorial Environment Kit**.

[Current plan](../../Plan.md) · [Full demo](../../Previews/demo-tutorial.png) · [Village close-up](../../Previews/tutorial-environment-detail.png)
