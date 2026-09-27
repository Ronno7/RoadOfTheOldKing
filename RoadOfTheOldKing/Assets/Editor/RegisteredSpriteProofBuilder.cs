using System;
using System.IO;
using System.Linq;
using TheLostShrine.Player;
using TheLostShrine.Weapons;
using UnityEditor;
using UnityEngine;

namespace TheLostShrine.Editor
{
    public static class RegisteredSpriteProofBuilder
    {
        public const string ThrowFolder = "Assets/Art/Sprites/Player/Actions/Throw";
        public const string ThrowAssetPath = "Assets/Animations/Player/Combat/Throw.asset";
        public const string ForehandFolder = "Assets/Art/Sprites/Player/Actions/Forehand";
        public const string ForehandAssetPath = "Assets/Animations/Player/Combat/Forehand.asset";
        public const string CatchFolder = "Assets/Art/Sprites/Player/Actions/Catch";
        public const string CatchAssetPath = "Assets/Animations/Player/Combat/Catch.asset";
        public const string SpinFolder = "Assets/Art/Sprites/Weapons/Spin";
        public const string SpinAssetPath = "Assets/Animations/Weapons/HatchetSpin.asset";
        public const string DashFolder = "Assets/Art/Sprites/Player/Actions/Dash";
        public const string DashAssetPath = "Assets/Animations/Player/Locomotion/Dash.asset";

        public const string SprintFolder = "Assets/Art/Sprites/Player/Actions/Sprint";
        public const string SprintAssetPath = "Assets/Animations/Player/Locomotion/Sprint.asset";

        [Serializable]
        private sealed class ForehandManifest
        {
            public int canvas, pixelsPerUnit, columns, contactCel, recoveryCel, possessionCel;
            public float[] exposures;
        }
        [Serializable]
        private sealed class SpinManifest
        {
            public int canvas, pixelsPerUnit, columns, celCount;
            public float rotationsPerSecond;
        }
        [MenuItem("Road of the Old King/Animation/Import Registered Animations")]
        public static void BuildAll() { BuildThrow(); BuildForehand(); BuildCatch(); BuildSpin(); BuildCarry(); BuildDash(); BuildSprint(); }

