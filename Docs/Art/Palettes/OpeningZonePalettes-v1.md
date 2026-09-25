# Opening-zone color palettes

Hearth and Meadow is the production tutorial environment palette. Field and Old Road is the planned Green Lowlands palette. The hex values below are the source of truth; generated reference imagery may approximate them.

The tutorial is a sheltered settlement within Green Lowlands. Keep both places green and sunlit, with warmth coming from earth, timber, cream plaster and clay roofs rather than an orange filter. Shared earth colors make the transition feel continuous.

## Tutorial - Hearth and Meadow

| Use | Hex |
| --- | --- |
| Grass shadow | `#384B3C` |
| Grass shade | `#60724D` |
| Grass base | `#8B9B5E` |
| Grass light | `#BECA82` |
| Deep timber | `#543F36` |
| Timber | `#826047` |
| Dirt | `#B58C61` |
| Dry earth / straw | `#DFC291` |
| Roof shadow | `#794A41` |
| Clay roof | `#B86F50` |
| Warm roof edge | `#D99A67` |
| Cream plaster | `#F1DEB0` |
| Well stone shade | `#77766A` |
| Well stone light | `#B2AE91` |
| Muted water | `#6E9F9A` |
| Sun gold | `#E7B85C` |

## Green Lowlands - Field and Old Road

| Use | Hex |
| --- | --- |
| Foliage shadow | `#304C42` |
| Foliage shade | `#4F7050` |
| Meadow base | `#7F995A` |
| Meadow light | `#B5C97C` |
| Deep timber | `#543F36` |
| Timber | `#826047` |
| Dirt | `#B58C61` |
| Dry earth / straw | `#DFC291` |
| Ruins shadow | `#585F58` |
| Weathered stone | `#7C8370` |
| Stone light / lichen | `#A6AA8A` |
| Sunlit stone / birch | `#D4CCAA` |
| Stream depth | `#395F66` |
| Stream base | `#638C8E` |
| Stream reflection | `#9BC0AE` |
| Sun gold | `#E7B85C` |

## Material and readability notes

- Tutorial: simple cream-plaster houses, clay roofs, warm timber fences, a gray-green stone well, soft green grass and readable dirt paths. Ground detail stays quieter than objects.
- Green Lowlands: meadows, farms, oak/birch woods, muted streams and old roads. Use low-contrast stone for standing stones, broken milestones, sun carvings and small grassy burial mounds.
- Both share four earth/timber colors and sun gold: 27 distinct environment colors across the two palettes. Objects can carry their original material colors across the zone boundary; a village roof should not change hue when seen from the meadow.
- These are working environment palettes, not a rule that every individual asset uses all 16 colors. Start simple materials with a base, shadow and highlight.
- Keep water muted blue-green. Save more saturated cyan for Recall or other interactive effects, so stream reflections do not resemble interactables.
- Keep sun gold rare: tiny carvings or accent details. Sun Shards, bonfires and important interactions can receive stronger contrast and distinct shapes.
- Red heart fragments and combat telegraphs retain dedicated gameplay colors outside these environment swatches. Confirm their readability against the finished ground tiles.
- Use the same light direction and pixel scale in both areas. Avoid blanket sepia tint, neon grass, pure-black texture noise and busy ground patterns.

## Relation to later zones

The palette progression can become cooler and more subdued in Barrow Woods, move toward exposed gray stone and desaturated grasses in Storm Highlands, and emphasize deep stone shadows, bronze and concentrated firelight in the main dungeon. Those palettes remain undecided; the opening should not already look haunted or monumental.

## Files

- [Tutorial palette](tutorial-hearth-and-meadow-v1.gpl)
- [Green Lowlands palette](green-lowlands-field-and-old-road-v1.gpl)
- Generated comparison sheet: `opening-zone-palettes-v1.png` (concept reference, not a tileset).
- [Image-generation prompt](opening-zone-palettes-v1-prompt.txt). Generated with the built-in image tool.
