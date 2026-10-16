using System.IO;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    // Asset-only importer: authored scene placement remains the source of truth.
    public static class LowlandsThicketKitBuilder
    {
        [MenuItem("Tools/Road of the Old King/Vegetation/Build Lowlands Small Bush")]
        public static void BuildSmallBush()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            const string art = "Assets/Art/Sprites/World/LowlandsVegetation/";
            var bush = ImportSmallProp(art + "SmallSageBush.png", new Vector2(.5f, 2f/16f));
            var contact = ImportSmallProp(art + "SmallBushContact.png", new Vector2(.5f,.5f));
            var scratch = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Small Sage Bush");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scratch);
                root.layer = LayerMask.NameToLayer("Environment");
                var collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(.625f,.375f);
                collider.offset = new Vector2(0,.125f);
                var leaves = new GameObject("Leaves");
                leaves.transform.SetParent(root.transform, false);
                var renderer = leaves.AddComponent<SpriteRenderer>();
                renderer.sprite = bush;
                renderer.sortingLayerName = "World";
                var depth = leaves.AddComponent<RoadOfTheOldKing.World.WorldPropDepth>();
                var serialized = new SerializedObject(depth);
                serialized.FindProperty("groundAnchor").objectReferenceValue = root.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var shadow = new GameObject("Contact shadow");
                shadow.transform.SetParent(root.transform, false);
                var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = contact;
                shadowRenderer.sortingLayerName = "Ground";
                shadowRenderer.sortingOrder = 36;
                shadowRenderer.color = new Color(1,1,1,.32f);
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/World/SmallSageBush.prefab");
            }
            finally { EditorSceneManager.ClosePreviewScene(scratch); }
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

        private const string Root = "Assets/Art/Tiles/GreenLowlands/Thickets";

        [MenuItem("Tools/Road of the Old King/Vegetation/Refresh Retained Lowlands Bramble Tiles")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/ThicketManifest.json")).entries;
            if (entries.Length != 6 || entries.Any(e => !e.name.StartsWith("Bramble_B_")))
                throw new InvalidDataException("The current kit contains only the six retained Bramble B slices.");
            var sprites = ImportSprites(Root + "/LowlandsThickets16.png", entries);
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
