# Changelog

## Unreleased

**Added**
- Smarter wolves: they circle, lunge along a telegraphed lane that locks just before the leap, snap when you get close, feint, dodge your swings, punish missed attacks and leave a short opening after each attack. Packs take turns attacking.
- Enemy poise: tougher enemies shrug off light hits until enough damage breaks their stance.
- Healing flasks: three charges refilled at bonfires. Press Q to drink; it heals near the end of the drink and can be interrupted, and wolves punish careless drinking.
- Clear hit feedback for the hero: a flinch animation, red flash, brief impact freeze, camera kick, invulnerability blink, and a red screen-edge flash that pulses at low health.

**Changed**
- Souls-style stamina: any action can start while you have stamina left and may push you into a deficit, which slows recovery and makes that attack weaker and slower. Stamina recovers faster between actions.
- The dodge is now a longer roll with more invincibility and no cooldown; stamina is the only limit.
- You can keep moving while making light attacks, and the charged cleave can't be interrupted once committed.
- Rebuilt the bonfire, upgrade and defeat screens with Unity's UI Toolkit: consistent menus, keyboard and mouse, with Escape stepping back through pages.
- Throw and Recall rebalanced: Recall now costs stamina, catching the axe leaves a brief opening, a recalled axe no longer hits the enemy it is pulled out of, thrown hits barely affect an enemy's guard, and wolves sidestep throws and rush you while your axe is away.
- Light attacks have shorter reach and cost more stamina.
- The wolf is redrawn as a larger eight-direction animal with walk, run, bite and death animations; the bite is timed to its attack.
- Rewrote the [systems overview](Docs/SystemsOverview.md) as a guide to the game's architecture, with diagrams.
- Moved rendering to Unity's Universal Render Pipeline (2D Renderer), opening the way for 2D lighting, shader-based effects and post-processing. The game looks the same.

## 0.4 — Vertical slice

### 0.4.6 — 2026-09-29 · Tutorial guide and movement feel

**Browser build:** now plays the Tutorial.

**Added**
- A Tutorial guide that teaches the route one hint at a time and completes each lesson when the player actually does it. Progress is saved.
- A contextual HUD: health and stamina appear when they matter, interaction prompts sit beside objects, and Tab opens a status panel.
- Recall awakening: striking the ancient sun-wheel stone with a thrown axe unlocks Recall, followed by a short Recall drill.
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
- The Tutorial's first fight, a Sun Shard reward and a bonfire checkpoint, with their own save.
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
