using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    public static class LowlandsArchitectureKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Environment/Architecture";

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Green Lowlands/Refresh Building Art")]
        public static void Build()
        {
            ImportKit("ArchitectureManifest.json", "LowlandsArchitecture16.png");
            BuildWaterworksInterior();
        }

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Green Lowlands/Refresh Waterworks Art")]
        public static void BuildWaterworksInterior()
        {
            ImportKit("WaterworksInteriorManifest.json", "WaterworksInterior16.png");
        }

        private static void ImportKit(string manifest, string atlas)
        {
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/" + manifest)).entries;
            var sprites = ImportSprites(Root + "/" + atlas, entries);
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
