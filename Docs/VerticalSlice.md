# Vertical slice: current state and next steps

Updated 27 September 2026. This is the development plan and consolidated deferred-work register; [Tutorial/Plan](Art/Tutorial/Plan.md) owns world layout, [AnimationPlan](AnimationPlan.md) owns animation details, and [SystemsOverview](SystemsOverview.md) owns technical behavior.

## Where we are

All current player animations are accepted as good for now. Further animation work is an incremental backlog alongside game development. Tutorial is walkable and supports player combat, but its teaching, enemy, checkpoint and progression sequence is not yet a complete playable slice.

| Area | Actual state |
| --- | --- |
| Tutorial world | Village, practice terrace, southern bridge, Recall ruins, safe practice space, enemy clearing, rest site and exit are built. World art and palette are established. |
| Tutorial gameplay | Player, follow/zoom camera, real stump pickup and practice dummy are present. No checkpoint session, working bonfire or enemy is connected. The intended Recall stone and practice stands remain static art. A separate active legacy `RecallAltar` still has an overlap-unlock component; reconcile it with the new route before teaching Recall. |
| Player | Original 16-cel locomotion/carrying and idle breathing, plus dedicated four-direction sprint, dash, opening strike, stationary throw/aim and catch. Tap/hold E, moving-aim gameplay and fast axe flight/Recall spin work. Dedicated moving-aim/catch drawings remain deferred. |
| Reusable mechanics | PrototypeLoop has movement, sprint/stamina, dodge, melee, throw/retrieve/Recall, health, a telegraphed melee enemy, bonfires, saving, a Recall puzzle, shards/upgrades and heart fragments. Reuse these systems. |
| New Recall sequence | Designed only. Replace the legacy overlap unlock with an actual outbound hit on the ancient stone, brief awakening/control lock, return, unlock and persistent completion. |
| Presentation | New body/weapon layers share gameplay timing and ownership. Future combo/cleave, reactions and interactions are ranked in the incremental animation backlog. Wolf presentation, contextual UI and final audio/effects remain; prototype feedback already exists. |
| Builds | Only PrototypeLoop is enabled in build settings. Existing browser output predates recent work; it is not the new tutorial. No overworld scene or exit transition exists yet. |

