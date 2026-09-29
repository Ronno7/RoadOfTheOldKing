# Changelog

## 28SEP2026 - New hero and PixelLab animation pipeline

- Replaced the player with a new arm-less hero in a blue hooded cloak, generated with PixelLab in eight directions at the world's native 16 pixels per unit. Standing views, an eight-frame walk and run, and a four-frame dash are live; further animations arrive one at a time.
- Replaced the layered four-direction animation system with one state-driven presenter and a one-command PixelLab importer. Walk and run follow distance travelled, actions follow the weapon's clocks, facing uses eight directions and keeps diagonals on key release, and states without artwork fall back to the standing views.
- Added an aged bronze halberd as the weapon sprite, outlined to match the hero, carried upright on the edge of his right side out of combat with per-direction mirroring and layering, a cloak fold wrapping the grip, and a bob that follows his walk; it starts planted in the Tutorial stump.
- Moved light-hit pause into the weapon and its settings, unchanged at 0.045 seconds (1.4x on the finisher).
- Retired the previous character art, its animation assets and tools (samples kept in `Docs/Art/Player/Legacy`), the unused old axe sprites, and the world-only DemoTutorial scene; Tutorial is now the single world reference.
- Renamed the weapon from "hatchet" to "axe" throughout code, assets, scenes and documentation. Existing saves keep their axe ownership and purchased upgrades.

## 27SEP2026 - Wolf enemy, combat feedback and Tutorial integration

- Added compact two-tone enemy health bars above the Wolf and MeleeSentinel, reading current and maximum health, hiding on defeat and returning on rest.
- Added a short impact sound and directional pixel-fleck burst to enemies and practice targets, driven by accepted damage and contact direction. Misses and guarded hits remain silent; lethal hits retain impact feedback.
- Added an original low-resolution wolf with twenty 48x48 sprites, mirrored side views, movement-driven gait, readable bite windup, hit reactions and directional death poses. The Wolf prefab reuses existing enemy combat, rewards and rest/reset behavior.
- Added a cursor-following aim marker and restrained light-melee windup, active damage footprint and slash feedback. Attack reach, width and timing follow the weapon's gameplay rules, including combos, upgrades and hit pause; the effect shows the full potential sector while damage remains terrain-blocked.
- Consolidated the finished character into one standard Player prefab shared by Tutorial and test scenes.
- Promoted the finished weapon to an independent `Axe.prefab` for gameplay and combat previews; retained the old weapon as `PrototypeAxe.prefab` for PrototypeLoop, preserving existing scene references and tuning.
- Added the Combat Effects Preview editor window.
- Added explicit F interactions for initial equipment and item pickups, with contextual prompts and one-item-per-press selection shared with bonfire controls; thrown axes retain automatic walk-over retrieval for combat.
- Simplified detached axe presentation to one larger sprite with faster spin, a short trail and consistent grounded placement; modestly increased Tutorial throw speed and reach.
- Established the Tutorial enemy, unique Sun Shard reward and bonfire/checkpoint setup using existing prefabs, with separate Tutorial saves and a compact HUD.
- Renamed the game to **Road of the Old King**, including Unity product settings, web presentation, editor menus and project folder paths.
- Completed four-direction catch coverage, finishing the current player animation set. Accepted the artwork for now and moved future animation work into a ranked backlog so development can focus on Tutorial gameplay.
- Organized animation definitions, sprite sheets, source art, previews and tools by purpose. Removed obsolete studies and temporary files while preserving working assets and useful development references.
- Separated review-only reveal sheets from runtime animation assets, preserving editor inspection and source rebuilds without changing gameplay artwork or timing.

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
