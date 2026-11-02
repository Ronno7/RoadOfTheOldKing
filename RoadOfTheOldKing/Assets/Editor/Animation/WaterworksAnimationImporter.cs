using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    public static class WaterworksAnimationImporter
    {
        public const string Destination = "Assets/Art/World/Waterworks/Drains";
        public const string GateDestination = "Assets/Art/World/Waterworks/Gates";
        public const string CanalDestination = "Assets/Art/World/Waterworks/Canal/v1";
        public const string CulvertDestination = "Assets/Art/World/Waterworks/Culvert/v1";

        [Serializable] private sealed class Manifest
        {
            public int version, pixelsPerUnit;
            public bool approved;
            public bool reviewed;
            public string sourceDirectory;
            public Part[] parts;
        }
        [Serializable] private sealed class Part
        {
            public string name, file, sha256;
            public int width, height;
            public float pivotX, pivotY;
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
                "../../ArtSource/GreenLowlands/Environment/Waterworks"));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source, "ProductionManifest.json")));
            string[] expected = { "DrainEDry", "DrainEWet", "DrainOutletWet", "DrainWDry", "DrainWWet" };
            if (manifest.version != 1 || !manifest.approved || manifest.pixelsPerUnit != 16 ||
                manifest.sourceDirectory != "Source" || manifest.parts == null ||
                !manifest.parts.Select(p => p.name).OrderBy(n => n).SequenceEqual(expected))
                throw new InvalidDataException("Unexpected or unapproved waterworks manifest.");

            var pending = new Dictionary<string, byte[]>();
            var textures = new Dictionary<string, Texture2D>();
            try
            {
                foreach (var part in manifest.parts)
                {
                    int width = part.name == "DrainOutletWet" ? 24 : 16;
                    if (part.file != part.name + ".png" || part.width != width || part.height != 24)
                        throw new InvalidDataException("Unexpected drain dimensions or source filename.");
                    byte[] bytes = File.ReadAllBytes(Path.Combine(source, manifest.sourceDirectory, part.file));
                    if (Hash(bytes) != part.sha256) throw new InvalidDataException("Source hash differs: " + part.file);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    textures.Add(part.name, texture);
                    if (!texture.LoadImage(bytes) || texture.width != width || texture.height != 24 ||
                        texture.GetPixels32().Any(p => p.a != 0 && p.a != 255))
                        throw new InvalidDataException("Invalid dimensions or soft alpha: " + part.file);
                    pending.Add(part.name, bytes);
                }
                foreach (string direction in new[] { "E", "W" })
                {
                    var wet = textures["Drain" + direction + "Wet"].GetPixels32();
                    var dry = textures["Drain" + direction + "Dry"].GetPixels32();
                    var pixels = new Color32[wet.Length];
                    for (int i = 0; i < wet.Length; i++)
                    {
                        if (wet[i].a != dry[i].a) throw new InvalidDataException("Wet/dry silhouettes differ.");
                        pixels[i] = wet[i].Equals(dry[i]) ? new Color32(0, 0, 0, 0) : wet[i];
                    }
                    var overlay = new Texture2D(16, 24, TextureFormat.RGBA32, false);
                    try
                    {
                        overlay.SetPixels32(pixels); overlay.Apply();
                        pending.Add("Drain" + direction + "Water", overlay.EncodeToPNG());
                    }
                    finally { UnityEngine.Object.DestroyImmediate(overlay); }
                }
            }
            finally { foreach (var texture in textures.Values) UnityEngine.Object.DestroyImmediate(texture); }

            WriteSprites(Destination, pending, name => Vector2.one * .5f);
            return "Waterworks drains: five approved sprites and two water-only layers imported at 16 PPU. Scene unchanged.";
        }

        public static string ImportGates()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Import in stopped Edit Mode.");
            string source = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/GreenLowlands/Environment/Waterworks"));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source, "GateProductionManifest.json")));
            var sizes = new Dictionary<string, Vector2Int>
            {
                { "GateGuide", new Vector2Int(6, 20) }, { "GateLeaf", new Vector2Int(50, 20) },
                { "SluiceApron", new Vector2Int(48, 40) }, { "SluiceBlade", new Vector2Int(48, 48) },
                { "SluiceFlow", new Vector2Int(48, 64) }, { "SluiceFrame", new Vector2Int(64, 64) }
            };
            if (manifest.version != 1 || !manifest.reviewed || manifest.pixelsPerUnit != 16 ||
                manifest.sourceDirectory != "Source" || manifest.parts == null ||
                !manifest.parts.Select(p => p.name).OrderBy(n => n).SequenceEqual(sizes.Keys.OrderBy(n => n)))
                throw new InvalidDataException("Unexpected or unreviewed gate manifest.");
            var pending = new Dictionary<string, byte[]>();
            var pivots = new Dictionary<string, Vector2>();
            foreach (var part in manifest.parts)
            {
                var size = sizes[part.name];
                float pivotY = part.name.StartsWith("Gate") ? 8 : size.y / 2f;
                if (part.file != part.name + ".png" || part.width != size.x || part.height != size.y ||
                    part.pivotX != size.x / 2f || part.pivotY != pivotY)
                    throw new InvalidDataException("Unexpected gate dimensions or pivot: " + part.name);
                byte[] bytes = File.ReadAllBytes(Path.Combine(source, manifest.sourceDirectory, part.file));
                if (Hash(bytes) != part.sha256) throw new InvalidDataException("Source hash differs: " + part.file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(bytes) || texture.width != size.x || texture.height != size.y ||
                        texture.GetPixels32().Any(p => p.a != 0 && p.a != 255))
                        throw new InvalidDataException("Invalid gate pixels: " + part.file);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
                pending.Add(part.name, bytes);
                pivots.Add(part.name, new Vector2(part.pivotX / size.x, part.pivotY / size.y));
            }
            WriteSprites(GateDestination, pending, name => pivots[name]);
            return "Waterworks gates: six reviewed parts imported at 16 PPU with fixed pivots. Scene unchanged.";
        }

        public static string ImportCanal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Import in stopped Edit Mode.");
            string source = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/GreenLowlands/Environment/Waterworks"));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source, "CanalProductionManifest.json")));
            var sizes = new Dictionary<string, Vector2Int>
            {
                { "CanalBed", new Vector2Int(48, 64) }, { "CanalBedTurn", new Vector2Int(48, 48) },
                { "CanalBedWest", new Vector2Int(64, 32) }, { "SpillwayFlow", new Vector2Int(192, 1216) }
            };
            if (manifest.version != 1 || !manifest.approved || manifest.pixelsPerUnit != 16 ||
                manifest.sourceDirectory != "Source" || manifest.parts == null ||
                !manifest.parts.Select(p => p.name).OrderBy(n => n).SequenceEqual(sizes.Keys.OrderBy(n => n)))
                throw new InvalidDataException("Unexpected or unapproved canal manifest.");
            var pending = new Dictionary<string, byte[]>();
            foreach (var part in manifest.parts)
            {
                var size = sizes[part.name];
                if (part.file != part.name + ".png" || part.width != size.x || part.height != size.y)
                    throw new InvalidDataException("Unexpected canal source: " + part.name);
                byte[] bytes = File.ReadAllBytes(Path.Combine(source, manifest.sourceDirectory, part.file));
                if (Hash(bytes) != part.sha256) throw new InvalidDataException("Source hash differs: " + part.file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(bytes) || texture.width != size.x || texture.height != size.y ||
                        texture.GetPixels32().Any(p => p.a != 0 && p.a != 255))
                        throw new InvalidDataException("Invalid canal pixels: " + part.file);
                    if (part.name != "SpillwayFlow") pending.Add(part.name, bytes);
                    else for (int width = 0; width < 13; width++)
                        for (int phase = 0; phase < 4; phase++)
                        {
                            // Approved sheet rows 6..18; top 16 pixels form seamless one-unit reaches.
                            var frame = new Texture2D(48, 16, TextureFormat.RGBA32, false);
                            try
                            {
                                frame.SetPixels(texture.GetPixels(phase * 48, (12 - width) * 64 + 48, 48, 16));
                                frame.Apply();
                                pending.Add("CanalFlow_" + width.ToString("00") + "_" + phase, frame.EncodeToPNG());
                            }
                            finally { UnityEngine.Object.DestroyImmediate(frame); }
                        }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            WriteSprites(CanalDestination, pending, name => Vector2.one * .5f);
            return "Waterworks canal: three approved bed parts and 52 flow frames imported at 16 PPU. Scene unchanged.";
        }

        public static string ImportCulvert()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Import in stopped Edit Mode.");
            string source = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/GreenLowlands/Environment/Waterworks"));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source, "CulvertProductionManifest.json")));
            string[] expected = { "CulvertDepthBars", "CulvertDepthBed", "CulvertDepthWater" };
            if (manifest.version != 1 || !manifest.approved || manifest.pixelsPerUnit != 16 ||
                manifest.sourceDirectory != "Source" || manifest.parts == null ||
                !manifest.parts.Select(p => p.name).OrderBy(n => n).SequenceEqual(expected))
                throw new InvalidDataException("Unexpected or unapproved culvert manifest.");
            var pending = new Dictionary<string, byte[]>();
            foreach (var part in manifest.parts)
            {
                int width = part.name == "CulvertDepthWater" ? 384 : 96;
                if (part.file != part.name + ".png" || part.width != width || part.height != 52 ||
                    part.pivotX != 48 || part.pivotY != 26)
                    throw new InvalidDataException("Unexpected culvert source or frame pivot: " + part.name);
                byte[] bytes = File.ReadAllBytes(Path.Combine(source, manifest.sourceDirectory, part.file));
                if (Hash(bytes) != part.sha256) throw new InvalidDataException("Source hash differs: " + part.file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(bytes) || texture.width != width || texture.height != 52 ||
                        texture.GetPixels32().Any(p => p.a != 0 && p.a != 255))
                        throw new InvalidDataException("Invalid culvert pixels: " + part.file);
                    if (part.name != "CulvertDepthWater") pending.Add(part.name, bytes);
                    else for (int level = 0; level < 10; level++)
                        for (int phase = 0; phase < 4; phase++)
                        {
                            var frame = new Texture2D(96, 52, TextureFormat.RGBA32, false);
                            try
                            {
                                var pixels = texture.GetPixels(phase * 96, 0, 96, 52);
                                // Crop the reviewed waterline downward; retain the fixed frame and bed pivot.
                                for (int y = 22 - level; y < 52; y++)
                                    for (int x = 0; x < 96; x++) pixels[y * 96 + x] = Color.clear;
                                frame.SetPixels(pixels); frame.Apply();
                                pending.Add("CulvertFlow_" + level.ToString("00") + "_" + phase, frame.EncodeToPNG());
                            }
                            finally { UnityEngine.Object.DestroyImmediate(frame); }
                        }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            WriteSprites(CulvertDestination, pending, name => Vector2.one * .5f);
            return "Waterworks culvert: two approved masonry/iron layers and 40 water frames imported at 16 PPU. Scene unchanged.";
        }

        private static void WriteSprites(string destination, Dictionary<string, byte[]> pending, Func<string, Vector2> pivot)
        {
            // Check every destination before writing; accepted or hand-edited art is never replaced.
            foreach (var pair in pending)
            {
                string path = destination + "/" + pair.Key + ".png";
                if (File.Exists(path) && !File.ReadAllBytes(path).SequenceEqual(pair.Value))
                    throw new InvalidDataException("Preserve differing existing art: " + path);
            }
            Directory.CreateDirectory(destination);
            foreach (var pair in pending)
            {
                string path = destination + "/" + pair.Key + ".png";
                if (!File.Exists(path)) File.WriteAllBytes(path, pair.Value);
            }
            AssetDatabase.Refresh();
            foreach (string name in pending.Keys)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(destination + "/" + name + ".png");
                DirectionalAnimationImporter.ApplyPixelSettings(importer, pivot(name));
                importer.SaveAndReimport();
            }
        }
    }
}
