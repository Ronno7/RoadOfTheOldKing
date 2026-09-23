# Zone tile art

Keep tile art organized as **zone / layer / assets**.

- **Tutorial/** — the warm Hearth & Meadow tutorial art. Ground, Terrain, Collision, Paths, Environment, DetailDecoration and AbovePlayer are separate folders. Ground, Terrain and Paths contain their atlases, Tiles, Rules, Palettes and documentation; Terrain also contains Animations. Environment, DetailDecoration and AbovePlayer are reserved for later art passes.
- **Prototype/Ground/** — the original mixed prototype tile kit, moved intact so PrototypeLoop retains its references.

One reusable blank template lives under `Tutorial/Templates/TutorialZoneTemplate.prefab`, with Ground, Terrain and Paths ready to paint. Interactive objects remain GameObjects under `Assets/Prefabs/`, rather than baked tile art.

**Open `Assets/Scenes/DemoTutorial.unity` for the world-only presentation.** It showcases the available material families, connected terrain and water animations in one landscape. There are no players, enemies, UI or gameplay scripts. Use Play Mode to see the tile animations. This is a presentation scene; author the real tutorial separately.

Kit rebuild commands update assets and palettes. They do not create presentation scenes. The separate **Tools > The Lost Shrine > Rebuild Demo Tutorial Presentation** command deliberately replaces the demo layout; normal scene editing does not require it. The demo is not in gameplay build settings.
