# Tools

Development tooling. The running game needs none of it.

| Folder | Does |
| --- | --- |
| [Art](Art) | Exports native tiles, UI art and fonts; downloads and fits recorded enemy art |
| [Audio](Audio) | Builds combat, reward and machinery sounds |
| [LevelLayout](LevelLayout) | Exports scene terrain and applies reviewed terrain edits |
| [Pacing](Pacing) | Simulates a route over real collision and charts its pacing. These are estimates, not playtests |
| [Release](Release) | Validates and packages a Unity WebGL export |
| [Verification](Verification) | Editor check snippets and offline regressions |

Sprite imports run inside Unity: **Road of the Old King > Animation Import**.

Files named `.cs.txt` are snippets run in the Editor against isolated saves, not an automatic test suite. Offline regressions need neither Unity nor saves:

```
python -B -m unittest discover -s Tools/Verification/Pacing -p "test_*.py"
python -B -m unittest discover -s Tools/Verification/Release -p "test_*.py"
python -B -m unittest discover -s Tools/Verification/Art -p "test_*.py"
```
