using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    // Imports accepted shared interaction art; preserves prefabs and scene wiring.
    public static class SharedInteractableKitBuilder
    {
        public const string Root = "Assets/Art/Sprites/World/Shared/Interactables";

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Interactive Art")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/InteractableManifest.json")).entries;
            ImportSprites(Root + "/Interactables16.png", entries);
            AssetDatabase.SaveAssets();
        }
    }
}
