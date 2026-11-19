# Changelog

## Unreleased

**Added**
- Volume sliders for music, ambience and effects.
- Game icon for the window and taskbar.
- "GUARDIAN FALLEN" marks a boss's defeat.

**Changed**
- The hero carries a new double-bladed axe sprite.
- Free look is two-thirds faster on a controller.
- Place names and "BONFIRE LIT" share one style and never overlap.
- Upgrade bonfires burn a larger gold flame in a bronze brazier.
- An attack clicked late in a dodge lands as the dodge ends.
- Resting at a bonfire brings the camera in closer.

**Fixed**
- Cliffs no longer make noise when you swing from on top of them.
- The upgrade menu keeps its size as you browse.
- Killing blows and hits no longer cancel your combo or charge.
- A missed wolf lunge chains at most one follow-up.
- Defeated enemies no longer fall again when you return or rest.
- Quitting from the pause menu or the window keeps your progress.
- New Game asks before replacing a save it can't read.
- The title waits for a key press at launch.

## 0.5 — Full production

### 0.5.0 — 2026-11-15 · The Green Lowlands

*Windows*

**Added**
- Controls page in the pause menu, for keyboard and controller.
- Rewards are held overhead for a moment when taken.
- Axe hits sound different on wood, stone, brush and earth; pots shatter.
- New boar animations.
- Sliders for volume, screen flashes and shake; a window mode setting.
- Cloak colour choice after the prologue.

**Changed**
- Scavengers, boars and rats move faster.
- Wolves run faster, see farther and chase farther.
- Wolves dodge, feint and punish more often.
- Scavenger, boar and wolf attacks track you until just before they land.
- Scavengers and boars need a full combo to interrupt (was one chop).
- Scavengers attack twice as often, stand closer and react sooner.
- Rats take two chops (was one) and dart nearly twice as often.
- Scavengers and rats press in while you drink, throw or recover.
- Boars charge 25% sooner and sweep from farther away.
- Scavengers sometimes sidestep thrown axes.
- Flask on R (was Q).
- The dodge bursts out head down and eases to a walk (was a steady glide).
- New title screen over animated paintings of each region.
- New environment art.
- The two waterworks wheels sound different when struck.
- Puzzle parts that Recall can strike sparkle subtly.
- Minor adjustments to sluice puzzle crank mechanics.
- Tutorial prompts teach controls only.

**Fixed**
- Fences, bushes and logs no longer draw over the hero in front of them.
- Grass no longer shows behind rocks and props.
- The rope no longer offers a blocked climb.
- Charging boars stun themselves on boulders, logs, fences and trees.
- New Game after playing shows the first place name again.

**Removed**
- Small sage bushes that looked breakable.

### 0.5.0-dev.2 — 2026-11-03 · The Broken Watcher and a bigger Lowlands

*Windows · playtest build*

**Added**
- The Broken Watcher, the first boss, with a slam, a pull and a fog wall.
- Thorn Rats, small enemies that dart in, nip and back off.
- Scavengers lunge with a thrust and swing an armoured overhead.
- Boars sweep with their tusks; new boar and scavenger animations.
- The hero takes a ready stance for standing attacks and the cleave.
- A bonfire where the King's Road leaves the Lowlands, and an end card.
- A bonfire before the Tutorial's first wolf.
- The Drowned Hayfields, an optional area.
- Stats on the inventory page.
- The health bar flashes with the heartbeat when low.

**Changed**
- Windows download replaces the browser version.
- Enemy attack warnings are drawn on the ground at their true width (were thin outlines).
- Enemies no longer take turns attacking.
- Enemies give up the chase only once you've got away.
- Scavengers chase 60% farther and search 50% longer.
- Enemies heal back quickly once out of the fight.
- Charging boars can't be stopped by throws but stun themselves on walls.
- Partly charged cleaves cost less stamina.
- The thrown axe flies 50% faster.
- The stamina arc shows whenever stamina isn't full (was combat only).
- New art for the waterworks, canals and the Watcher's court.
- The road to Barrow Woods is closed until the region is rebuilt.
- A longer road from Ardan's Edge to the Lowlands, with more enemies.
- High Pass reshaped; the camp's rope moved.
- The lantern casts warm, flickering light (was a hard-edged cut-out).
- Place names show once per save.
- Tutorial hints skip lessons you've already passed.
- Free look holds until you move, then eases back (was recentring on release).

