# Changelog

## 29SEP2026 - Tutorial guide, Recall awakening and movement feel

- Added a step-by-step Tutorial guide with one contextual hint at a time, saved progress and lessons completed from real actions: throwing at targets, dummy strikes, dodging, a Recall drill, the first shard, resting and the exit trail.
- Added a contextual HUD: health and stamina shown when relevant, interaction prompts beside objects, a weapon-away indicator, reward receipts and a Tab status panel.
- The ancient sun-wheel stone now awakens Recall when struck by a thrown axe, with new pixel art.
- Movement now glides and rebounds lightly off walls, with speed-scaled dust trails and skid clouds.
- Route-side trees now block the player and layer correctly; the Tutorial uses the shared follow camera and a tidier hierarchy.
- Added an F3 developer stats panel in the Editor and development builds.

## 28SEP2026 - New hero and combat feel

- Replaced the hero with eight-direction PixelLab artwork, animated walking, running and dashing, and a bronze halberd carried at his side.
- Reworked the light combo into two sweeping attacks and a powerful thrust with a short forward lunge.
- Strengthened knockback, hit particles and killing-blow feedback while shortening enemy stuns.
- Added a wolf detection “!” cue and Left Alt freelook with a three-tile limit.
- Added an Escape pause menu with checkpoint restart and quit.
- Retired the old character pipeline and DemoTutorial scene; Tutorial is now the main world reference.
- Renamed “hatchet” to “axe,” preserving existing saves and upgrades.

## 27SEP2026 - Wolf enemy, combat feedback and Tutorial integration

- Added the animated wolf enemy and enemy health bars.
- Added the aim marker, melee coverage effects, impact sounds and particles, plus a combat preview tool.
- Added F-to-pick-up interactions and clearer axe flight effects; thrown axes still return to hand when approached.
- Integrated the Tutorial's first enemy, Sun Shard reward and bonfire/checkpoint, with separate saves and a compact HUD.
- Unified player and weapon prefabs and completed the previous hero's catch animations.
- Renamed the game to **Road of the Old King**.

## 26SEP2026 - Directional movement and combat

- Completed four-direction sprint, dash, opening swings and stationary throws, with armed/empty-hand presentation and movement-driven foot timing.
- Established consistent character proportions, muted colors, patterned cap trim, simple hands and axe geometry to preserve the original sprite's old-school feel.
- Kept moving aim functional with existing walking art; dedicated aim-walk and moving-catch artwork remain future work.

## 25SEP2026 - Player and axe integration

- Integrated the animated Tutorial player, directional locomotion, idle breathing, follow/zoom camera and stump axe pickup/carrying; refined weapon scale and clarity.
- Added the initial combo presentation and practice-target recoil, wood chips and impact audio. Retired the rejected articulated rig and selected authored full-body sprites for the replacement.
- Implemented tap E to quick throw, hold E to aim and release, moving aim, a collision-aware guide and shared release timing. Gameplay now owns movement limits, once-per-throw stamina cost and interruption/Recall handling.
- Completed the east/north sprite proof: 36 matched forehand/throw/catch body cels and eight fast axe-spin cels. Matched pickup/carry and north combo axe views to the drawn weapon, corrected catch scale and perspective.
- Connected the east/north opening swing, stationary throw/catch and fast spinning axe to live Tutorial gameplay, with synchronized release/Recall handoffs, movement fallbacks.
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

- Added axe pickup, mouse aiming, a buffered three-hit combo, charged cleave, throwing, retrieval and Recall.
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
