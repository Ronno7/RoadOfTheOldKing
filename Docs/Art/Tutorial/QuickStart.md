# Build a tutorial scene with the existing tilemaps

Open the Unity project in `TheLostShrine/`. All `Assets/...` paths below are relative to that project. Use `Assets/Scenes/DemoTutorial.unity` as a visual reference; it contains art only. The playable zone is Assets/Scenes/Tutorial.unity.

**Current scene:** `Assets/Scenes/Tutorial.unity` includes the southern bridge, winding forest road, Recall courtyard/stone placeholder and safe practice clearing. Southern props are under Environment/Ancient Road and Recall Ruins; earlier props are under Environment/Village Dressing. The 100x100 playable bounds have additional camera backdrop scenery. Recall behavior remains deferred. Player locomotion, idle, follow/zoom camera and stump pickup/carrying are connected. Continue with combat presentation and the remaining gameplay; do not rerun builders. See [current layout and approaches](Plan.md#terrain-blockout-in-tutorialunity).

1. **For a new zone only, start with the blank template.** Create and save a new scene; do not recreate the existing Tutorial. Drag `Assets/Art/Tiles/Tutorial/Templates/TutorialZoneTemplate.prefab` into the scene at the origin. Unpack this instance if you need to change its hierarchy. Keep its one-unit grid and scale: the imported art is 16 pixels per unit, with one tile per cell.

2. **Paint the ground and teaching route.** Open **Window > 2D > Tile Palette**. Choose a palette, select the destination in **Active Tilemap**, then select its brush and paint in the Scene view. Start by filling `Ground/Grass`; add yards and routes using the mappings below.

   | Palette / brush | Active Tilemap | Use |
   | --- | --- | --- |
   | TutorialGround_Paint / Grass | Ground/Grass | Continuous base beneath the whole playable area |
   | TutorialGround_Paint / Earth | Ground/Earth | Bare yards and practice areas |
   | TutorialPaths / DirtLane | Paths/DirtLane | Main route, 2–3 cells wide |
   | TutorialPaths / Footpath | Paths/Footpath | One-cell garden/meadow trails |
   | TutorialPaths / Cobbles | Paths/Paving | Village square and paved approaches |
   | TutorialTerrain_Paint / MeadowWater | Terrain/MeadowWater | Stream or pond with built-in banks |
   | TutorialTerrain_Paint / GrassLedge or StoneWall | Matching map under Terrain | Raised ground or low walls |

   Automatic brushes connect as you paint and erase. Keep each material on its named map; manual tiles do not connect to RuleTiles. Overlap footpaths one cell beneath lanes, and dirt routes one cell beneath paving, to avoid gaps.

3. **Arrange the lesson sequence.** Follow the [village plan](Plan.md): home, stump, throwing practice, dummy yard, forest road, southern bridge, Recall awakening, safe Recall practice, easy enemy/first Sun Shard, bonfire, overworld exit. Keep practice spaces open and essential openings at least two cells wide.

4. **Place structures, then decoration.** Drag complete objects from `Assets/Prefabs/Environment/Tutorial`: cottages, trees, stump, fences and `Stone_Bridge`. These include separate base art, overhead art and collision. Keep overlapping objects as independent instances. Paint sparse flowers, leaves and wood chips from **TutorialDecoration** onto **Detail Decoration**. For manual structure painting, use **TutorialEnvironment**, putting upper canopy/roof pieces on **Above Player** and authoring collision separately.

5. **Block inaccessible areas.** In **TutorialGround_Paint**, select the red collision brush below the material brushes and paint on **Collision**. Block water, walls and inaccessible drops; ordinary visual tiles do not block movement. Leave gates, stairs and routes clear. The bridge spans 10×4 cells: erase world collision beneath its middle two deck rows so its crossing stays walkable. The Collision renderer is intentionally hidden.

6. **Connect remaining gameplay and walk the route.** Use `Assets/Scenes/PrototypeLoop.unity` as the reference for working player, camera, combat and checkpoint wiring. Place gameplay objects under **Interactive Objects** and connect their scene references. Prefabs in `Assets/Prefabs/WorldArt/Tutorial` are visual-only: targets, dummy and fire ring need gameplay integration. In Play Mode, check the complete route, solid boundaries, bridge crossing, canopy sorting and each lesson's behavior.

Normal painting requires no regeneration. **Rebuild Demo Tutorial Presentation** replaces the demo layout; it is not a save or refresh command. For stairs, waterfalls and other detailed techniques, see the [layer-specific painting guides](../ZoneTilemapStandard.md).
