#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoadOfTheOldKing.UI
{
    // F4's "Show collision": outlines every collider in view during play and names what the player is touching.
    // Refreshes a few times a second and keeps running while paused, so it also works with F4 open.
    public sealed class DevCollisionOverlay : MonoBehaviour
    {
        private const float RefreshSeconds = .2f, LineWidth = .0625f, ViewMargin = 1f;
        private const int CircleSides = 12;
        private static readonly Color SolidColor = new Color(1f, .25f, .2f), TriggerColor = new Color(1f, .85f, .2f, .55f),
            BodyColor = new Color(.3f, .85f, 1f), TouchingColor = Color.white;

        public string Touching { get; private set; } = "";

        private sealed class Outline { public int ShapeCount; public readonly List<Vector2> Points = new List<Vector2>(); }

        private readonly Dictionary<Collider2D, Outline> staticOutlines = new Dictionary<Collider2D, Outline>();
        private readonly PhysicsShapeGroup2D shapes = new PhysicsShapeGroup2D();
        private readonly List<Collider2D> inView = new List<Collider2D>();
        private readonly List<Vector2> shapePoints = new List<Vector2>();
        private readonly Collider2D[] contacts = new Collider2D[8];
        private readonly HashSet<Collider2D> touching = new HashSet<Collider2D>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();
        private ContactFilter2D everything;
        private DevRuntimeCommands commands;
        private GameObject holder;
        private Mesh mesh;
        private float refreshAt;

        private void Awake()
        {
            commands = GetComponent<DevRuntimeCommands>();
            everything = new ContactFilter2D { useTriggers = true };
            everything.NoFilter();
            mesh = new Mesh { name = "Collision outlines", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            holder = new GameObject("Collision outlines");
            holder.transform.SetParent(transform, false);
            holder.AddComponent<MeshFilter>().sharedMesh = mesh;
            var view = holder.AddComponent<MeshRenderer>();
            view.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            view.sortingLayerName = "Player";
            // Over the fog and enemy world UI.
            view.sortingOrder = 30006;
        }

        private void OnEnable() { refreshAt = 0f; if (holder != null) holder.SetActive(true); }
        private void OnDisable() { Touching = ""; if (holder != null) holder.SetActive(false); }
        private void OnDestroy() { if (mesh != null) Destroy(mesh); }

        private void LateUpdate()
        {
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + RefreshSeconds;
            var cam = Camera.main;
            if (cam == null) return;
            ReadTouching();
            float halfHeight = cam.orthographicSize + ViewMargin, halfWidth = cam.orthographicSize * cam.aspect + ViewMargin;
            Vector2 center = cam.transform.position;
            Vector2 min = center - new Vector2(halfWidth, halfHeight), max = center + new Vector2(halfWidth, halfHeight);
            vertices.Clear(); colors.Clear(); triangles.Clear();
            Physics2D.OverlapArea(min, max, everything, inView);
            foreach (var collider in inView)
            {
                if (collider == null) continue;
                var outline = OutlineOf(collider);
                var color = touching.Contains(collider) ? TouchingColor : collider.isTrigger ? TriggerColor :
                    IsBody(collider) ? BodyColor : SolidColor;
                for (int i = 0; i + 1 < outline.Count; i += 2) AddLine(outline[i], outline[i + 1], color, min, max);
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.bounds = new Bounds(center, new Vector3(halfWidth, halfHeight, 1f) * 2f);
        }

        private void ReadTouching()
        {
            touching.Clear();
            Touching = "";
            if (commands == null || !commands.HasPlayer) return;
            var names = new List<string>();
            foreach (var body in commands.Player.GetComponentsInChildren<Collider2D>())
            {
                if (body.isTrigger) continue;
                int count = body.GetContacts(contacts);
                for (int i = 0; i < count; i++)
                {
                    if (contacts[i].isTrigger || !touching.Add(contacts[i])) continue;
                    var parent = contacts[i].transform.parent;
                    names.Add(contacts[i].name + (parent != null ? " <" + parent.name + ">" : ""));
                }
            }
            Touching = string.Join(", ", names);
        }

        private static bool IsBody(Collider2D collider) =>
            collider.attachedRigidbody != null && collider.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic;

        // A world collider can hold thousands of shapes and doesn't move, so its outline is kept until its shapes change.
        private List<Vector2> OutlineOf(Collider2D collider)
        {
            bool keeps = !IsBody(collider);
            if (keeps && staticOutlines.TryGetValue(collider, out var kept) && kept.ShapeCount == collider.shapeCount)
                return kept.Points;
            var outline = new Outline { ShapeCount = collider.shapeCount };
            collider.GetShapes(shapes);
            var toWorld = shapes.localToWorldMatrix;
            for (int s = 0; s < shapes.shapeCount; s++)
            {
                var shape = shapes.GetShape(s);
                shapes.GetShapeVertices(s, shapePoints);
                for (int v = 0; v < shapePoints.Count; v++) shapePoints[v] = toWorld.MultiplyPoint3x4(shapePoints[v]);
                switch (shape.shapeType)
                {
                    case PhysicsShapeType2D.Circle:
                        AddCircle(outline.Points, shapePoints[0], shape.radius);
                        break;
                    case PhysicsShapeType2D.Capsule:
                        AddCircle(outline.Points, shapePoints[0], shape.radius);
                        AddCircle(outline.Points, shapePoints[1], shape.radius);
                        break;
                    case PhysicsShapeType2D.Polygon:
                        for (int v = 0; v < shapePoints.Count; v++)
                        {
                            outline.Points.Add(shapePoints[v]);
                            outline.Points.Add(shapePoints[(v + 1) % shapePoints.Count]);
                        }
                        break;
                    default:
                        for (int v = 0; v + 1 < shapePoints.Count; v++)
                        {
                            outline.Points.Add(shapePoints[v]);
                            outline.Points.Add(shapePoints[v + 1]);
                        }
                        break;
                }
            }
            if (keeps) staticOutlines[collider] = outline;
            return outline.Points;
        }

        private static void AddCircle(List<Vector2> points, Vector2 center, float radius)
        {
            for (int i = 0; i < CircleSides; i++)
            {
                float a = i * Mathf.PI * 2f / CircleSides, b = (i + 1) * Mathf.PI * 2f / CircleSides;
                points.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                points.Add(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius);
            }
        }

        private void AddLine(Vector2 a, Vector2 b, Color color, Vector2 min, Vector2 max)
        {
            if (Mathf.Max(a.x, b.x) < min.x || Mathf.Min(a.x, b.x) > max.x || Mathf.Max(a.y, b.y) < min.y || Mathf.Min(a.y, b.y) > max.y) return;
            Vector2 along = b - a;
            if (along.sqrMagnitude < .000001f) return;
            Vector2 side = new Vector2(-along.y, along.x).normalized * (LineWidth * .5f);
            int start = vertices.Count;
            vertices.Add(a - side); vertices.Add(a + side); vertices.Add(b + side); vertices.Add(b - side);
            for (int i = 0; i < 4; i++) colors.Add(color);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }
    }
}
#endif
