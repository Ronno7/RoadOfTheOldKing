# Tutorial village paths

Use **TutorialPaths** in the Tile Palette. The top row has three brushes, left to right:

| Brush | Paint on | Purpose |
| --- | --- | --- |
| DirtLane | Paths/DirtLane | Warm compacted-earth village lanes, with a soft, lightly worn edge. Paint 2–3 cells wide for the main route. |
| Footpath | Paths/Footpath | Narrow garden or meadow shortcuts. Paint one-cell lines for a roughly six-pixel worn trail; wider areas fill normally. |
| Cobbles | Paths/Paving | Short paved approaches and the village square. This reuses Ground's existing cobble brush and sprites. |

Each new dirt brush includes 47 connected shapes and three extra center fills (100 visual tiles total). Ends, bends, T/cross junctions, inner/outer corners and changes in width update automatically. The manual pieces are below the brush row in the same palette: DirtLane first, then Footpath, separated by a blank row. Plain manual tiles do not connect to RuleTiles; keep manual edits on their own map.

The one reusable `../Templates/TutorialZoneTemplate.prefab` includes all three Paths maps. Paint a footpath one cell underneath a lane when joining them; DirtLane renders over Footpath. Paving renders over both, so overlap one cell at dirt-to-stone thresholds. Each brush connects to its own neighbors on its own map. Ground/Earth remains the fill for bare yards, not the route brush.

Everything here is visual and walkable by default, with no tile-generated collision. Keep Collision clear along paths. `Assets/Scenes/DemoTutorial.unity` shows the lanes, a paved square/entrance and narrow trails to the spring and riverbank. No separate path preview scene or template is created.

Native 16px tiles, 16 PPU, point filtering, no mipmaps or compression. `TutorialPaths16.png` is 200x200 with 2px extrusion (20px pitch); sprite rectangles in `TileManifest.json` exclude padding. Run `Tools/Art/build_tutorial_paths.py` using Python/Pillow, then **Tools > The Lost Shrine > Build Tutorial Paths Kit**. This updates art, the single palette and the shared empty template; it never repaints your scene. The builder is Editor-only and the game uses stock Unity RuleTiles.
