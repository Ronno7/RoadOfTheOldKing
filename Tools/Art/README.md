# Art build tools

Python exporters require Python 3 with Pillow and run from the repository root. Normal playing/painting uses the generated Unity assets and does not run these tools.

| Folder | What it maintains |
| --- | --- |
| [Player](Player) | Accepted drawing registration, synchronized body/axe/reveal layers, sprint variants, prop views and current comparison boards |
| [World](World) | Native Ground, Terrain and Paths pixel geometry, manifests and atlas previews |

Player recipes and shared helper modules stay together. Follow the [player rebuild guide](../../ArtSource/Player/README.md#rebuild-and-verification); rebuild only the affected family, then its Unity importer.

World generators are `build_tutorial_ground.py`, `build_tutorial_terrain.py` and `build_tutorial_paths.py`. Example: `python Tools/Art/World/build_tutorial_terrain.py`. For a complete rebuild use Ground → Terrain → Paths, followed by the corresponding **Tools → The Lost Shrine → Build Tutorial … Kit** menus. Builders import/slice/register assets; they do not repaint scenes.

Environment/Decoration use the Unity Editor builders directly with [ArtSource/Tutorial](../../ArtSource/Tutorial/README.md). Accepted palette guides remain in [Docs/Art/Palettes](../../Docs/Art/Palettes/OpeningZonePalettes-v1.md). Exporters overwrite generated files, so inspect the resulting diff and run affected checks before accepting changes.
