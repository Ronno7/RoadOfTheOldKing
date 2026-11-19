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
        [Header("Upgrade fire")]
        [Tooltip("A fire that offers upgrades burns gold (user, 17 Nov): these frames, the same count and timing as the lit frames.")]
        [SerializeField] private Sprite[] upgradeFrames;
        [SerializeField] private Color upgradeGlowColor = new Color(1f, .84f, .48f);
        [Tooltip("The gold flame and its light are a little larger (user, 17 Nov: \"slightly larger\"); the flame grows up from its base.")]
        [SerializeField, Min(1f)] private float upgradeFlameScale = 1.25f;
        [Tooltip("Art pixels the gold flame sits higher, clear of the brazier's rim (user, 17 Nov: \"needs to move up a couple pixels\").")]
        [SerializeField, Min(0)] private int upgradeFlameLift = 2;
        [Tooltip("The fire's base, and the bronze brazier ring an upgrade fire shows instead (empty: the usual base).")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [Tooltip("The usual stone base. Held here because an upgrade fire's renderer is saved showing the brazier.")]
        [SerializeField] private Sprite stoneBase;
        [SerializeField] private Sprite upgradeBase;
        private Bonfire fire;
        private Sprite usualBase;
        private Color usualGlow;
        private float usualGlowRadius;
        private Vector3 usualFlameScale, usualFlamePosition;
        private bool captured;
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
            if (glow != null && glowIntensity < 0f) { glowIntensity = glow.intensity; usualGlow = glow.color; usualGlowRadius = glow.pointLightOuterRadius; }
            if (baseRenderer != null && usualBase == null) usualBase = stoneBase != null ? stoneBase : baseRenderer.sprite;
            if (flame != null && !captured) { usualFlameScale = flame.transform.localScale; usualFlamePosition = flame.transform.localPosition; captured = true; }
            frameClock = 0f;
            ApplySize();
            Refresh(0f);
        }

        // Scales the flame about its base (the frames share one pivot), so a larger flame still sits on the logs.
        private void ApplySize()
        {
            if (flame == null || !captured) return;
            float factor = Golden ? upgradeFlameScale : 1f;
            flame.transform.localScale = usualFlameScale * factor;
            var frames = Frames;
            float pivotUnits = frames != null && frames.Length > 0 && frames[0] != null ? frames[0].pivot.y / frames[0].pixelsPerUnit : 0f;
            float lift = Golden ? upgradeFlameLift / 16f : 0f;
            flame.transform.localPosition = usualFlamePosition + Vector3.up * (pivotUnits * usualFlameScale.y * (factor - 1f) + lift);
            if (glow != null && usualGlowRadius > 0f) glow.pointLightOuterRadius = usualGlowRadius * factor;
        }

#if UNITY_EDITOR
        // The base is saved with the scene, so an upgrade fire shows its brazier in the editor too, not only in play,
        // and a fire that stops offering upgrades gets its stone base back. Bonfire calls this when its flag changes.
        internal void SyncSavedBase()
        {
            if (Application.isPlaying || baseRenderer == null || stoneBase == null) return;
            var bonfire = GetComponent<Bonfire>();
            Sprite wanted = bonfire != null && bonfire.AllowsUpgrades && upgradeBase != null ? upgradeBase : stoneBase;
            if (baseRenderer.sprite == wanted) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || baseRenderer == null || baseRenderer.sprite == wanted) return;
                var sprite = new UnityEditor.SerializedObject(baseRenderer).FindProperty("m_Sprite");
                // Back to stone on a prefab instance: drop the override rather than store the prefab's own value.
                if (wanted == stoneBase && sprite.prefabOverride)
                    UnityEditor.PrefabUtility.RevertPropertyOverride(sprite, UnityEditor.InteractionMode.UserAction);
                else
                {
                    UnityEditor.Undo.RecordObject(baseRenderer, "Bonfire base");
                    baseRenderer.sprite = wanted;
                }
            };
        }

        private void OnValidate() => SyncSavedBase();
#endif

        private bool Golden => fire != null && fire.AllowsUpgrades;
        private Sprite[] Frames => Golden && upgradeFrames != null && upgradeFrames.Length > 0 ? upgradeFrames : litFrames;

        private void LateUpdate() => Refresh(Time.deltaTime);
        private void OnDisable() => smoke?.Stop();
        private void OnDestroy() { smoke?.Dispose(); smoke = null; }

        private void Refresh(float elapsed)
        {
            if (flame == null) return;
            // The brazier marks an upgrade fire lit or not, so it can be recognised before it's lit.
            if (baseRenderer != null && usualBase != null) baseRenderer.sprite = Golden && upgradeBase != null ? upgradeBase : usualBase;
            var frames = Frames;
            bool lit = fire != null && fire.IsDiscovered && frames != null && frames.Length > 0;
            flame.enabled = lit;
            smoke?.Refresh(lit, flame);
            if (glow != null) { glow.enabled = lit; glow.color = Golden ? upgradeGlowColor : usualGlow; }
            if (!lit)
            {
                frameClock = 0f;
                return;
            }
            frameClock = (frameClock + elapsed * Mathf.Max(0.1f, framesPerSecond)) % frames.Length;
            int frame = Mathf.FloorToInt(frameClock);
            flame.sprite = frames[frame];
            // A fixed scramble of the frame index: each frame always has the same brightness, never a smooth pulse.
            if (glow != null) glow.intensity = glowIntensity * (1f - glowFlicker * ((frame * 5 + 3) % 7) / 6f);
        }
    }
}
