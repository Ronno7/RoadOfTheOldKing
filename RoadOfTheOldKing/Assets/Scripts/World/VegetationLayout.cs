using System;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Baked authoring data only. Cutting is local to the loaded scene, never written to the save.
    [CreateAssetMenu(menuName = "Road of the Old King/Vegetation Layout")]
    public sealed class VegetationLayout : ScriptableObject
    {
        public Texture2D density;
        public Vector2Int origin;
        public int seed = 1703;
        public Species[] species = Array.Empty<Species>();
        public Plant[] plants = Array.Empty<Plant>();

        [Serializable] public sealed class Species
        {
            // Complete sprites, each rotating rigidly around its imported root pivot.
            // A species shares one atlas so its leaves remain in the same spatial batch.
            public Leaf[] leaves = Array.Empty<Leaf>();
            [Range(.25f, 2f)] public float flexibility = 1f;
            public Color debrisColor = new Color32(103, 132, 57, 255);
        }

        [Serializable] public sealed class Leaf
        {
            public Sprite sprite;
            public Vector2 offset;
            [Range(-70f, 70f)] public float restAngle;
            public float phaseOffset;
            public bool flipX;
        }

        [Serializable] public struct Plant
        {
            public Vector2 position;
            public int species;
            public float phase;
        }
    }
}
