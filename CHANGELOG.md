# Changelog

Listed newest first.

## 22SEP2026 - Sun Shards and first weapon upgrade

### Added

- Three one-time Sun Shards in PrototypeLoop: a sentinel drop, an automatic throw/Recall puzzle reward, and an exploration pickup on a short optional north trail beyond the puzzle door.
- A reusable gold shard pickup prefab, persistent reward IDs, and an unspent-shard HUD counter. Rewards save immediately, including before the first bonfire, and remain collected through rest, death, travel, and reload. Unclaimed sentinel drops remain available; respawned enemies cannot be farmed for shards.
- A first upgrade tier at the second bonfire costing 3 shards: Quick Hands (20% faster light attacks), Sweeping Edge (150-degree light arc), or Wide Cleave (2.6-unit radius). Damage and stamina costs stay unchanged. Players review and confirm one permanent choice, discarding the other two for that run.
- Data assets for upgrade choices and ordered tiers, purchase rules independent of UI/storage, and runtime stat composition shared by damage detection and visuals without changing the base weapon settings.
- Backward-compatible save fields for shard balance and selected upgrades, plus 39 repeatable shard/upgrade checks. Updated the systems overview; WebGL has not been rebuilt.

## 22SEP2026 - Combat balance

### Added

- Added a Spacebar dash: 2.2 units over 0.18 seconds, costing 25 stamina, with a 0.1-second opening dodge window and 0.35-second recovery before another dash. Direction follows movement or last facing when idle.
- Added a Space / short dash / dodge hint in the sentinel arena and updated the HUD and lesson text. Dash respects collisions, cannot cancel attacks, does not repeat on held Space, and resets at bonfires or respawn.
- Added 39 dash checks covering input, physics, protection, stamina, attack/sprint transitions, interruptions, and bonfires.
- Added a short Shift-to-sprint hint beneath the Back to Dummies trail sign.
- Added automatic hatchet Recall beyond 10 units of player separation once Recall is unlocked; it uses normal return damage, remains free, and leaves room for the marked puzzle repositioning.
- Added Shift sprint with equal diagonal speed, a 100-point stamina pool, delayed recovery, and a placeholder stamina bar with low-stamina/rejected-action feedback.
- Added stamina costs to the light combo (18/18/24), charged cleave (35), and throw (25). Recall remains free. Charging pays once up front, cannot regenerate while held, and does not refund cancelled charges.
- Added 50 repeatable stamina checks for keyboard bindings, exhaustion, action gating, recovery, charge cancellation, free Recall, bonfires, and death; all 271 current gameplay assertions pass, including 20 automatic Recall and 39 dash checks.

### Changed

- Sprint moves at 7.2 units/second, spends 20 stamina/second, and needs 20 stamina to start; walking remains free at 4.5. Recovery restores 25 stamina/second after 0.8 seconds without spending or performing melee.
- Player, sentinel, and practice dummy health now use 100-point pools. Light combo damage is 10/10/15, full cleave 30, throw 15, Recall 10, and sentinel strikes 20.
- Melee/charging, stagger, lost focus, disabled controls, fire menus, and death stop sprinting. Exhaustion falls back to walking.
- Bonfire rest, travel, and respawn refill stamina. Expanded the systems overview and updated existing combat/route/puzzle checks for the new damage scale.

## 21SEP2026 - PRD 02

### Added