**Fixed**
- The road into the Lowlands no longer has invisible walls.
- Waterworks collision matches the walls and the stream.
- Sluice roofs no longer hide the player.
- Lowlands forests cast their shadows.
- Wolves dodge when a cleave is released, not when it starts charging.
- Enemies blocked on their way home no longer walk on the spot.
- The Sunken Storehouse shows its own place name.

### 0.5.0-dev.1 — 2026-10-22 · Atmosphere, light and feedback

*Browser · playtest build*

**Added**
- Place names for regions and landmarks.
- Each region has its own atmosphere; cloud shadows and windblown seeds in the Lowlands.
- A Cinematic atmosphere option: softer light, mist and glowing dust.
- Bonfires cast warm, flickering light.
- Chimney smoke in Ardan's Edge.
- Coins clink when picked up.

**Changed**
- Sun Shards and heart fragments arrive in a falling light and shimmer until taken.
- The cleave spins the hero into the swing; a full charge flashes and rings.
- Coins dropped together form a pile.
- The Tutorial points out its first Sun Shard.

**Fixed**
- Scavengers and boars flash when hit.
- Tapping the charge no longer costs stamina; only a released cleave does.
- Sounds no longer hitch the first time they play.
- Continue returns you where you left off, not to the bonfire.

## 0.4 — Vertical slice

### 0.4.9 — 2026-10-20 · Controller support and world polish

*Browser*

**Added**
- Xbox controller support with matching prompts.
- An opening prologue, trader conversations and Tutorial breakables.

**Changed**
- Menus and HUD share one pixel-art style; zoom carries between areas.
- Lowlands cliffs, shores and paths rebuilt; the village opened up.
- Clearer waterworks feedback.

**Fixed**
- Lowlands cliffs and the drained basin no longer have invisible blockers.
- Tree shadows, bonfire layering, and jittery prompts and meters.
- The shop opens from trader dialogue; upgrade costs and stock are clearer.

### 0.4.8 — 2026-10-16 · Green Lowlands, exploration and economy

**Added**
- The Green Lowlands, reached by the Tutorial road.
- Ancient waterworks worked with the axe.
- A woodland approach and a lantern-lit interior.
- Mossback Boars and Road Scavengers.
- Bronze Coins, spent at a travelling trader.
- Readable lore and exploration tools.
- Cuttable grass and reeds that regrow when you rest.
- Title screen and Settings page.
- Continue resumes where you left off.

**Changed**
- One save for the whole game; tools, coins and purchases survive death.
- Areas remember defeated enemies and cut grass until you rest or die.
- First weapon upgrades reworked, bought at the Lowlands bonfire.
- Cleaves slow you while charging; hard obstacles need a full charge.
- The returning axe glows until caught.
- Bonfires light first, then rest on a second use; new flame and smoke.
- New Tutorial art with ground shadows.
- New Sun Shard and heart fragment art.
- Tab opens a paused inventory.
- Larger, restyled bonfire menu and defeat screen.
- Pause menu: Return to bonfire (was Restart) and Main Menu; New Game moved to the title.
- Respawns and new games fade.
- Softer collision against foliage.

**Fixed**
- Weapon hits land on enemies' visible bodies.
- The player draws in front of defeated enemies and bonfire flames.
- Bonfire prompts sit above the flame.
- The axe can be picked up from the front of its stump.
- Upgrade descriptions match current combat.
- Leftover development labels are gone.

### 0.4.7 — 2026-10-03 · Combat and Tutorial overhaul

**Added**
- Healing flasks, refilled at bonfires; drinking takes time and can be interrupted.
- Enemy poise, clearer hit feedback and a low-health warning.
- Drinking, throwing and seated bonfire animations.

**Changed**
- Wolves circle, feint, punish and attack as a pack; new eight-direction art.
- Stamina can go into debt, cleaves commit, and throw and Recall can't be spammed.
- Tutorial pacing and hints reworked.
- Pixel font, framed menus, prompts beside objects and a stamina arc overhead.
- Smaller bonfire menu with a close-up and upgrade previews; New Game moved to pause.

