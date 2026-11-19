# Tools

Development tooling. The running game needs none of it.

| Folder | Does |
| --- | --- |
| [Art](Art) | Exports native tiles, UI art and fonts; downloads and fits recorded enemy art |
| [Audio](Audio) | Builds combat, reward and machinery sounds |
| [Pacing](Pacing) | Simulates a route over real collision and charts its pacing. These are estimates, not playtests |
| [Verification](Verification) | Editor check snippets not yet moved to the Test Runner, and offline regressions |

Sprite imports run inside Unity: **Road of the Old King > Animation Import**.

Automatic tests live in the Unity project (`Assets/Tests`): **Window > General > Test Runner > PlayMode > Run All**. Files named `.cs.txt` are older snippets run in the Editor against isolated saves; each moves into the Test Runner when its area is next touched. Offline regressions need neither Unity nor saves:

```
python -B -m unittest discover -s Tools/Verification/Pacing -p "test_*.py"
python -B -m unittest discover -s Tools/Verification/Art -p "test_*.py"
```
