# Changelog

## Unreleased

**Added**
- Readable environmental lore with persistent discoveries and a shared reading interface.
- Reusable exploration tools and contextual traversal interactions.
- Bronze Coins, breakable pots, treasure chests and limited shop stock.
- A paused Tab inventory for owned equipment and tools, plus a traveling trader with a stock menu and purchase confirmation.
- Tools, coins and purchased stock persist through death and reload.
- Tutorial and Green Lowlands grass, flowers and reeds sway, react to footsteps, draw in front of or behind the player, and can be cut by the axe with spinning airborne blades and green particles; resting restores them.
- A title screen with Continue, New Game, Settings and Quit, and a Main Menu option in the pause menu.
- A Settings page, reachable from the pause menu and the title, holding the vitals option.
- Continue resumes exactly where you left off, with the health and flasks you had; dying still returns you to the bonfire.
- The Tutorial road now reaches Green Lowlands, with countryside encounters, an abandoned farmstead, river crossing and a bonfire for resting, saving and travel between regions.

**Changed**
- Added native artwork for treasure chests, clay pots, Sun Shards, heart fragments and bronze coins, with a brief chest-opening animation and player-relative prop depth.
- Replaced more interaction placeholders with native stonework, rope and tool artwork.
- Enlarged Green Lowlands with winding paths, branching exploration routes and combat clearings, plus a short combat approach connecting the regions.
- Added regional landscape art, woodland clusters, countryside landmarks, cliff faces and weathered stonework.
- All bonfires share the new animated flame; regional bonfire bases can vary.
- The bonfire menu has a new hearth look (oak beams, rope lashings and bronze trim) and is larger; the defeat screen has a bronze frame and carved heading.
- The pause menu's Restart is now Return to bonfire; New Game moved to the title screen.
- One save now covers the whole game instead of a single area; existing saves carry over automatically.
- Respawning, restarting and starting a new game fade the screen out and back in.

**Fixed**
- Improved Green Lowlands height readability, with clearer woodland boundaries, ascent steps and corrected tree/fence footing.
- Continue falls back to safe arrival/checkpoint footing when revised terrain obstructs a saved position, preserving saved health and flasks.
- The player now renders in front of defeated enemies and bonfire flames; revived enemies recover their original sorting.

## 0.4 — Vertical slice

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

The minor number is the development phase; the patch number is each notable update within it. **Browser build** marks updates published to the [playable web version](https://ronno7.github.io/RoadOfTheOldKing/).

| Version | Phase |
| --- | --- |
| 0.1 | Pre-production |
| 0.2 | Core prototype |
| 0.3 | Gameplay loop prototype |
| **0.4** | **Vertical slice (current)** |
| 0.5–0.6 | Full production |
| 0.7 | Alpha |
| 0.8 | Beta |
| 0.9 | Release candidate |
| 1.0 | Release |
