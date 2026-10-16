"""Fetch only the user-approved complete slots recorded in the Lowlands enemy manifest."""
import concurrent.futures
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
manifest = json.loads((ROOT / "ArtSource/Enemies/LowlandsManifest.json").read_text())


def fetch(task):
    character, frame = task
    relative = Path(character["name"]) / frame["slot"] / frame["file"]
    source = ROOT / "ArtSource/Enemies" / relative
    target = ROOT / "RoadOfTheOldKing/Assets/Art/Sprites/Enemies" / relative
    request = urllib.request.Request(frame["url"], headers={"User-Agent": "curl/8.0"})
    with urllib.request.urlopen(request, timeout=45) as response:
        data = response.read()
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError(f"Not a PNG: {relative}")
    for path in (source, target):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
    return len(data)


if __name__ == "__main__":
    tasks = [(c, f) for c in manifest["characters"] for f in c["files"]]
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        sizes = list(pool.map(fetch, tasks))
    print(f"Downloaded {len(sizes)} native PNGs ({sum(sizes)} bytes).")
