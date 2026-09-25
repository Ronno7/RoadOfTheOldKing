# Tutorial village plan

Direction: the boy's home village feels quaint, cozy and lived in, with a Shire-like warmth. Familiar daily life gives way gradually to older, wilder surroundings. Keep the teaching route readable and let wear, repairs and placement tell the story.

## Current state

Tutorial includes the [animated player and separate hatchet](../Player/README.md), idle breathing, follow/zoom camera and working stump pickup. Combat presentation and the remaining lesson interactions are next.

Ground, Terrain and Paths establish the simple, restrained visual style for Environment and Decoration.

- **Complete:** 31 cohesive objects, 275 tile pieces, one painting palette, 28 static structure prefabs and three practice-art prefabs. [Full demo](Previews/demo-tutorial.png) · [Village close-up](Previews/tutorial-environment-detail.png) · [Painting guide](../../../TheLostShrine/Assets/Art/Tiles/Tutorial/Environment/README.md).
- **Style:** 14 exact colors from [Hearth & Meadow](../Palettes/OpeningZonePalettes-v1.md), 16 pixels per unit, binary transparency, broad flat shapes and selective texture. Start materials with base, shadow and highlight. The approved oak, boulder and fence pixels remain unchanged; the cottages use the approved larger proportions.
- **Decoration complete:** 12 designs total, eight ground stamps (12 tile pieces), one palette and four visual-only props. Flowers, two grass shapes, leaves, carved stone, chips and split logs provide ground detail; worn earth marks the practice positions. Jar, bucket, sack and an unlit fire ring stay separate GameObjects. [Village detail](Previews/tutorial-decoration-village.png) · [Practice/rest detail](Previews/tutorial-decoration-practice.png) · [Painting guide](../../../TheLostShrine/Assets/Art/Tiles/Tutorial/DetailDecoration/README.md).
- **Restraint:** preserve the six approved sample designs exactly, reuse small clusters where they tell an everyday story, and keep routes clear. No further decoration expansion is planned for this tutorial pass.
- **Collision / Above Player:** the existing kit covers separate solid footprints and overhead canopy, roof, arch and ruin art. Sorting was checked against the actual player Body; upper maps have no colliders. [Above Player guide](../../../TheLostShrine/Assets/Art/Tiles/Tutorial/AbovePlayer/README.md). The current tutorial needs no additional overhead artwork; banners remain optional.
- **At the end:** consider URP's 2D Renderer, lighting, shadows, mist/fog and restrained effects. These remain deferred; the art stands on its own first.

Keep one DemoTutorial art reference scene and one shared blank template. Decoration uses the Detail Decoration map and a small visual-prop group. Source sheets and native references remain available for asset rebuilds.

### Placement and later integration

- **Replacement structures:** 6x5 timber workshop (96x80 pixels) with an offset entrance and covered work bay; 8x6 thatched longhouse (128x96 pixels) with a recessed porch and offset gable. Both use 16 PPU and the existing palette. [Current Unity view](Previews/cottages-village.png).
- Cottage approaches use two path columns centered on their doors and continue beneath the threshold to avoid a grass gap. The woodshed approach follows its offset doorway.
- Tree collision covers a single ground-level trunk cell. The upper trunk and canopy draw above the player and remain passable underneath. The kit checks verify the real player renderer's sorting layer and player-sized clearance behind a demo oak.
- Independent object Tilemaps preserve overlapping silhouettes; placing a rock no longer cuts cells out of a neighbouring tree. Fence sections use separate prefabs with native-pixel offsets to join posts.
- Player-visibility/camera follow-up: evaluate a gentle fade or cutaway of the occluding canopy/roof, with steady camera tracking; consider a player silhouette if needed. This is recorded, not implemented.
- Practice targets/dummy and household props are visual-only prefabs; connect them to the existing gameplay systems when building the actual tutorial. The unlit fire-ring art is ready, but bonfire/checkpoint behavior and fire effects are not wired into DemoTutorial.

## Tutorial route — 100x100 playable area

[Recall-route sketch](Previews/tutorial-recall-route-sketch.png). The world layout is now built in Tutorial.unity; the awakening sequence and tutorial logic remain deferred. The playable footprint stays 100x100, with forest scenery outside its boundaries for camera coverage.

