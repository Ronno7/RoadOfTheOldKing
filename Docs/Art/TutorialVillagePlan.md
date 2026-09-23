# Tutorial village plan

Direction: the boy's home village feels quaint, cozy and lived in, with a Shire-like warmth. Familiar daily life gives way gradually to older, wilder surroundings. Keep the teaching route readable and let wear, repairs and placement tell the story.

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
- Next art priority: stump and the two target-stand variants, followed by the dummy-yard props and village structures.

## Progression recommendation — proposed, not yet decided

Introduce Sun Shards and heart fragments after the bridge in the Green Lowlands. Let the village teach the hatchet, basic combat and bonfire rest without also introducing two collection systems.

Award the first Sun Shard from a meaningful overworld encounter or puzzle; arrange three earned shards before the first upgrade purchase, retaining the existing 3-shard price. Introduce heart fragments separately through an optional exploration reward, after the player understands the main route. Avoid presenting both new reward types together.

The stump, targets and worn practice yard can reward curiosity through storytelling before permanent collectibles appear. Recall's separate unlock and its puzzle placement still need to be fitted into the later route; picking up the hatchet does not grant Recall.
