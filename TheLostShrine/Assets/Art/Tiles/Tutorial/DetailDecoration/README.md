# Tutorial Detail / Decoration

A compact **12-design kit**, approved for production on 23 September 2026. The six approved sample designs retain their exact native pixels. The additions provide just enough variety for homes, work areas, practice and the outskirts.

| Ground stamps | Visual-only GameObjects |
| --- | --- |
| Cream meadow flowers, grass tuft, loose grass, fallen leaves, low carved stone, wood chips, split logs, worn practice ground | Clay jar, wooden bucket, grain sack, unlit stone fire ring |

## Painting and placement

Select **TutorialDecoration** in the Tile Palette. Its eight ground stamps use **12 native 16x16 tile pieces**. Copy the complete one- or two-cell stamp. All tiles have no collision and belong on **Detail Decoration**, Ground sorting order **30**, above paths and below structures/player.

The four prefabs live in **Assets/Prefabs/WorldArt/Tutorial** with names ending **_Visual**. Their origin is at the feet. Jar, bucket and sack draw on World/order 1; the low fire ring draws on Ground/order 31 so the player can pass over it. These are art-only props with no colliders, interaction, rewards or checkpoint behavior. Attach them to the existing gameplay objects during tutorial authoring; the unlit fire ring is the base for a later bonfire presentation.

Use sparse clusters at grass edges, underneath trees and near work/storage areas. Reserve worn-earth stamps for standing positions on bare earth. Keep doors, bridge decks and main paths quiet.

The native atlas uses **13 exact Hearth & Meadow colors**, **16 pixels per unit**, binary alpha, point filtering, no compression and no mipmaps. No atmosphere or renderer changes are included.

## Demo and rebuilding

**Assets/Scenes/DemoTutorial.unity** contains the dressed village. Ground dressing uses the shared **Detail Decoration** map; props sit under **Decoration Objects**. The blank zone template remains empty for authoring.

- **Tools > The Lost Shrine > Build Tutorial Decoration Kit** rebuilds assets and the palette without repainting scenes.
- **Dress Demo with Tutorial Decorations** replaces only decoration placements in a saved DemoTutorial and saves it.
- **Rebuild Demo Tutorial Presentation** recreates the full art demonstration, including decoration.

[Full demo](../../../../../../Docs/Art/Tutorial/Previews/demo-tutorial.png) · [Village detail](../../../../../../Docs/Art/Tutorial/Previews/tutorial-decoration-village.png) · [Practice and rest-site detail](../../../../../../Docs/Art/Tutorial/Previews/tutorial-decoration-practice.png)

## Source and preparation

[Sources and exact prompts](../../../../../../Docs/Art/Tutorial/Sources/Decoration/README.md) are saved in the art docs. New artwork uses the built-in image tool with the approved sample as reference. The importer copies approved pixels directly, crops/resamples the expansion, restricts its colors and makes alpha binary. Practice wear maps the source's pale dirt into earth/timber shades for lower contrast.

`DecorationManifest.json` records source rectangles, native placement/sizes and swatches. Source crop coordinates are top-left; atlas coordinates are bottom-left. Native entries come from ApprovedSample16.png; others come from Expansion.png. Existing tile/prefab GUIDs and sprite identifiers are retained.

[Current art plan](../../../../../../Docs/Art/Tutorial/Plan.md)
