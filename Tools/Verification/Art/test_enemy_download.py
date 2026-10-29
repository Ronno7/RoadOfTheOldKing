"""Offline regression checks: all writes stay inside a temporary fake project."""
import base64
import contextlib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location(
    "enemy_download", Path(__file__).resolve().parents[2] / "Art/download_lowlands_enemies.py")
download = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(download)
PNG = base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9n8AAAAASUVORK5CYII=")


class EnemyDownloadTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.manifest_path = self.root / "ArtSource/Enemies/LowlandsManifest.json"
        self.manifest_path.parent.mkdir(parents=True)
        self.frames = [dict(slot="Idle", file=f"south/frame_{i:03}.png", url=f"https://example.invalid/{i}") for i in range(2)]
        self.character = dict(name="Fixture", files=self.frames)
        self.manifest = dict(characters=[self.character])
        self.save_manifest()

    def save_manifest(self):
        self.manifest_path.write_text(json.dumps(self.manifest), encoding="utf-8")

    def destinations(self, index):
        return download.paths(self.root, self.character, self.frames[index])[1]

    def seed(self, index, data=PNG):
        for path in self.destinations(index):
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)

    def snapshot(self):
        return {p.relative_to(self.root).as_posix(): p.read_bytes() for p in self.root.rglob("*") if p.is_file()}

    def run_download(self, verify=False):
        with contextlib.redirect_stdout(io.StringIO()):
            download.run(self.root, self.manifest_path, verify)

    def test_verify_recorded_art_is_offline_and_read_only(self):
        for i, frame in enumerate(self.frames):
            self.seed(i)
            frame["sha256"] = hashlib.sha256(PNG).hexdigest()
        self.save_manifest()
        before = self.snapshot()
        with patch.object(download, "fetch") as fetch:
            self.run_download(verify=True)
            fetch.assert_not_called()
        self.assertEqual(before, self.snapshot())

    def test_verify_pending_art_does_not_download_or_write(self):
        before = self.snapshot()
        with patch.object(download, "fetch") as fetch, self.assertRaisesRegex(ValueError, "still need downloading"):
            self.run_download(verify=True)
        fetch.assert_not_called()
        self.assertEqual(before, self.snapshot())

    def test_recorded_mismatch_blocks_new_downloads(self):
        self.seed(0)
        self.frames[0]["sha256"] = hashlib.sha256(PNG).hexdigest()
        self.destinations(0)[1].write_bytes(b"accepted local edit")
        self.save_manifest()
        before = self.snapshot()
        with patch.object(download, "fetch") as fetch, self.assertRaisesRegex(ValueError, "differ or are missing"):
            self.run_download()
        fetch.assert_not_called()
        self.assertEqual(before, self.snapshot())

    def test_failed_download_leaves_entire_batch_untouched(self):
        before = self.snapshot()
        with patch.object(download, "fetch", side_effect=[PNG, OSError("network failure")]), self.assertRaises(OSError):
            self.run_download()
        self.assertEqual(before, self.snapshot())

    def test_unrecorded_existing_art_is_never_replaced(self):
        self.seed(1, b"accepted art without a hash")
        before = self.snapshot()
        with patch.object(download, "fetch", return_value=PNG), self.assertRaisesRegex(ValueError, "Refusing to replace"):
            self.run_download()
        self.assertEqual(before, self.snapshot())

    def test_success_records_both_copies_and_repeat_is_offline(self):
        meta = self.destinations(0)[1].with_suffix(".png.meta")
        meta.parent.mkdir(parents=True)
        meta.write_bytes(b"existing Unity GUID")
        with patch.object(download, "fetch", return_value=PNG):
            self.run_download()
        saved = json.loads(self.manifest_path.read_text())
        for i, frame in enumerate(saved["characters"][0]["files"]):
            self.assertEqual(hashlib.sha256(PNG).hexdigest(), frame["sha256"])
            for path in self.destinations(i):
                self.assertEqual(PNG, path.read_bytes())
        self.assertEqual(b"existing Unity GUID", meta.read_bytes())
        before = self.snapshot()
        with patch.object(download, "fetch") as fetch:
            self.run_download()
            fetch.assert_not_called()
        self.assertEqual(before, self.snapshot())

    def test_identical_interrupted_copy_can_be_recovered(self):
        source = self.destinations(0)[0]
        source.parent.mkdir(parents=True)
        source.write_bytes(PNG)
        with patch.object(download, "fetch", return_value=PNG):
            self.run_download()
        self.assertEqual(PNG, self.destinations(0)[1].read_bytes())
        self.run_download(verify=True)

    def test_manifest_write_failure_keeps_original_and_can_retry(self):
        original = self.manifest_path.read_bytes()
        with patch.object(download, "fetch", return_value=PNG), patch.object(Path, "replace", side_effect=OSError("disk failure")), self.assertRaises(OSError):
            self.run_download()
        self.assertEqual(original, self.manifest_path.read_bytes())
        self.assertFalse(list(self.manifest_path.parent.glob(".enemy-manifest-*.tmp")))
        with patch.object(download, "fetch", return_value=PNG):
            self.run_download()
        self.run_download(verify=True)

    def test_manifest_edit_during_download_is_preserved(self):
        def fetch(frame):
            if frame is self.frames[0]:
                raise AssertionError("run must load its own manifest")
            if frame["file"].endswith("000.png"):
                self.manifest_path.write_bytes(b'{"edited":true}')
            return PNG
        with patch.object(download, "fetch", side_effect=fetch), self.assertRaisesRegex(ValueError, "manifest changed"):
            self.run_download()
        self.assertEqual({"ArtSource/Enemies/LowlandsManifest.json": b'{"edited":true}'}, self.snapshot())

    def test_duplicate_destination_is_rejected(self):
        self.frames[1]["file"] = self.frames[0]["file"].upper()
        self.save_manifest()
        with patch.object(download, "fetch") as fetch, self.assertRaisesRegex(ValueError, "Duplicate"):
            self.run_download()
        fetch.assert_not_called()

    def test_path_outside_art_root_is_rejected(self):
        self.character["name"] = "../../../outside"
        self.save_manifest()
        with patch.object(download, "fetch") as fetch, self.assertRaisesRegex(ValueError, "leaves its art folder"):
            self.run_download()
        fetch.assert_not_called()

    def test_non_png_response_leaves_no_copies(self):
        before = self.snapshot()
        with patch.object(download.urllib.request, "urlopen", side_effect=lambda *a, **k: io.BytesIO(b"server error")), self.assertRaisesRegex(ValueError, "Not a PNG"):
            self.run_download()
        self.assertEqual(before, self.snapshot())


if __name__ == "__main__":
    unittest.main()
