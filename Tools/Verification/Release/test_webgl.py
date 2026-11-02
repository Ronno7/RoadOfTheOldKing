"""Packaging checks using synthetic exports; no Unity build or publication."""
import contextlib
import importlib.util
import io
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

SPEC = importlib.util.spec_from_file_location(
    "webgl", Path(__file__).resolve().parents[2] / "Release/webgl.py")
webgl = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(webgl)

HTML = '''<link href="TemplateData/style.css" rel="stylesheet">
<script>
var buildUrl = "Build";
var loaderUrl = buildUrl + "/game.loader.js";
var config = {
  dataUrl: buildUrl + "/game.data.unityweb",
  frameworkUrl: buildUrl + "/game.framework.js.unityweb",
  codeUrl: buildUrl + "/game.wasm.unityweb",
  productVersion: "0.5.0-dev.1"
};
</script>'''


class WebGLTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.addCleanup(lambda: self.assertFalse(list(self.root.glob(".webgl-*"))))
        self.source = self.root / "export"
        self.source.mkdir()
        self.files = {"index.html": HTML.encode(), "TemplateData/style.css": b"body{}",
                      "StreamingAssets/catalog.json": b"{}"}
        for name in ("game.loader.js", "game.data.unityweb", "game.framework.js.unityweb", "game.wasm.unityweb"):
            self.files["Build/" + name] = b"synthetic export fixture"
        for name, data in self.files.items():
            path = self.source / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        self.archive = self.root / "webgl.zip"
        self.site = self.root / "site"
        self.quiet = contextlib.redirect_stdout(io.StringIO())
        self.quiet.__enter__()
        self.addCleanup(self.quiet.__exit__, None, None, None)

    def pack(self):
        webgl.pack(self.source, self.archive)

    def unpack(self, version="0.5.0-dev.1"):
        webgl.unpack(self.archive, self.site, version)

    def test_round_trip_preserves_every_file(self):
        self.pack()
        self.unpack()
        actual = {p.relative_to(self.site).as_posix(): p.read_bytes() for p in self.site.rglob("*") if p.is_file()}
        self.assertEqual(self.files, actual)

    def test_pack_write_failure_leaves_no_archive_and_can_retry(self):
        original = zipfile.ZipFile.write
        calls = 0

        def fail_second(archive, *args, **kwargs):
            nonlocal calls
            calls += 1
            if calls == 2:
                raise OSError("simulated disk failure")
            return original(archive, *args, **kwargs)

        with patch.object(zipfile.ZipFile, "write", fail_second), self.assertRaises(OSError):
            self.pack()
        self.assertFalse(self.archive.exists())
        self.pack()
        self.unpack()

    def test_wrong_version_leaves_no_site_and_can_retry(self):
        self.pack()
        with self.assertRaisesRegex(ValueError, "does not match"):
            self.unpack("0.4.9")
        self.assertFalse(self.site.exists())
        self.unpack()

    def test_final_archive_copy_failure_is_cleaned_up(self):
        original = webgl.shutil.copyfileobj

        def fail_copy(src, dst, *args, **kwargs):
            if getattr(dst, "name", None) == str(self.archive):
                dst.write(b"partial archive")
                raise OSError("simulated final copy failure")
            return original(src, dst, *args, **kwargs)

        with patch.object(webgl.shutil, "copyfileobj", side_effect=fail_copy), self.assertRaises(OSError):
            self.pack()
        self.assertFalse(self.archive.exists())
        self.pack()
        self.unpack()

    def test_archive_created_during_compression_is_preserved(self):
        original = zipfile.ZipFile.write

        def concurrent_output(archive, *args, **kwargs):
            self.archive.write_bytes(b"another release")
            return original(archive, *args, **kwargs)

        with patch.object(zipfile.ZipFile, "write", concurrent_output), self.assertRaises(FileExistsError):
            self.pack()
        self.assertEqual(b"another release", self.archive.read_bytes())

    def test_destination_created_during_validation_is_preserved(self):
        self.pack()
        original = webgl.validate

        def concurrent_output(*args, **kwargs):
            actual = original(*args, **kwargs)
            self.site.mkdir()
            (self.site / "keep.txt").write_bytes(b"another site")
            return actual

        with patch.object(webgl, "validate", concurrent_output), self.assertRaisesRegex(ValueError, "appeared during validation"):
            self.unpack()
        self.assertEqual(b"another site", (self.site / "keep.txt").read_bytes())

    def test_copy_failure_leaves_no_site_and_can_retry(self):
        self.pack()
        with patch.object(webgl.shutil, "copyfileobj", side_effect=OSError("simulated disk failure")), self.assertRaises(OSError):
            self.unpack()
        self.assertFalse(self.site.exists())
        self.unpack()

    def test_missing_reference_leaves_no_site(self):
        with zipfile.ZipFile(self.archive, "w") as archive:
            archive.writestr("index.html", HTML)
        with self.assertRaisesRegex(ValueError, "Missing, empty"):
            self.unpack()
        self.assertFalse(self.site.exists())

    def test_existing_archive_is_preserved(self):
        self.archive.write_bytes(b"existing release")
        with self.assertRaisesRegex(ValueError, "already exists"):
            self.pack()
        self.assertEqual(b"existing release", self.archive.read_bytes())

    def test_existing_site_is_preserved(self):
        self.pack()
        self.site.mkdir()
        sentinel = self.site / "keep.txt"
        sentinel.write_bytes(b"existing site")
        with self.assertRaisesRegex(ValueError, "must not exist"):
            self.unpack()
        self.assertEqual(b"existing site", sentinel.read_bytes())

    def test_archive_path_traversal_is_rejected_without_output(self):
        with zipfile.ZipFile(self.archive, "w") as archive:
            archive.writestr("Build/../../outside.txt", "must not escape")
        with self.assertRaisesRegex(ValueError, "Unsupported"):
            self.unpack()
        self.assertFalse(self.site.exists())
        self.assertFalse((self.root / "outside.txt").exists())

    def test_case_colliding_entries_are_rejected(self):
        with zipfile.ZipFile(self.archive, "w") as archive:
            archive.writestr("Build/file", "first")
            archive.writestr("Build/FILE", "second")
        with self.assertRaisesRegex(ValueError, "duplicate"):
            self.unpack()
        self.assertFalse(self.site.exists())

    def test_direct_compression_is_rejected_before_packaging(self):
        (self.source / "index.html").write_text(HTML.replace("game.data.unityweb", "game.data.br"))
        (self.source / "Build/game.data.br").write_bytes(b"compressed data")
        with self.assertRaisesRegex(ValueError, "Decompression Fallback"):
            self.pack()
        self.assertFalse(self.archive.exists())


if __name__ == "__main__":
    unittest.main()
