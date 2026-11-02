using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    // The character importer measures feet. This mechanism instead keeps a fixed authored hinge.
    public static class HayfieldsAnimationImporter
    {
        public const string ArtPath = "Assets/Art/World/Hayfields/RecallReleaseArt.asset";
        private const string Destination = "Assets/Art/World/Hayfields";
        [Serializable] private sealed class Manifest
        {
            public int version, width, height, columns, frames, pixelsPerUnit, pivotX, pivotY;
            public Track[] tracks;
        }
        [Serializable] private sealed class Track
        {
            public string name, file, sha256;
            // An approved revision can replace only the exact previously imported bytes.
            public string[] replacesFrameHashes;
        }

        private static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public static string Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Import in stopped Edit Mode.");
            string source = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/GreenLowlands/Environment/Hayfields/Production"));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source, "Manifest.json")));
            if (manifest.version != 1 || manifest.width != 256 || manifest.height != 176 ||
                manifest.columns != 3 || manifest.frames != 7 || manifest.pixelsPerUnit != 16 ||
                manifest.pivotX != 120 || manifest.pivotY != 52 || manifest.tracks == null ||
                !manifest.tracks.Select(t => t.name).OrderBy(n => n).SequenceEqual(new[] { "BackRail", "Floor", "FrontRail", "Hinge", "Release" }))
                throw new InvalidDataException("Unexpected Hayfields manifest layout.");

            // Validate every source and existing destination before writing anything.
            var pending = new Dictionary<string, byte[]>();
            foreach (var track in manifest.tracks)
            {
                if (track.file != track.name + ".png") throw new InvalidDataException("Unexpected source filename.");
                byte[] bytes = File.ReadAllBytes(Path.Combine(source, track.file));
                using (var hash = SHA256.Create())
                    if (BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() != track.sha256)
                        throw new InvalidDataException("Source hash differs: " + track.file);
                var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var frame = new Texture2D(manifest.width, manifest.height, TextureFormat.RGBA32, false);
                try
                {
                    if (!sheet.LoadImage(bytes) || sheet.width != 768 || sheet.height != 528)
                        throw new InvalidDataException("Unexpected source sheet dimensions.");
                    var pixels = sheet.GetPixels32();
                    if (pixels.Any(p => p.a != 0 && p.a != 255)) throw new InvalidDataException("Soft alpha in " + track.file);
                    for (int i = 0; i < manifest.frames; i++)
                    {
                        int x = i % manifest.columns * manifest.width;
                        int y = sheet.height - (i / manifest.columns + 1) * manifest.height;
                        var cut = new Color32[manifest.width * manifest.height];
                        for (int row = 0; row < manifest.height; row++)
                            Array.Copy(pixels, (y + row) * sheet.width + x, cut, row * manifest.width, manifest.width);
                        frame.SetPixels32(cut); frame.Apply();
                        string path = Destination + "/" + track.name + "/" + i.ToString("00") + ".png";
                        byte[] png = frame.EncodeToPNG();
                        if (File.Exists(path))
                        {
                            var existing = File.ReadAllBytes(path);
                            if (!existing.SequenceEqual(png) && (track.replacesFrameHashes == null ||
                                track.replacesFrameHashes.Length != manifest.frames || Hash(existing) != track.replacesFrameHashes[i]))
                                throw new InvalidDataException("Preserve differing existing art: " + path);
                        }
                        pending.Add(path, png);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(sheet); UnityEngine.Object.DestroyImmediate(frame); }
            }
            foreach (var pair in pending)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pair.Key));
                if (!File.Exists(pair.Key) || !File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value))
                    File.WriteAllBytes(pair.Key, pair.Value);
            }
            AssetDatabase.Refresh();
            foreach (string path in pending.Keys)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                DirectionalAnimationImporter.ApplyPixelSettings(importer,
                    new Vector2(manifest.pivotX / (float)manifest.width, manifest.pivotY / (float)manifest.height));
                importer.SaveAndReimport();
            }
            var art = AssetDatabase.LoadAssetAtPath<RecallReleaseArt>(ArtPath);
            if (art == null) { art = ScriptableObject.CreateInstance<RecallReleaseArt>(); AssetDatabase.CreateAsset(art, ArtPath); }
            Sprite[] Frames(string name) => Enumerable.Range(0, manifest.frames)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(Destination + "/" + name + "/" + i.ToString("00") + ".png")).ToArray();
            art.floor = Frames("Floor"); art.backRail = Frames("BackRail"); art.frontRail = Frames("FrontRail");
            art.hinge = Frames("Hinge"); art.release = Frames("Release");
            EditorUtility.SetDirty(art); AssetDatabase.SaveAssetIfDirty(art);
            return "Hayfields: 35 native frames imported at 16 PPU with a fixed hinge; source hashes and hard alpha verified. Scene unchanged.";
        }
    }
}
