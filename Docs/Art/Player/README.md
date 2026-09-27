# Player and hatchet

**Current animations are accepted for now.** Tutorial has original walking/carrying and idle breathing, cardinal sprint/dash/opening strike/stationary aim/throw/catch, and eight-cel axe spin. Future work is ranked in the [animation backlog](../../AnimationPlan.md) and will accompany gameplay development.

The original [PlayerSheet.png](../../../TheLostShrine/Assets/Art/Sprites/Player/PlayerSheet.png) remains the style reference. Preserve compact proportions, muted colors, patterned cap trim, soft outlines, blob hands and consistent hatchet geometry. West sprint exactly mirrors approved east by request.

## Current action loop

![Accepted east/north action loop](Previews/Live-Action-Loop.gif)

[South loop](Previews/Live-Action-Loop-South.gif) · [West loop](Previews/Live-Action-Loop-West.gif) · [Current continuity comparison](Previews/Current-Continuity.png) · [Axe handoffs](Previews/Axe-Continuity.png)

These are native Tutorial captures with Recall temporarily enabled for recording. They demonstrate presentation, not a completed Tutorial awakening/progression sequence.

## Movement and combat previews

| Family | Current recordings |
| --- | --- |
| Walk / idle | [Original locomotion](Previews/Locomotion.gif), [idle breathing](Previews/Idle.gif) |
| Sprint | [East/west mirror](Previews/Sprint-East-West.gif), [east/north](Previews/Sprint-Directions.gif), [south](Previews/Sprint-South.gif) |
| Dash | [East/north](Previews/Dash-Directions.gif), [south](Previews/Dash-South.gif), [west](Previews/Dash-West.gif) |
| Opening strike | [South overhead](Previews/Forehand-South.gif), [west](Previews/Forehand-West.gif) |
| Quick/held throw | [South](Previews/Throw-South.gif), [west](Previews/Throw-West.gif) |

Current pose comparisons are beside the GIFs in [Previews](Previews). Superseded experiments and duplicate isolated recordings have been removed.

## Runtime and authoring

- Playback definitions live in `Assets/Animations/Player/Locomotion` and `Combat`; detached axe spin lives in `Assets/Animations/Weapons`. Both standard clips and custom frame-sequence assets belong there. Runtime sprite sheets remain under `Assets/Art/Sprites`, and tuning stays under `Assets/Settings`.
- Accepted input drawings, masks, underpaint and registered masters live in [ArtSource/Player](../../../ArtSource/Player/README.md). That guide owns rebuild instructions; historical prompts are retained as provenance data.
- Exporters live in `Tools/Art/Player`, native capture helpers in `Tools/Capture`, and current checks in `Tools/Verification`.
- Older ChopEast/ChopDirections/ComboContacts/BackhandNorth/HatchetTurns assets still support fallback actions. They are dependencies, not unused art.
- Gameplay timing, controls and persistence are documented in [SystemsOverview](../../SystemsOverview.md).
