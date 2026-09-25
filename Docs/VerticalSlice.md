# Vertical slice: current state and next steps

Updated 25 September 2026. This is the development plan; [Tutorial/Plan](Art/Tutorial/Plan.md) owns world layout and [SystemsOverview](SystemsOverview.md) owns technical behavior.

## Where we are

Tutorial is walkable with the animated player, idle breathing, follow camera and hatchet pickup/carrying. Its gameplay progression is still pending, so it is not yet a complete vertical slice.

| Area | Actual state |
| --- | --- |
| Tutorial world | Village, practice terrace, southern bridge, Recall ruins, safe practice space, enemy clearing, rest site and exit are built. World art and palette are established. |
| Tutorial gameplay | Player, follow/zoom camera, real stump hatchet pickup and practice dummy are present. Checkpoint session, working bonfire and enemy remain to be connected. Recall stone and practice stands are static art. |
| Player | TutorialPlayer uses the production character sheet and idle breathing. Four-direction standing/walk clips follow actual travel, including sprint cadence and wall stops. The separate hatchet follows all 16 hand positions while carrying. Dodge uses a held stride pose until dedicated action art is made. PrototypeLoop retains its reference visuals. |
| Reusable mechanics | PrototypeLoop has movement, sprint/stamina, dodge, melee, throw/retrieve/Recall, health, a telegraphed melee enemy, bonfires, saving, a Recall puzzle, shards/upgrades and heart fragments. Reuse these systems. |
| New Recall sequence | Designed only. Current unlock is an overlap pickup in PrototypeLoop; throwing into the ancient stone, control lock and awakening/return sequence need implementation. |
| Presentation | Final player/weapon animation, wolf art/animation, contextual tutorial UI, audio and effects remain. Lighting/fog and a possible URP 2D migration stay late decisions. |
| Builds | Only PrototypeLoop is enabled in build settings. Existing browser output predates recent work; it is not the new tutorial. No overworld scene or exit transition exists yet. |

Route clearance, camera framing, locomotion, carrying and idle checks are recorded in [SystemsOverview](SystemsOverview.md#verification). Continue playtesting feel and route pacing; camera bounds and occlusion fading remain open presentation work.

## Recommended slice finish line

A fresh player can learn movement and combat, awaken Recall at the ruin, use it intentionally, defeat the first enemy, collect a shard, rest/save, and enter a small overworld pocket. That pocket should demonstrate one useful Recall encounter/puzzle and award the next two earned shards, allowing the first three-shard upgrade purchase. End the slice there with a clear completion point.

This is a proposed bounded slice, not a requirement to build the full overworld, three trials, dungeon or boss now. Heart-fragment systems already exist, but their new-world introduction can wait until the main progression is clear.

## Build order

1. **Player locomotion integrated.** The approved player, native directional clips and existing camera are connected; movement, stopping, turns, sprinting and route clearance are verified. Review the feel and pacing in Play Mode before expanding the map. Keep the weapon separate; revisit camera bounds and occlusion fading during presentation polish.
2. **Connect the ordinary tutorial loop.** Stump pickup and carrying are connected and verified. Next add attack/throw presentation, real target hit/retrieval feedback, melee/dodge practice, one easy enemy, its one-time shard reward, the bonfire and save/respawn. Use a dedicated tutorial save key and stable IDs; preserve the prototype as a regression scene.
3. **Implement the Recall awakening.** Require an actual outbound hatchet hit on the stone. Briefly lock relevant controls, play the awakening, unlock Recall, return the axe, restore controls and save once. Handle interruption/reload safely. Follow with one manual Recall exercise; keep missed throws retrievable.
4. **Teach and present the complete route.** Add a small event-driven guide for this scene instead of copying prototype gates and its old sequence. Advance on successful actions, show one contextual hint, respect active bindings and saved milestones. Replace the sentinel placeholder with the intended wolf presentation/tuning; add essential combat, pickup and fire audio/feedback.
5. **Prove progression beyond the tutorial.** Build the small overworld pocket, transition/spawn handling, Recall challenge, next two shard rewards and first upgrade opportunity. Verify progress and checkpoint ownership across scenes; current reload/travel behavior is primarily prototype/same-scene based.
6. **Validate and package.** Test a fresh run, death before/after the first fire, reload, interrupted awakening, missed throws, unique rewards, scene travel and upgrade persistence. Run relevant existing regression checks on isolated saves. Profile camera/woodland rendering and produce a fresh WebGL build after updating the build scene list. Atmosphere is the last polish pass, with renderer migration only if justified.

**Current milestone:** the right-facing light-chop sample has full-body poses, planted movement, synchronized contact, local hit pause and practice-target recoil/chips. Review its in-game feel, then expand to four directions, distinct combo poses and throwing/catching, following the [combat animation plan](Art/Player/README.md#combat-and-interaction-plan). Connect the enemy and first bonfire before the Recall awakening.

## Workspace conventions

- Keep `Tutorial` as the production scene, `PrototypeLoop` as the mechanics reference, `DemoTutorial` as the art reference and `MovementPlayground` as the small movement test.
- Keep approved art sources, native generators in `Tools/Art`, and checks in `Tools/Verification`; they remain useful authoring/regression inputs.
- Keep current previews in `Docs/Art/Tutorial/Previews`. The latest Recall-route sketch supersedes earlier flow/blockout drafts.
- `tmp/` and Unity `Captures/` are ignored scratch space for recovery scenes, captures and the local Pillow cache. Published root `Build/`, `TemplateData/` and `index.html` are preserved.
