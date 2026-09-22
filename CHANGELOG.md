# Changelog

Listed newest first.

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
