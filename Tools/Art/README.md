# Art build tools

Keep these three scripts: they are the source generators for the current Ground, Terrain and Paths kits. The Unity project already contains their generated assets, so opening, painting or playing the game does not require running Python.

| Script | Produces |
| --- | --- |
| [build_tutorial_ground.py](build_tutorial_ground.py) | Ground textures and connected material shapes |
| [build_tutorial_terrain.py](build_tutorial_terrain.py) | Water/banks, ledges, walls, stairs and water-animation pixels |
| [build_tutorial_paths.py](build_tutorial_paths.py) | Connected dirt lanes and footpaths |

Each writes a native PNG atlas, tile manifest/index and generation report into its existing Unity art folder, plus a contact sheet in [Docs/Art/Tutorial/Previews](../../Docs/Art/Tutorial/Previews/tutorial-ground-production-atlas.png). Terrain and Paths import shared palette/connection helpers from the Ground script.

## When to use them

Run a generator when changing or reconstructing the pixel geometry for its kit. It overwrites that kit's generated art and metadata, so review the resulting diff before accepting changes. Ordinary level painting uses the existing Unity palettes.

From the repository root, using Python 3 with Pillow installed:

```powershell
python Tools/Art/build_tutorial_ground.py
python Tools/Art/build_tutorial_terrain.py
python Tools/Art/build_tutorial_paths.py
```

Run only the affected generator, then its matching **Tools > The Lost Shrine > Build Tutorial Ground/Terrain/Paths Kit** command in Unity. For a complete rebuild, use Ground first, then Terrain, then Paths. The Unity builders import/slice the output and update tiles, RuleTiles, palettes and the shared blank template. They do not repaint scenes.

Environment and Decoration follow a different source path: their Unity Editor builders read the approved/generated images in [Docs/Art/Tutorial/Sources](../../Docs/Art/README.md), then prepare the native atlases. All of these are authoring tools, excluded from runtime gameplay.
