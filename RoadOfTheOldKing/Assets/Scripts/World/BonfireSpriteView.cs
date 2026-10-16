using UnityEngine;

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
        private Bonfire fire;
        private float frameClock;
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
            if (!lit)
            {
                frameClock = 0f;
                return;
            }
            frameClock = (frameClock + elapsed * Mathf.Max(0.1f, framesPerSecond)) % litFrames.Length;
            flame.sprite = litFrames[Mathf.FloorToInt(frameClock)];
        }
    }
}