The read-only Unity audit confirmed the scene components and build list above. Route clearance, camera framing and earlier gameplay checks are recorded in [SystemsOverview](SystemsOverview.md#verification). Cross-scene persistence still needs implementation/verification; prototype bonfire travel is not proof of overworld transitions.

## End-of-day animation handoff

This work block began on 26 September afternoon in Phoenix and ended after midnight on the 27th. It spans both changelog headings.

- Completed dedicated sprint and dash in all four cardinal directions, with distance-driven sprint phase and armed/empty-hand presentation.
- Added south/west opening strikes and stationary throws, plus south and west catches. West catch closes the cardinal action loop.
- Refined compact proportions, muted colors, outlines, woven cap trim, blob hands and hatchet continuity. East sprint became the approved reference; west mirrors it by request. Redrew south empty-hand arms and its overhead strike; revised west forehand/throw/dash and east/south catches.
- Removed the rejected aim-walk drawings while preserving working movement/aim controls and their fallback. Kept native gameplay GIFs and approved sources; removed disposable drafts/captures throughout.
- The registered body set grew from **36 to 148 cel slots**, plus **eight shared axe-spin cels**. These counts include reuse and empty-hand variants, not 148 unique new drawings. The latest west-catch pass recorded 189 passing checks; that is a focused result, not a new full-project test run.

| Integrated family | Current body cels |
| --- | ---: |
| Opening forehand, 6 per cardinal view | 24 |
| Stationary throw/aim, 8 per view | 32 |
| Catch, 4 per view | 16 |
| Dash, 5 per view | 20 |
| Sprint, 8 per view, plus 24 separate empty-hand variants | 56 |
| **Registered total** | **148** |

The original 16-cel walk sheet is separate. Idle still uses breathing over existing art; the proposed new idle/walk budgets are not completed replacement sheets. The articulated rig was rejected and removed. Preserve the original sprite as the style authority, whole-body weight, rigid axe geometry and simple hands drawn into each pose.

**Next main milestone:** connect the ordinary Tutorial gameplay loop. Add future animation work only as its gameplay milestone needs it, using the ranked [animation backlog](AnimationPlan.md). Existing art and fallbacks are accepted and do not block Tutorial integration.

## Recommended slice finish line

A fresh player can learn movement and combat, awaken Recall at the ruin, use it intentionally, defeat the first enemy, collect a shard, rest/save, and enter a small overworld pocket. That pocket should demonstrate one useful Recall encounter/puzzle and award the next two earned shards, allowing the first three-shard upgrade purchase. End the slice there with a clear completion point.

This is a proposed bounded slice, not a requirement to build the full overworld, three trials, dungeon or boss now. Heart-fragment systems already exist, but their new-world introduction can wait until the main progression is clear.

## Build order

1. **Animation milestone accepted.** Preserve the current art and working fallbacks. Chip away at hurt/defeat, remaining combat and interaction animation as gameplay development needs them; no dedicated animation pass blocks the next steps.
2. **Connect the ordinary Tutorial loop.** Wire real target hit/retrieval feedback, melee/dodge practice, one easy enemy, its unique shard reward, the bonfire and save/respawn. Reuse working mechanics with a dedicated save key and stable IDs. Add the intended wolf visuals and tuning; preserve PrototypeLoop as a regression scene.
3. **Implement the Recall awakening.** Audit/replace the existing `RecallAltar` so it cannot bypass the lesson. Require an actual outbound hatchet hit on the ancient stone. Briefly lock relevant controls, play the awakening, unlock Recall, return the axe, restore controls and save once. Handle interruption/reload safely. Follow with one manual Recall exercise; keep missed throws retrievable.
4. **Teach and present the complete route.** Add a small event-driven Tutorial guide. Advance on successful actions, show one binding-aware contextual hint, respect early successes and saved milestones, and avoid unnecessary forced gates. Complete HUD/interaction feedback and essential combat, pickup, awakening and fire audio. Check route pacing, camera bounds and canopy/roof readability.
5. **Prove progression in Green Lowlands.** Scope and build a small entry pocket using the established Field & Old Road palette: arrival/spawn, one Recall positioning encounter or puzzle, a worthwhile optional detour, two further earned shards and a bonfire upgrade opportunity. Implement scene transition/return handling and shared progression/checkpoint ownership. The first three-shard purchase is the slice finish line; expand the region after that works.
6. **Validate and package.** Test a fresh run, death before/after the first fire, reload, interrupted awakening, missed throws, unique rewards, scene travel and upgrade persistence. Use relevant existing checks and isolated saves. Profile camera/woodland rendering and produce a fresh WebGL build with the correct scenes enabled. Add atmosphere last; renderer migration is conditional.

The intended player journey is **home/movement → stump pickup → throw/retrieve → melee/dodge → southern bridge → stone awakening → manual Recall practice → easy enemy/first shard → bonfire → northeast exit to Green Lowlands**. This supersedes the older idea that the first overworld puzzle grants Recall.

## Deferred work recovered from the sessions

This register separates explicit postponements, unfinished planned work and optional ideas. Completing an old placeholder system in PrototypeLoop does not mean its production Tutorial integration is finished.

| Item | Status and return point |
| --- | --- |
| Dedicated moving-aim art and moving catches | Explicitly deferred after the rejected gait study. Keep the working walking/carrying fallback; later draw forward/backward/strafe gaits and compatible whole-body catches. |
| North catch revision | Explicitly accepted for now; revisit only if gameplay exposes a distracting mismatch. |
| East, north and south throw revisions | Explicitly accepted for now. Preserve current tap/hold timing and motion. |
| South overhead opening strike | Accepted for now after the redraw; optional future refinement, not a missing action. |
| Aim presentation polish | Earlier placeholder acceptance has partly been addressed by stationary throw/hold art. Further hold variation/guide polish remains a later review, not missing aim mechanics. |
| Recall stone awakening | Explicitly postponed while building the world. Still required for Tutorial; now listed in build step 3. |
| Combat feel follow-ups | Early swing aim adjustment, late-recovery dodge buffering/cancel tuning and restrained cloth/satchel settling remain planned review items. Existing combo buffering works; swings currently commit direction when started and attacks reject dashes. Do not describe those proposed refinements as implemented. |
| Final player VFX and sound | Saved until body motion reads clearly. Add restrained separate spin streaks/trails, impact accents, release/flight/catch audio and other action sounds. Basic prototype trails, impact pause and temporary hit audio already exist. |
| Atmosphere and renderer | Lighting, shadows, mist/fog, subtle ambient particles/smoke/light shafts remain end-stage polish. URP 2D migration and normal maps are conditional options, not requirements. |
| Camera and overhead visibility | Bounds plus canopy/roof fading, cutaway or silhouette treatment remain open. Follow/zoom, overhead sorting and forest backdrop coverage already work. |
| Rear-Recall knockdown/stun | Explicitly deferred. Rear return hits already bypass shield protection and deal damage; the additional special reaction does not exist yet. |
| Upgrade rounds 2–6 | Accepted future design; names/effects below are drafts and need implementation, combination testing, trajectory feedback and costs. First-tier one-of-three purchase already works. |
| Random rewards and skilled-Recall bonus | Random Sun Shard drops and better chances for skilled multi-target Recall remain future work. Keep mandatory progression guaranteed; existing fixed one-time rewards are intentional. |
| Merchants and Bronze Coins | Explicitly deferred broader-game economy; not needed for the opening slice. |
| Heart-fragment introduction | Mechanics are complete: three fragments add 20 permanent max HP. Introduce them through a later optional discovery after the main route is understood. Production-world placement/pacing remains open. |
| Controller/gamepad support | Explicitly removed from current scope. Keyboard/mouse only; reconsider only if requested. |
| Project title and detailed lore | Rename was discussed without a new name being chosen. The cursed king premise remains; “Broken Sky-King” is provisional. Settle names/details when useful, without blocking gameplay. |
| Optional offhand shield and village banners | Discussed possibilities, not approved feature commitments. Keep the offhand expressive/free; current environment kit needs no extra banner work. |

**Updated decision:** all current player art is accepted. Dedicated diagonals, remaining combo/cleave, reactions/interactions and possible idle/walk expansion are future incremental work in the ranked animation backlog. Wolf presentation, Tutorial UI, production checkpoints and cross-scene progression remain active game-development work.

### Later upgrade rounds

One mutually exclusive choice per round; later costs, exact values and ordering remain unbalanced drafts. Round 1 is implemented: Quick Hands / Sweeping Edge / Wide Cleave.

| Round | Retained choices and intended effects |
| --- | --- |
| 2 — Outbound flight | **Crescent Flight:** curved outbound path. **Skipping Stone:** ricochet to one nearby enemy. **Thread the Needle:** pierce one enemy. |
| 3 — Return | **Snap Return:** faster throw/Recall. **Orbit Catch:** one small orbit before catching. **Catch and Cut:** brief, non-stacking next-slash bonus after catch. |
| 4 — Earned damage | **Deep Notch:** stronger third combo hit. **Heavy Head:** stronger fully charged cleave. **Longshot:** capped outbound damage bonus with distance. |
| 5 — Positioning | **Sling Step:** route Recall through the dash start once. **Ground Spinner:** a small cutting circle at unobstructed maximum throw range. **Reaping Trail:** short-lived slowing Recall trail. |
| 6 — Signature trick | **Crosscut:** full cleave emits a short crescent, without double-hitting spin targets. **Bank Shot:** one wall rebound within the travel budget. **Patient Hunter:** a planted axe builds a capped bonus for its first return hit. |

### Beyond the opening slice

The broader plan remains **Green Lowlands → Barrow Woods → Storm Highlands → ancient dungeon → boss**, with three major regional puzzles/trials opening progression and a final fight testing Recall mastery. Build complementary positioning enemies, worthwhile secrets and environmental storytelling as each region needs them. Enemy lists (beasts, scavengers, shielded warriors, ranged enemies and ancient guardians/constructs) are regional direction, not a request to build every type for Tutorial. Keep the Bronze Age/sun-wheel/horse/wolf/storm motifs and the shift from familiar countryside to ancient monumental ruins.

**Closed or superseded, not backlog:** tilemaps/animated water, house-scale correction, core health/stamina/combat, prototype saving/bonfire travel, first-tier upgrades and heart mechanics, player/weapon integration, cardinal sprint/dash/throw/catch. The rig is retired, not awaiting another attempt. Four-fragment health and first-overworld Recall-unlock ideas were replaced by the current three-fragment rule and Tutorial stone sequence.

History audit: nine local project root sessions dated 20–27 September, including the older design summaries preserved there, cross-checked against current docs/code and the open Unity scene. Key records are sessions `01a0c13b-4a63-75b3-8803-13ff70f829a6` (combat/progression), `01a0cfdb-0efa-7280-8a18-5659e13c5bf1` (world/Tutorial) and `01a0dafa-88d3-70e1-a741-50670a608c36` (animation/current handoff). Original session logs remain untouched; this register retains the decisions without copying whole chats.

## Workspace conventions

- Keep `Tutorial` as the production scene, `PrototypeLoop` as the mechanics reference, `DemoTutorial` as the art reference and `MovementPlayground` as the small movement test.
- Keep accepted inputs and registration masters in `ArtSource`, current previews in `Docs/Art`, exporters in `Tools/Art/Player` and `Tools/Art/World`, captures in `Tools/Capture`, and focused checks grouped under `Tools/Verification`.
- Keep current previews in `Docs/Art/Tutorial/Previews`. The latest Recall-route sketch supersedes earlier flow/blockout drafts.
- `tmp/` and Unity `Captures/` are ignored scratch space. The cleanup rollback archive and local Pillow cache are retained there; disposable old captures/recovery copies are removed. Published root `Build/`, `TemplateData/` and `index.html` are preserved.
