# Tutorial decoration sources

Production approved on 23 September 2026, with a deliberately small scope: **12 designs total**.

| Source | Purpose | Prompt |
| --- | --- | --- |
| [Sample.png](Sample.png) | Original six designs; approved style reference | [Sample prompt](Sample-prompt.txt) |
| [ApprovedSample16.png](ApprovedSample16.png) | Exact approved native pixels, copied unchanged into production | Native preparation of Sample.png |
| [Expansion.png](Expansion.png) | Loose grass, split logs, bucket, sack, worn earth and unlit fire ring | [Expansion prompt](Expansion-prompt.txt) |

Both generated sheets used the **built-in image tool**. The sample referenced the approved Environment source; the expansion referenced the decoration sample. Generated source files are retained unchanged.

`TutorialDecorationKitBuilder.cs` fits new sprites to the 16-pixel grid, maps them to the approved palette and makes alpha binary. The six approved native sprites are copied directly. The sample's Unity assets were promoted into the production folder while preserving their GUIDs; there is no duplicate sample kit or builder.

[Approved sample preview](Sample-preview.png) · [Current village detail](../../Previews/tutorial-decoration-village.png) · [Current practice/rest detail](../../Previews/tutorial-decoration-practice.png) · [Painting guide](../../../../../TheLostShrine/Assets/Art/Tiles/Tutorial/DetailDecoration/README.md)

Scope and deferred work remain in the [single village plan](../../Plan.md).
