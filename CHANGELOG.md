# Changelog

## Unreleased

## 0.5 — Full production

### 0.5.0 — 2026-11-15 · The Green Lowlands

**Windows build**

**Added**
- Controls page in the pause menu, for keyboard and controller.
- Rewards are held overhead for about a second when taken.
- Axe hits sound and fleck by material; pots break with a ceramic sound.
- New boar animations.
- Settings sliders for volume, screen flashes and shake; window mode choice.
- Cloak colour choice after the prologue.
- Undergrowth marks the arrival forest edge (30 bushes).

**Changed**
- Scavengers, boars and rats move at 4.8, 4 and 5.5 u/s (were about 2.8, 2.4 and 3.6).
- Wolves run at 6.5 u/s (was about 5), keep sight of you to 18 units (was 13.5) and chase to 40 from home (was 24).
- Wolves dodge, feint and punish more often.
- Scavenger, boar and wolf attacks lock aim 0.15 s before landing (was 0.3–0.5 s).
- Scavengers and boars need a full combo to interrupt (was one chop).
- Scavengers pause 0.4 s between attacks (was 0.8), stand closer and engage after 0.5 s of sight (was 0.9).
- Rats take two chops (20 health, was 10) and pause 0.5 s between darts (was 0.9).
- Scavengers and rats press in when you drink, throw or recover; boars charge 25% sooner.
- Scavengers sometimes sidestep thrown axes.
- Boars sweep whenever you're within 1.6 units (was 1.2).
- Flask on R (was Q).
- The dodge bursts out head down and eases to walking speed (was a steady 8.9 u/s).
- Title screen: logo and text menu over animated region paintings.
- New chest-height fences (23 px, was 29) in four styles, colliding 0.375 tiles deep (was 1).
- Three new carts (was one).
- New art for the mill bridge, stone posts, fallen logs and Tutorial props.
- Removed small sage bushes that looked breakable.
- The two waterworks wheels sound different when struck.
- Puzzle parts Recall can strike twinkle.
- The maintenance crank is one part, carried between sockets.
- Tutorial prompts teach controls only.
- Menus drop descriptions that added nothing.

**Fixed**
- Fences, bushes and logs no longer draw over the hero in front of them.
- Removed grass drawn behind rocks and props (129 in the Tutorial, 32 in the Lowlands).
- The rope no longer offers a blocked climb.
- Charging boars are stunned by boulders, logs, fences and trees.
- New Game after playing shows the first place name again.

### 0.5.0-dev.2 — 2026-11-03 · Development build: the Broken Watcher, a bigger Lowlands and Windows

**Development build:** a playtest pre-release toward 0.5.0.

**Windows build**

**Added**
- The Broken Watcher, a boss at the sluice, with a ground-cracking slam, a pull, a boss bar and a fog wall.
- Thorn Rats that dart, nip and back off; a nest of five in the drained basin.
- Scavengers fight with a lunging thrust and an armoured overhead, and chase after 0.9 s of sight.
- Boar tusk sweep and charge run; scavenger and boar idles.
- Hero ready stance for standing attacks, the charge and the cleave.
- North Bank Bonfire where the King's Road leaves the Lowlands, with an end card once the basin is drained.
- Bridge Bonfire before the Tutorial's first wolf.
- Drowned Hayfields south of the pasture, with a heart fragment.
- Breakable bushes close the way into the Drowned Hayfields.
- Stats on the inventory page.
- Low health bar flashes with the heartbeat.
- F4 Developer Tools show collision and bring fallen bosses back.