**Fixed**
- Changing vitals keeps the pause menu open; descriptions no longer resize it.
- Tutorial hints time correctly; either Alt key clears them.
- Wolf health bar height, axe swing alignment and dust layering.

**Removed**
- The Tutorial's target Recall puzzle and a duplicate bonfire prompt.

### 0.4.6 — 2026-09-29 · Tutorial guide and movement feel

*Browser*

**Added**
- Tutorial guide: one hint at a time, each lesson done when you do it.
- Contextual HUD, prompts beside objects and a Tab status panel.
- Tutorial ability practice.
- Dust trails while running and skid clouds on sharp stops and turns.

**Changed**
- The browser build plays the Tutorial.
- Movement has momentum and rebounds off walls.

**Fixed**
- Trees along the route block and layer correctly.

**Removed**
- The old prototype scene.

### 0.4.5 — 2026-09-28 · New hero and combat feel

**Added**
- A new eight-direction hero with walk, run and dash.
- Free look: hold Left Alt to look toward the cursor.
- Pause menu with restart and quit.
- An alert cue when the wolf spots you.

**Changed**
- Light combo: two wide sweeps and a lunging thrust.
- Stronger knockback, hit particles and killing blows; shorter enemy stuns.
- The hatchet is now the axe.

**Removed**
- The previous character art.

### 0.4.4 — 2026-09-27 · Wolf enemy and Tutorial integration

**Added**
- An animated wolf enemy and enemy health bars.
- Aim marker, attack effects, impact sounds and hit particles.
- Tutorial combat, rewards and bonfire checkpoints, with saved progress.
- F picks up items; walk over a thrown axe to retrieve it.

**Changed**
- The game is now called **Road of the Old King**.

### 0.4.3 — 2026-09-26 · Directional movement and combat

**Added**
- Four-direction sprint, dash, attack and throw animations for the previous hero.

**Changed**
- Consistent hero proportions and colours.

### 0.4.2 — 2026-09-25 · Player and axe integration

**Added**
- The animated player in the Tutorial, taking the axe from its stump.
- Tap E to throw, or hold to aim with a guide that stops at walls.
- Practice targets with wood chips and impact sounds.

**Changed**
- Hand-drawn sprites replace the articulated character rig.

### 0.4.1 — 2026-09-24 · Tutorial world

**Added**
- The Tutorial map: a village, practice terrace, forest road and ruins.

**Changed**
- Larger, redesigned village houses.

**Fixed**
- Cliff seams close up and the waterfall flows the right way.

### 0.4.0 — 2026-09-23 · World art kits

**Added**
- Ground, terrain and path tiles; animated water and waterfalls.
- Village and forest props.
- Trees, roofs and ruins you can walk behind.

## 0.3 — Gameplay loop prototype

### 0.3.1 — 2026-09-22 · Stamina and progression

**Added**
- Sprinting, stamina and a short dodge.
- A distant axe returns on its own.
- Sun Shards, spent at a bonfire on one of three weapon upgrades.
- Heart fragments: every three raise maximum health.

**Changed**
- Health and damage rebalanced.

### 0.3.0 — 2026-09-21 · Prototype loop

*Browser*

**Added**
- Player health, defeat and restart.
- A melee enemy with a telegraphed attack.
- A test route with a throw-and-Recall puzzle.
- Bonfires that save, set the respawn point and allow travel between fires.

## 0.2 — Core prototype

### 0.2.0 — 2026-09-21 · Axe combat

*Browser*

**Added**
- Axe combat: three-hit combo, charged spinning cleave, throwing, retrieving on foot, and Recall.
- Practice targets, shielded dummies, and breakable bushes and stones.

**Changed**
- Movement in eight directions.

## 0.1 — Pre-production

### 0.1.1 — 2026-09-20 · First tiles

*Browser*

**Added**
- First pixel-art tiles and a small test map.
- Mouse-wheel camera zoom.

### 0.1.0 — 2026-09-14 · Movement foundation

*Browser*

**Added**
- Player movement, collision and a following camera.
- Title and banner artwork.

## Versioning

Each minor number is a development phase; each patch is an update. *Windows* and *Browser* mark downloads on the [Releases page](https://github.com/Ronno7/RoadOfTheOldKing/releases); browser builds ran until 0.5.0. Playtest builds (`x.y.z-dev.N`) are pre-releases, superseded by their version's entry.

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
