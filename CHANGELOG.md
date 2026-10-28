# Changelog

## Unreleased

**Added**
- Scavengers watch you for 0.9 s before chasing; their suspicion meter drains if you break sight.
- Scavengers fight with a lunging thrust and a slow armoured overhead that leaves them open for 1.1 s; they take turns and hold back while waiting.
- Scavengers and boars have idle animations; boars play a run cycle when they charge.

**Changed**
- Road from Ardan's Edge to the Lowlands rebuilt: 188 units long (was 74), three fights (was two).
- Lantern casts warm, flickering light with soft shadows behind walls (was a hard-edged cut-out).
- Thrown axe flies 50% faster (full range in 0.39 s, was 0.58 s).
- Stamina arc shows whenever stamina is below full (was combat only).
- Enemies chase until they stray past their leash (wolves 20, scavengers 10–11, boar 12); hitting or following them re-engages.
- Tutorial hints skip lessons you've passed by the bridge, the Recall stone or a bonfire.
- Barrow Approach is closed until Barrow Woods is rebuilt.

**Fixed**
- The third swing-practice bush stays cleared after leaving Ardan's Edge and returning.
- Wolves dodge when a cleave is released, not when it starts charging.
- Charge and lunge warnings match the attack's real width (boar charge 0.67 units each side, was 1.05).
- Entering the Sunken Storehouse showed "Green Lowlands" as the place name.

## 0.5 — Full production

### 0.5.0-dev.1 — 2026-10-22 · Development build: atmosphere, light and feedback

**Development build** for playtesting. A pre-release of work in progress toward 0.5.0, not the finished update.

**Browser build**

**Added**
- Place names appear as you enter regions, with smaller names for landmarks within them.
- Each region has its own atmosphere: Ardan's Edge feels warm and sheltered; the Green Lowlands are cooler and more exposed, with cloud shadows drifting across the land and seeds on the wind.
- A Cinematic atmosphere option in Settings adds softer light, mist and glowing dust.
- Bonfires cast a warm, flickering light on the ground, grass and anyone standing near them.
- Smoke rises from the chimneys of Ardan's Edge.
- Picking up bronze coins makes a soft clink.

**Changed**
- Sun Shards and heart fragments arrive with a falling light and a soft glint, and shimmer while they wait to be collected.
- The charged cleave now spins the hero with the axe and steps into the swing; a full charge flashes and rings.
- Several coins dropped together now look like a small pile.
- The Tutorial points out its first Sun Shard once it appears.
- The orchard's coins are now carried by its scavengers instead of lying out of reach until they are defeated.

**Fixed**
- Scavengers and boars now flash when hit, like wolves.
- Tapping the charged attack no longer drains stamina; a cleave costs stamina only when it is released.
- Sounds no longer cause a brief hitch the first time they play.
- Continue returns you exactly where you left off in places your progress has changed, instead of at the bonfire.

## 0.4 — Vertical slice

### 0.4.9 — 2026-10-20 · Controller support and world polish

**Added**
- Xbox controller support with aiming, menu navigation and device-aware prompts.
- An opening prologue, trader conversations and Tutorial breakables that teach swings and charged cleaves.

**Changed**
- Unified pixel-art menus and HUD, clearer bonfire interactions, and zoom that persists between areas.
- Rebuilt Lowlands cliffs, shorelines and paths; opened up the village and refined Tutorial and Lowlands scenery.
- Added clearer animation, sound and visual feedback to the waterworks.

**Fixed**
- Removed invisible blockers around Lowlands cliffs and the drained basin.
- Corrected missing tree shadows, bonfire draw order and unstable prompts and meters.
- Fixed opening the shop from trader dialogue and clarified upgrade costs and purchased stock.

### 0.4.8 — 2026-10-16 · Green Lowlands, exploration and economy

