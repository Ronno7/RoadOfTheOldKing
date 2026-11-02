using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    public sealed class AnimationImportWindow : EditorWindow
    {
        int selected;
        Vector2 scroll;
        string result = "Choose a character to import its prepared sprite folders.";

        [MenuItem("Road of the Old King/Animation Import", priority = 30)]
        public static void Open() => GetWindow<AnimationImportWindow>("Animation Import");

        internal static DirectionalAnimationImporter.Target[] Characters()
        {
            // Ensure registration even when this window is opened immediately after a reload.
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(PlayerAnimationImporter).TypeHandle);
            return DirectionalAnimationImporter.Targets.ToArray();
        }

        void OnGUI()
        {
            var targets = Characters();
            if (targets.Length == 0) { EditorGUILayout.HelpBox("No characters registered.", MessageType.Info); return; }
            selected = Mathf.Clamp(selected, 0, targets.Length - 1);
            selected = EditorGUILayout.Popup("Character", selected, targets.Select(t => Regex.Replace(t.Name, "([a-z])([A-Z])", "$1 $2")).ToArray());
            var target = targets[selected];
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(target.ArtRoot);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Sprite folder", folder, typeof(DefaultAsset), false);
                EditorGUILayout.ObjectField("Animation set", AssetDatabase.LoadMainAssetAtPath(target.SetPath), typeof(UnityEngine.Object), false);
            }
            EditorGUILayout.HelpBox("Imports prepared files already in this character's sprite folder. Existing playback tuning is preserved. Disabled folders stay excluded.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || folder == null))
                if (GUILayout.Button("Import selected character"))
                {
                    try { result = DirectionalAnimationImporter.Import(target); }
                    catch (Exception e) { result = e.Message; Debug.LogException(e); }
                }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.SelectableLabel(result, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(140));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World mechanisms", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Hayfields imports the approved source manifest with a fixed hinge pivot. It does not change scenes or character art.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                if (GUILayout.Button("Import Hayfields mechanism"))
                {
                    try { result = HayfieldsAnimationImporter.Import(); }
                    catch (Exception e) { result = e.Message; Debug.LogException(e); }
                }
            EditorGUILayout.HelpBox("Waterworks imports the approved drain manifest and separates water from dry masonry. It does not change scenes or gate art.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                if (GUILayout.Button("Import Waterworks drains"))
                {
                    try { result = WaterworksAnimationImporter.Import(); }
                    catch (Exception e) { result = e.Message; Debug.LogException(e); }
                }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                if (GUILayout.Button("Import Waterworks gates"))
                {
                    try { result = WaterworksAnimationImporter.ImportGates(); }
                    catch (Exception e) { result = e.Message; Debug.LogException(e); }
                }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                if (GUILayout.Button("Import Waterworks canal"))
                {
                    try { result = WaterworksAnimationImporter.ImportCanal(); }
                    catch (Exception e) { result = e.Message; Debug.LogException(e); }
                }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                if (GUILayout.Button("Import Waterworks culvert"))
                {
                    try { result = WaterworksAnimationImporter.ImportCulvert(); }
                    catch (Exception e) { result = e.Message; Debug.LogException(e); }
                }
        }
    }
}
