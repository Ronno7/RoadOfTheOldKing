# Current gameplay captures

These helpers record native Unity presentation; they are not tests or source-art generators. Start with the scene in a saved state, read the script header, run in the specified mode, and stop Play Mode afterward to discard temporary fixture/equipment state. Do not use production saves for recordings.

| Unity script | Python packager |
| --- | --- |
| CaptureLivePlayerLoop.cs.txt | build_live_player_preview.py |
| CaptureLiveForehand.cs.txt | build_live_forehand_preview.py |
| CaptureLiveThrow.cs.txt | build_live_throw_preview.py |
| CaptureRegisteredDash.cs.txt | build_dash_preview.py / build_dash_directions_preview.py |
| CaptureRegisteredSprint.cs.txt | build_sprint_preview.py / build_sprint_directions_preview.py |

Directions are East=0, North=1, South=2, West=3 in these registered capture fixtures. Set the script's direction, then run its packager from the repository root, for example `python Tools/Capture/build_live_throw_preview.py --direction West`. Read each packager's arguments; the default action-loop package expects east and north captures.

Disposable PNG/CSV captures go under `tmp/`; packaged GIFs/comparisons go to `Docs/Art/Player/Previews`. Combined sprint/dash packages require captures for both directions. Keep current useful milestones; remove disposable captures after review.
