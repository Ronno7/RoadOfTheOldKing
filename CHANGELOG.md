# Changelog

Listed newest first.

## [Unreleased]

### Added

- Added a reusable placeholder hatchet prefab and a walk-over pickup near the Tutorial spawn.
- Added mouse aiming, a buffered three-hit light combo on left click, and a charged spinning cleave on right-click hold/release.
- Added E to throw the hatchet, sticking at obstacles or maximum range, with walk-up retrieval.
- Added a separate Recall unlock at the northern Tutorial altar. Once unlocked, E recalls the hatchet through targets to the moving player.
- Added charge and swing effects, a flight trail, and a Tutorial controls/status overlay.
- Added practice dummies with health, knockback, stagger, and automatic reset; shielded dummies and cracked stones require a full cleave to break their protection. Bushes can also be chopped.
- Separated combat input, weapon state, hit detection, damage responses, and visuals, with a shared hatchet settings asset for tuning.
- Documented prototype controls, tuning, puzzle integration, and repeatable Play Mode checks in `Docs/HatchetPrototype.md`.
- Added `Docs/SystemsOverview.md`, an overview of the implemented systems, their design, and their common settings.

### Changed

- Replaced the README artwork with the new top and bottom banners stored in the repository.
- Expanded WASD/arrow-key movement from four to eight directions, with equal diagonal speed.
- Kept four-direction visual facing while preserving the last full movement direction when idle.
- Simplified movement input to keyboard controls; mouse-wheel camera zoom remains available.

## 2026-09-20

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

## [Dev1]

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
