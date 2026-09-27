# Player source art

These files preserve the source drawings and registration data behind the runtime player sprites.

| Directory | Contents |
| --- | --- |
| Forehand / Throw / Catch / Dash | Whole-body drawings, weapon masks and hidden-body underpaint |
| Sprint | Directional sprint drawings and armed/unarmed source inputs |
| Weapon | Pickup, held-weapon and detached-spin source artwork |
| Registered | Combined masters, source-only reveal sheets and frame manifests |

JSON recipes in `Tools/Art/Player` describe source rectangles, scale, foot anchors and masks. Runtime body and weapon layers reconstruct the combined master. Reveal sheets support editor inspection and are not runtime dependencies.

[Provenance.json](Provenance.json) records original generation/edit briefs; names in historical briefs are not necessarily active dependencies.

See [player animation](../../Docs/PlayerAnimation.md) for layering, timing and gameplay previews.
