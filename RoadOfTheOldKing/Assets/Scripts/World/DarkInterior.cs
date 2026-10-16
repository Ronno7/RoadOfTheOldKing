using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // One world-space cover mesh, drawn after all world sprites and before screen UI.
    // Static, axis-aligned masonry bounds clip the lantern's visibility fan.
    [DisallowMultipleComponent, DefaultExecutionOrder(300)]
    public sealed class DarkInterior : MonoBehaviour
    {
        [SerializeField] private Shader darknessShader;
        [SerializeField] private BoxCollider2D[] walls = System.Array.Empty<BoxCollider2D>();
        [SerializeField] private Vector2 daylightCenter = new Vector2(0, -.5f);
        [SerializeField] private Vector2 daylightHalfSize = new Vector2(1.75f, 2f);
        [SerializeField, Min(.1f)] private float daylightFeather = .75f;
        [SerializeField, Min(.1f)] private float innerRadius = 2.5f;
        [SerializeField, Min(.2f)] private float outerRadius = 5.5f;
        private static DarkInterior active;
        private Transform player;
        private GameObject cover;
        private Mesh mesh;
        private Material material;
        private readonly List<float> angles = new List<float>(1024);
        private readonly List<Vector3> vertices = new List<Vector3>(8192);
        private readonly List<Vector2> uvs = new List<Vector2>(8192);
        private readonly List<int> indices = new List<int>(12288);
        private Vector2 origin;
        private bool hasLantern, presented;
        private const float CoverDistance = 256f;

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
            Vector2 position = player.position;
            if (!presented || position != origin || owned != hasLantern) Present(position, owned);
        }

        // Also usable by staged Edit Mode captures; never changes ownership or saves.
        public void Present(Vector2 position, bool lantern)
        {
            if (darknessShader == null) return;
            if (mesh == null)
            {
                mesh = new Mesh { name = "Interior visibility cover", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                material = new Material(darknessShader) { hideFlags = HideFlags.DontSave };
                cover = new GameObject("Interior darkness", typeof(MeshFilter), typeof(MeshRenderer));
                cover.hideFlags = HideFlags.DontSave;
                cover.transform.SetParent(transform, false);
                cover.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = cover.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = 32760;
            }
            origin = position; hasLantern = lantern; presented = true;
            angles.Clear(); vertices.Clear(); uvs.Clear(); indices.Clear();
            for (int i = 0; i < 128; i++) angles.Add(i * Mathf.PI * 2 / 128);
            if (lantern) foreach (var wall in walls)
            {
                if (wall == null || !wall.enabled || !wall.gameObject.activeInHierarchy) continue;
                var b = wall.bounds;
                if (Vector2.Distance(b.ClosestPoint(position), position) > outerRadius) continue;
                Corner(new Vector2(b.min.x, b.min.y)); Corner(new Vector2(b.max.x, b.min.y));
                Corner(new Vector2(b.min.x, b.max.y)); Corner(new Vector2(b.max.x, b.max.y));
            }
            angles.Sort();
            for (int i = 0; i < angles.Count; i++)
            {
                Vector2 a = Direction(angles[i]), b = Direction(angles[(i + 1) % angles.Count]);
                Vector2 nearA = position + a * (lantern ? Reach(a) : .01f);
                Vector2 nearB = position + b * (lantern ? Reach(b) : .01f);
                int start = vertices.Count;
                Vertex(position, 1); Vertex(nearA, 1); Vertex(nearB, 1);
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
                start = vertices.Count;
                Vertex(nearA, 0); Vertex(position + a * CoverDistance, 0);
                Vertex(position + b * CoverDistance, 0); Vertex(nearB, 0);
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
                indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(indices, 0);
            material.SetVector("_Origin", new Vector4(position.x, position.y, innerRadius, outerRadius));
            material.SetVector("_Daylight", new Vector4(daylightCenter.x, daylightCenter.y, daylightHalfSize.x, daylightHalfSize.y));
            material.SetFloat("_Feather", daylightFeather);
            material.SetFloat("_Lantern", lantern ? 1 : 0);
        }

        private void Corner(Vector2 point)
        {
            float a = Mathf.Atan2(point.y - origin.y, point.x - origin.x);
            for (int i = -1; i <= 1; i++) angles.Add(Mathf.Repeat(a + i * .0001f, Mathf.PI * 2));
        }
        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        private void Vertex(Vector2 world, float inside)
        {
            vertices.Add(transform.InverseTransformPoint(world)); uvs.Add(new Vector2(inside, 0));
        }
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
                    // Reveal a shallow masonry face so collision reads as stone, not a void.
                    // Stop within the blocker even when it is thinner than the face allowance.
                    nearest = Mathf.Min(nearest, enter + Mathf.Min(.375f, (exit - enter) * .5f));
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

        public float VisibilityAt(Vector2 point)
        {
            float edge = Mathf.Max(Mathf.Abs(point.x - daylightCenter.x) - daylightHalfSize.x,
                Mathf.Abs(point.y - daylightCenter.y) - daylightHalfSize.y);
            float daylight = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / daylightFeather));
            Vector2 delta = point - origin;
            float distance = delta.magnitude;
            float light = hasLantern && distance <= Reach(distance > 0 ? delta / distance : Vector2.right) ?
                1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(innerRadius, outerRadius, distance)) : 0;
            return Mathf.Max(daylight, light);
        }
        public static bool CanRead(Vector2 point) => active == null || active.VisibilityAt(point) >= .45f;

        private void OnDisable()
        {
            if (active == this) active = null;
            Dispose(cover); Dispose(mesh); Dispose(material);
            cover = null; mesh = null; material = null; presented = false;
        }
        private static void Dispose(Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
