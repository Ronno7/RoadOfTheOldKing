using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RoadOfTheOldKing.World
{
    /// <summary>Native frame animation for a discovered fire; the authored sprite scale stays fixed.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Bonfire))]
    public sealed class BonfireSpriteView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer flame;
        [SerializeField] private Sprite[] litFrames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;
        [SerializeField] private Material smokeMaterial;
        [Tooltip("Optional warm 2D light that burns with the flame; author it disabled at its full intensity.")]
        [SerializeField] private Light2D glow;
        [Tooltip("Share of the glow lost on the dimmest flame frame. Steps with the frames, so the light flickers in time with the art.")]
        [SerializeField, Range(0f, 0.5f)] private float glowFlicker = 0.15f;
        private Bonfire fire;
        private float frameClock, glowIntensity = -1f;
        private BonfireSmoke smoke;

        // Match Tutorial: three native pixels above the fixed 24-pixel flame canvas.
        // Use its authored transform so differing fire roots and animation frames cannot shift the gap.
        public Vector3 PromptPosition => flame != null
            ? flame.transform.TransformPoint(Vector3.up * (15f / 16f))
            : transform.position + Vector3.up * 1.25f;

        private void OnEnable()
        {
            fire = GetComponent<Bonfire>();
            if (Application.isPlaying && flame != null && smokeMaterial != null && smoke == null)
                smoke = new BonfireSmoke(transform, smokeMaterial);
            if (glow != null && glowIntensity < 0f) glowIntensity = glow.intensity;
            frameClock = 0f;
            Refresh(0f);
        }

        private void LateUpdate() => Refresh(Time.deltaTime);
        private void OnDisable() => smoke?.Stop();
        private void OnDestroy() { smoke?.Dispose(); smoke = null; }

        private void Refresh(float elapsed)
        {
            if (flame == null) return;
            bool lit = fire != null && fire.IsDiscovered && litFrames != null && litFrames.Length > 0;
            flame.enabled = lit;
            smoke?.Refresh(lit, flame);
            if (glow != null) glow.enabled = lit;
            if (!lit)
            {
                frameClock = 0f;
                return;
            }
            frameClock = (frameClock + elapsed * Mathf.Max(0.1f, framesPerSecond)) % litFrames.Length;
            int frame = Mathf.FloorToInt(frameClock);
            flame.sprite = litFrames[frame];
            // A fixed scramble of the frame index: each frame always has the same brightness, never a smooth pulse.
            if (glow != null) glow.intensity = glowIntensity * (1f - glowFlicker * ((frame * 5 + 3) % 7) / 6f);
        }
    }
}
