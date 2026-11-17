using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    // Which height each painted terrain map stands at, so presentation can tell whether someone is below a
    // cliff or on top of it. The Height plan is editor-only; these are the visible top and face maps, which
    // ship with the scene. Top maps stack (each is painted under every higher level too), so a point's level
    // is the highest map painted there. A face counts at the height of the top it holds up, but only for what
    // was struck: its bottom row overlaps walkable ground at the cliff's foot. Unpainted ground (the lowest
    // level) has no reading. Presentation only: nothing here changes collision or movement.
    [DisallowMultipleComponent]
    public sealed class ElevationLevels : MonoBehaviour
    {
        [Serializable]
        private struct Layer
        {
            public Tilemap map;
            public int level;
            [Tooltip("A cliff face: read for a struck point, never for where someone stands.")]
            public bool face;
        }

        [SerializeField] private Layer[] layers = Array.Empty<Layer>();
        private static readonly List<ElevationLevels> active = new List<ElevationLevels>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => active.Clear();

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        // Ground under someone's feet: top maps only.
        public static bool TryGround(Vector2 point, out int level) => TryLevel(point, false, out level);

        // A struck point: tops and faces.
        public static bool TryStruck(Vector2 point, out int level) => TryLevel(point, true, out level);

        private static bool TryLevel(Vector2 point, bool faces, out int level)
        {
            level = int.MinValue;
            bool found = false;
            foreach (var scene in active)
                foreach (var layer in scene.layers)
                {
                    if (layer.map == null || (layer.face && !faces) || !layer.map.HasTile(layer.map.WorldToCell(point))) continue;
                    level = Mathf.Max(level, layer.level);
                    found = true;
                }
            return found;
        }
    }
}
