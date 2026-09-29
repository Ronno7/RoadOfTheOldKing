using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TheLostShrine.Player;
using UnityEditor;
using UnityEngine;

namespace TheLostShrine.EditorTools
{
    // PixelLab exports become player animations. Each folder under ArtRoot is one slot of
    // PlayerAnimationSet (matched by name, ignoring case, spaces, hyphens and underscores) and
    // holds either <direction>.png single frames or <direction>/ folders of numbered frames.
    public sealed class PlayerAnimationImporter : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/Art/Sprites/Player";
        public const string AnimationRoot = "Assets/Animations/Player";
        public const string SetPath = AnimationRoot + "/PlayerAnimationSet.asset";
        private const float PixelsPerUnit = 16f;

        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "catch", "catching" }, { "sprint", "run" }, { "throw", "throwrelease" }, { "dodge", "dash" },
            { "walking", "walk" }, { "running", "run" }
        };

        // Defaults for newly created animations; later edits to the asset are preserved.
        private static readonly Dictionary<string, (float fps, bool loop)> Defaults = new Dictionary<string, (float, bool)>
        {
            { "rotations", (1f, true) }, { "idle", (6f, true) }, { "combatIdle", (8f, true) },
            { "walk", (10f, true) }, { "run", (12f, true) }, { "dash", (12f, false) },
            { "charge", (8f, true) }, { "throwAim", (8f, true) }, { "rest", (6f, true) },
            { "hurt", (10f, false) }, { "death", (8f, false) }, { "getUp", (8f, false) }
        };

        private void OnPreprocessTexture()
        {
            // New files only: the menu command owns settings afterwards. Until it measures the
            // feet, a new frame stands on its canvas bottom.
            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(ArtRoot + "/") && importer.importSettingsMissing)
                ApplyPixelSettings(importer, new Vector2(0.5f, 0f));
        }

        [MenuItem("Road of the Old King/Art/Import Player Animations")]
        public static void RebuildFromMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            AssetDatabase.Refresh();
            var report = new StringBuilder("Player animations\n");
            var slots = typeof(PlayerAnimationSet).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(DirectionalSpriteAnimation)).ToList();
            if (!AssetDatabase.IsValidFolder(AnimationRoot))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(AnimationRoot).Replace('\\', '/'), Path.GetFileName(AnimationRoot));
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<PlayerAnimationSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }

            var filled = new HashSet<FieldInfo>();
            foreach (string folder in AssetDatabase.GetSubFolders(ArtRoot))
            {
                string folderName = Path.GetFileName(folder);
                var slot = FindSlot(slots, folderName);
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

                string clipPath = $"{AnimationRoot}/{char.ToUpperInvariant(slot.Name[0])}{slot.Name.Substring(1)}.asset";
                var clip = AssetDatabase.LoadAssetAtPath<DirectionalSpriteAnimation>(clipPath);
                if (clip == null)
                {
                    clip = ScriptableObject.CreateInstance<DirectionalSpriteAnimation>();
                    var defaults = Defaults.TryGetValue(slot.Name, out var d) ? d : (12f, false);
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
                    int ground = MeasureGround(paths, out int height, out string warning);
                    foreach (string path in paths)
                        SetPivot(path, new Vector2(0.5f, ground / (float)height));
                    clip.SetFrames(octant, paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray());
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

        private static FieldInfo FindSlot(List<FieldInfo> slots, string folderName)
        {
            string key = Normalize(folderName);
            if (Aliases.TryGetValue(key, out string alias)) key = alias;
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

        private static int MeasureGround(List<string> paths, out int height, out string warning)
        {
            int ground = int.MaxValue;
            height = 0;
            warning = "";
            var texture = new Texture2D(2, 2);
            try
            {
                foreach (string path in paths)
                {
                    texture.LoadImage(File.ReadAllBytes(path));
                    if (height != 0 && texture.height != height) warning = " (mixed canvas sizes)";
                    height = texture.height;
                    var pixels = texture.GetPixels32();
                    // LoadImage rows run bottom-up, so the first opaque row found is the lowest.
                    for (int i = 0; i < pixels.Length; i++)
                        if (pixels[i].a > 0) { ground = Mathf.Min(ground, i / texture.width); break; }
                }
            }
            finally { Object.DestroyImmediate(texture); }
            return ground == int.MaxValue ? 0 : ground;
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