**Added**
- Green Lowlands, reached by the Tutorial road: countryside with branching routes, a farmstead, a village, a river crossing and a bonfire.
- Ancient waterworks, worked with the axe, that drain water to reveal new ground.
- A woodland approach and a lantern-lit dark interior, with gates and bridges that stay open.
- Two new enemies: charging Mossback Boars and coin-dropping Road Scavengers.
- Bronze Coins from enemies, grass, pots and chests, spent at a traveling trader.
- Readable lore, exploration tools and contextual traversal.
- Swaying, cuttable grass, flowers and reeds that regrow when you rest.
- A title screen (Continue, New Game, Settings, Quit) and a Settings page.
- Continue resumes exactly where you left off, health and flasks included.

**Changed**
- One save covers the whole game, keeping tools, coins and purchases through death; existing saves carry over.
- Areas remember defeated enemies and cut grass until you rest or die.
- Reworked the first weapon upgrades, now bought at the Lowlands bonfire; existing Sweeping Edge upgrades are kept.
- Charged cleaves build exponentially and slow you to a crawl; hard obstacles need a full charge.
- The returning axe glows spectral cyan until caught.
- Bonfires light first and rest on a second interaction, with a new animated flame and smoke.
- Rebuilt the Tutorial's art on the shared palette and daylight finish, with ground shadows and more dimensional village buildings.
- New artwork and animation for Sun Shards and heart fragments.
- Tab opens a paused inventory of carried items and resources.
- Restyled, larger bonfire menu and defeat screen.
- Pause menu: Restart is now Return to bonfire, plus Main Menu; New Game moved to the title screen.
- Respawns and new games fade the screen out and in.
- Softer collision against foliage.

**Fixed**
- Weapon hits land on enemies' visible bodies.
- The player draws in front of defeated enemies and bonfire flames.
- Bonfire prompts sit above the flame.
- The axe can be picked up from the front of its stump.
- Upgrade descriptions match current combat.
- Removed leftover development labels.

### 0.4.7 — 2026-10-03 · Combat and Tutorial overhaul

**Added**
- Healing flasks, refilled at bonfires; drinking takes time and can be interrupted.
- Enemy poise and clearer player hit feedback, including a low-health warning.
- Drinking, throwing and seated bonfire animations.
- F4 Developer Tools for resources, god mode, movement, collision, game speed, enemies and teleporting. Both F3 Debug Panel and F4 are available in the browser build.

**Changed**
- Wolves circle, feint and punish openings, with readable lunges and coordinated pack attacks; new eight-direction art.
- Reworked stamina, dodging and combat: stamina debt, moving light attacks, committed cleaves and less spammable throw/Recall.
- Reworked Tutorial encounter pacing, ability teaching and spatial hints.
- Pixel font and framed menus, world-space prompts and enemy bars, and an overhead stamina arc with an always-visible vitals option.
- Smaller bonfire menu with a camera close-up and upgrade previews before affordability. New Game moved to pause; defeat text simplified.
- Rendering now uses URP's 2D Renderer.

**Fixed**
- Changing vitals keeps the pause menu open; button descriptions no longer resize it.
- Tutorial hints linger long enough to read, wait for the axe to land, and clear after freelook with either Alt key.
- Corrected wolf health-bar height, axe swing/ground alignment and dust sorting.

**Removed**
- The Tutorial's target Recall puzzle and duplicate bonfire teaching prompt.

### 0.4.6 — 2026-09-29 · Tutorial guide and movement feel

**Browser build:** now plays the Tutorial.

**Added**
- A Tutorial guide that teaches the route one hint at a time and completes each lesson when the player actually does it. Progress is saved.
- A contextual HUD: health and stamina appear when they matter, interaction prompts sit beside objects, and Tab opens a status panel.
- A new ability introduction and practice sequence in the Tutorial.
- Dust trails while running and skid clouds on sharp stops and turns.
- An F3 developer stats panel in the Editor and development builds.

**Changed**
- Movement now has momentum and a light rebound off walls.
- Trees along the route now block the player and layer correctly.

**Removed**
- The old prototype scene from the build. It stays in the project for reference.

### 0.4.5 — 2026-09-28 · New hero and combat feel

**Added**
- A new eight-direction hero with walk, run and dash animations, carrying a bronze halberd at his side.
- Freelook: hold Left Alt to look toward the cursor.
- A pause menu with restart and quit.
- An alert cue when the wolf spots the player.

