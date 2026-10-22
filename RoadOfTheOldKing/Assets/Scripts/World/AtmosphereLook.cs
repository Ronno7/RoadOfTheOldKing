using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoadOfTheOldKing.World
{
    // One authored atmosphere for a region: its grade, its world overlay and its ambient particles.
    [CreateAssetMenu(menuName = "Road of the Old King/Atmosphere Look")]
    public sealed class AtmosphereLook : ScriptableObject
    {
        [Tooltip("A complete grade: it overrides every setting the scene's base volume sets.")]
        public VolumeProfile grade;
        [Tooltip("Overlay material (RoadOfTheOldKing/Atmosphere); empty for none.")]
        public Material overlay;
        public MoteLayer[] motes = Array.Empty<MoteLayer>();

#if UNITY_EDITOR
        // Grade and overlay edits show at once (shared assets); motes are built from these values, so a region
        // using this look rebuilds when it changes, in Edit and Play Mode alike.
        public static event Action<AtmosphereLook> Edited;
        private void OnValidate() => Edited?.Invoke(this);
#endif
    }

    [Serializable]
    public sealed class MoteLayer
    {
        public string name = "Motes";
        [Tooltip("RoadOfTheOldKing/AtmosphereMote material.")]
        public Material material;
        [Min(1)] public int maxParticles = 40;
        [Min(0f)] public float perSecond = 6f;
        public Vector2 lifetime = new Vector2(5f, 8f);
        [Tooltip("World units. Snapped motes cover whole 1/16 cells.")]
        public Vector2 size = new Vector2(1f / 16f, 1f / 16f);
        public Color colorA = Color.white, colorB = Color.white;
        [Tooltip("Steady drift, world units per second.")]
        public Vector2 drift = new Vector2(0.2f, 0.05f);
        [Tooltip("Wandering on top of the drift.")]
        [Min(0f)] public float wander = 0.15f;
    }
}
