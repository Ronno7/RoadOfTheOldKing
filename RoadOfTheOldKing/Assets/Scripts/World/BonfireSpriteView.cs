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
        [Tooltip("Gold embers over a lit upgrade fire (#90); ordinary fires have none.")]
        [SerializeField] private Material emberMaterial;
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
        [Header("Lighting")]
        [Tooltip("Seconds the ignition takes: the flame springs up from small, overshoots and settles; the light flares and eases back.")]
        [SerializeField, Min(.1f)] private float igniteSeconds = .45f;
        [Tooltip("Sparks, flame overshoot and light flare when an ordinary fire is lit (user, 18 Nov: lighting should feel responsive).")]
        [SerializeField, Min(0)] private int sparks = 12;
        [SerializeField, Min(1f)] private float flameOvershoot = 1.15f;
        [SerializeField, Min(1f)] private float glowFlare = 1.8f;
        [Tooltip("An upgrade fire's burst is brighter and larger (user: not too dramatic, but a strong moment).")]
        [SerializeField, Min(0)] private int upgradeSparks = 22;
        [SerializeField, Min(1f)] private float upgradeFlameOvershoot = 1.3f;
        [SerializeField, Min(1f)] private float upgradeGlowFlare = 2.6f;
        private Bonfire fire;
        private Sprite usualBase;
        private Color usualGlow;
        private float usualGlowRadius;
        private Vector3 usualFlameScale, usualFlamePosition;
        private bool captured;
        private float frameClock, glowIntensity = -1f;
        private BonfireSmoke smoke;
        private BonfireEmbers embers;
        private BonfireBurst burst;
        private Vector3 restFlameScale, restFlamePosition;
        private float pivotUnits;
        private float igniteAge = -1f; // seconds since lighting, or -1
        // Embers are born across the upper gold flame: this far below its canvas top.
        private const float EmberDrop = .55f;
        // Where smoke starts above a usual fire's root: seven art pixels above the top of its 24-pixel flame canvas.
        private const float SmokeHeight = 1.5f;

        // Match Tutorial: three native pixels above the fixed 24-pixel flame canvas.
        // Use its authored transform so differing fire roots and animation frames cannot shift the gap.
        public Vector3 PromptPosition => flame != null
            ? flame.transform.TransformPoint(Vector3.up * (15f / 16f))
            : transform.position + Vector3.up * 1.25f;

        private void OnEnable()
        {
            fire = GetComponent<Bonfire>();
            if (Application.isPlaying && flame != null && smokeMaterial != null && smoke == null)
                smoke = new BonfireSmoke(transform, smokeMaterial, "Bonfire smoke", new Vector3(0f, SmokeHeight, 0f));
            if (Application.isPlaying && flame != null && emberMaterial != null && embers == null && Golden)
                embers = new BonfireEmbers(transform, emberMaterial, Vector3.zero);
            if (Application.isPlaying && flame != null && emberMaterial != null && burst == null)
                burst = new BonfireBurst(transform, emberMaterial, Vector3.zero, Golden);
            if (fire != null) fire.Lit += Ignite;
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
            pivotUnits = frames != null && frames.Length > 0 && frames[0] != null ? frames[0].pivot.y / frames[0].pixelsPerUnit : 0f;
            float lift = Golden ? upgradeFlameLift / 16f : 0f;
            flame.transform.localPosition = usualFlamePosition + Vector3.up * (pivotUnits * usualFlameScale.y * (factor - 1f) + lift);
            restFlameScale = flame.transform.localScale;
            restFlamePosition = flame.transform.localPosition;
            // Sparks leap from the flame's base.
            burst?.SetBase(flame.transform.localPosition.y - pivotUnits * restFlameScale.y);
            // Smoke keeps the same gap above a larger flame's canvas top; it sat on the gold flame's tips (user, 18 Nov:
            // "the smoke particles on the fire are a little low").
            float canvas = frames != null && frames.Length > 0 && frames[0] != null ? frames[0].rect.height / frames[0].pixelsPerUnit : 0f;
            smoke?.SetHeight(SmokeHeight + canvas * usualFlameScale.y * (factor - 1f) + lift);
            smoke?.SetBright(Golden);
            // The flame canvas top, in this root's space.
            float canvasTop = flame.transform.localPosition.y + (frames != null && frames.Length > 0 && frames[0] != null
                ? (frames[0].rect.height - frames[0].pivot.y) / frames[0].pixelsPerUnit * flame.transform.localScale.y : 0f);
            embers?.SetHeight(canvasTop - EmberDrop);
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
        private void OnDisable()
        {
            if (fire != null) fire.Lit -= Ignite;
            smoke?.Stop(); embers?.Stop(); burst?.Stop();
            EndIgnition();
        }
        private void OnDestroy() { smoke?.Dispose(); smoke = null; embers?.Dispose(); embers = null; burst?.Dispose(); burst = null; }

        // Lighting (Bonfire.Lit): the flame springs up, the light flares, sparks and a puff of smoke burst out, with a
        // light rumble and the smallest camera thump. An upgrade fire's is brighter and larger. Never on load.
        private void Ignite()
        {
            if (!isActiveAndEnabled || flame == null) return;
            igniteAge = 0f;
            frameClock = 0f;
            Refresh(0f);
            bool gold = Golden;
            burst?.Fire(gold ? upgradeSparks : sparks, gold ? 1.25f : 1f, flame);
            smoke?.Burst(gold ? 3 : 2);
            RoadOfTheOldKing.Input.Rumble.Pulse(gold ? .45f : .3f, gold ? .65f : .45f, gold ? .22f : .15f);
            if (Camera.main != null && Camera.main.TryGetComponent(out RoadOfTheOldKing.Cameras.CameraFollow2D follow))
                follow.Kick(Vector2.down, gold ? .1f : .05f);
        }

        private void EndIgnition()
        {
            if (igniteAge < 0f) return;
            igniteAge = -1f;
            if (flame != null && captured) { flame.transform.localScale = restFlameScale; flame.transform.localPosition = restFlamePosition; }
        }

        // The flame's size through the ignition: from a quarter up past full (ease out), then back to full.
        private float IgnitionScale(float t)
        {
            float over = Golden ? upgradeFlameOvershoot : flameOvershoot;
            const float peak = .35f;
            if (t < peak) { float u = t / peak; return Mathf.Lerp(.25f, over, 1f - (1f - u) * (1f - u)); }
            return Mathf.Lerp(over, 1f, Mathf.SmoothStep(0f, 1f, (t - peak) / (1f - peak)));
        }

        private void Refresh(float elapsed)
        {
            if (flame == null) return;
            // The brazier marks an upgrade fire lit or not, so it can be recognised before it's lit.
            if (baseRenderer != null && usualBase != null) baseRenderer.sprite = Golden && upgradeBase != null ? upgradeBase : usualBase;
            var frames = Frames;
            bool lit = fire != null && fire.IsDiscovered && frames != null && frames.Length > 0;
            flame.enabled = lit;
            smoke?.Refresh(lit, flame);
            embers?.Refresh(lit && Golden, flame);
            if (glow != null) { glow.enabled = lit; glow.color = Golden ? upgradeGlowColor : usualGlow; }
            if (!lit)
            {
                frameClock = 0f;
                return;
            }
            frameClock = (frameClock + elapsed * Mathf.Max(0.1f, framesPerSecond)) % frames.Length;
            int frame = Mathf.FloorToInt(frameClock);
            flame.sprite = frames[frame];
            float flare = 1f;
            if (igniteAge >= 0f)
            {
                igniteAge += elapsed;
                float t = Mathf.Clamp01(igniteAge / igniteSeconds);
                float scale = IgnitionScale(t);
                // About the flame's base, so it grows up out of the logs.
                flame.transform.localScale = restFlameScale * scale;
                flame.transform.localPosition = restFlamePosition + Vector3.up * (pivotUnits * restFlameScale.y * (scale - 1f));
                // The flare follows the Screen flashes setting.
                flare = 1f + ((Golden ? upgradeGlowFlare : glowFlare) - 1f) * (1f - t) * (1f - t) *
                    Mathf.Clamp01(RoadOfTheOldKing.UI.FeedbackSettings.FlashScale);
                if (t >= 1f) EndIgnition();
            }
            // A fixed scramble of the frame index: each frame always has the same brightness, never a smooth pulse.
            if (glow != null) glow.intensity = glowIntensity * flare * (1f - glowFlicker * ((frame * 5 + 3) % 7) / 6f);
        }
    }
}