**Changed**
- Windows download replaces the browser version.
- Attack warnings drawn on the ground as lanes and wedges at true width (were thin outlines; boar charge 0.67 units each side, was 1.05).
- Enemies no longer take turns attacking.
- Enemies give up the chase only once you've got away.
- Scavengers chase 16 units from home (was 10) and search 3 s (was 2).
- Enemies out of the fight heal 10% a second; the Watcher fully heals at its seat.
- Charging boars can't be interrupted or staggered by throws; hitting a wall head on stuns them.
- Shorter cleave charges cost less stamina (down to about 19, 35 at full).
- Thrown axe flies 50% faster (full range in 0.39 s, was 0.58 s).
- Stamina arc shows whenever stamina is below full (was combat only).
- Improved waterworks, sluice gate and canal art; reshaped the cliffs and basin edge around them.
- Watcher's court dressed.
- Old Drain separated from the storehouse, entered by the basin arch; Barrow Approach closed until Barrow Woods is rebuilt.
- Longer road from Ardan's Edge to the Lowlands, with more enemies.
- Raised the ridge south of High Pass; camp rope moved to the east ledge.
- Lantern casts warm, flickering light with soft wall shadows (was a hard-edged cut-out).
- Bonfire menu says spare Sun Shards keep.
- Redrew the hero's north drinking frames.
- Place names show once per save.
- Tutorial hints skip lessons you've already passed.
- Free look holds until you move, then eases back (was recentring on release).

**Fixed**
- Invisible walls on the road into the Lowlands.
- Waterworks collision gaps, invisible barriers and the walkable basin stream.
- Sluice roofs hide the player and gates behind them.
- Missing shadows under Lowlands forests.
- Wolves dodge when a cleave is released, not when it starts charging.
- Enemies blocked by a wall on the way home no longer walk on the spot.
- The Sunken Storehouse showed "Green Lowlands" as its place name.

### 0.5.0-dev.1 — 2026-10-22 · Development build: atmosphere, light and feedback

**Development build:** a playtest pre-release toward 0.5.0.

**Browser build**

**Added**
- Place names for regions and landmarks.
- Each region has its own atmosphere; the Lowlands have drifting cloud shadows and windblown seeds.
- Cinematic atmosphere option: softer light, mist and glowing dust.
- Bonfires cast warm, flickering light.
- Chimney smoke in Ardan's Edge.
- Coins clink when picked up.

**Changed**
- Sun Shards and heart fragments arrive in a falling light and shimmer until taken.
- The cleave spins the hero and steps into the swing; a full charge flashes and rings.
- Coins dropped together form a pile.
- The Tutorial points out its first Sun Shard.
- The orchard's scavengers carry its coins.

**Fixed**
- Scavengers and boars flash when hit.
- Tapping the charge no longer costs stamina; only a released cleave does.
- Sounds no longer hitch the first time they play.
- Continue returns you where you left off, not to the bonfire, in areas your progress has changed.

## 0.4 — Vertical slice

### 0.4.9 — 2026-10-20 · Controller support and world polish

**Added**
- Xbox controller support with matching prompts.
- Opening prologue, trader conversations and Tutorial breakables.

**Changed**
- Unified pixel-art menus and HUD; zoom persists between areas.
- Rebuilt Lowlands cliffs, shores and paths; opened up the village.
- Clearer waterworks feedback.

**Fixed**
- Invisible blockers around Lowlands cliffs and the drained basin.
- Missing tree shadows, bonfire draw order, and unstable prompts and meters.
- Opening the shop from trader dialogue; clearer upgrade costs and stock.

### 0.4.8 — 2026-10-16 · Green Lowlands, exploration and economy

**Added**
- Green Lowlands, reached by the Tutorial road.
- Ancient waterworks, worked with the axe, that drain water to reveal new ground.
- A woodland approach and a lantern-lit interior.
- Mossback Boars and Road Scavengers.
- Bronze Coins, spent at a travelling trader.
- Readable lore, exploration tools and contextual traversal.
- Cuttable grass and reeds that regrow when you rest.
- Title screen and Settings page.
- Continue resumes where you left off.

**Changed**
- One save for the whole game; tools, coins and purchases survive death.
- Areas remember defeated enemies and cut grass until you rest or die.
- Reworked first weapon upgrades, bought at the Lowlands bonfire.
- Cleaves charge exponentially and slow you; hard obstacles need a full charge.
- The returning axe glows spectral cyan until caught.
- Bonfires light, then rest on a second use; new flame and smoke.
- Rebuilt Tutorial art on the shared palette, with ground shadows.
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
- Removed leftover development labels.

### 0.4.7 — 2026-10-03 · Combat and Tutorial overhaul

