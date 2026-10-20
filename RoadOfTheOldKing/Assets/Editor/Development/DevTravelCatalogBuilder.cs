using System.IO;
using System.Linq;
using RoadOfTheOldKing.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Keep the cheat menu in sync with authored arrivals; never save or replace an open scene.
[InitializeOnLoad]
public sealed class DevTravelCatalogBuilder : IPreprocessBuildWithReport
{
    private const string Path = "Assets/Resources/DevTools/TravelDestinations.json";
    public int callbackOrder => 0;

    static DevTravelCatalogBuilder()
    {
        EditorSceneManager.sceneSaved += _ => Schedule();
        EditorBuildSettings.sceneListChanged += Schedule;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) RefreshCatalog();
        };
    }

    private static void Schedule()
    {
        EditorApplication.delayCall -= RefreshWhenIdle;
        EditorApplication.delayCall += RefreshWhenIdle;
    }
    private static void RefreshWhenIdle()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) RefreshCatalog();
    }

    public void OnPreprocessBuild(BuildReport report) => RefreshCatalog();

    public static void RefreshCatalog()
    {
        var catalog = new DevTravelCatalog();
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            // Preview scenes read saved content without disturbing dirty scenes or active selection.
            var scene = EditorSceneManager.OpenPreviewScene(entry.path);
            try { catalog.destinations.AddRange(DevTravelCatalog.FromScene(scene)); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        catalog.destinations = catalog.destinations.OrderBy(d => d.scenePath).ThenBy(d => d.label).ToList();
        string json = JsonUtility.ToJson(catalog, true) + "\n";
        if (File.Exists(Path) && File.ReadAllText(Path) == json) return;
        File.WriteAllText(Path, json);
        AssetDatabase.ImportAsset(Path);
    }
}
