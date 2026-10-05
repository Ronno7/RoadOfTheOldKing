using System.Collections.Generic;
using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    // Ground-plane combat UI, independent of the body/held-weapon animation.
    // The entire sector is live at once; the moving crescent is a motion accent.
    [DisallowMultipleComponent, RequireComponent(typeof(AxeWeapon))]
    [DefaultExecutionOrder(220)]
    public sealed class AxeMeleeFeedback : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private Color tint = new Color(0.96f, 0.89f, 0.66f, 1f);
        [SerializeField, Range(0f, 1f)] private float windupOpacity = 0.22f;
        [SerializeField, Range(0f, 1f)] private float areaOpacity = 0.10f;
        [SerializeField, Range(0f, 1f)] private float edgeOpacity = 0.65f;
        [SerializeField, Min(0.01f)] private float edgeWidth = 0.025f;
        [SerializeField, Min(0.01f)] private float swipeWidth = 0.18f;
        private const int Segments = 48;
        private readonly Vector3[] boundary = new Vector3[Segments + 1];
        private readonly List<Vector3> vertices = new List<Vector3>(900);
        private readonly List<Color> colors = new List<Color>(900);
        private readonly List<int> triangles = new List<int>(1800);
        private AxeWeapon weapon;
        private Mesh mesh;
        private MeshRenderer surface;
        private Transform visual;
        private RoadOfTheOldKing.Player.PlayerCombatController lastOwner;
        private SpriteRenderer body;

        private void Awake()
        {
            weapon = GetComponent<AxeWeapon>();
            var go = new GameObject("Melee footprint");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            visual = go.transform;
            mesh = new Mesh { name = "Melee footprint (runtime)" };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface = go.AddComponent<MeshRenderer>();
            surface.sharedMaterial = material;
            // Sprites/Default expects a per-renderer texture. This procedural mesh has
            // no sprite/UVs: bind white explicitly so terrain sprite draws cannot leave
            // it sampling a transparent texel from another texture.
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            surface.SetPropertyBlock(properties);
            surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            surface.receiveShadows = false;
            surface.enabled = false;
        }

        private void LateUpdate()
        {
            var phase = weapon.LightPhase;
            bool visible = weapon.isActiveAndEnabled && weapon.Owner != null &&
                weapon.Owner.isActiveAndEnabled && Time.timeScale > 0f &&
                (phase == MeleePhase.Windup || phase == MeleePhase.Active);
            surface.enabled = visible;
            if (!visible) return;

            if (lastOwner != weapon.Owner)
            {
                lastOwner = weapon.Owner;
                body = lastOwner.GetComponentInChildren<SpriteRenderer>();
            }
            if (body != null)
            {
                surface.sortingLayerID = body.sortingLayerID;
                surface.sortingOrder = body.sortingOrder - 1;
            }
            visual.SetPositionAndRotation(weapon.Owner.transform.position, Quaternion.identity);
            bool active = phase == MeleePhase.Active;
            if (weapon.IsLightThrust)
            {
                DrawLane(active);
                return;
            }
            float aim = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg;
            float arc = weapon.LightArc;
            float reach = weapon.LightReach;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = (aim - arc * 0.5f + arc * i / Segments) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                boundary[i] = direction * reach;
            }

            vertices.Clear(); colors.Clear(); triangles.Clear();
            float progress = weapon.LightSwingProgress;
            if (weapon.ComboIndex == 1) progress = 1f - progress;
            for (int i = 0; i < Segments; i++)
            {
                Vector3 a = boundary[i], b = boundary[i + 1];
                if (active)
                    Triangle(Vector3.zero, a, b, Ink(areaOpacity * 0.25f), Ink(areaOpacity), Ink(areaOpacity));
                // Show full attack reach; terrain obstruction is handled only by damage detection.
                Band(a, b, edgeWidth, Ink(active ? edgeOpacity : windupOpacity));
                if (active)
                {
                    float t = (i + 0.5f) / Segments;
                    float distance = weapon.ComboIndex == 1 ? t - progress : progress - t;
                    // Short tapered swish, with a small leading cap so the first active
                    // sample is visible too. It never extends outside the full sector.
                    float strength = distance < 0f ? Mathf.Clamp01(1f + distance / 0.06f)
                        : Mathf.Clamp01(1f - distance / 0.38f);
                    Band(a, b, swipeWidth * strength, Ink(0.8f * strength));
                }
            }
            // Two quiet radial edges communicate the full angular limits.
            Radial(boundary[0], 1f, Ink(active ? edgeOpacity * 0.45f : windupOpacity));
            Radial(boundary[Segments], -1f, Ink(active ? edgeOpacity * 0.45f : windupOpacity));
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private Color Ink(float alpha) => new Color(tint.r, tint.g, tint.b, tint.a * alpha);

        private void DrawLane(bool active)
        {
            vertices.Clear(); colors.Clear(); triangles.Clear();
            Vector3 forward = weapon.AttackDirection;
            Vector3 side = new Vector3(-forward.y, forward.x, 0f);
            float halfWidth = weapon.LightLaneWidth * 0.5f;
            float reach = weapon.LightReach;
            if (active) LaneQuad(forward, side, 0f, reach, -halfWidth, halfWidth, Ink(areaOpacity));
            Color edge = Ink(active ? edgeOpacity : windupOpacity);
            float width = Mathf.Min(edgeWidth, halfWidth);
            LaneQuad(forward, side, 0f, reach, -halfWidth, -halfWidth + width, edge);
            LaneQuad(forward, side, 0f, reach, halfWidth - width, halfWidth, edge);
            LaneQuad(forward, side, 0f, width, -halfWidth, halfWidth, edge);
            LaneQuad(forward, side, reach - width, reach, -halfWidth, halfWidth, edge);
            if (active)
            {
                float tip = Mathf.Lerp(width, reach, weapon.LightSwingProgress);
                LaneQuad(forward, side, Mathf.Max(0f, tip - swipeWidth), tip,
                    -halfWidth + width, halfWidth - width, Ink(0.8f));
            }
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void LaneQuad(Vector3 forward, Vector3 side, float near, float far, float left, float right, Color color)
        {
            Vector3 a = forward * near + side * left, b = forward * far + side * left;
            Vector3 c = forward * far + side * right, d = forward * near + side * right;
            Triangle(a, b, c, color, color, color);
            Triangle(a, c, d, color, color, color);
        }

        private void Band(Vector3 a, Vector3 b, float width, Color color)
        {
            if (width <= 0f || color.a <= 0f) return;
            Vector3 innerA = a.normalized * Mathf.Max(0f, a.magnitude - width);
            Vector3 innerB = b.normalized * Mathf.Max(0f, b.magnitude - width);
            Triangle(innerA, a, b, color, color, color);
            Triangle(innerA, b, innerB, color, color, color);
        }

        private void Radial(Vector3 end, float side, Color color)
        {
            // Taper at both ends; do not spill past the arc's side or outer edge.
            Vector3 inset = new Vector3(-end.y, end.x, 0f).normalized * (Mathf.Min(edgeWidth, end.magnitude * 0.25f) * side);
            Triangle(Vector3.zero, end, end * 0.5f + inset, color, color, color);
        }

        private void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            int index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            colors.Add(ca); colors.Add(cb); colors.Add(cc);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
        }

        private void OnDisable() { if (surface != null) surface.enabled = false; }
        private void OnDestroy()
        {
            if (visual != null) Destroy(visual.gameObject);
            if (mesh != null) Destroy(mesh);
        }
    }
}
