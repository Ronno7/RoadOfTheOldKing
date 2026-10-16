using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    // Asset-only: scene swaps are explicit, preserving shared Tutorial prefabs.
    public static class LowlandsInteractableKitBuilder
    {
        public const string Root = "Assets/Art/Sprites/World/LowlandsInteractables";

        [MenuItem("Tools/Road of the Old King/Art/Build Lowlands Interactable Kit")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/InteractableManifest.json")).entries;
            ImportSprites(Root + "/LowlandsInteractables16.png", entries);
            AssetDatabase.SaveAssets();
        }
    }
}
