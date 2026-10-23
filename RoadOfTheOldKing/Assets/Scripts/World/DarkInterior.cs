using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RoadOfTheOldKing.World
{
    // Lantern-lit interior (reworked 22 Oct). Light is real URP 2D light: a warm, gently flickering lantern on the
    // player whose shadows come from the walls' ShadowCaster2Ds, and a daylight pool at the entrance. The scene's
    // global light stays near zero, and one cover quad drawn over the world (InteriorDarkness) turns black wherever
    // that light doesn't reach, so unlit content (world UI, effects) never shows through the dark.
    // Gameplay reads visibility analytically (VisibilityAt / CanRead): the same falloff, clipped by the walls.
    [DisallowMultipleComponent, DefaultExecutionOrder(300)]
    public sealed class DarkInterior : MonoBehaviour
    {
        [SerializeField] private Shader darknessShader;
        [Tooltip("Masonry blockers: they cast the lantern's shadows (ShadowCaster2D) and clip gameplay visibility.")]
        [SerializeField] private BoxCollider2D[] walls = System.Array.Empty<BoxCollider2D>();
        [Tooltip("How far light reaches into a wall's edge, so its stone face reads lit instead of a void. Shadow shapes " +
            "are inset by this much (rebuild with the context menu after changing it).")]
        [SerializeField, Min(0f)] private float wallFaceReveal = .45f;

        [Header("Daylight at the entrance")]
        [SerializeField] private Vector2 daylightCenter = new Vector2(0, -.5f);
        [SerializeField] private Vector2 daylightHalfSize = new Vector2(1.75f, 2f);
        [SerializeField, Min(.1f)] private float daylightFeather = .75f;
        [SerializeField, Min(0f)] private float daylightIntensity = .5f;

        [Header("Lantern")]
        [SerializeField, Min(.1f)] private float innerRadius = 2.5f;
        [SerializeField, Min(.2f)] private float outerRadius = 5.5f;
        [SerializeField] private Color lanternColor = new Color(1f, .8f, .58f);
        [SerializeField, Min(0f)] private float lanternIntensity = .85f;
        [Tooltip("Light2D falloff curve: higher keeps the core bright longer before it fades.")]
        [SerializeField, Range(0f, 1f)] private float lanternFalloff = .6f;
        [SerializeField, Range(0f, 1f)] private float shadowSoftness = .8f;
        [Tooltip("Slow flame flicker as a share of intensity.")]
        [SerializeField, Range(0f, .3f)] private float flicker = .06f;
        [SerializeField, Min(.1f)] private float flickerSpeed = 1.6f;
        [Tooltip("Lantern height above the player's feet, so the pool centres on the body.")]
        [SerializeField] private float lanternHeight = .5f;

        private static DarkInterior active;
        private Transform player;
        private GameObject cover;
        private Material material;
        private Light2D lantern, daylight;
        private Vector2 origin;
        private bool hasLantern;
        private const float CoverSize = 512f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => active = null;

        private void OnEnable()
        {
            active = this;
            if (Application.isPlaying)
            {
                player = FindFirstObjectByType<PlayerHealth>()?.transform;
                Present(player != null ? (Vector2)player.position : Vector2.zero, false);
            }
        }

        private void LateUpdate()
        {
            if (player == null) player = FindFirstObjectByType<PlayerHealth>()?.transform;
            if (player == null) return;
            bool owned = GameSession.Instance != null && GameSession.Instance.Progress.OwnsTool(WorldTool.ProtectedLantern);
            Present(player.position, owned);
        }

        // Also usable by staged Edit Mode captures; never changes ownership or saves.
        public void Present(Vector2 position, bool lanternOwned)
        {
            if (darknessShader == null) return;
            // Editor reloads keep some references and not others: rebuild the whole set if any part is missing.
            if (cover == null || lantern == null || daylight == null) { Release(); Build(); }
            origin = position; hasLantern = lanternOwned;
            lantern.enabled = lanternOwned;
            lantern.transform.position = new Vector3(position.x, position.y + lanternHeight, 0);
            float t = Application.isPlaying ? Time.time * flickerSpeed : 0f;
            // Two slow noise bands: a breathing flame, never a strobe.
            float wave = (Mathf.PerlinNoise(t, .37f) - .5f) * 2f * .7f + (Mathf.PerlinNoise(t * 2.3f, 1.91f) - .5f) * 2f * .3f;
            lantern.intensity = lanternIntensity * (1f + flicker * wave);
            lantern.pointLightOuterRadius = outerRadius * (1f + flicker * .25f * wave);
        }

        private void Build()
        {
            material = new Material(darknessShader) { hideFlags = HideFlags.DontSave };
            cover = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cover.name = "Interior darkness";
            cover.hideFlags = HideFlags.DontSave;
            DestroyImmediate(cover.GetComponent<Collider>());
            cover.transform.SetParent(transform, false);
            cover.transform.localScale = new Vector3(CoverSize, CoverSize, 1);
            var renderer = cover.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 32760;

            lantern = NewLight("Lantern light", Light2D.LightType.Point);
            lantern.color = lanternColor;
            lantern.pointLightInnerRadius = innerRadius;
            lantern.pointLightOuterRadius = outerRadius;
            lantern.falloffIntensity = lanternFalloff;
            lantern.shadowsEnabled = true;
            lantern.shadowIntensity = 1f;
            lantern.shadowSoftness = shadowSoftness;

            daylight = NewLight("Entrance daylight", Light2D.LightType.Freeform);
            daylight.intensity = daylightIntensity;
            daylight.shapeLightFalloffSize = daylightFeather;
            Vector2 c = daylightCenter, h = daylightHalfSize;
            daylight.SetShapePath(new[] { new Vector3(c.x - h.x, c.y - h.y), new Vector3(c.x + h.x, c.y - h.y),
                new Vector3(c.x + h.x, c.y + h.y), new Vector3(c.x - h.x, c.y + h.y) });
        }

        private Light2D NewLight(string name, Light2D.LightType type)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = type;
            return light;
        }

        // Gameplay visibility (reading inscriptions, revealing pickups): daylight rectangle plus the lantern's falloff,
        // stopped a shallow masonry face into the first wall along the ray, as the shadows draw it.
        public float VisibilityAt(Vector2 point)
        {
            float edge = Mathf.Max(Mathf.Abs(point.x - daylightCenter.x) - daylightHalfSize.x,
                Mathf.Abs(point.y - daylightCenter.y) - daylightHalfSize.y);
            float day = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / daylightFeather));
            Vector2 delta = point - origin;
            float distance = delta.magnitude;
            float light = hasLantern && distance <= Reach(distance > 0 ? delta / distance : Vector2.right) ?
                1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(innerRadius, outerRadius, distance)) : 0;
            return Mathf.Max(day, light);
        }
        public static bool CanRead(Vector2 point) => active == null || active.VisibilityAt(point) >= .45f;

        private float Reach(Vector2 direction)
        {
            float nearest = outerRadius;
            foreach (var wall in walls)
            {
                if (wall == null || !wall.enabled || !wall.gameObject.activeInHierarchy) continue;
                var b = wall.bounds;
                float enter = 0, exit = outerRadius;
                if (Slab(origin.x, direction.x, b.min.x, b.max.x, ref enter, ref exit) &&
                    Slab(origin.y, direction.y, b.min.y, b.max.y, ref enter, ref exit))
                    nearest = Mathf.Min(nearest, enter + Mathf.Min(wallFaceReveal, (exit - enter) * .5f));
            }
            return nearest;
        }
        private static bool Slab(float p, float d, float min, float max, ref float enter, ref float exit)
        {
            if (Mathf.Abs(d) < .000001f) return p >= min && p <= max;
            float a = (min - p) / d, b = (max - p) / d;
            enter = Mathf.Max(enter, Mathf.Min(a, b)); exit = Mathf.Min(exit, Mathf.Max(a, b));
            return enter <= exit;
        }

