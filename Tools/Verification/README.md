# Verification

Run the existing checks affected by a change; do not grow the suite for each art touch-up. These C# snippets are executed through the Unity editor bridge, not automatically discovered test assemblies. Read each header for its scene/mode/setup requirements. Never run save-mutating fixtures against a production save.

| Folder | Coverage |
| --- | --- |
| [Gameplay](Gameplay) | Combat, throw input/clock, stamina, dodge, Recall, enemies, checkpoints, puzzle, shards/upgrades and heart fragments |
| [Player](Player) | Original locomotion/idle/carry, registered imports, melee timing, return ownership, dash/sprint presentation and the retained aim-stride foundation |
| [World](World) | Tile kits, palette/collision separation, demo presentation and Tutorial route clearance |

For authoring-path or import changes, start with RegisteredForehandChecks, RegisteredThrowChecks and RegisteredReturnChecks in Edit Mode. Player Play Mode fixtures use isolated objects/scenes as described in their headers; registered direction indices are East=0, North=1, South=2, West=3.

Prototype progression checks use isolated `TheLostShrine.Verification.*` checkpoint slots and restore the normal slot afterward. Current presentation checks do not need a production save. Capture scripts have moved to [Tools/Capture](../Capture/README.md).

Obsolete assertions tied to the replaced first-chop/early-combo presentation, one-shot shard/heart setup scripts and isolated study captures were retired. Working gameplay coverage and current import/ownership checks remain. Historical passing counts in the changelog describe their original runs, not a claim that all suites were rerun today.
