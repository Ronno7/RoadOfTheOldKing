using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The retaining wall stays fixed while water recedes below it with the basin.
    [DisallowMultipleComponent]
    public sealed class SluiceCulvertView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer water;
        [SerializeField] private Sprite[] flowFrames;
        private Sprite authoredSprite;
        private bool cached;
        private const int Levels = 10, Phases = 4;

        public void Present(float basinProgress, int phase)
        {
            if (water == null || flowFrames == null || flowFrames.Length != Levels * Phases) return;
            if (!cached) { authoredSprite = water.sprite; cached = true; }
            int level = Mathf.RoundToInt(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(basinProgress)) * (Levels - 1));
            water.sprite = flowFrames[level * Phases + Mathf.Clamp(phase, 0, Phases - 1)];
        }

        public void RestoreAuthoredVisuals()
        {
            if (cached && water != null) water.sprite = authoredSprite;
        }

        private void OnDisable() => RestoreAuthoredVisuals();
    }
}
