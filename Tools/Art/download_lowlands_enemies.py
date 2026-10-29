"""Fetch approved enemy slots; --verify performs an offline, read-only manifest audit.

Recorded frames must match both local copies. Download the whole pending batch
before copying any frames, and never replace differing existing art.
"""
import argparse
import concurrent.futures
import hashlib
import json
from pathlib import Path
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "ArtSource/Enemies/LowlandsManifest.json"


def paths(root, character, frame):
    relative = Path(character["name"]) / frame["slot"] / frame["file"]
    roots = (root / "ArtSource/Enemies", root / "RoadOfTheOldKing/Assets/Art/Sprites/Enemies")
    destinations = tuple(base / relative for base in roots)
    if any(not path.resolve().is_relative_to(base.resolve()) for base, path in zip(roots, destinations)):
        raise ValueError(f"Frame path leaves its art folder: {relative}")
    return relative, destinations


def inspect(root, manifest):
    pending, mismatched, seen = [], [], set()
    recorded = 0
    for character in manifest["characters"]:
        for frame in character["files"]:
            relative, destinations = paths(root, character, frame)
            key = relative.as_posix().casefold()
            if key in seen:
                raise ValueError(f"Duplicate frame destination: {relative}")
            seen.add(key)
            if "sha256" not in frame:
                pending.append((frame, destinations))
                continue
            recorded += 1
            for path in destinations:
                if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != frame["sha256"]:
                    mismatched.append(str(path.relative_to(root)))
    if mismatched:
        raise ValueError(f"{len(mismatched)} recorded art copies differ or are missing; no files changed:\n  "
                         + "\n  ".join(mismatched[:20]))
    return recorded, pending


def fetch(frame):
    request = urllib.request.Request(frame["url"], headers={"User-Agent": "curl/8.0"})
    with urllib.request.urlopen(request, timeout=45) as response:
        data = response.read()
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError(f"Not a PNG: {frame['file']}")
    return data


def run(root=ROOT, manifest_path=MANIFEST, verify_only=False):
    original = manifest_path.read_bytes()
    manifest = json.loads(original)
    recorded, pending = inspect(root, manifest)
    if verify_only and pending:
        raise ValueError(f"Verified {recorded} recorded frames; {len(pending)} frames still need downloading. No files changed.")
    if not pending:
        print(f"Verified {recorded} frames in ArtSource and Assets; no files changed.")
        return

    # A failed request must not leave a partly copied, unrecorded animation set.
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        downloads = list(pool.map(fetch, (frame for frame, _ in pending)))
    if manifest_path.read_bytes() != original:
        raise ValueError("The manifest changed during download; no files copied. Run again after its edit is complete.")
    inspect(root, manifest)  # Recorded art may have changed during the requests.
    for (_, destinations), data in zip(pending, downloads):
        for path in destinations:
            if path.exists() and (not path.is_file() or path.read_bytes() != data):
                raise ValueError(f"Refusing to replace existing art without a matching recorded hash: {path}")

    for (frame, destinations), data in zip(pending, downloads):
        for path in destinations:
            if path.exists():
                if path.read_bytes() != data:
                    raise ValueError(f"Art changed during copying; kept that file: {path}")
                continue  # Identical files from an earlier interrupted copy are safe to record.
            path.parent.mkdir(parents=True, exist_ok=True)
            with path.open("xb") as output:  # Never overwrite files created while downloading.
                output.write(data)
        frame["sha256"] = hashlib.sha256(data).hexdigest()

    # Keep the original manifest if writing the updated JSON fails.
    staged = None
    try:
        with tempfile.NamedTemporaryFile(dir=manifest_path.parent, prefix=".enemy-manifest-", suffix=".tmp", delete=False) as output:
            staged = Path(output.name)
            output.write((json.dumps(manifest, indent=1) + "\n").encode("utf-8"))
        if manifest_path.read_bytes() != original:
            raise ValueError("Manifest edited during copying; kept that edit. Copied frames remain unrecorded; verify before retrying.")
        staged.replace(manifest_path)
    finally:
        if staged is not None and staged.exists():
            staged.unlink()
    print(f"Downloaded {len(downloads)} new native PNGs ({sum(map(len, downloads))} bytes); verified {recorded} recorded frames.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify", action="store_true", help="Check both local copies without network requests or writes")
    args = parser.parse_args()
    try:
        run(verify_only=args.verify)
    except (OSError, ValueError, KeyError) as error:
        parser.exit(1, f"Error: {error}\n")


if __name__ == "__main__":
    main()
