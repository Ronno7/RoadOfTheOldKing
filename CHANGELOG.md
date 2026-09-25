# Changelog

## 25SEP2026 - Player and hatchet integration

- Added the animated player to Tutorial with four-direction locomotion, sprint cadence, idle breathing and the follow/zoom camera.
- Added the hatchet pickup at the stump and frame-specific hand grips for carrying, turning, sprinting and dodging.
- Increased the hatchet's visual size by 30% and reduced its pixel density to match the character.
- Added a right-facing light-chop sample with four action poses, planted movement, hand-anchored weapon motion, synchronized damage and impact-only hit pause.
- Added practice-target artwork, recoil, wood chips and temporary impact audio; retained a short in-game combat milestone capture.
- Added player, pickup, carry and idle verification checks; verified access to all 13 tutorial route stops.
- Consolidated development and art documentation, removed obsolete previews, and simplified the changelog.

## 24SEP2026 - Player art and tutorial world

- Added the 16-frame weapon-free character sheet with simplified shading and softer outlines.
- Rebuilt the houses as a 6x5 workshop and an 8x6 thatched longhouse.
- Built Tutorial terrain, paths, village dressing, forest boundaries, practice terrace and separate collision.
- Fixed cliff and ledge seams, ramp returns and waterfall direction; increased waterfall speed and landing splash.
- Moved the bridge south and added the winding forest road, Recall ruins, stone placeholder and safe practice clearing.
- Extended surrounding woodland for camera coverage and checked route clearance.

## 23SEP2026 - Environment and decoration

- Added 31 environment objects and 12 decoration designs using the tutorial palette.
- Dressed DemoTutorial with village work areas, a practice yard, bridge and ruins.
- Added separate collision footprints and overhead sorting for trees, roofs and ruins.
- Consolidated production assets, painting palettes and rebuild sources.

## 22SEP2026 - Art kits and progression

- Added ground, terrain and path kits with automatic connections, painting palettes and animated water.
- Added DemoTutorial as the art reference scene and consolidated the blank zone template.
- Added sprinting, stamina costs and recovery, plus a short directional dash with an opening dodge window.
- Rebalanced player, enemy and dummy health to 100-point pools and updated attack damage.
- Added automatic Recall beyond 10 units after unlocking the ability.
- Added three unique Sun Shards, persistent rewards and a three-shard weapon upgrade choice in PrototypeLoop.
- Added heart fragments: every three grant 20 permanent maximum HP, with persistent collection and HUD feedback.
- Added verification for stamina, dodge, Recall, rewards, upgrades and heart fragments.

## 21SEP2026 - Combat and prototype loop

- Added hatchet pickup, mouse aiming, a buffered three-hit combo, charged cleave, throwing, retrieval and Recall.
- Added practice targets, shields, breakable bushes and cracked stone, plus a telegraphed melee enemy.
- Added player health, damage immunity, knockback, defeat and restart handling.
- Built the guided prototype route, Recall puzzle, bonfires, checkpoint saves and travel between discovered fires.
- Added persistent lesson and puzzle progress, checkpoint respawning and a new-run option.
- Renamed the original Tutorial scene to PrototypeLoop and set it as the build scene.
- Added attack effects and combo indicators; fixed backhand slash direction and rear Recall hits on shields.
- Changed movement to eight directions with normalized diagonal speed and four-direction visual facing.
- Added gameplay verification, the systems overview and the game-loop diagram.

## 20SEP2026 - Initial tutorial prototype

- Added 64 pixel-art tiles, a painting palette and a sample-map prefab.
- Built the initial tutorial clearing with paths, flowers, a pond and solid boundaries.
- Added mouse-wheel camera zoom with a starting size of 5.5 and limits of 3-8.
- Updated the browser build with the tutorial area and camera zoom.

## 14SEP2026 - Movement foundation

- Added keyboard movement, physics collision, smooth camera follow and a movement test scene.
- Added reusable player, obstacle and camera prefabs with separate input and movement components.
- Organized project folders and added title and banner artwork.
