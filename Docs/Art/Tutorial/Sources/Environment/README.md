# Environment art sources

The simplified sample was approved for full production on 23 September 2026. The previous high-detail Village/Construction sources and their kit assets were replaced.

| File | Role |
| --- | --- |
| `Sample.png`, `Sample-prompt.txt` | Approved generated style reference and exact prompt |
| `ApprovedSample16.png` | Exact native cottage, oak, boulder and horizontal fence pixels reused by production |
| `Sample-preview.png` | Historical approved sample shown on the first three tile layers |
| `Structures.png` | Thatched cottage, woodshed, well, arch, pillar and ruined wall |
| `Nature.png` | Birch, apple tree, hedge, boulder cluster, fallen log, notched stump, firewood and small stones |
| `Fences.png` | Vertical segment, corners, closed gate, repaired section and post; its open-gate draft is unused |
| `OpenGate.png` | Replacement open gate with a clear player-width passage |
| `Bridge.png` | Dilapidated stone crossing with broad walkable deck |
| `Practice.png` | Worn/new targets and straw dummy |

Each generated production sheet has a matching `*-prompt.txt`. All were made with the **built-in image_gen tool**, using `Sample.png` as the style reference. These are source images, not the production tile atlas: the Editor builder prepares native dimensions, applies material-specific swatches from the existing Tutorial palette, and restricts alpha to 0/255. `ApprovedSample16.png` is copied without resampling.

The production atlas and painting guide are under `TheLostShrine/Assets/Art/Tiles/Tutorial/Environment`. The manifest there is the source of truth for crop regions, footprints and overhead splits. Rebuild via **Tools > The Lost Shrine > Build Tutorial Environment Kit**.

[Current plan](../../Plan.md) · [Full demo](../../Previews/demo-tutorial.png) · [Village close-up](../../Previews/tutorial-environment-detail.png)
