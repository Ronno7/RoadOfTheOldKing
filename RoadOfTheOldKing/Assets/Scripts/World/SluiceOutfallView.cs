using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A visible outlet coupled to the existing gate stroke and pond reveal, not a water simulation.
    [DisallowMultipleComponent, DefaultExecutionOrder(40)]
    public sealed class SluiceOutfallView : MonoBehaviour
    {
        [SerializeField] private SluiceMechanismView mechanism;
        [SerializeField] private RecallSluice sluice;
        [SerializeField] private SluiceWaterReveal pondReveal;
        [SerializeField] private SpriteRenderer surge;
        [SerializeField] private Sprite[] flowFrames;
        [SerializeField] private SpriteRenderer upstreamSurface;
        [SerializeField] private Renderer[] wetMarks;

        private Color surgeColor, upstreamColor;
        private Sprite surgeSprite;
        private bool surgeEnabled, upstreamEnabled, cached;
        private bool[] wetEnabled;
        private float frameTime;
        private const float FramesPerSecond = 8f;

        private void Cache()
        {
            if (cached) return;
            cached = true;
            if (surge != null)
            {
                surgeColor = surge.color;
                surgeSprite = surge.sprite;
                surgeEnabled = surge.enabled;
            }
            if (upstreamSurface != null)
            {
                upstreamColor = upstreamSurface.color;
                upstreamEnabled = upstreamSurface.enabled;
            }
            if (wetMarks != null)
            {
                wetEnabled = new bool[wetMarks.Length];
                for (int i = 0; i < wetMarks.Length; i++)
                    if (wetMarks[i] != null) wetEnabled[i] = wetMarks[i].enabled;
            }
        }

        private void OnEnable()
        {
            Cache();
            frameTime = 0f;
            RefreshNow();
        }

        private void LateUpdate() => Present(Time.deltaTime);

        // The only local clock cycles the four artwork frames. Progress remains sluice-owned.
        public void Present(float seconds)
        {
            Cache();
            float opening = mechanism != null ? Mathf.Clamp01(mechanism.Opening) : 0f;
            int frameCount = flowFrames != null ? Mathf.Min(4, flowFrames.Length) : 0;
            if (opening > 0f && seconds > 0f && frameCount > 0)
                frameTime = Mathf.Repeat(frameTime + seconds, frameCount / FramesPerSecond);
            else if (opening <= 0f)
                frameTime = 0f;

            bool draining = pondReveal != null && pondReveal.IsDraining;
            bool settledLow = sluice != null && sluice.Stage == SluiceStage.PondLow && !draining;
            if (surge != null)
            {
                // The draining surge subsides into a small persistent rivulet under the open gate.
                float strength = draining ? Mathf.Lerp(1f, .3f, pondReveal.Progress) : .3f;
                var color = surgeColor;
                color.a *= opening * strength;
                surge.color = color;
                surge.enabled = opening > 0f;
                if (frameCount > 0 && opening > 0f)
                {
                    int frame = Mathf.Min(frameCount - 1, Mathf.FloorToInt(frameTime * FramesPerSecond));
                    if (flowFrames[frame] != null) surge.sprite = flowFrames[frame];
                }
            }
            if (upstreamSurface != null)
            {
                upstreamSurface.enabled = !settledLow;
                var color = upstreamColor;
                if (draining) color.a *= 1f - pondReveal.Progress;
                upstreamSurface.color = color;
            }
            if (wetMarks != null)
                for (int i = 0; i < wetMarks.Length; i++)
                    if (wetMarks[i] != null) wetMarks[i].enabled = settledLow;
        }

        public void RefreshNow() => Present(0f);

        private void OnDisable()
        {
            if (!cached) return;
            if (surge != null)
            {
                surge.color = surgeColor;
                surge.sprite = surgeSprite;
                surge.enabled = surgeEnabled;
            }
            if (upstreamSurface != null)
            {
                upstreamSurface.color = upstreamColor;
                upstreamSurface.enabled = upstreamEnabled;
            }
            if (wetMarks != null && wetEnabled != null)
                for (int i = 0; i < Mathf.Min(wetMarks.Length, wetEnabled.Length); i++)
                    if (wetMarks[i] != null) wetMarks[i].enabled = wetEnabled[i];
        }
    }
}
