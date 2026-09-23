# Tutorial village plan

Direction: the boy's home village feels quaint, cozy and lived in, with a Shire-like warmth. Familiar daily life gives way gradually to older, wilder surroundings. Keep the teaching route readable and let wear, repairs and placement tell the story.

## Current art work: Decoration complete; Collision and Above Player verified

The user approved the simplified Environment and Decoration samples for full production on 23 September 2026, requesting a restrained tutorial decoration kit. Ground, Terrain and Paths remain the visual reference. The old high-detail Environment kit has been replaced.

- **Complete:** 31 cohesive objects, 229 tile pieces, one painting palette, 28 static structure prefabs and three practice-art prefabs. [Full demo](Previews/demo-tutorial.png) · [Village close-up](Previews/tutorial-environment-detail.png) · [Painting guide](../../../TheLostShrine/Assets/Art/Tiles/Tutorial/Environment/README.md).
- **Style:** 14 exact colors from [Hearth & Meadow](../Palettes/OpeningZonePalettes-v1.md), 16 pixels per unit, binary transparency, broad flat shapes and selective texture. Start materials with base, shadow and highlight. The four approved sample sprites are preserved exactly in the production atlas.
- **Decoration complete:** 12 designs total, eight ground stamps (12 tile pieces), one palette and four visual-only props. Flowers, two grass shapes, leaves, carved stone, chips and split logs provide ground detail; worn earth marks the practice positions. Jar, bucket, sack and an unlit fire ring stay separate GameObjects. [Village detail](Previews/tutorial-decoration-village.png) · [Practice/rest detail](Previews/tutorial-decoration-practice.png) · [Painting guide](../../../TheLostShrine/Assets/Art/Tiles/Tutorial/DetailDecoration/README.md).
- **Restraint:** preserve the six approved sample designs exactly, reuse small clusters where they tell an everyday story, and keep routes clear. No further decoration expansion is planned for this tutorial pass.
- **Collision / Above Player:** the existing kit covers separate solid footprints and overhead canopy, roof, arch and ruin art. Sorting was checked against the actual player Body; upper maps have no colliders. [Above Player guide](../../../TheLostShrine/Assets/Art/Tiles/Tutorial/AbovePlayer/README.md). The current tutorial needs no additional overhead artwork; banners remain optional.
- **At the end:** consider URP's 2D Renderer, lighting, shadows, mist/fog and restrained effects. These remain deferred; the art stands on its own first.

Keep one DemoTutorial scene and one shared blank template. The temporary Environment sample assets/builder and old generated Environment sources were removed; its approved sample remains documented as a reference. The Decoration sample assets were promoted into production, with their GUIDs preserved; approved reference images remain in the docs. Decoration uses the demo's existing Detail Decoration map and a small visual-prop group.

### Placement and later integration

- Cottage approaches use two path columns centered on their doors and continue beneath the threshold to avoid a grass gap. The woodshed approach follows its offset doorway.
- Tree collision covers a single ground-level trunk cell. The upper trunk and canopy draw above the player and remain passable underneath. The kit checks verify the real player renderer's sorting layer and player-sized clearance behind a demo oak.
- Independent object Tilemaps preserve overlapping silhouettes; placing a rock no longer cuts cells out of a neighbouring tree. Fence sections use separate prefabs with native-pixel offsets to join posts.
- Player-visibility/camera follow-up: evaluate a gentle fade or cutaway of the occluding canopy/roof, with steady camera tracking; consider a player silhouette if needed. This is recorded, not implemented.
- Practice targets/dummy and household props are visual-only prefabs; connect them to the existing gameplay systems when building the actual tutorial. The unlit fire-ring art is ready, but bonfire/checkpoint behavior and fire effects are not wired into DemoTutorial.

## Route and required assets

| Beat | Assets to create | Story and teaching purpose |
| --- | --- | --- |
| Find the hatchet | Chopping stump with a permanent hatchet notch; split logs and wood chips | The hatchet has a familiar resting place. Keep the pickup separate so the empty notch remains visible afterward. |
| Throwing practice | Two variants of one static target stand: heavily worn and newer | Old cuts clustered around the center, a repaired brace and a worn throwing spot imply years of practice. Use the newer stand for the active lesson, with clear hit feedback. |
| Basic combat | Small dummy practice yard; appropriate dummy art and simple boundary props | Reuse the prototype combat lessons. Give the player clear room to slash, cleave and move. Repair patches and flattened ground make the yard feel used. |
| Village outskirts | Wilder grass/shrubs, less-maintained fencing and an old roadside marker | Gradually trade tended spaces for encroaching vegetation and older stonework. Space for one easy enemy; a wolf or equivalent is a later character-art task. |
| First rest | Bonfire site with stone ring and a modest resting spot | Introduce the existing rest/checkpoint mechanic after the first encounter. Preserve a clear approach and interaction area. |
| Leave home | Dilapidated stone bridge: deck, banks/abutments, broken parapets and chipped/mossy details | Cross into the Green Lowlands. Keep a clearly passable center despite the damaged edges. A faint weathered sun carving could hint at the older world. |

Village dressing also needs a small cohesive set of houses, a well, trees and fences. Start with reusable pieces; add restrained flowers, stacked firewood and household details around places where someone would actually use them.

## Authoring

- Existing Ground, Terrain and Paths supply the base. New static structures belong to Environment; small storytelling accents belong to DetailDecoration. Use AbovePlayer only where occlusion is needed and keep Collision separate.
- Hatchet pickups, working targets, dummies and the bonfire remain GameObjects using the existing systems. Static-looking does not mean baking their gameplay into a Tilemap.
- Extend the single DemoTutorial presentation as art becomes available. The user will build the actual tutorial scene later; no additional per-layer demo scenes or templates.
- The production Environment kit covers the stump, targets, dummy, cottages, well, trees, fencing, bridge and modest masonry. The completed Detail / Decoration kit covers chips/split logs, worn throwing spots, modest household dressing and an unlit bonfire base.

## Agreed progression direction

Keep the village grounded in everyday life and introduce fantasy/RPG discoveries after crossing the bridge into the Green Lowlands, as agreed in the development conversation. Let the village teach the ordinary hatchet, basic combat and bonfire rest before introducing Sun Shards and heart fragments.

Award the first Sun Shard from a meaningful overworld encounter or puzzle; arrange three earned shards before the first upgrade purchase, retaining the existing 3-shard price. Introduce heart fragments separately through an optional exploration reward, after the player understands the main route. Avoid presenting both new reward types together.

The stump, targets and worn practice yard can reward curiosity through storytelling before permanent collectibles appear. Recall's separate unlock and its puzzle placement still need to be fitted into the later route; picking up the hatchet does not grant Recall.
