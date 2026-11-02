using System;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Presentation only: both the advancing front and its recession use basin progress.
    [DisallowMultipleComponent]
    public sealed class SluiceCanalView : MonoBehaviour
    {
        [Serializable]
        private sealed class Reach
        {
            public SpriteRenderer surface;
            public float distance, length;
        }

        [SerializeField] private Reach[] reaches;
        [SerializeField] private Sprite[] flowFrames;
        private Vector3[] positions, starts, directions;
        private Vector2[] sizes;
        private Sprite[] sprites;
        private bool[] enabledStates;
        private float totalLength;
        private const int Phases = 4, Widths = 13;
        private const float FrontStart = .015f, FrontTravel = .4f;

        private void Cache()
        {
            if (positions != null || reaches == null) return;
            int count = reaches.Length;
            positions = new Vector3[count]; starts = new Vector3[count]; directions = new Vector3[count];
            sizes = new Vector2[count]; sprites = new Sprite[count]; enabledStates = new bool[count];
            for (int i = 0; i < count; i++)
            {
                var reach = reaches[i];
                if (reach.surface == null) continue;
                var surface = reach.surface;
                positions[i] = surface.transform.position;
                directions[i] = -surface.transform.up;
                starts[i] = positions[i] - directions[i] * reach.length * .5f;
                sizes[i] = surface.size; sprites[i] = surface.sprite; enabledStates[i] = surface.enabled;
                totalLength = Mathf.Max(totalLength, reach.distance + reach.length);
            }
        }

        public void Present(bool open, float basinProgress, int phase)
        {
            Cache();
            if (positions == null || flowFrames == null || flowFrames.Length != Widths * Phases) return;
            float progress = Mathf.Clamp01(basinProgress);
            float front = Mathf.Clamp01((progress - FrontStart) / FrontTravel) * totalLength;
            phase = Mathf.Clamp(phase, 0, Phases - 1);
            for (int i = 0; i < reaches.Length; i++)
            {
                var reach = reaches[i];
                var surface = reach.surface;
                if (surface == null) continue;
                float visible = Mathf.Round(Mathf.Clamp(front - reach.distance, 0f, reach.length) * 16f) / 16f;
                surface.enabled = open && visible > 0f;
                if (!surface.enabled) continue;
                float arrival = FrontStart + FrontTravel * reach.distance / Mathf.Max(totalLength, .001f);
                // Every reach settles at progress 1, exactly when the basin finishes draining.
                float recession = Mathf.InverseLerp(arrival, 1f, progress);
                int width = Mathf.RoundToInt(Mathf.SmoothStep(0f, 1f, recession) * (Widths - 1));
                surface.sprite = flowFrames[width * Phases + phase];
                surface.size = new Vector2(sizes[i].x, visible);
                surface.transform.position = starts[i] + directions[i] * visible * .5f;
            }
        }

        public void RestoreAuthoredVisuals()
        {
            if (positions == null) return;
            for (int i = 0; i < reaches.Length; i++)
            {
                var surface = reaches[i].surface;
                if (surface == null) continue;
                surface.sprite = sprites[i]; surface.size = sizes[i];
                surface.transform.position = positions[i]; surface.enabled = enabledStates[i];
            }
        }

        private void OnDisable() => RestoreAuthoredVisuals();
    }
}