- Added two bonfires after the sentinel and beyond the new puzzle door. F opens a rest menu that heals, saves, sets the checkpoint, resets combat targets, and allows travel to discovered fires.
- Added a short throw/Recall puzzle: hit the gold target with an outbound throw, reposition to the blue floor mark, then recall through the blue target to open the next door.
- Added local checkpoint saves for the hatchet, Recall, discovered fires, cleared route obstacles, lesson milestones, and completed puzzles, with a confirmed New run option.
- Added 38 repeatable bonfire/puzzle checks; verified checkpoint respawn, save loading across Play Mode sessions, travel, menu input, and physical door access.
- Added a linear prototype teaching route with step-by-step instructions and gates: hatchet pickup, slash/throw/retrieval, bushes, cracked stone, Recall unlock, return to the same dummies, then a separate enemy arena.
- Added a reusable six-health melee sentinel with approach behavior, a visible locked-direction wind-up, a dodgeable strike, recovery, stagger interruption, and a home-area leash.
- Added five-health player damage, short post-hit immunity, knockback, a placeholder HP display, and a defeat/restart prompt.
- Added repeatable checks for route progression, enemy behavior, player health, and defeat.
- Added a minimal black-and-white game-loop SVG to the README, showing the core loop, hatchet loop, and planned progression.
- Added a reusable placeholder hatchet prefab and a walk-over pickup near the Tutorial spawn.
- Added mouse aiming, a buffered three-hit light combo on left click, and a charged spinning cleave on right-click hold/release.
- Added E to throw the hatchet, sticking at obstacles or maximum range, with walk-up retrieval.
- Added a separate Recall unlock at the northern Tutorial altar. Once unlocked, E recalls the hatchet through targets to the moving player.
- Added charge and swing effects, a flight trail, and a Tutorial controls/status overlay.
- Added practice dummies with health, knockback, stagger, and automatic reset; shielded dummies and cracked stones require a full cleave to break their protection. Bushes can also be chopped.
- Separated combat input, weapon state, hit detection, damage responses, and visuals, with a shared hatchet settings asset for tuning.
- Documented prototype controls, tuning, puzzle integration, and repeatable Play Mode checks in `Docs/SystemsOverview.md`.
- Added `Docs/SystemsOverview.md`, an overview of the implemented systems, their design, and their common settings.

### Changed

- Expanded the prototype route beyond the enemy arena with a campfire, a marked Recall puzzle, and an exit fire. Completed routes and puzzle doors stay open when resting resets enemies.
- Defeat now offers R / Return to bonfire after discovering a checkpoint; before the first fire it restarts at the original spawn. Saved progress loads automatically on a new session.
- Renamed `Tutorial` to `PrototypeLoop`, preserving its scene GUID and updating the enabled build scene. Expanded the tile layout into a guided loop with water boundaries and a northwest enemy corner.
- Player movement now respects hit stagger, and player defeat cancels active hatchet damage and disables controls.
- Updated systems/weapon documentation and moved the existing hatchet verification fixtures outside the map so they remain independent of the new layout.
- Made light attacks faster, with a brief wind-up, a quick slash, and a short recovery. Damage is limited to the slash phase, and queued combo clicks are more forgiving.
- Fixed the backhand slash effect to follow the blade's direction, added a tapered crescent that fades quickly, and added three small combo pips above the player.
- Returning hatchets now damage shielded targets from behind without breaking their shields. Front and side Recall hits remain blocked; full cleaves still break guards.
- Added a facing arrow to the shield practice dummy and a controls hint for rear Recall hits. Special rear-hit knockdown/stun remains planned for later.
- Replaced the README artwork with the new top and bottom banners stored in the repository.
- Expanded WASD/arrow-key movement from four to eight directions, with equal diagonal speed.
- Kept four-direction visual facing while preserving the last full movement direction when idle.
- Simplified movement input to keyboard controls; mouse-wheel camera zoom remains available.

## 20SEP2026

### Added

- Imported 64 pixel-art tiles at 16x16 pixels, including grass, dirt, stone, flowers, rocks, water, and terrain transitions.
- Added a ground Tile Palette and sample-map prefab for painting and testing levels.
- Created the `Tutorial` scene with a grassy clearing, dirt loop, flowers, and a small pond using the imported tiles.
- Added solid pond and shoreline boundaries, plus the existing player and follow camera, to make Tutorial ready to explore.
- Added smooth mouse-wheel camera zoom: scroll up to zoom in and down to zoom out, limited to orthographic sizes 3-8 with a starting size of 5.5.
- Exposed zoom limits, sensitivity, and smoothing in the camera Inspector.

### Changed

- Set `Tutorial` as the enabled build scene in place of `MovementPlayground`.
- Updated the browser build with the tutorial area and camera zoom.

## 14SEP2026 - DEV 01

### Added

- Continuous four-direction player movement using WASD or arrow keys.
- Direction priority based on the most recently pressed held direction.
- Physics-based collision with obstacles and boundary walls.
- Smooth camera follow with adjustable smoothing.
- Basic test area with a reference grid and solid obstacles.
- Placeholder player sprite with a facing-direction marker.
- Reusable player, obstacle, and camera prefabs.
- Organized folders for future gameplay, art, animation, audio, and UI work.
- Separate input, movement, and camera components, with an interchangeable input interface.
- Pixel-art title and decorative banner artwork for the README.
