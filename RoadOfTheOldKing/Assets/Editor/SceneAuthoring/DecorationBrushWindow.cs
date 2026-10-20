using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    // Paint existing prefabs into a chosen parent. No scene-wide regeneration or replacement.
    public sealed class DecorationBrushWindow : EditorWindow
    {
        [SerializeField] List<GameObject> prefabs = new List<GameObject>();
        [SerializeField] List<Tilemap> exclusions = new List<Tilemap>();
        [SerializeField] Transform parent;
        [SerializeField] float radius = 2f, spacing = 1f, clearance = .25f;
        [SerializeField] int amount = 3;
        bool painting;
        int seed = 1, undoGroup = -1;
        Vector2 lastStamp;
        Vector2 scroll;

        [MenuItem("Road of the Old King/Decoration Brush", priority = 20)]
        public static void Open() => GetWindow<DecorationBrushWindow>("Decoration Brush");
        void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;
        void OnDisable() { SceneView.duringSceneGui -= DuringSceneGUI; FinishStroke(); }

        bool Ready => parent != null && parent.gameObject.scene == SceneManager.GetActiveScene()
            && !EditorUtility.IsPersistent(parent) && prefabs.Any(ValidPrefab)
            && Vector3.Distance(parent.lossyScale, Vector3.one) < .0001f && Quaternion.Angle(parent.rotation, Quaternion.identity) < .001f
            && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
            && PrefabStageUtility.GetCurrentPrefabStage() == null;

        static bool ValidPrefab(GameObject prefab) => prefab != null && PrefabUtility.IsPartOfPrefabAsset(prefab);

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            parent = (Transform)EditorGUILayout.ObjectField("Place under", parent, typeof(Transform), true);
            EditorGUILayout.HelpBox("Select prefab assets in the Project window, then add them below. Choose an unrotated, unit-scale scene parent for your decoration. Ordinary tiles use Unity's Tile Palette.", MessageType.Info);
            if (GUILayout.Button("Add selected prefabs"))
                foreach (var go in Selection.objects.OfType<GameObject>().Where(ValidPrefab))
                    if (!prefabs.Contains(go)) prefabs.Add(go);
            for (int i = 0; i < prefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                prefabs[i] = (GameObject)EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);
                if (GUILayout.Button("Remove", GUILayout.Width(65))) { prefabs.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            radius = EditorGUILayout.Slider("Brush radius", radius, .25f, 8f);
            amount = EditorGUILayout.IntSlider("Pieces per stamp", amount, 1, 12);
            spacing = EditorGUILayout.Slider("Minimum spacing", spacing, .25f, 5f);
            clearance = EditorGUILayout.Slider(new GUIContent("Solid clearance", "Clear space around each placement origin. Increase for larger props."), clearance, .0625f, 3f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Excluded tilemaps (paths, water, clearings)", EditorStyles.boldLabel);
            if (GUILayout.Button("Add exclusion map")) exclusions.Add(null);
            for (int i = 0; i < exclusions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                exclusions[i] = (Tilemap)EditorGUILayout.ObjectField(exclusions[i], typeof(Tilemap), true);
                if (GUILayout.Button("Remove", GUILayout.Width(65))) { exclusions.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            using (new EditorGUI.DisabledScope(!Ready))
                painting = GUILayout.Toggle(painting, "Paint in Scene view", "Button");
            if (!Ready) painting = false;
            EditorGUILayout.HelpBox("In 2D Scene view, green dots preview placement origins. Click or drag to add; Escape stops painting. Each stroke supports Undo. Existing objects are never replaced. Spacing checks this parent's children; solid colliders and assigned exclusion maps are avoided. After placement, update vegetation and shadows in Scene Tools.", MessageType.None);
            EditorGUILayout.EndScrollView();
            if (GUI.changed) SceneView.RepaintAll();
        }

        internal static List<Vector2> Candidates(Vector2 center, float radius, float spacing, int amount, int seed,
            Transform parent, IEnumerable<Tilemap> exclusions, float clearance)
        {
            var result = new List<Vector2>();
            var random = new System.Random(seed);
            var existing = parent.Cast<Transform>().Select(t => (Vector2)t.position).ToArray();
            var maps = exclusions.Where(m => m != null).ToArray();
            Physics2D.SyncTransforms();
            for (int attempt = 0; attempt < amount * 30 && result.Count < amount; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
                var p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                p = new Vector2(Mathf.Round(p.x * 16) / 16, Mathf.Round(p.y * 16) / 16);
                if ((p - center).sqrMagnitude > radius * radius) continue;
                if (existing.Concat(result).Any(other => (other - p).sqrMagnitude < spacing * spacing)) continue;
                if (Physics2D.OverlapCircleAll(p, clearance).Any(c => !c.isTrigger && c.gameObject.scene == parent.gameObject.scene)) continue;
                if (maps.Any(map => Excluded(map, p, clearance))) continue;
                result.Add(p);
            }
            return result;
        }

        static bool Excluded(Tilemap map, Vector2 point, float clearance)
        {
            // Sample all cells touched by the clearance square, including non-unit grids.
            var min = map.WorldToCell(point - Vector2.one * clearance);
            var max = map.WorldToCell(point + Vector2.one * clearance);
            for (int y = Mathf.Min(min.y, max.y); y <= Mathf.Max(min.y, max.y); y++)
                for (int x = Mathf.Min(min.x, max.x); x <= Mathf.Max(min.x, max.x); x++)
                    if (map.HasTile(new Vector3Int(x, y, min.z))) return true;
            return false;
        }

        void DuringSceneGUI(SceneView view)
        {
            if (!painting || !Ready || !view.in2DMode) { FinishStroke(); return; }
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            { painting = false; FinishStroke(); e.Use(); Repaint(); return; }
            if (e.type == EventType.MouseUp && e.button == 0) FinishStroke();
            if (e.alt) return;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!new Plane(Vector3.forward, new Vector3(0, 0, parent.position.z)).Raycast(ray, out float distance)) return;
            Vector2 center = ray.GetPoint(distance);
            var points = Candidates(center, radius, spacing, amount, seed, parent, exclusions, clearance);
            if (e.type == EventType.Repaint)
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(new Vector3(center.x, center.y, parent.position.z), Vector3.forward, radius);
                foreach (var p in points) Handles.DrawSolidDisc(new Vector3(p.x, p.y, parent.position.z), Vector3.forward, .08f);
            }
            if (e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                if (e.type == EventType.MouseDown)
                { Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Paint decoration"); }
                if (undoGroup >= 0 && (e.type == EventType.MouseDown || Vector2.Distance(center, lastStamp) >= spacing))
                {
                    Place(points);
                    lastStamp = center;
                }
                e.Use();
            }
            if (e.type == EventType.MouseUp && e.button == 0) { FinishStroke(); e.Use(); }
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) view.Repaint();
        }

        internal void Place(List<Vector2> points)
        {
            if (!Ready) throw new System.InvalidOperationException("Choose prefabs and a unit-scale parent in the active scene, outside Play or Prefab Mode.");
            var pool = prefabs.Where(ValidPrefab).ToArray();
            foreach (var p in points)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(pool[(seed - 1) % pool.Length], parent);
                instance.transform.position = new Vector3(p.x, p.y, parent.position.z);
                // The existing environment contract sorts upper pieces by the placed root's feet.
                foreach (var renderer in instance.GetComponentsInChildren<TilemapRenderer>(true))
                    if (renderer.name == "Above Player" && renderer.sortingLayerName == "Player")
                    {
                        renderer.sortingOrder = 500 - Mathf.RoundToInt(4 * p.y);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Paint decoration");
                seed++;
            }
            if (points.Count > 0) EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
        }

        void FinishStroke()
        {
            if (undoGroup < 0) return;
            Undo.CollapseUndoOperations(undoGroup);
            undoGroup = -1;
        }
    }
}
