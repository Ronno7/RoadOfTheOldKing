# Wolf source art

## Current PixelLab export

`Idle/` and `metadata.json` hold the approved eight-direction PixelLab export used since 1 October: standing/idle, Fast_Walk, Run and Bark (used as the bite), with later approved death frames. Runtime imports use native 16 pixels per unit. Import through **Road of the Old King > Animation Import > Wolf**; the importer creates clips and the predator animation set without rebuilding scenes.

`Wolf.prefab` uses `PredatorBrain`, the shared enemy actions and `PredatorSpriteView`; `PredatorAnimationSet` extends the shared animation data. [Enemy architecture](../../../Docs/SystemsOverview.md#enemies) describes their responsibilities. The Wolf asset/prefab names remain; there is no separate wolf-only runtime namespace.

## Historical source record

`Provenance.json` preserves the earlier imagegen prompts. The old drawings, 24-PPU runtime sheet and exporter were retired; use the current PixelLab import path above.
