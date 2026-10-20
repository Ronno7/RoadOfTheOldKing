using System.IO;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    // Asset-only importer: authored scene placement remains the source of truth.
    public static class SharedThicketKitBuilder
    {
        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Bush Art")]
        public static void BuildSmallBush()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            const string art = "Assets/Art/Sprites/World/Shared/Vegetation/";
            ImportSmallProp(art + "SmallSageBush.png", new Vector2(.5f, 2f/16f));
            ImportSmallProp(art + "SmallBushContact.png", new Vector2(.5f,.5f));
            AssetDatabase.SaveAssets();
        }

        static Sprite ImportSmallProp(string path, Vector2 pivot)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private const string Root = "Assets/Art/Tiles/Shared/Environment/Thickets";

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Thicket Tiles")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/ThicketManifest.json")).entries;
            if (entries.Length != 6 || entries.Any(e => !e.name.StartsWith("Bramble_B_")))
                throw new InvalidDataException("The current kit contains only the six retained Bramble B slices.");
            var sprites = ImportSprites(Root + "/Thickets16.png", entries);
            Directory.CreateDirectory(Root + "/Tiles");
            AssetDatabase.Refresh();
            foreach (var entry in entries)
            {
                var tile = GetOrCreate<Tile>(Root + "/Tiles/" + entry.name + ".asset");
                tile.sprite = sprites[entry.name];
                tile.colliderType = Tile.ColliderType.None;
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                EditorUtility.SetDirty(tile);
            }
            AssetDatabase.SaveAssets();
        }

    }
}
