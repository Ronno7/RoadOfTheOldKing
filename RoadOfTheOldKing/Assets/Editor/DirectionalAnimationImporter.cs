using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using RoadOfTheOldKing.Player;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    // Turns PixelLab exports into DirectionalSpriteAnimation slots of an animation-set asset.
    // Each folder under ArtRoot is one slot (matched by field name, ignoring case, spaces, hyphens
    // and underscores) and holds <direction>.png single frames or <direction>/ folders of numbered frames.
    public sealed class DirectionalAnimationImporter : AssetPostprocessor
    {
        public const float PixelsPerUnit = 16f;

        public sealed class Target
        {
            public string Name, ArtRoot, AnimationRoot, SetPath;
            public Type SetType;
            // Anchor every clip to the standing rotation's ground row (offset for canvas padding) instead of
            // each clip's lowest pixel. PixelLab draws a character at one canvas position across its clips, so
            // attacks that reach below the feet (a bite toward the camera) don't shift the whole body.
            public bool GroundFromRotations;
            // The same, for single slots of a target that otherwise measures each clip's lowest pixel
            // (e.g. a throw whose lunging foot steps below the standing feet).
            public HashSet<string> GroundFromRotationsSlots = new HashSet<string>();
            public Dictionary<string, string> Aliases = new Dictionary<string, string>();
            // Defaults for newly created animations; later edits to the asset are preserved.
            public Dictionary<string, (float fps, bool loop)> Defaults = new Dictionary<string, (float, bool)>();
        }

        public static readonly List<Target> Targets = new List<Target>();

        private void OnPreprocessTexture()
        {
            // New files only: the import command owns settings afterwards. Until it measures the
            // feet, a new frame stands on its canvas bottom.
            var importer = (TextureImporter)assetImporter;
            if (importer.importSettingsMissing && Targets.Any(t => assetPath.StartsWith(t.ArtRoot + "/")))
                ApplyPixelSettings(importer, new Vector2(0.5f, 0f));
        }

        public static string Import(Target target)
        {
            AssetDatabase.Refresh();
            var report = new StringBuilder(target.Name + " animations\n");
            var slots = target.SetType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(DirectionalSpriteAnimation)).ToList();
            EnsureFolder(target.AnimationRoot);
            var set = AssetDatabase.LoadAssetAtPath(target.SetPath, target.SetType);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance(target.SetType);
                AssetDatabase.CreateAsset(set, target.SetPath);
            }

            // Per-direction standing ground row and canvas height, from the rotations folder.
            var anchors = new (int ground, int height)?[DirectionalSpriteAnimation.DirectionCount];
            string rotationsFolder = AssetDatabase.GetSubFolders(target.ArtRoot)
                .FirstOrDefault(f => Normalize(Path.GetFileName(f)) == "rotations");
            if ((target.GroundFromRotations || target.GroundFromRotationsSlots.Count > 0) && rotationsFolder != null)
            {
                var rotationFrames = CollectFrames(rotationsFolder);
                for (int octant = 0; octant < anchors.Length; octant++)
                    if (rotationFrames[octant].Count > 0)
                        anchors[octant] = (MeasureGround(rotationFrames[octant], out int h, out _, out _), h);
            }

            var filled = new HashSet<FieldInfo>();
            foreach (string folder in AssetDatabase.GetSubFolders(target.ArtRoot))
            {
                string folderName = Path.GetFileName(folder);
                var slot = FindSlot(target, slots, folderName);
                if (slot == null)
                {
                    report.AppendLine($"  Skipped {folderName}: no matching slot");
                    continue;
                }
                var frames = CollectFrames(folder);
                if (frames.All(f => f.Count == 0))
                {
                    report.AppendLine($"  Skipped {folderName}: no direction images");
                    continue;
                }

                string clipPath = $"{target.AnimationRoot}/{char.ToUpperInvariant(slot.Name[0])}{slot.Name.Substring(1)}.asset";
                var clip = AssetDatabase.LoadAssetAtPath<DirectionalSpriteAnimation>(clipPath);
                if (clip == null)
                {
                    clip = ScriptableObject.CreateInstance<DirectionalSpriteAnimation>();
                    var defaults = target.Defaults.TryGetValue(slot.Name, out var d) ? d : (12f, false);
                    clip.framesPerSecond = defaults.Item1;
                    clip.loop = defaults.Item2;
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                var line = new StringBuilder($"  {slot.Name}:");
                for (int octant = 0; octant < DirectionalSpriteAnimation.DirectionCount; octant++)
                {
                    var paths = frames[octant];
                    if (paths.Count == 0)
                    {
                        clip.SetFrames(octant, null);
                        continue;
                    }
                    // Feet stay on the ground line: the lowest opaque row across the direction's frames.
                    int ground = MeasureGround(paths, out int height, out string warning, out int[] heights);
                    if (anchors[octant].HasValue && (target.GroundFromRotations || target.GroundFromRotationsSlots.Contains(slot.Name)))
                    {
                        var anchor = anchors[octant].Value;
                        int anchored = anchor.ground + (height - anchor.height) / 2;
                        for (int k = 0; k < heights.Length; k++) heights[k] += ground - anchored;
                        ground = anchored;
                    }
                    foreach (string path in paths)
                        SetPivot(path, new Vector2(0.5f, ground / (float)height));
                    clip.SetFrames(octant, paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray(), heights);
                    line.Append($" {DirectionalSpriteAnimation.DirectionNames[octant]} {paths.Count}f ground {ground}{warning};");
                }
                EditorUtility.SetDirty(clip);
                slot.SetValue(set, clip);
                filled.Add(slot);
                report.AppendLine(line.ToString());
            }

            foreach (var slot in slots.Where(s => !filled.Contains(s)))
                slot.SetValue(set, null); // The set mirrors the art folders; clip assets are kept.
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static FieldInfo FindSlot(Target target, List<FieldInfo> slots, string folderName)
        {
            string key = Normalize(folderName);
            if (target.Aliases.TryGetValue(key, out string alias)) key = alias;
            return slots.FirstOrDefault(s => Normalize(s.Name) == key);
        }

        private static string Normalize(string name) => Regex.Replace(name.ToLowerInvariant(), "[-_ ]", "");

        private static int ParseDirection(string name)
        {
            string key = Normalize(name);
            for (int i = 0; i < DirectionalSpriteAnimation.DirectionNames.Length; i++)
                if (Normalize(DirectionalSpriteAnimation.DirectionNames[i]) == key)
                    return i;
            return -1;
        }

        private static List<string>[] CollectFrames(string folder)
        {
            var result = Enumerable.Range(0, DirectionalSpriteAnimation.DirectionCount).Select(_ => new List<string>()).ToArray();
            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                int octant = ParseDirection(Path.GetFileNameWithoutExtension(file));
                if (octant >= 0) result[octant].Add(file.Replace('\\', '/'));
            }
            foreach (string sub in AssetDatabase.GetSubFolders(folder))
            {
                int octant = ParseDirection(Path.GetFileName(sub));
                if (octant < 0) continue;
                // Natural order, so frame_10 follows frame_9.
                result[octant].AddRange(Directory.GetFiles(sub, "*.png").Select(f => f.Replace('\\', '/'))
                    .OrderBy(f => Regex.Replace(Path.GetFileNameWithoutExtension(f), @"\d+", m => m.Value.PadLeft(8, '0'))));
            }
            return result;
        }

        // Also returns each frame's figure height above that ground, which lets carried items bob with the body.
        private static int MeasureGround(List<string> paths, out int height, out string warning, out int[] heights)
        {
            int ground = int.MaxValue;
            height = 0;
            warning = "";
            var tops = new int[paths.Count];
            var texture = new Texture2D(2, 2);
            try
            {
                for (int k = 0; k < paths.Count; k++)
                {
                    texture.LoadImage(File.ReadAllBytes(paths[k]));
                    if (height != 0 && texture.height != height) warning = " (mixed canvas sizes)";
                    height = texture.height;
                    var pixels = texture.GetPixels32();
                    // LoadImage rows run bottom-up: the first opaque pixel is on the lowest row, the last on the highest.
                    for (int i = 0; i < pixels.Length; i++)
                        if (pixels[i].a > 0) { ground = Mathf.Min(ground, i / texture.width); break; }
                    for (int i = pixels.Length - 1; i >= 0; i--)
                        if (pixels[i].a > 0) { tops[k] = i / texture.width; break; }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            if (ground == int.MaxValue) ground = 0;
            heights = tops.Select(top => top - ground + 1).ToArray();
            return ground;
        }

        private static void SetPivot(string path, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteAlignment == (int)SpriteAlignment.Custom && settings.spritePivot == pivot &&
                importer.spritePixelsPerUnit == PixelsPerUnit)
                return;
            ApplyPixelSettings(importer, pivot);
            importer.SaveAndReimport();
        }

        private static void ApplyPixelSettings(TextureImporter importer, Vector2? pivot)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            if (pivot.HasValue)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot.Value;
            }
            importer.SetTextureSettings(settings);
        }
    }
}
