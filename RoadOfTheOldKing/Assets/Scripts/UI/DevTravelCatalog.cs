#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Linq;
using RoadOfTheOldKing.World;
using UnityEngine.SceneManagement;

namespace RoadOfTheOldKing.UI
{
    [Serializable]
    public sealed class DevTravelDestination
    {
        public string scenePath, id, label;
        public bool bonfire;
    }

    // Generated from enabled scenes. IDs resolve to live transforms, never copied coordinates.
    [Serializable]
    public sealed class DevTravelCatalog
    {
        public List<DevTravelDestination> destinations = new List<DevTravelDestination>();

        public static IEnumerable<DevTravelDestination> FromScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var fire in root.GetComponentsInChildren<Bonfire>().Where(f => f.isActiveAndEnabled && !string.IsNullOrEmpty(f.Id)))
                    yield return new DevTravelDestination { scenePath = scene.path, id = fire.Id, label = fire.DisplayName, bonfire = true };
                foreach (var point in root.GetComponentsInChildren<SceneSpawnPoint>().Where(p => p.isActiveAndEnabled && !string.IsNullOrEmpty(p.Id)))
                    yield return new DevTravelDestination { scenePath = scene.path, id = point.Id, label = point.name };
            }
        }
    }
}
#endif
