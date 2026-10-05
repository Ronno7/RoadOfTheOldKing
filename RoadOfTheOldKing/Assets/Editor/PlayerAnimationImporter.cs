using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    // Import targets for PixelLab characters. The shared importer does the work; each target names
    // its art folder, animation folder and set asset. Add an enemy by adding a target and a menu item.
    [InitializeOnLoad]
    public static class PlayerAnimationImporter
    {
        public const string ArtRoot = "Assets/Art/Sprites/Player";
        public const string AnimationRoot = "Assets/Animations/Player";
        public const string SetPath = AnimationRoot + "/PlayerAnimationSet.asset";

        public static readonly DirectionalAnimationImporter.Target Player = new DirectionalAnimationImporter.Target
        {
            Name = "Player", ArtRoot = ArtRoot, AnimationRoot = AnimationRoot, SetPath = SetPath,
            SetType = typeof(PlayerAnimationSet),
            Aliases = new Dictionary<string, string>
            {
                { "catch", "catching" }, { "sprint", "run" }, { "throw", "throwrelease" }, { "dodge", "dash" },
                { "walking", "walk" }, { "running", "run" }
            },
            // The throw's lunge steps toward the camera, below the standing feet.
            GroundFromRotationsSlots = new HashSet<string> { "throwRelease" },
            Defaults = new Dictionary<string, (float, bool)>
            {
                { "rotations", (1f, true) }, { "idle", (6f, true) }, { "combatIdle", (8f, true) },
                { "walk", (10f, true) }, { "run", (12f, true) }, { "dash", (12f, false) },
                { "charge", (8f, true) }, { "throwAim", (8f, true) }, { "rest", (6f, true) },
                { "hurt", (10f, false) }, { "death", (8f, false) }, { "getUp", (8f, false) },
                { "drink", (8f, false) }
            }
        };

        public static readonly DirectionalAnimationImporter.Target Wolf = new DirectionalAnimationImporter.Target
        {
            Name = "Wolf", ArtRoot = "Assets/Art/Sprites/Enemies/Wolf", AnimationRoot = "Assets/Animations/Enemies/Wolf",
            // The set keeps its original path so Wolf.prefab's reference survives.
            SetPath = "Assets/Animations/Enemies/Wolf.asset", SetType = typeof(WolfAnimationSet), GroundFromRotations = true,
            Aliases = new Dictionary<string, string>
            {
                { "fastwalk", "walk" }, { "running", "run" }, { "running8frames", "run" }, { "sneaking", "stalk" }
            },
            Defaults = new Dictionary<string, (float, bool)>
            {
                { "rotations", (1f, true) }, { "idle", (6f, true) }, { "stalk", (8f, true) }, { "walk", (10f, true) }, { "run", (12f, true) },
                { "bark", (10f, false) }, { "bite", (12f, false) }, { "hurt", (12f, false) }, { "death", (10f, false) }
            }
        };

        static PlayerAnimationImporter()
        {
            DirectionalAnimationImporter.Targets.Clear();
            DirectionalAnimationImporter.Targets.Add(Player);
            DirectionalAnimationImporter.Targets.Add(Wolf);
        }

        [MenuItem("Road of the Old King/Art/Import Player Animations")]
        public static void RebuildFromMenu() => Debug.Log(Rebuild());

        [MenuItem("Road of the Old King/Art/Import Wolf Animations")]
        public static void RebuildWolfFromMenu() => Debug.Log(RebuildWolf());

        public static string Rebuild() => DirectionalAnimationImporter.Import(Player);
        public static string RebuildWolf() => DirectionalAnimationImporter.Import(Wolf);
    }
}
