# Accepted player source art

The current set is accepted for now. This folder owns rebuild inputs and registered outputs; [the gallery](../../Docs/Art/Player/README.md) owns current previews and [the backlog](../../Docs/AnimationPlan.md) owns future work.

## Sources

| Folder | Purpose |
| --- | --- |
| Forehand / Throw / Catch / Dash | Current accepted combined drawings, explicit axe masks and hidden-body underpaint where needed |
| Sprint | Accepted east source and carrying-arm recipe inputs, north/south mask and redraw inputs, south empty-hand drawings; west is generated as the approved east mirror |
| Weapon | Complete pickup/carry, alternate held views and free-axe spin sources |
| Registered | Full-canvas masters and manifests consumed by Unity import/review; generated from the source drawings |
| [Provenance.json](Provenance.json) | Original generation/edit briefs retained verbatim as historical records; obsolete drafts named there are not active dependencies |

Some north/south sprint files still contain “Study” or “Revision” in their names because current recipes read their transparency or targeted pixels. They are required inputs, not unused drafts. Keep them until the corresponding recipe is intentionally replaced.

## Rebuild and verification

Run from the repository root with Python 3 and Pillow. Existing runtime assets need no rebuild for ordinary play or level editing. A local Pillow cache may be available through `$env:PYTHONPATH = (Resolve-Path tmp/art-python).Path`.

| Changed family | Python exporter in Tools/Art/Player | Unity menu under The Lost Shrine → Animation |
| --- | --- | --- |
| Pickup/carry and held views | `build_player_weapon.py` | Refresh imports |
| Forehand | `build_player_forehand_layers.py` | Import Registered Forehand |
| Stationary throw | `build_player_throw_layers.py` | Import Registered Throw |
| Catch / spin | `build_player_return_layers.py` | Import Registered Animations |
| Dash | `build_player_dash_layers.py` | Import Registered Dash |
| Sprint | `build_player_sprint_layers.py` | Import Registered Sprint |

Example: `python Tools/Art/Player/build_player_throw_layers.py`. Rebuild only the family changed. Full dependency order is weapon, forehand, throw, return, dash, sprint. East carrying and south unarmed helper modules are invoked by the sprint exporter; west is rebuilt after east.

JSON recipes sit beside the exporters. They record source rectangles, fixed source density, foot anchors, masks, reveal patches and approved reuse. Body plus weapon must reconstruct the registered master exactly; verify bounds, pivots and gameplay handoffs before accepting new art. Do not change sprite geometry or gameplay timing as incidental cleanup.

Unity importers keep sprite sheets under `Assets/Art/Sprites` and write playback definitions under `Assets/Animations/Player/Locomotion`, `Assets/Animations/Player/Combat` and `Assets/Animations/Weapons`. Moving these definitions preserved their GUIDs; prefab references do not need reassignment.

Registered output uses 640×640 cels, 128 PPU and a shared foot pivot. Current slots: forehand 24, throw/aim 32, catch 16, dash 20, sprint 56 including separate unarmed tracks; eight shared prop-spin cels. Reused slots are not distinct drawings.

## Timing and ownership

`ThrowActionClock` owns real release, `CatchPresentationClock` follows confirmed return, and `RegisteredPlayerAnimation` selects matched layers. Weapon collision/damage and motor movement retain ownership. The complete contract is in the [animation backlog](../../Docs/AnimationPlan.md#timing-and-ownership).

## Review and captures

Use Unity's Registered Action Review and Registered Return Review for frame inspection. Check normal-speed gameplay before accepting a drawing. Current checks are indexed in [Tools/Verification](../../Tools/Verification/README.md); captures and GIF packaging are in [Tools/Capture](../../Tools/Capture/README.md).

`build_player_continuity_review.py` refreshes the current representative-pose board. `build_player_style_review.py` refreshes the original reference board only. Neither is evidence that every animation has been visually reviewed.

Earlier per-pass counts remain summarized in the changelog; obsolete sample assertions, one-off setup scripts, rejected drawings and duplicate review histories were removed during consolidation.
