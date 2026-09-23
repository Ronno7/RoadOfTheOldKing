using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace TheLostShrine.EditorTools
{
    // Shared Editor-only import/asset operations for native tile kits.
    internal static class TileKitAssets
    {
        internal static readonly Vector3Int[] Neighbors = {
            new Vector3Int(0,1,0), new Vector3Int(1,0,0), new Vector3Int(0,-1,0), new Vector3Int(-1,0,0),
            new Vector3Int(1,1,0), new Vector3Int(1,-1,0), new Vector3Int(-1,-1,0), new Vector3Int(-1,1,0)
        };
        [Serializable] internal sealed class Entry
        {
            public string name, material;
            public int mask, variant, index, x, y, width, height;
        }
        [Serializable] internal sealed class Manifest { public Entry[] entries; }

        internal static Dictionary<string, Sprite> ImportSprites(string path, Entry[] entries)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = entries.Select(e => new SpriteRect {
                name = e.name, rect = new Rect(e.x,e.y,e.width,e.height),
                alignment = SpriteAlignment.Center, pivot = Vector2.one * .5f,
                spriteID = previous.TryGetValue(e.name, out var guid) ? guid : GUID.Generate()
            }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rects.Select(r => new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        }

        internal static RuleTile.TilingRule MakeRule(Sprite[] sprites)
        {
            return new RuleTile.TilingRule {
                m_Sprites = sprites, m_Neighbors = new List<int>(), m_NeighborPositions = new List<Vector3Int>(),
                m_ColliderType = Tile.ColliderType.None,
                m_Output = sprites.Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Random : RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_PerlinScale = .43f, m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
                m_RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed
            };
        }

        internal static void SavePalette(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root,path);
            Object.DestroyImmediate(root);
            var settings = AssetDatabase.LoadAllAssetsAtPath(path).OfType<GridPalette>().FirstOrDefault();
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GridPalette>();
                settings.name = "Palette Settings";
                AssetDatabase.AddObjectToAsset(settings,path);
            }
            settings.cellSizing = GridPalette.CellSizing.Manual;
            EditorUtility.SetDirty(settings);
        }

        internal static Tilemap AddMap(Transform parent, string name, int order, string layer = "Ground")
        {
            var go = new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));
            go.transform.SetParent(parent,false);
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        internal static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path); }
            return asset;
        }
    }
}