| Beat | Space and teaching purpose |
| --- | --- |
| 1. Home / movement | Start outside the player's house in the southwest. An open yard, garden wall and visible northeast gate make a safe movement loop. Teach movement first; sprint is an optional follow-up once walking is understood. |
| 2. Hatchet stump | A nearby work clearing holds the separate pickup in the stump's permanent notch. Wood chips and stacked logs explain its usual resting place. The worn path and targets ahead show the next destination. |
| 3. Throw / retrieve | Three target stands, worn standing marks and a low backstop. Targets stay within the existing six-unit throw range and landing spots remain reachable on foot. Recall stays locked. Detect a thrown hit followed by retrieval, not merely pressing E. |
| 4. Melee / dodge | A roomy dummy clearing teaches left-click chops and Space dodge without danger. Charged cleave can be an optional follow-up here; avoid presenting the full control list at once. Angle the exit toward the bridge. |
| 5. Forest road / southern bridge | Descend from practice and follow two broad woodland bends south. Dirt and fence remnants give way to broken paving, mossy pillars and collapsed walls. The southern bridge has safe landings on both banks. The western overlook remains optional. |
| 6. Recall awakening | Throw into the ancient stone in the roofless courtyard. A successful hit will trigger the brief input lock, awakening and automatic return; then restore control and save the unlock. The current stone is a static placeholder; this sequence is not implemented. |
| 7. Safe Recall practice | A separate ruined clearing north of the courtyard reserves a throw-and-recall exercise. Keep missed throws retrievable on foot and teach the bound Recall input after awakening. |
| 8. Easy enemy / first Sun Shard | A wooded bend separates practice from the wolf/easy enemy clearing. Keep future aggro/leash out of practice. Award the first shard once on defeat using a persistent reward ID. |
| 9. Bonfire / rest | A sheltered bend beyond combat holds the first bonfire. Teach F to light/rest, heal and set respawn; one shard does not afford the existing three-shard upgrade. |
| 10. Overworld exit | The northeast trail leads into Green Lowlands. Trigger transition only after the player commits to the exit trail, not when approaching the bonfire. |

One main route with two small diversions: a village well/courtyard loop and a river overlook. Use paths roughly 2-3 tiles wide, open yards around 12-16 tiles across, and a combat clearing around 14-18 tiles across as starting ranges. Size buildings with the existing 6x5 and 8x6 footprints. Tune travel distances to the actual camera and movement speed.

The river runs from a northern spring and short waterfall to the south edge. Cliffs, trees and low walls frame spaces and hide distant beats until the player approaches. End the northern cliff band before the exit trail. Let the next destination be visible from each clearing's exit; avoid opaque roof/canopy coverage on lesson positions. World art and sounds guide travel; the sketch's orange arrows are annotations, not proposed navigation UI. If the player bypasses practice, offer a contextual reminder at the bridge rather than invisible barriers.

### Boundaries and elevation

Use the Recall-route sketch above for placement. Dense woodland and the practice terrace define the boundaries.

- Surround the clearings with continuous dense woodland, including the unused southeast. Use broad canopy masses outside play, with visible trunks, undergrowth, rocks and fallen logs closing the accessible edge. Crop excess forest after testing travel distances.
- Mix short fences and garden walls near homes, stacked logs near the stump, and boulders along riverbanks. Preserve generous lesson spaces and roughly 2-3 tile connecting paths; avoid a continuous fenced corridor.
- Add one low practice terrace, approximately one tile of visible cliff height. A four-cell-wide ramp rises between the stump and throwing area; another descends after the dummy toward the bridge. Close the sketch's apparent east-side terrace bypass with logs/rocks: the walking route passes through practice, without requiring lesson completion to unlock passage.
- Keep both bridge banks level, leave a safe landing before the Recall courtyard, and keep ruins, combat and bonfire pockets flat. Northern cliffs and the waterfall remain scenic boundaries.
- Implement faux elevation with cliff artwork and a separate Collision boundary along ledges, open at ramps. Ramps remain walkable on the same 2D physics plane; no jumping or height system is needed.
- Canopies alone do not block movement: current trees have trunk-only collision. Add continuous terrain collision along visibly closed forest edges, check diagonal gaps, and preserve passage beneath individual trees inside the playable area. Keep lesson positions clear of overhead occlusion.

### Terrain blockout in Tutorial.unity

Cliff ledges and faces share full-cell geometry, matching seams and thin side outlines. Stepped ends use neighboring faces, and ramp returns are closed.

The tutorial uses a 100x100 grass canvas. River, northern spring/waterfall, raised practice terrace, two four-cell-wide ramps, dense boundary woodland and separate collision are placed. The ten-cell bridge supplies a testable crossing. Static dressing, player/camera and stump pickup are complete. Combat presentation, lessons, enemy and checkpoint integration remain.

**Ground and paths placed:** [Current overview](Previews/tutorial-populated.png). Dirt routes link the lesson spaces, with narrower well/garden/overlook trails and a worn woodland approach to the southern crossing. Earth marks yards and practice spaces; dry grass leaves most meadow green. Damp earth sits near banks and foundations, worn stone marks the bridge and ruins, cobbles form two village pads, and two tilled beds make the kitchen garden.

Flat surfaces use existing Ground maps. Ground/Raised Surfaces keeps dry grass, earth and worn stone visible on the terrace. Stone pads and ramps overlap connecting dirt cleanly. Water, walls, woodland and cliff boundaries use separate collision.

**Structures and decoration placed:** [Village/work yard](Previews/tutorial-populated-home.png), [practice terrace](Previews/tutorial-populated-practice.png), [forest road](Previews/tutorial-forest-road.png), [Recall courtyard](Previews/tutorial-recall-courtyard.png), [safe Recall practice](Previews/tutorial-recall-practice.png). The village contains the workshop, longhouse, well, stump, target stands and practice dummy. Southern props are under Environment/Ancient Road and Recall Ruins; earlier dressing remains under Environment/Village Dressing. Accents use Detail Decoration, foundations use Terrain/StoneWall, and collision follows visible boundaries. The old crossing is closed by river collision and its former eastern approach is woodland.