#if UNITY_EDITOR
        // One ShadowCaster2D per wall: the collider's rectangle inset by the face reveal (thin walls keep a sliver,
        // so they still block). Explicit shapes, not the Collider2D provider, which failed to build in this URP.
        [ContextMenu("Rebuild wall shadows")]
        private void RebuildWallShadows()
        {
            foreach (var wall in walls)
            {
                if (wall == null) continue;
                var caster = wall.GetComponent<ShadowCaster2D>();
                if (caster == null) caster = UnityEditor.Undo.AddComponent<ShadowCaster2D>(wall.gameObject);
                var so = new UnityEditor.SerializedObject(caster);
                Vector2 o = wall.offset, h = wall.size * .5f;
                h = new Vector2(Mathf.Max(.05f, h.x - wallFaceReveal), Mathf.Max(.05f, h.y - wallFaceReveal));
                var path = so.FindProperty("m_ShapePath");
                path.arraySize = 4;
                path.GetArrayElementAtIndex(0).vector3Value = new Vector3(o.x - h.x, o.y - h.y);
                path.GetArrayElementAtIndex(1).vector3Value = new Vector3(o.x + h.x, o.y - h.y);
                path.GetArrayElementAtIndex(2).vector3Value = new Vector3(o.x + h.x, o.y + h.y);
                path.GetArrayElementAtIndex(3).vector3Value = new Vector3(o.x - h.x, o.y + h.y);
                // URP rebuilds the shadow mesh when this hash changes, so derive it from the new corners.
                so.FindProperty("m_ShapePathHash").intValue = (o, h, wallFaceReveal).GetHashCode() | 1;
                so.FindProperty("m_ShadowCastingSource").enumValueIndex = 1; // Shape editor
                so.FindProperty("m_CastingOption").enumValueIndex = 1; // Cast only, never self-shadow
                so.ApplyModifiedProperties();
                caster.enabled = false; caster.enabled = true; // rebuild the shadow mesh now
            }
        }
#endif

        private void OnDisable()
        {
            if (active == this) active = null;
            Release();
        }

        private void Release()
        {
            Dispose(cover); Dispose(material);
            if (lantern != null) Dispose(lantern.gameObject);
            if (daylight != null) Dispose(daylight.gameObject);
            cover = null; material = null; lantern = daylight = null;
        }
        private static void Dispose(Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