**Added**
- Healing flasks, refilled at bonfires; drinking takes time and can be interrupted.
- Enemy poise, clearer hit feedback and a low-health warning.
- Drinking, throwing and seated bonfire animations.
- F4 Developer Tools; F3 and F4 work in the browser build.

**Changed**
- Wolves circle, feint, punish and attack as a pack; new eight-direction art.
- Reworked stamina, dodging and combat: stamina debt, committed cleaves, less spammable throw and Recall.
- Reworked Tutorial pacing and hints.
- Pixel font, framed menus, world-space prompts and an overhead stamina arc.
- Smaller bonfire menu with a close-up and upgrade previews; New Game moved to pause.
- URP 2D rendering.

**Fixed**
- Changing vitals keeps the pause menu open; descriptions no longer resize it.
- Tutorial hint timing; freelook clears hints with either Alt key.
- Wolf health-bar height, axe swing alignment and dust sorting.

**Removed**
- The Tutorial's target Recall puzzle and duplicate bonfire prompt.

### 0.4.6 — 2026-09-29 · Tutorial guide and movement feel

**Browser build:** now plays the Tutorial.

**Added**
- Tutorial guide: one hint at a time, each lesson completed when you do it.
- Contextual HUD, prompts beside objects and a Tab status panel.
- Tutorial ability practice.
- Dust trails while running and skid clouds on sharp stops and turns.
- F3 stats panel in development builds.

**Changed**
- Movement has momentum and rebounds off walls.
- Trees along the route block and layer correctly.

**Removed**
- The old prototype scene from the build.

### 0.4.5 — 2026-09-28 · New hero and combat feel

**Added**
- New eight-direction hero with walk, run and dash.
- Freelook: hold Left Alt to look toward the cursor.
- Pause menu with restart and quit.
- Alert cue when the wolf spots you.

**Changed**
- Light combo: two wide sweeps and a lunging thrust.
- Stronger knockback, hit particles and killing blows; shorter enemy stuns.
- The hatchet is now the axe.

**Removed**
- The previous character art.

### 0.4.4 — 2026-09-27 · Wolf enemy and Tutorial integration

**Added**
- Animated wolf enemy and enemy health bars.
- Aim marker, attack coverage effects, impact sounds and hit particles.
- Tutorial combat, rewards and bonfire checkpoints, with saved progress.
- F picks up items; walk over a thrown axe to retrieve it.

**Changed**
- The game is now called **Road of the Old King**.

### 0.4.3 — 2026-09-26 · Directional movement and combat

**Added**
- Four-direction sprint, dash, attack and throw animations for the previous hero.

**Changed**
- Unified the hero's proportions and colours.

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
- Cliff seams and the waterfall's direction.

### 0.4.0 — 2026-09-23 · World art kits

**Added**
- Auto-connecting ground, terrain and path tiles; animated water and waterfalls.
- Village and forest props.
- Trees, roofs and ruins you can walk behind.

## 0.3 — Gameplay loop prototype

### 0.3.1 — 2026-09-22 · Stamina and progression

**Added**
- Sprinting, stamina and a short dodge dash.
- A distant axe returns on its own.
- Sun Shards, spent at a bonfire on one of three weapon upgrades.
- Heart fragments: every three raise maximum health.

**Changed**
- Rebalanced health and damage.

### 0.3.0 — 2026-09-21 · Prototype loop

**Browser build**

**Added**
- Player health, defeat and restart.
- A melee enemy with a telegraphed attack.
- A test route with a throw-and-Recall puzzle.
- Bonfires that save, set the respawn point and allow travel between fires.

## 0.2 — Core prototype

### 0.2.0 — 2026-09-21 · Axe combat

**Browser build**

**Added**
- Axe combat: three-hit combo, charged spinning cleave, throwing, retrieving on foot, and Recall.
- Practice targets, shielded dummies, and breakable bushes and stones.

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

The minor number is the development phase; the patch is each notable update. **Windows build** marks a download on the [Releases page](https://github.com/Ronno7/RoadOfTheOldKing/releases); **Browser build** marks the web version that ran until 0.5.0. **Development builds** (`x.y.z-dev.N`, GitHub pre-releases) are playtest snapshots; the version's final entry supersedes them.

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