`Assets/Prefabs/WorldArt/Tutorial/RecallStone_Placeholder.prefab` reuses the pillar art with footprint collision and an editor-only future hit anchor. Its scene position is (80.5,30.5), facing the throwing mark at (81.5,28.5), 3.25 units from the hit anchor. The practice stone is at (82,44.5). Neither has Recall behavior. Control lock, unlock/save logic, effects, prompts and enemy wiring remain deferred.

The 13 editor-only anchors mark home (15,13), stump (40.5,24.5), throwing (37,53.5), melee (48,56.5), forest road (55,39), bridge landing (72,27.5), Recall throw (81.5,28.5), Recall practice (82,42.5), enemy/shard (86,64), bonfire (87,82), exit (88,97), well (15,38.5) and overlook (60,69). Bridge origin is (60,26), with walkable rows y=27..28. The exit remains capped at y=100 until transition wiring, while its visible trail continues north.

All 13 anchors connect through actual 2D colliders with 0.8-unit clearance, slightly larger than the player footprint. Closing either ramp prevents reaching the southern bridge; closing the bridge prevents reaching the eastern ruins. A linecast from the throwing mark reaches the placeholder unobstructed. Gameplay framing was inspected at default camera size 5.5; backdrop coverage was inspected at maximum zoom 8 and 21:9 near home, rest and exit. Scenery covers x=-20..119, y=-12..111 without enlarging playable bounds. The real player motor reached all 13 anchors in Play Mode. Continue reviewing travel pacing during playtesting. Edit the scene directly; do not rerun builders over placed work.

### Proposed tutorial UI and implementation

| Type | Presentation | Implementation |
| --- | --- | --- |
| Diegetic: part of the world | Open yard gate, worn throwing mark, cut targets, logs at stump, roadside sign and bonfire flame. | Environment art plus existing interactive target/bonfire feedback. Signs point to places; avoid writing keyboard instructions into village scenery. |
| Non-diegetic: screen UI | One short control hint at a time: move; aim + E; retrieve on foot; left-click; Space. A concise first-shard toast, then the shard counter. | A screen-space tutorial presenter, driven by local trigger volumes and actual progress. Show health/stamina when relevant. Reuse the prototype guide's event-driven approach in a dedicated playable-zone controller rather than copying its old gated route. |
| Spatial: anchored in the world | A nearby F prompt above the fire, a restrained pickup cue at the stump, and clear hit confirmation on the active target. | World-space Canvas or screen-projected anchors; show only the nearest eligible interaction. Hatchet pickup currently uses overlap, so its cue says approach/take and does not invent an F requirement. |
| Meta: feedback over the view | A brief optional screen-edge hurt cue during the first fight. | Driven by actual player damage; keep it subtle. This is proposed feedback, not world lighting/fog work or an implemented feature. |
| Non-diegetic menu | Bonfire rest/checkpoint explanation and later upgrades. | Use the existing bonfire interaction/menu flow; explain the three-shard price without forcing an upgrade lesson before it is affordable. |

The guide should persist milestones through the existing save system, respect actions completed early, and avoid replaying completed hints after death/load. Advance on actual movement, pickup, target hit/retrieval, melee/dodge, Recall awakening and manual Recall, enemy defeat, shard collection and first rest. Input labels use active bindings; triggers provide context, not proof of mastery. Keep one hint active. The brief Recall awakening is the exception to uninterrupted control: freeze after a successful stone hit, auto-return the axe, restore control and save the unlock. This remains future gameplay work.

## Authoring

- Existing Ground, Terrain and Paths supply the base. New static structures belong to Environment; small storytelling accents belong to DetailDecoration. Use AbovePlayer only where occlusion is needed and keep Collision separate.
- Hatchet pickups, working targets, dummies and the bonfire remain GameObjects using the existing systems. Static-looking does not mean baking their gameplay into a Tilemap.
- Keep DemoTutorial as the art reference; continue playable-zone work in Tutorial.unity. No additional per-layer demo scenes or templates.
- The production Environment kit covers the stump, targets, dummy, cottages, well, trees, fencing, bridge and modest masonry. The completed Detail / Decoration kit covers chips/split logs, worn throwing spots, modest household dressing and an unlit bonfire base.

## Progression

Keep the village grounded in everyday life. The order is movement, hatchet, throwing/retrieval and melee, neglected forest road, southern bridge, Recall awakening, safe Recall practice, easy enemy/first Sun Shard, bonfire, then Green Lowlands. The courtyard introduces the wider game's fantasy; heart fragments remain a later optional discovery.

Award the first Sun Shard from the tutorial's far-bank encounter; place the next two earned shards in the overworld before the first affordable upgrade purchase, retaining the existing 3-shard price. Introduce heart fragments separately through an optional exploration reward, after the player understands the main route. Avoid presenting both new reward types together.

Picking up the hatchet does not grant Recall. Its separate unlock is reserved for the ancient stone after the southern bridge; the world is built while that interaction remains deferred.