**Changed**
- Reworked the light combo into two wide sweeps and a hard-hitting thrust finisher with a short lunge.
- Stronger knockback, hit particles and killing blows, with shorter enemy stuns.
- Renamed the hatchet to the axe. Existing saves carry over.

**Removed**
- The previous character art and its pipeline.

### 0.4.4 — 2026-09-27 · Wolf enemy and Tutorial integration

**Added**
- The animated wolf enemy and enemy health bars.
- An aim marker, attack coverage effects, impact sounds and hit particles.
- Tutorial combat, collectible rewards and bonfire checkpoints, with saved progression.
- Press F to pick up items. A thrown axe is still retrieved by walking over it.
- A combat effects preview tool for the Editor.

**Changed**
- The game is now called **Road of the Old King**.

### 0.4.3 — 2026-09-26 · Directional movement and combat

**Added**
- Four-direction sprint, dash, attack and throw animations for the previous hero.

**Changed**
- Unified the hero's proportions and colours for a consistent old-school look.

### 0.4.2 — 2026-09-25 · Player and axe integration

**Added**
- The animated player in the Tutorial, picking the axe up from its stump.
- Tap E to throw quickly, or hold E to aim (while moving) and release, with an aim guide that stops at walls.
- Practice targets that react to hits with wood chips and impact audio.

**Changed**
- Replaced the articulated character rig with hand-drawn sprites.

### 0.4.1 — 2026-09-24 · Tutorial world

**Added**
- The Tutorial map: a village, a raised practice terrace, a forest road, a river crossing and old ruins, enclosed by dense woodland.

**Changed**
- Larger, redesigned village houses.

**Fixed**
- Cliff seams and the waterfall's direction.

### 0.4.0 — 2026-09-23 · World art kits

**Added**
- Ground, terrain and path tile sets that connect their edges automatically, plus animated water and waterfalls.
- Environment objects and decorations for the Tutorial village and forest.
- Trees, roofs and ruins that the player can walk behind.

## 0.3 — Gameplay loop prototype

### 0.3.1 — 2026-09-22 · Stamina and progression

**Added**
- Sprinting, stamina and a short dodge dash.
- The axe returns automatically when it ends up too far away.
- Sun Shards, spent at a bonfire on one of three weapon upgrades.
- Heart fragments: every three raise maximum health.

**Changed**
- Rebalanced health and damage for the player, enemies and dummies.

### 0.3.0 — 2026-09-21 · Prototype loop

**Browser build**

**Added**
- Player health, defeat and restart.
- A melee enemy with a telegraphed attack.
- A guided test route with a throw-and-Recall puzzle.
- Bonfires that save progress, set the respawn point and allow travel between discovered fires.

## 0.2 — Core prototype

### 0.2.0 — 2026-09-21 · Axe combat

**Browser build**

**Added**
- Axe combat: a three-hit combo, a charged spinning cleave, throwing, retrieving the axe on foot, and Recall.
- Practice targets, shielded dummies broken by a full cleave or flanked with Recall, and breakable bushes and stones.

**Changed**
- Movement in eight directions.

## 0.1 — Pre-production

### 0.1.1 — 2026-09-20 · First tiles

**Browser build**

**Added**
- First pixel-art tiles and a small test map.
- Mouse-wheel camera zoom.

### 0.1.0 — 2026-09-14 · Movement foundation

**Browser build**

**Added**
- Player movement, collision and a following camera.
- Title and banner artwork.

## Versioning

The minor number is the development phase; the patch number is each notable update within it. **Browser build** marks updates published to the [playable web version](https://ronno7.github.io/RoadOfTheOldKing/). **Development builds** (`x.y.z-dev.N`, published as GitHub pre-releases) are playtest snapshots of work in progress toward the next version; that version's final entry supersedes them.

| Version | Phase |
| --- | --- |
| 0.1 | Pre-production |
| 0.2 | Core prototype |
| 0.3 | Gameplay loop prototype |
| 0.4 | Vertical slice |
| **0.5–0.6** | **Full production (current)** |
| 0.7 | Alpha |
| 0.8 | Beta |
| 0.9 | Release candidate |
| 1.0 | Release |
