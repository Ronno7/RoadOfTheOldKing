using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    public static class LowlandsArchitectureKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Architecture";

        [MenuItem("Tools/Road of the Old King/Build Lowlands Architecture Kit")]
        public static void Build()
        {
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/ArchitectureManifest.json")).entries;
            var sprites = ImportSprites(Root + "/LowlandsArchitecture16.png", entries);
            Directory.CreateDirectory(Root + "/Tiles");
            AssetDatabase.Refresh();
            foreach (var entry in entries)
            {
                if (entry.width != 16 || entry.height != 16) continue;
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
