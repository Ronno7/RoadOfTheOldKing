"""Package a user-built Unity Web export, or validate/extract it for GitHub Pages."""
import argparse
import re
import shutil
import stat
import zipfile
from html.parser import HTMLParser
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit


FOLDERS = {"Build", "TemplateData", "StreamingAssets"}
VERSION = re.compile(r"\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?")


def allowed(name):
    path = PurePosixPath(name)
    return ("\\" not in name and not path.is_absolute()
            and all(part not in {"", ".", ".."} for part in name.split("/"))
            and (name == "index.html" or (len(path.parts) > 1 and path.parts[0] in FOLDERS)))


class References(HTMLParser):
    def __init__(self):
        super().__init__()
        self.paths = []

    def handle_starttag(self, tag, attrs):
        for name, value in attrs:
            if value and name in {"src", "href"}:
                url = urlsplit(value)
                if not url.scheme and not url.netloc and url.path:
                    self.paths.append(unquote(url.path))


def validate(root, expected=None):
    html = (root / "index.html").read_text(encoding="utf-8-sig")
    version_match = re.search(r'productVersion\s*:\s*["\']([^"\']+)["\']', html)
    if not version_match or not VERSION.fullmatch(version_match[1]):
        raise ValueError("index.html must contain Unity's productVersion (for example 0.4.9).")
    version = version_match[1]
    if expected and version != expected:
        raise ValueError(f"Release version {expected} does not match build version {version}.")
    build = re.search(r'(?:var|let|const)\s+buildUrl\s*=\s*["\']([^"\']+)["\']', html)
    if not build or build[1] != "Build":
        raise ValueError("Expected the standard Unity Web template with buildUrl = 'Build'.")
    parser = References()
    parser.feed(html)
    paths = parser.paths
    for field in ("loaderUrl", "dataUrl", "frameworkUrl", "codeUrl"):
        match = re.search(r'\b' + field + r'\s*[:=]\s*buildUrl\s*\+\s*["\'](/[^"\']+)["\']', html)
        if not match:
            raise ValueError(f"Cannot identify {field} in index.html; use Unity's standard Web template.")
        paths.append("Build" + match[1])
    for name in paths:
        if not allowed(name) or not (root / name).is_file() or not (root / name).stat().st_size:
            raise ValueError(f"Missing, empty or unsupported build reference: {name}")
    # Pages cannot supply custom Content-Encoding headers for precompressed files.
    # Unity's decompression fallback uses .unityweb and handles this in the loader.
    if any(name.endswith((".gz", ".br")) for name in paths):
        raise ValueError("For compressed builds on Pages, enable Unity Decompression Fallback and rebuild (.unityweb files).")
    if (root / "ServiceWorker.js").exists() or (root / "manifest.webmanifest").exists():
        raise ValueError("PWA exports need additional publishing support; use Unity's standard Web template.")
    return version


def pack(source, output):
    source = source.resolve()
    version = validate(source)
    files = [source / "index.html"]
    for folder in sorted(FOLDERS):
        if (source / folder).exists():
            files.extend(sorted(p for p in (source / folder).rglob("*") if p.is_file()))
    for file in files:
        if file.is_symlink() or not file.resolve().is_relative_to(source):
            raise ValueError(f"Build contains a linked file: {file}")
        if not allowed(file.relative_to(source).as_posix()):
            raise ValueError(f"Unsupported build path: {file}")
    if output is None:
        repo = Path(__file__).resolve().parents[2]
        output = repo / "Builds" / "Releases" / version / "webgl.zip"
    output = output.resolve()
    if output.exists():
        raise ValueError(f"Archive already exists: {output}. Use --output for a different destination.")
    if output == source / "index.html" or any(output.is_relative_to(source / f) for f in FOLDERS):
        raise ValueError("Place the archive outside the exported build's asset folders.")
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for file in files:
            archive.write(file, file.relative_to(source).as_posix())
    total = sum(file.stat().st_size for file in files)
    print(f"Packaged {version}: {len(files)} files, {total / 1048576:.2f} MiB -> {output.stat().st_size / 1048576:.2f} MiB ZIP")
    print(output)


def unpack(source, output, version):
    if output.exists():
        raise ValueError(f"Extraction destination must not exist: {output}")
    with zipfile.ZipFile(source) as archive:
        files = [info for info in archive.infolist() if not info.is_dir()]
        seen = set()
        if sum(info.file_size for info in files) > 1024 ** 3:
            raise ValueError("Export exceeds the 1 GiB Pages site limit.")
        for info in files:
            if (not allowed(info.filename) or info.filename.casefold() in seen
                    or stat.S_ISLNK(info.external_attr >> 16)):
                raise ValueError(f"Unsupported or duplicate archive entry: {info.filename}")
            seen.add(info.filename.casefold())
        output.mkdir(parents=True)
        for info in files:
            target = output / info.filename
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(info) as src, target.open("xb") as dst:
                shutil.copyfileobj(src, dst)
    actual = validate(output, version)
    print(f"Validated {actual}: {len(files)} files ready for Pages at {output}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    package = commands.add_parser("pack", help="ZIP an existing Unity Web export; does not build or publish")
    package.add_argument("source", type=Path, help="Folder containing index.html and Build/")
    package.add_argument("--output", type=Path)
    extract = commands.add_parser("unpack", help="Validate and extract a release ZIP for Pages")
    extract.add_argument("source", type=Path)
    extract.add_argument("output", type=Path)
    extract.add_argument("--version", required=True)
    args = parser.parse_args()
    try:
        if args.command == "pack":
            pack(args.source, args.output)
        else:
            unpack(args.source, args.output, args.version)
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        parser.exit(1, f"Error: {error}\n")


if __name__ == "__main__":
    main()
