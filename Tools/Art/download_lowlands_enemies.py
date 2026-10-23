"""Fetch only the user-approved complete slots recorded in the Lowlands enemy manifest.

Entries that already carry a sha256 are verified on disk and never re-downloaded, so PixelLab regenerations can't
silently replace imported art. New entries are fetched, written to ArtSource and Assets, and their hash recorded."""
import concurrent.futures
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "ArtSource/Enemies/LowlandsManifest.json"
manifest = json.loads(MANIFEST.read_text())


def paths(character, frame):
    relative = Path(character["name"]) / frame["slot"] / frame["file"]
    return relative, ROOT / "ArtSource/Enemies" / relative, ROOT / "RoadOfTheOldKing/Assets/Art/Sprites/Enemies" / relative


def fetch(task):
    character, frame = task
    relative, source, target = paths(character, frame)
    request = urllib.request.Request(frame["url"], headers={"User-Agent": "curl/8.0"})
    with urllib.request.urlopen(request, timeout=45) as response:
        data = response.read()
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError(f"Not a PNG: {relative}")
    for path in (source, target):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
    frame["sha256"] = hashlib.sha256(data).hexdigest()
    return len(data)


if __name__ == "__main__":
    pending, mismatched = [], []
    for c in manifest["characters"]:
        for f in c["files"]:
            if "sha256" not in f:
                pending.append((c, f))
                continue
            relative, source, target = paths(c, f)
            for path in (source, target):
                if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest() != f["sha256"]:
                    mismatched.append(str(path.relative_to(ROOT)))
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        sizes = list(pool.map(fetch, pending))
    MANIFEST.write_text(json.dumps(manifest, indent=1) + "\n")
    print(f"Downloaded {len(sizes)} new native PNGs ({sum(sizes)} bytes); recorded files differing from the manifest: {len(mismatched)}.")
    for path in mismatched[:20]:
        print("  differs:", path)
