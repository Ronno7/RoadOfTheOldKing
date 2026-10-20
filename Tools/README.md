# Development tooling

These tools support asset generation and focused verification. They are not required by the running game. Player sprites are imported inside Unity by **Road of the Old King > Animation Import > Player**.

| Directory | Responsibility |
| --- | --- |
| [Art/World](Art/World) | Native tile geometry, atlas generation and manifests |
| [Art/UI](Art/UI) | Pixel font, HUD icons and panel art |
| [Verification](Verification) | Gameplay, player animation, progression and world-asset checks |

Verification `.cs.txt` files are focused Unity Editor snippets, not an automatically discovered test suite. Check their assumptions against the current code and use isolated saves for gameplay checks.

The implementation is described in the [systems overview](../Docs/SystemsOverview.md), [player animation](../Docs/PlayerAnimation.md) and [world documentation](../Docs/World.md).
