# Changelog

## 27SEP2026 - Animation milestone, cleanup and Tutorial integration

- Added explicit F interactions for initial equipment and item pickups, with contextual prompts and one-item-per-press selection shared with bonfire controls; thrown hatchets retain automatic walk-over retrieval for combat.
- Simplified detached hatchet presentation to one larger sprite with faster spin, a short trail and consistent grounded placement; modestly increased Tutorial throw speed and reach.
- Established the Tutorial enemy, unique Sun Shard reward and bonfire/checkpoint setup using existing prefabs, with separate Tutorial saves and a compact HUD.
- Renamed the game to **Road of the Old King**, including Unity product settings, web presentation, editor menus and project folder paths.
- Completed four-direction catch coverage, finishing the current player animation set. Accepted the artwork for now and moved future animation work into a ranked backlog so development can focus on Tutorial gameplay.
- Organized animation definitions, sprite sheets, source art, previews and tools by purpose. Removed obsolete studies and temporary files while preserving working assets and useful development references.
- Separated review-only reveal sheets from runtime animation assets, preserving editor inspection and source rebuilds without changing gameplay artwork or timing.

## 26SEP2026 - Directional movement and combat

- Completed four-direction sprint, dash, opening swings and stationary throws, with armed/empty-hand presentation and movement-driven foot timing.
- Established consistent character proportions, muted colors, patterned cap trim, simple hands and hatchet geometry to preserve the original sprite's old-school feel.
- Kept moving aim functional with existing walking art; dedicated aim-walk and moving-catch artwork remain future work.

## 25SEP2026 - Player and hatchet integration

- Integrated the animated Tutorial player, directional locomotion, idle breathing, follow/zoom camera and stump hatchet pickup/carrying; refined weapon scale and clarity.
- Added the initial combo presentation and practice-target recoil, wood chips and impact audio. Retired the rejected articulated rig and selected authored full-body sprites for the replacement.
- Implemented tap E to quick throw, hold E to aim and release, moving aim, a collision-aware guide and shared release timing. Gameplay now owns movement limits, once-per-throw stamina cost and interruption/Recall handling.
- Completed the east/north sprite proof: 36 matched forehand/throw/catch body cels and eight fast axe-spin cels. Matched pickup/carry and north combo axe views to the drawn weapon, corrected catch scale and perspective, softened heavy outer borders and redrew simple blob hands with continuous wrists.
- Connected the east/north opening swing, stationary throw/catch and fast spinning axe to live Tutorial gameplay, with synchronized release/Recall handoffs, movement fallbacks and a native-scale milestone GIF. Gameplay and import checks cover the integration.
- Consolidated animation documentation and rebuild instructions, preserved milestone GIFs and source art, and removed the superseded browser review and disposable captures.

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
