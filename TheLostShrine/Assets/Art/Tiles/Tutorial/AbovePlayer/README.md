# Tutorial Above Player

The current tutorial's overhead art is complete within the Environment kit. This folder documents the layer; the sprites remain in the shared Environment atlas so their palette, pixel scale and silhouettes stay identical to the base artwork.

| Object | Overhead portion |
| --- | --- |
| Oak, birch, apple tree | Upper trunk and canopy, starting one tile above the base |
| Clay and thatched cottages | Upper two rows, including roofs |
| Woodshed and well | Upper structure/roof, starting one tile above the base |
| Old arch | Upper pillars and arch, above the separate base supports |
| Broken pillar and ruined wall | Upper masonry above the base footprint |
| Stone bridge | Near and far parapet rows; middle deck stays below the player |

Other Environment prefabs also assign their raised portions where needed. Banners are optional future art; the compact tutorial currently uses the existing roofs, trees and masonry.

## Authoring

Place complete objects from **Assets/Prefabs/Environment/Tutorial**. Each contains separate **Environment**, **Above Player** and **Collision** Tilemaps, with the artwork already split.

For manual tile painting, use the **TutorialEnvironment** palette and paint upper pieces into the blank template's **Above Player** map. Paint the lower pieces into Environment, and author solid footprints in Collision. Avoid painting the same piece on both visual maps.

Above Player uses sorting layer **Player**, order **100**; the current player Body uses Player/order **0**. The demo assigns higher per-instance overhead orders based on object placement so nearer crowns overlap more distant crowns consistently. Keep independent prefab maps when objects overlap.

Overhead Tilemaps have **no Collider2D**; their visual tiles also have no collision. A canopy or roof can cover the player while its base remains solid. Walking beneath it never implies walking through its trunk or building footprint.

## Verified and deferred

On 23 September 2026, the Environment kit passed 927 checks. Additional inspection verified all ten primary tree/building/ruin prefabs and the blank template: overhead art exists, draws above the actual player Body, and has no collider.

The art demo remains a presentation scene. Canopy/roof fading, player silhouettes, camera visibility handling and gameplay integration remain deferred in the [village plan](../../../../../../Docs/Art/Tutorial/Plan.md).

[Environment guide](../Environment/README.md) · [Full demo](../../../../../../Docs/Art/Tutorial/Previews/demo-tutorial.png)
