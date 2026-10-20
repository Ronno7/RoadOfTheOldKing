using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadOfTheOldKing.EditorTools
{
    public sealed class SceneToolsWindow : EditorWindow
    {
        string result = "Run the updates needed after editing. Inspect the result, then save normally.";
        Vector2 scroll;

        [MenuItem("Road of the Old King/Scene Tools", priority = 10)]
        public static void Open() => GetWindow<SceneToolsWindow>("Scene Tools");

        void OnGUI()
        {
            var scene = SceneManager.GetActiveScene();
            bool tutorial = scene.path == "Assets/Scenes/Tutorial.unity";
            bool lowlands = scene.path == "Assets/Scenes/GreenLowlands.unity";
            bool barrow = scene.path == "Assets/Scenes/BarrowApproach.unity";
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField(scene.name, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Updates use your current scene edits. This window does not save or reload scenes. Bakes also write generated assets.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                if (lowlands)
                {
                    if (GUILayout.Button("Height Brush")) LowlandsElevationWindow.Open();
                    Button("Update basin drainage", () => { TerrainKitBuilder.BakeBasinDrainage(); return "Basin drainage updated."; });
                }
                if (tutorial || lowlands)
                {
                    Button("Update vegetation", () => {
                        if (tutorial) TutorialVegetationBuilder.Bake(); else GreenLowlandsVegetationBuilder.Bake();
                        return tutorial ? TutorialVegetationBuilder.ValidateLayout() : GreenLowlandsVegetationBuilder.ValidateLayout();
                    });
                    Button("Check vegetation", () => tutorial ? TutorialVegetationBuilder.ValidateLayout() : GreenLowlandsVegetationBuilder.ValidateLayout());
                }
                if (tutorial || lowlands || barrow)
                    Button("Update shadows", () => {
                        if (tutorial) GreenLowlandsShadowBuilder.BuildTutorial();
                        else if (barrow) GreenLowlandsShadowBuilder.BuildBarrow();
                        else GreenLowlandsShadowBuilder.Build();
                        return "Shadows updated.";
                    });
                else EditorGUILayout.HelpBox("No scene bakes are configured for this scene.", MessageType.None);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Painting assets", EditorStyles.boldLabel);
            if (tutorial)
            {
                Link("Tutorial environment palette", "Assets/Art/Tiles/Tutorial/Environment/Palettes/TutorialEnvironment.prefab");
                Link("Tutorial terrain palette", "Assets/Art/Tiles/Tutorial/Terrain/Palettes/TutorialTerrain.prefab");
                Link("Tutorial buildings and props", "Assets/Prefabs/Environment/Tutorial");
                Link("Tutorial teaching objects", "Assets/Prefabs/Tutorial");
            }
            if (lowlands || barrow || tutorial)
            {
                Link("Ground palettes", "Assets/Art/Tiles/Shared/Ground/Palettes");
                Link("Terrain palettes", "Assets/Art/Tiles/Shared/Terrain/Palettes");
                Link("Paths palettes", "Assets/Art/Tiles/Shared/Paths/Palettes");
                Link("Shared environment and trees", "Assets/Prefabs/Environment/Shared");
                Link("Shared decoration", "Assets/Prefabs/Decoration/Shared");
                Link("Shared interactive objects", "Assets/Prefabs/World/Shared");
                if (!tutorial)
                {
                    Link("Green Lowlands buildings", "Assets/Prefabs/Environment/GreenLowlands");
                    Link("Green Lowlands environment palette", "Assets/Art/Tiles/GreenLowlands/Environment/Palettes/GreenLowlandsEnvironment.prefab");
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(result, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        void Button(string label, Func<string> action)
        {
            if (!GUILayout.Button(label)) return;
            try { result = action(); }
            catch (Exception e) { result = e.Message; Debug.LogException(e); }
            SceneView.RepaintAll();
        }

        static void Link(string label, string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            using (new EditorGUI.DisabledScope(asset == null))
                if (GUILayout.Button(label)) { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
        }
    }
}
