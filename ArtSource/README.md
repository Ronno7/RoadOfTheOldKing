# Art sources

Source art and import inputs, kept outside Unity's runtime assets. Runtime sprites and tiles live in `RoadOfTheOldKing/Assets/Art`. See [World and tilemaps](../Docs/World.md#art-pipeline) for how they get there.

| Folder | Holds | Imported with |
| --- | --- | --- |
| `Player/` | Hero exports from PixelLab, as delivered | **Animation Import > Player** |
| `Enemies/` | Enemy exports, recorded in `LowlandsManifest.json`; the wolf in its own folder | **Animation Import**, per character |
| `NPCs/TravelingTrader/` | Merchant frames and source record | **Animation Import > Traveling Trader** |
| `Shared/` | Trees, vegetation, props, bonfire, decoration, palette | **Advanced > Asset Maintenance** |
| `Tutorial/` | Village buildings and recipes | **Advanced > Asset Maintenance** |
| `GreenLowlands/` | Buildings, fittings, elevation data, provenance | **Advanced > Asset Maintenance** |
| `GreenLowlands/Environment/Waterworks/` | Sluice, canal, culvert and drain sources, with one manifest per set | **Animation Import** |
| `GreenLowlands/Environment/Hayfields/` | Beam and release mechanism | **Animation Import** |
| `Weapons/` | Halberd and scavenger spear | Copied into `Assets/Art/Sprites` |

Keep only what an import reads, plus its provenance. Review material doesn't belong here.
