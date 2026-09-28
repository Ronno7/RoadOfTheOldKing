using System.Collections.Generic;
using UnityEngine;

namespace TheLostShrine.Combat
{
    // Reads the same health used by combat, including restoration and capacity changes.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    [DefaultExecutionOrder(300)]
    public sealed class EnemyHealthIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField, Min(.1f)] private float width = 1.05f;
        [SerializeField, Min(.04f)] private float height = .16f;
        [SerializeField, Min(0f)] private float gap = .12f;
        [SerializeField] private Color upperColor = new Color(.86f, .22f, .23f);
        [SerializeField] private Color lowerColor = new Color(.58f, .09f, .14f);
        [SerializeField] private Color emptyColor = new Color(.19f, .07f, .09f);
        private Damageable health;
        private MeshRenderer display;
        private Mesh mesh;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();
        private float previousFraction = -1f;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body == null) { enabled = false; return; }
            var child = new GameObject("Enemy health indicator");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Enemy health bubble" };
            mesh.MarkDynamic();
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            display = child.AddComponent<MeshRenderer>();
            display.sharedMaterial = body.sharedMaterial;
            display.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            display.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            display.SetPropertyBlock(properties);
            display.enabled = false;
        }

        private void LateUpdate()
        {
            if (display == null) return;
            display.enabled = health.IsAlive && body != null && body.enabled && body.gameObject.activeInHierarchy;
            if (!display.enabled) return;
            // Full-rect sprite bounds keep the anchor steady throughout the wolf's poses.
            display.transform.position = new Vector3(body.bounds.center.x, body.bounds.max.y + gap + height * .5f, transform.position.z);
            display.transform.rotation = Quaternion.identity;
            display.sortingLayerName = "Player";
            display.sortingOrder = 30000;
            float fraction = Mathf.Clamp01((float)health.Health / Mathf.Max(1, health.MaxHealth));
            if (fraction == previousFraction) return;
            previousFraction = fraction;
            Rebuild(fraction);
        }

        private void Rebuild(float fraction)
        {
            vertices.Clear(); colors.Clear(); triangles.Clear();
            // Rounded dark recess, with two flat red faces for a restrained bubble highlight.
            AddPill(width, height, 1f, emptyColor, emptyColor, .001f);
            AddPill(width - height * .24f, height * .7f, fraction, upperColor, lowerColor, 0f);
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        }

        private void AddPill(float w, float h, float fraction, Color top, Color bottom, float z)
        {
            w = Mathf.Max(w, h);
            float left = -w * .5f, end = left + w * fraction, radius = h * .5f;
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float a = left + w * i / segments;
                if (a >= end) break;
                float b = Mathf.Min(left + w * (i + 1) / segments, end);
                float ya = EdgeHeight(a, w, radius), yb = EdgeHeight(b, w, radius);
                AddQuad(a, b, 0f, 0f, ya, yb, z, top);
                AddQuad(a, b, -ya, -yb, 0f, 0f, z, bottom);
            }
        }

        private static float EdgeHeight(float x, float width, float radius)
        {
            float cap = Mathf.Max(0f, Mathf.Abs(x) - (width * .5f - radius));
            return Mathf.Sqrt(Mathf.Max(0f, radius * radius - cap * cap));
        }

        private void AddQuad(float a, float b, float lowA, float lowB, float highA, float highB, float z, Color color)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(a, lowA, z)); vertices.Add(new Vector3(a, highA, z));
            vertices.Add(new Vector3(b, highB, z)); vertices.Add(new Vector3(b, lowB, z));
            for (int i = 0; i < 4; i++) colors.Add(color);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private void OnDisable() { if (display != null) display.enabled = false; }
        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (display != null) Destroy(display.gameObject);
        }
    }
}