        [MenuItem("Road of the Old King/Animation/Import Registered Dash")]
        public static void BuildDash()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Import in Edit Mode.");
            var manifest = JsonUtility.FromJson<ForehandManifest>(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/Player/Registered/Dash/dash.json"))));
            if (manifest.canvas != 640 || manifest.pixelsPerUnit != 128 || manifest.columns != 3 || manifest.exposures.Length != 5)
                throw new InvalidOperationException("Dash export requires five registered cels.");
            var asset = AssetDatabase.LoadAssetAtPath<RegisteredActionSprites>(DashAssetPath);
            if (asset == null) { asset = ScriptableObject.CreateInstance<RegisteredActionSprites>(); AssetDatabase.CreateAsset(asset, DashAssetPath); }
            asset.views = new[] {
                MakeDashView("East", Vector2.right), MakeDashView("North", Vector2.up),
                MakeDashView("South", Vector2.down), MakeDashView("West", Vector2.left)
            };
            asset.celCount = 5; asset.exposures = manifest.exposures;
            asset.contactCel = asset.recoveryCel = asset.possessionCel = -1;
            if (!asset.IsRegistered(out var error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log("Dash: all four cardinal views, three travel poses and two interruptible recovery poses per view.");
        }

        private static RegisteredActionSprites.View MakeDashView(string name, Vector2 direction)
        {
            return new RegisteredActionSprites.View { name = name, direction = direction,
                body = ImportSheet(DashFolder+"/Dash-"+name+"-Body.png", name+"_DashBody", 5, 3, 640, 128, RegisteredActionSprites.GroundPivot),
                weapon = ImportSheet(DashFolder+"/Dash-"+name+"-Weapon.png", name+"_DashWeapon", 5, 3, 640, 128, RegisteredActionSprites.GroundPivot) };
        }

        [MenuItem("Road of the Old King/Animation/Import Registered Sprint")]
        public static void BuildSprint()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Import in Edit Mode.");
            var asset = AssetDatabase.LoadAssetAtPath<RegisteredActionSprites>(SprintAssetPath);
            if (asset == null) { asset = ScriptableObject.CreateInstance<RegisteredActionSprites>(); AssetDatabase.CreateAsset(asset, SprintAssetPath); }
            var names = new[] { "East", "North", "South", "West" };
            var directions = new[] { Vector2.right, Vector2.up, Vector2.down, Vector2.left };
            asset.views = names.Select((name, index) => new RegisteredActionSprites.View {
                name = name, direction = directions[index],
                body = ImportSheet(SprintFolder+"/Sprint-"+name+"-Body.png", name+"_SprintBody", 8, 4, 640, 128, RegisteredActionSprites.GroundPivot),
                unarmedBody = name == "East" || name == "West" || name == "South" ? ImportSheet(SprintFolder+"/Sprint-"+name+"-Unarmed.png", name+"_SprintUnarmed", 8, 4, 640, 128, RegisteredActionSprites.GroundPivot) : Array.Empty<Sprite>(),
                weapon = ImportSheet(SprintFolder+"/Sprint-"+name+"-Weapon.png", name+"_SprintWeapon", 8, 4, 640, 128, RegisteredActionSprites.GroundPivot)
            }).ToArray();
            asset.celCount = 8; asset.exposures = Array.Empty<float>();
            asset.contactCel = asset.recoveryCel = asset.possessionCel = -1;
            if (!asset.IsRegistered(out var error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log("Sprint: eight cels per cardinal view, matched layers, locomotion-owned distance phase.");
        }

        public static void BuildCarry()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Import in Edit Mode.");
            const string path = "Assets/Art/Sprites/Weapons/Hatchet.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 256;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(140f/256, 55f/256);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Sprites/Weapons/HatchetTurns.png");
            importer.spritePixelsPerUnit = 128;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 512;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
#pragma warning disable 618
            // Retain existing names/IDs so the north combo keeps its perspective views.
            var turns = importer.spritesheet;
            if (turns.Length != 2) throw new InvalidOperationException("Expected two held axe perspectives.");
            for (int i = 0; i < turns.Length; i++)
            {
                turns[i].rect = new Rect(i*256, 0, 256, 256);
                turns[i].alignment = (int)SpriteAlignment.Custom;
                turns[i].pivot = new Vector2(140f/256, 55f/256);
            }
            importer.spritesheet = turns;
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        [MenuItem("Road of the Old King/Animation/Import Registered Throw")]
        public static void BuildThrow() => Build("Throw", ThrowAssetPath, 8, 4, Array.Empty<float>(), -1, -1);

        [MenuItem("Road of the Old King/Animation/Import Registered Forehand")]
        public static void BuildForehand()
        {
            string manifestPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/Player/Registered/Combat/forehand-layers.json"));
            var manifest = JsonUtility.FromJson<ForehandManifest>(File.ReadAllText(manifestPath));
            if (manifest.canvas != RegisteredActionSprites.CanvasSize ||
                manifest.pixelsPerUnit != RegisteredActionSprites.PixelsPerUnit || manifest.exposures == null)
                throw new InvalidOperationException("Forehand export does not match the registered action specification.");
            Build("Forehand", ForehandAssetPath, manifest.exposures.Length, manifest.columns,
                manifest.exposures, manifest.contactCel, manifest.recoveryCel);
        }

        public static void BuildCatch()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/Player/Registered/Return/catch-layers.json"));
            var manifest = JsonUtility.FromJson<ForehandManifest>(File.ReadAllText(path));
            if (manifest.canvas != RegisteredActionSprites.CanvasSize ||
                manifest.pixelsPerUnit != RegisteredActionSprites.PixelsPerUnit || manifest.exposures == null)
                throw new InvalidOperationException("Catch export does not match the registered action specification.");
            Build("Catch", CatchAssetPath, manifest.exposures.Length, manifest.columns,
                manifest.exposures, -1, -1, manifest.possessionCel);
        }

        public static void BuildSpin()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Import in Edit Mode.");
            string path = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/Player/Registered/Return/spin.json"));
            var manifest = JsonUtility.FromJson<SpinManifest>(File.ReadAllText(path));
            if (manifest.canvas != AxeSpinSprites.CanvasSize || manifest.pixelsPerUnit != AxeSpinSprites.PixelsPerUnit || manifest.celCount != 8)
                throw new InvalidOperationException("Invalid spin export.");
            var cels = ImportSheet(SpinFolder+"/Axe-Spin.png", "Spin", 8, manifest.columns,
                AxeSpinSprites.CanvasSize, AxeSpinSprites.PixelsPerUnit, Vector2.one*.5f);
            var asset = AssetDatabase.LoadAssetAtPath<AxeSpinSprites>(SpinAssetPath);
            if (asset == null) { asset = ScriptableObject.CreateInstance<AxeSpinSprites>(); AssetDatabase.CreateAsset(asset, SpinAssetPath); }
            asset.cels = cels; asset.rotationsPerSecond = manifest.rotationsPerSecond;
            if (!asset.IsRegistered(out var error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log("Spin proof: eight drawn cels, " + asset.rotationsPerSecond + " rotations per second.");
        }

        private static void Build(string family, string path, int count, int columns, float[] exposures, int contact, int recovery, int possession = -1)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Import in Edit Mode.");
            var views = new System.Collections.Generic.List<RegisteredActionSprites.View> {
                MakeView(family, "East", Vector2.right, count, columns),
                MakeView(family, "North", Vector2.up, count, columns)
            };
            views.Add(MakeView(family, "South", Vector2.down, count, columns));
            if (family == "Forehand" || family == "Throw" || family == "Catch") views.Add(MakeView(family, "West", Vector2.left, count, columns));
            var asset = AssetDatabase.LoadAssetAtPath<RegisteredActionSprites>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<RegisteredActionSprites>(); AssetDatabase.CreateAsset(asset, path); }
            asset.celCount = count; asset.exposures = exposures;
            asset.contactCel = contact; asset.recoveryCel = recovery; asset.views = views.ToArray();
            asset.possessionCel = possession;
            if (!asset.IsRegistered(out var error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log(family + " proof: " + count*views.Count + " cels, matched body/weapon layers at 128 PPU.");
        }

        private static RegisteredActionSprites.View MakeView(string family, string name, Vector2 direction, int count, int columns)
        {
            return new RegisteredActionSprites.View { name = name, direction = direction,
                // South release: axe balance (516,886), foot anchor (644,922), source density 160.
                // West release: axe balance (162,433), foot anchor (379,545), source density 168.
                freePropOffset = family == "Throw" ? (name == "West" ? new Vector2(-217,112)/168f
                    : name == "South" ? new Vector2(-128,36)/160f
                    : name == "East" ? new Vector2(224,157)/128f : new Vector2(71,266)/128f)
                    // South catch redraw: axe balance (183,293), foot anchor (340,482), density 184.
                    // West catch: registered balance (207,398), foot origin (320,544), 128 PPU.
                    : family == "Catch" ? (name == "West" ? new Vector2(-113,146)/128f
                    : name == "South" ? new Vector2(-157,189)/184f
                    : name == "East" ? new Vector2(242,222)/178f : new Vector2(114,317)/176f) : Vector2.zero,
                freePropScale = family == "Catch" && name == "South" ? 1.05f
                    : family == "Catch" && name == "East" ? .94f
                    : name == "South" || name == "West" ? .8f : name == "East" ? 1f : .75f,
                spinStartCel = name == "North" ? 0 : 1,
                spinFlipX = name == "South" || name == "West",
                body = Import(family, name, "Body", count, columns),
                weapon = Import(family, name, "Weapon", count, columns) };
        }

        private static Sprite[] Import(string family, string direction, string layer, int count, int columns)
        {
            string folder = family == "Throw" ? ThrowFolder : family == "Catch" ? CatchFolder : ForehandFolder;
            string path = folder + "/" + family + "-" + direction + "-" + layer + ".png";
            return ImportSheet(path, direction+"_"+layer, count, columns,
                RegisteredActionSprites.CanvasSize, RegisteredActionSprites.PixelsPerUnit, RegisteredActionSprites.GroundPivot);
        }

        private static Sprite[] ImportSheet(string path, string prefix, int count, int columns, int size, int ppu, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            var importSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importSettings);
            importSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(importSettings);
            int rows = Mathf.CeilToInt((float)count / columns);
            var rects = new SpriteMetaData[count];
            for (int i = 0; i < count; i++)
                rects[i] = new SpriteMetaData { name = prefix + "_" + i.ToString("D2"),
                    rect = new Rect(i % columns * size, (rows - 1 - i / columns) * size, size, size),
                    alignment = (int)SpriteAlignment.Custom, pivot = pivot };
#pragma warning disable 618
            importer.spritesheet = rects;
#pragma warning restore 618
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (sprites.Length != count || sprites[0].texture.width != columns*size || sprites[0].texture.height != rows*size)
                throw new InvalidOperationException("Incorrect dimensions or slices: " + path);
            return sprites;
        }
    }
}
