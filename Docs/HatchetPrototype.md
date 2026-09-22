# Hatchet prototype

Open `Assets/Scenes/Tutorial.unity` and enter Play Mode. Walk north from the spawn over the golden hatchet. Practice targets sit to the west of the pond; the Recall altar is on the northern stone pad. All asset paths below are relative to the Unity project folder, `TheLostShrine/`.

## Controls and current rules

| Input | Behavior |
| --- | --- |
| WASD / arrows | Eight-direction movement, independent of aim. |
| Mouse position | Aim the held weapon, chops, and throws. |
| Left click | Three-hit combo: 1, 1, then 2 damage. Click during a swing to queue one follow-up attack. Three small pips above the player show the current hit. |
| Hold/release right click | Charge a 360-degree cleave. Minimum charge is 0.2 seconds; full gold charge is 0.8 seconds. Partial charge reduces damage, knockback, and stagger. |
| E | Throw up to 6 world units. After Recall unlocks, press E again to recall, including during outward flight. |

The player starts unarmed, and picking up the hatchet does **not** unlock Recall. Before unlocking, walk within 0.9 units of the stopped hatchet to retrieve it. Walk into the northern altar to unlock Recall for the current play session. This marker stands in for the future first-puzzle reward; no save/progression system is implemented yet.

Light attacks are configured for 0.20 seconds each, with a 0.23-second finisher. The first 12% is wind-up, the quick slash runs until 50%, and the remainder is recovery. Only the slash phase can deal damage. The tapered crescent follows the blade in both swing directions and fades during recovery. One follow-up click can wait up to 0.25 seconds; holding the button does not auto-attack. The combo resets after 0.55 seconds of idle time following a swing. The bright pip marks the current hit, earlier hits are gold, and all pips fade out as the combo expires.

Throws stop at the first solid target or terrain collider and deal 2 damage to a damageable target. The returning hatchet deals 2 damage to each target it crosses, tracks its moving owner, and ignores terrain so it can always come home. Each target can take one hit per swing or flight leg, even with multiple colliders. The same enemy can be hit outbound and again on return. Chops cannot pass through solid terrain. Only one weapon action runs at a time; melee is unavailable while the hatchet is away.

Full cleaves break shields and cracked stones. Bushes accept any damaging hit. Practice dummies have 8 health, receive knockback/stagger, and reset health, shield, and position four seconds after defeat. Breakable props reset when Play Mode restarts. These are test objects, without enemy AI.

Recall hits on a shielded target's rear half bypass protection and deal normal return damage without breaking its shield. Front and exact-side hits stay blocked. Ordinary chops, outward throws, and partial cleaves remain blocked even from behind. The blue arrow and shield on the practice dummy show its front, initially south. Throw past one side, move in front of the dummy, and recall so the return path strikes its back. Special rear-hit knockdown/stun is deferred; the ordinary Recall hit reaction is unchanged.

## Assets and tuning

- `Assets/Prefabs/Weapons/Hatchet.prefab`: the pickup and equipped/thrown weapon are the same instance. The `Model` child contains simple colored shapes that can be replaced with art.
- `Assets/Settings/Weapons/HatchetSettings.asset`: combo timing, reach, charge thresholds, damage, stagger, knockback, throw range, flight speeds, and retrieval distance.
- `Assets/Prefabs/Player/Player.prefab`: movement plus separate combat input/controller components. The controller exposes the short light-attack input buffer.
- `Assets/Prefabs/Combat/`: normal and shielded practice dummies.
- `Assets/Prefabs/World/`: bushes, cracked stones, and the separate Recall altar.

## Responsibilities and extension points

`PlayerCombatInput` adapts keyboard/mouse actions into `ICombatInput` frames. `PlayerCombatController` projects the cursor into the game plane, buffers clicks, and sends weapon commands. Movement remains an independent component. Focus loss, disabled input, and pausing cancel charging.

`HatchetWeapon` owns the explicit ground/held/attack/charge/flight/stuck/return state machine. `HatchetHitDetector` performs swept physics queries, melee occlusion, and hit deduplication. `HatchetView` renders swings, charge feedback, and flight separately from damage rules; it shares the light wind-up/slash timing settings with the weapon. `HatchetComboIndicator` reads combo state to display the three pips. The single signature weapon uses one state machine rather than a class for every small action.

Immutable `CombatHit` values carry attack type, damage, direction, impact position, knockback, stagger, and guard-breaking strength to `IHitReceiver`. New puzzle objects can implement that interface without changing input or weapon code. Solid hit colliders are required; trigger colliders are reserved for pickups/unlocks and ignored by attacks.

`Damageable` handles health and publishes hit/defeat events. Optional `IHitProtection` blocks damage; `ShieldProtection` supplies the prototype guard. `HitReaction` listens for accepted hits and applies knockback/stagger. Future enemy movement/AI should respect `HitReaction.IsStaggered` rather than overwrite its velocity during the reaction. `PracticeTarget` provides the tutorial-only labels, flashes, and reset behavior.

`ShieldProtection.IsRearRecallHit()` compares the world impact point against the target's center and configured front. It uses the actual contact position, not the player's position or the projectile's travel direction. Missing or central contact points cannot bypass a guard. The prefab's `Facing` child controls both the visible shield/arrow and the facing reference; rotate that child to turn the guard without rotating its labels. `localForward` defines its local front, defaulting to down. The impact data and classifier are available for later rear-hit reactions.

A completed puzzle can call `PlayerCombatController.UnlockRecall()`. It is idempotent and publishes `RecallUnlocked`; pickup/equipment never calls it. The current altar is a separate trigger adapter for that method. Persistence can later restore the unlock through the same method.

## Verification

Verified in the Unity Editor with no compilation errors or runtime warnings from this change:

- 78 Play Mode assertions cover actual pickup/unlock triggers, combo damage and timeout, action exclusivity, directional attacks, charge thresholds, knockback/stagger, shields, props, terrain obstruction, throw range, manual retrieval, return damage, moving-owner tracking, and survival when a struck target is destroyed. These include rear/front/side Recall classification, rotated facing, impact positions, shield preservation, complete throw/reposition/recall sequences, slash direction, damage timing, and combo-indicator visibility.
- 11 additional checks inject virtual mouse/keyboard events through the Input System, covering early queued clicks across the full combo, one attack per press, aim locking and retargeting, charged cleaves, throw/recall, and clearing queued attacks on focus loss.
- Visually inspected the pickup, held hatchet, practice targets, and controls overlay; validated scene references.

`Tools/Verification/HatchetPlayModeChecks.cs.txt` preserves the 78 combat assertions as a C# method body for Unity MCP `execute_code`. Run it in a **fresh** Play Mode session of Tutorial, after Unity has finished entering Play Mode and the Game view has started running, then stop Play Mode to discard test state. It creates temporary target instances and removes them in `finally`; it does not save the scene or assets. This is an Editor integration check, not a Unity Test Runner assembly.

For a manual pass: pick up the hatchet, chain three chops on a dummy, try partial/full cleaves on the shield, throw and retrieve before visiting the altar, then unlock and recall through a line of targets while moving. Browser-build behavior has not been verified for this change.
